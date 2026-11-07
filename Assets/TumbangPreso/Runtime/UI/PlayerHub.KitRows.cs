using System;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The rows inside the player record's cream panel, drawn as `boot_record.py` draws them: a "+" group is a
    /// berry card with its title in cream and a tan hint, a value or action row is the same card with a tag or a
    /// kit button on its right, a field is the kit field with its label in maroon lettering, an empty list is a
    /// dark well with the message centred in it. The earlier builders stay as `*Painted`, not called.
    /// </summary>
    public sealed partial class PlayerHub
    {
        private const float CardGap = 6;   // the slab overlaps the column's spacing, so cards sit 20 apart as on the board

        /// <summary>A rect spanning its parent's width, centred on cy from the top, inset left and right.</summary>
        private static RectTransform Across(RectTransform rect, float left, float cy, float height, float right = 40)
        {
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(0, .5f);
            rect.offsetMin = new Vector2(left, -(cy + height / 2)); rect.offsetMax = new Vector2(-right, -(cy - height / 2));
            return rect;
        }

        private static RectTransform CardRow(Transform list, string name, float height)
        {
            var row = OwnerUiLayout.Rect(list, name);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height + CardGap;
            return row;
        }

        private static void FillTop(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(.5f, 1);
            rect.offsetMin = new Vector2(0, -(height + HubKnit.SlabHeight)); rect.offsetMax = Vector2.zero;
        }

        private bool Group(string title, string subtitle, bool openByDefault = true)
        {
            string key = _tab + "/" + title; if (!_groups.TryGetValue(key, out bool open)) open = openByDefault;
            const float h = 116;
            var row = CardRow(_list, "Group_" + title, h);
            var card = HubKnit.Button(row, "ToggleGroup", null, Knit.Card, () => { MenuSfx.Click(); _groups[key] = !open; Show(_tab); });
            FillTop((RectTransform)card.transform, h);
            var head = HubKnit.Display(card.Body, "GroupTitle", (open ? "- " : "+ ") + title.ToUpperInvariant(), 38, HubKnit.KitCream);
            Across(head.rectTransform, 40, h * .34f, 56);
            var hint = HubKnit.Words(card.Body, "GroupHint", subtitle, 30, HubKnit.KitTan, TextAnchor.MiddleLeft, true);
            Across(hint.rectTransform, 42, h * .72f, 44);
            return open;
        }

        private RectTransform HubValue(string label, string value, string detail = "", Color? tint = null, string portrait = null)
            => KitRow("Value_" + label, label, value, detail, tint, portrait, 0);

        /// <summary>A berry card row: the name in cream, the detail in tan under it, the value as a tag (or words when long) on the right.</summary>
        private RectTransform KitRow(string name, string label, string value, string detail, Color? tint, string portrait, float rightReserve)
        {
            bool hasDetail = !string.IsNullOrEmpty(detail);
            float h = hasDetail ? 124 : 96;
            var row = CardRow(_list, name, h);
            var card = HubKnit.Plate(row, "Card", Knit.Card);
            FillTop(card, h);
            var face = HubKnit.FaceOf(card);
            float inset = 40;
            if (!string.IsNullOrEmpty(portrait))
            {
                var image = OwnerPortraitArt.Create(face, "ValuePortrait", portrait);
                HubKnit.In(image.rectTransform, 24, (h - 84) / 2 - 2, 84, 84);
                inset = 128;
            }
            bool longValue = (value?.Length ?? 0) > 22;
            float valueWidth = string.IsNullOrEmpty(value) ? 0 : longValue ? 720 : 300;
            float right = 40 + Mathf.Max(valueWidth, rightReserve);
            var title = HubKnit.Display(face, "ValueName", (label ?? "").ToUpperInvariant(), 40, HubKnit.KitCream);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            Across(title.rectTransform, inset, hasDetail ? 46 : h / 2 - 3, 56, right);
            HubKit.Fit(title);
            if (hasDetail)
            {
                var hint = HubKnit.Words(face, "ValueDetail", detail, 32, HubKnit.KitTan, TextAnchor.MiddleLeft, true);
                Across(hint.rectTransform, inset + 2, 94, 44, right);
            }
            if (!string.IsNullOrEmpty(value))
            {
                if (longValue)
                {
                    var words = HubKnit.Words(face, "ValueText", value, 32, HubKnit.KitCream, TextAnchor.MiddleRight, true);
                    HubKit.Place(words.rectTransform, new Vector2(1, 1), new Vector2(-40, -(hasDetail ? 24 : 22)), new Vector2(700, 52));
                    words.rectTransform.pivot = new Vector2(1, 1);
                }
                else
                {
                    bool good = tint.HasValue && tint.Value == OwnerUiTheme.Current.Green;
                    var tag = HubKnit.Tag(face, "ValueText", value.ToUpperInvariant(), good ? Knit.PillGo : Knit.PillPlain, 60);
                    tag.anchorMin = tag.anchorMax = tag.pivot = new Vector2(1, 1);
                    tag.anchoredPosition = new Vector2(-40, -20);
                }
            }
            return row;
        }

        private Button HubAction(string name, string title, string action, Action click, string detail = "")
        {
            Knit k = KnitForAction(action);
            int size = action.Length > 10 ? 36 : 44;
            float w = Mathf.Max(260, action.Length * size * .62f + 90);
            var row = KitRow("Value_" + title, title, "", detail, null, null, w);
            var face = HubKnit.FaceOf(row.Find("Card"));
            var button = HubKnit.Button(face, name, action, k, () => { MenuSfx.Click(); click(); }, size);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-40, -16);
            rect.sizeDelta = new Vector2(w, 90);
            return button;
        }

        /// <summary>The verb's colour: going forward is lime, a destructive verb is red, the rest yellow.</summary>
        private static Knit KnitForAction(string action)
        {
            string a = (action ?? "").ToUpperInvariant();
            if (a.Contains("DELETE") || a == "REMOVE" || a == "DECLINE") return Knit.Danger;
            if (a == "PLAY" || a == "JOIN" || a == "ACCEPT" || a.EndsWith("PTS")) return Knit.Go;
            return Knit.Yellow;
        }

        private void HubNote(string text)
        {
            var label = HubKnit.Words(_list, "HubNote", text, 36, HubKnit.KitInk, TextAnchor.MiddleLeft, false);
            label.gameObject.AddComponent<TumpParagraph>();
        }

        /// <summary>An empty list: a dark well with its message centred in cream.</summary>
        private void HubEmpty(string text, float height = 290)
        {
            var row = CardRow(_list, "HubEmpty", height);
            var well = HubKnit.Plate(row, "Well", Knit.Well);
            FillTop(well, height);
            var words = HubKnit.Words(HubKnit.FaceOf(well), "HubNote", text, 36, HubKnit.KitCream, TextAnchor.MiddleCenter, true);
            HubKit.Stretch(words.rectTransform, 40);
        }

        private void HubChoice(string name, string label, string[] options, int selected, Action<int> choose)
        {
            var control = KitFieldRow(_list, name, label, 720);
            var choice = RecordChoice.Create(control, name + "Value", options, selected, choose);
            DressField((RectTransform)choice.transform, 720, true);
        }

        private InputField HubField(string key, string label, string value, int limit, string placeholder, OwnerUiTheme.Piece frame)
        {
            var field = RecordFields.Input(KitFieldRow(_list, key + "Row", label, 800), key, placeholder);
            DressField((RectTransform)field.transform, 800, false);
            field.characterLimit = limit; field.SetTextWithoutNotify(_ownerDraft.TryGetValue(key, out var draft) ? draft : value ?? "");
            field.interactable = !_ownerSaving; field.onValueChanged.AddListener(text =>
            {
                _ownerDraft[key] = text; _notice = "";
                if (_footerNote != null && _tab == Tab.Profile) _footerNote.text = "Unsaved profile changes.";
            });
            return field;
        }

        /// <summary>A field row: the label in maroon lettering on the left, a slot for the kit field on the right.</summary>
        private static RectTransform KitFieldRow(Transform list, string name, string words, float width)
        {
            const float h = 100;
            var row = OwnerUiLayout.Rect(list, name); row.gameObject.AddComponent<LayoutElement>().preferredHeight = h + 20;
            var label = HubKnit.Display(row, "Label", (words ?? "").ToUpperInvariant(), 40, HubKnit.KitMaroon);
            Across(label.rectTransform, 10, h / 2 + 10, 60, width + 40);
            var control = OwnerUiLayout.Rect(row, "Control");
            control.anchorMin = control.anchorMax = control.pivot = new Vector2(1, .5f);
            control.anchoredPosition = new Vector2(-10, 0); control.sizeDelta = new Vector2(width, h);
            return control;
        }

        /// <summary>Puts a record field or choice on the kit field: the face behind it, its rule hidden, its words in the kit's type.</summary>
        private static void DressField(RectTransform root, float width, bool choice)
        {
            const float h = 100;
            OwnerUiLayout.Place(root, 0, 0, width, h);
            var face = HubKnit.Plate(root, "FieldFace", Knit.BootField);
            HubKit.Stretch(face); face.SetAsFirstSibling();
            foreach (var rule in new[] { "EntryRule", "ChoiceRule" })
            {
                var line = root.Find(rule)?.GetComponent<Image>();
                if (line != null) line.color = Color.clear;
            }
            float inset = Mathf.Round(h * .42f);
            foreach (var part in new[] { "EnteredText", "Placeholder", "SelectedValue" })
            {
                var text = root.Find(part)?.GetComponent<Text>();
                if (text == null) continue;
                OwnerUiLayout.Place(text.rectTransform, inset, 4, width - inset - (choice ? 90 : 30), h - 12);
                text.alignment = TextAnchor.MiddleLeft;
                text.fontSize = 40;
                if (part == "SelectedValue") { text.font = HubStyle.DisplayFont; text.color = HubKnit.KitInk; }
                else { text.font = HubKnit.SentenceFont(true); text.color = part == "Placeholder" ? HubKnit.KitTanDark : HubKnit.KitInk; }
            }
            var arrow = root.Find("Expand");
            if (arrow != null)
            {
                arrow.gameObject.SetActive(false);
                var chevron = HubKnit.Display(root, "Chevron", "v", 44, HubKnit.KitMaroon, TextAnchor.MiddleCenter);
                HubKnit.In(chevron.rectTransform, width - h * .55f - 30, 0, 60, h - 6);
            }
        }
    }
}
