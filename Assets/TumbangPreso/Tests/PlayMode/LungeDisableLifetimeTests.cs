using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class LungeDisableLifetimeTests
    {
        private GameObject _body;
        private CharacterMotor _motor;
        private CombatVerbs _verbs;
        private INetProvider _provider;
        private MatchStatsCollector _stats;
        private float Contact => (float)typeof(CombatVerbs).GetField("_lungeActiveLeft", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_verbs);
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; NetAuthority.Provider = new SoloProvider();
            _stats = GameServices.Stats; typeof(GameServices).GetProperty("Stats").SetValue(null, null);
            PresentationClock.RequestScale(1);
            _body = new GameObject("Direct host lunge lifetime");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 0; _motor.IsDefender = true; _motor.RoundActive = true;
            _verbs = _body.AddComponent<CombatVerbs>();
            Assert.IsTrue(_motor.CanAct());
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_body != null) Object.Destroy(_body); yield return null;
            typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            NetAuthority.Provider = _provider; PresentationClock.RequestScale(1);
            yield return PlayModeWorld.Reset();
        }
        private void OpenRealContact()
        {
            Assert.IsTrue(_verbs.HostResolveLunge(_body.transform.position, Vector3.forward, 1));
            Assert.Greater(Contact, 0); Assert.Greater(_verbs.LungeCooldownLeft, 0);
        }
        [UnityTest] public IEnumerator BodyRetirementCannotResumeTheOldContactButKeepsItsSpentCooldown()
        {
            OpenRealContact(); float cooldown = _verbs.LungeCooldownLeft;
            _body.SetActive(false); _body.SetActive(true);
            Assert.LessOrEqual(Contact, 0, "Reactivated body retained its pre-retirement lunge sweep.");
            Assert.AreEqual(cooldown, _verbs.LungeCooldownLeft);
            Assert.IsFalse(_verbs.HostResolveLunge(_body.transform.position, Vector3.forward, 1), "Retirement refunded the spent lunge.");
            yield return null;
            Assert.LessOrEqual(Contact, 0);
        }
        [UnityTest] public IEnumerator AClockHoldPreservesTheExistingContactWhileTheBodyStaysEnabled()
        {
            OpenRealContact(); float contact = Contact; float cooldown = _verbs.LungeCooldownLeft;
            PresentationClock.RequestScale(0);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.IsTrue(_body.activeInHierarchy); Assert.IsTrue(_verbs.enabled);
            Assert.AreEqual(contact, Contact); Assert.AreEqual(cooldown, _verbs.LungeCooldownLeft);
            PresentationClock.RequestScale(1);
        }
        [UnityTest] public IEnumerator UnchargedBodyRetirementDoesNotCreateContactOrCooldown()
        {
            _body.SetActive(false); _body.SetActive(true); yield return null;
            Assert.LessOrEqual(Contact, 0); Assert.AreEqual(0, _verbs.LungeCooldownLeft);
        }
    }
}
