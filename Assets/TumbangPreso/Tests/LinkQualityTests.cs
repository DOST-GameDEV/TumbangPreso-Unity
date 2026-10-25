using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Tests
{
    public sealed class LinkQualityTests
    {
        [Test]
        public void CurrentRoundTripsDoNotDependOnAnOldTransportEstimate()
        {
            var trips = new LinkRoundTrips(77);
            uint first = trips.Begin(10);
            Assert.IsTrue(trips.Accept(77, first, 10.270)); Assert.AreEqual(270, trips.RttMs);
            uint next = trips.Begin(11);
            Assert.IsTrue(trips.Accept(77, next, 11.600)); Assert.AreEqual(600, trips.RttMs);
            Assert.AreEqual(.5, trips.ReplyAge(12.1), .0001);
        }
        [Test]
        public void OlderEchoesCannotOverwriteANewerMeasurementOrRefreshItsAge()
        {
            var trips = new LinkRoundTrips(77);
            uint old = trips.Begin(1), fresh = trips.Begin(2);
            Assert.IsTrue(trips.Accept(77, fresh, 2.3));
            Assert.IsFalse(trips.Accept(77, old, 3)); Assert.IsFalse(trips.Accept(77, fresh, 3));
            Assert.AreEqual(300, trips.RttMs); Assert.AreEqual(.7, trips.ReplyAge(3), .0001);
        }
        [Test]
        public void InvalidEchoesDoNotConsumeARealOutstandingMeasurement()
        {
            var trips = new LinkRoundTrips(77); uint serial = trips.Begin(10);
            Assert.IsFalse(trips.Accept(78, serial, 10.2));
            Assert.IsFalse(trips.Accept(77, serial + 1, 10.2));
            Assert.IsFalse(trips.Accept(77, serial, double.NaN));
            Assert.IsFalse(trips.Accept(77, serial, 9));
            Assert.IsFalse(trips.Accept(77, serial, 19));
            Assert.IsTrue(trips.Accept(77, serial, 10.6));
            Assert.IsFalse(new LinkRoundTrips(88).Accept(77, serial, 10.7));
        }
        [Test]
        public void ASlowEchoMaySpanSeveralProbesButTheRingIsBounded()
        {
            var trips = new LinkRoundTrips(77); uint first = trips.Begin(1);
            trips.Begin(2); trips.Begin(3);
            Assert.IsTrue(trips.Accept(77, first, 3.5)); Assert.AreEqual(2500, trips.RttMs);
            for (int i = 4; i <= 10; i++) trips.Begin(i);
            Assert.IsFalse(trips.Accept(77, 2, 10.1));
            Assert.AreEqual(0, trips.Begin(double.PositiveInfinity));
        }
        [TestCase(false)] [TestCase(true)]
        public void FixedSizeVersionedPacketsRoundTrip(bool reply)
        {
            using var writer = new FastBufferWriter(64, Allocator.Temp); var buffer = writer;
            LinkQualityPacket.Write(ref buffer, reply, 77, 123);
            Assert.AreEqual(LinkQualityPacket.Bytes, writer.Length);
            using var reader = new FastBufferReader(writer, Allocator.Temp); var source = reader;
            Assert.IsTrue(LinkQualityPacket.TryRead(ref source, out bool actual, out ulong nonce, out uint serial));
            Assert.AreEqual(reply, actual); Assert.AreEqual(77, nonce); Assert.AreEqual(123, serial);
            for (int bytes = 0; bytes < writer.Length; bytes++)
            {
                using var cut = new FastBufferReader(writer, Allocator.Temp, bytes); var truncated = cut;
                Assert.IsFalse(LinkQualityPacket.TryRead(ref truncated, out _, out _, out _), "Truncation " + bytes);
            }
            buffer.WriteValueSafe((byte)0);
            using var tail = new FastBufferReader(writer, Allocator.Temp); var trailing = tail;
            Assert.IsFalse(LinkQualityPacket.TryRead(ref trailing, out _, out _, out _));
        }
        [TestCase(0u, 0x10, 77ul, 1u)]
        [TestCase(0x514C5054u, 0x12, 77ul, 1u)]
        [TestCase(0x514C5054u, 0x10, 0ul, 1u)]
        [TestCase(0x514C5054u, 0x10, 77ul, 0u)]
        public void UnrelatedOrInvalidPayloadsCannotBecomeQualityPackets(uint magic, int kind, ulong nonce, uint serial)
        {
            using var writer = new FastBufferWriter(32, Allocator.Temp);
            writer.WriteValueSafe(magic); writer.WriteValueSafe((byte)kind); writer.WriteValueSafe(nonce); writer.WriteValueSafe(serial);
            using var reader = new FastBufferReader(writer, Allocator.Temp); var source = reader;
            Assert.IsFalse(LinkQualityPacket.TryRead(ref source, out _, out _, out _));
        }
    }
}
