using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Every editor check, in ONE Unity launch.
    ///
    /// ⚠️⚠️ THE LAUNCHES ARE THE COST, NOT THE ASSERTIONS. 🧑, 2026-08-25: *"we have too many
    /// tests and we are wasting so many credits to run them all and fix the code for the test"*.
    /// `docs/TODO.md` § 7 measured where that goes: a full verification pass was Core, plus
    /// EditMode, plus PlayMode, plus **four separate `-executeMethod` launches** for
    /// `HeadlessCheck`, `ArenaCheck`, `MapGeometryCheck` and `AudioCueCheck`. Each launch pays
    /// the full editor start, the asset database open and the script compile, and the four
    /// checks between them do a few seconds of actual work. This is § 7 item 1, and it changes
    /// no test logic at all.
    ///
    /// ⚠️⚠️ IT RUNS ALL FOUR EVEN AFTER ONE FAILS. Stopping at the first is how a session fixes
    /// one thing, relaunches, finds the second, fixes it, relaunches, finds the third: the exact
    /// cost this exists to remove. Every check writes its own report file as it goes, so the
    /// batched run leaves the same evidence four separate runs did.
    ///
    /// ⚠️ AND IT REPORTS PER CHECK RATHER THAN AS ONE VERDICT. A single "FAILED" line tells the
    /// next reader to open four log files. The summary names which check went red so they can
    /// open one.
    ///
    /// ⚠️ `SceneScriptCheck` IS IN HERE TOO AND `GameBuilder` STILL RUNS IT SEPARATELY. That is
    /// deliberate, not duplication: it is the only check that can see a scene holding a component
    /// the PLAYER cannot bind (`CLAUDE.md` § 7.1), a shipped build once crashed with everything
    /// else green, and a gate that runs at build time must not depend on somebody having run this
    /// first.
    ///
    /// <code>
    /// Unity.exe -batchmode -projectPath . \
    ///   -executeMethod TumbangPreso.EditorTools.Checks.RunAll -logFile Logs/checks.log
    /// </code>
    /// </summary>
    public static class Checks
    {
        private readonly struct Check
        {
            public readonly string Name;
            public readonly Func<bool> Run;
            public readonly string Report;

            public Check(string name, Func<bool> run, string report)
            {
                Name = name;
                Run = run;
                Report = report;
            }
        }

        [MenuItem("Tumbang Preso/Run All Checks")]
        public static void RunFromMenu() => Execute();

        public static void RunAll() => EditorApplication.Exit(Execute() ? 0 : 1);

        public static bool Execute()
        {
            var checks = new List<Check>
            {
                // ⚠️ HEADLESS FIRST, because it is the one that answers "did anything compile
                // and is the rules core reachable from Unity at all". A failure here makes the
                // other three meaningless rather than merely also red.
                new Check("headless", HeadlessCheck.Execute, "Logs/headless-check.txt"),

                new Check("arena", () => MapKit.ArenaCheck.Execute(
                              AIController.PlayableHalfX, AIController.PlayableHalfZ),
                          "Logs/arena-check.txt"),

                new Check("map geometry", () => MapKit.MapGeometryCheck.Execute(true),
                          "Logs/map-geometry-check.txt"),

                new Check("audio cues", AudioCueCheck.Execute, "Logs/audio-cue-check.txt"),

                new Check("scene scripts", () => SceneScriptCheck.Execute(true),
                          "Logs/scene-script-check.txt"),

                // ⚠️⚠️ IT IS IN THE GATE RATHER THAN BEING A TEST BECAUSE IT IS THE ONLY CHECK
                // THAT CAN SEE A SCREEN NOBODY OPENED. Every runtime probe measures screens that
                // got loaded; this one reads the source, so a screen added on a branch and not
                // yet wired to a door is still caught. That is the same argument
                // `SceneScriptCheck` makes one line above, and `docs/TODO.md` § 96, § 114 and
                // § 124.11 are the three times an unreached screen went unmeasured.
                new Check("input surface", () => InputSurfaceCheck.Execute(true),
                          "Logs/input-surface-check.txt"),

                // ⚠️⚠️ IT REGENERATES THE COLLECTION RATHER THAN INSPECTING IT, which is the
                // `CLAUDE.md` § 4a argument: a warm-up list that is checked but not rewritten
                // goes stale the first time somebody adds a material, and a stale one warms the
                // wrong shaders while looking exactly like a working one. `docs/TODO.md`
                // § 126.10 is the ANR it replaced.
                new Check("shader warmup", ShaderWarmupCollection.Execute, ""),
            };

            var failed = new List<string>();
            var summary = new StringBuilder();
            summary.AppendLine("ALL CHECKS, ONE LAUNCH");
            summary.AppendLine();

            foreach (var check in checks)
            {
                bool ok;

                // ⚠️ A THROWN CHECK IS A FAILED CHECK, NOT A DEAD RUN. One of these blowing up
                // on a missing scene used to take the whole launch down with it, and the three
                // that had not run yet reported nothing at all.
                try
                {
                    ok = check.Run();
                }
                catch (Exception e)
                {
                    ok = false;
                    Debug.LogError($"[Checks] {check.Name} threw: {e}");
                }

                if (!ok) failed.Add(check.Name);

                summary.AppendLine($"  {(ok ? "OK  " : "FAIL")}  {check.Name,-14} {check.Report}");
            }

            summary.AppendLine();
            summary.AppendLine(failed.Count == 0
                ? $"RESULT: OK. All {checks.Count} checks passed in one launch."
                : $"RESULT: FAILED. {string.Join(", ", failed)}. " +
                  "Open that check's own report file above.");

            Debug.Log(summary.ToString());

            try
            {
                System.IO.Directory.CreateDirectory("Logs");
                System.IO.File.WriteAllText("Logs/checks.txt", summary.ToString());
            }
            catch (Exception e)
            {
                Debug.LogError($"[Checks] could not write Logs/checks.txt: {e.Message}");
            }

            return failed.Count == 0;
        }
    }
}
