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
    public sealed class FocusPulseBoundaryTests
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
        private IEnumerator Pulse(bool fresh)
        {
            Focus(false); bool delivered=false;
            var boundary=_motor.gameObject.AddComponent<PulseLateFocusBoundary1006>();
            boundary.Once=()=>
            {
                if(fresh)Focus(true);
                Queue(jump:true);Queue();
                if(!fresh)Focus(true);
                delivered=true;
            };
            while(!delivered)yield return null;
            yield return null;Step(_reader);
        }
        [UnityTest] public IEnumerator BackgroundRecoveryTapDoesNotBufferAfterFocusReturn()
        {
            yield return Pulse(false);
            Assert.IsFalse(_motor.Intent.Pressed(Verb.Jump));
            Assert.IsFalse(_motor.Intent.JustPressed(Verb.Jump),"A queued background down/up recovery pulse survived focus retirement.");
        }
        [UnityTest] public IEnumerator FreshFocusedRecoveryTapStillBuffersForPhysics()
        {
            yield return Pulse(true);
            Assert.IsTrue(_motor.Intent.JustPressed(Verb.Jump),"A genuine focused recovery tap was lost before physics consumption.");
        }
    }
    public sealed class PulseLateFocusBoundary1006 : MonoBehaviour
    {
        public System.Action Once;
        private void LateUpdate()
        {
            var action = Once; Once = null; action?.Invoke();
        }
    }
}
