using UnityEngine;

namespace TumbangPreso.UI
{
    // Owner requested a dark grey settings workspace; other screens keep their palettes.
    public static class SettingsPalette
    {
        public static readonly Color Background = new Color32(29, 31, 35, 255);
        public static readonly Color Surface = new Color32(42, 45, 50, 255);
        public static readonly Color Control = new Color32(58, 62, 68, 255);
        public static readonly Color Ink = new Color32(240, 238, 231, 255);
        public static readonly Color Muted = new Color32(180, 187, 192, 255);
        public static readonly Color Accent = new Color32(194, 211, 106, 255);
        public static readonly Color Rule = new Color32(94, 101, 110, 255);
        public static readonly Color Pressed = new Color32(221, 231, 158, 255);
    }
}
