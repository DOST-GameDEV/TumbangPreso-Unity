using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Drives the setup screen through both arrangements and both tabs, and asks the two
    /// questions a screenshot cannot answer.
    ///
    /// ⚠️⚠️ QUESTION ONE: DOES EVERY NODE THE SCRIPT REACHES FOR STILL RESOLVE? `ConvertedScreen`
    /// finds every control by the name Godot gave it and logs an ERROR when one is missing, and
    /// the Unity test framework fails a test on an unexpected `LogError`. So merely LOADING the
    /// screen under each style is the assertion: if `LobbyChrome` ever moves a node somewhere the
    /// finder cannot see, or a future edit renames one, this goes red with the name in the
    /// message. That is the guarantee `docs/TODO.md` § 68.4 asks for, made mechanical.
    ///
    /// ⚠️⚠️ QUESTION TWO: DOES ANY STRING RUN OUT OF ITS BOX? 🧑 2026-08-28, twice: *"make sure ur
    /// shti doesnt have iverfkiw"*, *"make sure ui and hud doesnt overflow"*. This project has
    /// shipped that bug at least four times under four separate diagnoses
    /// (`ConvertedScreen.SetHeadline` records three in one session, `GameVersion.ApplyTo` a
    /// fourth), and every fix was local to the label that was noticed. The failure is silent in
    /// BOTH directions: a `Wrap` label reflows into a box with no second line and is clipped, and
    /// an `Overflow` label draws straight past the edge. Neither shows up in a compile, a check,
    /// or a test that is not this one.
    ///
    /// ⚠️ IT ASSERTS ON `preferredWidth`, MEASURED THROUGH THE COMPONENT ITSELF, for the reason
    /// `Hud.WorstCaseNameWidth` and `ConvertedScreen.SetHeadline` both give: that is what THIS
    /// text, in THIS font, with THESE generator settings will actually lay out to. A spare font
    /// metric is a different number.
    /// </summary>
    public class LobbyStyleProbe
    {
        /// <summary>
        /// ⚠️ THE SHIPPED WINDOWED SIZE, NOT THE BATCH RUNNER'S 640x480. `UiClickProbe` carries
        /// the same line and the same reason: at 4:3 the right-hand column sits outside the
        /// viewport and every control in it reports as broken, which is a truthful statement
        /// about a 4:3 window and a useless one about the game.
        /// </summary>
        private const int Width = 1600;
        private const int Height = 900;

        /// <summary>
        /// ⚠️⚠️ LONG ENOUGH FOR THE FIT PASSES AND THE LAYOUT CHAIN, AND THREE FRAMES IS NOT.
        /// `ConvertedMatchSetup.FitEverything` runs once per frame for `FitPasses` frames after a
        /// refresh, and each pass depends on a `ContentSizeFitter` resolving from rows that
        /// resolve from their own children. `UiClickProbe` settled on 120 frames for the same
        /// chain after a three-frame version reported every seat row as unreachable.
        /// </summary>
        private const int SettleFrames = 120;

        [UnityTest]
        public IEnumerator EveryLabelFitsItsBoxInBothStyles()
        {
            Screen.SetResolution(Width, Height, false);
            for (int i = 0; i < 10; i++) yield return null;

            var previousStyle = LobbyChrome.Style;
            bool previousNetworked = SceneFlow.Networked;

            var report = new StringBuilder();
            var overflowing = new List<string>();

            foreach (var style in new[] { LobbyStyle.Classic, LobbyStyle.Street })
            {
                foreach (bool lobby in new[] { false, true })
                {
                    LobbyChrome.Style = style;
                    SceneFlow.Networked = lobby;

                    var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
                    while (load != null && !load.isDone) yield return null;

                    for (int i = 0; i < SettleFrames; i++) yield return null;

                    Canvas.ForceUpdateCanvases();

                    string what = $"{style} / {(lobby ? "LOBBY" : "PRACTICE")}";
                    report.AppendLine($"--- {what} ---");

                    Measure(what, report, overflowing);

                    // ⚠️ THE SESSION IS STOPPED BETWEEN ARMS. The lobby auto-hosts on arrival, so
                    // without this the second lobby arm starts a host while the first one is still
                    // listening and binds the same port. That is not the fault under test.
                    var net = Net.NetSession.Instance;
                    if (net != null) net.Stop();

                    for (int i = 0; i < 5; i++) yield return null;
                }
            }

            LobbyChrome.Style = previousStyle;
            SceneFlow.Networked = previousNetworked;

            Debug.Log("[Fit] setup screen\n" + report);

            Assert.IsEmpty(overflowing,
                "these labels draw outside the box they were given:\n  " +
                string.Join("\n  ", overflowing));
        }

        /// <summary>
        /// ⚠️ A `Wrap` LABEL IS MEASURED ON HEIGHT AND AN `Overflow` ONE ON WIDTH, because those
        /// are the two different ways each fails. Asking a wrapping paragraph whether it fits on
        /// one line would fail every hint on the screen for doing exactly what it is supposed to.
        /// </summary>
        private static void Measure(string arm, StringBuilder report, List<string> overflowing)
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,
                                                                    FindObjectsSortMode.None))
            {
                foreach (var label in canvas.GetComponentsInChildren<Text>(false))
                {
                    if (label == null) continue;
                    if (string.IsNullOrWhiteSpace(label.text)) continue;
                    if (label.color.a < 0.05f) continue;

                    var rect = label.rectTransform.rect;

                    // A rect the layout has not given a size to cannot be judged, and reporting it
                    // would be reporting the probe's own timing as a defect.
                    if (rect.width <= 1.0f || rect.height <= 1.0f) continue;

                    bool wraps = label.horizontalOverflow == HorizontalWrapMode.Wrap;

                    // ⚠️ ONE PIXEL OF SLACK. `preferredWidth` is computed from the same generator
                    // the renderer uses but rounds differently at the last glyph, and a zero
                    // tolerance reports a label that is visually perfect.
                    const float slack = 1.0f;

                    if (wraps)
                    {
                        if (label.preferredHeight <= rect.height + slack) continue;

                        string line = $"{Path(label.transform)}  wraps to " +
                                      $"{label.preferredHeight:F0} px in a {rect.height:F0} px box";
                        report.AppendLine("  OVER " + line);
                        overflowing.Add($"{arm}: {line}");
                        continue;
                    }

                    if (label.preferredWidth <= rect.width + slack) continue;

                    string over = $"{Path(label.transform)}  needs " +
                                  $"{label.preferredWidth:F0} px in a {rect.width:F0} px box " +
                                  $"at {label.fontSize} units: \"{Short(label.text)}\"";

                    report.AppendLine("  OVER " + over);
                    overflowing.Add($"{arm}: {over}");
                }
            }
        }

        private static string Short(string text)
            => text.Length <= 40 ? text : text.Substring(0, 40) + "...";

        private static string Path(Transform t)
        {
            var parts = new List<string>();

            while (t != null)
            {
                parts.Insert(0, t.name);
                t = t.parent;
            }

            return string.Join("/", parts);
        }
    }
}
