using NUnit.Framework;
using TumbangPreso.Abilities;

namespace TumbangPreso.PlayTests
{
    public sealed class AmihanWikiTests
    {
        [Test] public void DriftUsesCurrentNameAndCooldown()
        {
            var kit=new AmihanHeroKit();
            Assert.AreEqual("DRIFT",kit.Skill1.Name);
            Assert.AreEqual(35,kit.Skill1.Cooldown);
            Assert.AreEqual(40,kit.AttackingSkill.Cooldown);
            Assert.AreEqual(5,kit.AttackingSkill.Duration);
            Assert.AreEqual(35,kit.DefendingSkill.Cooldown);
            Assert.AreEqual("AIRBURST",kit.Ultimate.Name);
        }
        [Test] public void AcceptedCastsRefreshSecondWindWithoutStacking()
        {
            var kit=new AmihanHeroKit(); long sequence=1;
            foreach(var ability in new[]{kit.Skill1,kit.AttackingSkill,kit.DefendingSkill,kit.Ultimate})
            {
                ability.AdoptAcceptedCastEvent(sequence++,false);
                Assert.AreEqual(1.25f,kit.MovementSpeedScale,"Each accepted ability grants Second Wind.");
                kit.Tick(null,2.4f); Assert.AreEqual(1.25f,kit.MovementSpeedScale);
            }
            kit.Tick(null,.11f); Assert.AreEqual(1,kit.MovementSpeedScale);
            kit.Skill1.AdoptAcceptedCastEvent(sequence++,false);
            kit.ResetForRound(null); Assert.AreEqual(1,kit.MovementSpeedScale);
        }
        [Test] public void RecoveryRejectsInvalidAndOlderPassiveState()
        {
            var kit=new AmihanHeroKit();
            Assert.IsTrue(kit.RequiresOwnerCastEvents);
            kit.Skill1.AdoptAcceptedCastEvent(10,false); kit.Tick(null,1);
            kit.Skill1.AdoptAcceptedCastEvent(10,false); Assert.AreEqual(1.5f,kit.SecondWindRemaining);
            Assert.IsFalse(kit.RestoreSecondWind(2.5f,9));
            Assert.IsFalse(kit.RestoreSecondWind(float.NaN,11));
            Assert.IsFalse(kit.RestoreSecondWind(3,11));
            Assert.IsTrue(kit.RestoreSecondWind(.5f,11));
            Assert.AreEqual(1.25f,kit.MovementSpeedScale); kit.Tick(null,.51f);
            Assert.AreEqual(1,kit.MovementSpeedScale);
            kit.PracticeMode=true; Assert.IsFalse(kit.TryActivateSkill1(null));
            Assert.AreEqual(1,kit.MovementSpeedScale);
        }
    }
}
