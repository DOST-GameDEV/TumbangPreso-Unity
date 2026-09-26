using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ RAISING THE CAN, FROM YOUR OWN EYES: BOTH HANDS ON IT, RISING WITH THE CHANNEL.
    ///
    /// 🧑 2026-09-24: *"refine ... the raising of can"* in both views. Before this the 1.5 s reset was
    /// `GrabClip` (one hand, 0.40 s) re-fired four times: the right hand dipped and came back, and the left
    /// never appeared (its pivot sits half a screen below the bottom edge, measured by `GameplayActionShots`).
    /// Now both hands come in low to the centre of the screen as if gripping the can in front of you, lift with
    /// `Carrier.ChannelRatio` as it stands up, and give a short press at the end
    /// (`docs/reports/gameplay-animation-2026-09-24/research-and-analysis.md` § 2.3). The can's own drawing
    /// rises under them (`Lata`), so hands and can agree.
    ///
    /// Positions and a small pitch only; it releases instantly to any authored action, and the channel's
    /// timing is the game's.
    /// </summary>
    public sealed partial class ViewmodelArms
    {
        private float _raiseBlend;
        private bool _raiseApplied;
        private Vector3 _raiseRightPos, _raiseLeftPos;
        private Quaternion _raiseRightRot, _raiseLeftRot;

        private float RaiseRatio => _characterMotor != null ? _characterMotor.GetComponent<Carrier>()?.ChannelRatio ?? 0 : 0;

        /// <summary>True while the local taya is raising the can and the hands are in the raise pose.</summary>
        private bool RaisingCan => RaiseRatio > 0 && _raiseBlend > .5f;

        private void RestoreRaiseCan()
        {
            if (!_raiseApplied) return;
            if (_rightPivot != null) { _rightPivot.localPosition = _raiseRightPos; _rightPivot.localRotation = _raiseRightRot; }
            if (_leftPivot != null) { _leftPivot.localPosition = _raiseLeftPos; _leftPivot.localRotation = _raiseLeftRot; }
            _raiseApplied = false;
        }

        private void ApplyRaiseCan(float dt)
        {
            if (_rightPivot == null || _leftPivot == null) return;
            float ratio = RaiseRatio;
            _raiseBlend = Mathf.MoveTowards(_raiseBlend, ratio > 0 ? 1 : 0, Mathf.Max(0, dt) / (ratio > 0 ? .12f : .15f));
            if (_raiseBlend <= .001f) return;
            float p = ratio > 0 ? ratio : 1;
            // The body's shape (`Visual.CanRaiseShape`, 2026-09-26): crouch, grip, lift, set.
            float rise = Visual.CanRaiseShape.Rise(p), crouch = Visual.CanRaiseShape.Crouch(p);
            float press = Visual.CanRaiseShape.Press(p);
            float k = _raiseBlend;
            _raiseRightPos = _rightPivot.localPosition; _raiseLeftPos = _leftPivot.localPosition;
            _raiseRightRot = _rightPivot.localRotation; _raiseLeftRot = _leftPivot.localRotation;
            _raiseApplied = true;
            // In toward the middle, low, closing on the can; up with it; down again in the press.
            // In the squat the hands reach forward and down to the can on the road.
            float lift = .10f * rise - .05f * press - .03f * crouch;
            _rightPivot.localPosition += new Vector3(-.17f, -.02f + lift, .06f) * k;
            _leftPivot.localPosition += new Vector3(.17f, .30f + lift, .06f) * k;
            _rightPivot.localRotation = Quaternion.AngleAxis(-18f * k, Vector3.forward) * _rightPivot.localRotation;
            _leftPivot.localRotation = Quaternion.AngleAxis(18f * k, Vector3.forward) * _leftPivot.localRotation;
        }
    }
}
