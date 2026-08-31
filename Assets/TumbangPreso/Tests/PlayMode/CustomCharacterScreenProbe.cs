using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// MAKE YOUR OWN, driven for real, measured at nine resolutions and photographed at four.
    ///
    /// ⚠️⚠️ THE SCREEN THIS REPLACES HAD NO PROBE BECAUSE IT HAD NO SCREEN.
    /// `CustomCharacterCreator` was 388 lines of setters and events that drew nothing
    /// (`docs/TODO.md` § 108.1), and the two screens beside it were built at `sortingOrder` 95
    /// under a 93 per cent scrim at 500 (§ 108.2). **All three passed every gate in the
    /// repository**, because nothing in the repository asks whether a screen can be reached.
    ///
    /// ⚠️⚠️ SO THIS PROBE ASKS THE ONE QUESTION THE OTHERS DO NOT: it opens the screen through
    /// the same `Ensure().Open()` character select calls, and then asserts there are rows in the
    /// list and that the model stage exists. `PlayerHubLayoutProbe`'s own header records why
    /// counting labels is not enough: *"The header, the four tab buttons and the footer are all
    /// labels, so a tab whose entire content failed to render still cleared 'some labels were
    /// measured'."*
    ///
    /// ⚠️⚠️ EVERY SECTION, NOT THE ONE THAT OPENS FIRST. `CLAUDE.md` § 6.2b row 1: *"EVERY STATE,
    /// not the one you built first. A screen with a mode has two layouts and you have looked at
    /// one."* This screen has six, and FACE is the only one anybody would see by accident.
    ///
    /// ⚠️ THE PICTURES INCLUDE 1366x768, WHICH IS THE SHORT WIDE SHAPE `CLAUDE.md` § 6.2b ROW 3
    /// IS ABOUT. `Fullscreen` is false in his `settings.json`, and *"a screen that only exists at
    /// 16:9 is a screen nobody in this room has seen"*.
    /// </summary>
    public class CustomCharacterScreenProbe
    {
        private const string ShotDir = "Logs/ui";
        private const string OutDir = "Logs";

        /// <summary>⚠️ THE SAME NINE `AspectRatioProbes`, `HudOverflowProbe` AND
        /// `PlayerHubLayoutProbe` USE. A tenth list would be a tenth screen.</summary>
        private static readonly (int W, int H, string Name)[] Resolutions =
        {
            (1280,  720, "16:9 720p"),
            (1600,  900, "16:9 900p"),
            (1920, 1080, "16:9 1080p"),
            (2560, 1440, "16:9 1440p"),
            (1366,  768, "16:9 laptop"),
            (1920, 1200, "16:10"),
            (2560, 1080, "21:9"),
            (3440, 1440, "21:9 1440p"),
            (1024,  768, "4:3"),
        };

        private GameObject _host;
        private Camera _camera;
        private RenderTexture _target;
        private readonly List<Canvas> _canvases = new List<Canvas>();

        /// <summary>
        /// ⚠️⚠️ THE REAL `settings.json` IS RESTORED, BECAUSE THIS SCREEN WRITES TO IT. The editor
        /// and the built player share `Application.persistentDataPath`, so `KEEP AND USE` here
        /// would overwrite his actual saved characters. `PlayerHubLayoutProbe` carries the same
        /// note for the same reason and `CloudEndpointActionProbe`'s header is the network version
        /// of it.
        /// </summary>
        [SetUp]
        public void RememberTheSavedCharacters()
        {
            var settings = Settings.SettingsStore.Current;
            _savedWires = settings?.CustomCharacterWires == null
                ? new List<string>()
                : new List<string>(settings.CustomCharacterWires);
            _savedActive = settings?.ActiveCustomSlot ?? 0;
            _savedInUse = settings != null && settings.UseCustomCharacter;
        }

        private List<string> _savedWires;
        private int _savedActive;
        private bool _savedInUse;

        [UnityTearDown]
        public IEnumerator Restore()
        {
            var settings = Settings.SettingsStore.Current;
            if (settings != null)
            {
                settings.CustomCharacterWires = _savedWires;
                settings.ActiveCustomSlot = _savedActive;
                settings.UseCustomCharacter = _savedInUse;
            }

            CustomCharacterStore.Reload();

            foreach (var c in _canvases)
                if (c != null) c.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvases.Clear();

            if (_camera != null) _camera.targetTexture = null;
            if (_target != null) _target.Release();
            if (_host != null) Object.Destroy(_host);

            yield return null;
        }

        /// <summary>
        /// Every section of the screen, at every shipped resolution, with the pictures.
        /// </summary>
        [UnityTest]
        public IEnumerator EverySectionFitsItsBoxAndDrawsItsRows()
        {
            var report = new StringBuilder();
            yield return Boot(report);

            var screen = CustomCharacterScreen.Ensure();
            screen.Open();
            yield return null;
            yield return null;

            Assert.IsTrue(screen.IsOpen, "Open() did not leave the screen open");

            var root = Root("CustomCharacterCanvas");
            Assert.IsNotNull(root, "the screen built no canvas called CustomCharacterCanvas");

            // ⚠️⚠️ THE STAGE IS ASSERTED SEPARATELY FROM THE ROWS, BECAUSE THE MODEL IS THE ONE
            // THING ON THIS SCREEN (§ 0.5b question 1) AND A LIST OF NAMES WITHOUT IT IS A
            // DIFFERENT, WORSE SCREEN. `ModelPreview` is what makes a choice visible; the version
            // this replaces computed a palette and never handed it to one.
            var preview = root.GetComponentInChildren<ModelPreview>(true);
            Assert.IsNotNull(preview, "the screen built no ModelPreview, so nothing shows the "
                + "character the player is making. docs/TODO.md § 108.1.");

            foreach (var (w, h, name) in Resolutions)
            {
                yield return Resize(w, h);

                for (int section = 0; section < 6; section++)
                {
                    PressSection(section);
                    yield return null;
                    yield return null;

                    int rows = Rows(root);
                    Assert.Greater(rows, 0,
                        $"{name}: section {section} drew no rows at all. The chrome can draw "
                        + "perfectly with nothing in it (PlayerHubLayoutProbe's own finding).");

                    int measured = Measure(root, name, $"creator/{section}", report);
                    Assert.Greater(measured, 0,
                        $"{name}: section {section} drew no legible labels.");
                }
            }

            Write("custom-character", report);
        }

        /// <summary>
        /// The pictures. ⚠️ **VERSIONED FILENAMES**, `CLAUDE.md` § 6.1: chat clients cache by
        /// name, so overwriting a render leaves the previous one on screen and the whole review
        /// happens against an image that is no longer on disk.
        /// </summary>
        [UnityTest]
        public IEnumerator PhotographTheScreen()
        {
            var report = new StringBuilder();
            yield return Boot(report);

            var screen = CustomCharacterScreen.Ensure();
            screen.Open();
            yield return null;

            // ⚠️ 1366x768 IS IN THIS LIST ON PURPOSE AND IT IS THE ONE THAT MATTERS.
            // `CLAUDE.md` § 6.2b row 3: he plays in a short wide window and every one of the nine
            // probe resolutions used to be taller than it, which is how a column of hard-coded Y
            // offsets collapsed into a heap in the middle of the screen and nobody saw it.
            foreach (var (w, h, tag) in new[]
                     {
                         (1920, 1080, "1080p"),
                         (1366,  768, "laptop"),
                         (2560, 1080, "ultrawide"),
                         (1024,  768, "4x3"),
                     })
            {
                yield return Resize(w, h);

                PressSection(0);
                yield return null;
                yield return null;
                yield return Shoot($"20-creator-face-{tag}_v1");

                PressSection(4);
                yield return null;
                yield return null;
                yield return Shoot($"21-creator-gear-{tag}_v1");
            }

            Write("custom-character-shots", report);
        }

        // -------------------------------------------------------------------
        // § THE HARNESS
        // -------------------------------------------------------------------

        private IEnumerator Boot(StringBuilder report)
        {
            var load = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");
            for (int i = 0; i < 30; i++) yield return null;

            _host = new GameObject("CreatorProbeHost");

            _camera = Camera.main;

            if (_camera == null)
                foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude,
                                                                     FindObjectsSortMode.None))
                {
                    if (cam == null || cam.targetTexture != null) continue;
                    _camera = cam;
                    break;
                }

            if (_camera == null)
            {
                _camera = new GameObject("ProbeCamera", typeof(Camera)).GetComponent<Camera>();
                _camera.transform.SetParent(_host.transform, false);
            }

            report.AppendLine($"camera: {_camera.name}");
            yield return null;
        }

        /// <summary>
        /// ⚠️ THE CANVASES ARE COLLECTED AFTER THE SCREEN IS OPEN, NOT IN `Boot`. This screen
        /// builds its canvas lazily on the first `Open()`, so a sweep run before that finds
        /// everything except the one thing being photographed, and the capture comes back as the
        /// menu with nothing over it.
        /// </summary>
        private IEnumerator Resize(int w, int h)
        {
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                                                               FindObjectsSortMode.None))
            {
                // ⚠️⚠️ THE MENU'S OWN CANVASES ARE SWITCHED OFF, AND THE FIRST RUN OF THIS PROBE
                // IS WHY. Every overlay canvas was flipped to `ScreenSpaceCamera` at the same
                // `planeDistance`, and the first captures came back with PLAY, SETTINGS, TUTORIAL
                // and QUIT drawn straight through the creator. **In the real game that cannot
                // happen**: `MainMenu.unity` carries `m_SortingOrder: 0` and this screen is 520,
                // both overlay, so the creator is on top by 520. The bleed is an artefact of
                // co-planar ScreenSpaceCamera canvases and photographing it would send a reader
                // hunting a sorting bug that does not exist.
                //
                // ⚠️ THE 3D STREET SURVIVES, WHICH IS THE POINT. `CLAUDE.md` § 6.2b row 2: shoot
                // over the real background, never an empty scene, because every scrim and panel
                // alpha is tuned against what is behind it. The street is geometry rather than a
                // canvas, so switching the menu's UI off leaves exactly what a player sees with
                // this screen open. `PlayerHubLayoutProbe` takes the same approach.
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = _camera;

                // ⚠️ THE CREATOR SITS NEARER THE CAMERA THAN EVERYTHING ELSE, and that is a
                // property of THIS CAPTURE rather than of the game. Co-planar
                // `ScreenSpaceCamera` canvases sorted unpredictably in the first run and the
                // shots came back with PLAY, SETTINGS, TUTORIAL and QUIT drawn through the
                // screen. **In the real game that cannot happen**: `MainMenu.unity` carries
                // `m_SortingOrder: 0` and this screen is 520, both overlay. Separating the
                // planes reproduces the shipped order instead of an artefact of the harness.
                c.planeDistance = _camera.nearClipPlane
                                  + (c.name == "CustomCharacterCanvas" ? 0.01f : 0.30f);

                _canvases.Add(c);
            }

            var next = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            _camera.targetTexture = next;

            if (_target != null) _target.Release();
            _target = next;

            // Three frames: the scaler recomputes in its own Update, the layout rebuild lands the
            // frame after, and the ScrollRect's ContentSizeFitter settles on the third.
            for (int i = 0; i < 3; i++) yield return null;
        }

        /// <summary>
        /// ⚠️ THE SECTION TABS ARE PRESSED BY LABEL, THE WAY A PLAYER PRESSES THEM. A probe that
        /// reached into a private field would prove the field and not the button, and the two
        /// screens § 108.2 is about had buttons that drew perfectly and did nothing.
        /// </summary>
        private void PressSection(int index)
        {
            string[] titles = { "FACE", "HAIR", "BODY", "CLOTHES", "GEAR", "KIT" };
            string wanted = titles[Mathf.Clamp(index, 0, titles.Length - 1)];

            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include,
                                                                    FindObjectsSortMode.None))
            {
                var text = button.GetComponentInChildren<Text>(true);
                if (text != null && text.text == wanted) { button.onClick.Invoke(); return; }
            }

            Assert.Fail($"no section tab reading '{wanted}' on the creator screen");
        }

        private static Transform Root(string canvasName)
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                                                                     FindObjectsSortMode.None))
                if (canvas.name == canvasName) return canvas.transform;

            return null;
        }

        /// <summary>⚠️ ROWS IN THE LIST, NOT GRAPHICS ON THE CANVAS. `UiRows.ScrollList` names
        /// every row `Row_` and every header `Section_`, and an empty list under correct chrome is
        /// exactly what a fully transparent mask graphic produced once already.</summary>
        private static int Rows(Transform root)
        {
            int count = 0;

            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
                if (rect.name.StartsWith("Row_") || rect.name.StartsWith("Section_")) count++;

            return count;
        }

        private static int Measure(Transform root, string resolution, string screen,
                                   StringBuilder report)
        {
            int measured = 0;

            foreach (var label in root.GetComponentsInChildren<Text>(false))
            {
                if (label == null || string.IsNullOrWhiteSpace(label.text)) continue;
                if (label.color.a < 0.05f) continue;
                if (!label.isActiveAndEnabled) continue;

                float room = label.rectTransform.rect.width;
                if (room <= 1.0f) continue;

                measured++;

                Assert.GreaterOrEqual(label.fontSize, MenuKit.MinReadableUnits,
                    $"{resolution} {screen}: '{label.name}' is authored at {label.fontSize} "
                    + $"units, below the {MenuKit.MinReadableUnits}-unit floor.");

                // ⚠️ THE VERTICAL CHECK IS `docs/TODO.md` § 102.4: a wrapping hint's preferred
                // WIDTH is inside its box by definition, so the horizontal check says nothing
                // about it and every two-line hint in the game drew over the row below it while
                // two probes were green.
                if (label.horizontalOverflow == HorizontalWrapMode.Wrap)
                {
                    float tall = label.rectTransform.rect.height;
                    if (tall <= 1.0f) continue;

                    Assert.LessOrEqual(label.preferredHeight, tall + 1.0f,
                        $"{resolution} {screen}: '{label.name}' wraps to "
                        + $"{label.preferredHeight:F0} units in a {tall:F0}-unit box, so its last "
                        + "line draws over whatever is under it.");
                    continue;
                }

                Assert.LessOrEqual(label.preferredWidth, room + 1.0f,
                    $"{resolution} {screen}: '{label.name}' needs "
                    + $"{label.preferredWidth:F0} units in a {room:F0}-unit box: \"{label.text}\"");
            }

            report.AppendLine($"{resolution} {screen}: {measured} labels");
            return measured;
        }

        /// <summary>
        /// ⚠️⚠️ NO `WaitForEndOfFrame`. In `-batchmode` there is no display and it never resumes,
        /// so the coroutine simply stops and the run hangs with the log frozen. It has already
        /// cost this project one killed run; `PlayerHubLayoutProbe.Shoot` carries the same note,
        /// and `ProbeWait` exists because of the second one.
        /// </summary>
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

            Debug.Log($"[Creator] shot {ShotDir}/{name}.png");
        }

        private static void Write(string name, StringBuilder report)
        {
            Directory.CreateDirectory(OutDir);
            File.WriteAllText(Path.Combine(OutDir, $"{name}-probe.txt"), report.ToString());
            Debug.Log($"[Creator] wrote {OutDir}/{name}-probe.txt");
        }
    }
}
