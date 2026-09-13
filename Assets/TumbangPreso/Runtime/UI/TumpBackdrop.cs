using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(RawImage))]
    public sealed class TumpBackdrop : MonoBehaviour
    {
        private RawImage _base, _tree, _sun;
        private float _phase;
        private bool _focused = true;
        public bool Animate = true;
        private void Awake()
        {
            _base = GetComponent<RawImage>(); _base.raycastTarget = false;
            var custom = TumpUiTheme.Current.StreetBackground;
            _base.texture = custom != null ? custom : Resources.Load<Texture2D>("UI/illustrations/street_background");
            if (_base.texture == null) _base.texture = Resources.Load<Texture2D>("UI/illustrations/street_key_art");
            if (custom == null)
            {
                _tree = Layer("street_tree", new Vector2(.26f, .4f));
                _sun = Layer("street_sun", new Vector2(.735f, .88f));
            }
        }
        private RawImage Layer(string name, Vector2 pivot)
        {
            var texture = Resources.Load<Texture2D>("UI/illustrations/" + name);
            if (texture == null) return null;
            var image = TumpUiFactory.Rect(transform, name).gameObject.AddComponent<RawImage>();
            image.texture = texture; image.raycastTarget = false;
            image.rectTransform.pivot = pivot; TumpUiFactory.Stretch(image.rectTransform);
            return image;
        }
        private void OnApplicationFocus(bool focused) => _focused = focused;
        private void LateUpdate()
        {
            if (_base == null || _base.texture == null) return;
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            if (Animate && _focused && !reduced) _phase = Mathf.Repeat(_phase + Time.unscaledDeltaTime, 14);
            float t = reduced ? 0 : _phase / 14 * Mathf.PI * 2;
            float aspect = _base.rectTransform.rect.width / Mathf.Max(1, _base.rectTransform.rect.height);
            float source = (float)_base.texture.width / _base.texture.height;
            var uv = aspect < source ? new Rect((1 - aspect / source) * .5f, 0, aspect / source, 1)
                : new Rect(0, (1 - source / aspect) * .5f, 1, source / aspect);
            _base.uvRect = uv;
            if (_tree != null) { _tree.uvRect = uv; _tree.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t) * .45f); }
            if (_sun != null) { _sun.uvRect = uv; _sun.rectTransform.localScale = Vector3.one * (1 + Mathf.Sin(t) * .008f); }
        }
    }
}
