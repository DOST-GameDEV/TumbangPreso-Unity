using System;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class ThrowAimRulesTests
    {
        [Fact] public void HoldingSteadilySettlesButNeverRemovesAllMovement()
        {
            Assert.True(ThrowAimRules.Amplitude(.1f, 0) > ThrowAimRules.Amplitude(.7f, 0));
            Assert.True(ThrowAimRules.Amplitude(.7f, 0) > ThrowAimRules.Amplitude(2.5f, 0));
            Assert.True(ThrowAimRules.Amplitude(20, 0) > 0);
        }

        [Fact] public void MovementRemainsLessSteadyAtEveryHoldTime()
        {
            foreach(float hold in new[]{0f,.2f,1f,2.5f,8f})
            {
                float still=ThrowAimRules.Amplitude(hold,0);
                float walking=ThrowAimRules.Amplitude(hold,.55f);
                Assert.True(walking>still);Assert.True(ThrowAimRules.Amplitude(hold,1)>walking);
            }
        }

        [Fact] public void ImmediateReleaseHasMoreVisibleDriftThanSettledRelease()
        {
            double quick=0,held=0;
            for(int i=0;i<120;i++)
            {
                var a=ThrowAimRules.Sample(.02f,0,i/60f,.31f);
                var b=ThrowAimRules.Sample(2.5f,0,i/60f,.31f);
                quick+=a.Yaw*a.Yaw+a.Pitch*a.Pitch;held+=b.Yaw*b.Yaw+b.Pitch*b.Pitch;
            }
            Assert.True(quick>held*20);Assert.True(held>.01);
        }

        [Fact] public void PhaseContinuesAfterFullChargeWithoutSharpFrameJumps()
        {
            var previous=ThrowAimRules.Sample(5,1,5,.7f);bool moved=false;
            for(int i=1;i<=144;i++)
            {
                var now=ThrowAimRules.Sample(5+i/144f,1,5+i/144f,.7f);
                Assert.InRange(Math.Abs(now.Yaw-previous.Yaw),0,.25f);
                moved|=Math.Abs(now.Yaw-previous.Yaw)>.0001f;previous=now;
            }
            Assert.True(moved);
        }

        [Fact] public void SamplingDoesNotDependOnHowManyFramesWereRendered()
        {
            var expected=ThrowAimRules.Sample(2, .6f, 9, .4f);
            foreach(int rate in new[]{30,60,144})
            {
                for(int i=0;i<rate*2;i++)ThrowAimRules.Sample(i/(float)rate,.6f,7+i/(float)rate,.4f);
                Assert.Equal(expected,ThrowAimRules.Sample(2,.6f,9,.4f));
            }
        }

        [Fact] public void InvalidInputsCannotProduceNonfiniteAim()
        {
            var sample=ThrowAimRules.Sample(float.NaN,float.PositiveInfinity,float.NaN,float.NegativeInfinity);
            Assert.False(float.IsNaN(sample.Yaw)||float.IsInfinity(sample.Yaw));
            Assert.False(float.IsNaN(sample.Pitch)||float.IsInfinity(sample.Pitch));
        }
    }
}
