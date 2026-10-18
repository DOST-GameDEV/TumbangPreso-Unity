using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class PausedRoadSolidTests
    {
        private bool _autoSync;
        [UnitySetUp] public IEnumerator Before()
        { yield return PlayModeWorld.Reset(); _autoSync = Physics.autoSyncTransforms; Physics.autoSyncTransforms = false; }
        [UnityTearDown] public IEnumerator After()
        {
            typeof(PresentationClock).GetMethod("Release", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            Physics.autoSyncTransforms = _autoSync;
            yield return PlayModeWorld.Reset();
        }
        [UnityTest] public IEnumerator InitialVehicleSolidIsQueryableDuringTheHold() => Check(false);
        [UnityTest] public IEnumerator MovedVehicleSolidIsQueryableDuringTheHold() => Check(true);
        private IEnumerator Check(bool move)
        {
            var root = new GameObject("Paused road fixture");
            var traffic = root.AddComponent<KantoTraffic>(); traffic.enabled = false; traffic.HitsPlayers = true;
            var body = new GameObject("Authored vehicle"); body.transform.SetParent(root.transform);
            body.transform.position = new Vector3(50, 0, 25);
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube); mesh.transform.SetParent(body.transform, false);
            mesh.transform.localPosition = Vector3.up * .5f; mesh.transform.localScale = new Vector3(2, 1, 4);
            traffic.Routes = new[] { new KantoTraffic.Route { Points = new[] { new Vector3(50,0,0), new Vector3(50,0,100) } } };
            traffic.Drivers = new[] { new KantoTraffic.Driver { Body = body.transform, Lane = 0, Along = 25 } };
            Physics.SyncTransforms();
            typeof(PresentationClock).GetMethod("Hold", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            var hidden = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(KantoTraffic).GetMethod("Start", hidden).Invoke(traffic, null);
            float oldX = 0, newX = 50;
            if (move)
            {
                Physics.SyncTransforms(); oldX = 50; newX = 70;
                var at = (Vector3[])typeof(KantoTraffic).GetField("_poseAt", hidden).GetValue(traffic);
                at[0] = new Vector3(70, 0, 25); body.transform.position = at[0];
                typeof(KantoTraffic).GetMethod("UpdateSolids", hidden).Invoke(traffic, null);
            }
            float oldZ = move ? 19 : -6;
            bool phantom = Physics.Raycast(new Vector3(oldX,.5f,oldZ), Vector3.forward, 10, ~0, QueryTriggerInteraction.Ignore);
            bool actual = Physics.Raycast(new Vector3(newX,.5f,19), Vector3.forward, 10, ~0, QueryTriggerInteraction.Ignore);
            Assert.IsTrue(!phantom && actual, "Paused queries must match the visible vehicle immediately: stale=" + phantom + ", current=" + actual);
            Assert.IsTrue(PresentationClock.Held); Assert.AreEqual(0, Time.timeScale);
            yield return null;
        }
    }
}
