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
    public static class RooftopShadeFinishAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/SaBubong/ShadeFinish/",RootName="Resident shade finish";
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene(SaBubongBuilder.ScenePath);var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/roof-shade-finish");File.WriteAllText("Logs/roof-shade-finish/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("SaBubong");var shade=GameObject.Find("SaBubong/Dressing/Residents shade");
            if(shade==null)throw new InvalidOperationException("Missing retained shade.");
            var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var old=map.transform.Find("Dressing/"+RootName);if(old!=null)Object.DestroyImmediate(old.gameObject);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var parts=new[]{new List<CombineInstance>(),new List<CombineInstance>()};
            void Box(Vector3 at,Vector3 size,int material)
            {
                var box=GameObject.CreatePrimitive(PrimitiveType.Cube);
                parts[material].Add(new CombineInstance{mesh=box.GetComponent<MeshFilter>().sharedMesh,transform=Matrix4x4.TRS(at,Quaternion.identity,size)});
                Object.DestroyImmediate(box);
            }
            // Original posts end at3.3m and roof underside is3.2m. These members
            // meet those retained parts, leaving the resident furniture/headroom clear.
            foreach(float x in new[]{-12.35f,-8.15f})Box(new Vector3(x,3.08f,-8.4f),new Vector3(.18f,.24f,4.62f),0);
            foreach(float z in new[]{-10.6f,-9.5f,-8.4f,-7.3f,-6.2f})Box(new Vector3(-10.25f,3.12f,z),new Vector3(4.38f,.16f,.12f),0);
            // Keep the existing green roof; only restrained raised panel seams.
            for(int i=0;i<9;i++)Box(new Vector3(-12.50f+i*.5625f,3.408f,-8.4f),new Vector3(.027f,.028f,5.24f),1);
            var temporary=new List<Mesh>();var combined=new List<CombineInstance>();
            foreach(var group in parts){var part=new Mesh();part.CombineMeshes(group.ToArray(),true,true);temporary.Add(part);combined.Add(new CombineInstance{mesh=part,transform=Matrix4x4.identity});}
            var mesh=new Mesh{name="Roof shade fitted construction"};mesh.CombineMeshes(combined.ToArray(),false,true);mesh.RecalculateBounds();
            foreach(var part in temporary)Object.DestroyImmediate(part);
            if(mesh.bounds.min.y<2.95f||mesh.bounds.min.x<-12.70f||mesh.bounds.max.x>-7.80f||mesh.bounds.min.z<-11.05f||mesh.bounds.max.z>-5.75f)
                throw new InvalidOperationException("Shade finish exceeds original roof plan or headroom.");
            string path=Folder+"ShadeDetails.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);}
            var go=new GameObject(RootName);go.transform.SetParent(map.transform.Find("Dressing"),false);go.isStatic=true;
            go.AddComponent<MeshFilter>().sharedMesh=saved;
            go.AddComponent<MeshRenderer>().sharedMaterials=new[]{Mat("SupportedTimber",new Color(.37f,.26f,.16f)),Mat("GreenRoofSeams",new Color(.32f,.45f,.38f))};
            AirborneByDesign.Attach(go,"Beams/rafters meet retained shade posts and roof; seams are attached to its existing top.");
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Shade finish changed collision.");
            AssetDatabase.SaveAssets();report.AppendLine($"Retained shade/posts/furniture/plants/collision; two beams, five rafters and nine restrained roof seams. {saved.vertexCount}vertices, one renderer, no new collision.");
        }
        private static Material Mat(string name,Color color)
        {
            string path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Glossiness",.16f);EditorUtility.SetDirty(m);return m;
        }
    }
}
