using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class RafiRulesTests
    {
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
