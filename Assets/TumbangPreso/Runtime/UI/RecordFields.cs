using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public static class RecordFields
    {
        public static RectTransform Row(Transform parent, string name, string words)
        {
            var row = OwnerUiLayout.Rect(parent, name); row.gameObject.AddComponent<LayoutElement>().preferredHeight = 104;
            var label = OwnerUiLayout.Text(row, "Label", words, 32);
            OwnerUiLayout.Place(label.rectTransform, 3, 8, 861, 79); label.color = OwnerUiTheme.Current.EnteredInk;
            var field = OwnerUiLayout.Rect(row, "Control"); field.anchorMin = field.anchorMax = field.pivot = new Vector2(1, .5f);
            field.anchoredPosition = new Vector2(-24, 0); field.sizeDelta = new Vector2(533, 78); return field;
        }
        public static InputField Input(Transform parent, string name, string hint)
        {
            var root = OwnerUiLayout.Rect(parent, name); OwnerUiLayout.Place(root, 0, 0, 533, 78);
            var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var line = OwnerUiLayout.Rect(root, "EntryRule").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(line.rectTransform, 11, 72, 512, 3); line.raycastTarget = false;
            var field = root.gameObject.AddComponent<InputField>(); field.targetGraphic = line;
            field.lineType = InputField.LineType.SingleLine; field.customCaretColor = true; field.caretColor = OwnerUiTheme.Current.ActionInk;
            var colours = field.colors; colours.normalColor = new Color32(188, 135, 73, 255);
            colours.highlightedColor = colours.selectedColor = OwnerUiTheme.Current.Green; field.colors = colours;
            var text = OwnerUiLayout.Text(root, "EnteredText", "", 31); text.color = OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(text.rectTransform, 15, 4, 502, 66); text.horizontalOverflow = HorizontalWrapMode.Overflow; field.textComponent = text;
            var placeholder = OwnerUiLayout.Text(root, "Placeholder", hint, 28, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(placeholder.rectTransform, 15, 4, 502, 66); placeholder.color = OwnerUiTheme.Current.Ochre;
            field.placeholder = placeholder; return field;
        }
    }
}
