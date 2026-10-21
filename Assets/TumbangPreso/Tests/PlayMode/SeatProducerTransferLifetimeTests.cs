using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class SeatProducerTransferLifetimeTests
    {
        private sealed class Role : INetProvider
        {
            public bool IsNetworked => true;
            public bool IsHost { get; set; }
            public int LocalSlot { get; set; } = 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }
        private GameObject _root;
        private MatchInstaller _installer;
        private CharacterMotor _body;
        private RoundDirector _previousRound;
        private MatchRpc _rpc;
        private Role _role;
        private INetProvider _previousProvider;
        private bool _previousSpectator, _previousAllBots, _previousBots, _cursorVisible;
        private CursorLockMode _cursorLock;
        private readonly Dictionary<InputAction, bool> _actions = new Dictionary<InputAction, bool>();

        [UnitySetUp] public IEnumerator Before()
        {
            _previousProvider = NetAuthority.Provider; _previousSpectator = GameLaunch.Spectator;
            _previousAllBots = GameLaunch.AllBots; _previousBots = AIController.BotsEnabled;
            _cursorLock = Cursor.lockState; _cursorVisible = Cursor.visible;
            yield return PlayModeWorld.Reset();
            _previousRound = GameServices.Round;
            Assert.IsNull(NetSession.Instance, "These role transfers must not dispatch a live transport");
            _role = new Role(); NetAuthority.Provider = _role;
            GameLaunch.Spectator = false; GameLaunch.AllBots = false; AIController.BotsEnabled = true;
            _actions.Clear();
            foreach (var action in Resources.Load<InputActionAsset>("TumbangPreso")) _actions[action] = action.enabled;
            _root = new GameObject("Seat producer deferred retirement");
            var dormant = new GameObject("Dormant installer"); dormant.transform.SetParent(_root.transform); dormant.SetActive(false);
            _installer = dormant.AddComponent<MatchInstaller>();
            var round = dormant.AddComponent<RoundDirector>();
            _rpc = dormant.AddComponent<MatchRpc>();
            typeof(GameServices).GetProperty("Round").SetValue(null, round);
            var body = new GameObject("Seat1", typeof(CharacterController)); body.transform.SetParent(_root.transform);
            _body = body.AddComponent<CharacterMotor>(); _body.PlayerSlot = 1;
            round.Register(_body);
            var seats = new CharacterMotor[Core.Balance.PlayerCount]; seats[1] = _body;
            typeof(MatchInstaller).GetField("_seats", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_installer, seats);
            var watcher = new GameObject("Existing spectator"); watcher.transform.SetParent(_root.transform);
            watcher.AddComponent<SpectatorCamera>();
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            typeof(GameServices).GetProperty("Round").SetValue(null, _previousRound);
            yield return PlayModeWorld.Reset();
            foreach (var action in _actions) if (action.Value) action.Key.Enable(); else action.Key.Disable();
            NetAuthority.Provider = _previousProvider; GameLaunch.Spectator = _previousSpectator;
            GameLaunch.AllBots = _previousAllBots; AIController.BotsEnabled = _previousBots;
            Cursor.lockState = _cursorLock; Cursor.visible = _cursorVisible;
        }
        private void Play()
        { GameLaunch.Spectator = false; _role.LocalSlot = 1; _installer.RebindLocalSeat(1, false); }
        private void Watch()
        { GameLaunch.Spectator = true; _role.LocalSlot = -1; _installer.RebindLocalSeat(-1, true); }

        private IEnumerator ReaderReturns(bool host)
        {
            _role.IsHost = host; Play(); var first = _body.GetComponent<PlayerInputReader>();
            Assert.IsNotNull(first); Watch(); Play();
            Assert.IsTrue(_body.IsLocallySimulated(), "Prime the real simulation cache before deferred destruction settles");
            yield return null;
            var readers = _body.GetComponents<PlayerInputReader>();
            Assert.AreEqual(1, readers.Length, "The final player reused a reader already queued for destruction");
            Assert.IsTrue(readers[0].enabled); Assert.IsTrue(first == null);
            Assert.IsTrue(_body.IsLocallySimulated(), "The host cache retained a destroyed producer ahead of the replacement");
            Assert.IsFalse(_body.GetComponents<AIController>().Any(ai => ai.enabled));
        }
        [UnityTest] public IEnumerator ClientPlayerWatchPlayerKeepsItsFinalReader() => ReaderReturns(false);
        [UnityTest] public IEnumerator HostPlayerWatchPlayerKeepsItsFinalReaderAndSimulation() => ReaderReturns(true);

        [UnityTest] public IEnumerator HostBotPlayerWatchKeepsTheFinalBotAndSimulation()
        {
            _role.IsHost = true; _body.gameObject.AddComponent<AIController>(); Play(); Watch();
            Assert.IsTrue(_body.IsLocallySimulated(), "Prime the real cache while old producers still await destruction");
            yield return null;
            var brains = _body.GetComponents<AIController>();
            Assert.AreEqual(1, brains.Length, "Returning to watch skipped the replacement bot because the doomed one still existed");
            Assert.IsTrue(brains[0].enabled); Assert.IsTrue(_body.IsLocallySimulated());
            Assert.IsEmpty(_body.GetComponents<PlayerInputReader>());
        }

        [UnityTest] public IEnumerator RepeatingTheCurrentPlayerSeatKeepsOneExistingReader()
        {
            Play(); var reader = _body.GetComponent<PlayerInputReader>(); Play();
            yield return null;
            Assert.AreSame(reader, _body.GetComponent<PlayerInputReader>());
            Assert.IsTrue(reader.enabled); Assert.AreEqual(1, _body.GetComponents<PlayerInputReader>().Length);
        }

        [UnityTest] public IEnumerator RepeatingWatchKeepsOneHostBot()
        {
            _role.IsHost = true; Play(); Watch(); var brain = _body.GetComponents<AIController>().First(ai => ai.enabled);
            Watch(); yield return null;
            Assert.AreSame(brain, _body.GetComponent<AIController>()); Assert.IsTrue(brain.enabled);
            Assert.AreEqual(1, _body.GetComponents<AIController>().Length); Assert.IsEmpty(_body.GetComponents<PlayerInputReader>());
        }

        [UnityTest] public IEnumerator FinalWatchRetiresEveryReaderFromRapidRoleChanges()
        {
            Play(); Watch(); Play(); Watch(); yield return null;
            Assert.IsEmpty(_body.GetComponents<PlayerInputReader>());
            Assert.IsFalse(_body.IsLocallySimulated());
        }

        [UnityTest] public IEnumerator DisabledBotStillRetainsTheHostsExistingSimulationOwnership()
        {
            _role.IsHost = true; var brain = _body.gameObject.AddComponent<AIController>(); brain.enabled = false;
            _body.ForgetInputSource(); yield return null;
            Assert.IsTrue(_body.IsLocallySimulated(), "Pausing a surviving producer must preserve existing ownership semantics");
            Assert.AreSame(brain, _body.GetComponent<AIController>());
        }

        [UnityTest] public IEnumerator OnlineRebindCannotReactivateTheRetiredReaderAfterInstallerHandover()
        {
            _role.IsHost = true; Play(); Watch(); Play();
            var apply = typeof(MatchRpc).GetMethod("ApplyRebindLocalSeat", BindingFlags.Instance | BindingFlags.NonPublic);
            apply.Invoke(_rpc, new object[] { 1, 0, true, "Transfer check" });
            Assert.AreEqual(1, _body.GetComponents<PlayerInputReader>().Count(reader => reader.enabled),
                "The online handler re-enabled a reader already queued for Destroy alongside the installer's replacement");
            Assert.IsTrue(_body.IsLocallySimulated()); yield return null;
            var readers = _body.GetComponents<PlayerInputReader>();
            Assert.AreEqual(1, readers.Length); Assert.IsTrue(readers[0].enabled);
            Assert.IsTrue(_body.IsLocallySimulated(), "Online transfer left the cached simulation owner at the destroyed reader");
        }
    }
}
