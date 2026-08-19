using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
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

        /// <summary>
        /// The tansan. `project.godot` sets `config/icon="res://icon.png"` and the Windows
        /// export preset points at `icon.ico`; the same bottlecap has to be on the .exe, on the
        /// taskbar and in the window corner here.
        ///
        /// ⚠️⚠️ THIS WAS NOT SET AT ALL AND THE BUILD SHIPPED WITH UNITY'S OWN CUBE, which is
        /// the single most obvious "this is not the same game" signal a player gets before the
        /// game even opens. Reported with a side-by-side of the two shortcuts.
        ///
        /// ⚠️ `SetIconsForPlatform` WANTS THE FULL SIZE LIST AND SILENTLY KEEPS THE DEFAULT FOR
        /// ANY SIZE LEFT NULL. Passing one texture for one size leaves the taskbar on the Unity
        /// icon while the .exe looks correct in Explorer, which is a confusing half-fix. The same
        /// source texture is handed to every size and Unity downsamples.
        /// </summary>
        private static void ConfigureIcon()
        {
            const string path = "Assets/TumbangPreso/Art/ui/brand/app_icon.png";

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (icon == null)
            {
                Debug.LogWarning($"[Build] no app icon at {path}; the player keeps Unity's.");
                return;
            }

            // ⚠️ IT HAS TO BE READABLE AND UNCOMPRESSED or Unity refuses it as an icon source
            // and falls back without saying so.
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                bool changed = false;

                if (!importer.isReadable) { importer.isReadable = true; changed = true; }

                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    changed = true;
                }

                if (importer.npotScale != TextureImporterNPOTScale.None)
                {
                    importer.npotScale = TextureImporterNPOTScale.None;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
            }

            var group = NamedBuildTarget.Standalone;

            int[] sizes = PlayerSettings.GetIconSizes(group, IconKind.Any);
            var icons = new Texture2D[sizes.Length];

            for (int i = 0; i < icons.Length; i++) icons[i] = icon;

            PlayerSettings.SetIcons(group, icons, IconKind.Any);

            // The default icon is what anything without a per-platform entry falls back to,
            // including the editor's own game view and the WebGL favicon.
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            Debug.Log($"[Build] app icon set for {sizes.Length} sizes.");
        }

        /// <summary>
        /// ⚠️⚠️ A SHADER ONLY `Shader.Find` REFERENCES IS STRIPPED FROM THE BUILD. Every material
        /// this game builds at runtime — the arms, the tsinelas in the viewmodel hand, the aim
        /// arc, the impact burst, the map's sky — is created from a shader looked up by NAME,
        /// and nothing in any scene or Resources folder points at those shaders. The build
        /// therefore drops them, `Shader.Find` returns null in the player only, and the objects
        /// render as the error material. It is the same class of bug as the stripped animation
        /// clips: correct in the editor, broken exclusively in the thing you hand somebody.
        /// </summary>
        private static void EnsureRuntimeShaders()
        {
            string[] wanted =
            {
                "Standard",
                "Sprites/Default",
                "UI/Default",
                "Particles/Standard Unlit",
                "Skybox/Panoramic",

                // ⚠️ THE CHARACTER AND PROP MATERIAL. Nothing in a scene references it: every
                // material that uses it is built at runtime through `Shader.Find`, which is
                // exactly the case rule 10 exists for. Left out, the whole cast, both hero props
                // and the first-person arms render as the missing-shader material in the player
                // and are correct in the editor.
                "TumbangPreso/Toon",

                // ⚠️ THE COLOUR GRADE, FOR THE SAME REASON AND WITH A WORSE FAILURE. `ColourGrade`
                // reaches it through `Shader.Find` too, and its miss path blits the frame through
                // untouched. So a build that strips this looks merely a little flatter than the
                // editor rather than obviously broken, which is the kind of difference nobody
                // catches until the grade is being compared against the Godot build side by side.
                "TumbangPreso/ColourGrade",

                // ⚠️ § THE STUN FROST's SCREEN HALF, AND ITS MISS PATH IS THE QUIETEST OF THE
                // THREE. `Hud.BuildFrostVignette` returns without building anything when the
                // lookup fails, so a build that strips this has no frost at all and no error:
                // the body still ices over for everybody watching, which is the half that makes
                // it look like a working feature from every seat except the one it is for.
                // `StunFrostTests.TheVictimsScreenFrostsOver` is the editor-side guard; this line
                // is the player-side one, and only the pair covers both.
                "TumbangPreso/FrostVignette",

                // ⚠️ THE DANGER VIGNETTE, AND ITS MISS PATH IS A REGRESSION RATHER THAN AN
                // ABSENCE. `Hud.BuildDangerFlash` warns and falls back to a flat `Image` when
                // the lookup fails, which is exactly the uniform full-screen red the vignette
                // was added to replace — so a build that strips this ships the bug back while
                // the editor stays fixed.
                "TumbangPreso/DownedVignette",
            };

            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (settings == null || settings.Length == 0) return;

            var so = new SerializedObject(settings[0]);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            if (list == null) return;

            int added = 0;

            foreach (string name in wanted)
            {
                var shader = Shader.Find(name);
                if (shader == null) continue;

                bool present = false;

                for (int i = 0; i < list.arraySize; i++)
                {
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue != shader) continue;
                    present = true;
                    break;
                }

                if (present) continue;

                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                added++;
            }

            if (added == 0) return;

            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Debug.Log($"[Build] added {added} runtime shaders to the always-included list.");
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
            ConfigureIcon();
            EnsureRuntimeShaders();

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
