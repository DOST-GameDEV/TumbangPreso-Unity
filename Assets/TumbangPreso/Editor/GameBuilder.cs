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
    /// ⚠️⚠️ IT RUNS `SceneScriptCheck` BEFORE BUILDING AND DELETES THE PREVIOUS PLAYER FIRST.
    /// Both replaced something that shipped. The check catches a component the player cannot
    /// bind to a script, which crashed the released build on the Ilalim ng Tulay map select
    /// while every editor-side test stayed green. The delete makes every build a clean build,
    /// which `CLAUDE.md` § 7 asked for in prose and therefore did not always get.
    ///
    /// Run:
    ///   Unity.exe -batchmode -quit -nographics -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.GameBuilder.BuildWindows
    /// </summary>
    public static class GameBuilder
    {
        [MenuItem("Tumbang Preso/Build Windows Player")]
        public static void BuildFromMenu() => Execute(DefaultOutput(), BuildTarget.StandaloneWindows64);

        [MenuItem("Tumbang Preso/Build macOS Player")]
        public static void BuildMacFromMenu() => Execute(DefaultMacOutput(), BuildTarget.StandaloneOSX);

        public static void BuildWindows()
        {
            bool ok = Execute(CommandLineOutput() ?? DefaultOutput(), BuildTarget.StandaloneWindows64);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static void BuildMac()
        {
            bool ok = Execute(CommandLineOutput() ?? DefaultMacOutput(), BuildTarget.StandaloneOSX);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static string CommandLineOutput()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], "-buildOutput", StringComparison.OrdinalIgnoreCase))
                    continue;

                return string.IsNullOrWhiteSpace(args[i + 1]) ? null : args[i + 1];
            }

            return null;
        }

        private static string DefaultOutput()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            return Path.Combine(desktop, "TumbangPreso-Unity", "TumbangPreso.exe");
        }

        public static string DefaultMacOutput()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "Builds", "macOS", "TumbangPreso.app");
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
        /// this game builds at runtime (the arms, the tsinelas in the viewmodel hand, the aim
        /// arc, the impact burst, the map's sky) is created from a shader looked up by NAME,
        /// and nothing in any scene or Resources folder points at those shaders. The build
        /// therefore drops them, `Shader.Find` returns null in the player only, and the objects
        /// render as the error material. It is the same class of bug as the stripped animation
        /// clips: correct in the editor, broken exclusively in the thing you hand somebody.
        /// </summary>
        /// <summary>
        /// Writes the branch this build came off into `Resources/BuildBranch.txt`, which is the
        /// only way a player can know: there is no git inside a shipped .exe.
        ///
        /// ⚠⚠ IT RUNS ON EVERY BUILD AND IS NOT A THING ANYBODY HAS TO REMEMBER. The whole
        /// value of the corner stamp is that it is true without effort; a step you run by hand
        /// before a build is a step that is skipped on the build that mattered, which is exactly
        /// the history `PurgeOutputDirectory` has and why that one moved into code too.
        ///
        /// ⚠️ THE FILE IS WRITTEN EVEN WHEN THE NAME IS EMPTY, so a build off `main` or a
        /// detached HEAD OVERWRITES the previous branch's stamp instead of inheriting it. An
        /// empty file means "show the version number" and a missing one would mean the same
        /// thing, but only until a stale one from three branches ago is left lying beside it.
        /// </summary>
        private static void StampBuildBranch()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            string branch = BuildBranch.FromGit(root) ?? "";

            string dir = Path.Combine(Application.dataPath, "TumbangPreso/Resources");
            string file = Path.Combine(dir, BuildBranch.ResourceName + ".txt");

            try
            {
                Directory.CreateDirectory(dir);
                File.WriteAllText(file, branch);

                // The asset has to exist in the database before `BuildPipeline.BuildPlayer` walks
                // Resources, or the player ships without it and silently falls back to the number.
                AssetDatabase.ImportAsset("Assets/TumbangPreso/Resources/" +
                                          BuildBranch.ResourceName + ".txt",
                                          ImportAssetOptions.ForceUpdate);

                // ⚠️ THE EDITOR'S OWN CACHE IS DROPPED TOO. `BuildBranch` memoises the name on
                // first read, and this editor process may have read it on a different branch.
                BuildBranch.Forget();

                Debug.Log(string.IsNullOrEmpty(branch)
                    ? "[Build] no branch name; the corner stamp will show the version."
                    : $"[Build] corner stamp is the branch name: {branch}");
            }
            catch (Exception e)
            {
                // ⚠️ NOT FATAL. A stamp that cannot be written costs a label, and refusing to
                // build over a cosmetic file would be a worse trade than shipping "v4.72".
                Debug.LogWarning($"[Build] could not write the branch stamp: {e.Message}");
            }
        }

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

                // ⚠️ THE WORLD OUTLINE PROTOTYPE, ADDED WHEN IT WAS SWITCHED ON. It was
                // deliberately left OUT while it was inert, so a disabled prototype did not ship
                // its variants into every build. `CameraRig` now attaches and enables it, so the
                // reasoning inverted: reached through `Shader.Find`, it would be stripped, and the
                // pass logs an error and draws nothing. The editor would show the outline and the
                // .exe would not, which is the exact split rule 10 exists to prevent.
                //
                // ⚠️ IF THE PROTOTYPE IS REJECTED, DELETE THIS LINE WITH THE FEATURE. A shader
                // listed here is compiled into every player whether or not anything draws it.
                "TumbangPreso/WorldOutline",

                // ⚠️ THE ANTI-ALIASING FILTER, AND ITS MISS PATH IS THE MOST DECEPTIVE ON THIS
                // LIST. Every mode above Off in `Settings.AntiAliasModes` is "MSAA plus FXAA",
                // so a build that strips this still anti-aliases: the MSAA half is a
                // `QualitySettings` field that needs no shader and keeps working. What the
                // player gets is the setting doing roughly half of what it says while the editor
                // does all of it, which is far harder to notice than a control that does
                // nothing. `PostAntiAlias` logs a warning on the miss for that reason; this line
                // is what stops it being reached.
                "TumbangPreso/Fxaa",

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
                // was added to replace: so a build that strips this ships the bug back while
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

        /// <summary>
        /// Delete the previous player before writing a new one. Every build is a clean build.
        ///
        /// ⚠️⚠️ THIS IS A RULE, NOT AN OPTIMISATION, AND IT IS ENFORCED HERE RATHER THAN IN A
        /// DOCUMENT BECAUSE A DOCUMENT GETS SKIPPED. `CLAUDE.md` § 7 has carried "delete the
        /// previous output folder first" since an incremental rebuild kept a corrupted `level1`
        /// and cost an hour; it was still written as something the operator had to remember, and
        /// so it was forgotten. Unity happily rewrites `TumbangPreso_Data` while reusing the
        /// byte-identical launcher, which leaves Explorer showing the OLD creation timestamp on
        /// `TumbangPreso.exe` and makes a finished build look like a stale one. Deleting the
        /// directory outright is the only version of this that cannot half-happen.
        ///
        /// ⚠️ IT REFUSES TO DELETE ANYTHING THAT IS NOT OBVIOUSLY A PLAYER OUTPUT. `-buildOutput`
        /// takes an arbitrary path from the command line, so this will not touch a drive root, a
        /// directory holding a `.git`, or any directory that does not already look like a build.
        /// A wrong path should fail the build, never eat a folder.
        /// </summary>
        private static bool PurgeOutputDirectory(string dir)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return true;

            var info = new DirectoryInfo(Path.GetFullPath(dir));

            if (info.Parent == null)
            {
                Debug.LogError($"[Build] output '{dir}' is a drive root. Refusing to build there.");
                return false;
            }

            if (Directory.Exists(Path.Combine(info.FullName, ".git")))
            {
                Debug.LogError($"[Build] output '{dir}' holds a git repository. Refusing to delete it.");
                return false;
            }

            // What a Unity player always leaves behind. If none of it is here, this
            // directory is something else and deleting it is not this method's business.
            //
            // The .app entry is the macOS half of the same test. BuildMac writes a bundle
            // rather than an .exe beside a _Data folder, so a Windows-only check refused to
            // purge a perfectly ordinary previous macOS build and failed the build instead.
            bool looksLikeAPlayer =
                File.Exists(Path.Combine(info.FullName, "UnityPlayer.dll")) ||
                Directory.Exists(Path.Combine(info.FullName, "TumbangPreso_Data")) ||
                Directory.GetDirectories(info.FullName, "*.app").Length > 0 ||
                info.GetFileSystemInfos().Length == 0;

            if (!looksLikeAPlayer)
            {
                Debug.LogError($"[Build] output '{dir}' exists but does not look like a previous " +
                               "player (no UnityPlayer.dll, no TumbangPreso_Data, no .app). " +
                               "Refusing to delete it. Move it aside or point -buildOutput " +
                               "somewhere else.");
                return false;
            }

            try
            {
                Directory.Delete(info.FullName, recursive: true);
                Debug.Log($"[Build] deleted the previous player at {info.FullName}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Build] could not delete '{dir}': {e.Message}. The game or a " +
                               "file browser is probably still holding it open. Close it and " +
                               "build again rather than shipping a half-overwritten player.");
                return false;
            }
        }

        private static bool Execute(string outputPath, BuildTarget target = BuildTarget.StandaloneWindows64)
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
            // rejected a correctly-ordered build every time. A guard that only ever fires on
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

            // ⚠️⚠️ A SCENE WHOSE COMPONENTS THE PLAYER CANNOT BIND IS A CRASH, AND IT IS THE
            // ONE FAILURE THE REST OF THE GATE IS STRUCTURALLY BLIND TO. See `SceneScriptCheck`:
            // the editor resolves an inline MonoScript stub by class name and the player cannot,
            // so every in-editor test passes and the shipped .exe dies on the scene load. This
            // runs BEFORE the build because the point is to never write the broken player at
            // all, and because the crash it prevents cost a whole handoff to find.
            if (!SceneScriptCheck.Execute(gate: true))
            {
                Debug.LogError("[Build] refusing to build: a scene holds a script reference the " +
                               "player cannot bind. See Logs/scene-script-check.txt.");
                return false;
            }

            string dir = Path.GetDirectoryName(outputPath);
            if (!PurgeOutputDirectory(dir)) return false;
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            PlayerSettings.companyName = "BH Studios";
            PlayerSettings.productName = "Tumbang Preso";

            ConfigureSplash();
            ConfigureIcon();
            EnsureRuntimeShaders();
            StampBuildBranch();

            // Ship at the monitor's native resolution in borderless fullscreen. Starting the
            // player in a fixed 1600x900 window made a normal build look like a test harness;
            // switching that window to fullscreen could also leave Unity stretching the same
            // low-resolution backbuffer, which blurred the whole presentation.
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
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
