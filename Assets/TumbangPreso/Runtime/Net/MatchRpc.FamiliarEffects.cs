using TumbangPreso.Abilities;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        public void BroadcastFamiliarEffect(int slot, ulong? targetPeer = null)
        {
            if (!NetAuthority.ShouldResolve() || !ValidSlot(slot) || _nm?.CustomMessagingManager == null) return;
            var unit = Unit(slot); var pet = Familiar(slot); var kit = unit?.AbilitySystem?.Kit;
            // Possession is retired from the current kit. This is the live seance.
            if (pet == null || !pet.IsDevouring || kit?.Ultimate == null) return;
            var state = new FamiliarEffectState
            {
                Seat = slot, Scope = CaptureActionScope(slot), Phase = kit.Ultimate.AcceptedUltimatePhase,
                HeroId = new FixedString64Bytes(kit.HeroId), AbilityId = new FixedString64Bytes(kit.Ultimate.Id),
                Position = pet.DevourGround, ExpiresAt = _nm.ServerTime.Time + pet.DevourRemaining,
                Yaw = pet.transform.eulerAngles.y,
            };
            if (!state.IsValid) return;
            using var writer = new FastBufferWriter(FamiliarEffectState.MaxWireBytes, Allocator.Temp);
            writer.WriteNetworkSerializable(state);
            foreach (ulong peer in _nm.ConnectedClientsIds)
            {
                if (peer == _nm.LocalClientId || (targetPeer.HasValue && peer != targetPeer.Value)) continue;
                _nm.CustomMessagingManager.SendNamedMessage("FamiliarEffect", peer, writer, NetworkDelivery.ReliableSequenced);
            }
        }

        private void OnFamiliarEffectMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(senderClientId) || _nm == null ||
                !FamiliarEffectState.TryRead(ref reader, out var state)) return;
            ApplyFamiliarEffect(state, _nm.ServerTime.Time);
        }

        private bool ApplyFamiliarEffect(FamiliarEffectState state, double now)
        {
            if (!state.IsValid || double.IsNaN(now) || double.IsInfinity(now)) return false;
            var unit = Unit(state.Seat);
            if (unit == null || !state.Scope.Matches(PresentationMatchId,
                GameServices.Match?.RoundNumber ?? -1, unit.MovementEpoch) ||
                !(unit.AbilitySystem?.Kit is NemuHeroKit kit) || !state.MatchesKit(kit)) return false;
            var ability = kit.Ultimate;
            var pet = Familiar(state.Seat);
            if (pet == null || ability.ReservedForIntroduction || state.Phase < ability.AcceptedUltimatePhase) return false;
            if (state.Phase == ability.AcceptedUltimatePhase &&
                ((!ability.IsActive && !ability.IsWindingUp) || pet.IsDevouring)) return false;
            float remaining = Mathf.Clamp((float)(state.ExpiresAt - now), 0, ability.Duration);
            if (remaining <= 0) return false;
            kit.RestoreFamiliar(unit, 2, state.Position, remaining, state.Yaw);
            ability.AdoptUltimatePhase(state.Phase);
            return true;
        }
    }
}
