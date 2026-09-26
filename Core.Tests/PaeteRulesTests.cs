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
            float dragged = CarryRules.DistanceFor(PaeteRules.SentryPullSpeedFor(PaeteRules.SentryRadius), pull);
            Assert.True(Math.Abs(dragged - (PaeteRules.SentryRadius - PaeteRules.SentryHoldDistance)) < 1e-3f);
            // Every caught distance stops at the trunk, near or far: none is flung past it.
            foreach (float at in new[] { 2.0f, 2.5f, 3.0f, 4.5f, 6.0f, 9.0f })
            {
                float v = PaeteRules.SentryPullSpeedFor(at);
                float travelled = CarryRules.DistanceFor(v, PaeteRules.SentryPullHoldSeconds(at));
                Assert.True(Math.Abs(travelled - (at - PaeteRules.SentryHoldDistance)) < 1e-2f, $"{at} m travelled {travelled}");
                Assert.True(v <= PaeteRules.SentryPullSpeed + 1e-4f);
                Assert.True(PaeteRules.SentryPullArriveSeconds(at) < 1.5f, $"{at} m arrives too late");
            }
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

        // ⚠️ Owner, 2026-09-27: "dont let it be placed in a place it STANDS on can". A 7 m box half, as the plaza.
        private const float Half = 6.5f;

        private static float Gap(float x, float z, float cx, float cz) => (float)Math.Sqrt((x - cx) * (x - cx) + (z - cz) * (z - cz));

        [Fact]
        public void TheGuardianNeverStandsOnTheCan()
        {
            // Aimed right on the can: pushed back toward the caster, to the clearance.
            PaeteRules.SentrySpotClearOfCan(0f, 0f, 0f, 0f, 0f, -6f, Half, Half, out float x, out float z);
            Assert.True(Math.Abs(Gap(x, z, 0f, 0f) - PaeteRules.SentryCanClearance) < 1e-3f, $"({x}, {z})");
            Assert.True(z < 0f, "pushed toward the caster");
            // Aimed a metre past it: pushed straight on, along the aim, not sideways.
            PaeteRules.SentrySpotClearOfCan(0f, 1f, 0f, 0f, 0f, -6f, Half, Half, out x, out z);
            Assert.True(Math.Abs(x) < 1e-3f && Math.Abs(z - PaeteRules.SentryCanClearance) < 1e-3f, $"({x}, {z})");
            // Aimed clear of it: untouched.
            PaeteRules.SentrySpotClearOfCan(3f, 2f, 0f, 0f, 0f, -6f, Half, Half, out x, out z);
            Assert.Equal(3f, x); Assert.Equal(2f, z);
            // Every spot on a sweep round a centred can ends clear of it and inside the box.
            for (int a = 0; a < 360; a += 15)
                for (float r = 0f; r < PaeteRules.SentryCanClearance; r += 0.3f)
                {
                    float sx = r * (float)Math.Sin(a * Math.PI / 180), sz = r * (float)Math.Cos(a * Math.PI / 180);
                    PaeteRules.SentrySpotClearOfCan(sx, sz, 0f, 0f, 4f, -4f, Half, Half, out x, out z);
                    Assert.True(Gap(x, z, 0f, 0f) >= PaeteRules.SentryCanClearance - 1e-3f, $"{a} deg {r} m -> ({x}, {z})");
                    Assert.InRange(x, -Half, Half); Assert.InRange(z, -Half, Half);
                }
        }

        [Fact]
        public void AGuardianAimedAtACanInTheCornerStillClearsIt()
        {
            // The can knocked into the corner: straight out is off the court, so it goes round the can instead.
            PaeteRules.SentrySpotClearOfCan(6.4f, 6.4f, 6.0f, 6.0f, 0f, 0f, Half, Half, out float x, out float z);
            Assert.True(Gap(x, z, 6f, 6f) >= PaeteRules.SentryCanClearance - 1e-3f, $"({x}, {z})");
            Assert.InRange(x, -Half, Half); Assert.InRange(z, -Half, Half);
            // Deterministic: the same inputs give the same spot, which is what keeps every peer's tree in one place.
            PaeteRules.SentrySpotClearOfCan(6.4f, 6.4f, 6.0f, 6.0f, 0f, 0f, Half, Half, out float x2, out float z2);
            Assert.Equal(x, x2); Assert.Equal(z, z2);
        }

        [Fact]
        public void AHeldBodyIsPressedIntoTheRootsAndClearOfTheCan()
        {
            // The v9 tree's root knuckles are 1.17 m out at shin height (measured off the built glb at 1.3); a body 0.3 m
            // deep held at 1.4 m has its back within a few centimetres of them: pressed into the roots, not standing near.
            Assert.InRange(PaeteRules.SentryHoldDistance - 0.3f - 1.17f, -0.15f, 0.15f);
            // A prisoner held on the can's side of the tree still stands off the can.
            Assert.True(PaeteRules.SentryCanClearance - PaeteRules.SentryHoldDistance > 0.35f + 0.06f);
        }
    }
}
