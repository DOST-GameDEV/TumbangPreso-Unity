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
            if (File.Exists("Temp/cutscene-film.lock")) return;
            string output = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            var execute = typeof(GameBuilder).GetMethod("Execute", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            bool ok = (bool)execute.Invoke(null, new object[] { output, BuildTarget.StandaloneWindows64 });
            File.WriteAllText(Done, (ok ? "OK " : "FAIL ") + output + "\n");
        }
    }
}
