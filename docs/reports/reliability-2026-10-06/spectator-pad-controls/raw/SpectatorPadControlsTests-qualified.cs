using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorPadControlsTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root, _body;
        private SpectatorCamera _spectator;
        private CharacterMotor _actor;
        private InputActionAsset _asset;
        private Gamepad _pad;
        private Keyboard _keyboard, _controlKeyboard;
        private CursorLockMode _cursor;
        private bool _cursorVisible;
        private InputSettings.BackgroundBehavior _background;
        private InputSettings.EditorInputBehaviorInPlayMode _editor;
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _keyboard = Keyboard.current;
            if (_keyboard == null) _controlKeyboard = InputSystem.AddDevice<Keyboard>();
            _cursor = Cursor.lockState; _cursorVisible = Cursor.visible;
            _background = InputSystem.settings.backgroundBehavior;
            _editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _pad = InputSystem.AddDevice<Gamepad>(); InputSystem.EnableDevice(_pad);
            _asset = Object.Instantiate(Resources.Load<InputActionAsset>("TumbangPreso"));
            Assert.IsNotNull(_asset); _asset.Enable();
            _body = new GameObject("Spectator pad target"); _actor = _body.AddComponent<CharacterMotor>(); _actor.enabled = false;
            _root = new GameObject("Spectator pad operator"); _spectator = _root.AddComponent<SpectatorCamera>(); _spectator.enabled = false;
            SpectatorCamera.Register(_actor);
            foreach (var pair in new[] { new[] { "_cycleTarget", "SpectatorCycleTarget" }, new[] { "_freeFly", "SpectatorFreeFly" }, new[] { "_povToggle", "SpectatorPov" } })
                typeof(SpectatorCamera).GetField(pair[0], Hidden).SetValue(_spectator, _asset.FindAction(pair[1], true));
            yield return null;
        }
        [UnityTearDown] public IEnumerator After()
        {
            typeof(Keyboard).GetProperty("current", BindingFlags.Static | BindingFlags.Public).SetValue(null, _keyboard);
            if (_controlKeyboard != null && _controlKeyboard.added) InputSystem.RemoveDevice(_controlKeyboard);
            SpectatorCamera.Unregister(_actor);
            if (_pad != null && _pad.added) InputSystem.RemoveDevice(_pad);
            if (_asset != null) { _asset.Disable(); Object.Destroy(_asset); }
            if (_root != null) Object.Destroy(_root); if (_body != null) Object.Destroy(_body);
            yield return PlayModeWorld.Reset();
            Cursor.lockState = _cursor; Cursor.visible = _cursorVisible;
            InputSystem.settings.backgroundBehavior = _background;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editor;
        }
        private void Press(GamepadButton button, bool withoutKeyboard)
        {
            InputSystem.QueueStateEvent(_pad, new GamepadState().WithButton(button)); InputSystem.Update();
            if (withoutKeyboard) typeof(Keyboard).GetProperty("current", BindingFlags.Static | BindingFlags.Public).SetValue(null, null);
            Assert.IsTrue(_asset.FindAction(button == GamepadButton.RightShoulder ? "SpectatorCycleTarget" : button == GamepadButton.North ? "SpectatorFreeFly" : "SpectatorPov", true).WasPressedThisFrame());
            typeof(SpectatorCamera).GetMethod("StepKeys", Hidden).Invoke(_spectator, null);
        }
        private object Follow => typeof(SpectatorCamera).GetField("_follow", Hidden).GetValue(_spectator);
        private void FollowActor() => typeof(SpectatorCamera).GetField("_follow", Hidden).SetValue(_spectator, _actor);
        [Test] public void PadTargetCycleWorksWithoutAKeyboard()
        { Press(GamepadButton.RightShoulder, true); Assert.AreSame(_actor, Follow); }
        [Test] public void PadFreeFlightWorksWithoutAKeyboard()
        { FollowActor(); Press(GamepadButton.North, true); Assert.IsNull(Follow); }
        [Test] public void PadPovToggleWorksWithoutAKeyboard()
        { FollowActor(); Press(GamepadButton.West, true); Assert.IsTrue((bool)typeof(SpectatorCamera).GetField("_pov", Hidden).GetValue(_spectator)); }
        [Test] public void ExistingPadTargetCycleWithAKeyboardStillWorks()
        { Assert.IsNotNull(Keyboard.current); Press(GamepadButton.RightShoulder, false); Assert.AreSame(_actor, Follow); }
    }
}
