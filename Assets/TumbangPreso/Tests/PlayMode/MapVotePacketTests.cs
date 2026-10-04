using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using TumbangPreso.UI;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class MapVotePacketTests
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
        private INetProvider _oldProvider;
        private bool _oldBots;
        private object _oldSession;
        private GameObject _root, _resultRoot;
        private NetSession _session;
        private MatchRpc _rpc;
        private MatchResult _result;
        private Scene _oldScene, _preparation;

        [UnitySetUp]
        public IEnumerator Before()
        {
            _oldProvider = NetAuthority.Provider;
            _oldBots = AIController.BotsEnabled; AIController.BotsEnabled = true;
            yield return PlayModeWorld.Reset();
            _oldScene = SceneManager.GetActiveScene();
            _preparation = SceneManager.CreateScene(SceneFlow.MatchSetup);
            SceneManager.SetActiveScene(_preparation);
            NetAuthority.Provider = new Peer { Host = true };
            _root = new GameObject("Map packet session"); _root.SetActive(false);
            _session = _root.AddComponent<NetSession>();
            _rpc = _root.AddComponent<MatchRpc>();
            var instance = typeof(NetSession).GetProperty("Instance");
            _oldSession = instance.GetValue(null); instance.SetValue(null, _session);
            _session.Lobby.OpenLobby(new System.Random(17));
            _session.Lobby.Admit(0, "host", "Host");
            _session.Lobby.Admit(1, "guest", "Guest");
            _resultRoot = new GameObject("Map ballot");
            _result = _resultRoot.AddComponent<MatchResult>(); _result.enabled = false;
            // The test creates a hidden result board without its normal Show
            // lifecycle. Establish the same empty ballot before packet delivery.
            for (int i = 0; i < Ballot.Length; i++) Ballot[i] = -1;
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            typeof(NetSession).GetProperty("Instance").SetValue(null, _oldSession);
            Object.DestroyImmediate(_resultRoot); Object.DestroyImmediate(_root);
            SceneManager.SetActiveScene(_oldScene);
            yield return SceneManager.UnloadSceneAsync(_preparation);
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _oldProvider; AIController.BotsEnabled = _oldBots;
        }

        private void Deliver(string name, ulong sender, FastBufferWriter writer)
        {
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            typeof(MatchRpc).GetMethod(name, Hidden).Invoke(_rpc, new object[] { sender, reader });
        }
        private int[] Ballot => (int[])typeof(MatchResult).GetField("_mapVotes", Hidden).GetValue(_result);
        private void ObserveQueue()
        {
            NetAuthority.Provider = new Peer();
            typeof(MatchRpc).GetField("_queueMapVoting", Hidden).SetValue(_rpc, true);
        }

        [TestCase(0), TestCase(1), TestCase(3)]
        public void TruncatedGuestVoteIsIgnoredWithoutAReaderException(int bytes)
        {
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            for (int i = 0; i < bytes; i++) writer.WriteValueSafe((byte)0);
            Assert.DoesNotThrow(() => Deliver("OnSelectMapVoteMsg", 1, writer));
            Assert.That(Ballot[1], Is.EqualTo(-1));
        }

        [Test]
        public void TrailingGuestDataCannotCastABallot()
        {
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(2); writer.WriteValueSafe((byte)1);
            Deliver("OnSelectMapVoteMsg", 1, writer);
            Assert.That(Ballot[1], Is.EqualTo(-1));
        }

        [Test]
        public void OversizedSenderCannotWrapIntoAnExistingGuest()
        {
            using var writer = new FastBufferWriter(8, Allocator.Temp);
            writer.WriteValueSafe(2);
            Deliver("OnSelectMapVoteMsg", (1UL << 32) + 1, writer);
            Assert.That(Ballot[1], Is.EqualTo(-1));
        }

        [Test]
        public void ExactGuestVoteUsesItsAuthenticatedSeat()
        {
            using var writer = new FastBufferWriter(8, Allocator.Temp);
            writer.WriteValueSafe(2);
            Deliver("OnSelectMapVoteMsg", 1, writer);
            Assert.That(Ballot[1], Is.EqualTo(2));
            Assert.That(Ballot[0], Is.EqualTo(-1));
        }

        [TestCase(false), TestCase(true)]
        public void TruncatedHostTallyCannotThrow(bool header)
        {
            ObserveQueue();
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            if (header) { writer.WriteValueSafe(2); writer.WriteValueSafe(1); }
            Assert.DoesNotThrow(() => Deliver("OnMapVoteTallyMsg", NetworkManager.ServerClientId, writer));
            Assert.That(_rpc.QueueMapVoteFor(0), Is.EqualTo(-1));
        }

        [TestCase(false), TestCase(true)]
        public void InvalidOrTrailingTallyCannotPartiallyReplaceTheBallot(bool trailing)
        {
            ObserveQueue();
            using var writer = new FastBufferWriter(32, Allocator.Temp);
            writer.WriteValueSafe(2); writer.WriteValueSafe(1);
            writer.WriteValueSafe(trailing ? 2 : 999);
            if (trailing) writer.WriteValueSafe((byte)1);
            Deliver("OnMapVoteTallyMsg", NetworkManager.ServerClientId, writer);
            Assert.That(_rpc.QueueMapVoteFor(0), Is.EqualTo(-1));
            Assert.That(_rpc.QueueMapVoteFor(1), Is.EqualTo(-1));
        }

        [Test]
        public void ExactHostTallyStillReplacesTheBallot()
        {
            ObserveQueue();
            using var writer = new FastBufferWriter(32, Allocator.Temp);
            writer.WriteValueSafe(2); writer.WriteValueSafe(1); writer.WriteValueSafe(2);
            Deliver("OnMapVoteTallyMsg", NetworkManager.ServerClientId, writer);
            Assert.That(_rpc.QueueMapVoteFor(0), Is.EqualTo(1));
            Assert.That(_rpc.QueueMapVoteFor(1), Is.EqualTo(2));
            Assert.That(_rpc.QueueMapVoteFor(2), Is.EqualTo(-1));
        }

        [Test]
        public void CustomStartSelectsCharactersBeforeOpeningTheBallot()
        {
            _rpc.HostBeginCharacterSelection();
            Assert.IsTrue(_rpc.CharacterSelecting); Assert.IsFalse(_rpc.QueueMapVoting);
            Assert.That(_rpc.CharacterSelectSecondsLeft, Is.InRange(29f, 30f));
            _rpc.HostBeginCharacterSelection();
            var previous = SceneFlow.SelectedRules.MapVote;
            SceneFlow.SelectedRules.MapVote = true;
            try { _rpc.HostCompleteCharacterSelection(); }
            finally { SceneFlow.SelectedRules.MapVote = previous; }
            Assert.IsFalse(_rpc.CharacterSelecting); Assert.IsTrue(_rpc.QueueMapVoting);
            Assert.That(_rpc.QueueMapSecondsLeft, Is.InRange(11f, 12f));
        }

        [Test]
        public void MissingRequiredPlayerCancelsSelectionInsteadOfStrandingTheRoom()
        {
            bool bots = AIController.BotsEnabled;
            try
            {
                AIController.BotsEnabled = false;
                _rpc.HostBeginCharacterSelection(); Assert.IsTrue(_rpc.CharacterSelecting);
                _rpc.HostCompleteCharacterSelection();
                Assert.IsFalse(_rpc.CharacterSelecting); Assert.IsFalse(_rpc.QueueMapVoting);
                Assert.IsFalse(_session.Lobby.MatchInProgress);
            }
            finally { AIController.BotsEnabled = bots; }
        }

        [TestCase(0, true), TestCase(1, false)]
        public void ObserverReceivesPreparationPhaseWithoutAcceptingGuestsOrOlderPhases(int phase, bool selecting)
        {
            NetAuthority.Provider = new Peer();
            void State(ulong sender, int serial, int stage)
            {
                using var writer = new FastBufferWriter(32, Allocator.Temp);
                writer.WriteValueSafe(serial); writer.WriteValueSafe(8f);
                writer.WriteValueSafe(stage); writer.WriteValueSafe(-1);
                for (int i = 0; i < 4; i++) writer.WriteValueSafe(-1);
                Deliver("OnQueueVoteStateMsg", sender, writer);
            }
            State(99, 4, phase);
            Assert.IsFalse(_rpc.CharacterSelecting); Assert.IsFalse(_rpc.QueueMapVoting);
            State(NetworkManager.ServerClientId, 4, phase);
            Assert.AreEqual(selecting, _rpc.CharacterSelecting);
            Assert.AreEqual(!selecting, _rpc.QueueMapVoting);
            State(NetworkManager.ServerClientId, 3, 1-phase);
            Assert.AreEqual(selecting, _rpc.CharacterSelecting);
            Assert.AreEqual(!selecting, _rpc.QueueMapVoting);
        }

        [Test]
        public void HostCancellationClearsTheObserversPreparationPhase()
        {
            ObserveQueue();
            using var writer = new FastBufferWriter(32, Allocator.Temp);
            writer.WriteValueSafe(4); writer.WriteValueSafe(0f); writer.WriteValueSafe(2); writer.WriteValueSafe(-1);
            for (int i = 0; i < 4; i++) writer.WriteValueSafe(-1);
            Deliver("OnQueueVoteStateMsg", NetworkManager.ServerClientId, writer);
            Assert.IsFalse(_rpc.QueueMapVoting); Assert.IsFalse(_rpc.CharacterSelecting);
        }

        [TestCase(false), TestCase(true)]
        public void QueueStateRequiresExactlyItsPublishedWireLength(bool trailing)
        {
            ObserveQueue();
            using var writer = new FastBufferWriter(40, Allocator.Temp);
            writer.WriteValueSafe(1); writer.WriteValueSafe(8f); writer.WriteValueSafe(1); writer.WriteValueSafe(-1);
            for (int i = 0; i < 4; i++) writer.WriteValueSafe(i);
            if (trailing) writer.WriteValueSafe((byte)1);
            Deliver("OnQueueVoteStateMsg", NetworkManager.ServerClientId, writer);
            Assert.That(_rpc.QueueMapVoteFor(0), Is.EqualTo(trailing ? -1 : 0));
        }
    }
}
