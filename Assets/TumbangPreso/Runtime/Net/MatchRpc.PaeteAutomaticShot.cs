using TumbangPreso.Abilities;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private void BroadcastAutomaticPlantShot(PaetePlant plant, Vector3 aimPoint)
        {
            if (!NetAuthority.IsHost || plant == null || !ValidSlot(plant.OwnerSlot) ||
                plant.InstanceId <= 0 || !Finite(aimPoint) || _nm?.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(40, Allocator.Temp);
            writer.WriteValueSafe(plant.OwnerSlot);
            writer.WriteValueSafe(plant.InstanceId);
            writer.WriteValueSafe(EnsurePresentationMatch());
            writer.WriteValueSafe(GameServices.Match?.RoundNumber ?? 0);
            writer.WriteValueSafe(aimPoint);
            _nm.CustomMessagingManager.SendNamedMessageToAll("AutomaticPlantShot", writer, NetworkDelivery.ReliableSequenced);
        }

        private void OnAutomaticPlantShotMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(senderClientId) || reader.Length - reader.Position != 36 ||
                !reader.TryBeginRead(36)) return;
            reader.ReadValueSafe(out int ownerSlot);
            reader.ReadValueSafe(out long instanceId);
            reader.ReadValueSafe(out long match);
            reader.ReadValueSafe(out int round);
            reader.ReadValueSafe(out Vector3 aimPoint);
            if (!ValidSlot(ownerSlot) || instanceId <= 0 || !Finite(aimPoint) ||
                match <= 0 || match != PresentationMatchId || round != GameServices.Match?.RoundNumber) return;
            PaetePlant.ApplyAutomaticShot(ownerSlot, instanceId, aimPoint);
        }
    }
}
