using System.Collections;
using TumbangPreso.Abilities;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private bool _preparationFollowupPending;
        private static HeroAbility PreparationAbility(HeroKit kit,int kind)
        {
            if(kit==null)return null;
            switch((HeroAbilitySystem.Slot)kind)
            {
                case HeroAbilitySystem.Slot.Skill1:return kit.Skill1;
                case HeroAbilitySystem.Slot.Skill2:return kit.Skill2;
                case HeroAbilitySystem.Slot.Ultimate:return kit.Ultimate;
                default:return null;
            }
        }
        private void SendPreparationSnapshot(int seat,ulong peer)
        {
            if(!NetAuthority.IsHost || GameServices.Match==null || _nm?.CustomMessagingManager==null || peer==_nm.LocalClientId)return;
            var kit=Unit(seat)?.AbilitySystem?.Kit;if(kit==null)return;
            foreach(var slot in new[]{HeroAbilitySystem.Slot.Skill1,HeroAbilitySystem.Slot.Skill2,HeroAbilitySystem.Slot.Ultimate})
            {
                var ability=PreparationAbility(kit,(int)slot);
                if(ability?.SupportsPendingSnapshot!=true)continue;
                bool pending=ability.CapturePendingPreparation(out var context,out float remaining);
                using var writer=new FastBufferWriter(192,Allocator.Temp);
                writer.WriteValueSafe(seat);writer.WriteValueSafe(GameServices.Match.RoundNumber);
                writer.WriteValueSafe(kit.HeroId);writer.WriteValueSafe((int)slot);
                writer.WriteValueSafe(pending?remaining:0);writer.WriteValueSafe(pending?ability.HeldSecondsOnCast:0);
                writer.WriteValueSafe((float)_nm.ServerTime.Time);
                writer.WriteValueSafe(pending?context.Position:Vector3.zero);
                writer.WriteValueSafe(pending?context.Forward:Vector3.forward);
                writer.WriteValueSafe(pending?context.AimPoint:Vector3.zero);
                _nm.CustomMessagingManager.SendNamedMessage("CastPreparation",peer,writer,NetworkDelivery.ReliableSequenced);
            }
        }
        private void OnCastPreparationMsg(ulong sender,FastBufferReader reader)
        {
            if(NetAuthority.IsHost || !FromHost(sender))return;
            reader.ReadValueSafe(out int seat);reader.ReadValueSafe(out int round);
            reader.ReadValueSafe(out string hero);reader.ReadValueSafe(out int kind);
            reader.ReadValueSafe(out float remaining);reader.ReadValueSafe(out float held);
            reader.ReadValueSafe(out float sentAt);reader.ReadValueSafe(out Vector3 position);
            reader.ReadValueSafe(out Vector3 forward);reader.ReadValueSafe(out Vector3 aim);
            if(!ValidSlot(seat) || GameServices.Match==null || GameServices.Match.RoundNumber!=round
                || !Finite(remaining) || !Finite(held) || !Finite(sentAt) || !Finite(position) || !Finite(forward) || !Finite(aim)
                || remaining<0 || held<0)return;
            var system=Unit(seat)?.AbilitySystem;var ability=PreparationAbility(system?.Kit,kind);
            if(system?.Kit?.HeroId!=hero || ability?.SupportsPendingSnapshot!=true || remaining>ability.Windup+.0001f)return;
            float age=Mathf.Max(0,(float)_nm.ServerTime.Time-sentAt);
            float live=Mathf.Max(0,remaining-age);
            using(NetCue.SuppressRelay())system.RestoreJoiningPreparation((HeroAbilitySystem.Slot)kind,position,forward,aim,held,live);
            // Contact may have happened after the host captured this pre-impact
            // batch. Ask for current fields instead of replaying an expired hit.
            if(remaining>0 && live<=0 && !_preparationFollowupPending)
            {
                Debug.Log($"[CastPreparation] expired in transit seat={seat} kind={kind} remaining={remaining:F4} age={age:F4}");
                StartCoroutine(RefreshAfterExpiredPreparation(round));
            }
        }
        private IEnumerator RefreshAfterExpiredPreparation(int round)
        {
            _preparationFollowupPending=true;
            var network=_nm;ulong client=network.LocalClientId;string scene=SceneManager.GetActiveScene().name;
            yield return new WaitForSecondsRealtime(SnapshotRequestInterval+.05f);
            _preparationFollowupPending=false;
            if(_nm==network && network!=null && network.IsConnectedClient && network.LocalClientId==client
                && GameServices.Match?.RoundNumber==round && SceneManager.GetActiveScene().name==scene)
                RequestWorldSnapshot();
        }
    }
}
