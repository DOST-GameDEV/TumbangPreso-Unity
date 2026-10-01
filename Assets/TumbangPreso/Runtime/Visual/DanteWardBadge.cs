using TumbangPreso.CameraSystem;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>A camera-facing cue driven only by its owning ward's recorded clock.</summary>
    public sealed class DanteWardBadge : MonoBehaviour
    {
        private CharacterMotor _owner;
        private Transform _root;
        private SpriteRenderer _sprite;
        private float _height, _width;
        private bool _visible;

        public static DanteWardBadge Create(Transform root, CharacterMotor owner)
        {
            var go = new GameObject("DanteShieldBadge");
            go.transform.SetParent(root, false);
            var badge = go.AddComponent<DanteWardBadge>();
            badge._root = root; badge._owner = owner;
            badge._sprite = go.AddComponent<SpriteRenderer>();
            badge._sprite.sprite = AbilityIcons.For(AbilityGlyph.DanteShield);
            badge._sprite.sortingOrder = 29;
            badge._sprite.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var capsule = root.GetComponent<CharacterController>();
            badge._height = (capsule != null ? capsule.height * root.lossyScale.y : 1.8f) + .45f;
            badge._sprite.enabled = false;
            return badge;
        }

        public void StepTo(float age, float duration)
        {
            _visible = age >= 0 && age < duration;
            float arrival = Mathf.SmoothStep(0, 1, Mathf.Clamp01(age / .18f));
            float release = Mathf.Clamp01((duration - age) / .18f);
            _width = .36f * Mathf.Lerp(.85f, 1, arrival) * Mathf.Lerp(.85f, 1, release);
            _sprite.color = new Color(1, 1, 1, release);
            _sprite.enabled = _visible;
        }

        private void OnEnable() => Camera.onPreCull += BeforeCamera;
        private void OnDisable() => Camera.onPreCull -= BeforeCamera;

        private void BeforeCamera(Camera camera)
        {
            if (_sprite == null || _root == null) return;
            var rig = camera.GetComponent<CameraRig>();
            bool ownView = _owner != null && rig != null && rig.IsLocalFpp && rig.IsFollowing(_owner);
            _sprite.enabled = _visible && !ownView && _sprite.sprite != null;
            if (!_sprite.enabled) return;
            transform.position = _root.position + Vector3.up * _height;
            transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position, camera.transform.up);
            float spriteWidth = Mathf.Max(.01f, _sprite.sprite.bounds.size.x);
            float parentScale = Mathf.Max(.01f, _root.lossyScale.x);
            transform.localScale = Vector3.one * (_width / spriteWidth / parentScale);
        }
    }
}
