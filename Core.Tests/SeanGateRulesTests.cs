using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class SeanGateRulesTests
    {
        [Fact] public void OneWarningPrecedesTheThreeSecondArmedWindow()
        {
            Assert.Equal(35f,SeanGateRules.Cooldown);
            Assert.Equal(3f,SeanGateRules.HalfWidth*2);
            Assert.False(SeanGateRules.IsArmed(.349f));
            Assert.True(SeanGateRules.IsArmed(.35f));
            Assert.True(SeanGateRules.IsArmed(3.349f));
            Assert.False(SeanGateRules.IsArmed(3.35f));
            Assert.False(SeanGateRules.IsArmed(float.NaN));
        }
        [Theory]
        [InlineData(1f,-1f,1)]
        [InlineData(-1f,1f,-1)]
        public void CrossingsReturnTheApproachSide(float before,float after,int expected)
        {
            Assert.True(SeanGateRules.TryCrossing(0,before,0,after,.4f,0,out float time,out int side));
            Assert.Equal(.5f,time);Assert.Equal(expected,side);
        }
        [Fact] public void TouchingThenReturningDoesNotConsumeButContinuingAcrossDoes()
        {
            Assert.False(SeanGateRules.TryCrossing(0,1,0,0,.4f,1,out _,out _));
            Assert.False(SeanGateRules.TryCrossing(0,0,0,1,.4f,1,out _,out _));
            Assert.True(SeanGateRules.TryCrossing(0,0,0,-1,.4f,1,out float time,out int side));
            Assert.Equal(0,time);Assert.Equal(1,side);
            Assert.False(SeanGateRules.TryCrossing(0,0,0,-1,.4f,0,out _,out _));
        }
        [Fact] public void FiniteEndsIncludeTheRealBodyFootprintWithoutAnInfinitePlane()
        {
            Assert.True(SeanGateRules.TryCrossing(1.8f,1,1.8f,-1,.4f,1,out _,out _));
            Assert.False(SeanGateRules.TryCrossing(1.91f,1,1.91f,-1,.4f,1,out _,out _));
            Assert.False(SeanGateRules.TryCrossing(4,1,4,-1,.4f,1,out _,out _));
            Assert.True(SeanGateRules.TryCrossing(2,1,0,-1,0,1,out _,out _));
        }
        [Fact] public void EarliestCrossingCanBeSelectedIndependentlyOfPlayerEnumeration()
        {
            Assert.True(SeanGateRules.TryCrossing(0,1,0,-3,.4f,1,out float early,out _));
            Assert.True(SeanGateRules.TryCrossing(0,3,0,-1,.4f,1,out float late,out _));
            Assert.Equal(.25f,early);Assert.Equal(.75f,late);Assert.True(early<late);
            Assert.False(SeanGateRules.TryCrossing(0,1,0,2,.4f,1,out _,out _));
        }
        [Fact] public void InvalidSamplesCannotCreateContact()
        {
            Assert.False(SeanGateRules.TryCrossing(float.NaN,1,0,-1,.4f,1,out _,out _));
            Assert.False(SeanGateRules.TryCrossing(0,1,0,float.PositiveInfinity,.4f,1,out _,out _));
            Assert.False(SeanGateRules.TryCrossing(0,1,0,-1,-1,1,out _,out _));
            Assert.False(SeanGateRules.TryCrossing(0,1,0,-1,.4f,2,out _,out _));
            Assert.Equal(0,SeanGateRules.Side(float.NaN));
            Assert.Equal(0,SeanGateRules.Side(.00001f));
        }
    }
}
