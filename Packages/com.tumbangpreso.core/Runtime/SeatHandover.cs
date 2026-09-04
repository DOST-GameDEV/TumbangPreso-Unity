using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// What a seat was, for a whole match, once somebody has left it.
    ///
    /// ⚠️⚠️ THE THIRD STATE IS THE WHOLE POINT AND `MatchRecord.IsBot` CANNOT EXPRESS IT.
    /// `Attention.md` § 16.1, ruling: *"let ai on same skill level as them take over"*, and then
    /// the consequence nobody asked for: **"a seat that was HUMAN and then became a bot part way
    /// through is neither, and the career line for that match currently has no way to say so."**
    /// A boolean forces the record to lie in one of two directions: call it human and a bot's
    /// stretch is credited to a person, call it a bot and the half they actually played
    /// disappears.
    /// </summary>
    public enum SeatOrigin
    {
        /// <summary>A person sat here for the whole match.</summary>
        Human = 0,

        /// <summary>Nobody ever sat here. `BotFill` filled it before the match started.</summary>
        Bot = 1,

        /// <summary>A person sat here, left, and a bot finished the match in their chair.</summary>
        HandedToBot = 2,
    }

    /// <summary>
    /// The seat handover rules: which bot takes an absent player's chair, and what the ladder is
    /// allowed to conclude from a match in which that happened.
    ///
    /// ⚠️⚠️ THIS IS THE HALF OF `Attention.md` § 16.1 THAT WAS RULED AND NOT BUILT. The seat
    /// FILLING half already existed: `BotFillRules` drives an unfilled seat and
    /// `AIController.ApplyDifficulty` sets a tier. What did not exist is the MATCHING, *"because
    /// the game has no notion of 'this player's skill level' to hand the bot, and `Rating` is a
    /// ladder number rather than a difficulty tier."* <see cref="TierFor"/> is that missing step
    /// and it is three lines, which is the right size for it.
    ///
    /// ⚠️⚠️ AND THE SECOND HALF IS THE LADDER, WHICH IS THE PART THAT COULD DO REAL DAMAGE.
    /// § 16.1 states it plainly: *"a bot can lose you points you would not have lost, or win you
    /// points you did not earn"*, and **"a rating that counts a bot's stretch as the player's own
    /// is a ladder nobody trusts."** `BotFillRules.Weight` already scales a result by how many seats
    /// were people; all this has to do is stop a handed-over seat counting as one of them.
    ///
    /// ⚠️ ENGINE-FREE, LIKE EVERYTHING ELSE THAT DECIDES SOMETHING. `CLAUDE.md` § 4: the rules
    /// core never references `UnityEngine`, which is what lets these be asserted in forty
    /// milliseconds instead of played for an afternoon. <see cref="Difficulty"/> is already a
    /// core enum, so the tier answer needs nothing from the engine either.
    /// </summary>
    public static class SeatHandover
    {
        /// <summary>
        /// How far from the middle of the ladder a player has to be before a different bot tier
        /// is the honest answer.
        ///
        /// ⚠️⚠️ IT IS `MatchmakingRules.MaxHalfWidth`, NOT A NUMBER PICKED FOR THIS. That
        /// constant already carries the argument, in its own words: 500 is *"where banding stops
        /// meaning anything ... a queue that has widened this far has already said 'skill
        /// matching has failed'"*. **The distance at which the game refuses to call two players
        /// comparable is exactly the distance at which it should stop handing their seats the
        /// same bot.** Inventing a second threshold here would be a number that can disagree with
        /// that one, which is `docs/TODO.md` § 88.1a's fault.
        ///
        /// ⚠️ SO THE BANDS ARE 1000 AND 2000 AROUND A 1500 START, AND NEITHER IS TYPED HERE.
        /// Change `StartRating` or `MaxHalfWidth` and these follow.
        /// </summary>
        public static int BataCeiling =>
            (int)Math.Round(RatingRules.StartRating) - MatchmakingRules.MaxHalfWidth;

        /// <summary>See <see cref="BataCeiling"/>.</summary>
        public static int AstigFloor =>
            (int)Math.Round(RatingRules.StartRating) + MatchmakingRules.MaxHalfWidth;

        /// <summary>
        /// The bot tier that takes a seat from a player at this rating.
        ///
        /// ⚠️⚠️ THREE TIERS, BECAUSE THERE ARE THREE TIERS. `AiTuning.Tiers` holds Bata, Normal
        /// and Astig and nothing else, so a mapping with more resolution than that would be
        /// arithmetic nobody can act on. The ruling is *"same skill level"*, and the finest
        /// answer this game can actually give is one of three.
        ///
        /// ⚠️ THE MIDDLE BAND IS THE WIDE ONE ON PURPOSE. Most of a ladder sits inside one queue
        /// widening of the start, and Normal is what those players' seats have always been filled
        /// with. This changes the answer for the ends, which is where the current single global
        /// tier is most obviously wrong: an Astig player's seat handed to a Bata bot loses their
        /// side the match, and a Bata player's seat handed to an Astig bot wins one they were
        /// losing.
        /// </summary>
        public static Difficulty TierFor(int rating)
        {
            if (rating < BataCeiling) return Difficulty.Bata;
            if (rating >= AstigFloor) return Difficulty.Astig;
            return Difficulty.Normal;
        }

        /// <summary>
        /// Whether this seat may move the person who sat in it up or down the ladder.
        ///
        /// ⚠️⚠️ A HANDED-OVER SEAT MAY NOT, AND THAT IS THE ANSWER § 16.1 ASKS FOR RATHER THAN
        /// A CAUTIOUS CHOICE. The alternative is to credit the human fraction, which needs the
        /// match to record WHEN they left and the rating maths to be re-derived against a partial
        /// result: that is a bigger change than the feature, and it would still be arguable in
        /// every direction afterwards. **The result is simply not theirs**, and saying so is
        /// something a player can accept; a number they cannot audit is not.
        ///
        /// ⚠️ THIS IS `BotFillRules.MinHumansForRating`'S ARGUMENT ONE STEP ON. That constant already
        /// refuses to move a ladder on a result one person could farm; this refuses to move one
        /// on a result the person was not present for.
        /// </summary>
        public static bool RatingMovesFor(SeatOrigin origin) => origin == SeatOrigin.Human;

        /// <summary>
        /// How many of these seats count as people for <see cref="BotFillRules.Weight"/>.
        ///
        /// ⚠️⚠️ A HANDED-OVER SEAT COUNTS AS A BOT SEAT FOR EVERYBODY ELSE'S RATING, WHICH IS THE
        /// HALF THAT IS EASY TO FORGET. § 16.1's sentence is about the person who left, and the
        /// three who stayed have the same problem from the other side: they finished the match
        /// against an AI and `Weight` would still be paying them for a four-human result.
        /// `BotFillRules.Weight`'s own note says it scales *"gains and losses alike"* so accepting a
        /// bot-filled match is not a pure risk, and this is the same fairness applied to a match
        /// that became bot-filled after it started.
        /// </summary>
        public static int HumanSeats(IReadOnlyList<SeatOrigin> seats)
        {
            if (seats == null) return 0;

            int humans = 0;
            for (int i = 0; i < seats.Count; i++)
            {
                if (seats[i] == SeatOrigin.Human) humans++;
            }

            return humans;
        }

        /// <summary>
        /// The one question a caller has to ask: how much of this result may move MY rating.
        ///
        /// ⚠️⚠️ ONE FUNCTION BECAUSE IT IS ONE RULE, AND SPLITTING IT IS HOW HALF OF IT GETS
        /// APPLIED. Both halves of § 16.1's ladder problem are here: my own seat being handed
        /// over zeroes it, and somebody ELSE's seat being handed over reduces it, because the
        /// match I finished had fewer people in it than the one I started. A caller that
        /// remembered `RatingMovesFor` and forgot `HumanSeats` would pay three players a full
        /// four-human result for a match they finished against an AI, and nothing would say so.
        ///
        /// ⚠️ IT FEEDS `RatingRules.Blend`, whose own note is why the answer has to be a WEIGHT
        /// rather than a boolean: the deviation and the volatility are scaled by it too, and
        /// *"farming bots at a third of the rating gain while collecting a full match of
        /// confidence would let somebody lock in a soft rating and then defend it against
        /// people"*.
        /// </summary>
        public static double RatingWeightFor(SeatOrigin mine, IReadOnlyList<SeatOrigin> seats)
        {
            if (!RatingMovesFor(mine)) return 0.0;
            if (seats == null || seats.Count == 0) return 0.0;

            return BotFillRules.Weight(HumanSeats(seats), seats.Count);
        }

        /// <summary>
        /// What the end-of-match board says when somebody's chair finished the match without
        /// them.
        ///
        /// ⚠️ IT NAMES THE CAUSE, LIKE `BotFillRules.RatingNote`. A player who sees "this did not
        /// count" with no reason concludes the game is broken; one who is told a seat was handed
        /// over concludes somebody's wifi died, which is what happened.
        ///
        /// ⚠️ EMPTY WHEN NOTHING HAPPENED, so a caller can print it unconditionally.
        /// </summary>
        public static string HandoverNote(int handedOver)
        {
            if (handedOver <= 0) return "";

            return handedOver == 1
                ? "A player left and a bot finished in their seat, so that seat's rating did not move."
                : $"{handedOver} players left and bots finished in their seats, so those seats' "
                  + "ratings did not move.";
        }

        /// <summary>
        /// The label the scoreboard and the lobby wear for a handed-over seat.
        ///
        /// ⚠️⚠️ IT IS NOT `BotFillRules.BotTag`, AND THE DIFFERENCE IS THE WHOLE REASON THIS ENUM
        /// EXISTS. `FUTURE.md` § 11 makes labelling a constraint because *"a player who thinks
        /// they beat a person and did not will be angrier when they find out than they would have
        /// been to know"*. A seat that was a person for two rounds and a bot for two is a third
        /// thing, and calling it BOT erases the person while calling it nothing erases the bot.
        /// </summary>
        public const string HandoverTag = "LEFT · BOT";

        /// <summary>The tag for a seat, or empty for one that needs no explaining.</summary>
        public static string TagFor(SeatOrigin origin)
        {
            switch (origin)
            {
                case SeatOrigin.Bot: return BotFillRules.BotTag;
                case SeatOrigin.HandedToBot: return HandoverTag;
                default: return "";
            }
        }
    }
}
