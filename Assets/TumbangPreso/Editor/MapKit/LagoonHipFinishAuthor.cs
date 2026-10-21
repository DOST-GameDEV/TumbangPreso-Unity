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
    public static class LagoonHipFinishAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/LagoonHipFinish",RootName="Fitted hip thatch";
        private const string SourceTag="TumpLagoonHipSource",MeshTag="TUMP_LAGOON_HIP_MESH:";
        private static readonly int[] Ids={4,11,14};
        private static readonly Vector2[] Sizes={new Vector2(5.7f,5),new Vector2(4.8f,4.6f),new Vector2(4.6f,4.1f)};
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene(LagoonBuilder.ScenePath);var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/lagoon-hip-finish");File.WriteAllText("Logs/lagoon-hip-finish/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("Lagoon");var homes=map.transform.Find("Supported homes");
            var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TumbangPreso/Art/LagoonGableFinish/PalmFibres.png");
            var bindings=AssetDatabase.LoadAssetAtPath<Material>("Assets/TumbangPreso/Art/LagoonGableFinish/RidgeBindings.mat");
            if(texture==null||bindings==null)throw new InvalidOperationException("Missing the already-authored palm-fibre material inputs.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();int total=0;
            for(int i=0;i<Ids.Length;i++)
            {
                int id=Ids[i];string name=(id==4?"Neighbourhood home ":"Detached stilt home ")+id+" HipThatch";
                var home=homes.Find(name);if(home==null)throw new InvalidOperationException("Reassess changed hip family: "+name);
                var old=home.Find(RootName);if(old!=null)Object.DestroyImmediate(old.gameObject);
                var filter=home.GetComponent<MeshFilter>();var renderer=home.GetComponent<MeshRenderer>();
                var original=filter.sharedMesh;string sourcePath=AssetDatabase.GetAssetPath(original);var sourceImporter=AssetImporter.GetAtPath(sourcePath);
                if(sourceImporter!=null&&sourceImporter.userData.StartsWith(MeshTag))
                {sourcePath=sourceImporter.userData.Substring(MeshTag.Length);original=AssetDatabase.LoadAssetAtPath<Mesh>(sourcePath);}
                var slots=renderer.sharedMaterials;int roof=-1;
                for(int m=0;m<slots.Length;m++)
                {
                    string source=slots[m].GetTag(SourceTag,false);if(!string.IsNullOrEmpty(source))slots[m]=AssetDatabase.LoadAssetAtPath<Material>(source);
                    if(MapSurfaceAuthor.SourceMaterialNameForAuthoring(slots[m]).Replace(" ","").StartsWith("Layeredpalmthatch",StringComparison.OrdinalIgnoreCase))roof=m;
                }
                if(roof<0)throw new InvalidOperationException("Hip roof material missing.");
                var mesh=Object.Instantiate(original);mesh.name="Hip roof metre UV "+id;
                var uv=mesh.uv;var points=mesh.vertices;var normals=mesh.normals;var selected=new HashSet<int>(mesh.GetTriangles(roof));
                for(int m=0;m<mesh.subMeshCount;m++)if(m!=roof&&mesh.GetTriangles(m).Any(selected.Contains))throw new InvalidOperationException("Hip roof shares a preserved slot's vertices.");
                foreach(int v in selected)uv[v]=Vector2.Scale(LagoonGableFinishAuthor.RoofUv(points[v],normals[v]),new Vector2(.90f,1.10f))+new Vector2(id*.13f,id*.29f);
                mesh.uv=uv;
                if(!original.vertices.SequenceEqual(mesh.vertices)||!original.triangles.SequenceEqual(mesh.triangles))throw new InvalidOperationException("Hip source geometry changed.");
                string path=Folder+"/Hip"+id+".asset";var saved=Save(mesh,path);var importer=AssetImporter.GetAtPath(path);importer.userData=MeshTag+sourcePath;importer.SaveAndReimport();
                filter.sharedMesh=saved;slots[roof]=Material(slots[roof],texture,id);renderer.sharedMaterials=slots;
                float w=Sizes[i].x+.9f,d=Sizes[i].y+.9f,eave=2.15f+(id%3)*.12f;
                var g=new LagoonGableFinishAuthor.Geometry();var random=new System.Random(20261024+id);int tufts=0;
                foreach(float side in new[]{-1f,1f})
                {
                    int rows=Mathf.CeilToInt((d-.25f)/.18f);
                    for(int r=0;r<rows;r++)
                    {
                        float z=Mathf.Lerp(-d*.5f+.13f,d*.5f-.13f,(r+.5f)/rows),reach=.07f+(float)random.NextDouble()*.075f;
                        g.Tuft(new Vector3(side*(w*.5f-.07f),eave+.07f,z),new Vector3(side*(w*.5f+reach),eave-.025f,z),.11f);tufts++;
                    }
                    int columns=Mathf.CeilToInt((w-.25f)/.18f);
                    for(int c=0;c<columns;c++)
                    {
                        float x=Mathf.Lerp(-w*.5f+.13f,w*.5f-.13f,(c+.5f)/columns),reach=.07f+(float)random.NextDouble()*.075f;
                        g.TuftAcross(new Vector3(x,eave+.07f,side*(d*.5f-.07f)),new Vector3(x,eave-.025f,side*(d*.5f+reach)),.11f,Vector3.right);tufts++;
                    }
                }
                foreach(float factor in new[]{-.16f,0,.16f})
                {
                    float y=eave+.92f,z=d*factor;
                    g.Box(new Vector3(0,y+.079f,z),new Vector3(.18f,.025f,.06f));
                    foreach(float side in new[]{-1f,1f})g.Box(new Vector3(side*.079f,y,z),new Vector3(.025f,.18f,.06f));
                }
                var detail=g.Mesh("Hip eaves and ridge bindings "+id);total+=detail.vertexCount;
                if(detail.bounds.min.y<eave-.08f||Mathf.Max(Mathf.Abs(detail.bounds.min.x),detail.bounds.max.x)>w*.5f+.16f||
                    Mathf.Max(Mathf.Abs(detail.bounds.min.z),detail.bounds.max.z)>d*.5f+.16f)throw new InvalidOperationException("Hip fringe exceeds its fitted roof envelope.");
                var go=new GameObject(RootName);go.transform.SetParent(home,false);go.isStatic=true;go.AddComponent<MeshFilter>().sharedMesh=Save(detail,Folder+"/Fringe"+id+".asset");
                var finishRenderer=go.AddComponent<MeshRenderer>();finishRenderer.sharedMaterials=new[]{slots[roof],bindings};finishRenderer.shadowCastingMode=ShadowCastingMode.Off;
                AirborneByDesign.Attach(go,"Small palm ends overlap the retained hip eave beams; ties wrap the existing short ridge.");
                report.AppendLine(name+": four-slope palm material, "+tufts+"small eave tufts and three ridge wraps; source/non-roof slots retained.");
            }
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Hip finish changed collision.");
            report.AppendLine("Three hip roofs only; "+total+"added vertices/three renderers, no new colliders/shadow casters. Other families/gameplay/birds unchanged.");
        }
        private static Material Material(Material source,Texture2D texture,int id)
        {
            var draft=Visual.NearFade.CopySurfaceForAuthoring(source);var c=source.color;
            draft.name="Hip palm finish "+id;draft.color=new Color(c.r*1.02f/.85f,c.g/.85f,c.b*.97f/.85f,c.a);
            draft.mainTexture=texture;draft.mainTextureScale=Vector2.one;draft.mainTextureOffset=Vector2.zero;
            draft.SetFloat("_SurfaceKind",0);draft.SetFloat("_SurfaceVertexRoles",0);draft.SetFloat("_Glossiness",.08f);
            draft.SetOverrideTag(SourceTag,AssetDatabase.GetAssetPath(source));string path=Folder+"/Palm"+id+".mat";var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(saved==null){AssetDatabase.CreateAsset(draft,path);saved=draft;}else{EditorUtility.CopySerialized(draft,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(draft);}return saved;
        }
        private static Mesh Save(Mesh mesh,string path)
        {var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);return saved;}
    }
}
