using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TumbangPreso.UI
{
    /// <summary>One visual language for leaving a screen or dismissing a layer.</summary>
    public sealed class NavigationSymbol : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private StreetGraphic _surface;
        public static void Apply(Button button)
        {
            // New native views own their own complete visual language; navigation
            // discovery must never invoke retired skin builders on those controls.
            if (button != null && button.GetComponent<TumpSurface>() != null) return;
            if (button == null || button.GetComponent<NavigationSymbol>() != null) return;
            var label = button.GetComponentInChildren<Text>(true);
            string words = label != null ? label.text.Trim().ToUpperInvariant() : "";
            bool back = button.name.EndsWith("BackButton", System.StringComparison.Ordinal)
                     || words == "BACK" || words == "◀  BACK" || words == "‹  BACK";
            bool close = button.name == "HubClose" || button.name == "LoadoutClose"
                      || words == "CLOSE";
            if (!back && !close) return;
            var marker = button.gameObject.AddComponent<NavigationSymbol>();
            marker.Build(button, close ? StreetIcon.Glyph.Close : StreetIcon.Glyph.Back);
        }

        private void Build(Button button, StreetIcon.Glyph glyph)
        {
            _surface = StreetUi.Restyle(button, StreetGraphic.Surface.Navigation);
            var label = button.GetComponentInChildren<Text>(true);
            bool wordsFit = ((RectTransform)button.transform).rect.width >= 120;
            if (label != null)
            {
                label.enabled = wordsFit;
                label.gameObject.SetActive(true);
                label.text = glyph == StreetIcon.Glyph.Close ? "Close" : "Back";
                label.fontSize = 26;
                label.alignment = TextAnchor.MiddleCenter;
                MenuKit.Read(label, true);
                MenuKit.Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(34, 4);
                label.rectTransform.offsetMax = new Vector2(-8, -4);
            }
            StreetUi.Icon(button.transform, glyph, new Vector2(wordsFit ? 0 : .5f, .5f),
                new Vector2(wordsFit ? 22 : 0, 0), new Vector2(24, 24));
        }
        public void OnPointerEnter(PointerEventData e) { if (_surface != null) _surface.OnPointerEnter(e); }
        public void OnPointerExit(PointerEventData e) { if (_surface != null) _surface.OnPointerExit(e); }
        public void OnPointerDown(PointerEventData e) { if (_surface != null) _surface.OnPointerDown(e); }
        public void OnPointerUp(PointerEventData e) { if (_surface != null) _surface.OnPointerUp(e); }
        public void OnSelect(BaseEventData e) { if (_surface != null) _surface.OnSelect(e); }
        public void OnDeselect(BaseEventData e) { if (_surface != null) _surface.OnDeselect(e); }
    }
}
