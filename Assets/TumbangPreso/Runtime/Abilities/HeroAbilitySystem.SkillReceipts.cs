using System;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed partial class HeroAbilitySystem
    {
        private readonly long[] _skillRequests=new long[2];
        private readonly bool[] _skillSettled=new bool[2];
        private const int PendingEffectLimit = 64;
        private readonly Dictionary<long, (int slot, HeroAbility ability, bool reactivation)> _pendingSkillEffects =
            new Dictionary<long, (int, HeroAbility, bool)>();

        public bool TrackSkillRequest(int slot,long request, bool reactivation = false)
        {
            if(slot<0||slot>=2||request<=0)return false;
            var ability = AbilityFor((Slot)slot);
            if (ability == null) return false;
            if (ability.DefersPredictedEffect)
            {
                if (_pendingSkillEffects.ContainsKey(request) || _pendingSkillEffects.Count >= PendingEffectLimit) return false;
                _pendingSkillEffects.Add(request, (slot, ability, reactivation));
            }
            _skillRequests[slot]=request;_skillSettled[slot]=false;
            if(slot==1 && Kit is AmihanHeroKit) _motor.IdentifyFlightTakeoff(request,predicted:true);
            return true;
        }

        private bool PendingEffectMatches(int slot, long request)
            => _pendingSkillEffects.TryGetValue(request, out var pending) && pending.slot == slot
                && pending.ability == AbilityFor((Slot)slot);

        private bool TakePendingEffect(int slot, long request, out bool reactivation)
        {
            reactivation = false;
            if (!PendingEffectMatches(slot, request)) return false;
            reactivation = _pendingSkillEffects[request].reactivation;
            _pendingSkillEffects.Remove(request);
            return true;
        }

        private bool CanPredictSkill(Slot slot)
        {
            var ability = AbilityFor(slot);
            if (ability?.DefersPredictedEffect != true) return true;
            if (_pendingSkillEffects.Count >= PendingEffectLimit) return false;
            if (!ability.IsActive || !ability.CanReactivate) return true;
            foreach (var pending in _pendingSkillEffects.Values)
                if (pending.ability == ability) return false;
            return true;
        }

        public bool AwaitingSkillEffect(HeroAbility ability)
        {
            if (ability == null || !NetAuthority.IsNetworked || NetAuthority.IsHost
                || _motor == null || _motor.PlayerSlot != NetAuthority.LocalSlot) return false;
            foreach (var pending in _pendingSkillEffects.Values)
                if (pending.ability == ability && !pending.reactivation) return true;
            return false;
        }

        public bool HasPredictedSkillAfter(long processedRequest)
        {
            for (int slot = 0; slot < _skillRequests.Length; slot++)
                if (_skillRequests[slot] > processedRequest && !_skillSettled[slot]) return true;
            foreach (var pending in _pendingSkillEffects)
                if (pending.Key > processedRequest) return true;
            return _motor != null && _motor.FlightEpisode > processedRequest
                && _motor.PredictedFlightMatches(_motor.FlightEpisode);
        }

        public bool HasPendingUltimateAfter(long processedRequest) => _pendingUltimateRequest > processedRequest;

        public bool MatchesSkillRequest(int slot,long request)=>slot>=0&&slot<2&&request>0&&_skillRequests[slot]==request;
        public bool PendingSkillReceipt(int slot,long request)
            =>(MatchesSkillRequest(slot,request)&&!_skillSettled[slot])
                ||PendingEffectMatches(slot,request)
                ||(slot==1 && Kit is AmihanHeroKit && _motor.PredictedFlightMatches(request));
        private void ClearSkillReceipts()
        {Array.Clear(_skillRequests,0,2);Array.Clear(_skillSettled,0,2);_pendingSkillEffects.Clear();}
        internal void ResetNetworkSkillReceipts() { ClearSkillReceipts(); ResetNetworkAimTransport(); }
        public bool ResolveSkillReceipt(int slot,long request,bool accepted,float cooldown,int charges)
        {
            if(slot<0||slot>=2||request<=0||NetAuthority.IsHost||_motor.PlayerSlot!=NetAuthority.LocalSlot)return false;
            bool newest=MatchesSkillRequest(slot,request)&&!_skillSettled[slot];
            bool flightPrediction=slot==1 && Kit is AmihanHeroKit && _motor.PredictedFlightMatches(request);
            bool pendingEffect=PendingEffectMatches(slot,request);
            if(!newest&&!flightPrediction&&!pendingEffect)return false;
            bool deferredReactivation = pendingEffect && _pendingSkillEffects[request].reactivation;
            if (!accepted) _pendingSkillEffects.Remove(request);
            var ability=AbilityFor((Slot)slot);if(ability==null)return false;
            long previousFlight=_motor.PreviousFlightEpisode;
            bool previousFlightWasTerminal=_motor.PreviousFlightWasTerminal;
            if(newest)_skillSettled[slot]=true;
            if(!accepted&&newest)RollBackPredictedCast((Slot)slot,refundResources:false,
                preserveActiveEffect:deferredReactivation);
            if(flightPrediction)
            {
                if(accepted)_motor.ConfirmFlightTakeoff(request);
                else
                {
                    ((AmihanHeroKit)Kit).CancelFeatherfall(_motor);
                    _motor.RestoreRejectedFlightEpisode(previousFlight,previousFlightWasTerminal);
                }
            }
            // Older answers settle only their own prediction, never newer resources.
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
