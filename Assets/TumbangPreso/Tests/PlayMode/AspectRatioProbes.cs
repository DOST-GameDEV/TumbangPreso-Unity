using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The screen at every shape a monitor actually comes in.
    ///
    /// ⚠️⚠️ THE BUILD NOW OPENS AT THE MONITOR'S NATIVE RESOLUTION, so the one resolution the
    /// game used to be looked at in (1600x900 windowed, forced by the builder) is no longer
    /// the one anybody sees. Every screen in this game is authored against 1920x1080 and every
    /// canvas matched on HEIGHT, which is a crop on anything narrower than 16:9 and reads as
    /// "the READY button is missing" rather than as a scaling bug.
    ///
    /// ⚠️ THE RESOLUTION IS DRIVEN THROUGH A RENDER TARGET, NOT THROUGH `Screen`.
    /// `Screen.SetResolution` does nothing to `Screen.width` inside the editor, so a probe
    /// built on it asserts against the batch runner's own window at every "resolution" and
    /// passes for all of them. A `ScreenSpaceCamera` canvas lays out against the camera's pixel
    /// rect instead, which IS the render target, and `CanvasScaler` recomputes from exactly
    /// that. It is the same mechanism `UiRuntimeShots` photographs through, and it needs the
    /// same TWO frames after a target change before anything may be measured.
    /// </summary>
    public class AspectRatioProbes
    {
        /// <summary>
        /// The list the handoff asks for: 16:9 from 720p to 1440p, the common laptop panel,
        /// 16:10, both ultrawides, and 4:3.
        /// </summary>
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

        /// <summary>
        /// ⚠️ THE ARITHMETIC, ASSERTED SEPARATELY FROM THE SCENE. If this one fails the scaler
        /// rule is wrong and every layout claim below it is meaningless; keeping it apart means
        /// a failure says which of the two it is.
        /// </summary>
        [Test]
        public void EveryShippedResolutionKeepsTheWholeAuthoredLayoutOnScreen()
        {
            foreach (var (w, h, name) in Resolutions)
            {
                Vector2 size = AspectSafeCanvas.ReferenceSizeAt(w, h);

                Assert.GreaterOrEqual(size.x, AspectSafeCanvas.Reference.x - 0.5f,
                    $"{name} ({w}x{h}) gives a canvas {size.x:F0} reference units wide against a " +
                    $"layout authored at {AspectSafeCanvas.Reference.x:F0}, so " +
                    $"{AspectSafeCanvas.Reference.x - size.x:F0} units of it are off the edge.");

                Assert.GreaterOrEqual(size.y, AspectSafeCanvas.Reference.y - 0.5f,
                    $"{name} ({w}x{h}) gives a canvas {size.y:F0} reference units tall against a " +
                    $"layout authored at {AspectSafeCanvas.Reference.y:F0}.");
            }
        }

        /// <summary>
        /// The character screen, measured at all nine shapes: the model is not stretched, the
        /// panel and its buttons are on screen, and the text is big enough to read.
        /// </summary>
        [UnityTest]
        public IEnumerator TheCharacterScreenSurvivesEveryAspectRatio()
        {
            var previousMode = SceneFlow.SelectedMode;
            SceneFlow.SelectedMode = Core.GameMode.Classic;

            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");
            for (int i = 0; i < 30; i++) yield return null;

            var panel = Find("CharacterSelectPanel");
            Assert.IsNotNull(panel, "MatchSetup has no CharacterSelectPanel to open.");
            panel.SetActive(true);
            for (int i = 0; i < 20; i++) yield return null;

            var preview = panel.GetComponentInChildren<ModelPreview>(true);
            Assert.IsNotNull(preview, "The character panel built no ModelPreview.");

            var camera = Camera.main;
            Assert.IsNotNull(camera, "MatchSetup has no main camera to render a sized target.");

            var canvases = new List<Canvas>();
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,
                                                              FindObjectsSortMode.None))
            {
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = camera;
                c.planeDistance = camera.nearClipPlane + 0.01f;
                canvases.Add(c);
            }

            Assert.IsNotEmpty(canvases, "No overlay canvas to resize: the probe would prove nothing.");

            var previousTarget = camera.targetTexture;
            RenderTexture target = null;
            var report = new StringBuilder();

            foreach (var (w, h, name) in Resolutions)
            {
                var next = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = next;

                if (target != null) target.Release();
                target = next;

                // ⚠️ THREE FRAMES. CanvasScaler recomputes in ITS Update from the canvas's
                // rendering display size, the layout rebuild lands the frame after that, and
                // ModelPreview sizes its own render target from the finished panel rect in
                // LateUpdate. Measuring earlier reads the previous resolution's numbers.
                for (int i = 0; i < 3; i++) yield return null;

                // ---- THE MODEL IS NOT STRETCHED ----------------------------------------
                var panelRect = ((RectTransform)preview.transform).rect;
                Assert.IsNotNull(preview.Target, $"{name}: the preview has no render target.");

                float panelAspect = panelRect.width / panelRect.height;
                float targetAspect = (float)preview.Target.width / preview.Target.height;

                Assert.AreEqual(panelAspect, targetAspect, 0.02f,
                    $"{name} ({w}x{h}): the preview target is " +
                    $"{preview.Target.width}x{preview.Target.height} on a " +
                    $"{panelRect.width:F0}x{panelRect.height:F0} panel, so the character is " +
                    "stretched by the ratio between them.");

                Assert.AreEqual(targetAspect, preview.PreviewCamera.aspect, 0.02f,
                    $"{name}: the preview CAMERA disagrees with its own target, which is the " +
                    "same distortion one layer further in.");

                // ---- NOTHING IS CROPPED ------------------------------------------------
                var canvasRt = (RectTransform)panel.GetComponentInParent<Canvas>().transform;

                AssertInside(canvasRt, (RectTransform)panel.transform, name, "the panel itself");
                AssertInside(canvasRt, (RectTransform)preview.transform, name, "the model preview");

                foreach (var button in panel.GetComponentsInChildren<Button>(false))
                    AssertInside(canvasRt, (RectTransform)button.transform, name,
                                 $"button '{button.name}'");

                // ---- THE TEXT IS BIG ENOUGH TO READ ------------------------------------
                float scale = panel.GetComponentInParent<Canvas>().scaleFactor;

                foreach (var label in panel.GetComponentsInChildren<Text>(false))
                {
                    if (string.IsNullOrWhiteSpace(label.text)) continue;
                    if (label.color.a < 0.05f) continue;

                    // ⚠️ THE CLAIM IS ABOUT THE AUTHORED SIZE, NOT ABOUT THIS RESOLUTION'S
                    // PIXELS. A physical-pixel floor is the same assertion said badly: it
                    // passes at 1440p and fails at 720p for a label nobody changed, so the
                    // failure names a resolution instead of naming the label that is too
                    // small. The size in reference units is the number a developer actually
                    // wrote, it is the same at every resolution, and MenuKit.MinReadableUnits
                    // carries the arithmetic that turns it into pixels on the worst panel.
                    Assert.GreaterOrEqual(label.fontSize, MenuKit.MinReadableUnits,
                        $"'{label.name}' is authored at {label.fontSize} units, below the " +
                        $"{MenuKit.MinReadableUnits}-unit floor. At {name} ({w}x{h}) that is " +
                        $"{label.fontSize * scale:F1} physical pixels.");
                }

                report.AppendLine($"{name,-12} {w}x{h}  canvas scale {scale:F3}  " +
                                  $"preview {preview.Target.width}x{preview.Target.height}");
            }

            camera.targetTexture = previousTarget;
            if (target != null) target.Release();

            Debug.Log("[Aspect] character screen\n" + report);

            foreach (var c in canvases)
            {
                if (c == null) continue;
                c.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            SceneFlow.SelectedMode = previousMode;
        }

        private static void AssertInside(RectTransform canvas, RectTransform what,
                                         string resolution, string described)
        {
            if (what == null) return;

            // Elements inside a mask are clipped on purpose; asking them to fit is asking a
            // scroll list to have no scroll.
            if (what.GetComponentInParent<RectMask2D>() != null) return;
            if (what.GetComponentInParent<Mask>() != null) return;

            var canvasRect = canvas.rect;
            var corners = new Vector3[4];
            what.GetWorldCorners(corners);

            for (int i = 0; i < 4; i++)
            {
                Vector3 local = canvas.InverseTransformPoint(corners[i]);

                // ⚠️ HALF A UNIT OF SLACK. A canvas laid out in fractional reference units puts
                // an element flush with the edge a hair past it, and that is not a crop.
                Assert.GreaterOrEqual(local.x, canvasRect.xMin - 0.5f,
                    $"{resolution}: {described} runs {canvasRect.xMin - local.x:F0} units off " +
                    "the LEFT of the screen.");
                Assert.LessOrEqual(local.x, canvasRect.xMax + 0.5f,
                    $"{resolution}: {described} runs {local.x - canvasRect.xMax:F0} units off " +
                    "the RIGHT of the screen.");
                Assert.GreaterOrEqual(local.y, canvasRect.yMin - 0.5f,
                    $"{resolution}: {described} runs {canvasRect.yMin - local.y:F0} units off " +
                    "the BOTTOM of the screen.");
                Assert.LessOrEqual(local.y, canvasRect.yMax + 0.5f,
                    $"{resolution}: {described} runs {local.y - canvasRect.yMax:F0} units off " +
                    "the TOP of the screen.");
            }
        }

        private static GameObject Find(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                  FindObjectsSortMode.None))
                if (t.name == name) return t.gameObject;

            return null;
        }
    }
}
