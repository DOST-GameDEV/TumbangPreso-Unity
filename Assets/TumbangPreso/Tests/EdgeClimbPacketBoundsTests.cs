using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class EdgeClimbPacketBoundsTests
    {
        private sealed class Provider : INetProvider
        {
            public bool Host = true;
            public bool IsHost => Host;
            public bool IsNetworked => true;
            public int LocalSlot => 0;
            public int LocalPeerId => 0;
            public bool IsSeatlessReferee => false;
        }
        private INetProvider _previous;
        private Provider _provider;
        private GameObject _root;
        private MatchRpc _rpc;
        private static readonly MethodInfo Receive = typeof(MatchRpc).GetMethod(
            "OnReqEdgeClimbMsg", BindingFlags.Instance | BindingFlags.NonPublic);
        [SetUp] public void Before()
        {
            _previous = NetAuthority.Provider;
            NetAuthority.Provider = _provider = new Provider();
            _root = new GameObject("Dormant edge request receiver");
            _root.SetActive(false);
            _rpc = _root.AddComponent<MatchRpc>();
        }
        [TearDown] public void After()
        { Object.DestroyImmediate(_root); NetAuthority.Provider = _previous; }
        private static void WriteFrame(FastBufferWriter writer, bool trailing = false)
        {
            writer.WriteValueSafe(123UL); // NGO's consumed name hash is outside this payload.
            writer.WriteValueSafe(-1); // Cannot own a valid seat or mutate a body's recovery.
            writer.WriteValueSafe(1);
            if (trailing) writer.WriteValueSafe((byte)1);
        }
        [TestCase(8)] [TestCase(12)] [TestCase(15)]
        public void TruncatedClimbRequestIsRejectedWithoutPartialDecode(int length)
        {
            using var writer = new FastBufferWriter(32, Allocator.Temp);
            WriteFrame(writer);
            Assert.AreEqual(16, writer.Length);
            using var reader = new FastBufferReader(writer, Allocator.Temp, length);
            reader.ReadValueSafe(out ulong envelope);
            Assert.DoesNotThrow(() => Receive.Invoke(_rpc, new object[] { 7UL, reader }));
            Assert.AreEqual(8, reader.Position);
        }
        [Test] public void TrailingBytesCannotDecodeAClimbRequest()
        {
            using var writer = new FastBufferWriter(32, Allocator.Temp);
            WriteFrame(writer, true);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            reader.ReadValueSafe(out ulong envelope);
            Assert.DoesNotThrow(() => Receive.Invoke(_rpc, new object[] { 7UL, reader }));
            Assert.AreEqual(8, reader.Position);
        }
        [Test] public void CompletePayloadStillDecodesBeforeSeatOwnershipRejection()
        {
            using var writer = new FastBufferWriter(32, Allocator.Temp);
            WriteFrame(writer);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            reader.ReadValueSafe(out ulong envelope);
            Assert.DoesNotThrow(() => Receive.Invoke(_rpc, new object[] { 7UL, reader }));
            Assert.AreEqual(16, reader.Position);
        }
        [Test] public void AClientNeverDecodesAnEmptyClimbRequest()
        {
            _provider.Host = false;
            using var writer = new FastBufferWriter(8, Allocator.Temp);
            writer.WriteValueSafe(123UL);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            reader.ReadValueSafe(out ulong envelope);
            Assert.DoesNotThrow(() => Receive.Invoke(_rpc, new object[] { 7UL, reader }));
            Assert.AreEqual(8, reader.Position);
        }
    }
}
