using TumbangPreso.CameraSystem;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.UI;
namespace TumbangPreso.InputLayer
{
    public sealed partial class TouchHud
    {
        private bool SpectatorMode => Hud.Instance != null && Hud.Instance.Spectating;
        private bool SpectatorCanOperate => !Panel.AnyOpen && !ScreenTakeover.AnyOpen
            && !PresentationClock.BlocksInput && !MatchArrivalPresentation.OwnsCamera;
        private RectTransform _spectatorStrip;
        private SpectatorCamera _watcher;
        private bool _spectatorModeShown, _spectatorStripShown;
        private Button _touchPov;
        private Text _touchAutoLabel, _touchFeedLabel;
        private bool _autoLabelOn, _feedLabelOn;
        private readonly Button[] _spectatorButtons = new Button[7];
        private int _spectatorColumns;
        private void RefreshSpectatorControls()
        {
            bool watching = SpectatorMode && !TouchButton.Customising;
            if (watching != _spectatorModeShown)
            {
                _spectatorModeShown = watching; TouchInput.ReleaseAll(); ApplyLayout();
            }
            bool visible = watching && SpectatorCanOperate;
            if (visible && _spectatorStrip == null) BuildSpectatorControls();
            if (_spectatorStripShown != visible)
            {
                _spectatorStripShown = visible;
                if (_spectatorStrip != null) _spectatorStrip.gameObject.SetActive(visible);
                if (!visible && watching) TouchInput.ReleaseAll();
            }
            if (!visible) return;
            PlaceSpectatorControls();
            if (_watcher == null) _watcher = FindFirstObjectByType<SpectatorCamera>();
            bool available = _watcher != null && _watcher.isActiveAndEnabled;
            if (_touchPov != null) _touchPov.interactable = available && _watcher.HasFollowTarget;
            bool auto = available && _watcher.AutopilotEngaged;
            if (auto != _autoLabelOn) { _autoLabelOn = auto; _touchAutoLabel.text = auto ? "AUTO ON" : "AUTO OFF"; }
            bool clean = Hud.Instance != null && Hud.Instance.IsCleanFeed;
            if (clean != _feedLabelOn) { _feedLabelOn = clean; _touchFeedLabel.text = clean ? "SHOW HUD" : "CLEAN FEED"; }
        }
        private void BuildSpectatorControls()
        {
            var root = (RectTransform)_canvas.transform;
            _spectatorStrip = OwnerUiLayout.Rect(root, "SpectatorTouchStrip");
            _spectatorStrip.anchorMin = _spectatorStrip.anchorMax = _spectatorStrip.pivot = new Vector2(1, 0);
            _spectatorStrip.anchoredPosition = new Vector2(-32, 230);
            _spectatorStrip.sizeDelta = new Vector2(1064, 312);
            Add("Auto", "AUTO OFF", 0, () => Command(SpectatorCamera.ViewCommand.Autopilot));
            Add("Follow", "FOLLOW", 1, () => Command(SpectatorCamera.ViewCommand.FollowNext));
            _touchPov = Add("Pov", "POV", 2, () => Command(SpectatorCamera.ViewCommand.Pov));
            Add("Free", "FREE", 3, () => Command(SpectatorCamera.ViewCommand.FreeFlight));
            Add("Feed", "CLEAN FEED", 4, () => Command(SpectatorCamera.ViewCommand.CleanFeed));
            Add("Controls", "CONTROLS", 5, () => Command(SpectatorCamera.ViewCommand.Controls));
            Add("Menu", "MENU", 6, () => Panel.Open<PausePanel>(this));
            Button Add(string name, string words, int index, System.Action pressed)
            {
                var button = HubKit.Button(_spectatorStrip, "SpectatorTouch" + name, words,
                    index == 0 ? HubStyle.Chartreuse : HubStyle.Honey, pressed, 40);
                OwnerUiLayout.Place((RectTransform)button.transform, (index % 4) * 272, (index / 4) * 168, 248, 144);
                if (index == 0) _touchAutoLabel = button.GetComponentInChildren<Text>();
                if (index == 4) _touchFeedLabel = button.GetComponentInChildren<Text>();
                _spectatorButtons[index] = button;
                return button;
            }
            var focus = _canvas.GetComponent<ScreenFocus>(); if (focus != null) focus.enabled = false;
        }
        private void PlaceSpectatorControls()
        {
            int columns = _canvas.pixelRect.width < _canvas.pixelRect.height ? 2 : 4;
            if (_spectatorColumns == columns) return;
            _spectatorColumns = columns;
            int rows = (7 + columns - 1) / columns;
            _spectatorStrip.sizeDelta = new Vector2(columns * 272 - 24, rows * 168 - 24);
            for (int i = 0; i < _spectatorButtons.Length; i++)
                OwnerUiLayout.Place((RectTransform)_spectatorButtons[i].transform,
                    (i % columns) * 272, (i / columns) * 168, 248, 144);
        }
        private void Command(SpectatorCamera.ViewCommand command)
        {
            if (_watcher == null) _watcher = FindFirstObjectByType<SpectatorCamera>();
            _watcher?.ExecuteViewCommand(command);
        }
    }
}
