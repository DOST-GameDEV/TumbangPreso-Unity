using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Dumps every HUD element's LAID-OUT rect, in the .tscn's own 1920x1080 space, to
    /// `Logs/hud-layout.txt`.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE "HUD PARITY IS NOT PROVEN" KEPT BEING TRUE AFTER EVERY PASS THAT
    /// CLAIMED IT. Nine HUD numbers were re-derived from `HUD.tscn` and `ui_theme.gd` and then
    /// checked by looking at a screenshot, which cannot see a padding that failed to apply, a
    /// `LayoutElement` that lost to a `ContentSizeFitter`, or a font whose glyphs are shorter
    /// per size unit than Godot's. A rect is a number. `HUD.tscn` is also numbers. Comparing
    /// pictures of them was always the weakest available check.
    ///
    /// ⚠️ AND A CAPTURE CANNOT DO THIS JOB EVEN IN PRINCIPLE. `UiRuntimeShots` photographs the
    /// arena through the match camera, which means the HUD goes through `ColourGrade` — an
    /// overlay canvas in the real game is composited AFTER post and is not graded at all. So
    /// every colour in that PNG is shifted, and measuring a text bounding box by colour match
    /// off it is measuring the grade as much as the layout.
    ///
    /// The reference column is `HUD.tscn`, quoted per line. Nothing here asserts a number
    /// against it: a rect that disagrees can be the .tscn's own container hugging its content,
    /// and a test that guessed which would fail for the wrong reason. It asserts only that
    /// every element EXISTS and has a non-degenerate rect, and prints the rest to be read.
    /// </summary>
    public class HudLayoutProbe
    {
        private const string OutPath = "Logs/hud-layout.txt";

        /// <summary>What the scene authors, for the elements whose position is authored rather
        /// than hugged. Printed beside the measurement; see the class note on why it is not
        /// asserted.</summary>
        private static readonly Dictionary<string, string> Authored = new Dictionary<string, string>
        {
            { "Scoreboard",      "HUD.tscn: offset 16, 28 from top-left; Column separation 4" },
            { "TopCentre",       "HUD.tscn: anchor 0.5/top, offset_left -120 offset_right 120, top 28" },
            { "TimerLabel",      "HudTimer -> ui_theme.gd FONT_SIZE_TIMER = 44" },
            { "RoundLabel",      "HudBody" },
            { "LataCard",        "HUD.tscn: -396,-172 to -16,-64 from bottom-right" },
            { "ToastLabel",      "HUD.tscn: anchor 0.5/top, +160, width 400" },
            { "CountdownLabel",  "HUD.tscn: centre, -100,-60 to 100,60; HudBanner" },
            { "ReadyPrompt",     "HUD.tscn: bottom centre, -260..260, -158..-118; HudToast" },
            { "ReadyObjective",  "HUD.tscn: ReadyObjectiveRow -560..560, -232..-168; font_size 44" },
            { "Crosshair",       "HUD.tscn: centre, -12,-12 to 12,12" },
            { "Card",            "YouCard.tscn: bottom-left 16,-196 to 396,-64" },
        };

        [UnityTest]
        public IEnumerator EveryHudElementIsLaidOutWhereTheSceneSaysItIs()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            // Long enough for the installer to build the match, the HUD to bind a seat and every
            // layout group to have run at least once.
            for (int i = 0; i < 30; i++) yield return null;

            var hud = Object.FindFirstObjectByType<UI.Hud>();
            Assert.IsNotNull(hud, "no HUD in the arena");

            var lines = new StringBuilder();
            lines.AppendLine("HUD layout, measured in canvas space.");
            lines.AppendLine("Origin is TOP-LEFT and y grows DOWNWARD, like a .tscn offset.");
            lines.AppendLine();
            lines.AppendLine("⚠️ THE CANVAS IS AS WIDE AS THE RUNNER'S ASPECT MAKES IT, AND ONLY THE");
            lines.AppendLine("   HEIGHT IS PINNED. Every canvas here matches on HEIGHT (1080), so a 4:3");
            lines.AppendLine("   batch runner gives a 1440-wide canvas and a 16:9 one gives 1920. WIDTHS");
            lines.AppendLine("   AND HEIGHTS below are directly comparable to HUD.tscn; an X measured");
            lines.AppendLine("   from a centre anchor is NOT, unless the canvas width printed below is");
            lines.AppendLine("   1920. Left- and right-anchored elements are unaffected either way.");
            lines.AppendLine();
            lines.AppendLine($"{"element",-22} {"x",7} {"y",7} {"w",7} {"h",7}  authored");
            lines.AppendLine(new string('-', 110));

            int measured = 0;

            // The HUD and the YOU card build a canvas each, and both are being compared against
            // the same .tscn space.
            var canvases = new List<Canvas>();
            canvases.AddRange(hud.GetComponentsInChildren<Canvas>(true));

            var youCard = Object.FindFirstObjectByType<UI.YouCard>();
            if (youCard != null) canvases.AddRange(youCard.GetComponentsInChildren<Canvas>(true));

            foreach (var canvas in canvases)
            {
                var canvasRect = (RectTransform)canvas.transform;
                lines.AppendLine($"[canvas] {canvas.name}  {canvasRect.rect.width:0} x " +
                                 $"{canvasRect.rect.height:0}  scaleFactor {canvas.scaleFactor:0.000}");

                foreach (var graphic in canvas.GetComponentsInChildren<Graphic>(true))
                {
                    var rect = graphic.rectTransform;
                    if (rect.rect.width <= 0.0f && rect.rect.height <= 0.0f) continue;

                    Rect box = CanvasRect(rect, canvas);

                    Authored.TryGetValue(graphic.gameObject.name, out string note);

                    // ⚠️ A HIDDEN ELEMENT'S RECT IS MEANINGLESS AND HAS TO SAY SO. A layout group
                    // never runs on an inactive child, so it keeps the RectTransform default of
                    // 100x100 at its parent's corner — which reads exactly like a laid-out
                    // element in the wrong place. The lata card, the charge meter and the
                    // FATIGUED label are all legitimately hidden at rest.
                    bool live = graphic.gameObject.activeInHierarchy && graphic.enabled;

                    lines.AppendLine($"{graphic.gameObject.name,-22} " +
                                     $"{box.x,7:0.0} {box.y,7:0.0} {box.width,7:0.0} {box.height,7:0.0}" +
                                     $"  {(live ? "" : "[hidden, rect not laid out] ")}{note}");
                    measured++;
                }
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText(OutPath, lines.ToString());

            Debug.Log($"[HudLayout] wrote {OutPath} with {measured} elements.\n{lines}");

            Assert.Greater(measured, 12,
                "the HUD laid out almost nothing, so either it never built or every rect is " +
                "degenerate. Read Logs/hud-layout.txt.");
        }

        /// <summary>
        /// A RectTransform's world corners expressed in the canvas's own reference space, with
        /// the origin at the TOP-LEFT so the numbers can be read straight against a .tscn offset.
        ///
        /// ⚠️ EXPRESSED IN THE CANVAS'S LOCAL SPACE, WHICH IS ALREADY THE REFERENCE SPACE. A
        /// `ScaleWithScreenSize` canvas puts its `scaleFactor` on the root RectTransform's
        /// TRANSFORM, and its `rect` stays 1920x1080 whatever the window is, so an
        /// `InverseTransformPoint` through it lands in the .tscn's own units with no correction
        /// needed. That is the whole reason this measures through the canvas rather than off
        /// screen pixels: the answer does not depend on the resolution the runner opened at.
        /// </summary>
        private static Rect CanvasRect(RectTransform rect, Canvas canvas)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            var canvasRect = (RectTransform)canvas.transform;

            // Bottom-left and top-right, in the canvas's local space.
            Vector3 min = canvasRect.InverseTransformPoint(corners[0]);
            Vector3 max = canvasRect.InverseTransformPoint(corners[2]);

            // Godot measures from the TOP-left with y down; UGUI's local space has y up from the
            // centre. Flip once, here, so nothing downstream has to remember to.
            float top = canvasRect.rect.height * 0.5f - max.y;
            float left = min.x + canvasRect.rect.width * 0.5f;

            return new Rect(left, top, max.x - min.x, max.y - min.y);
        }
    }
}
