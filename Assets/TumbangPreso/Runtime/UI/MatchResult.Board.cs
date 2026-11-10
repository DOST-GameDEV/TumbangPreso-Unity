using UnityEngine;
using UnityEngine.UI;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using TumbangPreso.UI.Hub;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The results as the boot boards draw them (`tools/ui_artboards/boot_record.py` `result_frame`, `standings`,
    /// `your_match`, `players`): the court dimmed, the winner line top left with the mode under it, the kit tabs,
    /// one dark woven panel, and REMATCH, NEXT MAP and MAIN MENU as the lime, yellow and red kit buttons.
    ///
    /// STANDINGS is a ranked list of kit cards (rank, the player's face as the profile draws it, seat and name,
    /// points; the leader crowned, you in lime) beside the winner on a sunburst card. YOUR MATCH opens on the match
    /// moment as a reveal card; the summary, XP and rank this page always carried follow it below, one scroll down.
    /// PLAYERS lists everyone as kit cards, with ADD FRIEND and REPORT on the people who can be added.
    /// Every name, route and focus path the earlier sheet had is kept (`BuildNativeResult` is the sheet it replaces).
    /// </summary>
    public sealed partial class MatchResult
    {
        private bool _board;
        private readonly RectTransform[] _boardRows = new RectTransform[4];
        private readonly Graphic[] _boardSeats = new Graphic[4];
        private Text _winnerName, _momentWord, _momentName, _finishCaption;
        private Image _momentPortrait;
        private GameObject _momentCard;
        private static MatchResult _shown;

        /// <summary>The board's single-spaced dot ("RAGO · P1"); the shared labels carry a wide one.</summary>
        private static string Tight(string words) => string.IsNullOrEmpty(words) ? words : words.Replace("  ·  ", " · ");

        /// <summary>True while a results board is up; the live HUD stays hidden under it, as it does under the break.</summary>
        public static bool BoardShowing => _shown != null && _shown.IsVisible;

        private void BuildBoardResult()
        {
            _board = _nativeResult = true; _shown = this;
            _canvas = OwnerUiLayout.Canvas(transform, "OwnerResultCanvas", 400);
            CleanFeedLayer.Attach(_canvas.gameObject);
            // Its own root canvas at 400, not nested in the HUD's: nested it took the HUD's order (the TAYA reveal, 165,
            // drew over it), and the HUD's canvas is hidden while this board is up.
            var ground = OwnerUiLayout.Rect(_canvas.transform, "FinishSheetGround").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(ground.rectTransform); ground.color = new Color32(40, 8, 14, 150);
            ground.raycastTarget = true; // the full-screen click blocker
            var root = OwnerUiLayout.DesignArea(_canvas.transform, "ResultsComposition");

            _message = HubKnit.Display(root, "ResultHeadline", "", 90, HubKnit.KitWhite, TextAnchor.MiddleLeft, 7);
            HubKnit.In(_message.rectTransform, 60, 30, 1400, 110);
            _broadcastLine = HubKnit.Words(root, "ResultMode", "", 34, HubKnit.KitWhite, TextAnchor.MiddleLeft, true, 3);
            HubKnit.In(_broadcastLine.rectTransform, 62, 128, 1200, 46);

            // No tabs (owner, 2026-11-09: "just drop the tabs"): the results are the standings page alone. The other two
            // pages are still built, hidden, so the XP and record fields the result code fills keep their homes.
            // The panel fills the room the tabs left, from under the title down to the buttons (owner, 2026-11-09: "fill this space").
            HubKnit.Piece(root, "ResultPanel", Knit.DarkPanel, 60, 200, 1800, 690);

            BuildBoardStandings(root);
            BuildBoardMatch(root);
            BuildBoardPeople(root);

            var rematch = HubKnit.Pressable(root, "ResultRematch", "REMATCH", Knit.Go, 60, 924, 480, 112, 54, OnRematchPressed);
            _rematch = rematch;
            _mapVote = HubKnit.Pressable(root, "ResultNextMap", "NEXT MAP", Knit.Yellow, 570, 924, 820, 112, 44, OnMapVotePressed);
            _menu = HubKnit.Pressable(root, "ResultMainMenu", "MAIN MENU", Knit.Danger, 1420, 924, 440, 112, 44, OnMenuPressed);
            _rematchTally = HubKnit.Words(root, "RematchTally", "", 26, HubKnit.KitWhite, TextAnchor.MiddleCenter, true, 3);
            HubKnit.In(_rematchTally.rectTransform, 60, 1036, 480, 40);
            _mapVoteTally = HubKnit.Words(root, "MapVoteTally", "", 26, HubKnit.KitWhite, TextAnchor.MiddleCenter, true, 3);
            HubKnit.In(_mapVoteTally.rectTransform, 570, 1036, 820, 40);
            NativePage(0); NativeProgression(null, null); ScreenTakeover.Register(this, () => NativeVisible);
        }

        /// <summary>A standings row's height: four rows and three 30-unit gaps fill the panel inside its 32-unit margin.</summary>
        private const float RowH = 134;

        private void BuildBoardStandings(RectTransform root)
        {
            var page = OwnerUiLayout.Rect(root, "Standings"); OwnerUiLayout.Fill(page); _nativePages[0] = page.gameObject;
            for (int i = 0; i < 4; i++)
            {
                var card = HubKnit.Piece(page, "FinisherTicket" + i, Knit.Card, 110, 232 + i * 164, 1080, RowH);
                _boardRows[i] = card; _finishRowOrigins[i] = card.anchoredPosition;
                _finishFades[i] = card.gameObject.AddComponent<CanvasGroup>(); _finishFades[i].blocksRaycasts = false;
                var face = HubKnit.FaceOf(card);
                var place = HubKnit.Display(card, "FinisherPlace" + i, "", 62, HubKnit.KitTan, TextAnchor.MiddleCenter);
                HubKnit.In(place.rectTransform, 10, 0, 80, RowH);
                _nativePortraits[i] = HubKnit.SeatChip(face, "FinisherSwatch" + i, 102, (RowH - 96) * .5f, 96, out _boardSeats[i]);
                _nativePortraits[i].name = "FinisherPortrait" + i;
                _finishCrowns[i] = OwnerUiLayout.Rect(card, "FinisherCrown" + i).gameObject.AddComponent<HudBadge>();
                HubKnit.In(_finishCrowns[i].rectTransform, 114, -26, 72, 56); _finishCrowns[i].raycastTarget = false;
                var name = HubKnit.Display(card, "FinisherName" + i, "", 46, HubKnit.KitCream);
                HubKnit.In(name.rectTransform, 234, 0, 302, RowH);
                BuildAccolades(card, i, 548, (RowH - 42) * .5f);
                var score = HubKnit.Display(card, "FinisherScore" + i, "", 52, HubKnit.KitCream, TextAnchor.MiddleRight);
                HubKnit.In(score.rectTransform, 800, 0, 240, RowH);
                var title = HubKnit.Words(card, "FinisherTitle" + i, "", 26, HubKnit.KitCream); title.enabled = false;
                _rows.Add(new[] { place, name, score, title });
            }

            // The winner on a sunburst, under MATCH WINNER, with their name on a kit card below.
            var winner = HubKnit.Piece(page, "WinnerCard", Knit.Card, 1230, 232, 580, 626);
            var face2 = HubKnit.FaceOf(winner);
            HubKnit.Picture(face2, "Burst", Resources.Load<Texture2D>("UI/break/burst-winner"), 0, 0, 580, 626, .5f, HubKnit.ShowCardCut).CutEnd = HubKnit.ShowCardCut;
            _finishFigure = OwnerUiLayout.Rect(face2, "WinnerFigure"); HubKnit.In(_finishFigure, 20, 104, 540, 400);
            _finishPreview = _finishFigure.gameObject.AddComponent<ModelPreview>(); _finishPreview.Attach(_finishFigure);
            _finishCelebration = _finishFigure.gameObject.AddComponent<FinishCelebration>();
            _finishDraw = OwnerUiLayout.Rect(face2, "DrawCan").gameObject.AddComponent<TumpSymbol>();
            HubKnit.In(_finishDraw.rectTransform, 190, 170, 200, 240); _finishDraw.Kind = TumpSymbol.Icon.Can;
            _finishDraw.color = CourtPresentationPalette.Red; _finishDraw.raycastTarget = false; _finishDraw.gameObject.SetActive(false);
            HubKnit.RimOver(face2);
            var banner = HubKnit.Tag(page, "WinnerBanner", "MATCH WINNER", Knit.Go, 66);
            HubKnit.In(banner, 1330, 264, 380, 66);
            _finishCaption = HubKnit.FaceOf(banner) != null ? HubKnit.FaceOf(banner).GetComponentInChildren<Text>() : banner.GetComponentInChildren<Text>();
            _finishCaption.name = "WinnerCaption";
            var tag = HubKnit.Piece(page, "WinnerTag", Knit.Card, 1250, 734, 540, 92);
            _winnerName = HubKnit.Display(HubKnit.FaceOf(tag), "WinnerName", "", 60, HubKnit.KitCream, TextAnchor.MiddleCenter);
            OwnerUiLayout.Fill(_winnerName.rectTransform); _winnerName.rectTransform.offsetMin = new Vector2(16, 0); _winnerName.rectTransform.offsetMax = new Vector2(-16, 0);
            _winnerName.horizontalOverflow = HorizontalWrapMode.Wrap; _winnerName.resizeTextForBestFit = true; _winnerName.resizeTextMinSize = 32; _winnerName.resizeTextMaxSize = 60;
        }

        private void BuildBoardMatch(RectTransform root)
        {
            var details = OwnerUiLayout.Rect(root, "MatchDetailsPage"); OwnerUiLayout.Fill(details); _nativePages[1] = details.gameObject;
            var content = OwnerScrollColumn.Build(details, "MatchDetails", new Rect(110, 350, 1710, 512), out var scroll);
            if (scroll.verticalScrollbar != null) scroll.verticalScrollbar.targetGraphic.color = HubKnit.KitTan;
            var layout = content.GetComponent<VerticalLayoutGroup>(); layout.spacing = 14; layout.padding = new RectOffset(10, 24, 18, 24);

            // The moment, as the board opens it: its line, a rule, and the reveal card.
            _highlightLine = HubKnit.Display(content, "MatchHighlight", "", 44, HubKnit.KitCream);
            _highlightLine.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
            var rule = HubKit.Rect(content, "MomentRule").gameObject.AddComponent<Image>(); rule.color = HubKnit.KitTan; rule.raycastTarget = false;
            rule.gameObject.AddComponent<LayoutElement>().preferredHeight = 3;
            var holder = HubKit.Rect(content, "MomentHolder"); holder.gameObject.AddComponent<LayoutElement>().preferredHeight = 372;
            _momentCard = holder.gameObject;
            var card = HubKnit.Piece(holder, "MomentCard", Knit.Card, 180, 22, 1320, 340);
            var face = HubKnit.FaceOf(card);
            HubKnit.Picture(face, "Burst", Resources.Load<Texture2D>("UI/break/burst-moment"), 0, 0, 1320, 340, .5f, HubKnit.ShowCardCut).CutEnd = HubKnit.ShowCardCut;
            HubKnit.RimOver(face);
            // The player stands proud of the card's top edge, clipped at its bottom.
            var window = HubKnit.In(OwnerUiLayout.Rect(holder, "MomentWindow"), 180, -40, 1320, 402);
            window.gameObject.AddComponent<RectMask2D>();
            _momentPortrait = OwnerUiLayout.Rect(window, "MomentPortrait").gameObject.AddComponent<Image>();
            _momentPortrait.preserveAspect = true; _momentPortrait.raycastTarget = false;
            HubKnit.In(_momentPortrait.rectTransform, 150, 0, 420, 420);
            _momentWord = HubKnit.Display(holder, "MomentWord", "", 150, HubKnit.KitWhite, TextAnchor.MiddleCenter, 9);
            HubKnit.In(_momentWord.rectTransform, 780, 34, 660, 150);
            var plate = HubKnit.Piece(holder, "MomentPlate", Knit.CardOn, 780, 212, 660, 104);
            _momentName = HubKnit.Display(HubKnit.FaceOf(plate), "MomentName", "", 50, HubKnit.KitLime, TextAnchor.MiddleCenter);
            OwnerUiLayout.Fill(_momentName.rectTransform);

            // What this page always said, under the moment: the empty state, the summary, XP and rank.
            _ownerEmptyDetails = HubKnit.Words(content, "EmptyMatchDetails", "No match summary was recorded for this game.", 32, HubKnit.KitCream);
            _ownerEmptyDetails.gameObject.AddComponent<TumpParagraph>();
            _yourMatchLine = HubKnit.Words(content, "YourMatchSummary", "", 32, HubKnit.KitCream, TextAnchor.UpperLeft, false);
            _yourMatchLine.gameObject.AddComponent<TumpParagraph>();
            _xpHeadline = HubKnit.Display(content, "EarnedXp", "", 44, HubKnit.KitWhite);
            _xpHeadline.gameObject.AddComponent<LayoutElement>().preferredHeight = 64;
            var track = HubKit.Rect(content, "XpTrack").gameObject.AddComponent<Image>();
            track.color = new Color32(0x4A, 0x0C, 0x1A, 255); track.raycastTarget = false;
            track.gameObject.AddComponent<LayoutElement>().preferredHeight = 22; _xpBarTrack = track.rectTransform;
            _xpBarFill = HubKit.Rect(track.transform, "XpFill").gameObject.AddComponent<Image>();
            _xpBarFill.color = HubKnit.KitLime; _xpBarFill.raycastTarget = false; OwnerUiLayout.Fill(_xpBarFill.rectTransform);
            _nativeRank = HubKit.Rect(content, "RankBadge").gameObject.AddComponent<TumpRankBadge>(); _nativeRank.raycastTarget = false;
            var badge = _nativeRank.gameObject.AddComponent<LayoutElement>(); badge.preferredHeight = 138; badge.preferredWidth = 138;
            _xpDetail = HubKnit.Words(content, "RewardDetails", "", 30, HubKnit.KitCream, TextAnchor.UpperLeft, false);
            _xpDetail.gameObject.AddComponent<TumpParagraph>();
        }

        private void BuildBoardPeople(RectTransform root)
        {
            var people = OwnerUiLayout.Rect(root, "RecentPeoplePage"); OwnerUiLayout.Fill(people); _nativePages[2] = people.gameObject;
            _nativePeople = OwnerScrollColumn.Build(people, "RecentPlayers", new Rect(110, 350, 1710, 512), out var scroll);
            if (scroll.verticalScrollbar != null) scroll.verticalScrollbar.targetGraphic.color = HubKnit.KitTan;
            var layout = _nativePeople.GetComponent<VerticalLayoutGroup>(); layout.spacing = 2; layout.padding = new RectOffset(0, 40, 12, 16);   // the board's 122 row pitch
        }

        /// <summary>The tabs, the open page, and PLAYERS rebuilt when it opens.</summary>
        private void BoardPage(int page)
        {
            RefreshOwnerResultEmptyState();
            // Only the standings show; with no tabs there is no other page to open.
            for (int i = 0; i < 3; i++) if (_nativePages[i] != null) _nativePages[i].SetActive(i == 0);
            _canvas.GetComponent<ScreenFocus>().Rebuild();
        }

        /// <summary>The moment card: who, what, in the board's reveal; hidden with no moment to show.</summary>
        private void PaintBoardMoment(HighlightMarker? best)
        {
            if (_momentCard == null) return;
            _momentCard.SetActive(best.HasValue);
            if (!best.HasValue) return;
            var actor = GameServices.Round?.PlayerAt(best.Value.Actor);
            _momentPortrait.sprite = PortraitFor(actor); _momentPortrait.enabled = _momentPortrait.sprite != null;
            _momentWord.text = HighlightRules.Describe(best.Value).ToUpperInvariant();
            HubKit.Fit(_momentWord, 660);
            _momentName.text = Tight(NameFor(best.Value.Actor));
            HubKit.Fit(_momentName, 620);
        }

        private static Sprite PortraitFor(CharacterMotor actor)
        {
            if (actor == null) return null;
            var people = Roster.GetPeople(actor.Mode);
            return actor.CharacterIndex >= 0 && actor.CharacterIndex < people.Count ? OwnerPortraitArt.Get("UI/portraits/" + people[actor.CharacterIndex].Id) : null;
        }

        /// <summary>The rendered close-up the profile uses, or the full portrait for anyone without one.</summary>
        private static Sprite FaceFor(CharacterMotor actor)
        {
            if (actor == null) return null;
            var people = Roster.GetPeople(actor.Mode);
            if (actor.CharacterIndex < 0 || actor.CharacterIndex >= people.Count) return null;
            var cut = Resources.Load<Sprite>("UI/hero-cuts/" + people[actor.CharacterIndex].Id);
            return cut != null ? cut : PortraitFor(actor);
        }

        /// <summary>The board's colours on the standings: seat fills, the leader's crown, your row in lime.</summary>
        private void PaintBoardStandings(int[] order, int winner, bool hasWinner)
        {
            int mine = LocalResultSeat();
            for (int i = 0; i < 4; i++)
            {
                bool me = order[i] == mine, best = hasWinner && order[i] == winner;
                HubKnit.Recolour(_boardRows[i], me ? Knit.CardOn : Knit.Card);
                if (_boardSeats[i] != null) _boardSeats[i].color = PlayerIdentity.Colour(order[i]);
                _nativePortraits[i].sprite = FaceFor(GameServices.Round?.PlayerAt(order[i])); _nativePortraits[i].enabled = _nativePortraits[i].sprite != null;
                _finishCrowns[i].Show(best ? HudBadge.Glyph.Crown : HudBadge.Glyph.None, CourtPresentationPalette.Gold, Color.clear);
                _rows[i][0].color = me ? HubKnit.KitLime : HubKnit.KitTan;
                _rows[i][1].color = _rows[i][2].color = me ? HubKnit.KitLime : HubKnit.KitCream;
                var name = _rows[i][1];
                name.text = Tight(SeatLabel.WithIdentity(order[i]));
                name.fontSize = HubStyle.Size(46);
                HubKit.Fit(name, name.rectTransform.rect.width - 8);
            }
            if (_winnerName != null) _winnerName.text = hasWinner ? Tight(NameFor(winner)) : "EVEN AT THE TOP";
            if (_finishCaption != null) _finishCaption.text = hasWinner ? "MATCH WINNER" : "EVEN AT THE TOP";
        }

        private void BoardRecentPlayers()
        {
            if (_nativePeople == null) return;
            foreach (Transform child in _nativePeople) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            var social = GameServices.Social; var record = GameServices.Stats?.Last;
            var offers = SocialRules.RecentPlayers(record, social?.List, Net.CareerStore.LocalPlayerId);
            int mine = LocalResultSeat();
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var actor = GameServices.Round?.PlayerAt(slot);
                var identity = OwnerUiLayout.Rect(_nativePeople, "RecentPlayerIdentity" + slot);
                identity.gameObject.AddComponent<LayoutElement>().preferredHeight = 106 + HubKnit.SlabHeight;
                var card = HubKnit.Plate(identity, "RecentPlayerCard", slot == mine ? Knit.CardOn : Knit.Card);
                HubKnit.In(card, 0, 0, 1560, 106 + HubKnit.SlabHeight);
                var face = HubKnit.FaceOf(card);
                var portrait = HubKnit.SeatChip(face, "RecentPlayerSwatch", 28, 14, 78, out var seat);
                portrait.name = "RecentPlayerPortrait"; seat.color = PlayerIdentity.Colour(slot);
                portrait.sprite = FaceFor(actor); portrait.enabled = portrait.sprite != null;
                var name = HubKnit.Display(card, "RecentPlayerName", Tight(NameFor(slot)), 46, slot == mine ? HubKnit.KitLime : HubKnit.KitCream);
                HubKnit.In(name.rectTransform, 140, 0, 760, 106); HubKit.Fit(name, 760);
                string title = TitleFor(slot);
                if (!string.IsNullOrEmpty(title))
                {
                    var build = HubKnit.Words(_nativePeople, "PublicBuildAndTitle", title, 28, HubKnit.KitCream);
                    build.gameObject.AddComponent<TumpParagraph>();
                }
                FriendRef person = null;
                foreach (var candidate in offers)
                {
                    var line = MatchRecordRules.LineFor(record, candidate.PlayerId); if (line != null && line.Slot == slot) { person = candidate; break; }
                }
                PlayerMatchStats opponent = null;
                if (record?.Players != null) foreach (var line in record.Players)
                    if (line != null && line.Slot == slot && !line.IsBot && SocialRules.IsAddressable(line.PlayerId)
                        && line.PlayerId != Net.CareerStore.LocalPlayerId) { opponent = line; break; }
                if (opponent == null) continue;
                string id = opponent.PlayerId, handle = opponent.Handle;
                Button add = null, report = null;
                var reportStatus = RecentActionStatus("ReportRequestStatus");
                // The actions sit on the player's own card, at its right end.
                if (social != null && person != null)
                {
                    var friendStatus = RecentActionStatus("FriendRequestStatus");
                    add = HubKnit.Pressable(card, "AddRecentPlayer", "ADD FRIEND", Knit.ThinGo, 950, 20, 300, 72, 30,
                        () => { _ = CompleteRecentAction(add, friendStatus, () => social.RequestAsync(id, handle), "SENDING...", "REQUEST SENT", () => social.SearchStatus); });
                }
                report = HubKnit.Pressable(card, "ReportRecentPlayer", "REPORT", Knit.ThinRed, 1270, 20, 260, 72, 30,
                    () => { _ = CompleteRecentAction(report, reportStatus, () => GameServices.Career != null ? GameServices.Career.ReportAsync(id, ReportReason.Other) : System.Threading.Tasks.Task.FromResult(false), "SENDING...", "REPORTED", () => GameServices.Account != null && GameServices.Account.IsSignedIn && !GameServices.Account.IsGuest ? "Report was not sent. Try again." : "Sign in to report players."); });
            }
        }
    }
}
