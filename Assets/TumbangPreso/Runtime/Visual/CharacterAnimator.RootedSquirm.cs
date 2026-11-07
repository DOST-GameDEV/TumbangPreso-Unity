using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ FIGHTING OUT OF PAETE'S ROOTS, ON TOP OF THE STRUGGLE LOOP (owner, 2026-11-05: *"when u hold f u slowly like remove
    /// shit off of u tapos as u get more and more out mas nakaka squiggle ka"*, *"thoroughly think abt it how the struggle and
    /// escape should look for everyone"*, every Hero Strike body). The loop (`rooted-struggle`) is the body testing the hold;
    /// this layer is the fight, and it grows with how far out the body is (`CharacterMotor.PresentedBreakFreeProgress`, timed
    /// on every peer, the same number the roots peel by in `PaeteRootBands`):
    ///  * just caught, holding F: a tight, quick wrench of the trunk side to side, the arms jammed down at the roots on the
    ///    thighs and tearing up at them in turn: hands working the binding off;
    ///  * half out: the wrench is wider and faster, the trunk bends with it, the head thrashes, the arms rip up past the hips;
    ///  * nearly out (past half): the knees start coming up out of what is left, one then the other, and near the end the
    ///    whole body bucks off its soles: the squiggle the owner asked for, biggest just before the snap;
    ///  * let go of F: it eases off to a restless sway that is wider the further out the body already is (the progress stays).
    /// Every turn is about the body's own axes and its own bones, so it fits a slight Nemu and a broad Dante alike; the bob is
    /// scaled by the body's height. A one-shot (a throw while held) and the plant heave win over it.
    /// </summary>
    public sealed partial class CharacterAnimator
    {
        private bool _rsApplied;
        private Transform _rsTorso, _rsHead, _rsArmL, _rsArmR, _rsLegL, _rsLegR, _rsRoot;
        private Quaternion _rsTorsoRest, _rsHeadRest, _rsArmLRest, _rsArmRRest, _rsLegLRest, _rsLegRRest;
        private Vector3 _rsRootRest;
        private float _rsEffort, _rsPhase, _rsHeight = -1f;

        /// <summary>For tests: how hard the body is fighting the roots right now, 0 to 1.</summary>
        public float RootedSquirmEffort => _rsEffort;

        private void RestoreRootedSquirm()
        {
            if (!_rsApplied) return;
            if (_rsTorso != null) _rsTorso.localRotation = _rsTorsoRest;
            if (_rsHead != null) _rsHead.localRotation = _rsHeadRest;
            if (_rsArmL != null) _rsArmL.localRotation = _rsArmLRest;
            if (_rsArmR != null) _rsArmR.localRotation = _rsArmRRest;
            if (_rsLegL != null) _rsLegL.localRotation = _rsLegLRest;
            if (_rsLegR != null) _rsLegR.localRotation = _rsLegRRest;
            if (_rsRoot != null) _rsRoot.localPosition = _rsRootRest;
            _rsApplied = false;
        }

        private void ApplyRootedSquirm()
        {
            float dt = Time.deltaTime;
            // A prisoner still throws: the wind-up, release and cancel own the arms and trunk, so the fight stands down.
            bool throwing = _chargePosing || _throwReleaseTime >= 0f || _throwCancelTime >= 0f;
            bool held = _motor != null && _motor.IsRooted && _oneShotLeft <= 0f && _motor.PullingPlantProgress <= 0f && !_motor.IsTagged && !throwing;
            float progress = held ? _motor.PresentedBreakFreeProgress : 0f;
            bool fighting = held && _motor.IsStruggling;
            float target = !held ? 0f : fighting ? 0.35f + 0.65f * progress : 0.10f + 0.20f * progress;
            _rsEffort = Mathf.MoveTowards(_rsEffort, target, dt * (target > _rsEffort ? 3.5f : 2.0f));
            if (!held || _rsEffort < 0.01f) return;
            // Quicker the further out: about a wrench a second when caught, nearly three when almost free.
            _rsPhase += dt * (fighting ? 7f + 10f * progress : 3f + 2f * progress);

            if (!_swingBonesResolved) ResolveSwingBones();
            _rsTorso = _swingTorso; _rsHead = _swingHead; _rsArmL = _swingArmL; _rsArmR = _swingArmR;
            _rsLegL = _swingLegL; _rsLegR = _swingLegR; _rsRoot = _swingRoot;
            if (_rsTorso == null) return;
            _rsTorsoRest = _rsTorso.localRotation;
            if (_rsHead != null) _rsHeadRest = _rsHead.localRotation;
            if (_rsArmL != null) _rsArmLRest = _rsArmL.localRotation;
            if (_rsArmR != null) _rsArmRRest = _rsArmR.localRotation;
            if (_rsLegL != null) _rsLegLRest = _rsLegL.localRotation;
            if (_rsLegR != null) _rsLegRRest = _rsLegR.localRotation;
            if (_rsRoot != null) _rsRootRest = _rsRoot.localPosition;
            _rsApplied = true;
            if (_rsHeight < 0f) _rsHeight = MeasureHeight();

            float e = _rsEffort, ph = _rsPhase;
            Vector3 up = transform.up, forward = transform.forward, right = transform.right;
            // The trunk wrenches round and bends with it, leaning into the strain.
            float yaw = (10f + 12f * progress) * e * Mathf.Sin(ph);
            float roll = (6f + 10f * progress) * e * Mathf.Sin(ph * 0.73f + 1.3f);
            float lean = (4f + 6f * progress) * e * (0.5f + 0.5f * Mathf.Sin(ph * 1.31f));
            _rsTorso.rotation = Quaternion.AngleAxis(yaw, up) * Quaternion.AngleAxis(roll, forward) * Quaternion.AngleAxis(lean, right) * _rsTorso.rotation;
            // The head keeps half its eyes on the court, and thrashes as it gets close.
            if (_rsHead != null)
                _rsHead.rotation = Quaternion.AngleAxis(-0.55f * yaw, up) * Quaternion.AngleAxis(-0.4f * roll + 9f * progress * e * Mathf.Sin(ph * 2.1f), forward)
                                   * _rsHead.rotation;
            // The hands work the roots off: one jammed down at its thigh while the other tears up and out, in turn. Early the
            // tear stops at the hip; by the end it rips up past the shoulder.
            float tearL = 0.5f + 0.5f * Mathf.Sin(ph * 0.5f), tearR = 1f - tearL;
            float reach = 0.15f + 0.55f * progress;
            Vector3 down = new Vector3(0.38f, -0.85f, 0.32f), torn = new Vector3(0.85f, reach, 0.15f);
            float arms = (fighting ? 0.55f : 0.25f) * e;
            if (_rsArmR != null) PointArm(_rsArmR, _alongR, Vector3.Lerp(down, torn, tearR), arms, 0f);
            if (_rsArmL != null) { var l = Vector3.Lerp(down, torn, tearL); l.x = -l.x; PointArm(_rsArmL, _alongL, l, arms, 0f); }
            // Past half, the knees come up out of what is left of the roots, one then the other.
            float knees = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, progress)) * e * (fighting ? 1f : 0.3f);
            if (knees > 0.001f)
            {
                if (_rsLegL != null) _rsLegL.rotation = Quaternion.AngleAxis(-30f * knees * Mathf.Max(0f, Mathf.Sin(ph * 0.5f)), right) * _rsLegL.rotation;
                if (_rsLegR != null) _rsLegR.rotation = Quaternion.AngleAxis(-30f * knees * Mathf.Max(0f, Mathf.Sin(ph * 0.5f + Mathf.PI)), right) * _rsLegR.rotation;
            }
            // The body sways in the hold, and near the end bucks off its soles.
            if (_rsRoot != null)
            {
                float scale = Mathf.Clamp(_rsHeight / 1.87f, 0.6f, 1.4f);
                float buck = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 1f, progress)) * (fighting ? 1f : 0f);
                Vector3 offset = right * (0.03f * e * Mathf.Sin(ph * 0.73f)) + up * (0.05f * buck * e * Mathf.Max(0f, Mathf.Sin(ph)));
                _rsRoot.position += offset * scale;
            }
        }

        /// <summary>The body's height in metres, from its renderers, once; Sean is 1.87.</summary>
        private float MeasureHeight()
        {
            float low = float.MaxValue, high = float.MinValue;
            foreach (var r in GetComponentsInChildren<SkinnedMeshRenderer>(false))
            { var b = r.bounds; low = Mathf.Min(low, b.min.y); high = Mathf.Max(high, b.max.y); }
            return high > low ? high - low : 1.87f;
        }

        private void ClearRootedSquirm()
        {
            RestoreRootedSquirm();
            _rsTorso = _rsHead = _rsArmL = _rsArmR = _rsLegL = _rsLegR = _rsRoot = null;
            _rsEffort = 0f; _rsHeight = -1f;
        }
    }
}
