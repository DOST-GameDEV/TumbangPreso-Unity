using System.Collections;
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
    /// The hero picker's config column, measured rather than described.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE THE SAME GAP HAS NOW BEEN "FIXED" THREE TIMES WITHOUT MOVING.
    /// 🧑 reported a band of empty wood between the hero's description and the first ability row
    /// on 2026-08-25 (*"theres big empty space in between character names and description"*), it
    /// was answered by top-aligning the label, then by resizing its `LayoutElement`, then by
    /// switching the `ContentSizeFitter`'s vertical axis off, and on 2026-08-26 he sent the same
    /// screenshot again: *"fix ui here, theres big open space"*. Three plausible causes were
    /// argued from the source and none of them was checked against a running layout.
    ///
    /// ⚠️ SO IT PRINTS THE WHOLE COLUMN, NOT JUST THE ASSERTION. A failing bound says the gap is
    /// too big; the dump says WHICH row is holding the height, which is the question every one of
    /// those three passes had to guess at. `docs/VISION.md` § 5: verify by measuring.
    ///
    /// ⚠️ IT RUNS IN PLAY MODE BECAUSE THE LAYOUT IS A RUNTIME PRODUCT. `ConvertedCharacterSelect`
    /// writes the tagline's height, the trait column's height and every ability row in `Refresh`,
    /// off a live roster; the authored scene shows none of those numbers.
    /// </summary>
    public class HeroPickerLayoutProbe
    {
        /// <summary>
        /// The most wood that may sit between the description and the first power.
        ///
        /// ⚠️ IT IS THE COLUMN'S OWN SPACING PLUS A LINE OF SLACK, NOT A ROUND NUMBER. `Rows` in
        /// `CharacterSelect.unity` is a `VerticalLayoutGroup` with `m_Spacing: 10`, so 10 px is
        /// the gap the design asks for and anything much past it is a box reserving height its
        /// text does not use. 24 leaves room for a descender and a border and still fails the
        /// 50-plus px band that was reported.
        /// </summary>
        private const float MaxGap = 24.0f;

        [UnityTest]
        public IEnumerator TheHeroPickerHasNoDeadBandAboveTheAbilityRows()
        {
            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            yield return new WaitForSecondsRealtime(1.0f);

            // The hero variant is the one with the ability rows in it; Classic's trait meters
            // are a different, simpler column and cannot show this fault.
            SceneFlow.SelectedMode = Core.GameMode.HeroStrike;

            var panel = Find("CharacterSelectPanel");
            Assert.IsNotNull(panel, "MatchSetup has no CharacterSelectPanel to open");
            panel.SetActive(true);

            for (int i = 0; i < 6; i++) yield return null;
            yield return new WaitForSecondsRealtime(0.5f);

            var rows = Find("TraitRows");
            var tagline = Find("TaglineLabel");

            Assert.IsNotNull(tagline, "no TaglineLabel in the open picker");
            Assert.IsNotNull(rows, "no TraitRows in the open picker");

            Transform firstAbility = null;
            foreach (Transform child in rows.transform)
            {
                if (!child.name.StartsWith("AbilityRow_")) continue;
                firstAbility = child;
                break;
            }

            Assert.IsNotNull(firstAbility,
                "the hero picker built no AbilityRow_0, so either the mode did not take or " +
                "RefreshHeroLoadout did not run. The dump below says what the column does hold.\n"
                + Dump(FindUnder(panel, "ConfigPanel"), 1.0f));

            // ⚠️⚠️ MEASURED IN THE CANVAS'S OWN 1920x1080 UNITS, NOT IN SCREEN PIXELS. The batch
            // runner renders at whatever window size it feels like, so a raw world-corner gap is
            // a different number on every machine and cannot be held against a layout constant
            // that was authored in reference space. Dividing by `scaleFactor` puts the answer
            // back into the units `Rows`' 10 px spacing is written in.
            var canvas = panel.GetComponentInParent<Canvas>();
            float scale = canvas != null && canvas.scaleFactor > 0.0001f ? canvas.scaleFactor : 1.0f;

            float taglineBottom = BottomOf(tagline.GetComponent<RectTransform>());
            float abilityTop = TopOf(firstAbility.GetComponent<RectTransform>());
            float gap = (taglineBottom - abilityTop) / scale;

            string dump = Dump(FindUnder(panel, "ConfigPanel"), scale);

            // ⚠️⚠️ THE SPACE IS INSIDE THE BOX, NOT BETWEEN THE BOXES, AND THE FIRST VERSION OF
            // THIS PROBE MEASURED THE WRONG ONE AND PASSED. Box to box was 10 px, exactly the
            // column's spacing, and looked healthy; the tagline's own rect was 96 px holding
            // about 50 px of text, so the band the player sees is the bottom half of a label.
            // A layout probe that only measures gaps between rects cannot see a box that is
            // simply too big for what is drawn in it.
            var taglineRt = tagline.GetComponent<RectTransform>();
            var taglineText = tagline.GetComponent<Text>();
            float slack = taglineRt.rect.height - taglineText.preferredHeight;

            Debug.Log($"[Picker] scaleFactor={scale:F3}  gap={gap:F1}  tagline box=" +
                      $"{taglineRt.rect.height:F0} text={taglineText.preferredHeight:F0} " +
                      $"slack={slack:F0}\n{dump}");

            Assert.LessOrEqual(gap, MaxGap,
                $"{gap:F0} px of empty wood sits between the hero's description and the first " +
                $"ability row, against a column spacing of 10. The row heights below name the " +
                $"box that is holding it.\n{dump}");

            Assert.LessOrEqual(slack, MaxSlack,
                $"the tagline's box is {taglineRt.rect.height:F0} px around " +
                $"{taglineText.preferredHeight:F0} px of text, so {slack:F0} px of empty wood is " +
                $"reserved under the hero's description. Check the LE() values below: " +
                $"GetPreferredHeight is Max(min, pref), so a stale minHeight beats whatever " +
                $"Refresh writes as the preference.\n{dump}");

            // ---- THE BOTTOM OF THE COLUMN AGAINST THE PANEL --------------------------------
            //
            // ⚠️⚠️ THIS PROBE WAS GREEN WHILE THE ULTIMATE'S PLATE DREW OUTSIDE THE WOOD, AND
            // THAT IS WHY THIS BLOCK EXISTS. 🧑 2026-08-29 with a screenshot of Dante's picker,
            // `docs/reports/2026-08-29/reported/13.png`: *"fix hud here it overflows"*. The fix
            // shipped as § 79.6 and this file did not change, so nothing here would have caught
            // it and nothing here would catch it coming back.
            //
            // ⚠️ THE TWO ASSERTIONS ABOVE CANNOT SEE IT, AND THAT IS STRUCTURAL RATHER THAN AN
            // OVERSIGHT. Both measure INSIDE the column: one the gap between two rows, the other
            // one box against its own text. A column whose rows are each correctly sized and
            // correctly spaced can still be taller than the panel holding it, and every
            // measurement between its own children stays healthy while it runs off the bottom.
            // The only thing that can see it is a child edge against the PARENT's edge.
            //
            // ⚠️ THE LAST ROW IS THE ONE MEASURED, not the column's rect. The column is a
            // `VerticalLayoutGroup` and its own rect is whatever the group computed, which is the
            // number that was already correct; the ULTIMATE row is the last child and the thing
            // that was drawn past the wood. Measuring the container would have passed then too.
            Transform lastAbility = null;
            foreach (Transform child in rows.transform)
            {
                if (child.name.StartsWith("AbilityRow_")) lastAbility = child;
            }

            Assert.IsNotNull(lastAbility, "the hero picker built no ability rows at all.\n" + dump);

            var configPanel = FindUnder(panel, "ConfigPanel");
            Assert.IsNotNull(configPanel, "no ConfigPanel to measure the column against.\n" + dump);

            float columnBottom = BottomOf(lastAbility.GetComponent<RectTransform>());
            float panelBottom = BottomOf(configPanel.GetComponent<RectTransform>());

            // Positive means the row's bottom edge is BELOW the panel's, in canvas units.
            float overflow = (panelBottom - columnBottom) / scale;

            Debug.Log($"[Picker] lastRow={lastAbility.name} overflow={overflow:F1}");

            Assert.LessOrEqual(overflow, MaxBottomOverflow,
                $"'{lastAbility.name}' is drawn {overflow:F0} px below the bottom of the wood " +
                $"panel that is supposed to contain it, so the ultimate's plate hangs outside " +
                $"the frame. This is the fault in reported/13.png. The row heights below say " +
                $"which box is pushing the column past the panel.\n{dump}");
        }

        /// <summary>
        /// How far the last ability row may sit below the panel's inner edge.
        ///
        /// ⚠️ IT IS NOT ZERO, AND THE SLACK IS THE PANEL'S OWN BORDER RATHER THAN TOLERANCE FOR
        /// A BUG. `ConfigPanel` is a nine-patch wood box whose sliced border is a few pixels of
        /// frame, and the column is laid out against the padded content rect inside it, so a
        /// correctly fitted column's last row sits a little inside the panel's outer corners
        /// and can round to a pixel or two the other way. The reported overflow was the whole
        /// ultimate plate, tens of pixels, so this fails on the real thing and passes on the
        /// rounding, which is the same standard <see cref="MaxSlack"/> is set to.
        /// </summary>
        private const float MaxBottomOverflow = 4.0f;

        /// <summary>
        /// How much taller than its text the tagline's box may be.
        ///
        /// ⚠️ IT IS ONE LINE OF SLACK, WHICH IS WHAT `HeroTaglineHeight`'s 1.35 FACTOR BUYS
        /// against a line that actually measures about 1.16. The reported fault was 50 px, so
        /// this fails on the real thing and passes on the rounding.
        /// </summary>
        private const float MaxSlack = 28.0f;

        /// <summary>World-space top edge, in the canvas's own units, y growing upward.</summary>
        private static float TopOf(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return Mathf.Max(corners[1].y, corners[2].y);
        }

        private static float BottomOf(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return Mathf.Min(corners[0].y, corners[3].y);
        }

        /// <summary>
        /// Every rect in the column with the three numbers that decide its height, so a failure
        /// names the box rather than inviting another guess at it.
        /// </summary>
        private static string Dump(GameObject root, float scale)
        {
            if (root == null) return "   (no ConfigPanel found)";

            var sb = new StringBuilder();

            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
            {
                var corners = new Vector3[4];
                rt.GetWorldCorners(corners);

                // ⚠️ THE RAW COMPONENT VALUES AS WELL AS THE RESOLVED ONES. `LayoutUtility`
                // answers "what won"; these answer "what was asked for", and the whole reason
                // this probe exists is that three passes could not tell those apart from the
                // source. A `LayoutElement` written at 46 that resolves to 96 is a completely
                // different bug from one that was never written at all.
                string asked = "";
                if (rt.TryGetComponent<LayoutElement>(out var le))
                    asked += $" LE(on={le.isActiveAndEnabled},min={le.minHeight:F0}," +
                             $"pref={le.preferredHeight:F0},prio={le.layoutPriority})";
                if (rt.TryGetComponent<ContentSizeFitter>(out var fit))
                    asked += $" CSF(on={fit.isActiveAndEnabled},h={fit.horizontalFit},v={fit.verticalFit})";

                sb.AppendLine(
                    $"   {rt.name,-20} h={rt.rect.height:F0} top={corners[1].y / scale:F0} " +
                    $"bottom={corners[0].y / scale:F0} " +
                    $"min={LayoutUtility.GetMinHeight(rt):F0} " +
                    $"pref={LayoutUtility.GetPreferredHeight(rt):F0} " +
                    $"flex={LayoutUtility.GetFlexibleHeight(rt):F0}{asked}");
            }

            return sb.ToString();
        }

        private static GameObject Find(string name)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
                    if (rt.name == name) return rt.gameObject;
            }

            return null;
        }

        /// <summary>
        /// ⚠️ SCOPED TO THE PANEL, BECAUSE `MatchSetup` HAS ITS OWN `Rows` AND IT COMES FIRST.
        /// The first run of this probe dumped the setup screen's Map / Mode / Difficulty column
        /// and reported it as the hero picker's, which would have sent the next reader hunting a
        /// gap in the wrong screen.
        /// </summary>
        private static GameObject FindUnder(GameObject parent, string name)
        {
            if (parent == null) return null;

            foreach (var rt in parent.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == name) return rt.gameObject;

            return null;
        }
    }
}
