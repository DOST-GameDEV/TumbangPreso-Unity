using System.Linq;
using NUnit.Framework;
using TumbangPreso.EditorTools.MapKit;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class ResidentLaundryPlacementTests
    {
        [TestCase("courtyard-line",0,4.4f)]
        [TestCase("courtyard-line",90,4.4f)]
        [TestCase("alley-line",0,15.5f)]
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
