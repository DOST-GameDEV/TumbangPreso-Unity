using UnityEngine;

namespace TumbangPreso.UI
{
    // Seat identity is independent of hero element and rotating taya role.
    public static class PlayerIdentity
    {
        private static readonly Color[] Colours = {
            new Color32(120, 220, 222, 255), new Color32(255, 158, 122, 255),
            new Color32(244, 218, 114, 255), new Color32(204, 177, 244, 255)
        };
        public static Color Colour(int slot) => slot >= 0 && slot < Colours.Length ? Colours[slot] : Color.white;
        public static string Label(int slot) => slot >= 0 && slot < Colours.Length ? "P" + (slot + 1) : "?";
    }
}
