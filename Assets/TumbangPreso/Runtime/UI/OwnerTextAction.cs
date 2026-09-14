using UnityEngine;

namespace TumbangPreso.UI
{
    public sealed class OwnerTextAction : UnityEngine.UI.Button
    {
        protected override void DoStateTransition(SelectionState state,bool instant)
        {
            var motion=GetComponentInChildren<OwnerUiMotion>();
            motion?.SetState(state==SelectionState.Highlighted || state==SelectionState.Selected,
                state==SelectionState.Pressed,state==SelectionState.Disabled);
        }
    }
}
