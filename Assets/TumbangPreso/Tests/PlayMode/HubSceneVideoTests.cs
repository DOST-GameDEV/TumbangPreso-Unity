using System.Collections;
using NUnit.Framework;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Video;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The owner's animated HOME scene (`docs/reports/home-scene/README.md`) plays in the hub's
    /// reserved background layer, advances, is photographed at the reference shape and the owner's
    /// own window, and PAUSES when a screen covers HOME.
    ///
    /// ⚠️ THIS ASKS THE DECODER, NOT THE HIERARCHY. A `HubSceneVideo` that exists but never prepared
    /// would sit showing its poster and pass any test that only looked for the component, so the
    /// frame counter has to move between two reads a second apart.
    /// </summary>
    public sealed class HubSceneVideoTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest, Timeout(240000)]
        public IEnumerator HomePlaysTheAnimatedSceneAndPausesBehindOtherScreens()
        {
            Assert.IsNotNull(Resources.Load<VideoClip>(HubSceneVideo.ClipPath), "The loop is not in Resources: " + HubSceneVideo.ClipPath);
            Assert.IsNotNull(Resources.Load<Texture2D>(HubSceneVideo.PosterPath), "The poster is not in Resources: " + HubSceneVideo.PosterPath);

            yield return HubFlowTests.OpenHome();
            var hub = TumpHub.Current;
            var scene = hub.Scene.GetComponentInChildren<HubSceneVideo>(true);
            Assert.IsNotNull(scene, "HOME has no animated scene in its reserved layer.");

            float until = Time.realtimeSinceStartup + 30;
            while (Time.realtimeSinceStartup < until && !(scene.Prepared && scene.Player != null && scene.Player.isPlaying && scene.Player.frame > 2))
                yield return null;
            Assert.IsTrue(scene.Prepared, "The loop never prepared.");
            Assert.IsTrue(scene.ShowingVideo, "The layer is still showing the poster, not the video.");
            long first = scene.Player.frame;
            yield return new WaitForSecondsRealtime(1.0f);
            Assert.Greater(scene.Player.frame, first, "The loop is not advancing.");

            yield return TumpUiCapture.Capture("Hub-HomeScene-1920x1080", hub.Canvas, 1920, 1080, checkPalette: false);
            yield return TumpUiCapture.Capture("Hub-HomeScene-1600x680", hub.Canvas, 1600, 680, checkPalette: false);

            yield return HubFlowTests.Press("HeroButton");
            Assert.IsInstanceOf<HubHero>(hub.Top);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsFalse(scene.Player.isPlaying, "The loop kept playing behind HERO.");
            hub.Back();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(scene.Player.isPlaying, "The loop did not resume on returning HOME.");
        }
    }
}
