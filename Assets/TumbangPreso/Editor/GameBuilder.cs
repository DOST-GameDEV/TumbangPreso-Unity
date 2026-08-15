using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Builds the Windows player.
    ///
    /// ⚠️ IT REFUSES TO BUILD A GAME THAT WOULD OPEN ON THE WRONG SCENE. Scene 0 is what a
    /// built player starts with, and the boot sting is meant to play on every launch. In the
    /// editor the flow works regardless because scenes load by path; in a build, a wrong index
    /// 0 means the shipped game opens on whatever was saved last. That is a bug you only find
    /// after handing somebody the .exe.
    ///
    /// ⚠️ AND IT FAILS LOUDLY ON A MISSING SCENE. A scene absent from the build settings loads
    /// fine in the editor and does nothing in a build, so every button that leads to it becomes
    /// dead on the shipped copy only.
    ///
    /// Run:
    ///   Unity.exe -batchmode -quit -nographics -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.GameBuilder.BuildWindows
    /// </summary>
    public static class GameBuilder
    {
        [MenuItem("Tumbang Preso/Build Windows Player")]
        public static void BuildFromMenu() => Execute(DefaultOutput());

        public static void BuildWindows()
        {
            bool ok = Execute(DefaultOutput());
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static string DefaultOutput()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            return Path.Combine(desktop, "TumbangPreso-Unity", "TumbangPreso.exe");
        }

        private static bool Execute(string outputPath)
        {
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[Build] no scenes are enabled in the build settings.");
                return false;
            }

            if (!scenes[0].EndsWith("/Splash.unity"))
            {
                Debug.LogError($"[Build] scene 0 is '{scenes[0]}', not Splash.unity. A built " +
                               "player opens on scene 0, so this would ship without the boot " +
                               "sting and land the player somewhere arbitrary. Run " +
                               "Tumbang Preso > Build All Menu Scenes first.");
                return false;
            }

            foreach (var s in scenes)
            {
                if (File.Exists(s)) continue;

                Debug.LogError($"[Build] scene '{s}' is listed but missing from disk.");
                return false;
            }

            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            PlayerSettings.companyName = "BH Studios";
            PlayerSettings.productName = "Tumbang Preso";

            // ⚠️ WINDOWED BY DEFAULT FOR A TEST BUILD. An exclusive-fullscreen build that
            // starts on a broken frame is genuinely hard to get out of, and the whole point of
            // this build is that somebody is about to look at it critically.
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            Debug.Log($"[Build] building {scenes.Length} scenes to {outputPath}");
            foreach (var s in scenes) Debug.Log($"[Build]   {s}");

            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Build] SUCCEEDED. {summary.totalSize / (1024 * 1024)} MB, " +
                          $"{summary.totalTime.TotalSeconds:F0}s -> {outputPath}");
                return true;
            }

            Debug.LogError($"[Build] FAILED: {summary.result}, {summary.totalErrors} error(s).");

            foreach (var step in report.steps)
                foreach (var msg in step.messages)
                    if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        Debug.LogError($"[Build]   {step.name}: {msg.content}");

            return false;
        }
    }
}
