using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class CatchReconstructionTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        private static IEnumerator Open()
        {
            SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false;
            yield return SceneManager.LoadSceneAsync("Eskinita"); yield return new WaitForSeconds(.2f);
            foreach (var ai in Object.FindObjectsByType<AIController>()) ai.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>()) input.enabled = false;
            Object.FindAnyObjectByType<SliceRunner>().Begin(); yield return null;
            foreach (var actor in GameServices.Round.Players) { actor.Intent.Clear(); actor.ClearStun(); actor.ClearTrip(); }
            Hud.Instance.ShowReadyPrompt(false);
            float until = Time.unscaledTime + 3;
            while (GameServices.Round.Lata.IsProtected && Time.unscaledTime < until) yield return null;
        }
        private static void Stage()
        {
            var round = GameServices.Round; var can = round.Lata.transform.position;
            var taya = round.PlayerAt(0); var victim = round.PlayerAt(1);
            victim.ClearStun(); victim.ClearTrip(); victim.Teleport(can + Vector3.back * 2.2f);
            taya.Teleport(can + Vector3.back * 3.2f); taya.transform.forward = Vector3.forward;
            victim.transform.forward = Vector3.forward;
            for (int i = 2; i < 4; i++) round.PlayerAt(i).Teleport(can + new Vector3(-5, 0, i * 2));
        }
        [UnityTest]
        public IEnumerator TenActualCatchesPreserveTayaAndReturnBeforeControl()
        {
            yield return Open();
            var round = GameServices.Round; var taya = round.PlayerAt(0); var victim = round.PlayerAt(1);
            var view = Object.FindAnyObjectByType<CatchReconstruction>(); Assert.IsNotNull(view);
            var rig = Camera.main.GetComponent<CameraRig>(); Assert.IsTrue(rig.IsFollowing(victim));
            float lens = Camera.main.fieldOfView;
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            Settings.SettingsStore.Current.ReducedUiMotion = false;
            try
            {
                for (int n = 0; n < 10; n++)
                {
                    Stage(); yield return new WaitForSeconds(.4f);
                    Assert.IsTrue(victim.IsTaggable());
                    Vector3 contact = victim.transform.position, tayaAt = taya.transform.position;
                    int score = GameServices.Match.ScoreFor(0);
                    Assert.IsTrue(taya.GetComponent<CombatVerbs>().HostResolvePunch(tayaAt, taya.transform.forward));
                    Assert.AreEqual(score + MatchRules.PointsFor(ScoreEvent.Tag), GameServices.Match.ScoreFor(0));
                    Assert.IsTrue(view.Playing, "Accepted tag must produce the victim view from actual prior poses.");
                    Assert.AreEqual(tayaAt, taya.transform.position, "The reconstruction must not move the live taya.");
                    Assert.IsTrue(taya.CanAct(), "Presentation must not add taya recovery.");
                    Assert.Greater((victim.transform.position - contact).magnitude, 2, "Authority still teleports immediately.");
                    float remaining = view.Remaining;
                    MatchFlair.Play(MatchFlair.Kind.Tag, 0, 1, contact);
                    Assert.LessOrEqual(view.Remaining, remaining + .01f, "Duplicate delivery must not restart the camera.");
                    yield return new WaitForSecondsRealtime(.18f);
                    var copies = GameObject.Find("~CatchPlaybackCopies"); Assert.IsNotNull(copies);
                    Assert.IsEmpty(copies.GetComponentsInChildren<Collider>(true));
                    Assert.IsTrue(copies.GetComponentsInChildren<Renderer>(true).All(r => r.forceRenderingOff),
                        "Copies must stay invisible to ordinary world cameras.");
                    if (n == 0)
                        yield return GameplayShots.Render(Camera.main, "victim-catch", true, outDir: "Logs/catch-reconstruction-v1");
                    yield return new WaitForSecondsRealtime(1.1f);
                    Assert.IsFalse(view.Playing); Assert.IsFalse(victim.CanAct(), "Camera exit does not cancel the tag penalty.");
                    Assert.IsTrue(rig.IsFollowing(victim)); Assert.AreEqual(lens, Camera.main.fieldOfView, .1f);
                    Assert.IsTrue(victim.GetComponent<Carrier>().Held.GetComponentsInChildren<Renderer>(true).All(r => !r.forceRenderingOff),
                        "Held-item render flags must restore even when enumerated through two owners.");
                }
            }
            finally { Settings.SettingsStore.Current.ReducedUiMotion = reduced; view.End(); }
        }
        [UnityTest]
        public IEnumerator LateEventsAndReducedMotionNeverExtendRecovery()
        {
            yield return Open(); Stage(); yield return new WaitForSeconds(.4f);
            var victim = GameServices.Round.PlayerAt(1); var view = Object.FindAnyObjectByType<CatchReconstruction>();
            victim.ApplyStagger(.2f); MatchFlair.Play(MatchFlair.Kind.Tag, 0, 1, victim.transform.position);
            Assert.IsFalse(view.Playing, "A late event with no useful recovery window has no takeover.");
            victim.ClearStun();
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            try
            {
                Settings.SettingsStore.Current.ReducedUiMotion = true;
                GameServices.Round.ResolveTag(GameServices.Round.PlayerAt(0), victim);
                Assert.IsFalse(view.Playing); Assert.Greater(victim.StunLeft, 4);
            }
            finally { Settings.SettingsStore.Current.ReducedUiMotion = reduced; }
        }
    }
}
