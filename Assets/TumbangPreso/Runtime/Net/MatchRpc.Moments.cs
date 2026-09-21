using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        public long PresentationMatchId { get; private set; }
        private readonly List<(MatchMoment moment, float expires)> _pendingMoments = new List<(MatchMoment, float)>(8);
        private void PreparePresentationMatch()
        { PresentationMatchId = Math.Max(DateTime.UtcNow.Ticks, PresentationMatchId + 1); _pendingMoments.Clear(); }
        public long EnsurePresentationMatch()
        {
            if (PresentationMatchId == 0 && NetAuthority.ShouldResolve()) PreparePresentationMatch();
            return PresentationMatchId;
        }
        private bool AdoptPresentationMatch(long id)
        {
            if (id <= 0 || id < PresentationMatchId) return false;
            if (id != PresentationMatchId) { PresentationMatchId = id; _pendingMoments.Clear(); }
            GameServices.Match?.AdoptPresentationMatch(id); return true;
        }
        public void BroadcastMatchMoment(MatchMoment moment)
        {
            if (!NetAuthority.ShouldResolve() || !moment.IsValid || _nm?.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(moment.MatchId); writer.WriteValueSafe(moment.Sequence);
            writer.WriteValueSafe(moment.Round); writer.WriteValueSafe(moment.Actor);
            writer.WriteValueSafe((byte)moment.Kind); writer.WriteValueSafe(moment.Count); writer.WriteValueSafe(moment.Bonus);
            _nm.CustomMessagingManager.SendNamedMessageToAll("MatchMoment", writer);
        }
        private void OnMatchMomentMsg(ulong sender, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(sender) || !reader.TryBeginRead(33)) return;
            reader.ReadValueSafe(out long match); reader.ReadValueSafe(out long sequence);
            reader.ReadValueSafe(out int round); reader.ReadValueSafe(out int actor);
            reader.ReadValueSafe(out byte kind); reader.ReadValueSafe(out int count); reader.ReadValueSafe(out int bonus);
            var moment = new MatchMoment(match, sequence, round, actor, (MatchMomentKind)kind, count, bonus);
            if (!moment.IsValid || match != PresentationMatchId) return;
            if (GameServices.Match != null && GameServices.Match.RoundNumber == round)
            { GameServices.Match.ApplyNetworkMoment(moment); return; }
            // A round state may follow the event during scene loading. Bounded,
            // short-lived delivery prevents a stale celebration after late join.
            if (_pendingMoments.Count < 8) _pendingMoments.Add((moment, Time.unscaledTime + 1));
        }
        private void Update()
        {
            TickReplayTransfer();
            for (int i = 0; i < _pendingMoments.Count;)
            {
                var entry = _pendingMoments[i]; var match = GameServices.Match;
                if (Time.unscaledTime > entry.expires || entry.moment.MatchId != PresentationMatchId
                    || (match != null && entry.moment.Round < match.RoundNumber))
                { _pendingMoments.RemoveAt(i); continue; }
                if (match != null && match.RoundNumber == entry.moment.Round)
                { match.ApplyNetworkMoment(entry.moment); _pendingMoments.RemoveAt(i); continue; }
                i++;
            }
        }
    }
}
