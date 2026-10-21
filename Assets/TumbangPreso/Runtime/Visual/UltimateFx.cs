using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// THE ULTIMATE CUTSCENES' EFFECTS KIT: swept ribbons, shard fans and discs, all drawn by `Shaders/UltimateErode`.
    ///
    /// ⚠️ WHY IT EXISTS. The owner, of the cutscene effects so far: they lack "proper shaders and effects"; and "you should
    /// be more creative with particle effects and do more with emmissives". He pointed at four references and said "try
    /// doing these": a "wispy explosion" made of thin ribbons swept along curves with a mask scrolled along them and
    /// eroded by a threshold; ribbon trails each driven by one small greyscale mask; comic pop explosions (flat pointed
    /// shards in two or three colours, hard BLACK shards behind them, a white core, a halftone patch, a few streaks, all
    /// over in under half a second); a set of magic effects. He had already rejected pixel-art sprite explosions and
    /// bland dot particles. So nothing here is a sprite or a particle. Each piece is a MESH with a silhouette, and it
    /// leaves by tearing apart, not by fading (the shader's header has how).
    ///
    /// ⚠️ THE SHAPES ARE BUILT HERE, AT RUN TIME, NOT EXPORTED AS MODELS. They are a few hundred vertices each, built once
    /// and kept. A file per shape would be a second place for the uv layout the shader depends on to drift, and an asset
    /// the open editor has to import before a cutscene can use it.
    ///
    /// ⚠️ EVERY SHAPE IS TYPED, part by part: each lobe of the crown, each shard of each fan and each streak of the pop
    /// has its own line below with its own numbers. Nothing is scattered by a random number, so there is no seed to
    /// keep in step between peers.
    ///
    /// ⚠️ EVERYTHING IS DRIVEN BY AN AGE THE STAGE HANDS IN. Nothing here reads a clock. The same age draws the same frame
    /// on every peer, in a replay and in a film.
    ///
    /// ⚠️ A MISSING SHADER IS NOT AN ERROR. Every maker returns a piece that draws nothing, and every call on it does
    /// nothing, so the cutscene still runs. `Ready` says which, for a stage that has something older to fall back on.
    ///
    /// A stage does:
    ///     var wisp = UltimateFx.Wisp(root, "crown", body, foam);          // once, when it builds
    ///     UltimateFx.Place(wisp, at, turn, size);                          // every frame
    ///     UltimateFx.Set(wisp, age, erode);                                // 0 fresh, 1 gone
    ///     var pop = UltimateFx.Pop(root, deep, bright, pale);              // once
    ///     UltimateFx.Play(pop, age, at, towardLens, size);                 // every frame; draws nothing outside its life
    /// </summary>
    public static class UltimateFx
    {
        /// <summary>The mask a piece is torn by (`_Look`).</summary>
        public const int Cells = 0, Smoke = 1, Ink = 2, Streak = 3;
        /// <summary>How long a pop is on screen, in seconds. Outside it `Play` draws nothing.</summary>
        public const float PopSeconds = .4f;

        private static readonly int AgeId = Shader.PropertyToID("_Age"), ErodeId = Shader.PropertyToID("_Erode"),
                                    OpacityId = Shader.PropertyToID("_Opacity"), SeedId = Shader.PropertyToID("_Seed");
        private static Shader _shader;
        private static bool _looked;
        private static readonly Dictionary<string, Mesh> Shapes = new Dictionary<string, Mesh>();

        /// <summary>One drawn piece. Its material is its own, so a stage may set any of the shader's properties on it.</summary>
        public sealed class Piece
        {
            public Transform Transform;
            public Renderer Renderer;
            public Material Material;
            public float Seed;
            internal readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();
            /// <summary>Set one of the shader's numbers on this piece; does nothing on a piece that could not be made.</summary>
            public Piece With(string property, float value) { if (Material != null) Material.SetFloat(property, value); return this; }
            public Piece With(string property, Vector4 value) { if (Material != null) Material.SetVector(property, value); return this; }
            public Piece With(string property, Color value) { if (Material != null) Material.SetColor(property, value); return this; }
        }

        /// <summary>A comic pop: its pieces, played together by <see cref="Play"/>.</summary>
        public sealed class Burst
        {
            internal Piece BlackFront, BlackSide, Deep, Bright, Pale, Core, Dots;
            internal readonly Piece[] Streaks = new Piece[PopStreaks.Length];
        }

        /// <summary>True when the shader is there and this machine can draw it.</summary>
        public static bool Ready
        {
            get
            {
                if (!_looked || _shader == null) { _shader = Resources.Load<Shader>("Shaders/UltimateErode"); _looked = true; }
                return _shader != null && _shader.isSupported;
            }
        }

        // ---------------------------------------------------------------- makers

        /// <summary>
        /// A ribbon of <paramref name="shape"/> ("crown", "hook", "whip", "arc", "s", "spiral", or any name of
        /// <see cref="Shape"/>): <paramref name="core"/> in its body, <paramref name="edge"/> where it is thin and where it
        /// tears. It draws nothing until <see cref="Set"/> is called.
        /// </summary>
        public static Piece Wisp(Transform parent, string shape, Color core, Color edge, int look = Smoke, int order = 0)
        {
            var piece = Make(parent, "UltimateWisp-" + shape, Shape(shape), order);
            if (piece.Material == null) return piece;
            piece.Material.SetColor("_Core", core); piece.Material.SetColor("_Edge", edge);
            piece.Material.SetColor("_Hot", Color.Lerp(core, edge, .5f));
            piece.Material.SetFloat("_Look", look);
            return piece;
        }

        /// <summary>
        /// A disc lying on the ground, one across, eaten from its middle outward along streaks as its erosion rises: the
        /// wash an impact sends out. Scale it to its size; it draws nothing until <see cref="Set"/> is called.
        /// </summary>
        public static Piece Disc(Transform parent, Color core, Color edge, int look = Streak, int order = 0)
        {
            var piece = Make(parent, "UltimateDisc", Shape("disc"), order);
            if (piece.Material == null) return piece;
            piece.Material.SetColor("_Core", core); piece.Material.SetColor("_Edge", edge);
            piece.Material.SetColor("_Hot", edge);
            piece.Material.SetFloat("_Look", look); piece.Material.SetFloat("_Radial", 1f);
            piece.Material.SetVector("_Scale", new Vector4(1.2f, 7f, 0, 0)); piece.Material.SetVector("_Scroll", new Vector4(1.5f, 0, 0, 0));
            piece.Material.SetFloat("_Shape", 0f); piece.Material.SetFloat("_Band", .26f);
            return piece;
        }

        /// <summary>
        /// A comic pop in three colours, darkest first, with black shards behind them. <paramref name="order"/> is the
        /// sorting order of its back layer; it takes the six above it too.
        /// </summary>
        public static Burst Pop(Transform parent, Color deep, Color bright, Color pale, int order = 20)
        {
            var burst = new Burst();
            burst.Dots = Make(parent, "UltimatePop-Halftone", Shape("card"), order);
            burst.BlackSide = PopPiece(parent, "fanA", Color.black, Color.black, order + 1, dark: true);
            burst.BlackFront = PopPiece(parent, "fanA", Color.black, Color.black, order + 2, dark: true);
            burst.Deep = PopPiece(parent, "fanB", deep, Color.Lerp(deep, bright, .55f), order + 3, dark: false);
            burst.Bright = PopPiece(parent, "fanB", bright, Color.Lerp(bright, pale, .6f), order + 4, dark: false);
            burst.Pale = PopPiece(parent, "fanC", pale, Color.white, order + 5, dark: false);
            burst.Core = Make(parent, "UltimatePop-Core", Shape("card"), order + 6);
            for (int i = 0; i < burst.Streaks.Length; i++)
                burst.Streaks[i] = PopPiece(parent, "streak", i % 2 == 0 ? pale : bright, pale, order + 5, dark: false);
            if (burst.Dots.Material != null)
            {
                // The halftone patch: a disc of dots, big in its middle and shrinking to nothing at its rim.
                var m = burst.Dots.Material;
                m.SetColor("_Core", bright); m.SetFloat("_Radial", 1f); m.SetFloat("_Dots", 1f); m.SetFloat("_DotScale", 13f);
                m.SetFloat("_Look", Smoke); m.SetVector("_Scale", new Vector4(.4f, .6f, 0, 0)); m.SetVector("_Scroll", Vector4.zero);
                m.SetFloat("_Shape", 0f); m.SetFloat("_Spread", 1.2f); m.SetFloat("_Warp", .2f); m.SetFloat("_Fill", .8f);
            }
            if (burst.Core.Material != null)
            {
                // The white-hot middle: two or three frames, eaten from its own centre.
                var m = burst.Core.Material;
                m.SetColor("_Core", Color.white); m.SetColor("_Edge", pale); m.SetColor("_Hot", pale);
                m.SetFloat("_Radial", 1f); m.SetFloat("_Look", Streak); m.SetVector("_Scale", new Vector4(.6f, 5f, 0, 0));
                m.SetVector("_Scroll", new Vector4(2f, 0, 0, 0)); m.SetFloat("_Shape", 0f); m.SetFloat("_Band", .3f); m.SetFloat("_Spread", .5f);
            }
            return burst;
        }

        private static Piece PopPiece(Transform parent, string shape, Color core, Color edge, int order, bool dark)
        {
            var piece = Make(parent, "UltimatePop-" + shape, Shape(shape), order);
            if (piece.Material == null) return piece;
            var m = piece.Material;
            m.SetColor("_Core", core); m.SetColor("_Edge", edge); m.SetColor("_Hot", edge);
            // A shard is a flat, hard shape: the mask only decides how it breaks up, root first, when it goes.
            m.SetFloat("_Look", Streak); m.SetVector("_Scale", new Vector4(1.4f, 2.2f, 0, 0)); m.SetVector("_Scroll", new Vector4(.8f, 0, 0, 0));
            m.SetFloat("_Shape", 0f); m.SetFloat("_Sweep", 1.3f); m.SetFloat("_Band", .1f); m.SetFloat("_Soft", .015f);
            m.SetFloat("_Dark", dark ? 1f : 0f); m.SetFloat("_Glow", dark ? 0f : .35f);
            return piece;
        }

        private static Piece Make(Transform parent, string name, Mesh mesh, int order)
        {
            var piece = new Piece();
            if (parent == null || mesh == null || !Ready) return piece;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.sortingOrder = order; renderer.enabled = false;
            var material = new Material(_shader) { name = name };
            renderer.sharedMaterial = material;
            // An effect, not part of anyone's model; and the material is this object's to free.
            VfxRenderTag.Own(go, material);
            piece.Transform = go.transform; piece.Renderer = renderer; piece.Material = material;
            return piece;
        }

        // ---------------------------------------------------------------- every frame

        /// <summary>Where a piece is, in its parent's space.</summary>
        public static void Place(Piece piece, Vector3 at, Quaternion turn, Vector3 size)
        {
            if (piece == null || piece.Transform == null) return;
            piece.Transform.localPosition = at; piece.Transform.localRotation = turn; piece.Transform.localScale = size;
        }

        /// <summary>
        /// A piece's moment: <paramref name="age"/> runs its mask along it, <paramref name="erode"/> is how much of it is
        /// gone (0 fresh, 1 gone). A negative age, an erosion of 1 or no opacity hides it.
        /// </summary>
        public static void Set(Piece piece, float age, float erode, float opacity = 1f)
        {
            if (piece == null || piece.Renderer == null) return;
            bool seen = age >= 0f && erode < .999f && opacity > .002f;
            piece.Renderer.enabled = seen;
            if (!seen) return;
            piece.Block.SetFloat(AgeId, age); piece.Block.SetFloat(ErodeId, Mathf.Clamp01(erode));
            piece.Block.SetFloat(OpacityId, Mathf.Clamp01(opacity)); piece.Block.SetFloat(SeedId, piece.Seed);
            piece.Renderer.SetPropertyBlock(piece.Block);
        }

        private static float Smooth(float from, float to, float value)
        { float u = Mathf.Clamp01((value - from) / (to - from)); return u * u * (3 - 2 * u); }

        /// <summary>Out fast, past its size, and back to it: how a drawn burst arrives.</summary>
        private static float Overshoot(float u, float past = 1.9f)
        {
            u = Mathf.Clamp01(u) - 1f;
            return 1f + (past + 1f) * u * u * u + past * u * u;
        }

        /// <summary>
        /// The pop at <paramref name="age"/> seconds, at <paramref name="at"/>, turned to face along
        /// <paramref name="toLens"/> (both in its parent's space), <paramref name="size"/> metres to its longest tip.
        /// Black out first and fastest; the colours a frame behind, past their size and back; a white core for two or
        /// three frames; the halftone patch and the streaks; then everything tears away from its root.
        /// <paramref name="flash"/> false leaves the white core out, for reduced effects.
        /// </summary>
        public static void Play(Burst burst, float age, Vector3 at, Vector3 toLens, float size, bool flash = true)
        {
            if (burst == null) return;
            bool on = age >= 0f && age < PopSeconds;
            var face = Quaternion.LookRotation(toLens.sqrMagnitude > 1e-6f ? toLens.normalized : Vector3.forward, Vector3.up);
            float frame = 1f / 30f;
            // Black: there on the first frame at nearly its full size, creeping on, the first to go.
            float black = Overshoot(age / .06f, 1.2f) * (1f + .25f * age);
            Layer(burst.BlackFront, on, age, at, face, size * black, Smooth(.10f, .27f, age));
            Layer(burst.BlackSide, on, age, at, face * Quaternion.Euler(0, 68f, 0) * Quaternion.Euler(0, 0, 14f), size * black * .92f, Smooth(.09f, .25f, age));
            // The colours: a frame behind the black and a frame behind each other, each smaller and turned off the last.
            float deep = age - frame, bright = age - frame * 1.5f, pale = age - frame;
            Layer(burst.Deep, on, deep, at, face * Quaternion.Euler(0, 0, -7f), size * .88f * Overshoot(deep / .1f), Smooth(.11f, .28f, deep));
            Layer(burst.Bright, on, bright, at, face * Quaternion.Euler(0, 0, 11f), size * .7f * Overshoot(bright / .1f), Smooth(.12f, .30f, bright));
            Layer(burst.Pale, on, pale, at, face * Quaternion.Euler(0, 0, 3f), size * .62f * Overshoot(pale / .08f), Smooth(.09f, .24f, pale));
            // The core: white, small, and eaten out from its middle at once.
            Layer(burst.Core, on && flash, age, at, face, size * Mathf.Lerp(.16f, .34f, Mathf.Clamp01(age / .06f)), Smooth(.02f, .12f, age));
            // The halftone patch: behind everything and off to one side, its dots shrinking.
            float dots = age - frame;
            Layer(burst.Dots, on, dots, at + face * new Vector3(-.34f, .3f, 0) * size, face, size * Mathf.Lerp(.9f, 1.25f, Smooth(0, .3f, dots)), Smooth(.08f, .30f, dots));
            for (int i = 0; i < burst.Streaks.Length; i++)
            {
                // A streak: a thin line thrown out along its own way, slowing, shortening.
                var typed = PopStreaks[i];
                float a = age - typed.Delay, go = 1f - Mathf.Pow(1f - Mathf.Clamp01(a / .3f), 2.4f);
                var turn = face * Quaternion.Euler(0, 0, -typed.Angle);
                var from = at + turn * Vector3.up * (size * (typed.Start + typed.Reach * go));
                float length = size * typed.Long * (1f - .5f * go);
                if (burst.Streaks[i] != null && burst.Streaks[i].Transform != null)
                    Place(burst.Streaks[i], from, turn, new Vector3(size * .05f, length, 1f));
                Set(burst.Streaks[i], on ? a : -1f, Smooth(.10f, .28f, a));
            }
        }

        private static void Layer(Piece piece, bool on, float age, Vector3 at, Quaternion turn, float size, float erode)
        {
            if (piece == null || piece.Transform == null) return;
            Place(piece, at, turn, Vector3.one * Mathf.Max(.001f, size));
            Set(piece, on ? age : -1f, erode);
        }

        // ---------------------------------------------------------------- the shapes, each typed

        /// <summary>
        /// A shape by name, built once. Ribbons have uv.x along them and uv.y across, and their ends are tapered both in
        /// width and in vertex alpha (which the shader reads as erosion, so an end is torn, never cut).
        ///   "crown"   seven curled lobes thrown up and out round a ring; 1 high, about 1 out.
        ///   "hook"    one lobe that rises, goes over and hooks back under. Out along +z, up +y.
        ///   "whip"    one lobe thrown lower and further, its tip trailing.
        ///   "arc"     a plain quarter arc.
        ///   "s"       an S standing up.
        ///   "spiral"  a ribbon winding up and outward round +y.
        ///   "fanA" "fanB" "fanC"  flat fans of pointed shards in the xy plane, radiating up from the origin (9 long,
        ///             8 middling, 7 short); a shard's uv.x runs from its root to its tip.
        ///   "streak"  one thin shard along +y, 1 long and 1 wide (scale it).
        ///   "disc"    a disc on the ground (xz), 1 in radius, its rim's alpha going to nothing.
        ///   "card"    the same disc standing up (xy), for a piece that faces the lens.
        /// </summary>
        public static Mesh Shape(string name)
        {
            if (Shapes.TryGetValue(name, out var kept) && kept != null) return kept;
            Mesh mesh;
            switch (name)
            {
                case "crown": mesh = CrownMesh(); break;
                case "hook": mesh = LobeMesh(HookOut, HookUp, HookWide, 1f, 1f, .5f, 50f); break;
                case "whip": mesh = LobeMesh(WhipOut, WhipUp, WhipWide, 1f, 1f, .5f, -40f); break;
                case "arc": mesh = LobeMesh(ArcOut, ArcUp, ArcWide, 1f, 1f, .4f, 25f); break;
                case "s": mesh = Single(SPath, SWide, STwist, Vector3.forward); break;
                case "spiral": mesh = Single(SpiralPath, SpiralWide, SpiralTwist, Vector3.up); break;
                case "fanA": mesh = FanMesh(FanA); break;
                case "fanB": mesh = FanMesh(FanB); break;
                case "fanC": mesh = FanMesh(FanC); break;
                case "streak": mesh = FanMesh(OneStreak); break;
                case "disc": mesh = DiscMesh(false); break;
                case "card": mesh = DiscMesh(true); break;
                default: return null;
            }
            mesh.name = "UltimateFx " + name;
            // Kept for the life of the program and never saved into a scene.
            mesh.hideFlags = HideFlags.HideAndDontSave;
            Shapes[name] = mesh;
            return mesh;
        }

        // A lobe of thrown water in its own upright plane, row by row from its root: how far out (as a share of its
        // reach), how high (as a share of its height) and how wide (as a share of its width).
        // The HOOK rises steeply, goes over the top and curls back under itself.
        private static readonly float[] HookOut = { .10f, .16f, .30f, .52f, .78f, .98f, 1.04f, .96f };
        private static readonly float[] HookUp = { 0f, .34f, .66f, .90f, 1f, .86f, .62f, .46f };
        private static readonly float[] HookWide = { .55f, .9f, 1f, .95f, .8f, .55f, .3f, .06f };
        // The WHIP is thrown lower and further and its tip trails down.
        private static readonly float[] WhipOut = { .10f, .22f, .42f, .66f, .88f, 1.02f, 1.10f };
        private static readonly float[] WhipUp = { 0f, .30f, .55f, .72f, .78f, .70f, .52f };
        private static readonly float[] WhipWide = { .5f, .85f, 1f, .85f, .6f, .32f, .05f };
        private static readonly float[] ArcOut = { 0f, .20f, .45f, .72f, .92f, 1f };
        private static readonly float[] ArcUp = { 0f, .42f, .74f, .92f, .98f, .90f };
        private static readonly float[] ArcWide = { .4f, .85f, 1f, .8f, .45f, .05f };

        private struct Lobe { public float Azimuth, Reach, Height, Wide, Twist; public bool Whip; }
        // THE CROWN, lobe by lobe round the ring: where (degrees), how far out, how high, how wide, how much it twists
        // along its length, and which of the two profiles it is. No two alike, and no two neighbours the same height.
        private static readonly Lobe[] CrownLobes =
        {
            new Lobe { Azimuth = 8f, Reach = 1.00f, Height = 1.00f, Wide = .50f, Twist = 40f, Whip = false },
            new Lobe { Azimuth = 62f, Reach = .82f, Height = .74f, Wide = .42f, Twist = -30f, Whip = true },
            new Lobe { Azimuth = 110f, Reach = 1.08f, Height = .90f, Wide = .50f, Twist = 55f, Whip = false },
            new Lobe { Azimuth = 163f, Reach = .70f, Height = 1.05f, Wide = .40f, Twist = -45f, Whip = false },
            new Lobe { Azimuth = 215f, Reach = .95f, Height = .68f, Wide = .46f, Twist = 35f, Whip = true },
            new Lobe { Azimuth = 262f, Reach = .78f, Height = .96f, Wide = .44f, Twist = -50f, Whip = false },
            new Lobe { Azimuth = 318f, Reach = 1.10f, Height = .80f, Wide = .50f, Twist = 30f, Whip = true },
        };

        // An S standing up, and a spiral winding up and out, point by point.
        private static readonly Vector3[] SPath =
        {
            new Vector3(0f, 0f, 0f), new Vector3(.25f, .30f, .10f), new Vector3(.10f, .60f, .30f),
            new Vector3(-.20f, .85f, .20f), new Vector3(-.05f, 1.15f, -.05f), new Vector3(.20f, 1.40f, 0f),
        };
        private static readonly float[] SWide = { .12f, .26f, .30f, .28f, .20f, .03f }, STwist = { 0f, 20f, 50f, 80f, 100f, 110f };
        private static readonly Vector3[] SpiralPath =
        {
            new Vector3(0f, 0f, .20f), new Vector3(.26f, .12f, .15f), new Vector3(.35f, .26f, -.20f), new Vector3(0f, .42f, -.50f),
            new Vector3(-.52f, .60f, -.30f), new Vector3(-.61f, .80f, .35f), new Vector3(0f, 1.00f, .80f), new Vector3(.76f, 1.18f, .44f),
        };
        private static readonly float[] SpiralWide = { .06f, .16f, .22f, .26f, .26f, .22f, .15f, .02f },
                                        SpiralTwist = { 0f, 8f, 16f, 22f, 28f, 34f, 40f, 46f };

        private struct Shard { public float Angle, Long, Wide, Root; }
        // THE FANS, shard by shard: its angle off straight up (degrees, + to the right), how long, how wide at its
        // shoulder (half), and how far from the middle its root is. Long beside short, never two of a length together.
        private static readonly Shard[] FanA =
        {
            new Shard { Angle = -96f, Long = .62f, Wide = .085f, Root = .05f }, new Shard { Angle = -71f, Long = .90f, Wide = .100f, Root = .04f },
            new Shard { Angle = -48f, Long = .70f, Wide = .090f, Root = .06f }, new Shard { Angle = -27f, Long = 1.00f, Wide = .110f, Root = .03f },
            new Shard { Angle = -6f, Long = .78f, Wide = .090f, Root = .05f }, new Shard { Angle = 14f, Long = 1.08f, Wide = .115f, Root = .03f },
            new Shard { Angle = 37f, Long = .72f, Wide = .090f, Root = .06f }, new Shard { Angle = 58f, Long = .95f, Wide = .100f, Root = .04f },
            new Shard { Angle = 88f, Long = .66f, Wide = .085f, Root = .05f },
        };
        private static readonly Shard[] FanB =
        {
            new Shard { Angle = -84f, Long = .70f, Wide = .100f, Root = .04f }, new Shard { Angle = -58f, Long = .55f, Wide = .090f, Root = .05f },
            new Shard { Angle = -38f, Long = .92f, Wide = .120f, Root = .03f }, new Shard { Angle = -15f, Long = .62f, Wide = .100f, Root = .05f },
            new Shard { Angle = 4f, Long = .98f, Wide = .125f, Root = .03f }, new Shard { Angle = 26f, Long = .60f, Wide = .095f, Root = .05f },
            new Shard { Angle = 47f, Long = .86f, Wide = .115f, Root = .04f }, new Shard { Angle = 74f, Long = .58f, Wide = .090f, Root = .05f },
        };
        private static readonly Shard[] FanC =
        {
            new Shard { Angle = -66f, Long = .42f, Wide = .090f, Root = .02f }, new Shard { Angle = -42f, Long = .50f, Wide = .100f, Root = .02f },
            new Shard { Angle = -20f, Long = .38f, Wide = .085f, Root = .02f }, new Shard { Angle = 2f, Long = .56f, Wide = .110f, Root = .02f },
            new Shard { Angle = 22f, Long = .40f, Wide = .090f, Root = .02f }, new Shard { Angle = 44f, Long = .50f, Wide = .100f, Root = .02f },
            new Shard { Angle = 68f, Long = .36f, Wide = .085f, Root = .02f },
        };
        private static readonly Shard[] OneStreak = { new Shard { Angle = 0f, Long = 1f, Wide = .5f, Root = 0f } };

        private struct Thrown { public float Angle, Start, Reach, Long, Delay; }
        // The pop's streak lines: which way (degrees off up), where each starts and how far it flies (in pop sizes), how
        // long it is, and how late it leaves.
        private static readonly Thrown[] PopStreaks =
        {
            new Thrown { Angle = -63f, Start = .55f, Reach = .50f, Long = .38f, Delay = .03f },
            new Thrown { Angle = -32f, Start = .80f, Reach = .62f, Long = .46f, Delay = .05f },
            new Thrown { Angle = 9f, Start = .90f, Reach = .70f, Long = .52f, Delay = .02f },
            new Thrown { Angle = 41f, Start = .70f, Reach = .55f, Long = .40f, Delay = .06f },
            new Thrown { Angle = 72f, Start = .60f, Reach = .48f, Long = .34f, Delay = .04f },
        };

        // ---------------------------------------------------------------- how a shape is made

        private sealed class Build
        {
            public readonly List<Vector3> At = new List<Vector3>();
            public readonly List<Vector2> Uv = new List<Vector2>();
            public readonly List<Color> Paint = new List<Color>();
            public readonly List<int> Triangles = new List<int>();
            public Mesh Finish()
            {
                var mesh = new Mesh();
                mesh.SetVertices(At); mesh.SetUVs(0, Uv); mesh.SetColors(Paint); mesh.SetTriangles(Triangles, 0);
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                return mesh;
            }
        }

        private static Vector3 Curve(Vector3[] p, float at)
        {
            int last = p.Length - 1, i = Mathf.Clamp(Mathf.FloorToInt(at), 0, last - 1);
            float s = at - i, s2 = s * s, s3 = s2 * s;
            Vector3 a = p[Mathf.Max(i - 1, 0)], b = p[i], c = p[i + 1], d = p[Mathf.Min(i + 2, last)];
            return .5f * ((2f * b) + (c - a) * s + (2f * a - 5f * b + 4f * c - d) * s2 + (3f * b - a - 3f * c + d) * s3);
        }

        private static float Along(float[] values, float at)
        {
            int last = values.Length - 1, i = Mathf.Clamp(Mathf.FloorToInt(at), 0, last - 1);
            return Mathf.Lerp(values[i], values[i + 1], at - i);
        }

        /// <summary>
        /// Sweep a ribbon along a typed path: three vertices across (its middle raised a little, so it is a trough and
        /// catches the eye as a surface and not a card), smooth between the typed points, twisting by the typed degrees.
        /// <paramref name="side"/> is roughly which way its width lies.
        /// </summary>
        private static void Sweep(Build into, Vector3[] path, float[] wide, float[] twist, Vector3 side)
        {
            const int between = 5;
            int rows = (path.Length - 1) * between + 1, first = into.At.Count;
            float length = 0f; var before = path[0];
            var lengths = new float[rows];
            for (int row = 0; row < rows; row++)
            {
                var here = Curve(path, row / (float)between);
                length += Vector3.Distance(here, before); lengths[row] = length; before = here;
            }
            for (int row = 0; row < rows; row++)
            {
                float at = row / (float)between, u = length > 0f ? lengths[row] / length : 0f;
                var here = Curve(path, at);
                var way = (Curve(path, Mathf.Min(at + .05f, path.Length - 1)) - Curve(path, Mathf.Max(at - .05f, 0f))).normalized;
                var across = side - way * Vector3.Dot(side, way);
                across = across.sqrMagnitude > 1e-6f ? across.normalized : Vector3.right;
                across = Quaternion.AngleAxis(Along(twist, at), way) * across;
                var bulge = Vector3.Cross(way, across);
                float w = Along(wide, at) * .5f;
                // Torn at both ends: quickly at the root, over a longer run at the tip.
                float whole = Mathf.Clamp01(u / .08f) * Mathf.Clamp01((1f - u) / .22f);
                into.At.Add(here - across * w); into.At.Add(here + bulge * (w * .28f)); into.At.Add(here + across * w);
                into.Uv.Add(new Vector2(u, 0f)); into.Uv.Add(new Vector2(u, .5f)); into.Uv.Add(new Vector2(u, 1f));
                var paint = new Color(1, 1, 1, whole);
                into.Paint.Add(paint); into.Paint.Add(paint); into.Paint.Add(paint);
                if (row == rows - 1) continue;
                int v = first + row * 3;
                for (int k = 0; k < 2; k++)
                {
                    into.Triangles.Add(v + k); into.Triangles.Add(v + k + 3); into.Triangles.Add(v + k + 1);
                    into.Triangles.Add(v + k + 1); into.Triangles.Add(v + k + 3); into.Triangles.Add(v + k + 4);
                }
            }
        }

        private static Mesh Single(Vector3[] path, float[] wide, float[] twist, Vector3 side)
        {
            var build = new Build();
            Sweep(build, path, wide, twist, side);
            return build.Finish();
        }

        /// <summary>One lobe's path, width and twist from a typed profile, turned to its place on the ring.</summary>
        private static void AddLobe(Build into, float[] outward, float[] up, float[] wide, float azimuth, float reach, float height, float width, float twist)
        {
            float a = azimuth * Mathf.Deg2Rad;
            var away = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
            int n = outward.Length;
            var path = new Vector3[n]; var widths = new float[n]; var twists = new float[n];
            for (int i = 0; i < n; i++)
            {
                path[i] = away * (outward[i] * reach) + Vector3.up * (up[i] * height);
                widths[i] = wide[i] * width;
                twists[i] = twist * i / (n - 1f);
            }
            Sweep(into, path, widths, twists, Vector3.Cross(Vector3.up, away));
        }

        private static Mesh LobeMesh(float[] outward, float[] up, float[] wide, float reach, float height, float width, float twist)
        {
            var build = new Build();
            AddLobe(build, outward, up, wide, 0f, reach, height, width, twist);
            return build.Finish();
        }

        private static Mesh CrownMesh()
        {
            var build = new Build();
            foreach (var lobe in CrownLobes)
            {
                if (lobe.Whip) AddLobe(build, WhipOut, WhipUp, WhipWide, lobe.Azimuth, lobe.Reach, lobe.Height, lobe.Wide, lobe.Twist);
                else AddLobe(build, HookOut, HookUp, HookWide, lobe.Azimuth, lobe.Reach, lobe.Height, lobe.Wide, lobe.Twist);
            }
            return build.Finish();
        }

        /// <summary>A fan of flat pointed shards in the xy plane: each a narrow root, a shoulder a quarter of the way along, and a point.</summary>
        private static Mesh FanMesh(Shard[] shards)
        {
            var build = new Build();
            foreach (var shard in shards)
            {
                var turn = Quaternion.Euler(0, 0, -shard.Angle);
                Vector3 along = turn * Vector3.up, across = turn * Vector3.right;
                int v = build.At.Count;
                float shoulder = shard.Root + shard.Long * .26f, tip = shard.Root + shard.Long;
                build.At.Add(along * shard.Root - across * (shard.Wide * .5f)); build.Uv.Add(new Vector2(0f, .25f));
                build.At.Add(along * shard.Root + across * (shard.Wide * .5f)); build.Uv.Add(new Vector2(0f, .75f));
                build.At.Add(along * shoulder - across * shard.Wide); build.Uv.Add(new Vector2(.26f, 0f));
                build.At.Add(along * shoulder + across * shard.Wide); build.Uv.Add(new Vector2(.26f, 1f));
                build.At.Add(along * tip); build.Uv.Add(new Vector2(1f, .5f));
                for (int k = 0; k < 5; k++) build.Paint.Add(Color.white);
                build.Triangles.Add(v); build.Triangles.Add(v + 2); build.Triangles.Add(v + 1);
                build.Triangles.Add(v + 1); build.Triangles.Add(v + 2); build.Triangles.Add(v + 3);
                build.Triangles.Add(v + 2); build.Triangles.Add(v + 4); build.Triangles.Add(v + 3);
            }
            return build.Finish();
        }

        // The disc, ring by ring from its middle: how far out, and how whole it is there (its rim is torn, never cut).
        private static readonly float[] DiscRings = { 0f, .25f, .5f, .72f, .88f, 1f }, DiscWhole = { 1f, 1f, .88f, .62f, .32f, 0f };

        private static Mesh DiscMesh(bool standing)
        {
            const int sides = 36; int rows = DiscRings.Length;
            var build = new Build();
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides;
                var rim = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
                for (int row = 0; row < rows; row++)
                {
                    var p = rim * DiscRings[row];
                    build.At.Add(standing ? new Vector3(p.x, p.y, 0) : new Vector3(p.x, 0, p.y));
                    build.Uv.Add(p * .5f + new Vector2(.5f, .5f));
                    build.Paint.Add(new Color(1, 1, 1, DiscWhole[row]));
                    if (i == sides || row == rows - 1) continue;
                    int v = i * rows + row;
                    build.Triangles.Add(v); build.Triangles.Add(v + rows); build.Triangles.Add(v + 1);
                    build.Triangles.Add(v + rows); build.Triangles.Add(v + rows + 1); build.Triangles.Add(v + 1);
                }
            }
            return build.Finish();
        }
    }
}
