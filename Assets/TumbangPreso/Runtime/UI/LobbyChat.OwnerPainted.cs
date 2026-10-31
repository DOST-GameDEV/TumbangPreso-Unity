using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class LobbyChat
    {
        private CanvasGroup _ownerVisibility;
        public event Action<bool> PresentedChanged;
        private static readonly Color ChatSurface=new Color32(220,230,219,255);
        private static readonly Color ChatInk=new Color32(35,49,43,255);
        public bool IsPresented=>_ownerVisibility==null || _ownerVisibility.alpha>.5f;
        public void SetPresented(bool shown)
        {
            bool changed=IsPresented!=shown;
            if(_ownerVisibility==null)_ownerVisibility=gameObject.AddComponent<CanvasGroup>();
            _ownerVisibility.alpha=shown?1:0;_ownerVisibility.interactable=shown;_ownerVisibility.blocksRaycasts=shown;
            if(!shown){Close();CloseHistory();}
            if(_field!=null)_field.interactable=shown;
            if(changed)PresentLobbyColumn(shown);
            if(changed)PresentedChanged?.Invoke(shown);
        }
        public void PlaceLobbyColumn()
        {
            if(_rect==null || _inMatch)return;
            // The wine drawer stands at the right of the room, from the players' heading down to the
            // floor, centred on the canvas like the cards it sits beside.
            _rect.anchorMin=_rect.anchorMax=new Vector2(.5f,.5f);
            _rect.pivot=new Vector2(0,1);
            _rect.anchoredPosition=new Vector2(Hub.HubLobby.RoomChatLeft,Hub.HubLobby.CardsTop+80);
            _rect.sizeDelta=new Vector2(Hub.HubLobby.RoomChatWidth,_lobbyColumn?DrawerHeight:_rect.sizeDelta.y);
        }
        private void ConstructNative()
        {
            if(!_inMatch){ConstructLobbyColumn();return;}
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
            // Owner, October 31: "make that smaller ... ang distracting and unnecessary". A small quiet strip in a match.
            var fieldHost=OwnerUiLayout.Rect(_rect,"ChatFieldRow");Height(fieldHost,_inMatch?40:78);_fieldRow=fieldHost.gameObject;
            _field=BuildChatEntry(fieldHost);
            _field.textComponent.fontSize=_inMatch?20:30;_field.characterLimit=Net.MatchRpc.MaxChatLength;_field.onSubmit.AddListener(Submit);
            if(_inMatch)_fieldRow.SetActive(false);else BuildNativeHistory();
            SetNativeLines();
            if (_inMatch) HudReadingLayout.Watch(_rect, new Vector2(0, 184));
        }
        private InputField BuildChatEntry(Transform parent)
        {
            var rect=OwnerUiLayout.Rect(parent,"ChatInput");OwnerUiLayout.Fill(rect);
            if(_inMatch)rect.anchorMax=new Vector2(.68f,1);
            var background=rect.gameObject.AddComponent<Image>();background.color=_inMatch?new Color32(31,39,45,0):new Color32(242,243,226,255);
            // Owner, October 31: "js big slab of black in the middle". No plate over the match: words on a thin line.
            if(_inMatch)
            {
                var rule=OwnerUiLayout.Rect(rect,"ChatEntryRule").gameObject.AddComponent<Image>();rule.raycastTarget=false;
                rule.color=new Color(OwnerUiTheme.Current.Pale.r,OwnerUiTheme.Current.Pale.g,OwnerUiTheme.Current.Pale.b,.45f);
                rule.rectTransform.anchorMin=Vector2.zero;rule.rectTransform.anchorMax=Vector2.right;rule.rectTransform.pivot=Vector2.zero;
                rule.rectTransform.offsetMin=Vector2.zero;rule.rectTransform.offsetMax=new Vector2(0,2);
            }
            var field=rect.gameObject.AddComponent<InputField>();field.targetGraphic=background;field.transition=Selectable.Transition.None;
            field.customCaretColor=true;field.caretColor=_inMatch?OwnerUiTheme.Current.Pale:ChatInk;
            field.selectionColor=new Color(.42f,.60f,.39f,.45f);field.lineType=InputField.LineType.SingleLine;
            var area=OwnerUiLayout.Rect(rect,"ChatEntryArea");OwnerUiLayout.Fill(area);area.offsetMin=new Vector2(_inMatch?12:20,_inMatch?4:8);area.offsetMax=new Vector2(_inMatch?-12:-20,_inMatch?-4:-8);
            var text=OwnerUiLayout.Text(area,"EnteredText","",30);OwnerUiLayout.Fill(text.rectTransform);text.supportRichText=false;
            text.color=field.caretColor;text.horizontalOverflow=HorizontalWrapMode.Overflow;field.textComponent=text;
            var hint=OwnerUiLayout.Text(area,"Placeholder",_inMatch?"TALK  ·  ESC TO HIDE":"SAY SOMETHING",_inMatch?20:30,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Fill(hint.rectTransform);hint.color=_inMatch?new Color32(184,203,192,190):new Color32(79,105,87,255);field.placeholder=hint;
            if(_inMatch)foreach(var words in new[]{text,hint})
            {
                var outline=words.gameObject.AddComponent<Outline>();outline.effectColor=UiTheme.InGameOutline;outline.effectDistance=new Vector2(2,-2);
            }
            return field;
        }
        private static void Height(RectTransform rect,float height)
        {
            var layout=rect.GetComponent<LayoutElement>();if(layout==null)layout=rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight=layout.preferredHeight=height;
        }
        private void SetNativeLines()
        {
            if(_lobbyColumn){DrawLobbyColumn();return;}
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
                line.fontSize=30;Height(line.rectTransform,NativeLineHeight);
            }
            // Newly activated rows have not received the column's width yet. Fitting now
            // permanently reduces the first match message to a few letters. Lay out all
            // visible rows once per received message before measuring their text.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
            foreach(var line in _lines)
                if(line.gameObject.activeSelf)Ellipsise(line,NativeLineHeight);
        }
        private void BuildNativeHistory()
        {
            var rect=OwnerUiLayout.Rect(_rect,"OwnerChatHistory");_historyPanel=rect.gameObject;
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout=true;
            // Expand over the compact transcript; keep one composer below, not two copies of the same conversation.
            rect.anchorMin=Vector2.zero;rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(.5f,0);
            rect.offsetMin=new Vector2(0,98);rect.offsetMax=new Vector2(0,600);
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
