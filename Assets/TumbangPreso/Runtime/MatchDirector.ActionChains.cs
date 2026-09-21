using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    public sealed partial class MatchDirector
    {
        private readonly ActionChains _hostChains = new ActionChains();
        private long _chainEpoch, _throwSequence, _tagSequence, _chainEventSequence;
        private double _chainStart;
        public long HostChainEpoch => _chainEpoch;
        public ChainResult LastHostChainResult { get; private set; }
        public int LastHostChainActor { get; private set; } = -1;
        public long LastHostChainEventId { get; private set; }
        public bool LastHostChainIsCatch { get; private set; }
        public int HostAccuracyChainFor(int seat) => _hostChains.AccuracyFor(seat);
        private bool CanRecordHostChain => NetAuthority.ShouldResolve() && MatchInProgress &&
            !IsWarmupBuffer && GameServices.Round != null && GameServices.Round.RoundActive;

        private void ResetHostChains()
        {
            _hostChains.ResetRound(); _chainEpoch++; _throwSequence = _tagSequence = _chainEventSequence = 0;
            _chainStart = Time.timeAsDouble; LastHostChainResult = default; LastHostChainActor = -1;
            LastHostChainEventId = 0; LastHostChainIsCatch = false;
        }
        internal long BeginHostThrowChain(int seat, int canSerial)
        {
            if (!CanRecordHostChain || seat == DefenderSlot) return 0;
            long id = ++_throwSequence;
            return _hostChains.BeginThrow(seat, id, canSerial) ? id : 0;
        }
        internal void FinishHostThrowChain(long epoch, int seat, long id, ThrowChainEnd outcome,
            int canSerial, bool launchCycleConsumed)
        {
            if (!CanRecordHostChain || epoch != _chainEpoch) return;
            var result = _hostChains.ResolveThrow(seat, id, outcome, canSerial, launchCycleConsumed);
            if (!result.Applied) return;
            LastHostChainActor = seat; LastHostChainResult = result;
            LastHostChainEventId = ++_chainEventSequence; LastHostChainIsCatch = false;
            // No score, charge or network event here yet. The C4-gated accepted
            // identity/bonus transport must join MatchDirector.AddScore exactly once.
        }
        internal void RecordHostTagChain(int taya, int victim)
        {
            if (!CanRecordHostChain) return;
            var result = _hostChains.AcceptedTag(taya, victim, ++_tagSequence, Time.timeAsDouble - _chainStart);
            if (!result.Applied) return;
            LastHostChainActor = taya; LastHostChainResult = result;
            LastHostChainEventId = ++_chainEventSequence; LastHostChainIsCatch = true;
        }
    }
}
