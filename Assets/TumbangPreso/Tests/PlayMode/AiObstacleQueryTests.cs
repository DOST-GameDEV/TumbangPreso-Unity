using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class AiObstacleQueryTests
    {
        readonly List<GameObject> _built = new List<GameObject>();
        System.Func<Vector3, float, float, bool> _clear;
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            var go = Track(new GameObject("Obstacle query bot")); go.transform.position = Vector3.left * 5;
            go.AddComponent<CharacterMotor>().enabled = false;
            var brain = go.AddComponent<AIController>(); brain.enabled = false;
            _clear = (System.Func<Vector3, float, float, bool>)System.Delegate.CreateDelegate(
                typeof(System.Func<Vector3, float, float, bool>), brain,
                typeof(AIController).GetMethod("ShoveRouteIsClear", BindingFlags.Instance | BindingFlags.NonPublic));
        }
        [UnityTearDown] public IEnumerator After()
        {
            foreach (var go in _built) if (go != null) Object.DestroyImmediate(go); _built.Clear();
            yield return PlayModeWorld.Reset();
        }
        GameObject Track(GameObject go) { _built.Add(go); return go; }
        GameObject Box(float z, bool body = false, bool trigger = false)
        {
            var go = Track(new GameObject("Route object", typeof(BoxCollider)));
            go.transform.position = new Vector3(0, 2, z);
            var box = go.GetComponent<BoxCollider>(); box.size = new Vector3(1, 4, .05f); box.isTrigger = trigger;
            if (body) go.AddComponent<CharacterMotor>().enabled = false;
            Physics.SyncTransforms(); return go;
        }

        [Test] public void BodiesAndTriggersAreIgnoredButRealWallsBlock()
        {
            Assert.IsTrue(_clear(Vector3.zero, 0, 6));
            Box(1, body: true); Box(2, trigger: true);
            Assert.IsTrue(_clear(Vector3.zero, 0, 6));
            Box(4); Assert.IsFalse(_clear(Vector3.zero, 0, 6));
            Assert.IsTrue(_clear(Vector3.zero, 0, 0), "A zero-length route has no obstruction.");
        }

        [Test] public void WarmOrdinaryQueriesAllocateNoManagedHitArrays()
        {
            Box(2, body: true);
            for (int i = 0; i < 5; i++) _clear(Vector3.zero, 0, 6);
            using (var calibrationRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 100, ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            {
                Assert.IsTrue(calibrationRecorder.Valid);
                var calibration = new byte[4096]; System.GC.KeepAlive(calibration);
                calibrationRecorder.Stop();
                Assert.Greater(calibrationRecorder.Count, 0, "The native recorder must detect the deliberate allocation.");
            }
            bool clear = true; int allocations;
            using (var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 256, ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            {
                for (int i = 0; i < 100; i++) clear &= _clear(Vector3.zero, 0, 6);
                recorder.Stop(); allocations = recorder.Count;
            }
            Assert.IsTrue(clear); Assert.AreEqual(0, allocations);
        }

        [Test] public void DenseIgnoredBodiesDoNotHideTheWallBeyondTheHitBuffer()
        {
            for (int i = 0; i < 40; i++) Box(.2f + i * .1f, body: true);
            Assert.IsTrue(_clear(Vector3.zero, 0, 6));
            Box(5); Assert.IsFalse(_clear(Vector3.zero, 0, 6), "Overflow must use complete collision results.");
        }
    }
}
