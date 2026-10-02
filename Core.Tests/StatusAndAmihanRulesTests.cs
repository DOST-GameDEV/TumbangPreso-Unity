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
            Assert.Equal("Disabled Slipper Retrieval and Can Reset", whirled.Tooltip);

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
            // Paete's Rooted (2026-09-25): no movement, but throws and skills still work.
            var rooted = StatusRules.For(StatusKind.Rooted);
            Assert.True(rooted.BlocksMovement);
            Assert.False(rooted.BlocksInteraction, "Rooted players can still throw and use skills (owner).");
            Assert.True(rooted.Removable, "A hold or a tag ends Rooted.");
            Assert.Equal(PaeteRules.SentryLifeSeconds, rooted.Seconds);
            // Haunted and then Zapped were appended; existing wire IDs stay unchanged.
            Assert.Equal(12, (int)StatusKind.Haunted);
            Assert.Equal(13, (int)StatusKind.Zapped);
            Assert.Equal(13, StatusRules.All.Count);
            var zapped = StatusRules.For(StatusKind.Zapped);
            Assert.Equal(5.0f, zapped.Seconds);
            Assert.Equal(1.0f, zapped.SpeedScale);
            Assert.Equal("Disabled Ability Cast", zapped.Tooltip);
            Assert.False(zapped.BlocksMovement || zapped.BlocksInteraction
                || zapped.BlocksSlipperRetrieval || zapped.DropsHeldSlipper);
            Assert.True(zapped.Removable && zapped.ImmunityApplies);
        }

        [Fact]
        public void TheReworkStatusesAreTheOwnersAnswers()
        {
            // ABILITY-2 (owner, 2026-09-26). Appended, never renumbered: the byte crosses the wire.
            Assert.Equal(6, (int)StatusKind.Concussed);
            Assert.Equal(7, (int)StatusKind.Feared);
            Assert.Equal(8, (int)StatusKind.Disoriented);
            Assert.Equal(9, (int)StatusKind.Vulnerable);

            var concussed = StatusRules.For(StatusKind.Concussed);
            Assert.Equal(0.25f, concussed.SpeedScale);
            Assert.Equal(2.5f, StatusRules.ConcussedSeconds);
            Assert.Equal(35.0f, GeoRules.BoulderCooldown);
            Assert.False(concussed.BlocksMovement || concussed.BlocksInteraction);

            // "Flee from kuro and drop slipper".
            var feared = StatusRules.For(StatusKind.Feared);
            Assert.True(feared.DropsHeldSlipper);
            Assert.True(feared.BlocksMovement && feared.BlocksInteraction && feared.BlocksSlipperRetrieval);

            // Hallucinations live on the victim's own screen: nothing about the body is blocked.
            var disoriented = StatusRules.For(StatusKind.Disoriented);
            Assert.False(disoriented.BlocksMovement || disoriented.BlocksInteraction || disoriented.DropsHeldSlipper);

            // "easier to tag".
            Assert.True(StatusRules.VulnerableTagReachScale > 1.0f);
            Assert.True(StatusRules.For(StatusKind.Vulnerable).ImmunityApplies);
        }

        [Fact]
        public void TheReworkKitsCarryTheOwnersNumbers()
        {
            Assert.Equal(7.5f, CryoRules.ColdFeetSeconds);
            Assert.Equal(35.0f, CryoRules.ColdFeetCooldown);
            Assert.Equal(35.0f, CryoRules.FrostbiteCooldown);
            Assert.Equal(3, CryoRules.GlacialWallHits);
            Assert.Equal(35.0f, CryoRules.GlacialWallCooldown);
            Assert.Equal(12.0f, CryoRules.AbsoluteZeroCost);
            Assert.Equal(1.5f, CryoRules.AbsoluteZeroDelay);
            Assert.Equal(15.0f, GeoRules.ShieldSeconds);
            Assert.Equal(7.5f, GeoRules.BarrierSeconds);
            Assert.Equal(35.0f, GeoRules.BarrierCooldown);
            // Higop's "no escape": the pull beats a run, so pushing away only buys a moment.
            Assert.True(VoodooRules.HigopPullOverRun > 0.0f);
            // A normal skill's footprint stays inside VISION section 2's 1.6 to 2.3 m band.
            Assert.InRange(CryoRules.ColdFeetRadius, 1.6f, 2.3f);
            Assert.InRange(NecroRules.TerrifyRadius, 1.6f, 2.3f);
        }

        [Fact]
        public void TheSkillTreeIsSwitchedOff()
        {
            // Owner, 2026-09-26: "JS REMOVE ITS UI FOR NOW AND HARDCODE THE SKILLS".
            Assert.False(HeroLoadoutRules.SidegradesOpen);
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
        public void FeatherfallSnapshotAgesOnlyWithTheAdoptedSimulationClock()
        {
            Assert.Equal(3.0f, AmihanRules.FlightRemainingAtClock(3, 42, 42));
            Assert.Equal(2.75f, AmihanRules.FlightRemainingAtClock(3, 42, 41.75f));
            Assert.Equal(3.0f, AmihanRules.FlightRemainingAtClock(3, 42, 42.1f));
            Assert.Equal(0.0f, AmihanRules.FlightRemainingAtClock(3, 42, 36));
        }

        [Fact]
        public void AirburstCourtRangeCoversExpandedShoreWithoutPoisonedBounds()
        {
            Assert.Equal(40f, AmihanRules.StormSurgeRangeForCourt(-8.6f,8.6f,-13,13));
            float shore=AmihanRules.StormSurgeRangeForCourt(-16,16,-13,24);
            Assert.InRange(shore,48.9f,49f);
            Assert.Equal(40f,AmihanRules.StormSurgeRangeForCourt(float.NaN,16,-13,24));
            Assert.Equal(40f,AmihanRules.StormSurgeRangeForCourt(-16,float.PositiveInfinity,-13,24));
            Assert.Equal(40f,AmihanRules.StormSurgeRangeForCourt(16,-16,-13,24));
            Assert.Equal(40f,AmihanRules.StormSurgeRangeForCourt(-16,16,24,24));
            Assert.Equal(40f,AmihanRules.StormSurgeRangeForCourt(-float.MaxValue,float.MaxValue,-float.MaxValue,float.MaxValue));
        }
        [Fact]
        public void AmihanUsesTheOwnersCostsAndStaysInsideTheKnockbackCap()
        {
            Assert.Equal(35.0f, AmihanRules.QuickDashCooldown);
            Assert.Equal(35.0f, AmihanRules.WhirlwindCooldown);
            Assert.Equal(2.5f, AmihanRules.WhirlwindSeconds);
            Assert.Equal(5.0f, AmihanRules.UpdraftSeconds);
            Assert.Equal(40.0f, AmihanRules.UpdraftCooldown);
            Assert.Equal(15.0f, AmihanRules.StormSurgeCost);
            Assert.Equal(1.5f, AmihanRules.StormSurgeGatherSeconds);
            Assert.Equal(60.0f, AmihanRules.StormSurgeHalfAngle * 2);

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
