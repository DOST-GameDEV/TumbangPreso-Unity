using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The settings workspace's dark grey, which the owner asked for on 2026-09-15.
    ///
    /// ⚠️⚠️ THE GREY IS WARM, AND IT WAS NOT UNTIL 2026-09-23. The first values (29,31,35 ground,
    /// 42,45,50 surface, 58,62,68 control, 94,101,110 rule) all carried more blue than red, which is
    /// `CLAUDE.md` § 6.4's "cold grey" by its own test ("if a hex has more blue in it than red, it
    /// does not belong in a menu"), and next to the warm HOME and the in-match cards the settings
    /// page read as a generic dashboard pasted into the game. The same values with the red channel
    /// lifted over the blue keep the owner's dark grey and the same contrast steps (ground to
    /// surface to control each about +12 to +16 in value), so nothing that was readable stopped
    /// being readable. Controller map and touch layout share this palette on purpose.
    /// </summary>
    public static class SettingsPalette
    {
        // UI revamp 2026-10-06: the match HUD's toy family. The owner's dark workspace stays dark,
        // now the warm brown of the clock tile; cream words and honey for the live choice.
        public static readonly Color Background = new Color32(44, 27, 17, 255);
        public static readonly Color Surface = new Color32(58, 37, 24, 255);
        public static readonly Color Control = new Color32(80, 53, 35, 255);
        public static readonly Color Ink = new Color32(255, 245, 224, 255);
        public static readonly Color Muted = new Color32(214, 190, 160, 255);
        public static readonly Color Accent = new Color32(255, 196, 64, 255);
        public static readonly Color Rule = new Color32(112, 80, 56, 255);
        public static readonly Color Pressed = new Color32(255, 222, 150, 255);

        /// <summary>The dark lettering drawn ON the accent (the filled SAVE, a listening keycap).</summary>
        public static readonly Color OnAccent = new Color32(56, 33, 20, 255);

        /// <summary>The live row's band: accent at 18 per cent over the surface (see
        /// `SettingsControlFocus` for why 9 per cent was not enough).</summary>
        public static Color RowFocus => new Color(Accent.r, Accent.g, Accent.b, 0.18f);
    }
}
