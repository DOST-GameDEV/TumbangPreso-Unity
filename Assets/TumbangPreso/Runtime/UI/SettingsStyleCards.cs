using System;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The Graphics tab's lighting style row: one picture card per <see cref="LightingStyles"/>
    /// slot, the picked one ringed in the accent. The owner's reference is PUBG Mobile's Style
    /// row (2026-09-25), a strip of same-camera thumbnails with a caption under each.
    ///
    /// ⚠️⚠️ A PICTURE, BECAUSE A LIGHTING STYLE CANNOT BE NAMED INTO SOMEBODY'S HEAD. "Standard"
    /// and "Nostalgic" are words; the difference between them is a frame. Every card is rendered
    /// from ONE camera on ONE map (`WorldCourtCueTests.LightingStyleThumbnails`), so what differs
    /// between two cards is the lighting and nothing else.
    ///
    /// ⚠️ IT IS THE ONLY SETTINGS ROW WITH ITS LABEL ABOVE THE CONTROL. Three cards at a readable
    /// size need the column's whole width; beside a 585-unit label they would be postage stamps.
    ///
    /// ⚠️ THE PLACEHOLDER IS DRAWN, DISABLED. A pad or a thumb can reach the two real cards and
    /// never the third, and a pick can only ever land on a style that draws something.
    ///
    /// Device paths: each card is a `Button`, so `ScreenFocus` gives it a pad focus stop and a
    /// thumb-sized hit area (each is well over the 144-unit floor); `SettingsControlFocus` draws
    /// the same focus underline every other settings control carries.
    /// </summary>
    public static class SettingsStyleCards
    {
        // ⚠️ THE ARITHMETIC. The settings column is 1240 units wide and every row's rule stops 15
        // short of it, at 1225, to clear the scrollbar. Three 376-unit cards with 20-unit gaps
        // take 1168, which leaves the selected card's 5-unit ring inside the rule too. The first
        // cut used 384 and 22 (1196) and its third card ran past the rule once measured against
        // the list's real width, photographed at 1920x1080. 376 x 211.5 is 16:9, the shape the
        // thumbnails are rendered at.
        private const float CardWidth = 376, ImageHeight = 211.5f, Caption = 56, Gap = 20, Label = 64;
        private const float Ring = 5;

        public static RectTransform Row(Transform list, string name, string label, int current, Action<int> picked)
        {
            var root = OwnerUiLayout.Rect(list, name);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = Label + ImageHeight + Caption + 30;
            var title = OwnerUiLayout.Text(root, "Label", label, 31, OwnerUiLayout.TypeRole.Reading);
            OwnerUiLayout.Place(title.rectTransform, 3, 0, 900, Label); title.color = SettingsPalette.Ink;
            var rule = OwnerUiLayout.Rect(root, "RowRule").gameObject.AddComponent<Image>();
            rule.rectTransform.anchorMin = Vector2.zero; rule.rectTransform.anchorMax = new Vector2(1, 0);
            rule.rectTransform.offsetMin = Vector2.zero; rule.rectTransform.offsetMax = new Vector2(-15, 1);
            rule.color = new Color(1, 1, 1, .12f); rule.raycastTarget = false;
            var rings = new Image[LightingStyles.All.Length];
            var captions = new Text[LightingStyles.All.Length];
            void Show(int selected)
            {
                for (int i = 0; i < rings.Length; i++)
                {
                    rings[i].enabled = i == selected;
                    captions[i].color = i == selected ? SettingsPalette.Accent : LightingStyles.All[i].Available ? SettingsPalette.Ink : SettingsPalette.Muted;
                }
            }
            for (int i = 0; i < LightingStyles.All.Length; i++)
            {
                int index = i; var entry = LightingStyles.All[i];
                var card = OwnerUiLayout.Rect(root, "LightingStyle" + i);
                OwnerUiLayout.Place(card, i * (CardWidth + Gap), Label, CardWidth, ImageHeight + Caption);
                var hit = card.gameObject.AddComponent<Image>(); hit.color = Color.clear;
                // The ring sits behind the picture and shows as a frame around it.
                var ring = OwnerUiLayout.Rect(card, "Selected").gameObject.AddComponent<Image>();
                OwnerUiLayout.Place(ring.rectTransform, -Ring, -Ring, CardWidth + Ring * 2, ImageHeight + Ring * 2);
                ring.color = SettingsPalette.Accent; ring.raycastTarget = false; rings[i] = ring;
                var well = OwnerUiLayout.Rect(card, "Picture").gameObject.AddComponent<Image>();
                OwnerUiLayout.Place(well.rectTransform, 0, 0, CardWidth, ImageHeight);
                well.color = SettingsPalette.Control; well.raycastTarget = false;
                var picture = entry.Thumbnail != null ? Resources.Load<Texture2D>(entry.Thumbnail) : null;
                if (picture != null)
                {
                    var art = OwnerUiLayout.Rect(well.transform, "Thumbnail").gameObject.AddComponent<RawImage>();
                    OwnerUiLayout.Fill(art.rectTransform); art.texture = picture; art.raycastTarget = false;
                }
                else if (!entry.Available)
                {
                    var empty = OwnerUiLayout.Text(well.transform, "Empty", "?", 96, OwnerUiLayout.TypeRole.Display);
                    OwnerUiLayout.Fill(empty.rectTransform); empty.alignment = TextAnchor.MiddleCenter; empty.color = SettingsPalette.Rule;
                }
                var caption = OwnerUiLayout.Text(card, "Caption", entry.Label, 30, OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(caption.rectTransform, 0, ImageHeight + 6, CardWidth, Caption - 6);
                caption.alignment = TextAnchor.MiddleCenter; captions[i] = caption;
                var button = card.gameObject.AddComponent<Button>(); button.targetGraphic = hit;
                button.transition = Selectable.Transition.None; button.interactable = entry.Available;
                if (entry.Available)
                {
                    card.gameObject.AddComponent<SettingsControlFocus>();
                    button.onClick.AddListener(() => { MenuSfx.Click(); Show(index); picked(index); });
                }
            }
            Show(current);
            return root;
        }
    }
}
