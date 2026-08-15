using System;
using TumbangPreso.Core;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// A headless smoke check, for CI and for any agent working without the editor open.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE `Unity.exe -batchmode -quit` PROVES NOTHING. It exits after
    /// package registration, before script compilation, and still returns exit code 0. That
    /// reads as a clean build and is not one. Reaching this method AT ALL means every
    /// assembly compiled, because `-executeMethod` cannot invoke a method in an assembly
    /// that failed to build.
    ///
    /// Run:
    ///   Unity.exe -batchmode -quit -nographics -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.HeadlessCheck.Run -logFile -
    /// </summary>
    public static class HeadlessCheck
    {
        /// <summary>
        /// ⚠️ RESULTS GO TO A FILE, NOT ONLY TO Debug.Log. `EditorApplication.Exit` kills the
        /// process before Unity flushes its log buffer, so the first version of this check
        /// compiled everything, ran, and left NO trace of its own output in the log: it was
        /// indistinguishable from a method that never ran. A file write is flushed
        /// deterministically and can be read back by whatever invoked Unity.
        /// </summary>
        private const string ResultPath = "Logs/headless-check.txt";

        private static readonly System.Text.StringBuilder Report = new System.Text.StringBuilder();

        public static void Run()
        {
            int failures = 0;

            try
            {
                // The rules core has to be reachable from Unity, not just from dotnet test.
                // If the local package or its asmdef is misconfigured, this is where it shows.
                Check(ref failures, "roster size", Roster.People.Count == 12);
                Check(ref failures, "cans", Roster.Cans.Count == 4);
                Check(ref failures, "slippers", Roster.Slippers.Count == 4);

                Check(ref failures, "neutral is exactly 1.0",
                    Mathf.Approximately(Roster.TraitScale(3, Balance.TraitSpeedPerPoint), 1.0f));

                Check(ref failures, "every slot defends exactly once",
                    MatchRules.DefenderSlotFor(1) == 0 && MatchRules.DefenderSlotFor(4) == 3);

                Check(ref failures, "the box is a square",
                    Confinement.IsInsideBox(6.9f, 6.9f) && !Confinement.IsInsideBox(7.1f, 0.0f));

                Check(ref failures, "shove solves to 2.50 m",
                    Mathf.Abs(Combat.ShoveDistance() - 2.50f) < 0.01f);

                Check(ref failures, "hit window on BOYBEN",
                    Mathf.Abs(ThrowRules.HitWindow(1) - 0.493f) < 0.001f);
            }
            catch (Exception e)
            {
                Report.AppendLine($"THREW: {e}");
                failures++;
            }

            Report.AppendLine(failures > 0
                ? $"RESULT: FAILED with {failures} problem(s)."
                : "RESULT: OK. All assemblies compiled and the rules core is reachable from Unity.");

            Flush();

            Debug.Log(Report.ToString());
            EditorApplication.Exit(failures > 0 ? 1 : 0);
        }

        private static void Flush()
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(ResultPath);
                if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(ResultPath, Report.ToString());
            }
            catch (Exception e)
            {
                Debug.LogError($"[HeadlessCheck] could not write {ResultPath}: {e.Message}");
            }
        }

        private static void Check(ref int failures, string what, bool ok)
        {
            Report.AppendLine(ok ? $"ok   : {what}" : $"FAIL : {what}");
            if (!ok) failures++;
        }
    }
}
