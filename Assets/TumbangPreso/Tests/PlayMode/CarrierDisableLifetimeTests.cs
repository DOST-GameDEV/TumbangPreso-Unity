using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class CarrierDisableLifetimeTests
    {
        private GameObject _body, _shoeBody, _canBody;
        private CharacterMotor _motor;
        private Carrier _carrier;
        private Slipper _shoe;
        private INetProvider _provider;
        private MatchStatsCollector _stats;
        private Lata _previousCan;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; NetAuthority.Provider = new SoloProvider();
            PresentationClock.RequestScale(1); GameServices.Ensure();
            _stats = GameServices.Stats; typeof(GameServices).GetProperty("Stats").SetValue(null, null);
            _previousCan = GameServices.Round.Lata;
            _canBody = new GameObject("Carrier lifetime can");
            var can = _canBody.AddComponent<Lata>(); can.enabled = false;
            GameServices.Round.Lata = can; GameServices.Round.BeginRound();
            _body = new GameObject("Carrier lifetime attacker");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 1; _motor.IsDefender = false; _motor.RoundActive = true;
            _motor.Mode = GameMode.Classic;
            _body.transform.position = Vector3.back * (Balance.ConfinementRadius + 1);
            _motor.Intent.AimPoint = Vector3.zero;
            _carrier = _body.AddComponent<Carrier>();
            _shoeBody = new GameObject("Carrier lifetime owned shoe");
            _shoe = _shoeBody.AddComponent<Slipper>(); _shoe.enabled = false; _shoe.OwnerSlot = 1;
            Assert.IsTrue(_shoe.HostForceEquip(_motor));
            Assert.IsTrue(GameServices.Round.CanThrow(_motor));
        }

        [UnityTearDown] public IEnumerator After()
        {
            if (_body != null) Object.Destroy(_body);
            if (_shoeBody != null) Object.Destroy(_shoeBody);
            if (_canBody != null) Object.Destroy(_canBody);
            yield return null;
            GameServices.Round.Lata = _previousCan;
            typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            NetAuthority.Provider = _provider; PresentationClock.RequestScale(1);
            yield return PlayModeWorld.Reset();
        }

        private IEnumerator StartActualCharge()
        {
            _motor.Intent.Set(Verb.SpecialAbility, true);
            float until = Time.realtimeSinceStartup + 3;
            while (!_carrier.IsCharging && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(_carrier.IsCharging); Assert.GreaterOrEqual(_carrier.ObservedChargePower, 0);
            Assert.AreSame(_shoe, _carrier.Held);
        }

        [UnityTest] public IEnumerator RetiredBodyCannotResumeAnOldChargeOrThrowOnReactivation()
        {
            yield return StartActualCharge();
            _body.SetActive(false); _motor.Intent.Set(Verb.SpecialAbility, false); _body.SetActive(true);
            yield return null;
            Assert.AreSame(_shoe, _carrier.Held, "Reactivation released the retired throw windup.");
            Assert.AreEqual(SlipperState.Held, _shoe.State);
            Assert.IsFalse(_carrier.IsCharging, "Reactivated carrier retained a retired throw windup.");
            Assert.AreEqual(-1, _carrier.ObservedChargePower);
        }

        [UnityTest] public IEnumerator RetiredBodyCannotReuseThePreviousObservedThrowTellOrSpin()
        {
            _carrier.ApplyObservedCharge(true, .2f, .5f);
            Assert.GreaterOrEqual(_carrier.ObservedChargePower, 0);
            Assert.AreEqual(.5f, _carrier.ObservedPektusSpin);
            _body.SetActive(false); _body.SetActive(true);
            Assert.AreEqual(-1, _carrier.ObservedChargePower, "Reactivated carrier retained the old peer's throw tell.");
            Assert.AreEqual(0, _carrier.ObservedPektusSpin);
            Assert.AreSame(_shoe, _carrier.Held); yield return null;
        }

        [UnityTest] public IEnumerator EnabledBodyStillChargesAndThrowsOnItsActualRelease()
        {
            yield return StartActualCharge();
            _motor.Intent.Set(Verb.SpecialAbility, false);
            float until = Time.realtimeSinceStartup + 3;
            while (_carrier.Held != null && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsFalse(_carrier.IsCharging); Assert.AreEqual(-1, _carrier.ObservedChargePower);
            Assert.IsNull(_carrier.Held); Assert.AreEqual(SlipperState.InFlight, _shoe.State);
        }
    }
}
