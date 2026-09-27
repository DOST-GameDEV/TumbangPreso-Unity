using System;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed partial class HeroAbilitySystem
    {
        private readonly long[] _skillRequests=new long[2];
        private readonly bool[] _skillSettled=new bool[2];
        public void TrackSkillRequest(int slot,long request)
        {
            if(slot<0||slot>=2||request<=0)return;
            _skillRequests[slot]=request;_skillSettled[slot]=false;
            if(slot==1 && Kit is AmihanHeroKit) _motor.IdentifyFlightTakeoff(request,predicted:true);
        }
        public bool MatchesSkillRequest(int slot,long request)=>slot>=0&&slot<2&&request>0&&_skillRequests[slot]==request;
        public bool PendingSkillReceipt(int slot,long request)
            =>(MatchesSkillRequest(slot,request)&&!_skillSettled[slot])
                ||(slot==1 && Kit is AmihanHeroKit && _motor.PredictedFlightMatches(request));
        private void ClearSkillReceipts(){Array.Clear(_skillRequests,0,2);Array.Clear(_skillSettled,0,2);}
        public bool ResolveSkillReceipt(int slot,long request,bool accepted,float cooldown,int charges)
        {
            if(slot<0||slot>=2||request<=0||NetAuthority.IsHost||_motor.PlayerSlot!=NetAuthority.LocalSlot)return false;
            bool newest=MatchesSkillRequest(slot,request)&&!_skillSettled[slot];
            bool flightPrediction=slot==1 && Kit is AmihanHeroKit && _motor.PredictedFlightMatches(request);
            if(!newest&&!flightPrediction)return false;
            var ability=AbilityFor((Slot)slot);if(ability==null)return false;
            long previousFlight=_motor.PreviousFlightEpisode;
            bool previousFlightWasTerminal=_motor.PreviousFlightWasTerminal;
            if(newest)_skillSettled[slot]=true;
            if(!accepted&&newest)RollBackPredictedCast((Slot)slot,refundResources:false);
            if(flightPrediction)
            {
                if(accepted)_motor.ConfirmFlightTakeoff(request);
                else
                {
                    ((AmihanHeroKit)Kit).CancelFeatherfall(_motor);
                    _motor.RestoreRejectedFlightEpisode(previousFlight,previousFlightWasTerminal);
                }
            }
            // An older takeoff answer can settle its dependent descent, never newer resources.
            if(!newest)return true;
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
