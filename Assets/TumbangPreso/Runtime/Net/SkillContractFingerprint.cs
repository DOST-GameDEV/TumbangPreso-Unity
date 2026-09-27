using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Net
{
    public static class SkillContractFingerprint
    {
        private static string _current;
        public static string Current => _current ??= Compute(CurrentKits(),
            (hero, held) => UltimatePerformance.For(hero, held)?.Seconds ?? UltimatePerformance.DefaultSeconds);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => _current = null;

        private static IEnumerable<HeroKit> CurrentKits()
        {
            foreach (var hero in Roster.HeroPeople) yield return HeroAbilitySystem.CreateKitFor(hero.Id);
        }

        public static bool Matches(string received)
            => received != null && received.Length == 64 && string.Equals(received, Current, StringComparison.Ordinal);

        // Only shared simulation/phase metadata belongs here. Cosmetic files,
        // labels, glyphs, palettes, clips and cue names must not invalidate a peer.
        // This supplements the protocol version; arbitrary code changes still need
        // explicit versioning and coverage of their own serialization/state contract.
        public static string Compute(IEnumerable<HeroKit> kits, Func<string, bool, float> introductionSeconds)
        {
            if (kits == null) throw new ArgumentNullException(nameof(kits));
            if (introductionSeconds == null) throw new ArgumentNullException(nameof(introductionSeconds));
            using var bytes = new MemoryStream();
            using (var writer = new BinaryWriter(bytes, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write("TUMP-skill-contract-1");
                int count = 0;
                foreach (var kit in kits)
                {
                    AbilityNetworking.Validate(kit);
                    writer.Write(count++); // Roster order is also a replicated index.
                    writer.Write(kit.HeroId);
                    writer.Write(kit.UltimateCost);
                    writer.Write(kit.HasRoleAbilities);
                    WriteAbility(writer, kit.Skill1);
                    // Canonical role order, independent of this body's current taya role.
                    WriteAbility(writer, kit.HasRoleAbilities ? kit.AttackingSkill : kit.Skill2);
                    WriteAbility(writer, kit.HasRoleAbilities ? kit.DefendingSkill : null);
                    WriteAbility(writer, kit.Ultimate);
                    writer.Write(introductionSeconds(kit.HeroId, false));
                    writer.Write(introductionSeconds(kit.HeroId, true));
                }
                writer.Write(count);
            }
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(bytes.ToArray())).Replace("-", "");
        }

        private static void WriteAbility(BinaryWriter writer, HeroAbility ability)
        {
            writer.Write(ability != null);
            if (ability == null) return;
            writer.Write(ability.Id);
            writer.Write((int)ability.NetworkMode);
            writer.Write(ability.Cooldown);
            writer.Write(ability.Duration);
            writer.Write(ability.Windup);
            writer.Write(ability.MaxCharges);
            writer.Write((int)ability.RechargedBy);
            writer.Write(ability.SupportsPendingSnapshot);
            writer.Write(ability.CanReactivate);
            writer.Write(ability.HoldToAim);
            writer.Write(ability.AimsWhereLooking);
            writer.Write(ability.AimsInTheAir);
            writer.Write(ability.AimMinHeight);
            writer.Write(ability.AimMaxHeight);
            writer.Write(ability.AimMinRange);
            writer.Write(ability.AimMaxRange);
            writer.Write(ability.AimRampSeconds);
            writer.Write(ability.MaxAimSeconds);
        }
    }
}
