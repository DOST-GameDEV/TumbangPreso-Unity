using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class LobbyChat
    {
        private CanvasGroup _ownerVisibility;
        private static readonly Color ChatSurface=new Color32(220,230,219,255);
        private static readonly Color ChatInk=new Color32(35,49,43,255);
        public bool IsPresented=>_ownerVisibility==null || _ownerVisibility.alpha>.5f;
        public void SetPresented(bool shown)
        {
            if(_ownerVisibility==null)_ownerVisibility=gameObject.AddComponent<CanvasGroup>();
            _ownerVisibility.alpha=shown?1:0;_ownerVisibility.interactable=shown;_ownerVisibility.blocksRaycasts=shown;
            if(!shown){Close();CloseHistory();}
            if(_field!=null)_field.interactable=shown;
        }
        private void ConstructNative()
        {
            _nativeChat=true;_rect=gameObject.AddComponent<RectTransform>();
            gameObject.AddComponent<OwnerUiCanvas>();
            _rect.anchorMin=_rect.anchorMax=_rect.pivot=Vector2.zero;
            _rect.anchoredPosition=new Vector2(38,232);_rect.sizeDelta=new Vector2(580,172);
            var column=gameObject.AddComponent<VerticalLayoutGroup>();
            column.childControlHeight=column.childControlWidth=true;column.childForceExpandHeight=false;
            column.childForceExpandWidth=true;column.childAlignment=TextAnchor.LowerLeft;column.spacing=6;
            column.padding=new RectOffset(20,20,16,18);
            gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            if(!_inMatch)
            {
                var paper=OwnerUiLayout.Rect(_rect,"ChatPaper").gameObject.AddComponent<Image>();
                paper.gameObject.AddComponent<LayoutElement>().ignoreLayout=true;OwnerUiLayout.Fill(paper.rectTransform);
                paper.color=ChatSurface;paper.raycastTarget=true;
                var head=OwnerUiLayout.Rect(_rect,"ChatHeading");Height(head,54);
                var title=OwnerUiLayout.Text(head,"ChatTitle","ROOM CHAT",29,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(title.rectTransform,4,0,260,54);
                OwnerTextAction.Create(head,"ChatHistoryButton","HISTORY",OpenHistory,282,0,196,54,30);
                var close=OwnerTextAction.Create(head,"CloseChatButton","CLOSE",()=>SetPresented(false),0,0,150,54,30);
                var rt=(RectTransform)close.transform;rt.anchorMin=rt.anchorMax=rt.pivot=Vector2.one;rt.anchoredPosition=Vector2.zero;
            }
            for(int i=0;i<MaxLines;i++)
            {
                var line=OwnerUiLayout.Text(_rect,"ChatLine"+i,"",30);
                line.color=_inMatch?OwnerUiTheme.Current.Pale:ChatInk;
                Height(line.rectTransform,NativeLineHeight);
                if(_inMatch)
                {
                    var outline=line.gameObject.AddComponent<Outline>();outline.effectColor=UiTheme.InGameOutline;
                    outline.effectDistance=new Vector2(2,-2);
                }
                _lines.Add(line);_stamps.Add(-999);
            }
            var fieldHost=OwnerUiLayout.Rect(_rect,"ChatFieldRow");Height(fieldHost,78);_fieldRow=fieldHost.gameObject;
            _field=BuildChatEntry(fieldHost);
            _field.textComponent.fontSize=30;_field.characterLimit=Net.MatchRpc.MaxChatLength;_field.onSubmit.AddListener(Submit);
            if(_inMatch)_fieldRow.SetActive(false);else BuildNativeHistory();
            SetNativeLines();
        }
        private InputField BuildChatEntry(Transform parent)
        {
            var rect=OwnerUiLayout.Rect(parent,"ChatInput");OwnerUiLayout.Fill(rect);
            var background=rect.gameObject.AddComponent<Image>();background.color=_inMatch?new Color32(31,39,45,245):new Color32(242,243,226,255);
            var field=rect.gameObject.AddComponent<InputField>();field.targetGraphic=background;field.transition=Selectable.Transition.None;
            field.customCaretColor=true;field.caretColor=_inMatch?OwnerUiTheme.Current.Pale:ChatInk;
            field.selectionColor=new Color(.42f,.60f,.39f,.45f);field.lineType=InputField.LineType.SingleLine;
            var area=OwnerUiLayout.Rect(rect,"ChatEntryArea");OwnerUiLayout.Fill(area);area.offsetMin=new Vector2(20,8);area.offsetMax=new Vector2(-20,-8);
            var text=OwnerUiLayout.Text(area,"EnteredText","",30);OwnerUiLayout.Fill(text.rectTransform);text.supportRichText=false;
            text.color=field.caretColor;text.horizontalOverflow=HorizontalWrapMode.Overflow;field.textComponent=text;
            var hint=OwnerUiLayout.Text(area,"Placeholder",_inMatch?"ENTER TO TALK":"SAY SOMETHING",30,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Fill(hint.rectTransform);hint.color=_inMatch?new Color32(184,203,192,255):new Color32(79,105,87,255);field.placeholder=hint;
            return field;
        }
        private static void Height(RectTransform rect,float height)
        {
            var layout=rect.GetComponent<LayoutElement>();if(layout==null)layout=rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight=layout.preferredHeight=height;
        }
        private void SetNativeLines()
        {
            if(!_inMatch)
            {
                int first=Mathf.Max(0,_history.Count-LobbyVisibleLines);
                int target=MaxLines-Mathf.Min(LobbyVisibleLines,_history.Count);
                for(int i=0;i<_lines.Count;i++)_lines[i].text=i<target?"":_history[first+i-target];
                if(_history.Count==0)_lines[_lines.Count-1].text=EmptyLog;
            }
            foreach(var line in _lines)
            {
                bool shown=!string.IsNullOrEmpty(line.text);line.gameObject.SetActive(shown);if(!shown)continue;
                line.color=_inMatch?OwnerUiTheme.Current.Pale:ChatInk;
                line.fontSize=30;Height(line.rectTransform,NativeLineHeight);Ellipsise(line,NativeLineHeight);
            }
        }
        private void BuildNativeHistory()
        {
            var rect=OwnerUiLayout.Rect(_rect,"OwnerChatHistory");_historyPanel=rect.gameObject;
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout=true;
            // Expand over the compact transcript; keep one composer below, not two copies of the same conversation.
            rect.anchorMin=Vector2.zero;rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(.5f,0);
            rect.offsetMin=new Vector2(0,98);rect.offsetMax=new Vector2(0,650);
            var canvas=rect.gameObject.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.vertexColorAlwaysGammaSpace=true;
            var parent=_rect.GetComponentInParent<Canvas>();canvas.sortingOrder=parent!=null?parent.sortingOrder+10:710;
            rect.gameObject.AddComponent<GraphicRaycaster>();
            var paper=OwnerUiLayout.Rect(rect,"HistoryPaper").gameObject.AddComponent<Image>();OwnerUiLayout.Fill(paper.rectTransform);paper.color=ChatSurface;
            var title=OwnerUiLayout.Text(rect,"ConversationTitle","CONVERSATION",37,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform,24,12,440,76);
            var close=OwnerTextAction.Create(rect,"ChatHistoryBack","CLOSE",CloseHistory,0,0,170,68,30);
            var rt=(RectTransform)close.transform;rt.anchorMin=rt.anchorMax=rt.pivot=Vector2.one;rt.anchoredPosition=new Vector2(-22,-18);
            var content=OwnerScrollColumn.Build(rect,"ChatHistoryScroll",new Rect(24,106,960,380),out _historyScroll);
            var scroll=(RectTransform)_historyScroll.transform;scroll.anchorMin=Vector2.zero;scroll.anchorMax=Vector2.one;
            scroll.offsetMin=new Vector2(24,24);scroll.offsetMax=new Vector2(-24,-106);
            _historyText=OwnerUiLayout.Text(content,"FullTranscript",EmptyLog,30);_historyText.color=ChatInk;
            _historyText.alignment=TextAnchor.UpperLeft;
            InputLayer.ScreenFocus.Install(rect.gameObject);_historyPanel.SetActive(false);
            ScreenTakeover.Register(this,()=>_historyPanel!=null && _historyPanel.activeInHierarchy);
        }
    }
}
