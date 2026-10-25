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
            Assert.Equal(14, (int)StatusKind.Possessed);
            Assert.Equal(15, (int)StatusKind.Blinded);
            Assert.Equal(15, StatusRules.All.Count);
            // Nemu's PEEKER (owner 2026-10-21): eyes covered and 25 % slower, both for 2.5 s.
            var blinded = StatusRules.For(StatusKind.Blinded);
            Assert.Equal(2.5f, blinded.Seconds);
            Assert.Equal(0.75f, blinded.SpeedScale);
            Assert.False(blinded.BlocksMovement || blinded.BlocksInteraction
                || blinded.BlocksSlipperRetrieval || blinded.DropsHeldSlipper);
            Assert.True(blinded.Removable && blinded.ImmunityApplies);
            // Nemu's PRUSISYON (owner 2026-10-11): 15 s, control kept, cleansable.
            var possessed = StatusRules.For(StatusKind.Possessed);
            Assert.Equal(15.0f, possessed.Seconds);
            Assert.Equal(0.9f, possessed.SpeedScale); // owner: "maybe possessed players get 10% slow too"
            Assert.False(possessed.BlocksMovement || possessed.BlocksInteraction
                || possessed.BlocksSlipperRetrieval || possessed.DropsHeldSlipper);
            Assert.True(possessed.Removable && possessed.ImmunityApplies);
            // Zack's rework (owner's V3 sheet, 2026-10-25): a 1 s full stun, clearly shorter than Frozen's 2.5 s.
            var zapped = StatusRules.For(StatusKind.Zapped);
            Assert.Equal(1.0f, zapped.Seconds);
            Assert.Equal("Cannot move or act for 1 second.", zapped.Description);
            Assert.Equal(0.0f, zapped.SpeedScale);
            Assert.True(zapped.BlocksMovement && zapped.BlocksInteraction && zapped.BlocksSlipperRetrieval);
            Assert.False(zapped.DropsHeldSlipper);
            Assert.True(zapped.Removable && zapped.ImmunityApplies);
            Assert.True(zapped.Seconds < StatusRules.For(StatusKind.Frozen).Seconds);
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
            Assert.Equal(5.0f, StatusRules.ConcussedSeconds); // the Wiki's "75% slower for 5 seconds"
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
            Assert.Equal(15.0f, CryoRules.FrostbiteLoadSeconds);
            Assert.Equal(15.0f, GeoRules.BoulderLoadSeconds);
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
            Assert.Equal(2.5f, CryoRules.ColdFeetRadius);
            Assert.Equal(.5f,CryoRules.ColdFeetMinRange);
            Assert.Equal(10f,CryoRules.GlacialWallSeconds);
            Assert.Equal(5f,CryoRules.GlacialWallArcLength);
            Assert.Equal(3f,CryoRules.GlacialWallArcRadius);
            Assert.Equal(1.5f,CryoRules.GlacialWallMinRange);
            Assert.InRange(NecroRules.TerrifyRadius, 1.6f, 2.3f);
        }

        [Fact]
        public void KeeperStandsTheCanUpIn1Point5SecondsFor15Seconds()
        {
            // Owner, 2026-10-21: 15 s, stands the can up in 1.5 s every time, 15 s cooldown after it is reclaimed.
            Assert.Equal(15.0f, NecroRules.LookoutSeconds);
            Assert.Equal(1.5f, NecroRules.LookoutRestoreSeconds);
            Assert.Equal(15.0f, NecroRules.LookoutCooldown);
        }

        [Fact]
        public void PeekerIsTheOwnersTripwire()
        {
            // Owner, 2026-10-21: fades over 1.5 s, waits up to 15 s, a 2 m trigger, blind and 25 % slower for 2.5 s.
            Assert.Equal(1.5f, NecroRules.DontLookArmSeconds);
            Assert.Equal(15.0f, NecroRules.DontLookLifeSeconds);
            Assert.Equal(2.0f, NecroRules.DontLookTriggerRadius);
            Assert.Equal(2.5f, NecroRules.DontLookBlindSeconds);
            Assert.Equal(2.5f, NecroRules.DontLookSlowSeconds);
            Assert.Equal(15.0f, NecroRules.DontLookCooldown);
            // A flat 25 % slow for as long as it lasts, none once it runs out.
            Assert.Equal(0.75f, NecroRules.DontLookSpeedScale(2.5f), 4);
            Assert.Equal(0.75f, NecroRules.DontLookSpeedScale(0.1f), 4);
            Assert.Equal(1.0f, NecroRules.DontLookSpeedScale(0.0f), 4);
            Assert.Equal(1.0f, NecroRules.DontLookSpeedScale(float.NaN), 4);
            // Eyes covered for the whole of it.
            Assert.True(NecroRules.DontLookCoversEyes(2.5f));
            Assert.True(NecroRules.DontLookCoversEyes(0.01f));
            Assert.False(NecroRules.DontLookCoversEyes(0.0f));
        }

        [Fact]
        public void FetcherWaitsForItsReclaimedGhost()
        {
            Assert.Equal(15.0f, NecroRules.FetchCooldown);
            Assert.Equal(25.0f, NecroRules.SoulboundMissedCooldown);
            Assert.Equal(8.0f, NecroRules.FetchSeconds);
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
        public void DriftHitTimingHasTheSameCanonicalFloatOnEveryTarget()
        {
            Assert.Equal(0x3ed07507, BitConverter.SingleToInt32Bits(AmihanRules.QuickDashHitSeconds));
            double speed = AmihanRules.QuickDashSpeed;
            double tail = speed / (2d * Balance.Friction);
            double held = Math.Max(0d, AmihanRules.QuickDashDistance / speed - tail);
            Assert.Equal(AmihanRules.QuickDashHitSeconds, (float)(held + tail + .05d));
            Assert.InRange(Math.Abs(AmihanRules.QuickDashHitSeconds
                - (AmihanRules.QuickDashHoldSeconds + AmihanRules.QuickDashSpeed / (2f * Balance.Friction) + .05f)), 0, .000001f);
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
            // AIRBURST v3.2 expands the owner's attack to the whole frontal half-plane.
            Assert.Equal(180.0f, AmihanRules.StormSurgeHalfAngle * 2);

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

        [Fact]
        public void TheWhirlwindIsSizedFromItsOneRadius()
        {
            // It is born clear of her: its near edge is in front of her feet, never on them.
            Assert.Equal(AmihanRules.WhirlwindRadius + AmihanRules.WhirlwindClearance, AmihanRules.WhirlwindStart, 4);
            Assert.True(AmihanRules.WhirlwindStart - AmihanRules.WhirlwindRadius > 0.0f);
            // Where its middle is: at its start, then at its speed, and it stops at the end of its life.
            Assert.Equal(AmihanRules.WhirlwindStart, AmihanRules.WhirlwindCentre(0.0f), 4);
            Assert.Equal(AmihanRules.WhirlwindStart + AmihanRules.WhirlwindSpeed, AmihanRules.WhirlwindCentre(1.0f), 4);
            Assert.Equal(AmihanRules.WhirlwindCentre(AmihanRules.WhirlwindSeconds), AmihanRules.WhirlwindCentre(99.0f), 4);
            Assert.Equal(AmihanRules.WhirlwindCentre(0.0f), AmihanRules.WhirlwindCentre(-1.0f), 4);
            // Its far edge by the end: start, travel and one more radius.
            Assert.Equal(AmihanRules.WhirlwindStart + AmihanRules.WhirlwindTravel + AmihanRules.WhirlwindRadius, AmihanRules.WhirlwindReach, 4);
            // A lane, not a wall (VISION section 2): narrower than the 14 m box is wide by a good margin.
            Assert.True(2 * AmihanRules.WhirlwindRadius < Balance.ConfinementRadius);
        }

        [Fact]
        public void TheWhirlwindCatchesACircleSweptBetweenTwoSteps()
        {
            float r = AmihanRules.WhirlwindRadius, reach = r + AmihanRules.WhirlwindBodyAllowance, c = 6.0f;
            // Standing still: inside the circle (and a body's half width past it) is caught, beyond is not, all round.
            Assert.True(AmihanRules.WhirlwindCatches(0.0f, c, c, c));
            Assert.True(AmihanRules.WhirlwindCatches(reach - 0.01f, c, c, c));
            Assert.False(AmihanRules.WhirlwindCatches(reach + 0.01f, c, c, c));
            Assert.True(AmihanRules.WhirlwindCatches(0.0f, c + reach - 0.01f, c, c));
            Assert.False(AmihanRules.WhirlwindCatches(0.0f, c + reach + 0.01f, c, c));
            Assert.True(AmihanRules.WhirlwindCatches(0.0f, c - reach + 0.01f, c, c));
            Assert.False(AmihanRules.WhirlwindCatches(0.0f, c - reach - 0.01f, c, c));
            // A circle, not a square: its corner is outside.
            Assert.False(AmihanRules.WhirlwindCatches(reach * 0.8f, c + reach * 0.8f, c, c));
            // Either side of the lane alike.
            Assert.Equal(AmihanRules.WhirlwindCatches(1.3f, c + 0.7f, c, c), AmihanRules.WhirlwindCatches(-1.3f, c + 0.7f, c, c));
            // A hitch: the circle jumped 9 m in one step. A body it passed over in between is caught, one beside its path is not.
            Assert.True(AmihanRules.WhirlwindCatches(0.5f, c + 4.5f, c, c + 9.0f));
            Assert.False(AmihanRules.WhirlwindCatches(reach + 0.2f, c + 4.5f, c, c + 9.0f));
            // The two steps given either way round are the same sweep.
            Assert.True(AmihanRules.WhirlwindCatches(0.5f, c + 4.5f, c + 9.0f, c));
        }
    }
}
