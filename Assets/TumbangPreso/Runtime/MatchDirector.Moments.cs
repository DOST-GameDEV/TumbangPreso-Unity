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
        private float _lastLeadMomentAt;

        private void BeginPresentationMatch()
        {
            PresentationMatchId = NetAuthority.IsNetworked && Net.MatchRpc.Instance != null
                ? Net.MatchRpc.Instance.EnsurePresentationMatch() : DateTime.UtcNow.Ticks;
            _momentSequence = _receivedMomentSequence = 0;
            _firstKnockdownRound = 0; _lastLeadMomentAt = -100;
            LastMoment = default;
        }
        public void AdoptPresentationMatch(long id)
        {
            if (NetAuthority.ShouldResolve() || id <= 0 || id == PresentationMatchId) return;
            PresentationMatchId = id; _receivedMomentSequence = 0; LastMoment = default;
        }
        private void HostScoreMoment(int actor, ScoreEvent score, int previousLeader)
        {
            if (!CanRecordHostChain) return;
            if (score == ScoreEvent.LataKnocked && _firstKnockdownRound != RoundNumber)
            {
                _firstKnockdownRound = RoundNumber;
                PresentHostMoment(actor, MatchMomentKind.FirstKnockdown, 1);
            }
            else if (score == ScoreEvent.LataKnocked && GameServices.Round.TimeLeft <= 10)
                PresentHostMoment(actor, MatchMomentKind.LateKnockdown);
            else if ((score == ScoreEvent.Tag || score == ScoreEvent.LataKnocked)
                && previousLeader >= 0 && previousLeader != actor && _scores.WinningSlot() == actor
                && Time.time - _lastLeadMomentAt >= 8)
            {
                _lastLeadMomentAt = Time.time;
                PresentHostMoment(actor, MatchMomentKind.LeadChange);
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
            if (result.Bonus > 0)
            {
                ScoreEvent score = caught ? (result.Count == 2 ? ScoreEvent.DoubleCatch : ScoreEvent.TripleCatch)
                    : result.Count == 2 ? ScoreEvent.AccuracyChainTwo
                    : result.Count == 3 ? ScoreEvent.AccuracyChainThree : ScoreEvent.AccuracyChainLong;
                AddScore(actor, score);
            }
            switch (result.Milestone)
            {
                case ChainMilestone.AccurateThree: PresentHostMoment(actor, MatchMomentKind.AccurateThree, result.Count, result.Bonus); break;
                case ChainMilestone.AccurateFive: PresentHostMoment(actor, MatchMomentKind.AccurateFive, result.Count, result.Bonus); break;
                case ChainMilestone.DoubleCatch: PresentHostMoment(actor, MatchMomentKind.DoubleCatch, result.Count, result.Bonus); break;
                case ChainMilestone.TripleCatch: PresentHostMoment(actor, MatchMomentKind.TripleCatch, result.Count, result.Bonus); break;
            }
        }
    }
}
