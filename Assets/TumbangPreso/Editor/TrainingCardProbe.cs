using System.IO;
using TumbangPreso.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Photographs the guided training card so its layout can be judged instead of argued about.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE THE CARD HAS BEEN REJECTED TWICE ON ITS LAYOUT AND BOTH TIMES
    /// THE ONLY EVIDENCE WAS A GAMEPLAY SCREENSHOT 🧑 HAD TO TAKE. *"the ui has problems like big
    /// open space"*. Reading the source could not settle where the space came from, because the
    /// answer was a layout product: rows at hand-written offsets inside a fixed box, where a
    /// short title leaves a hole and nothing in the code says so. `CLAUDE.md` § 6.1 is explicit
    /// that a change with no render attached cannot be judged, and that rule was written about
    /// models; it is just as true of a card.
    ///
    /// ⚠️ IT BUILDS THE REAL `GuidedTrainingHud`, NOT A MOCK, for the reason `HeroUiProbe`
    /// records about the inspect tray: a mock photographs whatever the probe author believed the
    /// layout was, which is the one thing a screenshot is supposed to rule out.
    ///
    /// ⚠️ AND IT SHOOTS THE LONGEST AND SHORTEST LESSONS, not one representative frame. The
    /// whole failure mode is a box that fits one string and not another, so a single capture of
    /// a middling lesson is the picture most likely to look fine while the fault is live.
    ///
    /// ⚠️ IT MUST RUN WITHOUT `-nographics`. That flag leaves the process with no rendering
    /// device, so the PNG comes back blank while the run reports success.
    ///
    /// Run:
    ///   Unity.exe -batchmode -quit -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.TrainingCardProbe.CaptureAll
    /// </summary>
    public static class TrainingCardProbe
    {
        private const string OutDir = "Logs/shots-training";

        /// <summary>The card is 690 wide at 36 in from the edge, and the footer hangs under it.
        /// A little air on every side so the wood border is not flush with the frame.</summary>
        private const int Width = 820;
        private const int Height = 520;

        [MenuItem("Tumbang Preso/Capture Training Card")]
        public static void CaptureFromMenu() => Execute();

        public static void CaptureAll()
        {
            Execute();
            EditorApplication.Exit(0);
        }

        private static void Execute()
        {
            Directory.CreateDirectory(OutDir);

            // Three shapes of lesson: the shortest title with one key, the longest body in the
            // route with two keys, and a lesson whose action is prose rather than a control.
            Shoot("training_card_short_v1", 3, "JUMP",
                  "Jump once. Use it to clear street clutter, not to escape the defender's box.",
                  "[SPACE]  ·  JUMP");

            Shoot("training_card_long_v1", 6, "CURVE A PEKTUS THROW",
                  "Charge another throw, add spin with the wheel or arrow keys, then release. " +
                  "Strong spin can bank once.",
                  "[LMB] + MOUSE WHEEL / ARROWS");

            Shoot("training_card_twokeys_v1", 2, "SPRINT",
                  "Sprint while moving for one second. A full stamina bar buys roughly one " +
                  "crossing of the danger box.",
                  "[LEFT SHIFT] + [WASD]");

            Shoot("training_card_nokey_v1", 0, "LOOK AROUND",
                  "Move the mouse and find the lata. Your camera is also your aim.",
                  "MOUSE  ·  LOOK AND AIM");
        }

        private static void Shoot(string name, int lesson, string title, string body, string action)
        {
            var rig = new GameObject("~TrainingCardRig");

            try
            {
                var cameraGo = new GameObject("Cam");
                cameraGo.transform.SetParent(rig.transform, false);
                var cam = cameraGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;

                // A mid grey, not black: the card is dark wood and a dark card on black cannot
                // be told from the background it sits on.
                cam.backgroundColor = new Color(0.34f, 0.42f, 0.52f, 1.0f);
                cam.orthographic = true;

                var hud = GuidedTrainingHud.Build(rig.transform);

                // ⚠️ THE CARD BUILDS ITS OWN SCREEN-SPACE-OVERLAY CANVAS, which renders to the
                // game view and not to a texture. Re-pointing it at this camera is the whole
                // trick, and it is the same one `HeroUiProbe` uses.
                var canvas = hud.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1.0f;

                // ⚠️⚠️ THE REFERENCE IS THE RENDER TEXTURE, NOT 1920x1080. The card's own scaler
                // matches on HEIGHT against a 1080 reference, so rendering it into a 520 px
                // texture would scale it by 0.48 and photograph a stamp. Matching the reference
                // to the texture draws it at 1:1, which is the size a player sees.
                var scaler = hud.GetComponent<CanvasScaler>();
                scaler.referenceResolution = new Vector2(Width, Height);
                scaler.matchWidthOrHeight = 1.0f;

                hud.SetLesson(lesson, TumbangPreso.GuidedTraining.LessonCount,
                              title, body, action, UiTheme.Offense);
                hud.SetProgress(0.45f);

                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)hud.transform);
                Canvas.ForceUpdateCanvases();

                Capture(cam, name);
            }
            finally
            {
                Object.DestroyImmediate(rig);
            }
        }

        private static void Capture(Camera cam, string name)
        {
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 2
            };

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            cam.targetTexture = null;

            File.WriteAllBytes($"{OutDir}/{name}.png", tex.EncodeToPNG());
            Debug.Log($"[TrainingCard] wrote {OutDir}/{name}.png");

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
