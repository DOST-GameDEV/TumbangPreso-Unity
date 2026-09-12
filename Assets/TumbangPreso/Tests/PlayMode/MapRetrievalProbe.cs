using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class MapRetrievalProbe
    {
        private bool _bots, _spectator, _pinned;
        private int _seat;
        private CustomRules _rules;
        private static string Output => Environment.GetEnvironmentVariable("TUMP_MAP_RETRIEVAL") ?? "Logs/map-retrieval";

        [UnitySetUp] public IEnumerator Before()
        {
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            _pinned = SceneFlow.RulesPinned; _rules = SceneFlow.SelectedRules.Clone();
            yield return PlayModeWorld.Reset();
        }

        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        internal static IEnumerator Load(string map, GameMode mode = GameMode.Classic)
        {
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(mode));
            GameLaunch.AllBots = false; GameLaunch.Spectator = false; GameLaunch.SoloSeat = 1;
            yield return SceneManager.LoadSceneAsync(map);
            yield return new WaitForSeconds(.3f);
            Object.FindFirstObjectByType<SliceRunner>().Begin();
            yield return new WaitForSeconds(.3f);
            // Disable every live input writer, including the solo seat switcher and
            // ready countdown, before manipulating a throw. Keep real flight/carry active.
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) reader.enabled = false;
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>(FindObjectsSortMode.None)) switcher.enabled = false;
            foreach (var ready in Object.FindObjectsByType<ReadyGate>(FindObjectsSortMode.None)) ready.enabled = false;
            foreach (var player in GameServices.Round.Players)
            {
                player.IsBot = true;
                player.Intent.Clear(); player.Intent.CommitFrame(); player.Intent.Parked = true;
                typeof(Carrier).GetMethod("CancelAll", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).Invoke(player.GetComponent<Carrier>(), null);
            }
            yield return null;
            Assert.IsFalse(Object.FindObjectsByType<AIController>(FindObjectsSortMode.None).Any(b => b.enabled));
            Assert.IsFalse(Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None).Any(r => r.enabled));
        }

        [UnityTest]
        public IEnumerator DirectThrowDetachesThePreviousCarriersReference()
        {
            yield return Load(SceneFlow.IlalimNgTulay);
            var owner = GameServices.Round.PlayerAt(1);
            var carrier = owner.GetComponent<Carrier>();
            var shoe = carrier.Held;
            Assert.IsNotNull(shoe);
            var origin = new Vector3(2, 1.8f, -2);
            shoe.HostThrow(owner, origin, Vector3.right * 2);
            Assert.IsNull(carrier.Held, "The carry loop still owns the directly released slipper.");
            Assert.IsFalse(owner.HoldingSlipper);
            Assert.IsNull(shoe.Holder);
            yield return null;
            Assert.Greater(shoe.transform.position.y, 1.5f, "LateUpdate pulled the flight back into the hand.");
        }

        [UnityTest]
        public IEnumerator CarrierThrowBelowTheViaductContinuesItsActualFlight()
        {
            yield return Load(SceneFlow.IlalimNgTulay);
            var owner = GameServices.Round.PlayerAt(1);
            var carrier = owner.GetComponent<Carrier>();
            var shoe = carrier.Held;
            Assert.IsNotNull(shoe);
            Vector3 origin = new Vector3(2, 3.6f, -2);
            Assert.IsTrue(Physics.Raycast(origin, Vector3.down, out var floor, 4, ~0, QueryTriggerInteraction.Ignore));
            Assert.AreEqual(0, floor.point.y, .02f);
            carrier.HostThrowAt(origin, new Vector3(6, 3.6f, -2), .5f);
            Assert.IsNull(carrier.Held, "The real carrier throw must release ownership before flight is measured.");
            Assert.Less(Vector3.Distance(origin, shoe.transform.position), .001f);
            var trace = new StringBuilder("fixed_time,state,x,y,z,vx,vy,vz,carrier_held,ground_query\n");
            try
            {
                for (int step = 0; step < 5; step++)
                {
                    yield return new WaitForFixedUpdate();
                    Vector3 p = shoe.transform.position, v = shoe.Velocity;
                    trace.AppendLine(FormattableString.Invariant($"{Time.fixedTime:F4},{shoe.State},{p.x:F4},{p.y:F4},{p.z:F4},{v.x:F4},{v.y:F4},{v.z:F4},{carrier.Held != null},{Slipper.GroundY(p):F4}"));
                    Assert.AreEqual(SlipperState.InFlight, shoe.State, "The overhead deck ended a throw below it.");
                    Assert.Greater(p.y, 3, "The in-flight slipper was recovered to its owner's mark.");
                }
                Assert.Greater(shoe.transform.position.x, origin.x + .1f);
            }
            finally
            {
                Directory.CreateDirectory(Output);
                File.WriteAllText(Path.Combine(Output, "carrier-under-viaduct.csv"), trace.ToString());
            }
        }

        [UnityTest]
        public IEnumerator FastDescentStillLandsOnRaisedSupportBelowAnOverhang()
        {
            yield return Load(SceneFlow.IlalimNgTulay);
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "RetrievalRaisedSupport";
            slab.transform.position = new Vector3(2, .5f, -2);
            slab.transform.localScale = new Vector3(3, 1, 3);
            Physics.SyncTransforms();
            var owner = GameServices.Round.PlayerAt(1);
            var shoe = owner.GetComponent<Carrier>().Held;
            shoe.HostThrow(owner, new Vector3(2, 1.2f, -2), Vector3.down * 25);
            for (int i = 0; i < 10 && shoe.State == SlipperState.InFlight; i++)
                yield return new WaitForFixedUpdate();
            Assert.AreEqual(SlipperState.Loose, shoe.State);
            Assert.AreEqual(2, shoe.transform.position.x, .01f);
            Assert.AreEqual(-2, shoe.transform.position.z, .01f);
            Assert.AreEqual(1 + shoe.RestHeight, shoe.transform.position.y, .01f,
                "The descending step skipped a reachable raised surface or selected the roof.");
        }

        [UnityTest]
        public IEnumerator LandingOnAnUnreachableRoofStillRecoversToTheOwner()
        {
            yield return Load(SceneFlow.Eskinita);
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "RetrievalUnreachableRoof";
            roof.transform.position = new Vector3(2, 2.8f, -2);
            roof.transform.localScale = new Vector3(3, .4f, 3);
            Physics.SyncTransforms();
            var owner = GameServices.Round.PlayerAt(1);
            var shoe = owner.GetComponent<Carrier>().Held;
            shoe.HostThrow(owner, new Vector3(2, 3.2f, -2), Vector3.down * 5);
            for (int i = 0; i < 30 && shoe.State == SlipperState.InFlight; i++)
                yield return new WaitForFixedUpdate();
            Assert.AreEqual(SlipperState.Loose, shoe.State);
            Assert.Less(Vector3.Distance(owner.transform.position, shoe.transform.position), .3f,
                "A correctly detected roof landing stranded the slipper above pickup reach.");
            Assert.IsTrue(shoe.CanBeGrabbedBy(owner));
        }
    }
}
