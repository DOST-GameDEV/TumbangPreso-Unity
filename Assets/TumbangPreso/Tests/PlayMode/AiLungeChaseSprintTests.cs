using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class AiLungeChaseSprintTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private Difficulty _difficulty;
        private bool _edgeSense;
        private CustomRules _rules;
        private bool _rulesPinned;

        [UnitySetUp] public IEnumerator Before()
        {
            _difficulty = AIController.ActiveDifficulty; _edgeSense = AIController.EdgeSense;
            _rules = UI.SceneFlow.SelectedRules.Clone(); _rulesPinned = UI.SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
            AIController.ActiveDifficulty = Difficulty.Normal; AIController.EdgeSense = false;
        }

        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            AIController.ActiveDifficulty = _difficulty; AIController.EdgeSense = _edgeSense;
            UI.SceneFlow.AdoptRemoteRules(_rules);
            if (_rulesPinned) UI.SceneFlow.PinSelectedRules(_rules); else UI.SceneFlow.UnpinSelectedRules();
        }

        private static object Call(object owner, string method, params object[] args)
            => owner.GetType().GetMethod(method, Hidden).Invoke(owner, args);
        private static void Write(object owner, string field, object value)
            => owner.GetType().GetField(field, Hidden).SetValue(owner, value);

        private static (CharacterMotor actor, CharacterMotor victim, AIController brain, CombatVerbs verbs) Chase(float held = 1.2f)
        {
            UI.SceneFlow.Networked = false;
            UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameServices.Ensure(); GameServices.Round.Clear();
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = Vector3.down * .1f; floor.transform.localScale = new Vector3(30, .2f, 30);
            var can = new GameObject("Chase sprint can").AddComponent<Lata>(); can.enabled = false;
            can.transform.position = new Vector3(5, 0, 5); GameServices.Round.Lata = can;
            CharacterMotor Seat(int slot, Vector3 at)
            {
                var go = new GameObject("Chase sprint seat" + slot, typeof(CharacterController));
                var cc = go.GetComponent<CharacterController>(); cc.height = 1.6f; cc.radius = .35f; cc.center = Vector3.up * .8f;
                var motor = go.AddComponent<CharacterMotor>(); motor.enabled = false; motor.PlayerSlot = slot;
                motor.IsBot = true; motor.Mode = GameMode.Classic; motor.SpawnPosition = at;
                go.AddComponent<Carrier>().enabled = false; go.AddComponent<CombatVerbs>().enabled = false;
                GameServices.Round.Register(motor); return motor;
            }
            var actor = Seat(0, new Vector3(0, .1f, -6));
            var victim = Seat(1, new Vector3(0, .1f, .6f));
            GameServices.Match.StartMatch(); GameServices.Round.BeginRound();
            actor.IsDefender = true; victim.IsDefender = false; victim.HoldingSlipper = true;
            actor.transform.position = actor.SpawnPosition; victim.transform.position = victim.SpawnPosition;
            actor.transform.forward = victim.transform.forward = Vector3.forward;
            actor.Intent.Parked = victim.Intent.Parked = false; victim.Intent.Move = Vector2.up; victim.Intent.Set(Verb.Sprint, true);
            Write(actor, "_spawnSettle", 0); Write(victim, "_spawnSettle", 0);
            Write(actor, "_velocity", Vector3.forward * Balance.DefenderWalkSpeed);
            Write(victim, "_velocity", Vector3.forward * Balance.AttackerRunSpeed);
            actor.Stamina.RefillAndClearFatigue(); victim.Stamina.RefillAndClearFatigue();
            var brain = actor.gameObject.AddComponent<AIController>(); brain.enabled = false;
            typeof(AIController).GetProperty("Plan").SetValue(brain, AiPlan.Hunt);
            Write(brain, "_sprintBurstLeft", .8f); Write(brain, "_sprintWantHeld", .2f); Write(brain, "_sprintAsked", true);
            var verbs = actor.GetComponent<CombatVerbs>();
            actor.Intent.Set(Verb.Lunge, true); Call(verbs, "StepLunge", held);
            Write(brain, "_lungeHeld", held); actor.Intent.CommitFrame(); Physics.SyncTransforms();
            Call(brain, "Observe", Time.fixedDeltaTime);
            Assert.IsTrue(actor.CanAct()); Assert.IsTrue(victim.IsTaggable());
            Assert.IsFalse((bool)Call(brain, "LungeCanReach", victim, 1f), "The held chase must start outside actual dash contact.");
            return (actor, victim, brain, verbs);
        }

        private static void Tick((CharacterMotor actor, CharacterMotor victim, AIController brain, CombatVerbs verbs) x)
        {
            float dt = Time.fixedDeltaTime;
            Call(x.brain, "Observe", dt); Call(x.brain, "StepSprintKey", dt);
            Call(x.brain, "Act", x.actor.Intent, dt); Call(x.verbs, "StepLunge", dt);
            // Both bodies consume ordinary intent through their actual motor and
            // CharacterController. Never impose an equal displacement on the pair.
            Call(x.actor, "FixedUpdate"); Call(x.victim, "FixedUpdate"); Physics.SyncTransforms();
        }

        [TestCase(.6f), TestCase(1.2f)]
        public void HeldChaseKeepsItsLegalSprintAndClosesOnRunningTarget(float held)
        {
            var x = Chase(held); float gap = x.victim.transform.position.z - x.actor.transform.position.z;
            for (int tick = 0; tick < 4; tick++) Tick(x);
            Debug.Log($"[HeldChaseSprint] held={held:F2} gapBefore={gap:F4} gapAfter={x.victim.transform.position.z - x.actor.transform.position.z:F4} defenderSpeed={x.actor.Velocity.z:F2} attackerSpeed={x.victim.Velocity.z:F2} sprint={x.actor.Intent.Pressed(Verb.Sprint)} cooldown={x.verbs.LungeCooldownLeft:F3}");
            Assert.Zero(x.verbs.LungeCooldownLeft, "The distant target must not spend an unreachable dash.");
            Assert.IsTrue(x.actor.Intent.Pressed(Verb.Lunge));
            Assert.IsTrue(x.actor.Intent.Pressed(Verb.Sprint), "Long-held aim correction overwrote the legal chase sprint.");
            Assert.IsTrue(x.actor.Stamina.IsSprinting, "The normal motor must consume sprint, not a test-imposed speed.");
            Assert.Less(x.victim.transform.position.z - x.actor.transform.position.z, gap,
                "A rested sprinting defender should close on an ordinary sprinting attacker.");
        }

        [Test]
        public void LongHeldChaseCanBuildTheOrdinarySprintCommitment()
        {
            var x = Chase(); Write(x.brain, "_sprintBurstLeft", 0f); Write(x.brain, "_sprintWantHeld", 0f); Write(x.brain, "_sprintAsked", false);
            Tick(x); Assert.IsFalse(x.actor.Intent.Pressed(Verb.Sprint), "The first frame must still pay the normal sprint commitment.");
            for (int tick = 0; tick < 11; tick++) Tick(x);
            Assert.Zero(x.verbs.LungeCooldownLeft);
            Assert.IsTrue(x.actor.Intent.Pressed(Verb.Sprint), "The second Drive cleared sprint demand every frame and prevented commitment.");
        }

        [TestCase(10f, 0f), TestCase(100f, .5f)]
        public void LongHeldChaseRespectsStaminaReserveAndFatigue(float stamina, float fatigue)
        {
            var x = Chase(); x.actor.Stamina.ApplyNetworkSnapshot(stamina, 0, fatigue);
            Tick(x);
            Assert.IsFalse(x.actor.Intent.Pressed(Verb.Sprint)); Assert.IsFalse(x.actor.Stamina.IsSprinting);
            Assert.IsTrue(x.actor.Intent.Pressed(Verb.Lunge)); Assert.Zero(x.verbs.LungeCooldownLeft);
        }

        [Test]
        public void LongHeldChaseRespectsTheSprintRestBeat()
        {
            var x = Chase(); Write(x.brain, "_sprintBurstLeft", 0f); Write(x.brain, "_sprintRestLeft", .5f);
            Tick(x);
            Assert.IsFalse(x.actor.Intent.Pressed(Verb.Sprint)); Assert.IsTrue(x.actor.Intent.Pressed(Verb.Lunge));
            Assert.Zero(x.verbs.LungeCooldownLeft);
        }

        [Test]
        public void CooldownKeepsOrdinaryChaseWithoutStartingAnotherCharge()
        {
            var x = Chase(); Write(x.verbs, "_lungeCooldown", .8f); Write(x.verbs, "_lungeCharging", false); Write(x.verbs, "_lungeCharge", 0f);
            Tick(x);
            Assert.IsTrue(x.actor.Intent.Pressed(Verb.Sprint)); Assert.IsFalse(x.actor.Intent.Pressed(Verb.Lunge));
            Assert.AreEqual(.8f, x.verbs.LungeCooldownLeft); Assert.Less(x.verbs.ObservedLungeCharge, 0);
        }
    }
}
