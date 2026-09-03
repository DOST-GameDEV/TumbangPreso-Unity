using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// Every control the game teaches has a picture, and every picture is on its sheet.
    ///
    /// ⚠️⚠️ THIS IS `CLAUDE.md` § 4a's ARGUMENT APPLIED TO ART RATHER THAN TO CODE. That section
    /// is about making forgetting impossible by construction: *"a new `Verb` does not compile
    /// until it has a pad binding and a thumb target"*, because **a lookup table keyed by id is a
    /// second place to forget, and forgetting it compiles.** `InputGlyphs` is exactly such a
    /// table, and nothing in the compiler can see that a binding added tomorrow has no cell in
    /// it: the prompt would silently fall back to text and nobody would notice for a month.
    ///
    /// **So the test walks the SHIPPED INPUT ASSET rather than a list**, on both devices, and
    /// asks the same question `Hud.KeyLabel` asks. A binding added without a glyph fails here.
    ///
    /// ⚠️ IT IS EDITMODE, DELIBERATELY. Nothing here needs a scene, a frame or a camera, and
    /// `docs/TODO.md` § 126.8 is a long entry about what running things in PlayMode that do not
    /// need to be there costs. `Resources.Load` works in the editor without play mode.
    /// </summary>
    public class InputGlyphTests
    {
        private static InputActionAsset Asset()
        {
            var asset = Resources.Load<InputActionAsset>("TumbangPreso");
            Assert.IsNotNull(asset, "the shipped input asset is missing from Resources");
            return asset;
        }

        /// <summary>
        /// The same transformation `Hud.SingleKeyLabel` performs, because the glyph table is
        /// keyed on that function's OUTPUT.
        ///
        /// ⚠️⚠️ IT IS COPIED RATHER THAN CALLED, AND THE COPY IS THE POINT OF THE ASSERTION
        /// BELOW. `Hud.SingleKeyLabel` is private and `Hud` is a `MonoBehaviour` in a 4,800-line
        /// file that pulls the whole UI assembly in with it. What matters is that the two agree,
        /// and `TheLabelsThisTestBuildsAreTheLabelsTheHudBuilds` asserts exactly that against the
        /// public `Hud.KeyLabelFor`, so this copy cannot drift silently.
        /// </summary>
        private static string LabelFor(string effectivePath)
        {
            if (string.IsNullOrEmpty(effectivePath)) return "";

            string key = InputControlPath.ToHumanReadableString(
                effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);

            if (string.IsNullOrEmpty(key)) return "";

            if (key == "Left Button" || key == "LeftButton") return "LMB";
            if (key == "Right Button" || key == "RightButton") return "RMB";
            if (key == "Middle Button" || key == "MiddleButton") return "MMB";

            return key.ToUpperInvariant();
        }

        [Test]
        public void EveryShippedBindingHasAGlyph()
        {
            var asset = Asset();
            var map = asset.FindActionMap("Player");
            Assert.IsNotNull(map, "the Player action map is gone");

            var missing = new SortedDictionary<string, string>();

            foreach (var action in map.actions)
            {
                foreach (var binding in action.bindings)
                {
                    // A composite HEAD is not a control. `Hud.ResolveKeyLabel` records what
                    // printing one costs: the tutorial taught `[2DVECTOR(MODE:2)]`.
                    if (binding.isComposite) continue;

                    string label = LabelFor(binding.effectivePath);
                    if (string.IsNullOrEmpty(label)) continue;
                    if (InputGlyphs.Has(label)) continue;

                    missing[label] = action.name + "  " + binding.effectivePath;
                }
            }

            if (missing.Count == 0) return;

            var report = new StringBuilder();
            report.AppendLine(
                "controls the game teaches that have no picture, so their prompt falls back to " +
                "text. Add a row to `InputGlyphs.BuildTable` for each, or say here why it is " +
                "text on purpose:");

            foreach (var pair in missing)
                report.AppendLine($"    \"{pair.Key}\"   ({pair.Value})");

            Assert.Fail(report.ToString());
        }

        /// <summary>
        /// ⚠️ THE TABLE'S OWN CELLS ARE CHECKED AGAINST THE REAL SHEETS, because a row with a
        /// plausible number in it is the failure mode this whole file is about. `InputGlyphs.For`
        /// returns null for an out-of-bounds cell rather than throwing, so a typo would present
        /// as one prompt quietly drawing text.
        /// </summary>
        [Test]
        public void EveryTableEntryResolvesToASpriteOnBothGrounds()
        {
            var dead = new List<string>();

            foreach (string label in InputGlyphs.KnownLabels())
            {
                if (InputGlyphs.For(label, true) == null) dead.Add(label + " (on dark)");
                if (InputGlyphs.For(label, false) == null) dead.Add(label + " (on light)");
            }

            Assert.IsEmpty(dead,
                "these glyph table rows point at a cell that is not on the sheet, so the prompt " +
                "silently draws text. Check the row and column against `Logs/input-glyphs-v1.png`, " +
                "which `tools/build_input_glyphs.py --contact` writes:\n    " +
                string.Join("\n    ", dead));
        }

        /// <summary>
        /// ⚠️⚠️ THE SHEETS ARE THE OUTPUT OF `tools/build_input_glyphs.py` AND THEIR SIZE IS THE
        /// CONTRACT. Every row and column number in `InputGlyphs` was read off these exact
        /// dimensions; a sheet re-exported at a different size puts every glyph in the game one
        /// cell out, and the failure looks like art rather than like a size change.
        /// </summary>
        [Test]
        public void TheSheetsAreTheSizeTheTableWasWrittenAgainst()
        {
            var expected = new (string Path, int W, int H)[]
            {
                ("UI/input/glyphs_key_v1", 256, 416),
                ("UI/input/glyphs_pad_v1", 112, 432),
                ("UI/input/glyphs_mouse_v1", 96, 160),
                ("UI/input/glyphs_stick_v1", 96, 192),
            };

            foreach (var (path, w, h) in expected)
            {
                var texture = Resources.Load<Texture2D>(path);
                Assert.IsNotNull(texture,
                    $"{path} is missing. Run `python tools/build_input_glyphs.py` with the pack " +
                    "in `scratchpad/input-icons/`.");

                Assert.AreEqual(w, texture.width, $"{path} is not the width the table assumes");
                Assert.AreEqual(h, texture.height, $"{path} is not the height the table assumes");
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE COPY OF `SingleKeyLabel` IN THIS FILE IS CHECKED AGAINST THE REAL ONE.
        /// `LabelFor` above restates a private method, which is exactly the *"two independent
        /// statements of the same fact"* `GuidedTrainingHud.SkipKeyLabel` warns about. This makes
        /// the restatement safe: if `Hud` ever changes how it renders a control name, the glyph
        /// table is keyed on the OLD strings and every prompt loses its picture at once, and this
        /// is the assertion that says so instead of letting it happen quietly.
        /// </summary>
        [Test]
        public void TheLabelsThisTestBuildsAreTheLabelsTheHudBuilds()
        {
            var asset = Asset();
            var map = asset.FindActionMap("Player");

            foreach (string action in new[] { "Jump", "Sprint", "Grab", "Skill1", "Skill2", "Ultimate" })
            {
                var found = map.FindAction(action);
                Assert.IsNotNull(found, $"{action} is gone from the Player map");

                string mine = LabelFor(found.bindings[0].effectivePath);
                string theirs = Hud.KeyLabelFor(action);

                Assert.AreEqual(mine, theirs,
                    $"this test and `Hud.KeyLabel` disagree about {action}, so the glyph table is " +
                    "keyed on strings the game no longer produces");
            }
        }
    }
}
