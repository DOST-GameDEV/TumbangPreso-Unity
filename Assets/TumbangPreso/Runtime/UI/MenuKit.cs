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
        /// <summary>
        /// Which job a string is doing, which is the only thing that decides its face.
        ///
        /// ⚠️⚠️ THIS EXISTS BECAUSE ONE DISPLAY FACE WAS SETTING FOUR-LINE ABILITY DESCRIPTIONS.
        /// `docs/TODO.md` § 133 opens on 🧑's own diagnosis: **"I think the problem is we use the
        /// same font for everything"**, and § 132.8 is the render that proves it, arrived at from
        /// the other end after three rounds of chasing blurry text on the TAB tray.
        ///
        /// ⚠️ IT IS A ROLE, NOT A FONT, FOR `WoodCraft.Surface`'S REASON. § 6.5: *"pick a role,
        /// not a fill"*, and the failure that rule replaced was a screen of twelve plates that
        /// were one call with a different parameter. A caller that could pass a `Font` could pass
        /// the wrong one; a caller that passes a ROLE cannot.
        /// </summary>
        public enum Face
        {
            /// <summary>Darumadrop. A word somebody LOOKS AT: a screen heading, the `Display` and
            /// `Title` steps of <see cref="PaperKit"/>'s scale, the one primary action on a
            /// screen, a hero or player name, a big value, a pennant.</summary>
            Display,

            /// <summary>Work Sans. A word somebody READS: a sentence, a settings row, a
            /// caption, a chat line, a form field and its hint, a secondary button, a list
            /// row.</summary>
            Body,
        }

        private static Font _font;
        private static Font _body;
        private static Font _bodyBold;

        /// <summary>
        /// The display face.
        ///
        /// ⚠️ IT KEEPS THE NAME `Font` RATHER THAN BECOMING `DisplayFont`, and that is deliberate
        /// rather than lazy: every screen in the game reads this one static (§ 133.3 names it as
        /// the trap of this whole pass), so renaming it would have put a mechanical rename through
        /// forty files in the same commit as a palette change and a layout change, with no way to
        /// tell afterwards which of the three broke what.
        /// </summary>
        public static Font Font => _font != null
            ? _font
            : _font = Load("UI/fonts/DarumadropOne-Regular", "Darumadrop");

        /// <summary>Work Sans Regular. See <see cref="Face.Body"/>.</summary>
        public static Font BodyFont => _body != null
            ? _body
            : _body = Load("UI/fonts/WorkSans-Regular", "Work Sans Regular");

        /// <summary>
        /// Work Sans Bold, as a SEPARATE FILE rather than as a font style.
        ///
        /// ⚠️⚠️ THE WHOLE OF § 133 IS ABOUT THIS ONE LINE. Legacy `Text` given
        /// `FontStyle.Bold` on a face that ships no bold does not fail and does not warn: it
        /// draws every glyph twice at an offset, which is a SMEAR rather than a weight, and it
        /// is worst at <see cref="MinReadableUnits"/>, which is where most of the words in this
        /// game live. There were **42 of those** across the project when this landed.
        ///
        /// ⚠️ SO THE BOLD IS A FONT, NOT A STYLE, AND <see cref="Apply"/> IS THE ONLY CALLER.
        /// `CLAUDE.md` § 4a's argument: *"the answer is construction, not discipline."* A rule
        /// saying "do not write FontStyle.Bold" is a rule somebody forgets, and forgetting it
        /// compiles and even looks approximately right in a screenshot.
        /// </summary>
        public static Font BodyBoldFont => _bodyBold != null
            ? _bodyBold
            : _bodyBold = Load("UI/fonts/WorkSans-Bold", "Work Sans Bold");

        private static Font Load(string path, string human)
        {
            var loaded = Resources.Load<Font>(path);

            if (loaded != null) return loaded;

            // ⚠️ LOUD, BECAUSE THE FALLBACK IS SURVIVABLE AND WRONG. A missing face draws every
            // menu in `LegacyRuntime`, which is legible enough that it has shipped unnoticed
            // before: `SplashScreen`'s own note records the one line a player reads before
            // anything else being in the wrong face for exactly that reason.
            Debug.LogWarning($"[UI] {human} is missing from Resources/{path}; " +
                             "the menus will draw in the wrong face.");

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        /// <summary>
        /// Puts a label in the face its JOB asks for, and returns it so it can be chained.
        ///
        /// ⚠️⚠️ IT ALWAYS CLEARS `fontStyle`, AND THAT IS THE POINT OF THE FUNCTION. A caller
        /// asking for bold gets the bold FILE; a caller that had set `FontStyle.Bold` before this
        /// runs has it undone. There is no path through here that leaves Unity synthesising a
        /// weight, which is what makes the fault unreachable rather than merely fixed.
        ///
        /// ⚠️⚠️ AND BOLD ON <see cref="Face.Display"/> IS A DELIBERATE NO-OP RATHER THAN AN
        /// ERROR. Darumadrop ships one weight and there is nothing to reach for, so the honest
        /// answer is the regular face: it is already the heaviest thing on any screen it appears
        /// on. Throwing here would turn a cosmetic request into a crash on a screen that was
        /// drawing fine, and silently setting `FontStyle.Bold` is the exact bug this replaces.
        /// **If a display word needs more weight, it needs more SIZE**, which is what the type
        /// scale is for.
        ///
        /// ⚠️ IT DOES NOT TOUCH SIZE, COLOUR OR ALIGNMENT. Those are the caller's, and a helper
        /// that quietly restyled three properties when asked about one is how a shared kit stops
        /// being usable.
        /// </summary>
        public static Text Apply(Text label, Face face, bool bold = false)
        {
            if (label == null) return null;

            label.font = face == Face.Body
                ? (bold ? BodyBoldFont : BodyFont)
                : Font;

            label.fontStyle = FontStyle.Normal;

            return label;
        }

        /// <summary>
        /// <see cref="Apply"/> for the common case: a label that is being read rather than
        /// looked at.
        /// </summary>
        public static Text Read(Text label, bool bold = false) => Apply(label, Face.Body, bold);

        /// <summary>
        /// Takes the BLUE out of a text field's selection highlight and caret.
        ///
        /// ⚠️⚠️ EVERY `InputField` IN THIS GAME HIGHLIGHTED SELECTED TEXT IN LIGHT BLUE, AND IT
        /// HAD DONE SINCE THE FIRST ONE WAS BUILT. Unity's `InputField.selectionColor` defaults
        /// to `(168, 206, 255)`, which is `a8ceff`: more blue than red by 87 levels, and
        /// `CLAUDE.md` § 6.4's test is *"if a hex has more blue in it than red, it does not belong
        /// in a menu"*. Nothing in the project had ever assigned it, so the default shipped on the
        /// username row, the join-code box, the chat line and every sign-in field.
        ///
        /// ⚠️⚠️ AND IT IS EXACTLY THE FAULT CLASS § 6.4 SAYS TO GREP FOR RATHER THAN LOOK FOR.
        /// A selection highlight is only on screen while text is selected, so it appears in no
        /// render, no layout probe and no code review: `grep -rn selectionColor` returned NOTHING
        /// across the whole project, which is what found it. That section's own receipt is
        /// `UiTheme.Ink` being a near-black navy for the entire life of the file because nobody
        /// looked at the third channel.
        ///
        /// ⚠️ IT IS A FUNCTION RATHER THAN A LINE AT EACH OF THE FOUR SITES, for `CLAUDE.md`
        /// § 4a's reason: a fifth field added next month inherits the fix instead of inheriting
        /// the default. `PaperPurityProbe.NoFieldHighlightsInBlue` asserts it for the ones that
        /// exist.
        /// </summary>
        public static InputField Dress(InputField field)
        {
            if (field == null) return null;

            // ⚠️ PaperSunk AT PARTIAL ALPHA, which is the same sand a pressed paper control
            // darkens into. A selection is a temporary press on a run of words, so it reads as
            // one, and it stays under the ink rather than competing with it.
            var sand = UiTheme.PaperSunk;
            field.selectionColor = new Color(sand.r, sand.g, sand.b, 0.75f);

            // ⚠️ THE CARET HAS TO BE ASKED FOR EXPLICITLY. `caretColor` is ignored entirely
            // unless `customCaretColor` is true, so assigning it alone is a silent no-op and the
            // caret keeps drawing in the text colour.
            field.customCaretColor = true;
            field.caretColor = UiTheme.PaperInk;

            return field;
        }

        public static Canvas BuildCanvas(Transform parent, string name)
        {
            var go = new GameObject(name);

            // ⚠️⚠️ A TAKEOVER CANVAS IS BUILT AT THE SCENE ROOT WHEN ITS ASKED-FOR PARENT IS
            // INSIDE ANOTHER CANVAS, AND THE BOOT SCREEN 🧑 PHOTOGRAPHED IS WHY.
            // *"wtf is thhis shhit"*, 2026-08-31, with the account form floating over a fully lit
            // menu, no wood column and no key art. `docs/TODO.md` § 111.2 and
            // `NestedCanvasProbe` reproduce it in one picture.
            //
            // **A NESTED CANVAS IGNORES ITS OWN `CanvasScaler`.** Unity resolves scale on the ROOT
            // canvas only, so a nested one inherits the root's `scaleFactor` whatever its own
            // scaler says. Everything below in this method (the 1920x1080 reference, the match on
            // height, `AspectSafeCanvas.Apply`) is INERT on a nested canvas, and every offset,
            // column width and image fit the screen computes is then in the wrong unit space. The
            // probe measured `SignInCanvas` at `scaleFactor 0.711` with `isRootCanvas false`.
            //
            // ⚠️⚠️ AND § 99 IS THE SAME TRAP ONE PROPERTY OVER, HALF FIXED. That entry records
            // `sortingOrder` being silently ignored on a nested canvas and answers it with
            // `overrideSorting = true` below. **Nobody asked what else a nested canvas ignores**,
            // and the answer is the scaler. Detaching answers both at once and makes
            // `overrideSorting` redundant rather than load-bearing; it is kept because a caller
            // may still pass a root parent.
            //
            // ⚠️ THE OWNER IS NOT ABANDONED. `CanvasLifetime` destroys the detached canvas when
            // the object that asked for it goes, because `Destroy(owner)` no longer takes it.
            bool nested = parent != null && parent.GetComponentInParent<Canvas>() != null;

            if (nested)
            {
                var scene = parent.gameObject.scene;
                if (scene.IsValid())
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);

                go.AddComponent<CanvasLifetime>().Bind(parent.gameObject);
            }
            else
            {
                go.transform.SetParent(parent, false);
            }

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            // ⚠️⚠️ WITHOUT THIS, EVERY `sortingOrder` A CODE-BUILT SCREEN SETS IS SILENTLY
            // IGNORED, AND THAT SHIPPED. A `Canvas` nested inside another `Canvas` renders as
            // part of its PARENT's batch, in hierarchy order, and only honours its own
            // `sortingOrder` when `overrideSorting` is true. `PlayerNameplate`, `PlayerHub` and
            // `SignInScreen` all live on `ConvertedMainMenu`'s GameObject, which is inside
            // `MainMenuCanvas`, so **480, 500 and 510 were three numbers that did nothing.**
            //
            // ⚠️⚠️ 🧑 OPENED THE 2026-08-31 00:24 PLAYER AND GOT THE ACCOUNT FORM FLOATING OVER
            // A FULLY LIT TITLE SCREEN: *"i opened the game what the fuclk is this"*. The
            // sign-in screen's own 72 per cent scrim and its opaque wood column were both drawn
            // UNDER the menu's pennants and street, leaving only the labels that happened to land
            // later in the hierarchy. Nothing was wrong with that screen's layout: the same
            // screen renders correctly in `Logs/ui/09-signin-at-boot-windowed.png`, because a
            // probe builds it with no menu around it.
            //
            // ⚠️ AND `docs/TODO.md` § 92.7 ALREADY RECORDED THE SYMPTOM WITHOUT THE CAUSE. It
            // reads *"At sorting order 85 the hub had the MULTIPLAYER setup screen drawn through
            // it... The hub is 500 now and the sign-in screen 510."* Raising the numbers appeared
            // to fix it and cannot have: the numbers were inert. What actually changed that day
            // was which screen was loaded. **A fix that works for a reason nobody checked is a
            // fix that comes back**, and it came back here.
            canvas.overrideSorting = true;

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
            InputLayer.UiInputModule.Ensure();

            // ⚠️⚠️ EVERY CODE-BUILT SCREEN GETS ITS CONTROLLER FOCUS PATH AND ITS THUMB-SIZED HIT
            // AREAS HERE, BY CONSTRUCTION, AND THAT IS THE WHOLE FUTURE-PROOFING ARGUMENT. 🧑:
            // *"anytime we add a feature, make sure all controller and mobile is considered"*.
            // Every screen this game builds in code goes through this one method, so a screen
            // written next month inherits both without anybody remembering to ask for them.
            // `ScreenFocus`'s own note carries the three times a per-screen list went stale
            // (`docs/TODO.md` § 96, § 114, § 124.11) and `InputSurfaceCheck` refuses a source file
            // that builds a Canvas without coming through here.
            InputLayer.ScreenFocus.Install(go);

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

        /// <summary>
        /// How a game mode is written for a player to read.
        ///
        /// ⚠️⚠️ `GameMode.HeroStrike.ToString()` IS A WIRE VALUE AND UPPERCASING IT PRINTS
        /// `HEROSTRIKE`, WHICH IS NOT THE NAME OF ANYTHING. `docs/VISION.md` § 1 calls the mode
        /// **HERO STRIKE** and every other screen in the game writes it that way; the hub's match
        /// history and match detail were uppercasing the enum, so one screen out of five spelled
        /// the mode differently from the rest. Found in the first render of the MATCHES tab.
        ///
        /// ⚠️ IT TAKES THE STRING, NOT THE ENUM, ON PURPOSE. A `MatchRecord` carries `Mode` as
        /// text because it is stored and replayed and may hold a mode this build does not know;
        /// `FUTURE.md` § 0.5 rule 5 is the same argument about wire-facing identity. **An
        /// unknown mode is uppercased and shown rather than blanked**, so a record from a newer
        /// build reads as its own name instead of as nothing.
        /// </summary>
        public static string ModeLabel(string mode)
        {
            if (string.IsNullOrWhiteSpace(mode)) return "";
            return string.Equals(mode.Trim(), Core.GameMode.HeroStrike.ToString(),
                                 System.StringComparison.OrdinalIgnoreCase)
                ? "HERO STRIKE"
                : mode.Trim().ToUpperInvariant();
        }

        /// <summary>The same, from the enum the menus hold.</summary>
        public static string ModeLabel(Core.GameMode mode) => ModeLabel(mode.ToString());

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

            // ⚠️⚠️ A SUB-FLOOR RESULT REGISTERS ITSELF, AND THAT IS WHY THE MARKER IS ATTACHED
            // HERE RATHER THAN BY THE CALLER. `docs/TODO.md` § 126.13: three calls in
            // `ConvertedCharacterSelect` passed 14 as `floorSize`, so a label that did not fit was
            // allowed down to 14, and `AspectRatioProbes` **could not tell that from an authored
            // 14 that nobody meant** — which made the probe a permanent red that taught the next
            // reader to skim the results. The entry names the cause as *"a local exemption that
            // was copied twice and never encoded anywhere a test could see"*.
            //
            // ⚠️ THIS IS `CLAUDE.md` § 4a's ARGUMENT: *"the answer is construction, not
            // discipline."* A marker the caller had to remember to add is a second place to
            // forget, and forgetting it compiles. `Fit` is the only function in the project that
            // can produce a label below the floor, so it is the only place that can register one.
            //
            // ⚠️ AND IT REGISTERS THE RESULT, NOT THE REQUEST. A caller may pass a floor of 14 and
            // the string may still fit at 20, which is not an exemption and must not be recorded
            // as one: the list is meant to be short enough to read, and padding it with labels
            // that are perfectly legible is how a list stops being read.
            if (label.fontSize < MinReadableUnits)
            {
                var mark = label.GetComponent<TightLabel>();
                if (mark == null) mark = label.gameObject.AddComponent<TightLabel>();

                mark.Floor = floorSize;
                mark.Settled = label.fontSize;
                mark.Room = room;
            }

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
