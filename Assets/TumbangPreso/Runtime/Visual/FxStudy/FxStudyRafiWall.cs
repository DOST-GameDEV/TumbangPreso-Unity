using UnityEngine;
using Knot = TumbangPreso.Visual.FxStudyKit.Knot;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// STUDY I, THE WATER WALL (water is an element this bench had never built). Ilyas raises a thin curtain of water
    /// across a line: 4 m wide (`RafiRules.WallHalfWidth` either side of its middle), 1.8 m tall (`WallHeight`),
    /// standing 0.25 s after the cast (`WallGather`) and there for 4 s (`WallSeconds`). People walk through it; the
    /// first flying slipper to cross it is dropped where it crossed and the wall breaks. So the picture is a FALLING
    /// SHEET on a line, that wide and that tall and no more, see-through, and it ends by coming down.
    ///
    /// What it is made of, in the method's terms (`docs/VFX_BENCH_METHOD.md` section 1a):
    ///   * THE SHEET is one curtain swept from the ground up and over a shoulder (`FxStudyKit.Sweep`), wearing a coarse
    ///     noise drawn out along the fall (`water_flow`) that runs DOWN it; a second copy behind runs slower and deeper,
    ///     and a faint even veil under both keeps the gaps from reading as holes in nothing.
    ///   * THE WHITE is thin lines only (`water_lines`), running down faster than the body, in patches.
    ///   * THE LIP along the top and THE FOAM along the foot are lobes hung from one edge (`water_foam`).
    ///   * THE ARCS are short broken rings on the ground (`water_ring`), only when it gathers and when it lands.
    ///   * THE DROPS are few and fat (`water_drop`, through the bench's `FxFlecks`): thrown at birth and at landing.
    ///   * THE SPLASH CROWN at landing is big foam lobes (`water_crest`) thrown up and out.
    /// The bench's own kit draws all of it (`FxStudyRibbon.shader`, the sweep, the flecks, the glow, its erosion maps);
    /// only the six water masks are new (`tools/build_rafi_water_textures.py`).
    ///
    /// ⚠️ THE BODY IS THE FIRST SAMPLE'S, KEPT. Two were shown: the first (film w2), and a second with fewer and wider
    /// streaks, torn ends and a scalloped crest (w3). The owner: "rafi use the first". So the standing wall is w2's, and
    /// what was added round it is only how it is born and how it ends.
    ///
    /// ⚠️ NOT ICE AND NOT GLASS. The body is moving streaks with air between them, the sheet is folded along its width
    /// like cloth and bends over at its top, and its ends thin out. Ice stands still and has facets; this never stops
    /// running. Nothing of it stands outside 4 m by 1.8 m.
    ///
    /// ⚠️ IT ENDS BY FALLING, AND NOT BY BEING SQUASHED OR FADED. Water that loses its hold drops: the sheet stays where
    /// it stands, its picture is let go and runs down faster and faster (`Pose` slides it by the fall of a dropped
    /// thing), and it is eaten from the top by its own noise, so what is left on the way down is separate tongues (the
    /// whirlwind's mouth is made the same way). The lip falls with it. Where it lands a crown of foam lobes is thrown up
    /// and out and falls back, the foot's foam is thrown wide, two arcs run out and the drops fly.
    ///
    /// ⚠️ IT STANDS UNTIL IT STOPS BLOCKING. It lets go at the moment the rule ends (4 s, or the moment a slipper
    /// breaks it) and never before, so the fall and the splash are AFTER the wall is gone from the rules.
    ///
    /// The motion is our own design: what was looked at were stills of stylised game waterfalls.
    /// </summary>
    public sealed class FxStudyRafiWall : FxStudyEffect
    {
        // The rule's own numbers, so the picture cannot drift from what blocks.
        public const float HalfWide = Core.RafiRules.WallHalfWidth, Tall = Core.RafiRules.WallHeight,
                           Gather = Core.RafiRules.WallGather, Life = Core.RafiRules.WallSeconds;
        /// <summary>How long the sheet takes to come down once it lets go, and how long the splash lasts after it lands.</summary>
        public const float Fall = 0.42f, Splash = 0.75f;
        // How long the curtain is from its foot to its top along its own surface (m): what a fall is measured against.
        private const float Run_ = 2.1f;

        /// <summary>How one wall is coloured. `Bench` is the dark floor's; a wall stood in daylight hands in its own.</summary>
        public struct Look
        {
            /// <summary>The falling body: its faint edge, its body, and the spine of a streak.</summary>
            public Color Deep, Body, Pale;
            /// <summary>The thin lines, the crest and the foam.</summary>
            public Color Foam;
            /// <summary>How much of every piece is light laid on (1) and how much covers what is behind it (0).</summary>
            public float Light;
            /// <summary>How far each piece's hue wanders either side of its own (degrees): toward a green teal one way
            /// and a deep blue the other.</summary>
            public float HueDrift;

            // ⚠️ TWO NAMED EXTREMES. The body sits at hue 188; a drift of 16 degrees takes it to a green teal (172, the
            // lagoon's shallows) one way and a deep blue (204) the other. Bluer than Amihan's wind (168), so the two
            // kits do not read as one element. White is kept for the lines, the crest and the foam.
            public static Look Bench => new Look
            {
                Deep = Color.HSVToRGB(198f / 360f, 0.88f, 0.34f), Body = Color.HSVToRGB(188f / 360f, 0.82f, 0.74f),
                Pale = Color.HSVToRGB(178f / 360f, 0.42f, 0.96f), Foam = new Color(0.93f, 1f, 0.98f),
                Light = 0.45f, HueDrift = 16f,
            };
        }

        // One drop: where along the wall it starts (m), how high, how far off the line, how it is thrown (m/s), how
        // big it is (m), which of the four drops it wears, and when (s after its event) it is thrown.
        private readonly struct Drop
        {
            public readonly Vector3 From, Throw; public readonly float Size, After; public readonly int Cell;
            public Drop(float x, float y, float z, float vx, float vy, float vz, float size, int cell, float after)
            { From = new Vector3(x, y, z); Throw = new Vector3(vx, vy, vz); Size = size; Cell = cell; After = after; }
        }
        // ⚠️ FEW AND FAT. Eight small ones at birth barely registered. Five, each the size of a fist, thrown up off the
        // foot as the sheet surges.
        private static readonly Drop[] Born =
        {
            new Drop(-1.70f, 0.10f, 0.05f, -0.9f, 3.6f,  0.9f, 0.26f, 0, 0.00f), new Drop(-0.60f, 0.10f, -0.05f, -0.2f, 4.6f, -1.2f, 0.20f, 1, 0.03f),
            new Drop( 0.30f, 0.10f, 0.05f,  0.3f, 3.2f,  1.5f, 0.30f, 0, 0.01f), new Drop( 1.20f, 0.10f, -0.05f,  0.4f, 4.2f, -1.0f, 0.22f, 3, 0.04f),
            new Drop( 1.85f, 0.10f, 0.05f,  1.1f, 3.4f,  0.6f, 0.24f, 0, 0.02f),
        };
        // Thrown outward, both ways, where the sheet lands: seven.
        private static readonly Drop[] Landed =
        {
            new Drop(-1.80f, 0.10f,  0.1f, -1.2f, 2.8f,  2.3f, 0.30f, 0, 0.00f), new Drop(-1.10f, 0.10f, -0.1f, -0.3f, 3.6f, -2.8f, 0.24f, 1, 0.03f),
            new Drop(-0.40f, 0.10f,  0.1f,  0.2f, 2.4f,  3.1f, 0.34f, 0, 0.00f), new Drop( 0.20f, 0.10f, -0.1f, -0.2f, 4.2f, -1.9f, 0.22f, 3, 0.05f),
            new Drop( 0.85f, 0.10f,  0.1f,  0.3f, 3.0f,  2.6f, 0.28f, 0, 0.02f), new Drop( 1.40f, 0.10f, -0.1f,  0.6f, 2.6f, -3.0f, 0.32f, 0, 0.00f),
            new Drop( 1.90f, 0.10f,  0.1f,  1.3f, 3.4f,  1.5f, 0.24f, 2, 0.04f),
        };
        // The crest's own line: how far under the wall's full height each of its nine points stands (m), so the top is
        // uneven and never a ruled edge. Nothing stands over `Tall`.
        private static readonly float[] Crown = { 0.14f, 0.06f, 0.10f, 0.02f, 0.07f, 0.03f, 0.11f, 0.05f, 0.15f };
        // How tall the splash crown is at each of seven points along the wall (a share of its full throw), near side and far.
        private static readonly float[,] CrownHigh = { { 0.50f, 1.00f, 0.42f, 0.78f, 0.36f, 0.92f, 0.55f }, { 0.72f, 0.40f, 0.95f, 0.48f, 0.85f, 0.38f, 0.66f } };
        // How many columns the curtain's mesh has across its 4 m.
        private const int Columns = 12;

        private readonly Look _look;
        private readonly float _benchLetGo, _benchBurst;
        private readonly Transform _stand, _foot, _crestAt;
        private readonly Transform[] _crownAt = new Transform[2];
        private readonly FxPiece _veil, _back, _sheet, _lines, _crest, _foamNear, _foamFar, _puddle;
        private readonly FxPiece[] _crown = new FxPiece[2];
        private readonly FxPiece[] _ring = new FxPiece[3];
        private readonly FxFlecks _drops;

        public override float Seconds => 0.3f + _benchLetGo + Fall + Splash + 0.1f;
        public override Color FloorColour => new Color(0.150f, 0.150f, 0.158f);
        public override Color FloorLine => new Color(0.120f, 0.120f, 0.128f);
        public override float BloomIntensity => 0.30f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            focus = new Vector3(0f, 0.80f, 0f);
            eye = focus + new Vector3(3.4f, 2.3f, -6.2f);
            fov = 32f;
        }

        public FxStudyRafiWall(Transform parent) : this(parent, Look.Bench) { }

        /// <summary>`benchLetGo` and `benchBurst` are only what the bench's own film does (`Sample`): when its wall lets
        /// go, and which way it bursts. A wall in a match is posed by `Pose` and is told both there.</summary>
        public FxStudyRafiWall(Transform parent, Look look, float benchLetGo = Life, float benchBurst = 0f) : base("rafiwall", parent)
        {
            _look = look; _benchLetGo = benchLetGo; _benchBurst = benchBurst;
            float light = look.Light;

            // ---- on the ground: a faint wet patch, the arcs, and the foam either side of the line.
            _puddle = AddGlow("puddle", 0, look.Pale, look.Body, lying: true);
            _puddle.At.localPosition = new Vector3(0f, 0.012f, 0f);
            _puddle.At.localScale = new Vector3(HalfWide * 3f, 1f, 2.4f);
            for (int i = 0; i < _ring.Length; i++)
            {
                _ring[i] = AddFlat("ring-" + i, 1 + i, "water_ring", true, "erode_cloud");
                _ring[i].Colours(look.Deep, look.Body, look.Pale, look.Body).Set("_Add", Mathf.Lerp(0.3f, 0.8f, light)).Set("_Cut", 0.06f, 0.70f).Set("_Stops", 0.30f, 0.70f, 1.0f)
                        .Set("_ErodeTile", 1.3f, 1.3f, i * 0.31f, i * 0.17f).Set("_Soft", 0.25f).Set("_Hue", look.HueDrift, 0f, i * 0.4f, 1f);
                _ring[i].At.localPosition = new Vector3(0f, 0.016f + 0.002f * i, 0f);
            }
            _foot = Group("foot");
            _foamNear = Foam("foam-near", 4, false, 0.00f);
            _foamFar = Foam("foam-far", 5, true, 0.43f);

            // ---- the standing sheet. Everything that stands is under one group, so it surges up as one.
            // ⚠️ KEEP THIS BODY. Shown the first sample (film w2) beside a second with fewer, wider streaks, torn ends and a
            // scalloped crest (w3), the owner said: "rafi use the first". The curtain, its two flowing sheets, the thin fast
            // lines and the foam lip below are the first sample's, number for number.
            _stand = Group("stand");
            _veil = Add("veil", Curtain(0f), FxStudyKit.Ribbon(null), 6, _stand);
            // (No picture: an even faint body. An even white mask reads the body colour everywhere.)
            _veil.Colours(look.Deep, look.Body, look.Body, look.Body).Set("_Cut", 0f, 1f).Set("_Stops", 0.5f, 2f, 3f).Set("_Ends", 0.03f, 0.34f, 0.10f)
                 .Set("_Scroll", 0f, 0f).Set("_Halo", 0f).Set("_Emit", 0f).Set("_Add", light * 0.7f).Set("_Hue", look.HueDrift, 0f, 0.6f, 1f);
            // (`_Sweep` matters only once the wall lets go: while it stands nothing of either sheet is eaten.)
            _back = Add("back", Curtain(-0.07f), FxStudyKit.Ribbon("water_flow", "water_flow"), 7, _stand);
            _back.Colours(look.Deep, Color.Lerp(look.Deep, look.Body, 0.6f), look.Body, look.Deep)
                 .Set("_Cut", 0.05f, 0.75f).Set("_Stops", 0.30f, 0.85f, 1.0f).Set("_Ends", 0.04f, 0.20f, 0.07f).Set("_Sweep", -2f).Set("_Soft", 0.22f)
                 .Set("_Halo", 0f).Set("_Emit", 0f).Set("_Add", light * 0.5f).Set("_Hue", look.HueDrift, 4f, 0.9f, 1f);
            _sheet = Add("sheet", Curtain(0f), FxStudyKit.Ribbon("water_flow", "water_flow"), 8, _stand);
            // The streak's spine reaches the pale colour and no further: white is the lines', the lip's and the foam's.
            _sheet.Colours(look.Deep, look.Body, look.Pale, look.Body)
                  .Set("_Cut", 0.05f, 0.70f).Set("_Stops", 0.30f, 0.80f, 1.0f).Set("_Ends", 0.05f, 0.16f, 0.05f).Set("_Sweep", -2f).Set("_Soft", 0.22f)
                  .Set("_Halo", 0f).Set("_Emit", 0.2f).Set("_Add", light).Set("_Hue", look.HueDrift, 5f, 0.2f, 1f).Set("_Warp", 0.02f, 1.5f, 0.4f, 1.6f);
            _lines = Add("lines", Curtain(0.02f), FxStudyKit.Ribbon("water_lines", "water_flow"), 9, _stand);
            _lines.Colours(look.Body, look.Pale, look.Foam, look.Pale)
                  .Set("_Cut", 0.22f, 0.70f).Set("_Stops", 0.30f, 0.50f, 0.90f).Set("_Ends", 0.05f, 0.14f, 0.06f).Set("_Sweep", 0f).Set("_Soft", 0.20f)
                  .Set("_Halo", 0f).Set("_Emit", 0.5f).Set("_Add", Mathf.Lerp(0.5f, 0.9f, light)).Set("_Warp", 0.02f, 1.5f, 0.4f, 1.6f);

            // ---- the lip: lobes hung from an uneven line along the shoulder, leaning the way the water comes over. It has
            // a group of its own, since when the wall lets go it drops as a body.
            _crestAt = new GameObject("lip") { hideFlags = HideFlags.DontSave }.transform;
            _crestAt.SetParent(_stand, false);
            var knots = new Knot[Crown.Length];
            for (int k = 0; k < knots.Length; k++)
            {
                float along = k / (float)(knots.Length - 1);
                knots[k] = new Knot(new Vector3(Mathf.Lerp(-HalfWide, HalfWide, along), Tall - Crown[k], -0.12f), 0.50f - Crown[k] * 0.8f, -30f + 90f * (Crown[k] - 0.08f));
            }
            _crest = Add("lip", FxStudyKit.Sweep(knots, Vector3.forward, perKnot: 6, across: 6, cup: 0.22f), FxStudyKit.Ribbon("water_foam", "erode_cloud"), 10, _crestAt);
            _crest.Colours(look.Body, look.Pale, look.Foam, look.Pale)
                  .Set("_Tile", 2.4f, 0.94f, 0.2f, 0.02f).Set("_Scroll", 0.10f, 0f).Set("_Cut", 0.10f, 0.50f).Set("_Stops", 0.30f, 0.66f, 0.92f)
                  .Set("_Ends", 0.07f, 0.07f, 0f).Set("_ErodeTile", 2f, 1f).Set("_Soft", 0.15f).Set("_Halo", 0f).Set("_Emit", 0.3f)
                  .Set("_Add", Mathf.Lerp(0.2f, 0.6f, light)).Set("_Warp", 0.035f, 2.0f, 0.9f, 2.4f);

            // ---- the splash crown: big foam lobes standing on the line either side of it, thrown up and out.
            // ⚠️ UNEVEN, FEW, AND NOT WHITE. The first crown was twelve lobes of one height and the brightest thing in the
            // effect: a picket fence. Each side's strip is now a different typed height at each of its seven points, wears
            // about five lobes along the wall, and is the body's colours with only its rims pale.
            for (int i = 0; i < 2; i++)
            {
                _crownAt[i] = Group("crown-" + i);
                _crownAt[i].localPosition = new Vector3(0f, 0.02f, i == 0 ? -0.10f : 0.10f);
                var low = new Knot[CrownHigh.GetLength(1)];
                for (int k = 0; k < low.Length; k++) low[k] = new Knot(new Vector3(Mathf.Lerp(-HalfWide, HalfWide, k / (float)(low.Length - 1)), CrownHigh[i, k] * 0.5f, 0f), CrownHigh[i, k]);
                // (Faced backward, the sweep lays v 0 along the ground and the lobes of the picture upward.)
                _crown[i] = Add("crown-" + i, FxStudyKit.Sweep(low, Vector3.back, perKnot: 4, across: 6), FxStudyKit.Ribbon("water_crest", "erode_cloud"), 11, _crownAt[i]);
                _crown[i].Colours(look.Deep, look.Body, look.Pale, look.Body)
                         .Set("_Tile", 1.1f, 0.94f, 0.31f + i * 0.47f, 0.02f).Set("_Scroll", 0f, 0f).Set("_Cut", 0.10f, 0.50f).Set("_Stops", 0.30f, 0.74f, 0.98f)
                         .Set("_Ends", 0.08f, 0.08f, 0f).Set("_ErodeTile", 2f, 1f, i * 0.4f, 0f).Set("_Soft", 0.18f).Set("_Halo", 0f).Set("_Emit", 0.05f)
                         .Set("_Add", Mathf.Lerp(0.15f, 0.45f, light)).Set("_Hue", look.HueDrift, 0f, 0.3f + i * 0.4f, 1f);
            }

            _drops = AddFlecks("drops", 12, "water_drop");
            _drops.Grid = 2;
            _drops.Piece.Colours(look.Deep, look.Body, look.Foam, look.Body).Set("_Cut", 0.10f, 0.45f).Set("_Stops", 0.30f, 0.70f, 0.95f).Set("_Add", Mathf.Lerp(0.2f, 0.6f, light));
        }

        // The curtain: swept from the ground up and over a shoulder, the full width of the rule, then folded along its
        // width like cloth. u runs from its foot (0) to its far end over the shoulder (1), v across it.
        private static Mesh Curtain(float off)
        {
            float wide = HalfWide * 2f;
            var knots = new[]
            {
                new Knot(new Vector3(0f, 0.00f, 0.00f), wide), new Knot(new Vector3(0f, 0.50f, 0.03f), wide), new Knot(new Vector3(0f, 1.00f, 0.04f), wide),
                new Knot(new Vector3(0f, 1.45f, 0.01f), wide), new Knot(new Vector3(0f, 1.70f, -0.07f), wide), new Knot(new Vector3(0f, Tall - 0.03f, -0.18f), wide),
                new Knot(new Vector3(0f, 1.71f, -0.28f), wide),
            };
            // (The way its flat side faces is leaned back and down, so it holds one side all the way over the shoulder: faced
            // straight ahead, the sheet turns itself inside out where it starts to come down.)
            var mesh = FxStudyKit.Sweep(knots, new Vector3(0f, -0.91f, -0.42f), perKnot: 6, across: Columns);
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = vertices[i];
                p.z += off + 0.050f * Mathf.Sin(p.x * 2.3f + 0.7f) + 0.028f * Mathf.Sin(p.x * 5.1f + 2.0f + off * 20f);
                vertices[i] = p;
            }
            mesh.vertices = vertices; mesh.RecalculateBounds();
            return mesh;
        }

        // One side's foam: a strip lying on the ground with its hanging edge on the wall's line and its lobes outward.
        private FxPiece Foam(string name, int order, bool far, float slide)
        {
            const float reach = 0.46f;
            var knots = new Knot[5];
            for (int k = 0; k < knots.Length; k++)
                knots[k] = new Knot(new Vector3(Mathf.Lerp(-HalfWide, HalfWide, k / 4f), 0.02f, far ? reach * 0.5f : -reach * 0.5f), reach);
            // (The sweep lays v 0 on one side of its line; facing it up or down chooses which.)
            var piece = Add(name, FxStudyKit.Sweep(knots, far ? Vector3.up : Vector3.down, perKnot: 4, across: 4), FxStudyKit.Ribbon("water_foam", "erode_cloud"), order, _foot);
            piece.Colours(_look.Body, _look.Pale, _look.Foam, _look.Pale)
                 .Set("_Tile", 3f, 0.94f, slide, 0.02f).Set("_Scroll", far ? 0.06f : -0.05f, 0f).Set("_Cut", 0.10f, 0.50f).Set("_Stops", 0.30f, 0.66f, 0.92f)
                 .Set("_Ends", 0.06f, 0.06f, 0f).Set("_ErodeTile", 2f, 1f, slide, 0f).Set("_Soft", 0.18f).Set("_Halo", 0f).Set("_Emit", 0.2f)
                 .Set("_Add", Mathf.Lerp(0.15f, 0.5f, _look.Light));
            return piece;
        }

        // On the bench: a beat of nothing, then the wall, which lets go when the bench was told it does.
        public override void Sample(float age, Vector3 eye) => Pose(age - 0.3f, eye, _benchLetGo, _benchBurst);

        /// <summary>
        /// Poses the wall `t` seconds after its cast. `letGo` is when it stops standing: the moment a slipper broke it, or
        /// `Life` for one nothing broke. It has landed `Fall` later and its splash runs `Splash` after that, so it is
        /// drawn for `Fall + Splash` AFTER the rule has let go of it. `burst` is which side the landing is thrown to:
        /// 0 both alike, +1 forward of the wall (the far side from its caster), -1 back.
        /// </summary>
        public void Pose(float t, Vector3 eye, float letGo, float burst = 0f)
        {
            float age = Mathf.Max(0f, t);
            bool live = t >= 0f;
            // ⚠️ THE GATHER, THEN THE SURGE. For 0.15 s there is no wall: the foam is wide on the ground and is drawn in to the
            // line, and an arc runs INWARD. Then the sheet surges up in a tenth of a second and is whole when the rule
            // begins to block. Its overshoot is kept inside the rule's height: it arrives, sags a little and recovers.
            float surge = FxStudyKit.EaseOut(FxStudyKit.Run(t, 0.15f, Gather));
            float sag = 0.07f * Mathf.Sin(Mathf.PI * FxStudyKit.Run(t, Gather, Gather + 0.30f));
            _stand.localScale = new Vector3(1f, Mathf.Max(0.02f, surge - sag), 1f);

            // ---- the fall. `fell` is how far a dropped thing has fallen since the wall let go (m): slow, then fast.
            float since = t - letGo, k = FxStudyKit.Run(since, 0f, Fall), fell = Tall * k * k, landed = since - Fall;
            bool standing = live && surge > 0f && landed < 0f;
            // Eaten from the top down by its own noise: whole, then tongues, and the last of them reach the ground.
            float eaten = Mathf.Pow(k, 1.7f);
            Shed(_back, 0.85f, 1.7f, 0.37f, 0.21f, -0.50f, -0.015f, 0.85f, 1.7f, 0.37f, 0.21f, -0.50f, -0.015f, fell);
            Shed(_sheet, 1.2f, 2.5f, 0f, 0f, -0.85f, 0.02f, 1.2f, 2.5f, 0f, 0f, -0.85f, 0.02f, fell);
            Shed(_lines, 1.2f, 2.5f, 0.3f, 0.1f, -1.15f, 0f, 0.6f, 1.2f, 0.5f, 0.2f, -0.30f, 0f, fell);
            _veil.Frame(age, standing ? 0.20f * (1f - FxStudyKit.Run(k, 0f, 0.30f)) : 0f);
            _back.Frame(age, standing ? 0.60f : 0f, eaten);
            _sheet.Frame(age, standing ? 0.85f : 0f, eaten);
            _lines.Frame(age, standing ? 0.80f : 0f, Mathf.Lerp(0.40f, 1f, k));
            // The lip drops as a body, breaking up as it goes.
            _crestAt.localPosition = new Vector3(0f, -Mathf.Min(fell, Tall - 0.3f), 0f);
            _crest.Frame(age, standing ? 0.95f * FxStudyKit.Run(t, 0.19f, Gather + 0.03f) : 0f, 0.85f * k);

            // ---- the foot: wide and drawn in as it gathers, breathing while it stands, thrown wide when the sheet lands.
            float drawnIn = FxStudyKit.Ease(FxStudyKit.Run(t, 0f, 0.17f)), thrown = FxStudyKit.EaseOut(FxStudyKit.Run(landed, 0f, 0.40f));
            float gone = FxStudyKit.Run(landed, 0.10f, Splash);
            _foot.localScale = new Vector3(1f, 1f, Mathf.Lerp(2.6f, 1f, drawnIn) + 0.10f * Mathf.Sin(age * 3.1f) + 1.9f * thrown);
            float foamEaten = Mathf.Max(Mathf.Lerp(0.55f, 0.12f, drawnIn), gone);
            _foamNear.Frame(age, live ? 0.90f : 0f, foamEaten);
            _foamFar.Frame(age, live ? 0.90f : 0f, foamEaten);
            _puddle.Frame(age, live ? 0.10f * drawnIn * (1f - gone) : 0f);

            // ---- the arcs: one drawn in as it gathers, two thrown out as it lands. None while it stands.
            Arc(_ring[0], age, t, 0.30f, 1.20f, 0.74f, 0.40f);
            Arc(_ring[1], age, landed, 0.55f, 0.80f, 1.30f, 0.50f);
            Arc(_ring[2], age, landed - 0.10f, 0.60f, 0.95f, 1.60f, 0.32f);

            // ---- the crown: thrown up and out where the sheet lands, and it falls back. More of it on the side it bursts to.
            // (It starts a little before the last tongues are down: begun exactly as they landed, one frame had neither.)
            for (int i = 0; i < 2; i++)
            {
                float q = (landed + 0.09f) / 0.52f, side = i == 0 ? -1f : 1f, share = Mathf.Clamp(1f + 0.7f * burst * side, 0.25f, 1.7f);
                if (q < 0f || q >= 1f) { _crown[i].Frame(age, 0f); continue; }
                // Up fast and down under its own weight, and leaning further out the whole time.
                float high = 4f * q * (1f - q) * 0.95f * share + 0.06f;
                _crownAt[i].localRotation = Quaternion.Euler(side * Mathf.Lerp(8f, 42f, q), 0f, 0f);
                _crownAt[i].localScale = new Vector3(1f, high, 1f);
                _crown[i].Frame(age, 0.90f, FxStudyKit.Run(q, 0.45f, 1f) * 0.95f);
            }

            _drops.Begin(eye);
            foreach (Drop d in Born) Throw(d, t - 0.17f - d.After, 0f);
            foreach (Drop d in Landed) Throw(d, landed - d.After, burst);
            _drops.End(age);
        }

        // Lays one sheet's picture, and the map that eats it, where they are now: each is run down by the clock (the shader
        // does that, so many sheets a second) and slid further down by how far a dropped thing has fallen.
        private static void Shed(FxPiece piece, float along, float across, float slideAlong, float slideAcross, float rate, float drift,
                                 float eatAlong, float eatAcross, float eatSlideAlong, float eatSlideAcross, float eatRate, float eatDrift, float fell)
        {
            float share = fell / Run_;
            piece.Set("_Tile", along, across, slideAlong + share * along, slideAcross).Set("_Scroll", rate, drift)
                 .Set("_ErodeTile", eatAlong, eatAcross, eatSlideAlong + share * eatAlong, eatSlideAcross).Set("_ErodeScroll", eatRate, eatDrift);
        }

        // An arc `at` seconds into its life of `life`: it runs from one size to another, slowing, and is eaten as it goes.
        private void Arc(FxPiece ring, float age, float at, float life, float from, float to, float strength)
        {
            float k = at / life;
            if (at < 0f || k >= 1f) { ring.Frame(age, 0f); return; }
            float size = Mathf.Lerp(from, to, FxStudyKit.EaseOut(k)), broad = HalfWide * 2f + 1.7f;
            ring.At.localScale = new Vector3(broad * size, 1f, broad / 3.25f * size * 1.25f);
            ring.Frame(age, strength * Mathf.Sin(Mathf.PI * Mathf.Sqrt(k)), 0.25f + 0.70f * k);
        }

        // A drop `at` seconds after it was thrown: it flies its arc under its own weight and is gone where it meets the
        // ground. `burst` throws the drops of one side harder and the other side's softer.
        private void Throw(Drop d, float at, float burst)
        {
            if (at < 0f) return;
            Vector3 thrown = d.Throw;
            thrown.z *= Mathf.Clamp(1f + 0.6f * burst * Mathf.Sign(thrown.z), 0.3f, 1.6f);
            Vector3 going = thrown + Vector3.down * (9.8f * at), here = d.From + thrown * at + Vector3.down * (4.9f * at * at);
            if (here.y < 0.03f) return;
            // Longer the faster it goes, as a drop is; it thins as it nears the ground.
            _drops.Add(here, going, d.Size * Mathf.Lerp(1.4f, 2.2f, Mathf.Clamp01(going.magnitude / 6f)), d.Size, Mathf.Clamp01(here.y / 0.25f + 0.35f), d.Cell, Color.white);
        }
    }
}
