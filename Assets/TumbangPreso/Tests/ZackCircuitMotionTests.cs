using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class ZackCircuitMotionTests
    {
        [Test] public void CircuitUsesDistinctBodyAndOwnerActionsWithoutRetuning()
        {
            var skill = new ZackHeroKit().DefendingSkill;
            Assert.AreEqual("hero-zack-circuit", skill.CastAction);
            Assert.AreEqual("closed-circuit", skill.ViewmodelAction);
            Assert.AreEqual(35f, skill.Cooldown);
        }

        [Test] public void ShippingRosterContainsAnAnimatedAcquisitionAndPreservesOldClips()
        {
            var roster = Resources.Load<RosterEntryAsset>("Roster/person_zack");
            var clip = roster.Clips.Single(c => c != null && c.name == "hero-zack-circuit");
            Assert.That(clip.length, Is.InRange(.63f, .65f));
            Assert.GreaterOrEqual(roster.Clips.Length, 37);
            foreach (string old in new[] { "hero-zack-charge", "hero-zack-sprint", "hero-zack-summon" })
                Assert.IsTrue(roster.Clips.Any(c => c != null && c.name == old), old);
            var model = Object.Instantiate(roster.Model);
            try
            {
                var arm = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == "arm-left");
                clip.SampleAnimation(model, 0); var neutral = arm.localRotation;
                clip.SampleAnimation(model, .18f);
                Assert.Greater(Quaternion.Angle(neutral, arm.localRotation), 45f);
                clip.SampleAnimation(model, clip.length);
                Assert.Less(Quaternion.Angle(neutral, arm.localRotation), .1f);
            }
            finally { Object.DestroyImmediate(model); }
        }

        [Test] public void OwnerDispatcherResolvesTheDedicatedGestureInsteadOfThrust()
        {
            var go = new GameObject("Circuit owner dispatch probe"); go.SetActive(false);
            try
            {
                var arms = go.AddComponent<ViewmodelArms>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var field = typeof(ViewmodelArms).GetField("_clip", flags);
                Assert.IsTrue(arms.PlayAction("cast")); var thrust = field.GetValue(arms);
                Assert.IsTrue(arms.PlayAction("closed-circuit")); var circuit = field.GetValue(arms);
                Assert.IsNotNull(circuit); Assert.AreNotSame(thrust, circuit);
                Assert.AreSame(typeof(ViewmodelArms).GetField("ClosedCircuitClip",
                    BindingFlags.NonPublic | BindingFlags.Static).GetValue(null), circuit);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
