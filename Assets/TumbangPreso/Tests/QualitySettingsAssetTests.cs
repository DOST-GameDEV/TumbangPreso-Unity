using NUnit.Framework;
using TumbangPreso.Settings;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The six stored quality levels still say what <see cref="AntiAliasModes"/> says they say.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THE TABLE WAS PROSE AND PROSE DRIFTED. `docs/TODO.md` § 125.14:
    /// `ProjectSettings/QualitySettings.asset` had Ultra on `antiAliasing: 0` while
    /// `AntiAliasModes`'s header said 4, and the disagreement sat in the working copy as an
    /// uncommitted diff that three sessions read as somebody's stray edit before anybody read
    /// the number. **Nothing in the suite looked at these six values at all.**
    ///
    /// ⚠️ `AntiAliasStateProbe` IS NOT THIS AND MUST NOT BE CHANGED INTO IT. That one measures
    /// the sample count on the live render TARGET, deliberately, because the driver may decline a
    /// request and the effect is what matters. This asserts the stored INTENT. Two different
    /// questions, and the gap between them is where the drift lived.
    ///
    /// ⚠️⚠️ IT READS THE ASSET RATHER THAN CALLING `QualitySettings.SetQualityLevel`, AND THAT IS
    /// THE WHOLE POINT OF IT BEING CHEAP. Walking the levels to read them would SELECT each one,
    /// which is a write, on the exact field this test exists to protect. A `SerializedObject`
    /// over the settings asset answers the question without touching anything, needs no play
    /// session, and runs in EditMode in milliseconds, which is the bound `docs/TODO.md` § 124.11
    /// says belongs in the forty-millisecond test rather than in a twelve-minute one.
    /// </summary>
    public sealed class QualitySettingsAssetTests
    {
        private const string AssetPath = "ProjectSettings/QualitySettings.asset";

        /// <summary>
        /// ⚠️ THE NAMES ARE ASSERTED TOO, BECAUSE AN ARRAY OF SIX INTEGERS MEANS NOTHING WITHOUT
        /// THEM. If somebody inserts a seventh level, or reorders them, the sample counts would
        /// still "match" while every one of them had moved to a different row. Anchoring the
        /// order is what makes <see cref="AntiAliasModes.QualityLevelSamples"/> readable as a
        /// table rather than as six loose numbers.
        /// </summary>
        private static readonly string[] ExpectedNames =
        {
            "Very Low", "Low", "Medium", "High", "Very High", "Ultra",
        };

        [Test]
        public void TheSixQualityLevelsAreInTheOrderTheTableAssumes()
        {
            Assert.AreEqual(ExpectedNames, QualitySettings.names,
                            "the quality levels have been renamed or reordered, so " +
                            "AntiAliasModes.QualityLevelSamples no longer names the rows it " +
                            "thinks it does. Fix the table and this list together.");

            Assert.AreEqual(ExpectedNames.Length, AntiAliasModes.QualityLevelSamples.Length,
                            "AntiAliasModes.QualityLevelSamples must carry one entry per quality " +
                            "level.");
        }

        [Test]
        public void EveryStoredAntiAliasLevelMatchesTheDocumentedTable()
        {
            var serialized = LoadSettings();
            var levels = serialized.FindProperty("m_QualitySettings");

            Assert.IsNotNull(levels,
                             $"{AssetPath} has no 'm_QualitySettings' array. Unity has changed " +
                             "the shape of this asset and this test needs rewriting rather than " +
                             "deleting: the drift it guards is real (docs/TODO.md 125.14).");

            Assert.AreEqual(AntiAliasModes.QualityLevelSamples.Length, levels.arraySize,
                            "the asset holds a different number of quality levels than " +
                            "AntiAliasModes.QualityLevelSamples describes.");

            for (int i = 0; i < levels.arraySize; i++)
            {
                var stored = levels.GetArrayElementAtIndex(i).FindPropertyRelative("antiAliasing");

                Assert.IsNotNull(stored, $"level {i} has no 'antiAliasing' field.");

                Assert.AreEqual(
                    AntiAliasModes.QualityLevelSamples[i], stored.intValue,
                    $"quality level {i} ('{ExpectedNames[i]}') stores antiAliasing " +
                    $"{stored.intValue}, and AntiAliasModes.QualityLevelSamples says " +
                    $"{AntiAliasModes.QualityLevelSamples[i]}. Read that field's note before " +
                    "changing either: in the editor, writing QualitySettings.antiAliasing during " +
                    "PLAY writes through to this asset, so a level whose stored count differs " +
                    "from what the boot mode applies is re-dirtied by every play session. The " +
                    "level at risk is whichever m_PerPlatformDefaultQuality selects for the " +
                    "editor's CURRENT build target: Standalone is 5 (Ultra), Android is 2 " +
                    "(Medium).");
            }
        }

        /// <summary>
        /// ⚠️ THE MODE LIST'S OWN SAMPLE COUNTS ARE THE OTHER HALF, and they are asserted here
        /// rather than in a second file because they are the same claim from the player's side:
        /// `QualitySettings.antiAliasing` rejects anything that is not 0, 2, 4 or 8, and a row
        /// added to <see cref="AntiAliasModes.All"/> with, say, 3 in it would be accepted by the
        /// settings panel, stored in the player's file, and silently refused by the engine.
        /// </summary>
        [Test]
        public void EveryOfferedModeAsksForASampleCountTheEngineAccepts()
        {
            foreach (var entry in AntiAliasModes.All)
            {
                Assert.Contains(entry.Samples, new[] { 0, 2, 4, 8 },
                                $"'{entry.Label}' asks for {entry.Samples} samples, which " +
                                "QualitySettings.antiAliasing does not accept.");
            }

            Assert.That(AntiAliasModes.Default, Is.InRange(0, AntiAliasModes.All.Length - 1),
                        "AntiAliasModes.Default is not a row in AntiAliasModes.All.");
        }

        private static SerializedObject LoadSettings()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(AssetPath);

            Assert.IsNotNull(assets, $"could not open {AssetPath}.");
            Assert.IsNotEmpty(assets, $"{AssetPath} loaded no objects.");

            return new SerializedObject(assets[0]);
        }
    }
}
