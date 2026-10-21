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
    public sealed class SeatAssignmentPacketTests
    {
        GameObject _root;
        NetSession _session;
        bool _spectator;
        int _events;
        readonly MethodInfo _receive = typeof(NetSession).GetMethod("OnSeatAssignmentMessage", BindingFlags.Instance | BindingFlags.NonPublic);

        [SetUp]
        public void Before()
        {
            _spectator = GameLaunch.Spectator;
            _root = new GameObject("Seat packet qualification");
            _root.SetActive(false);
            _session = _root.AddComponent<NetSession>();
            Assert.IsNull(_root.GetComponent<NetworkManager>(), "The inactive fixture must not start a session.");
            _session.ApplyAssignedSeat(1);
            _session.SeatingChanged += () => _events++;
            _events = 0;
        }

        [TearDown]
        public void After()
        {
            Object.DestroyImmediate(_root);
            GameLaunch.Spectator = _spectator;
        }

        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 1)]
        [TestCase(5, 2)]
        [TestCase(8, 2)]
        [TestCase(4, -2)]
        [TestCase(4, Balance.PlayerCount)]
        [TestCase(4, int.MaxValue)]
        [TestCase(4, int.MinValue)]
        public void MalformedSeatAssignmentCannotThrowOrChangeTheLocalSeat(int length, int seat)
        {
            var payload = new byte[length];
            var encoded = BitConverter.GetBytes(seat);
            Array.Copy(encoded, payload, Math.Min(encoded.Length, payload.Length));
            Receive(NetworkManager.ServerClientId, payload);
            Assert.AreEqual(1, _session.LocalSlot);
            Assert.IsFalse(GameLaunch.Spectator);
            Assert.AreEqual(0, _events, "Invalid packets must not rebuild the local player's controls.");
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void HostCanAssignEveryPlayerSeatAndTheSpectatorSentinel(int seat)
        {
            Receive(NetworkManager.ServerClientId, BitConverter.GetBytes(seat));
            Assert.AreEqual(seat, _session.LocalSlot);
            Assert.AreEqual(seat == -1, GameLaunch.Spectator);
            Assert.AreEqual(seat == 1 ? 0 : 1, _events);
        }

        [TestCase(0)]
        [TestCase(4)]
        [TestCase(8)]
        public void NonHostCannotAssignASeatEvenWithMalformedPayload(int length)
        {
            Receive(NetworkManager.ServerClientId + 1, new byte[length]);
            Assert.AreEqual(1, _session.LocalSlot);
            Assert.IsFalse(GameLaunch.Spectator);
            Assert.AreEqual(0, _events);
        }

        [Test]
        public void RepeatedValidAssignmentDoesNotRebuildTheSeat()
        {
            Receive(NetworkManager.ServerClientId, BitConverter.GetBytes(2));
            Receive(NetworkManager.ServerClientId, BitConverter.GetBytes(2));
            Assert.AreEqual(2, _session.LocalSlot);
            Assert.AreEqual(1, _events);
        }

        void Receive(ulong sender, byte[] payload)
        {
            using var reader = new FastBufferReader(payload, Allocator.Temp);
            Assert.DoesNotThrow(() => _receive.Invoke(_session, new object[] { sender, reader }));
        }
    }
}
