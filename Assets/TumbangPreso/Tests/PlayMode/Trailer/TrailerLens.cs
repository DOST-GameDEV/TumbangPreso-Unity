using UnityEngine;

namespace TumbangPreso.PlayTests.Trailer
{
    /// <summary>Trailer-only depth-aware focus. The built-in pipeline is verified in this checkout.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class TrailerLens : MonoBehaviour
    {
        public Vector3 Focus;
        public float BlurPixels;
        private Camera _camera;
        private Material _material;
        private DepthTextureMode _was;
        public bool Ready => _material!=null;
        private void OnEnable()
        {
            _camera=GetComponent<Camera>(); _was=_camera.depthTextureMode;
            _camera.depthTextureMode|=DepthTextureMode.Depth;
            var shader=Shader.Find("Hidden/TUMP/TrailerFocus");
            if(shader!=null && shader.isSupported) _material=new Material(shader) {hideFlags=HideFlags.HideAndDontSave};
            Debug.Log("[TrailerLens] depth-aware focus ready="+Ready);
        }
        private void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(_material==null || BlurPixels<.01f) {Graphics.Blit(source,destination);return;}
            _material.SetFloat("_Focus",Mathf.Max(.1f,Vector3.Dot(Focus-transform.position,transform.forward)));
            _material.SetFloat("_Radius",BlurPixels*source.height/1080f);
            Graphics.Blit(source,destination,_material);
        }
        private void OnDisable()
        {
            if(_camera!=null) _camera.depthTextureMode=_was;
            if(_material!=null) Destroy(_material);
        }
    }
}
