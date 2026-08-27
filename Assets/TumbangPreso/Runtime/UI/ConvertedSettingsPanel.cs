using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using UnityEngine;
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
            BuildSlipperHighlightRow();
            WireSliders();
            WireChecks();
            WireNameField();
            ConfigureScroll();

            OnClick("ResetAllButton", ResetAll);
            OnClick("ApplyButton", Apply);
            OnClick("BackButton", Back);

            SetText("SettingsStatusLabel", "");
            RefreshApplyState();
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
            var anchor = Node("FullscreenCheck");
            if (anchor == null || anchor.parent == null) return;

            var rowGo = new GameObject("SlipperHighlightRow");
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

            var label = MenuKit.Styled(rowGo.transform, "MenuBody",
                                       "Slipper Highlight", TextAnchor.MiddleLeft);
            label.raycastTarget = false;

            var labelElement = label.gameObject.AddComponent<LayoutElement>();
            labelElement.preferredWidth = ActionLabelWidth;
            labelElement.minHeight = HighlightControlSize.y;

            var options = new System.Collections.Generic.List<SwatchDropdown.Option>();

            foreach (var entry in SlipperHighlights.All)
            {
                // ⚠️ "Off" PASSES A NULL SWATCH rather than its stored colour. Row 0 holds black
                // as a placeholder because the palette is an array and every row needs a value;
                // drawing it would put a black chip beside "Off" that reads like a sixth colour.
                bool hasColour = entry.Label != "Off";
                options.Add(new SwatchDropdown.Option(
                    entry.Label, hasColour ? entry.Colour : (Color?)null));
            }

            _highlight = SwatchDropdown.Build(rowGo.transform, options,
                                              SettingsStore.Current.SlipperHighlight,
                                              HighlightControlSize, PickSlipperHighlight);

            var element = _highlight.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = HighlightControlSize.x;
            element.preferredHeight = HighlightControlSize.y;
            element.flexibleWidth = 1.0f;
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

            slider.SetValueWithoutNotify(seed);
            SetText(labelNode, format(seed));

            slider.onValueChanged.AddListener(v =>
            {
                setter(v);
                SetText(labelNode, format(v));

                SettingsStore.Current.Apply();
                if (preview) MenuSfx.Click();

                RefreshApplyState();
            });
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
