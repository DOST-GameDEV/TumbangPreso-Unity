using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Tests
{
    public sealed class VoodooBodySnapshotTests
    {
        [Test]
        public void EveryPlayerAndCompanionPoseAcceptsItsVoodooTail()
        {
            var state = new VoodooBodySnapshot { MarkSource = -1, ReachTarget = -1 };
            using var writer = new FastBufferWriter(VoodooBodySnapshot.WireBytes, Allocator.Temp);
            writer.WriteNetworkSerializable(state);
            for (int seat = 0; seat < CompanionSeats.BodyCount; seat++)
            {
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                var input = reader;
                Assert.IsTrue(VoodooBodySnapshot.TryRead(ref input, seat, out _),
                    "The voodoo tail discarded the movement packet for body " + seat);
            }
        }

        [Test]
        public void CompanionCanReceiveACurseFromAPlayer()
        {
            var state = new VoodooBodySnapshot
            {
                MarkKind = (byte)VoodooMarkKind.Drain, MarkSource = 1, MarkAge = .2f, ReachTarget = -1
            };
            Assert.IsTrue(state.IsValid(CompanionSeats.For(0)));
            state.MarkSource = CompanionSeats.For(1);
            Assert.IsFalse(state.IsValid(CompanionSeats.For(0)), "A doll cannot claim a player's curse ability.");
        }

        [TestCase(-1)] [TestCase(8)] [TestCase(int.MaxValue)]
        public void OutOfRangeBodiesAreStillRefused(int seat)
        {
            Assert.IsFalse(new VoodooBodySnapshot { MarkSource = -1, ReachTarget = -1 }.IsValid(seat));
        }
    }
}
