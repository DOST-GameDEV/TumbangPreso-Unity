using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// THE REPEATABLE PRESENTATION RUN. One Hero Strike match, watched by the improved spectator
    /// autopilot, photographed densely, with all six ultimates forced so none can be missed, a
    /// manual replay called on a marked event, and a decision log written beside the frames.
    ///
    /// ⚠️⚠️ THE POINT IS THE PICTURES, NOT THE ASSERTIONS. `CLAUDE.md` § 6.2a: *"a green layout
    /// probe is not a good screen"*, and § 6.1: *"show, do not describe. A model change with no
    /// render attached cannot be judged."* Every failure in `docs/TODO.md` § 134.3 is a thing a
    /// person sees in a frame and no assertion can name: the subject leaves frame, the lata
    /// disappears during a retrieval, the camera faces geometry, a cut hides the outcome.
    /// **This exists so somebody can look.**
    ///
    /// ⚠️⚠️ THE SAME RUNTIME SYSTEMS THE PLAYER BUILD USES ACTUALLY EXECUTE, WHICH THE BRIEF
    /// REQUIRES. The bots press `InputIntent` exactly as a human does (`CLAUDE.md` § 4), the
    /// ultimates go through `HeroAbilitySystem.TryCast`'s real branch order, the autopilot is
    /// `SpectatorDirector` with nothing stubbed, and the replay is reached through the same `if`
    /// that reads the bound key. **Nothing here is a mock.** The only hooks are the two the brief
    /// permits: rebinding a seat's hero so all six can be captured in one run, and raising
    /// `SpectatorCamera.ProbeReplayRequest`.
    ///
    /// ⚠️ `[Category("WallClock")]`, LIKE `AiDiagnosticProbe`, AND FOR THE SAME REASON. It runs a
    /// real match at 1x for about two minutes, so its duration depends on how busy the machine
    /// is and it is excluded from the default PlayMode run. Run it on purpose:
    ///
    /// ```
    /// Unity.exe -batchmode -runTests -projectPath . -testPlatform PlayMode
    ///           -testCategory "WallClock"
    ///           -testFilter "TumbangPreso.PlayTests.NationalsShowcaseProbe"
    ///           -testResults Logs/showcase.xml -logFile Logs/showcase.log
    /// ```
    ///
    /// ⚠️⚠️ AND NO `-nographics`. `CLAUDE.md` § 7: PlayMode with `-nographics` selects
    /// `NullGfxDevice`, the first offscreen camera dies inside it, no `.xml` is written and the
    /// run still exits 0. This probe photographs things, so it needs a real device more than
    /// most.
    /// </summary>
    [Category("WallClock")]
    public class NationalsShowcaseProbe
    {
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        private const string OutDir = "Logs/shots-showcase";

        /// <summary>
        /// Seconds between frames during free play.
        ///
        /// ⚠️ 0.35 s IS A DENSE SEQUENCE RATHER THAN A VIDEO, AND THAT IS THE BRIEF'S OWN
        /// WORDING (*"a versioned video or dense frame sequence"*). At about three frames a
        /// second a cut is unmistakable, a whip-pan would be visible as a smear across two
        /// frames, and ninety seconds of match is around 260 files rather than 5,400.
        /// </summary>
        private const float FreePlayInterval = 0.35f;

        /// <summary>How long the autopilot is left to cover natural play, in seconds.</summary>
        private const float FreePlaySeconds = 40.0f;

        /// <summary>
        /// How many times the probe re-presses an ultimate before giving up on that hero.
        ///
        /// ⚠️ 12 ATTEMPTS AT ABOUT 0.17 s EACH IS ROUGHLY TWO SECONDS, which covers a shove
        /// stun (1.25 s) and most of the gap between two tags. It does NOT cover a full
        /// `Balance.TagStunTime` of 5.0 s, deliberately: a seat that has been tagged has stopped
        /// being a good subject for a hero shot anyway, and the coverage report saying so is
        /// better than a run that stalls waiting for one bot to stand up.
        /// </summary>
        private const int UltimateCastAttempts = 12;

        /// <summary>Frames captured around each forced ultimate.</summary>
        private const int UltimateFrames = 8;

        private static readonly string[] Heroes =
            { "dante", "cheska", "sean", "zack", "nemu", "phaister" };

        [UnityTest]
        public IEnumerator CaptureTheNationalsShowcase()
        {
            Directory.CreateDirectory(OutDir);

            var log = new StringBuilder();
            int shot = 0;

            // ⚠️ HERO STRIKE, FOUR SEATS, ALL BOTS, AND A SPECTATOR. `GameLaunch.AllBots` is what
            // stops seat 0 being a parked human; `docs/TODO.md` § 34 records what a parked seat
            // did to every measurement taken before it existed.
            var previousMode = UI.SceneFlow.SelectedMode;
            UI.SceneFlow.SelectedMode = GameMode.HeroStrike;

            GameLaunch.AllBots = true;
            GameLaunch.Spectator = true;

            Hitstop.End();
            Time.timeScale = 1.0f;

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");
            for (int i = 0; i < 25; i++) yield return null;

            var round = GameServices.Round;
            Assert.IsNotNull(round, "no round director");

            var runner = Object.FindFirstObjectByType<SliceRunner>();
            Assert.IsNotNull(runner, "no slice runner");
            runner.Begin();

            var camera = Object.FindFirstObjectByType<SpectatorCamera>();
            Assert.IsNotNull(camera, "the showcase needs a spectator camera");

            var director = camera.GetComponent<SpectatorDirector>();
            Assert.IsNotNull(director, "the spectator camera has no autopilot");

            director.Engaged = true;

            // ⚠️ THE SPECTATOR'S OWN CAMERA, RESOLVED ONCE. `SpectatorCamera.ReclaimView` raises
            // its own depth every frame against any rig that wakes up, so this is the camera the
            // viewer is actually looking through whatever else the scene contains.
            var shotCamera = camera.GetComponent<Camera>();
            Assert.IsNotNull(shotCamera, "the spectator rig has no Camera to photograph with");

            log.AppendLine("nationals showcase  ·  Hero Strike  ·  Eskinita  ·  autopilot engaged");
            log.AppendLine($"captured {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            log.AppendLine();

            // -------------------------------------------------------------------
            // 1. Natural play under the autopilot: retrievals, chases, sabotage,
            //    knockdowns and tags all arrive from the bots rather than being staged.
            // -------------------------------------------------------------------
            log.AppendLine("§ FREE PLAY");

            float elapsed = 0.0f;
            float next = 0.0f;
            var beatsSeen = new HashSet<SpectatorBeat>();
            var shotsSeen = new HashSet<ShotType>();

            while (elapsed < FreePlaySeconds)
            {
                elapsed += Time.unscaledDeltaTime;

                beatsSeen.Add(director.Beat);
                shotsSeen.Add(director.Shot);

                if (elapsed >= next)
                {
                    next = elapsed + FreePlayInterval;

                    yield return Frame(shotCamera, $"showcase_{shot:D4}_{director.Beat}");
                    log.AppendLine($"  {shot:D4}  {elapsed,5:F1}s  {director.Beat,-14} "
                                   + $"{director.Shot,-18} {director.ShotName(),-24} "
                                   + $"{director.Diagnostic}");
                    shot++;
                }

                yield return null;
            }

            // -------------------------------------------------------------------
            // 2. All six ultimates, so a capture cannot silently miss one.
            //
            // ⚠️⚠️ THE SEAT IS REBOUND AND THE VERB IS PRESSED. `HeroAbilitySystem.BindHero` is
            // the same call `MatchInstaller` makes when a match starts, and the press goes
            // through `InputIntent`, which is the same table a keyboard writes into. The cast
            // then runs `TryCast`'s real branch order, spends the real meter, raises the real
            // `UltimateStarted` event, and the introduction card and the autopilot both react to
            // it exactly as they would in a played match.
            // -------------------------------------------------------------------
            log.AppendLine();
            log.AppendLine("§ THE SIX ULTIMATES");

            var caster = FirstAttacker(round);

            if (caster == null)
            {
                log.AppendLine("  no attacker seat available; ultimates not captured");
            }
            else
            {
                var abilities = caster.GetComponent<HeroAbilitySystem>();

                foreach (string hero in Heroes)
                {
                    if (abilities == null) break;

                    abilities.BindHero(hero);
                    yield return null;

                    // ⚠️⚠️ THE BOT HAS TO LET GO OF THE SEAT FIRST, AND THE FIRST RUN OF THIS
                    // PROBE PROVED IT. It wrote `caster.Intent.Set(Verb.Ultimate, true)` straight
                    // onto a seat that `AIController.Update` rewrites every frame, so the press
                    // was cleared before `CharacterMotor.FixedUpdate` ever took its snapshot:
                    // **not one of the six ultimates cast**, `SpectatorBeat.Ultimate` came back
                    // `NOT SEEN` in the coverage report, and the eight frames named `ult_dante`
                    // were eight frames of ordinary play.
                    //
                    // ⚠️ DISABLING THE BOT IS THE HONEST HOOK RATHER THAN CALLING THE KIT
                    // DIRECTLY. `CLAUDE.md` § 4: a bot presses the same buttons a human does, and
                    // there is no second path where either can do something the other cannot.
                    // Taking the seat over and pressing the verb IS the human path; reaching into
                    // `HeroKit.CastUltimate` would be the second path that invariant forbids.
                    var ai = caster.GetComponent<AIController>();
                    if (ai != null) ai.enabled = false;

                    // Top the meter up the way the practice bench does, then press.
                    PracticeSandbox.Wanted = true;
                    for (int i = 0; i < 4; i++) yield return null;

                    var kit = abilities.Kit;
                    string name = kit != null && kit.Ultimate != null ? kit.Ultimate.Name : "?";

                    // ⚠️⚠️ IT PRESSES UNTIL THE CAST IS ACCEPTED, AND THE SECOND CAPTURE RUN IS
                    // WHY. Dante's card appeared and Cheska's did not: a single press lands on
                    // whatever the seat happens to be doing, and in a live Hero Strike round that
                    // seat is regularly **stunned**. `HeroAbilitySystem.Cast` answers `CannotAct`
                    // then, `ServiceBuffer` retries only inside `InputBufferWindow` (0.30 s), and
                    // `Balance.TagStunTime` is **5.0 s**. A press that has to be lucky is not a
                    // capture hook, it is a coin flip that makes the report say `NOT SEEN` about
                    // working code.
                    //
                    // ⚠️ HELD FOR A FEW FRAMES PER ATTEMPT, NOT ONE. `Aim` reads a press EDGE and
                    // `CharacterMotor.FixedUpdate` snapshots on the physics step, so a press that
                    // lives for a single render frame can fall between two steps and never be
                    // seen. `InputIntent`'s own note records the same trap for mash-to-get-up.
                    //
                    // ⚠️ AND IT GIVES UP RATHER THAN HANGING. A seat that cannot act for the whole
                    // window leaves this hero uncaptured and the coverage report says so, which is
                    // the honest outcome; blocking would turn one stunned bot into a dead run.
                    bool cast = false;

                    for (int attempt = 0; attempt < UltimateCastAttempts && !cast; attempt++)
                    {
                        caster.Intent.Set(Verb.Ultimate, true);
                        for (int i = 0; i < 4; i++) yield return null;
                        caster.Intent.Set(Verb.Ultimate, false);

                        for (int i = 0; i < 6; i++)
                        {
                            yield return null;

                            if (abilities.LastAnswer(HeroAbilitySystem.Slot.Ultimate)
                                == HeroKit.CastOutcome.Cast)
                            {
                                cast = true;
                                break;
                            }
                        }
                    }

                    log.AppendLine($"        {hero,-9} cast={cast}");

                    for (int f = 0; f < UltimateFrames; f++)
                    {
                        // ⚠️ SAMPLED INSIDE THE LOOP. The first version recorded the beat once,
                        // AFTER all eight frames and the settle, by which point a 0.4 s wind-up
                        // was long over: the coverage report would have said `NOT SEEN` even on a
                        // run where the camera had covered it perfectly.
                        beatsSeen.Add(director.Beat);
                        shotsSeen.Add(director.Shot);

                        yield return Frame(shotCamera, $"showcase_{shot:D4}_ult_{hero}");
                        log.AppendLine($"  {shot:D4}  {hero,-9} {name,-18} "
                                       + $"{director.Beat,-12} {director.Shot}");
                        shot++;

                        for (int w = 0; w < 5; w++) yield return null;
                    }

                    if (ai != null) ai.enabled = true;

                    PracticeSandbox.Clear();

                    beatsSeen.Add(director.Beat);
                    shotsSeen.Add(director.Shot);

                    // Let the arena settle so one ultimate is not photographed over the last.
                    for (int w = 0; w < 60; w++) yield return null;
                }
            }

            // -------------------------------------------------------------------
            // 3. The manual replay, centred on whatever the buffer last marked.
            // -------------------------------------------------------------------
            log.AppendLine();
            log.AppendLine("§ MANUAL REPLAY");

            SpectatorCamera.ProbeReplayRequest = true;
            for (int w = 0; w < 3; w++) yield return null;

            for (int f = 0; f < 14; f++)
            {
                yield return Frame(shotCamera, $"showcase_{shot:D4}_replay");
                log.AppendLine($"  {shot:D4}  replay frame {f}");
                shot++;

                for (int w = 0; w < 4; w++) yield return null;
            }

            // -------------------------------------------------------------------
            // 4. What the run actually covered, stated rather than assumed.
            //
            // ⚠️⚠️ IT REPORTS THE BEATS AND SHOTS THAT DID *NOT* APPEAR. `docs/TODO.md` § 96,
            // § 114 and § 124.11 are all one shape: a probe that was green about something it had
            // stopped exercising. A capture run that quietly never produced a chase shot is that
            // fault with pictures attached, and the only defence is for the run to say so.
            // -------------------------------------------------------------------
            log.AppendLine();
            log.AppendLine("§ COVERAGE");

            foreach (SpectatorBeat beat in System.Enum.GetValues(typeof(SpectatorBeat)))
                log.AppendLine($"  beat {beat,-16} {(beatsSeen.Contains(beat) ? "seen" : "NOT SEEN")}");

            foreach (ShotType s in System.Enum.GetValues(typeof(ShotType)))
                log.AppendLine($"  shot {s,-20} {(shotsSeen.Contains(s) ? "seen" : "NOT SEEN")}");

            log.AppendLine();
            log.AppendLine($"  occluded poses re-solved : {director.OccludedPoseRejections}");
            log.AppendLine($"  safe-pose fallbacks      : {director.SafePoseFallbacks}");
            log.AppendLine($"  cuts                     : {director.Cuts}");
            log.AppendLine($"  frames written           : {shot}");

            File.WriteAllText($"{OutDir}/showcase-log.txt", log.ToString());
            Debug.Log(log.ToString());

            UI.SceneFlow.SelectedMode = previousMode;
            GameLaunch.Spectator = false;
            GameLaunch.AllBots = false;

            // ⚠️ THE ASSERTIONS ARE A FLOOR ON THE CAPTURE, NOT ON THE GAME. They exist so a run
            // that produced nothing fails loudly instead of writing an empty folder somebody then
            // reviews and signs off.
            // ⚠️⚠️ COUNTED OFF DISK, NOT OFF THE LOOP VARIABLE. The first version asserted on
            // `shot`, which increments whether or not a file appeared, and would have passed over
            // an empty folder when `ScreenCapture` turned out to write nothing in batch mode.
            // **A capture probe has to assert that it captured something.**
            int onDisk = Directory.GetFiles(OutDir, "showcase_*.png").Length;
            log.AppendLine($"  frames on disk           : {onDisk}");
            File.WriteAllText($"{OutDir}/showcase-log.txt", log.ToString());

            Assert.Greater(onDisk, 100,
                $"the showcase wrote {onDisk} frames to {OutDir}, which is too few to review.");

            Assert.IsTrue(beatsSeen.Contains(SpectatorBeat.Ultimate),
                "the autopilot never recognised an ultimate, which is the one beat this run "
                + "forces and therefore cannot legitimately miss.");

            Assert.LessOrEqual(director.SafePoseFallbacks, shot / 4,
                $"the pose validator fell back to the safe overhead {director.SafePoseFallbacks} "
                + "times, which means most bearings were refused and the shot vocabulary is not "
                + "reaching the arena. See `docs/TODO.md` § 134.5.");
        }

        private static CharacterMotor FirstAttacker(RoundDirector round)
        {
            foreach (var p in round.Players)
                if (p != null && !p.IsDefender && p.RoundActive) return p;

            return null;
        }

        /// <summary>
        /// Writes one PNG of what the spectator camera is looking at, with the HUD on it.
        ///
        /// ⚠️⚠️ THE FIRST VERSION OF THIS HUNG THE WHOLE RUN AND WROTE NOTHING, AND BOTH HALVES
        /// OF THAT ARE WORTH RECORDING BECAUSE THE CALL LOOKED OBVIOUS. It was three lines:
        /// `yield return new WaitForEndOfFrame();` then `ScreenCapture.CaptureScreenshot(path)`.
        ///
        ///   1. **`WaitForEndOfFrame` NEVER RESUMES IN `-batchmode`.** There is no rendering loop
        ///      to reach the end of, so the coroutine parks forever. The run sat with a static
        ///      log for minutes and had to be killed; nothing in the output said why.
        ///   2. **`ScreenCapture.CaptureScreenshot` WRITES NOTHING IN `-batchmode` EITHER.** There
        ///      is no swap chain to capture. It fails silently, so even without the hang the run
        ///      would have gone green over an empty folder.
        ///
        /// ⚠️⚠️ SO IT GOES THROUGH `GameplayShots.Render`, WHICH IS THE PATH THAT WORKS, AND
        /// EVERY PARAGRAPH IN THAT METHOD IS A FAULT SOMEBODY ALREADY PAID FOR: the HDR target
        /// resolved through an sRGB one (without it every shot is a stop and a half too dark),
        /// the UI drawn by a SECOND ungraded camera (without it the scoreboard photographs as
        /// pure black), the render target created before the layout pass (without it every font
        /// is rasterised small and then enlarged), and the layer restore. **Writing a second
        /// capture path would have been writing all four of those bugs again.**
        ///
        /// ⚠️ THE CAMERA IS THE SPECTATOR'S OWN, NOT `Camera.main`. The whole subject of this run
        /// is what the autopilot chose to look at.
        /// </summary>
        private static IEnumerator Frame(Camera cam, string name)
        {
            if (cam == null) yield break;

            yield return GameplayShots.Render(cam, name, flipCanvases: true, outDir: OutDir);
        }
    }
}
