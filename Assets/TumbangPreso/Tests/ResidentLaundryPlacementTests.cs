using System.Linq;
using NUnit.Framework;
using TumbangPreso.EditorTools.MapKit;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class ResidentLaundryPlacementTests
    {
        [Test]
        public void InspectSavedEskinitaClotheslineAnchors()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/Eskinita.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                Physics.SyncTransforms();
                var root=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="NeighborhoodRework");
                var lines=root.Cast<Transform>().Where(t=>t.name.StartsWith("Resident laundry ")).ToArray();
                Assert.AreEqual(4,lines.Length,"Original street line plus three different household lines.");
                Assert.AreEqual(4,lines.Select(t=>t.name).Distinct().Count());
                Assert.AreEqual(8,lines.Sum(t=>t.GetComponentsInChildren<MeshFilter>().Count(f=>f.name.StartsWith("Supported rope hitch"))));
                foreach(var yard in lines.Where(t=>t.name.Contains("courtyard-")))
                {
                    var posts=yard.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Clothesline end post").ToArray();
                    Assert.AreEqual(2,posts.Length);
                    foreach(var post in posts)
                    {
                        float near=Mathf.Min(Mathf.Abs(post.bounds.min.x),Mathf.Abs(post.bounds.max.x));
                        float far=Mathf.Max(Mathf.Abs(post.bounds.min.x),Mathf.Abs(post.bounds.max.x));
                        Assert.Greater(near,8.3f,"Support entered the street or boundary.");
                        Assert.Less(far,9.7f,"Support entered the retained house facade.");
                        Assert.AreEqual(.108f,post.bounds.min.y,.005f,"Support does not meet the existing yard paving.");
                    }
                }
                var west=root.Find("PosteRework_Poste_2").GetComponent<BoxCollider>();
                var east=root.Find("PosteRework_Poste_8").GetComponent<BoxCollider>();
                var line=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Resident laundry alley-line");
                var rope=line.GetComponentsInChildren<MeshFilter>().First(f=>f.name=="Fixed clothesline");
                var points=rope.sharedMesh.vertices.Select(rope.transform.TransformPoint).ToArray();
                float lo=points.Min(v=>v.x),hi=points.Max(v=>v.x);
                var first=points.Where(v=>v.x<lo+.03f).Aggregate(Vector3.zero,(sum,v)=>sum+v)/points.Count(v=>v.x<lo+.03f);
                var last=points.Where(v=>v.x>hi-.03f).Aggregate(Vector3.zero,(sum,v)=>sum+v)/points.Count(v=>v.x>hi-.03f);
                Debug.Log("[LaundryAnchors] west="+west.bounds+" east="+east.bounds+" start="+first.ToString("F3")+" end="+last.ToString("F3")+
                    " gaps="+Vector3.Distance(first,west.ClosestPoint(first)).ToString("F3")+","+Vector3.Distance(last,east.ClosestPoint(last)).ToString("F3"));
                Assert.Less(Vector3.Distance(first,west.ClosestPoint(first)),.035f);
                Assert.Less(Vector3.Distance(last,east.ClosestPoint(last)),.035f);
            }
            finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
        }
        [TestCase("courtyard-line",0,4.4f)]
        [TestCase("courtyard-line",90,4.4f)]
        [TestCase("alley-line",0,15.5f)]
        [TestCase("alley-colour-line",0,15.5f)]
        [TestCase("courtyard-family-line",-90,4.4f)]
        [TestCase("courtyard-sheets-line",-90,4.4f)]
        public void BothPostsMeetTheActualImportedLine(string model,float yaw,float span)
        {
            var root=new GameObject("Laundry placement test");
            try
            {
                var line=ResidentLaundryAuthor.Place(root.transform,model,new Vector3(3,4,7),yaw,.1f);
                var rope=line.GetComponentsInChildren<MeshFilter>().First(f=>f.name=="Fixed clothesline");
                var points=rope.sharedMesh.vertices.Select(v=>rope.transform.TransformPoint(v)).ToArray();
                var posts=line.GetComponentsInChildren<Transform>().Where(t=>t.name=="Clothesline end post").ToArray();
                Assert.AreEqual(2,posts.Length);
                Assert.AreEqual(span,Vector3.Distance(posts[0].position,posts[1].position),.02f);
                foreach(var post in posts)
                {
                    var anchor=post.position+Vector3.up*(post.lossyScale.y*.5f-.035f);
                    Assert.Less(points.Min(p=>Vector3.Distance(p,anchor)),.02f,"Post misses its actual rope endpoint");
                    Assert.AreEqual(.1f,post.GetComponent<Renderer>().bounds.min.y,.001f);
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
