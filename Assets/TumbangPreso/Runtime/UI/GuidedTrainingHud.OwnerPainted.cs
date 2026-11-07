using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso
{
    public sealed partial class GuidedTrainingHud
    {
        private Text _ownerSkipLabel;
        private RectTransform _ownerQuit;
        private bool _phoneControls;
        private bool _hasLessonAction;
        private bool _inspecting;
        public void SetInspecting(bool held)
        {
            if (_inspecting == held) return;
            _inspecting = held;
            // The kit tray supplies the long descriptions while held. Retain the
            // objective/progress/exit controls in a compact card above that tray.
            if (_body != null) _body.gameObject.SetActive(!held);
            if (_keyRow != null) _keyRow.gameObject.SetActive(!held && _hasLessonAction);
        }
        private static readonly Color TrainingInk=Color.white;
        private static readonly Color TrainingMuted=Color.white;
        // The bars' track: the HUD's deep glass, darker than the grey boxes so a bar reads on them (owner, 2026-11-08).
        private static readonly Color TrainingTrack=new Color32(0x14,0x12,0x1C,235);
        private static readonly Color TrainingDone=new Color32(0xBF,0xD8,0x3A,255);
        private static readonly Color TrainingCurrent=Color.white;
        private void BuildUi()
        {
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting=true;canvas.sortingOrder=240;canvas.vertexColorAlwaysGammaSpace=true;canvas.pixelPerfect=true;
            gameObject.AddComponent<OwnerUiCanvas>();gameObject.AddComponent<OwnerUiOverrides>();gameObject.AddComponent<GraphicRaycaster>();InputLayer.UiInputModule.Ensure();
            var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=OwnerUiTheme.Current.ReferenceResolution;scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            bool touch = Application.isMobilePlatform || InputLayer.TouchHud.ForceVisible;
            _phoneControls = touch;
            var content = touch ? UiSafeArea.Content(canvas, "TrainingSafeArea") : (RectTransform)transform;
            var card=OwnerUiLayout.Rect(content,"ObjectiveCard");OwnerUiLayout.Place(card,36,36,touch ? 900 : CardWidth,0);
            // The HUD's grey woven plate (owner, 2026-11-08), as the F1 reference's plates draw it.
            var panel=card.gameObject.AddComponent<Image>();panel.raycastTarget=false;
            panel.sprite=TumbangPreso.UI.Hub.HubKnit.Knitted(LiveHud.Folder+"ref-frame-flat",new Vector4(18,18,18,18));panel.type=Image.Type.Sliced;
            // The F1 plates' stroke and slab, with their weave inside at its own scale, cropped not stretched, as see-through as they are.
            KitPanelWeave.On(card);
            // The dark grey boxes (owner, 2026-11-08): one behind where you are, one behind how to leave.
            KitPanelWeave.Box(card,"HeaderBox",true,94);KitPanelWeave.Box(card,"FooterBox",false,92);
            var edge=OwnerUiLayout.Rect(card,"TrainingEdge").gameObject.AddComponent<Image>();
            edge.gameObject.AddComponent<LayoutElement>().ignoreLayout=true;edge.raycastTarget=false;edge.color=Color.clear;
            edge.rectTransform.anchorMin=Vector2.zero;edge.rectTransform.anchorMax=new Vector2(0,1);
            edge.rectTransform.offsetMin=Vector2.zero;edge.rectTransform.offsetMax=new Vector2(5,0);
            var column=card.gameObject.AddComponent<VerticalLayoutGroup>();column.spacing=0;
            // One 32 margin left and right; rows set their own rhythm (owner, 2026-11-08: hierarchy, spacing, alignment).
            column.padding=new RectOffset(32,32,20,0);column.childControlWidth=column.childControlHeight=true;
            column.childForceExpandWidth=true;column.childForceExpandHeight=false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            // Header, in its dark box: the route name and the lesson count, small, and one solid route bar under them.
            var header=OwnerRow(card,"TrainingHeader",36);
            var word=OwnerUiLayout.Text(header,"TrainingWord","TRAINING",26,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(word.rectTransform,0,0,330,36);word.color=TrainingMuted;word.verticalOverflow=VerticalWrapMode.Overflow;
            _counter=OwnerUiLayout.Text(header,"LessonCounter","01 / " + GuidedTraining.LessonCount,26,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_counter.rectTransform,CardWidth-64-285,0,285,36);_counter.alignment=TextAnchor.MiddleRight;_counter.color=TrainingMuted;_counter.verticalOverflow=VerticalWrapMode.Overflow;
            OwnerRow(card,"RailGap",8);
            // Inside the header box, with as much room under it as the box has above the words.
            var rail=OwnerRow(card,"RouteRail",8);
            var railTrack=rail.gameObject.AddComponent<Image>();railTrack.color=TrainingTrack;railTrack.raycastTarget=false;
            _route=OwnerUiLayout.Rect(rail,"RouteFill").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(_route.rectTransform);_route.color=TrainingDone;_route.raycastTarget=false;_route.rectTransform.anchorMax=new Vector2(0,1);
            OwnerRow(card,"HeaderGap",40);
            // The lesson: its name biggest, the sentence under it, then what to press.
            _title=OwnerUiLayout.Text(card,"LessonTitle","TRAINING",48,OwnerUiLayout.TypeRole.Display);
            _title.color=TrainingInk;_title.gameObject.AddComponent<LayoutElement>().preferredHeight=56;_title.verticalOverflow=VerticalWrapMode.Overflow;
            OwnerRow(card,"TitleGap",6);
            _body=OwnerUiLayout.Text(card,"LessonBody","",28);_body.color=TrainingInk;_body.lineSpacing=1.05f;
            _body.alignment=TextAnchor.UpperLeft;_body.verticalOverflow=VerticalWrapMode.Overflow;
            _body.gameObject.AddComponent<LayoutElement>().minHeight=32;
            OwnerRow(card,"BodyGap",16);
            _keyRow=OwnerRow(card,"KeyRow",56);var keys=_keyRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            keys.childControlHeight=keys.childControlWidth=true;keys.childForceExpandHeight=keys.childForceExpandWidth=false;keys.spacing=9;
            OwnerRow(card,"KeyGap",18);
            var progress=OwnerRow(card,"ProgressBack",8).gameObject.AddComponent<Image>();progress.color=TrainingTrack;progress.raycastTarget=false;
            _fill=OwnerUiLayout.Rect(progress.transform,"ProgressFill").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(_fill.rectTransform);_fill.color=TrainingDone;_fill.raycastTarget=false;
            _fill.rectTransform.anchorMax=new Vector2(0,1);
            _complete=OwnerUiLayout.Text(transform,"LessonComplete","LESSON COMPLETE",45,OwnerUiLayout.TypeRole.Display);
            _complete.color=OwnerUiTheme.Current.Lime;_complete.alignment=TextAnchor.MiddleCenter;
            _complete.rectTransform.anchorMin=_complete.rectTransform.anchorMax=_complete.rectTransform.pivot=new Vector2(.5f,.5f);
            _complete.rectTransform.anchoredPosition=new Vector2(0,150);_complete.rectTransform.sizeDelta=new Vector2(790,85);_complete.enabled=false;
            var outline=_complete.gameObject.AddComponent<Outline>();outline.effectColor=UiTheme.InGameOutline;outline.effectDistance=new Vector2(2,-2);
            OwnerRow(card,"FooterGap",touch ? 0 : 22);
            // The leaving row, centred up and down in its dark box (equal room above and below).
            var footer=OwnerRow(card,"RouteControls",touch ? 152 : 92);
            var training=GetComponentInParent<GuidedTraining>();
            var skip=TrainingAction(footer,"SkipTrainingLesson","SKIP LESSON","ENTER",()=>training?.SkipFromUi(),0,330);
            _ownerSkipLabel=skip.GetComponentInChildren<Text>();_ownerSkipLabel.color=touch ? HudDraw.Brown : (Color)LiveHud.Yellow;
            // Quit on the right margin.
            var quit=TrainingAction(footer,"QuitTraining","QUIT","BACKSPACE",()=>training?.QuitFromUi(),CardWidth-64-260,260);
            quit.GetComponentInChildren<Text>().color=touch ? HudDraw.Brown : TrainingMuted;
            _ownerQuit=(RectTransform)quit.transform;
            if (touch)
            {
                var phoneHeader = OwnerRow(card, "PhoneLessonHeader", InputLayer.TouchMetrics.MinTargetUnits);
                phoneHeader.SetAsFirstSibling();
                _counter.transform.SetParent(phoneHeader, false);
                OwnerUiLayout.Place(_counter.rectTransform, 0, 0, 520, 42); _counter.alignment = TextAnchor.MiddleLeft;
                _title.transform.SetParent(phoneHeader, false);
                OwnerUiLayout.Place(_title.rectTransform, 0, 48, 520, 90);
                footer.SetParent(phoneHeader, false);
                OwnerUiLayout.Place(footer, 540, 0, 308, InputLayer.TouchMetrics.MinTargetUnits);
                OwnerUiLayout.Place((RectTransform)skip.transform, 0, 0, 144, 144);
                OwnerUiLayout.Place(_ownerQuit, 164, 0, 144, 144);
                _ownerSkipLabel.text = "SKIP";
                header.gameObject.SetActive(false);
            }
            KitFonts(card); KitFonts(_complete.transform);
            HudReadingLayout.Watch(card);
            HudReadingLayout.Watch(_complete.rectTransform);
        }
        /// <summary>The kit's type, as the F1 plates set it (owner, 2026-11-08): Darumadrop for headings and actions,
        /// Grandstander for the sentences and key words.</summary>
        private static void KitFonts(Transform root)
        {
            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                bool sentence = text.name == "LessonBody" || text.name == "Words" || text.transform.parent.name == "SkipTrainingLesson" || text.transform.parent.name == "QuitTraining";
                text.font = sentence ? TumbangPreso.UI.Hub.HubKnit.SentenceFont(text.name != "LessonBody") : TumbangPreso.UI.Hub.HubStyle.DisplayFont;
                text.fontStyle = FontStyle.Normal;
            }
        }
        private static OwnerTextAction TrainingAction(Transform parent,string name,string words,string key,
            System.Action callback,float x,float width,float scale=1)
        {
            bool touch = Application.isMobilePlatform || InputLayer.TouchHud.ForceVisible;
            var action=OwnerTextAction.Create(parent,name,words,callback,x,18,width,touch ? InputLayer.TouchMetrics.MinTargetUnits : 56,30);
            // The prompt's words in Grandstander (owner, 2026-11-08), like the key row's.
            var label=action.GetComponentInChildren<Text>();label.font=TumbangPreso.UI.Hub.HubKnit.SentenceFont(true);
            if (touch)
            {
                SettingsWorkspaceRows.Slab(action, name == "SkipTrainingLesson");
                return action;
            }
            label.alignment=TextAnchor.MiddleLeft;
            // Every key one height, every label on one line with it, centred on the key.
            KeyCap(action.transform,key);
            var cap=(RectTransform)action.transform.Find("Key_"+key);
            float capW=cap.GetComponent<LayoutElement>().preferredWidth;
            OwnerUiLayout.Place(cap,0,0,capW,56);
            OwnerUiLayout.Place(label.rectTransform,capW+14,0,width-capW-14,56);
            return action;
        }
        private static RectTransform OwnerRow(Transform parent,string name,float height)
        {
            var rect=OwnerUiLayout.Rect(parent,name);var element=rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight=element.preferredHeight=height;return rect;
        }
        /// <summary>The control's own Xelu icon, never redrawn (owner, 2026-11-08); a word only when the pack has none.</summary>
        private static void KeyCap(Transform parent,string key)
        {
            var root=OwnerUiLayout.Rect(parent,"Key_"+key);var layout=root.gameObject.AddComponent<LayoutElement>();
            var sprite=InputGlyphs.For(key,onDark:true);
            if(sprite!=null)
            {
                var image=root.gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;
                layout.preferredHeight=56;layout.preferredWidth=InputGlyphs.PromptWidth(sprite,56);return;
            }
            var face=root.gameObject.AddComponent<Image>();face.color=TrainingTrack;face.raycastTarget=false;
            var text=OwnerUiLayout.Text(root,"Cap",key,26);text.alignment=TextAnchor.MiddleCenter;text.color=TrainingInk;
            text.font=TumbangPreso.UI.Hub.HubKnit.SentenceFont(true);
            OwnerUiLayout.Fill(text.rectTransform);layout.preferredWidth=Mathf.Max(56,text.preferredWidth+20);layout.preferredHeight=56;
        }
        private static void Chip(Transform parent,string words,Color? colour=null)
        {
            // What to press reads in lime, the colour of doing (owner, 2026-11-08: colour variety in the important words).
            var text=OwnerUiLayout.Text(parent,"Words",words,30);text.color=TrainingDone;text.font=TumbangPreso.UI.Hub.HubKnit.SentenceFont(true);
            text.horizontalOverflow=HorizontalWrapMode.Overflow;text.alignment=TextAnchor.MiddleCenter;
            var layout=text.gameObject.AddComponent<LayoutElement>();layout.preferredWidth=text.preferredWidth+10;layout.preferredHeight=56;
        }
    }
}
