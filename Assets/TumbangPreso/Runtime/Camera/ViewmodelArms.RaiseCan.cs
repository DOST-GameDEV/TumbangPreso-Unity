using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ RAISING THE CAN, FROM YOUR OWN EYES: BOTH HANDS ON IT, RISING WITH THE CHANNEL.
    ///
    /// 🧑 2026-09-24: *"refine ... the raising of can"* in both views. Before this the 1.5 s reset was
    /// `GrabClip` (one hand, 0.40 s) re-fired four times: the right hand dipped and came back, and the left
    /// never appeared (its pivot sits half a screen below the bottom edge, measured by `GameplayActionShots`).
    ///
    /// 🧑 2026-10-23, v2: *"raising can animation in fpp is weird, feels like im js wobbing my can and i js did a quick
    /// squat"*. v1 slid both hands a fixed amount toward the middle of the screen, so they hovered beside the can, never
    /// on it, and bobbed with a curve of their own (Logs/fpp-1023/before). Now the fists are put ON THE CAN: each one on
    /// the side of the can as it is drawn this frame (`Lata`'s own Visual, which turns upright under them), seen along the
    /// line from the eye, so the hands close on it, turn up with it as it stands, and give one short press at the end.
    /// The eye's lean is `CameraRig.FppRaiseSquat` (a slow bend held through the grip, not a dip).
    ///
    /// Positions and turns only; it releases instantly to any authored action, and the channel's timing is the game's.
    /// </summary>
    public sealed partial class ViewmodelArms
    {
        private float _raiseBlend;
        private bool _raiseApplied;
        private Vector3 _raiseRightPos, _raiseLeftPos;
        private Quaternion _raiseRightRot, _raiseLeftRot, _raiseRightArm, _raiseLeftArm;
        private Lata _raiseCan;
        private Renderer[] _raiseCanBody;

        /// <summary>How far from the eye, in the view model's space, the gripping fists are drawn.</summary>
        public const float GripDepth = 1.22f;
        /// <summary>
        /// How far either side of the can's middle each fist is drawn, in the view model's space. The can is small on
        /// screen and a fist is wide: put on its true sides the two fists met in the middle and hid it (Logs/fpp-1023/after1),
        /// so they close on it from either side and leave it showing between them.
        /// </summary>
        public const float GripSpread = .31f;
        private static readonly Vector3 LeftShoulder = new Vector3(-.46f, -.88f, -.05f);

        // CAN-CARRY-1024: the raise's shape off the can's carry (pickup, held through the walk, set-down), not the HUD ratio.
        private float RaiseRatio => _characterMotor != null ? _characterMotor.GetComponent<Carrier>()?.RaiseShape ?? 0 : 0;

        /// <summary>True while the local taya is raising the can and the hands are in the raise pose.</summary>
        private bool RaisingCan => RaiseRatio > 0 && _raiseBlend > .5f;

        private void RestoreRaiseCan()
        {
            if (!_raiseApplied) return;
            if (_rightPivot != null) { _rightPivot.localPosition = _raiseRightPos; _rightPivot.localRotation = _raiseRightRot; }
            if (_leftPivot != null) { _leftPivot.localPosition = _raiseLeftPos; _leftPivot.localRotation = _raiseLeftRot; }
            if (_rightArm != null) _rightArm.localRotation = _raiseRightArm;
            if (_leftArm != null) _leftArm.localRotation = _raiseLeftArm;
            _raiseApplied = false;
        }

        private void ApplyRaiseCan(float dt)
        {
            if (_rightPivot == null || _leftPivot == null || _rightArm == null || _leftArm == null) return;
            float ratio = RaiseRatio;
            _raiseBlend = Mathf.MoveTowards(_raiseBlend, ratio > 0 ? 1 : 0, Mathf.Max(0, dt) / (ratio > 0 ? .16f : .2f));
            if (_raiseBlend <= .001f) return;
            float p = ratio > 0 ? ratio : 1;
            float press = Visual.CanRaiseShape.Press(p);
            float k = Mathf.SmoothStep(0, 1, _raiseBlend);
            _raiseRightPos = _rightPivot.localPosition; _raiseLeftPos = _leftPivot.localPosition;
            _raiseRightRot = _rightPivot.localRotation; _raiseLeftRot = _leftPivot.localRotation;
            _raiseRightArm = _rightArm.localRotation; _raiseLeftArm = _leftArm.localRotation;
            _raiseApplied = true;

            Vector3 rightFist, leftFist;
            if (TryCanGrip(out rightFist, out leftFist))
            {
                // The press: both fists push down on the can as it seats.
                rightFist += Vector3.down * (.035f * press); leftFist += Vector3.down * (.035f * press);
            }
            else
            {
                // No can drawn this frame (a remote raise relayed late): hold both hands low in the middle.
                rightFist = new Vector3(.16f, -.44f, GripDepth * .9f); leftFist = new Vector3(-.16f, -.44f, GripDepth * .9f);
            }
            // Palms turned in toward the can between them.
            AimForearm(_rightPivot, _rightArm, _rightRest, rightFist, RightShoulder, k, -24f);
            AimForearm(_leftPivot, _leftArm, _leftRest, leftFist, LeftShoulder, k, 24f);
        }

        /// <summary>
        /// Where each fist must be drawn to sit either side of the can as it is drawn now: on the eye's line to the can's
        /// middle at <see cref="GripDepth"/>, <see cref="GripSpread"/> to each side. False when there is no can, or it draws nothing.
        /// </summary>
        private bool TryCanGrip(out Vector3 right, out Vector3 left)
        {
            right = left = default;
            var lata = GameServices.Round?.Lata;
            if (lata == null) return false;
            if (lata != _raiseCan || _raiseCanBody == null)
            {
                _raiseCan = lata;
                var visual = lata.transform.Find("Visual");
                _raiseCanBody = (visual != null ? visual : lata.transform).GetComponentsInChildren<Renderer>(true);
            }
            bool any = false; var box = new Bounds();
            foreach (var r in _raiseCanBody)
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || r.GetComponent<Visual.VfxRenderTag>() != null) continue;
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                if (!any) { box = r.bounds; any = true; } else box.Encapsulate(r.bounds);
            }
            var eye = _viewCamera != null ? _viewCamera : GetComponentInParent<Camera>();
            if (!any || eye == null) return false;
            Vector3 origin = DrawnFromWorld(eye.transform.position);
            Vector3 ray = DrawnFromWorld(box.center) - origin;
            Vector3 middle = origin + (ray.sqrMagnitude > 1e-6f ? ray.normalized : Vector3.forward) * GripDepth;
            right = middle + Vector3.right * GripSpread; left = middle - Vector3.right * GripSpread;
            return true;
        }
    }
}
