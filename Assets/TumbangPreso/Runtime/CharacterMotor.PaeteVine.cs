using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso
{
    public sealed partial class CharacterMotor
    {
        private PaetePlayerPull _paetePull;
        internal float PaeteBodyRadius => _cc != null ? _cc.radius*Mathf.Max(transform.lossyScale.x,transform.lossyScale.z) : .35f;
        internal float PaetePullDistanceScale => IncomingKnockbackSpeedScale*IncomingKnockbackSpeedScale;
        internal bool CanAcceptPaetePull => _paetePull == null && !IsCarried && _externalVelocity.sqrMagnitude < .01f;
        internal void AttachPaetePull(PaetePlayerPull pull) { _paetePull=pull; }
        internal void ReleasePaetePull(PaetePlayerPull pull)
        {
            if(_paetePull!=pull)return;
            _paetePull=null;
            // This constraint never owns an impact/carry. Preserve any newer one.
            if(IsLocallySimulated()){_velocity.x=0;_velocity.z=0;}
        }
        private bool PaetePullVelocity(float dt,out Vector3 velocity)
        {
            velocity=Vector3.zero;
            if(_paetePull==null)return false;
            if(_externalVelocity.sqrMagnitude>.01f||IsCarried)
            { _paetePull.Stop("new impact");return false; }
            return _paetePull.VelocityFor(this,dt,out velocity);
        }
    }
}
