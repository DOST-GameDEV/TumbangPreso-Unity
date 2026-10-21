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
    public static class EskinitaCatHabitatAuthor
    {
        public static void Run()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/Eskinita.unity",OpenSceneMode.Single);
            var report=new StringBuilder();FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/eskinita-cat");File.WriteAllText("Logs/eskinita-cat/author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        public static void FinishLoadedScene(StringBuilder report)
        {
            var life=UnityEngine.Object.FindFirstObjectByType<AmbientLife>();
            if(life==null)throw new InvalidOperationException("Eskinita ambient owner missing");
            var animal=life.Animals.Single(a=>a.Id=="pusakal-tabby");
            using(var geometry=new AmbientLifeAuthor.SurfaceQueries())
            {
                var points=new List<Vector3>();
                // Keep this cat in its measured east-side neighborhood; retain
                // multiple connected choices instead of the old longest patrol.
                points.Add(animal.Route[0]);
                for(float x=6.3f;x<=7.8f;x+=.5f)
                for(float z=-12;z<=12;z+=.5f)
                    if(AmbientLifeAuthor.Ground(new Vector3(x,.3f,z),out var foot)&&AmbientLifeAuthor.ClearBody(foot)&&points.All(p=>(p-foot).sqrMagnitude>.025f))points.Add(foot);
                var links=points.Select(_=>new List<int>()).ToArray();
                for(int a=0;a<points.Count;a++)for(int b=a+1;b<points.Count;b++)
                {
                    float distance=Vector3.Distance(points[a],points[b]);
                    if(distance>1.45f||!SafeSegment(points[a],points[b]))continue;
                    links[a].Add(b);links[b].Add(a);
                }
                var connected=new HashSet<int>{0};var queue=new Queue<int>();queue.Enqueue(0);
                while(queue.Count>0)foreach(int next in links[queue.Dequeue()])if(connected.Add(next))queue.Enqueue(next);
                var kept=connected.OrderBy(i=>i).ToArray();var remap=kept.Select((id,i)=>(id,i)).ToDictionary(p=>p.id,p=>p.i);
                if(kept.Length<8)throw new InvalidOperationException("No useful connected cat habitat");
                animal.Habitat=kept.Select(i=>new AmbientLife.HabitatNode{Point=points[i],Links=links[i].Where(connected.Contains).Select(j=>remap[j]).ToArray()}).ToArray();
                var sites=new List<AmbientLife.ActivitySite>();
                void Site(string name,Vector3 near,AmbientLife.Activity kind,Vector3 look)
                {
                    int node=Enumerable.Range(0,animal.Habitat.Length).OrderBy(i=>(animal.Habitat[i].Point-near).sqrMagnitude).First();
                    if(sites.Any(s=>s.Node==node))return;
                    sites.Add(new AmbientLife.ActivitySite{Name=name,Node=node,Kind=kind,LookAt=look});
                }
                var shop=GameObject.Find("Eskinita").transform.Find("Dressing/Kalat/SariSari_E");
                if(shop==null)throw new InvalidOperationException("Tabby shop reference is missing");
                Site("Shop threshold watch",shop.position+Vector3.left,AmbientLife.Activity.Watch,shop.position);
                var tree=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                    .Where(t=>t.name.IndexOf("broadleaf",StringComparison.OrdinalIgnoreCase)>=0&&t.position.x>0)
                    .OrderBy(t=>animal.Habitat.Min(n=>(n.Point-t.position).sqrMagnitude)).FirstOrDefault();
                if(tree!=null)
                {
                    // Investigate a clear spot beside the tree; never place
                    // the muzzle through its trunk or force a dog marking pose.
                    var target=tree.position-Vector3.forward*1.2f;
                    Site("Tree-side investigation",target,AmbientLife.Activity.Investigate,tree.position);
                }
                var ordered=animal.Habitat.OrderBy(n=>n.Point.z).ToArray();
                var south=ordered[ordered.Length/4].Point;var north=ordered[ordered.Length*3/4].Point;
                Site("South verge investigation",south,AmbientLife.Activity.Investigate,south+Vector3.right);
                Site("North fence watch",north,AmbientLife.Activity.Watch,new Vector3(0,north.y,north.z));
                if(sites.Count<3)throw new InvalidOperationException("Cat habitat needs distinct usable activities");
                animal.Activities=sites.ToArray();animal.QuietCat=true;animal.WalkSpeed=.20f;animal.RunSpeed=1.10f;
                report.AppendLine("Eskinita pusakal-tabby only: "+animal.Habitat.Length+" supported nodes, "+animal.Habitat.Sum(n=>n.Links.Length)/2+" clear links.");
                foreach(var site in sites)report.AppendLine(site.Name+" / "+site.Kind+" at "+animal.Habitat[site.Node].Point);
            }
            EditorUtility.SetDirty(life);
        }
        private static bool SafeSegment(Vector3 from,Vector3 to)
        {
            if(!AmbientLifeAuthor.ClearStep(from,to))return false;
            int count=Mathf.CeilToInt(Vector3.Distance(from,to)/.2f);
            for(int i=0;i<=count;i++)
            {
                var point=Vector3.Lerp(from,to,i/(float)count);
                if(!AmbientLifeAuthor.Ground(point,out var foot)||Mathf.Abs(foot.y-point.y)>.08f||!AmbientLifeAuthor.ClearBody(point))return false;
            }
            return true;
        }
    }
}
