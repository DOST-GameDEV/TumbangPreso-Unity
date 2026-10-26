using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpPowerReadout
    {
        private void BuildDetails(Transform root)
        {
            if (Application.isMobilePlatform || InputLayer.TouchHud.ForceVisible) { BuildPhoneDetails(root); return; }
            _detail=OwnerUiLayout.Rect(root,"HeldPowerReference");
            _detail.anchorMin=_detail.anchorMax=_detail.pivot=new Vector2(.5f,0);
            _detail.anchoredPosition=new Vector2(0,248);_detail.sizeDelta=new Vector2(1770,430);
            var canvas=_detail.gameObject.AddComponent<Canvas>();canvas.vertexColorAlwaysGammaSpace=true;
            _detail.gameObject.AddComponent<OwnerUiCanvas>();
            // UI revamp 2026-10-06: the held reference is a brown toy tray holding one cream card
            // per power: its art on a brown tile, the name in the display face, the timing on a
            // honey chip and the full description, every card as tall as the longest one.
            var paper=OwnerUiLayout.Rect(_detail,"ReferencePaper").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Fill(paper.rectTransform);paper.Toy(HudDraw.Brown,HudDraw.BrownSide,10,26,.45f).raycastTarget=false;
            paper.color=new Color(HudDraw.Brown.r,HudDraw.Brown.g,HudDraw.Brown.b,.96f);
            for(int i=0;i<3;i++)
            {
                float x=42+i*573;
                var card=OwnerUiLayout.Rect(_detail,"PowerCard"+i).gameObject.AddComponent<HudCard>();
                card.Toy(HudDraw.Cream,HudDraw.CreamSide,7,18,.3f).raycastTarget=false;
                card.rectTransform.anchorMin=Vector2.zero;card.rectTransform.anchorMax=new Vector2(0,1);card.rectTransform.pivot=new Vector2(0,.5f);
                card.rectTransform.offsetMin=new Vector2(x-16,24);card.rectTransform.offsetMax=new Vector2(x+546,-16);
                var tile=OwnerUiLayout.Rect(_detail,"DetailTile"+i).gameObject.AddComponent<HudCard>();
                tile.Toy(HudDraw.Brown,HudDraw.BrownSide,4,16,0).raycastTarget=false;tile.Sheen=false;
                OwnerUiLayout.Place(tile.rectTransform,x,14,92,92);
                _detailSymbols[i]=OwnerUiLayout.Rect(_detail,"DetailIcon"+i).gameObject.AddComponent<TumpAbilitySymbol>();
                _detailSymbols[i].color=Color.white;_detailSymbols[i].raycastTarget=false;
                OwnerUiLayout.Place(_detailSymbols[i].rectTransform,x+6,20,80,80);
                _names[i]=OwnerUiLayout.Text(_detail,"PowerName"+i,"",34,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(_names[i].rectTransform,x+110,14,424,99);_names[i].color=HudDraw.Brown;
                _timings[i]=OwnerUiLayout.Text(_detail,"PowerTiming"+i,"",28);_timings[i].color=new Color32(176,70,14,255);
                OwnerUiLayout.Place(_timings[i].rectTransform,x+2,123,530,57);
                _bodies[i]=OwnerUiLayout.Text(_detail,"PowerDescription"+i,"",28);_bodies[i].color=new Color32(86,56,38,255);
                _bodies[i].alignment=TextAnchor.UpperLeft;OwnerUiLayout.Place(_bodies[i].rectTransform,x+2,195,530,211);
            }
            _detail.gameObject.AddComponent<PowerReferenceReadingLayout>().Bind(_names, _timings, _bodies);
            _detail.gameObject.SetActive(false);
        }
        private void BuildPhoneDetails(Transform root)
        {
            _detail = OwnerUiLayout.Rect(root, "HeldPowerReference");
            _detail.anchorMin = _detail.anchorMax = _detail.pivot = new Vector2(.5f, .5f);
            _detail.sizeDelta = new Vector2(1770, 720);
            var canvas = _detail.gameObject.AddComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = 310;
            canvas.vertexColorAlwaysGammaSpace = true; _detail.gameObject.AddComponent<OwnerUiCanvas>();
            _detail.gameObject.AddComponent<GraphicRaycaster>();
            var paper = OwnerUiLayout.Rect(_detail, "ReferencePaper").gameObject.AddComponent<HudCard>();
            OwnerUiLayout.Fill(paper.rectTransform); paper.Toy(HudDraw.Brown, HudDraw.BrownSide, 10, 26, .45f).raycastTarget = true;
            var heading = OwnerUiLayout.Text(_detail, "PhonePowerHeading", "YOUR POWERS", 42, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(heading.rectTransform, 32, 8, 700, 70); heading.color = HudDraw.Cream;
            var hint = OwnerUiLayout.Text(_detail, "PhonePowerHint", "SWIPE TO READ", 28);
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = hint.rectTransform.pivot = Vector2.one;
            hint.rectTransform.anchoredPosition = new Vector2(-32, -12); hint.rectTransform.sizeDelta = new Vector2(400, 60);
            hint.alignment = TextAnchor.MiddleRight; hint.color = HudDraw.Cream;
            var list = OwnerScrollColumn.Build(_detail, "PhonePowerList", new Rect(24, 88, 1722, 608), out var scroll);
            var cards = new RectTransform[3];
            for (int i = 0; i < 3; i++)
            {
                var card = OwnerUiLayout.Rect(list, "PowerCard" + i); cards[i] = card;
                card.gameObject.AddComponent<LayoutElement>().preferredHeight = 300;
                var face = card.gameObject.AddComponent<HudCard>(); face.Toy(HudDraw.Cream, HudDraw.CreamSide, 7, 18, .3f).raycastTarget = true;
                var tile = OwnerUiLayout.Rect(card, "DetailTile" + i).gameObject.AddComponent<HudCard>();
                tile.Toy(HudDraw.Brown, HudDraw.BrownSide, 4, 16, 0).raycastTarget = false; tile.Sheen = false;
                OwnerUiLayout.Place(tile.rectTransform, 24, 20, 92, 92);
                _detailSymbols[i] = OwnerUiLayout.Rect(card, "DetailIcon" + i).gameObject.AddComponent<TumpAbilitySymbol>();
                _detailSymbols[i].color = Color.white; _detailSymbols[i].raycastTarget = false;
                OwnerUiLayout.Place(_detailSymbols[i].rectTransform, 30, 26, 80, 80);
                _names[i] = OwnerUiLayout.Text(card, "PowerName" + i, "", 34, OwnerUiLayout.TypeRole.Display); _names[i].color = HudDraw.Brown;
                _timings[i] = OwnerUiLayout.Text(card, "PowerTiming" + i, "", 28); _timings[i].color = new Color32(176, 70, 14, 255);
                _bodies[i] = OwnerUiLayout.Text(card, "PowerDescription" + i, "", 28); _bodies[i].color = new Color32(86, 56, 38, 255);
                _bodies[i].alignment = TextAnchor.UpperLeft;
            }
            _detail.gameObject.AddComponent<PowerReferenceReadingLayout>().BindPhone(_names, _timings, _bodies, cards, scroll);
            _detail.gameObject.SetActive(false);
        }
    }
}
