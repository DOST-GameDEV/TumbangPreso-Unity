using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    public sealed class HubDisconnectedNotice : HubScreen
    {
        public string Message;
        private HubButton _ok;
        public override bool IsPopup => true;
        public override bool ShowsQueuePlate => false;
        public override Selectable FirstFocus => _ok;
        public override void Build()
        {
            var shade = Root.GetComponent<Image>();
            if (shade != null) shade.color = new Color(HubStyle.Night.r, HubStyle.Night.g, HubStyle.Night.b, .84f);
            var panel = HubKit.Place(HubKit.Rect(Root, "ConnectionLostPanel"), HubKit.Centre, Vector2.zero, new Vector2(720, 360));
            var plate = HubKit.Shape(panel, "Plate", HubStyle.Night, false, 102501, 3, 18);
            HubKit.Stretch(plate.rectTransform); plate.ShadowOffset = new Vector2(0, -6);
            string heading = MatchAbandon.Cause == Core.SessionEndCause.HostLost ? "HOST CONNECTION LOST" : "CONNECTION LOST";
            var title = HubKit.Text(panel, "ConnectionLostTitle", heading, 44, true, HubStyle.Honey, TextAnchor.MiddleLeft);
            HubKit.Place(title.rectTransform, HubKit.TopLeft, new Vector2(36, -28), new Vector2(648, 60));
            string lead = MatchAbandon.RoundNumber > 0 && !MatchAbandon.MatchWasCompleted
                ? "The match has stopped." : "The room connection ended.";
            var summary = HubKit.Text(panel, "ConnectionLostSummary", lead, 34, true, HubStyle.Paper, TextAnchor.MiddleLeft);
            HubKit.Place(summary.rectTransform, HubKit.TopLeft, new Vector2(36, -104), new Vector2(648, 48));
            string detail = string.IsNullOrWhiteSpace(Message) || Message == MatchAbandon.PlayerLine
                ? "Return Home to join or host a room." : Message;
            var text = HubKit.Text(panel, "DisconnectReason", detail,
                28, false, HubStyle.HoneySoft, TextAnchor.UpperLeft);
            text.supportRichText = false;
            HubKit.Place(text.rectTransform, HubKit.TopLeft, new Vector2(36, -162), new Vector2(648, 72));
            _ok = HubKit.Button(panel, "AcknowledgeDisconnect", "BACK TO HOME", HubStyle.Golden, Close, HubStyle.Label, 102502);
            HubKit.Place((RectTransform)_ok.transform, HubKit.BottomLeft, new Vector2(36, 28), new Vector2(648, 76));
        }
    }
}
