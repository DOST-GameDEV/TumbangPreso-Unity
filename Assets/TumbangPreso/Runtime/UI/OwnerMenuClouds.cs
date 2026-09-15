using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(RawImage))]
    public sealed class OwnerMenuClouds : MonoBehaviour
    {
        private static readonly int DriftId=Shader.PropertyToID("_CloudDrift");
        private RawImage _image;
        private Material _material;
        private Material _previous;
        private float _elapsed;

        private void Awake()
        {
            _image=GetComponent<RawImage>();
            var shader=Resources.Load<Shader>("UI/OwnerMenuSky");
            if(shader==null || !shader.isSupported)
            {Debug.LogWarning("[OwnerMenuClouds] Sky shader unavailable; preserving original illustration.");return;}
            _previous=_image.material;
            _material=new Material(shader){name="OwnerMenuSkyMotion",hideFlags=HideFlags.DontSave};
            _material.SetTexture("_SkyMask",OwnerMenuArt.Texture("main-sky-mask"));
            _image.material=_material;
        }

        private void LateUpdate()
        {
            if(_material==null)return;
            bool reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            if(!reduced)_elapsed+=Time.unscaledDeltaTime;
            // Very long drift cycle, initially about one source pixel per second.
            // Slightly different depth speeds keep both cloud masses from moving
            // as one rigid cutout. The shader protects foreground silhouettes.
            var drift=reduced?Vector4.zero:new Vector4(Mathf.Sin(_elapsed*.03f)*36,
                Mathf.Sin(_elapsed*.018f)*3,0,0);
            _material.SetVector(DriftId,drift);
        }

        private void OnDestroy()
        {
            if(_image!=null && _image.material==_material)_image.material=_previous;
            if(_material!=null)Destroy(_material);
        }
    }
}
