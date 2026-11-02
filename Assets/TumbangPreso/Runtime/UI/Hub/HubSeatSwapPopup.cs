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
            var panel = HubKnit.Modal(Root, "SWITCH SEATS?", 860, 440);
            var message = HubKnit.Words(panel, "Request", Offer.RequesterName + " wants to switch seats with you.\n"
                + "You move from P" + (Offer.ToSeat + 1) + " to P" + (Offer.FromSeat + 1) + ".",
                30, HubKnit.KitInk, TextAnchor.MiddleCenter, false);
            message.supportRichText = false;
            HubKnit.In(message.rectTransform, 50, 146, 760, 100);
            var yes = HubKnit.Button(panel, "AcceptSeatSwap", "YES", Knit.Go, () => Answer(true), 50, HubGlyph.Mark.None, primary: true);
            HubKnit.In((RectTransform)yes.transform, 70, 286, 340, 112);
            _no = HubKnit.Button(panel, "DeclineSeatSwap", "NO", Knit.Danger, () => Answer(false), 50);
            HubKnit.In((RectTransform)_no.transform, 450, 286, 340, 112);
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
