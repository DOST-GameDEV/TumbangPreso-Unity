using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Builds the <see cref="ShaderVariantCollection"/> the loading screen warms a slice at a
    /// time, from every shader this project actually reaches.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE `Shader.WarmupAllShaders()` ANR'D A PHONE AT BOOT, TWICE.
    /// `docs/TODO.md` § 126.10: the .apk never got past its own "preparing shaders" bar in two
    /// separate launches, several minutes each, and Android raised its "isn't responding" dialog
    /// over the loading screen both times. That call compiles every variant in the build in ONE
    /// blocking call, and it was the single stage in `SplashScreen.PreloadGameAssets` that could
    /// not yield, in a routine whose own header says *"IT YIELDS BETWEEN EVERY STAGE,
    /// DELIBERATELY"*. **An ANR at boot is the worst place to have one, because Android offers
    /// the player a button that closes the game.**
    ///
    /// ⚠️⚠️ A COLLECTION IS THE ONLY INCREMENTAL WARM-UP UNITY EXPOSES. `Shader.WarmupAllShaders`
    /// and `ShaderVariantCollection.WarmUp` are both all-or-nothing;
    /// `ShaderVariantCollection.WarmUpProgressively(n)` is the one API that hands the main thread
    /// back between slices, and it needs a collection asset to work from. So the asset is not a
    /// convenience, it is the mechanism.
    ///
    /// ⚠️⚠️ AND THE SOURCE OF TRUTH IS THE `Shader.Find` LITERAL IN THE RUNTIME SOURCE, WHICH THE
    /// FIRST VERSION OF THIS FILE GOT WRONG IN A WAY WORTH RECORDING. It walked MATERIALS only,
    /// which is the obvious way to enumerate shaders, and it found **five**. This project has
    /// **eight** `.mat` assets in `Assets/` and builds essentially every material it draws with
    /// **in code** (`GodotTheme.Box`, `PaperCraft`, `WoodCraft`, every VFX builder), and the
    /// runtime names **nineteen** shaders by string. So the material walk measured the handful of
    /// authored materials and missed the whole game.
    ///
    /// ⚠️⚠️ **A WARM-UP THAT COVERS LESS THAN THE CALL IT REPLACED IS A REGRESSION WEARING THE
    /// FIX'S CLOTHES, AND IT WOULD HAVE BEEN INVISIBLE.** The loading bar still moves, the ANR is
    /// still gone, and the hitch simply comes back later, during a round, which is precisely the
    /// fault `PreloadGameAssets`'s header was written about: *"the work did not disappear, it just
    /// happened at the worst possible moment."* **The shader count is printed at the end for this
    /// reason** — 5 and 19 are very different answers and only one of them was ever on screen.
    ///
    /// ⚠️ READING THE SOURCE AS TEXT IS `SceneScriptCheck` AND `InputSurfaceCheck`'S METHOD, for
    /// their reason: a shader nothing instantiated during a scan is still a shader the player
    /// meets, so anything that only sees what was LOADED sees a subset of the game shaped like
    /// somebody's route through it.
    ///
    /// ⚠️ THE PASS TYPES ARE TRIED IN ORDER AND A REFUSAL IS NORMAL, NOT AN ERROR.
    /// `ShaderVariant`'s constructor throws when a shader has no such pass, and a URP lit shader
    /// genuinely has no <see cref="PassType.Normal"/> pass while a legacy UI shader genuinely has
    /// no <see cref="PassType.ScriptableRenderPipeline"/> one. Catching per attempt is how the two
    /// families are told apart without hard-coding which is which.
    /// </summary>
    public static class ShaderWarmupCollection
    {
        /// <summary>⚠️ IN `Resources` BECAUSE THE PLAYER LOADS IT BY NAME AT BOOT, before any
        /// scene that could hold a reference to it has come up.</summary>
        public const string AssetPath =
            "Assets/TumbangPreso/Resources/ShaderWarmup.shadervariants";

        /// <summary>The name <see cref="Resources.Load"/> is given at runtime.</summary>
        public const string ResourceName = "ShaderWarmup";

        /// <summary>
        /// ⚠️ EVERY PASS TYPE A MATERIAL IN THIS PROJECT CAN CARRY, MOST LIKELY FIRST.
        /// `ScriptableRenderPipeline` is URP's, `Normal` is the built-in and UI one, and the
        /// other three are the passes that compile separately and therefore hitch separately.
        /// </summary>
        private static readonly PassType[] PassTypes =
        {
            PassType.ScriptableRenderPipeline,
            PassType.Normal,
            PassType.ShadowCaster,
            PassType.Meta,
            PassType.ForwardBase,
        };

        /// <summary>Where the runtime's `Shader.Find` calls live.</summary>
        private static readonly string[] RuntimeSourceRoots =
        {
            "Assets/TumbangPreso/Runtime",
        };

        private static readonly Regex ShaderFindLiteral =
            new Regex("Shader\\s*\\.\\s*Find\\s*\\(\\s*\"([^\"]+)\"", RegexOptions.Compiled);

        [MenuItem("Tumbang Preso/Rebuild Shader Warmup Collection")]
        public static void RebuildFromMenu() => Rebuild(true);

        /// <summary>Batch entry point, for a build script or the command line.</summary>
        public static void RebuildForBuild() => Rebuild(true);

        /// <summary>Every shader name the runtime asks for by string, read out of the source.</summary>
        public static HashSet<string> ShaderNamesInSource()
        {
            var names = new HashSet<string>(StringComparer.Ordinal);

            foreach (string root in RuntimeSourceRoots)
            {
                if (!Directory.Exists(root)) continue;

                foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                {
                    string text;
                    try { text = File.ReadAllText(file); }
                    catch (IOException) { continue; }

                    foreach (Match m in ShaderFindLiteral.Matches(text))
                        if (m.Groups.Count > 1) names.Add(m.Groups[1].Value);
                }
            }

            return names;
        }

        /// <summary>
        /// Rewrites <see cref="AssetPath"/> from every shader this project reaches.
        ///
        /// Returns the number of variants written, which is what <see cref="Execute"/> asserts is
        /// not zero. ⚠️ A collection that exists and is empty warms nothing and reads exactly like
        /// one that works, which is the failure mode worth having a number for.
        /// </summary>
        public static int Rebuild(bool log)
        {
            var collection = new ShaderVariantCollection();

            var shaders = new HashSet<Shader>();
            var keywordsByShader = new Dictionary<Shader, HashSet<string>>();

            void Want(Shader shader)
            {
                if (shader == null) return;

                shaders.Add(shader);
                if (!keywordsByShader.TryGetValue(shader, out var set))
                {
                    set = new HashSet<string>(StringComparer.Ordinal);
                    keywordsByShader[shader] = set;
                }

                // The keyword-free variant is the one the base material draws with, and it is
                // the one that costs the boot hitch.
                set.Add("");
            }

            // ---- SOURCE 1: every shader the runtime asks for BY NAME. ---------------------
            // ⚠️ THE LARGEST SOURCE BY FAR, AND THE ONE THE FIRST VERSION HAD NONE OF.
            int unresolved = 0;
            foreach (string name in ShaderNamesInSource())
            {
                var found = Shader.Find(name);

                // ⚠️ A NAME THAT DOES NOT RESOLVE IS REPORTED, NOT SKIPPED SILENTLY. `Shader.Find`
                // answering null at BUILD time means the runtime call answers null too, and that
                // is a pink-material bug in the shipped player rather than a warm-up problem.
                // Finding it here is free, and this is the only place in the project that looks.
                if (found == null)
                {
                    unresolved++;
                    Debug.LogWarning($"[ShaderWarmup] the runtime calls Shader.Find(\"{name}\") " +
                                     "and it does not resolve in the editor, so it will not " +
                                     "resolve in the player either.");
                    continue;
                }

                Want(found);
            }

            // ---- SOURCE 2: every shader AUTHORED in this project. -------------------------
            // ⚠️ A `.shader` IN `Assets/` IS IN THE BUILD WHETHER OR NOT THE REGEX ABOVE CAN SEE
            // WHO NAMES IT, so this catches one wired up through a material set in the inspector
            // or a `Resources.Load`.
            foreach (string guid in AssetDatabase.FindAssets("t:Shader"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)) continue;

                Want(AssetDatabase.LoadAssetAtPath<Shader>(path));
            }

            // ---- SOURCE 3: the authored MATERIALS, for their KEYWORDS. --------------------
            // ⚠️ THIS IS THE ORIGINAL WALK, KEPT FOR THE HALF IT IS ACTUALLY GOOD AT. A shader
            // found by name contributes its keyword-free variant; a MATERIAL carries a real
            // keyword set somebody configured (`_NORMALMAP _METALLICGLOSSMAP`), and each of those
            // compiles separately. **The names give breadth, the materials give depth, and
            // neither is the other.**
            //
            // ⚠️ PACKAGES ARE SKIPPED AND `Assets/` IS NOT. A package material belongs to a tool
            // (the test framework's, the input system's) and is not in the player, so warming it
            // spends boot time on something no frame will ever draw.
            foreach (string guid in AssetDatabase.FindAssets("t:Material"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)) continue;

                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || material.shader == null) continue;

                Want(material.shader);

                string[] keywords = material.shaderKeywords;
                if (keywords != null && keywords.Length > 0)
                {
                    keywordsByShader[material.shader]
                        .Add(string.Join(" ", keywords.OrderBy(k => k, StringComparer.Ordinal)));
                }
            }

            int added = 0;
            int refused = 0;

            foreach (Shader shader in shaders.OrderBy(s => s.name, StringComparer.Ordinal))
            {
                foreach (string line in keywordsByShader[shader].OrderBy(k => k, StringComparer.Ordinal))
                {
                    string[] keywords = string.IsNullOrEmpty(line)
                        ? Array.Empty<string>()
                        : line.Split(' ');

                    foreach (PassType pass in PassTypes)
                    {
                        try
                        {
                            var variant = new ShaderVariantCollection.ShaderVariant(shader, pass, keywords);
                            if (collection.Add(variant)) added++;
                        }
                        catch (ArgumentException)
                        {
                            // This shader has no such pass. Normal: see the class header.
                            refused++;
                        }
                    }
                }
            }

            string directory = Path.GetDirectoryName(AssetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            AssetDatabase.CreateAsset(collection, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (log)
            {
                // ⚠️ THE SHADER COUNT IS THE NUMBER THAT MATTERS, NOT THE VARIANT COUNT. The
                // first version of this file wrote 20 variants over 5 shaders and looked healthy;
                // the fault was breadth, and breadth is the first number here for that reason.
                Debug.Log($"[ShaderWarmup] {collection.shaderCount} shaders, {added} variants " +
                          $"written to {AssetPath}. {refused} pass and shader pairs do not exist " +
                          $"(expected); {unresolved} Shader.Find name(s) did not resolve. " +
                          "Warmed a slice per frame by SplashScreen.PreloadGameAssets.");
            }

            return added;
        }

        /// <summary>
        /// Build gate: the collection must exist and must not be empty.
        ///
        /// ⚠️⚠️ IT REBUILDS RATHER THAN MERELY CHECKING, WHICH IS `CLAUDE.md` § 4a's
        /// "construction, not discipline" applied here. A collection that is checked but not
        /// regenerated goes stale the first time somebody adds a material, and a stale one warms
        /// the wrong shaders while looking exactly like a working one. The only version of this
        /// that cannot rot is the one rewritten on every build.
        /// </summary>
        public static bool Execute()
        {
            try
            {
                int added = Rebuild(true);
                if (added > 0) return true;

                Debug.LogError("[ShaderWarmup] the collection came out EMPTY, so the loading " +
                               "screen would warm nothing and every shader would compile at " +
                               "first use. docs/TODO.md " + "§ 126.10.");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ShaderWarmup] could not build the collection: {e}");
                return false;
            }
        }
    }
}
