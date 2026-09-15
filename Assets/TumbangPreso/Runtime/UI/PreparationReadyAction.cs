using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed class PreparationReadyAction : Button
    {
        private PreparationReadyArt _art;
        private Text _label;
        public static PreparationReadyAction Create(Transform parent, string name, Action pressed)
        {
            var root = OwnerUiLayout.Rect(parent, name); OwnerUiLayout.Place(root, 1250, 967, 590, 85);
            var art = root.gameObject.AddComponent<PreparationReadyArt>();
            var button = root.gameObject.AddComponent<PreparationReadyAction>();
            button._art = art; button.targetGraphic = art; button.transition = Transition.None;
            button._label = OwnerUiLayout.Text(root, "Label", "START MATCH", 34, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(button._label.rectTransform, 18, 2, 485, 78);
            button._label.color = OwnerUiTheme.Current.Green; button._label.alignment = TextAnchor.MiddleCenter;
            button.onClick.AddListener(() => { MenuSfx.Click(); pressed?.Invoke(); });
            return button;
        }
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (_art == null) _art = GetComponent<PreparationReadyArt>();
            if (_art == null) return;
            _art.Focused = state == SelectionState.Highlighted || state == SelectionState.Selected;
            _art.Pressed = state == SelectionState.Pressed; _art.Disabled = state == SelectionState.Disabled;
            _art.SetVerticesDirty();
            if (_label != null) { var colour = OwnerUiTheme.Current.Green; colour.a = _art.Disabled ? .55f : 1; _label.color = colour; }
        }
    }
}
