using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public class IntegrityTests
    {
        private static MatchRecord Honest()
        {
            var record = new MatchRecord
            {
                MatchId = "m-0001",
                Mode = GameMode.Classic.ToString(),
                MapId = "eskinita",
                Rounds = 4,
                DurationSeconds = 400.0f,
                PlayedUtc = "2026-08-31T10:00:00Z",
                Players = new[]
                {
                    new PlayerMatchStats { Slot = 0, PlayerId = "alpha", CharacterId = "dante",  Score = 1200, Throws = 20, Knockdowns = 5, Retrievals = 14, ShoveAttempts = 4, ShoveHits = 2 },
                    new PlayerMatchStats { Slot = 1, PlayerId = "bravo", CharacterId = "cheska", Score = 900,  Throws = 18, Knockdowns = 3, Retrievals = 12 },
                    new PlayerMatchStats { Slot = 2, PlayerId = "chloe", CharacterId = "sean",   Score = 700,  Throws = 15, Knockdowns = 2, Retrievals = 10 },
                    new PlayerMatchStats { Slot = 3, PlayerId = "delta", CharacterId = "zack",   Score = 400,  Throws = 12, Knockdowns = 1, Retrievals = 8 },
                },
            };

            MatchRecordRules.AssignPlacements(record);
            record.WinningSlot = 0;
            return record;
        }

        // ------------------------------------------------------------------------------
        // The digest
        // ------------------------------------------------------------------------------

        [Fact]
        public void TwoHonestPeersProduceTheSameDigestForTheSameMatch()
        {
            Assert.Equal(IntegrityRules.Digest(Honest()), IntegrityRules.Digest(Honest()));
            Assert.Equal(16, IntegrityRules.Digest(Honest()).Length);
        }

        /// <summary>
        /// ⚠️⚠️ THE ATTACK THIS WHOLE PHASE EXISTS FOR: a host that plays honestly and then
        /// submits a better scoreboard. One changed score is one changed digest.
        /// </summary>
        [Fact]
        public void AnAlteredScoreChangesTheDigestAndTheMatchIsDisputed()
        {
            var honest = Honest();
            var liar = Honest();
            liar.Players[3].Score = 5000;
            MatchRecordRules.AssignPlacements(liar);
            liar.WinningSlot = 3;

            Assert.NotEqual(IntegrityRules.Digest(honest), IntegrityRules.Digest(liar));

            var submissions = new List<string>
            {
                IntegrityRules.Digest(liar),
                IntegrityRules.Digest(honest),
                IntegrityRules.Digest(honest),
                IntegrityRules.Digest(honest),
            };

            Assert.Equal(ResultVerdict.Disputed, IntegrityRules.Corroborate(submissions));
        }

        [Fact]
        public void OneSubmissionIsPendingAndTwoAgreeingIsWitnessed()
        {
            string d = IntegrityRules.Digest(Honest());

            Assert.Equal(ResultVerdict.Pending, IntegrityRules.Corroborate(new List<string> { d }));
            Assert.Equal(ResultVerdict.Witnessed, IntegrityRules.Corroborate(new List<string> { d, d }));
            Assert.Equal(ResultVerdict.Pending, IntegrityRules.Corroborate(new List<string>()));
        }

        /// <summary>
        /// ⚠️ A DISAGREEMENT BEATS A MAJORITY. Three agreeing and one dissenting is disputed, not
        /// witnessed: a vote would let three colluding players ratify anything.
        /// </summary>
        [Fact]
        public void AMajorityDoesNotOverruleADissenter()
        {
            string good = IntegrityRules.Digest(Honest());
            var forged = Honest();
            forged.Players[0].Score = 9999;
            MatchRecordRules.AssignPlacements(forged);

            var submissions = new List<string> { good, good, good, IntegrityRules.Digest(forged) };
            Assert.Equal(ResultVerdict.Disputed, IntegrityRules.Corroborate(submissions));
        }

        /// <summary>
        /// ⚠️ A MISSING SUBMISSION IS SILENCE, NOT A DISPUTE. A phone losing signal at the whistle
        /// is indistinguishable from a client refusing to corroborate, and punishing the first is
        /// how a network problem becomes a cheating accusation.
        /// </summary>
        [Fact]
        public void AMissingSubmissionIsSilenceRatherThanAnAccusation()
        {
            string d = IntegrityRules.Digest(Honest());
            Assert.Equal(ResultVerdict.Witnessed,
                         IntegrityRules.Corroborate(new List<string> { d, "", d, null }));
        }

        /// <summary>
        /// ⚠️⚠️ THE PER-MACHINE MEASUREMENTS ARE DELIBERATELY OUTSIDE THE DIGEST. Two honest peers
        /// sample distance travelled and time-to-first-throw off their own frame timing and never
        /// agree exactly. If those were in the digest, every match in the game would be disputed
        /// and the mechanism would be switched off inside a week.
        /// </summary>
        [Fact]
        public void PerMachineMeasurementsDoNotDisputeAMatch()
        {
            var a = Honest();
            var b = Honest();

            b.Players[0].DistanceTravelled = 522.4f;
            b.Players[0].TimeToFirstThrow = 3.117f;
            b.Players[0].DefenceTicks = 91;
            b.DurationSeconds = 400.03f;

            Assert.Equal(IntegrityRules.Digest(a), IntegrityRules.Digest(b));
        }

        // ------------------------------------------------------------------------------
        // Sanity
        // ------------------------------------------------------------------------------

        [Fact]
        public void AnHonestRecordPassesEverySanityCheck()
        {
            Assert.Equal(SanityFault.None, IntegrityRules.Check(Honest()));
        }

        [Fact]
        public void TheImpossibleIsRefusedBeforeAnybodyIsAskedToWitnessIt()
        {
            var noId = Honest();
            noId.MatchId = "";
            Assert.Equal(SanityFault.NoMatchId, IntegrityRules.Check(noId));

            var tooManyKnockdowns = Honest();
            tooManyKnockdowns.Players[0].Knockdowns = 999;
            Assert.Equal(SanityFault.MoreKnockdownsThanThrows, IntegrityRules.Check(tooManyKnockdowns));

            var tooManyRetrievals = Honest();
            tooManyRetrievals.Players[1].Retrievals = 500;
            Assert.Equal(SanityFault.MoreRetrievalsThanThrows, IntegrityRules.Check(tooManyRetrievals));

            var impossibleHits = Honest();
            impossibleHits.Players[0].ShoveHits = 99;
            Assert.Equal(SanityFault.MoreHitsThanAttempts, IntegrityRules.Check(impossibleHits));

            var longMatch = Honest();
            longMatch.DurationSeconds = 99999.0f;
            Assert.Equal(SanityFault.ImpossibleDuration, IntegrityRules.Check(longMatch));

            var teleport = Honest();
            teleport.Players[2].DistanceTravelled = 900000.0f;
            Assert.Equal(SanityFault.ImpossibleTravel, IntegrityRules.Check(teleport));

            var hugeScore = Honest();
            hugeScore.Players[0].Score = int.MaxValue / 2;
            MatchRecordRules.AssignPlacements(hugeScore);
            Assert.Equal(SanityFault.ImpossibleScore, IntegrityRules.Check(hugeScore));
        }

        /// <summary>
        /// ⚠️ REWRITING A PLACEMENT WITHOUT REWRITING THE SCORE IT CAME FROM IS REFUSED, which is
        /// what makes the digest's placement field pointless to forge on its own.
        /// </summary>
        [Fact]
        public void APlacementThatDisagreesWithTheScoresIsRefused()
        {
            var record = Honest();
            record.Players[3].Placement = 0;

            Assert.Equal(SanityFault.PlacementsDisagreeWithScores, IntegrityRules.Check(record));
        }

        /// <summary>
        /// ⚠️⚠️ THE CEILING HAS TO BE UNREACHABLE, NOT REALISTIC. Refusing a real result is worse
        /// than accepting a modest lie: the modest lie is caught by the digest, and the refusal is
        /// a player being told their best game never happened. A whole match of uncontested
        /// passive defence is 900 points a round in this game (`docs/Design.md`, the known balance
        /// risk), so an eight-round Hero Strike blowout has to fit under the bound comfortably.
        /// </summary>
        [Fact]
        public void TheScoreCeilingClearsTheBestMatchAnybodyCouldActuallyPlay()
        {
            int ceiling = IntegrityRules.ScoreCeiling(8, 8 * Balance.RoundTime);
            int uncontestedDefence = 8 * 900;

            Assert.True(ceiling > uncontestedDefence * 2,
                        $"ceiling {ceiling} must clear a full eight rounds of uncontested defence ({uncontestedDefence}) with room to spare");
        }

        // ------------------------------------------------------------------------------
        // Leavers and cooldowns
        // ------------------------------------------------------------------------------

        [Fact]
        public void ComingBackInsideTheWindowIsNotALeaveAtAll()
        {
            Assert.False(IntegrityRules.IsAbandon(DepartureKind.Completed));
            Assert.False(IntegrityRules.IsAbandon(DepartureKind.Returned));
            Assert.True(IntegrityRules.IsAbandon(DepartureKind.Announced));
            Assert.True(IntegrityRules.IsAbandon(DepartureKind.Dropped));

            Assert.True(IntegrityRules.ReconnectWindowSeconds > Balance.RoundTime,
                        "the window has to outlast a round or a Wi-Fi handover is a leave");
        }

        /// <summary>
        /// ⚠️ THE FIRST ABANDON COSTS NOTHING. This audience is students on home connections in
        /// Metro Manila, and one lost match is a doorbell or a brownout. The escalation is aimed
        /// at a habit.
        /// </summary>
        [Fact]
        public void CooldownsEscalateAndTheFirstOneIsFree()
        {
            Assert.Equal(0, IntegrityRules.CooldownFor(0));
            Assert.Equal(0, IntegrityRules.CooldownFor(-3));
            Assert.Equal(120, IntegrityRules.CooldownFor(1));
            Assert.Equal(600, IntegrityRules.CooldownFor(2));
            Assert.Equal(3600, IntegrityRules.CooldownFor(4));

            // And it stops climbing rather than reaching a day.
            Assert.Equal(3600, IntegrityRules.CooldownFor(50));

            for (int i = 1; i < IntegrityRules.CooldownSeconds.Length; i++)
                Assert.True(IntegrityRules.CooldownSeconds[i] > IntegrityRules.CooldownSeconds[i - 1]);
        }

        /// <summary>
        /// ⚠️ A DEAD BUTTON IS A BUG REPORT AND A BUTTON THAT SAYS WHY IS A CONSEQUENCE.
        /// `CLAUDE.md` § 6.3: a dead end is a bug.
        /// </summary>
        [Fact]
        public void ACooldownExplainsItselfInWords()
        {
            Assert.Equal("", IntegrityRules.CooldownLabel(0));
            Assert.Contains("left a match early", IntegrityRules.CooldownLabel(120));
            Assert.Contains("2 minutes", IntegrityRules.CooldownLabel(120));
            Assert.Contains("a minute", IntegrityRules.CooldownLabel(30));
        }

        [Fact]
        public void AbandonsAreForgottenAfterAWeek()
        {
            Assert.Equal(7, IntegrityRules.AbandonMemoryDays);
        }

        /// <summary>
        /// ⚠️ A RANKED ABANDON IS SCORED AS FINISHING LAST AND NOT AS A SEPARATE PENALTY NUMBER.
        /// `FUTURE.md` § 9 has no leaver-specific rating arithmetic, and inventing one would be a
        /// second tuning surface for something the pairwise expansion already says.
        /// </summary>
        [Fact]
        public void ARankedAbandonIsScoredAsFinishingLast()
        {
            Assert.Equal(4, IntegrityRules.AbandonPlacement);

            var before = new[]
            {
                new RankState { Rating = 1500, Deviation = 50 },
                new RankState { Rating = 1500, Deviation = 50 },
                new RankState { Rating = 1500, Deviation = 50 },
                new RankState { Rating = 1500, Deviation = 50 },
            };

            var after = RatingRules.UpdateAll(before, new[] { IntegrityRules.AbandonPlacement, 1, 2, 3 });
            Assert.True(after[0].Rating < 1500.0, "leaving a ranked match costs rating");
        }

        // ------------------------------------------------------------------------------
        // Rate limits
        // ------------------------------------------------------------------------------

        /// <summary>
        /// ⚠️ A FREE TIER IS A BUDGET AN ABUSIVE CLIENT CAN SPEND (`FUTURE.md` § 19.8 step 5). The
        /// floor has to be invisible to an honest player: a real match is minutes long, so five
        /// seconds between career writes never touches anybody who is actually playing.
        /// </summary>
        [Fact]
        public void TheWriteFloorIsInvisibleToAnybodyActuallyPlaying()
        {
            Assert.True(IntegrityRules.WriteFloorSeconds < Balance.RoundTime);
            Assert.True(IntegrityRules.WritesPerHour * IntegrityRules.WriteFloorSeconds <= 3600);
            Assert.True(IntegrityRules.ReportsPerDay > 0);
        }
    }
}
