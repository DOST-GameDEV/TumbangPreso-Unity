using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// Every GameObject the code-built UI creates gets a `RectTransform`.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE ONE MISSING TYPE ARGUMENT TOOK THE WHOLE HUD DOWN AND EVERY SUITE
    /// STAYED GREEN. `BuildHeroDeck` created its root with `new GameObject("HeroDeck")`, which
    /// gives a plain `Transform`. It had been getting a `RectTransform` for free because the very
    /// next line added an `Image`, and every uGUI graphic requires one; the Overwatch redesign
    /// removed that background plate and silently removed the `RectTransform` with it.
    /// `GetComponent&lt;RectTransform&gt;()` then returned null and `Hud.Build` threw out of
    /// `Awake`.
    ///
    /// ⚠️⚠️ AND THE SYMPTOM LOOKED LIKE THREE UNRELATED BUGS. An exception in `Awake` abandons
    /// the REST of `Build`, so the scoreboard drew as an empty box, the ability deck was missing
    /// and the crosshair never appeared. Nothing pointed at the deck. It was found by reading
    /// `Player.log` after `tools/shoot_player.ps1`, which was the only check in the project that
    /// looks at a built HUD at all: 95 EditMode and 55 PlayMode tests passed over the top of it.
    ///
    /// ⚠️⚠️ IT READS THE SOURCE RATHER THAN BUILDING A HUD, AND BOTH ALTERNATIVES WERE TRIED
    /// FIRST. In EditMode, `AddComponent` does NOT run `Awake`, so a constructed HUD builds
    /// nothing at all and the test asserts against an empty object. In PlayMode it does run, but
    /// adding three more tests to that suite turned `LandedHighlightTests` red: that test's own
    /// comment warns it is order-sensitive and *"passed in isolation and failed in a full run"*.
    /// A source audit has neither problem, runs in a millisecond, and is the same technique
    /// `DeadFeatureAudit` already uses in this folder.
    /// </summary>
    public sealed class HudBuildTests
    {
        private static readonly string UiRoot =
            Path.Combine("Assets", "TumbangPreso", "Runtime", "UI");

        /// <summary>
        /// A `new GameObject("Name")` with no second argument, and the line number it is on.
        /// </summary>
        private static readonly Regex BareConstruction =
            new Regex(@"new GameObject\(\s*(""[^""]*""|\$""[^""]*""|[A-Za-z_][\w.]*)\s*\)");

        /// <summary>
        /// Anything that brings a `RectTransform` with it.
        ///
        /// ⚠️ THE LAYOUT COMPONENTS COUNT, AND LEAVING THEM OUT GAVE THREE FALSE POSITIVES ON
        /// THE FIRST RUN. `HorizontalLayoutGroup`, `VerticalLayoutGroup` and `LayoutElement` all
        /// carry `[RequireComponent(typeof(RectTransform))]` exactly as `Graphic` does, so a
        /// score row that only ever gets a layout group is perfectly correct. A test that cries
        /// wolf on working code is a test that gets deleted.
        /// </summary>
        private static readonly Regex BringsARect =
            new Regex(@"AddComponent<\s*(Image|Text|RawImage|Canvas|CanvasGroup|" +
                      @"HorizontalLayoutGroup|VerticalLayoutGroup|GridLayoutGroup|" +
                      @"LayoutElement|ContentSizeFitter|ScrollRect|Mask|RectMask2D|" +
                      @"UnityEngine\.UI\.[A-Za-z]+)\s*>");

        /// <summary>
        /// ⚠️⚠️ THE RULE IS NARROW ON PURPOSE, AND THE BROAD VERSION WAS TRIED FIRST. "Every
        /// GameObject in the UI must be built with a RectTransform" flagged twenty-eight call
        /// sites, nearly all of them correct: a `HorizontalLayoutGroup` requires a rect just as
        /// a `Graphic` does, cameras and the EventSystem are not uGUI at all, and a row that is
        /// only ever parented never needs one. A test with twenty-seven false positives is a
        /// test somebody deletes.
        ///
        /// What actually breaks is narrower and entirely mechanical: an object whose
        /// `RectTransform` is READ before anything has given it one. That is the exact shape of
        /// the deck fault, it cannot be argued with, and it has no false positives.
        /// </summary>
        [Test]
        public void NothingReadsARectTransformBeforeSomethingCreatesIt()
        {
            var offences = new List<string>();

            foreach (string file in Directory.GetFiles(UiRoot, "*.cs", SearchOption.AllDirectories))
            {
                string[] lines = File.ReadAllLines(file);

                for (int i = 0; i < lines.Length; i++)
                {
                    var made = BareConstruction.Match(lines[i]);
                    if (!made.Success) continue;

                    // Only interested when the construction is bound to a local we can follow.
                    var bound = Regex.Match(lines[i], @"var\s+(\w+)\s*=\s*new GameObject\(");
                    if (!bound.Success) continue;

                    string name = bound.Groups[1].Value;
                    var reads = new Regex($@"\b{name}\b\s*\.\s*GetComponent<\s*RectTransform\s*>");
                    var casts = new Regex($@"\(RectTransform\)\s*{name}\b\s*\.\s*transform");

                    // Walk forward to the end of the block and see which comes first: something
                    // that CREATES the rect, or something that READS it.
                    for (int j = i + 1; j < System.Math.Min(i + 30, lines.Length); j++)
                    {
                        if (BringsARect.IsMatch(lines[j])) break;
                        if (lines[j].Contains("typeof(RectTransform)")) break;
                        if (lines[j].Contains($"{name}.AddComponent<RectTransform>")) break;

                        if (!reads.IsMatch(lines[j]) && !casts.IsMatch(lines[j])) continue;

                        offences.Add(
                            $"{Path.GetFileName(file)}:{j + 1} reads the rect of '{name}', " +
                            $"built on line {i + 1} with a plain Transform");
                        break;
                    }
                }
            }

            Assert.IsEmpty(offences,
                "A uGUI object built with `new GameObject(name)` has a plain Transform, and " +
                "`GetComponent<RectTransform>()` on it returns NULL. Pass " +
                "`typeof(RectTransform)` to the constructor. The failure is not local: an " +
                "exception thrown out of a Build method abandons every widget after it, so one " +
                "of these takes down half a screen and looks like several unrelated bugs.\n  " +
                string.Join("\n  ", offences));
        }

        /// <summary>
        /// ⚠️ THE DECK ROOT SPECIFICALLY, BY NAME, because it is the one that actually broke and
        /// because it is now the only object in the deck with no graphic of its own: the
        /// redesign deleted its background plate on purpose, so it can never get a
        /// `RectTransform` the lazy way again.
        /// </summary>
        [Test]
        public void TheHeroDeckRootAsksForItsRectTransformExplicitly()
        {
            string hud = File.ReadAllText(Path.Combine(UiRoot, "Hud.cs"));

            Assert.IsTrue(hud.Contains("new GameObject(\"HeroDeck\", typeof(RectTransform))"),
                "the hero deck root no longer asks for a RectTransform. It has no background " +
                "image to bring one along, so `Hud.Build` will throw out of Awake and take the " +
                "scoreboard, the deck and the crosshair with it.");
        }
    }
}
