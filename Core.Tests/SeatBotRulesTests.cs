using Xunit;
using TumbangPreso.Core;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// ADD BOT in the player's room: a bot chosen seat by seat, carried on the room's rules.
    ///
    /// ⚠️ THE OLD "FILL EVERY EMPTY SEAT" MEANING MUST SURVIVE. An older build's rules have no
    /// seat mask at all, and a room it opens with bots on still fills every empty seat here.
    /// </summary>
    public class SeatBotRulesTests
    {
        [Fact]
        public void BotsOffMeansNoSeatHasABot()
        {
            var rules = CustomGameRules.Defaults(GameMode.HeroStrike);
            for (int seat = 0; seat < Balance.PlayerCount; seat++)
                Assert.False(CustomGameRules.SeatHasBot(rules, seat));
        }

        [Fact]
        public void BotsOnWithNoSeatChosenFillsEveryEmptySeat()
        {
            var rules = CustomGameRules.Defaults(GameMode.HeroStrike);
            rules.Bots = CustomGameRules.MaxBots;
            for (int seat = 0; seat < Balance.PlayerCount; seat++)
                Assert.True(CustomGameRules.SeatHasBot(rules, seat));
        }

        [Fact]
        public void AddingOneBotFillsOnlyThatSeat()
        {
            var rules = CustomGameRules.Defaults(GameMode.HeroStrike);
            CustomGameRules.SetSeatBot(rules, 2, true);

            Assert.True(rules.Bots > 0);
            Assert.False(CustomGameRules.SeatHasBot(rules, 1));
            Assert.True(CustomGameRules.SeatHasBot(rules, 2));
            Assert.False(CustomGameRules.SeatHasBot(rules, 3));
            Assert.False(CustomGameRules.CanBeRanked(rules));
        }

        [Fact]
        public void RemovingOneBotFromAFullRoomKeepsTheOthers()
        {
            var rules = CustomGameRules.Defaults(GameMode.HeroStrike);
            rules.Bots = CustomGameRules.MaxBots;
            CustomGameRules.SetSeatBot(rules, 1, false);

            Assert.False(CustomGameRules.SeatHasBot(rules, 1));
            Assert.True(CustomGameRules.SeatHasBot(rules, 0));
            Assert.True(CustomGameRules.SeatHasBot(rules, 2));
            Assert.True(CustomGameRules.SeatHasBot(rules, 3));
        }

        [Fact]
        public void RemovingTheLastBotTurnsBotsOffRatherThanFillingEverySeat()
        {
            var rules = CustomGameRules.Defaults(GameMode.HeroStrike);
            CustomGameRules.SetSeatBot(rules, 3, true);
            CustomGameRules.SetSeatBot(rules, 3, false);

            Assert.Equal(0, rules.Bots);
            Assert.Equal(0, rules.BotSeats);
            for (int seat = 0; seat < Balance.PlayerCount; seat++)
                Assert.False(CustomGameRules.SeatHasBot(rules, seat));
        }

        [Fact]
        public void TheSeatChoiceTravelsAndAnOlderWireReadsAsEverySeat()
        {
            var rules = CustomGameRules.Defaults(GameMode.Classic);
            CustomGameRules.SetSeatBot(rules, 1, true);
            CustomGameRules.SetSeatBot(rules, 3, true);

            var got = CustomGameRules.Parse(CustomGameRules.ToWire(rules), GameMode.HeroStrike);
            Assert.Equal(rules.BotSeats, got.BotSeats);
            Assert.Equal(rules.BotSeats, rules.Clone().BotSeats);
            Assert.False(CustomGameRules.SeatHasBot(got, 2));
            Assert.True(CustomGameRules.SeatHasBot(got, 3));

            var older = CustomGameRules.Parse("0|0|8|90|0|3|3|1|0|0|0", GameMode.Classic);
            Assert.Equal(0, older.BotSeats);
            Assert.True(CustomGameRules.SeatHasBot(older, 2));
        }

        [Fact]
        public void ANewRoomFillsEverySeatAndStillLetsTheHostRemoveOne()
        {
            var rules = CustomGameRules.Defaults(GameMode.HeroStrike);
            CustomGameRules.FillEmptySeatsWithBots(rules);
            for (int seat = 0; seat < Balance.PlayerCount; seat++)
                Assert.True(CustomGameRules.SeatHasBot(rules, seat));

            CustomGameRules.SetSeatBot(rules, 2, false);
            Assert.False(CustomGameRules.SeatHasBot(rules, 2));
            Assert.True(CustomGameRules.SeatHasBot(rules, 3));
            Assert.Equal(rules.BotSeats, CustomGameRules.Parse(CustomGameRules.ToWire(rules), GameMode.HeroStrike).BotSeats);
        }

        [Fact]
        public void AMaskWiderThanTheTableIsClamped()
        {
            var got = CustomGameRules.Parse("0|0|8|90|0|3|3|1|0|0|0|255", GameMode.Classic);
            Assert.Equal(CustomGameRules.AllBotSeats, got.BotSeats);
        }
    }
}
