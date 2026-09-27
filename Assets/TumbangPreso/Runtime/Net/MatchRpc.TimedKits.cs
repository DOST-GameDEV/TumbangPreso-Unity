using System;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private readonly long[] _timedKitSequence = new long[Balance.PlayerCount];
        private readonly long[] _receivedTimedKitSequence = new long[Balance.PlayerCount];
        private long _timedKitReceivedMatch;

        private void ResetTimedKitTransport()
        {
            _timedKitReceivedMatch = 0;
            Array.Clear(_receivedTimedKitSequence, 0, _receivedTimedKitSequence.Length);
        }

        private void SendBoundTimedKit(int slot, ulong peer, HeroKit kit, ITimedKitReplication replication)
        {
            var unit = Unit(slot);
            if (unit == null || !ValidSlot(slot)) return;
            var scope = new GameplayActionScope { Match = EnsurePresentationMatch(),
                Round = GameServices.Match.RoundNumber, Epoch = unit.MovementEpoch };
            var state = TimedKitState.Capture(kit, replication.CaptureTimedKit(), slot, scope,
                ++_timedKitSequence[slot], GameServices.Round.TimeLeft);
            if (!state.IsValid) return;
            using var writer = new FastBufferWriter(TimedKitState.MaxWireBytes, Allocator.Temp);
            writer.WriteNetworkSerializable(state);
            _nm.CustomMessagingManager.SendNamedMessage("TimedKitState", peer, writer, NetworkDelivery.ReliableSequenced);
        }

        private void OnTimedKitStateMsg(ulong sender, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(sender) ||
                !TimedKitState.TryRead(ref reader, out var state)) return;
            ApplyTimedKitState(state, GameServices.Round?.TimeLeft ?? -1);
        }

        private bool ApplyTimedKitState(TimedKitState state, float now)
        {
            if (!state.IsValid) return false;
            if (GameServices.Round?.RoundActive != true &&
                (state.PersonalRemaining > 0 || state.UltimateRemaining > 0 || state.UltimatePending)) return false;
            var unit = Unit(state.Seat);
            if (unit == null || !state.Scope.Matches(PresentationMatchId,
                GameServices.Match?.RoundNumber ?? -1, unit.MovementEpoch)) return false;
            if (_timedKitReceivedMatch != state.Scope.Match)
            {
                ResetTimedKitTransport(); _timedKitReceivedMatch = state.Scope.Match;
            }
            if (state.Sequence <= _receivedTimedKitSequence[state.Seat]) return false;
            var kit = unit.AbilitySystem?.Kit;
            if (!state.TryResolve(kit, now, out var aged)) return false;
            using (NetCue.SuppressRelay()) ((ITimedKitReplication)kit).RestoreTimedKit(unit, aged);
            // A valid no-op (already active/consumed) still closes older snapshots.
            _receivedTimedKitSequence[state.Seat] = state.Sequence;
            return true;
        }
    }
}
