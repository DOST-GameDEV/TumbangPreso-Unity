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
    /// <summary>An operator's parked tricycle in one existing Eskinita household vehicle bay.</summary>
    public static class EskinitaVehicleAuthor
    {
        public const string RootName="EskinitaStreetLife";
        public const string OriginalVehicle="Eskinita/Dressing/Bahay/Sasakyan_Rework_Sasakyan_2_W";
        public static void ClearPrevious(string map)
        {
            if(map!="Eskinita")return;
            var previous=GameObject.Find("Eskinita/Dressing/"+RootName);
            if(previous!=null)Object.DestroyImmediate(previous);
        }
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/Eskinita.unity");
            var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Logs/eskinita-street-life");
            File.WriteAllText("Logs/eskinita-street-life/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("Eskinita");if(map==null)throw new InvalidOperationException("Eskinita only.");
            var old=GameObject.Find(OriginalVehicle);if(old==null)throw new InvalidOperationException("The retained2_Wvehicle lot is missing.");
            var originalRenderers=old.GetComponentsInChildren<MeshRenderer>(true);
            foreach(var renderer in originalRenderers)renderer.enabled=true;
            var originalBounds=BoundsOf(old);
            var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            ClearPrevious("Eskinita");
            var root=new GameObject(RootName).transform;root.SetParent(map.transform.Find("Dressing"),false);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/models/eskinita-street/eskinita-passenger-tricycle.glb");
            if(source==null)throw new InvalidOperationException("The new Eskinita tricycle must be imported first.");
            var parked=(GameObject)PrefabUtility.InstantiatePrefab(source);
            parked.name="ParkedPassengerTricycle07";parked.transform.SetParent(root,false);
            parked.transform.localScale=Vector3.one;parked.transform.rotation=Quaternion.Euler(0,102,0);
            var bounds=BoundsOf(parked);
            // Park near the gate-facing end, with a small quarter turn. Centering this
            // shorter vehicle in the old van footprint hid it behind the street pole.
            parked.transform.position+=new Vector3(originalBounds.max.x-.10f-bounds.max.x,originalBounds.min.y-bounds.min.y,originalBounds.center.z-bounds.center.z);
            bounds=BoundsOf(parked);
            if(bounds.min.x<originalBounds.min.x-.01f||bounds.max.x>originalBounds.max.x+.01f||
               bounds.min.z<originalBounds.min.z-.01f||bounds.max.z>originalBounds.max.z+.01f)
                throw new InvalidOperationException("Tricycle exceeds the existing private vehicle bay footprint.");
            if(Mathf.Abs(bounds.min.y-originalBounds.min.y)>.002f)throw new InvalidOperationException("Tricycle is not grounded.");
            var contacts=GroundContacts(parked,originalBounds.min.y);
            if(contacts.Count!=3)throw new InvalidOperationException("Expected three distinct tyre contacts, found "+contacts.Count);
            // Keep the retained object's transform/colliders and all existing bay boundaries.
            // Only its visible mesh is replaced; no player route or contact contract changes.
            foreach(var renderer in originalRenderers)renderer.enabled=false;
            foreach(var node in parked.GetComponentsInChildren<Transform>())node.gameObject.isStatic=true;
            MapSurfaceAuthor.FinishLoadedScene("Eskinita",report,root);
            if(root.GetComponentsInChildren<Collider>(true).Length!=0||solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||
                solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))throw new InvalidOperationException("Original gameplay collision changed.");
            report.AppendLine("Only Eskinita2_W: tricycle07 replaces the visible delivery vehicle inside its existing private bay; old source/colliders remain.");
            report.AppendLine("Original bounds "+originalBounds+"; new bounds "+bounds+"; tyre contacts "+contacts.Count);
            report.AppendLine("No other map, building, vehicle lot or gameplay collision changed.");
        }

        private static Bounds BoundsOf(GameObject item)
        {
            var renderers=item.GetComponentsInChildren<MeshRenderer>(true);var bounds=renderers[0].bounds;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);return bounds;
        }
        private static List<Vector3> GroundContacts(GameObject item,float ground)
        {
            var contacts=new List<Vector3>();
            foreach(var filter in item.GetComponentsInChildren<MeshFilter>())
            {
                var materials=filter.GetComponent<MeshRenderer>().sharedMaterials;var points=filter.sharedMesh.vertices;
                for(int sub=0;sub<filter.sharedMesh.subMeshCount;sub++)
                {
                    if(!materials[sub].name.StartsWith("Tyres and grips",StringComparison.Ordinal))continue;
                    foreach(int index in filter.sharedMesh.GetTriangles(sub))
                    {
                        Vector3 p=filter.transform.TransformPoint(points[index]);
                        if(Mathf.Abs(p.y-ground)>.025f)continue;
                        if(!contacts.Any(c=>Vector3.Distance(c,p)<.4f))contacts.Add(p);
                    }
                }
            }
            return contacts;
        }
    }
}
