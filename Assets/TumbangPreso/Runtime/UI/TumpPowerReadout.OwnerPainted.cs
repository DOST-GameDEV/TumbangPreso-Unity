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
            _detail.anchoredPosition=new Vector2(0,248);_detail.sizeDelta=new Vector2(1770,430);
            var canvas=_detail.gameObject.AddComponent<Canvas>();canvas.vertexColorAlwaysGammaSpace=true;
            _detail.gameObject.AddComponent<OwnerUiCanvas>();
            var paper=OwnerUiLayout.Rect(_detail,"ReferencePaper").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(paper.rectTransform);paper.color=new Color32(35,29,33,242);paper.raycastTarget=false;
            for(int i=0;i<3;i++)
            {
                float x=42+i*573;
                _detailSymbols[i]=OwnerUiLayout.Rect(_detail,"DetailIcon"+i).gameObject.AddComponent<TumpAbilitySymbol>();
                _detailSymbols[i].color=CourtPresentationPalette.Gold;_detailSymbols[i].raycastTarget=false;
                OwnerUiLayout.Place(_detailSymbols[i].rectTransform,x,19,86,86);
                _names[i]=OwnerUiLayout.Text(_detail,"PowerName"+i,"",34,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(_names[i].rectTransform,x+110,14,424,99);_names[i].color=OwnerUiTheme.Current.Pale;
                _timings[i]=OwnerUiLayout.Text(_detail,"PowerTiming"+i,"",28);_timings[i].color=OwnerUiTheme.Current.Orange;
                OwnerUiLayout.Place(_timings[i].rectTransform,x+2,123,530,57);
                _bodies[i]=OwnerUiLayout.Text(_detail,"PowerDescription"+i,"",28);_bodies[i].color=OwnerUiTheme.Current.Pale;
                _bodies[i].alignment=TextAnchor.UpperLeft;OwnerUiLayout.Place(_bodies[i].rectTransform,x+2,195,530,211);
                if(i>0)
                {
                    var rule=OwnerUiLayout.Rect(_detail,"ColumnRule").gameObject.AddComponent<Image>();
                    rule.color=OwnerUiTheme.Current.Peach;rule.raycastTarget=false;
                    rule.rectTransform.anchorMin=Vector2.zero;rule.rectTransform.anchorMax=new Vector2(0,1);
                    rule.rectTransform.offsetMin=new Vector2(x-22,26);rule.rectTransform.offsetMax=new Vector2(x-20,-26);
                }
            }
            _detail.gameObject.AddComponent<PowerReferenceReadingLayout>().Bind(_names, _timings, _bodies);
            _detail.gameObject.SetActive(false);
        }
    }
}
