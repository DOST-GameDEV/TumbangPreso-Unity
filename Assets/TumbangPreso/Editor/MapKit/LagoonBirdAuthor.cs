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
    public static class LagoonBirdAuthor
    {
        private const string RootName="Coastal bird life";
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene(LagoonBuilder.ScenePath);var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/lagoon-birds");File.WriteAllText("Logs/lagoon-birds/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var map=GameObject.Find("Lagoon");var solids=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.bounds);
            var old=map.transform.Find(RootName);if(old!=null)Object.DestroyImmediate(old.gameObject);
            if(map.GetComponentsInChildren<AmbientLife>(true).Any(l=>l.Animals.Any(a=>a.Bird)))
                throw new InvalidOperationException("Lagoon already has another bird setup; reconcile rather than duplicate it.");
            var starts=new[]{new Vector3(-15,8.5f,20),new Vector3(26,16,-22),new Vector3(12,9,18)};
            var directions=new[]{new Vector3(1,0,.2f),new Vector3(-.8f,0,.6f),new Vector3(-.6f,0,.8f)};
            var specs=new AmbientLife.Animal[3];
            for(int i=0;i<specs.Length;i++)
            {
                string model=i==2?"maya":"kalapati";
                var animal=AmbientLifeAuthor.Animal(model,new[]{starts[i],starts[i]+directions[i]*18},true);
                if(animal.Model==null||animal.Model.GetComponentsInChildren<Collider>(true).Length!=0)
                    throw new InvalidOperationException("Expected the existing non-colliding bird model: "+model);
                animal.Id="lagoon-"+model+"-"+i;animal.AerialWander=true;
                animal.FlightBounds=i==0?new Bounds(new Vector3(0,10.2f,3),new Vector3(68,5.4f,72)):
                    i==2?new Bounds(new Vector3(0,10,3),new Vector3(62,4,68)):
                    new Bounds(new Vector3(0,14,3),new Vector3(128,8,120));
                animal.GlideDuration=i==2?new Vector2(.2f,.7f):new Vector2(.8f,2.4f);specs[i]=animal;
            }
            var root=new GameObject(RootName);root.transform.SetParent(map.transform,false);root.AddComponent<AmbientLife>().Animals=specs;
            if(solids.Count!=map.GetComponentsInChildren<Collider>(true).Length||solids.Any(p=>p.Key==null||p.Key.bounds!=p.Value))
                throw new InvalidOperationException("Bird setup changed map collision.");
            report.AppendLine("Lagoon: two existing kalapati and one maya, independent curved destination flights and glide/wingbeat timing above the village. No new models, gameplay collider, network state, fixed waypoint loop or changes to other maps' ambient actors.");
        }
    }
}
