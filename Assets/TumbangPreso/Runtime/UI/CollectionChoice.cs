using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    // Icon-only collection. Selection and keyboard focus remain independently visible.
    public sealed class CollectionChoice : Button
    {
        public Image Face;
        public Graphic Check;
        private bool _picked, _focus;
        public void SetPicked(bool picked) { _picked = picked; Refresh(); }
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            _focus = state == SelectionState.Selected || state == SelectionState.Highlighted || state == SelectionState.Pressed;
            Refresh();
        }
        private void Refresh()
        {
            if (Face != null) Face.color = _picked ? new Color32(206, 215, 115, 255)
                : _focus ? new Color32(245, 234, 213, 255) : new Color32(85,57,43,255);
            var label = Face != null ? Face.GetComponentInChildren<Text>() : null;
            if (label != null) label.color = _picked || _focus ? OwnerUiTheme.Current.ActionInk : OwnerUiTheme.Current.Pale;
            if (Check != null) Check.gameObject.SetActive(_picked);
        }
    }
}
