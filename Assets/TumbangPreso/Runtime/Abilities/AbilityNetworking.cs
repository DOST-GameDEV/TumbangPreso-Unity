using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.UI;

namespace TumbangPreso.Abilities
{
    public enum AbilityNetworkMode
    {
        Unspecified,
        Predicted,
        HostConfirmed,
        SharedUltimate,
        Unavailable
    }

    public static class AbilityNetworking
    {
        public static void Validate(HeroKit kit)
        {
            if (kit == null) throw new ArgumentNullException(nameof(kit));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var ability in kit.AllAbilities)
            {
                if (ability == null || string.IsNullOrWhiteSpace(ability.Id) || !ids.Add(ability.Id))
                    throw new InvalidOperationException(kit.HeroId + " has a missing or duplicate network ability ID.");
                var mode = ability.NetworkMode;
                if ((int)mode <= 0 || (int)mode > (int)AbilityNetworkMode.Unavailable)
                    throw new InvalidOperationException(ability.Id + " must declare its network delivery mode.");
                if (mode == AbilityNetworkMode.Unavailable)
                {
                    if (ability.Glyph != AbilityGlyph.ComingSoon)
                        throw new InvalidOperationException(ability.Id + " cannot hide a real skill behind an unavailable network mode.");
                    continue;
                }
                if ((ability == kit.Ultimate) != (mode == AbilityNetworkMode.SharedUltimate))
                    throw new InvalidOperationException(ability.Id + " must use the shared ultimate route only in the ultimate slot.");
            }
        }

        public static void ValidateRoster()
        {
            foreach (var hero in Roster.HeroPeople)
            {
                var kit = HeroAbilitySystem.CreateKitFor(hero.Id);
                if (!string.Equals(hero.Id, kit.HeroId, StringComparison.Ordinal))
                    throw new InvalidOperationException(hero.Id + " has no explicit hero-kit factory registration.");
                Validate(kit);
            }
        }
    }
}
