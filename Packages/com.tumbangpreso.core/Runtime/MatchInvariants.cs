using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// One peer's view of a match, reduced to the facts an invariant can be stated about.
    ///
    /// ⚠️ IT IS A SNAPSHOT AND NOT A REFERENCE TO THE LIVE MATCH, so the same type serves the
    /// host, a client, a replayed soak iteration and a fuzz seed without any of them needing a
    /// `MonoBehaviour`. That is what lets every invariant below be asserted in a millisecond
    /// instead of by watching a match.
    /// </summary>
    public readonly struct MatchSnapshot
    {
        public readonly int RoundNumber;
        public readonly int TotalRounds;
        public readonly int DefenderSlot;
        public readonly bool InProgress;
        public readonly bool IsWarmupBuffer;
        public readonly int[] Scores;

        /// <summary>
        /// Who owns each seat, indexed by slot. A null or empty entry is an unclaimed seat.
        ///
        /// ⚠️⚠️ A SEAT IS NOT A PEER ID. `NetAuthority.LocalPeerId`'s note carries the whole
        /// argument: seats are 0-3 and always four of them, peer ids are handed out by the
        /// transport, and a set keyed by "whichever of the two was to hand" collapses two peers
        /// into one entry. These are OWNER TOKENS, which is the namespace that survives a seat
        /// being vacated and reused.
        /// </summary>
        public readonly string[] SeatOwners;

        /// <summary>
        /// Every CLAIM on a seat, rather than one answer per seat.
        ///
        /// ⚠️⚠️ THE SHAPE IS THE POINT AND <see cref="SeatOwners"/> COULD NOT EXPRESS HALF THE
        /// RULE IT WAS CHECKED AGAINST. `CheckSeatOwnership`'s own note claims both directions:
        /// *"Two seats with one owner is the spectator-and-a-driven-seat fault (§ 141) ... One
        /// seat with two owners is the reconnect fault: a peer whose slot was reused comes back
        /// and both believe they are driving it."* The first is findable in an owner-per-seat
        /// array. **The second is not representable in one**: an array indexed by seat holds
        /// exactly one owner per seat, so by the time the snapshot exists the second claimant has
        /// already been overwritten by the first, or the first by the second, and the checker is
        /// asking a question the data structure answered for it.
        ///
        /// ⚠️ SO A CLAIM IS A ROW, NOT A CELL. Two peers claiming seat 2 is two rows with the
        /// same `Seat`; one peer holding two seats is two rows with the same `Owner`. Both are
        /// ordinary list contents, and neither can be silently collapsed on the way in.
        ///
        /// ⚠️ NULL IS LEGAL AND MEANS "THIS OBSERVER ONLY KNOWS THE OWNER ARRAY". The seven
        /// argument constructor derives a row per occupied seat, so every existing caller keeps
        /// exactly the coverage it had.
        /// </summary>
        public readonly SeatClaim[] SeatClaims;

        public MatchSnapshot(int roundNumber, int totalRounds, int defenderSlot, bool inProgress,
                             bool isWarmupBuffer, int[] scores, string[] seatOwners)
            : this(roundNumber, totalRounds, defenderSlot, inProgress, isWarmupBuffer, scores,
                   seatOwners, ClaimsFrom(seatOwners))
        {
        }

        public MatchSnapshot(int roundNumber, int totalRounds, int defenderSlot, bool inProgress,
                             bool isWarmupBuffer, int[] scores, string[] seatOwners,
                             SeatClaim[] seatClaims)
        {
            RoundNumber = roundNumber;
            TotalRounds = totalRounds;
            DefenderSlot = defenderSlot;
            InProgress = inProgress;
            IsWarmupBuffer = isWarmupBuffer;
            Scores = scores;
            SeatOwners = seatOwners;
            SeatClaims = seatClaims;
        }

        /// <summary>
        /// One row per occupied seat, for an observer that only has the array.
        ///
        /// ⚠️ EVERY DERIVED ROW IS `Driving` AND NOT A SPECTATOR, which is exactly what an owner
        /// array asserts and no more. Deriving anything richer would be inventing evidence.
        /// </summary>
        private static SeatClaim[] ClaimsFrom(string[] owners)
        {
            if (owners == null) return null;

            int occupied = 0;
            for (int i = 0; i < owners.Length; i++)
                if (!string.IsNullOrEmpty(owners[i])) occupied++;

            var claims = new SeatClaim[occupied];
            int at = 0;
            for (int i = 0; i < owners.Length; i++)
            {
                if (string.IsNullOrEmpty(owners[i])) continue;
                claims[at++] = new SeatClaim(owners[i], i, driving: true, spectating: false);
            }

            return claims;
        }
    }

    /// <summary>
    /// One peer's claim on one seat.
    ///
    /// ⚠️⚠️ AN OWNER IS A TOKEN AND NOT A SEAT AND NOT A PEER ID. `NetAuthority.LocalPeerId`
    /// carries the whole argument: seats are 0-3 and always four of them, peer ids are handed out
    /// by the transport and are reused across a reconnect, and a set keyed by "whichever of the
    /// two was to hand" collapses two peers into one entry. The durable connection token is the
    /// namespace that survives a seat being vacated and reused, which is the only namespace in
    /// which "the same person is in two chairs" is a statement about a person.
    /// </summary>
    public readonly struct SeatClaim
    {
        /// <summary>The durable owner token. Empty is not a claim.</summary>
        public readonly string Owner;

        /// <summary>The seat claimed, or -1 for a peer that holds none.</summary>
        public readonly int Seat;

        /// <summary>
        /// Whether this claimant is actually driving a body in that seat.
        ///
        /// ⚠️ A HELD SEAT IS NOT A DRIVEN ONE. `LobbySession.Depart` holds a chair against a
        /// durable token so a returning player gets their own back, and for that window the seat
        /// legitimately has a claimant who is driving nothing. Counting that as a second driver
        /// would report the reconnect feature as a fault every time it worked.
        /// </summary>
        public readonly bool Driving;

        /// <summary>
        /// Whether this claimant is spectating.
        ///
        /// ⚠️⚠️ A SPECTATOR HOLDING A DRIVEN SEAT IS `docs/TODO.md` § 141 EXACTLY, and it is the
        /// one fault that needs both flags to be sayable at once: 🧑, with a screenshot, **"IF IT
        /// isnt spectator why do i see spectator hud"**. A model with one string per seat cannot
        /// state it at all, so the entry was closed on a cause rather than on an invariant.
        /// </summary>
        public readonly bool Spectating;

        public SeatClaim(string owner, int seat, bool driving, bool spectating)
        {
            Owner = owner ?? "";
            Seat = seat;
            Driving = driving;
            Spectating = spectating;
        }

        public override string ToString()
            => $"'{Owner}' seat {Seat}{(Driving ? " driving" : "")}{(Spectating ? " spectating" : "")}";
    }

    /// <summary>
    /// The things that must never be true of a match, stated once so that the soak harness, the
    /// fuzzer and a live host can all ask the same question.
    ///
    /// ⚠️⚠️ WHY A CHECKER RATHER THAN MORE ASSERTS IN MORE TESTS. Every invariant here was
    /// previously implicit in the shape of some other code: "the taya is derived" is a property of
    /// `MatchRules.DefenderSlotFor`, "score is created in one place" is a property of
    /// `MatchDirector.AddScore`, "a round cannot finish twice" is a property of a bool flag. Those
    /// are all true and none of them is CHECKABLE at runtime, so a soak run of two hundred matches
    /// could break one and report a clean sheet. This turns each property into a question that can
    /// be asked of a state, which is the difference between "the code is written correctly" and
    /// "the match currently running has not gone wrong".
    ///
    /// ⚠️ IT DUPLICATES NO RULE. Every check reads the same core function the game reads
    /// (`DefenderSlotFor`, `PointsFor`, `WinningSlot`); it asserts agreement rather than restating
    /// arithmetic. `CLAUDE.md` § 4's "do not scatter duplicate logic merely for testing" is the
    /// constraint, and a checker that recomputed the taya schedule its own way would be a second
    /// schedule that can disagree with the first.
    ///
    /// ⚠️ A VIOLATION IS A SENTENCE NAMING THE NUMBERS. "invariant failed" in a soak log over two
    /// hundred matches is a day of bisecting; "round 5 of 4" is the bug.
    /// </summary>
    public static class MatchInvariants
    {
        /// <summary>
        /// Everything that can be said about one state on its own, with no history.
        ///
        /// Returns an empty list when the state is legal. ⚠️ It appends rather than returning on
        /// the first fault, because two violations at once is the interesting case: one is a bug
        /// and two is usually a torn read.
        /// </summary>
        public static List<string> Check(MatchSnapshot s)
        {
            var faults = new List<string>();

            // ---- the round -------------------------------------------------
            if (s.InProgress)
            {
                if (s.RoundNumber < 1)
                    faults.Add($"a match is in progress at round {s.RoundNumber}; rounds are 1-based");
                else if (s.RoundNumber > s.TotalRounds)
                    faults.Add($"round {s.RoundNumber} of {s.TotalRounds}: the match should have " +
                               $"ended when the round count was exceeded");
            }

            if (s.TotalRounds < 1)
                faults.Add($"the match runs {s.TotalRounds} rounds");

            // ---- exactly one taya, and it is the derived one ----------------
            //
            // ⚠️ "EXACTLY ONE" IS TRUE BY CONSTRUCTION AND THIS ASKS ANYWAY. The role is a pure
            // function of the round (`CLAUDE.md` § 4), so a second taya cannot be created by the
            // rules. What CAN happen is a peer holding a stale `DefenderSlot` beside a fresh
            // `RoundNumber`, and that reads as two tayas on two screens.
            if (s.RoundNumber >= 1)
            {
                int derived = MatchRules.DefenderSlotFor(s.RoundNumber);
                if (s.DefenderSlot != derived)
                    faults.Add($"round {s.RoundNumber} says seat {s.DefenderSlot} is the taya; " +
                               $"the schedule derives seat {derived}. The role is not accumulated");
            }

            if (s.DefenderSlot < 0 || s.DefenderSlot >= Balance.PlayerCount)
                faults.Add($"taya seat {s.DefenderSlot} is outside the four seats");

            // ---- the scoreboard --------------------------------------------
            if (s.Scores == null)
            {
                faults.Add("there is no scoreboard");
            }
            else
            {
                if (s.Scores.Length != Balance.PlayerCount)
                    faults.Add($"the scoreboard has {s.Scores.Length} seats, not {Balance.PlayerCount}");

                for (int i = 0; i < s.Scores.Length; i++)
                {
                    // `Scoreboard.Add` clamps at zero, so a negative total means something wrote
                    // the array directly rather than going through the one mutator.
                    if (s.Scores[i] < 0)
                        faults.Add($"seat {i} holds {s.Scores[i]} points; the scoreboard clamps at 0, " +
                                   $"so this was not written through Scoreboard.Add");
                }
            }

            // ---- the buffer ------------------------------------------------
            if (s.IsWarmupBuffer && !s.InProgress)
                faults.Add("the intermission is running on a match that is not in progress");

            // ---- seat ownership --------------------------------------------
            faults.AddRange(CheckSeatOwnership(s.SeatOwners));
            faults.AddRange(CheckSeatClaims(s.SeatClaims));

            return faults;
        }

        /// <summary>
        /// A player owns at most one seat, and a seat has at most one owner.
        ///
        /// ⚠️⚠️ BOTH HALVES ARE REAL AND THEY FAIL DIFFERENTLY. Two seats with one owner is the
        /// spectator-and-a-driven-seat fault (`docs/TODO.md` § 141): somebody re-seats without the
        /// previous seat letting go, and the scoreboard then carries the same name twice. One seat
        /// with two owners is the reconnect fault: a peer whose slot was reused comes back and
        /// both believe they are driving it.
        /// </summary>
        /// ⚠️⚠️ AND ONLY THE FIRST HALF IS FINDABLE HERE, WHICH IS WHY <see cref="CheckSeatClaims"/>
        /// EXISTS. An array indexed by seat holds exactly one owner per seat, so "two peers claim
        /// seat 2" is not a state this parameter can be IN: whichever of the two wrote the cell
        /// last is the only one the checker will ever see. The note above claimed both directions
        /// for months and the data could only carry one of them, which is the exact shape
        /// `docs/TODO.md` § 143.9's brief warns about, *"do not make comments claim an invariant
        /// is covered unless the data model can express its violation."*
        public static List<string> CheckSeatOwnership(string[] owners)
        {
            var faults = new List<string>();
            if (owners == null) return faults;

            if (owners.Length != Balance.PlayerCount)
                faults.Add($"there are {owners.Length} seats, not {Balance.PlayerCount}");

            var seen = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < owners.Length; i++)
            {
                string owner = owners[i];
                if (string.IsNullOrEmpty(owner)) continue;

                if (seen.TryGetValue(owner, out int first))
                    faults.Add($"'{owner}' owns seat {first} and seat {i}. A player holds one seat");
                else
                    seen[owner] = i;
            }

            return faults;
        }

        /// <summary>
        /// The same two rules stated over CLAIMS, where both of them are representable, plus the
        /// one § 141 is about.
        ///
        /// ⚠️⚠️ THREE FAULTS, AND EACH ONE HAS A RECEIPT IN THIS REPOSITORY:
        ///
        ///  1. **One owner driving two seats.** `docs/TODO.md` § 141: somebody re-seats without the
        ///     previous seat letting go and the scoreboard carries the same name twice. 🧑 reported
        ///     it as a duplicate name he could see.
        ///  2. **Two owners driving one seat.** The reconnect fault: `LobbySession.Admit` replaces
        ///     a transport whose durable token matches, and `NetSession.OnClientConnected`
        ///     disconnects the stale socket *after* the new one takes the chair, "without it, it
        ///     can keep submitting movement and verbs for the same player". That window is exactly
        ///     this state, and nothing could ask about it.
        ///  3. **A spectator driving a seat.** § 141's headline, in 🧑's words: **"IF IT isnt
        ///     spectator why do i see spectator hud"**. `Hud.EnterSpectatorMode` had no inverse.
        ///
        /// ⚠️ A HELD SEAT IS NOT A DRIVEN ONE and rule 2 only counts drivers. The reconnect window
        /// deliberately leaves a chair claimed by somebody who is driving nothing, and reporting
        /// that would be reporting the feature.
        /// </summary>
        public static List<string> CheckSeatClaims(SeatClaim[] claims)
        {
            var faults = new List<string>();
            if (claims == null) return faults;

            var drivenSeatFor = new Dictionary<string, int>(StringComparer.Ordinal);
            var driverOfSeat = new Dictionary<int, string>();

            for (int i = 0; i < claims.Length; i++)
            {
                var claim = claims[i];
                if (string.IsNullOrEmpty(claim.Owner)) continue;

                // ⚠️ RULE 3 IS ASKED OF EVERY ROW AND NOT ONLY OF DRIVERS' ROWS, because the
                // whole fault is that the two flags disagree: a body that is being driven while
                // its owner believes it is spectating is precisely what § 141 photographed.
                if (claim.Spectating && claim.Driving && claim.Seat >= 0)
                    faults.Add($"'{claim.Owner}' is spectating and driving seat {claim.Seat} at " +
                               $"once. A spectator has no body");

                if (!claim.Driving || claim.Seat < 0) continue;

                if (claim.Seat >= Balance.PlayerCount)
                {
                    faults.Add($"'{claim.Owner}' drives seat {claim.Seat}, which is outside the " +
                               $"{Balance.PlayerCount} seats");
                    continue;
                }

                if (drivenSeatFor.TryGetValue(claim.Owner, out int already))
                    faults.Add($"'{claim.Owner}' drives seat {already} and seat {claim.Seat}. " +
                               $"A player holds one seat");
                else
                    drivenSeatFor[claim.Owner] = claim.Seat;

                if (driverOfSeat.TryGetValue(claim.Seat, out string other))
                {
                    if (!string.Equals(other, claim.Owner, StringComparison.Ordinal))
                        faults.Add($"seat {claim.Seat} is driven by '{other}' and by " +
                                   $"'{claim.Owner}'. One seat has one driver");
                }
                else
                {
                    driverOfSeat[claim.Seat] = claim.Owner;
                }
            }

            return faults;
        }

        /// <summary>
        /// Who drives each seat, for an observer that wants the old array shape back.
        ///
        /// ⚠️ IT IS LOSSY BY CONSTRUCTION AND THAT IS WHY IT IS NOT WHAT ANYTHING CHECKS. Two
        /// drivers of one seat collapse into whichever came first, which is the exact information
        /// loss `SeatClaims` exists to avoid. It is here so a REPORT can print four tidy rows
        /// after the checker has already asked its questions of the claims.
        /// </summary>
        public static string[] DrivenSeats(SeatClaim[] claims)
        {
            var owners = new string[Balance.PlayerCount];
            if (claims == null) return owners;

            for (int i = 0; i < claims.Length; i++)
            {
                var c = claims[i];
                if (!c.Driving || c.Seat < 0 || c.Seat >= owners.Length) continue;
                if (string.IsNullOrEmpty(owners[c.Seat])) owners[c.Seat] = c.Owner;
            }

            return owners;
        }

        /// <summary>
        /// Everything that can only be said by comparing one state to the one before it.
        ///
        /// ⚠️ THE PAIR IS THE UNIT. Monotonic round progression, "a round cannot finish twice" and
        /// "score does not fall" are all statements about a TRANSITION, and a soak harness that
        /// only checks states would miss every one of them.
        /// </summary>
        public static List<string> CheckTransition(MatchSnapshot before, MatchSnapshot after,
                                                   bool restarted = false, int maxEvents = 2)
        {
            var faults = new List<string>();

            // ---- rounds only ever go forwards, one at a time ---------------
            //
            // ⚠️⚠️ A RESTART IS THE ONE LEGAL WAY BACKWARDS AND IT IS DECLARED BY THE CALLER,
            // NEVER INFERRED FROM THE STATE. The first version of this method inferred it: "the
            // round is back to 1 and the scoreboard is empty, so somebody must have restarted."
            // The seeded fuzzer in `MatchInvariantTests.EveryCorruptedOrderingIsCaught` broke that
            // on its 117th trial, and the case it produced is not hypothetical: **it is
            // `docs/TODO.md` § 82 exactly.** A snapshot the host wrote BEFORE its own arena
            // finished loading carries round 1 and four zero scores, and it arrives at a client
            // that is already playing. Inferring a restart from those two facts is agreeing with
            // the packet that ended the match for every client in the room.
            //
            // So the caller says. `MatchDirector.StartMatch` is the only thing in the game that
            // restarts a match and it already knows; `HostConfirmedInProgress` exists for the same
            // reason one level down. A checker that guesses is a checker that agrees with the bug.
            if (!restarted)
            {
                if (after.RoundNumber < before.RoundNumber)
                    faults.Add($"the round went from {before.RoundNumber} back to {after.RoundNumber} " +
                               $"without the scoreboard being reset. A stale snapshot was applied");

                if (after.RoundNumber > before.RoundNumber + 1)
                    faults.Add($"the round jumped from {before.RoundNumber} to {after.RoundNumber}. " +
                               $"A round was skipped, so somebody advanced twice");
            }

            // ---- the match does not restart itself -------------------------
            if (!before.InProgress && after.InProgress && after.RoundNumber > 1 && !restarted)
                faults.Add($"a match that had ended is in progress again at round {after.RoundNumber} " +
                           $"without a restart");

            // ---- scores --------------------------------------------------
            if (before.Scores != null && after.Scores != null &&
                before.Scores.Length == after.Scores.Length && !restarted)
            {
                for (int i = 0; i < after.Scores.Length; i++)
                {
                    int delta = after.Scores[i] - before.Scores[i];
                    if (delta == 0) continue;

                    // ⚠️⚠️ THE ONLY LEGAL DELTAS ARE SUMS OF `ScoreEvent` VALUES, AND A SINGLE
                    // STEP IS ONE OF THEM. This is the check that catches a point awarded twice
                    // for one gameplay event: 200 where the event pays 100 is not a bigger award,
                    // it is two awards. It is stated as "reachable in one event" rather than as an
                    // exact value because a 5 Hz snapshot can legitimately carry two events.
                    if (!IsReachableDelta(delta, maxEvents))
                        faults.Add($"seat {i} moved by {delta} points, which is not any " +
                                   $"ScoreEvent's value. Something wrote the scoreboard directly");
                }
            }

            // ---- the taya schedule stays derived across the boundary -------
            if (after.RoundNumber >= 1 &&
                after.DefenderSlot != MatchRules.DefenderSlotFor(after.RoundNumber))
                faults.Add($"after advancing to round {after.RoundNumber} the taya is seat " +
                           $"{after.DefenderSlot} rather than the derived " +
                           $"{MatchRules.DefenderSlotFor(after.RoundNumber)}");

            return faults;
        }

        /// <summary>
        /// Whether a score delta could be the sum of at most <paramref name="maxEvents"/> awards.
        ///
        /// ⚠️⚠️ THE BOUND IS THE OBSERVER'S SAMPLE RATE AND IT IS AN ARGUMENT THE CALLER HAS TO
        /// MAKE. `MatchRpc` writes `SyncWorld` at 5 Hz, so a network snapshot pair spans 200 ms:
        /// a defence tick and a tag inside one window is ordinary and three separate awards is
        /// not, which is why 2 is the default and the only value the wire needs.
        ///
        /// ⚠️⚠️ AND THE SOAK HARNESS BROKE THAT ASSUMPTION ON ITS FIRST RUN, CORRECTLY. It steps
        /// at `Time.timeScale = 60`, so a single frame covers about a second of game time and the
        /// defence tick pays every `Balance.DefenseTickInterval`. Seat 0 legitimately moved by 70
        /// in one observed step, which is seven ticks, and the check called it a direct write.
        /// **The check was not wrong and the observer was not wrong; the bound belonged to the
        /// observer and was hard-coded in the rule.** A checker that cannot be told how long it
        /// looked away is a checker that can only be used at one sample rate.
        ///
        /// ⚠️ AN UNBOUNDED VERSION WOULD ACCEPT EVERY NUMBER, which is the same as not having the
        /// check, so the answer is a parameter rather than removing the limit.
        /// </summary>
        public static bool IsReachableDelta(int delta, int maxEvents = 2)
        {
            if (maxEvents < 1) maxEvents = 1;

            var values = ScoreEventValues();

            // Reachable sums, breadth first, capped at `maxEvents` terms.
            var reachable = new HashSet<int> { 0 };

            for (int step = 0; step < maxEvents; step++)
            {
                var next = new HashSet<int>(reachable);
                foreach (int sum in reachable)
                    foreach (int value in values)
                        next.Add(sum + value);
                reachable = next;

                if (reachable.Contains(delta)) return true;
            }

            return reachable.Contains(delta);
        }

        private static int[] ScoreEventValues()
        {
            var events = (ScoreEvent[])Enum.GetValues(typeof(ScoreEvent));
            var values = new int[events.Length];
            for (int i = 0; i < events.Length; i++) values[i] = MatchRules.PointsFor(events[i]);
            return values;
        }

        private static int TotalOf(int[] scores)
        {
            if (scores == null) return 0;
            int sum = 0;
            for (int i = 0; i < scores.Length; i++) sum += scores[i];
            return sum;
        }

        /// <summary>
        /// Whether two peers describing the same match agree about everything that decides it.
        ///
        /// ⚠️⚠️ THIS IS THE TOURNAMENT QUESTION AND IT IS NOT "ARE THE SCORES EQUAL". A host and a
        /// client can hold identical scoreboards and still disagree about the WINNER if one of
        /// them computes the ranking its own way, and they can agree about the winner while
        /// disagreeing about whose round it is. Every field below has cost this game something:
        /// § 82 is the round, § 141 is the seat, and `MatchDirector.ApplyNetworkScoreEvent`
        /// exists because a client that derives its own score disagrees at exactly the moments
        /// that matter.
        /// </summary>
        public static List<string> CheckPeersAgree(string aName, MatchSnapshot a,
                                                   string bName, MatchSnapshot b)
        {
            var faults = new List<string>();

            if (a.RoundNumber != b.RoundNumber)
                faults.Add($"{aName} is on round {a.RoundNumber}, {bName} on round {b.RoundNumber}");

            if (a.DefenderSlot != b.DefenderSlot)
                faults.Add($"{aName} says seat {a.DefenderSlot} is the taya, {bName} says " +
                           $"seat {b.DefenderSlot}");

            if (a.InProgress != b.InProgress)
                faults.Add($"{aName} says the match is {(a.InProgress ? "running" : "over")} and " +
                           $"{bName} says it is {(b.InProgress ? "running" : "over")}");

            if (a.Scores != null && b.Scores != null && a.Scores.Length == b.Scores.Length)
            {
                for (int i = 0; i < a.Scores.Length; i++)
                    if (a.Scores[i] != b.Scores[i])
                        faults.Add($"seat {i}: {aName} has {a.Scores[i]}, {bName} has {b.Scores[i]}");
            }

            int winnerA = WinnerOf(a.Scores);
            int winnerB = WinnerOf(b.Scores);
            if (winnerA != winnerB)
                faults.Add($"{aName} has winner {winnerA} and {bName} has winner {winnerB}. " +
                           $"Two peers cannot end one match differently");

            // ---- and who is in which chair ---------------------------------
            //
            // ⚠️⚠️ OWNERSHIP WAS MISSING FROM THIS METHOD AND ITS OWN HEADER SAID IT SHOULD NOT
            // BE: *"§ 141 is the seat"*. It compared the round, the taya, the progress flag, the
            // scores and the winner, and every one of those can be identical on two peers that
            // disagree about WHO seat 2 is. That disagreement is not cosmetic in this game: the
            // seat decides whose tsinelas is whose, who the tag lands on and which line of the
            // scoreboard a point is written to, so two peers that agree on four numbers and
            // disagree about a chair are describing two different matches with one result.
            faults.AddRange(CompareSeats(aName, a, bName, b));

            return faults;
        }

        /// <summary>
        /// Whether two peers agree about who is sitting where.
        ///
        /// ⚠️ IT READS THE DRIVEN SEATS RATHER THAN THE RAW CLAIMS, because a claim list is one
        /// peer's own bookkeeping and legitimately differs: a host holds a departed player's chair
        /// against their token and a client has never heard of that. **What must agree is who is
        /// DRIVING**, which is the only part of the claim that reaches the arena.
        /// </summary>
        private static List<string> CompareSeats(string aName, MatchSnapshot a,
                                                 string bName, MatchSnapshot b)
        {
            var faults = new List<string>();

            string[] left = a.SeatClaims != null ? DrivenSeats(a.SeatClaims) : a.SeatOwners;
            string[] right = b.SeatClaims != null ? DrivenSeats(b.SeatClaims) : b.SeatOwners;

            if (left == null || right == null) return faults;
            if (left.Length != right.Length)
            {
                faults.Add($"{aName} describes {left.Length} seats and {bName} describes " +
                           $"{right.Length}");
                return faults;
            }

            for (int i = 0; i < left.Length; i++)
            {
                string one = left[i] ?? "";
                string two = right[i] ?? "";

                // ⚠️ AN EMPTY SEAT ON ONE SIDE ONLY IS NOT A DISAGREEMENT WORTH FAILING ON. A
                // client is routinely mid-build (`docs/TODO.md` § 82.1) and a bot-filled chair has
                // no owner token at all; the fault worth naming is two peers naming DIFFERENT
                // people, which is the one that decides points wrongly.
                if (one.Length == 0 || two.Length == 0) continue;

                if (!string.Equals(one, two, StringComparison.Ordinal))
                    faults.Add($"seat {i}: {aName} says '{one}' and {bName} says '{two}'. " +
                               $"A seat decides whose tsinelas is whose");
            }

            return faults;
        }

        private static int WinnerOf(int[] scores)
        {
            if (scores == null) return -1;
            var board = new Scoreboard();
            board.SetAll(scores);
            return board.WinningSlot();
        }
    }
}
