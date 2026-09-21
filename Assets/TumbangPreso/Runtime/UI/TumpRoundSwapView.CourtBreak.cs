using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpRoundSwapView
    {
        private RectTransform _popup;
        private CanvasGroup _popupFade;
        private Text _nextRound,_notice;
        private readonly CourtPopupGraphic[] _tickets=new CourtPopupGraphic[4];
        private float _entered,_remaining=3;
        private int _nextRoundNumber;
        private bool _popupBuilt;
        public void Build(Transform owner,Action dismiss)
        {
            Canvas=OwnerUiLayout.Canvas(owner,"OwnerRoundSwapCanvas",260);
            var focus=Canvas.GetComponent<InputLayer.ScreenFocus>();if(focus!=null)focus.enabled=false;
            // Passive graphic over the court. It does not open a page or need a click.
            _popup=OwnerUiLayout.Rect(Canvas.transform,"CourtBreakPopup");
            _popup.anchorMin=_popup.anchorMax=new Vector2(.5f,0);_popup.pivot=new Vector2(.5f,0);
            _popup.sizeDelta=new Vector2(1120,238);_popup.anchoredPosition=new Vector2(0,76);
            _popupFade=_popup.gameObject.AddComponent<CanvasGroup>();_popupFade.blocksRaycasts=false;_popupFade.interactable=false;
            Plate(_popup,"HeadlineShadow",8,5,552,103,CourtPresentationPalette.Ink,true);
            Plate(_popup,"HalftimeBrush",0,0,552,100,CourtPresentationPalette.Red,true);
            _round=Label(_popup,"RoundHeadline","HALFTIME",74,CourtPresentationPalette.Paper);
            OwnerUiLayout.Place(_round.rectTransform,33,0,438,100);
            _round.rectTransform.localRotation=Quaternion.Euler(0,0,2);
            var can=OwnerUiLayout.Rect(_popup,"TinCanMark").gameObject.AddComponent<TumpSymbol>();can.Kind=TumpSymbol.Icon.Can;
            can.color=CourtPresentationPalette.Paper;can.raycastTarget=false;OwnerUiLayout.Place(can.rectTransform,478,22,43,55);
            can.rectTransform.localRotation=Quaternion.Euler(0,0,-12);
            Plate(_popup,"NextTayaTicket",553,10,370,82,CourtPresentationPalette.DeepRed);
            var next=Label(_popup,"NextRole","NEXT TAYA",22,CourtPresentationPalette.Gold);
            OwnerUiLayout.Place(next.rectTransform,629,13,270,29);
            _portrait=OwnerUiLayout.Rect(_popup,"NextDefenderPortrait").gameObject.AddComponent<Image>();
            _portrait.preserveAspect=true;_portrait.raycastTarget=false;OwnerUiLayout.Place(_portrait.rectTransform,574,19,47,62);
            _name=Label(_popup,"NextDefenderName","",29,CourtPresentationPalette.Paper);
            OwnerUiLayout.Place(_name.rectTransform,629,42,278,43);
            Plate(_popup,"ReturnTimer",935,10,185,82,CourtPresentationPalette.Ink);
            _nextRound=Label(_popup,"NextRound","ROUND 5",22,CourtPresentationPalette.Paper);
            OwnerUiLayout.Place(_nextRound.rectTransform,953,13,149,28);_nextRound.alignment=TextAnchor.MiddleCenter;
            _buffer=Label(_popup,"WarmupTime","4s",42,CourtPresentationPalette.Gold);
            OwnerUiLayout.Place(_buffer.rectTransform,953,39,149,52);_buffer.alignment=TextAnchor.MiddleCenter;
            for(int i=0;i<4;i++)
            {
                float x=i*282;
                Plate(_popup,"ScoreShadow"+i,x+4,113,273,100,CourtPresentationPalette.Ink);
                _tickets[i]=Plate(_popup,"ScoreTicket"+i,x,108,273,100,CourtPresentationPalette.Paper);
                _portraits[i]=OwnerUiLayout.Rect(_popup,"StandingPortrait"+i).gameObject.AddComponent<Image>();
                _portraits[i].preserveAspect=true;_portraits[i].raycastTarget=false;OwnerUiLayout.Place(_portraits[i].rectTransform,x+16,128,50,64);
                _names[i]=Label(_popup,"StandingName"+i,"",23,CourtPresentationPalette.Ink);
                OwnerUiLayout.Place(_names[i].rectTransform,x+76,115,183,33);
                _names[i].resizeTextForBestFit=true;_names[i].resizeTextMinSize=21;_names[i].resizeTextMaxSize=23;
                _scores[i]=Label(_popup,"StandingScore"+i,"",44,CourtPresentationPalette.Ink);
                OwnerUiLayout.Place(_scores[i].rectTransform,x+75,146,180,55);
            }
            _notice=Label(_popup,"ReplayNotice","",23,CourtPresentationPalette.Paper);
            OwnerUiLayout.Place(_notice.rectTransform,12,210,1096,28);_notice.alignment=TextAnchor.MiddleCenter;
            _popupBuilt=true;
        }
        private static Text Label(Transform parent,string name,string text,int size,Color colour)
        {
            var label=OwnerUiLayout.Text(parent,name,text,size,OwnerUiLayout.TypeRole.Display);
            label.color=colour;label.raycastTarget=false;label.alignment=TextAnchor.MiddleLeft;
            label.horizontalOverflow=HorizontalWrapMode.Overflow;label.verticalOverflow=VerticalWrapMode.Overflow;return label;
        }
        private static CourtPopupGraphic Plate(Transform parent,string name,float x,float y,float width,float height,Color colour,bool brush=false)
        {
            var plate=OwnerUiLayout.Rect(parent,name).gameObject.AddComponent<CourtPopupGraphic>();
            plate.Brush=brush;plate.color=colour;plate.raycastTarget=false;OwnerUiLayout.Place(plate.rectTransform,x,y,width,height);return plate;
        }
        private void LateUpdate()
        {
            if(!_popupBuilt||Canvas==null||!Canvas.gameObject.activeSelf)return;
            float age=Time.unscaledTime-_entered;
            bool reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            float appear=reduced?1:Mathf.Clamp01(age/.28f);
            float settle=1-Mathf.Pow(1-appear,3);
            float exit=Mathf.SmoothStep(0,1,Mathf.Clamp01(_remaining/.2f));
            _popupFade.alpha=settle*exit;
            _popup.anchoredPosition=new Vector2(0,76+(reduced?0:(1-settle)*-24));
            float pop=reduced?1:1+.025f*Mathf.Sin(appear*Mathf.PI)*(1-appear);
            _popup.localScale=Vector3.one*pop;
        }
    }
}
