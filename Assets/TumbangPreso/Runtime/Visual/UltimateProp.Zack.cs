using UnityEngine;

namespace TumbangPreso.Visual
{
    public static partial class UltimateProp
    {
        /// <summary>
        /// Zack's storm (`tools/build_ultimate_props_zack.py`, slot for slot). The buildings' fronts and the clouds are
        /// PAINTED (`zack_storm-atlas.png` beside the models), so these sixteen are only the flat pieces on them: steel,
        /// trim, roof slabs, a mast's red lamp, an awning, a lit hoarding.
        ///
        /// ⚠️ NOTHING HERE, AND NOTHING IN THE PAINT, IS AS DARK AS HIS HAIR, ON PURPOSE. His hair is black and the lens puts
        /// the skyline right behind his head in three shots of five; the first skyline was near black and his head sank
        /// into it.
        /// </summary>
        public static readonly Color[] StormPalette =
        {
            Hex(0x3A4054), Hex(0x2C3040), Hex(0x20232D), Hex(0xE2B85C), Hex(0x94683A), Hex(0x191B23), Hex(0x5B6276), Hex(0x4D6680),
            Hex(0xFF5A4A), Hex(0x3D4252), Hex(0x282B36), Hex(0x535A70), Hex(0x6C7389), Hex(0x4C5162), Hex(0x8A4B3A), Hex(0x14161C),
        };

        /// <summary>
        /// The same city one street further off: the flat pieces pulled most of the way into the storm's own haze, and
        /// <see cref="StormFarTint"/> laid over everything, paint included (a painted piece takes no palette), so the far
        /// ring reads as behind the near one.
        /// </summary>
        public static readonly Color[] StormFarPalette = Hazed(StormPalette, Hex(0x3A3D50), .5f);
        public static readonly Color StormFarTint = new Color(.62f, .64f, .80f, 1f);

        private static Color[] Hazed(Color[] from, Color haze, float amount)
        {
            var to = new Color[from.Length];
            for (int i = 0; i < from.Length; i++) to[i] = Color.Lerp(from[i], haze, amount);
            return to;
        }
    }
}
