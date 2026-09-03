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

        /// <summary>
        /// The browser build. `docs/FUTURE.md` Phase 18: *"A WebGL build in the browser. The
        /// module is already installed. One click from a link converts far better than a download,
        /// and this game is small enough to actually work there."*
        ///
        /// ⚠️⚠️ THE TARGET IS SWITCHED BEFORE THE BUILD, FOR `BuildAndroidPlayer`'S REASON AND
        /// NOT AS A COPY OF IT. Switching is what re-evaluates the platform defines, and this
        /// project branches on them in `Matchmaker.LocalPlatform`, `Matchmaker.LocalInputDevice`
        /// and `TouchHud.ShouldShow`. A player built without the switch compiles those three for
        /// the EDITOR's platform and nothing errors.
        ///
        /// ⚠️⚠️ AND WEBGL IS A DIFFERENT GAME FROM THE OTHER THREE IN ONE RESPECT THAT HAS TO BE
        /// SAID OUT LOUD RATHER THAN DISCOVERED: **the browser has no UDP sockets.** `LanBeacon`
        /// broadcasts over UDP and `UnityTransport` uses it for the LAN path, so **LAN hosting
        /// and LAN join cannot work in a browser build and never will**. The relay path is the
        /// one that works there, which is the same conclusion `Attention.md` § 1 reaches about
        /// the Android emulator from a completely different direction. A WebGL build is therefore
        /// a **shop window**, not a tournament client, and `docs/FUTURE.md` Phase 17's unplugged
        /// LAN requirement is a claim about the DESKTOP player.
        ///
        /// ⚠️ IT IS NOT WIRED INTO ANY GATE AND MUST NOT BE. A WebGL player takes far longer to
        /// link than a desktop one, and `CLAUDE.md` § 2.2's shape is WORK then BUILD: this is a
        /// distribution build, run on purpose when there is something to show.
        /// </summary>
        [MenuItem("Tumbang Preso/Build WebGL Player")]
        public static void BuildWebGlFromMenu() => BuildWebGlPlayer(DefaultWebGlOutput());

        public static void BuildWebGl()
        {
            bool ok = BuildWebGlPlayer(CommandLineOutput() ?? DefaultWebGlOutput());
            EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>
        /// ⚠️ A FOLDER, NOT A FILE, WHICH IS THE THIRD OUTPUT SHAPE IN THIS CLASS. Windows writes
        /// an .exe beside a `_Data` folder, macOS writes a bundle, Android writes a single .apk,
        /// and WebGL writes a DIRECTORY containing `index.html`, `Build/` and `TemplateData/`.
        /// `BuildPipeline.BuildPlayer` takes the directory itself as the "location" for WebGL,
        /// so there is no file name to append and `PurgeOutputDirectory` is handed exactly the
        /// folder it is about to fill.
        ///
        /// ⚠️ IT GOES UNDER `Builds/` RATHER THAN ON THE DESKTOP, unlike Windows and Android.
        /// Those two are opened by hand and belong where 🧑 can double-click them; a WebGL build
        /// is uploaded rather than run, and a folder of ten thousand files on somebody's Desktop
        /// is a nuisance. `DefaultMacOutput` already made this choice for the same reason.
        /// </summary>
        public static string DefaultWebGlOutput()
            => Path.Combine(Directory.GetCurrentDirectory(), "Builds", "WebGL");

        private static bool BuildWebGlPlayer(string outputPath)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                        NamedBuildTarget.WebGL, BuildTarget.WebGL))
                {
                    Debug.LogError("[Build] could not switch to the WebGL target. Is the WebGL " +
                                   "module installed for this editor? Check " +
                                   "Editor/Data/PlaybackEngines for WebGLSupport.");
                    return false;
                }
            }

            ConfigureWebGl();

            return Execute(outputPath, BuildTarget.WebGL);
        }

        /// <summary>
        /// The WebGL player settings, argued rather than defaulted.
        ///
        /// ⚠️⚠️ **BROTLI IS THE DEFAULT AND IT IS THE WRONG ONE FOR itch.io.** Brotli needs the
        /// host to send `Content-Encoding: br`, and itch.io's static hosting does not, so a
        /// Brotli build downloads and then fails to decompress with a console error most players
        /// will read as "the game is broken". **Gzip is served correctly by every static host**
        /// and costs about fifteen per cent more download. `docs/FUTURE.md` Phase 18 names itch.io
        /// as the first destination, so that is the host this is tuned for.
        ///
        /// ⚠️ AND THE DECOMPRESSION FALLBACK IS ON, which is the belt for the same braces: with
        /// it, a host that sends no encoding header at all still works, at the cost of a small
        /// JavaScript decompressor in the loader.
        ///
        /// ⚠️⚠️ **EXCEPTIONS ARE `ExplicitlyThrownExceptionsOnly`, NOT `None`.** `None` is faster
        /// and smaller and it also means a null reference silently corrupts the heap instead of
        /// throwing, which in a browser presents as the game freezing with no message. This
        /// project's whole verification strategy is that a failure says what it was
        /// (`CLAUDE.md` § 7.1), and a build that cannot report its own exceptions cannot be
        /// debugged by anybody who is not sitting at this machine.
        ///
        /// ⚠️ THE MEMORY SIZE IS LEFT AT UNITY 6'S DEFAULT ON PURPOSE. Unity 6 grows the WASM
        /// heap dynamically and the old fixed `memorySize` knob is deprecated; pinning it is how
        /// a build that ran fine on a desktop browser dies on a Chromebook.
        /// </summary>
        private static void ConfigureWebGl()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            // ⚠️ NO DATA CACHING. It stores the whole data file in IndexedDB, which makes a second
            // visit fast and makes the FIRST visit fail on a browser with a small storage quota,
            // silently, after the download has already happened. A shop window is judged on the
            // first visit.
            PlayerSettings.WebGL.dataCaching = false;
        }

        [MenuItem("Tumbang Preso/Build Android Player")]
        public static void BuildAndroidFromMenu() => BuildAndroidPlayer(DefaultAndroidOutput());

        public static void BuildAndroid()
        {
            bool ok = BuildAndroidPlayer(CommandLineOutput() ?? DefaultAndroidOutput());
            EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>
        /// The .apk, beside the Windows player on the same Desktop.
        ///
        /// ⚠️ ONE FILE, NOT A FOLDER, WHICH `PurgeOutputDirectory` HAS TO BE TOLD ABOUT. The
        /// Windows player is a directory it can delete wholesale; an .apk is a single file in a
        /// directory that may hold other things. It gets its OWN directory for exactly that
        /// reason, so the purge rule that protects the Windows build protects this one unchanged.
        /// </summary>
        public static string DefaultAndroidOutput()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            return Path.Combine(desktop, "TumbangPreso-Android", "TumbangPreso.apk");
        }

        private static bool BuildAndroidPlayer(string outputPath)
        {
            if (!ConfigureAndroid()) return false;

            // ⚠️⚠️ THE TARGET IS SWITCHED BEFORE THE BUILD, NOT LEFT TO `BuildPlayer`. Switching
            // is what re-evaluates `UNITY_ANDROID`, and this project branches on it in
            // `Matchmaker.LocalPlatform`, `Matchmaker.LocalInputDevice` and
            // `TouchHud.ShouldShow`. A player built without the switch compiles those three
            // branches for the EDITOR's platform, so the .apk would report itself as a desktop
            // peer and ship with no touch controls, and nothing would error.
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                        NamedBuildTarget.Android, BuildTarget.Android))
                {
                    Debug.LogError("[Build] could not switch to the Android target. Is the " +
                                   "Android module installed for this editor? Check " +
                                   "Editor/Data/PlaybackEngines for AndroidPlayer.");
                    return false;
                }
            }

            return Execute(outputPath, BuildTarget.Android);
        }

        /// <summary>
        /// The Android player settings, all of them argued rather than defaulted.
        ///
        /// ⚠️⚠️ THIS ASKS FOR `ARM64 | X86_64` AND THE .apk COMES OUT ARM64-ONLY. MEASURED, NOT
        /// GUESSED, ON THE FIRST BUILD THIS PROJECT EVER MADE (2026-09-03): unzipping the shipped
        /// file lists **`lib/arm64-v8a` and nothing else**, 7 files and 119.9 MB. Unity 6 does not
        /// emit an x86_64 Android slice, and it does not error about being asked to; the flag is
        /// simply ignored. **A setting the engine declines is not a setting**, which is
        /// `CLAUDE.md` § 6.4's `ConfigureSplash` lesson arriving from the other direction, and
        /// `minSdkVersion` below is the third instance in this one method.
        ///
        /// ⚠️⚠️ AND THE PARAGRAPH THIS REPLACES WAS WRONG IN BOTH DIRECTIONS AT ONCE, WHICH IS WHY
        /// NOBODY CAUGHT IT. It read: *"An ARM64-only .apk installs on neither emulator and can
        /// only be verified by somebody with a handset, and there is not one on this team."*
        /// **The .apk IS ARM64-only and it installed and ran on the emulator anyway.** The Android
        /// 14 x86_64 system image translates arm64, so `adb install` succeeded and the player
        /// logged `Scripting Backend 'il2cpp', CPU 'arm64-v8a'` on a device whose
        /// `ro.product.cpu.abi` is `x86_64`. A false premise and a false conclusion cancelled out
        /// and the file read as correct for a day.
        ///
        /// ⚠️ THE REQUEST IS KEPT RATHER THAN DELETED, and that is a decision. Writing
        /// `ARM64` alone would be tidier and would also delete the record of what was asked for;
        /// if a future editor emits the slice, this picks it up. **What must not survive is the
        /// COMMENT claiming the slice is there.** `docs/TODO.md` § 126.10 has the measurement.
        ///
        /// ⚠️ AND THE TESTABILITY ARGUMENT STILL HOLDS, THROUGH A DIFFERENT MECHANISM. 🧑 has no
        /// handset (2026-09-02: *"i dont have any nadroid at all"*), so the emulator is the only
        /// device this game can be checked on, and it works because of TRANSLATION rather than
        /// because of a second slice. That is slower than native and it is not a performance
        /// measurement (§ 126.10), but it is a real run on a real Android.
        ///
        /// ⚠️ X86_64 FORCES IL2CPP. Mono only emits ARMv7 and x86 on Android, so the scripting
        /// backend is not a free choice here; it follows from wanting to run the thing.
        ///
        /// ⚠️ LANDSCAPE, LOCKED. The arena is 14 m by 14 m read across the screen and the whole
        /// front end is authored against a 1920x1080 canvas matched on HEIGHT; a portrait phone
        /// would crop it to a 9:16 slice, which is `AspectSafeCanvas`'s failure mode rather than
        /// a layout to design. `InputSurfaceProbe` drives the two landscape phone shapes.
        /// </summary>
        private static bool ConfigureAndroid()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,
                                                    "com.bhstudios.tumbangpreso");

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures =
                AndroidArchitecture.ARM64 | AndroidArchitecture.X86_64;

            // ⚠️⚠️ 26, AND IT WAS 24 UNTIL THE FIRST BUILD ACTUALLY RAN. Unity 6 REFUSES the older
            // value: the very first `BuildAndroid` printed
            //
            //     Minimum supported Android API level is 26 (Android 8.0 Oreo).
            //     Please use AndroidApiLevel26 or higher.
            //
            // as a `Debug.LogError` out of this exact line, and then carried on and built a player
            // whose manifest said something this file did not. **A setting the engine declines is
            // not a setting**, which is `CLAUDE.md` § 6.4's `ConfigureSplash` lesson (*"a colour
            // set in `ProjectSettings.asset` is not set"*) arriving from the other direction: the
            // write is here, in code, on every build, and it still did not take.
            //
            // ⚠️ THE ORIGINAL REASONING IS KEPT BECAUSE IT WAS SOUND AND ONLY THE NUMBER WAS
            // WRONG: *"API 24 is Android 7.0: it is what the Input System's touch stack and
            // Netcode's transport both assume, and it is old enough to cover the phones this game
            // is actually aimed at in Metro Manila."* Android 8.0 is 2017 hardware, so the floor
            // moved by one year of phones and the argument survives it. **If this ever needs to go
            // lower, the blocker is the editor and not this file.**
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // ⚠️⚠️ THE INTERNET PERMISSION IS REQUIRED OR CROSSPLAY IS DEAD ON ARRIVAL, AND THE
            // FAILURE IS SILENT. Without it every UGS call and every Netcode connection fails at
            // runtime with a socket error, which reads exactly like "the lobby is empty" rather
            // than like a missing manifest line. `forceInternetPermission` is the one-line answer
            // and it costs a desktop build nothing.
            PlayerSettings.Android.forceInternetPermission = true;

            // A phone throttles hard. 30 is a floor the thermals can hold; the desktop build is
            // untouched, because this is an Android-only player setting.
            PlayerSettings.Android.startInFullscreen = true;

            return true;
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
        /// The engine splash: white, Unity's mark alone, and nothing of ours on it.
        ///
        /// ⚠️⚠️ THIS REVERSES `docs/TODO.md` § 111.1, WHICH PUT THE BH STUDIOS MARK ON THIS
        /// SCREEN, AND THE REVERSAL WAS ASKED FOR BY NAME. 🧑 2026-09-01, over a screenshot of
        /// exactly what that entry built: *"remove bh studios here and use white screen for unity
        /// too"*. **The studio mark is not lost, it moved to its own beat**: the boot sequence is
        /// UNITY (white) then BH STUDIOS (the video in `SplashScreen.unity`, also white) then
        /// LOGIN. § 114.1 and § 114.2.
        ///
        /// ⚠️⚠️ THE SAME MARK ON NAVY AND THEN ON WHITE IS WHY. § 111.1 matched this screen to
        /// "the menu's own navy" so the two would read as one sequence; the sting itself is a
        /// black mark on white, so what actually shipped was the studio's logo twice, in two
        /// background colours, with a hard colour cut between them. White here and white there is
        /// one continuous frame, which is what that entry was trying to buy.
        ///
        /// ⚠️⚠️ AND THIS METHOD IS WHY THE `ProjectSettings.asset` FIELDS ARE NOT ENOUGH ON THEIR
        /// OWN. It runs on EVERY build and writes `backgroundColor`, `logos` and `unityLogoStyle`
        /// over whatever the asset holds, so a change made only in the inspector survives exactly
        /// until the next build and then silently reverts. **Both places or neither.**
        ///
        /// ⚠️ `showUnityLogo` STAYS TRUE AND MUST. Unity's splash is not removable on this
        /// licence, which is the fact `BootSting`'s header records and the reason the sting is
        /// timed to play across it. `DarkOnLight` is what makes the engine mark legible now that
        /// the background is white.
        ///
        /// ⚠️ THE LOGO LOOKUP IS DELETED WITH THE LOGO. It was force-import, `LoadAllAssetsAtPath`
        /// for the sub-asset, a `spriteImportMode` repair and a `Resources` fallback, and every
        /// one of those lines existed to get `bh_studios_logo.png` onto THIS screen. Nothing else
        /// in the project reads a Sprite from that file. **The file itself is kept**: it is still
        /// the source of the in-game sting's own artwork.
        /// </summary>
        private static void ConfigureSplash()
        {
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = true;
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.DarkOnLight;
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.UnityLogoBelow;

            PlayerSettings.SplashScreen.backgroundColor = Color.white;

            // ⚠️ EMPTY, NOT UNSET. An unassigned array leaves whatever the last build wrote.
            PlayerSettings.SplashScreen.logos = new PlayerSettings.SplashScreenLogo[0];
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

                // ⚠️ ITS BLEND-MODE TWIN, ADDED 2026-08-28 WITH THE FUZZY SLIPPER. Reached the
                // same way, through `Shader.Find` in `ToonSkin.TransparentShader`, so it is
                // stripped by the same rule if it is not named here. Its miss path falls back to
                // the opaque `TumbangPreso/Toon`, which means a stripped build does not go
                // magenta: the fur just renders solid again, exactly the bug this shader was
                // written to fix, and only in the player. That is the worst possible failure to
                // debug, which is why this line exists rather than being trusted to the fallback.
                "TumbangPreso/ToonTransparent",

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

                // ⚠️ THE NEAR-CAMERA DISSOLVE, AND ITS MISS PATH SHIPS THE ORIGINAL REPORT BACK.
                // `NearFade.Install` reaches it through `Shader.Find` and nothing in any scene
                // references it, which is exactly the case this list exists for. Stripped, the
                // install warns and leaves every post on its solid material, so the .exe goes back
                // to a utility pole filling half the screen while the editor keeps working. The
                // warning is the editor-side guard; this line is the player-side one.
                "TumbangPreso/NearFade",

                // ⚠️ DANTE'S GROUND, AND THE FIRST SHADER ANY ABILITY HAS EVER HAD. `VfxMaterial
                // .Volcanic` reaches it through `Shader.Find` and nothing in any scene references
                // it, which is exactly the case this list exists for.
                //
                // ⚠️ ITS MISS PATH IS THE GENTLEST ON THIS LIST AND THAT IS THE DANGER. The
                // painter falls back to `Solid` and `Ghost` on a miss, so a stripped build does
                // not ship a magenta stomp: it ships the FLAT one, which is precisely the look
                // 🧑 reported on 2026-08-28 and which this shader was written to answer. The
                // editor would be fixed and the .exe would show the original complaint, and the
                // only thing saying so would be one warning in a log nobody reads during a
                // playtest. That is worse than pink, not better.
                "TumbangPreso/VolcanicRock",
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
            //
            // ⚠️⚠️ AND THE `.apk` ENTRY IS THE ANDROID HALF, WHICH IS THE **THIRD** TIME THIS ONE
            // TEST HAS BEEN TOO NARROW FOR A PLATFORM SOMEBODY ADDED AFTERWARDS. The paragraph
            // above records the macOS instance in its own words; this is the same sentence with a
            // different extension, and it had the same symptom:
            //
            //     [Build] output 'C:\Users\Matthew\Desktop\TumbangPreso-Android' exists but does
            //     not look like a previous player (no UnityPlayer.dll, no TumbangPreso_Data, no
            //     .app). Refusing to delete it.
            //
            // ⚠️⚠️ **SO EVERY ANDROID REBUILD ON THIS MACHINE FAILED THE MOMENT ONE .apk EXISTED**,
            // and it failed AFTER switching build target and running the whole scene check, which
            // is several minutes in. `docs/TODO.md` § 130.17. An Android build writes a single
            // `.apk` (or `.aab`) plus a `*_BurstDebugInformation_DoNotShip` folder and none of the
            // three desktop markers, so the guard was correct about what it saw and wrong about
            // what it meant.
            //
            // ⚠️ THE GUARD'S INTENT IS UNCHANGED AND MUST STAY: `-buildOutput` takes an arbitrary
            // path, so this still refuses a drive root, a directory holding a `.git`, and anything
            // that does not already look like a build. **What widened is the definition of "looks
            // like a build", not the willingness to delete.**
            // ⚠️⚠️ AND ON 2026-09-03 IT WAS REWRITTEN TO NAME THE SHAPE OF THE MISTAKE RATHER
            // THAN A FOURTH PLATFORM, BECAUSE § 130.17 ALREADY SAID WHAT THE SHAPE WAS AND THE
            // NEXT PLATFORM WOULD OTHERWISE HAVE BEEN THE FOURTH TIME. That entry's own words:
            // *"this test enumerates what today's platforms leave behind, so every platform added
            // later fails it once."* WebGL is that platform: it writes `index.html`, `Build/` and
            // `TemplateData/` and **not one of the five markers above**, so the first WebGL
            // rebuild on any machine would have failed exactly as Android's did, several minutes
            // in, after the target switch, with an error about a missing `UnityPlayer.dll`.
            //
            // ⚠️⚠️ `*_BurstDebugInformation_DoNotShip` IS THE GENERAL MARKER AND IT IS WHY THIS
            // SHOULD NOT NEED A FIFTH VISIT. Unity emits that folder beside the player on **every
            // IL2CPP target**, desktop, mobile and console alike, so it identifies a build
            // directory without knowing which platform wrote it. The named markers stay because
            // a Mono build does not emit it and because naming them is what makes the error
            // message useful, but the general one is what catches a platform nobody has added
            // yet.
            //
            // ⚠️ `TumbangPreso_Data` BECAME `*_Data`, WHICH IS THE SAME LESSON ONE SIZE DOWN.
            // `PlayerSettings.productName` is written by `Execute` a few lines below and could be
            // changed for a demo build or a jam entry; the guard would then stop recognising this
            // project's own output. A wildcard costs nothing and removes the coupling.
            //
            // ⚠️ THE GUARD'S INTENT IS STILL UNCHANGED, for the third time: it refuses a drive
            // root, a directory holding a `.git`, and anything that does not already look like a
            // build. **What keeps widening is the definition of "looks like a build", never the
            // willingness to delete.**
            bool looksLikeAPlayer =
                File.Exists(Path.Combine(info.FullName, "UnityPlayer.dll")) ||
                Directory.GetDirectories(info.FullName, "*_Data").Length > 0 ||
                Directory.GetDirectories(info.FullName, "*.app").Length > 0 ||
                Directory.GetFiles(info.FullName, "*.apk").Length > 0 ||
                Directory.GetFiles(info.FullName, "*.aab").Length > 0 ||
                Directory.GetDirectories(info.FullName,
                                         "*_BurstDebugInformation_DoNotShip").Length > 0 ||
                (File.Exists(Path.Combine(info.FullName, "index.html")) &&
                 Directory.Exists(Path.Combine(info.FullName, "Build"))) ||
                info.GetFileSystemInfos().Length == 0;

            if (!looksLikeAPlayer)
            {
                Debug.LogError($"[Build] output '{dir}' exists but does not look like a previous " +
                               "player: no UnityPlayer.dll, no *_Data folder, no .app, no .apk " +
                               "or .aab, no Burst debug folder, and no index.html beside a " +
                               "Build/ folder. Refusing to delete it. Move it aside or point " +
                               "-buildOutput somewhere else.");
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

            // ⚠️⚠️ WEBGL'S "LOCATION" IS THE DIRECTORY ITSELF, NOT A FILE INSIDE ONE, AND TAKING
            // `GetDirectoryName` OF IT WOULD HAVE AIMED THE PURGE ONE LEVEL TOO HIGH.
            // `DefaultWebGlOutput` is `Builds/WebGL`, so the old line would have handed
            // `PurgeOutputDirectory` the whole `Builds/` folder, **which also holds the macOS
            // build**. It fails safe rather than dangerously (a folder holding only other build
            // folders matches none of the markers, so the guard refuses and the build stops), but
            // "safe" here means every WebGL build fails several minutes in, after the target
            // switch, which is § 130.17's symptom exactly and is the fourth time this class of
            // path assumption would have bitten.
            //
            // ⚠️ THE TARGET DECIDES, NOT THE STRING. Sniffing for a trailing extension would be a
            // guess about a path a caller may write however they like (`-buildOutput` takes an
            // arbitrary one); the build target is the fact that actually determines the shape.
            string dir = target == BuildTarget.WebGL
                ? outputPath
                : Path.GetDirectoryName(outputPath);

            if (!PurgeOutputDirectory(dir)) return false;
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            PlayerSettings.companyName = "BH Studios";
            PlayerSettings.productName = "Tumbang Preso";

            ConfigureSplash();
            ConfigureIcon();
            EnsureRuntimeShaders();

            // ⚠️⚠️ REGENERATED ON EVERY BUILD, FOR `ConfigureSplash`'s OWN REASON ONE LINE UP.
            // The loading screen warms shaders a slice per frame out of this collection
            // (`docs/TODO.md` § 126.10, the Android ANR), and a collection generated by hand goes
            // stale the first time somebody adds a material. A stale one warms the wrong shaders
            // and looks exactly like a working one, which is the failure `CLAUDE.md` § 6.4's
            // splash paragraph is about: **both places or neither.**
            ShaderWarmupCollection.Rebuild(true);

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
