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
    /// <summary>Secondary glazing divisions on the three retained straight-sided B towers.</summary>
    public static class RooftopTowerBFinishAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/SaBubong/TowerBFinish",RootName="Straight tower glazing finish";
        private sealed class Face
        {
            public Vector3 Normal,Horizontal;public float Plane;
            public Vector2 Min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),Max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            public readonly List<Vector2[]> Triangles=new List<Vector2[]>();
            public Vector3 Point(Vector2 p)=>Horizontal*p.x+Vector3.up*p.y+Normal*Plane;
            public bool Contains(Vector2 p)
            {
                float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
                foreach(var t in Triangles)
                {float a=Cross(t[1]-t[0],p-t[0]),b=Cross(t[2]-t[1],p-t[1]),c=Cross(t[0]-t[2],p-t[2]);
                    if((a>=-.000001f&&b>=-.000001f&&c>=-.000001f)||(a<=.000001f&&b<=.000001f&&c<=.000001f))return true;}
                return false;
            }
            public bool Contains(Rect r)
            {foreach(float x in new[]{r.xMin,r.center.x,r.xMax})foreach(float y in new[]{r.yMin,r.center.y,r.yMax})if(!Contains(new Vector2(x,y)))return false;return true;}
        }
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene(SaBubongBuilder.ScenePath);var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/rooftop-tower-b");File.WriteAllText("Logs/rooftop-tower-b/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("SaBubong");var city=map.transform.Find("Dressing/Metro rooftops");
            var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var old=city.Find(RootName);if(old!=null)Object.DestroyImmediate(old.gameObject);
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var indices=new List<int>();int strips=0;
            foreach(int id in new[]{1,6,11})
            {
                var body=city.Find("CityBlock_"+id);var filter=body.GetComponent<MeshFilter>();
                var original=MapSurfaceAuthor.SourceMeshForAuthoring(filter.sharedMesh);
                if(Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(original))!="building-skyscraper-b")
                    throw new InvalidOperationException("Retained tower family changed: "+body.name);
                int before=strips;var transform=city.worldToLocalMatrix*body.localToWorldMatrix;
                foreach(var face in Faces(filter.sharedMesh))
                {
                    float sx=body.TransformVector(face.Horizontal).magnitude,sy=body.TransformVector(Vector3.up).magnitude;
                    float width=(face.Max.x-face.Min.x)*sx,height=(face.Max.y-face.Min.y)*sy;
                    if(width<3.2f||height<5)continue;
                    int bays=width>=6?3:2;
                    for(int i=1;i<bays;i++)
                    {
                        float x=Mathf.Lerp(face.Min.x,face.Max.x,i/(float)bays);
                        var rect=Rect.MinMaxRect(x-.055f/sx,face.Min.y+.08f/sy,x+.055f/sx,face.Max.y-.08f/sy);
                        if(!face.Contains(rect))continue;
                        int first=vertices.Count;var normal=transform.inverse.transpose.MultiplyVector(face.Normal).normalized;
                        float offset=.04f/body.TransformVector(face.Normal).magnitude;
                        foreach(var p in new[]{new Vector2(rect.xMin,rect.yMin),new Vector2(rect.xMax,rect.yMin),new Vector2(rect.xMax,rect.yMax),new Vector2(rect.xMin,rect.yMax)})
                        {vertices.Add(transform.MultiplyPoint3x4(face.Point(p)+face.Normal*offset));normals.Add(normal);}
                        indices.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});strips++;
                    }
                }
                if(strips==before)throw new InvalidOperationException("No fitted glazing detail on "+body.name);
                report.AppendLine(body.name+": "+(strips-before)+" secondary divisions within original glass planes.");
            }
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var mesh=new Mesh{name="Tower B fitted mullions"};
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
            string meshPath=Folder+"/Mullions.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(saved==null){AssetDatabase.CreateAsset(mesh,meshPath);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);}
            string materialPath=Folder+"/GlazingFrames.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,materialPath);}
            material.color=new Color(.43f,.48f,.50f);material.SetFloat("_Glossiness",.24f);material.SetFloat("_Metallic",.18f);EditorUtility.SetDirty(material);
            var root=new GameObject(RootName);root.transform.SetParent(city,false);root.isStatic=true;root.AddComponent<MeshFilter>().sharedMesh=saved;
            var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            AirborneByDesign.Attach(root,"Secondary glazing members fitted to original tower B window backing planes.");
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))
                throw new InvalidOperationException("Glazing finish changed collision.");
            report.AppendLine($"Three B towers retained with {strips}secondary mullions, {vertices.Count}vertices/one renderer. Original glass/palette/body/primary frames and other tower families untouched; no new collision/shadow caster.");
        }
        private static List<Face> Faces(Mesh mesh)
        {
            var points=mesh.vertices;var uv=mesh.uv;var triangles=mesh.triangles;var groups=new Dictionary<string,Face>();
            for(int i=0;i<triangles.Length;i+=3)
            {
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2];
                if(new[]{a,b,c}.Any(v=>Mathf.Abs(uv[v].x-.71875f)>.001f))continue;
                var normal=Vector3.Cross(points[b]-points[a],points[c]-points[a]).normalized;if(Mathf.Abs(normal.y)>.01f)continue;
                float plane=Vector3.Dot(normal,points[a]);string key=Mathf.RoundToInt(normal.x*1000)+"_"+Mathf.RoundToInt(normal.z*1000)+"_"+Mathf.RoundToInt(plane*10000);
                if(!groups.TryGetValue(key,out var face)){face=new Face{Normal=normal,Horizontal=Vector3.Cross(Vector3.up,normal).normalized,Plane=plane};groups.Add(key,face);}
                var row=new[]{a,b,c}.Select(v=>new Vector2(Vector3.Dot(points[v],face.Horizontal),points[v].y)).ToArray();face.Triangles.Add(row);
                foreach(var p in row){face.Min=Vector2.Min(face.Min,p);face.Max=Vector2.Max(face.Max,p);}
            }
            return groups.Values.ToList();
        }
    }
}
