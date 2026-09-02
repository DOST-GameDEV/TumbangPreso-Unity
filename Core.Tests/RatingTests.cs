using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// Glicko-2 for a four-player free for all.
    ///
    /// ⚠️ THE THREE TESTS `FUTURE.md` § 19.9 NAMES AS THE DONE-WHEN ARE
    /// <see cref="ASimulatedSeasonSortsFourPlayersIntoTheirTrueOrder"/>,
    /// <see cref="ANewPlayerSettlesInsideTenMatchesFromAMidLadderStart"/> and
    /// <see cref="AClearlyStrongerNewAccountClimbsOutOfALowBandQuickly"/>. The rest are the
    /// invariants those three would still pass without.
    /// </summary>
    public class RatingTests
    {
        private static RankState Fresh() => new RankState();

        private static RankState At(double rating, double deviation)
            => new RankState { Rating = rating, Deviation = deviation, Volatility = RatingRules.StartVolatility };

        /// <summary>
        /// Runs one match between four players whose TRUE strengths are given, and returns the new
        /// states. The placement is the true order with a deterministic wobble, so the simulation
        /// is a skill test rather than a coin flip.
        ///
        /// ⚠️ SEEDED, LIKE EVERY OTHER SIMULATION IN THIS REPOSITORY. `CLAUDE.md` § 7.1: the bot
        /// probe was unseeded once and the same build measured 110 and then 467 penalties on
        /// consecutive runs.
        /// </summary>
        private static RankState[] PlayOne(RankState[] states, double[] trueSkill, Random rng)
        {
            var noisy = new (int Slot, double Roll)[trueSkill.Length];
            for (int i = 0; i < trueSkill.Length; i++)
                noisy[i] = (i, trueSkill[i] + ((rng.NextDouble() - 0.5) * 400.0));

            Array.Sort(noisy, (a, b) => b.Roll.CompareTo(a.Roll));

            var placements = new int[trueSkill.Length];
            for (int place = 0; place < noisy.Length; place++) placements[noisy[place].Slot] = place;

            return RatingRules.UpdateAll(states, placements);
        }

        // ------------------------------------------------------------------------------
        // The pairwise expansion
        // ------------------------------------------------------------------------------

        [Fact]
        public void OneFourPlayerMatchIsSixPairwiseOutcomes()
        {
            var pairs = RatingRules.Pairwise(new[] { 0, 1, 2, 3 });

            Assert.Equal(6, pairs.Count);
            Assert.All(pairs, p => Assert.Equal(p.A < p.B ? 1.0 : 0.0, p.ScoreForA));
        }

        [Fact]
        public void EqualPlacementIsADrawAndNotAWin()
        {
            // Two players tied for second, which `MatchRecordRules.Placements` produces from equal
            // scores and which a team game usually cannot express at all.
            var pairs = RatingRules.Pairwise(new[] { 0, 1, 1, 3 });

            var tie = pairs.Find(p => p.A == 1 && p.B == 2);
            Assert.Equal(0.5, tie.ScoreForA);
        }

        /// <summary>
        /// ⚠️⚠️ THE ORDER THE FOUR LINES ARE PROCESSED IN MUST NOT CHANGE THE ANSWER. Glicko-2 is
        /// a batch system and every player is updated against the state everybody was in BEFORE
        /// the match. If player 0's new rating leaked into player 1's update, the result would
        /// depend on the order the record happens to list its seats in, and the client's preview
        /// and the server's write would disagree about the same match.
        /// </summary>
        [Fact]
        public void EveryPlayerIsUpdatedAgainstTheStateEverybodyStartedIn()
        {
            var before = new[] { At(1500, 200), At(1400, 120), At(1600, 60), At(1500, 350) };
            var after = RatingRules.UpdateAll(before, new[] { 0, 1, 2, 3 });

            // Nothing wrote back into the inputs.
            Assert.Equal(1500.0, before[0].Rating, 6);
            Assert.Equal(1400.0, before[1].Rating, 6);

            // And the same batch, presented with the seats reversed, gives each player the same
            // number as before.
            var reversed = new[] { before[3], before[2], before[1], before[0] };
            var reversedAfter = RatingRules.UpdateAll(reversed, new[] { 3, 2, 1, 0 });

            Assert.Equal(after[0].Rating, reversedAfter[3].Rating, 6);
            Assert.Equal(after[2].Rating, reversedAfter[1].Rating, 6);
        }

        // ------------------------------------------------------------------------------
        // The done-when tests
        // ------------------------------------------------------------------------------

        /// <summary>
        /// A whole season, four players of genuinely different strength, all starting at 1500.
        /// The ladder has to end up in the true order.
        /// </summary>
        [Fact]
        public void ASimulatedSeasonSortsFourPlayersIntoTheirTrueOrder()
        {
            var states = new[] { Fresh(), Fresh(), Fresh(), Fresh() };
            var trueSkill = new[] { 1900.0, 1600.0, 1300.0, 1000.0 };
            var rng = new Random(20260831);

            for (int match = 0; match < 120; match++) states = PlayOne(states, trueSkill, rng);

            Assert.True(states[0].Rating > states[1].Rating,
                        $"strongest {states[0].Rating:F0} should beat second {states[1].Rating:F0}");
            Assert.True(states[1].Rating > states[2].Rating,
                        $"second {states[1].Rating:F0} should beat third {states[2].Rating:F0}");
            Assert.True(states[2].Rating > states[3].Rating,
                        $"third {states[2].Rating:F0} should beat fourth {states[3].Rating:F0}");

            // And they are on different rungs by the end, not four points apart.
            Assert.True(RatingRules.TierFor(states[0].Rating) > RatingRules.TierFor(states[3].Rating));

            // Everybody's deviation has converged. Nobody is still "settling" after 120 matches.
            Assert.All(states, s => Assert.True(s.Deviation < RatingRules.SettledDeviation,
                                                $"deviation {s.Deviation:F0} should be settled"));
        }

        /// <summary>
        /// ⚠️ THE REASON THERE ARE NO PLACEMENT MATCHES. `FUTURE.md` § 9 cut them on the argument
        /// that a wide starting deviation converges in the same handful of games and shows the
        /// player a tier from match one. This is that argument as a measurement.
        /// </summary>
        [Fact]
        public void ANewPlayerSettlesInsideTenMatchesFromAMidLadderStart()
        {
            var newcomer = Fresh();

            Assert.Equal(RankTier.Barangay, RatingRules.TierFor(newcomer.Rating));
            Assert.True(newcomer.Deviation > RatingRules.SettledDeviation, "a fresh account starts unsettled");

            var settled = new[] { At(1500, 60), At(1500, 60), At(1500, 60) };

            // ⚠️ A GENUINELY AVERAGE PLAYER, WHICH MEANS THE TEN PLACEMENTS AVERAGE EXACTLY 1.5.
            // A sequence that merely cycles 0,1,2,3 is one place too good over ten matches and
            // would drift the newcomer upward, which would be a test measuring its own fixture.
            //
            // ⚠️⚠️ AND THE ORDER OF THE TEN MATTERS AS MUCH AS THE AVERAGE, WHICH IS THE
            // MECHANISM RATHER THAN A FLAW IN THE FIXTURE. While the deviation is still 350 wide
            // one result moves the rating a long way, and by the tenth it moves it a little, so
            // an early lean is never fully paid back by a late correction. Measured on this
            // model: the same ten placements ordered 0,3,1,2,2,1,3,0,1,2 finish at **1606**, and
            // ordered 1,2,0,3,... at **1605**, because both lean high in the first three matches.
            // The sequence below is balanced in ADJACENT PAIRS, so every prefix is neutral too
            // and the test measures convergence instead of measuring its own opening roll.
            //
            // ⚠️ THAT ASYMMETRY IS A FEATURE AND NOT SOMETHING TO TUNE AWAY. It is the same
            // property `AClearlyStrongerNewAccountClimbsOutOfALowBandQuickly` depends on, and it
            // is why `FUTURE.md` § 8.3 gets smurf handling for free.
            var mine = new[] { 1, 2, 2, 1, 0, 3, 3, 0, 1, 2 };

            for (int match = 0; match < mine.Length; match++)
            {
                var table = new[] { newcomer, settled[0], settled[1], settled[2] };
                var placements = new int[4];
                placements[0] = mine[match];

                int next = 0;
                for (int seat = 1; seat < 4; seat++)
                {
                    while (next == mine[match]) next++;
                    placements[seat] = next++;
                }

                newcomer = RatingRules.UpdateAll(table, placements)[0];
            }

            Assert.True(newcomer.Deviation <= RatingRules.SettledDeviation,
                        $"after ten matches the deviation is {newcomer.Deviation:F0}, which should be settled");
            Assert.True(RankTier.Barangay == RatingRules.TierFor(newcomer.Rating),
                        $"an average newcomer should still be mid-ladder, got {newcomer.Rating:F0}");
        }

        /// <summary>
        /// ⚠️⚠️ SMURF HANDLING WITH NO SMURF SYSTEM. `FUTURE.md` § 8.3: "a new account with a very
        /// high early win rate gets a wide rating deviation and climbs fast. Glicko-2 does this
        /// for free if the deviation is not clamped too tightly." A strong player who makes a new
        /// account must leave a low band in a handful of matches, or the low band is where they
        /// spend their first evening and everybody else's evening is ruined.
        /// </summary>
        [Fact]
        public void AClearlyStrongerNewAccountClimbsOutOfALowBandQuickly()
        {
            var smurf = Fresh();
            var locals = new[] { At(1100, 60), At(1100, 60), At(1100, 60) };

            for (int match = 0; match < 8; match++)
            {
                var table = new[] { smurf, locals[0], locals[1], locals[2] };
                smurf = RatingRules.UpdateAll(table, new[] { 0, 1, 2, 3 })[0];
            }

            Assert.True(smurf.Rating > 1700.0,
                        $"eight straight wins should have moved a new account well clear of the field, got {smurf.Rating:F0}");
            Assert.True(RatingRules.TierFor(smurf.Rating) >= RankTier.Kampeon);
        }

        /// <summary>
        /// ⚠️ ONE MATCH MOVES A SETTLED PLAYER ABOUT AS MUCH AS ONE GAME SHOULD, which is
        /// `FUTURE.md` § 9's phrasing of the scaling requirement. Three opponents in one rating
        /// period is exactly the shape Glicko-2's period update was written for, so this is a
        /// bound rather than a tuning knob, and no artificial scale factor exists to be tuned.
        /// </summary>
        [Fact]
        public void OneMatchMovesASettledPlayerAboutAsMuchAsOneGameShould()
        {
            var settled = At(1500, 50);
            var table = new[] { settled, At(1500, 50), At(1500, 50), At(1500, 50) };

            var won = RatingRules.UpdateAll(table, new[] { 0, 1, 2, 3 })[0];
            var lost = RatingRules.UpdateAll(table, new[] { 3, 0, 1, 2 })[0];

            double up = won.Rating - 1500.0;
            double down = 1500.0 - lost.Rating;

            Assert.InRange(up, 5.0, 40.0);
            Assert.InRange(down, 5.0, 40.0);
        }

        // ------------------------------------------------------------------------------
        // Tiers, floors and seasons
        // ------------------------------------------------------------------------------

        [Fact]
        public void ThereAreFiveTiersAndTheStartIsTheMiddleOne()
        {
            Assert.Equal(5, RatingRules.TierNames.Length);
            Assert.Equal(5, RatingRules.TierFloors.Length);
            Assert.Equal(5, RatingRules.TierBlurbs.Length);

            Assert.Equal(RankTier.Barangay, RatingRules.TierFor(RatingRules.StartRating));

            Assert.Equal("BATA", RatingRules.TierName(RankTier.Bata));
            Assert.Equal("ALAMAT", RatingRules.TierName(RankTier.Alamat));
            Assert.Equal("UNRANKED", RatingRules.TierName(RankTier.Unranked));

            // Every tier has a sentence, because a rung nobody can decode is a word.
            for (int i = 0; i < RatingRules.TierNames.Length; i++)
                Assert.False(string.IsNullOrWhiteSpace(RatingRules.TierBlurb((RankTier)i)));
        }

        [Fact]
        public void TheApexReportsFullProgressRatherThanAnInventedFraction()
        {
            Assert.Equal(1.0f, RatingRules.TierProgress(9999.0));
            Assert.InRange(RatingRules.TierProgress(1500.0), 0.0f, 1.0f);
        }

        /// <summary>
        /// ⚠️ RANK FLOORS, `INSPIRATION.md` § 2.19: once a tier is reached the season cannot fall
        /// below it. The floor is raised BEFORE it is enforced, so reaching a tier and immediately
        /// losing cannot drop out of the tier that was just reached.
        /// </summary>
        [Fact]
        public void OnceATierIsReachedTheSeasonCannotFallBelowIt()
        {
            var state = RatingRules.ApplyFloors(At(1620, 60));
            Assert.Equal((int)RankTier.Kampeon, state.FloorTier);

            state.Rating = 1200.0;
            RatingRules.ApplyFloors(state);

            Assert.Equal(1600.0, state.Rating, 6);
            Assert.Equal(RankTier.Kampeon, RatingRules.TierFor(state.Rating));
        }

        [Fact]
        public void TheSeasonSoftResetPullsTowardTheMeanKeepsThePeakAndDropsTheFloor()
        {
            var state = RatingRules.ApplyFloors(At(1900, 45));
            Assert.Equal((int)RankTier.Alamat, state.PeakTier);

            RatingRules.BeginSeason(state, 2);

            Assert.True(state.Rating < 1900.0, "it moved toward the mean");
            Assert.True(state.Rating > RatingRules.StartRating, "and it is not a wipe");
            Assert.Equal((int)RankTier.Alamat, state.PeakTier);
            Assert.Equal(0, state.FloorTier);
            Assert.Equal(0, state.MatchesThisSeason);
            Assert.True(state.Deviation >= RatingRules.SeasonDeviation);
            Assert.Equal(2, state.Season);
        }

        [Fact]
        public void ASeasonIsTenWeeksCountedFromAFixedEpochRatherThanFromAConfigNobodyMoves()
        {
            Assert.Equal(1, RatingRules.SeasonAt(RatingRules.SeasonOneStartUtc));
            Assert.Equal(1, RatingRules.SeasonAt(RatingRules.SeasonOneStartUtc.AddDays(69)));
            Assert.Equal(2, RatingRules.SeasonAt(RatingRules.SeasonOneStartUtc.AddDays(71)));
            Assert.Equal(3, RatingRules.SeasonAt(RatingRules.SeasonOneStartUtc.AddDays(141)));
        }

        /// <summary>
        /// ⚠️ NO DECAY. `FUTURE.md` § 9: "decay punishes people with jobs and school, which is this
        /// whole audience." Nothing in this file reads a clock except the season boundary, and this
        /// asserts it: a state left alone for a year inside its season is untouched.
        /// </summary>
        [Fact]
        public void NothingDecaysWhileAPlayerIsAway()
        {
            var state = At(1750, 55);
            state.Season = 1;
            state.FloorTier = (int)RankTier.Kampeon;

            RatingRules.BeginSeason(state, 1);

            Assert.Equal(1750.0, state.Rating, 6);
            Assert.Equal(55.0, state.Deviation, 6);
            Assert.Equal((int)RankTier.Kampeon, state.FloorTier);
        }

        [Fact]
        public void TheEndOfMatchBoardIsToldWhichWayItMovedAndByHowMuch()
        {
            var before = At(1500, 50);
            var after = RatingRules.ApplyFloors(At(1540, 48));

            var change = RatingRules.Describe(before, after);

            Assert.Equal(40, change.Delta);
            Assert.Equal(RankTier.Barangay, change.TierBefore);
            Assert.Equal(RankTier.Barangay, change.TierAfter);
            Assert.False(change.StillSettling);
        }
    }
}
