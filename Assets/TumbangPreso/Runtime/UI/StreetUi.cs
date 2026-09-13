using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public static class StreetUi
    {
        /// <summary>Reskin a wired control in place; retain its listeners, navigation and identity.</summary>
        public static StreetGraphic Restyle(Button button, StreetGraphic.Surface style)
        {
            if (button == null) return null;
            var oldFeedback = button.GetComponent<PaperButton>();
            if (oldFeedback != null) oldFeedback.enabled = false;
            var oldSkin = button.GetComponent<PaperSkin>();
            if (oldSkin != null) oldSkin.enabled = false;
            var oldWood = button.GetComponent<GodotButton>();
            if (oldWood != null) oldWood.enabled = false;
            var oldArrow = button.GetComponent<ArrowButtonView>();
            if (oldArrow != null) oldArrow.enabled = false;
            var oldImage = button.GetComponent<Image>();
            if (oldImage != null) { oldImage.enabled = false; oldImage.raycastTarget = false; }
            foreach (string layer in new[] { "Face", "Shadow" })
            {
                var child = button.transform.Find(layer);
                if (child != null) child.gameObject.SetActive(false);
            }
            var surface = button.GetComponent<StreetGraphic>()
                ?? button.transform.Find("BrandSurface")?.GetComponent<StreetGraphic>();
            if (surface == null)
            {
                surface = Detail(button.transform, "BrandSurface", style);
                surface.transform.SetAsFirstSibling();
                MenuKit.Stretch(surface.rectTransform);
            }
            surface.Style = style;
            surface.raycastTarget = true;
            button.targetGraphic = surface;
            button.transition = Selectable.Transition.None;
            button.transform.localScale = Vector3.one;
            foreach (var label in button.GetComponentsInChildren<Text>(true))
            {
                MenuKit.Read(label, label.name == "Label");
                label.color = style == StreetGraphic.Surface.Action ? UiTheme.Paper : UiTheme.PaperInk;
                label.raycastTarget = false;
                var outline = label.GetComponent<GodotOutline>();
                if (outline != null) outline.enabled = false;
            }
            var feedback = button.GetComponent<StreetControl>();
            if (feedback == null) feedback = button.gameObject.AddComponent<StreetControl>();
            feedback.Bind(button, surface);
            surface.SetVerticesDirty();
            return surface;
        }

        public static Button Button(Transform parent, string name, string words, int size,
                                    StreetGraphic.Surface style)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(StreetGraphic),typeof(Button));
            go.transform.SetParent(parent,false);
            var surface=go.GetComponent<StreetGraphic>(); surface.Style=style;
            var button=go.GetComponent<Button>(); button.targetGraphic=surface;
            button.transition=Selectable.Transition.None;
            var label=MenuKit.Label(go.transform,words,size,style==StreetGraphic.Surface.Action?UiTheme.Paper:UiTheme.PaperInk,Vector2.zero,Vector2.zero,Vector2.zero);
            label.name="Label"; label.raycastTarget=false;
            MenuKit.Apply(label,style==StreetGraphic.Surface.Action?MenuKit.Face.Display:MenuKit.Face.Body,true);
            MenuKit.Stretch(label.rectTransform,-12);
            go.AddComponent<StreetControl>().Bind(button, surface);
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
