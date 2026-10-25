using System;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// Zack's speedster kit against the owner's V3 sheet (2026-10-25, `docs/reports/zack-kit-2026-10-25/plan.md`) and the
    /// pure rules every peer computes alike: Momentum's ramp and reset, the Backtrack window, the copies' fan, the wire's sag
    /// and its crossing test, and the redefined Zapped.
    /// </summary>
    public class ZackRulesTests
    {
        [Fact]
        public void TheOwnersNumbersAreTheOnesInTheKit()
        {
            // "up to 20% faster after 3 seconds"
            Assert.Equal(0.20f, ZackRules.MomentumMaxBonus);
            Assert.Equal(3.0f, ZackRules.MomentumRampSeconds);
            // Backtrack: "3 seconds ago", 30 s; Overclocked "recharges in 3 seconds".
            Assert.Equal(3.0f, ZackRules.BacktrackSeconds);
            Assert.Equal(30.0f, ZackRules.BacktrackCooldown);
            Assert.Equal(3.0f, ZackRules.BacktrackOverclockedCooldown);
            // Afterimage: "for 3 seconds", 30 s; Overclocked "sends two copies".
            Assert.Equal(3.0f, ZackRules.AfterimageSeconds);
            Assert.Equal(30.0f, ZackRules.AfterimageCooldown);
            Assert.Equal(1, ZackRules.AfterimageCopies(false));
            Assert.Equal(2, ZackRules.AfterimageCopies(true));
            // Live Wire: "up to 6 metres apart", "Lasts 10 seconds", 30 s.
            Assert.Equal(6.0f, ZackRules.LiveWireMaxLength);
            Assert.Equal(10.0f, ZackRules.LiveWireSeconds);
            Assert.Equal(30.0f, ZackRules.LiveWireCooldown);
            // Overclock: 15 points, 25 seconds, "Moves 40% faster".
            Assert.Equal(15.0f, ZackRules.OverclockCost);
            Assert.Equal(25.0f, ZackRules.OverclockSeconds);
            Assert.Equal(1.4f, ZackRules.OverclockSpeedScale);
            // Zapped: "Cannot move or act for 1 second."
            Assert.Equal(1.0f, StatusRules.ZappedSeconds);
        }

        [Fact]
        public void MomentumRampsToTwentyPercentOverThreeSecondsInThreeVisibleSteps()
        {
            Assert.Equal(1.0f, ZackRules.MomentumScale(0.0f));
            Assert.Equal(1.1f, ZackRules.MomentumScale(1.5f), 4);
            Assert.Equal(1.2f, ZackRules.MomentumScale(3.0f), 4);
            Assert.Equal(1.2f, ZackRules.MomentumScale(30.0f), 4);
            Assert.Equal(1.0f, ZackRules.MomentumScale(float.NaN));
            Assert.Equal(0, ZackRules.MomentumStep(0.99f));
            Assert.Equal(1, ZackRules.MomentumStep(1.0f));
            Assert.Equal(2, ZackRules.MomentumStep(2.5f));
            Assert.Equal(3, ZackRules.MomentumStep(3.0f));
        }

        [Fact]
        public void MomentumBuildsWhileRunningAndResetsOnlyOnARealStop()
        {
            float stopped = 0.0f, seconds = 0.0f;
            for (int i = 0; i < 400; i++) seconds = ZackRules.StepMomentum(seconds, ref stopped, true, 0.01f);
            Assert.Equal(ZackRules.MomentumRampSeconds, seconds, 3);
            // One slow footstep (0.1 s under the run speed) keeps it.
            for (int i = 0; i < 10; i++) seconds = ZackRules.StepMomentum(seconds, ref stopped, false, 0.01f);
            Assert.Equal(ZackRules.MomentumRampSeconds, seconds, 3);
            seconds = ZackRules.StepMomentum(seconds, ref stopped, true, 0.01f);
            Assert.Equal(0.0f, stopped);
            // A real stop (past the 0.15 s grace) resets it.
            for (int i = 0; i < 20; i++) seconds = ZackRules.StepMomentum(seconds, ref stopped, false, 0.01f);
            Assert.Equal(0.0f, seconds);
        }

        [Fact]
        public void BacktrackLooksThreeSecondsBackOrToTheOldestPointThisRound()
        {
            Assert.Equal(3.0f, ZackRules.BacktrackLookBack(4.0f));
            Assert.Equal(1.2f, ZackRules.BacktrackLookBack(1.2f), 4);
            Assert.True(ZackRules.BacktrackLookBack(0.3f) < 0.0f);
            Assert.True(ZackRules.BacktrackLookBack(float.NaN) < 0.0f);
            Assert.Equal(30.0f, ZackRules.BacktrackCooldownFor(false));
            Assert.Equal(3.0f, ZackRules.BacktrackCooldownFor(true));
            // The host keeps enough history to check the window, and the window contains the owner's point.
            Assert.True(ZackRules.BacktrackHistoryKeep >= ZackRules.BacktrackWindowLate);
            Assert.True(ZackRules.BacktrackWindowEarly < ZackRules.BacktrackSeconds && ZackRules.BacktrackSeconds < ZackRules.BacktrackWindowLate);
        }

        [Fact]
        public void TheCopiesRunAnAttackersRunAndFanOutWhenOverclocked()
        {
            Assert.Equal(Balance.AttackerRunSpeed, ZackRules.AfterimageSpeed);
            Assert.Equal(15.0f, ZackRules.AfterimageReach, 3);
            Assert.Equal(0.0f, ZackRules.AfterimageYaw(0, 1));
            Assert.Equal(-ZackRules.AfterimageYaw(0, 2), ZackRules.AfterimageYaw(1, 2));
            Assert.True(ZackRules.AfterimageYaw(1, 2) > 0.0f);
            // A full path runs exactly 3 s; a short one runs to its end, stalls, then fizzles.
            Assert.Equal(3.0f, ZackRules.AfterimageLife(ZackRules.AfterimageReach), 3);
            Assert.Equal(1.0f + ZackRules.AfterimageStallSeconds, ZackRules.AfterimageLife(5.0f), 3);
            Assert.Equal(5.0f, ZackRules.AfterimageTravel(2.0f, 5.0f), 3);
            Assert.Equal(2.5f, ZackRules.AfterimageTravel(0.5f, 15.0f), 3);
        }

        [Fact]
        public void TheWireHangsAboveAnyJumpAndBelowAFlyer()
        {
            float jumpApex = Balance.JumpVelocity * Balance.JumpVelocity / (2.0f * Balance.CharacterGravity);
            Assert.True(ZackRules.LiveWireSagHeight > jumpApex, $"the lowest point {ZackRules.LiveWireSagHeight} must clear a {jumpApex} m hop");
            Assert.Equal(ZackRules.LiveWirePostHeight, ZackRules.LiveWireHeightAt(0.0f), 4);
            Assert.Equal(ZackRules.LiveWireSagHeight, ZackRules.LiveWireHeightAt(0.5f), 4);
            Assert.Equal(ZackRules.LiveWirePostHeight, ZackRules.LiveWireHeightAt(1.0f), 4);
            Assert.True(ZackRules.LiveWireReachAbove < AmihanRules.UpdraftHeight, "a Featherfall flyer passes over the wire");
            Assert.True(ZackRules.LiveWireLengthValid(6.0f) && ZackRules.LiveWireLengthValid(1.5f));
            Assert.False(ZackRules.LiveWireLengthValid(6.2f) || ZackRules.LiveWireLengthValid(1.0f) || ZackRules.LiveWireLengthValid(float.NaN));
        }

        [Fact]
        public void CrossingIsSideToSideThroughTheSpanNeverRoundItsEnds()
        {
            // A wire from (0,0) to (4,0).
            Assert.True(ZackRules.Crossed(0, 0, 4, 0, 2, -0.1f, 2, 0.1f, out float along));
            Assert.Equal(0.5f, along, 3);
            // The same step reversed crosses too.
            Assert.True(ZackRules.Crossed(0, 0, 4, 0, 1, 0.3f, 1, -0.2f, out _));
            // Round the end, not through it.
            Assert.False(ZackRules.Crossed(0, 0, 4, 0, 4.5f, -0.1f, 4.5f, 0.1f, out _));
            // Along it, on one side.
            Assert.False(ZackRules.Crossed(0, 0, 4, 0, 1, 0.2f, 3, 0.2f, out _));
            // Standing exactly on the line is not a crossing, so a body on it is not zapped every frame.
            Assert.False(ZackRules.Crossed(0, 0, 4, 0, 2, 0, 2, 0.2f, out _));
            Assert.False(ZackRules.Crossed(0, 0, 4, 0, 2, -0.2f, 2, 0, out _));
            Assert.False(ZackRules.Crossed(0, 0, 4, 0, float.NaN, 0, 2, 0.2f, out _));
            Assert.Equal(1, ZackRules.Side(0, 0, 4, 0, 2, 1));
            Assert.Equal(-1, ZackRules.Side(0, 0, 4, 0, 2, -1));
        }

        [Fact]
        public void OverclockedStacksWithMomentumAsTheOwnerChose()
        {
            Assert.Equal(1.4f, ZackRules.SpeedScale(0.0f, true), 4);
            Assert.Equal(1.68f, ZackRules.SpeedScale(3.0f, true), 4);
            Assert.Equal(1.2f, ZackRules.SpeedScale(3.0f, false), 4);
            // The fastest he can ever go (a taya, full Momentum, Overclocked, on Ilalim's 1.5x turbo pad) stays inside the
            // host's movement budget.
            float fastest = Balance.DefenderRunSpeed * ZackRules.SpeedScale(3.0f, true) * 1.5f;
            Assert.True(fastest < MoveBudget.MetresPerSecond, $"{fastest} m/s");
        }
    }
}
