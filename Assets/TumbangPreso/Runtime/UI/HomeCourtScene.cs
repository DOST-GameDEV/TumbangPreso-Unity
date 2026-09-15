using UnityEngine;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(UnityEngine.UI.RawImage))]
    public sealed class HomeCourtScene : MonoBehaviour
    {
        public Texture2D Illustration;
        public float Drift = .0015f;
        public float WideVerticalFocus = .32f;
        public float NarrowHorizontalFocus = .56f;
        private UnityEngine.UI.RawImage _image;
        private void Awake()
        {
            _image = GetComponent<UnityEngine.UI.RawImage>(); _image.raycastTarget = false;
            if (Illustration == null) Illustration = Resources.Load<Texture2D>("UI/composition-redesign/home-court");
            _image.texture = Illustration;
            _image.color = Illustration != null ? Color.white : new Color32(224, 181, 144, 255);
        }
        private void LateUpdate()
        {
            if (Illustration == null || _image.rectTransform.rect.height <= 0) return;
            float source = Illustration.width / (float)Illustration.height;
            var size = _image.rectTransform.rect.size;
            float screen = size.x / size.y;
            var uv = screen > source ? new Vector2(1, source / screen) : new Vector2(screen / source, 1);
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            // Keep the can/slipper focal area in view instead of slicing its base
            // off on ultrawide displays. Narrower crops retain the right-hand can.
            var centre = new Vector2(
                Mathf.Lerp(.5f, NarrowHorizontalFocus, Mathf.Clamp01((1 - uv.x) / .3f)),
                Mathf.Lerp(.5f, WideVerticalFocus, Mathf.Clamp01((1 - uv.y) / .5f)));
            if (!reduced)
            {
                uv /= 1.008f;
                centre += new Vector2(Mathf.Sin(Time.unscaledTime * .10f), Mathf.Sin(Time.unscaledTime * .075f)) * Drift;
            }
            centre.x = Mathf.Clamp(centre.x, uv.x * .5f, 1 - uv.x * .5f);
            centre.y = Mathf.Clamp(centre.y, uv.y * .5f, 1 - uv.y * .5f);
            _image.uvRect = new Rect(centre - uv * .5f, uv);
        }
    }
}
