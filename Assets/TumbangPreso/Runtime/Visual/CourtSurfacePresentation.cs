using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // V2: the same real court geometry chooses a contrasting physical medium.
    // Surface wear is subdued structure, never another filled rule zone.
    public sealed class CourtSurfacePresentation : MonoBehaviour
    {
        private sealed class Mark
        {
            public Renderer Renderer;public Material[] Original,Styled;
        }
        private readonly List<Mark> _marks=new List<Mark>();
        private Material _chalk,_wear;private Mesh _mesh;private Renderer _floor;
        private bool _styled;private WorldLookPresentation _look;
        public int AuthoredMarkRenderers=>_marks.Count;
        public static CourtSurfacePresentation Install(Transform parent,WorldLookPresentation look)
        {
            if(look==null)return null;
            var go=new GameObject("Court surface treatment");go.SetActive(false);go.transform.SetParent(parent,false);
            var cue=go.AddComponent<CourtSurfacePresentation>();cue._look=look;cue.Build();go.SetActive(true);return cue;
        }
        private void Build()
        {
            var shader=Shader.Find("TumbangPreso/CourtSurface");
            _chalk=new Material(shader){name="Authored chalk medium"};_wear=new Material(shader){name="Local court wear"};
            _wear.SetFloat("_Mode",1);
            // Modulate the existing lit floor. An alpha-painted light colour
            // washed out real shadows and revealed the overlay's polygon edge.
            _wear.SetInt("_SrcBlend",(int)BlendMode.DstColor);_wear.SetInt("_DstBlend",(int)BlendMode.Zero);
            foreach(var renderer in FindObjectsByType<MeshRenderer>())
            {
                if(renderer.gameObject.scene!=gameObject.scene || renderer.GetComponentInParent<VfxRenderTag>()!=null)continue;
                var bounds=renderer.bounds;
                if(bounds.max.y>_look.Floor+.55f || bounds.min.y<_look.Floor-.3f ||
                    Mathf.Abs(bounds.center.x)>Balance.ConfinementRadius+6 || Mathf.Abs(bounds.center.z)>Balance.ConfinementRadius+6)continue;
                var materials=renderer.sharedMaterials;Material[] styled=null;
                bool namedMark=(renderer.transform.parent!=null && renderer.transform.parent.name=="Chalk") ||
                    renderer.name.IndexOf("chalk",System.StringComparison.OrdinalIgnoreCase)>=0;
                for(int i=0;i<materials.Length;i++)
                {
                    if(materials[i]==null || (!namedMark && materials[i].name.IndexOf("chalk",System.StringComparison.OrdinalIgnoreCase)<0))continue;
                    if(styled==null)styled=(Material[])materials.Clone();styled[i]=_chalk;
                }
                if(styled!=null)_marks.Add(new Mark{Renderer=renderer,Original=materials,Styled=styled});
            }
            const int cells=28;float radius=Balance.ConfinementRadius;
            var vertices=new Vector3[(cells+1)*(cells+1)];var valid=new bool[vertices.Length];var triangles=new List<int>();
            for(int z=0;z<=cells;z++)for(int x=0;x<=cells;x++)
            {
                int at=z*(cells+1)+x;var point=new Vector3(Mathf.Lerp(-radius,radius,x/(float)cells),_look.Floor,Mathf.Lerp(-radius,radius,z/(float)cells));
                valid[at]=WorldGround.TryBelow(point,.7f,1.7f,out float height) && Mathf.Abs(height-_look.Floor)<.45f;
                point.y=valid[at]?height+.012f:_look.Floor;vertices[at]=transform.InverseTransformPoint(point);
            }
            for(int z=0;z<cells;z++)for(int x=0;x<cells;x++)
            {
                int a=z*(cells+1)+x,b=a+1,c=a+cells+1,d=c+1;
                if(!valid[a]||!valid[b]||!valid[c]||!valid[d])continue;
                triangles.Add(a);triangles.Add(c);triangles.Add(b);triangles.Add(b);triangles.Add(c);triangles.Add(d);
            }
            _mesh=new Mesh{name="Sampled court surface overlays"};_mesh.vertices=vertices;_mesh.SetTriangles(triangles,0);_mesh.RecalculateBounds();
            var floor=new GameObject("Court wear");floor.transform.SetParent(transform,false);floor.AddComponent<MeshFilter>().sharedMesh=_mesh;
            _floor=floor.AddComponent<MeshRenderer>();_floor.sharedMaterial=_wear;_floor.shadowCastingMode=ShadowCastingMode.Off;_floor.receiveShadows=false;
            Apply();
        }
        private void Update()=>Apply();
        private void Apply()
        {
            if(_chalk==null || _look==null)return;float weight=Mathf.Clamp01(WorldCueProfile.Current.CourtSurface);
            bool on=weight>0;
            if(on!=_styled)
            {foreach(var mark in _marks)if(mark.Renderer!=null)mark.Renderer.sharedMaterials=on?mark.Styled:mark.Original;_styled=on;}
            _floor.enabled=on;_chalk.SetColor("_Medium",WorldLookPresentation.CourtChalk);
            _chalk.SetFloat("_Weight",weight);_chalk.SetFloat("_WearKind",_look.Look.WearKind);
            _wear.SetFloat("_Weight",weight);_wear.SetFloat("_WearKind",_look.Look.WearKind);
            _wear.SetFloat("_Radius",Balance.ConfinementRadius);
        }
        private void OnDisable()
        {foreach(var mark in _marks)if(mark.Renderer!=null)mark.Renderer.sharedMaterials=mark.Original;_styled=false;}
        private void OnDestroy()
        {if(_chalk!=null)Destroy(_chalk);if(_wear!=null)Destroy(_wear);if(_mesh!=null)Destroy(_mesh);}
    }
}
