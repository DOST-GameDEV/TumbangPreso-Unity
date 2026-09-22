using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class PeerDepartureTests
    {
        private sealed class Client : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => 2;
            public int LocalPeerId => 2;
            public bool IsSeatlessReferee => false;
        }
        private INetProvider _before;
        private GameObject _root;
        private MatchRpc _rpc;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [SetUp] public void Before()
        {
            _before = NetAuthority.Provider; NetAuthority.Provider = new Client();
            _root = new GameObject("Peer departure receiver"); _rpc = _root.AddComponent<MatchRpc>();
            typeof(MatchRpc).GetProperty(nameof(MatchRpc.PresentationMatchId))
                .GetSetMethod(true).Invoke(_rpc, new object[] { 100L });
        }
        [TearDown] public void After()
        { Object.DestroyImmediate(_root); NetAuthority.Provider = _before; }

        private void Deliver(ulong sender, long match, int sequence, int seat, byte reason,
            bool bot = true, string name = "Maya")
        {
            using var writer = new FastBufferWriter(192, Allocator.Temp);
            writer.WriteValueSafe(match); writer.WriteValueSafe(sequence); writer.WriteValueSafe(seat);
            writer.WriteValueSafe(reason); writer.WriteValueSafe(bot);
            writer.WriteValueSafe(name.Length);
            foreach (char character in name) writer.WriteValueSafe((ushort)character);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            typeof(MatchRpc).GetMethod("OnPeerDepartureMsg", Private)
                .Invoke(_rpc, new object[] { sender, reader });
        }

        [Test] public void OnlyFreshHostNoticesReachThePlayerOnce()
        {
            Deliver(9, 100, 1, 1, 1); // A peer cannot announce somebody else's departure.
            Deliver(0, 99, 1, 1, 1); // Nor can the previous match.
            Deliver(0, 100, 1, 9, 1);
            Deliver(0, 100, 1, 1, 2);
            Assert.AreEqual(0, _rpc.PeerDepartureNotices);
            Deliver(0, 100, 1, 1, 1);
            Assert.AreEqual("Maya LEFT · BOT TAKES OVER", _rpc.LastPeerDepartureText);
            Deliver(0, 100, 1, 1, 1);
            Assert.AreEqual(1, _rpc.PeerDepartureNotices);
            Deliver(0, 100, 2, 1, 0, false);
            Assert.AreEqual("Maya DISCONNECTED · SEAT RESERVED", _rpc.LastPeerDepartureText);
            Assert.AreEqual(2, _rpc.PeerDepartureNotices);
        }

        [Test] public void NewTransportClearsNoticeStateAndNamesCannotInjectMarkup()
        {
            Deliver(0, 100, 10, 1, 1, true, "<size=100>\nMaya");
            StringAssert.DoesNotContain("<", _rpc.LastPeerDepartureText);
            StringAssert.DoesNotContain("\n", _rpc.LastPeerDepartureText);
            typeof(MatchRpc).GetMethod("ClearPeerDepartureState", Private).Invoke(_rpc, null);
            Assert.AreEqual(0, _rpc.PeerDepartureNotices);
            Assert.IsEmpty(_rpc.LastPeerDepartureText);
            Deliver(0, 100, 1, 1, 0);
            Assert.AreEqual(1, _rpc.PeerDepartureNotices);
        }
    }
}
