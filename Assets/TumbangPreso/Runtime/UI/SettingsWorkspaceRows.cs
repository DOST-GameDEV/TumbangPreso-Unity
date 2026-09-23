using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    // A settings-only control family. Account artwork and shared hub/touch rows stay separate.
    public static class SettingsWorkspaceRows
    {
        public static RectTransform Row(Transform parent, string name, string label)
        {
            var root = OwnerUiLayout.Rect(parent, name); root.gameObject.AddComponent<LayoutElement>().preferredHeight = 104;
            var text = OwnerUiLayout.Text(root, "Label", label, 31, OwnerUiLayout.TypeRole.Reading);
            OwnerUiLayout.Place(text.rectTransform, 3, 9, 585, 78); text.color = SettingsPalette.Ink;
            var line = OwnerUiLayout.Rect(root, "RowRule").gameObject.AddComponent<Image>();
            line.rectTransform.anchorMin = new Vector2(0, 0); line.rectTransform.anchorMax = new Vector2(1, 0);
            line.rectTransform.offsetMin = new Vector2(0, 0); line.rectTransform.offsetMax = new Vector2(-15, 1);
            line.color = new Color(1, 1, 1, .12f); line.raycastTarget = false;
            var control = OwnerUiLayout.Rect(root, "Control");
            control.anchorMin = control.anchorMax = control.pivot = new Vector2(1, .5f);
            control.anchoredPosition = new Vector2(-20, 0); control.sizeDelta = new Vector2(533, 78);
            return control;
        }
        public static void Toggle(Transform parent, string name, bool current, Action<bool> changed)
        {
            var root = OwnerUiLayout.Rect(parent, name); OwnerUiLayout.Place(root, 0, 0, 533, 78);
            var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var toggle = root.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = hit; toggle.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<SettingsControlFocus>();
            var face = OwnerUiLayout.Rect(root, "Switch").gameObject.AddComponent<SettingsSwitchFace>();
            OwnerUiLayout.Place(face.rectTransform, 14, 17, 95, 44); face.On = current; face.raycastTarget = false;
            var text = OwnerUiLayout.Text(root, "Value", current ? "ON" : "OFF", 30, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(text.rectTransform, 134, 4, 350, 68); text.color = SettingsPalette.Ink; toggle.SetIsOnWithoutNotify(current);
            toggle.onValueChanged.AddListener(on => { face.On = on; face.SetVerticesDirty(); text.text = on ? "ON" : "OFF"; MenuSfx.Click(); changed(on); });
        }
        public static Slider Slider(Transform parent, string name, float value, float minimum, float maximum, Action<float> changed, Func<float, string> show)
        {
            var root = OwnerUiLayout.Rect(parent, name); OwnerUiLayout.Place(root, 0, 0, 533, 78);
            var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var slider = root.gameObject.AddComponent<Slider>(); slider.minValue = minimum; slider.maxValue = maximum;
            root.gameObject.AddComponent<SettingsControlFocus>();
            var track = OwnerUiLayout.Rect(root, "Track").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(track.rectTransform, 17, 33, 360, 10); track.color = SettingsPalette.Rule; track.raycastTarget = false;
            var fill = OwnerUiLayout.Rect(track.transform, "Fill").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(fill.rectTransform); fill.color = SettingsPalette.Accent; fill.raycastTarget = false; slider.fillRect = fill.rectTransform;
            var handles = OwnerUiLayout.Rect(root, "HandleArea"); OwnerUiLayout.Place(handles, 17, 17, 360, 44);
            var handle = OwnerUiLayout.Rect(handles, "Handle").gameObject.AddComponent<SettingsSwitchFace>(); handle.KnobOnly = true;
            handle.rectTransform.sizeDelta = new Vector2(34, -10); handle.raycastTarget = false;
            slider.handleRect = handle.rectTransform; slider.targetGraphic = handle; slider.transition = Selectable.Transition.None;
            var text = OwnerUiLayout.Text(root, "Value", show(value), 30, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(text.rectTransform, 406, 4, 120, 68); text.alignment = TextAnchor.MiddleCenter; text.color = SettingsPalette.Ink;
            slider.SetValueWithoutNotify(value); slider.onValueChanged.AddListener(v => { text.text = show(v); changed(v); }); return slider;
        }
        public static InputField Entry(Transform parent, string name, string placeholder)
        {
            var root = OwnerUiLayout.Rect(parent, name).gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(root.rectTransform, 0, 0, 533, 78); root.color = SettingsPalette.Control;
            var field = root.gameObject.AddComponent<InputField>(); field.targetGraphic = root; field.lineType = InputField.LineType.SingleLine; field.customCaretColor = true; field.caretColor = SettingsPalette.Ink;
            var typed = OwnerUiLayout.Text(root.transform, "EnteredText", "", 31); typed.color = SettingsPalette.Ink;
            OwnerUiLayout.Place(typed.rectTransform, 21, 4, 491, 70); typed.horizontalOverflow = HorizontalWrapMode.Overflow; field.textComponent = typed;
            var hint = OwnerUiLayout.Text(root.transform, "Placeholder", placeholder, 28); hint.color = SettingsPalette.Ink;
            OwnerUiLayout.Place(hint.rectTransform, 21, 4, 491, 70); field.placeholder = hint;
            return field;
        }
        public static Button Action(Transform parent, string name, string words, System.Action action, float width = 533)
        {
            var root = OwnerUiLayout.Rect(parent, name); OwnerUiLayout.Place(root, 0, 0, width, 78);
            var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var label = OwnerUiLayout.Text(root, "Label", words, 30, OwnerUiLayout.TypeRole.Reading);
            OwnerUiLayout.Place(label.rectTransform, 18, 2, width - 36, 73); label.color = Color.white;
            var button = root.gameObject.AddComponent<Button>(); button.targetGraphic = label;
            var colours = button.colors; colours.normalColor = SettingsPalette.Ink;
            colours.highlightedColor = colours.selectedColor = SettingsPalette.Accent;
            colours.disabledColor = SettingsPalette.Muted; colours.pressedColor = SettingsPalette.Pressed;
            button.colors = colours; button.onClick.AddListener(() => { MenuSfx.Click(); action(); });
            return button;
        }

        /// <summary>
        /// Give a row's action a face: a KEYCAP for a binding, an outlined PILL for a command
        /// (OPEN, ARRANGE, RESET CONTROLS).
        ///
        /// ⚠️ UNTIL 2026-09-23 BOTH WERE BARE WORDS. "W" and "OPEN" sat in the control column as
        /// plain lettering, the same weight as the row labels, so nothing said they could be pressed
        /// (`CLAUDE.md` § 6.3: "a door is a thing that looks pressable") and a key did not look like a
        /// key. The face is sized to the lettering, recomputed by <see cref="FitChip"/> whenever the
        /// binding text changes, and never narrower than a thumb-sized 120 units.
        /// </summary>
        public static void Chip(Button button, bool keycap)
        {
            var label = button.GetComponentInChildren<Text>();
            var face = OwnerUiLayout.Rect(button.transform, "ChipFace").gameObject.AddComponent<Image>();
            face.rectTransform.SetAsFirstSibling(); face.raycastTarget = false;
            face.color = keycap ? SettingsPalette.Control : new Color(0, 0, 0, 0);
            var edge = face.gameObject.AddComponent<Outline>();
            edge.effectColor = keycap ? SettingsPalette.Rule : SettingsPalette.Accent; edge.effectDistance = new Vector2(2, -2);
            edge.useGraphicAlpha = false;   // the pill's face is transparent; see `TumpSettingsView.Build`
            if (keycap)
            {
                // The keycap's lower lip: a darker strip that makes it read as a key sitting up.
                var lip = OwnerUiLayout.Rect(face.transform, "KeyLip").gameObject.AddComponent<Image>();
                lip.rectTransform.anchorMin = Vector2.zero; lip.rectTransform.anchorMax = new Vector2(1, 0);
                lip.rectTransform.offsetMin = Vector2.zero; lip.rectTransform.offsetMax = new Vector2(0, 6);
                lip.color = SettingsPalette.Background; lip.raycastTarget = false;
            }
            label.alignment = TextAnchor.MiddleCenter; label.font = OwnerUiTheme.Current.Display;
            FitChip(button);
        }

        /// <summary>A full-width decision button face: filled accent for the one expected answer,
        /// an outline for the others. The label centres on it and takes the matching ink.</summary>
        public static void Slab(Button button, bool primary)
        {
            var label = button.GetComponentInChildren<Text>();
            var face = OwnerUiLayout.Rect(button.transform, "SlabFace").gameObject.AddComponent<Image>();
            face.rectTransform.SetAsFirstSibling(); OwnerUiLayout.Fill(face.rectTransform); face.raycastTarget = false;
            face.color = primary ? SettingsPalette.Accent : new Color(0, 0, 0, 0);
            var edge = face.gameObject.AddComponent<Outline>();
            edge.effectColor = primary ? SettingsPalette.OnAccent : SettingsPalette.Rule;
            edge.effectDistance = new Vector2(3, -3); edge.useGraphicAlpha = false;
            label.alignment = TextAnchor.MiddleCenter; label.font = OwnerUiTheme.Current.Display;
            OwnerUiLayout.Fill(label.rectTransform);
            if (primary)
            {
                var colours = button.colors;
                colours.normalColor = colours.highlightedColor = colours.selectedColor = SettingsPalette.OnAccent;
                colours.pressedColor = SettingsPalette.Background;
                button.colors = colours;
            }
            button.gameObject.AddComponent<SettingsControlFocus>();
        }

        public static void FitChip(Button button)
        {
            var face = button.transform.Find("ChipFace") as RectTransform;
            var label = button.GetComponentInChildren<Text>();
            if (face == null || label == null) return;
            float width = Mathf.Clamp(label.preferredWidth + 60, 120, 533);
            face.anchorMin = face.anchorMax = face.pivot = new Vector2(0, .5f);
            face.anchoredPosition = new Vector2(4, 0); face.sizeDelta = new Vector2(width, 64);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = new Vector2(0, .5f);
            label.rectTransform.anchoredPosition = new Vector2(4, 2); label.rectTransform.sizeDelta = new Vector2(width, 60);
        }

        /// <summary>
        /// A group header inside a section: small accent capitals in the reading face. Settings
        /// pages over a handful of rows are grouped (the Overwatch/Valorant settings pattern), so a
        /// long list is scanned by its three or four headers instead of read row by row.
        /// </summary>
        public static Text Header(Transform list, string words)
        {
            var text = OwnerUiLayout.Text(list, "GroupHeader", words, 28, OwnerUiLayout.TypeRole.Reading);
            text.color = SettingsPalette.Accent; text.alignment = TextAnchor.LowerLeft; text.raycastTarget = false;
            var box = text.gameObject.AddComponent<LayoutElement>(); box.preferredHeight = 74; box.minHeight = 74;
            // A short accent rule under the words: the eye jumps from header to header down a long
            // tab (Hi-Fi RUSH's red group strips, research finding 2) instead of reading the header
            // as one more row label.
            var rule = OwnerUiLayout.Rect(text.transform, "GroupRule").gameObject.AddComponent<Image>();
            rule.rectTransform.anchorMin = rule.rectTransform.anchorMax = rule.rectTransform.pivot = Vector2.zero;
            rule.rectTransform.anchoredPosition = new Vector2(0, -10); rule.rectTransform.sizeDelta = new Vector2(72, 4);
            rule.color = SettingsPalette.Accent; rule.raycastTarget = false;
            return text;
        }
    }
}
