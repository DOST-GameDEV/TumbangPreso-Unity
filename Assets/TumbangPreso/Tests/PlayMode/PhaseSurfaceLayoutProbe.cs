using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The two surfaces phases 1 to 4 built that `PlayerHubLayoutProbe` cannot reach: the
    /// end-of-match XP block on `MatchResult`, and the telemetry row on the settings panel.
    ///
    /// ⚠️⚠️ `FUTURE.md` § 4.5.3 NAMES BOTH OF THEM AS UNCOVERED AND THAT LIST WAS RIGHT.
    /// `PlayerHubLayoutProbe` drives the hub, the sign-in screen and the nameplate, and it is
    /// deliberately scoped to those canvases, so neither of these has ever been measured at any
    /// resolution. The XP block is Phase 4's only in-game surface and the telemetry row is
    /// Phase 3's only one; between them they are everything a player can see of two phases.
    ///
    /// ⚠️⚠️ AND ONE OF THEM HAS ALREADY BEEN CAUGHT ONCE BY ACCIDENT. `docs/TODO.md` § 92.5
    /// records `MatchResult._yourMatchLine` shipping at **17 units**, under the 18-unit floor,
    /// found only because the hub probe briefly searched the whole scene instead of its own
    /// canvas. A fault that can only be found by a probe pointing at the wrong thing is a fault
    /// nobody is looking for.
    ///
    /// ⚠️ THE SAME NINE RESOLUTIONS, THE SAME `preferredWidth` MEASUREMENT AND THE SAME FONT
    /// FLOOR as `AspectRatioProbes`, `HudOverflowProbe` and `PlayerHubLayoutProbe`. If this list
    /// ever disagrees with theirs, one of the four files is testing a screen the game does not
    /// ship.
    /// </summary>
    public class PhaseSurfaceLayoutProbe
    {
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

        private GameObject _host;
        private Camera _camera;
        private RenderTexture _target;
        private readonly List<Canvas> _canvases = new List<Canvas>();

        /// <summary>
        /// ⚠️⚠️ THE RESULT CANVAS IS DESTROYED BY NAME AND NOT BY DESTROYING `_host`.
        /// `MatchResult.Build` creates `ResultCanvas` as a NEW GameObject and parents it under
        /// whatever `Hud` is in the scene rather than under the component's own object, so
        /// destroying the host leaves the canvas behind in whichever scene the previous suite
        /// loaded. A leaked always-on canvas is precisely the fault § 92.7 records the full
        /// PlayMode sweep finding: a new piece of chrome covering the corner of every panel on a
        /// screen it has nothing to do with, invisible to the probe that created it.
        /// </summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var c in _canvases)
                if (c != null) c.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvases.Clear();

            if (_camera != null) _camera.targetTexture = null;
            if (_target != null) _target.Release();
            if (_host != null) Object.Destroy(_host);

            var leaked = Root("ResultCanvas");
            if (leaked != null) Object.Destroy(leaked.gameObject);

            // ⚠️⚠️ AND IT LEAVES AN EMPTY SCENE, FOR THE REASON `MatchRecordIdentityProbe`'s
            // teardown records at length. This one loads `MainMenu`, which has a
            // `PlayerNameplate` and a `PlayerHub` of its own, and `PlayerHubLayoutProbe` looks
            // its hub up by object name: handing it a menu means `GameObject.Find` can answer
            // with the MENU's hub, which is closed, and the probe measures a screen nobody
            // opened. It reported "the PROFILE tab drew no labels at all" about a tab that was
            // fine, on a hub it had never touched.
            yield return Blank();
        }

        /// <summary>Replaces every loaded scene with one empty one.</summary>
        private static IEnumerator Blank()
        {
            var blank = SceneManager.CreateScene($"PhaseSurfaceBlank{Time.frameCount}");
            SceneManager.SetActiveScene(blank);

            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene == blank || !scene.isLoaded) continue;

                var unload = SceneManager.UnloadSceneAsync(scene);
                while (unload != null && !unload.isDone) yield return null;
            }

            yield return null;
        }

        /// <summary>Activates every inactive ancestor so a rect can be measured. See the note in
        /// the XP case for why the hierarchy may arrive switched off.</summary>
        private static void Raise(Transform target)
        {
            for (var t = target; t != null; t = t.parent)
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);

            var canvas = target.GetComponent<Canvas>();
            if (canvas != null) canvas.enabled = true;
        }

        // -------------------------------------------------------------------
        // § PHASE 4: the end-of-match XP block
        // -------------------------------------------------------------------

        /// <summary>
        /// The level line, the bar and the detail line, driven with the longest strings the
        /// screen can produce.
        ///
        /// ⚠️⚠️ IT DRIVES `ShowProgression` THROUGH REFLECTION, AND THE TWO ALTERNATIVES ARE
        /// BOTH WORSE. **Making it public would be adding a seam to shipping code so a test can
        /// reach it**, which `FUTURE.md` § 4.5.6 rules out by name. **Routing through the real
        /// path is actively dangerous**: the block is filled from `CareerStore.LastAward`, which
        /// is only ever written by `CareerStore.Record`, which saves `career.json` and calls
        /// `FlushAsync`. The editor and the built player share `Application.persistentDataPath`,
        /// and `PlayerAccount` signs in anonymously at boot, so a probe taking that path would
        /// write a fabricated match into the player's real career **and submit it to their live
        /// account**. Reflection touches nothing outside this process.
        ///
        /// ⚠️ THE TEST SUPPLIES THE INPUTS AND THE SCREEN PRODUCES THE STRINGS. The award comes
        /// out of `ProgressionRules.Award`, the same call the game makes, so what gets measured is
        /// the shipping code's own formatting rather than a string this file invented. A probe
        /// that typed `"LEVEL 12 · LEVEL UP · +215 XP"` in itself would keep passing after the
        /// screen changed how it writes that line.
        ///
        /// ⚠️ THE PROFILE IS BUILT TO LEVEL UP ON THIS MATCH, which is the longest of the four
        /// headlines the block can draw. Measuring the short one proves nothing about the one
        /// that overflows.
        /// </summary>
        [UnityTest]
        public IEnumerator TheEndOfMatchXpBlockFitsItsBoxAtEveryShippedResolution()
        {
            var report = new StringBuilder();

            _host = new GameObject("ResultProbeHost");
            var board = _host.AddComponent<MatchResult>();
            yield return null;

            var (profile, record, line) = LevelUpFixture();
            var award = ProgressionRules.Award(profile, record, line);
            Assert.IsNotNull(award, "ProgressionRules.Award paid nothing for a clean winning match");
            Assert.Greater(award.LevelAfter, award.LevelBefore,
                "the fixture did not level up, so this probe would measure the short headline");

            var show = typeof(MatchResult).GetMethod("ShowProgression",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(show,
                "MatchResult.ShowProgression is gone or renamed. It is the only way into the XP " +
                "block that does not write the player's real career; read this test's header " +
                "before pointing it at something else.");

            show.Invoke(board, new object[] { award, profile });
            yield return null;

            var canvas = Root("ResultCanvas");
            Assert.IsNotNull(canvas, "MatchResult built no ResultCanvas");

            // ⚠️⚠️ THE BOARD IS BUILT HIDDEN AND THE PROBE HAS TO RAISE IT, WHICH IS NOT THE
            // SAME AS THE PROBE FAKING IT. `MatchResult.Awake`'s own note says the CANVAS hides
            // rather than the object, so that `OnEnable` still fires and the component still
            // subscribes to `MatchEnded`; and `Build` parents the canvas under whatever `Hud` is
            // in the scene, which in a PlayMode sweep is another suite's arena and may itself be
            // inactive. **`activeInHierarchy` therefore answers a question about the scene the
            // probe happens to have inherited, not about the XP block.** The block's own
            // visibility is `activeSelf` on the three objects `ShowProgression` toggles, so that
            // is what is asserted, and the ancestors are raised so the rects can be measured.
            Raise(canvas);
            yield return null;

            var headline = Find(canvas, "XpHeadline");
            Assert.IsNotNull(headline, "the XP block has no headline");
            Assert.IsTrue(headline.gameObject.activeSelf,
                "ShowProgression was given a real award and left the XP block hidden");
            Assert.IsNotEmpty(headline.GetComponent<Text>().text,
                "the XP headline is active and empty");

            yield return Drive(canvas);

            foreach (var (w, h, name) in Resolutions)
            {
                yield return Resize(w, h);

                int measured = Measure(canvas, name, "match-result/xp", report);
                Assert.Greater(measured, 0,
                    $"{name}: the results board drew no labels, so this proves nothing");

                // ⚠️ THE BAR IS ASSERTED SEPARATELY FROM THE LABELS. It is an Image, so no
                // amount of text measurement can see it running off the card, and a progress bar
                // that is off screen is the one part of this block a player actually looks at.
                var bar = Find(canvas, "XpBar");
                Assert.IsNotNull(bar, "the XP block has no bar");
                AssertInside((RectTransform)canvas, (RectTransform)bar.transform, name, "the XP bar");

                report.AppendLine($"{name,-14} {w}x{h}  xp block ok, {measured} labels");
            }

            Debug.Log($"[PhaseSurfaceLayoutProbe] XP block\n{report}");
        }

        // -------------------------------------------------------------------
        // § PHASE 3: the telemetry row on the settings panel
        // -------------------------------------------------------------------

        /// <summary>
        /// The opt-out picker and the sentence under it that says what is actually collected.
        ///
        /// ⚠️⚠️ THE SENTENCE IS THE PART THAT MATTERS AND IT IS THE PART THAT CAN BREAK
        /// SILENTLY. `ConvertedSettingsPanel.BuildTelemetryNote`'s own header records the trap
        /// twice over: legacy `Text` defaults to WRAP, so a sentence longer than its box is cut
        /// in half with nothing reported, and the note sets `verticalOverflow = Overflow` to
        /// escape it. **A privacy disclosure that is silently truncated is worse than one that is
        /// absent**, because the half that survives is "Counts only: matches played, modes, maps,
        /// picks and frame rate" and the half that goes is "No names, no chat, nothing you type."
        ///
        /// ⚠️ THIS PROBE IS SCOPED TO THE SETTINGS CANVAS, for the reason `PlayerHubLayoutProbe`
        /// gives about reporting another screen's fault under this screen's name.
        ///
        /// ⚠️⚠️ `LobbyChrome.SettingsSummary` IS 16 UNITS AND IS NOT IN SCOPE AND IS NOT A BUG.
        /// The hub probe tripped over it once and § 92.6 left it unowned. It is on the LOBBY, not
        /// on this panel, it is an authored exception with 🧑's own instruction beside it
        /// (*"make font size here smaller"*), and `LobbyChrome`'s constant carries the argument:
        /// it is three words restated at 26 units by the drawer directly beneath it. **Recorded
        /// here so the next probe to find it stops looking rather than "fixing" a decision.**
        /// </summary>
        [UnityTest]
        public IEnumerator TheTelemetryRowFitsItsBoxAtEveryShippedResolution()
        {
            var report = new StringBuilder();

            // ⚠️⚠️ `MainMenu`, NOT `SettingsPanel`, AND THE FIRST RUN OF THIS PROBE IS HOW THAT
            // WAS LEARNED. `Assets/TumbangPreso/Scenes/Ui/SettingsPanel.unity` exists on disk and
            // **is not in the build profile**: the panel is a `Panel` instanced into the title
            // screen at runtime, which is what `Panel`'s header says and what the scene list in
            // `ProjectSettings/EditorBuildSettings.asset` confirms. Loading it by name fails with
            // *"couldn't be loaded because it has not been added to the active build profile"*,
            // which reads exactly like a missing scene. **A screen a player reaches through the
            // menu has to be probed through the menu**, or the probe is testing an asset the
            // build does not ship.
            var load = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;
            for (int i = 0; i < 30; i++) yield return null;

            var panel = Object.FindFirstObjectByType<ConvertedSettingsPanel>(FindObjectsInactive.Include);
            Assert.IsNotNull(panel,
                "the SettingsPanel scene built no ConvertedSettingsPanel, so the telemetry " +
                "opt-out has no home. FUTURE.md PHASE 3 puts it here and docs/TODO.md § 92.4 " +
                "records it as the one phase 1 to 3 surface that did NOT move.");
            panel.gameObject.SetActive(true);
            for (int i = 0; i < 10; i++) yield return null;

            var canvas = panel.GetComponentInParent<Canvas>(true);
            Assert.IsNotNull(canvas, "the settings panel is not under a canvas");

            var row = Find(canvas.transform, "TelemetryRow");
            var note = Find(canvas.transform, "TelemetryNote");

            Assert.IsNotNull(row,
                "there is no TelemetryRow on the settings panel. The opt-out is the only thing " +
                "standing between Phase 3 and collecting from somebody who said no.");
            Assert.IsNotNull(note,
                "the telemetry opt-out has no note saying what is collected. The picker without " +
                "the sentence is a switch with no label on what it switches.");

            var noteText = note.GetComponentInChildren<Text>(true);
            Assert.IsNotNull(noteText, "the telemetry note built no label");
            Assert.GreaterOrEqual(noteText.fontSize, MenuKit.MinReadableUnits,
                $"the telemetry disclosure is {noteText.fontSize} units, under the " +
                $"{MenuKit.MinReadableUnits}-unit floor. It is the one sentence on this screen a " +
                "player has to be able to read.");
            // ⚠️⚠️ BOTH AXES ARE ASSERTED AND ONLY ONE OF THEM USED TO BE SET, WHICH IS WHAT
            // THIS PROBE FOUND ON ITS FIRST GREEN RUN. The note allowed a second line and never
            // wrapped to one, so at 1280x720 it drew **795 px of sentence in a 688 px box** and
            // the clause that fell off the edge was *"No names, no chat, nothing you type."*
            // Asserting the vertical mode alone would have passed the broken version.
            Assert.AreEqual(HorizontalWrapMode.Wrap, noteText.horizontalOverflow,
                "the telemetry note does not wrap, so it cannot have a second line however much " +
                "vertical room it is given: it runs off the side of the panel instead, silently. " +
                "docs/TODO.md § 94.4.");
            Assert.AreEqual(VerticalWrapMode.Overflow, noteText.verticalOverflow,
                "the telemetry note is set to clip vertically, so the second half of the " +
                "disclosure, which is the part promising what is NOT collected, is cut off " +
                "silently at any width that does not fit it on one line.");

            _camera = new GameObject("ProbeCamera", typeof(Camera)).GetComponent<Camera>();
            yield return Drive(canvas.transform);

            // ⚠️⚠️ THE WHOLE PANEL IS DUMPED AT THE NARROWEST SHAPE AND NOTHING IS ASSERTED ON
            // IT. `docs/TODO.md` § 95 is a label named `Caption` needing 330 px in a 320 px box at
            // 1280x720, found while this probe was briefly measuring the whole settings canvas and
            // then lost when it was correctly scoped to its own surface. **A finding with no
            // repro is a finding somebody has to re-find**, so the repro is now permanent: every
            // overflowing label on this panel is written to the log with its full path and its
            // string, on every run, and none of it can fail this test.
            //
            // ⚠️ IT MUST NOT BECOME AN ASSERTION HERE. The rest of the settings panel belongs to
            // `AspectRatioProbes`, and a probe that fails naming another screen's fault sends the
            // next reader to the wrong file, which § 92.6 already paid for once.
            yield return Resize(1280, 720);
            DumpOverflowing(canvas.transform, "1280x720 settings panel");

            // ⚠️⚠️ "IS IT ON SCREEN" IS THE WRONG QUESTION FOR THIS ROW AND THE FIRST RUN ASKED
            // IT ANYWAY. The settings panel is a SCROLL LIST, and the telemetry row sits well
            // down it: at 1280x720 it is 237 px below the fold, which is not a fault, it is what
            // a scroll list is. `TheWheelScrollsTheSettingsListFromEveryPartOfIt` already owns
            // whether it can be scrolled to. **What this probe owns is that the row is inside the
            // scrolling content at all** rather than parked outside it, where no amount of
            // scrolling would ever reach it.
            Assert.IsNotNull(row.GetComponentInParent<ScrollRect>(true),
                "the telemetry row is not inside the settings panel's scroll list, so it cannot " +
                "be scrolled to and a player below the fold can never reach the opt-out");

            foreach (var (w, h, name) in Resolutions)
            {
                yield return Resize(w, h);

                // ⚠️ SCOPED TO THE ROW AND ITS NOTE, NOT TO THE WHOLE PANEL, and that is the
                // ownership rule § 92.6 already paid for once: the hub probe briefly measured the
                // whole scene and failed both its cases naming a label neither of them draws.
                // The rest of this panel belongs to `AspectRatioProbes`.
                int measured = Measure(row, name, "settings/telemetry-row", report)
                             + Measure(note, name, "settings/telemetry-note", report);

                Assert.Greater(measured, 0,
                    $"{name}: the telemetry row and its note drew no labels between them");

                report.AppendLine($"{name,-14} {w}x{h}  telemetry row ok, {measured} labels");
            }

            if (_camera != null) Object.Destroy(_camera.gameObject);
            Debug.Log($"[PhaseSurfaceLayoutProbe] telemetry row\n{report}");
        }

        // -------------------------------------------------------------------
        // § THE HARNESS
        // -------------------------------------------------------------------

        /// <summary>
        /// A profile one match short of a level, and a match that clears it.
        ///
        /// ⚠️ THE XP IS `XpPerLevel - 1` RATHER THAN A ROUND NUMBER, so any match at all levels
        /// it up whatever the award rates become. Hard-coding "900 plus a 215 match" would make
        /// this probe fail the day somebody tunes a rate, for a reason that has nothing to do
        /// with layout.
        /// </summary>
        private static (PlayerProfile, MatchRecord, PlayerMatchStats) LevelUpFixture()
        {
            var players = new PlayerMatchStats[Balance.PlayerCount];
            for (int i = 0; i < players.Length; i++)
                players[i] = new PlayerMatchStats
                {
                    Slot = i,
                    IsBot = i != 0,
                    PlayerId = i == 0 ? "probe-player" : "",
                    Handle = i == 0 ? "Probe#0001" : $"BOT {i}",
                    CharacterId = i == 0 ? "zack" : "",
                    Score = i == 0 ? 1200 : 100 * i,
                    Knockdowns = i == 0 ? 3 : 0,
                    Retrievals = i == 0 ? 4 : 0,
                    RetrievalsUnderPressure = i == 0 ? 2 : 0,
                    Tags = i == 0 ? 2 : 0,
                    Sabotages = i == 0 ? 1 : 0,
                    Throws = i == 0 ? 9 : 0,
                    TimeToFirstThrow = 3.0f,
                    ActiveRounds = Balance.Rounds,
                };

            var record = new MatchRecord
            {
                MatchId = "probe-levelup",
                Mode = GameMode.Classic.ToString(),
                MapId = "eskinita",
                Rounds = Balance.Rounds,
                DurationSeconds = Balance.Rounds * Balance.RoundTime,
                PlayedUtc = System.DateTime.UtcNow.ToString("O"),
                WinningSlot = 0,
                DefenderByRound = new[] { 0, 1, 2, 3 },
                Players = players,
            };

            MatchRecordRules.Normalise(record);

            var profile = new PlayerProfile
            {
                PlayerId = "probe-player",
                Xp = ProgressionRules.XpPerLevel * 12 - 1,
            };
            profile.Level = ProgressionRules.LevelForXp(profile.Xp);

            return (profile, record, record.Players[0]);
        }

        /// <summary>
        /// ⚠️ THE SAME OFFSCREEN-CAMERA TRICK `AspectRatioProbes` USES, AND IT IS THE ONLY ONE
        /// THAT WORKS IN BATCH MODE: `Screen.SetResolution` does nothing to a run with no display,
        /// so every overlay canvas is switched to render through a camera whose target texture IS
        /// the resolution.
        /// </summary>
        private IEnumerator Drive(Transform scope)
        {
            if (_camera == null)
            {
                _camera = new GameObject("ProbeCamera", typeof(Camera)).GetComponent<Camera>();
                if (_host != null) _camera.transform.SetParent(_host.transform, false);
            }

            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                                                               FindObjectsSortMode.None))
            {
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = _camera;
                c.planeDistance = _camera.nearClipPlane + 0.01f;
                _canvases.Add(c);
            }

            Assert.IsNotEmpty(_canvases,
                "no overlay canvas to resize: the probe would prove nothing");
            yield return null;
        }

        private IEnumerator Resize(int w, int h)
        {
            var next = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            _camera.targetTexture = next;

            if (_target != null) _target.Release();
            _target = next;

            // Three frames, for the reason `PlayerHubLayoutProbe.Resize` records: the scaler
            // recomputes in its own Update, the layout rebuild lands the frame after, and a
            // ContentSizeFitter inside a ScrollRect settles on the third.
            for (int i = 0; i < 3; i++) yield return null;
        }

        /// <summary>
        /// ⚠️ IT SEARCHES INACTIVE OBJECTS AND `GameObject.Find` DOES NOT, which cost this probe
        /// a run. `MatchResult.Awake` builds the board and immediately hides it, so the canvas it
        /// just created is invisible to `GameObject.Find` from the first frame of its life. The
        /// probe reported "MatchResult built no ResultCanvas" about a canvas that was right there.
        /// </summary>
        private static Transform Root(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                   FindObjectsSortMode.None))
                if (t.name == name) return t;
            return null;
        }

        /// <summary>
        /// Logs every label under `root` that does not fit, without asserting on any of them.
        /// See the call site for why this is evidence rather than a gate.
        /// </summary>
        private static void DumpOverflowing(Transform root, string what)
        {
            var report = new StringBuilder();
            int overflowing = 0;

            foreach (var label in root.GetComponentsInChildren<Text>(false))
            {
                if (label == null || string.IsNullOrWhiteSpace(label.text)) continue;
                if (label.horizontalOverflow != HorizontalWrapMode.Overflow) continue;

                float box = label.rectTransform.rect.width;
                if (box <= 1.0f || label.preferredWidth <= box + 1.0f) continue;

                overflowing++;
                report.AppendLine(
                    $"  needs {label.preferredWidth:0} in {box:0} at {label.fontSize}u  " +
                    $"{Where(label.transform)}  \"{label.text}\"");
            }

            Debug.Log(overflowing == 0
                ? $"[PhaseSurfaceLayoutProbe] {what}: no label overflows its box"
                : $"[PhaseSurfaceLayoutProbe] {what}: {overflowing} label(s) overflow, " +
                  $"docs/TODO.md § 95\n{report}");
        }

        /// <summary>A readable path for a label, so a failure names the row rather than a
        /// generic child called "Caption".</summary>
        private static string Where(Transform t)
        {
            string path = t.name;
            for (var p = t.parent; p != null && path.Length < 90; p = p.parent)
                path = p.name + "/" + path;
            return path;
        }

        private static Transform Find(Transform scope, string name)
        {
            foreach (var t in scope.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        /// <summary>
        /// ⚠️ `preferredWidth` IS THE MEASUREMENT AND A FONT METRIC IS NOT, for the reason
        /// `PlayerHubLayoutProbe.Measure` records at length: it is what THIS string, in THIS font,
        /// at THIS size will actually lay out to. A zero-width box is skipped rather than failed,
        /// because a label inside a layout group that has not run yet reports a rect of 0.
        /// </summary>
        private static int Measure(Transform root, string resolution, string screen,
                                   StringBuilder report)
        {
            int measured = 0;

            foreach (var label in root.GetComponentsInChildren<Text>(false))
            {
                if (label == null || string.IsNullOrWhiteSpace(label.text)) continue;

                var rect = label.rectTransform.rect;
                if (rect.width <= 1.0f) continue;

                Assert.GreaterOrEqual(label.fontSize, MenuKit.MinReadableUnits,
                    $"{resolution} {screen}: '{label.name}' is {label.fontSize} units, under the " +
                    $"{MenuKit.MinReadableUnits}-unit floor. docs/TODO.md § 92.5.");

                if (label.horizontalOverflow == HorizontalWrapMode.Overflow)
                    Assert.LessOrEqual(label.preferredWidth, rect.width + 1.0f,
                        $"{resolution} {screen}: '{Where(label.transform)}' reading " +
                        $"\"{label.text}\" needs {label.preferredWidth:0} px and was given " +
                        $"{rect.width:0}. MenuKit.Label sets Overflow, so it draws straight " +
                        "over its neighbour and nothing errors.");

                measured++;
            }

            report.AppendLine($"  {screen}: measured {measured}");
            return measured;
        }

        private static void AssertInside(RectTransform canvas, RectTransform target,
                                         string resolution, string what)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);

            var bounds = new Vector3[4];
            canvas.GetWorldCorners(bounds);

            for (int i = 0; i < 4; i++)
            {
                Assert.GreaterOrEqual(corners[i].x, bounds[0].x - 1.0f,
                    $"{resolution}: {what} runs off the left edge");
                Assert.LessOrEqual(corners[i].x, bounds[2].x + 1.0f,
                    $"{resolution}: {what} runs off the right edge");
                Assert.GreaterOrEqual(corners[i].y, bounds[0].y - 1.0f,
                    $"{resolution}: {what} runs off the bottom edge");
                Assert.LessOrEqual(corners[i].y, bounds[2].y + 1.0f,
                    $"{resolution}: {what} runs off the top edge");
            }
        }
    }
}
