using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    public sealed class HubSeatSwapPopup : HubScreen
    {
        public Net.LobbySeatSwapOffer Offer;
        public override bool IsPopup => true;
        private HubButton _no;
        private bool _answered;
        public override Selectable FirstFocus => _no;
        public override void Build()
        {
            var panel = HubCards.Panel(Root, this, "SWITCH SEATS?", "", new Vector2(760, 400));
            var message = HubKit.Text(panel, "Request", Offer.RequesterName + " wants to switch seats with you.\n"
                + "You move from P" + (Offer.ToSeat + 1) + " to P" + (Offer.FromSeat + 1) + ".",
                HubStyle.Body, false, HubStyle.Honey, TextAnchor.MiddleCenter);
            message.supportRichText = false;
            HubKit.Place(message.rectTransform, HubKit.Centre, new Vector2(0, 0), new Vector2(650, 120));
            var yes = HubKit.Button(panel, "AcceptSeatSwap", "YES", HubStyle.Chartreuse, () => Answer(true), HubStyle.Label, 991);
            HubKit.Place((RectTransform)yes.transform, HubKit.BottomLeft, new Vector2(44, 36), new Vector2(320, 88));
            _no = HubKit.Button(panel, "DeclineSeatSwap", "NO", HubStyle.Honey, () => Answer(false), HubStyle.Label, 992);
            HubKit.Place((RectTransform)_no.transform, HubKit.BottomRight, new Vector2(-44, 36), new Vector2(320, 88));
        }
        private void Answer(bool accept)
        {
            if (_answered) return;
            _answered = true;
            if (Hub.Host.SeatSwapOffer?.Id == Offer.Id) Hub.Host.RespondToSeatSwap(Offer.Id, accept);
            Close();
        }
        public override bool Back() { Answer(false); return true; }
        public override void Tick()
        {
            if (!Hub.Host.InRoom || Hub.Host.MatchInProgress || Hub.Host.SeatSwapOffer?.Id != Offer.Id) Close();
        }
    }
}
