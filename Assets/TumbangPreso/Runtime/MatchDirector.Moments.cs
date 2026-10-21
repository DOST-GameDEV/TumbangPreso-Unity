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

        private bool _presentationPrepared;

        /// <summary>
        /// Stamp the coming match's id BEFORE it starts, on the host or offline, and have `StartMatch`
        /// keep it. For a map whose look before the start depends on the id: the Arena derives its
        /// layout from it (`ArenaStage.LayoutFor`), and with the id stamped only in `StartMatch` the
        /// stage its opening built was the layout of id 0 and the one played was another (owner,
        /// 2026-10-05: "the map in the cinematic is different from what you first play").
        /// Nothing is sent: a client adopts the host's id as it always has.
        /// </summary>
        public void PreparePresentationMatch()
        {
            if (MatchInProgress || _presentationPrepared || !NetAuthority.ShouldResolve()) return;
            // ⚠️ ONLINE THE ID IS THE TRANSPORT'S, AND NOT BEFORE THE TRANSPORT IS THERE. The first online test
            // (owner, 2026-10-05: "the map doesnt update for the other people") had the host on one layout and
            // everyone else on another. This is called in the arena's first frames; with `MatchRpc.Instance` not
            // yet set the host stamped an id of its own here and `StartMatch` then KEPT it, while every client
            // adopted the transport's. Wait for the transport; `BeginPresentationMatch` reads it again anyway.
            if (NetAuthority.IsNetworked && Net.MatchRpc.Instance == null) return;
            BeginPresentationMatch();
            _presentationPrepared = PresentationMatchId != 0;
        }

        private void BeginPresentationMatch()
        {
            if (_presentationPrepared)
            {
                // Prepared before the start: the id stands, the rest begins as ever.
                _presentationPrepared = false;
                _momentSequence = _receivedMomentSequence = 0; _firstKnockdownRound = 0; LastMoment = default;
                // Online the transport's id is the one every peer has: take it again, whatever was prepared.
                if (NetAuthority.IsNetworked && Net.MatchRpc.Instance != null)
                { PresentationMatchId = Net.MatchRpc.Instance.EnsurePresentationMatch(); return; }
                if (PresentationMatchId != 0) return;
            }

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
