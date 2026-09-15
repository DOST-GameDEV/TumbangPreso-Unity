using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class NormalMatchLengthTests
    {
        [Theory]
        [InlineData(GameMode.Classic)]
        [InlineData(GameMode.HeroStrike)]
        public void NormalMatchesGiveEveryPlayerTwoDefenderTurns(GameMode mode)
        {
            var rules = CustomGameRules.Defaults(mode);
            Assert.Equal(8, rules.Rounds);
            var turns = new int[Balance.PlayerCount];
            for (int round = 1; round <= rules.Rounds; round++) turns[MatchRules.DefenderSlotFor(round)]++;
            foreach (int count in turns) Assert.Equal(2, count);
        }
        [Fact]
        public void ExplicitCustomFourRoundWireStillMeansFourRounds()
        {
            var custom = CustomGameRules.Defaults(GameMode.Classic); custom.Rounds = 4;
            Assert.Equal(4, CustomGameRules.Parse(CustomGameRules.ToWire(custom), GameMode.Classic).Rounds);
        }
    }
}
