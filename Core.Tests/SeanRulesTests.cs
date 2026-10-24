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
            foreach (float push in new[] { SeanRules.RamTravel, SeanRules.RamLaunch, SeanRules.EruptedDistance(SeanRules.RamLaunch),
                                           SeanRules.EruptedDistance(Combat.ShoveDistance()), SeanRules.EruptSplashPush,
                                           SeanRules.FistPush, SeanRules.AwakeningPush, SeanRules.BombPush, SeanRules.DroppedBombPush,
                                           SeanRules.HotCanSlide })
            {
                Assert.True(push <= SeanRules.MaxPushDistance, $"{push} m is past the cap");
                Assert.True(Combat.SpeedForDistance(push) <= Balance.MaxKnockbackSpeed);
            }
        }

        [Fact]
        public void SpeedsAreSolvedFromTheDistancesTheyCarry()
        {
            Assert.Equal(SeanRules.RamTravel, Combat.KnockbackDistance(SeanRules.RamSpeed), 3);
            Assert.Equal(SeanRules.RamLaunch, Combat.KnockbackDistance(SeanRules.RamLaunchSpeed), 3);
            Assert.Equal(SeanRules.FistPush, Combat.KnockbackDistance(SeanRules.FistPushSpeed), 3);
            Assert.Equal(SeanRules.HotCanSlide, Combat.KnockbackDistance(SeanRules.HotCanSlideSpeed), 3);
            Assert.InRange(SeanRules.MoltenThrowSpeedScale, 1.0f, 1.3f);
            Assert.Equal(15.49f, SeanRules.RamSpeed, 2);
            Assert.Equal(0.516f, SeanRules.RamTravelSeconds, 3);
            // The knock-up reaches its height under character gravity and stays under the lift cap.
            Assert.Equal(SeanRules.FistKnockUpHeight,
                SeanRules.FistLiftSpeed * SeanRules.FistLiftSpeed / (2 * Balance.CharacterGravity), 3);
            Assert.True(SeanRules.FistLiftSpeed <= Balance.MaxKnockbackLift);
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

        [Theory]
        [InlineData(5f, 0f, true)]
        [InlineData(9f, 15f, true)]
        [InlineData(9.01f, 0f, false)]
        [InlineData(5f, -15.1f, false)]
        [InlineData(0f, 0f, false)]
        [InlineData(float.NaN, 0f, false)]
        public void TheFistLocksOnlyInsideItsRangeAndCone(float distance, float offAim, bool locks)
            => Assert.Equal(locks, SeanRules.FistCanLock(distance, offAim));

        [Theory]
        [InlineData(0f, 90f, 0.5f, 45f)]       // limited to 90 degrees a second
        [InlineData(0f, 10f, 0.5f, 10f)]       // reaches it when it is near
        [InlineData(170f, -170f, 0.1f, 179f)]  // the short way round, across the seam (+9 degrees)
        [InlineData(-170f, 170f, 0.1f, -179f)]
        [InlineData(30f, 90f, 0f, 30f)]        // no time, no turn
        public void TheFistTurnsTheShortWayAtItsTurnRate(float heading, float toTarget, float dt, float expected)
            => Assert.Equal(expected, SeanRules.FistSteer(heading, toTarget, dt), 3);

        [Fact]
        public void TheFistCanAlwaysReachTheEdgeOfItsLockInItsLife()
        {
            Assert.True(SeanRules.FistMaxFlightSeconds * SeanRules.FistSpeed >= SeanRules.FistLockRange);
            Assert.True(SeanRules.FistArriveRadius < SeanRules.FistPunchRadius);
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
    }
}
