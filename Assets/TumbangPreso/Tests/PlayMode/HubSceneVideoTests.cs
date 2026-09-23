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
    /// own window, and HIDES and pauses under every other hub screen, so the lobby shows the map.
    ///
    /// ⚠️ THIS ASKS THE DECODER, NOT THE HIERARCHY. A `HubSceneVideo` that exists but never prepared
    /// would sit showing its poster and pass any test that only looked for the component, so the
    /// frame counter has to move between two reads a second apart.
    /// </summary>
    public sealed class HubSceneVideoTests
    {
        [UnitySetUp] public IEnumerator Before() { HubSceneVideo.ForcedHero = null; yield return PlayModeWorld.Reset(); }
        [UnityTearDown] public IEnumerator After() { HubSceneVideo.ForcedHero = null; yield return PlayModeWorld.Reset(); }

        /// <summary>
        /// ⚠️ EVERY HERO THE PICK CAN LAND ON SHIPS BOTH HALVES OF ITS PAIR. A hero listed with a clip and
        /// no poster would fall back to Zack's pair; with neither, the pick would quietly never choose
        /// it and nobody would notice that loop was not shipping.
        /// </summary>
        [Test]
        public void EveryListedHeroShipsItsLoopAndPoster()
        {
            foreach (var hero in HubSceneVideo.Heroes)
            {
                Assert.IsNotNull(Resources.Load<VideoClip>(HubSceneVideo.ClipPathFor(hero)), "No HOME loop for " + hero + ": " + HubSceneVideo.ClipPathFor(hero));
                Assert.IsNotNull(Resources.Load<Texture2D>(HubSceneVideo.PosterPathFor(hero)), "No HOME poster for " + hero + ": " + HubSceneVideo.PosterPathFor(hero));
            }
        }

        /// <summary>
        /// The pick is random over every hero that has a loop (🧑 2026-09-24: *"make it random which one
        /// shows up"*), skips a hero whose clip is missing, and falls back to Zack when none resolves.
        /// </summary>
        [Test]
        public void ThePickCanLandOnEveryHeroAndFallsBackToZack()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < HubSceneVideo.Heroes.Length; i++)
            {
                int k = i;
                seen.Add(HubSceneVideo.Pick(n => k, _ => true));
            }
            CollectionAssert.AreEquivalent(HubSceneVideo.Heroes, seen, "Some hero's loop can never be picked.");
            Assert.AreEqual("phaister", HubSceneVideo.Pick(n => 0, h => h == "phaister"), "A hero with no clip was still a candidate.");
            Assert.AreEqual("zack", HubSceneVideo.Pick(n => 0, _ => false), "With no clip anywhere the pick did not fall back to Zack.");
        }

        /// <summary>Phaister's loop, picked, prepares and advances in the real hub exactly as Zack's does.</summary>
        [UnityTest, Timeout(240000)]
        public IEnumerator PhaistersLoopPlaysWhenPicked()
        {
            HubSceneVideo.ForcedHero = "phaister";
            yield return HubFlowTests.OpenHome();
            var hub = TumpHub.Current;
            var scene = hub.Scene.GetComponentInChildren<HubSceneVideo>(true);
            Assert.IsNotNull(scene, "HOME has no animated scene in its reserved layer.");
            Assert.AreEqual("phaister", scene.Hero, "The forced pick was not honoured.");
            float until = Time.realtimeSinceStartup + 30;
            while (Time.realtimeSinceStartup < until && !(scene.Prepared && scene.Player != null && scene.Player.isPlaying && scene.Player.frame > 2))
                yield return null;
            Assert.IsTrue(scene.Prepared, "Phaister's loop never prepared.");
            Assert.IsTrue(scene.ShowingVideo, "The layer is still showing the poster, not the video.");
            long first = scene.Player.frame;
            yield return new WaitForSecondsRealtime(1.0f);
            Assert.Greater(scene.Player.frame, first, "Phaister's loop is not advancing.");
            yield return TumpUiCapture.Capture("Hub-HomeScene-Phaister-1920x1080", hub.Canvas, 1920, 1080, checkPalette: false);
            yield return TumpUiCapture.Capture("Hub-HomeScene-Phaister-1600x680", hub.Canvas, 1600, 680, checkPalette: false);
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator HomePlaysTheAnimatedSceneAndHidesItUnderOtherScreens()
        {
            // Zack's, forced, so these captures stay comparable with every run before the random pick.
            HubSceneVideo.ForcedHero = "zack";
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
            Assert.IsFalse(scene.GetComponent<UnityEngine.UI.RawImage>().enabled, "The HOME scene still covers the map behind HERO.");
            hub.Back();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(scene.Player.isPlaying, "The loop did not resume on returning HOME.");
        }
            /// <summary>
        /// ⚠️ The BH Studios mark on the loading band was drawn vertically stretched (🧑 2026-09-23).
        /// The logo is a Default texture, whose import rounded a 445x370 image up to a 512x512 power
        /// of two, and `HubLoading` builds its sprite from the texture's size, so `preserveAspect`
        /// faithfully kept a SQUARE. `nPOTScale: 0` in its meta keeps the real size; this holds it.
        /// </summary>
        [Test]
        public void TheStudioMarkLoadsAtItsOwnShape()
        {
            var logo = Resources.Load<Texture2D>("UI/brand/bh_studios_logo");
            Assert.IsNotNull(logo, "The studio mark is not in Resources.");
            Assert.AreEqual(445, logo.width, "The studio mark was rescaled on import.");
            Assert.AreEqual(370, logo.height, "The studio mark was rescaled on import.");
        }
    }
}
