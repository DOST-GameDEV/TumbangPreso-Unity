using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// One player's place on the ladder, as the three numbers Glicko-2 needs plus the two facts a
    /// season needs.
    ///
    /// ⚠️⚠️ THE PLAYER NEVER SEES ANY OF THESE NUMBERS. `FUTURE.md` § 9: "the player never sees
    /// the number, only the tier, so it costs nothing in player-facing complexity". The rating is
    /// the arithmetic; <see cref="RankTier"/> is the product. A screen that draws 1487 has
    /// imported a spreadsheet into a street game.
    /// </summary>
    [Serializable]
    public sealed class RankState
    {
        /// <summary>Glicko-2 rating on the familiar 1500-centred scale.</summary>
        public double Rating = RatingRules.StartRating;

        /// <summary>
        /// Rating deviation: how unsure the system is about <see cref="Rating"/>.
        ///
        /// ⚠️⚠️ THIS FIELD IS WHY THERE ARE NO PLACEMENT MATCHES AND WHY SMURF HANDLING IS FREE.
        /// `FUTURE.md` § 9 cut placements on 2026-08-31: "five games in a hidden state with their
        /// own rules and their own UI was a separate concept doing a job Glicko-2 already does by
        /// itself". A new account starts wide, so its first few results move it a long way; a
        /// settled account is narrow and moves slowly. A strong new player therefore climbs out of
        /// a low band in a handful of matches with no smurf-detection system anywhere.
        /// </summary>
        public double Deviation = RatingRules.StartDeviation;

        /// <summary>Glicko-2 volatility: how erratic this player's results have been.</summary>
        public double Volatility = RatingRules.StartVolatility;

        /// <summary>Ranked matches this season, for the "still settling" state on the badge.</summary>
        public int MatchesThisSeason;

        /// <summary>
        /// The season this state belongs to. A state from an older season is soft reset on read.
        /// </summary>
        public int Season = 1;

        /// <summary>
        /// The highest tier reached THIS season, which the season cannot fall below.
        ///
        /// ⚠️ RANK FLOORS, `INSPIRATION.md` § 2.19. "It costs one comparison and it removes the
        /// most common reason people stop queueing." Stored as the tier index rather than as a
        /// rating so the floor survives any retune of the thresholds.
        /// </summary>
        public int FloorTier;

        /// <summary>The highest tier ever reached, in any season. Permanent, and the thing people
        /// screenshot (`FUTURE.md` § 9: "the peak is the thing people brag about").</summary>
        public int PeakTier;
    }

    /// <summary>
    /// The five rungs. ⚠️ FIVE, WITH NO DIVISIONS, CHOSEN BY 🧑 ON 2026-08-31.
    ///
    /// `FUTURE.md` § 19.9 step 4 flagged the shape as an open question: "five tiers times three
    /// divisions plus an apex is sixteen rungs of invented vocabulary before a player knows
    /// whether they are any good". Asked directly, he took the five. `FUTURE.md` § 0.5 rule 11b is
    /// the test that decides it: the cost of a feature is what the PLAYER has to hold in their
    /// head, and sixteen rungs is fifteen more words than five.
    /// </summary>
    public enum RankTier
    {
        Unranked = -1,
        Bata = 0,
        Kanto = 1,
        Barangay = 2,
        Kampeon = 3,
        Alamat = 4,
    }

    /// <summary>What one ranked match did to one player, for the end-of-match board.</summary>
    public sealed class RankChange
    {
        public RankTier TierBefore = RankTier.Unranked;
        public RankTier TierAfter = RankTier.Unranked;
        public double RatingBefore;
        public double RatingAfter;

        /// <summary>True when the floor stopped a loss from costing a tier.</summary>
        public bool HeldByFloor;

        /// <summary>True when this match set a new personal best tier.</summary>
        public bool NewPeak;

        /// <summary>Whether the rating is still moving fast, which the badge says rather than
        /// pretending a first-week tier is settled.</summary>
        public bool StillSettling;

        public int Delta => (int)Math.Round(RatingAfter - RatingBefore);
    }

    /// <summary>
    /// Glicko-2, adapted for a four-player free for all, plus the tiers and the season.
    ///
    /// ⚠️⚠️ ELO IS A TWO-PLAYER SYSTEM AND THIS IS NOT A TWO-PLAYER GAME. `FUTURE.md` § 9 is
    /// explicit about the adaptation: resolve one match as **six pairwise outcomes** (1st beat
    /// 2nd, 1st beat 3rd, 1st beat 4th, 2nd beat 3rd, 2nd beat 4th, 3rd beat 4th) and feed all six
    /// in. Each player therefore faces three opponents in one rating period, which is exactly the
    /// shape Glicko-2's period update was written for, so no artificial step scaling is needed and
    /// none is applied. `RatingTests.OneMatchMovesASettledPlayerAboutAsMuchAsOneGameShould` is the
    /// assertion that keeps it that way.
    ///
    /// ⚠️⚠️ A DRAW IS A REAL OUTCOME HERE AND IN A TEAM GAME IT USUALLY IS NOT. Two players can
    /// finish a Tumbang Preso match on the same cumulative score, and `MatchRecordRules.Placements`
    /// already assigns them the same placement. Equal placement is 0.5 in the pairwise expansion.
    ///
    /// ⚠️ THE WHOLE MODEL IS IN THE CORE AND THE UNITY SIDE IS SUBMISSION AND DISPLAY, per
    /// `FUTURE.md` § 19.9's constraint. There is a second copy in `ugs/cloud-code/match-record.js`
    /// because the server is the only writer of a rating; `docs/TODO.md` § 89.6 is the entry about
    /// a rule written twice on purpose and what keeps the two honest.
    /// </summary>
    public static class RatingRules
    {
        /// <summary>
        /// Everyone starts here. ⚠️ MID-LADDER, WHICH IS <see cref="RankTier.Barangay"/>, AND
        /// THAT IS THE POINT OF CUTTING PLACEMENTS: "a new player sees where they stand on their
        /// first match instead of their sixth" (`FUTURE.md` § 9).
        /// </summary>
        public const double StartRating = 1500.0;

        /// <summary>
        /// ⚠️ 350 IS GLICKO'S OWN "I KNOW NOTHING ABOUT YOU" VALUE AND IT IS NOT CLAMPED DOWN.
        /// `FUTURE.md` § 8.3: "Glicko-2 does this for free if the deviation is not clamped too
        /// tightly, which is a real argument for it over plain Elo." A tighter start would make a
        /// strong new account grind through a low band, which is the smurf problem people build
        /// whole systems to solve.
        /// </summary>
        public const double StartDeviation = 350.0;

        public const double StartVolatility = 0.06;

        /// <summary>
        /// Glicko-2's system constant, constraining how fast volatility moves.
        ///
        /// ⚠️ 0.5 IS THE MIDDLE OF GLICKO'S OWN RECOMMENDED 0.3 TO 1.2. Smaller is steadier and
        /// slower to react to a genuine change in skill. There is no queue population to measure
        /// this against yet, so it is the published default rather than a number invented here.
        /// </summary>
        public const double Tau = 0.5;

        /// <summary>The Glicko-2 internal scale factor.</summary>
        private const double Scale = 173.7178;

        /// <summary>Convergence tolerance for the volatility iteration.</summary>
        private const double Epsilon = 0.000001;

        /// <summary>
        /// A deviation at or under this counts as settled, which is what the badge stops
        /// qualifying.
        ///
        /// ⚠️ 100 IS ABOUT TEN MATCHES FROM A COLD START AT THESE CONSTANTS, and
        /// `RatingTests.ANewPlayerSettlesInsideTenMatches` is the measurement rather than this
        /// comment being the claim.
        /// </summary>
        public const double SettledDeviation = 100.0;

        /// <summary>
        /// The rating each tier begins at. ⚠️ INDEX IS THE <see cref="RankTier"/> VALUE.
        ///
        /// ⚠️ THE START RATING SITS IN <see cref="RankTier.Barangay"/> ON PURPOSE. Starting a
        /// player in the middle tier is what "start everyone mid-ladder" means in words a player
        /// can read; starting them at the bottom of a five-rung ladder would make the first
        /// twenty matches feel like a punishment for being new.
        /// </summary>
        public static readonly int[] TierFloors = { 0, 1250, 1400, 1600, 1800 };

        /// <summary>
        /// The names, in the game's own voice. ⚠️ 🧑 CHOSE THIS SET ON 2026-08-31, from
        /// `FUTURE.md` § 9's suggestion. **This array is the one place they are written.**
        /// </summary>
        public static readonly string[] TierNames = { "BATA", "KANTO", "BARANGAY", "KAMPEON", "ALAMAT" };

        /// <summary>
        /// One line each, because a tier name nobody can decode is a word rather than a rank.
        ///
        /// ⚠️ THEY DESCRIBE THE STREET GAME, NOT THE LADDER. "Silver 2" tells a player nothing
        /// except that there is a Silver 1. These say what kind of player the rung is.
        /// </summary>
        public static readonly string[] TierBlurbs =
        {
            "Learning the throw and the run back in.",
            "Knows the arc. Still gets caught retrieving.",
            "Reads the taya and picks the moment.",
            "Wins the round nobody thought was winnable.",
            "The leaderboard. Everyone knows the name.",
        };

        /// <summary>How many weeks a season lasts. `FUTURE.md` § 9: ten.</summary>
        public const int SeasonWeeks = 10;

        /// <summary>
        /// How far a soft reset pulls a rating toward the mean.
        ///
        /// ⚠️⚠️ IT IS A PULL AND NEVER A WIPE, AND THE DIFFERENCE IS THE WHOLE POINT. `FUTURE.md`
        /// § 9: "soft reset toward the mean, never a wipe. Keep a permanent peak on the profile."
        /// A wipe throws away a season of evidence and makes the first week of every season a
        /// stomp; a pull keeps the ordering and just loosens the certainty.
        /// </summary>
        public const double SeasonPullToMean = 0.4;

        /// <summary>
        /// What the deviation is widened to at a season boundary, so early-season results move
        /// faster and the ladder re-sorts itself in a week rather than in ten.
        /// </summary>
        public const double SeasonDeviation = 200.0;

        // ------------------------------------------------------------------------------
        // Tiers
        // ------------------------------------------------------------------------------

        public static RankTier TierFor(double rating)
        {
            for (int i = TierFloors.Length - 1; i >= 0; i--)
                if (rating >= TierFloors[i]) return (RankTier)i;

            return RankTier.Bata;
        }

        public static string TierName(RankTier tier)
        {
            int i = (int)tier;
            return i < 0 || i >= TierNames.Length ? "UNRANKED" : TierNames[i];
        }

        public static string TierBlurb(RankTier tier)
        {
            int i = (int)tier;
            return i < 0 || i >= TierBlurbs.Length ? "Play one ranked match to be placed." : TierBlurbs[i];
        }

        /// <summary>
        /// The rating a player cannot be pushed below once <paramref name="floorTier"/> is reached.
        /// </summary>
        public static double FloorRating(int floorTier)
        {
            if (floorTier <= 0) return 0.0;
            int i = floorTier >= TierFloors.Length ? TierFloors.Length - 1 : floorTier;
            return TierFloors[i];
        }

        /// <summary>
        /// Progress through the current tier, 0 to 1, for a bar that is honest at both ends.
        ///
        /// ⚠️ THE APEX HAS NO CEILING, SO IT REPORTS FULL RATHER THAN AN INVENTED FRACTION. A bar
        /// that creeps toward a number that does not exist is a bar that lies.
        /// </summary>
        public static float TierProgress(double rating)
        {
            var tier = TierFor(rating);
            int i = (int)tier;
            if (i >= TierFloors.Length - 1) return 1.0f;

            double low = TierFloors[i];
            double high = TierFloors[i + 1];
            if (high <= low) return 1.0f;

            double p = (rating - low) / (high - low);
            if (p < 0.0) return 0.0f;
            if (p > 1.0) return 1.0f;
            return (float)p;
        }

        // ------------------------------------------------------------------------------
        // The pairwise expansion
        // ------------------------------------------------------------------------------

        /// <summary>
        /// The six outcomes one four-player match produces, as (a, b, scoreForA) with
        /// a and b as slot indices.
        ///
        /// ⚠️ PLACEMENT DECIDES, NOT SCORE. Two players 40 points apart and two players 400 points
        /// apart are the same 1-0 here, which is `FUTURE.md` § 9's cut of the score-margin
        /// multiplier made structural: "a tuning surface that has to be balanced forever in
        /// exchange for a nuance nobody will feel".
        /// </summary>
        public static List<(int A, int B, double ScoreForA)> Pairwise(IReadOnlyList<int> placements)
        {
            var pairs = new List<(int, int, double)>(6);
            if (placements == null) return pairs;

            for (int a = 0; a < placements.Count; a++)
            {
                for (int b = a + 1; b < placements.Count; b++)
                {
                    if (placements[a] == placements[b]) pairs.Add((a, b, 0.5));
                    else if (placements[a] < placements[b]) pairs.Add((a, b, 1.0));
                    else pairs.Add((a, b, 0.0));
                }
            }

            return pairs;
        }

        // ------------------------------------------------------------------------------
        // Glicko-2
        // ------------------------------------------------------------------------------

        private static double G(double phi) => 1.0 / Math.Sqrt(1.0 + (3.0 * phi * phi / (Math.PI * Math.PI)));

        private static double E(double mu, double muJ, double phiJ)
            => 1.0 / (1.0 + Math.Exp(-G(phiJ) * (mu - muJ)));

        /// <summary>
        /// One rating period for one player, against every opponent met in it.
        ///
        /// ⚠️⚠️ THE OPPONENT RATINGS ARE THE ONES FROM BEFORE THE MATCH, FOR ALL FOUR PLAYERS.
        /// Glicko-2 is a batch system: every player in a period is updated against the state
        /// everybody was in when the period started. Updating player 0 and then feeding its NEW
        /// rating into player 1's update makes the result depend on the order the four lines are
        /// processed in, and the order is whatever the record happens to list. That is a rating
        /// that differs between the client's preview and the server's write, which is the exact
        /// class of bug `docs/TODO.md` § 89.6 exists to stop.
        /// </summary>
        public static RankState Update(RankState before, IReadOnlyList<RankState> opponents,
                                       IReadOnlyList<double> scores)
        {
            var after = new RankState
            {
                Rating = before.Rating,
                Deviation = before.Deviation,
                Volatility = before.Volatility,
                MatchesThisSeason = before.MatchesThisSeason,
                Season = before.Season,
                FloorTier = before.FloorTier,
                PeakTier = before.PeakTier,
            };

            if (opponents == null || scores == null || opponents.Count == 0) return after;

            double mu = (before.Rating - StartRating) / Scale;
            double phi = before.Deviation / Scale;
            double sigma = before.Volatility;

            // Step 3: the estimated variance of the rating, from game outcomes alone.
            double vInv = 0.0;
            double delta = 0.0;

            for (int i = 0; i < opponents.Count && i < scores.Count; i++)
            {
                double muJ = (opponents[i].Rating - StartRating) / Scale;
                double phiJ = opponents[i].Deviation / Scale;

                double g = G(phiJ);
                double e = E(mu, muJ, phiJ);

                vInv += g * g * e * (1.0 - e);
                delta += g * (scores[i] - e);
            }

            if (vInv <= 0.0) return after;

            double v = 1.0 / vInv;
            double deltaHat = v * delta;

            // Step 5: the new volatility, by the Illinois variant of regula falsi.
            double a = Math.Log(sigma * sigma);
            double phiSq = phi * phi;
            double deltaSq = deltaHat * deltaHat;

            Func<double, double> f = x =>
            {
                double ex = Math.Exp(x);
                double num = ex * (deltaSq - phiSq - v - ex);
                double den = 2.0 * (phiSq + v + ex) * (phiSq + v + ex);
                return (num / den) - ((x - a) / (Tau * Tau));
            };

            double A = a;
            double B;

            if (deltaSq > phiSq + v)
            {
                B = Math.Log(deltaSq - phiSq - v);
            }
            else
            {
                int k = 1;
                while (f(a - (k * Tau)) < 0.0 && k < 100) k++;
                B = a - (k * Tau);
            }

            double fA = f(A);
            double fB = f(B);
            int guard = 0;

            while (Math.Abs(B - A) > Epsilon && guard++ < 200)
            {
                double C = A + ((A - B) * fA / (fB - fA));
                double fC = f(C);

                if (fC * fB <= 0.0)
                {
                    A = B;
                    fA = fB;
                }
                else
                {
                    fA /= 2.0;
                }

                B = C;
                fB = fC;
            }

            double sigmaPrime = Math.Exp(A / 2.0);

            // Step 6 and 7: pre-period deviation, then the update itself.
            double phiStar = Math.Sqrt(phiSq + (sigmaPrime * sigmaPrime));
            double phiPrime = 1.0 / Math.Sqrt((1.0 / (phiStar * phiStar)) + (1.0 / v));
            double muPrime = mu + (phiPrime * phiPrime * delta);

            after.Rating = (muPrime * Scale) + StartRating;
            after.Deviation = phiPrime * Scale;
            after.Volatility = sigmaPrime;

            // ⚠️ THE DEVIATION IS FLOORED, NOT THE RATING. A deviation that converges to nearly
            // zero freezes a player's rank permanently, because every future result would move
            // them by fractions of a point. Glicko's own guidance is a floor around 30.
            if (after.Deviation < 30.0) after.Deviation = 30.0;
            if (after.Deviation > StartDeviation) after.Deviation = StartDeviation;

            after.MatchesThisSeason = before.MatchesThisSeason + 1;
            return after;
        }

        /// <summary>
        /// The whole table for one match: four states in, four states out.
        ///
        /// ⚠️ BOTS AND UNRANKED SEATS ARE PASSED THROUGH UNCHANGED BY THE CALLER, not filtered
        /// here. This method takes exactly the players who are on the ladder, and a ranked match
        /// has no bots in it at all (`INSPIRATION.md` § 3.1).
        /// </summary>
        public static RankState[] UpdateAll(IReadOnlyList<RankState> before, IReadOnlyList<int> placements)
        {
            if (before == null || placements == null) return Array.Empty<RankState>();

            int n = Math.Min(before.Count, placements.Count);
            var after = new RankState[n];
            var pairs = Pairwise(placements);

            for (int i = 0; i < n; i++)
            {
                var opponents = new List<RankState>();
                var scores = new List<double>();

                foreach (var (a, b, scoreForA) in pairs)
                {
                    if (a >= n || b >= n) continue;
                    if (a == i) { opponents.Add(before[b]); scores.Add(scoreForA); }
                    else if (b == i) { opponents.Add(before[a]); scores.Add(1.0 - scoreForA); }
                }

                after[i] = ApplyFloors(Update(before[i], opponents, scores));
            }

            return after;
        }

        /// <summary>
        /// Rank floors and the permanent peak, applied after every update.
        ///
        /// ⚠️ THE FLOOR IS RAISED BEFORE IT IS ENFORCED, so reaching a tier and immediately losing
        /// cannot drop out of the tier that was just reached. That ordering IS the promise.
        /// </summary>
        public static RankState ApplyFloors(RankState state)
        {
            if (state == null) return null;

            int reached = (int)TierFor(state.Rating);
            if (reached > state.FloorTier) state.FloorTier = reached;
            if (state.FloorTier > state.PeakTier) state.PeakTier = state.FloorTier;

            double floor = FloorRating(state.FloorTier);
            if (state.Rating < floor) state.Rating = floor;

            return state;
        }

        /// <summary>
        /// The change one match made, for the end-of-match board.
        ///
        /// ⚠️ `FUTURE.md` § 0.5b, phase 9 row: the one thing on that surface is "which way the
        /// number moved, and by how much". Not the rating: the TIER and the direction.
        /// </summary>
        public static RankChange Describe(RankState before, RankState after)
        {
            var change = new RankChange
            {
                RatingBefore = before?.Rating ?? StartRating,
                RatingAfter = after?.Rating ?? StartRating,
                TierBefore = before == null ? RankTier.Unranked : TierFor(before.Rating),
                TierAfter = after == null ? RankTier.Unranked : TierFor(after.Rating),
                StillSettling = after != null && after.Deviation > SettledDeviation,
            };

            change.NewPeak = before != null && after != null && after.PeakTier > before.PeakTier;

            // The floor held when the raw arithmetic would have dropped a tier and did not.
            change.HeldByFloor = before != null && after != null &&
                                 change.RatingAfter <= FloorRating(after.FloorTier) &&
                                 change.RatingAfter < change.RatingBefore;

            return change;
        }

        // ------------------------------------------------------------------------------
        // Seasons
        // ------------------------------------------------------------------------------

        /// <summary>
        /// The soft reset. ⚠️ NEVER A WIPE, AND THE PEAK SURVIVES IT.
        ///
        /// ⚠️ THE FLOOR DOES NOT SURVIVE IT, AND THAT IS DELIBERATE. A floor is a promise about
        /// one season ("you cannot fall out of KAMPEON this season"). Carrying it forward would
        /// make every season start at last season's best and the ladder would only ever ratchet
        /// upward, which is a leaderboard of who has played longest.
        /// </summary>
        public static RankState BeginSeason(RankState state, int season)
        {
            state ??= new RankState();
            if (state.Season == season) return state;

            double pulled = state.Rating + ((StartRating - state.Rating) * SeasonPullToMean);

            state.Rating = pulled;
            state.Deviation = Math.Max(state.Deviation, SeasonDeviation);
            state.Volatility = StartVolatility;
            state.MatchesThisSeason = 0;
            state.Season = season;
            state.FloorTier = 0;

            int reached = (int)TierFor(state.Rating);
            if (reached > state.PeakTier) state.PeakTier = reached;

            return state;
        }

        /// <summary>
        /// Which season a UTC instant falls in, counting from the season-one start.
        ///
        /// ⚠️ THE BOUNDARY IS ARITHMETIC ON A FIXED EPOCH RATHER THAN A DATE IN A CONFIG THAT
        /// SOMEBODY HAS TO REMEMBER TO MOVE. A season that has to be rolled over by hand is a
        /// season that stays open for eight months.
        /// </summary>
        public static readonly DateTime SeasonOneStartUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

        public static int SeasonAt(DateTime utc)
        {
            if (utc <= SeasonOneStartUtc) return 1;

            double weeks = (utc - SeasonOneStartUtc).TotalDays / 7.0;
            return 1 + (int)(weeks / SeasonWeeks);
        }
    }
}
