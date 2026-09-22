using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class CharacterNameplate
    {
        private sealed class RingScope
        {
            public Camera Camera;public Mesh Mesh;public Vector3 Scale;public Material Material;
            public MaterialPropertyBlock Block;
        }
        private readonly List<RingScope> _ringScopes=new List<RingScope>();
        private readonly Stack<RingScope> _ringPool=new Stack<RingScope>();
        private static Mesh _catchable;
        private Material _catchMaterial;
        private void OnEnable(){Camera.onPreCull+=BeginCatchable;Camera.onPostRender+=EndCatchable;}
        private void OnDisable()
        {
            Camera.onPreCull-=BeginCatchable;Camera.onPostRender-=EndCatchable;
            for(int i=_ringScopes.Count-1;i>=0;i--)RestoreRing(_ringScopes[i]);_ringScopes.Clear();
        }
        private void OnDestroy(){if(_catchMaterial!=null)Destroy(_catchMaterial);}
        private void BeginCatchable(Camera camera)
        {
            if(_ringRenderer==null)return;
            var state=_ringPool.Count>0?_ringPool.Pop():new RingScope{Block=new MaterialPropertyBlock()};
            state.Camera=camera;state.Mesh=_ringFilter.sharedMesh;state.Scale=_ring.localScale;
            state.Material=_ringRenderer.sharedMaterial;_ringRenderer.GetPropertyBlock(state.Block);
            if(_ringScopes.Count>0)ApplyRing(_ringScopes[0]);
            _ringScopes.Add(state);
            if(!CharacterVisual.CatchableFor(camera,_character) || WorldCueProfile.Current.TayaTarget<=0)return;
            if(_catchable==null)_catchable=BuildCatchable();
            if(_catchMaterial==null)_catchMaterial=new Material(Shader.Find("TumbangPreso/WorldClock")){name="Catchable ink brackets"};
            _ringFilter.sharedMesh=_catchable;_ringRenderer.sharedMaterial=_catchMaterial;
            var capsule=_character.GetComponent<CharacterController>();float radius=(capsule!=null?capsule.radius:.4f)*2.1f;
            _ring.localScale=new Vector3(radius,1,radius);
            _ringRenderer.GetPropertyBlock(_ringBlock);
            _ringBlock.SetFloat("_Fill",1);_ringBlock.SetFloat("_Weight",WorldCueProfile.Current.TayaTarget);
            _ringBlock.SetColor("_Face",UI.UiTheme.Defense);_ringRenderer.SetPropertyBlock(_ringBlock);
        }
        private void EndCatchable(Camera camera)
        {
            for(int i=_ringScopes.Count-1;i>=0;i--)if(_ringScopes[i].Camera==camera)
            {var state=_ringScopes[i];_ringScopes.RemoveAt(i);RestoreRing(state);return;}
        }
        private void ApplyRing(RingScope state)
        {
            if(_ringRenderer==null)return;
            _ringFilter.sharedMesh=state.Mesh;_ring.localScale=state.Scale;
            _ringRenderer.sharedMaterial=state.Material;_ringRenderer.SetPropertyBlock(state.Block);
        }
        private void RestoreRing(RingScope state){ApplyRing(state);state.Camera=null;_ringPool.Push(state);}
        private static Mesh BuildCatchable()
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            // Four open brackets are visibly different from the taya's unbroken
            // octagon and the ordinary attacker's filled identity disc.
            for(int quadrant=0;quadrant<4;quadrant++)for(int part=0;part<8;part++)
            {
                float a=(quadrant*90+14+part*7.75f)*Mathf.Deg2Rad,b=a+7.75f*Mathf.Deg2Rad;
                int at=vertices.Count;
                vertices.Add(new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)));
                vertices.Add(new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*.72f);
                vertices.Add(new Vector3(Mathf.Sin(b),0,Mathf.Cos(b))*.72f);
                vertices.Add(new Vector3(Mathf.Sin(b),0,Mathf.Cos(b)));
                uv.Add(new Vector2(0,0));uv.Add(new Vector2(0,1));uv.Add(new Vector2(0,1));uv.Add(new Vector2(0,0));
                triangles.Add(at);triangles.Add(at+1);triangles.Add(at+2);triangles.Add(at);triangles.Add(at+2);triangles.Add(at+3);
                // A shallow outer lip survives a low eye angle at10m. It stays
                // at ankle level, not an upright beacon or filled target area.
                int wall=vertices.Count;
                vertices.Add(vertices[at]);vertices.Add(vertices[at]+Vector3.up*.065f);
                vertices.Add(vertices[at+3]+Vector3.up*.065f);vertices.Add(vertices[at+3]);
                uv.Add(new Vector2(0,0));uv.Add(new Vector2(0,1));uv.Add(new Vector2(0,1));uv.Add(new Vector2(0,0));
                triangles.Add(wall);triangles.Add(wall+1);triangles.Add(wall+2);triangles.Add(wall);triangles.Add(wall+2);triangles.Add(wall+3);
            }
            var mesh=new Mesh{name="Catchable four brackets",hideFlags=HideFlags.HideAndDontSave};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
        }
    }
}
