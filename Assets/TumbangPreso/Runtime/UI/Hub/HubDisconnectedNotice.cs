using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// CONNECTION LOST on the boot kit (`boot_menus.py`): a dark panel taped across two corners, a
    /// warning mark above it in the yellow button's paint, and BACK TO HOME in lime.
    /// </summary>
    public sealed class HubDisconnectedNotice : HubScreen
    {
        public string Message;
        private HubButton _ok;
        public override bool IsPopup => true;
        public override bool ShowsQueuePlate => false;
        public override Selectable FirstFocus => _ok;

        private static readonly Color Warn = HubBoard.Colour("#F6BC3A"), WarnInk = HubBoard.Colour("#A84E0C");

        private static RawImage Strip(Transform parent, string name, string art, float cx, float cy, float w, float h, float angle)
        {
            var strip = HubKit.Rect(parent, name).gameObject.AddComponent<RawImage>();
            strip.texture = Resources.Load<Texture2D>("UI/boot-kit/" + art);
            strip.raycastTarget = false;
            HubKnit.At(strip.rectTransform, cx - w / 2, cy - h / 2, w, h);
            strip.rectTransform.pivot = new Vector2(.5f, .5f);
            strip.rectTransform.anchoredPosition = new Vector2(cx - 960, 540 - cy);
            strip.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            return strip;
        }

        public override void Build()
        {
            var shade = Root.GetComponent<Image>();
            if (shade != null) shade.color = new Color(20 / 255f, 4 / 255f, 8 / 255f, .745f);
            const float x = 490, y = 270, w = 940, h = 560;
            string heading = MatchAbandon.Cause == Core.SessionEndCause.HostLost ? "HOST CONNECTION LOST" : "CONNECTION LOST";
            var panel = HubKnit.DarkModal(Root, Hub, heading, null, w, h, out var plate, false);
            HubKnit.At(plate, x, y, w, h + HubKnit.SlabHeight);
            var title = panel.Find("Heading").GetComponent<Text>();
            title.color = Warn;
            HubKnit.In(title.rectTransform, 40, 100, w - 80, 100);
            HubKit.Fit(title, w - 80);

            // Warning tape across two corners and the mark above the panel, drawn over it.
            Strip(Root, "TapeA", "warning-tape", x + 30, y + 26, 300, 86, 36);
            Strip(Root, "TapeB", "warning-tape", x + w - 30, y + h - 14, 300, 86, 36);
            var mark = Strip(Root, "WarningMark", "warning-mark", 960, y - 120 + 109, 210, 218, 0);
            var bang = HubKnit.Display(mark.transform, "Bang", "!", 105, WarnInk, TextAnchor.MiddleCenter);
            HubKnit.In(bang.rectTransform, 0, 60, 210, 120);

            string lead = MatchAbandon.RoundNumber > 0 && !MatchAbandon.MatchWasCompleted
                ? "The match has stopped." : "The room connection ended.";
            var summary = HubKnit.Words(panel, "ConnectionLostSummary", lead, 40, HubKnit.KitCream, TextAnchor.MiddleCenter, true);
            HubKnit.In(summary.rectTransform, 40, 195, w - 80, 56);
            string detail = string.IsNullOrWhiteSpace(Message) || Message == MatchAbandon.PlayerLine
                ? "Return Home to join or host a room." : Message;
            var text = HubKnit.Words(panel, "DisconnectReason", detail, 34, HubKnit.KitTan, TextAnchor.MiddleCenter, true);
            text.supportRichText = false;
            HubKnit.In(text.rectTransform, 40, 258, w - 80, 96);
            _ok = HubKnit.Button(panel, "AcknowledgeDisconnect", "BACK TO HOME", Knit.Go, Close, 54);
            HubKnit.In((RectTransform)_ok.transform, 190, 370, 560, 128);
        }
    }
}
