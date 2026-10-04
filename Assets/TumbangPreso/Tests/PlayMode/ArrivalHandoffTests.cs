using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.UI.Hub;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class ArrivalHandoffTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<GameObject> _owned = new List<GameObject>();
        private GameObject Make(string name) { var go = new GameObject(name); _owned.Add(go); return go; }
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
        private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
        [UnitySetUp] public IEnumerator Before() { yield return PlayModeWorld.Reset(); }
        [UnityTearDown] public IEnumerator After()
        {
            HubLoading.Cancel();
            foreach (var go in _owned) if (go != null) Object.DestroyImmediate(go);
            _owned.Clear(); yield return PlayModeWorld.Reset();
        }
        private Camera Camera()
        {
            var camera = Make("Arrival camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.transform.SetPositionAndRotation(new Vector3(0, -8, 0), Quaternion.Euler(90, 180, 0)); return camera;
        }
        [UnityTest] public IEnumerator EntryIsOpaqueBeforeTheFirstYieldAndCancellationClearsIt()
        {
            Camera(); var arrival = Make("Arrival").AddComponent<MatchArrivalPresentation>();
            var run = arrival.Run(); Assert.IsTrue(run.MoveNext());
            Assert.IsTrue(MatchArrivalPresentation.Active);
            Assert.AreEqual(1, Get<Image>(arrival, "_ink").color.a);
            Assert.AreEqual(0, Get<CanvasGroup>(arrival, "_captionGroup").alpha);
            arrival.Cancel(); (run as System.IDisposable)?.Dispose(); yield return null;
            Assert.IsFalse(MatchArrivalPresentation.Active); Assert.IsFalse(PresentationClock.Held);
            Assert.IsNull(GameObject.Find("MatchArrivalCanvas"));
        }
        [UnityTest] public IEnumerator OpeningTakesCameraBeforeLoadingCurtainDisappears()
        {
            var camera = Camera();
            var loading = Make("Controlled loading").AddComponent<HubLoading>();
            typeof(HubLoading).GetField("_current", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, loading);
            var arrival = Make("Arrival").AddComponent<MatchArrivalPresentation>();
            var run = arrival.Run(); Assert.IsTrue(run.MoveNext()); Assert.IsTrue(run.MoveNext());
            Assert.IsTrue(HubLoading.Preparing); Assert.AreEqual(-8, camera.transform.position.y);
            Assert.AreEqual(1, Get<Image>(arrival, "_ink").color.a);
            Set(loading, "_revealing", true);
            Assert.IsTrue(run.MoveNext());
            Assert.IsTrue(HubLoading.Visible); Assert.IsFalse(HubLoading.Preparing);
            Assert.Greater(camera.transform.position.y, 0, "Opening shot must already exist while loading remains visible.");
            Assert.AreEqual(1, Get<Image>(arrival, "_ink").color.a);
            HubLoading.Cancel(); arrival.Cancel(); (run as System.IDisposable)?.Dispose(); yield return null;
            Assert.IsFalse(PresentationClock.Held);
        }
        [UnityTest] public IEnumerator HeldRigResolvesPlayerEyeInsteadOfInheritedUnderMapPose()
        {
            var camera = Camera(); var motor = Make("Player above water").AddComponent<CharacterMotor>();
            motor.transform.SetPositionAndRotation(new Vector3(8, 6, 3), Quaternion.Euler(0, 47, 0));
            var rig = camera.gameObject.AddComponent<CameraRig>(); Set(rig, "_character", motor); Set(rig, "_active", true);
            var arrival = Make("Arrival").AddComponent<MatchArrivalPresentation>();
            var run = arrival.Run(); Assert.IsTrue(run.MoveNext()); Assert.IsTrue(run.MoveNext());
            Assert.IsTrue(PresentationClock.Held);
            Vector3 expected = motor.transform.position + Vector3.up * (CameraRig.PersonCapsuleHeight * .5f + CameraRig.FppEyeHeight);
            Assert.Less(Vector3.Distance(Get<Vector3>(arrival, "_position"), expected), .001f);
            Assert.Less(Quaternion.Angle(Get<Quaternion>(arrival, "_rotation"), Quaternion.Euler(0, 47, 0)), .01f);
            arrival.Cancel(); (run as System.IDisposable)?.Dispose();
            Assert.Less(Vector3.Distance(camera.transform.position, expected), .001f); yield return null;
        }
        [UnityTest] public IEnumerator NameplatesHideDuringIntroductionAndReturnAfterCancellation()
        {
            Camera(); var motor = Make("Other player").AddComponent<CharacterMotor>();
            var plate = motor.gameObject.AddComponent<CharacterNameplate>(); yield return null;
            var label = Get<TextMesh>(plate, "_label"); Assert.IsNotNull(label);
            var arrival = Make("Arrival").AddComponent<MatchArrivalPresentation>();
            var run = arrival.Run(); Assert.IsTrue(run.MoveNext());
            typeof(CharacterNameplate).GetMethod("LateUpdate", Private).Invoke(plate, null);
            Assert.IsFalse(label.gameObject.activeSelf);
            arrival.Cancel(); (run as System.IDisposable)?.Dispose();
            typeof(CharacterNameplate).GetMethod("LateUpdate", Private).Invoke(plate, null);
            Assert.IsTrue(label.gameObject.activeSelf); yield return null;
        }
    }
}
