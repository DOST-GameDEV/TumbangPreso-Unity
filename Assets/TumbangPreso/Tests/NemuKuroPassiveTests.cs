using NUnit.Framework;
using TumbangPreso.Abilities;

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
    }
}
