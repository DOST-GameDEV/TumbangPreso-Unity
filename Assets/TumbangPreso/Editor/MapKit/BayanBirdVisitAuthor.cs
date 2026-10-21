using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    public static class BayanBirdVisitAuthor
    {
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/BayanPlaza.unity",OpenSceneMode.Single);
            var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/bayan-bird-visits");File.WriteAllText("Logs/bayan-bird-visits/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var life=UnityEngine.Object.FindFirstObjectByType<AmbientLife>();
            using(var geometry=new AmbientLifeAuthor.SurfaceQueries())
            foreach(var bird in life.Animals.Where(a=>a.Bird&&!a.AerialWander))
            {
                var origin=bird.Route[1];var sites=new List<Vector3>{origin};
                var offsets=bird.Id=="maya"?new[]{new Vector3(-1.8f,0,1),new Vector3(.7f,0,2.2f),new Vector3(-1,0,-1.3f)}:
                    bird.Id=="kalapati"?new[]{new Vector3(2.4f,0,-.8f),new Vector3(.4f,0,-2.4f),new Vector3(-1.4f,0,-1.8f)}:
                    new[]{new Vector3(-1.8f,0,-1.4f),new Vector3(1.8f,0,-1.4f),new Vector3(-2.4f,0,-2.2f),new Vector3(2.4f,0,-2.2f)};
                foreach(var offset in offsets)
                {
                    if(sites.Count==3)break;
                    if(!AmbientLifeAuthor.Ground(origin+offset,out var foot)||Mathf.Abs(foot.y-origin.y)>.12f)continue;
                    if(new Vector2(foot.x,foot.z).sqrMagnitude<6.25f)continue;
                    if(Physics.CheckBox(foot+Vector3.up*.20f,new Vector3(.36f,.18f,.36f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))continue;
                    sites.Add(foot);
                }
                if(sites.Count!=3)throw new InvalidOperationException("Find three real clear landing sites for "+bird.Id);
                bird.Perches=sites.ToArray();bird.FanWatch=bird.Id=="fantail";
                bird.BirdBeatRate=bird.Id=="maya"?1.35f:bird.Id=="kalapati"?.9f:1.15f;
                bird.PeckChance=bird.FanWatch?0:bird.Id=="maya"?.65f:.8f;
                bird.PerchWait=bird.Id=="maya"?new Vector2(4.5f,10):bird.Id=="kalapati"?new Vector2(9,16):new Vector2(4,9);
                report.AppendLine("BayanPlaza "+bird.Id+": "+string.Join("; ",sites)+", beat "+bird.BirdBeatRate+", wait "+bird.PerchWait+", fan watch "+bird.FanWatch);
            }
            EditorUtility.SetDirty(life);
        }
    }
}
