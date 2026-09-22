using System;
using System.Collections.Generic;
using System.Linq;
using TumbangPreso.InputLayer;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpSettingsView : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _list;
        private Text _heading, _status;
        private Button _save;
        private GameObject _decision;
        private readonly List<Button> _tabs = new List<Button>();
        private readonly Dictionary<string, Button> _bindingRows = new Dictionary<string, Button>();
        private TumpSettingsSession _session;
        public TumpSettingsSession Session => _session;
        private int _tab, _group;
        private InputDeviceKind _device = InputDeviceKind.KeyboardMouse;
        private Action _back, _controller, _touch;
        private bool _suspended;
        private bool _genericSupportShown;
        private TumpChoice _frameCap;
        private Text _frameReason;
        public static readonly string[] Sections = { "Controls", "Audio", "Graphics", "Player", "Accessibility" };

        public void Open(Transform owner, Action back, Action controller, Action touch)
        {
            _back = back; _controller = controller; _touch = touch;
            ReleaseSession();
            _session = new TumpSettingsSession(); _session.Changed += Changed;
            if (_canvas == null) Build(owner);
            ScreenTakeover.Register(this, () => !_suspended && _canvas != null && _canvas.gameObject.activeInHierarchy);
            _suspended = false; _canvas.gameObject.SetActive(true); ShowSection(_tab);
        }
        public void Suspend() { _suspended = true; _canvas.gameObject.SetActive(false); }
        public void Resume() { _suspended = false; _canvas.gameObject.SetActive(true); ShowSection(_tab); }
        private void OnDisable()
        {
            if (_canvas != null) _canvas.gameObject.SetActive(false);
            if (_session != null && _session.Dirty) _session.Discard();
            ReleaseSession();
        }
        private void Update()
        {
            if (_suspended || _canvas == null || !_canvas.gameObject.activeSelf) return;
            if (_tab == 0 && _device != InputDeviceKind.Touch && !_session.Listening
                && SettingsOptionMenu.OpenOption == null && _genericSupportShown != ControllerWatch.HasUnrecognised)
                ShowSection(0);
            if (!MenuNav.CancelPressed || ScreenTakeover.EscapeIsSpokenExcept(this)) return;
            ScreenTakeover.ConsumeEscape();
            if (_session.Listening) { _session.CancelRebind(); return; }
            if (SettingsOptionMenu.OpenOption != null) { SettingsOptionMenu.OpenOption.Close(); return; }
            if (_decision != null && _decision.activeSelf) { _decision.SetActive(false); return; }
            Back();
        }
        private void ReleaseSession()
        {
            if (_session == null) return;
            _session.Changed -= Changed;
            _session.Dispose();
            _session = null;
        }
        private void OnDestroy() { ReleaseSession(); ScreenTakeover.Unregister(this); }
        private void BuildPrevious(Transform owner)
        {
            var f = TumpUiTheme.Current;
            _canvas = TumpUiFactory.Canvas(owner, "TumpSettingsCanvas", 800);
            var root = (RectTransform)_canvas.transform;
            TumpUiFactory.Ground(root, f.Cream);
            var rail = TumpUiFactory.Surface(root, "CategoryRail", TumpSurface.Form.WavePanel, f.DeepOlive, false);
            rail.rectTransform.anchorMin = new Vector2(0, 0); rail.rectTransform.anchorMax = new Vector2(0, 1);
            rail.rectTransform.offsetMin = new Vector2(32, 32); rail.rectTransform.offsetMax = new Vector2(396, -32);
            var back = TumpUiFactory.BackButton(root, "TumpSettingsBack", Back);
            TumpUiFactory.Place((RectTransform)back.transform, 464, 28, 160, 74);
            _heading = TumpUiFactory.Text(root, "Heading", "Settings", 62, false, true);
            _heading.color = f.Brick; TumpUiFactory.Place(_heading.rectTransform, 476, 110, 1100, 100);
            var logo = TumpUiFactory.Art(rail.transform, "OriginalLogo", TumpUiFactory.Sprite("UI/brand/tump_logo"));
            TumpUiFactory.Place(logo.rectTransform, 44, 38, 278, 182);
            TumpSymbol.Icon[] icons = { TumpSymbol.Icon.Controller, TumpSymbol.Icon.Audio, TumpSymbol.Icon.Eye, TumpSymbol.Icon.Person, TumpSymbol.Icon.Settings };
            for (int i = 0; i < Sections.Length; i++)
            {
                int index = i;
                var tab = TumpUiFactory.Button(rail.transform, "SettingsSection" + i, Sections[i], () => ShowSection(index), TumpSurface.Form.Link, f.Cream, 32);
                TumpUiFactory.Place((RectTransform)tab.transform, 20, 272 + i * 122, 324, 96);
                tab.GetComponent<TumpSurface>().LightInk = true;
                var text = tab.GetComponentInChildren<Text>(); text.color = f.Cream;
                text.alignment = TextAnchor.MiddleLeft; text.rectTransform.offsetMin = new Vector2(82, 4);
                var icon = TumpUiFactory.Rect(tab.transform, "Icon").gameObject.AddComponent<TumpSymbol>();
                icon.Kind = icons[i]; icon.color = f.Lime; icon.raycastTarget = false;
                TumpUiFactory.Place(icon.rectTransform, 14, 20, 56, 56);
                _tabs.Add(tab);
            }
            _list = TumpUiFactory.Scroll(root, "SettingsList", out var scroll);
            var view = (RectTransform)scroll.transform;
            view.anchorMin = Vector2.zero; view.anchorMax = Vector2.one;
            view.offsetMin = new Vector2(476, 208); view.offsetMax = new Vector2(-72, -236);
            _status = TumpUiFactory.Text(root, "SettingsStatus", "", 26);
            _status.rectTransform.anchorMin = Vector2.zero; _status.rectTransform.anchorMax = new Vector2(1, 0);
            _status.rectTransform.offsetMin = new Vector2(476, 144); _status.rectTransform.offsetMax = new Vector2(-72, 196);
            _save = TumpUiFactory.Button(root, "TumpSaveSettings", "Save changes", () => _session.Save(), TumpSurface.Form.Slap, f.Lime, 42);
            TumpUiFactory.Anchor((RectTransform)_save.transform, new Vector2(1, 0), new Vector2(-340, 80), new Vector2(536, 100));
        }
        public void ShowSectionPrevious(int index)
        {
            _tab = Mathf.Clamp(index, 0, Sections.Length - 1);
            if (_canvas == null) return;
            TumpChoice.OpenChoice?.Close();
            foreach (Transform child in _list) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            _bindingRows.Clear();
            foreach (var button in _tabs) { var face = button.GetComponent<TumpSurface>(); face.Selected = button == _tabs[_tab]; face.SetVerticesDirty(); }
            _heading.text = Sections[_tab];
            switch (_tab)
            {
                case 0: Controls(); break;
                case 1: Audio(); break;
                case 2: Graphics(); break;
                case 3: Player(); break;
                case 4: Accessibility(); break;
            }
            var scroll = _list.GetComponentInParent<ScrollRect>(); scroll.verticalNormalizedPosition = 1;
            Changed(""); _canvas.GetComponent<ScreenFocus>().Rebuild();
        }
        private void NotePrevious(string words)
        {
            var text = TumpUiFactory.Text(_list, "Note", words, 26);
            text.alignment = TextAnchor.UpperLeft;
            TumpUiFactory.Height(text, 78);
        }
        private RectTransform RowPrevious(string name, string label) => TumpFormWidgets.Row(_list, name, label);
        private void TogglePrevious(string name, string label, bool value, Action<bool> set, Action apply = null)
            => TumpFormWidgets.Toggle(Row(name, label), name + "Value", value, v => { set(v); _session.Preview(apply); });
        private void ChoicePrevious(string name, string label, string[] values, int value, Action<int> set)
            => TumpFormWidgets.Choice(Row(name, label), name + "Value", values, value, v => { set(v); _session.Preview(); });
        private void AudioPrevious()
        {
            Note("Listen as you adjust. Save your changes when they feel right.");
            var s = SettingsStore.Current;
            AudioSlider("MasterVolume", "Master volume", s.MasterVolume, v => s.MasterVolume = v);
            AudioSlider("SoundVolume", "Sound effects", s.SfxVolume, v => s.SfxVolume = v);
            AudioSlider("MusicVolume", "Music", s.MusicVolume, v => s.MusicVolume = v);
        }
        private void AudioSliderPrevious(string name, string label, float value, Action<float> set)
            => TumpFormWidgets.Slider(Row(name, label), name + "Value", value, 0, 1,
                v => { set(v); _session.Preview(); }, v => Mathf.RoundToInt(v * 100) + "%");
        private void GraphicsPrevious()
        {
            Note("Changes preview immediately. You can save them or go back to your previous settings.");
            var s = SettingsStore.Current;
            Choice("GraphicsQuality", "Graphics quality", GraphicsProfiles.All.Select(p => p.Label).ToArray(), s.GraphicsQuality,
                v => { s.GraphicsQuality = v; GraphicsProfiles.Apply(v); });
            Choice("RenderStyle", "Visual style", RenderStyles.All.Select(p => p.Label).ToArray(), s.RenderStyle,
                v => { s.RenderStyle = v; RenderStyles.Apply(v); });
            Choice("AntiAliasing", "Smooth edges", AntiAliasModes.All.Select(p => p.Label).ToArray(), s.AntiAliasMode,
                v => { s.AntiAliasMode = v; AntiAliasModes.Apply(v); });
            Choice("VSync", "Vertical sync", VSyncModes.All.Select(p => p.Label).ToArray(), s.VSyncMode,
                v => { s.VSyncMode = v; VSyncModes.Apply(v); FrameRateOptions.Apply(s.FrameRateLimit); UpdateFrameCapState(); });
            _frameCap = TumpFormWidgets.Choice(Row("FrameRate", "Frame rate limit"), "FrameRateValue",
                FrameRateOptions.All.Select(FrameRateOptions.Label).ToArray(), Array.IndexOf(FrameRateOptions.All, s.FrameRateLimit),
                v => { s.FrameRateLimit = FrameRateOptions.All[v]; FrameRateOptions.Apply(s.FrameRateLimit); _session.Preview(); });
            _frameReason = TumpUiFactory.Text(_list, "FrameRateReason", "", 26);
            TumpUiFactory.Height(_frameReason, 64);
            UpdateFrameCapState();
            Toggle("Fullscreen", "Fullscreen", s.Fullscreen, v => s.Fullscreen = v, s.ApplyDisplay);
        }
        private void UpdateFrameCapStatePrevious()
        {
            if (_frameCap == null || _frameReason == null) return;
            bool sync = VSyncModes.Of(SettingsStore.Current.VSyncMode).Count > 0;
            int launch = FrameRateOptions.ReadOperatorLimit(Environment.GetCommandLineArgs());
            _frameCap.GetComponent<Button>().interactable = !sync && launch <= 0;
            _frameReason.text = launch > 0 ? "Launch settings control the frame rate: " + launch + " FPS."
                : sync ? "Turn vertical sync off to use your saved frame rate limit." : "Your limit applies while vertical sync is off.";
        }
        private void PlayerPrevious()
        {
            var s = SettingsStore.Current;
            var name = TumpUiFactory.Field(Row("PlayerName", "Player name"), "PlayerNameField", "Your player name", s.PlayerName);
            name.characterLimit = Core.Balance.PlayerNameMax;
            name.onValueChanged.AddListener(v => { s.PlayerName = v; _session.Preview(); });
            Toggle("Telemetry", "Share play statistics", s.TelemetryEnabled, v => s.TelemetryEnabled = v);
            Note("Counts only: matches, modes, maps, picks and frame rate. No names, chat or anything you type.");
        }
        private void AccessibilityPrevious()
        {
            var s = SettingsStore.Current;
            Toggle("ReducedUiMotion", "Reduce interface motion", s.ReducedUiMotion, v => s.ReducedUiMotion = v);
            Choice("SlipperHighlight", "Slipper highlight", SlipperHighlights.All.Select(p => p.Label).ToArray(), s.SlipperHighlight, v => s.SlipperHighlight = v);
            Toggle("Rumble", "Controller vibration", s.Rumble, v => s.Rumble = v, () => Rumble.Enabled = s.Rumble);
            Note("Interface motion can be reduced while gameplay movement stays visible.");
        }
        private void ControlsPrevious()
        {
            Choice("InputDevice", "Input device", new[] { "Keyboard & mouse", "Controller", "Touch" }, (int)_device, v =>
            { _device = (InputDeviceKind)v; ShowSection(0); });
            if (_device == InputDeviceKind.Touch)
            {
                ActionRow("TouchLayout", "Touch controls", "Arrange controls", () => { Suspend(); _touch?.Invoke(); });
                return;
            }
            if (_device == InputDeviceKind.Gamepad)
                ActionRow("ControllerMap", "Controller map", "See controller", () => { Suspend(); _controller?.Invoke(); });
            Choice("BindingGroup", "Control group", Rebinding.Groups.Select(g => g.Title).ToArray(), _group, v => { _group = v; ShowSection(0); });
            foreach (string action in Rebinding.Groups[_group].Actions)
            {
                string id = action;
                bool own = Rebinding.HasBindingFor(_session.Actions, action, _device);
                var button = ActionRow("Binding_" + action, Rebinding.LabelFor(action), BindingLabel(action), () => _session.BeginRebind(id, _device));
                button.interactable = own;
                _bindingRows[action] = button;
            }
            if (_device == InputDeviceKind.KeyboardMouse && _group == 0)
            {
                var s = SettingsStore.Current;
                TumpFormWidgets.Slider(Row("Sensitivity", "Mouse sensitivity"), "SensitivityValue", s.MouseSensitivity, .1f, 5,
                    v => { s.MouseSensitivity = v; _session.Preview(); }, v => v.ToString("0.0") + "×");
                Toggle("InvertY", "Invert vertical look", s.InvertY, v => s.InvertY = v);
            }
            ActionRow("ResetControls", "Reset bindings", "Reset controls", _session.ResetControls);
        }
        private string BindingLabel(string action)
        {
            string label = Rebinding.DisplayNameFor(_session.Actions, action, _device);
            if (!Rebinding.HasBindingFor(_session.Actions, action, _device))
            {
                string shared = Rebinding.SharedDisplayNameFor(_session.Actions, action, _device);
                if (!string.IsNullOrEmpty(shared)) label = shared;
            }
            return label;
        }
        private Button ActionRowPrevious(string name, string title, string label, Action action)
        {
            bool binding = name.StartsWith("Binding_", StringComparison.Ordinal);
            var button = TumpUiFactory.Button(Row(name, title), name + "Action", label, action,
                binding ? TumpSurface.Form.Pebble : TumpSurface.Form.Link,
                binding ? TumpUiTheme.Current.Cream : TumpUiTheme.Current.Apricot, binding && label.Length <= 3 ? 40 : 30);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(0, 0); rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, .5f);
            rect.offsetMin = Vector2.zero; rect.offsetMax = new Vector2(binding && label.Length <= 3 ? 150 : 380, 0);
            return button;
        }
        private void Changed(string message)
        {
            if (_status != null) _status.text = message;
            if (_save != null) _save.interactable = _session.Dirty;
            foreach (var pair in _bindingRows)
                if (pair.Value != null) pair.Value.GetComponentInChildren<Text>().text = BindingLabel(pair.Key);
        }
        public void Back()
        {
            if (_session.Listening) { _session.CancelRebind(); return; }
            if (_session.Dirty) { Decision(); return; }
            _canvas.gameObject.SetActive(false); _back?.Invoke();
        }
        private void DecisionPrevious()
        {
            if (_decision != null) { _decision.SetActive(true); return; }
            var f = TumpUiTheme.Current;
            var root = TumpUiFactory.Rect(_canvas.transform, "UnsavedDecision"); TumpUiFactory.Stretch(root); _decision = root.gameObject;
            TumpUiFactory.Ground(root, new Color(f.DeepOlive.r, f.DeepOlive.g, f.DeepOlive.b, .82f));
            var paper = TumpUiFactory.Surface(root, "DecisionPaper", TumpSurface.Form.Pebble, f.Cream);
            TumpUiFactory.Anchor(paper.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(960, 620));
            var title = TumpUiFactory.Text(paper.transform, "Heading", "Keep your changes?", 52, true);
            TumpUiFactory.Place(title.rectTransform, 90, 70, 800, 94);
            DecisionAction(paper.transform, "SaveAndBack", "Save & go back", 226, true, () => { _session.Save(); _decision.SetActive(false); Back(); });
            DecisionAction(paper.transform, "DiscardAndBack", "Discard changes", 366, false, () => { _session.Discard(); _decision.SetActive(false); Back(); });
            DecisionAction(paper.transform, "KeepEditing", "Keep editing", 484, false, () => _decision.SetActive(false));
            ScreenFocus.Install(root.gameObject).Rebuild();
        }
        private static void DecisionAction(Transform parent, string name, string words, float y, bool primary, Action action)
        {
            var button = TumpUiFactory.Button(parent, name, words, action, primary ? TumpSurface.Form.Slap : TumpSurface.Form.Link, TumpUiTheme.Current.Lime, 38);
            TumpUiFactory.Place((RectTransform)button.transform, 160, y, 640, 100);
        }
    }
}
