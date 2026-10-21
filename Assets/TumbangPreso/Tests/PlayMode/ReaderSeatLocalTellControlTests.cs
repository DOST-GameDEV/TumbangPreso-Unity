using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ReaderSeatLocalTellControlTests
    {
        private sealed class OtherLocalSeat : INetProvider
        {
            public bool IsHost => false; public bool IsNetworked => true;
            public int LocalSlot => 2; public int LocalPeerId => 2; public bool IsSeatlessReferee => false;
        }
        // Reuse the frozen public world setup/cleanup; the original five cases stay unchanged.
        private readonly ReaderSeatPresentationLifetimeTests _world = new();
        [UnitySetUp] public IEnumerator Before() => _world.Before();
        [UnityTearDown] public IEnumerator After() => _world.After();
        [Test] public void ObsoleteReaderWithoutReceivedRefreshRetiresItsOwnLocalThrowTell()
        {
            var body = GameObject.Find("Reader custody original seat"); Assert.IsNotNull(body);
            var motor = body.GetComponent<CharacterMotor>(); var carrier = body.GetComponent<Carrier>();
            var reader = body.GetComponent<PlayerInputReader>(); var held = carrier.Held;
            Assert.IsNotNull(held); Assert.IsTrue(reader.enabled);
            motor.Intent.Set(Verb.SpecialAbility, true);
            var step = typeof(Carrier).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            step.Invoke(carrier, null); step.Invoke(carrier, null);
            Assert.IsTrue(carrier.IsCharging); Assert.Greater(carrier.ChargeRatio, 0);
            Assert.Greater(carrier.ObservedChargePower, 0, "The real local broadcast did not create its own visible tell.");
            // No ApplyObservedCharge refresh arrives after the last local broadcast.
            NetAuthority.Provider = new OtherLocalSeat(); Assert.AreNotEqual(motor.PlayerSlot, NetAuthority.LocalSlot);
            reader.enabled = false;
            Assert.IsFalse(carrier.IsCharging); Assert.Zero(carrier.ChargeRatio);
            Assert.AreEqual(-1, carrier.ObservedChargePower, "An obsolete local tell survived without a new received update.");
            Assert.Zero(carrier.ObservedPektusSpin); Assert.AreSame(held, carrier.Held);
        }
    }
}
