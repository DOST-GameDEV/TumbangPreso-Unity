using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // The loaded state owns the cue. Never tint or replace the authored shoe surface.
    public sealed class RafiSkimCoating : MonoBehaviour
    {
        private RafiHeroKit _kit;
        private Slipper _shoe;
        private Renderer _source;
        private LineRenderer _edge;
        private Material _material;
        private MeshFilter _target;
        private Mesh _mesh;
        public Slipper Shoe => _shoe;
        public float VisibleStrength {get;private set;}
        private static readonly int Colour=Shader.PropertyToID("_Color");
        private MaterialPropertyBlock _block;

        public static void Ensure(MeshFilter target,Slipper shoe,RafiHeroKit kit)
        {
            if(target==null||target.sharedMesh==null||shoe==null||kit==null||!kit.IsSkimLoadedFor(shoe))return;
            var existing=target.GetComponentInChildren<RafiSkimCoating>(true);
            if(existing!=null)
            {
                if(existing._shoe==shoe&&existing._kit==kit&&existing._mesh==target.sharedMesh)return;
                existing.gameObject.SetActive(false);Destroy(existing.gameObject);
            }
            var go=new GameObject("SkimSoleMeniscus");go.layer=target.gameObject.layer;
            go.transform.SetParent(target.transform,false);
            var effect=go.AddComponent<RafiSkimCoating>();
            effect._kit=kit;effect._shoe=shoe;effect._target=target;effect._mesh=target.sharedMesh;
            effect._source=target.GetComponent<Renderer>();effect.Build();effect.Sample();
        }

        private void Build()
        {
            _block=new MaterialPropertyBlock();
            var bounds=_mesh.bounds;var size=bounds.size;
            Vector3 along=size.x>=size.y&&size.x>=size.z?Vector3.right:size.y>=size.z?Vector3.up:Vector3.forward;
            Vector3 normal=size.y<=size.x&&size.y<=size.z?Vector3.up:size.x<=size.z?Vector3.right:Vector3.forward;
            if(Mathf.Abs(Vector3.Dot(along,normal))>.5f)normal=Vector3.up;
            Vector3 across=Vector3.Cross(normal,along);
            float length=Mathf.Max(size.x,size.y,size.z);
            float width=Mathf.Abs(Vector3.Dot(size,across));
            float thick=Mathf.Abs(Vector3.Dot(size,normal));
            Vector3 centre=bounds.center-normal*thick*.43f;
            _edge=gameObject.AddComponent<LineRenderer>();_edge.useWorldSpace=false;
            _edge.loop=true;_edge.positionCount=10;_edge.widthMultiplier=length*.023f;
            _edge.numCapVertices=1;_edge.numCornerVertices=1;
            _edge.receiveShadows=false;_edge.shadowCastingMode=ShadowCastingMode.Off;
            // Broad toe, close waist and tucked heel, rather than a halo or a sphere.
            _edge.SetPositions(new[]{
                centre+along*length*.46f,
                centre+along*length*.39f+across*width*.44f,
                centre+along*length*.16f+across*width*.52f,
                centre-along*length*.19f+across*width*.35f,
                centre-along*length*.41f+across*width*.27f,
                centre-along*length*.46f,
                centre-along*length*.40f-across*width*.29f,
                centre-along*length*.18f-across*width*.37f,
                centre+along*length*.17f-across*width*.52f,
                centre+along*length*.39f-across*width*.42f});
            var shader=Resources.Load<Shader>("Shaders/RafiWater");
            if(shader==null)throw new System.InvalidOperationException("Ilyas water shader is missing.");
            _material=new Material(shader){name="Skim sole water"};_edge.sharedMaterial=_material;
            VfxRenderTag.Own(gameObject,_material);
        }
        private void LateUpdate()=>Sample();
        private void Sample()
        {
            if(_kit==null||_shoe==null||!_kit.IsSkimLoadedFor(_shoe)||_target==null||_target.sharedMesh!=_mesh)
            {gameObject.SetActive(false);Destroy(gameObject);return;}
            float remaining=_kit.AttackingSkill.DurationRemaining;
            float age=RafiRules.SkimLoadSeconds-remaining;
            VisibleStrength=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.18f))*Mathf.Clamp01(remaining/.4f);
            _block.SetColor(Colour,new Color(.26f,.76f,.78f,.82f*VisibleStrength));_edge.SetPropertyBlock(_block);
            if(_source!=null)
            {
                _edge.enabled=_source.enabled;
                _edge.forceRenderingOff=_source.forceRenderingOff;
                _edge.shadowCastingMode=_source.shadowCastingMode==ShadowCastingMode.ShadowsOnly?ShadowCastingMode.ShadowsOnly:ShadowCastingMode.Off;
            }
        }
    }
}
