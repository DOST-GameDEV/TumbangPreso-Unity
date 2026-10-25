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
            var panel = HubCards.Panel(Root, this, "DISCONNECTED", "", new Vector2(800, 430));
            var text = HubKit.Text(panel, "DisconnectReason", string.IsNullOrWhiteSpace(Message) ? "The connection to the room ended." : Message,
                HubStyle.Body, false, HubStyle.Honey, TextAnchor.MiddleCenter);
            text.supportRichText = false;
            HubKit.Place(text.rectTransform, HubKit.Centre, new Vector2(0, 16), new Vector2(680, 160));
            _ok = HubKit.Button(panel, "AcknowledgeDisconnect", "OK", HubStyle.Chartreuse, Close, HubStyle.Label, 102501);
            HubKit.Place((RectTransform)_ok.transform, HubKit.BottomLeft, new Vector2(220, 36), new Vector2(360, 82));
        }
    }
}
