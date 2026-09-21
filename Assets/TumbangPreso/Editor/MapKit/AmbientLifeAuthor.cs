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
    /// <summary>Original animals with measured ground routes and clear bird approaches.</summary>
    public static class AmbientLifeAuthor
    {
        public const string Folder="Assets/TumbangPreso/Art/models/ambient-life";
        private static readonly string[] GroundIds={"aspin-tan","aspin-patched","aspin-cream","pusakal-tabby","pusakal-tuxedo","pusakal-ginger"};
        private static readonly string[] BirdIds={"maya","kalapati","fantail"};
        private static bool _prepared;
        [Serializable] private sealed class NativeSize {public float scale=1,walkCycleSpeed=.4f,runCycleSpeed=1.3f;}
        public static void Run()
        {
            var report=new StringBuilder();
            foreach(string map in new[]{"Eskinita","BayanPlaza","IlalimNgTulay","SaBubong"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/"+map+".unity",OpenSceneMode.Single);
                FinishLoadedScene(map,report);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();Directory.CreateDirectory("Logs");File.WriteAllText("Logs/ambient-life-author.txt",report.ToString());
            Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        private static void Prepare()
        {
            if(_prepared)return;
            Directory.CreateDirectory(Folder);
            foreach(string id in GroundIds.Concat(BirdIds))
            {
                string source="MapSource/environment/ambient-life/"+
                    (GroundIds.Contains(id)?"street-animals/study-v7/":"birds/study-v5/")+id+".glb";
                string target=Folder+"/"+id+".glb";
                if(!File.Exists(target)||!File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(target)))File.Copy(source,target,true);
            }
            AssetDatabase.Refresh();
            foreach(string id in GroundIds.Concat(BirdIds))
            {
                var importer=AssetImporter.GetAtPath(Folder+"/"+id+".glb");var serialized=new SerializedObject(importer);
                var method=serialized.FindProperty("importSettings.animationMethod");
                if(method==null)throw new InvalidOperationException("Unknown animal animation importer: "+id);
                if(method.intValue!=2){method.intValue=2;serialized.ApplyModifiedPropertiesWithoutUndo();importer.SaveAndReimport();}
            }
            _prepared=true;
        }
        private static AmbientLife.Animal Animal(string id,Vector3[] route,bool bird)
        {
            string path=Folder+"/"+id+".glb";
            var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().OrderBy(c=>c.name,StringComparer.Ordinal).ToArray();
            foreach(string name in bird?new[]{"idle","peck","fly"}:new[]{"idle","walk","run","alert"})
                if(!clips.Any(c=>c.name==name&&!c.legacy&&c.length>0))throw new InvalidOperationException(id+" lacks a nonzero Mecanim "+name);
            var size=bird?new NativeSize():JsonUtility.FromJson<NativeSize>(File.ReadAllText("MapSource/environment/ambient-life/street-animals/study-v7/"+id+".json"));
            // Authored .30m legs sweep +/-23 degrees over a .60s walking stance,
            // and +/-36 over a .276s running stance. Match clip cadence to travel.
            var animal=new AmbientLife.Animal{Id=id,Model=AssetDatabase.LoadAssetAtPath<GameObject>(path),Clips=clips,Route=route,Bird=bird,
                WalkSpeed=id.StartsWith("pusakal",StringComparison.Ordinal)?.32f:.45f,RunSpeed=id.StartsWith("pusakal",StringComparison.Ordinal)?1.8f:2.2f,
                WalkCycleSpeed=size.walkCycleSpeed,RunCycleSpeed=size.runCycleSpeed};
            if(id.StartsWith("aspin",StringComparison.Ordinal))
            {
                if(!clips.Any(c=>c.name=="pee"&&c.length>0))throw new InvalidOperationException(id+" lacks its leg-lift clip");
                FindDogSurface(animal);
            }
            return animal;
        }
        private static void FindDogSurface(AmbientLife.Animal animal)
        {
            float best=0;
            for(int i=0;i<animal.Route.Length;i++)
            foreach(var direction in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
            foreach(var hit in Physics.RaycastAll(animal.Route[i]+Vector3.up*.25f,direction,1.5f,~0,QueryTriggerInteraction.Ignore))
            {
                if(Mathf.Abs(hit.normal.y)>.25f||hit.distance<.65f)continue;
                string path=hit.transform.name;var at=hit.transform.parent;
                while(at!=null){path=at.name+"/"+path;at=at.parent;}
                float score=2-hit.distance;
                if(path.IndexOf("tree",StringComparison.OrdinalIgnoreCase)>=0||path.IndexOf("broadleaf",StringComparison.OrdinalIgnoreCase)>=0)score+=3;
                if(score<=best)continue;
                best=score;animal.PeeWaypoint=i;animal.PeeTarget=hit.point;
            }
        }
        public static void FinishLoadedScene(string map,StringBuilder report)
        {
            Prepare();var old=GameObject.Find("Neighborhood life");if(old!=null)Object.DestroyImmediate(old);
            var dressing=GameObject.Find(map+"/Dressing")??GameObject.Find("Dressing");
            var root=new GameObject("Neighborhood life");if(dressing!=null)root.transform.SetParent(dressing.transform,false);
            var specs=new List<AmbientLife.Animal>();
            using(var geometry=new SurfaceQueries())
            {
                if(map!="SaBubong")
                {
                    float edge=map=="Eskinita"?7.1f:map=="BayanPlaza"?8.8f:9.8f;
                    var west=Route(Rect.MinMaxRect(-edge-1,-12,-edge+.95f,12));
                    var east=Route(Rect.MinMaxRect(edge-.95f,-12,edge+1,12));
                    string dog=map=="Eskinita"?GroundIds[0]:map=="BayanPlaza"?GroundIds[2]:GroundIds[1];
                    string cat=map=="Eskinita"?GroundIds[3]:map=="BayanPlaza"?GroundIds[5]:GroundIds[4];
                    specs.Add(Animal(dog,west,false));specs.Add(Animal(cat,east,false));
                    report.AppendLine(map+": ground paths "+west.Length+"/"+east.Length+" samples, "+dog+" and "+cat);
                }
                var landings=map=="SaBubong"?new[]{new Vector3(-3,.1f,4),new Vector3(4,.1f,-4),new Vector3(-10.2f,3.5f,-8.4f)}:
                    new[]{new Vector3(-3,.3f,4),new Vector3(4,.3f,-4),new Vector3(0,.3f,10)};
                for(int i=0;i<landings.Length;i++)
                {
                    if(!Ground(landings[i],out var landing))throw new InvalidOperationException(map+" bird landing lacks a surface");
                    Vector3 approach=default,departure=default;bool found=false;
                    foreach(var offset in new[]{new Vector3(0,2.7f,4),new Vector3(0,2.7f,-4),new Vector3(4,2.7f,0),new Vector3(-4,2.7f,0)})
                    {
                        var air=landing+offset;
                        if(!ClearFlight(air,landing))continue;
                        approach=air;departure=air+Vector3.up*.3f;found=ClearFlight(landing,departure);if(found)break;
                    }
                    // The owner explicitly permits flight through scenery.
                    // Keep a preferred clear approach, but never block a map
                    // improvement on perfect cosmetic bird obstacle avoidance.
                    if(!found){approach=landing+new Vector3(0,2.7f,4);departure=approach+Vector3.up*.3f;}
                    specs.Add(Animal(BirdIds[i],new[]{approach,landing,departure},true));
                }
            }
            root.AddComponent<AmbientLife>().Animals=specs.ToArray();
            report.AppendLine(map+": "+specs.Count+" ambient animals, private cosmetic visits, no gameplay colliders");
        }
        private static bool Ground(Vector3 near,out Vector3 foot)
        {
            foot=default;float best=float.NegativeInfinity;
            foreach(var hit in Physics.RaycastAll(near+Vector3.up*.45f,Vector3.down,5,~0,QueryTriggerInteraction.Ignore))
            {
                if(hit.normal.y<.65f||hit.point.y>near.y+.15f||hit.point.y<near.y-.8f||hit.point.y<=best)continue;
                best=hit.point.y;foot=hit.point;
            }
            return !float.IsNegativeInfinity(best);
        }
        private static bool ClearBody(Vector3 at)
            =>Physics.OverlapBox(at+Vector3.up*.5f,new Vector3(.67f,.48f,.67f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Length==0;
        private static bool ClearStep(Vector3 a,Vector3 b)
        {
            if(Mathf.Abs(a.y-b.y)>.15f)return false;
            var delta=b-a;
            // Guard the full head/tail envelope and turning room. The first
            // small capsule admitted a route with a dog's head inside a tree.
            return !Physics.BoxCast(a+Vector3.up*.5f,new Vector3(.67f,.48f,.67f),delta.normalized,out _,Quaternion.identity,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
        }
        private static bool ClearFlight(Vector3 a,Vector3 b)
        {
            // Body clearance above the feet avoids counting the landing surface
            // itself as an obstacle, while guarding the full visible wingspan.
            Vector3 lift=Vector3.up*.40f,delta=b-a;
            return !Physics.SphereCast(a+lift,.32f,delta.normalized,out _,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
        }
        private static Vector3[] Route(Rect area)
        {
            const float step=.4f;var nodes=new Dictionary<Vector2Int,Vector3>();
            for(int x=Mathf.CeilToInt(area.xMin/step);x<=Mathf.FloorToInt(area.xMax/step);x++)
            for(int z=Mathf.CeilToInt(area.yMin/step);z<=Mathf.FloorToInt(area.yMax/step);z++)
            {
                if(Ground(new Vector3(x*step,.3f,z*step),out var foot)&&ClearBody(foot))nodes.Add(new Vector2Int(x,z),foot);
            }
            var neighbours=new Dictionary<Vector2Int,List<Vector2Int>>();
            foreach(var at in nodes.Keys)
            {
                var edges=new List<Vector2Int>();neighbours.Add(at,edges);
                foreach(var offset in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})
                {var next=at+offset;if(nodes.TryGetValue(next,out var point)&&ClearStep(nodes[at],point))edges.Add(next);}
            }
            var best=new List<Vector2Int>();var seen=new HashSet<Vector2Int>();
            foreach(var start in nodes.Keys)
            {
                if(seen.Contains(start))continue;
                var first=Flood(start,nodes,neighbours);foreach(var key in first.Keys)seen.Add(key);
                var far=first.Keys.OrderByDescending(key=>(nodes[key]-nodes[start]).sqrMagnitude).First();
                var parent=Flood(far,nodes,neighbours);
                var end=parent.Keys.OrderByDescending(key=>(nodes[key]-nodes[far]).sqrMagnitude).First();
                var path=new List<Vector2Int>();for(var at=end;at!=far;at=parent[at])path.Add(at);path.Add(far);
                if(path.Count>best.Count)best=path;
            }
            if(best.Count<8)throw new InvalidOperationException("No useful safe animal route in "+area);
            return best.Select(key=>nodes[key]).ToArray();
        }
        private static Dictionary<Vector2Int,Vector2Int> Flood(Vector2Int start,Dictionary<Vector2Int,Vector3> nodes,Dictionary<Vector2Int,List<Vector2Int>> neighbours)
        {
                var parent=new Dictionary<Vector2Int,Vector2Int>{{start,start}};var queue=new Queue<Vector2Int>();queue.Enqueue(start);
                while(queue.Count>0)
                {
                    var at=queue.Dequeue();
                    foreach(var next in neighbours[at])
                    {
                        if(parent.ContainsKey(next))continue;
                        parent.Add(next,at);queue.Enqueue(next);
                    }
                }
                return parent;
        }
        private sealed class SurfaceQueries:IDisposable
        {
            private readonly List<MeshCollider> _temporary=new List<MeshCollider>();
            public SurfaceQueries()
            {
                foreach(var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                {
                    if(filter.sharedMesh==null||filter.GetComponent<Collider>()!=null)continue;
                    var renderer=filter.GetComponent<Renderer>();if(renderer==null||!renderer.enabled)continue;
                    if(renderer.sharedMaterials.Any(m=>m!=null&&m.renderQueue>=3000))continue;
                    var query=filter.gameObject.AddComponent<MeshCollider>();query.sharedMesh=filter.sharedMesh;_temporary.Add(query);
                }
                Physics.SyncTransforms();
            }
            public void Dispose(){foreach(var query in _temporary)if(query!=null)Object.DestroyImmediate(query);Physics.SyncTransforms();}
        }
    }
}
