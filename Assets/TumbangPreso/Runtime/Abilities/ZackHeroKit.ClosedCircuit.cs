using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed partial class ZackHeroKit
    {
        private ClosedCircuitAbility Circuit => (ClosedCircuitAbility)DefendingSkill;
        public bool CircuitSequenceActive => Circuit.Phase != CircuitPhase.Idle;
        public CircuitPhase CircuitStage => Circuit.Phase;
        public int CircuitTarget => Circuit.Target;
        public float CircuitRemaining => Circuit.DurationRemaining;
        public long CircuitEpisode => Circuit.Episode;
        public ZackCircuitState CaptureCircuit() => Circuit.Capture();
        public bool RestoreCircuit(CharacterMotor motor,ZackCircuitState state,float age)
            => Circuit.Restore(motor,state,age);
        public bool ReceiveCircuitAim(CharacterMotor motor,ZackCircuitAim aim)
            => Circuit.ReceiveAim(motor,aim);

        private sealed class ClosedCircuitAbility : HeroAbility
        {
            public const float Range=6,LockSeconds=.4f,FollowupSeconds=1,AimFreshSeconds=.25f;
            public override AbilityNetworkMode NetworkMode=>AbilityNetworkMode.HostConfirmed;
            public override bool CanReactivate=>true;
            public override bool ReactivateReady=>Phase==CircuitPhase.Followup && DurationRemaining>0;
            public CircuitPhase Phase {get;private set;}
            public int Target {get;private set;}=-1;
            public long Episode {get;private set;}
            private int _first=-1;
            private bool _second,_dirty;
            private long _aimSequence,_sentAimSequence;
            private Vector3 _remoteAim;
            private float _aimClock,_nextAim;
            private readonly ZackHeroKit _kit;
            private readonly RaycastHit[] _hits=new RaycastHit[32];
            public ClosedCircuitAbility(ZackHeroKit kit)
                :base("zack_skill2d","CLOSED CIRCUIT",
                    "Keep a visible rival within six metres in your aim for 0.4 seconds to Zap them for two seconds. Losing aim or sight cancels. Overclock offers one different target within one second.",
                    35,FollowupSeconds,AbilityGlyph.ZackOvercharge,
                    summary:"Maintain a visible lock; Zapped blocks abilities only.",
                    castAction:"hero-zack-circuit",viewmodelAction:"closed-circuit",castCue:"") { _kit=kit; }

            public override bool CanActivate(AbilityContext ctx)
                =>base.CanActivate(ctx) && Phase==CircuitPhase.Idle && ctx.Motor.IsDefender
                    && ctx.Round?.RoundActive==true && Select(ctx,ctx.AimPoint,-1)!=null;
            public override void Activate(AbilityContext ctx)
            { _second=false;_first=-1;base.Activate(ctx);CooldownRemaining=0; }
            public override void Reactivate(AbilityContext ctx)
            {
                if(ctx?.Motor==null || (!ctx.IsApprovedReplay && !ReactivateReady))return;
                float cooldown=CooldownRemaining;
                _second=true;Begin(ctx);CooldownRemaining=cooldown;
            }
            protected override void OnActivate(AbilityContext ctx)=>Begin(ctx);
            private void Begin(AbilityContext ctx)
            {
                if(ctx?.Motor==null)return;
                var target=Select(ctx,ctx.AimPoint,_second?_first:-1);
                if(target==null){Clear();return;}
                Episode++;Target=target.PlayerSlot;Phase=CircuitPhase.Acquiring;
                DurationRemaining=LockSeconds;_remoteAim=ctx.AimPoint;
                _aimClock=ctx.Round.TimeLeft;_aimSequence=0;_nextAim=0;_dirty=true;
                Visual.ZackCircuitTell.Ensure(ctx.Motor,_kit);
            }
            private CharacterMotor Select(AbilityContext ctx,Vector3 aim,int excluded)
            {
                if(ctx?.Round==null)return null;
                CharacterMotor best=null;float nearest=float.PositiveInfinity;
                foreach(var target in ctx.Round.Players)
                {
                    if(target==null || target.PlayerSlot==excluded || !Eligible(ctx,target,aim))continue;
                    float distance=(target.transform.position-ctx.Position).sqrMagnitude;
                    if(distance<nearest){nearest=distance;best=target;}
                }
                return best;
            }
            private bool Eligible(AbilityContext ctx,CharacterMotor target,Vector3 aim)
            {
                if(target==null || target==ctx.Motor || target.PlayerSlot<0 || target.PlayerSlot>=Balance.PlayerCount
                    || target.IsDefender || target.IsTagged || !target.gameObject.activeInHierarchy)return false;
                var capsule=target.GetComponent<CharacterController>();
                var own=ctx.Motor.GetComponent<CharacterController>();
                Vector3 origin=ctx.Position+Vector3.up*(own!=null?own.height*.78f:1.25f);
                Vector3 centre=target.transform.position+(capsule!=null?capsule.center:Vector3.up*.8f);
                Vector3 delta=centre-origin,ray=aim-origin;
                if(!Finite(ray) || ray.sqrMagnitude<.0001f || delta.sqrMagnitude>Range*Range)return false;
                if(Vector3.Dot(ctx.Forward,Vector3.ProjectOnPlane(delta,Vector3.up))<=0)return false;
                ray.Normalize();float along=Vector3.Dot(delta,ray);
                float radius=(capsule!=null?capsule.radius:.4f)+.15f;
                if(along<=0 || (delta-ray*along).sqrMagnitude>radius*radius)return false;
                int count=Physics.RaycastNonAlloc(origin,delta.normalized,_hits,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
                if(count==_hits.Length)return false;
                for(int i=0;i<count;i++)
                {
                    var collider=_hits[i].collider;var body=collider.GetComponentInParent<CharacterMotor>();
                    if(body==ctx.Motor || body==target || collider.GetComponentInParent<Slipper>()!=null)continue;
                    return false;
                }
                return true;
            }
            private static bool Finite(Vector3 value)=>float.IsFinite(value.x)&&float.IsFinite(value.y)&&float.IsFinite(value.z);
            private static bool RemoteOwner(CharacterMotor motor)=>NetAuthority.IsNetworked && NetAuthority.IsHost
                && !motor.IsBot && motor.PlayerSlot!=NetAuthority.LocalSlot;
            protected override void OnTick(AbilityContext ctx,float dt)
            {
                if(!NetAuthority.ShouldResolve() || dt<=0)return;
                if(ctx?.Motor==null || ctx.Round?.RoundActive!=true || !ctx.Motor.IsDefender
                    || !ctx.Motor.CanAct() || ctx.Motor.IsZapped){Clear();return;}
                if(Phase!=CircuitPhase.Acquiring)return;
                Vector3 aim=RemoteOwner(ctx.Motor)?_remoteAim:ctx.AimPoint;
                if(RemoteOwner(ctx.Motor) && _aimClock-ctx.Round.TimeLeft>AimFreshSeconds){Clear();return;}
                var target=ctx.Round.PlayerAt(Target);
                if(!Eligible(ctx,target,aim)){Clear();return;}
                if(DurationRemaining>0)return;
                if(NetAuthority.ShouldResolve())target.ApplyZapped(2);
                if(!_second)CooldownRemaining=Cooldown;
                if(!_second && _kit.IsOverclocked && target.IsZapped)
                { _first=Target;Target=-1;Phase=CircuitPhase.Followup;DurationRemaining=FollowupSeconds;_dirty=true; }
                else Clear();
            }
            public override void Tick(AbilityContext ctx,float dt)
            {
                base.Tick(ctx,dt);
                if(ctx?.Motor==null)return;
                if(NetAuthority.IsNetworked && !NetAuthority.IsHost && ctx.Motor.PlayerSlot==NetAuthority.LocalSlot
                    && Phase==CircuitPhase.Acquiring && dt>0 && Time.unscaledTime>=_nextAim)
                {
                    _nextAim=Time.unscaledTime+.05f;
                    MatchRpc.Instance?.SendCircuitAim(ctx.Motor.PlayerSlot,Episode,++_sentAimSequence,ctx.AimPoint);
                }
                // Tick precedes input service, so this is after the accepted cast broadcast.
                if(_dirty && NetAuthority.ShouldResolve())
                { _dirty=false;MatchRpc.Instance?.BroadcastCircuitState(ctx.Motor.PlayerSlot); }
            }
            private void Clear()
            {
                bool changed=Phase!=CircuitPhase.Idle || Target!=-1 || _first!=-1;
                Phase=CircuitPhase.Idle;Target=_first=-1;_second=false;DurationRemaining=0;
                _dirty|=changed;
            }
            protected override void OnEnd(AbilityContext ctx)=>Clear();
            protected override void OnCancelled(AbilityContext ctx)=>Clear();
            public override void Reset(){base.Reset();Clear();_aimSequence=0;}
            public bool ReceiveAim(CharacterMotor motor,ZackCircuitAim aim)
            {
                if(!NetAuthority.IsHost || motor==null || !aim.IsValid || Phase!=CircuitPhase.Acquiring
                    || aim.Episode!=Episode || aim.Sequence<=_aimSequence || aim.Seat!=motor.PlayerSlot
                    || (aim.Point-motor.transform.position).sqrMagnitude>80*80 || GameServices.Round?.RoundActive!=true)return false;
                _aimSequence=aim.Sequence;_remoteAim=aim.Point;_aimClock=GameServices.Round.TimeLeft;return true;
            }
            public ZackCircuitState Capture()=>new ZackCircuitState { Phase=Phase,Target=Target,FirstTarget=_first,
                Episode=Episode,Remaining=DurationRemaining,Cooldown=CooldownRemaining,Second=_second };
            public bool Restore(CharacterMotor motor,ZackCircuitState state,float age)
            {
                if(NetAuthority.IsHost || motor==null || !state.IsValid || !float.IsFinite(age) || age<0)return false;
                Episode=state.Episode;Phase=state.Phase;Target=state.Target;_first=state.FirstTarget;_second=state.Second;
                DurationRemaining=Mathf.Max(0,state.Remaining-age);
                CooldownRemaining=Mathf.Max(0,state.Cooldown-age*OverheadPassWindow.CooldownRate);
                _dirty=false;
                if(DurationRemaining<=0)Clear();
                else Visual.ZackCircuitTell.Ensure(motor,_kit);
                return true;
            }
        }
    }
}
