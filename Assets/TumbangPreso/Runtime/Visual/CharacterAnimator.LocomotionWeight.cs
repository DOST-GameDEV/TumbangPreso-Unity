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
            if (!ordinary || teleported || !_weightSampled) _locomotionLean = Vector2.zero;
            else if (dt > 0)
            {
                Vector3 acceleration = Vector3.ClampMagnitude((velocity - _weightVelocity) / Mathf.Max(.01f, dt), 24);
                var localAcceleration = transform.InverseTransformDirection(acceleration);
                var localVelocity = transform.InverseTransformDirection(velocity);
                float turning = Mathf.Clamp(Mathf.DeltaAngle(_weightYaw, yaw) / Mathf.Max(.01f, dt), -240, 240);
                float carry = _motor.HoldingSlipper ? .72f : 1;
                var target = new Vector2(
                    Mathf.Clamp(localVelocity.z * .55f + localAcceleration.z * .3f, -6, 8),
                    Mathf.Clamp(-localAcceleration.x * .22f - turning * .014f * Mathf.Clamp01(velocity.magnitude / 2), -5, 5)) * carry;
                _locomotionLean = Vector2.Lerp(_locomotionLean, target, 1 - Mathf.Exp(-13 * Mathf.Min(dt, .1f)));
            }
            _weightVelocity = velocity; _weightPosition = position; _weightYaw = yaw; _weightSampled = true;
            if (!ordinary || _locomotionLean.sqrMagnitude < .00001f) return;
            _weightTorsoRest = _weightTorso.localRotation;
            _weightTorso.localRotation = _weightTorsoRest * Quaternion.Euler(_locomotionLean.x, 0, _locomotionLean.y);
            if (_weightHead != null)
            {
                _weightHeadRest = _weightHead.localRotation;
                // Keep the face looking toward play as the chest loads into a turn.
                _weightHead.localRotation = _weightHeadRest * Quaternion.Euler(-_locomotionLean.x * .45f, 0, -_locomotionLean.y * .35f);
            }
            _weightApplied = true;
        }

        private void OnDisable() => ClearLocomotionWeight();
    }
}
