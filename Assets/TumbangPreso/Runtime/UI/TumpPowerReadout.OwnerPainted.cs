using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpPowerReadout
    {
        private void BuildDetails(Transform root)
        {
            _detail=OwnerUiLayout.Rect(root,"HeldPowerReference");
            _detail.anchorMin=_detail.anchorMax=_detail.pivot=new Vector2(.5f,0);
            _detail.anchoredPosition=new Vector2(0,230);_detail.sizeDelta=new Vector2(1770,485);
            var canvas=_detail.gameObject.AddComponent<Canvas>();canvas.vertexColorAlwaysGammaSpace=true;
            _detail.gameObject.AddComponent<OwnerUiCanvas>();
            var paper=OwnerUiLayout.Rect(_detail,"ReferencePaper").gameObject.AddComponent<OwnerUiPaper>();
            OwnerUiLayout.Fill(paper.rectTransform);paper.raycastTarget=false;
            for(int i=0;i<3;i++)
            {
                float x=42+i*573;
                _detailSymbols[i]=OwnerUiLayout.Rect(_detail,"DetailIcon"+i).gameObject.AddComponent<TumpAbilitySymbol>();
                _detailSymbols[i].color=OwnerUiTheme.Current.ActionInk;_detailSymbols[i].raycastTarget=false;
                OwnerUiLayout.Place(_detailSymbols[i].rectTransform,x,32,86,86);
                _names[i]=OwnerUiLayout.Text(_detail,"PowerName"+i,"",35,OwnerUiLayout.TypeRole.Accent);
                OwnerUiLayout.Place(_names[i].rectTransform,x+110,22,424,107);
                _timings[i]=OwnerUiLayout.Text(_detail,"PowerTiming"+i,"",26);_timings[i].color=OwnerUiTheme.Current.Green;
                OwnerUiLayout.Place(_timings[i].rectTransform,x+2,149,530,63);
                _bodies[i]=OwnerUiLayout.Text(_detail,"PowerDescription"+i,"",28);_bodies[i].color=OwnerUiTheme.Current.EnteredInk;
                _bodies[i].alignment=TextAnchor.UpperLeft;OwnerUiLayout.Place(_bodies[i].rectTransform,x+2,226,530,225);
                if(i>0)
                {
                    var rule=OwnerUiLayout.Rect(_detail,"ColumnRule").gameObject.AddComponent<Image>();
                    rule.color=OwnerUiTheme.Current.Peach;rule.raycastTarget=false;OwnerUiLayout.Place(rule.rectTransform,x-22,34,3,418);
                }
            }
            _detail.gameObject.SetActive(false);
        }
    }
}
