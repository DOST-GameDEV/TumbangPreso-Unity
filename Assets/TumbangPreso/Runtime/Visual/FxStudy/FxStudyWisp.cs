using UnityEngine;
using Knot = TumbangPreso.Visual.FxStudyKit.Knot;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// STUDY B, THE WISPY EXPLOSION. A ball of light bursts in a shallow dark dish and throws up big two-tone wisps that
    /// turn about it like the blades of a turbine and tear away along their length. 2.2 seconds, in the reference's five
    /// layers, each of which is a different KIND of thing, which is why it reads as rich and not as one shape fading:
    ///
    ///   1. LIGHT BALL. A white heart, cyan where it turns away, in a dark navy dish with a lit lip; a bubble round it
    ///      for its first quarter second; thin white arcs spinning about it as it goes. All of its glow is sprites
    ///      (`AddGlow`): a hot one on the ball, a wide cyan one behind it, a pool on the floor.
    ///   2. WISP MESHES. Ribbons swept along typed paths (`FxStudyKit.Sweep`), each DRAWN along its path head first and
    ///      then eaten from its root by the streaked erosion map. Two tones, and never mixed in one piece: the dark ones
    ///      are saturated ultramarine and cover what is behind them, the pale ones are cyan-white and thin.
    ///   3. SMOKE PLUMES. The two biggest dark wisps: one tall and thin that climbs out of the frame, one low and wide
    ///      that rolls away to the side.
    ///   4. IMPACTS. Flat BLACK ink splats thrown out across the floor, and two dark tufts beside the dish. They are
    ///      there for contrast: the light is only blinding because something beside it is black.
    ///   5. SPARKLES. Teardrop flecks: a star of them on the floor in the first frames, a ring thrown outward, a few
    ///      rising with the wisps.
    ///
    /// ⚠️ EVERY PATH IS TYPED. The seven wisps and five flames below are each written out knot by knot (the owner's
    /// standing rule for builders: "manually do each part ... dont js auto generate looping shit"). Only the sparks are
    /// laid out by a fixed hash, since a spark has no shape to design.
    /// </summary>
    public sealed class FxStudyWisp : FxStudyEffect
    {
        private static readonly Color Ultramarine = new Color(0.07f, 0.03f, 0.52f), UltraDeep = new Color(0.05f, 0.02f, 0.40f),
                                      UltraLit = new Color(0.10f, 0.06f, 0.66f), Pale = new Color(0.44f, 0.58f, 0.74f),
                                      PaleDeep = new Color(0.30f, 0.45f, 0.70f), PaleHot = new Color(0.70f, 0.80f, 0.90f),
                                      Cyan = new Color(0.30f, 0.78f, 1f), Ink = new Color(0.012f, 0.006f, 0.03f);

        // One wisp and its timing: when it starts to be drawn, how long the drawing takes, when its root starts to be
        // eaten and when it is gone.
        // `Speed` is how fast its picture runs along it when it is born (pictures a second), `Tile` and `Phase` where that
        // picture sits: see `Draw`.
        // (Public, with `DressShell`, `DressDishStreaks` and `Draw`, since the whirlwind study is built of these same
        // shells: it calls them here so that there is one copy of how a wisp is dressed and drawn, and it is this one.)
        public struct Wisp { public FxPiece Piece; public float Start, Grow, EatFrom, EatTo, Speed, Tile, Phase; }
        private struct Billow { public FxPiece Piece; public Vector3 At, Size; public float Start; }
        private readonly Billow[] _billow = new Billow[6];
        private readonly FxSmoke[] _smoke = new FxSmoke[2];
        // How fast each wisp's picture runs along it, typed, so no two blades slide at the same rate.
        private static readonly float[] WispSpeed = { 1.9f, 1.4f, 2.3f, 2.0f, 1.6f, 2.4f, 1.5f };
        private struct Splat { public FxPiece Piece; public Vector3 At; public float Size, Turn; }

        private readonly Transform _wisps, _arcs;
        private readonly Wisp[] _wisp = new Wisp[7];
        private readonly Wisp[] _flame = new Wisp[5];
        private readonly Wisp[] _swoosh = new Wisp[2];
        private readonly Splat[] _splat = new Splat[6];
        private readonly FxPiece[] _tuft = new FxPiece[2];
        private readonly FxPiece[] _arc = new FxPiece[5];
        private readonly FxPiece _pool, _halo, _hot, _flash, _ball, _swirl, _bubble, _dish, _dishStreaks, _crown, _flare;
        private readonly FxPiece[] _thrown = new FxPiece[3];
        // The sparks' colours: the reference's are cyan, aqua, white and violet, never one colour.
        private static readonly Color[] Tones = { new Color(0.52f, 0.90f, 1f), new Color(0.74f, 0.97f, 0.97f), Color.white, new Color(0.72f, 0.66f, 1f),
                                                  new Color(0.52f, 0.92f, 1f), new Color(0.86f, 0.90f, 1f), new Color(0.46f, 0.60f, 1f) };
        private static Color Tone(int n) => Tones[((n % Tones.Length) + Tones.Length) % Tones.Length];
        private readonly FxFlecks _flecks, _hairs;

        public override float Seconds => 2.2f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            // ⚠️ MEASURED, NOT GUESSED (2026-10-10). Enlarged, the reference's dish lip is an ellipse about a fifth as tall as
            // it is wide: its camera looks down only about eleven degrees, nearly side-on, which is why its dish is a
            // bowl in profile and its flecks lie in a thin band. The first sixteen films looked down seventeen degrees and
            // everything on the floor spread into a flat flower.
            eye = new Vector3(0f, 1.93f, -5.30f); focus = new Vector3(0f, 0.9f, 0f); fov = 30f;
        }

        private readonly bool _showShells;

        public FxStudyWisp(Transform parent, bool showShells = false) : base("wisp", parent)
        {
            _showShells = showShells;
            if (showShells) { _wisps = Group("wisps"); BuildShells(); return; }
            // ---- the floor: the pool of light, and the ink thrown across it.
            _pool = AddGlow("pool", 0, new Color(0.90f, 0.96f, 1f), new Color(0.30f, 0.42f, 0.58f), lying: true);
            _pool.At.localPosition = new Vector3(0f, 0.012f, 0f);

            // The violet flare that spreads on the floor under the opening: streaks thrown outward, light only.
            _flare = AddFlat("flare", 1, "erode_radial", lying: true, erode: "erode_cloud");
            _flare.At.localPosition = new Vector3(0f, 0.015f, 0f);
            _flare.Colours(new Color(0.12f, 0.10f, 0.72f), new Color(0.26f, 0.24f, 0.98f), new Color(0.68f, 0.70f, 1f), new Color(0.2f, 0.2f, 0.9f))
                  .Set("_Cut", 0.50f, 0.72f).Set("_ErodeTile", 1.3f, 1.3f, 0.2f, 0.6f).Set("_Soft", 0.2f);
            // The blades thrown straight up in the first tenth of a second: long, streaked, see-through, gone at once.
            // (where it leaves from, which way it leans, how long, how wide)
            _thrown[0] = BuildThrown(0, -0.30f, -0.22f, 1.5f, 1.20f);
            _thrown[1] = BuildThrown(1, 0.34f, 0.26f, 1.3f, 1.00f);
            _thrown[2] = BuildThrown(2, 0.02f, 0.04f, 1.1f, 0.78f);

            // ⚠️ THE DARK SMOKE IS VOLUMES. The owner: "i think the dark blue smoke is actually volumetric/3d". It had been
            // flat black splats lying on the floor. These are lumpy closed shapes piled against the dish on both sides,
            // dark navy with a blue-violet rim where they turn away, and they are eaten in holes, not faded. The flat
            // splats stay, smaller, as the shadow under them. (where, size, when it starts, its own unevenness)
            // ⚠️ AND THE VOLUMES ARE CLUSTERS OF BALLS NOW (`FxSmoke`): "the smoke again is not just one smooth shape. its
            // more like multiple rough spheres". Two masses, low at the bowl's sides and front, dark navy with a
            // blue-violet lit tone. Two draw calls.
            var navyLit = new Color(0.17f, 0.15f, 0.56f); var navyMid = new Color(0.055f, 0.05f, 0.26f); var navyDark = new Color(0.012f, 0.012f, 0.075f);
            _smoke[0] = AddSmoke("smoke-left", 11, 30, navyLit, navyMid, navyDark);
            _smoke[1] = AddSmoke("smoke-right", 12, 30, navyLit, navyMid, navyDark);

            // (where, size, turn, which of the four splats on the sheet)
            BuildSplat(0, new Vector3(-1.50f, 0f, 0.05f), 1.95f, 20f, 1);
            BuildSplat(1, new Vector3(1.50f, 0f, 0.20f), 1.80f, 200f, 3);
            BuildSplat(2, new Vector3(-0.95f, 0f, -1.15f), 1.15f, 95f, 2);
            BuildSplat(3, new Vector3(0.90f, 0f, -1.20f), 1.10f, 310f, 2);
            BuildSplat(4, new Vector3(-2.40f, 0f, 0.55f), 1.20f, 150f, 0);
            BuildSplat(5, new Vector3(2.45f, 0f, 0.60f), 1.40f, 260f, 0);

            // ---- the dish, and the pale crown that stands on its lip.
            _dish = Add("dish", FxStudyKit.Revolve(new[] { new Vector2(0f, 0.03f), new Vector2(0.45f, 0.05f), new Vector2(0.78f, 0.15f),
                                                           new Vector2(0.95f, 0.28f), new Vector2(1.02f, 0.40f) }), FxStudyKit.Orb(), 4);
            // ⚠️ THE DISH'S LIP IS WAVED. The owner, tracing ours as a clean ellipse beside the reference's: "that ocean blue
            // disc needs a waved edge btw". A bowl turned on a lathe is a perfect ring from every side. So the same bowl
            // is pushed out of true by a slow unevenness (`FxStudyKit.Lump`: the references' "deform w/ geo noise"): its
            // lip rises and dips in a few big waves and bulges unevenly, it turns slowly so the waves move, and streaks
            // of pale water run round it on a second skin.
            var bowl = new[] { new Vector2(0.02f, 0.03f), new Vector2(0.45f, 0.05f), new Vector2(0.78f, 0.15f), new Vector2(0.95f, 0.28f), new Vector2(1.02f, 0.40f) };
            var waved = _dish.Go.GetComponent<MeshFilter>();
            if (waved != null) waved.sharedMesh = FxStudyKit.Lump(bowl, 1.7f, 0.30f, 40, 0.07f);
            _dishStreaks = Add("dish-streaks", FxStudyKit.Lump(bowl, 1.7f, 0.30f, 40, 0.07f), FxStudyKit.Ribbon("erode_streak", "erode_cloud"), 5);
            DressDishStreaks(_dishStreaks);
            _dish.Set("_Core", new Color(0.03f, 0.07f, 0.42f)).Set("_Rim", new Color(0.06f, 0.15f, 0.68f)).Set("_CoreAlpha", 1f).Set("_RimAlpha", 1f)
                 .Set("_Power", 1.5f).Set("_Band", 1f, 0.14f, 0.9f).Set("_BandColor", new Color(0.30f, 0.58f, 1f));

            _crown = Add("crown", FxStudyKit.Revolve(new[] { new Vector2(1.00f, 0.22f), new Vector2(1.06f, 0.42f), new Vector2(1.16f, 0.62f),
                                                             new Vector2(1.30f, 0.78f) }, 56), FxStudyKit.Ribbon("trail_bands"), 6);
            _crown.Colours(PaleDeep, Pale, PaleHot, Cyan).Set("_Tile", 3f, 1f).Set("_Scroll", 0.6f, 0f).Set("_Cut", 0.05f, 0.30f)
                  .Set("_Stops", 0.35f, 0.60f, 0.92f).Set("_Ends", 0f, 0f, 0.10f).Set("_ErodeTile", 1f, 1f).Set("_ErodeScroll", 0.3f, 0f).Set("_Soft", 0.12f)
                  .Set("_Halo", 0.25f).Set("_Add", 0.35f);

            // Two pale sweeps of water thrown round the dish, one from each side, rising as they go: what the reference has
            // where a ring would be. (where it starts round the dish, how far round it goes, how high it ends, how wide)
            BuildSwoosh(0, 160f, 86f, 0.55f, 0.48f, 0.26f);
            BuildSwoosh(1, 20f, -82f, 0.45f, 0.42f, 0.34f);

            _tuft[0] = BuildTuft(0, new Vector3(-1.45f, 0.50f, 0.25f), 1.05f, 3);
            _tuft[1] = BuildTuft(1, new Vector3(1.48f, 0.44f, 0.25f), 0.92f, 1);

            // ---- light behind the ball.
            _halo = AddGlow("halo", 5, new Color(0.85f, 0.95f, 1f), new Color(0.40f, 0.62f, 0.95f));
            _halo.At.localPosition = new Vector3(0f, 0.42f, 0.1f);

            // ---- the first flames: five violet blades thrown up and to the left in the first quarter second.
            var violet = (deep: new Color(0.26f, 0.18f, 0.80f), body: new Color(0.52f, 0.44f, 1f), hot: new Color(0.90f, 0.88f, 1f));
            BuildFlame(0, violet, 0.04f, new[] { new Knot(-0.10f, 0.45f, 0f, 0.12f), new Knot(-0.45f, 1.00f, 0f, 0.10f), new Knot(-0.78f, 1.55f, 0f, 0.01f) });
            BuildFlame(1, violet, 0.06f, new[] { new Knot(0.02f, 0.55f, 0f, 0.11f), new Knot(-0.14f, 1.05f, 0f, 0.08f), new Knot(-0.24f, 1.45f, 0f, 0.01f) });
            BuildFlame(2, violet, 0.05f, new[] { new Knot(-0.30f, 0.40f, 0f, 0.10f), new Knot(-0.82f, 0.80f, 0f, 0.08f), new Knot(-1.22f, 1.10f, 0f, 0.01f) });
            BuildFlame(3, violet, 0.08f, new[] { new Knot(0.20f, 0.55f, 0f, 0.09f), new Knot(0.40f, 0.98f, 0f, 0.06f), new Knot(0.62f, 1.26f, 0f, 0.01f) });
            BuildFlame(4, violet, 0.07f, new[] { new Knot(-0.50f, 0.30f, 0f, 0.08f), new Knot(-1.00f, 0.50f, 0f, 0.06f), new Knot(-1.40f, 0.56f, 0f, 0.01f) });

            // ---- the wisps. One group, so the whole turbine can swell and turn as one.
            _wisps = Group("wisps");
            BuildShells();

            // ---- the ball, and the bubble round it.
            // Behind the wisps: in the reference the dark blades cross IN FRONT of the light, which is what gives it depth.
            _ball = Add("ball", FxStudyKit.Ball(), FxStudyKit.Orb(), 7);
            _ball.At.localPosition = new Vector3(0f, 0.40f, 0f);
            _ball.Set("_Emit", 0.8f).Set("_Core", Color.white).Set("_Rim", new Color(0.62f, 0.90f, 1f)).Set("_Power", 2.6f).Set("_Gain", 1.2f).Set("_RimAlpha", 0.10f);
            // ⚠️ THE BUBBLE IS TEXTURED. The owner, of the clean dome it used to be: "transparent outer white orb here is also
            // untextured in your version". It wears the curtain mask, scrolling round it, see-through and uneven, white
            // through pale cyan to lilac, and it is eaten away along the streak map; it does not fade.
            _bubble = Add("bubble", FxStudyKit.Ball(32, 12, 0f, 1f), FxStudyKit.Ribbon("trail_smoke"), 9);
            _bubble.At.localPosition = new Vector3(0f, 0.26f, 0f);
            _bubble.Colours(new Color(0.58f, 0.54f, 0.96f), new Color(0.72f, 0.88f, 1f), Color.white, new Color(0.6f, 0.75f, 1f))
                   .Set("_Tile", 2f, 1.1f, 0.2f, -0.05f).Set("_Scroll", 0.9f, 0.25f).Set("_Cut", 0.12f, 1.1f).Set("_Stops", 0.35f, 0.70f, 0.98f)
                   .Set("_Ends", 0f, 0f, 0f).Set("_ErodeTile", 2f, 1f).Set("_ErodeScroll", 0.6f, 0.1f).Set("_Soft", 0.15f).Set("_Burn", 0.4f)
                   .Set("_Halo", 0.25f).Set("_Add", 0.75f).Set("_Hue", 12f, 0f, 0.3f, 1f);
            // The swirl: a shell just over the ball whose picture is slid round it as it climbs, wearing the two ribbons: a
            // spiral band, scrolled (the references' "sphere spiral" with a scrolled alpha). Without it the ball is a flat disc.
            _swirl = Add("swirl", FxStudyKit.Ball(32, 16, twist: 1.0f), FxStudyKit.Ribbon("trail_bands"), 8);
            _swirl.At.localPosition = new Vector3(0f, 0.40f, 0f);
            var streak = new Color(0.45f, 0.68f, 0.95f);
            _swirl.Colours(streak, streak, new Color(0.30f, 0.50f, 0.90f), streak).Set("_Tile", 1f, 1f).Set("_Scroll", 1.4f, 0f).Set("_Cut", 0.35f, 1.10f)
                  .Set("_Ends", 0f, 0f, 0.18f).Set("_Halo", 0f).Set("_Add", 0f);
            _hot = AddGlow("hot", 9, Color.white, new Color(0.84f, 0.91f, 1f));
            _hot.At.localPosition = new Vector3(0f, 0.42f, -0.1f);
            _flash = AddGlow("flash", 23, Color.white, new Color(0.86f, 0.92f, 1f));
            _flash.At.localPosition = new Vector3(0f, 0.45f, -0.2f);

            // ---- thin white arcs spinning about the ball as it goes out. (radius, height, tilt, where it starts, how far round)
            _arcs = Group("arcs");
            _arcs.localPosition = new Vector3(0f, 0.5f, 0f);
            _arc[0] = BuildArc(0, 0.72f, 0.05f, 12f, 20f, 150f);
            _arc[1] = BuildArc(1, 0.92f, -0.10f, -18f, 170f, 120f);
            _arc[2] = BuildArc(2, 1.10f, 0.12f, 8f, 250f, 160f);
            _arc[3] = BuildArc(3, 0.58f, 0.22f, -30f, 60f, 110f);
            _arc[4] = BuildArc(4, 1.28f, -0.20f, 20f, 330f, 100f);

            _flecks = AddFlecks("flecks", 30, "fleck_sheet");
            _flecks.Grid = 4;
            _flecks.Piece.Colours(new Color(0.20f, 0.65f, 0.90f), new Color(0.55f, 0.90f, 1f), Color.white, Cyan).Set("_Gain", 0.95f).Set("_Stops", 0.38f, 0.62f, 0.96f).Set("_Cut", 0.10f, 0.75f);
            // The violet hairs of the first quarter second: many, thin, thrown up and back off the ball.
            _hairs = AddFlecks("hairs", 6);
            _hairs.Piece.Colours(new Color(0.08f, 0.12f, 0.66f), new Color(0.22f, 0.32f, 0.98f), new Color(0.62f, 0.74f, 1f), new Color(0.2f, 0.3f, 0.95f)).Set("_Add", 0.7f);
        }

        private void BuildSplat(int index, Vector3 at, float size, float turn, int which)
        {
            // The sheet's quarters, counted from its lower left: 0 is the scatter of specks, 1 to 3 are the three big blots.
            var part = new Rect((which % 2) * 0.5f, (which / 2) * 0.5f, 0.5f, 0.5f);
            var piece = Add("splat-" + index, FxStudyKit.Square(true, part), FxStudyKit.Ribbon("ink_splats", "erode_blobs"), 2);
            // Ink is flat and hard: one colour, a cut edge, and it covers the floor's light.
            piece.Colours(Ink, Ink, Ink, Ink).Set("_Scroll", 0f, 0f).Set("_Ends", 0f, 0f, 0f).Set("_Cut", 0.42f, 0.52f).Set("_Halo", 0f).Set("_Add", 0f)
                 .Set("_ErodeTile", 0.9f, 0.9f, index * 0.31f, index * 0.17f).Set("_Soft", 0.04f);
            _splat[index] = new Splat { Piece = piece, At = at, Size = size, Turn = turn };
        }

        // ⚠️ THE WISPS ARE SHELLS (2026-10-10). The owner: "i think most of the wisps are done using textured spheres, does
        // ours do that too?" It did not: each wisp was a narrow swept ribbon whose outline WAS the wisp, which is why they
        // held one shape for life and could only be wiped on and off. Each is now a broad curved sheet standing round the
        // dish (`FxStudyKit.Shell`), several times wider than the stroke that is seen, and the stroke is the streak map
        // run over it and cut hard: it travels, it is eaten in cloud holes, and the sheet under it bends a little.
        // Every sheet is typed: its profile from foot to far end as (radius, height), where round the dish it stands
        // (270 is toward the camera), how wide it is round the dish, and how far it winds round by its far end.
        // (Both faces of a sheet are drawn, so the references' second, flipped copy of each is not needed.)
        /// <summary>The streaks of pale water that run round the dish: how that skin is dressed.</summary>
        public static FxPiece DressDishStreaks(FxPiece piece)
        {
            return piece.Colours(new Color(0.10f, 0.26f, 0.80f), new Color(0.32f, 0.62f, 1f), new Color(0.78f, 0.92f, 1f), new Color(0.2f, 0.5f, 1f))
                        .Set("_Tile", 2f, 1f).Set("_Scroll", 0.9f, 0f).Set("_Cut", 0.34f, 0.44f).Set("_Stops", 0.46f, 0.70f, 0.96f).Set("_Ends", 0f, 0f, 0.10f)
                        .Set("_ErodeTile", 2f, 1f).Set("_ErodeScroll", 0.3f, 0f).Set("_Soft", 0.12f).Set("_Halo", 0.1f).Set("_Add", 0.35f);
        }

        private void BuildShells()
        {
            // 0. The tall cupped sheet the plume runs up: a bar that leaves the frame.
            BuildShell(0, true, 0.06f, 0.24f, 0.45f, 1.05f, 1.86f, 12, new[] { new Vector2(0.42f, 0.05f), new Vector2(0.58f, 1.0f), new Vector2(0.52f, 2.2f), new Vector2(0.38f, 3.4f), new Vector2(0.22f, 4.5f) }, 238f, 62f, 28f);
            // ⚠️ THE TALL STROKE LIES ON A DOME. The owner, tracing ours as a straight bar beside the reference's line, which
            // climbs from the floor on the left and curves over the top: "this final wisp is still the same thing on a sphere
            // btw". So its sheet is not an upright cupped strip any more: it is an arch on an egg round the ball
            // (`FxStudyKit.Arch`), from the floor on the left, over the crown and partway down the right. The streak
            // map runs along it, so the stroke travels up and over; the group's swelling lifts the dome away as it goes.
            var arch = _wisp[0].Piece.Go.GetComponent<MeshFilter>();
            if (arch != null) arch.sharedMesh = FxStudyKit.Arch(new Vector3(0f, 0.15f, 0.05f), new Vector3(1.25f, 3.3f, 1.25f), 96f, -62f, 36f, 14f);
            // The two pale sheets arch over the same dome, the other way and wider out, so the three cross as a peak.
            // 1. The low wide petal rolling away to the right: the fat lens.
            BuildShell(1, true, 0.08f, 0.60f, 0.45f, 1.10f, 1.88f, 13, new[] { new Vector2(0.40f, 0.15f), new Vector2(1.10f, 0.85f), new Vector2(1.80f, 1.30f), new Vector2(2.30f, 1.05f), new Vector2(2.60f, 0.45f) }, 322f, 66f, -22f);
            // 2. A pale petal thrown up on the left, crossing in front of the plume.
            BuildShell(2, false, 0.58f, 0.50f, 0.50f, 1.25f, 1.70f, 15, new[] { new Vector2(0.50f, 0.30f), new Vector2(1.20f, 1.05f), new Vector2(1.70f, 1.55f), new Vector2(2.05f, 1.45f) }, 214f, 64f, 52f);
            // 3. A pale tall sheet on the right: with the plume it makes the peak.
            BuildShell(3, false, 0.60f, 0.42f, 0.50f, 1.20f, 1.70f, 16, new[] { new Vector2(0.30f, 0.40f), new Vector2(0.60f, 1.40f), new Vector2(0.90f, 2.30f), new Vector2(1.00f, 3.10f) }, 322f, 52f, -58f);
            // 4. A dark petal behind, low on the left.
            BuildShell(4, true, 0.12f, 0.80f, 0.40f, 1.10f, 1.86f, 10, new[] { new Vector2(0.40f, 0.20f), new Vector2(0.95f, 0.80f), new Vector2(1.35f, 1.05f), new Vector2(1.60f, 0.75f) }, 150f, 60f, 30f);
            // 5. A small pale petal low on the left.
            BuildShell(5, false, 0.60f, 0.32f, 0.40f, 0.90f, 1.45f, 17, new[] { new Vector2(0.90f, 0.50f), new Vector2(1.40f, 0.95f), new Vector2(1.90f, 1.20f) }, 196f, 44f, 20f);
            if (_wisp[3].Piece != null)
            {
                var pale = _wisp[3].Piece.Go.GetComponent<MeshFilter>();
                if (pale != null) pale.sharedMesh = FxStudyKit.Arch(new Vector3(0f, 0.15f, 0f), new Vector3(1.6f, 2.6f, 1.6f), -92f, 40f, 30f, -18f);
            }
            // 6. The dark crescent standing off to the right.
            BuildShell(6, true, 0.12f, 0.84f, 0.38f, 1.10f, 1.86f, 11, new[] { new Vector2(1.80f, 0.20f), new Vector2(2.10f, 0.80f), new Vector2(2.02f, 1.45f), new Vector2(1.80f, 1.80f) }, 350f, 24f, 16f);
        }

        private void BuildShell(int index, bool dark, float cut, float start, float grow, float eatFrom, float eatTo, int order, Vector2[] profile, float centre, float span, float twist)
        {
            bool plain = _showShells;
            var piece = Add("shell-" + index, FxStudyKit.Shell(profile, centre, span, twist), plain ? FxStudyKit.Ribbon(null) : FxStudyKit.Ribbon("erode_streak", "erode_cloud"), order, _wisps);
            if (plain)
            {
                // The structure check: every sheet whole and flat, each in its own grey, shaded across its width.
                var grey = Color.Lerp(new Color(0.25f, 0.30f, 0.55f), new Color(0.85f, 0.90f, 1f), index / 6f);
                piece.Colours(grey * 0.5f, grey, grey, grey).Set("_Cut", 0f, 0.01f).Set("_Ends", 0f, 0f, 0f).Set("_Scroll", 0f, 0f).Set("_Shade", 0.7f).Set("_Add", 0f).Set("_Halo", 0f);
                _wisp[index] = new Wisp { Piece = piece, Start = 0f, Grow = 0.01f, EatFrom = 90f, EatTo = 99f };
                return;
            }
            DressShell(piece, index, dark, cut);
            _wisp[index] = new Wisp { Piece = piece, Start = start, Grow = grow, EatFrom = eatFrom, EatTo = eatTo, Speed = WispSpeed[index % WispSpeed.Length] * 0.6f, Tile = 1.0f, Phase = index * 0.31f };
        }

        /// <summary>How a wisp's shell is dressed: one of the two tones (dark covers, pale is thin light), the streak map
        /// cut hard at `cut`, its borders faded, the cloud eating it. `index` only seeds where its pictures sit.</summary>
        public static FxPiece DressShell(FxPiece piece, int index, bool dark, float cut)
        {
            if (dark)
                piece.Colours(UltraDeep, Ultramarine, UltraLit, Ultramarine).Set("_Shade", 0.40f).Set("_Add", 0f).Set("_Halo", 0f).Set("_Hue", 4f, 0f, index * 0.31f, 1f);
            else
                piece.Colours(PaleDeep, Pale, PaleHot, Cyan).Set("_Shade", 0.30f).Set("_Add", 0.20f).Set("_Halo", 0.10f).Set("_Hue", 8f, 0f, index * 0.31f, 1f);
            // The stroke is the streak map cut HARD (`cut`): below it the sheet is not there at all. The sheet's own four
            // borders are faded out, so no edge of the mesh is ever seen. It is eaten by the cloud, with a lit torn edge.
            piece.Set("_Tile", 1.0f, 1.3f, index * 0.31f, index * 0.17f).Set("_Scroll", 0f, 0f).Set("_Cut", cut, cut + 0.07f).Set("_Stops", cut + 0.10f, 0.72f, 0.98f)
                 .Set("_Ends", 0.14f, index == 0 ? 0.08f : 0.20f, 0.20f)
                 .Set("_ErodeTile", 1.1f, 0.8f, index * 0.27f, index * 0.43f).Set("_ErodeScroll", 0.22f, 0.04f).Set("_Sweep", 0.30f).Set("_Soft", 0.12f)
                 .Set("_Burn", dark ? 0.25f : 0.6f).WindowSoft(0.06f);
            return piece;
        }

        private void BuildSwoosh(int index, float from, float sweep, float rise, float width, float start)
        {
            var knots = new Knot[7];
            for (int i = 0; i < knots.Length; i++)
            {
                float t = i / (float)(knots.Length - 1), turn = (from + sweep * t) * Mathf.Deg2Rad, radius = 1.10f + 0.35f * t;
                knots[i] = new Knot(new Vector3(Mathf.Cos(turn) * radius, 0.30f + rise * t * t, Mathf.Sin(turn) * radius),
                                    // (The sine of pi comes back a hair below nothing in floats, and a root of that is not a number: one such width
                                    // makes the whole mesh vanish, which is what hid these for five films.)
                                    1.30f * width * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Lerp(0.08f, 1f, t))), 0.7f) + 0.01f, 78f);
            }
            var piece = Add("swoosh-" + index, // Stood on edge (rolled a quarter turn off the flat) so it is a wall of water seen from the side. It wears the
            // streak map at a HARD cut, so it is torn into a few thick tongues, and it is narrow and short-lived: plain
            // and wide, the two of them closed into one pale bowl in front of the dish and hid the ball.
            FxStudyKit.Sweep(knots, Vector3.up, perKnot: 10, across: 6, cup: 0.2f), FxStudyKit.Ribbon("erode_streak"), 14);
            piece.Colours(PaleDeep, Pale, PaleHot, Cyan).Set("_Tile", 1.2f, 1f, index * 0.4f, 0f).Set("_Scroll", 0f, 0f).Set("_Cut", 0.22f, 0.32f)
                 .Set("_Stops", 0.30f, 0.60f, 0.98f).Set("_Ends", 0.20f, 0.30f, 0.08f).Set("_Shade", 0.3f).Set("_Add", 0.2f).Set("_Halo", 0.08f)
                 .Set("_ErodeTile", 1.4f, 1f, index * 0.3f, 0f).Set("_ErodeScroll", 0.7f, 0.05f).Set("_Sweep", 1.0f).Set("_Soft", 0.10f).WindowSoft(0.06f);
            _swoosh[index] = new Wisp { Piece = piece, Start = start, Grow = 0.22f, EatFrom = start + 0.15f, EatTo = start + 0.45f, Speed = 1.8f + 0.5f * index, Tile = 1.2f, Phase = index * 0.4f };
        }

        private void BuildBillow(int index, Vector3 at, Vector3 size, float start, float seed)
        {
            // A gourd: (radius, height) from its foot to its crown.
            var profile = new[] { new Vector2(0f, 0f), new Vector2(0.55f, 0.04f), new Vector2(0.95f, 0.28f), new Vector2(1.00f, 0.58f),
                                  new Vector2(0.80f, 0.88f), new Vector2(0.45f, 1.06f), new Vector2(0f, 1.12f) };
            var piece = Add("billow-" + index, FxStudyKit.Lump(profile, seed, 0.30f), FxStudyKit.Orb(), 3);
            piece.Set("_Core", new Color(0.020f, 0.022f, 0.14f)).Set("_Rim", new Color(0.20f, 0.17f, 0.62f)).Set("_CoreAlpha", 1f).Set("_RimAlpha", 1f)
                 .Set("_Power", 2.2f).Set("_Band", 1f, 0.3f, 0f);
            _billow[index] = new Billow { Piece = piece, At = at, Size = size, Start = start };
        }

        private FxPiece BuildThrown(int index, float from, float lean, float length, float width)
        {
            var knots = new[] { new Knot(from, 0f, 0f, 0.03f), new Knot(from + lean * 0.4f, length * 0.4f, 0f, width), new Knot(from + lean, length, 0f, 0.03f) };
            var piece = Add("thrown-" + index, FxStudyKit.Sweep(knots, Vector3.back, perKnot: 8, across: 6), FxStudyKit.Ribbon("erode_streak"), 25);
            piece.Colours(new Color(0.36f, 0.42f, 0.62f), new Color(0.70f, 0.80f, 0.92f), Color.white, new Color(0.6f, 0.8f, 1f))
                 .Set("_Tile", 0.7f, 1f, index * 0.3f, 0f).Set("_Scroll", 3f, 0f).Set("_Cut", 0.06f, 1.00f).Set("_Ends", 0.35f, 0.35f, 0.32f)
                 .Set("_Halo", 0.2f).Set("_Add", 0.6f).Set("_Hue", 20f, 0f, index * 0.5f, 1f);
            return piece;
        }

        private FxPiece BuildTuft(int index, Vector3 at, float size, int which)
        {
            var part = new Rect((which % 2) * 0.5f, (which / 2) * 0.5f, 0.5f, 0.5f);
            var piece = Add("tuft-" + index, FxStudyKit.Square(false, part), FxStudyKit.Ribbon("ink_splats", "erode_blobs"), 5);
            var indigo = new Color(0.03f, 0.02f, 0.16f);
            piece.Colours(indigo, indigo, indigo, indigo).Set("_Scroll", 0f, 0f).Set("_Ends", 0f, 0f, 0f).Set("_Cut", 0.42f, 0.52f).Set("_Halo", 0f).Set("_Add", 0f)
                 .Set("_ErodeTile", 0.9f, 0.9f, 0.4f + index * 0.3f, 0.2f).Set("_Soft", 0.05f);
            piece.At.localPosition = at;
            piece.At.localScale = Vector3.one * size;
            return piece;
        }

        private void BuildFlame(int index, (Color deep, Color body, Color hot) colours, float start, Knot[] knots)
        {
            var piece = Add("flame-" + index, // Wide and soft, on the curtain mask: flame shapes with torn tips, not needles.
            FxStudyKit.Sweep(System.Array.ConvertAll(knots, k => new Knot(new Vector3(k.At.x * 0.8f, 0.35f + (k.At.y - 0.35f) * 0.72f, k.At.z), k.Width > 0.02f ? k.Width * 1.5f : 0.02f, k.Roll)), Vector3.back, perKnot: 10, across: 6),
            FxStudyKit.Ribbon("erode_streak", "erode_cloud"), 6);
            piece.Colours(colours.deep, colours.body, colours.hot, colours.body).Set("_Tile", 0.5f, 1f, index * 0.23f, 0f).Set("_Scroll", 0f, 0f).Set("_Hue", 10f, 0f, index * 0.4f, 1f)
                 .Set("_Cut", 0.22f, 0.40f).Set("_Stops", 0.40f, 0.70f, 1.0f).Set("_Ends", 0.12f, 0.34f, 0.22f)
                 .Set("_ErodeTile", 1f, 1f, index * 0.19f, 0f).Set("_Sweep", 0.8f).Set("_Soft", 0.15f).Set("_Halo", 0.5f).Set("_Add", 0.7f).Set("_Gain", 1.2f);
            _flame[index] = new Wisp { Piece = piece, Start = start, Grow = 0.14f, EatFrom = start + 0.10f, EatTo = start + 0.34f, Speed = 2.2f + 0.3f * index, Tile = 0.5f, Phase = index * 0.23f };
        }

        private void BuildWisp(int index, bool dark, float lead, float cup, float start, float grow, float eatFrom, float eatTo, int order, Knot[] knots)
        {
            // Measured: the reference's dark blades are fatter than these were typed and its pale ones thinner.
            var sized = new Knot[knots.Length];
            for (int i = 0; i < knots.Length; i++) sized[i] = new Knot(knots[i].At, knots[i].Width * (dark ? 1.15f : 0.70f), knots[i].Roll);
            var piece = Add("wisp-" + index, FxStudyKit.Sweep(sized, Vector3.back, perKnot: 14, across: 8, cup: cup), FxStudyKit.Ribbon("erode_streak", "erode_cloud"), order, _wisps);
            if (dark)
                // Saturated, shaded across its width so it reads as a blade with a lit side and a shadowed one, and it
                // COVERS: the ball's light does not show through it.
                piece.Colours(UltraDeep, Ultramarine, UltraLit, Ultramarine).Set("_Shade", 0.45f).Set("_Add", 0f).Set("_Halo", 0f)
                     .Set("_Lead", lead).Set("_LeadColor", Pale);
            else
                piece.Colours(PaleDeep, Pale, PaleHot, Cyan).Set("_Shade", 0.35f).Set("_Add", 0.20f).Set("_Halo", 0.10f);
            // It WEARS the streak map: solid through its middle, brushed with streaks of its lit colour, and where it thins
            // toward either end the streaks are what is left, so it ends torn and never in a clean vector point.
            piece.Set("_Tile", 1.2f, 1f, index * 0.31f, index * 0.17f).Set("_Scroll", 0f, 0f).Set("_Cut", 0.04f, 0.16f)
                 .Set("_Stops", dark ? 0.46f : 0.30f, dark ? 0.76f : 0.60f, 0.98f).Set("_Ends", 0.26f, 0.34f, 0.07f)
                 // The streaks run along the wisp and drift toward its head, and the root is eaten first.
                 .Set("_ErodeTile", 1.1f, 0.8f, index * 0.27f, index * 0.43f).Set("_ErodeScroll", 0.22f, 0.04f).Set("_Sweep", 0.30f).Set("_Soft", 0.12f)
                 .Set("_Burn", dark ? 0.25f : 0.6f).WindowSoft(0.06f);
            if (dark) piece.Set("_Cut", 0.015f, 0.07f).Set("_Ends", 0.14f, index == 0 ? 0.05f : 0.16f, 0.07f);
            piece.Set("_Hue", dark ? 4f : 8f, 0f, index * 0.31f, 1f);
            _wisp[index] = new Wisp { Piece = piece, Start = start, Grow = grow, EatFrom = eatFrom, EatTo = eatTo, Speed = WispSpeed[index % WispSpeed.Length], Tile = 1.2f, Phase = index * 0.31f };
        }

        private FxPiece BuildArc(int index, float radius, float height, float tilt, float from, float sweep)
        {
            var knots = new Knot[7];
            for (int i = 0; i < knots.Length; i++)
            {
                float t = i / (float)(knots.Length - 1), turn = (from + sweep * t) * Mathf.Deg2Rad;
                knots[i] = new Knot(Quaternion.Euler(tilt, 0f, tilt * 0.5f) * new Vector3(Mathf.Cos(turn) * radius, height, Mathf.Sin(turn) * radius),
                                    0.12f * Mathf.Sin(Mathf.PI * t) + 0.008f);
            }
            var piece = Add("arc-" + index, FxStudyKit.Sweep(knots, Vector3.up, perKnot: 8, across: 2), FxStudyKit.Ribbon(null), 24, _arcs);
            piece.Colours(Cyan, PaleHot, Color.white, Cyan).Set("_Scroll", 0f, 0f).Set("_Cut", 0.05f, 0.5f).Set("_Ends", 0.35f, 0.35f, 0.30f).Set("_Halo", 0.2f).Set("_Add", 1f);
            return piece;
        }

        public override void Sample(float age, Vector3 eye)
        {
            if (_showShells)
            {
                // Turned slowly, so the sheets are seen from all round.
                _wisps.localScale = Vector3.one * 0.62f;
                _wisps.localRotation = Quaternion.Euler(0f, age * 160f, 0f);
                foreach (var shell in _wisp) { if (shell.Piece == null) continue; shell.Piece.Open(-1f, 2f); shell.Piece.Frame(age, 1f, 0f); }
                return;
            }

            // ---- 1. the light. On in two frames, blinding to 0.75 s, out by 0.95.
            float light = FxStudyKit.Envelope(age, 0.0f, 0.05f, 0.80f, 0.24f);
            float swell = FxStudyKit.Pop(FxStudyKit.Run(age, 0.02f, 0.22f), 0.08f);
            _ball.At.localScale = Vector3.one * (0.42f * swell * (1f - 0.35f * FxStudyKit.Ease(FxStudyKit.Run(age, 0.78f, 1.0f))));
            _ball.Frame(age, FxStudyKit.Envelope(age, 0.02f, 0.10f, 0.82f, 0.18f));
            _swirl.At.localScale = _ball.At.localScale * 1.03f;
            _swirl.At.localRotation = Quaternion.Euler(20f, age * 300f, 10f);
            _swirl.Frame(age, 0.55f * FxStudyKit.Envelope(age, 0.10f, 0.10f, 0.82f, 0.18f));
            // Measured: the reference's blown-out white is SMALLER than ours through the first quarter second and bigger
            // after it, with a second peak near 0.85 s as the ball comes apart into white arcs.
            float later = FxStudyKit.Ease(FxStudyKit.Run(age, 0.25f, 0.45f)), parting = FxStudyKit.Envelope(age, 0.72f, 0.12f, 1.00f, 0.22f);
            _hot.At.localScale = Vector3.one * (Mathf.Lerp(0.85f, 2.3f, later) * swell + 1.4f * parting);
            _hot.Face(eye); _hot.Frame(age, Mathf.Max(light * Mathf.Lerp(0.70f, 1f, later), parting));
            _halo.At.localScale = Vector3.one * (3.0f * (0.6f + 0.4f * swell));
            _halo.Face(eye); _halo.Frame(age, light * 0.28f);
            _pool.At.localScale = Vector3.one * (6.0f * (0.5f + 0.5f * swell));
            _pool.Frame(age, light * 0.30f);
            // The first frames: a burst of light bigger than the ball that follows it.
            // The same sprite, in FRONT of the blades, is the white the ball goes out in near 0.85 s: in the reference that
            // white hides most of the young blades behind it.
            float veil = FxStudyKit.Envelope(age, 0.62f, 0.14f, 0.82f, 0.14f);
            _flash.At.localScale = Vector3.one * (age < 0.3f ? Mathf.Lerp(1.6f, 4.2f, FxStudyKit.EaseOut(FxStudyKit.Run(age, 0f, 0.10f))) : 1.5f + 0.5f * veil);
            _flash.Face(eye); _flash.Frame(age, Mathf.Max(0.12f * FxStudyKit.Envelope(age, -0.02f, 0.02f, 0.03f, 0.10f), 0.30f * veil));

            _flare.At.localScale = Vector3.one * Mathf.Lerp(1.2f, 4.0f, FxStudyKit.EaseOut(FxStudyKit.Run(age, 0f, 0.28f)));
            _flare.At.localRotation = Quaternion.Euler(0f, 35f, 0f);
            _flare.Frame(age, 0.55f * FxStudyKit.Envelope(age, 0.0f, 0.03f, 0.12f, 0.22f), 0.30f + 0.5f * FxStudyKit.Run(age, 0.08f, 0.34f));
            for (int i = 0; i < _thrown.Length; i++)
            {
                // Up and out of the frame inside two tenths of a second, stretching as it goes.
                float flown = FxStudyKit.Run(age, 0.02f + i * 0.015f, 0.24f);
                _thrown[i].At.localPosition = new Vector3(0f, 0.5f + 3.6f * flown, -0.1f);
                _thrown[i].At.localScale = new Vector3(1f, 1f + 1.2f * flown, 1f);
                _thrown[i].Frame(age, 0.65f * FxStudyKit.Envelope(age, 0.02f + i * 0.015f, 0.02f, 0.12f, 0.10f));
            }
            _bubble.At.localScale = Vector3.one * Mathf.Lerp(0.42f, 0.74f, FxStudyKit.EaseOut(FxStudyKit.Run(age, 0.04f, 0.32f)));
            _bubble.Frame(age, 0.8f * FxStudyKit.Envelope(age, 0.04f, 0.04f, 0.22f, 0.10f), 0.9f * FxStudyKit.Run(age, 0.10f, 0.32f));

            // ---- the dish: lands with the flash, and is only its lit lip by the time the ball goes out.
            float dish = FxStudyKit.Pop(FxStudyKit.Run(age, 0.05f, 0.25f), 0.2f);
            _dish.At.localScale = new Vector3(dish, Mathf.Lerp(0.6f, 1f, dish), dish) // Measured on the reference: its dish is 0.34 of the frame wide at 0.15 s and its lip 0.52 by 0.80 s.
                                      * Mathf.Lerp(0.74f, 1.30f, FxStudyKit.Ease(FxStudyKit.Run(age, 0.12f, 0.80f)));
            _dish.Set("_CoreAlpha", (1f - FxStudyKit.Ease(FxStudyKit.Run(age, 0.40f, 0.70f))));
            _dish.Set("_RimAlpha", (1f - FxStudyKit.Ease(FxStudyKit.Run(age, 0.50f, 0.85f))));
            _dish.At.localRotation = Quaternion.Euler(0f, age * 22f, 0f);
            _dish.At.localPosition = new Vector3(0f, 0.03f, 0f);
            _dish.Frame(age, FxStudyKit.Envelope(age, 0.05f, 0.04f, 0.95f, 0.20f));
            _dishStreaks.At.localScale = _dish.At.localScale * 1.02f;
            _dishStreaks.At.localRotation = _dish.At.localRotation;
            _dishStreaks.At.localPosition = _dish.At.localPosition;
            _dishStreaks.Frame(age, 0.9f * FxStudyKit.Envelope(age, 0.08f, 0.06f, 0.90f, 0.15f), FxStudyKit.Run(age, 0.60f, 1.05f));
            float crown = FxStudyKit.EaseOut(FxStudyKit.Run(age, 0.18f, 0.60f));
            _crown.At.localScale = new Vector3(Mathf.Lerp(0.9f, 1.2f, crown), Mathf.Lerp(0.3f, 0.9f, crown), Mathf.Lerp(0.9f, 1.2f, crown));
            _crown.At.localRotation = Quaternion.Euler(0f, age * 60f, 0f);
            // Never a whole ring: the erosion map leaves it as two or three sweeps of pale water round the lip.
            _crown.Frame(age, 0.10f * FxStudyKit.Envelope(age, 0.22f, 0.08f, 0.60f, 0.25f), 0.50f + 0.45f * FxStudyKit.Run(age, 0.50f, 0.90f));
            foreach (var sweep in _swoosh) Draw(sweep, age, 0.75f * (1f - FxStudyKit.Run(age, sweep.Start + 0.34f, sweep.Start + 0.50f)), 0f);

            // ---- the first flames.
            foreach (var flame in _flame) Draw(flame, age, 1f, 0f);

            // ---- 2 and 3. the wisps: the whole turbine swells from the dish and turns a little as it does.
            // ⚠️ MEASURED ON THE REFERENCE: its blades spread WIDE early and stand LOW until 0.85 s, then shoot up: the top of
            // its blue is 0.9 m up at 0.70 s, 1.4 m at 1.00, 1.9 m at 1.15 and out of the frame by 1.30. One scale for
            // both ways (the first films) had the plume through the top of the frame by 0.70.
            float wide = 0.30f + 0.90f * FxStudyKit.Run(age, 0.25f, 1.70f);
            float tall = 0.23f + 0.92f * Mathf.Pow(FxStudyKit.Run(age, 0.50f, 1.65f), 1.6f);
            _wisps.localScale = new Vector3(wide, tall, wide);
            _wisps.localRotation = Quaternion.Euler(0f, Mathf.Lerp(28f, -14f, FxStudyKit.EaseOut(FxStudyKit.Run(age, 0.30f, 1.8f))), 0f);
            _wisps.localPosition = new Vector3(0f, 0.25f * FxStudyKit.Ease(FxStudyKit.Run(age, 0.9f, 2.2f)), 0f);
            // They do not snap out: over the last second they go to a ghost of themselves while the streaks open in them.
            float thin = 1f - FxStudyKit.Ease(FxStudyKit.Run(age, 1.18f, 1.76f));
            // Measured: the reference's pale blades are all but gone by 1.3 s, well before its dark ones.
            float paleThin = 1f - FxStudyKit.Ease(FxStudyKit.Run(age, 1.30f, 1.65f));
            // ⚠️ NO WISP FADES AS A WHOLE ANY MORE. The owner: "your's keeps the same shape all throughout its lifetime, and fades
            // the whole wisp once". Each is eaten by the cloud map instead (`Draw`): holes open, widen, join, and the last
            // islands go; and each bends a little more the older it gets.
            for (int i = 0; i < _wisp.Length; i++) Draw(_wisp[i], age, i == 2 || i == 3 || i == 5 ? 1f : 0.92f, 0.35f);

            // ---- 4. ink. Thrown outward as the dish lands, then eaten to specks.
            for (int i = 0; i < _splat.Length; i++)
            {
                var splat = _splat[i];
                float thrown = FxStudyKit.EaseOut(FxStudyKit.Run(age, 0.20f + i * 0.012f, 0.50f));
                splat.Piece.At.localPosition = new Vector3(splat.At.x * Mathf.Lerp(0.45f, 1f, thrown), 0.02f + i * 0.001f, splat.At.z * Mathf.Lerp(0.45f, 1f, thrown));
                splat.Piece.At.localRotation = Quaternion.Euler(0f, splat.Turn, 0f);
                splat.Piece.At.localScale = Vector3.one * (splat.Size * 0.62f * Mathf.Lerp(0.25f, 1f, thrown));
                splat.Piece.Frame(age, FxStudyKit.Run(age, 0.20f, 0.24f), FxStudyKit.Run(age, 0.50f + i * 0.02f, 1.08f));
            }
            // Out from the bowl's two sides and forward, low, curling a little; born from 0.18 s for about a second.
            if (_smoke[0] != null) _smoke[0].Pose(age, 0.18f, 1.00f, new Vector3(-1.10f, 0.06f, -0.30f), new Vector3(0.22f, 0.10f, 0.30f), new Vector3(-1.15f, 0.22f, -0.35f), 26f, 0.30f);
            if (_smoke[1] != null) _smoke[1].Pose(age, 0.20f, 1.00f, new Vector3(1.10f, 0.06f, -0.25f), new Vector3(0.22f, 0.10f, 0.30f), new Vector3(1.10f, 0.20f, -0.30f), -24f, 0.30f);
            for (int i = 0; i < _billow.Length; i++)
            {
                var billow = _billow[i];
                if (billow.Piece == null) continue;
                // Swells up past its size and settles, then drifts slowly outward as it is eaten.
                float grown = FxStudyKit.Pop(FxStudyKit.Run(age, billow.Start, billow.Start + 0.26f), 0.16f);
                float drift = 1f + 0.22f * FxStudyKit.Ease(FxStudyKit.Run(age, billow.Start, 1.2f));
                billow.Piece.At.localPosition = new Vector3(billow.At.x * drift, billow.At.y, billow.At.z);
                // A round puff when it is born, a low spread mass by the time it is eaten.
                float spread = FxStudyKit.Ease(FxStudyKit.Run(age, billow.Start + 0.1f, 1.1f));
                billow.Piece.At.localScale = Vector3.Scale(billow.Size, new Vector3(1f + 0.55f * spread, 1f - 0.34f * spread, 1f + 0.30f * spread)) * grown;
                billow.Piece.Set("_Billow", 0.42f * FxStudyKit.Run(age, billow.Start, billow.Start + 0.10f) * (1f + 0.18f * Mathf.Sin(age * 9f + i)), 2.4f, 4.2f + 0.4f * i, i * 1.9f);
                billow.Piece.At.localRotation = Quaternion.Euler(0f, i * 67f, 0f);
                billow.Piece.Frame(age, age < billow.Start ? 0f : 1f, FxStudyKit.Run(age, 0.58f + i * 0.03f, 1.10f + i * 0.02f));
            }
            for (int i = 0; i < _tuft.Length; i++)
            {
                if (_tuft[i] == null) continue;
                float up = FxStudyKit.EaseOut(FxStudyKit.Run(age, 0.16f, 0.60f));
                _tuft[i].At.localScale = Vector3.one * ((i == 0 ? 1.40f : 1.25f) * Mathf.Lerp(0.4f, 1f, up));
                _tuft[i].Face(eye);
                _tuft[i].Frame(age, FxStudyKit.Run(age, 0.16f, 0.20f), FxStudyKit.Run(age, 0.40f, 0.85f));
            }

            // ---- the arcs about the ball as it goes out.
            _arcs.localRotation = Quaternion.Euler(0f, -age * 420f, 0f);
            _arcs.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.15f, FxStudyKit.Ease(FxStudyKit.Run(age, 0.50f, 1.15f)));
            for (int i = 0; i < _arc.Length; i++)
                _arc[i].Frame(age, FxStudyKit.Envelope(age, 0.62f + i * 0.04f, 0.10f, 0.98f + i * 0.03f, 0.22f));

            // ---- 5. sparks.
            _flecks.Begin(eye);
            // The star on the floor: twenty-two teardrops, every third one dimmer (so cyan, not white) flung flat from the middle in the first frames.
            // (Low and flat round the dish, a few big and many small, each its own dab and its own colour, see-through.)
            for (int i = 0; i < 32; i++)
            {
                float turn = (i + FxStudyKit.Hash(i, 1) * 0.9f) / 32f * Mathf.PI * 2f, big = FxStudyKit.Hash(i, 2);
                float reach = 0.55f + 1.00f * big * big;
                float flung = FxStudyKit.EaseOut(FxStudyKit.Run(age, 0f, 0.34f));
                var going = new Vector3(Mathf.Cos(turn), 0.02f + 0.06f * FxStudyKit.Hash(i, 12), Mathf.Sin(turn)).normalized;
                _flecks.Add(going * (0.34f + 0.95f * flung * (0.6f + 0.6f * FxStudyKit.Hash(i, 13))) + Vector3.up * 0.04f, going,
                            Mathf.Lerp(0.55f, 0.26f, flung) * reach, Mathf.Lerp(0.34f, 0.18f, flung) * reach,
                            (0.62f + 0.38f * FxStudyKit.Hash(i, 14)) * FxStudyKit.Envelope(age, 0.0f, 0.02f, 0.14f + 0.12f * FxStudyKit.Hash(i, 3), 0.24f),
                            i * 7 + 2, Tone(i));
            }
            // The ring: small ones thrown wider and a little up, hanging a moment.
            for (int i = 0; i < 30; i++)
            {
                float turn = (i + FxStudyKit.Hash(i, 4)) / 30f * Mathf.PI * 2f, from = 0.10f + 0.25f * FxStudyKit.Hash(i, 5);
                float flung = FxStudyKit.EaseOut(FxStudyKit.Run(age, from, from + 0.55f));
                var going = new Vector3(Mathf.Cos(turn), 0.25f + 0.5f * FxStudyKit.Hash(i, 6), Mathf.Sin(turn)).normalized;
                Vector3 at = going * (0.9f + 1.7f * flung * (0.5f + 0.5f * FxStudyKit.Hash(i, 7))) + Vector3.up * 0.1f;
                at.y -= 0.5f * flung * flung;
                _flecks.Add(at, going, 0.10f + 0.12f * FxStudyKit.Hash(i, 15), 0.07f, 0.75f * FxStudyKit.Envelope(age, from, 0.03f, from + 0.30f, 0.30f), i * 5 + 3, Tone(i + 2));
            }
            // A few rising with the wisps.
            for (int i = 0; i < 10; i++)
            {
                float turn = (i + FxStudyKit.Hash(i, 8)) / 10f * Mathf.PI * 2f, from = 0.70f + 0.30f * FxStudyKit.Hash(i, 9), far = 0.35f + 0.9f * FxStudyKit.Hash(i, 10);
                float risen = FxStudyKit.EaseOut(FxStudyKit.Run(age, from, from + 0.6f));
                var at = new Vector3(Mathf.Cos(turn) * far, 0.7f + 1.3f * risen * (0.4f + FxStudyKit.Hash(i, 11)), Mathf.Sin(turn) * far * 0.6f);
                _flecks.Add(at, new Vector3(Mathf.Cos(turn) * 0.3f, 1f, 0f), 0.15f, 0.055f, 0.75f * FxStudyKit.Envelope(age, from, 0.04f, from + 0.25f, 0.25f), i * 3 + 1, Tone(i + 4));
            }
            _flecks.End(age);

            _hairs.Begin(eye);
            for (int i = 0; i < 14; i++)
            {
                // A fan over the ball, leaning up and to the left as the reference's does.
                float lean = Mathf.Lerp(-80f, 55f, (i + FxStudyKit.Hash(i, 21) * 0.8f) / 14f) * Mathf.Deg2Rad, start = 0.05f + 0.06f * FxStudyKit.Hash(i, 22);
                float grown = FxStudyKit.EaseOut(FxStudyKit.Run(age, start, start + 0.14f)), reach = 0.45f + 0.60f * FxStudyKit.Hash(i, 23);
                var going = new Vector3(Mathf.Sin(lean) - 0.25f, Mathf.Cos(lean), 0.3f * (FxStudyKit.Hash(i, 24) - 0.5f)).normalized;
                _hairs.Add(new Vector3(0f, 0.45f, 0.05f) + going * (0.35f + reach * grown), going, reach * grown, 0.045f,
                           FxStudyKit.Envelope(age, start, 0.03f, start + 0.12f, 0.14f));
            }
            _hairs.End(age);
        }

        // Draws one wisp for this age: its head runs out along its path, then its root lets go and is eaten.
        public static void Draw(Wisp wisp, float age, float opacity, float letGo)
        {
            if (wisp.Piece == null) return;
            float head = FxStudyKit.EaseOut(FxStudyKit.Run(age, wisp.Start, wisp.Start + wisp.Grow)) * 1.15f;
            float tail = letGo * FxStudyKit.Ease(FxStudyKit.Run(age, wisp.EatFrom, wisp.EatTo)) - 0.1f;
            wisp.Piece.Open(tail, head);
            // ⚠️ THE PICTURE TRAVELS. The owner: "the original actually moves the texture itself too, not just the start and
            // end of the wisp" (the breakdown's step is "scroll on wisp mesh"). So the mask runs along the piece toward its
            // head the whole time it is alive, fast when it is born and easing to a crawl, and drifts a little sideways;
            // the erosion map slides at its own steady rate, so the two move against each other.
            if (wisp.Speed > 0f)
            {
                float life = Mathf.Max(0f, age - wisp.Start);
                float run = wisp.Speed * (0.30f * (1f - Mathf.Exp(-life / 0.30f)) + 0.22f * life);
                wisp.Piece.Set("_Tile", wisp.Tile, 1f, wisp.Phase - run, wisp.Phase * 0.55f + 0.05f * life);
            }
            // Eaten on a curve that starts gently: a few holes early, everything by the end. Never later than 1.88 s, when
            // the reference is empty.
            float end = Mathf.Min(wisp.EatTo, 1.88f);
            float eaten = Mathf.Pow(FxStudyKit.Run(age, Mathf.Min(wisp.Start + 0.40f, end - 0.3f), end), 1.6f);
            wisp.Piece.Frame(age, age < wisp.Start ? 0f : opacity, eaten);
            wisp.Piece.Set("_Warp", 0.02f + 0.16f * Mathf.Max(0f, age - wisp.Start), 1.3f, wisp.Phase * 7f, 2.2f);
        }
    }
}
