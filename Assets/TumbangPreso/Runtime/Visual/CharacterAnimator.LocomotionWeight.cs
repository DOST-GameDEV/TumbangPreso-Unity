using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class CharacterAnimator
    {
        private Transform _weightTorso, _weightHead;
        private Quaternion _weightTorsoRest, _weightHeadRest;
        private Vector3 _weightVelocity, _weightPosition;
        private Vector2 _locomotionLean;
        private float _weightYaw;
        private bool _weightSampled, _weightApplied, _weightBonesResolved;

        // Read-only diagnostics describe the drawn body, never input or hitbox state.
        public Vector2 LocomotionLean => _locomotionLean;

        /// <summary>
        /// ⚠️ THE GAIT'S OWN LEAN AND WEIGHT SHIFT (owner, 2026-09-26: *"thihs walk still sucks pls imrpove still on all"*).
        /// The lean above answers acceleration and turning only, so a body walking at a steady pace stood bolt upright with its
        /// chest square over its hips all the way through the stride. A walker leans into the travel a little and a runner a lot,
        /// and the chest rolls over whichever leg is bearing the weight (`CharacterAnimator.StanceSide`, peaking at mid-stance),
        /// less at a run where the feet land closer to the line. The head gives back part of both so the face stays level and
        /// readable. Degrees; both ride the arm layer's swing amount, so they arrive and leave with the walk.
        /// </summary>
        public const float WalkGaitLeanDegrees = 4f, RunGaitLeanDegrees = 9f;
        public const float WalkWeightRollDegrees = 3f, RunWeightRollDegrees = 1.5f;

        private void RestoreLocomotionWeight()
        {
            if (!_weightApplied) return;
            if (_weightTorso != null) _weightTorso.localRotation = _weightTorsoRest;
            if (_weightHead != null) _weightHead.localRotation = _weightHeadRest;
            _weightApplied = false;
        }

        private void ClearLocomotionWeight()
        {
            RestoreLocomotionWeight();
            _weightTorso = _weightHead = null;
            _locomotionLean = Vector2.zero;
            _weightSampled = _weightBonesResolved = false;
        }

        private void ApplyLocomotionWeight()
        {
            if (_motor == null || _animator == null || !_graph.IsValid()) return;
            if (!_weightBonesResolved)
            {
                foreach (var bone in _animator.GetComponentsInChildren<Transform>(true))
                {
                    if (bone.name == "torso") _weightTorso = bone;
                    else if (bone.name == "head") _weightHead = bone;
                }
                _weightBonesResolved = true;
            }
            if (_weightTorso == null) return;

            float dt = Time.deltaTime;
            Vector3 velocity = _motor.Velocity; velocity.y = 0;
            Vector3 position = _motor.transform.position;
            float yaw = _motor.transform.eulerAngles.y;
            bool teleported = _weightSampled && (position - _weightPosition).sqrMagnitude > 4;
            bool ordinary = _motor.IsGrounded && !_motor.IsSwimming && !_motor.IsStunned
                && !_motor.IsTripped && !_motor.Stamina.IsFatigued && _oneShotLeft <= 0
                && !_chargePosing && _throwReleaseTime < 0 && _throwCancelTime < 0
                && _introductionBones == null && (_emote == null || !_emote.IsEmoting)
                && (_carrier == null || _carrier.ChannelRatio <= 0);

            // Never bend an authored cast/recovery or manufacture a braking pose from
            // a teleport. Only the torso/head move; root, legs and collision stay exact.
            if (!ordinary || teleported || !_weightSampled)
            { _locomotionLean = Vector2.zero; _weightVelocity = velocity; }
            else if (dt > 0)
            {
                // Physics velocity changes only on fixed ticks. Filtering the measured
                // velocity spreads that impulse over real time instead of losing almost
                // all of it in one 500-fps render sample (or amplifying a slow frame).
                Vector3 observed=Vector3.Lerp(_weightVelocity,velocity,1-Mathf.Exp(-18*Mathf.Min(dt,.1f)));
                Vector3 acceleration = Vector3.ClampMagnitude((observed - _weightVelocity) / Mathf.Max(.0001f, dt), 24);
                _weightVelocity=observed;
                var localAcceleration = transform.InverseTransformDirection(acceleration);
                var localVelocity = transform.InverseTransformDirection(velocity);
                float turning = Mathf.Clamp(Mathf.DeltaAngle(_weightYaw, yaw) / Mathf.Max(.01f, dt), -240, 240);
                float carry = _motor.HoldingSlipper ? .72f : 1;
                var target = new Vector2(
                    Mathf.Clamp(localVelocity.z * .55f + localAcceleration.z * .3f, -6, 8),
                    Mathf.Clamp(-localAcceleration.x * .22f - turning * .014f * Mathf.Clamp01(velocity.magnitude / 2), -5, 5)) * carry;
                _locomotionLean = Vector2.Lerp(_locomotionLean, target, 1 - Mathf.Exp(-13 * Mathf.Min(dt, .1f)));
            }
            _weightPosition = position; _weightYaw = yaw; _weightSampled = true;
            // The gait lean and weight roll (see `WalkGaitLeanDegrees`), last frame's arm-layer amount and stance.
            float gait = ordinary ? _armSwingAmount : 0f;
            float gaitLean = Mathf.Lerp(WalkGaitLeanDegrees, RunGaitLeanDegrees, _runWeight) * gait * _gaitTravelDirection;
            float roll = Mathf.Lerp(WalkWeightRollDegrees, RunWeightRollDegrees, _runWeight) * StanceSide;
            bool gaitPose = Mathf.Abs(gaitLean) > .01f || Mathf.Abs(roll) > .01f;
            if (!ordinary || (_locomotionLean.sqrMagnitude < .00001f && !gaitPose)) return;
            _weightTorsoRest = _weightTorso.localRotation;
            _weightTorso.localRotation = _weightTorsoRest * Quaternion.Euler(_locomotionLean.x, 0, _locomotionLean.y);
            // In the character's frame: pitch into the travel about its right axis; roll the chest's top toward the stance leg
            // (+StanceSide is the LEFT leg, the character's -x; a positive turn about +forward tips +up toward -x).
            _weightTorso.rotation = Quaternion.AngleAxis(gaitLean, transform.right) * Quaternion.AngleAxis(roll, transform.forward) * _weightTorso.rotation;
            if (_weightHead != null)
            {
                _weightHeadRest = _weightHead.localRotation;
                // Keep the face looking toward play as the chest loads into a turn.
                _weightHead.localRotation = _weightHeadRest * Quaternion.Euler(-_locomotionLean.x * .45f, 0, -_locomotionLean.y * .35f);
                _weightHead.rotation = Quaternion.AngleAxis(-gaitLean * .5f, transform.right) * Quaternion.AngleAxis(-roll * .7f, transform.forward) * _weightHead.rotation;
            }
            _weightApplied = true;
        }

        private void OnDisable() { ClearLocomotionArms(); ClearLocomotionWeight(); }
    }
}
