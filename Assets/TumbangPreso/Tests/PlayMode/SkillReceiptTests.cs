using System.Collections;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class SkillReceiptTests
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
        [UnityTest]
        public IEnumerator RefusedFreeRecallDoesNotCreateAChargeAndEligibilityDoesNotMutateHeldTime()
        {
            yield return MapRetrievalProbe.Load("Eskinita",GameMode.HeroStrike);
            var actor=GameServices.Round.PlayerAt(1);actor.AbilitySystem.BindHero("nemu");yield return null;
            var system=actor.AbilitySystem;var kit=system.Kit;
            var context=new AbilityContext(actor,actor.GetComponent<Carrier>(),actor.GetComponent<CombatVerbs>());
            Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(context));Assert.AreEqual(0,kit.Skill2.ChargesRemaining);
            Assert.IsTrue(kit.Skill2.IsActive);Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(context));
            kit.Skill2.RollBackPredictedCast(context,refundResources:false);
            Assert.AreEqual(0,kit.Skill2.ChargesRemaining,"A free reactivation has no charge to refund");
            float heldBefore=kit.Skill1.HeldSecondsOnCast;
            Assert.AreEqual(HeroKit.CastOutcome.Cast,system.CheckNetworkSkill(HeroAbilitySystem.Slot.Skill1,actor.transform.position,actor.transform.forward,actor.transform.position+Vector3.forward*3,1.1f));
            Assert.AreEqual(heldBefore,kit.Skill1.HeldSecondsOnCast);Assert.AreEqual(0,kit.Skill1.CooldownRemaining);
            Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill1(context));
            float cooldown=kit.Skill1.CooldownRemaining;
            Assert.AreEqual(HeroKit.CastOutcome.Cooling,system.CheckNetworkSkill(HeroAbilitySystem.Slot.Skill1,actor.transform.position,actor.transform.forward,actor.transform.position+Vector3.forward*3,2));
            Assert.AreEqual(heldBefore,kit.Skill1.HeldSecondsOnCast);Assert.AreEqual(cooldown,kit.Skill1.CooldownRemaining);
            system.TrackSkillRequest(0,1);system.TrackSkillRequest(0,2);
            Assert.IsFalse(system.PendingSkillReceipt(0,1));Assert.IsTrue(system.PendingSkillReceipt(0,2));
            system.ResetKit();Assert.IsFalse(system.PendingSkillReceipt(0,2),"Round resets retire outstanding predictions");
        }
    }
}
