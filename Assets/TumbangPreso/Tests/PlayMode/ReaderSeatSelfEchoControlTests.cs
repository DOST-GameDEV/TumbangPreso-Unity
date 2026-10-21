using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ReaderSeatSelfEchoControlTests
    {
        private sealed class HostOwner : INetProvider
        {
            public bool IsHost => true; public bool IsNetworked => true;
            public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false;
        }
        private sealed class OtherLocalSeat : INetProvider
        {
            public bool IsHost => false; public bool IsNetworked => true;
            public int LocalSlot => 2; public int LocalPeerId => 2; public bool IsSeatlessReferee => false;
        }
        private readonly ReaderSeatPresentationLifetimeTests _world = new();
        [UnitySetUp] public IEnumerator Before() => _world.Before();
        [UnityTearDown] public IEnumerator After() => _world.After();
        [Test] public void OwnedSnapshotSelfApplyDoesNotTurnItsOldLocalTellIntoAReceivedRefresh()
        {
            var body = GameObject.Find("Reader custody original seat"); Assert.IsNotNull(body);
            var motor = body.GetComponent<CharacterMotor>(); var carrier = body.GetComponent<Carrier>();
            var reader = body.GetComponent<PlayerInputReader>(); var held = carrier.Held;
            Assert.IsNotNull(held); Assert.IsTrue(reader.enabled);
            NetAuthority.Provider = new HostOwner(); Assert.AreEqual(motor.PlayerSlot, NetAuthority.LocalSlot);
            motor.Intent.Set(Verb.SpecialAbility, true);
            var step = typeof(Carrier).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            step.Invoke(carrier, null); step.Invoke(carrier, null);
            Assert.IsTrue(carrier.IsCharging); Assert.Greater(carrier.ObservedChargePower, 0);
            // Same getter-to-public-setter sample as BroadcastWorldSnapshot's host self-application.
            // The fixture supplies the self-application seam, with no live RPC delivery.
            carrier.ApplyObservedCharge(true, carrier.ObservedChargePower * Balance.ChargeFullTime,
                carrier.ObservedPektusSpin);
            Assert.Greater(carrier.ObservedChargePower, 0);
            NetAuthority.Provider = new OtherLocalSeat(); reader.enabled = false;
            Assert.IsFalse(carrier.IsCharging); Assert.Zero(carrier.ChargeRatio); Assert.AreSame(held, carrier.Held);
            Assert.AreEqual(-1, carrier.ObservedChargePower,
                "A host snapshot echo reclassified the old local tell as a new received refresh.");
            Assert.Zero(carrier.ObservedPektusSpin);
        }
    }
}
