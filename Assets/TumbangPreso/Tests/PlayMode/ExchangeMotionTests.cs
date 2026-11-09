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

        [UnityTest] public IEnumerator CanEdgesKeepProtectionCueWithoutContactStrokes()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            SettingsStore.Current.FlashIntensity = 1;
            var can = GameServices.Round.Lata;
            yield return new WaitForSecondsRealtime(.45f);
            NetAuthority.Provider = new Replica();
            Vector3 at = can.transform.position;
            can.ApplySnapshotState(at, Quaternion.identity, false, 0);
            can.ApplySnapshotState(at, Quaternion.identity, false, 0);
            Assert.AreEqual(0, Object.FindObjectsByType<CanContactAccent>().Length, "Knockdown should not draw temporary contact strokes.");
            yield return new WaitForSecondsRealtime(.45f);
            can.ApplySnapshotState(at, Quaternion.identity, true, 0);
            can.ApplySnapshotState(at, Quaternion.identity, true, 0);
            Assert.AreEqual(0, Object.FindObjectsByType<CanContactAccent>().Length, "Restoring the can should leave only its protection cue.");
            Assert.That(can.ProtectionLeft, Is.EqualTo(4f).Within(.001f), "A client sees the same restoration window.");
            yield return null;
            if (!SettingsStore.Current.ReducedEffects)
            {
                var block = new MaterialPropertyBlock();
                var skin = can.transform.Find("Visual").GetComponentInChildren<Renderer>();
                skin.GetPropertyBlock(block);
                float steady = block.GetFloat("_RimStrength");
                Assert.Greater(steady, 0, "The protected can needs a visible rim.");
                Assert.That(block.GetColor("_RimColor").g, Is.GreaterThan(.65f), "Protection uses gold instead of the downed can's red rim.");
                var clock = typeof(Lata).GetField("_restoreProtectionLeft", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                bool reducedMotion = SettingsStore.Current.ReducedUiMotion;
                try
                {
                    SettingsStore.Current.ReducedUiMotion = false;
                    float dimmest = steady, brightest = 0;
                    for (int i = 0; i < 12; i++)
                    {
                        clock.SetValue(can, .98f - i * .075f);
                        yield return null;
                        skin.GetPropertyBlock(block);
                        float rim = block.GetFloat("_RimStrength");
                        dimmest = Mathf.Min(dimmest, rim); brightest = Mathf.Max(brightest, rim);
                    }
                    Assert.Greater(dimmest, 0, "The can is still visibly protected between pulses.");
                    Assert.Greater(brightest - dimmest, .3f, "The last second must visibly pulse.");
                    SettingsStore.Current.ReducedUiMotion = true;
                    clock.SetValue(can, .53f);
                    yield return null;
                    skin.GetPropertyBlock(block);
                    Assert.That(block.GetFloat("_RimStrength"), Is.EqualTo(steady).Within(.001f));
                    clock.SetValue(can, 0f);
                    yield return null;
                    skin.GetPropertyBlock(block);
                    Assert.AreEqual(0, block.GetFloat("_RimStrength"), "Protection ends on the actual clock edge.");
                }
                finally { SettingsStore.Current.ReducedUiMotion = reducedMotion; }
            }
            yield return new WaitForSecondsRealtime(.45f);
            SettingsStore.Current.FlashIntensity = 0;
            can.ApplySnapshotState(at, Quaternion.identity, false, 0);
            Assert.AreEqual(0, Object.FindObjectsByType<CanContactAccent>().Length,
                "Changing flash settings must not revive the removed accent.");
            Assert.IsFalse(can.IsUpright);
        }
    }
}
