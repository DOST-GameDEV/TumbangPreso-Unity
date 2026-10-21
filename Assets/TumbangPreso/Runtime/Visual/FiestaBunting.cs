using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // Sparse neighbourhood decoration tied to existing supports, outside the box.
    // One batched mesh, no colliders/physics. Shared sky time makes replay seekable.
    public sealed class FiestaBunting : MonoBehaviour
    {
        private Mesh _mesh;
        private Material _material;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _block;
        public int SpanCount {get;private set;}
        private static readonly Color[] Colours={new Color(.83f,.39f,.24f),new Color(.94f,.73f,.28f),new Color(.41f,.60f,.64f),new Color(.90f,.81f,.62f)};
        public static FiestaBunting Install(Transform parent)
        {
            string map=parent.gameObject.scene.name;
            if(map!="BayanPlaza" && map!="Eskinita")return null;
            var go=new GameObject("Peripheral fiesta bunting");go.transform.SetParent(parent,false);
            AirborneByDesign.Attach(go,"Sagging decorative cord tied to existing peripheral tree/pole supports; cloth hangs below its pinned top edge.");
            var owner=go.AddComponent<FiestaBunting>();owner.Build(map);return owner;
        }
        private void Build(string map)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var phases=new List<Vector2>();var colours=new List<Color>();var triangles=new List<int>();
            var supports=FindObjectsByType<Transform>().Where(t=>t.gameObject.scene==gameObject.scene && t.gameObject.activeInHierarchy &&
                (map=="Eskinita"?t.name.StartsWith("PosteRework_"):t.name.StartsWith("Broadleaf_") || t.name.StartsWith("CivicGardenTree_")))
                .Where(t=>Mathf.Abs(t.position.x)>Core.Balance.ConfinementRadius+.6f).OrderBy(t=>t.position.z).ToArray();
            foreach(int side in new[]{-1,1})
            {
                var row=supports.Where(t=>Mathf.Sign(t.position.x)==side).ToArray();
                var pairs=new List<(Transform A,Transform B)>();
                for(int i=0;i<row.Length;i++)for(int j=i+1;j<row.Length;j++)
                    if(Mathf.Abs(row[i].position.x-row[j].position.x)<2.5f && Mathf.Abs(row[i].position.z-row[j].position.z)>4 && Vector3.Distance(row[i].position,row[j].position)<20)
                        pairs.Add((row[i],row[j]));
                foreach(var pair in pairs.OrderBy(p=>Mathf.Abs((p.A.position.z+p.B.position.z)*.5f)))
                {
                    Vector3 a=pair.A.position+Vector3.up*4.2f,b=pair.B.position+Vector3.up*4.2f;
                    if(!ClearSpan(pair.A,pair.B,a,b))continue;
                    AddSpan(a,b,SpanCount,vertices,uv,phases,colours,triangles);SpanCount++;break;
                }
            }
            _mesh=new Mesh{name=map+" tied bunting"};_mesh.SetVertices(vertices);_mesh.SetUVs(0,uv);_mesh.SetUVs(1,phases);_mesh.SetColors(colours);_mesh.SetTriangles(triangles,0);_mesh.RecalculateNormals();_mesh.RecalculateBounds();
            var bounds=_mesh.bounds;bounds.Expand(.3f);_mesh.bounds=bounds;
            gameObject.AddComponent<MeshFilter>().sharedMesh=_mesh;_renderer=gameObject.AddComponent<MeshRenderer>();
            _material=new Material(Shader.Find("TumbangPreso/FiestaCloth")){name="Fiesta cloth and cord"};_renderer.sharedMaterial=_material;
            _renderer.shadowCastingMode=ShadowCastingMode.Off;_renderer.receiveShadows=false;_block=new MaterialPropertyBlock();
            Step();
        }
        private static bool ClearSpan(Transform supportA,Transform supportB,Vector3 a,Vector3 b)
        {
            var delta=b-a;float length=delta.magnitude;
            foreach(var hit in Physics.RaycastAll(a,delta.normalized,length,~0,QueryTriggerInteraction.Ignore))
            {
                var t=hit.collider.transform;
                if(t.IsChildOf(supportA) || t.IsChildOf(supportB) || hit.distance<.3f || hit.distance>length-.3f)continue;
                if(hit.collider.GetComponentInParent<CharacterMotor>()!=null)continue;
                return false;
            }
            return true;
        }
        private void AddSpan(Vector3 a,Vector3 b,int span,List<Vector3> v,List<Vector2> uv,List<Vector2> phase,List<Color> c,List<int> indices)
        {
            Vector3 along=(b-a).normalized;int count=Mathf.Clamp(Mathf.FloorToInt(Vector3.Distance(a,b)/.85f),5,18);
            Vector3 Point(float t)=>Vector3.Lerp(a,b,t)-Vector3.up*(4*t*(1-t)*.4f);
            void Vertex(Vector3 point,Vector2 tex,Color colour,float clock)
            {v.Add(transform.InverseTransformPoint(point));uv.Add(tex);phase.Add(new Vector2(clock,0));c.Add(colour);}
            // Fine sagging cord. Its uv.y stays0, pinning it during cloth motion.
            for(int k=0;k<20;k++)
            {
                int start=v.Count;var p=Point(k/20f);var q=Point((k+1)/20f);Color ink=new Color(.18f,.15f,.11f);
                Vertex(p+Vector3.up*.008f,Vector2.zero,ink,0);Vertex(p-Vector3.up*.008f,Vector2.zero,ink,0);
                Vertex(q-Vector3.up*.008f,Vector2.zero,ink,0);Vertex(q+Vector3.up*.008f,Vector2.zero,ink,0);
                indices.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            for(int k=0;k<count;k++)
            {
                float t=(k+.5f)/count,clock=k*1.73f+span*2.31f;var at=Point(t);int start=v.Count;
                float width=.38f,drop=k%3==0?.51f:.43f;Color colour=Colours[(k+span)%Colours.Length];
                Vertex(at-along*width*.5f,new Vector2(0,0),colour,clock);
                Vertex(at+along*width*.5f,new Vector2(1,0),colour,clock);
                Vertex(at-Vector3.up*drop,new Vector2(.5f,1),colour,clock);
                indices.AddRange(new[]{start,start+1,start+2});
            }
        }
        private void LateUpdate()=>Step();
        private void Step()
        {
            if(_renderer==null)return;
            _renderer.enabled=SpanCount>0 && WorldCueProfile.Current.EnvironmentAppeal>0;
            _block.SetFloat("_Weight",WorldCueProfile.Current.EnvironmentAppeal);
            _block.SetFloat("_Motion",Settings.SettingsStore.Current.ReducedUiMotion?0:Settings.SettingsStore.Current.ReducedEffects?.3f:1);
            _renderer.SetPropertyBlock(_block);
        }
        private void OnDestroy(){if(_mesh!=null)Destroy(_mesh);if(_material!=null)Destroy(_material);}
    }
}
