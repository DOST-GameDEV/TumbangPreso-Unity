using System;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// Paete's kit numbers against the owner's answers (2026-09-25,
    /// `docs/reports/paete-kit-2026-09-25/plan.md` § 7) and against the carry arithmetic they move
    /// bodies with.
    /// </summary>
    public class PaeteRulesTests
    {
        [Fact]
        public void TheOwnersNumbersAreTheOnesInTheKit()
        {
            Assert.Equal(9.0f, PaeteRules.SentryRadius);            // "WITHIN 9 meters"
            Assert.Equal(7.0f, PaeteRules.BreakFreeHoldSeconds);    // "hold for like 7 seconds"
            Assert.Equal(16.0f, PaeteRules.SentryCost);             // "yes" to 16
            Assert.Equal(15.0f, PaeteRules.PlantRootedSeconds);     // "invincible for the first 15 seconds"
            Assert.InRange(PaeteRules.PlantReloadSeconds, 10.0f, 20.0f); // "a cooldown of 10-20 seconds"
        }

        [Fact]
        public void AWoodenSlipperKnockdownIsWorthLessThanARealOne()
        {
            // "maybe lessened plus": more than nothing, less than a knockdown.
            int sprout = MatchRules.PointsFor(ScoreEvent.SproutKnock);
            Assert.True(sprout > 0);
            Assert.True(sprout < MatchRules.PointsFor(ScoreEvent.LataKnocked));
            Assert.Equal(PaeteRules.SproutKnockPoints, sprout);
        }

        [Fact]
        public void MovesStayUnderTheSingleImpulseCapAndCoverTheirDistance()
        {
            Assert.True(PaeteRules.VineReelSpeed <= Balance.MaxKnockbackSpeed);
            Assert.True(PaeteRules.SentryPullSpeed <= Balance.MaxKnockbackSpeed);

            // A reel to an anchor at full range stops VineStopShort before it.
            float hold = PaeteRules.VineHoldSeconds(PaeteRules.VineRange);
            float covered = CarryRules.DistanceFor(PaeteRules.VineReelSpeed, hold);
            Assert.True(Math.Abs(covered - (PaeteRules.VineRange - PaeteRules.VineStopShort)) < 1e-3f);

            // A body pulled from the sentry's rim ends against the sentry.
            float pull = PaeteRules.SentryPullHoldSeconds(PaeteRules.SentryRadius);
            float dragged = CarryRules.DistanceFor(PaeteRules.SentryPullSpeed, pull);
            Assert.True(Math.Abs(dragged - (PaeteRules.SentryRadius - PaeteRules.SentryHoldDistance)) < 1e-3f);
        }

        [Fact]
        public void APlantCannotOutliveItsWelcome()
        {
            // Rooted first, then pullable, then gone on its own; never a whole round of free throws.
            Assert.True(PaeteRules.PlantRootedSeconds < PaeteRules.PlantLifeSeconds);
            int shots = 1 + (int)((PaeteRules.PlantLifeSeconds - PaeteRules.PlantFirstShotSeconds) / PaeteRules.PlantReloadSeconds);
            Assert.True(shots <= 3, $"{shots} wooden throws from one plant.");
        }

        [Fact]
        public void TheSentryFreesEveryoneBeforeARoundEnds()
        {
            Assert.True(PaeteRules.BreakFreeHoldSeconds < PaeteRules.SentryLifeSeconds);
            Assert.True(PaeteRules.SentryLifeSeconds < Balance.RoundTime * 0.25f);
        }
    }
}
