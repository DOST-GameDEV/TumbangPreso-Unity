using UnityEngine;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(UnityEngine.UI.RawImage))]
    public sealed class HomeCourtScene : MonoBehaviour
    {
        public Texture2D Illustration;
        public float Drift = .0015f;
        /// <summary>
        /// Where the crop keeps its grip when the window is not 16:9.
        ///
        /// ⚠⚠ BOTH OF THESE POINTED AT THE CAN AND THEY HAD TO TURN ROUND ON
        /// 2026-09-18, BECAUSE THE LOGO IS INSIDE THE PICTURE NOW. They were
        /// tuned to hold the can and the slipper, which was right while the TUMP
        /// mark was a separate UI sprite floating over the street and could not
        /// be cropped by anything. Her redraw made the graffiti on the wall the
        /// title, so the old bias cut the game's own name off: at 1280x960 the
        /// crop took the T and the screen read "uMP", and at 3840x1080 the whole
        /// graffiti sat above the top edge. Both are in this repository's own PC
        /// viewport list, and `docs/TODO.md` § 153.13 has the two captures.
        ///
        /// ⚠ THE GRAFFITI IS AT u 0.02-0.45, v 0.46-0.95 of her plate, so these
        /// are the values that keep all of it: a narrow window holds the left
        /// edge, a wide one holds the top. The can and the slipper are what gets
        /// given up at the extremes, and that is the right way round -- a title
        /// screen may lose a prop, it may not lose its title.
        /// </summary>
        public float WideVerticalFocus = .71f;
        public float NarrowHorizontalFocus = .30f;
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
            // Hold her graffiti, which is the logo: the left edge as the window
            // narrows, the top as it widens. The clamp below decides the rest.
            var centre = new Vector2(
                Mathf.Lerp(.5f, NarrowHorizontalFocus, Mathf.Clamp01((1 - uv.x) / .3f)),
                Mathf.Lerp(.5f, WideVerticalFocus, Mathf.Clamp01((1 - uv.y) / .5f)));
            if (!reduced)
            {
                if(Drift>0)uv /= 1.008f;
                centre += new Vector2(Mathf.Sin(Time.unscaledTime * .10f), Mathf.Sin(Time.unscaledTime * .075f)) * Drift;
            }
            centre.x = Mathf.Clamp(centre.x, uv.x * .5f, 1 - uv.x * .5f);
            centre.y = Mathf.Clamp(centre.y, uv.y * .5f, 1 - uv.y * .5f);
            _image.uvRect = new Rect(centre - uv * .5f, uv);
        }
    }
}
