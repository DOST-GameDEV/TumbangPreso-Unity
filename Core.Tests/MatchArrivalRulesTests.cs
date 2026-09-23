using System;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public class MatchArrivalRulesTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RoomReadyChoiceSurvivesCloneAndWire(bool manual)
        {
            var rules = CustomGameRules.Defaults(GameMode.Classic);
            rules.ManualReady = manual;
            Assert.Equal(manual, rules.Clone().ManualReady);
            Assert.Equal(manual, CustomGameRules.Parse(CustomGameRules.ToWire(rules), GameMode.HeroStrike).ManualReady);
        }

        [Fact]
        public void OlderWireRetainsManualReadyAndItsOriginalRulePrefix()
        {
            const string old = "0|0|8|90|0|3|0|1|0";
            var rules = CustomGameRules.Parse(old, GameMode.HeroStrike);
            Assert.True(rules.ManualReady);
            rules.ManualReady = false;
            string wire = CustomGameRules.ToWire(rules);
            Assert.Equal(old, wire.Substring(0, wire.LastIndexOf('|')));
            Assert.False(CustomGameRules.Parse(wire, GameMode.HeroStrike).ManualReady);
            Assert.True(CustomGameRules.Parse(old + "|unrecognised", GameMode.Classic).ManualReady);
        }
    }
}
