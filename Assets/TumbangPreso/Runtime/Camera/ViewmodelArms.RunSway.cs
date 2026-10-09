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
        /// <summary>Amihan's floating run: a round glide of the hands, and a small pump.</summary>
        public const float AmihanGlideBob = .012f, AmihanRunPumpDegrees = 6f, AmihanRunPumpReach = .016f;

        /// <summary>How much more the natural (live-model) arms bob, sway and pump than the block arms.</summary>
        public const float NaturalSwayGain = 2.2f;

        private Visual.CharacterAnimator _gaitAnimator;
        private bool _swayApplied;
        private float _swayBlend;
        private Quaternion _swayLeftBase, _swayRightBase;
        private Vector3 _swayLeftPosition, _swayRightPosition;
        /// <summary>What the stride added to each pivot this frame (`HandLife` takes it off and puts it back).</summary>
        private Vector3 _swayLeftOffset, _swayRightOffset;
        private Quaternion _swayLeftTurn = Quaternion.identity, _swayRightTurn = Quaternion.identity;

        private void RestoreRunSway()
        {
            if (!_swayApplied) return;
            if (_leftPivot != null) { _leftPivot.localRotation = _swayLeftBase; _leftPivot.localPosition = _swayLeftPosition; }
            if (_rightPivot != null) { _rightPivot.localRotation = _swayRightBase; _rightPivot.localPosition = _swayRightPosition; }
            _swayApplied = false;
        }

        private void ApplyRunSway(float dt)
        {
            if (_leftPivot == null || _rightPivot == null) return;
            bool free; float target, run, phase; bool glides = false;
            if (_characterMotor == null)
            {
                // No body (a probe): the same sway, from the stride the probe plays (`HandLife.ProbeMood`), so a render
                // shows the hands walking and whatever rides them being carried.
                if (!ProbeMood.HasValue) return;
                var mood = ProbeMood.Value;
                free = mood.Free && mood.Grounded && !mood.Tagged && _clip == null;
                target = free ? Mathf.Clamp01(mood.Walk) : 0f; run = mood.Run; phase = mood.GaitPhase * Mathf.PI * 2f;
            }
            else
            {
                if (_gaitAnimator == null) _gaitAnimator = _characterMotor.GetComponentInChildren<Visual.CharacterAnimator>();
                if (_gaitAnimator == null) return;
                free = _charge < 0 && _clip == null && _actionReturnLeft <= 0 && string.IsNullOrEmpty(_aimPreview)
                    && !_characterMotor.IsSwimming && !_characterMotor.IsEdgeRecovering;
                // The body's own arm-swing amount already knows grounded, stunned, fatigued and every authored
                // action; the hands follow it so the two views never disagree about whether she is running.
                target = free ? _gaitAnimator.LocomotionArmAmount : 0f;
                run = _gaitAnimator.GaitRunWeight; phase = _gaitAnimator.GaitPhase * Mathf.PI * 2f;
                glides = _gaitAnimator.Style == Visual.GaitStyles.Amihan;
            }
            _swayBlend = target < _swayBlend && !free ? 0f : Mathf.MoveTowards(_swayBlend, target, Mathf.Max(0, dt) / .15f);
            if (_swayBlend <= .001f) return;

            // Two footfalls per cycle: the hands dip at each contact and ride up between them.
            float bob = -Mathf.Abs(Mathf.Sin(phase)) * Mathf.Lerp(WalkStepBob, RunStepBob, run);
            float across = Mathf.Cos(phase) * Mathf.Lerp(.004f, .009f, run);
            float pump = Mathf.Sin(phase);
            float pumpDegrees = Mathf.Lerp(WalkPumpDegrees, RunPumpDegrees, run);
            float pumpReach = Mathf.Lerp(WalkPumpReach, RunPumpReach, run);
            if (glides)
            {
                // AMIHAN RUNS ON THE AIR (`AmihanAirStep`, owner 2026-10-02): her run has no contact to dip at, so the hands
                // glide on a smaller round swell, and pump little because her arms ride swept back like wings.
                bob = Mathf.Lerp(bob, (Mathf.Cos(2f * phase) - 1f) * .5f * AmihanGlideBob, run);
                pumpDegrees = Mathf.Lerp(WalkPumpDegrees, AmihanRunPumpDegrees, run);
                pumpReach = Mathf.Lerp(WalkPumpReach, AmihanRunPumpReach, run);
            }

            _swayLeftBase = _leftPivot.localRotation; _swayLeftPosition = _leftPivot.localPosition;
            _swayRightBase = _rightPivot.localRotation; _swayRightPosition = _rightPivot.localPosition;
            _swayApplied = true;

            // ⚠️ THE NATURAL ARMS SWING HARDER. The numbers above were tuned for the block arms held under the chin, where a
            // centimetre is a lot of screen. On the live-model arms in the corners the same sway barely showed (owner,
            // 2026-10-06: "paete does a really subtle version for the walk but idk if thats intentional". It was not).
            float k = _swayBlend * (NaturalArms ? NaturalSwayGain : 1f);
            _swayLeftOffset = new Vector3(across, bob, pump * pumpReach) * k;
            _swayLeftTurn = Quaternion.Euler(-pump * pumpDegrees * k, 0f, 0f);
            _leftPivot.localPosition += _swayLeftOffset;
            _leftPivot.localRotation *= _swayLeftTurn;
            if (_carrying)
            {
                _swayRightOffset = new Vector3(across, bob * .8f, 0f) * k; _swayRightTurn = Quaternion.identity;
                _rightPivot.localPosition += _swayRightOffset;
            }
            else
            {
                _swayRightOffset = new Vector3(across, bob, -pump * pumpReach) * k;
                _swayRightTurn = Quaternion.Euler(pump * pumpDegrees * k, 0f, 0f);
                _rightPivot.localPosition += _swayRightOffset;
                _rightPivot.localRotation *= _swayRightTurn;
            }
        }
    }
}
