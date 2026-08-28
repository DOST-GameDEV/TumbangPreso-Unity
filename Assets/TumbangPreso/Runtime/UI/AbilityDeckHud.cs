using System;
using System.Globalization;
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

        /// <summary>Formats cooldowns for fast reading without rebuilding text every tenth for
        /// the entire cooldown.</summary>
        public static string CooldownLabel(float secondsRemaining)
        {
            if (secondsRemaining <= 0.0f) return string.Empty;
            return secondsRemaining < 3.0f
                ? secondsRemaining.ToString("0.0", CultureInfo.InvariantCulture)
                : Mathf.CeilToInt(secondsRemaining).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Returns the remaining radial veil. One means fully covered, zero means ready.</summary>
        public static float CooldownSweep(float secondsRemaining, float totalSeconds)
        {
            if (totalSeconds <= 0.0f) return 0.0f;
            return Mathf.Clamp01(secondsRemaining / totalSeconds);
        }

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
