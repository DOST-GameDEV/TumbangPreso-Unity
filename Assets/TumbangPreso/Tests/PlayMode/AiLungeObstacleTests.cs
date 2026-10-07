using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class AiLungeObstacleTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        private static void Call(object owner, string method, params object[] args)
            => owner.GetType().GetMethod(method, Hidden).Invoke(owner, args);

        [Test]
        public void LegacyBareCapsuleIntersectsFloorButInstalledGameCapsuleClearsIt()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.layer = 8;
            floor.transform.position = Vector3.down * .1f; floor.transform.localScale = new Vector3(30, .2f, 30);
            Physics.SyncTransforms(); var feet = new Vector3(0, .1f, -3);
            // Bare CharacterController: height2, radius.5, centre0. The fixture's
            // claimed feet pose places its capsule through the floor.
            Assert.IsTrue(Physics.CheckCapsule(feet - Vector3.up * .5f,
                feet + Vector3.up * .5f, .5f, 1 << 8, QueryTriggerInteraction.Ignore));
            // MatchInstaller: height1.6, radius.35, centre.8 above the actual feet.
            Assert.IsFalse(Physics.CheckCapsule(feet + Vector3.up * .35f,
                feet + Vector3.up * 1.25f, .35f, 1 << 8, QueryTriggerInteraction.Ignore));
        }

        [TestCase(1.2f, false, 0f, 4f, 3f), TestCase(5f, true, 0f, 4f, 3f), TestCase(-2f, true, 0f, 4f, 3f)]
        [TestCase(1.2f, false, .38f, .2f, 3f), TestCase(1.2f, true, 0f, 4f, .15f)]
        public void ChargeReleaseRequiresTravelBeforeAnObstruction(float wallDistance, bool reachable,
            float wallSide, float wallWidth, float wallHeight)
        {
            UI.SceneFlow.Networked = false; UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameServices.Ensure(); GameServices.Round.Clear();
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = Vector3.down * .1f; floor.transform.localScale = new Vector3(30, .2f, 30);
            GameServices.Round.Lata = new GameObject("Obstacle can").AddComponent<Lata>();
            GameServices.Round.Lata.transform.position = new Vector3(5, 0, 5);
            CharacterMotor Seat(int slot, Vector3 at)
            {
                var go = new GameObject("Obstacle seat" + slot, typeof(CharacterController));
                var cc = go.GetComponent<CharacterController>(); cc.height = 1.6f; cc.radius = .35f; cc.center = Vector3.up * .8f;
                var motor = go.AddComponent<CharacterMotor>(); motor.enabled = false; motor.PlayerSlot = slot;
                motor.Mode = GameMode.Classic; motor.SpawnPosition = at;
                go.AddComponent<Carrier>().enabled = false; go.AddComponent<CombatVerbs>().enabled = false;
                GameServices.Round.Register(motor); return motor;
            }
            var actor = Seat(0, new Vector3(0, .1f, -3)); var victim = Seat(1, new Vector3(0, .1f, .5f));
            GameServices.Match.StartMatch(); GameServices.Round.BeginRound();
            actor.IsDefender = true; victim.IsDefender = false; victim.HoldingSlipper = true;
            actor.transform.position = actor.SpawnPosition; victim.transform.position = victim.SpawnPosition;
            actor.transform.forward = Vector3.forward;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "Actual dash obstruction";
            wall.transform.position = new Vector3(actor.transform.position.x + wallSide, wallHeight * .5f,
                actor.transform.position.z + wallDistance);
            wall.transform.localScale = new Vector3(wallWidth, wallHeight, .2f); Physics.SyncTransforms();
            var brain = actor.gameObject.AddComponent<AIController>(); brain.enabled = false;
            typeof(AIController).GetProperty("Plan").SetValue(brain, AiPlan.Hunt);
            var verbs = actor.GetComponent<CombatVerbs>(); actor.Intent.Set(Verb.Lunge, true); Call(verbs, "StepLunge", 1f);
            typeof(AIController).GetField("_lungeHeld", Hidden).SetValue(brain, 1f); actor.Intent.CommitFrame();
            Assert.IsTrue(victim.IsTaggable());
            Call(brain, "StepLungeIntent", actor.Intent, victim, .016f); Call(verbs, "StepLunge", .016f);
            if (reachable) Assert.Greater(verbs.LungeCooldownLeft, 0, "Walls behind the victim or behind the defender cannot forbid an otherwise reachable release.");
            else
            {
                Assert.Zero(verbs.LungeCooldownLeft, "A capsule-blocking wall makes the predicted contact unreachable; do not spend a dash against it.");
                Assert.Greater(verbs.ObservedLungeCharge, 0);
            }
        }
    }
}
