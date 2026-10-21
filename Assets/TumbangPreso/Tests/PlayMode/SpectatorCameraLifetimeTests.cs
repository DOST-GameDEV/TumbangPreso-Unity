using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorCameraLifetimeTests
    {
        private sealed class Client : INetProvider
        {
            public bool IsNetworked => true;
            public bool IsHost => false;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }
        private GameObject _root;
        private MatchInstaller _installer;
        private SpectatorCamera _watcher;
        private Camera _camera;
        private CharacterMotor _body;
        private INetProvider _provider;
        private bool _spectator, _allBots, _cursorVisible;
        private CursorLockMode _cursorLock;
        private readonly Dictionary<InputAction, bool> _actionStates = new Dictionary<InputAction, bool>();

        [UnitySetUp]
        public IEnumerator Before()
        {
            _provider = NetAuthority.Provider;
            _spectator = GameLaunch.Spectator; _allBots = GameLaunch.AllBots;
            _cursorLock = Cursor.lockState; _cursorVisible = Cursor.visible;
            yield return PlayModeWorld.Reset();
            Assert.IsNull(Object.FindFirstObjectByType<SpectatorCamera>());
            Assert.IsNull(Object.FindFirstObjectByType<CameraRig>());
            NetAuthority.Provider = new Client(); GameLaunch.AllBots = false;
            _actionStates.Clear();
            var actions = Resources.Load<InputActionAsset>("TumbangPreso");
            Assert.IsNotNull(actions);
            foreach (var action in actions) _actionStates[action] = action.enabled;

            _root = new GameObject("Spectator role lifetime");
            var installerRoot = new GameObject("Inactive installer");
            installerRoot.transform.SetParent(_root.transform); installerRoot.SetActive(false);
            _installer = installerRoot.AddComponent<MatchInstaller>();
            var bodyRoot = new GameObject("Seat1", typeof(CharacterController));
            bodyRoot.transform.SetParent(_root.transform);
            _body = bodyRoot.AddComponent<CharacterMotor>(); _body.PlayerSlot = 1;
            var seats = new CharacterMotor[Core.Balance.PlayerCount]; seats[1] = _body;
            typeof(MatchInstaller).GetField("_seats", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_installer, seats);
            var watchRoot = new GameObject("Existing spectator"); watchRoot.transform.SetParent(_root.transform);
            _watcher = watchRoot.AddComponent<SpectatorCamera>();
            _camera = watchRoot.GetComponent<Camera>();
            _camera.depth = 99;
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            yield return PlayModeWorld.Reset();
            foreach (var state in _actionStates)
                if (state.Value) state.Key.Enable(); else state.Key.Disable();
            NetAuthority.Provider = _provider;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots;
            Cursor.lockState = _cursorLock; Cursor.visible = _cursorVisible;
        }

        [Test]
        public void DisabledSpectatorControllerCannotKeepRenderingOverThePlayer()
        {
            Assert.IsTrue(_camera.enabled);
            _watcher.enabled = false;
            Assert.IsFalse(_camera.enabled,
                "Disabling the controller left its high-depth Unity Camera rendering.");
        }

        [Test]
        public void WatchingAgainReusesAndEnablesTheExistingController()
        {
            _watcher.enabled = false;
            _camera.enabled = false; // establish the retired state independently of the other fix
            GameLaunch.Spectator = true;
            _installer.RebindLocalSeat(-1, true);
            Assert.IsTrue(_watcher.enabled, "The watcher was found but never re-enabled.");
            Assert.IsTrue(_camera.enabled);
            Assert.AreEqual(1, Object.FindObjectsByType<SpectatorCamera>(FindObjectsSortMode.None).Length);
        }

        [Test]
        public void PublicSeatChangesCompleteWatchSeatWatchWithOneCameraOwner()
        {
            GameLaunch.Spectator = true; _installer.RebindLocalSeat(-1, true);
            Assert.IsTrue(_watcher.enabled); Assert.IsTrue(_camera.enabled);
            GameLaunch.Spectator = false; _installer.RebindLocalSeat(1, false);
            Assert.IsFalse(_watcher.enabled);
            Assert.IsFalse(_camera.enabled, "The retired spectator still owned a rendered view while seated.");
            Assert.IsNotNull(_body.GetComponent<PlayerInputReader>());
            GameLaunch.Spectator = true; _installer.RebindLocalSeat(-1, true);
            Assert.IsTrue(_watcher.enabled); Assert.IsTrue(_camera.enabled);
            var reader = _body.GetComponent<PlayerInputReader>();
            Assert.IsTrue(reader == null || !reader.enabled);
            Assert.AreEqual(1, Object.FindObjectsByType<SpectatorCamera>(FindObjectsSortMode.None).Length);
        }
    }
}
