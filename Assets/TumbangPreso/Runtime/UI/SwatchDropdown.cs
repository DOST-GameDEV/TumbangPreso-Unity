using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// A themed dropdown whose rows carry a colour swatch beside the label, built to match the
    /// Godot settings panel's colour pickers.
    ///
    /// ⚠️⚠️ A DROPDOWN, NOT A CYCLING BUTTON, AND THE DIFFERENCE IS THE SWATCHES. The first port
    /// of the landed-highlight row was a button that advanced through the palette and printed
    /// the colour's NAME. It worked, and it was the wrong control: the player is choosing a
    /// colour, and a control that says "Purple" in ink on wood shows them everything except the
    /// purple. The Godot original shows every option at once with the colour beside it, so the
    /// choice is made by eye rather than by reading. Do not simplify this back to a cycler.
    ///
    /// ⚠️ THE RADIO IS TWO GRAPHICS, NOT ONE. Unity's Toggle shows its `graphic` only while
    /// selected, so a single checkmark image gives an empty gap on every unselected row. The
    /// original draws an outlined circle on EVERY row and fills the selected one, which is what
    /// makes the list read as a set of choices rather than as a list with one stray mark. So the
    /// ring is the Toggle's `targetGraphic`, always drawn, and the dot is its `graphic`.
    ///
    /// ⚠️ AND A ROW MAY HAVE NO SWATCH. "Off" is a real entry in the palette rather than a
    /// sentinel beside it (see <see cref="Settings.SlipperHighlights"/>), and it has no colour to
    /// show. Passing a null swatch hides that row's image instead of drawing a black square,
    /// which is what a `default(Color)` would have done.
    /// </summary>
    public static class SwatchDropdown
    {
        /// <summary>One row: what it says, and the colour beside it if it has one.</summary>
        public readonly struct Option
        {
            public readonly string Label;
            public readonly Color? Swatch;

            public Option(string label, Color? swatch)
            {
                Label = label;
                Swatch = swatch;
            }
        }

        private const int FontSize = 22;
        private const float RowHeight = 44.0f;
        private const float SwatchSize = 26.0f;
        private const float RadioSize = 20.0f;
        private const float PadLeft = 14.0f;

        /// <summary>Ink on near-white, the same pairing the Godot panel uses for these rows.</summary>
        private static Color Face => UiTheme.Card;
        private static Color Ink => UiTheme.Ink;

        /// <summary>The unselected ring. Mid grey so it reads as "not chosen" without competing
        /// with the swatch beside it.</summary>
        // ⚠️ WARM, NOT COOL. It was (0.62, 0.64, 0.68), a blue-grey, which is `CLAUDE.md` § 6.4's
        // rule broken by a ring rather than by an outline. Same value, no blue in it.
        private static readonly Color RingGrey = new Color(0.66f, 0.62f, 0.56f, 1.0f);

        public static Dropdown Build(Transform parent, IList<Option> options, int initial,
                                     Vector2 size, Action<int> onChanged)
        {
            var go = new GameObject("SwatchDropdown");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;

            var face = go.AddComponent<Image>();
            face.sprite = GodotTheme.CardBox(Face, Ink);
            face.type = Image.Type.Sliced;

            var dropdown = go.AddComponent<Dropdown>();
            dropdown.targetGraphic = face;

            // --- the closed face: swatch, label, chevron ---------------------------------
            var caption = Swatch(go.transform, "CaptionSwatch");
            Anchor(caption.rectTransform, new Vector2(0.0f, 0.5f), new Vector2(PadLeft, 0.0f),
                   new Vector2(SwatchSize, SwatchSize));

            var captionText = MenuKit.Label(go.transform, "", FontSize, Ink,
                                            Vector2.zero, Vector2.zero, Vector2.zero,
                                            TextAnchor.MiddleLeft);
            captionText.name = "CaptionLabel";
            var ctr = captionText.rectTransform;
            ctr.anchorMin = new Vector2(0.0f, 0.0f);
            ctr.anchorMax = new Vector2(1.0f, 1.0f);
            ctr.offsetMin = new Vector2(PadLeft + SwatchSize + 10.0f, 0.0f);
            ctr.offsetMax = new Vector2(-34.0f, 0.0f);

            Chevron(go.transform);

            // --- the open list ------------------------------------------------------------
            var template = BuildTemplate(go.transform, options.Count, out var itemText,
                                         out var itemImage);

            dropdown.template = template;
            dropdown.captionText = captionText;
            dropdown.captionImage = caption;
            dropdown.itemText = itemText;
            dropdown.itemImage = itemImage;

            var data = new List<Dropdown.OptionData>();

            foreach (var o in options)
            {
                // ⚠️ THE SWATCH IS A SPRITE WITH THE COLOUR BAKED IN, not a tint. Unity's
                // OptionData carries a Sprite and no colour, and the item image is one shared
                // component reused for every row, so tinting it would paint whichever row drew
                // last. GodotTheme.Box caches by colour, so five colours is five small sprites
                // built once for the whole run.
                data.Add(new Dropdown.OptionData(o.Label,
                    o.Swatch.HasValue ? GodotTheme.Box(o.Swatch.Value, Ink, 2, 6) : null));
            }

            dropdown.options = data;
            dropdown.value = Mathf.Clamp(initial, 0, options.Count - 1);
            dropdown.RefreshShownValue();

            if (onChanged != null) dropdown.onValueChanged.AddListener(v => onChanged(v));

            return dropdown;
        }

        /// <summary>
        /// The list that drops open. Unity instantiates this at runtime, so it is built once,
        /// left INACTIVE, and never touched again.
        ///
        /// ⚠️ IT MUST BE INACTIVE. An active template renders as a second list stuck under the
        /// control at all times, which reads as the dropdown being permanently open.
        /// </summary>
        private static RectTransform BuildTemplate(Transform parent, int rows, out Text itemText,
                                                   out Image itemImage)
        {
            var template = new GameObject("Template");
            template.transform.SetParent(parent, false);

            var trt = template.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.0f, 0.0f);
            trt.anchorMax = new Vector2(1.0f, 0.0f);
            trt.pivot = new Vector2(0.5f, 1.0f);
            trt.anchoredPosition = new Vector2(0.0f, -4.0f);

            // Tall enough for every row, so the list never scrolls at this palette size.
            trt.sizeDelta = new Vector2(0.0f, rows * RowHeight + 12.0f);

            var bg = template.AddComponent<Image>();
            bg.sprite = GodotTheme.CardBox(Face, Ink);
            bg.type = Image.Type.Sliced;

            var scroll = template.AddComponent<ScrollRect>();

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(template.transform, false);
            var vrt = viewport.AddComponent<RectTransform>();
            MenuKit.Stretch(vrt);
            vrt.pivot = new Vector2(0.0f, 1.0f);
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var crt = content.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.0f, 1.0f);
            crt.anchorMax = new Vector2(1.0f, 1.0f);
            crt.pivot = new Vector2(0.5f, 1.0f);
            crt.anchoredPosition = new Vector2(0.0f, -6.0f);
            crt.sizeDelta = new Vector2(0.0f, RowHeight);

            // --- one row, which Unity clones per option ---------------------------------
            var item = new GameObject("Item");
            item.transform.SetParent(content.transform, false);

            var irt = item.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.0f, 0.5f);
            irt.anchorMax = new Vector2(1.0f, 0.5f);
            irt.sizeDelta = new Vector2(0.0f, RowHeight);

            var toggle = item.AddComponent<Toggle>();

            // ⚠️ THE ROW'S OWN BACKGROUND IS TRANSPARENT AND STILL HAS TO EXIST, because it is
            // what receives the click across the whole row. Without a graphic here only the
            // ring and the text are clickable and the row feels broken between them.
            var itemBg = NewImage(item.transform, "ItemBackground");
            MenuKit.Stretch(itemBg.rectTransform);
            itemBg.color = new Color(0, 0, 0, 0);
            itemBg.sprite = GodotTheme.Plain(6);
            itemBg.type = Image.Type.Sliced;

            var ring = NewImage(item.transform, "Ring");
            ring.sprite = GodotTheme.Box(RingGrey, RingGrey, 0, (int)(RadioSize * 0.5f));
            ring.type = Image.Type.Sliced;
            Anchor(ring.rectTransform, new Vector2(0.0f, 0.5f), new Vector2(PadLeft, 0.0f),
                   new Vector2(RadioSize, RadioSize));

            var dot = NewImage(item.transform, "Dot");
            dot.sprite = GodotTheme.Box(Ink, Ink, 0, (int)(RadioSize * 0.35f));
            dot.type = Image.Type.Sliced;
            Anchor(dot.rectTransform, new Vector2(0.0f, 0.5f), new Vector2(PadLeft, 0.0f),
                   new Vector2(RadioSize * 0.55f, RadioSize * 0.55f));

            toggle.targetGraphic = ring;   // always drawn
            toggle.graphic = dot;          // only while selected

            var swatch = Swatch(item.transform, "ItemSwatch");
            Anchor(swatch.rectTransform, new Vector2(0.0f, 0.5f),
                   new Vector2(PadLeft + RadioSize + 12.0f, 0.0f),
                   new Vector2(SwatchSize, SwatchSize));

            var label = MenuKit.Label(item.transform, "", FontSize, Ink,
                                      Vector2.zero, Vector2.zero, Vector2.zero,
                                      TextAnchor.MiddleLeft);
            label.name = "ItemLabel";
            var lrt = label.rectTransform;
            lrt.anchorMin = new Vector2(0.0f, 0.0f);
            lrt.anchorMax = new Vector2(1.0f, 1.0f);
            lrt.offsetMin = new Vector2(PadLeft + RadioSize + SwatchSize + 22.0f, 0.0f);
            lrt.offsetMax = new Vector2(-10.0f, 0.0f);

            scroll.content = crt;
            scroll.viewport = vrt;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 0.0f;

            itemText = label;
            itemImage = swatch;

            template.SetActive(false);
            return trt;
        }

        private static Image Swatch(Transform parent, string name)
        {
            var img = NewImage(parent, name);
            img.type = Image.Type.Sliced;
            img.preserveAspect = false;
            return img;
        }

        /// <summary>The little "open me" arrow. Drawn as a rotated square rather than shipping a
        /// glyph, so it needs no font and no asset.</summary>
        private static void Chevron(Transform parent)
        {
            var img = NewImage(parent, "Chevron");
            img.sprite = GodotTheme.Plain(2);
            img.type = Image.Type.Sliced;
            img.color = new Color(Ink.r, Ink.g, Ink.b, 0.55f);

            Anchor(img.rectTransform, new Vector2(1.0f, 0.5f), new Vector2(-20.0f, 3.0f),
                   new Vector2(11.0f, 11.0f));

            img.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
        }

        private static Image NewImage(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(anchor.x, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
        }
    }
}
