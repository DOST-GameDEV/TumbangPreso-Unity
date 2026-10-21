using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Focus and hover for a settings control.
    ///
    /// ⚠️ THE WHOLE ROW LIGHTS, NOT ONLY THE CONTROL. Until 2026-09-23 the only focus mark was a
    /// 3-unit underline under the control, which is about 1.5 pixels at 960x540 and sat 600 units
    /// from the row's label, so a pad user moving down a list of fifteen switches had to hunt for
    /// which one was live. The row now gets a faint accent band and a 6-unit accent bar at its left
    /// edge (the Overwatch and Valorant settings pattern: the focused ROW is the unit), and the
    /// underline stays under the control for the pointer case. Both come and go together, so hover
    /// and focus read as one language.
    ///
    /// ⚠️ AND IT IS LOUD. The first draft of this band was the accent at 9 per cent, which on the
    /// warm grey is visible only if you already know it is there. Hi-Fi RUSH's options screen
    /// (inspected 2026-09-23, `docs/reports/ui-hud-review-2026-09-23/research.md` finding 1) fills
    /// the focused row solid and adds a pointer; Splatoon 3's most-cited menu complaint is a
    /// current-location mark "too similar to the clickable options". So the band is 18 per cent,
    /// the bar is a solid 8 units, and the row's own label turns accent: three cues, one of which
    /// (the label colour) survives a glance from across a room.
    /// </summary>
    public sealed class SettingsControlFocus : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private Image _line, _band, _bar;
        private Text _label;
        private bool _selected, _hover;
        private void Awake()
        {
            _line = OwnerUiLayout.Rect(transform, "ControlFocus").gameObject.AddComponent<Image>();
            _line.rectTransform.anchorMin = Vector2.zero; _line.rectTransform.anchorMax = new Vector2(1, 0);
            _line.rectTransform.offsetMin = new Vector2(10, 0); _line.rectTransform.offsetMax = new Vector2(-10, 3);
            _line.color = SettingsPalette.Accent; _line.raycastTarget = false;
            var row = RowOf(transform);
            if (row != null)
            {
                _band = OwnerUiLayout.Rect(row, "RowFocus").gameObject.AddComponent<Image>();
                _band.rectTransform.SetAsFirstSibling();
                _band.rectTransform.anchorMin = Vector2.zero; _band.rectTransform.anchorMax = Vector2.one;
                _band.rectTransform.offsetMin = new Vector2(-14, 1); _band.rectTransform.offsetMax = new Vector2(-6, -1);
                _band.color = SettingsPalette.RowFocus; _band.raycastTarget = false;
                _bar = OwnerUiLayout.Rect(_band.transform, "RowFocusBar").gameObject.AddComponent<Image>();
                _bar.rectTransform.anchorMin = Vector2.zero; _bar.rectTransform.anchorMax = new Vector2(0, 1);
                _bar.rectTransform.offsetMin = new Vector2(0, 8); _bar.rectTransform.offsetMax = new Vector2(8, -8);
                _bar.color = SettingsPalette.Accent; _bar.raycastTarget = false;
                _label = row.Find("Label")?.GetComponent<Text>();
            }
            Refresh();
        }
        /// <summary>The list row this control lives in: the ancestor laid out by the scroll column.</summary>
        private static Transform RowOf(Transform control)
        {
            for (var t = control.parent; t != null && t.parent != null; t = t.parent)
                if (t.parent.GetComponent<VerticalLayoutGroup>() != null && t.GetComponent<LayoutElement>() != null) return t;
            return null;
        }
        public void OnSelect(BaseEventData e) { _selected = true; Refresh(); }
        public void OnDeselect(BaseEventData e) { _selected = false; Refresh(); }
        public void OnPointerEnter(PointerEventData e) { _hover = true; Refresh(); }
        public void OnPointerExit(PointerEventData e) { _hover = false; Refresh(); }
        private void OnDisable() { _selected = _hover = false; Refresh(); }
        private void Refresh()
        {
            bool live = _selected || _hover;
            if (_line != null) _line.enabled = live;
            if (_band != null) _band.enabled = live;
            if (_label != null) _label.color = live ? SettingsPalette.Accent : SettingsPalette.Ink;
        }
    }
}
