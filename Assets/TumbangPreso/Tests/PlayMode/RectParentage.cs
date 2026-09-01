using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// `docs/TODO.md` § 114.14, and it is ONE assertion about parentage rather than a layout
    /// probe.
    ///
    /// ⚠️⚠️ NOTHING IN THIS PROJECT COULD SEE THAT A CARD WAS IN THE WRONG PART OF THE SCREEN,
    /// AND THE SAME ONE-LINE CAUSE HAS SHIPPED THREE TIMES. § 114.13 is the third: `QueueCard.Build`
    /// did `new GameObject("QueueCard")`, which arrives with a plain `Transform`, and nothing in
    /// `Construct` put a `Graphic` on that object, so it never acquired a `RectTransform`. **A
    /// `RectTransform` whose parent is a plain `Transform` has no parent rect to resolve against**,
    /// so every anchor under it resolved against a zero-sized point at the canvas centre, and a
    /// 560-unit amber QUICK MATCH bar anchored 96 units off the BOTTOM edge drew across all four
    /// characters' faces in the middle of the window. `SplashScreen.BuildSurface`'s postage-stamp
    /// logo and `SignInScreen.BuildLogo`'s three-hundred-pixel wordmark are the first two.
    ///
    /// ⚠️⚠️ `QueueCardLayoutProbe` WAS GREEN THROUGH ALL OF IT, and so was every other layout
    /// check here, because they all ask whether a label fits its own box and every row inside that
    /// card fitted the card. 🧑 read it in one glance and no test could.
    ///
    /// ⚠️⚠️ THE QUESTION IS DELIBERATELY NARROW AND MUST STAY NARROW. "Is this in the right place"
    /// is not a question a test can answer; "does this rect have a rect to be placed against" is.
    /// § 114.14 says so in as many words: **do not turn this into a general layout probe.**
    ///
    /// ⚠️ IT ASKS ABOUT `RectTransform`s ONLY, NEVER ABOUT EVERY CHILD OF A CANVAS. A plain
    /// `Transform` under a canvas is legitimate on its own (a holder for a previewed 3D model, a
    /// pooled effect, an audio source); what is never legitimate is a `RectTransform` hanging off
    /// one, because that is the object whose anchors have nothing to resolve against.
    /// </summary>
    public static class RectParentage
    {
        /// <summary>
        /// Every `RectTransform` below <paramref name="root"/> has a `RectTransform` parent.
        ///
        /// <paramref name="where"/> names the surface for the failure message, because "a rect
        /// has no rect parent" is not actionable and "QueueCard/Door under QueueCard" is a
        /// one-line fix.
        /// </summary>
        public static void AssertEveryRectHasARectParent(Transform root, string where)
        {
            Assert.IsNotNull(root, $"{where}: nothing was built, so this proves nothing.");

            var broken = new List<string>();

            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect == root) continue;

                var parent = rect.parent;
                if (parent == null || parent is RectTransform) continue;

                broken.Add($"'{Path(rect)}' is a RectTransform whose parent "
                           + $"'{parent.name}' is a plain Transform");
            }

            if (broken.Count == 0) return;

            var report = new StringBuilder();
            report.AppendLine($"{where}: {broken.Count} rect(s) have no parent rect to resolve "
                              + "against, so every anchor under them is measured from a "
                              + "zero-sized point at the canvas centre. docs/TODO.md § 114.14.");
            report.AppendLine("Add a Graphic to the offending parent, or build it with "
                              + "`new GameObject(name, typeof(RectTransform))`.");
            foreach (string line in broken) report.AppendLine("  " + line);

            Assert.Fail(report.ToString());
        }

        private static string Path(Transform target)
        {
            var parts = new List<string>();
            for (var t = target; t != null; t = t.parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }
    }
}
