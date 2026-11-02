using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The settings family in the boot kit (owner 2026-10-30, `docs/UI_REVAMP.md`). The plain names
    /// are for controls standing on the berry weave (controller map, touch layout, option popups);
    /// the Panel names are for words on the cream panel.
    /// </summary>
    public static class SettingsPalette
    {
        public static readonly Color Background = new Color32(106, 27, 44, 255);
        public static readonly Color Surface = new Color32(138, 42, 60, 255);
        public static readonly Color Control = new Color32(92, 20, 34, 255);
        public static readonly Color Ink = new Color32(243, 234, 226, 255);
        public static readonly Color Muted = new Color32(226, 180, 140, 255);
        public static readonly Color Accent = new Color32(191, 216, 58, 255);
        public static readonly Color Rule = new Color32(226, 180, 140, 255);
        public static readonly Color Pressed = new Color32(255, 250, 244, 255);

        /// <summary>The dark lettering drawn ON the accent (the filled SAVE, a listening keycap).</summary>
        public static readonly Color OnAccent = new Color32(15, 89, 19, 255);

        public static readonly Color PanelInk = new Color32(74, 16, 24, 255);
        public static readonly Color PanelHead = new Color32(184, 134, 96, 255);
        public static readonly Color PanelAccent = new Color32(134, 20, 34, 255);

        /// <summary>The live row's band on the cream panel (see `SettingsControlFocus`).</summary>
        public static Color RowFocus => new Color(Rule.r, Rule.g, Rule.b, 0.3f);
    }
}
