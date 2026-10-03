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
        private LobbySeatSwapRequests Swaps => (LobbySeatSwapRequests)typeof(MatchRpc).GetField("_seatSwaps", Hidden).GetValue(_rpc);
        private LobbySeatSwapRequest PendingSwap()
        {
            var pending = (Dictionary<long,LobbySeatSwapRequest>)typeof(LobbySeatSwapRequests).GetField("_pending", Hidden).GetValue(Swaps);
            foreach (var request in pending.Values) return request;
            return null;
        }
        private void Reply(ulong sender, long id, bool accept, int length = 9)
        {
            var bytes = new byte[length]; Array.Copy(BitConverter.GetBytes(id), bytes, Math.Min(8,length));
            if(length>8)bytes[8]=accept?(byte)1:(byte)0;
            using var reader=new FastBufferReader(bytes,Allocator.Temp);
            Assert.DoesNotThrow(()=>typeof(MatchRpc).GetMethod("OnSeatSwapReplyMsg",Hidden).Invoke(_rpc,new object[]{sender,reader}));
        }
        [Test] public void HumanRequestWaitsForRecipientThenClearsBothReadyStates()
        {
            _ready.Add(0); Receive(1,0); Unchanged();
            var request=PendingSwap(); Assert.IsNotNull(request);
            Reply(1,request.Id,true);Unchanged();Assert.AreEqual(1,Swaps.Count);
            Reply(0,request.Id,true);
            Assert.AreEqual(1,_session.Lobby.PeerById(0).Seat);Assert.AreEqual(0,_session.Lobby.PeerById(1).Seat);
            Assert.IsFalse(_ready.Contains(0));Assert.IsFalse(_ready.Contains(1));Assert.AreEqual(1,_events);
            Reply(0,request.Id,true);Assert.AreEqual(1,_events);Assert.AreEqual(0,Swaps.Count);
        }
        [Test] public void RecipientDeclineKeepsSeatsAndReadiness()
        {
            Receive(1,0);var request=PendingSwap();Reply(0,request.Id,false);Unchanged();Assert.AreEqual(0,Swaps.Count);
        }
        [TestCase(0),TestCase(8),TestCase(10)] public void MalformedSwapRepliesDoNotConsumeConsent(int length)
        {
            Receive(1,0);var request=PendingSwap();Reply(0,request.Id,true,length);Unchanged();Assert.AreEqual(1,Swaps.Count);
        }
        [Test] public void WideSwapSenderCannotImpersonateTheRecipient()
        {
            Receive(1,0);var request=PendingSwap();Reply(4294967296UL,request.Id,true);Unchanged();Assert.AreEqual(1,Swaps.Count);
        }

        private void Offer(ulong sender, long id, int from, int to, float seconds, bool trailing = false)
        {
            using var writer=new FastBufferWriter(256,Allocator.Temp);
            writer.WriteValueSafe(id);writer.WriteValueSafe(from);writer.WriteValueSafe(to);writer.WriteValueSafe(true);
            writer.WriteValueSafe(seconds);writer.WriteValueSafe("Guest");if(trailing)writer.WriteValueSafe((byte)5);
            using var reader=new FastBufferReader(writer,Allocator.Temp);
            Assert.DoesNotThrow(()=>typeof(MatchRpc).GetMethod("OnSeatSwapOfferMsg",Hidden).Invoke(_rpc,new object[]{sender,reader}));
        }
        [Test] public void ClientOfferAndEndPacketsRequireHostAndKeepTheCurrentRequest()
        {
            _peer.Host=false;Offer(1,31,1,0,20);Assert.IsNull(_rpc.SeatSwapOffer);
            Offer(0,31,1,0,20);Assert.AreEqual(31,_rpc.SeatSwapOffer.Id);
            Assert.IsTrue(_rpc.SeatSwapOffer.Incoming);Assert.AreEqual("Guest",_rpc.SeatSwapOffer.RequesterName);
            Offer(0,30,2,0,20);Assert.AreEqual(31,_rpc.SeatSwapOffer.Id);
            using var writer=new FastBufferWriter(16,Allocator.Temp);writer.WriteValueSafe(31L);writer.WriteValueSafe((byte)1);
            var method=typeof(MatchRpc).GetMethod("OnSeatSwapEndMsg",Hidden);
            using(var reader=new FastBufferReader(writer,Allocator.Temp))method.Invoke(_rpc,new object[]{1UL,reader});
            Assert.IsNotNull(_rpc.SeatSwapOffer);
            using(var reader=new FastBufferReader(writer,Allocator.Temp))method.Invoke(_rpc,new object[]{0UL,reader});
            Assert.IsNull(_rpc.SeatSwapOffer);Assert.AreEqual("Seats switched.",_rpc.SeatSwapResult);
        }
        [Test] public void InvalidClientOffersCannotReplaceTheCurrentConsentPrompt()
        {
            _peer.Host=false;Offer(0,31,1,0,20);Assert.IsNotNull(_rpc.SeatSwapOffer);
            Offer(0,32,1,0,float.NaN);Offer(0,32,1,0,21);Offer(0,32,1,0,0);
            Offer(0,32,1,1,20);Offer(0,32,4,0,20);Offer(0,32,1,0,20,true);
            Assert.AreEqual(31,_rpc.SeatSwapOffer.Id);
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
