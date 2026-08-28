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

        /// <summary>Near-black navy: text, borders, pressed fills.</summary>
        public static readonly Color Ink = Hex("040838");

        /// <summary>Light neutral: screen background.</summary>
        public static readonly Color Panel = Hex("e1e5e8");

        /// <summary>Slightly lighter: raised card and control fill.</summary>
        public static readonly Color Card = Hex("f5f7fa");

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
        // THE WOOD SET. The in-match HUD panels and the menu chrome.
        // ⚠️ This is the look the pitch deck and the sponsorship proposals were both
        // designed from, so it is effectively the team's brand and not only a UI skin.
        // -------------------------------------------------------------------

        public static readonly Color WoodDeep = Hex("31190b");
        public static readonly Color WoodMid = Hex("5a2f14");
        public static readonly Color WoodDark = Hex("1d0e06");
        public static readonly Color WoodEdge = Hex("8b5227");
        public static readonly Color Cream = Hex("f5e6c8");
        public static readonly Color Amber = Hex("ffba00");

        public static Color CreamMuted => new Color(Cream.r, Cream.g, Cream.b, 0.68f);

        public static readonly Color MenuGreen = Hex("21a131");
        public static readonly Color MenuGreenLit = Hex("69e548");
        public static readonly Color MenuRed = Hex("ed2136");
        public static readonly Color MenuRedLit = Hex("fa7653");

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

