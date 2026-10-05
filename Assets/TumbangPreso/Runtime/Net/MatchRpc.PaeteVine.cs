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
        private readonly PaeteVineState[] _paeteVines=new PaeteVineState[Balance.PlayerCount];
        private readonly long[] _paeteVineSequence=new long[Balance.PlayerCount];
        private readonly long[] _receivedPaeteVineSequence=new long[Balance.PlayerCount];
        private long _paeteVineReceivedMatch;
        private void ResetPaeteVineTransport()
        {
            Array.Clear(_paeteVines,0,_paeteVines.Length);
            Array.Clear(_paeteVineSequence,0,_paeteVineSequence.Length);
            Array.Clear(_receivedPaeteVineSequence,0,_receivedPaeteVineSequence.Length);
            _paeteVineReceivedMatch=0;
        }
        public void BroadcastPaeteVine(PaeteVineState state)
        {
            if(!NetAuthority.ShouldResolve()||!ValidSlot(state.Owner)||_nm?.CustomMessagingManager==null)return;
            var unit=Unit(state.Owner);
            if(unit==null||!(unit.AbilitySystem?.Kit is PaeteHeroKit))return;
            state.Scope=new GameplayActionScope{Match=EnsurePresentationMatch(),Round=GameServices.Match?.RoundNumber??0,Epoch=unit.MovementEpoch};
            state.Sequence=++_paeteVineSequence[state.Owner];state.RoundClock=GameServices.Round?.TimeLeft??0;
            if(!state.IsValid)return;
            _paeteVines[state.Owner]=state;
            using var writer=new FastBufferWriter(PaeteVineState.WireBytes,Allocator.Temp);
            writer.WriteNetworkSerializable(state);
            _nm.CustomMessagingManager.SendNamedMessageToAll("PaeteVine",writer,NetworkDelivery.ReliableSequenced);
        }
        public void EndPaeteVine(int owner,int epoch,int targetEpoch)
        {
            if(!NetAuthority.ShouldResolve()||!ValidSlot(owner))return;
            var state=_paeteVines[owner];
            if(state.Phase!=PaeteVinePhase.Player||state.Scope.Epoch!=epoch||state.TargetEpoch!=targetEpoch)return;
            state.Phase=PaeteVinePhase.Ended;state.Duration=0;
            BroadcastPaeteVine(state);
        }
        private void SendPaeteVineSnapshot(int owner,ulong peer)
        {
            if(!NetAuthority.ShouldResolve()||!ValidSlot(owner)||_nm?.CustomMessagingManager==null||peer==_nm.LocalClientId)return;
            var state=_paeteVines[owner];var unit=Unit(owner);
            if(unit==null||!state.IsValid||!state.Scope.Matches(PresentationMatchId,GameServices.Match?.RoundNumber??-1,unit.MovementEpoch))return;
            using var writer=new FastBufferWriter(PaeteVineState.WireBytes,Allocator.Temp);
            writer.WriteNetworkSerializable(state);
            _nm.CustomMessagingManager.SendNamedMessage("PaeteVine",peer,writer,NetworkDelivery.ReliableSequenced);
        }
        private void OnPaeteVineMsg(ulong sender,FastBufferReader reader)
        {
            if(NetAuthority.IsHost||!FromHost(sender)||!PaeteVineState.TryRead(ref reader,out var state))return;
            var unit=Unit(state.Owner);
            if(unit==null||!(unit.AbilitySystem?.Kit is PaeteHeroKit kit)
                ||!state.Scope.Matches(PresentationMatchId,GameServices.Match?.RoundNumber??-1,unit.MovementEpoch)
                ||(state.Phase!=PaeteVinePhase.Ended&&GameServices.Round?.RoundActive!=true))return;
            var target=state.Target>=0?Unit(state.Target):null;
            if(state.Target>=0&&(target==null||target.MovementEpoch!=state.TargetEpoch))return;
            if(_paeteVineReceivedMatch!=state.Scope.Match)
            { _paeteVineReceivedMatch=state.Scope.Match;Array.Clear(_receivedPaeteVineSequence,0,_receivedPaeteVineSequence.Length); }
            if(state.Sequence<=_receivedPaeteVineSequence[state.Owner])return;
            float age=Mathf.Max(0,state.RoundClock-(GameServices.Round?.TimeLeft??state.RoundClock));
            if(kit.ReceiveVine(unit,state,age))_receivedPaeteVineSequence[state.Owner]=state.Sequence;
        }
    }
}
