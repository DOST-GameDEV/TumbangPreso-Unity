using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI.Hub;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class HubShapeAllocationTests
    {
        [TestCase(true)] [TestCase(false)]
        public void RepeatedAnimatedContoursReuseNoiseAndSeedChangesRemainDeterministic(bool pressable)
        {
            var root = new GameObject("Owned animated UI contour allocation check");
            try
            {
                var shape = root.AddComponent<HubShape>(); shape.Pressable = pressable; shape.Seed = 47;
                var method = typeof(HubShape).GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic);
                var build = (Action<List<Vector2>,Rect,float,float>)Delegate.CreateDelegate(typeof(Action<List<Vector2>,Rect,float,float>), shape, method);
                var points = new List<Vector2>(32); var rect = new Rect(-200,-70,400,140);
                build(points,rect,18,5); var first = points.ToArray();
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 500; i++) build(points,new Rect(-200,-70,400+i%3,140),18,5);
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.AreEqual(0, allocated, "Repeated contour rebuilds still allocate per-frame noise state.");
                build(points,rect,18,5); CollectionAssert.AreEqual(first,points);
                shape.Seed = 48; build(points,rect,18,5); CollectionAssert.AreNotEqual(first,points);
                shape.Seed = 47; build(points,rect,18,5); CollectionAssert.AreEqual(first,points);
                Debug.Log("[UIContourAllocation] pressable="+pressable+" rebuilds=500 allocatedBytes="+allocated);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
