using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public static class StreetUi
    {
        public static Button Button(Transform parent, string name, string words, int size,
                                    StreetGraphic.Surface style)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(StreetGraphic),typeof(Button));
            go.transform.SetParent(parent,false);
            var surface=go.GetComponent<StreetGraphic>(); surface.Style=style;
            var button=go.GetComponent<Button>(); button.targetGraphic=surface;
            button.transition=Selectable.Transition.None;
            var label=MenuKit.Label(go.transform,words,size,UiTheme.PaperInk,Vector2.zero,Vector2.zero,Vector2.zero);
            label.name="Label"; label.raycastTarget=false;
            MenuKit.Apply(label,MenuKit.Face.Accent);
            MenuKit.Stretch(label.rectTransform,-12);
            return button;
        }
        public static StreetIcon Icon(Transform parent, StreetIcon.Glyph kind, Vector2 anchor,
                                      Vector2 position, Vector2 size)
        {
            var go=new GameObject(kind+"Icon",typeof(RectTransform),typeof(StreetIcon));
            go.transform.SetParent(parent,false);
            var icon=go.GetComponent<StreetIcon>(); icon.Kind=kind; icon.color=UiTheme.PaperInk;
            icon.raycastTarget=false; MenuKit.Place(icon.rectTransform,anchor,position,size);
            return icon;
        }
        public static StreetGraphic Detail(Transform parent,string name,StreetGraphic.Surface style)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(StreetGraphic));
            go.transform.SetParent(parent,false);
            var graphic=go.GetComponent<StreetGraphic>(); graphic.Style=style; graphic.raycastTarget=false;
            return graphic;
        }
    }
}
