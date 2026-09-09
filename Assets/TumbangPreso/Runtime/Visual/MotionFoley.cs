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
        private void Awake() { _motor=GetComponent<CharacterMotor>(); _animator=GetComponent<CharacterAnimator>(); }
        private void OnEnable() { _sampled=false; _distance=0; }
        private void LateUpdate()
        {
            if (_animator == null) _animator=GetComponent<CharacterAnimator>();
            Vector3 now=transform.position;
            float travel=_sampled ? new Vector2(now.x-_last.x,now.z-_last.z).magnitude : 0;
            _last=now;_sampled=true;
            float speed=new Vector2(_motor.Velocity.x,_motor.Velocity.z).magnitude;
            bool powered=_motor.AbilitySystem?.Kit?.Skill1?.IsActive ?? false;
            if (!_motor.IsGrounded || _motor.IsStunned || !_motor.IsPerson || powered
                || (_animator != null && _animator.IsPlayingAction) || speed<.4f || travel>.8f)
            { _distance=0;return; }
            _distance+=travel;
            // One quiet accent per cycle avoids a wall of taps from four runners.
            float stride=_animator!=null ? Mathf.Max(.3f,_animator.FootfallCycleMetres) : 1f;
            if (_distance<stride)return;
            _distance%=stride;_left=!_left;
            // Quiet, short rubber contact. It cannot play from a parked or teleported
            // body, and never fabricates an approaching-enemy proximity warning.
            var randomState=Random.state;
            try { GameServices.Audio?.PlayAtVaried("step_rubber",now+transform.right*(_left?-.11f:.11f),.96f,1.04f,.9f); }
            finally { Random.state=randomState; } // Cosmetic contacts must not advance the AI's random stream.
        }
    }
}
