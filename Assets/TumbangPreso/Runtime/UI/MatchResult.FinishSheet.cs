using System;
using UnityEngine;
using UnityEngine.UI;
using TumbangPreso.InputLayer;
using TumbangPreso.UI.Hub;

namespace TumbangPreso.UI
{
    public sealed partial class MatchResult
    {
        private static readonly Color SheetGround = new Color32(32, 13, 20, 255);
        private static readonly Color SheetCard = new Color32(43, 34, 38, 235);

        private void BuildNativeResult()
        {
            _nativeResult = true;
            _canvas = OwnerUiLayout.Canvas(transform, "OwnerResultCanvas", 400);
            if (Hud.Instance != null)
            {
                _canvas.transform.SetParent(Hud.Instance.CleanFeedRoot, false);
                OwnerUiLayout.Fill((RectTransform)_canvas.transform);
            }
            var backdrop = OwnerUiLayout.Rect(_canvas.transform, "FinishSheetGround").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(backdrop.rectTransform);
            backdrop.color = new Color(SheetGround.r, SheetGround.g, SheetGround.b, .81f);
            backdrop.raycastTarget = true;

            var root = OwnerUiLayout.DesignArea(_canvas.transform, "ResultsComposition");
            _message = LiveHud.Lettering(root, "ResultHeadline", "", 78, LiveHud.Cream,
                UiTheme.InGameOutline, UiTheme.InGameOutline, 4, 3, 0);
            OwnerUiLayout.Place(_message.rectTransform, 100, 60, 1720, 104);
            _broadcastLine = LiveHud.Fact(root, "ResultMode", "", 32, LiveHud.Cream);
            OwnerUiLayout.Place(_broadcastLine.rectTransform, 100, 164, 1720, 46);

            BuildFinishStandings(root);
            _xpHeadline = LiveHud.Fact(root, "EarnedXp", "", 35, HudDraw.Honey);
            OwnerUiLayout.Place(_xpHeadline.rectTransform, 104, 744, 1110, 44);
            _xpHeadline.alignment = TextAnchor.MiddleLeft;
            _xpDetail = LiveHud.Fact(root, "RewardDetails", "", 28, LiveHud.Cream);
            OwnerUiLayout.Place(_xpDetail.rectTransform, 104, 791, 1706, 68);
            _xpDetail.alignment = TextAnchor.UpperLeft;
            _xpDetail.horizontalOverflow = HorizontalWrapMode.Wrap;

            _rematch = FinishAction(root, "ResultRematch", "REMATCH", Knit.Go, OnRematchPressed, 104, 440);
            _mapVote = FinishAction(root, "ResultNextMap", "NEXT MAP", Knit.DarkCard, OnMapVotePressed, 588, 716);
            _menu = FinishAction(root, "ResultMainMenu", "MAIN MENU", Knit.DarkCard, OnMenuPressed, 1348, 468);
            _rematchTally = LiveHud.Fact(root, "RematchTally", "", 28, LiveHud.Cream);
            OwnerUiLayout.Place(_rematchTally.rectTransform, 104, 985, 440, 46);
            _mapVoteTally = LiveHud.Fact(root, "MapVoteTally", "", 28, LiveHud.Cream);
            OwnerUiLayout.Place(_mapVoteTally.rectTransform, 588, 985, 716, 54);

            NativeProgression(null, null);
            ScreenTakeover.Register(this, () => NativeVisible);
        }

        private static HubButton FinishAction(RectTransform root, string name, string words, Knit style, Action action, float x, float width)
        {
            var button = HubKnit.Button(root, name, words, style, action, 40);
            OwnerUiLayout.Place((RectTransform)button.transform, x, 883, width, 90);
            return button;
        }

        private void BuildFinishStandings(RectTransform root)
        {
            var page = OwnerUiLayout.Rect(root, "Standings");
            OwnerUiLayout.Place(page, 104, 240, 1712, 487);
            _nativePages[0] = page.gameObject;
            for (int i = 0; i < 4; i++)
            {
                var row = OwnerUiLayout.Rect(page, "FinisherTicket" + i);
                OwnerUiLayout.Place(row, 0, i * 121, 1100, 106);
                var card = _finishTickets[i] = row.gameObject.AddComponent<HudCard>();
                card.Glass(SheetCard, slant: HudDraw.Lean, radius: 12);
                card.raycastTarget = false;
                _finishFades[i] = row.gameObject.AddComponent<CanvasGroup>();
                _finishFades[i].blocksRaycasts = false;
                _finishRowOrigins[i] = row.anchoredPosition;

                var place = LiveHud.Fact(row, "FinisherPlace" + i, "", 43, LiveHud.Cream);
                OwnerUiLayout.Place(place.rectTransform, 12, 0, 82, 106);
                var swatch = _finishSwatches[i] = OwnerUiLayout.Rect(row, "FinisherSwatch" + i).gameObject.AddComponent<HudCard>();
                OwnerUiLayout.Place(swatch.rectTransform, 102, 12, 82, 82);
                swatch.Radius = 10; swatch.ShadowAlpha = 0; swatch.FollowContrast = false; swatch.raycastTarget = false;
                _nativePortraits[i] = OwnerUiLayout.Rect(swatch.transform, "FinisherPortrait" + i).gameObject.AddComponent<Image>();
                _nativePortraits[i].preserveAspect = true; _nativePortraits[i].raycastTarget = false;
                OwnerUiLayout.Place(_nativePortraits[i].rectTransform, 3, 3, 76, 76);
                _finishCrowns[i] = OwnerUiLayout.Rect(row, "FinisherCrown" + i).gameObject.AddComponent<HudBadge>();
                OwnerUiLayout.Place(_finishCrowns[i].rectTransform, 128, -13, 30, 28);
                _finishCrowns[i].raycastTarget = false;

                var name = LiveHud.Fact(row, "FinisherName" + i, "", 39, LiveHud.Cream);
                OwnerUiLayout.Place(name.rectTransform, 207, 0, 566, 106);
                name.alignment = TextAnchor.MiddleLeft;
                var score = LiveHud.Fact(row, "FinisherScore" + i, "", 44, LiveHud.Cream);
                OwnerUiLayout.Place(score.rectTransform, 774, 0, 299, 106);
                score.alignment = TextAnchor.MiddleRight;
                var title = OwnerUiLayout.Text(row, "FinisherTitle" + i, "", 28);
                title.enabled = false;
                _rows.Add(new[] { place, name, score, title });
            }
            BuildFinishFigure(page);
        }
    }
}
