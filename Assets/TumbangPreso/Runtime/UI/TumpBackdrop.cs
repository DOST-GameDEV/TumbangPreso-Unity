using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(RawImage))]
    public sealed class TumpBackdrop : MonoBehaviour
    {
        private RawImage _base, _tree, _sun;
        private Image _can, _slipper;
        private Sprite _canSprite, _slipperSprite;
        private float _phase;
        private bool _focused = true;
        public bool Animate = true;
        public bool AlignCropRight;
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
            var theme = TumpUiTheme.Current;
            if (theme.StreetProps != null)
            {
                _canSprite = Sprite.Create(theme.StreetProps, theme.StreetCanRect, new Vector2(.5f, 0), 100, 0, SpriteMeshType.FullRect);
                _slipperSprite = Sprite.Create(theme.StreetProps, theme.StreetSlipperRect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                _can = TumpUiFactory.Art(transform, "IllustratedCan", _canSprite);
                _slipper = TumpUiFactory.Art(transform, "IllustratedSlipper", _slipperSprite);
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
            var bounds = _base.rectTransform.rect;
            // A Canvas has no usable rect before its first layout (and during teardown).
            // Mapping source coordinates through a zero-width crop would write infinity
            // into both foreground anchors, then CanvasRenderer rejects their bounds.
            bool laidOut = bounds.width > .01f && bounds.height > .01f;
            if (_can != null) _can.enabled = laidOut;
            if (_slipper != null) _slipper.enabled = laidOut;
            if (!laidOut) return;
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            if (Animate && _focused && !reduced) _phase = Mathf.Repeat(_phase + Time.unscaledDeltaTime, 14);
            float t = reduced ? 0 : _phase / 14 * Mathf.PI * 2;
            float aspect = bounds.width / bounds.height;
            float source = (float)_base.texture.width / _base.texture.height;
            var uv = aspect < source ? new Rect((1 - aspect / source) * .5f, 0, aspect / source, 1)
                : new Rect(0, (1 - source / aspect) * .5f, 1, source / aspect);
            if (AlignCropRight && uv.width < 1) uv.x = 1 - uv.width;
            _base.uvRect = uv;
            if (_tree != null) { _tree.uvRect = uv; _tree.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t) * .45f); }
            if (_sun != null) { _sun.uvRect = uv; _sun.rectTransform.localScale = Vector3.one * (1 + Mathf.Sin(t) * .008f); }
            DrawProps(uv, reduced);
        }
        private void DrawProps(Rect uv, bool reduced)
        {
            if (_can == null || _slipper == null) return;
            var rect = _base.rectTransform.rect;
            float unit = rect.height / uv.height;
            float phase = reduced ? 0 : _phase;
            float flight = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(4, 5.1f, phase));
            float fall = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(5, 5.7f, phase));
            float restore = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(10, 11.5f, phase));
            float alpha = Mathf.Abs(restore * 2 - 1);
            if (restore >= .5f) { flight = 0; fall = 0; }
            float canX = .79f - fall * .006f;
            PlaceProp(_can.rectTransform, new Vector2(canX, .15f), new Vector2(.10f, .15f) * unit, uv);
            _can.rectTransform.pivot = new Vector2(.5f, .03f);
            _can.rectTransform.localRotation = Quaternion.Euler(0, 0, -fall * 78);
            float x = Mathf.Lerp(.58f, .77f, flight);
            float y = .135f + Mathf.Sin(flight * Mathf.PI) * .075f;
            PlaceProp(_slipper.rectTransform, new Vector2(x, y), new Vector2(.10f, .082f) * unit, uv);
            _slipper.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-24, 20, flight));
            _can.color = _slipper.color = new Color(1, 1, 1, alpha);
        }
        private static void PlaceProp(RectTransform rect, Vector2 source, Vector2 size, Rect uv)
        {
            Vector2 anchor = new Vector2((source.x - uv.x) / uv.width, (source.y - uv.y) / uv.height);
            TumpUiFactory.Anchor(rect, anchor, Vector2.zero, size);
        }
        private void OnDestroy()
        {
            if (_canSprite != null) Destroy(_canSprite);
            if (_slipperSprite != null) Destroy(_slipperSprite);
        }
    }
}
