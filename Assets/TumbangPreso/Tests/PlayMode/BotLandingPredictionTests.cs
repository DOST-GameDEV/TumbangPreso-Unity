using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;
using System;
using Unity.Profiling;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class BotLandingPredictionTests
    {
        private delegate bool Landing(Slipper shoe, out Vector3 point);
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [Test] public void AThrowFromRaisedTerrainPredictsTheActualLowerFloorLanding()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "FloorPredictionTest";
            floor.transform.position = Vector3.down * .5f;
            floor.transform.localScale = new Vector3(30, 1, 30);
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "RaisedPredictionPlatform";
            platform.transform.position = new Vector3(0, 4.5f, -4);
            platform.transform.localScale = new Vector3(4, 1, 2);
            Physics.SyncTransforms();
            var shoe = new GameObject("Bot landing flight").AddComponent<Slipper>(); shoe.enabled = false;
            try
            {
                using (NetCue.SuppressRelay())
                {
                    shoe.HostThrow(null, new Vector3(0, 6, -2), new Vector3(0, 3, 7));
                    var args = new object[] { shoe, Vector3.zero };
                    Assert.IsTrue((bool)typeof(AIController).GetMethod("TryPredictedLanding", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args));
                    var predicted = (Vector3)args[1];
                    var step = typeof(Slipper).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
                    for (int i = 0; i < 320 && shoe.State == SlipperState.InFlight; i++) step.Invoke(shoe, null);
                    Assert.AreEqual(SlipperState.Loose, shoe.State);
                    var actual = shoe.transform.position;
                    Assert.That(actual.y, Is.EqualTo(Balance.SlipperRestHeight).Within(.001f));
                    TestContext.WriteLine("Predicted=" + predicted + " actual=" + actual);
                    Assert.Less(Vector2.Distance(new Vector2(predicted.x, predicted.z), new Vector2(actual.x, actual.z)), .12f,
                        "A bot heads to the early flight height instead of the actual lower-floor landing.");
                }
            }
            finally
            {
                Object.DestroyImmediate(shoe.gameObject);
                Object.DestroyImmediate(platform); Object.DestroyImmediate(floor);
            }
        }
        [TestCase(0f, 0f, false)]
        [TestCase(.6f, .65f, false)]
        [TestCase(0f, .9f, true)]
        public void BotPredictionRetainsFlatCurvedAndBankedWorldFlight(float ground, float spin, bool bank)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "FloorBotComparison";
            floor.transform.position = new Vector3(0, ground - .5f, 0); floor.transform.localScale = new Vector3(30, 1, 30);
            GameObject wall = null;
            if (bank)
            {
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "BankBotComparison";
                wall.transform.position = new Vector3(0, ground + 2, 2); wall.transform.localScale = new Vector3(8, 4, .4f);
            }
            Physics.SyncTransforms();
            var shoe = new GameObject("Bot flight comparison").AddComponent<Slipper>(); shoe.enabled = false;
            try
            {
                using (NetCue.SuppressRelay())
                {
                    shoe.HostThrow(null, new Vector3(0, ground + 1, -2), new Vector3(0, 3, 7), SlipperAffinity.Normal, spin);
                    var args = new object[] { shoe, Vector3.zero };
                    Assert.IsTrue((bool)typeof(AIController).GetMethod("TryPredictedLanding", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args));
                    var predicted = (Vector3)args[1];
                    var step = typeof(Slipper).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
                    for (int i = 0; i < 320 && shoe.State == SlipperState.InFlight; i++) step.Invoke(shoe, null);
                    Assert.AreEqual(SlipperState.Loose, shoe.State);
                    var actual = shoe.transform.position; actual.y = predicted.y;
                    Assert.Less(Vector3.Distance(actual, predicted), .12f);
                    Assert.That(predicted.y, Is.EqualTo(ground + TrajectoryPreview.FloorEpsilon).Within(.001f));
                }
            }
            finally { Object.DestroyImmediate(shoe.gameObject); if (wall != null) Object.DestroyImmediate(wall); Object.DestroyImmediate(floor); }
        }

        [Test] public void RetrievalPredictionRefreshesAtThinkCadenceAndRetiresOldSources()
        {
            var root = new GameObject("Cached landing reader"); var motor = root.AddComponent<CharacterMotor>(); motor.enabled = false;
            var brain = root.AddComponent<AIController>(); brain.enabled = false; brain.SeatDifficulty = Difficulty.Normal;
            var query = (Landing)Delegate.CreateDelegate(typeof(Landing), brain,
                typeof(AIController).GetMethod("TryPositionLanding", BindingFlags.Instance | BindingFlags.NonPublic));
            var shoe = new GameObject("Cached landing shoe").AddComponent<Slipper>(); shoe.enabled = false;
            var other = new GameObject("Replacement landing shoe").AddComponent<Slipper>(); other.enabled = false;
            try
            {
                using (NetCue.SuppressRelay())
                {
                    shoe.HostThrow(null, new Vector3(0, 1, -2), new Vector3(0, 3, 7));
                    Assert.IsTrue(query(shoe, out var first));
                    shoe.transform.position += Vector3.right * 2;
                    Assert.IsTrue(query(shoe, out var cached)); Assert.AreEqual(first, cached);
                    typeof(AIController).GetField("_landingRefreshAt", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(brain, Time.time - 1);
                    Assert.IsTrue(query(shoe, out var refreshed)); Assert.Greater(refreshed.x, first.x + 1);
                    other.HostThrow(null, new Vector3(-2, 1, -2), new Vector3(0, 3, 7));
                    Assert.IsTrue(query(other, out var replaced)); Assert.Less(replaced.x, refreshed.x - 1);
                    other.ApplySnapshotState(SlipperState.Loose, null, other.transform.position, Quaternion.identity, Vector3.zero, 0, SlipperAffinity.Normal, 0);
                    Assert.IsFalse(query(other, out _));
                    Assert.IsFalse(query(null, out _));
                    other.HostThrow(null, new Vector3(1, 1, -2), new Vector3(0, 3, 7));
                    Assert.IsTrue(query(other, out var relaunched)); Assert.Greater(relaunched.x, replaced.x + 1);
                    using (var calibration = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 100, ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
                    { Assert.IsTrue(calibration.Valid); GC.KeepAlive(new byte[4096]); calibration.Stop(); Assert.Greater(calibration.Count, 0); }
                    int allocations;
                    using (var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 256, ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
                    { for (int i = 0; i < 100; i++) query(other, out _); recorder.Stop(); allocations = recorder.Count; }
                    Assert.AreEqual(0, allocations, "Warmed retrieval reads must not allocate.");
                }
            }
            finally { Object.DestroyImmediate(other.gameObject); Object.DestroyImmediate(shoe.gameObject); Object.DestroyImmediate(root); }
        }

        [Test] public void ALoadedSkimPredictionIncludesItsRealGroundContinuation()
            => CompareSkim(false, false);

        [Test] public void ASkimmingSlipperPredictsOnlyItsRemainingContinuation()
            => CompareSkim(true, false);

        [Test] public void SkimPredictionStopsAtTheSameSolidWorldCover()
            => CompareSkim(false, true);

        private void CompareSkim(bool afterFirstGround, bool withWall)
        {
            GameServices.Ensure(); GameServices.Round.Clear();
            GameServices.Match.ApplySnapshot(new int[4], 2, true);
            GameServices.Round.ApplySnapshot(100, true, 1, true);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "FloorSkimPrediction";
            floor.transform.position = Vector3.down * .5f; floor.transform.localScale = new Vector3(30, 1, 30);
            GameObject wall = null;
            if (withWall)
            {
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "Skim prediction cover";
                wall.transform.position = new Vector3(0, .5f, 0); wall.transform.localScale = new Vector3(4, 1, .2f);
            }
            Physics.SyncTransforms();
            var shoe = new GameObject("Skim prediction flight").AddComponent<Slipper>(); shoe.enabled = false;
            try
            {
                using (NetCue.SuppressRelay())
                {
                    shoe.HostThrow(null, new Vector3(0, .3f, -2), new Vector3(0, -1, 5), SlipperAffinity.Skim);
                    var step = typeof(Slipper).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (afterFirstGround)
                    {
                        for (int i = 0; i < 60 && !shoe.IsSkimming && shoe.State == SlipperState.InFlight; i++) step.Invoke(shoe, null);
                        Assert.IsTrue(shoe.IsSkimming);
                        step.Invoke(shoe, null);
                    }
                    var args = new object[] { shoe, Vector3.zero };
                    Assert.IsTrue((bool)typeof(AIController).GetMethod("TryPredictedLanding", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args));
                    var predicted = (Vector3)args[1];
                    for (int i = 0; i < 320 && shoe.State == SlipperState.InFlight; i++) step.Invoke(shoe, null);
                    Assert.AreEqual(SlipperState.Loose, shoe.State);
                    var actual = shoe.transform.position; actual.y = predicted.y;
                    TestContext.WriteLine("Skim predicted=" + predicted + " actual=" + actual);
                    Assert.Less(Vector3.Distance(actual, predicted), .12f, "Prediction stopped at first ground contact instead of the complete Skim.");
                }
            }
            finally { Object.DestroyImmediate(shoe.gameObject); if (wall != null) Object.DestroyImmediate(wall); Object.DestroyImmediate(floor); }
        }
    }
}
