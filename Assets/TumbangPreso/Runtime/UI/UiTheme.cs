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
        // HERO ELEMENTS. Saturated kid-friendly arcade colors for Hero Strike mode.
        // -------------------------------------------------------------------

        public static readonly Color HeroFire = Hex("ff2a44");
        public static readonly Color HeroFireBright = Hex("ff7043");
        public static readonly Color HeroIce = Hex("00e5ff");
        public static readonly Color HeroIceBright = Hex("80d8ff");
        public static readonly Color HeroElectric = Hex("ffea00");
        public static readonly Color HeroElectricBright = Hex("ffff52");
        public static readonly Color HeroSpirit = Hex("d500f9");
        public static readonly Color HeroSpiritBright = Hex("e040fb");
        public static readonly Color HeroEarth = Hex("ff6d00");
        public static readonly Color HeroEarthBright = Hex("ffab40");

        /// <summary>Resolve primary element color for a given hero ID.</summary>
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

