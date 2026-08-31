using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The boot sign-in screen built the way the GAME builds it: nested inside another canvas.
    ///
    /// ⚠️⚠️ EVERY OTHER PROBE OF THIS SCREEN BUILDS IT ON A BARE `GameObject`, WHICH MAKES ITS
    /// CANVAS A ROOT CANVAS, AND THAT IS WHY THEY ARE ALL GREEN WHILE THE BUILD IS BROKEN.
    /// 🧑, opening the 2026-08-31 player: *"wtf is thhis shhit"*, with the account form floating
    /// over a fully lit main menu, no key art and the column on the wrong side.
    /// `Logs/ui/09-signin-at-boot-windowed.png`, taken by `PlayerHubLayoutProbe` minutes earlier,
    /// shows the screen **correct**: wood column on the left, cast filling the right.
    ///
    /// **The two pictures are of the same method. The only difference is the parent.**
    /// `PlayerHubLayoutProbe.Boot` does `_host = new GameObject("HubProbeHost")` and adds the
    /// nameplate to it, so `MenuKit.BuildCanvas` makes a canvas with no `Canvas` above it. In the
    /// game, `ConvertedMainMenu` installs the nameplate **on its own GameObject**, which lives
    /// inside `MainMenuCanvas`.
    ///
    /// ⚠️⚠️ AND A NESTED CANVAS IGNORES ITS OWN `CanvasScaler`. Unity resolves scale on the ROOT
    /// canvas only: a nested one inherits the root's `scaleFactor` whatever its own scaler says.
    /// `MenuKit.BuildCanvas` adds a `CanvasScaler`, sets `referenceResolution` to 1920x1080,
    /// matches on height and calls `AspectSafeCanvas.Apply`, **and all four of those are inert on
    /// a nested canvas.** Every offset, every column width and every image fit in `SignInScreen`
    /// is then computed in the wrong unit space.
    ///
    /// ⚠️ `docs/TODO.md` § 99 IS THE SAME TRAP ONE PROPERTY OVER AND IT WAS ONLY HALF FIXED. That
    /// entry records `sortingOrder` being silently ignored on a nested canvas, and the fix was
    /// `overrideSorting = true`. **Nobody asked what else a nested canvas ignores.**
    ///
    /// This probe exists to make that visible. It does not fix it: the fix is a change to how
    /// every code-built takeover screen is parented, it touches the hub, the sign-in screen, the
    /// nameplate and the character creator at once, and it is `docs/TODO.md` § 111.2 with the two
    /// candidate approaches written out.
    /// </summary>
    public class NestedCanvasProbe
    {
        private const string ShotDir = "Logs/ui";

        private GameObject _host;
        private Camera _camera;
        private RenderTexture _target;
        private bool _savedBooted;
        private bool _savedChoice;

        [SetUp]
        public void Remember()
        {
            _savedBooted = SceneFlow.BootedThroughSplash;

            var settings = Settings.SettingsStore.Current;
            _savedChoice = settings != null && settings.AccountChoiceMade;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            SceneFlow.BootedThroughSplash = _savedBooted;

            var settings = Settings.SettingsStore.Current;
            if (settings != null) settings.AccountChoiceMade = _savedChoice;

            if (_camera != null) _camera.targetTexture = null;
            if (_target != null) _target.Release();
            if (_host != null) Object.Destroy(_host);

            yield return null;
        }

        /// <summary>
        /// ⚠️ 1366x768, WHICH IS THE SHAPE HE ACTUALLY PLAYS AT. `CLAUDE.md` § 6.2b row 3:
        /// `Fullscreen` is false in his `settings.json`, and the screenshot he sent is that shape.
        /// </summary>
        [UnityTest]
        public IEnumerator TheBootScreenNestedInsideACanvasIsTheOneHeOpened()
        {
            var load = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");
            for (int i = 0; i < 30; i++) yield return null;

            foreach (var plate in Object.FindObjectsByType<PlayerNameplate>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(plate.gameObject);

            yield return null;

            _host = new GameObject("NestedProbeHost");

            // ⚠️⚠️ THE PARENT IS A CANVAS, WHICH IS THE WHOLE EXPERIMENT. This is what
            // `ConvertedMainMenu` is: the nameplate goes on a GameObject that already sits inside
            // `MainMenuCanvas`, so every canvas built under it is a NESTED canvas.
            var outer = new GameObject("OuterCanvas", typeof(RectTransform));
            outer.transform.SetParent(_host.transform, false);

            var outerCanvas = outer.AddComponent<Canvas>();
            outerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var outerScaler = outer.AddComponent<CanvasScaler>();
            outerScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            outerScaler.referenceResolution = new Vector2(1920, 1080);
            outerScaler.matchWidthOrHeight = 1.0f;
            outer.AddComponent<GraphicRaycaster>();

            var owner = new GameObject("MenuOwner", typeof(RectTransform));
            owner.transform.SetParent(outer.transform, false);

            var settings = Settings.SettingsStore.Current;
            if (settings != null) settings.AccountChoiceMade = true;

            var nameplate = owner.AddComponent<PlayerNameplate>();
            nameplate.Install();
            yield return null;

            var hub = owner.GetComponent<PlayerHub>();
            Assert.IsNotNull(hub, "the nameplate did not install a hub");

            var signIn = owner.GetComponent<SignInScreen>();
            Assert.IsNotNull(signIn, "the hub did not install a sign-in screen");

            SceneFlow.BootedThroughSplash = true;
            signIn.OpenAtBoot();
            yield return null;

            _camera = Camera.main;
            if (_camera == null)
                foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude,
                                                                     FindObjectsSortMode.None))
                {
                    if (cam == null || cam.targetTexture != null) continue;
                    _camera = cam;
                    break;
                }

            Assert.IsNotNull(_camera, "no camera in MainMenu to render through");

            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                                                               FindObjectsSortMode.None))
            {
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = _camera;
                c.planeDistance = _camera.nearClipPlane + 0.01f;
            }

            _target = new RenderTexture(1366, 768, 24, RenderTextureFormat.ARGB32);
            _camera.targetTexture = _target;

            for (int i = 0; i < 4; i++) yield return null;

            yield return Shoot("30-signin-at-boot-detached_v1");

            // ⚠️⚠️ THE ASSERTION IS ON THE CANVAS, NOT ON THE PICTURE, so this reports the CAUSE
            // rather than a difference in pixels somebody has to interpret.
            //
            // ⚠️⚠️ AND IT IS THE OTHER WAY ROUND FROM HOW THIS PROBE WAS FIRST WRITTEN. The first
            // version asserted that the canvas WAS nested, because it was written to reproduce the
            // bug: it did, in one picture, and `Logs/ui/30-signin-at-boot-NESTED_v1.png` from that
            // run is the form floating on the wrong side with no column and no key art, which is
            // 🧑's screenshot exactly. `MenuKit.BuildCanvas` detaches now, so the same assertion
            // inverted is the regression guard.
            var signInCanvas = Find("SignInCanvas");
            Assert.IsNotNull(signInCanvas, "the sign-in screen built no canvas");

            Assert.IsTrue(signInCanvas.isRootCanvas,
                "SignInCanvas is nested inside another canvas, so **its own CanvasScaler is being "
                + "ignored**: Unity resolves scale on the root canvas only. Every offset, column "
                + "width and image fit in SignInScreen is then computed in the wrong unit space, "
                + "and the screen draws as a floating form with no wood column and no key art. "
                + "That is the build 🧑 opened on 2026-08-31. docs/TODO.md 111.2, and "
                + "MenuKit.BuildCanvas is where the detach lives.");

            var hubCanvas = Find("PlayerHubCanvas");
            if (hubCanvas != null)
                Assert.IsTrue(hubCanvas.isRootCanvas,
                    "PlayerHubCanvas is nested, so the hub is laid out at the wrong scale too. "
                    + "Same cause, same fix.");

            Debug.Log($"[Nested] SignInCanvas scaleFactor {signInCanvas.scaleFactor:F3}, "
                      + $"isRootCanvas {signInCanvas.isRootCanvas}, "
                      + $"overrideSorting {signInCanvas.overrideSorting}, "
                      + $"sortingOrder {signInCanvas.sortingOrder}.");
        }

        private static Canvas Find(string name)
        {
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                                                               FindObjectsSortMode.None))
                if (c.name == name) return c;

            return null;
        }

        private IEnumerator Shoot(string name)
        {
            yield return null;

            _camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = _target;

            var shot = new Texture2D(_target.width, _target.height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0);
            shot.Apply();

            RenderTexture.active = previous;

            Directory.CreateDirectory(ShotDir);
            File.WriteAllBytes(Path.Combine(ShotDir, name + ".png"), shot.EncodeToPNG());
            Object.Destroy(shot);

            Debug.Log($"[Nested] wrote {ShotDir}/{name}.png");
        }
    }
}
