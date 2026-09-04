using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Does the mouse wheel actually scroll the settings list, from every pixel of it?
    ///
    /// ⚠️⚠️ THIS IS THE FOURTH TIME THE SETTINGS SCROLL HAS BEEN REPORTED AND THE FIRST TIME
    /// ANYTHING HAS MEASURED THE WHEEL. 🧑 2026-08-27: *"u can scroll by holding scroll and yes i
    /// want to keep that feature but u cant scroll by using mouse scroll or laptop pad scroll ...
    /// repeated complaint! it feels so clunky/doesnt work at all"*. `SettingsScrollProbe` checks
    /// the bar's geometry and that the list can be moved BY SETTING ITS NORMALISED POSITION, which
    /// is not the thing a player does and would pass on a panel the wheel never reaches.
    ///
    /// ⚠️⚠️ IT SAMPLES A GRID, NOT THE CENTRE. The fault was never "the wheel does nothing", it
    /// was "the wheel does nothing over about half the panel": Unity delivers a scroll to whatever
    /// the pointer raycast HITS and drops it when the raycast hits nothing, and the list's gaps,
    /// margins and heading had no graphic on them at all. One sample in the middle of a key cap
    /// passes against the broken build. The grid is what makes the hole visible.
    /// </summary>
    public class SettingsWheelProbe
    {
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES THIS SUITE'S RESULT MEAN SOMETHING IN A FULL RUN.
        /// `docs/TODO.md` § 126.8: this fixture is one of the five named by stack trace in two
        /// full PlayMode runs that came back 42 red and 41 red **with eleven suites swapping
        /// sides**, and it had no teardown of any kind. `PlayModeWorld.Reset` has the mechanism
        /// and why both hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator SetUpWorld() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator TearDownWorld() => PlayModeWorld.Reset();

        /// <summary>Points across the panel, in normalised panel space.</summary>
        private const int GridX = 5;
        private const int GridY = 9;

        [UnityTest]
        public IEnumerator TheWheelScrollsTheSettingsListFromEveryPartOfIt()
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
            Assert.IsNotNull(scroll.content, "the ScrollRect has no content, so nothing can scroll.");

            var events = Object.FindFirstObjectByType<EventSystem>();
            Assert.IsNotNull(events, "no EventSystem, so no pointer event can be delivered.");

            var canvas = panel.GetComponentInParent<Canvas>();
            Assert.IsNotNull(canvas);

            var panelRt = (RectTransform)panel.transform;
            var corners = new Vector3[4];
            panelRt.GetWorldCorners(corners);

            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            var report = new StringBuilder();
            report.AppendLine("THE WHEEL, ACROSS THE WHOLE SETTINGS PANEL.");
            report.AppendLine();
            report.AppendLine($"{"point",-12} {"hit",-28} {"handler",-28} {"moved px",9}");
            report.AppendLine(new string('-', 82));

            var dead = new List<string>();
            int live = 0;

            for (int gy = 0; gy < GridY; gy++)
            {
                for (int gx = 0; gx < GridX; gx++)
                {
                    // Inset from the very edge: a corner pixel is a rounding argument, not a
                    // place anybody points at.
                    float u = Mathf.Lerp(0.08f, 0.92f, GridX == 1 ? 0.5f : gx / (float)(GridX - 1));
                    float v = Mathf.Lerp(0.06f, 0.94f, GridY == 1 ? 0.5f : gy / (float)(GridY - 1));

                    Vector3 world = Vector3.Lerp(
                        Vector3.Lerp(corners[0], corners[3], u),
                        Vector3.Lerp(corners[1], corners[2], u), v);

                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, world);

                    // Halfway down the list, so a scroll in either direction has room to move.
                    scroll.verticalNormalizedPosition = 0.5f;
                    yield return null;

                    float before = scroll.content.anchoredPosition.y;

                    var pointer = new PointerEventData(events)
                    {
                        position = screen,
                        scrollDelta = new Vector2(0.0f, -1.0f),
                    };

                    var hits = new List<RaycastResult>();
                    events.RaycastAll(pointer, hits);

                    GameObject hit = hits.Count > 0 ? hits[0].gameObject : null;
                    pointer.pointerCurrentRaycast = hits.Count > 0 ? hits[0] : default;

                    GameObject handler = hit != null
                        ? ExecuteEvents.GetEventHandler<IScrollHandler>(hit)
                        : null;

                    if (handler != null)
                        ExecuteEvents.ExecuteHierarchy(handler, pointer, ExecuteEvents.scrollHandler);

                    yield return null;

                    float moved = scroll.content.anchoredPosition.y - before;

                    report.AppendLine($"{gx + "," + gy,-12} {Name(hit),-28} {Name(handler),-28} {moved,9:F1}");

                    if (Mathf.Abs(moved) > 0.5f)
                    {
                        live++;
                    }
                    else
                    {
                        dead.Add($"({gx},{gy}) over '{Name(hit)}' -> handler '{Name(handler)}'");
                    }
                }
            }

            report.AppendLine();
            report.AppendLine($"{live} of {GridX * GridY} sample points scroll the list.");
            if (dead.Count > 0)
            {
                report.AppendLine();
                report.AppendLine("DEAD POINTS:");
                foreach (var d in dead) report.AppendLine("  " + d);
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/settings-wheel.txt", report.ToString());
            Debug.Log(report.ToString());

            Assert.IsEmpty(dead,
                $"{dead.Count} of {GridX * GridY} points on the settings panel swallow the mouse "
                + "wheel. Every pixel of an open panel must scroll its one list. Read "
                + "Logs/settings-wheel.txt: " + string.Join(" | ", dead));
        }

        private static string Name(GameObject go) => go == null ? "<nothing>" : go.name;

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
