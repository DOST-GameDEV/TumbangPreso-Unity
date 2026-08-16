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
    /// Asks, for every button on every screen, whether a click at its centre actually reaches it.
    ///
    /// ⚠️⚠️ "THE BUTTONS DON'T WORK" IS INVISIBLE TO EVERY OTHER CHECK WE HAVE. A screenshot
    /// shows a perfectly drawn control; the importer reports it converted; the ported script
    /// wires a listener onto it and logs nothing. The one thing that decides whether it responds
    /// is whether some OTHER graphic with `raycastTarget` on happens to sit over it, and that is
    /// only knowable by doing the raycast.
    ///
    /// Godot has no equivalent failure because `mouse_filter = MOUSE_FILTER_IGNORE` is authored
    /// on every decorative node in the .tscn and the converter has to reproduce it per node type.
    /// A type it does not know about arrives with Unity's default, which is "eat every click".
    ///
    /// ⚠️ IT REPORTS THE BLOCKER BY NAME. "BackButton is blocked" is not actionable; "BackButton
    /// is blocked by Body/Columns/LeftColumn" is a one-line fix, and the name is what tells you
    /// which converter branch to correct.
    /// </summary>
    public class UiClickProbe
    {
        private const string OutPath = "Logs/ui-clicks.txt";

        /// <summary>
        /// ⚠️ SCENES ONLY. `SettingsPanel`, `CreditsPanel`, `TutorialPanel` and
        /// `CharacterSelectPanel` are OVERLAYS that live inside these screens rather than
        /// scenes of their own, so asking the build settings for them reports four false
        /// failures. They are opened in place below.
        /// </summary>
        private static readonly string[] Screens =
        {
            "MainMenu", "ModeSelect", "MatchSetup", "MultiplayerSetup", "MatchResult",
        };

        /// <summary>The in-place overlays, and the screen each one lives on.</summary>
        private static readonly (string Screen, string Node)[] Overlays =
        {
            ("MainMenu", "SettingsPanel"),
            ("MainMenu", "TutorialPanel"),
            ("MainMenu", "CreditsPanel"),
            ("MatchSetup", "CharacterSelectPanel"),
        };

        /// <summary>
        /// ⚠️⚠️ LONG ENOUGH FOR THE LAYOUT AND THE ENTRANCE TO SETTLE, AND THREE FRAMES WAS NOT.
        /// The pennants unfurl from `scale.x = 0` over 0.45 s with a stagger, and a Godot
        /// container's Unity equivalent needs a layout pass before its children have a size at
        /// all. Probing at frame three reported START MATCH and every seat row as unreachable
        /// when the only thing wrong was that they had no width yet — a false positive that
        /// would have sent somebody rewriting a working screen.
        /// </summary>
        private const int SettleFrames = 120;

        [UnityTest]
        public IEnumerator EveryButtonIsReachable()
        {
            var report = new StringBuilder();
            var blocked = new List<string>();

            // ⚠️⚠️ PROBE AT THE SHIPPED ASPECT. The batch runner's game view is 640x480, and the
            // menus are authored for 1920x1080 with the canvas matching on HEIGHT — so at 4:3
            // the right-hand column of the setup screen and both pennants on the mode screen sit
            // outside the viewport, and every one of them reports as unreachable. That is a
            // truthful statement about a 4:3 window and a useless one about the game, which
            // ships windowed at 1600x900.
            Screen.SetResolution(1600, 900, false);
            for (int i = 0; i < 10; i++) yield return null;

            foreach (string screen in Screens)
            {
                if (!Application.CanStreamedLevelBeLoaded(screen))
                {
                    report.AppendLine($"{screen}: NOT IN BUILD SETTINGS");
                    continue;
                }

                var load = SceneManager.LoadSceneAsync(screen, LoadSceneMode.Single);
                while (load != null && !load.isDone) yield return null;

                for (int i = 0; i < SettleFrames; i++) yield return null;

                Canvas.ForceUpdateCanvases();

                report.AppendLine($"--- {screen} --- ({Screen.width}x{Screen.height})");
                Probe(screen, report, blocked);

                // The overlays this screen owns, opened in place exactly as a player opens them.
                foreach (var overlay in Overlays)
                {
                    if (overlay.Screen != screen) continue;

                    var node = FindByName(overlay.Node);

                    if (node == null)
                    {
                        report.AppendLine($"--- {overlay.Node} --- NOT PRESENT on {screen}");
                        blocked.Add($"{screen}: overlay '{overlay.Node}' is missing");
                        continue;
                    }

                    node.SetActive(true);

                    for (int i = 0; i < SettleFrames; i++) yield return null;

                    Canvas.ForceUpdateCanvases();

                    report.AppendLine($"--- {overlay.Node} ---");

                    // ⚠️ ONLY THE OVERLAY'S OWN CONTROLS ARE ASSERTED ON. The screen underneath
                    // is SUPPOSED to be covered: an open character panel that let you press the
                    // map arrows behind it would be the bug. Probing everything reported a dozen
                    // correct behaviours as failures and buried the one real one.
                    Probe(overlay.Node, report, blocked, node.transform);

                    node.SetActive(false);
                    yield return null;
                }
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText(OutPath, report.ToString(), new UTF8Encoding(false));
            Debug.Log(report.ToString());

            Assert.IsEmpty(blocked, "buttons a player cannot click:\n" + string.Join("\n", blocked));
        }

        private static void Probe(string screen, StringBuilder report, List<string> blocked,
                                  Transform only = null)
        {
            var system = EventSystem.current;

            if (system == null)
            {
                report.AppendLine("   NO EVENT SYSTEM: nothing on this screen is clickable at all.");
                blocked.Add($"{screen}: no EventSystem");
                return;
            }

            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,
                                                                    FindObjectsSortMode.None))
            {
                var rect = button.transform as RectTransform;
                if (rect == null) continue;
                if (only != null && !rect.IsChildOf(only)) continue;

                // The centre of the control, in screen space. An overlay canvas needs a null
                // camera here; anything else needs its own.
                var canvas = button.GetComponentInParent<Canvas>();
                var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? canvas.worldCamera
                    : null;

                // ⚠️⚠️ THE RECT'S CENTRE, NOT `rect.position`. `position` is the PIVOT, and this
                // project moves pivots: `arrow_button.gd` parks a pennant's pivot on an
                // off-screen flagpole 300 px to its left so the entrance can unfurl from there.
                // Probing the pivot therefore aimed a third of a screen away from the control
                // and reported five working pennants as unreachable, which is a false positive
                // that reads exactly like the real bug.
                Vector3 centre = rect.TransformPoint(rect.rect.center);
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, centre);

                // ⚠️⚠️ "OFF SCREEN" AND "COVERED" ARE DIFFERENT FAILURES AND MUST NOT BE
                // REPORTED AS ONE. The first run of this probe called four seat rows and the
                // SPECTATE button unreachable; they were neither missing nor covered, they were
                // simply outside the batch-mode game view, which is not 16:9. Reported as one
                // category that reads as "the buttons are dead" and is the wrong thing to fix.
                //
                // It is still worth printing, because a control off the right edge at a narrow
                // aspect is a real complaint on somebody's monitor even when it is fine at 16:9.
                if (screenPoint.x < 0.0f || screenPoint.y < 0.0f
                    || screenPoint.x > Screen.width || screenPoint.y > Screen.height)
                {
                    // ⚠️ REPORTED, NOT FAILED. The batch runner's game view is 640x480 and these
                    // menus are authored for 16:9 with the canvas matching on HEIGHT, so at 4:3
                    // the right-hand column really is outside the viewport — a true statement
                    // about a 4:3 window and a useless one about a game that ships at 1600x900.
                    // Failing on it trains you to ignore this test.
                    report.AppendLine($"   {Path(button.transform)}: OFF SCREEN at " +
                                      $"{screenPoint} in {Screen.width}x{Screen.height}");
                    continue;
                }

                var data = new PointerEventData(system) { position = screenPoint };
                var hits = new List<RaycastResult>();
                system.RaycastAll(data, hits);

                if (hits.Count == 0)
                {
                    report.AppendLine($"   {Path(button.transform)}: NOTHING HIT  " +
                                      $"[{Describe(button)}]");

                    blocked.Add($"{screen}: {button.name} receives no raycast at all");
                    continue;
                }

                // A click lands on the button when the topmost hit is the button itself or one of
                // its own children. Anything else is a graphic sitting over it.
                var top = hits[0].gameObject.transform;
                bool mine = top == rect || top.IsChildOf(rect);

                if (mine)
                {
                    report.AppendLine($"   {Path(button.transform)}: ok");
                    continue;
                }

                report.AppendLine($"   {Path(button.transform)}: BLOCKED BY {Path(top)}"
                                  + $"  [{Describe(button)}]");

                blocked.Add($"{screen}: {button.name} is blocked by {Path(top)}");
            }
        }

        /// <summary>
        /// Everything about a control that can stop a click reaching it, in one line.
        ///
        /// ⚠️ EACH OF THESE HAS ALREADY BEEN A CAUSE ONCE. A zero scale left by an unfinished
        /// entrance, a CanvasGroup that blocks nothing, a targetGraphic that is not a raycast
        /// target, an interactable turned off by a mode this screen forgot to re-enable. Naming
        /// which one it is turns a rewrite into a one-line fix.
        /// </summary>
        private static string Describe(Selectable button)
        {
            var rect = (RectTransform)button.transform;
            var graphic = button.targetGraphic;
            var group = button.GetComponentInParent<CanvasGroup>();

            string blocks = group == null
                ? "no group"
                : $"group a={group.alpha:0.00} blocks={group.blocksRaycasts} on={group.interactable}";

            return $"rect={rect.rect.size} scale={rect.lossyScale.x:0.00} "
                   + $"active={button.gameObject.activeInHierarchy} on={button.interactable} "
                   + $"target={(graphic == null ? "NONE" : graphic.name)} "
                   + $"ray={(graphic != null && graphic.raycastTarget)} {blocks}";
        }

        private static GameObject FindByName(string name)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var hit = FindIn(root.transform, name);
                if (hit != null) return hit;
            }

            return null;
        }

        private static GameObject FindIn(Transform t, string name)
        {
            if (t.name == name) return t.gameObject;

            for (int i = 0; i < t.childCount; i++)
            {
                var hit = FindIn(t.GetChild(i), name);
                if (hit != null) return hit;
            }

            return null;
        }

        private static string Path(Transform t)
        {
            var parts = new List<string>();

            for (var step = t; step != null; step = step.parent) parts.Insert(0, step.name);
            return string.Join("/", parts);
        }
    }
}
