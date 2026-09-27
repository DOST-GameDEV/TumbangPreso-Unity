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
        private INetProvider _provider;
        private sealed class PredictingOwner : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }
        [UnitySetUp] public IEnumerator Before()
        { _provider = NetAuthority.Provider; yield return PlayModeWorld.Reset(); }
        [UnityTearDown] public IEnumerator After()
        { yield return PlayModeWorld.Reset(); NetAuthority.Provider = _provider; }

        private static HeroAbilitySystem Owner(string hero)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0, -.25f, 0);
            floor.transform.localScale = new Vector3(40, .5f, 40);
            var owner = new GameObject("Receipt owner");
            var motor = owner.AddComponent<CharacterMotor>();
            motor.PlayerSlot = 1; motor.enabled = false;
            var system = owner.AddComponent<HeroAbilitySystem>();
            system.enabled = false; system.BindHero(hero);
            NetAuthority.Provider = new PredictingOwner();
            return system;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator EarlierConfirmedEffectsSurviveNewerRequestsWithoutDuplicateReplay()
        {
            var system = Owner("cheska");
            var motor = system.GetComponent<CharacterMotor>();
            var skill = system.Kit.Skill1;
            var first = new AbilityContext(motor, null, null, Vector3.zero, Vector3.forward, Vector3.forward * 5);
            var second = new AbilityContext(motor, null, null, Vector3.zero, Vector3.right, Vector3.right * 5);
            skill.Activate(first); Assert.IsTrue(system.TrackSkillRequest(0, 101));
            skill.Activate(second); Assert.IsTrue(system.TrackSkillRequest(0, 102));
            Assert.IsTrue(system.PendingSkillReceipt(0, 101));
            Assert.IsTrue(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill1, 101,
                first.Position, first.Forward, first.AimPoint, .55f));
            Assert.IsFalse(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill1, 101,
                first.Position, first.Forward, first.AimPoint, .55f));
            yield return null;
            Assert.AreEqual(1, Object.FindObjectsByType<HeroHazards.IceSheetComponent>(FindObjectsSortMode.None).Length);
            Assert.IsTrue(system.ResolveSkillReceipt(0, 102, false, 4, 0));
            Assert.IsFalse(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill1, 102,
                second.Position, second.Forward, second.AimPoint, .55f));

            skill.Activate(first); system.TrackSkillRequest(0, 103);
            skill.Activate(second); system.TrackSkillRequest(0, 104);
            float cooldown = skill.CooldownRemaining;
            Assert.IsTrue(system.ResolveSkillReceipt(0, 103, false, 0, 0));
            Assert.AreEqual(cooldown, skill.CooldownRemaining, "An older refusal rewrote newer resources.");
            Assert.IsTrue(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill1, 104,
                second.Position, second.Forward, second.AimPoint, .55f));
            yield return null;
            Assert.AreEqual(2, Object.FindObjectsByType<HeroHazards.IceSheetComponent>(FindObjectsSortMode.None).Length);
            skill.Activate(first); system.TrackSkillRequest(0, 105);
            system.ResetKit();
            Assert.IsFalse(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill1, 105,
                first.Position, first.Forward, first.AimPoint, .55f));
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator DeferredPlantWaitsForItsObjectAndCommandsCannotReplaceOrCancelIt()
        {
            var system = Owner("paete");
            var motor = system.GetComponent<CharacterMotor>();
            motor.transform.position = new Vector3(-10, .12f, -10);
            var pose = new AbilityContext(motor, null, null, motor.transform.position,
                Vector3.forward, new Vector3(-10, .12f, -6));
            var skill = system.Kit.Skill2;
            skill.Activate(pose); Assert.IsTrue(system.TrackSkillRequest(1, 201));
            float remaining = skill.DurationRemaining;
            skill.Tick(pose, 2);
            Assert.AreEqual(remaining, skill.DurationRemaining, "A missing unconfirmed plant ended the active skill.");
            var canPredict = typeof(HeroAbilitySystem).GetMethod("CanPredictSkill",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsFalse((bool)canPredict.Invoke(system, new object[] { HeroAbilitySystem.Slot.Skill2 }));
            Assert.IsTrue(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill2, 201,
                pose.Position, pose.Forward, pose.AimPoint, 0));
            yield return null;
            var plant = PaetePlant.OwnedBy(1);
            Assert.IsNotNull(plant);
            Assert.IsTrue((bool)canPredict.Invoke(system, new object[] { HeroAbilitySystem.Slot.Skill2 }));

            Assert.IsTrue(system.TrackSkillRequest(1, 202, reactivation: true));
            Assert.IsTrue(system.ConfirmPredictedWorldEffect(HeroAbilitySystem.Slot.Skill2, 202,
                pose.Position, pose.Forward, pose.AimPoint, 0));
            Assert.AreSame(plant, PaetePlant.OwnedBy(1));
            Assert.AreEqual(1, PaetePlant.Live.Count, "A command confirmation planted a second tree.");
            Assert.IsTrue(system.TrackSkillRequest(1, 203, reactivation: true));
            Assert.IsTrue(system.ResolveSkillReceipt(1, 203, false, 3, 0));
            Assert.IsTrue(skill.IsActive, "A denied command cancelled the earlier accepted plant skill.");
            Assert.AreSame(plant, PaetePlant.OwnedBy(1));
        }

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

        [UnityTest, Timeout(30000)]
        public IEnumerator ApprovedPlantPlaybackDoesNotRecheckReplicaReadinessOrDeferAnUnpredictedCast()
        {
            var system = Owner("paete");
            var motor = system.GetComponent<CharacterMotor>();
            var at = new Vector3(-10, .12f, -10);
            var aim = at + Vector3.forward * 4;
            motor.transform.position = at;
            var skill = system.Kit.Skill2;
            skill.Activate(new AbilityContext(motor, null, null, at, Vector3.forward, aim));
            Assert.IsNull(PaetePlant.OwnedBy(1), "Local prediction must still await acceptance.");
            system.ResetKit();

            Assert.AreEqual(HeroKit.CastOutcome.Cast, system.ApplyNetworkCast(
                HeroAbilitySystem.Slot.Skill2, at, Vector3.forward, aim, 0, authoritative: false));
            var plant = PaetePlant.OwnedBy(1);
            Assert.IsNotNull(plant, "An unpredicted approved owner cast must initialize its world effect.");
            Assert.IsTrue(skill.IsActive);
            Assert.IsFalse(plant.ShotReady);
            Assert.IsFalse(plant.Fire(aim), "An ordinary command must still respect reload.");
            Assert.AreEqual(HeroKit.CastOutcome.NotYet, system.ApplyNetworkCast(
                HeroAbilitySystem.Slot.Skill2, at, Vector3.forward, aim, 0, authoritative: true));
            Assert.AreEqual(0, Object.FindObjectsByType<PaeteWoodenSlipper>(FindObjectsSortMode.None).Length);

            Assert.AreEqual(HeroKit.CastOutcome.Cast, system.ApplyNetworkCast(
                HeroAbilitySystem.Slot.Skill2, at, Vector3.forward, aim, 0, authoritative: false));
            Assert.AreEqual(1, Object.FindObjectsByType<PaeteWoodenSlipper>(FindObjectsSortMode.None).Length,
                "Replica clock drift must not swallow a host-approved command.");
            Assert.AreSame(plant, PaetePlant.OwnedBy(1));
            Assert.AreEqual("hero-paete-command", skill.CastAction);
            Assert.IsFalse(plant.Fire(aim), "Playback must not unlock further local shots.");
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator JoinedStatusPresentationNeedsNoOriginalCastAndCleansUpPerBody()
        {
            var system = Owner("phaister");
            var first = system.GetComponent<CharacterMotor>();
            var second = new GameObject("Other joined body").AddComponent<CharacterMotor>();
            second.PlayerSlot = 2; second.enabled = false;
            // No original cast and no RoundDirector: these are the received joining timers.
            first.ApplyNetworkReworkStatuses(0, 0, 2, 2, Vector3.zero);
            second.ApplyNetworkReworkStatuses(0, 0, 2, 2, Vector3.zero);
            yield return null;
            Assert.AreEqual(2, Object.FindObjectsByType<Visual.PhaisterMoonlight>(FindObjectsSortMode.None).Length);
            Assert.IsNotNull(GameObject.Find("HexMark"));

            first.gameObject.SetActive(false);
            yield return null;
            Assert.AreEqual(1, Object.FindObjectsByType<Visual.PhaisterMoonlight>(FindObjectsSortMode.None).Length,
                "Despawn must clean only that body's presentation.");
            first.gameObject.SetActive(true);
            yield return null;
            yield return null;
            Assert.AreEqual(2, Object.FindObjectsByType<Visual.PhaisterMoonlight>(FindObjectsSortMode.None).Length);

            first.ApplyNetworkReworkStatuses(0, 0, 0, 0, Vector3.zero);
            second.ApplyNetworkReworkStatuses(0, 0, 0, 0, Vector3.zero);
            yield return null;
            yield return null;
            Assert.IsNull(GameObject.Find("HexMark"));
            foreach (var moon in Object.FindObjectsByType<Visual.PhaisterMoonlight>(FindObjectsSortMode.None))
                Assert.IsTrue(moon.Ending, "The authored end must follow received status expiry.");
            Object.Destroy(first.gameObject);
            Object.Destroy(second.gameObject);
            yield return null;
            Assert.AreEqual(0, Object.FindObjectsByType<Visual.PhaisterMoonlight>(FindObjectsSortMode.None).Length);
        }
    }
}
