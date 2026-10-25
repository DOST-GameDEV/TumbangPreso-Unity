using TumbangPreso.Net;
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
        private Text _text, _hint;
        private HudCard _plate;
        private long _match;
        private int _seen;
        private float _left, _hintAt;
        public float VisibleSecondsLeft => _left;
        public bool IsShowing => _text != null && _text.enabled && _canvas != null && _canvas.isActiveAndEnabled;
        public static PeerConnectionNotice Create(RectTransform parent)
        {
            var rect = OwnerUiLayout.Rect(parent, "PeerConnectionNotice");
            OwnerUiLayout.Place(rect, 30, 150, 740, 132);
            var notice = rect.gameObject.AddComponent<PeerConnectionNotice>();
            notice._canvas = parent.GetComponentInParent<Canvas>();
            notice._plate = rect.gameObject.AddComponent<HudCard>();
            notice._plate.Glass(HudDraw.Glass).raycastTarget = false;
            notice._text = Words(rect, "PeerDepartureText", 8, 78);
            notice._hint = Words(rect, "PeerDeparturePauseHint", 89, 36);
            notice._hint.color = new Color(1, .8f, .25f);
            notice.SetVisible(false); return notice;
        }
        private static Text Words(RectTransform root, string name, float y, float height)
        {
            var text = OwnerUiLayout.Text(root, name, "", 28); text.color = HudDraw.Ivory;
            text.supportRichText = false; text.raycastTarget = false;
            OwnerUiLayout.Place(text.rectTransform, 16, y, 708, height);
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = UiTheme.InGameOutline; outline.effectDistance = new Vector2(1, -1);
            return text;
        }
        public void Present(string text)
        {
            _text.text = text; _left = 12; _hintAt = 0;
            transform.SetAsLastSibling();
        }
        private void LateUpdate()
        {
            if (_text == null) return;
            if (!GameLaunch.Spectator || !NetAuthority.IsNetworked) { _left = 0; SetVisible(false); return; }
            var rpc = MatchRpc.Instance;
            if (rpc != null)
            {
                if (_match != rpc.PresentationMatchId) { _match = rpc.PresentationMatchId; _seen = 0; _left = 0; }
                if (rpc.PeerDepartureNotices > _seen)
                { _seen = rpc.PeerDepartureNotices; Present(rpc.LastPeerDepartureText); }
            }
            if (PresentationClock.RequestedScale <= 0) _left = 0;
            bool visible = _left > 0 && _canvas != null && _canvas.isActiveAndEnabled
                && _text.canvasRenderer.GetInheritedAlpha() > .95f && !ScreenTakeover.AnyOpen;
            SetVisible(visible);
            if (!visible) return;
            _left = Mathf.Max(0, _left - Time.unscaledDeltaTime);
            if (Time.unscaledTime >= _hintAt)
            {
                _hintAt = Time.unscaledTime + .5f;
                _hint.text = Hud.OnTouch ? "PAUSE MATCH FROM CONTROLS" : Hud.KeyLabelFor("SpectatorPause") + "  PAUSE MATCH";
            }
        }
        private void SetVisible(bool visible)
        { _text.enabled = visible; _hint.enabled = visible; _plate.enabled = visible; }
    }
}
