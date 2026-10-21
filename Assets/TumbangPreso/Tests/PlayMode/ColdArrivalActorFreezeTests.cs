using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ColdArrivalActorFreezeTests
    {
        private sealed class Client : INetProvider
        {
            public bool IsNetworked => true;
            public bool IsHost => false;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }

        private readonly Dictionary<string, object> _previous = new Dictionary<string, object>();
        private GameObject _root;
        private MatchDirector _match;
        private RoundDirector _round;
        private MatchRpc _rpc, _previousRpc;
        private NetSession _previousNet;
        private SliceRunner _runner;
        private CharacterMotor[] _seats;
        private INetProvider _provider;
        private CustomRules _rules;
        private bool _pinned;
        private float _timeScale;
        private int _ends;

        [UnitySetUp]
        public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _previous.Clear();
            foreach (string name in new[] { "Match", "Round", "Stats", "Telemetry" })
                _previous[name] = typeof(GameServices).GetProperty(name).GetValue(null);
            _provider = NetAuthority.Provider;
            _previousNet = NetSession.Instance; _previousRpc = MatchRpc.Instance;
            _rules = UI.SceneFlow.SelectedRules.Clone(); _pinned = UI.SceneFlow.RulesPinned;
            _timeScale = Time.timeScale; _ends = 0;
            _root = new GameObject("Cold arrival actors");
            var services = new GameObject("Dormant client services");
            services.transform.SetParent(_root.transform); services.SetActive(false);
            _match = services.AddComponent<MatchDirector>();
            _round = services.AddComponent<RoundDirector>();
            _rpc = services.AddComponent<MatchRpc>();
            var net = services.AddComponent<NetSession>();
            SetService("Match", _match); SetService("Round", _round);
            SetService("Stats", null); SetService("Telemetry", null);
            typeof(NetSession).GetProperty("Instance").SetValue(null, net);
            typeof(MatchRpc).GetProperty("Instance").SetValue(null, _rpc);
            NetAuthority.Provider = new Client();
            UI.SceneFlow.AdoptRemoteRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            net.Lobby.MatchInProgress = true;
            _seats = new CharacterMotor[Balance.PlayerCount];
            for (int slot = 0; slot < _seats.Length; slot++)
            {
                var body = new GameObject("Arrival actor " + slot, typeof(CharacterController));
                body.transform.SetParent(_root.transform);
                body.transform.position = new Vector3(slot * 3, 0, 0);
                var motor = body.AddComponent<CharacterMotor>(); motor.PlayerSlot = slot;
                motor.Intent.Move = Vector2.up; motor.Intent.Set(Verb.Sprint, true);
                _seats[slot] = motor; _round.Register(motor);
            }
            var runnerRoot = new GameObject("Unbegun arena runner");
            runnerRoot.transform.SetParent(_root.transform);
            _runner = runnerRoot.AddComponent<SliceRunner>();
            _runner.AutoStart = false; _runner.Seats = _seats;
            _match.MatchEnded += _ => _ends++;
            Assert.IsFalse(_runner.Running);
            Assert.IsFalse(_match.HostConfirmedInProgress);
            AssertFreeRoam();
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            if (_runner != null) Object.DestroyImmediate(_runner);
            if (_root != null) Object.DestroyImmediate(_root);
            foreach (var previous in _previous) SetService(previous.Key, previous.Value);
            typeof(NetSession).GetProperty("Instance").SetValue(null, _previousNet);
            typeof(MatchRpc).GetProperty("Instance").SetValue(null, _previousRpc);
            NetAuthority.Provider = _provider;
            if (_rules != null)
            {
                if (_pinned) UI.SceneFlow.PinSelectedRules(_rules);
                else { UI.SceneFlow.AdoptRemoteRules(_rules); UI.SceneFlow.UnpinSelectedRules(); }
            }
            PresentationClock.RequestScale(_timeScale);
            yield return PlayModeWorld.Reset();
        }

        private static void SetService(string name, object value)
            => typeof(GameServices).GetProperty(name).SetValue(null, value);

        private void Receive(int round)
            => _rpc.SyncWorldSnapshotClientRpc(round, MatchRules.DefenderSlotFor(round), 0,
                new[] { 10, 20, 350, 40 }, false, false);

        private void AssertFreeRoam()
        {
            foreach (var seat in _seats)
            {
                Assert.IsTrue(seat.RoundActive, "Pre-match actor " + seat.PlayerSlot);
                Assert.IsFalse(seat.Intent.Parked);
                Assert.AreEqual(Vector2.up, seat.Intent.MoveAxis);
            }
        }

        [Test]
        public void CompletedSnapshotBeforeBeginFreezesAllFourActors()
        {
            Receive(_match.TotalRounds);
            Assert.AreEqual(1, _ends, "The real completed-arrival transition must have occurred.");
            Assert.IsFalse(_round.RoundActive); Assert.IsFalse(_runner.Running);
            var failures = new List<string>();
            foreach (var seat in _seats)
            {
                if (seat.RoundActive) failures.Add("Actor " + seat.PlayerSlot + " can still act.");
                if (!seat.Intent.Parked) failures.Add("Actor " + seat.PlayerSlot + " was never parked.");
                if (seat.Intent.MoveAxis != Vector2.zero) failures.Add("Actor " + seat.PlayerSlot + " movement was not released.");
                if (seat.Intent.Pressed(Verb.Sprint)) failures.Add("Actor " + seat.PlayerSlot + " is still sprinting.");
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
            Receive(_match.TotalRounds);
            Assert.AreEqual(1, _ends, "Repeated final packets must not replay completion.");
        }

        [Test]
        public void PrestartSnapshotKeepsAllFourActorsAvailableForFreeRoam()
        {
            Receive(0);
            Assert.AreEqual(0, _ends); Assert.IsFalse(_runner.Running);
            AssertFreeRoam();
        }

        [Test]
        public void DestroyedRunnerCannotFreezeActorsFromALaterCompletedSnapshot()
        {
            Object.DestroyImmediate(_runner);
            Receive(_match.TotalRounds);
            Assert.AreEqual(1, _ends);
            AssertFreeRoam();
        }
    }
}
