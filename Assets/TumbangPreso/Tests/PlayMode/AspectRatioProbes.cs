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
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code. `PlayModeWorld.Reset` has the
        /// mechanism and why BOTH hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        /// <summary>
        /// The nine desktop shapes plus the phones, from <see cref="ProbeResolutions"/>.
        ///
        /// ⚠️⚠️ THE LIST MOVED OUT OF THIS FILE ON 2026-09-02 AND GAINED THE PHONE SHAPES WITH
        /// THE MOVE. It was private here and `InputSurfaceProbe` needed the same nine; a copy is
        /// two lists that agree until somebody edits one, which is `docs/TODO.md` § 124.11's
        /// fault in a different costume. Adding a resolution now reaches every layout probe in
        /// the project at once, which is the point.
        /// </summary>
        private static readonly (int W, int H, string Name)[] Resolutions = ProbeResolutions.All();

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

            // Every label under the floor, deduped by name and size, collected across all nine
            // shapes and asserted ONCE at the end. See the note at the collection site for why
            // that matters more than it looks.
            var tooSmall = new HashSet<string>();
            var tooSmallRows = new List<string>();

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

                    // ⚠️⚠️ A REGISTERED EXEMPTION IS SKIPPED AND COUNTED, NOT SKIPPED SILENTLY,
                    // AND THE PROBE'S FLOOR IS UNTOUCHED. `docs/TODO.md` § 126.13 closes with
                    // **"Do not lower the probe's floor to make it green"**, and offers two ways
                    // out: widen the box, or write the exemption in by name. This is the second,
                    // and `MenuKit.Fit` attaches `TightLabel` itself so no caller can claim an
                    // exemption it did not declare.
                    //
                    // ⚠️ THE REPORT LINE IS THE HALF THAT MATTERS. § 126.13 exists because one
                    // local exemption was copied twice with nothing anywhere able to enumerate
                    // the set, so the way this stays honest is that every one of them is printed
                    // with its floor, its settled size and the room it was fighting for. **A
                    // growing list in this report is the signal that a screen needs more room,
                    // and a silent skip would hide exactly that.**
                    var tight = label.GetComponent<TumbangPreso.UI.TightLabel>();
                    if (tight != null)
                    {
                        report.AppendLine(
                            $"{name,-12} EXEMPT  '{label.name}' settled at {tight.Settled} " +
                            $"units against a {tight.Floor} floor in {tight.Room:F0} units of room");
                        continue;
                    }

                    // ⚠️ THE CLAIM IS ABOUT THE AUTHORED SIZE, NOT ABOUT THIS RESOLUTION'S
                    // PIXELS. A physical-pixel floor is the same assertion said badly: it
                    // passes at 1440p and fails at 720p for a label nobody changed, so the
                    // failure names a resolution instead of naming the label that is too
                    // small. The size in reference units is the number a developer actually
                    // wrote, it is the same at every resolution, and MenuKit.MinReadableUnits
                    // carries the arithmetic that turns it into pixels on the worst panel.
                    // ⚠️⚠️ A LABEL AT EXACTLY `PaperKit.Caption` IS THE ONE OPEN DESIGN QUESTION
                    // ON THIS SCREEN, AND THE MESSAGE SAYS SO RATHER THAN LEAVING THE NEXT READER
                    // TO RE-DERIVE IT. `docs/TODO.md` § 121.8: `PaperKit.Caption` is 16,
                    // `MenuKit.MinReadableUnits` is 18, `PaperKit`'s own header states the
                    // conflict as a deliberate decision, and that entry says in as many words
                    // that it is **"settled by looking at the running build and not by either
                    // file winning on paper"**. It is not settled, so this stays RED.
                    //
                    // ⚠️ THE FLOOR IS NOT LOWERED AND THE CAPTION IS NOT EXEMPTED. § 126.13:
                    // *"Do not lower the probe's floor to make it green."* What changed is that
                    // after that batch's three `Fit(..., 14)` fixes, **this is the only remaining
                    // source of red on this screen**, so the probe has been narrowed from "some
                    // label somewhere is small" to one named constant and one open entry.
                    string why = label.fontSize == PaperKit.Caption
                        ? " This is exactly PaperKit.Caption, which is docs/TODO.md " +
                          "§ 121.8's open question rather than an unnoticed bug: settle that " +
                          "entry before changing either constant, and do it with a render."
                        : "";

                    // ⚠️⚠️ COLLECTED AND ASSERTED ONCE AT THE END, NOT THROWN HERE, AND THAT
                    // CHANGE IS WORTH MORE THAN IT LOOKS. `Assert` throws on the FIRST failing
                    // label, so every label after it in the walk is invisible and the report
                    // names one problem however many there are. **`docs/TODO.md` § 130.15 went
                    // stale exactly this way**: it records the character screen's only red as
                    // `DoorCaption` at 16, which is § 121.8's open question, and the first
                    // failure is actually a label authored at **13**. So § 121.8 has been the
                    // entry blocking a probe that was failing on something else entirely, and
                    // `Attention.md` § 3 has been asking 🧑 to settle a question that is not what
                    // is red.
                    //
                    // ⚠️ THE FLOOR IS UNTOUCHED AND NOTHING IS EXEMPTED HERE. § 126.13: *"Do not
                    // lower the probe's floor to make it green."* This only changes how many of
                    // the failures a reader gets to see per run, which is the difference between
                    // a worklist and a whack-a-mole.
                    if (label.fontSize < MenuKit.MinReadableUnits)
                    {
                        // ⚠️⚠️ THE LETTERING IS IN THE REPORT, NOT JUST THE NODE NAME, AND THE
                        // FIRST RUN OF THIS IS WHY. `MenuKit.Label` leaves every label it makes
                        // called "Label", and `ConvertedCharacterSelect` has FIVE at 13 units
                        // (EQUIPPED, LOCKED, the gain/cost line, the progress count and the key
                        // chip). They all reported as `'Label' at 13`, which names the fault
                        // without naming which of the five it is: a worklist nobody can act on.
                        // **The words are the only thing that tells them apart**, and they are
                        // also what the reader would search the source for.
                        string words = label.text.Trim().Replace("\n", " ");
                        if (words.Length > 40) words = words.Substring(0, 37) + "...";

                        string row = $"'{label.name}' (\"{words}\") is authored at "
                                     + $"{label.fontSize} units, below the "
                                     + $"{MenuKit.MinReadableUnits}-unit floor. At {name} "
                                     + $"({w}x{h}) that is {label.fontSize * scale:F1} physical "
                                     + $"pixels.{why}";

                        // ⚠️ DEDUPED ON THE WORDS AND THE SIZE, because the walk runs once per
                        // resolution and the authored size is the same at all nine. Without a
                        // dedupe one small label reports nine times and the list stops being
                        // readable, which is the fault this whole change exists to fix.
                        string key = $"{label.name}|{words}|{label.fontSize}";
                        if (tooSmall.Add(key)) tooSmallRows.Add(row);
                    }
                }

                report.AppendLine($"{name,-12} {w}x{h}  canvas scale {scale:F3}  " +
                                  $"preview {preview.Target.width}x{preview.Target.height}");
            }

            camera.targetTexture = previousTarget;
            if (target != null) target.Release();

            Debug.Log("[Aspect] character screen\n" + report);

            // ⚠️ ONE ASSERTION FOR EVERY LABEL UNDER THE FLOOR, so the failure is the whole
            // worklist rather than whichever one the walk happened to reach first.
            Assert.IsEmpty(tooSmallRows,
                $"{tooSmallRows.Count} label(s) on the character screen are authored below "
                + $"MenuKit.MinReadableUnits ({MenuKit.MinReadableUnits}). Do NOT lower the floor "
                + "to make this green: docs/TODO.md § 126.13. Widen the box, cut the words, or "
                + "register the exemption through MenuKit.Fit so it is counted rather than "
                + "hidden:\n  " + string.Join("\n  ", tooSmallRows));

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
