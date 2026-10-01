using System;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private const int ObjectiveCooldownBytes = GameplayActionScope.WireBytes + sizeof(int) + sizeof(long) * 2 + sizeof(float);
        private readonly long[] _objectiveCooldownSequence = new long[Balance.PlayerCount];
        private readonly long[] _receivedObjectiveCooldown = new long[Balance.PlayerCount];
        private long _objectiveCooldownMatch;
        private void ResetObjectiveCooldownTransport()
        {
            _objectiveCooldownMatch = 0;
            Array.Clear(_objectiveCooldownSequence, 0, _objectiveCooldownSequence.Length);
            Array.Clear(_receivedObjectiveCooldown, 0, _receivedObjectiveCooldown.Length);
        }

        public void BroadcastObjectiveCooldown(int seat, float amount)
        {
            if (!NetAuthority.IsHost || !ValidSlot(seat) || !ValidObjectiveIncome(amount) || _nm?.CustomMessagingManager == null) return;
            var unit = Unit(seat);
            if (!(unit?.AbilitySystem?.Kit is ZackHeroKit)) return;
            PrepareSkillReceipts();
            var scope = new GameplayActionScope { Match = EnsurePresentationMatch(), Round = GameServices.Match.RoundNumber, Epoch = unit.MovementEpoch };
            if (!scope.IsValid) return;
            var peer = NetSession.Instance?.Lobby?.PeerInSeat(seat);
            long processed = peer != null && _lastSkillRequest.TryGetValue((ulong)peer.PeerId, out var request) ? request.request : 0;
            using var writer = new FastBufferWriter(ObjectiveCooldownBytes, Allocator.Temp);
            writer.WriteNetworkSerializable(scope); writer.WriteValueSafe(seat);
            writer.WriteValueSafe(++_objectiveCooldownSequence[seat]); writer.WriteValueSafe(amount); writer.WriteValueSafe(processed);
            _nm.CustomMessagingManager.SendNamedMessageToAll("ObjectiveCooldown", writer, NetworkDelivery.ReliableSequenced);
        }

        private static bool ValidObjectiveIncome(float amount) => float.IsFinite(amount) && amount > 0 &&
            amount <= Math.Max(Math.Max(Balance.UltimateChargeLataKnock, Balance.UltimateChargeTag),
                Math.Max(Balance.UltimateChargeOwnSlipperRetrieved, Balance.UltimateChargeLegalThrow));

        private void OnObjectiveCooldownMsg(ulong sender, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(sender) || reader.Length - reader.Position != ObjectiveCooldownBytes ||
                !reader.TryBeginRead(ObjectiveCooldownBytes)) return;
            reader.ReadNetworkSerializable(out GameplayActionScope scope); reader.ReadValueSafe(out int seat);
            reader.ReadValueSafe(out long sequence); reader.ReadValueSafe(out float amount); reader.ReadValueSafe(out long processed);
            ApplyObjectiveCooldown(scope, seat, sequence, amount, processed);
        }

        private bool ApplyObjectiveCooldown(GameplayActionScope scope, int seat, long sequence, float amount, long processed)
        {
            if (!ValidSlot(seat) || sequence <= 0 || processed < 0 || !ValidObjectiveIncome(amount) ||
                GameServices.Round?.RoundActive != true) return false;
            var unit = Unit(seat); var system = unit?.AbilitySystem;
            if (!(system?.Kit is ZackHeroKit kit) || !scope.Matches(PresentationMatchId, GameServices.Match.RoundNumber, unit.MovementEpoch)) return false;
            if (_objectiveCooldownMatch != scope.Match)
            {
                _objectiveCooldownMatch = scope.Match;
                Array.Clear(_receivedObjectiveCooldown, 0, _receivedObjectiveCooldown.Length);
            }
            if (sequence <= _receivedObjectiveCooldown[seat]) return false;
            bool mine = seat == NetAuthority.LocalSlot;
            kit.ApplyObjectiveCooldown(amount,
                !mine || system.LatestSkillRequest(0) <= processed,
                !mine || system.LatestSkillRequest(1) <= processed);
            _receivedObjectiveCooldown[seat] = sequence;
            return true;
        }
    }
}
