using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    // A settings-only control family. Account artwork and shared hub/touch rows stay separate.
    public static class SettingsWorkspaceRows
    {
        public const float ControlWidth = 620;

        public static RectTransform Row(Transform parent, string name, string label)
        {
            var root = OwnerUiLayout.Rect(parent, name); root.gameObject.AddComponent<LayoutElement>().preferredHeight = 104;
            var text = OwnerUiLayout.Text(root, "Label", label, 38, OwnerUiLayout.TypeRole.Reading);
            OwnerUiLayout.Place(text.rectTransform, 3, 9, 540, 78); text.color = SettingsPalette.PanelInk; text.font = Hub.HubKnit.SentenceFont(true);
            var line = OwnerUiLayout.Rect(root, "RowRule").gameObject.AddComponent<Image>();
            line.rectTransform.anchorMin = new Vector2(0, 0); line.rectTransform.anchorMax = new Vector2(1, 0);
            line.rectTransform.offsetMin = new Vector2(0, 0); line.rectTransform.offsetMax = new Vector2(-15, 4);
            line.color = SettingsPalette.Rule; line.raycastTarget = false;
            var control = OwnerUiLayout.Rect(root, "Control");
            control.anchorMin = control.anchorMax = control.pivot = new Vector2(1, .5f);
            control.anchoredPosition = new Vector2(-60, 0); control.sizeDelta = new Vector2(ControlWidth, 78);
            return control;
        }
        public static void Toggle(Transform parent, string name, bool current, Action<bool> changed)
        {
            // OFF / ON on the tab track. ON is the lime pill on the right; OFF a cream pill on the left,
            // so lime always means on.
            var root = OwnerUiLayout.Rect(parent, name); OwnerUiLayout.Place(root, 0, -1, 240, 80);
            var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var toggle = root.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = hit; toggle.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<SettingsControlFocus>();
            var track = OwnerUiLayout.Rect(root, "Track").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(track.rectTransform); Hub.HubKnit.KitFace(track, Hub.Knit.Track);
            var pill = OwnerUiLayout.Rect(root, "Switch").gameObject.AddComponent<Image>();
            var off = Word(root, "OffWord", "OFF", 20);
            var on = Word(root, "OnWord", "ON", 120);
            var text = OwnerUiLayout.Text(root, "Value", current ? "ON" : "OFF", 28, OwnerUiLayout.TypeRole.Display);
            text.enabled = false;
            void Show(bool value)
            {
                Hub.HubKnit.KitFace(pill, value ? Hub.Knit.PillGo : Hub.Knit.PillPlain);
                OwnerUiLayout.Place(pill.rectTransform, value ? 120 : 20, 4, 100, 59);
                pill.GetComponent<Hub.KitSlice>().Apply();
                on.color = value ? Hub.HubKnit.GreenInk : Hub.HubKnit.KitSoftRed;
                off.color = value ? Hub.HubKnit.KitSoftRed : Hub.HubKnit.KitMaroon;
                text.text = value ? "ON" : "OFF";
            }
            Show(current);
            toggle.SetIsOnWithoutNotify(current);
            toggle.onValueChanged.AddListener(value => { Show(value); MenuSfx.Click(); changed(value); });
            Text Word(Transform owner, string id, string words, float x)
            {
                var word = OwnerUiLayout.Text(owner, id, words, 28, OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(word.rectTransform, x, 4, 100, 54); word.alignment = TextAnchor.MiddleCenter;
                return word;
            }
        }
        public static Slider Slider(Transform parent, string name, float value, float minimum, float maximum, Action<float> changed, Func<float, string> show)
        {
            var root = OwnerUiLayout.Rect(parent, name); OwnerUiLayout.Place(root, 0, 0, ControlWidth, 78);
            var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var slider = root.gameObject.AddComponent<Slider>(); slider.minValue = minimum; slider.maxValue = maximum;
            root.gameObject.AddComponent<SettingsControlFocus>();
            var track = OwnerUiLayout.Rect(root, "Track").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(track.rectTransform, 0, 10, 500, 58); Hub.HubKnit.KitFace(track, Hub.Knit.Track);
            var fillArea = OwnerUiLayout.Rect(root, "FillArea"); OwnerUiLayout.Place(fillArea, 18, 18, 464, 36);
            var fill = OwnerUiLayout.Rect(fillArea, "Fill").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(fill.rectTransform); Hub.HubKnit.KitFace(fill, Hub.Knit.PillGo); slider.fillRect = fill.rectTransform;
            var handles = OwnerUiLayout.Rect(root, "HandleArea"); OwnerUiLayout.Place(handles, 18, 0, 464, 78);
            var handle = OwnerUiLayout.Rect(handles, "Handle").gameObject.AddComponent<Image>();
            handle.sprite = Hub.HubKnit.Knob(); handle.preserveAspect = true;
            handle.rectTransform.sizeDelta = new Vector2(64, 0); handle.raycastTarget = false;
            slider.handleRect = handle.rectTransform; slider.targetGraphic = handle; slider.transition = Selectable.Transition.None;
            var text = OwnerUiLayout.Text(root, "Value", show(value), 36, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(text.rectTransform, 508, 4, 112, 70); text.alignment = TextAnchor.MiddleCenter; text.color = SettingsPalette.PanelAccent;
            slider.SetValueWithoutNotify(value); slider.onValueChanged.AddListener(v => { text.text = show(v); changed(v); }); return slider;
        }
        public static InputField Entry(Transform parent, string name, string placeholder)
        {
            var root = OwnerUiLayout.Rect(parent, name).gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(root.rectTransform, 0, 0, ControlWidth, 78); Hub.HubKnit.KitFace(root, Hub.Knit.BootField); root.raycastTarget = true;
            var field = root.gameObject.AddComponent<InputField>(); field.targetGraphic = root; field.lineType = InputField.LineType.SingleLine; field.customCaretColor = true; field.caretColor = SettingsPalette.PanelInk;
            var typed = OwnerUiLayout.Text(root.transform, "EnteredText", "", 30); typed.color = SettingsPalette.PanelInk; typed.font = Hub.HubKnit.SentenceFont(true);
            OwnerUiLayout.Place(typed.rectTransform, 34, 2, 552, 70); typed.horizontalOverflow = HorizontalWrapMode.Overflow; field.textComponent = typed;
            var hint = OwnerUiLayout.Text(root.transform, "Placeholder", placeholder, 28); hint.color = SettingsPalette.PanelHead; hint.font = Hub.HubKnit.SentenceFont(false);
            OwnerUiLayout.Place(hint.rectTransform, 34, 2, 552, 70); field.placeholder = hint;
            return field;
        }
        public static Button Action(Transform parent, string name, string words, System.Action action, float width = ControlWidth)
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
            // A keycap is a quiet cream button; a command the yellow one. Both are the boot's button.
            var face = OwnerUiLayout.Rect(button.transform, "ChipFace").gameObject.AddComponent<Image>();
            face.rectTransform.SetAsFirstSibling(); Hub.HubKnit.KitFace(face, keycap ? Hub.Knit.Plain : Hub.Knit.Yellow);
            label.alignment = TextAnchor.MiddleCenter; label.font = InGameTypography.Resolve(OwnerUiTheme.Current.Display, true, label);
            Inks(button, Hub.HubKnit.InkOf(keycap ? Hub.Knit.Plain : Hub.Knit.Yellow));
            FitChip(button);
        }

        /// <summary>A full-width decision button face: filled accent for the one expected answer,
        /// an outline for the others. The label centres on it and takes the matching ink.</summary>
        public static void Slab(Button button, bool primary)
        {
            var label = button.GetComponentInChildren<Text>();
            var kind = primary ? Hub.Knit.Go : Hub.Knit.Plain;
            var face = OwnerUiLayout.Rect(button.transform, "SlabFace").gameObject.AddComponent<Image>();
            face.rectTransform.SetAsFirstSibling(); OwnerUiLayout.Fill(face.rectTransform); Hub.HubKnit.KitFace(face, kind);
            label.alignment = TextAnchor.MiddleCenter; label.font = InGameTypography.Resolve(OwnerUiTheme.Current.Display, true, label);
            OwnerUiLayout.Fill(label.rectTransform); label.rectTransform.anchorMin = new Vector2(0, .10f); label.fontSize = 38;
            Inks(button, Hub.HubKnit.InkOf(kind));
            button.gameObject.AddComponent<SettingsControlFocus>();
        }

        /// <summary>Gives a bare text action a kit button's face, place and ink.</summary>
        public static void KitAction(Button button, Hub.Knit kind, float x, float y, float w, float h, int size)
        {
            OwnerUiLayout.Place((RectTransform)button.transform, x, y, w, h);
            var face = OwnerUiLayout.Rect(button.transform, "KitFace").gameObject.AddComponent<Image>();
            face.rectTransform.SetAsFirstSibling(); OwnerUiLayout.Fill(face.rectTransform); Hub.HubKnit.KitFace(face, kind);
            var label = button.GetComponentInChildren<Text>();
            OwnerUiLayout.Fill(label.rectTransform); label.rectTransform.anchorMin = new Vector2(0, .10f);
            label.alignment = TextAnchor.MiddleCenter; label.fontSize = size; label.color = Hub.HubKnit.InkOf(kind);
        }

        /// <summary>A kit button's label stays its button's own dark ink; focus only deepens it.</summary>
        public static void Inks(Button button, Color ink)
        {
            var colours = button.colors;
            colours.normalColor = colours.pressedColor = ink;
            colours.highlightedColor = colours.selectedColor = Color.Lerp(ink, Color.black, .25f);
            colours.disabledColor = new Color(ink.r, ink.g, ink.b, .45f);
            button.colors = colours;
        }

        public static void FitChip(Button button)
        {
            var face = button.transform.Find("ChipFace") as RectTransform;
            var label = button.GetComponentInChildren<Text>();
            if (face == null || label == null) return;
            float width = Mathf.Clamp(label.preferredWidth + 96, 140, ControlWidth);
            face.anchorMin = face.anchorMax = face.pivot = new Vector2(0, .5f);
            face.anchoredPosition = new Vector2(0, 0); face.sizeDelta = new Vector2(width, 76);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = new Vector2(0, .5f);
            label.rectTransform.anchoredPosition = new Vector2(0, 4); label.rectTransform.sizeDelta = new Vector2(width, 60);
        }

        /// <summary>
        /// A group header inside a section: small accent capitals in the reading face. Settings
        /// pages over a handful of rows are grouped (the Overwatch/Valorant settings pattern), so a
        /// long list is scanned by its three or four headers instead of read row by row.
        /// </summary>
        public static Text Header(Transform list, string words)
        {
            var text = OwnerUiLayout.Text(list, "GroupHeader", words, 30, OwnerUiLayout.TypeRole.Display);
            text.color = SettingsPalette.PanelHead; text.alignment = TextAnchor.LowerLeft; text.raycastTarget = false;
            var box = text.gameObject.AddComponent<LayoutElement>(); box.preferredHeight = 60; box.minHeight = 60;
            return text;
        }
    }
}
