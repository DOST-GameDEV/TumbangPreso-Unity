using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private readonly ulong[] _unitPoseSerial=new ulong[TumbangPreso.Core.Balance.PlayerCount];
        public void BeginEdgeMovementOwnership(int slot)
        {
            if(!NetAuthority.IsHost||!ValidSlot(slot))return;
            var unit=Unit(slot);if(unit==null)return;
            unit.AdoptMovementEpoch(++_movementEpochs[slot]);_moveBudgets.Remove(slot);
            // Owners stop sending movement during recovery. A lost last ordinary
            // pose would otherwise leave them stuck forever after the host releases.
            SendUnitPose(slot,unit.transform.position,unit.transform.eulerAngles.y,unit.Velocity,true);
        }

        public void RequestEdgeClimbServerRpc(int slot,int epoch)
        {
            if(_nm?.CustomMessagingManager==null||NetAuthority.IsHost)return;
            using var writer=new FastBufferWriter(8,Allocator.Temp);
            writer.WriteValueSafe(slot);writer.WriteValueSafe(epoch);
            _nm.CustomMessagingManager.SendNamedMessage("ReqEdgeClimb",NetworkManager.ServerClientId,writer);
        }

        private void OnReqEdgeClimbMsg(ulong sender,FastBufferReader reader)
        {
            if(!NetAuthority.IsHost)return;
            reader.ReadValueSafe(out int slot);reader.ReadValueSafe(out int epoch);
            if(!SenderOwnsClaimedSeat(sender,slot,out var unit)||epoch!=_movementEpochs[slot])return;
            // The client supplies no anchor or landing point. Current host-side
            // swim position and actual geometry decide whether a climb can start.
            unit.TryBeginLagoonEdgeRecovery();
            SyncUnitTransformClientRpc(slot,unit.transform.position,unit.transform.eulerAngles.y,unit.Velocity);
        }
    }
}
