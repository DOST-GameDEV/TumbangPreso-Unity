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
    /// The owner requested centered standings on 2026-09-30. The ordinary HUD hides
    /// while this card is visible, including non-halftime breaks and late-peer cards.
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
        private const float PopupWidth=1100, PopupHeight=470, PopupTop=0;
        // Owner, October 31: dark cards, with "the highlight or sides blue na di pixelated". Red is the taya, blue everyone else.
        private static readonly Color CardFill=new Color32(30,18,12,245);
        private static readonly Color EdgeBlue=new Color32(72,142,255,255), EdgeRed=new Color32(236,48,56,255);
        private static void Plate(HudCard card,float lean,Color edge)
        {
            card.Glass(CardFill,lean,6,.5f);card.Border=edge;card.BorderWidth=3;card.SoftEdge=3;card.FollowContrast=false;
        }
        private readonly Text[] _ranks=new Text[4];
        private RectTransform _headline;
                // ⚠️ ON THE ARENA THE BREAK IS A SHOW, AND THIS CARD WAS STANDING IN FRONT OF IT (owner,
        // 2026-10-05: "map transformation is so dull, theres no emphasis on it"). That map draws
        // its stage rebuilding itself behind the card (`HalftimePresentation.LiveStageBreak`),
        // and an 1100 by 270 card dead centre covered the can, the hologram and every lock. So
        // on that map's ordinary break, and nowhere else, the same card sits in the lower third
        // at 0.7 of its size. Halftime's package there (replay, standings), and every break on
        // every other map, is untouched: the anchors and the scale below are exactly the old ones
        // whenever `lower` is false. When that map's halftime reaches its show the card goes down too.
        private const float LowerThirdScale=.7f, LowerThirdMargin=30;
        private bool _lowerThird;

        // ⚠️ THE ROUND'S OWN STORY, IN THE STANDINGS (owner, 2026-10-22: "everything has to be animated"; a round summary
        // and rank changes that slide). The card showed the running totals and nothing said who had just earned what, or
        // that anyone had moved. Each chip now carries what that seat earned since the last break, counting up from
        // nothing, and the chips drop in one after another and slide from last break's rank to this one. A separate
        // summary strip would have stood over this same card in the same two seconds; this is the card telling it.
        // THE ROLE SWAP IS THE ONE FACT THAT CHANGES THE NEXT ROUND, so it is the card's beat: the next-taya ticket turns
        // over to show who it is, its can badge stamps down, and the new taya's chip in the standings swells once.
        private HudCard _ticket;
        private RectTransform _tayaBadge;
        private int _nextTayaRank=-1;
        private readonly Text[] _gains=new Text[4];
        private readonly Vector2[] _chipHome=new Vector2[4];
        private readonly float[] _chipFrom=new float[4];
        private readonly int[] _gainOf=new int[4];
        private static readonly int[] LastScores=new int[4];
        private static readonly int[] LastRank={0,1,2,3};
        private static long _lastMatch=-1;
        private static int _gainsRound=-1;
        /// <summary>For tests: what the chip at <paramref name="rank"/> says its seat earned since the last break.</summary>
        public int GainAt(int rank)=>rank>=0&&rank<4?_gainOf[rank]:0;

        public void Build(Transform owner,Action dismiss)
        {
            Canvas=OwnerUiLayout.Canvas(owner,"OwnerRoundSwapCanvas",260);
            var focus=Canvas.GetComponent<InputLayer.ScreenFocus>();if(focus!=null)focus.enabled=false;
            // Passive graphic over the court. It does not open a page or need a click.
            _popup=OwnerUiLayout.Rect(Canvas.transform,"CourtBreakPopup");
            _popup.anchorMin=_popup.anchorMax=new Vector2(.5f,.5f);_popup.pivot=new Vector2(.5f,.5f);
            _popup.sizeDelta=new Vector2(PopupWidth,PopupHeight);_popup.anchoredPosition=new Vector2(0,-PopupTop);
            _popupFade=_popup.gameObject.AddComponent<CanvasGroup>();_popupFade.blocksRaycasts=false;_popupFade.interactable=false;

            // Owner, October 31: "am panget ng visual hierarchy", "put halftime in the middle", "text di mabasa", "check out
            // games like mario". Three reads, in order, as a results screen: the one big word, centred; then who is winning,
            // as big player cards ranked left to right, each with a large portrait, rank and score; then the next taya and the
            // clock. Solid dark cards so every word stays legible over any street.
            var band=OwnerUiLayout.Rect(_popup,"BreakBand").gameObject.AddComponent<Image>();
            band.color=new Color32(18,10,6,185);band.raycastTarget=false;OwnerUiLayout.Place(band.rectTransform,-1400,-30,PopupWidth+2800,PopupHeight+60);
            foreach(float edgeY in new[]{-30f,PopupHeight+27})
            {
                var edge=OwnerUiLayout.Rect(_popup,"BreakBandEdge").gameObject.AddComponent<Image>();
                edge.color=HudDraw.Honey;edge.raycastTarget=false;OwnerUiLayout.Place(edge.rectTransform,-1400,edgeY,PopupWidth+2800,3);
            }

            var headline=Card(_popup,"HalftimePlate",250,0,600,112,HudDraw.Alarm);
            headline.Glass(HudDraw.Alarm,HudDraw.Lean,3,.5f);headline.FollowContrast=false;_headline=headline.rectTransform;
            _round=Label(headline.transform,"RoundHeadline","HALFTIME",84,HudDraw.Ivory);
            OwnerUiLayout.Place(_round.rectTransform,0,0,600,112);_round.alignment=TextAnchor.MiddleCenter;_round.fontStyle=FontStyle.Italic;
            _round.horizontalOverflow=HorizontalWrapMode.Wrap;_round.resizeTextForBestFit=true;_round.resizeTextMinSize=48;_round.resizeTextMaxSize=84;
            var headlineInk=_round.gameObject.AddComponent<Outline>();headlineInk.effectColor=UiTheme.InGameOutline;headlineInk.effectDistance=new Vector2(3,-3);

            for(int i=0;i<4;i++)
            {
                float x=i*(266+12);
                var chip=_chips[i]=Card(_popup,"ScoreTicket"+i,x,128,266,226,CardFill);
                Plate(chip,.06f,EdgeBlue);
                var swatch=_chipSwatches[i]=Card(chip.transform,"StandingSwatch"+i,10,10,246,118,Color.white);
                swatch.ShadowAlpha=0;swatch.FollowContrast=false;swatch.Radius=4;swatch.Slant=.06f;
                // The owner kept this: pale diagonal slats behind the portrait, the wipe's pattern.
                var slats=OwnerUiLayout.Rect(swatch.transform,"StandingSlats");OwnerUiLayout.Place(slats,0,0,246,118);
                slats.gameObject.AddComponent<RectMask2D>();
                for(int s=0;s<7;s++)
                {
                    var slat=OwnerUiLayout.Rect(slats,"StandingSlat").gameObject.AddComponent<Image>();
                    slat.color=new Color(1,1,1,.13f);slat.raycastTarget=false;
                    OwnerUiLayout.Place(slat.rectTransform,-20+s*44,-30,20,178);slat.rectTransform.localRotation=Quaternion.Euler(0,0,-14);
                }
                _portraits[i]=OwnerUiLayout.Rect(swatch.transform,"StandingPortrait"+i).gameObject.AddComponent<Image>();
                _portraits[i].preserveAspect=true;_portraits[i].raycastTarget=false;OwnerUiLayout.Place(_portraits[i].rectTransform,58,0,130,118);
                _ranks[i]=Label(swatch.transform,"StandingRank"+i,(i+1).ToString(),60,HudDraw.Ivory);
                OwnerUiLayout.Place(_ranks[i].rectTransform,14,4,60,70);_ranks[i].fontStyle=FontStyle.Italic;
                var rankInk=_ranks[i].gameObject.AddComponent<Outline>();rankInk.effectColor=UiTheme.InGameOutline;rankInk.effectDistance=new Vector2(3,-3);
                _chipCrowns[i]=OwnerUiLayout.Rect(chip.transform,"StandingCrown"+i).gameObject.AddComponent<HudBadge>();
                OwnerUiLayout.Place(_chipCrowns[i].rectTransform,108,-24,50,38);_chipCrowns[i].raycastTarget=false;
                _chipRoles[i]=OwnerUiLayout.Rect(chip.transform,"StandingRole"+i).gameObject.AddComponent<HudBadge>();
                OwnerUiLayout.Place(_chipRoles[i].rectTransform,204,76,44,44);_chipRoles[i].Detail=UiTheme.Defense;_chipRoles[i].raycastTarget=false;
                _names[i]=Label(chip.transform,"StandingName"+i,"",28,HudDraw.Ivory);
                OwnerUiLayout.Place(_names[i].rectTransform,16,130,234,36);
                _names[i].horizontalOverflow=HorizontalWrapMode.Wrap;_names[i].resizeTextForBestFit=true;_names[i].resizeTextMinSize=20;_names[i].resizeTextMaxSize=28;
                _scores[i]=Label(chip.transform,"StandingScore"+i,"",60,HudDraw.Ivory);
                OwnerUiLayout.Place(_scores[i].rectTransform,96,160,158,62);_scores[i].alignment=TextAnchor.MiddleRight;_scores[i].fontStyle=FontStyle.Italic;
                // What this seat earned since the last break, beside its total.
                _gains[i]=Label(chip.transform,"StandingGain"+i,"",30,HudDraw.Honey);
                OwnerUiLayout.Place(_gains[i].rectTransform,16,172,84,40);_gains[i].fontStyle=FontStyle.Italic;
                var gainInk=_gains[i].gameObject.AddComponent<Outline>();gainInk.effectColor=UiTheme.InGameOutline;gainInk.effectDistance=new Vector2(2,-2);
                _chipHome[i]=chip.rectTransform.anchoredPosition;_chipFrom[i]=_chipHome[i].x;
            }

            var ticket=_ticket=Card(_popup,"NextTayaTicket",180,370,500,88,CardFill);
            Plate(ticket,HudDraw.Lean,EdgeRed);
            _nextSwatch=Card(ticket.transform,"NextTayaSwatch",30,8,72,72,Color.white);_nextSwatch.Slant=HudDraw.Lean;
            _nextSwatch.ShadowAlpha=0;_nextSwatch.FollowContrast=false;_nextSwatch.Radius=4;
            _portrait=OwnerUiLayout.Rect(_nextSwatch.transform,"NextDefenderPortrait").gameObject.AddComponent<Image>();
            _portrait.preserveAspect=true;_portrait.raycastTarget=false;OwnerUiLayout.Place(_portrait.rectTransform,3,3,66,66);
            var badge=OwnerUiLayout.Rect(ticket.transform,"NextTayaBadge").gameObject.AddComponent<HudBadge>();
            OwnerUiLayout.Place(badge.rectTransform,84,54,32,32);badge.Detail=UiTheme.Defense;badge.raycastTarget=false;
            badge.Show(HudBadge.Glyph.Can,HudDraw.Ivory,UiTheme.Defense);_tayaBadge=badge.rectTransform;
            var next=Label(ticket.transform,"NextRole","NEXT TAYA",28,HudDraw.Honey);
            next.gameObject.AddComponent<Outline>().effectColor=UiTheme.InGameOutline;
            OwnerUiLayout.Place(next.rectTransform,128,4,360,38);
            _name=Label(ticket.transform,"NextDefenderName","",36,HudDraw.Ivory);
            OwnerUiLayout.Place(_name.rectTransform,128,38,360,46);
            _name.horizontalOverflow=HorizontalWrapMode.Wrap;_name.resizeTextForBestFit=true;
            _name.resizeTextMinSize=28;_name.resizeTextMaxSize=36;

            var clock=Card(_popup,"ReturnTimer",692,370,228,88,CardFill);
            Plate(clock,HudDraw.Lean,EdgeBlue);
            _returnClock=OwnerUiLayout.Rect(clock.transform,"ReturnClock").gameObject.AddComponent<HudRing>();
            OwnerUiLayout.Place(_returnClock.rectTransform,26,8,72,72);_returnClock.Thickness=6;_returnClock.raycastTarget=false;
            _returnClock.color=HudDraw.Honey;_returnClock.Track=new Color(1,1,1,.14f);
            _buffer=Label(clock.transform,"WarmupTime","4",34,HudDraw.Honey);
            OwnerUiLayout.Place(_buffer.rectTransform,26,8,72,72);_buffer.alignment=TextAnchor.MiddleCenter;
            _nextRound=Label(clock.transform,"NextRound","ROUND 5",28,HudDraw.Ivory);
            OwnerUiLayout.Place(_nextRound.rectTransform,102,20,122,48);_nextRound.alignment=TextAnchor.MiddleCenter;

            _notice=Label(_popup,"ReplayNotice","",28,HudDraw.Ivory);
            OwnerUiLayout.Place(_notice.rectTransform,0,PopupHeight-8,PopupWidth,36);_notice.alignment=TextAnchor.MiddleCenter;
            _notice.gameObject.AddComponent<Outline>().effectColor=UiTheme.InGameOutline;
            _popupBuilt=true;
        }

        /// <summary>
        /// Notes, once per break, what each seat earned since the last one and where it stood then, so the chips can count
        /// it up and slide from the old rank. A late or repeated Show of the same break keeps the first reading.
        /// </summary>
        private void NoteRoundGains(int[] order,MatchDirector match)
        {
            if(!_popupBuilt||match==null)return;
            if(match.PresentationMatchId!=_lastMatch)
            {
                _lastMatch=match.PresentationMatchId;_gainsRound=-1;
                for(int s=0;s<4;s++){LastScores[s]=0;LastRank[s]=s;}
            }
            if(_nextRoundNumber==_gainsRound)return;
            _gainsRound=_nextRoundNumber;
            for(int i=0;i<4;i++)
            {
                int seat=order[i];
                _gainOf[i]=match.ScoreFor(seat)-LastScores[seat];
                int was=Mathf.Clamp(LastRank[seat],0,3);
                _chipFrom[i]=_chipHome[was].x;
            }
            for(int i=0;i<4;i++){LastScores[order[i]]=match.ScoreFor(order[i]);LastRank[order[i]]=i;}
            _nextTayaRank=System.Array.IndexOf(order,Core.MatchRules.DefenderSlotFor(_nextRoundNumber));
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
                _chips[i].Style(CardFill,taya?EdgeRed:EdgeBlue,3,
                    seat==mine?HudDraw.Honey:Color.clear,seat==mine?6:0);
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
            // Arena's halftime ends in the same show: the card goes down to the lower third when it starts.
            bool lower=HalftimePresentation.LiveStageBreak&&(!_halftime||HalftimePresentation.Instance?.StageShowPlaying==true);
            if(lower!=_lowerThird)
            {
                _lowerThird=lower;
                // Mid-card (halftime's show taking over): it comes in again where it now sits.
                if(_halftime&&lower){_entered=Time.unscaledTime;age=0;appear=reduced?1:0;settle=appear;_popupFade.alpha=settle*exit;}
                _popup.anchorMin=_popup.anchorMax=new Vector2(.5f,lower?0:.5f);_popup.pivot=new Vector2(.5f,lower?0:.5f);
            }
            _popup.anchoredPosition=lower?new Vector2(0,LowerThirdMargin-(reduced?0:(1-settle)*24))
                :new Vector2(0,-PopupTop+(reduced?0:(1-settle)*24));
            float pop=reduced?1:1+.025f*Mathf.Sin(appear*Mathf.PI)*(1-appear);
            _popup.localScale=Vector3.one*pop*(lower?LowerThirdScale:1);
            if(_returnClock!=null)_returnClock.Set(_remainingStart>0?_remaining/_remainingStart:0);
            PaintHeadline(age,reduced);
            PaintChipStory(age,reduced);
            PaintRoleSwap(age,reduced);
        }

        /// <summary>The big word slams down oversized and tilted, overshoots and settles, then breathes while it holds.</summary>
        private void PaintHeadline(float age,bool reduced)
        {
            if(_headline==null)return;
            if(reduced){_headline.localScale=Vector3.one;_headline.localRotation=Quaternion.identity;return;}
            const float slam=.13f,settle=.2f;
            float scale,tilt;
            if(age<slam){float u=age/slam;scale=Mathf.Lerp(2.3f,.9f,u*u);tilt=Mathf.Lerp(-10,2,u);}
            else if(age<slam+settle){float k=Mathf.SmoothStep(0,1,(age-slam)/settle);scale=Mathf.Lerp(.9f,1,k);tilt=Mathf.Lerp(2,0,k);}
            else{scale=1+.012f*Mathf.Sin((age-slam-settle)*3.2f);tilt=0;}
            _headline.localScale=Vector3.one*scale;_headline.localRotation=Quaternion.Euler(0,0,tilt);
        }

        /// <summary>The next-taya ticket turns over, its can badge stamps, and that seat's chip swells once.</summary>
        private void PaintRoleSwap(float age,bool reduced)
        {
            if(_ticket==null)return;
            var ticket=_ticket.rectTransform;
            if(reduced){ticket.localScale=Vector3.one;if(_tayaBadge!=null)_tayaBadge.localScale=Vector3.one;return;}
            // Edge-on until a quarter second in, then turns face-up past flat and settles.
            float turn=Mathf.Clamp01((age-.85f)/.3f);
            // Ease out with a little overshoot (back): it turns past face-up and settles.
            float k=turn-1,face=1+2.70158f*k*k*k+1.70158f*k*k;
            ticket.localScale=new Vector3(Mathf.Max(.02f,face),1,1);
            if(_tayaBadge!=null)
            {
                float stamp=Mathf.Clamp01((age-1.15f)/.18f);
                _tayaBadge.localScale=Vector3.one*(stamp<=0?0:Mathf.Lerp(1.8f,1,1-(1-stamp)*(1-stamp)));
                _tayaBadge.localRotation=Quaternion.Euler(0,0,-20*(1-stamp));
            }
            if(_nextTayaRank>=0&&_nextTayaRank<4&&_chips[_nextTayaRank]!=null)
            {
                float swell=Mathf.Clamp01(1-Mathf.Abs(age-1.35f)/.16f);
                _chips[_nextTayaRank].rectTransform.localScale*=1+.08f*swell;
            }
        }

        /// <summary>Chips drop in one after another, slide from last break's rank, and count up what each earned.</summary>
        private void PaintChipStory(float age,bool reduced)
        {
            for(int i=0;i<4;i++)
            {
                if(_chips[i]==null||_gains[i]==null)continue;
                // Each card bounces up into place in rank order, overshooting a little; its rank pops as it lands.
                float land=reduced?1:Mathf.Clamp01((age-.24f-i*.1f)/.32f);
                float back=land-1,e=1+2.70158f*back*back*back+1.70158f*back*back;
                float slide=reduced?1:Mathf.SmoothStep(0,1,Mathf.Clamp01((age-.7f)/.4f));
                var home=_chipHome[i];
                _chips[i].rectTransform.anchoredPosition=new Vector2(Mathf.Lerp(_chipFrom[i],home.x,slide),home.y-(reduced?0:170*(1-e)));
                _chips[i].rectTransform.localScale=Vector3.one*(reduced?1:land<=0?0:Mathf.Lerp(.6f,1,e));
                if(_ranks[i]!=null)_ranks[i].rectTransform.localScale=Vector3.one*(reduced?1:1+.7f*Mathf.Clamp01(1-Mathf.Abs(land-1)*1)*Mathf.Clamp01(1-(age-.56f-i*.1f)/.2f));
                if(_chipCrowns[i]!=null&&!reduced)_chipCrowns[i].rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(age*5)*8);
                int gain=_gainOf[i];
                float count=reduced?1:Mathf.SmoothStep(0,1,Mathf.Clamp01((age-.8f-i*.1f)/.5f));
                int shown=Mathf.RoundToInt(gain*count);
                // Blank until the count begins: a "+0" waiting on the chip read as a score of nothing.
                _gains[i].text=gain==0||shown==0?"":(gain>0?"+":"")+shown;
                var c=gain<0?HudDraw.Alarm:HudDraw.Honey;c.a=e;_gains[i].color=c;
                // The tab punches once as its count lands.
                float punch=reduced?0:Mathf.Clamp01(1-Mathf.Abs(age-(1.3f+i*.1f))/.12f);
                _gains[i].rectTransform.localScale=Vector3.one*(1+.25f*punch);
            }
        }
    }
}
