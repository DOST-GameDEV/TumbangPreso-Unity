using System.Collections.Generic;
using Xunit;
using TumbangPreso.Core;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// `Attention.md` § 16.1, the seat handover ruling: *"let ai on same skill level as them take
    /// over"*, and the consequence it names, which is the part with teeth: **"a rating that
    /// counts a bot's stretch as the player's own is a ladder nobody trusts."**
    /// </summary>
    public class SeatHandoverTests
    {
        // -------------------------------------------------------------------
        // WHICH BOT TAKES THE CHAIR
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ THE POINT OF THIS ONE IS THAT NEITHER NUMBER IS TYPED IN THE RULES FILE. If
        /// somebody retunes the ladder's start or the queue's widest band, the handover bands
        /// move with them, and this is what says so out loud rather than pinning two constants
        /// that can drift apart.
        /// </summary>
        [Fact]
        public void TheTierBandsAreDerivedFromTheQueueRatherThanPicked()
        {
            Assert.Equal(1000, SeatHandover.BataCeiling);
            Assert.Equal(2000, SeatHandover.AstigFloor);

            Assert.Equal((int)RatingRules.StartRating - MatchmakingRules.MaxHalfWidth,
                         SeatHandover.BataCeiling);
            Assert.Equal((int)RatingRules.StartRating + MatchmakingRules.MaxHalfWidth,
                         SeatHandover.AstigFloor);
        }

        [Fact]
        public void ASeatIsHandedToABotAtTheAbsentPlayersOwnLevel()
        {
            Assert.Equal(Difficulty.Bata, SeatHandover.TierFor(400));
            Assert.Equal(Difficulty.Bata, SeatHandover.TierFor(SeatHandover.BataCeiling - 1));

            Assert.Equal(Difficulty.Normal, SeatHandover.TierFor(SeatHandover.BataCeiling));
            Assert.Equal(Difficulty.Normal, SeatHandover.TierFor(1500));
            Assert.Equal(Difficulty.Normal, SeatHandover.TierFor(SeatHandover.AstigFloor - 1));

            Assert.Equal(Difficulty.Astig, SeatHandover.TierFor(SeatHandover.AstigFloor));
            Assert.Equal(Difficulty.Astig, SeatHandover.TierFor(3000));
        }

        /// <summary>
        /// ⚠️ THE TIER MUST NOT GO BACKWARDS. A better player getting a worse bot is the exact
        /// failure the ruling exists to prevent, and it is the kind of thing a "tidied" mapping
        /// introduces silently.
        /// </summary>
        [Fact]
        public void TheTierNeverFallsAsTheRatingRises()
        {
            var previous = SeatHandover.TierFor(0);

            for (int rating = 0; rating <= 3000; rating += 25)
            {
                var tier = SeatHandover.TierFor(rating);
                Assert.True((int)tier >= (int)previous,
                            $"the bot tier fell between {rating - 25} and {rating}");
                previous = tier;
            }
        }

        // -------------------------------------------------------------------
        // WHAT THE LADDER IS ALLOWED TO CONCLUDE
        // -------------------------------------------------------------------

        [Fact]
        public void ABotsStretchNeverMovesTheRatingOfThePersonWhoLeft()
        {
            var seats = new List<SeatOrigin>
            {
                SeatOrigin.Human, SeatOrigin.Human, SeatOrigin.Human, SeatOrigin.HandedToBot,
            };

            Assert.Equal(0.0, SeatHandover.RatingWeightFor(SeatOrigin.HandedToBot, seats));

            Assert.False(SeatHandover.RatingMovesFor(SeatOrigin.HandedToBot));
            Assert.False(SeatHandover.RatingMovesFor(SeatOrigin.Bot));
            Assert.True(SeatHandover.RatingMovesFor(SeatOrigin.Human));
        }

        /// <summary>
        /// ⚠️⚠️ THE HALF THAT IS EASY TO FORGET, AND IT IS ABOUT THE THREE WHO STAYED. They
        /// finished the match against an AI. `BotFillRules.Weight` already says a result with a
        /// bot in it is worth less; a seat that BECAME a bot has to reach that same arithmetic or
        /// the game pays a four-human result for a three-human match.
        /// </summary>
        [Fact]
        public void AHandedOverSeatCountsAsABotForEverybodyElsesRating()
        {
            var allHuman = new List<SeatOrigin>
            {
                SeatOrigin.Human, SeatOrigin.Human, SeatOrigin.Human, SeatOrigin.Human,
            };
            var oneLeft = new List<SeatOrigin>
            {
                SeatOrigin.Human, SeatOrigin.Human, SeatOrigin.Human, SeatOrigin.HandedToBot,
            };

            Assert.Equal(4, SeatHandover.HumanSeats(allHuman));
            Assert.Equal(3, SeatHandover.HumanSeats(oneLeft));

            double full = SeatHandover.RatingWeightFor(SeatOrigin.Human, allHuman);
            double after = SeatHandover.RatingWeightFor(SeatOrigin.Human, oneLeft);

            Assert.Equal(1.0, full, 9);
            Assert.Equal(BotFillRules.Weight(3, 4), after, 9);
            Assert.True(after < full,
                        "a seat that became a bot is still being paid as a person");
        }

        [Fact]
        public void AMatchNobodyLeftIsUnaffectedByAnyOfThis()
        {
            var seats = new List<SeatOrigin>
            {
                SeatOrigin.Human, SeatOrigin.Human, SeatOrigin.Bot, SeatOrigin.Bot,
            };

            Assert.Equal(BotFillRules.Weight(2, 4),
                         SeatHandover.RatingWeightFor(SeatOrigin.Human, seats), 9);
        }

        // -------------------------------------------------------------------
        // WHAT THE PLAYER IS TOLD
        // -------------------------------------------------------------------

        [Fact]
        public void AHandedOverSeatIsLabelledAsNeitherAPersonNorAPlainBot()
        {
            Assert.Equal("", SeatHandover.TagFor(SeatOrigin.Human));
            Assert.Equal(BotFillRules.BotTag, SeatHandover.TagFor(SeatOrigin.Bot));

            // Calling it BOT erases the person who played most of it; calling it nothing erases
            // the bot that finished it.
            Assert.NotEqual(BotFillRules.BotTag, SeatHandover.TagFor(SeatOrigin.HandedToBot));
            Assert.NotEmpty(SeatHandover.TagFor(SeatOrigin.HandedToBot));
        }

        [Fact]
        public void TheBoardNamesTheCauseOrSaysNothingAtAll()
        {
            Assert.Equal("", SeatHandover.HandoverNote(0));
            Assert.Contains("bot", SeatHandover.HandoverNote(1).ToLowerInvariant());
            Assert.Contains("2", SeatHandover.HandoverNote(2));
        }

        // -------------------------------------------------------------------
        // THE RECORD
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ `IsBot` STAYS ON THE WIRE AND `Origin` SITS BESIDE IT, so a record written by a
        /// path that has not been taught about handovers reads exactly as it did before rather
        /// than as an unknown third thing.
        /// </summary>
        [Fact]
        public void ANewLineIsAPersonUntilSomethingSaysOtherwise()
        {
            var line = new PlayerMatchStats();

            Assert.False(line.IsBot);
            Assert.Equal(SeatOrigin.Human, line.Origin);
            Assert.True(SeatHandover.RatingMovesFor(line.Origin));
        }

        [Fact]
        public void ABotLineAndABotOriginAgree()
        {
            var line = new PlayerMatchStats { IsBot = true, Origin = SeatOrigin.Bot };
            Assert.True(line.IsBot);
            Assert.False(SeatHandover.RatingMovesFor(line.Origin));

            // The handed-over case is the one the boolean cannot express, and this is the
            // assertion that says so rather than a comment claiming it.
            var handed = new PlayerMatchStats { IsBot = false, Origin = SeatOrigin.HandedToBot };
            Assert.False(handed.IsBot);
            Assert.False(SeatHandover.RatingMovesFor(handed.Origin));
        }
    }
}
