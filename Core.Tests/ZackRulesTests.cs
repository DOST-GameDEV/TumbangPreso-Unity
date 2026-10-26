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
        public void FullThrottleHoldsMomentumFullThroughAStopButNotAfterIt()
        {
            float stopped = 0.0f;
            // From nothing, one locked step is full Momentum: the whole bonus at once.
            float m = ZackRules.StepMomentum(0.0f, ref stopped, false, 0.016f, throttled: true);
            Assert.Equal(ZackRules.MomentumRampSeconds, m);
            Assert.Equal(3, ZackRules.MomentumStep(m));
            Assert.Equal(ZackRules.BaseSpeedScale * 1.25f, ZackRules.SpeedScale(m, false), 4);
            // Standing still for a whole second inside the lock costs nothing.
            for (int i = 0; i < 60; i++) m = ZackRules.StepMomentum(m, ref stopped, false, 1.0f / 60.0f, throttled: true);
            Assert.Equal(ZackRules.MomentumRampSeconds, m);
            Assert.Equal(0.0f, stopped);
            // Once the lock ends the ordinary rule applies: a stop past the grace resets it.
            for (int i = 0; i < 20; i++) m = ZackRules.StepMomentum(m, ref stopped, false, 1.0f / 60.0f, throttled: false);
            Assert.Equal(0.0f, m);
        }

        [Fact]
        public void TheOwnersNumbersAreTheOnesInTheKit()
        {
            // Owner, 2026-10-26: modestly faster, and Momentum builds faster: +8% always, up to +25% after 2 seconds.
            Assert.Equal(1.08f, ZackRules.BaseSpeedScale);
            Assert.Equal(0.25f, ZackRules.MomentumMaxBonus);
            Assert.Equal(2.0f, ZackRules.MomentumRampSeconds);
            // Backtrack: "3 seconds ago", 30 s; Overclocked "recharges in 3 seconds".
            Assert.Equal(3.0f, ZackRules.BacktrackSeconds);
            Assert.Equal(30.0f, ZackRules.BacktrackCooldown);
            Assert.Equal(3.0f, ZackRules.BacktrackOverclockedCooldown);
            // Full Throttle (owner pick, 2026-10-26): full Momentum and Charge at once, locked 3 s; 24 s, Overclocked 8 s.
            Assert.Equal(3.0f, ZackRules.FullThrottleSeconds);
            Assert.Equal(24.0f, ZackRules.FullThrottleCooldownFor(false));
            Assert.Equal(8.0f, ZackRules.FullThrottleCooldownFor(true));
            // The retired Afterimage's numbers still restore old replays: "for 3 seconds", 30 s, two copies Overclocked.
            Assert.Equal(3.0f, ZackRules.AfterimageSeconds);
            Assert.Equal(30.0f, ZackRules.AfterimageCooldown);
            Assert.Equal(1, ZackRules.AfterimageCopies(false));
            Assert.Equal(2, ZackRules.AfterimageCopies(true));
            // Live Wire: up to 6 metres, stays on the court, dark 5 s after a trip, 20 s cooldown.
            Assert.Equal(6.0f, ZackRules.LiveWireMaxLength);
            Assert.Equal(5.0f, ZackRules.LiveWireDarkSeconds);
            Assert.Equal(20.0f, ZackRules.LiveWireCooldown);
            Assert.True(ZackRules.LiveWireLifeBound > Balance.RoundTime * 2.0f, "the bound is never an expiry inside a round");
            // Overclock: 15 points, 25 seconds of fuel, 30% faster.
            Assert.Equal(15.0f, ZackRules.OverclockCost);
            Assert.Equal(25.0f, ZackRules.OverclockSeconds);
            Assert.Equal(1.3f, ZackRules.OverclockSpeedScale);
            // Zapped: "Cannot move or act for 1 second."
            Assert.Equal(1.0f, StatusRules.ZappedSeconds);
        }

        [Fact]
        public void MomentumRampsToTwentyFivePercentOverTwoSecondsInThreeVisibleStages()
        {
            Assert.Equal(1.0f, ZackRules.MomentumScale(0.0f));
            Assert.Equal(1.125f, ZackRules.MomentumScale(1.0f), 4);
            Assert.Equal(1.25f, ZackRules.MomentumScale(2.0f), 4);
            Assert.Equal(1.25f, ZackRules.MomentumScale(30.0f), 4);
            Assert.Equal(1.0f, ZackRules.MomentumScale(float.NaN));
            Assert.Equal(0, ZackRules.MomentumStep(0.69f));
            Assert.Equal(1, ZackRules.MomentumStep(0.7f));
            Assert.Equal(2, ZackRules.MomentumStep(1.5f));
            Assert.Equal(3, ZackRules.MomentumStep(2.0f));
            Assert.Equal(0, ZackRules.MomentumStep(float.NaN));
        }

        [Fact]
        public void MomentumBuildsWhileRunningAndResetsOnlyOnARealStop()
        {
            float stopped = 0.0f, seconds = 0.0f;
            for (int i = 0; i < 400; i++) seconds = ZackRules.StepMomentum(seconds, ref stopped, true, 0.01f);
            Assert.Equal(ZackRules.MomentumRampSeconds, seconds, 3);
            // A hard cut (0.2 s under the run speed) keeps it.
            for (int i = 0; i < 20; i++) seconds = ZackRules.StepMomentum(seconds, ref stopped, false, 0.01f);
            Assert.Equal(ZackRules.MomentumRampSeconds, seconds, 3);
            seconds = ZackRules.StepMomentum(seconds, ref stopped, true, 0.01f);
            Assert.Equal(0.0f, stopped);
            // A real stop (past the 0.25 s grace) resets it.
            for (int i = 0; i < 30; i++) seconds = ZackRules.StepMomentum(seconds, ref stopped, false, 0.01f);
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
        public void TheLowTensionWireCanBeClearedByATimedJump()
        {
            float jumpApex = Balance.JumpVelocity * Balance.JumpVelocity / (2.0f * Balance.CharacterGravity);
            Assert.True(ZackRules.LiveWirePostHeight <= .30f);
            Assert.True(ZackRules.LiveWireReachAbove < jumpApex, "A timed hop must clear the low wire.");
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
        public void HisBaseMomentumAndOverclockedStack()
        {
            Assert.Equal(1.08f, ZackRules.SpeedScale(0.0f, false), 4);
            Assert.Equal(1.08f * 1.3f, ZackRules.SpeedScale(0.0f, true), 4);
            Assert.Equal(1.08f * 1.25f, ZackRules.SpeedScale(2.0f, false), 4);
            Assert.Equal(1.08f * 1.25f * 1.3f, ZackRules.SpeedScale(2.0f, true), 4);
            // The fastest he can ever go (a taya, full Momentum, Overclocked, on the strongest map boost a motor accepts) stays
            // inside the host's movement budget.
            float fastest = Balance.DefenderRunSpeed * ZackRules.SpeedScale(2.0f, true) * 2.0f;
            Assert.True(fastest < MoveBudget.MetresPerSecond, $"{fastest} m/s");
        }

        [Fact]
        public void ChargeOutlastsTheSharedBarAndOverclockMakesTheSprintFree()
        {
            var shared = new Stamina();
            var charge = new Stamina { Profile = ZackRules.ChargeProfile(false) };
            float sharedSeconds = 0, chargeSeconds = 0;
            while (shared.Step(0.01f, true, true) > 1.0f) sharedSeconds += 0.01f;
            while (charge.Step(0.01f, true, true) > 1.0f) chargeSeconds += 0.01f;
            Assert.InRange(sharedSeconds, 2.4f, 2.6f);
            Assert.InRange(chargeSeconds, 4.0f, 4.3f);
            // Refills from empty in about 0.6 + 2 seconds.
            var refill = new Stamina { Profile = ZackRules.ChargeProfile(false) };
            refill.Deplete();
            float t = 0;
            while (refill.Current < Balance.StaminaMax && t < 10) { refill.Step(0.01f, false, false); t += 0.01f; }
            Assert.InRange(t, 2.5f, 2.7f);
            // Overclocked: sprinting spends nothing, starts below the floor, and the bar keeps refilling under it.
            var free = new Stamina { Profile = ZackRules.ChargeProfile(true) };
            free.Deplete();
            Assert.Equal(Balance.SprintScale, free.Step(0.01f, true, true));
            for (int i = 0; i < 300; i++) free.Step(0.01f, true, true);
            Assert.True(free.IsSprinting);
            Assert.True(free.Current > 200.0f);
            // A broken profile falls back to the shared rates.
            var broken = new Stamina { Profile = new StaminaProfile(float.NaN, 0, 0) };
            Assert.Equal(Balance.StaminaDrainRate, broken.Profile.DrainRate);
        }

        [Fact]
        public void TheWireGoesDarkAfterATripAndRearms()
        {
            Assert.False(ZackRules.LiveWireDark(0.0f, 3.0f));
            Assert.True(ZackRules.LiveWireDark(2.0f, 2.0f));
            Assert.True(ZackRules.LiveWireDark(2.0f, 6.9f));
            Assert.False(ZackRules.LiveWireDark(2.0f, 7.0f));
            Assert.False(ZackRules.LiveWireDark(float.NaN, 3.0f));
            Assert.True(ZackRules.LiveWireDarkSeconds > StatusRules.ZappedSeconds + 2.0f, "the victim has time to step away");
        }

        [Fact]
        public void LookingAtYourOwnWireIsMeasuredAlongTheLookWithinReach()
        {
            // A wire from (-1,3) to (1,3); looking straight down +z from the origin passes right over its middle.
            Assert.Equal(0.0f, ZackRules.LookToWire(0, 0, 0, 1, 4, -1, 3, 1, 3), 3);
            // Looking away, or with the wire beyond reach, finds nothing.
            Assert.True(float.IsPositiveInfinity(ZackRules.LookToWire(0, 0, 0, -1, 4, -1, 3, 1, 3)));
            Assert.True(float.IsPositiveInfinity(ZackRules.LookToWire(0, 0, 0, 1, 2, -1, 3, 1, 3)));
            // Looking well past its end misses it.
            Assert.True(ZackRules.LookToWire(0, 0, 1, 1, 4, -1, 3, 1, 3) > ZackRules.LiveWireReelAimTolerance);
            Assert.True(float.IsPositiveInfinity(ZackRules.LookToWire(0, 0, float.NaN, 1, 4, -1, 3, 1, 3)));
        }

        [Fact]
        public void TheZapLocksAfterOneSecondHoldsThroughAFlickerAndBurnsFuelFaster()
        {
            float broken = 0, held = 0;
            for (int i = 0; i < 50; i++) held = ZackRules.StepZapLock(held, ref broken, true, 0.01f);
            // A 0.2 s flicker off the target keeps the progress.
            for (int i = 0; i < 20; i++) held = ZackRules.StepZapLock(held, ref broken, false, 0.01f);
            Assert.Equal(0.5f, held, 3);
            for (int i = 0; i < 60; i++) held = ZackRules.StepZapLock(held, ref broken, true, 0.01f);
            Assert.Equal(ZackRules.ZapLockSeconds, held, 3);
            // A real break starts it over.
            for (int i = 0; i < 40; i++) held = ZackRules.StepZapLock(held, ref broken, false, 0.01f);
            Assert.Equal(0.0f, held);
            Assert.Equal(1.0f, ZackRules.FuelBurn(false, false));
            Assert.Equal(3.0f, ZackRules.FuelBurn(true, true));
            Assert.Equal(1.5f, ZackRules.FuelBurn(true, false));
            // A full tank held on rivals lasts long enough for several Zaps, never the whole 25 s.
            Assert.InRange(ZackRules.OverclockSeconds / ZackRules.FuelBurn(true, true), 7.0f, 9.0f);
            Assert.True(ZackRules.ZapImmunitySeconds > StatusRules.ZappedSeconds + ZackRules.ZapLockSeconds,
                        "one held beam cannot chain-stun a rival");
            Assert.Equal(0.0f, ZackRules.FlatAngle(0, 1, 0, 5), 3);
            Assert.Equal(90.0f, ZackRules.FlatAngle(0, 1, 3, 0), 3);
            Assert.Equal(180.0f, ZackRules.FlatAngle(0, 0, 3, 0), 3);
        }
    }
}
