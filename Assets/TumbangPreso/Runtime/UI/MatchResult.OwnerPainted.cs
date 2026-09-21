using UnityEngine;
using UnityEngine.UI;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;

namespace TumbangPreso.UI
{
    public sealed partial class MatchResult
    {
        private Text _ownerEmptyDetails;
        private void BuildPreviousOwnerResult()
        {
            _nativeResult=true;_canvas=OwnerUiLayout.Canvas(transform,"OwnerResultCanvas",400);
            if(Hud.Instance!=null){_canvas.transform.SetParent(Hud.Instance.CleanFeedRoot,false);OwnerUiLayout.Fill((RectTransform)_canvas.transform);}
            OwnerUiBackdrop.Build(_canvas.transform);
            var root=OwnerUiLayout.DesignArea(_canvas.transform,"ResultsComposition");
            _message=OwnerUiLayout.Text(root,"ResultHeadline","",63,OwnerUiLayout.TypeRole.Display);_message.alignment=TextAnchor.MiddleCenter;
            OwnerUiLayout.Place(_message.rectTransform,87,30,1745,111);
            _broadcastLine=OwnerUiLayout.Text(root,"ResultMode","",30);_broadcastLine.color=OwnerUiTheme.Current.EnteredInk;
            _broadcastLine.alignment=TextAnchor.MiddleCenter;OwnerUiLayout.Place(_broadcastLine.rectTransform,99,151,1722,58);
            var labels=new[]{"STANDINGS","YOUR MATCH","PLAYERS"};
            for(int i=0;i<3;i++)
            {
                int page=i;var tab=OwnerTextAction.Create(root,"ResultTab"+i,labels[i],()=>NativePage(page),437+i*348,230,326,72,32);
                var line=OwnerUiLayout.Art(tab.transform,"SelectedPage",OwnerUiTheme.Piece.LeftRule);OwnerUiLayout.Place(line.rectTransform,42,67,242,6);_nativeTabs[i]=tab;
            }
            BuildNativeStandings(root);
            var details=OwnerUiLayout.Rect(root,"MatchDetailsPage");OwnerUiLayout.Fill(details);_nativePages[1]=details.gameObject;
            var detailPaper=OwnerUiLayout.Rect(details,"MatchNotes").gameObject.AddComponent<OwnerUiPaper>();
            OwnerUiLayout.Place(detailPaper.rectTransform,110,350,1700,489);detailPaper.raycastTarget=false;
            var content=OwnerScrollColumn.Build(details,"MatchDetails",new Rect(151,385,1605,422),out var detailScroll);
            _ownerEmptyDetails=OwnerUiLayout.Text(content,"EmptyMatchDetails","No match summary was recorded for this game.",30);
            _ownerEmptyDetails.color=OwnerUiTheme.Current.EnteredInk;_ownerEmptyDetails.gameObject.AddComponent<TumpParagraph>();
            _yourMatchLine=OwnerUiLayout.Text(content,"YourMatchSummary","",29);_yourMatchLine.color=OwnerUiTheme.Current.EnteredInk;_yourMatchLine.gameObject.AddComponent<TumpParagraph>();
            _highlightLine=OwnerUiLayout.Text(content,"MatchHighlight","",32,OwnerUiLayout.TypeRole.Accent);_highlightLine.gameObject.AddComponent<TumpParagraph>();
            _xpHeadline=OwnerUiLayout.Text(content,"EarnedXp","",41,OwnerUiLayout.TypeRole.Display);_xpHeadline.gameObject.AddComponent<LayoutElement>().preferredHeight=83;
            var track=OwnerUiLayout.Rect(content,"XpTrack").gameObject.AddComponent<Image>();track.color=OwnerUiTheme.Current.Peach;track.raycastTarget=false;
            track.gameObject.AddComponent<LayoutElement>().preferredHeight=12;_xpBarTrack=track.rectTransform;
            _xpBarFill=OwnerUiLayout.Rect(track.transform,"XpFill").gameObject.AddComponent<Image>();_xpBarFill.color=OwnerUiTheme.Current.Green;
            _xpBarFill.raycastTarget=false;OwnerUiLayout.Fill(_xpBarFill.rectTransform);
            _nativeRank=OwnerUiLayout.Rect(content,"RankBadge").gameObject.AddComponent<TumpRankBadge>();_nativeRank.raycastTarget=false;
            var badgeSize=_nativeRank.gameObject.AddComponent<LayoutElement>();badgeSize.preferredHeight=138;badgeSize.preferredWidth=138;
            _xpDetail=OwnerUiLayout.Text(content,"RewardDetails","",29);_xpDetail.color=OwnerUiTheme.Current.EnteredInk;_xpDetail.gameObject.AddComponent<TumpParagraph>();
            var people=OwnerUiLayout.Rect(root,"RecentPeoplePage");OwnerUiLayout.Fill(people);_nativePages[2]=people.gameObject;
            var peoplePaper=OwnerUiLayout.Rect(people,"PeoplePaper").gameObject.AddComponent<OwnerUiPaper>();
            OwnerUiLayout.Place(peoplePaper.rectTransform,110,350,1700,489);peoplePaper.raycastTarget=false;
            _nativePeople=OwnerScrollColumn.Build(people,"RecentPlayers",new Rect(185,385,1520,422),out var peopleScroll);
            _rematch=OwnerPaintedAction.Create(root,"ResultRematch","REMATCH",OnRematchPressed,false,48);
            OwnerUiLayout.Place((RectTransform)_rematch.transform,107,916,413,91);
            _rematchTally=OwnerUiLayout.Text(root,"RematchTally","",27);_rematchTally.color=OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(_rematchTally.rectTransform,105,1018,563,48);_rematchTally.alignment=TextAnchor.MiddleCenter;
            _mapVote=OwnerTextAction.Create(root,"ResultNextMap","NEXT MAP",OnMapVotePressed,659,909,724,92,31);
            _mapVoteTally=OwnerUiLayout.Text(root,"MapVoteTally","",27);_mapVoteTally.color=OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(_mapVoteTally.rectTransform,674,1015,710,51);_mapVoteTally.alignment=TextAnchor.MiddleCenter;
            _menu=OwnerTextAction.Create(root,"ResultMainMenu","MAIN MENU",OnMenuPressed,1450,918,357,83,31);
            NativePage(0);NativeProgression(null,null);ScreenTakeover.Register(this,()=>NativeVisible);
        }
        private void BuildNativeStandings(Transform root)
        {
            var page=OwnerUiLayout.Rect(root,"Standings");OwnerUiLayout.Place(page,91,352,1736,500);_nativePages[0]=page.gameObject;
            for(int i=0;i<4;i++)
            {
                float x=i*435;
                var mat=OwnerUiLayout.Rect(page,"PortraitMat"+i).gameObject.AddComponent<OwnerPreviewMat>();
                OwnerUiLayout.Place(mat.rectTransform,x+43,42,342,260);mat.raycastTarget=false;
                var place=OwnerUiLayout.Text(page,"FinisherPlace"+i,"",44,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(place.rectTransform,x+14,0,83,80);place.alignment=TextAnchor.MiddleCenter;
                _nativePortraits[i]=OwnerUiLayout.Rect(page,"FinisherPortrait"+i).gameObject.AddComponent<Image>();
                _nativePortraits[i].preserveAspect=true;_nativePortraits[i].raycastTarget=false;OwnerUiLayout.Place(_nativePortraits[i].rectTransform,x+79,31,275,275);
                var name=OwnerUiLayout.Text(page,"FinisherName"+i,"",36,OwnerUiLayout.TypeRole.Display);name.alignment=TextAnchor.MiddleCenter;
                OwnerUiLayout.Place(name.rectTransform,x+6,329,419,91);
                var score=OwnerUiLayout.Text(page,"FinisherScore"+i,"",44,OwnerUiLayout.TypeRole.Display);score.alignment=TextAnchor.MiddleCenter;
                OwnerUiLayout.Place(score.rectTransform,x+17,427,396,72);
                var title=OwnerUiLayout.Text(page,"FinisherTitle"+i,"",25);title.enabled=false;_rows.Add(new[]{place,name,score,title});
            }
        }
        private void NativePage(int page)
        {
            RefreshOwnerResultEmptyState();
            for(int i=0;i<3;i++)
            {
                if(_nativePages[i]!=null)_nativePages[i].SetActive(i==page);
                _nativeTabs[i].GetComponentInChildren<Text>().color=i==page?CourtPresentationPalette.Red:OwnerUiTheme.Current.EnteredInk;
                _nativeTabs[i].transform.Find("SelectedPage").gameObject.SetActive(i==page);
            }
            if(page==2)NativeRecentPlayers();_canvas.GetComponent<ScreenFocus>().Rebuild();
        }
        private void RefreshOwnerResultEmptyState()
        {
            if(_ownerEmptyDetails==null)return;
            bool hasDetails=(_yourMatchLine!=null && !string.IsNullOrWhiteSpace(_yourMatchLine.text)) ||
                (_highlightLine!=null && !string.IsNullOrWhiteSpace(_highlightLine.text)) ||
                (_xpHeadline!=null && _xpHeadline.gameObject.activeSelf && !string.IsNullOrWhiteSpace(_xpHeadline.text));
            _ownerEmptyDetails.gameObject.SetActive(!hasDetails);
        }
        private void NativeRecentPlayers()
        {
            if(_nativePeople==null)return;
            foreach(Transform child in _nativePeople){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var social=GameServices.Social;var record=GameServices.Stats?.Last;
            var offers=SocialRules.RecentPlayers(record,social?.List,Net.CareerStore.LocalPlayerId);
            for(int slot=0;slot<Balance.PlayerCount;slot++)
            {
                var actor=GameServices.Round?.PlayerAt(slot);var identity=OwnerUiLayout.Rect(_nativePeople,"RecentPlayerIdentity"+slot);
                identity.gameObject.AddComponent<LayoutElement>().preferredHeight=102;
                if(actor!=null)
                {
                    var people=Roster.GetPeople(actor.Mode);
                    if(actor.CharacterIndex>=0 && actor.CharacterIndex<people.Count)
                    {
                        var portrait=OwnerPortraitArt.Create(identity,"RecentPlayerPortrait","UI/portraits/"+people[actor.CharacterIndex].Id);
                        OwnerUiLayout.Place(portrait.rectTransform,8,0,96,96);
                    }
                }
                var name=OwnerUiLayout.Text(identity,"RecentPlayerName",NameFor(slot),36,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(name.rectTransform,130,0,1290,96);
                string title=TitleFor(slot);
                if(!string.IsNullOrEmpty(title))
                {
                    var build=OwnerUiLayout.Text(_nativePeople,"PublicBuildAndTitle",title,27);build.color=OwnerUiTheme.Current.EnteredInk;build.gameObject.AddComponent<TumpParagraph>();
                }
                FriendRef person=null;
                foreach(var candidate in offers)
                {
                    var line=MatchRecordRules.LineFor(record,candidate.PlayerId);if(line!=null && line.Slot==slot){person=candidate;break;}
                }
                if(social==null || person==null)continue;
                string id=person.PlayerId,handle=person.Handle;
                var row=OwnerUiLayout.Rect(_nativePeople,"PlayerActions");row.gameObject.AddComponent<LayoutElement>().preferredHeight=72;
                Button add=null,report=null;
                add=OwnerTextAction.Create(row,"AddRecentPlayer","ADD FRIEND",()=>{social.Request(id,handle);add.interactable=false;add.GetComponentInChildren<Text>().text="REQUEST SENT";},0,0,350,72,29);
                report=OwnerTextAction.Create(row,"ReportRecentPlayer","REPORT",()=>{GameServices.Career?.Report(id,ReportReason.Other);report.interactable=false;report.GetComponentInChildren<Text>().text="REPORTED";},432,0,310,72,29);
            }
        }
    }
}
