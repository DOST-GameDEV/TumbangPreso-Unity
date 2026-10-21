using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    // Keep physical-control leaders connected to the row that owns keyboard/mouse focus.
    public sealed class ControllerCalloutButton : Button
    {
        private Graphic[] _leaders;
        private GameObject _target;
        private bool _focused;
        public void BindLeaders(Graphic[] leaders,GameObject target)
        { _leaders=leaders;_target=target;Paint(); }
        protected override void DoStateTransition(SelectionState state,bool instant)
        {
            _focused=state==SelectionState.Highlighted || state==SelectionState.Selected || state==SelectionState.Pressed;
            Paint();
        }
        private void Paint()
        {
            var face=GetComponent<ControllerCalloutFace>();
            if(face!=null){face.Focused=_focused;face.Available=IsInteractable();face.SetVerticesDirty();}
            if(_leaders!=null)foreach(var line in _leaders)
                if(line!=null)line.color=_focused?SettingsPalette.Accent:new Color32(146,139,131,255);
            if(_target!=null)_target.SetActive(_focused);
        }
    }
}
