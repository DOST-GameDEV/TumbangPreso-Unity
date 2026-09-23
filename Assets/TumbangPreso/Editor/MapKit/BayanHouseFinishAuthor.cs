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
    public static class BayanHouseFinishAuthor
    {
        private const string Folder="Assets/TumbangPreso/Art/BayanHouseFinishes/",RootName="BayanHouseFinishes";
        public static void ClearPrevious(string map)
        {
            if(map!="BayanPlaza")return;
            var old=GameObject.Find("BayanPlaza/Dressing/"+RootName);if(old!=null)Object.DestroyImmediate(old);
        }
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/BayanPlaza.unity");
            var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/bayan-house-finish");File.WriteAllText("Logs/bayan-house-finish/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            ClearPrevious("BayanPlaza");var map=GameObject.Find("BayanPlaza");
            var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var root=new GameObject(RootName).transform;root.SetParent(map.transform.Find("Dressing"),false);
            var finishes=new List<(Transform root,char kind)>();
            foreach(var filter in map.transform.Find("Dressing/Bahay").GetComponentsInChildren<MeshFilter>())
            {
                var original=MapSurfaceAuthor.SourceMeshForAuthoring(filter.sharedMesh);
                string source=Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(original));
                if(source!="building-type-e" && source!="building-type-o")continue;
                char kind=source[source.Length-1];
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/retained-house-details/retained-house-"+kind+"-details.glb");
                if(prefab==null)throw new InvalidOperationException("Missing existing fitted detail model "+kind);
                var addition=(GameObject)PrefabUtility.InstantiatePrefab(prefab);addition.name="Finish_"+filter.gameObject.name;
                addition.transform.SetParent(root,false);addition.transform.SetPositionAndRotation(filter.transform.position,filter.transform.rotation);
                addition.isStatic=true;
                foreach(var collider in addition.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
                var originalBounds=filter.GetComponent<MeshRenderer>().bounds;var allowed=originalBounds;allowed.Expand(.7f);
                foreach(var renderer in addition.GetComponentsInChildren<MeshRenderer>())
                    if(!allowed.Contains(renderer.bounds.min)||!allowed.Contains(renderer.bounds.max))
                        throw new InvalidOperationException(addition.name+" is not fitted to its measured retained body.");
                finishes.Add((addition.transform,kind));
                report.AppendLine(filter.gameObject.name+": retained "+kind+" body, fitted existing timber/jalousie"+(kind=='o'?"/terrace":"")+" details.");
            }
            if(finishes.Count!=16)throw new InvalidOperationException("Reassess changed Bayan house inventory: expected16e/o bodies, found"+finishes.Count);
            MapSurfaceAuthor.FinishLoadedScene("BayanPlaza",report,root);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var cache=new Dictionary<string,Material>();
            foreach(var finish in finishes)
            {
                int changed=0;
                foreach(var renderer in finish.root.GetComponentsInChildren<MeshRenderer>())
                {
                    var materials=renderer.sharedMaterials;
                    for(int i=0;i<materials.Length;i++)
                    {
                        var original=materials[i];string semantic=MapSurfaceAuthor.SourceMaterialNameForAuthoring(original);
                        bool plank=semantic.StartsWith("Timber upper ",StringComparison.Ordinal);
                        bool joint=semantic.StartsWith("Timber course joints",StringComparison.Ordinal);
                        if(!plank&&!joint)continue;
                        string label=joint?"CourseJoints":semantic.Contains("warm")?"WarmBoards":"QuietBoards";
                        string key=finish.kind+"_"+label;
                        if(!cache.TryGetValue(key,out var material))
                        {
                            // Reuse the corrected single-plank mode: real horizontal
                            // courses already contain the joints, so no second grid.
                            var draft=new Material(original){name="Bayan_"+key};
                            draft.SetFloat("_DeckSurface",1);draft.SetFloat("_SurfaceStrength",joint?0:.8f);
                            string path=Folder+key+".mat";material=AssetDatabase.LoadAssetAtPath<Material>(path);
                            if(material==null){AssetDatabase.CreateAsset(draft,path);material=draft;}
                            else{EditorUtility.CopySerialized(draft,material);Object.DestroyImmediate(draft);}
                            EditorUtility.SetDirty(material);cache.Add(key,material);
                        }
                        materials[i]=material;changed++;
                    }
                    renderer.sharedMaterials=materials;
                }
                if(changed!=3)throw new InvalidOperationException(finish.root.name+": expected two board finishes and joint backing.");
            }
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length || solids.Any(p=>p.Key==null || p.Key.bounds!=p.Value))
                throw new InvalidOperationException("House finishes changed original collision.");
            AssetDatabase.SaveAssets();report.AppendLine("16local additions;6shared timber derivatives; bodies, lower walls, roof hues, collision and other maps retained.");
        }
    }
}
