using System.Collections.Generic;
using TumbangPreso.Abilities;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private readonly Dictionary<ulong,(long request,int slot,bool accepted)> _lastSkillRequest=new Dictionary<ulong,(long,int,bool)>();
        private readonly long[] _lastSkillEvent=new long[4];
        private long _skillRequestSequence,_skillEventSequence,_skillEpoch;
        private int _skillRound;
        private void PrepareSkillReceipts()
        {
            int round=GameServices.Match?.RoundNumber??0;
            if(_skillEpoch==PresentationMatchId&&_skillRound==round)return;
            _skillEpoch=PresentationMatchId;_skillRound=round;_lastSkillRequest.Clear();System.Array.Clear(_lastSkillEvent,0,4);
        }
        private static HeroAbility Skill(CharacterMotor actor,int slot)=>slot==0?actor?.AbilitySystem?.Kit?.Skill1:slot==1?actor?.AbilitySystem?.Kit?.Skill2:null;
        private void AcceptSkillReceipt(ulong client,int seat,int slot,long request)
        {
            var ability=Skill(Unit(seat),slot);if(ability==null||request<=0)return;
            using var writer=new FastBufferWriter(48,Allocator.Temp);
            writer.WriteValueSafe(PresentationMatchId);writer.WriteValueSafe(GameServices.Match.RoundNumber);writer.WriteValueSafe(seat);writer.WriteValueSafe(slot);writer.WriteValueSafe(request);
            writer.WriteValueSafe(ability.CooldownRemaining);writer.WriteValueSafe(ability.ChargesRemaining);writer.WriteValueSafe(SharedUltimatePhase.Now);
            _nm.CustomMessagingManager.SendNamedMessage("CastAccepted",client,writer);
        }
        private void OnCastAccepted(ulong sender,FastBufferReader reader)
        {
            if(NetAuthority.IsHost||!FromHost(sender)||!reader.TryBeginRead(44))return;
            reader.ReadValueSafe(out long match);reader.ReadValueSafe(out int round);reader.ReadValueSafe(out int seat);reader.ReadValueSafe(out int slot);reader.ReadValueSafe(out long request);
            reader.ReadValueSafe(out float cooldown);reader.ReadValueSafe(out int charges);reader.ReadValueSafe(out double at);
            if(!ValidSkillReceipt(match,round,seat,slot,request,cooldown,charges,at))return;
            Unit(seat)?.AbilitySystem?.ResolveSkillReceipt(slot,request,true,Mathf.Max(0,cooldown-(float)System.Math.Max(0,SharedUltimatePhase.Now-at)),charges);
        }
        private bool ValidSkillReceipt(long match,int round,int seat,int slot,long request,float cooldown,int charges,double at)
            =>match==PresentationMatchId&&round==GameServices.Match?.RoundNumber&&seat==NetAuthority.LocalSlot&&ValidSlot(seat)&&slot>=0&&slot<2&&request>0
            &&Finite(cooldown)&&cooldown>=0&&cooldown<=300&&charges>=0&&charges<=32&&!double.IsNaN(at)&&!double.IsInfinity(at)&&at<=SharedUltimatePhase.Now+1;
    }
}
