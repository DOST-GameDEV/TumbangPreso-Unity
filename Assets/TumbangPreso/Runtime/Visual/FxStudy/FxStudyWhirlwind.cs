using UnityEngine;
using Knot = TumbangPreso.Visual.FxStudyKit.Knot;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// STUDY G, THE WHIRLWIND. The owner sent a picture of the whirlwind he means (a stylised tornado on a dark floor) and
    /// then said what it was to be made of: "not my picture, but the effects we made like the wisp and trail effect".
    /// So all of it but the horn is the bench's own effects, called as they are and stood up as a whirlwind:
    ///
    ///   * THE TAPES that orbit it are the approved trail ribbons (`FxStudyKit.Sweep`, `FxStudyTrails.Dress`), swept round
    ///     tilted rings and stood on edge the way the wisp's swooshes are.
    ///   * THE GROUND is the wisp's dish streaks (`FxStudyWisp.DressDishStreaks`) on a flat lathed disc, with trail 2
    ///     round its rim.
    ///   * THE MOTES are the bench's glow, as sparks (`FxFlecks`).
    ///
    /// ⚠️ THOSE THREE ARE APPROVED AND KEPT. Of the first film of this (h1) the owner said on 2026-10-11: "rest of the sfx
    /// looks good". Their tables, colours, widths, counts and brightness below, and this effect's bloom, are that film;
    /// it is backed up in `tools/cutscene_rework/approved/whirlwind_h1/`. They are marked KEEP where they stand.
    ///
    /// ⚠️ AND THE HORN IS ONE NOISE, IN HIS WORDS: "the middle horn is supposed to be a skewed large transparent celled
    /// perlin noise". In h1 it was six shells of the wisp's streak map with trail 1's veins over them, and it stood as
    /// a rigid cone of bright shards. He then showed the noise he meant (simplex noise with its negative half cut
    /// off: soft white blobs on black) and said of it "but more white" and "feather". So the horn is ONE SMOOTH
    /// SURFACE, a trumpet turned on the lathe, wearing that noise (`noise_simplex_cells`, painted for this and for
    /// nothing else): where the noise is black the horn is not there, so the far side shows through the near side;
    /// the noise is sheared, so its cells wind round the horn; and it runs round and up the horn, which is the spin.
    /// Of that horn's first film (h2) he said "a bit too sparse for the main horn", and of its mouth: "instead of just
    /// feathering at the rim, you should instead increase the empty cells near the top so its more wispy at the rim".
    /// So the body is dense (few gaps, the cells joined) and the gaps grow with height through its upper part until
    /// only separate tongues are left at the lip: `Skin.Wisps` has how. And of its cells, shown two sizes: "use the finer
    /// cells" (six lattice steps of the noise, not four). And of that one: "i think the noise is too blob-y. and you
    /// should still fade out the top", and "some parts should reach bright whites". So the cells are drawn out into
    /// streaks that climb round the horn (the painter has how), each bright along its spine; the top fades as well as
    /// being eaten; and the spines of the fullest streaks go to near-white.
    /// A second, wider copy runs at another rate behind the first, for depth.
    ///
    /// What was read off his picture of the whirlwind, which the tables are:
    ///   * The horn is a TRUMPET on a point: 40 px wide at its foot, 110 a bit under half way up, 220 at two thirds, 290
    ///     at its mouth. About one to seven, with a thin stem for its lower two fifths.
    ///   * The tapes are flat, milky and greyer than the horn, and their rings widen DOWNWARD while the horn widens
    ///     upward: the whole silhouette is an hourglass.
    ///   * The ground swirl is near rings, dark in the middle, brightest in its outer third, and is as wide as the whole
    ///     thing is tall.
    ///   * About sixteen motes: rounded ovals with a bright heart, most near the foot, a few in a column over the mouth.
    ///
    /// It loops every four seconds on the bench: every rate below is a whole number of turns or pictures in four.
    /// </summary>
    public sealed class FxStudyWhirlwind : FxStudyEffect
    {
        /// <summary>
        /// ⚠️ ONE SWITCH BETWEEN TWO HORNS, and a third setting for none. `Noise` is the one smooth trumpet wearing the
        /// simplex cells (the bench's `whirlwind`). `Shells` is the horn of the first film, h1: six wisp shells with trail
        /// 1's veins over them (the bench's `whirlwindh1`). The owner, of the noise horn's last pass: "if this pass doesnt
        /// look good, lets just keep the original". So the original is kept whole and filmable by name, and going
        /// back to it is this one word where a whirlwind is built. `None` builds the approved parts alone.
        ///
        /// ⚠️ AND `Mix` IS BOTH AT ONCE. Shown the noise horn's last film (h7) beside the original, he said: "try doing a mix
        /// of the original and this latest h7 one". So the noise horn is the BODY (the soft wound streaks, the stem,
        /// the faded wispy mouth), and the original's vein net and three of its shells are drawn inside and over it for
        /// its thin bright lines, wound the way the noise winds and bent as it bends, so the two are one object.
        /// Both are light, so each is turned down for the other (`mix` is how far toward the original: see the build),
        /// and the white is left to the original's thin lines: with both at full strength the horn is one white cone.
        /// `Noise` and `Shells` alone are exactly films h7 and h1, and stay filmable by name.
        ///
        /// ⚠️ KEEP THE MIX AS IT IS. Of its film (h8, the mix at 0.55) the owner said on 2026-10-11: "that looks amazing as
        /// is". Every number of the horn below, of both horns and of how they are turned down for each other, is that
        /// film, and so are the tapes, the ground and the motes. It is backed up in
        /// `tools/cutscene_rework/approved/whirlwind_h8/` (and, in the colours it wears now, in `whirlwind_h9/`). After a change to anything this effect shares (the kit, the
        /// ribbon shader, the wisp, the trails, the generator), film it again and lay it against the kept frames
        /// (`tools/cutscene_rework/fxsame.py whirlwind <tag>`), as the trails are laid against theirs.
        /// </summary>
        public enum Horn { None, Noise, Shells, Mix }

        /// <summary>
        /// How one whirlwind looks and is proportioned. `Picture` is the owner's picture on the dark bench. Something
        /// that stands a whirlwind somewhere else (in daylight, in a hero's colours, taller for its width) hands in its own.
        /// </summary>
        public struct Look
        {
            /// <summary>The horn: the colour of its faint edge, of its body, of the heart of a cell, and its halo.</summary>
            public Color SilkDeep, Silk, SilkHot, SilkHalo;
            /// <summary>The tapes: milky, and paler where the picture they wear is bright.</summary>
            public Color TapeDeep, Tape, TapeHot;
            /// <summary>The swirl on the ground.</summary>
            public Color GroundDeep, Ground, GroundHot;
            public Color MoteHeart, MoteHalo;
            /// <summary>How much of every piece is light laid on (1) and how much covers what is behind it (0). The picture
            /// is light on a dark floor; on a lit road light adds nothing, and the same pieces have to cover.</summary>
            public float Light;
            /// <summary>How much of the horn's noise shows where the noise is white (the rest is see-through).</summary>
            public float Body;
            /// <summary>Height of the stem (the lower 44 percent of the horn) and of the rest above it, as multiples of
            /// the picture's. Stretching the stem more than the mouth keeps the trumpet when a whirlwind has to be tall.</summary>
            public float Stem, Crown;
            /// <summary>Width of everything, as a multiple of the picture's.</summary>
            public float Wide;
            /// <summary>Turns this whirlwind's winding and puts its clocks out of step with another's (0 to 1).</summary>
            public float Seed;
            /// <summary>How far every piece's colour wanders either side of its own, in degrees of hue, by a slow noise that
            /// runs along the piece and drifts with the clock, each piece from its own seed. 0 leaves every piece the drift
            /// it was dressed with (as film h8 was).</summary>
            public float HueDrift;

            // ⚠️ THE COLOURS, AND THEY ARE THE COURT'S TOO. The whirlwind was approved (film h8) in a paler, bluer set, hue about
            // 175. Stood on the court it read as cyan, and the owner said: "it reads more as cyan than teal.. make it more
            // green, and add some hueshifts with green and blue as extremes"; and of the bench: "it doesnt really matter
            // waht color the bench is. but maybe apply hueshifts to the bench too". So this one definition is both: a
            // deeper teal at hue 168 for the horn, the ground swirl leaning to the green end, the motes at the blue end,
            // and every piece's hue wandering 28 degrees either side of its own (green near 140, blue near 196). White
            // stays white. Only the colours changed from h8: no shape, count, motion or share of light. (hue, saturation, value)
            // ⚠️ KEEP THESE VALUES. They are the court film he approved (one2: "looks good") and then played ("whirlwind is
            // already good"), and the bench's approved state h9 (`tools/cutscene_rework/approved/whirlwind_h9/`).
            public static Look Picture => new Look
            {
                SilkDeep = Color.HSVToRGB(160f / 360f, 0.92f, 0.26f), Silk = Color.HSVToRGB(168f / 360f, 0.88f, 0.58f), SilkHot = new Color(0.86f, 1f, 0.95f), SilkHalo = Color.HSVToRGB(168f / 360f, 0.88f, 0.42f),
                TapeDeep = Color.HSVToRGB(170f / 360f, 0.60f, 0.34f), Tape = Color.HSVToRGB(172f / 360f, 0.50f, 0.62f), TapeHot = Color.HSVToRGB(172f / 360f, 0.22f, 0.92f),
                GroundDeep = Color.HSVToRGB(150f / 360f, 0.90f, 0.22f), Ground = Color.HSVToRGB(156f / 360f, 0.82f, 0.46f), GroundHot = Color.HSVToRGB(160f / 360f, 0.45f, 0.88f),
                MoteHeart = new Color(0.85f, 0.97f, 1f), MoteHalo = Color.HSVToRGB(198f / 360f, 0.85f, 0.85f),
                Light = 0.85f, Body = 0.80f, Stem = 1f, Crown = 1f, Wide = 1f, Seed = 0f, HueDrift = 28f,
            };
        }

        // ---- the horn, in metres, for a whirlwind two metres tall: (radius, height) read off the picture.
        private static readonly Vector2[] Trumpet =
        {
            new Vector2(0.040f, 0.00f), new Vector2(0.082f, 0.31f), new Vector2(0.226f, 0.88f),
            new Vector2(0.454f, 1.31f), new Vector2(0.580f, 1.74f), new Vector2(0.600f, 2.00f),
        };
        private const float Tall = 2.0f, StemTop = 0.88f, GroundRadius = 1.03f;

        // The horn's skins: the stretch of the trumpet each lies on (shares of its height), how much wider than the
        // trumpet it stands, how far it winds by its top (degrees, on top of the shear the noise already has), how fast
        // its noise runs up it and round it (sheets a second), how much of it shows, where on the sheet it starts, and the
        // share of its own length above which its cells begin to be eaten away (`Wisps`).
        private readonly struct Skin
        {
            public readonly float From, To, Out, Twist, Up, Round, Shown, Slide, Opens;
            public Skin(float from, float to, float out_, float twist, float up, float round, float shown, float slide, float opens)
            { From = from; To = to; Out = out_; Twist = twist; Up = up; Round = round; Shown = shown; Slide = slide; Opens = opens; }
            // ⚠️ HOW THE MOUTH IS MADE WISPY, AND IT IS NOT A FADE. The owner, of the first horn of this noise: "instead of
            // just feathering at the rim, you should instead increase the empty cells near the top so its more wispy at the
            // rim". The sheet is a slope (see its painter), and the shader can eat a piece by a second picture, sooner
            // toward one end (`_Sweep`). Handed the SAME noise as that second picture, laid and run exactly as the
            // first, it eats each cell from its own rim inward, and by more the higher on the horn: nothing up to
            // `Opens`, then twice the height above it, so that at the very top only the hearts of the tallest cells are
            // left, as separate tongues with air between them. It goes by HEIGHT ON THE HORN, not by the sheet: a cell
            // travelling up thins out and breaks as it comes into the mouth. This is the erosion that draws that
            // (the shader's threshold worked back from: nothing eaten below `Opens`, with its sweep of 2 and its softness.
            // At a softness of 0.12, film h5, some tongues at the mouth were flat hard-edged paddles; it is 0.22.)
            public float Wisps => (2f - 2f * Opens + WispSoft) / (3f + 2f * WispSoft);
        }
        private const float WispSweep = -2f, WispSoft = 0.22f;
        // ⚠️ THE THIRD IS FOR THE STEM ALONE. On the first horn of this noise (h2) the stem all but vanished: it is a few
        // centimetres round, so a cell of the noise is a sliver there and its gaps are most of it. A second skin over
        // the lower half, a little fatter and starting elsewhere on the sheet, fills what the first leaves open, so the
        // stem reads as solid as the mouth.
        private static readonly Skin[] Skins =
        {
            new Skin(0.00f, 1.10f, 1.00f, 120f, 0.50f, 0.75f, 1.00f, 0.00f, 0.56f),
            new Skin(0.00f, 1.10f, 1.16f,  70f, 0.25f, 0.50f, 0.55f, 0.43f, 0.42f),
            new Skin(0.00f, 0.52f, 1.30f, 150f, 0.50f, 0.50f, 0.60f, 0.71f, 0.45f),
        };

        // ---- THE HORN OF FILM h1 (`Horn.Shells`), as it was filmed. One shell of it: the stretch of the trumpet it lies on (shares of its height), how much wider than the
        // trumpet it stands (a turn that overhangs the one below is a wider one), where round the axis its middle is, how
        // far round it reaches, how far it winds by its top (degrees), which of the wisp's two tones it is dressed as and
        // where its streak map is cut, and when in the shared cycle it is born.
        private readonly struct Wrap
        {
            public readonly string Name; public readonly float From, To, Out, Centre, Span, Twist, Cut, Born; public readonly bool Broad;
            public Wrap(string name, float from, float to, float out_, float centre, float span, float twist, bool broad, float cut, float born)
            { Name = name; From = from; To = to; Out = out_; Centre = centre; Span = span; Twist = twist; Broad = broad; Cut = cut; Born = born; }
        }
        // ⚠️ SIX, NO TWO ALIKE, EACH TYPED. A broad one is dressed as the wisp's dark tone (its streak map cut low, so most
        // of the sheet is there: the silk); a thin one as its pale tone (cut high, so only the map's bright streaks are).
        private static readonly Wrap[] Wraps =
        {
            new Wrap("stem",     0.00f, 0.52f, 1.00f,  20f, 230f, 330f, true,  0.08f, 0.00f),
            new Wrap("belly",    0.28f, 0.80f, 1.00f, 200f, 180f, 250f, true,  0.10f, 0.34f),
            new Wrap("wrap",     0.14f, 0.70f, 1.14f, 110f, 150f, 290f, true,  0.12f, 0.58f),
            new Wrap("shoulder", 0.50f, 0.98f, 1.12f, 320f, 160f, 210f, true,  0.10f, 0.78f),
            new Wrap("tongue",   0.70f, 1.08f, 1.20f,  60f, 130f, 150f, true,  0.14f, 0.16f),
            new Wrap("streaks",  0.36f, 1.05f, 1.06f, 250f, 210f, 230f, false, 0.52f, 0.46f),
        };
        // One wisp's life, which every shell lives over and over: drawn on in the first 0.4 s, whole until 0.9, eaten by 1.8.
        private const float Cycle = 2.0f, Grow = 0.40f, EatFrom = 0.90f, EatTo = 1.80f;
        private static readonly float[] Pace = { 1.1f, 0.9f, 1.3f, 1.0f, 1.4f, 1.2f };

        // How much of a broad shell showed in h1 (the look's `Body` then; the noise horn has since taken that for its own).
        private const float ShellBody = 0.62f;
        // ⚠️ WHICH OF THE SIX SHELLS GO INTO THE MIX (in the order of `Wraps`): the belly, the wrap and the thin pale streaks.
        // Not the stem (the noise horn has a skin for its stem), and not the shoulder or the tongue: those two are the
        // widest and stand at the mouth, and in film h1 they were what read as geometry and not as wind, an opaque dark
        // blade behind the mouth and a flat slab across its top.
        private static readonly bool[] Mixed = { false, true, true, false, false, true };

        // One tape: which of the five trails it wears (0 to 4), how high its ring stands and how wide it is (m), how the
        // ring is tilted (degrees about x and z), where the tape starts round it and how far it goes (degrees), how broad
        // the tape's sheet is and how far it climbs along its length (m), how many turns it makes in four seconds, and
        // how many of its pictures lie along it.
        private readonly struct Tape
        {
            public readonly string Name; public readonly int Trail; public readonly float Height, Radius, TiltX, TiltZ, From, Sweep, Broad, Climb, Turns, Tile;
            public Tape(string name, int trail, float height, float radius, float tiltX, float tiltZ, float from, float sweep, float broad, float climb, float turns, float tile)
            { Name = name; Trail = trail; Height = height; Radius = radius; TiltX = tiltX; TiltZ = tiltZ; From = from; Sweep = sweep; Broad = broad; Climb = climb; Turns = turns; Tile = tile; }
        }
        // ⚠️ KEEP (approved in h1, see the top). WIDEST AT THE GROUND (read off the picture: about 400 px across low down, 385 in the middle, 300 above, 200 at
        // the top). Trail 5 (`trail_bands`, the folded silk ribbon) is the broad tapes, since it is an even band with
        // crisp edges; trail 3 (`trail_ink`) one tape; trail 4 (`trail_squiggle`) the thin ones.
        private static readonly Tape[] Tapes =
        {
            new Tape("low",    4, 0.20f, 0.84f,  10f,   4f, 200f, 300f, 0.26f, 0.10f, 2f, 2f),
            new Tape("middle", 4, 0.68f, 0.80f, -14f,   8f,  40f, 320f, 0.24f, 0.16f, 3f, 2f),
            new Tape("upper",  2, 1.38f, 0.64f,   8f, -12f, 300f, 250f, 0.22f, 0.10f, 2f, 2f),
            new Tape("top",    3, 2.05f, 0.44f,  -6f,  10f, 120f, 200f, 0.16f, 0.06f, 3f, 1.5f),
            new Tape("hair",   3, 1.02f, 0.95f,  20f,  -8f, 170f, 150f, 0.10f, 0.20f, 2f, 1f),
        };

        // One mote: where round the axis it is, how far out, how high it starts and how far it rises (m), how big, and
        // when in the four seconds it is born.
        private readonly struct Mote
        {
            public readonly float Turn, Out, Low, Rise, Size, Born;
            public Mote(float turn, float out_, float low, float rise, float size, float born) { Turn = turn; Out = out_; Low = low; Rise = rise; Size = size; Born = born; }
        }
        // ⚠️ KEEP (approved in h1). Seven low round the foot, four beside the horn, five in a column over its mouth, as the
        // picture has them.
        private static readonly Mote[] Motes =
        {
            new Mote( 10f, 0.48f, 0.10f, 0.30f, 0.12f, 0.00f), new Mote( 70f, 0.62f, 0.06f, 0.24f, 0.09f, 0.31f), new Mote(135f, 0.40f, 0.14f, 0.34f, 0.10f, 0.62f),
            new Mote(200f, 0.70f, 0.08f, 0.22f, 0.13f, 0.17f), new Mote(250f, 0.34f, 0.18f, 0.40f, 0.08f, 0.48f), new Mote(300f, 0.58f, 0.05f, 0.28f, 0.11f, 0.79f),
            new Mote(340f, 0.80f, 0.12f, 0.20f, 0.09f, 0.90f),
            new Mote( 40f, 0.92f, 0.90f, 0.40f, 0.11f, 0.22f), new Mote(160f, 0.98f, 1.20f, 0.36f, 0.09f, 0.55f), new Mote(225f, 0.86f, 0.70f, 0.44f, 0.10f, 0.08f),
            new Mote(320f, 1.04f, 1.05f, 0.30f, 0.08f, 0.70f),
            new Mote( 20f, 0.16f, 2.10f, 0.60f, 0.11f, 0.12f), new Mote(110f, 0.24f, 2.25f, 0.55f, 0.08f, 0.44f), new Mote(190f, 0.10f, 2.40f, 0.70f, 0.12f, 0.66f),
            new Mote(260f, 0.30f, 2.15f, 0.50f, 0.07f, 0.86f), new Mote(330f, 0.20f, 2.55f, 0.60f, 0.09f, 0.30f),
        };

        private struct Sheet { public FxStudyWisp.Wisp Wisp; public float Born, Opacity, BendWaves, BendAt; }
        private struct Ring { public FxPiece Piece; public float Turns; }

        private readonly Look _look;
        private readonly bool _mixed;
        private readonly float _mix;
        private readonly Transform _column, _spin;
        private readonly FxPiece[] _horn = new FxPiece[Skins.Length];
        private readonly Sheet[] _sheet = new Sheet[Wraps.Length];
        private readonly FxPiece[] _vein = new FxPiece[2];
        private readonly Ring[] _ring = new Ring[Tapes.Length];
        private readonly FxPiece _ground, _licks, _pool;
        private readonly FxFlecks _motes;

        public override float Seconds => 4f;
        // The picture's floor is one dark grey with no grid on it.
        public override Color FloorColour => new Color(0.150f, 0.150f, 0.158f);
        public override Color FloorLine => new Color(0.150f, 0.150f, 0.158f);
        public override float BloomIntensity => 0.30f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            // ⚠️ THE PICTURE'S CAMERA, FROM ITS GROUND SWIRL. The swirl is an ellipse 500 px wide and 250 deep, so the camera
            // looks down thirty degrees; the whirlwind is 420 px tall on a frame 800 tall, its foot 195 px below the frame's
            // middle and 55 to the right of it. With the whirlwind two metres tall that is this eye and this field of view,
            // and a frame of the film can be laid on the picture at one size.
            focus = new Vector3(-0.23f, 0.93f, 0f);
            eye = focus + new Vector3(0f, 3.08f, -5.33f);
            fov = 30f;
        }

        public FxStudyWhirlwind(Transform parent) : this(parent, Look.Picture) { }

        /// <summary>`mix` matters only to `Horn.Mix`: 0 leans all the way to the noise horn, 1 to the original.</summary>
        // ⚠️ ITS MESHES ARE MADE ONCE FOR THE PROCESS AND SHARED BY EVERY WHIRLWIND (`AddShared`). The owner allowed this one
        // change to a whirlwind he had called final, for its cost alone: "no, do the fixes" (2026-10-17). Making them was
        // most of what a cast cost, and they are the same every time: they depend only on the shape (`Look.Seed`,
        // `Look.Wide`, the horn), which is the key. Nothing of how it looks, moves or is coloured is touched: every
        // material is still each whirlwind's own, so two at once still draw apart. A copy of this file as it was is in
        // `tools/cutscene_rework/approved/whirlwind_final_2026-10-11/pre_costfix_2026-10-17/`.
        private static readonly System.Collections.Generic.Dictionary<string, Mesh> Meshes = new System.Collections.Generic.Dictionary<string, Mesh>();
        private static Mesh Once(string name, Look look, Horn horn, System.Func<Mesh> make)
        {
            string key = name + "|" + look.Seed.ToString("R") + "|" + look.Wide.ToString("R") + "|" + horn;
            if (Meshes.TryGetValue(key, out var mesh) && mesh != null) return mesh;
            mesh = make();
            if (mesh != null) mesh.hideFlags = HideFlags.HideAndDontSave;
            Meshes[key] = mesh;
            return mesh;
        }

        public FxStudyWhirlwind(Transform parent, Look look, Horn horn = Horn.Mix, float mix = 0.55f) : base("whirlwind", parent)
        {
            _look = look;
            bool mixed = horn == Horn.Mix, noisy = horn == Horn.Noise || mixed, shelled = horn == Horn.Shells || mixed;
            _mixed = mixed; _mix = Mathf.Clamp01(mix);
            float wide = look.Wide, light = look.Light;

            // ---- the ground: the wisp's dish streaks on a flat disc, and trail 2 round its rim. ⚠️ KEEP (approved in h1).
            float rim = GroundRadius * wide;
            _pool = AddGlow("pool", 0, look.GroundHot, look.Ground, lying: true);
            _pool.At.localPosition = new Vector3(0f, 0.012f, 0f);
            _pool.At.localScale = Vector3.one * (rim * 2.5f);
            _ground = AddShared("ground", Once("ground", look, horn, () => FxStudyKit.Revolve(new[] { new Vector2(rim * 0.13f, 0.02f), new Vector2(rim * 0.42f, 0.02f), new Vector2(rim * 0.74f, 0.02f), new Vector2(rim, 0.02f) }, 56)),
                          FxStudyKit.Ribbon("erode_streak", "erode_cloud"), 1);
            FxStudyWisp.DressDishStreaks(_ground).Colours(look.GroundDeep, look.Ground, look.GroundHot, look.Ground).Set("_Add", light);
            _licks = AddShared("licks", Once("licks", look, horn, () => FxStudyKit.Revolve(new[] { new Vector2(rim * 0.66f, 0.025f), new Vector2(rim * 0.84f, 0.025f), new Vector2(rim * 1.04f, 0.025f) }, 56)),
                         FxStudyKit.Ribbon(FxStudyTrails.MaskOf(1)), 2);
            // Trail 2 was dressed for a ribbon two metres long; round a rim six metres about, six of its pictures fit.
            FxStudyTrails.Dress(_licks, 1).Colours(look.GroundDeep, look.Ground, look.GroundHot, look.Ground).Set("_Tile", 6f, 1f, 0.1f, 0f).Set("_Add", light)
                         .Set("_Hue", 8f, 4f, 0.2f, 1f);

            // ---- the horn: one smooth trumpet wearing the cells, and a wider one behind it. The column sways as one.
            _column = Group("column");
            _spin = new GameObject("spin") { hideFlags = HideFlags.DontSave }.transform;
            _spin.SetParent(_column, false);
            for (int i = 0; i < Skins.Length && noisy; i++)
            {
                Skin k = Skins[i];
                // All the way round (a shell of 360 degrees closes on itself, and the noise wraps, so there is no seam),
                // both faces drawn: through every gap in the near side the far side is seen running the other way.
                float reach = k.To - k.From;
                var piece = AddShared("horn-" + i, Once("horn-" + i, look, horn, () => FxStudyKit.Shell(Stretch(k.From, k.To, k.Out * wide, 9), look.Seed * 360f, 360f, k.Twist * reach, 44, 48)),
                                FxStudyKit.Ribbon("noise_simplex_cells", "noise_simplex_cells"), 5 + i, _spin);
                piece.Colours(look.SilkDeep, look.Silk, look.SilkHot, look.SilkHalo)
                     // (As much of a sheet along it as it has of the horn's height, so a cell is the same size on every skin.)
                     .Set("_Tile", reach, 1f, k.Slide + look.Seed, k.Slide * 0.7f).Set("_Scroll", k.Up, k.Round)
                     // ⚠️ A STREAK IS BRIGHT ALONG ITS SPINE AND FALLS AWAY TO ITS EDGES AND ITS TIPS. The sheet is a slope, and this
                     // lets nearly the whole of the slope through as how much of the streak is there: whole only at the very spine.
                     // (Cut to be whole by 0.30 and then by 0.46 of the slope, films h5 and h6, every cell was a flat pale
                     // plateau with a rim round it, which is a pill: "i think the noise is too blob-y".)
                     .Set("_Cut", 0.04f, 0.80f).Set("_Curve", 1.0f)
                     // ⚠️ "some parts should reach bright whites". Deep at a streak's edge, the body colour through most of it,
                     // and the upper third of the slope (the spines of the fullest streaks) goes to near-white. A FEW hot places
                     // on a teal body: with every cell pale right across and the horn nearly opaque (film h3) three skins and
                     // their far sides added up to a solid white cone, 1.8 percent of the frame blown to white.
                     .Set("_Stops", 0.28f, 0.58f, 0.90f)
                     // It thins to its point at the foot. ⚠️ AND ITS TOP IS FADED AS WELL AS EATEN. The cells are eaten away with
                     // height (`Skin.Wisps`), which makes the mouth wispy; with that alone (films h5 and h6) the last tongues
                     // still ended in a cut edge where the surface stops, as flat paddles sticking out, and he said: "and you
                     // should still fade out the top". So every skin, the wider one behind as well, also thins to nothing over its last third: fewer cells and each fainter.
                     .Set("_Ends", 0.06f, k.To < 1f ? 0.34f : 0.30f, 0f)
                     .Set("_ErodeTile", reach, 1f, k.Slide + look.Seed, k.Slide * 0.7f).Set("_ErodeScroll", k.Up, k.Round).Set("_Sweep", WispSweep).Set("_Soft", WispSoft)
                     // Soft: no halo of its own. Where a camera blooms, only its pale spines go over white and spill.
                     .Set("_Halo", 0f).Set("_Emit", 0.45f).Set("_Add", light).Set("_Hue", 6f, 4f, i * 0.31f + look.Seed, 1f)
                     // The S bend: a slow wave along it, more toward the mouth, standing still (the column's lean moves it).
                     .Set("_Warp", 0.09f * wide, 1.0f, 1.2f + i * 0.4f + look.Seed * 6.28f, 0f);
                // In the mix the noise is the teal body and the white is the original's: pale only at the very spine of a
                // streak, and nothing of it pushed over white for a bloom to spread.
                if (mixed) piece.Set("_Stops", 0.28f, 0.86f, 1.0f).Set("_Emit", 0.04f);
                _horn[i] = piece;
            }

            // ---- or the horn of film h1: the shells and veins turn inside the column.
            for (int i = 0; i < Wraps.Length && shelled; i++)
            {
                Wrap w = Wraps[i];
                if (mixed && !Mixed[i]) continue;
                // ⚠️ IN THE MIX A SHELL WINDS THE WAY THE NOISE DOES. The noise's streaks climb the horn going one way round and
                // the original's shells were wound the other; laid over each other as they were, the two cross as an X and
                // read as a cone standing inside a horn. Turned over, a shell's streaks run with the noise's.
                float twist = mixed ? -w.Twist : w.Twist;
                var piece = AddShared("shell-" + w.Name, Once("shell-" + w.Name, look, horn, () => FxStudyKit.Shell(Stretch(w.From, w.To, w.Out * wide, 5), w.Centre + look.Seed * 360f, w.Span, twist, 30, 18)),
                                FxStudyKit.Ribbon("erode_streak", "erode_cloud"), 5 + i + (mixed ? 4 : 0), _spin);
                FxStudyWisp.DressShell(piece, i, w.Broad, w.Cut).Colours(look.SilkDeep, look.Silk, look.SilkHot, look.SilkHalo)
                           .Set("_Add", w.Broad ? light : Mathf.Lerp(0.2f, 1f, light)).Set("_Hue", 6f, 4f, i * 0.31f, 1f);
                // In the mix a shell fades toward its top as the noise horn does (a third of its length), so none ends in an edge.
                if (mixed) piece.Set("_Ends", 0.14f, 0.34f, 0.20f);
                _sheet[i] = new Sheet
                {
                    Wisp = new FxStudyWisp.Wisp { Piece = piece, Start = 0f, Grow = Grow, EatFrom = EatFrom, EatTo = EatTo, Speed = Pace[i], Tile = 1f, Phase = i * 0.31f + look.Seed },
                    // In the mix: the broad shells are only a faint second body under the noise's, the thin pale one most of itself.
                    Born = w.Born + look.Seed, Opacity = !mixed ? (w.Broad ? ShellBody : 1f) : w.Broad ? ShellBody * Mathf.Lerp(0.12f, 0.60f, _mix) : Mathf.Lerp(0.35f, 1f, _mix),
                    // Where on the horn's one S bend this shell lies (see `Pose`): how much of a wave it spans, and where it starts.
                    BendWaves = (w.To - w.From) / 1.10f, BendAt = 1.2f + look.Seed * 6.28f + w.From / 1.10f * 6.2832f,
                };
            }
            for (int i = 0; i < _vein.Length && shelled; i++)
            {
                var piece = AddShared("veins-" + i, Once("veins-" + i, look, horn, () => FxStudyKit.Shell(Stretch(0.06f, 1.0f, 1.03f * wide, 5), i * 180f + 70f + look.Seed * 360f, 215f, mixed ? -140f : 140f, 30, 24)),
                                FxStudyKit.Ribbon(FxStudyTrails.MaskOf(0)), 12 + i + (mixed ? 4 : 0), _spin);
                FxStudyTrails.Dress(piece, 0).Colours(look.SilkDeep, look.Silk, look.SilkHot, look.SilkHalo).Set("_Tile", 1f, 1f, i * 0.43f, i * 0.5f)
                             .Set("_Add", Mathf.Lerp(0.3f, 1f, light)).Set("_Hue", 8f, 6f, i * 0.37f, 1f);
                // In the mix the veins fade over their top third too, and bend with the horn (they run its whole length).
                if (mixed) piece.Set("_Ends", 0.04f, 0.32f, 0.06f).Set("_Warp", 0.09f * wide, 0.94f / 1.10f, 1.2f + look.Seed * 6.28f + 0.06f / 1.10f * 6.2832f, 0f);
                _vein[i] = piece;
            }

            // ---- the tapes: each on its own tilted ring, turning about that ring's own axis. ⚠️ KEEP (approved in h1).
            for (int i = 0; i < Tapes.Length; i++)
            {
                Tape t = Tapes[i];
                var pivot = Group("ring-" + t.Name);
                pivot.localPosition = new Vector3(0f, Height(t.Height / Tall), 0f);
                pivot.localRotation = Quaternion.Euler(t.TiltX, look.Seed * 360f, t.TiltZ);
                int count = Mathf.Max(4, Mathf.RoundToInt(Mathf.Abs(t.Sweep) / 30f) + 1);
                var knots = new Knot[count];
                for (int k = 0; k < count; k++)
                {
                    float along = k / (float)(count - 1), turn = (t.From + t.Sweep * along) * Mathf.Deg2Rad;
                    // Broad through its middle and down to nothing at both ends. (A root of a hair below nothing is not a
                    // number, and one such width makes the whole mesh vanish: the wisp's swooshes found that out.)
                    float broad = t.Broad * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Lerp(0.05f, 0.95f, along))), 0.5f) + 0.01f;
                    knots[k] = new Knot(new Vector3(Mathf.Cos(turn) * t.Radius * wide, t.Climb * (along - 0.5f), Mathf.Sin(turn) * t.Radius * wide), broad, 84f);
                }
                var piece = AddShared("tape-" + t.Name, Once("tape-" + t.Name, look, horn, () => FxStudyKit.Sweep(knots, Vector3.up, perKnot: 8, across: 6)), FxStudyKit.Ribbon(FxStudyTrails.MaskOf(t.Trail)), 20 + i, pivot);
                // A trail was dressed for a ribbon two metres long. A tape is four or five, so more of its picture lies along
                // it; nothing else of the trail's dressing is touched but its colours and how much of it is light.
                FxStudyTrails.Dress(piece, t.Trail).Colours(look.TapeDeep, look.Tape, look.TapeHot, look.Tape).Set("_Tile", t.Tile, 1f, i * 0.37f, 0f)
                             .Set("_Add", Mathf.Lerp(0.25f, 0.70f, light)).Set("_Hue", 6f, 4f, i * 0.37f, 1f);
                _ring[i] = new Ring { Piece = piece, Turns = t.Turns };
            }

            // A whirlwind stood somewhere else may ask for its hue to wander further (`Look.HueDrift`); the bench's does not.
            if (look.HueDrift > 0f)
            {
                void Wander(FxPiece piece) { if (piece == null || piece.Material == null) return; Vector4 hue = piece.Material.GetVector("_Hue"); hue.x = look.HueDrift; piece.Material.SetVector("_Hue", hue); }
                foreach (var piece in _horn) Wander(piece);
                foreach (var sheet in _sheet) Wander(sheet.Wisp.Piece);
                foreach (var piece in _vein) Wander(piece);
                foreach (var ring in _ring) Wander(ring.Piece);
                Wander(_ground); Wander(_licks);
            }

            // ---- the motes. ⚠️ KEEP (approved in h1).
            _motes = AddFlecks("motes", 40, "glow_soft");
            _motes.Piece.Colours(look.MoteHalo * 0.6f, look.MoteHalo, look.MoteHeart, look.MoteHalo).Set("_Cut", 0.04f, 0.55f).Set("_Stops", 0.30f, 0.55f, 0.92f).Set("_Gain", 1.3f);
        }

        // The height of a share of the horn's own height (0 its foot, 1 its mouth), with the stem and the crown stretched.
        private float Height(float share)
        {
            float y = share * Tall;
            return y <= StemTop ? y * _look.Stem : StemTop * _look.Stem + (y - StemTop) * _look.Crown;
        }

        // The trumpet between two shares of its height, as a shell's profile: `points` of them, each `out_` times as wide.
        private Vector2[] Stretch(float from, float to, float out_, int points)
        {
            var profile = new Vector2[points];
            for (int i = 0; i < profile.Length; i++)
            {
                float share = Mathf.Lerp(from, to, i / (float)(profile.Length - 1)), y = share * Tall;
                int at = 0;
                while (at < Trumpet.Length - 2 && Trumpet[at + 1].y < y) at++;
                float radius = Mathf.LerpUnclamped(Trumpet[at].x, Trumpet[at + 1].x, (y - Trumpet[at].y) / (Trumpet[at + 1].y - Trumpet[at].y));
                profile[i] = new Vector2(Mathf.Max(0.02f, radius) * out_, Height(share));
            }
            return profile;
        }

        public override void Sample(float age, Vector3 eye) => Pose(age, eye, 1f, 0f);

        /// <summary>
        /// Poses the whirlwind for `age`. `stand` is how far it has stood up (0 nothing, 1 whole) and `undone` how far it
        /// has come apart (0 whole, 1 gone); the bench plays it whole.
        /// </summary>
        public void Pose(float age, Vector3 eye, float stand, float undone)
        {
            float turn = age / Seconds * Mathf.PI * 2f, shown = Mathf.Clamp01(stand);
            // The column leans a few degrees and the lean goes round once in four seconds.
            _column.localRotation = Quaternion.Euler(3.0f * Mathf.Sin(turn + _look.Seed * 6.28f), 0f, 3.0f * Mathf.Cos(turn + _look.Seed * 6.28f));
            // Three turns in four seconds.
            _spin.localRotation = Quaternion.Euler(0f, -age * 270f, 0f);
            for (int i = 0; i < _horn.Length; i++)
                if (_horn[i] != null) _horn[i].Frame(age, _look.Body * Skins[i].Shown * shown * (_mixed ? Mathf.Lerp(1f, 0.55f, _mix) : 1f), Mathf.Lerp(Skins[i].Wisps, 1f, undone));
            for (int i = 0; i < _sheet.Length; i++)
            {
                Sheet sheet = _sheet[i];
                if (sheet.Wisp.Piece == null) continue;
                FxStudyWisp.Draw(sheet.Wisp, Mathf.Repeat(age + sheet.Born * Cycle, Cycle), sheet.Opacity * shown * (1f - undone), 0.35f);
                // A wisp bends more the older it gets, each its own way (`Draw` has just set that). In the mix a shell lies on
                // the noise horn's one standing S bend instead, where it is on the horn, so the two bend as one thing.
                if (_mixed) sheet.Wisp.Piece.Set("_Warp", 0.09f * _look.Wide, sheet.BendWaves, sheet.BendAt, 0f);
            }
            foreach (var vein in _vein) if (vein != null) vein.Frame(age, 0.85f * shown * (_mixed ? Mathf.Lerp(0.45f, 1f, _mix) : 1f), undone);
            for (int i = 0; i < _ring.Length; i++)
            {
                _ring[i].Piece.At.localRotation = Quaternion.Euler(0f, -age * 90f * _ring[i].Turns, 0f);
                _ring[i].Piece.Frame(age, 0.80f * shown, undone);
            }
            _ground.At.localRotation = Quaternion.Euler(0f, -age * 90f, 0f);
            _ground.Frame(age, 0.85f * shown, Mathf.Max(0.22f, undone));
            _licks.Frame(age, 0.55f * shown, undone);
            _pool.Frame(age, 0.10f * shown * (1f - undone));

            _motes.Begin(eye);
            for (int i = 0; i < Motes.Length; i++)
            {
                Mote m = Motes[i];
                float life = Mathf.Repeat(age / Seconds + m.Born + _look.Seed, 1f);
                float around = (m.Turn + _look.Seed * 360f - life * 140f) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Cos(around) * m.Out * _look.Wide, Height((m.Low + m.Rise * life) / Tall), Mathf.Sin(around) * m.Out * _look.Wide);
                var going = new Vector3(Mathf.Sin(around) * 0.5f, 1f, -Mathf.Cos(around) * 0.5f);
                // A rounded oval about two to one, lying along the way it is going.
                _motes.Add(at + going.normalized * (m.Size * 0.9f), going, m.Size * 1.8f, m.Size, shown * (1f - undone) * Mathf.Sin(Mathf.PI * life));
            }
            _motes.End(age);
        }
    }
}
