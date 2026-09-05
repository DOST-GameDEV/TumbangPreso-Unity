using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TumbangPreso.Settings;
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
    /// which is a write, on the exact field this test exists to protect. Reading the settings
    /// answers the question without touching anything, needs no play session, and runs in EditMode
    /// in milliseconds, which is the bound `docs/TODO.md` § 124.11 says belongs in the
    /// forty-millisecond test rather than in a twelve-minute one.
    ///
    /// ⚠️⚠️ AND IT READS THE FILE ON DISK RATHER THAN THE LOADED OBJECT, WHICH IS A CORRECTION
    /// AND NOT A REFACTOR. `AssetDatabase.LoadAllAssetsAtPath` answers the LIVE settings
    /// singleton, and `GameSettings.Apply` writes the player's own anti-alias choice into it at
    /// boot, in batch mode as well. So in any session where the game has booted, a
    /// `SerializedObject` over that object reports **what the last player setting applied**, not
    /// what the project stores: this test read 0 on a checkout whose file says 4, and called it
    /// drift. `docs/TODO.md` § 149.11. Reading the YAML is the only way to measure the claim in
    /// the summary above, which is about the **stored** intent.
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
            var stored = StoredAntiAliasLevels();

            Assert.AreEqual(AntiAliasModes.QualityLevelSamples.Length, stored.Count,
                            $"{AssetPath} holds {stored.Count} antiAliasing rows and " +
                            "AntiAliasModes.QualityLevelSamples describes " +
                            $"{AntiAliasModes.QualityLevelSamples.Length}. Unity has changed the " +
                            "shape of this asset and this test needs rewriting rather than " +
                            "deleting: the drift it guards is real (docs/TODO.md 125.14).");

            for (int i = 0; i < stored.Count; i++)
            {
                Assert.AreEqual(
                    AntiAliasModes.QualityLevelSamples[i], stored[i],
                    $"quality level {i} ('{ExpectedNames[i]}') stores antiAliasing " +
                    $"{stored[i]} ON DISK, and AntiAliasModes.QualityLevelSamples says " +
                    $"{AntiAliasModes.QualityLevelSamples[i]}. The table is the source and the " +
                    "asset is generated from it by QualityLevelStamp, so this failing means the " +
                    "stamp has not run since somebody hand-edited the file. Run " +
                    "Tumbang Preso/Checks/Restore stored quality levels, or edit the TABLE if " +
                    "the intent is what changed. docs/TODO.md 125.14 and 149.11.");
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

        /// <summary>
        /// The `antiAliasing` value stored for each quality level, read out of the YAML.
        ///
        /// ⚠️ ONE VALUE PER LEVEL, IN FILE ORDER, AND THE COUNT IS ASSERTED BY THE CALLER. The
        /// asset writes the six levels in the order `QualitySettings.names` reports them, and the
        /// key appears exactly once inside each, so a regex over the file is a complete reading
        /// rather than a sample. If Unity ever changes the shape of this file the count stops
        /// matching and the caller says so, which is the loud failure rather than the quiet one.
        /// </summary>
        private static List<int> StoredAntiAliasLevels()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            string path = Path.Combine(root, AssetPath.Replace('/', Path.DirectorySeparatorChar));

            Assert.IsTrue(File.Exists(path), $"could not find {AssetPath} at {path}.");

            var found = new List<int>();
            foreach (Match m in Regex.Matches(File.ReadAllText(path),
                                              @"^\s*antiAliasing:\s*(?<n>\d+)\s*$",
                                              RegexOptions.Multiline))
            {
                found.Add(int.Parse(m.Groups["n"].Value));
            }

            return found;
        }
    }
}
