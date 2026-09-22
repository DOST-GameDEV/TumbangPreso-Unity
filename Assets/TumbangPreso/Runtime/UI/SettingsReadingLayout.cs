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
                child.GetComponent<LayoutElement>().preferredHeight = large ? 190 : 104;
                OwnerUiLayout.Place(label.rectTransform, 3, 9, large ? 1160 : 585, large ? 64 : 78);
                label.verticalOverflow = VerticalWrapMode.Overflow;
                control.anchorMin = control.anchorMax = control.pivot = new Vector2(1, large ? 1 : .5f);
                control.anchoredPosition = new Vector2(-20, large ? -90 : 0);
            }
            LayoutRebuilder.MarkLayoutForRebuild(list);
        }
    }
}
