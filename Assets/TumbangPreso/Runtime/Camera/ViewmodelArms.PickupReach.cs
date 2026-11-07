using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    public sealed partial class ViewmodelArms
    {
        private Carrier _pickupSource;
        private bool _pickupApplied;
        private float _pickupReachWeight;
        private Vector3 _pickupRightPosition, _pickupLeftPosition, _pickupShoePosition;
        private Quaternion _pickupRightRotation, _pickupRightArmRotation, _pickupShoeRotation;

        public void SetPickupSource(Carrier source)
        {
            RestorePickupReach();
            if (_pickupSource != source) _pickupReachWeight = 0;
            _pickupSource = source;
        }

        // CameraRig polls holding/skin every frame. Reapply after that poll as well as in StepVisuals,
        // so neither LateUpdate order nor MatchSkin's grip normalisation can snap the shoe into the hand.
        public void RefreshPickupPresentation()
        {
            Vector3 normalPosition = _heldSlipper != null ? _heldSlipper.localPosition : Vector3.zero;
            Quaternion normalRotation = _heldSlipper != null ? _heldSlipper.localRotation : Quaternion.identity;
            RestorePickupReach();
            if (_heldSlipper != null)
            {
                _heldSlipper.localPosition = normalPosition;
                _heldSlipper.localRotation = normalRotation;
            }
            ApplyPickupReach(0);
        }

        private void RestorePickupReach()
        {
            if (!_pickupApplied) return;
            if (_rightPivot != null) { _rightPivot.localPosition = _pickupRightPosition; _rightPivot.localRotation = _pickupRightRotation; }
            if (_rightArm != null) _rightArm.localRotation = _pickupRightArmRotation;
            if (_leftPivot != null) _leftPivot.localPosition = _pickupLeftPosition;
            if (_heldSlipper != null) { _heldSlipper.localPosition = _pickupShoePosition; _heldSlipper.localRotation = _pickupShoeRotation; }
            _pickupApplied = false;
        }

        private void ApplyPickupReach(float dt)
        {
            if (_pickupSource == null || _rightPivot == null || _rightArm == null || _leftPivot == null) return;
            bool motion = !Settings.SettingsStore.Current.ReducedUiMotion && _charge < 0;
            bool reaching = motion && (_pickupSource.AutoPickupRatio > 0 || _pickupSource.PickupLifting);
            float bend = reaching ? _pickupSource.PickupBend : 0;
            _pickupReachWeight = Mathf.MoveTowards(_pickupReachWeight, bend, Mathf.Max(0, dt) / .10f);
            if (_pickupReachWeight <= .001f && !reaching) return;

            _pickupRightPosition = _rightPivot.localPosition; _pickupRightRotation = _rightPivot.localRotation;
            _pickupRightArmRotation = _rightArm.localRotation; _pickupLeftPosition = _leftPivot.localPosition;
            _pickupShoePosition = _heldSlipper != null ? _heldSlipper.localPosition : Vector3.zero;
            _pickupShoeRotation = _heldSlipper != null ? _heldSlipper.localRotation : Quaternion.identity;
            _pickupApplied = true;

            var style = _pickupSource.PickupStyle;
            Vector3 ground = DrawnFromWorld(_pickupSource.PickupGround);
            float depth = Mathf.Clamp(ground.z, .55f, 1.2f);
            Vector3 low = ground.z > .1f ? ground * (depth / ground.z) : new Vector3(.14f, -1.1f, depth);
            low = new Vector3(Mathf.Clamp(low.x, -.60f, .65f), Mathf.Clamp(low.y, -1.1f, -.20f), depth);
            float lift = _pickupSource.PickupLifting ? _pickupSource.PickupLift : 0;
            Vector3 fist = Vector3.Lerp(low, CarryAnchor + new Vector3(0, .04f, .02f), lift);
            AimForearm(_rightPivot, _rightArm, _rightRest, fist, PickupMotion.Shoulder(style), _pickupReachWeight);
            _leftPivot.localPosition += PickupMotion.Brace(style) * _pickupReachWeight;

            // The local player sees this copy, not Carrier's world shoe. Start it where the shoe lay.
            if (_carrying && reaching && _heldSlipper != null && _pickupSource.PickupLifting)
            {
                Vector3 grip = transform.InverseTransformPoint(_heldSlipper.position);
                float caught = _pickupSource.PickupGrip;
                _heldSlipper.position = transform.TransformPoint(Vector3.Lerp(ground, grip, caught));
                _heldSlipper.rotation = Quaternion.Slerp(_pickupSource.PickupGroundRotation, _heldSlipper.rotation, caught);
            }
        }
    }
}
