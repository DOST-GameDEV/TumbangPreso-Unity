using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed class OwnerCharacterStoryView : MonoBehaviour
    {
        private Canvas _canvas;
        private Text _title,_origin,_court,_shortLine,_body;
        private Image _portrait;
        private ScrollRect _scroll;
        private Action _back;
        public void Open(string id,string name,Action back)
        {
            var story=OwnerCharacterStories.For(id);if(story==null)return;
            _back=back;if(_canvas==null)Build();
            _title.text="MEET "+name;_origin.text=story.origin;_court.text="Home court · "+story.homeCourt;
            _shortLine.text=story.shortLine;_body.text=story.introduction;_portrait.sprite=OwnerPortraitArt.Get("UI/portraits/"+id);
            _canvas.gameObject.SetActive(true);_scroll.verticalNormalizedPosition=1;_canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }
        private void Build()
        {
            _canvas=OwnerUiLayout.Canvas(transform,"OwnerCharacterStoryCanvas",730);OwnerUiBackdrop.Build(_canvas.transform);
            var root=OwnerUiLayout.DesignArea(_canvas.transform,"CharacterStoryComposition");
            OwnerTextAction.Create(root,"CloseCharacterStory","BACK",Close,55,25,170,70,30);
            _title=OwnerUiLayout.Text(root,"StoryTitle","",64,OwnerUiLayout.TypeRole.Display);OwnerUiLayout.Place(_title.rectTransform,91,138,1718,111);
            var mat=OwnerUiLayout.Rect(root,"StoryPortraitMat").gameObject.AddComponent<OwnerPreviewMat>();
            OwnerUiLayout.Place(mat.rectTransform,118,358,614,467);mat.raycastTarget=false;
            _portrait=OwnerUiLayout.Rect(root,"StoryPortrait").gameObject.AddComponent<Image>();_portrait.preserveAspect=true;_portrait.raycastTarget=false;
            OwnerUiLayout.Place(_portrait.rectTransform,169,328,501,487);
            _shortLine=OwnerUiLayout.Text(root,"PersonalityLine","",32);_shortLine.alignment=TextAnchor.MiddleCenter;_shortLine.color=OwnerUiTheme.Current.EnteredInk;
            OwnerUiLayout.Place(_shortLine.rectTransform,100,888,667,103);
            var paper=OwnerUiLayout.Rect(root,"StoryPaper").gameObject.AddComponent<OwnerUiPaper>();OwnerUiLayout.Place(paper.rectTransform,813,303,1015,701);paper.raycastTarget=false;
            _origin=OwnerUiLayout.Text(root,"StoryOrigin","",40,OwnerUiLayout.TypeRole.Accent);OwnerUiLayout.Place(_origin.rectTransform,860,339,924,90);
            _court=OwnerUiLayout.Text(root,"StoryCourt","",27);_court.color=OwnerUiTheme.Current.EnteredInk;OwnerUiLayout.Place(_court.rectTransform,862,449,921,85);
            var content=OwnerScrollColumn.Build(root,"StoryReading",new Rect(862,562,909,385),out _scroll);
            _body=OwnerUiLayout.Text(content,"StoryText","",32);_body.alignment=TextAnchor.UpperLeft;_body.color=OwnerUiTheme.Current.EnteredInk;_body.lineSpacing=1.12f;_body.gameObject.AddComponent<TumpParagraph>();
            ScreenTakeover.Register(this,()=>_canvas!=null && _canvas.gameObject.activeInHierarchy);
        }
        private void Close(){if(_canvas!=null)_canvas.gameObject.SetActive(false);_back?.Invoke();}
        private void Update()
        {
            if(_canvas==null || !_canvas.gameObject.activeSelf || !InputLayer.MenuNav.CancelPressed || ScreenTakeover.EscapeIsSpokenExcept(this))return;
            ScreenTakeover.ConsumeEscape();Close();
        }
        private void OnDisable(){if(_canvas!=null)_canvas.gameObject.SetActive(false);}
        private void OnDestroy()=>ScreenTakeover.Unregister(this);
    }
}
