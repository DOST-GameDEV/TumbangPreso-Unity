using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Forwards focus to a child surface while keeping the original wired Button.</summary>
    public sealed class StreetControl : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private Button _button;
        private StreetGraphic _surface;
        private bool _available = true;

        public void Bind(Button button, StreetGraphic surface)
        {
            _button = button;
            _surface = surface;
            Refresh();
        }

        public void OnSelect(BaseEventData data) => _surface?.OnSelect(data);
        public void OnDeselect(BaseEventData data) => _surface?.OnDeselect(data);

        private void Update()
        {
            if (_button != null && _available != _button.IsInteractable()) Refresh();
        }

        private void Refresh()
        {
            if (_button == null || _surface == null) return;
            _available = _button.IsInteractable();
            _surface.Available = _available;
            _surface.SetVerticesDirty();
            foreach (var label in GetComponentsInChildren<Text>(true))
                label.color = _surface.Style == StreetGraphic.Surface.Action
                    ? (_available ? UiTheme.Paper : UiTheme.PaperInk)
                    : (_available ? UiTheme.PaperInk : UiTheme.PaperInkSoft);
        }

        private void OnDisable()
        {
            if (_surface == null) return;
            _surface.OnDeselect(null);
            _surface.OnPointerExit(null);
        }
    }
}
