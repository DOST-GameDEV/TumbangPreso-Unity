using UnityEngine;

namespace TumbangPreso.Visual
{
    // Ordinary grounded travel, heard at the moving body on each peer. No relay:
    // replicas already present that same travel and a relay would double each step.
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class MotionFoley : MonoBehaviour
    {
        private CharacterMotor _motor;
        private CharacterAnimator _animator;
        private Vector3 _last;
        private float _distance;
        private bool _sampled;
        private bool _left;
        private int _teleport;
        private float _lastSkid=-100;
        private CombatVerbs _verbs;
        private void Awake() { _motor=GetComponent<CharacterMotor>(); _animator=GetComponent<CharacterAnimator>(); }
        private void OnEnable() { _sampled=false; _distance=0; }
        private void LateUpdate()
        {
            if (_animator == null) _animator=GetComponent<CharacterAnimator>();
            Vector3 now=transform.position;
            float travel=_sampled ? new Vector2(now.x-_last.x,now.z-_last.z).magnitude : 0;
            if(_verbs==null)_verbs=GetComponent<CombatVerbs>();
            bool sliding=(_verbs!=null && _verbs.SlideActive) || (_animator!=null && _animator.IsPlayingAction && _animator.CurrentClipName=="slide");
            if(_sampled && _teleport==_motor.PresentationTeleportSerial && travel>.005f && travel<.8f &&
                _motor.IsGrounded && _motor.RoundActive && sliding && Time.time-_lastSkid>.3f && WorldCueProfile.Current.InkEffects>0 &&
                TryChalkCrossing(_last,now,out var chalk))
            {
                _lastSkid=Time.time;
                // Each peer already observes the motion. Do not relay a second
                // sound or claim a pickup; the local cue supplies replay timing.
                var skidRandom=Random.state;
                try{GameServices.Audio?.PlayAtVaried("court_skid",chalk,1,1,.16f);}
                finally{Random.state=skidRandom;}
            }
            _teleport=_motor.PresentationTeleportSerial;
            _last=now;_sampled=true;
            float speed=new Vector2(_motor.Velocity.x,_motor.Velocity.z).magnitude;
            bool swimming=_motor.IsSwimming;
            bool powered=_motor.AbilitySystem?.Kit?.Skill1?.IsActive ?? false;
            if ((!_motor.IsGrounded && !swimming) || _motor.IsStunned || !_motor.IsPerson || powered
                || (_animator != null && _animator.IsPlayingAction) || speed<.4f || travel>.8f)
            { _distance=0;return; }
            _distance+=travel;
            // One quiet accent per cycle avoids a wall of taps from four runners.
            float stride=swimming ? 2.3f : _animator!=null ? Mathf.Max(.3f,_animator.FootfallCycleMetres) : 1f;
            if (_distance<stride)return;
            _distance%=stride;_left=!_left;
            // Quiet, short rubber contact. It cannot play from a parked or teleported
            // body, and never fabricates an approaching-enemy proximity warning.
            var randomState=Random.state;
            string cue="step_rubber";Vector3 contact=now+transform.right*(_left?-.11f:.11f);
            if(swimming)
            {cue="sfx_swim_stroke";if(RooftopPool.TrySurface(now,out float water))contact.y=water;}
            else if(LagoonWater.Instance!=null&&LagoonWater.Instance.Active
                &&Physics.Raycast(now+Vector3.up*.25f,Vector3.down,out var hit,.60f,~0,QueryTriggerInteraction.Ignore))
            {
                string surface=hit.collider.name.ToLowerInvariant();
                if(surface.Contains("deck")||surface.Contains("step")||surface.Contains("rail"))cue="sfx_step_deck";
            }
            try { GameServices.Audio?.PlayAtVaried(cue,contact,.96f,1.04f,swimming?.65f:.9f); }
            finally { Random.state=randomState; } // Cosmetic contacts must not advance the AI's random stream.
        }
        public static bool TryChalkCrossing(Vector3 from,Vector3 to,out Vector3 at)
        {
            float radius=Core.Balance.ConfinementRadius,first=2;at=to;
            for(int axis=0;axis<2;axis++)for(int side=-1;side<=1;side+=2)
            {
                float a=axis==0?from.x:from.z,b=axis==0?to.x:to.z;
                if(Mathf.Abs(b-a)<.00001f)continue;
                float t=(side*radius-a)/(b-a);
                if(t<=0 || t>1 || t>=first)continue;
                var point=Vector3.Lerp(from,to,t);float other=axis==0?point.z:point.x;
                if(Mathf.Abs(other)>radius+.12f)continue;
                first=t;at=point;
            }
            return first<=1;
        }
    }
}
