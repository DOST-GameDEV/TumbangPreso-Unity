using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Shared widgets for anything the game builds at runtime rather than converting.
    ///
    /// ⚠️⚠️ IT DRAWS THROUGH <see cref="GodotTheme"/> NOW, NOT WITH FLAT RECTANGLES. Every
    /// widget here used to be a plain Image in a palette colour, so a control built in code and
    /// a control converted from a `.tscn` sat side by side on the pause overlay looking like
    /// they came from two different games: one with a tan border, a 12px radius and a drop
    /// shadow, the other a brown box. The theme is the whole point of having a theme.
    ///
    /// ⚠️ THE DISPLAY FACE IS DARUMADROP, NOT THE BUILT-IN ONE. Godot sets it project-wide, so
    /// every string in the game is that face. Falling back to LegacyRuntime silently is how the
    /// converted screens ended up in a different typeface from the ported ones.
    /// </summary>
    public static class MenuKit
    {
        private static Font _font;

        public static Font Font
        {
            get
            {
                if (_font != null) return _font;

                _font = Resources.Load<Font>("UI/fonts/DarumadropOne-Regular");

                if (_font == null)
                {
                    Debug.LogWarning("[UI] Darumadrop is missing from Resources/UI/fonts; " +
                                     "the menus will draw in the wrong face.");
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return _font;
            }
        }

        public static Canvas BuildCanvas(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // ⚠️ MATCH ON HEIGHT, like the converted screens. Matching halfway makes a code-built
            // overlay drift against the converted screen underneath it on a non-16:9 monitor.
            scaler.matchWidthOrHeight = 1.0f;

            go.AddComponent<GraphicRaycaster>();

            // ⚠️ A MENU NEEDS AN EVENT SYSTEM OR NOTHING IS CLICKABLE, and the failure mode is
            // silent: the buttons draw perfectly and simply never respond.
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            return canvas;
        }

        public static Image Backdrop(Transform parent, Color color)
        {
            var go = new GameObject("Backdrop");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = color;
            Stretch(img.rectTransform);
            return img;
        }

        /// <summary>A label in one of the theme's named variations.</summary>
        public static Text Styled(Transform parent, string variation, string text,
                                  TextAnchor align = TextAnchor.MiddleCenter)
        {
            GodotTheme.TryText(variation, out var style);

            var t = Label(parent, text, style.Size, style.Colour, Vector2.zero, Vector2.zero,
                          Vector2.zero, align);

            if (style.Outline <= 0) return t;

            var ring = t.gameObject.AddComponent<GodotOutline>();
            ring.OutlineColour = style.OutlineColour;
            ring.Radius = Mathf.Max(1.0f, style.Outline * 0.5f);

            return t;
        }

        public static Text Label(Transform parent, string text, int size, Color color,
                                 Vector2 anchor, Vector2 offset, Vector2 boxSize,
                                 TextAnchor align = TextAnchor.MiddleCenter)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);

            var t = go.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            // See the note in TscnUiImporter.MakeText: this is Godot's BASELINE_OFFSET.
            t.alignByGeometry = true;

            if (boxSize != Vector2.zero) Place(t.rectTransform, anchor, offset, boxSize);
            return t;
        }

        /// <summary>
        /// A wood-faced button, with the five StyleBox states, the sink on press, and the two
        /// sounds. Identical to what the converter produces for a `WoodButton`.
        /// </summary>
        public static Button WoodButton(Transform parent, string text, Vector2 anchor,
                                        Vector2 offset, Vector2 size, Action onClick,
                                        string variation = "WoodButton")
        {
            var go = new GameObject($"Button_{text}");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            Place(img.rectTransform, anchor, offset, size);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var style = GodotTheme.ForButton(variation);

            var label = Label(go.transform, text, style.FontSize, style.Ink,
                              new Vector2(0.5f, 0.5f), Vector2.zero, size);
            label.raycastTarget = false;

            var skin = go.AddComponent<GodotButton>();
            skin.Variation = variation;

            if (onClick != null)
            {
                btn.onClick.AddListener(() => onClick());
            }

            return btn;
        }

        /// <summary>A wood panel with a vertical layout inside it, for a runtime-built card.</summary>
        public static VerticalLayoutGroup WoodPanel(Transform parent, string name,
                                                    string variation = "WoodPanel")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            go.AddComponent<Image>();

            var group = go.AddComponent<VerticalLayoutGroup>();
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = true;

            var skin = go.AddComponent<GodotPanel>();
            skin.Variation = variation;

            return group;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt, float inset = 0.0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-inset, -inset);
            rt.offsetMax = new Vector2(inset, inset);
        }
    }
}
