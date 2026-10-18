using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// GLACIAL WALL, REWORKED LOOK (sample g1, 2026-10-18). Yasmin's defending skill: an arc of ice 5 m long on a 3 m
    /// radius, bowed away from her, that stops bodies and slippers, breaks on three slipper hits and lasts 10 s
    /// (`Core.CryoRules`). The old look is five glassy slabs (`CheskaIceVisuals.BuildArc`); this is the same arc drawn
    /// as a packed wall of the APPROVED ice crystals (`IceCrystalKit`, `Shaders/IceCrystal`), used as they are.
    ///
    /// References read for it (looked at only, nothing downloaded or traced): League of Legends, Anivia's Crystallize (a
    /// long wall of packed crystals, uneven skyline, shards leaning out both ways, small ones at the foot); Overwatch,
    /// Mei's Ice Wall (chunky stylised pillars, light tops, dark creases). Both are stills; the motion is our design.
    ///
    /// How it is built, and why it is cheap:
    ///   * Every crystal is TYPED in <see cref="Crystals"/>: its kind, where it stands on the arc, its size in metres,
    ///     its lean, its turn and its birth. No loop stamps one shape round the arc.
    ///   * The crystals of one kind are BAKED INTO ONE MESH, once per session, in the wall's own space. Each vertex
    ///     carries its crystal's foot, birth, height and death, so the crystal shader (opt-in `_Grow`) raises it out of
    ///     the road and sinks it again on its own. A cast makes renderers that share those meshes and one material a
    ///     kind; a frame sets one property block on each. Four crystal draws and one frost draw a wall.
    ///   * The frost under it (`Shaders/CheskaWallFrost`) runs out from the aimed point to both ends first; the
    ///     crystals rise behind its leading edge.
    ///
    /// ⚠️ LOOK ONLY. It carries no collider and reads no rule. The arc's colliders stay the old slabs'. Nothing in a
    /// match builds this until <see cref="Enabled"/> is turned on (default off).
    /// </summary>
    public static class CheskaGlacialWallLook
    {
        /// <summary>The switch for matches. Off: the old slabs, exactly as before.</summary>
        public static bool Enabled = false;

        /// <summary>How long the frost takes to run to both ends; the crystals rise behind it.</summary>
        public const float RunSeconds = 0.30f;
        /// <summary>How long one crystal takes to come up out of the road.</summary>
        public const float RiseSeconds = 0.24f;
        /// <summary>How long before its end the wall starts to sink, and how long one crystal takes to sink.</summary>
        public const float SinkLead = 0.62f, SinkSeconds = 0.40f;

        private const string Kinds0 = IceCrystalKit.Shard, Kinds1 = IceCrystalKit.Spike, Kinds2 = IceCrystalKit.Stub, Kinds3 = IceCrystalKit.Cluster;
        private static readonly string[] KindNames = { Kinds0, Kinds1, Kinds2, Kinds3 };

        /// <summary>One crystal of the wall, typed.</summary>
        private readonly struct Crystal
        {
            public readonly int Kind;          // 0 shard, 1 spike, 2 stub, 3 cluster
            public readonly float Along;       // metres along the arc from the aimed point (+ to her right)
            public readonly float Out;         // metres off the arc line (+ away from her)
            public readonly float Wide, Tall, Deep; // its size in metres: along the arc, upright, through the wall
            public readonly float LeanOut;     // degrees, + leans away from her
            public readonly float LeanAlong;   // degrees, + leans toward her right
            public readonly float Turn;        // degrees about its own length
            public readonly float Late;        // seconds after the frost reaches it
            public Crystal(int kind, float along, float @out, float wide, float tall, float deep, float leanOut, float leanAlong, float turn, float late)
            { Kind = kind; Along = along; Out = @out; Wide = wide; Tall = tall; Deep = deep; LeanOut = leanOut; LeanAlong = leanAlong; Turn = turn; Late = late; }
        }

        // The wall, typed. The body is seven shards, tallest in the middle; six spikes between them make the skyline;
        // eight stubs sit at the foot on both faces; one small cluster closes each end. Kept inside about 0.35 m of the
        // arc line, against the old slabs' 0.28 m half thickness, so what is drawn is close to what blocks.
        private static readonly Crystal[] Crystals =
        {
            //          kind  along   out    wide  tall  deep  leanOut leanAlong turn  late
            new Crystal(0, -2.12f,  0.00f, 0.86f, 1.22f, 0.52f,   5f,  -11f,   30f, 0.00f),
            new Crystal(0, -1.43f,  0.04f, 0.96f, 1.46f, 0.58f,   3f,   -6f,  160f, 0.02f),
            new Crystal(0, -0.70f, -0.03f, 1.00f, 1.70f, 0.60f,  -4f,   -3f,   75f, 0.00f),
            new Crystal(0,  0.04f,  0.02f, 1.06f, 1.88f, 0.62f,   2f,    1f,  250f, 0.00f),
            new Crystal(0,  0.78f, -0.03f, 1.00f, 1.66f, 0.58f,   4f,    5f,  300f, 0.03f),
            new Crystal(0,  1.48f,  0.03f, 0.94f, 1.44f, 0.58f,  -3f,    9f,   20f, 0.01f),
            new Crystal(0,  2.18f,  0.00f, 0.84f, 1.18f, 0.52f,   5f,   13f,  200f, 0.02f),
            new Crystal(1, -1.80f, -0.08f, 0.40f, 1.42f, 0.36f,  -9f,  -16f,   40f, 0.05f),
            new Crystal(1, -1.06f,  0.10f, 0.44f, 1.86f, 0.38f,  11f,   -7f,  130f, 0.06f),
            new Crystal(1, -0.32f, -0.07f, 0.46f, 2.14f, 0.40f,  -7f,   -3f,  210f, 0.05f),
            new Crystal(1,  0.43f,  0.08f, 0.46f, 2.04f, 0.40f,  10f,    5f,  330f, 0.07f),
            new Crystal(1,  1.14f, -0.09f, 0.42f, 1.74f, 0.38f, -10f,    9f,   90f, 0.05f),
            new Crystal(1,  1.86f,  0.07f, 0.38f, 1.38f, 0.34f,   9f,   18f,  260f, 0.06f),
            new Crystal(2, -2.36f,  0.26f, 0.46f, 0.40f, 0.36f,  18f,  -22f,   10f, 0.08f),
            new Crystal(2, -0.98f,  0.32f, 0.52f, 0.46f, 0.38f,  22f,    0f,  140f, 0.09f),
            new Crystal(2,  0.38f,  0.30f, 0.46f, 0.38f, 0.34f,  20f,    6f,  280f, 0.08f),
            new Crystal(2,  1.72f,  0.28f, 0.50f, 0.44f, 0.36f,  16f,   14f,   60f, 0.10f),
            new Crystal(2, -1.62f, -0.30f, 0.46f, 0.40f, 0.34f, -20f,   -8f,  200f, 0.09f),
            new Crystal(2, -0.12f, -0.32f, 0.52f, 0.46f, 0.38f, -24f,    0f,  320f, 0.08f),
            new Crystal(2,  1.20f, -0.29f, 0.48f, 0.42f, 0.36f, -18f,   10f,   95f, 0.10f),
            new Crystal(2,  2.40f, -0.24f, 0.42f, 0.36f, 0.32f, -16f,   22f,  250f, 0.09f),
            new Crystal(3, -2.50f,  0.00f, 0.92f, 0.92f, 0.56f,   0f,  -12f,    0f, 0.04f),
            new Crystal(3,  2.52f,  0.00f, 0.88f, 0.86f, 0.54f,   0f,   12f,  120f, 0.04f),
        };

        private static readonly int AgeId = Shader.PropertyToID("_Age"), LitId = Shader.PropertyToID("_Lit"),
            DieAtId = Shader.PropertyToID("_DieAt"), FrontId = Shader.PropertyToID("_Front"), ThawId = Shader.PropertyToID("_Thaw");

        // Made once and shared by every wall: a mesh and a material a kind, the frost band and its material.
        private static readonly Mesh[] _meshes = new Mesh[4];
        private static readonly Material[] _looks = new Material[4];
        private static Mesh _frostMesh;
        private static Material _frostLook;
        private static float _bakedLength = -1f, _bakedRadius = -1f;
        private static MaterialPropertyBlock _block;

        /// <summary>
        /// How stylised the wall is (owner, of the crystal samples: "same for cheska's ice.. doesnt fit the style of
        /// the game rn"). Crystal: the approved crystals as they are (g1, g2). Painted: the same crystals, painted in
        /// three flat values. Chunky: our own few broad six-sided pieces with blunt tops, painted. Blocky: fewer,
        /// cube-like chunks with bevelled tops, painted, closest to the cast's blocky build.
        /// </summary>
        public enum Style
        {
            Crystal = 0, Painted = 1, Chunky = 2, Blocky = 3,
            // In between (g4, owner of g3: "Painted, Chunky and Blocky all look the same to me. try to give me an
            // in-between texture design. and something slightly more cyan"). `Shaders/CheskaWallBetween`: four crisp
            // value bands, a frosted rim, painted inner streaks and cracks, a little see-through depth, a climbing
            // shimmer and crawling sparkles, in a cyan nearer the game's ice. A, B and C differ in band contrast and
            // inner detail (A softest and plainest, C boldest and busiest). The first three are on the approved
            // crystals' silhouette, the last three on Chunky's.
            BetweenA = 4, BetweenB = 5, BetweenC = 6, ChunkyBetweenA = 7, ChunkyBetweenB = 8, ChunkyBetweenC = 9,
            // With an inside (g6, owner of g5: "glacial wall designs are still lacking the depth detail the realistic
            // one had"). B's painted bands and cyan, plus the crystal's depth language made stylised: a dark core where
            // the ice is thick (the crystal's own baked thickness on the crystal shapes), bright fracture veins at three
            // depths that slide under the surface as the view moves, a cloudy inner volume and a frosted shell where it
            // is thin. DepthPlus goes further toward Crystal.
            DepthB = 10, DepthPlus = 11, ChunkyDepthB = 12, ChunkyDepthPlus = 13,
        }

        /// <summary>0, 1 or 2 for an in-between style (its variant), or -1.</summary>
        private static int BetweenOf(Style style) =>
            style >= Style.DepthB ? 3 + ((int)style - (int)Style.DepthB) % 2 :
            style >= Style.BetweenA ? ((int)style - (int)Style.BetweenA) % 3 : -1;
        /// <summary>The mesh set a style is drawn on: Crystal (the crystals), Chunky or Blocky.</summary>
        private static Style ShapeOf(Style style) =>
            style == Style.Painted || (style >= Style.BetweenA && style <= Style.BetweenC) || style == Style.DepthB || style == Style.DepthPlus ? Style.Crystal :
            style >= Style.ChunkyBetweenA ? Style.Chunky : style;

        // The three in-between variants: band contrast, flat facets, rim, streaks, cracks, depth, sparkle, shimmer.
        private static readonly float[,] BetweenTable =
        {
            //   contrast facet  rim   streaks cracks depth sparkle shimmer | core  shell inner innerDepth cloud glint
            {    0.62f,  0.55f, 0.80f, 0.35f,  0.00f, 0.25f, 0.55f,  0.16f,   0f,   0f,   0f,   0.30f,    0f,  1.4f },  // A: soft bands, plain inside
            {    0.88f,  0.80f, 0.90f, 0.55f,  0.45f, 0.45f, 0.80f,  0.22f,   0f,   0f,   0f,   0.30f,    0f,  1.4f },  // B: clear bands, streaks and a few cracks
            {    1.08f,  1.00f, 1.00f, 0.80f,  0.85f, 0.70f, 1.00f,  0.24f,   0f,   0f,   0f,   0.30f,    0f,  1.4f },  // C: bold bands, busy inside, deepest
            {    0.88f,  0.80f, 0.90f, 0.30f,  0.00f, 0.20f, 0.80f,  0.20f, 0.60f, 0.28f, 0.85f, 0.28f,  0.40f, 1.4f },  // DepthB: B with an inside
            {    0.95f,  0.85f, 0.95f, 0.20f,  0.00f, 0.30f, 0.80f,  0.18f, 0.75f, 0.70f, 1.15f, 0.42f,  0.60f, 1.4f },  // DepthPlus: deeper, closer to Crystal
        };
        private static readonly string[] BetweenKeys = { "_Contrast", "_Facet", "_Rim", "_Streaks", "_Cracks", "_Depth", "_Sparkle", "_Shimmer",
                                                         "_Core", "_Shell", "_Inner", "_InnerDepth", "_Cloud", "_Glint" };
        private static readonly Material[] _between = new Material[5];
        // The depth variants on the crystal shapes read each kind's own baked thickness, so they need a copy a kind.
        private static readonly Material[,] _betweenKind = new Material[5, 4];
        private static Material BetweenLook(int variant, int kind, bool crystalShape)
        {
            var baseLook = _between[variant];
            if (baseLook == null || variant < 3 || !crystalShape) return baseLook;
            if (_betweenKind[variant, kind] != null) return _betweenKind[variant, kind];
            var mask = _looks[kind] != null && _looks[kind].HasProperty("_MaskTex") ? _looks[kind].GetTexture("_MaskTex") : null;
            if (mask == null) return baseLook;
            var m = new Material(baseLook) { name = baseLook.name + "_" + KindNames[kind] };
            m.SetTexture("_MaskTex", mask);
            m.SetFloat("_HasMask", 1f);
            return _betweenKind[variant, kind] = m;
        }

        private static Material _paint, _frostPainted;
        private static readonly Mesh[] _blocks = new Mesh[4];

        /// <summary>What the last bake found, for a film's report.</summary>
        public static string BakeReport { get; private set; } = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Forget()
        {
            for (int i = 0; i < 4; i++) { _meshes[i] = null; _looks[i] = null; }
            _frostMesh = null; _frostLook = null; _bakedLength = -1f; _block = null;
            _paint = null; _frostPainted = null; for (int i = 0; i < 4; i++) _blocks[i] = null;
            for (int i = 0; i < 5; i++) { _between[i] = null; for (int k = 0; k < 4; k++) _betweenKind[i, k] = null; }
        }

        /// <summary>One wall's renderers, posed by <see cref="Pose"/>.</summary>
        public sealed class Wall
        {
            public readonly GameObject Root;
            public readonly Renderer[] Parts;
            public readonly Renderer Frost;
            internal Wall(GameObject root, Renderer[] parts, Renderer frost) { Root = root; Parts = parts; Frost = frost; }
        }

        /// <summary>A wall under <paramref name="parent"/> (its +Z is her aim, its origin the aimed point on the road).</summary>
        // KEEP: owner 2026-10-18, "depth b looks good" (g6 DepthB on the crystal shapes). Approved look; do not change.
        public static Wall Build(Transform parent, float arcLength, float arcRadius, Style style = Style.DepthB)
        {
            Bake(arcLength, arcRadius);
            EnsureStyle(style, arcLength, arcRadius);
            var root = new GameObject("GlacialWallLook-" + style);
            root.transform.SetParent(parent, false);
            var parts = new List<Renderer>();
            Style shape = ShapeOf(style);
            int between = BetweenOf(style);
            for (int k = 0; k < 4; k++)
            {
                Mesh mesh; Material look;
                if (style == Style.Crystal) { mesh = _meshes[k]; look = _looks[k]; }
                else if (shape == Style.Crystal) { mesh = _meshes[k]; look = between >= 0 ? BetweenLook(between, k, true) : _paint; }
                else { mesh = k == 0 ? _blocks[(int)shape] : null; look = between >= 0 ? _between[between] : _paint; }
                if (mesh == null || look == null) continue;
                var go = new GameObject("Ice-" + (shape == Style.Crystal ? KindNames[k] : style.ToString()));
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = look;
                r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
                VfxRenderTag.Attach(go);
                parts.Add(r);
            }
            Renderer frost = null;
            if (_frostMesh != null && _frostLook != null)
            {
                var go = new GameObject("Frost");
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(0f, 0.012f, 0f);
                go.AddComponent<MeshFilter>().sharedMesh = _frostMesh;
                frost = go.AddComponent<MeshRenderer>();
                frost.sharedMaterial = style == Style.Crystal || _frostPainted == null ? _frostLook : _frostPainted;
                frost.shadowCastingMode = ShadowCastingMode.Off; frost.receiveShadows = false;
                VfxRenderTag.Attach(go);
            }
            var wall = new Wall(root, parts.ToArray(), frost);
            Pose(wall, 0f, 10f, 0f);
            return wall;
        }

        /// <summary>
        /// Poses a wall <paramref name="age"/> seconds after the cast, for a wall that lives <paramref name="life"/>
        /// seconds. <paramref name="flash"/> (0 to 1) is a slipper's hit landing: the wall lights from inside.
        /// </summary>
        public static void Pose(Wall wall, float age, float life, float flash)
        {
            if (wall == null) return;
            _block ??= new MaterialPropertyBlock();
            float dieAt = life - SinkLead;
            foreach (var part in wall.Parts)
            {
                if (part == null) continue;
                part.GetPropertyBlock(_block);
                _block.SetFloat(AgeId, age);
                _block.SetFloat(DieAtId, dieAt);
                _block.SetFloat(LitId, 1f + 1.4f * flash);
                part.SetPropertyBlock(_block);
            }
            if (wall.Frost != null)
            {
                wall.Frost.GetPropertyBlock(_block);
                _block.SetFloat(AgeId, age);
                _block.SetFloat(FrontId, Mathf.Clamp01(age / RunSeconds) * 1.06f);
                _block.SetFloat(ThawId, Mathf.Clamp01((age - (life - SinkLead + 0.10f)) / (SinkLead - 0.05f)) * 1.08f);
                wall.Frost.SetPropertyBlock(_block);
            }
        }

        // ------------------------------------------------------------------------------------------- painted styles

        /// <summary>One piece of a painted wall, typed: a few broad faces round a blunt top, never a spike.</summary>
        private readonly struct Block
        {
            public readonly float Along, Out, Wide, Deep, Tall; // metres; Wide along the arc, Deep through the wall
            public readonly int Sides;                           // faces round it
            public readonly float Box;                           // 1 round, under 1 squarer (a cube-like chunk)
            public readonly float Cap, Shoulder, Apex, ApexOff;  // share of its height that is top; ring sizes; tip offset
            public readonly float LeanOut, LeanAlong, Turn, Late;
            public Block(float along, float @out, float wide, float deep, float tall, int sides, float box, float cap, float shoulder, float apex, float apexOff,
                         float leanOut, float leanAlong, float turn, float late)
            {
                Along = along; Out = @out; Wide = wide; Deep = deep; Tall = tall; Sides = sides; Box = box; Cap = cap; Shoulder = shoulder;
                Apex = apex; ApexOff = apexOff; LeanOut = leanOut; LeanAlong = leanAlong; Turn = turn; Late = late;
            }
        }

        // CHUNKY: seven broad six-sided pieces, tallest in the middle, four narrower five-sided ones between them for
        // the skyline, seven small ones at the foot. Every top is cut blunt (a flat apex face), so the skyline reads as
        // friendly chunks, not knives. Each typed.
        private static readonly Block[] Chunky =
        {
            //        along   out    wide  deep  tall  sides box  cap   shld  apex  apexOff lnOut lnAlong turn late
            new Block(-2.15f, 0.00f, 0.84f, 0.52f, 1.12f, 6, 1.0f, 0.34f, 0.90f, 0.34f,  0.10f,  4f, -10f,  10f, 0.00f),
            new Block(-1.45f, 0.03f, 0.96f, 0.58f, 1.42f, 6, 1.0f, 0.32f, 0.92f, 0.30f, -0.08f,  2f,  -6f,  35f, 0.02f),
            new Block(-0.75f,-0.02f, 1.00f, 0.60f, 1.66f, 6, 1.0f, 0.30f, 0.90f, 0.32f,  0.12f, -3f,  -3f,   5f, 0.00f),
            new Block( 0.00f, 0.02f, 1.10f, 0.62f, 1.92f, 6, 1.0f, 0.30f, 0.92f, 0.30f, -0.06f,  2f,   0f,  20f, 0.00f),
            new Block( 0.75f,-0.03f, 1.00f, 0.60f, 1.62f, 6, 1.0f, 0.31f, 0.90f, 0.34f,  0.08f,  3f,   4f,  40f, 0.03f),
            new Block( 1.45f, 0.02f, 0.94f, 0.58f, 1.38f, 6, 1.0f, 0.33f, 0.92f, 0.30f, -0.10f, -2f,   7f,  15f, 0.01f),
            new Block( 2.15f, 0.00f, 0.84f, 0.52f, 1.08f, 6, 1.0f, 0.35f, 0.90f, 0.36f,  0.06f,  4f,  11f,  30f, 0.02f),
            new Block(-1.10f, 0.10f, 0.58f, 0.40f, 1.82f, 5, 1.0f, 0.38f, 0.88f, 0.24f,  0.10f,  8f,  -6f,   0f, 0.05f),
            new Block(-0.38f,-0.10f, 0.62f, 0.42f, 2.12f, 5, 1.0f, 0.36f, 0.88f, 0.22f, -0.08f, -6f,  -2f,  20f, 0.06f),
            new Block( 0.38f, 0.09f, 0.62f, 0.42f, 2.02f, 5, 1.0f, 0.37f, 0.88f, 0.24f,  0.08f,  7f,   3f,  10f, 0.05f),
            new Block( 1.10f,-0.09f, 0.58f, 0.40f, 1.72f, 5, 1.0f, 0.38f, 0.88f, 0.24f, -0.10f, -7f,   6f,  30f, 0.06f),
            new Block(-2.45f, 0.22f, 0.46f, 0.34f, 0.52f, 5, 1.0f, 0.45f, 0.90f, 0.40f,  0.05f, 15f, -15f,   0f, 0.08f),
            new Block(-0.95f, 0.30f, 0.44f, 0.32f, 0.44f, 5, 1.0f, 0.45f, 0.90f, 0.42f, -0.05f, 18f,   0f,  20f, 0.09f),
            new Block( 0.55f, 0.30f, 0.48f, 0.34f, 0.50f, 5, 1.0f, 0.44f, 0.90f, 0.40f,  0.06f, 16f,   5f,  10f, 0.08f),
            new Block( 1.85f, 0.26f, 0.44f, 0.32f, 0.46f, 5, 1.0f, 0.45f, 0.90f, 0.42f, -0.04f, 14f,  12f,   0f, 0.10f),
            new Block(-1.70f,-0.28f, 0.44f, 0.32f, 0.46f, 5, 1.0f, 0.45f, 0.90f, 0.40f,  0.05f,-16f,  -6f,  15f, 0.09f),
            new Block( 0.15f,-0.30f, 0.48f, 0.34f, 0.50f, 5, 1.0f, 0.44f, 0.90f, 0.42f, -0.06f,-18f,   0f,   5f, 0.08f),
            new Block( 2.45f,-0.22f, 0.44f, 0.32f, 0.44f, 5, 1.0f, 0.45f, 0.90f, 0.40f,  0.04f,-14f,  15f,  25f, 0.10f),
        };

        // BLOCKY: five big cube-like chunks (eight faces round a near-square, a short bevelled top), two smaller ones
        // standing between them, and six little cubes at the foot. The cast's own build: big plain planes.
        private static readonly Block[] Blocky =
        {
            //        along   out    wide  deep  tall  sides box   cap   shld  apex  apexOff lnOut lnAlong turn late
            new Block(-1.95f, 0.00f, 1.02f, 0.58f, 1.12f, 8, 0.35f, 0.16f, 0.96f, 0.74f, 0.00f,   3f,  -6f,   0f, 0.00f),
            new Block(-0.98f, 0.02f, 1.04f, 0.60f, 1.52f, 8, 0.35f, 0.14f, 0.96f, 0.76f, 0.00f,  -2f,  -3f,   4f, 0.01f),
            new Block( 0.00f, 0.00f, 1.06f, 0.62f, 1.86f, 8, 0.35f, 0.13f, 0.96f, 0.76f, 0.00f,   2f,   0f,  -3f, 0.00f),
            new Block( 0.98f,-0.02f, 1.04f, 0.60f, 1.48f, 8, 0.35f, 0.14f, 0.96f, 0.76f, 0.00f,  -2f,   3f,   3f, 0.01f),
            new Block( 1.95f, 0.00f, 1.00f, 0.58f, 1.08f, 8, 0.35f, 0.16f, 0.96f, 0.74f, 0.00f,   3f,   6f,  -4f, 0.00f),
            new Block(-1.48f, 0.06f, 0.56f, 0.42f, 1.74f, 8, 0.40f, 0.18f, 0.94f, 0.70f, 0.00f,   5f,  -8f,  12f, 0.06f),
            new Block( 0.50f,-0.06f, 0.56f, 0.42f, 2.12f, 8, 0.40f, 0.17f, 0.94f, 0.70f, 0.00f,  -5f,   5f, -10f, 0.06f),
            new Block(-2.48f, 0.20f, 0.46f, 0.40f, 0.48f, 8, 0.38f, 0.22f, 0.94f, 0.70f, 0.00f,  12f, -14f,  18f, 0.09f),
            new Block(-0.50f, 0.32f, 0.48f, 0.40f, 0.44f, 8, 0.38f, 0.22f, 0.94f, 0.70f, 0.00f,  14f,   0f, -12f, 0.08f),
            new Block( 1.45f, 0.30f, 0.46f, 0.40f, 0.46f, 8, 0.38f, 0.22f, 0.94f, 0.70f, 0.00f,  12f,   8f,  22f, 0.09f),
            new Block(-1.40f,-0.30f, 0.46f, 0.40f, 0.42f, 8, 0.38f, 0.22f, 0.94f, 0.70f, 0.00f, -14f,  -4f,   8f, 0.08f),
            new Block( 0.45f,-0.32f, 0.48f, 0.40f, 0.46f, 8, 0.38f, 0.22f, 0.94f, 0.70f, 0.00f, -12f,   4f, -16f, 0.09f),
            new Block( 2.46f,-0.20f, 0.44f, 0.38f, 0.42f, 8, 0.38f, 0.22f, 0.94f, 0.70f, 0.00f, -12f,  14f,  10f, 0.08f),
        };

        private static void EnsureStyle(Style style, float arcLength, float radius)
        {
            if (style == Style.Crystal) return;
            if (_paint == null)
            {
                var shader = Resources.Load<Shader>("Shaders/CheskaWallIce");
                if (shader != null && shader.isSupported)
                {
                    _paint = new Material(shader) { name = "GlacialWallPainted" };
                    _paint.SetVector("_KeyDir", IceCrystalKit.TowardKey);
                    _paint.SetFloat("_GrowTime", RiseSeconds);
                    _paint.SetFloat("_DieTime", SinkSeconds);
                }
                else BakeReport += "painted shader missing; ";
            }
            if (_frostPainted == null && _frostLook != null)
            {
                _frostPainted = new Material(_frostLook) { name = "GlacialWallFrostPainted" };
                _frostPainted.SetFloat("_Painted", 1f);
            }
            int between = BetweenOf(style);
            if (between >= 0 && _between[between] == null)
            {
                var shader = Resources.Load<Shader>("Shaders/CheskaWallBetween");
                if (shader != null && shader.isSupported)
                {
                    var m = new Material(shader) { name = "GlacialWallBetween" + (between < 3 ? ((char)('A' + between)).ToString() : between == 3 ? "DepthB" : "DepthPlus") };
                    m.SetVector("_KeyDir", IceCrystalKit.TowardKey);
                    m.SetFloat("_GrowTime", RiseSeconds);
                    m.SetFloat("_DieTime", SinkSeconds);
                    for (int q = 0; q < BetweenKeys.Length; q++) m.SetFloat(BetweenKeys[q], BetweenTable[between, q]);
                    _between[between] = m;
                }
                else BakeReport += "in-between shader missing; ";
            }
            style = ShapeOf(style);
            if (style == Style.Crystal) return;
            if (_blocks[(int)style] != null) return;
            var table = style == Style.Chunky ? Chunky : Blocky;
            float stretch = arcLength / 5f;
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var feet = new List<Vector4>(); var lives = new List<Vector4>(); var tris = new List<int>();
            for (int c = 0; c < table.Length; c++)
            {
                var b = table[c];
                float along = b.Along * stretch;
                Vector3 foot = ArcPoint(along, b.Out, radius);
                float heading = along / radius * Mathf.Rad2Deg;
                Matrix4x4 place = Matrix4x4.TRS(foot, Quaternion.Euler(0f, heading, 0f) * Quaternion.Euler(b.LeanOut, 0f, -b.LeanAlong) * Quaternion.Euler(0f, b.Turn, 0f), Vector3.one);
                float birth = RunSeconds * Mathf.Abs(along) / (arcLength * 0.5f) + b.Late;
                float death = (1f - Mathf.Abs(along) / (arcLength * 0.5f)) * 0.16f + b.Late * 0.6f;
                var life = new Vector4(birth, b.Tall, death, c);
                var footV = new Vector4(foot.x, foot.y, foot.z, 0f);
                // Three rings: a buried foot, a shoulder, a blunt apex; flat faces between them and a flat top.
                int n = b.Sides;
                var ring = new Vector3[3, n];
                float shoulderY = b.Tall * (1f - b.Cap);
                for (int k = 0; k < n; k++)
                {
                    float a = (k + 0.5f) / n * Mathf.PI * 2f;
                    float cx = Mathf.Cos(a), sz = Mathf.Sin(a);
                    float x = Mathf.Sign(cx) * Mathf.Pow(Mathf.Abs(cx), b.Box), z = Mathf.Sign(sz) * Mathf.Pow(Mathf.Abs(sz), b.Box);
                    float hx = b.Wide * 0.5f, hz = b.Deep * 0.5f;
                    ring[0, k] = new Vector3(x * hx, -0.18f, z * hz);
                    ring[1, k] = new Vector3(x * hx * b.Shoulder, shoulderY, z * hz * b.Shoulder);
                    ring[2, k] = new Vector3(x * hx * b.Apex + b.ApexOff * hx, b.Tall, z * hz * b.Apex);
                }
                void Face(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, bool quad)
                {
                    Vector3 w0 = place.MultiplyPoint3x4(p0), w1 = place.MultiplyPoint3x4(p1), w2 = place.MultiplyPoint3x4(p2);
                    Vector3 nrm = Vector3.Cross(w1 - w0, w2 - w0).normalized;
                    int s0 = verts.Count;
                    verts.Add(w0); verts.Add(w1); verts.Add(w2);
                    if (quad) verts.Add(place.MultiplyPoint3x4(p3));
                    int count = quad ? 4 : 3;
                    for (int q = 0; q < count; q++) { norms.Add(nrm); feet.Add(footV); lives.Add(life); }
                    tris.Add(s0); tris.Add(s0 + 1); tris.Add(s0 + 2);
                    if (quad) { tris.Add(s0); tris.Add(s0 + 2); tris.Add(s0 + 3); }
                }
                for (int k = 0; k < n; k++)
                {
                    int j = (k + 1) % n;
                    Face(ring[0, k], ring[1, k], ring[1, j], ring[0, j], true);
                    Face(ring[1, k], ring[2, k], ring[2, j], ring[1, j], true);
                }
                for (int k = 1; k < n - 1; k++) Face(ring[2, 0], ring[2, k + 1], ring[2, k], Vector3.zero, false);
            }
            var mesh = new Mesh { name = "GlacialWall_" + style };
            mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetUVs(1, feet); mesh.SetUVs(2, lives); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            var bb = mesh.bounds; bb.Encapsulate(bb.min - Vector3.up * 2.3f); mesh.bounds = bb;
            mesh.UploadMeshData(true);
            _blocks[(int)style] = mesh;
            BakeReport += style + ": " + table.Length + " pieces, " + tris.Count / 3 + " triangles; ";
        }

        // ------------------------------------------------------------------------------------------------- the bake

        private static Vector3 ArcPoint(float along, float @out, float radius)
        {
            // As `CheskaIceVisuals.BuildArc`: the arc's centre is `radius` behind the aimed point, so it bows away.
            float a = along / radius;
            float r = radius + @out;
            return new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r - radius);
        }

        private static void Bake(float arcLength, float radius)
        {
            bool have = _frostMesh != null;
            for (int k = 0; k < 4; k++) have &= _meshes[k] != null && _looks[k] != null;
            if (have && Mathf.Approximately(arcLength, _bakedLength) && Mathf.Approximately(radius, _bakedRadius)) return;
            _bakedLength = arcLength; _bakedRadius = radius;
            float stretch = arcLength / 5f;
            var report = new System.Text.StringBuilder();
            var flowTex = Resources.Load<Texture2D>("Textures/UltimateFx/erode_cloud");

            for (int k = 0; k < 4; k++)
            {
                // The approved crystal, made the kit's way, measured and taken apart into the parts it is drawn with.
                var probe = new GameObject("~bake");
                var holder = IceCrystalKit.Spawn(KindNames[k], probe.transform, out float tall, out float wide);
                if (holder == null) { Kill(probe); report.Append(KindNames[k]).Append(" missing; "); continue; }
                var filters = holder.GetComponentsInChildren<MeshFilter>(true);
                var renderer = holder.GetComponentInChildren<MeshRenderer>(true);
                var look = renderer != null ? new Material(renderer.sharedMaterial) { name = "GlacialWall_" + KindNames[k] } : null;
                // Its own bounds in the holder's space: along x, up y, through z.
                var bounds = new Bounds(); bool any = false;
                foreach (var f in filters)
                {
                    Matrix4x4 m = holder.transform.worldToLocalMatrix * f.transform.localToWorldMatrix;
                    foreach (var v in f.sharedMesh.vertices)
                    { var p = m.MultiplyPoint3x4(v); if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; } else bounds.Encapsulate(p); }
                }
                var verts = new List<Vector3>(); var norms = new List<Vector3>(); var tans = new List<Vector4>();
                var uvs = new List<Vector2>(); var feet = new List<Vector4>(); var lives = new List<Vector4>(); var tris = new List<int>();
                bool readable = true;
                int count = 0;
                for (int c = 0; c < Crystals.Length; c++)
                {
                    var cr = Crystals[c];
                    if (cr.Kind != k) continue;
                    count++;
                    float along = cr.Along * stretch;
                    Vector3 foot = ArcPoint(along, cr.Out, radius);
                    float heading = along / radius * Mathf.Rad2Deg;
                    // Size first (in its own axes, turned only about its length), then the lean, then onto the arc.
                    Vector3 size = new Vector3(cr.Wide / Mathf.Max(0.01f, bounds.size.x), cr.Tall / Mathf.Max(0.01f, bounds.size.y), cr.Deep / Mathf.Max(0.01f, bounds.size.z));
                    Matrix4x4 place = Matrix4x4.TRS(foot, Quaternion.Euler(0f, heading, 0f) * Quaternion.Euler(cr.LeanOut, 0f, -cr.LeanAlong), Vector3.one)
                                    * Matrix4x4.Scale(size) * Matrix4x4.Rotate(Quaternion.Euler(0f, cr.Turn, 0f))
                                    * Matrix4x4.Translate(new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z));
                    Matrix4x4 bend = place.inverse.transpose;
                    // Its birth: when the frost's edge reaches it, plus its own lateness.
                    float birth = RunSeconds * Mathf.Abs(along) / (arcLength * 0.5f) + cr.Late;
                    float death = (1f - Mathf.Abs(along) / (arcLength * 0.5f)) * 0.16f + cr.Late * 0.6f;
                    foreach (var f in filters)
                    {
                        var mesh = f.sharedMesh;
                        if (!mesh.isReadable) readable = false;
                        Matrix4x4 m = place * holder.transform.worldToLocalMatrix * f.transform.localToWorldMatrix;
                        Matrix4x4 mn = (holder.transform.worldToLocalMatrix * f.transform.localToWorldMatrix).inverse.transpose;
                        int start = verts.Count;
                        var mv = mesh.vertices; var mnrm = mesh.normals; var mt = mesh.tangents; var mu = mesh.uv;
                        for (int i = 0; i < mv.Length; i++)
                        {
                            verts.Add(m.MultiplyPoint3x4(mv[i]));
                            Vector3 n = i < mnrm.Length ? mnrm[i] : Vector3.up;
                            norms.Add(bend.MultiplyVector(mn.MultiplyVector(n)).normalized);
                            if (i < mt.Length) { Vector3 t = m.MultiplyVector(new Vector3(mt[i].x, mt[i].y, mt[i].z)).normalized; tans.Add(new Vector4(t.x, t.y, t.z, mt[i].w)); }
                            uvs.Add(i < mu.Length ? mu[i] : Vector2.zero);
                            feet.Add(new Vector4(foot.x, foot.y, foot.z, 0f));
                            lives.Add(new Vector4(birth, cr.Tall, death, c));
                        }
                        foreach (int t in mesh.triangles) tris.Add(start + t);
                    }
                }
                var baked = new Mesh { name = "GlacialWall_" + KindNames[k], indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                baked.SetVertices(verts); baked.SetNormals(norms); baked.SetUVs(0, uvs);
                baked.SetUVs(1, feet); baked.SetUVs(2, lives);
                baked.SetTriangles(tris, 0);
                if (tans.Count == verts.Count) baked.SetTangents(tans); else baked.RecalculateTangents();
                baked.RecalculateBounds();
                // The sunk crystals sit under the road: the bounds must hold them there too, or they cull while rising.
                var b = baked.bounds; b.Encapsulate(b.min - Vector3.up * 2.3f); baked.bounds = b;
                baked.UploadMeshData(true);
                if (look != null)
                {
                    // The shader's growth and drift exist only in this variant; the approved crystals never switch it on.
                    look.EnableKeyword("_GLACIAL_WALL");
                    look.SetFloat("_Grow", 1f);
                    // Lighter than the approved crystal's dark-room values, so the wall reads as ice on a sunlit road and
                    // not as a navy block (g1 read heavy): paler darkest step, less navy in turned faces and deep parts,
                    // bright faces pushed further toward frost. The crystal's own maps and construction are untouched.
                    look.SetFloat("_Shade", 0.62f);
                    look.SetFloat("_DeepAmount", 0.24f);
                    look.SetFloat("_Depth", 0.34f);
                    look.SetColor("_Deep", new Color(0.07f, 0.24f, 0.46f, 1f));
                    look.SetFloat("_LitAmount", 0.62f);
                    look.SetColor("_LitColor", new Color(0.70f, 0.92f, 1.00f, 1f));
                    look.SetFloat("_GrowTime", RiseSeconds);
                    look.SetFloat("_DieTime", SinkSeconds);
                    look.SetFloat("_Shimmer", 0.6f);
                    if (flowTex != null) { look.SetTexture("_FlowTex", flowTex); look.SetFloat("_Flow", 0.50f); }
                }
                Kill(_meshes[k]); Kill(_looks[k]);
                _meshes[k] = baked; _looks[k] = look;
                report.Append(KindNames[k]).Append(": ").Append(count).Append(" crystals, ").Append(tris.Count / 3).Append(" triangles")
                      .Append(readable ? "" : " (source mesh NOT readable: a player build cannot bake it)").Append("; ");
                Kill(probe);
            }

            // The frost band: 40 steps along the arc, 3 across, 1.5 m wide, laid in the wall's space.
            const int along_ = 40, across_ = 3;
            float width = 1.5f, reach = arcLength + 0.9f;
            var fv = new List<Vector3>(); var fu = new List<Vector2>(); var fm = new List<Vector2>(); var ft = new List<int>();
            for (int i = 0; i <= along_; i++)
                for (int j = 0; j <= across_; j++)
                {
                    float s = (i / (float)along_ - 0.5f) * reach, o = (j / (float)across_ - 0.5f) * width;
                    fv.Add(ArcPoint(s, o, radius));
                    fu.Add(new Vector2(i / (float)along_, j / (float)across_));
                    fm.Add(new Vector2(s, o));
                }
            for (int i = 0; i < along_; i++)
                for (int j = 0; j < across_; j++)
                {
                    int a = i * (across_ + 1) + j, b2 = a + across_ + 1;
                    ft.Add(a); ft.Add(a + 1); ft.Add(b2); ft.Add(a + 1); ft.Add(b2 + 1); ft.Add(b2);
                }
            Kill(_frostMesh);
            _frostMesh = new Mesh { name = "GlacialWallFrost" };
            _frostMesh.SetVertices(fv); _frostMesh.SetUVs(0, fu); _frostMesh.SetUVs(1, fm); _frostMesh.SetTriangles(ft, 0);
            _frostMesh.RecalculateNormals(); _frostMesh.RecalculateBounds(); _frostMesh.UploadMeshData(true);
            var frostShader = Resources.Load<Shader>("Shaders/CheskaWallFrost");
            Kill(_frostLook); _frostLook = null;
            if (frostShader != null && frostShader.isSupported)
            {
                _frostLook = new Material(frostShader) { name = "GlacialWallFrost" };
                _frostLook.SetTexture("_MainTex", Resources.Load<Texture2D>("Textures/UltimateFx/crystal_grunge"));
                _frostLook.SetTexture("_ErodeTex", flowTex);
            }
            else report.Append("frost shader missing; ");
            BakeReport = report.ToString();
        }

        private static void Kill(Object thing)
        {
            if (thing == null) return;
            if (Application.isPlaying) Object.Destroy(thing); else Object.DestroyImmediate(thing);
        }
    }
}
