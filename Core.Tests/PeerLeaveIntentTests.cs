using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class PeerLeaveIntentTests
    {
        [Fact] public void OnlyTheDepartingPeerConsumesItsOwnFreshIntentOnce()
        {
            var intents = new PeerLeaveIntents();
            Assert.True(intents.Remember(7, 100, 4));
            Assert.False(intents.Consume(8, 100, 5));
            Assert.Equal(1, intents.Count);
            Assert.True(intents.Consume(7, 100, 5));
            Assert.False(intents.Consume(7, 100, 5));
        }

        [Fact] public void AnOldMatchOrExpiredIntentCannotLabelALaterDropAsALeave()
        {
            var intents = new PeerLeaveIntents();
            intents.Remember(7, 100, 4);
            Assert.False(intents.Consume(7, 101, 5));
            intents.Remember(7, 101, 4);
            Assert.False(intents.Consume(7, 101, 4 + PeerLeaveIntents.LifetimeSeconds + .01));
            Assert.Equal(0, intents.Count);
        }

        [Fact] public void InvalidOrRewoundClocksNeverConfirmAnIntent()
        {
            var intents = new PeerLeaveIntents();
            Assert.False(intents.Remember(7, 100, double.NaN));
            Assert.False(intents.Remember(-1, 100, 1));
            Assert.False(intents.Remember(7, 0, 1));
            intents.Remember(7, 100, 4);
            Assert.False(intents.Consume(7, 100, 3));
            intents.Remember(7, 100, 4);
            Assert.False(intents.Consume(7, 100, double.PositiveInfinity));
        }

        [Fact] public void ANewTransportCannotInheritIntentForAReusedPeerId()
        {
            var intents = new PeerLeaveIntents();
            intents.Remember(7, 100, 4);
            intents.Clear();
            Assert.Equal(0, intents.Count);
            Assert.False(intents.Consume(7, 100, 5));
        }

        [Fact] public void StorageIsBoundedAndExpiredClaimsMakeRoom()
        {
            var intents = new PeerLeaveIntents();
            for (int i = 0; i < Balance.PlayerCount; i++) Assert.True(intents.Remember(i, 100, 1));
            Assert.False(intents.Remember(100, 100, 2));
            Assert.True(intents.Remember(0, 100, 2));
            Assert.Equal(Balance.PlayerCount, intents.Count);
            Assert.True(intents.Remember(100, 100, 20));
            Assert.Equal(1, intents.Count);
        }
    }
}
