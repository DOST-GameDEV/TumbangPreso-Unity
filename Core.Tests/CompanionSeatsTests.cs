using Xunit;
using TumbangPreso.Core;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The companion seats (HERO-10 v3, Phaister's VOODOO DOLL, owner 2026-09-28: *"Own slipper, throws"*): one seat per body,
    /// four players then one companion each, and a companion's points are its owner's (*"The doll gives points gained to
    /// Phaister"*).
    /// </summary>
    public class CompanionSeatsTests
    {
        [Fact]
        public void EachPlayerOwnsTheSeatFourAboveTheirs()
        {
            for (int owner = 0; owner < Balance.PlayerCount; owner++)
            {
                int seat = CompanionSeats.For(owner);
                Assert.Equal(Balance.PlayerCount + owner, seat);
                Assert.True(CompanionSeats.IsCompanion(seat));
                Assert.False(CompanionSeats.IsPlayer(seat));
                Assert.Equal(owner, CompanionSeats.OwnerOf(seat));
            }
            Assert.Equal(-1, CompanionSeats.For(-1));
            Assert.Equal(-1, CompanionSeats.For(Balance.PlayerCount));
        }

        [Fact]
        public void PlayersKeepTheirOwnPointsAndNothingElseScores()
        {
            for (int seat = 0; seat < Balance.PlayerCount; seat++)
            {
                Assert.True(CompanionSeats.IsPlayer(seat));
                Assert.True(CompanionSeats.IsBody(seat));
                Assert.Equal(seat, CompanionSeats.OwnerOf(seat));
            }
            Assert.Equal(-1, CompanionSeats.OwnerOf(-1));
            Assert.Equal(-1, CompanionSeats.OwnerOf(CompanionSeats.BodyCount));
            Assert.False(CompanionSeats.IsBody(CompanionSeats.BodyCount));
            Assert.Equal(8, CompanionSeats.BodyCount);
        }
    }
}
