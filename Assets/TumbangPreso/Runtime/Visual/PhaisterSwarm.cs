using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ VANISHING ACT: SHE BURSTS INTO A BARANG SWARM, IT STREAMS ALONG HER AIM, AND IT KNITS HER BACK FROM THE FEET UP
    /// (HERO-10, `docs/reports/phaister-kit-2026-09-27/plan.md` 4.1 and 4.5). The mambabarang's insect sorcery (Visayan
    /// folklore, fiction here); owner: *"Barang swarm"*, *"she's supposed to be using MAGIC AND VOODOO"*.
    ///
    /// Every moving part, in the plan's order (all her turning is CLOCKWISE seen from above; wings stay slow and the speed
    /// lives in the paths):
    ///
    /// | Beat | Time | What moves |
    /// |---|---|---|
    /// | Burst | 0.00 to 0.10 | every insect flies OUT of her body along its own typed direction; her body is hidden |
    /// | Stream | 0.08 to 0.30 | each turns and streams LOW along the aim in a ribbon that twists clockwise about the path |
    /// | Knit | 0.24 to 0.46 | each spirals clockwise up round the arrival and packs into her at its own height, lowest first |
    /// | Reveal | 0.30 | her body shows again; the swarm still covers her upper half, so she reads as knitting upward |
    /// | Stragglers | 0.46 to 1.30 | five moths settle on her brim and shoulders, beat slowly, then crawl into her sleeves |
    ///
    /// ⚠️ EVERY INSECT IS A TYPED ROW, NOT A LOOP WITH RANDOM NUMBERS (owner's standing rule). Each row gives its own start
    /// on her body, burst direction, stream delay, ribbon phase and radius, knit height and angle, wing rate and size.
    ///
    /// ⚠️ HER BODY IS HIDDEN WITH `forceRenderingOff`, never `enabled`: first person and the capture paths own `enabled` and
    /// `shadowCastingMode`. Only the renderers this effect switched off are switched back on.
    /// </summary>
    public sealed class PhaisterSwarm : MonoBehaviour, IVfxTimeline
    {
        private struct Row
        {
            public bool Beetle;
            public Vector3 Start;      // on her body at the start, (right, up, forward) metres
            public Vector3 Burst;      // burst direction, same frame
            public float Delay;        // extra stream delay, seconds
            public float Phase;        // ribbon phase, degrees
            public float Radius;       // ribbon radius, metres
            public float KnitHeight;   // where on her it packs in, metres up
            public float KnitAngle;    // where round her it starts its spiral, degrees
            public float WingHz;       // wing beats a second (slow: 3 to 6)
            public float Size;         // prop scale
            public int Perch;          // -1 none, else a straggler's perch
        }

        // ⚠️ TYPED BY HAND, 2026-09-27: twenty moths and eight beetles, each its own row.
        private static readonly Row[] Rows =
        {
            new Row { Start = new Vector3( 0.10f, 1.05f,  0.12f), Burst = new Vector3( 0.4f,  0.9f,  0.2f), Delay = 0.00f, Phase =   0f, Radius = 0.22f, KnitHeight = 1.25f, KnitAngle =   0f, WingHz = 4.6f, Size = 1.55f, Perch = 0 },
            new Row { Start = new Vector3(-0.14f, 0.98f,  0.10f), Burst = new Vector3(-0.6f,  0.7f,  0.3f), Delay = 0.02f, Phase =  40f, Radius = 0.26f, KnitHeight = 1.10f, KnitAngle =  35f, WingHz = 5.1f, Size = 1.45f, Perch = -1 },
            new Row { Start = new Vector3( 0.22f, 0.84f,  0.02f), Burst = new Vector3( 0.9f,  0.3f, -0.2f), Delay = 0.01f, Phase =  95f, Radius = 0.18f, KnitHeight = 0.92f, KnitAngle =  80f, WingHz = 3.8f, Size = 1.60f, Perch = 2 },
            new Row { Start = new Vector3(-0.24f, 0.80f, -0.04f), Burst = new Vector3(-0.9f,  0.2f, -0.3f), Delay = 0.03f, Phase = 150f, Radius = 0.30f, KnitHeight = 0.86f, KnitAngle = 118f, WingHz = 4.2f, Size = 1.50f, Perch = 3 },
            new Row { Start = new Vector3( 0.05f, 1.40f,  0.05f), Burst = new Vector3( 0.2f,  1.0f,  0.1f), Delay = 0.04f, Phase = 205f, Radius = 0.20f, KnitHeight = 1.52f, KnitAngle = 160f, WingHz = 5.4f, Size = 1.40f, Perch = 1 },
            new Row { Start = new Vector3(-0.08f, 1.36f, -0.10f), Burst = new Vector3(-0.3f,  0.9f, -0.5f), Delay = 0.01f, Phase = 260f, Radius = 0.24f, KnitHeight = 1.46f, KnitAngle = 200f, WingHz = 4.9f, Size = 1.35f, Perch = 4 },
            new Row { Start = new Vector3( 0.16f, 0.62f,  0.14f), Burst = new Vector3( 0.6f,  0.1f,  0.8f), Delay = 0.05f, Phase = 310f, Radius = 0.16f, KnitHeight = 0.64f, KnitAngle = 245f, WingHz = 3.6f, Size = 1.55f, Perch = -1 },
            new Row { Start = new Vector3(-0.18f, 0.58f,  0.12f), Burst = new Vector3(-0.5f,  0.0f,  0.9f), Delay = 0.02f, Phase =  20f, Radius = 0.28f, KnitHeight = 0.58f, KnitAngle = 290f, WingHz = 4.0f, Size = 1.50f, Perch = -1 },
            new Row { Start = new Vector3( 0.08f, 0.40f,  0.10f), Burst = new Vector3( 0.3f, -0.1f,  1.0f), Delay = 0.00f, Phase =  70f, Radius = 0.14f, KnitHeight = 0.32f, KnitAngle = 330f, WingHz = 5.8f, Size = 1.30f, Perch = -1 },
            new Row { Start = new Vector3(-0.10f, 0.36f, -0.08f), Burst = new Vector3(-0.4f,  0.0f, -0.9f), Delay = 0.03f, Phase = 125f, Radius = 0.20f, KnitHeight = 0.28f, KnitAngle =  15f, WingHz = 4.4f, Size = 1.35f, Perch = -1 },
            new Row { Start = new Vector3( 0.28f, 1.12f, -0.06f), Burst = new Vector3( 1.0f,  0.5f, -0.1f), Delay = 0.06f, Phase = 180f, Radius = 0.32f, KnitHeight = 1.30f, KnitAngle =  60f, WingHz = 3.9f, Size = 1.45f, Perch = -1 },
            new Row { Start = new Vector3(-0.30f, 1.08f,  0.02f), Burst = new Vector3(-1.0f,  0.4f,  0.1f), Delay = 0.04f, Phase = 235f, Radius = 0.18f, KnitHeight = 1.18f, KnitAngle = 140f, WingHz = 5.2f, Size = 1.40f, Perch = -1 },
            new Row { Start = new Vector3( 0.00f, 0.74f,  0.18f), Burst = new Vector3( 0.0f,  0.4f,  1.0f), Delay = 0.01f, Phase = 285f, Radius = 0.26f, KnitHeight = 0.78f, KnitAngle = 185f, WingHz = 4.7f, Size = 1.60f, Perch = -1 },
            new Row { Start = new Vector3( 0.02f, 0.70f, -0.16f), Burst = new Vector3( 0.1f,  0.3f, -1.0f), Delay = 0.05f, Phase = 340f, Radius = 0.22f, KnitHeight = 0.72f, KnitAngle = 225f, WingHz = 3.7f, Size = 1.50f, Perch = -1 },
            new Row { Start = new Vector3( 0.20f, 0.30f, -0.02f), Burst = new Vector3( 0.8f, -0.2f,  0.1f), Delay = 0.02f, Phase =  55f, Radius = 0.12f, KnitHeight = 0.18f, KnitAngle = 270f, WingHz = 6.0f, Size = 1.25f, Perch = -1 },
            new Row { Start = new Vector3(-0.22f, 0.26f,  0.04f), Burst = new Vector3(-0.8f, -0.2f,  0.2f), Delay = 0.00f, Phase = 110f, Radius = 0.16f, KnitHeight = 0.14f, KnitAngle = 315f, WingHz = 5.5f, Size = 1.30f, Perch = -1 },
            new Row { Start = new Vector3( 0.12f, 1.55f, -0.02f), Burst = new Vector3( 0.3f,  1.0f, -0.2f), Delay = 0.06f, Phase = 165f, Radius = 0.30f, KnitHeight = 1.64f, KnitAngle = 100f, WingHz = 4.1f, Size = 1.30f, Perch = -1 },
            new Row { Start = new Vector3(-0.12f, 1.50f,  0.08f), Burst = new Vector3(-0.2f,  1.0f,  0.4f), Delay = 0.03f, Phase = 220f, Radius = 0.20f, KnitHeight = 1.58f, KnitAngle = 250f, WingHz = 5.0f, Size = 1.35f, Perch = -1 },
            new Row { Start = new Vector3( 0.26f, 0.52f,  0.06f), Burst = new Vector3( 0.9f,  0.0f,  0.4f), Delay = 0.07f, Phase = 275f, Radius = 0.24f, KnitHeight = 0.50f, KnitAngle =  45f, WingHz = 4.3f, Size = 1.45f, Perch = -1 },
            new Row { Start = new Vector3(-0.26f, 0.48f, -0.08f), Burst = new Vector3(-0.9f, -0.1f, -0.4f), Delay = 0.05f, Phase = 325f, Radius = 0.28f, KnitHeight = 0.44f, KnitAngle = 175f, WingHz = 3.5f, Size = 1.55f, Perch = -1 },
            new Row { Beetle = true, Start = new Vector3( 0.06f, 0.90f,  0.14f), Burst = new Vector3( 0.2f,  0.6f,  0.9f), Delay = 0.00f, Phase =  10f, Radius = 0.10f, KnitHeight = 0.96f, KnitAngle =  20f, WingHz = 0f, Size = 1.70f, Perch = -1 },
            new Row { Beetle = true, Start = new Vector3(-0.08f, 0.66f,  0.12f), Burst = new Vector3(-0.3f,  0.2f,  1.0f), Delay = 0.02f, Phase =  90f, Radius = 0.12f, KnitHeight = 0.62f, KnitAngle = 110f, WingHz = 0f, Size = 1.80f, Perch = -1 },
            new Row { Beetle = true, Start = new Vector3( 0.18f, 0.44f,  0.00f), Burst = new Vector3( 0.7f,  0.0f,  0.6f), Delay = 0.01f, Phase = 170f, Radius = 0.08f, KnitHeight = 0.40f, KnitAngle = 200f, WingHz = 0f, Size = 1.60f, Perch = -1 },
            new Row { Beetle = true, Start = new Vector3(-0.16f, 1.20f, -0.04f), Burst = new Vector3(-0.5f,  0.8f, -0.4f), Delay = 0.04f, Phase = 250f, Radius = 0.14f, KnitHeight = 1.22f, KnitAngle = 290f, WingHz = 0f, Size = 1.70f, Perch = -1 },
            new Row { Beetle = true, Start = new Vector3( 0.24f, 0.98f,  0.04f), Burst = new Vector3( 1.0f,  0.4f,  0.2f), Delay = 0.03f, Phase = 320f, Radius = 0.12f, KnitHeight = 1.04f, KnitAngle = 340f, WingHz = 0f, Size = 1.75f, Perch = -1 },
            new Row { Beetle = true, Start = new Vector3(-0.02f, 0.30f,  0.16f), Burst = new Vector3( 0.0f, -0.1f,  1.0f), Delay = 0.00f, Phase =  45f, Radius = 0.06f, KnitHeight = 0.22f, KnitAngle =  75f, WingHz = 0f, Size = 1.65f, Perch = -1 },
            new Row { Beetle = true, Start = new Vector3( 0.12f, 1.30f,  0.10f), Burst = new Vector3( 0.4f,  0.9f,  0.4f), Delay = 0.06f, Phase = 135f, Radius = 0.16f, KnitHeight = 1.36f, KnitAngle = 150f, WingHz = 0f, Size = 1.60f, Perch = -1 },
            new Row { Beetle = true, Start = new Vector3(-0.20f, 0.82f, -0.12f), Burst = new Vector3(-0.8f,  0.3f, -0.6f), Delay = 0.02f, Phase = 215f, Radius = 0.10f, KnitHeight = 0.84f, KnitAngle = 240f, WingHz = 0f, Size = 1.70f, Perch = -1 },
        };

        /// <summary>Where the five stragglers settle, in her frame (right, up, forward): brim, brim, shoulders, brim back.</summary>
        private static readonly Vector3[] Perches =
        {
            new Vector3(-0.42f, 1.64f,  0.52f), new Vector3( 0.56f, 1.64f, -0.18f), new Vector3( 0.60f, 0.72f,  0.06f),
            new Vector3(-0.62f, 0.74f, -0.04f), new Vector3( 0.08f, 1.65f, -0.60f),
        };

        public const float Burst = 0.10f, StreamEnd = 0.30f, KnitStart = 0.24f, KnitEnd = 0.46f, Reveal = 0.30f, Life = 1.30f;

        /// <summary>
        /// ⚠️ v2 (film v1): AT THE ROWS' OWN SIZES THE SWARM READ AS CONFETTI from the court camera 6 m away (a 0.26 m moth is a
        /// few pixels), and the burst was too small to read as her bursting. Paete's lesson (film r16): judge sizes at the
        /// camera's real distance. Every row is drawn this much larger, and the burst throws them twice as far.
        /// </summary>
        public const float SizeScale = 1.8f, BurstReach = 0.85f;

        /// <summary>Violet smoke where she vanishes and where she knits back: (height, size, drift right, drift up, delay).</summary>
        private static readonly (float Y, float Size, float Right, float Up, float Delay)[] StartPuffs =
        {
            (0.25f, 0.55f, -0.30f, 0.20f, 0.00f), (0.70f, 0.70f, 0.35f, 0.30f, 0.01f), (1.10f, 0.60f, -0.20f, 0.45f, 0.02f),
            (1.50f, 0.50f, 0.15f, 0.55f, 0.00f), (0.45f, 0.45f, 0.45f, 0.10f, 0.03f),
        };
        private static readonly (float Y, float Size, float Right, float Up, float Delay)[] EndPuffs =
        {
            (0.20f, 0.45f, 0.30f, 0.15f, 0.26f), (0.80f, 0.55f, -0.30f, 0.25f, 0.30f), (1.35f, 0.45f, 0.20f, 0.35f, 0.34f),
            (0.50f, 0.40f, -0.40f, 0.10f, 0.28f),
        };
        /// <summary>
        /// ⚠️ v4 (film v7): the smoke along the path it took, so the court can read where she went: six puffs laid low along the
        /// stream as its middle passes each one, lingering and fraying (Castorice's blade ribbon lingers 0.3 s, then frays into
        /// motes). (share of the path, height, size, side offset, linger).
        /// </summary>
        // v4b (film v8: six 0.6 m puffs read from the court as a dotted line of small purple dots): nine, near a metre across,
        // overlapping, low, lingering longer toward where she left, so the path reads as one smoke ribbon fraying away.
        private static readonly (float U, float Y, float Size, float Side, float Linger)[] PathPuffs =
        {
            (0.06f, 0.36f, 0.95f, 0.08f, 0.60f), (0.17f, 0.30f, 1.05f, -0.12f, 0.58f), (0.28f, 0.40f, 0.90f, 0.05f, 0.55f),
            (0.39f, 0.28f, 1.00f, -0.06f, 0.52f), (0.50f, 0.36f, 0.95f, 0.12f, 0.50f), (0.61f, 0.30f, 1.05f, -0.10f, 0.48f),
            (0.72f, 0.38f, 0.90f, 0.04f, 0.45f), (0.82f, 0.30f, 0.85f, -0.08f, 0.42f), (0.91f, 0.34f, 0.80f, 0.06f, 0.40f),
        };

        /// <summary>
        /// ⚠️ v4 (film v7): on her OWN screen one moth crossed the lens at half the frame's size. Nothing of the swarm comes within a
        /// metre of her eye now; instead four small moths flick across the edges of her frame as she goes (plan 4.1: "a flicker of
        /// dark wings across the screen"). Each: start and end in the frame (x, y from -1 to 1), start time, size.
        /// </summary>
        private static readonly (Vector2 From, Vector2 To, float At, float Size)[] EdgeMoths =
        {
            (new Vector2(-0.95f, -0.80f), new Vector2(-1.30f, 0.35f), 0.02f, 0.26f), (new Vector2(0.92f, -0.75f), new Vector2(1.35f, 0.20f), 0.05f, 0.24f),
            (new Vector2(-0.70f, 0.95f), new Vector2(-1.25f, 1.20f), 0.10f, 0.20f), (new Vector2(0.80f, 0.90f), new Vector2(1.30f, 1.25f), 0.14f, 0.22f),
        };
        private readonly List<(Transform T, Transform[] W)> _edge = new List<(Transform, Transform[])>();

        private readonly List<(Transform T, Material M, Vector3 From, Vector3 Drift, float Size, float Delay, float Life)> _puffs =
            new List<(Transform, Material, Vector3, Vector3, float, float, float)>();
        private bool _ownView;
        public float LifeSeconds => Life;

        private Transform _her;
        private Vector3 _from, _to, _right, _forward;
        private readonly List<Transform> _insects = new List<Transform>();
        private readonly List<Transform[]> _wings = new List<Transform[]>();
        private readonly List<Renderer> _hidden = new List<Renderer>();
        private float _age;
        private bool _revealed;
        private Transform _ring;
        private Material _ringInk;

        /// <summary>The shove, drawn: a violet dust ring racing out over the court to the 2.5 m it pushes (0.15 s), thinning.</summary>
        public const float RingRadius = 2.5f, RingOut = 0.15f, RingFade = 0.45f;

        /// <summary>
        /// Plays the whole act. <paramref name="her"/> is her motor's transform (the stragglers ride it; it may be null in a
        /// preview); <paramref name="from"/> and <paramref name="to"/> are ground points.
        /// </summary>
        public static PhaisterSwarm Play(Transform her, Vector3 from, Vector3 to, Vector3 facing)
        {
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.001f) facing = to - from;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.001f) facing = Vector3.forward;
            var go = new GameObject("PhaisterVanishingAct");
            var fx = go.AddComponent<PhaisterSwarm>();
            fx._her = her;
            fx._from = PhaisterProp.OnCourt(from);
            fx._to = PhaisterProp.OnCourt(to);
            fx._forward = facing.normalized;
            fx._right = Vector3.Cross(Vector3.up, fx._forward).normalized;
            foreach (var row in Rows)
            {
                var prop = PhaisterProp.Spawn(row.Beetle ? "beetle" : "moth", go.transform, null, PhaisterProp.InsectOutlineWidth);
                if (prop == null) { fx._insects.Add(null); fx._wings.Add(null); continue; }
                prop.transform.localScale = Vector3.one * row.Size * SizeScale;
                fx._insects.Add(prop.transform);
                fx._wings.Add(row.Beetle
                    ? new[] { PhaisterProp.Find(prop, "elytron-l"), PhaisterProp.Find(prop, "elytron-r") }
                    : new[] { PhaisterProp.Find(prop, "wing-l"), PhaisterProp.Find(prop, "wing-r") });
            }
            var ring = VfxShapes.Lay(go.transform, "ShoveRing", VfxShapes.Hollow(40, 0.78f, 0.22f, 7), 1f, 0.03f);
            ring.transform.position = fx._from + Vector3.up * 0.03f;
            VfxMaterial.Ghost(ring.GetComponent<Renderer>(), new Color(0.58f, 0.36f, 0.82f, 0.7f), 0.35f);
            fx._ring = ring.transform;
            fx._ringInk = ring.GetComponent<Renderer>().sharedMaterial;
            // Her own screen: moths perched on her brim would sit against her camera (film v1), so they perch on her shoulders.
            var motor = her != null ? her.GetComponent<CharacterMotor>() : null;
            fx._ownView = CameraSystem.ViewmodelArms.IsFirstPersonFor(motor);
            int seed = 31;
            // ⚠️ On her own screen the smoke where she stands would sit on her lens (film v7): only the low puffs, at her feet.
            foreach (var (y, size, right, up, delay) in StartPuffs)
                if (!fx._ownView || y < 0.5f) fx.Puff(fx._from + Vector3.up * y, fx._right * right + Vector3.up * up, size, delay, 0.45f, seed++);
            foreach (var (y, size, right, up, delay) in EndPuffs)
                if (!fx._ownView || y < 0.5f) fx.Puff(fx._to + Vector3.up * y, fx._right * right + Vector3.up * up, size, delay, 0.45f, seed++);
            Vector3 along = fx._to - fx._from; along.y = 0f;
            Vector3 aside = along.sqrMagnitude > 0.01f ? Vector3.Cross(Vector3.up, along.normalized) : fx._right;
            Vector3 alongDir = along.sqrMagnitude > 0.01f ? along.normalized : fx._forward;
            foreach (var row in PathPuffs)
            {
                float passes = 0.08f + row.U * (StreamEnd - 0.08f);
                fx.Puff(Vector3.Lerp(fx._from, fx._to, row.U) + Vector3.up * row.Y + aside * row.Side, Vector3.up * 0.25f + alongDir * 0.3f,
                    row.Size, passes, row.Linger, seed++);
            }
            if (fx._ownView)
                foreach (var row in EdgeMoths)
                {
                    var m = PhaisterProp.Spawn("moth", go.transform, null, PhaisterProp.InsectOutlineWidth);
                    if (m != null) fx._edge.Add((m.transform, new[] { PhaisterProp.Find(m, "wing-l"), PhaisterProp.Find(m, "wing-r") }));
                }
            fx.HideHer();
            fx.StepTo(0f);
            return fx;
        }

        /// <summary>
        /// ⚠️ v4 (film v7): SOFT SMOKE (`Shaders/SoftPuff`), a camera-facing round puff that frays into holes as it dies. The splat
        /// discs before it were flat squares seen edge-on and a purple sheet on her own lens. Falls back to a flat ghost, with a
        /// warning, if the shader is missing.
        /// </summary>
        private void Puff(Vector3 at, Vector3 drift, float size, float delay, float life, int seed)
        {
            var shader = Resources.Load<Shader>("Shaders/SoftPuff");
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "SwarmSmoke"; VfxMaterial.StripCollider(go);
            go.transform.SetParent(transform, false);
            go.transform.position = at;
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            Material m;
            if (shader != null)
            {
                m = new Material(shader) { name = "SwarmSmoke" };
                // Her smoke: a deep violet, dense in the middle, each puff its own rim.
                m.SetColor("_Color", new Color(0.26f, 0.09f, 0.38f, 0.82f));
                m.SetFloat("_Seed", seed * 0.37f);
                m.SetFloat("_Near", 1.3f);
                r.sharedMaterial = m;
                VfxRenderTag.Own(go, m);
            }
            else
            {
                Debug.LogWarning("[PhaisterSwarm] Shaders/SoftPuff is missing; the smoke falls back to a flat ghost.");
                VfxMaterial.Ghost(r, new Color(0.30f, 0.12f, 0.46f, 0f), 0.5f);
                m = r.sharedMaterial;
            }
            _puffs.Add((go.transform, m, at, drift, size, delay, life));
        }

        private void HideHer()
        {
            if (_her == null) return;
            var visual = _her.GetComponentInChildren<CharacterVisual>();
            var model = visual != null ? visual.Model : null;
            if (model == null) return;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
                if (!r.forceRenderingOff) { r.forceRenderingOff = true; _hidden.Add(r); }
        }

        private void ShowHer()
        {
            foreach (var r in _hidden) if (r != null) r.forceRenderingOff = false;
            _hidden.Clear();
            _revealed = true;
        }

        private void OnDestroy() { if (!_revealed) ShowHer(); }

        private void Update()
        {
            StepTo(_age + Time.deltaTime);
            if (_age >= Life) Destroy(gameObject);
        }

        private Vector3 Frame(Vector3 local) => _right * local.x + Vector3.up * local.y + _forward * local.z;

        public void StepTo(float seconds)
        {
            _age = seconds;
            if (!_revealed && seconds >= Reveal) ShowHer();
            if (_ring != null)
            {
                float out01 = Mathf.Clamp01(seconds / RingOut);
                float r = Mathf.Lerp(0.3f, RingRadius, 1f - (1f - out01) * (1f - out01));
                _ring.localScale = new Vector3(r, 1f, r);
                if (_ringInk != null)
                {
                    PhaisterProp.SetAlpha(_ringInk, 0.7f * Mathf.Clamp01(1f - seconds / RingFade));
                }
                _ring.gameObject.SetActive(seconds < RingFade);
            }
            Vector3 path = _to - _from;
            float length = path.magnitude;
            Vector3 along = length > 0.01f ? path / length : _forward;
            Vector3 side = Vector3.Cross(Vector3.up, along).normalized;
            Vector3 herAt = _her != null ? _her.position : _to;
            Vector3 herRight = _her != null ? _her.right : _right, herForward = _her != null ? _her.forward : _forward;

            foreach (var (pt, pm, pFrom, drift, size, delay, life) in _puffs)
            {
                float u = (seconds - delay) / life;
                pt.gameObject.SetActive(u >= 0f && u < 1f);
                if (u < 0f || u >= 1f || pm == null) continue;
                pt.position = pFrom + drift * u;
                // Blooms fast, then drifts and frays: never a shape fading by alpha alone.
                float k = size * (0.45f + 0.95f * Mathf.Sqrt(u));
                pt.localScale = new Vector3(k, k, k);
                if (pm.HasProperty("_Fray")) { pm.SetFloat("_Fray", Mathf.Clamp01((u - 0.25f) / 0.75f)); PhaisterProp.SetAlpha(pm, 0.82f * Mathf.Clamp01(u * 10f)); }
                else PhaisterProp.SetAlpha(pm, 0.92f * (1f - u * u) * Mathf.Clamp01(u * 8f));
            }
            StepEdgeMoths(seconds);
            var eye = _ownView && UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform.position : new Vector3(0f, -1000f, 0f);

            for (int i = 0; i < Rows.Length; i++)
            {
                var t = _insects[i];
                if (t == null) continue;
                var row = Rows[i];
                Vector3 start = _from + Frame(row.Start);
                Vector3 burstAt = start + Frame(row.Burst.normalized) * BurstReach;
                float streamStart = 0.08f + row.Delay, streamEnd = StreamEnd + row.Delay * 0.5f;
                float knitStart = KnitStart + row.Delay, knitEnd = KnitEnd + row.Delay * 0.5f;
                Vector3 p; Vector3 heading; float scale = row.Size;

                if (seconds < streamStart)
                {
                    // BURST: out of her body along its own direction, fast then easing.
                    float u = Mathf.Clamp01(seconds / Burst);
                    p = Vector3.Lerp(start, burstAt, 1f - (1f - u) * (1f - u));
                    heading = Frame(row.Burst.normalized);
                    scale *= Mathf.Lerp(0.3f, 1f, u);
                }
                else if (seconds < knitStart + 0.02f)
                {
                    // STREAM: low along the aim, a ribbon twisting CLOCKWISE about the path (two turns end to end).
                    float u = Mathf.Clamp01((seconds - streamStart) / Mathf.Max(0.01f, streamEnd - streamStart));
                    float e = u * u * (3f - 2f * u);
                    Vector3 centre = Vector3.Lerp(burstAt, _to + Vector3.up * 0.35f, e);
                    centre.y = Mathf.Lerp(burstAt.y, 0.35f + _to.y, Mathf.Clamp01(u * 2.2f)) + Mathf.Sin(u * Mathf.PI) * 0.15f;
                    float a = (row.Phase - 720f * u) * Mathf.Deg2Rad;
                    p = centre + (side * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * row.Radius * Mathf.Sin(Mathf.Clamp01(u * 1.3f) * Mathf.PI * 0.5f);
                    heading = along;
                }
                else if (row.Perch >= 0 && !_ownView && seconds >= knitEnd)
                {
                    // STRAGGLERS: settle on her brim and shoulders, beat slowly, then crawl into her sleeves.
                    var spot = Perches[_ownView && Perches[row.Perch].y > 1.0f ? 2 + row.Perch % 2 : row.Perch];
                    Vector3 perch = herAt + herRight * spot.x + Vector3.up * spot.y + herForward * spot.z;
                    Vector3 sleeve = herAt + herRight * (Perches[row.Perch].x > 0 ? 0.55f : -0.55f) + Vector3.up * 0.55f;
                    float settle = Mathf.Clamp01((seconds - knitEnd) / 0.18f);
                    float crawl = Mathf.Clamp01((seconds - 1.02f) / 0.26f);
                    Vector3 knitPoint = herAt + Vector3.up * row.KnitHeight;
                    p = Vector3.Lerp(Vector3.Lerp(knitPoint, perch, settle * settle * (3f - 2f * settle)), sleeve, crawl);
                    heading = herForward;
                    scale *= 1f - crawl;
                }
                else
                {
                    // KNIT: spiral CLOCKWISE up round the arrival, tightening, and pack in at its own height (lowest first).
                    float u = Mathf.Clamp01((seconds - knitStart) / Mathf.Max(0.01f, knitEnd - knitStart));
                    float e = u * u;
                    float a = (row.KnitAngle - 300f * u) * Mathf.Deg2Rad;
                    float r = Mathf.Lerp(0.55f, 0.04f, e);
                    p = herAt + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r + Vector3.up * Mathf.Lerp(0.30f, row.KnitHeight, e);
                    heading = new Vector3(Mathf.Sin(a), 0f, -Mathf.Cos(a));
                    // ⚠️ On her own screen every insect packs in (film v2: a perched moth sat against her camera even on a shoulder).
                    if (row.Perch < 0 || _ownView) scale *= 1f - Mathf.Clamp01((u - 0.7f) / 0.3f);
                }

                // ⚠️ Nothing within a metre of her own eye (film v7: a moth the size of half her frame).
                if (_ownView) scale *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 1.5f, Vector3.Distance(p, eye)));
                t.position = p;
                if (heading.sqrMagnitude > 0.0001f) t.rotation = Quaternion.LookRotation(heading.normalized, Vector3.up);
                t.localScale = Vector3.one * Mathf.Max(0.0001f, scale * SizeScale);
                t.gameObject.SetActive(scale > 0.001f);
                Beat(i, row, seconds);
            }
        }

        /// <summary>Her own screen: four small moths flick across the frame's edges and out (`EdgeMoths`), 0.3 s each.</summary>
        private void StepEdgeMoths(float seconds)
        {
            var cam = UnityEngine.Camera.main;
            for (int i = 0; i < _edge.Count; i++)
            {
                var (t, w) = _edge[i];
                var row = EdgeMoths[i];
                float u = (seconds - row.At) / 0.30f;
                bool on = cam != null && u >= 0f && u < 1f;
                t.gameObject.SetActive(on);
                if (!on) continue;
                const float Depth = 0.75f;
                float halfH = Depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad), halfW = halfH * cam.aspect;
                Vector2 at = Vector2.Lerp(row.From, row.To, u * u);
                var ct = cam.transform;
                t.position = ct.position + ct.forward * Depth + ct.right * at.x * halfW + ct.up * at.y * halfH;
                Vector3 dir = ct.right * (row.To.x - row.From.x) * halfW + ct.up * (row.To.y - row.From.y) * halfH;
                t.rotation = Quaternion.LookRotation(dir.normalized, -ct.forward);
                t.localScale = Vector3.one * row.Size;
                float open = 10f + 60f * (0.5f + 0.5f * Mathf.Sin(seconds * 5.5f * Mathf.PI * 2f + i));
                if (w[0] != null) w[0].localRotation = Quaternion.AngleAxis(open, Vector3.forward);
                if (w[1] != null) w[1].localRotation = Quaternion.AngleAxis(-open, Vector3.forward);
            }
        }

        /// <summary>Wings beat slowly about the body's long axis (3 to 6 a second); a beetle's shell stays half open in flight.</summary>
        private void Beat(int i, Row row, float seconds)
        {
            var w = _wings[i];
            if (w == null) return;
            float open = row.Beetle ? 35f : 10f + 55f * (0.5f + 0.5f * Mathf.Sin(seconds * row.WingHz * Mathf.PI * 2f + row.Phase * Mathf.Deg2Rad));
            if (w[0] != null) w[0].localRotation = Quaternion.AngleAxis(open, Vector3.forward);
            if (w[1] != null) w[1].localRotation = Quaternion.AngleAxis(-open, Vector3.forward);
        }
    }
}
