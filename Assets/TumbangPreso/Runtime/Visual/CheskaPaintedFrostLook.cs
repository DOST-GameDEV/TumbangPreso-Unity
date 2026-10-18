using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Painted ice for Cold Feet and Frostbite, matching the Glacial Wall's painted direction.
    /// Both switches default to FALSE so the shipped look is unchanged until the owner turns
    /// them on. Each new shader keeps the original's property contract, so the presentation
    /// classes drive it with no other change. Loaded from Resources so a build always carries it.
    /// </summary>
    public static class CheskaPaintedFrostLook
    {
        public static bool PaintedColdFeet = false;
        public static bool PaintedFrostbite = false;

        public static Shader Field(Shader original)
        {
            if (!PaintedColdFeet) return original;
            var painted = Resources.Load<Shader>("Shaders/CheskaColdFeetPainted");
            return painted != null ? painted : original;
        }

        public static Shader Coat(Shader original)
        {
            if (!PaintedFrostbite) return original;
            var painted = Resources.Load<Shader>("Shaders/CheskaFrostbitePainted");
            return painted != null ? painted : original;
        }
    }
}
