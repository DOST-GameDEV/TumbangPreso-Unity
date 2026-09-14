using UnityEngine;

namespace TumbangPreso.UI
{
    public sealed class OwnerAccountTabArt : MonoBehaviour
    {
        public UnityEngine.UI.Image Left,Right;
        private float _blend,_target;
        private bool _initialized;
        public void SelectLeft(bool left)
        {
            _target=left?0:1;
            if(!_initialized){_initialized=true;_blend=_target;Apply();}
        }
        private void Update()
        {
            _blend=Settings.SettingsStore.Current.ReducedUiMotion?_target:
                Mathf.MoveTowards(_blend,_target,Time.unscaledDeltaTime/Mathf.Max(.05f,OwnerUiTheme.Current.StateSeconds));
            Apply();
        }
        private void Apply()
        {
            if(Left!=null)Left.color=new Color(1,1,1,1-_blend);
            if(Right!=null)Right.color=new Color(1,1,1,_blend);
        }
    }
}
