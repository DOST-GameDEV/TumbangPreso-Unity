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

        /// <summary>
        /// Puts the studio's mark on the one screen the engine insists on drawing.
        ///
        /// ⚠️⚠️ UNITY'S SPLASH CANNOT BE TURNED OFF ON THIS LICENCE, so the alternative is not
        /// "no engine logo", it is an unbranded engine logo in front of the game's own opening
        /// animation. Adding the BH Studios mark beside it and matching the background to the
        /// menu's navy makes the two read as one sequence rather than as an interruption. The
        /// boot sting starts before this screen (see <c>BootSting</c>), so it has sound too.
        /// </summary>
        private static void ConfigureSplash()
        {
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/TumbangPreso/Art/ui/brand/bh_studios_logo.png");

            if (logo == null)
            {
                var importer = AssetImporter.GetAtPath(
                    "Assets/TumbangPreso/Art/ui/brand/bh_studios_logo.png") as TextureImporter;

                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.SaveAndReimport();

                    logo = AssetDatabase.LoadAssetAtPath<Sprite>(
                        "Assets/TumbangPreso/Art/ui/brand/bh_studios_logo.png");
                }
            }

            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = true;
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.UnityLogoBelow;

            // The menu's own navy, so the engine logo hands over to the sting without a flash.
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.0157f, 0.0314f, 0.2196f, 1.0f);

            if (logo == null)
            {
                Debug.LogWarning("[Build] no BH Studios logo for the splash; " +
                                 "the engine logo will show on its own.");
                return;
            }

            PlayerSettings.SplashScreen.logos = new[]
            {
                PlayerSettings.SplashScreenLogo.Create(2.0f, logo),
            };
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

            // ⚠️ THE NAME COMES FROM SceneFlow, NOT A LITERAL. This guard hard-coded
            // "Splash.unity" while the scene has always been "SplashScreen.unity", so it
            // rejected a correctly-ordered build every time — a guard that only ever fires on
            // valid input is worse than no guard, because it trains you to bypass it.
            if (!scenes[0].EndsWith($"/{TumbangPreso.UI.SceneFlow.Splash}.unity"))
            {
                Debug.LogError($"[Build] scene 0 is '{scenes[0]}', not " +
                               $"{TumbangPreso.UI.SceneFlow.Splash}.unity. A built " +
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

            ConfigureSplash();

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
