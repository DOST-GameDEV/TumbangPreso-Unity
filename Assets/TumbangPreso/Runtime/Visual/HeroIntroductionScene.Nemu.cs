using System;
using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // NEMU, PRUSISYON, 5.8 s (owner brief 2026-10-11; `docs/reports/nemu-ultimate-2026-10-11/direction.md` section 4).
        // The owner: *"below 6 seconds"*, *"make it look very eerie"*, *"the ghosts will all go INSIDE the ppl"*, *"the ghosts
        // are tryna get out of the body but they get sucked in anyways"*, *"use black and purple"*.
        //
        // One sentence: she hushes the world grey, and the dead she is always half-listening to file past in procession and are
        // drawn into every player. One travelling light: a ghost-fire flame, always moving left to right on screen.
        //
        //   A THE HUSH 0.00-1.20, close: gazing off; her eyes come to the lens; finger to her lips (0.40); the world drains and
        //     darkens from her feet out (0.50) under one toll of the death bell; Kuro flinches and hides behind her head; a
        //     ghost-fire flame kindles at her fingertip (0.85) and drifts off screen right.
        //   B THE PROCESSION 1.20-3.30, wide and low, trucking with them: out of a black mist on the left the dead file past
        //     behind her, IN STEP: Lola with her rosary, the Sakristan ringing his bell, the Kandila souls with their flames,
        //     the White Lady a head above them at the back. At 2.95 every head turns to the lens at once.
        //   C THE ENTERING 3.30-5.80, one continuous move: she pinches the air and every flame dies (3.30); the souls tear loose
        //     as wisps and stream out at the real players (3.42); at each body they fight, wheeling round it trying to pull
        //     away, and are SUCKED IN (4.38); the player jolts, a black-violet aura blooms round them, and they slowly turn to
        //     face her; their eyes light violet (5.25). The ink closes in to black (5.55).
        //
        // ⚠️ THE PLAYERS ARE THE REAL ONES (`StageOthers`), animated with the clips their own rigs carry (`RootedMotion`: the
        // breakout's flung arms for the jolt, the struggle while possessed). Nobody on court: the souls dive into the court at
        // three typed spots. Play picks up with them possessed (`CharacterMotor.Possession.cs`): never shown twice.
        // ⚠️ Nothing here runs on Update: every piece is posed from the scene clock. No `Random`: every row is typed.
        // =========================================================================================
        private const float NmHush = .40f, NmGrey = .50f, NmKindle = .85f, NmProcession = 1.20f, NmTurn = 2.95f;
        // Film n1: the hand-back begins 0.4 s before the end (`UltimatePerformance.HandoffLead`), so the payoff is done by 5.4:
        // they turn (4.55), their eyes ignite (4.95), and the world sinks to black round their burning eyes (5.2).
        private const float NmSnuff = 3.30f, NmRelease = 3.42f, NmArrive = 4.06f, NmSuck = 4.38f, NmFace = 4.45f, NmEyes = 4.95f, NmIris = 5.2f;
        private static readonly float[] NmBells = { 1.55f, 2.05f, 2.55f };
        // Film n2: with the mist's edge at 4.4 m most of the line rose after the shot was half over; it is at 7.2 m now, so the
        // line is already walking when the shot opens and only the last few rise out of the dark behind it.
        private const float NmWalkSpeed = 2.0f, NmMistEdge = 7.2f;

        // The black-and-purple family: smoky near-black veils with a violet edge, ghost-fire flames, violet eyes.
        // Film n1: dark veils with a deep rim vanished against the black dome. The dead keep their dark bodies but carry a
        // pale violet edge (the Lady a pale grey one), so every silhouette reads against the dark and against the grey court.
        // Film n4: on small round bodies the rim covers most of the surface, so a bright rim made lilac glass. A dimmer violet
        // edge over a darker body: black-purple figures with a thin light round them.
        private static readonly Color NmVeil = new Color(.08f, .04f, .13f, 1f), NmVeilRim = new Color(.52f, .30f, .84f, 1f);
        private static readonly Color NmLadyVeil = new Color(.22f, .20f, .27f, 1f), NmLadyRim = new Color(.80f, .76f, .88f, 1f);
        private static readonly Color NmFire = new Color(.72f, .46f, 1f, 1f), NmEye = new Color(.70f, .40f, 1f, 1f);
        private static readonly Color NmMist = new Color(.03f, .01f, .05f, .82f), NmMoth = new Color(.05f, .03f, .07f, 1f);

        /// <summary>A FILM OUTSIDE PLAY HAS NO KURO: the editor's cutscene film stands one beside her and names it here.</summary>
        public static GhostPetCompanion FilmCompanion;
        private MatchPoseHistory.Copy _kuro;
        private KuroRagePresentation _rage;
        private Vector3 _nmKuroScale;
        private Quaternion _nmKuroFront;
        private Renderer[] _kuroRenderers;
        // Film n2: at the old seat he filled the close lens beside her face. Higher and behind her shoulder.
        private static readonly Vector3 KuroSeat = new Vector3(-.8f, .95f, -.3f);

        private enum NmKind { Lola, Sakristan, Soul, Lady }

        /// <summary>The procession, typed one by one: (kind, lane z, x at 1.20 s, scale). The leader is furthest along (most
        /// -x, screen right); the souls walk two abreast; the White Lady last and highest.</summary>
        private static readonly (NmKind Kind, float Lane, float X0, float Scale)[] NmRows =
        {
            (NmKind.Lola, -2.5f, 3.3f, 1f), (NmKind.Sakristan, -2.5f, 4.3f, 1f),
            (NmKind.Soul, -2.2f, 5.1f, 1f), (NmKind.Soul, -2.9f, 5.25f, .94f), (NmKind.Soul, -2.2f, 5.9f, 1.04f),
            (NmKind.Soul, -2.9f, 6.05f, .9f), (NmKind.Soul, -2.2f, 6.7f, .97f), (NmKind.Soul, -2.9f, 6.85f, 1.06f),
            (NmKind.Soul, -2.2f, 7.5f, .92f), (NmKind.Soul, -2.9f, 7.65f, 1f), (NmKind.Soul, -2.5f, 8.3f, .95f),
            (NmKind.Lady, -4.1f, 7.6f, 1f),
        };

        private sealed class NmFigure
        {
            public NmKind Kind;
            public Transform Root, Body;
            public Renderer[] Veil;
            public MaterialPropertyBlock Block;
            public int EyeL = -1, EyeR = -1, Flame = -1, Smoke = -1;
            public Transform Prop; // a candle, the bell, the rosary
            public float Lane, X0, Scale, Height;
            // The soul's wisp after the snuff: which target, when it leaves, its trail.
            public int Target = -1, Wisp = -1;
            public float Leave;
            public LineRenderer Trail;
            public readonly Vector3[] TrailPoints = new Vector3[10];
        }

        private sealed class NmVictim
        {
            public GameObject Holder;
            public Transform AnimRoot, Model, Head;
            public Transform[] Bones;
            public Quaternion[] Tmp;
            public Vector3[] TmpPos;
            public AnimationClip Shock, Struggle;
            public Vector3 Home;
            public float Yaw, Top = 1.7f, Width = .7f;
            public int AuraIndex;
            public Renderer AuraRenderer;
            public MaterialPropertyBlock AuraBlock;
            public int EyeL, EyeR, Burst;
        }

        private readonly List<NmFigure> _nmFigures = new List<NmFigure>(12);
        private readonly List<NmVictim> _nmVictims = new List<NmVictim>(3);
        private readonly List<int> _nmNearMoths = new List<int>(8), _nmMists = new List<int>(6);
        private int _nmDome, _nmGreyRing, _nmFingerFlame, _nmFingerGlow, _nmShroud, _nmToll;
        private Vector3 _nmEye = new Vector3(0, 1.3f, 3f);
        private static Shader _nmGhostShader;

        /// <summary>Moths drifting past the lens, typed: (shot start, screen-ish x from, x to, y, depth share of eye-to-focus, size).</summary>
        private static readonly (float From, float X0, float X1, float Y, float Near, float Size)[] NmMothRows =
        {
            (0f, -.9f, 1.1f, .35f, .22f, .16f), (.6f, -1.1f, .9f, -.25f, .30f, .12f), (1.3f, -1.0f, 1.0f, .15f, .25f, .14f),
            (2.0f, -1.2f, .8f, .45f, .35f, .11f), (3.45f, -.8f, 1.2f, -.1f, .28f, .15f), (4.4f, -1.1f, 1.0f, .3f, .32f, .12f),
            (4.9f, -.9f, 1.1f, -.3f, .26f, .13f),
        };
        /// <summary>With nobody on court the souls dive into the court here: (angle off her facing, metres out).</summary>
        private static readonly Vector2[] NmEmptyDives = { new Vector2(-30f, 4.2f), new Vector2(25f, 5.0f), new Vector2(160f, 3.6f) };

        // ------------------------------------------------------------------ build

        private Material NmGhostMaterial(string name, Color veil, Color rim, float alpha, float rimAlpha, float glow, float fadeLow, float fadeHigh)
        {
            if (_nmGhostShader == null) _nmGhostShader = Resources.Load<Shader>("Shaders/ProcessionGhost");
            if (_nmGhostShader == null) return null;
            var m = new Material(_nmGhostShader) { name = name };
            m.SetColor("_Tint", veil); m.SetColor("_RimColor", rim);
            m.SetFloat("_Alpha", alpha); m.SetFloat("_RimAlpha", rimAlpha); m.SetFloat("_Glow", glow);
            m.SetFloat("_FadeLow", fadeLow); m.SetFloat("_FadeHigh", fadeHigh);
            return m;
        }

        private Renderer NmPiece(Transform parent, string name, Mesh mesh, Vector3 at, Vector3 scale, Material ghost, Color solid, float emission = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at; go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            if (ghost != null) { r.sharedMaterial = ghost; VfxRenderTag.Attach(go); }
            else VfxMaterial.Solid(r, solid, emission);
            return r;
        }

        private void BuildNemu(CharacterMotor source)
        {
            // ------------------------------------------------------------- Kuro, retained, calm and frightened
            var companion = source.GetComponent<CharacterVisual>()?.Companion ?? FilmCompanion;
            if (companion == null) throw new InvalidOperationException("Nemu's introduction requires retained Kuro.");
            var track = new MatchPoseHistory.Track(source, companion.gameObject);
            track.Record(0); track.Record(.05f); _kuro = track.Clone(_root.transform);
            if (_kuro == null) throw new InvalidOperationException("Kuro exceeded the render-copy contract.");
            track.Apply(_kuro, .05f); _kuro.Root.SetActive(true);
            _kuro.Root.transform.localPosition = KuroSeat;
            _nmKuroScale = companion.RestScale;
            Vector3 face = companion.MouthPosition - companion.transform.position; face.y = 0;
            Vector3 localFace = companion.transform.InverseTransformDirection(face.normalized);
            _nmKuroFront = localFace.sqrMagnitude > .01f ? Quaternion.FromToRotation(localFace, Vector3.forward) : Quaternion.identity;
            var calm = GhostPetCompanion.FindForm(_kuro.Root.transform, "CalmForm");
            var rageForm = GhostPetCompanion.FindForm(_kuro.Root.transform, "RageForm");
            if (calm == null || rageForm == null) throw new InvalidOperationException("Retained Kuro calm/rage forms are missing.");
            foreach (var bone in _kuro.Bones)
                if (bone.name == "KuroEyeWisp")
                { bone.gameObject.SetActive(false); bone.SetParent(_root.transform, false); ObjectDestroy(bone.gameObject); }
            _rage = new KuroRagePresentation(_kuro.Root, calm, rageForm);
            _kuroRenderers = _kuro.Root.GetComponentsInChildren<Renderer>(true);

            // ------------------------------------------------------------- the hush: the world steps back
            // A dark dome closes over the stage (open sky reads as a hole in the night, Phaister's lesson) and a ring of grey
            // runs out over the court from her feet.
            _nmDome = Wall("NmHushDome", 0f, 14f, new Color(.02f, .01f, .04f, .88f), 15f, 36, .02f, cap: true);
            ZackUnlitBackdrop(_nmDome);
            _nmGreyRing = Add("NmGreyRing", VfxShapes.Collar(36, .02f, .93f), new Color(.30f, .22f, .40f, .7f), .25f);
            // The black mist bank the dead walk out of, on the left, low and layered (each its own size, never a stamp).
            var mistRows = new[] { new Vector4(5.4f, -2.6f, 2.6f, 0f), new Vector4(6.6f, -3.3f, 3.1f, 1f), new Vector4(4.9f, -3.9f, 2.2f, 2f),
                                   new Vector4(7.6f, -2.2f, 2.8f, 3f), new Vector4(6.1f, -4.6f, 3.4f, 4f), new Vector4(8.4f, -3.5f, 2.4f, 5f) };
            foreach (var row in mistRows)
                _nmMists.Add(Add("NmMist" + (int)row.w, VfxShapes.Splat(14, .32f, 300 + (int)row.w), NmMist, .02f, plain: true));
            foreach (var i in _nmMists) ZackUnlitBackdrop(i);

            // Her fingertip flame.
            _nmFingerFlame = Add("NmFingerFlame", ProcessionShapes.Flame, NmFire, 1.4f);
            _nmFingerGlow = AddGlow("NmFingerGlow", NmFire, falloff: 2.6f, core: .9f);

            // ------------------------------------------------------------- the procession
            for (int i = 0; i < NmRows.Length; i++) _nmFigures.Add(BuildFigure(i));

            // ------------------------------------------------------------- moths at the lens
            for (int i = 0; i < NmMothRows.Length; i++)
                _nmNearMoths.Add(Add("NmLensMoth" + i, ProcessionShapes.Wing, NmMoth, 0f, plain: true));
            foreach (var i in _nmNearMoths) ZackUnlitBackdrop(i);

            // ------------------------------------------------------------- the real players
            StageOthers(2.8f, 8.5f);
            for (int i = 0; i < _others.Count; i++) _nmVictims.Add(BuildVictim(_others[i], i));
            // Who each soul goes into: round the players in turn; nobody there, the court.
            int souls = 0;
            foreach (var f in _nmFigures)
            {
                if (f.Kind != NmKind.Soul) continue;
                f.Target = _nmVictims.Count > 0 ? souls % _nmVictims.Count : -1 - (souls % NmEmptyDives.Length);
                f.Leave = NmRelease + .028f * souls;
                f.Wisp = AddGlow("NmWisp" + souls, NmFire, falloff: 2.2f, core: .8f);
                f.Trail = Line("NmWispTrail" + souls, f.TrailPoints.Length, .07f, new Color(.30f, .12f, .52f, .75f));
                souls++;
            }

            // The death bell's toll ring on the court (the motif) and the closing ink.
            _nmToll = Add("NmTollRing", VfxShapes.Collar(30, .02f, .9f), new Color(.40f, .24f, .62f, .6f), .3f);
            _nmShroud = Add("NmInk", VfxShapes.Prism(12, 1, 1), new Color(.01f, 0f, .02f, 1f), 0f, plain: true);
            ZackUnlitBackdrop(_nmShroud);
        }

        private NmFigure BuildFigure(int index)
        {
            var row = NmRows[index];
            var f = new NmFigure { Kind = row.Kind, Lane = row.Lane, X0 = row.X0, Scale = row.Scale, Block = new MaterialPropertyBlock() };
            f.Root = new GameObject("NmFigure" + index + "-" + row.Kind).transform;
            f.Root.SetParent(_root.transform, false);
            f.Body = new GameObject("Body").transform; f.Body.SetParent(f.Root, false);
            bool lady = row.Kind == NmKind.Lady;
            var veil = NmGhostMaterial("NmVeil" + index, lady ? NmLadyVeil : NmVeil, lady ? NmLadyRim : NmVeilRim,
                                       lady ? .78f : .76f, .55f, .8f, -.02f, .18f);
            Mesh mesh = row.Kind == NmKind.Lola ? ProcessionShapes.Lola : row.Kind == NmKind.Sakristan ? ProcessionShapes.Sakristan
                      : lady ? ProcessionShapes.Lady : ProcessionShapes.Soul;
            f.Height = mesh.bounds.max.y;
            var parts = new List<Renderer> { NmPiece(f.Body, "Veil", mesh, Vector3.zero, Vector3.one, veil, default) };
            // Eyes: violet, set unevenly on the face (+x is her right, -x walking direction; faces turn to -x as they walk).
            float eyeY = row.Kind == NmKind.Lola ? .80f : row.Kind == NmKind.Sakristan ? .67f : f.Height - .11f;
            float eyeZ = row.Kind == NmKind.Lola ? .21f : row.Kind == NmKind.Sakristan ? .12f : .135f;
            if (!lady)
            {
                f.EyeL = AddGlow("NmEyeL" + index, NmEye, falloff: 2.8f, core: .9f);
                f.EyeR = AddGlow("NmEyeR" + index, NmEye, falloff: 2.8f, core: .9f);
                _pieces[f.EyeL].Transform.SetParent(f.Body, false); _pieces[f.EyeL].Transform.localPosition = new Vector3(-.055f, eyeY, eyeZ);
                _pieces[f.EyeR].Transform.SetParent(f.Body, false); _pieces[f.EyeR].Transform.localPosition = new Vector3(.06f, eyeY - .01f, eyeZ);
            }
            else
            {
                // The White Lady has no face: a dark hollow where it should be, and her long hair down the front of the veil.
                parts.Add(NmPiece(f.Body, "Hollow", ProcessionShapes.Bead, new Vector3(0f, 2.3f, .15f), new Vector3(.2f, .26f, .06f), null, new Color(.01f, 0f, .02f)));
                var hair = NmGhostMaterial("NmLadyHair", new Color(.01f, .01f, .02f, 1f), new Color(.15f, .1f, .2f, 1f), .7f, .4f, .3f, 1.0f, 1.3f);
                parts.Add(NmPiece(f.Body, "HairL", ProcessionShapes.Sleeve, new Vector3(-.09f, 2.25f, .14f), new Vector3(.9f, .9f, 3.6f), hair, default));
                parts.Add(NmPiece(f.Body, "HairR", ProcessionShapes.Sleeve, new Vector3(.08f, 2.27f, .13f), new Vector3(.8f, .8f, 3.2f), hair, default));
                parts[parts.Count - 1].transform.localRotation = Quaternion.Euler(84f, 0f, 0f);
                parts[parts.Count - 2].transform.localRotation = Quaternion.Euler(86f, 0f, 0f);
            }
            // What each carries.
            if (row.Kind == NmKind.Soul || row.Kind == NmKind.Lola)
            {
                f.Prop = new GameObject("Candle").transform; f.Prop.SetParent(f.Body, false);
                f.Prop.localPosition = row.Kind == NmKind.Lola ? new Vector3(.12f, .52f, .26f) : new Vector3(.02f, .24f, .21f);
                parts.Add(NmPiece(f.Prop, "Wax", ProcessionShapes.Candle, Vector3.zero, Vector3.one, null, new Color(.78f, .74f, .82f), .1f));
                parts.Add(NmPiece(f.Body, "Hands", ProcessionShapes.Bead, f.Prop.localPosition + new Vector3(0f, .03f, -.01f), new Vector3(.12f, .07f, .09f), veil, default));
                f.Flame = AddGlow("NmFlame" + index, NmFire, falloff: 2.4f, core: .85f);
                f.Smoke = Add("NmSmoke" + index, ProcessionShapes.Sleeve, new Color(.22f, .16f, .3f, .5f), .1f);
            }
            if (row.Kind == NmKind.Lola)
            {
                // Her rosary hangs from her hands: nine beads down a loop, then the cross. Each bead its own place.
                var beads = new[] { new Vector3(-.06f, .47f, .27f), new Vector3(-.09f, .42f, .26f), new Vector3(-.1f, .36f, .25f), new Vector3(-.08f, .30f, .25f),
                                    new Vector3(-.03f, .27f, .26f), new Vector3(.03f, .28f, .27f), new Vector3(.07f, .33f, .27f), new Vector3(.09f, .39f, .27f),
                                    new Vector3(.06f, .45f, .27f) };
                foreach (var b in beads) parts.Add(NmPiece(f.Body, "Bead", ProcessionShapes.Bead, b, Vector3.one * .028f, null, new Color(.10f, .06f, .14f), .2f));
                parts.Add(NmPiece(f.Body, "CrossBar", ProcessionShapes.Candle, new Vector3(-.0f, .21f, .27f), new Vector3(.4f, .4f, .4f), null, new Color(.30f, .22f, .40f), .3f));
            }
            if (row.Kind == NmKind.Sakristan)
            {
                f.Prop = new GameObject("Bell").transform; f.Prop.SetParent(f.Body, false);
                f.Prop.localPosition = new Vector3(.2f, .78f, .1f);
                parts.Add(NmPiece(f.Prop, "Bell", ProcessionShapes.Bell, Vector3.zero, Vector3.one * 1.4f, null, new Color(.42f, .36f, .26f), .2f));
                parts.Add(NmPiece(f.Body, "BellHand", ProcessionShapes.Bead, new Vector3(.2f, .76f, .1f), new Vector3(.08f, .07f, .08f), veil, default));
            }
            f.Veil = parts.ToArray();
            // Film n1: at their first size a soul was a speck behind her. They are a third bigger (still child-sized beside her).
            f.Root.localScale = Vector3.one * row.Scale * 1.5f;
            return f;
        }

        private NmVictim BuildVictim(Other other, int index)
        {
            var v = new NmVictim { Holder = other.Holder, Home = other.Home, Yaw = other.Yaw };
            // The live player this copy is (`StageOthers` names its holder by seat), for their rig's own clips.
            CharacterMotor live = null;
            var round = GameServices.Round;
            if (round != null)
                foreach (var p in round.Players)
                    if (p != null && other.Holder.name == "IntroductionOther-P" + (p.PlayerSlot + 1)) { live = p; break; }
            var animator = live != null ? live.GetComponent<CharacterVisual>()?.Model?.GetComponentInChildren<Animator>() : null;
            if (animator != null)
            {
                foreach (var t in other.Holder.GetComponentsInChildren<Transform>(true)) if (t.name == animator.transform.name) { v.AnimRoot = t; break; }
                var set = GeneratedMotionAssets.For(RootedMotion.Folder, DanceClip.ResourceName(animator.transform));
                if (set != null && set.Clips != null)
                    foreach (var clip in set.Clips)
                    {
                        if (clip == null) continue;
                        if (clip.name == RootedMotion.Breakout) v.Shock = clip;
                        else if (clip.name == RootedMotion.Struggle) v.Struggle = clip;
                    }
            }
            if (other.Holder.transform.childCount > 0) v.Model = other.Holder.transform.GetChild(0);
            foreach (var t in other.Holder.GetComponentsInChildren<Transform>(true)) if (t.name == "head") { v.Head = t; break; }
            v.Bones = other.Holder.GetComponentsInChildren<Transform>(true);
            v.Tmp = new Quaternion[v.Bones.Length]; v.TmpPos = new Vector3[v.Bones.Length];
            // Their size, from their own surfaces.
            bool any = false; Bounds b = default;
            foreach (var r in other.Renderers) { if (r == null) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            if (any)
            {
                v.Top = Mathf.Clamp(b.max.y - other.Holder.transform.position.y, .9f, 2.6f);
                v.Width = Mathf.Clamp(Mathf.Max(b.size.x, b.size.z), .4f, 1.2f);
            }
            // The soul inside them: the same black-violet aura play keeps (`PossessedPresence`).
            var aura = NmGhostMaterial("NmAura" + index, new Color(.05f, .01f, .09f, 1f), new Color(.52f, .20f, .86f, 1f), .3f, 1f, 1.1f, 0f, .16f);
            v.AuraRenderer = NmPiece(_root.transform, "NmAura" + index, ProcessionShapes.Soul, Vector3.zero, Vector3.one, aura, default);
            v.AuraBlock = new MaterialPropertyBlock();
            v.EyeL = AddGlow("NmVictimEyeL" + index, NmEye, falloff: 2.6f, core: .9f);
            v.EyeR = AddGlow("NmVictimEyeR" + index, NmEye, falloff: 2.6f, core: .9f);
            v.Burst = Add("NmSuckRing" + index, VfxShapes.Collar(24, .02f, .86f), new Color(.40f, .18f, .66f, .8f), .5f);
            // Hidden until THE ENTERING: switched off at the holder (`SetVisibleForCapture` would turn renderers back on).
            other.Holder.SetActive(false);
            return v;
        }

        // ------------------------------------------------------------------ sample

        private void SampleNemu(float t)
        {
            float present = StagePresence(t, .2f);

            // ------------------------------------------------------------- Kuro: at her shoulder; flinches and hides
            float bob = Mathf.Sin(t * 2.4f) * .05f;
            float hide = Ease(.68f, .9f, t);
            Vector3 hidden = new Vector3(-.35f, 1.15f, -.45f);
            Vector3 kat = Vector3.Lerp(KuroSeat + Vector3.up * bob, hidden + Vector3.up * bob * .3f, hide);
            float flinch = Mathf.Clamp01(1f - Mathf.Abs(t - .7f) / .08f);
            _kuro.Root.transform.localPosition = kat + new Vector3(-.06f, .05f, 0f) * flinch;
            // He peeks out at the procession, then ducks again when they look at the lens.
            float peek = Ease(1.6f, 1.9f, t) * (1f - Ease(NmTurn - .05f, NmTurn + .08f, t));
            _kuro.Root.transform.localRotation = Quaternion.Euler(0f, -30f + 60f * peek, 0f) * _nmKuroFront;
            float shiver = _reducedEffects ? 0f : Mathf.Sin(t * 47f) * .04f * (hide + flinch);
            _kuro.Root.transform.localScale = _nmKuroScale * (1f - .18f * hide) + Vector3.one * shiver * _nmKuroScale.x;
            _rage.Sample(0, t, .12f);

            // ------------------------------------------------------------- the hush
            float grey = Ease(NmGrey, NmGrey + .55f, t);
            Place(_nmGreyRing, Vector3.up * .03f, Vector3.one * Mathf.Lerp(.4f, 15f, grey), Quaternion.identity, grey > 0f ? (1f - grey) * .9f : 0f);
            // The dark holds to the very end (film n1: fading it with the stage showed the plain plaza before the payoff).
            Place(_nmDome, Vector3.zero, new Vector3(15f, 14f, 15f), Quaternion.identity, Ease(NmGrey, NmGrey + .7f, t));
            float toll = Mathf.Clamp01((t - (NmGrey + .05f)) / .9f);
            Place(_nmToll, Vector3.up * .04f, Vector3.one * Mathf.Lerp(.6f, 6f, toll), Quaternion.identity, t > NmGrey ? (1f - toll) * .7f : 0f);
            for (int i = 0; i < _nmMists.Count; i++)
            {
                var rows = new[] { new Vector4(5.4f, -2.6f, 2.6f, 0f), new Vector4(6.6f, -3.3f, 3.1f, 1f), new Vector4(4.9f, -3.9f, 2.2f, 2f),
                                   new Vector4(7.6f, -2.2f, 2.8f, 3f), new Vector4(6.1f, -4.6f, 3.4f, 4f), new Vector4(8.4f, -3.5f, 2.4f, 5f) };
                var r = rows[i];
                float rise = Ease(NmProcession - .5f + .06f * i, NmProcession + .1f + .06f * i, t) * (1f - Ease(NmRelease, NmRelease + .6f, t));
                float drift = _reducedEffects ? 0f : Mathf.Sin(t * .7f + i) * .25f;
                Place(_nmMists[i], new Vector3(r.x + drift, .05f + .02f * i, r.y), new Vector3(r.z, 1f, r.z * .6f), Quaternion.Euler(0f, 20f * i, 0f), rise);
            }

            // Her fingertip flame: it kindles at her raised finger and drifts off screen right (her left, -x).
            float kindle = Ease(NmKindle, NmKindle + .1f, t);
            float drift2 = Ease(1.0f, 1.45f, t);
            Vector3 finger = (_performance != null && _rightHand != null ? RightPalm : new Vector3(.1f, 1.25f, .25f)) + new Vector3(0f, .1f, .08f);
            Vector3 flameAt = Vector3.Lerp(finger, new Vector3(-2.4f, 1.5f, .6f), drift2);
            float flicker = _reducedEffects ? 1f : .85f + .15f * Mathf.Sin(t * 23f);
            Place(_nmFingerFlame, flameAt, Vector3.one * (1.2f * kindle * flicker), Quaternion.identity, kindle * (1f - drift2));
            PlaceGlow(_nmFingerGlow, flameAt + Vector3.up * .05f, Vector3.one * (.32f * kindle), Quaternion.identity, kindle * (1.4f - drift2) * flicker);

            // ------------------------------------------------------------- the procession
            float turn = Ease(NmTurn - .04f, NmTurn + .1f, t);
            float stepPhase = (t - NmProcession) * 1.6f * Mathf.PI * 2f; // every one of them on the same beat
            for (int i = 0; i < _nmFigures.Count; i++) SampleFigure(_nmFigures[i], i, t, turn, stepPhase);

            // ------------------------------------------------------------- the players
            for (int i = 0; i < _nmVictims.Count; i++) SampleVictim(_nmVictims[i], i, t);

            // ------------------------------------------------------------- moths at the lens
            Vector3 focus = new Vector3(0f, 1.1f, 0f);
            for (int i = 0; i < _nmNearMoths.Count; i++)
            {
                var m = NmMothRows[i];
                float u = Mathf.Clamp01((t - m.From) / 1.1f);
                bool flying = t >= m.From && t < m.From + 1.1f;
                Vector3 eye = _nmEye, toward = (focus - eye).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, toward).normalized, up = Vector3.up;
                Vector3 at = eye + toward * (Vector3.Distance(eye, focus) * m.Near) + right * Mathf.Lerp(m.X0, m.X1, u) * .7f + up * (m.Y * .5f + Mathf.Sin(u * 9f) * .05f);
                float beat = _reducedEffects ? 20f : Mathf.Sin(t * 9f * 6.283f + i) * 60f;
                Place(_nmNearMoths[i], at, new Vector3(m.Size * 6f * (1f - Mathf.Abs(beat) / 120f), 1f, m.Size * 6f),
                      Quaternion.LookRotation(right, up) * Quaternion.Euler(0f, 0f, beat), flying ? 1f : 0f);
            }

            // ------------------------------------------------------------- the ink closes
            // The ink is the grade now (`NemuGrade`): the world sinks round their eyes, which keep burning.
            Place(_nmShroud, Vector3.zero, Vector3.one * .01f, Quaternion.identity, 0f);
        }

        private void SampleFigure(NmFigure f, int index, float t, float turn, float stepPhase)
        {
            bool lady = f.Kind == NmKind.Lady;
            float walking = t - NmProcession;
            float x = f.X0 - NmWalkSpeed * Mathf.Max(0f, walking) * (lady ? .9f : 1f);
            // Each rises out of the mist as it reaches the mist's edge (the ones already past it rise as the shot opens).
            float emerge = NmProcession - .25f + Mathf.Max(0f, (f.X0 - NmMistEdge) / NmWalkSpeed);
            float risen = Ease(emerge, emerge + .4f, t);
            // In step: a bob and a sway on the same beat for every one of them. The White Lady glides: no step at all.
            float bob = lady ? Mathf.Sin(t * 1.1f) * .04f : Mathf.Abs(Mathf.Sin(stepPhase * .5f)) * .05f;
            float sway = lady || _reducedEffects ? 0f : Mathf.Sin(stepPhase * .5f) * 4f;
            float sink = (1f - risen) * f.Height * f.Scale * 1.5f;
            // After the snuff: the souls tear loose (handled as wisps); the others dissolve where they stand. The Lady stays
            // watching from the back to the very end.
            float dissolve = f.Kind == NmKind.Soul ? 1f - Ease(f.Leave - .05f, f.Leave + .05f, t)
                           : lady ? 1f - Ease(NmIris - .3f, NmIris, t) : 1f - Ease(NmSnuff + .1f, NmSnuff + .7f, t);
            float shown = risen * dissolve * StagePresence(t, .2f);
            f.Root.localPosition = new Vector3(x, -sink + bob, f.Lane);
            // Walking toward -x; on the turn, every head (the whole body: they have no necks) comes round to the lens.
            float yaw = Mathf.Lerp(-90f, -18f + (index % 3) * 9f, turn);
            f.Root.localRotation = Quaternion.Euler(0f, yaw, sway);
            f.Block.SetFloat("_Presence", shown);
            foreach (var r in f.Veil)
            {
                if (r == null) continue;
                r.SetPropertyBlock(f.Block);
                r.enabled = shown > .01f;
            }
            // Eyes: dim while they walk, FLARE on the turn, die with them.
            if (f.EyeL >= 0)
            {
                float flare = 1f + 2.2f * Mathf.Clamp01(1f - Mathf.Abs(t - (NmTurn + .08f)) / .25f) * turn;
                float eye = shown * (.5f + .5f * turn) * flare;
                var el = _pieces[f.EyeL]; var er = _pieces[f.EyeR];
                PlaceGlow(f.EyeL, el.Transform.localPosition, Vector3.one * .15f, Quaternion.identity, eye * 1.4f);
                PlaceGlow(f.EyeR, er.Transform.localPosition, Vector3.one * .14f, Quaternion.identity, eye * 1.4f);
            }
            // The flame, and its death: snuffed together at 3.30, a thread of smoke rising off each wick.
            if (f.Flame >= 0)
            {
                Vector3 wick = _root.transform.InverseTransformPoint(f.Prop.TransformPoint(new Vector3(0f, .16f, 0f)));
                float alive = shown * (1f - Ease(NmSnuff - .02f, NmSnuff + .04f, t));
                float flare = 1f + 1.2f * Mathf.Clamp01(1f - Mathf.Abs(t - (NmTurn + .08f)) / .2f) * turn;
                float flick = _reducedEffects ? 1f : .8f + .2f * Mathf.Sin(t * 19f + index * 1.7f);
                PlaceGlow(f.Flame, wick + Vector3.up * .04f, Vector3.one * (.3f * f.Scale * flick * flare), Quaternion.identity, alive * 1.3f * flare);
                float smoke = t > NmSnuff ? Mathf.Clamp01(1f - (t - NmSnuff) / .9f) * risen : 0f;
                Place(f.Smoke, wick + Vector3.up * (.12f + (t - NmSnuff) * .4f), new Vector3(.2f, .2f, 1.2f + (t - NmSnuff)), Quaternion.Euler(-90f, 0f, 0f), smoke * .8f);
            }
            // The Sakristan's bell rings on the beat: a swing and a ring of sound on the court.
            if (f.Kind == NmKind.Sakristan && f.Prop != null)
            {
                float swing = 0f;
                foreach (float b in NmBells) swing += Mathf.Sin(Mathf.Clamp01((t - b) / .3f) * Mathf.PI * 2f) * Mathf.Clamp01(1f - (t - b) / .3f) * (t >= b ? 1f : 0f);
                f.Prop.localRotation = Quaternion.Euler(0f, 0f, 28f * swing);
            }
            // The wisp: torn loose at the snuff, streaming to its target, wheeling round them fighting, then sucked in.
            if (f.Kind == NmKind.Soul && f.Wisp >= 0) SampleWisp(f, t);
        }

        /// <summary>Where a soul goes: into a staged player's chest, or (nobody there) into the court at a typed spot.</summary>
        private Vector3 NmTargetPoint(int target, out float width)
        {
            width = .7f;
            if (target >= 0 && target < _nmVictims.Count)
            {
                var v = _nmVictims[target]; width = v.Width;
                return v.Home + Vector3.up * (v.Top * .55f);
            }
            var dive = NmEmptyDives[Mathf.Clamp(-1 - target, 0, NmEmptyDives.Length - 1)];
            return Quaternion.Euler(0f, dive.x, 0f) * Vector3.forward * dive.y;
        }

        private void SampleWisp(NmFigure f, float t)
        {
            float u = (t - f.Leave) / (NmArrive - f.Leave + .05f);
            if (t < f.Leave || t > NmSuck + .14f) { PlaceGlow(f.Wisp, Vector3.zero, Vector3.one * .01f, Quaternion.identity, 0f); f.Trail.enabled = false; return; }
            Vector3 start = new Vector3(f.X0 - NmWalkSpeed * (f.Leave - NmProcession), f.Height * f.Scale * 1.5f * .6f, f.Lane);
            Vector3 target = NmTargetPoint(f.Target, out float width);
            Vector3 at;
            // Violet, not white (film n1: at 1.4 the additive core clipped to white blobs).
            float size = .26f, strength = .75f;
            if (t < NmArrive)
            {
                // The flight: up out of the procession, an arc over toward them, accelerating.
                float k = Mathf.Clamp01(u); float e = k * k * (3f - 2f * k);
                at = Vector3.Lerp(start, target, e) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.4f;
            }
            else
            {
                // The fight at the body: it wheels round them, pulling away, tighter and tighter, then is SUCKED IN.
                float fight = Mathf.Clamp01((t - NmArrive) / (NmSuck - NmArrive));
                float suck = Mathf.Clamp01((t - NmSuck) / .12f);
                float wheel = (t - NmArrive) * 14f + f.Leave * 31f;
                float radius = Mathf.Lerp(width * .9f, width * .45f, fight) * (1f - suck);
                float pull = Mathf.Sin(wheel * .5f) * .25f * (1f - fight);
                at = target + new Vector3(Mathf.Sin(wheel), .25f * Mathf.Sin(wheel * 1.3f) + pull, Mathf.Cos(wheel)) * radius;
                size *= (1f - suck) * (1f + .4f * fight);
                strength *= 1f + .8f * fight;
            }
            PlaceGlow(f.Wisp, at, Vector3.one * size, Quaternion.identity, strength);
            // Its trail: the last ten places it has been along the same path, sampled back in time.
            f.Trail.enabled = true;
            for (int k = 0; k < f.TrailPoints.Length; k++)
            {
                float back = t - k * .025f;
                f.TrailPoints[k] = back <= f.Leave ? start : WispPoint(f, back, start, target, width);
            }
            f.Trail.SetPositions(f.TrailPoints);
            f.Trail.widthMultiplier = .07f * (t > NmSuck ? 1f - Mathf.Clamp01((t - NmSuck) / .12f) : 1f);
        }

        private Vector3 WispPoint(NmFigure f, float t, Vector3 start, Vector3 target, float width)
        {
            if (t < NmArrive)
            {
                float k = Mathf.Clamp01((t - f.Leave) / (NmArrive - f.Leave + .05f)); float e = k * k * (3f - 2f * k);
                return Vector3.Lerp(start, target, e) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.4f;
            }
            float fight = Mathf.Clamp01((t - NmArrive) / (NmSuck - NmArrive));
            float suck = Mathf.Clamp01((t - NmSuck) / .12f);
            float wheel = (t - NmArrive) * 14f + f.Leave * 31f;
            float radius = Mathf.Lerp(width * .9f, width * .45f, fight) * (1f - suck);
            return target + new Vector3(Mathf.Sin(wheel), .25f * Mathf.Sin(wheel * 1.3f), Mathf.Cos(wheel)) * radius;
        }

        private void SampleVictim(NmVictim v, int index, float t)
        {
            bool on = t >= NmSnuff;
            if (v.Holder.activeSelf != on) v.Holder.SetActive(on);
            if (!on)
            {
                v.AuraRenderer.enabled = false;
                PlaceGlow(v.EyeL, Vector3.zero, Vector3.one * .01f, Quaternion.identity, 0f); PlaceGlow(v.EyeR, Vector3.zero, Vector3.one * .01f, Quaternion.identity, 0f);
                return;
            }
            // THE JOLT as the soul goes in, then the struggle: they fight it as the play will make them.
            float jolt = Mathf.Clamp01(1f - Mathf.Abs(t - (NmSuck + .08f)) / .22f);
            float held = Ease(NmSuck + .2f, NmSuck + .5f, t);
            if (v.AnimRoot != null)
            {
                if (v.Shock != null && jolt > 0f) NmBlend(v, v.Shock, .12f + .015f * Mathf.Sin(t * 41f), jolt);
                if (v.Struggle != null && held > 0f) NmBlend(v, v.Struggle, Mathf.Repeat((t - NmSuck) * .9f + .3f * index, v.Struggle.length), held * .75f);
            }
            // They turn, slowly, to face her.
            Vector3 toHer = -v.Home; toHer.y = 0f;
            float faceYaw = Mathf.Atan2(toHer.x, toHer.z) * Mathf.Rad2Deg;
            float facing = Ease(NmFace, NmEyes, t);
            v.Holder.transform.localRotation = Quaternion.Euler(0f, Mathf.LerpAngle(v.Yaw, faceYaw, facing), 0f);
            Vector3 forward = Quaternion.Euler(0f, Mathf.LerpAngle(v.Yaw, faceYaw, facing), 0f) * Vector3.forward;

            // THE SOUL IN THEM: the aura blooms as it is sucked in (a hard swell, then settles to breathing).
            float bloom = Ease(NmSuck, NmSuck + .1f, t);
            float pulse = Mathf.Clamp01(1f - Mathf.Abs(t - (NmSuck + .1f)) / .2f);
            float breathe = _reducedEffects ? 0f : Mathf.Sin(t * 2.2f + index) * .03f;
            v.AuraRenderer.transform.localPosition = v.Home - Vector3.up * .04f;
            v.AuraRenderer.transform.localRotation = Quaternion.LookRotation(forward, Vector3.up);
            float s = 1.07f + .25f * pulse + breathe;
            v.AuraRenderer.transform.localScale = new Vector3(v.Width * s / .44f, v.Top * 1.04f / .62f * (1f + .1f * pulse), v.Width * s / .44f);
            v.AuraBlock.SetFloat("_Presence", bloom * StagePresence(t, .2f));
            v.AuraBlock.SetFloat("_Glow", .85f + 1.6f * pulse);
            v.AuraRenderer.SetPropertyBlock(v.AuraBlock);
            v.AuraRenderer.enabled = bloom > .01f;
            // The ring the suck leaves on the court.
            float ring = Mathf.Clamp01((t - NmSuck) / .45f);
            Place(v.Burst, v.Home + Vector3.up * .04f, Vector3.one * Mathf.Lerp(v.Width * .4f, v.Width * 2.4f, ring), Quaternion.identity, t > NmSuck ? (1f - ring) * .9f : 0f);
            // Their eyes light violet.
            float eyes = Ease(NmEyes, NmEyes + .12f, t) * (1f + .8f * Mathf.Clamp01(1f - Mathf.Abs(t - (NmEyes + .1f)) / .2f));
            // As the world goes black their eyes burn brighter, so they are what is left.
            eyes *= 1f + 3f * Ease(NmIris, NmIris + .2f, t);
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            // On the face, from the copy's own head (film n3: estimated from the body they sat beside the face). The head bone is
            // the neck's pivot; the eyes are a little above it and in front, the way the face looks.
            Vector3 eyeAt;
            if (v.Head != null)
            {
                Vector3 head = _root.transform.InverseTransformPoint(v.Head.position);
                // They have turned to face her by now (`NmFace`): the face looks along the line from their head to her (film n4:
                // the head bone's own axis is not the face's).
                Vector3 look = -head; look.y = 0f;
                if (look.sqrMagnitude > .01f) forward = look.normalized;
                side = Vector3.Cross(Vector3.up, forward);
                float headSize = Mathf.Max(.35f, v.Home.y + v.Top - head.y);
                eyeAt = head + Vector3.up * (headSize * .32f) + forward * (v.Width * .3f);
            }
            else eyeAt = v.Home + Vector3.up * (v.Top * .6f) + forward * (v.Width * .62f);
            PlaceGlow(v.EyeL, eyeAt - side * v.Width * .14f, Vector3.one * .4f, Quaternion.identity, eyes * 2.2f);
            PlaceGlow(v.EyeR, eyeAt + side * v.Width * .14f, Vector3.one * .4f, Quaternion.identity, eyes * 2.2f);
        }

        private static void NmBlend(NmVictim v, AnimationClip clip, float time, float weight)
        {
            int n = v.Bones.Length;
            for (int i = 0; i < n; i++) { v.Tmp[i] = v.Bones[i].localRotation; v.TmpPos[i] = v.Bones[i].localPosition; }
            var holderPos = v.Holder.transform.localPosition; var holderRot = v.Holder.transform.localRotation;
            Vector3 modelPos = v.Model != null ? v.Model.localPosition : Vector3.zero;
            Quaternion modelRot = v.Model != null ? v.Model.localRotation : Quaternion.identity;
            clip.SampleAnimation(v.AnimRoot.gameObject, time);
            v.Holder.transform.localPosition = holderPos; v.Holder.transform.localRotation = holderRot;
            if (v.Model != null) { v.Model.localPosition = modelPos; v.Model.localRotation = modelRot; }
            if (weight >= .999f) return;
            for (int i = 0; i < n; i++)
            {
                v.Bones[i].localRotation = Quaternion.Slerp(v.Tmp[i], v.Bones[i].localRotation, weight);
                v.Bones[i].localPosition = Vector3.Lerp(v.TmpPos[i], v.Bones[i].localPosition, weight);
            }
        }

        // ------------------------------------------------------------------ camera and grade

        /// <summary>THE ENTERING's one continuous move, computed from where the players really are: out over her shoulder,
        /// riding the lead soul to the first player (ending beside them, never into them), then swinging back and wide to hold
        /// every one of them with her small in the foreground.</summary>
        private void NemuFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            if (index == 2 && _nmVictims.Count > 0)
            {
                Vector3 centre = Vector3.zero;
                foreach (var v in _nmVictims) centre += v.Home;
                centre /= _nmVictims.Count;
                Vector3 out1 = centre; out1.y = 0f;
                Vector3 way = out1.sqrMagnitude > .01f ? out1.normalized : Vector3.forward;
                // Over her shoulder, looking out at them.
                Vector3 shoulder = -way * 2.6f + Vector3.up * 2.0f + Vector3.Cross(Vector3.up, way) * .9f;
                Vector3 shoulderLook = centre + Vector3.up * .9f;
                // Beside the first player, 50 degrees round from the line the soul travels, 2.4 m off.
                var first = _nmVictims[0].Home;
                Vector3 line = first; line.y = 0f; line = line.sqrMagnitude > .01f ? line.normalized : way;
                Vector3 beside = first + Quaternion.Euler(0f, 50f, 0f) * (-line) * 2.4f + Vector3.up * 1.5f;
                // Film n1: swinging back wide made them specks for the payoff. Now it stays with them: a slow ease back from
                // the first player's side so the others come into the frame behind them as they turn and their eyes light.
                float ride = Ease(NmRelease, NmArrive + .1f, t), back = Ease(NmSuck + .1f, NmIris, t);
                Vector3 off = beside - first; off.y = 0f; off = off.sqrMagnitude > .01f ? off.normalized : -way;
                Vector3 settle = beside + off * .9f + Vector3.up * .25f;
                Vector3 settleLook = Vector3.Lerp(first + Vector3.up * 1.0f, centre + Vector3.up * .9f, .35f);
                eye = Vector3.Lerp(Vector3.Lerp(shoulder, beside, ride), settle, back);
                look = Vector3.Lerp(Vector3.Lerp(shoulderLook, first + Vector3.up * 1.0f, ride), settleLook, back);
                fov = Mathf.Lerp(Mathf.Lerp(50f, 46f, ride), 50f, back);
                eye = KeepLensOffOthers(eye, 1.6f);
            }
            if (!_reducedEffects && index == 2)
            {
                // A small tremor as they are entered.
                float jolt = Mathf.Clamp01(1f - Mathf.Abs(t - (NmSuck + .05f)) / .15f);
                var tremble = new Vector3(Mathf.Sin(t * 71f), Mathf.Sin(t * 83f + 1.3f), 0f) * (.04f * jolt);
                eye += tremble; look += tremble * .5f;
            }
            _nmEye = eye;
        }

        private Material _nmInk;

        /// <summary>The last frames: the world sinks into ink round the violet light of their eyes (`Shaders/NemuInk`).</summary>
        private void NemuPostProcess(RenderTexture frame, Camera camera, float t)
        {
            float ink = Ease(NmIris, NmIris + .22f, t);
            if (ink <= .01f) return;
            if (_nmInk == null)
            {
                var shader = Resources.Load<Shader>("Shaders/NemuInk");
                if (shader == null) return;
                _nmInk = new Material(shader) { name = "NemuInk" };
            }
            // All the way (film n5: the frame is linear, so 3 per cent left over shows as a fifth grey on screen).
            _nmInk.SetFloat("_Ink", ink);
            var tmp = RenderTexture.GetTemporary(frame.descriptor);
            Graphics.Blit(frame, tmp, _nmInk);
            Graphics.Blit(tmp, frame);
            RenderTexture.ReleaseTemporary(tmp);
        }

        /// <summary>The hush drains the world: grey and dark from 0.5 s, darker still at the snuff, then the ink.</summary>
        private void NemuGrade(float t, out float brightness, out float saturation)
        {
            float hush = Ease(NmGrey, NmGrey + .5f, t);
            float snuff = Mathf.Clamp01(1f - Mathf.Abs(t - (NmSnuff + .06f)) / .16f);
            // The ink is `NemuPostProcess`'s, which keeps the eyes; darkening here first would put them out with everything else.
            brightness = Mathf.Lerp(1f, .7f, hush) * (1f - .4f * snuff);
            // Film n2: at 0.14 the whole-frame grade turned every violet to white glass. Grey enough to drain the day, colour
            // enough that the dead stay black-purple.
            saturation = Mathf.Lerp(1f, .38f, hush);
        }
    }
}
