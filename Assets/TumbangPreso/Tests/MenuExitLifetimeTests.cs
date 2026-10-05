using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.Tests
{
    public sealed class MenuExitLifetimeTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticHidden = BindingFlags.Static | BindingFlags.NonPublic;
        private GameObject _root;
        private MatchDirector _match, _previousMatch;
        private RoundDirector _round, _previousRound;
        private HalftimePresentation _break, _previousBreak;
        private Net.NetSession _previousNet;
        private object _previousTelemetry;
        private INetProvider _provider;
        private string _pendingScene;
        private int _pendingFrame;
        private bool _networked, _visible, _hubEnabled;
        private CursorLockMode _cursor;
        private float _scale;
        private HubEntry _entry;
        private object _lobbyMode;
        private readonly Dictionary<FieldInfo, object> _launch = new Dictionary<FieldInfo, object>();
        private readonly Dictionary<PropertyInfo, object> _abandon = new Dictionary<PropertyInfo, object>();
        private static void Property(System.Type type, string name, object value) => type.GetProperty(name).SetValue(null, value);
        private static void Value(object target, string name, object value) => target.GetType().GetProperty(name).SetValue(target, value);
        private static void Clock(string name) => typeof(PresentationClock).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);

        [SetUp] public void Before()
        {
            Assert.IsFalse(PresentationClock.Held, "No unrelated presentation may own this fixture.");
            _scale = Time.timeScale; _cursor = Cursor.lockState; _visible = Cursor.visible;
            _provider = NetAuthority.Provider; NetAuthority.Provider = null;
            _previousMatch = GameServices.Match; _previousRound = GameServices.Round;
            _previousBreak = HalftimePresentation.Instance; _previousNet = Net.NetSession.Instance;
            _previousTelemetry = typeof(GameServices).GetProperty("Telemetry").GetValue(null);
            Property(typeof(GameServices), "Telemetry", null); Property(typeof(Net.NetSession), "Instance", null);
            _pendingScene = (string)typeof(SceneFlow).GetField("_pendingScene", StaticHidden).GetValue(null);
            _pendingFrame = (int)typeof(SceneFlow).GetField("_pendingFrame", StaticHidden).GetValue(null);
            _networked = SceneFlow.Networked; _entry = TumpHub.PendingEntry;
            _hubEnabled = ConvertedMatchSetup.HubEnabled;
            _lobbyMode = typeof(PlaySelectionScreen).GetField("RequestedLobbyMode").GetValue(null);
            _launch.Clear();
            foreach (var field in typeof(GameLaunch).GetFields(BindingFlags.Static | BindingFlags.Public))
                if (!field.IsInitOnly && !field.IsLiteral) _launch[field] = field.GetValue(null);
            _abandon.Clear();
            foreach (var property in typeof(MatchAbandon).GetProperties(BindingFlags.Static | BindingFlags.Public))
                if (property.GetSetMethod(true) != null) _abandon[property] = property.GetValue(null);
            _root = new GameObject("Dormant menu lifetime"); _root.SetActive(false);
            _match = _root.AddComponent<MatchDirector>(); _round = _root.AddComponent<RoundDirector>();
            _break = _root.AddComponent<HalftimePresentation>();
            Property(typeof(GameServices), "Match", _match); Property(typeof(GameServices), "Round", _round);
            Property(typeof(HalftimePresentation), "Instance", _break);
            var actor = _root.AddComponent<CharacterMotor>(); actor.PlayerSlot = 0; _round.Register(actor);
            _round.BeginRound(); Value(_match, "MatchInProgress", true); Value(_match, "RoundNumber", 1);
            Time.timeScale = 1;
        }
        [TearDown] public void After()
        {
            _break?.End(false);
            if (_root != null) Object.DestroyImmediate(_root);
            Property(typeof(GameServices), "Match", _previousMatch); Property(typeof(GameServices), "Round", _previousRound);
            Property(typeof(GameServices), "Telemetry", _previousTelemetry); Property(typeof(HalftimePresentation), "Instance", _previousBreak);
            Property(typeof(Net.NetSession), "Instance", _previousNet); NetAuthority.Provider = _provider;
            typeof(SceneFlow).GetField("_pendingScene", StaticHidden).SetValue(null, _pendingScene);
            typeof(SceneFlow).GetField("_pendingFrame", StaticHidden).SetValue(null, _pendingFrame);
            foreach (var saved in _launch) saved.Key.SetValue(null, saved.Value);
            foreach (var saved in _abandon) saved.Key.SetValue(null, saved.Value);
            SceneFlow.Networked = _networked; TumpHub.PendingEntry = _entry;
            ConvertedMatchSetup.HubEnabled = _hubEnabled;
            typeof(PlaySelectionScreen).GetField("RequestedLobbyMode").SetValue(null, _lobbyMode);
            PresentationClock.RequestScale(_scale); Cursor.lockState = _cursor; Cursor.visible = _visible;
        }
        private void Leave()
        {
            // HOME is already queued in this frame. Exercise the actual exit API while its
            // existing duplicate-load guard avoids an unrelated scene load in EditMode.
            typeof(SceneFlow).GetField("_pendingScene", StaticHidden).SetValue(null, SceneFlow.MatchSetup);
            typeof(SceneFlow).GetField("_pendingFrame", StaticHidden).SetValue(null, Time.frameCount);
            if (!Application.CanStreamedLevelBeLoaded(SceneFlow.MatchSetup))
                LogAssert.Expect(LogType.Error, "[Flow] scene '" + SceneFlow.MatchSetup + "' is not in the build settings. Add it, or the button that asked for it will do nothing in a build.");
            SceneFlow.LeaveMatchToMainMenu();
        }
        [Test] public void LeavingALiveRoundRetiresPersistentSimulation()
        {
            int ended = 0; _match.MatchEnded += _ => ended++;
            Leave();
            Assert.IsFalse(_round.RoundActive, "The old arena keeps ticking on HOME.");
            Assert.IsFalse(_match.MatchInProgress); Assert.IsFalse(_match.IsWarmupBuffer);
            Assert.AreEqual(0, _round.Players.Count); Assert.AreEqual(0, ended, "Abandonment must not author a completed match.");
            float remaining = _round.TimeLeft;
            typeof(RoundDirector).GetMethod("FixedUpdate", Hidden).Invoke(_round, null);
            Assert.AreEqual(remaining, _round.TimeLeft);
        }
        [Test] public void LeavingAnIntermissionRetiresItsHoldWithoutAdvancing()
        {
            _round.EndRound(); _match.IsWarmupBuffer = true;
            Value(_break, "Active", true); Value(_break, "IsHalftime", true); Clock("Hold");
            int advances = 0; _match.RoundStarted += (_, __) => advances++;
            Leave();
            Assert.IsFalse(_break.Active, "Persistent break can advance the abandoned match.");
            Assert.IsFalse(PresentationClock.Held); Assert.IsFalse(_match.IsWarmupBuffer);
            Assert.IsFalse(_match.MatchInProgress); Assert.AreEqual(0, advances);
        }
        [Test] public void RepeatedExitRemainsQuiescent()
        {
            Leave(); Leave();
            Assert.IsFalse(_round.RoundActive); Assert.IsFalse(_match.MatchInProgress);
            Assert.AreEqual(0, _round.Players.Count); Assert.IsFalse(PresentationClock.Held);
        }
        [Test] public void TheControlRoundReallyTicksBeforeExit()
        {
            float remaining = _round.TimeLeft;
            typeof(RoundDirector).GetMethod("FixedUpdate", Hidden).Invoke(_round, null);
            Assert.Less(_round.TimeLeft, remaining); Assert.IsTrue(_round.RoundActive);
        }
        private void HostLoss()
        {
            Assert.AreNotEqual(SceneFlow.MatchSetup, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            MatchAbandon.Note("timeout", false);
            typeof(SceneFlow).GetField("_pendingScene", StaticHidden).SetValue(null, SceneFlow.MatchSetup);
            typeof(SceneFlow).GetField("_pendingFrame", StaticHidden).SetValue(null, Time.frameCount);
            if (!Application.CanStreamedLevelBeLoaded(SceneFlow.MatchSetup))
                LogAssert.Expect(LogType.Error, "[Flow] scene '" + SceneFlow.MatchSetup + "' is not in the build settings. Add it, or the button that asked for it will do nothing in a build.");
            var rpc = _root.AddComponent<Net.MatchRpc>();
            typeof(Net.MatchRpc).GetMethod("HandleClientDisconnected", Hidden).Invoke(rpc, new object[] { "Host lost." });
        }
        [Test] public void HostLossRetiresSimulationBeforeReturningToTheLobby()
        {
            ConvertedMatchSetup.HubEnabled = false;
            HostLoss();
            Assert.IsFalse(_round.RoundActive); Assert.IsFalse(_match.MatchInProgress);
            Assert.IsTrue(SceneFlow.Networked, "The existing empty online-lobby route must remain available.");
            Assert.AreEqual(1, MatchAbandon.RoundNumber, "Capture the failed round before retiring its state.");
        }
        [TestCase(false)]
        [TestCase(true)]
        public void HubHostLossReturnsHomeWithoutRequestingAnotherRoom(bool completed)
        {
            ConvertedMatchSetup.HubEnabled = true;
            Value(_match, "HasCompleted", completed);
            PlaySelectionScreen.RequestedLobbyMode = LobbyMode.Custom;
            TumpHub.PendingEntry = HubEntry.Lobby;
            HostLoss();
            Assert.IsFalse(SceneFlow.Networked,
                "The disconnected client must not request an automatically hosted replacement room.");
            Assert.IsNull(PlaySelectionScreen.RequestedLobbyMode);
            Assert.AreEqual(HubEntry.Home, TumpHub.PendingEntry);
            Assert.IsFalse(_round.RoundActive);
            Assert.IsFalse(_match.MatchInProgress);
            Assert.AreEqual(completed, MatchAbandon.MatchWasCompleted);
        }
        [Test] public void LobbyAuthorityRestorationCannotReviveTheAbandonedClock()
        {
            HostLoss(); MatchAbandon.Clear();
            float remaining = _round.TimeLeft;
            typeof(RoundDirector).GetMethod("FixedUpdate", Hidden).Invoke(_round, null);
            Assert.AreEqual(remaining, _round.TimeLeft); Assert.IsFalse(_round.RoundActive);
        }
    }
}
