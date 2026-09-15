using System;
using UnityEngine;

namespace TumbangPreso.UI
{
    public sealed class OwnerTermsView : MonoBehaviour
    {
        private Canvas _canvas;
        private Action<bool> _closed;
        public bool IsOpen=>_canvas!=null && _canvas.gameObject.activeSelf;
        public static OwnerTermsView Open(Transform owner,Action<bool> closed)
        {
            var node=new GameObject("OwnerTermsView");node.transform.SetParent(owner,false);
            var view=node.AddComponent<OwnerTermsView>();view.Build(closed);return view;
        }
        private void Build(Action<bool> closed)
        {
            _closed=closed;_canvas=OwnerUiLayout.Canvas(transform,"OwnerTermsCanvas",950);
            ScreenTakeover.Register(this,()=>IsOpen);
            var scrim=OwnerUiLayout.Rect(_canvas.transform,"ModalBlocker").gameObject.AddComponent<UnityEngine.UI.Image>();
            OwnerUiLayout.Fill(scrim.rectTransform);scrim.color=new Color(0,0,0,.24f);scrim.raycastTarget=true;
            var design=OwnerUiLayout.DesignArea(_canvas.transform,"TermsComposition");
            var paper=OwnerUiLayout.Rect(design,"ReadingSheet").gameObject.AddComponent<OwnerUiPaper>();paper.Style=OwnerUiPaper.Treatment.Dialog;
            OwnerUiLayout.Place(paper.rectTransform,450,95,1020,890);paper.raycastTarget=true;
            var title=OwnerUiLayout.Text(design,"Title","Terms & Conditions",46,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform,505,140,910,70);title.alignment=TextAnchor.MiddleCenter;title.color=OwnerUiTheme.Current.ActionInk;
            var scroll=OwnerUiLayout.Rect(design,"TermsScroll").gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            OwnerUiLayout.Place((RectTransform)scroll.transform,510,230,900,557);
            scroll.horizontal=false;scroll.vertical=true;scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity=35;
            var viewport=OwnerUiLayout.Rect(scroll.transform,"Viewport");OwnerUiLayout.Fill(viewport);
            viewport.gameObject.AddComponent<UnityEngine.UI.Image>();var mask=viewport.gameObject.AddComponent<UnityEngine.UI.Mask>();mask.showMaskGraphic=false;
            var content=OwnerUiLayout.Rect(viewport,"Content");content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;
            content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
            var layout=content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing=6;layout.childControlWidth=true;layout.childControlHeight=true;
            layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
            var fit=content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();fit.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport;scroll.content=content;
            var document=Resources.Load<TextAsset>("UI/owner-painted/play-terms");
            string words=document!=null?document.text:"";
            foreach(var block in words.Replace("\r","").Split(new[]{"\n\n"},StringSplitOptions.RemoveEmptyEntries))
            {
                int split=block.IndexOf('\n');
                string heading=split>=0?block.Substring(0,split):block;
                var label=OwnerUiLayout.Text(content,"SectionTitle",heading,28,OwnerUiLayout.TypeRole.Display);
                label.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight=52;
                if(split<0)continue;
                var body=OwnerUiLayout.Text(content,"SectionText",block.Substring(split+1).Trim(),28);
                body.color=OwnerUiTheme.Current.EnteredInk;body.verticalOverflow=VerticalWrapMode.Overflow;
            }
            var agree=OwnerPaintedAction.Create(design,"AcceptGuidelines","I AGREE",()=>Close(true),false,48);
            OwnerUiLayout.Place((RectTransform)agree.transform,755,817,413,91);
            var back=OwnerUiLayout.Rect(design,"TermsBack");OwnerUiLayout.Place(back,600,919,720,46);
            var hit=back.gameObject.AddComponent<UnityEngine.UI.Image>();hit.color=Color.clear;
            var button=back.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=hit;button.transition=UnityEngine.UI.Selectable.Transition.None;
            button.onClick.AddListener(()=>Close(false));
            var text=OwnerUiLayout.Text(back,"Label","BACK",28,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Fill(text.rectTransform);text.alignment=TextAnchor.MiddleCenter;
            Canvas.ForceUpdateCanvases();UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scroll.verticalNormalizedPosition=1;_canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }
        private void Update()
        {
            if(IsOpen && InputLayer.MenuNav.CancelPressed){ScreenTakeover.ConsumeEscape();Close(false);}
        }
        private void Close(bool accepted)
        {
            if(!IsOpen)return;_canvas.gameObject.SetActive(false);ScreenTakeover.ConsumeEscape();
            _closed?.Invoke(accepted);Destroy(gameObject);
        }
        private void OnDestroy()=>ScreenTakeover.Unregister(this);
    }
}
