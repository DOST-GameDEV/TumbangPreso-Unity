using System;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// PRUSISYON's possession contest (`docs/reports/nemu-ultimate-2026-10-11/direction.md` section 2): the owner's
    /// numbers, and the promises the design makes about how it feels to fight it.
    /// </summary>
    public class NemuRulesTests
    {
        private const float MidDrift = 3.0f; // between seat 0's first surge and its second tell

        [Fact]
        public void TheOwnersNumbersAreTheOnesInTheKit()
        {
            Assert.Equal(15.0f, NemuRules.PossessedSeconds);
            Assert.Equal(0.9f, NemuRules.PossessedSpeedScale);
            Assert.True(NemuRules.ProcessionSeconds < 6.0f, "owner: below 6 seconds");
        }

        [Fact]
        public void EverySeatSurgesSixTimesClearOfTheSettleAndTheLeave()
        {
            for (int seat = 0; seat < 4; seat++)
            {
                float previousEnd = NemuRules.SettleSeconds;
                for (int i = 0; i < NemuRules.SurgesPerPossession; i++)
                {
                    float s = NemuRules.SurgeStart(seat, i);
                    Assert.True(s - NemuRules.TellSeconds >= previousEnd - 1e-4f, $"seat {seat} surge {i} overlaps");
                    previousEnd = s + NemuRules.SurgeSeconds;
                }
                Assert.True(previousEnd <= NemuRules.PossessedSeconds - NemuRules.LeaveSeconds);
            }
        }

        [Fact]
        public void FourVictimsNeverLurchOnTheSameFrame()
        {
            for (int i = 0; i < NemuRules.SurgesPerPossession; i++)
                for (int a = 0; a < 4; a++)
                    for (int b = a + 1; b < 4; b++)
                        Assert.True(Math.Abs(NemuRules.SurgeStart(a, i) - NemuRules.SurgeStart(b, i)) >= 0.15f - 1e-4f);
        }

        [Fact]
        public void HandsOffYouDriftAtFortyTwoPercentAndASurgeWalksYou()
        {
            NemuRules.Pull(MidDrift, 0, 0f, 1f, out float px, out float pz);
            NemuRules.Contest(0f, 0f, px, pz, 1f, out float wx, out float wz);
            Assert.Equal(NemuRules.DriftPull, Length(wx, wz), 3);
            Assert.True(wz > 0.35f, "it pulls toward the lure");

            float surge = NemuRules.SurgeStart(0, 1) + 0.3f;
            Assert.Equal(NemuRules.Phase.Surge, NemuRules.PhaseAt(surge, 0));
            NemuRules.Pull(surge, 0, 0f, 1f, out px, out pz);
            NemuRules.Contest(0f, 0f, px, pz, 1f, out wx, out wz);
            Assert.Equal(1f, Length(wx, wz), 3);
        }

        [Fact]
        public void FightingItKeepsMostOfYourSpeedAndASurgeOnlyHoldsYou()
        {
            // Pushing straight against a straight pull (no sway: lure along the input's opposite).
            float drift = 0.42f, surge = 1.05f;
            NemuRules.Contest(0f, -1f, 0f, drift, 1f, out _, out float wz);
            Assert.Equal(-0.58f, wz, 3);
            NemuRules.Contest(0f, -1f, 0f, surge, 1f, out _, out wz);
            Assert.InRange(wz, 0f, 0.06f); // held: never thrown
            // Worn down to the floor.
            NemuRules.Contest(0f, -1f, 0f, drift, NemuRules.GripFloor, out _, out wz);
            Assert.Equal(-0.811f, wz, 3);
            NemuRules.Contest(0f, -1f, 0f, surge, NemuRules.GripFloor, out _, out wz);
            Assert.InRange(wz, -0.5285f, -0.5265f);
        }

        [Fact]
        public void GripWearsDownOnlyWhileYouFightAndRecovers()
        {
            Assert.True(NemuRules.Resisting(0f, -1f, 0f, 0.42f));
            Assert.False(NemuRules.Resisting(1f, 0f, 0f, 0.42f), "sideways is not fighting");
            Assert.False(NemuRules.Resisting(0f, -0.3f, 0f, 0.42f), "a nudge is not fighting");
            float grip = 1f;
            for (int i = 0; i < 15 * 60; i++) grip = NemuRules.StepGrip(grip, true, 1f / 60f);
            Assert.Equal(NemuRules.GripFloor, grip, 4);
            grip = NemuRules.StepGrip(grip, false, 1f);
            Assert.Equal(NemuRules.GripFloor + NemuRules.GripRecoverPerSecond, grip, 4);
            Assert.Equal(1f, NemuRules.StepGrip(1f, false, 5f));
        }

        [Fact]
        public void TheTellSlackensBeforeEverySurge()
        {
            for (int seat = 0; seat < 4; seat++)
            {
                float s = NemuRules.SurgeStart(seat, 2);
                Assert.Equal(NemuRules.Phase.Tell, NemuRules.PhaseAt(s - 0.2f, seat));
                Assert.True(NemuRules.Strength(s - 0.2f, seat) < NemuRules.Strength(s - 0.8f, seat));
                Assert.True(NemuRules.TellAmount(s - 0.1f, seat) > 0.7f);
            }
        }

        [Fact]
        public void ItSettlesInAndClimbsOutAndIsNothingOutsideItsClock()
        {
            Assert.Equal(0f, NemuRules.Strength(0f, 0));
            Assert.Equal(0f, NemuRules.Strength(-1f, 0));
            Assert.Equal(0f, NemuRules.Strength(15f, 0));
            Assert.Equal(0f, NemuRules.Strength(float.NaN, 0));
            Assert.True(NemuRules.Strength(14.5f, 0) < NemuRules.DriftPull * 0.5f);
            Assert.Equal(NemuRules.Phase.Leave, NemuRules.PhaseAt(14.0f, 3));
        }

        [Fact]
        public void TheGhostsPullTowardTheWorstPlace()
        {
            // An attacker toward the taya.
            Assert.True(NemuRules.Lure(1, 0f, 5f, false, true, 0f, 0f, 3f, 0f, out float dx, out float dz));
            Assert.Equal(0f, dx, 3); Assert.Equal(-1f, dz, 3);
            // The taya away from the can.
            Assert.True(NemuRules.Lure(0, 1f, 0f, true, true, 1f, 0f, 0f, 0f, out dx, out dz));
            Assert.Equal(1f, dx, 3);
            // The taya on the can: a fixed way out per seat, a unit vector.
            Assert.True(NemuRules.Lure(2, 0f, 0f, true, true, 0f, 0f, 0f, 0f, out dx, out dz));
            Assert.Equal(1f, Length(dx, dz), 3);
            // No taya: toward the can.
            Assert.True(NemuRules.Lure(1, 0f, 4f, false, false, 0f, 0f, 0f, 0f, out dx, out dz));
            Assert.Equal(-1f, dz, 3);
            // Already there: nothing to pull toward.
            Assert.False(NemuRules.Lure(1, 0f, 0.1f, false, true, 0f, 0f, 3f, 0f, out dx, out dz));
            Assert.Equal(0f, dx); Assert.Equal(0f, dz);
        }

        [Fact]
        public void TheSwayStaysInsideItsBoundAndStraightensInASurge()
        {
            for (float t = 0f; t < 15f; t += 0.05f)
                for (int seat = 0; seat < 4; seat++)
                    Assert.InRange(Math.Abs(NemuRules.SwayAt(t, seat)), 0f, NemuRules.SwayDegrees + 1e-3f);
            float s = NemuRules.SurgeStart(0, 0) + 0.3f;
            Assert.InRange(Math.Abs(NemuRules.SwayAt(s, 0)), 0f, NemuRules.SwayDegrees * NemuRules.SurgeSwayScale + 1e-3f);
        }

        [Fact]
        public void EveryPeerComputesTheSamePull()
        {
            NemuRules.Pull(7.31f, 2, 0.6f, 0.8f, out float ax, out float az);
            NemuRules.Pull(7.31f, 2, 0.6f, 0.8f, out float bx, out float bz);
            Assert.Equal(ax, bx); Assert.Equal(az, bz);
        }

        private static float Length(float x, float z) => (float)Math.Sqrt(x * x + z * z);
    }
}
