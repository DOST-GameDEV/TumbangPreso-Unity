using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Central registry and lookup for bespoke hero ability icons and glyph definitions.
    /// Ensures every ability in the game has a unique, non-overlapping glyph representation.
    /// </summary>
    public static class HeroGlyphs
    {
        private static readonly Dictionary<string, AbilityGlyph> AbilityToGlyphMap = new Dictionary<string, AbilityGlyph>
        {
            // Dante (Earth Juggernaut)
            { "dante_skill1", AbilityGlyph.DanteStomp },
            { "dante_skill2", AbilityGlyph.DanteShield },
            { "dante_ultimate", AbilityGlyph.DanteFissure },

            // Sean (Fire Brawler)
            { "sean_skill1", AbilityGlyph.SeanRush },
            { "sean_skill2", AbilityGlyph.SeanIgnite },
            { "sean_ultimate", AbilityGlyph.SeanSupernova },

            // Cheska (Ice Guardian)
            { "cheska_skill1", AbilityGlyph.CheskaFrostSheet },
            { "cheska_skill2", AbilityGlyph.CheskaBarricade },
            { "cheska_ultimate", AbilityGlyph.CheskaNova },

            // Zack (Lightning Skater)
            { "zack_skill1", AbilityGlyph.ZackSprint },
            { "zack_skill2", AbilityGlyph.ZackOvercharge },
            { "zack_ultimate", AbilityGlyph.ZackThunderstrike },

            // Nemu (Spirit Summoner)
            { "nemu_skill1", AbilityGlyph.NemuPhase },
            { "nemu_skill2", AbilityGlyph.NemuAstralPet },
            { "nemu_ultimate", AbilityGlyph.NemuSeanceVoid },

            // Phaister (Street Witch)
            { "phaister_skill1", AbilityGlyph.PhaisterHexSigil },
            { "phaister_skill2", AbilityGlyph.PhaisterShadowBlink },
            { "phaister_ultimate", AbilityGlyph.PhaisterEclipse },
        };

        /// <summary>
        /// Resolve the unique bespoke glyph for a given ability identifier.
        /// </summary>
        public static AbilityGlyph ForAbilityId(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId)) return AbilityGlyph.Burst;
            return AbilityToGlyphMap.TryGetValue(abilityId, out var glyph) ? glyph : AbilityGlyph.Burst;
        }

        /// <summary>
        /// Resolve the unique bespoke sprite for a given ability identifier.
        /// </summary>
        public static Sprite SpriteForAbilityId(string abilityId)
        {
            return AbilityIcons.For(ForAbilityId(abilityId));
        }
    }
}
