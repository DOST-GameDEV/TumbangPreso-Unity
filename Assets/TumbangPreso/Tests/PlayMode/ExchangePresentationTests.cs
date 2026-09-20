using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class ExchangePresentationTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest] public IEnumerator ClassicExchangeUsesTruthfulStateAndPersonalConfirmation() => Exchange(GameMode.Classic);
        [UnityTest] public IEnumerator HeroExchangeUsesTruthfulStateAndPersonalConfirmation() => Exchange(GameMode.HeroStrike);

        private static IEnumerator Exchange(GameMode mode)
        {
            SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(mode));
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false;
            yield return SceneManager.LoadSceneAsync("Eskinita");
            yield return new WaitForSecondsRealtime(.4f);
            foreach (var ai in Object.FindObjectsByType<AIController>()) ai.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>()) input.enabled = false;
            var runner = Object.FindAnyObjectByType<SliceRunner>(); Assert.IsNotNull(runner);
            if (!runner.Running) runner.Begin();
            yield return null;
            var round = GameServices.Round; var match = GameServices.Match;
            Assert.IsTrue(round.RoundActive);
            foreach (var player in round.Players) { player.Intent.Clear(); player.ClearStun(); player.ClearTrip(); }
            Hud.Instance.ShowReadyPrompt(false);
            var me = round.PlayerAt(1); var other = round.PlayerAt(2); var taya = round.PlayerAt(match.DefenderSlot);
            Assert.IsNotNull(me); Assert.IsNotNull(other); Assert.IsNotNull(taya);
            var lata = round.Lata;
            me.Teleport(lata.transform.position + new Vector3(0, 0, -3));
            me.transform.rotation = Quaternion.identity;
            yield return null;
            var feed = Object.FindAnyObjectByType<MatchEventFeed>(); Assert.IsNotNull(feed);
            var marker = Object.FindAnyObjectByType<OffscreenIndicators>(); Assert.IsNotNull(marker);
            var canvas = GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            Text Label(string name) => canvas.GetComponentsInChildren<Text>(true).First(t => t.name == name);
            int points = match.ScoreFor(me.PlayerSlot);
            float beforeScale = Time.timeScale;
            lata.HostKnockDown(me.PlayerSlot);
            Assert.AreEqual(beforeScale, Time.timeScale, "A normal tin contact must not pause unrelated players.");
            Assert.AreEqual(points + MatchRules.PointsFor(ScoreEvent.LataKnocked), match.ScoreFor(me.PlayerSlot));
            Assert.AreEqual("P2  DOWNED LATA", feed.Entry(0));
            Assert.IsNotNull(GameObject.Find("~TinContact"));
            Assert.IsNull(GameObject.Find("ConfettiRibbon"), "Routine tin contact does not use milestone confetti.");
            Assert.IsTrue(Label("HitConfirmation").enabled, "The credited local thrower gets confirmation.");
            Assert.IsFalse(Label("MatchToast").enabled, "The side feed replaces the duplicate ordinary score toast.");
            yield return null;
            Assert.IsTrue(marker.CanMarkerVisible);
            Assert.AreEqual("DOWN", marker.CanMarkerState);
            Assert.AreNotEqual("You can be tagged", Label("ActionPrompt").text, "A down can does not permit tags.");
            yield return GameplayShots.Render(Camera.main, mode + "-can-down", true, outDir: "Logs/exchange-presentation-v1");

            lata.HostRestore(); yield return null;
            Assert.AreEqual("P1  RESTORED LATA", feed.Entry(0));
            Assert.AreEqual("CAN PROTECTED", marker.CanMarkerState);
            Assert.AreEqual("You can be tagged", Label("ActionPrompt").text,
                "Can protection does not protect an armed attacker inside the box.");
            yield return GameplayShots.Render(Camera.main, mode + "-restored-danger", true, outDir: "Logs/exchange-presentation-v1");
            int count = feed.Count;
            MatchFlair.Play(MatchFlair.Kind.Throw, 1, -1, me.transform.position);
            Assert.AreEqual(count, feed.Count, "The feed excludes routine release spam.");
            MatchFlair.Play(MatchFlair.Kind.Block, 2, 0, lata.transform.position);
            Assert.AreEqual("P1  BLOCKED P3", feed.Entry(0));
            MatchFlair.Play(MatchFlair.Kind.Block, 2, 0, lata.transform.position);
            Assert.AreEqual(Mathf.Min(3, count + 1), feed.Count, "Immediate duplicate contact must not occupy another row.");
            yield return new WaitForSeconds(.3f);
            Assert.IsFalse(Label("HitConfirmation").enabled);
            MatchFlair.Play(MatchFlair.Kind.LataDown, 2, -1, lata.transform.position);
            Assert.IsFalse(Label("HitConfirmation").enabled, "Another scorer cannot produce my hitmarker.");
            Assert.AreEqual(3, feed.Count, "Storage is capped, with no pending stale queue.");
            Hud.Instance.EnterSpectatorMode(); yield return null;
            Hud.Instance.SetCleanFeed(true); Assert.IsFalse(feed.gameObject.activeInHierarchy);
            Hud.Instance.SetCleanFeed(false); yield return null;
            Assert.AreEqual(0, feed.Count, "Hiding and rebuilding presentation must not replay old events.");
            Hud.Instance.ExitSpectatorMode();
        }
    }
}
