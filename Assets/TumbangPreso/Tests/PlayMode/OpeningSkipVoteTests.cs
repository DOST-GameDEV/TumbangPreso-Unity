using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class OpeningSkipVoteTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest, Timeout(180000)]
        public IEnumerator BanaueSkipUsesOneSmallControlAndRestoresTheHud()
        {
            bool cinematic = SettingsStore.Current.CinematicCameraMotion, reduced = SettingsStore.Current.ReducedUiMotion;
            try
            {
                SettingsStore.Current.CinematicCameraMotion = true; SettingsStore.Current.ReducedUiMotion = false;
                SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
                yield return SceneManager.LoadSceneAsync(SceneFlow.Payo);
                float until = Time.realtimeSinceStartup + 60;
                while (!OpeningSkipVote.Showing && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsTrue(MatchArrivalPresentation.Rolling, "The opening reached the screen.");
                Assert.IsTrue(OpeningSkipVote.Showing, "The skip button is up while it plays.");
                yield return new WaitForSecondsRealtime(1.6f);

                var canvas = GameObject.Find("OpeningSkipCanvas").GetComponent<Canvas>();
                Assert.IsTrue(canvas.isActiveAndEnabled && canvas.enabled, "Its canvas survives the film hiding the HUD.");
                Assert.GreaterOrEqual(canvas.sortingOrder, 150, "It sits above the curtain.");
                var vote = Object.FindFirstObjectByType<OpeningSkipVote>();
                StringAssert.Contains(">>", vote.Words);
                var control = GameObject.Find("OpeningSkip").GetComponent<RectTransform>();
                Assert.LessOrEqual(control.rect.width, 112);
                Assert.IsNotNull(control.Find("Votes"));
                var filmOverlay = GameObject.Find("Map film overlay");
                Assert.IsNotNull(filmOverlay);
                Assert.AreEqual(0, filmOverlay.transform.Find("Skip").GetComponent<CanvasGroup>().alpha,
                    "The old local skip ring must not duplicate the shared vote.");
                yield return GameplayShots.Render(Camera.main, "banaue-skip", true,
                    outDir: "Logs/multiplayer-fixes-20261104");

                vote.Cast();
                yield return null; yield return null; yield return null;
                Assert.IsFalse(MatchArrivalPresentation.Rolling, "A solo press ends the opening.");
                Assert.IsFalse(OpeningSkipVote.Showing, "And the button goes with it.");
                Assert.IsFalse(Map.MatchOpening.HidesUi, "The HUD comes back.");
                until = Time.realtimeSinceStartup + 15;
                while (GameServices.Round?.RoundActive != true && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsTrue(GameServices.Round.RoundActive, "Skipping the opening still starts the round.");
                Assert.IsTrue(Hud.Instance.GetComponent<TumpMatchReadout>().Canvas.isActiveAndEnabled,
                    "Banaue's live readout must be visible after the opening.");
            }
            finally
            {
                SettingsStore.Current.CinematicCameraMotion = cinematic; SettingsStore.Current.ReducedUiMotion = reduced;
            }
        }
    }
}
