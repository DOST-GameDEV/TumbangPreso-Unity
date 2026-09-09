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
            // Keep the original callback and target rectangle. Only its visual
            // changes, so Back retains discard handling and controller semantics.
            foreach (var image in button.GetComponentsInChildren<Graphic>(true)) image.enabled = false;
            foreach (var skin in button.GetComponents<GodotButton>()) skin.enabled = false;
            foreach (var skin in button.GetComponents<WoodSkin>()) skin.enabled = false;
            foreach (var skin in button.GetComponents<PaperSkin>()) skin.enabled = false;
            foreach (var skin in button.GetComponents<ArrowButtonView>()) skin.enabled = false;
            _surface = button.GetComponent<StreetGraphic>();
            if (_surface == null)
            {
                // Graphic permits only one instance per object. Keep the original
                // image as an invisible hit area, and put the new drawing below it
                // as a non-raycasting child. The original Button receives input.
                var hit = button.GetComponent<Graphic>();
                if (hit == null) hit = button.gameObject.AddComponent<Image>();
                hit.enabled = true;
                hit.color = Color.clear;
                hit.raycastTarget = true;
                button.targetGraphic = hit;
                _surface = StreetUi.Detail(button.transform, "NavigationFace", StreetGraphic.Surface.Navigation);
                MenuKit.Stretch(_surface.rectTransform);
            }
            else { _surface.enabled = true; button.targetGraphic = _surface; }
            _surface.Style = StreetGraphic.Surface.Navigation;
            button.transition = Selectable.Transition.None;
            StreetUi.Icon(button.transform, glyph, new Vector2(.5f,.5f), Vector2.zero, new Vector2(32,32));
        }
        public void OnPointerEnter(PointerEventData e) { if (_surface != null) _surface.OnPointerEnter(e); }
        public void OnPointerExit(PointerEventData e) { if (_surface != null) _surface.OnPointerExit(e); }
        public void OnPointerDown(PointerEventData e) { if (_surface != null) _surface.OnPointerDown(e); }
        public void OnPointerUp(PointerEventData e) { if (_surface != null) _surface.OnPointerUp(e); }
        public void OnSelect(BaseEventData e) { if (_surface != null) _surface.OnSelect(e); }
        public void OnDeselect(BaseEventData e) { if (_surface != null) _surface.OnDeselect(e); }
    }
}
