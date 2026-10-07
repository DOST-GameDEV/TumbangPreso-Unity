using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Map
{
    /// <summary>
    /// The Arena's pooled light: every spark, ring, streak, glint and beam the map's effects draw
    /// (owner, 2026-10-05: "there should be more vfx overall in the map, including the drone
    /// stuff"). The transformation's show (`ArenaShow`), the drone (`ArenaDrone`) and the ambient
    /// and reactive effects (`ArenaAmbience`) all draw through this and own no renderer.
    ///
    /// ONE MESH, ONE MATERIAL, ONE DRAW CALL. Each effect is a quad in a fixed pool, textured
    /// from one small atlas painted here in code (a soft dot, a shock ring, a streak, a glint,
    /// a chevron, a disc, a thin ring, a scan ring, and eight COMIC shapes with hard edges and
    /// flat fills: see `Cell`) and coloured by its vertices, on
    /// TumbangPreso/ArenaGlow with `_VertexTint` on: unlit, added, no depth write. The quads are
    /// rebuilt each frame into arrays made once, so nothing is allocated after `Awake`, and with
    /// nothing alive nothing is uploaded.
    ///
    /// TWO KINDS OF QUAD. A PARTICLE is emitted once and lives its life here (position, speed,
    /// gravity, drag, growth, fade). An IMMEDIATE quad is drawn for this frame only, by a caller
    /// that poses it every frame itself (a searchlight, the shaft's rings, a tractor beam): call
    /// before this component's `LateUpdate`, which runs after everything else.
    ///
    /// ⚠️ ITS CLOCK STILL RUNS IN A BREAK. `Time.timeScale` is 0 while the stage transforms, and
    /// that is exactly when this is busiest, so in a hold it steps on unscaled time. Outside one
    /// it steps on game time, so a pause stops the sparks with everything else.
    ///
    /// PRESENTATION ONLY. Nothing here is sent, read by gameplay, or drawn from the gameplay
    /// random stream (`Rand` is its own). Reduced effects keeps every shape, halves the light
    /// and halves what a burst throws.
    ///
    /// It installs itself (`Ensure`) wherever an `ArenaStage` is loaded, so a scene built before
    /// it existed has it too; `ArenaSceneBuilder` adds it plainly.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed partial class ArenaFx : MonoBehaviour
    {
        public static ArenaFx Instance { get; private set; }

        /// <summary>
        /// The atlas cells, left to right, top row first. The first eight are LIGHT (soft, the
        /// show's and the ambience's). The second eight are COMIC (owner, 2026-10-05: "drone
        /// design and ufo effect needs to be more stylized"): hard edges and flat fills, the
        /// game's own callout language drawn as shapes. Band: a bold flat ring. Star5: a chunky
        /// five-pointed star. Burst: a zigzag crown, a comic burst's outline. Halftone: a disc
        /// of dots that shrink toward its rim. Target: a ring, four darts and a spot. Line: a
        /// speed line, a sliver pointed at both ends. Sparkle: a fat four-pointed twinkle.
        /// Puff: a three-lobed cartoon cloud. `tools/review_arena_drone.py` paints the same
        /// sixteen (`fx_shape`) into Logs/arena/stage/drone_fx_atlas_v6.png.
        /// </summary>
        public enum Cell : byte
        {
            Dot, Ring, Streak, Star, Chevron, Disc, ThinRing, Scan,
            Band, Star5, Burst, Halftone, Target, Line, Sparkle, Puff
        }

        /// <summary>How a particle's quad is turned. Billboard faces the eye; Flat lies on the
        /// ground plane turned by its yaw; Stretch runs along its own travel; Upright stands on
        /// the vertical, its foot at the position.</summary>
        public enum Mode : byte { Billboard, Flat, Stretch, Upright }

        public const int MaxParticles = 1024, MaxImmediate = 640;
        private const int Quads = MaxParticles + MaxImmediate;
        private const int AtlasColumns = 4, AtlasRows = 4, CellPixels = 128;

        // The palette. Nothing near the team hues #f87020 and #0080e8 (the art brief's rule 7):
        // the stage's own cyan-white, a deep LED indigo, the kit's teal, lime and violet, a
        // floodlight white, a pyro gold (yellow, not orange) and the city's magenta.
        public static readonly Color Cyan = new Color(0.45f, 0.95f, 1.0f);
        public static readonly Color White = new Color(0.92f, 0.96f, 1.0f);
        public static readonly Color Indigo = new Color(0.22f, 0.26f, 1.0f);
        public static readonly Color Teal = new Color(0.30f, 0.95f, 0.85f);
        public static readonly Color Lime = new Color(0.72f, 1.0f, 0.45f);
        public static readonly Color Violet = new Color(0.72f, 0.42f, 1.0f);
        public static readonly Color Gold = new Color(1.0f, 0.86f, 0.30f);
        public static readonly Color Magenta = new Color(1.0f, 0.30f, 0.85f);

        private struct Particle
        {
            public Vector3 Position, Velocity;
            public float Age, Life, Gravity, Drag;
            public float Width0, Width1, Length0, Length1, Yaw, FadeIn, Alpha;
            public Color Colour;
            public Cell Cell;
            public Mode Mode;
        }

        private readonly Particle[] _particles = new Particle[MaxParticles];
        private int _alive;

        private readonly Vector3[] _vertices = new Vector3[Quads * 4];
        private readonly Color32[] _colours = new Color32[Quads * 4];
        private readonly Vector2[] _uvs = new Vector2[Quads * 4];
        private int _immediate, _drawn;

        private Mesh _mesh;
        private Material _material;
        private Texture2D _atlas;
        private MeshRenderer _renderer;
        private uint _seed = 0x9E3779B9u;
        private Vector3 _eye, _right = Vector3.right, _up = Vector3.up;

        /// <summary>1, or 0.5 under reduced effects: what every alpha is multiplied by.</summary>
        public static float Light => Settings.SettingsStore.Current.ReducedEffects ? 0.5f : 1.0f;

        /// <summary>How many of `count` a burst throws: half under reduced effects, never none.</summary>
        public static int Count(int count) =>
            Settings.SettingsStore.Current.ReducedEffects ? Mathf.Max(1, count / 2) : count;

        /// <summary>
        /// The clock the map's ambient motion runs on: in a session the server clock every peer
        /// shares (as `ArenaTraffic` reads it), so every peer draws the same sky; offline this
        /// peer's own game clock.
        /// </summary>
        public static double SharedClock
        {
            get
            {
                var nm = Unity.Netcode.NetworkManager.Singleton;
                return NetAuthority.IsNetworked && nm != null && nm.IsListening ? nm.ServerTime.Time : Time.timeAsDouble;
            }
        }

        /// <summary>The step effects advance by this frame: game time, or real time in a hold.</summary>
        public static float Step => PresentationClock.Held ? Mathf.Min(Time.unscaledDeltaTime, 0.05f) : Time.deltaTime;

        /// <summary>The camera the picture is being drawn through: the break's while it shows, else the game's.</summary>
        public static Camera View
        {
            get
            {
                var cut = ArenaBreakCamera.Showing;
                return cut != null ? cut : Camera.main;
            }
        }

        /// <summary>
        /// A sound in the world for an effect of this map, on THIS peer only. ⚠️ NEVER `NetCue`:
        /// every peer fires these for itself from state it already has (the break's clock, a
        /// body's replicated carry), so a relay would play each of them twice.
        /// </summary>
        public static void Cue(string id, Vector3 at, float pitchLow = 0.97f, float pitchHigh = 1.03f, float volume = 1.0f)
            => GameServices.Audio?.PlayAtVaried(id, at, pitchLow, pitchHigh, volume);

        /// <summary>The same, not in the world: the break is watched through its own camera,
        /// far from the game camera that carries the listener.</summary>
        public static void CueFlat(string id, float pitchLow = 1.0f, float pitchHigh = 1.0f, float volume = 1.0f)
            => GameServices.Audio?.PlayUiVaried(id, pitchLow, pitchHigh, volume);

        /// <summary>The effects of the loaded Arena, made if the scene has none. Null on every other map.</summary>
        public static ArenaFx Ensure()
        {
            if (Instance != null) return Instance;
            var stage = ArenaStage.Instance;
            if (stage == null) return null;

            var holder = new GameObject("Effects (self-installed)");
            holder.transform.SetParent(stage.transform.parent, false);
            return holder.AddComponent<ArenaFx>();
        }

        private void Awake()
        {
            Instance = this;
            // The mesh is in world space whatever this object's parent does.
            var drawn = new GameObject("Arena effects mesh");
            drawn.transform.SetParent(transform, false);
            drawn.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            _mesh = new Mesh { name = "Arena effects" };
            _mesh.MarkDynamic();
            var triangles = new int[Quads * 6];
            for (int q = 0; q < Quads; q++)
            {
                int v = q * 4, t = q * 6;
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                triangles[t + 3] = v; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colours);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetTriangles(triangles, 0, false);
            // Never culled by its bounds: the effects are anywhere from the shaft to the sky.
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 6000.0f);

            _atlas = PaintAtlas();
            _material = MakeMaterial(_atlas);

            drawn.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = drawn.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = _material;
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _renderer.enabled = false;

            if (GetComponent<ArenaShow>() == null) gameObject.AddComponent<ArenaShow>();
            if (GetComponent<ArenaAmbience>() == null) gameObject.AddComponent<ArenaAmbience>();
        }

        private void OnEnable() => Instance = this;

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
            _alive = 0; _immediate = 0;
            if (_renderer != null) _renderer.enabled = false;
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_material != null) Destroy(_material);
            if (_atlas != null) Destroy(_atlas);
        }

        // ------------------------------------------------------------------ the material and its atlas

        private static Material MakeMaterial(Texture2D atlas)
        {
            var shader = Shader.Find("TumbangPreso/ArenaGlow");
            Material material;
            if (shader != null)
            {
                material = new Material(shader) { name = "Arena effects" };
                material.SetColor("_Color", new Color(1.5f, 1.5f, 1.5f, 1.0f));
                material.SetFloat("_Rim", 0.0f);
                material.SetVector("_Near", new Vector4(0.25f, 1.2f, 0.0f, 0.0f));
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.One);
                material.SetFloat("_Cull", (float)CullMode.Off);
                material.SetFloat("_Fog", 0.0f);
                material.SetFloat("_VertexTint", 1.0f);
            }
            else
            {
                // Sprites/Default is always in a build: blended, not added, but the shapes and colours stand.
                Debug.LogWarning("[Arena] TumbangPreso/ArenaGlow is missing: the map's effects are drawn blended, not as light.");
                material = new Material(Shader.Find("Sprites/Default")) { name = "Arena effects (fallback)" };
            }

            material.mainTexture = atlas;
            material.renderQueue = (int)RenderQueue.Transparent + 40;
            material.hideFlags = HideFlags.DontSave;
            return material;
        }

        /// <summary>The sixteen shapes, white with the shape in the alpha, each fading to nothing
        /// inside its own cell so neighbours never bleed.</summary>
        private static Texture2D PaintAtlas()
        {
            int width = AtlasColumns * CellPixels, height = AtlasRows * CellPixels;
            var pixels = new Color32[width * height];
            for (int cell = 0; cell < AtlasColumns * AtlasRows; cell++)
            {
                int column = cell % AtlasColumns, row = AtlasRows - 1 - cell / AtlasColumns;
                for (int py = 0; py < CellPixels; py++)
                    for (int px = 0; px < CellPixels; px++)
                    {
                        // -1..1 across the cell, +y up.
                        float x = (px + 0.5f) / CellPixels * 2.0f - 1.0f, y = (py + 0.5f) / CellPixels * 2.0f - 1.0f;
                        float alpha = Mathf.Clamp01(Shape((Cell)cell, x, y));
                        // A hard guarantee of nothing at the cell's rim, whatever the shape says.
                        alpha *= Mathf.Clamp01((1.0f - Mathf.Max(Mathf.Abs(x), Mathf.Abs(y))) * 12.0f);
                        pixels[(row * CellPixels + py) * width + column * CellPixels + px] = new Color32(255, 255, 255, (byte)(alpha * 255.0f + 0.5f));
                    }
            }

            var atlas = new Texture2D(width, height, TextureFormat.RGBA32, true) { name = "Arena effects atlas", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            atlas.hideFlags = HideFlags.DontSave;
            atlas.SetPixels32(pixels);
            atlas.Apply(true, true);
            return atlas;
        }

        private static float Shape(Cell cell, float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            switch (cell)
            {
                case Cell.Dot: return Mathf.Exp(-r * r * 5.5f) * Mathf.Clamp01((0.95f - r) * 6.0f);
                case Cell.Ring: return Band(r, 0.80f, 0.085f) + 0.22f * Mathf.Clamp01((r - 0.35f) / 0.45f) * Mathf.Clamp01((0.80f - r) * 30.0f);
                case Cell.Streak: return Mathf.Exp(-x * x * 26.0f) * Mathf.Clamp01((0.92f - Mathf.Abs(y)) * 2.2f) * Mathf.Clamp01((1.0f - Mathf.Abs(y)) * 1.2f);
                case Cell.Star:
                {
                    float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
                    float cross = Mathf.Max(Mathf.Exp(-ay * ay * 420.0f) * (1.0f - ax), Mathf.Exp(-ax * ax * 420.0f) * (1.0f - ay));
                    float d0 = Mathf.Abs(x - y) * 0.7071f, d1 = Mathf.Abs(x + y) * 0.7071f;
                    float diagonal = Mathf.Max(Mathf.Exp(-d0 * d0 * 700.0f), Mathf.Exp(-d1 * d1 * 700.0f)) * Mathf.Clamp01(1.0f - r * 2.2f) * 0.7f;
                    return Mathf.Max(cross * Mathf.Clamp01(1.05f - r), diagonal) + Mathf.Exp(-r * r * 40.0f);
                }
                case Cell.Chevron:
                {
                    // A "^" whose point is up: two arms 0.16 thick.
                    float arm = Mathf.Abs(y - (0.35f - Mathf.Abs(x) * 0.9f));
                    return Mathf.Clamp01((0.13f - arm) * 14.0f) * Mathf.Clamp01((0.82f - Mathf.Abs(x)) * 8.0f);
                }
                case Cell.Disc: return Mathf.Clamp01((0.92f - r) * 3.2f) * (0.55f + 0.45f * Mathf.Exp(-r * r * 2.0f));
                case Cell.ThinRing: return Band(r, 0.90f, 0.022f);
                case Cell.Scan: return Band(r, 0.90f, 0.03f) + 0.5f * Mathf.Clamp01((r - 0.55f) / 0.35f) * Mathf.Clamp01((0.90f - r) * 40.0f);

                // ---- The comic shapes: a hard edge is a ramp `Hard` steep, about three pixels of feather.
                case Cell.Band: return Mathf.Clamp01((0.115f - Mathf.Abs(r - 0.76f)) * Hard);
                case Cell.Star5:
                {
                    // The straight edge from a point (0.92) to the notch beside it (0.44), by the angle from the nearest point.
                    float u = Mathf.Atan2(x, y) * 5.0f / (2.0f * Mathf.PI);
                    float phi = Mathf.Abs(u - Mathf.Floor(u + 0.5f)) * (2.0f * Mathf.PI / 5.0f);
                    const float fifth = Mathf.PI / 5.0f;
                    float edge = 0.92f * 0.44f * Mathf.Sin(fifth) / (0.92f * Mathf.Sin(phi) + 0.44f * Mathf.Sin(fifth - phi));
                    return Mathf.Clamp01((edge - r) * Hard);
                }
                case Cell.Burst:
                {
                    // Twelve teeth: the line zigzags between 0.54 and 0.86.
                    float edge = 0.70f + 0.16f * (Mathf.Abs(Mathf.Repeat(Mathf.Atan2(x, y) * 12.0f / (2.0f * Mathf.PI), 1.0f) * 2.0f - 1.0f) * 2.0f - 1.0f);
                    return Mathf.Clamp01((0.075f - Mathf.Abs(r - edge)) * Hard);
                }
                case Cell.Halftone:
                {
                    float gx = Mathf.Repeat(x * 5.0f + 0.5f, 1.0f) - 0.5f, gy = Mathf.Repeat(y * 5.0f + 0.5f, 1.0f) - 0.5f;
                    return Mathf.Clamp01((0.44f * (1.0f - r) - Mathf.Sqrt(gx * gx + gy * gy)) * 18.0f) * Mathf.Clamp01((0.9f - r) * 20.0f);
                }
                case Cell.Target:
                {
                    float angle = Mathf.Atan2(x, y);
                    float ring = Mathf.Clamp01((0.045f - Mathf.Abs(r - 0.84f)) * Hard);
                    // The angle from the nearest of four axes: a dart on each, its point inward.
                    float q = Mathf.Repeat(angle + Mathf.PI / 4.0f, Mathf.PI / 2.0f) - Mathf.PI / 4.0f;
                    float dart = Mathf.Clamp01((0.30f * (0.70f - r) / 0.26f - Mathf.Abs(q) * r) * Hard) * Mathf.Clamp01((r - 0.44f) * Hard) * Mathf.Clamp01((0.70f - r) * Hard);
                    float spot = Mathf.Clamp01((0.12f - r) * Hard);
                    float dash = Mathf.Clamp01((0.03f - Mathf.Abs(r - 0.30f)) * Hard)
                               * Mathf.Clamp01((Mathf.Abs(Mathf.Repeat(angle * 8.0f / (2.0f * Mathf.PI), 1.0f) - 0.5f) - 0.2f) * 12.0f);
                    return Mathf.Max(Mathf.Max(ring, dart), Mathf.Max(spot, dash));
                }
                case Cell.Line: return Mathf.Clamp01((0.085f * (1.0f - Mathf.Abs(y) / 0.94f) - Mathf.Abs(x)) * 60.0f);
                case Cell.Sparkle:
                {
                    float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
                    return Mathf.Clamp01((0.86f - (ax + ay + 2.6f * Mathf.Sqrt(ax * ay))) * 22.0f);
                }
                case Cell.Puff:
                {
                    float left = Mathf.Sqrt((x + 0.36f) * (x + 0.36f) + (y + 0.10f) * (y + 0.10f)) - 0.36f;
                    float right = Mathf.Sqrt((x - 0.36f) * (x - 0.36f) + (y + 0.10f) * (y + 0.10f)) - 0.36f;
                    float top = Mathf.Sqrt(x * x + (y - 0.20f) * (y - 0.20f)) - 0.46f;
                    return Mathf.Clamp01(-Mathf.Min(Mathf.Min(left, right), top) * Hard) * Mathf.Clamp01((y + 0.42f) * Hard);
                }
            }

            return 0.0f;
        }

        private static float Band(float r, float at, float half) => Mathf.Exp(-(r - at) * (r - at) / (half * half));

        /// <summary>How steep a comic shape's edge is: one over the feather, in cell half widths.</summary>
        private const float Hard = 40.0f;

        // ------------------------------------------------------------------ its own random stream

        /// <summary>0 to 1. A local stream: effects never take a number from gameplay's.</summary>
        public float Rand()
        {
            _seed ^= _seed << 13; _seed ^= _seed >> 17; _seed ^= _seed << 5;
            return (_seed & 0xFFFFFFu) / 16777216.0f;
        }

        public float Rand(float low, float high) => low + (high - low) * Rand();

        /// <summary>A direction: anywhere on the sphere, or pressed toward the vertical by `up` (1 is a hemisphere's worth).</summary>
        public Vector3 RandDirection(float up = 0.0f)
        {
            float z = Rand(-1.0f, 1.0f), a = Rand(0.0f, Mathf.PI * 2.0f), s = Mathf.Sqrt(Mathf.Max(0.0f, 1.0f - z * z));
            var d = new Vector3(s * Mathf.Cos(a), z, s * Mathf.Sin(a));
            if (up > 0.0f) { d.y = Mathf.Abs(d.y) + up; d.Normalize(); }
            return d;
        }

        // ------------------------------------------------------------------ particles

        /// <summary>
        /// One particle. `width` and `length` each run from their first value to their second
        /// over the life (a billboard uses width for both unless a length is given); `fadeIn` is
        /// the share of the life spent coming up, and it fades out over the rest.
        /// </summary>
        public void Emit(Cell cell, Mode mode, Vector3 position, Vector3 velocity, Color colour, float alpha, float life,
                         float width0, float width1, float length0 = -1.0f, float length1 = -1.0f,
                         float gravity = 0.0f, float drag = 0.0f, float yaw = 0.0f, float fadeIn = 0.05f)
        {
            if (life <= 0.0f || !isActiveAndEnabled) return;
            // Full: the oldest quarter gives way rather than the new effect being lost.
            if (_alive >= MaxParticles) { System.Array.Copy(_particles, MaxParticles / 4, _particles, 0, MaxParticles - MaxParticles / 4); _alive -= MaxParticles / 4; }

            ref var p = ref _particles[_alive++];
            p.Cell = cell; p.Mode = mode; p.Position = position; p.Velocity = velocity; p.Colour = colour; p.Alpha = alpha;
            p.Age = 0.0f; p.Life = life; p.Gravity = gravity; p.Drag = drag; p.Yaw = yaw; p.FadeIn = Mathf.Clamp(fadeIn, 0.001f, 0.95f);
            p.Width0 = width0; p.Width1 = width1;
            p.Length0 = length0 < 0.0f ? width0 : length0; p.Length1 = length1 < 0.0f ? (length0 < 0.0f ? width1 : length0) : length1;
        }

        /// <summary>A shock ring lying on the ground plane, growing from one radius to another.</summary>
        public void Ring(Vector3 at, float radius0, float radius1, Color colour, float alpha, float life, Cell cell = Cell.Ring, float rise = 0.0f)
            => Emit(cell, Mode.Flat, at, Vector3.up * rise, colour, alpha, life, radius0 * RingScale(cell), radius1 * RingScale(cell), -1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.04f);

        /// <summary>A ring's quad is this many times its radius across: the shock ring's band is
        /// painted at 0.80 of its cell's half width, the thin and scan rings' at 0.90.</summary>
        public static float RingScale(Cell cell) => cell == Cell.Ring ? 2.5f : 2.0f / 0.9f;

        /// <summary>A soft ball of light that swells and goes.</summary>
        public void Flash(Vector3 at, float size0, float size1, Color colour, float alpha, float life)
            => Emit(Cell.Dot, Mode.Billboard, at, Vector3.zero, colour, alpha, life, size0, size1, -1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.08f);

        /// <summary>
        /// Comic stars thrown from a point: chunky five-pointed stars and fat sparkles that
        /// tumble out, hang and fall, each turned its own way (a flat quad's yaw; a billboard
        /// keeps its own up, so every other one is the sparkle, which reads at any turn).
        /// </summary>
        public void Stars(Vector3 at, Vector3 direction, float spread, int count, float speedLow, float speedHigh, Color colour, Color other,
                          float alpha, float lifeLow, float lifeHigh, float size, float gravity = 5.0f, float drag = 1.6f)
        {
            count = Count(count);
            var forward = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.up;
            var turn = Quaternion.FromToRotation(Vector3.up, forward);
            float cosLimit = Mathf.Cos(Mathf.Clamp(spread, 0.0f, 180.0f) * Mathf.Deg2Rad);
            for (int i = 0; i < count; i++)
            {
                float c = Rand(cosLimit, 1.0f), a = Rand(0.0f, Mathf.PI * 2.0f), s = Mathf.Sqrt(Mathf.Max(0.0f, 1.0f - c * c));
                var d = turn * new Vector3(s * Mathf.Cos(a), c, s * Mathf.Sin(a));
                float big = size * Rand(0.6f, 1.15f);
                Emit(i % 2 == 0 ? Cell.Star5 : Cell.Sparkle, Mode.Billboard, at, d * Rand(speedLow, speedHigh), i % 3 == 0 ? other : colour, alpha,
                     Rand(lifeLow, lifeHigh), big * 0.5f, big, -1.0f, -1.0f, gravity, drag, 0.0f, 0.12f);
            }
        }

        /// <summary>A four-pointed glint.</summary>
        public void Glint(Vector3 at, float size, Color colour, float alpha, float life)
            => Emit(Cell.Star, Mode.Billboard, at, Vector3.zero, colour, alpha, life, size * 0.4f, size, -1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.3f);

        /// <summary>A column of light standing on a point.</summary>
        public void Pillar(Vector3 foot, float height, float width, Color colour, float alpha, float life)
            => Emit(Cell.Streak, Mode.Upright, foot, Vector3.zero, colour, alpha, life, width, width * 0.4f, height * 0.5f, height, 0.0f, 0.0f, 0.0f, 0.1f);

        /// <summary>
        /// Sparks thrown from a point: each a short streak along its own travel. `spread` is the
        /// half angle about `direction` in degrees (180 is every way).
        /// </summary>
        public void Sparks(Vector3 at, Vector3 direction, float spread, int count, float speedLow, float speedHigh, Color colour,
                           float alpha, float lifeLow, float lifeHigh, float size, float gravity, float drag = 0.6f)
        {
            count = Count(count);
            var forward = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.up;
            var turn = Quaternion.FromToRotation(Vector3.up, forward);
            float cosLimit = Mathf.Cos(Mathf.Clamp(spread, 0.0f, 180.0f) * Mathf.Deg2Rad);
            for (int i = 0; i < count; i++)
            {
                float c = Rand(cosLimit, 1.0f), a = Rand(0.0f, Mathf.PI * 2.0f), s = Mathf.Sqrt(Mathf.Max(0.0f, 1.0f - c * c));
                var d = turn * new Vector3(s * Mathf.Cos(a), c, s * Mathf.Sin(a));
                float speed = Rand(speedLow, speedHigh);
                Emit(Cell.Streak, Mode.Stretch, at, d * speed, colour, alpha, Rand(lifeLow, lifeHigh), size, size * 0.3f,
                     size * 2.0f + speed * 0.05f, size * 1.2f, gravity, drag, 0.0f, 0.02f);
            }
        }

        /// <summary>Soft dots thrown from a point (confetti, embers, motes).</summary>
        public void Dots(Vector3 at, Vector3 direction, float spread, int count, float speedLow, float speedHigh, Color colour,
                         float alpha, float lifeLow, float lifeHigh, float size, float gravity, float drag = 0.8f)
        {
            count = Count(count);
            var forward = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.up;
            var turn = Quaternion.FromToRotation(Vector3.up, forward);
            float cosLimit = Mathf.Cos(Mathf.Clamp(spread, 0.0f, 180.0f) * Mathf.Deg2Rad);
            for (int i = 0; i < count; i++)
            {
                float c = Rand(cosLimit, 1.0f), a = Rand(0.0f, Mathf.PI * 2.0f), s = Mathf.Sqrt(Mathf.Max(0.0f, 1.0f - c * c));
                var d = turn * new Vector3(s * Mathf.Cos(a), c, s * Mathf.Sin(a));
                Emit(Cell.Dot, Mode.Billboard, at, d * Rand(speedLow, speedHigh), colour, alpha, Rand(lifeLow, lifeHigh), size, size * 0.5f,
                     -1.0f, -1.0f, gravity, drag, 0.0f, 0.05f);
            }
        }

        /// <summary>A firework: a shell's flash, a sphere of falling sparks, a few glints.</summary>
        public void Firework(Vector3 at, Color colour, float scale = 1.0f)
        {
            Flash(at, 4.0f * scale, 22.0f * scale, colour, 0.9f, 0.45f);
            Sparks(at, Vector3.up, 180.0f, 44, 9.0f * scale, 20.0f * scale, colour, 0.95f, 0.9f, 1.7f, 0.55f * scale, 6.0f, 1.3f);
            Sparks(at, Vector3.up, 180.0f, 14, 4.0f * scale, 9.0f * scale, White, 0.9f, 0.6f, 1.2f, 0.4f * scale, 5.0f, 1.3f);
            for (int i = 0; i < Count(6); i++) Glint(at + RandDirection() * Rand(2.0f, 9.0f) * scale, Rand(1.5f, 3.2f) * scale, White, 0.9f, Rand(0.35f, 0.8f));
        }

        // ------------------------------------------------------------------ immediate quads

        /// <summary>For this frame only: a quad lying on the ground plane (a ring, a disc, a chevron), turned by `yaw` degrees.</summary>
        public void DrawFlat(Cell cell, Vector3 centre, float width, float length, float yaw, Color colour, float alpha)
        {
            if (alpha <= 0.002f || !Reserve(out int v)) return;
            float rad = yaw * Mathf.Deg2Rad;
            var forward = new Vector3(Mathf.Sin(rad), 0.0f, Mathf.Cos(rad)) * (length * 0.5f);
            var right = new Vector3(Mathf.Cos(rad), 0.0f, -Mathf.Sin(rad)) * (width * 0.5f);
            Write(v, centre - right - forward, centre + right - forward, centre + right + forward, centre - right + forward, cell, colour, alpha);
        }

        /// <summary>For this frame only: a quad facing the eye.</summary>
        public void DrawBillboard(Cell cell, Vector3 centre, float size, Color colour, float alpha)
        {
            if (alpha <= 0.002f || !Reserve(out int v)) return;
            Vector3 right = _right * (size * 0.5f), up = _up * (size * 0.5f);
            Write(v, centre - right - up, centre + right - up, centre + right + up, centre - right + up, cell, colour, alpha, RecordedFacing.Billboard);
        }

        /// <summary>For this frame only: a quad facing the eye, turned `degrees` about the line of sight (a target closing, a tumbling star).</summary>
        public void DrawSpun(Cell cell, Vector3 centre, float size, float degrees, Color colour, float alpha)
        {
            if (alpha <= 0.002f || !Reserve(out int v)) return;
            float rad = degrees * Mathf.Deg2Rad, c = Mathf.Cos(rad) * size * 0.5f, s = Mathf.Sin(rad) * size * 0.5f;
            Vector3 right = _right * c + _up * s, up = _up * c - _right * s;
            Write(v, centre - right - up, centre + right - up, centre + right + up, centre - right + up, cell, colour, alpha, RecordedFacing.Billboard);
        }

        /// <summary>For this frame only: a beam of light from one point to another, turned to
        /// face the eye about its own length, `width0` wide at its foot and `width1` at its end.</summary>
        public void DrawBeam(Vector3 from, Vector3 to, float width0, float width1, Color colour, float alpha, Cell cell = Cell.Streak)
        {
            if (alpha <= 0.002f || !Reserve(out int v)) return;
            Vector3 along = to - from;
            Vector3 side = Vector3.Cross(along, _eye - (from + to) * 0.5f);
            if (side.sqrMagnitude < 1e-8f) side = Vector3.Cross(along, Vector3.up);
            if (side.sqrMagnitude < 1e-8f) side = Vector3.right;
            side.Normalize();
            Write(v, from - side * (width0 * 0.5f), from + side * (width0 * 0.5f), to + side * (width1 * 0.5f), to - side * (width1 * 0.5f), cell, colour, alpha, RecordedFacing.Beam);
        }

        /// <summary>For this frame only: a quad standing where it is told to (a screen's flash).</summary>
        public void DrawQuad(Cell cell, Vector3 centre, Vector3 halfRight, Vector3 halfUp, Color colour, float alpha)
        {
            if (alpha <= 0.002f || !Reserve(out int v)) return;
            Write(v, centre - halfRight - halfUp, centre + halfRight - halfUp, centre + halfRight + halfUp, centre - halfRight + halfUp, cell, colour, alpha);
        }

        private bool Reserve(out int vertex)
        {
            vertex = (MaxParticles + _immediate) * 4;
            if (_immediate >= MaxImmediate || !isActiveAndEnabled) return false;
            _immediate++;
            return true;
        }

        private void Write(int v, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Cell cell, Color colour, float alpha, RecordedFacing facing = RecordedFacing.Fixed)
        {
            _recordedFacing[v / 4] = facing;
            _recordedEye[v / 4] = _eye; _recordedRight[v / 4] = _right; _recordedUp[v / 4] = _up;
            _vertices[v] = a; _vertices[v + 1] = b; _vertices[v + 2] = c; _vertices[v + 3] = d;

            alpha = Mathf.Clamp01(alpha * _light);
            var tint = new Color32((byte)(Mathf.Clamp01(colour.r) * 255.0f), (byte)(Mathf.Clamp01(colour.g) * 255.0f),
                                   (byte)(Mathf.Clamp01(colour.b) * 255.0f), (byte)(alpha * 255.0f));
            _colours[v] = tint; _colours[v + 1] = tint; _colours[v + 2] = tint; _colours[v + 3] = tint;

            int column = (int)cell % AtlasColumns, row = AtlasRows - 1 - (int)cell / AtlasColumns;
            const float inset = 1.0f / CellPixels;
            float u0 = (column + inset) / AtlasColumns, u1 = (column + 1 - inset) / AtlasColumns;
            float v0 = (row + inset) / AtlasRows, v1 = (row + 1 - inset) / AtlasRows;
            _uvs[v] = new Vector2(u0, v0); _uvs[v + 1] = new Vector2(u1, v0); _uvs[v + 2] = new Vector2(u1, v1); _uvs[v + 3] = new Vector2(u0, v1);
        }

        // ------------------------------------------------------------------ the frame

        private float _light = 1.0f;

        private void Update()
        {
            _light = Light;
            // The eye for this frame's immediate quads: last frame's pose, which a beam hundreds
            // of metres long cannot tell from this frame's.
            var view = View;
            if (view != null) { var t = view.transform; _eye = t.position; _right = t.right; _up = t.up; }
        }

        private void LateUpdate()
        {
            // The lamps' flares, asked for by everything that ran before this (order 1000), go on the screen.
            ArenaGlare.Present(transform);

            var view = View;
            if (view != null) { var t = view.transform; _eye = t.position; _right = t.right; _up = t.up; }
            _light = Light;

            float dt = Step;
            int kept = 0;
            for (int i = 0; i < _alive; i++)
            {
                ref var p = ref _particles[i];
                p.Age += dt;
                if (p.Age >= p.Life) continue;

                p.Velocity.y -= p.Gravity * dt;
                if (p.Drag > 0.0f) p.Velocity *= Mathf.Max(0.0f, 1.0f - p.Drag * dt);
                p.Position += p.Velocity * dt;
                if (kept != i) _particles[kept] = p;
                kept++;
            }
            _alive = kept;

            for (int i = 0; i < _alive; i++) Build(i);

            int used = _alive, immediate = _immediate;
            _immediate = 0;
            // Dead particle slots between the live ones and the immediate block, and what was
            // drawn last frame and is not now, collapse to nothing.
            for (int q = used; q < Mathf.Min(_drawn, MaxParticles); q++) Collapse(q);
            for (int q = MaxParticles + immediate; q < MaxParticles + _lastImmediate; q++) Collapse(q);
            _drawn = used; _lastImmediate = immediate;

            bool any = used > 0 || immediate > 0;
            if (any || _renderer.enabled)
            {
                // The bounds are the whole map's, set once: never recalculated from the quads.
                const MeshUpdateFlags keep = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices;
                _mesh.SetVertices(_vertices, 0, _vertices.Length, keep);
                _mesh.SetColors(_colours, 0, _colours.Length, keep);
                _mesh.SetUVs(0, _uvs, 0, _uvs.Length, keep);
            }
            if (_renderer.enabled != any) _renderer.enabled = any;
            FrameRendered?.Invoke(Time.time);
        }

        private int _lastImmediate;

        private void Collapse(int quad)
        {
            int v = quad * 4;
            _vertices[v] = _vertices[v + 1] = _vertices[v + 2] = _vertices[v + 3] = Vector3.zero;
        }

        private void Build(int index)
        {
            ref var p = ref _particles[index];
            float t = p.Age / p.Life;
            float fade = t < p.FadeIn ? t / p.FadeIn : 1.0f - (t - p.FadeIn) / (1.0f - p.FadeIn);
            float alpha = p.Alpha * fade * (2.0f - fade);
            float grow = 1.0f - (1.0f - t) * (1.0f - t);
            float width = Mathf.Lerp(p.Width0, p.Width1, grow) * 0.5f, length = Mathf.Lerp(p.Length0, p.Length1, grow) * 0.5f;
            Vector3 at = p.Position, a, b;

            switch (p.Mode)
            {
                case Mode.Flat:
                {
                    float rad = p.Yaw * Mathf.Deg2Rad;
                    a = new Vector3(Mathf.Cos(rad), 0.0f, -Mathf.Sin(rad)) * width;
                    b = new Vector3(Mathf.Sin(rad), 0.0f, Mathf.Cos(rad)) * length;
                    break;
                }
                case Mode.Stretch:
                {
                    Vector3 along = p.Velocity.sqrMagnitude > 1e-6f ? p.Velocity.normalized : Vector3.up;
                    Vector3 side = Vector3.Cross(along, _eye - at);
                    side = side.sqrMagnitude > 1e-8f ? side.normalized : _right;
                    a = side * width; b = along * length;
                    break;
                }
                case Mode.Upright:
                {
                    Vector3 side = Vector3.Cross(Vector3.up, _eye - at);
                    side = side.sqrMagnitude > 1e-8f ? side.normalized : _right;
                    a = side * width; b = Vector3.up * length;
                    at += b;
                    break;
                }
                default:
                    a = _right * width; b = _up * length;
                    break;
            }

            Write(index * 4, at - a - b, at + a - b, at + a + b, at - a + b, p.Cell, p.Colour, alpha,
                p.Mode == Mode.Flat ? RecordedFacing.Fixed : p.Mode == Mode.Stretch ? RecordedFacing.Stretch :
                p.Mode == Mode.Upright ? RecordedFacing.Upright : RecordedFacing.Billboard);
        }
    }
}
