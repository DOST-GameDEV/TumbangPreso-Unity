using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class PracticeBotHopRestoreLifetimeTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly PracticeProducerResetLifetimeTests _world = new();
        private CharacterMotor _bot;
        private AIController _brain;
        private PracticeRange _range;
        private Random.State _random;

        [UnitySetUp] public IEnumerator Before()
        {
            _random = Random.state;
            yield return _world.Before();
            _bot = GameObject.Find("Practice active producer bot").GetComponent<CharacterMotor>();
            _brain = _bot.GetComponent<AIController>(); _range = PracticeRange.Instance;
            Assert.IsTrue(_range.CanEdit); Assert.IsTrue(_brain.AbilitiesEnabled);
            // Public snapshots keep this bot the defender before and after restoration.
            GameServices.Match.ApplySnapshot(new int[Balance.PlayerCount], 1, true);
            GameServices.Round.ApplySnapshot(80, true, _bot.PlayerSlot, true);
            Assert.IsTrue(_bot.IsDefender);
            // Supply a feet-based capsule and clear floor, so spawn placement cannot
            // create an upward depenetration that would masquerade as a jump.
            var capsule = _bot.GetComponent<CharacterController>();
            capsule.center = Vector3.up * (capsule.height * .5f);
            Assert.IsTrue(_range.SetBot(_bot.PlayerSlot, true, true));
            _bot.transform.position = new Vector3(2, .1f, 2);
            Physics.SyncTransforms();
            for (int step = 0; step < 40; step++) MotorStep();
            Assert.IsTrue(_bot.IsGrounded, "Shipping motor steps did not establish a real floor contact.");
            Assert.LessOrEqual(_bot.Velocity.y, 0); Assert.IsFalse(_bot.Intent.JustPressed(Verb.Jump));
            Assert.IsTrue(_range.SetBot(_bot.PlayerSlot, true, false));
            Assert.IsTrue(_brain.enabled); Assert.IsTrue(_bot.CanAct());
            Assert.Greater(Time.fixedDeltaTime, 0);
            Assert.Less(Time.deltaTime * (Balance.SpawnSettleFrames + 1), AiTuning.HopIntervalMin,
                "The manually stepped update interval could generate a new hop during spawn settling.");
            Random.InitState(51003);
        }
        [UnityTearDown] public IEnumerator After()
        {
            try { yield return _world.After(); }
            finally { Random.state = _random; }
        }
        private void MotorStep() => typeof(CharacterMotor).GetMethod("FixedUpdate", Hidden).Invoke(_bot, null);
        private void BrainStep() => typeof(AIController).GetMethod("Update", Hidden).Invoke(_brain, null);
        private void Hop()
        {
            Assert.IsTrue(_bot.IsGrounded); Assert.IsTrue(_bot.CanMove());
            Assert.IsTrue((bool)typeof(AIController).GetMethod("MayHop", Hidden).Invoke(_brain, null));
            // Advance the shipping producer with bounded, finite positive intervals.
            // Neither its private cadence/held fields nor raw intent are seeded.
            for (int attempt = 0; attempt < 128 && !_bot.Intent.JustPressed(Verb.Jump); attempt++)
                typeof(AIController).GetMethod("StepHop", Hidden).Invoke(_brain,
                    new object[] { _bot.Intent, AiTuning.HopIntervalMax + 1f });
            Assert.IsTrue(_bot.Intent.Pressed(Verb.Jump), "The real hop producer never published a held jump.");
            Assert.IsTrue(_bot.Intent.JustPressed(Verb.Jump), "The real hop producer never buffered a jump edge.");
        }
        private void RemoveAndRestore()
        {
            Assert.IsTrue(_range.SetBot(_bot.PlayerSlot, false, false));
            Assert.IsFalse(_bot.gameObject.activeSelf); Assert.IsFalse(_brain.enabled);
            Assert.IsTrue(_range.SetBot(_bot.PlayerSlot, true, false));
            Assert.IsTrue(_bot.gameObject.activeSelf); Assert.IsTrue(_brain.enabled);
            Assert.IsFalse(_bot.Intent.Parked); Assert.IsTrue(_bot.IsGrounded);
            Physics.SyncTransforms();
            // Manually step shipping callbacks in legal FixedUpdate-before-Update order.
            // The full brain Update gets each settling interval; it is not starved.
            for (int step = 0; step < Balance.SpawnSettleFrames; step++)
            {
                Vector3 before = _bot.transform.position; MotorStep();
                Assert.AreEqual(before, _bot.transform.position, "Expected the shipping spawn-settle early return.");
                BrainStep();
            }
            Assert.IsTrue(_bot.CanMove()); Assert.IsTrue(_bot.IsGrounded);
        }
        private void AssertJump()
        {
            Vector3 before = _bot.transform.position; MotorStep();
            Assert.Greater(_bot.Velocity.y, 0, "A real fresh jump failed to impart upward velocity.");
            Assert.Greater(_bot.transform.position.y - before.y, .0001f,
                "The jump control did not physically rise on the supplied clear floor.");
        }
        [Test] public void PublicRemovalRestoreDoesNotReviveTheOldHopEdge()
        {
            Hop(); RemoveAndRestore();
            Assert.IsFalse(_bot.Intent.JustPressed(Verb.Jump),
                "The producer's buffered jump survived public removal and restoration.");
        }
        [Test] public void FirstNonSettlingStepDoesNotJumpFromTheRemovedProducer()
        {
            Hop(); RemoveAndRestore();
            Vector3 before = _bot.transform.position; MotorStep();
            Assert.LessOrEqual(_bot.Velocity.y, 0, "The removed producer's hop launched after spawn settling.");
            Assert.Less(_bot.transform.position.y - before.y, .0001f,
                "The first non-settling shipping motor step physically rose from an obsolete hop.");
        }
        [Test] public void AnUninterruptedRealProducerHopStillPhysicallyJumps()
        {
            Hop(); AssertJump();
        }
        [Test] public void RestorationWithoutAnOldHopDoesNotPhysicallyJump()
        {
            RemoveAndRestore();
            Assert.IsFalse(_bot.Intent.JustPressed(Verb.Jump));
            Vector3 before = _bot.transform.position; MotorStep();
            Assert.LessOrEqual(_bot.Velocity.y, 0);
            Assert.Less(_bot.transform.position.y - before.y, .0001f);
        }
        [Test] public void AFreshProducerHopStillPhysicallyJumpsAtTheRestoredPosition()
        {
            RemoveAndRestore();
            // A normal motor commit establishes the restored floor before the new hop.
            MotorStep(); Assert.IsTrue(_bot.IsGrounded);
            Hop(); AssertJump();
        }
    }
}
