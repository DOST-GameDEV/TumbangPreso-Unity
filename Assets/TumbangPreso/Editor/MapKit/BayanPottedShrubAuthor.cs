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
    public static class BayanPottedShrubAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/BayanPottedShrubs/",Group="PlantedShrub";
        private static readonly string[] Names={"MonHedge_1","RimHedge_0","RimHedge_1","RimHedge_2"};
        private sealed class Geometry
        {
            public readonly List<Vector3> Points=new List<Vector3>(),Normals=new List<Vector3>();
            public readonly List<Vector2> Uvs=new List<Vector2>();public readonly List<int> Indices=new List<int>();
            public void Triangle(Vector3 a,Vector3 b,Vector3 c,Vector3? center=null,Vector3? radii=null,bool stem=false)
            {
                var normal=Vector3.Cross(b-a,c-a);if(normal.sqrMagnitude<.000000001f)return;normal.Normalize();
                foreach(var point in new[]{a,b,c})
                {
                    var n=normal;
                    if(center.HasValue)
                    {var r=radii.Value;n=Vector3.Scale(point-center.Value,new Vector3(1/(r.x*r.x),1/(r.y*r.y),1/(r.z*r.z))).normalized;}
                    Indices.Add(Points.Count);Points.Add(point);Normals.Add(n);Uvs.Add(new Vector2(.5f,stem?.12f:Mathf.Clamp01((point.y-.45f)/.72f)));
                }
            }
            public Mesh Mesh(string name)
            {var m=new Mesh{name=name};m.SetVertices(Points);m.SetNormals(Normals);m.SetUVs(0,Uvs);m.SetTriangles(Indices,0);m.RecalculateBounds();return m;}
        }
        public static void ClearPrevious(string map)
        {
            if(map!="BayanPlaza")return;
            foreach(string name in Names)
            {
                var original=GameObject.Find("BayanPlaza/Dressing/Monument/"+name+"/default");if(original==null)continue;
                original.GetComponent<MeshRenderer>().enabled=true;
                var old=original.transform.Find(Group);if(old!=null)Object.DestroyImmediate(old.gameObject);
            }
        }
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/BayanPlaza.unity");
            var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/bayan-potted-shrubs");File.WriteAllText("Logs/bayan-potted-shrubs/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            ClearPrevious("BayanPlaza");Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var map=GameObject.Find("BayanPlaza");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/TumbangPreso/Art/BayanGarden/ShrubFoliage.mat");
            if(material==null)throw new InvalidOperationException("Bayan side planting material must exist first.");
            string soilPath=Folder+"PotSoil.mat";var soilMaterial=AssetDatabase.LoadAssetAtPath<Material>(soilPath);
            if(soilMaterial==null){soilMaterial=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(soilMaterial,soilPath);}
            soilMaterial.color=new Color(.22f,.18f,.11f);soilMaterial.SetFloat("_Glossiness",.05f);EditorUtility.SetDirty(soilMaterial);
            Vector3[][] crowns={
                new[]{new Vector3(0,.86f,0),new Vector3(-.23f,.72f,.10f),new Vector3(.22f,.73f,-.10f)},
                new[]{new Vector3(-.04f,.78f,.04f),new Vector3(-.22f,.68f,-.16f),new Vector3(.23f,.70f,.13f)},
                new[]{new Vector3(0,.86f,-.04f),new Vector3(.17f,.72f,.15f),new Vector3(-.19f,.72f,.10f)},
                new[]{new Vector3(.06f,.81f,-.06f),new Vector3(-.21f,.69f,.12f),new Vector3(.22f,.74f,.10f)}};
            for(int pot=0;pot<Names.Length;pot++)
            {
                string name=Names[pot];var original=GameObject.Find("BayanPlaza/Dressing/Monument/"+name+"/default");
                if(original==null)throw new InvalidOperationException("Missing original pot "+name);
                var oldRenderer=original.GetComponent<MeshRenderer>();var materials=oldRenderer.sharedMaterials;
                var source=original.GetComponent<MeshFilter>().sharedMesh;
                if(source.subMeshCount!=4 || !materials[0].name.StartsWith("concrete") || !materials[1].name.StartsWith("coping"))
                    throw new InvalidOperationException("Reassess changed pot construction before refining.");
                var root=new GameObject(Group).transform;root.SetParent(original.transform,false);
                var baseMesh=Object.Instantiate(source);baseMesh.name="RetainedBayanPot";baseMesh.subMeshCount=2;
                var baseGo=new GameObject("Retained concrete pot");baseGo.transform.SetParent(root,false);baseGo.isStatic=true;
                baseGo.AddComponent<MeshFilter>().sharedMesh=Save(baseMesh,Folder+"RetainedPot.asset");
                baseGo.AddComponent<MeshRenderer>().sharedMaterials=materials.Take(2).ToArray();
                // The inherited coping is a solid cap. A contained soil surface gives
                // the plant a readable growing place instead of a bare white plinth.
                var soil=GameObject.CreatePrimitive(PrimitiveType.Quad);soil.name="Contained soil";soil.transform.SetParent(root,false);
                soil.transform.localPosition=new Vector3(0,.463f,0);soil.transform.localRotation=Quaternion.Euler(90,0,0);
                soil.transform.localScale=new Vector3(.98f,.98f,1);Object.DestroyImmediate(soil.GetComponent<Collider>());soil.isStatic=true;
                soil.GetComponent<MeshRenderer>().sharedMaterial=soilMaterial;
                soil.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                var g=new Geometry();
                for(int crown=0;crown<3;crown++)
                {
                    var center=crowns[pot][crown];var radii=new Vector3(crown==0?.26f:.24f,crown==0?.22f:.18f,.25f);
                    Stem(g,new Vector3(0,.46f,0),center);
                    for(int ring=0;ring<4;ring++)for(int side=0;side<7;side++)
                    {
                        var a=Point(ring,side);var b=Point(ring+1,side);var c=Point(ring+1,side+1);var d=Point(ring,side+1);
                        g.Triangle(a,b,c,center,radii);g.Triangle(a,c,d,center,radii);
                    }
                    for(int blade=0;blade<2;blade++)
                    {
                        float angle=(pot*31+crown*77+blade*160)*Mathf.Deg2Rad;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                        var across=Vector3.Cross(direction,Vector3.up)*.075f;var stem=center+Vector3.up*.065f;
                        var tip=stem+direction*.30f+Vector3.up*.13f;var middle=Vector3.Lerp(stem,tip,.55f);var ridge=middle+Vector3.up*.045f;
                        g.Triangle(stem,middle+across,ridge);g.Triangle(middle+across,tip,ridge);
                        g.Triangle(tip,middle-across,ridge);g.Triangle(middle-across,stem,ridge);
                    }
                    Vector3 Point(int ring,int side)
                    {
                        float latitude=(-90+45*ring)*Mathf.Deg2Rad,angle=(side*360f/7+pot*13+crown*21)*Mathf.Deg2Rad;
                        return center+Vector3.Scale(radii,new Vector3(Mathf.Cos(latitude)*Mathf.Cos(angle),Mathf.Sin(latitude),Mathf.Cos(latitude)*Mathf.Sin(angle)));
                    }
                }
                if(g.Points.Any(p=>Mathf.Abs(p.x)>.60f || Mathf.Abs(p.z)>.60f || original.transform.TransformPoint(p).y>1.2f))
                    throw new InvalidOperationException(name+" foliage exceeds its retained coping/height.");
                var plant=new GameObject("Supported foliage");plant.transform.SetParent(root,false);plant.isStatic=true;
                plant.AddComponent<MeshFilter>().sharedMesh=Save(g.Mesh(name+" foliage"),Folder+name+".asset");
                plant.AddComponent<MeshRenderer>().sharedMaterial=material;oldRenderer.enabled=false;
                report.AppendLine(name+": exact concrete/coping submeshes retained;3supported crowns,"+g.Points.Count+" foliage vertices.");
            }
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length || solids.Any(p=>p.Key==null || p.Key.bounds!=p.Value))
                throw new InvalidOperationException("Potted shrubs changed collision.");
        }
        private static void Stem(Geometry g,Vector3 a,Vector3 b)
        {
            var direction=(b-a).normalized;var right=Vector3.Cross(direction,Vector3.forward).normalized*.018f;
            var across=Vector3.Cross(right,direction).normalized*.018f;
            for(int i=0;i<6;i++)
            {
                float x=i*Mathf.PI/3,y=(i+1)*Mathf.PI/3;
                var p=right*Mathf.Cos(x)+across*Mathf.Sin(x);var q=right*Mathf.Cos(y)+across*Mathf.Sin(y);
                g.Triangle(a+p,b+p,b+q,stem:true);g.Triangle(a+p,b+q,a+q,stem:true);
            }
        }
        private static Mesh Save(Mesh draft,string path)
        {
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(draft,path);return draft;}
            EditorUtility.CopySerialized(draft,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(draft);return saved;
        }
    }
}
