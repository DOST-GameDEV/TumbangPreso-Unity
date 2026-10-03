using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class ClientMovementEffectPacketBoundsTests
    {
        private sealed class Provider : INetProvider
        {
            public bool Host;
            public bool IsHost => Host;
            public bool IsNetworked => true;
            public int LocalSlot => 0;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }

        private INetProvider _previous;
        private Provider _provider;
        private GameObject _root;
        private MatchRpc _rpc;

        [SetUp] public void Before()
        {
            _previous = NetAuthority.Provider;
            NetAuthority.Provider = _provider = new Provider();
            _root = new GameObject("Dormant client movement receiver");
            _root.SetActive(false);
            _rpc = _root.AddComponent<MatchRpc>();
        }

        [TearDown] public void After()
        {
            Object.DestroyImmediate(_root);
            NetAuthority.Provider = _previous;
        }

        private void Receive(string method, FastBufferReader reader, ulong sender = NetworkManager.ServerClientId)
        {
            var receive = typeof(MatchRpc).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(receive);
            receive.Invoke(_rpc, new object[] { sender, reader });
        }

        private static int PayloadBytes(string method) => method == "OnImpactMsg" ? 20 : method == "OnCarryMsg" ? 24 : 25;

        private static void WriteFrame(FastBufferWriter writer, string method, bool trailing = false)
        {
            writer.WriteValueSafe(123UL); // Consumed NGO message-name envelope, outside this payload.
            writer.WriteValueSafe(-1); // A complete frame still must reject an invalid seat.
            writer.WriteValueSafe(0);
            writer.WriteValueSafe(Vector3.zero);
            if (method != "OnImpactMsg") writer.WriteValueSafe(0f);
            if (method == "OnSyncFamiliarMsg") writer.WriteValueSafe(false);
            if (trailing) writer.WriteValueSafe((byte)1);
        }

        [TestCase("OnImpactMsg", 8)] [TestCase("OnImpactMsg", 27)]
        [TestCase("OnCarryMsg", 8)] [TestCase("OnCarryMsg", 31)]
        [TestCase("OnSyncFamiliarMsg", 8)] [TestCase("OnSyncFamiliarMsg", 32)]
        public void TruncatedPayloadIsRejectedBeforePartialDecode(string method, int length)
        {
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            WriteFrame(writer, method);
            Assert.AreEqual(8 + PayloadBytes(method), writer.Length);
            using var reader = new FastBufferReader(writer, Allocator.Temp, length);
            reader.ReadValueSafe(out ulong envelope);
            Assert.DoesNotThrow(() => Receive(method, reader));
            Assert.AreEqual(8, reader.Position);
        }

        [TestCase("OnImpactMsg")] [TestCase("OnCarryMsg")] [TestCase("OnSyncFamiliarMsg")]
        public void TrailingBytesAreRejectedBeforeDecode(string method)
        {
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            WriteFrame(writer, method, true);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            reader.ReadValueSafe(out ulong envelope);
            Assert.DoesNotThrow(() => Receive(method, reader));
            Assert.AreEqual(8, reader.Position);
        }

        [TestCase("OnImpactMsg")] [TestCase("OnCarryMsg")] [TestCase("OnSyncFamiliarMsg")]
        public void CompleteUnreadPayloadDecodesBeforeSeatRejection(string method)
        {
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            WriteFrame(writer, method);
            Assert.AreEqual(8 + PayloadBytes(method), writer.Length);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            reader.ReadValueSafe(out ulong envelope);
            Assert.DoesNotThrow(() => Receive(method, reader));
            Assert.AreEqual(writer.Length, reader.Position);
        }

        [TestCase("OnImpactMsg", false)] [TestCase("OnCarryMsg", false)] [TestCase("OnSyncFamiliarMsg", false)]
        [TestCase("OnImpactMsg", true)] [TestCase("OnCarryMsg", true)] [TestCase("OnSyncFamiliarMsg", true)]
        public void ForeignSenderAndHostLoopbackNeverDecode(string method, bool host)
        {
            _provider.Host = host;
            using var writer = new FastBufferWriter(8, Allocator.Temp);
            writer.WriteValueSafe(123UL);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            reader.ReadValueSafe(out ulong envelope);
            Assert.DoesNotThrow(() => Receive(method, reader, host ? NetworkManager.ServerClientId : 99UL));
            Assert.AreEqual(8, reader.Position);
        }
    }
}
