using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Local upper-growth shaping and corrected planter foliage; roots stay put.</summary>
    public static class EskinitaCanopyAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/EskinitaGarden";
        private const string FoliageRoot="EskinitaPlanterFoliage";
        [Serializable] private sealed class Tree { public string name,sourceMesh;public float widthFactor,crownLift;public Vector3 rootPosition,originalMin,originalMax; }
        [Serializable] private sealed class Plan { public Tree[] trees; }

        public static void ClearPrevious(string map)
        {
            if(map!="Eskinita")return;
            var root=GameObject.Find("Eskinita/Dressing/"+FoliageRoot);if(root!=null)Object.DestroyImmediate(root);
        }
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/Eskinita.unity");
            var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Logs/eskinita-garden");File.WriteAllText("Logs/eskinita-garden/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("Eskinita");if(map==null)throw new InvalidOperationException("Eskinita only.");
            var colliders=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var plan=JsonUtility.FromJson<Plan>(File.ReadAllText("MapSource/environment/layouts/eskinita-canopy-refinement-20260924.json"));
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            foreach(var recipe in plan.trees)
            {
                var tree=map.transform.Find("Dressing/MapFinalPass/"+recipe.name);
                if(tree==null||Vector3.Distance(tree.position,recipe.rootPosition)>.03f)
                    throw new InvalidOperationException("Reassess changed tree placement before shaping: "+recipe.name);
                var filter=tree.GetComponentInChildren<MeshFilter>();
                var source=MapSurfaceAuthor.SourceMeshForAuthoring(AssetDatabase.LoadAssetAtPath<Mesh>(recipe.sourceMesh));
                if(source==null)throw new InvalidOperationException("Missing recorded tree source: "+recipe.name);
                var mesh=Object.Instantiate(source);mesh.name="Eskinita_"+recipe.name;
                var points=mesh.vertices;var normals=mesh.normals;
                var toWorld=filter.transform.localToWorldMatrix;var toLocal=filter.transform.worldToLocalMatrix;
                var normalWorld=toWorld.inverse.transpose;var normalLocal=toWorld.transpose;
                for(int i=0;i<points.Length;i++)
                {
                    Vector3 p=toWorld.MultiplyPoint3x4(points[i]);Vector3 original=p;
                    float t=Mathf.Clamp01((p.y-recipe.originalMin.y-1.4f)/2.2f);
                    float w=t*t*(3-2*t),derivative=t>0&&t<1?6*t*(1-t)/2.2f:0;
                    float scale=Mathf.Lerp(1,recipe.widthFactor,w),ds=(recipe.widthFactor-1)*derivative;
                    float x=p.x-tree.position.x,z=p.z-tree.position.z;
                    var jacobian=Matrix4x4.identity;jacobian.m00=scale;jacobian.m22=scale;
                    jacobian.m01=ds*x;jacobian.m21=ds*z;jacobian.m11=1+recipe.crownLift*derivative;
                    Vector3 n=jacobian.inverse.transpose.MultiplyVector(normalWorld.MultiplyVector(normals[i]));
                    normals[i]=normalLocal.MultiplyVector(n).normalized;
                    p.x=tree.position.x+x*scale;p.z=tree.position.z+z*scale;p.y+=recipe.crownLift*w;
                    if(original.y<=recipe.originalMin.y+1.4f&&Vector3.Distance(original,p)>.0005f)
                        throw new InvalidOperationException("Lower trunk moved: "+recipe.name);
                    points[i]=toLocal.MultiplyPoint3x4(p);
                }
                mesh.vertices=points;mesh.normals=normals;mesh.RecalculateBounds();
                filter.sharedMesh=Save(mesh,Folder+"/Canopy_"+recipe.name+".asset");
                // Use the established surface author on this raw derivative, preserving its
                // original-source contract and metre coordinates for repeatable future runs.
                MapSurfaceAuthor.FinishLoadedScene("Eskinita",report,tree);
                report.AppendLine(recipe.name+": upper width factor "+recipe.widthFactor+", lift "+recipe.crownLift+"m; lower1.4m and root unchanged.");
            }
            AddPlanterFoliage(map.transform,report);
            if(colliders.Count!=map.GetComponentsInChildren<Collider>(true).Length||colliders.Any(p=>p.Key==null||p.Key.bounds!=p.Value))
                throw new InvalidOperationException("Garden finish changed original collision.");
            AssetDatabase.SaveAssets();report.AppendLine("Five canopies only; all roots/colliders and four other trees retained. Four timber-house planter colors corrected.");
        }

        private static void AddPlanterFoliage(Transform map,StringBuilder report)
        {
            ClearPrevious("Eskinita");var root=new GameObject(FoliageRoot).transform;root.SetParent(map.Find("Dressing"),false);
            var material=LeafMaterial();
            foreach(string lot in new[]{"5_W","2_E","5_E","6_E"})
            {
                var home=map.Find("Dressing/Bahay/Bahay_Rework_Bahay_"+lot);
                if(home==null)throw new InvalidOperationException("Missing planter home "+lot);
                var target=new GameObject("PlanterLeaves_"+lot).transform;target.SetParent(root,false);target.SetPositionAndRotation(home.position,home.rotation);
                var positions=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
                foreach(var filter in home.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh=filter.sharedMesh;var p=mesh.vertices;var n=mesh.normals;var oldUv=mesh.uv;var triangles=mesh.triangles;
                    var matrix=target.worldToLocalMatrix*filter.transform.localToWorldMatrix;var normalMatrix=matrix.inverse.transpose;
                    for(int t=0;t<triangles.Length;t+=3)
                    {
                        int a=triangles[t];if(Mathf.Abs(oldUv[a].x-.21875f)>.018f||oldUv[a].y>=.25f)continue;
                        for(int c=0;c<3;c++)
                        {
                            int i=triangles[t+c];Vector3 normal=normalMatrix.MultiplyVector(n[i]).normalized;
                            positions.Add(matrix.MultiplyPoint3x4(p[i])+normal*.006f);normals.Add(normal);
                            uv.Add(new Vector2(.5f,Mathf.Clamp01(oldUv[i].y/.25f)));indices.Add(positions.Count-1);
                        }
                    }
                }
                if(positions.Count<30)throw new InvalidOperationException("Expected measured planter foliage at "+lot);
                var leaves=new Mesh{name="Eskinita_planter_"+lot};leaves.SetVertices(positions);leaves.SetNormals(normals);leaves.SetUVs(0,uv);leaves.SetTriangles(indices,0);leaves.RecalculateBounds();
                target.gameObject.AddComponent<MeshFilter>().sharedMesh=Save(leaves,Folder+"/Planter_"+lot+".asset");
                var renderer=target.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                target.gameObject.isStatic=true;AirborneByDesign.Attach(target.gameObject,"Foliage-only color finish fitted to the retained shrubs in their existing solid planters.");
                report.AppendLine(lot+": corrected "+positions.Count+" foliage vertices; planter and building unchanged.");
            }
        }
        private static Mesh Save(Mesh draft,string path)
        {
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(draft,path);return draft;}
            EditorUtility.CopySerialized(draft,saved);Object.DestroyImmediate(draft);return saved;
        }
        private static Material LeafMaterial()
        {
            var texture=new Texture2D(2,16,TextureFormat.RGB24,false);
            for(int y=0;y<16;y++)for(int x=0;x<2;x++)texture.SetPixel(x,y,Color.Lerp(new Color(.19f,.29f,.14f),new Color(.40f,.53f,.28f),y/15f));
            texture.Apply();string path=Folder+"/PlanterLeafRamp.png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;
            importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            string matPath=Folder+"/PlanterFoliage.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(material==null){material=new Material(Shader.Find("TumbangPreso/NearFade"));AssetDatabase.CreateAsset(material,matPath);}
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);material.color=Color.white;
            material.SetFloat("_SurfaceKind",15);material.SetFloat("_SurfaceStrength",.45f);material.SetFloat("_Glossiness",.16f);
            EditorUtility.SetDirty(material);return material;
        }
    }
}
