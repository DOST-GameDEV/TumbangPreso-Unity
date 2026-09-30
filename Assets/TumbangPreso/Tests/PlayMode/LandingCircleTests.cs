using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class LandingCircleTests
    {
        readonly List<GameObject> _built = new List<GameObject>();
        static readonly MethodInfo Flight = typeof(Slipper).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly MethodInfo Ground = typeof(Slipper).GetMethod("FindGroundY", BindingFlags.Static | BindingFlags.NonPublic);
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [TearDown] public void Cleanup() { foreach (var go in _built) if (go != null) Object.DestroyImmediate(go); _built.Clear(); }
        GameObject Track(GameObject go) { _built.Add(go); return go; }
        GameObject Slab(string name, Vector3 at, Vector3 scale)
        {
            var go = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); go.name = name;
            go.transform.position = at; go.transform.localScale = scale; Physics.SyncTransforms(); return go;
        }
        void Equivalent(float ground, float spin, bool wall)
        {
            Slab("FloorTest", new Vector3(0, ground - .5f, 0), new Vector3(30, 1, 30));
            if (wall) Slab("BankWall", new Vector3(0, ground + 2, 2), new Vector3(8, 4, .4f));
            var guide = Track(new GameObject("LandingPreview")).AddComponent<TrajectoryPreview>();
            guide.enabled = false;
            var shoe = Track(new GameObject("FlightComparison")).AddComponent<Slipper>(); shoe.enabled = false;
            Vector3 origin = new Vector3(0, ground + 1, -2), velocity = new Vector3(0, 3, 7);
            Assert.IsTrue(guide.TryPredictLanding(origin, velocity, spin, out var landing));
            using (NetCue.SuppressRelay())
            {
                shoe.HostThrow(null, origin, velocity, SlipperAffinity.Normal, spin);
                for (int i = 0; i < 320 && shoe.State == SlipperState.InFlight; i++) Flight.Invoke(shoe, null);
            }
            Assert.AreEqual(SlipperState.Loose, shoe.State);
            var actual = shoe.transform.position; actual.y = landing.y;
            Assert.Less(Vector3.Distance(actual, landing), .12f, "Preview and actual host flight must agree before actor interception.");
            Assert.That(landing.y, Is.EqualTo(ground + TrajectoryPreview.FloorEpsilon).Within(.001f));
        }

        [Test] public void FlatFlightMatchesTheGroundCircle() => Equivalent(0, 0, false);
        [Test] public void RaisedGroundAndCurveMatchRealFlight() => Equivalent(.6f, .65f, false);
        [Test] public void BankShotMatchesTheGroundCircle() => Equivalent(0, .9f, true);

        [Test] public void SupportQueryDoesNotAllocateAfterWarmupAndDenseOverflowKeepsHighestFloor()
        {
            Slab("Support", new Vector3(0, -.5f, 0), new Vector3(20, 1, 20));
            var query = (System.Func<Vector3, float, float>)System.Delegate.CreateDelegate(typeof(System.Func<Vector3, float, float>), Ground);
            for (int i = 0; i < 5; i++) query(Vector3.up, .045f);
            using (var calibrationRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 100, ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            {
                Assert.IsTrue(calibrationRecorder.Valid);
                var calibration = new byte[4096]; System.GC.KeepAlive(calibration); calibrationRecorder.Stop();
                Assert.Greater(calibrationRecorder.Count, 0);
            }
            float total = 0; int allocations;
            using (var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 256, ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            {
                for (int i = 0; i < 100; i++) total += query(Vector3.up, .045f);
                recorder.Stop(); allocations = recorder.Count;
            }
            Assert.AreEqual(0, total, .001f); Assert.AreEqual(0, allocations, "Normal support queries should reuse their hit storage.");
            for (int i = 0; i < 70; i++) Slab("DenseSupport" + i, new Vector3(0, .1f + i * .02f, 0), new Vector3(2, .01f, 2));
            Assert.That(query(Vector3.up * 3, .045f), Is.EqualTo(1.485f).Within(.001f), "A full nonalloc buffer must not silently lose the highest support.");
        }

        [UnityTest, Timeout(60000)] public IEnumerator TheRealLocalChargeDrawsOnlyAGroundCircleAndReleaseHidesIt()
        {
            yield return MapRetrievalProbe.Load(UI.SceneFlow.Eskinita);
            var ready = Object.FindFirstObjectByType<ReadyGate>();
            ready.enabled = true; ready.StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            Assert.IsFalse(ready.AwaitingReady || ready.CountingDown);
            var who = GameServices.Round.PlayerAt(1); var carrier = who.GetComponent<Carrier>();
            var rig = Object.FindFirstObjectByType<CameraSystem.CameraRig>();
            foreach (var brain in Object.FindObjectsByType<AIController>()) brain.enabled = false;
            who.Teleport(new Vector3(-3, .12f, -8)); who.Intent.Clear(); who.Intent.Parked = false;
            who.transform.rotation = Quaternion.identity;
            who.Intent.AimPoint = new Vector3(-3, .12f, 0);
            rig.Follow(who); rig.SetAimSource(CameraSystem.AimSource.Movement);
            yield return null;
            var direction = who.Intent.AimPoint - rig.transform.position;
            typeof(CameraSystem.CameraRig).GetField("_pitchDeg", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(rig, Mathf.Atan2(-direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg);
            yield return null;
            carrier.enabled = false; who.Intent.Set(Verb.SpecialAbility, true);
            Assert.IsTrue(GameServices.Round.CanThrow(who), "The visual check needs a legal throw outside the defender box.");
            var step = typeof(Carrier).GetMethod("StepAttacker", BindingFlags.Instance | BindingFlags.NonPublic);
            step.Invoke(carrier, new object[] { 0f }); step.Invoke(carrier, new object[] { Balance.ChargeFullTime });
            Assert.IsTrue(carrier.IsCharging);
            var guide = GameObject.Find("~AimArc1").GetComponent<TrajectoryPreview>();
            typeof(TrajectoryPreview).GetMethod("Rebuild", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(guide, null);
            Assert.IsTrue(guide.LandingVisible);
            Debug.Log("Landing review point=" + guide.LandingPoint + " viewport=" + rig.Camera.WorldToViewportPoint(guide.LandingPoint)
                + " camera=" + rig.Camera.transform.position + " facing=" + rig.Camera.transform.forward + " rigLocal=" + rig.IsLocalFpp);
            foreach (var vertex in guide.GetComponent<MeshFilter>().sharedMesh.vertices)
            {
                Assert.That(vertex.y, Is.EqualTo(guide.LandingPoint.y).Within(.001));
                Assert.Less(Vector3.Distance(vertex, guide.LandingPoint), .6f, "The old flight line must not remain in the mesh.");
            }
            System.IO.Directory.CreateDirectory("Logs/feedback-0930/landing-circle");
            typeof(ThrowAimIntegrationProbe).GetMethod("CaptureGuideDifference", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { rig.Camera, guide, "Logs/feedback-0930/landing-circle", "landing-visible" });
            yield return GameplayShots.Render(rig.Camera, "Landing-circle-charge", true, "Logs/feedback-0930/landing-circle");
            Assert.IsTrue(guide.LandingVisible, "Actual render frames must retain the local landing circle. charged=" + carrier.IsCharging
                + " held=" + (carrier.Held != null) + " round=" + GameServices.Round.RoundActive + " brain=" + (who.GetComponent<AIController>()?.enabled == true)
                + " main=" + (Camera.main == rig.Camera) + " local=" + rig.IsLocalFpp + " follows=" + rig.IsFollowing(who));
            who.Intent.Set(Verb.SpecialAbility, false); step.Invoke(carrier, new object[] { .01f });
            guide.SendMessage("LateUpdate"); Assert.IsFalse(guide.LandingVisible);
        }
    }
}
