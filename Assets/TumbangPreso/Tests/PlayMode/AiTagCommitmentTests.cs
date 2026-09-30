using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class AiTagCommitmentTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        private static void Call(object owner, string method, params object[] args)
            => owner.GetType().GetMethod(method, Hidden).Invoke(owner, args);

        [TestCase(false), TestCase(true)]
        public void NearTargetUsesOneTagCommitment(bool alreadyCharging)
        {
            UI.SceneFlow.Networked = false;
            UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameServices.Ensure(); GameServices.Round.Clear();
            GameServices.Round.Lata = new GameObject("Bot tag can").AddComponent<Lata>();
            GameServices.Round.Lata.transform.position = new Vector3(5, 0, 5);
            CharacterMotor Seat(int slot, Vector3 position)
            {
                var go = new GameObject("Bot tag seat " + slot, typeof(CharacterController));
                var motor = go.AddComponent<CharacterMotor>(); motor.enabled = false;
                motor.PlayerSlot = slot; motor.Mode = GameMode.Classic; motor.SpawnPosition = position;
                go.AddComponent<Carrier>(); go.AddComponent<CombatVerbs>().enabled = false;
                go.transform.position = position; go.transform.forward = Vector3.forward;
                GameServices.Round.Register(motor); return motor;
            }
            var actor = Seat(0, new Vector3(0, 1, -3));
            var victim = Seat(1, new Vector3(0, 1, -2));
            GameServices.Match.StartMatch(); GameServices.Round.BeginRound();
            actor.IsDefender = true; victim.IsDefender = false; victim.HoldingSlipper = true;
            actor.transform.position = new Vector3(0, 1, -3);
            victim.transform.position = new Vector3(0, 1, -2);
            Assert.IsTrue(actor.CanAct()); Assert.IsTrue(victim.IsTaggable());
            var ai = actor.gameObject.AddComponent<AIController>(); ai.enabled = false;
            typeof(AIController).GetProperty("Plan").SetValue(ai, AiPlan.Hunt);
            var verbs = actor.GetComponent<CombatVerbs>();
            if (alreadyCharging)
            {
                // Seed an already committed charge through the real input consumer.
                // The private planner clock mirrors that same held duration.
                actor.Intent.Set(Verb.Lunge, true);
                Call(verbs, "StepLunge", .2f);
                typeof(AIController).GetField("_lungeHeld", Hidden).SetValue(ai, .2f);
                actor.Intent.CommitFrame();
                Assert.Greater(verbs.ObservedLungeCharge, 0);
            }
            Call(ai, "Act", actor.Intent, AiTuning.LungeHoldTime);
            Call(verbs, "StepPunch");
            Call(verbs, "StepLunge", .016f);
            int spent = (verbs.PunchCooldownLeft > 0 ? 1 : 0) + (verbs.LungeCooldownLeft > 0 ? 1 : 0);
            Assert.AreEqual(1, spent, "A close target must not trigger a punch plus an accidental charged dash.");
            if (alreadyCharging) Assert.AreEqual(0, verbs.PunchCooldownLeft, "Finish the lunge already being held.");
            else Assert.AreEqual(0, verbs.LungeCooldownLeft, "A fresh close target should get the immediate punch.");
        }
    }
}
