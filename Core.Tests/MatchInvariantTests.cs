using System;
using System.Collections.Generic;
using System.Linq;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The match invariants, and a seeded fuzzer over legal and near-legal event orders.
    ///
    /// ⚠️⚠️ THE FUZZER IS THE HALF THAT FINDS THINGS NOBODY THOUGHT OF, AND IT IS SEEDED SO A
    /// FINDING CAN BE REPRODUCED. `BotBehaviourProbe`'s own header carries the rule this follows:
    /// *"It is seeded. Do not change the seed to make a run pass; if a run goes red, change the
    /// code."* Every failure message below prints the seed that produced it.
    ///
    /// ⚠️ IT FUZZES ORDERINGS, NOT ONLY VALUES. A client already serialises its inputs perfectly,
    /// so feeding the rules random legal numbers proves very little. What actually breaks a
    /// networked match is a message arriving TWICE, LATE, or BEFORE the one that should precede
    /// it, so the sequences below deliberately include duplicates, stale replays and swaps.
    /// </summary>
    public class MatchInvariantTests
    {
        private static string[] FourSeats(params string[] owners)
        {
            var seats = new string[Balance.PlayerCount];
            for (int i = 0; i < owners.Length && i < seats.Length; i++) seats[i] = owners[i];
            return seats;
        }

        private static MatchSnapshot Legal(int round, int[] scores = null, bool inProgress = true,
                                           bool buffer = false, string[] owners = null)
            => new MatchSnapshot(round, 4, MatchRules.DefenderSlotFor(round), inProgress, buffer,
                                 scores ?? new int[Balance.PlayerCount],
                                 owners ?? FourSeats("a", "b", "c", "d"));

        // ------------------------------------------------------------------
        // The single-state invariants
        // ------------------------------------------------------------------

        [Fact]
        public void AnOrdinaryRoundHasNothingWrongWithIt()
        {
            for (int round = 1; round <= 4; round++)
                Assert.Empty(MatchInvariants.Check(Legal(round)));
        }

        [Fact]
        public void ASecondTayaIsCaught()
        {
            // Round 2 derives seat 1. A peer holding seat 0 is the two-tayas-on-two-screens fault.
            var s = new MatchSnapshot(2, 4, 0, true, false, new int[4], FourSeats("a", "b", "c", "d"));
            var faults = MatchInvariants.Check(s);

            Assert.Single(faults);
            Assert.Contains("derives seat 1", faults[0]);
        }

        [Fact]
        public void EveryRoundOfEveryShippedFormatDerivesExactlyOneTayaInsideTheFourSeats()
        {
            // ⚠️ BOTH MODES, because Hero Strike plays two complete rotations and the second one
            // is where an accumulated role would first disagree with a derived one.
            foreach (var mode in new[] { GameMode.Classic, GameMode.HeroStrike })
            {
                int rounds = MatchRules.RoundCountFor(mode);
                var defended = new HashSet<int>();

                for (int round = 1; round <= rounds; round++)
                {
                    int taya = MatchRules.DefenderSlotFor(round);
                    Assert.InRange(taya, 0, Balance.PlayerCount - 1);
                    defended.Add(taya);

                    Assert.Empty(MatchInvariants.Check(
                        new MatchSnapshot(round, rounds, taya, true, false,
                                          new int[Balance.PlayerCount],
                                          FourSeats("a", "b", "c", "d"))));
                }

                // Everybody defends, and in Classic exactly once each.
                Assert.Equal(Balance.PlayerCount, defended.Count);
            }
        }

        [Fact]
        public void PlayingPastTheLastRoundIsCaught()
        {
            var s = new MatchSnapshot(5, 4, MatchRules.DefenderSlotFor(5), true, false,
                                      new int[4], FourSeats("a", "b", "c", "d"));
            Assert.Contains(MatchInvariants.Check(s), f => f.Contains("round 5 of 4"));
        }

        [Fact]
        public void ANegativeTotalIsCaughtBecauseTheScoreboardClampsAtZero()
        {
            var s = Legal(1, new[] { -10, 0, 0, 0 });
            Assert.Contains(MatchInvariants.Check(s), f => f.Contains("not written through Scoreboard.Add"));
        }

        [Fact]
        public void TheClampIsRealSoAPenaltyCannotDriveASeatNegative()
        {
            // The invariant above is only worth asserting because the mutator actually holds the
            // floor. This is that half.
            var board = new Scoreboard();
            for (int i = 0; i < 20; i++) board.Add(0, ScoreEvent.TayaCampPenalty);
            Assert.Equal(0, board[0]);
        }

        [Fact]
        public void OnePlayerCannotHoldTwoSeats()
        {
            var faults = MatchInvariants.CheckSeatOwnership(FourSeats("a", "b", "a", "d"));
            Assert.Single(faults);
            Assert.Contains("owns seat 0 and seat 2", faults[0]);
        }

        [Fact]
        public void AnEmptySeatIsNotAnOwner()
        {
            // Two unclaimed seats are two unclaimed seats, not one player holding both.
            Assert.Empty(MatchInvariants.CheckSeatOwnership(FourSeats("a", "", null, "d")));
        }

        [Fact]
        public void AnIntermissionOnAFinishedMatchIsCaught()
        {
            var s = new MatchSnapshot(4, 4, 3, false, true, new int[4], FourSeats("a"));
            Assert.Contains(MatchInvariants.Check(s), f => f.Contains("not in progress"));
        }

        // ------------------------------------------------------------------
        // The transition invariants
        // ------------------------------------------------------------------

        [Fact]
        public void AdvancingOneRoundIsLegal()
        {
            Assert.Empty(MatchInvariants.CheckTransition(Legal(1), Legal(2)));
        }

        [Fact]
        public void ASkippedRoundIsCaught()
        {
            var faults = MatchInvariants.CheckTransition(Legal(1), Legal(3));
            Assert.Contains(faults, f => f.Contains("jumped from 1 to 3"));
        }

        [Fact]
        public void AStaleSnapshotGoingBackwardsIsCaught()
        {
            // docs/TODO.md § 82: a packet the host wrote before its own arena loaded.
            var before = Legal(3, new[] { 100, 0, 0, 0 });
            var after = Legal(2, new[] { 100, 0, 0, 0 });

            Assert.Contains(MatchInvariants.CheckTransition(before, after),
                            f => f.Contains("A stale snapshot was applied"));
        }

        [Fact]
        public void ARestartIsTheOneLegalWayBackwards()
        {
            var before = Legal(4, new[] { 900, 100, 0, 0 });
            var after = Legal(1, new int[4]);

            // ⚠️⚠️ THE CALLER DECLARES THE RESTART AND THE CHECKER NEVER GUESSES IT. The seeded
            // fuzzer below broke the guessing version on its 117th trial, and the state it
            // produced is `docs/TODO.md` § 82's packet: round 1 with four zero scores, written by
            // a host before its arena loaded and arriving at a client mid-match. Undeclared, it
            // must be caught; declared, it is a rematch.
            Assert.Empty(MatchInvariants.CheckTransition(before, after, restarted: true));
            Assert.NotEmpty(MatchInvariants.CheckTransition(before, after));
        }

        [Fact]
        public void OneGameplayEventCannotAwardItsPointsTwice()
        {
            // ⚠️⚠️ THIS IS THE DUPLICATE-MESSAGE INVARIANT. A knockdown pays 100. A seat that
            // moved by 300 in one step did not have a bigger knockdown, it had three awards, and
            // that is what a replayed request looks like from the outside.
            Assert.True(MatchInvariants.IsReachableDelta(Balance.ScoreLataKnocked));
            Assert.True(MatchInvariants.IsReachableDelta(
                Balance.ScoreLataKnocked + Balance.ScoreDefensePerTick));

            Assert.False(MatchInvariants.IsReachableDelta(Balance.ScoreLataKnocked * 3));
            Assert.False(MatchInvariants.IsReachableDelta(7));
        }

        [Fact]
        public void ADirectWriteToTheScoreboardIsCaught()
        {
            var before = Legal(2, new[] { 0, 0, 0, 0 });
            var after = Legal(2, new[] { 1234, 0, 0, 0 });

            Assert.Contains(MatchInvariants.CheckTransition(before, after),
                            f => f.Contains("wrote the scoreboard directly"));
        }

        // ------------------------------------------------------------------
        // Two peers
        // ------------------------------------------------------------------

        [Fact]
        public void TwoPeersDescribingTheSameMatchAgree()
        {
            var scores = new[] { 300, 100, 0, 50 };
            Assert.Empty(MatchInvariants.CheckPeersAgree("host", Legal(3, scores),
                                                         "client", Legal(3, scores)));
        }

        [Fact]
        public void AWinnerDisagreementIsCaughtEvenWhenNeitherPeerIsObviouslyWrong()
        {
            var host = Legal(4, new[] { 300, 100, 0, 0 });
            var client = Legal(4, new[] { 100, 300, 0, 0 });

            var faults = MatchInvariants.CheckPeersAgree("host", host, "client", client);
            Assert.Contains(faults, f => f.Contains("cannot end one match differently"));
        }

        [Fact]
        public void ADrawIsADrawOnBothPeers()
        {
            // A tie at the top returns -1 on both sides, so an honest draw is not a disagreement.
            var tied = new[] { 200, 200, 0, 0 };
            Assert.Empty(MatchInvariants.CheckPeersAgree("host", Legal(4, tied),
                                                         "client", Legal(4, tied)));
        }

        [Fact]
        public void ARoundDisagreementIsCaught()
        {
            Assert.Contains(MatchInvariants.CheckPeersAgree("host", Legal(2), "client", Legal(3)),
                            f => f.Contains("is on round 2"));
        }

        // ------------------------------------------------------------------
        // § THE FUZZER
        // ------------------------------------------------------------------

        /// <summary>
        /// A match played forwards through the REAL scoreboard and the REAL role schedule, with a
        /// randomised but legal stream of awards, asserting that nothing the rules can do to
        /// themselves violates an invariant.
        ///
        /// ⚠️ IT USES `Scoreboard` AND `MatchRules` RATHER THAN A MODEL OF THEM. A fuzzer over a
        /// reimplementation tests the reimplementation. The only thing written here is the ORDER
        /// of the events, which is the thing being fuzzed.
        /// </summary>
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(20260904)]
        [InlineData(int.MaxValue / 3)]
        public void APlayedMatchNeverViolatesAnInvariant(int seed)
        {
            var rng = new Random(seed);
            var events = (ScoreEvent[])Enum.GetValues(typeof(ScoreEvent));

            for (int trial = 0; trial < 200; trial++)
            {
                var board = new Scoreboard();
                var owners = FourSeats("p0", "p1", "p2", "p3");
                int rounds = rng.Next(2) == 0 ? Balance.Rounds : Balance.HeroStrikeRounds;

                var previous = new MatchSnapshot(1, rounds, MatchRules.DefenderSlotFor(1), true,
                                                 false, Snapshot(board), owners);
                Assert.Empty(MatchInvariants.Check(previous));

                for (int round = 1; round <= rounds; round++)
                {
                    // At most two awards per observed step, which is what a 5 Hz snapshot can
                    // legitimately carry. Three would be an honest gap in the check rather than
                    // a bug, and the invariant says so.
                    int awards = rng.Next(0, 3);
                    for (int i = 0; i < awards; i++)
                        board.Add(rng.Next(Balance.PlayerCount), events[rng.Next(events.Length)]);

                    var current = new MatchSnapshot(round, rounds,
                                                    MatchRules.DefenderSlotFor(round), true,
                                                    false, Snapshot(board), owners);

                    var state = MatchInvariants.Check(current);
                    Assert.True(state.Count == 0,
                                $"seed {seed}, trial {trial}, round {round}: " +
                                string.Join(" | ", state));

                    var step = MatchInvariants.CheckTransition(previous, current);
                    Assert.True(step.Count == 0,
                                $"seed {seed}, trial {trial}, round {round}: " +
                                string.Join(" | ", step));

                    previous = current;
                }

                // The match ends. Every peer computing the winner from the same board agrees.
                var finished = new MatchSnapshot(previous.RoundNumber, rounds,
                                                 previous.DefenderSlot, false, false,
                                                 Snapshot(board), owners);
                Assert.Empty(MatchInvariants.CheckPeersAgree("host", finished, "client", finished));
            }
        }

        /// <summary>
        /// The same match, with the snapshot stream CORRUPTED the way a network corrupts one:
        /// duplicated, delayed, reordered. Every corruption must be caught.
        ///
        /// ⚠️⚠️ THIS IS THE HALF THAT PROVES THE CHECKER IS WORTH RUNNING. An invariant set that
        /// accepts everything passes the test above trivially. This asserts the other direction:
        /// that the illegal orderings a real link produces do not get through.
        /// </summary>
        [Theory]
        [InlineData(7)]
        [InlineData(11)]
        [InlineData(20260904)]
        public void EveryCorruptedOrderingIsCaught(int seed)
        {
            var rng = new Random(seed);
            int caught = 0;

            for (int trial = 0; trial < 300; trial++)
            {
                int round = rng.Next(2, 4);
                var scores = new[] { rng.Next(0, 10) * 100, rng.Next(0, 10) * 100, 0, 0 };
                var before = Legal(round, scores);

                // Four ways a link breaks an ordering, one picked per trial.
                switch (rng.Next(4))
                {
                    case 0: // a stale packet replayed after a newer one
                    {
                        var after = Legal(round - 1, scores);
                        var faults = MatchInvariants.CheckTransition(before, after);
                        Assert.True(faults.Count > 0, $"seed {seed} trial {trial}: a stale round " +
                                                      $"{round - 1} after {round} went undetected");
                        caught++;
                        break;
                    }
                    case 1: // a round advance delivered twice
                    {
                        var after = Legal(round + 2, scores);
                        var faults = MatchInvariants.CheckTransition(before, after);
                        Assert.True(faults.Count > 0, $"seed {seed} trial {trial}: a double " +
                                                      $"advance to {round + 2} went undetected");
                        caught++;
                        break;
                    }
                    case 2: // one award applied twice on top of itself
                    {
                        var doubled = (int[])scores.Clone();
                        doubled[0] += Balance.ScoreLataKnocked * 3;
                        var after = Legal(round, doubled);
                        var faults = MatchInvariants.CheckTransition(before, after);
                        Assert.True(faults.Count > 0, $"seed {seed} trial {trial}: a tripled " +
                                                      $"award went undetected");
                        caught++;
                        break;
                    }
                    default: // the taya frozen while the round moved on
                    {
                        var after = new MatchSnapshot(round + 1, 4, before.DefenderSlot, true,
                                                      false, scores, before.SeatOwners);
                        var faults = MatchInvariants.CheckTransition(before, after);
                        Assert.True(faults.Count > 0, $"seed {seed} trial {trial}: a stale taya " +
                                                      $"across a round boundary went undetected");
                        caught++;
                        break;
                    }
                }
            }

            Assert.Equal(300, caught);
        }

        private static int[] Snapshot(Scoreboard board)
        {
            var scores = new int[Balance.PlayerCount];
            for (int i = 0; i < scores.Length; i++) scores[i] = board[i];
            return scores;
        }
    }
}
