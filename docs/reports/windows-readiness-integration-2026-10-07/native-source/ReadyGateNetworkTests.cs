using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ReadyGateNetworkTests
    {
        private sealed class Peer : INetProvider
        {
            public bool Host;
            public bool IsHost => Host;
            public bool IsNetworked => true;
            public int LocalSlot => Host ? 0 : 1;
            public int LocalPeerId => Host ? 0 : 1;
            public bool IsSeatlessReferee => false;
        }

        private INetProvider _provider;
        private CustomRules _rules;
        private bool _pinned;
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        [UnitySetUp] public IEnumerator Before()
        {
            _provider = NetAuthority.Provider;
            _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
            var rules = CustomGameRules.Defaults(GameMode.HeroStrike); rules.ManualReady = true;
            SceneFlow.PinSelectedRules(rules);
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _provider;
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        private static void Deliver(MatchRpc rpc, string method, ulong sender, long match,
                                    byte? vote = null, bool trailing = false, bool truncated = false)
        {
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            if (!truncated) writer.WriteValueSafe(match);
            if (vote.HasValue) writer.WriteValueSafe(vote.Value);
            if (trailing) writer.WriteValueSafe((byte)0);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            typeof(MatchRpc).GetMethod(method, Hidden).Invoke(rpc, new object[] { sender, reader });
        }

        [Test]
        public void RematchRotatesWorldIdentityOnceAndRejectsOldAnnouncements()
        {
            GameServices.Ensure(); NetAuthority.Provider = new Peer { Host = true };
            var match = GameServices.Match; match.ApplySnapshot(new int[4], 8, false);
            typeof(MatchDirector).GetProperty("PresentationMatchId").SetValue(match, 123L);
            var root = new GameObject("Rematch identity"); root.SetActive(false);
            var rpc = root.AddComponent<MatchRpc>();
            var identity = typeof(MatchRpc).GetProperty("PresentationMatchId"); identity.SetValue(rpc, 123L);
            try
            {
                rpc.BeginRematchClientRpc(); long next = rpc.PresentationMatchId;
                Assert.Greater(next, 123);
                Assert.IsTrue((bool)typeof(MatchRpc).GetField("_loadingOwnArena", Hidden).GetValue(rpc));
                rpc.BeginRematchClientRpc(); Assert.AreEqual(next, rpc.PresentationMatchId);
                Assert.IsFalse(new GameplayActionScope { Match = 123, Round = 1, Epoch = 0 }.Matches(next, 1, 0),
                    "A prior match's first-round/body-zero packet matched the rematch.");

                NetAuthority.Provider = new Peer(); identity.SetValue(rpc, 123L);
                var adopt = typeof(MatchRpc).GetMethod("AdoptRematchIdentity", Hidden);
                bool Accept(long previous, long target) => (bool)adopt.Invoke(rpc, new object[] { previous, target });
                Assert.IsFalse(Accept(122, next)); Assert.IsFalse(Accept(123, 123)); Assert.IsFalse(Accept(0, next));
                Assert.IsTrue(Accept(123, next)); Assert.AreEqual(next, match.PresentationMatchId);
                Assert.IsFalse(Accept(123, next)); Assert.IsFalse(Accept(next, 123));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void RematchVotesRequireCurrentMembershipAndAcknowledgeTheLocalSeat()
        {
            GameServices.Ensure();
            var match = GameServices.Match; match.ApplySnapshot(new int[4], 8, false);
            typeof(MatchDirector).GetProperty("PresentationMatchId").SetValue(match, 123L);
            var sessionRoot = new GameObject("Rematch session"); sessionRoot.SetActive(false);
            var session = sessionRoot.AddComponent<NetSession>();
            var sessionInstance = typeof(NetSession).GetProperty("Instance"); var previous = sessionInstance.GetValue(null);
            var root = new GameObject("Rematch membership"); var result = root.AddComponent<MatchResult>(); result.enabled = false;
            var canvas = (Canvas)typeof(MatchResult).GetField("_canvas", Hidden).GetValue(result);
            canvas.gameObject.SetActive(true);
            var pending = typeof(MatchResult).GetField("_rematchPending", Hidden);
            try
            {
                NetAuthority.Provider = new Peer { Host = true }; sessionInstance.SetValue(null, session);
                session.Lobby.OpenLobby(new System.Random(42));
                session.Lobby.Admit(0, "host", "Host");
                var guest = session.Lobby.Admit(1, "guest", "Guest");
                var watcher = session.Lobby.Admit(2, "watch", "Watch"); watcher.Spectator = true; watcher.Seat = -1;
                session.Lobby.Admit(3, "third", "Third");
                result.HostReceiveVote(2); result.HostReceiveVote(99); Assert.AreEqual(0, result.VoteCount);
                result.HostReceiveVote(1); result.HostReceiveVote(1); Assert.AreEqual(1, result.VoteCount);
                guest.Spectator = true; guest.Seat = -1;
                result.HostReceiveVote(0);
                Assert.AreEqual(1, result.VoteCount, "A withdrawn participant's old vote survived.");
                Assert.AreEqual(2, result.ExpectedVotes()); Assert.IsTrue(result.IsVisible);

                guest.Spectator = false; guest.Seat = 1; NetAuthority.Provider = new Peer();
                pending.SetValue(result, true);
                result.ApplyRematchTally(1, 3, 1); Assert.IsTrue((bool)pending.GetValue(result));
                result.ApplyRematchTally(1, 3, 2); Assert.IsFalse((bool)pending.GetValue(result));
                Assert.AreEqual(MatchResult.TallyLine(1, 3), result.TallyText);
                result.RequestRematch(); Assert.IsFalse((bool)pending.GetValue(result), "An acknowledged vote was requeued.");
                Assert.AreEqual(MatchResult.TallyLine(1, 3), result.TallyText, "A local press erased the received tally.");
            }
            finally
            {
                sessionInstance.SetValue(null, previous);
                Object.DestroyImmediate(root); Object.DestroyImmediate(sessionRoot);
            }
        }

        [Test]
        public void BufferVotesAcceptSeatedClientsAndRejectOldBreaks()
        {
            GameServices.Ensure();
            var match = GameServices.Match; match.ApplySnapshot(new int[4], 3, true); match.IsWarmupBuffer = true;
            typeof(MatchDirector).GetProperty("PresentationMatchId").SetValue(match, 123L);
            typeof(MatchDirector).GetProperty("SkipRequested").SetValue(match, false);
            var sessionRoot = new GameObject("Buffer session"); sessionRoot.SetActive(false);
            var session = sessionRoot.AddComponent<NetSession>(); var rpc = sessionRoot.AddComponent<MatchRpc>();
            typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(rpc, 123L);
            var sessionInstance = typeof(NetSession).GetProperty("Instance"); var previous = sessionInstance.GetValue(null);
            var root = new GameObject("Buffer ballot"); var ballot = root.AddComponent<BufferSkipVote>(); ballot.enabled = false;
            var receive = typeof(MatchRpc).GetMethod("OnSkipBufferMsg", Hidden);
            void Vote(ulong sender, long identity = 123, int round = 3, bool trailing = false)
            {
                using var writer = new FastBufferWriter(16, Allocator.Temp);
                writer.WriteValueSafe(identity); writer.WriteValueSafe(round);
                if (trailing) writer.WriteValueSafe((byte)0);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                receive.Invoke(rpc, new object[] { sender, reader });
            }
            try
            {
                NetAuthority.Provider = new Peer { Host = true }; sessionInstance.SetValue(null, session);
                session.Lobby.OpenLobby(new System.Random(42));
                session.Lobby.Admit(0, "host", "Host"); session.Lobby.Admit(1, "guest", "Guest");
                var watcher = session.Lobby.Admit(2, "watch", "Watch"); watcher.Spectator = true; watcher.Seat = -1;
                var seatless = session.Lobby.Admit(3, "none", "None"); seatless.Seat = -1;
                Vote(1);
                Assert.AreEqual(1, BufferSkipVote.Votes, "The host discarded a real client vote.");
                Assert.AreEqual(2, BufferSkipVote.VotesNeeded); Assert.IsFalse(match.SkipRequested);
                Vote(1); Vote(2); Vote(3); Vote(99); Vote(ulong.MaxValue);
                Vote(0, 122); Vote(0, round: 2); Vote(0, trailing: true);
                Assert.AreEqual(1, BufferSkipVote.Votes); Assert.IsFalse(match.SkipRequested);
                Vote(0); Assert.AreEqual(2, BufferSkipVote.Votes); Assert.IsTrue(match.SkipRequested);

                match.ApplySnapshot(new int[4], 4, true); match.IsWarmupBuffer = true;
                typeof(MatchDirector).GetProperty("SkipRequested").SetValue(match, false);
                typeof(BufferSkipVote).GetMethod("Update", Hidden).Invoke(ballot, null);
                Vote(1); Assert.AreEqual(0, BufferSkipVote.Votes); Assert.IsFalse(match.SkipRequested);
                Vote(1, round: 4); Assert.AreEqual(1, BufferSkipVote.Votes); Assert.IsFalse(match.SkipRequested);
                session.Lobby.Depart(0); ballot.OnPeerLeft(0);
                Assert.IsTrue(match.SkipRequested, "The remaining valid quorum was not reevaluated on departure.");
            }
            finally { sessionInstance.SetValue(null, previous); Object.DestroyImmediate(root); Object.DestroyImmediate(sessionRoot); }
        }

        [Test]
        public void BufferObserverMirrorsTallyAndAcknowledgementWithoutRoundEvents()
        {
            GameServices.Ensure(); NetAuthority.Provider = new Peer();
            var match = GameServices.Match; match.ApplySnapshot(new int[4], 3, true);
            typeof(MatchDirector).GetProperty("PresentationMatchId").SetValue(match, 123L);
            var root = new GameObject("Observer ballot"); var ballot = root.AddComponent<BufferSkipVote>(); ballot.enabled = false;
            var routerRoot = new GameObject("Observer ballot receiver"); routerRoot.SetActive(false);
            var rpc = routerRoot.AddComponent<MatchRpc>(); typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(rpc, 123L);
            var boundary = typeof(MatchRpc).GetMethod("ApplyNetworkRoundBoundary", BindingFlags.Static | BindingFlags.NonPublic);
            var receive = typeof(MatchRpc).GetMethod("OnBufferVotesMsg", Hidden);
            var update = typeof(BufferSkipVote).GetMethod("Update", Hidden);
            var pending = typeof(BufferSkipVote).GetField("_sendPending", Hidden);
            int events = 0;
            System.Action<int, int> observed = (_, __) => events++;
            match.IntermissionStarted += observed;
            void Tally(int votes, int needed, byte mask, ulong sender = 0, long identity = 123, int round = 3, bool trailing = false)
            {
                using var writer = new FastBufferWriter(32, Allocator.Temp);
                writer.WriteValueSafe(identity); writer.WriteValueSafe(round);
                writer.WriteValueSafe(votes); writer.WriteValueSafe(needed); writer.WriteValueSafe(mask);
                if (trailing) writer.WriteValueSafe((byte)0);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                receive.Invoke(rpc, new object[] { sender, reader });
            }
            try
            {
                boundary.Invoke(null, new object[] { true, false, true, 3 });
                Assert.IsTrue(match.IsWarmupBuffer); Assert.AreEqual(0, events);
                update.Invoke(ballot, null); pending.SetValue(ballot, true);
                Tally(1, 2, 1); Assert.AreEqual(1, BufferSkipVote.Votes); Assert.AreEqual(2, BufferSkipVote.VotesNeeded);
                Assert.IsTrue((bool)pending.GetValue(ballot));
                Tally(1, 2, 2); Assert.IsFalse((bool)pending.GetValue(ballot));
                update.Invoke(ballot, null); Assert.AreEqual(1, BufferSkipVote.Votes, "Client Update erased the host tally.");
                Tally(2, 2, 3, sender: 1); Tally(2, 2, 3, identity: 122); Tally(2, 2, 3, round: 2);
                Tally(2, 2, 1); Tally(1, 5, 2); Tally(2, 2, 3, trailing: true);
                Assert.AreEqual(1, BufferSkipVote.Votes);
                boundary.Invoke(null, new object[] { false, true, true, 4 });
                update.Invoke(ballot, null); Assert.IsFalse(match.IsWarmupBuffer); Assert.IsFalse(BufferSkipVote.Showing);
                Assert.AreEqual(0, events, "Client state mirroring raised an authoritative intermission event.");
            }
            finally
            {
                match.IntermissionStarted -= observed;
                Object.DestroyImmediate(root); Object.DestroyImmediate(routerRoot);
            }
        }

        [Test]
        public void ReadyVotesRequireCurrentMatchSeatedSendersAndLoadedHost()
        {
            var sessionRoot = new GameObject("Ready session"); sessionRoot.SetActive(false);
            var session = sessionRoot.AddComponent<NetSession>();
            var rpc = sessionRoot.AddComponent<MatchRpc>();
            var sessionInstance = typeof(NetSession).GetProperty("Instance");
            var previousSession = sessionInstance.GetValue(null);
            var loadingRoot = new GameObject("Ready loading"); loadingRoot.SetActive(false);
            var loading = loadingRoot.AddComponent<HubLoading>();
            var loadingInstance = typeof(HubLoading).GetField("_current", BindingFlags.Static | BindingFlags.NonPublic);
            var previousLoading = loadingInstance.GetValue(null);
            var gateRoot = new GameObject("Ready gate"); var gate = gateRoot.AddComponent<ReadyGate>();
            gate.enabled = false;
            try
            {
                NetAuthority.Provider = new Peer { Host = true }; sessionInstance.SetValue(null, session);
                typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(rpc, 123L);
                var lobby = session.Lobby; lobby.OpenLobby(new System.Random(42));
                lobby.Admit(0, "host", "Host"); lobby.Admit(1, "guest", "Guest");
                var watcher = lobby.Admit(2, "watch", "Watch"); watcher.Spectator = true; watcher.Seat = -1;
                var seatless = lobby.Admit(3, "seatless", "Seatless"); seatless.Seat = -1;
                Assert.AreEqual(2, lobby.SeatedPeerCount());
                Assert.IsFalse(lobby.IsSeatedPeer(2)); Assert.IsFalse(lobby.IsSeatedPeer(3));
                gate.Open(null); gate.OpenNetworked();
                int ready = 0, expected = 0;
                gate.NetReadyChanged += (count, total) => { ready = count; expected = total; };
                loadingInstance.SetValue(null, loading);
                foreach (ulong sender in new ulong[] { 2, 3, 99, ulong.MaxValue })
                    Deliver(rpc, "OnDeclareReadyMsg", sender, 123, 1);
                foreach (long match in new long[] { -1, 0, 122, 124 })
                    Deliver(rpc, "OnDeclareReadyMsg", 1, match, 1);
                Deliver(rpc, "OnDeclareReadyMsg", 1, 123, 2);
                Deliver(rpc, "OnDeclareReadyMsg", 1, 123, 1, trailing: true);
                Deliver(rpc, "OnDeclareReadyMsg", 1, 123, 1, truncated: true);
                Assert.AreEqual(0, ready);
                Deliver(rpc, "OnDeclareReadyMsg", 0, 123, 1);
                Deliver(rpc, "OnDeclareReadyMsg", 1, 123, 0); Assert.AreEqual(1, ready);
                Deliver(rpc, "OnDeclareReadyMsg", 1, 123, 1);
                Deliver(rpc, "OnDeclareReadyMsg", 1, 123, 1);
                Assert.AreEqual(2, ready); Assert.AreEqual(2, expected);
                Assert.IsFalse(gate.CountingDown, "Readied peers started under the host's loading curtain.");
                typeof(ReadyGate).GetField("_readySendPending", Hidden).SetValue(gate, true);
                typeof(ReadyGate).GetMethod("Update", Hidden).Invoke(gate, null);
                Assert.IsFalse(gate.CountingDown);
                loadingInstance.SetValue(null, previousLoading);
                typeof(ReadyGate).GetMethod("Update", Hidden).Invoke(gate, null);
                Assert.IsFalse(gate.CountingDown, "Loading completion cannot skip the court introduction.");
                typeof(ReadyGate).GetField("_introductionDone", Hidden).SetValue(gate, true);
                typeof(ReadyGate).GetMethod("Update", Hidden).Invoke(gate, null);
                Assert.IsTrue(gate.CountingDown, "Stored valid votes were lost when loading finished.");
                Assert.IsFalse((bool)typeof(ReadyGate).GetField("_readySendPending", Hidden).GetValue(gate));

                Object.DestroyImmediate(gateRoot);
                var lobbyReady = (System.Collections.Generic.HashSet<int>)typeof(MatchRpc)
                    .GetField("_lobbyReady", Hidden).GetValue(rpc);
                Deliver(rpc, "OnDeclareReadyMsg", 1, 123, 1); Assert.IsEmpty(lobbyReady);
                lobby.MatchInProgress = true;
                Deliver(rpc, "OnDeclareReadyMsg", 1, 0, 1); Assert.IsEmpty(lobbyReady);
                lobby.MatchInProgress = false;
                Deliver(rpc, "OnDeclareReadyMsg", 1, 0, 1); Assert.IsTrue(lobbyReady.Contains(1));
            }
            finally
            {
                loadingInstance.SetValue(null, previousLoading); sessionInstance.SetValue(null, previousSession);
                Object.DestroyImmediate(gateRoot); Object.DestroyImmediate(loadingRoot); Object.DestroyImmediate(sessionRoot);
            }
        }

        [UnityTest]
        public IEnumerator ScopedCountdownRejectsOldWorldAndRemainsConsumedAfterCompletion()
        {
            NetAuthority.Provider = new Peer();
            var routerRoot = new GameObject("Ready receiver"); routerRoot.SetActive(false);
            var rpc = routerRoot.AddComponent<MatchRpc>();
            typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(rpc, 123L);
            var gateRoot = new GameObject("Client ready"); var gate = gateRoot.AddComponent<ReadyGate>();
            gate.enabled = false; gate.Open(null);
            var ticks = new System.Collections.Generic.List<string>(); int starts = 0;
            gate.CountdownTick += ticks.Add; gate.RoundShouldBegin += () => starts++;
            try
            {
                foreach (long match in new long[] { -1, 0, 122, 124 })
                    Deliver(rpc, "OnBeginCountdownMsg", 0, match);
                Deliver(rpc, "OnBeginCountdownMsg", 1, 123);
                Deliver(rpc, "OnBeginCountdownMsg", 0, 123, trailing: true);
                Deliver(rpc, "OnBeginCountdownMsg", 0, 123, truncated: true);
                Assert.IsFalse(gate.CountingDown); Assert.AreEqual(0, ticks.Count);
                Deliver(rpc, "OnBeginCountdownMsg", 0, 123);
                Deliver(rpc, "OnBeginCountdownMsg", 0, 123);
                Assert.IsTrue(gate.CountingDown); Assert.AreEqual(1, ticks.Count);
                float deadline = Time.realtimeSinceStartup + 6;
                while (gate.CountingDown && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsFalse(gate.CountingDown); Assert.AreEqual(1, starts); CollectionAssert.AreEqual(new[] { "3", "2", "1", "GO!" }, ticks);
                Deliver(rpc, "OnBeginCountdownMsg", 0, 123); gate.StartLocalCountdown();
                Assert.IsFalse(gate.CountingDown); Assert.AreEqual(1, starts); CollectionAssert.AreEqual(new[] { "3", "2", "1", "GO!" }, ticks);
                gate.Open(null); Deliver(rpc, "OnBeginCountdownMsg", 0, 123);
                Assert.IsTrue(gate.CountingDown, "Explicitly reopening the gate must reset its one-time lifecycle.");
            }
            finally { Object.DestroyImmediate(gateRoot); Object.DestroyImmediate(routerRoot); }
        }
    }
}
