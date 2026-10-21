using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class LungeWindupLifetimeTests
    {
        private GameObject _body;
        private CharacterMotor _motor;
        private CombatVerbs _verbs;
        private INetProvider _provider;
        private MatchStatsCollector _stats;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; NetAuthority.Provider = new SoloProvider();
            _stats = GameServices.Stats; typeof(GameServices).GetProperty("Stats").SetValue(null, null);
            PresentationClock.RequestScale(1);
            _body = new GameObject("Lunge windup lifetime actor");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 0; _motor.IsDefender = true; _motor.RoundActive = true;
            _verbs = _body.AddComponent<CombatVerbs>(); Assert.IsTrue(_motor.CanAct());
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_body != null) Object.Destroy(_body); yield return null;
            typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            NetAuthority.Provider = _provider; PresentationClock.RequestScale(1);
            yield return PlayModeWorld.Reset();
        }
        private IEnumerator StartLocalCharge()
        {
            _motor.Intent.Set(Verb.Lunge, true);
            float until = Time.realtimeSinceStartup + 3;
            while (_verbs.ObservedLungeCharge < 0 && Time.realtimeSinceStartup < until) yield return null;
            Assert.GreaterOrEqual(_verbs.ObservedLungeCharge, 0, "Actual held input did not start the local windup.");
            Assert.Zero(_verbs.LungeCooldownLeft);
        }

        [UnityTest] public IEnumerator DisabledLocalWindupDoesNotReleaseASpontaneousDashAfterReactivation()
        {
            yield return StartLocalCharge();
            _body.SetActive(false); _motor.Intent.Set(Verb.Lunge, false); _body.SetActive(true);
            yield return null;
            Assert.Zero(_verbs.LungeCooldownLeft, "Reactivation released the retired local windup.");
            Assert.Less(_verbs.ObservedLungeCharge, 0); Assert.Zero(_verbs.LungeChargeRatio);
        }

        [UnityTest] public IEnumerator DisabledObservedWindupDoesNotReappearOnTheReactivatedBody()
        {
            _verbs.ApplyObservedLungeCharge(true, .2f);
            Assert.GreaterOrEqual(_verbs.ObservedLungeCharge, 0);
            _body.SetActive(false); _body.SetActive(true);
            Assert.Less(_verbs.ObservedLungeCharge, 0, "The retired observed windup reappeared before its stale-packet expiry.");
            Assert.Zero(_verbs.LungeCooldownLeft); yield return null;
        }

        [UnityTest] public IEnumerator AnEnabledBodyStillReleasesItsActualLocalWindupNormally()
        {
            yield return StartLocalCharge(); _motor.Intent.Set(Verb.Lunge, false);
            float until = Time.realtimeSinceStartup + 3;
            while (_verbs.LungeCooldownLeft <= 0 && Time.realtimeSinceStartup < until) yield return null;
            Assert.Greater(_verbs.LungeCooldownLeft, 0); Assert.Less(_verbs.ObservedLungeCharge, 0);
        }
    }
}
