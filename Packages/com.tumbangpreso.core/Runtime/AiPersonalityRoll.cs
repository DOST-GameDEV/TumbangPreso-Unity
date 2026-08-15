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
