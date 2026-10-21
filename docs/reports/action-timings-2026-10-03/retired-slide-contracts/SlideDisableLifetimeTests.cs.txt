using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class SlideDisableLifetimeTests
    {
        private GameObject _body, _shoeBody;
        private CharacterMotor _motor;
        private CombatVerbs _verbs;
        private INetProvider _provider;
        private MatchStatsCollector _stats;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; NetAuthority.Provider = new SoloProvider();
            GameServices.Ensure();
            _stats = GameServices.Stats; typeof(GameServices).GetProperty("Stats").SetValue(null, null);
            PresentationClock.RequestScale(1);
            _body = new GameObject("Direct host slide lifetime");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 1; _motor.IsDefender = false; _motor.RoundActive = true;
            _verbs = _body.AddComponent<CombatVerbs>();
            _shoeBody = new GameObject("Owned loose slide target");
            var shoe = _shoeBody.AddComponent<Slipper>(); shoe.enabled = false;
            shoe.SeatOfOrigin = 1; shoe.OwnerSlot = 1;
            _shoeBody.transform.position = Vector3.forward * (Balance.PickupRadius + Balance.SlideDistance * .4f);
            Physics.SyncTransforms();
            Assert.IsTrue(_motor.CanAct());
        }

        [UnityTearDown] public IEnumerator After()
        {
            if (_body != null) Object.Destroy(_body);
            if (_shoeBody != null) Object.Destroy(_shoeBody);
            yield return null;
            typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            NetAuthority.Provider = _provider; PresentationClock.RequestScale(1);
            yield return PlayModeWorld.Reset();
        }

        private void OpenRealRetrievalWindow()
        {
            Assert.IsTrue(_verbs.HostResolveSlide(_body.transform.position, Vector3.forward));
            Assert.IsTrue(_verbs.SlideActive); Assert.Greater(_verbs.SlideCooldownLeft, 0);
        }

        [UnityTest] public IEnumerator BodyRetirementCannotResumeTheOldRetrievalButKeepsItsSpentCooldown()
        {
            OpenRealRetrievalWindow(); float cooldown = _verbs.SlideCooldownLeft;
            _body.SetActive(false); _body.SetActive(true);
            Assert.IsFalse(_verbs.SlideActive, "Reactivated body retained its pre-retirement retrieval sweep.");
            Assert.AreEqual(cooldown, _verbs.SlideCooldownLeft);
            Assert.IsFalse(_verbs.HostResolveSlide(_body.transform.position, Vector3.forward));
            yield return null; Assert.IsFalse(_verbs.SlideActive);
        }

        [UnityTest] public IEnumerator AClockHoldPreservesRetrievalWhileTheBodyStaysEnabled()
        {
            OpenRealRetrievalWindow(); float cooldown = _verbs.SlideCooldownLeft;
            PresentationClock.RequestScale(0); yield return new WaitForSecondsRealtime(.1f);
            Assert.IsTrue(_body.activeInHierarchy); Assert.IsTrue(_verbs.enabled);
            Assert.IsTrue(_verbs.SlideActive); Assert.AreEqual(cooldown, _verbs.SlideCooldownLeft);
            PresentationClock.RequestScale(1);
        }

        [UnityTest] public IEnumerator IdleBodyRetirementDoesNotCreateRetrievalOrCooldown()
        {
            _body.SetActive(false); _body.SetActive(true); yield return null;
            Assert.IsFalse(_verbs.SlideActive); Assert.AreEqual(0, _verbs.SlideCooldownLeft);
        }
    }
}
