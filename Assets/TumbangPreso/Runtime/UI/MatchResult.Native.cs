using UnityEngine;
using UnityEngine.UI;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;

namespace TumbangPreso.UI
{
    public sealed partial class MatchResult
    {
        private bool _nativeResult;
        private readonly GameObject[] _nativePages = new GameObject[3];
        private readonly Button[] _nativeTabs = new Button[3];
        private readonly Image[] _nativePortraits = new Image[4];
        private RectTransform _nativePeople;
        private TumpRankBadge _nativeRank;
        private bool NativeVisible => _canvas != null && _canvas.gameObject.activeInHierarchy;
        private void BuildNativeResult()
        {
            _nativeResult = true;
            _canvas = TumpUiFactory.Canvas(transform, "TumpResultCanvas", 400);
            if (Hud.Instance != null)
            {
                _canvas.transform.SetParent(Hud.Instance.CleanFeedRoot, false);
                TumpUiFactory.Stretch((RectTransform)_canvas.transform);
            }
            var root = (RectTransform)_canvas.transform; var f = TumpUiTheme.Current;
            TumpUiFactory.Ground(root, f.Cream);
            var composition = TumpUiFactory.Rect(root, "ResultsComposition");
            TumpUiFactory.Anchor(composition, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1920, 1080));
            _message = TumpUiFactory.Text(composition, "ResultHeadline", "", 62, true);
            _message.color = f.Brick; _message.alignment = TextAnchor.MiddleCenter;
            TumpUiFactory.Place(_message.rectTransform, 88, 22, 1744, 116);
            _broadcastLine = TumpUiFactory.Text(composition, "ResultMode", "", 28);
            _broadcastLine.alignment = TextAnchor.MiddleCenter; TumpUiFactory.Place(_broadcastLine.rectTransform, 100, 144, 1720, 56);
            var labels = new[] { "Standings", "Your match", "Players" };
            for (int i = 0; i < 3; i++)
            {
                int page = i;
                var tab = _nativeTabs[i] = TumpUiFactory.Button(composition, "ResultTab" + i, labels[i], () => NativePage(page), TumpSurface.Form.Tab, f.Cream, 34);
                TumpUiFactory.Place((RectTransform)tab.transform, 454 + i * 344, 204, 324, 80);
            }
            BuildNativeStandings(composition);
            var detail = TumpUiFactory.Scroll(composition, "MatchDetails", out var detailScroll);
            TumpUiFactory.Place((RectTransform)detailScroll.transform, 168, 326, 1584, 470);
            _nativePages[1] = detailScroll.gameObject;
            _yourMatchLine = TumpUiFactory.Text(detail, "YourMatchSummary", "", 28); _yourMatchLine.gameObject.AddComponent<TumpParagraph>();
            _highlightLine = TumpUiFactory.Text(detail, "MatchHighlight", "", 30, true); _highlightLine.gameObject.AddComponent<TumpParagraph>();
            _xpHeadline = TumpUiFactory.Text(detail, "EarnedXp", "", 40, true); _xpHeadline.color = f.Brick;
            TumpUiFactory.Height(_xpHeadline, 88);
            var track = TumpUiFactory.Rect(detail, "XpTrack").gameObject.AddComponent<Image>();
            track.color = f.OliveSand; track.raycastTarget = false; TumpUiFactory.Height(track, 14, 620);
            _xpBarTrack = track.rectTransform;
            _xpBarFill = TumpUiFactory.Rect(track.transform, "XpFill").gameObject.AddComponent<Image>();
            _xpBarFill.color = f.Orange; _xpBarFill.raycastTarget = false; TumpUiFactory.Stretch(_xpBarFill.rectTransform);
            _nativeRank = TumpUiFactory.Rect(detail, "RankBadge").gameObject.AddComponent<TumpRankBadge>();
            _nativeRank.raycastTarget = false; TumpUiFactory.Height(_nativeRank, 138, 138);
            _xpDetail = TumpUiFactory.Text(detail, "RewardDetails", "", 28); _xpDetail.gameObject.AddComponent<TumpParagraph>();
            _nativePeople = TumpUiFactory.Scroll(composition, "RecentPlayers", out var peopleScroll);
            TumpUiFactory.Place((RectTransform)peopleScroll.transform, 286, 326, 1350, 470); _nativePages[2] = peopleScroll.gameObject;
            _mapVote = TumpUiFactory.Button(composition, "ResultNextMap", "Next map", OnMapVotePressed, TumpSurface.Form.Link, f.Cream, 32);
            TumpUiFactory.Place((RectTransform)_mapVote.transform, 672, 856, 700, 96);
            _mapVoteTally = TumpUiFactory.Text(composition, "MapVoteTally", "", 24); _mapVoteTally.alignment = TextAnchor.MiddleCenter;
            TumpUiFactory.Place(_mapVoteTally.rectTransform, 672, 952, 700, 58);
            _rematch = TumpUiFactory.Button(composition, "ResultRematch", "Rematch", OnRematchPressed, TumpSurface.Form.Slap, f.Lime, 46);
            TumpUiFactory.Place((RectTransform)_rematch.transform, 108, 850, 462, 116);
            _rematchTally = TumpUiFactory.Text(composition, "RematchTally", "", 26); _rematchTally.alignment = TextAnchor.MiddleCenter;
            TumpUiFactory.Place(_rematchTally.rectTransform, 100, 974, 590, 68);
            _menu = TumpUiFactory.Button(composition, "ResultMainMenu", "Main menu", OnMenuPressed, TumpSurface.Form.Link, f.Cream, 34);
            TumpUiFactory.Place((RectTransform)_menu.transform, 1450, 868, 356, 94);
            NativePage(0); NativeProgression(null, null);
            ScreenTakeover.Register(this, () => NativeVisible);
        }
        private void BuildNativeStandings(Transform root)
        {
            var page = TumpUiFactory.Rect(root, "PortraitStandings"); _nativePages[0] = page.gameObject;
            TumpUiFactory.Place(page, 58, 302, 1804, 518);
            for (int i = 0; i < 4; i++)
            {
                float x = i * 452;
                var place = TumpUiFactory.Text(page, "Place" + i, "", 42, true);
                place.alignment = TextAnchor.MiddleCenter; place.color = TumpUiTheme.Current.Brick;
                TumpUiFactory.Place(place.rectTransform, x + 14, 0, 72, 72);
                _nativePortraits[i] = TumpUiFactory.Art(page, "FinisherPortrait" + i, null);
                TumpUiFactory.Place(_nativePortraits[i].rectTransform, x + 104, 20, 240, 240);
                var line = TumpUiFactory.Rect(page, "FinishLine" + i).gameObject.AddComponent<Image>();
                line.color = i == 0 ? TumpUiTheme.Current.Orange : TumpUiTheme.Current.OliveSand; line.raycastTarget = false;
                TumpUiFactory.Place(line.rectTransform, x + 36, 280, 364, i == 0 ? 10 : 3);
                var name = TumpUiFactory.Text(page, "FinisherName" + i, "", 36, true); name.alignment = TextAnchor.MiddleCenter;
                TumpUiFactory.Place(name.rectTransform, x + 20, 298, 406, 126);
                var score = TumpUiFactory.Text(page, "FinisherScore" + i, "", 44, true); score.alignment = TextAnchor.MiddleCenter;
                TumpUiFactory.Place(score.rectTransform, x + 34, 430, 380, 70);
                var title = TumpUiFactory.Text(page, "FinisherTitle" + i, "", 24); title.enabled = false;
                _rows.Add(new[] { place, name, score, title });
            }
        }
        private void NativePage(int page)
        {
            for(int i=0;i<3;i++)
            {
                if (_nativePages[i] != null) _nativePages[i].SetActive(i == page);
                var surface = _nativeTabs[i].GetComponent<TumpSurface>(); surface.Selected = i == page; surface.SetVerticesDirty();
            }
            if (page == 2) NativeRecentPlayers();
            _canvas.GetComponent<ScreenFocus>().Rebuild();
        }
        private void PresentNativeResult(int winner)
        {
            _message.text = winner < 0 ? "It's a draw!" : NameFor(winner) + " wins!"; _message.color = TumpUiTheme.Current.Brick;
            _broadcastLine.text = (SceneFlow.SelectedMode == GameMode.Classic ? "Classic" : "Hero Strike") + " · " + GameServices.Match.TotalRounds + " rounds";
            _broadcastLine.color = TumpUiTheme.Current.DeepOlive;
            var order = GameServices.Match.Ranking();
            for(int i=0;i<4;i++)
            {
                var actor = GameServices.Round?.PlayerAt(order[i]); Sprite portrait = null;
                if (actor != null)
                {
                    var people=Roster.GetPeople(actor.Mode);
                    if(actor.CharacterIndex>=0&&actor.CharacterIndex<people.Count)portrait=TumpUiFactory.Sprite("UI/portraits/"+people[actor.CharacterIndex].Id);
                }
                _nativePortraits[i].sprite=portrait;_nativePortraits[i].enabled=portrait!=null;
            }
            NativePage(0);
        }
        private void NativeProgression(XpAward award, PlayerProfile profile)
        {
            bool show = award != null && profile != null && !IsSpectator;
            _xpHeadline.gameObject.SetActive(show); _xpBarTrack.gameObject.SetActive(show); _xpDetail.gameObject.SetActive(show);
            _nativeRank.gameObject.SetActive(false); if(!show)return;
            int level=ProgressionRules.LevelForXp(profile.Xp);
            _xpHeadline.text=award.Afk||award.Suspended ? "Level "+level+" · No XP this match"
                : award.LevelAfter>award.LevelBefore ? "Level "+award.LevelAfter+" · Level up! +"+award.MatchXp+" XP"
                : "Level "+level+" · +"+award.MatchXp+" XP";
            float ratio=ProgressionRules.XpIntoLevel(profile.Xp)/(float)ProgressionRules.XpPerLevel;
            _xpBarFill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(ratio),1);
            _xpDetail.text=DetailFor(award)+RankLine(profile); _xpDetail.color=TumpUiTheme.Current.DeepOlive;
            if(profile.Rank!=null&&profile.Rank.MatchesThisSeason>0)
            {
                _nativeRank.Tier=(int)RatingRules.TierFor(profile.Rank.Rating);_nativeRank.SetVerticesDirty();_nativeRank.gameObject.SetActive(true);
            }
        }
        private void NativeRecentPlayers()
        {
            if(_nativePeople==null)return;
            foreach(Transform child in _nativePeople){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var social=GameServices.Social;var record=GameServices.Stats?.Last;
            var offer=SocialRules.RecentPlayers(record,social?.List,Net.CareerStore.LocalPlayerId);
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var actor = GameServices.Round?.PlayerAt(slot);
                var identity = TumpUiFactory.Rect(_nativePeople, "RecentPlayerIdentity" + slot); TumpUiFactory.Height(identity, 104);
                if (actor != null)
                {
                    var people = Roster.GetPeople(actor.Mode);
                    if (actor.CharacterIndex >= 0 && actor.CharacterIndex < people.Count)
                    {
                        var portrait = TumpUiFactory.Art(identity, "RecentPlayerPortrait", TumpUiFactory.Sprite("UI/portraits/" + people[actor.CharacterIndex].Id));
                        TumpUiFactory.Place(portrait.rectTransform, 4, 0, 96, 96);
                    }
                }
                var name=TumpUiFactory.Text(identity,"RecentPlayerName",NameFor(slot),38,true);
                TumpUiFactory.Place(name.rectTransform, 128, 0, 1050, 92);
                string title = TitleFor(slot);
                if (!string.IsNullOrEmpty(title))
                {
                    var build = TumpUiFactory.Text(_nativePeople, "PublicBuildAndTitle", title, 26);
                    build.gameObject.AddComponent<TumpParagraph>();
                }
                FriendRef person = null;
                foreach (var candidate in offer)
                {
                    var line = MatchRecordRules.LineFor(record, candidate.PlayerId);
                    if (line != null && line.Slot == slot) { person = candidate; break; }
                }
                if (social == null || person == null) continue;
                string id=person.PlayerId,handle=person.Handle;
                var row=TumpUiFactory.Rect(_nativePeople,"PlayerActions");TumpUiFactory.Height(row,86);
                Button add=null;
                add=TumpUiFactory.Button(row,"AddRecentPlayer","Add friend",()=>{social.Request(id,handle);add.interactable=false;add.GetComponentInChildren<Text>().text="Request sent";},TumpSurface.Form.Link,TumpUiTheme.Current.Cream,30);
                TumpUiFactory.Place((RectTransform)add.transform,0,0,370,84);
                Button report=null;
                report=TumpUiFactory.Button(row,"ReportRecentPlayer","Report",()=>{GameServices.Career?.Report(id,ReportReason.Other);report.interactable=false;report.GetComponentInChildren<Text>().text="Reported";},TumpSurface.Form.Link,TumpUiTheme.Current.Cream,28);
                TumpUiFactory.Place((RectTransform)report.transform,450,0,300,84);
            }
        }
        private void Update()
        {
            if(!_nativeResult||!NativeVisible||!MenuNav.CancelPressed||ScreenTakeover.EscapeIsSpokenExcept(this))return;
            ScreenTakeover.ConsumeEscape();OnMenuPressed();
        }
    }
}
