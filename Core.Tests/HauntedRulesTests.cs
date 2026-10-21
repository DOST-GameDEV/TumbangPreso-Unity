using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class HauntedRulesTests
    {
        [Fact]
        public void HauntedUsesTheCurrentWikiPerceptionContractWithoutRenumberingStatuses()
        {
            Assert.Equal(11, (byte)StatusKind.Hexed);
            Assert.Equal(12, (byte)StatusKind.Haunted);
            var rule = StatusRules.For(StatusKind.Haunted);
            Assert.NotNull(rule);
            Assert.Equal(7.5f, rule.Seconds);
            Assert.Equal("Reduced Perception", rule.Tooltip);
            Assert.False(rule.ImmunityApplies);
            Assert.False(rule.BlocksMovement || rule.BlocksInteraction || rule.BlocksSlipperRetrieval || rule.DropsHeldSlipper);
            Assert.Equal(1f, rule.SpeedScale);
            Assert.Contains("HUD markers", rule.Description);
            Assert.Contains("sight", rule.Description);
            Assert.Contains("audio", rule.Description);
        }
    }
}
