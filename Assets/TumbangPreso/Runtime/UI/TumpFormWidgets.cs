using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>New native form controls used by settings and room configuration.</summary>
    public static class TumpFormWidgets
    {
        public static RectTransform Row(Transform list, string name, string caption)
        {
            var row = TumpUiFactory.Rect(list, name);
            TumpUiFactory.Height(row, 100);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft; layout.spacing = 28;
            var title = TumpUiFactory.Text(row, "Label", caption, 32, true);
            TumpUiFactory.Height(title, 90, 450);
            var slot = TumpUiFactory.Rect(row, "Control");
            var le = slot.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = 84; le.flexibleWidth = 1;
            return slot;
        }
        public static Slider Slider(Transform parent, string name, float value, float min, float max, Action<float> changed, Func<float,string> format)
        {
            var f = TumpUiTheme.Current;
            var root = TumpUiFactory.Rect(parent, name); TumpUiFactory.Stretch(root);
            var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = min; slider.maxValue = max;
            var track = TumpUiFactory.Rect(root, "Track").gameObject.AddComponent<Image>();
            track.color = f.OliveSand; track.raycastTarget = false;
            track.rectTransform.anchorMin = new Vector2(0, .5f); track.rectTransform.anchorMax = new Vector2(1, .5f);
            track.rectTransform.offsetMin = new Vector2(24, -5); track.rectTransform.offsetMax = new Vector2(-154, 5);
            var fillArea = TumpUiFactory.Rect(track.transform, "FillArea"); TumpUiFactory.Stretch(fillArea);
            var fill = TumpUiFactory.Rect(fillArea, "Fill").gameObject.AddComponent<Image>();
            fill.color = f.HotOrange; fill.raycastTarget = false; TumpUiFactory.Stretch(fill.rectTransform);
            slider.fillRect = fill.rectTransform;
            var handles = TumpUiFactory.Rect(root, "HandleArea");
            handles.anchorMin = new Vector2(0, .5f); handles.anchorMax = new Vector2(1, .5f);
            handles.offsetMin = new Vector2(24, -28); handles.offsetMax = new Vector2(-154, 28);
            var knob = TumpUiFactory.Surface(handles, "Handle", TumpSurface.Form.Pebble, f.Lime);
            knob.rectTransform.sizeDelta = new Vector2(40, 56);
            slider.handleRect = knob.rectTransform; slider.targetGraphic = knob;
            var label = TumpUiFactory.Text(root, "Value", format(value), 32, true);
            label.rectTransform.anchorMin = new Vector2(1, 0); label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(-140, 0); label.rectTransform.offsetMax = Vector2.zero;
            label.alignment = TextAnchor.MiddleRight;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v => { label.text = format(v); changed?.Invoke(v); });
            return slider;
        }
        public static Toggle Toggle(Transform parent, string name, bool value, Action<bool> changed)
        {
            var f = TumpUiTheme.Current;
            var root = TumpUiFactory.Rect(parent, name); TumpUiFactory.Stretch(root);
            var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var toggle = root.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = hit;
            var box = TumpUiFactory.Surface(root, "ToggleBox", TumpSurface.Form.Pebble, f.Cream);
            TumpUiFactory.Place(box.rectTransform, 14, 10, 76, 64);
            var check = TumpUiFactory.Rect(box.transform, "Checked").gameObject.AddComponent<TumpSymbol>();
            check.Kind = TumpSymbol.Icon.Check; check.color = f.Brick; check.raycastTarget = false;
            TumpUiFactory.Stretch(check.rectTransform, 12);
            toggle.graphic = check;
            var label = TumpUiFactory.Text(root, "Value", value ? "On" : "Off", 32, true);
            TumpUiFactory.Place(label.rectTransform, 118, 4, 330, 76);
            toggle.SetIsOnWithoutNotify(value);
            toggle.onValueChanged.AddListener(v => { label.text = v ? "On" : "Off"; changed?.Invoke(v); });
            return toggle;
        }
        public static TumpChoice Choice(Transform parent, string name, string[] choices, int index, Action<int> changed)
        {
            var button = TumpUiFactory.Button(parent, name, "", null, TumpSurface.Form.Ticket, TumpUiTheme.Current.Apricot, 32);
            TumpUiFactory.Stretch((RectTransform)button.transform);
            var picker = button.gameObject.AddComponent<TumpChoice>();
            picker.Bind(choices, index, changed);
            return picker;
        }
    }
}
