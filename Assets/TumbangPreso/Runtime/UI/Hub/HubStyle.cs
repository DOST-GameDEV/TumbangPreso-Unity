using UnityEngine;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// The UX-1 front end's palette, type and sizes, in one place.
    ///
    /// ⚠️⚠️ EVERY COLOUR IS THE LOGO'S, NONE IS NEW. `CLAUDE.md` § 6.4's table is the source and
    /// `tools/read_brand_palette.py` measured it. The one derived value is <see cref="Ink"/>, the
    /// warm black the in-game UI already outlines in (`AGENTS.md`, black outlines 2026-09-22) and
    /// the front end's old `UiTheme.Ink`: a sticker outline that is the same black in a menu and in
    /// a match reads as one game.
    ///
    /// ⚠️ NO BLUE, NO NAVY, NO COLD GREY, AND NO PALE DEFAULT GROUND. The owner's brief for UX-1:
    /// "no white or pale default palettes". Honey Quartz appears only as a small tag or a text
    /// colour on a dark sticker, never as a screen's ground; grounds are the live court, Army and
    /// the warm dark <see cref="Night"/>.
    ///
    /// ⚠️ THE ROLE TABLE IS THE SAME AS `Front_End_Design.md` § 4 AND THAT IS THE POINT: Chartreuse
    /// is the one primary per screen, Persimmon is the one marker, deep red is the outline of the
    /// destructive thing. A new screen picks a role, not a colour.
    /// </summary>
    public static class HubStyle
    {
        public static readonly Color Ink = Hex(0x1C0F06);
        public static readonly Color DeepRed = Hex(0x980715);
        public static readonly Color RimRed = Hex(0xC32E0D);
        public static readonly Color Honey = Hex(0xFCD39F);
        public static readonly Color Chartreuse = Hex(0xD6CE01);
        public static readonly Color Persimmon = Hex(0xFD8041);
        public static readonly Color Golden = Hex(0xF5B521);
        public static readonly Color Army = Hex(0xB3A828);

        /// <summary>Army taken down two thirds in value: the dark card body. Hue 55, red over blue.</summary>
        public static readonly Color ArmyDeep = Hex(0x4A4510);

        /// <summary>The warm dark behind popups and under the queue plate. Red 42 over blue 11.</summary>
        public static readonly Color Night = Hex(0x2A160B);

        /// <summary>The dimmer behind a popup. 70 per cent of <see cref="Night"/>, measured to keep
        /// the court readable as a place without competing with the popup's own lettering.</summary>
        public static Color Scrim => new Color(Night.r, Night.g, Night.b, 0.72f);

        /// <summary>Honey at half strength: secondary lettering on a dark sticker.</summary>
        public static Color HoneySoft => new Color(Honey.r, Honey.g, Honey.b, 0.78f);

        // ------------------------------------------------------------------ type

        /// <summary>Canvas-unit type steps. ⚠️ THE FLOOR IS 28, the brief's number and
        /// `TumpNativeHudTests`' floor for the match; nothing in the hub is drawn smaller.</summary>
        public const int Floor = 28;
        public const int Body = 30;
        public const int Label = 34;
        public const int Title = 48;
        public const int Display = 76;
        public const int Hero = 132;

        public static Font DisplayFont => _display != null ? _display
            : _display = Resources.Load<Font>("UI/fonts/DarumadropOne-Regular");

        /// <summary>
        /// ⚠️ NUNITO IS THE SUPPORTING FACE, PER `Front_End_Design.md` § 3: the sub font for the
        /// things a player reads underneath the display lettering, never the other way round.
        /// Bold, because the reading lines sit on saturated stickers and over a live court.
        /// </summary>
        public static Font ReadingFont => _reading != null ? _reading
            : _reading = Resources.Load<Font>("UI/fonts/Nunito-Bold");

        private static Font _display, _reading;

        /// <summary>
        /// The size a label is actually drawn at: its authored step, raised by the Larger text
        /// setting. ⚠️ Never below <see cref="Floor"/>, whatever a caller asks for.
        /// </summary>
        public static int Size(int step)
        {
            var settings = Settings.SettingsStore.Current;
            float scale = settings != null && settings.LargerText ? 1.15f : 1.0f;
            return Mathf.Max(Floor, Mathf.RoundToInt(step * scale));
        }

        /// <summary>High contrast thickens every sticker outline, the same setting the HUD reads.</summary>
        public static float OutlineScale
        {
            get
            {
                var settings = Settings.SettingsStore.Current;
                return settings != null && settings.HighContrastHud ? 1.45f : 1.0f;
            }
        }

        public static bool ReducedMotion
        {
            get
            {
                var settings = Settings.SettingsStore.Current;
                return settings != null && settings.ReducedUiMotion;
            }
        }

        public static Color Hex(int rgb) =>
            new Color32((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF), 255);

        /// <summary>Ink or Honey, whichever reads on <paramref name="fill"/>.</summary>
        public static Color TextOn(Color fill)
        {
            float luminance = 0.2126f * fill.r + 0.7152f * fill.g + 0.0722f * fill.b;
            return luminance > 0.42f ? Ink : Honey;
        }
    }
}
