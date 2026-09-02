using System;

namespace TumbangPreso.Core
{
    /// <summary>
    /// PHASE 11. When a queue gives up waiting for people and offers bots instead, and what that
    /// costs the result.
    ///
    /// ⚠️⚠️ THIS IS THE POPULATION PROBLEM AND IT IS THE DIFFERENCE BETWEEN A GAME THAT LIVES
    /// AND ONE THAT DOES NOT. `FUTURE.md` § 11: *"a 4-player game with 30 concurrent players has a
    /// queue problem that no amount of ranked polish fixes"*, and *"a 45-second queue that ends in
    /// a playable match beats a 4-minute queue that ends in nothing"*. Everything in this file is
    /// that sentence turned into numbers.
    ///
    /// ⚠️⚠️ AND IT IS NOT BACKFILL. `FUTURE.md` § 11 records backfill as **CUT on 2026-08-30**,
    /// 🧑: *"we dont want bot backfill"*, and it also records that the cut is narrower than it
    /// looks. **A seat that empties DURING a match still gets an `AIController`** so a 1-vs-3 does
    /// not become a 0-vs-3; that is a body nobody is driving being driven, and
    /// `MatchRpc.HostPeerLeft` has always done it. What is cut is padding a MATCH out of the
    /// queue. This file is the third thing, which neither of those covers: a queue that has found
    /// nobody, offering to start anyway.
    ///
    /// ⚠️⚠️ BOTS IN RANKED ARE ALLOWED, AND THAT REVERSES THE LINE THIS PHASE USED TO DRAW.
    /// `FUTURE.md` § 11, 🧑 2026-08-30: *"im okay with bot showing up in rank if theres no ppl bcz
    /// no one plays this game yet anyways"*. The bullet it overrules read *"Never bots in ranked.
    /// Not once, not 'just to fill', not disclosed. That is the line."*, and `FUTURE.md` § 19.11's
    /// PROMPT still says that and still asks for a test asserting it. **The body is newer than the
    /// prompt and the body is the decision**; § 0.6 is the standing instruction to re-verify a
    /// prompt against the section before trusting it, and this is a case where they disagree.
    ///
    /// ⚠️ THE DECISION HAS AN EXPIRY AND THE REASON GIVEN IS THE EXPIRY. *"no one plays this game
    /// yet"* is a claim about the population, so it stops being true the day the queue fills
    /// itself. <see cref="RankedAcceptsBots"/> is one constant and turning it off is one edit.
    /// </summary>
    public static class BotFillRules
    {
        /// <summary>
        /// How many seconds a CASUAL queue waits for people before it offers to start with bots.
        ///
        /// ⚠️ 45 IS QUOTED FROM `FUTURE.md` § 11 RATHER THAN CHOSEN: *"a 45-second queue that ends
        /// in a playable match beats a four-minute queue that ends in nothing"*. It is also one
        /// widening step past `MatchmakingRules.SecondsToWidest` being reached at 60, which means
        /// the search has NOT yet run out of band when the offer appears: the player is told the
        /// game can start now while the real search is still running behind it.
        /// </summary>
        public const float CasualFillAfterSeconds = 45.0f;

        /// <summary>
        /// The same for RANKED, and it is deliberately more than three times as long.
        ///
        /// ⚠️⚠️ A RANKED MATCH AGAINST BOTS IS A REAL RESULT ON A REAL LADDER, so the game tries
        /// much harder to find people first. 150 s is `MatchmakingRules.SecondsToWidest` (60 s to
        /// the widest band) plus a minute and a half of searching AT the widest band, so the offer
        /// only appears after the queue has genuinely looked at everybody in the mode and found
        /// nothing.
        /// </summary>
        public const float RankedFillAfterSeconds = 150.0f;

        /// <summary>
        /// Whether the ranked queue may fill with bots at all. See the class note for the
        /// reversal, the reason and the expiry.
        /// </summary>
        public const bool RankedAcceptsBots = true;

        /// <summary>
        /// How many humans a match needs before its result moves anybody's rating at all.
        ///
        /// ⚠️⚠️ TWO, AND ONE IS THE NUMBER THIS EXISTS TO REFUSE. A single human against three
        /// bots is not a competitive result whatever the bots are tuned to: `AIController` is
        /// seeded and deterministic in its personality roll, so a player who learns one tier's
        /// habits can farm it, and `FUTURE.md` § 11 names that outcome exactly — *"the fastest
        /// climb in the game is queueing at 4 a.m."*. The match still HAPPENS, still pays XP and
        /// still lands in the career; it just does not touch the ladder.
        /// </summary>
        public const int MinHumansForRating = 2;

        /// <summary>
        /// How many seats a match has. ⚠️ Four, everywhere, and it is
        /// `MatchRules.PlayerCount`'s number restated here only so the arithmetic below reads as
        /// arithmetic. Never a second source of truth: <see cref="Weight"/> takes it as an
        /// argument.
        /// </summary>
        public const int Seats = 4;

        /// <summary>The wait threshold for a stake.</summary>
        public static float FillAfterSeconds(QueueStake stake)
            => stake == QueueStake.Ranked ? RankedFillAfterSeconds : CasualFillAfterSeconds;

