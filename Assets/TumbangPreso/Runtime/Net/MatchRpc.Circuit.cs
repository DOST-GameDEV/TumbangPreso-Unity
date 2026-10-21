using System;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private readonly long[] _circuitSequence=new long[Balance.PlayerCount];
        private readonly long[] _receivedCircuitSequence=new long[Balance.PlayerCount];
        private long _circuitReceivedMatch;
        public void SendCircuitAim(int seat,long episode,long sequence,Vector3 point)
        {
            if(NetAuthority.IsHost || seat!=NetAuthority.LocalSlot || _nm?.CustomMessagingManager==null)return;
            var unit=Unit(seat);if(unit==null || !(unit.AbilitySystem?.Kit is ZackHeroKit))return;
            var aim=new ZackCircuitAim { Seat=seat,Episode=episode,Sequence=sequence,Point=point,
                Scope=new GameplayActionScope { Match=PresentationMatchId,Round=GameServices.Match?.RoundNumber??0,Epoch=unit.MovementEpoch } };
            if(!aim.IsValid)return;
            using var writer=new FastBufferWriter(ZackCircuitAim.WireBytes,Allocator.Temp);
            writer.WriteNetworkSerializable(aim);
            _nm.CustomMessagingManager.SendNamedMessage("CircuitAim",NetworkManager.ServerClientId,writer,NetworkDelivery.UnreliableSequenced);
        }
        private void OnCircuitAimMsg(ulong sender,FastBufferReader reader)
        {
            if(!NetAuthority.IsHost || !ZackCircuitAim.TryRead(ref reader,out var aim)
                || !SenderOwnsClaimedSeat(sender,aim.Seat,out var unit)
                || !aim.Scope.Matches(PresentationMatchId,GameServices.Match?.RoundNumber??-1,unit.MovementEpoch))return;
            if(unit.AbilitySystem?.Kit is ZackHeroKit kit)kit.ReceiveCircuitAim(unit,aim);
        }
        public void BroadcastCircuitState(int seat)
        {
            if(!NetAuthority.IsHost || !ValidSlot(seat) || _nm?.CustomMessagingManager==null)return;
            if(!CaptureCircuitState(seat,out var state))return;
            using var writer=new FastBufferWriter(ZackCircuitState.WireBytes,Allocator.Temp);
            writer.WriteNetworkSerializable(state);
            _nm.CustomMessagingManager.SendNamedMessageToAll("CircuitState",writer,NetworkDelivery.ReliableSequenced);
        }
        private bool CaptureCircuitState(int seat,out ZackCircuitState state)
        {
            state=default;var unit=Unit(seat);
            if(unit==null || !(unit.AbilitySystem?.Kit is ZackHeroKit kit) || GameServices.Round==null)return false;
            state=kit.CaptureCircuit();state.Seat=seat;
            state.Scope=new GameplayActionScope{Match=EnsurePresentationMatch(),Round=GameServices.Match?.RoundNumber??0,Epoch=unit.MovementEpoch};
            state.Sequence=++_circuitSequence[seat];state.RoundClock=GameServices.Round.TimeLeft;
            return state.IsValid;
        }
        private void SendCircuitSnapshot(int seat,ulong peer)
        {
            if(!NetAuthority.IsHost || !ValidSlot(seat) || _nm?.CustomMessagingManager==null || peer==_nm.LocalClientId
                || !CaptureCircuitState(seat,out var state))return;
            using var writer=new FastBufferWriter(ZackCircuitState.WireBytes,Allocator.Temp);
            writer.WriteNetworkSerializable(state);
            _nm.CustomMessagingManager.SendNamedMessage("CircuitState",peer,writer,NetworkDelivery.ReliableSequenced);
        }
        private void OnCircuitStateMsg(ulong sender,FastBufferReader reader)
        {
            if(NetAuthority.IsHost || !FromHost(sender) || !ZackCircuitState.TryRead(ref reader,out var state))return;
            var unit=Unit(state.Seat);
            if(unit==null || !(unit.AbilitySystem?.Kit is ZackHeroKit kit)
                || !state.Scope.Matches(PresentationMatchId,GameServices.Match?.RoundNumber??-1,unit.MovementEpoch)
                || (state.Phase!=CircuitPhase.Idle && GameServices.Round?.RoundActive!=true))return;
            if(_circuitReceivedMatch!=state.Scope.Match)
            { _circuitReceivedMatch=state.Scope.Match;Array.Clear(_receivedCircuitSequence,0,_receivedCircuitSequence.Length); }
            if(state.Sequence<=_receivedCircuitSequence[state.Seat])return;
            float age=Mathf.Max(0,state.RoundClock-(GameServices.Round?.TimeLeft??state.RoundClock));
            if(kit.RestoreCircuit(unit,state,age))_receivedCircuitSequence[state.Seat]=state.Sequence;
        }
    }
}
