using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class QueueCard
    {
        private bool _nativeQueue;
        private RectTransform _nativeQueueBounds;
        private void ConstructNativeQueue()
        {
            _nativeQueue = true;
            var f = TumpUiTheme.Current;
            _queue = Matchmaker.Ensure();
            _queue.Changed += Refresh; _queue.Joined += OnQueueJoined;
            if (!_docked)
            {
                _open = TumpUiFactory.Button(transform, "QuickMatchButton", "Find a match", OnQuickMatchPressed, TumpSurface.Form.Slap, f.Lime, 38);
                TumpUiFactory.Anchor((RectTransform)_open.transform, new Vector2(.5f, 0), new Vector2(0, 96), new Vector2(560, 100));
            }
            else if (_open != null) _open.onClick.AddListener(OnQuickMatchPressed);
            // This is deliberately a nonmodal status ticket in the owning lobby canvas.
            // Players can keep using their loadout and party controls while searching.
            var surface = TumpUiFactory.Surface(transform, "NativeQueueState", TumpSurface.Form.Ticket, f.DeepOlive);
            surface.raycastTarget = true; _card = surface.gameObject;
            if (_docked)
            {
                _nativeQueueBounds = (RectTransform)transform;
                _nativeQueueBounds.sizeDelta = new Vector2(620, 440);
                TumpUiFactory.Stretch(surface.rectTransform);
            }
            else
            {
                _nativeQueueBounds = surface.rectTransform;
                TumpUiFactory.Anchor(_nativeQueueBounds, new Vector2(.5f, 0), new Vector2(0, 180), new Vector2(620, 440));
                _nativeQueueBounds.pivot = new Vector2(.5f, 0);
            }
            var root = surface.rectTransform;
            _headline = TumpUiFactory.Text(root, "QueueHeadline", "Finding a match", 36, true);
            _headline.color = f.Cream; TumpUiFactory.Place(_headline.rectTransform, 26, 18, 568, 72);
            _band = TumpUiFactory.Text(root, "SearchBand", "", 26); _band.color = f.Cream;
            TumpUiFactory.Place(_band.rectTransform, 28, 102, 560, 68);
            _elapsed = TumpUiFactory.Text(root, "QueueElapsed", "", 30, true); _elapsed.color = f.Yellow;
            TumpUiFactory.Place(_elapsed.rectTransform, 28, 174, 560, 64);
            var track = TumpUiFactory.Rect(root, "SearchRangeTrack").gameObject.AddComponent<Image>();
            track.color = f.OliveSand; track.raycastTarget = false;
            TumpUiFactory.Place(track.rectTransform, 30, 254, 556, 10);
            _barFill = TumpUiFactory.Rect(track.transform, "SearchRangeFill").gameObject.AddComponent<Image>();
            _barFill.color = f.Lime; _barFill.raycastTarget = false; TumpUiFactory.Stretch(_barFill.rectTransform);
            _promise = TumpUiFactory.Text(root, "RotationPromise", MatchmakingRules.TayaRotationPromise, 23);
            _promise.color = f.Cream; TumpUiFactory.Place(_promise.rectTransform, 28, 276, 560, 74);
            _fill = TumpUiFactory.Button(root, "StartWithBotsButton", "", OnFillPressed, TumpSurface.Form.Pebble, f.Lime, 28);
            TumpUiFactory.Place((RectTransform)_fill.transform, 22, 356, 374, 74);
            _fillLabel = _fill.GetComponentInChildren<Text>();
            _fillCaveat = TumpUiFactory.Text(root, "BotFillCaveat", "", 24); _fillCaveat.color = f.Cream;
            TumpUiFactory.Place(_fillCaveat.rectTransform, 30, 448, 556, 110);
            _cancel = TumpUiFactory.Button(root, "CancelQueueButton", "Cancel", OnCancelPressed, TumpSurface.Form.Link, f.Cream, 30);
            _cancel.GetComponent<TumpSurface>().LightInk = true;
            _cancel.GetComponentInChildren<Text>().color = f.Cream;
            TumpUiFactory.Place((RectTransform)_cancel.transform, 414, 352, 182, 82);
            RefreshNativeQueue();
        }
        private void RefreshNativeQueue()
        {
            if (_queue == null || _card == null) return;
            bool queueing = _queue.IsQueueing;
            bool wasVisible = _card.activeSelf;
            if (_open != null)
            {
                _open.interactable = !queueing;
                if (!_docked) _open.gameObject.SetActive(!queueing);
            }
            _card.SetActive(queueing && gameObject.activeInHierarchy);
            if (!queueing) return;
            _headline.text = _queue.State == QueueState.Joining ? "Found a match" : _queue.State == QueueState.Hosting ? "Opening a room"
                : MatchmakingRules.TakesAnybody(_queue.Elapsed) ? "Searching everywhere" : "Finding a match";
            _band.text = (_queue.Mode == GameMode.Classic ? "Classic" : "Hero Strike") + " · " + _queue.SearchLabel;
            _barFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(_queue.WideningProgress), 1);
            _barFill.enabled = _queue.WideningProgress > .001f;
            int seconds = Mathf.FloorToInt(_queue.Elapsed);
            _elapsed.text = (seconds < 60 ? seconds + "s" : seconds / 60 + "m " + (seconds % 60).ToString("00") + "s") + " waiting"
                + (_queue.PartySize > 1 ? " · " + _queue.PartySize + " of you" : "");
            bool fill = _queue.OffersBotFill;
            bool fillChanged = _fill.gameObject.activeSelf != fill;
            _fill.gameObject.SetActive(fill); _fillCaveat.gameObject.SetActive(fill);
            if (fill)
            {
                _fillLabel.text = BotFillRules.FillOffer(_queue.BotsToFill);
                _fillCaveat.text = BotFillRules.FillCaveat(_queue.Stake, _queue.PartySize, Balance.PlayerCount);
            }
            _nativeQueueBounds.sizeDelta = new Vector2(620, fill ? 580 : 440);
            if (!wasVisible || fillChanged) GetComponentInParent<ScreenFocus>()?.Rebuild();
        }
        private void Update()
        {
            if (!_nativeQueue || _card == null || !_card.activeInHierarchy) return;
            RefreshNativeQueue();
            if (!MenuNav.CancelPressed || ScreenTakeover.EscapeIsSpoken) return;
            ScreenTakeover.ConsumeEscape(); OnCancelPressed();
        }
        public void CancelSearch() => OnCancelPressed();
        private void OnDisable() { if (_card != null) _card.SetActive(false); }
        private void OnEnable() { if (_card != null) RefreshNativeQueue(); }
    }
}
