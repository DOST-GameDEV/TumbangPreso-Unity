using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Every string the HUD can draw, measured against the box it is drawn in, at all nine
    /// shipped resolutions.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE HUD OVERFLOW HAS BEEN FIXED ONE STRING AT A TIME AND KEPT COMING
    /// BACK. 🧑, from playing the 2026-08-26 build: *"fix all UI overflows as well for the HUDS
    /// bcz theres a lot"*. `docs/TODO.md` § 18 is the entry, and its first requirement is this
    /// probe rather than the next fix: *"A probe that FINDS them, before anything is fixed."*
    /// One instance was already closed by hand (§ 9.5, the objective card reading
    /// `FETCH SLIPPER · -5 / SEC`), and closing them individually is what produced an entry
    /// about the CLASS.
    ///
    /// ⚠️⚠️ THE CAUSE IS ONE DELIBERATE LINE AND IT IS NOT A BUG. `Hud.HudLabel` sets
    /// `horizontalOverflow` and `verticalOverflow` to `Overflow` on every label it builds, so a
    /// string that does not fit neither wraps nor shrinks: it hangs out of its box. That is the
    /// right default for a HUD, because a wrapped timer or a shrunk score is worse than a wide
    /// one. What follows from it is that **every card must be sized against the longest string it
    /// can EVER show**, not against whatever was in it when somebody looked.
    ///
    /// ⚠️⚠️ AND THE FONT SIZE IS NEVER THE LEVER. `ui_theme.gd` records these sizes going 16/13,
    /// then 22/19, then 30/28, answered every time with *"text still small"*. Size the box, or
    /// shorten the string. The one documented exception is `RecastFontSize`, 14 against the
    /// deck's 22, because six bold capitals do not fit a 60 px tile.
    ///
    /// ⚠️⚠️ MEASURED IN THE CANVAS'S OWN SPACE, NEVER IN WORLD CORNERS. `SettingsScrollProbe` did
    /// the latter first and printed ZERO for every column while passing all nine resolutions,
    /// because on a canvas rendering to a camera every element sits within a hair of the same
    /// world x. `AspectRatioProbes.AssertInside` has the conversion and this uses the same one.
    ///
    /// ⚠️ IT DOES NOT ACTIVATE ANYTHING, WHICH IS A DELIBERATE LIMIT ON WHAT IT CLAIMS. § 18 asks
    /// for the HUD to be driven through every state it has. Forcing every hidden group active
    /// would run `Update` on components whose match state does not exist in a probe, and an
    /// unexpected error log is a PlayMode failure: the probe would go red for a reason that has
    /// nothing to do with a string being too wide. Instead it measures every `Text` the HUD
    /// BUILDS, active or not, and substitutes the worst-case string each one can hold. A label's
    /// preferred width is a property of the font, the size and the string; it does not depend on
    /// whether the object is switched on. What this cannot see is a box whose width is computed
    /// by a layout group only while it is active, and the report says so per line.
    /// </summary>
    public class HudOverflowProbe
    {
        private const string OutPath = "Logs/hud-overflow.txt";

        /// <summary>Half a unit, the same slack `AspectRatioProbes` allows for fractional layout.</summary>
        private const float Slack = 0.5f;

        /// <summary>⚠️ THE SAME NINE `AspectRatioProbes` USES. Kept as a copy rather than shared
        /// because that probe's list is private to it and this one must not silently start
        /// testing a different set of screens than the layout probe does. If they ever disagree,
        /// that is a defect in one of the two files and not in the HUD.</summary>
        private static readonly (int W, int H, string Name)[] Resolutions =
        {
            (1280,  720, "16:9 720p"),
            (1600,  900, "16:9 900p"),
            (1920, 1080, "16:9 1080p"),
            (2560, 1440, "16:9 1440p"),
            (1366,  768, "16:9 laptop"),
            (1920, 1200, "16:10"),
            (2560, 1080, "21:9"),
            (3440, 1440, "21:9 1440p"),
            (1024,  768, "4:3"),
        };

        /// <summary>
        /// The longest ability names in the game, at 17 characters.
        ///
        /// ⚠️ THEY ARE THE WORST CASE FOR ANY LABEL THAT EVER HOLDS A POWER'S NAME, and § 18
        /// names all three. A probe fed "P1" proves nothing.
        /// </summary>
        private static readonly string[] LongestAbilityNames =
        {
            "PERMAFROST SHEET", "DEMONIC CARAPACE", "ASTRAL PROJECTION",
        };

        /// <summary>
        /// ⚠️⚠️ THE LABELS WHOSE BOX IS FIXED AND WHOSE OVERFLOW IS THEREFORE A DEFECT.
        /// § 18: *"Assert on the ones that are inside a fixed-width card; report the rest."*
        /// Everything else in the HUD is deliberately allowed to hang: `CrosshairLabel` is a
        /// 34 pt glyph centred in a 24 unit box on purpose, `HitmarkerLabel` and `LataDownAlert`
        /// are centred banners with the whole screen to spill into, and asserting on those would
        /// make this probe fail for the design rather than for a fault.
        /// </summary>
        private static readonly HashSet<string> Asserted = new HashSet<string>
        {
            "Name", "Score", "Role", "LataHintLabel", "LataLabel",
            "RoundLabel", "TimerPressure", "TimerLabel",
            "State", "Key", "HypeTitle", "HypeEvent",
        };

        [UnityTest]
        public IEnumerator EveryHudStringFitsTheBoxItIsDrawnIn()
        {
            var previousMode = SceneFlow.SelectedMode;

            // ⚠️ HERO STRIKE, BECAUSE IT IS THE SUPERSET. The deck, the ultimate meter and the
            // recast tile only exist in this mode, and every Classic label exists here too.
            // Probing Classic would leave a third of the HUD unmeasured.
            SceneFlow.SelectedMode = GameMode.HeroStrike;

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;
            for (int i = 0; i < 30; i++) yield return null;

            var hud = Object.FindFirstObjectByType<Hud>();
            Assert.IsNotNull(hud, "no HUD in the arena, so there is nothing to measure.");

            var canvas = hud.GetComponentInChildren<Canvas>(true);
            Assert.IsNotNull(canvas, "the HUD built no canvas.");

            var camera = Camera.main;
            Assert.IsNotNull(camera, "the arena has no main camera to render a sized target.");

            // Same mechanism as `AspectRatioProbes`: `Screen.SetResolution` does nothing to
            // `Screen.width` in the editor, so a probe built on it asserts against the batch
            // runner's own window at every "resolution" and passes for all nine.
            var previousMode2 = canvas.renderMode;
            var previousCam = canvas.worldCamera;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = camera.nearClipPlane + 0.01f;

            var canvasRt = (RectTransform)canvas.transform;
            var labels = new List<Text>(hud.GetComponentsInChildren<Text>(true));
            Assert.IsNotEmpty(labels, "the HUD built no labels at all.");

            var report = new StringBuilder();
            var failures = new List<string>();

            report.AppendLine("HUD OVERFLOW, worst-case strings, all nine shipped resolutions.");
            report.AppendLine("box   = the width the label is laid out in, in canvas reference units");
            report.AppendLine("want  = Text.preferredWidth for the worst string it can hold");
            report.AppendLine("over  = want - box, positive means it hangs out of its box");
            report.AppendLine();

            var previousTarget = camera.targetTexture;
            RenderTexture target = null;

            foreach (var (w, h, name) in Resolutions)
            {
                var next = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = next;

                if (target != null) target.Release();
                target = next;

                // Three frames, for the reason `AspectRatioProbes` records: the scaler recomputes
                // in its own Update and the layout rebuild lands the frame after that.
                for (int i = 0; i < 3; i++) yield return null;

                report.AppendLine($"---- {name} ({w}x{h}) canvas {canvasRt.rect.width:F0}x{canvasRt.rect.height:F0}");

                foreach (var label in labels)
                {
                    if (label == null || label.font == null) continue;

                    Measure(label, name, report, failures);
                }

                report.AppendLine();
            }

            camera.targetTexture = previousTarget;
            if (target != null) target.Release();

            canvas.renderMode = previousMode2;
            canvas.worldCamera = previousCam;
            SceneFlow.SelectedMode = previousMode;

            Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
            File.WriteAllText(OutPath, report.ToString());
            Debug.Log($"[HudOverflow] wrote {OutPath}\n{report}");

            if (failures.Count > 0)
            {
                Assert.Fail($"{failures.Count} HUD label(s) overflow a fixed box. " +
                            "Size the card, or shorten the string, and never the font " +
                            "(docs/TODO.md section 18):\n  " + string.Join("\n  ", failures));
            }
        }

        /// <summary>
        /// Try every string this label can ever hold and keep the widest.
        ///
        /// ⚠️ IT RESTORES THE LIVE TEXT AFTERWARDS. The HUD is a running object in a loaded
        /// scene and the next resolution's pass reads the same components; leaving a probe string
        /// in a label would make every later measurement a measurement of the probe.
        /// </summary>
        private static void Measure(Text label, string resolution,
                                    StringBuilder report, List<string> failures)
        {
            string keep = label.text;

            string worstString = keep ?? string.Empty;
            float worstWidth = -1.0f;

            int keepSize = label.fontSize;

            foreach (string candidate in Candidates(label, keep))
            {
                if (candidate == null) continue;

                // ⚠️⚠️ ONE STRING IN THIS HUD IS DRAWN AT A DIFFERENT SIZE FROM THE LABEL THAT
                // HOLDS IT, and measuring it at the label's own size invents an overflow.
                // `Hud.PaintSkillCard` sets `RecastFontSize` (14) instead of the deck's 22
                // whenever a card is recastable, because six bold capitals do not fit a 60 px
                // tile. `docs/TODO.md` § 18 calls that out as the one documented place where
                // shrinking WAS the answer, and it is an exception rather than a pattern. The
                // probe has to honour it or it reports the exception as the bug.
                label.fontSize = candidate == "RECAST" && label.name == "State"
                    ? Hud.RecastFontSize
                    : keepSize;

                label.text = candidate;
                float width = label.preferredWidth;

                if (width > worstWidth)
                {
                    worstWidth = width;
                    worstString = candidate;
                }
            }

            label.text = keep;
            label.fontSize = keepSize;

            float box = BoxWidth(label, out string boxKind);
            bool fixedBox = box > 0.0f;
            float over = fixedBox ? worstWidth - box : 0.0f;

            string live = label.gameObject.activeInHierarchy ? "" : "  [hidden]";

            report.AppendLine(
                $"  {label.name,-20} box {(fixedBox ? box.ToString("F0") : "free"),6} " +
                $"want {worstWidth,6:F0}  over {(fixedBox ? over.ToString("F0") : "-"),5} " +
                $"({boxKind}) \"{Trim(worstString)}\"{live}");

            // ⚠️⚠️ A HIDDEN LABEL'S BOX IS NOT A BOX, AND ASSERTING ON ONE IS 200 FALSE
            // FAILURES. The first run of this probe reported 205 overflows and most of them were
            // this: a `RectTransform` that has never been laid out reports its authored
            // `sizeDelta`, which for anything built by `MenuKit.Stretch` or driven by a parent
            // layout group is the uGUI default of **100 x 100**. So `LataHintLabel` came back as
            // a 527-unit string in a "100-unit box" while the card it actually lives in is sized
            // by `Hud.WidestLineWidth` and fits it exactly. The class note already says this
            // probe cannot see a box computed only while active; this is where that limit has to
            // be enforced rather than merely written down.
            //
            // ⚠️ THEY ARE STILL PRINTED, MARKED `[hidden]`. The width is real even when the box
            // is not, so the line is worth reading when a card is being sized by hand. It is the
            // assertion that has to stay off them.
            if (fixedBox && over > Slack && Asserted.Contains(label.name)
                && label.gameObject.activeInHierarchy)
            {
                failures.Add(
                    $"{resolution}: '{label.name}' needs {worstWidth:F0} units for " +
                    $"\"{Trim(worstString)}\" in a {box:F0}-unit box ({boxKind}), so it hangs " +
                    $"{over:F0} units out.");
            }

            // ⚠️⚠️ AND THE SECOND HALF, WHICH IS THE ONE § 9.5 ACTUALLY WAS. A card anchored to
            // the RIGHT screen corner does not merely overflow its plate: what runs past it
            // leaves the SCREEN. The objective card read `FETCH SLIPPER · -5 / SEC` because the
            // string is `-5 / SECOND` and there was no room to the right of it at all. A label
            // can fit its box perfectly and still be cut off by the edge of the display.
            if (label.gameObject.activeInHierarchy && worstWidth > 0.0f)
            {
                AssertOnScreen(label, worstWidth, worstString, resolution, failures);
            }
        }

        /// <summary>
        /// Where the drawn string actually lands, in canvas units, given that it is allowed to
        /// overflow its own rect.
        ///
        /// ⚠️ THE ALIGNMENT DECIDES WHICH WAY IT SPILLS, and that is the whole calculation. A
        /// left-aligned label grows to the RIGHT off its rect's left edge; a right-aligned one
        /// grows to the LEFT off its right edge; a centred one grows both ways at half the rate.
        /// Assuming centred for everything would report a right-anchored card as fine, which is
        /// exactly the bug this half exists for.
        /// </summary>
        private static void AssertOnScreen(Text label, float wantWidth,
                                           string worstString, string resolution,
                                           List<string> failures)
        {
            // ⚠️⚠️ EACH LABEL IS MEASURED AGAINST ITS OWN CANVAS, AND USING THE HUD'S ROOT CANVAS
            // FOR ALL OF THEM IS THE SAME TRAP § 18 WARNS ABOUT, ONE LEVEL UP. The class note
            // above records `SettingsScrollProbe` measuring in world corners and printing zero
            // for everything; this is the mirror image, and the probe found it on itself. The
            // HUD is not one canvas: `OffscreenIndicators` builds its OWN canvas for the arrows
            // that point at the lata when it is off camera, and that canvas sits somewhere else
            // entirely in world space. Converting one of its corners into the HUD canvas's local
            // space reported `CanArrow` as running **3,323,799 units off the LEFT of the
            // screen**, which is not an overflow, it is two different coordinate systems.
            //
            // ⚠️ A NUMBER THAT ABSURD IS THE TELL, and it is worth saying so: a real overflow
            // here is tens of units. Anything in the millions means the conversion is wrong
            // rather than the layout.
            var canvas = label.canvas;
            if (canvas == null) return;

            var canvasRt = (RectTransform)canvas.transform;

            var rt = label.rectTransform;
            var rect = rt.rect;

            float min, max;

            switch (label.alignment)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.LowerLeft:
                    min = rect.xMin;
                    max = rect.xMin + wantWidth;
                    break;

                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    min = rect.xMax - wantWidth;
                    max = rect.xMax;
                    break;

                default:
                    float mid = rect.center.x;
                    min = mid - wantWidth * 0.5f;
                    max = mid + wantWidth * 0.5f;
                    break;
            }

            Vector3 left = canvasRt.InverseTransformPoint(rt.TransformPoint(new Vector3(min, 0.0f, 0.0f)));
            Vector3 right = canvasRt.InverseTransformPoint(rt.TransformPoint(new Vector3(max, 0.0f, 0.0f)));

            var canvasRect = canvasRt.rect;

            if (left.x < canvasRect.xMin - Slack)
            {
                failures.Add($"{resolution}: '{label.name}' drawing \"{Trim(worstString)}\" runs " +
                             $"{canvasRect.xMin - left.x:F0} units off the LEFT of the screen.");
            }

            if (right.x > canvasRect.xMax + Slack)
            {
                failures.Add($"{resolution}: '{label.name}' drawing \"{Trim(worstString)}\" runs " +
                             $"{right.x - canvasRect.xMax:F0} units off the RIGHT of the screen.");
            }
        }

        /// <summary>
        /// The width this label is laid out in, and where that number came from.
        ///
        /// ⚠️ A `ContentSizeFitter` MEANS THERE IS NO BOX. The label grows to whatever it holds,
        /// so overflow is impossible by construction and reporting a number for it would be
        /// reporting the string's own width twice.
        /// </summary>
        private static float BoxWidth(Text label, out string kind)
        {
            var fitter = label.GetComponent<ContentSizeFitter>();
            if (fitter != null && fitter.horizontalFit != ContentSizeFitter.FitMode.Unconstrained)
            {
                kind = "fitter";
                return -1.0f;
            }

            var element = label.GetComponent<LayoutElement>();
            if (element != null && element.preferredWidth >= 0.0f)
            {
                kind = "LayoutElement";
                return element.preferredWidth;
            }

            // ⚠️⚠️ A LABEL WHOSE PARENT LAYOUT GROUP CONTROLS ITS WIDTH HAS NO FIXED BOX EITHER,
            // AND TREATING ITS RECT AS ONE IS A FALSE POSITIVE THIS PROBE REPORTED NINE TIMES.
            // With `childControlWidth` on, the group sizes each child to that child's own
            // PREFERRED width, so the rect tracks whatever string is in the label right now. The
            // probe then swapped in a longer string, compared its width against a rect laid out
            // for the shorter one, and called the difference an overflow. `RoundLabel` reported
            // "needs 510 in a 305-unit box" while the column around it was already over 510 wide
            // and the label would have grown to fit on the next layout pass.
            //
            // ⚠️ WHAT ACTUALLY CONSTRAINS SUCH A LABEL IS THE GROUP'S OWN RECT, because the
            // preferred width is clamped to it. That is a real limit and it is worth printing,
            // so the kind carries the group's inner width rather than nothing. It is not
            // asserted, for the same reason the `fitter` case is not: the box moves with the
            // content by design.
            var group = label.transform.parent != null
                ? label.transform.parent.GetComponent<HorizontalOrVerticalLayoutGroup>()
                : null;

            if (group != null && group.childControlWidth)
            {
                var groupRt = (RectTransform)group.transform;
                float inner = groupRt.rect.width - group.padding.left - group.padding.right;

                kind = $"group {inner:F0}";
                return -1.0f;
            }

            kind = "rect";
            return label.rectTransform.rect.width;
        }

        /// <summary>
        /// Every string a given label can hold, worst cases first.
        ///
        /// ⚠️⚠️ THE LATA HINTS COME FROM `Hud.LataHintLines` ITSELF, NOT FROM A COPY. § 18 warns
        /// that a line added to `UpdateLataCard` and not to that array is an overflow again;
        /// transcribing the strings here would add a third place to forget, and the one nothing
        /// fails over.
        ///
        /// ⚠️ AND THE NAME CASE IS `PlayerNameMax` "W"s, THE SAME CONSTANT THE NAME FIELD CLAMPS
        /// TO, measured the way `Hud.WorstCaseNameWidth` measures it. "W" is the worst glyph; a
        /// name like MMMMMM would beat any average-case guess.
        /// </summary>
        private static IEnumerable<string> Candidates(Text label, string live)
        {
            yield return live;

            switch (label.name)
            {
                case "Name":
                    yield return new string('W', Balance.PlayerNameMax);
                    break;

                case "Score":
                    yield return "-999";
                    break;

                case "Role":
                    yield return Hud.TayaBadge;
                    break;

                case "RoundLabel":
                    // ⚠️⚠️ FROM `Hud.TopCentreLines()`, NOT FROM A STRING TYPED HERE. The first
                    // version of this case guessed "ROUND 8 / 8" and the live label already held
                    // something far longer: the real line is `ROUND n / N   ·   DEFENDER: name`,
                    // and the warmup line is longer still. A probe fed a guess measures the
                    // guess. The HUD now builds both through one method and this reads it.
                    foreach (string line in Hud.TopCentreLines()) yield return line;
                    break;

                case "TimerLabel":
                    yield return "0:00";
                    break;

                case "TimerPressure":
                    yield return "SUDDEN DEATH";
                    break;

                case "LataLabel":
                case "LataHintLabel":
                    foreach (string line in Hud.LataHintLines) yield return line;
                    break;

                case "ReadyObjective":
                    yield return "TAYA (DEFENDER) P4";
                    foreach (string line in Hud.LataHintLines) yield return line;
                    break;

                case "State":
                    // The recast word is the six bold capitals `RecastFontSize` exists for.
                    yield return "RECAST";
                    yield return "99";
                    break;

                case "Key":
                    yield return "MOUSE4";
                    break;

                case "HypeTitle":
                case "HypeEvent":
                    yield return "PERFECT BANK SHOT";
                    break;

                default:
                    // ⚠️ ANY LABEL THAT MIGHT EVER NAME A POWER GETS THE THREE LONGEST NAMES.
                    // Cheap, and it is the case § 18 calls out by name.
                    if (label.name.Contains("Ability") || label.name.Contains("Skill") ||
                        label.name.Contains("Power") || label.name.Contains("Ult"))
                    {
                        foreach (string n in LongestAbilityNames) yield return n;
                    }
                    break;
            }
        }

        private static string Trim(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= 28 ? s : s.Substring(0, 27) + "…";
        }
    }
}
