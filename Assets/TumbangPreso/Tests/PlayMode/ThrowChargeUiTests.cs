using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class ThrowChargeUiTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        static readonly MethodInfo Step = typeof(Carrier).GetMethod("StepAttacker", BindingFlags.Instance | BindingFlags.NonPublic);

        [UnityTest]
        public IEnumerator CentreDotStaysFilledAndOnlyPulsesForItsOwnersThrow()
        {
            var root = new GameObject("DotTestCanvas", typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var go = new GameObject("Reticle", typeof(RectTransform));
            go.transform.SetParent(root.transform, false);
            var reticle = go.AddComponent<HudReticle>();
            reticle.rectTransform.sizeDelta = new Vector2(96, 96);
            var settings = Settings.SettingsStore.Current;
            bool motion = settings.ReducedUiMotion, effects = settings.ReducedEffects;
            try
            {
                settings.ReducedUiMotion = settings.ReducedEffects = false;
                reticle.SetOwner(1);
                foreach (var charge in new[] { 0f, .5f, 1f })
                {
                    reticle.Set(charge, -.7f, .5f, false, true);
                    AssertDot(reticle);
                }
                reticle.Set(.8f, .7f, 0, true, false); AssertDot(reticle);
                reticle.Set(0, 0, 0, false, false); AssertDot(reticle);
                Assert.Zero(reticle.ReleasePulseRemaining, "Cancellation is not a confirmed release.");
                Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Throw, 2, -1, Vector3.zero);
                Assert.Zero(reticle.ReleasePulseRemaining, "Another player must not pulse this aim.");
                Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Throw, 1, -1, Vector3.zero);
                Assert.Greater(reticle.ReleasePulseRemaining, 0);
                yield return new WaitForSeconds(.26f);
                Assert.Zero(reticle.ReleasePulseRemaining);
                settings.ReducedUiMotion = true;
                Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Throw, 1, -1, Vector3.zero);
                Assert.Zero(reticle.ReleasePulseRemaining);
                reticle.Set(1, 0, 0, false, false);
                Assert.AreEqual(3.2f, reticle.AimRadius, .001f);
                reticle.enabled = false;
                settings.ReducedUiMotion = false;
                Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Throw, 1, -1, Vector3.zero);
                Assert.Zero(reticle.ReleasePulseRemaining, "Disabled reticle must unsubscribe.");
                reticle.enabled = true; reticle.SetOwner(1);
                root.GetComponent<Canvas>().enabled = false;
                Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Throw, 1, -1, Vector3.zero);
                Assert.Zero(reticle.ReleasePulseRemaining, "Hidden HUD must not retain a pulse.");
            }
            finally
            {
                settings.ReducedUiMotion = motion; settings.ReducedEffects = effects;
                Object.Destroy(root);
            }
        }

        private static void AssertDot(HudReticle reticle)
        {
            using var vh = new VertexHelper();
            typeof(HudReticle).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(VertexHelper) }, null)
                .Invoke(reticle, new object[] { vh });
            var mesh = new Mesh();
            try
            {
                vh.FillMesh(mesh);
                Assert.Greater(mesh.vertexCount, 0);
                var vertices = mesh.vertices; var triangles = mesh.triangles;
                var centre = reticle.rectTransform.rect.center;
                bool fillsCentre=false;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    Vector2 a=vertices[triangles[i]],b=vertices[triangles[i+1]],c=vertices[triangles[i+2]];
                    float x=Cross(b-a,centre-a),y=Cross(c-b,centre-b),z=Cross(a-c,centre-c);
                    if((x>=0&&y>=0&&z>=0)||(x<=0&&y<=0&&z<=0))fillsCentre=true;
                }
                Assert.IsTrue(fillsCentre,"The aim centre must contain the filled dot");
                if(reticle.Charge==0&&reticle.Cooldown==0)
                    foreach(var v in vertices)Assert.LessOrEqual(Vector2.Distance(v,centre),5.2f,"Idle aim must stay a compact dot");
            }
            finally { Object.Destroy(mesh); }
        }
        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

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
            var canvas = GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            yield return TumpUiCapture.Capture("Aim-dot-idle-960x540", canvas, 960, 540, false, true);
            Assert.IsTrue(GameServices.Round.CanThrow(who));
            who.Intent.Set(Verb.SpecialAbility, true);
            Step.Invoke(carrier, new object[] { 0f }); Step.Invoke(carrier, new object[] { Balance.ChargeFullTime * .5f });
            Assert.IsTrue(carrier.IsCharging);
            yield return null;
            var reticle = Object.FindFirstObjectByType<HudReticle>(); Assert.IsNotNull(reticle);
            Assert.AreEqual("50%", reticle.ChargeCaption); Assert.AreEqual(.5f, reticle.Charge, .001f);
            Step.Invoke(carrier, new object[] { Balance.ChargeFullTime * .5f }); yield return null;
            Assert.AreEqual("FULL · RELEASE", reticle.ChargeCaption); Assert.IsFalse(reticle.Refused);
            yield return TumpUiCapture.Capture("Throw-charge-full-960x540", canvas, 960, 540, false, true);
            yield return TumpUiCapture.Capture("Throw-charge-full-1600x680", canvas, 1600, 680, false, true);
            can.HostKnockDown(2);
            yield return null;
            Step.Invoke(carrier, new object[] { .02f });yield return null;
            Assert.IsFalse(can.IsUpright); Assert.IsFalse(GameServices.Round.CanThrow(who));
            Assert.IsFalse(carrier.IsCharging,"Knockdown must cancel the existing charge");
            Assert.AreEqual(0,carrier.ChargeRatio);
            Assert.AreEqual("",reticle.ChargeCaption,"Cancelled charge must leave no full-power prompt");
            can.HostRestore(); yield return null;
            Step.Invoke(carrier,new object[]{.1f});
            Assert.IsFalse(GameServices.Round.CanThrow(who));Assert.IsFalse(carrier.IsCharging,"Barrier must not bank a new charge");
            float until = Time.realtimeSinceStartup + 6;
            while ((!GameServices.Round.CanThrow(who) || can.IsProtected) && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(GameServices.Round.CanThrow(who)); Assert.IsFalse(can.IsProtected); yield return null;
            Step.Invoke(carrier,new object[]{0f});Assert.IsTrue(carrier.IsCharging);Assert.AreEqual(0,carrier.ChargeRatio);
            Step.Invoke(carrier,new object[]{Balance.ChargeFullTime});yield return null;
            Assert.AreEqual("FULL · RELEASE", reticle.ChargeCaption);
            reticle.enabled = false; Assert.AreEqual("", reticle.ChargeCaption, "Hidden reticle must hide its caption too.");
            reticle.enabled = true; yield return null;
            who.Intent.Set(Verb.SpecialAbility, false); Step.Invoke(carrier, new object[] { .01f }); yield return null;
            Assert.IsFalse(carrier.IsCharging); Assert.AreEqual("", reticle.ChargeCaption);
            Assert.Greater(reticle.ReleasePulseRemaining, 0, "The actual accepted throw must pulse the circle.");
            AssertDot(reticle);
            yield return TumpUiCapture.Capture("Aim-circle-release-1600x680", canvas, 1600, 680, false, true);
            yield return new WaitForSeconds(.3f);
            Assert.Zero(reticle.ReleasePulseRemaining);
        }
    }
}
