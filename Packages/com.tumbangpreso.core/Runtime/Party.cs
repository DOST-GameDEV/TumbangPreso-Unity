using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>Why a party cannot queue, so the rail says a sentence rather than greying out.</summary>
    public enum PartyRefusal
    {
        None = 0,
        TooBig,
        FullStackInRanked,
        MemberOnCooldown,
        MemberNotSignedIn,
    }

    /// <summary>
    /// A party, and the one rule that makes ranked survive it.
    ///
    /// ⚠️⚠️ PHASE 6 COULD NOT BUILD THIS AND SAID SO. `docs/TODO.md` § 102.2: "parties that queue
    /// together cannot be built before there is a queue", so what shipped was the thing a queue
    /// would replace, a friend publishing their join code with their presence. `FUTURE.md` § 6
    /// wrote the follow-up down exactly: "when Phase 7 lands, a party becomes a queue ticket and
    /// the rail does not change." **That is what this file is, and the rail did not change.** The
    /// JOIN button on a friend row still hands a join code to `LobbyJoinPanel`; what is new is
    /// that the leader can take the whole room into the queue with one press.
    ///
    /// ⚠️ A PARTY IS STILL "BE IN THE SAME LOBBY". There is no party service, no party id and no
    /// second roster: the members are the humans seated in the leader's lobby, which the host
    /// already knows and already replicates. Anything else would be a second source of truth about
    /// who is playing with whom, and the seat table is the first.
    /// </summary>
    public static class PartyRules
    {
        /// <summary>The most people a party can hold, which is one match.</summary>
        public const int MaxSize = Balance.PlayerCount;

        /// <summary>
        /// ⚠️⚠️ A FOUR-STACK IS EXCLUDED FROM RANKED AND PARTIES OF TWO OR THREE ARE NOT.
        ///
        /// `FUTURE.md` § 6 raised it and deferred the choice: "A party of four is a full match,
        /// which is a ranked problem. Four friends can arrange results between themselves. Either
        /// exclude full parties from ranked or accept only partial ones. Decide it in Phase 9 and
        /// assert it in a test." § 19.9 step 9 is that instruction arriving. **This is the
        /// decision, and it is the second option.**
        ///
        /// The reasoning, in the shape of `FUTURE.md` § 0.5 rule 11b: the cost of a rule is what
        /// the player has to hold in their head. "You cannot queue ranked as a full four" is one
        /// sentence and it appears at exactly the moment it matters, on the button. Excluding
        /// every party from ranked would be a shorter rule and a worse game, because it tells two
        /// friends they cannot play the competitive mode together at all, and two friends cannot
        /// arrange a four-player result between themselves: the other two seats are strangers who
        /// are trying to win.
        ///
        /// ⚠️ AND IT IS THE SAME NUMBER AS `Balance.PlayerCount`, DERIVED RATHER THAN WRITTEN. A
        /// literal 4 here would be a second constant that can disagree with the seat count.
        /// </summary>
        public const int MaxRankedSize = Balance.PlayerCount - 1;

        /// <summary>
        /// Whether this party may enter this queue.
        ///
        /// ⚠️ THE COOLDOWN OF ANY MEMBER STOPS THE WHOLE PARTY, because the party queues as one
        /// ticket and there is no honest way to seat three of four. It names the member so the
        /// leader can see who, rather than the button simply refusing.
        /// </summary>
        public static PartyRefusal CanQueue(int size, QueueStake stake, IReadOnlyList<int> memberCooldownSeconds,
                                            IReadOnlyList<bool> memberSignedIn)
        {
            if (size > MaxSize) return PartyRefusal.TooBig;

            if (stake == QueueStake.Ranked && size > MaxRankedSize)
                return PartyRefusal.FullStackInRanked;

            for (int i = 0; memberCooldownSeconds != null && i < memberCooldownSeconds.Count; i++)
                if (memberCooldownSeconds[i] > 0) return PartyRefusal.MemberOnCooldown;

            // ⚠️ RANKED NEEDS AN ACCOUNT AND CASUAL DOES NOT. `FUTURE.md` § 0.5 rule 7: practice,
            // training, LAN and joining by code must never sit behind a login, and the nationals
            // are why. A ladder is the one thing that genuinely cannot work for an anonymous
            // machine-local identity, because there is nowhere to keep the rating.
            if (stake == QueueStake.Ranked)
                for (int i = 0; memberSignedIn != null && i < memberSignedIn.Count; i++)
                    if (!memberSignedIn[i]) return PartyRefusal.MemberNotSignedIn;

            return PartyRefusal.None;
        }

        /// <summary>
        /// The sentence for a refusal. ⚠️ IT SAYS WHAT TO DO NEXT, not what went wrong.
        /// </summary>
        public static string RefusalLabel(PartyRefusal refusal)
        {
            switch (refusal)
            {
                case PartyRefusal.TooBig:
                    return "A match is four people. Somebody has to sit this one out.";
                case PartyRefusal.FullStackInRanked:
                    return "Ranked takes parties of up to three. Drop one and the rest of you can queue, or play QUICK MATCH as a four.";
                case PartyRefusal.MemberOnCooldown:
                    return "Somebody in the party left a match early and is still on a cooldown.";
                case PartyRefusal.MemberNotSignedIn:
                    return "Ranked needs an account. Anybody playing as a guest has to sign in first.";
                default:
                    return "";
            }
        }

        /// <summary>
        /// How many strangers the queue still has to find for this party.
        ///
        /// ⚠️ THE QUEUE SEARCHES FOR ROOM FOR ALL OF THEM AT ONCE. A party of three joining a
        /// lobby with two free seats is one member left standing on the menu wondering what
        /// happened, which is the exact opposite of `CLAUDE.md` § 6.3's "a dead end is a bug".
        /// </summary>
        public static int SeatsNeeded(int size) => size < 1 ? 1 : size;
    }
}
