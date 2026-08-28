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
        /// <summary>Room left either side of a button's label, so the words never touch the
        /// wooden border.</summary>
        public const float ButtonLabelPadding = 14.0f;

        /// <summary>
        /// How large a button's label should be, given the theme's size and the box it sits in.
        ///
        /// ⚠️⚠️ THE THEME'S `FontSizeButton` IS ONE NUMBER FOR EVERY BUTTON IN THE GAME, AND
        /// THAT IS WHAT READS AS UNBALANCED. 🧑 2026-08-29: *"and laki ng join at spectate button
        /// tas ang liit naman ng mga text hindi balanced"*. A 40 px chip and a 940 px browser row
        /// both got 18 units, so the small controls looked right and every large one looked like
        /// a big empty plank with a caption in the middle of it. The complaint is not that the
        /// type is too small OR that the boxes are too big; it is that the two do not move
        /// together, and only one of them was ever a variable.
        ///
        /// ⚠️ IT ONLY EVER GROWS. `Mathf.Max` against the theme size means no button anywhere
        /// gets SMALLER type than it has today, so this cannot regress a screen nobody reported.
        /// The floor is `MinReadableUnits` by construction because `FontSizeButton` is 18, which
        /// is that constant.
        ///
        /// ⚠️ 0.42 IS THE CAP HEIGHT A BUTTON LABEL WANTS, not a number picked to taste: the
        /// theme's own 18 units in the 40 px chips this game already ships is 0.45, and the
        /// 28-unit heading in a 64 px header row is 0.44. Applying the ratio the small controls
        /// already have to the large ones is what makes them one family. A 48 px row goes 18 to
        /// 20, and a 64 px button goes 18 to 26.
        /// </summary>
        public static int BalancedButtonUnits(int themeUnits, float boxHeight)
        {
            if (boxHeight <= 1.0f) return themeUnits;

            int wanted = Mathf.RoundToInt(boxHeight * 0.42f);
            return Mathf.Max(themeUnits, Mathf.Min(wanted, MaxButtonUnits));
        }

        /// <summary>
        /// ⚠️ A CEILING, BECAUSE A FULL-WIDTH BAR IS NOT A HEADLINE. Some lobby rows are 80 px
        /// tall to give a wooden plate presence, and 34-unit type in one would compete with the
        /// screen's actual heading. `GodotTheme.FontSizeHeading` is 28 and a button is never
        /// more important than a heading.
        /// </summary>
        public const int MaxButtonUnits = 28;

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

            var label = Label(go.transform, text, BalancedButtonUnits(style.FontSize, size.y),
                              style.Ink, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            label.raycastTarget = false;

            // ⚠️ AND THEN SHRUNK BACK IF THE WORDS ARE LONGER THAN THE BOX IS WIDE. Scaling type
            // to a button's HEIGHT says nothing about its WIDTH, and BACK TO LOBBY in a narrow
            // box would simply be bigger and clipped. `Fit` stops at `MinReadableUnits`, so the
            // two rules compose: fill the box when there is room, never go below the floor.
            if (size.x > 1.0f) Fit(label, size.x - (ButtonLabelPadding * 2.0f));

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

        /// <summary>
        /// Shrinks a label until it fits the width it was given, and reports whether it managed.
        ///
        /// ⚠️⚠️ THIS IS THE FIFTH TIME A STRING HAS RUN OUT OF ITS BOX IN THIS PROJECT AND THE
        /// FIRST TIME THE FIX IS SHARED. `ConvertedScreen.SetHeadline` records three of them in
        /// one session (the objective card's "-5 / SECOND" off the screen edge, the deck tile's
        /// "RECAST", the character ribbon's "CHOOSE YOUR HERO"), `GameVersion.ApplyTo` records the
        /// fourth (a 132 px corner label cut a branch name in half), and `docs/TODO.md` § 18 is a
        /// whole section of them. Every one was the same two facts:
        ///
        ///   1. Legacy `Text` defaults to WRAP, so an overflow is SILENT: the line simply
        ///      reflows into a box that has no room for a second line and the bottom half is
        ///      clipped rather than drawn somewhere obvious.
        ///   2. Every label `MenuKit` and the converter make is `Overflow` instead, which is
        ///      honest but draws straight past the edge.
        ///
        /// Neither is a size that fits. This measures through the component itself, which is what
        /// `Hud.WorstCaseNameWidth` and `SetHeadline` both do and for the same reason:
        /// `preferredWidth` is what THIS text, in THIS font, with THESE generator settings will
        /// actually lay out to, and a spare font metric is a different number.
        ///
        /// ⚠️ IT ONLY EVER SHRINKS, never grows, so a short string cannot inflate and change a
        /// row's height from screen to screen.
        ///
        /// ⚠️ AND IT STOPS AT <see cref="MinReadableUnits"/> RATHER THAN AT WHATEVER FITS.
        /// `AspectRatioProbes` fails a label below that floor, so shrinking past it to dodge an
        /// overflow would trade a visible bug for a failing test. When it cannot fit at the floor
        /// it returns false, and the caller must give it more room or fewer words. A caller that
        /// ignores the answer has an overflow it has been told about.
        /// </summary>
        public static bool Fit(Text label, float room, int floorSize = MinReadableUnits)
        {
            if (label == null) return true;

            // A rect that has not been laid out yet reports 0 and would drive the font to its
            // floor for no reason. Leaving the authored size alone is what shipped.
            if (room <= 1.0f) return true;

            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            while (label.fontSize > floorSize && label.preferredWidth > room)
                label.fontSize -= 1;

            return label.preferredWidth <= room;
        }

        /// <summary>
        /// <see cref="Fit"/> against the label's own rect, for the common case where the box is
        /// already the right size and only the type has to give.
        ///
        /// ⚠️ CALL IT AFTER A LAYOUT PASS. A label inside a `HorizontalLayoutGroup` has no width
        /// until the group has run, so measuring in the same frame it was built reads zero and
        /// this returns without doing anything. `Canvas.ForceUpdateCanvases()` first, or call it
        /// from the end of the frame.
        /// </summary>
        public static bool FitBox(Text label)
            => label == null || Fit(label, label.rectTransform.rect.width);

        /// <summary>
        /// The other half of the problem: a paragraph, which should WRAP and then be given as much
        /// height as the wrapping needs.
        ///
        /// ⚠️⚠️ `Fit` IS WRONG FOR PROSE AND WOULD SHRINK IT TO NOTHING. A hint line is 140
        /// characters; fitting that on ONE line inside a 500 px box would drive the type to the
        /// readable floor and still overflow, and it would be the wrong answer anyway, because a
        /// paragraph is supposed to be several lines. What it actually needs is the opposite of
        /// what a headline needs: wrap on, and then a box tall enough for the result.
        ///
        /// ⚠️⚠️ AND THE HEIGHT HAS TO REACH THE LAYOUT GROUP, NOT THE RECT. Inside a
        /// `VerticalLayoutGroup` the parent drives every child's rect during the layout pass, so
        /// setting `sizeDelta` here is overwritten within the frame. `LayoutElement.preferredHeight`
        /// is the only channel a child has to ask for room. Without it, `SeatHint` wrapped to three
        /// lines inside a box the group had sized for two and the last line was drawn under the
        /// seat rows: measured off `Logs/shots-runtime/Lobby-v1.png`, where the word "others" is
        /// half behind P1.
        ///
        /// ⚠️ IT IS CALLED AFTER A LAYOUT PASS. `preferredHeight` depends on the WIDTH the group
        /// gave the label, which is zero until the group has run at least once. See
        /// `ConvertedMatchSetup.FitEverything`.
        /// </summary>
        public static void FitBlock(Text label, float maxHeight = 0.0f)
        {
            if (label == null) return;

            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            float room = label.rectTransform.rect.width;
            if (room <= 1.0f) return;

            float wanted = label.preferredHeight;

            // ⚠️ SHRINK ONLY WHEN A CAP IS GIVEN AND THE PROSE BLOWS PAST IT. A caller with a
            // fixed slot (a nameplate, a row) has nowhere to grow into; one inside a vertical
            // group does, and shrinking its type there would be solving a problem it does not
            // have.
            if (maxHeight > 1.0f)
            {
                while (label.fontSize > MinReadableUnits && label.preferredHeight > maxHeight)
                    label.fontSize -= 1;

                wanted = Mathf.Min(label.preferredHeight, maxHeight);
            }

            var element = label.GetComponent<LayoutElement>();
            if (element == null) element = label.gameObject.AddComponent<LayoutElement>();

            element.preferredHeight = wanted;
            element.minHeight = wanted;
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
