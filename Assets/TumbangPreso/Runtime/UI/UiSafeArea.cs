using UnityEngine;

namespace TumbangPreso.UI
{
    // Edge controls respect cutouts and system gestures. World art and aim stay full-screen.
    [DefaultExecutionOrder(-200)]
    public sealed class UiSafeArea : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _last;
        private Vector2 _display;
        private RectTransform _design;

        public void KeepDesignInside(RectTransform design) { _design = design; ResizeDesign(); }
        private void ResizeDesign()
        {
            if (_design == null || _rect == null) return;
            Vector2 size = _rect.rect.size, reference = _design.sizeDelta;
            if (size.x <= 0 || size.y <= 0 || reference.x <= 0 || reference.y <= 0) return;
            float scale = Mathf.Min(1, size.x / reference.x, size.y / reference.y);
            _design.localScale = new Vector3(scale, scale, 1);
        }

        public static RectTransform Content(Canvas canvas, string name)
        {
            var rect = OwnerUiLayout.Rect(canvas.transform, name);
            rect.gameObject.AddComponent<UiSafeArea>().Refresh();
            return rect;
        }

        public void Fit(Rect pixels, Vector2 display)
        {
            if (display.x <= 0 || display.y <= 0) return;
            if (_rect == null) _rect = (RectTransform)transform;
            var min = new Vector2(Mathf.Clamp01(pixels.xMin / display.x), Mathf.Clamp01(pixels.yMin / display.y));
            var max = new Vector2(Mathf.Clamp01(pixels.xMax / display.x), Mathf.Clamp01(pixels.yMax / display.y));
            if (max.x <= min.x || max.y <= min.y) { min = Vector2.zero; max = Vector2.one; }
            _rect.anchorMin = min; _rect.anchorMax = max;
            _rect.offsetMin = _rect.offsetMax = Vector2.zero;
            ResizeDesign();
        }

        private void Refresh()
        {
            _last = Screen.safeArea; _display = new Vector2(Screen.width, Screen.height);
            Fit(_last, _display);
        }
        private void Update()
        {
            if (_last != Screen.safeArea || _display.x != Screen.width || _display.y != Screen.height) Refresh();
            ResizeDesign();
        }
    }
}
