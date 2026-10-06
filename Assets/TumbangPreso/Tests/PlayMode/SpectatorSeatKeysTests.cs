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
    public sealed class SpectatorSeatKeysTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root; private CharacterMotor _actor; private DebugPlayerSwitcher _switcher;
        private MatchInstaller _installer; private SpectatorCamera _watcher;
        private Keyboard _keyboard, _originalKeyboard; private INetProvider _provider;
        private bool _spectator, _allBots; private CursorLockMode _cursor; private bool _cursorVisible;
        private InputSettings.BackgroundBehavior _background; private InputSettings.EditorInputBehaviorInPlayMode _editor;
        private sealed class Solo : INetProvider
        { public bool IsHost => true; public bool IsNetworked => false; public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false; }
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset(); _provider = NetAuthority.Provider; NetAuthority.Provider = new Solo();
            _spectator = GameLaunch.Spectator; _allBots = GameLaunch.AllBots; GameLaunch.Spectator = false; GameLaunch.AllBots = false;
            _cursor = Cursor.lockState; _cursorVisible = Cursor.visible;
            _background = InputSystem.settings.backgroundBehavior; _editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _originalKeyboard = Keyboard.current; _keyboard = InputSystem.AddDevice<Keyboard>();
            _root = new GameObject("Spectator seat key fixture");
            var body = new GameObject("Seat1", typeof(CharacterController)); body.transform.SetParent(_root.transform);
            _actor = body.AddComponent<CharacterMotor>(); _actor.enabled = false; _actor.PlayerSlot = 1;
            body.AddComponent<PlayerInputReader>();
            var installer = new GameObject("Inactive installer"); installer.transform.SetParent(_root.transform); installer.SetActive(false);
            _installer = installer.AddComponent<MatchInstaller>(); var seats = new CharacterMotor[4]; seats[1] = _actor;
            typeof(MatchInstaller).GetField("_seats", Hidden).SetValue(_installer, seats);
            var watch = new GameObject("Watcher"); watch.transform.SetParent(_root.transform); _watcher = watch.AddComponent<SpectatorCamera>(); _watcher.enabled = false;
            _switcher = _root.AddComponent<DebugPlayerSwitcher>(); _switcher.enabled = false;
            yield return null;
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
            typeof(Keyboard).GetProperty("current", BindingFlags.Public | BindingFlags.Static).SetValue(null, _originalKeyboard);
            if (_root != null) Object.Destroy(_root); yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _provider; GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots;
            InputSystem.settings.backgroundBehavior = _background; InputSystem.settings.editorInputBehaviorInPlayMode = _editor;
            Cursor.lockState = _cursor; Cursor.visible = _cursorVisible;
        }
        private void Press()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.F2)); InputSystem.Update();
            Assert.IsTrue(Keyboard.current.f2Key.wasPressedThisFrame);
            typeof(DebugPlayerSwitcher).GetMethod("Update", Hidden).Invoke(_switcher, null);
        }
        [Test] public void WatchHandoffCannotBeUndoneByAPovShortcut()
        {
            GameLaunch.Spectator = true; _installer.RebindLocalSeat(-1, true);
            Assert.IsTrue(_watcher.enabled); Press();
            Assert.IsTrue(_watcher.enabled, "F2 took a body instead of leaving spectator POV in control.");
            var reader = _actor.GetComponent<PlayerInputReader>(); Assert.IsTrue(reader == null || !reader.enabled);
        }
        [Test] public void ActiveWatcherWithoutLaunchFlagStillOwnsItsPovKeys()
        {
            _installer.RebindLocalSeat(-1, true); Assert.IsFalse(GameLaunch.Spectator);
            Assert.IsTrue(_watcher.enabled); Press(); Assert.IsTrue(_watcher.enabled);
            var reader = _actor.GetComponent<PlayerInputReader>(); Assert.IsTrue(reader == null || !reader.enabled);
        }
        [Test] public void GameplayStillSupportsTheSoloSeatShortcut()
        { Assert.IsFalse(_watcher.enabled); Press(); Assert.AreEqual(1, _switcher.DrivenSlot); Assert.IsTrue(_actor.GetComponent<PlayerInputReader>().enabled); }
        [Test] public void PovShortcutSelectsAViewWithoutClaimingItsBody()
        {
            _installer.RebindLocalSeat(-1, true); Press();
            typeof(SpectatorCamera).GetMethod("StepBroadcastKeys", Hidden).Invoke(_watcher, null);
            Assert.AreSame(_actor, typeof(SpectatorCamera).GetField("_follow", Hidden).GetValue(_watcher));
            Assert.IsTrue((bool)typeof(SpectatorCamera).GetField("_pov", Hidden).GetValue(_watcher));
            Assert.IsTrue(_watcher.enabled);
            var reader = _actor.GetComponent<PlayerInputReader>(); Assert.IsTrue(reader == null || !reader.enabled);
        }
    }
}
