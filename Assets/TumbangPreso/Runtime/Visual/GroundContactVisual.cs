using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // Small object support, not an area effect. One shared quad, no collider.
    public sealed class GroundContactVisual : System.IDisposable
    {
        private static Mesh _quad;
        private readonly GameObject _root;private readonly Renderer _renderer;private readonly MaterialPropertyBlock _block;
        public Renderer Renderer=>_renderer;
        public GroundContactVisual(Transform parent,string name,bool recorded=false)
        {
            if(_quad==null)
            {
                _quad=new Mesh{name="Shared ground cue quad",hideFlags=HideFlags.HideAndDontSave};
                _quad.vertices=new[]{new Vector3(-1,0,-1),new Vector3(-1,0,1),new Vector3(1,0,1),new Vector3(1,0,-1)};
                _quad.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};
                _quad.triangles=new[]{0,1,2,0,2,3};_quad.RecalculateBounds();
            }
            _root=new GameObject(name);_root.transform.SetParent(parent,false);_root.AddComponent<MeshFilter>().sharedMesh=_quad;
            _renderer=_root.AddComponent<MeshRenderer>();_renderer.shadowCastingMode=ShadowCastingMode.Off;_renderer.receiveShadows=false;
            var material=new Material(Shader.Find("TumbangPreso/GroundContact")){name=name+" ink"};_renderer.sharedMaterial=material;
            VfxRenderTag.Own(_root,material);_block=new MaterialPropertyBlock();_renderer.enabled=false;
            _renderer.forceRenderingOff=recorded;
        }
        public bool Place(Vector3 supportPoint,float lowestY,Vector2 radius,float weight,bool landing=false)
        {
            if(weight<=0 || !WorldGround.TryBelow(supportPoint,.5f,3.5f,out float ground))
            {_renderer.enabled=false;return false;}
            float gap=Mathf.Max(0,lowestY-ground);
            if(!landing && gap>2.5f){_renderer.enabled=false;return false;}
            _root.transform.position=new Vector3(supportPoint.x,ground+.015f,supportPoint.z);
            _root.transform.localScale=new Vector3(radius.x,1,radius.y);
            _block.SetFloat("_Landing",landing?1:0);
            _block.SetFloat("_Weight",Mathf.Clamp01(weight)*(landing?1:Mathf.Clamp01(1-gap/2.5f)));
            _block.SetColor("_Face",WorldLookPresentation.CourtChalk);_block.SetColor("_Ink",WorldLookPresentation.CourtEdge);
            _renderer.SetPropertyBlock(_block);_renderer.enabled=true;return true;
        }
        public void Hide()=>_renderer.enabled=false;
        public void Visible(bool visible)=>_renderer.forceRenderingOff=!visible;
        public void Dispose(){if(_root!=null)Object.Destroy(_root);}
    }
}
