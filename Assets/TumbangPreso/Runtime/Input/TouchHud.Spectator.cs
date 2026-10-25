using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;
namespace TumbangPreso.InputLayer
{
    public sealed partial class TouchHud
    {
        private bool SpectatorMode => Hud.Instance != null && Hud.Instance.Spectating;
        private bool SpectatorCanOperate => !Panel.AnyOpen
            && (!ScreenTakeover.AnyOpen || (_watcher != null && !ScreenTakeover.AnyOpenOutside(_watcher.transform)))
            && !PresentationClock.Held && !SharedUltimatePhase.BlocksActions && !MatchArrivalPresentation.OwnsCamera;
        private RectTransform _spectatorStrip, _spectatorDrawer, _cameraTools, _matchTools;
        private SpectatorCamera _watcher;
        private bool _spectatorModeShown, _spectatorStripShown, _toolsOpen;
        private Button _touchPov, _touchReturn, _touchZoom;
        private HudCard _cameraTabFace, _matchTabFace;
        private Text _touchAutoLabel, _touchFeedLabel, _touchPauseLabel, _touchPovLabel, _touchToolsLabel, _touchHelp;
        private readonly List<Button> _spectatorButtons = new();
        private int _spectatorColumns;

        private void RefreshSpectatorControls()
        {
            bool watching = SpectatorMode && !TouchButton.Customising;
            if (watching != _spectatorModeShown)
            {
                _spectatorModeShown = watching; CancelTouches(); ApplyLayout();
            }
            bool visible = watching && SpectatorCanOperate && !SpectatorCutscene.Hides;
            if (visible && _spectatorStrip == null) BuildSpectatorControls();
            if (_spectatorStripShown != visible)
            {
                _spectatorStripShown = visible;
                if (_spectatorStrip != null) _spectatorStrip.gameObject.SetActive(visible);
                if (!visible) { _toolsOpen = false; CancelTouches(); }
            }
            if (_spectatorDrawer != null) _spectatorDrawer.gameObject.SetActive(visible && _toolsOpen);
            if (!visible) return;
            PlaceSpectatorControls();
            if (_watcher == null) _watcher = FindFirstObjectByType<SpectatorCamera>();
            bool available = _watcher != null && _watcher.isActiveAndEnabled;
            _touchPov.interactable = available && _watcher.HasFollowTarget;
            _touchReturn.interactable = available && _watcher.HasCameraMark;
            _touchZoom.interactable = available && !_watcher.WatchingFirstPerson;
            Set(_touchAutoLabel, available && _watcher.AutopilotEngaged ? "AUTO\nON" : "AUTO\nOFF");
            Set(_touchPovLabel, available && _watcher.WatchingFirstPerson ? "FIRST\nPERSON" : "THIRD\nPERSON");
            Set(_touchPauseLabel, PresentationClock.RequestedScale <= 0 ? "RESUME\nMATCH" : "PAUSE\nMATCH");
            Set(_touchFeedLabel, Hud.Instance != null && Hud.Instance.IsCleanFeed ? "SHOW\nHUD" : "CLEAN\nFEED");
            Set(_touchToolsLabel, _toolsOpen ? "CLOSE\nTOOLS" : "MORE\nTOOLS");
            static void Set(Text label, string words) { if (label.text != words) label.text = words; }
        }

