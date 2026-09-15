using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed class PlayChoiceButton : Button
    {
        public PlayChoiceSurface Surface;
        private float _target;
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            _target = state == SelectionState.Highlighted || state == SelectionState.Selected || state == SelectionState.Pressed ? 1 : 0;
            if (instant && Surface != null) { Surface.Focus = _target; Surface.SetVerticesDirty(); }
        }
        private void LateUpdate()
        {
            if (Surface == null) return;
            float next = Settings.SettingsStore.Current.ReducedUiMotion ? _target
                : Mathf.MoveTowards(Surface.Focus, _target, Time.unscaledDeltaTime * 10);
            if (Mathf.Abs(next - Surface.Focus) < .001f) return;
            Surface.Focus = next; Surface.SetVerticesDirty();
        }
    }
}