        /// <summary>
        /// Whether the queue should now be offering to start with bots.
        ///
        /// ⚠️ IT IS AN OFFER AND NEVER AN ACTION, WHICH IS THE WHOLE DESIGN OF THE FEATURE. The
        /// queue does not silently swap people for bots at 45 seconds; the card grows a button
        /// that says how many bots and the player presses it. `FUTURE.md` § 11's constraint is
        /// *"disclosed clearly in the UI"*, and a thing that happens to you is not disclosed by
        /// being written down afterwards.
        /// </summary>
        public static bool OffersFill(QueueStake stake, float secondsQueued, int humans, int seats)
        {
            if (seats <= 0 || humans >= seats) return false;
            if (humans < 1) return false;
            if (stake == QueueStake.Ranked && !RankedAcceptsBots) return false;

            return secondsQueued >= FillAfterSeconds(stake);
        }

        /// <summary>How many bots it would take to start right now.</summary>
        public static int BotsToFill(int humans, int seats)
        {
            if (seats <= 0) return 0;
            int missing = seats - humans;
            return missing < 0 ? 0 : missing;
        }

        /// <summary>
        /// The sentence on the button, which is the disclosure.
        ///
        /// ⚠️⚠️ IT SAYS THE NUMBER AND THE CONSEQUENCE, NOT THE FEATURE. *"Start with 3 bots"*
        /// alone is a fact the player then has to work out the meaning of; the second half is what
        /// they actually want to know, and for a ranked queue it is the only thing that matters.
        /// `FUTURE.md` § 11: *"a player who thinks they beat a person and did not will be angrier
        /// when they find out than they would have been to know"*.
        /// </summary>
        public static string FillOffer(int bots)
        {
            if (bots <= 0) return "";

            return bots == 1 ? "START WITH 1 BOT" : $"START WITH {bots} BOTS";
        }

        /// <summary>The line under the button. See <see cref="FillOffer"/>.</summary>
        public static string FillCaveat(QueueStake stake, int humans, int seats)
        {
            if (RatingCounts(humans, seats))
                return "Bots are labelled in the lobby and on the scoreboard.";

            return stake == QueueStake.Ranked
                ? "Labelled as bots, and this one will not move your rating."
                : "Bots are labelled in the lobby and on the scoreboard.";
        }

        /// <summary>Whether a match with this many humans moves ratings at all.</summary>
        public static bool RatingCounts(int humans, int seats)
            => humans >= MinHumansForRating && seats >= MinHumansForRating;

        /// <summary>
        /// How much of a rating change a result with bots in it is worth, from 0 to 1.
        ///
        /// ⚠️⚠️ `FUTURE.md` § 11 STATES THE REQUIREMENT AND NOT THE NUMBER: *"a result with a bot
        /// in it cannot move a rating the same amount as one without, or the fastest climb in the
        /// game is queueing at 4 a.m. Phase 9 owns the number; what it may not do is pretend the
        /// two are the same match."* This is that number, and it is a straight line rather than a
        /// curve because a straight line can be read off the screen: **every human seat past the
        /// first is a quarter of the result.** Four humans is 1.0, three is 0.667, two is 0.333,
        /// one is 0.0, which is <see cref="MinHumansForRating"/> falling out of the same formula
        /// rather than being a second rule bolted beside it.
        ///
        /// ⚠️ IT SCALES THE WHOLE DELTA, GAINS AND LOSSES ALIKE, AND THAT IS WHAT SATISFIES
        /// § 19.11's *"the humans who stayed take reduced rating loss"*. Scaling only the gain
        /// would make a bot-filled ranked match a pure risk, so nobody would ever accept one, and
        /// the offer would exist to be refused.
        /// </summary>
        public static double Weight(int humans, int seats)
        {
            if (seats <= 1) return 0.0;

            int capped = humans < 0 ? 0 : (humans > seats ? seats : humans);
            double w = (capped - 1.0) / (seats - 1.0);

            return w < 0.0 ? 0.0 : (w > 1.0 ? 1.0 : w);
        }

        /// <summary>
        /// The label a bot wears everywhere it appears.
        ///
        /// ⚠️ ONE STRING, IN THE CORE, BECAUSE IT IS A PROMISE RATHER THAN A CAPTION. `FUTURE.md`
        /// § 11 makes labelling a constraint and names three surfaces (the lobby, the scoreboard
        /// and the match history); three surfaces each with their own literal is three places for
        /// one of them to quietly stop saying it. `Phase11Tests` asserts every surface uses this.
        /// </summary>
        public const string BotTag = "BOT";

        /// <summary>
        /// What the end-of-match board says about a result that did not count.
        ///
        /// ⚠️ IT NAMES THE CAUSE. "Unranked" alone reads as a setting somebody chose; the players
        /// in this match did not choose it, the queue did, and they are owed the reason.
        /// </summary>
        public static string RatingNote(int humans, int seats, bool ranked)
        {
            if (!ranked) return "";

            if (!RatingCounts(humans, seats))
                return "Not enough people in this one, so nobody's rating moved.";

            double w = Weight(humans, seats);
            if (w >= 1.0) return "";

            return $"{humans} of {seats} seats were people, so this counted for "
                   + $"{Math.Round(w * 100.0)} per cent of a rating change.";
        }
    }
}
