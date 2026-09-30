using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso
{
    public sealed partial class GuidedTrainingHud
    {
        private Text _ownerSkipLabel;
        private RectTransform _ownerQuit;
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
        private static readonly Color TrainingInk=new Color32(244,238,219,255);
        private static readonly Color TrainingMuted=new Color32(180,202,212,255);
        private static readonly Color TrainingTrack=new Color32(74,87,101,255);
        private static readonly Color TrainingDone=new Color32(170,204,130,255);
        private static readonly Color TrainingCurrent=new Color32(237,191,104,255);
        private void BuildUi()
        {
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting=true;canvas.sortingOrder=240;canvas.vertexColorAlwaysGammaSpace=true;canvas.pixelPerfect=true;
            gameObject.AddComponent<OwnerUiCanvas>();gameObject.AddComponent<OwnerUiOverrides>();gameObject.AddComponent<GraphicRaycaster>();InputLayer.UiInputModule.Ensure();
            var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=OwnerUiTheme.Current.ReferenceResolution;scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var card=OwnerUiLayout.Rect(transform,"ObjectiveCard");OwnerUiLayout.Place(card,36,36,CardWidth,0);
            var panel=card.gameObject.AddComponent<Image>();panel.color=new Color32(28,42,58,242);panel.raycastTarget=false;
            var edge=OwnerUiLayout.Rect(card,"TrainingEdge").gameObject.AddComponent<Image>();
            edge.gameObject.AddComponent<LayoutElement>().ignoreLayout=true;edge.raycastTarget=false;edge.color=TrainingMuted;
            edge.rectTransform.anchorMin=Vector2.zero;edge.rectTransform.anchorMax=new Vector2(0,1);
            edge.rectTransform.offsetMin=Vector2.zero;edge.rectTransform.offsetMax=new Vector2(5,0);
            var column=card.gameObject.AddComponent<VerticalLayoutGroup>();column.spacing=12;
            column.padding=new RectOffset(26,26,24,22);column.childControlWidth=column.childControlHeight=true;
            column.childForceExpandWidth=true;column.childForceExpandHeight=false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var header=OwnerRow(card,"TrainingHeader",52);
            var word=OwnerUiLayout.Text(header,"TrainingWord","TRAINING",30,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(word.rectTransform,0,0,330,52);word.color=TrainingMuted;
            _counter=OwnerUiLayout.Text(header,"LessonCounter","01 / " + GuidedTraining.LessonCount,30,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_counter.rectTransform,350,0,285,52);_counter.alignment=TextAnchor.MiddleRight;_counter.color=TrainingMuted;_counter.verticalOverflow=VerticalWrapMode.Overflow;
            var rail=OwnerRow(card,"RouteRail",8);var track=rail.gameObject.AddComponent<HorizontalLayoutGroup>();
            track.spacing=4;track.childControlWidth=track.childControlHeight=true;track.childForceExpandWidth=track.childForceExpandHeight=true;
            for(int i=0;i<GuidedTraining.LessonCount;i++)
            {
                var pip=OwnerUiLayout.Rect(rail,"Pip"+i).gameObject.AddComponent<Image>();
                pip.color=TrainingTrack;pip.raycastTarget=false;_pips.Add(pip);
            }
            _title=OwnerUiLayout.Text(card,"LessonTitle","TRAINING",42,OwnerUiLayout.TypeRole.Display);
            _title.color=TrainingInk;_title.gameObject.AddComponent<LayoutElement>().preferredHeight=62;_title.verticalOverflow=VerticalWrapMode.Overflow;
            _body=OwnerUiLayout.Text(card,"LessonBody","",30);_body.color=TrainingInk;
            _body.alignment=TextAnchor.UpperLeft;_body.verticalOverflow=VerticalWrapMode.Overflow;
            _body.gameObject.AddComponent<LayoutElement>().minHeight=32;
            _keyRow=OwnerRow(card,"KeyRow",72);var keys=_keyRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            keys.childControlHeight=keys.childControlWidth=true;keys.childForceExpandHeight=keys.childForceExpandWidth=false;keys.spacing=9;
            var progress=OwnerRow(card,"ProgressBack",10).gameObject.AddComponent<Image>();progress.color=TrainingTrack;progress.raycastTarget=false;
            _fill=OwnerUiLayout.Rect(progress.transform,"ProgressFill").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(_fill.rectTransform);_fill.color=TrainingDone;_fill.raycastTarget=false;
            _fill.rectTransform.anchorMax=new Vector2(0,1);
            _complete=OwnerUiLayout.Text(transform,"LessonComplete","LESSON COMPLETE",45,OwnerUiLayout.TypeRole.Display);
            _complete.color=OwnerUiTheme.Current.Lime;_complete.alignment=TextAnchor.MiddleCenter;
            _complete.rectTransform.anchorMin=_complete.rectTransform.anchorMax=_complete.rectTransform.pivot=new Vector2(.5f,.5f);
            _complete.rectTransform.anchoredPosition=new Vector2(0,150);_complete.rectTransform.sizeDelta=new Vector2(790,85);_complete.enabled=false;
            var outline=_complete.gameObject.AddComponent<Outline>();outline.effectColor=UiTheme.InGameOutline;outline.effectDistance=new Vector2(2,-2);
            var footer=OwnerRow(card,"RouteControls",76);
            var training=GetComponentInParent<GuidedTraining>();
            var skip=TrainingAction(footer,"SkipTrainingLesson","SKIP LESSON","ENTER",()=>training?.SkipFromUi(),0,300);
            _ownerSkipLabel=skip.GetComponentInChildren<Text>();_ownerSkipLabel.color=TrainingCurrent;
            var quit=TrainingAction(footer,"QuitTraining","QUIT","BACKSPACE",()=>training?.QuitFromUi(),320,318);
            quit.GetComponentInChildren<Text>().color=TrainingMuted;
            _ownerQuit=(RectTransform)quit.transform;
            HudReadingLayout.Watch(card);
            HudReadingLayout.Watch(_complete.rectTransform);
        }
        private static OwnerTextAction TrainingAction(Transform parent,string name,string words,string key,
            System.Action callback,float x,float width)
        {
            var action=OwnerTextAction.Create(parent,name,words,callback,x,0,width,76,30);
            var label=action.GetComponentInChildren<Text>();label.font=OwnerUiTheme.Current.Display;
            label.alignment=TextAnchor.MiddleLeft;
            OwnerUiLayout.Place(label.rectTransform,88,0,width-96,76);
            KeyCap(action.transform,key);
            OwnerUiLayout.Place((RectTransform)action.transform.Find("Key_"+key),8,2,72,72);
            return action;
        }
        private static RectTransform OwnerRow(Transform parent,string name,float height)
        {
            var rect=OwnerUiLayout.Rect(parent,name);var element=rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight=element.preferredHeight=height;return rect;
        }
        private static void KeyCap(Transform parent,string key)
        {
            var root=OwnerUiLayout.Rect(parent,"Key_"+key);var layout=root.gameObject.AddComponent<LayoutElement>();
            if(key=="WHEEL UP" || key=="WHEEL DOWN")
            {
                var wheel=root.gameObject.AddComponent<TrainingWheelGlyph>();
                wheel.Up=key=="WHEEL UP";wheel.raycastTarget=false;
                layout.preferredWidth=layout.preferredHeight=72;return;
            }
            var sprite=InputGlyphs.For(key,onDark:true);
            if(sprite!=null)
            {
                var image=root.gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;
                layout.preferredWidth=layout.preferredHeight=72;return;
            }
            var face=root.gameObject.AddComponent<Image>();face.color=TrainingTrack;face.raycastTarget=false;
            var text=OwnerUiLayout.Text(root,"Cap",key,30,OwnerUiLayout.TypeRole.Display);text.alignment=TextAnchor.MiddleCenter;text.color=TrainingInk;
            OwnerUiLayout.Fill(text.rectTransform);layout.preferredWidth=Mathf.Max(72,text.preferredWidth+20);layout.preferredHeight=72;
        }
        private static void Chip(Transform parent,string words,Color? colour=null)
        {
            var text=OwnerUiLayout.Text(parent,"Words",words,30);text.color=TrainingInk;
            text.horizontalOverflow=HorizontalWrapMode.Overflow;text.alignment=TextAnchor.MiddleCenter;
            var layout=text.gameObject.AddComponent<LayoutElement>();layout.preferredWidth=text.preferredWidth+10;layout.preferredHeight=72;
        }
        // Original vector mouse/scroll marks, kept readable beside the Xelu keys.
        // Only actual wheel bindings use these; rebound controls retain their own glyph.
        [RequireComponent(typeof(CanvasRenderer))]
        private sealed class TrainingWheelGlyph : MaskableGraphic
        {
            public bool Up;
            protected override void OnPopulateMesh(VertexHelper h)
            {
                h.Clear();var r=GetPixelAdjustedRect();
                var rim=new Color32(97,99,101,255);var face=new Color32(239,240,238,255);
                Ellipse(h,r,.38f,.48f,.29f,.44f,rim);
                Ellipse(h,r,.38f,.48f,.255f,.405f,face);
                Box(h,r,.12f,.62f,.64f,.65f,rim);
                Box(h,r,.365f,.64f,.395f,.87f,rim);
                Box(h,r,.33f,.66f,.43f,.80f,rim);
                Box(h,r,.355f,.685f,.405f,.775f,face);
                Box(h,r,.775f,.31f,.865f,.66f,rim);
                Box(h,r,.80f,.32f,.84f,.65f,face);
                float tip=Up ? .86f : .12f,baseY=Up ? .62f : .36f;
                Tri(h,r,new Vector2(.64f,baseY),new Vector2(.99f,baseY),new Vector2(.82f,tip),rim);
                Tri(h,r,new Vector2(.705f,baseY+(Up ? .025f : -.025f)),
                    new Vector2(.925f,baseY+(Up ? .025f : -.025f)),new Vector2(.82f,tip+(Up ? -.055f : .055f)),face);
            }
            private static void Ellipse(VertexHelper h,Rect r,float x,float y,float rx,float ry,Color32 c)
            {
                int start=h.currentVertCount;Add(h,r,x,y,c);
                for(int i=0;i<=24;i++)
                {float a=i*Mathf.PI*2/24;Add(h,r,x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry,c);}
                for(int i=0;i<24;i++)h.AddTriangle(start,start+i+1,start+i+2);
            }
            private static void Box(VertexHelper h,Rect r,float x0,float y0,float x1,float y1,Color32 c)
            {
                int start=h.currentVertCount;Add(h,r,x0,y0,c);Add(h,r,x1,y0,c);Add(h,r,x1,y1,c);Add(h,r,x0,y1,c);
                h.AddTriangle(start,start+1,start+2);h.AddTriangle(start,start+2,start+3);
            }
            private static void Tri(VertexHelper h,Rect r,Vector2 a,Vector2 b,Vector2 c,Color32 tint)
            {
                int start=h.currentVertCount;Add(h,r,a.x,a.y,tint);Add(h,r,b.x,b.y,tint);Add(h,r,c.x,c.y,tint);
                h.AddTriangle(start,start+1,start+2);
            }
            private static void Add(VertexHelper h,Rect r,float x,float y,Color32 c)
                =>h.AddVert(new Vector3(r.x+x*r.width,r.y+y*r.height),c,Vector2.zero);
        }
    }
}
