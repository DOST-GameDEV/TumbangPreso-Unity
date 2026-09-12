using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    // Read-only scene inventory for authored map changes. No scenes/assets saved.
    public static class MapFinalInventory
    {
        [Serializable] private sealed class Surface
        {
            public string path,asset;public bool enabled;public Vector3 position,scale,rotation,min,max;
            public string[] materials;public int parentColliders;
        }
        [Serializable] private sealed class Solid
        {
            public string path,type;public bool trigger,enabled;public Vector3 min,max;
        }
        [Serializable] private sealed class Inventory
        {
            public string map;public Surface[] surfaces;public Solid[] solids;
        }
        private static string PathOf(Transform t)
        {
            string path=t.name;while(t.parent!=null){t=t.parent;path=t.name+"/"+path;}return path;
        }
        public static void Run()
        {
            Directory.CreateDirectory("Logs/map-inventory");
            foreach(string map in new[]{"Eskinita","BayanPlaza","IlalimNgTulay"})
            {
                EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/"+map+".unity",OpenSceneMode.Single);
                WriteLoadedScene(map, "Logs/map-inventory");
            }
            EditorApplication.Exit(0);
        }

        public static void WriteLoadedScene(string map, string directory)
        {
                Directory.CreateDirectory(directory);
                var inventory=new Inventory{map=map};
                inventory.surfaces=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Select(r=>new Surface{
                    path=PathOf(r.transform),asset=AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh),enabled=r.enabled,
                    position=r.transform.position,scale=r.transform.lossyScale,rotation=r.transform.eulerAngles,min=r.bounds.min,max=r.bounds.max,
                    materials=r.sharedMaterials.Select(m=>m==null?"null":m.name+" | "+m.shader.name+" | "+(m.HasProperty("_Color")?m.color.ToString():"imported")).ToArray(),
                    parentColliders=r.GetComponentsInParent<Collider>().Length}).ToArray();
                inventory.solids=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Select(c=>new Solid{
                    path=PathOf(c.transform),type=c.GetType().Name,trigger=c.isTrigger,enabled=c.enabled,min=c.bounds.min,max=c.bounds.max}).ToArray();
                File.WriteAllText(Path.Combine(directory,map+".json"),JsonUtility.ToJson(inventory,true));
                Debug.Log($"[Map inventory] {map}: {inventory.surfaces.Length} surfaces, {inventory.solids.Length} colliders");
        }
    }
}
