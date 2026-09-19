using System;
using UnityEngine;

namespace TumbangPreso.UI
{
    public sealed class OwnerTextAction : UnityEngine.UI.Button
    {
        public OwnerUiMotion FeedbackMotion;

        /// <summary>
        /// ⚠️⚠️ EVERY OWNER BUTTON WAS SILENT UNTIL IT WAS PRESSED, AND THE WHOLE
        /// FRONT END IS OWNER BUTTONS NOW. `MenuSfx.Hover` has existed since the conversion and
        /// its own header says why: *"every control in the Godot build makes a noise and the
        /// conversion was silent"*. Four classes call it, and all four are the retired wood and
        /// pennant controls. Nothing she drew makes a sound when you move onto it, so the login
        /// answers a press and ignores everything up to it. 🧑: *"atleast sounds when u
        /// interact with shit"*.
        ///
        /// ⚠️⚠️ IT IS HERE RATHER THAN AT THE CALL SITES, WHICH IS `CLAUDE.md` § 4A'S RULE.
        /// A screen added next month cannot forget a sound it never has to remember, and
        /// `MenuSfx` already guarantees one cue per frame, so a control that also plays its own
        /// press sound cannot double up.
        ///
        /// ⚠️ SELECTED COUNTS AS WELL AS HIGHLIGHTED, BECAUSE A PAD HAS NO POINTER. Moving
        /// the stick down a column is the same act as moving the mouse across it and has to
        /// sound the same, which is the third question `CLAUDE.md` § 4a makes you answer out
        /// loud for anything new.
        ///
        /// ⚠️⚠️ AND THE FIRST THIRD OF A SECOND IS SILENT, WHICH IS NOT A FUDGE. Unity sends
        /// a state transition when a Selectable is enabled, and `ScreenFocus.Rebuild` selects the
        /// first control the moment a screen opens: without this the login would open on a
        /// chord of every button on it announcing itself at once.
        /// </summary>
        private float _quietUntil;

        private bool _wasOver;

        protected override void OnEnable()
        {
            base.OnEnable();
            _quietUntil = Time.unscaledTime + .3f;
            _wasOver = false;
        }

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
            bool over=state==SelectionState.Highlighted || state==SelectionState.Selected;
            if(over && !_wasOver && interactable && Application.isPlaying
               && Time.unscaledTime>=_quietUntil)MenuSfx.Hover();
            _wasOver=over;
            var motion=FeedbackMotion!=null?FeedbackMotion:GetComponentInChildren<OwnerUiMotion>();
            motion?.SetState(over,state==SelectionState.Pressed,state==SelectionState.Disabled);
        }
    }
}
