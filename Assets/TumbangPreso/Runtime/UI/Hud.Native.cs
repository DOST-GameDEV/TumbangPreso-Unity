using UnityEngine;

namespace TumbangPreso.UI
{
    public sealed partial class Hud
    {
        private TumpMatchReadout _nativeReadout;
        private SlipperRecall _recall;
        public bool NativePresentation => _nativeReadout != null;
        private void BuildNative()
        {
            _nativeReadout = gameObject.AddComponent<TumpMatchReadout>();
            _nativeReadout.Build(transform); _canvas = _nativeReadout.Canvas; _root = (RectTransform)_canvas.transform;
            CameraSystem.CatchReconstruction.Attach(gameObject);
            _trainingChrome = GameLaunch.GuidedTutorial;
            var indicators = new GameObject("NativeTargetIndicators"); indicators.transform.SetParent(transform, false);
            _indicators = indicators.AddComponent<OffscreenIndicators>();

            // § THE RECALL MARK. Built beside the arrows and for the same reason they have their
            // own canvas: a world-tracking marker has to draw over the match chrome rather than
            // underneath whichever status row happened to be built after it.
            var recall = new GameObject("NativeRecallMark"); recall.transform.SetParent(transform, false);
            _recall = recall.AddComponent<SlipperRecall>();
            _recall.Build(recall.transform);
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

            // ⚠️ A SPECTATOR HAS NO TSINELAS TO RECALL. `CLAUDE.md` § 4 makes this argument for
            // the whole spectator set: no body, no seat, no `CharacterMotor`. It is turned off
            // here rather than left to `Track`'s own guard because `NativeTick` stops calling
            // that at all while spectating, so a mark drawn on the last played frame would
            // simply stay on screen. `PopHitmarker`'s note records the identical fault.
            if (_recall != null) _recall.SetVisible(!spectating);
            foreach (var card in FindObjectsByType<YouCard>(FindObjectsInactive.Include, FindObjectsSortMode.None)) card.gameObject.SetActive(!spectating);
            foreach (var card in FindObjectsByType<RoleSwapCard>(FindObjectsInactive.Include, FindObjectsSortMode.None)) card.gameObject.SetActive(!spectating);
        }
    }
}
