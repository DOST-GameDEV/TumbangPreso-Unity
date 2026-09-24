using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Films the throw, both pektus and (see the reset case) the can being raised, in BOTH views at once:
    /// a witness on the body and the owner's own first-person camera, through `ImprovementEvidenceProbe.Record`.
    ///
    /// 🧑 2026-09-24: *"refine other gameplay animations too for both FPP and TPP view"*, *"analyze how it
    /// should look to communicate that that action is happening"*. The analysis is
    /// `docs/reports/gameplay-animation-2026-09-24/research-and-analysis.md`; these frames are what it is
    /// judged against. The charge goes through `Carrier.ApplyObservedCharge`, the same seam the network
    /// drives, so the body and the owner's hand prepare exactly as in a match, and the release is the
    /// render-only `PlayAction`: no physical slipper is thrown and no score is touched.
    /// Frames land in `$TUMP_EVIDENCE/action-<name>/` (and `owner/` beside them).
    /// </summary>
    public sealed class GameplayActionShots
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator ThrowAndBothPektusInBothViews()
        {
            yield return MapRetrievalProbe.Load("BayanPlaza");
            var who = GameServices.Round.PlayerAt(1);
            who.Teleport(new Vector3(0, .18f, -8)); who.transform.rotation = Quaternion.identity;
            var carrier = who.GetComponent<Carrier>();
            Assert.IsNotNull(carrier.Held, "The filmed thrower must be carrying.");
            var animator = who.GetComponent<CharacterAnimator>();
            var rig = Camera.main.GetComponent<CameraRig>(); rig.Follow(who, true); rig.SetAimSource(AimSource.Movement);
            var camera = new GameObject("Action witness").AddComponent<Camera>();
            camera.CopyFrom(Camera.main); camera.enabled = false; camera.tag = "Untagged";
            camera.cullingMask &= ~(1 << 5); camera.fieldOfView = 40;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var arms = Object.FindAnyObjectByType<ViewmodelArms>();
            var pivotField = typeof(ViewmodelArms).GetField("_rightPivot", BindingFlags.Instance | BindingFlags.NonPublic);
            var leftField = typeof(ViewmodelArms).GetField("_leftPivot", BindingFlags.Instance | BindingFlags.NonPublic);
            var hands = new StringBuilder("action,t,right_vx,right_vy,right_depth,left_vx,left_vy,left_depth").AppendLine();
            void Hands(string name, float t)
            {
                if (arms == null) return;
                var cam = Camera.main;
                var r = (Transform)pivotField.GetValue(arms); var l = (Transform)leftField.GetValue(arms);
                if (r == null || l == null || cam == null) return;
                var rv = cam.WorldToViewportPoint(r.position); var lv = cam.WorldToViewportPoint(l.position);
                hands.AppendLine(FormattableString.Invariant($"{name},{t:F2},{rv.x:F3},{rv.y:F3},{rv.z:F3},{lv.x:F3},{lv.y:F3},{lv.z:F3}"));
            }
            try
            {
                foreach (var (name, spin) in new[] { ("throw", 0f), ("pektus-left", -1f), ("pektus-right", 1f) })
                {
                    bool released = false;
                    // Charge 0.3 to 1.3 s (a full wind-up), release at 1.3 s, then 1.0 s of follow-through.
                    yield return ImprovementEvidenceProbe.Record(camera, "action-" + name, 2.3f, who, t =>
                    {
                        Hands(name, t);
                        if (t >= .3f && t < 1.3f) carrier.ApplyObservedCharge(true, Balance.ChargeFullTime * Mathf.Clamp01((t - .3f) / 1f), spin);
                        else if (t >= 1.3f && !released)
                        {
                            released = true; carrier.ApplyObservedCharge(false);
                            animator.PlayAction(ThrowGesture.Action(spin));
                        }
                    }, new Vector3(2.6f, 1.3f, 2.2f));
                    // The taya's view: straight in front of the thrower.
                    released = false;
                    yield return new WaitForSeconds(.4f);
                    yield return ImprovementEvidenceProbe.Record(camera, "action-" + name + "-front", 2.3f, who, t =>
                    {
                        if (t >= .3f && t < 1.3f) carrier.ApplyObservedCharge(true, Balance.ChargeFullTime * Mathf.Clamp01((t - .3f) / 1f), spin);
                        else if (t >= 1.3f && !released)
                        {
                            released = true; carrier.ApplyObservedCharge(false);
                            animator.PlayAction(ThrowGesture.Action(spin));
                        }
                    }, new Vector3(0.4f, 1.2f, 3.6f));
                    Assert.IsTrue(released, $"{name}: never released.");
                    yield return new WaitForSeconds(.5f);
                }
            }
            finally
            {
                carrier.ApplyObservedCharge(false); Object.Destroy(camera.gameObject);
                var dir = Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs/improvement-baseline-v1";
                Directory.CreateDirectory(dir); File.WriteAllText(Path.Combine(dir, "hands.csv"), hands.ToString());
            }
        }

        [UnityTest]
        public IEnumerator RaisingTheCanInBothViews()
        {
            yield return MapRetrievalProbe.Load("BayanPlaza");
            var lata = GameServices.Round.Lata;
            CharacterMotor taya = null;
            foreach (var p in GameServices.Round.Players) if (p.IsDefender) taya = p;
            Assert.IsNotNull(taya, "No taya this round.");
            foreach (var p in GameServices.Round.Players) if (p != taya) p.Teleport(new Vector3(20 + p.PlayerSlot * 3, .2f, -20));
            // The taya's bot would right the can itself; this probe is the only hand on it.
            foreach (var brain in taya.GetComponents<MonoBehaviour>()) if (brain.GetType().Name == "AIController") brain.enabled = false;
            var at = lata.transform.position; at.y = .18f;
            taya.Teleport(at + new Vector3(0, 0, -.9f)); taya.transform.rotation = Quaternion.identity;
            // Knocked down once any restore protection has lapsed, and confirmed down before the hold.
            for (int i = 0; i < 20 && lata.IsUpright; i++) { lata.HostKnockDown(1); yield return new WaitForSeconds(.25f); }
            Assert.IsFalse(lata.IsUpright, "Could not knock the can down for the raise.");
            var rig = Camera.main.GetComponent<CameraRig>(); rig.Follow(taya, true); rig.SetAimSource(AimSource.Movement);
            var hold = taya.gameObject.AddComponent<HoldGrab>();
            var camera = new GameObject("Raise witness").AddComponent<Camera>();
            camera.CopyFrom(Camera.main); camera.enabled = false; camera.tag = "Untagged";
            camera.cullingMask &= ~(1 << 5); camera.fieldOfView = 40;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            float peak = 0; string why = "";
            try
            {
                yield return ImprovementEvidenceProbe.Record(camera, "action-raise", 2.4f, taya, t =>
                {
                    hold.Hold = t >= .3f;
                    peak = Mathf.Max(peak, taya.GetComponent<CharacterAnimator>().ResetRaiseProgress);
                    var c = taya.GetComponent<Carrier>();
                    if (t > 1f && why == "") why = $"canAct {taya.CanAct()} target {c.HasResetTarget} grab {taya.Intent.Pressed(Verb.Grab)} parked {taya.Intent.Parked} ratio {c.ChannelRatio:F2} upright {lata.IsUpright}";
                }, new Vector3(3.3f, 1.5f, .4f));
            }
            finally { Object.Destroy(hold); Object.Destroy(camera.gameObject); }
            Assert.Greater(peak, .5f, "The raise never showed progress: " + why);
        }

        [DefaultExecutionOrder(-300)] private sealed class HoldGrab : MonoBehaviour
        {
            public bool Hold;
            private void Update() { var who = GetComponent<CharacterMotor>(); who.Intent.Parked = false; who.Intent.Move = Vector2.zero; who.Intent.Set(Verb.Grab, Hold); }
        }
    }
}
