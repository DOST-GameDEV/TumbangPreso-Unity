using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ThrowChargeUiTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        static readonly MethodInfo Step = typeof(Carrier).GetMethod("StepAttacker", BindingFlags.Instance | BindingFlags.NonPublic);

        [UnityTest, Timeout(60000)]
        public IEnumerator LiveChargeShowsPowerFullAndRealRefusalsThenClearsOnRelease()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            var ready = Object.FindFirstObjectByType<ReadyGate>(); ready.enabled = true; ready.StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            Assert.IsFalse(ready.AwaitingReady || ready.CountingDown);
            var who = GameServices.Round.PlayerAt(1); var carrier = who.GetComponent<Carrier>();
            var can = GameServices.Round.Lata;
            float clearBy = Time.realtimeSinceStartup + 6;
            while (can.IsProtected && Time.realtimeSinceStartup < clearBy) yield return null;
            Assert.IsFalse(can.IsProtected);
            foreach (var brain in Object.FindObjectsByType<AIController>()) brain.enabled = false;
            who.Teleport(new Vector3(-3, .12f, -8)); who.transform.rotation = Quaternion.identity;
            who.Intent.Clear(); who.Intent.Parked = false; who.Intent.AimPoint = new Vector3(-3, .12f, 0);
            var rig = Object.FindFirstObjectByType<CameraSystem.CameraRig>(); rig.Follow(who);
            rig.SetAimSource(CameraSystem.AimSource.Movement);
            yield return null;
            var direction = who.Intent.AimPoint - rig.transform.position;
            typeof(CameraSystem.CameraRig).GetField("_pitchDeg", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(rig, Mathf.Atan2(-direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg);
            carrier.enabled = false;
            Assert.IsTrue(GameServices.Round.CanThrow(who));
            who.Intent.Set(Verb.SpecialAbility, true);
            Step.Invoke(carrier, new object[] { 0f }); Step.Invoke(carrier, new object[] { Balance.ChargeFullTime * .5f });
            Assert.IsTrue(carrier.IsCharging);
            yield return null;
            var reticle = Object.FindFirstObjectByType<HudReticle>(); Assert.IsNotNull(reticle);
            Assert.AreEqual("50%", reticle.ChargeCaption); Assert.AreEqual(.5f, reticle.Charge, .001f);
            Step.Invoke(carrier, new object[] { Balance.ChargeFullTime * .5f }); yield return null;
            Assert.AreEqual("FULL · RELEASE", reticle.ChargeCaption); Assert.IsFalse(reticle.Refused);
            var canvas = GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            yield return TumpUiCapture.Capture("Throw-charge-full-960x540", canvas, 960, 540, false, true);
            yield return TumpUiCapture.Capture("Throw-charge-full-1600x680", canvas, 1600, 680, false, true);
            can.HostKnockDown(2);
            yield return null;
            Assert.IsFalse(can.IsUpright); Assert.IsTrue(GameServices.Round.CanThrow(who)); Assert.IsTrue(carrier.IsCharging);
            Assert.IsFalse(reticle.Refused); Assert.AreEqual("FULL · RELEASE", reticle.ChargeCaption, "The current rules explicitly allow release while the can is down.");
            can.HostRestore(); yield return null;
            Assert.IsTrue(reticle.Refused); Assert.AreEqual("WAIT", reticle.ChargeCaption, "Restoration protection still refuses the charged throw.");
            float until = Time.realtimeSinceStartup + 6;
            while ((!GameServices.Round.CanThrow(who) || can.IsProtected) && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(GameServices.Round.CanThrow(who)); Assert.IsFalse(can.IsProtected); yield return null;
            Assert.AreEqual("FULL · RELEASE", reticle.ChargeCaption);
            reticle.enabled = false; Assert.AreEqual("", reticle.ChargeCaption, "Hidden reticle must hide its caption too.");
            reticle.enabled = true;
            who.Intent.Set(Verb.SpecialAbility, false); Step.Invoke(carrier, new object[] { .01f }); yield return null;
            Assert.IsFalse(carrier.IsCharging); Assert.AreEqual("", reticle.ChargeCaption);
        }
    }
}
