using System;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed partial class HeroAbilitySystem
    {
        private readonly long[] _skillRequests=new long[2];
        private readonly bool[] _skillSettled=new bool[2];
        public void TrackSkillRequest(int slot,long request)
        {if(slot<0||slot>=2||request<=0)return;_skillRequests[slot]=request;_skillSettled[slot]=false;}
        public bool MatchesSkillRequest(int slot,long request)=>slot>=0&&slot<2&&request>0&&_skillRequests[slot]==request;
        public bool PendingSkillReceipt(int slot,long request)=>MatchesSkillRequest(slot,request)&&!_skillSettled[slot];
        private void ClearSkillReceipts(){Array.Clear(_skillRequests,0,2);Array.Clear(_skillSettled,0,2);}
        public bool ResolveSkillReceipt(int slot,long request,bool accepted,float cooldown,int charges)
        {
            if(!MatchesSkillRequest(slot,request)||_skillSettled[slot]||NetAuthority.IsHost||_motor.PlayerSlot!=NetAuthority.LocalSlot)return false;
            var ability=AbilityFor((Slot)slot);if(ability==null)return false;
            _skillSettled[slot]=true;
            if(!accepted)RollBackPredictedCast((Slot)slot,refundResources:false);
            // The host reports the actual resource result. A free reactivation
            // cannot mint a charge; an old answer cannot edit a later prediction.
            ability.ApplyNetworkSnapshot(cooldown,charges,mayLower:true);
            return true;
        }
        public HeroKit.CastOutcome CheckNetworkSkill(Slot slot,Vector3 position,Vector3 forward,Vector3 aim,float held)
        {
            if(Kit==null||_motor==null||slot==Slot.Ultimate)return HeroKit.CastOutcome.Missing;
            if(PresentationClock.BlocksInput)return HeroKit.CastOutcome.CannotAct;
            var ability=AbilityFor(slot);if(ability==null)return HeroKit.CastOutcome.Missing;
            float previous=ability.HeldSecondsOnCast;
            try{ability.HeldSecondsOnCast=held;return Kit.CheckSkill((int)slot,new AbilityContext(_motor,_carrier,_verbs,position,forward,aim));}
            finally{ability.HeldSecondsOnCast=previous;}
        }
    }
}
