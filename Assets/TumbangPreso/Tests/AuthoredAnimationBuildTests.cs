using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using TumbangPreso.EditorTools;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class AuthoredAnimationBuildTests
    {
        [Test]
        public void MissingRigRepairsBindAndMoveTheActualBones()
        {
            var book = RosterBook.Load(); Assert.IsNotNull(book);
            foreach (string rig in new[] { "team-amihan", "team-paete" })
            {
                var entry = book.People.First(e => e?.Model != null && DanceClip.ResourceName(
                    e.Model.GetComponentInChildren<Animator>()?.transform ?? e.Model.transform) == rig);
                var instance = Object.Instantiate(entry.Model); instance.hideFlags = HideFlags.HideAndDontSave;
                try
                {
                    var root = instance.GetComponentInChildren<Animator>()?.transform ?? instance.transform;
                    foreach (string folder in new[] { SwimmingMotion.Folder, RecoveryMotion.Folder })
                    {
                        var set = GeneratedMotionAssets.For(folder, rig); Assert.IsNotNull(set, folder + "/" + rig);
                        foreach (var clip in set.Clips)
                        {
                            var targets = AnimationUtility.GetCurveBindings(clip).Select(b => root.Find(b.path)).Distinct().ToArray();
                            Assert.IsTrue(targets.All(t => t != null), rig + "/" + clip.name + " has an unbound path.");
                            clip.SampleAnimation(root.gameObject, 0);
                            var positions = targets.Select(t => t.localPosition).ToArray();
                            var rotations = targets.Select(t => t.localRotation).ToArray();
                            clip.SampleAnimation(root.gameObject, clip.length * .35f);
                            Assert.IsTrue(targets.Select((t, i) => Vector3.Distance(t.localPosition, positions[i]) > .0001f
                                || Quaternion.Angle(t.localRotation, rotations[i]) > .01f).Any(moved => moved), rig + "/" + clip.name + " did not move its bones.");
                        }
                    }
                }
                finally { Object.DestroyImmediate(instance); }
            }
        }

        [Test]
        public void NativeBuildPrerequisitesPreserveEveryRetainedAssetByte()
        {
            var files = new[] { SwimmingMotion.Folder, RecoveryMotion.Folder }
                .SelectMany(folder => Directory.GetFiles("Assets/TumbangPreso/Resources/" + folder, "*", SearchOption.AllDirectories)).ToArray();
            Assert.IsNotEmpty(files);
            string Hash(string file) { using var sha = SHA256.Create(); return System.Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(file))); }
            var before = files.ToDictionary(file => file, Hash);
            Assert.IsTrue(AuthoredAnimationBuildCheck.Validate(out int rigs, out string error), error);
            Assert.Greater(rigs, 0);
            foreach (var file in files) Assert.AreEqual(before[file], Hash(file), "A build prerequisite rewrote " + file);
        }

        [Test]
        public void MissingEmptyAndDuplicateClipsRefuseButAnAuthoredCurveIsRetained()
        {
            var set = ScriptableObject.CreateInstance<GeneratedAnimationSet>();
            var clip = new AnimationClip { name = "retained" };
            try
            {
                Assert.IsFalse(AuthoredAnimationBuildCheck.ValidateSet(set, new[] { "retained" }, out _));
                set.Clips = new[] { clip };
                Assert.IsFalse(AuthoredAnimationBuildCheck.ValidateSet(set, new[] { "retained" }, out _));
                var curve = new AnimationCurve(new Keyframe(0, .73f), new Keyframe(1, -.42f));
                clip.SetCurve("root/torso", typeof(Transform), "localPosition.x", curve);
                Assert.IsTrue(AuthoredAnimationBuildCheck.ValidateSet(set, new[] { "retained" }, out _));
                var binding = AnimationUtility.GetCurveBindings(clip).Single(b => b.path == "root/torso" && b.propertyName.EndsWith(".x"));
                Assert.AreEqual(.73f, AnimationUtility.GetEditorCurve(clip, binding).Evaluate(0), .0001f);
                set.Clips = new[] { clip, clip };
                Assert.IsFalse(AuthoredAnimationBuildCheck.ValidateSet(set, new[] { "retained" }, out _));
            }
            finally { Object.DestroyImmediate(set); Object.DestroyImmediate(clip); }
        }
    }
}
