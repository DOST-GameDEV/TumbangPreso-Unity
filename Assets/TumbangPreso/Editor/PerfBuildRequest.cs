using System.IO;
using UnityEditor;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// `Temp/perfbuild.request` holding an output path builds the Windows player into it from the OPEN editor (owner,
    /// 2026-10-29: the integrated-GPU pass, measured on the Ryzen's Radeon with `-tp-graphicsreport`). Batch builds need
    /// the owner's editor closed; this does not. The answer is `Temp/perfbuild.done`.
    /// </summary>
    [InitializeOnLoad]
    internal static class PerfBuildRequest
    {
        private const string Request = "Temp/perfbuild.request", Done = "Temp/perfbuild.done";

        static PerfBuildRequest() => EditorApplication.update += Watch;

        private static void Watch()
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (File.Exists("Temp/cutscene-film.lock") && !File.ReadAllText("Temp/cutscene-film.lock").StartsWith("kanto perfbuild")) return;   // our own lock is the one we build under
            string output = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            bool development = output.StartsWith("dev ");   // `dev PATH`: markers readable, for the uneven frames (owner, 2026-10-31)
            if (development) output = output.Substring(4).Trim();
            var execute = typeof(GameBuilder).GetMethod("Execute", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            // Frame timing stats on for the perf build only, so `FrameBudgetProbe` reads main, render and GPU times
            // (owner, 2026-10-30: "i want it so optimized i'm able to run at 300+ fps").
            bool timing = PlayerSettings.enableFrameTimingStats;
            PlayerSettings.enableFrameTimingStats = true;
            GameBuilder.ForceDevelopment = development;
            bool ok;
            try { ok = (bool)execute.Invoke(null, new object[] { output, BuildTarget.StandaloneWindows64 }); }
            finally { PlayerSettings.enableFrameTimingStats = timing; GameBuilder.ForceDevelopment = false; }
            File.WriteAllText(Done, (ok ? "OK " : "FAIL ") + output + "\n");
        }
    }
}
