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
    /// The overlays that are instanced into a screen rather than loaded as one.
    ///
    /// ⚠️⚠️ THEY ARE CONVERTED SCENES NOW, NOT CODE-BUILT REPLACEMENTS. Settings, Tutorial and
    /// Credits were hand-drawn in C# with absolute anchors while the real screens sat unused in
    /// `MapSource/scenes_ui`, which is how the settings overlay ended up as five labels stacked
    /// on the same pixel with no sliders, no keybind rows and no scroll. `main_menu.gd`
    /// instances all three into the title screen and shows them in place, and so does this.
    ///
    /// ⚠️ SHOWN IN PLACE, NOT SWITCHED TO. A scene change would tear down and rebuild the title
    /// screen behind a panel the player is about to close in a few seconds.
    /// </summary>
    public abstract class ConvertedOverlay : ConvertedScreen
    {
        /// <summary>Raised when the panel's own BACK is pressed, so the screen can re-unfurl.</summary>
        public event Action BackPressed;

        protected void RaiseBack() => BackPressed?.Invoke();

        public virtual void Close()
        {
            gameObject.SetActive(false);
            RaiseBack();
        }

        /// <summary>
        /// ⚠️ EVERY PANEL IN THE GAME HAS A WORKING ESC. It is in the Godot build's own
        /// checklist as U-7, and a panel you can only leave with the mouse is the one that
        /// strands a player who opened it with the keyboard.
        /// </summary>
        protected virtual void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            MenuSfx.Back();
            Close();
        }

        /// <summary>
        /// A chip-and-body row: a sunken wood slot holding the term, and the explanation beside
        /// it. Shared by the tutorial and the credits so a player who has seen one reads the
        /// other the same way, which is why `credits_panel.gd` copies Tutorial's CHIP_WIDTH.
        /// </summary>
        protected static GameObject Row(Transform parent, string chip, string body)
        {
            var rowGo = new GameObject("Row");
            rowGo.AddComponent<RectTransform>();
            rowGo.transform.SetParent(parent, false);

            var row = rowGo.AddComponent<HorizontalLayoutGroup>();
            row.childControlHeight = true;
            row.childControlWidth = true;
            row.childForceExpandHeight = false;
            row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.UpperLeft;
            row.spacing = 16.0f;

            var slotGo = new GameObject("Slot");
            slotGo.AddComponent<RectTransform>();
            slotGo.transform.SetParent(rowGo.transform, false);
            slotGo.AddComponent<Image>();

            var slotGroup = slotGo.AddComponent<VerticalLayoutGroup>();
            slotGroup.childControlHeight = true;
            slotGroup.childControlWidth = true;
            slotGroup.childForceExpandHeight = false;
            slotGroup.childForceExpandWidth = true;

            var skin = slotGo.AddComponent<GodotPanel>();
            skin.Variation = "WoodSlot";

            var slotElement = slotGo.AddComponent<LayoutElement>();
            slotElement.preferredWidth = TutorialContent.ChipWidth;
            slotElement.flexibleWidth = 0.0f;

            var chipText = MenuKit.Styled(slotGo.transform, "MenuValue", chip, TextAnchor.MiddleCenter);
            chipText.raycastTarget = false;

            var bodyText = MenuKit.Styled(rowGo.transform, "MenuBody", body, TextAnchor.UpperLeft);
            bodyText.raycastTarget = false;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var bodyElement = bodyText.gameObject.AddComponent<LayoutElement>();
            bodyElement.flexibleWidth = 1.0f;

            return rowGo;
        }

        protected static Text Heading(Transform parent, string words)
        {
            var text = MenuKit.Styled(parent, "MenuHeading", words, TextAnchor.MiddleLeft);
            text.raycastTarget = false;
            return text;
        }

        protected static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
    }

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

    /// <summary>
    /// Ported from `tutorial.gd`, driving the converted `Tutorial.tscn`.
    ///
    /// ⚠️ IT TEACHES THE THESIS, NOT JUST THE KEYS. The tension is the retrieval, not the
    /// throw: throwing is safe and free, and getting your slipper back is what costs you. A
    /// player who learns only the controls plays the game backwards for their first match.
    /// </summary>
    public sealed class ConvertedTutorialPanel : ConvertedOverlay
    {
        private int _page;

        protected override void Wire()
        {
            SetText("BannerLabel", "HOW TO PLAY");

            OnClick("PrevButton", () => Turn(-1));
            OnClick("NextButton", () => Turn(1));
            OnClick("BackButton", Close);

            ApplyPage();
        }

        public void ResetToFirstPage()
        {
            _page = 0;
            ApplyPage();
        }

        /// <summary>⚠️ IT CLAMPS RATHER THAN WRAPPING. Wrapping from the last page back to the
        /// first reads as the panel having closed and reopened.</summary>
        private void Turn(int delta)
        {
            _page = Mathf.Clamp(_page + delta, 0, TutorialContent.Pages.Length - 1);
            ApplyPage();
        }

        private void ApplyPage()
        {
            var page = TutorialContent.Pages[_page];

            SetText("PageTitle", page.Title);
            SetText("PageLede", page.Lede);
            SetText("PageLabel", $"{_page + 1} / {TutorialContent.Pages.Length}");

            var rows = Node("Rows");
            if (rows == null) return;

            Clear(rows);

            foreach (var row in page.Rows) Row(rows, row.Chip, row.Body);
        }
    }

    /// <summary>
    /// Ported from `credits_panel.gd`, driving the converted `CreditsPanel.tscn`.
    ///
    /// ⚠️⚠️ THIS SCREEN IS LICENCE COMPLIANCE, NOT POLISH. Three CC-BY-4.0 models ship and
    /// their one requirement is that the author be reachable from somewhere the game actually
    /// ships. An earlier Unity pass rebuilt this as a studio blurb with NO asset credits at all.
    /// The strings are each model's own LICENSE.txt, verbatim. **Do not reword or trim them.**
    /// </summary>
    public sealed class ConvertedCreditsPanel : ConvertedOverlay
    {
        protected override void Wire()
        {
            SetText("Title", "CREDITS");
            OnClick("BackButton", Close);

            var rows = Node("Rows");
            if (rows == null) return;

            Clear(rows);

            Heading(rows, "TUMBANG PRESO  ·  BH STUDIOS");

            var made = MenuKit.Styled(rows, "MenuBody",
                "1st place, Gear Up NCR Esports Game Dev Challenge  ·  " +
                "NCR's entry at the nationals in General Santos City", TextAnchor.UpperLeft);
            made.horizontalOverflow = HorizontalWrapMode.Wrap;
            made.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.0f;

            foreach (var person in CreditsContent.TeamCredits) Row(rows, person.Name, person.Role);

            Heading(rows, "THIRD-PARTY MODELS  ·  CC-BY-4.0");
            foreach (var credit in CreditsContent.CcByCredits) Row(rows, credit.Chip, credit.Body);

            Heading(rows, "EVERYTHING ELSE");
            foreach (var credit in CreditsContent.CourtesyCredits) Row(rows, credit.Chip, credit.Body);
        }
    }
}
