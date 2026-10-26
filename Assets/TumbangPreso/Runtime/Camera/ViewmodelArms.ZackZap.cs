using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    public sealed partial class ViewmodelArms
    {
        // ⚠️ ZACK'S OVERCLOCK ZAP IN HIS OWN EYES (owner, 2026-10-26: "BOTH hands must visibly participate", "not a one-hand Neon
        // imitation"). While he holds the zap his LEFT hand is thrust up toward the lower centre of the frame, palm out at the
        // rival, and his RIGHT hand comes in and up beside it, a fist's width lower and closer, as the second clamp. The bridge arc
        // runs between the two tips (`TryZackHandTips`, drawn by `ZackZapLook`) and the bolt leaves the left palm. Both arms
        // tremble with the current. The crosshair stays clear: the hands sit in the bottom third. Reeling in Live Wire uses a
        // smaller version of the same pull: the right hand draws back toward the chest as the wire comes in.
        private Vector3 _zapLeft, _zapRight;
        private Quaternion _zapLeftTurn = Quaternion.identity, _zapRightTurn = Quaternion.identity;
        private float _zap, _reel;
        private bool _zapApplied;

        private void RestoreZackZap()
        {
            if (!_zapApplied) return;
            if (_leftPivot != null) { _leftPivot.localPosition -= _zapLeft; _leftPivot.localRotation = Quaternion.Inverse(_zapLeftTurn) * _leftPivot.localRotation; }
            if (_rightPivot != null) { _rightPivot.localPosition -= _zapRight; _rightPivot.localRotation = Quaternion.Inverse(_zapRightTurn) * _rightPivot.localRotation; }
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
            if ((_zap <= 0f && _reel <= 0f) || _leftPivot == null || _rightPivot == null) return;
            float e = _zap * _zap * (3f - 2f * _zap);
            float shake = Settings.SettingsStore.Current.ReducedEffects ? 0f : .006f * e;
            var tremble = new Vector3(Mathf.Sin(Time.time * 61f), Mathf.Sin(Time.time * 47f + 1f), 0f) * shake;
            _zapLeft = (new Vector3(.24f, .36f, .14f) + tremble) * e;
            _zapRight = _carrying ? new Vector3(-.05f, .08f, .04f) * e : (new Vector3(-.16f, .20f, .10f) - tremble) * e;
            _zapLeftTurn = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(-26f, 8f, 0f), e);
            _zapRightTurn = _carrying ? Quaternion.identity : Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(-18f, -10f, 0f), e);
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
