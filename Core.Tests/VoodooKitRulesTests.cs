using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// Phaister's voodoo kit (HERO-10 v3), asserted against the owner's own table of 2026-09-27
    /// (`docs/reports/phaister-kit-2026-09-27/plan.md` section 9). A retune that drifts from what he wrote fails here.
    /// </summary>
    public sealed class VoodooKitRulesTests
    {
        [Fact]
        public void TheOwnersNumbersAreTheOnesInTheKit()
        {
            Assert.Equal(35.0f, VoodooRules.TeleportCooldown);   // "35 Seconds Cooldown"
            Assert.Equal(35.0f, VoodooRules.DrainCooldown);      // "35 Seconds Cooldown"
            Assert.Equal(35.0f, VoodooRules.HexCooldown);        // "35 Seconds Cooldown."
            Assert.Equal(1.5f, VoodooRules.DrainDelaySeconds);   // "After a 1.5 seconds delay"
            Assert.Equal(10.0f, VoodooRules.HexArmSeconds);      // "After 10 seconds it can be recast"
            Assert.Equal(12.0f, VoodooRules.DollCost);           // "12 Objective Points"
            Assert.Equal(0.10f, VoodooRules.PassiveSpeedShare);  // "takes 10% of their speed and they slow down by 10%"
            Assert.Equal(2.0f, VoodooRules.ReachSeconds);        // "for like 2 seconds or smth"
        }

        [Fact]
        public void TheVoodooStatusesAreTheOwnersRows()
        {
            // Appended, never renumbered: the byte crosses the wire.
            Assert.Equal(10, (int)StatusKind.Drained);
            Assert.Equal(11, (int)StatusKind.Hexed);

            var drained = StatusRules.For(StatusKind.Drained);
            Assert.Equal("Depletes stamina to 0. Prevents stamina recovery for 2.5 seconds.", drained.Description);
            Assert.Equal("Disabled Stamina Recovery", drained.Tooltip);
            Assert.Equal(2.5f, drained.Seconds);
            Assert.Equal(1.0f, drained.SpeedScale);
            Assert.False(drained.BlocksMovement || drained.BlocksInteraction);

            var hexed = StatusRules.For(StatusKind.Hexed);
            Assert.Equal("Hallucinations of slippers randomly appear on your screen for 7.5 seconds.", hexed.Description);
            Assert.Equal("Hallucinations", hexed.Tooltip);
            Assert.Equal(7.5f, hexed.Seconds);
            Assert.False(hexed.BlocksMovement || hexed.BlocksInteraction);
        }

        [Fact]
        public void DrainedEmptiesTheBarAndHoldsItEmptyForTwoAndAHalfSeconds()
        {
            var s = new Stamina();
            s.Deplete();
            Assert.Equal(0.0f, s.Current);
            Assert.False(s.IsFatigued, "The owner's row has no fatigue slow in it.");

            // Standing still under the status: nothing comes back.
            s.RecoveryBlocked = true;
            for (float t = 0.0f; t < StatusRules.DrainedSeconds; t += 0.02f) s.Step(0.02f, moving: false, sprintHeld: false);
            Assert.Equal(0.0f, s.Current);

            // An empty bar cannot start a sprint.
            Assert.Equal(1.0f, s.Step(0.02f, moving: true, sprintHeld: true));

            // The status ends: the idle clock kept counting, so recovery starts at once.
            s.RecoveryBlocked = false;
            s.Step(0.1f, moving: false, sprintHeld: false);
            Assert.True(s.Current > 0.0f);
        }

        [Fact]
        public void TheReachStartsInsideNineMetresAndSnapsPastEleven()
        {
            Assert.True(VoodooRules.ReachCanStart(8.9f, 10.0f, inSight: true));
            Assert.False(VoodooRules.ReachCanStart(9.1f, 10.0f, inSight: true));
            Assert.False(VoodooRules.ReachCanStart(5.0f, 40.0f, inSight: true));
            Assert.False(VoodooRules.ReachCanStart(5.0f, 10.0f, inSight: false));

            // Once running it may stretch, then snaps.
            Assert.True(VoodooRules.ReachHolds(10.5f, 30.0f, inSight: true));
            Assert.False(VoodooRules.ReachHolds(11.2f, 30.0f, inSight: true));
            Assert.False(VoodooRules.ReachHolds(5.0f, 36.0f, inSight: true));
            Assert.False(VoodooRules.ReachHolds(5.0f, 5.0f, inSight: false));
        }

        [Fact]
        public void AHexMarkArmsAtTenSecondsAndFraysAwayUnused()
        {
            Assert.False(VoodooRules.HexArmed(9.9f));
            Assert.True(VoodooRules.HexArmed(10.0f));
            Assert.True(VoodooRules.HexArmed(24.9f));
            Assert.False(VoodooRules.HexArmed(25.0f));
            Assert.True(VoodooRules.HexExpired(25.0f));
        }

        [Fact]
        public void ThePassiveMovesTenPercentFromTheMarkedToHer()
        {
            Assert.Equal(1.10f, VoodooRules.PassiveSpeedScale(isTheCaster: true), 4);
            Assert.Equal(0.90f, VoodooRules.PassiveSpeedScale(isTheCaster: false), 4);
        }
    }
}
