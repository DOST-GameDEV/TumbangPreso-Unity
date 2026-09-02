using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The front end's surfaces: three MATERIALS built three different ways, sharing one colour
    /// system taken from 🧑's own art.
    ///
    /// 🧑 2026-09-01, on the pass that had already replaced every button and every plate:
    /// *"ui still looks unnatural and ugly"*, then the diagnosis in his own words: *"the issue
    /// with old UI is everything feels repetitive bcz i think u use the same code to generate
    /// them all"*, and, sharpening it: **"make sure all ui isnt generated in the same way but
    /// follows a central theme bcz old issue was it read as repetitive with everyone just being
    /// brown and boring"**.
    ///
    /// ⚠️⚠️ THAT IS TWO SEPARATE COMPLAINTS AND THE PREVIOUS TWO PASSES EACH ANSWERED ONE AND
    /// BROKE THE OTHER. `GodotTheme.WoodBox` gave every surface in the game one construction, so
    /// the screen read as a form. `UiMaterials` then added grain and a lit edge, which is more
    /// variety inside the SAME construction, so the screen read as a slightly nicer form. Neither
    /// touched the thing he is actually pointing at: **the whole front end is brown rectangles**,
    /// and no amount of bevel fixes brown-on-brown-on-brown.
    ///
    /// **So this file draws three materials that are not variations of each other.**
    ///
    /// | Material | What it is | How it is built | What it carries |
    /// |---|---|---|---|
    /// | **WOOD** | the furniture and the frame | keyline, dark rim, varnish band, vertical ramp | buttons, headers, cards, the rails |
    /// | **PAPER** | a cream label pinned to the wood | flat, fibre speckle, NO bevel and NO ramp, one ink hairline | anything you READ or TYPE: fields, list rows, values |
    /// | **SLATE** | the asphalt the game is played on | matte near-black, no keyline, a single lit lip along the top | logs, wells, anything chalk is drawn on |
    ///
    /// ⚠️⚠️ THE POINT IS THAT PAPER IS NOT BROWN. `CLAUDE.md` § 6.4 fixes the palette at wood,
    /// cream, amber and ink and forbids a fifth hue, and every pass so far read that as "use
    /// wood", leaving cream for type only. **Cream is a SURFACE in his own art**: the login
    /// screen's input boxes are cream plates on a wood column, and they are the only thing on
    /// that screen that is not brown. Promoting cream from a text colour to a material is the one
    /// move that breaks the monotony without inventing a colour, and it is already how he draws
    /// it.
    ///
    /// ⚠️ AND SLATE IS THE GAME'S OWN GROUND. `VISION.md` opens on a street game and § 2 rule 5
    /// names the chalk and the asphalt as things a frame must show; `MapGeometryCheck` gates the
    /// chalk box in every arena. A log drawn as dark asphalt with a chalk rule on it is this
    /// game's surface and nothing else's, and it costs no new colour at all.
    ///
    /// -----------------------------------------------------------------------------------------
    /// THE CENTRAL THEME: one colour ramp, measured off the authored art rather than chosen.
    /// -----------------------------------------------------------------------------------------
    ///
    /// ⚠️⚠️ EVERY NUMBER IN THE WOOD SECTION WAS SAMPLED FROM `Art/ui/host-game/*.png`, NOT
    /// PICKED. The four authored pieces are one construction with two silhouettes:
    ///
    /// | Piece | Silhouette | Keyline | Rim | Face peak | Face floor |
    /// |---|---|---|---|---|---|
    /// | `BUTTON LONG` | chamfer, 30 px cut at 135 tall | `99572b`, 7 px | `612e15`, 6 px | `793e1f` | `421806` |
    /// | `TEXT FIELD` | chamfer, same cut | `99572b`, 7 px | `3f1a0c`, 6 px | `4e2211` | `2a0d03` |
    /// | `MAP MODE DISPLAY` | round, ~10 px radius | `99572b`, 6 px | `612e15` | `793e1f` | `421807` |
    /// | `SETTINGS CONFIG PANEL` | round, hand-wobbled | `99572b`, 5 px | `612d15` | `783e1f` | `4a1b07` |
    /// | `JOIN BUTTON` | chamfer, same cut | `90ea40`, 7 px | `3caf2d` | `51dd38` | `188427` |
    ///
    /// **Read the last row across and the system falls out**: the green button is the brown button
    /// with one colour swapped. Keyline, rim and every stop of the face are the SAME COLOUR at
    /// different VALUES, so one base colour generates a whole control. That is why his pieces look
    /// like each other, and why nothing drawn in code looked like any of them: code drew a
    /// rounded rect with a DARK outline, and every piece he authored is a chamfered slab with a
    /// BRIGHT one. Opposite constructions, both on screen at once in the lobby.
    ///
    /// ⚠️⚠️ THE GRADIENT IS NOT A RAMP, AND THAT IS THE DETAIL THAT MAKES IT PAINTED WOOD. Down
    /// the centre of `BUTTON LONG` the face reads `6d371b` at 10 per cent, `793e1f` at 25,
    /// `6e3619` at 50, `59270f` at 75 and `421806` at 92: slightly DARK under the keyline, a PEAK
    /// a quarter of the way down, then a fall to near black. A monotonic top-to-bottom lerp, which
    /// is what `UiMaterials.Plank` draws, is the one thing this is not. The bright band at 0.25 is
    /// a varnish highlight and it is the difference between a slab of paint and a piece of
    /// furniture.
    ///
    /// ⚠️⚠️ AND THE SILHOUETTE SAYS WHAT KIND OF THING IT IS, WHICH IS THE OTHER HALF OF THE
    /// ANSWER TO "THEY ALL LOOK THE SAME". In his art a CHAMFER means you can touch it (button,
    /// field) and a ROUND means you cannot (panel, display). `game-ui-design`'s ordering tools are
    /// position, size, weight and colour in that order: shape sits above colour in every one of
    /// them, and a colourblind player reads a chamfer.
    ///
    /// ⚠️ NOTHING HERE REPAINTS HIS ART. The pennants, `BUTTON LONG`, `JOIN BUTTON`, the arrow
    /// textures and the key art are untouched and still drawn from the PNGs. This is what the
    /// surfaces AROUND them are made of, so a code-built row and an authored button can share a
    /// rail without one of them looking like a mistake.
    /// </summary>
    public static class WoodCraft
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// The surfaces this front end is allowed to be made of, and there are seven.
        ///
        /// ⚠️⚠️ IT IS A CLOSED LIST ON PURPOSE. The failure this file replaces is a screen made
        /// of twelve plates that were all the same call with a different fill, and the way that
        /// happened is that the fill was a free parameter. Here the CALLER PICKS A ROLE and the
        /// material, the silhouette, the relief and the colour all follow from it, so two
        /// surfaces with different jobs cannot accidentally come out identical and a new job has
        /// to be argued for in this enum rather than invented at a call site.
        /// </summary>
        public enum Surface
        {
            /// <summary>A pressable wooden control. Chamfered, raised, varnish band.</summary>
            Button,

            /// <summary>The one primary action on a screen. Amber, chamfered, heavier keyline.
            /// </summary>
            Action,

            /// <summary>A wooden card or rail: the furniture things sit on. Rounded, raised.
            /// </summary>
            Panel,

            /// <summary>A section header: rounded at the bottom, square at the top, so it reads
            /// as a sign hung on the panel below it rather than as another card.</summary>
            Header,

            /// <summary>
            /// A tab: cut at the TOP two corners and square along the bottom, so it stands on the
            /// row rather than floating in it.
            ///
            /// ⚠️⚠️ IT EXISTS BECAUSE THE LIVE TAB WAS BORROWING <see cref="Header"/> AND THAT
            /// SHAPE IS UPSIDE DOWN FOR THIS JOB. 🧑 2026-09-01, with a crop of the sign-in row:
            /// **"its also weird that create is just a rectanhle"**. He is describing the top
            /// edge: `Header` is SQUARE along the top and rounded below, which is exactly right
            /// for a sign nailed across the top of the drawer under it and exactly wrong for a
            /// tab, where the square end is the one that should be sitting on the row.
            ///
            /// ⚠️ SO IT IS THE CHAMFER, FLIPPED. The cut is the same 45 degrees at the same 0.22
            /// of the height that every button in this front end uses, applied to the top two
            /// corners only. A tab is then visibly a member of the same family as the buttons
            /// without being mistakable for one, which is the whole reason `Surface` is a list of
            /// roles rather than a list of fills.
            /// </summary>
            Tab,

            /// <summary>
            /// A dark wooden slot you type into. Chamfered like a button, because you touch it,
            /// and near-black because it is cut in rather than standing out.
            ///
            /// ⚠️ IT IS CHAMFERED AND `Slate` IS NOT, WHICH IS THE DISTINCTION THAT MATTERS.
            /// Both are dark. A field is a CONTROL and takes the pressable silhouette; a well is
            /// furniture you read out of and takes the rounded one. Without that, the chat's log
            /// and the chat's input would be one tall dark rectangle with a line across it.
            /// </summary>
            Field,

            /// <summary>A cream label or value plate. Flat, fibre, one ink hairline. Ink type.
            /// </summary>
            Paper,

            /// <summary>A cream plate you can type into. Paper, plus a sunk inner shadow.
            /// </summary>
            PaperField,

            /// <summary>Asphalt: a matte near-black well with a lit lip. Chalk and cream type.
            /// </summary>
            Slate,
        }

        // -----------------------------------------------------------------------------------
        // THE WOOD RAMP. Seven values of ONE colour.
        //
        // ⚠️⚠️ THESE ARE MULTIPLIERS ON VALUE (the V of HSV), NOT COLOURS. Converting `BUTTON
        // LONG` and `JOIN BUTTON` to HSV gives the same seven ratios against each piece's own
        // peak, with hue and saturation held: that is the whole reason a green button and a brown
        // button by the same hand read as siblings. Writing the stops as hexes would be writing
        // ONE control down and needing a second table for every other colour, which is exactly how
        // `GodotTheme` ended up with four rectangles differing only by fill.
        //
        // ⚠️ SATURATION CREEPS UP AS VALUE FALLS, which is what paint does in shadow and what his
        // ramp measurably does: `793e1f` is 74 per cent saturated and `421806` is 91. Without it
        // the bottom of every control goes grey-brown and the piece stops looking like one object.
        // -----------------------------------------------------------------------------------

        private const float KeylineValue = 1.28f;
        private const float KeylineSat = 0.97f;
        private const float RimValue = 0.81f;
        private const float RimSat = 1.05f;

        /// <summary>Down the face: shaded under the keyline, the varnish band, mid, falling, and
        /// the floor. Measured at 0.10 / 0.25 / 0.50 / 0.75 / 0.92 of the authored faces.
        /// </summary>
        private static readonly float[] Stops = { 0.0f, 0.22f, 0.50f, 0.78f, 1.0f };
        private static readonly float[] RaisedValues = { 0.88f, 1.00f, 0.92f, 0.74f, 0.55f };
        private static readonly float[] RaisedSats = { 1.00f, 1.00f, 1.02f, 1.05f, 1.08f };

        /// <summary>Measured: `BUTTON LONG` is 818x135 with a 45-degree cut of about 30 px, which
        /// is 0.22 of the HEIGHT. A fraction of height and never of width, which is why a long
        /// button and a short one by the same hand have the same end angle.</summary>
        private const float ChamferFraction = 0.22f;

        /// <summary>Measured off `MAP MODE DISPLAY`: about 10 px of radius at 107 tall.</summary>
        private const float RoundFraction = 0.09f;

        /// <summary>The keyline, as a fraction of height. `BUTTON LONG`: 7 px at 135.</summary>
        private const float KeylineFraction = 0.052f;

        /// <summary>The dark rim inside the keyline. About 6 px at 135.</summary>
        private const float RimFraction = 0.045f;

        /// <summary>
        /// One surface, at one height.
        ///
        /// ⚠️⚠️ IT IS SLICED HORIZONTALLY ONLY AND THAT IS WHY IT TAKES A HEIGHT. The border it
        /// returns is `(cap, 0, cap, 0)`, so Unity stretches the middle COLUMNS and never the
        /// rows: the vertical gradient, the varnish band and the top and bottom keylines arrive on
        /// screen as authored at any width. `UiMaterials.CarvedButton` records the opposite
        /// approach and its cost in its own header: a four-sided nine-slice smears any gradient
        /// across the centre row, so that function keeps its face FLAT, and the varnish band,
        /// which is most of what makes his art look like wood, could not exist there at all.
        ///
        /// ⚠️ THE CALLER MUST DRAW IT AT THIS HEIGHT, and <see cref="WoodSkin"/> is what
        /// guarantees that: it watches its own rect and regenerates when the height moves. A
        /// sprite drawn at a height it was not built for stretches the gradient, which is the
        /// exact fault this signature exists to prevent.
        ///
        /// ⚠️ HEIGHTS QUANTISE TO 4 UNITS FOR THE CACHE KEY. A layout settling at 63.4 and then
        /// 63.6 would otherwise bake two textures for one control, and this is called from a
        /// layout callback.
        /// </summary>
        /// <summary>
        /// The tallest surface that gets a real, unstretched, full-height gradient.
        ///
        /// ⚠️⚠️ ABOVE THIS THE SPRITE IS SLICED ON ALL FOUR SIDES INSTEAD, AND THAT IS A
        /// CORRECTION RATHER THAN AN OPTIMISATION. `Slab` slices horizontally only, so the sprite
        /// is correct at exactly the height it was built for; the height was also being CLAMPED,
        /// so a 1080-unit sign-in column asked for 1080, got a 320-unit texture and had it
        /// stretched three and a half times. **The varnish band smeared over a third of the
        /// screen and the whole column read as a flat brown slab**, which is visible in
        /// `Logs/ui/07-signin.png` and was the one surface in that pass that did not improve.
        ///
        /// ⚠️ 400 IS MEASURED, NOT PICKED. `SETTINGS CONFIG PANEL.png` is 845x379 and carries a
        /// real top-to-bottom gradient, so a panel of that size is inside what 🧑 himself draws
        /// by hand. Nothing in the front end between 400 and the full screen height is a plate a
        /// varnish highlight would help; a full-height column wants a keyline, a warm face and
        /// nothing else, because a highlight a third of the way down a 1080-unit board is not a
        /// highlight, it is a horizon.
        /// </summary>
        internal const int TallSurface = 400;

        /// <summary>
        /// The same shape, painted solid, for the drop shadow under a control.
        ///
        /// ⚠️⚠️ A CHAMFERED BUTTON WITH A ROUNDED SHADOW IS WHAT 🧑 SAW AND NAMED: *"the shadows
        /// dont follow the actual ckickables as well"*, with a crop of CREATE ACCOUNT. Every wood
        /// control draws a cartoon drop shadow six units grown and five down
        /// (`SkinLayers.Shadow`), and that shadow was `GodotTheme.ShadowBox()`, **a rounded
        /// rectangle**, from the era when every face was one too. The faces are chamfered now, so
        /// the shadow poked out of all four cut corners: the control read as a chamfered slab
        /// sitting on a rounded one.
        ///
        /// ⚠️ IT IS WHITE AND TINTED BY THE CALLER, so one silhouette per shape and height serves
        /// every shadow colour and the alpha stays in `Image.color`. The edge keeps the same
        /// one-pixel anti-aliasing the face has, which is what stops a shadow reading as a
        /// stair-stepped halo at the corners.
        /// </summary>
        public static Sprite Silhouette(Surface surface, float height)
        {
            bool tall = height > TallSurface;
            int h = tall ? 96 : Mathf.Clamp(Mathf.RoundToInt(height / 4.0f) * 4, 20, TallSurface);

            string key = $"wcsil_{surface}_{h}_{tall}";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            bool chamfer = surface == Surface.Button || surface == Surface.Action
                           || surface == Surface.Field;
            bool sign = surface == Surface.Header;
            bool tab = surface == Surface.Tab;

            float corner = chamfer || tab ? h * ChamferFraction
                                          : Mathf.Max(6.0f, h * RoundFraction);
            int cap = Mathf.CeilToInt(corner) + 2;
            int width = (cap * 2) + 4;

            var pixels = new Color[width * h];

            float midX = (width - 1) * 0.5f;
            float midY = (h - 1) * 0.5f;
            float halfW = midX + 0.5f;
            float halfH = midY + 0.5f;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < width; x++)
                {
                    float dx = halfW - Mathf.Abs(x - midX);
                    float dy = halfH - Mathf.Abs(y - midY);

                    // ⚠️ THE SHADOW READS THE SAME SHAPE RULES AS THE FACE, which is the whole
                    // point of this method: 🧑 caught a rounded shadow under a chamfered button
                    // (*"the shadows dont follow the actual ckickables as well"*) and a tab would
                    // have been the same fault one shape later.
                    bool upper = y > midY;
                    bool square = (sign && upper) || (tab && !upper);

                    float depth = square
                        ? Mathf.Min(dx, dy)
                        : Depth(dx, dy, corner, chamfer || (tab && upper), false);

                    pixels[(y * width) + x] =
                        new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp01(depth));
                }

            var made = Finish(pixels, width, h, cap, tall, key);
            Cache[key] = made;
            return made;
        }

        public static Sprite Slab(Surface surface, float height, Pose pose = Pose.Rest,
                                  Color? overrideBase = null)
        {
            // ⚠️ A TALL SURFACE IS BUILT AT A FIXED HEIGHT AND SLICED ON ALL FOUR SIDES, so its
            // keylines and rims survive at the top and bottom and only the flat middle stretches.
            // That is the ordinary nine-slice behaviour, and it is right here for the same reason
            // it is wrong on a button: there is no gradient left to smear.
            bool tall = height > TallSurface;
            int h = tall ? 96 : Mathf.Clamp(Mathf.RoundToInt(height / 4.0f) * 4, 20, TallSurface);

            Color baseColour = overrideBase ?? BaseFor(surface);
            string key = $"wc_{surface}_{pose}_{h}_{tall}_{ColorUtility.ToHtmlStringRGB(baseColour)}";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            Sprite made = surface == Surface.Paper || surface == Surface.PaperField
                ? PaintPaper(surface, baseColour, h, tall, key)
                : surface == Surface.Slate
                    ? PaintSlate(baseColour, h, tall, key)
                    : PaintWood(surface, baseColour, h, pose, tall, key);

            Cache[key] = made;
            return made;
        }

        /// <summary>
        /// What the pointer is doing to a wooden control.
        ///
        /// ⚠️⚠️ A PRESS INVERTS THE GRADIENT RATHER THAN DARKENING THE FILL, AND THAT IS THE
        /// WHOLE REASON A PRESS READS AS A PRESS WITHOUT THE BUTTON MOVING. The varnish band is a
        /// highlight from a light source above the screen; push the object in and the light now
        /// falls on the far wall, so the band goes to the BOTTOM. Darkening the whole face
        /// instead is what every flat UI does and it reads as "disabled", which is the one state
        /// it must not be confusable with. `GodotButton` still sinks the LABEL by five units on
        /// top of this, which is Godot's own behaviour ported.
        /// </summary>
        public enum Pose
        {
            Rest,
            Hover,
            Press,
            Off,
        }

        /// <summary>The colour each surface is made of when the caller does not override it.
        /// ⚠️ Every one of these is in `UiTheme`; nothing names a hex here. `Art_Direction.md`
        /// § 1: the palette file is the only place a colour is named.</summary>
        private static Color BaseFor(Surface surface)
        {
            switch (surface)
            {
                case Surface.Action: return UiTheme.Amber;
                case Surface.Button: return UiTheme.WoodFace;
                case Surface.Header: return UiTheme.WoodFace;
                case Surface.Tab: return UiTheme.WoodFace;
                case Surface.Panel: return UiTheme.WoodPanelFace;
                case Surface.Field: return UiTheme.WoodFieldFace;
                case Surface.Paper: return UiTheme.Card;
                case Surface.PaperField: return UiTheme.Card;
                case Surface.Slate: return UiTheme.Asphalt;
                default: return UiTheme.WoodFace;
            }
        }

        // -----------------------------------------------------------------------------------
        // WOOD
        // -----------------------------------------------------------------------------------

        private static Sprite PaintWood(Surface surface, Color baseColour, int h, Pose pose,
                                        bool tall, string key)
        {
            bool chamfer = surface == Surface.Button || surface == Surface.Action
                           || surface == Surface.Field;
            bool sign = surface == Surface.Header;
            bool tab = surface == Surface.Tab;

            // ⚠️ A FIELD IS LIT FROM BELOW, WHICH IS WHAT MAKES IT READ AS CUT IN. The light is
            // above the screen, so a raised board carries its varnish band near the top and the
            // near wall of a recess is the one in shadow. His `TEXT FIELD.png` measures exactly
            // that: `461e0f` at 10 per cent down against `4e2211` at 25, so the top is the DARK
            // end. Inverting the ramp is one line and it is the whole difference between a slot
            // and a very dark button.
            bool recessed = surface == Surface.Field;

            // ⚠️ THE POSE MOVES THE LIGHT, NOT THE PAINT. See `Pose`: only `Off` changes the
            // colour itself, because a disabled control is genuinely a different object and every
            // other state is the same object under a different light.
            bool inverted = (pose == Pose.Press) ^ recessed;

            if (pose == Pose.Hover) baseColour = Shift(baseColour, 1.10f, 0.98f);
            else if (pose == Pose.Press) baseColour = Shift(baseColour, 0.92f, 1.0f);
            // ⚠️⚠️ A DISABLED CONTROL GOES DARKER, NOT GREYER, AND 0.55 OF DESATURATION WAS
            // GREY. `CLAUDE.md` § 6.4 bans cold grey anywhere in the UI, and the browser rows in
            // the join panel are eight disabled buttons in a column: at 0.55 they came out a
            // washed grey-brown, which is the one hue family this front end has been told five
            // times to stop drawing. 0.22 keeps the wood warm and lets the VALUE drop do the
            // work, which is what a plank in shadow actually looks like.
            else if (pose == Pose.Off) baseColour = Desaturate(Shift(baseColour, 0.72f, 1.0f),
                                                              0.22f);

            float keylineScale = surface == Surface.Action ? 1.35f : 1.0f;

            int keyline = Mathf.Max(2, Mathf.RoundToInt(h * KeylineFraction * keylineScale));
            int rim = Mathf.Max(2, Mathf.RoundToInt(h * RimFraction));

            float corner = chamfer || tab ? h * ChamferFraction
                                          : Mathf.Max(6.0f, h * RoundFraction);

            int cap = Mathf.CeilToInt(corner) + keyline + rim + 2;
            int width = (cap * 2) + 4;

            var pixels = new Color[width * h];

            Color keyColour = Shift(baseColour, KeylineValue, KeylineSat);
            Color rimColour = Shift(baseColour, RimValue, RimSat);

            float midX = (width - 1) * 0.5f;
            float midY = (h - 1) * 0.5f;
            float halfW = midX + 0.5f;
            float halfH = midY + 0.5f;

            for (int y = 0; y < h; y++)
            {
                // ⚠️ TEXTURE ROWS RUN BOTTOM-UP AND THE GRADIENT IS DESCRIBED TOP-DOWN. Getting
                // this the wrong way round puts the varnish band under the control, which reads
                // as a reflection rather than a highlight and is invisible in a code review.
                float down = 1.0f - (y / (float)(h - 1));

                // ⚠️ A TALL SURFACE HOLDS ITS FACE AT THE MID STOP. Its middle rows are what
                // Unity stretches, so a ramp there would be one colour smeared over hundreds of
                // units; the keyline and rim in the unstretched caps are what carry the look.
                Color face = tall
                    ? Sample(baseColour, 0.5f, RaisedValues, RaisedSats)
                    : Sample(baseColour, inverted ? 1.0f - down : down, RaisedValues, RaisedSats);

                for (int x = 0; x < width; x++)
                {
                    float dx = halfW - Mathf.Abs(x - midX);
                    float dy = halfH - Mathf.Abs(y - midY);

                    // ⚠️ A HEADER IS SQUARE ALONG ITS TOP EDGE AND ROUNDED BELOW, so it reads as
                    // a sign hung ON the panel under it rather than as a second floating card.
                    // The two corners it keeps are the only difference between it and `Panel`,
                    // and it is enough: a shape difference survives a photograph and a
                    // colourblind player, which a fill difference does not.
                    //
                    // ⚠️ A TAB IS THE OPPOSITE WAY UP: cut at the top, square along the bottom,
                    // so it stands ON the row. Texture rows run bottom-up, so `y > midY` is the
                    // TOP of the image and the two roles read as mirror images here.
                    bool upper = y > midY;
                    bool square = (sign && upper) || (tab && !upper);

                    float depth = square
                        ? Mathf.Min(dx, dy)
                        : Depth(dx, dy, corner, chamfer || (tab && upper), false);

                    Color c;

                    if (depth <= 0.0f)
                    {
                        c = Color.clear;
                    }
                    else if (depth <= keyline)
                    {
                        // ⚠️ ANTI-ALIASED ON THE OUTER FACE ONLY. His art has a one-pixel soft
                        // edge (alpha 38 then 248 at the far left of `BUTTON LONG`); a hard edge
                        // draws visible stairs on a 30 px diagonal at the sizes this game uses.
                        c = keyColour;
                        c.a = Mathf.Clamp01(depth);
                    }
                    else if (depth <= keyline + rim)
                    {
                        c = rimColour;
                    }
                    else
                    {
                        // ⚠️ THE GRAIN IS A FUNCTION OF x ONLY, so it runs DOWN the board the way
                        // a grain does. Two-dimensional noise reads as dirt.
                        float grain = Mathf.PerlinNoise(x * 0.35f, 0.0f) - 0.5f;
                        c = Lift(face, grain * 0.022f);
                    }

                    pixels[(y * width) + x] = c;
                }
            }

            return Finish(pixels, width, h, cap, tall, key);
        }

        // -----------------------------------------------------------------------------------
        // PAPER
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// A cream plate: the thing you read, or type into.
        ///
        /// ⚠️⚠️ IT IS BUILT BY A DIFFERENT SET OF RULES FROM WOOD AND THAT IS THE ENTIRE POINT.
        /// No keyline, no rim, no varnish band, no vertical ramp: paper is FLAT, because paper is
        /// flat. What it has instead is a fibre speckle and a single ink hairline, and the corner
        /// radius is small and constant rather than a fraction of the height, because a paper
        /// label does not get rounder as it gets taller. **Two surfaces that share a colour ramp
        /// and nothing else cannot read as the same object**, which is what 🧑 asked for: a
        /// central theme without one generator's fingerprints on everything.
        ///
        /// ⚠️ THE TYPE ON IT IS INK, NOT CREAM, and every caller has to honour that. This is the
        /// only surface in the front end that inverts the type colour, and it is what buys the
        /// screen its contrast: a wall of cream-on-brown rows with one ink-on-cream value in it
        /// has a place for the eye to land. `UiTheme.Ink` is the warm near-black § 6.4 settled.
        /// </summary>
        private static Sprite PaintPaper(Surface surface, Color baseColour, int h, bool tall,
                                         string key)
        {
            bool sunk = surface == Surface.PaperField;

            const int radius = 5;
            int cap = radius + 4;
            int width = (cap * 2) + 4;

            var pixels = new Color[width * h];

            float midX = (width - 1) * 0.5f;
            float midY = (h - 1) * 0.5f;
            float halfW = midX + 0.5f;
            float halfH = midY + 0.5f;

            Color hairline = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.55f);

            for (int y = 0; y < h; y++)
            {
                float down = 1.0f - (y / (float)(h - 1));

                for (int x = 0; x < width; x++)
                {
                    float dx = halfW - Mathf.Abs(x - midX);
                    float dy = halfH - Mathf.Abs(y - midY);
                    float depth = Depth(dx, dy, radius, false, false);

                    Color c;

                    if (depth <= 0.0f)
                    {
                        c = Color.clear;
                    }
                    else if (depth <= 1.6f)
                    {
                        c = hairline;
                        c.a *= Mathf.Clamp01(depth);
                    }
                    else
                    {
                        // ⚠️ THE FIBRE IS TWO-DIMENSIONAL HERE, UNLIKE WOOD'S GRAIN, because
                        // paper has no direction. It is 1.5 per cent: at anything more it reads
                        // as dirt on a light surface, where noise is far more visible than it is
                        // on brown.
                        float fibre = Mathf.PerlinNoise(x * 1.7f, y * 1.7f) - 0.5f;
                        c = Lift(baseColour, fibre * 0.015f);

                        // ⚠️ A FIELD GETS AN INNER SHADOW ALONG ITS TOP EDGE AND NOTHING ELSE.
                        // The light is above the screen, so the near wall of anything cut INTO a
                        // surface is the top one. Four units, because a field is a shallow tray
                        // rather than a hole, and it is the only thing distinguishing a plate you
                        // read from a plate you type in.
                        if (sunk && !tall && down < 0.14f)
                        {
                            float t = 1.0f - (down / 0.14f);
                            c = Color.Lerp(c, UiTheme.Ink, t * 0.16f);
                        }
                    }

                    pixels[(y * width) + x] = c;
                }
            }

            return Finish(pixels, width, h, cap, tall, key);
        }

        // -----------------------------------------------------------------------------------
        // SLATE
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// Asphalt: the surface the game is actually played on.
        ///
        /// ⚠️⚠️ IT IS THE ONE SURFACE IN THIS FILE THAT IS ABOUT THE GAME RATHER THAN ABOUT
        /// DRAWING, and it exists so a LOG does not have to be a wooden card. `VISION.md`'s
        /// one-paragraph version is a street game; § 2 rule 5 names the chalk and the road as
        /// things a frame has to show, and `MapGeometryCheck` gates the chalk box in every arena.
        /// The chat log, the friends list and the match history are all lists of lines, and a list
        /// of lines drawn on dark asphalt with a chalk rule under each one is this game's surface
        /// and nobody else's. It also costs no new colour: it is `UiTheme.Asphalt`, which the
        /// arenas already use.
        ///
        /// ⚠️ NO KEYLINE, NO CHAMFER, NO GRADIENT. A well is a hole, and a hole has no outline of
        /// its own: what says it is a hole is the LIT LIP along its top edge, two units of
        /// `WoodEdge` where the surface above it catches the light. That single line is the whole
        /// read, and adding a bright border around all four sides is what would turn it back into
        /// another rectangle.
        /// </summary>
        private static Sprite PaintSlate(Color baseColour, int h, bool tall, string key)
        {
            const int radius = 4;
            int cap = radius + 4;
            int width = (cap * 2) + 4;

            var pixels = new Color[width * h];

            float midX = (width - 1) * 0.5f;
            float midY = (h - 1) * 0.5f;
            float halfW = midX + 0.5f;
            float halfH = midY + 0.5f;

            int lip = Mathf.Max(2, Mathf.RoundToInt(h * 0.02f));

            for (int y = 0; y < h; y++)
            {
                float down = 1.0f - (y / (float)(h - 1));

                for (int x = 0; x < width; x++)
                {
                    float dx = halfW - Mathf.Abs(x - midX);
                    float dy = halfH - Mathf.Abs(y - midY);
                    float depth = Depth(dx, dy, radius, false, false);

                    Color c;

                    if (depth <= 0.0f)
                    {
                        c = Color.clear;
                    }
                    else if (down * (h - 1) < lip && depth > 1.0f)
                    {
                        // The lip: the edge of the road, catching the light from above.
                        c = UiTheme.WoodEdge;
                    }
                    else
                    {
                        // ⚠️ THE AGGREGATE IS COARSE AND FAINT. Asphalt is grit rather than
                        // grain, so this is two-dimensional like paper's fibre and at a much
                        // lower frequency, and it is 2.5 per cent: enough that a 400-unit panel
                        // is not one flat colour, little enough that cream type over it is not
                        // fighting texture.
                        float grit = Mathf.PerlinNoise(x * 0.55f, y * 0.55f) - 0.5f;
                        c = Lift(baseColour, grit * 0.025f);

                        // A soft darkening into the top of the well, so it reads as depth rather
                        // than as a flat dark card.
                        // ⚠️ 0.12 AND NOT 0.22, MEASURED BY LOOKING. On
                        // `Logs/shots-runtime/Lobby-v44.png` the chat's log read as a HOLE cut in
                        // the screen rather than as a surface with writing on it: a dark base plus
                        // a fifth of black at the top is darker than the night road behind it, and
                        // a well that is darker than everything around it stops being a well and
                        // becomes an absence. A groove needs enough shading to say "below", not
                        // enough to say "empty".
                        if (!tall && down < 0.18f)
                        {
                            float t = 1.0f - (down / 0.18f);
                            c = Color.Lerp(c, Color.black, t * 0.12f);
                        }
                    }

                    pixels[(y * width) + x] = c;
                }
            }

            return Finish(pixels, width, h, cap, tall, key);
        }

        // -----------------------------------------------------------------------------------
        // Shared geometry and colour
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// How far inside the silhouette a pixel is, positive inside, in units of pixels.
        ///
        /// ⚠️ ONE FUNCTION FOR ALL THREE MATERIALS, WHICH IS THE PART THAT SHOULD BE SHARED. The
        /// materials differ in what they PAINT at a given depth, not in what "inside" means, and
        /// having three copies of a rounded-rect distance field is how two of them end up with
        /// different corner anti-aliasing and the screen picks up a seam nobody can name.
        /// </summary>
        internal static float Depth(float dx, float dy, float corner, bool chamfer, bool squareTop)
        {
            if (squareTop) return Mathf.Min(dx, dy);

            if (chamfer)
            {
                // The 45-degree cut is one more half-plane: how far inside the diagonal joining
                // the two edges `corner` from the point where they would have met.
                float cut = (dx + dy - corner) * 0.70710678f;
                return Mathf.Min(Mathf.Min(dx, dy), cut);
            }

            float ox = corner - dx;
            float oy = corner - dy;

            if (ox <= 0.0f || oy <= 0.0f) return Mathf.Min(dx, dy);

            return corner - Mathf.Sqrt((ox * ox) + (oy * oy));
        }

        /// <summary>The face colour at a fraction of the way DOWN the control.</summary>
        private static Color Sample(Color baseColour, float down, float[] values, float[] sats)
        {
            for (int i = 1; i < Stops.Length; i++)
            {
                if (down > Stops[i] && i < Stops.Length - 1) continue;

                float span = Stops[i] - Stops[i - 1];
                float t = span <= 0.0f ? 0.0f : Mathf.Clamp01((down - Stops[i - 1]) / span);

                return Shift(baseColour, Mathf.Lerp(values[i - 1], values[i], t),
                             Mathf.Lerp(sats[i - 1], sats[i], t));
            }

            return baseColour;
        }

        /// <summary>
        /// The same colour at a different value and saturation.
        ///
        /// ⚠️⚠️ THE OVERFLOW GOES INTO SATURATION RATHER THAN BEING CLAMPED, AND AMBER IS WHY.
        /// `UiTheme.Amber` is `ffba00`, already at full value, so a keyline asking for 1.28 of it
        /// would clamp to the identical colour and the primary action, the one control that most
        /// needs an edge, would ship without one. Spending the excess on saturation instead gives
        /// a pale gold keyline over an amber face, which is what a lighter yellow actually looks
        /// like. Found by drawing it: the amber slab came out as a flat rectangle.
        /// </summary>
        internal static Color Shift(Color c, float value, float saturation)
        {
            Color.RGBToHSV(c, out float hue, out float sat, out float val);

            float wanted = val * value;

            if (wanted > 1.0f)
            {
                sat *= Mathf.Clamp01(1.0f / wanted) * saturation;
                wanted = 1.0f;
            }
            else
            {
                sat *= saturation;
            }

            var shifted = Color.HSVToRGB(hue, Mathf.Clamp01(sat), Mathf.Clamp01(wanted));
            shifted.a = c.a;
            return shifted;
        }

        private static Color Desaturate(Color c, float amount)
        {
            float grey = (c.r * 0.299f) + (c.g * 0.587f) + (c.b * 0.114f);
            return Color.Lerp(c, new Color(grey, grey, grey, c.a), amount);
        }

        internal static Color Lift(Color c, float amount) => new Color(
            Mathf.Clamp01(c.r + amount),
            Mathf.Clamp01(c.g + (amount * 0.94f)),
            Mathf.Clamp01(c.b + (amount * 0.86f)),
            c.a);

        internal static Sprite Finish(Color[] pixels, int width, int height, int cap, bool tall,
                                      string key)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = key,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            texture.SetPixels(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height),
                                       new Vector2(0.5f, 0.5f), 100.0f, 0,
                                       SpriteMeshType.FullRect,
                                       // ⚠️ A TALL SURFACE SLICES ON ALL FOUR SIDES so its top
                                       // and bottom keylines survive and only the flat middle
                                       // stretches; everything else slices horizontally only, so
                                       // its full-height gradient arrives as authored. See
                                       // `TallSurface`.
                                       tall ? new Vector4(cap, cap, cap, cap)
                                            : new Vector4(cap, 0, cap, 0));
            sprite.name = key;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }

    /// <summary>
    /// Puts a <see cref="WoodCraft"/> surface on an Image and keeps it at the right height.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THE SPRITE IS ONLY CORRECT AT THE HEIGHT IT WAS BUILT FOR. The
    /// slab is sliced horizontally only, so a control laid out taller than its sprite stretches
    /// the whole face: the varnish band smears and the bottom keyline is drawn across the middle
    /// of the button. Every rect in this front end is driven by a layout group, a
    /// `ContentSizeFitter` or an aspect-scaled canvas, so **no caller can know its own height at
    /// the moment it builds itself.** Watching the rect is the only version of this that cannot
    /// go stale.
    ///
    /// ⚠️ IT REBUILDS ON A 2-UNIT CHANGE, NOT EVERY FRAME. `WoodCraft.Slab` quantises to 4 units
    /// and caches, so a settled layout costs one float compare per frame and a resize costs one
    /// texture the first time that height is ever seen.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public sealed class WoodSkin : MonoBehaviour
    {
        public WoodCraft.Surface Surface = WoodCraft.Surface.Panel;

        /// <summary>Set only where a control needs a colour its role does not imply, such as a
        /// danger button. ⚠️ Leave it alone otherwise: the role is supposed to decide.</summary>
        public Color Tint = Color.clear;

        private Image _image;
        private float _built = -1.0f;

        public static WoodSkin Apply(GameObject target, WoodCraft.Surface surface)
            => Apply(target, surface, Color.clear);

        public static WoodSkin Apply(GameObject target, WoodCraft.Surface surface, Color tint)
        {
            if (target == null) return null;

            var image = target.GetComponent<Image>();
            if (image == null) image = target.AddComponent<Image>();

            var skin = target.GetComponent<WoodSkin>();
            if (skin == null) skin = target.AddComponent<WoodSkin>();

            skin.Surface = surface;
            skin.Tint = tint;
            skin._image = image;
            skin._built = -1.0f;
            skin.Rebuild();

            return skin;
        }

        private void OnEnable()
        {
            _built = -1.0f;
            Rebuild();
        }

        private void OnRectTransformDimensionsChange() => Rebuild();

        private void Update() => Rebuild();

        public void Rebuild()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_image == null) return;

            float height = _image.rectTransform.rect.height;

            // ⚠️ A RECT THAT HAS NOT BEEN LAID OUT YET REPORTS 0, and baking against that would
            // pin the control to the 20-unit floor forever: the next frame's real height is a
            // change, but the sprite it produced in between is what a screenshot taken on frame
            // one shows. Waiting costs one frame of an unskinned control and nothing else.
            if (height <= 1.0f) return;
            if (_built > 0.0f && Mathf.Abs(height - _built) < 2.0f) return;

            _built = height;

            _image.sprite = WoodCraft.Slab(Surface, height, WoodCraft.Pose.Rest,
                                           Tint.a > 0.0f ? Tint : (Color?)null);
            _image.type = Image.Type.Sliced;
            _image.color = Color.white;

            // ⚠️ WITHOUT THIS THE SLICE IS SCALED BY THE SPRITE'S PIXELS-PER-UNIT AND THE CAPS
            // ARRIVE AT THE WRONG SIZE. Every sliced sprite in this project sets it, for the same
            // reason; see `GodotButton.Apply`.
            _image.pixelsPerUnitMultiplier = 1.0f;
        }
    }
}
