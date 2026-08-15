using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Shared widgets for every menu, in the game's wood-panel style.
    ///
    /// ⚠️ THE LOOK IS THE TEAM'S BRAND, NOT JUST A UI SKIN. The same wood, cream and amber
    /// palette carries the pitch deck and the sponsorship proposals, so it is the thing a
    /// sponsor recognises. Restyle it freely, but keep it consistent with those documents
    /// rather than drifting to a Unity default.
    ///
    /// ⚠️ BUILT IN CODE, LIKE THE ARENA AND THE HUD. Menus authored by hand are binary scene
    /// blobs that cannot be diffed and cannot be regenerated when the palette moves. Everything
    /// here is a function, so a palette change is one edit and a rebuild.
    /// </summary>
    public static class MenuKit
    {
        public static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Canvas BuildCanvas(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

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

            Place(t.rectTransform, anchor, offset, boxSize);
            return t;
        }

        /// <summary>
        /// A wood-faced button.
        ///
        /// ⚠️ THE HOVER STATE IS THE LIT FACE, NOT A TINT. The palette carries WoodMid
        /// specifically as "the lit face, on hover", so hovering brightens the wood rather than
        /// washing the whole control, and the lettering goes amber. Doing it as a generic tint
        /// makes every control look like the same greyed-out widget.
        /// </summary>
        public static Button WoodButton(Transform parent, string text, Vector2 anchor,
                                        Vector2 offset, Vector2 size, Action onClick)
        {
            var go = new GameObject($"Button_{text}");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = UiTheme.WoodDeep;
            Place(img.rectTransform, anchor, offset, size);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.6f, 1.6f, 1.6f, 1.0f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1.0f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            var border = new GameObject("Edge");
            border.transform.SetParent(go.transform, false);
            var borderImg = border.AddComponent<Image>();
            borderImg.color = UiTheme.WoodEdge;
            borderImg.raycastTarget = false;
            Stretch(borderImg.rectTransform, 3.0f);
            border.transform.SetAsFirstSibling();

            var label = Label(go.transform, text, 32, UiTheme.Cream,
                              new Vector2(0.5f, 0.5f), Vector2.zero, size);
            label.raycastTarget = false;

            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
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
