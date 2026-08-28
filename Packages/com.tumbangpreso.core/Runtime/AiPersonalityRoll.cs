namespace TumbangPreso.Core
{
    /// <summary>
    /// The per-bot jitter on top of a tier, from `ai_controller.gd`'s `_Personality`.
    ///
    /// ⚠️⚠️ SEEDED FROM THE SEAT, NOT FROM THE CLOCK. Two runs of the same match give the
    /// same four characters, which is what makes a fairness number reproducible and a bug
    /// re-findable. The variation is between BOTS, not between RUNS. Seeding this from the
    /// system clock would quietly destroy every measurement the balance rests on.
    ///
    /// ⚠️ NOBODY IS EXACTLY THE TIER. Three bots on Normal that behave identically read as
    /// one bot copied three times; the point of this roll is that they do not.
    /// </summary>
    public sealed class AiPersonalityRoll
    {
        /// <summary>0.85..1.20 on the think interval — some players deliberate, some snap.</summary>
        public readonly float Tempo;

        /// <summary>0.80..1.25 on aim scatter.</summary>
        public readonly float Hands;

        /// <summary>0.85..1.15 on reaction.</summary>
        public readonly float Nerves;

        /// <summary>0.75..1.30 on how far it will push its luck fetching and shoving.</summary>
        public readonly float NerveForTheBox;

        /// <summary>Radians. Its favourite corner of the ring to work from — which is what
        /// stops three attackers converging on one bearing without any of them coordinating.</summary>
        public readonly float HomeBearing;

        /// <summary>
        /// Seconds of pause before committing to a NEW plan.
        ///
        /// ⚠️ A NEW PLAN COSTS A BEAT, AND THIS IS WHY BOTS DO NOT READ AS MACHINES. Without
        /// it a bot flips plan on the frame the world changes, which is faster than a human
        /// can move a hand and is the single most machine-like thing a bot does. Per-bot, so
        /// the three of them do not even hesitate together.
        /// </summary>
        public readonly float Hesitation;

        /// <summary>
        /// 0..1. How long after a round goes live this particular bot waits before it will spend
        /// a power at all, as a fraction of `AiTuning.AbilityOpeningJitterSeconds`.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE A SHARED OPENING DELAY DOES NOT STAGGER ANYTHING, IT ONLY MOVES
        /// THE PILE-UP. 🧑 2026-08-27: *"try to make it so that AI think or pretend to think when
        /// to use skills bcz they all js spam it at the same time bru at thhe start"*. There WAS
        /// already an opening delay (`AbilityOpeningDelaySeconds`, 2.5 s) and a per-bot cadence,
        /// and the report is still exactly right: one constant shared by four seats means all
        /// four gates open on the same frame, so the frame-one dump became a frame-150 dump.
        ///
        /// ⚠️ SEEDED FROM THE SEAT LIKE EVERY OTHER FIELD HERE, so the stagger is reproducible
        /// and `BotBehaviourProbe` still measures the same match twice. A `Random` here would put
        /// run-to-run noise into the one thing the probe's numbers are compared across.
        ///
        /// ⚠️ APPENDED AT THE END OF THE ROLL, ON PURPOSE. `Range` advances one xorshift stream in
        /// declaration order, so inserting a field ABOVE any existing one silently re-rolls every
        /// personality in the game and invalidates every measurement taken against them.
        /// </summary>
        public readonly float Patience;

        /// <summary>
        /// 0..1 per slot, in `HeroAbilitySystem.Slot` order: skill 1, skill 2, ultimate. How
        /// EAGER this particular bot is to reach for that key.
        ///
        /// ⚠️⚠️ 🧑 2026-08-27: *"i want it to be possible too for them to not use some skills at
        /// all if they cant find opportunity bcz thats normal and human"*. Four bots that all
        /// weigh the same power for the same length of time and take every chance it offers are
        /// four copies of one player. A real four-player lobby has somebody who never remembers
        /// they have an ultimate and somebody who spams their dash.
        ///
        /// ⚠️ IT LENGTHENS THE CONVICTION WINDOW, IT DOES NOT ADD A DICE ROLL. A random refusal
        /// is a bot ignoring a chance it saw, which reads as broken. A long window is a bot that
        /// wants to be surer, so a marginal opportunity passes before it commits and a clear one
        /// still gets taken. Whether a slot goes unused for a whole round is then decided by the
        /// BOARD, which is what makes it read as a person rather than as a coin.
        ///
        /// ⚠️ AND IT IS SEAT-SEEDED LIKE EVERYTHING ELSE HERE, so "seat 2 hardly ever ults" is a
        /// reproducible fact about a match rather than run-to-run noise the probe would inherit.
        /// </summary>
        public readonly float[] SkillAppetite = new float[3];

        /// <summary>
        /// 0..1. How much this bot plays to the crowd: taunts, celebrations, idle emotes.
        ///
        /// ⚠️⚠️ A CELEBRATION IS A SELF-INFLICTED STUN, SO THE APPETITE FOR ONE IS A REAL
        /// PERSONALITY AXIS AND NOT DECORATION. `EmotePlayer` refuses to play while a body
        /// cannot act and cancels on the first movement key, so a bot that stops to dance has
        /// genuinely stopped: it is out of position for as long as the clip runs. A lobby has
        /// somebody who bows after every knockdown and somebody who never presses B once, and
        /// the difference between them is a difference in what they are willing to pay for it.
        ///
        /// ⚠️ SEAT-SEEDED LIKE EVERY OTHER FIELD HERE, so "seat 3 is the show-off" is a
        /// reproducible fact about a match rather than run-to-run noise the probe would inherit.
        /// </summary>
        public readonly float Showmanship;

        /// <summary>
        /// 0..1. How often this bot's feet leave the ground for no tactical reason at all.
        ///
        /// ⚠️⚠️ IT IS THE MOST HUMAN THING IN THIS FILE AND IT BUYS NOTHING. Jumping costs no
        /// stamina and confers no advantage in this game, which is exactly why a person does it
        /// while they wait: it is fidgeting with the one verb that is free. Bots walked at one
        /// height for the entire port, and a body that never leaves the floor is the tell that
        /// survives every other fix.
        ///
        /// ⚠️ THE ROLL IS THE HABIT, NOT THE PERMISSION. `AiTuning.HopIntervalMin` and the tier's
        /// `Hops` decide how often the habit gets a chance; this decides whether this particular
        /// bot has it at all. At the bottom of the range a seat essentially never hops.
        /// </summary>
        public readonly float Springiness;

        /// <summary>
        /// 0..1. How hard this bot is to distract. The inverse of how often its attention lapses.
        ///
        /// ⚠️⚠️ 🧑 2026-08-28: *"let it make mistakes bcz humans do mistakes sometimes"*. The tier's
        /// `Mistake` was the only error model in the game and it reached exactly one decision:
        /// the throw wind-up (`_blundering`). Nothing else a bot did could ever go wrong, so a
        /// bot that was not currently charging a shot was playing perfectly.
        ///
        /// ⚠️⚠️ AND A LAPSE IS NOT A WRONG ANSWER, IT IS A LATE ONE. Deliberately choosing the
        /// second-best plan reads as a broken bot, because the mistake is visible in the choice.
        /// Attention that wanders for a beat is invisible in the choice and visible only in the
        /// timing, which is what being outplayed by a person actually looks like. See
        /// `AIController` § ATTENTION WANDERS.
        /// </summary>
        public readonly float Focus;

        /// <summary>
        /// 0..1, mapped onto the seats at read time. Which rival this bot takes personally.
        ///
        /// ⚠️⚠️ § 33.1 KILLED THE SEAT-ORDER SELECTOR AND FOUR IDENTICAL SCORERS STILL AGREE.
        /// The chase is a score now rather than a `foreach` with a `return` in it, so nobody is
        /// singled out by construction. But all four seats weigh the board with the same four
        /// weights, so whoever the board favours is favoured by every bot at once, and a person
        /// who plays differently from three bots (deeper into the chalk, holding a tsinelas
        /// longer) is different in the terms the score reads. 🧑 2026-08-28: *"I dont want the
        /// bots to only go after the human too (sometimes it only targets human)"*.
        ///
        /// ⚠️ A SMALL BIAS, NOT A TARGET. It is worth `AiTuning.TagRivalryWeight`, which is under
        /// the helpless bonus and under a metre and a half of distance: it decides ties and near
        /// ties and never overrules a tag that is actually available. Four bots that break ties
        /// differently is the whole of the fix.
        /// </summary>
        public readonly float RivalPick;

        public AiPersonalityRoll(int seatSeed)
        {
            // ⚠️ A DETERMINISTIC HASH, NOT string.GetHashCode(). .NET's string hash is
            // randomised per process by default, so the same seat would roll a different
            // personality every launch — exactly the run-to-run variation this must not have.
            uint state = Fnv1a($"tumbang-preso-bot-{seatSeed}");

            Tempo = Range(ref state, 0.85f, 1.20f);
            Hands = Range(ref state, 0.80f, 1.25f);
            Nerves = Range(ref state, 0.85f, 1.15f);
            NerveForTheBox = Range(ref state, 0.75f, 1.30f);
            HomeBearing = Range(ref state, -3.14159265f, 3.14159265f);
            Hesitation = Range(ref state, 0.05f, 0.28f);

            // ⚠️ LAST. See the fields' notes: the stream is positional, so anything appended has
            // to stay at the bottom or every personality above it re-rolls.
            Patience = Range(ref state, 0.0f, 1.0f);

            SkillAppetite[0] = Range(ref state, 0.0f, 1.0f);
            SkillAppetite[1] = Range(ref state, 0.0f, 1.0f);
            SkillAppetite[2] = Range(ref state, 0.0f, 1.0f);

            // ⚠️⚠️ APPENDED BELOW EVERYTHING, FOR THE REASON `Patience` RECORDS ONE FIELD UP.
            // `Range` advances one xorshift stream in declaration order, so a field inserted
            // ABOVE any existing one silently re-rolls every personality in the game and
            // invalidates every measurement ever taken against them. The four below are the
            // 2026-08-28 humanising pass and they sit at the bottom for that reason alone.
            Showmanship = Range(ref state, 0.0f, 1.0f);
            Springiness = Range(ref state, 0.0f, 1.0f);
            Focus = Range(ref state, 0.0f, 1.0f);
            RivalPick = Range(ref state, 0.0f, 1.0f);
        }

        /// <summary>Eagerness for one slot, safe against an out-of-range index.</summary>
        public float AppetiteFor(int slot)
            => slot >= 0 && slot < SkillAppetite.Length ? SkillAppetite[slot] : 0.5f;

        /// <summary>
        /// Which of <paramref name="candidateCount"/> rivals this bot takes personally, or -1
        /// when there are none.
        ///
        /// ⚠️ THE MAPPING LIVES HERE RATHER THAN AT THE CALL SITE so the seat that gets picked
        /// is a property of the personality and not of whatever order the caller happened to
        /// build its list in. The caller owns the list; this owns the choice.
        /// </summary>
        public int RivalIndex(int candidateCount)
        {
            if (candidateCount <= 0) return -1;

            int index = (int)(RivalPick * candidateCount);
            return index >= candidateCount ? candidateCount - 1 : index;
        }

        private static uint Fnv1a(string s)
        {
            uint hash = 2166136261u;

            foreach (char c in s)
            {
                hash ^= c;
                hash *= 16777619u;
            }

            return hash == 0u ? 1u : hash;
        }

        /// <summary>xorshift32, advanced in place — one stream per bot, in a fixed order.</summary>
        private static float Range(ref uint state, float min, float max)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;

            return min + (state / (float)uint.MaxValue) * (max - min);
        }
    }
}
