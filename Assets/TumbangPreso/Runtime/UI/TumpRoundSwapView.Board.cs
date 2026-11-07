using System;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// NEXT ROUND and HALFTIME as the boot boards draw them (`tools/ui_artboards/boot_record.py` `round_break`,
    /// owner 2026-11-08: follow the board). The court dimmed behind; the title top left; the next taya on an
    /// orange sunburst card with their name on a cream plate; the countdown as a stopwatch on a berry sunburst
    /// with the round on a cream plate; and the standings as a podium of kit cards, tallest for the leader.
    /// Pictures come from `tools/ui_artboards/export_break_art.py`. The dark card it replaces is `BuildDarkCard`.
    /// </summary>
    public sealed partial class TumpRoundSwapView
    {
        private bool _board;
        private readonly RectTransform[] _chipRect = new RectTransform[4];
        private readonly RectTransform[] _boardPlate = new RectTransform[4];
        private readonly KitCutPicture[] _boardSwatch = new KitCutPicture[4];
        private RectTransform _ticketRect;
        private static readonly float[] PodiumHeights = { 330, 306, 282, 258 };

        public void Build(Transform owner, Action dismiss)
        {
            _board = true;
            Canvas = OwnerUiLayout.Canvas(owner, "OwnerRoundSwapCanvas", 260);
            var focus = Canvas.GetComponent<InputLayer.ScreenFocus>(); if (focus != null) focus.enabled = false;
            // Passive graphic over the court. It does not open a page or need a click.
            _popup = OwnerUiLayout.DesignArea(Canvas.transform, "CourtBreakPopup");
            _popupFade = _popup.gameObject.AddComponent<CanvasGroup>(); _popupFade.blocksRaycasts = false; _popupFade.interactable = false;

            // The court dimmed as the board dims it, past the design area on wide screens.
            var dim = OwnerUiLayout.Rect(_popup, "BreakDim").gameObject.AddComponent<Image>();
            dim.color = new Color32(40, 8, 14, 110); dim.raycastTarget = false;
            OwnerUiLayout.Fill(dim.rectTransform); dim.rectTransform.offsetMin = new Vector2(-1200, -600); dim.rectTransform.offsetMax = new Vector2(1200, 600);

            // The title, white with the berry edge, its caps centred at y 85.
            _round = HubKnit.Display(_popup, "RoundHeadline", "NEXT ROUND", 110, HubKnit.KitWhite, TextAnchor.MiddleLeft, 7);
            HubKnit.In(_round.rectTransform, 60, 25, 1100, 120);
            _headline = _round.rectTransform;
            _notice = HubKnit.Display(_popup, "ReplayNotice", "", 42, HubKnit.KitWhite, TextAnchor.MiddleCenter, 4);
            HubKnit.In(_notice.rectTransform, 1000, 46, 780, 60);

            // The hazard tape at the two top corners of the cards.
            Tape("TapeLeft", 62, 214, 40);
            Tape("TapeRight", 1846, 214, -40);

            BuildTayaCard();
            BuildClockCard();
            for (int i = 0; i < 4; i++) BuildPodium(i);
            _popupBuilt = true;
        }

        private void Tape(string name, float cx, float cy, float angle)
        {
            var tape = OwnerUiLayout.Rect(_popup, name).gameObject.AddComponent<RawImage>();
            tape.texture = Resources.Load<Texture2D>("UI/break/hazard-tape"); tape.raycastTarget = false;
            tape.rectTransform.anchorMin = tape.rectTransform.anchorMax = new Vector2(0, 1); tape.rectTransform.pivot = new Vector2(.5f, .5f);
            tape.rectTransform.anchoredPosition = new Vector2(cx, -cy); tape.rectTransform.sizeDelta = new Vector2(240, 30);
            tape.rectTransform.localRotation = Quaternion.Euler(0, 0, -angle);
        }

        /// <summary>A kit card at a board rectangle whose whole opening is a picture, cut at both chamfers, the rim over it.</summary>
        private RectTransform BurstCard(string name, string picture, float x, float y, float w, float h)
        {
            var card = HubKnit.Piece(_popup, name, Knit.Card, x, y, w, h);
            var face = HubKnit.FaceOf(card);
            HubKnit.Picture(face, "Burst", Resources.Load<Texture2D>("UI/break/" + picture), 0, 0, w, h, .5f, HubKnit.ShowCardCut).CutEnd = HubKnit.ShowCardCut;
            return face;
        }

        private void BuildTayaCard()
        {
            var face = BurstCard("NextTayaCard", "burst-taya", 50, 190, 880, 450);
            HubKnit.RimOver(face);
            // The next taya breaks out of the top of the card, so the portrait hangs from the popup, not the face.
            // Clipped at the card's bottom edge; the top stays open so the head rises over the rim.
            var hang = HubKnit.In(OwnerUiLayout.Rect(_popup, "NextTayaWindow"), 50, 0, 880, 640);
            hang.gameObject.AddComponent<RectMask2D>();
            _portrait = OwnerUiLayout.Rect(hang, "NextDefenderPortrait").gameObject.AddComponent<Image>();
            _portrait.preserveAspect = true; _portrait.raycastTarget = false;
            HubKnit.In(_portrait.rectTransform, -32, 130, 564, 564);
            var badge = OwnerUiLayout.Rect(_popup, "NextTayaBadge").gameObject.AddComponent<HudBadge>();
            HubKnit.In(badge.rectTransform, 425, 490, 84, 84); badge.Detail = UiTheme.Defense; badge.raycastTarget = false;
            badge.Show(HudBadge.Glyph.Can, HudDraw.Ivory, UiTheme.Defense); _tayaBadge = badge.rectTransform;
            var tag = HubKnit.Tag(_popup, "NextRole", "NEXT TAYA", Knit.Danger, 74);
            HubKnit.In(tag, 560, 226, 340, 74);
            var plate = HubKnit.Piece(_popup, "NextTayaTicket", Knit.Plain, 90, 566, 800, 104 - HubKnit.SlabHeight);
            _ticketRect = plate;
            _name = HubKnit.Display(HubKnit.FaceOf(plate), "NextDefenderName", "", 58, HubKnit.InkOf(Knit.Plain), TextAnchor.MiddleCenter);
            OwnerUiLayout.Fill(_name.rectTransform); _name.rectTransform.offsetMin = new Vector2(24, 8); _name.rectTransform.offsetMax = new Vector2(-24, 0);
            _name.horizontalOverflow = HorizontalWrapMode.Wrap; _name.resizeTextForBestFit = true; _name.resizeTextMinSize = 34; _name.resizeTextMaxSize = 58;
        }

        private void BuildClockCard()
        {
            var face = BurstCard("ReturnTimer", "burst-clock", 980, 190, 880, 380);
            // The stopwatch, its dial at (1420, 390): the berry track is in the picture, the lime drains over it.
            var watch = OwnerUiLayout.Rect(face, "Stopwatch").gameObject.AddComponent<RawImage>();
            watch.texture = Resources.Load<Texture2D>("UI/break/stopwatch"); watch.raycastTarget = false;
            HubKnit.In(watch.rectTransform, 290, 6, 300, 364);
            _returnClock = OwnerUiLayout.Rect(face, "ReturnClock").gameObject.AddComponent<HudRing>();
            HubKnit.In(_returnClock.rectTransform, 440 - 124, 200 - 124, 248, 248);
            _returnClock.Thickness = 20; _returnClock.raycastTarget = false;
            _returnClock.color = HubKnit.KitLime; _returnClock.Track = Color.clear;
            _buffer = HubKnit.Display(face, "WarmupTime", "", 130, HubKnit.KitCream, TextAnchor.MiddleCenter);
            HubKnit.In(_buffer.rectTransform, 290, 130, 300, 140);
            HubKnit.RimOver(face);
            var plate = HubKnit.Piece(_popup, "RoundPlate", Knit.Plain, 1020, 566, 800, 104 - HubKnit.SlabHeight);
            _nextRound = HubKnit.Display(HubKnit.FaceOf(plate), "NextRound", "ROUND 2", 70, HubKnit.InkOf(Knit.Plain), TextAnchor.MiddleCenter);
            OwnerUiLayout.Fill(_nextRound.rectTransform); _nextRound.rectTransform.offsetMin = new Vector2(0, 8);
        }

        private void BuildPodium(int i)
        {
            float w = 400, h = PodiumHeights[i], x = 115 + i * 430, y = 1030 - h;
            var card = HubKnit.Piece(_popup, "ScoreTicket" + i, Knit.Card, x, y, w, h);
            _boardPlate[i] = card; _chipRect[i] = card;
            var face = HubKnit.FaceOf(card);
            var swatch = _boardSwatch[i] = OwnerUiLayout.Rect(face, "StandingSwatch" + i).gameObject.AddComponent<KitCutPicture>();
            swatch.Cut = swatch.CutEnd = 18; swatch.raycastTarget = false;
            HubKnit.In(swatch.rectTransform, 26, 26, 126, 126);
            _portraits[i] = OwnerUiLayout.Rect(face, "StandingPortrait" + i).gameObject.AddComponent<Image>();
            _portraits[i].preserveAspect = true; _portraits[i].raycastTarget = false;
            HubKnit.In(_portraits[i].rectTransform, 35, 37, 108, 108);
            _chipCrowns[i] = OwnerUiLayout.Rect(card, "StandingCrown" + i).gameObject.AddComponent<HudBadge>();
            HubKnit.In(_chipCrowns[i].rectTransform, 54, -36, 72, 56); _chipCrowns[i].raycastTarget = false;
            _chipRoles[i] = OwnerUiLayout.Rect(face, "StandingRole" + i).gameObject.AddComponent<HudBadge>();
            HubKnit.In(_chipRoles[i].rectTransform, 118, 118, 44, 44); _chipRoles[i].Detail = UiTheme.Defense; _chipRoles[i].raycastTarget = false;
            _names[i] = HubKnit.Display(card, "StandingName" + i, "", 46, HubKnit.KitTan);
            HubKnit.In(_names[i].rectTransform, 176, 26, 150, 60);
            _gains[i] = HubKnit.Display(card, "StandingGain" + i, "", 40, HubKnit.KitCream);
            HubKnit.In(_gains[i].rectTransform, 176, 86, 200, 52);
            _ranks[i] = HubKnit.Display(card, "StandingRank" + i, (i + 1).ToString(), 84, HubKnit.KitTan, TextAnchor.MiddleRight);
            HubKnit.In(_ranks[i].rectTransform, w - 130, 12, 100, 100);
            _scores[i] = HubKnit.Display(card, "StandingScore" + i, "", 92, HubKnit.KitCream, TextAnchor.MiddleCenter);
            HubKnit.In(_scores[i].rectTransform, 20, h - 56 - 55, w - 40, 110);
            // The words hang from the card itself (tests read `ScoreTicketN/StandingGainN`), drawn over its face.
            _chipHome[i] = card.anchoredPosition; _chipFrom[i] = _chipHome[i].x;
        }

        /// <summary>The board's colours for this break: seat swatches, your card lime, crown and taya mark.</summary>
        private void PaintBoardChips(int[] order, int nextTaya, bool uniqueLeader)
        {
            int mine = LocalSeat();
            for (int i = 0; i < 4; i++)
            {
                int seat = order[i]; bool taya = seat == nextTaya, me = seat == mine;
                _boardSwatch[i].color = PlayerIdentity.Colour(seat);
                HubKnit.Recolour(_boardPlate[i], me ? Knit.CardOn : Knit.Card);
                _names[i].color = _ranks[i].color = me ? HubKnit.KitLime : HubKnit.KitTan;
                _scores[i].color = me ? HubKnit.KitLime : HubKnit.KitCream;
                _chipCrowns[i].Show(i == 0 && uniqueLeader ? HudBadge.Glyph.Crown : HudBadge.Glyph.None, CourtPresentationPalette.Gold, Color.clear);
                _chipRoles[i].Show(taya ? HudBadge.Glyph.Can : HudBadge.Glyph.None, CourtPresentationPalette.Paper, UiTheme.Defense);
            }
        }
    }
}
