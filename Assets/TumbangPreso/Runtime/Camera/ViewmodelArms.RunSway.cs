using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ THE FIRST-PERSON HANDS MOVE WITH THE FEET.
    ///
    /// 🧑 2026-09-24: *"my biggest issue is where the hands are and what they do when u run"*. Until this,
    /// running changed nothing in the first-person view: the empty hand played the same slow idle breathe
    /// as standing still and the carrying hand was fixed, so a sprint read as gliding with frozen hands.
    ///
    /// Now both hands take the body's gait (`CharacterAnimator.GaitPhase`, so a hand lands with a foot on
    /// every peer's clock the body uses): a bob per footfall, a small sway across, and the EMPTY hand pumps
    /// forward and back against the stride, harder in a sprint.
    ///
    /// ⚠️ THE CARRYING HAND ONLY TRANSLATES. Rotating it swings the held tsinelas in the grip, which is the
    /// *"arms float ... when i run while holding"* report `StepVisuals` already guards against; moving the
    /// whole pivot carries hand and slipper together. Anything authored (a throw, a cast, the charge, an aim
    /// preview, swimming, a climb) owns the hands outright and the sway fades out under it.
    /// </summary>
    public sealed partial class ViewmodelArms
    {
        /// <summary>Metres the hands drop at each footfall, walking and sprinting.</summary>
        public const float WalkStepBob = .010f, RunStepBob = .024f;
        /// <summary>Degrees the empty hand pumps forward and back, walking and sprinting.</summary>
        public const float WalkPumpDegrees = 5f, RunPumpDegrees = 15f;
        /// <summary>Metres the empty hand travels forward and back with the pump.</summary>
        public const float WalkPumpReach = .012f, RunPumpReach = .040f;

        private Visual.CharacterAnimator _gaitAnimator;
        private bool _swayApplied;
        private float _swayBlend;
        private Quaternion _swayLeftBase, _swayRightBase;
        private Vector3 _swayLeftPosition, _swayRightPosition;

        private void RestoreRunSway()
        {
            if (!_swayApplied) return;
            if (_leftPivot != null) { _leftPivot.localRotation = _swayLeftBase; _leftPivot.localPosition = _swayLeftPosition; }
            if (_rightPivot != null) { _rightPivot.localRotation = _swayRightBase; _rightPivot.localPosition = _swayRightPosition; }
            _swayApplied = false;
        }

        private void ApplyRunSway(float dt)
        {
            if (_leftPivot == null || _rightPivot == null || _characterMotor == null) return;
            if (_gaitAnimator == null) _gaitAnimator = _characterMotor.GetComponentInChildren<Visual.CharacterAnimator>();
            if (_gaitAnimator == null) return;
            bool free = _charge < 0 && _clip == null && _actionReturnLeft <= 0 && string.IsNullOrEmpty(_aimPreview)
                && !_characterMotor.IsSwimming && !_characterMotor.IsEdgeRecovering;
            // The body's own arm-swing amount already knows grounded, stunned, fatigued and every authored
            // action; the hands follow it so the two views never disagree about whether she is running.
            float target = free ? _gaitAnimator.LocomotionArmAmount : 0f;
            _swayBlend = target < _swayBlend && !free ? 0f : Mathf.MoveTowards(_swayBlend, target, Mathf.Max(0, dt) / .15f);
            if (_swayBlend <= .001f) return;

            float run = _gaitAnimator.GaitRunWeight;
            float phase = _gaitAnimator.GaitPhase * Mathf.PI * 2f;
            // Two footfalls per cycle: the hands dip at each contact and ride up between them.
            float bob = -Mathf.Abs(Mathf.Sin(phase)) * Mathf.Lerp(WalkStepBob, RunStepBob, run);
            float across = Mathf.Cos(phase) * Mathf.Lerp(.004f, .009f, run);
            float pump = Mathf.Sin(phase);
            float pumpDegrees = Mathf.Lerp(WalkPumpDegrees, RunPumpDegrees, run);
            float pumpReach = Mathf.Lerp(WalkPumpReach, RunPumpReach, run);

            _swayLeftBase = _leftPivot.localRotation; _swayLeftPosition = _leftPivot.localPosition;
            _swayRightBase = _rightPivot.localRotation; _swayRightPosition = _rightPivot.localPosition;
            _swayApplied = true;

            float k = _swayBlend;
            _leftPivot.localPosition += new Vector3(across, bob, pump * pumpReach) * k;
            _leftPivot.localRotation *= Quaternion.Euler(-pump * pumpDegrees * k, 0f, 0f);
            if (_carrying)
            {
                _rightPivot.localPosition += new Vector3(across, bob * .8f, 0f) * k;
            }
            else
            {
                _rightPivot.localPosition += new Vector3(across, bob, -pump * pumpReach) * k;
                _rightPivot.localRotation *= Quaternion.Euler(pump * pumpDegrees * k, 0f, 0f);
            }
        }
    }
}
