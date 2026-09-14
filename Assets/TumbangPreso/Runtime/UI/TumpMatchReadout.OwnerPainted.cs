using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class TumpMatchReadout
    {
        public void Build(Transform owner)
        {
            Canvas=OwnerUiLayout.Canvas(owner,"OwnerMatchCanvas",100);
            Canvas.GetComponent<InputLayer.ScreenFocus>().enabled=false;_root=(RectTransform)Canvas.transform;
            _effects=gameObject.AddComponent<TumpHudEffects>();_effects.Build(_root);
            BuildScores();BuildClock();BuildCan();BuildPersonal();BuildPrompts();
            _powers=gameObject.AddComponent<TumpPowerReadout>();_powers.Build(_root);
            _toast=Ink(_root,"MatchToast","",38,true);Pin(_toast.rectTransform,new Vector2(.5f,1),new Vector2(0,-204),new Vector2(1040,78));_toast.enabled=false;
            _countdown=Ink(_root,"Countdown","",110,true);Pin(_countdown.rectTransform,new Vector2(.5f,.58f),Vector2.zero,new Vector2(740,180));_countdown.enabled=false;
            _crosshair=Ink(_root,"Reticle","+",34,false);Pin(_crosshair.rectTransform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(76,76));
            _hit=Ink(_root,"HitConfirmation","×",72,true);Pin(_hit.rectTransform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(120,120));_hit.enabled=false;
            _spectator=Ink(_root,"SpectatorReadout","",27,false);Pin(_spectator.rectTransform,new Vector2(.5f,0),new Vector2(0,100),new Vector2(1420,136));
            _sandbox=Ink(_root,"SandboxState","",25,false);Pin(_sandbox.rectTransform,new Vector2(1,0),new Vector2(-250,38),new Vector2(456,54));
            var version=Ink(_root,"GameVersion","",20,false);Pin(version.rectTransform,new Vector2(1,0),new Vector2(-230,14),new Vector2(420,28));GameVersion.ApplyTo(version);
        }
        private static void Pin(RectTransform rect,Vector2 anchor,Vector2 at,Vector2 size)
        {
            rect.anchorMin=rect.anchorMax=anchor;rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=at;rect.sizeDelta=size;
        }
        private static Text Ink(Transform root,string name,string words,int size,bool main)
        {
            var text=OwnerUiLayout.Text(root,name,words,size,main?OwnerUiLayout.TypeRole.Display:OwnerUiLayout.TypeRole.Reading);
            text.color=OwnerUiTheme.Current.Pale;text.alignment=TextAnchor.MiddleCenter;text.verticalOverflow=VerticalWrapMode.Overflow;
            var edge=text.gameObject.AddComponent<Outline>();edge.effectColor=OwnerUiTheme.Current.DeepInk;edge.effectDistance=new Vector2(1.5f,-1.5f);
            return text;
        }
        private void BuildScores()
        {
            _scoreRoot=OwnerUiLayout.Rect(_root,"MatchScores");Pin(_scoreRoot,new Vector2(0,1),new Vector2(274,-168),new Vector2(480,292));
            for(int i=0;i<4;i++)
            {
                var row=OwnerUiLayout.Rect(_scoreRoot,"ScoreRow"+i);OwnerUiLayout.Place(row,0,i*72,480,70);_scoreRows[i]=row;
                row.gameObject.AddComponent<OwnerScoreStrip>().raycastTarget=false;
                _portraits[i]=OwnerPortraitArt.Create(row,"PlayerPortrait","");OwnerUiLayout.Place(_portraits[i].rectTransform,8,3,62,62);
                _names[i]=Ink(row,"PlayerName","",30,true);_names[i].alignment=TextAnchor.MiddleLeft;
                OwnerUiLayout.Place(_names[i].rectTransform,80,0,274,49);
                _scores[i]=Ink(row,"Score","",33,true);_scores[i].alignment=TextAnchor.MiddleRight;
                OwnerUiLayout.Place(_scores[i].rectTransform,358,2,103,56);
                _roles[i]=Ink(row,"RoleState","",24,false);_roles[i].alignment=TextAnchor.MiddleLeft;
                OwnerUiLayout.Place(_roles[i].rectTransform,82,42,370,27);
            }
        }
        private void BuildClock()
        {
            _clockRoot=OwnerUiLayout.Rect(_root,"RoundClock");Pin(_clockRoot,new Vector2(.5f,1),new Vector2(0,-93),new Vector2(620,164));
            var face=OwnerUiLayout.Rect(_clockRoot,"ClockFace").gameObject.AddComponent<OwnerUiPaper>();face.Style=OwnerUiPaper.Treatment.Note;
            OwnerUiLayout.Place(face.rectTransform,151,0,318,102);face.raycastTarget=false;
            _clock=OwnerUiLayout.Text(_clockRoot,"TimeLeft","",61,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_clock.rectTransform,166,2,288,95);_clock.alignment=TextAnchor.MiddleCenter;_clock.verticalOverflow=VerticalWrapMode.Overflow;
            _round=Ink(_clockRoot,"RoundLabel","",27,true);OwnerUiLayout.Place(_round.rectTransform,0,104,620,56);
        }
        private void BuildCan()
        {
            _canRoot=OwnerUiLayout.Rect(_root,"CanReadout");Pin(_canRoot,new Vector2(1,1),new Vector2(-252,-112),new Vector2(464,176));
            var can=OwnerUiLayout.Rect(_canRoot,"CanStateIcon").gameObject.AddComponent<TumpSymbol>();
            can.Kind=TumpSymbol.Icon.Can;can.color=OwnerUiTheme.Current.Pale;can.raycastTarget=false;
            var edge=can.gameObject.AddComponent<Outline>();edge.effectColor=OwnerUiTheme.Current.DeepInk;edge.effectDistance=new Vector2(2,-2);
            OwnerUiLayout.Place(can.rectTransform,0,0,84,99);
            _canState=Ink(_canRoot,"CanState","",34,true);_canState.alignment=TextAnchor.MiddleLeft;OwnerUiLayout.Place(_canState.rectTransform,106,12,348,73);
            _canHint=Ink(_canRoot,"CanHint","",27,false);_canHint.alignment=TextAnchor.UpperRight;OwnerUiLayout.Place(_canHint.rectTransform,0,110,452,66);
        }
        private void BuildPersonal()
        {
            _personalRoot=OwnerUiLayout.Rect(_root,"LocalState");Pin(_personalRoot,new Vector2(0,0),new Vector2(264,110),new Vector2(464,174));
            _role=Ink(_personalRoot,"LocalRole","",35,true);_role.alignment=TextAnchor.MiddleLeft;OwnerUiLayout.Place(_role.rectTransform,0,0,464,64);
            _stock=Ink(_personalRoot,"SlipperState","",27,false);_stock.alignment=TextAnchor.MiddleLeft;OwnerUiLayout.Place(_stock.rectTransform,0,64,464,54);
            var track=OwnerUiLayout.Rect(_personalRoot,"StaminaTrack").gameObject.AddComponent<Image>();track.color=OwnerUiTheme.Current.DeepInk;track.raycastTarget=false;
            OwnerUiLayout.Place(track.rectTransform,0,134,326,13);
            _stamina=OwnerUiLayout.Rect(track.transform,"StaminaFill").gameObject.AddComponent<Image>();_stamina.color=OwnerUiTheme.Current.Lime;_stamina.raycastTarget=false;
            OwnerUiLayout.Fill(_stamina.rectTransform);_stamina.rectTransform.offsetMin=new Vector2(2,2);_stamina.rectTransform.offsetMax=new Vector2(-2,-2);
            var label=Ink(_personalRoot,"StaminaLabel","Stamina",23,false);OwnerUiLayout.Place(label.rectTransform,337,117,120,42);
            for(int i=0;i<4;i++)
            {
                _status[i]=Ink(_root,"TimedStatus"+i,"",25,false);_status[i].alignment=TextAnchor.MiddleLeft;OwnerUiLayout.Place(_status[i].rectTransform,38,338+i*46,450,44);
            }
        }
        private void BuildPrompts()
        {
            _promptRoot=OwnerUiLayout.Rect(_root,"ContextualAction");Pin(_promptRoot,new Vector2(.5f,.32f),Vector2.zero,new Vector2(1100,176));
            _prompt=Ink(_promptRoot,"ActionPrompt","",36,true);OwnerUiLayout.Place(_prompt.rectTransform,0,0,1100,74);
            _context=Ink(_promptRoot,"ActionDetail","",27,false);OwnerUiLayout.Place(_context.rectTransform,0,78,1100,64);
            var track=OwnerUiLayout.Rect(_promptRoot,"RecoveryProgress").gameObject.AddComponent<Image>();track.color=OwnerUiTheme.Current.DeepInk;track.raycastTarget=false;
            OwnerUiLayout.Place(track.rectTransform,320,148,460,14);
            _progress=OwnerUiLayout.Rect(track.transform,"ProgressFill").gameObject.AddComponent<Image>();_progress.color=OwnerUiTheme.Current.Lime;_progress.raycastTarget=false;
            OwnerUiLayout.Fill(_progress.rectTransform);_progress.rectTransform.offsetMin=new Vector2(2,2);_progress.rectTransform.offsetMax=new Vector2(-2,-2);track.gameObject.SetActive(false);
        }
    }
}
