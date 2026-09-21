using UnityEngine;
using UnityEngine.UI;
using TumbangPreso.InputLayer;

namespace TumbangPreso.UI
{
    public sealed partial class MatchResult
    {
        private void BuildNativeResult()
        {
            _nativeResult = true; _canvas = OwnerUiLayout.Canvas(transform, "OwnerResultCanvas", 400);
            if (Hud.Instance != null) { _canvas.transform.SetParent(Hud.Instance.CleanFeedRoot, false); OwnerUiLayout.Fill((RectTransform)_canvas.transform); }
            var backdrop = OwnerUiLayout.Rect(_canvas.transform, "FinishSheetGround").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(backdrop.rectTransform); backdrop.color = new Color32(241, 229, 202, 255); backdrop.raycastTarget = false;
            var root = OwnerUiLayout.DesignArea(_canvas.transform, "ResultsComposition");
            _message = OwnerUiLayout.Text(root, "ResultHeadline", "", 78, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_message.rectTransform, 93, 52, 1710, 122);
            _broadcastLine = OwnerUiLayout.Text(root, "ResultMode", "", 30);
            OwnerUiLayout.Place(_broadcastLine.rectTransform, 100, 184, 1694, 63);
            string[] labels = { "STANDINGS", "YOUR MATCH", "PLAYERS" };
            for (int i = 0; i < 3; i++)
            {
                int page = i;
                var tab = OwnerTextAction.Create(root, "ResultTab" + i, labels[i], () => NativePage(page), 99 + i * 568, 263, 534, 72, 35);
                var line = OwnerUiLayout.Rect(tab.transform, "SelectedPage").gameObject.AddComponent<Image>();
                OwnerUiLayout.Place(line.rectTransform, 90, 66, 350, 4); line.color = CourtPresentationPalette.Red; line.raycastTarget = false;
                _nativeTabs[i] = tab;
            }
            BuildFinishStandings(root);
            var details = OwnerUiLayout.Rect(root, "MatchDetailsPage"); OwnerUiLayout.Fill(details); _nativePages[1] = details.gameObject;
            var content = OwnerScrollColumn.Build(details, "MatchDetails", new Rect(132, 377, 1634, 476), out var detailScroll);
            _ownerEmptyDetails = OwnerUiLayout.Text(content, "EmptyMatchDetails", "No match summary was recorded for this game.", 31);
            _ownerEmptyDetails.color = OwnerUiTheme.Current.EnteredInk; _ownerEmptyDetails.gameObject.AddComponent<TumpParagraph>();
            _yourMatchLine = OwnerUiLayout.Text(content, "YourMatchSummary", "", 31);
            _yourMatchLine.color = OwnerUiTheme.Current.EnteredInk; _yourMatchLine.gameObject.AddComponent<TumpParagraph>();
            _highlightLine = OwnerUiLayout.Text(content, "MatchHighlight", "", 34, OwnerUiLayout.TypeRole.Accent); _highlightLine.gameObject.AddComponent<TumpParagraph>();
            _xpHeadline = OwnerUiLayout.Text(content, "EarnedXp", "", 43, OwnerUiLayout.TypeRole.Display);
            _xpHeadline.gameObject.AddComponent<LayoutElement>().preferredHeight = 93;
            var track = OwnerUiLayout.Rect(content, "XpTrack").gameObject.AddComponent<Image>();
            track.color = new Color32(171, 137, 92, 255); track.raycastTarget = false;
            track.gameObject.AddComponent<LayoutElement>().preferredHeight = 12; _xpBarTrack = track.rectTransform;
            _xpBarFill = OwnerUiLayout.Rect(track.transform, "XpFill").gameObject.AddComponent<Image>();
            _xpBarFill.color = CourtPresentationPalette.Gold; _xpBarFill.raycastTarget = false; OwnerUiLayout.Fill(_xpBarFill.rectTransform);
            _nativeRank = OwnerUiLayout.Rect(content, "RankBadge").gameObject.AddComponent<TumpRankBadge>(); _nativeRank.raycastTarget = false;
            var badge = _nativeRank.gameObject.AddComponent<LayoutElement>(); badge.preferredHeight = 138; badge.preferredWidth = 138;
            _xpDetail = OwnerUiLayout.Text(content, "RewardDetails", "", 30); _xpDetail.color = OwnerUiTheme.Current.EnteredInk; _xpDetail.gameObject.AddComponent<TumpParagraph>();
            var people = OwnerUiLayout.Rect(root, "RecentPeoplePage"); OwnerUiLayout.Fill(people); _nativePages[2] = people.gameObject;
            _nativePeople = OwnerScrollColumn.Build(people, "RecentPlayers", new Rect(136, 375, 1628, 477), out var peopleScroll);
            _rematch = OwnerTextAction.Create(root, "ResultRematch", "REMATCH", OnRematchPressed, 100, 913, 477, 92, 44);
            _rematch.GetComponentInChildren<Text>().font = OwnerUiTheme.Current.Display;
            _rematchTally = OwnerUiLayout.Text(root, "RematchTally", "", 28);
            OwnerUiLayout.Place(_rematchTally.rectTransform, 104, 1012, 531, 57); _rematchTally.alignment = TextAnchor.MiddleCenter;
            _mapVote = OwnerTextAction.Create(root, "ResultNextMap", "NEXT MAP", OnMapVotePressed, 660, 921, 742, 83, 31);
            _mapVoteTally = OwnerUiLayout.Text(root, "MapVoteTally", "", 28);
            OwnerUiLayout.Place(_mapVoteTally.rectTransform, 672, 1011, 728, 57); _mapVoteTally.alignment = TextAnchor.MiddleCenter;
            _menu = OwnerTextAction.Create(root, "ResultMainMenu", "MAIN MENU", OnMenuPressed, 1452, 922, 354, 81, 31);
            NativePage(0); NativeProgression(null, null); ScreenTakeover.Register(this, () => NativeVisible);
        }
        private void BuildFinishStandings(Transform root)
        {
            var page=OwnerUiLayout.Rect(root,"Standings");OwnerUiLayout.Place(page,94,361,1738,505);_nativePages[0]=page.gameObject;
            for(int i=0;i<4;i++)
            {
                var row=OwnerUiLayout.Rect(page,"FinisherTicket"+i);OwnerUiLayout.Place(row,0,i*126,1138,114);
                _finishTickets[i]=row.gameObject.AddComponent<CourtPopupGraphic>();_finishTickets[i].raycastTarget=false;
                _finishFades[i]=row.gameObject.AddComponent<CanvasGroup>();_finishFades[i].blocksRaycasts=false;_finishRowOrigins[i]=row.anchoredPosition;
                var place=OwnerUiLayout.Text(row,"FinisherPlace"+i,"",44,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(place.rectTransform,24,12,88,92);place.alignment=TextAnchor.MiddleCenter;
                _nativePortraits[i]=OwnerUiLayout.Rect(row,"FinisherPortrait"+i).gameObject.AddComponent<Image>();
                _nativePortraits[i].preserveAspect=true;_nativePortraits[i].raycastTarget=false;OwnerUiLayout.Place(_nativePortraits[i].rectTransform,131,10,92,92);
                var name=OwnerUiLayout.Text(row,"FinisherName"+i,"",36,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(name.rectTransform,246,10,581,91);
                var score=OwnerUiLayout.Text(row,"FinisherScore"+i,"",39,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(score.rectTransform,837,10,263,91);score.alignment=TextAnchor.MiddleRight;
                var title=OwnerUiLayout.Text(row,"FinisherTitle"+i,"",28);title.enabled=false;_rows.Add(new[]{place,name,score,title});
            }
            BuildFinishFigure(page);
        }
    }
}
