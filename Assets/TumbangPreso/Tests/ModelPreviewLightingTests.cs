using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TumbangPreso.UI;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The character screen is lit in the same units as the maps.
    ///
    /// ⚠️⚠️ THIS IS `docs/TODO.md` § 79.1 PINNED SO IT CANNOT COME BACK. Godot `light_energy` and
    /// Unity `intensity` are not the same unit. `TscnImporter` has always known that and scales
    /// every imported map light by `KeyEnergyToIntensity = 0.651f`, calibrated against a captured
    /// Godot frame. `ModelPreview` transcribed `CharacterSelect.tscn`'s energies of 1.35 and 0.45
    /// and applied nothing, so the character portrait was lit 1.54x hotter than every map in the
    /// game and the cast read pale and washed out there while looking rich in the lobby.
    ///
    /// 🧑, across two sittings: *"fix shader on chara select too look at pic 1 vs pic 2, it should
    /// look more like pic 2"*, then *"yea cheska still pale af"*, *"compared to the other 2 maps
    /// that have better lighting for characters"*.
    ///
    /// ⚠️ THE TWO COPIES CANNOT BE ONE CONSTANT, WHICH IS WHY THIS IS A TEST AND NOT A REFERENCE.
    /// `TscnImporter` is in the Editor assembly and `ModelPreview` is in Runtime, and Runtime
    /// referencing Editor is the one dependency that may never exist. So the number is written
    /// twice and this asserts they agree, by reading the importer's source as text. That is the
    /// same trade `SceneScriptCheck` and the three `tools/audit_*.py` scripts make.
    ///
    /// ⚠️ IT MATTERS BECAUSE THE IMPORTER'S COPY IS EXPECTED TO MOVE. Its own note says
    /// `ToneSweep` plus `tools/read_sweep.py` will re-derive 0.651 if the render path ever
    /// changes. A re-derivation that moved the map copy and left this one behind would silently
    /// put the character screen back where it started, and the fault took three sessions to find
    /// the first time.
    /// </summary>
    public class ModelPreviewLightingTests
    {
        private const string ImporterPath =
            "Assets/TumbangPreso/Editor/MapKit/TscnImporter.cs";

        [Test]
        public void ThePreviewConvertsGodotEnergyWithTheSameFactorTheMapImporterUses()
        {
            Assert.IsTrue(File.Exists(ImporterPath), ImporterPath + " is missing");

            string source = File.ReadAllText(ImporterPath);

            var match = Regex.Match(
                source, @"KeyEnergyToIntensity\s*=\s*([0-9]*\.?[0-9]+)f");

            Assert.IsTrue(match.Success,
                "TscnImporter no longer declares KeyEnergyToIntensity, so the character screen "
                + "has nothing to agree with. If the conversion moved, move "
                + "ModelPreview.LightExposure with it and update this test.");

            float importer = float.Parse(match.Groups[1].Value,
                                         System.Globalization.CultureInfo.InvariantCulture);

            Assert.AreEqual(importer, ModelPreview.LightExposure, 0.0005f,
                $"the map importer converts Godot light_energy to Unity intensity by {importer} "
                + $"and the character screen uses {ModelPreview.LightExposure}. While these "
                + "disagree the picker is lit "
                + $"{importer / ModelPreview.LightExposure:F2}x differently from every map, "
                + "which is docs/TODO.md § 79.1: the cast reads pale on the character screen and "
                + "correct in the lobby.");
        }

        /// <summary>
        /// ⚠️ A GUARD ON THE DIRECTION, so a later pass cannot quietly restore 1.0 and call it a
        /// transcription of the .tscn. It IS a faithful transcription of the .tscn; the .tscn is
        /// in Godot's units, which is the whole point.
        /// </summary>
        [Test]
        public void TheConversionIsActuallyApplied()
        {
            Assert.Less(ModelPreview.LightExposure, 0.999f,
                "ModelPreview.LightExposure is back at 1.0, which means the character screen is "
                + "applying Godot light energies to Unity lights unconverted. That is the § 79.1 "
                + "fault exactly.");

            Assert.Greater(ModelPreview.LightExposure, 0.3f,
                "ModelPreview.LightExposure is far below the map conversion, which would leave "
                + "the picker darker than the game rather than brighter.");
        }
    }
}
