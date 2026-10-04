using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    // A short, bounded meeting of two capsules. It supplies velocity to each
    // locally simulated motor; CharacterController.Move still resolves obstacles.
    public sealed class PaetePlayerPull : MonoBehaviour
    {
        private CharacterMotor _caster, _target;
        private Vector3 _casterEnd, _targetEnd;
        private Vector3 _casterLast, _targetLast;
        private int _casterEpoch, _targetEpoch, _round;
        private long _match;
        private float _age, _gap, _casterSpeed, _targetSpeed, _duration;
        private bool _stopped, _casterRole, _targetRole;
        private HeroKit _casterKit, _targetKit;
        private float _casterStalled, _targetStalled;
        private PaeteVineReach _visual;
        private bool _publishEnd;
        public Vector3 CasterEnd => _casterEnd;
        public Vector3 TargetEnd => _targetEnd;
        public float Duration => _duration;
        public bool Active => !_stopped;
        public string EndReason { get; private set; }
        public CharacterMotor Caster => _caster;
        public CharacterMotor Target => _target;

        public static PaetePlayerPull Begin(CharacterMotor caster, CharacterMotor target)
        {
            if (!NetAuthority.ShouldResolve() || !Eligible(caster) || !Eligible(target) || caster == target
                || !caster.CanAcceptPaetePull || !target.CanAcceptPaetePull) return null;
            Vector3 delta = target.transform.position-caster.transform.position; delta.y=0;
            float gap = caster.PaeteBodyRadius+target.PaeteBodyRadius+.08f;
            if (!PaeteRules.VinePairTravel(delta.magnitude,gap,out float a,out float b)) return null;
            b*=target.PaetePullDistanceScale; a=Mathf.Max(0,delta.magnitude-gap-b);
            var go = new GameObject("Paete player vine pull");
            var pull = go.AddComponent<PaetePlayerPull>();
            pull._caster=caster; pull._target=target; pull._gap=gap;
            pull._casterKit=caster.AbilitySystem?.Kit;pull._targetKit=target.AbilitySystem?.Kit;
            pull._casterRole=caster.IsDefender;pull._targetRole=target.IsDefender;
            Vector3 direction=delta.normalized;
            pull._casterEnd=caster.transform.position+direction*a;
            pull._targetEnd=target.transform.position-direction*b;
            float travel=Mathf.Max(.12f,a/PaeteRules.VineReelSpeed);
            pull._casterSpeed=a/travel; pull._targetSpeed=b/travel;
            pull._duration=PaeteRules.VineReachSeconds+travel+.3f;
            pull._casterEpoch=caster.MovementEpoch; pull._targetEpoch=target.MovementEpoch;
            pull._round=GameServices.Match?.RoundNumber??0;
            pull._match=GameServices.Match?.PresentationMatchId??0;
            pull._casterLast=caster.transform.position;pull._targetLast=target.transform.position;
            caster.AttachPaetePull(pull);target.AttachPaetePull(pull);
            return pull;
        }
        public void BindVisual(PaeteVineReach visual, bool publishEnd)
        { _visual=visual; _publishEnd=publishEnd; }
        public static PaetePlayerPull Restore(CharacterMotor caster,CharacterMotor target,PaeteVineState state,float age)
        {
            if(NetAuthority.IsHost||!state.IsValid||age>=state.Duration||!Eligible(caster)||!Eligible(target)
                ||!caster.CanAcceptPaetePull||!target.CanAcceptPaetePull)return null;
            var pull=new GameObject("Paete player vine pull").AddComponent<PaetePlayerPull>();
            pull._caster=caster;pull._target=target;
            pull._casterEnd=state.CasterEnd;pull._targetEnd=state.TargetEnd;
            pull._gap=caster.PaeteBodyRadius+target.PaeteBodyRadius+.08f;
            pull._duration=state.Duration;pull._age=Mathf.Max(0,age);
            float remaining=Mathf.Max(.02f,state.Duration-Mathf.Max(PaeteRules.VineReachSeconds,age)-.3f);
            pull._casterSpeed=Mathf.Min(PaeteRules.VineReelSpeed,Flat(state.CasterEnd-caster.transform.position).magnitude/remaining);
            pull._targetSpeed=Mathf.Min(PaeteRules.VineReelSpeed,Flat(state.TargetEnd-target.transform.position).magnitude/remaining);
            pull._casterEpoch=caster.MovementEpoch;pull._targetEpoch=target.MovementEpoch;
            pull._round=state.Scope.Round;pull._match=state.Scope.Match;
            pull._casterKit=caster.AbilitySystem?.Kit;pull._targetKit=target.AbilitySystem?.Kit;
            pull._casterRole=caster.IsDefender;pull._targetRole=target.IsDefender;
            pull._casterLast=caster.transform.position;pull._targetLast=target.transform.position;
            caster.AttachPaetePull(pull);target.AttachPaetePull(pull);
            return pull;
        }
        private static bool Eligible(CharacterMotor who) => who!=null && who.isActiveAndEnabled
            && who.gameObject.activeInHierarchy && who.RoundActive && !who.IsTagged
            && !who.IsStunned && !who.IsRooted && !who.IsFlying
            && who.AbilitySystem?.IsImmuneToStuns!=true;
        private void FixedUpdate()
        {
            if(_stopped)return;
            if(MatchAbandon.AuthorityRevoked||!Eligible(_caster)||!Eligible(_target)
                ||_caster.AbilitySystem?.Kit!=_casterKit||_target.AbilitySystem?.Kit!=_targetKit
                ||_caster.IsDefender!=_casterRole||_target.IsDefender!=_targetRole
                ||_caster.MovementEpoch!=_casterEpoch||_target.MovementEpoch!=_targetEpoch
                ||(GameServices.Match?.RoundNumber??0)!=_round
                ||(GameServices.Match?.PresentationMatchId??0)!=_match)
            { Stop("interrupted");return; }
            _age+=Time.fixedDeltaTime;
            if(_age>_duration){Stop("travel complete");return;}
            var separation=Flat(_target.transform.position-_caster.transform.position);
            if(separation.magnitude<=_gap+.025f){Stop("contact");return;}
            if(_age>PaeteRules.VineReachSeconds+.12f)
            {
                _casterStalled=Flat(_casterEnd-_caster.transform.position).magnitude>.05f
                    &&Flat(_caster.transform.position-_casterLast).magnitude<.001f?_casterStalled+Time.fixedDeltaTime:0;
                _targetStalled=Flat(_targetEnd-_target.transform.position).magnitude>.05f
                    &&Flat(_target.transform.position-_targetLast).magnitude<.001f?_targetStalled+Time.fixedDeltaTime:0;
                if(_casterStalled>.16f||_targetStalled>.16f){Stop("blocked");return;}
            }
            _casterLast=_caster.transform.position;_targetLast=_target.transform.position;
        }
        internal bool VelocityFor(CharacterMotor actor,float dt,out Vector3 velocity)
        {
            velocity=Vector3.zero;
            if(_stopped||dt<=0||actor!=_caster&&actor!=_target)return false;
            if(!Eligible(_caster)||!Eligible(_target)){Stop("interrupted");return false;}
            if(_age<PaeteRules.VineReachSeconds)return true;
            bool owner=actor==_caster;
            Vector3 delta=Flat((owner?_casterEnd:_targetEnd)-actor.transform.position);
            Vector3 other=Flat((owner?_target:_caster).transform.position-actor.transform.position);
            if(delta.sqrMagnitude<.0001f)return true;
            float clearance=Mathf.Max(0,Vector3.Dot(other,delta.normalized)-_gap);
            float ramp=Mathf.SmoothStep(0,1,(_age-PaeteRules.VineReachSeconds)/.10f);
            float step=Mathf.Min(Mathf.Min(delta.magnitude,clearance),(owner?_casterSpeed:_targetSpeed)*ramp*dt);
            velocity=delta.normalized*(step/dt);
            return true;
        }
        public void Stop(string reason)
        {
            if(_stopped)return;
            _stopped=true;EndReason=reason;
            if(reason=="contact"&&_caster!=null&&_target!=null)
            {
                Vector3 direction=Flat(_target.transform.position-_caster.transform.position).normalized;
                _caster.GetComponentInChildren<CharacterSquashStretch>()?.Impact(-direction,.08f);
                _target.GetComponentInChildren<CharacterSquashStretch>()?.Impact(direction,.08f);
            }
            _visual?.ReturnNow();
            if(_publishEnd&&NetAuthority.ShouldResolve())MatchRpc.Instance?.EndPaeteVine(_caster.PlayerSlot,_casterEpoch,_targetEpoch);
            if(_caster!=null)_caster.ReleasePaetePull(this);if(_target!=null)_target.ReleasePaetePull(this);
            Destroy(gameObject);
        }
        private void OnDestroy()
        { if(_caster!=null)_caster.ReleasePaetePull(this);if(_target!=null)_target.ReleasePaetePull(this); }
        private static Vector3 Flat(Vector3 v) => new Vector3(v.x,0,v.z);
    }
}
