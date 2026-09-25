using System;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The owner's status table (2026-09-25) and Amihan's kit numbers, asserted against what he
    /// wrote. A retune that drifts from the table fails here in a second.
    /// </summary>
    public sealed class StatusAndAmihanRulesTests
    {
        [Fact]
        public void TheStatusTableIsTheOwnersTable()
        {
            var whirled = StatusRules.For(StatusKind.Whirled);
            Assert.Equal(2.5f, whirled.Seconds);
            Assert.True(whirled.DropsHeldSlipper && whirled.BlocksSlipperRetrieval);
            Assert.False(whirled.BlocksMovement || whirled.BlocksInteraction);
            Assert.Equal("Disabled Slipper Retrieval", whirled.Tooltip);

            var chilled = StatusRules.For(StatusKind.Chilled);
            Assert.Equal(5.0f, chilled.Seconds);
            Assert.Equal(0.5f, chilled.SpeedScale);
            Assert.Equal("Reduced Movement Speed", chilled.Tooltip);

            var frozen = StatusRules.For(StatusKind.Frozen);
            Assert.Equal(2.5f, frozen.Seconds);
            Assert.True(frozen.BlocksMovement && frozen.BlocksInteraction);
            Assert.Equal("Disabled Movement and Interaction", frozen.Tooltip);

            var tagged = StatusRules.For(StatusKind.Tagged);
            Assert.Equal(Balance.TagStunTime, tagged.Seconds);
            Assert.Equal(5.0f, tagged.Seconds);
            Assert.False(tagged.Removable, "Tagged cannot be removed (owner's table).");
            Assert.False(tagged.ImmunityApplies, "Nothing may be immune to Tagged (owner's table).");
            Assert.Equal(4, StatusRules.All.Count);
        }

        [Fact]
        public void StatusesOverlapByMaxNeverAdd()
        {
            Assert.Equal(2.5f, StatusRules.Refresh(1.0f, 2.5f));
            Assert.Equal(2.5f, StatusRules.Refresh(2.5f, 1.0f));
        }

        [Fact]
        public void ACarryCoversExactlyTheWrittenDistance()
        {
            foreach (var (distance, speed) in new[] { (5.0f, 14.0f), (16.0f, 15.0f), (9.0f, 12.0f) })
            {
                float hold = CarryRules.SecondsFor(distance, speed);
                Assert.True(Math.Abs(CarryRules.DistanceFor(speed, hold) - distance) < 1e-4f,
                    $"{speed} m/s held {hold} s does not cover {distance} m.");
            }
            // The release tail is the old impulse rule exactly: v^2 / (2 x Friction).
            Assert.Equal(15.0f * 15.0f / (2.0f * Balance.Friction), CarryRules.TailDistance(15.0f), 4);
            Assert.Equal(0.0f, CarryRules.SecondsFor(1.0f, 14.0f));
            Assert.Equal(1.2f, CarryRules.TailDistance(CarryRules.ImpulseFor(1.2f)), 4);
        }

        [Fact]
        public void AmihanUsesTheOwnersCostsAndStaysInsideTheKnockbackCap()
        {
            Assert.Equal(40.0f, AmihanRules.QuickDashCooldown);
            Assert.Equal(35.0f, AmihanRules.WhirlwindCooldown);
            Assert.Equal(2.5f, AmihanRules.WhirlwindSeconds);
            Assert.Equal(10.0f, AmihanRules.UpdraftSeconds);
            Assert.Equal(15.0f, AmihanRules.StormSurgeCost);
            Assert.Equal(2.5f, AmihanRules.StormSurgeGatherSeconds);

            // Every held speed is under the single-impulse cap, so a clamp never shortens a move.
            Assert.True(AmihanRules.QuickDashSpeed <= Balance.MaxKnockbackSpeed);
            Assert.True(AmihanRules.StormSurgeSpeed <= Balance.MaxKnockbackSpeed);
            Assert.True(AmihanRules.QuickDashPushSpeed <= Balance.MaxKnockbackSpeed);

            // "Very far": further than the widest street map is wide (2 x 8.6 m half width).
            Assert.True(AmihanRules.StormSurgeDistance >= 2 * 8.0f);

            // The gale crosses the court: longer than the 14 m box.
            Assert.True(AmihanRules.WhirlwindTravel >= 2 * Balance.ConfinementRadius - 0.5f);

            // Updraft is out of any standing reach: higher than a lunge can be read as reaching.
            Assert.True(AmihanRules.UpdraftHeight > 2.0f);
        }
    }
}
