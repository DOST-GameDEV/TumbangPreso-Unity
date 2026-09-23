using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    public static class BayanGardenAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/BayanGarden/";
        private const string RootName="BayanGardenRefinement";
        [Serializable] private sealed class Bed { public string path,mesh;public string[] materials;public Vector3 min,max;public float[] longOffsets,heights; }
        [Serializable] private sealed class Plan { public Bed[] beds; }
        private static Plan ReadPlan()=>JsonUtility.FromJson<Plan>(File.ReadAllText("MapSource/environment/layouts/bayan-garden-refinement-20260924.json"));
        public static void ClearPrevious(string map)
        {
            if(map!="BayanPlaza")return;
            foreach(var bed in ReadPlan().beds)
            {
                var original=GameObject.Find(bed.path);
                if(original!=null)original.GetComponent<MeshRenderer>().enabled=true;
            }
            var old=GameObject.Find("BayanPlaza/Dressing/"+RootName);if(old!=null)Object.DestroyImmediate(old);
        }
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/BayanPlaza.unity");
            var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/bayan-garden");File.WriteAllText("Logs/bayan-garden/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            ClearPrevious("BayanPlaza");var map=GameObject.Find("BayanPlaza");
            var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var root=new GameObject(RootName).transform;root.SetParent(map.transform.Find("Dressing"),false);
            var foliage=LeafMaterial();int ordinal=0;
            foreach(var bed in ReadPlan().beds)
            {
                var original=GameObject.Find(bed.path);if(original==null)throw new InvalidOperationException("Missing measured bed "+bed.path);
                var renderer=original.GetComponent<MeshRenderer>();var bounds=renderer.bounds;
                if(Vector3.Distance(bounds.min,bed.min)>.02f || Vector3.Distance(bounds.max,bed.max)>.02f)
                    throw new InvalidOperationException("Reassess changed planter bounds: "+bed.path);
                var source=AssetDatabase.LoadAssetAtPath<Mesh>(bed.mesh);
                var points=source.vertices;var normals=source.normals;var uv=source.uv;
                var vertices=new List<Vector3>();var outputNormals=new List<Vector3>();var outputUv=new List<Vector2>();
                var baseIndices=new List<int>();var leafIndices=new List<int>();
                var matrix=root.worldToLocalMatrix*original.transform.localToWorldMatrix;var normalMatrix=matrix.inverse.transpose;
                var triangles=source.triangles;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    // Original atlas u.21875 is the concrete trough; u.46875 is the
                    // mint foliage. Preserve the exact base faces, UV and normals.
                    if(uv[triangles[i]].x>.4f)continue;
                    for(int c=0;c<3;c++)
                    {
                        int v=triangles[i+c];baseIndices.Add(vertices.Count);vertices.Add(matrix.MultiplyPoint3x4(points[v]));
                        outputNormals.Add(normalMatrix.MultiplyVector(normals[v]).normalized);outputUv.Add(uv[v]);
                    }
                }
                if(baseIndices.Count!=36)throw new InvalidOperationException("Expected the retained six-face planter base.");
                for(int c=0;c<bed.longOffsets.Length;c++)
                {
                    float height=bed.heights[c];Vector3 center=new Vector3(bounds.center.x,bounds.min.y+.26f+height*.43f,bounds.center.z+bed.longOffsets[c]);
                    var radii=new Vector3(.29f+(c%2)*.015f,height*.65f,c==0||c==bed.longOffsets.Length-1?.30f:.46f);
                    for(int band=0;band<4;band++)for(int side=0;side<7;side++)
                    {
                        Vector3 a=Point(band,side),b=Point(band+1,side),d=Point(band,side+1),e=Point(band+1,side+1);
                        Triangle(a,b,e,true);Triangle(a,e,d,true);
                    }
                    // A few large folded blades make these read as living shrubs,
                    // rather than the first draft's row of faceted green stones.
                    for(int leaf=0;leaf<2;leaf++)
                    {
                        float turn=(ordinal*29+c*43+leaf*155)*Mathf.Deg2Rad;
                        var forward=new Vector3(Mathf.Cos(turn),0,Mathf.Sin(turn));
                        var across=Vector3.Cross(forward,Vector3.up)*.085f;
                        var stem=center+Vector3.up*(height*.38f);
                        var tip=stem+forward*.33f+Vector3.up*.13f;
                        var mid=Vector3.Lerp(stem,tip,.55f);
                        var ridge=mid+Vector3.up*.045f;
                        var a=root.InverseTransformPoint(stem);var b=root.InverseTransformPoint(mid+across);
                        var d=root.InverseTransformPoint(mid-across);var e=root.InverseTransformPoint(tip);
                        var peak=root.InverseTransformPoint(ridge);
                        Triangle(a,b,peak);Triangle(b,e,peak);Triangle(e,d,peak);Triangle(d,a,peak);
                    }
                    Vector3 Point(int band,int side)
                    {
                        float latitude=(-90+band*45)*Mathf.Deg2Rad,angle=(side*360f/7+ordinal*13+c*17)*Mathf.Deg2Rad;
                        return root.InverseTransformPoint(center+Vector3.Scale(radii,new Vector3(Mathf.Cos(latitude)*Mathf.Cos(angle),Mathf.Sin(latitude),Mathf.Cos(latitude)*Mathf.Sin(angle))));
                    }
                    void Triangle(Vector3 a,Vector3 b,Vector3 c1,bool softCrown=false)
                    {
                        var normal=Vector3.Cross(b-a,c1-a);if(normal.sqrMagnitude<.00000001f)return;normal.Normalize();
                        foreach(var point in new[]{a,b,c1})
                        {
                            var world=root.TransformPoint(point);
                            if(Mathf.Abs(world.x)<12.5f || world.y>.91f)throw new InvalidOperationException("Foliage enters the court or hides its edge.");
                            leafIndices.Add(vertices.Count);vertices.Add(point);
                            var surfaceNormal=normal;
                            if(softCrown)
                            {
                                var radial=Vector3.Scale(world-center,new Vector3(1/(radii.x*radii.x),1/(radii.y*radii.y),1/(radii.z*radii.z)));
                                surfaceNormal=root.localToWorldMatrix.transpose.MultiplyVector(radial).normalized;
                            }
                            outputNormals.Add(surfaceNormal);
                            outputUv.Add(new Vector2(.5f,Mathf.Clamp01((world.y-bounds.min.y-.18f)/.70f)));
                        }
                    }
                }
                string name=original.name;var mesh=new Mesh{name="Bayan_"+name+"_planting"};
                mesh.SetVertices(vertices);mesh.SetNormals(outputNormals);mesh.SetUVs(0,outputUv);mesh.subMeshCount=2;
                mesh.SetTriangles(baseIndices,0);mesh.SetTriangles(leafIndices,1);mesh.RecalculateBounds();
                string meshPath=Folder+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if(saved==null){AssetDatabase.CreateAsset(mesh,meshPath);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);}
                var target=new GameObject(name).transform;target.SetParent(root,false);target.gameObject.isStatic=true;
                target.gameObject.AddComponent<MeshFilter>().sharedMesh=saved;
                target.gameObject.AddComponent<MeshRenderer>().sharedMaterials=new[]{BaseMaterial(renderer.sharedMaterial),foliage};
                renderer.enabled=false;ordinal++;
                report.AppendLine(name+": retained36planter face vertices,5authored shrub groups,"+vertices.Count+"total vertices; original renderer retained disabled.");
            }
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length || solids.Any(p=>p.Key==null || p.Key.bounds!=p.Value))
                throw new InvalidOperationException("Garden refinement changed gameplay collision.");
            report.AppendLine("Eight side beds only; original mesh/materials/transforms/collision and monument planters retained.");
        }
        private static Material BaseMaterial(Material source)
        {
            string path=Folder+"RetainedConcrete.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,path);}
            material.mainTexture=source.mainTexture;material.color=source.color;material.SetFloat("_Glossiness",.12f);
            EditorUtility.SetDirty(material);return material;
        }
        private static Material LeafMaterial()
        {
            var texture=new Texture2D(2,16,TextureFormat.RGB24,false);
            for(int y=0;y<16;y++)for(int x=0;x<2;x++)texture.SetPixel(x,y,Color.Lerp(new Color(.18f,.32f,.095f),new Color(.42f,.58f,.23f),y/15f));
            texture.Apply();string path=Folder+"LeafRamp.png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=true;importer.mipmapEnabled=false;
            importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            string matPath=Folder+"ShrubFoliage.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,matPath);}
            material.color=Color.white;material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);material.SetFloat("_Glossiness",.12f);
            EditorUtility.SetDirty(material);return material;
        }
    }
}
