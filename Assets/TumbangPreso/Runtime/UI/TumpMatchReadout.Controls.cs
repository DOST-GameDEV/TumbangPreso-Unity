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

        private void BuildSpectatorHelp()
        {
            _spectatorHelp = OwnerUiLayout.Rect(_root, "SpectatorControlsPanel");
            PlaceSpectatorHelp();
            var plate = _spectatorHelp.gameObject.AddComponent<HudCard>();
            plate.Glass(new Color(.06f, .055f, .05f, .94f), 0).raycastTarget = false;
            plate.BorderWidth = 1; plate.FollowContrast = false;
            var title = Ink(_spectatorHelp, "SpectatorControlsTitle", "SPECTATOR CONTROLS", 38, true);
            title.alignment = TextAnchor.MiddleLeft;
            OwnerUiLayout.Place(title.rectTransform, 28, 14, 950, 52);
            var close = Ink(_spectatorHelp, "SpectatorControlsClose", "", 28, false);
            close.alignment = TextAnchor.MiddleRight;
            OwnerUiLayout.Place(close.rectTransform, 1000, 20, 270, 40);
            _spectatorBindings.Add((close, "SpectatorControls"));

            Group("Watch", 22, 80, new[] {
                ("SpectatorCycleTarget", "Next player"), ("SpectatorPov", "First / third person"),
                ("SpectatorAutopilot", "Automatic camera"), ("SpectatorFreeFly", "Free camera") });
            Group("Fly", 665, 80, new[] {
                ("SpectatorUp", "Rise"), ("SpectatorDown", "Lower"),
                ("Sprint", "Move faster"), ("SpectatorZoom", "Zoom / camera speed") });
            Group("Match time", 22, 292, new[] {
                ("SpectatorPause", "Pause / resume match"), ("SpectatorSpeed", "Cycle slow motion"),
                ("SpectatorReplay", "Instant replay") });
            Group("Camera tools", 665, 292, new[] {
                ("SpectatorMark", "Save camera position"), ("SpectatorRecall", "Return to saved position"),
                ("CleanFeed", "Hide / show HUD") });

            _directSeatKeys = Ink(_spectatorHelp, "SpectatorDirectSeatKeys", "", 28, false);
            _directSeatKeys.alignment = TextAnchor.MiddleLeft;
            OwnerUiLayout.Place(_directSeatKeys.rectTransform, 42, 478, 1216, 32);
            _spectatorHelp.gameObject.SetActive(false);
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
            Pin(_spectatorHelp, new Vector2(.5f, .5f), new Vector2(0, -55), new Vector2(1300, 520));
        }

        private void Group(string title, float x, float y, (string Action, string Label)[] rows)
        {
            var group = OwnerUiLayout.Rect(_spectatorHelp, "SpectatorGroup" + title.Replace(" ", ""));
            OwnerUiLayout.Place(group, x, y, 612, 242);
            var heading = Ink(group, "GroupHeading", title.ToUpperInvariant(), 30, true);
            heading.color = HudDraw.Honey; heading.alignment = TextAnchor.MiddleLeft;
            OwnerUiLayout.Place(heading.rectTransform, 20, 0, 570, 40);
            for (int i = 0; i < rows.Length; i++)
            {
                var key = OwnerUiLayout.Rect(group, rows[i].Action + "Key");
                OwnerUiLayout.Place(key, 18, 48 + i * 42, 210, 36);
                var cap = key.gameObject.AddComponent<HudCard>();
                cap.Glass(new Color(1, 1, 1, .075f), 0).raycastTarget = false;
                cap.BorderWidth = 1; cap.FollowContrast = false;
                var binding = Ink(key, "Binding", "", 28, false);
                OwnerUiLayout.Fill(binding.rectTransform);
                _spectatorBindings.Add((binding, rows[i].Action));
                var label = Ink(group, "ControlLabel", rows[i].Label, 28, false);
                label.alignment = TextAnchor.MiddleLeft;
                OwnerUiLayout.Place(label.rectTransform, 246, 48 + i * 42, 354, 36);
            }
        }

        private void TickSpectatorHelp(bool spectating, bool showControls)
        {
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
                    foreach (var row in _spectatorBindings)
                    {
                        string words = CompactBinding(Hud.KeyLabelFor(row.Action));
                        if (row.Action == "SpectatorControls") words += "  close";
                        if (row.Text.text != words) row.Text.text = words;
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
            if (_spectator.text != hint) _spectator.text = hint;
        }
    }
}
