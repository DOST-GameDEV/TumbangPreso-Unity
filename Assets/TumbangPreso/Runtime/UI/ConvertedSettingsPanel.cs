using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Ported from `settings_panel.gd`, driving the converted `SettingsPanel.tscn`.
    ///
    /// ⚠️⚠️ THE EDIT IS STAGED AND BACK DISCARDS IT. Every setter in the Godot build writes
    /// `settings.cfg` on its own, and `begin_edit()` is the only thing that stops it, so the
    /// APPLY / RESET ALL / BACK row is not decoration: BACK is the only control on the screen
    /// that can mean "no". A snapshot is taken when the panel opens and restored if the player
    /// leaves without applying, which also has to put the LIVE state back — a rebind is already
    /// in the input map and a volume is already on the bus.
    ///
    /// ⚠️ AND BACK ASKS FIRST, ONCE, ONLY WHEN THERE IS SOMETHING TO LOSE. The confirm is a
    /// second press of the same button rather than a dialog, because this panel is instanced
    /// into the title screen and has no popup layer of its own.
    /// </summary>
    public sealed class ConvertedSettingsPanel : ConvertedOverlay
    {
        private InputActionAsset _actions;
        private readonly Dictionary<string, Button> _rebindButtons = new Dictionary<string, Button>();
        private string _listening = "";
        private InputActionRebindingExtensions.RebindingOperation _rebindOp;

        private string _snapshot;
        private bool _backArmed;

        private Button _apply;
        private Button _back;

        /// <summary>Width of a rebind row's action name, so every key button lines up.</summary>
        private const float ActionLabelWidth = 260.0f;

        /// <summary>⚠️ SHARED BY THE KEYCAPS AND THE NAME FIELD, so the two grids cannot
        /// drift. It was reported once when they had.</summary>
        private static readonly Vector2 BindingControlSize = new Vector2(170.0f, 46.0f);

        protected override void Wire()
        {
            _actions = Resources.Load<InputActionAsset>("TumbangPreso");

            // ⚠️ LOAD THE OVERRIDES BEFORE DRAWING A SINGLE LABEL, or every row shows the
            // default and the player believes their rebind was forgotten.
            Rebinding.Load(_actions);

            _apply = FindButton("ApplyButton");
            _back = FindButton("BackButton");

            Snapshot();

            BuildRebindRows();

            // ⚠️ BUILT FIRST SO IT SITS FURTHEST FROM THE FULLSCREEN BOX, which is the bottom of
            // the picker stack. Every row here inserts directly under that box, so build order is
            // display order reversed (see the note below). This is not a display setting and must
            // not read as one; it is the last row on the panel, under its own sentence.
            BuildTelemetryRow();
            BuildSlipperHighlightRow();

            // ⚠️ THE BUILD ORDER IS THE DISPLAY ORDER, REVERSED. All three picker rows insert
            // themselves directly under `FullscreenCheck`, so the one built LAST ends up nearest
            // the box and the first built ends up furthest from it. Top to bottom the screen
            // therefore reads: fullscreen, render style, anti-aliasing, slipper highlight.
            //
            // Render style is nearest the box because it is the largest visual choice on the
            // panel by a distance: it decides whether the game has ink outlines at all. Then
            // anti-aliasing, which is also a display setting. The slipper highlight is an
            // accessibility colour rather than a display mode and reads fine below both.
            BuildAntiAliasRow();
            BuildVSyncRow();
            BuildRenderStyleRow();
            WireSliders();
            WireChecks();
            WireNameField();
            ConfigureScroll();

            OnClick("ResetAllButton", ResetAll);
            OnClick("ApplyButton", Apply);
            OnClick("BackButton", Back);

            SetText("SettingsStatusLabel", "");
            RefreshApplyState();
        
            // ⚠️⚠️ ONE CALL DRESSES THIS WHOLE SCREEN IN PAPER, AND IT IS SCOPED TO THIS SUBTREE
            // ON PURPOSE. `GodotPanel` and `GodotButton` are the choke points every converted
            // screen is skinned through, so editing either of them would have repainted the main
            // menu and the in-match HUD, which 🧑 scoped out twice. `PaperKit.PaperDress.Screen`
            // walks a given root instead. See `docs/TODO.md` § 119.2 and § 119.5.
            PaperDress.Screen(transform);
        }

        /// <summary>
        /// Everything that makes this list scrollable, and it is more than one line now.
        ///
        /// ⚠️⚠️ THE LIST IS TWICE THE HEIGHT OF ITS WINDOW AND NOTHING ON SCREEN SAID SO.
        /// 🧑, 2026-08-26, twice: *"make it easier to scroll thru settings bcz its so hard to"*
        /// and *"here its so weird to scroll in setttings here"*, with a screenshot of a row cut
        /// in half at the bottom edge. Three separate things were wrong and only the first is
        /// the one people reach for:
        ///
        ///  1. NO SCROLLBAR AT ALL. `ScrollRect.verticalScrollbar` was never assigned, so there
        ///     was no handle to drag, no indication that there was more below, and no way to tell
        ///     how much. A cut-off row is the only cue the panel gave, and a cut-off row reads as
        ///     a layout bug rather than as an invitation.
        ///  2. THE WHEEL WAS SET TO 45, which is about four rows a notch on a 46 px row. It was
        ///     commented "fast, smooth, responsive" and it is the first two: one notch skips a
        ///     whole group heading and its rows, which is exactly what "weird" describes. 24 is
        ///     two rows.
        ///  3. NO KEYBOARD. A settings screen with fifteen rebindable rows is one a player is
        ///     already using the keyboard on.
        ///
        /// ⚠️ THE BAR IS BUILT IN CODE BECAUSE THE .tscn HAS NO NODE FOR ONE. `TscnUiImporter`
        /// converts what Godot authored, and Godot's `ScrollContainer` draws its own bar from the
        /// theme rather than from a child node, so there was nothing to import. It is drawn in
        /// wood and amber (`docs/VISION.md` § 6) rather than in Unity's default grey.
        /// </summary>
        private void ConfigureScroll()
        {
            var scroll = GetComponentInChildren<ScrollRect>(true);
            if (scroll == null) return;

            _scroll = scroll;

            scroll.scrollSensitivity = WheelStep;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.horizontal = false;
            scroll.vertical = true;

            // ⚠️ INERTIA OFF. With it, a flick keeps sliding after the wheel stops and the row
            // under the cursor is not the row that ends up there.
            scroll.inertia = false;

            scroll.verticalNormalizedPosition = 1.0f; // Reset to top showing MOVEMENT / WASD

            if (scroll.viewport != null)
            {
                if (scroll.viewport.GetComponent<RectMask2D>() == null && scroll.viewport.GetComponent<Mask>() == null)
                    scroll.viewport.gameObject.AddComponent<RectMask2D>();
            }

            EnsureWheelReachesTheList(scroll);
            ScrollWheelRelay.Install(gameObject, scroll);

            if (scroll.verticalScrollbar == null) BuildScrollbar(scroll);
        }

        /// <summary>
        /// ⚠️⚠️ THE WHEEL, FOR THE FOURTH TIME, AND THIS IS THE CAUSE THE PREVIOUS THREE PASSES
        /// MISSED. 🧑 2026-08-27: *"u can scroll by holding scroll and yes i want to keep that
        /// feature but u cant scroll by using mouse scroll or laptop pad scroll ... repeated
        /// complaint! it feels so clunky/doesnt work at all"*. § 15.8 added a scrollbar and
        /// changed the wheel step, § 32.3 gave the slider rows a hit rectangle. Both were real
        /// and neither is this.
        ///
        /// ⚠️⚠️ UNITY DELIVERS A WHEEL EVENT TO WHATEVER THE POINTER IS OVER, AND THEN WALKS UP
        /// FROM IT. `StandaloneInputModule` takes `pointerCurrentRaycast.gameObject` and asks
        /// `GetEventHandler&lt;IScrollHandler&gt;` for the nearest ancestor that handles a scroll.
        /// **When the raycast hits nothing, there is no object to walk up from and the wheel is
        /// simply discarded.** `TscnUiImporter`'s `ScrollContainer` case adds a `ScrollRect` and a
        /// `RectMask2D` and NO GRAPHIC, and the content is a layout group with no graphic either,
        /// so the only raycastable pixels in the whole list are the row widgets themselves.
        ///
        /// The gaps between rows, the padding down both edges and the strip beside the scrollbar
        /// are all holes: the wheel works if the cursor happens to be over a key cap and does
        /// nothing one pixel above it. That is not "broken", which is why it survived three
        /// passes, and it is exactly what *"clunky"* describes.
        ///
        /// ⚠️ AN INVISIBLE FULL-RECT GRAPHIC AT THE BACK OF THE VIEWPORT IS THE FIX, and it is
        /// the same idiom § 32.3 used on the slider rows for the same reason. Alpha 0 draws
        /// nothing. `SetAsFirstSibling` keeps it behind the content, so it can never swallow a
        /// click meant for a row.
        /// </summary>
        private static void EnsureWheelReachesTheList(ScrollRect scroll)
        {
            var viewport = scroll.viewport;
            if (viewport == null) return;

            var existing = viewport.GetComponent<Graphic>();
            if (existing != null)
            {
                existing.raycastTarget = true;
                return;
            }

            // ⚠️ ON A CHILD RATHER THAN ON THE VIEWPORT ITSELF. `RectMask2D` lives on the
            // viewport here (the importer puts the mask on the scroll node, which is also the
            // viewport), and a `Graphic` on the same object as the ScrollRect would also become
            // the ScrollRect's own `targetGraphic` candidate for other tooling to trip over.
            var backing = new GameObject("WheelCatcher", typeof(RectTransform), typeof(Image));
            backing.transform.SetParent(viewport, false);
            backing.transform.SetAsFirstSibling();

            var rt = backing.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = backing.GetComponent<Image>();
            image.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            image.raycastTarget = true;

            // ⚠️ IT MUST NOT BE LAID OUT. The viewport is also the content's parent here, and a
            // full-rect child inside a layout group would be given a row of its own.
            var element = backing.AddComponent<LayoutElement>();
            element.ignoreLayout = true;
        }

        /// <summary>Two rows of the list per wheel notch. A row is
        /// <see cref="BindingControlSize"/>.y = 46 px.</summary>
        private const float WheelStep = 24.0f;

        /// <summary>How far one key press moves the list, in pixels.</summary>
        private const float KeyStep = 92.0f;

        private ScrollRect _scroll;

        /// <summary>
        /// A wood track with an amber handle, down the right-hand edge of the viewport.
        ///
        /// ⚠️ IT SHRINKS THE VIEWPORT BY ITS OWN WIDTH rather than floating over the rows. A
        /// bar drawn on top of the list covers the right end of every key cap, which is the one
        /// column on this screen a player is reading.
        /// </summary>
        private void BuildScrollbar(ScrollRect scroll)
        {
            var viewport = scroll.viewport;
            if (viewport == null) return;

            const float Width = 14.0f;

            var barGo = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image));
            barGo.transform.SetParent(scroll.transform, false);

            var barRt = barGo.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(1.0f, 0.0f);
            barRt.anchorMax = Vector2.one;
            barRt.pivot = new Vector2(1.0f, 0.5f);
            barRt.offsetMin = new Vector2(-Width, 0.0f);
            barRt.offsetMax = Vector2.zero;

            var track = barGo.GetComponent<Image>();
            track.color = new Color(UiTheme.WoodDark.r, UiTheme.WoodDark.g, UiTheme.WoodDark.b, 0.85f);

            var handleAreaGo = new GameObject("SlidingArea", typeof(RectTransform));
            handleAreaGo.transform.SetParent(barGo.transform, false);
            var areaRt = handleAreaGo.GetComponent<RectTransform>();
            areaRt.anchorMin = Vector2.zero;
            areaRt.anchorMax = Vector2.one;
            areaRt.offsetMin = new Vector2(2.0f, 2.0f);
            areaRt.offsetMax = new Vector2(-2.0f, -2.0f);

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.offsetMin = Vector2.zero;
            handleRt.offsetMax = Vector2.zero;
            handleGo.GetComponent<Image>().color = UiTheme.Amber;

            var bar = barGo.AddComponent<Scrollbar>();
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.handleRect = handleRt;
            bar.targetGraphic = handleGo.GetComponent<Image>();

            scroll.verticalScrollbar = bar;

            // ⚠️ PERMANENT, NOT AUTO-HIDE. The list is always longer than the window (fifteen
            // rebind rows, four sliders and the name field against about eleven rows of glass),
            // so auto-hide only ever costs the affordance that was missing in the first place.
            // `AutoHideAndExpandViewport` also asks the ScrollRect to drive the viewport rect,
            // which fights the sizes `TscnUiImporter` authored.
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scroll.verticalScrollbarSpacing = 0.0f;

            // ⚠️⚠️ THE ROWS ARE PADDED AWAY FROM THE BAR RATHER THAN THE VIEWPORT BEING SHRUNK,
            // AND THE FIRST VERSION DID THE OTHER ONE. The content is authored at a fixed width
            // out of the .tscn rather than stretched to its viewport, so moving the viewport's
            // right edge moved the window and left the rows where they were: the bar drew over
            // the right end of every key cap and cut the username field in half.
            // `Logs/shots-runtime/SettingsPanel.png` showed it in one frame.
            var group = scroll.content != null
                ? scroll.content.GetComponent<HorizontalOrVerticalLayoutGroup>()
                : null;

            if (group != null)
            {
                var pad = group.padding;
                pad.right = Mathf.Max(pad.right, (int)Width + 6);
                group.padding = pad;
            }
        }

        /// <summary>
        /// Page Up / Page Down / Home / End and the arrow keys move the list.
        ///
        /// ⚠️ IT IS REFUSED WHILE A REBIND IS LISTENING, because during one the whole keyboard
        /// belongs to the key being captured and scrolling on the press that is being recorded
        /// would move the list out from under the row you just clicked.
        /// </summary>
        private void StepScrollFromKeyboard()
        {
            if (_scroll == null || _scroll.content == null || _scroll.viewport == null) return;
            if (!string.IsNullOrEmpty(_listening)) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            float window = _scroll.viewport.rect.height;
            float travel = Mathf.Max(1.0f, _scroll.content.rect.height - window);
            float page = window * 0.85f;
            float move = 0.0f;

            if (keyboard.pageDownKey.wasPressedThisFrame) move -= page;
            if (keyboard.pageUpKey.wasPressedThisFrame) move += page;
            if (keyboard.downArrowKey.isPressed) move -= KeyStep * Time.unscaledDeltaTime * 12.0f;
            if (keyboard.upArrowKey.isPressed) move += KeyStep * Time.unscaledDeltaTime * 12.0f;

            if (keyboard.homeKey.wasPressedThisFrame)
            {
                _scroll.verticalNormalizedPosition = 1.0f;
                return;
            }

            if (keyboard.endKey.wasPressedThisFrame)
            {
                _scroll.verticalNormalizedPosition = 0.0f;
                return;
            }

            if (Mathf.Approximately(move, 0.0f)) return;

            _scroll.verticalNormalizedPosition =
                Mathf.Clamp01(_scroll.verticalNormalizedPosition + move / travel);
        }

        private Button FindButton(string node)
        {
            var t = Node(node);
            return t == null ? null : t.GetComponent<Button>();
        }

        private void Snapshot() => _snapshot = JsonUtility.ToJson(SettingsStore.Current);

        private bool Dirty() => JsonUtility.ToJson(SettingsStore.Current) != _snapshot;

        // --- Rebind rows -------------------------------------------------------------

        /// <summary>
        /// ⚠️ THE AUTHORED `PlayerNameRow` IS SKIPPED, NOT CLEARED. It is the first child of
        /// BindingsList in the scene, and an earlier version of this loop freed every child
        /// before rebuilding, which destroyed the name field in the same frame the code below
        /// wired it up. It read as "the username option disappeared" rather than as never built.
        /// </summary>
        private void BuildRebindRows()
        {
            var list = Node("BindingsList");
            if (list == null) return;

            for (int i = list.childCount - 1; i >= 0; i--)
            {
                var child = list.GetChild(i);
                if (child.name == "PlayerNameRow") continue;

                Destroy(child.gameObject);
            }

            foreach (var group in Rebinding.Groups)
            {
                BuildGroupHeading(list, group.Title);

                foreach (var action in group.Actions) BuildRebindRow(list, action);
            }
        }

        /// <summary>
        /// A group heading, and the one line of explanation under it where there is one.
        ///
        /// ⚠️ THE HEADING IS AMBER AND THE ROWS ARE NOT, which is the whole reason the grouping
        /// reads at a glance. A heading in the same weight as its rows is another row.
        /// </summary>
        private void BuildGroupHeading(Transform list, string title)
        {
            var headingGo = new GameObject($"{title}Heading");
            headingGo.AddComponent<RectTransform>();
            headingGo.transform.SetParent(list, false);

            var column = headingGo.AddComponent<VerticalLayoutGroup>();
            column.childControlHeight = true;
            column.childControlWidth = true;
            column.childForceExpandHeight = false;
            column.childForceExpandWidth = true;
            column.spacing = 0.0f;
            column.padding = new RectOffset(0, 0, 14, 4);

            var heading = MenuKit.Styled(headingGo.transform, "MenuBody", title,
                                         TextAnchor.MiddleLeft);
            heading.raycastTarget = false;
            heading.color = UiTheme.Amber;
            heading.fontStyle = FontStyle.Bold;
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 26.0f;

            string blurb = Rebinding.BlurbFor(title);
            if (string.IsNullOrEmpty(blurb)) return;

            var note = MenuKit.Styled(headingGo.transform, "MenuBody", blurb,
                                      TextAnchor.MiddleLeft);
            note.raycastTarget = false;
            note.color = UiTheme.CreamMuted;
            note.fontSize = MenuKit.MinReadableUnits;
            note.gameObject.AddComponent<LayoutElement>().preferredHeight = 20.0f;
        }

        private void BuildRebindRow(Transform list, string action)
        {
            {
                var rowGo = new GameObject($"{action}Row");
                rowGo.AddComponent<RectTransform>();
                rowGo.transform.SetParent(list, false);

                var row = rowGo.AddComponent<HorizontalLayoutGroup>();
                row.childControlHeight = true;
                row.childControlWidth = true;
                row.childForceExpandHeight = false;
                row.childForceExpandWidth = false;
                row.childAlignment = TextAnchor.MiddleLeft;
                row.spacing = 0.0f;

                var label = MenuKit.Styled(rowGo.transform, "MenuBody",
                                           Rebinding.LabelFor(action), TextAnchor.MiddleLeft);
                label.raycastTarget = false;

                var labelElement = label.gameObject.AddComponent<LayoutElement>();
                labelElement.preferredWidth = ActionLabelWidth;
                labelElement.minHeight = BindingControlSize.y;

                // ⚠️ DELIBERATELY THE THEME'S DEFAULT BUTTON, light fill with INK lettering. It
                // is the one control on this screen that should read as a physical keycap, and
                // inverting it to wood loses that.
                string capture = action;
                var button = MenuKit.WoodButton(rowGo.transform,
                    Rebinding.DisplayNameFor(_actions, action), Vector2.zero, Vector2.zero,
                    BindingControlSize, () => BeginRebind(capture), "Button");

                var buttonElement = button.gameObject.AddComponent<LayoutElement>();
                buttonElement.preferredWidth = BindingControlSize.x;
                buttonElement.preferredHeight = BindingControlSize.y;
                buttonElement.flexibleWidth = 1.0f;

                _rebindButtons[action] = button;
            }
        }

        // --- The landed-highlight colour ---------------------------------------------

        private Dropdown _highlight;
        private Dropdown _antiAlias;
        private Dropdown _vsync;
        private Dropdown _renderStyle;

        /// <summary>
        /// § THE LANDED HIGHLIGHT's colour picker, from `settings_panel.gd`.
        ///
        /// ⚠️ BUILT IN CODE AND PLACED NEXT TO THE FULLSCREEN BOX, rather than authored into
        /// `SettingsPanel.unity`. The scene is a conversion of the .tscn and gets rebaked by
        /// `TscnImporter`, so a row added to the asset by hand is a row that disappears the
        /// next time somebody reimports the maps. Everything on this screen that the importer
        /// does not own is built here, which is the same reason the rebind rows are.
        ///
        /// ⚠️⚠️ IT IS A LIST WITH SWATCHES, NOT A CYCLING BUTTON, AND THAT WAS A CORRECTION.
        /// The first version advanced through the palette on click and printed the colour's
        /// NAME. It worked and it was the wrong control: the player is picking a COLOUR, and a
        /// button reading "Purple" in ink on wood shows them everything except the purple. The
        /// Godot panel puts every option on screen at once with the colour beside it, so the
        /// choice is made by eye. See `SwatchDropdown`.
        /// </summary>
        private void BuildSlipperHighlightRow()
        {
            var options = new List<SwatchDropdown.Option>();

            foreach (var entry in SlipperHighlights.All)
            {
                // ⚠️ "Off" PASSES A NULL SWATCH rather than its stored colour. Row 0 holds black
                // as a placeholder because the palette is an array and every row needs a value;
                // drawing it would put a black chip beside "Off" that reads like a sixth colour.
                bool hasColour = entry.Label != "Off";
                options.Add(new SwatchDropdown.Option(
                    entry.Label, hasColour ? entry.Colour : (Color?)null));
            }

            _highlight = BuildDropdownRow("SlipperHighlightRow", "Slipper Highlight", options,
                                          SettingsStore.Current.SlipperHighlight,
                                          PickSlipperHighlight);
        }

        /// <summary>
        /// The anti-aliasing picker.
        ///
        /// ⚠️⚠️ IT IS ON THIS SCREEN AT ALL BECAUSE THE GAME HAD NO WAY TO ASK FOR IT. The
        /// sample count a player rendered at came from whichever of the six quality levels the
        /// platform default happened to select, four of the six carried none, and nothing in the
        /// game ever showed a quality level or let anybody change one. See
        /// <see cref="AntiAliasModes"/> for what was measured.
        ///
        /// ⚠️ AND IT IS A DROPDOWN WITH NO SWATCHES rather than the cycling button the first
        /// version of the highlight row used. Same reasoning inverted: the highlight is a colour
        /// and has to be shown, this is a list of five named modes and the only thing a player
        /// needs to see is which one is on and what else there is. A null swatch on every row
        /// hides the chip, which is the same path "Off" already takes.
        /// </summary>
        /// <summary>
        /// The telemetry opt-out, and the sentence that says what it is opting out of.
        ///
        /// ⚠️⚠️ THE SENTENCE IS NOT DECORATION, IT IS HALF THE FEATURE. `FUTURE.md` § 19.3 asks
        /// for *"a visible opt-out in Settings that is honoured completely, and a plain statement
        /// of what is collected"*. A switch with no explanation asks a player to make a decision
        /// with no information, and the honest answer here is short enough to fit on one line:
        /// counts, no names, nothing typed.
        ///
        /// ⚠️ AND "HONOURED COMPLETELY" MEANS `TelemetrySink` STOPS COUNTING, not just stops
        /// sending. An opt-out that only gates the upload leaves a buffer somebody could later
        /// decide to flush, which is the same thing as no opt-out.
        ///
        /// ⚠️ A DROPDOWN RATHER THAN A TICK BOX, and that is a reuse decision rather than a
        /// design one: every tick box on this panel is a node the importer owns, and a row added
        /// to `SettingsPanel.unity` by hand disappears the next time `TscnImporter` rebakes it.
        /// `BuildDropdownRow` is the built-in-code path three rows already use.
        /// </summary>
        private void BuildTelemetryRow()
        {
            var options = new List<SwatchDropdown.Option>
            {
                new SwatchDropdown.Option("OFF", null),
                new SwatchDropdown.Option("ON", null),
            };

            BuildDropdownRow("TelemetryRow", "Share Anonymous Stats", options,
                             SettingsStore.Current.TelemetryEnabled ? 1 : 0,
                             index =>
                             {
                                 // ⚠️ IT GOES THROUGH THE PANEL'S APPLY/BACK TRANSACTION LIKE
                                 // EVERY OTHER ROW, RATHER THAN SAVING ON THE PICK. `Snapshot`
                                 // captures the whole settings object as JSON and Back restores
                                 // it, so a row that wrote to disk immediately would be a row
                                 // Back cannot undo, on the one screen where every other control
                                 // can be.
                                 SettingsStore.Current.TelemetryEnabled = index == 1;
                                 RefreshApplyState();
                             });

            BuildTelemetryNote();
        }

        /// <summary>
        /// ⚠️ THE NOTE GOES IN AFTER THE ROW, SO IT LANDS ABOVE IT ON SCREEN. Rows insert under
        /// `FullscreenCheck` and the last one inserted is nearest the box, which puts this line
        /// directly under the picker it explains once the whole stack is reversed. Sizing it as
        /// its own row rather than as a second label inside the picker keeps the label column
        /// aligned with every other row on the panel, which is the alignment fault this file's
        /// `BuildDropdownRow` header already records being reported once.
        /// </summary>
        private void BuildTelemetryNote()
        {
            var anchor = Node("FullscreenCheck");
            if (anchor == null || anchor.parent == null) return;

            var noteGo = new GameObject("TelemetryNote");
            noteGo.AddComponent<RectTransform>();
            noteGo.transform.SetParent(anchor.parent, false);
            noteGo.transform.SetSiblingIndex(anchor.GetSiblingIndex() + 1);

            // ⚠️ THE ROW NEEDS ITS OWN LAYOUT GROUP, exactly as `BuildDropdownRow` gives its row
            // one. `MenuKit.Styled` parents the label to this object, and a child of a plain
            // RectTransform inside a vertical list is laid out by nothing at all: it collapses to
            // its default rect and the sentence is invisible while the row still takes its height,
            // which reads as a gap somebody left rather than as text that failed to draw.
            var layout = noteGo.AddComponent<HorizontalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            layout.childAlignment = TextAnchor.MiddleLeft;

            var text = MenuKit.Styled(noteGo.transform, "MenuBody",
                "Counts only: matches played, modes, maps, picks and frame rate. " +
                "No names, no chat, nothing you type.", TextAnchor.MiddleLeft);
            text.raycastTarget = false;
            text.fontSize = 18;

            // ⚠️⚠️ BOTH AXES, AND SETTING ONLY THE VERTICAL ONE DID NOTHING AT ALL. This block
            // used to set `verticalOverflow = Overflow` and stop, with a note saying the sentence
            // is *"two lines wide by design"*. **It was never two lines.** `MenuKit.Styled`
            // leaves `horizontalOverflow = Overflow`, so the text does not wrap, so it never
            // needs a second line, so allowing a second line changes nothing: the sentence simply
            // ran off the side of the panel. Measured 2026-08-30 by
            // `PhaseSurfaceLayoutProbe.TheTelemetryRowFitsItsBoxAtEveryShippedResolution` at
            // 1280x720: **795 px of text in a 688 px box, 107 px past the edge**, and nothing
            // errors, because Overflow is silent by construction.
            //
            // ⚠️⚠️ AND THE HALF THAT WENT IS THE HALF THAT MATTERS. The sentence is
            // *"Counts only: ... No names, no chat, nothing you type."* The clause that survives
            // at 720p is the list of what IS collected; the promise about what is not is the part
            // off the edge. `FUTURE.md` § 19.3 asks for *"a plain statement of what is
            // collected"*, and **a privacy disclosure that is silently truncated is worse than
            // one that is absent**, because the reader has no way to know they are seeing half.
            //
            // ⚠️ `Wrap` PLUS `Overflow` IS THE PAIR. Wrap alone would clip the second line to the
            // 48 px box, which is the wrap-and-clip trap `GameVersion.ApplyTo` and
            // `ConvertedScreen.SetHeadline` both record. Wrapping makes the lines; vertical
            // overflow lets them be drawn.
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            // ⚠️ 56 RATHER THAN 48, because two 18-unit lines plus their leading do not fit in
            // 48 and the row below would be drawn through. The label may still overflow this box
            // downward by design; the height is what the LIST reserves for it.
            var element = noteGo.AddComponent<LayoutElement>();
            element.minHeight = 56.0f;
            element.preferredHeight = 56.0f;
        }

        private void BuildAntiAliasRow()
        {
            var options = new List<SwatchDropdown.Option>();

            foreach (var entry in AntiAliasModes.All)
                options.Add(new SwatchDropdown.Option(entry.Label, null));

            _antiAlias = BuildDropdownRow("AntiAliasRow", "Anti-Aliasing", options,
                                          SettingsStore.Current.AntiAliasMode, PickAntiAlias);
        }

        /// <summary>
        /// The vertical sync picker.
        ///
        /// ⚠️ IT APPLIES ON THE PICK, like anti-aliasing and the render style. Tearing and judder
        /// are the entire content of this setting and neither can be judged from a label, so a
        /// value that only took effect on APPLY would make the choice guesswork. `SettingsStore`'s
        /// snapshot still restores it on Back, because `GameSettings.Apply` pushes the stored index
        /// back through `VSyncModes.Apply`.
        /// </summary>
        private void BuildVSyncRow()
        {
            var options = new List<SwatchDropdown.Option>();

            foreach (var entry in VSyncModes.All)
                options.Add(new SwatchDropdown.Option(entry.Label, null));

            _vsync = BuildDropdownRow("VSyncRow", "Vertical Sync", options,
                                      SettingsStore.Current.VSyncMode, PickVSync);
        }

        private void PickVSync(int index)
        {
            SettingsStore.Current.VSyncMode =
                Mathf.Clamp(index, 0, VSyncModes.All.Length - 1);

            VSyncModes.Apply(SettingsStore.Current.VSyncMode);
            RefreshApplyState();
        }

        /// <summary>
        /// The render style picker.
        ///
        /// ⚠️⚠️ IT IS ON THIS SCREEN SO THE TWO LOOKS CAN BE COMPARED WITHOUT A REBUILD. 🧑 wants
        /// to judge a softer post-processed look with visible colour fringing against the ink
        /// outlines the game ships with, and judging two looks means flipping between them on the
        /// same frame rather than describing them. See <see cref="RenderStyles"/> for what each
        /// row switches and why Toon is row 0 and the default.
        ///
        /// ⚠️ NO SWATCHES, for the same reason the anti-aliasing row has none: these are two
        /// named looks, not colours, and the only thing a player needs to see is which one is on
        /// and what else there is. The picture behind the panel is the swatch.
        /// </summary>
        private void BuildRenderStyleRow()
        {
            var options = new List<SwatchDropdown.Option>();

            foreach (var entry in RenderStyles.All)
                options.Add(new SwatchDropdown.Option(entry.Label, null));

            _renderStyle = BuildDropdownRow("RenderStyleRow", "Render Style", options,
                                            SettingsStore.Current.RenderStyle, PickRenderStyle);
        }

        /// <summary>
        /// The scaffolding both picker rows sit in.
        ///
        /// ⚠️ SHARED RATHER THAN COPIED, AND THE REASON IS THE COLUMN. `ActionLabelWidth` is what
        /// lines a row's label up with the fifteen rebind rows above it, and
        /// <see cref="HighlightControlSize"/> is what keeps the control the same width as the
        /// one below. Two copies of this method are two places for one of those to be edited,
        /// and a settings list where one row's control starts forty pixels further right than
        /// its neighbour's has already been reported once on this panel.
        ///
        /// ⚠️ BUILT IN CODE AND PLACED NEXT TO THE FULLSCREEN BOX, rather than authored into
        /// `SettingsPanel.unity`. The scene is a conversion of the .tscn and gets rebaked by
        /// `TscnImporter`, so a row added to the asset by hand is a row that disappears the next
        /// time somebody reimports the maps. Everything on this screen that the importer does
        /// not own is built here, which is the same reason the rebind rows are.
        /// </summary>
        private Dropdown BuildDropdownRow(string rowName, string label,
                                          IList<SwatchDropdown.Option> options,
                                          int initial, Action<int> onChanged)
        {
            var anchor = Node("FullscreenCheck");
            if (anchor == null || anchor.parent == null) return null;

            var rowGo = new GameObject(rowName);
            rowGo.AddComponent<RectTransform>();
            rowGo.transform.SetParent(anchor.parent, false);

            // Directly under the fullscreen box, so it reads as part of DISPLAY rather than as
            // the first row of whatever section follows.
            rowGo.transform.SetSiblingIndex(anchor.GetSiblingIndex() + 1);

            var row = rowGo.AddComponent<HorizontalLayoutGroup>();
            row.childControlHeight = true;
            row.childControlWidth = true;
            row.childForceExpandHeight = false;
            row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.spacing = 0.0f;

            var text = MenuKit.Styled(rowGo.transform, "MenuBody", label, TextAnchor.MiddleLeft);
            text.raycastTarget = false;

            var labelElement = text.gameObject.AddComponent<LayoutElement>();
            labelElement.preferredWidth = ActionLabelWidth;
            labelElement.minHeight = HighlightControlSize.y;

            var dropdown = SwatchDropdown.Build(rowGo.transform, options, initial,
                                                HighlightControlSize, onChanged);

            var element = dropdown.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = HighlightControlSize.x;
            element.preferredHeight = HighlightControlSize.y;
            element.flexibleWidth = 1.0f;

            return dropdown;
        }

        /// <summary>⚠️ WIDER THAN A KEYCAP. This row carries a swatch, a label and a chevron
        /// rather than one short key name, and at the rebind rows' 170 the colour names
        /// collided with the arrow.</summary>
        private static readonly Vector2 HighlightControlSize = new Vector2(300.0f, 46.0f);

        /// <summary>
        /// ⚠️ IT ANNOUNCES THE CHANGE, and that is the half that makes the control feel real.
        /// This panel is reachable from the in-match pause menu, so every tsinelas already
        /// lying on the arena repaints on this pick rather than at its next landing.
        /// </summary>
        private void PickSlipperHighlight(int index)
        {
            SettingsStore.Current.SlipperHighlight =
                Mathf.Clamp(index, 0, SlipperHighlights.All.Length - 1);

            SettingsStore.RaiseSlipperHighlightChanged();
            RefreshApplyState();
        }

        /// <summary>
        /// ⚠️⚠️ IT APPLIES ON THE PICK RATHER THAN ON APPLY, AND THAT IS DELIBERATE ON A PANEL
        /// WHERE MOST THINGS DO NOT. This is the same rule the volume sliders and the highlight
        /// colour already follow: a setting whose whole content is how the screen looks has to
        /// change the screen while the player is looking at it, or the control is a claim they
        /// cannot check. This panel is reachable from the in-match pause menu, so on the pick
        /// there is a real frame behind it to judge.
        ///
        /// ⚠️ AND BACK STILL UNDOES IT, which is the half that makes applying early safe.
        /// `SettingsStore.Restore` calls `GameSettings.Apply`, and `Apply` pushes the mode back
        /// through `AntiAliasModes.Apply`, so a discarded pick is off the screen as well as out
        /// of the file. Nothing here writes the disk.
        /// </summary>
        private void PickAntiAlias(int index)
        {
            SettingsStore.Current.AntiAliasMode =
                Mathf.Clamp(index, 0, AntiAliasModes.All.Length - 1);

            AntiAliasModes.Apply(SettingsStore.Current.AntiAliasMode);
            RefreshApplyState();
        }

        /// <summary>
        /// ⚠️ IT APPLIES ON THE PICK, for the reason <see cref="PickAntiAlias"/> gives at length
        /// and with more force than anything else on this panel: the whole content of this
        /// setting is what the screen looks like, and it exists so two looks can be COMPARED.
        /// A style that only took effect on APPLY, or on the next match, would make the one
        /// question it was added to answer impossible to ask.
        ///
        /// ⚠️ AND BACK STILL UNDOES IT. `SettingsStore.Restore` calls `GameSettings.Apply`, which
        /// pushes the snapshot's index back through `RenderStyles.Apply`, and that re-writes the
        /// `_OutlineSuppress` global as well as the two statics. Nothing here writes the disk.
        /// </summary>
        private void PickRenderStyle(int index)
        {
            SettingsStore.Current.RenderStyle =
                Mathf.Clamp(index, 0, RenderStyles.All.Length - 1);

            RenderStyles.Apply(SettingsStore.Current.RenderStyle);
            RefreshApplyState();
        }

        private void BeginRebind(string action)
        {
            if (_rebindOp != null) return;   // one at a time

            _listening = action;
            SetText("SettingsStatusLabel",
                    $"Press any key for \"{Rebinding.LabelFor(action)}\"…  (Esc to cancel)");

            SetButtonText(action, "…");

            if (!Rebinding.ResolveActionAndBindingIndex(_actions, action, out var target, out int targetIndex))
            {
                CancelRebind();
                return;
            }

            // The action must be disabled while it is being rebound, or the press being captured
            // also fires the verb it is bound to.
            target.Disable();

            _rebindOp = target.PerformInteractiveRebinding()
                .WithTargetBinding(targetIndex)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(op =>
                {
                    target.Enable();
                    MenuSfx.Back();
                    CancelRebind();
                })
                .OnComplete(op =>
                {
                    var control = op.selectedControl;
                    op.Dispose();
                    _rebindOp = null;
                    target.Enable();

                    // The override the operation already applied is undone first, because the
                    // conflict check has to run against the other actions and report a refusal
                    // rather than leave two verbs sharing one key.
                    target.RemoveBindingOverride(targetIndex);

                    // ⚠️ THE LINE ABOVE IS A BINDING CHANGE TOO, even though it is undoing one,
                    // and `Rebinding.Revision` is what lets a screen cache a key label. The net
                    // effect is zero only when `TryRebind` goes on to accept; on a refusal this
                    // is the write that restores the original key.
                    Rebinding.Invalidate();

                    string conflict = Rebinding.TryRebind(_actions, action, control);

                    if (conflict == null)
                    {
                        SetText("SettingsStatusLabel", $"\"{Rebinding.LabelFor(action)}\" rebound.");
                        MenuSfx.Click();
                    }
                    else
                    {
                        SetText("SettingsStatusLabel",
                                $"That key is already \"{conflict}\". Choose a different key.");
                        MenuSfx.Error();
                    }

                    RefreshBindingLabels();
                    _listening = "";
                    RefreshApplyState();
                })
                .Start();
        }

        private void CancelRebind()
        {
            _rebindOp?.Dispose();
            _rebindOp = null;
            _listening = "";
            SetText("SettingsStatusLabel", "Rebind cancelled.");
            RefreshBindingLabels();
        }

        private void SetButtonText(string action, string value)
        {
            if (!_rebindButtons.TryGetValue(action, out var button) || button == null) return;

            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = value;
        }

        private void RefreshBindingLabels()
        {
            foreach (var pair in _rebindButtons)
                SetButtonText(pair.Key, Rebinding.DisplayNameFor(_actions, pair.Key));
        }

        // --- Sliders and checks ------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE VALUE IS SEEDED BEFORE THE CALLBACK IS CONNECTED, NOT AFTER. Assigning to a
        /// slider fires its changed event synchronously in both engines, so connecting first
        /// means opening this panel writes the settings file three times before the player has
        /// touched anything.
        /// </summary>
        private void WireSliders()
        {
            Slider("SensitivitySlider", "SensitivityValueLabel",
                   SettingsStore.Current.MouseSensitivity,
                   v => SettingsStore.Current.MouseSensitivity = v,
                   v => $"{v:0.0}x", preview: false);

            Slider("MasterVolumeSlider", "MasterVolumeValueLabel",
                   SettingsStore.Current.MasterVolume,
                   v => SettingsStore.Current.MasterVolume = v,
                   Percent, preview: true);

            Slider("SfxVolumeSlider", "SfxVolumeValueLabel",
                   SettingsStore.Current.SfxVolume,
                   v => SettingsStore.Current.SfxVolume = v,
                   Percent, preview: true);

            // ⚠️ NO PREVIEW CLICK ON THE MUSIC ROW. The preview is the ordinary UI click, which
            // is on the SFX bus: it demonstrates Master and SFX honestly and would say nothing
            // truthful about the music level.
            Slider("MusicVolumeSlider", "MusicVolumeValueLabel",
                   SettingsStore.Current.MusicVolume,
                   v => SettingsStore.Current.MusicVolume = v,
                   Percent, preview: false);
        }

        private static string Percent(float v) => $"{Mathf.RoundToInt(v * 100.0f)}%";

        private void Slider(string sliderNode, string labelNode, float seed, Action<float> setter,
                            Func<float, string> format, bool preview)
        {
            var t = Node(sliderNode);
            if (t == null) return;

            var slider = t.GetComponent<Slider>();
            if (slider == null) return;

            // ⚠⚠ WITHOUT THIS THE ROW IS DECORATION. The converted slider reaches us with every
            // graphic under it muted, so it takes no pointer event at all. See
            // MenuKit.EnsureHitArea for the whole story; it is repaired here rather than only in
            // the importer because the converted prefabs are committed assets and a player
            // running the shipped build never re-runs the converter.
            MenuKit.EnsureHitArea(slider);

            slider.SetValueWithoutNotify(seed);
            SetText(labelNode, format(seed));

            // ⚠️⚠️ THE CLICK PLAYS ON RELEASE, NOT ON EVERY VALUE CHANGE. `onValueChanged`
            // fires once per frame of a drag, so playing MenuSfx.Click() straight from the
            // listener turned one drag into dozens of overlapping clicks a second. The
            // gate below tracks whether the pointer is currently down on the slider; while it
            // is, a change only marks the sfx pending, and SliderCommitGate.Released fires it
            // once when the drag ends. A keyboard nudge (no pointer ever down) still plays
            // immediately, because it is already one discrete completed movement.
            var gate = slider.GetComponent<SliderCommitGate>();
            if (gate == null) gate = slider.gameObject.AddComponent<SliderCommitGate>();

            bool pendingSfx = false;
            gate.Released += () =>
            {
                if (!pendingSfx) return;
                pendingSfx = false;
                if (preview) MenuSfx.Click();
            };

            slider.onValueChanged.AddListener(v =>
            {
                setter(v);
                SetText(labelNode, format(v));

                // ⚠⚠ NO `Apply()` HERE, AND IT USED TO BE CALLED ON EVERY VALUE CHANGE.
                // `GameSettings.Apply` is ApplyDisplay plus the AI difficulty, and ApplyDisplay
                // is a `Screen.SetResolution`: dragging one volume slider across its groove fired
                // a window resize on every frame of the drag, which stalls the drag it is
                // reacting to. No slider on this panel feeds either system. The volumes are read
                // live off the store by the music bed, the announcer and the SFX bus, and the
                // sensitivity is read live by the camera, which is what makes a drag audible and
                // visible immediately without pushing anything anywhere.
                if (preview)
                {
                    if (gate.IsPressed) pendingSfx = true;
                    else MenuSfx.Click();
                }

                RefreshApplyState();
            });
        }

        /// <summary>
        /// Tracks whether the pointer is currently held down on a slider, so its owner can defer
        /// a "movement finished" action until <see cref="Released"/> fires instead of running it
        /// on every intermediate value change of a drag.
        /// </summary>
        private sealed class SliderCommitGate : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IEndDragHandler
        {
            public bool IsPressed { get; private set; }
            public event Action Released;

            public void OnPointerDown(PointerEventData e) => IsPressed = true;

            public void OnPointerUp(PointerEventData e)
            {
                IsPressed = false;
                Released?.Invoke();
            }

            public void OnEndDrag(PointerEventData e)
            {
                IsPressed = false;
                Released?.Invoke();
            }
        }

        private void WireChecks()
        {
            Check("InvertYCheck", SettingsStore.Current.InvertY,
                  v => SettingsStore.Current.InvertY = v);

            Check("FullscreenCheck", SettingsStore.Current.Fullscreen, v =>
            {
                SettingsStore.Current.Fullscreen = v;

                // ⚠️ APPLIED, NOT ONLY STORED. Fullscreen was saved and displayed and never
                // actually set for the whole port, so the box claimed the opposite of the window.
                SettingsStore.Current.ApplyDisplay();
            });
        }

        private void Check(string node, bool seed, Action<bool> setter)
        {
            var t = Node(node);
            if (t == null) return;

            var toggle = t.GetComponent<Toggle>();
            if (toggle == null) return;

            // ⚠️⚠️ THE HIT AREA, FOR THE SAME REASON THE SLIDERS GET ONE HERE AND NOT IN THE
            // IMPORTER. Both boxes on this panel converted, skinned, seeded and wired their
            // listener, and neither could be pressed: a Toggle's tick box is on a child node, so
            // the shipped scene has no raycast target under the row at all. See
            // `MenuKit.EnsureHitArea(Toggle)`. Runtime, because the player never re-runs the bake.
            MenuKit.EnsureHitArea(toggle);

            // ⚠️ SEEDED WITHOUT NOTIFYING, like the sliders: assigning `isOn` emits the change,
            // and a fullscreen toggle that fires on every open flips the window mode for free.
            toggle.SetIsOnWithoutNotify(seed);

            toggle.onValueChanged.AddListener(v =>
            {
                setter(v);
                MenuSfx.Click();
                RefreshApplyState();
            });
        }

        private void WireNameField()
        {
            var t = Node("PlayerNameField");
            if (t == null) return;

            var field = t.GetComponent<InputField>();
            if (field == null) return;

            field.characterLimit = Balance.PlayerNameMax;
            field.text = SettingsStore.Current.PlayerName;

            // ⚠️⚠️ ON EVERY KEYSTROKE, AND WITHOUT IT APPLY COULD NOT BE REACHED AT ALL. The
            // obvious way to leave the field is to click APPLY, but APPLY is disabled until
            // something is dirty, a disabled button takes no focus and emits nothing, so the
            // click did nothing and the name was never staged. Typing is the change, so typing
            // is what reports it. Safe per keystroke: nothing here touches the disk.
            field.onValueChanged.AddListener(v =>
            {
                SettingsStore.Current.PlayerName = GameSettings.SanitiseName(v);
                RefreshApplyState();
            });
        }

        // --- The apply / discard pair -------------------------------------------------

        private void Apply()
        {
            SettingsStore.Current.Validate();
            SettingsStore.Save();
            SettingsStore.Current.Apply();

            if (_actions != null) Rebinding.Save(_actions);

            AIController.ApplyDifficulty(SettingsStore.Current.AiDifficulty);

            // Straight into a new transaction: the panel is still open, so the next change the
            // player makes has to be revertible too.
            Snapshot();
            _backArmed = false;

            SetText("SettingsStatusLabel", "Settings saved.");
            RefreshApplyState();
        }

        private void ResetAll()
        {
            if (_actions != null) Rebinding.ResetAll(_actions);
            RefreshBindingLabels();

            // ⚠️ RESET IS A STAGED EDIT LIKE ANY OTHER and the wording has to say so, or the
            // button promises something that has not happened until APPLY is pressed.
            SetText("SettingsStatusLabel", "All controls reset — press APPLY CHANGES to keep it.");
            RefreshApplyState();
        }

        private void Back()
        {
            if (Dirty() && !_backArmed)
            {
                _backArmed = true;
                MenuSfx.Error();

                SetButtonLabel(_back, "◀  DISCARD & GO BACK");
                SetText("SettingsStatusLabel",
                        "You have unsaved changes. Press BACK again to discard them.");

                RefreshApplyState();
                return;
            }

            Revert();
            Close();
        }

        /// <summary>
        /// ⚠️ REVERT, NOT JUST CLOSE. Nothing has been written since the snapshot, so the FILE
        /// is already right, but the running process is not: a rebind is live in the input map
        /// and a volume is live on the bus. Leaving without this hands a player who pressed BACK
        /// the settings they just rejected, until a restart quietly puts the saved ones back.
        /// </summary>
        private void Revert()
        {
            if (!string.IsNullOrEmpty(_snapshot))
                SettingsStore.Restore(JsonUtility.FromJson<GameSettings>(_snapshot));

            if (_actions != null) Rebinding.Load(_actions);

            // ⚠️ THE PICKER'S FACE IS PART OF THE STATE BEING REVERTED. `Restore` puts the value
            // back and repaints the arena, but the control still shows whatever the player
            // chose, and a swatch reading Red beside slippers lit Blue is the setting looking
            // broken.
            //
            // ⚠️ SetValueWithoutNotify, NOT `value`. Assigning `value` fires onValueChanged,
            // which would write the reverted index straight back into the settings as a fresh
            // edit and re-dirty the panel we are in the middle of cleaning up.
            if (_highlight != null)
                _highlight.SetValueWithoutNotify(SettingsStore.Current.SlipperHighlight);

            // ⚠️ THE SAME FOR THE ANTI-ALIASING FACE, and it has the stronger claim of the two:
            // `Restore` re-applies the mode to the engine, so the frame behind this panel is
            // already back to what it was. A picker still reading "Off" over a filtered frame is
            // the reverted state showing through the one place that has not been told.
            if (_vsync != null)
                _vsync.SetValueWithoutNotify(SettingsStore.Current.VSyncMode);

            if (_antiAlias != null)
                _antiAlias.SetValueWithoutNotify(SettingsStore.Current.AntiAliasMode);

            // ⚠️ AND THE SAME FOR THE STYLE, which has the strongest claim of the three: a picker
            // reading "Chromatic" over a frame that has just been given its ink outlines back is
            // the discard looking like it failed.
            if (_renderStyle != null)
                _renderStyle.SetValueWithoutNotify(SettingsStore.Current.RenderStyle);

            _backArmed = false;
            SetButtonLabel(_back, "◀  BACK");
            SetText("SettingsStatusLabel", "");
        }

        private static void SetButtonLabel(Button button, string value)
        {
            if (button == null) return;

            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = value;
        }

        /// <summary>APPLY is live only when there is something to apply, and BACK goes back to
        /// saying BACK the moment the player undoes whatever armed it.</summary>
        private void RefreshApplyState()
        {
            bool dirty = Dirty();

            if (_apply != null) _apply.interactable = dirty;

            if (dirty || !_backArmed) return;

            _backArmed = false;
            SetButtonLabel(_back, "◀  BACK");
        }

        protected override void Update()
        {
            // ⚠️ BEFORE THE ESC GUARD BELOW, because that guard RETURNS on a listening rebind and
            // the keyboard scroll has its own refusal for exactly that case. Putting it after
            // would have made this work only when nothing else was going on.
            StepScrollFromKeyboard();

            // ⚠️ ESC IS THE REBIND'S CANCEL WHILE ONE IS LISTENING, not the panel's exit. The
            // rebinding operation already owns that key; letting the base class also act on it
            // closes the whole panel on a cancelled rebind.
            if (_rebindOp != null || _listening != "") return;

            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            MenuSfx.Back();
            Back();
        }

        private void OnDestroy() => _rebindOp?.Dispose();
    }
}
