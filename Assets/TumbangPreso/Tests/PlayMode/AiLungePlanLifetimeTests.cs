using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class AiLungePlanLifetimeTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        private static void Call(object owner, string name, params object[] args)
            => owner.GetType().GetMethod(name, Hidden).Invoke(owner, args);
        private static void Write(object owner, string name, object value)
            => owner.GetType().GetField(name, Hidden).SetValue(owner, value);
        private static (CharacterMotor actor, AIController brain, CombatVerbs verbs, Carrier carrier, Lata can) Charge(AiPlan plan)
        {
            UI.SceneFlow.Networked = false;
            UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameServices.Ensure(); GameServices.Round.Clear();
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = Vector3.down * .1f; floor.transform.localScale = new Vector3(30, .2f, 30);
            var can = new GameObject("Plan lifetime can").AddComponent<Lata>(); can.enabled = false;
            can.transform.position = new Vector3(5, 0, 5); GameServices.Round.Lata = can;
            var go = new GameObject("Plan lifetime defender", typeof(CharacterController));
            var actor = go.AddComponent<CharacterMotor>(); actor.enabled = false;
            actor.PlayerSlot = 0; actor.Mode = GameMode.Classic; actor.SpawnPosition = new Vector3(0, .1f, -3);
            var carrier = go.AddComponent<Carrier>(); carrier.enabled = false;
            var verbs = go.AddComponent<CombatVerbs>(); verbs.enabled = false;
            GameServices.Round.Register(actor); GameServices.Match.StartMatch(); GameServices.Round.BeginRound();
            actor.IsDefender = true; actor.transform.position = actor.SpawnPosition; actor.transform.forward = Vector3.forward;
            var brain = go.AddComponent<AIController>(); brain.enabled = false;
            typeof(AIController).GetProperty("Plan").SetValue(brain, plan);
            actor.Intent.Set(Verb.Lunge, true); Call(verbs, "StepLunge", .8f);
            Write(brain, "_lungeHeld", .8f); actor.Intent.CommitFrame(); Physics.SyncTransforms();
            Assert.IsTrue(actor.CanAct()); Assert.Greater(verbs.ObservedLungeCharge, 0);
            return (actor, brain, verbs, carrier, can);
        }

        [TestCase(AiPlan.Guard), TestCase(AiPlan.Intercept), TestCase(AiPlan.Reset), TestCase(AiPlan.Hunt)]
        public void TargetlessPlanDoesNotReleaseAnExistingCharge(AiPlan plan)
        {
            var x = Charge(plan);
            if (plan == AiPlan.Reset) Write(x.can, "_isUpright", false);
            Call(x.brain, "Act", x.actor.Intent, .016f); Call(x.verbs, "StepLunge", .016f);
            Assert.Zero(x.verbs.LungeCooldownLeft, "Planner fallback silently released a charged dash without a reachable target.");
            Assert.IsTrue(x.actor.Intent.Pressed(Verb.Lunge), "Keep the ordinary held input until an aimed release or real reset channel.");
            Assert.Greater(x.verbs.ObservedLungeCharge, 0);
            Assert.GreaterOrEqual((float)typeof(AIController).GetField("_lungeHeld", Hidden).GetValue(x.brain), 0);
        }

        [Test]
        public void ActualCanResetCancelsTheHoldWithoutSpendingTheDash()
        {
            var x = Charge(AiPlan.Reset); Write(x.can, "_isUpright", false);
            x.can.transform.position = x.actor.transform.position;
            Call(x.brain, "Act", x.actor.Intent, .016f);
            Assert.IsTrue(x.actor.Intent.Pressed(Verb.Grab));
            Call(x.carrier, "StepDefender", .016f); Assert.Greater(x.carrier.ChannelRatio, 0);
            Call(x.verbs, "StepLunge", .016f);
            Assert.Zero(x.verbs.LungeCooldownLeft); Assert.Less(x.verbs.ObservedLungeCharge, 0);
            x.actor.Intent.CommitFrame(); Call(x.brain, "Act", x.actor.Intent, .016f); Call(x.verbs, "StepLunge", .016f);
            Assert.IsFalse(x.actor.Intent.Pressed(Verb.Lunge), "The cancelled charge must not start again during the channel.");
            Assert.Zero(x.verbs.LungeCooldownLeft);
        }
    }
}
