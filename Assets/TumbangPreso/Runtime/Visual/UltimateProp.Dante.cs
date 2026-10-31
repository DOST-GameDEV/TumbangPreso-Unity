using UnityEngine;

namespace TumbangPreso.Visual
{
    public static partial class UltimateProp
    {
        /// <summary>
        /// The flat colours of Basilio's longhorn (`Models/UltimateProps/longhorn-titan`, `tools/build_ultimate_props_dante_titan.py`,
        /// and the first one, `longhorn`, `tools/build_ultimate_props_dante.py`; restored 2026-10-29 with the longhorn cutscene):
        /// its molten seams, its ember eyes, its mouth and the dark of its hollows. Its rock, horns and muzzle are PAINTED
        /// (the atlas beside the model), so most of these slots are only what the paint was mixed from. The slot numbers
        /// match the builder's table.
        /// </summary>
        public static readonly Color[] EarthPalette =
        {
            Hex(0x4B4441), Hex(0x322D2C), Hex(0x6B6058), Hex(0xFF7A18), Hex(0xFFC548), Hex(0xE6D6B4), Hex(0xA98F6C), Hex(0xFFA21F),
            Hex(0xFFF1B8), Hex(0xC23A16), Hex(0xF2E8D0), Hex(0x1E1A1A), Hex(0x8C8178), Hex(0xB8420E), Hex(0x7D726B), Hex(0x151212),
        };
    }
}
