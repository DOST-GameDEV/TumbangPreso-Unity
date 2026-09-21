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
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code. `PlayModeWorld.Reset` has the
        /// mechanism and why BOTH hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        /// <summary>
        /// The most paper that may sit between the description and the first control under it.
        ///
        /// ⚠️ IT IS THE CARD'S OWN SPACING PLUS A LINE OF SLACK, NOT A ROUND NUMBER. It was
        /// 24 against `Rows`' authored `m_Spacing: 10` on the converted column; the painted card
        /// spaces the description from the links by **exactly 24** (`y += 24` in
        /// `TumpPickerView.LateUpdate`), so a bound OF 24 would be the design's own number with
        /// no room for a rounding and would fail on a healthy screen. 32 is that spacing plus a
        /// descender, and it still fails the 50-plus band that was reported three times.
        /// </summary>
        private const float MaxGap = 32.0f;

        [UnityTest]
        public IEnumerator TheHeroPickerHasNoDeadBandAboveTheAbilityRows()
        {
            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            yield return new WaitForSecondsRealtime(1.0f);

            // The hero variant is the one with the ability rows in it; Classic's trait meters
            // are a different, simpler column and cannot show this fault.
            SceneFlow.SelectedMode = Core.GameMode.HeroStrike;

            var panel = Find("CharacterSelectPanel");
            Assert.IsNotNull(panel, "MatchSetup has no CharacterSelectPanel to open");
            panel.SetActive(true);

            for (int i = 0; i < 6; i++) yield return null;
            yield return new WaitForSecondsRealtime(0.5f);

            // ⚠️⚠️ THE COLUMN THIS PROBE WAS WRITTEN ABOUT IS GONE AND ITS THREE CLAIMS ARE
            // NOT. `ConfigPanel` held a `TraitRows` list of `AbilityRow_n` plates under a
            // `TaglineLabel`; the painted picker has one READING CARD,
            // `CollectionReadingCard`, carrying `SelectedName`, `CharacterOrigin`,
            // `Description` and the two links under them, and `TumpPickerView.LateUpdate` sizes
            // every one of those boxes off its own `preferredHeight` rather than reserving a
            // fixed height. **That is the fix this probe was written to hold**, so the three
            // measurements move onto it rather than being retired with the column:
            //
            //   1. the gap between the hero's description and the first control under it,
            //   2. the empty room reserved INSIDE the description's own box,
            //   3. whether the last control is drawn below the card holding it.
            //
            // ⚠️ AND MEASUREMENT 2 IS THE ONE THAT CAN STILL FAIL FOR A REAL REASON, which
            // is why it is kept rather than trimmed to the two geometric ones. The card is laid
            // out in `LateUpdate` behind an `if(!_detailsDirty ...) return`, so a refresh that
            // never sets that flag leaves every box at the authored height it was built with:
            // `Description` is authored 210 units tall around about 70 units of text.
            var card = Find("CollectionReadingCard");
            var description = Find("Description");
            var door = Find("TumpSkills");

            Assert.IsNotNull(card, "the hero picker has no CollectionReadingCard.");
            Assert.IsNotNull(description, "no Description on the picker's reading card.");
            Assert.IsNotNull(door,
                "the hero picker has no SKILLS door under the description, so there is nothing "
                + "for the description to be spaced against and no way to read the kit.\n"
                + Dump(card, 1.0f));

            // ⚠️⚠️ MEASURED IN THE CANVAS'S OWN 1920x1080 UNITS, NOT IN SCREEN PIXELS. The
            // batch runner renders at whatever window size it feels like, so a raw world-corner
            // gap is a different number on every machine and cannot be held against a layout
            // constant authored in reference space.
            var canvas = card.GetComponentInParent<Canvas>();
            float scale = canvas != null && canvas.scaleFactor > 0.0001f ? canvas.scaleFactor : 1.0f;

            float descriptionBottom = BottomOf(description.GetComponent<RectTransform>());
            float doorTop = TopOf(door.GetComponent<RectTransform>());
            float gap = (descriptionBottom - doorTop) / scale;

            string dump = Dump(card, scale);

            var descriptionRt = description.GetComponent<RectTransform>();
            var descriptionText = description.GetComponent<Text>();
            float slack = descriptionRt.rect.height - descriptionText.preferredHeight;

            Debug.Log($"[Picker] scaleFactor={scale:F3}  gap={gap:F1}  description box=" +
                      $"{descriptionRt.rect.height:F0} text={descriptionText.preferredHeight:F0} " +
                      $"slack={slack:F0}\n{dump}");

            Assert.LessOrEqual(gap, MaxGap,
                $"{gap:F0} px of empty paper sits between the hero's description and the SKILLS " +
                $"door under it. TumpPickerView.LateUpdate spaces them by 24 units; anything " +
                $"much past that is a box reserving height its text does not use.\n{dump}");

            Assert.LessOrEqual(slack, MaxSlack,
                $"the description's box is {descriptionRt.rect.height:F0} px around " +
                $"{descriptionText.preferredHeight:F0} px of text, so {slack:F0} px of empty " +
                $"paper is reserved under the hero's description. LateUpdate writes " +
                $"Max(65, preferredHeight + 20) here, so a box far past that means the layout " +
                $"pass did not run at all.\n{dump}");

            // ---- THE BOTTOM OF THE CARD ----------------------------------------------------
            //
            // ⚠️⚠️ THIS PROBE WAS GREEN WHILE THE ULTIMATE'S PLATE DREW OUTSIDE THE WOOD, AND
            // THAT IS WHY THIS BLOCK EXISTS. 🧑 2026-08-29 with a screenshot of Dante's picker,
            // `docs/reports/2026-08-29/reported/13.png`: *"fix hud here it overflows"*. The fix
            // shipped as § 79.6 and this file did not change, so nothing here would have caught
            // it and nothing here would catch it coming back.
            //
            // ⚠️ THE TWO ASSERTIONS ABOVE CANNOT SEE IT, AND THAT IS STRUCTURAL RATHER THAN AN
            // OVERSIGHT. Both measure INSIDE the card: one the gap between two rows, the other
            // one box against its own text. A card whose rows are each correctly sized and
            // correctly spaced can still be taller than the paper holding it, and every
            // measurement between its own children stays healthy while it runs off the bottom.
            // The only thing that can see it is a child edge against the PARENT's edge.
            var story = Find("MeetCharacter");
            var last = story != null && story.activeInHierarchy ? story : door;

            float lastBottom = BottomOf(last.GetComponent<RectTransform>());
            float cardBottom = BottomOf(card.GetComponent<RectTransform>());

            // Positive means the control's bottom edge is BELOW the card's, in canvas units.
            float overflow = (cardBottom - lastBottom) / scale;

            Debug.Log($"[Picker] lastRow={last.name} overflow={overflow:F1}");

            Assert.LessOrEqual(overflow, MaxBottomOverflow,
                $"'{last.name}' is drawn {overflow:F0} px below the bottom of the reading card " +
                $"that is supposed to contain it, so the control hangs outside the paper. This " +
                $"is the fault in reported/13.png one screen later. The row heights below say " +
                $"which box is pushing the card past its own edge.\n{dump}");
        }

        /// <summary>
        /// How far the last control on the reading card may sit below the card's edge.
        ///
        /// ⚠️ IT IS NOT ZERO, AND THE SLACK IS THE CARD'S OWN BORDER RATHER THAN TOLERANCE
        /// FOR A BUG. `CollectionReadingCard` is an `OwnerUiPaper` whose edge is a few units of
        /// drawn paper, and `LateUpdate` lays the controls out against the height it just
        /// computed, so a correctly fitted card's last control sits a little inside its outer
        /// corners and can round to a unit or two the other way. The reported overflow was a
        /// whole plate, tens of units, so this fails on the real thing and passes on the
        /// rounding, which is the same standard <see cref="MaxSlack"/> is set to.
        /// </summary>
        private const float MaxBottomOverflow = 4.0f;

        /// <summary>
        /// How much taller than its text the description's box may be.
        ///
        /// ⚠️ IT IS THE CARD'S OWN PADDING PLUS A LINE OF SLACK. `TumpPickerView.LateUpdate`
        /// writes `Max(65, preferredHeight + 20)` for this box, so 20 units is what the design
        /// asks for and 28 leaves room for a descender and a rounding. The authored height the
        /// layout pass is supposed to replace is 210 units around about 70 of text, so this
        /// fails on a layout pass that never ran and passes on one that did.
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
            if (root == null) return "   (no reading card found)";

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

    }
}
