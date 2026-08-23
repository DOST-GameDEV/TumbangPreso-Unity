using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Utility and controller for the in-game Hero Ability Deck HUD.
    /// Manages the three ability cards (Skill1, Skill2, Ultimate), state rendering,
    /// cooldown clocks, pop responses, and bespoke ability glyph displays.
    /// </summary>
    public static class AbilityDeckHud
    {
        public const float CastFlashSeconds = 0.14f;
        public const float RefusalFlashSeconds = 0.12f;
        public const float ReadyPopSeconds = 0.18f;

        /// <summary>
        /// Paints the appropriate bespoke glyph for the ability.
        /// </summary>
        public static void PaintGlyph(Image target, Abilities.HeroAbility ability)
        {
            if (target == null || ability == null) return;
            var want = AbilityIcons.For(ability.Glyph);
            if (target.sprite != want) target.sprite = want;
        }

        /// <summary>
        /// Resolves the human-readable description label for an ability's glyph.
        /// </summary>
        public static string GetGlyphLabel(Abilities.HeroAbility ability)
        {
            if (ability == null) return "POWER";
            return AbilityIcons.LabelFor(ability.Glyph);
        }
    }
}
