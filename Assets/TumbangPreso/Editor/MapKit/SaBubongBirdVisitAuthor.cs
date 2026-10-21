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
    public static class SaBubongBirdVisitAuthor
    {
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/SaBubong.unity",OpenSceneMode.Single);
            var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/sabubong-bird-visits");File.WriteAllText("Logs/sabubong-bird-visits/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var life=UnityEngine.Object.FindFirstObjectByType<AmbientLife>();
            using(var geometry=new AmbientLifeAuthor.SurfaceQueries())
            foreach(var bird in life.Animals.Where(a=>a.Bird&&!a.AerialWander))
            {
                var origin=bird.Route[1];var sites=new List<Vector3>();
                if(bird.Id!="fantail")sites.Add(origin);
                var offsets=bird.Id=="maya"?new[]{new Vector3(-1.3f,0,.9f),new Vector3(.9f,0,1.3f),new Vector3(.8f,0,-1.1f)}:
                    bird.Id=="kalapati"?new[]{new Vector3(1.1f,0,-.9f),new Vector3(-.9f,0,-1),new Vector3(-1.3f,0,.8f)}:
                    Array.Empty<Vector3>();
                // The finished roof's shallow seams are 0.5625m apart. A bird's
                // turning envelope spans a seam, so use its real top as the foot
                // support instead of clipping the 22mm raised metal from a flat panel.
                // These three pads remain inside the retained 4.9 x 5.3m canopy.
                var candidates=bird.Id=="fantail"?new[]{
                    new Vector3(-10.25f,3.4f,-8.4f),
                    new Vector3(-11.375f,3.4f,-7.6f),
                    new Vector3(-9.125f,3.4f,-9.2f)}:offsets.Select(offset=>origin+offset);
                foreach(var candidate in candidates)
                {
                    if(sites.Count==3)break;
                    if(!AmbientLifeAuthor.Ground(candidate,out var foot)||Mathf.Abs(foot.y-origin.y)>.12f)continue;
                    if(new Vector2(foot.x,foot.z).sqrMagnitude<6.25f)continue;
                    if(Physics.CheckBox(foot+Vector3.up*.20f,new Vector3(.36f,.18f,.36f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))continue;
                    sites.Add(foot);
                }
                if(sites.Count!=3)throw new InvalidOperationException("Find three real clear landing sites for "+bird.Id);
                bird.Perches=sites.ToArray();bird.FanWatch=bird.Id=="fantail";
                bird.BirdBeatRate=bird.Id=="maya"?1.35f:bird.Id=="kalapati"?.9f:1.15f;
                bird.PeckChance=bird.FanWatch?0:bird.Id=="maya"?.65f:.8f;
                bird.PerchWait=bird.Id=="maya"?new Vector2(4,9):bird.Id=="kalapati"?new Vector2(8,16):new Vector2(6,12);
                report.AppendLine("SaBubong "+bird.Id+": "+string.Join("; ",sites)+", beat "+bird.BirdBeatRate+", wait "+bird.PerchWait+", fan watch "+bird.FanWatch);
            }
            EditorUtility.SetDirty(life);
        }
    }
}
