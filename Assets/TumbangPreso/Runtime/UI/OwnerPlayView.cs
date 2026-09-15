using UnityEngine;
using TumbangPreso.Core;

namespace TumbangPreso.UI
{
    public sealed class OwnerPlayView : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _classicTick,_heroTick;
        private RectTransform[] _routes=new RectTransform[3];
        public void Build(Transform owner)
        {
            _canvas=OwnerUiLayout.Canvas(owner,"OwnerPlayCanvas",100);
            OwnerUiBackdrop.Build(_canvas.transform);
            var design=OwnerUiLayout.DesignArea(_canvas.transform,"PlayComposition");
            OwnerTextAction.Create(design,"BackButton","BACK",()=>SceneFlow.Go(SceneFlow.MainMenu),58,32,164,68,30);
            var title=OwnerUiLayout.Text(design,"Heading","LET'S PLAY",64,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform,84,122,1400,100);title.color=OwnerUiTheme.Current.ActionInk;
            var logo=OwnerUiLayout.Art(design,"OriginalOwnerLogo",OwnerUiTheme.Piece.Logo);
            OwnerUiLayout.Place(logo.rectTransform,1608,53,190,190*273f/407);
            _classicTick=Choice(design,"ClassicButton","CLASSIC","Eight rounds. No powers.",GameMode.Classic,158);
            _heroTick=Choice(design,"HeroStrikeButton","HERO STRIKE","Eight rounds. Six heroes.",GameMode.HeroStrike,1018);
            var routeTitle=OwnerUiLayout.Text(design,"RoutesHeading","HOW ARE YOU PLAYING?",33,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(routeTitle.rectTransform,230,671,1460,60);routeTitle.alignment=TextAnchor.MiddleCenter;
            _routes[0]=Route(design,"PracticeButton","WITH BOTS","Offline match",LobbyMode.Practice,false);
            _routes[1]=Route(design,"CustomButton","WITH FRIENDS","Create or join a room",LobbyMode.Custom,true);
            _routes[2]=Route(design,"RankedButton","RANKED","Online. Sign-in required.",LobbyMode.Ranked,false);
            OwnerTextAction.Create(design,"TutorialButton","LEARN TO PLAY",SceneFlow.StartTraining,713,960,494,66,30);
            Select(SceneFlow.SelectedMode);
        }
        private GameObject Choice(RectTransform parent,string name,string title,string detail,GameMode mode,float x)
        {
            var root=OwnerUiLayout.Rect(parent,name);OwnerUiLayout.Place(root,x,275,745,342);
            var hit=root.gameObject.AddComponent<UnityEngine.UI.Image>();hit.color=Color.clear;
            var button=root.gameObject.AddComponent<OwnerTextAction>();button.targetGraphic=hit;button.transition=UnityEngine.UI.Selectable.Transition.None;
            button.onClick.AddListener(()=>{MenuSfx.Click();Select(mode);});
            var paper=OwnerUiLayout.Rect(root,"ModeSheet").gameObject.AddComponent<OwnerUiPaper>();paper.Style=OwnerUiPaper.Treatment.Note;OwnerUiLayout.Fill(paper.rectTransform);paper.raycastTarget=false;
            paper.gameObject.AddComponent<OwnerUiMotion>();
            var label=OwnerUiLayout.Text(paper.transform,"ModeName",title,42,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(label.rectTransform,35,38,470,62);
            var entries=Roster.GetPeople(mode);
            for(int i=0;i<Mathf.Min(3,entries.Count);i++)
            {
                var portrait=OwnerPortraitArt.Create(paper.transform,"ModePortrait"+i,"UI/portraits/"+entries[i].Id);
                OwnerUiLayout.Place(portrait.rectTransform,218+i*155,85,174,195);
            }
            var caption=OwnerUiLayout.Text(paper.transform,"ModeDetails",detail,29);
            OwnerUiLayout.Place(caption.rectTransform,35,275,660,45);caption.color=OwnerUiTheme.Current.EnteredInk;
            var tick=OwnerUiGlyph.Create(paper.transform,"SelectedMode",OwnerUiGlyph.Mark.Check,OwnerUiTheme.Current.Green);
            OwnerUiLayout.Place(tick.rectTransform,651,36,45,45);return tick.gameObject;
        }
        private RectTransform Route(RectTransform parent,string name,string title,string caption,LobbyMode mode,bool orange)
        {
            var group=OwnerUiLayout.Rect(parent,name+"Group");OwnerUiLayout.Place(group,0,773,413,145);
            var action=OwnerPaintedAction.Create(group,name,title,()=>
            {
                PlaySelectionScreen.RequestedLobbyMode=mode;SceneFlow.Networked=mode!=LobbyMode.Practice;
                SceneFlow.Go(SceneFlow.MatchSetup);
            },orange,39);
            OwnerUiLayout.Place((RectTransform)action.transform,0,0,413,91);
            var hint=OwnerUiLayout.Text(group,"RouteDetails",caption,26);OwnerUiLayout.Place(hint.rectTransform,0,102,413,43);
            hint.alignment=TextAnchor.MiddleCenter;hint.color=OwnerUiTheme.Current.EnteredInk;return group;
        }
        public void Select(GameMode mode)
        {
            SceneFlow.SelectedMode=mode;SceneFlow.SetSelectedRules(SceneFlow.SelectedRules);
            _classicTick.SetActive(mode==GameMode.Classic);_heroTick.SetActive(mode==GameMode.HeroStrike);
            bool hero=mode==GameMode.HeroStrike;_routes[2].gameObject.SetActive(hero);
            for(int i=0;i<3;i++)OwnerUiLayout.Place(_routes[i],hero?217+i*538:470+i*539,773,413,145);
            _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }
        private void OnDisable(){if(_canvas!=null)_canvas.gameObject.SetActive(false);}
    }
}
