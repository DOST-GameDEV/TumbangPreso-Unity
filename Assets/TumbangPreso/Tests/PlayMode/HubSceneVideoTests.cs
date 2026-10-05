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

        [UnityTest, Timeout(30000)]
        public IEnumerator OpaqueHomeMediaSuspendsTheHiddenCourtAndRestoresItsFallback()
        {
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            GameObject owner = null, previewRoot = null;
            try
            {
                Settings.SettingsStore.Current.ReducedUiMotion = true;
                HubSceneVideo.ForcedHero = "zack";
                owner = new GameObject("Home render ownership");
                var canvas = UI.OwnerUiLayout.Canvas(owner.transform, "HomeRenderCanvas", 100);
                var scene = UI.OwnerUiLayout.Rect(canvas.transform, "Scene"); UI.OwnerUiLayout.Fill(scene);
                previewRoot = new GameObject("Covered court", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
                var preview = previewRoot.AddComponent<UI.MapPreviewSurface>(); preview.enabled = false;
                var video = HubSceneVideo.Install(scene, preview);
                var image = video.GetComponent<UnityEngine.UI.RawImage>();
                const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var update = typeof(HubSceneVideo).GetMethod("Update", flags);
                yield return null; Canvas.ForceUpdateCanvases(); update.Invoke(video, null);
                Assert.IsNull(video.Player, "Reduced motion opened a decoder.");
                Assert.IsTrue(image.enabled); Assert.IsNotNull(image.texture);
                typeof(UI.MapPreviewSurface).GetMethod("EnsureCamera", flags).Invoke(preview, null);
                Assert.IsFalse(preview.Camera.enabled, "A late-created camera ignored its covered state.");
                Assert.IsFalse(previewRoot.GetComponent<UnityEngine.UI.RawImage>().enabled);
                var camera = preview.Camera; var target = camera.targetTexture;
                var posterField = typeof(HubSceneVideo).GetField("_poster", flags);
                var poster = posterField.GetValue(video);
                posterField.SetValue(video, null); update.Invoke(video, null);
                Assert.IsFalse(image.enabled); Assert.IsTrue(camera.enabled, "Missing media did not restore the live court.");
                posterField.SetValue(video, poster); image.color = new Color(1, 1, 1, .5f); update.Invoke(video, null);
                Assert.IsTrue(camera.enabled, "Translucent media was treated as complete coverage.");
                image.color = Color.white; update.Invoke(video, null);
                Assert.IsFalse(camera.enabled);
                video.gameObject.SetActive(false);
                Assert.IsTrue(camera.enabled, "Disabling the media kept its fallback camera hidden.");
                Assert.AreSame(camera, preview.Camera); Assert.AreSame(target, camera.targetTexture);
                video.gameObject.SetActive(true); update.Invoke(video, null);
                Assert.IsFalse(camera.enabled);
                Object.Destroy(video.gameObject); yield return null;
                Assert.IsTrue(camera.enabled, "Destroying the media did not release court visibility.");
            }
            finally
            {
                if (owner != null) Object.Destroy(owner);
                if (previewRoot != null) Object.Destroy(previewRoot);
                Settings.SettingsStore.Current.ReducedUiMotion = reduced;
            }
        }

        [UnityTest, Timeout(45000)]
        public IEnumerator BootWarmupRetainsOnePausedDecodedFrameAndHandsTheSamePlayerToHome()
        {
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            GameObject parent = null;
            try
            {
                Settings.SettingsStore.Current.ReducedUiMotion = false;
                HubSceneVideo.ForcedHero = "zack";
                float progress = 0;
                yield return HubSceneVideo.Warmup(done => { Assert.GreaterOrEqual(done, progress); progress = done; });
                Assert.AreEqual(1f, progress);
                var prepared = Object.FindAnyObjectByType<HubSceneVideo>();
                Assert.IsNotNull(prepared); Assert.IsTrue(prepared.Prepared); Assert.IsTrue(prepared.FirstFrameReady);
                var player = prepared.Player; var target = player.targetTexture;
                Assert.IsNotNull(target); Assert.IsFalse(player.isPlaying);
                Assert.IsFalse(player.sendFrameReadyEvents, "Warmup left a per-frame callback running.");
                Assert.IsFalse(prepared.GetComponent<UnityEngine.UI.RawImage>().enabled);
                parent = new GameObject("HomeWarmupAdoption", typeof(RectTransform));
                var adopted = HubSceneVideo.Install((RectTransform)parent.transform);
                Assert.AreSame(prepared, adopted); Assert.AreSame(player, adopted.Player);
                Assert.AreSame(target, adopted.Player.targetTexture);
                yield return null;
                Assert.IsTrue(adopted.ShowingVideo);
                Assert.IsTrue(player.isPlaying);
                Object.Destroy(parent); parent = null;
                yield return null;
                Assert.IsTrue(player == null); Assert.IsTrue(target == null);
            }
            finally
            {
                if (parent != null) Object.Destroy(parent);
                Settings.SettingsStore.Current.ReducedUiMotion = reduced;
            }
        }

        [UnityTest, Timeout(45000)]
        public IEnumerator FailedBootWarmupGetsFreshPlaybackWhenHomeOpens()
        {
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            GameObject parent = null;
            try
            {
                Settings.SettingsStore.Current.ReducedUiMotion = false;
                HubSceneVideo.ForcedHero = "zack";
                yield return HubSceneVideo.Warmup();
                var warm = Object.FindAnyObjectByType<HubSceneVideo>();
                Assert.IsNotNull(warm); Assert.IsTrue(warm.FirstFrameReady);
                var failedPlayer = warm.Player;
                var failedTarget = failedPlayer.targetTexture;
                LogAssert.Expect(LogType.Warning, "[HubSceneVideo] Controlled boot decoder failure - showing the poster instead.");
                typeof(HubSceneVideo).GetMethod("OnError",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(warm, new object[] { failedPlayer, "Controlled boot decoder failure" });
                Assert.IsFalse(warm.Prepared); Assert.IsFalse(warm.FirstFrameReady);
                parent = new GameObject("FailedWarmupHome", typeof(RectTransform));
                var home = HubSceneVideo.Install((RectTransform)parent.transform);
                Assert.AreNotSame(warm, home, "Home adopted a failed preload and cannot restart its animation.");
                Assert.IsNotNull(home.GetComponent<UnityEngine.UI.RawImage>().texture,
                    "Home must keep its poster while a fresh decoder prepares.");
                float until = Time.realtimeSinceStartup + 15;
                while (!home.FirstFrameReady && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsTrue(home.FirstFrameReady, "Fresh Home playback never decoded a frame.");
                yield return null;
                Assert.IsTrue(home.ShowingVideo); Assert.IsTrue(home.Player.isPlaying);
                long frame = home.Player.frame;
                yield return new WaitForSecondsRealtime(.6f);
                Assert.AreNotEqual(frame, home.Player.frame, "Home remained a static poster/frame.");
                Assert.IsTrue(failedPlayer == null); Assert.IsTrue(failedTarget == null,
                    "Failed warmup resources survived adoption.");
            }
            finally
            {
                if (parent != null) Object.Destroy(parent);
                Settings.SettingsStore.Current.ReducedUiMotion = reduced;
            }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ReducedMotionWarmupUsesThePosterWithoutOpeningADecoder()
        {
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            GameObject parent = null;
            try
            {
                Settings.SettingsStore.Current.ReducedUiMotion = true;
                HubSceneVideo.ForcedHero = "phaister";
                yield return HubSceneVideo.Warmup();
                parent = new GameObject("ReducedHomeAdoption", typeof(RectTransform));
                var adopted = HubSceneVideo.Install((RectTransform)parent.transform);
                yield return null;
                Assert.IsNull(adopted.Player); Assert.IsFalse(adopted.ShowingVideo);
                Assert.IsNotNull(adopted.GetComponent<UnityEngine.UI.RawImage>().texture);
            }
            finally
            {
                if (parent != null) Object.Destroy(parent);
                Settings.SettingsStore.Current.ReducedUiMotion = reduced;
            }
        }

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
