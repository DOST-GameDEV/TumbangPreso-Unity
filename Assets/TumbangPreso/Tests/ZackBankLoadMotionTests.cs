using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class ZackBankLoadMotionTests
    {
        [Test] public void BankLoadHasItsOwnActionsAndKeepsImmediateEightSecondMechanics()
        {
            var skill = new ZackHeroKit().AttackingSkill;
            Assert.AreEqual("hero-zack-bankshot", skill.CastAction);
            Assert.AreEqual("bank-load", skill.ViewmodelAction);
            Assert.AreEqual(35f, skill.Cooldown); Assert.AreEqual(8f, skill.Duration);
            Assert.AreEqual(0f, skill.Windup);
        }

        [Test] public void ShippingRosterContainsTheLoadAndRetainsTheOldClips()
        {
            var roster = Resources.Load<RosterEntryAsset>("Roster/person_zack");
            var clip = roster.Clips.Single(c => c != null && c.name == "hero-zack-bankshot");
            Assert.That(clip.length, Is.InRange(.63f, .65f));
            Assert.GreaterOrEqual(roster.Clips.Length, 38);
            foreach (string previous in new[] { "hero-zack-charge", "hero-zack-circuit", "hero-zack-sprint", "hero-zack-summon" })
                Assert.IsTrue(roster.Clips.Any(c => c != null && c.name == previous), previous);
            var model = Object.Instantiate(roster.Model);
            try
            {
                var arm = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == "arm-left");
                clip.SampleAnimation(model, 0); var neutral = arm.localRotation;
                clip.SampleAnimation(model, .28f);
                Assert.Greater(Quaternion.Angle(neutral, arm.localRotation), 40f);
                clip.SampleAnimation(model, clip.length);
                Assert.Less(Quaternion.Angle(neutral, arm.localRotation), .1f);
            }
            finally { Object.DestroyImmediate(model); }
        }

        [Test] public void OwnerLoadUsesItsOwnClipAndTwoHandPathInsteadOfRecall()
        {
            var go = new GameObject("Bank load owner dispatch"); go.SetActive(false);
            try
            {
                var arms = go.AddComponent<ViewmodelArms>();
                const BindingFlags instance = BindingFlags.NonPublic | BindingFlags.Instance;
                const BindingFlags shared = BindingFlags.NonPublic | BindingFlags.Static;
                var field = typeof(ViewmodelArms).GetField("_clip", instance);
                Assert.IsTrue(arms.PlayAction("overcharge")); var recall = field.GetValue(arms);
                Assert.IsTrue(arms.PlayAction("bank-load"));
                Assert.AreNotSame(recall, field.GetValue(arms));
                Assert.AreSame(typeof(ViewmodelArms).GetField("BankLoadClip", shared).GetValue(null), field.GetValue(arms));
                var paths = (System.Collections.IDictionary)typeof(ViewmodelArms).GetField("CastPaths", shared).GetValue(null);
                Assert.IsTrue(paths.Contains("bank-load"));
                Assert.AreNotSame(paths["overcharge"], paths["bank-load"]);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
