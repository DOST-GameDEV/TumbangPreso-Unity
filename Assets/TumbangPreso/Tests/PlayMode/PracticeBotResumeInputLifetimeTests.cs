using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class PracticeBotResumeInputLifetimeTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly PracticeProducerResetLifetimeTests _world = new();
        private CharacterMotor _bot;
        private AIController _brain;
        private PracticeRange _range;
        [UnitySetUp] public IEnumerator Before()
        {
            yield return _world.Before();
            _bot = GameObject.Find("Practice active producer bot").GetComponent<CharacterMotor>();
            _brain = _bot.GetComponent<AIController>(); _range = PracticeRange.Instance;
            Assert.IsTrue(_range.CanEdit); Assert.IsTrue(_brain.enabled); Assert.IsFalse(_bot.Intent.Parked);
            // Real disable/re-enable resets producer cadence; raw intent is generated below.
            _brain.enabled = false; _brain.enabled = true;
            // Supplied clear floor placement, away from the can's collider.
            _bot.transform.position = new Vector3(2, .1f, 2);
            Physics.SyncTransforms();
        }
        [UnityTearDown] public IEnumerator After() => _world.After();
        private void Drive()
        {
            typeof(AIController).GetMethod("Drive", Hidden).Invoke(_brain,
                new object[] { _bot.Intent, Vector3.forward, false, false });
            Assert.Greater(_bot.Intent.MoveAxis.sqrMagnitude, .5f, "The shipping producer did not publish a real move command.");
        }
        private void IdleAndResume()
        {
            Assert.IsTrue(_range.SetBot(_bot.PlayerSlot, true, true));
            Assert.IsFalse(_brain.enabled); Assert.IsTrue(_bot.Intent.Parked); Assert.AreEqual(Vector2.zero, _bot.Intent.MoveAxis);
            Assert.IsTrue(_range.SetBot(_bot.PlayerSlot, true, false));
            Assert.IsTrue(_brain.enabled); Assert.IsFalse(_bot.Intent.Parked); Assert.IsTrue(_bot.CanAct());
        }
        private void MotorStep() => typeof(CharacterMotor).GetMethod("FixedUpdate", Hidden).Invoke(_bot, null);
        private float PlanarSpeed()
        {
            var velocity = _bot.Velocity; velocity.y = 0; return velocity.magnitude;
        }
        [Test] public void PublicIdleResumeDoesNotRestoreTheOldProducerMove()
        {
            Drive(); IdleAndResume();
            Assert.AreEqual(Vector2.zero, _bot.Intent.MoveAxis, "Unparking restored the producer's old raw move without a new command.");
        }
        [Test] public void FirstResumedMotorStepDoesNotSteerFromTheOldCommand()
        {
            Drive(); Assert.IsTrue(_range.SetBot(_bot.PlayerSlot, true, true));
            // Manually invoke shipping fixed steps to settle momentum; no velocity field is reset.
            for (int step = 0; step < 40; step++) MotorStep();
            Assert.Less(PlanarSpeed(), .0001f); Assert.IsTrue(_bot.Intent.Parked); Assert.IsFalse(_brain.enabled);
            Assert.IsTrue(_range.SetBot(_bot.PlayerSlot, true, false));
            Assert.IsTrue(_bot.CanMove()); Assert.Greater(Time.fixedDeltaTime, 0);
            Vector3 before = _bot.transform.position; MotorStep();
            Vector3 displacement = _bot.transform.position - before; displacement.y = 0;
            Assert.Less(displacement.magnitude, .0001f,
                "The first legal FixedUpdate before AI.Update consumed a move retained across public idle.");
        }
        [Test] public void RepeatingActivePreservesTheCurrentProducerCommand()
        {
            Drive(); Vector2 move = _bot.Intent.MoveAxis;
            Assert.IsTrue(_range.SetBot(_bot.PlayerSlot, true, false));
            Assert.IsTrue(_brain.enabled); Assert.AreEqual(move, _bot.Intent.MoveAxis);
        }
        [Test] public void ANewProducerCommandStillMovesAfterIdleResume()
        {
            Drive(); IdleAndResume(); Drive();
            Assert.IsTrue(_bot.CanMove()); Assert.Greater(Time.fixedDeltaTime, 0);
            Vector3 before = _bot.transform.position;
            for (int step = 0; step < 4; step++) MotorStep();
            Vector3 displacement = _bot.transform.position - before; displacement.y = 0;
            Assert.Greater(PlanarSpeed(), 0, "A genuine fresh producer command was lost after resume.");
            Assert.Greater(displacement.magnitude, .0001f, "The fresh movement control was blocked or vacuous.");
        }
        [Test] public void ProducerWithSuppressedAbilitiesKeepsSharedHumanHeroKeys()
        {
            _brain.AbilitiesEnabled = false; Drive();
            _bot.Intent.Set(Verb.Skill1, true); _bot.Intent.Set(Verb.Skill2, true); _bot.Intent.Set(Verb.Ultimate, true);
            IdleAndResume();
            Assert.IsTrue(_bot.Intent.Pressed(Verb.Skill1)); Assert.IsTrue(_bot.Intent.Pressed(Verb.Skill2));
            Assert.IsTrue(_bot.Intent.Pressed(Verb.Ultimate)); Assert.IsFalse(_brain.AbilitiesEnabled);
        }
        [Test] public void AnAlreadyDisabledAttachedBrainDoesNotOwnHeldHumanKeys()
        {
            _brain.enabled = false;
            _bot.Intent.Set(Verb.Skill1, true); _bot.Intent.Set(Verb.Skill2, true); _bot.Intent.Set(Verb.Ultimate, true);
            Assert.IsTrue(_range.SetBot(_bot.PlayerSlot, true, true));
            Assert.IsTrue(_range.SetBot(_bot.PlayerSlot, true, false));
            Assert.IsTrue(_bot.Intent.Pressed(Verb.Skill1)); Assert.IsTrue(_bot.Intent.Pressed(Verb.Skill2));
            Assert.IsTrue(_bot.Intent.Pressed(Verb.Ultimate));
        }
    }
}
