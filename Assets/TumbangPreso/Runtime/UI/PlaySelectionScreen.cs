using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Choose the game and how to join it in one place.</summary>
    public sealed class PlaySelectionScreen : MonoBehaviour
    {
        public static LobbyMode? RequestedLobbyMode;
        private Canvas _canvas;
        private RectTransform _routes;
        private GameObject _routeRows;
        private StreetGraphic _classic, _hero;
        private Sprite _generatedMark;

        public static void Install(ConvertedModeSelect owner)
        {
            var screen=owner.GetComponent<PlaySelectionScreen>();
            if(screen==null)screen=owner.gameObject.AddComponent<PlaySelectionScreen>();
            screen.Build();
        }

        private void Build()
        {
            if(_canvas!=null)return;
            foreach(Transform child in transform)
            {
                child.gameObject.SetActive(false);
                foreach(var button in child.GetComponentsInChildren<Button>(true))button.name="Retired_"+button.name;
            }
            var entrance=GetComponent<PennantEntrance>();if(entrance!=null)entrance.enabled=false;
            _canvas=MenuKit.BuildCanvas(transform,"PlayChoiceCanvas");
            var root=(RectTransform)_canvas.transform;
            MenuKit.Backdrop(root,UiTheme.Paper);
            var back=StreetUi.Button(root,"BackButton","Back",26,StreetGraphic.Surface.Navigation);
            MenuKit.Place((RectTransform)back.transform,new Vector2(0,1),new Vector2(116,-58),new Vector2(152,60));
            back.onClick.AddListener(()=>{MenuSfx.Back();Back();});
            var heading=MenuKit.Label(root,"Play",66,UiTheme.BrandRed,new Vector2(0,1),
                new Vector2(190,-158),new Vector2(220,90));heading.alignment=TextAnchor.MiddleLeft;heading.raycastTarget=false;
            var logo=new GameObject("TumpMark",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            logo.transform.SetParent(root,false);logo.sprite=Resources.Load<Sprite>("UI/brand/tump_logo");
            if(logo.sprite==null)
            {
                var texture=Resources.Load<Texture2D>("UI/brand/tump_logo");
                if(texture!=null)logo.sprite=_generatedMark=Sprite.Create(texture,
                    new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));
            }
            logo.preserveAspect=true;logo.raycastTarget=false;
            MenuKit.Place(logo.rectTransform,new Vector2(1,1),new Vector2(-188,-104),new Vector2(210,138));

            var content=Rect(root,"PlayChoices",new Vector2(.07f,.16f),new Vector2(.93f,.78f));
            var games=Rect(content,"GameChoice",Vector2.zero,new Vector2(.35f,1));
            games.offsetMax=new Vector2(-28,0);
            _routes=Rect(content,"AccessChoice",new Vector2(.35f,0),Vector2.one);
            _routes.offsetMin=new Vector2(28,0);
            Caption(games,"Game");Caption(_routes,"How would you like to play?");
            _classic=GameChoice(games,"ClassicButton","Classic","Four rounds. No powers.",
                StreetIcon.Glyph.Can,66,GameMode.Classic);
            _hero=GameChoice(games,"HeroStrikeButton","Hero Strike","Eight rounds. Hero abilities.",
                StreetIcon.Glyph.Star,244,GameMode.HeroStrike);
            var tutorial=StreetUi.Button(root,"TutorialButton","Learn to play",26,StreetGraphic.Surface.Navigation);
            MenuKit.Place((RectTransform)tutorial.transform,new Vector2(.5f,0),new Vector2(0,70),new Vector2(280,60));
            tutorial.onClick.AddListener(()=>{MenuSfx.Click();SceneFlow.StartTraining();});
            Choose(SceneFlow.SelectedMode,false);
        }

        public void Back()=>SceneFlow.Go(SceneFlow.MainMenu);
        private void OnDestroy(){if(_generatedMark!=null)Destroy(_generatedMark);}

        private StreetGraphic GameChoice(Transform parent,string name,string title,string detail,
            StreetIcon.Glyph icon,float top,GameMode mode)
        {
            var button=StreetUi.Button(parent,name,"",30,StreetGraphic.Surface.Option);
            Row((RectTransform)button.transform,top,154);
            StreetUi.Icon(button.transform,icon,new Vector2(0,.5f),new Vector2(45,4),new Vector2(50,64));
            RowText(button.transform,title,36,true,82,26);
            RowText(button.transform,detail,24,false,82,-26);
            button.onClick.AddListener(()=>Choose(mode,true));
            return button.GetComponent<StreetGraphic>();
        }

        private void Choose(GameMode mode,bool sound)
        {
            if(sound)MenuSfx.Click();
            SceneFlow.SelectedMode=mode;
            SceneFlow.SetSelectedRules(SceneFlow.SelectedRules);
            _classic.Chosen=mode==GameMode.Classic;_classic.SetVerticesDirty();
            _hero.Chosen=mode==GameMode.HeroStrike;_hero.SetVerticesDirty();
            if(_routeRows!=null){_routeRows.SetActive(false);Destroy(_routeRows);}
            _routeRows=Rect(_routes,"Routes",Vector2.zero,Vector2.one).gameObject;
            Route("PracticeButton","Play with bots","A full offline match with bot opponents.",
                StreetIcon.Glyph.Slipper,66,LobbyMode.Practice);
            Route("CustomButton","Play with friends","Create or join a custom room.",
                StreetIcon.Glyph.People,242,LobbyMode.Custom);
            if(mode==GameMode.HeroStrike)
                Route("RankedButton","Ranked","Online competitive play. Sign-in required.",
                    StreetIcon.Glyph.Medal,418,LobbyMode.Ranked);
            _canvas.GetComponent<InputLayer.ScreenFocus>()?.Rebuild();
        }

        private void Route(string name,string title,string detail,StreetIcon.Glyph icon,float top,LobbyMode mode)
        {
            var button=StreetUi.Button(_routeRows.transform,name,"",30,StreetGraphic.Surface.Route);
            Row((RectTransform)button.transform,top,148);
            StreetUi.Icon(button.transform,icon,new Vector2(0,.5f),new Vector2(60,0),new Vector2(68,68));
            MenuKit.Read(RowText(button.transform,title,34,true,120,25),true);
            RowText(button.transform,detail,25,false,120,-29);
            button.onClick.AddListener(()=>
            {
                MenuSfx.Click();RequestedLobbyMode=mode;SceneFlow.Networked=mode!=LobbyMode.Practice;
                SceneFlow.Go(SceneFlow.MatchSetup);
            });
        }

        private static void Caption(Transform parent,string words)
        {
            var label=MenuKit.Label(parent,words,26,UiTheme.PaperInkSoft,new Vector2(.5f,1),
                new Vector2(0,-22),new Vector2(0,44));MenuKit.Read(label,true);
            label.rectTransform.anchorMin=new Vector2(0,1);label.rectTransform.anchorMax=new Vector2(1,1);
            label.rectTransform.offsetMin=new Vector2(8,label.rectTransform.offsetMin.y);
            label.rectTransform.offsetMax=new Vector2(-8,label.rectTransform.offsetMax.y);
            label.alignment=TextAnchor.MiddleLeft;label.raycastTarget=false;
        }
        private static Text RowText(Transform parent,string words,int size,bool heading,float inset,float y)
        {
            var label=MenuKit.Label(parent,words,size,heading?UiTheme.BrandRed:UiTheme.PaperInk,
                new Vector2(.5f,.5f),new Vector2(0,y),new Vector2(0,58));
            if(!heading)MenuKit.Read(label);
            label.rectTransform.anchorMin=new Vector2(0,.5f);label.rectTransform.anchorMax=new Vector2(1,.5f);
            label.rectTransform.offsetMin=new Vector2(inset,y-29);
            label.rectTransform.offsetMax=new Vector2(-24,y+29);
            label.alignment=TextAnchor.MiddleLeft;label.horizontalOverflow=HorizontalWrapMode.Wrap;
            label.raycastTarget=false;
            return label;
        }
        private static void Row(RectTransform row,float top,float height)
        {
            row.anchorMin=new Vector2(0,1);row.anchorMax=Vector2.one;row.pivot=new Vector2(.5f,1);
            row.sizeDelta=new Vector2(0,height);row.anchoredPosition=new Vector2(0,-top);
        }
        private static RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false);rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
            return rect;
        }
    }
}
