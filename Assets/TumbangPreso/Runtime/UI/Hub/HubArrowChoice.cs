using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>Previous/current/next choices on the existing room form, with no popup.</summary>
    public sealed class HubArrowChoice : MonoBehaviour
    {
        public string[] Options { get; private set; } = Array.Empty<string>();
        public int Value { get; private set; }
        public event Action<int> Changed;
        public HubButton Previous { get; private set; }
        public HubButton Next { get; private set; }
        private Text _label;

        public static HubArrowChoice Build(Transform parent,string name,string[] options,int value,int seed)
        {
            var root=HubKit.Rect(parent,name+"Choice");
            var choice=root.gameObject.AddComponent<HubArrowChoice>();
            choice.Options=options ?? Array.Empty<string>();
            var plate=HubKit.Shape(root,"Sticker",HubStyle.Honey,true,seed);
            HubKit.Stretch(plate.rectTransform);
            choice._label=HubKit.Text(root,"CurrentValue","",HubStyle.Label,true,HubStyle.Ink,TextAnchor.MiddleCenter);
            HubKit.Stretch(choice._label.rectTransform);
            choice._label.rectTransform.offsetMin=new Vector2(72,6);
            choice._label.rectTransform.offsetMax=new Vector2(-72,-6);
            choice.Previous=Arrow(root,name+"Previous",HubGlyph.Mark.Left,()=>choice.Step(-1),seed+20);
            choice.Next=Arrow(root,name+"Next",HubGlyph.Mark.Right,()=>choice.Step(1),seed+21);
            HubKit.Place((RectTransform)choice.Previous.transform,HubKit.Left,new Vector2(4,0),new Vector2(64,80));
            HubKit.Place((RectTransform)choice.Next.transform,HubKit.Right,new Vector2(-4,0),new Vector2(64,80));
            choice.Set(value,false);
            return choice;
        }

        private static HubButton Arrow(Transform parent,string name,HubGlyph.Mark mark,Action clicked,int seed)
        {
            // An inline chevron keeps the existing field as one surface. The
            // standard button still owns pointer/touch/Submit and menu sounds.
            var root=HubKit.Rect(parent,name);
            var hit=root.gameObject.AddComponent<Image>(); hit.color=Color.clear;
            var button=root.gameObject.AddComponent<HubButton>();
            button.Body=HubKit.Stretch(HubKit.Rect(root,"Body"));
            var arrow=HubKit.Glyph(button.Body,"Arrow",mark,Color.white,.12f);
            HubKit.Place(arrow.rectTransform,HubKit.Centre,Vector2.zero,new Vector2(42,42));
            button.targetGraphic=arrow; button.transition=Selectable.Transition.ColorTint;
            var colours=button.colors;
            colours.normalColor=HubStyle.Ink;
            colours.highlightedColor=colours.selectedColor=Color.Lerp(HubStyle.Persimmon,HubStyle.Ink,.45f);
            colours.pressedColor=HubStyle.Ink;
            var disabled=HubStyle.Ink; disabled.a=.35f; colours.disabledColor=disabled;
            colours.fadeDuration=.08f; button.colors=colours;
            button.onClick.AddListener(()=>clicked());
            return button;
        }

        private void Step(int direction)
        {
            if(Options.Length<2) return;
            Set((Value+direction+Options.Length)%Options.Length,true);
        }

        public void Set(int value,bool notify)
        {
            int previous=Value;
            Value=Options.Length==0?0:Mathf.Clamp(value,0,Options.Length-1);
            _label.text=Options.Length==0?"":Options[Value];
            _label.fontSize=HubStyle.Size(HubStyle.Label); HubKit.Fit(_label);
            Previous.interactable=Next.interactable=Options.Length>1;
            if(notify && previous!=Value) Changed?.Invoke(Value);
        }
    }
}
