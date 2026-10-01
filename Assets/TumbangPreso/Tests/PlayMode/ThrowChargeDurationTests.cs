using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ThrowChargeDurationTests
    {
        private sealed class Offline : INetProvider
        {
            public bool IsHost => true;
            public bool IsNetworked => false;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }
        private INetProvider _previous;
        private GameObject _owner, _shoeRoot;
        private CharacterMotor _motor;
        private Carrier _carrier;
        private Slipper _shoe;
        private static readonly MethodInfo Step = typeof(Carrier).GetMethod("StepAttacker",
            BindingFlags.Instance | BindingFlags.NonPublic);

        [UnitySetUp]
        public IEnumerator Before()
        {
            _previous = NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = new Offline();
            GameServices.Ensure(); GameServices.Round.BeginRound();
            _owner = new GameObject("Throw charge owner");
            _motor = _owner.AddComponent<CharacterMotor>();
            _motor.enabled = false; _motor.PlayerSlot = 1;
            _motor.transform.position = Vector3.back * (Balance.ConfinementRadius + 1f);
            _motor.Intent.AimPoint = Vector3.zero;
            _carrier = _owner.AddComponent<Carrier>(); _carrier.enabled = false;
            _shoeRoot = new GameObject("Throw charge slipper");
            _shoe = _shoeRoot.AddComponent<Slipper>(); _shoe.enabled = false; _shoe.OwnerSlot = 1;
            Assert.That(_shoe.HostForceEquip(_motor), Is.True);
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            Object.Destroy(_owner); Object.Destroy(_shoeRoot);
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _previous;
        }

        private void Hold(GameMode mode, float seconds)
        {
            _motor.Mode = mode;
            _motor.Intent.Set(Verb.SpecialAbility, true);
            using (NetCue.SuppressRelay())
            {
                Step.Invoke(_carrier, new object[] { 0f });
                Assert.That(_carrier.IsCharging, Is.True);
                Step.Invoke(_carrier, new object[] { seconds });
            }
        }

        [TestCase(GameMode.Classic), TestCase(GameMode.HeroStrike)]
        public void FullPowerIsReadyAfterOneAndAQuarterSeconds(GameMode mode)
        {
            Hold(mode, 1.25f);
            Assert.That(_carrier.ChargeRatio, Is.EqualTo(1f).Within(.0001f));
            Assert.That(_carrier.ObservedChargePower, Is.EqualTo(1f).Within(.0001f));
            Assert.That(_shoe.State, Is.EqualTo(SlipperState.Held));
        }

        [TestCase(GameMode.Classic), TestCase(GameMode.HeroStrike)]
        public void HalfChargeAndLongHoldsStayBounded(GameMode mode)
        {
            Hold(mode, .625f);
            Assert.That(_carrier.ChargeRatio, Is.EqualTo(.5f).Within(.0001f));
            using (NetCue.SuppressRelay()) Step.Invoke(_carrier, new object[] { 10f });
            Assert.That(_carrier.ChargeRatio, Is.EqualTo(1f));
        }

        [TestCase(GameMode.Classic), TestCase(GameMode.HeroStrike)]
        public void ReleaseUsesTheDisplayedFullPowerAndClearsWindup(GameMode mode)
        {
            Hold(mode, 1.25f);
            Assert.That(_carrier.ChargeRatio, Is.EqualTo(1f));
            var expected = _carrier.LaunchVelocityNow();
            _motor.Intent.Set(Verb.SpecialAbility, false);
            using (NetCue.SuppressRelay()) Step.Invoke(_carrier, new object[] { .01f });
            Assert.That(_carrier.IsCharging, Is.False);
            Assert.That(_carrier.Held, Is.Null);
            Assert.That(_carrier.ObservedChargePower, Is.EqualTo(-1f));
            Assert.That(_shoe.State, Is.EqualTo(SlipperState.InFlight));
            Assert.That(Vector3.Distance(_shoe.Velocity, expected), Is.LessThan(.001f));
        }

        [TestCase(GameMode.Classic), TestCase(GameMode.HeroStrike)]
        public void ObservedChargeUsesTheSameNewDuration(GameMode mode)
        {
            _motor.Mode = mode;
            _carrier.ApplyObservedCharge(true, .625f);
            Assert.That(_carrier.ObservedChargePower, Is.EqualTo(.5f).Within(.0001f));
            _carrier.ApplyObservedCharge(true, 1.25f);
            Assert.That(_carrier.ObservedChargePower, Is.EqualTo(1f));
            _carrier.ApplyObservedCharge(false);
            Assert.That(_carrier.ObservedChargePower, Is.EqualTo(-1f));
        }
    }
}
