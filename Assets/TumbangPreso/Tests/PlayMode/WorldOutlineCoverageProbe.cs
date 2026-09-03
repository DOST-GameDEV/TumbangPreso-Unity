using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Measures WHERE on the screen the world outline actually lands, in PLAY mode, at a wide
    /// aspect.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE THREE DIAGNOSES IN A ROW WERE REASONED OFF A SCREENSHOT AND ALL
    /// THREE WERE WRONG. 🧑 2026-08-28: *"notice how only the center of the screen has the bold
    /// outlines"*, at roughly 2.06:1. A JPEG cannot distinguish "the pass covers a centred
    /// rectangle" from "the pass covers everything and the fade eats the distance", and those two
    /// have completely different fixes. This counts ink pixels per cell of a grid and prints the
    /// grid, so the shape of the coverage is a number rather than an impression.
    ///
    /// ⚠️ IT MUST RUN IN PLAY MODE. `OnRenderImage` on the opaque hook does not fire under
    /// `Camera.Render()` in edit mode, which is what made `WorldOutlineProbe` photograph four
    /// blank frames. In play mode the full loop runs, so rendering the live rig camera into a
    /// RenderTexture here DOES carry the effect.
    ///
    /// ⚠️ AND IT RENDERS AT 1434x696 ON PURPOSE. That is the aspect from the report, 2.06:1,
    /// far wider than 16:9. Anything that goes wrong as a function of distance from screen centre
    /// (a view-ray reconstruction, a texel-size assumption, an aspect baked at the wrong moment)
    /// is largest at an extreme aspect and can vanish entirely at 16:9.
    /// </summary>
    public sealed class WorldOutlineCoverageProbe
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

        private const int Width = 1434;
        private const int Height = 696;
        private const int Cols = 6;
        private const int Rows = 4;

        /// <summary>The ink is a very dark navy, `ToonSkin.Ink` = (0.016, 0.031, 0.220). Anything
        /// this dark in a daylit street scene is either outline or deep shadow.</summary>
        private const float InkLuma = 0.18f;

        [UnityTest]
        [Category("WallClock")]
        public IEnumerator OutlineCoverageAcrossTheFrame()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 40; i++) yield return null;

            CameraRig rig = null;

            foreach (var r in Object.FindObjectsByType<CameraRig>(FindObjectsSortMode.None))
            {
                if (r.Camera == null || !r.Camera.enabled) continue;
                rig = r;
                break;
            }

            Assert.IsNotNull(rig, "no active CameraRig, so there is no FPP frame to measure");

            var cam = rig.Camera;
            var outline = cam.GetComponent<WorldOutline>();

            Assert.IsNotNull(outline, "the rig camera has no WorldOutline component; CameraRig.Awake " +
                                      "is supposed to add it");

            var report = new StringBuilder();
            report.AppendLine("=== WORLD OUTLINE COVERAGE ===");
            report.AppendLine($"aspect={(float)Width / Height:F3} fov={cam.fieldOfView} " +
                              $"camAspect={cam.aspect:F3} far={cam.farClipPlane}");
            report.AppendLine($"fog={RenderSettings.fog} mode={RenderSettings.fogMode} " +
                              $"start={RenderSettings.fogStartDistance} end={RenderSettings.fogEndDistance}");
            report.AppendLine();

            // Both states, so the grid separates "the outline is here" from "this cell is just
            // dark". Subtracting the off frame is the only way to attribute ink to the pass.
            var off = Capture(cam, outline, false);
            var on = Capture(cam, outline, true);

            report.AppendLine($"ink % per cell, {Cols}x{Rows}, OUTLINE ON minus OFF:");

            for (int row = 0; row < Rows; row++)
            {
                var line = new StringBuilder("  ");

                for (int col = 0; col < Cols; col++)
                {
                    float delta = on[col, row] - off[col, row];
                    line.Append($"{delta * 100.0f,7:F2}");
                }

                report.AppendLine(line.ToString());
            }

            Debug.Log(report.ToString());

            // ⚠️ A PROBE, NOT A GATE. It prints and passes. The bound that would make this an
            // assertion has not been measured yet, and a red test carrying an invented number
            // teaches the next session to raise the number.
            Assert.Pass();
        }

        private static float[,] Capture(Camera cam, WorldOutline outline, bool on)
        {
            outline.PrototypeEnabled = on;

            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = cam.targetTexture;
            var previousAspect = cam.aspect;

            cam.targetTexture = rt;
            cam.aspect = (float)Width / Height;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            cam.targetTexture = previousTarget;
            cam.aspect = previousAspect;

            Directory.CreateDirectory("Logs/shots-world-outline");
            File.WriteAllBytes($"Logs/shots-world-outline/play_{(on ? "on" : "off")}.png",
                               tex.EncodeToPNG());

            var grid = new float[Cols, Rows];
            var pixels = tex.GetPixels();

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    int x0 = col * Width / Cols, x1 = (col + 1) * Width / Cols;

                    // Row 0 is the TOP of the frame for a reader, so flip: ReadPixels puts y=0 at
                    // the bottom and a grid printed upside down is a second thing to get wrong.
                    int y0 = (Rows - 1 - row) * Height / Rows, y1 = (Rows - row) * Height / Rows;

                    int ink = 0, total = 0;

                    for (int y = y0; y < y1; y++)
                    {
                        for (int x = x0; x < x1; x++)
                        {
                            var c = pixels[y * Width + x];
                            float luma = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                            if (luma < InkLuma) ink++;
                            total++;
                        }
                    }

                    grid[col, row] = total == 0 ? 0.0f : (float)ink / total;
                }
            }

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);

            return grid;
        }
    }
}
