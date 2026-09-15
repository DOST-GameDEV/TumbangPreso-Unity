using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpRoundSwapView
    {
        public void Build(Transform owner,Action dismiss)
        {
            Canvas=OwnerUiLayout.Canvas(owner,"OwnerRoundSwapCanvas",220);
            if(Hud.Instance!=null){Canvas.transform.SetParent(Hud.Instance.CleanFeedRoot,false);OwnerUiLayout.Fill((RectTransform)Canvas.transform);}
            var shade=OwnerUiLayout.Rect(Canvas.transform,"CourtBreakShade").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(shade.rectTransform);shade.color=new Color32(17,29,27,220);shade.raycastTarget=false;
            var root=OwnerUiLayout.DesignArea(Canvas.transform,"RoundComposition");
            _round=OwnerUiLayout.Text(root,"RoundHeadline","",72,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_round.rectTransform,98,52,1715,123);_round.color=new Color32(245,218,169,255);
            var next=OwnerUiLayout.Text(root,"NextRole","NEXT DEFENDER",31,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(next.rectTransform,98,257,668,75);next.color=new Color32(198,218,135,255);
            _portrait=OwnerUiLayout.Rect(root,"NextDefenderPortrait").gameObject.AddComponent<Image>();
            _portrait.preserveAspect=true;_portrait.raycastTarget=false;OwnerUiLayout.Place(_portrait.rectTransform,119,352,544,390);
            _name=OwnerUiLayout.Text(root,"NextDefenderName","",49,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_name.rectTransform,98,766,697,104);_name.color=new Color32(247,235,210,255);
            var divider=OwnerUiLayout.Rect(root,"BreakDivider").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(divider.rectTransform,831,271,2,587);divider.color=new Color32(196,206,179,80);divider.raycastTarget=false;
            var title=OwnerUiLayout.Text(root,"StandingsTitle","SCORE SO FAR",31,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform,903,257,877,75);title.color=new Color32(198,218,135,255);
            for(int i=0;i<4;i++)
            {
                float y=364+i*122;
                var place=OwnerUiLayout.Text(root,"Place"+i,(i+1).ToString(),38,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(place.rectTransform,905,y,68,86);place.color=new Color32(188,195,174,255);
                _portraits[i]=OwnerUiLayout.Rect(root,"StandingPortrait"+i).gameObject.AddComponent<Image>();
                _portraits[i].preserveAspect=true;_portraits[i].raycastTarget=false;OwnerUiLayout.Place(_portraits[i].rectTransform,993,y,93,93);
                _names[i]=OwnerUiLayout.Text(root,"StandingName"+i,"",36,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(_names[i].rectTransform,1125,y,452,87);_names[i].color=new Color32(247,235,210,255);
                _scores[i]=OwnerUiLayout.Text(root,"StandingScore"+i,"",42,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(_scores[i].rectTransform,1608,y,184,87);_scores[i].alignment=TextAnchor.MiddleRight;_scores[i].color=new Color32(245,218,169,255);
            }
            var go=OwnerTextAction.Create(root,"ContinueWarmup","KEEP WARMING UP",dismiss,1322,949,476,88,31);
            go.GetComponentInChildren<Text>().color=new Color32(198,218,135,255);
            _buffer=OwnerUiLayout.Text(root,"WarmupTime","",29);_buffer.color=new Color32(227,224,207,255);
            OwnerUiLayout.Place(_buffer.rectTransform,104,956,1158,75);
        }
    }
}
