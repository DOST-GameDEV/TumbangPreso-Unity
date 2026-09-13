using UnityEngine;

namespace TumbangPreso.UI
{
    public sealed partial class Hud
    {
        private TumpMatchReadout _nativeReadout;
        public bool NativePresentation => _nativeReadout != null;
        private void BuildNative()
        {
            _nativeReadout = gameObject.AddComponent<TumpMatchReadout>();
            _nativeReadout.Build(transform); _canvas = _nativeReadout.Canvas; _root = (RectTransform)_canvas.transform;
            _trainingChrome = GameLaunch.GuidedTutorial;
            var indicators = new GameObject("NativeTargetIndicators"); indicators.transform.SetParent(transform, false);
            _indicators = indicators.AddComponent<OffscreenIndicators>();
            if (NetAuthority.IsNetworked)
            {
                _chat = LobbyChat.Attach(_root, true);
                _chat.PlaceBottomRight(38, 232, 540);
            }
        }
        private void NativeTick()
        {
            if (_spectating && Fired(SpectatorAction("CleanFeed"))) SetCleanFeed(!_cleanFeed);
            if (_spectating && Fired(SpectatorAction("SpectatorControls"))) SetSpectatorControlsVisible(!_spectatorControlsVisible);
            TrySubscribeRound();
            _nativeReadout.Tick(_local, _spectating, _trainingChrome, _trainingDeckHidden, _spectatorControlsVisible);
            if (!_spectating && _local != null)
            {
                // Target resolution is retained; only the arrow objects are newly authored.
                UpdateIndicators();
                _indicators.SetCanArrowColour(_local.IsDefender ? TumpUiTheme.Current.Yellow : TumpUiTheme.Current.Cream);
            }
        }
        private void NativeSpectator(bool spectating)
        {
            _spectating = spectating;
            if (spectating) { _readyWindowOpen = false; _nativeReadout.ReadyWindow = false; }
            if (_indicators != null) _indicators.gameObject.SetActive(!spectating);
            foreach (var card in FindObjectsByType<YouCard>(FindObjectsInactive.Include, FindObjectsSortMode.None)) card.gameObject.SetActive(!spectating);
            foreach (var card in FindObjectsByType<RoleSwapCard>(FindObjectsInactive.Include, FindObjectsSortMode.None)) card.gameObject.SetActive(!spectating);
        }
    }
}
