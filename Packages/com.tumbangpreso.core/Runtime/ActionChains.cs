using System;

namespace TumbangPreso.Core
{
    public enum ThrowChainEnd { Hit, Block, Miss, NoContest }
    public enum ChainMilestone { None, AccurateThree, AccurateFive, DoubleCatch, TripleCatch, SingleCatch, MultiCatch }

    // A result describes one accepted resolution. The match score authority must
    // explicitly consume Bonus; reading this value cannot award points or charge.
    public readonly struct ChainResult
    {
        public readonly bool Applied;
        public readonly int Count, Bonus;
        public readonly ChainMilestone Milestone;
        public ChainResult(int count, int bonus, ChainMilestone milestone = ChainMilestone.None)
        { Applied = true; Count = count; Bonus = bonus; Milestone = milestone; }
    }

    /// <summary>
    /// Deterministic per-round chain bookkeeping. Call only from accepted host
    /// outcomes, with monotonic action IDs and can cycles. No Unity, transport,
    /// score callbacks or clock reads. Integration must supply active-play time.
    /// </summary>
    public sealed class ActionChains
    {
        public const double CatchWindow = 5;
        private sealed class Seat
        {
            public long LastThrow, PendingThrow, LastTag;
            public int Accuracy, LaunchCycle, CatchCount;
            public double LastCatch;
        }
        private readonly Seat[] _seats = new Seat[Balance.PlayerCount];
        private int _lastHitCycle = -1, _lastKnockdownCycle = -1;
        private readonly int[] _knockdowns = new int[Balance.PlayerCount];
        public ActionChains() { ResetRound(); }
        public void ResetRound()
        {
            for (int i = 0; i < _seats.Length; i++) _seats[i] = new Seat();
            _lastHitCycle = _lastKnockdownCycle = -1;
            Array.Clear(_knockdowns, 0, _knockdowns.Length);
        }
        private bool Valid(int seat) => seat >= 0 && seat < _seats.Length;
        public int AccuracyFor(int seat) => Valid(seat) ? _seats[seat].Accuracy : 0;

        public bool BeginThrow(int seat, long actionId, int canCycle)
        {
            if (!Valid(seat) || actionId <= 0 || canCycle < 0) return false;
            var s = _seats[seat];
            if (actionId <= s.LastThrow || s.PendingThrow != 0) return false;
            s.LastThrow = s.PendingThrow = actionId; s.LaunchCycle = canCycle;
            return true;
        }

        public ChainResult ResolveThrow(int seat, long actionId, ThrowChainEnd outcome, int canCycle,
            bool launchCycleConsumed = false)
        {
            if (!Valid(seat) || actionId <= 0 || canCycle < 0) return default;
            var s = _seats[seat];
            if (s.PendingThrow != actionId || canCycle < s.LaunchCycle || !Enum.IsDefined(typeof(ThrowChainEnd), outcome)) return default;
            // A live later hit takes precedence over a consumed launch cycle. A
            // caller must not resolve NoContest until the flight can no longer hit.
            if (outcome == ThrowChainEnd.Hit && canCycle <= _lastHitCycle) return default;
            if (outcome == ThrowChainEnd.NoContest && !launchCycleConsumed) return default;
            s.PendingThrow = 0;
            if (outcome == ThrowChainEnd.Hit)
            {
                _lastHitCycle = canCycle;
                s.Accuracy++;
                // Accuracy remains a statistic. The current paid streak is accepted
                // knockdowns since tag/round, not the retired accuracy reward schedule.
                return new ChainResult(s.Accuracy, 0);
            }
            if (outcome == ThrowChainEnd.Block || (outcome == ThrowChainEnd.Miss && !launchCycleConsumed))
                s.Accuracy = 0;
            return new ChainResult(s.Accuracy, 0);
        }

        public void AttackerTagged(int seat)
        {
            if (!Valid(seat)) return;
            _seats[seat].Accuracy = 0;
            _knockdowns[seat] = 0;
            // A pre-tag flight cannot later rebuild this player's broken sequence.
            _seats[seat].PendingThrow = 0;
        }

        public void ResetCatchChains()
        {
            foreach (var seat in _seats) seat.CatchCount = 0;
        }

        public ChainResult AcceptedKnockdown(int seat, int canCycle)
        {
            if (!Valid(seat) || canCycle < 0 || canCycle <= _lastKnockdownCycle) return default;
            _lastKnockdownCycle = canCycle;
            ResetCatchChains();
            int count = ++_knockdowns[seat];
            return new ChainResult(count, count >= 3 ? 50 : 0);
        }

        public ChainResult AcceptedTag(int taya, int victim, long eventId, double activeSeconds)
        {
            if (!Valid(taya) || !Valid(victim) || taya == victim || eventId <= 0 ||
                double.IsNaN(activeSeconds) || double.IsInfinity(activeSeconds) || activeSeconds < 0) return default;
            var s = _seats[taya];
            if (eventId <= s.LastTag || activeSeconds < s.LastCatch) return default;
            s.LastTag = eventId;
            AttackerTagged(victim);
            if (s.CatchCount == 0 || activeSeconds - s.LastCatch > CatchWindow) s.CatchCount = 0;
            s.LastCatch = activeSeconds;
            int count = ++s.CatchCount;
            return new ChainResult(count, count >= 2 ? 25 : 0,
                count == 1 ? ChainMilestone.SingleCatch : count == 2 ? ChainMilestone.DoubleCatch :
                count == 3 ? ChainMilestone.TripleCatch : ChainMilestone.MultiCatch);
        }
    }
}
