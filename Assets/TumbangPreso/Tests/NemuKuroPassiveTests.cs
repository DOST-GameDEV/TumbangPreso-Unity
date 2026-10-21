using NUnit.Framework;
using System.Reflection;
using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class NemuKuroPassiveTests
    {
        // SOULBOUND (owner 2026-10-21) replaced DEAD WEIGHT, which had replaced KURO!: her passive never touches her own
        // speed, whatever her cooldowns are doing, and her three skills no longer share one clock.
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void HerOwnSpeedNeverChangesWithHerCooldowns(int abilityIndex)
        {
            var kit = new NemuHeroKit();
            var ability = abilityIndex == 0 ? kit.Skill1
                : abilityIndex == 1 ? kit.AttackingSkill : kit.DefendingSkill;
            ability.ApplyNetworkSnapshot(2f, 0);

            Assert.That(kit.MovementSpeedScale, Is.EqualTo(1f));
        }

        [Test]
        public void TheTrayNamesSoulboundAndTheReclaim()
        {
            var passive = HeroPassives.For("nemu");
            Assert.That(passive.Name, Is.EqualTo("SOULBOUND"));
            Assert.That(passive.KitRule, Does.Contain("reclaimed"));
        }

        [Test]
        public void TheSkillsCarryTheirNewNames()
        {
            var kit = new NemuHeroKit();
            Assert.That(kit.Skill1.Name, Is.EqualTo("PEEKER"));
            Assert.That(kit.AttackingSkill.Name, Is.EqualTo("FETCHER"));
            Assert.That(kit.DefendingSkill.Name, Is.EqualTo("KEEPER"));
            Assert.That(kit.Skill1.Id, Is.EqualTo("nemu_skill1"), "IDs stay for saves, mastery and the wire");
        }
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void AcceptedBasicCastStartsOnlyItsOwnTwentySecondCooldown(int abilityIndex)
        {
            var go = new GameObject("Nemu accepted cooldown contract");
            var kit = new NemuHeroKit();
            var context = new AbilityContext(go.AddComponent<CharacterMotor>(), null, null);
            try
            {
                var ability = abilityIndex == 0 ? kit.Skill1
                    : abilityIndex == 1 ? kit.AttackingSkill : kit.DefendingSkill;
                float banked = kit.UltimateCharge;
                // Accepted observer playback calls Activate directly.
                using (NetCue.SuppressRelay()) ability.Activate(context);

                Assert.That(kit.Skill1.CooldownRemaining, Is.EqualTo(abilityIndex == 0 ? 20f : 0f));
                Assert.That(kit.AttackingSkill.CooldownRemaining, Is.EqualTo(abilityIndex == 1 ? 20f : 0f));
                Assert.That(kit.DefendingSkill.CooldownRemaining, Is.EqualTo(abilityIndex == 2 ? 20f : 0f));
                Assert.That(kit.Ultimate.CooldownRemaining, Is.Zero);
                Assert.That(kit.UltimateCharge, Is.EqualTo(banked));
            }
            finally
            {
                kit.ResetForRound(context);
                Object.DestroyImmediate(go);
                foreach (var g in Object.FindObjectsByType<NemuBlindfold>(FindObjectsSortMode.None)) Object.DestroyImmediate(g.gameObject);
                foreach (var g in Object.FindObjectsByType<NemuSkitter>(FindObjectsSortMode.None)) Object.DestroyImmediate(g.gameObject);
            }
        }

        [Test]
        public void RefusedBasicCastDoesNotStartAnyCooldown()
        {
            var kit = new NemuHeroKit();

            Assert.That(kit.CastSkill1(null), Is.EqualTo(HeroKit.CastOutcome.CannotAct));
            Assert.That(kit.Skill1.CooldownRemaining, Is.Zero);
            Assert.That(kit.AttackingSkill.CooldownRemaining, Is.Zero);
            Assert.That(kit.DefendingSkill.CooldownRemaining, Is.Zero);
        }
        private sealed class PredictingOwner : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => 2;
            public int LocalPeerId => 2;
            public bool IsSeatlessReferee => false;
        }

        private static HeroAbilitySystem ReceiptSystem(GameObject go, string hero = "nemu")
        {
            go.SetActive(false);
            var motor = go.AddComponent<CharacterMotor>();
            motor.PlayerSlot = 2;
            var system = go.AddComponent<HeroAbilitySystem>();
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(HeroAbilitySystem).GetField("_motor", fields).SetValue(system, motor);
            typeof(HeroAbilitySystem).GetField("_context", fields)
                .SetValue(system, new AbilityContext(motor, null, null));
            system.BindHero(hero);
            return system;
        }

        private static void SetBasicClocks(HeroKit kit, float value)
        {
            kit.Skill1.ApplyNetworkSnapshot(value, 0);
            kit.AttackingSkill.ApplyNetworkSnapshot(value, 0);
            kit.DefendingSkill.ApplyNetworkSnapshot(value, 0);
        }

        [TestCase("zack")]
        [TestCase("nemu")]
        public void IndependentKitReceiptStillCorrectsOnlyItsOwnAbility(string hero)
        {
            var previous = NetAuthority.Provider;
            var go = new GameObject("Independent resource receipt");
            try
            {
                NetAuthority.Provider = new PredictingOwner();
                var system = ReceiptSystem(go, hero);
                SetBasicClocks(system.Kit, 25f);
                Assert.That(system.TrackSkillRequest(0, 301), Is.True);
                Assert.That(system.TrackSkillRequest(1, 302), Is.True);

                Assert.That(system.ResolveSkillReceipt(0, 301, false, 0f, 0), Is.True);

                Assert.That(system.Kit.Skill1.CooldownRemaining, Is.Zero);
                Assert.That(system.Kit.AttackingSkill.CooldownRemaining, Is.EqualTo(25f));
                Assert.That(system.Kit.DefendingSkill.CooldownRemaining, Is.EqualTo(25f));
            }
            finally { Object.DestroyImmediate(go); NetAuthority.Provider = previous; }
        }
    }
}
