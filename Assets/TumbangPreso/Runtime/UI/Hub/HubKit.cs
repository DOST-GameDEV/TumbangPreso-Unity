using System;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// Builders for the UX-1 screens. Layout helpers, lettering, stickers, plates, glyphs.
    ///
    /// ⚠️⚠️ NOTHING HERE MAKES A CANVAS. Every hub screen is a child of the one canvas `TumpHub`
    /// builds through `OwnerUiLayout.Canvas`, which installs `ScreenFocus` and is the kit
    /// `InputSurfaceCheck` accepts (`CLAUDE.md` § 4a). A popup gets its own `ScreenFocus` on its
    /// root through <see cref="Modal"/>, so a pad cannot walk out of it onto the screen behind.
    ///
    /// ⚠️ POSITIONS ARE CORNER-ANCHORED, NOT CENTRED IN A 1920x1080 DESIGN BOX. The canvas scales on
    /// `Expand`, so it is never under 1920x1080 units and is WIDER than that on the owner's 1600x680
    /// window and TALLER at 4:3. A HOME whose corners sit in a fixed box leaves a dead band at the
    /// window's edges on every shape but 16:9; anchoring to the corners puts the avatar in the
    /// corner the player looks at, at every shape.
    /// </summary>
    public static class HubKit
    {
        public static readonly Vector2 TopLeft = new Vector2(0, 1), TopRight = new Vector2(1, 1);
        public static readonly Vector2 BottomLeft = new Vector2(0, 0), BottomRight = new Vector2(1, 0);
        public static readonly Vector2 Centre = new Vector2(0.5f, 0.5f), Top = new Vector2(0.5f, 1);
        public static readonly Vector2 Bottom = new Vector2(0.5f, 0), Left = new Vector2(0, 0.5f), Right = new Vector2(1, 0.5f);

        /// <summary>The margin every screen keeps from the canvas edge.</summary>
        public const float Margin = 56.0f;

        /// <summary>
        /// The component on <paramref name="go"/>, added if absent.
        ///
        /// ⚠️⚠️ NEVER `GetComponent&lt;T&gt;() ?? AddComponent&lt;T&gt;()`. Unity answers a missing
        /// component with a fake null that `??` treats as an object, so the add never runs and the
        /// next line throws `MissingComponentException`. The first hub run died on exactly that.
        /// </summary>
        public static T Ensure<T>(GameObject go) where T : Component =>
            go.TryGetComponent(out T found) ? found : go.AddComponent<T>();

        public static RectTransform Rect(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.gameObject.layer = parent != null ? parent.gameObject.layer : 5;
            return rect;
        }

        /// <summary>Anchor and pivot at <paramref name="corner"/>, offset by <paramref name="position"/>
        /// (positive x goes right, positive y goes UP, as Unity does).</summary>
        public static RectTransform Place(RectTransform rect, Vector2 corner, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = corner;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect, float inset = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Centre;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        /// <summary>Fill a fraction of the parent: <paramref name="min"/> and <paramref name="max"/> are anchors.</summary>
        public static RectTransform Span(RectTransform rect, Vector2 min, Vector2 max, Vector2 insetMin = default, Vector2 insetMax = default)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = Centre;
            rect.offsetMin = insetMin;
            rect.offsetMax = -insetMax;
            return rect;
        }

        // ------------------------------------------------------------------ lettering

        public static Text Text(Transform parent, string name, string words, int step, bool display,
                                Color colour, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var text = Rect(parent, name).gameObject.AddComponent<Text>();
            text.font = display ? HubStyle.DisplayFont : HubStyle.ReadingFont;
            text.fontSize = HubStyle.Size(step);
            text.fontStyle = FontStyle.Normal;
            text.color = colour;
            text.text = words ?? "";
            text.alignment = align;
            text.alignByGeometry = display;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = display ? 0.9f : 1.0f;
            return text;
        }

        /// <summary>
        /// Shrink a single-line label until it fits its own box, never below the floor.
        ///
        /// ⚠️ A LEGACY `Text` OVERFLOWS SILENTLY, which is `docs/TODO.md` § 108's right-hand arrow
        /// drawn off the screen. Every name that comes from data (a hero, a map, a player's handle)
        /// goes through here, because its width is not known when the screen is designed.
        /// </summary>
        public static void Fit(Text text, float width = -1)
        {
            if (text == null) return;
            if (width <= 0) width = text.rectTransform.rect.width;
            if (width <= 1) return;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            while (text.fontSize > HubStyle.Floor && text.preferredWidth > width) text.fontSize -= 2;
        }

        // ------------------------------------------------------------------ surfaces

        public static HubShape Shape(Transform parent, string name, Color fill, bool pressable, int seed,
                                     float outline = 5.0f, float corner = 18.0f)
        {
            var shape = Rect(parent, name).gameObject.AddComponent<HubShape>();
            shape.Fill = fill;
            shape.Pressable = pressable;
            shape.Seed = seed;
            shape.OutlineWidth = outline;
            shape.Corner = corner;
            shape.raycastTarget = false;
            if (!pressable) shape.ShadowOffset = Vector2.zero;
            return shape;
        }

        /// <summary>Furniture: a rounded plate with no shadow and no press.</summary>
        public static HubShape Plate(Transform parent, string name, Color fill, int seed, float outline = 4.0f)
        {
            var plate = Shape(parent, name, fill, false, seed, outline, 22.0f);
            Stretch(plate.rectTransform);
            return plate;
        }

        public static HubGlyph Glyph(Transform parent, string name, HubGlyph.Mark mark, Color colour, float weight = 0.085f)
        {
            var glyph = Rect(parent, name).gameObject.AddComponent<HubGlyph>();
            glyph.Kind = mark;
            glyph.color = colour;
            glyph.Weight = weight;
            glyph.raycastTarget = false;
            return glyph;
        }

        public static Image Picture(Transform parent, string name, Sprite sprite, bool preserve = true)
        {
            var image = Rect(parent, name).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = preserve;
            image.raycastTarget = false;
            image.color = sprite != null ? Color.white : new Color(0, 0, 0, 0);
            return image;
        }

        public static Sprite Portrait(string id) =>
            string.IsNullOrEmpty(id) ? null : Resources.Load<Sprite>("UI/portraits/" + id)
                                               ?? SpriteFromTexture("UI/portraits/" + id);

        private static readonly System.Collections.Generic.Dictionary<string, Sprite> Made =
            new System.Collections.Generic.Dictionary<string, Sprite>();

        private static Sprite SpriteFromTexture(string path)
        {
            if (Made.TryGetValue(path, out var made) && made != null) return made;
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) return null;
            made = Sprite.Create(texture, new UnityEngine.Rect(0, 0, texture.width, texture.height), Centre);
            made.name = "Hub_" + path;
            Made[path] = made;
            return made;
        }

        // ------------------------------------------------------------------ controls

        /// <summary>
        /// A sticker button: an invisible raycast root, a moving body with the sticker and the
        /// lettering. <paramref name="seed"/> gives it its own cut, stable for this control.
        /// </summary>
        public static HubButton Button(Transform parent, string name, string words, Color fill,
                                       Action onClick, int step = HubStyle.Label, int seed = 0,
                                       HubGlyph.Mark icon = HubGlyph.Mark.None)
        {
            var root = Rect(parent, name);
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var button = root.gameObject.AddComponent<HubButton>();
            button.targetGraphic = hit;

            var body = Stretch(Rect(root, "Body"));
            button.Body = body;
            var shape = Shape(body, "Sticker", fill, true, seed == 0 ? name.GetHashCode() : seed);
            Stretch(shape.rectTransform);
            button.Shape = shape;

            Color ink = HubStyle.TextOn(fill);
            if (icon != HubGlyph.Mark.None)
            {
                var glyph = Glyph(body, "Icon", icon, ink);
                glyph.rectTransform.anchorMin = new Vector2(0, 0.5f);
                glyph.rectTransform.anchorMax = new Vector2(0, 0.5f);
                glyph.rectTransform.pivot = new Vector2(0, 0.5f);
                glyph.rectTransform.anchoredPosition = new Vector2(20, 0);
                glyph.rectTransform.sizeDelta = new Vector2(48, 48);
            }

            // ⚠️ NULL MEANS "NO LABEL" AND EMPTY MEANS "A LABEL FILLED IN LATER". HERO's primary is
            // built empty and named once the hero is known; treating empty as none left it with no
            // label to name, and the first hub run threw on it.
            if (words != null)
            {
                var label = Text(body, "Label", words, step, true, ink, TextAnchor.MiddleCenter);
                Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(icon != HubGlyph.Mark.None ? 72 : 18, 8);
                label.rectTransform.offsetMax = new Vector2(-18, -6);
            }

            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>A square sticker holding one glyph: BACK, the hamburger, +, X.</summary>
        public static HubButton IconButton(Transform parent, string name, HubGlyph.Mark mark, Color fill,
                                           Action onClick, int seed = 0)
        {
            var button = Button(parent, name, null, fill, onClick, HubStyle.Label, seed);
            var glyph = Glyph(button.Body, "Icon", mark, HubStyle.TextOn(fill), 0.11f);
            Stretch(glyph.rectTransform, 14);
            return button;
        }

        public static Text LabelOf(HubButton button) => button != null ? button.Body.Find("Label")?.GetComponent<Text>() : null;

        public static void SetLabel(HubButton button, string words)
        {
            var label = LabelOf(button);
            if (label == null) return;
            label.text = words;
            label.fontSize = HubStyle.Size(HubStyle.Label);
        }

        public static void SetFill(HubButton button, Color fill)
        {
            if (button == null || button.Shape == null) return;
            button.Shape.Fill = fill;
            button.Shape.Redraw();
            var label = LabelOf(button);
            if (label != null) label.color = HubStyle.TextOn(fill);
            var icon = button.Body.Find("Icon")?.GetComponent<HubGlyph>();
            if (icon != null) icon.color = HubStyle.TextOn(fill);
        }

        /// <summary>
        /// A dot on a door that has something new behind it: the SKILL TREE card, a finished task.
        /// ⚠️ Persimmon with an ink ring, the one marker colour (§ 4), sized 30 so it survives 960x540.
        /// </summary>
        public static GameObject Notice(Transform parent)
        {
            var dot = Shape(parent, "Notice", HubStyle.Persimmon, false, 9, 4.0f, 15.0f);
            Place(dot.rectTransform, TopRight, new Vector2(10, 10), new Vector2(30, 30));
            return dot.gameObject;
        }

        // ------------------------------------------------------------------ modal

        /// <summary>
        /// A popup's root: a full-canvas scrim that eats every press behind it, and a
        /// <see cref="ScreenFocus"/> so the pad's focus stays inside.
        ///
        /// ⚠️⚠️ THE SCRIM IS THE CLICK BLOCKER AND THAT IS ITS STATED JOB (`CLAUDE.md` § 6.2c,
        /// "if I delete this, what else was it doing"). It is also the dim the brief draws: the item
        /// popup "over the dimmed grid".
        /// </summary>
        public static RectTransform Modal(Transform parent, string name)
        {
            var root = Stretch(Rect(parent, name));
            var scrim = root.gameObject.AddComponent<Image>();
            scrim.color = HubStyle.Scrim;
            scrim.raycastTarget = true;
            ScreenFocus.Install(root.gameObject);
            return root;
        }

        /// <summary>The small prompt beside BACK: ESC, or the pad's east button, or nothing on touch.</summary>
        public static HubPrompt BackPrompt(Transform parent) => HubPrompt.Build(parent, "BackPrompt", "ESC", "BUTTON EAST");
    }
}
