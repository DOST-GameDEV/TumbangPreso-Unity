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
            int ticks = 0, starts = 0;
            gate.CountdownTick += _ => ticks++; gate.RoundShouldBegin += () => starts++;
            try
            {
                foreach (long match in new long[] { -1, 0, 122, 124 })
                    Deliver(rpc, "OnBeginCountdownMsg", 0, match);
                Deliver(rpc, "OnBeginCountdownMsg", 1, 123);
                Deliver(rpc, "OnBeginCountdownMsg", 0, 123, trailing: true);
                Deliver(rpc, "OnBeginCountdownMsg", 0, 123, truncated: true);
                Assert.IsFalse(gate.CountingDown); Assert.AreEqual(0, ticks);
                Deliver(rpc, "OnBeginCountdownMsg", 0, 123);
                Deliver(rpc, "OnBeginCountdownMsg", 0, 123);
                Assert.IsTrue(gate.CountingDown); Assert.AreEqual(1, ticks);
                float deadline = Time.realtimeSinceStartup + 6;
                while (gate.CountingDown && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsFalse(gate.CountingDown); Assert.AreEqual(1, starts); Assert.AreEqual(4, ticks);
                Deliver(rpc, "OnBeginCountdownMsg", 0, 123); gate.StartLocalCountdown();
                Assert.IsFalse(gate.CountingDown); Assert.AreEqual(1, starts); Assert.AreEqual(4, ticks);
                gate.Open(null); Deliver(rpc, "OnBeginCountdownMsg", 0, 123);
                Assert.IsTrue(gate.CountingDown, "Explicitly reopening the gate must reset its one-time lifecycle.");
            }
            finally { Object.DestroyImmediate(gateRoot); Object.DestroyImmediate(routerRoot); }
        }
    }
}
