using System.Collections.Generic;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Reflows settings into labelled rows with controls beneath at larger type sizes.</summary>
    public sealed class SettingsReadingLayout : MonoBehaviour
    {
        private readonly Dictionary<Text, int> _sizes = new Dictionary<Text, int>();
        public static void Apply(Canvas canvas, RectTransform list)
        {
            if (canvas == null) return;
            var layout = canvas.GetComponent<SettingsReadingLayout>();
            if (layout == null) layout = canvas.gameObject.AddComponent<SettingsReadingLayout>();
            layout.Refresh(list);
        }
        private void Refresh(RectTransform list)
        {
            bool large = SettingsStore.Current.LargerText;
            bool touch = Application.isMobilePlatform || InputLayer.TouchHud.ForceVisible;
            var dead = new List<Text>();
            foreach (var item in _sizes) if (item.Key == null) dead.Add(item.Key);
            foreach (var text in dead) _sizes.Remove(text);
            foreach (var text in GetComponentsInChildren<Text>(true))
            {
                if (!_sizes.TryGetValue(text, out int size)) _sizes[text] = size = text.fontSize;
                // Large display headings already exceed the reading size.
                text.fontSize = large && size < 50 ? Mathf.RoundToInt(size * 1.2f) : size;
            }
            if (list == null) return;
            foreach (Transform child in list)
            {
                var control = child.Find("Control") as RectTransform;
                var label = child.Find("Label")?.GetComponent<Text>();
                if (control == null || label == null) continue;
                // A phone's first editable row must remain above its keyboard.
                // Short entry labels still fit beside the larger text field.
                bool stacked = large && !(touch && control.GetComponentInChildren<InputField>() != null);
                child.GetComponent<LayoutElement>().preferredHeight = stacked ? (touch ? 252 : 190) : (touch ? 164 : 104);
                OwnerUiLayout.Place(label.rectTransform, 3, 9, stacked ? 1160 : 585, stacked ? 64 : 78);
                label.verticalOverflow = VerticalWrapMode.Overflow;
                control.anchorMin = control.anchorMax = control.pivot = new Vector2(1, stacked ? 1 : .5f);
                control.anchoredPosition = new Vector2(-20, stacked ? -90 : 0);
                if (touch)
                {
                    float target = InputLayer.TouchMetrics.MinTargetUnits;
                    control.sizeDelta = new Vector2(control.sizeDelta.x, target);
                    foreach (RectTransform widget in control)
                    {
                        if (widget.GetComponent<Selectable>() == null || widget.sizeDelta.y >= target) continue;
                        float extra = (target - widget.sizeDelta.y) * .5f;
                        widget.sizeDelta = new Vector2(widget.sizeDelta.x, target);
                        foreach (RectTransform part in widget)
                            if (part.anchorMin == Vector2.up && part.anchorMax == Vector2.up)
                                part.anchoredPosition += Vector2.down * extra;
                    }
                }
            }
            LayoutRebuilder.MarkLayoutForRebuild(list);
        }
    }
}
