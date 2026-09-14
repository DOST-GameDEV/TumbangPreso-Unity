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

        [Fact] public void MovingEarlyThrowsCanMissACentreAimAtStreetGameDistance()
        {
            float maximum = 0;
            for (int i = 0; i < 240; i++)
                maximum = Math.Max(maximum, Math.Abs(ThrowAimRules.Sample(.08f, 1, i / 60f, .4f).Yaw));
            float lateralMiss = 10 * (float)Math.Tan(maximum * Math.PI / 180);
            Assert.True(lateralMiss > ThrowRules.HitWindow(-1), $"Worst moving miss was only {lateralMiss:F3} m at 10 m.");
            Assert.InRange(ThrowAimRules.Amplitude(2.5f, 0), .07f, .08f);
        }

        [Fact] public void DirectionGuideOnlyExtendsAfterAStationaryHoldSettles()
        {
            Assert.InRange(ThrowAimRules.GuideHorizon(0, 0), .2f, .3f);
            Assert.True(ThrowAimRules.GuideHorizon(2.5f, 0) > ThrowAimRules.GuideHorizon(.2f, 0) * 2);
            Assert.Equal(0, ThrowAimRules.GuideConfidence(2.5f, 1));
            Assert.Equal(1, ThrowAimRules.GuideConfidence(2.5f, 0));
        }
    }
}
