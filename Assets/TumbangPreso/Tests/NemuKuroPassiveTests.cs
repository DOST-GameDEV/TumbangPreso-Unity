using NUnit.Framework;
using System.Reflection;
using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class NemuKuroPassiveTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void AnyBasicCooldownGivesTenPercentMovementSpeed(int abilityIndex)
        {
            var kit = new NemuHeroKit();
            var ability = abilityIndex == 0 ? kit.Skill1
                : abilityIndex == 1 ? kit.AttackingSkill : kit.DefendingSkill;
            ability.ApplyNetworkSnapshot(2f, 0);

            Assert.That(kit.MovementSpeedScale, Is.EqualTo(1.1f).Within(0.0001f));
        }

        [Test]
        public void SeveralCooldownsDoNotStackTheBonus()
        {
            var kit = new NemuHeroKit();
            kit.Skill1.ApplyNetworkSnapshot(2f, 0);
            kit.AttackingSkill.ApplyNetworkSnapshot(3f, 0);
            kit.DefendingSkill.ApplyNetworkSnapshot(4f, 0);

            Assert.That(kit.MovementSpeedScale, Is.EqualTo(1.1f).Within(0.0001f));
        }

        [Test]
        public void ReadyKitHasNormalSpeedEvenWithUltimateCharge()
        {
            var kit = new NemuHeroKit();
            kit.AddUltimateCharge(kit.UltimateCost);

            Assert.That(kit.MovementSpeedScale, Is.EqualTo(1f));
        }

        [Test]
        public void ExpiringTheLastCooldownRemovesTheBonus()
        {
            var kit = new NemuHeroKit();
            kit.Skill1.ApplyNetworkSnapshot(0.1f, 0);
            Assert.That(kit.MovementSpeedScale, Is.EqualTo(1.1f).Within(0.0001f));

            kit.Tick(null, 1f);

            Assert.That(kit.Skill1.CooldownRemaining, Is.Zero);
            Assert.That(kit.MovementSpeedScale, Is.EqualTo(1f));
        }

        [Test]
        public void RoundResetRemovesTheBonus()
        {
            var kit = new NemuHeroKit();
            kit.AttackingSkill.ApplyNetworkSnapshot(2f, 0);
            Assert.That(kit.MovementSpeedScale, Is.EqualTo(1.1f).Within(0.0001f));

            kit.ResetForRound(null);

            Assert.That(kit.MovementSpeedScale, Is.EqualTo(1f));
        }

        [Test]
        public void AuthoritativeCooldownCorrectionRemovesTheBonus()
        {
            var kit = new NemuHeroKit();
            kit.DefendingSkill.ApplyNetworkSnapshot(2f, 0);
            Assert.That(kit.MovementSpeedScale, Is.EqualTo(1.1f).Within(0.0001f));

            kit.DefendingSkill.ApplyNetworkSnapshot(0f, 0);

            Assert.That(kit.MovementSpeedScale, Is.EqualTo(1f));
        }
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void AcceptedBasicCastStartsAllThreeCooldownsAtTwentyFiveSeconds(int abilityIndex)
        {
            var go = new GameObject("Nemu accepted cooldown contract");
            var kit = new NemuHeroKit();
            var context = new AbilityContext(go.AddComponent<CharacterMotor>(), null, null);
            try
            {
                var ability = abilityIndex == 0 ? kit.Skill1
                    : abilityIndex == 1 ? kit.AttackingSkill : kit.DefendingSkill;
                float banked = kit.UltimateCharge;
                // Accepted observer playback calls Activate directly. This must
                // share clocks too, without relying only on local input routing.
                using (NetCue.SuppressRelay()) ability.Activate(context);

                Assert.That(kit.Skill1.CooldownRemaining, Is.EqualTo(25f));
                Assert.That(kit.AttackingSkill.CooldownRemaining, Is.EqualTo(25f));
                Assert.That(kit.DefendingSkill.CooldownRemaining, Is.EqualTo(25f));
                Assert.That(kit.Ultimate.CooldownRemaining, Is.Zero);
                Assert.That(kit.UltimateCharge, Is.EqualTo(banked));
            }
            finally
            {
                kit.ResetForRound(context);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SharedBasicCooldownsTickTogether()
        {
            var go = new GameObject("Nemu cooldown ticking contract");
            var kit = new NemuHeroKit();
            var context = new AbilityContext(go.AddComponent<CharacterMotor>(), null, null);
            try
            {
                using (NetCue.SuppressRelay()) kit.Skill1.Activate(context);
                kit.Tick(context, 1f);

                Assert.That(kit.Skill1.CooldownRemaining, Is.EqualTo(24f).Within(0.001f));
                Assert.That(kit.AttackingSkill.CooldownRemaining, Is.EqualTo(24f).Within(0.001f));
                Assert.That(kit.DefendingSkill.CooldownRemaining, Is.EqualTo(24f).Within(0.001f));
            }
            finally
            {
                kit.ResetForRound(context);
                Object.DestroyImmediate(go);
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

        private static void AssertBasicClocks(HeroKit kit, float value)
        {
            Assert.That(kit.Skill1.CooldownRemaining, Is.EqualTo(value));
            Assert.That(kit.AttackingSkill.CooldownRemaining, Is.EqualTo(value));
            Assert.That(kit.DefendingSkill.CooldownRemaining, Is.EqualTo(value));
        }

        [TestCase(false, 0f)]
        [TestCase(true, 4f)]
        public void LatestHostReceiptCorrectsTheEntireSharedGroup(bool accepted, float remaining)
        {
            var previous = NetAuthority.Provider;
            var go = new GameObject("Nemu resource receipt");
            try
            {
                NetAuthority.Provider = new PredictingOwner();
                var system = ReceiptSystem(go);
                SetBasicClocks(system.Kit, 25f);
                Assert.That(system.TrackSkillRequest(0, 101), Is.True);

                Assert.That(system.ResolveSkillReceipt(0, 101, accepted, remaining, 0), Is.True);

                AssertBasicClocks(system.Kit, remaining);
                Assert.That(system.ResolveSkillReceipt(0, 101, accepted, remaining, 0), Is.False);
            }
            finally { Object.DestroyImmediate(go); NetAuthority.Provider = previous; }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OlderOtherSlotReceiptCannotOverwriteALaterRequest(bool laterAlreadyAccepted)
        {
            var previous = NetAuthority.Provider;
            var go = new GameObject("Nemu reordered resource receipt");
            try
            {
                NetAuthority.Provider = new PredictingOwner();
                var system = ReceiptSystem(go);
                SetBasicClocks(system.Kit, 25f);
                Assert.That(system.TrackSkillRequest(0, 201), Is.True);
                Assert.That(system.TrackSkillRequest(1, 202), Is.True);
                if (laterAlreadyAccepted)
                    Assert.That(system.ResolveSkillReceipt(1, 202, true, 12f, 0), Is.True);

                Assert.That(system.ResolveSkillReceipt(0, 201, false, 0f, 0), Is.True);

                AssertBasicClocks(system.Kit, laterAlreadyAccepted ? 12f : 25f);
            }
            finally { Object.DestroyImmediate(go); NetAuthority.Provider = previous; }
        }

        [Test]
        public void IndependentKitReceiptStillCorrectsOnlyItsOwnAbility()
        {
            var previous = NetAuthority.Provider;
            var go = new GameObject("Independent resource receipt");
            try
            {
                NetAuthority.Provider = new PredictingOwner();
                var system = ReceiptSystem(go, "zack");
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
