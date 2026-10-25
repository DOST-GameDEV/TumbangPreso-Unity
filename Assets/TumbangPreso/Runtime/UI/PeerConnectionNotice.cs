using TumbangPreso.Net;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    // Spectator reaction time is visible time, including after a cutscene/menu.
    // This observes the existing validated departure; it never requests a pause.
    [DefaultExecutionOrder(32000)]
    public sealed class PeerConnectionNotice : MonoBehaviour
    {
        private Canvas _canvas;
        public const float CardHeight = 144;
        private Text _text, _title, _detail, _key, _pause;
        private HudCard _plate, _keyPlate;
        private Image _accent;
        private RectTransform _rect;
        private MatchEventFeed _feed;
        private long _match;
        private int _seen;
        private float _left, _hintAt;
        private bool _showDetail = true, _showKey = true;
        public float VisibleSecondsLeft => _left;
        public string RawNotice { get; private set; } = "";
        public bool IsShowing => _text != null && _text.enabled && _canvas != null && _canvas.isActiveAndEnabled;
        public static PeerConnectionNotice Create(RectTransform parent)
        {
            var rect = OwnerUiLayout.Rect(parent, "PeerConnectionNotice");
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-30, TopFor(Hud.OnTouch)); rect.sizeDelta = new Vector2(500, CardHeight);
            var notice = rect.gameObject.AddComponent<PeerConnectionNotice>();
            notice._rect = rect;
            notice._canvas = parent.GetComponentInParent<Canvas>();
            notice._plate = rect.gameObject.AddComponent<HudCard>();
            notice._plate.Glass(HudDraw.GlassDeep, 0, 8).raycastTarget = false;
            notice._accent = OwnerUiLayout.Rect(rect, "ConnectionAccent").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(notice._accent.rectTransform, 0, 14, 4, CardHeight - 28);
            notice._accent.color = HudDraw.Honey; notice._accent.raycastTarget = false;
            notice._title = Words(rect, "ConnectionStatus", 18, 10, 464, 40, true);
            notice._title.color = HudDraw.Honey;
            notice._text = Words(rect, "ConnectionPlayer", 18, 49, 464, 40);
            notice._detail = Words(rect, "ConnectionOutcome", 18, 96, 232, 36);
            notice._detail.color = new Color(.9f, .84f, .75f, 1);
            notice._keyPlate = OwnerUiLayout.Rect(rect, "ConnectionPauseKey").gameObject.AddComponent<HudCard>();
            notice._keyPlate.Glass(HudDraw.GlassDeep, 0, 5).raycastTarget = false;
            notice._keyPlate.Border = HudDraw.Honey; notice._keyPlate.BorderWidth = 1;
            notice._key = Words(rect, "ConnectionPauseBinding", 324, 94, 42, 40);
            notice._key.alignment = TextAnchor.MiddleCenter;
            notice._pause = Words(rect, "ConnectionPauseAction", 378, 96, 104, 36, true);
            notice._pause.text = "PAUSE";
            notice.SetVisible(false); return notice;
        }
        public static float TopFor(bool touch) => touch ? -170 : -150;
        public void ShareRailWith(MatchEventFeed feed) => _feed = feed;
        private static Text Words(RectTransform root, string name, float x, float y, float width, float height, bool display = false)
        {
            var text = OwnerUiLayout.Text(root, name, "", 28, display ? OwnerUiLayout.TypeRole.Display : OwnerUiLayout.TypeRole.Reading); text.color = HudDraw.Ivory;
            text.supportRichText = false; text.raycastTarget = false;
            text.verticalOverflow = VerticalWrapMode.Overflow; text.horizontalOverflow = HorizontalWrapMode.Overflow;
            OwnerUiLayout.Place(text.rectTransform, x, y, width, height);
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = UiTheme.InGameOutline; outline.effectDistance = new Vector2(1, -1);
            return text;
        }
        public void Present(string text, int seat = -1, string name = null, bool intentional = false, bool bot = false)
        {
            RawNotice = text;
            _title.text = intentional ? "PLAYER LEFT" : "PLAYER DISCONNECTED";
            _detail.text = bot ? "Bot took over" : "Seat saved";
            string prefix = seat >= 0 ? "P" + (seat + 1) + " · " : "";
            var full = new StringInfo(name ?? text); int count = full.LengthInTextElements;
            do
            {
                _text.text = prefix + full.SubstringByTextElements(0, count) + (count < full.LengthInTextElements ? "…" : "");
                if (_text.preferredWidth <= _text.rectTransform.rect.width || count == 0) break;
                count--;
            } while (true);
            _left = 12; _hintAt = 0;
            transform.SetAsLastSibling();
        }
        private void LateUpdate()
        {
            if (_text == null) return;
            if (!(GameLaunch.Spectator || NetAuthority.IsSeatlessReferee) || !NetAuthority.IsNetworked)
            { _left = 0; SetVisible(false); return; }
            var rpc = MatchRpc.Instance;
            if (rpc != null)
            {
                if (_match != rpc.PresentationMatchId) { _match = rpc.PresentationMatchId; _seen = 0; _left = 0; }
                if (rpc.PeerDepartureNotices > _seen)
                { _seen = rpc.PeerDepartureNotices; Present(rpc.LastPeerDepartureText, rpc.LastPeerDepartureSeat,
                    rpc.LastPeerDepartureName, rpc.LastPeerDepartureIntentional, rpc.LastPeerDepartureBot); }
            }
            if (PresentationClock.RequestedScale <= 0) _left = 0;
            bool visible = _left > 0 && _canvas != null && _canvas.isActiveAndEnabled
                && _text.canvasRenderer.GetInheritedAlpha() > .95f && !ScreenTakeover.AnyOpen;
            SetVisible(visible);
            if (!visible) return;
            var position = new Vector2(-30, TopFor(Hud.OnTouch));
            if (_rect.anchoredPosition != position) _rect.anchoredPosition = position;
            _left = Mathf.Max(0, _left - Time.unscaledDeltaTime);
            if (Time.unscaledTime >= _hintAt)
            {
                _hintAt = Time.unscaledTime + .5f;
                _key.text = Hud.OnTouch ? "" : Hud.KeyLabelFor("SpectatorPause");
                float keyWidth = Mathf.Clamp(_key.preferredWidth + 16, 40, 320);
                OwnerUiLayout.Place(_keyPlate.rectTransform, 366 - keyWidth, 94, keyWidth, 40);
                OwnerUiLayout.Place(_key.rectTransform, 366 - keyWidth + 6, 94, keyWidth - 12, 40);
                _showDetail = keyWidth < 100; _showKey = !Hud.OnTouch;
                _detail.enabled = _showDetail; _key.enabled = _keyPlate.enabled = _showKey;
                _pause.text = Hud.OnTouch ? "TAP PAUSE" : "PAUSE";
                OwnerUiLayout.Place(_pause.rectTransform, Hud.OnTouch ? 260 : 378, 96, Hud.OnTouch ? 222 : 104, 36);
            }
        }
        private void SetVisible(bool visible)
        {
            _text.enabled = _title.enabled = _pause.enabled = visible;
            _detail.enabled = visible && _showDetail; _key.enabled = _keyPlate.enabled = visible && _showKey;
            _plate.enabled = _accent.enabled = visible;
            _feed?.ReserveConnectionSpace(visible);
        }
    }
}
