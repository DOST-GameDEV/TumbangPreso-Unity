using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class ActionChainTests
    {
        [Fact]
        public void IndependentAccuracyCapsAndMilestonesDoNotChangeBaseScore()
        {
            var chains = new ActionChains();
            int[] bonuses = { 0, 10, 20, 25, 25, 25 };
            for (int i = 1; i <= 6; i++)
            {
                Assert.True(chains.BeginThrow(1, i, i * 2));
                var result = chains.ResolveThrow(1, i, ThrowChainEnd.Hit, i * 2);
                Assert.Equal(i, result.Count); Assert.Equal(bonuses[i - 1], result.Bonus);
                Assert.Equal(i == 3 ? ChainMilestone.AccurateThree : i == 5 ? ChainMilestone.AccurateFive : ChainMilestone.None, result.Milestone);
                Assert.True(chains.BeginThrow(2, i, i * 2 + 1));
                chains.ResolveThrow(2, i, ThrowChainEnd.Hit, i * 2 + 1);
                Assert.Equal(i, chains.AccuracyFor(1));
            }
        }
        [Fact]
        public void DuplicateThrowCycleAndOutOfOrderResolutionCannotAwardAgain()
        {
            var c = new ActionChains();
            Assert.True(c.BeginThrow(1, 1, 0)); Assert.False(c.BeginThrow(1, 2, 0));
            Assert.False(c.ResolveThrow(1, 2, ThrowChainEnd.Hit, 0).Applied);
            Assert.True(c.ResolveThrow(1, 1, ThrowChainEnd.Hit, 0).Applied);
            Assert.False(c.ResolveThrow(1, 1, ThrowChainEnd.Hit, 0).Applied);
            Assert.False(c.BeginThrow(1, 1, 0));
            Assert.True(c.BeginThrow(2, 1, 0));
            Assert.False(c.ResolveThrow(2, 1, ThrowChainEnd.Hit, 0).Applied);
            Assert.True(c.ResolveThrow(2, 1, ThrowChainEnd.NoContest, 0, true).Applied);
        }
        [Fact]
        public void MissAndBlockResetButConsumedFlightDoesNotAndLaterHitStillCounts()
        {
            var c = new ActionChains();
            c.BeginThrow(1, 1, 0); c.ResolveThrow(1, 1, ThrowChainEnd.Hit, 0);
            c.BeginThrow(1, 2, 1);
            Assert.False(c.ResolveThrow(1, 2, ThrowChainEnd.NoContest, 1).Applied);
            Assert.Equal(1, c.ResolveThrow(1, 2, ThrowChainEnd.NoContest, 1, true).Count);
            c.BeginThrow(1, 3, 1);
            Assert.Equal(2, c.ResolveThrow(1, 3, ThrowChainEnd.Hit, 2, true).Count);
            c.BeginThrow(1, 4, 3);
            Assert.Equal(0, c.ResolveThrow(1, 4, ThrowChainEnd.Block, 3, true).Count);
            c.BeginThrow(1, 5, 4); c.ResolveThrow(1, 5, ThrowChainEnd.Hit, 4);
            c.BeginThrow(1, 6, 5);
            Assert.Equal(0, c.ResolveThrow(1, 6, ThrowChainEnd.Miss, 5).Count);
        }
        [Fact]
        public void TagBreaksPendingAccuracyAndRoundClearsAllState()
        {
            var c = new ActionChains();
            c.BeginThrow(1, 1, 0); c.ResolveThrow(1, 1, ThrowChainEnd.Hit, 0); c.BeginThrow(1, 2, 1);
            c.AcceptedTag(0, 1, 1, 1);
            Assert.Equal(0, c.AccuracyFor(1)); Assert.False(c.ResolveThrow(1, 2, ThrowChainEnd.Hit, 1).Applied);
            c.ResetRound();
            Assert.True(c.BeginThrow(1, 1, 0)); Assert.Equal(1, c.AcceptedTag(0, 1, 1, 0).Count);
        }
        [Fact]
        public void CatchUsesDistinctVictimsAndDoesNotRefreshOnRepeat()
        {
            var c = new ActionChains();
            Assert.Equal(0, c.AcceptedTag(0, 1, 1, 0).Bonus);
            Assert.False(c.AcceptedTag(0, 1, 2, 7).Applied);
            Assert.Equal(1, c.AcceptedTag(0, 2, 3, 9).Count);
            Assert.Equal(10, c.AcceptedTag(0, 1, 4, 10).Bonus);
            var last = c.AcceptedTag(0, 3, 5, 11);
            Assert.Equal(3, last.Count); Assert.Equal(25, last.Bonus); Assert.Equal(ChainMilestone.TripleCatch, last.Milestone);
            Assert.False(c.AcceptedTag(0, 3, 5, 11).Applied);
        }
        [Fact]
        public void CatchWindowIncludesBoundaryAndRejectsInvalidTimeAndIdentity()
        {
            var c = new ActionChains();
            c.AcceptedTag(0, 1, 1, 0);
            Assert.Equal(2, c.AcceptedTag(0, 2, 2, 8).Count);
            Assert.False(c.AcceptedTag(0, 3, 3, double.NaN).Applied);
            Assert.False(c.AcceptedTag(0, 3, 3, 7).Applied);
            Assert.Equal(3, c.AcceptedTag(0, 3, 3, 8).Count);
            Assert.False(c.AcceptedTag(0, 0, 4, 9).Applied);
            Assert.False(c.AcceptedTag(4, 0, 4, 9).Applied);
        }
    }
}
