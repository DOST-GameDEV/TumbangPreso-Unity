using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class AiLungeChargePowerTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        private static void Call(object owner, string method, params object[] args)
            => owner.GetType().GetMethod(method, Hidden).Invoke(owner, args);

        [TestCase(.45f, 4.4f, false), TestCase(.45f, 3.5f, true), TestCase(1f, 4.4f, true)]
        public void ReleaseUsesConsumerChargeAlreadyAccumulated(float charge, float distance, bool reachable)
        {
            UI.SceneFlow.Networked = false; UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameServices.Ensure(); GameServices.Round.Clear();
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = Vector3.down * .1f; floor.transform.localScale = new Vector3(30, .2f, 30);
            GameServices.Round.Lata = new GameObject("Charge power can").AddComponent<Lata>();
            GameServices.Round.Lata.transform.position = new Vector3(5, 0, 5);
            CharacterMotor Seat(int slot, Vector3 at)
            {
                var go = new GameObject("Charge power seat" + slot, typeof(CharacterController));
                var cc = go.GetComponent<CharacterController>(); cc.height = 1.6f; cc.radius = .35f; cc.center = Vector3.up * .8f;
                var motor = go.AddComponent<CharacterMotor>(); motor.enabled = false; motor.PlayerSlot = slot;
                motor.Mode = GameMode.Classic; motor.SpawnPosition = at;
                go.AddComponent<Carrier>().enabled = false; go.AddComponent<CombatVerbs>().enabled = false;
                GameServices.Round.Register(motor); return motor;
            }
            var actor = Seat(0, new Vector3(0, .1f, -3)); var victim = Seat(1, actor.SpawnPosition + Vector3.forward * distance);
            GameServices.Match.StartMatch(); GameServices.Round.BeginRound();
            actor.IsDefender = true; victim.IsDefender = false; victim.HoldingSlipper = true;
            actor.transform.position = actor.SpawnPosition; victim.transform.position = victim.SpawnPosition;
            actor.transform.forward = Vector3.forward; Physics.SyncTransforms();
            var brain = actor.gameObject.AddComponent<AIController>(); brain.enabled = false;
            typeof(AIController).GetProperty("Plan").SetValue(brain, AiPlan.Hunt);
            var verbs = actor.GetComponent<CombatVerbs>(); actor.Intent.Set(Verb.Lunge, true); Call(verbs, "StepLunge", charge);
            typeof(AIController).GetField("_lungeHeld", Hidden).SetValue(brain, charge); actor.Intent.CommitFrame();
            // At a20fps boundary, AI advances its clock before the consumer. A
            // release does not add this frame's50ms to the consumer's held charge.
            Call(brain, "StepLungeIntent", actor.Intent, victim, .05f); Call(verbs, "StepLunge", .05f);
            if (reachable) Assert.Greater(verbs.LungeCooldownLeft, 0, "Actual partial/full charge already reaches this target.");
            else
            {
                Assert.Zero(verbs.LungeCooldownLeft, "The planner used full-power travel before the consumer had accumulated full charge.");
                Assert.IsTrue(actor.Intent.Pressed(Verb.Lunge));
                // The held consumer now owns full power; ordinary release must work.
                actor.Intent.CommitFrame(); Call(brain, "StepLungeIntent", actor.Intent, victim, .016f); Call(verbs, "StepLunge", .016f);
                Assert.Greater(verbs.LungeCooldownLeft, 0);
            }
        }
    }
}
