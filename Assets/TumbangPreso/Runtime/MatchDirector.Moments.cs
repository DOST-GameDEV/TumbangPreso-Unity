using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    public sealed partial class MatchDirector
    {
        public event Action<MatchMoment> MomentPresented;
        public long PresentationMatchId { get; private set; }
        public MatchMoment LastMoment { get; private set; }
        private long _momentSequence, _receivedMomentSequence;
        private int _firstKnockdownRound;

        private void BeginPresentationMatch()
        {
            PresentationMatchId = NetAuthority.IsNetworked && Net.MatchRpc.Instance != null
                ? Net.MatchRpc.Instance.EnsurePresentationMatch()
                : Math.Max(DateTime.UtcNow.Ticks, PresentationMatchId + 1);
            _momentSequence = _receivedMomentSequence = 0;
            _firstKnockdownRound = 0;
            LastMoment = default;
        }
        public void AdoptPresentationMatch(long id)
        {
            if (NetAuthority.ShouldResolve() || id <= 0 || id == PresentationMatchId) return;
            PresentationMatchId = id; _receivedMomentSequence = 0; LastMoment = default;
        }
        internal void ResetHostCatchChain()
        {
            if (CanRecordHostChain) _hostChains.ResetCatchChains();
        }
        private void HostScoreMoment(int actor, ScoreEvent score, int previousLeader)
        {
            if (!CanRecordHostChain || (score != ScoreEvent.LataKnocked && score != ScoreEvent.SproutKnock)) return;
            var result = _hostChains.AcceptedKnockdown(actor, GameServices.Round.Lata.HostKnockdownSerial);
            if (!result.Applied) return;
            if (_firstKnockdownRound != RoundNumber)
            {
                _firstKnockdownRound = RoundNumber;
                AddScore(actor, ScoreEvent.FirstKnockdownBonus);
                PresentHostMoment(actor, MatchMomentKind.FirstKnockdown, result.Count, 50);
            }
            if (GameServices.Round.TimeLeft <= 10)
            {
                AddScore(actor, ScoreEvent.LateKnockdownBonus);
                PresentHostMoment(actor, MatchMomentKind.LateKnockdown, result.Count, 50);
            }
            if (result.Bonus > 0)
            {
                AddScore(actor, ScoreEvent.MultiKnockdownBonus);
                PresentHostMoment(actor, MatchMomentKind.MultiKnockdown, result.Count, 50);
            }
        }
        private void PresentHostMoment(int actor, MatchMomentKind kind, int count = 0, int bonus = 0)
        {
            var moment = new MatchMoment(PresentationMatchId, ++_momentSequence, RoundNumber, actor, kind, count, bonus);
            if (!moment.IsValid) return;
            LastMoment = moment; MomentPresented?.Invoke(moment);
            Net.MatchRpc.Instance?.BroadcastMatchMoment(moment);
        }
        public bool ApplyNetworkMoment(MatchMoment moment)
        {
            if (NetAuthority.ShouldResolve() || !MatchInProgress || IsWarmupBuffer || !moment.IsValid
                || moment.MatchId != PresentationMatchId || moment.Round != RoundNumber
                || moment.Sequence <= _receivedMomentSequence) return false;
            _receivedMomentSequence = moment.Sequence; LastMoment = moment;
            MomentPresented?.Invoke(moment); return true;
        }
        private void AwardHostChain(int actor, ChainResult result, bool caught)
        {
            if (!caught) return; // Retired accuracy rewards must not double-pay the new knockdown rule.
            if (result.Bonus > 0)
                AddScore(actor, result.Count == 2 ? ScoreEvent.DoubleCatch : result.Count == 3 ? ScoreEvent.TripleCatch : ScoreEvent.MultiCatch);
            var kind = result.Count == 1 ? MatchMomentKind.SingleCatch : result.Count == 2 ? MatchMomentKind.DoubleCatch :
                result.Count == 3 ? MatchMomentKind.TripleCatch : MatchMomentKind.MultiCatch;
            PresentHostMoment(actor, kind, result.Count, result.Bonus);
        }
    }
}
