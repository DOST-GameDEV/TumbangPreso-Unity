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
    /// <summary>Material and modest edge construction for Lagoon's four gabled-thatch homes.</summary>
    public static class LagoonGableFinishAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/LagoonGableFinish",RootName="Fitted gable thatch";
        private const string SourceTag="TumpLagoonGableSource",MeshTag="TUMP_LAGOON_GABLE_MESH:";
        private static readonly int[] Ids={0,8,12,16};
        private static readonly Vector2[] Sizes={new Vector2(5.4f,4.5f),new Vector2(4.6f,4.2f),new Vector2(4.2f,4),new Vector2(4.2f,3.7f)};
        internal sealed class Geometry
        {
            private readonly List<Vector3> vertices=new List<Vector3>();
            private readonly List<int>[] indices={new List<int>(),new List<int>()};
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int material)
            {int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});indices[material].AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            public void Tuft(Vector3 start,Vector3 end,float width)
                =>TuftAcross(start,end,width,Vector3.forward);
            public void TuftAcross(Vector3 start,Vector3 end,float width,Vector3 across)
            {
                var p=new[]{start-across*width*.5f,start+across*width*.5f,end+across*width*.3f,end-across*width*.3f};
                if(Vector3.Cross(p[1]-p[0],p[2]-p[0]).y<0)Array.Reverse(p);
                var n=Vector3.Cross(p[1]-p[0],p[2]-p[0]).normalized*.018f;
                Quad(p[0],p[1],p[2],p[3],0);Quad(p[3]-n,p[2]-n,p[1]-n,p[0]-n,0);
                for(int i=0;i<4;i++){int j=(i+1)%4;Quad(p[i]-n,p[j]-n,p[j],p[i],0);}
            }
            public void Box(Vector3 p,Vector3 size)
            {
                var a=p-size*.5f;var b=p+size*.5f;
                Quad(new Vector3(a.x,a.y,b.z),new Vector3(b.x,a.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(a.x,b.y,b.z),1);
                Quad(new Vector3(b.x,a.y,a.z),new Vector3(a.x,a.y,a.z),new Vector3(a.x,b.y,a.z),new Vector3(b.x,b.y,a.z),1);
                Quad(new Vector3(a.x,a.y,a.z),new Vector3(a.x,a.y,b.z),new Vector3(a.x,b.y,b.z),new Vector3(a.x,b.y,a.z),1);
                Quad(new Vector3(b.x,a.y,b.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(b.x,b.y,b.z),1);
                Quad(new Vector3(a.x,b.y,a.z),new Vector3(a.x,b.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(b.x,b.y,a.z),1);
                Quad(new Vector3(a.x,a.y,b.z),new Vector3(a.x,a.y,a.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,a.y,b.z),1);
            }
            public Mesh Mesh(string name)
            {
                var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.subMeshCount=2;
                mesh.SetTriangles(indices[0],0);mesh.SetTriangles(indices[1],1);mesh.RecalculateNormals();mesh.RecalculateBounds();
                var uv=new Vector2[vertices.Count];var normals=mesh.normals;
                for(int i=0;i<uv.Length;i++)uv[i]=RoofUv(vertices[i],normals[i]);mesh.uv=uv;return mesh;
            }
        }
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene(LagoonBuilder.ScenePath);var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/lagoon-gable-finish");File.WriteAllText("Logs/lagoon-gable-finish/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("Lagoon");var homes=map.transform.Find("Supported homes");
            var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var texture=Fibres();var binding=Binding();
            int total=0;
            for(int index=0;index<Ids.Length;index++)
            {
                int id=Ids[index];string name=(id==0?"Neighbourhood home ":"Detached stilt home ")+id+" ThatchGable";
                var home=homes.Find(name);if(home==null)throw new InvalidOperationException("Reassess changed gable family: "+name);
                var old=home.Find(RootName);if(old!=null)Object.DestroyImmediate(old.gameObject);
                var filter=home.GetComponent<MeshFilter>();var renderer=home.GetComponent<MeshRenderer>();
                var original=filter.sharedMesh;string sourcePath=AssetDatabase.GetAssetPath(original);
                var input=AssetImporter.GetAtPath(sourcePath);
                if(input!=null&&input.userData.StartsWith(MeshTag)){sourcePath=input.userData.Substring(MeshTag.Length);original=AssetDatabase.LoadAssetAtPath<Mesh>(sourcePath);}
                var slots=renderer.sharedMaterials;int roof=-1;
                for(int m=0;m<slots.Length;m++)
                {
                    string source=slots[m].GetTag(SourceTag,false);if(!string.IsNullOrEmpty(source))slots[m]=AssetDatabase.LoadAssetAtPath<Material>(source);
                    if(MapSurfaceAuthor.SourceMaterialNameForAuthoring(slots[m]).Replace(" ","").StartsWith("Layeredpalmthatch",StringComparison.OrdinalIgnoreCase))
                    {if(roof>=0)throw new InvalidOperationException("Unexpected extra thatch slot.");roof=m;}
                }
                if(roof<0)throw new InvalidOperationException("Missing original thatch material on "+name);
                var draft=Object.Instantiate(original);draft.name="Gable thatch metre UV "+id;var uv=draft.uv;
                var positions=draft.vertices;var normals=draft.normals;var selected=new HashSet<int>(draft.GetTriangles(roof));
                for(int m=0;m<draft.subMeshCount;m++)if(m!=roof&&draft.GetTriangles(m).Any(selected.Contains))
                    throw new InvalidOperationException("Roof shares vertices with a preserved material slot.");
                foreach(int v in selected)uv[v]=RoofUv(positions[v],normals[v])+new Vector2(id*.173f,id*.291f);
                draft.uv=uv;
                if(!original.vertices.SequenceEqual(draft.vertices)||!original.triangles.SequenceEqual(draft.triangles))throw new InvalidOperationException("Gable source geometry changed.");
                string path=Folder+"/Gable"+id+".asset";var saved=Save(draft,path);var importer=AssetImporter.GetAtPath(path);importer.userData=MeshTag+sourcePath;importer.SaveAndReimport();
                filter.sharedMesh=saved;slots[roof]=RoofMaterial(slots[roof],texture,id);renderer.sharedMaterials=slots;
                float width=Sizes[index].x+.9f,depth=Sizes[index].y+.9f,eave=2.15f+(id%3)*.12f;
                var g=new Geometry();var random=new System.Random(20260924+id);int count=Mathf.CeilToInt(depth/.16f);
                foreach(float side in new[]{-1f,1f})for(int i=0;i<count;i++)
                {
                    float z=-depth*.5f+(i+.5f)*depth/count,w=depth/count*.72f;
                    float reach=.085f+(float)random.NextDouble()*.10f,drop=.03f+(float)random.NextDouble()*.04f;
                    g.Tuft(new Vector3(side*(width*.5f-.075f),eave+.105f,z),new Vector3(side*(width*.5f+reach),eave-drop,z),w);
                }
                foreach(float factor in new[]{-.36f,-.12f,.12f,.36f})
                {
                    float y=eave+1.24f+.10f,z=factor*depth;
                    g.Box(new Vector3(0,y+.095f,z),new Vector3(.35f,.025f,.075f));
                    foreach(float side in new[]{-1f,1f})g.Box(new Vector3(side*.16f,y,z),new Vector3(.025f,.20f,.075f));
                }
                var detail=g.Mesh("Fitted palm eaves and ridge ties "+id);total+=detail.vertexCount;
                if(detail.bounds.min.y<eave-.10f||detail.bounds.max.x>width*.5f+.20f||detail.bounds.min.x<-width*.5f-.20f)
                    throw new InvalidOperationException("Thatch finish detached from its retained roof.");
                var go=new GameObject(RootName);go.transform.SetParent(home,false);go.isStatic=true;go.AddComponent<MeshFilter>().sharedMesh=Save(detail,Folder+"/Fringe"+id+".asset");
                var r=go.AddComponent<MeshRenderer>();r.sharedMaterials=new[]{slots[roof],binding};r.shadowCastingMode=ShadowCastingMode.Off;
                AirborneByDesign.Attach(go,"Modest eave fibres overlap the retained roof; ridge ties wrap its existing cap.");
                report.AppendLine(name+": original topology/non-roof slots retained; roof fibre material and "+count*2+" small supported eave tufts.");
            }
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Gable finish changed collision.");
            report.AppendLine("Four gabled roofs only; "+total+" added fringe/tie vertices, four renderers, no new collider/shadow caster. Other families, deck, water recovery and flying birds unchanged.");
        }
        internal static Vector2 RoofUv(Vector3 p,Vector3 normal)
        {
            var down=Vector3.ProjectOnPlane(Vector3.down,normal).normalized;
            if(down.sqrMagnitude<.1f)down=Vector3.right;
            var across=Vector3.Cross(normal,down).normalized;
            return new Vector2(Vector3.Dot(p,across)/.72f,Vector3.Dot(p,down)/1.2f);
        }
        private static Texture2D Fibres()
        {
            const int w=128,h=256;var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);var pixels=new Color[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                float u=x/(float)w,v=y/(float)h;int bundle=Mathf.FloorToInt(u*6);
                float strand=Mathf.Cos(u*Mathf.PI*2*48+Mathf.Sin(v*Mathf.PI*2)*.65f);
                float tone=Mathf.Repeat(Mathf.Sin((bundle+3)*17.3f)*137.7f,1)-.5f;
                float lap=Mathf.Pow(.5f+.5f*Mathf.Cos((v*2+(bundle%2)*.16f)*Mathf.PI*2),16);
                float value=.85f+strand*.025f+tone*.095f-lap*.075f;
                pixels[y*w+x]=new Color(value,value,value,1);
            }
            tex.SetPixels(pixels);tex.Apply();string path=Folder+"/PalmFibres.png";File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Bilinear;
            importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static Material RoofMaterial(Material source,Texture2D texture,int id)
        {
            var draft=Visual.NearFade.CopySurfaceForAuthoring(source);var c=source.color;
            draft.name="Layered gable palm "+id;draft.color=new Color(c.r*1.06f/.85f,c.g*1.025f/.85f,c.b*.98f/.85f,c.a);
            draft.mainTexture=texture;draft.mainTextureScale=Vector2.one;draft.mainTextureOffset=Vector2.zero;
            draft.SetFloat("_SurfaceKind",0);draft.SetFloat("_SurfaceVertexRoles",0);draft.SetFloat("_Glossiness",.08f);
            draft.SetOverrideTag(SourceTag,AssetDatabase.GetAssetPath(source));string path=Folder+"/Palm"+id+".mat";var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(saved==null){AssetDatabase.CreateAsset(draft,path);saved=draft;}else{EditorUtility.CopySerialized(draft,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(draft);}return saved;
        }
        private static Material Binding()
        {
            string path=Folder+"/RidgeBindings.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
            m.color=new Color(.65f,.58f,.41f);m.SetFloat("_Glossiness",.1f);EditorUtility.SetDirty(m);return m;
        }
        private static Mesh Save(Mesh mesh,string path)
        {var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);return saved;}
    }
}
