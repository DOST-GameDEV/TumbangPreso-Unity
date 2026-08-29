using System.Collections.Generic;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class PlayerProfileTests
    {
        private static MatchRecord Match(string id, int winner, params int[] scores)
        {
            var players = new PlayerMatchStats[scores.Length];
            for (int i = 0; i < scores.Length; i++)
                players[i] = new PlayerMatchStats
                {
                    Slot = i,
                    PlayerId = $"player-{i}",
                    Handle = $"Seat {i}#000{i}",
                    CharacterId = i == 0 ? "maring" : "totoy",
                    SlipperId = i == 0 ? "crocs" : "tsinelas",
                    Score = scores[i],
                    ScoreAtFinalRound = scores[i],
                };

            var record = new MatchRecord
            {
                MatchId = id,
                Mode = GameMode.Classic.ToString(),
                MapId = "eskinita",
                Rounds = Balance.Rounds,
                DurationSeconds = 360.0f,
                PlayedUtc = "2026-08-30T00:00:00.0000000Z",
                WinningSlot = winner,
                Players = players,
            };

            MatchRecordRules.AssignPlacements(record);
            return record;
        }

        [Fact]
        public void OneMatchLandsInTheModeItWasPlayedIn()
        {
            var profile = new PlayerProfile();
            Assert.True(ProfileRules.Apply(profile, Match("m1", 0, 400, 300, 200, 100), "player-0"));

            var classic = ProfileRules.ModeFor(profile, GameMode.Classic.ToString()).Totals;
            Assert.Equal(1, classic.Matches);
            Assert.Equal(1, classic.Wins);
            Assert.Equal(1, classic.Placements[0]);
            Assert.Equal(0, ProfileRules.ModeFor(profile, GameMode.HeroStrike.ToString()).Totals.Matches);
        }

        /// <summary>
        /// ⚠️⚠️ THE OFFLINE QUEUE WILL EVENTUALLY SUBMIT THE SAME RECORD TWICE, so this is the
        /// property the whole design rests on rather than a nicety. Without it a career doubles a
        /// match every time the Wi-Fi drops at the wrong moment, silently.
        /// </summary>
        [Fact]
        public void TheSameMatchIdIsNeverCountedTwice()
        {
            var profile = new PlayerProfile();
            var record = Match("m1", 0, 400, 300, 200, 100);

            Assert.True(ProfileRules.Apply(profile, record, "player-0"));
            Assert.False(ProfileRules.Apply(profile, record, "player-0"));

            Assert.Equal(1, ProfileRules.ModeFor(profile, GameMode.Classic.ToString()).Totals.Matches);
        }

        /// <summary>A refused record changes nothing at all, so a replayed queue entry cannot
        /// leave a career half-counted.</summary>
        [Fact]
        public void ARefusedRecordLeavesEveryTotalUntouched()
        {
            var profile = new PlayerProfile();
            ProfileRules.Apply(profile, Match("m1", 0, 400, 300, 200, 100), "player-0");

            var totals = ProfileRules.ModeFor(profile, GameMode.Classic.ToString()).Totals;
            int matches = totals.Matches, wins = totals.Wins, score = totals.TotalScore;

            Assert.False(ProfileRules.Apply(profile, Match("m1", 0, 400, 300, 200, 100), "player-0"));
            Assert.False(ProfileRules.Apply(profile, Match("m2", 0, 400, 300, 200, 100), "nobody"));

            Assert.Equal(matches, totals.Matches);
            Assert.Equal(wins, totals.Wins);
            Assert.Equal(score, totals.TotalScore);
        }

        [Fact]
        public void AMatchWithoutAnIdIsRefusedBecauseItCannotBeDeduplicated()
        {
            var profile = new PlayerProfile();
            var record = Match("", 0, 400, 300, 200, 100);
            Assert.False(ProfileRules.Apply(profile, record, "player-0"));
        }

        /// <summary>⚠️ A DRAW BREAKS A STREAK. `Scoreboard.WinningSlot` returns -1 for a tie at
        /// the top and calls it an honest draw; a streak surviving one claims a win the rules
        /// refused to award.</summary>
        [Fact]
        public void ADrawBreaksTheWinStreakAndCountsAsADraw()
        {
            var profile = new PlayerProfile();
            ProfileRules.Apply(profile, Match("m1", 0, 400, 100, 100, 100), "player-0");
            ProfileRules.Apply(profile, Match("m2", 0, 400, 100, 100, 100), "player-0");
            ProfileRules.Apply(profile, Match("m3", -1, 400, 400, 100, 100), "player-0");

            var t = ProfileRules.ModeFor(profile, GameMode.Classic.ToString()).Totals;
            Assert.Equal(2, t.LongestWinStreak);
            Assert.Equal(0, t.CurrentWinStreak);
            Assert.Equal(1, t.Draws);
            Assert.Equal(2, t.Wins);
        }

        /// <summary>
        /// ⚠️ THE DENOMINATOR IS COUNTED WHETHER OR NOT THE COMEBACK LANDED. Counting only the
        /// successes gives a player who has never been behind an undefined rate and a player who
        /// came back once from one chance a perfect one.
        /// </summary>
        [Fact]
        public void EveryComebackChanceIsCountedNotOnlyTheSuccessfulOnes()
        {
            var profile = new PlayerProfile();

            var won = Match("m1", 0, 400, 300, 200, 100);
            won.Players[0].ScoreAtFinalRound = 0;
            ProfileRules.Apply(profile, won, "player-0");

            var lost = Match("m2", 1, 100, 400, 300, 200);
            lost.Players[0].ScoreAtFinalRound = 0;
            ProfileRules.Apply(profile, lost, "player-0");

            var t = ProfileRules.ModeFor(profile, GameMode.Classic.ToString()).Totals;
            Assert.Equal(2, t.ComebackChances);
            Assert.Equal(1, t.Clutches);
            Assert.Equal(0.5f, ProfileRules.ClutchRate(t));
        }

        /// <summary>⚠️ A never-threw match must not be averaged in as zero. The denominator is
        /// matches with a throw, not matches.</summary>
        [Fact]
        public void AMatchWithNoThrowIsLeftOutOfTheFirstThrowAverage()
        {
            var profile = new PlayerProfile();

            var threw = Match("m1", 0, 400, 300, 200, 100);
            threw.Players[0].TimeToFirstThrow = 10.0f;
            threw.Players[0].Throws = 5;
            ProfileRules.Apply(profile, threw, "player-0");

            var silent = Match("m2", 0, 400, 300, 200, 100);
            silent.Players[0].TimeToFirstThrow = -1.0f;
            ProfileRules.Apply(profile, silent, "player-0");

            var t = ProfileRules.ModeFor(profile, GameMode.Classic.ToString()).Totals;
            Assert.Equal(1, t.MatchesWithAThrow);
            Assert.Equal(10.0f, ProfileRules.AverageTimeToFirstThrow(t));
        }

        [Fact]
        public void ClassicAndHeroStrikeNeverShareATotal()
        {
            var profile = new PlayerProfile();
            var classic = Match("m1", 0, 400, 300, 200, 100);
            var hero = Match("m2", 0, 400, 300, 200, 100);
            hero.Mode = GameMode.HeroStrike.ToString();

            ProfileRules.Apply(profile, classic, "player-0");
            ProfileRules.Apply(profile, hero, "player-0");

            Assert.Equal(1, ProfileRules.ModeFor(profile, GameMode.Classic.ToString()).Totals.Matches);
            Assert.Equal(1, ProfileRules.ModeFor(profile, GameMode.HeroStrike.ToString()).Totals.Matches);
        }

        [Fact]
        public void CharacterAndSlipperRecordsCountGamesAndWinsSeparately()
        {
            var profile = new PlayerProfile();
            ProfileRules.Apply(profile, Match("m1", 0, 400, 300, 200, 100), "player-0");
            ProfileRules.Apply(profile, Match("m2", 1, 100, 400, 300, 200), "player-0");

            var maring = ProfileRules.Favourite(profile.Characters);
            Assert.Equal("maring", maring.Id);
            Assert.Equal(2, maring.Games);
            Assert.Equal(1, maring.Wins);

            var crocs = ProfileRules.Favourite(profile.Slippers);
            Assert.Equal("crocs", crocs.Id);
            Assert.Equal(2, crocs.Games);
        }

        [Fact]
        public void HistoryKeepsTheNewestHundredAndRefusesADuplicate()
        {
            var history = new List<MatchRecord>();
            for (int i = 0; i < ProfileRules.HistoryLimit + 5; i++)
                history = ProfileRules.Remember(history, Match($"m{i}", 0, 400, 300, 200, 100));

            Assert.Equal(ProfileRules.HistoryLimit, history.Count);
            Assert.Equal($"m{ProfileRules.HistoryLimit + 4}", history[0].MatchId);

            int before = history.Count;
            history = ProfileRules.Remember(history, Match($"m{ProfileRules.HistoryLimit + 4}", 0, 400, 300, 200, 100));
            Assert.Equal(before, history.Count);
        }

        /// <summary>
        /// ⚠️ THE REPLAY WINDOW HAS TO OUTLIVE THE HISTORY, or the record that was just rolled
        /// into the totals becomes resubmittable. It is twice the history limit for that reason.
        /// </summary>
        [Fact]
        public void TheReplayWindowOutlivesTheHistory()
        {
            Assert.True(ProfileRules.AppliedIdMemory > ProfileRules.HistoryLimit);

            var profile = new PlayerProfile();
            for (int i = 0; i < ProfileRules.AppliedIdMemory + 10; i++)
                ProfileRules.Apply(profile, Match($"m{i}", 0, 400, 300, 200, 100), "player-0");

            Assert.Equal(ProfileRules.AppliedIdMemory, profile.AppliedMatchIds.Count);
        }

        /// <summary>
        /// ⚠️ THE PLACEMENT ARRAY IS THE ONE FIELD THAT CAN ARRIVE THE WRONG LENGTH AND CRASH A
        /// READER. A profile is JSON written by a server and kept for months, and the career
        /// screen indexes it to `Balance.PlayerCount`. It is resized rather than replaced, so a
        /// short array keeps the counts it does carry.
        /// </summary>
        [Fact]
        public void AShortPlacementArrayIsResizedRatherThanDiscarded()
        {
            var profile = new PlayerProfile();
            var mode = ProfileRules.ModeFor(profile, GameMode.Classic.ToString());
            mode.Totals.Placements = new[] { 7, 3 };

            ProfileRules.Normalise(profile);

            Assert.Equal(Balance.PlayerCount, mode.Totals.Placements.Length);
            Assert.Equal(7, mode.Totals.Placements[0]);
            Assert.Equal(3, mode.Totals.Placements[1]);
            Assert.Equal(0, mode.Totals.Placements[Balance.PlayerCount - 1]);
        }

        [Fact]
        public void NormaliseFillsInEveryListAProfileScreenWalks()
        {
            var profile = new PlayerProfile
            {
                Modes = null, Characters = null, Slippers = null,
                AppliedMatchIds = null, Inventory = null, Level = 0,
            };

            ProfileRules.Normalise(profile);

            Assert.NotNull(profile.Modes);
            Assert.NotNull(profile.Characters);
            Assert.NotNull(profile.Slippers);
            Assert.NotNull(profile.AppliedMatchIds);
            Assert.NotNull(profile.Inventory);
            Assert.Equal(1, profile.Level);
        }

        [Fact]
        public void DistanceIsReportedPerRoundSoTheTwoModesCompare()
        {
            var t = new CareerTotals { Matches = 2, DistanceTravelled = 800.0f };
            Assert.Equal(100.0f, ProfileRules.DistancePerRound(t, Balance.Rounds));
        }

        /// <summary>⚠️ Phase 2 carries the progression FIELDS and awards nothing. Phase 4 owns
        /// the curve; a profile written today must read as unranked and level 1.</summary>
        [Fact]
        public void PlayingAMatchAwardsNoXpAndNoRank()
        {
            var profile = new PlayerProfile();
            ProfileRules.Apply(profile, Match("m1", 0, 400, 300, 200, 100), "player-0");

            Assert.Equal(1, profile.Level);
            Assert.Equal(0, profile.Xp);
            Assert.Equal("", profile.RankTier);
            Assert.Empty(profile.Inventory);
        }
    }
}
