using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// What is at stake in a queue. ⚠️ IT IS NOT A RULESET.
    ///
    /// `INSPIRATION.md` § 3.1: modes are rulesets and queues are stakes. A ranked MODE would be a
    /// third game to balance and would stop practice in casual transferring to ranked, which is
    /// the fastest way to make a competitive game feel unfair. The mode is chosen inside the
    /// queue, never beside it, and every rule in `docs/Design.md` is identical in both.
    /// </summary>
    public enum QueueStake
    {
        Casual = 0,
        Ranked = 1,
    }

    /// <summary>
    /// The input device a seat is played on.
    ///
    /// ⚠️ THERE IS ONLY ONE OF THESE TODAY AND THE ENUM EXISTS ANYWAY. `FUTURE.md` § 7 asks for
    /// pools separated by input device "which is free and removes the entire aim-assist argument
    /// before it starts", and the moment to make a pool key carry a field is before anything is
    /// advertising it, not after. `TumbangPreso.inputactions` has zero gamepad bindings as of
    /// 2026-08-31 (`FUTURE.md` § 0.6's own check), so every real lobby advertises
    /// <see cref="KeyboardMouse"/> and Phase 14 turns the second value on without a wire change.
    /// </summary>
    public enum InputDevice
    {
        KeyboardMouse = 0,
        Gamepad = 1,
        Touch = 2,
    }

    /// <summary>
    /// The platform family a seat is played on, for the same reason as <see cref="InputDevice"/>.
    ///
    /// ⚠️ FAMILY, NOT `RuntimePlatform`. Windows, Linux and Mac desktop builds of this game play
    /// identically and splitting them would be splitting a population for nothing. What actually
    /// differs is a phone (Phase 15) and a browser build, so those are the values that exist.
    /// </summary>
    public enum PlatformFamily
    {
        Desktop = 0,
        Web = 1,
        Mobile = 2,
    }

    /// <summary>
    /// An inclusive rating window, advertised by a lobby and carried by a queuing player.
    /// </summary>
    public readonly struct RatingBand
    {
        public readonly int Low;
        public readonly int High;

        public RatingBand(int low, int high)
        {
            Low = Math.Min(low, high);
            High = Math.Max(low, high);
        }

        public bool Contains(int rating) => rating >= Low && rating <= High;

        public int Width => High - Low;

        /// <summary>Half the width, which is the number the widening schedule counts in.</summary>
        public int HalfWidth => Width / 2;

        public int Centre => Low + HalfWidth;

        public override string ToString() => $"{Low}-{High}";
    }

    /// <summary>
    /// What a lobby says about itself so a queuing player can decide without a second query.
    ///
    /// ⚠️⚠️ EVERY FIELD HERE RIDES THE LOBBY RECORD `ServerQuery` ALREADY READS EVERY 4 SECONDS,
    /// AND THAT IS THE WHOLE COST CONTROL. `FUTURE.md` § 19.7: "this must not raise the query rate
    /// against the free tier". A matchmaker that asked the service its own questions would double
    /// the request rate of the one service this game cannot do without, so the queue reads the
    /// browse loop's existing answers and adds nothing. Six extra strings on a record that is
    /// already being fetched cost zero requests.
    ///
    /// ⚠️ THE SEATED MINIMUM AND MAXIMUM ARE HERE RATHER THAN THE FOUR RATINGS. The joiner needs
    /// the SPREAD it would create, and spread is decided by the extremes; publishing four numbers
    /// would publish three players' ratings to every browser in the game for no extra decision.
    /// </summary>
    public readonly struct LobbyAdvert
    {
        public readonly string PoolKey;
        public readonly RatingBand Band;
        public readonly int SeatedLow;
        public readonly int SeatedHigh;
        public readonly int Seated;
        public readonly int Capacity;
        public readonly bool InProgress;
        public readonly bool Backfilling;
        public readonly string HostPlayerId;

        public LobbyAdvert(string poolKey, RatingBand band, int seatedLow, int seatedHigh,
                           int seated, int capacity, bool inProgress, bool backfilling,
                           string hostPlayerId)
        {
            PoolKey = poolKey ?? "";
            Band = band;
            SeatedLow = seatedLow;
            SeatedHigh = seatedHigh;
            Seated = seated;
            Capacity = capacity;
            InProgress = inProgress;
            Backfilling = backfilling;
            HostPlayerId = hostPlayerId ?? "";
        }
    }

    /// <summary>Why a lobby was passed over. Every refusal is nameable, so a queue that is not
    /// finding anything can say what it is rejecting instead of spinning.</summary>
    public enum JoinRefusal
    {
        None = 0,
        WrongPool,
        Full,
        InProgressWithNoSeat,
        OutsideTheirBand,
        OutsideOurBand,
        SpreadTooWide,
        Blocked,
        OurOwn,
    }

    /// <summary>
    /// Rating-banded quick match, as arithmetic. Nothing here knows what a lobby service is.
    ///
    /// ⚠️⚠️ THE MATCH-QUALITY METRIC IS THE SPREAD OF FOUR RATINGS AND NOT THE GAP BETWEEN TWO
    /// AVERAGES, AND THIS IS THE ONE THING IN THIS FILE THAT IS EASY TO GET WRONG BY COPYING
    /// SOMEBODY ELSE. `FUTURE.md` § 7: "A 4-player free for all matches differently from a team
    /// game. There is no team to balance. The job is not make two sides equal, it is put four
    /// players of similar skill in one room." A lobby holding one 1400 and three 900s is a bad
    /// match for all four people in it: the 1400 wins every round and the other three are playing
    /// a different game. **Every team-based fairness formula calls that lobby balanced**, because
    /// 1400+900 against 900+900 is a 250-point gap and there are team games shipping today that
    /// would take it. <see cref="Spread"/> is the metric; <see cref="BestTeamSplitGap"/> exists
    /// only so a test can prove the two disagree on exactly that lobby, and nothing in the game
    /// calls it.
    ///
    /// ⚠️ EVERY NUMBER BELOW IS A STARTING POINT FOR A MEASUREMENT. `FUTURE.md` § 0.6: the numbers
    /// in the plan files are illustrations. There is no queue population to measure against yet,
    /// so these are the plan's own values, written once, in one place, so a real measurement is a
    /// one-line change rather than a hunt.
    /// </summary>
    public static class MatchmakingRules
    {
        // ---- the widening schedule, from `FUTURE.md` § 7 ---------------------------------

        /// <summary>Half-width of the band a queue opens with, in rating points.</summary>
        public const int StartHalfWidth = 100;

        /// <summary>How much the half-width grows each step.</summary>
        public const int WidenStep = 100;

        /// <summary>How long a step lasts, in seconds.</summary>
        public const float WidenSeconds = 15.0f;

        /// <summary>
        /// The widest the band gets before the queue takes anybody.
        ///
        /// ⚠️ 500 IS WHERE BANDING STOPS MEANING ANYTHING, NOT WHERE IT STOPS WORKING. A window
        /// 1000 points wide contains most of a ladder, so a queue that has widened this far has
        /// already said "skill matching has failed, a match is better than no match". Naming the
        /// last step honestly is what lets <see cref="TakesAnybody"/> exist rather than the band
        /// growing forever and the UI lying about it.
        /// </summary>
        public const int MaxHalfWidth = 500;

        /// <summary>
        /// How many widening steps there are, which is what the UI draws as progress.
        ///
        /// ⚠️ IT IS DERIVED, NOT WRITTEN. A second constant here would be a number that can
        /// disagree with the three above it, and `docs/TODO.md` § 88.1a is the entry about two
        /// constants for one quantity.
        /// </summary>
        public static int WidenSteps => (MaxHalfWidth - StartHalfWidth) / WidenStep;

        /// <summary>Seconds after which the band stops widening and the queue takes anybody.</summary>
        public static float SecondsToWidest => WidenSteps * WidenSeconds;

        /// <summary>
        /// The band this player is searching with after <paramref name="secondsQueued"/>.
        /// </summary>
        public static RatingBand BandFor(int rating, float secondsQueued)
        {
            int half = HalfWidthAt(secondsQueued);
            return new RatingBand(rating - half, rating + half);
        }

        public static int HalfWidthAt(float secondsQueued)
        {
            if (secondsQueued <= 0.0f) return StartHalfWidth;

            int steps = (int)(secondsQueued / WidenSeconds);
            int half = StartHalfWidth + (steps * WidenStep);
            return half > MaxHalfWidth ? MaxHalfWidth : half;
        }

        /// <summary>
        /// True once the band has reached its widest and the queue stops filtering on rating.
        ///
        /// ⚠️ THIS IS A REAL STATE AND NOT A ROUNDING ARTEFACT. At the widest step the queue will
        /// take a lobby whose band does not contain us and whose spread we would blow out, because
        /// the alternative after that many seconds is not a better match, it is no match. The UI
        /// says so in words rather than leaving the player to notice.
        /// </summary>
        public static bool TakesAnybody(float secondsQueued) => HalfWidthAt(secondsQueued) >= MaxHalfWidth;

        // ---- the quality metric ---------------------------------------------------------

        /// <summary>
        /// The match-quality metric: how far apart the best and worst player in a room are.
        ///
        /// ⚠️⚠️ LOWER IS BETTER AND THERE IS NO TEAM IN IT. See this class's header.
        /// </summary>
        public static int Spread(IReadOnlyList<int> ratings)
        {
            if (ratings == null || ratings.Count == 0) return 0;

            int low = int.MaxValue;
            int high = int.MinValue;

            for (int i = 0; i < ratings.Count; i++)
            {
                if (ratings[i] < low) low = ratings[i];
                if (ratings[i] > high) high = ratings[i];
            }

            return high - low;
        }

        /// <summary>The spread a room would have if this rating sat down in it.</summary>
        public static int SpreadWith(int seatedLow, int seatedHigh, int candidate)
        {
            if (seatedLow > seatedHigh) return 0;   // an empty room takes anybody

            int low = Math.Min(seatedLow, candidate);
            int high = Math.Max(seatedHigh, candidate);
            return high - low;
        }

        /// <summary>
        /// ⚠️⚠️ THE WRONG METRIC, IN THE CORE ON PURPOSE, CALLED BY NOTHING IN THE GAME.
        ///
        /// The smallest gap between the two team averages over every way of splitting four
        /// players into two pairs. This is what a team-based matchmaker optimises and it is the
        /// formula a session copying League, Overwatch or Valorant would reach for first.
        /// `MatchQualityTests.ALobbyOfOne1400AndThreeNineHundredsIsRefusedBySpreadAndAcceptedByTeamBalance`
        /// asserts the two disagree on exactly the lobby `FUTURE.md` § 7 names, which is what
        /// makes the rule un-deletable by somebody who thinks the spread check looks naive.
        ///
        /// ⚠️ DELETING THIS DELETES THE PROOF, NOT DEAD CODE. If it ever becomes genuinely
        /// unreachable, the test is what has gone missing.
        /// </summary>
        public static double BestTeamSplitGap(IReadOnlyList<int> ratings)
        {
            if (ratings == null || ratings.Count != 4) return 0.0;

            // Three distinct ways to split four players into two pairs.
            var splits = new[]
            {
                new[] { 0, 1, 2, 3 },
                new[] { 0, 2, 1, 3 },
                new[] { 0, 3, 1, 2 },
            };

            double best = double.MaxValue;

            foreach (var split in splits)
            {
                double a = (ratings[split[0]] + ratings[split[1]]) / 2.0;
                double b = (ratings[split[2]] + ratings[split[3]]) / 2.0;
                double gap = Math.Abs(a - b);
                if (gap < best) best = gap;
            }

            return best;
        }

        /// <summary>
        /// The widest spread a room may have while the searcher's band is this wide.
        ///
        /// ⚠️ IT IS THE FULL BAND WIDTH, WHICH IS TWICE THE HALF-WIDTH, AND THAT IS THE ONLY
        /// COHERENT ANSWER. If the searcher will accept anybody within plus or minus 100 of
        /// itself, then the widest room it can create is one holding somebody 100 above and
        /// somebody 100 below, which is a spread of 200. Any other number here would let the
        /// queue accept a room it would not have created.
        /// </summary>
        public static int MaxAcceptableSpread(float secondsQueued) => HalfWidthAt(secondsQueued) * 2;

        // ---- the pool key ---------------------------------------------------------------

        /// <summary>
        /// The advertised identity of a queue. Two players match only when these strings are equal.
        ///
        /// ⚠️⚠️ IT IS A STRING AND NOT A PACKED INTEGER, FOR THE SAME REASON EVERY COSMETIC ID IS
        /// (`FUTURE.md` § 5). This value crosses the wire, is stored in a lobby record, and is
        /// compared by a build that may be older than the one that wrote it. An enum ordinal that
        /// shifts when a value is inserted silently matches two pools that are not the same pool,
        /// and nothing errors; a string that stops matching simply stops matching.
        ///
        /// ⚠️ THE PROTOCOL VERSION IS IN THE KEY. Two builds that would refuse each other at
        /// connection approval must not be offered each other by the queue first: the player would
        /// watch a queue find a match and then bounce off it with a version message. Handing the
        /// version in rather than reading it keeps this file engine-free.
        /// </summary>
        public static string PoolKey(GameMode mode, QueueStake stake, InputDevice device,
                                     PlatformFamily platform, int protocolVersion)
        {
            return $"v{protocolVersion}.{mode}.{stake}.{device}.{platform}";
        }

        // ---- the decision ---------------------------------------------------------------

        /// <summary>
        /// Whether this lobby is worth joining, and if not, precisely why.
        ///
        /// ⚠️⚠️ BOTH BANDS ARE CHECKED, NOT ONE. `FUTURE.md` § 19.7 says "search for a joinable
        /// lobby whose band contains the local player", and that alone is asymmetric: a host that
        /// has been waiting three minutes advertises a 1000-wide band and would swallow a player
        /// who has been queuing for four seconds and is still asking for plus or minus 100. The
        /// searcher's own band has to contain the host too, or the widening schedule only ever
        /// applies to whoever waited longer. The two refusals are named separately so a queue can
        /// say which side is still too narrow.
        ///
        /// ⚠️ THE SPREAD CHECK IS SEPARATE FROM BOTH BANDS AND IS NOT REDUNDANT. Two bands can
        /// both contain the other's centre while the room already holds somebody at an extreme;
        /// the band is about the HOST and the spread is about the ROOM.
        /// </summary>
        public static JoinRefusal Evaluate(LobbyAdvert advert, string myPlayerId, int myRating,
                                           float secondsQueued, string myPoolKey,
                                           Func<string, bool> isBlocked, int seatsNeeded = 1)
        {
            if (!string.IsNullOrEmpty(advert.HostPlayerId) &&
                advert.HostPlayerId == myPlayerId) return JoinRefusal.OurOwn;

            if (advert.PoolKey != myPoolKey) return JoinRefusal.WrongPool;

            // ⚠️ BLOCKING IS CHECKED BEFORE THE RATING, ALWAYS. `FUTURE.md` § 6: "blocking must
            // survive matchmaking: a blocked player is never queued into your match". A block
            // that only applied while the band was narrow would be a block that expires after
            // seventy-five seconds of queuing, which is not a block.
            if (isBlocked != null && !string.IsNullOrEmpty(advert.HostPlayerId) &&
                isBlocked(advert.HostPlayerId)) return JoinRefusal.Blocked;

            // ⚠️ ROOM FOR THE WHOLE PARTY, NOT FOR ONE. `PartyRules.SeatsNeeded`: a party of three
            // joining a lobby with two free chairs is one member left standing on the menu
            // wondering what happened.
            if (seatsNeeded < 1) seatsNeeded = 1;
            if (advert.Capacity - advert.Seated < seatsNeeded) return JoinRefusal.Full;

            // A running match with a free seat is a backfill and is joinable; a running match
            // that has not advertised a seat is not.
            if (advert.InProgress && !advert.Backfilling) return JoinRefusal.InProgressWithNoSeat;

            if (TakesAnybody(secondsQueued)) return JoinRefusal.None;

            if (!advert.Band.Contains(myRating)) return JoinRefusal.OutsideTheirBand;

            var mine = BandFor(myRating, secondsQueued);
            if (advert.SeatedLow <= advert.SeatedHigh && !mine.Contains(advert.Band.Centre))
                return JoinRefusal.OutsideOurBand;

            int spread = SpreadWith(advert.SeatedLow, advert.SeatedHigh, myRating);
            if (spread > MaxAcceptableSpread(secondsQueued)) return JoinRefusal.SpreadTooWide;

            return JoinRefusal.None;
        }

        /// <summary>
        /// The best of the lobbies on offer, or -1.
        ///
        /// ⚠️ TIGHTEST SPREAD WINS, THEN FULLEST ROOM. Filling a room that already has three
        /// people starts a match now; filling a room that has one starts a wait. Sorting on
        /// spread first is what keeps the quality metric the thing that decides, and using
        /// occupancy only to break a tie is what stops the queue preferring an empty room forever.
        /// </summary>
        public static int Best(IReadOnlyList<LobbyAdvert> adverts, string myPlayerId, int myRating,
                              float secondsQueued, string myPoolKey, Func<string, bool> isBlocked,
                              int seatsNeeded = 1)
        {
            int bestIndex = -1;
            int bestSpread = int.MaxValue;
            int bestSeated = -1;

            for (int i = 0; adverts != null && i < adverts.Count; i++)
            {
                if (Evaluate(adverts[i], myPlayerId, myRating, secondsQueued, myPoolKey, isBlocked,
                             seatsNeeded) != JoinRefusal.None) continue;

                int spread = SpreadWith(adverts[i].SeatedLow, adverts[i].SeatedHigh, myRating);

                if (spread > bestSpread) continue;
                if (spread == bestSpread && adverts[i].Seated <= bestSeated) continue;

                bestIndex = i;
                bestSpread = spread;
                bestSeated = adverts[i].Seated;
            }

            return bestIndex;
        }

        // ---- what the player is told ----------------------------------------------------

        /// <summary>
        /// The queue's own sentence. ⚠️ **A SPINNER IS NOT A STATE** (`FUTURE.md` § 0.5b, phase 7
        /// row): the surface has to say the mode, the time elapsed and how to cancel, and § 7 asks
        /// for the widening to be SHOWN "so a long queue reads as progress rather than as a hang".
        /// This is the widening half, in words, and it is in the core so the wording is asserted
        /// rather than typed into a label.
        /// </summary>
        public static string SearchLabel(int rating, float secondsQueued)
        {
            if (TakesAnybody(secondsQueued))
                return "Searching every skill level";

            var band = BandFor(rating, secondsQueued);
            return $"Searching {band.Low} to {band.High} skill";
        }

        /// <summary>
        /// How far the widening has come, 0 to 1, for a bar that fills.
        ///
        /// ⚠️ IT REACHES 1 AND STAYS THERE. The widening finishing is not the queue failing, and a
        /// bar that resets or that never arrives is the "hang" this whole surface exists to avoid.
        /// </summary>
        public static float WideningProgress(float secondsQueued)
        {
            if (WidenSteps <= 0) return 1.0f;
            if (secondsQueued <= 0.0f) return 0.0f;

            float p = secondsQueued / SecondsToWidest;
            return p >= 1.0f ? 1.0f : p;
        }

        /// <summary>
        /// The one thing about this game's format that makes a mismatched room survivable, said
        /// out loud in the queue.
        ///
        /// ⚠️⚠️ `FUTURE.md` § 7: "THE TAYA ROTATION IS WHAT MAKES THIS FAIR AT ALL, and it is
        /// worth saying in the queue UI: everyone defends once, so a bad first round is not a lost
        /// match." `INSPIRATION.md` § 4.5 is titled "the taya rotation is a gift and nobody knows
        /// it". The role is derived, `(round - 1) % 4` (`CLAUDE.md` § 4), so this sentence is true
        /// by construction and not by bookkeeping, and the game has never once said it to a
        /// player. It lives in the core beside the rule it describes so it cannot drift from it.
        /// </summary>
        public const string TayaRotationPromise =
            "The taya rotates every round, so everyone defends exactly once. A bad first round is not a lost match.";
    }
}
