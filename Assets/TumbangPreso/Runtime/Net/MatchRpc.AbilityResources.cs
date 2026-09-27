using System;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private readonly long[] _abilityResourceSequence = new long[Balance.PlayerCount];
        private readonly long[] _receivedAbilityResourceSequence = new long[Balance.PlayerCount];
        private long _abilityResourceMatch;

        private void BroadcastAbilityState(int slot, CharacterMotor unit)
        {
            if (!NetAuthority.IsHost || !ValidSlot(slot) || _nm?.CustomMessagingManager == null) return;
            var kit = unit?.AbilitySystem?.Kit;
            if (kit == null) return;
            var scope = new GameplayActionScope { Match = EnsurePresentationMatch(),
                Round = GameServices.Match?.RoundNumber ?? 0, Epoch = unit.MovementEpoch };
            var snapshot = AbilityResourceSnapshot.Capture(kit, slot, scope, ++_abilityResourceSequence[slot]);
            if (!snapshot.IsValid) return;
            using var writer = new FastBufferWriter(AbilityResourceSnapshot.MaxWireBytes, Allocator.Temp);
            writer.WriteNetworkSerializable(snapshot);
            _nm.CustomMessagingManager.SendNamedMessageToAll("SyncAbility", writer, NetworkDelivery.ReliableSequenced);
        }

        private void OnSyncAbilityMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(senderClientId) ||
                !AbilityResourceSnapshot.TryRead(ref reader, out var snapshot)) return;
            var unit = Unit(snapshot.Seat);
            if (unit == null || !snapshot.Scope.Matches(PresentationMatchId,
                GameServices.Match?.RoundNumber ?? -1, unit.MovementEpoch)) return;
            if (_abilityResourceMatch != snapshot.Scope.Match)
            {
                _abilityResourceMatch = snapshot.Scope.Match;
                Array.Clear(_receivedAbilityResourceSequence, 0, _receivedAbilityResourceSequence.Length);
            }
            if (snapshot.Sequence <= _receivedAbilityResourceSequence[snapshot.Seat]) return;
            bool mine = snapshot.Seat == NetAuthority.LocalSlot;
            bool roundLive = GameServices.Round != null && GameServices.Round.RoundActive;
            // A delayed host snapshot cannot refund the owner's predicted spend.
            if (snapshot.TryApply(unit.AbilitySystem?.Kit, mayLower: !mine || !roundLive))
                _receivedAbilityResourceSequence[snapshot.Seat] = snapshot.Sequence;
        }
    }
}
