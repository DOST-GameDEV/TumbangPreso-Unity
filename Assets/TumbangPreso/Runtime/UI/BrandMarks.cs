using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The recurring marks, drawn rather than described.
    ///
    /// ⚠️⚠️ `docs/Front_End_Design.md` § 1.2 LISTS SIX SIGNS AND EXACTLY ONE OF THEM EXISTED IN
    /// CODE. That is the shape of the whole § 133.13 rejection one level down: the vocabulary was
    /// written out, the outline was built as `PaperCraft.Surface.Brand`, and the drip, the hatch,
    /// the lean, the sag and the mark were prose. 🧑 asked for them twice by name, once relaying
    /// Paul Andrei (*"maybe pwede natin iincorporate yung crown thingy sa game"*) and once in his
    /// own words: **"i want there to be things we reuse or assets we use to showcase the
    /// personality of our game, like a crown thingy"**.
    ///
    /// ⚠️⚠️ EVERY MARK HERE MEANS EXACTLY ONE THING AND THE LIST IS CLOSED, WHICH IS THE ONLY
    /// REASON A VOCABULARY IS WORTH ANYTHING. § 1.2: *"a vocabulary of six signs a player learns
    /// once is wayfinding; the same six used decoratively is § 92 again"*, and § 92 is
    /// *"theres liek 20 shits at once"*. A seventh sign costs every player a seventh thing to
    /// learn. **Decoration is different and it is allowed**, because a drawing that means nothing
    /// costs nobody anything to ignore: § 1.3 says where it may go and gives the number
    /// (under 1.5:1 against its own ground, or outside every content rect).
    ///
    /// ⚠️ NOTHING IN HERE REPAINTS HIS AUTHORED ART. The pennants, `BUTTON LONG`, `JOIN BUTTON`,
    /// the arrows and the key art are still drawn from the PNGs, and the tsinelas mark is loaded
    /// from `tsinelas_hit.png` rather than redrawn. `docs/VISION.md` § 6.
    ///
    /// ⚠️ AND EVERY COLOUR IS A `UiTheme` BRAND CONSTANT. `CLAUDE.md` § 6.4 bans blue, navy and
    /// cold grey in any layer; there is no `fill` parameter anywhere in this file, for
    /// § 6.5's reason (*"pick a role, not a fill"*), so no caller can introduce one.
    /// </summary>
    public static class BrandMarks
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        // -----------------------------------------------------------------------------------
        // THE SAG: a tarpaulin, which is the lobby's motif and the whole of "filipino-esque"
        // on that screen.
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// A vinyl tarp strung between two points, so its bottom edge is a CURVE and not a rule.
        ///
        /// ⚠️⚠️ THIS IS THE LOBBY'S ANSWER TO *"i want it to be quirky and feel filipino-esque
        /// ... but dont force the filipino shit, i js want it to be felt from it"*. A tarp over a
        /// barangay court is a thing the room is MADE of rather than an ornament applied to it,
        /// so nothing on screen is labelled as Filipino and no decoration is added: a straight
        /// band became a hung one and that is the entire change.
        ///
        /// ⚠️⚠️ IT REPLACES THE CREAM ISLAND RAIL, WHICH IS THE OBJECT § 133.13 IS ABOUT. That
        /// rail held six pills of one size in one row, and it is the single clearest instance of
        /// *"the failed pass drew the red line and kept the grid"*. **The composition is what
        /// changed here, not the palette.**
        ///
        /// ⚠️ IT IS STRETCHED HORIZONTALLY RATHER THAN NINE-SLICED, WHICH IS DELIBERATE AND IS
        /// THE OPPOSITE OF EVERY OTHER SURFACE IN THIS FRONT END. A nine-slice preserves the caps
        /// and stretches the middle, which is exactly wrong for a sag: the dip belongs to the
        /// WHOLE span, so a wider tarp has to sag over a wider distance rather than growing a
        /// longer flat middle. Drawn once at a reference width and scaled, the curve stays a
        /// curve. The cost is that the stroke's horizontal thickness scales with the width, and
        /// that is invisible: at every window this game runs at, the rail is between 1.7x and
        /// 2.2x the reference and the stroke is 9 units of a 190-unit band.
        ///
        /// ⚠️ AND THE TARP RUNS OFF BOTH SCREEN EDGES, so it has no end caps by design. That is
        /// how a tarp is actually hung, and it is the logo's *"escaping its own boundary"*
        /// applied to the largest object on the screen. The ties are separate marks
        /// (<see cref="Tie"/>) for the same reason the sag is not sliced: an eyelet stretched to
        /// twice its width stops being an eyelet.
        /// </summary>
        public static Sprite Tarpaulin()
        {
            const string key = "bm_tarp";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            const int w = 1024;
            const int h = 232;

            // Measured against the band rather than picked: the dip is 11 per cent of the band's
            // own height, which is the shallowest sag that still reads as hung at a glance and
            // the deepest that leaves the room code's 74-unit line clear of the curve.
            // ⚠️ 0.22 OF THE BAND, AND IT WAS 0.11 IN THE VERSION THAT SHIPPED IN
            // `Logs/shots-runtime/Lobby-v84.png`, WHERE IT READ AS A STRAIGHT LINE. The reason
            // is the aspect ratio and it is arithmetic rather than taste: the sprite is 1024 wide
            // and is drawn about 2100 units wide, so the dip is spread over TWICE the distance it
            // was authored at. A 25-unit dip across 2100 units is a slope of about one in eighty,
            // which the eye reads as flat. At 0.22 it is 51 units over the same span and reads as
            // weight.
            const float sag = h * 0.17f;

            // ⚠️ 4, AND IT WAS 9. The tilt and the sag are the same gesture and they ADD: at 9
            // units of lean on top of a 51-unit dip, `Logs/shots-runtime/Lobby-v85.png` reads as
            // a wedge rather than as a hung sheet, with the right end sweeping up past the
            // identity chip. **A tarp is not level and it is also not a flag**, and the sag is
            // the part carrying the meaning, so the lean is what gives way.
            const float tilt = 4.0f;
            const int stroke = 9;
            const int rim = 6;

            var pixels = new Color[w * h];

            for (int x = 0; x < w; x++)
            {
                float u = x / (float)(w - 1);

                // the top edge wanders a little; the bottom edge is the sag
                float top = 10.0f + (tilt * u)
                            + ((Mathf.PerlinNoise(u * 3.1f, 0.7f) - 0.5f) * 5.0f);
                // ⚠️⚠️ THE SAG IS SUBTRACTED FROM THE BASE, AND WITHOUT THAT IT FALLS OFF THE
                // BOTTOM OF THE TEXTURE. The band's base bottom was `h - 22` = 210 in a 232-tall
                // sprite, so adding a 39-unit dip put the middle of the curve at 251 and the
                // sprite CLIPPED it flat: `Logs/shots-runtime/Lobby-v86.png` has a hard
                // horizontal cut across the middle of the sag, which is the one part of the
                // shape carrying the whole idea.
                //
                // ⚠️ AND SUBTRACTING IT IS ALSO THE RIGHT DRAWING RATHER THAN A WORKAROUND. A
                // hung sheet is NARROWEST at its ties and deepest in the middle; with the sag
                // added the band was a constant height that moved down, which is a ribbon being
                // dragged rather than a tarp taking its own weight.
                float bot = (h - 22.0f - sag) + (tilt * u) + (sag * Mathf.Sin(Mathf.PI * u));

                for (int y = 0; y < h; y++)
                {
                    float fromTop = (h - 1) - y;
                    float depth = Mathf.Min(fromTop - top, bot - fromTop);

                    Color c;
                    if (depth < 0.0f) c = Color.clear;
                    else if (depth <= stroke) c = Fade(UiTheme.BrandRed, depth);
                    else if (bot - fromTop < stroke + 4 + rim) c = UiTheme.BrandRimRed;
                    else c = UiTheme.BrandHoney;

                    pixels[(y * w) + x] = c;
                }
            }

            return Store(key, pixels, w, h, 0);
        }

        /// <summary>One eyelet and the cord above it. A tarp is held by something.</summary>
        public static Sprite Tie()
        {
            const string key = "bm_tie";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            const int w = 40;
            const int h = 64;
            var pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fromTop = (h - 1) - y;
                    Color c = Color.clear;

                    // the cord: a short lean, because a cord is never vertical
                    float cordX = 20.0f + (fromTop * 0.28f);
                    if (fromTop < 34.0f && Mathf.Abs(x - cordX) < 3.5f) c = UiTheme.BrandRed;

                    // the eyelet
                    float dx = x - 20.0f, dy = fromTop - 44.0f;
                    float r = Mathf.Sqrt((dx * dx) + (dy * dy));
                    if (r < 11.0f) c = r > 5.5f ? UiTheme.BrandRed : UiTheme.BrandRimRed;

                    pixels[(y * w) + x] = c;
                }

            return Store(key, pixels, w, h, 0);
        }

        // -----------------------------------------------------------------------------------
        // THE TORN EDGE: where the paper stops and the picture starts.
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// The right-hand edge of the login column, drawn rather than ruled.
        ///
        /// ⚠️⚠️ THE SEAM WAS THE WORST THING ON THAT SCREEN AND NO PROBE COULD SEE IT.
        /// `Logs/shots-runtime/SignInBoot-v83.png` is a Honey Quartz column and a piece of key
        /// art meeting at a **perfectly straight vertical line down the middle of the window**.
        /// Every rect fitted its box, every colour was in the palette, and the composition reads
        /// as two images in one window rather than as one screen, because the one edge the player
        /// actually looks at is the one edge in the entire design that no hand drew.
        ///
        /// ⚠️ IT IS THE LOGO'S OWN ARGUMENT APPLIED TO THE LARGEST EDGE IN THE FRONT END.
        /// `docs/TODO.md` § 133.13: *"the character is in things overlapping, leaning, escaping
        /// their own boundary, and being irregular at the OUTSIDE EDGE rather than at the corner
        /// radius."* A rounded corner on a straight-sided panel is a corner radius. This is the
        /// outside edge.
        ///
        /// ⚠️ IT WOBBLES ON Y ONLY AND IS STRETCHED VERTICALLY, WHICH IS THE OPPOSITE CONSTRAINT
        /// FROM EVERY BUTTON IN THIS FILE AND IS CORRECT FOR THE SAME REASON. A button is
        /// nine-sliced horizontally so its variation must live on y; this strip is one column
        /// scaled on y, so its variation must live on **x**. It is drawn at 1200 units against a
        /// canvas that is 1080 to 1350 tall, so the stretch is between 0.9x and 1.13x and the
        /// wobble's wavelength moves by about a tenth, which is under the eye's threshold for a
        /// hand-drawn line and would be well over it for a repeated pattern.
        /// </summary>
        /// <summary>
        /// How far down the page the drip starts, as a fraction of the page's height.
        ///
        /// ⚠️ 0.44, WHICH PUTS IT BESIDE THE FORM RATHER THAN BESIDE THE WORDMARK OR THE
        /// PRIMARY. Higher and it competes with the game's own name, which is the one thing the
        /// login screen is built around (`Front_End_Design.md` § 2.1); lower and it lands level
        /// with CREATE ACCOUNT, and an ornament beside the one action on a screen is
        /// § 1.3's *"anywhere that raises the count of things a player has to SCAN"*.
        /// </summary>
        private const float DripAt = 0.44f;

        /// <summary>⚠️ 0.11 of the page, so on a 1080-unit screen the run is about 120 units:
        /// roughly one form field, which is the scale the wordmark's own drip has against its
        /// letters.</summary>
        private const float DripRun = 0.11f;

        public static Sprite ColumnEdge()
        {
            const string key = "bm_edge";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            // ⚠️ WIDE ENOUGH TO CARRY THE DRIP, because the drip is part of this edge now rather
            // than an object placed against it. See the note below.
            const int w = 176;
            const int h = 1200;

            // ⚠️ 16, AND IT WAS 9. This edge is drawn about 40 units wide against a 1080-unit
            // screen, where a 9-unit stroke is a HAIRLINE: `Logs/shots-runtime/SignInBoot-v86.png`
            // reads it as a thin red rule, which is the straight line it was built to replace
            // wearing the brand colour. **The stroke on this screen has to be the same weight as
            // the stroke on a button**, because it is the same sign meaning the same thing, and
            // on a 132-unit primary that is 11 units.
            const int stroke = 16;

            var pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                // two frequencies and a bow, the same three the brand stroke uses, so the edge of
                // the page and the edge of a button are the same hand
                float n1 = Mathf.PerlinNoise(1.7f, y * 0.010f) - 0.5f;
                float n2 = Mathf.PerlinNoise(8.3f, y * 0.035f) - 0.5f;
                float n3 = Mathf.PerlinNoise(4.1f, y * 0.0035f) - 0.5f;
                float fromTopFrac = ((h - 1) - y) / (float)(h - 1);
                // ⚠️ THE WANDER IS +/- 13 UNITS AND IT WAS +/- 8. Over 1200 units of height at
                // the wavelengths above, 8 units is a line that is very slightly not straight,
                // which the eye files as a rendering artefact rather than as a hand. The whole
                // point of this edge is that somebody drew it.
                float edge = 30.0f + (((n1 * 0.45f) + (n2 * 0.25f) + (n3 * 0.30f)) * 26.0f);

                // ⚠️⚠️ THE DRIP IS A BULGE IN THIS EDGE, NOT AN OBJECT SITTING AGAINST IT, AND
                // TWO RENDERS OF THE OTHER VERSION ARE WHY. Drawn as its own sprite hung beside
                // the seam it came out as a flat-topped bar over a circle:
                // `Logs/shots-runtime/SignInBoot-v87.png` and `-v88.png` both read it as an
                // **exclamation mark**, because two shapes butted against a third have two seams
                // of their own and the eye finds all three.
                //
                // ⚠️ AND IT IS THE MORE FAITHFUL DRAWING ANYWAY. In the wordmark the drip is not
                // a blob placed under the letters: it is the OUTLINE ITSELF running off the
                // bottom-right corner and gathering at the end. So the boundary of the page is
                // the boundary of the drip, one continuous line, which is what makes a hand-drawn
                // mark read as drawn in one movement rather than assembled.
                float bulge = 0.0f;
                float t = (fromTopFrac - DripAt) / DripRun;
                if (t > 0.0f && t < 1.0f)
                {
                    // ⚠️⚠️ THE PROFILE IS ASYMMETRIC AND `sin` IS NOT, WHICH IS WHY THE FIRST
                    // ONE READ AS AN EYE. `Logs/ui/login-seam-v89.png`: a sine lobe is identical
                    // above and below its widest point, so the bulge came out as a lens, and a
                    // lens is a shape somebody drew ON the edge rather than something that ran
                    // DOWN it. **A drip is heavier at the bottom than at the top; that is the
                    // entire reason it fell.**
                    //
                    // ⚠️ `t^1.4 * (1-t)^0.5` PEAKS AT t = 1.4 / 1.9 = 0.74, so the run leaves the
                    // edge narrow, gathers its weight three quarters of the way down and closes
                    // off quickly under it. The 3.02 is the normaliser that puts that peak back
                    // at the full width rather than a number anybody chose: the raw expression
                    // maxes at 0.331.
                    bulge = 3.02f * 62.0f
                            * Mathf.Pow(t, 1.4f) * Mathf.Pow(1.0f - t, 0.5f);
                }

                float right = edge + Mathf.Max(0.0f, bulge);

                for (int x = 0; x < w; x++)
                {
                    Color c;
                    if (x < right - stroke)
                    {
                        // ⚠️ THE DRIP'S INSIDE IS THE LOGO'S OWN ORANGE AND THE PAGE'S IS HONEY,
                        // which is the one place the two fills meet. In the mark the drip is the
                        // only shape that is not the letter fill, and it is what makes it read as
                        // something that ran rather than as the page bulging.
                        c = x > edge - stroke ? UiTheme.BrandGolden : UiTheme.BrandHoney;
                    }
                    else if (x < right) c = UiTheme.BrandRed;
                    else c = Color.clear;

                    pixels[(y * w) + x] = c;
                }
            }

            return Store(key, pixels, w, h, 0);
        }

        // -----------------------------------------------------------------------------------
        // THE DRIP: "there is more below".
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// The orange run escaping a bottom edge, ending in a blob.
        ///
        /// ⚠️ IT IS TAKEN STRAIGHT OFF THE WORDMARK, where the orange runs off the bottom-right
        /// corner and finishes in a swirl. § 1.2 gives it one meaning and only one: **there is
        /// more below.** A list that scrolls drips at its bottom edge and a list that does not,
        /// does not, so the mark answers a question the player would otherwise have to answer by
        /// dragging.
        ///
        /// ⚠️ IT IS DRAWN HANGING OUTSIDE ITS PARENT'S RECT ON PURPOSE, which is the one place in
        /// this front end where that is correct: the whole point of the sign is a thing escaping
        /// its own boundary, and a drip tucked inside a card is a stain.
        /// </summary>
        public static Sprite Drip()
        {
            const string key = "bm_drip";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            const int w = 72;
            const int h = 104;
            const int stroke = 6;
            var pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fromTop = (h - 1) - y;

                    // ⚠️⚠️ THE RUN BARELY NARROWS AND THE BLOB IS INSIDE IT, AND THE FIRST
                    // VERSION GOT BOTH WRONG. It tapered from 15 units to 5.5 over 62 rows and
                    // then put a detached 14-unit circle under the point, which is not a drip:
                    // `Logs/shots-runtime/SignInBoot-v87.png` reads it as an **exclamation
                    // mark**, a narrow triangle over a dot. A running drip is nearly the same
                    // width all the way down and gathers weight at the bottom, because what
                    // makes it fall is that the bottom is heavier than the top.
                    //
                    // ⚠️ SO THE TAPER IS 20 TO 13 AND THE BLOB'S CENTRE SITS INSIDE THE RUN'S
                    // LAST ROWS RATHER THAN BELOW THEM. The two shapes are unioned, so with the
                    // centre inside there is no pinch where they meet and the silhouette is one
                    // continuous outline, which is how every shape in the mark is drawn.
                    float t = Mathf.Clamp01(fromTop / 74.0f);
                    float half = Mathf.Lerp(20.0f, 13.0f, t);
                    float cx = 30.0f + (fromTop * 0.09f);

                    float d = half - Mathf.Abs(x - cx);

                    float bx = x - 38.0f, by = fromTop - 72.0f;
                    float blob = 19.0f - Mathf.Sqrt((bx * bx) + (by * by));

                    float depth = Mathf.Max(fromTop <= 74.0f ? d : -1.0f, blob);

                    Color c;
                    if (depth < 0.0f) c = Color.clear;
                    else if (depth <= stroke) c = Fade(UiTheme.BrandRed, depth);
                    else c = blob > stroke ? UiTheme.BrandPersimmon : UiTheme.BrandGolden;

                    pixels[(y * w) + x] = c;
                }

            return Store(key, pixels, w, h, 0);
        }

        // -----------------------------------------------------------------------------------
        // THE BURST: the one thing on the screen is about to happen.
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// A hand-drawn impact behind the ONE action on a screen.
        ///
        /// ⚠️⚠️ 🧑 ASKED FOR THIS IN AS MANY WORDS: **"i want start match button to have genuine
        /// emphases and look adn feel good to press"**. The sprite alone cannot answer that. A
        /// button is emphatic because of what is AROUND it, and the four ordering tools put
        /// position, size and space before colour: the primary already has the corner every
        /// console flow uses, the largest size on the screen and the only chartreuse in the
        /// palette, and what it did not have is anything saying the press MATTERS.
        ///
        /// ⚠️ IT IS THE MARK'S OWN BURST, not a glow. `tsinelas_hit.png` is a slipper with an
        /// impact drawn behind it, so the game's own art already answers "what does a hit look
        /// like in this hand": irregular spokes, flat colour, no blur. A soft radial glow would
        /// be the one thing in the front end drawn by nobody's hand.
        ///
        /// ⚠️ AND IT IS UNDER 1.5:1 AGAINST ITS GROUND, which is § 1.3's number for an ornament.
        /// It sits BEHIND the primary and outside its rect, it carries no meaning, and it must
        /// never compete with the lettering on top of it.
        /// </summary>
        public static Sprite Burst()
        {
            const string key = "bm_burst";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            const int w = 512;
            const int h = 256;
            var pixels = new Color[w * h];

            const float cx = w * 0.5f;
            const float cy = h * 0.5f;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // measured in an ellipse so the spokes reach further sideways than up: the
                    // control this sits behind is three times as wide as it is tall
                    float dx = (x - cx) / (w * 0.5f);
                    float dy = (y - cy) / (h * 0.5f);
                    float r = Mathf.Sqrt((dx * dx) + (dy * dy));

                    float a = Mathf.Atan2(dy, dx);

                    // thirteen spokes, uneven, because a drawn burst is not a starburst gradient
                    float spoke = Mathf.PerlinNoise((Mathf.Cos(a) * 2.3f) + 4.0f,
                                                    (Mathf.Sin(a) * 2.3f) + 4.0f);
                    float reach = 0.52f + (spoke * 0.46f);

                    Color c = Color.clear;
                    if (r > 0.30f && r < reach)
                    {
                        float t = Mathf.InverseLerp(0.30f, reach, r);
                        var tint = UiTheme.BrandGolden;
                        tint.a = (1.0f - t) * 0.34f;
                        c = tint;
                    }

                    pixels[(y * w) + x] = c;
                }

            return Store(key, pixels, w, h, 0);
        }

        // -----------------------------------------------------------------------------------
        // THE HATCH: "this is not available".
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// Diagonal strokes, tiled, over anything locked or disabled.
        ///
        /// ⚠️ IT REPLACES A TINT AND THAT IS AN UPGRADE RATHER THAN A SWAP. `game-ui-design`'s
        /// **Color-Only Information** anti-pattern is explicit that a control distinguishable
        /// only by colour is not distinguishable, and this project has a measured colourblind
        /// problem (`docs/TODO.md` § 16.1). A hatched control is legibly unavailable in
        /// greyscale, at a glance, to everybody. `PaperButton`'s own note argues it from the
        /// other side: *"the disabled state is a pose, not a tint"*.
        ///
        /// ⚠️ THE STROKES ARE IN THE MARK'S GREY-ON-CHARTREUSE, which is the one place the logo
        /// uses hatching: the shading across the `1` and across the blob. So the sign is quoted
        /// from the drawing rather than invented.
        /// </summary>
        public static Sprite Hatch()
        {
            const string key = "bm_hatch";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            const int s = 32;
            var pixels = new Color[s * s];

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    int d = (x + y) % 16;
                    var c = UiTheme.PaperInk;
                    c.a = d < 5 ? 0.22f : 0.0f;
                    pixels[(y * s) + x] = c;
                }

            var sprite = Store(key, pixels, s, s, 0);
            sprite.texture.wrapMode = TextureWrapMode.Repeat;
            return sprite;
        }

        // -----------------------------------------------------------------------------------
        // CHALK: decoration, and the only thing in this file that means nothing.
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// Scribbles on the asphalt. This game is chalk on a road.
        ///
        /// ⚠️⚠️ IT IS DECORATION AND IT IS ALLOWED, WHICH IS § 1.3 AND 🧑'S OWN INSTRUCTION:
        /// **"u can add random shit and designs to the ui too btw to give our screens character,
        /// not everything has to be functional"**. The resolution against § 92's *"theres liek 20
        /// shits at once"* is not "how much", it is WHERE: *decoration is free where nothing has
        /// to be read, and expensive where something does.* Six buttons in six visual languages
        /// were six things the player had to look at, decide about and dismiss. **A drawing that
        /// means nothing costs none of that.**
        ///
        /// ⚠️ SO IT ONLY EVER GOES IN THE DEAD GROUND, and § 118.1 row 2 measured how much of that
        /// there is: **680 units of nothing down the lobby's left side and 475 down its right.**
        /// That is not space that needs protecting, it is space that is already doing nothing.
        ///
        /// ⚠️ AND IT SITS UNDER § 1.3'S RATIO. Cream at 0.16 alpha on asphalt is well under 1.5:1,
        /// so it cannot compete with anything at `Caption` or larger, all of which measure 5:1 or
        /// better. **A drawing that fails that ratio is not decoration, it is a seventh sign.**
        /// </summary>
        public static Sprite Chalk(int variant)
        {
            string key = $"bm_chalk_{variant}";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            const int w = 256;
            const int h = 128;
            var pixels = new Color[w * h];

            float seed = 3.7f + (variant * 11.3f);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) pixels[(y * w) + x] = Color.clear;

            // three wandering strokes and a cross, which is what a hopscotch grid on a street
            // actually looks like once it has been walked on
            for (int s = 0; s < 3; s++)
            {
                float baseY = 26.0f + (s * 34.0f);
                for (int x = 6; x < w - 6; x++)
                {
                    float ny = baseY
                               + ((Mathf.PerlinNoise(seed + (s * 5.0f), x * 0.017f) - 0.5f) * 26.0f);
                    Ink(pixels, w, h, x, Mathf.RoundToInt(ny), 3);
                }
            }

            for (int i = -14; i <= 14; i++)
            {
                Ink(pixels, w, h, 200 + i, 64 + i, 3);
                Ink(pixels, w, h, 200 + i, 64 - i, 3);
            }

            return Store(key, pixels, w, h, 0);
        }

        private static void Ink(Color[] pixels, int w, int h, int cx, int cy, int r)
        {
            var c = UiTheme.Cream;
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || x >= w || y < 0 || y >= h) continue;
                    float d = Mathf.Sqrt((dx * dx) + (dy * dy));
                    if (d > r) continue;
                    var t = c;
                    t.a = Mathf.Max(pixels[(y * w) + x].a, 1.0f - (d / r));
                    pixels[(y * w) + x] = t;
                }
        }

        // -----------------------------------------------------------------------------------

        /// <summary>
        /// The mark, and it is loaded rather than drawn.
        ///
        /// ⚠️⚠️ IT IS 🧑'S OWN ART AND `docs/VISION.md` § 6 SAYS HIS UI ART IS THE DESIGN SYSTEM.
        /// `tsinelas_hit.png` is the tsinelas with an impact behind it, drawn in the same hand as
        /// the wordmark, and `tools/build_brand_art.py` already keys its page to alpha.
        /// **Redrawing it here would be repainting his art to satisfy a rule**, which § 6.4
        /// forbids in as many words.
        ///
        /// ⚠️ ONE PER SCREEN, AND IT ONLY EVER SAYS "THIS ONE". § 1.1: the moment it appears
        /// twice it stops meaning "this one" and becomes a bullet, and a bullet is decoration.
        /// It is not pressable and it never navigates, so it can never become § 6.3's second
        /// door.
        /// </summary>
        /// <remarks>
        /// ⚠️⚠️ IT RETURNS A `Texture2D` AND NOT A `Sprite`, AND THE FIRST VERSION RETURNED A
        /// SPRITE AND DREW A WHITE BOX ON EVERY SEAT. `tsinelas_hit.png.meta` carries
        /// `textureType: 0` and `spriteMode: 0`, so the asset is a plain texture and
        /// `Resources.Load&lt;Sprite&gt;` answers **null** for a file that is right there. An
        /// `Image` with a null sprite draws a filled white rectangle, which is why
        /// `Logs/shots-runtime/Lobby-v84.png` has a blank white card beside the player's own
        /// nameplate.
        ///
        /// ⚠️ THIS IS THE THIRD TIME AND `SignInScreen.BuildLogo` ALREADY RECORDS THE OTHER TWO,
        /// in its own words: *"the render showed the fallback label and nothing said why"*. Its
        /// conclusion is the one followed here: **a `.meta` is a file nobody edits by hand and a
        /// re-import can reset it**, so the caller draws a `RawImage` and takes whatever import
        /// settings the file arrived with, instead of the asset being made to match the code.
        /// </remarks>
        public static Texture2D Mark() => Resources.Load<Texture2D>("UI/brand/tsinelas_hit");

        // -----------------------------------------------------------------------------------

        private static Color Fade(Color c, float depth)
        {
            // the same one-unit feather every surface in `PaperCraft` uses, so a mark's edge and
            // a button's edge are the same edge
            if (depth >= 1.0f) return c;
            var f = c;
            f.a *= Mathf.Clamp01(depth);
            return f;
        }

        private static Sprite Store(string key, Color[] pixels, int w, int h, int cap)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = key,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            texture.SetPixels(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f),
                                       100.0f, 0, SpriteMeshType.FullRect,
                                       new Vector4(cap, 0, cap, 0));
            sprite.name = key;
            sprite.hideFlags = HideFlags.HideAndDontSave;

            Cache[key] = sprite;
            return sprite;
        }
    }
}
