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
            // Standard HubButton keeps pointer/touch/Submit, sounds and focus feedback.
            return HubKit.IconButton(parent,name,mark,HubStyle.Honey,clicked,seed);
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
