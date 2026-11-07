using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpPowerReadout
    {
        private void BuildDetails(Transform root)
        {
            if (Application.isMobilePlatform || InputLayer.TouchHud.ForceVisible) { BuildPhoneDetails(root); return; }
            BuildPlates(root); return;
        }
        private void BuildPanelDetails(Transform root)
        {
            _detail=OwnerUiLayout.Rect(root,"HeldPowerReference");
            _detail.anchorMin=_detail.anchorMax=_detail.pivot=new Vector2(.5f,0);
            _detail.anchoredPosition=new Vector2(0,248);_detail.sizeDelta=new Vector2(ReferenceWidth,560);
            var canvas=_detail.gameObject.AddComponent<Canvas>();canvas.vertexColorAlwaysGammaSpace=true;
            _detail.gameObject.AddComponent<OwnerUiCanvas>();
            // Owner, 2026-11-05, with a drawn layout: a maroon plate in a pink rim; a chevron tab naming the page
            // (ATTACKING / DEFENDING) over a rule, the page key top right; three columns split by rules with a diamond,
            // each the power's mark on a sunburst card, its name, an outlined chip with its key icon and timings, and
            // an outlined EFFECT box holding the sentence with key icons set in it. Taller than the old tray on purpose.
            var bold=Hub.HubKnit.SentenceFont(true);
            Shape(_detail,"ReferencePlate",0,0,0,0,ReferenceShape.Form.Box,ReferenceMaroon,ReferencePink,6,30,true);
            Shape(_detail,"PageTab",6,6,470,96,ReferenceShape.Form.Tab,ReferenceDeep,ReferencePink,4,0);
            Shape(_detail,"PageRule",476,100,ReferenceWidth-476-40,4,ReferenceShape.Form.Box,ReferencePink,Color.clear,0,2);
            _pageTitle=Hub.HubKnit.Display(_detail,"PowerPageTitle","",62,Hub.HubKnit.KitLime,TextAnchor.MiddleLeft);
            Hub.HubKnit.In(_pageTitle.rectTransform,40,8,400,92);
            _pageHint=Inline(_detail,"PowerPageHint",0,30,bold,28,Hub.HubKnit.KitCream);
            for(int i=0;i<3;i++)
            {
                float x=ReferencePad+i*(ColumnWidth+ColumnGap);
                if(i>0)
                {
                    float rule=x-ColumnGap*.5f;
                    _dividers[i-1]=Shape(_detail,"ColumnRule"+i,rule-1.5f,TileTop,3,300,ReferenceShape.Form.Box,ReferencePinkSoft,Color.clear,0,1);
                    Shape(_detail,"ColumnDiamond"+i,rule-11,EffectTop-11,22,22,ReferenceShape.Form.Diamond,ReferenceMaroon,ReferencePink,3,0);
                }
                // A round sunburst badge in a pink ring (owner, 2026-11-05: "a circle like this instead").
                var tile=Shape(_detail,"DetailTile"+i,x,TileTop,150,150,ReferenceShape.Form.Badge,Color.white,ReferencePink,7,0);
                _detailSymbols[i]=Hub.HubKit.Rect(tile,"DetailIcon"+i).gameObject.AddComponent<TumpAbilitySymbol>();
                _detailSymbols[i].color=Color.white;_detailSymbols[i].raycastTarget=false;
                Hub.HubKit.Stretch(_detailSymbols[i].rectTransform,24);
                float side=x+174,sideWidth=ColumnWidth-174;
                _names[i]=Hub.HubKnit.Display(_detail,"PowerName"+i,"",52,Color.white,TextAnchor.MiddleLeft);
                _names[i].horizontalOverflow=HorizontalWrapMode.Wrap;_names[i].verticalOverflow=VerticalWrapMode.Truncate;
                _names[i].resizeTextForBestFit=true;_names[i].resizeTextMinSize=28;_names[i].resizeTextMaxSize=52;
                // 62 tall: two lines never fit at the floor size, so a long name shrinks onto one line instead of wrapping.
                Hub.HubKnit.In(_names[i].rectTransform,side,TileTop+8,sideWidth,62);
                Shape(_detail,"PowerChip"+i,side,TileTop+82,sideWidth,62,ReferenceShape.Form.Box,ReferenceDeep,ReferencePink,3,16);
                _inlineCosts[i]=Inline(_detail,"PowerChipWords"+i,side+16,TileTop+96,bold,26,Hub.HubKnit.KitCream);
                _inlineCosts[i].Width=sideWidth-32;_inlineCosts[i].SingleLineFloor=16;
                _effectBoxes[i]=Shape(_detail,"EffectBox"+i,x,EffectTop,ColumnWidth,160,ReferenceShape.Form.Box,ReferenceDeep,ReferencePink,3,20);
                _inlineBodies[i]=Inline(_detail,"PowerSentence"+i,x+28,EffectTop+26,bold,30,Hub.HubKnit.KitCream);
                _inlineBodies[i].Width=ColumnWidth-56;_inlineBodies[i].Rebuilt=LayoutReference;
                // The plain words behind each picture line, for tests and anything that reads the panel as text.
                _timings[i]=Hub.HubKnit.Words(_detail,"PowerTiming"+i,"",26,Hub.HubKnit.KitCream);
                _bodies[i]=Hub.HubKnit.Words(_detail,"PowerDescription"+i,"",28,Hub.HubKnit.KitCream);
                _timings[i].gameObject.SetActive(false);_bodies[i].gameObject.SetActive(false);
            }
            _pageHint.Rebuilt=LayoutReference;
            _detail.gameObject.SetActive(false);
        }
        private const float ReferenceWidth=1770,ReferencePad=40,ColumnGap=64,ColumnWidth=(ReferenceWidth-2*ReferencePad-2*ColumnGap)/3,
                            TileTop=132,EffectTop=310;
        private static readonly Color ReferenceMaroon=new Color32(58,14,28,246),ReferenceDeep=new Color32(40,8,18,255),
                                      ReferencePink=new Color32(228,106,134,255),ReferencePinkSoft=new Color32(228,106,134,150);
        private readonly InlinePromptText[] _inlineBodies=new InlinePromptText[3],_inlineCosts=new InlinePromptText[3];
        private readonly RectTransform[] _effectBoxes=new RectTransform[3],_dividers=new RectTransform[2];

        private static RectTransform Shape(RectTransform parent,string name,float x,float y,float w,float h,ReferenceShape.Form form,
                                           Color fill,Color border,float width,float radius,bool stretch=false)
        {
            var rect=Hub.HubKit.Rect(parent,name);
            if(stretch)Hub.HubKit.Stretch(rect);else Hub.HubKnit.In(rect,x,y,w,h);
            rect.gameObject.AddComponent<ReferenceShape>().Set(form,fill,border,width,radius);
            return rect;
        }

        private static InlinePromptText Inline(RectTransform parent,string name,float x,float y,Font font,int size,Color colour)
        {
            var rect=Hub.HubKnit.In(Hub.HubKit.Rect(parent,name),x,y,10,10);
            var inline=rect.gameObject.AddComponent<InlinePromptText>();
            inline.Font=font;inline.Size=size;inline.Colour=colour;inline.KeyColour=Hub.HubKnit.KitLime;
            return inline;
        }

        /// <summary>Sizes the EFFECT boxes, the column rules and the plate to the longest sentence, and sets the page key top right.</summary>
        private void LayoutReference()
        {
            if(_plates){LayoutPlates();return;}
            if(_inlineBodies[0]==null)return;
            float body=0;
            for(int i=0;i<3;i++)body=Mathf.Max(body,_inlineBodies[i].Height);
            float box=Mathf.Max(140,body+52);
            for(int i=0;i<3;i++)_effectBoxes[i].sizeDelta=new Vector2(ColumnWidth,box);
            foreach(var rule in _dividers)rule.sizeDelta=new Vector2(3,EffectTop+box-TileTop);
            for(int i=0;i<3;i++)
            {
                var chip=_inlineCosts[i];
                chip.GetComponent<RectTransform>().anchoredPosition=new Vector2(ReferencePad+i*(ColumnWidth+ColumnGap)+174+16,-(TileTop+82+(62-chip.Height)*.5f));
            }
            _pageHint.GetComponent<RectTransform>().anchoredPosition=new Vector2(ReferenceWidth-44-_pageHint.UsedWidth,-30);
            _detail.sizeDelta=new Vector2(ReferenceWidth,EffectTop+box+40);
            _detail.anchoredPosition=new Vector2(0,Settings.SettingsStore.Current.LargerText?280:248);
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
