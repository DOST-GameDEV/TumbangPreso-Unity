using System;
using UnityEngine;

namespace TumbangPreso.UI
{
    public sealed class OwnerTextAction : UnityEngine.UI.Button
    {
        public OwnerUiMotion FeedbackMotion;
        public static OwnerTextAction Create(Transform parent,string name,string words,Action click,
            float x,float y,float width,float height,int size=30)
        {
            var root=OwnerUiLayout.Rect(parent,name);OwnerUiLayout.Place(root,x,y,width,height);
            var hit=root.gameObject.AddComponent<UnityEngine.UI.Image>();hit.color=Color.clear;
            var action=root.gameObject.AddComponent<OwnerTextAction>();action.targetGraphic=hit;action.transition=Transition.None;
            var label=OwnerUiLayout.Text(root,"Label",words,size,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Fill(label.rectTransform);label.alignment=TextAnchor.MiddleCenter;
            label.color=OwnerUiTheme.Current.ActionInk;label.gameObject.AddComponent<OwnerUiMotion>();
            if(click!=null)action.onClick.AddListener(()=>{MenuSfx.Click();click();});
            return action;
        }
        protected override void DoStateTransition(SelectionState state,bool instant)
        {
            var motion=FeedbackMotion!=null?FeedbackMotion:GetComponentInChildren<OwnerUiMotion>();
            motion?.SetState(state==SelectionState.Highlighted || state==SelectionState.Selected,
                state==SelectionState.Pressed,state==SelectionState.Disabled);
        }
    }
}
