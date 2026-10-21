using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class CastPreparationPacketBoundsTests
    {
        private sealed class Client : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }
        private INetProvider _previous;
        private GameObject _root;
        private MatchRpc _rpc;
        private static readonly MethodInfo Receive = typeof(MatchRpc).GetMethod(
            "OnCastPreparationMsg", BindingFlags.Instance | BindingFlags.NonPublic);

        [SetUp] public void Before()
        {
            _previous = NetAuthority.Provider;
            NetAuthority.Provider = new Client();
            _root = new GameObject("Dormant cast preparation receiver");
            _root.SetActive(false);
            _rpc = _root.AddComponent<MatchRpc>();
        }
        [TearDown] public void After()
        {
            Object.DestroyImmediate(_root);
            NetAuthority.Provider = _previous;
        }

        private static void WriteFrame(FastBufferWriter writer, string hero = "sean", bool trailing = false)
        {
            writer.WriteValueSafe(123UL); // NGO's already consumed name hash.
            writer.WriteValueSafe(-1); // A departed seat: framing must still be safe.
            writer.WriteValueSafe(1);
            writer.WriteValueSafe(hero);
            writer.WriteValueSafe(0);
            writer.WriteValueSafe(.2f);
            writer.WriteValueSafe(0f);
            writer.WriteValueSafe(1f);
            writer.WriteValueSafe(Vector3.zero);
            writer.WriteValueSafe(Vector3.forward);
            writer.WriteValueSafe(Vector3.one);
            if (trailing) writer.WriteValueSafe((byte)1);
        }

        [TestCase(8)] [TestCase(12)] [TestCase(18)]
        [TestCase(23)] [TestCase(31)] [TestCase(79)]
        public void TruncatedHostPreparationDoesNotThrowOrAdvanceDecode(int bytes)
        {
            using var writer = new FastBufferWriter(128, Allocator.Temp);
            WriteFrame(writer);
            Assert.AreEqual(80, writer.Length);
            using var reader = new FastBufferReader(writer, Allocator.Temp, bytes);
            reader.ReadValueSafe(out ulong envelope);
            Assert.DoesNotThrow(() => Receive.Invoke(_rpc, new object[] { 0UL, reader }));
            Assert.AreEqual(8, reader.Position, "Rejected framing must not partially decode a cast.");
        }

        [Test] public void TrailingBytesRejectTheWholePreparation()
        {
            using var writer = new FastBufferWriter(128, Allocator.Temp);
            WriteFrame(writer, trailing: true);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            reader.ReadValueSafe(out ulong envelope);
            Assert.DoesNotThrow(() => Receive.Invoke(_rpc, new object[] { 0UL, reader }));
            Assert.AreEqual(8, reader.Position);
        }

        [TestCase("sean")] [TestCase("雨")]
        public void CompleteUtf16FrameStillDecodesBeforeDepartedSeatRejection(string hero)
        {
            using var writer = new FastBufferWriter(128, Allocator.Temp);
            WriteFrame(writer, hero);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            reader.ReadValueSafe(out ulong envelope);
            Assert.DoesNotThrow(() => Receive.Invoke(_rpc, new object[] { 0UL, reader }));
            Assert.AreEqual(reader.Length, reader.Position);
        }

        [Test] public void NonHostCannotDecodeEvenAnEmptyPayload()
        {
            using var writer = new FastBufferWriter(8, Allocator.Temp);
            writer.WriteValueSafe(123UL);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            reader.ReadValueSafe(out ulong envelope);
            Assert.DoesNotThrow(() => Receive.Invoke(_rpc, new object[] { 7UL, reader }));
            Assert.AreEqual(8, reader.Position);
        }
    }
}
