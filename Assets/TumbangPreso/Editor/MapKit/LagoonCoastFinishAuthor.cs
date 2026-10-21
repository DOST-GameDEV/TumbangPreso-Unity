using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    // Local background finish: preserve authored geography, palms and all physics.
    public static class LagoonCoastFinishAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/LagoonCoastFinish";
        public const string DetailName="Grounded coastal vegetation and stone";
        public const string SourceTag="TumpLagoonCoastSource",MeshTag="TUMP_LAGOON_COAST_MESH:";
        private static readonly Color[] Palette={new Color(.18f,.31f,.15f),new Color(.25f,.38f,.18f),
            new Color(.32f,.43f,.21f),new Color(.41f,.48f,.26f),new Color(.35f,.29f,.19f),
            new Color(.53f,.53f,.43f),new Color(.64f,.62f,.49f),new Color(.43f,.45f,.37f)};
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene(LagoonBuilder.ScenePath);var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/lagoon-coast-finish");File.WriteAllText("Logs/lagoon-coast-finish/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("Lagoon");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var horizon=map.transform.Find("Seeded island and mountain background");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var detailMat=DetailMaterial();int added=0;
            for(int id=0;id<11;id++)
            {
                var island=horizon.Find("Coastal island "+id);if(island==null)throw new InvalidOperationException("Missing coastal island "+id);
                var old=island.Find(DetailName);if(old!=null)Object.DestroyImmediate(old.gameObject);
                var terrain=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TumbangPreso/Art/Lagoon/IslandTerrain"+id+".asset");
                var surface=new Surface(terrain);var filter=island.GetComponent<MeshFilter>();var renderer=island.GetComponent<MeshRenderer>();
                var original=filter.sharedMesh;string sourcePath=AssetDatabase.GetAssetPath(original);var importer=AssetImporter.GetAtPath(sourcePath);
                if(importer!=null&&importer.userData.StartsWith(MeshTag)){sourcePath=importer.userData.Substring(MeshTag.Length);original=AssetDatabase.LoadAssetAtPath<Mesh>(sourcePath);}
                var slots=renderer.sharedMaterials;var mesh=Object.Instantiate(original);var uv=mesh.uv;
                if(uv.Length!=mesh.vertexCount)uv=new Vector2[mesh.vertexCount];var vertices=mesh.vertices;int changed=0;
                var texture=TerrainTexture(surface,id);
                for(int m=0;m<slots.Length;m++)
                {
                    string source=slots[m].GetTag(SourceTag,false);if(!string.IsNullOrEmpty(source))slots[m]=AssetDatabase.LoadAssetAtPath<Material>(source);
                    string name=MapSurfaceAuthor.SourceMaterialNameForAuthoring(slots[m]).Replace(" ","");
                    if(!name.StartsWith("Coastalcoloursilhouette",StringComparison.OrdinalIgnoreCase)&&!name.StartsWith("Rockysandyshore",StringComparison.OrdinalIgnoreCase))continue;
                    foreach(int index in mesh.GetTriangles(m).Distinct())uv[index]=surface.Uv(vertices[index]);
                    slots[m]=SurfaceMaterial(slots[m],texture,id,m);changed++;
                }
                if(changed!=2)throw new InvalidOperationException("Expected land and shore slots on island "+id);
                mesh.uv=uv;mesh.name="Fitted coastal land "+id;
                if(!mesh.vertices.SequenceEqual(original.vertices)||!mesh.triangles.SequenceEqual(original.triangles))throw new InvalidOperationException("Coast geography changed.");
                string path=Folder+"/Island"+id+".asset";filter.sharedMesh=Save(mesh,path);importer=AssetImporter.GetAtPath(path);importer.userData=MeshTag+sourcePath;importer.SaveAndReimport();
                renderer.sharedMaterials=slots;
                var g=new Geometry();Dress(g,surface,id,island);var detail=g.Build("Coastal groups "+id);int count=detail.vertexCount;added+=count;
                var go=new GameObject(DetailName);go.transform.SetParent(island,false);go.isStatic=true;
                go.AddComponent<MeshFilter>().sharedMesh=Save(detail,Folder+"/Groups"+id+".asset");
                var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=detailMat;r.shadowCastingMode=ShadowCastingMode.Off;
                AirborneByDesign.Attach(go,"Background trees, roots and stone are sampled onto the retained island terrain; no gameplay collision.");
                report.AppendLine("Island"+id+": "+Treatment(id)+", retained terrain/palms and "+count+" grounded detail vertices.");
            }
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Coast changed gameplay collision.");
            report.AppendLine("Nine distant mountain islands unchanged. Added "+added+" vertices/11 renderers/one detail palette, no colliders/shadow casters.");
        }
        private static string Treatment(int id)=>new[]{0,6,10}.Contains(id)?"rocky edge":new[]{1,4,8}.Contains(id)?"beach woodland":new[]{2,5}.Contains(id)?"low scrub":"sheltered mangroves";
        private static void Dress(Geometry g,Surface s,int id,Transform island)
        {
            var rng=new System.Random(92471+id*31);float Next()=> (float)rng.NextDouble();
            string type=Treatment(id);int count=type=="beach woodland"?10:type=="low scrub"?9:type=="sheltered mangroves"?7:5;
            float phase=id*1.13f;var centre=new Vector2(Mathf.Cos(phase)*.25f,Mathf.Sin(phase)*.25f);
            for(int i=0;i<count;i++)
            {
                float angle=i*2.39996f,spread=Mathf.Sqrt((i+.5f)/count)*.32f;
                var unit=centre+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*spread;
                if(type=="sheltered mangroves")unit=new Vector2(Mathf.Cos(phase+(i-count*.5f)*.10f),Mathf.Sin(phase+(i-count*.5f)*.10f))*(.88f+Next()*.035f);
                var xz=s.Point(unit);if(!s.Sample(xz,out float height,out _))continue;
                var at=new Vector3(xz.x,height-.18f,xz.y);float size=type=="low scrub"?.9f+Next()*.8f:2.1f+Next()*1.4f;
                if(type=="low scrub"){g.Blob(at+Vector3.up*size*.5f,new Vector3(size*1.2f,size*.65f,size),id%3,7,3,i*.31f);continue;}
                var fork=at+new Vector3(.24f,size*.95f,-.1f);g.Branch(at,fork,.13f,4);
                for(int j=0;j<3;j++)
                {
                    float a=j*2.094f+i;var tip=fork+new Vector3(Mathf.Cos(a)*size*.6f,size*.45f,Mathf.Sin(a)*size*.6f);
                    g.Branch(fork,tip,.09f,4);g.Blob(tip+Vector3.up*size*.18f,new Vector3(size*.95f,size*.72f,size*.88f),(id+j)%3,9,4,i+j*.7f);
                }
                if(type=="sheltered mangroves")for(int j=0;j<4;j++)
                {
                    float a=j*Mathf.PI*.5f+i;var foot=xz+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*size*.6f;
                    if(!s.Sample(foot,out float ground,out _))ground=LagoonWater.SurfaceY-.4f;
                    g.Branch(at+Vector3.up*size*.65f,new Vector3(foot.x,ground-.12f,foot.y),.09f,4);
                }
            }
            if(type!="rocky edge")return;
            var village=island.InverseTransformPoint(Vector3.zero);phase=Mathf.Atan2(village.z,village.x);
            for(int i=0;i<7;i++)
            {
                float a=phase+(i-3)*.19f;var xz=s.Point(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(.69f+Next()*.18f));
                if(!s.Sample(xz,out float y,out _))continue;
                float size=1.9f+Next()*1.9f;
                g.Rock(new Vector3(xz.x,y,xz.y),new Vector3(size,size*.45f,size*.78f),5+i%3,i*.8f,s);
            }
        }
        private sealed class Surface
        {
            public readonly Bounds Bounds;private readonly Vector3[] points;private readonly int[] indices;
            public Surface(Mesh mesh){if(mesh==null)throw new InvalidOperationException("Missing source terrain.");Bounds=mesh.bounds;points=mesh.vertices;indices=mesh.triangles;}
            public Vector2 Uv(Vector3 p)=>new Vector2(Mathf.InverseLerp(Bounds.min.x,Bounds.max.x,p.x),Mathf.InverseLerp(Bounds.min.z,Bounds.max.z,p.z));
            public Vector2 Point(Vector2 unit)=>new Vector2(Bounds.center.x+unit.x*Bounds.extents.x,Bounds.center.z+unit.y*Bounds.extents.z);
            public bool Sample(Vector2 p,out float y,out float slope)
            {
                for(int i=0;i<indices.Length;i+=3)
                {
                    var a=points[indices[i]];var b=points[indices[i+1]];var c=points[indices[i+2]];
                    float d=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
                    if(Mathf.Abs(d)<.00001f)continue;
                    float u=((b.z-c.z)*(p.x-c.x)+(c.x-b.x)*(p.y-c.z))/d;
                    float v=((c.z-a.z)*(p.x-c.x)+(a.x-c.x)*(p.y-c.z))/d;
                    if(u<-.001f||v<-.001f||u+v>1.001f)continue;
                    y=a.y*u+b.y*v+c.y*(1-u-v);slope=1-Mathf.Abs(Vector3.Cross(b-a,c-a).normalized.y);return true;
                }
                y=LagoonWater.SurfaceY;slope=0;return false;
            }
        }
        private static Texture2D TerrainTexture(Surface s,int id)
        {
            const int size=256;var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var p=new Vector2(Mathf.Lerp(s.Bounds.min.x,s.Bounds.max.x,x/(float)(size-1)),Mathf.Lerp(s.Bounds.min.z,s.Bounds.max.z,y/(float)(size-1)));
                s.Sample(p,out float height,out float slope);float n=Mathf.PerlinNoise(p.x*.10f+id*3.7f,p.y*.10f+9);
                float beach=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.05f,.9f+n*.8f,height-LagoonWater.SurfaceY));
                float exposed=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.17f,.43f,slope))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.38f,.72f,n));
                var land=Color.Lerp(new Color(.28f,.39f,.20f),new Color(.43f,.48f,.27f),Mathf.SmoothStep(.25f,.8f,n));
                land=Color.Lerp(land,new Color(.57f,.57f,.45f),exposed*.9f);
                var sand=Color.Lerp(new Color(.57f,.57f,.43f),new Color(.70f,.66f,.49f),Mathf.Clamp01((height-LagoonWater.SurfaceY)*1.5f));
                pixels[y*size+x]=Color.Lerp(land,sand,beach);
            }
            return Texture(pixels,size,size,Folder+"/Coast"+id+".png",FilterMode.Bilinear,true);
        }
        private static Texture2D Texture(Color[] pixels,int w,int h,string path,FilterMode filter,bool mip)
        {
            var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);tex.SetPixels(pixels);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
            var i=(TextureImporter)AssetImporter.GetAtPath(path);i.wrapMode=TextureWrapMode.Clamp;i.filterMode=filter;i.mipmapEnabled=mip;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static Material SurfaceMaterial(Material source,Texture2D texture,int id,int slot)
        {
            var m=Visual.NearFade.CopySurfaceForAuthoring(source);m.color=Color.white;m.mainTexture=texture;m.mainTextureScale=Vector2.one;m.mainTextureOffset=Vector2.zero;
            m.SetFloat("_SurfaceKind",0);m.SetFloat("_SurfaceVertexRoles",0);m.SetFloat("_Glossiness",.03f);m.SetOverrideTag(SourceTag,AssetDatabase.GetAssetPath(source));
            return Save(m,Folder+"/Coast"+id+"Slot"+slot+".mat");
        }
        private static Material DetailMaterial()
        {
            var texture=Texture(Palette,8,1,Folder+"/CoastalPalette.png",FilterMode.Point,false);
            var m=new Material(Shader.Find("Standard"));m.color=Color.white;m.mainTexture=texture;m.SetFloat("_Glossiness",.03f);return Save(m,Folder+"/CoastalGroups.mat");
        }
        private static T Save<T>(T draft,string path) where T:Object
        {var saved=AssetDatabase.LoadAssetAtPath<T>(path);if(saved==null){AssetDatabase.CreateAsset(draft,path);return draft;}EditorUtility.CopySerialized(draft,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(draft);return saved;}
        private sealed class Geometry
        {
            private readonly List<Vector3> points=new List<Vector3>(),normals=new List<Vector3>();private readonly List<Vector2> uv=new List<Vector2>();private readonly List<int> indices=new List<int>();
            private void Triangle(Vector3 a,Vector3 b,Vector3 c,int colour,Vector3? centre=null,Vector3? axes=null)
            {
                int n=points.Count;points.AddRange(new[]{a,b,c});indices.AddRange(new[]{n,n+1,n+2});var face=Vector3.Cross(b-a,c-a).normalized;
                foreach(var p in new[]{a,b,c})
                {
                    uv.Add(new Vector2((colour+.5f)/8,.5f));var normal=face;
                    if(centre.HasValue){var d=p-centre.Value;var s=axes.Value;normal=new Vector3(d.x/(s.x*s.x),d.y/(s.y*s.y),d.z/(s.z*s.z)).normalized;}
                    normals.Add(normal);
                }
            }
            public void Blob(Vector3 at,Vector3 size,int colour,int segments,int rings,float phase)
            {
                Vector3 P(int ring,int sector)
                {
                    float latitude=-Mathf.PI*.5f+Mathf.PI*ring/rings,a=sector*Mathf.PI*2/segments+phase;
                    latitude+=Mathf.Cos(latitude)*Mathf.Sin(a*3+phase)*.13f;
                    float r=Mathf.Cos(latitude)*(1+Mathf.Sin(a*3+phase)*.10f+Mathf.Cos(a*2-phase)*.06f);
                    return at+Vector3.Scale(size,new Vector3(r*Mathf.Cos(a),Mathf.Sin(latitude),r*Mathf.Sin(a)));
                }
                for(int r=0;r<rings;r++)for(int s=0;s<segments;s++)
                {var a=P(r,s);var b=P(r,s+1);var c=P(r+1,s);var d=P(r+1,s+1);Vector3? centre=colour<4?at:(Vector3?)null;
                    if(r>0)Triangle(a,c,b,colour,centre,size);if(r<rings-1)Triangle(b,c,d,colour,centre,size);}
            }
            public void Branch(Vector3 a,Vector3 b,float radius,int colour)
            {
                var direction=(b-a).normalized;var right=Vector3.Cross(direction,Vector3.forward).normalized;if(right.sqrMagnitude<.1f)right=Vector3.right;
                var forward=Vector3.Cross(direction,right);Vector3 Offset(int i)=> (right*Mathf.Cos(i*Mathf.PI/3)+forward*Mathf.Sin(i*Mathf.PI/3))*radius;
                for(int i=0;i<6;i++){var p=a+Offset(i);var q=a+Offset(i+1);var r=b+Offset(i)*.7f;var s=b+Offset(i+1)*.7f;Triangle(p,r,q,colour);Triangle(q,r,s,colour);}
            }
            public void Rock(Vector3 at,Vector3 size,int colour,float phase,Surface surface)
            {
                // Low embedded shelves with tilted polygon tops, not pointed cylinders.
                Vector3 P(int ring,int sector)
                {
                    float a=sector*Mathf.PI/3+phase,r=(ring==0?.72f:ring==1?1:.58f)*(1+Mathf.Sin(a*2+phase)*.18f);
                    float y=ring==0?-.75f:ring==1?-.1f:.72f+Mathf.Cos(a+phase)*.20f;
                    var p=at+Vector3.Scale(size,new Vector3(r*Mathf.Cos(a)+(ring==2?.16f:0),y,r*Mathf.Sin(a)));
                    // A centre-height sample alone leaves the downhill edge floating.
                    // Sink the full footprint into the actual original terrain.
                    if(ring==0)
                    {
                        if(!surface.Sample(new Vector2(p.x,p.z),out float ground,out _))ground=LagoonWater.SurfaceY-.25f;
                        p.y=Mathf.Min(p.y,ground-.18f);
                    }
                    return p;
                }
                for(int s=0;s<6;s++)
                {
                    for(int r=0;r<2;r++){Triangle(P(r,s),P(r+1,s),P(r,s+1),colour);Triangle(P(r,s+1),P(r+1,s),P(r+1,s+1),colour);}
                    Triangle(P(2,s),at+Vector3.Scale(size,new Vector3(.16f,.72f,0)),P(2,s+1),colour);
                }
            }
            public Mesh Build(string name){var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(points);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.SetNormals(normals);mesh.RecalculateBounds();return mesh;}
        }
    }
}
