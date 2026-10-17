using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Run
{
    /// <summary>
    /// PAETE'S RUN, WHAT MOVES THAT IS NOT THE RACE (owner, 2026-10-10, after playing it: "the map still looks
    /// unlively and bare"). Everything here is scenery:
    ///
    ///  * ⚠️ NOT THE CLOTH. The banners are still (owner, 2026-10-10, of a wind shader on them: unnatural; "just bake
    ///    in simulated cloth physics, or maybe dont make them move at all"). The wind here only says which way the
    ///    sand is blown.
    ///  * SAND WISPS: a few long curling ribbons of blown sand that slide along the tiers and off the summit, one
    ///    at a time each on its own lane, fading in and out. Few, large and soft on purpose (never a particle spray).
    ///  * LIZARDS at a few spots beside the course: they bask, do their push-ups, turn their heads to a racer who
    ///    comes near, and dart off when he comes close. Built from rounded parts and posed part by part.
    ///  * THE GEYSERS IDLING: a low plume of soft puffs that swells and breaks between throws, and a tall burst
    ///    when a racer is thrown.
    ///  * THE LANTERNS: their shell glows and gutters, each on its own time.
    ///  * WHERE A SLIDE ENDS: a ring of foam that keeps spreading and a few drops thrown up.
    ///
    /// ⚠️ NONE OF IT IS GAMEPLAY. No collider (a part's primitive collider is switched off and removed as the part
    /// is made), nothing the race reads, nothing networked (two peers see different lizards and that is correct),
    /// a private fixed-seed random (never `UnityEngine.Random`, which gameplay shares). It reads where the racers
    /// are and nothing else.
    /// ⚠️ STILL IN THE MENUS' MAP PREVIEW, as the alley's life is: there it builds nothing and hides what it built.
    /// ⚠️ NOBODY HAS SEEN ANY OF IT MOVE. The scene builder's review poses it once (<see cref="ReviewPose"/>) so the
    /// shapes can be looked at; the motion is the owner's to judge.
    /// </summary>
    [DefaultExecutionOrder(900)]
    [DisallowMultipleComponent]
    public sealed class RunLife : MonoBehaviour
    {
        [Serializable] public struct Lane { public Vector3 From, To; public float Length, Height; }

        /// <summary>Soft materials on `TumbangPreso/PaeteRunDust` (made by the scene builder): the ribbon of sand,
        /// the geyser's puff, the white spray, the lantern's glow. And the lizards' skin (a plain lit material).</summary>
        public Material Dust, Puff, Spray, Glow, Skin;
        /// <summary>The way the wind blows the sand (it need not be a unit).</summary>
        public Vector3 WindDirection = new Vector3(-0.78f, 0f, -0.62f);
        /// <summary>Where a wisp may travel: from, to, how long the ribbon is and how tall.</summary>
        public Lane[] Lanes = Array.Empty<Lane>();
        public int WispsAtOnce = 5;
        public Vector2 WispSeconds = new Vector2(7f, 12f), WispGap = new Vector2(1.5f, 6f);
        public Vector3[] LizardSpots = Array.Empty<Vector3>();
        public Vector3[] Geysers = Array.Empty<Vector3>();
        /// <summary>Where each slide's water arrives, and the way it was running.</summary>
        public Vector3[] SlideEnds = Array.Empty<Vector3>(), SlideWays = Array.Empty<Vector3>();
        /// <summary>The lanterns' glowing shells: a renderer and which of its materials is the shell.</summary>
        public Renderer[] Lanterns = Array.Empty<Renderer>();
        public int[] LanternSlots = Array.Empty<int>();
        /// <summary>The middle of each shell, for its glow (the builder measures it; a renderer's own bounds take in the post).</summary>
        public Vector3[] LanternAt = Array.Empty<Vector3>();

        /// <summary>How many lizards found ground to stand on (for the builder's report and probes).</summary>
        public int LizardCount => _lizards.Count;
        public int WispCount => _wisps.Count;

        private static readonly int AlphaId = Shader.PropertyToID("_Alpha"), SeedId = Shader.PropertyToID("_Seed");
        private static readonly int ColourId = Shader.PropertyToID("_Color"), EmissionId = Shader.PropertyToID("_EmissionColor");

        private sealed class Wisp
        {
            public Transform Root; public MeshRenderer Renderer; public int Lane = -1;
            public float Start, Seconds, Seed, Rise; public Vector3 Side;
        }

        private sealed class Lizard
        {
            public Transform Root, Body, Head, TailA, TailB;
            public readonly Transform[] Legs = new Transform[4];
            public Vector3 Home, From, To, Up = Vector3.up;
            public float Yaw, FromYaw, ToYaw, Since, For, Phase, HeadYaw;
            public bool Darting, PushUps;
        }

        private sealed class Plume { public Vector3 At; public readonly List<Transform> Puffs = new List<Transform>(); public readonly List<MeshRenderer> Renderers = new List<MeshRenderer>(); public float Burst = -10f, Seed; }
        private sealed class Splash { public Vector3 At, Way; public Transform Ring; public MeshRenderer RingRenderer; public readonly List<Transform> Drops = new List<Transform>(); public readonly List<MeshRenderer> DropRenderers = new List<MeshRenderer>(); public float Seed; }
        private sealed class Lamp { public Renderer Shell; public int Slot; public Color Emission; public Transform Halo; public MeshRenderer HaloRenderer; public float Seed; }

        private readonly List<Wisp> _wisps = new List<Wisp>();
        private readonly List<Lizard> _lizards = new List<Lizard>();
        private readonly List<Plume> _plumes = new List<Plume>();
        private readonly List<Splash> _splashes = new List<Splash>();
        private readonly List<Lamp> _lamps = new List<Lamp>();
        private readonly List<Transform> _faceCamera = new List<Transform>();
        /// <summary>⚠️ PRIVATE AND FIXED-SEED.</summary>
        private readonly System.Random _random = new System.Random(0x50525541);
        private MaterialPropertyBlock _block;
        private Mesh _ribbon, _quad;
        private GameObject _root;
        private bool _built;
        private float _clock;

        private float Range(float a, float b) => a + (float)_random.NextDouble() * (b - a);

        private bool IsPreview() => MatchInstaller.PreviewOnly || gameObject.layer == UI.MapPreviewSurface.PreviewLayer;

        private static void Kill(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        private void OnDestroy()
        {
            Kill(_ribbon); Kill(_quad);
        }

        private void LateUpdate()
        {
            if (IsPreview())
            {
                if (_root != null && _root.activeSelf) _root.SetActive(false);
                return;
            }
            if (!_built) Build();
            if (_root != null && !_root.activeSelf) _root.SetActive(true);
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            _clock += dt;
            var settings = Settings.SettingsStore.Current;
            float motion = settings != null && settings.ReducedUiMotion ? 0f : settings != null && settings.ReducedEffects ? 0.3f : 1f;
            Step(dt, motion, Camera.main);
        }

        /// <summary>
        /// For the scene builder's review, in the editor and not in Play: builds everything and poses it once as it
        /// would stand a few seconds into a race (every wisp in mid air), facing the given camera. Returns the
        /// object that holds what was built, for the caller to destroy.
        /// </summary>
        public GameObject ReviewPose(float clock, Camera camera)
        {
            if (!_built) Build();
            _clock = clock;
            for (int i = 0; i < _wisps.Count; i++)
            {
                var w = _wisps[i];
                if (Lanes.Length == 0) break;
                Launch(w, i % Lanes.Length);
                w.Start = _clock - w.Seconds * 0.5f;
            }
            Step(0f, 1f, camera);
            return _root;
        }

        // ------------------------------------------------------------------ building

        private void Build()
        {
            _built = true;
            _block = new MaterialPropertyBlock();
            _root = new GameObject("RunLife things");
            _root.transform.SetParent(transform, false);
            _ribbon = RibbonMesh();
            _quad = QuadMesh();

            if (Dust != null && Lanes != null && Lanes.Length > 0)
                for (int i = 0; i < Mathf.Max(0, WispsAtOnce); i++)
                {
                    var go = new GameObject("Sand wisp " + i);
                    go.transform.SetParent(_root.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = _ribbon;
                    var r = go.AddComponent<MeshRenderer>();
                    Plain(r, Dust);
                    r.enabled = false;
                    _wisps.Add(new Wisp { Root = go.transform, Renderer = r, Start = _clock + Range(0.5f, 6f) + i * 1.3f, Seconds = 0f });
                }

            if (Skin != null && LizardSpots != null)
                for (int i = 0; i < LizardSpots.Length; i++)
                {
                    if (!Ground(LizardSpots[i], out var at, out var up)) continue;
                    var lizard = BuildLizard(_root.transform, i);
                    lizard.Home = at; lizard.From = lizard.To = at; lizard.Up = up;
                    lizard.Root.position = at;
                    lizard.Yaw = lizard.FromYaw = lizard.ToYaw = Range(0f, 360f);
                    lizard.For = Range(1.5f, 4f); lizard.Phase = Range(0f, 10f);
                    _lizards.Add(lizard);
                }

            if (Puff != null && Geysers != null)
                foreach (var at in Geysers)
                {
                    var plume = new Plume { At = at, Seed = Range(0f, 10f) };
                    for (int k = 0; k < 5; k++)
                    {
                        var puff = Billboard("Geyser puff", Puff, out var r);
                        plume.Puffs.Add(puff); plume.Renderers.Add(r);
                    }
                    _plumes.Add(plume);
                }

            if (Spray != null && SlideEnds != null)
                for (int i = 0; i < SlideEnds.Length; i++)
                {
                    var way = SlideWays != null && i < SlideWays.Length ? SlideWays[i] : Vector3.forward;
                    way.y = 0f; way = way.sqrMagnitude > 1e-4f ? way.normalized : Vector3.forward;
                    var splash = new Splash { At = SlideEnds[i], Way = way, Seed = Range(0f, 10f) };
                    var ring = new GameObject("Slide foam ring");
                    ring.transform.SetParent(_root.transform, false);
                    ring.transform.SetPositionAndRotation(SlideEnds[i] + Vector3.up * 0.06f, Quaternion.Euler(90f, 0f, 0f));
                    ring.AddComponent<MeshFilter>().sharedMesh = _quad;
                    splash.RingRenderer = ring.AddComponent<MeshRenderer>();
                    Plain(splash.RingRenderer, Spray);
                    splash.Ring = ring.transform;
                    for (int k = 0; k < 5; k++)
                    {
                        var drop = Billboard("Slide spray", Spray, out var r);
                        splash.Drops.Add(drop); splash.DropRenderers.Add(r);
                    }
                    _splashes.Add(splash);
                }

            if (Lanterns != null)
                for (int i = 0; i < Lanterns.Length; i++)
                {
                    var shell = Lanterns[i];
                    if (shell == null) continue;
                    int slot = LanternSlots != null && i < LanternSlots.Length ? LanternSlots[i] : 0;
                    var materials = shell.sharedMaterials;
                    if (slot < 0 || slot >= materials.Length || materials[slot] == null) continue;
                    var lamp = new Lamp { Shell = shell, Slot = slot, Seed = Range(0f, 50f) };
                    lamp.Emission = materials[slot].HasProperty(EmissionId) ? materials[slot].GetColor(EmissionId) : Color.black;
                    if (Glow != null)
                    {
                        lamp.Halo = Billboard("Lantern glow", Glow, out lamp.HaloRenderer);
                        lamp.Halo.position = LanternAt != null && i < LanternAt.Length ? LanternAt[i] : shell.bounds.center;
                    }
                    _lamps.Add(lamp);
                }
        }

        private static void Plain(MeshRenderer r, Material material)
        {
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private Transform Billboard(string name, Material material, out MeshRenderer renderer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            renderer = go.AddComponent<MeshRenderer>();
            Plain(renderer, material);
            _faceCamera.Add(go.transform);
            return go.transform;
        }

        /// <summary>A unit square in XY about its middle, facing both ways (the shader does not cull).</summary>
        private static Mesh QuadMesh()
        {
            var mesh = new Mesh { name = "PaeteRun soft quad" };
            mesh.SetVertices(new List<Vector3> { new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f) });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) });
            mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// A wisp: two crossed ribbons one metre long on +Z and one tall, each bent into a slow S and curled up at its
        /// leading end, so from any side there is a shape and not a card on edge. u runs from the tail to the head.
        /// </summary>
        private static Mesh RibbonMesh()
        {
            const int steps = 20;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            for (int strip = 0; strip < 2; strip++)
            {
                var roll = Quaternion.Euler(0f, 0f, strip == 0 ? 8f : 68f);
                float tall = strip == 0 ? 1f : 0.62f;
                int first = v.Count;
                for (int i = 0; i <= steps; i++)
                {
                    float u = i / (float)steps;
                    float bend = Mathf.Sin(u * Mathf.PI * 2f + strip * 1.3f) * 0.09f;
                    float curl = u * u * 0.22f;
                    var spine = new Vector3(bend, curl, u - 0.5f);
                    var across = roll * Vector3.up * (tall * 0.5f * (0.55f + 0.45f * Mathf.Sin(u * Mathf.PI)));
                    v.Add(spine - across); uv.Add(new Vector2(u, 0f));
                    v.Add(spine + across); uv.Add(new Vector2(u, 1f));
                }
                for (int i = 0; i < steps; i++)
                {
                    int a = first + i * 2;
                    tri.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2 });
                }
            }
            var mesh = new Mesh { name = "PaeteRun sand wisp" };
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>The ground under a point (this map's solids and nothing that moves or races), and its slope.</summary>
        private static bool Ground(Vector3 p, out Vector3 at, out Vector3 up)
        {
            at = p; up = Vector3.up;
            if (!Physics.Raycast(p + Vector3.up * 1.5f, Vector3.down, out var hit, 4.5f, ~0, QueryTriggerInteraction.Ignore)) return false;
            if (hit.collider.GetComponentInParent<CharacterMotor>() != null || hit.collider.GetComponentInParent<RunMover>() != null) return false;
            if (hit.normal.y < 0.8f) return false;
            at = hit.point; up = hit.normal;
            return true;
        }

        // ------------------------------------------------------------------ a lizard, part by part

        private Transform Joint(Transform parent, string name, Vector3 at)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false); t.localPosition = at;
            return t;
        }

        private void Part(Transform joint, PrimitiveType type, Vector3 at, Vector3 euler, Vector3 scale, Color colour)
        {
            var go = GameObject.CreatePrimitive(type);
            var collider = go.GetComponent<Collider>();
            if (collider != null) { collider.enabled = false; Kill(collider); }
            go.name = "part";
            go.transform.SetParent(joint, false);
            go.transform.localPosition = at; go.transform.localRotation = Quaternion.Euler(euler); go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = Skin;
            r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _block.Clear(); _block.SetColor(ColourId, colour);
            r.SetPropertyBlock(_block);
        }

        /// <summary>
        /// A rock lizard about 1.1 m nose to tail: a long rounded body, a wedge of a head with two dark eyes, a tail
        /// of two tapering lengths, four splayed legs. Its origin is on the ground under its middle and it faces +Z.
        /// Colours: sand olive, a rust head (a male agama's), a darker banded tail.
        /// </summary>
        private Lizard BuildLizard(Transform parent, int index)
        {
            var body = new Color(0.60f, 0.53f, 0.33f); var head = new Color(0.86f, 0.46f, 0.22f);
            var dark = new Color(0.40f, 0.33f, 0.22f); var eye = new Color(0.07f, 0.06f, 0.05f);
            var pale = new Color(0.80f, 0.72f, 0.50f);
            if (index % 2 == 1) { body = new Color(0.50f, 0.56f, 0.44f); head = new Color(0.30f, 0.62f, 0.62f); }
            var l = new Lizard { Root = new GameObject("Lizard " + index).transform };
            l.Root.SetParent(parent, false);
            l.Body = Joint(l.Root, "body", new Vector3(0f, 0.11f, 0f));
            // Unity's capsule stands on Y, 2 m tall and 1 m across: laid along Z.
            Part(l.Body, PrimitiveType.Capsule, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(0.17f, 0.21f, 0.13f), body);
            Part(l.Body, PrimitiveType.Capsule, new Vector3(0f, 0.045f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.07f, 0.17f, 0.07f), pale);   // the pale ridge down its back
            l.Head = Joint(l.Body, "head", new Vector3(0f, 0.02f, 0.2f));
            Part(l.Head, PrimitiveType.Sphere, new Vector3(0f, 0.01f, 0.07f), Vector3.zero, new Vector3(0.14f, 0.1f, 0.19f), head);
            Part(l.Head, PrimitiveType.Sphere, new Vector3(0.05f, 0.045f, 0.09f), Vector3.zero, Vector3.one * 0.032f, eye);
            Part(l.Head, PrimitiveType.Sphere, new Vector3(-0.05f, 0.045f, 0.09f), Vector3.zero, Vector3.one * 0.032f, eye);
            l.TailA = Joint(l.Body, "tail_a", new Vector3(0f, -0.01f, -0.19f));
            Part(l.TailA, PrimitiveType.Capsule, new Vector3(0f, 0f, -0.13f), new Vector3(90f, 0f, 0f), new Vector3(0.1f, 0.16f, 0.085f), dark);
            l.TailB = Joint(l.TailA, "tail_b", new Vector3(0f, 0f, -0.27f));
            Part(l.TailB, PrimitiveType.Capsule, new Vector3(0f, 0f, -0.14f), new Vector3(90f, 0f, 0f), new Vector3(0.055f, 0.16f, 0.05f), body);
            for (int k = 0; k < 4; k++)
            {
                float side = k % 2 == 0 ? 1f : -1f, along = k < 2 ? 0.13f : -0.14f;
                l.Legs[k] = Joint(l.Body, "leg_" + k, new Vector3(side * 0.07f, -0.01f, along));
                // Out to the side and down to the ground: a lizard's legs splay.
                Part(l.Legs[k], PrimitiveType.Capsule, new Vector3(side * 0.07f, -0.045f, 0f), new Vector3(0f, 0f, side * 58f), new Vector3(0.05f, 0.085f, 0.05f), dark);
            }
            l.Root.localScale = Vector3.one * 1.25f;
            return l;
        }

        // ------------------------------------------------------------------ the frame

        private void Step(float dt, float motion, Camera camera)
        {
            var wind = WindDirection; wind.y = 0f;
            wind = wind.sqrMagnitude > 1e-4f ? wind.normalized : Vector3.right;

            StepWisps(wind, motion);
            StepLizards(dt);
            StepPlumes(motion);
            StepSplashes(motion);
            StepLamps(motion);

            if (camera != null)
            {
                var facing = camera.transform.rotation;
                for (int i = 0; i < _faceCamera.Count; i++) if (_faceCamera[i] != null) _faceCamera[i].rotation = facing;
            }
        }

        private void Launch(Wisp w, int lane)
        {
            w.Lane = lane;
            w.Start = _clock; w.Seconds = Range(WispSeconds.x, WispSeconds.y); w.Seed = Range(0f, 40f);
            w.Rise = Range(0.4f, 1.6f);
            w.Side = new Vector3(Range(-1.5f, 1.5f), 0f, Range(-1.5f, 1.5f));
        }

        private void StepWisps(Vector3 wind, float motion)
        {
            if (Lanes == null || Lanes.Length == 0) return;
            for (int i = 0; i < _wisps.Count; i++)
            {
                var w = _wisps[i];
                if (w.Lane < 0 || _clock - w.Start > w.Seconds)
                {
                    // Waiting: `Start` holds when it may go again.
                    if (w.Lane >= 0) { w.Lane = -1; w.Start = _clock + Range(WispGap.x, WispGap.y); w.Renderer.enabled = false; continue; }
                    if (_clock < w.Start || motion <= 0f) { w.Renderer.enabled = false; continue; }
                    // A lane nobody else is on.
                    int lane = _random.Next(Lanes.Length), tries = 0;
                    while (tries++ < 6 && _wisps.Exists(o => o != w && o.Lane == lane)) lane = _random.Next(Lanes.Length);
                    Launch(w, lane);
                }
                var path = Lanes[w.Lane];
                float u = Mathf.Clamp01((_clock - w.Start) / Mathf.Max(0.1f, w.Seconds));
                Vector3 along = path.To - path.From;
                Vector3 at = path.From + along * u + w.Side + Vector3.up * (w.Rise * u + 0.35f * Mathf.Sin(_clock * 0.9f + w.Seed));
                Vector3 heading = along.sqrMagnitude > 1e-3f ? along.normalized : wind;
                // It leans a little into its own drift, so the ribbon is never a ruler laid along the lane.
                heading = Quaternion.AngleAxis(Mathf.Sin(_clock * 0.37f + w.Seed) * 14f, Vector3.up) * heading;
                w.Root.SetPositionAndRotation(at, Quaternion.LookRotation(heading, Vector3.up));
                float length = path.Length > 0f ? path.Length : 12f, height = path.Height > 0f ? path.Height : 1.8f;
                w.Root.localScale = new Vector3(height, height, length * (0.8f + 0.4f * u));
                _block.Clear();
                _block.SetFloat(AlphaId, Mathf.Sin(u * Mathf.PI) * motion);
                _block.SetFloat(SeedId, w.Seed);
                w.Renderer.SetPropertyBlock(_block);
                w.Renderer.enabled = true;
            }
        }

        private static float Smoother(float x) { x = Mathf.Clamp01(x); return x * x * x * (x * (x * 6f - 15f) + 10f); }

        private void StepLizards(float dt)
        {
            if (_lizards.Count == 0) return;
            var mode = RunMode.Instance;
            foreach (var l in _lizards)
            {
                l.Since += dt;
                // The nearest racer.
                Vector3 nearest = Vector3.zero; float nearestSq = float.MaxValue;
                if (mode != null)
                    foreach (var r in mode.Racers)
                    {
                        if (r == null || r.Body == null) continue;
                        float d = (r.Body.transform.position - l.Root.position).sqrMagnitude;
                        if (d < nearestSq) { nearestSq = d; nearest = r.Body.transform.position; }
                    }
                bool watched = nearestSq < 14f * 14f, scared = nearestSq < 4.5f * 4.5f;

                if (!l.Darting)
                {
                    if ((scared && l.Since > 0.25f) || l.Since >= l.For)
                    {
                        // Somewhere new within a couple of metres of home: away from the racer if he is close.
                        Vector3 best = l.Home, bestUp = Vector3.up; float bestScore = float.MinValue; bool found = false;
                        for (int k = 0; k < 6; k++)
                        {
                            float a = Range(0f, Mathf.PI * 2f), d = Range(0.7f, 2.2f);
                            if (!Ground(l.Home + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d), out var at, out var up)) continue;
                            if (Mathf.Abs(at.y - l.Home.y) > 0.35f) continue;
                            float score = scared ? (at - nearest).sqrMagnitude : Range(0f, 1f);
                            if (score > bestScore) { bestScore = score; best = at; bestUp = up; found = true; }
                        }
                        if (found && (best - l.Root.position).sqrMagnitude > 0.09f)
                        {
                            l.Darting = true; l.Since = 0f;
                            l.From = l.Root.position; l.To = best; l.Up = bestUp;
                            Vector3 way = best - l.From;
                            l.FromYaw = l.Yaw; l.ToYaw = Mathf.Atan2(way.x, way.z) * Mathf.Rad2Deg;
                            l.For = Mathf.Max(0.3f, way.magnitude / (scared ? 5.5f : 3.0f));
                        }
                        else { l.Since = 0f; l.For = Range(1.5f, 4f); }
                        l.PushUps = false;
                    }
                }

                float wriggle = 0f, stride = 0f, lift = 0f;
                if (l.Darting)
                {
                    float u = Mathf.Clamp01(l.Since / l.For), eased = Smoother(u);
                    // The turn is done in the first third, as the legs gather; then it runs.
                    l.Yaw = Mathf.LerpAngle(l.FromYaw, l.ToYaw, Smoother(u * 3f));
                    l.Root.position = Vector3.Lerp(l.From, l.To, eased);
                    float pace = Mathf.Sin(u * Mathf.PI);          // fastest in the middle of the dart
                    stride = Mathf.Sin(_clock * 30f + l.Phase) * pace;
                    wriggle = stride * 13f;
                    lift = 0.02f * pace;
                    if (u >= 1f)
                    {
                        l.Darting = false; l.Since = 0f; l.For = Range(2.5f, 7f);
                        l.PushUps = _random.NextDouble() < 0.6;
                    }
                }
                else if (l.PushUps && l.Since > 0.5f && l.Since < 2.3f)
                {
                    // An agama's push-ups: three quick lifts of the chest.
                    lift = Mathf.Max(0f, Mathf.Sin((l.Since - 0.5f) * Mathf.PI * 2f * 1.7f)) * 0.045f;
                }
                // A breath, always.
                float breath = Mathf.Sin(_clock * 2.1f + l.Phase) * 0.004f;

                var ground = Quaternion.FromToRotation(Vector3.up, l.Up);
                l.Root.rotation = ground * Quaternion.Euler(0f, l.Yaw + wriggle * 0.4f, 0f);
                l.Body.localPosition = new Vector3(0f, 0.11f + lift + breath, 0f);
                l.Body.localRotation = Quaternion.Euler(-lift * 260f, wriggle * 0.5f, 0f);
                // The head: toward a racer who is near (it watches), level otherwise.
                float want = 0f;
                if (watched && !l.Darting)
                {
                    Vector3 to = l.Root.InverseTransformPoint(nearest);
                    want = Mathf.Clamp(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, -55f, 55f);
                }
                l.HeadYaw = Mathf.Lerp(l.HeadYaw, want, 1f - Mathf.Exp(-dt * 7f));
                l.Head.localRotation = Quaternion.Euler(-8f + lift * 150f, l.HeadYaw - wriggle * 0.6f, 0f);
                float sway = l.Darting ? -wriggle * 1.6f : Mathf.Sin(_clock * 0.8f + l.Phase) * 6f;
                l.TailA.localRotation = Quaternion.Euler(4f, sway, 0f);
                l.TailB.localRotation = Quaternion.Euler(-6f, sway * 1.4f + Mathf.Sin(_clock * 1.7f + l.Phase) * 5f, 0f);
                for (int k = 0; k < 4; k++)
                {
                    // Diagonal pairs step together.
                    float sign = k == 0 || k == 3 ? 1f : -1f;
                    l.Legs[k].localRotation = Quaternion.Euler(0f, stride * sign * 38f, 0f);
                }
            }
        }

        private void StepPlumes(float motion)
        {
            if (_plumes.Count == 0) return;
            var mode = RunMode.Instance;
            foreach (var p in _plumes)
            {
                // A racer going up fast over the pad: he has just been thrown.
                if (mode != null && _clock - p.Burst > 1.2f)
                    foreach (var r in mode.Racers)
                    {
                        if (r == null || r.Body == null) continue;
                        Vector3 d = r.Body.transform.position - p.At;
                        if (d.x * d.x + d.z * d.z < 2.2f * 2.2f && d.y > -0.5f && d.y < 5f && r.Body.Velocity.y > 9f) { p.Burst = _clock; break; }
                    }
                float burst = Mathf.Clamp01(1f - (_clock - p.Burst) / 1.1f);
                for (int k = 0; k < p.Puffs.Count; k++)
                {
                    // Each puff swells out of the vent, rises and thins away, one after another.
                    float u = Mathf.Repeat(_clock * (0.42f + burst * 1.4f) + p.Seed + k / (float)p.Puffs.Count, 1f);
                    float rise = (0.15f + 1.15f * u) * (1f + burst * 4.5f);
                    float wobble = Mathf.Sin(_clock * 1.9f + k * 2.1f + p.Seed) * 0.16f * (0.3f + u);
                    p.Puffs[k].position = p.At + new Vector3(wobble, rise, Mathf.Cos(_clock * 1.6f + k * 1.3f) * 0.14f * (0.3f + u));
                    float size = (0.55f + 0.95f * u) * (1f + burst * 1.3f);
                    p.Puffs[k].localScale = new Vector3(size, size * (1f + burst * 0.8f), size);
                    _block.Clear();
                    _block.SetFloat(AlphaId, Mathf.Sin(u * Mathf.PI) * (0.7f + 0.3f * burst) * Mathf.Max(motion, 0.3f));
                    _block.SetFloat(SeedId, p.Seed + k);
                    p.Renderers[k].SetPropertyBlock(_block);
                }
            }
        }

        private void StepSplashes(float motion)
        {
            foreach (var s in _splashes)
            {
                float u = Mathf.Repeat(_clock * 0.55f + s.Seed, 1f);
                float size = 1.4f + 3.2f * u;
                s.Ring.localScale = new Vector3(size, size, 1f);
                _block.Clear();
                _block.SetFloat(AlphaId, (1f - u) * (1f - u) * 0.9f);
                _block.SetFloat(SeedId, s.Seed);
                s.RingRenderer.SetPropertyBlock(_block);
                for (int k = 0; k < s.Drops.Count; k++)
                {
                    // Thrown up and forward the way the water was running, falling back: a short arc, one after another.
                    float v = Mathf.Repeat(_clock * 0.9f + s.Seed * 1.7f + k * 0.21f, 1f);
                    float spread = (k - (s.Drops.Count - 1) * 0.5f) * 0.42f;
                    Vector3 side = Vector3.Cross(Vector3.up, s.Way);
                    Vector3 at = s.At + s.Way * (0.4f + 1.6f * v) + side * spread * (0.5f + v) + Vector3.up * (0.1f + 1.5f * 4f * v * (1f - v));
                    s.Drops[k].position = at;
                    float drop = 0.34f + 0.3f * Mathf.Sin(v * Mathf.PI);
                    s.Drops[k].localScale = new Vector3(drop, drop, drop);
                    _block.Clear();
                    _block.SetFloat(AlphaId, Mathf.Sin(v * Mathf.PI) * Mathf.Max(motion, 0.3f));
                    _block.SetFloat(SeedId, s.Seed + k * 3f);
                    s.DropRenderers[k].SetPropertyBlock(_block);
                }
            }
        }

        private void StepLamps(float motion)
        {
            foreach (var lamp in _lamps)
            {
                if (lamp.Shell == null) continue;
                // A flame behind shell: a slow breathing, a quick shiver, and now and then a gutter.
                float slow = Mathf.PerlinNoise(_clock * 0.9f + lamp.Seed, lamp.Seed * 0.37f);
                float quick = Mathf.PerlinNoise(_clock * 7.5f + lamp.Seed * 2f, 0.5f);
                float gutter = Mathf.PerlinNoise(_clock * 0.33f + lamp.Seed * 5f, 1.7f) < 0.2f ? 0.72f : 1f;
                float light = Mathf.Lerp(1f, (0.78f + 0.3f * slow + 0.16f * quick) * gutter, Mathf.Max(motion, 0.3f));
                lamp.Shell.GetPropertyBlock(_block, lamp.Slot);
                _block.SetColor(EmissionId, lamp.Emission * (light * 1.35f));
                lamp.Shell.SetPropertyBlock(_block, lamp.Slot);
                if (lamp.Halo != null)
                {
                    float size = 1.5f + 0.5f * light;
                    lamp.Halo.localScale = new Vector3(size, size, size);
                    _block.Clear();
                    _block.SetFloat(AlphaId, 0.55f * light);
                    _block.SetFloat(SeedId, lamp.Seed);
                    lamp.HaloRenderer.SetPropertyBlock(_block);
                }
            }
        }
    }
}
