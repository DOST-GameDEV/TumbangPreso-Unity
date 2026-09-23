using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// A dropdown: a sticker showing the value and a chevron, which opens a list popup.
    ///
    /// ⚠️ A POPUP LIST RATHER THAN UNITY'S `Dropdown`, BECAUSE A PAD CAN WALK A POPUP. The popup has
    /// its own `ScreenFocus`, B closes it, and every option is a thumb-sized sticker; a stock dropdown
    /// opens a template outside the screen's focus path, which is the controller trap `CLAUDE.md` § 4a
    /// asks about ("how is it LEFT on a pad?").
    /// </summary>
    public sealed class HubDropdown : MonoBehaviour
    {
        public string[] Options = Array.Empty<string>();
        public int Value;
        public event Action<int> Changed;
        private Text _label;
        private TumpHub _hub;
        private string _title;
        public HubButton Button { get; private set; }

        public static HubDropdown Build(Transform parent, TumpHub hub, string name, string title, string[] options, int value, int seed)
        {
            var button = HubKit.Button(parent, name, null, HubStyle.Honey, null, 0, seed);
            var dropdown = button.gameObject.AddComponent<HubDropdown>();
            dropdown.Button = button;
            dropdown._hub = hub;
            dropdown._title = title;
            dropdown.Options = options;
            dropdown.Value = Mathf.Clamp(value, 0, options.Length - 1);
            dropdown._label = HubKit.Text(button.Body, "Label", "", HubStyle.Label, true, HubStyle.Ink, TextAnchor.MiddleLeft);
            HubKit.Stretch(dropdown._label.rectTransform);
            dropdown._label.rectTransform.offsetMin = new Vector2(26, 6);
            dropdown._label.rectTransform.offsetMax = new Vector2(-80, -6);
            var chevron = HubKit.Glyph(button.Body, "Chevron", HubGlyph.Mark.Down, HubStyle.Ink, 0.14f);
            HubKit.Place(chevron.rectTransform, HubKit.Right, new Vector2(-18, 0), new Vector2(48, 48));
            button.onClick.AddListener(dropdown.Open);
            dropdown.Draw();
            return dropdown;
        }

        public void Set(int value, bool notify)
        {
            Value = Mathf.Clamp(value, 0, Options.Length - 1);
            Draw();
            if (notify) Changed?.Invoke(Value);
        }

        private void Draw()
        {
            if (_label == null) return;
            _label.text = Options.Length > 0 ? Options[Value] : "";
            _label.fontSize = HubStyle.Size(HubStyle.Label);
            HubKit.Fit(_label);
        }

        private void Open()
        {
            _hub.Push<HubChoicePopup>(p =>
            {
                p.Title = _title;
                p.Options = Options;
                p.Current = Value;
                p.Chosen = i => Set(i, true);
            });
        }
    }

    /// <summary>The list a <see cref="HubDropdown"/> opens.</summary>
    public sealed class HubChoicePopup : HubScreen
    {
        public override bool IsPopup => true;
        public string Title = "";
        public string[] Options = Array.Empty<string>();
        public int Current;
        public Action<int> Chosen;

        public override void Build()
        {
            float row = 92, gap = 14;
            float height = 170 + Options.Length * (row + gap) + 30;
            var panel = HubCards.Panel(Root, this, Title, "", new Vector2(760, height));
            for (int i = 0; i < Options.Length; i++)
            {
                int index = i;
                var option = HubKit.Button(panel, "Option" + i, Options[i], i == Current ? HubStyle.Persimmon : HubStyle.Honey,
                                           () => { Chosen?.Invoke(index); Close(); }, HubStyle.Label, 800 + i);
                HubKit.Place((RectTransform)option.transform, HubKit.TopLeft, new Vector2(48, -(160 + i * (row + gap))), new Vector2(664, row));
            }
        }
    }

    /// <summary>A text field as a sticker plate. The InputField is Unity's; the look is ours.</summary>
    public static class HubField
    {
        public static InputField Build(Transform parent, string name, string value, string placeholder, int limit, int seed)
        {
            var root = HubKit.Rect(parent, name);
            var plate = HubKit.Shape(root, "Plate", HubStyle.Honey, false, seed, 5, 18);
            HubKit.Stretch(plate.rectTransform);
            plate.raycastTarget = true;

            var text = HubKit.Text(root, "Text", "", HubStyle.Label, true, HubStyle.Ink, TextAnchor.MiddleLeft);
            HubKit.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(24, 6);
            text.rectTransform.offsetMax = new Vector2(-24, -6);
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            var hint = HubKit.Text(root, "Placeholder", placeholder, HubStyle.Body, false,
                                   new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0.5f), TextAnchor.MiddleLeft);
            HubKit.Stretch(hint.rectTransform);
            hint.rectTransform.offsetMin = new Vector2(24, 6);
            hint.rectTransform.offsetMax = new Vector2(-24, -6);

            var field = root.gameObject.AddComponent<InputField>();
            field.targetGraphic = plate;
            field.textComponent = text;
            field.placeholder = hint;
            field.characterLimit = limit;
            field.caretColor = HubStyle.Ink;
            field.caretWidth = 3;
            field.selectionColor = new Color(HubStyle.Persimmon.r, HubStyle.Persimmon.g, HubStyle.Persimmon.b, 0.5f);
            field.transition = Selectable.Transition.ColorTint;
            var colours = field.colors;
            colours.normalColor = Color.white;
            colours.highlightedColor = new Color(1, 0.97f, 0.9f);
            colours.selectedColor = new Color(1, 0.94f, 0.85f);
            field.colors = colours;
            field.text = value ?? "";
            return field;
        }

        /// <summary>The label above a form row, in the reading face, capitals letterspaced by eye.</summary>
        public static Text Label(Transform parent, string words, Vector2 at, float width = 600)
        {
            var label = HubKit.Text(parent, "Label_" + words.Replace(" ", ""), words, HubStyle.Floor, false, HubStyle.Golden, TextAnchor.MiddleLeft);
            bool larger = Settings.SettingsStore.Current.LargerText;
            HubKit.Place(label.rectTransform, HubKit.TopLeft, at + new Vector2(0, larger ? 6 : 0), new Vector2(width, larger ? 48 : 40));
            return label;
        }
    }
}
