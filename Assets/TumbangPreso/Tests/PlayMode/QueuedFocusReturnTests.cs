using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class QueuedFocusReturnTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly CarrierDisableLifetimeTests _world = new();
        private Gamepad _pad;
        private CharacterMotor _motor;
        private PlayerInputReader _reader;
        private Carrier _carrier;
        private Slipper _shoe;
        private bool _touch;
        private InputSettings.BackgroundBehavior _background;
        private InputSettings.EditorInputBehaviorInPlayMode _editor;
        [UnitySetUp] public IEnumerator Before()
        {
            yield return _world.Before();
            _touch = TouchInput.Active; TouchInput.ReleaseAll(); TouchInput.Active = false;
            _background = InputSystem.settings.backgroundBehavior;
            _editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _pad = InputSystem.AddDevice<Gamepad>(); InputSystem.EnableDevice(_pad);
            _motor = GameObject.Find("Carrier lifetime attacker").GetComponent<CharacterMotor>();
            _carrier = _motor.GetComponent<Carrier>(); _shoe = _carrier.Held;
            _reader = _motor.gameObject.AddComponent<PlayerInputReader>();
            yield return null; Focus(true); yield return null;
            Assert.IsTrue(_reader.enabled); Assert.IsTrue(_motor.CanAct()); Assert.IsNotNull(_shoe);
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_pad != null && _pad.added) InputSystem.RemoveDevice(_pad);
            TouchInput.ReleaseAll(); TouchInput.Active = _touch;
            yield return _world.After();
            InputSystem.settings.backgroundBehavior = _background;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editor;
        }
        private void Focus(bool focused) => _motor.gameObject.SendMessage("OnApplicationFocus", focused, SendMessageOptions.DontRequireReceiver);
        private static void Step(Component component) => component.GetType().GetMethod("Update", Hidden).Invoke(component, null);
        private void Queue(float throwValue = 0, bool jump = false, Vector2 move = default)
        {
            var state = new GamepadState { rightTrigger = throwValue, leftStick = move };
            if (jump) state = state.WithButton(GamepadButton.South);
            InputSystem.QueueStateEvent(_pad, state);
        }
        private void Process()
        {
            InputSystem.Update(); Step(_reader); Step(_carrier);
        }
        [UnityTest] public IEnumerator ABackgroundQueuedThrowCannotStartAfterFocusReturn()
        {
            Focus(false); Queue(1); Focus(true);
            // The device event was produced while unfocused, but its state reaches
            // the actions only after the focus callback's immediate capture.
            Process(); yield return null; Step(_reader); Step(_carrier);
            Assert.IsFalse(_carrier.IsCharging, "A queued background hold started a new charge after focus return.");
            Queue(); Process(); yield return null;
            Assert.AreSame(_shoe, _carrier.Held, "Releasing a background hold launched the shoe.");
        }
        [UnityTest] public IEnumerator ABackgroundQueuedRecoveryCannotAppearAfterFocusReturn()
        {
            Focus(false); Queue(jump: true); Focus(true); Process();
            yield return null; Step(_reader);
            Assert.IsFalse(_motor.Intent.Pressed(Verb.Jump), "Queued background recovery became gameplay input.");
            Assert.IsFalse(_motor.Intent.JustPressed(Verb.Jump));
        }
        [UnityTest] public IEnumerator AnAlreadyProcessedBackgroundHoldStillWaitsForRelease()
        {
            Focus(false); Queue(1); Process(); Focus(true);
            yield return null; Step(_reader); Step(_carrier);
            Assert.IsFalse(_carrier.IsCharging); Assert.AreSame(_shoe, _carrier.Held);
        }
        [UnityTest] public IEnumerator AFreshThrowAfterFocusSettlesStillChargesAndReleases()
        {
            Focus(false); Focus(true); yield return null; yield return null;
            Queue(1); Process(); yield return null; Step(_reader); Step(_carrier);
            Assert.IsTrue(_carrier.IsCharging, "A fresh focused press must still start its normal charge.");
            Queue(); Process(); yield return null;
            Assert.IsNull(_carrier.Held); Assert.AreEqual(SlipperState.InFlight, _shoe.State);
        }
        [UnityTest] public IEnumerator RecoveryRetirementDoesNotBlockRecoveredMovement()
        {
            Focus(false); Queue(jump: true, move: Vector2.right); Focus(true); Process();
            yield return null; Step(_reader);
            Assert.AreEqual(Vector2.right, _motor.Intent.MoveAxis);
            Assert.IsFalse(_motor.Intent.Parked);
            Assert.IsFalse(_motor.Intent.Pressed(Verb.Jump), "Movement may recover without reviving its queued recovery button.");
        }
    }
}
