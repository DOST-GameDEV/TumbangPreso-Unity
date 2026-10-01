using System;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class ReadyTallyPacketTests
    {
        private sealed class Peer : INetProvider
        {
            public bool Host;
            public bool IsHost => Host;
            public bool IsNetworked => true;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }
        private INetProvider _previous;
        private Peer _peer;
        private GameObject _root;
        private MatchRpc _rpc;
        private int _events, _ready, _expected;
        private readonly MethodInfo _receive = typeof(MatchRpc).GetMethod("OnReadyTallyMsg", BindingFlags.Instance | BindingFlags.NonPublic);

        [SetUp]
        public void Before()
        {
            _previous = NetAuthority.Provider; _peer = new Peer(); NetAuthority.Provider = _peer;
            _root = new GameObject("Ready tally packet qualification"); _root.SetActive(false);
            _rpc = _root.AddComponent<MatchRpc>();
            _events = 0; _ready = _expected = -10;
            MatchRpc.OnLobbyReadyChanged += Observe;
        }
        [TearDown]
        public void After()
        {
            MatchRpc.OnLobbyReadyChanged -= Observe;
            Object.DestroyImmediate(_root); NetAuthority.Provider = _previous;
        }
        private void Observe(int ready, int expected) { _events++; _ready = ready; _expected = expected; }
        private void Receive(ulong sender, int length, int ready = 1, int expected = 3)
        {
            var payload = new byte[length];
            Array.Copy(BitConverter.GetBytes(ready), payload, Math.Min(4, length));
            if (length > 4) Array.Copy(BitConverter.GetBytes(expected), 0, payload, 4, Math.Min(4, length - 4));
            using var reader = new FastBufferReader(payload, Allocator.Temp);
            Assert.DoesNotThrow(() => _receive.Invoke(_rpc, new object[] { sender, reader }));
        }
        [TestCase(0), TestCase(1), TestCase(2), TestCase(3), TestCase(4)]
        [TestCase(5), TestCase(6), TestCase(7), TestCase(9), TestCase(12)]
        public void MalformedLengthCannotThrowOrPublishATally(int length)
        {
            Receive(NetworkManager.ServerClientId, length);
            Assert.AreEqual(0, _events);
        }
        [TestCase(-1, 3), TestCase(0, -1), TestCase(4, 3)]
        [TestCase(0, 5), TestCase(int.MaxValue, 4), TestCase(0, int.MaxValue)]
        public void ImpossibleCountsCannotReachTheLobby(int ready, int expected)
        {
            Receive(NetworkManager.ServerClientId, 8, ready, expected);
            Assert.AreEqual(0, _events);
        }
        [TestCase(0, 0), TestCase(0, 1), TestCase(1, 1)]
        [TestCase(2, 3), TestCase(0, 4), TestCase(4, 4)]
        public void ValidHostCountsIncludeTheEmptyLobbyAndFourSeatedGuests(int ready, int expected)
        {
            Receive(NetworkManager.ServerClientId, 8, ready, expected);
            Assert.AreEqual(1, _events); Assert.AreEqual(ready, _ready); Assert.AreEqual(expected, _expected);
        }
        [TestCase(0), TestCase(8)]
        public void NonHostCannotPublishEvenAMalformedTally(int length)
        {
            Receive(NetworkManager.ServerClientId + 1, length);
            Assert.AreEqual(0, _events);
        }
        [Test]
        public void HostLoopbackDoesNotDuplicateTheLocallyPublishedTally()
        {
            _peer.Host = true; Receive(NetworkManager.ServerClientId, 8);
            Assert.AreEqual(0, _events);
        }
    }
}
