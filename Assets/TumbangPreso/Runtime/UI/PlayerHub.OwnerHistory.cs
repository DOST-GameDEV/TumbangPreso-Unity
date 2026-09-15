using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class PlayerHub
    {
        private void BuildMatchesTab()
        {
            var career=GameServices.Career;
            if(_page==0 && _shown.Count==0 && career?.History!=null)
            {
                _shown=new List<MatchRecord>();
                for(int i=0;i<career.History.Count && i<HistoryPageSize;i++)_shown.Add(career.History[i]);
            }
            HubNote("Recent matches, newest first. Select a match for its scorecard.");
            if(_shown.Count==0)HubNote(_page==0?"No matches recorded yet. Your first completed match appears here.":"No older matches on this page.");
            string me=Net.CareerStore.LocalPlayerId;
            foreach(var record in _shown)
            {
                if(record==null)continue;
                var line=MatchRecordRules.LineFor(record,me);string place=line!=null?Ordinal(line.Placement):"-";
                var captured=record;
                HubAction("OpenMatchDetail",place+" · "+MenuKit.ModeLabel(record.Mode),line!=null?line.Score+" PTS":"VIEW",
                    ()=>OpenDetail(captured),$"{SceneFlow.PreviewFor(record.MapId).Name} · {record.Rounds} rounds · {record.DurationSeconds/60f:0} min · {Short(record.PlayedUtc)}");
            }
            if(_shown.Count>0 || _page>0)
            {
            var pages=OwnerUiLayout.Rect(_list,"HistoryPages");pages.gameObject.AddComponent<LayoutElement>().preferredHeight=76;
            if(_page>0)OwnerTextAction.Create(pages,"NewerMatches","NEWER",()=>{_page--;_shown.Clear();RefreshMatches();},0,0,430,70,29);
            var label=OwnerUiLayout.Text(pages,"HistoryPage","PAGE "+(_page+1),28,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(label.rectTransform,575,0,409,70);label.alignment=TextAnchor.MiddleCenter;
            if(_shown.Count>=HistoryPageSize)OwnerTextAction.Create(pages,"OlderMatches","OLDER",()=>{_page++;_shown.Clear();RefreshMatches();},1121,0,430,70,29);
            }
            SetFooter(career!=null?"REFRESH":"",career?.Status??"");
        }
        private void OpenDetail(MatchRecord record)
        {
            if(record?.Players==null)return;
            if(_detail==null)
            {
                var cover=OwnerUiLayout.Rect(_root.transform,"MatchDetail");OwnerUiLayout.Fill(cover);_detail=cover.gameObject;
                var dim=cover.gameObject.AddComponent<Image>();var ink=OwnerUiTheme.Current.DeepInk;dim.color=new Color(ink.r,ink.g,ink.b,.82f);
                var design=OwnerUiLayout.DesignArea(cover,"MatchDetailComposition");
                var sheet=OwnerUiLayout.Rect(design,"ScorecardPaper").gameObject.AddComponent<OwnerUiPaper>();sheet.Style=OwnerUiPaper.Treatment.Dialog;
                OwnerUiLayout.Place(sheet.rectTransform,132,100,1656,890);sheet.raycastTarget=true;
                _detailTitle=OwnerUiLayout.Text(design,"MatchDetailTitle","",39,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(_detailTitle.rectTransform,175,137,1554,113);
                _ownerDetailList=OwnerScrollColumn.Build(design,"ScorecardRows",new Rect(181,280,1535,570),out var scroll);
                var layout=_ownerDetailList.GetComponent<VerticalLayoutGroup>();layout.spacing=10;layout.padding.bottom=12;
                OwnerTextAction.Create(design,"CloseMatchDetail","CLOSE",()=>_detail.SetActive(false),685,887,550,74,34);
                InputLayer.ScreenFocus.Install(cover.gameObject);
            }
            foreach(Transform child in _ownerDetailList){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            _detailTitle.text=MenuKit.ModeLabel(record.Mode)+" · "+SceneFlow.PreviewFor(record.MapId).Name+" · "+Short(record.PlayedUtc);
            DetailRow(new[]{"PLACE","PLAYER","POINTS","KNOCKS","THROWS","RETRIEVALS","TAGS","SABOTAGES","PASSIVE DEF."},true);
            var players=new List<PlayerMatchStats>(record.Players);players.Sort((a,b)=>(a?.Placement??9).CompareTo(b?.Placement??9));
            foreach(var player in players)
            {
                if(player==null)continue;
                DetailRow(new[]{Ordinal(player.Placement),string.IsNullOrEmpty(player.Handle)?"P"+(player.Slot+1):player.Handle,
                    player.Score.ToString(),player.Knockdowns.ToString(),player.Throws.ToString(),player.Retrievals.ToString(),player.Tags.ToString(),
                    player.Sabotages.ToString(),MatchRecordRules.PassiveDefenceSeconds(player).ToString("0")+"s"},false);
            }
            if(record.DefenderByRound!=null && record.DefenderByRound.Length>0)
            {
                var names=new List<string>();for(int i=0;i<record.DefenderByRound.Length;i++)names.Add("R"+(i+1)+": P"+(record.DefenderByRound[i]+1));
                var note=OwnerUiLayout.Text(_ownerDetailList,"DefendersByRound","DEFENDER EACH ROUND\n"+string.Join(" · ",names),28);
                note.color=OwnerUiTheme.Current.EnteredInk;note.gameObject.AddComponent<TumpParagraph>();
            }
            _detail.SetActive(true);_detail.GetComponent<InputLayer.ScreenFocus>().Rebuild();
            _ownerDetailList.GetComponentInParent<ScrollRect>().verticalNormalizedPosition=1;
        }
        private void DetailRow(string[] values,bool heading)
        {
            var row=OwnerUiLayout.Rect(_ownerDetailList,heading?"TableHead":"PlayerStats");row.gameObject.AddComponent<LayoutElement>().preferredHeight=heading?64:74;
            float[] widths={112,308,125,148,132,185,100,190,214};float x=0;
            for(int i=0;i<values.Length;i++)
            {
                var cell=OwnerUiLayout.Text(row,"Cell"+i,values[i],heading?28:29,heading?OwnerUiLayout.TypeRole.Accent:OwnerUiLayout.TypeRole.Reading);
                cell.color=heading?OwnerUiTheme.Current.ActionInk:OwnerUiTheme.Current.EnteredInk;
                OwnerUiLayout.Place(cell.rectTransform,x,0,widths[i]-8,heading?60:70);cell.alignment=i==1?TextAnchor.MiddleLeft:TextAnchor.MiddleCenter;x+=widths[i];
            }
        }
    }
}
