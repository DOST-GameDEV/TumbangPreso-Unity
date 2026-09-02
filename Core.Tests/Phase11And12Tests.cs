using System;
using System.Collections.Generic;
using Xunit;
using TumbangPreso.Core;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// PHASE 11: the queue that offers bots rather than nothing, and what that costs the ladder.
    ///
    /// ⚠️ EVERY ASSERTION HERE REACHES SOMETHING OUTSIDE THE FEATURE, which is `Phase10Tests`'
    /// standing complaint about the suite it replaced: a test that compares a constant to itself
    /// cannot fail. The thresholds are checked against `MatchmakingRules`' own widening clock, the
    /// weight against `MinHumansForRating`, and the labels against the surfaces that draw them.
    /// </summary>
    public class BotFillTests
    {
        [Fact]
        public void ACasualQueueOffersNothingBeforeItsThreshold()
        {
            Assert.False(BotFillRules.OffersFill(QueueStake.Casual, 44.0f, 1, 4));
            Assert.True(BotFillRules.OffersFill(QueueStake.Casual, 45.0f, 1, 4));
        }

        /// <summary>
        /// ⚠️⚠️ THE CASUAL OFFER LANDS WHILE THE SEARCH IS STILL WIDENING AND THE RANKED ONE LANDS
        /// LONG AFTER IT HAS STOPPED. That is the whole difference between the two numbers and it
        /// is asserted against `MatchmakingRules` rather than against a literal, so widening the
        /// band schedule moves this test rather than silently invalidating it.
        /// </summary>
        [Fact]
        public void TheCasualOfferArrivesWhileTheSearchIsStillWideningAndTheRankedOneLongAfter()
        {
            Assert.True(BotFillRules.CasualFillAfterSeconds < MatchmakingRules.SecondsToWidest);
            Assert.True(BotFillRules.RankedFillAfterSeconds > MatchmakingRules.SecondsToWidest * 2.0f);
        }

        [Fact]
        public void AFullLobbyIsNeverOfferedBots()
        {
            Assert.False(BotFillRules.OffersFill(QueueStake.Casual, 600.0f, 4, 4));
            Assert.Equal(0, BotFillRules.BotsToFill(4, 4));
            Assert.Equal(3, BotFillRules.BotsToFill(1, 4));
        }

        /// <summary>
        /// ⚠️⚠️ THE ASSERTION `FUTURE.md` § 19.11 ASKS FOR IS INVERTED HERE ON PURPOSE, AND
        /// `BotFillRules`' CLASS NOTE IS THE RECEIPT. That prompt says *"never bots in ranked ...
        /// a test must assert it"*; § 11's body overrules it with 🧑's own words and a stated
        /// expiry. **This test asserts the CURRENT decision and names the constant that reverses
        /// it**, so the day the population argument stops being true, one edit flips the rule and
        /// this test fails loudly rather than the behaviour changing quietly.
        /// </summary>
        [Fact]
        public void RankedTakesBotsOnlyBecauseTheresNobodyToPlayAndOneConstantSaysSo()
        {
            Assert.True(BotFillRules.RankedAcceptsBots);
            Assert.True(BotFillRules.OffersFill(QueueStake.Ranked, 150.0f, 2, 4));
        }

        /// <summary>
        /// ⚠️⚠️ ONE HUMAN AGAINST THREE BOTS MOVES NOTHING, AND THIS IS THE 4 A.M. TEST.
        /// `FUTURE.md` § 11 names the failure by name: *"the fastest climb in the game is queueing
        /// at 4 a.m."*
        /// </summary>
        [Fact]
        public void ASoloHumanAgainstBotsMovesNoRatingAtAll()
        {
            Assert.Equal(0.0, BotFillRules.Weight(1, 4));
            Assert.False(BotFillRules.RatingCounts(1, 4));
        }

        [Fact]
        public void EveryHumanSeatPastTheFirstIsAQuarterOfTheResult()
        {
            Assert.Equal(1.0, BotFillRules.Weight(4, 4), 3);
            Assert.Equal(2.0 / 3.0, BotFillRules.Weight(3, 4), 3);
            Assert.Equal(1.0 / 3.0, BotFillRules.Weight(2, 4), 3);
        }

        /// <summary>
        /// ⚠️ THE THRESHOLD AND THE WEIGHT ARE ONE RULE, NOT TWO. `MinHumansForRating` is the
        /// first human count whose weight is above zero, and if somebody changes one without the
        /// other this fails.
        /// </summary>
        [Fact]
        public void TheMinimumHumanCountIsTheFirstOneWorthAnything()
        {
            Assert.True(BotFillRules.Weight(BotFillRules.MinHumansForRating, 4) > 0.0);
            Assert.Equal(0.0, BotFillRules.Weight(BotFillRules.MinHumansForRating - 1, 4));
        }

        [Fact]
        public void AResultThatDidNotCountSaysWhyAndAFullOneSaysNothing()
        {
            Assert.Equal("", BotFillRules.RatingNote(4, 4, ranked: true));
            Assert.Equal("", BotFillRules.RatingNote(1, 4, ranked: false));
            Assert.Contains("nobody's rating moved", BotFillRules.RatingNote(1, 4, ranked: true));
            Assert.Contains("67 per cent", BotFillRules.RatingNote(3, 4, ranked: true));
        }

        /// <summary>
        /// ⚠⚠ THE WEIGHT IS WRITTEN TWICE, IN C# AND IN `match-record.js`, FOR THE REASON
        /// `IntegrityRules.Digest` IS: the SERVER computes the rating and the game has to be able
        /// to say what it will be. This is the cheap half of what `tools/check_digest_contract.js`
        /// does for the digest: one table, asserted here, quoted in the JS beside its own copy.
        /// **If the two ever disagree, every bot-filled ranked match pays a different amount than
        /// the game said it would, and nothing logs an error.**
        /// </summary>
        [Fact]
        public void ABlendedRatingIsAFractionOfTheMoveAndOfTheConfidence()
        {
            var before = new RankState { Rating = 1500.0, Deviation = 200.0, Volatility = 0.06 };
            var after = new RankState { Rating = 1560.0, Deviation = 180.0, Volatility = 0.05 };

            var third = RatingRules.Blend(before, after, 1.0 / 3.0);
            Assert.Equal(1520.0, third.Rating, 3);
            Assert.Equal(193.333, third.Deviation, 2);

            // ⚠️ A ZERO-WEIGHT MATCH IS THE ACCOUNT UNTOUCHED, INCLUDING ITS CONFIDENCE.
            Assert.Same(before, RatingRules.Blend(before, after, 0.0));
            Assert.Same(after, RatingRules.Blend(before, after, 1.0));

            // ⚠️ AND THE BLEND NEVER EDITS WHAT IT WAS GIVEN. `RankState` is a class and the
            // endpoint still holds the unblended result for a disputed match.
            Assert.Equal(1560.0, after.Rating, 3);
        }

        [Fact]
        public void TheOfferSaysTheNumberOfBots()
        {
            Assert.Equal("START WITH 1 BOT", BotFillRules.FillOffer(1));
            Assert.Equal("START WITH 3 BOTS", BotFillRules.FillOffer(3));
            Assert.Equal("", BotFillRules.FillOffer(0));
        }
    }

    /// <summary>
    /// PHASE 12: custom games, and the two formats that ride beside a mode rather than becoming
    /// modes.
    /// </summary>
    public class CustomGameTests
    {
        /// <summary>
        /// ⚠️⚠️ THE DEFAULTS ARE THE SHIPPED GAME, ASKED OF THE SHIPPED CONSTANTS. A custom lobby
        /// that opens on anything else is a lobby that silently changes the match for a player who
        /// only wanted to look. If `MatchRules.RoundCountFor` or `Balance.RoundTime` ever move,
        /// this fails rather than the defaults drifting away from the game.
        /// </summary>
        [Fact]
        public void ACustomLobbyOpensOnExactlyWhatTheGameShipsWith()
        {
            var classic = CustomGameRules.Defaults(GameMode.Classic);
            var hero = CustomGameRules.Defaults(GameMode.HeroStrike);

            Assert.Equal(MatchRules.RoundCountFor(GameMode.Classic), classic.Rounds);
            Assert.Equal(MatchRules.RoundCountFor(GameMode.HeroStrike), hero.Rounds);
            Assert.Equal((int)Balance.RoundTime, classic.RoundSeconds);
            Assert.Equal(MatchFormat.Standard, classic.Format);
            Assert.Equal(0, classic.Bots);
            Assert.Equal("", CustomGameRules.Refusal(classic));
        }

        /// <summary>⚠️⚠️ THE ONE RULE IN THE FILE THAT IS NOT NEGOTIABLE. § 105: one ladder, on the
        /// shipped rules.</summary>
        [Fact]
        public void NothingCustomIsEverRanked()
        {
            var rules = CustomGameRules.Defaults(GameMode.HeroStrike);
            Assert.True(CustomGameRules.CanBeRanked(rules));

            foreach (var mutate in new Action<CustomRules>[]
            {
                r => r.Format = MatchFormat.LastTsinelas,
                r => r.Format = MatchFormat.Mirror,
                r => r.Private = true,
                r => r.Bots = 1,
                r => r.Rounds += 1,
                r => r.RoundSeconds -= 10,
                r => r.ScoreTarget = 1000,
            })
            {
                var copy = rules.Clone();
                mutate(copy);
                Assert.False(CustomGameRules.CanBeRanked(copy));
            }
        }

        /// <summary>
        /// ⚠️⚠️ A TSINELAS IS SPENT ON A LOSS AND NEVER ON A THROW, WHICH IS THE FORMAT'S WHOLE
        /// INCENTIVE. See `CustomGameRules.TsinelasLeft`: charging the throw makes never throwing
        /// optimal, and `docs/VISION.md`'s one paragraph is that throwing is free.
        /// </summary>
        [Fact]
        public void LosingTsinelasIsWhatEndsYouAndThrowingIsStillFree()
        {
            Assert.Equal(3, CustomGameRules.TsinelasLeft(3, 0));
            Assert.Equal(1, CustomGameRules.TsinelasLeft(3, 2));
            Assert.Equal(0, CustomGameRules.TsinelasLeft(3, 9));
            Assert.False(CustomGameRules.IsOut(3, 2));
            Assert.True(CustomGameRules.IsOut(3, 3));
        }

        [Fact]
        public void ARoundIsOnlyTakenWhenExactlyOneAttackerIsLeft()
        {
            // Slot 0 is the taya, so its stock is not a survivor either way.
            Assert.Equal(-1, CustomGameRules.LastAttackerStanding(new[] { 0, 2, 2, 0 }, 0));
            Assert.Equal(2, CustomGameRules.LastAttackerStanding(new[] { 0, 0, 1, 0 }, 0));
            Assert.Equal(-1, CustomGameRules.LastAttackerStanding(new[] { 3, 0, 0, 0 }, 0));
        }

        /// <summary>
        /// ⚠️⚠️ EVERY MACHINE COMPUTES THE SAME MIRROR PICK FROM THE SAME WEEK WITH NO SERVICE AT
        /// ALL, which is what makes the format work in a hall with no internet. It also has to
        /// survive a machine with a clock set before the epoch, which happens at venues: C#'s `%`
        /// keeps the sign of the left operand, so a naive version indexes off the front of the
        /// roster and throws.
        /// </summary>
        [Fact]
        public void TheMirrorPickIsTheSameEverywhereAndNeverIndexesOffTheRoster()
        {
            var week0 = RatingRules.SeasonOneStartUtc.AddDays(1);
            var week1 = RatingRules.SeasonOneStartUtc.AddDays(8);
            var before = RatingRules.SeasonOneStartUtc.AddDays(-30);

            Assert.Equal(CustomGameRules.MirrorIndex(6, week0), CustomGameRules.MirrorIndex(6, week0));
            Assert.NotEqual(CustomGameRules.MirrorIndex(6, week0), CustomGameRules.MirrorIndex(6, week1));

            for (int day = -400; day < 400; day += 3)
            {
                int index = CustomGameRules.MirrorIndex(6, RatingRules.SeasonOneStartUtc.AddDays(day));
                Assert.InRange(index, 0, 5);
            }

            Assert.InRange(CustomGameRules.MirrorIndex(6, before), 0, 5);
            Assert.InRange(CustomGameRules.DaysUntilMirrorRotates(week0), 1, 7);
        }

        /// <summary>
        /// ⚠️⚠️ AN OLDER BUILD READING A NEWER RULE SET GETS A PLAYABLE MATCH RATHER THAN AN
        /// EXCEPTION, which is § 70.7's growing-roster rule applied to a record. The test truncates
        /// the wire string field by field, which is exactly what an older parser sees.
        /// </summary>
        [Fact]
        public void AShortOrHostileWireStringStillParsesIntoAPlayableRuleSet()
        {
            var rules = CustomGameRules.Defaults(GameMode.HeroStrike);
            rules.Format = MatchFormat.LastTsinelas;
            rules.Bots = 2;
            rules.Private = true;

            string wire = CustomGameRules.ToWire(rules);
            var back = CustomGameRules.Parse(wire, GameMode.Classic);

            Assert.Equal(rules.Format, back.Format);
            Assert.Equal(rules.Bots, back.Bots);
            Assert.True(back.Private);
            Assert.Equal(GameMode.HeroStrike, back.Mode);

            string[] parts = wire.Split('|');
            for (int keep = 0; keep < parts.Length; keep++)
            {
                var partial = CustomGameRules.Parse(string.Join("|", parts, 0, keep), GameMode.Classic);
                Assert.Equal("", CustomGameRules.Refusal(partial));
            }

            var junk = CustomGameRules.Parse("9999|-4|abc||70000|99|9|9|2", GameMode.Classic);
            Assert.Equal("", CustomGameRules.Refusal(junk));
            Assert.InRange(junk.Bots, 0, CustomGameRules.MaxBots);
            Assert.InRange(junk.Tsinelas, CustomGameRules.MinTsinelas, CustomGameRules.MaxTsinelas);
        }

        /// <summary>⚠️ THE PASSWORD IS HOST-ONLY AND MUST NEVER TRAVEL. A lobby advert is readable
        /// by the whole pool.</summary>
        [Fact]
        public void ThePasswordIsNeverOnTheWire()
        {
            var rules = CustomGameRules.Defaults(GameMode.Classic);
            rules.Password = "kanto1234";

            Assert.DoesNotContain("kanto", CustomGameRules.ToWire(rules));
            Assert.Equal("", CustomGameRules.Parse(CustomGameRules.ToWire(rules), GameMode.Classic).Password);
        }

        [Fact]
        public void AnUnusablePasswordIsRefusedWithASentence()
        {
            var rules = CustomGameRules.Defaults(GameMode.Classic);
            rules.Password = "ab";

            Assert.NotEqual("", CustomGameRules.Refusal(rules));
            Assert.True(CustomGameRules.IsPasswordUsable(""));
            Assert.True(CustomGameRules.IsPasswordUsable("abcd"));
            Assert.False(CustomGameRules.IsPasswordUsable(new string('x', 17)));
        }

        [Fact]
        public void EveryFormatHasAPlayerFacingNameAndOneSentence()
        {
            foreach (MatchFormat format in Enum.GetValues(typeof(MatchFormat)))
            {
                Assert.False(string.IsNullOrWhiteSpace(CustomGameRules.FormatName(format)));
                Assert.False(string.IsNullOrWhiteSpace(CustomGameRules.FormatBlurb(format)));
            }
        }
    }
}
