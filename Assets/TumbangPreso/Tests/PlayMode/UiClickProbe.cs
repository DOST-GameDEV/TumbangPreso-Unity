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
        /// ⚠️ SCENES ONLY. `SettingsPanel`, `CreditsPanel` and `CharacterSelectPanel` are
        /// OVERLAYS that live inside these screens rather than scenes of their own, so asking the
        /// build settings for them reports three false failures. They are opened in place below.
        ///
        /// ⚠️ `ModeSelect` AND `MultiplayerSetup` ARE STILL PROBED THOUGH NOTHING NAVIGATES TO
        /// THEM. Both are the kept fallbacks of `docs/TODO.md` § 68.3, and a fallback nobody
        /// checks is not a fallback; the whole value of keeping them is that the revert is one
        /// line rather than a repair.
        /// </summary>
        private static readonly string[] Screens =
        {
            "MainMenu", "ModeSelect", "MatchSetup", "MultiplayerSetup", "MatchResult",
        };

        /// <summary>The in-place overlays, and the screen each one lives on.</summary>
        private static readonly (string Screen, string Node)[] Overlays =
        {
            ("MainMenu", "SettingsPanel"),

            // ⚠️ `TutorialPanel` IS GONE FROM THE SCENE, NOT MERELY UNLINKED. The six-page
            // reference card was deleted on 2026-08-28 and TUTORIAL now enters the playable
            // route directly; see `ConvertedMainMenu.Wire`. Probing for it would report a
            // missing overlay on every run.
            ("MainMenu", "CreditsPanel"),
            ("MatchSetup", "CharacterSelectPanel"),

            // ⚠️ BUILT FROM CODE AND PARKED INACTIVE, NOT AUTHORED IN THE .unity.
            // `LobbyJoinPanel.Build` constructs the card and calls `SetActive(false)` on it, so
            // it is a child of the lobby's canvas from the first frame and `FindByName` reaches
            // it. It carries the JOIN CODE OR IP ADDRESS field, which is one of the two controls
            // that widening this probe to `InputField` exists to check, and it is behind a
            // button rather than on the screen: nothing else in the suite ever opens it.
            ("MatchSetup", "LobbyJoinPanel"),
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
                yield return ProbeWait.Done(load, "scene load");

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

        /// <summary>
        /// Scrolls <paramref name="target"/> into its ScrollRect's viewport, if it is in one.
        ///
        /// ⚠️ IT MOVES THE CONTENT, NOT THE TARGET. The control keeps its place in the layout;
        /// what changes is which part of the content the viewport is showing, which is exactly
        /// what a player's wheel does.
        ///
        /// ⚠️ AND IT SETTLES THE LAYOUT SYNCHRONOUSLY. `Probe` is not a coroutine and cannot
        /// yield a frame, so the canvas is forced to rebuild here; without that the positions
        /// read back a frame stale and the control is judged where it used to be.
        /// </summary>
        private static void ScrollIntoView(RectTransform target)
        {
            var scroll = target.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.content == null || scroll.viewport == null) return;
            if (!target.IsChildOf(scroll.content)) return;

            Canvas.ForceUpdateCanvases();

            var content = scroll.content;
            var viewport = scroll.viewport;

            // How far the content has to move, in its own space, to put the target's centre on
            // the viewport's centre.
            Vector3 targetCentre = target.TransformPoint(target.rect.center);
            Vector3 viewCentre = viewport.TransformPoint(viewport.rect.center);

            Vector3 deltaWorld = viewCentre - targetCentre;
            Vector2 local = content.parent.InverseTransformVector(deltaWorld);

            var pos = content.anchoredPosition;
            if (scroll.vertical) pos.y += local.y;
            if (scroll.horizontal) pos.x += local.x;

            // ⚠️ CLAMPED TO THE RANGE THE SCROLLBAR ACTUALLY HAS. Content shorter than the
            // viewport cannot scroll at all, and pushing it anyway would drag a perfectly
            // visible control OUT of view and invent a failure.
            float slackY = Mathf.Max(0.0f, content.rect.height - viewport.rect.height);
            float slackX = Mathf.Max(0.0f, content.rect.width - viewport.rect.width);

            pos.y = Mathf.Clamp(pos.y, 0.0f, slackY);
            pos.x = Mathf.Clamp(pos.x, -slackX, 0.0f);

            content.anchoredPosition = pos;

            Canvas.ForceUpdateCanvases();
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

            foreach (var button in Object.FindObjectsByType<Selectable>(FindObjectsInactive.Exclude,
                                                                        FindObjectsSortMode.None))
            {
                // ⚠️⚠️ BUTTONS AND DROPDOWNS, AND THIS USED TO BE BUTTONS ALONE. The settings
                // panel's colour picker started as a Button, was caught by this probe when it
                // landed below the fold, and was then rebuilt as a Dropdown to show its
                // swatches. That rebuild would have silently dropped it out of this test: a
                // control that is unreachable is unreachable whatever class it is, and the one
                // control with a history of being unreachable would have been the one no longer
                // checked.
                //
                // ⚠⚠ AND SLIDERS, WIDENED DELIBERATELY AFTER ALL FOUR OF THEM SHIPPED DEAD.
                // The report was that the settings sliders were "hardcoded and broken" and that
                // the volume could not be changed with the mouse. They were not hardcoded:
                // `ClearStrayRaycastTargets` muted every graphic under them, because a Slider
                // keeps its Background, Fill and Handle on CHILD nodes and the sweep only
                // recognised a hit area on the control's own node. This probe is the only check
                // in the project that could have seen it, and it was the one class of Selectable
                // on the screen it did not enumerate.
                //
                // ⚠️ IT IS A DENYLIST RATHER THAN EVERY Selectable ON PURPOSE. Toggles are also
                // Selectables, and several of them sit below the fold on this same screen;
                // sweeping them all in is a bigger claim than this probe has ever made and would
                // fail on controls nobody has reported. Widen it deliberately, not by accident.
                //
                // ⚠️⚠️ AND `InputField` IS IN AS OF 2026-08-29, DELIBERATELY, BECAUSE THE
                // EXCLUSION HID THE ONLY TWO CONTROLS ANYBODY HAS REPORTED DEAD SINCE. 🧑 named
                // both on the same day: *"sa lobby hindi nagana yung player name, hindi makapag
                // input ng name"* and *"hindi maka input ng code and lobby code sa lobby"*. They
                // are `LobbyChrome.BuildNameField`'s `PlayerNameEdit` and
                // `LobbyJoinPanel.BuildEntryRow`'s `JoinAddressEdit`, and they are both
                // `InputField`s: the one class of Selectable on the one screen that this probe
                // was written to check was the class it stepped over. The note above said to
                // widen it deliberately rather than by accident, and this is that.
                //
                // ⚠️ A FIELD IS JUDGED ON EXACTLY THE SAME TERMS AS A BUTTON, and that is the
                // point rather than a shortcut. `InputField.ActivateInputField` is reached by the
                // EventSystem's pointer-down on the field's own graphic, so "the topmost hit at
                // my centre is me or my child" IS the question of whether a player can type in
                // it. `LobbyChat` already works around a screen that fails this, by calling
                // `ActivateInputField` from its own `OnPointerClick`; that workaround is evidence
                // the failure is real and reachable, not a reason to stop checking for it.
                if (!(button is Button) && !(button is Dropdown) && !(button is Slider)
                    && !(button is InputField)) continue;

                var rect = button.transform as RectTransform;
                if (rect == null) continue;
                if (only != null && !rect.IsChildOf(only)) continue;

                // ⚠️⚠️ A CONTROL INSIDE A SCROLL PANEL IS BROUGHT INTO VIEW FIRST, AND WITHOUT
                // THIS THE PROBE CANNOT SEE PAST THE FOLD. The Settings card is taller than any
                // viewport by design: eleven rebind rows, then MOUSE, then DISPLAY, then AUDIO.
                // Everything below the fold is clipped by the viewport's mask, and a clipped
                // graphic takes no raycast, so it lands in the NOTHING HIT branch below and is
                // reported as a button a player cannot click. It is nothing of the kind: the
                // player scrolls to it, which is what the panel's ScrollRect is for.
                //
                // The blind spot went unnoticed because until now every Button on that screen
                // lived in `BindingsList`, the second child of the content and comfortably above
                // the fold. The DISPLAY section's only controls were Toggles, which this probe
                // does not enumerate. The first Button added down there failed instantly.
                //
                // ⚠️ SCROLLED, THEN JUDGED ON THE SAME TERMS AS ANYTHING ELSE. This does not
                // excuse the control: after scrolling it must still take the raycast and still
                // be the topmost hit. A button genuinely covered by a graphic inside the scroll
                // panel fails exactly as it did before, which is the case the probe exists for.
                ScrollIntoView(rect);

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
