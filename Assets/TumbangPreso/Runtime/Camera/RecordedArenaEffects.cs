using System;
using UnityEngine;
using UnityEngine.Rendering;
using ArenaFx=TumbangPreso.Map.ArenaFx;

namespace TumbangPreso.CameraSystem
{
    // Owned render mesh only. It neither emits effects nor advances the live pool.
    public sealed class RecordedArenaEffects:IDisposable
    {
        private GameObject _root;
        private Mesh _mesh;
        private MeshRenderer _renderer;
        private Vector3[] _vertices=Array.Empty<Vector3>();
        private Color32[] _colours=Array.Empty<Color32>();
        private Vector2[] _uv=Array.Empty<Vector2>();
        private int[] _triangles=Array.Empty<int>();
        public RecordedArenaEffects(Transform owner,Material material)
        {
            _root=new GameObject("Recorded Arena effects");_root.transform.SetParent(owner,false);
            _root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);_root.transform.localScale=Vector3.one;
            _mesh=new Mesh{name="Recorded Arena effects mesh"};_mesh.MarkDynamic();
            _root.AddComponent<MeshFilter>().sharedMesh=_mesh;_renderer=_root.AddComponent<MeshRenderer>();_renderer.sharedMaterial=material;
            _renderer.shadowCastingMode=ShadowCastingMode.Off;_renderer.receiveShadows=false;
            _renderer.lightProbeUsage=LightProbeUsage.Off;_renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;_renderer.enabled=false;
        }
        public void Draw(ArenaFx.RecordedQuad[] quads,Camera camera)
        {
            int count=quads?.Length??0;_renderer.enabled=count>0;if(count==0)return;
            if(_vertices.Length<count*4)
            {
                _vertices=new Vector3[count*4];_colours=new Color32[count*4];_uv=new Vector2[count*4];_triangles=new int[count*6];
                for(int i=0;i<count;i++){int v=i*4,t=i*6;_triangles[t]=v;_triangles[t+1]=v+1;_triangles[t+2]=v+2;_triangles[t+3]=v;_triangles[t+4]=v+2;_triangles[t+5]=v+3;}
            }
            var vertices=_vertices;var colours=_colours;var uv=_uv;
            for(int i=0;i<count;i++)
            {
                var q=quads[i];Corners(q,camera.transform,out var a,out var b,out var c,out var d);int v=i*4;
                vertices[v]=a;vertices[v+1]=b;vertices[v+2]=c;vertices[v+3]=d;
                var colour=new Color32((byte)q.Colour,(byte)(q.Colour>>8),(byte)(q.Colour>>16),(byte)(q.Colour>>24));
                for(int j=0;j<4;j++)colours[v+j]=colour;
                int column=q.Cell%4,row=3-q.Cell/4;const float inset=1f/128;
                uv[v]=new Vector2((column+inset)/4,(row+inset)/4);uv[v+1]=new Vector2((column+1-inset)/4,(row+inset)/4);
                uv[v+2]=new Vector2((column+1-inset)/4,(row+1-inset)/4);uv[v+3]=new Vector2((column+inset)/4,(row+1-inset)/4);
            }
            _mesh.Clear();_mesh.SetVertices(vertices,0,count*4);_mesh.SetColors(colours,0,count*4);_mesh.SetUVs(0,uv,0,count*4);_mesh.SetTriangles(_triangles,0,count*6,0,false);_mesh.bounds=new Bounds(Vector3.zero,Vector3.one*6000);
        }
        public static void Corners(ArenaFx.RecordedQuad q,Transform camera,out Vector3 a,out Vector3 b,out Vector3 c,out Vector3 d)
        {
            a=q.A;b=q.B;c=q.C;d=q.D;
            if(q.Facing==ArenaFx.RecordedFacing.Fixed||((camera.position-q.Eye).sqrMagnitude<1e-10f&&
                (camera.right-q.Right).sqrMagnitude<1e-10f&&(camera.up-q.Up).sqrMagnitude<1e-10f))return;
            var centre=(q.A+q.C)*.5f;
            if(q.Facing==ArenaFx.RecordedFacing.Billboard)
            {
                Vector3 Face(Vector3 point){var delta=point-centre;return centre+camera.right*Vector3.Dot(delta,q.Right)+camera.up*Vector3.Dot(delta,q.Up);}
                a=Face(q.A);b=Face(q.B);c=Face(q.C);d=Face(q.D);return;
            }
            if(q.Facing==ArenaFx.RecordedFacing.Beam)
            {
                var from=(q.A+q.B)*.5f;var to=(q.C+q.D)*.5f;var along=to-from;
                var side=Vector3.Cross(along,camera.position-(from+to)*.5f);if(side.sqrMagnitude<1e-8f)side=Vector3.Cross(along,Vector3.up);if(side.sqrMagnitude<1e-8f)side=Vector3.right;side.Normalize();
                float width0=(q.B-q.A).magnitude*.5f,width1=(q.C-q.D).magnitude*.5f;
                a=from-side*width0;b=from+side*width0;c=to+side*width1;d=to-side*width1;return;
            }
            var up=(q.D-q.A)*.5f;var axis=q.Facing==ArenaFx.RecordedFacing.Upright?Vector3.up:up.normalized;
            var right=Vector3.Cross(axis,camera.position-centre);right=right.sqrMagnitude>1e-8f?right.normalized:camera.right;
            right*=Vector3.Distance(q.A,q.B)*.5f;a=centre-right-up;b=centre+right-up;c=centre+right+up;d=centre-right+up;
        }
        public void Visible(bool visible){if(_renderer!=null)_renderer.forceRenderingOff=!visible;}
        public void Dispose(){if(_root!=null)UnityEngine.Object.Destroy(_root);if(_mesh!=null)UnityEngine.Object.Destroy(_mesh);_root=null;_mesh=null;}
    }
}
