using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class MatchRecordTests
    {
        private static MatchRecord Match(params int[] scores)
        {
            var players = new PlayerMatchStats[scores.Length];
            for (int i = 0; i < scores.Length; i++)
                players[i] = new PlayerMatchStats
                {
                    Slot = i,
                    PlayerId = $"player-{i}",
                    Handle = $"Seat {i}#000{i}",
                    Score = scores[i],
                };

            var record = new MatchRecord
            {
                MatchId = "match-1",
                Mode = GameMode.Classic.ToString(),
                MapId = "eskinita",
                Rounds = Balance.Rounds,
                DurationSeconds = Balance.Rounds * Balance.RoundTime,
                PlayedUtc = "2026-08-30T00:00:00.0000000Z",
                Players = players,
            };

            MatchRecordRules.AssignPlacements(record);
            record.WinningSlot = Winner(scores);
            return record;
        }

        private static int Winner(int[] scores)
        {
            int best = -1, bestScore = int.MinValue;
            bool tied = false;
            for (int i = 0; i < scores.Length; i++)
            {
                if (scores[i] > bestScore) { bestScore = scores[i]; best = i; tied = false; }
                else if (scores[i] == bestScore) tied = true;
            }
            return tied ? -1 : best;
        }

        [Fact]
        public void PlacementsUseCompetitionRankingSoATieDoesNotConsumeTheNextPlace()
        {
            Assert.Equal(new[] { 1, 2, 2, 4 }, MatchRecordRules.Placements(new[] { 400, 300, 300, 100 }));
        }

        /// <summary>
        /// ⚠️ THE BOARD BREAKS THIS TIE BY SEAT ORDER AND A PLACEMENT MUST NOT.
        /// `MatchDirector.Ranking` sorts equal scores by slot so the rows do not reshuffle
        /// between frames; that is a drawing decision. Two players on the same score placed
        /// equally, or the seat that happened to be round 1's taya is handed a better career.
        /// </summary>
        [Fact]
        public void EverySeatOnTheSameScoreGetsTheSamePlacement()
        {
            Assert.Equal(new[] { 1, 1, 1, 1 }, MatchRecordRules.Placements(new[] { 250, 250, 250, 250 }));
        }

        [Fact]
        public void AssignPlacementsWritesThroughToEveryLine()
        {
            var record = Match(500, 100, 300, 300);
            Assert.Equal(1, record.Players[0].Placement);
            Assert.Equal(4, record.Players[1].Placement);
            Assert.Equal(2, record.Players[2].Placement);
            Assert.Equal(2, record.Players[3].Placement);
        }

        /// <summary>
        /// ⚠️ THE STAT IS DERIVED, NOT RAISED. `FUTURE.md` § 19.2 check 4: there is no `Clutch`
        /// score event and nothing should go looking for one. This is the whole derivation.
        /// </summary>
        [Fact]
        public void AClutchIsAWinFromLastPlaceEnteringTheFinalRound()
        {
            var record = Match(400, 380, 350, 300);
            record.Players[0].ScoreAtFinalRound = 100;
            record.Players[1].ScoreAtFinalRound = 380;
            record.Players[2].ScoreAtFinalRound = 350;
            record.Players[3].ScoreAtFinalRound = 300;

            Assert.True(MatchRecordRules.IsClutch(record, 0));
            Assert.False(MatchRecordRules.IsClutch(record, 1));
        }

        [Fact]
        public void LeadingIntoTheFinalRoundAndWinningIsNotAClutch()
        {
            var record = Match(400, 380, 350, 300);
            record.Players[0].ScoreAtFinalRound = 390;
            record.Players[1].ScoreAtFinalRound = 380;
            record.Players[2].ScoreAtFinalRound = 350;
            record.Players[3].ScoreAtFinalRound = 300;

            Assert.False(MatchRecordRules.IsClutch(record, 0));
        }

        /// <summary>Tied-last is still last: both were losing the final round, and refusing the
        /// tie would make the stat depend on somebody else's score matching yours.</summary>
        [Fact]
        public void TiedLastEnteringTheFinalRoundStillCounts()
        {
            var record = Match(400, 380, 350, 300);
            record.Players[0].ScoreAtFinalRound = 100;
            record.Players[1].ScoreAtFinalRound = 380;
            record.Players[2].ScoreAtFinalRound = 350;
            record.Players[3].ScoreAtFinalRound = 100;

            Assert.True(MatchRecordRules.IsClutch(record, 0));
        }

        [Fact]
        public void ADrawIsNeverAClutchBecauseNobodyWon()
        {
            var record = Match(400, 400, 350, 300);
            Assert.Equal(-1, record.WinningSlot);
            record.Players[0].ScoreAtFinalRound = 0;
            Assert.False(MatchRecordRules.IsClutch(record, 0));
        }

        /// <summary>
        /// ⚠️ THE RATE IS NEVER STORED, SO THE DIVIDER IS THE ONLY PLACE THE EMPTY CASE IS
        /// DECIDED. A NaN reaching the profile screen prints "NaN%", which is how a stat page
        /// tells a player it is broken.
        /// </summary>
        [Fact]
        public void AnEmptyDenominatorIsZeroRatherThanNaN()
        {
            Assert.Equal(0.0f, MatchRecordRules.Rate(5, 0));
            Assert.False(float.IsNaN(MatchRecordRules.Rate(0, 0)));
        }

        /// <summary>`FUTURE.md` § 2.2: do not show a stat you will not defend.</summary>
        [Fact]
        public void ARateBelowTheSampleFloorIsNotReportable()
        {
            Assert.False(MatchRecordRules.IsReportable(MatchRecordRules.MinimumSampleForARate - 1));
            Assert.True(MatchRecordRules.IsReportable(MatchRecordRules.MinimumSampleForARate));
        }

        /// <summary>
        /// ⚠️ THE TICK IS STORED AND THE SECOND IS DERIVED, so a record written today still reads
        /// correctly if `Balance.DefenseTickInterval` ever moves.
        /// </summary>
        [Fact]
        public void PassiveDefenceSecondsComeFromTheTickInterval()
        {
            var line = new PlayerMatchStats { DefenceTicks = 45 };
            Assert.Equal(45 * Balance.DefenseTickInterval, MatchRecordRules.PassiveDefenceSeconds(line));
        }

        /// <summary>
        /// ⚠️ THE THRESHOLD IS DERIVED FROM TWO MEASURED CONSTANTS AND MUST STAY THAT WAY. If
        /// somebody replaces it with a literal, this fails: the lunge covers
        /// `LungeSpeed²/(2·Friction)` and then sweeps `LungeTagRadius`.
        /// </summary>
        [Fact]
        public void ThePressureRadiusIsTheTayasStandingLungeReach()
        {
            float dash = Balance.LungeSpeed * Balance.LungeSpeed / (2.0f * Balance.Friction);
            Assert.Equal(MatchRecordRules.PressureRadius, dash + Balance.LungeTagRadius, 3);
            Assert.Equal(2.3f, MatchRecordRules.PressureRadius, 2);
        }

        [Fact]
        public void NormaliseClampsCountsThatCannotBeTrueOfEachOther()
        {
            var record = Match(100, 0, 0, 0);
            record.Players[0].Retrievals = 2;
            record.Players[0].RetrievalsUnderPressure = 9;
            record.Players[0].ShoveHits = 7;
            record.Players[0].ShoveAttempts = 1;
            record.Players[0].RoundsDefended = 99;

            MatchRecordRules.Normalise(record);

            Assert.Equal(2, record.Players[0].RetrievalsUnderPressure);
            Assert.Equal(7, record.Players[0].ShoveAttempts);
            Assert.Equal(record.Rounds, record.Players[0].RoundsDefended);
        }

        /// <summary>-1 is "never threw" and survives normalisation; 0 would report the most
        /// passive player in the room as the most aggressive.</summary>
        [Fact]
        public void NeverThrewSurvivesNormalisationAsMinusOne()
        {
            var record = Match(0, 0, 0, 0);
            record.Players[0].TimeToFirstThrow = -1.0f;
            record.Players[1].TimeToFirstThrow = 99999.0f;

            MatchRecordRules.Normalise(record);

            Assert.Equal(-1.0f, record.Players[0].TimeToFirstThrow);
            Assert.Equal(record.DurationSeconds, record.Players[1].TimeToFirstThrow);
        }

        [Fact]
        public void ABotIsNeverMatchedByPlayerId()
        {
            var record = Match(100, 0, 0, 0);
            record.Players[1].IsBot = true;
            record.Players[1].PlayerId = "player-1";

            Assert.Null(MatchRecordRules.LineFor(record, "player-1"));
            Assert.NotNull(MatchRecordRules.LineFor(record, "player-0"));
        }
    }
}
