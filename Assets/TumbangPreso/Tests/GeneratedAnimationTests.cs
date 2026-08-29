using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// Proves the generated dances are real assets with real curves, and that each rig's set
    /// binds to the person model it was authored for.
    /// </summary>
    public sealed class GeneratedAnimationTests
    {
        [Test]
        public void ThePlayerCarriesOneBakedDancePerRig()
        {
            var book = Resources.Load<RosterBook>("RosterBook");
            Assert.IsNotNull(book);

            foreach (var entry in book.People)
            {
                var animator = entry.Model.GetComponentInChildren<Animator>();
                Transform root = animator != null ? animator.transform : entry.Model.transform;
                string resourceName = DanceClip.ResourceName(root);
                var set = Resources.Load<GeneratedAnimationSet>(
                    DanceClip.ResourceFolder + "/" + resourceName);

                Assert.IsNotNull(set, entry.Id + " has no generated animation set");
                Assert.AreEqual(1, set.Clips.Length, entry.Id + " needs exactly one baked dance");

                var names = new HashSet<string>(set.Clips.Select(c => c.name));
                Assert.IsTrue(names.Contains(DanceClip.ClipName), entry.Id + " has no dance");

                foreach (var clip in set.Clips)
                {
                    Assert.IsFalse(clip.legacy, clip.name + " cannot enter AnimationClipPlayable");
                    Assert.IsNotEmpty(AnimationUtility.GetCurveBindings(clip),
                        clip.name + " is the built-player failure: a valid clip with zero curves");
                }
            }
        }

        [Test]
        public void EveryGeneratedBindingExistsOnEveryRosterPerson()
        {
            var book = Resources.Load<RosterBook>("RosterBook");

            Assert.IsNotNull(book);
            Assert.IsNotEmpty(book.People);

            foreach (var entry in book.People)
            {
                Assert.IsNotNull(entry);
                Assert.IsNotNull(entry.Model, entry.Id);

                var animator = entry.Model.GetComponentInChildren<Animator>();
                Transform root = animator != null ? animator.transform : entry.Model.transform;
                string resourceName = DanceClip.ResourceName(root);
                var set = Resources.Load<GeneratedAnimationSet>(
                    DanceClip.ResourceFolder + "/" + resourceName);

                Assert.IsNotNull(set, entry.Id);

                foreach (var clip in set.Clips)
                {
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                        Assert.IsNotNull(root.Find(binding.path),
                            $"{entry.Id} cannot bind {clip.name} path '{binding.path}'");
                }
            }
        }

        [Test]
        public void TheBakedDanceMovesBothArmsFarFromTheBindPose()
        {
            var book = Resources.Load<RosterBook>("RosterBook");

            Assert.IsNotNull(book);

            foreach (var entry in book.People)
            {
                var instance = Object.Instantiate(entry.Model);

                try
                {
                    Transform left = Find(instance.transform, "arm-left");
                    Transform right = Find(instance.transform, "arm-right");
                    var animator = instance.GetComponentInChildren<Animator>();
                    Transform root = animator != null ? animator.transform : instance.transform;
                    string resourceName = DanceClip.ResourceName(root);
                    var set = Resources.Load<GeneratedAnimationSet>(
                        DanceClip.ResourceFolder + "/" + resourceName);
                    var dance = set.Clips.First(c => c.name == DanceClip.ClipName);
                    Quaternion leftBind = left.localRotation;
                    Quaternion rightBind = right.localRotation;

                    dance.SampleAnimation(instance, 0.50f);

                    float leftMove = Quaternion.Angle(leftBind, left.localRotation);
                    float rightMove = Quaternion.Angle(rightBind, right.localRotation);

                    // At this beat one arm is at 160 degrees and its partner is deliberately
                    // down at 25. Both must move, and the raised arm must be unmistakable.
                    Assert.Greater(leftMove, 20.0f,
                        entry.Id + " dance left arm stayed in the bind pose");
                    Assert.Greater(rightMove, 20.0f,
                        entry.Id + " dance right arm stayed in the bind pose");
                    Assert.Greater(Mathf.Max(leftMove, rightMove), 100.0f,
                        entry.Id + " dance never raised either arm");
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = Find(root.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }
    }
}
