using TumbangPreso.Abilities;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE WATER WALL OF ILYAS, DRAWN AS FALLING WATER. It is behind a switch (`RafiWaterVisual.NewWall`); the folded
    /// sheet it replaces is still there, one word away.
    ///
    /// ⚠️ THE WALL ITSELF IS NOT MADE HERE. It is the effects study's (`FxStudyRafiWall`), which takes its width, height,
    /// gather and life from `RafiRules`. This CALLS that, once, at its own size, and changes nothing of it. The one
    /// thing that differs from the bench is how much of it is light laid on (`DayLight`), as with Amihan's whirlwind.
    ///
    /// ⚠️ WHY IT IS AN OBJECT OF ITS OWN, AND OUTLIVES THE FIELD. The wall stands until the rule lets go of it (4 s, or
    /// the moment a slipper breaks it) and only THEN falls and splashes, so that it is never seen coming down while
    /// it still blocks. But the field that owns the rule destroys itself, and everything under it, at that same
    /// moment. So in a match the wall is not under the field: it stands beside it, is posed by it while it lives, is
    /// told when it has gone (`Release`), and then runs the fall and the splash on its own clock and removes itself.
    /// A field that goes early (the round ended) takes the wall with it at once.
    ///
    /// ⚠️ LOOK ONLY. No collider, no registry, nothing on the wire, nothing that decides anything. Which side a broken
    /// wall bursts to is not known to every peer (the field's state carries when it broke, not from where), so in a
    /// match it bursts both ways alike.
    ///
    /// In a replay (`RecordedFieldView`) and in a film there is no field: the wall hangs under whoever built it, so it
    /// is hidden and thrown away with them, and it is only ever as old as it is posed. A replay's field is gone the
    /// moment the recorded one is, so a replay shows the wall standing and a break, and not the fall after a full life.
    /// </summary>
    public sealed class RafiWallCurtain : MonoBehaviour
    {
        /// <summary>A film or a replay has no main camera: it says where the lens is, and the drops turn to it.</summary>
        public static Vector3? StagedEye;
        /// <summary>⚠️ FOR A FILM ONLY, read when one is built: how much of it is light laid on (below 0 is `DayLight`).</summary>
        public static float StagedLight = -1f;
        // ⚠️ HOW MUCH OF IT IS LIGHT IN A MATCH. On the dark bench 0.45 of the body is light laid on. Light laid on a sunlit
        // road adds almost nothing and goes pale, so in a match the share is this and the rest of each piece COVERS what
        // is behind it. It is the bench's share cut by what the approved whirlwind's was cut by (0.85 to 0.50).
        public const float DayLight = 0.27f;
        /// <summary>How long after the rule has let go of the wall it is still drawn: its fall and its splash.</summary>
        public const float TailSeconds = FxStudyRafiWall.Fall + FxStudyRafiWall.Splash;

        private FxStudyRafiWall _wall;
        private WorldEffectSnapshot.Field _state;
        private float _age;
        private bool _ownLife, _released;
        private int _round;
        private long _epoch;

        public static RafiWallCurtain Build(RafiWaterVisual owner, WorldEffectSnapshot.Field state)
        {
            var go = new GameObject("IlyasWaterWall");
            // Only a wall with a live field over it has to outlive anything.
            bool ownLife = Application.isPlaying && owner.GetComponentInParent<RafiWaterField>() != null;
            if (ownLife) go.transform.SetPositionAndRotation(owner.transform.position, owner.transform.rotation);
            else go.transform.SetParent(owner.transform, false);
            var curtain = go.AddComponent<RafiWallCurtain>();
            curtain.enabled = false;
            curtain._ownLife = ownLife; curtain._state = state;
            if (ownLife) { curtain._round = GameServices.Match?.RoundNumber ?? 0; curtain._epoch = MatchRpc.Instance?.PresentationMatchId ?? 0; }
            var look = FxStudyRafiWall.Look.Bench;
            look.Light = StagedLight >= 0f ? StagedLight : DayLight;
            curtain._wall = new FxStudyRafiWall(go.transform, look);
            // An effect, not a prop: the ground queries and the prop checks pass over it.
            if (RafiWaterVisual.WallGlow) curtain.BuildGlow();
            foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>(true)) VfxRenderTag.Attach(renderer.gameObject);
            return curtain;
        }

        /// <summary>
        /// Poses it for the field's age. ⚠️ A PURE FUNCTION OF THAT AGE AND THE STATE: nothing is kept from the frame
        /// before, so a replay can scrub it and every peer draws the same frame. An age past the field's life is the
        /// fall and the splash.
        /// </summary>
        public void Pose(float seconds, WorldEffectSnapshot.Field state)
        {
            if (_wall == null) return;
            _state = state; _age = seconds;
            Vector3 eye;
            if (StagedEye.HasValue) eye = StagedEye.Value;
            else { var lens = Camera.main; eye = lens != null ? lens.transform.position : transform.position + new Vector3(0f, 1.6f, -6f); }
            _wall.Pose(seconds, eye, state.Split ? state.FirstScale : state.Duration);
            SyncGlow(seconds);
            SyncLight(seconds);
        }

        /// <summary>The field has gone, at this age. A wall that stood its whole life now falls by its own clock; one whose
        /// field went early goes with it.</summary>
        public void Release(float seconds, WorldEffectSnapshot.Field state)
        {
            if (!_ownLife || this == null) return;
            if (seconds < state.Duration - 0.05f) { Destroy(gameObject); return; }
            _state = state; _age = seconds; _released = true; enabled = true;
        }

        private void Update()
        {
            if (!_released) return;
            // ⚠️ The current's test for a round that has moved on (`RafiCurrentStream`), which this wall lacked: a wall let go
            // in the last second of a round could fall and splash on into the next one.
            if ((GameServices.Round != null && !GameServices.Round.RoundActive)
                || (GameServices.Match?.RoundNumber ?? 0) != _round
                || (MatchRpc.Instance?.PresentationMatchId ?? 0) != _epoch)
            { Destroy(gameObject); return; }
            _age += Time.deltaTime;
            float letGo = _state.Split ? _state.FirstScale : _state.Duration;
            if (_age >= letGo + TailSeconds) { Destroy(gameObject); return; }
            Pose(_age, _state);
        }

        /// <summary>Frees the wall's own meshes and materials. The pictures it wears are everybody's.</summary>
        public void Free() { _wall?.Dispose(); _wall = null; _glow.Clear(); if (_seamMesh != null) { if (Application.isPlaying) Destroy(_seamMesh); else DestroyImmediate(_seamMesh); _seamMesh = null; } }

        // ---------------------------------------------------------------- the lit accents (`RafiWaterVisual.WallGlow`)
        //
        // ⚠️ THE WALL ITSELF IS UNTOUCHED. The owner, of the k1 film: "can the waterwall have some emmissives on it". So three
        // accents are laid OVER the bench's own pieces, never in place of them: a lit twin of its `lines` (glowing threads
        // running down the sheet), a lit twin of its `lip` (light along the foam crest), and a soft cyan seam on the ground
        // under its line. Each twin is a child of the piece it lights, wears that piece's own mesh, and copies that piece's
        // clock, opacity, erosion, tiling and scroll every frame, so it stands, runs, falls and is eaten exactly with it and
        // can never be seen where the wall is not. Only the twin's picture cut and its colours differ (`RafiWaterPaint.Glow*`).
        private sealed class Glow
        {
            public Renderer Source; public MeshRenderer Lit; public readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();
        }
        private static readonly string[] Copied = { "_Age", "_Opacity", "_Erode", "_Window" };
        private static readonly string[] CopiedVectors = { "_Tile", "_Scroll", "_ErodeTile", "_ErodeScroll", "_Warp" };
        private readonly System.Collections.Generic.List<Glow> _glow = new System.Collections.Generic.List<Glow>();
        private MeshRenderer _seam;
        private Renderer _seamSource;
        private Mesh _seamMesh;
        // ⚠️ MADE IN `BuildGlow`, NEVER IN A FIELD INITIALISER. Unity refuses `new MaterialPropertyBlock()` in a
        // MonoBehaviour's constructor or instance field initialiser: it reports the refusal and leaves the field null, so
        // the first frame the seam lit threw a NullReferenceException out of every wall's `StepTo`, in a match and in the
        // court film alike (rafikit k2). The twins' blocks are safe where they are: `Glow` is a plain class.
        private MaterialPropertyBlock _seamBlock;

        private void BuildGlow()
        {
            foreach (var source in GetComponentsInChildren<MeshRenderer>(true))
            {
                Material lit = source.name == "lines" ? RafiWaterPaint.GlowLines : source.name == "lip" ? RafiWaterPaint.GlowCrest : null;
                var filter = source.GetComponent<MeshFilter>();
                if (lit == null || filter == null || filter.sharedMesh == null) continue;
                var go = new GameObject(source.name + "-lit") { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(source.transform, false);
                // A hair in front of the piece it lights, toward the near side, so the two never fight for the same pixel.
                go.transform.localPosition = new Vector3(0f, 0f, -0.006f);
                go.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = lit; renderer.receiveShadows = false;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _glow.Add(new Glow { Source = source, Lit = renderer });
                if (source.name == "lines") _seamSource = source;
            }
            // The seam: a thin band lying along the wall's foot, its full 4 m, lit while the sheet stands.
            var knots = new FxStudyKit.Knot[5];
            for (int k = 0; k < knots.Length; k++) knots[k] = new FxStudyKit.Knot(new Vector3(Mathf.Lerp(-FxStudyRafiWall.HalfWide, FxStudyRafiWall.HalfWide, k / 4f), 0f, 0f), 0.16f);
            _seamMesh = FxStudyKit.Sweep(knots, Vector3.up, perKnot: 6, across: 3);
            _seamMesh.name = "IlyasWallSeam";
            var seam = new GameObject("seam-lit") { hideFlags = HideFlags.DontSave };
            seam.transform.SetParent(transform, false);
            seam.transform.localPosition = new Vector3(0f, 0.02f, -0.01f);
            seam.AddComponent<MeshFilter>().sharedMesh = _seamMesh;
            _seam = seam.AddComponent<MeshRenderer>();
            _seam.sharedMaterial = RafiWaterPaint.GlowSeam; _seam.receiveShadows = false;
            _seam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _seam.enabled = false;
            _seamBlock = new MaterialPropertyBlock();
            BuildLight();
        }

        private void SyncGlow(float seconds)
        {
            foreach (var glow in _glow)
            {
                if (glow.Source == null || glow.Lit == null) continue;
                var from = glow.Source.sharedMaterial;
                bool on = glow.Source.enabled && from != null;
                glow.Lit.enabled = on;
                if (!on) continue;
                glow.Block.Clear();
                foreach (string name in Copied) if (from.HasProperty(name)) { if (name == "_Window") glow.Block.SetVector(name, from.GetVector(name)); else glow.Block.SetFloat(name, from.GetFloat(name)); }
                foreach (string name in CopiedVectors) if (from.HasProperty(name)) glow.Block.SetVector(name, from.GetVector(name));
                glow.Lit.SetPropertyBlock(glow.Block);
            }
            if (_seam == null || _seamBlock == null) return;
            // The seam is lit only while the sheet stands over it (the lines' own opacity says so) and goes with it.
            var lines = _seamSource != null ? _seamSource.sharedMaterial : null;
            float opacity = _seamSource != null && _seamSource.gameObject.activeInHierarchy && lines != null ? lines.GetFloat("_Opacity") : 0f;
            float eaten = lines != null ? lines.GetFloat("_Erode") : 1f;
            _seam.enabled = opacity > 0.01f;
            if (!_seam.enabled) return;
            _seamBlock.Clear();
            _seamBlock.SetFloat("_Age", seconds); _seamBlock.SetFloat("_Opacity", Mathf.Clamp01(opacity * 0.9f));
            // Broken into lengths by the cloud map even while whole, so it reads as lit water and not a neon tube.
            _seamBlock.SetFloat("_Erode", Mathf.Max(0.18f, (eaten - 0.4f) / 0.6f));
            _seamBlock.SetVector("_ErodeTile", new Vector4(3f, 1f, 0f, 0f));
            _seam.SetPropertyBlock(_seamBlock);
        }

        // ---------------------------------------------------------------- the light of the body (`RafiWallGlow.shader`)
        //
        // ⚠️ THE OWNER, OF THE k3 FILM: "the wall itself dont look like its glowing". The accents above light only the threads,
        // the crest and a seam, so the sheet between them stayed a flat translucent cyan. Nothing blooms in a match, so a glow
        // in daylight is sold by VALUE and by SPILL: the body is brighter than anything round it, and it colours the ground.
        // Three pieces, each one draw on a mesh built once, everything that moves in the shader off the wall's own clock:
        //   * THE INNER LIGHT, a twin of the sheet on the sheet's own mesh: pale aqua low in the middle and down the streaks,
        //     the sheet's cyan at its edges, breathing, with caustic bands climbing through it. It copies the sheet's clock,
        //     tiling, scroll and erosion, so it runs, falls and tears exactly with the water.
        //   * THE HALO, a card in the wall's plane a little bigger than the wall: a soft fringe outside the edge and a lit rim
        //     inside it. It surges with the wall (it hangs from the same group) and drops with it as it is eaten.
        //   * THE SPILL, a card on the ground: an aqua pool brightest at the foot and gone `SpillReach` out on both sides,
        //     with caustic ripples. The strongest single cue in sunlight that something gives off light.
        // `RafiWaterVisual.WallGlowStrength` scales all three, read every frame.
        private const float HaloPad = 0.50f, HaloReach = 0.36f, SpillReach = 1.40f;
        // How far the pool runs out when the water lands (m), and so how big its card is.
        private const float SpillLanded = 2.05f;
        // The halo's top: the sheet's line tops out at 1.77 m and the crest's points stand 0.02 to 0.15 m under `Tall`.
        private const float HaloTop = FxStudyRafiWall.Tall - 0.05f;
        // The sheet's opacity while it stands (`FxStudyRafiWall.Pose`): what "fully lit" is measured against.
        private const float SheetStands = 0.85f;
        private static Material _innerLook, _haloLook, _spillLook;
        // The k11 variant's (`RafiWaterVisual.WallGlowVariant`), from its own shader so the approved k10 one is untouched.
        private static Material _innerK11, _haloK11, _spillK11;
        private static Mesh _upright, _lying;
        private static readonly int StrengthId = Shader.PropertyToID("_Strength"), QuadId = Shader.PropertyToID("_Quad"), BoxId = Shader.PropertyToID("_Box");
        private static readonly string[] SheetFloats = { "_Age", "_Erode", "_Sweep", "_Soft" };
        private static readonly string[] SheetVectors = { "_Tile", "_Scroll", "_ErodeTile", "_ErodeScroll", "_Warp" };
        private Renderer _sheetSource;
        private Transform _stand;
        private MeshRenderer _inner, _halo, _spill;
        private MaterialPropertyBlock _innerBlock, _haloBlock, _spillBlock;

        // ⚠️ ONE MATERIAL PER PIECE, SHARED BY EVERY WALL, never freed: three for the life of the game. Everything per wall
        // and per frame is in each renderer's block.
        private static Material Look(int mode, int queue, float add, string shaderName = "RafiWallGlow")
        {
            var shader = Resources.Load<Shader>("Shaders/" + shaderName);
            if (shader == null) return null;
            var m = new Material(shader) { name = "Ilyas wall light " + mode, hideFlags = HideFlags.HideAndDontSave, renderQueue = queue };
            m.SetFloat("_Mode", mode); m.SetFloat("_Add", add);
            m.SetColor("_Edge", RafiWaterPaint.GlowEdge); m.SetColor("_Body", RafiWaterPaint.GlowBody);
            // ⚠️ The core is aqua and never white: its red is held down, so even laid over the white threads no pixel of the
            // wall reaches the white the film counts as clipped.
            m.SetColor("_Core", new Color(0.70f, 1.00f, 0.96f));
            var cloud = FxStudyKit.Mask("erode_cloud");
            if (cloud != null) m.SetTexture("_CloudTex", cloud);
            var flow = FxStudyKit.Mask("water_flow");
            if (flow != null) { m.SetTexture("_MaskTex", flow); m.SetTexture("_ErodeTex", flow); }
            return m;
        }

        private MeshRenderer Card(string name, Transform parent, Mesh mesh, Material look)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = look; renderer.receiveShadows = false;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.enabled = false;
            return renderer;
        }

        private void BuildLight()
        {
            // Spill first in the queue (under every piece of water), halo behind the sheet, inner light over the sheet and
            // its lines, under the lit twins, so the threads and the crest stay crisp on top of it.
            if (_innerLook == null) _innerLook = Look(0, 3011, 0f);
            if (_haloLook == null) _haloLook = Look(1, 3005, 0.55f);
            if (_spillLook == null) _spillLook = Look(2, 2999, 0.15f);
            if (_innerLook == null || _haloLook == null || _spillLook == null) return;
            if (_upright == null) _upright = FxStudyKit.Square(false);
            if (_lying == null) _lying = FxStudyKit.Square(true);
            foreach (var source in GetComponentsInChildren<MeshRenderer>(true)) if (source.name == "sheet") { _sheetSource = source; break; }
            if (_sheetSource == null) return;
            _stand = _sheetSource.transform.parent;
            var filter = _sheetSource.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return;
            _inner = Card("sheet-light", _sheetSource.transform, filter.sharedMesh, _innerLook);
            // A hair toward the near side, as the other twins are.
            _inner.transform.localPosition = new Vector3(0f, 0f, -0.004f);

            float wide = 2f * (FxStudyRafiWall.HalfWide + HaloPad), high = HaloTop + HaloPad;
            _halo = Card("halo", _stand, _upright, _haloLook);
            // Behind the sheet's middle (it folds about 0.08 m either way and leans back over its shoulder).
            _halo.transform.localPosition = new Vector3(0f, high * 0.5f, -0.06f);
            _halo.transform.localScale = new Vector3(wide, high, 1f);

            float spillWide = 2f * (FxStudyRafiWall.HalfWide + SpillLanded), spillDeep = 2f * SpillLanded;
            _spill = Card("spill", transform, _lying, _spillLook);
            _spill.transform.localPosition = new Vector3(0f, 0.011f, 0f);
            _spill.transform.localScale = new Vector3(spillWide, 1f, spillDeep);

            _innerBlock = new MaterialPropertyBlock(); _haloBlock = new MaterialPropertyBlock(); _spillBlock = new MaterialPropertyBlock();
            _haloBlock.SetVector(QuadId, new Vector4(wide, high, 0f, high * 0.5f));
            _spillBlock.SetVector(QuadId, new Vector4(spillWide, spillDeep, 0f, 0f));
        }

        private void SyncLight(float seconds)
        {
            if (_inner == null || _sheetSource == null) return;
            float strength = Mathf.Clamp01(RafiWaterVisual.WallGlowStrength);
            // The variant is chosen every frame by which shared material each piece wears; K10 wears exactly what it did.
            bool k11 = RafiWaterVisual.WallGlowVariant == RafiWaterVisual.WallGlowLook.K11;
            if (k11 && _innerK11 == null)
            {
                _innerK11 = Look(0, 3011, 0f, "RafiWallGlowK11"); _haloK11 = Look(1, 3005, 0.55f, "RafiWallGlowK11"); _spillK11 = Look(2, 2999, 0.15f, "RafiWallGlowK11");
            }
            if (k11 && (_innerK11 == null || _haloK11 == null || _spillK11 == null)) k11 = false;
            Wear(_inner, k11 ? _innerK11 : _innerLook); Wear(_halo, k11 ? _haloK11 : _haloLook); Wear(_spill, k11 ? _spillK11 : _spillLook);
            var sheet = _sheetSource.sharedMaterial;
            bool standing = sheet != null && _sheetSource.gameObject.activeInHierarchy;
            float lit = standing ? Mathf.Clamp01(sheet.GetFloat("_Opacity") / SheetStands) : 0f;
            float eaten = standing ? sheet.GetFloat("_Erode") : 1f;
            // How far it has surged up (the group it stands in is scaled from nothing to whole as it gathers).
            float risen = _stand != null ? Mathf.Clamp01(_stand.localScale.y) : 1f;

            bool on = lit > 0.01f && strength > 0.001f;
            _inner.enabled = on;
            if (on)
            {
                _innerBlock.Clear();
                foreach (string name in SheetFloats) _innerBlock.SetFloat(name, sheet.GetFloat(name));
                foreach (string name in SheetVectors) _innerBlock.SetVector(name, sheet.GetVector(name));
                _innerBlock.SetFloat("_Opacity", lit); _innerBlock.SetFloat(StrengthId, strength);
                _inner.SetPropertyBlock(_innerBlock);
            }

            // ⚠️ THE FRINGE GOES QUICKLY AS THE SHEET LETS GO, AND DOES NOT COME DOWN WITH IT. Lowered with the sheet (k7)
            // it was a lit box with a ruled top sinking to the ground, under water that tears into tongues. The tongues
            // are the inner light's, eaten with the sheet; the fringe only has to be gone before they are.
            float haloLit = lit * risen * Mathf.Pow(1f - eaten, 4f);
            _halo.enabled = haloLit > 0.01f && strength > 0.001f;
            if (_halo.enabled)
            {
                _haloBlock.SetFloat("_Age", seconds); _haloBlock.SetFloat("_Opacity", haloLit); _haloBlock.SetFloat(StrengthId, strength);
                _haloBlock.SetVector(BoxId, new Vector4(FxStudyRafiWall.HalfWide, HaloTop, HaloReach, 0f));
                _halo.SetPropertyBlock(_haloBlock);
            }

            // The pool lasts as long as there is water standing over it, and goes as the last of it comes down.
            // ⚠️ AND IT FLARES WHERE THE WATER LANDS. Through k9 the pool went out with the last tongue, so the splash
            // landed on a dark road: the impact had no light. Now the pool swells back as the sheet comes down, runs out
            // further, paler, and dies through the splash (`FxStudyRafiWall.Fall`, `Splash`), the follow-through of the light.
            float letGo = _state.Split ? _state.FirstScale : _state.Duration;
            float landed = seconds - letGo - FxStudyRafiWall.Fall;
            float flare = landed < 0f ? FxStudyKit.Ease(FxStudyKit.Run(landed, -0.14f, 0f)) : 1f - FxStudyKit.Ease(FxStudyKit.Run(landed, 0.05f, FxStudyRafiWall.Splash * 0.9f));
            float reach = Mathf.Lerp(SpillReach, SpillLanded, FxStudyKit.EaseOut(FxStudyKit.Run(landed, -0.05f, 0.45f)));
            float spillLit = Mathf.Max(lit * risen * Mathf.Pow(1f - eaten, 0.7f), 0.85f * flare);
            _spill.enabled = spillLit > 0.01f && strength > 0.001f;
            if (_spill.enabled)
            {
                _spillBlock.SetFloat("_Age", seconds); _spillBlock.SetFloat("_Opacity", spillLit); _spillBlock.SetFloat(StrengthId, strength);
                _spillBlock.SetVector(BoxId, new Vector4(FxStudyRafiWall.HalfWide, flare, reach, 0f));
                _spill.SetPropertyBlock(_spillBlock);
            }
        }
        private static void Wear(MeshRenderer renderer, Material look) { if (renderer != null && renderer.sharedMaterial != look) renderer.sharedMaterial = look; }
        private void OnDestroy() => Free();
    }
}
