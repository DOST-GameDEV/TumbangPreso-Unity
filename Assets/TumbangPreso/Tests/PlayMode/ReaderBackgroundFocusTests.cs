using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ReaderBackgroundFocusTests
    {
        private GameObject _body;
        private CharacterMotor _motor;
        private PlayerInputReader _reader;
        private Gamepad _pad;
        private INetProvider _provider;
        private bool _touch;
        private InputSettings.BackgroundBehavior _background;
        private InputSettings.EditorInputBehaviorInPlayMode _editor;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; NetAuthority.Provider = new SoloProvider();
            _touch = TouchInput.Active; TouchInput.ReleaseAll(); TouchInput.Active = false;
            _background = InputSystem.settings.backgroundBehavior;
            _editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            // Model a device that can continue delivering input to a background player.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _pad = InputSystem.AddDevice<Gamepad>(); InputSystem.EnableDevice(_pad);
            PresentationClock.RequestScale(1);
            _body = new GameObject("Background focus reader owner");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 1; _motor.RoundActive = true;
            _reader = _body.AddComponent<PlayerInputReader>();
            yield return null; Assert.IsTrue(_reader.enabled);
        }

        [UnityTearDown] public IEnumerator After()
        {
            if (_body != null) Object.Destroy(_body); yield return null;
            if (_pad != null && _pad.added) InputSystem.RemoveDevice(_pad);
            InputSystem.settings.backgroundBehavior = _background;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editor;
            TouchInput.ReleaseAll(); TouchInput.Active = _touch;
            NetAuthority.Provider = _provider; PresentationClock.RequestScale(1);
            yield return PlayModeWorld.Reset();
        }

        private void Pad(Vector2 move, bool jump = false)
        {
            var state = new GamepadState { leftStick = move };
            if (jump) state = state.WithButton(GamepadButton.South);
            InputSystem.QueueStateEvent(_pad, state); InputSystem.Update();
            typeof(PlayerInputReader).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_reader, null);
        }
        private void Focus(bool focused) => _body.SendMessage("OnApplicationFocus", focused, SendMessageOptions.DontRequireReceiver);

        [Test] public void BackgroundFramesCannotRestoreHeldMovement()
        {
            Pad(Vector2.up); Assert.AreEqual(Vector2.up, _motor.Intent.MoveAxis);
            Focus(false); Pad(Vector2.up); Pad(Vector2.up);
            Assert.AreEqual(Vector2.zero, _motor.Intent.MoveAxis, "Background frames restored hardware movement after focus retirement.");
            Assert.IsFalse(_motor.Intent.Parked, "Focus ownership must not replace pause ownership.");
        }

        [Test] public void NewBackgroundButtonCannotBufferRecovery()
        {
            Focus(false); Pad(Vector2.zero, true);
            Assert.IsFalse(_motor.Intent.Pressed(Verb.Jump), "A new background press reached gameplay.");
            Assert.IsFalse(_motor.Intent.JustPressed(Verb.Jump), "A new background press buffered recovery.");
        }

        [UnityTest] public IEnumerator FocusReturnWaitsForBackgroundHeldButtonRelease()
        {
            Focus(false); Pad(Vector2.zero, true); Focus(true);
            yield return null; Pad(Vector2.zero, true);
            Assert.IsFalse(_motor.Intent.Pressed(Verb.Jump), "Focus return imported a button already held in the background.");
            Assert.IsFalse(_motor.Intent.JustPressed(Verb.Jump));
            Pad(Vector2.zero); yield return null; Pad(Vector2.zero, true);
            Assert.IsTrue(_motor.Intent.Pressed(Verb.Jump)); Assert.IsTrue(_motor.Intent.JustPressed(Verb.Jump));
        }

        [Test] public void FocusedHardwareAndRecoveredMovementRemainAvailable()
        {
            Pad(Vector2.right, true);
            Assert.AreEqual(Vector2.right, _motor.Intent.MoveAxis); Assert.IsTrue(_motor.Intent.Pressed(Verb.Jump));
            Focus(false); Pad(Vector2.zero); Focus(true); Pad(Vector2.up);
            Assert.AreEqual(Vector2.up, _motor.Intent.MoveAxis); Assert.IsFalse(_motor.Intent.Parked);
        }
    }
}
