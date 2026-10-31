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
        // Small cards, so berry inside a tan rim, lime when chosen (owner, 2026-10-30), right of the label.
        private const float CardWidth = 270, CardHeight = 164, Caption = 44, Gap = 18, Left = 340;

        public static RectTransform Row(Transform list, string name, string label, int current, Action<int> picked)
        {
            var root = OwnerUiLayout.Rect(list, name);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = CardHeight + Caption + 20;
            var title = OwnerUiLayout.Text(root, "Label", label, 36, OwnerUiLayout.TypeRole.Reading);
            OwnerUiLayout.Place(title.rectTransform, 3, 0, Left - 10, CardHeight + Caption); title.color = SettingsPalette.PanelInk; title.font = Hub.HubKnit.SentenceFont(true);
            var rule = OwnerUiLayout.Rect(root, "RowRule").gameObject.AddComponent<Image>();
            rule.rectTransform.anchorMin = Vector2.zero; rule.rectTransform.anchorMax = new Vector2(1, 0);
            rule.rectTransform.offsetMin = Vector2.zero; rule.rectTransform.offsetMax = new Vector2(-15, 4);
            rule.color = SettingsPalette.Rule; rule.raycastTarget = false;
            var rings = new Image[LightingStyles.All.Length];
            var captions = new Text[LightingStyles.All.Length];
            void Show(int selected)
            {
                for (int i = 0; i < rings.Length; i++)
                {
                    bool on = i == selected, real = LightingStyles.All[i].Available;
                    rings[i].enabled = on;
                    captions[i].color = on ? Hub.HubKnit.GreenInk : real ? SettingsPalette.PanelAccent : SettingsPalette.PanelHead;
                }
            }
            for (int i = 0; i < LightingStyles.All.Length; i++)
            {
                int index = i; var entry = LightingStyles.All[i];
                var card = OwnerUiLayout.Rect(root, "LightingStyle" + i);
                OwnerUiLayout.Place(card, Left + i * (CardWidth + Gap), 2, CardWidth, CardHeight + Caption);
                var hit = card.gameObject.AddComponent<Image>(); hit.color = Color.clear;
                var plain = OwnerUiLayout.Rect(card, "Frame").gameObject.AddComponent<Image>();
                OwnerUiLayout.Place(plain.rectTransform, 0, 0, CardWidth, CardHeight); Hub.HubKnit.KitFace(plain, entry.Available ? Hub.Knit.Card : Hub.Knit.Well);
                // The lime card over the berry one, shown only on the chosen style.
                var ring = OwnerUiLayout.Rect(card, "Selected").gameObject.AddComponent<Image>();
                OwnerUiLayout.Place(ring.rectTransform, 0, 0, CardWidth, CardHeight); Hub.HubKnit.KitFace(ring, Hub.Knit.CardOn); rings[i] = ring;
                var picture = entry.Thumbnail != null ? Resources.Load<Texture2D>(entry.Thumbnail) : null;
                if (picture != null)
                {
                    var art = OwnerUiLayout.Rect(card, "Thumbnail").gameObject.AddComponent<Hub.KitCutPicture>();
                    OwnerUiLayout.Place(art.rectTransform, 13, 13, CardWidth - 26, CardHeight - 41); art.texture = picture; art.raycastTarget = false;
                }
                else if (!entry.Available)
                {
                    var empty = OwnerUiLayout.Text(card, "Empty", "?", 72, OwnerUiLayout.TypeRole.Display);
                    OwnerUiLayout.Place(empty.rectTransform, 0, 8, CardWidth, CardHeight - 30); empty.alignment = TextAnchor.MiddleCenter; empty.color = SettingsPalette.Muted;
                }
                var caption = OwnerUiLayout.Text(card, "Caption", entry.Label.ToUpperInvariant(), 28, OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(caption.rectTransform, 0, CardHeight + 2, CardWidth, Caption - 2);
                caption.alignment = TextAnchor.MiddleCenter; captions[i] = caption;
                var button = card.gameObject.AddComponent<Button>(); button.targetGraphic = hit;
                button.transition = Selectable.Transition.None; button.interactable = entry.Available;
                if (entry.Available)
                {
                    card.gameObject.AddComponent<SettingsControlFocus>();
                    card.gameObject.AddComponent<Hub.KitPress>().Face = plain;
                    button.onClick.AddListener(() => { MenuSfx.Click(); Show(index); picked(index); });
                }
            }
            Show(current);
            return root;
        }
    }
}
