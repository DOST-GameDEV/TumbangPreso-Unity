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
            { "dante_skill1", AbilityGlyph.DanteShield },
            { "dante_skill2", AbilityGlyph.DanteBoulder },
            { "dante_skill2d", AbilityGlyph.DanteBarrier },
            { "dante_ultimate", AbilityGlyph.DanteFissure },

            // Sean (Fire Brawler)
            { "sean_skill1", AbilityGlyph.SeanRush },
            { "sean_skill2", AbilityGlyph.SeanIgnite },
            { "sean_ultimate", AbilityGlyph.SeanSupernova },

            // Cheska (Ice Guardian)
            { "cheska_skill1", AbilityGlyph.CheskaFrostSheet },
            { "cheska_skill2", AbilityGlyph.CheskaFrostbite },
            { "cheska_skill2d", AbilityGlyph.CheskaBarricade },
            { "cheska_ultimate", AbilityGlyph.CheskaNova },

            // Zack (the Speedster, 2026-10-25)
            { "zack_skill1", AbilityGlyph.ZackBacktrack },
            { "zack_skill2", AbilityGlyph.ZackAfterimage },
            { "zack_skill2d", AbilityGlyph.ZackLiveWire },
            { "zack_ultimate", AbilityGlyph.ZackOverclock },

            // Nemu (Spirit Summoner)
            { "nemu_skill1", AbilityGlyph.NemuDontLook },
            { "nemu_skill2", AbilityGlyph.NemuSkitter },
            { "nemu_skill2d", AbilityGlyph.NemuLookout },
            { "nemu_ultimate", AbilityGlyph.NemuSeanceVoid },

            // Phaister (Street Witch)
            { "phaister_skill1", AbilityGlyph.PhaisterShadowBlink },
            { "phaister_skill2", AbilityGlyph.PhaisterCursedDoll },
            { "phaister_skill2d", AbilityGlyph.PhaisterVulnerable },
            { "phaister_ultimate", AbilityGlyph.PhaisterVoodooDoll },
            { "rafi_skill1", AbilityGlyph.RafiCrosscurrent },
            { "rafi_skill2", AbilityGlyph.RafiMirrorwake },
            { "rafi_ultimate", AbilityGlyph.RafiBreakwater },
            { "amihan_skill1", AbilityGlyph.AmihanQuickDash },
            { "amihan_skill2", AbilityGlyph.AmihanUpdraft },
            { "amihan_skill2d", AbilityGlyph.AmihanWhirlwind },
            { "amihan_ultimate", AbilityGlyph.AmihanStormSurge },
            { "paete_skill1", AbilityGlyph.PaeteVine },
            { "paete_skill2", AbilityGlyph.PaeteSprout },
            { "paete_skill2d", AbilityGlyph.PaeteThorn },
            { "paete_ultimate", AbilityGlyph.PaeteSentry },
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
