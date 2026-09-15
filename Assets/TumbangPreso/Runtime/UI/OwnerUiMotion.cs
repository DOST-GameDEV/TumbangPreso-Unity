using UnityEngine;

namespace TumbangPreso.UI
{
    // Animate artwork inside a stationary interaction target, never the hit box.
    public sealed class OwnerUiMotion : MonoBehaviour
    {
        public bool GentleFloat;
        public float EntryDelay;
        private RectTransform _rect;
        private CanvasGroup _group;
        private Vector2 _rest;
        private float _started, _scale=1, _goal=1;
        private bool _ready;
        private bool _disabled;
        public bool Entering=>isActiveAndEnabled && Time.unscaledTime<_started+EntryDelay+OwnerUiTheme.Current.EnterSeconds;
        public void SetState(bool focused,bool pressed,bool disabled)
        {
            _disabled=disabled;
            _goal=disabled?1:pressed?.975f:focused?1.025f:1;
        }
        private void OnEnable(){_started=Time.unscaledTime;_ready=false;}
        private void OnDisable()
        {
            if(_ready && _rect!=null){_rect.anchoredPosition=_rest;_rect.localScale=Vector3.one;}
            _scale=1;
        }
        private void LateUpdate()
        {
            if(!_ready)
            {
                _rect=(RectTransform)transform;_rest=_rect.anchoredPosition;
                _group=GetComponent<CanvasGroup>();if(_group==null)_group=gameObject.AddComponent<CanvasGroup>();
                _ready=true;
            }
            bool reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            float age=Time.unscaledTime-_started-EntryDelay;
            float entry=Mathf.Clamp01(age/Mathf.Max(.05f,OwnerUiTheme.Current.EnterSeconds));
            float ease=1-Mathf.Pow(1-entry,3);
            _scale=reduced?1:Mathf.Lerp(_scale,_goal,1-Mathf.Exp(-Time.unscaledDeltaTime/Mathf.Max(.03f,OwnerUiTheme.Current.StateSeconds)));
            _rect.localScale=Vector3.one*_scale;
            float drift=GentleFloat && !reduced?Mathf.Sin(Time.unscaledTime*.85f)*2:0;
            _rect.anchoredPosition=_rest+Vector2.up*(reduced?0:(1-ease)*-10+drift);
            _group.alpha=ease*(_disabled?.58f:1);
        }
    }
}
