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
    public static class RooftopStairheadFinishAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/SaBubong/StairheadFinish/",RootName="Resident stairhead finish";
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene(SaBubongBuilder.ScenePath);var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/roof-stairhead-finish");File.WriteAllText("Logs/roof-stairhead-finish/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("SaBubong");var original=GameObject.Find("SaBubong/Dressing/Residential stairhead");
            if(original==null)throw new InvalidOperationException("Missing retained stairhead.");
            var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var old=map.transform.Find("Dressing/"+RootName);if(old!=null)Object.DestroyImmediate(old.gameObject);
            var allowed=original.GetComponent<MeshRenderer>().bounds;allowed.Expand(.45f);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var parts=new[]{new List<CombineInstance>(),new List<CombineInstance>(),new List<CombineInstance>()};
            void Box(Vector3 at,Vector3 size,int material,float tilt=0)
            {
                var box=GameObject.CreatePrimitive(PrimitiveType.Cube);
                parts[material].Add(new CombineInstance{mesh=box.GetComponent<MeshFilter>().sharedMesh,
                    transform=Matrix4x4.TRS(at,Quaternion.Euler(tilt,0,0),size)});Object.DestroyImmediate(box);
            }
            foreach(float x in new[]{6.80f,7.90f})Box(new Vector3(x,1.16f,16.417f),new Vector3(.07f,2.12f,.10f),0);
            Box(new Vector3(7.35f,2.215f,16.417f),new Vector3(1.17f,.07f,.10f),0);
            Box(new Vector3(7.70f,1.17f,16.443f),new Vector3(.11f,.22f,.028f),0);
            Box(new Vector3(7.70f,1.18f,16.417f),new Vector3(.034f,.035f,.084f),0);
            Box(new Vector3(7.62f,1.18f,16.379f),new Vector3(.22f,.036f,.060f),0);
            Box(new Vector3(9.2f,2.35f,16.418f),new Vector3(.84f,.54f,.04f),1);
            foreach(float x in new[]{8.72f,9.68f})Box(new Vector3(x,2.35f,16.394f),new Vector3(.07f,.70f,.09f),0);
            foreach(float y in new[]{2.035f,2.665f})Box(new Vector3(9.2f,y,16.394f),new Vector3(1.03f,.07f,.09f),0);
            for(int i=0;i<5;i++)Box(new Vector3(9.2f,2.11f+i*.12f,16.38f),new Vector3(.82f,.065f,.10f),0,-20);
            Box(new Vector3(7.35f,2.62f,16.43f),new Vector3(.38f,.16f,.10f),0);
            Box(new Vector3(7.35f,2.62f,16.372f),new Vector3(.30f,.095f,.025f),2);
            Box(new Vector3(8,3.25f,16.289f),new Vector3(5.03f,.09f,.045f),0);
            var temporary=new List<Mesh>();var combined=new List<CombineInstance>();
            foreach(var group in parts){var part=new Mesh();part.CombineMeshes(group.ToArray(),true,true);temporary.Add(part);combined.Add(new CombineInstance{mesh=part,transform=Matrix4x4.identity});}
            var mesh=new Mesh{name="Stairhead fitted hardware"};mesh.CombineMeshes(combined.ToArray(),false,true);mesh.RecalculateBounds();
            foreach(var item in temporary)Object.DestroyImmediate(item);
            if(!allowed.Contains(mesh.bounds.min)||!allowed.Contains(mesh.bounds.max))throw new InvalidOperationException("Stairhead finish exceeds retained body envelope.");
            string path=Folder+"StairheadDetails.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);}
            var go=new GameObject(RootName);go.transform.SetParent(map.transform.Find("Dressing"),false);go.isStatic=true;
            go.AddComponent<MeshFilter>().sharedMesh=saved;
            go.AddComponent<MeshRenderer>().sharedMaterials=new[]{Mat("DullDoorSteel",new Color(.31f,.35f,.35f),.23f),Mat("VentRecess",new Color(.12f,.16f,.16f),.08f),Mat("EntryLens",new Color(.86f,.78f,.58f),.20f)};
            AirborneByDesign.Attach(go,"Door hardware, ventilation blades, entry-light housing and cap flashing fitted to the retained stairhead.");
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Stairhead finish changed collision.");
            AssetDatabase.SaveAssets();report.AppendLine($"Retained stairhead/noticeboard/mural/collision; fitted door frame/lever, five ventilation blades, light housing and front flashing. {saved.vertexCount}vertices, one renderer, no new light or runtime script.");
        }
        private static Material Mat(string name,Color color,float smooth)
        {
            string path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Glossiness",smooth);EditorUtility.SetDirty(m);return m;
        }
    }
}
