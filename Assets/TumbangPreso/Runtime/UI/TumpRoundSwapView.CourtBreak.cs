using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The halftime and round-swap popup over the live court (TODO VISUAL-1.17).
    ///
    /// ⚠️⚠️ ONE FAMILY WITH THE MATCH BAR. The popup was drawn in its own hand: skewed tickets,
    /// a crimson "next taya" plate, 22-unit labels (below the 28-unit reading floor, which is
    /// why `TumpNativeHudTests` has been red on NextRole since before this pass) and a leader
    /// written in red, the colour that means "something is wrong". It is now the bar's own
    /// pieces grown: the halftime brush headline, a dark plate for the next taya with the taya
    /// border and can badge the bar's chip wears, a return clock as a draining ring, and the
    /// standings as the bar's cream chips (portrait on the seat colour, seat tag, score, crown
    /// for a unique leader, gold underline for you). The one highlighted fact is the next taya.
    ///
    /// ⚠️ UPPER THIRD, NOT BOTTOM CENTRE. It sat over the first-person arms and the held
    /// slipper, the one part of the frame that is already full. The match bar is hidden while
    /// it shows (`TumpMatchReadout.Tick`), so the popup takes the bar's place at the top and
    /// the court stays visible underneath it.
    /// </summary>
    public sealed partial class TumpRoundSwapView
    {
        private RectTransform _popup;
        private CanvasGroup _popupFade;
        private Text _nextRound,_notice;
        private readonly HudCard[] _chips=new HudCard[4];
        private readonly HudCard[] _chipSwatches=new HudCard[4];
        private readonly HudBadge[] _chipCrowns=new HudBadge[4], _chipRoles=new HudBadge[4];
        private HudCard _nextSwatch;
        private HudRing _returnClock;
        private float _entered,_remaining=3,_remainingStart;
        private int _nextRoundNumber;
        private bool _popupBuilt;
        private const float PopupWidth=1100, PopupTop=28;

        public void Build(Transform owner,Action dismiss)
        {
            Canvas=OwnerUiLayout.Canvas(owner,"OwnerRoundSwapCanvas",260);
            var focus=Canvas.GetComponent<InputLayer.ScreenFocus>();if(focus!=null)focus.enabled=false;
            // Passive graphic over the court. It does not open a page or need a click.
            _popup=OwnerUiLayout.Rect(Canvas.transform,"CourtBreakPopup");
            _popup.anchorMin=_popup.anchorMax=new Vector2(.5f,1);_popup.pivot=new Vector2(.5f,1);
            _popup.sizeDelta=new Vector2(PopupWidth,270);_popup.anchoredPosition=new Vector2(0,-PopupTop);
            _popupFade=_popup.gameObject.AddComponent<CanvasGroup>();_popupFade.blocksRaycasts=false;_popupFade.interactable=false;

            // Row one: the headline brush, the next taya, the return clock.
            Brush(_popup,"HeadlineShadow",6,5,430,96,CourtPresentationPalette.Ink);
            Brush(_popup,"HalftimeBrush",0,0,430,96,CourtPresentationPalette.Red);
            _round=Label(_popup,"RoundHeadline","HALFTIME",64,CourtPresentationPalette.Paper);
            OwnerUiLayout.Place(_round.rectTransform,30,0,380,96);_round.rectTransform.localRotation=Quaternion.Euler(0,0,2);

            var ticket=Card(_popup,"NextTayaTicket",446,0,412,96,HudDraw.Plate);
            ticket.Border=UiTheme.Defense;ticket.BorderWidth=3.5f;
            _nextSwatch=Card(ticket.transform,"NextTayaSwatch",12,12,72,72,Color.white);
            _nextSwatch.ShadowAlpha=0;_nextSwatch.FollowContrast=false;_nextSwatch.Radius=12;
            _portrait=OwnerUiLayout.Rect(_nextSwatch.transform,"NextDefenderPortrait").gameObject.AddComponent<Image>();
            _portrait.preserveAspect=true;_portrait.raycastTarget=false;OwnerUiLayout.Place(_portrait.rectTransform,3,3,66,66);
            var badge=OwnerUiLayout.Rect(ticket.transform,"NextTayaBadge").gameObject.AddComponent<HudBadge>();
            OwnerUiLayout.Place(badge.rectTransform,62,58,32,32);badge.Detail=UiTheme.Defense;badge.raycastTarget=false;
            badge.Show(HudBadge.Glyph.Can,CourtPresentationPalette.Paper,UiTheme.Defense);
            var next=Label(ticket.transform,"NextRole","NEXT TAYA",28,CourtPresentationPalette.Gold);
            OwnerUiLayout.Place(next.rectTransform,100,6,300,40);
            _name=Label(ticket.transform,"NextDefenderName","",32,CourtPresentationPalette.Paper);
            OwnerUiLayout.Place(_name.rectTransform,100,44,300,46);
            // A long name shrinks to the reading floor rather than running into the clock.
            _name.horizontalOverflow=HorizontalWrapMode.Wrap;_name.resizeTextForBestFit=true;
            _name.resizeTextMinSize=28;_name.resizeTextMaxSize=32;

            var clock=Card(_popup,"ReturnTimer",870,0,230,96,HudDraw.Plate);
            _returnClock=OwnerUiLayout.Rect(clock.transform,"ReturnClock").gameObject.AddComponent<HudRing>();
            OwnerUiLayout.Place(_returnClock.rectTransform,10,10,76,76);_returnClock.Thickness=6;_returnClock.raycastTarget=false;
            _returnClock.color=CourtPresentationPalette.Gold;_returnClock.Track=new Color(1,1,1,.14f);
            _buffer=Label(clock.transform,"WarmupTime","4",34,CourtPresentationPalette.Gold);
            OwnerUiLayout.Place(_buffer.rectTransform,10,10,76,76);_buffer.alignment=TextAnchor.MiddleCenter;
            _nextRound=Label(clock.transform,"NextRound","ROUND 5",28,CourtPresentationPalette.Paper);
            OwnerUiLayout.Place(_nextRound.rectTransform,94,24,130,48);_nextRound.alignment=TextAnchor.MiddleCenter;

            // Row two: the standings, as the match bar's chips grown.
            for(int i=0;i<4;i++)
            {
                float x=i*(266+12);
                var chip=_chips[i]=Card(_popup,"ScoreTicket"+i,x,112,266,100,CourtPresentationPalette.Paper);chip.Radius=16;
                var swatch=_chipSwatches[i]=Card(chip.transform,"StandingSwatch"+i,12,12,76,76,Color.white);
                swatch.ShadowAlpha=0;swatch.FollowContrast=false;swatch.Radius=12;
                _portraits[i]=OwnerUiLayout.Rect(swatch.transform,"StandingPortrait"+i).gameObject.AddComponent<Image>();
                _portraits[i].preserveAspect=true;_portraits[i].raycastTarget=false;OwnerUiLayout.Place(_portraits[i].rectTransform,3,3,70,70);
                _chipCrowns[i]=OwnerUiLayout.Rect(chip.transform,"StandingCrown"+i).gameObject.AddComponent<HudBadge>();
                OwnerUiLayout.Place(_chipCrowns[i].rectTransform,34,-16,34,26);_chipCrowns[i].raycastTarget=false;
                _chipRoles[i]=OwnerUiLayout.Rect(chip.transform,"StandingRole"+i).gameObject.AddComponent<HudBadge>();
                OwnerUiLayout.Place(_chipRoles[i].rectTransform,64,62,32,32);_chipRoles[i].Detail=UiTheme.Defense;_chipRoles[i].raycastTarget=false;
                _names[i]=Label(chip.transform,"StandingName"+i,"",28,HudDraw.CardInk);
                OwnerUiLayout.Place(_names[i].rectTransform,100,8,154,38);
                _scores[i]=Label(chip.transform,"StandingScore"+i,"",44,HudDraw.CardInk);
                OwnerUiLayout.Place(_scores[i].rectTransform,100,40,154,56);_scores[i].alignment=TextAnchor.MiddleRight;
            }
            _notice=Label(_popup,"ReplayNotice","",28,CourtPresentationPalette.Paper);
            OwnerUiLayout.Place(_notice.rectTransform,0,222,PopupWidth,40);_notice.alignment=TextAnchor.MiddleCenter;
            _notice.gameObject.AddComponent<Outline>().effectColor=UiTheme.InGameOutline;
            _popupBuilt=true;
        }

        /// <summary>Paints the chips for this break: seat colours, crown, taya badge, your mark.</summary>
        private void PaintChips(int[] order,int nextTaya,bool uniqueLeader)
        {
            if(!_popupBuilt)return;
            int mine=LocalSeat();
            _nextSwatch.color=PlayerIdentity.Colour(nextTaya);
            for(int i=0;i<4;i++)
            {
                int seat=order[i];bool taya=seat==nextTaya;
                _chipSwatches[i].color=PlayerIdentity.Colour(seat);
                _chips[i].Style(CourtPresentationPalette.Paper,taya?UiTheme.Defense:Color.clear,taya?3.5f:0,
                    seat==mine?CourtPresentationPalette.Gold:Color.clear,seat==mine?4:0);
                _chipCrowns[i].Show(i==0&&uniqueLeader?HudBadge.Glyph.Crown:HudBadge.Glyph.None,CourtPresentationPalette.Gold,Color.clear);
                _chipRoles[i].Show(taya?HudBadge.Glyph.Can:HudBadge.Glyph.None,CourtPresentationPalette.Paper,UiTheme.Defense);
            }
        }

        /// <summary>The seat this screen belongs to: the network's local seat online, the one
        /// human seat offline, or none for an all-bot run.</summary>
        private static int LocalSeat()
        {
            if(NetAuthority.IsNetworked)return NetAuthority.LocalSlot;
            var round=GameServices.Round;if(round==null)return -1;
            foreach(var player in round.Players)if(player!=null&&!player.IsBot)return player.PlayerSlot;
            return -1;
        }
        private static Text Label(Transform parent,string name,string text,int size,Color colour)
        {
            var label=OwnerUiLayout.Text(parent,name,text,size,OwnerUiLayout.TypeRole.Display);
            label.color=colour;label.raycastTarget=false;label.alignment=TextAnchor.MiddleLeft;
            label.horizontalOverflow=HorizontalWrapMode.Overflow;label.verticalOverflow=VerticalWrapMode.Overflow;return label;
        }
        private static CourtPopupGraphic Brush(Transform parent,string name,float x,float y,float width,float height,Color colour)
        {
            var plate=OwnerUiLayout.Rect(parent,name).gameObject.AddComponent<CourtPopupGraphic>();
            plate.Brush=true;plate.color=colour;plate.raycastTarget=false;OwnerUiLayout.Place(plate.rectTransform,x,y,width,height);return plate;
        }
        private static HudCard Card(Transform parent,string name,float x,float y,float width,float height,Color colour)
        {
            var card=OwnerUiLayout.Rect(parent,name).gameObject.AddComponent<HudCard>();
            card.color=colour;card.Radius=14;card.raycastTarget=false;OwnerUiLayout.Place(card.rectTransform,x,y,width,height);return card;
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
            _popup.anchoredPosition=new Vector2(0,-PopupTop+(reduced?0:(1-settle)*24));
            float pop=reduced?1:1+.025f*Mathf.Sin(appear*Mathf.PI)*(1-appear);
            _popup.localScale=Vector3.one*pop;
            if(_returnClock!=null)_returnClock.Set(_remainingStart>0?_remaining/_remainingStart:0);
        }
    }
}