        private void BuildSpectatorControls()
        {
            var root = _touchRoot != null ? _touchRoot : (RectTransform)_canvas.transform;
            _spectatorStrip = OwnerUiLayout.Rect(root, "SpectatorTouchStrip");
            _spectatorStrip.anchorMin = _spectatorStrip.anchorMax = _spectatorStrip.pivot = new Vector2(1, 0);
            _spectatorStrip.anchoredPosition = new Vector2(-28, 68);
            _touchAutoLabel = Add(_spectatorStrip, "Auto", "AUTO\nOFF", () => Command(SpectatorCamera.ViewCommand.Autopilot)).GetComponentInChildren<Text>();
            Add(_spectatorStrip, "Follow", "NEXT\nPLAYER", () => Command(SpectatorCamera.ViewCommand.FollowNext));
            _touchPov = Add(_spectatorStrip, "Pov", "THIRD\nPERSON", () => Command(SpectatorCamera.ViewCommand.Pov));
            _touchPovLabel = _touchPov.GetComponentInChildren<Text>();
            _touchPauseLabel = Add(_spectatorStrip, "Pause", "PAUSE\nMATCH", () => Command(SpectatorCamera.ViewCommand.Pause)).GetComponentInChildren<Text>();
            _touchToolsLabel = Add(_spectatorStrip, "Tools", "MORE\nTOOLS", () => _toolsOpen = !_toolsOpen).GetComponentInChildren<Text>();

            _spectatorDrawer = OwnerUiLayout.Rect(root, "SpectatorTouchTools");
            _spectatorDrawer.anchorMin = _spectatorDrawer.anchorMax = _spectatorDrawer.pivot = new Vector2(1, 0);
            _spectatorDrawer.anchoredPosition = new Vector2(-28, 236);
            var plate = _spectatorDrawer.gameObject.AddComponent<HudCard>();
            plate.Glass(new Color(.06f, .055f, .05f, .96f), 0); plate.raycastTarget = true;
            var cameraTab = Add(_spectatorDrawer, "CameraTab", "CAMERA", () => ShowTools(true), false);
            var matchTab = Add(_spectatorDrawer, "MatchTab", "MATCH", () => ShowTools(false), false);
            _cameraTabFace = cameraTab.GetComponent<HudCard>(); _matchTabFace = matchTab.GetComponent<HudCard>();
            OwnerUiLayout.Place((RectTransform)cameraTab.transform, 16, 12, 354, 80);
            OwnerUiLayout.Place((RectTransform)matchTab.transform, 386, 12, 354, 80);
            _cameraTools = OwnerUiLayout.Rect(_spectatorDrawer, "CameraTools"); OwnerUiLayout.Fill(_cameraTools);
            _matchTools = OwnerUiLayout.Rect(_spectatorDrawer, "MatchTools"); OwnerUiLayout.Fill(_matchTools);
            Tool(_cameraTools, "Free", "FREE\nCAMERA", 0, SpectatorCamera.ViewCommand.FreeFlight);
            _touchZoom = Tool(_cameraTools, "Zoom", "ZOOM /\nFLY SPEED", 1, SpectatorCamera.ViewCommand.Zoom);
            Tool(_cameraTools, "Rise", "MOVE\nUP", 2, SpectatorCamera.ViewCommand.Rise);
            Tool(_cameraTools, "Lower", "MOVE\nDOWN", 3, SpectatorCamera.ViewCommand.Lower);
            Tool(_cameraTools, "Save", "SAVE\nCAMERA", 4, SpectatorCamera.ViewCommand.Save);
            _touchReturn = Tool(_cameraTools, "Return", "RETURN\nCAMERA", 5, SpectatorCamera.ViewCommand.Recall);
            Tool(_matchTools, "Replay", "INSTANT\nREPLAY", 0, SpectatorCamera.ViewCommand.Replay);
            Tool(_matchTools, "Speed", "SLOW\nMOTION", 1, SpectatorCamera.ViewCommand.Speed);
            _touchFeedLabel = Tool(_matchTools, "Feed", "CLEAN\nFEED", 2, SpectatorCamera.ViewCommand.CleanFeed).GetComponentInChildren<Text>();
            var help = Add(_matchTools, "Controls", "TOUCH\nHELP", () => _touchHelp.enabled = !_touchHelp.enabled, false);
            PlaceTool(help, 3);
            var menu = Add(_matchTools, "Menu", "MATCH\nMENU", () => Panel.Open<PausePanel>(this), false); PlaceTool(menu, 4);
            var done = Add(_matchTools, "Done", "DONE", () => _toolsOpen = false, false); PlaceTool(done, 5);
            _touchHelp = Label(_spectatorDrawer, "TouchHelp", "Left stick: fly. Drag right: look. Next player: follow. Tap view to switch eyes / body.");
            OwnerUiLayout.Place(_touchHelp.rectTransform, 24, 370, 708, 64);
            ShowTools(true); _spectatorDrawer.gameObject.SetActive(false);
            var focus = _canvas.GetComponent<ScreenFocus>(); if (focus != null) focus.enabled = false;
        }

