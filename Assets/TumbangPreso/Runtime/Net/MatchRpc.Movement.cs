using TumbangPreso.Abilities;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private void SendMovementSnapshot(int seat,ulong peer,int fieldGeneration)
        {
            if(!NetAuthority.IsHost || GameServices.Match==null || _nm?.CustomMessagingManager==null || peer==_nm.LocalClientId) return;
            var kit=Unit(seat)?.AbilitySystem?.Kit;
            HeroMovementState state;
            if(kit is ZackHeroKit zack) state=zack.CaptureMovementState();
            else if(kit is SeanHeroKit sean) state=sean.CaptureMovementState();
            else return;
            using var writer=new FastBufferWriter(160,Allocator.Temp);
            writer.WriteValueSafe(seat); writer.WriteValueSafe(GameServices.Match.RoundNumber);
            writer.WriteValueSafe(kit.HeroId); writer.WriteValueSafe(fieldGeneration);
            writer.WriteValueSafe(state.Remaining); writer.WriteValueSafe(state.UntilNextEmission);
            writer.WriteValueSafe((float)_nm.ServerTime.Time);
            int count=state.Wake?.Length??0;
            writer.WriteValueSafe(count);
            writer.WriteValueSafe(state.KnownWake);
            for(int i=0;i<count;i++) writer.WriteValueSafe(state.Wake[i]);
            _nm.CustomMessagingManager.SendNamedMessage("MovementWindow",peer,writer,NetworkDelivery.ReliableSequenced);
        }

        private void OnMovementWindowMsg(ulong sender,FastBufferReader reader)
        {
            if(NetAuthority.IsHost || !FromHost(sender)) return;
            reader.ReadValueSafe(out int seat); reader.ReadValueSafe(out int round);
            reader.ReadValueSafe(out string hero); reader.ReadValueSafe(out int fieldGeneration);
            reader.ReadValueSafe(out float remaining); reader.ReadValueSafe(out float untilNext);
            reader.ReadValueSafe(out float sentAt); reader.ReadValueSafe(out int count);
            reader.ReadValueSafe(out uint knownWake);
            if(!ValidSlot(seat) || GameServices.Match==null || GameServices.Match.RoundNumber!=round
                || fieldGeneration<=0 || fieldGeneration!=_lastWorldFieldGeneration || count<0 || count>HeroMovementState.MaxWakePoints
                || (hero=="sean" && count!=0)
                || !Finite(sentAt)) return;
            var wake=new Vector3[count];
            for(int i=0;i<count;i++) reader.ReadValueSafe(out wake[i]);
            var system=Unit(seat)?.AbilitySystem;
            if(system?.Kit?.HeroId!=hero || (hero!="zack" && hero!="sean")) return;
            float age=Mathf.Max(0,(float)_nm.ServerTime.Time-sentAt);
            var state=new HeroMovementState { Remaining=remaining,UntilNextEmission=untilNext,Wake=wake,KnownWake=knownWake };
            float interval=hero=="zack"?.30f:.15f;
            if(!state.Valid(system.Kit.Skill1.Duration,interval,age)) return;
            if(remaining>0 && age>=remaining)
                Debug.Log($"[MovementWindow] expired in transit seat={seat} remaining={remaining:F4} age={age:F4}");
            using(NetCue.SuppressRelay()) system.RestoreJoiningMovement(state,age);
            // A spike can hide host emissions that happened after this captured
            // field batch. Recover those real fields, never replay guessed drops.
            if(remaining>0 && age>=untilNext && !_preparationFollowupPending)
                StartCoroutine(RefreshAfterExpiredPreparation(round));
        }
    }
}
