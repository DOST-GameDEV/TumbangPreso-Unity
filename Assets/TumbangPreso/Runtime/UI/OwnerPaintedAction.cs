using System;
using UnityEngine;

namespace TumbangPreso.UI
{
    public sealed class OwnerPaintedAction : UnityEngine.UI.Button
    {
        private OwnerUiMotion _motion;
        public static OwnerPaintedAction Create(Transform parent,string name,string words,Action click,bool orange=false,int fontSize=54)
        {
            var root=OwnerUiLayout.Rect(parent,name);root.sizeDelta=new Vector2(413,91);
            var hit=root.gameObject.AddComponent<UnityEngine.UI.Image>();hit.color=Color.clear;hit.raycastTarget=true;
            var button=root.gameObject.AddComponent<OwnerPaintedAction>();button.targetGraphic=hit;
            button.transition=Transition.None;
            var piece=orange?OwnerUiTheme.Piece.OrangeAction:OwnerUiTheme.Piece.LimeAction;
            var art=OwnerUiLayout.Art(root,"PaintedArtwork",piece);OwnerUiLayout.Fill(art.rectTransform);
            var label=OwnerUiLayout.Text(art.transform,"Label",words,fontSize,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Fill(label.rectTransform);label.rectTransform.offsetMin=new Vector2(14,10);
            label.rectTransform.offsetMax=new Vector2(-14,-3);label.alignment=TextAnchor.MiddleCenter;
            // Darumadrop's line metrics exceed its painted glyph height. Do not
            // truncate the whole large word inside a correctly sized art button.
            label.verticalOverflow=VerticalWrapMode.Overflow;
            label.horizontalOverflow=HorizontalWrapMode.Overflow;
            label.color=orange?OwnerUiTheme.Current.GuestInk:OwnerUiTheme.Current.ActionInk;
            button._motion=art.gameObject.AddComponent<OwnerUiMotion>();
            if(click!=null)button.onClick.AddListener(()=>{MenuSfx.Click();click();});
            return button;
        }
        protected override void DoStateTransition(SelectionState state,bool instant)
        {
            if(_motion==null)_motion=GetComponentInChildren<OwnerUiMotion>();
            _motion?.SetState(state==SelectionState.Highlighted || state==SelectionState.Selected,
                state==SelectionState.Pressed,state==SelectionState.Disabled);
        }
    }
}
