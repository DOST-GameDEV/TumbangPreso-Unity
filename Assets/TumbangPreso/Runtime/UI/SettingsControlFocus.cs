using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed class SettingsControlFocus : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private Image _line;
        private bool _selected, _hover;
        private void Awake()
        {
            _line = OwnerUiLayout.Rect(transform, "ControlFocus").gameObject.AddComponent<Image>();
            _line.rectTransform.anchorMin = Vector2.zero; _line.rectTransform.anchorMax = new Vector2(1, 0);
            _line.rectTransform.offsetMin = new Vector2(10, 0); _line.rectTransform.offsetMax = new Vector2(-10, 3);
            _line.color = SettingsPalette.Accent; _line.raycastTarget = false; Refresh();
        }
        public void OnSelect(BaseEventData e) { _selected = true; Refresh(); }
        public void OnDeselect(BaseEventData e) { _selected = false; Refresh(); }
        public void OnPointerEnter(PointerEventData e) { _hover = true; Refresh(); }
        public void OnPointerExit(PointerEventData e) { _hover = false; Refresh(); }
        private void Refresh() { if (_line != null) _line.enabled = _selected || _hover; }
    }
}
