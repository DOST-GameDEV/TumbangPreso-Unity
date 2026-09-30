using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class SeatRequestPacketTests
    {
        private sealed class Peer : INetProvider
        {
            public bool Host = true;
            public bool IsHost => Host;
            public bool IsNetworked => true;
            public int LocalSlot => 0;
            public int LocalPeerId => 0;
            public bool IsSeatlessReferee => false;
        }
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private INetProvider _previousProvider;
        private object _previousSession;
        private GameObject _root;
        private NetSession _session;
        private MatchRpc _rpc;
        private Peer _peer;
        private HashSet<int> _ready;
        private int _events;
        private readonly MethodInfo _receive = typeof(MatchRpc).GetMethod("OnReqSeatMsg", Hidden);

        [SetUp]
        public void Before()
        {
            _previousProvider = NetAuthority.Provider;
            _previousSession = typeof(NetSession).GetProperty("Instance").GetValue(null);
            _peer = new Peer(); NetAuthority.Provider = _peer;
            _root = new GameObject("Seat request qualification"); _root.SetActive(false);
            _session = _root.AddComponent<NetSession>(); _rpc = _root.AddComponent<MatchRpc>();
            typeof(NetSession).GetProperty("Instance").SetValue(null, _session);
            _session.Lobby.OpenLobby(new System.Random(29));
            _session.Lobby.Admit(0, "host", "Host"); _session.Lobby.Admit(1, "guest", "Guest");
            Assert.AreEqual(0, _session.Lobby.PeerById(0).Seat); Assert.AreEqual(1, _session.Lobby.PeerById(1).Seat);
            _ready = (HashSet<int>)typeof(MatchRpc).GetField("_lobbyReady", Hidden).GetValue(_rpc);
            _ready.Add(1); _events = 0; MatchRpc.OnLobbyReadyChanged += Observe;
        }
        [TearDown]
        public void After()
        {
            MatchRpc.OnLobbyReadyChanged -= Observe;
            typeof(NetSession).GetProperty("Instance").SetValue(null, _previousSession);
            Object.DestroyImmediate(_root); NetAuthority.Provider = _previousProvider;
        }
        private void Observe(int ready, int expected) => _events++;
        private void Receive(ulong sender, int seat, int length = 4)
        {
            var payload = new byte[length]; Array.Copy(BitConverter.GetBytes(seat), payload, Math.Min(4, length));
            using var reader = new FastBufferReader(payload, Allocator.Temp);
            Assert.DoesNotThrow(() => _receive.Invoke(_rpc, new object[] { sender, reader }));
        }
        private void Unchanged()
        {
            Assert.AreEqual(0, _session.Lobby.PeerById(0).Seat);
            Assert.AreEqual(1, _session.Lobby.PeerById(1).Seat);
            Assert.IsTrue(_ready.Contains(1)); Assert.AreEqual(0, _events);
        }
        [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(5), TestCase(8)]
        public void MalformedRequestsCannotThrowOrMoveAReadyGuest(int length)
        {
            Receive(1, 2, length); Unchanged();
        }
        [TestCase(4294967296UL), TestCase(4294967297UL), TestCase(ulong.MaxValue)]
        public void WideTransportIdsCannotWrapIntoTheHostOrGuest(ulong sender)
        {
            Receive(sender, 2); Unchanged();
        }
        [Test]
        public void RepeatingTheCurrentSeatPreservesReadyAndDoesNotPublishAChange()
        {
            Receive(1, 1); Receive(1, 1); Unchanged();
        }
        [TestCase(2), TestCase(-1)]
        public void AnActualSeatChangeClearsOnlyTheChangingGuestsReady(int seat)
        {
            Receive(1, seat);
            Assert.AreEqual(seat, _session.Lobby.PeerById(1).Seat);
            Assert.AreEqual(seat < 0, _session.Lobby.PeerById(1).Spectator);
            Assert.IsFalse(_ready.Contains(1)); Assert.AreEqual(0, _session.Lobby.PeerById(0).Seat);
            Assert.AreEqual(1, _events);
        }
        [TestCase(-2), TestCase(4)]
        public void InvalidSeatsCannotChangeReadiness(int seat) { Receive(1, seat); Unchanged(); }
        [Test]
        public void OccupiedSeatCannotDisplaceItsOwner() { Receive(1, 0); Unchanged(); }
        [Test]
        public void LiveMatchCannotChangeSeats() { _session.Lobby.MatchInProgress = true; Receive(1, 2); Unchanged(); }
        [Test]
        public void AClientCannotHandleSeatRequests() { _peer.Host = false; Receive(1, 2, 0); Unchanged(); }
    }
}