        private static Text Label(Transform parent, string name, string words)
        {
            var text = OwnerUiLayout.Text(parent, name, words, 28, OwnerUiLayout.TypeRole.Display);
            text.color = HudDraw.Ivory; text.alignment = TextAnchor.MiddleCenter;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = text.gameObject.AddComponent<Outline>(); outline.effectColor = UiTheme.InGameOutline;
            outline.effectDistance = new Vector2(1.5f, -1.5f); return text;
        }
        private Button Add(Transform parent, string name, string words, System.Action pressed, bool primary = true)
        {
            var rect = OwnerUiLayout.Rect(parent, "SpectatorTouch" + name);
            var face = rect.gameObject.AddComponent<HudCard>();
            face.Glass(new Color(.12f, .105f, .08f, .9f), 0); face.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = face;
            var colors = button.colors; colors.highlightedColor = new Color(1, .88f, .55f); colors.pressedColor = new Color(.75f, .6f, .25f);
            colors.disabledColor = new Color(1, 1, 1, .3f); button.colors = colors;
            var label = Label(rect, "Label", words); OwnerUiLayout.Fill(label.rectTransform);
            button.onClick.AddListener(() => { CancelTouches(); pressed(); });
            if (primary) _spectatorButtons.Add(button);
            return button;
        }
        private Button Tool(Transform parent, string name, string words, int index, SpectatorCamera.ViewCommand command)
        {
            var button = Add(parent, name, words, () => Command(command), false); PlaceTool(button, index); return button;
        }
        private static void PlaceTool(Button button, int index)
            => OwnerUiLayout.Place((RectTransform)button.transform, 16 + index % 3 * 248, 108 + index / 3 * 128, 232, 112);
        private void ShowTools(bool camera)
        {
            _cameraTools.gameObject.SetActive(camera); _matchTools.gameObject.SetActive(!camera);
            _cameraTabFace.Border = camera ? HudDraw.Honey : HudDraw.GlassEdge;
            _matchTabFace.Border = camera ? HudDraw.GlassEdge : HudDraw.Honey;
            _cameraTabFace.BorderWidth = camera ? 3 : 1; _matchTabFace.BorderWidth = camera ? 1 : 3;
            _cameraTabFace.SetVerticesDirty(); _matchTabFace.SetVerticesDirty();
        }
        private void PlaceSpectatorControls()
        {
            int columns = _canvas.pixelRect.width < _canvas.pixelRect.height ? 3 : 5;
            if (_spectatorColumns == columns) return;
            _spectatorColumns = columns;
            int rows = (5 + columns - 1) / columns;
            _spectatorStrip.sizeDelta = new Vector2(columns * 156 - 12, rows * 156 - 12);
            for (int i = 0; i < _spectatorButtons.Count; i++)
                OwnerUiLayout.Place((RectTransform)_spectatorButtons[i].transform, i % columns * 156, i / columns * 156, 144, 144);
            _spectatorDrawer.sizeDelta = new Vector2(756, 448);
            _spectatorDrawer.anchoredPosition = new Vector2(-28, 68 + rows * 156 + 12);
        }
        private void Command(SpectatorCamera.ViewCommand command)
        {
            if (_watcher == null) _watcher = FindFirstObjectByType<SpectatorCamera>();
            _watcher?.ExecuteViewCommand(command);
        }
    }
}
