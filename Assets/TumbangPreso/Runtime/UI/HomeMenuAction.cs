using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    // Home navigation uses ink/underline feedback. It never scales its hit target.
    public sealed class HomeMenuAction : Button
    {
        private Text _label;
        private HomeMenuStroke _paint;
        private bool _focus, _pressed, _disabled;
        private float _shown;
        public void Configure(Text label, HomeMenuStroke paint) { _label = label; _paint = paint; }
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            _focus = state == SelectionState.Highlighted || state == SelectionState.Selected;
            _pressed = state == SelectionState.Pressed; _disabled = state == SelectionState.Disabled;
            if (instant) _shown = _focus || _pressed ? 1 : 0;
        }
        private void LateUpdate()
        {
            if (_label == null || _paint == null) return;
            float target = _focus || _pressed ? 1 : 0;
            _shown = Settings.SettingsStore.Current.ReducedUiMotion ? target
                : Mathf.MoveTowards(_shown, target, Time.unscaledDeltaTime * 10);
            var ink = _focus || _pressed ? OwnerUiTheme.Current.Green : OwnerUiTheme.Current.ActionInk;
            ink.a = _disabled ? .5f : 1; _label.color = ink;
            _paint.SetState(_shown, _pressed, _disabled);
        }
    }
}
