using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ExchangeMotionTests
    {
        private INetProvider _provider;
        private int _quality;
        private float _flash;
        private sealed class Replica : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }

        [UnitySetUp] public IEnumerator Before()
        {
            _provider = NetAuthority.Provider;
            _quality = SettingsStore.Current.GraphicsQuality;
            _flash = SettingsStore.Current.FlashIntensity;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            NetAuthority.Provider = _provider;
            SettingsStore.Current.GraphicsQuality = _quality;
            SettingsStore.Current.FlashIntensity = _flash;
            yield return PlayModeWorld.Reset();
        }

        [UnityTest] public IEnumerator ClassicFlightAndPossessionKeepOneTruthfulStroke() => Flight(GameMode.Classic);
        [UnityTest] public IEnumerator HeroFlightAndPossessionKeepOneTruthfulStroke() => Flight(GameMode.HeroStrike);
        private static IEnumerator Flight(GameMode mode)
        {
            yield return MapRetrievalProbe.Load("Eskinita", mode);
            var owner = GameServices.Round.PlayerAt(1);
            var shoe = Object.FindObjectsByType<Slipper>().First(s => s.OwnerSlot == 1);
            Assert.IsTrue(shoe.HostForceEquip(owner));
            var accent = shoe.GetComponent<SlipperMotionAccent>();
            Assert.IsNotNull(accent); Assert.IsFalse(accent.Emitting);
            shoe.HostThrow(owner, new Vector3(0, 2, -5), Vector3.forward * 8);
            yield return null; yield return null;
            Assert.AreEqual(SlipperState.InFlight, shoe.State);
            Assert.IsTrue(accent.Emitting);
            Assert.AreEqual(1, shoe.GetComponentsInChildren<TrailRenderer>().Length,
                "A normal Hero throw must not retain the previous second trail.");
            Assert.IsTrue(shoe.HostForceEquip(owner));
            Assert.IsFalse(accent.Emitting, "Accepted possession clears flight before the next render.");
            Assert.IsFalse(shoe.GetComponentInChildren<TrailRenderer>().enabled);
            yield return null; yield return null;
            Assert.AreEqual(0, shoe.GetComponentInChildren<TrailRenderer>().positionCount);

            // Exercise the existing replica state entry, without pretending this
            // provider seam is a real transport test or invoking a host release.
            NetAuthority.Provider = new Replica();
            void Snapshot(SlipperState state, Vector3 at) => shoe.ApplySnapshotState(state,
                state == SlipperState.Held ? owner : null, at, Quaternion.identity,
                Vector3.forward * 8, 0, SlipperAffinity.Normal, 1);
            Snapshot(SlipperState.InFlight, new Vector3(0, 2, -4));
            yield return null; yield return null;
            var trail = shoe.GetComponentInChildren<TrailRenderer>();
            Assert.IsTrue(accent.Emitting);
            SettingsStore.Current.GraphicsQuality = 0;
            Snapshot(SlipperState.InFlight, new Vector3(0, 2, -3.8f));
            yield return null; yield return null;
            Assert.Greater(trail.widthMultiplier, 0, "Low preserves flight direction.");
            Snapshot(SlipperState.InFlight, new Vector3(6, 2, 4));
            yield return null; yield return null;
            var vertices = new Vector3[trail.positionCount]; trail.GetPositions(vertices);
            Assert.IsTrue(vertices.All(p => Vector3.Distance(p, shoe.transform.position) < 1),
                "A correction cannot leave a fabricated cross-court route.");
            Snapshot(SlipperState.Loose, new Vector3(6, shoe.RestHeight, 4));
            Assert.IsFalse(accent.Emitting); Assert.IsFalse(trail.enabled);
            yield return null; yield return null;
            Assert.AreEqual(0, trail.positionCount);
            Snapshot(SlipperState.InFlight, new Vector3(0, 2, -4));
            yield return null; yield return null;
            Assert.IsTrue(accent.Emitting);
            accent.enabled = false;
            Assert.IsFalse(accent.Emitting); Assert.IsFalse(trail.enabled);
            yield return null; yield return null;
            Assert.AreEqual(0, trail.positionCount);
        }

        [UnityTest] public IEnumerator CanEdgesExpireAndRepeatedStateDoesNotReplayContact()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            SettingsStore.Current.FlashIntensity = 1;
            var can = GameServices.Round.Lata;
            yield return new WaitForSecondsRealtime(.45f);
            NetAuthority.Provider = new Replica();
            Vector3 at = can.transform.position;
            can.ApplySnapshotState(at, Quaternion.identity, false, 0);
            Assert.AreEqual(1, Object.FindObjectsByType<CanContactAccent>().Length);
            can.ApplySnapshotState(at, Quaternion.identity, false, 0);
            Assert.AreEqual(1, Object.FindObjectsByType<CanContactAccent>().Length);
            Assert.AreEqual(0, Object.FindAnyObjectByType<CanContactAccent>().GetComponentsInChildren<Collider>().Length);
            yield return new WaitForSecondsRealtime(.45f);
            Assert.AreEqual(0, Object.FindObjectsByType<CanContactAccent>().Length);
            can.ApplySnapshotState(at, Quaternion.identity, true, 0);
            can.ApplySnapshotState(at, Quaternion.identity, true, 0);
            Assert.AreEqual(1, Object.FindObjectsByType<CanContactAccent>().Length);
            yield return new WaitForSecondsRealtime(.45f);
            SettingsStore.Current.FlashIntensity = 0;
            can.ApplySnapshotState(at, Quaternion.identity, false, 0);
            Assert.AreEqual(0, Object.FindObjectsByType<CanContactAccent>().Length,
                "Reduced flash must not suppress actual can state, only this decorative accent.");
            Assert.IsFalse(can.IsUpright);
        }
    }
}
