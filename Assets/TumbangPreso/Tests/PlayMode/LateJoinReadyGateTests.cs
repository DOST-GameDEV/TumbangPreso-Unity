using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class LateJoinReadyGateTests
    {
        private sealed class Peer : INetProvider
        {
            public bool Host;
            public bool IsHost => Host;
            public bool IsNetworked => true;
            public int LocalSlot => Host ? 0 : 1;
            public int LocalPeerId => LocalSlot;
            public bool IsSeatlessReferee => false;
        }

        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private INetProvider _previousProvider;
        private GameObject _root;
        private ReadyGate _gate;
        private CharacterMotor _body;
        private int _starts;

        [UnitySetUp]
        public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _previousProvider = NetAuthority.Provider;
            NetAuthority.Provider = new Peer();
            GameServices.Ensure();
            _starts = 0;
            _root = new GameObject("Late join ready gate");
            var actor = new GameObject("Joining actor", typeof(CharacterController));
            actor.transform.SetParent(_root.transform);
            _body = actor.AddComponent<CharacterMotor>();
            _body.enabled = false;
            _body.PlayerSlot = 1;
            GameServices.Round.Register(_body);
            _gate = _root.AddComponent<ReadyGate>();
            _gate.enabled = false;
            _gate.RoundShouldBegin += () => _starts++;
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _previousProvider;
        }

        private void TickGate() => typeof(ReadyGate).GetMethod("Update", Hidden).Invoke(_gate, null);

        private void ReceiveRunningRound(int number = 1, bool active = true)
        {
            GameServices.Match.ApplySnapshot(new int[Balance.PlayerCount], number, true);
            GameServices.Round.ApplySnapshot(60, active, MatchRules.DefenderSlotFor(number), true);
        }

        private IEnumerator FinishIntroduction()
        {
            _gate.Open(_body);
            float deadline = Time.realtimeSinceStartup + 20;
            var field = typeof(ReadyGate).GetField("_introductionDone", Hidden);
            while (!(bool)field.GetValue(_gate) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsTrue((bool)field.GetValue(_gate), "The real introduction did not finish.");
            Assert.IsTrue(PresentationClock.Held, "The gate must own its waiting hold before the snapshot.");
        }

        [UnityTest]
        public IEnumerator RunningSnapshotReleasesMissedCountdownHoldWithoutStartingAnotherRound()
        {
            yield return FinishIntroduction();
            ReceiveRunningRound();
            TickGate();
            Assert.IsFalse(PresentationClock.Held, "A late join missed the one-time countdown and remains frozen.");
            Assert.IsFalse(_gate.AwaitingReady);
            Assert.IsTrue(_body.CanAct());
            _body.Intent.Parked = false;
            _body.Intent.Move = Vector2.up;
            Vector3 before = _body.transform.position;
            var step = typeof(CharacterMotor).GetMethod("FixedUpdate", Hidden);
            for (int i = 0; i < 20; i++) step.Invoke(_body, null);
            Assert.Greater(_body.transform.position.z - before.z, .25f,
                "The joining owner's real motor must move after the gate releases.");
            Assert.AreEqual(0, _starts, "A joining client must adopt the host round, not start another one.");
            _gate.StartLocalCountdown();
            Assert.IsFalse(_gate.CountingDown, "A delayed countdown must not freeze the live joiner again.");
        }

        [UnityTest]
        public IEnumerator RunningSnapshotCancelsAnIntroductionStillInFlight()
        {
            _gate.Open(_body);
            Assert.IsNotNull(_root.GetComponent<MatchArrivalPresentation>());
            ReceiveRunningRound();
            TickGate();
            yield return null;
            Assert.IsFalse(MatchArrivalPresentation.Active);
            Assert.IsFalse(PresentationClock.Held);
            Assert.IsFalse(_gate.AwaitingReady);
            Assert.AreEqual(0, _starts);
        }

        [UnityTest]
        public IEnumerator RebindingDuringAnExistingRoundDoesNotReplayTheOpening()
        {
            ReceiveRunningRound(3);
            _gate.Open(_body);
            yield return null;
            Assert.IsFalse(_gate.AwaitingReady);
            Assert.IsFalse(MatchArrivalPresentation.Active);
            Assert.IsFalse(PresentationClock.Held);
            Assert.AreEqual(3, GameServices.Match.RoundNumber);
            Assert.AreEqual(0, _starts);
        }

        [UnityTest]
        public IEnumerator PrestartSnapshotDoesNotReleaseTheRealStartBarrier()
        {
            yield return FinishIntroduction();
            GameServices.Match.ApplySnapshot(new int[Balance.PlayerCount], 0, false);
            TickGate();
            Assert.IsTrue(PresentationClock.Held);
            Assert.IsTrue(_gate.AwaitingReady);
            Assert.AreEqual(0, _starts);
        }

        [UnityTest]
        public IEnumerator IntermissionArrivalReleasesOnlyTheOpeningAndKeepsGameplayInactive()
        {
            yield return FinishIntroduction();
            ReceiveRunningRound(3, false);
            TickGate();
            Assert.IsFalse(PresentationClock.Held);
            Assert.IsFalse(_body.RoundActive);
            Assert.IsFalse(_body.CanAct());
            Assert.IsTrue(GameServices.Match.IsWarmupBuffer);
            Assert.AreEqual(0, _starts);
        }

        [UnityTest]
        public IEnumerator AdoptionPreservesRequestedPauseAndDoesNotUnfreezeStatusEffects()
        {
            yield return FinishIntroduction();
            PresentationClock.RequestScale(0);
            ReceiveRunningRound();
            TickGate();
            Assert.IsFalse(PresentationClock.Held);
            Assert.AreEqual(0, Time.timeScale);
            Assert.IsFalse(_body.CanAct());
            PresentationClock.RequestScale(1);
            Assert.IsTrue(_body.CanAct());
            _body.ApplyStagger(2.5f, StunElement.Ice, 0);
            TickGate();
            Assert.IsTrue(_body.IsFrozen);
            Assert.IsFalse(_body.CanAct());
        }

        [UnityTest]
        public IEnumerator AlreadyAdoptedGateLeavesAnotherPresentationHoldAlone()
        {
            ReceiveRunningRound();
            _gate.Open(_body);
            typeof(PresentationClock).GetMethod("Hold", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            try
            {
                TickGate();
                Assert.IsTrue(PresentationClock.Held);
                Assert.AreEqual(0, Time.timeScale);
            }
            finally
            {
                typeof(PresentationClock).GetMethod("Release", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator HostRetainsItsNormalStartBarrier()
        {
            NetAuthority.Provider = new Peer { Host = true };
            yield return FinishIntroduction();
            ReceiveRunningRound();
            TickGate();
            Assert.IsTrue(PresentationClock.Held);
            Assert.AreEqual(0, _starts);
        }

        [UnityTest]
        public IEnumerator ArrivingUltimateKeepsItsOwnClockHoldAfterOpeningRetires()
        {
            yield return FinishIntroduction();
            ReceiveRunningRound();
            var previousRpc = MatchRpc.Instance;
            long previousMatch = GameServices.Match.PresentationMatchId;
            var router = new GameObject("Joining ultimate receiver");
            router.SetActive(false);
            var rpc = router.AddComponent<MatchRpc>();
            var instance = typeof(MatchRpc).GetProperty("Instance");
            var matchId = typeof(MatchDirector).GetProperty("PresentationMatchId");
            var phase = SharedUltimatePhase.Ensure();
            phase.enabled = false;
            try
            {
                instance.SetValue(null, rpc);
                typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(rpc, 123L);
                matchId.SetValue(GameServices.Match, 123L);
                var commits = new[] { new UltimateCommit(1, 1, Vector3.zero, Vector3.forward, Vector3.forward, 0) };
                typeof(SharedUltimatePhase).GetMethod("ReceiveTimed", Hidden).Invoke(phase,
                    new object[] { 123L, 1, 1L, SharedUltimatePhase.Now, 1f, commits, 60f, 4d });
                Assert.IsTrue(phase.Active, "The actual snapshot receiver must accept the incoming phase.");
                TickGate();
                Assert.IsFalse(_gate.AwaitingReady);
                Assert.IsTrue(phase.Active);
                Assert.IsTrue(PresentationClock.Held);
                Assert.AreEqual(0, Time.timeScale);
                Assert.IsFalse(_body.CanAct());
            }
            finally
            {
                phase.Cancel();
                Object.DestroyImmediate(router);
                instance.SetValue(null, previousRpc);
                matchId.SetValue(GameServices.Match, previousMatch);
            }
        }
    }
}
