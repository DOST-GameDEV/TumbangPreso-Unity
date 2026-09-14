using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    // Settings-specific rows: quiet labels with a discrete control area. Never used as a menu/HUD skin.
    public static class OwnerSettingsRows
    {
        public static RectTransform Row(Transform parent,string name,string label)
        {
            var root=OwnerUiLayout.Rect(parent,name);root.gameObject.AddComponent<LayoutElement>().preferredHeight=96;
            var text=OwnerUiLayout.Text(root,"Label",label,31,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(text.rectTransform,14,9,822,78);
            var controls=OwnerUiLayout.Rect(root,"Control");
            controls.anchorMin=controls.anchorMax=controls.pivot=new Vector2(1,.5f);
            controls.anchoredPosition=new Vector2(-22,0);controls.sizeDelta=new Vector2(533,78);return controls;
        }
        public static void Toggle(Transform parent,string name,bool value,Action<bool> changed)
        {
            var root=OwnerUiLayout.Rect(parent,name);OwnerUiLayout.Place(root,0,0,533,78);
            var hit=root.gameObject.AddComponent<Image>();hit.color=Color.clear;
            var toggle=root.gameObject.AddComponent<Toggle>();toggle.targetGraphic=hit;toggle.transition=Selectable.Transition.None;
            var box=OwnerUiLayout.Art(root,"CheckBox",OwnerUiTheme.Piece.Checkbox);
            OwnerUiLayout.Place(box.rectTransform,38,27,21,24);
            var check=OwnerUiGlyph.Create(root,"Check",OwnerUiGlyph.Mark.Check,OwnerUiTheme.Current.Green);
            OwnerUiLayout.Place(check.rectTransform,39,29,18,20);toggle.graphic=check;toggle.SetIsOnWithoutNotify(value);
            var label=OwnerUiLayout.Text(root,"Value",value?"ON":"OFF",30,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(label.rectTransform,93,5,400,67);
            toggle.onValueChanged.AddListener(on=>{label.text=on?"ON":"OFF";MenuSfx.Click();changed(on);});
        }
        public static Slider Slider(Transform parent,string name,float current,float minimum,float maximum,Action<float> changed,Func<float,string> show)
        {
            var root=OwnerUiLayout.Rect(parent,name);OwnerUiLayout.Place(root,0,0,533,78);
            var hit=root.gameObject.AddComponent<Image>();hit.color=Color.clear;
            var slider=root.gameObject.AddComponent<Slider>();slider.minValue=minimum;slider.maxValue=maximum;
            slider.transition=Selectable.Transition.None;
            var rail=OwnerUiLayout.Rect(root,"Track").gameObject.AddComponent<Image>();rail.color=OwnerUiTheme.Current.Peach;
            OwnerUiLayout.Place(rail.rectTransform,12,32,365,13);rail.raycastTarget=false;
            var area=OwnerUiLayout.Rect(rail.transform,"FillArea");OwnerUiLayout.Fill(area);
            var fill=OwnerUiLayout.Rect(area,"Fill").gameObject.AddComponent<Image>();fill.color=OwnerUiTheme.Current.Green;fill.raycastTarget=false;
            OwnerUiLayout.Fill(fill.rectTransform);slider.fillRect=fill.rectTransform;
            var handles=OwnerUiLayout.Rect(root,"HandleArea");OwnerUiLayout.Place(handles,12,9,365,58);
            var handle=OwnerUiLayout.Rect(handles,"Handle").gameObject.AddComponent<Image>();
            // Slider stretches the non-moving axis; inset it instead of adding height to the whole handle area.
            handle.color=OwnerUiTheme.Current.Green;handle.rectTransform.sizeDelta=new Vector2(25,-20);handle.raycastTarget=false;
            slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;
            var value=OwnerUiLayout.Text(root,"Value",show(current),29,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(value.rectTransform,408,5,125,67);value.alignment=TextAnchor.MiddleCenter;
            slider.SetValueWithoutNotify(current);slider.onValueChanged.AddListener(v=>{value.text=show(v);changed(v);});return slider;
        }
    }
}
