using UnityEngine;

namespace TumbangPreso.UI
{
    // Seat identity is independent of hero element and rotating taya role.
    // Owner, 2026-10-06: one colour per player number, everywhere a player appears, from the TUMP
    // guide's own palette: P1 Strawberry Red, P2 Honey Bronze, P3 Light Green, P4 Cool Horizon.
    // The role is carried by the can and slipper icons, never by a team colour.
    public static class PlayerIdentity
    {
        private static readonly Color[] Colours = {
            new Color32(254, 72, 73, 255), new Color32(252, 189, 91, 255),
            new Color32(154, 229, 121, 255), new Color32(94, 161, 245, 255)
        };
        public static Color Colour(int slot) => slot >= 0 && slot < Colours.Length ? Colours[slot] : Color.white;
        public static string Label(int slot) => slot >= 0 && slot < Colours.Length ? "P" + (slot + 1) : "?";
    }
}
