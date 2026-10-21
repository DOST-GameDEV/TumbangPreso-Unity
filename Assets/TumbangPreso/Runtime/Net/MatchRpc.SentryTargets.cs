using System.Collections.Generic;
using TumbangPreso.Abilities;
using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private readonly Dictionary<(int owner, long instance), SentryTargetState> _pendingSentryTargets =
            new Dictionary<(int, long), SentryTargetState>();
        private long _sentryTargetMatch;
        private int _sentryTargetRound = -1;

        private void PrepareSentryTargetScope(long match, int round)
        {
            if (_sentryTargetMatch == match && _sentryTargetRound == round) return;
            _pendingSentryTargets.Clear(); _sentryTargetMatch = match; _sentryTargetRound = round;
        }

        public void SentryReady(PaeteSentry sentry)
        {
            if (!NetAuthority.IsNetworked || sentry == null) return;
            var state = SentryTargetState.Capture(sentry);
            if (!state.IsValid || state.Match != PresentationMatchId || state.Round != GameServices.Match?.RoundNumber) return;
            PrepareSentryTargetScope(state.Match, state.Round);
            if (NetAuthority.IsHost)
            {
                if (!sentry.OwnsSelection || _nm?.CustomMessagingManager == null) return;
                // Establish match/round on the same reliable stream before its target set.
                BroadcastMatchState();
                using var writer = new FastBufferWriter(SentryTargetState.WireBytes, Allocator.Temp);
                writer.WriteNetworkSerializable(state);
                _nm.CustomMessagingManager.SendNamedMessageToAll("SentryTargets", writer, NetworkDelivery.ReliableSequenced);
                return;
            }
            var key = (state.Owner, state.Instance);
            if (_pendingSentryTargets.TryGetValue(key, out var pending))
            {
                _pendingSentryTargets.Remove(key);
                sentry.ApplyTargetState(pending);
            }
        }

        private void OnSentryTargetsMsg(ulong sender, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(sender) || !SentryTargetState.TryRead(ref reader, out var state) ||
                state.Match != PresentationMatchId || state.Round != GameServices.Match?.RoundNumber) return;
            PrepareSentryTargetScope(state.Match, state.Round);
            var sentry = PaeteSentry.Find(state);
            if (sentry != null) { sentry.ApplyTargetState(state); return; }
            var key = (state.Owner, state.Instance);
            if (_pendingSentryTargets.ContainsKey(key)) return;
            if (_pendingSentryTargets.Count >= WorldEffectSnapshot.MaxFields)
            {
                _pendingSentryTargets.Clear();
                QueueWorldSnapshotRefresh(state.Round);
            }
            _pendingSentryTargets.Add(key, state);
        }
    }
}
