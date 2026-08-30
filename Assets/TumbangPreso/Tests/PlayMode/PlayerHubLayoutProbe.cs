using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Every tab of the player hub and the sign-in screen, measured at nine resolutions.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THE SCREENS IT REPLACED WERE FOUND BROKEN BY 🧑 PLAYING, NOT BY A
    /// TEST. He photographed the career page with its buttons running off the bottom edge and a
    /// stray CLASSIC label drawn straight through the HERO STRIKE tab, and the account page with
    /// six fields and six equal buttons on one block. `docs/TODO.md` § 92 has all five faults.
    /// **Three of the five were layout faults that a measurement would have caught the day they
    /// were written**, and the reason none did is that there was no probe for these screens: the
    /// UI probes cover the HUD (`HudOverflowProbe`), the character screen and the hero picker.
    ///
    /// ⚠️⚠️ THE OVERFLOW CHECK IS THE POINT, AND IT IS THE SAME ONE `HudOverflowProbe` MAKES FOR
    /// THE SAME REASON. `MenuKit.Label` sets `horizontalOverflow = Overflow`, so a string that
    /// does not fit does not wrap and does not shrink: it draws straight over whatever is beside
    /// it. That is precisely the CLASSIC-through-HERO-STRIKE artefact in the screenshot, and it is
    /// silent, because nothing errors and the label is still "there".
    ///
    /// ⚠️ IT DRIVES THE REAL SCREENS RATHER THAN A FIXTURE. `PlayerNameplate.Install` builds the
    /// hub and the hub builds the sign-in screen, so this exercises the one path the menu uses.
    /// A probe against a hand-built copy proves the copy.
    ///
    /// ⚠️ `GameServices.Account` AND `.Career` ARE NULL HERE AND THAT IS A CASE WORTH COVERING
    /// RATHER THAN A LIMITATION. A player who boots with no connection sees exactly this: an
    /// account that has not resolved and a career with nothing in it. Every empty state on these
    /// screens is therefore measured, which is the state the old career page got most wrong.
    /// </summary>
    public class PlayerHubLayoutProbe
    {
        /// <summary>
        /// ⚠️ ONE FILE PER CASE. The first version wrote all three to one path and the last
        /// test to finish silently overwrote the other two, so the evidence on disk described one
        /// of the three runs and nothing said which. A report that can be overwritten by a
        /// passing sibling is a report nobody can quote.
        /// </summary>
        private const string OutDir = "Logs";

        /// <summary>⚠️ PNGs GO IN THEIR OWN FOLDER, not beside the text reports. They are for a
        /// person to look at and `CLAUDE.md` § 6.1 wants every iteration versioned by NAME, so a
        /// folder that can be cleared and re-shot in one go is what that rule needs.</summary>
        private const string ShotDir = "Logs/ui";

        /// <summary>⚠️ THE SAME NINE `AspectRatioProbes` AND `HudOverflowProbe` USE. If this list
        /// ever disagrees with theirs, one of the three files is testing a screen the game does
        /// not ship.</summary>
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

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var c in _canvases)
                if (c != null) c.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvases.Clear();

            if (_camera != null) _camera.targetTexture = null;
            if (_target != null) _target.Release();
            if (_host != null) Object.Destroy(_host);

            yield return null;
        }

        /// <summary>
        /// Builds the real hub, walks all four tabs at all nine resolutions, and fails on the
        /// first label that does not fit the box it was given.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryTabFitsItsBoxAtEveryShippedResolution()
        {
            var report = new StringBuilder();
            yield return Boot(report);

            var hub = _host.GetComponent<PlayerHub>();
            Assert.IsNotNull(hub, "the nameplate did not install a hub");
            hub.Open();
            yield return null;

            foreach (var (w, h, name) in Resolutions)
            {
                yield return Resize(w, h);

                // ⚠️ EVERY TAB, NOT THE ONE THAT HAPPENS TO BE OPEN. The tabs are rebuilt on
                // switch rather than kept alive, so a fault on ACCOUNT is invisible while PROFILE
                // is showing, which is how a screen ships broken on one tab.
                foreach (var tab in new[] { "PROFILE", "CAREER", "MATCHES", "ACCOUNT" })
                {
                    Press(tab);
                    yield return null;
                    yield return null;

                    int checked_ = Measure(Root("PlayerHubCanvas"), name, $"hub/{tab}", report);
                    Assert.Greater(checked_, 0,
                        $"{name}: the {tab} tab drew no labels at all, so this proves nothing.");

                    // ⚠️⚠️ COUNTING LABELS WAS NOT ENOUGH AND AN EMPTY LIST PASSED BECAUSE OF
                    // IT. The header, the four tab buttons and the footer are all labels, so a
                    // tab whose entire content failed to render still cleared "some labels were
                    // measured". That is exactly what happened: `UiRows.ScrollList` built its
                    // mask on a fully transparent graphic, which masks everything out, and the
                    // first screenshot showed chrome over an empty field. **Assert on the thing
                    // the screen is for.**
                    Assert.Greater(Rows(), 0,
                        $"{name}: the {tab} tab has no rows or sections in its list. The chrome " +
                        "can draw perfectly with nothing in it.");
                }
            }

            Write("tabs", report);
        }

        /// <summary>
        /// The sign-in screen, which is the one 🧑 named the reference for.
        ///
        /// ⚠️ IT IS A SEPARATE CASE BECAUSE IT IS A SEPARATE SCREEN. Folding it into the tab loop
        /// would hide which of the two failed behind one assertion message.
        /// </summary>
        [UnityTest]
        public IEnumerator TheSignInScreenFitsItsBoxAtEveryShippedResolution()
        {
            var report = new StringBuilder();
            yield return Boot(report);

            var signIn = _host.GetComponent<SignInScreen>();
            Assert.IsNotNull(signIn, "the hub did not install a sign-in screen");
            signIn.Open();
            yield return null;

            foreach (var (w, h, name) in Resolutions)
            {
                yield return Resize(w, h);
                yield return null;

                int checked_ = Measure(Root("SignInCanvas"), name, "signin", report);
                Assert.Greater(checked_, 0, $"{name}: the sign-in screen drew no labels.");
            }

            Write("signin", report);
        }

        /// <summary>
        /// ⚠️⚠️ THE NAMEPLATE REPLACED TWO BUTTONS THAT WERE IN THE WRONG PLACE, so "is it on the
        /// screen" is the assertion this whole redesign turns on. 🧑: *"look wtf why are these
        /// buttons here"*. A nameplate that runs off the left edge at 4:3 is the same bug with
        /// better art.
        /// </summary>
        [UnityTest]
        public IEnumerator TheNameplateStaysOnScreenAtEveryShippedResolution()
        {
            var report = new StringBuilder();
            yield return Boot(report);

            foreach (var (w, h, name) in Resolutions)
            {
                yield return Resize(w, h);
                yield return null;

                var plate = Find("Nameplate");
                Assert.IsNotNull(plate, "the nameplate was never built");

                var canvas = plate.GetComponentInParent<Canvas>();
                AssertInside((RectTransform)canvas.transform, (RectTransform)plate.transform,
                             name, "the nameplate");

                report.AppendLine($"{name,-14} {w}x{h}  nameplate ok");
            }

            Write("nameplate", report);
        }

        /// <summary>
        /// Photographs every screen at 1920x1080 and writes them to `Logs/ui/`.
        ///
        /// ⚠️⚠️ `CLAUDE.md` § 6.1: SHOW, DO NOT DESCRIBE. A UI change with no render attached
        /// cannot be judged, and describing a layout in prose is the slowest possible way to be
        /// told it is wrong. 🧑 asked for exactly this: *"send pics of updated ui!"*. The rule was
        /// written for models and it is the same rule here.
        ///
        /// ⚠️⚠️ IT SEEDS A REAL CAREER FIRST, because the interesting screen is the one with
        /// numbers on it. `GameServices.Ensure` has already run by the time a PlayMode test
        /// starts (it is a `BeforeSceneLoad` hook), so the career exists and is empty;
        /// `ProfileRules.Apply` fills it with the same call the game uses, which also means the
        /// picture shows real formatting of real derived rates rather than placeholder text.
        ///
        /// ⚠️ IT IS NOT AN ASSERTION AND IT MUST NOT BECOME ONE. It exists to produce evidence a
        /// person looks at. The assertions live in the three cases above; a test that fails
        /// because a picture changed is a test nobody can keep green.
        /// </summary>
        [UnityTest]
        public IEnumerator PhotographEveryScreen()
        {
            var report = new StringBuilder();
            yield return Boot(report);

            // ⚠️⚠️ EVERY OTHER CANVAS IS SWITCHED OFF BEFORE A SINGLE SHOT IS TAKEN. PlayMode
            // runs these cases after other suites have loaded scenes, so the first render of the
            // career tab had the MULTIPLAYER setup screen drawn through it, join code field and
            // all. **A screenshot with another screen in it cannot be judged**, which is the
            // whole point of taking one, and the fix belongs here rather than in the game: the
            // game is never in two screens at once and the probe was.
            foreach (var c in _canvases)
                if (c != null && !c.name.StartsWith("PlayerHub") && !c.name.StartsWith("SignIn")
                    && !c.name.StartsWith("Nameplate"))
                    c.enabled = false;

            SeedCareer(report);

            var hub = _host.GetComponent<PlayerHub>();
            var signIn = _host.GetComponent<SignInScreen>();

            yield return Resize(1920, 1080);

            // The nameplate on its own, which is what the title screen grew.
            yield return Shoot("01-nameplate");

            hub.Open();
            yield return null;
            yield return Shoot("02-hub-profile");

            Press("CAREER");
            yield return null;
            yield return null;
            yield return Shoot("03-hub-career-collapsed");

            // ⚠️ ONE GROUP OPENED, WHICH IS THE STATE THE COLLAPSING EXISTS FOR. A picture of
            // everything shut says nothing about what happens when you press one.
            Press("+  ATTACK");
            yield return null;
            yield return null;
            yield return Shoot("04-hub-career-open");

            Press("MATCHES");
            yield return null;
            yield return null;
            yield return Shoot("05-hub-matches");

            Press("ACCOUNT");
            yield return null;
            yield return null;
            yield return Shoot("06-hub-account");

            signIn.Open();
            yield return null;
            yield return null;
            yield return Shoot("07-signin");

            Write("shots", report);
        }

        /// <summary>
        /// Fills the career with four finished matches so the screens have something to draw.
        ///
        /// ⚠️ THE SAME `ProfileRules.Apply` THE GAME USES, so every rate on the picture is derived
        /// the way it will be in play. Writing numbers straight into `CareerTotals` would produce
        /// a screenshot of a state the game cannot reach.
        /// </summary>
        private static void SeedCareer(StringBuilder report)
        {
            var career = GameServices.Career;
            if (career?.Profile == null) { report.AppendLine("no career to seed"); return; }

            string me = GameServices.Account?.ConnectionToken ?? TumbangPreso.Net.NetIdentity.Token;
            string[] heroes = { "zack", "sean", "zack", "cheska" };

            for (int m = 0; m < 12; m++)
            {
                var players = new PlayerMatchStats[Balance.PlayerCount];
                for (int i = 0; i < players.Length; i++)
                    players[i] = new PlayerMatchStats
                    {
                        Slot = i,
                        PlayerId = i == 0 ? me : $"bot-{i}",
                        Handle = i == 0 ? "You#0000" : $"Player {i + 1}#111{i}",
                        IsBot = i != 0,
                        CharacterId = i == 0 ? heroes[m % heroes.Length] : "totoy",
                        SlipperId = "tsinelas",
                        Score = 400 - ((i + m) % 4) * 90,
                        ScoreAtFinalRound = 300 - ((i + m) % 4) * 70,
                        Throws = 24 + i * 3,
                        Knockdowns = 6 + i,
                        Retrievals = 14 + i,
                        RetrievalsUnderPressure = 5 + i,
                        Tags = 4 + i,
                        Sabotages = 2,
                        RoundsDefended = 1,
                        DefenceTicks = 46,
                        ShoveAttempts = 9,
                        ShoveHits = 5,
                        LungeAttempts = 7,
                        LungeHits = 3,
                        DistanceTravelled = 520.0f,
                        TimeToFirstThrow = 6.4f,
                        LongestLastAttacker = 11.5f,
                        ActiveRounds = Balance.Rounds,
                    };

                var record = new MatchRecord
                {
                    MatchId = $"shot-{m}",
                    Mode = GameMode.Classic.ToString(),
                    MapId = m % 2 == 0 ? "eskinita" : "ilalim_ng_tulay",
                    Rounds = Balance.Rounds,
                    DurationSeconds = 372.0f,
                    PlayedUtc = System.DateTime.UtcNow.AddHours(-m).ToString("O"),
                    WinningSlot = m % 4,
                    Online = true,
                    DefenderByRound = new[] { 0, 1, 2, 3 },
                    Players = players,
                };

                MatchRecordRules.Normalise(record);

                // ⚠️ THE CAREER IS SEEDED AND THE HISTORY IS NOT. `CareerStore.History` is an
                // `IReadOnlyList` by design, and the only public way to add to it is `Record`,
                // which also queues, saves and flushes to the endpoint. A probe must not post
                // twelve invented matches to a live career, so the MATCHES tab is photographed in
                // its empty state and that is the honest picture of it.
                ProfileRules.Apply(career.Profile, record, me);
            }

            report.AppendLine($"seeded 12 matches, xp {career.Profile.Xp}");
        }

        /// <summary>
        /// ⚠️⚠️ NO `WaitForEndOfFrame`, AND THIS HUNG A WHOLE RUN BEFORE THE LINE WAS REMOVED.
        /// In `-batchmode` there is no display and `WaitForEndOfFrame` never resumes, so the
        /// coroutine simply stops: Unity sat there with the log frozen, wrote no `.xml`, and had
        /// to be killed. It is the ordinary way to wait for a frame to finish drawing and it is
        /// wrong here, because **nothing is drawing a frame**: `Camera.Render` is an explicit
        /// draw into a target texture and needs no frame boundary at all.
        ///
        /// ⚠️ AND IT IS THE SAME SHAPE AS `CLAUDE.md` § 7's `-nographics` warning: an editor API
        /// that assumes a screen, in a run that has none, failing by doing nothing rather than by
        /// reporting anything.
        /// </summary>
        private IEnumerator Shoot(string name)
        {
            yield return null;

            _camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = _target;

            var shot = new Texture2D(_target.width, _target.height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0);
            shot.Apply();

            RenderTexture.active = previous;

            Directory.CreateDirectory(ShotDir);
            File.WriteAllBytes(Path.Combine(ShotDir, name + ".png"), shot.EncodeToPNG());
            Object.Destroy(shot);

            Debug.Log($"[PlayerHub] shot {name}");
        }

        // -------------------------------------------------------------------
        // § THE HARNESS
        // -------------------------------------------------------------------

        private IEnumerator Boot(StringBuilder report)
        {
            _host = new GameObject("HubProbeHost");

            var nameplate = _host.AddComponent<PlayerNameplate>();
            nameplate.Install();
            yield return null;

            _camera = new GameObject("ProbeCamera", typeof(Camera)).GetComponent<Camera>();
            _camera.transform.SetParent(_host.transform, false);

            // ⚠️ THE SAME TRICK `AspectRatioProbes` USES, AND IT IS THE ONLY ONE THAT WORKS IN
            // BATCH MODE: `Screen.SetResolution` does nothing to an offscreen run, so the canvas
            // is switched to render through a camera whose target texture is the resolution.
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                                                               FindObjectsSortMode.None))
            {
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = _camera;
                c.planeDistance = _camera.nearClipPlane + 0.01f;
                _canvases.Add(c);
            }

            Assert.IsNotEmpty(_canvases, "no overlay canvas to resize: the probe would prove nothing");
            report.AppendLine($"canvases driven: {_canvases.Count}");
        }

        private IEnumerator Resize(int w, int h)
        {
            var next = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            _camera.targetTexture = next;

            if (_target != null) _target.Release();
            _target = next;

            // Three frames: the scaler recomputes in its own Update, the layout rebuild lands the
            // frame after, and a ContentSizeFitter inside a ScrollRect settles on the third.
            for (int i = 0; i < 3; i++) yield return null;
        }

        private void Press(string label)
        {
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include,
                                                                    FindObjectsSortMode.None))
            {
                var text = button.GetComponentInChildren<Text>(true);
                if (text != null && text.text == label) { button.onClick.Invoke(); return; }
            }

            Assert.Fail($"no button reading '{label}' on the hub");
        }

        /// <summary>
        /// ⚠️⚠️ `preferredWidth` IS THE MEASUREMENT AND A FONT METRIC IS NOT. It is what THIS
        /// string, in THIS font, at THIS size, with THESE generator settings will actually lay out
        /// to, which is the same thing `MenuKit.Fit`, `Hud.WorstCaseNameWidth` and
        /// `HudOverflowProbe` all ask. Anything else is an estimate of the thing that broke.
        ///
        /// ⚠️ A ZERO-WIDTH BOX IS SKIPPED RATHER THAN FAILED. A label inside a layout group that
        /// has not run yet reports a rect of 0 and would fail every assertion for a reason that
        /// has nothing to do with the string.
        /// </summary>
        private static int Measure(Transform root, string resolution, string screen,
                                   StringBuilder report)
        {
            int measured = 0;
            int worst = 0;
            string worstName = "";

            // ⚠️⚠️ SCOPED TO THIS SCREEN'S CANVAS, AND THE FIRST FULL RUN IS WHY. It searched
            // the whole scene, so it measured `SettingsSummary` on the settings panel, found it
            // authored at 16 units, and failed BOTH of this file's cases with the name of a label
            // neither of them draws. **A probe that reports another screen's fault under this
            // screen's name sends the next reader to the wrong file.** The 16-unit label is a
            // real finding and belongs to whoever owns that panel; `docs/TODO.md` § 92.6 records
            // it rather than this probe silently owning it.
            Assert.IsNotNull(root, $"{resolution} {screen}: the canvas was never built");

            foreach (var label in root.GetComponentsInChildren<Text>(false))
            {
                if (label == null || string.IsNullOrWhiteSpace(label.text)) continue;
                if (label.color.a < 0.05f) continue;
                if (!label.isActiveAndEnabled) continue;

                float room = label.rectTransform.rect.width;
                if (room <= 1.0f) continue;

                measured++;

                Assert.GreaterOrEqual(label.fontSize, MenuKit.MinReadableUnits,
                    $"{resolution} {screen}: '{label.name}' is authored at {label.fontSize} " +
                    $"units, below the {MenuKit.MinReadableUnits}-unit floor.");

                if (label.horizontalOverflow == HorizontalWrapMode.Wrap) continue;

                float needed = label.preferredWidth;
                int over = Mathf.RoundToInt(needed - room);

                if (over > worst) { worst = over; worstName = label.name; }

                Assert.LessOrEqual(needed, room + 1.0f,
                    $"{resolution} {screen}: '{label.name}' needs {needed:F0} units for " +
                    $"\"{label.text}\" and was given {room:F0}. It does not wrap and does not " +
                    "shrink, so it draws over whatever is beside it.");
            }

            report.AppendLine($"{resolution,-14} {screen,-14} {measured,3} labels" +
                              (worst > 0 ? $"   worst spare {-worst} ({worstName})" : ""));
            return measured;
        }

        private static void AssertInside(RectTransform canvas, RectTransform what,
                                         string resolution, string described)
        {
            var canvasRect = canvas.rect;
            var corners = new Vector3[4];
            what.GetWorldCorners(corners);

            for (int i = 0; i < 4; i++)
            {
                Vector3 local = canvas.InverseTransformPoint(corners[i]);

                Assert.GreaterOrEqual(local.x, canvasRect.xMin - 0.5f,
                    $"{resolution}: {described} runs {canvasRect.xMin - local.x:F0} units off the LEFT.");
                Assert.LessOrEqual(local.x, canvasRect.xMax + 0.5f,
                    $"{resolution}: {described} runs {local.x - canvasRect.xMax:F0} units off the RIGHT.");
                Assert.GreaterOrEqual(local.y, canvasRect.yMin - 0.5f,
                    $"{resolution}: {described} runs {canvasRect.yMin - local.y:F0} units off the BOTTOM.");
                Assert.LessOrEqual(local.y, canvasRect.yMax + 0.5f,
                    $"{resolution}: {described} runs {local.y - canvasRect.yMax:F0} units off the TOP.");
            }
        }

        /// <summary>How many rows and section headings the open tab actually built.</summary>
        private static int Rows()
        {
            int found = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude,
                                                                  FindObjectsSortMode.None))
                if (t.name.StartsWith("Row_") || t.name.StartsWith("Section_")) found++;
            return found;
        }

        /// <summary>The canvas a case is about, by name.</summary>
        private static Transform Root(string canvas)
        {
            var go = Find(canvas);
            return go != null ? go.transform : null;
        }

        private static GameObject Find(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                  FindObjectsSortMode.None))
                if (t.name == name) return t.gameObject;
            return null;
        }

        private static void Write(string name, StringBuilder report)
        {
            Directory.CreateDirectory(OutDir);
            File.WriteAllText(Path.Combine(OutDir, $"player-hub-{name}.txt"), report.ToString());
            Debug.Log($"[PlayerHub] {name}\n" + report);
        }
    }
}
