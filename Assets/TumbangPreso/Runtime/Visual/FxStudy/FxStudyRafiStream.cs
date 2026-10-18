using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// STUDY K, THE CROSSCURRENT AS A STREAM (the owner chose a stream over a rolling ball). Ilyas aims a narrow current:
    /// after 0.18 s (`RafiRules.CurrentGather`) its head runs straight out at 8 m/s for 6 m (`CurrentRange`), and the
    /// first flying slipper it meets is bent 40 degrees, which spends it; solid cover spends it too. What meets a slipper
    /// is a column round the head, 0.65 m in radius (`CurrentRadius`) at 0.85 m up. It moves no player.
    ///
    /// So the picture is a STREAM OF WATER thrown along the aim at chest height, exactly as wide as what it hits (1.3 m),
    /// whose head is where the rule's column is, and whose body behind is HISTORY: it stays on the line it was thrown
    /// along, is never turned, and dies where it lies, thinning and tearing from its tail until it is gone.
    ///
    /// What it is made of, in the wall's language (the body the owner chose there):
    ///   * TWO SEE-THROUGH TUBES, an outer one the full width and a deeper inner one, wearing the wall's streaks
    ///     (`water_flow`) read twice at two speeds and multiplied, so the inside churns every frame; their outline
    ///     swells, pinches and sways from the clock (`RafiStream.shader`);
    ///   * THREE STRANDS, thin tubes wound round it, each with its own lag, speed and death: the thin fast white lines;
    ///   * A CHURNING WHITE CAP where it leads; the rim of each tube a little paler where it turns from the eye.
    ///
    /// ⚠️ CHEAP BY CONSTRUCTION. Two meshes, built once and shared by every cast; two materials per light setting, shared;
    /// nothing allocated or rebuilt while it plays. Per frame a cast sets one property block per renderer (clock,
    /// where its head and tail are, its burst, its opacity) and the shader does the rest. No drops: in a match the spray
    /// at its burst is meant to be the pooled cloud puffs (`CloudPuffs.Spawn`) in water colours.
    ///
    /// The motion is our own design. What was studied: the game's own lagoon water (`LagoonCoveWater.shader`: turquoise
    /// shallows to a blue-green deep, bright wobbly lines) and the wall.
    /// </summary>
    public sealed class FxStudyRafiStream : FxStudyEffect
    {
        public const float Radius = Core.RafiRules.CurrentRadius, Height = 0.85f, Gather = Core.RafiRules.CurrentGather,
                           Range = Core.RafiRules.CurrentRange, Speed = 8f;
        /// <summary>Its life untouched (the field's own duration), and how long it takes to die after it ends or is spent.</summary>
        public const float Life = Gather + Range / Speed, Die = 0.45f;
        // How tall it is against how wide: a stream, not a pipe. (The rule's column is 1.4 m tall; the stream is lower.)
        private const float Flat = 0.62f;

        public const string ShaderName = "TumbangPreso/RafiStream";
        private static readonly int AgeId = Shader.PropertyToID("_Age"), OpacityId = Shader.PropertyToID("_Opacity"), HeadId = Shader.PropertyToID("_Head"),
                                    TailId = Shader.PropertyToID("_Tail"), BurstId = Shader.PropertyToID("_Burst"), SpeedId = Shader.PropertyToID("_Speed"), SeedId = Shader.PropertyToID("_Seed");

        // ---- built once, shared by every cast.
        private static Mesh _tube, _strand;
        private sealed class Kit { public Material Outer, Inner, Strand; }
        private static readonly Dictionary<int, Kit> Kits = new Dictionary<int, Kit>();

        // One strand: where round the stream it starts (degrees), how far behind the head it runs and how far its tail
        // leads the stream's (m), how fast its pictures run (a share), and its seed.
        private readonly struct Strand
        {
            public readonly float Turn, Lag, Lead, Pace, Seed;
            public Strand(float turn, float lag, float lead, float pace, float seed) { Turn = turn; Lag = lag; Lead = lead; Pace = pace; Seed = seed; }
        }
        private static readonly Strand[] Strands =
        {
            new Strand(  0f, 0.10f, 0.45f, 1.25f, 0.31f),
            new Strand(130f, 0.30f, 0.15f, 0.95f, 0.67f),
            new Strand(250f, 0.18f, 0.80f, 1.10f, 0.13f),
        };

        private sealed class Part { public MeshRenderer Renderer; public readonly MaterialPropertyBlock Block = new MaterialPropertyBlock(); }
        private readonly Part _outer, _inner;
        private readonly Part[] _strands = new Part[Strands.Length];
        private readonly float _seed, _benchSpentAt;
        private readonly int _view;

        public override float Seconds => 0.3f + (_benchSpentAt > 0f ? _benchSpentAt : Life) + Die + 0.15f;
        public override Color FloorColour => new Color(0.150f, 0.150f, 0.158f);
        public override Color FloorLine => new Color(0.120f, 0.120f, 0.128f);
        public override float BloomIntensity => 0.30f;
        /// <summary>The four views it is judged from: 0 the side, 1 behind the caster, 2 in front looking back down its
        /// path (a thrower's), 3 from above.</summary>
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov) => View(_view, out eye, out focus, out fov);
        public static void View(int view, out Vector3 eye, out Vector3 focus, out float fov)
        {
            switch (view)
            {
                case 1: focus = new Vector3(0f, 0.9f, 3.5f); eye = new Vector3(0.6f, 1.75f, -1.6f); fov = 60f; return;
                case 2: focus = new Vector3(0f, 0.9f, 2.5f); eye = new Vector3(-0.5f, 1.6f, 9.0f); fov = 60f; return;
                case 3: focus = new Vector3(0f, 0.0f, 3.0f); eye = new Vector3(0.01f, 9.5f, 3.0f); fov = 50f; return;
                default: focus = new Vector3(0f, 0.85f, 3.0f); eye = focus + new Vector3(6.6f, 2.0f, -2.6f); fov = 44f; return;
            }
        }

        public FxStudyRafiStream(Transform parent) : this(parent, FxStudyRafiWall.Look.Bench, -1f, 0f, 0) { }

        /// <summary>`benchSpentAt` above 0 spends the bench's stream at that age; `seed` slides each cast's pictures; `view`
        /// is which of the four views the bench films it from.</summary>
        public FxStudyRafiStream(Transform parent, FxStudyRafiWall.Look look, float benchSpentAt, float seed, int view) : base("rafi-stream", parent)
        {
            _benchSpentAt = benchSpentAt; _seed = seed; _view = view;
            BuildMeshes();
            var kit = KitFor(look);
            var along = Group("along");
            along.localPosition = new Vector3(0f, Height, 0f);
            _outer = Make("outer", _tube, kit.Outer, along, Vector3.one);
            _inner = Make("inner", _tube, kit.Inner, along, new Vector3(0.62f, 0.62f, 1f));
            for (int i = 0; i < Strands.Length; i++)
            {
                var part = Make("strand-" + i, _strand, kit.Strand, along, Vector3.one);
                part.Renderer.transform.localRotation = Quaternion.Euler(0f, 0f, Strands[i].Turn);
                _strands[i] = part;
            }
        }

        private static Part Make(string name, Mesh mesh, Material material, Transform parent, Vector3 scale)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.enabled = false;
            return new Part { Renderer = renderer };
        }

        // A tube along +z from 0 to `Range`, its middle following `path`, `rx` wide and `ry` tall either side of it.
        // uv x along it, y round it; uv2 the middle of its cross-section, which the shader thins and sways about.
        private static Mesh Tube(int rows, int round, float rx, float ry, System.Func<float, Vector3> path)
        {
            int columns = round + 1;
            var vertices = new Vector3[(rows + 1) * columns]; var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length]; var middles = new List<Vector3>(vertices.Length);
            for (int r = 0; r <= rows; r++)
            {
                float u = r / (float)rows; Vector3 c = path(u);
                for (int k = 0; k < columns; k++)
                {
                    float a = k / (float)round * Mathf.PI * 2f, cos = Mathf.Cos(a), sin = Mathf.Sin(a);
                    vertices[r * columns + k] = c + new Vector3(cos * rx, sin * ry, 0f);
                    normals[r * columns + k] = new Vector3(cos / rx, sin / ry, 0f).normalized;
                    uvs[r * columns + k] = new Vector2(u, k / (float)round);
                    middles.Add(c);
                }
            }
            var triangles = new int[rows * round * 6]; int n = 0;
            for (int r = 0; r < rows; r++)
                for (int k = 0; k < round; k++)
                {
                    int i = r * columns + k;
                    triangles[n++] = i; triangles[n++] = i + columns; triangles[n++] = i + 1;
                    triangles[n++] = i + 1; triangles[n++] = i + columns; triangles[n++] = i + columns + 1;
                }
            var mesh = new Mesh { name = "RafiStreamTube", hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uvs; mesh.SetUVs(1, middles); mesh.triangles = triangles;
            // Its bounds are what it can reach once the shader has swollen and swayed it.
            mesh.bounds = new Bounds(new Vector3(0f, 0f, Range * 0.5f), new Vector3(rx * 4f, ry * 4f, Range + 1f));
            mesh.UploadMeshData(true);
            return mesh;
        }

        private static void BuildMeshes()
        {
            if (_tube != null) return;
            _tube = Tube(72, 16, Radius, Radius * Flat, u => new Vector3(0f, 0f, u * Range));
            // A strand winds one and a half times round the stream, inside its rim.
            _strand = Tube(96, 6, 0.045f, 0.045f, u => { float a = u * Mathf.PI * 3f; return new Vector3(Mathf.Cos(a) * Radius * 0.82f, Mathf.Sin(a) * Radius * Flat * 0.82f, u * Range); });
        }

        private static Material Paint(FxStudyRafiWall.Look look, float light, int queue)
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null) return null;
            var m = new Material(shader) { hideFlags = HideFlags.HideAndDontSave, renderQueue = queue };
            m.SetTexture("_MaskTex", FxStudyKit.Mask("water_flow")); m.SetTexture("_LineTex", FxStudyKit.Mask("water_lines")); m.SetTexture("_ErodeTex", FxStudyKit.Mask("erode_cloud"));
            m.SetColor("_Deep", look.Deep); m.SetColor("_Body", look.Body); m.SetColor("_Pale", look.Pale); m.SetColor("_Foam", look.Foam);
            m.SetFloat("_Light", light); m.SetFloat("_HueDrift", look.HueDrift);
            return m;
        }

        private static Kit KitFor(FxStudyRafiWall.Look look)
        {
            int key = Mathf.RoundToInt(look.Light * 100f);
            if (Kits.TryGetValue(key, out var kit) && kit.Outer != null) return kit;
            kit = new Kit();
            kit.Outer = Paint(look, look.Light, 3002);
            kit.Inner = Paint(look, look.Light * 0.5f, 3001);
            if (kit.Inner != null)
            {
                // Deeper, slower, the other way round, and fewer lines: the body inside the body.
                kit.Inner.SetColor("_Body", Color.Lerp(look.Deep, look.Body, 0.6f)); kit.Inner.SetColor("_Pale", look.Body);
                kit.Inner.SetVector("_Scroll1", new Vector4(1.4f, -0.4f)); kit.Inner.SetVector("_Scroll2", new Vector4(0.8f, 0.3f));
                kit.Inner.SetFloat("_LineGain", 0.25f); kit.Inner.SetFloat("_Rim", 0.3f);
                kit.Inner.SetVector("_Wobble", new Vector4(0.04f, 4f, 5.5f)); kit.Inner.SetVector("_Cut", new Vector4(0.06f, 0.65f));
            }
            kit.Strand = Paint(look, Mathf.Lerp(0.5f, 0.9f, look.Light), 3003);
            if (kit.Strand != null)
            {
                // A strand is mostly line: its body only where both streak pictures are bright, its lines fast.
                kit.Strand.SetVector("_Cut", new Vector4(0.30f, 0.80f)); kit.Strand.SetFloat("_LineGain", 1.6f); kit.Strand.SetFloat("_Rim", 0.2f);
                kit.Strand.SetVector("_LineTile", new Vector4(3f, 1f)); kit.Strand.SetVector("_LineScroll", new Vector4(4.0f, 0f));
                kit.Strand.SetFloat("_Thin", 0.10f); kit.Strand.SetFloat("_Nose", 0.03f);
                kit.Strand.SetVector("_Wobble", new Vector4(0.07f, 3f, 9f)); kit.Strand.SetVector("_Ripple", new Vector4(0.25f, 1f, 30f, 18f));
            }
            Kits[key] = kit;
            return kit;
        }

        public override void Sample(float age, Vector3 eye) => Pose(age - 0.3f, _benchSpentAt);

        /// <summary>
        /// Poses it `t` seconds after its cast, in its own space (its start at the origin, thrown along +z). `spentAt`
        /// above 0 is when it was spent; otherwise it runs its whole 6 m. ⚠️ ITS HEAD IS WHERE THE RULE'S COLUMN IS:
        /// `Speed` metres a second from the end of the gather (`RafiWaterField.Travel`'s sum), and it never goes past
        /// where it ended.
        /// </summary>
        public void Pose(float t, float spentAt)
        {
            float age = Mathf.Max(0f, t), end = spentAt > 0f ? Mathf.Min(spentAt, Life) : Life;
            bool live = t >= 0f;
            float travel = Mathf.Min(Range, Mathf.Max(0f, Mathf.Min(t, end) - Gather) * Speed);
            // While it gathers it is a short swell of water at the hand that grows; then the head runs.
            float head = Mathf.Max(travel + Radius * 0.6f, 0.55f * FxStudyKit.EaseOut(FxStudyKit.Run(t, 0f, Gather)));
            // The tail: the stream is let go from the hand a little after it is thrown and its tail follows more slowly
            // than the head (3.5 m/s against 8), so it lengthens to nearly 5 m by its end; then the tail runs up to the head
            // and it dies there. (At 6.2 m/s, film s1, the stream never grew past 2.4 m and read as a blob, not a stream.)
            float after = Mathf.Max(0f, t - end);
            float tail = Mathf.Max(0f, t - Gather - 0.30f) * 3.5f + after * after * 30f;
            tail = Mathf.Min(tail, head);
            float burst = FxStudyKit.Run(t, end, end + Die);
            float opacity = live && t < end + Die ? 0.9f : 0f;
            Frame(_outer, age, opacity, head, tail, burst, 1f, _seed);
            Frame(_inner, age, opacity * 0.8f, head - 0.15f, tail + 0.2f, burst, 1f, _seed + 0.41f);
            for (int i = 0; i < Strands.Length; i++)
            {
                Strand s = Strands[i];
                float strandHead = Mathf.Max(0f, head - s.Lag), strandTail = Mathf.Min(strandHead, tail + s.Lead + after * 2f);
                Frame(_strands[i], age, opacity, strandHead, strandTail, FxStudyKit.Run(t, end - 0.05f * i, end + Die * 0.8f), s.Pace, _seed + s.Seed);
            }
        }

        // Its clock, where its head and tail are (m along it), how far it has burst and how fast its pictures run, in its
        // own block: the shared material is not touched.
        private static void Frame(Part part, float age, float opacity, float head, float tail, float burst, float pace, float seed)
        {
            bool on = opacity > 0.002f && head - tail > 0.02f && burst < 0.999f && part.Renderer.sharedMaterial != null;
            if (part.Renderer.enabled != on) part.Renderer.enabled = on;
            if (!on) return;
            part.Block.SetFloat(AgeId, age); part.Block.SetFloat(OpacityId, opacity); part.Block.SetFloat(HeadId, head / Range);
            part.Block.SetFloat(TailId, tail / Range); part.Block.SetFloat(BurstId, burst); part.Block.SetFloat(SpeedId, pace); part.Block.SetFloat(SeedId, seed);
            part.Renderer.SetPropertyBlock(part.Block);
        }
    }
}
