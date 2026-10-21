using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    public static class LagoonWaterFinishAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/LagoonWaterFinish";
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene(LagoonBuilder.ScenePath);var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/lagoon-water-finish");File.WriteAllText("Logs/lagoon-water-finish/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("Lagoon");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var bed=map.transform.Find("Sandy lagoon bed");var water=map.transform.Find("Moving lagoon surface");
            if(bed==null||water==null)throw new InvalidOperationException("Missing existing physical bed/visual water.");
            var filter=bed.GetComponent<MeshFilter>();var source=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TumbangPreso/Art/LagoonWaterFinish/OriginalBed.asset");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            if(source==null){source=Object.Instantiate(filter.sharedMesh);source.name="Original lagoon bed";AssetDatabase.CreateAsset(source,Folder+"/OriginalBed.asset");}
            var mesh=Object.Instantiate(source);mesh.name="Wide visual lagoon bed";var points=mesh.vertices;
            float width=source.bounds.size.x*bed.lossyScale.x,depth=source.bounds.size.z*bed.lossyScale.z;
            for(int i=0;i<points.Length;i++){points[i].x*=1600/width;points[i].z*=1600/depth;}
            mesh.vertices=points;mesh.RecalculateBounds();string path=Folder+"/VisualBed.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);}
            filter.sharedMesh=saved;
            var surfaceMaterial=water.GetComponent<Renderer>().sharedMaterial;var bedMaterial=bed.GetComponent<Renderer>().sharedMaterial;
            if(surfaceMaterial.shader.name!="TumbangPreso/LagoonWater"||bedMaterial.shader.name!="TumbangPreso/LagoonBed")throw new InvalidOperationException("Unexpected lagoon surface shaders.");
            if(ShaderUtil.ShaderHasError(surfaceMaterial.shader)||ShaderUtil.ShaderHasError(bedMaterial.shader))throw new InvalidOperationException("Lagoon water/bed shader compilation failed.");
            surfaceMaterial.SetFloat("_Refinement",1);surfaceMaterial.SetFloat("_ShallowAlpha",.38f);surfaceMaterial.SetFloat("_DeepAlpha",.88f);bedMaterial.SetFloat("_Refinement",1);
            EditorUtility.SetDirty(surfaceMaterial);EditorUtility.SetDirty(bedMaterial);
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Water finish changed physical collision.");
            if(Mathf.Abs(water.position.y-LagoonWater.SurfaceY)>.0001f||Mathf.Abs(bed.GetComponent<BoxCollider>().bounds.max.y-LagoonWater.FloorY)>.0001f)
                throw new InvalidOperationException("Physical lagoon surface/bed level changed.");
            report.AppendLine("Lagoon-only clarity/bed presentation enabled. Visual bed1600m, original180m collider/transform and water levels retained. No new renderer/collider/camera-depth owner; swimming/fall/stock/boat behavior unchanged.");
        }
    }
}
