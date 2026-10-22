using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// WHO IS TAYA, at a glance, on every screen (owner, 2026-10-22, the presentation pass):
    ///   * a glowing headband in `UiTheme.Defense` on the taya, fitted to that rig's own head;
    ///   * a ground ring in the same colour that follows them;
    ///   * a TAYA reveal when each round goes live (`UI.TayaReveal`), with the ring flaring out on the court.
    ///
    /// ⚠️ EVERY BODY IS DIFFERENT (owner: *"iba iba size nila"*, *"paete isnt even human"*). The band is measured off
    /// the skin actually weighted to the rig's `head` bone, so a small Nemu and a tall Dante each get a band that
    /// wraps their own head. A rig with no `head` bone, or whose mesh cannot be read, gets no band rather than a
    /// guessed one floating in the air; the ring and the reveal still mark them.
    ///
    /// Presentation only: it reads `IsDefender` and the round state every peer already has and decides nothing.
    /// </summary>
    public sealed class TayaIdentity : MonoBehaviour
    {
        private static bool _hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            if (_hooked) return;
            _hooked = true;
            SceneManager.sceneLoaded += (scene, mode) => Install(scene);
            Install(SceneManager.GetActiveScene());
        }

        private static void Install(Scene scene)
        {
            if (!scene.IsValid() || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            foreach (var root in scene.GetRootGameObjects()) if (root.GetComponent<TayaIdentity>() != null) return;
            var go = new GameObject("~TayaIdentity");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<TayaIdentity>();
        }

        private CharacterMotor _taya;
        private GameObject _model;
        private int _bandAttempts;
        /// <summary>The taya's headband, or null when their rig has no measurable head. For tests and tools.</summary>
        public static Renderer Band => _live != null ? _live._bandRenderer : null;
        /// <summary>The taya's ground ring, or null when there is no taya.</summary>
        public static GameObject Ring => _live != null ? _live._ring : null;
        private static TayaIdentity _live;
        private void Awake() => _live = this;
        private GameObject _band, _ring;
        private Material _bandMaterial, _ringMaterial;
        private Renderer _bandRenderer;
        private long _revealedMatch = -1;
        private int _revealedRound = -1;
        private float _flareAt = -100;
        private const float RingRadius = .78f, FlareSeconds = .7f;

        // ⚠️ WHO CAN BE TAGGED, AND WHETHER THE TAYA CAN TAG AT ALL (owner, 2026-10-22: *"easier to tell if someone can be
        // taya and if someone can't"*). Read off the ground, for every screen and the spectators:
        //   * an attacker the taya may tag right now (`IsTaggable`, can standing) stands in three arcs in the taya's
        //     own colour that snap in, turn and throb in step with the taya-only rim, like a reticle on the road;
        //   * the moment they become safe (out of the box, slipper gone, a power), the arcs spin open and are gone;
        //   * while the can is down the taya CANNOT tag (`CombatVerbs` refuses), so their own ring breaks into grey
        //     dashes and the headband dims, and both flare back to colour when the can stands.
        // Why the ground: auras through walls would end hiding, a shield on every body would bury four silhouettes,
        // and a HUD icon only tells its own player. The screen-edge frames (`TumpHudEffects`) still tell each player
        // their own state; these tell everybody everyone's. Presentation only, from replicated state.
        private sealed class Danger
        {
            public GameObject Ring; public Material Material; public float Since = -100, Until = -100; public bool On;
            public Renderer Body;
        }
        private readonly System.Collections.Generic.Dictionary<CharacterMotor, Danger> _danger =
            new System.Collections.Generic.Dictionary<CharacterMotor, Danger>();
        private GameObject _broken;
        private Material _brokenMaterial;
        private bool _disarmed;
        private float _disarmedSince = -100;
        private const float DangerRadius = .62f, DangerIn = .2f, DangerOut = .22f;
        private static readonly Color Disarmed = new Color(.62f, .62f, .68f, 1);

        private void LateUpdate()
        {
            var round = GameServices.Round; var match = GameServices.Match;
            CharacterMotor taya = null;
            if (round != null && match != null && match.MatchInProgress)
                foreach (var actor in round.Players)
                    if (actor != null && actor.IsDefender) { taya = actor; break; }
            if (taya != _taya) { Clear(); _taya = taya; if (taya != null) Build(taya); }
            StepDanger(round, match);
            if (_taya == null) return;
            // A new model (skin swap, rejoin) takes the old head and its band with it: fit a band to the new one.
            var model = _taya.GetComponent<CharacterVisual>()?.Model;
            if (model != _model)
            {
                DestroyBand();
                _model = model; _bandAttempts = 0;
            }
            // A freshly applied model can take a frame or two to bind its skin: keep trying briefly.
            if (_band == null && _bandAttempts < 30) { _bandAttempts++; BuildBand(_taya); }

            // The reveal: once per round, on the frame it goes live.
            if (round.RoundActive && !match.IsWarmupBuffer &&
                (match.PresentationMatchId != _revealedMatch || match.RoundNumber != _revealedRound))
            {
                _revealedMatch = match.PresentationMatchId; _revealedRound = match.RoundNumber;
                _flareAt = Time.unscaledTime;
                UI.TayaReveal.Play(_taya.PlayerSlot);
            }

            float t = Time.unscaledTime;
            bool still = Settings.SettingsStore.Current.ReducedUiMotion;
            float glow = still ? .85f : .7f + .3f * Mathf.Sin(t * 3.1f);
            var defense = UI.UiTheme.Defense;
            // The can down disarms the taya: their colour drains and the ring breaks; standing it re-arms them in a flare.
            bool disarmed = round.Lata != null && !round.Lata.IsUpright && round.RoundActive;
            if (disarmed != _disarmed) { _disarmed = disarmed; _disarmedSince = t; if (!disarmed) _flareAt = t; }
            float drain = Mathf.Clamp01((t - _disarmedSince) / .25f);
            float armed = disarmed ? 1 - drain : 1;
            defense = Color.Lerp(Disarmed, defense, armed);
            glow *= Mathf.Lerp(.35f, 1, armed);

            if (_band != null)
            {
                // The taya's own first-person view would put the band in front of the lens.
                var rig = Camera.main != null ? Camera.main.GetComponent<CameraSystem.CameraRig>() : null;
                _bandRenderer.enabled = !(rig != null && rig.IsLocalFpp && rig.IsFollowing(_taya));
                SetGlow(_bandMaterial, defense, 1f, .55f * glow);
                PlaceTails(t, still);
                if (_tails != null) foreach (var tail in _tails) if (tail != null) tail.GetComponent<Renderer>().enabled = _bandRenderer.enabled;
            }
            if (_ring != null)
            {
                float flare = Mathf.Clamp01((t - _flareAt) / FlareSeconds);
                float burst = still ? 0 : (1 - flare) * (1 - flare);
                float r = RingRadius * (1 + 1.6f * burst + (still ? 0 : .04f * Mathf.Sin(t * 2.4f)));
                Vector3 feet = _taya.transform.position;
                _ring.transform.position = new Vector3(feet.x, feet.y + .03f, feet.z);
                _ring.transform.rotation = Quaternion.Euler(0, still ? 0 : t * 24f, 0);
                _ring.transform.localScale = new Vector3(r, 1, r);
                bool shown = _taya.gameObject.activeInHierarchy && !_taya.IsSwimming;
                _ring.SetActive(shown && armed > .02f);
                SetGlow(_ringMaterial, defense, (.55f + .35f * burst) * armed, glow + 1.5f * burst);
                StepBroken(feet, shown, disarmed, t, still);
            }
        }

        /// <summary>The disarmed taya's ring: grey dashes that crack apart from the whole ring and turn slowly.</summary>
        private void StepBroken(Vector3 feet, bool shown, bool disarmed, float t, bool still)
        {
            float u = disarmed ? Mathf.Clamp01((t - _disarmedSince) / .25f) : 1 - Mathf.Clamp01((t - _disarmedSince) / .18f);
            if (u <= .001f) { if (_broken != null) _broken.SetActive(false); return; }
            if (_broken == null)
            {
                _broken = VfxShapes.Lay(transform, "TayaDisarmedRing", Arcs(10, .48f, .9f), RingRadius, 0);
                VfxMaterial.Ghost(_broken.GetComponent<Renderer>(), Disarmed, .4f);
                _brokenMaterial = _broken.GetComponent<Renderer>().sharedMaterial;
                ShadowsOff(_broken);
            }
            _broken.SetActive(shown);
            // The dashes crack outward a little as the colour drains, then sit, turning slowly.
            float r = RingRadius * (1 + .12f * (1 - (1 - u) * (1 - u)));
            _broken.transform.position = new Vector3(feet.x, feet.y + .03f, feet.z);
            _broken.transform.rotation = Quaternion.Euler(0, still ? 0 : -t * 9f, 0);
            _broken.transform.localScale = new Vector3(r, 1, r);
            SetGlow(_brokenMaterial, Disarmed, .7f * u, .35f * u);
        }

        private void StepDanger(RoundDirector round, MatchDirector match)
        {
            float t = Time.unscaledTime;
            bool live = round != null && match != null && match.MatchInProgress && round.RoundActive;
            bool canTag = live && round.Lata != null && round.Lata.IsUpright && WorldCueProfile.Current.TayaTarget > 0;
            bool still = Settings.SettingsStore.Current.ReducedUiMotion;
            if (live)
                foreach (var actor in round.Bodies)
                {
                    if (actor == null || actor.IsDefender) continue;
                    if (!_danger.TryGetValue(actor, out var cue)) _danger[actor] = cue = new Danger();
                    // Never reveal a body the world is not drawing (a power that hides it, a fall, a swim).
                    if (cue.Body == null || !cue.Body.gameObject.activeInHierarchy)
                    {
                        var model = actor.GetComponent<CharacterVisual>()?.Model;
                        cue.Body = model != null ? model.GetComponentInChildren<SkinnedMeshRenderer>(false) : null;
                    }
                    bool drawn = cue.Body != null && cue.Body.enabled && cue.Body.gameObject.activeInHierarchy && !actor.IsSwimming;
                    bool on = canTag && drawn && actor.IsTaggable();
                    if (on != cue.On) { cue.On = on; if (on) cue.Since = t; else cue.Until = t; }
                }
            foreach (var pair in _danger)
            {
                var actor = pair.Key; var cue = pair.Value;
                float appear = cue.On ? Mathf.Clamp01((t - cue.Since) / DangerIn) : 1 - Mathf.Clamp01((t - cue.Until) / DangerOut);
                if (actor == null || !live || appear <= .001f) { if (cue.Ring != null) cue.Ring.SetActive(false); continue; }
                if (cue.Ring == null)
                {
                    cue.Ring = VfxShapes.Lay(transform, "TagDangerRing", Arcs(3, .62f, .8f), DangerRadius, 0);
                    VfxMaterial.Ghost(cue.Ring.GetComponent<Renderer>(), UI.UiTheme.Defense, 1.2f);
                    cue.Material = cue.Ring.GetComponent<Renderer>().sharedMaterial;
                    ShadowsOff(cue.Ring);
                }
                cue.Ring.SetActive(true);
                // In: snaps down from wide with a little overshoot. Out: spins open and widens away.
                float size;
                if (cue.On) { float u = appear; size = still ? 1 : 1 + .7f * (1 - u) * (1 - u) - .08f * Mathf.Sin(u * Mathf.PI); }
                else size = still ? 1 : 1 + .45f * (1 - appear);
                // The throb matches the taya-only rim (`CharacterVisual.Readability`), so both read as one signal.
                float throb = still ? 1 : .8f + .3f * Mathf.Sin(t * 6.5f);
                float spin = still ? 0 : t * 70f + (cue.On ? 0 : 260f * (1 - appear));
                Vector3 feet = actor.transform.position;
                cue.Ring.transform.position = new Vector3(feet.x, feet.y + .035f, feet.z);
                cue.Ring.transform.rotation = Quaternion.Euler(0, spin, 0);
                float r = DangerRadius * size;
                cue.Ring.transform.localScale = new Vector3(r, 1, r);
                SetGlow(cue.Material, UI.UiTheme.Defense, .85f * appear, 1.3f * throb * appear);
            }
        }

        /// <summary>A flat ring of <paramref name="count"/> arcs, each <paramref name="fill"/> of its share, drawn both ways up.</summary>
        private static Mesh Arcs(int count, float fill, float inner, int steps = 8)
        {
            var v = new System.Collections.Generic.List<Vector3>(); var tri = new System.Collections.Generic.List<int>();
            for (int k = 0; k < count; k++)
            {
                float a0 = k * Mathf.PI * 2 / count, span = Mathf.PI * 2 / count * fill;
                int b = v.Count;
                for (int i = 0; i <= steps; i++)
                {
                    float a = a0 + span * i / steps; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    v.Add(d); v.Add(d * inner);
                }
                for (int i = 0; i < steps; i++)
                {
                    int o = b + i * 2;
                    tri.Add(o); tri.Add(o + 1); tri.Add(o + 2); tri.Add(o + 1); tri.Add(o + 3); tri.Add(o + 2);
                    tri.Add(o); tri.Add(o + 2); tri.Add(o + 1); tri.Add(o + 1); tri.Add(o + 2); tri.Add(o + 3);
                }
            }
            var mesh = new Mesh { name = "TagArcs" };
            mesh.SetVertices(v); mesh.SetTriangles(tri, 0);
            // Facing up, set rather than recalculated: the two windings share vertices, and recalculated normals cancel
            // to nothing, which lit the arcs black on the road.
            var up = new Vector3[v.Count];
            for (int i = 0; i < up.Length; i++) up[i] = Vector3.up;
            mesh.normals = up; mesh.RecalculateBounds();
            return mesh;
        }

        private static void SetGlow(Material material, Color colour, float alpha, float emission)
        {
            if (material == null) return;
            var c = new Color(colour.r, colour.g, colour.b, alpha);
            material.color = c; material.SetColor("_BaseColor", c);
            material.SetColor("_EmissionColor", new Color(colour.r, colour.g, colour.b, 1) * emission);
        }

        private void Build(CharacterMotor taya)
        {
            _ring = VfxShapes.Lay(transform, "TayaGroundRing", VfxShapes.Collar(40, .025f, .9f), RingRadius, 0);
            VfxMaterial.Ghost(_ring.GetComponent<Renderer>(), UI.UiTheme.Defense, 1f);
            _ringMaterial = _ring.GetComponent<Renderer>().sharedMaterial;
            ShadowsOff(_ring);
            _model = taya.GetComponent<CharacterVisual>()?.Model; _bandAttempts = 0;
        }

        /// <summary>
        /// Where the band sits on each head, in metres above the `head` bone. Every hero's head is one block drawn by a
        /// single `head-mesh` (face, hair, hat and all), so the brow cannot be told from the hair by bone or by bounds; it
        /// was read off each hero's side view against a centimetre ruler (Logs/taya-identity, 2026-10-22): on the
        /// hairline for Basilio, Amihan and Isagani; round the hood above the face for Nemu; on top of Paete's face
        /// panel; round the crown of Phaister's hat, just above the brim, as a hatband; at the base of Yasmin's box hat
        /// under the goggles; mid-forehead for Rago; over Ilyas's own bandana. A model not in the table gets 45 per cent
        /// of what it draws above the neck.
        /// </summary>
        private static float? BrowFor(string model)
        {
            if (string.IsNullOrEmpty(model)) return null;
            model = model.ToLowerInvariant();
            if (model.Contains("dante")) return .38f;
            if (model.Contains("nemu")) return .31f;
            if (model.Contains("amihan")) return .38f;
            if (model.Contains("paete")) return .49f;
            if (model.Contains("phaister")) return .67f;
            if (model.Contains("cheska")) return .41f;
            if (model.Contains("sean")) return .55f;
            if (model.Contains("zack")) return .38f;
            if (model.Contains("rafi")) return .48f;
            return null;
        }

        /// <summary>The value below which <paramref name="share"/> of the sorted values lie.</summary>
        private static float Percentile(System.Collections.Generic.List<float> sorted, float share)
            => sorted[Mathf.Clamp(Mathf.RoundToInt(share * (sorted.Count - 1)), 0, sorted.Count - 1)];

        /// <summary>
        /// A headband tied round the brow: wrapped to the head's own cross-section at that height, so it sits on the
        /// head rather than floating round it, with two short tails knotted at the back.
        ///
        /// ⚠️ MEASURED FROM WHAT IS DRAWN ABOVE THE NECK, NOT FROM THE SKIN WEIGHTED TO `head`. The first cut used the
        /// head-weighted skin (Logs/taya-identity, 2026-10-22): only a neck slab of Dante's and Nemu's skin is weighted
        /// there, so their bands sat on their chins, and hair wider than the face made every band a loose hoop. Now the
        /// band takes the height from <see cref="BrowFor"/> and its width from a thin slice of the drawn head at that
        /// height. A mesh the CPU cannot read contributes its drawn bounds instead.
        /// </summary>
        private void BuildBand(CharacterMotor taya)
        {
            var visual = taya.GetComponent<CharacterVisual>();
            var model = visual != null ? visual.Model : null;
            if (model == null) return;
            Transform head = null;
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                if (!skin.enabled || skin.bones == null) continue;
                foreach (var b in skin.bones) if (b != null && b.name == "head") { head = b; break; }
                if (head != null) break;
            }
            if (head == null) return;
            float neck = taya.transform.InverseTransformPoint(head.position).y;
            var points = new System.Collections.Generic.List<Vector3>(4096);
            // Every readable surface in the body's frame, triangles and all, for the cross-section at the brow below.
            var surfaces = new System.Collections.Generic.List<(Vector3[] verts, int[] tris)>();
            var baked = new Mesh();
            try
            {
                foreach (var renderer in model.GetComponentsInChildren<Renderer>(false))
                {
                    if (!renderer.enabled || renderer.GetComponent<VfxRenderTag>() != null) continue;
                    Mesh mesh = null;
                    if (renderer is SkinnedMeshRenderer skin && skin.sharedMesh != null)
                    {
                        if (skin.sharedMesh.isReadable) { skin.BakeMesh(baked, true); mesh = baked; }
                    }
                    else if (renderer is MeshRenderer)
                    {
                        var filter = renderer.GetComponent<MeshFilter>();
                        if (filter != null && filter.sharedMesh != null && filter.sharedMesh.isReadable) mesh = filter.sharedMesh;
                    }
                    else continue;
                    if (mesh != null)
                    {
                        var vertices = mesh.vertices;
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            Vector3 q = taya.transform.InverseTransformPoint(renderer.transform.TransformPoint(vertices[i]));
                            vertices[i] = q;
                            if (q.y >= neck) points.Add(q);
                        }
                        surfaces.Add((vertices, mesh.triangles));
                    }
                    else
                    {
                        // Unreadable: its drawn box stands in, corner by corner.
                        var box = renderer.bounds;
                        for (int c = 0; c < 8; c++)
                        {
                            Vector3 corner = box.center + Vector3.Scale(box.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
                            Vector3 q = taya.transform.InverseTransformPoint(corner);
                            if (q.y >= neck) points.Add(q);
                        }
                    }
                }
            }
            finally { Destroy(baked); }
            if (points.Count < 8) return;
            var region = new Bounds(points[0], Vector3.zero);
            foreach (var q in points) region.Encapsulate(q);
            if (region.size.y < .05f) return;

            string source = visual.SourceModel != null ? visual.SourceModel.name : model.name;
            float? table = BrowFor(source);
            float brow = table.HasValue ? neck + table.Value : region.min.y + .45f * region.size.y;
            // The head's own width and depth at the brow: a thin slice, its outer 5 per cent on each side dropped, so a
            // lock hanging out to one side or the lip of a brim cannot turn the band into a hoop. It still wraps over the
            // hair, as a real headband does.
            //
            // ⚠️ CUT THE TRIANGLES, NOT THE VERTICES. These blocky heads have vertices only at box corners, so a thin
            // slab of vertices through the middle of a box is nearly empty, and widening it caught hat brims (Phaister
            // 0.69 m) or lost the depth (Nemu 3 cm). Each triangle edge that crosses the brow plane gives one point of
            // the exact cross-section, whatever the mesh.
            var xs = new System.Collections.Generic.List<float>(); var zs = new System.Collections.Generic.List<float>();
            foreach (var (verts, tris) in surfaces)
                for (int t = 0; t + 2 < tris.Length; t += 3)
                    for (int e = 0; e < 3; e++)
                    {
                        Vector3 a = verts[tris[t + e]], b = verts[tris[t + (e + 1) % 3]];
                        if ((a.y - brow) * (b.y - brow) >= 0) continue;
                        Vector3 cut = Vector3.Lerp(a, b, (brow - a.y) / (b.y - a.y));
                        xs.Add(cut.x); zs.Add(cut.z);
                    }
            // An unreadable mesh has no triangles to cut: its corners near the brow stand in.
            if (xs.Count < 6)
                foreach (var q in points) if (Mathf.Abs(q.y - brow) <= .08f) { xs.Add(q.x); zs.Add(q.z); }
            Bounds slice;
            if (xs.Count >= 6)
            {
                xs.Sort(); zs.Sort();
                float x0 = Percentile(xs, .05f), x1 = Percentile(xs, .95f), z0 = Percentile(zs, .05f), z1 = Percentile(zs, .95f);
                slice = new Bounds(new Vector3((x0 + x1) * .5f, brow, (z0 + z1) * .5f), new Vector3(x1 - x0, 0, z1 - z0));
            }
            else slice = region;
            // Cloth, not a line: thick enough to read at court distance, scaled to the head it is on.
            BandDepth = Mathf.Clamp(region.size.y * .085f, .045f, .085f);
            float halfX = slice.extents.x + .015f, halfZ = slice.extents.z + .015f;
            Vector3 centre = new Vector3(slice.center.x, brow - BandDepth * .5f, slice.center.z);
            BandHalf = new Vector2(halfX, halfZ);

            _band = new GameObject("TayaHeadband");
            _band.transform.SetParent(head, false);
            _band.transform.position = taya.transform.TransformPoint(centre);
            _band.transform.rotation = taya.transform.rotation;
            Vector3 parent = head.lossyScale;
            Vector3 Inverse(Vector3 world) => new Vector3(world.x / Mathf.Max(.0001f, parent.x), world.y / Mathf.Max(.0001f, parent.y), world.z / Mathf.Max(.0001f, parent.z));
            // A square collar's corners sit at radius 1, its sides at 0.707: size the sides to the head.
            _band.transform.localScale = Inverse(new Vector3(halfX * 1.4142f, BandDepth, halfZ * 1.4142f));
            var ring = VfxShapes.Collar(4, 1f, .86f, 0, Mathf.PI * .25f);
            _band.AddComponent<MeshFilter>().sharedMesh = ring;
            _bandRenderer = _band.AddComponent<MeshRenderer>();
            // Solid lit cloth with a glow in it, so it reads as a band tied on, not a hologram ring.
            VfxMaterial.Solid(_bandRenderer, UI.UiTheme.Defense, .6f);
            _bandMaterial = _bandRenderer.sharedMaterial;
            ShadowsOff(_band);
            _band.AddComponent<OwnedMesh>().Mesh = ring;

            // The knot's two tails, hanging from the back of the band and lifting in the run.
            _tails = new Transform[2];
            for (int k = 0; k < 2; k++)
            {
                var tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tail.name = "TayaHeadbandTail" + k;
                Destroy(tail.GetComponent<Collider>());
                tail.transform.SetParent(_band.transform.parent, false);
                tail.GetComponent<Renderer>().sharedMaterial = _bandMaterial;
                // An effect on the head, never part of the model: replays, outlines and readability skip it.
                VfxRenderTag.Attach(tail);
                ShadowsOff(tail);
                _tails[k] = tail.transform;
            }
            _tailLength = Mathf.Clamp(region.size.y * .32f, .1f, .24f);
            _tailRoot = head.InverseTransformPoint(taya.transform.TransformPoint(new Vector3(slice.center.x, brow - BandDepth * .5f, slice.min.z - .01f)));
            _tailWidth = BandDepth * .9f;
            PlaceTails(Time.unscaledTime, Settings.SettingsStore.Current.ReducedUiMotion);
        }

        /// <summary>For tests: the band's half width and half depth round the head, and its depth, metres.</summary>
        public static Vector2 BandHalf { get; private set; }
        public static float BandDepth { get; private set; }
        private Transform[] _tails;
        private Vector3 _tailRoot;
        private float _tailLength, _tailWidth;

        private void PlaceTails(float t, bool still)
        {
            if (_tails == null || _band == null || _taya == null) return;
            var head = _band.transform.parent;
            Vector3 root = head.TransformPoint(_tailRoot);
            Vector3 back = -_taya.transform.forward, right = _taya.transform.right;
            // Hangs at a slant behind the head; running lifts it toward level, a flutter on top.
            var motor = _taya;
            float speed = motor.Velocity.magnitude;
            float lift = Mathf.Clamp01(speed / 4f);
            for (int k = 0; k < _tails.Length; k++)
            {
                if (_tails[k] == null) continue;
                float side = k == 0 ? -1 : 1;
                float flutter = still ? 0 : Mathf.Sin(t * (9f + 3f * k) + k) * (6f + 14f * lift);
                float droop = Mathf.Lerp(62f, 18f, lift) + flutter;
                Vector3 along = Quaternion.AngleAxis(droop, right) * back;
                along = Quaternion.AngleAxis(side * 16f, Vector3.up) * along;
                _tails[k].SetPositionAndRotation(root + along * (_tailLength * .5f) + right * side * _tailWidth * .6f,
                    Quaternion.LookRotation(along, Vector3.Cross(along, right)));
                _tails[k].localScale = Vector3.one;
                var world = new Vector3(_tailWidth * 1.1f, _tailWidth * .35f, _tailLength);
                var parent = _tails[k].parent != null ? _tails[k].parent.lossyScale : Vector3.one;
                _tails[k].localScale = new Vector3(world.x / Mathf.Max(.0001f, parent.x), world.y / Mathf.Max(.0001f, parent.y), world.z / Mathf.Max(.0001f, parent.z));
            }
        }

        private static void ShadowsOff(GameObject go)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        }

        private void Clear()
        {
            DestroyBand();
            if (_ring != null) Destroy(_ring);
            if (_broken != null) Destroy(_broken);
            _broken = null; _brokenMaterial = null;
            _band = _ring = null; _bandRenderer = null; _bandMaterial = _ringMaterial = null; _taya = null; _model = null;
        }

        private void OnDestroy()
        {
            Clear();
            foreach (var cue in _danger.Values) if (cue.Ring != null) Destroy(cue.Ring);
            _danger.Clear();
            if (_live == this) _live = null;
        }

        /// <summary>For tests: whether the taggable cue is showing under <paramref name="actor"/>.</summary>
        public static bool DangerShown(CharacterMotor actor)
            => _live != null && actor != null && _live._danger.TryGetValue(actor, out var cue) && cue.On && cue.Ring != null && cue.Ring.activeSelf;
        /// <summary>For tests: whether the taya is drawn disarmed (the can is down).</summary>
        public static bool TayaDisarmed => _live != null && _live._disarmed;

        private void DestroyBand()
        {
            if (_band != null) Destroy(_band);
            if (_tails != null) foreach (var tail in _tails) if (tail != null) Destroy(tail.gameObject);
            _band = null; _tails = null; _bandRenderer = null; _bandMaterial = null;
        }

        /// <summary>Frees the band's generated mesh with the band.</summary>
        private sealed class OwnedMesh : MonoBehaviour
        {
            public Mesh Mesh;
            private void OnDestroy() { if (Mesh != null) Destroy(Mesh); }
        }
    }
}
