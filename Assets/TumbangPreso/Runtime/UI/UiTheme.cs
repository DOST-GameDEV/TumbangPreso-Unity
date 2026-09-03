using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The game's palette, transcribed exactly from `ui_theme.gd`.
    ///
    /// ⚠️⚠️ TWO OF THESE COLOURS CARRY MEANING AND THE REST DO NOT. <see cref="Offense"/> and
    /// <see cref="Defense"/> answer "which side is this", so they are the only colours in the
    /// game a player has to READ rather than merely see. Everything else can be restyled
    /// freely; those two cannot be reused, and nothing else may drift close to them in hue.
    ///
    /// ⚠️ THAT CONSTRAINT REACHES BEYOND THE UI. The prop palette exists precisely so a hero
    /// prop can be more saturated than any environment colour, because it is the most looked
    /// at object in the game and has to read against asphalt, WITHOUT approaching the two
    /// side colours. A new can tinted orange would read as "the attacking team's can".
    ///
    /// ⚠️ THE ART IS BEING REPLACED (see docs/Port_Plan.md section 8), BUT THIS SURVIVES IT.
    /// A palette is a design system, not an asset. New meshes get tinted from these, and the
    /// meaning of Offense and Defense holds whatever the models look like.
    /// </summary>
    public static class UiTheme
    {
        private static Color Hex(string rgb)
        {
            return ColorUtility.TryParseHtmlString("#" + rgb, out var c) ? c : Color.magenta;
        }

        // -------------------------------------------------------------------
        // CORE. Text, surfaces, and the two that mean something.
        // -------------------------------------------------------------------

        /// <summary>
        /// Near-black WARM ink: text, borders, pressed fills, and the outline on every menu type
        /// style in <see cref="GodotTheme"/>.
        ///
        /// ⚠️⚠️ IT WAS `040838`, A NEAR-BLACK NAVY, AND THAT IS THE BLUE `CLAUDE.md` § 6.4 IS
        /// ABOUT. 🧑 2026-08-31: *"i dont like blue outlines its out of theme"*, and again on
        /// 2026-09-01: *"i dont want to see blue shit, thats not in theme"*. § 6.4 was written
        /// after the first and fixed the rank emblems; it could not fix this, because **every
        /// `MenuDisplay`, `MenuHeading`, `MenuBody`, `MenuCaption` and `MenuValue` outline in the
        /// game read this constant**, so the whole menu was outlined in navy on brown wood. At
        /// four to six pixels on a heading that is a visible cold ring.
        ///
        /// `1c0f06` is the ink named in § 6.4's palette and in `VISION.md` § 6 (wood `31190B`,
        /// `5A2F14`, `8B5227`, cream `F5E6C8`, amber `FFBA00`, ink `1C0F06`). It is the same
        /// darkness and none of the hue.
        ///
        /// ⚠️ IT IS ALSO THE TEXT COLOUR ON CREAM AND WHITE FIELDS (the sign-in inputs, the
        /// tab labels), where warm near-black is strictly more correct than navy against paper.
        /// </summary>
        public static readonly Color Ink = Hex("1c0f06");

        /// <summary>
        /// Light neutral: screen background.
        ///
        /// ⚠️ IT WAS `e1e5e8`, A COOL BLUE-GREY, AND IT IS WARM PAPER NOW. See <see cref="Ink"/>
        /// and `CLAUDE.md` § 6.4: the rule is the whole palette, not only outlines, and a grey
        /// with a blue cast next to `8b5227` wood reads as blue rather than as neutral.
        /// </summary>
        public static readonly Color Panel = Hex("fddfba");

        /// <summary>Slightly lighter: raised card and control fill. ⚠️ Was `f5f7fa`, same
        /// reason as <see cref="Panel"/>.</summary>
        public static readonly Color Card = Hex("feebd4");

        /// <summary>⚠️ MEANS "ATTACKING SIDE". Never reuse it decoratively.</summary>
        public static readonly Color Offense = Hex("f87020");

        /// <summary>⚠️ MEANS "DEFENDING SIDE", the taya. Never reuse it decoratively.</summary>
        public static readonly Color Defense = Hex("0080e8");

        /// <summary>Pink: hits, focus, emphasis.</summary>
        public static readonly Color Impact = Hex("f468a8");

        /// <summary>Yellow: timers under pressure, hover.</summary>
        public static readonly Color Highlight = Hex("f8d028");

        /// <summary>Red: destructive, out of bounds.</summary>
        public static readonly Color Danger = Hex("f80000");

        public static Color InkMuted => new Color(Ink.r, Ink.g, Ink.b, 0.62f);

        // -------------------------------------------------------------------
        // THE BRAND SET, 2026-09-03. READ OUT OF THE LOGO, NOT PICKED.
        //
        // ⚠️⚠️ THIS IS THE PALETTE NOW, AND THE WOOD SET BELOW IS THE OLD ONE.
        // 🧑 2026-09-03: *"the colors are final, ask it to use the same colors as logo"*, and
        // separately *"the colors are final"* again. `docs/TODO.md` § 133.1 records that the art
        // is work in progress and the palette is not.
        //
        // ⚠️⚠️ EVERY HEX IN THIS BLOCK WAS MEASURED BY A SCRIPT AND NONE WAS TYPED BY EYE, which
        // is § 133.1's instruction in as many words: *"READ THE HEXES OFF THE COMMITTED FILE. DO
        // NOT TYPE THEM IN BY EYE AND DO NOT SAMPLE A CHAT THUMBNAIL."*
        // `tools/read_brand_palette.py` clusters the committed artwork's flat fills and prints
        // each one's share of the drawing; the percentages below are its output.
        //
        // ⚠️ IT AGREED WITH ITSELF ACROSS TWO INDEPENDENT FILES, which is why these are trusted.
        // `tump_logo_colour.jpg` and `tsinelas_hit.jpg` were drawn separately and return the same
        // six colours within one or two levels on every channel. A palette that reproduces across
        // two drawings is a palette; one read off a single file is a sample.
        //
        // ⚠️ THE FILES ARRIVE AS JPEG, so the reader merges values within 14 levels before
        // counting. Its first run reported the outline EIGHT times (970617, 980716, 980613,
        // 960514, 970615, 980515, 960516, 9d0710) because chroma subsampling smeared one flat
        // fill, and eight rows for one colour is exactly the guess that script exists to remove.
        // -------------------------------------------------------------------

        /// <summary>The outline every shape in the mark is held in, and **34.3 per cent of the
        /// drawing**: the single largest area in the logo.
        ///
        /// ⚠️⚠️ IT IS THE OUTLINE ROLE AND NOT A GROUND, WHICH IS WHY NO SCREEN USES IT AS A
        /// FIELD. In the mark it is what holds every other colour in, so in the front end it is
        /// the stroke on the one primary action, the selection frame, and the one destructive
        /// control. `docs/Front_End_Design.md` § 4 is the role table.</summary>
        public static readonly Color BrandRed = Hex("980715");

        /// <summary>Honey Quartz: the fill inside the letters, **23.1 per cent**. The front end's
        /// ground, and the base every paper tone below is derived from.</summary>
        public static readonly Color BrandHoney = Hex("fcd39f");

        /// <summary>Chartreuse: the blob behind the wordmark, **17.0 per cent**. ⚠️ **THE ACTION
        /// COLOUR, and it REPLACES <see cref="MenuGreen"/>'s role rather than joining it.** His
        /// authored `JOIN BUTTON.png` and the PLAY pennant are already green and `CLAUDE.md`
        /// § 6.5 names green as his primary, so this is the same role moving to the settled
        /// palette rather than a new hue arriving.</summary>
        public static readonly Color BrandChartreuse = Hex("d6ce01");

        /// <summary>Persimmon: the diagonal fill on the `1`, **5.7 per cent**. The MARKER: the one
        /// value or selection on a screen that matters.</summary>
        public static readonly Color BrandPersimmon = Hex("fd8041");

        /// <summary>The swirl in the drip, **4.2 per cent**. ⚠️ This is what <see cref="Amber"/>
        /// becomes: it is the logo's own gold and it is two thirds of a step warmer than the
        /// `ffba00` it replaces.</summary>
        public static readonly Color BrandGolden = Hex("f5b521");

        /// <summary>The brighter rim marks under the letters, **3.8 per cent**. The lit state of
        /// <see cref="BrandRed"/>, and it was drawn as exactly that.</summary>
        public static readonly Color BrandRimRed = Hex("c32e0d");

        /// <summary>Army: the shading strokes across the blob, **1.4 per cent**. The dark ground,
        /// and the only one. `docs/Front_End_Design.md` § 2.3: the fighter picker is the one dark
        /// screen in the front end and 🧑 asked for that by name.</summary>
        public static readonly Color BrandArmy = Hex("b3a828");

        /// <summary>
        /// Khaki: the quiet ground, for a tray or a sunk row on a screen that is already Honey.
        ///
        /// ⚠️⚠️ THIS IS THE ONE COLOUR IN THIS BLOCK THAT IS DERIVED RATHER THAN MEASURED, AND
        /// SAYING SO IS THE POINT. The five named swatches are Honey Quartz, Chartreuse,
        /// Persimmon, Khaki and Army; the first, second, third and fifth all appear as fills in
        /// the artwork and this one does not, because the drawing never needed a quiet mid-tone.
        /// It is **Honey Quartz mixed 72:28 toward Army**, which is the two neighbours it sits
        /// between on his own swatch strip, and ink measures **9.2:1** on it.
        ///
        /// ⚠️ `Attention.md` § 12 CARRIES THE ASK TO CONFIRM IT. The swatch strip that shipped
        /// with the first version of the logo has the real value printed on it, and that image is
        /// still only in the chat. **If it disagrees, this constant is the only thing that
        /// changes**, which is the whole reason the palette is named rather than inlined.
        /// </summary>
        public static readonly Color BrandKhaki = Hex("e8c77e");

        // -------------------------------------------------------------------
        // THE WOOD SET. ⚠️⚠️ THIS IS THE **OLD** PALETTE AS OF 2026-09-03.
        //
        // ⚠️ IT IS KEPT RATHER THAN DELETED FOR TWO REASONS, AND NEITHER IS SENTIMENT.
        // `PaperPurityProbe.WoodFills` lists these exact hexes to DETECT a leftover from the
        // old front end, so deleting them would blind the gate that proves the overhaul
        // finished; and the in-match HUD is out of scope for `docs/TODO.md` § 133 and is still
        // drawn in them on purpose.
        //
        // ⚠️ This is the look the pitch deck and the sponsorship proposals were both
        // designed from, so it is effectively the team's brand and not only a UI skin.
        // -------------------------------------------------------------------

        public static readonly Color WoodDeep = Hex("31190b");
        public static readonly Color WoodMid = Hex("5a2f14");
        public static readonly Color WoodDark = Hex("1d0e06");
        public static readonly Color WoodEdge = Hex("8b5227");
        public static readonly Color Cream = Hex("f5e6c8");
        /// <summary>
        /// ⚠️⚠️ IT IS STILL `ffba00` AND IT WAS DELIBERATELY NOT MOVED TO THE BRAND GOLD.
        /// This constant is read **15 times in `Hud.cs`** and again in `AbilityInspectPanel`,
        /// `PlayerNameplate` and `RoleSwapCard`, all of which are the in-match layer that
        /// `docs/TODO.md` § 133.4 scopes out of this pass in as many words: *"if it is drawn while
        /// a round is live, it is out of scope. Not deferred, not do it last."*
        ///
        /// **A one-line palette edit here would have repainted the HUD**, which is exactly the
        /// class of accident § 6.4 records `UiTheme.Ink` causing in the other direction: one
        /// constant reaching further than the person editing it realised. The front end uses
        /// <see cref="BrandGolden"/>, which is the logo's own gold and is two thirds of a step
        /// warmer than this; the two live side by side until the HUD is repainted on purpose.
        /// </summary>
        public static readonly Color Amber = Hex("ffba00");

        /// <summary>
        /// The face of a wooden CONTROL, measured off 🧑's own `BUTTON LONG.png`.
        ///
        /// ⚠️⚠️ IT IS SAMPLED FROM THE AUTHORED ART RATHER THAN PICKED FROM THE SET ABOVE, AND
        /// THAT IS THE WHOLE REASON IT EXISTS. `WoodCraft` generates a control as seven values of
        /// ONE colour, so the base it starts from decides whether a code-drawn button sits beside
        /// his `BUTTON LONG` as a sibling or as a near miss. `793e1f` is the varnish band at 25
        /// per cent down the centre of that texture, which is the brightest point of the face and
        /// therefore the anchor the rest of the ramp is expressed against.
        ///
        /// ⚠️ IT SITS BETWEEN <see cref="WoodMid"/> `5a2f14` AND <see cref="WoodEdge"/> `8b5227`
        /// AND IS NEITHER. Both of those were already being used as button fills, and both are
        /// wrong by enough to see: `WoodMid` is a fifth darker than his face and `WoodEdge` is his
        /// KEYLINE colour, so a button filled with it came out the colour of its own outline.
        /// </summary>
        public static readonly Color WoodFace = Hex("3f3a0e");

        /// <summary>
        /// The face of a wooden PANEL, off `SETTINGS CONFIG PANEL.png` and `MAP MODE DISPLAY.png`.
        ///
        /// ⚠️ ONE VALUE DARKER THAN <see cref="WoodFace"/> AND THAT ONE VALUE IS DELIBERATE IN
        /// HIS ART. Both panel textures peak at `783e1f` and both button textures at `793e1f`:
        /// furniture sits a shade back from the controls standing on it, which is what keeps a
        /// row of buttons legible against the card they are in. Keeping the two constants apart
        /// means a future palette change cannot accidentally flatten them together.
        /// </summary>
        public static readonly Color WoodPanelFace = Hex("3f3a0e");

        /// <summary>
        /// The face of a wooden SLOT you type into, off `TEXT FIELD.png`.
        ///
        /// ⚠️⚠️ IT IS THE SAME CONSTRUCTION AS THE BUTTON AT A LOWER VALUE, WHICH IS THE PART
        /// WORTH KNOWING. `TEXT FIELD.png` and `BUTTON LONG.png` are the same 818x135 chamfered
        /// slab with the same `99572b` keyline; only the face differs, `4e2211` against `793e1f`,
        /// which is 0.64 of the value. **His field is his button, darker.** Two controls a player
        /// can tell apart at a glance, out of one drawing, and that relationship is what
        /// `WoodCraft` reproduces rather than a second hand-tuned texture.
        ///
        /// ⚠️ AND THE RAMP IS INVERTED ON IT: `461e0f` at 10 per cent down against `4e2211` at
        /// 25, so the TOP is the dark end. The light is above the screen and the near wall of a
        /// recess is the one in shadow, so a slot is lit from below and a board is lit from above.
        /// `WoodCraft.PaintWood` flips it on this surface and on no other.
        /// </summary>
        public static readonly Color WoodFieldFace = Hex("2a2709");

        /// <summary>
        /// The darkest wooden face: an unselected tab, and anything that has to sit clearly
        /// BEHIND a control of the same shape beside it.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE <see cref="WoodFieldFace"/> WAS NOT DARK ENOUGH TO SEPARATE
        /// TWO TABS AND THAT WAS MEASURED BY LOOKING. `793e1f` against `4e2211` is an obvious
        /// difference in a colour picker, and on `Logs/shots-runtime/Lobby-v44.png` PRACTICE and
        /// MULTIPLAYER still read as the same control: every `WoodCraft` face carries a varnish
        /// band, the eye compares the BRIGHT band of one against the bright band of the other,
        /// and `4e2211`'s peak sits inside `793e1f`'s ramp. At `36180c` the idle tab's brightest
        /// pixel is darker than the live tab's darkest, so the two cannot be confused at any size.
        ///
        /// ⚠️ THIS IS THE ARGUMENT FOR CHECKING CONTRAST AGAINST THE RENDER RATHER THAN AGAINST
        /// THE CONSTANTS. A palette diff says these two are far apart; the picture said they were
        /// not, because what a gradient actually shows a reader is its highlight.
        /// </summary>
        public static readonly Color WoodSlot = Hex("242109");

        /// <summary>
        /// The road, for a UI well: a log, a list, anything chalk is drawn on.
        ///
        /// ⚠️⚠️ IT IS NOT <see cref="EnvAsphalt"/> AND IT MUST NOT BECOME IT. That constant is
        /// `4a4e57`, which has **more blue in it than red**, and `CLAUDE.md` § 6.4 states the test
        /// in exactly those terms: a hex with more blue than red does not belong in a menu. It is
        /// correct where it lives, on an arena floor under a graded 3D light, and it would read as
        /// the cold slate this front end has already been told five times to stop drawing.
        ///
        /// ⚠️ IT IS A WARM NEAR-BLACK ONE STEP OFF <see cref="WoodDark"/>, so a slate well inside
        /// a wooden card reads as a hole in the wood rather than as a foreign panel laid on it.
        /// </summary>
        public static readonly Color Asphalt = Hex("322f0b");

        // -------------------------------------------------------------------
        // THE PAPER SET. The front end's DOMINANT surface as of 2026-09-01.
        //
        // ⚠️⚠️ THIS REVERSES THE FIGURE AND THE GROUND, AND IT IS 🧑'S OWN INSTRUCTION WITH TWO
        // HEXES ATTACHED. He sent the `TUMP` sticker logo and a two-swatch card and said: *"game
        // reads as too brown bcz the game itself is brown already (the map and shit)"*, *"can we
        // remodel the color of all UI for lobby and login to look like this?"*, *"i want us to
        // play around the 2 colors i attached"*.
        //
        // ⚠️⚠️ HE IS DESCRIBING A CONTRAST FAULT RATHER THAN A TASTE ONE, AND IT IS MEASURABLE.
        // Eskinita's road, houses and poles occupy hue 20 to 40 at 30 to 60 per cent saturation.
        // `WoodFace` `793e1f` is hue 22 at 74 per cent. **The furniture and the world it sits on
        // were the same colour**, so every panel in `Lobby-v51.png` has to be found by its keyline
        // rather than seen as a shape. Cream at `f4ecdd` is 6 per cent saturated: it separates
        // from that world on VALUE and SATURATION at once, which is the one pair of axes the
        // street does not already occupy.
        //
        // ⚠️ AND IT IS NOT A NEW HUE. Both swatches are hue 34 to 38, one step off `Cream`
        // `f5e6c8` and inside the same warm family as the wood. `CLAUDE.md` § 6.4's palette is
        // unchanged and no blue, navy or cold grey enters anywhere: what changes is which member
        // of it is the FIELD and which is the FIGURE. Wood is the ink and the frame now; paper is
        // the surface.
        // -------------------------------------------------------------------

        /// <summary>
        /// The front end's primary surface.
        ///
        /// ⚠️⚠️ IT IS <see cref="BrandHoney"/> LIGHTENED 55 PER CENT TOWARD WHITE, AND THE WHOLE
        /// RAMP BELOW IS ONE COLOUR AT FOUR TINTS. That is `CLAUDE.md` § 6.5's own mechanism
        /// moved up a level: *"`JOIN BUTTON.png` is `BUTTON LONG.png` with one colour swapped,
        /// keyline to floor, so one base colour generates a whole control"*. Four tints of one
        /// brand colour cannot drift apart the way four separately chosen creams did.
        ///
        /// ⚠️ IT IS A TINT RATHER THAN THE MEASURED `fcd39f` ITSELF, AND THAT IS A DECISION WITH
        /// A REASON. Honey Quartz at full strength is the fill inside the LETTERS, which is a
        /// shape a few hundred units wide; the same value across an entire 1920-unit screen is a
        /// different amount of colour entirely. The brand tone is kept as
        /// <see cref="PaperEdge"/>, where it does the job it does in the mark: the band around
        /// the outside of a cut-out.
        ///
        /// ⚠️ IT WAS `f4ecdd` UNTIL 2026-09-03, which was sampled off the OLD `TUMP.png`.
        /// `docs/TODO.md` § 133.1 replaced that mark.
        /// </summary>
        public static readonly Color Paper = Hex("feebd4");

        /// <summary>
        /// The warmer of his two swatches: anything RECESSED into <see cref="Paper"/>.
        ///
        /// ⚠️ IT IS <see cref="BrandHoney"/> LIGHTENED 28 PER CENT, one step down the same ramp.
        /// ⚠️ THE TWO ARE ONLY 4 PER CENT APART IN VALUE AND THAT IS THE POINT. A tray cut into a
        /// sheet is the same paper under less light, so the difference has to be small enough to
        /// read as shading and large enough to find. Anything wider turns a form into a set of
        /// stripes, which is what the zebra bands in § 92 were.
        /// </summary>
        public static readonly Color PaperWarm = Hex("fddfba");

        /// <summary>
        /// The die-cut halo: the band of sand a sticker keeps around its own artwork.
        ///
        /// ⚠️⚠️ IT IS THE LOGO'S CONSTRUCTION, NOT A BORDER COLOUR. Every letter of `TUMP` and the
        /// blob behind it carry the same outside band, which is what makes the mark read as a
        /// physical cut-out lying on a surface rather than as a shape drawn on one. `PaperCraft`
        /// puts that band OUTSIDE the fill rather than inside it, which is why a cream panel here
        /// does not read as a cream rectangle with a line round it.
        /// </summary>
        public static readonly Color PaperEdge = Hex("fcd39f");

        /// <summary>The sand under a paper control that is pressed, and the lip along its bottom.
        /// ⚠️ <see cref="BrandHoney"/> DARKENED 12 PER CENT, so a pressed token darkens INTO its own
        /// halo rather than picking up a second colour.</summary>
        public static readonly Color PaperSunk = Hex("deba8c");

        /// <summary>
        /// Ink on paper: the type colour for every word drawn on <see cref="Paper"/>.
        ///
        /// ⚠️⚠️ IT IS A MIX OF THE TWO DARKEST BRAND COLOURS RATHER THAN EITHER OF THEM, AND THE
        /// REASON IS THAT PURE DEEP RED READS AS AN ERROR. <see cref="BrandRed"/> darkened is still
        /// unmistakably RED at body size, and red text means something wrong in every UI
        /// convention a player has ever met. This is `BrandRed` mixed 55:45 with
        /// <see cref="BrandArmy"/> and darkened 48 per cent, which lands warm and dark and belongs
        /// to the palette without claiming a state.
        ///
        /// ⚠️ IT HITS THE SAME TARGET THE OLD INK WAS ARGUED TO, WHICH IS WHY THIS NUMBER AND NOT A
        /// DARKER ONE. Near-black on paper is about 17:1 and reads as a printed form, which is the
        /// opposite of the calm 🧑 asked for (*"ur goal is to make it ... calming"*). `55290f` on
        /// `feebd4` measures **10.5:1** and 9.6 on `PaperWarm`, against the old `3b2415`'s 10.4;
        /// both are far above the 4.5:1 floor `game-ui-design`'s `validations.md` sets for body
        /// copy. `scratchpad/fontsrc/ramp.py` computes it rather than anybody eyeballing it.
        /// </summary>
        public static readonly Color PaperInk = Hex("55290f");

        /// <summary>
        /// Secondary type on paper: captions, hints, the second line of a row.
        ///
        /// ⚠️ A LIGHTER INK RATHER THAN A TRANSPARENT ONE, because alpha over a sheet that itself
        /// sits over a live street changes colour with whatever is behind the screen.
        ///
        /// ⚠️⚠️ IT WAS `8a6c50` AND THAT MEASURED 4.1:1 AGAINST `PaperWarm`, WHICH IS UNDER THE
        /// FLOOR. `game-ui-design`'s `validations.md` sets 4.5:1 for body copy, and this colour
        /// carries every caption on the front end: the loadout line under a character name, the
        /// settings summary under the chip, `tap to copy`, the seat plates' second line. Computing
        /// the ratio rather than eyeballing it is the whole lesson of `CLAUDE.md` § 6.4, where a
        /// near-black navy looked black in a code review for the entire life of the file.
        /// ⚠️ IT IS THE SAME BRAND MIX AS <see cref="PaperInk"/>, DARKENED ONLY 8 PER CENT INSTEAD OF
        /// 48, so the two are one voice at two strengths rather than two colours. `97491b` measures
        /// **5.5:1** on `feebd4` and **5.0:1** on `fddfba`, both over the floor, against the old
        /// `7a5c40`'s 5.2 and 4.9. `scratchpad/fontsrc/ramp.py` computes it.
        /// </summary>
        public static readonly Color PaperInkSoft = Hex("97491b");

        public static Font Font => MenuKit.Font;

        public static Color CreamMuted => new Color(Cream.r, Cream.g, Cream.b, 0.68f);

        public static readonly Color MenuGreen = Hex("a09b01");
        public static readonly Color MenuGreenLit = Hex("e8e14a");

        /// <summary>
        /// The face of the PRIMARY action, measured off 🧑's own `JOIN BUTTON.png`.
        ///
        /// ⚠️⚠️ GREEN IS HIS PRIMARY COLOUR AND THAT IS EVIDENCE RATHER THAN TASTE. `JOIN
        /// BUTTON.png` and the `PLAY` pennant are both authored green, and `JOIN BUTTON` is
        /// pixel-for-pixel the same construction as `BUTTON LONG` with one colour swapped:
        /// keyline `90ea40` against `99572b`, rim `3caf2d` against `612e15`, face peak `51dd38`
        /// against `793e1f`, floor `188427` against `421806`. **The same seven values of a
        /// different hue**, which is the whole system `WoodCraft` transcribes.
        ///
        /// ⚠️ THIS IS THE PEAK, NOT <see cref="MenuGreen"/> `21a131`, WHICH IS A THIRD DARKER
        /// THAN ANY PIXEL IN HIS BUTTON. A primary drawn at the old constant came out as a
        /// muddy bottle green beside his art; `WoodCraft` expresses its whole ramp against the
        /// peak, so handing it the floor of the ramp as the base darkened every stop again.
        ///
        /// ⚠️ AND IT IS NOWHERE NEAR `Offense` or `Defense`. Hue 110, against 22 and 207: green
        /// is 88 degrees off the attacker's orange and 97 off the taya's blue, so a green button
        /// cannot be read as a role. That constraint is `Art_Direction.md` § 1 and it is the only
        /// thing that limits which hues a control may take.
        /// </summary>
        public static readonly Color MenuGreenFace = Hex("d6ce01");
        public static readonly Color MenuRed = Hex("980715");
        public static readonly Color MenuRedLit = Hex("c32e0d");

        // -------------------------------------------------------------------
        // ENVIRONMENT. Deliberately desaturated so the props read against it.
        // -------------------------------------------------------------------

        public static readonly Color EnvAsphalt = Hex("4a4e57");
        public static readonly Color EnvConcrete = Hex("b7b2a6");
        public static readonly Color EnvConcreteDark = Hex("8c877c");
        public static readonly Color EnvGiSheet = Hex("9aa3a2");
        public static readonly Color EnvRust = Hex("a65a3a");
        public static readonly Color EnvWood = Hex("a8763f");
        public static readonly Color EnvWoodDark = Hex("6b4a28");
        public static readonly Color EnvFoliage = Hex("4f8c3b");
        public static readonly Color EnvFoliageDark = Hex("35652a");
        public static readonly Color EnvDirt = Hex("c2a878");
        public static readonly Color EnvTarp = Hex("dcd5c4");
        public static readonly Color EnvRubber = Hex("2b2b30");

        /// <summary>The default Manila facade, and the four repaints that vary a street.</summary>
        public static readonly Color EnvPaintCream = Hex("e2d2ac");
        public static readonly Color EnvPaintTerra = Hex("b5664c");
        public static readonly Color EnvPaintMint = Hex("86b4a6");
        public static readonly Color EnvPaintOchre = Hex("c9994a");
        public static readonly Color EnvPaintPlinth = Hex("6d5f52");

        // -------------------------------------------------------------------
        // PROPS. May out-saturate anything in ENV, must stay clear of the two side colours.
        // -------------------------------------------------------------------

        public static readonly Color PropFoam = Hex("7a5741");
        public static readonly Color PropFoamDark = Hex("54382a");
        public static readonly Color PropWebbing = Hex("c69a6b");
        public static readonly Color PropSarsiRed = Hex("d8221c");

        // -------------------------------------------------------------------
        // HERO STRIKE CHROME. The deck, the inspect tray and the character select ribbon.
        //
        // ⚠️⚠️ THESE EXIST BECAUSE THE HERO UI HAD SEVENTEEN COLOURS NAMED INLINE AND NONE OF
        // THEM WERE IN THIS FILE. `Art_Direction.md` § 1 ends with "ui_theme.gd is the only
        // place a colour is named. Read it, never restate it", and the first pass at the hero
        // layer restated a whole slate-blue palette across `Hud.cs`, `AbilityInspectPanel.cs`
        // and `ConvertedCharacterSelect.cs`: `rgba(16, 22, 34, 0.90)` plates,
        // `rgba(61, 82, 112, 0.60)` rims, near-white glyphs. A colour that is not in the
        // palette file cannot be checked against the palette, so the entire hero layer drifted
        // into the opposite hue family from the brand without anything catching it.
        //
        // 🧑 2026-08-23: *"i lowk dont get why we use light blue and shit in some parts of ui,
        // it doesnt really look good with brown"*. It read worst against the wooden scoreboard
        // and clock, which are on screen at the same time.
        //
        // ⚠️ THE FIX IS THAT THE HERO UI HAS NO PALETTE OF ITS OWN. Its chrome is the wood set
        // at alpha, its accents are the five below. `docs/Hero_Strike_UI.md` § 2 carries the
        // table and the reasoning.
        // -------------------------------------------------------------------

        /// <summary>
        /// The panel behind a block of hero text: the inspect tray, the objective banner.
        ///
        /// ⚠️⚠️ NEAR-BLACK AND TRANSLUCENT, NOT A SLAB OF WOOD, AND THAT IS THE SECOND
        /// CORRECTION IN ONE SESSION. The first pass at fixing the imported slate blue swung the
        /// whole hero layer onto the WOOD SET, which is right for the menus and wrong here.
        /// 🧑, on the result: *"the brown shit looks ugly. kinda wanted just the icons like in
        /// overwatchh or something"*, with a reference frame attached.
        ///
        /// ⚠️⚠️ THE LESSON IS THAT THE MENU CHROME AND THE COMBAT CHROME ARE DIFFERENT JOBS.
        /// A menu panel is FURNITURE: it is the thing you are looking at, it can be opaque, and
        /// the painted wood is the brand. A combat overlay is a WINDOW: the thing you are
        /// looking at is the court behind it, so its job is to disappear and let the glyph on it
        /// read. Overwatch, Valorant and every shooter in that lineage draw the same conclusion,
        /// which is why all of them use a translucent near-black with a bright rim.
        ///
        /// ⚠️ IT IS WARM near-black rather than neutral. 10, 8, 7 out of 255 is imperceptible
        /// as a hue and still sits in the same family as the wood, so the deck does not read as
        /// a foreign object beside the wooden scoreboard the way the slate blue did.
        /// </summary>
        public static readonly Color HeroPlate = new Color(0.039f, 0.033f, 0.030f, 0.72f);

        /// <summary>
        /// A single ability tile.
        ///
        /// ⚠️ 0.55 ALPHA, BECAUSE THE COURT SHOWING THROUGH IS THE POINT. An opaque tile is a
        /// hole cut in the game; a translucent one is a label laid over it.
        /// </summary>
        public static readonly Color HeroPlateRaised = new Color(0.055f, 0.046f, 0.040f, 0.55f);

        /// <summary>The groove a meter drains inside.</summary>
        public static readonly Color HeroPlateSunk = new Color(0.0f, 0.0f, 0.0f, 0.55f);

        /// <summary>
        /// Every resting border.
        ///
        /// ⚠️⚠️ CREAM AT LOW ALPHA, WHICH IS WHAT MAKES THE TILE READ AS AN OUTLINE RATHER THAN
        /// AS A BOX. The wood-tan rim at 0.55 was a visible brown frame around every icon, and
        /// three brown frames in a row is the "brown shit" in the report. A bright rim at low
        /// opacity reads as an edge, not as furniture, and it goes to FULL cream the moment the
        /// power is ready, which is the only time the edge has something to say.
        /// </summary>
        public static Color HeroRim => new Color(Cream.r, Cream.g, Cream.b, 0.26f);

        /// <summary>A rim on a power that is up. The brightest thing in the deck.</summary>
        public static Color HeroRimLit => new Color(Cream.r, Cream.g, Cream.b, 0.95f);

        /// <summary>
        /// A glyph that is available.
        ///
        /// ⚠️ CREAM, NOT WHITE. Pure white on a near-black brown plate reads as a hole punched
        /// through the panel. The brand's own paper colour reads as ink on a painted sign,
        /// which is what every other surface in this UI already looks like.
        /// </summary>
        public static Color HeroGlyphOn => Cream;

        /// <summary>
        /// A glyph that is not available.
        ///
        /// ⚠️ IT IS THE SAME COLOUR AT LOW ALPHA, AND THAT IS THE POINT. The old value was a
        /// cool grey-blue, so a second hue family arrived through the back door on the one
        /// state a player looks at most: a skill on cooldown. Dropping alpha on the colour
        /// already there cannot introduce a hue.
        /// </summary>
        public static Color HeroGlyphOff => new Color(Cream.r, Cream.g, Cream.b, 0.20f);

        /// <summary>
        /// The countdown over a cooling tile, and the charge figure on the ultimate.
        ///
        /// ⚠️ CREAM RATHER THAN AMBER. Amber is the game's TIMER colour and it is already
        /// carrying the round clock at the top of the screen; using it again on three tiles at
        /// the bottom made the deck compete with the clock for the same glance. White-cream
        /// numbers on a dark tile are what every shooter in this lineage draws, and they cannot
        /// be confused with the clock.
        /// </summary>
        public static Color HeroNumber => new Color(Cream.r, Cream.g, Cream.b, 0.96f);

        // -------------------------------------------------------------------
        // HERO ACCENTS. One per hero, and they answer the colour law above.
        //
        // ⚠️⚠️ TWO OF THESE USED TO SIT ON TOP OF THE ROLE HUES. `Art_Direction.md` § 1 reserves
        // orange `#f87020` for OFFENSE and blue `#0080e8` for DEFENCE and says nothing else in
        // the frame may sit near them. Dante was `#ff6d00`, hue 26, FOUR degrees off Offense:
        // a saturated orange fill placed beside other saturated orange fills that mean "this
        // player is an attacker". Cheska was `#00e5ff`, hue 187, twenty off Defence.
        //
        // The set below is spread across the legal arc. Smallest gap between any two heroes is
        // Dante and Cheska at 34 degrees, separated further by lightness (jade L 45 against
        // mint L 64); every other pair is 70 or more apart, which is the spacing a 60 px tile
        // rim needs. Nearest approach to either role hue is Sean at 27 degrees.
        //
        // ⚠️ DANTE IS GREEN AND IT IS DELIBERATE. His kit is magma and orange is the one colour
        // he cannot have, so his accent is the colour of the CRUST, not of the melt: the rim,
        // the tile and the reticle are basalt jade while the fissure light, the embers and the
        // magma core inside them stay hot orange. His ultimate already builds basalt pillars,
        // so the stone half of his fiction was on screen before this. If it is ever reverted,
        // the value it must NOT be reverted to is `#ff6d00`.
        // -------------------------------------------------------------------

        public static readonly Color HeroFire = Hex("ff3355");
        public static readonly Color HeroFireBright = Hex("ff8fa3");
        public static readonly Color HeroIce = Hex("5fe8d0");
        public static readonly Color HeroIceBright = Hex("b8fff2");
        public static readonly Color HeroElectric = Hex("e8f53a");
        public static readonly Color HeroElectricBright = Hex("f6ffa0");
        public static Color HeroLightning => HeroElectric;
        public static Color HeroLightningBright => HeroElectricBright;
        public static readonly Color HeroSpirit = Hex("b44dff");
        public static readonly Color HeroSpiritBright = Hex("dfaaff");
        /// <summary>
        /// Phaister's orchid. Hue 311.
        ///
        /// ⚠️⚠️ IT SHIPPED AT `e82882`, HUE 332, WHICH IS **18.1 DEGREES FROM SEAN** AND BROKE THE
        /// ONE COLOUR LAW THIS GAME HAS. `HeroPresentationTests.TheFiveHeroAccentsAreTellableApart`
        /// caught it the moment the branch merged: *"sean and phaister are only 18.1 degrees
        /// apart, which is one colour on a deck tile"*. The rule is 30 degrees between any two
        /// hero accents and 25 clear of both ROLE colours, because orange tracks the attacker and
        /// blue the defender and those two rotate every round: they are the only colours a player
        /// has to READ rather than merely see.
        ///
        /// ⚠️ 311 IS NOT A TASTE CHOICE, IT IS ONE OF THREE LEGAL WINDOWS. With fire at 350, ice
        /// at 170, electric at 64, spirit at 275, earth at 137 and the two roles at 22 and 207,
        /// exactly three bands satisfy both constraints: 95 to 106 (a yellow-green), 232 to 244
        /// (a blue-violet) and 305 to 320 (this one). The first two are not colours a witch can
        /// have, and inside the third, 311 sits furthest from its nearest neighbour at 36.2
        /// degrees from Nemu's violet and 39.1 from Sean's red.
        ///
        /// ⚠️ THE SATURATION AND VALUE ARE THE ONES THE HERO SHIPPED WITH, carried across
        /// unchanged. Only the hue moved, so she is the same vivid magenta she was authored as,
        /// a step further from Sean and no further from anybody else.
        /// </summary>
        public static readonly Color HeroWitch = Hex("e828c5");
        public static readonly Color HeroWitchBright = Hex("f444d4");
        public static readonly Color HeroEarth = Hex("3fa65c");
        public static readonly Color HeroEarthBright = Hex("8fe0a0");

        /// <summary>
        /// The hot orange that stays hot: Dante's magma core, the fissure light, the embers.
        ///
        /// ⚠️ IT IS NOT AN ACCENT AND MUST NEVER BE USED AS ONE. It exists only inside his own
        /// effects, where it is surrounded by his jade rim and cannot be mistaken for a role
        /// fill on a body or a nameplate. See the note above.
        /// </summary>
        public static readonly Color HeroMagmaCore = Hex("ff9a2e");

        /// <summary>Resolve primary element color for a given hero ID.</summary>
        /// <summary>
        /// The hero's accent at TELEGRAPH brightness, for anything drawn on the ground in the
        /// world rather than on a UI panel.
        ///
        /// ⚠️⚠️ THE BASE ACCENTS ARE PICKED AGAINST CREAM UI, AND ILALIM NG TULAY IS A STREET
        /// UNDER A VIADUCT. 🧑 2026-08-27, on Phaister's hold-to-aim: *"I dont want Phaister's E
        /// HOLD for casting To just be a shadow, keep that outline and give it her color so that
        /// it could be seen more"*. `HeroWitch` is `e828c5`, a saturated but MID-VALUE magenta;
        /// ghosted geometry on a dark asphalt road is lit by its emission and almost nothing
        /// else, so a mid-value colour there is that colour's silhouette. The `*Bright` set is
        /// the same hue lifted to a value that survives the map, and it already exists because
        /// every ability VFX in the game reaches for it for exactly this reason.
        ///
        /// ⚠️ IT FALLS BACK TO THE BASE ACCENT, so a hero without a bright variant is merely
        /// dimmer rather than the wrong colour.
        /// </summary>
        public static Color BrightForHero(string heroId)
        {
            if (string.IsNullOrEmpty(heroId)) return HeroEarthBright;
            switch (heroId.ToLowerInvariant())
            {
                case "sean":
                case "kuya_boy":
                case "iggy":
                    return HeroFireBright;
                case "cheska":
                case "inday":
                    return HeroIceBright;
                case "zack":
                    return HeroElectricBright;
                case "nemu":
                    return HeroSpiritBright;
                case "phaister":
                case "witch":
                    return HeroWitchBright;
                case "dante":
                case "bayan":
                default:
                    return HeroEarthBright;
            }
        }

        public static Color ColorForHero(string heroId)
        {
            if (string.IsNullOrEmpty(heroId)) return HeroEarth;
            switch (heroId.ToLowerInvariant())
            {
                case "sean":
                case "kuya_boy":
                case "iggy":
                    return HeroFire;
                case "cheska":
                case "inday":
                    return HeroIce;
                case "zack":
                    return HeroElectric;
                case "nemu":
                    return HeroSpirit;
                case "phaister":
                case "witch":
                    return HeroWitch;
                case "dante":
                case "bayan":
                default:
                    return HeroEarth;
            }
        }

        /// <summary>
        /// The colour for a seat, by role. ⚠️ BY ROLE AND NOT BY SEAT NUMBER: the taya rotates
        /// every round, so a fixed per-seat colour would tell the player the wrong thing for
        /// three rounds out of four.
        /// </summary>
        public static Color ForRole(bool isDefender) => isDefender ? Defense : Offense;
    }
}

