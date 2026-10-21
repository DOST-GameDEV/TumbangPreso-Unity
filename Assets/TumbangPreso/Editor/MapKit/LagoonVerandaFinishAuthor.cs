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
    public static class LagoonVerandaFinishAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/LagoonVerandaFinish",RootName="Veranda palm and weave";
        private const string SourceTag="TumpLagoonVerandaSource",MeshTag="TUMP_LAGOON_VERANDA_MESH:";
        private static readonly int[] Ids={3,5,6,10,17};
        private static readonly Vector2[] Sizes={new Vector2(5.5f,4.8f),new Vector2(6.1f,4.7f),new Vector2(5,4.5f),new Vector2(5,4.3f),new Vector2(4.8f,4.2f)};
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene(LagoonBuilder.ScenePath);var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/lagoon-veranda-finish");File.WriteAllText("Logs/lagoon-veranda-finish/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("Lagoon");var homes=map.transform.Find("Supported homes");
            var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var palm=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TumbangPreso/Art/LagoonGableFinish/PalmFibres.png");
            var binding=AssetDatabase.LoadAssetAtPath<Material>("Assets/TumbangPreso/Art/LagoonGableFinish/RidgeBindings.mat");
            if(palm==null||binding==null)throw new InvalidOperationException("Missing authored palm material inputs.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var bambooTexture=BambooFibres();int total=0,screens=0;
            for(int i=0;i<Ids.Length;i++)
            {
                int id=Ids[i];string kind=id==5?"CommunityShade":"ScreenVeranda";
                var home=homes.Find((id<8?"Neighbourhood home ":"Detached stilt home ")+id+" "+kind);
                if(home==null)throw new InvalidOperationException("Changed veranda family: "+id);
                var old=home.Find(RootName);if(old!=null)Object.DestroyImmediate(old.gameObject);
                var filter=home.GetComponent<MeshFilter>();var renderer=home.GetComponent<MeshRenderer>();
                var original=filter.sharedMesh;string sourcePath=AssetDatabase.GetAssetPath(original);var input=AssetImporter.GetAtPath(sourcePath);
                if(input!=null&&input.userData.StartsWith(MeshTag)){sourcePath=input.userData.Substring(MeshTag.Length);original=AssetDatabase.LoadAssetAtPath<Mesh>(sourcePath);}
                var slots=renderer.sharedMaterials;int roof=-1,bamboo=-1;
                for(int m=0;m<slots.Length;m++)
                {
                    string source=slots[m].GetTag(SourceTag,false);if(!string.IsNullOrEmpty(source))slots[m]=AssetDatabase.LoadAssetAtPath<Material>(source);
                    string name=MapSurfaceAuthor.SourceMaterialNameForAuthoring(slots[m]).Replace(" ","");
                    if(name.StartsWith("Layeredpalmthatch",StringComparison.OrdinalIgnoreCase))roof=m;
                    if(name.Equals("Splitbambooscreen",StringComparison.OrdinalIgnoreCase))bamboo=m;
                }
                if(roof<0||(id!=5&&bamboo<0))throw new InvalidOperationException("Required roof/screen slots missing on "+id);
                var mesh=Object.Instantiate(original);mesh.name="Veranda material coordinates "+id;
                var points=mesh.vertices;var normals=mesh.normals;var sourceUv=mesh.uv;var uv=(Vector2[])sourceUv.Clone();
                var roofVertices=new HashSet<int>(mesh.GetTriangles(roof));
                foreach(int v in roofVertices)uv[v]=LagoonGableFinishAuthor.RoofUv(points[v],normals[v])+new Vector2(id*.19f,id*.27f);
                if(bamboo>=0)
                {
                    var tris=mesh.GetTriangles(bamboo);var screenVertices=new HashSet<int>(tris);
                    if(screenVertices.Overlaps(roofVertices))throw new InvalidOperationException("Screen and roof share a vertex.");
                    // The existing weave has real vertical and horizontal strip patches.
                    // Its original UV unit square follows each patch, so UV derivatives
                    // identify the long fibre axis without relying on world orientation.
                    for(int t=0;t<tris.Length;t+=3)
                    {
                        int a=tris[t],b=tris[t+1],c=tris[t+2];var p=points[b]-points[a];var q=points[c]-points[a];
                        var s=sourceUv[b]-sourceUv[a];var u=sourceUv[c]-sourceUv[a];float det=s.x*u.y-s.y*u.x;
                        if(Mathf.Abs(det)<.000001f)throw new InvalidOperationException("Woven strip has no usable UV basis.");
                        var alongU=(p*u.y-q*s.y)/det;var alongV=(q*s.x-p*u.x)/det;
                        bool swap=alongU.sqrMagnitude>alongV.sqrMagnitude;
                        foreach(int v in new[]{a,b,c})uv[v]=swap?new Vector2(sourceUv[v].y,sourceUv[v].x):sourceUv[v];
                    }
                    slots[bamboo]=Material(slots[bamboo],bambooTexture,id,true);screens++;
                }
                for(int m=0;m<mesh.subMeshCount;m++)if(m!=roof&&m!=bamboo&&mesh.GetTriangles(m).Any(v=>uv[v]!=sourceUv[v]))
                    throw new InvalidOperationException("A preserved material slot's UV changed.");
                mesh.uv=uv;
                if(!original.vertices.SequenceEqual(mesh.vertices)||!original.triangles.SequenceEqual(mesh.triangles))throw new InvalidOperationException("Veranda geometry changed.");
                string path=Folder+"/Veranda"+id+".asset";var saved=Save(mesh,path);var importer=AssetImporter.GetAtPath(path);importer.userData=MeshTag+sourcePath;importer.SaveAndReimport();
                filter.sharedMesh=saved;slots[roof]=Material(slots[roof],palm,id,false);renderer.sharedMaterials=slots;
                float width=Sizes[i].x+.9f,depth=Sizes[i].y+.9f,eave=2.15f+(id%3)*.12f;
                var g=new LagoonGableFinishAuthor.Geometry();var random=new System.Random(20261124+id);int count=Mathf.CeilToInt(depth/.19f);
                foreach(float side in new[]{-1f,1f})for(int j=0;j<count;j++)
                {
                    float z=-depth*.5f+(j+.5f)*depth/count,reach=.07f+(float)random.NextDouble()*.07f;
                    g.Tuft(new Vector3(side*(width*.5f-.075f),eave+.105f,z),new Vector3(side*(width*.5f+reach),eave-.025f,z),.12f);
                }
                foreach(float factor in new[]{-.32f,0,.32f})
                {
                    float y=eave+.72f+.10f,z=factor*depth;g.Box(new Vector3(0,y+.095f,z),new Vector3(.35f,.025f,.07f));
                    foreach(float side in new[]{-1f,1f})g.Box(new Vector3(side*.16f,y,z),new Vector3(.025f,.20f,.07f));
                }
                var detail=g.Mesh("Veranda palm ends "+id);total+=detail.vertexCount;
                if(detail.bounds.min.y<eave-.08f||Mathf.Max(Mathf.Abs(detail.bounds.min.x),detail.bounds.max.x)>width*.5f+.16f)
                    throw new InvalidOperationException("Veranda palm ends are not fitted to the retained roof.");
                var go=new GameObject(RootName);go.transform.SetParent(home,false);go.isStatic=true;go.AddComponent<MeshFilter>().sharedMesh=Save(detail,Folder+"/Fringe"+id+".asset");
                var finish=go.AddComponent<MeshRenderer>();finish.sharedMaterials=new[]{slots[roof],binding};finish.shadowCastingMode=ShadowCastingMode.Off;
                AirborneByDesign.Attach(go,"Small fibre ends and ridge wraps fitted to the existing low palm roof.");
                report.AppendLine(id+" "+kind+": low-palm texture/edges"+(bamboo>=0?", actual woven-strip fibre axes":"")+"; original topology/other slots retained.");
            }
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Veranda finish changed collision.");
            report.AppendLine("Five low roofs and "+screens+" woven screens; "+total+" added edge vertices/five renderers, no new colliders/shadow casters. Gable/hip/gameplay/birds unchanged.");
        }
        private static Texture2D BambooFibres()
        {
            const int w=128,h=32;var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);var pixels=new Color[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                float u=x/(float)(w-1),v=y/(float)(h-1);float edge=Mathf.Pow(Mathf.Abs(u-.5f)*2,8);
                float fibre=Mathf.Sin(u*Mathf.PI*18+Mathf.Sin(v*Mathf.PI*2)*.18f);
                float value=.86f-edge*.10f+fibre*.023f+Mathf.Sin(v*Mathf.PI)*.016f;pixels[y*w+x]=new Color(value,value,value,1);
            }
            tex.SetPixels(pixels);tex.Apply();string path=Folder+"/BambooStrip.png";File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static Material Material(Material source,Texture2D texture,int id,bool bamboo)
        {
            var draft=Visual.NearFade.CopySurfaceForAuthoring(source);var c=source.color;float grey=bamboo?.84f:.85f;
            draft.name=(bamboo?"Woven split bamboo ":"Low palm roof ")+id;
            draft.color=new Color(c.r/grey,c.g/grey,c.b/grey,c.a);draft.mainTexture=texture;
            draft.mainTextureScale=Vector2.one;draft.mainTextureOffset=Vector2.zero;draft.SetFloat("_SurfaceKind",0);draft.SetFloat("_SurfaceVertexRoles",0);
            draft.SetFloat("_Glossiness",bamboo?.16f:.08f);draft.SetOverrideTag(SourceTag,AssetDatabase.GetAssetPath(source));
            string path=Folder+"/"+(bamboo?"Bamboo":"Palm")+id+".mat";var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(saved==null){AssetDatabase.CreateAsset(draft,path);saved=draft;}else{EditorUtility.CopySerialized(draft,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(draft);}return saved;
        }
        private static Mesh Save(Mesh mesh,string path)
        {var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);return saved;}
    }
}
