using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    public sealed partial class ViewmodelArms
    {
        private Vector3 _featherLeft, _featherRight;
        private bool _featherApplied;

        private void RestoreFeatherfall()
        {
            if (!_featherApplied) return;
            if (_leftPivot != null) _leftPivot.localPosition -= _featherLeft;
            if (_rightPivot != null) _rightPivot.localPosition -= _featherRight;
            _featherApplied = false;
        }

        private void ApplyFeatherfall()
        {
            if (_characterMotor == null || _characterMotor.AbilitySystem?.Kit?.HeroId != "amihan"
                || !_characterMotor.IsFlying || _characterMotor.IsStunned || _clip != null || _charge >= 0
                || _actionReturnLeft > 0 || !string.IsNullOrEmpty(_aimPreview)) return;
            float breath = Mathf.Sin(_phase * 1.8f);
            float balance = Mathf.Clamp(_characterMotor.transform.InverseTransformDirection(_characterMotor.Velocity).x / 5, -1, 1);
            float descend = _characterMotor.IsAloft ? 0 : .035f;
            _featherLeft = new Vector3(-.045f - balance * .012f, -.025f + breath * .009f + descend, .015f);
            // The carrying hand keeps its grip. Only an empty right hand balances the flight.
            _featherRight = _carrying ? Vector3.zero : new Vector3(.025f - balance * .008f, -.018f - breath * .006f + descend, .008f);
            if (_leftPivot != null) _leftPivot.localPosition += _featherLeft;
            if (_rightPivot != null) _rightPivot.localPosition += _featherRight;
            _featherApplied = true;
        }
    }
}
