using System;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// Sean's lava brawler kit against the owner's answers (2026-10-24, `docs/reports/sean-kit-2026-10-24/plan.md` § 6) and
    /// against the friction and knockback-cap arithmetic his pushes move bodies with.
    /// </summary>
    public class SeanRulesTests
    {
        [Fact]
        public void TheOwnersNumbersAreTheOnesInTheKit()
        {
            Assert.Equal(0.0f, SeanRules.MoltenKnockbackScale);   // "No knockback"
            Assert.Equal(7.0f, SeanRules.CutsceneSeconds);        // "7.0 s"
            Assert.Equal(3, SeanRules.PressureBars);              // "Keep PRESSURE"
            Assert.Equal(15.0f, SeanRules.UltimateCost);          // as Supernova, a power that can score itself
            // "Drops at his feet": the unthrown bomb still splashes, smaller than a thrown one.
            Assert.True(SeanRules.DroppedBombRadius > 0 && SeanRules.DroppedBombRadius < SeanRules.BombRadius);
            Assert.True(SeanRules.DroppedBombPush > 0 && SeanRules.DroppedBombPush < SeanRules.BombPush);
        }

        [Fact]
        public void EveryGroundPushStaysUnderTheMotorsKnockbackCap()
        {
            // `CharacterMotor.ApplyImpulse` clamps a ground push to `Balance.MaxKnockbackSpeed`; a number past it would lie.
            Assert.Equal(4.267f, SeanRules.MaxPushDistance, 3);
            foreach (float push in new[] { SeanRules.RamLaunch, SeanRules.EruptedDistance(SeanRules.RamLaunch),
                                           SeanRules.EruptedDistance(Combat.ShoveDistance()), SeanRules.EruptSplashPush,
                                           SeanRules.FistPush, SeanRules.AwakeningPush, SeanRules.BombPush, SeanRules.DroppedBombPush,
                                           SeanRules.HotCanSlide })
            {
                Assert.True(push <= SeanRules.MaxPushDistance, $"{push} m is past the cap");
                Assert.True(Combat.SpeedForDistance(push) <= Balance.MaxKnockbackSpeed);
            }
        }

        [Fact]
        public void ARamHitStumblesASecondThenTheSlowFadesOverTwo()
        {
            // Owner, 2026-10-24: "make them stumble a bit longer (1 second) tapos slowed for like 2 seconds na nagdedecay".
            Assert.Equal(1.0f, SeanRules.RamStumbleSeconds);
            Assert.Equal(3.0f, SeanRules.SearedTotalSeconds, 3);
            Assert.Equal(SeanRules.RamSlowScale, SeanRules.SearedSpeedScale(SeanRules.SearedTotalSeconds), 3);
            Assert.Equal(SeanRules.RamSlowScale, SeanRules.SearedSpeedScale(SeanRules.RamSlowSeconds), 3);
            Assert.Equal(0.75f, SeanRules.SearedSpeedScale(1.0f), 3);
            Assert.Equal(1.0f, SeanRules.SearedSpeedScale(0), 3);
            Assert.Equal(1.0f, SeanRules.SearedSpeedScale(float.NaN), 3);
        }

        [Fact]
        public void SpeedsAreSolvedFromTheDistancesTheyCarry()
        {
            // The charge is held (`CarryRules`), so it covers the long distance at a speed under the knockback cap.
            Assert.Equal(SeanRules.RamTravel, CarryRules.DistanceFor(SeanRules.RamSpeed, SeanRules.RamHoldSeconds), 3);
            Assert.True(SeanRules.RamSpeed <= Balance.MaxKnockbackSpeed);
            Assert.Equal(SeanRules.RamLaunch, Combat.KnockbackDistance(SeanRules.RamLaunchSpeed), 3);
            Assert.Equal(SeanRules.FistPush, Combat.KnockbackDistance(SeanRules.FistPushSpeed), 3);
            Assert.Equal(SeanRules.HotCanSlide, Combat.KnockbackDistance(SeanRules.HotCanSlideSpeed), 3);
            Assert.InRange(SeanRules.MoltenThrowSpeedScale, 1.0f, 1.3f);
            Assert.Equal(8.0f, SeanRules.RamTravel, 3);
            Assert.Equal(0.805f, SeanRules.RamTravelSeconds, 3);
            // The knock-up reaches its height under character gravity and stays under the lift cap.
            Assert.Equal(SeanRules.FistKnockUpHeight,
                SeanRules.FistLiftSpeed * SeanRules.FistLiftSpeed / (2 * Balance.CharacterGravity), 3);
            Assert.True(SeanRules.FistLiftSpeed <= Balance.MaxKnockbackLift);
        }

        [Fact]
        public void TheStruckCanFliesThenSkidsTheSameDistance()
        {
            float v = SeanRules.HotCanFlightSpeed;
            float flown = v * SeanRules.HotCanFlightSeconds;
            float skid = v * v / (2.0f * SeanRules.HotCanSkidFriction);
            Assert.Equal(SeanRules.HotCanSlide, flown + skid, 3);
            // It peaks at the apex halfway through the flight and is back on the road at its end.
            float up = SeanRules.HotCanLaunchUp, g = SeanRules.HotCanGravity, half = SeanRules.HotCanFlightSeconds * 0.5f;
            Assert.Equal(SeanRules.HotCanApex, up * half - 0.5f * g * half * half, 3);
            Assert.Equal(0.0f, up * SeanRules.HotCanFlightSeconds - 0.5f * g * SeanRules.HotCanFlightSeconds * SeanRules.HotCanFlightSeconds, 3);
            // Slow enough to watch: the whole move takes far longer than the old 0.45 s friction stop.
            Assert.True(SeanRules.HotCanFlightSeconds + v / SeanRules.HotCanSkidFriction > 0.8f);
        }

        [Fact]
        public void PressureFillsOneBarPerHitAndNeverOverflows()
        {
            int bars = 0;
            for (int i = 0; i < 5; i++) bars = SeanRules.PressureAfterHit(bars);
            Assert.Equal(SeanRules.PressureBars, bars);
            Assert.True(SeanRules.PressureFull(bars));
            Assert.Equal(1, SeanRules.PressureAfterHit(-4));
            Assert.False(SeanRules.PressureFull(SeanRules.PressureBars - 1));
        }

        [Theory]
        [InlineData(2, 0f, 2)]
        [InlineData(2, 5.99f, 2)]
        [InlineData(2, 6f, 0)]
        [InlineData(3, 60f, 0)]
        [InlineData(9, 1f, 3)]
        [InlineData(2, float.NaN, 0)]
        public void PressureCoolsAwayAllAtOnceAfterSixIdleSeconds(int bars, float idle, int expected)
            => Assert.Equal(expected, SeanRules.PressureAfterIdle(bars, idle));

        [Fact]
        public void AnEruptionSendsFartherButNeverPastTheCap()
        {
            Assert.Equal(3.5f, SeanRules.EruptedDistance(Combat.ShoveDistance()), 2);   // a 2.5 m shove, erupted
            Assert.Equal(4.2f, SeanRules.EruptedDistance(SeanRules.RamLaunch), 3);
            Assert.Equal(SeanRules.MaxPushDistance, SeanRules.EruptedDistance(4.0f), 3);
            Assert.Equal(0f, SeanRules.EruptedDistance(-1f));
            Assert.Equal(0f, SeanRules.EruptedDistance(float.NaN));
        }

        [Fact]
        public void MoltenMakesTheRamABrawlAndTakesNoKnockback()
        {
            Assert.Equal(SeanRules.RamCooldown, SeanRules.RamCooldownFor(false));
            Assert.Equal(SeanRules.RamMoltenCooldown, SeanRules.RamCooldownFor(true));
            Assert.True(SeanRules.RamMoltenCooldown < SeanRules.RamCooldown);
            // At least one RAM fits inside Molten after the first, at the Molten cooldown.
            Assert.True(SeanRules.MoltenSeconds > SeanRules.RamMoltenCooldown + SeanRules.RamTravelSeconds);
            Assert.Equal(0f, SeanRules.IncomingKnockbackScale(true));
            Assert.Equal(1f, SeanRules.IncomingKnockbackScale(false));
        }

        [Theory]
        [InlineData(0f, false)]
        [InlineData(1.49f, false)]
        [InlineData(1.5f, true)]
        [InlineData(float.NaN, false)]
        public void TheHotCanCannotBeResetUntilItHasCooled(float seconds, bool resettable)
            => Assert.Equal(resettable, SeanRules.HotCanResettable(seconds));

        [Fact]
        public void TheFistIsPlacedInItsRingAndWarnsBeforeItHits()
        {
            // Owner, 2026-10-24: a prayer, then a long press places it. Placed where he looks between the two ranges; the
            // puddle warns before the fist comes, and the hit lands inside the rise that follows.
            Assert.True(SeanRules.FistAimMinRange > 0 && SeanRules.FistAimMinRange < SeanRules.FistAimRange);
            Assert.InRange(SeanRules.FistWarnSeconds, 0.3f, 0.6f);
            Assert.InRange(SeanRules.FistHitDelay, 0.01f, SeanRules.FistRiseSeconds);
        }

        [Fact]
        public void TheFistPushesStraightAwayFromTheCan()
        {
            SeanRules.FistPushDirection(3f, 4f, 0f, 0f, 1f, 0f, out float x, out float z);
            Assert.Equal(0.6f, x, 3); Assert.Equal(0.8f, z, 3);

            // On the can itself: the way the fist was travelling, the same on every peer.
            SeanRules.FistPushDirection(1f, 1f, 1f, 1f, 0f, -2f, out x, out z);
            Assert.Equal(0f, x, 3); Assert.Equal(-1f, z, 3);

            // Nothing to go by: a fixed direction rather than NaN.
            SeanRules.FistPushDirection(1f, 1f, 1f, 1f, 0f, 0f, out x, out z);
            Assert.Equal(1f, x * x + z * z, 3);
        }

        [Theory]
        [InlineData(6f, 6f)]
        [InlineData(20f, 14f)]
        [InlineData(-3f, 0f)]
        [InlineData(float.NaN, 0f)]
        public void TheBombLandsWhereAimedButNoFartherThanItsRange(float aimed, float lands)
            => Assert.Equal(lands, SeanRules.BombLandingDistance(aimed), 3);

        [Fact]
        public void TheBombSparesTheTayasOwnCanAndShrinksWhenDropped()
        {
            Assert.True(SeanRules.BombKnocksCan(casterIsTaya: false));
            Assert.False(SeanRules.BombKnocksCan(casterIsTaya: true));
            Assert.Equal(SeanRules.BombRadius, SeanRules.BombRadiusFor(true));
            Assert.Equal(SeanRules.DroppedBombRadius, SeanRules.BombRadiusFor(false));
            Assert.Equal(SeanRules.DroppedBombPush, SeanRules.BombPushFor(false));
            Assert.InRange(SeanRules.PoolSpeedScale, 0.01f, 0.99f);
            // The gather and the flight fit inside Molten, so a bomb thrown at the last moment still lands in the state.
            Assert.True(SeanRules.BombTellSeconds + SeanRules.BombFlightSeconds < SeanRules.MoltenSeconds);
        }

        [Theory]
        [InlineData(0f, true)]
        [InlineData(1.1f, true)]      // a standing jump over a street blast is still caught
        [InlineData(-1.8f, true)]
        [InlineData(2.5f, false)]     // a roof above the street
        [InlineData(-3.0f, false)]    // the street below a roof
        [InlineData(float.NaN, false)]
        public void ABlastReachesOnlyItsOwnLevel(float dy, bool reached)
            => Assert.Equal(reached, SeanRules.WithinReach(dy, SeanRules.BlastReachHeight));

        [Theory]
        [InlineData(0, 3f)]
        [InlineData(1, 6f)]
        [InlineData(2, 0.4f)]
        [InlineData(3, 5.99f)]
        public void TheHeatBarsReachEveryPeerIntact(int bars, float cool)
        {
            float wire = SeanRules.HeatWire(bars, cool);
            Assert.InRange(wire, 0f, SeanRules.HeatWireCapacity);
            SeanRules.HeatFromWire(wire, out int gotBars, out float gotCool);
            Assert.Equal(bars, gotBars);
            if (bars > 0) Assert.Equal(System.Math.Min(cool, SeanRules.PressureCoolSeconds - 0.001f), gotCool, 2);
            else Assert.Equal(0f, gotCool);
            SeanRules.HeatFromWire(float.NaN, out gotBars, out _);
            Assert.Equal(0, gotBars);
        }

        [Fact]
        public void TheCourtLimitsKeepEachThingOnItsOwnLevel()
        {
            // The fist punches up through a jump but not onto a terrace above; it climbs kerbs a ground mark climbs.
            Assert.True(SeanRules.FistReachHeight < SeanRules.BlastReachHeight);
            Assert.True(SeanRules.FistStepUp <= 0.6f);
            Assert.True(SeanRules.HotCanStepUp < SeanRules.FistStepUp);
            // The hit lands inside the rise, before the fist reaches the top.
            Assert.InRange(SeanRules.FistHitDelay, 0.01f, SeanRules.FistRiseSeconds);
            Assert.True(SeanRules.BombStaggerSeconds >= SeanRules.AwakeningStaggerSeconds);
        }
    }
}
