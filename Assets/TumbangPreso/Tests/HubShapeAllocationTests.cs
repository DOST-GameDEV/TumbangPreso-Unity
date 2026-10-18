using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Profiling;
using TumbangPreso.UI.Hub;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class HubShapeAllocationTests
    {
        private static void CalibrateAllocationRecorder()
        {
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 100,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            Assert.IsTrue(recorder.Valid);
            var retained = new byte[16384]; GC.KeepAlive(retained);
            recorder.Stop();
            Assert.Greater(recorder.Count, 0, "The native recorder must detect a known retained allocation.");
        }

        [Test]
        public void RepeatedBuntingDrawsAllocateNoRandomOrClosureState()
        {
            var root = new GameObject("Owned bunting allocation check", typeof(RectTransform));
            bool reduced = TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion;
            try
            {
                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion = false;
                ((RectTransform)root.transform).sizeDelta = new Vector2(1600, 160);
                var bunting = root.AddComponent<HubBunting>(); bunting.Seed = 47;
                var method = typeof(HubBunting).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(UnityEngine.UI.VertexHelper) }, null);
                var draw = (Action<UnityEngine.UI.VertexHelper>)Delegate.CreateDelegate(typeof(Action<UnityEngine.UI.VertexHelper>), bunting, method);
                using var mesh = new UnityEngine.UI.VertexHelper();
                draw(mesh); draw(mesh);
                var first = new List<UIVertex>(); mesh.GetUIVertexStream(first);
                CalibrateAllocationRecorder();
                using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 4096,
                    ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
                for (int i = 0; i < 100; i++) draw(mesh);
                recorder.Stop();
                TestContext.WriteLine("100 warm bunting draws: " + recorder.Count + " GC.Alloc events");
                Assert.AreEqual(0, recorder.Count, "The 30Hz animation still allocates on every mesh rebuild.");
                var again = new List<UIVertex>(); mesh.GetUIVertexStream(again);
                Assert.AreEqual(first.Count, again.Count);
                for (int i = 0; i < first.Count; i++)
                {
                    Assert.AreEqual(first[i].position, again[i].position);
                    Assert.AreEqual(first[i].color, again[i].color);
                }
            }
            finally
            {
                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion = reduced;
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RebuiltBuntingMatchesFreshSeedAndWidthAfterReducedMotion()
        {
            var root = new GameObject("Resized bunting", typeof(RectTransform));
            bool reduced = TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion;
            try
            {
                var bunting = root.AddComponent<HubBunting>();
                var method = typeof(HubBunting).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(UnityEngine.UI.VertexHelper) }, null);
                var draw = (Action<UnityEngine.UI.VertexHelper>)Delegate.CreateDelegate(typeof(Action<UnityEngine.UI.VertexHelper>), bunting, method);
                using var mesh = new UnityEngine.UI.VertexHelper();
                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion = true;
                ((RectTransform)root.transform).sizeDelta = new Vector2(1600, 160); draw(mesh);
                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion = false;
                foreach (var variant in new[] { new Vector2Int(720, 47), new Vector2Int(1600, 48),
                    new Vector2Int(2560, 47), new Vector2Int(1600, 47), new Vector2Int(0, 47) })
                {
                    bunting.Seed = variant.y;
                    ((RectTransform)root.transform).sizeDelta = new Vector2(variant.x, 160); draw(mesh);
                    var observed = new List<UIVertex>(); mesh.GetUIVertexStream(observed);
                    var freshRoot = new GameObject("Fresh comparison bunting", typeof(RectTransform));
                    try
                    {
                        ((RectTransform)freshRoot.transform).sizeDelta = new Vector2(variant.x, 160);
                        var fresh = freshRoot.AddComponent<HubBunting>(); fresh.Seed = variant.y;
                        var drawFresh = (Action<UnityEngine.UI.VertexHelper>)Delegate.CreateDelegate(typeof(Action<UnityEngine.UI.VertexHelper>), fresh, method);
                        using var freshMesh = new UnityEngine.UI.VertexHelper(); drawFresh(freshMesh);
                        var expected = new List<UIVertex>(); freshMesh.GetUIVertexStream(expected);
                        Assert.AreEqual(expected.Count, observed.Count);
                        for (int i = 0; i < expected.Count; i++)
                        {
                            Assert.AreEqual(expected[i].position, observed[i].position);
                            Assert.AreEqual(expected[i].color, observed[i].color);
                        }
                    }
                    finally { UnityEngine.Object.DestroyImmediate(freshRoot); }
                }
            }
            finally
            {
                TumbangPreso.Settings.SettingsStore.Current.ReducedUiMotion = reduced;
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

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
                CalibrateAllocationRecorder();
                using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 4096,
                    ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
                for (int i = 0; i < 500; i++) build(points,new Rect(-200,-70,400+i%3,140),18,5);
                recorder.Stop();
                Assert.AreEqual(0, recorder.Count, "Repeated contour rebuilds still allocate per-frame noise state.");
                build(points,rect,18,5); CollectionAssert.AreEqual(first,points);
                shape.Seed = 48; build(points,rect,18,5); CollectionAssert.AreNotEqual(first,points);
                shape.Seed = 47; build(points,rect,18,5); CollectionAssert.AreEqual(first,points);
                Debug.Log("[UIContourAllocation] pressable="+pressable+" rebuilds=500 allocationEvents="+recorder.Count);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
