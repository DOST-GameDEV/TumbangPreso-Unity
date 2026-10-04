using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class RafiRulesTests
    {
        [Fact] public void BahaKeepsTheAdoptedWarningCarryAndCost()
        {
            Assert.Equal(.8f,RafiRules.BahaWarning);Assert.Equal(3,RafiRules.BahaCarryDistance);
            Assert.Equal(15,RafiRules.BahaCost);Assert.Equal(3.4f,RafiRules.BahaDuration(10),5);
        }
        [Fact] public void BahaReachesAsymmetricCourtEdgesAlongActualAim()
        {
            Assert.Equal(18,RafiRules.BahaLaneExit(0,-6,0,1,-10,14,-12,12));
            Assert.Equal(14,RafiRules.BahaLaneExit(0,-6,1,0,-10,14,-12,12));
            Assert.Equal(10,RafiRules.BahaLaneExit(0,-6,-1,0,-10,14,-12,12));
            Assert.Equal(6,RafiRules.BahaLaneExit(0,-6,0,-1,-10,14,-12,12));
        }
        [Fact] public void BahaDiagonalEndsAtTheFirstBoundary()
        {
            float d=(float)System.Math.Sqrt(.5);
            Assert.Equal(10/d,RafiRules.BahaLaneExit(0,0,d,d,-20,10,-20,20),4);
            Assert.Equal(0,RafiRules.BahaLaneExit(11,0,d,d,-20,10,-20,20));
            Assert.Equal(0,RafiRules.BahaLaneExit(10,0,1,0,-20,10,-20,20));
        }
        [Fact] public void BahaRejectsMalformedGeometryAndCapsHugeMaps()
        {
            Assert.Equal(0,RafiRules.BahaLaneExit(0,0,float.NaN,1,-10,10,-10,10));
            Assert.Equal(0,RafiRules.BahaLaneExit(0,0,0,2,-10,10,-10,10));
            Assert.Equal(0,RafiRules.BahaLaneExit(0,0,0,1,10,-10,-10,10));
            Assert.Equal(64,RafiRules.BahaLaneExit(0,0,0,1,-100,100,-100,100));
        }
        [Fact] public void BahaGroundedSweepDoesNotHitAirborneOrBeyondCover()
        {
            Assert.True(RafiRules.BahaCrosses(0,0,4,3.8f,4.1f,10,true));
            Assert.False(RafiRules.BahaCrosses(0,.1f,4,3.8f,4.1f,10,false));
            Assert.False(RafiRules.BahaCrosses(0,0,4,3.8f,4.1f,3.9f,true));
            Assert.False(RafiRules.BahaCrosses(3.1f,0,4,3.8f,4.1f,10,true));
            Assert.False(RafiRules.BahaCrosses(0,1,4,3.8f,4.1f,10,true));
        }
        [Fact] public void BahaSweepRejectsBacktrackingAndBadNumbers()
        {
            Assert.False(RafiRules.BahaCrosses(0,0,4,4.1f,3.8f,10,true));
            Assert.False(RafiRules.BahaCrosses(0,0,float.NaN,3.8f,4.1f,10,true));
            Assert.False(RafiRules.BahaCrosses(0,0,-.1f,0,.1f,10,true));
        }

        [Fact] public void CurrentUsesThePublishedCooldownAndExistingBoundedFootprint()
        {
            Assert.Equal(35f, RafiRules.CurrentCooldown);
            Assert.Equal(6f, RafiRules.CurrentRange);
            Assert.Equal(.65f, RafiRules.CurrentRadius);
        }

        [Fact] public void SkimHasOneBoundedLoadAndGroundTravel()
        {
            Assert.Equal(35f,RafiRules.SkimCooldown);
            Assert.Equal(8f,RafiRules.SkimLoadSeconds);
            Assert.Equal(2f,RafiRules.SkimDistance);
            Assert.Equal(.4f,RafiRules.SkimDistance/RafiRules.SkimSpeed,5);
        }

        [Fact] public void WaterwallCrossingIsBoundedAndWorksFromEitherSide()
        {
            Assert.Equal(35,RafiRules.WallCooldown);Assert.Equal(4,RafiRules.WallSeconds);
            Assert.True(RafiRules.WallCrossing(0,1,-1,0,1,1,out float a));
            Assert.True(RafiRules.WallCrossing(0,1,1,0,1,-1,out float b));
            Assert.Equal(.5f,a);Assert.Equal(a,b);
            Assert.False(RafiRules.WallCrossing(3,1,-1,3,1,1,out _));
            Assert.False(RafiRules.WallCrossing(0,3,-1,0,3,1,out _));
            Assert.False(RafiRules.WallCrossing(0,1,-2,0,1,-1,out _));
            Assert.False(RafiRules.WallCrossing(0,1,float.NaN,0,1,1,out _));
        }

        [Fact] public void FirstEntryPrecedesClosestApproachAndIsOrderable()
        {
            Assert.True(RafiRules.FirstCurrentContact(0, 0, 2, 0, 0, -2, .5f, out float later));
            Assert.True(RafiRules.FirstCurrentContact(0, 0, 1, 0, 0, -3, .5f, out float earlier));
            Assert.Equal(.375f, later, 5); Assert.Equal(.125f, earlier, 5);
            Assert.True(earlier < later);
        }

        [Fact] public void VerticalAndHorizontalWindowsMustOverlap()
        {
            Assert.True(RafiRules.FirstCurrentContact(0, 2, 0, 0, -2, 0, .5f, out float time));
            Assert.Equal(.325f, time, 5);
            Assert.False(RafiRules.FirstCurrentContact(0, 2, 2, 0, 1, -2, .5f, out _));
            Assert.False(RafiRules.FirstCurrentContact(2, 0, 0, 2, 0, 0, .5f, out _));
        }

        [Fact] public void InsideStationaryAndInvalidInputsHaveExplicitOutcomes()
        {
            Assert.True(RafiRules.FirstCurrentContact(0, 0, 0, 0, 0, 0, .5f, out float time));
            Assert.Equal(0, time);
            Assert.False(RafiRules.FirstCurrentContact(float.NaN, 0, 0, 0, 0, 0, .5f, out _));
            Assert.False(RafiRules.FirstCurrentContact(0, 0, 0, 0, 0, 0, float.PositiveInfinity, out _));
        }
    }
}
