using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// PHASE 12: which map a lobby plays next, by rotation when nobody asks and by vote when
    /// somebody does.
    ///
    /// ⚠️⚠️ THIS IS THE PART OF PHASE 12 `docs/TODO.md` § 128.2 CALLS **"the genuinely unbuilt
    /// cheap win"**, and `FUTURE.md` § 12 says why it comes before content: *"A map is the most
    /// expensive content in the game. Map rotation and a map vote are nearly free and buy most of
    /// the same freshness. Build those before building a fourth map."* § 19.12 orders it third in
    /// the phase and repeats the instruction: *"Do these before building a fourth map."*
    ///
    /// ⚠️⚠️ THE ROTATION AND THE VOTE ARE ONE FEATURE AND NOT TWO, WHICH IS THE DESIGN DECISION
    /// WORTH READING BEFORE CHANGING EITHER. A vote answers "what do these four people want"; a
    /// rotation answers "what happens when nobody says". Building the vote alone leaves a lobby
    /// where four abstentions replay the same map for ever, which is the exact staleness the
    /// feature exists to remove. Building the rotation alone takes the choice away from a room
    /// that has one. **<see cref="Decide"/> is the whole feature: the vote decides when there are
    /// votes and the rotation decides when there are not.**
    ///
    /// ⚠️⚠️ EVERY FUNCTION HERE IS PURE AND DETERMINISTIC, AND THAT IS A NETCODE REQUIREMENT
    /// RATHER THAN A STYLE. `docs/VISION.md` § 4: *"the host decides everything that scores"*, and
    /// the host decides this too — but every peer DRAWS the result, and a peer that computed a
    /// different winner from the same votes would show the wrong map in the lobby until the next
    /// sync corrected it. `CustomGameRules.MirrorIndex` makes the same argument for the same
    /// reason: *"every machine computes the same answer from the same UTC week with no service,
    /// no document and no wire field"*.
    ///
    /// ⚠️ NOTHING IN HERE KNOWS WHAT A MAP IS. It counts them and indexes them, exactly as
    /// `MirrorIndex` counts a roster, so `SceneFlow.Maps` can grow without this file changing and
    /// a headless test can assert the whole feature with no scene on disk.
    /// </summary>
    public static class MapRotationRules
    {
        /// <summary>A seat that has not voted. ⚠️ NOT 0: zero is a real map index, and the first
        /// version of any tally that conflates "no answer" with "the first option" gives every
        /// silent lobby to Eskinita and looks like a working vote.</summary>
        public const int NoVote = -1;

        /// <summary>
        /// The next map in a plain rotation.
        ///
        /// ⚠️ IT IS `current + 1`, NOT A RANDOM PICK, AND THAT IS DELIBERATE. Random repeats: with
        /// three maps a uniform draw replays the same one about a third of the time, which is the
        /// staleness this feature exists to remove, and a player cannot tell a repeat from a bug.
        /// A cycle visits every map before revisiting any, which is the strongest freshness
        /// guarantee available and needs no state beyond the map you just played.
        /// </summary>
        public static int NextInRotation(int current, int mapCount)
        {
            if (mapCount <= 0) return 0;
            if (mapCount == 1) return 0;

            int from = current < 0 || current >= mapCount ? -1 : current;
            return (from + 1) % mapCount;
        }

        /// <summary>
        /// The map a fresh lobby opens on, derived from the week the way MIRROR's character is.
        ///
        /// ⚠️ IT EXISTS SO TWO PEOPLE WHO HAVE NEVER PLAYED TOGETHER DO NOT BOTH OPEN ON MAP 0.
        /// `NextInRotation` needs somewhere to start, and "whatever this machine last played" is a
        /// per-machine answer that makes a fresh install and a veteran disagree about what a new
        /// lobby looks like. The week number is the same on every machine with no service, which
        /// is `CustomGameRules.MirrorIndex`'s argument exactly, and it is counted from the same
        /// <see cref="RatingRules.SeasonOneStartUtc"/> so the two rotations do not drift past each
        /// other by a few days a year.
        /// </summary>
        public static int OpeningMap(int mapCount, DateTime utc)
        {
            if (mapCount <= 0) return 0;

            var start = RatingRules.SeasonOneStartUtc;

            // ⚠️ A PRE-EPOCH CLOCK IS A REAL CASE AND IT MUST NOT GO NEGATIVE. A venue machine
            // with a flat CMOS battery boots in 2000, and C# `%` keeps the sign of the left
            // operand, so a negative week would index backwards out of the array.
            // `CustomGameRules.MirrorIndex` carries a test for exactly this and this is the same
            // guard rather than a new idea.
            double days = (utc - start).TotalDays;
            long weeks = (long)Math.Floor(days / 7.0);

            long index = weeks % mapCount;
            if (index < 0) index += mapCount;

            return (int)index;
        }

        /// <summary>
        /// Who won a map vote, or <see cref="NoVote"/> when nobody voted at all.
        ///
        /// ⚠️⚠️ THE TIE-BREAK PREFERS A MAP THAT IS NOT THE ONE YOU JUST PLAYED, AND THAT IS THE
        /// WHOLE POINT OF THE FEATURE EXPRESSED AS A RULE. The obvious tie-break is "lowest index
        /// wins", and it is wrong here in a way that is invisible in a unit test and obvious in a
        /// lobby: with a 2-2 split between the current map and another, lowest-index hands it to
        /// whichever happens to sort first, which is the CURRENT map half the time. `FUTURE.md`
        /// § 12 bought this feature to *"buy most of the same freshness"* as a new map, and a tie
        /// that silently replays the same street buys none of it.
        ///
        /// ⚠️ A MAJORITY CAN STILL KEEP THE MAP THEY ARE ON. This only decides TIES: three votes
        /// for the current map beats one for anything else, and a room that loves Eskinita is
        /// allowed to stay on Eskinita. What it cannot do is stay there by accident.
        ///
        /// ⚠️ AND THE SECOND TIE-BREAK IS THE LOWEST INDEX, which is arbitrary and says so.
        /// Anything cleverer would need state the lobby does not have, and an arbitrary rule that
        /// every peer computes identically beats a fair one they can disagree about
        /// (<see cref="MapRotationRules"/>'s header on determinism).
        /// </summary>
        public static int TallyVote(IReadOnlyList<int> votes, int mapCount, int currentMap)
        {
            if (votes == null || mapCount <= 0) return NoVote;

            var counts = new int[mapCount];
            int cast = 0;

            for (int i = 0; i < votes.Count; i++)
            {
                int v = votes[i];
                if (v < 0 || v >= mapCount) continue;

                counts[v]++;
                cast++;
            }

            if (cast == 0) return NoVote;

            int best = NoVote;
            int bestCount = 0;

            for (int map = 0; map < mapCount; map++)
            {
                if (counts[map] == 0) continue;

                if (counts[map] > bestCount)
                {
                    best = map;
                    bestCount = counts[map];
                    continue;
                }

                if (counts[map] != bestCount) continue;

                // A tie. The current map loses it; otherwise the lower index already holds.
                if (best == currentMap && map != currentMap) best = map;
            }

            return best;
        }

        /// <summary>
        /// The whole feature: what the lobby plays next, given how the room voted and where it
        /// has just been.
        ///
        /// ⚠️⚠️ THE FALLBACK IS THE ROTATION AND NOT "STAY HERE", WHICH IS THE ONE LINE THAT
        /// MAKES THIS WORTH BUILDING. A lobby where nobody presses anything is the COMMON case,
        /// not the edge case: four people who have just finished a match are looking at a
        /// scoreboard, not at a ballot. If silence meant "same map again", the feature would only
        /// ever fire for rooms that were already bored enough to act, which is precisely the rooms
        /// that did not need it.
        /// </summary>
        public static int Decide(IReadOnlyList<int> votes, int mapCount, int currentMap)
        {
            if (mapCount <= 0) return 0;

            int voted = TallyVote(votes, mapCount, currentMap);
            if (voted != NoVote) return voted;

            return NextInRotation(currentMap, mapCount);
        }

        /// <summary>
        /// How long a lobby collects votes for.
        ///
        /// ⚠️ TWENTY SECONDS, AGAINST `BotFillRules.CasualFillAfterSeconds` OF 45 AND THE
        /// INTERMISSION THIS RIDES INSIDE. Phase 11's shipped argument is *"a 45-second queue that
        /// ends in a playable match beats a 4-minute queue that ends in nothing"*, and the same
        /// impatience applies here one level down: a vote is a thing standing between four people
        /// and the next match. Long enough to read three names and press one, short enough that a
        /// player who has walked away does not hold the room.
        /// </summary>
        public const float VoteSeconds = 20.0f;

        /// <summary>
        /// True once every seat has answered, so the lobby can stop waiting early.
        ///
        /// ⚠️ IT COUNTS SEATS, NOT VOTES, because an empty seat can never answer and a room of two
        /// would otherwise always wait the full <see cref="VoteSeconds"/>. `LobbySession` knows how
        /// many seats are occupied; this only needs the number.
        /// </summary>
        public static bool EveryoneHasVoted(IReadOnlyList<int> votes, int occupiedSeats)
        {
            if (votes == null || occupiedSeats <= 0) return false;

            int cast = 0;
            for (int i = 0; i < votes.Count && i < occupiedSeats; i++)
                if (votes[i] != NoVote) cast++;

            return cast >= occupiedSeats;
        }
    }
}
