using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace TumbangPreso.Tests
{
    public sealed class MotionContinuityTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _seat;
        private CharacterAnimator _driver;
        private CharacterMotor _motor;
        private GameObject _model;
        private RosterEntryAsset _entry;

        [SetUp]
        public void SetUp()
        {
            _entry = Resources.Load<RosterBook>("RosterBook").People.First(p => p.Id == "sean");
            _seat = new GameObject("Motion contract");
            _motor = _seat.AddComponent<CharacterMotor>();
            Invoke(_motor, "Awake");
            _driver = _seat.AddComponent<CharacterAnimator>();
            Invoke(_driver, "Awake");
            _model = Object.Instantiate(_entry.Model, _seat.transform);
            _driver.Bind(_model, _entry.Clips);
            var graph = Get<PlayableGraph>(_driver, "_graph");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            graph.Evaluate(0);
        }

        [TearDown] public void TearDown() { if (_seat != null) Object.DestroyImmediate(_seat); }

        [Test]
        public void RebindingRetiresThePreviousGraphAndItsAction()
        {
            var previous = Get<PlayableGraph>(_driver, "_graph");
            _driver.PlayAction("slide");
            Assert.IsTrue(_driver.IsPlayingAction);
            _driver.Bind(_model, _entry.Clips);
            Assert.IsFalse(previous.IsValid(), "The old graph still owns an animation output.");
            Assert.IsFalse(_driver.IsPlayingAction, "The previous model's action survives the swap.");
            Assert.IsTrue(Get<PlayableGraph>(_driver, "_graph").IsValid());
        }

        [Test]
        public void InterruptingACrossfadeStartsAtThePoseAlreadyOnScreen()
        {
            var graph = Get<PlayableGraph>(_driver, "_graph");
            _driver.PlayOneShot("holding-right");
            var mixer = Get<AnimationMixerPlayable>(_driver, "_mixer");
            mixer.SetInputWeight(0, .7f); mixer.SetInputWeight(1, .3f);
            Set(_driver, "_weight", .3f);
            graph.Evaluate(.04f);
            var arm = _model.GetComponentsInChildren<Transform>().First(t => t.name == "arm-right");
            Quaternion before = arm.localRotation;
            _driver.PlayOneShot("pick-up");
            graph.Evaluate(0);
            Assert.Less(Quaternion.Angle(before, arm.localRotation), .1f,
                "Discarding the partially blended outgoing clip snapped the arm.");
        }

        [Test]
        public void ACarryingWalkMovesBothLegsWithoutSwingingTheGrip()
        {
            Set(_motor, "_grounded", true);
            Set(_motor, "_velocity", new Vector3(0, 0, 2.4f));
            _motor.HoldingSlipper = true;
            Invoke(_driver, "Play", "holding-right", true, true);
            Set(_driver, "_weight", 1f);
            Invoke(_driver, "Blend");
            Set(_driver, "_gaitWeight", 1f);
            var graph = Get<PlayableGraph>(_driver, "_graph");
            var arm = _model.GetComponentsInChildren<Transform>().First(t => t.name == "arm-right");
            var leg = _model.GetComponentsInChildren<Transform>().First(t => t.name == "leg-right");
            Set(_driver, "_gaitPhase", .25f);
            Invoke(_driver, "StepGait"); graph.Evaluate(0);
            Quaternion firstLeg = leg.localRotation, grip = arm.localRotation;
            Set(_driver, "_gaitPhase", .75f);
            Invoke(_driver, "StepGait"); graph.Evaluate(0);
            Assert.Greater(Quaternion.Angle(firstLeg, leg.localRotation), 10f, "The moving carrier has frozen legs.");
            Assert.Less(Quaternion.Angle(grip, arm.localRotation), .1f, "The gait mask reaches the carrying arm.");
        }

        [TestCase(false, 2.53f, 3.795f)]
        [TestCase(true, 5.06f, 7.59f)]
        public void GaitFollowsObservedRoleSpeedRatherThanALocalSprintKey(bool defender, float walk, float sprint)
        {
            _motor.CharacterIndex = -1;
            _motor.IsDefender = defender;
            Set(_motor, "_grounded", true);
            Set(_motor, "_velocity", new Vector3(walk, 0, 0));
            Invoke(_driver, "StepGait");
            Assert.IsFalse(Get<bool>(_driver, "_running"));
            Set(_motor, "_velocity", new Vector3(sprint, 0, 0));
            Invoke(_driver, "StepGait");
            Assert.IsTrue(Get<bool>(_driver, "_running"));
        }

        [Test]
        public void RecoverySettlesEquallyAtThirtySixtyAndOneFortyFourFrames()
        {
            var arms = _seat.AddComponent<ViewmodelArms>();
            arms.EnsureBuilt();
            var pivot = Get<Transform>(arms, "_rightPivot");
            Vector3 expected = Vector3.zero;
            foreach (int fps in new[] { 30, 60, 144 })
            {
                pivot.localPosition = Vector3.one;
                for (int i = 0; i < fps; i++)
                    Invoke(arms, "StepToward", Vector3.zero, Quaternion.identity, Vector3.one, .2f / fps);
                if (fps == 30) expected = pivot.localPosition;
                else Assert.Less(Vector3.Distance(expected, pivot.localPosition), .00001f);
            }
        }

        private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
        private static object Invoke(object target, string name, params object[] args)
            => target.GetType().GetMethod(name, Private).Invoke(target, args);
    }
}
