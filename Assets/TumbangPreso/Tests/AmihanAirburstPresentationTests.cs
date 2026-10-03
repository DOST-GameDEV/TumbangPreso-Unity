using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// AIRBURST presentation timing (`docs/reports/amihan-presentation-2026-10-02`). The body clip her roster entry
    /// ships, and her first-person hands, must release ON the gameplay release (`AmihanRules.StormSurgeGatherSeconds`),
    /// and the body must start in the pose the shared cutscene ends on. The previous baked clip and first-person path
    /// shoved at 2.5 s, a full second after the host had already thrown everyone in the fan.
    /// </summary>
    public sealed class AmihanAirburstPresentationTests
    {
        private const string StormPath = "Assets/TumbangPreso/Art/characters/amihan-motion/hero-amihan-storm.anim";
        private const float Frame = 1.0f / 60.0f;

        private static AnimationClip Shipped()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(StormPath);
            Assert.IsNotNull(clip, "The baked Airburst clip is missing.");
            return clip;
        }

        private static AnimationCurve Curve(AnimationClip clip, string bone, string axis)
        {
            var binding = AnimationUtility.GetCurveBindings(clip)
                .Single(b => (b.path == bone || b.path.EndsWith("/" + bone)) && b.propertyName == "localEulerAnglesRaw." + axis);
            return AnimationUtility.GetEditorCurve(clip, binding);
        }

        [Test]
        public void TheCheckedClipIsTheOneHerRosterEntryShips()
        {
            var art = RosterBook.Load().FindPersonArt("amihan");
            Assert.IsNotNull(art);
            Assert.IsTrue(art.Clips.Contains(Shipped()), "person_amihan must reference the baked hero-amihan-storm asset.");
        }

        [Test]
        public void TheBodyDrivesBothPalmsOnTheGameplayRelease()
        {
            var clip = Shipped();
            float release = AmihanRules.StormSurgeGatherSeconds;
            // Right arm pitch: most negative is the furthest forward drive.
            var pitch = Curve(clip, "arm-right", "x");
            float deepest = float.MaxValue, first = -1;
            for (float t = 0; t <= clip.length; t += Frame) deepest = Mathf.Min(deepest, pitch.Evaluate(t));
            for (float t = 0; t <= clip.length; t += Frame)
                if (pitch.Evaluate(t) <= deepest + 1.0f) { first = t; break; }
            Assert.That(first, Is.InRange(release - 2 * Frame, release + Frame),
                "The full forward drive must land on the gameplay release, not before or after it.");
            // A quarter second before, the hands are still drawn back at the hip, not already pushed.
            Assert.Greater(pitch.Evaluate(release - .25f), deepest + 45.0f, "The body must not spend its release pose during the dodge window.");
            Assert.Less(clip.length, release + 0.8f, "A long tail after the release would fight her running.");
        }

        [Test]
        public void TheBodyStartsInTheCutscenesLastPose()
        {
            var clip = Shipped();
            var performance = UltimatePerformance.For("amihan");
            Assert.IsNotNull(performance);
            Assert.AreEqual(5.6f, performance.Seconds, 1e-4f, "The shared phase derives its boundary from this length.");
            var last = performance.Keys[performance.Keys.Count - 1];
            void Same(string bone, Vector3 intro)
            {
                for (int axis = 0; axis < 3; axis++)
                {
                    float live = Curve(clip, bone, "xyz"[axis].ToString()).Evaluate(0);
                    Assert.AreEqual(0, Mathf.DeltaAngle(intro[axis], live), .5f, bone + " axis " + axis);
                }
            }
            Same("torso", last.Torso); Same("head", last.Head);
            Same("arm-left", last.ArmLeft); Same("arm-right", last.ArmRight);
            Same("leg-left", last.LegLeft); Same("leg-right", last.LegRight);
        }

        [Test]
        public void TheFirstPersonHandsDriveOnTheGameplayRelease()
        {
            float release = AmihanRules.StormSurgeGatherSeconds;
            var arms = typeof(CameraSystem.ViewmodelArms);
            var paths = (IDictionary)arms.GetField("CastPaths", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var path = paths["storm-call"];
            float contact = (float)path.GetType().GetField("Contact").GetValue(path);
            Assert.AreEqual(release, contact, 1e-4f, "The storm-call path's contact key is the release.");
            var keys = (System.Array)arms.GetField("StormCallClip", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            float deepest = float.MaxValue, at = -1;
            foreach (var key in keys)
            {
                float t = (float)key.GetType().GetField("T").GetValue(key);
                var euler = (Vector3)key.GetType().GetField("Godot").GetValue(key);
                if (euler.x < deepest) { deepest = euler.x; at = t; }
            }
            Assert.AreEqual(release, at, 1e-4f, "The first-person shove must be the release key.");
        }
    }
}
