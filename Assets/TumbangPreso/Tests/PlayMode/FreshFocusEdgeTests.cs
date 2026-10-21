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
    public sealed class FreshFocusEdgeTests
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
        private IEnumerator LateBoundary(bool fresh)
        {
            Focus(false);
            bool delivered = false;
            var boundary = _motor.gameObject.AddComponent<LateFocusBoundary1006>();
            boundary.Once = () =>
            {
                if (fresh) { Focus(true); Queue(1); }
                else { Queue(1); Focus(true); }
                delivered = true;
            };
            while (!delivered) yield return null;
            yield return null;
            Step(_reader); Step(_carrier);
        }
        [UnityTest] public IEnumerator FreshThrowAfterFocusCallbackBeforeFirstReadRemainsAvailable()
        {
            yield return LateBoundary(true);
            Assert.IsTrue(_carrier.IsCharging, "A genuinely fresh focused press before the first resumed read was discarded.");
        }
        [UnityTest] public IEnumerator BackgroundThrowBeforeLateFocusCallbackStillWaitsForRelease()
        {
            yield return LateBoundary(false);
            Assert.IsFalse(_carrier.IsCharging, "Queued background input escaped release suppression.");
            Queue(); Process(); yield return null;
            Assert.AreSame(_shoe, _carrier.Held);
        }
    }
    public sealed class LateFocusBoundary1006 : MonoBehaviour
    {
        public System.Action Once;
        private void LateUpdate()
        {
            var action = Once; Once = null; action?.Invoke();
        }
    }
}
