using UnityEngine;

namespace TumbangPreso.UI
{
    public sealed class OwnerUiSlide : MonoBehaviour
    {
        private RectTransform _rect;
        private Vector2 _from,_target;
        private float _started;
        public static void Move(RectTransform rect,Vector2 target,bool instant)
        {
            var slide=rect.GetComponent<OwnerUiSlide>();if(slide==null)slide=rect.gameObject.AddComponent<OwnerUiSlide>();
            slide._rect=rect;slide._from=rect.anchoredPosition;slide._target=target;slide._started=Time.unscaledTime;
            if(instant || Settings.SettingsStore.Current.ReducedUiMotion){rect.anchoredPosition=target;slide.enabled=false;}
            else slide.enabled=true;
        }
        private void Update()
        {
            float t=Mathf.Clamp01((Time.unscaledTime-_started)/.18f);
            _rect.anchoredPosition=Vector2.LerpUnclamped(_from,_target,1-Mathf.Pow(1-t,3));
            if(t>=1)enabled=false;
        }
    }
}
