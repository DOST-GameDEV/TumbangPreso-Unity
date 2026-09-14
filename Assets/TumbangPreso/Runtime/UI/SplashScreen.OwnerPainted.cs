using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class SplashScreen
    {
        private bool _ownerLoading;
        private void BuildOwnerLoadingSurface()
        {
            _ownerLoading=true;
            var canvas=OwnerUiLayout.Canvas(transform,"OwnerLoadingCanvas",500);
            _canvas=canvas.gameObject;HideConvertedContent();
            OwnerUiBackdrop.Build(canvas.transform);
            var design=OwnerUiLayout.DesignArea(canvas.transform,"OwnerLoadingComposition");
            var logo=OwnerUiLayout.Art(design,"LoadingOwnerLogo",OwnerUiTheme.Piece.Logo);
            OwnerUiLayout.Place(logo.rectTransform,685,162,550,550*273f/407);
            logo.gameObject.AddComponent<OwnerUiMotion>().GentleFloat=true;
            logo.raycastTarget=true;_artButton=logo.gameObject.AddComponent<Button>();
            _artButton.targetGraphic=logo;_artButton.transition=Selectable.Transition.None;
            _artButton.onClick.AddListener(()=>ShowStory(true));
            _loadingLabel=OwnerUiLayout.Text(design,"LoadingStatus","GETTING READY",30,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(_loadingLabel.rectTransform,600,591,720,60);_loadingLabel.alignment=TextAnchor.MiddleCenter;
            var rail=OwnerUiLayout.Art(design,"ProgressTrack",OwnerUiTheme.Piece.LeftRule);
            OwnerUiLayout.Place(rail.rectTransform,718,677,484,12);rail.color=new Color(1,1,1,.22f);
            _loadingFill=OwnerUiLayout.Art(design,"ActualProgress",OwnerUiTheme.Piece.LeftRule);
            OwnerUiLayout.Place(_loadingFill.rectTransform,718,677,484,12);
            _loadingFill.type=Image.Type.Filled;_loadingFill.fillMethod=Image.FillMethod.Horizontal;
            _loadingFill.fillOrigin=(int)Image.OriginHorizontal.Left;_loadingFill.fillAmount=0;
            OwnerTextAction.Create(design,"LoadingStories","STORIES & TIPS",()=>ShowStory(true),703,750,514,68,29);

            _storyRoot=OwnerUiLayout.Rect(design,"LoadingStoryRoot").gameObject;
            OwnerUiLayout.Fill((RectTransform)_storyRoot.transform);
            var blocker=_storyRoot.AddComponent<Image>();blocker.color=new Color(0,0,0,.22f);blocker.raycastTarget=true;
            var sheet=OwnerUiLayout.Rect(_storyRoot.transform,"StorySheet").gameObject.AddComponent<OwnerUiPaper>();
            OwnerUiLayout.Place(sheet.rectTransform,497,281,926,516);sheet.raycastTarget=true;
            var title=OwnerUiLayout.Text(_storyRoot.transform,"StoryHeading","FROM THE STREET",34,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(title.rectTransform,552,321,806,62);
            _storyText=OwnerUiLayout.Text(_storyRoot.transform,"StoryText","",30);
            OwnerUiLayout.Place(_storyText.rectTransform,552,411,806,224);_storyText.color=OwnerUiTheme.Current.EnteredInk;
            _storyText.alignment=TextAnchor.UpperLeft;
            OwnerTextAction.Create(_storyRoot.transform,"LoadingStoryClose","CLOSE",()=>ShowStory(false),554,698,222,64,28);
            OwnerTextAction.Create(_storyRoot.transform,"LoadingStoryNext","NEXT STORY",()=>{_storyIndex++;RefreshStory();},1044,698,323,64,28);
            InputLayer.ScreenFocus.Install(_storyRoot);_storyRoot.SetActive(false);

            var fade=OwnerUiLayout.Rect(canvas.transform,"LoadingFade").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(fade.rectTransform);fade.color=new Color32(238,108,74,255);fade.raycastTarget=false;_fade=fade;
        }
        private static string FriendlyLoadingStage(string stage)
        {
            if(string.IsNullOrEmpty(stage))return "GETTING READY";
            if(stage=="ready")return "READY TO PLAY";
            if(stage=="signing in")return "CONNECTING YOUR PROFILE";
            if(stage.Contains("character") || stage.Contains("abilit"))return "GETTING THE PLAYERS READY";
            if(stage.Contains("map") || stage.Contains("arena"))return "GETTING THE COURT READY";
            return "GETTING READY";
        }
    }
}
