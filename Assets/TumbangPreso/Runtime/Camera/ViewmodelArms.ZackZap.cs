using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    public sealed partial class ViewmodelArms
    {
        // ⚠️ ZACK'S OVERCLOCK ZAP IN HIS OWN EYES (owner, 2026-10-26: "BOTH hands must visibly participate", "not a one-hand Neon
        // imitation"). Both hands reach forward below the crosshair and feed a shared convergence point. His viewmodel slipper
        // is stowed during the channel and return, without changing possession. The bridge arcs run from both tips
        // (`TryZackHandTips`, drawn by `ZackZapLook`) to that point. Both arms
        // tremble with the current. The crosshair stays clear: the hands sit in the bottom third. Reeling in Live Wire uses a
        // smaller version of the same pull: the right hand draws back toward the chest as the wire comes in.
        private Vector3 _zapLeft, _zapRight;
        private Quaternion _zapLeftTurn = Quaternion.identity, _zapRightTurn = Quaternion.identity;
        private float _zap, _reel;
        private bool _zapApplied;
        private bool _zapShoeStowed;
        private Vector3 _zapRightBaseScale = Vector3.one;

        private void RestoreZackZap()
        {
            if (!_zapApplied) return;
            if (_leftPivot != null) { _leftPivot.localPosition -= _zapLeft; _leftPivot.localRotation = Quaternion.Inverse(_zapLeftTurn) * _leftPivot.localRotation; }
            if (_rightPivot != null) { _rightPivot.localPosition -= _zapRight; _rightPivot.localRotation = Quaternion.Inverse(_zapRightTurn) * _rightPivot.localRotation; _rightPivot.localScale = _zapRightBaseScale; }
            _zapApplied = false;
        }

        private ZackHeroKit ZackKit => _characterMotor != null ? _characterMotor.AbilitySystem?.Kit as ZackHeroKit : null;

        private void ApplyZackZap(float dt)
        {
            var kit = ZackKit;
            bool zapping = kit != null && kit.Zapping && _characterMotor.CanAct();
            bool reeling = kit != null && kit.ReelProgress >= 0.0f;
            _zap = Mathf.MoveTowards(_zap, zapping ? 1f : 0f, dt / (zapping ? .08f : .15f));
            _reel = Mathf.MoveTowards(_reel, reeling ? 1f : 0f, dt / .08f);
            if (_heldSlipper != null)
            {
                if (kit != null && _zap > .03f)
                { _heldSlipper.gameObject.SetActive(false); _zapShoeStowed = true; }
                else if (_zapShoeStowed)
                { _heldSlipper.gameObject.SetActive(_carrying); _zapShoeStowed = false; }
            }
            if ((_zap <= 0f && _reel <= 0f) || _leftPivot == null || _rightPivot == null) return;
            float e = _zap * _zap * (3f - 2f * _zap);
            _zapRightBaseScale = _rightPivot.localScale;
            // Carrying normally shrinks the grip arm. Both empty zap hands must have the same proportions.
            _rightPivot.localScale = Vector3.Lerp(_zapRightBaseScale, _leftPivot.localScale, e);
            float shake = Settings.SettingsStore.Current.ReducedEffects ? 0f : .006f * e;
            var tremble = new Vector3(Mathf.Sin(Time.time * 61f), Mathf.Sin(Time.time * 47f + 1f), 0f) * shake;
            ZackPalmPose(_leftPivot, new Vector3(-.29f, -.25f, .70f) + tremble, new Vector3(.04f, .65f, .76f), e,
                out _zapLeft, out _zapLeftTurn);
            ZackPalmPose(_rightPivot, new Vector3(.29f, -.27f, .70f) - tremble, new Vector3(-.04f, .58f, .81f), e,
                out _zapRight, out _zapRightTurn);
            if (_reel > 0f && kit != null)
            {
                // The reel: the right hand draws back toward the chest as the wire comes in.
                float r = _reel * Mathf.Clamp01(kit.ReelProgress < 0 ? 1 : kit.ReelProgress);
                _zapRight += new Vector3(-.06f, .10f, -.10f) * r;
            }
            _leftPivot.localPosition += _zapLeft; _leftPivot.localRotation = _zapLeftTurn * _leftPivot.localRotation;
            _rightPivot.localPosition += _zapRight; _rightPivot.localRotation = _zapRightTurn * _rightPivot.localRotation;
            _zapApplied = true;
        }

        private static void ZackPalmPose(Transform pivot, Vector3 tip, Vector3 along, float weight, out Vector3 offset, out Quaternion turn)
        {
            var target = Quaternion.FromToRotation(Vector3.up, along.normalized);
            var reach = target * Vector3.Scale(pivot.localScale, Vector3.up * ArmLength * .96f);
            offset = (tip - reach - pivot.localPosition) * weight;
            turn = Quaternion.Slerp(pivot.localRotation, target, weight) * Quaternion.Inverse(pivot.localRotation);
        }

        /// <summary>World positions of the two camera hands' tips (lead = his left), for the zap's bridge arc and bolt.</summary>
        public bool TryZackHandTips(out Vector3 lead, out Vector3 rear)
        {
            lead = rear = Vector3.zero;
            if (_leftPivot == null || _rightPivot == null || !_leftPivot.gameObject.activeInHierarchy) return false;
            lead = _leftPivot.TransformPoint(Vector3.up * ArmLength * .96f);
            rear = _rightPivot.TransformPoint(Vector3.up * ArmLength * .96f);
            return true;
        }
    }
}
