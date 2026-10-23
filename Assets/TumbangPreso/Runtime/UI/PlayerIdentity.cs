using UnityEngine;

namespace TumbangPreso.UI
{
    // Seat identity is independent of hero element and rotating taya role.
    // Owner, 2026-10-06: one colour per player number, everywhere a player appears, from the TUMP
    // guide's own palette: P1 Grape Purple, P2 Honey Bronze, P3 Light Green, P4 Cool Horizon.
    // P1 was Strawberry Red until 2026-10-23 (owner: "No red seat colour"): a red ring under a player read as the
    // taya or as danger after their taya round ended, and red fights the taggable fill and Offense's "can down".
    // The role is carried by the can and slipper icons, never by a team colour.
    public static class PlayerIdentity
    {
        private static readonly Color[] Colours = {
            new Color32(168, 112, 255, 255), new Color32(252, 189, 91, 255),
            new Color32(154, 229, 121, 255), new Color32(94, 161, 245, 255)
        };
        public static Color Colour(int slot) => slot >= 0 && slot < Colours.Length ? Colours[slot] : Color.white;
        public static string Label(int slot) => slot >= 0 && slot < Colours.Length ? "P" + (slot + 1) : "?";
    }
}
