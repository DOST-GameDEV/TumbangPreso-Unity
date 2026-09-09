using System.Linq;
using NUnit.Framework;
using TumbangPreso.EditorTools;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The two claims an animation review in this repository now rests on, asserted rather than
    /// described. `docs/TODO.md` § 151.16 and § 151.20 are the entries.
    ///
    /// ⚠️⚠️ NEITHER OF THESE IS A PICTURE AND NEITHER REPLACES ONE. `ClipMotionStrip` writes the
    /// picture; this file asserts the two things that decide whether the picture is of the right
    /// clip and whether the rig it came off would still fail honestly.
    /// </summary>
    public sealed class AnimationReviewTests
    {
        // -------------------------------------------------------------------
        // § 151.20 · a rig with MORE authored clips is not a rig that lost any
        // -------------------------------------------------------------------

        [Test]
        public void AnExtraAuthoredActionIsNotALostBaseClip()
        {
            var lost = PersonSwapProbe.MissingBaseClips(
                rebuilt: new[] { "idle", "walk", "slide", "hero-sean-dash" },
                baseRig: new[] { "idle", "walk" });

            Assert.IsEmpty(lost,
                "authoring extra actions onto a rig is the work, not a regression: " +
                string.Join(", ", lost));
        }

        [Test]
        public void AMissingBaseClipIsStillALoss()
        {
            var lost = PersonSwapProbe.MissingBaseClips(
                rebuilt: new[] { "idle", "slide", "hero-sean-dash", "hero-sean-ignite" },
                baseRig: new[] { "idle", "walk", "sprint" });

            // ⚠️ THE COUNT IS BIGGER AND THE RIG IS STILL BROKEN, which is exactly the case the
            // old equality check answered backwards: four against three would have passed it.
            Assert.AreEqual(new[] { "sprint", "walk" }, lost,
                "a base clip that disappeared has to fail even when the total went up");
        }

        [Test]
        public void TheRealRebuiltRigPassesAgainstTheRealReferenceRig()
        {
            var rebuilt = ClipNamesOf(PersonSwapProbe.NewModel);
            var reference = ClipNamesOf(PersonSwapProbe.OldModel);

            Assert.IsNotEmpty(rebuilt, PersonSwapProbe.NewModel + " imported no clips at all");
            Assert.IsNotEmpty(reference, PersonSwapProbe.OldModel + " imported no clips at all");

            var lost = PersonSwapProbe.MissingBaseClips(rebuilt, reference);

            Assert.IsEmpty(lost,
                $"{rebuilt.Length} clips against the reference rig's {reference.Length}, missing: " +
                string.Join(", ", lost));

            // The state that broke the old check, asserted so nobody puts an equality back:
            // these two files legitimately hold different numbers of clips.
            Assert.AreNotEqual(reference.Length, rebuilt.Length,
                "if these ever match again this test stops proving anything about the superset rule");
        }

        // -------------------------------------------------------------------
        // § 151.16 · the strip photographs the clip the GAME plays, across its length
        // -------------------------------------------------------------------

        [Test]
        public void SeansSlideResolvesToItsOwnClipRatherThanTheLungeFallback()
        {
            var entry = Resources.Load<RosterBook>("RosterBook").People.First(p => p.Id == "sean");

            string resolved = ClipMotionStrip.ResolveThroughGameChain(
                "slide", entry.Clips, out var chain, out int slot);

            Assert.AreEqual("slide", chain[0], "the chain's first slot is the authored clip");
            Assert.AreEqual("slide", resolved,
                "the game would play '" + resolved + "' on Sean, so that is what a review of " +
                "'slide' would actually be a review of");
            Assert.AreEqual(0, slot,
                "slot " + slot + " is a FALLBACK. `attack-kick-right` is the lunge, and a rig that " +
                "drops to it looks like a dash with nothing logged.");
        }

        [Test]
        public void AMissingSlideStillFallsThroughToTheLunge()
        {
            // ⚠️ THE OTHER HALF, because a resolver that cannot report a fallback cannot warn
            // about one either. `CharacterAnimator`'s chains are silent by design.
            var onlyTheOldSet = new[]
            {
                Clip("idle"), Clip("attack-kick-right"), Clip("attack-melee-right"),
            };

            string resolved = ClipMotionStrip.ResolveThroughGameChain(
                "slide", onlyTheOldSet, out _, out int slot);

            Assert.AreEqual("attack-kick-right", resolved);
            Assert.AreEqual(1, slot);
        }

        [Test]
        public void TheStripSamplesAcrossTheClipRatherThanFrameZero()
        {
            // The retrieval slide's real shape: 0.95 s with contact at 0.14, 0.25 and 0.342.
            float[] times = ClipMotionStrip.SampleTimes(0.95f, 8, new[] { 0.140f, 0.250f, 0.342f });

            Assert.Greater(times.Length, 4, "a strip of four poses is not a review");
            Assert.AreEqual(0.0f, times.First(), 0.001f, "the entry frame is the first thing to judge");
            Assert.AreEqual(0.95f, times.Last(), 0.001f, "the return to rest is the last");

            foreach (float beat in new[] { 0.140f, 0.250f, 0.342f })
            {
                Assert.IsTrue(times.Any(t => Mathf.Abs(t - beat) < 0.001f),
                    "the authored contact beat at " + beat + " s has to be one of the photographs");
            }

            for (int i = 1; i < times.Length; i++)
            {
                Assert.Greater(times[i], times[i - 1], "sample times have to be strictly increasing");
                Assert.GreaterOrEqual(times[i] - times[i - 1], 0.02f,
                    "two poses 20 ms apart are one photograph twice and cost the strip a beat");
            }
        }

        // -------------------------------------------------------------------

        private static string[] ClipNamesOf(string assetPath)
            => AssetDatabase.LoadAllAssetsAtPath(assetPath)
                            .OfType<AnimationClip>()
                            .Where(c => !c.name.StartsWith("__preview"))
                            .Select(c => c.name)
                            .ToArray();

        private static AnimationClip Clip(string name)
        {
            var clip = new AnimationClip { name = name };
            return clip;
        }
    }
}
