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
        /// <summary>The thin-slab kit face and the words on it: plain, yellow while focused.</summary>
        public UnityEngine.UI.Image Kit;
        public UnityEngine.UI.Text Words;
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
            if(Kit!=null)
            {
                var kind=_focused?Hub.Knit.ThinYellow:Hub.Knit.ThinPlain;
                Hub.HubKnit.KitFace(Kit,kind);
                Kit.color=IsInteractable()?Color.white:new Color(1,1,1,.66f);
                if(Words!=null)Words.color=Hub.HubKnit.InkOf(kind);
            }
            if(_leaders!=null)foreach(var line in _leaders)
                if(line!=null)line.color=_focused?SettingsPalette.Accent:SettingsPalette.Rule;
            if(_target!=null)_target.SetActive(_focused);
        }
    }
}
