using UnityEngine;
using UnityEngine.UI;
using TumbangPreso.InputLayer;
using TumbangPreso.UI.Hub;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The match-end board (TODO VISUAL-1.16).
    ///
    /// ⚠️⚠️ THE MATCH'S OWN CARDS, OVER THE COURT, NOT A CREAM PAGE. The board was a full-screen
    /// cream sheet with red diagonal tickets and three text buttons of equal weight, drawn in a
    /// fourth visual language after the match bar, the feed and the halftime popup. It is now
    /// the same family grown: the halftime brush for the winner line, the bar's cream chips for
    /// the standings (portrait on the seat colour, crown for the winner, gold underline for
    /// you), and on each chip two ACCOLADES, a glyph and a number from the match record
    /// (`MatchResult.Accolades`), in the way Rocket League's podium gives every player two
    /// earned lines. No invented stat: an accolade with nothing behind it is not drawn.
    ///
    /// ⚠️ THE COURT STAYS VISIBLE. The ground is the clock's warm plate at 62 percent, which is
    /// what the cards need to read over a bright sky, and it is still the full-screen click
    /// blocker the cream sheet was (`CLAUDE.md` § 6.2c: name the replacement blocker). The
    /// winner's figure stands on the right, where it always did.
    ///
    /// ⚠️ ONE OBVIOUS NEXT ACTION, IN THE GAME'S OWN BUTTON (2026-09-24). REMATCH is the chartreuse
    /// primary and NEXT MAP and MAIN MENU are honey, all three the hub's pressable sticker
    /// (`HubShape`: die-cut rim, varnish, lip, hard shadow), and the three tabs are stickers too.
    /// They were a flat gold box and two flat dark plates, so the last screen of a match spoke a
    /// different button language from every menu the player walks back into; the owner:
    /// "each screen shoudl feel like their own screen" and "include ingame ui". Every route,
    /// name, tab and focus path is unchanged, so the pad, the thumb and `TumpNativeResultTests`
    /// reach exactly what they reached before.
    ///
    /// ⚠️ AND THE FIESTA ARRIVES. Bunting drops in over the winner's banner a beat after it, the
    /// same strings HubScenery hangs over the lobby and MATCH FOUND and the maps hang across
    /// their streets, stirring on the title street's breeze. Reduced motion hangs it still.
    /// </summary>
    public sealed partial class MatchResult
    {
        private void BuildNativeResult()
        {
            _nativeResult = true; _canvas = OwnerUiLayout.Canvas(transform, "OwnerResultCanvas", 400);
            if (Hud.Instance != null) { _canvas.transform.SetParent(Hud.Instance.CleanFeedRoot, false); OwnerUiLayout.Fill((RectTransform)_canvas.transform); }
            var backdrop = OwnerUiLayout.Rect(_canvas.transform, "FinishSheetGround").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(backdrop.rectTransform); backdrop.color = new Color(HudDraw.Plate.r, HudDraw.Plate.g, HudDraw.Plate.b, .62f);
            backdrop.raycastTarget = true; // the click blocker the cream sheet used to be
            var root = OwnerUiLayout.DesignArea(_canvas.transform, "ResultsComposition");
            HubScenery.Bunting(root, 11, -6, 0.35f);

            var bannerShadow = OwnerUiLayout.Rect(root, "HeadlineShadow").gameObject.AddComponent<CourtPopupGraphic>();
            bannerShadow.Brush = true; bannerShadow.color = CourtPresentationPalette.Ink; bannerShadow.raycastTarget = false;
            OwnerUiLayout.Place(bannerShadow.rectTransform, 488, 44, 960, 124);
            var banner = OwnerUiLayout.Rect(root, "HeadlineBrush").gameObject.AddComponent<CourtPopupGraphic>();
            banner.Brush = true; banner.color = CourtPresentationPalette.Red; banner.raycastTarget = false;
            OwnerUiLayout.Place(banner.rectTransform, 480, 38, 960, 124);
            _message = OwnerUiLayout.Text(root, "ResultHeadline", "", 72, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_message.rectTransform, 500, 38, 920, 124); _message.alignment = TextAnchor.MiddleCenter;
            _message.rectTransform.localRotation = Quaternion.Euler(0, 0, 1.2f);
            _broadcastLine = OwnerUiLayout.Text(root, "ResultMode", "", 30);
            OwnerUiLayout.Place(_broadcastLine.rectTransform, 480, 172, 960, 46); _broadcastLine.alignment = TextAnchor.MiddleCenter;
            Keel(_broadcastLine);

            string[] labels = { "STANDINGS", "YOUR MATCH", "PLAYERS" };
            for (int i = 0; i < 3; i++)
            {
                int page = i;
                var tab = OwnerTextAction.Create(root, "ResultTab" + i, labels[i], () => NativePage(page), 530 + i * 290, 232, 268, 64, 30);
                var line = OwnerUiLayout.Rect(tab.transform, "SelectedPage").gameObject.AddComponent<Image>();
                OwnerUiLayout.Place(line.rectTransform, 50, 58, 180, 5); line.color = CourtPresentationPalette.Gold; line.raycastTarget = false;
                Sticker((RectTransform)tab.transform, HubStyle.Honey, 930 + i);
                tab.GetComponentInChildren<Text>().font = OwnerUiTheme.Current.Display;
                _nativeTabs[i] = tab;
            }
            BuildFinishStandings(root);

            var details = OwnerUiLayout.Rect(root, "MatchDetailsPage"); OwnerUiLayout.Fill(details); _nativePages[1] = details.gameObject;
            var detailCard = OwnerUiLayout.Rect(details, "MatchNotes").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(detailCard.rectTransform, 110, 318, 1700, 560); detailCard.color = CourtPresentationPalette.Paper; detailCard.Radius = 22; detailCard.raycastTarget = false;
            var content = OwnerScrollColumn.Build(details, "MatchDetails", new Rect(150, 348, 1620, 500), out var detailScroll);
            _ownerEmptyDetails = OwnerUiLayout.Text(content, "EmptyMatchDetails", "No match summary was recorded for this game.", 31);
            _ownerEmptyDetails.color = HudDraw.CardInk; _ownerEmptyDetails.gameObject.AddComponent<TumpParagraph>();
            _yourMatchLine = OwnerUiLayout.Text(content, "YourMatchSummary", "", 31);
            _yourMatchLine.color = HudDraw.CardInk; _yourMatchLine.gameObject.AddComponent<TumpParagraph>();
            _highlightLine = OwnerUiLayout.Text(content, "MatchHighlight", "", 34, OwnerUiLayout.TypeRole.Accent); _highlightLine.gameObject.AddComponent<TumpParagraph>();
            _xpHeadline = OwnerUiLayout.Text(content, "EarnedXp", "", 43, OwnerUiLayout.TypeRole.Display);
            _xpHeadline.color = HudDraw.CardInk; _xpHeadline.gameObject.AddComponent<LayoutElement>().preferredHeight = 93;
            var track = OwnerUiLayout.Rect(content, "XpTrack").gameObject.AddComponent<Image>();
            track.color = new Color32(171, 137, 92, 255); track.raycastTarget = false;
            // A chunky inked track, the hub's, rather than a 12-unit hairline under a big number.
            track.gameObject.AddComponent<LayoutElement>().preferredHeight = 26; _xpBarTrack = track.rectTransform;
            var trackEdge = track.gameObject.AddComponent<Outline>(); trackEdge.effectColor = HudDraw.CardInk; trackEdge.effectDistance = new Vector2(3, -3);
            _xpBarFill = OwnerUiLayout.Rect(track.transform, "XpFill").gameObject.AddComponent<Image>();
            _xpBarFill.color = CourtPresentationPalette.Gold; _xpBarFill.raycastTarget = false; OwnerUiLayout.Fill(_xpBarFill.rectTransform);
            _nativeRank = OwnerUiLayout.Rect(content, "RankBadge").gameObject.AddComponent<TumpRankBadge>(); _nativeRank.raycastTarget = false;
            var badge = _nativeRank.gameObject.AddComponent<LayoutElement>(); badge.preferredHeight = 138; badge.preferredWidth = 138;
            _xpDetail = OwnerUiLayout.Text(content, "RewardDetails", "", 30); _xpDetail.color = HudDraw.CardInk; _xpDetail.gameObject.AddComponent<TumpParagraph>();

            var people = OwnerUiLayout.Rect(root, "RecentPeoplePage"); OwnerUiLayout.Fill(people); _nativePages[2] = people.gameObject;
            var peopleCard = OwnerUiLayout.Rect(people, "PeoplePaper").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Place(peopleCard.rectTransform, 110, 318, 1700, 560); peopleCard.color = CourtPresentationPalette.Paper; peopleCard.Radius = 22; peopleCard.raycastTarget = false;
            _nativePeople = OwnerScrollColumn.Build(people, "RecentPlayers", new Rect(150, 348, 1620, 500), out var peopleScroll);

            // The one obvious next action, then the two quiet ones.
            _rematch = OwnerTextAction.Create(root, "ResultRematch", "REMATCH", OnRematchPressed, 100, 912, 480, 96, 46);
            Sticker((RectTransform)_rematch.transform, HubStyle.Chartreuse, 940);
            _rematch.GetComponentInChildren<Text>().font = OwnerUiTheme.Current.Display;
            _rematchTally = OwnerUiLayout.Text(root, "RematchTally", "", 28);
            OwnerUiLayout.Place(_rematchTally.rectTransform, 100, 1014, 480, 52); _rematchTally.alignment = TextAnchor.MiddleCenter; Keel(_rematchTally);
            _mapVote = OwnerTextAction.Create(root, "ResultNextMap", "NEXT MAP", OnMapVotePressed, 660, 920, 740, 80, 31);
            Sticker((RectTransform)_mapVote.transform, HubStyle.Honey, 941);
            _mapVote.GetComponentInChildren<Text>().font = OwnerUiTheme.Current.Display;
            _mapVoteTally = OwnerUiLayout.Text(root, "MapVoteTally", "", 28);
            OwnerUiLayout.Place(_mapVoteTally.rectTransform, 660, 1010, 740, 56); _mapVoteTally.alignment = TextAnchor.MiddleCenter; Keel(_mapVoteTally);
            _menu = OwnerTextAction.Create(root, "ResultMainMenu", "MAIN MENU", OnMenuPressed, 1470, 920, 350, 80, 31);
            Sticker((RectTransform)_menu.transform, HubStyle.Honey, 942);
            _menu.GetComponentInChildren<Text>().font = OwnerUiTheme.Current.Display;
            NativePage(0); NativeProgression(null, null); ScreenTakeover.Register(this, () => NativeVisible);
        }

        /// <summary>
        /// The hub's pressable sticker behind a text action, painted as the first child so the label
        /// reads over it. Named ActionPlate still, because that is what it replaced.
        /// </summary>
        private static HubShape Sticker(RectTransform action, Color fill, int seed)
        {
            var sticker = HubKit.Shape(action, "ActionPlate", fill, true, seed, 5, 16);
            HubKit.Stretch(sticker.rectTransform);
            sticker.transform.SetAsFirstSibling();
            var label = action.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.color = HubStyle.TextOn(fill);
                foreach (var edge in label.GetComponents<Outline>()) Destroy(edge);
            }
            return sticker;
        }

        /// <summary>Paper type over the dimmed court gets the in-game black outline.</summary>
        private static void Keel(Text text)
        {
            if (text == null) return;
            text.color = CourtPresentationPalette.Paper;
            var edge = text.gameObject.AddComponent<Outline>(); edge.effectColor = UiTheme.InGameOutline; edge.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private void BuildFinishStandings(RectTransform root)
        {
            var page = OwnerUiLayout.Rect(root, "Standings"); OwnerUiLayout.Place(page, 94, 318, 1738, 560); _nativePages[0] = page.gameObject;
            for (int i = 0; i < 4; i++)
            {
                var row = OwnerUiLayout.Rect(page, "FinisherTicket" + i); OwnerUiLayout.Place(row, 0, i * 132, 1138, 120);
                var card = _finishTickets[i] = row.gameObject.AddComponent<HudCard>(); card.color = CourtPresentationPalette.Paper; card.Radius = 20; card.raycastTarget = false;
                _finishFades[i] = row.gameObject.AddComponent<CanvasGroup>(); _finishFades[i].blocksRaycasts = false; _finishRowOrigins[i] = row.anchoredPosition;
                var place = OwnerUiLayout.Text(row, "FinisherPlace" + i, "", 48, OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(place.rectTransform, 14, 14, 86, 92); place.alignment = TextAnchor.MiddleCenter;
                var swatch = _finishSwatches[i] = OwnerUiLayout.Rect(row, "FinisherSwatch" + i).gameObject.AddComponent<HudCard>();
                OwnerUiLayout.Place(swatch.rectTransform, 104, 12, 96, 96); swatch.Radius = 14; swatch.ShadowAlpha = 0; swatch.FollowContrast = false; swatch.raycastTarget = false;
                _nativePortraits[i] = OwnerUiLayout.Rect(swatch.transform, "FinisherPortrait" + i).gameObject.AddComponent<Image>();
                _nativePortraits[i].preserveAspect = true; _nativePortraits[i].raycastTarget = false; OwnerUiLayout.Place(_nativePortraits[i].rectTransform, 4, 4, 88, 88);
                _finishCrowns[i] = OwnerUiLayout.Rect(row, "FinisherCrown" + i).gameObject.AddComponent<HudBadge>();
                OwnerUiLayout.Place(_finishCrowns[i].rectTransform, 130, -18, 44, 34); _finishCrowns[i].raycastTarget = false;
                var name = OwnerUiLayout.Text(row, "FinisherName" + i, "", 36, OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(name.rectTransform, 222, 6, 560, 56);
                BuildAccolades(row, i, 222, 64);
                var score = OwnerUiLayout.Text(row, "FinisherScore" + i, "", 44, OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(score.rectTransform, 800, 14, 310, 92); score.alignment = TextAnchor.MiddleRight;
                var title = OwnerUiLayout.Text(row, "FinisherTitle" + i, "", 28); title.enabled = false; _rows.Add(new[] { place, name, score, title });
            }
            BuildFinishFigure(page);
        }
    }
}
