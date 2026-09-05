using TumbangPreso.Settings;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Puts <c>ProjectSettings/QualitySettings.asset</c>'s six stored anti-alias counts back to
    /// what <see cref="AntiAliasModes.QualityLevelSamples"/> says they are.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE EVERY UNITY RUN LEFT THE WORKING TREE DIRTY, AND THAT NOW BREAKS
    /// THE GATE. `tools/qualify.py` refuses to certify a tree it cannot tie to a commit
    /// (`docs/TODO.md` § 145.1), so a run of the qualification that dirties a tracked file **is a
    /// qualification that can never pass**: the EditMode stage rewrites the asset, the report
    /// stage then reads a dirty tree, and the verdict is NOT QUALIFIED with every test green.
    /// Measured 2026-09-05 on `837eb0a`: a plain `-batchmode -runTests -testPlatform EditMode`
    /// launch moved Ultra's `antiAliasing` from 4 to 0, twice in a row, on a clean checkout.
    ///
    /// ⚠️⚠️ AND THE NOTE IN `AntiAliasModes` SAYING A BATCH RUN LEAVES IT CLEAN IS NOW FALSIFIED.
    /// It reads *"MEASURED RATHER THAN ASSUMED, 2026-09-03: a full batchmode PlayMode suite ...
    /// left `ProjectSettings/QualitySettings.asset` completely clean. The write-through the
    /// header warns about is an INTERACTIVE editor behaviour"*. It is not. `GameSettings.Apply`
    /// calls `AntiAliasModes.Apply` at boot, in batch mode as well, and in the EDITOR
    /// `QualitySettings.antiAliasing` is the serialized asset: writing the live value writes the
    /// file. The level it lands on is whichever `m_PerPlatformDefaultQuality` selects for the
    /// current build target, which is **5 (Ultra) for Standalone and 2 (Medium) for Android**.
    ///
    /// ⚠️⚠️ THE TABLE IS THE SOURCE AND THE ASSET IS GENERATED FROM IT, WHICH IS THE SAME SHAPE
    /// `GameBuilder.ConfigureSplash` AND `ShaderWarmupCollection` ALREADY HAVE. `CLAUDE.md` § 6.4:
    /// **both places or neither.** A number set in `ProjectSettings.asset` survives until the next
    /// thing that writes it and then silently reverts, so the answer is to write it from one place
    /// on a schedule rather than to ask everybody to remember. To change what a quality level
    /// stores, edit `AntiAliasModes.QualityLevelSamples`; this brings the asset to it.
    ///
    /// ⚠️ IT WRITES ONLY THE `antiAliasing` FIELD AND ONLY WHEN IT DIFFERS. Nothing else in that
    /// asset is this class's business, and a save on every editor launch would be its own kind of
    /// churn. `QualitySettingsAssetTests` still asserts the six values, so a hand edit that this
    /// has not yet corrected is still caught.
    ///
    /// ⚠️ AND IT RUNS AT LOAD **AND** AT QUIT. Load is what makes a fresh batch launch measure a
    /// corrected file; quit is what stops the run that just happened from leaving one behind.
    /// `EditorApplication.quitting` fires in `-batchmode`, which is the case that matters.
    /// </summary>
    [InitializeOnLoad]
    public static class QualityLevelStamp
    {
        private const string AssetPath = "ProjectSettings/QualitySettings.asset";

        static QualityLevelStamp()
        {
            // ⚠️⚠️ THIS IS THE ONE THAT ACTUALLY WORKS, AND THE OTHER THREE ARE BELT AND BRACES.
            // Restoring the file at QUIT was tried first and measured failing: the hook fires (its
            // `[Quality] level 5 ... Restoring (editor quit)` line is in the log) and Unity flushes
            // the project settings from its own in-memory copy AFTERWARDS, so the good value is
            // written and then overwritten in the same shutdown. **Stopping the bad value from
            // ever being marked for saving beats racing to save the good one.**
            //
            // ⚠️ THE LIVE VALUE IS UNTOUCHED. `ClearDirty` says "this object does not need
            // writing to disk"; the sample count `AntiAliasModes.Apply` just set is still what the
            // frame renders with, so `AntiAliasStateProbe` and the post-AA pass see exactly what
            // they saw before. `docs/TODO.md` § 149.11.
            Settings.AntiAliasModes.AppliedInEditor += ForgetTheWrite;

            // ⚠️ DEFERRED BY ONE EDITOR TICK. A static constructor runs during domain reload,
            // where `AssetDatabase` is not reliably ready; `delayCall` is the documented seam and
            // it fires in batch mode as well as interactively.
            EditorApplication.delayCall += () => Restore("editor load");

            EditorApplication.quitting += () => Restore("editor quit");
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) Restore("leaving play mode");
            };
        }

        /// <summary>
        /// Mark the quality settings asset as not needing a save, right after the game wrote a
        /// player's own anti-alias choice into it.
        ///
        /// ⚠️ IT IS NARROW ON PURPOSE: it runs only in the same call as our own write, so an edit
        /// somebody makes in the Quality window afterwards re-dirties the object normally and is
        /// saved as they expect.
        /// </summary>
        private static void ForgetTheWrite()
        {
            try
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(AssetPath);
                if (assets == null || assets.Length == 0) return;

                EditorUtility.ClearDirty(assets[0]);
            }
            catch
            {
                // ⚠️ SILENT, AND ONLY HERE. This runs on every settings apply, including several
                // times during a boot; a warning per call would bury the log this exists to keep
                // readable, and `Restore` above still corrects the file if one gets through.
            }
        }

        [MenuItem("Tumbang Preso/Checks/Restore stored quality levels")]
        public static void RestoreFromMenu() => Restore("menu");

        /// <summary>
        /// Bring the stored counts back to the table. Returns how many rows it corrected.
        ///
        /// ⚠️ IT READS AND WRITES THROUGH `SerializedObject` RATHER THAN WALKING THE LEVELS.
        /// `QualitySettings.SetQualityLevel` SELECTS a level, which is itself a write to the
        /// field this exists to protect, and `QualitySettingsAssetTests` already records that as
        /// the reason it reads the asset instead.
        /// </summary>
        public static int Restore(string why)
        {
            try
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(AssetPath);
                if (assets == null || assets.Length == 0) return 0;

                var serialized = new SerializedObject(assets[0]);
                var levels = serialized.FindProperty("m_QualitySettings");
                if (levels == null) return 0;

                var table = AntiAliasModes.QualityLevelSamples;
                if (levels.arraySize != table.Length) return 0;

                int corrected = 0;
                for (int i = 0; i < levels.arraySize; i++)
                {
                    var stored = levels.GetArrayElementAtIndex(i)
                                       .FindPropertyRelative("antiAliasing");
                    if (stored == null || stored.intValue == table[i]) continue;

                    Debug.Log($"[Quality] level {i} stored antiAliasing {stored.intValue}; the " +
                              $"table says {table[i]}. Restoring ({why}).");
                    stored.intValue = table[i];
                    corrected++;
                }

                if (corrected == 0) return 0;

                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                return corrected;
            }
            catch (System.Exception e)
            {
                // ⚠️ NOT FATAL, for `StampBuildIdentity`'s reason. A tidy-up that cannot run
                // costs a dirty file; one that throws during a domain reload costs the editor.
                Debug.LogWarning($"[Quality] could not restore the stored quality levels: {e.Message}");
                return 0;
            }
        }
    }
}
