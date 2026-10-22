using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Casts read on every screen (owner, 2026-10-22, pictograms over text): an accepted cast is presented once, pops
    /// a seat-colour disc with the ability's own icon over the caster (`CastCallout`) and adds one icon row to the
    /// event feed (`MatchEventFeed`). The captures in Logs/shots-native-ui are the review.
    /// </summary>
    public sealed class CastPresentationTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator AcceptedCastPopsTheCasterCalloutAndOneFeedRow()
        {
            SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita); yield return new WaitForSeconds(.2f);
            foreach (var ai in Object.FindObjectsByType<AIController>()) ai.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>()) input.enabled = false;
            Object.FindAnyObjectByType<SliceRunner>().Begin(); yield return null;
            foreach (var actor in GameServices.Round.Players) { actor.Intent.Clear(); actor.ClearStun(); actor.ClearTrip(); }
            Hud.Instance.ShowReadyPrompt(false);
            yield return new WaitForSeconds(.5f);
            Assert.IsTrue(GameServices.Round.RoundActive);

            int seen = 0; CharacterMotor caster = null; HeroAbility cast = null; bool ultimate = true;
            System.Action<CharacterMotor, HeroAbility, bool> note = (c, a, u) => { seen++; caster = c; cast = a; ultimate = u; };
            HeroAbilitySystem.CastPresented += note;
            try
            {
                var feed = Object.FindAnyObjectByType<MatchEventFeed>();
                Assert.IsNotNull(feed, "The match HUD carries the event feed.");
                int rows = feed.Count;
                var who = GameServices.Round.PlayerAt(2);
                // In front of this screen's camera, so the pop over the caster is in the review capture.
                var eye = Camera.main.transform;
                Vector3 ahead = eye.position + Vector3.ProjectOnPlane(eye.forward, Vector3.up).normalized * 4.5f;
                who.Teleport(new Vector3(ahead.x, who.transform.position.y, ahead.z));
                yield return null;
                who.AbilitySystem.BindHero("zack");
                who.Intent.Parked = false;
                who.Intent.Set(Verb.Skill1, true);
                yield return new WaitForSeconds(.1f);
                who.Intent.Set(Verb.Skill1, false);
                float until = Time.unscaledTime + 2;
                while (seen == 0 && Time.unscaledTime < until) yield return null;
                yield return null;
                Assert.AreEqual(1, seen, "An accepted cast is presented exactly once on this peer.");
                Assert.AreSame(who, caster); Assert.IsFalse(ultimate);

                var callouts = GameObject.Find("~CastCallouts");
                Assert.IsNotNull(callouts, "The cast pops a callout over the caster.");
                var icon = AbilityIcons.For(cast.Glyph);
                Assert.IsTrue(callouts.GetComponentsInChildren<Image>().Any(i => i.name == "CastCalloutIcon" && i.sprite == icon),
                    "The callout shows the ability's own icon.");
                yield return new WaitForSecondsRealtime(.2f);
                // Over the caster on the real screen. Measured live, because the review capture re-renders the canvas
                // at its own resolution and is not where the screen draws it.
                var pop = callouts.GetComponentsInChildren<RectTransform>().First(r => r.name == "CastCallout" && r.gameObject.activeInHierarchy);
                Vector2 popOnScreen = RectTransformUtility.WorldToScreenPoint(null, pop.position);
                var capsule = who.GetComponent<CharacterController>();
                Vector3 casterOnScreen = Camera.main.WorldToScreenPoint(who.transform.position + Vector3.up * (capsule != null ? capsule.height : 1.7f));
                Debug.Log("[CastCallout] pop " + popOnScreen + " caster " + casterOnScreen + " screen " + Screen.width + "x" + Screen.height);
                Assert.Less(Mathf.Abs(popOnScreen.x - casterOnScreen.x), Screen.width * .04f, "The callout stands over its caster.");
                Assert.That(popOnScreen.y - casterOnScreen.y, Is.InRange(-Screen.height * .02f, Screen.height * .12f), "Just above the caster's head.");
                yield return TumpUiCapture.Capture("CastCallout-zack-sprint", callouts.GetComponent<Canvas>(), 1280, 720, false, true);

                Assert.AreEqual(rows + 1, feed.Count, "One feed row per accepted cast.");
                Assert.IsTrue(feed.GetComponentsInChildren<Image>().Any(i => i.name == "EventAbility" && i.enabled && i.sprite == icon),
                    "The feed row carries the ability's icon.");
                var hud = Object.FindAnyObjectByType<TumpMatchReadout>();
                yield return TumpUiCapture.Capture("CastFeed-zack-sprint", hud.Canvas, 1280, 720, false, true);
            }
            finally { HeroAbilitySystem.CastPresented -= note; }
        }
    }
}
