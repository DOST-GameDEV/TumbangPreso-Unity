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
            _material.SetTexture("_SkyMask",OwnerMenuArt.Texture("main-sky-cutout"));
            _material.SetTexture("_CloudA",OwnerMenuArt.Texture("cloud-bank-a"));
            _material.SetTexture("_CloudB",OwnerMenuArt.Texture("cloud-bank-b"));
            _image.material=_material;
        }

        private void LateUpdate()
        {
            if(_material==null)return;
            bool reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            if(!reduced)_elapsed+=Time.unscaledDeltaTime;
            // Independent painted layers travel continuously with the wind.
            // Wrap occurs entirely behind the scene, outside the sky opening.
            var drift=reduced?Vector4.zero:new Vector4(_elapsed*2.8f,_elapsed*1.35f,0,0);
            _material.SetVector(DriftId,drift);
        }

        private void OnDestroy()
        {
            if(_image!=null && _image.material==_material)_image.material=_previous;
            if(_material!=null)Destroy(_material);
        }
    }
}
