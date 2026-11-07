using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class TerrainPresentationTests
    {
        private readonly List<GameObject> _owned = new List<GameObject>();
        private static readonly Vector3 At = new Vector3(1200, 300, 1200);
        private GameObject Floor(Vector3 offset, Vector3 size, float tilt = 0)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); _owned.Add(go);
            go.transform.SetPositionAndRotation(At+offset, Quaternion.Euler(0, 0, tilt));
            go.transform.localScale = size; Physics.SyncTransforms(); return go;
        }
        [TearDown] public void After()
        { foreach (var go in _owned) Object.DestroyImmediate(go); _owned.Clear(); Physics.SyncTransforms(); }

        [Test]
        public void AnUltimateUsesTheFloorUnderItsFeetInsteadOfTheOverheadStep()
        {
            Floor(new Vector3(0,-.5f,0), new Vector3(8,1,8));
            Floor(new Vector3(0,.65f,0), new Vector3(8,.1f,8));
            Assert.AreEqual(At.y, WorldGround.UnderFeet(At+Vector3.up*.08f), .002f);
            Assert.AreEqual(At.y+.7f, WorldGround.UnderFeet(At+Vector3.up*.78f), .002f);
        }

        [Test]
        public void DenseFloorQueriesStillKeepTheHighestSupport()
        {
            for (int i=0;i<80;i++) Floor(new Vector3(0,i*.01f,0),new Vector3(3,.005f,3));
            Assert.IsTrue(WorldGround.TryBelow(At+Vector3.up, .25f, 3f, out float floor));
            Assert.AreEqual(At.y+.7925f, floor, .002f);
        }

        private static Vector3 RingPoint(Vector3 point, float referenceHeight)
        {
            var method = typeof(TrajectoryPreview).GetMethod("ProjectRingPoint", BindingFlags.NonPublic|BindingFlags.Static);
            var args = new object[] { point, referenceHeight, Vector3.zero };
            Assert.IsTrue((bool)method.Invoke(null,args)); return (Vector3)args[2];
        }
        [Test]
        public void ThrowRingFollowsBothSidesOfAStep()
        {
            Floor(new Vector3(0,-.5f,0),new Vector3(8,1,8));
            Floor(new Vector3(2,.3f,0),new Vector3(4,.6f,8));
            var low=RingPoint(At+Vector3.left*.4f,At.y);
            var high=RingPoint(At+Vector3.right*.4f,At.y);
            Assert.AreEqual(At.y+TrajectoryPreview.FloorEpsilon,low.y,.002f);
            Assert.AreEqual(At.y+.6f+TrajectoryPreview.FloorEpsilon,high.y,.002f);
        }
        [Test]
        public void ThrowRingFollowsASlopedLandingSurface()
        {
            var slab=Floor(Vector3.down*.5f,new Vector3(8,1,8),20);
            foreach(float x in new[]{-.4f,0,.4f})
            {
                Vector3 point=At+Vector3.right*x;
                Assert.IsTrue(slab.GetComponent<Collider>().Raycast(new Ray(point+Vector3.up*3,Vector3.down),out var hit,8));
                Assert.AreEqual(hit.point.y+TrajectoryPreview.FloorEpsilon,RingPoint(point,At.y).y,.002f);
            }
        }
    }
}
