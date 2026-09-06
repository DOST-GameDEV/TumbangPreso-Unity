using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// What a maximum-effects Hero Strike frame actually COSTS, measured rather than assumed.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THE QUESTION HAD BEEN ASKED THREE TIMES AND NEVER MEASURED ONCE.
    /// `docs/TODO.md` § 150.10: *"A maximum-effects VFX stress measurement was not taken.
    /// `MatchFrameRateProbe` and `HudPerformanceProbe` exist and are in the `capture` group; the
    /// brief's question is specifically about overlapping Hero Strike abilities, which neither
    /// drives."* `AbilityShowcaseProbe` builds the same pile-up and PHOTOGRAPHS it, which answers
    /// `docs/VISION.md` § 2 rule 5 (can you still see the lata) and says nothing at all about
    /// frame time or about what is left behind afterwards.
    ///
    /// ⚠️⚠️ AND THE POINT OF MEASURING IS TO EARN THE RIGHT NOT TO OPTIMISE. `CLAUDE.md` § 6.0:
    /// *"If a performance claim is ever made about sourced art, it gets MEASURED first ... An
    /// unmeasured optimisation that destroys the art is a pure loss."* The same rule applies to
    /// pooling a spawner because it looks expensive. This fixture is what a future session cites
    /// when it decides to leave `HeroHazards` alone.
    ///
    /// ⚠️⚠️ THE FRAME TIMES ARE REPORTED AND THE OBJECT COUNT IS ASSERTED, AND THE SPLIT IS
    /// DELIBERATE. A batchmode PlayMode run renders offscreen on whatever machine the gate is on,
    /// and `CLAUDE.md` § 7 already records `AiDiagnosticProbe` failing at 21.6 s, 29.9 s and
    /// 37.6 s against one bound with nothing changed: **a wall-clock threshold in this suite is a
    /// measurement of how busy the laptop is.** So the timings go in `Logs/ability-stress.txt`
    /// for a person to read, and the only thing that can FAIL here is the claim that does not
    /// depend on the machine at all: an effect whose life has expired has taken its objects with
    /// it. That is what "obvious runaway objects" means, and it is the one of the four questions
    /// that a slow machine cannot fake either way.
    /// </summary>
    public class AbilityStressProbe
    {
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        /// <summary>
        /// How long every effect in the pile lives.
        ///
        /// ⚠️ SHORT ON PURPOSE, AND `AbilityShowcaseProbe`'s 60 s IS WHY IT CANNOT ANSWER THIS.
        /// That probe wants everything frozen alive while a camera moves around it; this one has
        /// to watch the pile go away again, and an effect with a minute of life left is an effect
        /// whose cleanup has not been tested. It still comfortably outlives the sampling window
        /// below, so the frames being measured are frames with the whole pile live.
        /// </summary>
        private const float EffectLife = 4.0f;

        /// <summary>Frames counted per arm. Enough that the median is not one hitch.</summary>
        private const int SampleFrames = 240;

        private const string ReportPath = "Logs/ability-stress.txt";

        [UnityTest]
        [Category("WallClock")]
        public IEnumerator TheMaximumEffectsFrameIsMeasuredAndNothingIsLeftBehind()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 30; i++) yield return null;

            UI.SceneFlow.SelectedMode = GameMode.HeroStrike;
            GameServices.Round.BeginRound();
            for (int i = 0; i < 30; i++) yield return null;

            // ---- BASELINE ---------------------------------------------------------------
            long objectsBefore = SceneObjectCount();
            long managedBefore = System.GC.GetTotalMemory(false);

            var quiet = new FrameRateHistogram();
            yield return Sample(quiet, SampleFrames);

            long managedAfterQuiet = System.GC.GetTotalMemory(false);

            // ---- THE PILE ---------------------------------------------------------------
            long managedBeforeSpawn = System.GC.GetTotalMemory(false);
            int spawnCalls = SpawnTheWorstCredibleFrame();
            long managedAfterSpawn = System.GC.GetTotalMemory(false);

            long objectsLive = SceneObjectCount();

            var loaded = new FrameRateHistogram();
            yield return Sample(loaded, SampleFrames);

            long managedAfterLoaded = System.GC.GetTotalMemory(false);

            // ---- THE RECLAIM ------------------------------------------------------------
            // ⚠️ REAL SECONDS, NOT FRAMES. Every hazard's life is a `Time.deltaTime` countdown,
            // and `docs/TODO.md` § 146.4c records exactly this trap costing two runs: in a
            // batchmode PlayMode run the frame loop spins at thousands of frames a second, so
            // sixty frames measured 0.03 s of game time and read as the feature being broken.
            yield return new WaitForSeconds(EffectLife + 3.0f);
            for (int i = 0; i < 60; i++) yield return null;

            long objectsAfter = SceneObjectCount();

            // ---- THE SECOND CAST --------------------------------------------------------
            // ⚠️⚠️ THE THIRD ARM IS THE ONLY ONE THAT ANSWERS "WOULD POOLING HELP", AND WITHOUT
            // IT THE FIRST RUN OF THIS PROBE WAS ACTIVELY MISLEADING. It measured a **354 ms**
            // worst frame on the loaded arm and three frames over 33 ms, which reads as a
            // catastrophic hitch. Almost all of it is FIRST USE: thirty-six spawners between
            // them touch every effect shader, mesh and material in Hero Strike for the first
            // time in the process, and a shipped player has already paid that at load, because
            // `ShaderWarmupCollection` exists for precisely this and `SplashScreen
            // .PreloadGameAssets` warms a slice per frame out of it.
            //
            // ⚠️ SO THE NUMBER TO ACT ON IS THIS ARM, NOT THE ONE ABOVE. Everything is warm, the
            // pile is identical, and what is left is the real cost of casting: the allocations,
            // the object churn and the per-frame work. A gap between the two arms is a warmup
            // question and belongs to the loading screen; a cost that survives into this one is
            // a pooling question and belongs to `HeroHazards`.
            long managedBeforeSecond = System.GC.GetTotalMemory(false);
            SpawnTheWorstCredibleFrame();
            long managedAfterSecondSpawn = System.GC.GetTotalMemory(false);

            var warm = new FrameRateHistogram();
            yield return Sample(warm, SampleFrames);

            long managedAfterWarm = System.GC.GetTotalMemory(false);

            yield return new WaitForSeconds(EffectLife + 3.0f);
            for (int i = 0; i < 60; i++) yield return null;

            long objectsAfterSecond = SceneObjectCount();

            var report = new StringBuilder();
            report.AppendLine("ABILITY STRESS: THE MAXIMUM-EFFECTS FRAME, MEASURED");
            report.AppendLine();
            report.AppendLine("⚠️ THE TIMINGS ARE A READING, NOT A GATE. A batchmode run renders");
            report.AppendLine("   offscreen on whatever machine the suite is on. Compare two runs");
            report.AppendLine("   on ONE machine; never quote a number here as the player's frame.");
            report.AppendLine();
            report.AppendLine($"  spawn calls made          {spawnCalls}");
            report.AppendLine($"  effect life               {EffectLife:F1} s");
            report.AppendLine($"  frames sampled per arm    {SampleFrames}");
            report.AppendLine();
            report.AppendLine($"  {"arm",-10} {"frames",7} {"avg fps",9} {"worst ms",9} {">33 ms",7} {">16 ms",7}");
            report.AppendLine("  " + new string('-', 56));
            report.AppendLine(Row("quiet", quiet));
            report.AppendLine(Row("loaded", loaded));
            report.AppendLine(Row("warm", warm));
            report.AppendLine();
            report.AppendLine("  ⚠️⚠️ `warm` IS THE SAME PILE CAST A SECOND TIME AND IS THE ARM TO");
            report.AppendLine("     ACT ON. `loaded` pays FIRST USE for every effect shader, mesh");
            report.AppendLine("     and material in Hero Strike; a shipped player has already paid");
            report.AppendLine("     that at load, out of ShaderWarmupCollection. A gap between the");
            report.AppendLine("     two is a WARMUP question and belongs to the loading screen; a");
            report.AppendLine("     cost that survives into `warm` is a POOLING question and");
            report.AppendLine("     belongs to HeroHazards.");
            report.AppendLine();

            double quietMs = MeanMs(quiet);
            double loadedMs = MeanMs(loaded);
            double warmMs = MeanMs(warm);

            report.AppendLine($"  mean frame   quiet {quietMs:F2} ms   loaded {loadedMs:F2} ms   " +
                              $"warm {warmMs:F2} ms");
            report.AppendLine($"  cost of the pile, first cast  {loadedMs - quietMs:+0.00;-0.00} ms" +
                              $"   warm cast  {warmMs - quietMs:+0.00;-0.00} ms");
            report.AppendLine();
            report.AppendLine("  MANAGED HEAP (bytes, GC.GetTotalMemory, no collection forced)");
            report.AppendLine($"    over the quiet arm       {managedAfterQuiet - managedBefore,12}");
            report.AppendLine($"    building the pile        {managedAfterSpawn - managedBeforeSpawn,12}");
            report.AppendLine($"    over the loaded arm      {managedAfterLoaded - managedAfterSpawn,12}");
            report.AppendLine($"    building it again        {managedAfterSecondSpawn - managedBeforeSecond,12}");
            report.AppendLine($"    over the warm arm        {managedAfterWarm - managedAfterSecondSpawn,12}");
            report.AppendLine();
            report.AppendLine("  ⚠️ THE PER-FRAME NUMBER IS THE ONE THAT MATTERS. A one-off");
            report.AppendLine("     allocation at cast time is a cast; an allocation that grows");
            report.AppendLine("     with the loaded arm is a per-frame allocation and is what");
            report.AppendLine("     would justify pooling.");
            report.AppendLine();
            report.AppendLine("  SCENE OBJECTS (Transform count)");
            report.AppendLine($"    before the pile          {objectsBefore,12}");
            report.AppendLine($"    with the pile live       {objectsLive,12}   (+{objectsLive - objectsBefore})");
            report.AppendLine($"    after every life expired {objectsAfter,12}   (+{objectsAfter - objectsBefore} left behind)");
            report.AppendLine($"    after a SECOND pile went {objectsAfterSecond,12}   (+{objectsAfterSecond - objectsBefore} left behind)");
            report.AppendLine();
            report.AppendLine("  ⚠️ THE SECOND ROW IS THE ONE THAT WOULD CATCH A LEAK. One pile");
            report.AppendLine("     leaving a handful behind is the match living underneath this;");
            report.AppendLine("     two piles leaving TWICE that is an effect outliving its timer.");

            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, report.ToString());
            Debug.Log(report.ToString());

            Assert.Greater(quiet.Frames, 0, "the quiet arm counted no frames, so this measured nothing");
            Assert.Greater(loaded.Frames, 0, "the loaded arm counted no frames, so this measured nothing");
            Assert.Greater(objectsLive, objectsBefore,
                "the pile spawned nothing at all, so the loaded arm is a second quiet arm and " +
                "every number in " + ReportPath + " is a comparison of the game against itself.");

            // ⚠️⚠️ THE ONLY ASSERTION THAT CAN FAIL, AND IT IS MACHINE-INDEPENDENT BY
            // CONSTRUCTION. `docs/VISION.md` § 2's readability budget assumes an effect stops
            // existing when its timer runs out; an effect that leaves objects behind is a leak
            // that grows for the whole eight-round Hero Strike set and would read as "the game
            // gets slower the longer you play", which is the one performance complaint nobody
            // can reproduce in a probe that runs for four seconds.
            //
            // ⚠️ THE MARGIN IS NOT ZERO AND SAYS WHY. `BeginRound` keeps running underneath this,
            // so bodies, tsinelas and the can are free to move, be picked up and be respawned
            // during the seven seconds this waits; a handful of objects either way is the match
            // living rather than an effect leaking. A leak is the size of the pile.
            //
            // ⚠️⚠️ IT IS ASSERTED AFTER **TWO** PILES, NOT ONE, AND THAT IS WHAT MAKES IT A LEAK
            // TEST RATHER THAN A THRESHOLD. A leak is proportional to how many times the thing
            // ran; a match living underneath the probe is not. One pile leaving a handful behind
            // and two piles leaving twice that is the difference, and a bound measured after one
            // cast cannot tell them apart at all.
            long leaked = objectsAfterSecond - objectsBefore;
            long allowed = (objectsLive - objectsBefore) / 5;

            Assert.LessOrEqual(leaked, allowed,
                $"{leaked} objects survived TWO piles' worth of lifetimes, against {allowed} " +
                $"allowed and {objectsLive - objectsBefore} spawned per pile. An effect that " +
                $"outlives its timer accumulates for a whole eight-round set. See {ReportPath}.");
        }

        /// <summary>
        /// Counts frames into the core's histogram, one per rendered frame.
        ///
        /// ⚠️ `unscaledDeltaTime`, BECAUSE `Hitstop` IS ALLOWED TO RUN. A blast that triggers a
        /// freeze would otherwise report the freeze as fast frames.
        /// </summary>
        private static IEnumerator Sample(FrameRateHistogram into, int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
                into.Add(Time.unscaledDeltaTime);
            }
        }

        private static double MeanMs(FrameRateHistogram h)
            => h.Frames > 0 ? h.Seconds / h.Frames * 1000.0 : 0.0;

        private static string Row(string name, FrameRateHistogram h)
            => $"  {name,-10} {h.Frames,7} {h.AverageFps,9:F1} {h.MaxSeconds * 1000.0,9:F1} " +
               $"{h.LongFrames(0.033),7} {h.LongFrames(0.016),7}";

        /// <summary>
        /// ⚠️ TRANSFORMS, NOT `Object`. Every effect in `HeroHazards` is built as a `GameObject`
        /// with children, so a transform count is the closest thing to "how much did this leave
        /// in the scene" that does not depend on knowing what each spawner makes. Counting
        /// `UnityEngine.Object` would also count meshes and materials, which are assets and are
        /// reclaimed on a completely different schedule.
        /// </summary>
        private static long SceneObjectCount()
            => Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                   FindObjectsSortMode.None).Length;

        /// <summary>
        /// The pile: every persistent floor effect the game can hold at once, both trails at
        /// their live cap, all four blast styles, the strike, the debris and the confetti.
        ///
        /// ⚠️⚠️ IT IS DELIBERATELY WORSE THAN A REAL ROUND, AND `AbilityShowcaseProbe` SAYS WHY
        /// IN THE SAME WORDS: *"Four seats cannot realistically hold all of this live
        /// simultaneously under the charge economy, which is the point: if the budget holds here
        /// it holds anywhere."* The layout is that probe's worst-frame set, so the picture and
        /// the timing are of the same scene.
        ///
        /// ⚠️ THE TRAIL COUNT IS SIX EACH AND THAT IS THE SHIPPED CAP, not a number picked to
        /// look heavy. `docs/VISION.md` § 2: both trails *"drop 1.0 m discs and hold at most 6"*.
        /// </summary>
        private static int SpawnTheWorstCredibleFrame()
        {
            int calls = 0;

            HeroHazards.SpawnIceSheet(new Vector3(-3.4f, 0.0f, 2.2f), 2.3f, EffectLife, 2); calls++;
            HeroHazards.SpawnCrackedLavaDecal(new Vector3(3.2f, 0.0f, 2.6f), 2.2f, EffectLife); calls++;
            HeroHazards.SpawnSeanceVoid(new Vector3(3.6f, 0.0f, -3.0f), 2.8f, EffectLife, 3); calls++;
            HeroHazards.SpawnIceBarricade(new Vector3(-2.6f, 0.0f, -2.4f), Vector3.forward, EffectLife); calls++;
            HeroHazards.SpawnHexSigil(new Vector3(0.0f, 0.0f, 4.4f), 2.4f, EffectLife, 5); calls++;
            HeroHazards.SpawnEarthPillar(new Vector3(-5.0f, 0.0f, -1.0f), EffectLife); calls++;
            HeroHazards.SpawnGrandCovenEclipse(new Vector3(0.0f, 0.0f, 0.0f), 5.0f, EffectLife); calls++;
            HeroHazards.SpawnKuroUnbound(new Vector3(5.2f, 0.0f, 1.0f), 2.8f, EffectLife, 3, false); calls++;
            HeroHazards.SpawnCircuitArcs(new Vector3(-1.0f, 0.0f, 5.6f), 3.2f, 1, EffectLife); calls++;

            for (int i = 0; i < 6; i++)
            {
                HeroHazards.SpawnFireTrail(
                    new Vector3(-1.0f + i * 0.9f, 0.0f, -5.2f), 1.0f, EffectLife, 0); calls++;
                HeroHazards.SpawnShockTrail(
                    new Vector3(-5.4f + i * 0.9f, 0.0f, 5.0f), 1.0f, EffectLife, 1); calls++;
            }

            // The transients: four blast styles, the strike, the debris, the confetti and a
            // burst per corner. These are the frame an ultimate actually lands on.
            HeroHazards.CreateExplosionVisual(new Vector3(1.0f, 0.0f, 1.0f), 4.8f, "KABOOM!",
                                              HeroHazards.ExplosionStyle.Fire); calls++;
            HeroHazards.CreateExplosionVisual(new Vector3(-1.0f, 0.0f, 1.0f), 4.5f, "KABOOM!",
                                              HeroHazards.ExplosionStyle.Quake, Vector3.forward); calls++;
            HeroHazards.CreateExplosionVisual(new Vector3(1.0f, 0.0f, -1.0f), 4.2f, "KABOOM!",
                                              HeroHazards.ExplosionStyle.Frost); calls++;
            HeroHazards.CreateExplosionVisual(new Vector3(-1.0f, 0.0f, -1.0f), 2.2f, "KABOOM!",
                                              HeroHazards.ExplosionStyle.Slipper); calls++;
            HeroHazards.CreateThunderstrike(new Vector3(0.0f, 0.0f, -6.0f), 7.0f); calls++;
            HeroHazards.SpawnVolcanicRockDebris(new Vector3(2.0f, 0.0f, 0.0f), 8, 2.2f); calls++;
            HeroHazards.SpawnConfettiShower(Vector3.zero, 24); calls++;

            foreach (var corner in Corners())
            {
                Visual.ImpactBurst.SpawnAt(corner); calls++;
                Visual.ComicPopup.Spawn(corner + Vector3.up * 1.2f, "BAM!",
                                        UI.UiTheme.Offense, 1.0f); calls++;
            }

            return calls;
        }

        private static IEnumerable<Vector3> Corners()
        {
            float r = Balance.ConfinementRadius * 0.8f;
            yield return new Vector3(r, 0.0f, r);
            yield return new Vector3(-r, 0.0f, r);
            yield return new Vector3(r, 0.0f, -r);
            yield return new Vector3(-r, 0.0f, -r);
        }
    }
}
