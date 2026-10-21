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
        public void TheCutsceneDrivesBothPalmsAndPlayResumesOnTheHit()
        {
            // v3.2 (owner: "show the ult actually hitting and knocking abck ppl already in the cutscene"): the drive is in the
            // cutscene, on its own punch, and play resumes on the hit with no live delay.
            Assert.AreEqual(0f, AmihanRules.StormSurgeDelaySeconds, 1e-6f);
            var performance = UltimatePerformance.For("amihan");
            Assert.IsNotNull(performance);
            const float drive = 4.55f;
            Assert.IsTrue(performance.Punches.Any(p => Mathf.Abs(p - drive) < 1e-3f), "The drive is a punch.");
            var key = performance.Keys.First(k => Mathf.Abs(k.Time - drive) < 1e-3f);
            Assert.AreEqual(-104f, key.ArmRight.x, .5f, "Both palms driven forward on the release.");
            Assert.AreEqual(-104f, key.ArmLeft.x, .5f);
            // v6: the hang slows the story clock after the drive; at the hand-back it has advanced exactly the tail.
            Assert.AreEqual(HeroIntroductionScene.AmRealTail, performance.Seconds - drive, 1e-3f, "The scene's hang is timed to this length.");
            Assert.AreEqual(Abilities.AmihanStorm.CutsceneTail, HeroIntroductionScene.AmihanStoryAfterRelease(performance.Seconds - drive), 1e-3f,
                "The live fan picks up at the age the cutscene's last frame drew.");
            Assert.Less(Shipped().length, .6f, "After the hand-back the body only settles.");
        }

        [Test]
        public void TheBodyStartsInTheCutscenesLastPose()
        {
            var clip = Shipped();
            var performance = UltimatePerformance.For("amihan");
            Assert.IsNotNull(performance);
            Assert.AreEqual(5.9f, performance.Seconds, 1e-4f, "The shared phase derives its boundary from this length.");
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
        public void TheFirstPersonHandsOnlyFollowThrough()
        {
            // v3.2: "no need to reshow it in fpp". The storm-call path starts in the drive (contact 0) and settles.
            var arms = typeof(CameraSystem.ViewmodelArms);
            var paths = (IDictionary)arms.GetField("CastPaths", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var path = paths["storm-call"];
            Assert.AreEqual(0f, (float)path.GetType().GetField("Contact").GetValue(path), 1e-4f);
            var keys = (System.Array)arms.GetField("StormCallClip", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            float last = 0;
            foreach (var key in keys) last = Mathf.Max(last, (float)key.GetType().GetField("T").GetValue(key));
            Assert.LessOrEqual(last, .5f + 1e-4f, "First person settles within half a second.");
        }
    }
}
