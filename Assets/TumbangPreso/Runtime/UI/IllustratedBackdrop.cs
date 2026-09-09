using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Gentle motion in illustrated layers; interface geometry stays still.</summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class IllustratedBackdrop : MonoBehaviour
    {
        public const float Period = 14f;
        public bool Animating = true;
        public float Phase { get; private set; }
        public bool HasArtwork => _base != null && _base.texture != null;
        public bool HasIndependentLayers => _tree != null && _sun != null;
        private RawImage _base, _tree, _sun;

        private void Awake()
        {
            _base = GetComponent<RawImage>();
            var full = Resources.Load<Texture2D>("UI/illustrations/street_key_art");
            var background = Resources.Load<Texture2D>("UI/illustrations/street_background");
            bool layersReady = background != null
                && Resources.Load<Texture2D>("UI/illustrations/street_tree") != null
                && Resources.Load<Texture2D>("UI/illustrations/street_sun") != null;
            _base.texture = layersReady ? background : full;
            _base.color = _base.texture != null ? Color.white : Color.clear;
            _base.raycastTarget = false;
            if (layersReady)
            {
                _tree = Layer("street_tree", new Vector2(.26f,.4f));
                _sun = Layer("street_sun", new Vector2(.735f,.88f));
            }
        }

        private RawImage Layer(string resource, Vector2 pivot)
        {
            var texture = Resources.Load<Texture2D>("UI/illustrations/" + resource);
            if (texture == null) return null;
            var go = new GameObject(resource, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(transform, false);
            var image = go.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            image.rectTransform.pivot = pivot;
            MenuKit.Stretch(image.rectTransform);
            return image;
        }

        private void LateUpdate()
        {
            if (_base == null || _base.texture == null) return;
            if (Animating) Phase = Mathf.Repeat(Phase + Time.unscaledDeltaTime, Period);
            float t = Phase / Period * Mathf.PI * 2f;
            float aspect = _base.rectTransform.rect.width / Mathf.Max(1, _base.rectTransform.rect.height);
            float source = _base.texture.width / (float)_base.texture.height;
            Rect uv = aspect < source
                ? new Rect((1f-aspect/source)*.5f, 0, aspect/source, 1)
                : new Rect(0, (1f-source/aspect)*.5f, 1, source/aspect);
            _base.uvRect = uv;
            if (_tree != null)
            {
                _tree.uvRect = uv;
                _tree.rectTransform.localRotation = Quaternion.Euler(0,0,Mathf.Sin(t)*.45f);
            }
            if (_sun != null)
            {
                _sun.uvRect = uv;
                _sun.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t)*.008f);
            }
        }
    }
}
