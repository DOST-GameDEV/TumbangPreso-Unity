namespace TumbangPreso.Core
{
    /// <summary>The two distinct match play modes.</summary>
    public enum GameMode
    {
        /// <summary>Classic Tumbang Preso with traditional 12-character Kenney street roster.</summary>
        Classic,

        /// <summary>Action-packed Hero Strike mode with the 5 superpowered heroes and abilities.</summary>
        HeroStrike
    }

    /// <summary>The four ways a point can be created. There are no others.</summary>
    public enum ScoreEvent
    {
        /// <summary>Knock the lata down. To the thrower.</summary>
        LataKnocked,

        /// <summary>Tag a vulnerable Attacker. To the Defender.</summary>
        Tag,

        /// <summary>Shove an Attacker who is tagged within SabotageWindow. To the shover.</summary>
        Sabotage,

        /// <summary>Per second the lata is upright. To the Defender.</summary>
        DefenseTick,

        /// <summary>Penalty for Taya camping inside can ring without engaging.</summary>
        TayaCampPenalty,

        /// <summary>Penalty for Attacker failing to retrieve loose slipper.</summary>
        UnretrievedSlipperPenalty,
    }

    /// <summary>
    /// Match structure and scoring.
    ///
    /// ⚠️⚠️ ROLE IS DERIVED, NEVER ACCUMULATED, AND THAT IS THE WHOLE FAIRNESS ARGUMENT.
    /// DefenderSlotFor is a pure function of the round number. A schedule expressed as a
    /// mutating counter has no way to state the invariant it is supposed to keep, and it
    /// desyncs the moment one peer misses one call. "Everyone defends exactly once,
    /// clockwise" is true here BY CONSTRUCTION. The 2v2 format this replaced needed a
    /// whole paired-set system to reach the same property; four players and four rounds
    /// get it for free.
    /// </summary>
    public static class MatchRules
    {
        /// <summary>
        /// Which slot is the taya in a given round. Rounds are 1-based.
        ///
        /// | round | 1 | 2 | 3 | 4 |
        /// | taya  | 0 | 1 | 2 | 3 |
        /// </summary>
        public static int DefenderSlotFor(int round)
        {
            int r = round < 1 ? 1 : round;
            return (r - 1) % Balance.PlayerCount;
        }

        public static bool IsDefenderSlot(int slot, int round) => slot == DefenderSlotFor(round);

        public static int PointsFor(ScoreEvent e)
        {
            switch (e)
            {
                case ScoreEvent.LataKnocked: return Balance.ScoreLataKnocked;
                case ScoreEvent.Tag: return Balance.ScoreTag;
                case ScoreEvent.Sabotage: return Balance.ScoreSabotage;
                case ScoreEvent.DefenseTick: return Balance.ScoreDefensePerTick;
                case ScoreEvent.TayaCampPenalty: return Balance.ScoreTayaCampPenalty;
                case ScoreEvent.UnretrievedSlipperPenalty: return Balance.ScoreUnretrievedPenalty;
                default: return 0;
            }
        }
    }

    /// <summary>
    /// Cumulative per-player score across the whole match.
    ///
    /// ⚠️ EVERY POINT IN THE GAME IS AWARDED THROUGH Add(), host-side. The predecessor
    /// spread its win conditions across four files and the recurring bug class was a rule
    /// that fired on the wrong peer. A point that can only be created in one function
    /// cannot be created on a client at all. Keep it that way through the netcode port:
    /// this type must never be mutated from a client path.
    ///
    /// ⚠️ THERE IS NO PER-ROUND WINNER. Scores persist across the round boundary; only
    /// the taya role rotates.
    /// </summary>
    public sealed class Scoreboard
    {
        private readonly int[] _scores = new int[Balance.PlayerCount];

        public int this[int slot] => (slot < 0 || slot >= _scores.Length) ? 0 : _scores[slot];

        public void Add(int slot, ScoreEvent e)
        {
            if (slot < 0 || slot >= _scores.Length) return;
            _scores[slot] = System.Math.Max(0, _scores[slot] + MatchRules.PointsFor(e));
        }

        public void Set(int slot, int score)
        {
            if (slot < 0 || slot >= _scores.Length) return;
            _scores[slot] = score;
        }

        public void SetAll(int[] scores)
        {
            if (scores == null) return;
            for (int i = 0; i < _scores.Length && i < scores.Length; i++)
                _scores[i] = scores[i];
        }

        public void Reset()
        {
            for (int i = 0; i < _scores.Length; i++) _scores[i] = 0;
        }

        public int Total
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < _scores.Length; i++) sum += _scores[i];
                return sum;
            }
        }

        /// <summary>
        /// The winning slot, or -1.
        ///
        /// ⚠️ A TIE AT THE TOP RETURNS -1 AND IS AN HONEST DRAW. It is not broken by seat
        /// order, which would hand round 1's taya a structural advantage in a game whose
        /// entire fairness argument is that the seats are symmetric.
        /// </summary>
        public int WinningSlot()
        {
            int best = -1;
            int bestScore = int.MinValue;
            bool tied = false;

            for (int i = 0; i < _scores.Length; i++)
            {
                if (_scores[i] > bestScore)
                {
                    bestScore = _scores[i];
                    best = i;
                    tied = false;
                }
                else if (_scores[i] == bestScore)
                {
                    tied = true;
                }
            }

            return tied ? -1 : best;
        }
    }
}
