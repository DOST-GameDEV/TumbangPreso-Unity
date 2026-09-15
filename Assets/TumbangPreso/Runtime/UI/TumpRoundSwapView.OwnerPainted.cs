using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpRoundSwapView
    {
        private void BuildPreviousOwnerBreak(Transform owner,Action dismiss)
        {
            Canvas=OwnerUiLayout.Canvas(owner,"OwnerRoundSwapCanvas",220);
            if(Hud.Instance!=null){Canvas.transform.SetParent(Hud.Instance.CleanFeedRoot,false);OwnerUiLayout.Fill((RectTransform)Canvas.transform);}
            OwnerUiBackdrop.Build(Canvas.transform);
            var root=OwnerUiLayout.DesignArea(Canvas.transform,"RoundComposition");
            _round=OwnerUiLayout.Text(root,"RoundHeadline","",63,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_round.rectTransform,95,50,1725,108);
            var next=OwnerUiLayout.Text(root,"NextRole","NEXT DEFENDER",36,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(next.rectTransform,123,233,695,76);
            var mat=OwnerUiLayout.Rect(root,"DefenderMat").gameObject.AddComponent<OwnerPreviewMat>();
            OwnerUiLayout.Place(mat.rectTransform,107,348,650,431);mat.raycastTarget=false;
            _portrait=OwnerUiLayout.Rect(root,"NextDefenderPortrait").gameObject.AddComponent<Image>();
            _portrait.preserveAspect=true;_portrait.raycastTarget=false;OwnerUiLayout.Place(_portrait.rectTransform,149,308,546,483);
            _name=OwnerUiLayout.Text(root,"NextDefenderName","",49,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_name.rectTransform,118,822,698,92);_name.alignment=TextAnchor.MiddleCenter;
            var sheet=OwnerUiLayout.Rect(root,"StandingsPaper").gameObject.AddComponent<OwnerUiPaper>();
            OwnerUiLayout.Place(sheet.rectTransform,877,226,949,625);sheet.raycastTarget=false;
            var title=OwnerUiLayout.Text(root,"StandingsTitle","STANDINGS",39,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(title.rectTransform,925,249,847,70);
            for(int i=0;i<4;i++)
            {
                float y=336+i*118;
                var place=OwnerUiLayout.Text(root,"Place"+i,(i+1).ToString(),44,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(place.rectTransform,915,y,73,81);
                _portraits[i]=OwnerUiLayout.Rect(root,"StandingPortrait"+i).gameObject.AddComponent<Image>();
                _portraits[i].preserveAspect=true;_portraits[i].raycastTarget=false;OwnerUiLayout.Place(_portraits[i].rectTransform,1002,y-3,92,92);
                _names[i]=OwnerUiLayout.Text(root,"StandingName"+i,"",35,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(_names[i].rectTransform,1128,y,427,82);
                _scores[i]=OwnerUiLayout.Text(root,"StandingScore"+i,"",43,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(_scores[i].rectTransform,1585,y,197,82);_scores[i].alignment=TextAnchor.MiddleRight;
            }
            var go=OwnerPaintedAction.Create(root,"ContinueWarmup","KEEP WARMING UP",dismiss,false,31);
            OwnerUiLayout.Place((RectTransform)go.transform,1390,949,413,91);
            _buffer=OwnerUiLayout.Text(root,"WarmupTime","",29);_buffer.color=OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(_buffer.rectTransform,104,956,1190,74);
        }
    }
}
