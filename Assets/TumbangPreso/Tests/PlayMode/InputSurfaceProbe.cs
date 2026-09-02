using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Asks, of every screen the game can put on a player, whether a CONTROLLER can walk it and a
    /// THUMB can hit it, at every shape a screen is ever seen in.
    ///
    /// ⚠️⚠️ IT DISCOVERS SCREENS RATHER THAN LISTING THEM, AND THAT IS THE WHOLE DESIGN. Three
    /// probes in this repository have already been left behind by a screen that moved:
    /// `docs/TODO.md` § 96 (the hub's one door, which nobody found), § 114 (`PlayerNameplate` no
    /// longer installed by any screen while `PlayerHubLayoutProbe` still drove it) and § 124.11
    /// (`LoadoutSurfaceProbe` knocking on a door § 122 had moved). All three are the same shape:
    /// **a hard-coded list of screens, and a move that did not update it.** `UiClickProbe` still
    /// carries one and is the fault pre-installed. So this probe asks the build settings which
    /// scenes exist and asks the assembly which screens exist, and a screen written next month is
    /// covered without anybody editing this file.
    ///
    /// ⚠️⚠️ AND A GREEN RUN HERE IS NOT A GOOD SCREEN, WHICH `CLAUDE.md` § 6.2a SAYS ABOUT EVERY
    /// PROBE IN THIS PROJECT. It asserts that a pad can reach every control and a thumb can hit
    /// it. It cannot tell you the focus order is confusing, that the first selected control is
    /// the wrong one, or that a button nobody looks at is a button nobody finds. Take the picture.
    /// </summary>
    public class InputSurfaceProbe
    {
        private const string OutPath = "Logs/input-surface.txt";

        /// <summary>
        /// ⚠️ THE SAME 120 FRAMES `UiClickProbe` SETTLES FOR, AND FOR ITS REASONS: the pennants
        /// unfurl over 0.45 s and a converted container needs a layout pass before its children
        /// have a size at all. Measuring earlier reports every control as zero-sized, which reads
        /// as "nothing meets the touch floor" and is a false positive on every screen at once.
        /// </summary>
        private const int SettleFrames = 120;

        /// <summary>
        /// Every control measured below the thumb floor, filled by the sweep above.
        ///
        /// ⚠️ STATIC SO THE MEASUREMENT SURVIVES BETWEEN THE SWEEP AND THE TEST THAT ASSERTS ON
        /// IT. NUnit builds a fresh fixture per test, and driving twelve resolutions across every
        /// screen twice, once to assert reachability and again to assert size, would double the
        /// most expensive probe in the suite for one list of strings.
        /// </summary>
        private static readonly List<string> ThumbFloorShortfalls = new List<string>();

        [UnityTest]
        public IEnumerator EveryScreenHasAFocusPathAndReachableTouchTargets()
        {
            var report = new StringBuilder();
            var faults = new List<string>();

            // ⚠️ CLEARED, BECAUSE IT IS STATIC AND A HALF-FILLED LIST IS WORSE THAN AN EMPTY ONE.
            // `TheFrontEndMeetsTheThumbFloor` only re-runs this sweep when the list is EMPTY, so a
            // sweep that threw part way through used to leave a partial list that the second test
            // then accepted as the whole measurement and reported as the worklist.
            ThumbFloorShortfalls.Clear();

            // ⚠️ THE THUMB LAYER IS FORCED ON. This editor has no touchscreen, so
            // `TouchHud.ShouldShow` is false and the controls would not exist to be measured.
            // Forcing it is the only way this layout is ever checked on this machine.
            bool previousForce = TouchHud.ForceVisible;
            TouchHud.ForceVisible = true;

            // ⚠️⚠️ THE RESTORE IS IN A `finally` AND THAT IS NOT TIDINESS: WITHOUT IT THIS
            // PROBE'S OWN CRASH TURNED 2 RED TESTS INTO 42. `TouchHud.ForceVisible` is a global
            // static, and the restore used to sit after the sweep, so **any** throw inside it
            // left the thumb layer switched on for every test that ran afterwards. On
            // 2026-09-03 a `MissingReferenceException` on a destroyed `Camera` did exactly that:
            // every later suite then built touch pads it would never build on this machine, hit
            // a second bug in the pad itself, and reported failures in steering, stun, tutorial,
            // volcanic and lobby code that had nothing wrong with them. Twenty of the twenty-two
            // red suites were downstream of this one line's placement.
            //
            // ⚠️ `yield return` INSIDE A `try` WITH A `finally` IS LEGAL in a C# iterator, and
            // the `finally` also runs when NUnit disposes an abandoned enumerator, which is the
            // case that matters here. A `try`/`catch` around a `yield` is what is illegal.
            try
            {
                foreach (var scene in DiscoverScenes())
                {
                    var load = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
                    yield return ProbeWait.Done(load, "scene load");

                    for (int i = 0; i < SettleFrames; i++) yield return null;

                    report.AppendLine($"=== scene {scene} ===");

                    // ⚠️⚠️ THE BASE SCREEN IS MEASURED WITH EVERY OVERLAY CLOSED, AND EACH OVERLAY
                    // IS THEN OPENED ON ITS OWN. Opening them all at once and measuring everything
                    // reported the main menu's five buttons as unreachable at every resolution,
                    // **and they were: the settings panel was open on top of them.** That is the
                    // screen working. `UiClickProbe` learned the same lesson in its own words:
                    // *"the screen underneath is SUPPOSED to be covered: an open character panel
                    // that let you press the map arrows behind it would be the bug. Probing
                    // everything reported a dozen correct behaviours as failures and buried the one
                    // real one."*
                    var overlays = DiscoverOverlays();

                    yield return Measure(scene, null, report, faults);

                    // ⚠️ OVERLAYS ARE DISCOVERED, NOT LISTED. `UiClickProbe` names four by hand and
                    // is one screen move away from probing nothing. Anything parked inactive with a
                    // pile of controls under it is a screen somebody opens.
                    foreach (var overlay in overlays)
                    {
                        if (overlay == null) continue;

                        // ⚠️⚠️ THE NAME IS TAKEN BEFORE THE SETTLE AND NULL IS RE-CHECKED AFTER IT,
                        // BECAUSE OPENING A SCREEN CAN DESTROY IT. `ConvertedSettingsPanel.Build`
                        // rebuilds its own rebind list and destroys the children it found, and other
                        // screens replace a parked placeholder with a freshly built one. Reading
                        // `overlay.name` after the wait threw `MissingReferenceException` and took
                        // the whole probe down with it, which is a probe reporting a crash instead of
                        // a result.
                        string overlayName = overlay.name;

                        overlay.SetActive(true);
                        for (int i = 0; i < SettleFrames; i++) yield return null;

                        if (overlay == null)
                        {
                            report.AppendLine($"--- overlay {overlayName} --- destroyed itself on open");
                            continue;
                        }

                        report.AppendLine($"--- overlay {overlayName} ---");
                        yield return Measure(scene, overlay.transform, report, faults);

                        if (overlay != null) overlay.SetActive(false);
                        yield return null;
                    }
                }
            }
            finally
            {
                TouchHud.ForceVisible = previousForce;
                TouchInput.ReleaseAll();
            }


            report.AppendLine();
            report.AppendLine($"=== {ThumbFloorShortfalls.Count} controls under the " +
                              $"{TouchMetrics.MinTargetUnits}-unit thumb floor ===");

            foreach (string shortfall in ThumbFloorShortfalls) report.AppendLine("  " + shortfall);

            Directory.CreateDirectory("Logs");
            File.WriteAllText(OutPath, report.ToString(), new UTF8Encoding(false));
            Debug.Log(report.ToString());

            Assert.IsEmpty(faults,
                "screens a controller cannot walk, or where a press lands on the wrong control:\n"
                + string.Join("\n", faults));
        }

        /// <summary>
        /// Every menu control is big enough for a thumb.
        ///
        /// ⚠️⚠️ THIS IS EXPECTED TO FAIL TODAY AND IT IS IN THE SUITE ANYWAY. The front end was
        /// authored for a mouse: the settings rows are about 34 units apart and the main menu's
        /// pennants about 60, against a 144-unit floor, and `ScreenFocus` cannot pad them up
        /// without covering their neighbours. **Fixing it is a layout pass on the converted
        /// screens**, which is `docs/TODO.md` § 125.13 and is not input work.
        ///
        /// ⚠️ `[Category("ThumbFloor")]` AND EXCLUDED FROM THE DEFAULT RUN, exactly as
        /// `AiDiagnosticProbe` is excluded with `WallClock` and for the same reason `CLAUDE.md`
        /// § 7 gives: a test whose red is expected trains people to ignore red. Run it on purpose
        /// with `-testCategory "ThumbFloor"` and the failure message is the worklist.
        ///
        /// ⚠️⚠️ IT IS NOT DELETED AND MUST NOT BE. **A known gap with no test is a gap that gets
        /// forgotten**, which is the whole argument of § 96, § 114 and § 124.11. This is the
        /// number, it is reproducible, and it will go green when somebody does the layout pass.
        /// </summary>
        [UnityTest]
        [Category("ThumbFloor")]
        public IEnumerator TheFrontEndMeetsTheThumbFloor()
        {
            if (ThumbFloorShortfalls.Count == 0)
                yield return EveryScreenHasAFocusPathAndReachableTouchTargets();

            Assert.IsEmpty(ThumbFloorShortfalls,
                $"{ThumbFloorShortfalls.Count} controls are smaller than a thumb. See " +
                $"{OutPath} and docs/TODO.md 125.13:\n"
                + string.Join("\n", ThumbFloorShortfalls));
        }

        /// <summary>
        /// Every scene in the build settings, which is every scene a player can reach.
        ///
        /// ⚠️ FROM THE BUILD SETTINGS RATHER THAN FROM A LITERAL. A scene added to the game is
        /// added there by definition, so this list cannot go stale the way a string array can.
        /// </summary>
        private static IEnumerable<string> DiscoverScenes()
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string name = Path.GetFileNameWithoutExtension(path);

                // The splash is a timed video with no controls at all; asserting a focus path on
                // it would be asserting that a cutscene is a menu.
                if (name == UI.SceneFlow.Splash) continue;

                yield return name;
            }
        }

        /// <summary>
        /// The screens parked inactive inside the one that is loaded.
        ///
        /// ⚠️ IT LOOKS FOR CONTROLS, NOT FOR A NAME. Matching `*Panel` would have found
        /// `SettingsPanel` and `CreditsPanel` and missed `LobbyJoinPanel`, which is built from
        /// code and parked, and every screen added since. An inactive object carrying three or
        /// more Selectables is a screen whatever it is called.
        /// </summary>
        private static List<GameObject> DiscoverOverlays()
        {
            var found = new List<GameObject>();

            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                  FindObjectsSortMode.None))
            {
                var go = t.gameObject;
                if (go.activeInHierarchy) continue;
                if (go.GetComponentsInChildren<Selectable>(true).Length < 3) continue;

                // Only the topmost inactive node; activating a child of one is meaningless.
                if (t.parent != null && !t.parent.gameObject.activeInHierarchy) continue;

                found.Add(go);
            }

            return found;
        }

        /// <summary>
        /// Drives every shape and asserts on the screens in scope.
        ///
        /// <paramref name="only"/> narrows the sweep to one overlay's own screens; null means the
        /// base screen with nothing open on top of it.
        /// </summary>
        private IEnumerator Measure(string scene, Transform only, StringBuilder report,
                                    List<string> faults)
        {
            var camera = Camera.main;

            if (camera == null)
            {
                faults.Add($"{scene}: no main camera, so no resolution can be driven");
                yield break;
            }

            // ⚠️ THE RESOLUTION IS DRIVEN THROUGH A RENDER TARGET, exactly as
            // `AspectRatioProbes` does and for the reason its class note gives:
            // `Screen.SetResolution` does nothing to `Screen.width` inside the editor, so a probe
            // built on it asserts against the batch runner's own window at every "resolution" and
            // passes for all of them.
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

            var previousTarget = camera.targetTexture;
            RenderTexture target = null;

            foreach (var (w, h, name) in ProbeResolutions.All())
            {
                // ⚠️⚠️ THE CAMERA IS RE-ACQUIRED EVERY SHAPE, AND NOT HOLDING IT COST A WHOLE
                // SUITE. `Camera.main` is read once at the top of this method, and the settle
                // frames below let a screen's own `Start` run: a screen that routes onward calls
                // `SceneFlow.Go`, the scene unloads, and the camera this method is holding is
                // destroyed while the loop still has shapes to drive. Writing `targetTexture` on
                // it then throws `MissingReferenceException`, which took this probe down and,
                // through the `TouchHud.ForceVisible` this test used to leak on a throw, twenty
                // suites after it.
                //
                // ⚠️⚠️ AND IT LOOKS THE CAMERA UP AGAIN RATHER THAN GIVING UP, WHICH THE FIRST
                // VERSION OF THIS GUARD DID NOT. Bailing out turned the crash into a TRUNCATION:
                // three scenes stopped being measured part way through their shape list and the
                // shortfall count that came back read as an improvement when it was a shorter
                // sweep. **A probe that measures less and says less is the failure mode this
                // whole file exists to prevent** (`CLAUDE.md` § 4a: a green probe for a screen
                // nobody reaches). The new scene has its own camera; picking it up continues the
                // sweep honestly, and the report says the swap happened.
                if (camera == null)
                {
                    camera = Camera.main;

                    if (camera == null)
                    {
                        report.AppendLine($"  {scene}: the camera was destroyed part way through " +
                                          "the sweep and the scene has no other, so the shapes " +
                                          "after this one were not measured.");
                        yield break;
                    }

                    report.AppendLine($"  {scene}: the camera was replaced part way through the " +
                                      "sweep, which means a screen on this scene changes scene " +
                                      "from its own Start. The shapes after this one are measured " +
                                      "against the new one.");

                    previousTarget = camera.targetTexture;

                    // The canvases collected above point at the camera that has gone.
                    foreach (var c in canvases)
                    {
                        if (c == null) continue;

                        c.worldCamera = camera;
                        c.planeDistance = camera.nearClipPlane + 0.01f;
                    }
                }

                var next = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = next;

                if (target != null) target.Release();
                target = next;

                // Three frames: the scaler recomputes in its Update, the layout rebuild lands the
                // frame after, and ScreenFocus rebuilds on the count change after that.
                for (int i = 0; i < 3; i++) yield return null;

                Canvas.ForceUpdateCanvases();

                foreach (var focus in Object.FindObjectsByType<ScreenFocus>(
                             FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (only != null && !focus.transform.IsChildOf(only)) continue;

                    focus.Rebuild();
                    CheckScreen(scene, name, w, h, focus, report, faults);
                }
            }

            if (camera != null) camera.targetTexture = previousTarget;
            if (target != null) target.Release();

            foreach (var c in canvases)
            {
                if (c == null) continue;
                c.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }

        private static void CheckScreen(string scene, string resolution, int w, int h,
                                        ScreenFocus focus, StringBuilder report,
                                        List<string> faults)
        {
            string where = $"{scene}/{focus.name} @ {resolution} ({w}x{h})";

            var controls = new List<Selectable>();

            foreach (var s in focus.GetComponentsInChildren<Selectable>(includeInactive: false))
            {
                if (!s.IsInteractable()) continue;
                if (s.navigation.mode == Navigation.Mode.None) continue;

                // ⚠️ THE SAME OWNERSHIP RULE `ScreenFocus` USES. A nested screen (an open
                // dropdown, the settings panel over the main menu) owns its own controls and is
                // checked on its own pass. Counting them here reported *"visits 5 of 49"* on the
                // main menu and called 44 perfectly reachable controls unreachable.
                if (ScreenFocus.OwnerOf(s) != focus) continue;

                controls.Add(s);
            }

            if (controls.Count == 0)
            {
                // A screen with no controls is a display, not a menu. Nothing to walk.
                report.AppendLine($"  {where}: no controls");
                return;
            }

            // ---- 1 · A CONTROLLER CAN REACH EVERY CONTROL ---------------------------------
            //
            // ⚠️⚠️ WALKED, NOT COUNTED. Asserting that the path holds N controls proves nothing
            // about whether pressing DOWN N times visits them: an explicit chain with one broken
            // link is a path that loops over a subset for ever, and the count is unchanged. This
            // starts at the first control and follows `selectOnDown` exactly as the input module
            // does.
            var visited = new HashSet<Selectable>();
            var walker = focus.Order.Count > 0 ? focus.Order[0] : null;

            if (walker == null)
            {
                faults.Add($"{where}: has {controls.Count} controls and no focus path at all. " +
                           "A pad press does nothing on this screen.");
                return;
            }

            for (int i = 0; i < controls.Count + 1 && walker != null; i++)
            {
                if (!visited.Add(walker)) break; // wrapped, which is correct
                walker = walker.navigation.selectOnDown;
            }

            foreach (var control in controls)
            {
                if (visited.Contains(control)) continue;

                faults.Add($"{where}: '{NamePath(control)}' is not on the controller focus path. " +
                           $"Walking DOWN from '{focus.Order[0].name}' visits {visited.Count} of " +
                           $"{controls.Count} controls and never arrives at it.");
            }

            // ---- 2 · A THUMB CAN HIT EVERY CONTROL ----------------------------------------
            var canvas = focus.GetComponentInParent<Canvas>();

            if (canvas == null)
            {
                faults.Add($"{where}: has controls and no Canvas above them.");
                return;
            }

            var canvasRt = (RectTransform)canvas.transform;

            foreach (var control in controls)
            {
                // ⚠️ IN THE CANVAS'S OWN UNITS. See `ScreenFocus.SizeInCanvasUnits`: world units
                // are not canvas units on a `ScreenSpaceCamera` canvas, and the first version of
                // this divided world size by `scaleFactor` and reported every control on every
                // screen as 0x0 against a 144 floor.
                Vector2 size = ScreenFocus.SizeInCanvasUnits(canvasRt,
                                                             ScreenFocus.HitRectOf(control));

                // ⚠️⚠️ THE THUMB-FLOOR SHORTFALL IS COLLECTED, NOT ASSERTED, AND THAT IS A
                // MEASURED DECISION RATHER THAN A FUDGE. Run against the front end as it stands,
                // **most menu controls are under 144 units and cannot be padded up to it without
                // covering their neighbour**: the settings rows are about 34 units apart and the
                // main menu's pennants about 60, so `ScreenFocus.ApplyTouchTargets` clamps the
                // growth and the shortfall remains. That is a true and useful finding about the
                // front end's readiness for a thumb, and it is a LAYOUT pass on the converted
                // screens rather than anything this input work can repair.
                //
                // ⚠️ ASSERTING IT HERE WOULD MAKE THIS PROBE PERMANENTLY RED, and `CLAUDE.md`
                // § 6.2a's warning cuts both ways: **a probe that is always red teaches the next
                // reader to skim the results exactly as a falsely green one does.** The numbers
                // go to `Logs/input-surface.txt` and to `docs/TODO.md` § 125.13, and
                // `TheFrontEndMeetsTheThumbFloor` is the test that fails on them, excluded from
                // the default run the way `AiDiagnosticProbe` is.
                //
                // ⚠️ WHAT IS ASSERTED UNCONDITIONALLY IS THE ONE THAT MATTERS TODAY: a press at a
                // control's centre must land on that control. That is check 3 below, and it is
                // the check that caught the padding bug.
                // ⚠️⚠️ A SCROLLBAR IS EXEMPT, AND THIS IS A DISTINCTION RATHER THAN AN EXCUSE.
                // `TouchMetrics.MinTargetUnits`'s own words are *"the smallest a touch target may
                // be"*, and the number comes from how accurately a thumb can PRESS a discrete
                // control. A scrollbar is dragged, not pressed, it is the one control on the
                // screen whose position already tells you where it is, and on a phone the list
                // itself is what the thumb actually drags. Held to the floor it would have to be
                // 144 units wide, which is a fifth of the settings panel spent on a readout.
                // `ConvertedSettingsPanel.BuildScrollbar` widens it to 44 on touch instead, which
                // is a catchable handle, and `ScreenFocus.FollowSelectionIntoView` is what makes
                // it optional rather than the only way a pad can reach row thirty.
                //
                // ⚠️ IT IS EXEMPTED HERE, WHERE THE NUMBER IS ASSERTED, RATHER THAN BY LEAVING IT
                // OFF THE FOCUS PATH. Taking it out of `ScreenFocus._order` would also take it
                // out of check 1 and check 3, and a scrollbar covering somebody else's control is
                // still a bug this probe should catch.
                if (control is Scrollbar)
                {
                    report.AppendLine($"  {where}: '{NamePath(control)}' is a scrollbar at " +
                                      $"{size.x:F0}x{size.y:F0}, exempt from the thumb floor.");
                }
                else if (size.x + 0.5f < TouchMetrics.MinTargetUnits
                         || size.y + 0.5f < TouchMetrics.MinTargetUnits)
                {
                    ThumbFloorShortfalls.Add(
                        $"{where}: '{NamePath(control)}' offers a " +
                        $"{size.x:F0}x{size.y:F0} unit thumb target against the " +
                        $"{TouchMetrics.MinTargetUnits}-unit floor.");
                }
            }

            // ---- 3 · AND THE PAD DID NOT COVER SOMETHING ELSE -----------------------------
            //
            // ⚠️⚠️ THIS IS THE HALF THAT MAKES GROWING HIT AREAS SAFE. `EnsureTouchTarget` grows
            // a 40-unit chip to 144 without asking what is beside it, so in a tight row it can
            // draw its transparent pad straight over its neighbour and eat that neighbour's
            // press. Raycasting each control's own centre afterwards is what catches it, and the
            // failure names the blocker, which is `UiClickProbe`'s rule: *"BackButton is blocked"
            // is not actionable.
            var system = EventSystem.current;

            if (system == null)
            {
                faults.Add($"{where}: no EventSystem, so nothing on this screen is pressable.");
                return;
            }

            foreach (var control in controls)
            {
                // ⚠️⚠️ A CONTROL SCROLLED OUT OF ITS OWN VIEWPORT IS NOT BLOCKED, IT IS BELOW THE
                // FOLD. The settings list is forty rows in a viewport that shows about ten, so a
                // raycast at row thirty's centre correctly lands on whatever is drawn at that
                // point on screen, which is the panel. Reporting that as "a press lands on
                // something else" is the same error `AspectRatioProbes.AssertInside` names when it
                // skips masked elements: *"asking them to fit is asking a scroll list to have no
                // scroll."* `UiClickProbe` scrolls each control into view instead; skipping is the
                // cheaper half of the same judgement and it does not perturb the layout of the
                // controls measured after it.
                if (!InsideOwnViewport(control)) continue;

                var hit = ScreenFocus.HitRectOf(control);

                // ⚠️ A RAYCAST TAKES A SCREEN POINT, AND A WORLD CENTRE IS NOT ONE. On a
                // `ScreenSpaceCamera` canvas the control's world position is a few units in front
                // of the camera, so feeding it in as a pixel coordinate raycasts the bottom-left
                // corner of the screen for every control on the screen.
                Vector2 centre = RectTransformUtility.WorldToScreenPoint(
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                    hit.TransformPoint(hit.rect.center));

                var data = new PointerEventData(system) { position = centre };
                var hits = new List<RaycastResult>();
                system.RaycastAll(data, hits);

                if (hits.Count == 0) continue; // off-screen at this shape; § 1 and § 2 cover that

                var top = hits[0].gameObject;

                if (top == control.gameObject) continue;
                if (top.transform.IsChildOf(control.transform)) continue;

                faults.Add($"{where}: a press at the centre of '{NamePath(control)}' lands on " +
                           $"'{NamePath(top.transform)}' instead.");
            }

            report.AppendLine($"  {where}: {controls.Count} controls, " +
                              $"{visited.Count} on the focus path");
        }

        /// <summary>
        /// True when a press AT THIS CONTROL'S CENTRE would land inside its scroll viewport.
        ///
        /// ⚠️⚠️ THE TEST IS THE CENTRE, NOT AN OVERLAP, AND THE OVERLAP VERSION PRODUCED ONE
        /// FALSE POSITIVE AT EVERY RESOLUTION. `Button_Z` is the row straddling the bottom edge
        /// of the settings viewport: its RECT overlaps, so an overlap test called it visible,
        /// while its CENTRE is below the fold and clipped. The probe then raycast that centre,
        /// correctly hit the panel, and reported a working screen as broken ten times over.
        ///
        /// **The question this check exists to ask is the same one the raycast asks**, so it has
        /// to be asked about the same point. A control whose centre is clipped is not pressable at
        /// its centre, and that is not a defect: it is a scroll list having scroll.
        /// </summary>
        private static bool InsideOwnViewport(Selectable control)
        {
            var scroll = control.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.viewport == null) return true;

            var rt = (RectTransform)control.transform;
            Vector3 centre = rt.TransformPoint(rt.rect.center);

            return WorldRect(scroll.viewport).Contains(new Vector2(centre.x, centre.y));
        }

        private static Rect WorldRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            float minX = Mathf.Min(corners[0].x, corners[2].x);
            float maxX = Mathf.Max(corners[0].x, corners[2].x);
            float minY = Mathf.Min(corners[0].y, corners[2].y);
            float maxY = Mathf.Max(corners[0].y, corners[2].y);

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private static bool Overlaps(Rect a, Rect b)
            => a.xMin < b.xMax && b.xMin < a.xMax && a.yMin < b.yMax && b.yMin < a.yMax;

        private static string NamePath(Component c) => NamePath(c.transform);

        private static string NamePath(Transform t)
        {
            var parts = new List<string>();

            for (var cursor = t; cursor != null; cursor = cursor.parent)
                parts.Add(cursor.name);

            parts.Reverse();
            return string.Join("/", parts);
        }

        /// <summary>
        /// The match's thumb layer, measured at every shape.
        ///
        /// ⚠️⚠️ IT IS A SEPARATE TEST BECAUSE THE MATCH LAYER IS NOT A SCREEN. It has no
        /// `Selectable` on it at all: `TouchButton` is a raw pointer handler, deliberately, so
        /// that a hold reaches `HeroAbility.CastsOnReleaseOnly` as a hold rather than as
        /// `Button.onClick`'s single up-edge. So the sweep above cannot see it, and the one
        /// control set the whole mobile port rests on would have gone unmeasured.
        /// </summary>
        [UnityTest]
        public IEnumerator TheThumbLayerFitsEveryPhoneShapeAndNothingOverlaps()
        {
            bool previousForce = TouchHud.ForceVisible;
            TouchHud.ForceVisible = true;

            var hud = TouchHud.Install();
            Assert.IsNotNull(hud, "TouchHud.Install returned null with ForceVisible set.");

            for (int i = 0; i < 10; i++) yield return null;

            var faults = new List<string>();
            var report = new StringBuilder();

            var camera = Camera.main;

            if (camera == null)
            {
                var go = new GameObject("ProbeCamera");
                camera = go.AddComponent<Camera>();
            }

            // ⚠️ ASKED FOR BY NAME, NOT SEARCHED FOR. `MenuKit.BuildCanvas` detaches to the scene
            // root, so the layer's canvas is not a child of the layer. See `TouchHud.Canvas`.
            var canvas = hud.Canvas;
            Assert.IsNotNull(canvas, "the thumb layer built no canvas.");

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = camera.nearClipPlane + 0.01f;

            var previousTarget = camera.targetTexture;
            RenderTexture target = null;

            foreach (var (w, h, name) in ProbeResolutions.All())
            {
                var next = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = next;

                if (target != null) target.Release();
                target = next;

                for (int i = 0; i < 3; i++) yield return null;
                Canvas.ForceUpdateCanvases();

                var canvasRect = (RectTransform)canvas.transform;
                float scale = canvas.scaleFactor > 0.0001f ? canvas.scaleFactor : 1.0f;

                var live = new List<TouchButton>();

                foreach (var button in hud.Buttons)
                    if (button != null && button.gameObject.activeInHierarchy) live.Add(button);

                // ---- every target is on screen ------------------------------------------
                foreach (var button in live)
                {
                    var rt = (RectTransform)button.transform;

                    if (!Inside(canvasRect, rt))
                        faults.Add($"{name} ({w}x{h}): '{button.name}' is off the edge of the " +
                                   "screen. A verb whose only touch control is off screen is a " +
                                   "verb a phone player does not have.");

                    float units = rt.rect.width;

                    if (units + 0.5f < TouchMetrics.MinTargetUnits)
                        faults.Add($"{name}: '{button.name}' is {units:F0} units against the " +
                                   $"{TouchMetrics.MinTargetUnits}-unit floor.");
                }

                // ---- and no two are close enough for one thumb to bridge ----------------
                //
                // ⚠️ CENTRE DISTANCE AGAINST THE TWO HALF-WIDTHS PLUS THE GAP, which is the same
                // arithmetic `TouchHud`'s layout constants are written against. A pair that fails
                // here is a pair where a press meant for one lands on the other, and on a phone
                // that means throwing when you meant to grab.
                for (int a = 0; a < live.Count; a++)
                {
                    for (int b = a + 1; b < live.Count; b++)
                    {
                        var ra = (RectTransform)live[a].transform;
                        var rb = (RectTransform)live[b].transform;

                        // ⚠️ BOTH CENTRES PUSHED INTO THE CANVAS'S OWN SPACE, not divided by
                        // `scaleFactor`. See `ScreenFocus.SizeInCanvasUnits`: world units are a
                        // third unit again on a `ScreenSpaceCamera` canvas, and mixing them with
                        // `rect.width` (which IS canvas units) compares two different quantities.
                        float distance = Vector2.Distance(
                            canvasRect.InverseTransformPoint(ra.TransformPoint(ra.rect.center)),
                            canvasRect.InverseTransformPoint(rb.TransformPoint(rb.rect.center)));

                        float required = (ra.rect.width + rb.rect.width) * 0.5f
                                         + TouchMetrics.MinGapUnits;

                        if (distance + 0.5f < required)
                            faults.Add($"{name}: '{live[a].name}' and '{live[b].name}' are " +
                                       $"{distance:F0} units apart and need {required:F0}.");
                    }
                }

                report.AppendLine($"  {name,-20} {w}x{h}  {live.Count} touch controls, " +
                                  $"scale {scale:F3}");
            }

            camera.targetTexture = previousTarget;
            if (target != null) target.Release();

            TouchHud.ForceVisible = previousForce;
            TouchInput.ReleaseAll();

            Debug.Log("[Touch] thumb layer\n" + report);

            Assert.IsEmpty(faults, "thumb layer faults:\n" + string.Join("\n", faults));
        }

        private static bool Inside(RectTransform canvas, RectTransform what)
        {
            var canvasRect = canvas.rect;
            var corners = new Vector3[4];
            what.GetWorldCorners(corners);

            for (int i = 0; i < 4; i++)
            {
                Vector3 local = canvas.InverseTransformPoint(corners[i]);

                if (local.x < canvasRect.xMin - 0.5f || local.x > canvasRect.xMax + 0.5f)
                    return false;

                if (local.y < canvasRect.yMin - 0.5f || local.y > canvasRect.yMax + 0.5f)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// A thumb press produces the same held-hold-release a key does, all the way to the
        /// ability layer's release edge.
        ///
        /// ⚠️⚠️ THIS IS THE ONE THAT WOULD HAVE CAUGHT THE FAULT `docs/TODO.md` § 124.1 RECORDS
        /// AGAINST THE BOTS. `AIController.Tap` alternated a key on and off every frame, so a
        /// bot's hold was one frame long and `AimRangeFor` returned the MINIMUM for every
        /// hold-to-aim power. A touch button wired to `Button.onClick` would have done exactly
        /// the same thing to every phone player, silently, and the five aimed powers would all
        /// have landed at their shortest reach with nothing erroring.
        /// </summary>
        [UnityTest]
        public IEnumerator AThumbHoldSurvivesAsAHoldAndNotAsATap()
        {
            bool previousForce = TouchHud.ForceVisible;
            TouchHud.ForceVisible = true;

            var hud = TouchHud.Install();
            Assert.IsNotNull(hud, "TouchHud.Install returned null with ForceVisible set.");

            for (int i = 0; i < 5; i++) yield return null;

            TouchButton throwButton = null;

            foreach (var button in hud.Buttons)
                if (button != null && button.Entry.Verb == Verb.SpecialAbility) throwButton = button;

            Assert.IsNotNull(throwButton, "the thumb layer has no SpecialAbility control.");

            var intent = new InputIntent();

            // Frame 1: the finger goes down.
            throwButton.SetHeld(true);
            intent.Set(Verb.SpecialAbility, TouchInput.Pressed(Verb.SpecialAbility));
            Assert.IsTrue(intent.JustPressed(Verb.SpecialAbility),
                          "a finger going down did not read as a press.");
            intent.CommitFrame();

            // Frames 2 to 30: it stays down. This is the whole point.
            for (int frame = 0; frame < 30; frame++)
            {
                intent.Set(Verb.SpecialAbility, TouchInput.Pressed(Verb.SpecialAbility));

                Assert.IsTrue(intent.Pressed(Verb.SpecialAbility),
                              $"the hold was dropped on frame {frame + 2}. A touch control that " +
                              "reports a tap pins every hold-to-aim power to its minimum range.");

                Assert.IsFalse(intent.JustPressed(Verb.SpecialAbility),
                               "a held finger re-reported a press edge, which would re-cast.");

                intent.CommitFrame();
                yield return null;
            }

            // The lift, which is the edge `HeroAbility.CastsOnReleaseOnly` casts on.
            throwButton.SetHeld(false);
            intent.Set(Verb.SpecialAbility, TouchInput.Pressed(Verb.SpecialAbility));

            Assert.IsTrue(intent.JustReleased(Verb.SpecialAbility),
                          "lifting the finger produced no release edge, so a hold-to-aim power " +
                          "would never cast at all.");

            TouchHud.ForceVisible = previousForce;
            TouchInput.ReleaseAll();
        }

        /// <summary>
        /// Photographs the thumb layer over the real street, at the shapes it is played at.
        ///
        /// ⚠️⚠️ `CLAUDE.md` § 6.2a: *"A GREEN LAYOUT PROBE IS NOT A GOOD SCREEN."* Everything
        /// above this method asserts that a thumb can hit a control and a pad can reach it, and
        /// **not one of those assertions can tell you the layout is good.** § 6.2b then says what
        /// to take a picture OF, and this obeys all four of its rows: the layer is shot OVER THE
        /// REAL BACKGROUND rather than an empty scene, at HIS OWN WINDOW SHAPE as well as the
        /// phone ones, in BOTH STATES (Classic hides the skill rail, Hero Strike shows it), and
        /// with the HUD still live underneath it.
        ///
        /// ⚠️ VERSIONED FILENAMES, per § 6.1: *"chat clients cache images by filename, so
        /// overwriting a render leaves the previous one on screen and the whole review is
        /// conducted against an image that no longer exists on disk."*
        /// </summary>
        [UnityTest]
        public IEnumerator PhotographTheThumbLayerOverTheRealStreet()
        {
            const string outDir = "Logs/shots-touch";
            Directory.CreateDirectory(outDir);

            bool previousForce = TouchHud.ForceVisible;
            TouchHud.ForceVisible = true;

            var previousMode = UI.SceneFlow.SelectedMode;

            foreach (var mode in new[] { Core.GameMode.Classic, Core.GameMode.HeroStrike })
            {
                UI.SceneFlow.SelectedMode = mode;

                // ⚠️⚠️ THE ARENA, NOT THE SETUP SCREEN, AND THE FIRST VERSION SHOT THE WRONG
                // SCREEN ENTIRELY. `CLAUDE.md` § 6.2b: *"That screen HAD a render. Four of them,
                // green, at nine resolutions. Every one was of a different screen than the one he
                // opened."* Loading `MatchSetup` put the boot sign-in over it, so the photograph
                // of the thumb layer was a photograph of a login form with no controls on it at
                // all. `Eskinita` is the street `GameplayShots` uses and it is where these
                // controls are actually held.
                var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
                yield return ProbeWait.Done(load, "scene load");

                for (int i = 0; i < 120; i++) yield return null;

                // ⚠️⚠️ A LIVE ROUND IS STARTED, BECAUSE THE CONTROLS ONLY EXIST IN ONE. The first
                // two versions of this shot got it wrong in opposite directions: one photographed
                // the layer sitting on top of the LOBBY (🧑: *"yo why the buttons here"*,
                // *"shouldnt the buttons only be in the game"*), and the fix for that switched
                // every menu off and produced a picture of the controls on a BLACK FRAME, which
                // fails `CLAUDE.md` § 6.2b's second row just as badly: *"over the real
                // background, never an empty scene."*
                //
                // ⚠️ `SliceRunner.Begin` IS THE PATH A PLAYER TAKES, which is why `GameplayShots`
                // uses it and calls the alternative out: `RoundDirector.BeginRound` only flips
                // the round flags, while everything that PLACES the world hangs off
                // `MatchDirector.RoundStarted`, which only the runner raises. A shot taken the
                // other way is a shot of the probe rather than of the game.
                var runner = Object.FindFirstObjectByType<SliceRunner>();

                if (runner != null) runner.Begin();
                else GameServices.Round?.BeginRound();

                yield return new WaitForSecondsRealtime(3.0f);

                var hud = TouchHud.Install();

                if (hud == null)
                {
                    Debug.LogWarning("[Touch] no layer to photograph.");
                    continue;
                }

                hud.ApplyModeVisibility();
                hud.ApplyLayout();

                for (int i = 0; i < 10; i++) yield return null;

                foreach (var (w, h, name) in ProbeResolutions.Phone)
                {
                    yield return Shoot(hud, w, h,
                        $"{outDir}/touch-{mode}-{name.Replace(' ', '-').Replace(':', '-')}-v3.png");
                }

                // ⚠️⚠️ THE RUNNER IS TAKEN DOWN AGAIN, AND `GameplayShots` RECORDS WHAT LEAVING IT
                // COSTS: `Begin` subscribes it to the `DontDestroyOnLoad` directors, so it
                // outlives this test, hears the NEXT test's `RoundStarted` and runs `ResetWorld`,
                // teleporting every seat back to its spawn mark. That was measured as
                // `MatchRunTests.AnAttackerMovesFreelyThroughTheChalk` failing in a batch and
                // passing alone, which is the signature of exactly this.
                if (runner != null) Object.DestroyImmediate(runner.gameObject);
            }

            UI.SceneFlow.SelectedMode = previousMode;
            TouchHud.ForceVisible = previousForce;
            TouchInput.ReleaseAll();
        }

        /// <summary>
        /// One frame: the scene, then the UI on top, into a PNG.
        ///
        /// ⚠️ THE UI CANVASES ARE PUT BACK TO OVERLAY AFTERWARDS. `GameplayShots` carries the
        /// same note and the same reason: leaving them pointed at a destroyed camera blanks the
        /// HUD for every shot after this one.
        /// </summary>
        private static IEnumerator Shoot(TouchHud hud, int w, int h, string path)
        {
            var camera = Camera.main;
            if (camera == null) yield break;

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

            var previous = camera.targetTexture;
            // ⚠️⚠️ sRGB IS A CONSTRUCTOR ARGUMENT, NOT A PROPERTY: `RenderTexture.sRGB` is
            // read-only and assigning it does not compile. The colour space has to be chosen when
            // the target is created because it decides the format the GPU allocates.
            //
            // ⚠️ AND IT MATTERS FOR THE REASON `GameplayShots` RECORDS: a linear target read
            // straight into an RGB24 texture writes linear values into a file that will be
            // displayed as sRGB, so mid-grey 0.5 lands at 0.21 and the street reads as asphalt at
            // night. Every render of this layer would have been judged against a picture that was
            // a stop and a half too dark.
            var target = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32,
                                           RenderTextureReadWrite.sRGB);

            camera.targetTexture = target;

            for (int i = 0; i < 3; i++) yield return null;

            Canvas.ForceUpdateCanvases();
            camera.Render();

            RenderTexture.active = target;

            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            camera.targetTexture = previous;

            foreach (var c in canvases)
            {
                if (c == null) continue;
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.worldCamera = null;
            }

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            target.Release();

            Debug.Log($"[Touch] wrote {path}");
        }

        /// <summary>
        /// ⚠️ REFLECTION KEEPS THIS HONEST WITHOUT A LIST. If a screen type is added with a
        /// public static opener, it is opened and measured. The invocation is allowed to fail:
        /// a screen that needs a live match or a signed-in account cannot be built here, and
        /// treating that as a fault would make the probe red for reasons that are not defects.
        /// What is NOT allowed to fail is a screen that DOES open and has no focus path.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryCodeBuiltScreenWithAnOpenerGetsAFocusPath()
        {
            var faults = new List<string>();
            var opened = new List<string>();

            var assembly = typeof(UI.MenuKit).Assembly;

            foreach (var type in assembly.GetTypes())
            {
                if (!typeof(MonoBehaviour).IsAssignableFrom(type)) continue;

                var opener = type.GetMethod("Open",
                    BindingFlags.Public | BindingFlags.Static, null, System.Type.EmptyTypes, null);

                if (opener == null) continue;

                // ⚠️⚠️ COUNTED BEFORE AND AFTER, NOT SEARCHED FOR UNDER THE OPENED OBJECT.
                // `MenuKit.BuildCanvas` DETACHES its canvas to the scene root when the parent it
                // is handed sits inside another canvas, because a nested canvas silently ignores
                // its own scaler and sorting order. So a screen's `ScreenFocus` is very often NOT
                // a child or a parent of the component that opened it: the first version of this
                // test looked in both directions and reported `TouchLayoutScreen` as having no
                // focus path, about a screen built by the one method that always installs one.
                // **A new ScreenFocus appearing in the scene is the honest question.**
                int before = Object.FindObjectsByType<ScreenFocus>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

                object built;

                try { built = opener.Invoke(null, null); }
                catch { continue; } // needs a match, an account, or a scene we are not in

                if (built == null) continue;

                opened.Add(type.Name);

                for (int i = 0; i < 30; i++) yield return null;

                int after = Object.FindObjectsByType<ScreenFocus>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

                var behaviour = built as MonoBehaviour;

                bool hasFocus = after > before
                                || (behaviour != null
                                    && (behaviour.GetComponentInChildren<ScreenFocus>(true) != null
                                        || behaviour.GetComponentInParent<ScreenFocus>() != null));

                if (!hasFocus)
                {
                    faults.Add($"{type.Name}: opened a screen with no ScreenFocus on it. It did " +
                               "not come through `MenuKit.BuildCanvas` or `ConvertedScreen`, " +
                               "which are the two places that install one.");
                }
            }

            Debug.Log($"[Input] opened and checked: {string.Join(", ", opened)}");

            Assert.IsEmpty(faults, "code-built screens with no focus path:\n"
                                   + string.Join("\n", faults));
        }
    }
}
