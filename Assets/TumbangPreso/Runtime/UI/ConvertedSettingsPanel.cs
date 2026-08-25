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

            OnClick("ResetAllButton", ResetAll);
            OnClick("ApplyButton", Apply);
            OnClick("BackButton", Back);

            SetText("SettingsStatusLabel", "");
            RefreshApplyState();
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

            foreach (var action in Rebinding.RebindableActions)
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

            var target = _actions?.FindActionMap("Player", false)?.FindAction(action, false);
            if (target == null) { CancelRebind(); return; }

            // The action must be disabled while it is being rebound, or the press being captured
            // also fires the verb it is bound to.
            target.Disable();

            _rebindOp = target.PerformInteractiveRebinding()
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

                    // ⚠️ THE OVERRIDE THE OPERATION ALREADY APPLIED IS UNDONE FIRST, because the
                    // conflict check has to run against the OTHER actions and report a refusal
                    // rather than leave two verbs sharing one key.
                    target.RemoveBindingOverride(op.bindingMask ?? default);

                    string conflict = Rebinding.TryRebind(_actions, action, control);

                    if (conflict == null)
                    {
                        SetText("SettingsStatusLabel", $"\"{Rebinding.LabelFor(action)}\" rebound.");
                        MenuSfx.Click();
                    }
                    else
                    {
                        // ⚠️ THE CONFLICT BUZZ. A player looking at the keyboard rather than at
                        // the status label gets no signal at all that the press was refused.
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
                Screen.fullScreen = v;
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
