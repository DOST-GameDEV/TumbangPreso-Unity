using System.Collections.Generic;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpMatchReadout
    {
        private RectTransform _spectatorHelp;
        private readonly List<(Text Text, string Action)> _spectatorBindings = new();
        private int _spectatorBindingRevision = -1, _spectatorDeviceRevision = -1;
        private Text _directSeatKeys;
        private Text _spectatorViewMode;
        private float _spectatorStatusAt;
        private bool _spectatorControlsShown;
        private Vector2 _helpViewport;

        private readonly List<(Image Icon, Text Words, Image Cap, string Action)> _spectatorKeys = new();
        private static readonly Color SpectatorTrack = new Color32(0x14, 0x12, 0x1C, 235);

        /// <summary>
        /// The spectator controls on the HUD's grey woven plate (owner, 2026-11-08: the spectator screens take the in-match
        /// HUD's grey, not maroon): the F1 plates' stroke with the quiet weave, the F1 dark box behind the title, lime
        /// section words, every key as its Xelu icon (`InputGlyphs.For`, never redrawn) and its words in Grandstander.
        /// The board `match-spectator-controls` sets the layout: two columns of two groups and a line of extra keys.
        /// </summary>
        private void BuildSpectatorHelp()
        {
            _spectatorHelp = OwnerUiLayout.Rect(_root, "SpectatorControlsPanel");
            PlaceSpectatorHelp();
            var panel = _spectatorHelp.gameObject.AddComponent<Image>(); panel.raycastTarget = false;
            panel.sprite = Hub.HubKnit.Knitted(LiveHud.Folder + "ref-frame-flat", new Vector4(18, 18, 18, 18)); panel.type = Image.Type.Sliced;
            KitPanelWeave.On(_spectatorHelp);
            // The F1 header box as a band across the top, its rounded ends kept by slicing.
            var band = OwnerUiLayout.Rect(_spectatorHelp, "HeaderBox").gameObject.AddComponent<Image>();
            band.sprite = Hub.HubKnit.Knitted(LiveHud.Folder + "band-top-690x94", new Vector4(26, 0, 26, 0)); band.type = Image.Type.Sliced;
            band.raycastTarget = false; OwnerUiLayout.Place(band.rectTransform, 0, 0, 1380, 94);
            var title = Hub.HubKnit.Display(_spectatorHelp, "SpectatorControlsTitle", "SPECTATOR CONTROLS", 52, Color.white);
            OwnerUiLayout.Place(title.rectTransform, 40, 14, 900, 66);
            Key(_spectatorHelp, "SpectatorControls", 1110, 23, "SpectatorControlsClose", "close");

            Group("Watch", 40, 118, new[] {
                ("SpectatorCycleTarget", "Next player"), ("SpectatorPov", "First / third person"),
                ("SpectatorAutopilot", "Automatic camera"), ("SpectatorFreeFly", "Free camera") });
            Group("Fly", 700, 118, new[] {
                ("SpectatorUp", "Rise"), ("SpectatorDown", "Lower"),
                ("Sprint", "Move faster"), ("SpectatorZoom", "Zoom / camera speed") });
            Group("Match time", 40, 380, new[] {
                ("SpectatorPause", "Pause / resume match"), ("SpectatorSpeed", "Cycle slow motion"),
                ("SpectatorReplay", "Instant replay") });
            Group("Camera tools", 700, 380, new[] {
                ("SpectatorMark", "Save camera position"), ("SpectatorRecall", "Return to saved position"),
                ("CleanFeed", "Hide / show HUD") });

            _directSeatKeys = Hub.HubKnit.Words(_spectatorHelp, "SpectatorDirectSeatKeys", "", 30, Color.white);
            OwnerUiLayout.Place(_directSeatKeys.rectTransform, 40, 612, 1300, 44);
            _spectatorHelp.gameObject.SetActive(false);
        }

        /// <summary>One control: its Xelu icon 48 tall (a word on the deep track only if the pack has none), then its words.</summary>
        private void Key(RectTransform parent, string action, float x, float y, string name, string words)
        {
            var key = OwnerUiLayout.Rect(parent, name);
            OwnerUiLayout.Place(key, x, y, 600, 48);
            var cap = OwnerUiLayout.Rect(key, "Cap").gameObject.AddComponent<Image>(); cap.color = SpectatorTrack; cap.raycastTarget = false;
            OwnerUiLayout.Place(cap.rectTransform, 0, 0, 48, 48);
            var fallback = Hub.HubKnit.Words(cap.transform, "Binding", "", 26, Color.white, TextAnchor.MiddleCenter);
            OwnerUiLayout.Fill(fallback.rectTransform); fallback.horizontalOverflow = HorizontalWrapMode.Overflow;
            var icon = OwnerUiLayout.Rect(key, "Icon").gameObject.AddComponent<Image>(); icon.preserveAspect = true; icon.raycastTarget = false;
            OwnerUiLayout.Place(icon.rectTransform, 0, 0, 48, 48);
            var label = Hub.HubKnit.Words(key, "ControlLabel", words, 34, Color.white);
            OwnerUiLayout.Place(label.rectTransform, 64, 0, 530, 48);
            _spectatorKeys.Add((icon, fallback, cap, action));
        }

        private static string CompactBinding(string value)
            => value.ToUpperInvariant().Replace("LEFT CONTROL", "L CTRL").Replace("RIGHT CONTROL", "R CTRL")
                .Replace("LEFT SHIFT", "L SHIFT").Replace("RIGHT SHIFT", "R SHIFT")
                .Replace("LEFT ALT", "L ALT").Replace("RIGHT ALT", "R ALT");

        private void PlaceSpectatorHelp()
        {
            Vector2 viewport = Canvas.pixelRect.size;
            if (_helpViewport == viewport) return;
            _helpViewport = viewport;
            Pin(_spectatorHelp, new Vector2(.5f, .5f), new Vector2(0, -10), new Vector2(1380, 680));
        }

        private void Group(string title, float x, float y, (string Action, string Label)[] rows)
        {
            var group = OwnerUiLayout.Rect(_spectatorHelp, "SpectatorGroup" + title.Replace(" ", ""));
            OwnerUiLayout.Place(group, x, y, 640, 260);
            // What the group is for, in lime (the training panel's colour for what to press).
            var heading = Hub.HubKnit.Display(group, "GroupHeading", title.ToUpperInvariant(), 32, new Color32(0xBF, 0xD8, 0x3A, 255));
            OwnerUiLayout.Place(heading.rectTransform, 0, 0, 600, 44);
            for (int i = 0; i < rows.Length; i++) Key(group, rows[i].Action, 0, 54 + i * 52, rows[i].Action + "Key", rows[i].Label);
        }

        private void TickSpectatorHelp(bool spectating, bool showControls)
        {
            bool touch = TouchHud.ShouldShow;
            if (touch) showControls = false;
            if (spectating && _spectatorViewMode == null)
            {
                _spectatorViewMode = Ink(_root, "SpectatorViewMode", "", 28, false);
                _spectatorViewMode.color = HudDraw.Honey;
                Pin(_spectatorViewMode.rectTransform, new Vector2(.5f, 0), new Vector2(0, 84), new Vector2(1200, 40));
            }
            if (_spectatorViewMode != null) _spectatorViewMode.enabled = spectating;
            if (spectating && showControls && _spectatorHelp == null) BuildSpectatorHelp();
            if (_spectatorHelp != null)
            {
                bool shown = spectating && showControls;
                if (_spectatorHelp.gameObject.activeSelf != shown) _spectatorHelp.gameObject.SetActive(shown);
                if (shown) PlaceSpectatorHelp();
                if (shown && (_spectatorBindingRevision != Settings.Rebinding.Revision
                    || _spectatorDeviceRevision != LastInputDevice.Revision))
                {
                    _spectatorBindingRevision = Settings.Rebinding.Revision;
                    _spectatorDeviceRevision = LastInputDevice.Revision;
                    foreach (var key in _spectatorKeys)
                    {
                        string label = Hud.KeyLabelFor(key.Action);
                        var sprite = InputGlyphs.For(label, onDark: true);
                        key.Icon.sprite = sprite; key.Icon.enabled = sprite != null;
                        key.Cap.enabled = sprite == null; key.Words.text = sprite == null ? CompactBinding(label) : "";
                        float width = sprite != null ? InputGlyphs.PromptWidth(sprite, 48) : Mathf.Max(48, key.Words.preferredWidth + 20);
                        key.Icon.rectTransform.sizeDelta = new Vector2(width, 48); key.Cap.rectTransform.sizeDelta = new Vector2(width, 48);
                        var words = key.Icon.transform.parent.Find("ControlLabel") as RectTransform;
                        if (words != null) words.anchoredPosition = new Vector2(width + 16, words.anchoredPosition.y);
                    }
                    _directSeatKeys.text = LastInputDevice.Current == InputDeviceKind.KeyboardMouse
                        ? "Movement keys: move. Mouse: look. F1-F4: player 1-4. Wheel: fine zoom / speed."
                        : "Left stick: move. Right stick: look. Follow a player, then switch first / third person.";
                }
            }
            _spectator.enabled = spectating;
            if (!spectating) return;
            // The restore hint stays visible when the full control list is hidden.
            bool changed = _spectatorControlsShown != showControls;
            _spectatorControlsShown = showControls;
            if (!changed && Time.unscaledTime < _spectatorStatusAt) return;
            _spectatorStatusAt = Time.unscaledTime + .15f;
            if (_spectatorCamera == null) _spectatorCamera = FindAnyObjectByType<CameraSystem.SpectatorCamera>();
            string mode = _spectatorCamera != null ? _spectatorCamera.ViewLabel : "SPECTATING";
            if (_spectatorViewMode.text != mode) _spectatorViewMode.text = mode;
            string hint = Hud.KeyLabelFor("SpectatorControls")
                + (showControls ? " hide controls" : " show controls");
            if (showControls) hint += "  |  " + Hud.KeyLabelFor("CleanFeed") + " hide HUD";
            if (touch) hint = "Left stick: fly  |  Drag right: look  |  More tools: camera and match";
            if (_spectator.text != hint) _spectator.text = hint;
        }
    }
}
