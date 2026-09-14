using UnityEngine;

namespace TumbangPreso.UI
{
    public static class OwnerScrollColumn
    {
        public static RectTransform Build(Transform parent,string name,Rect bounds,out UnityEngine.UI.ScrollRect scroll)
        {
            var root=OwnerUiLayout.Rect(parent,name);OwnerUiLayout.Place(root,bounds.x,bounds.y,bounds.width,bounds.height);
            scroll=root.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();scroll.horizontal=false;scroll.vertical=true;
            scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=36;
            var viewport=OwnerUiLayout.Rect(root,"Viewport");OwnerUiLayout.Fill(viewport);viewport.offsetMax=new Vector2(-24,0);
            viewport.gameObject.AddComponent<UnityEngine.UI.Image>();viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=false;
            var content=OwnerUiLayout.Rect(viewport,"Content");content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;
            content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
            var layout=content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing=16;layout.padding=new RectOffset(0,8,0,56);layout.childControlWidth=true;layout.childControlHeight=true;
            layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
            content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport;scroll.content=content;
            var track=OwnerUiLayout.Rect(root,"Scrollbar");OwnerUiLayout.Place(track,bounds.width-12,0,10,bounds.height);
            var rail=track.gameObject.AddComponent<UnityEngine.UI.Image>();var ink=OwnerUiTheme.Current.ActionInk;
            rail.color=new Color(ink.r,ink.g,ink.b,.14f);
            var bar=track.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();bar.direction=UnityEngine.UI.Scrollbar.Direction.BottomToTop;
            var area=OwnerUiLayout.Rect(track,"SlidingArea");OwnerUiLayout.Fill(area);
            var handle=OwnerUiLayout.Rect(area,"Handle").gameObject.AddComponent<UnityEngine.UI.Image>();
            OwnerUiLayout.Fill(handle.rectTransform);handle.color=ink;
            bar.targetGraphic=handle;bar.handleRect=handle.rectTransform;bar.transition=UnityEngine.UI.Selectable.Transition.None;
            scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
            return content;
        }
    }
}
