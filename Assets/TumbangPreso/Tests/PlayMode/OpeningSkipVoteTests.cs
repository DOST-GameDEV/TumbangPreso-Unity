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
        public IEnumerator SkipButtonShowsOnTheOpeningAndAPressEndsIt()
        {
            bool cinematic = SettingsStore.Current.CinematicCameraMotion, reduced = SettingsStore.Current.ReducedUiMotion;
            try
            {
                SettingsStore.Current.CinematicCameraMotion = true; SettingsStore.Current.ReducedUiMotion = false;
                SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
                yield return SceneManager.LoadSceneAsync("Eskinita");
                float until = Time.realtimeSinceStartup + 60;
                while (!OpeningSkipVote.Showing && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsTrue(MatchArrivalPresentation.Rolling, "The opening reached the screen.");
                Assert.IsTrue(OpeningSkipVote.Showing, "The skip button is up while it plays.");
                yield return null;

                var canvas = GameObject.Find("OpeningSkipCanvas").GetComponent<Canvas>();
                Assert.IsTrue(canvas.isActiveAndEnabled && canvas.enabled, "Its canvas survives the film hiding the HUD.");
                Assert.GreaterOrEqual(canvas.sortingOrder, 150, "It sits above the curtain.");
                var vote = Object.FindFirstObjectByType<OpeningSkipVote>();
                StringAssert.Contains("SKIP", vote.Words);

                vote.Cast();
                yield return null; yield return null; yield return null;
                Assert.IsFalse(MatchArrivalPresentation.Rolling, "A solo press ends the opening.");
                Assert.IsFalse(OpeningSkipVote.Showing, "And the button goes with it.");
                Assert.IsFalse(Map.MatchOpening.HidesUi, "The HUD comes back.");
            }
            finally
            {
                SettingsStore.Current.CinematicCameraMotion = cinematic; SettingsStore.Current.ReducedUiMotion = reduced;
            }
        }
    }
}
