using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    /// The settings list, at every resolution the game ships at: does the bar exist, is it
    /// inside the panel, does it cover a control, and does the list actually reach its last row.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE A SCROLLBAR WAS ADDED TO THAT PANEL ON 2026-08-26 AND NOTHING
    /// COULD SEE IT. 🧑 had reported the list twice: *"make it easier to scroll thru settings bcz
    /// its so hard to"* and *"here its so weird to scroll in setttings here"*, with a screenshot
    /// of a row cut in half at the bottom edge. `AspectRatioProbes` drives the CHARACTER SELECT
    /// panel through nine resolutions and no other screen, `UiRuntimeShots` photographs Settings
    /// at 1920x1080 only, and the first version of the bar drew straight over the right end of
    /// every key cap at that one resolution. A control the player cannot reach is worse than the
    /// missing affordance it replaced.
    ///
    /// ⚠️ IT ASSERTS GEOMETRY, NOT PIXELS. Whether the amber reads as amber is a judgement and
    /// `Logs/shots-runtime/SettingsPanel.png` is where that is made. What a test can settle is
    /// that the handle is on screen, that no rebind button is underneath it, and that the last
    /// row of the list can be brought into the window at all.
    /// </summary>
    public class SettingsScrollProbe
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
        /// ⚠️ THE SAME NINE `AspectRatioProbes` USES, and deliberately its list rather than a
        /// second one: 4:3 and 16:10 are where a fixed-width panel in a scaled canvas goes wrong,
        /// 1280x720 is the floor the readable-text bound is solved against, and 2560x1080 is the
        /// ultrawide the scaler matches on HEIGHT.
        /// </summary>
        private static readonly (int W, int H, string Name)[] Resolutions =
        {
            (1920, 1080, "1080p"),
            (1280, 720, "720p"),
            (2560, 1440, "1440p"),
            (1600, 900, "900p"),
            (1366, 768, "768p"),
            (1024, 768, "4:3"),
            (1440, 900, "16:10"),
            (2560, 1080, "ultrawide"),
            (800, 600, "600p"),
        };

        [UnityTest]
        public IEnumerator TheSettingsListScrollsAndItsBarCoversNothing()
        {
            var load = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 10; i++) yield return null;

            var panel = Find("SettingsPanel");
            Assert.IsNotNull(panel, "MainMenu has no SettingsPanel to open.");

            panel.SetActive(true);

            for (int i = 0; i < 5; i++) yield return null;

            var scroll = panel.GetComponentInChildren<ScrollRect>(true);
            Assert.IsNotNull(scroll, "the settings panel has no ScrollRect at all.");

            Assert.IsNotNull(scroll.verticalScrollbar,
                "the settings list has no scrollbar, so nothing on screen says the list is longer "
                + "than its window. That is the report this probe was written for.");

            var canvas = panel.GetComponentInParent<Canvas>();
            Assert.IsNotNull(canvas);

            var camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            Assert.IsNotNull(camera, "no camera to render the menu at a chosen size.");

            // ⚠️ THE CANVAS IS TAKEN OFF OVERLAY SO A RENDER TEXTURE CAN SET ITS SIZE. An overlay
            // canvas renders at the real display size and ignores the camera entirely, so a probe
            // that only swapped the target texture would measure 1080p nine times.
            //  `AspectRatioProbes` carries the same manoeuvre and the same warning.
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            var previousTarget = camera.targetTexture;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 0.01f;
            }

            var report = new StringBuilder();
            report.AppendLine("THE SETTINGS LIST, AT EVERY RESOLUTION.");
            report.AppendLine();
            report.AppendLine($"{"where",-10} {"viewport h",11} {"content h",10} {"scrollable",11} " +
                              $"{"bar x",8} {"panel right",12} {"covered",8}");
            report.AppendLine(new string('-', 82));

            var faults = new List<string>();
            RenderTexture target = null;

            foreach (var (w, h, name) in Resolutions)
            {
                var next = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = next;

                if (target != null) target.Release();
                target = next;

                // Three frames: the scaler recomputes in its own Update, the layout rebuild lands
                // after that, and the content size fitter after that.
                for (int i = 0; i < 3; i++) yield return null;

                var viewport = scroll.viewport;
                var content = scroll.content;
                Assert.IsNotNull(viewport);
                Assert.IsNotNull(content);

                float windowHeight = viewport.rect.height;
                float list = content.rect.height;
                float scrollable = list - windowHeight;

                var barRt = (RectTransform)scroll.verticalScrollbar.transform;
                var panelRt = (RectTransform)panel.transform;
                var canvasRt = (RectTransform)canvas.transform;

                // ⚠️⚠️ MEASURED IN THE CANVAS'S OWN SPACE, NOT IN WORLD CORNERS, AND THE FIRST
                // VERSION OF THIS PROBE USED WORLD CORNERS AND PRINTED **ZERO FOR EVERY COLUMN**.
                // A canvas rendering to a camera sits at the near plane, so every corner of every
                // element is within a hair of the same world x, and a comparison between two of
                // them is a comparison between two roundings. It passed nine resolutions while
                // measuring nothing, which is the failure `docs/TODO.md` § 15 keeps recording.
                // `AspectRatioProbes.AssertInside` does the same conversion for the same reason.
                var bar = InCanvas(canvasRt, barRt);
                var panelBox = InCanvas(canvasRt, panelRt);

                bool inside = bar.xMax <= panelBox.xMax + 1.0f && bar.xMin >= panelBox.xMin - 1.0f;

                // ---- does the bar sit on top of a control? -----------------------------
                //
                // ⚠️⚠️ THIS IS THE ONE THAT ACTUALLY FIRED. The first version of the bar shrank
                // the VIEWPORT to make room for itself, which moved the window and left the rows
                // where they were, because the content is authored at a fixed width out of the
                // .tscn rather than stretched to fit. Every key cap lost its right end and the
                // username field was cut in half, at the one resolution anybody had looked at.
                string covered = "-";

                var window = InCanvas(canvasRt, viewport);

                foreach (var button in content.GetComponentsInChildren<Button>(false))
                {
                    var row = InCanvas(canvasRt, (RectTransform)button.transform);

                    // Only rows currently inside the window can be judged; one scrolled out of
                    // sight is clipped by the mask and overlaps nothing.
                    if (row.yMax < window.yMin || row.yMin > window.yMax) continue;

                    if (row.xMax > bar.xMin + 0.5f)
                    {
                        covered = button.name;
                        faults.Add($"{name} ({w}x{h}): '{button.name}' reaches x {row.xMax:F0} " +
                                   $"and the scrollbar starts at {bar.xMin:F0}, so the bar is " +
                                   "drawn over it");
                        break;
                    }
                }

                report.AppendLine($"{name,-10} {windowHeight,11:F0} {list,10:F0} {scrollable,11:F0} " +
                                  $"{bar.xMin,8:F0} {panelBox.xMax,12:F0} {covered,8}");

                if (!inside)
                {
                    faults.Add($"{name} ({w}x{h}): the scrollbar spans x {bar.xMin:F0} to " +
                               $"{bar.xMax:F0} and the panel ends at {panelBox.xMax:F0}");
                }

                // ---- can the list actually be scrolled to its end? ---------------------
                if (scrollable > 1.0f)
                {
                    scroll.verticalNormalizedPosition = 0.0f;
                    yield return null;

                    float bottom = content.anchoredPosition.y;

                    scroll.verticalNormalizedPosition = 1.0f;
                    yield return null;

                    float top = content.anchoredPosition.y;

                    if (Mathf.Abs(bottom - top) < scrollable * 0.9f)
                    {
                        faults.Add($"{name} ({w}x{h}): the list moves {Mathf.Abs(bottom - top):F0} px " +
                                   $"between its ends but is {scrollable:F0} px longer than its window, " +
                                   "so part of it cannot be reached.");
                    }
                }
                else
                {
                    faults.Add($"{name} ({w}x{h}): the list is not longer than its window " +
                               $"({list:F0} against {windowHeight:F0}), which means the rows were not built.");
                }
            }

            camera.targetTexture = previousTarget;
            if (target != null) target.Release();
            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCamera;

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/settings-scroll.txt", report.ToString());
            Debug.Log(report.ToString());

            Assert.IsEmpty(faults,
                "the settings list is wrong at one or more resolutions: "
                + string.Join(" | ", faults) + ". Read Logs/settings-scroll.txt.");
        }

        /// <summary>
        /// An element's box in the canvas's own units, which is the only space in which two
        /// elements on one canvas can be compared. See the call site.
        /// </summary>
        private static Rect InCanvas(RectTransform canvas, RectTransform what)
        {
            var corners = new Vector3[4];
            what.GetWorldCorners(corners);

            float xMin = float.MaxValue, xMax = float.MinValue;
            float yMin = float.MaxValue, yMax = float.MinValue;

            for (int i = 0; i < 4; i++)
            {
                Vector3 local = canvas.InverseTransformPoint(corners[i]);
                xMin = Mathf.Min(xMin, local.x);
                xMax = Mathf.Max(xMax, local.x);
                yMin = Mathf.Min(yMin, local.y);
                yMax = Mathf.Max(yMax, local.y);
            }

            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private static GameObject Find(string name)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var hit = FindIn(root.transform, name);
                if (hit != null) return hit;
            }

            return null;
        }

        private static GameObject FindIn(Transform where, string name)
        {
            if (where.name == name) return where.gameObject;

            for (int i = 0; i < where.childCount; i++)
            {
                var hit = FindIn(where.GetChild(i), name);
                if (hit != null) return hit;
            }

            return null;
        }
    }
}
