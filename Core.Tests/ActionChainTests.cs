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
            int[] bonuses = { 0, 0, 0, 0, 0, 0 };
            for (int i = 1; i <= 6; i++)
            {
                Assert.True(chains.BeginThrow(1, i, i * 2));
                var result = chains.ResolveThrow(1, i, ThrowChainEnd.Hit, i * 2);
                Assert.Equal(i, result.Count); Assert.Equal(bonuses[i - 1], result.Bonus);
                Assert.Equal(ChainMilestone.None, result.Milestone);
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
        public void AcceptedCatchesRefreshFiveSecondsAndPermitLegalRepeatVictims()
        {
            var c = new ActionChains();
            Assert.Equal(ChainMilestone.SingleCatch, c.AcceptedTag(0, 1, 1, 0).Milestone);
            Assert.Equal(25, c.AcceptedTag(0, 2, 2, 4).Bonus);
            Assert.Equal(ChainMilestone.TripleCatch, c.AcceptedTag(0, 3, 3, 8).Milestone);
            var fourth = c.AcceptedTag(0, 1, 4, 12);
            Assert.Equal(4, fourth.Count); Assert.Equal(25, fourth.Bonus); Assert.Equal(ChainMilestone.MultiCatch, fourth.Milestone);
            Assert.False(c.AcceptedTag(0, 1, 4, 12).Applied);
            Assert.Equal(1, c.AcceptedTag(0, 2, 5, 17.001).Count);
        }
        [Fact]
        public void CatchWindowIncludesBoundaryAndRejectsInvalidTimeAndIdentity()
        {
            var c = new ActionChains();
            c.AcceptedTag(0, 1, 1, 0);
            Assert.Equal(2, c.AcceptedTag(0, 2, 2, 5).Count);
            Assert.False(c.AcceptedTag(0, 3, 3, double.NaN).Applied);
            Assert.False(c.AcceptedTag(0, 3, 3, 4).Applied);
            Assert.Equal(3, c.AcceptedTag(0, 3, 3, 5).Count);
            Assert.False(c.AcceptedTag(0, 0, 4, 6).Applied);
            Assert.False(c.AcceptedTag(4, 0, 4, 6).Applied);
        }
        [Fact]
        public void KnockdownStreakPaysFromThreeAndResetsOnTagWhileCanDownBreaksCatches()
        {
            var c = new ActionChains();
            Assert.Equal(0,c.AcceptedKnockdown(1,1).Bonus);
            Assert.Equal(0,c.AcceptedKnockdown(1,2).Bonus);
            c.BeginThrow(1,1,2); c.ResolveThrow(1,1,ThrowChainEnd.Miss,2);
            Assert.Equal(50,c.AcceptedKnockdown(1,3).Bonus);
            Assert.False(c.AcceptedKnockdown(2,3).Applied);
            Assert.Equal(50,c.AcceptedKnockdown(1,4).Bonus);
            c.AcceptedTag(0,1,1,0);
            Assert.Equal(1,c.AcceptedKnockdown(1,5).Count);
            Assert.Equal(1,c.AcceptedTag(0,2,2,1).Count);
            c.ResetCatchChains();
            Assert.Equal(1,c.AcceptedTag(0,3,3,2).Count);
            c.ResetRound(); Assert.Equal(1,c.AcceptedKnockdown(1,1).Count);
        }
        [Fact]
        public void NewBonusAndMomentValuesRemainExplicitAndSerializable()
        {
            Assert.Equal(50,MatchRules.PointsFor(ScoreEvent.FirstKnockdownBonus));
            Assert.Equal(50,MatchRules.PointsFor(ScoreEvent.LateKnockdownBonus));
            Assert.Equal(50,MatchRules.PointsFor(ScoreEvent.MultiKnockdownBonus));
            Assert.Equal(25,MatchRules.PointsFor(ScoreEvent.DoubleCatch));
            Assert.Equal(25,MatchRules.PointsFor(ScoreEvent.TripleCatch));
            Assert.Equal(25,MatchRules.PointsFor(ScoreEvent.MultiCatch));
            Assert.True(new MatchMoment(1,1,1,1,MatchMomentKind.MultiKnockdown,3,50).IsValid);
            Assert.True(new MatchMoment(1,2,1,0,MatchMomentKind.MultiCatch,4,25).IsValid);
            Assert.False(new MatchMoment(1,3,1,0,MatchMomentKind.MultiCatch,4,999).IsValid);
        }
    }
}
