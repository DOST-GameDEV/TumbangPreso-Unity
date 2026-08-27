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
            canvas.pixelPerfect = true;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // ⚠️ MATCH ON HEIGHT, like the converted screens. Matching halfway makes a code-built
            // overlay drift against the converted screen underneath it on a non-16:9 monitor.
            scaler.matchWidthOrHeight = 1.0f;
            AspectSafeCanvas.Apply(scaler);

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

        /// <summary>
        /// Gives a slider one hit area covering its whole rect, and returns it.
        ///
        /// ⚠️⚠️ THIS IS THE FIX FOR "I CANT CHANGE VOLUME WITH MY MOUSE", AND THE SLIDER WAS
        /// NEVER RECEIVING A SINGLE POINTER EVENT. `TscnUiImporter.ClearStrayRaycastTargets`
        /// mutes every graphic that is not the `targetGraphic` of a Selectable ON ITS OWN NODE.
        /// A Button passes that test, because a Button's targetGraphic is the image beside it on
        /// the same GameObject. A Slider does not: Unity puts a Slider's Background, Fill and
        /// Handle on CHILD nodes, so all three were muted, the control was left with no raycast
        /// target anywhere beneath it, and a press at its centre hit the card behind it. It drew
        /// correctly, seeded correctly and reported its listener wired, which is why four
        /// sliders shipped dead and read as "hardcoded".
        ///
        /// ⚠️ THE HIT AREA IS THE WHOLE CONTROL, NOT THE GROOVE. The converted groove is a
        /// 14 px band centred in a 34 px row, so restoring the Background alone would have given
        /// the player a 14 px tall target to hit; a settings row is aimed at with a mouse in one
        /// pass and the rest of the row must count. It is a fully transparent Image: alpha plays
        /// no part in a graphic raycast (`Image.alphaHitTestMinimumThreshold` is 0 by default),
        /// so an invisible one takes the press and the Slider's own handler does the rest.
        ///
        /// ⚠️ FIRST SIBLING, so the artwork still draws over it, and no visual changes at all.
        ///
        /// ⚠️ AND IT IS IDEMPOTENT. This runs every time a panel wires itself, and a panel
        /// that is closed and reopened must not grow a new pad each time.
        /// </summary>
        public static Graphic EnsureHitArea(Slider slider)
        {
            if (slider == null) return null;

            // A graphic already on the slider's own node is the hit area by construction: the
            // importer keeps that one, because it is the one node the raycast sweep can see.
            var own = slider.GetComponent<Graphic>();

            if (own != null)
            {
                own.raycastTarget = true;
                return own;
            }

            var existing = slider.transform.Find(HitAreaName);

            var go = existing != null
                ? existing.gameObject
                : new GameObject(HitAreaName, typeof(RectTransform));

            if (existing == null)
            {
                go.transform.SetParent(slider.transform, false);
                go.transform.SetAsFirstSibling();
            }

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var pad = go.GetComponent<Image>();
            if (pad == null) pad = go.AddComponent<Image>();

            pad.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            pad.raycastTarget = true;

            return pad;
        }

        /// <summary>
        /// The same fix for a converted CheckBox, and it is the same bug one control across.
        ///
        /// ⚠️⚠️ "INVERT Y AND FULLSCREEN ARE UNCLICKABLE", 🧑 2026-08-27 with a screenshot of both
        /// rows. `TscnUiImporter.BuildCheckBox` puts the tick box on a CHILD node and points
        /// `Toggle.targetGraphic` at it, exactly as Unity does for a Slider's handle, so the
        /// Toggle's own GameObject carries no Graphic and the row has no hit area of its own.
        ///
        /// ⚠️⚠️ AND THE IMPORTER-SIDE FIX CANNOT REACH THE SHIPPED SCENES. `ClearStrayRaycastTargets`
        /// keeps a Selectable's `targetGraphic` alive now, but that runs at IMPORT time and writes
        /// a `.unity` asset; **running the player never re-runs the converter**, which is the
        /// identical reason `ConvertedSettingsPanel` calls the Slider overload above at runtime
        /// rather than trusting the bake. A row baked before that change is still muted on disk.
        ///
        /// ⚠️ AND THE WHOLE ROW BECOMES THE TARGET, NOT THE 30 px BOX. Restoring the box alone
        /// would leave the player aiming at a 30 px square at the far left of a 380 px row while
        /// the words next to it, which are what they are actually reading, do nothing. Every
        /// other settings row is pressed anywhere along it.
        /// </summary>
        public static Graphic EnsureHitArea(Toggle toggle)
        {
            if (toggle == null) return null;

            // ⚠️ THE TICK BOX IS UN-MUTED WHATEVER ELSE HAPPENS. It is the graphic Unity swaps
            // the pressed and disabled colours on, so a muted one also loses the press tint.
            if (toggle.targetGraphic != null) toggle.targetGraphic.raycastTarget = true;

            var own = toggle.GetComponent<Graphic>();
            if (own != null)
            {
                own.raycastTarget = true;
                return own;
            }

            var existing = toggle.transform.Find(HitAreaName);

            var go = existing != null
                ? existing.gameObject
                : new GameObject(HitAreaName, typeof(RectTransform));

            if (existing == null)
            {
                go.transform.SetParent(toggle.transform, false);
                go.transform.SetAsFirstSibling();
            }

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var pad = go.GetComponent<Image>();
            if (pad == null) pad = go.AddComponent<Image>();

            pad.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            pad.raycastTarget = true;

            return pad;
        }

        /// <summary>The name <see cref="EnsureHitArea"/> parks its pad under, so a reopened
        /// panel finds the one it made last time instead of stacking another.</summary>
        public const string HitAreaName = "HitArea";

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

        /// <summary>
        /// ⚠️⚠️ THE SMALLEST TYPE ANY SCREEN MAY USE, IN THE AUTHORED 1920x1080 SPACE. Every
        /// canvas scales down on a panel smaller or narrower than the reference, so a font size
        /// is not a pixel size: what a label ACTUALLY renders at is `fontSize x scaleFactor`,
        /// and the smallest scale the game supports is the 4:3 case at 1024x768, which is
        /// 768/1440 = 0.533 once AspectSafeCanvas stops the layout being cropped instead.
        ///
        /// So 18 units is the floor because 18 x 0.533 = 9.6 physical pixels, and Darumadrop is
        /// a rounded display face that stops resolving below roughly ten. The two character
        /// screen hint lines were authored at 14, which is 9.3 px at 720p and 7.5 px on a 4:3
        /// panel: small enough that the line reads as a smudge rather than as words.
        ///
        /// `AspectRatioProbes` asserts this floor across all nine supported resolutions, so a
        /// new label added below it fails a test rather than shipping.
        /// </summary>
        public const int MinReadableUnits = 18;

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

            // ⚠️ RE-APPLIED AFTER THE VARIATION IS SET. AddComponent runs OnEnable immediately,
            // which skins the button with the field's DEFAULT variation; assigning the real one
            // afterwards changes nothing on screen. That is why the settings panel's keycaps
            // came out as wood planks when they are meant to be the theme's light Button, the
            // one control on that screen that should read as a physical key.
            var skin = go.AddComponent<GodotButton>();
            skin.Variation = variation;
            skin.Apply();
            skin.Refresh();

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
