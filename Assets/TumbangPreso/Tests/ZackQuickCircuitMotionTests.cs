using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class ZackQuickCircuitMotionTests
    {
        [Test] public void QuickCircuitBodyLeavesTravelToTheMotorAndSettlesOnce()
        {
            var roster = Resources.Load<RosterEntryAsset>("Roster/person_zack");
            var clip = roster.Clips.Single(c => c != null && c.name == "hero-zack-sprint");
            Assert.That(clip.length, Is.InRange(.63f, .65f));
            var model = Object.Instantiate(roster.Model);
            try
            {
                var bones = model.GetComponentsInChildren<Transform>(true);
                var root = bones.Single(t => t.name == "root");
                var torso = bones.Single(t => t.name == "torso");
                clip.SampleAnimation(model, 0);
                var rest = root.localPosition; var restTorso = torso.localRotation;
                foreach (float t in new[] { .08f, .15f, .28f, .46f, .64f })
                {
                    clip.SampleAnimation(model, t);
                    Assert.AreEqual(rest.x, root.localPosition.x, .001f, "The motor owns sideways travel.");
                    Assert.AreEqual(rest.z, root.localPosition.z, .001f, "A lateral cut must not skate forward visually.");
                }
                Assert.Less(Quaternion.Angle(restTorso, torso.localRotation), .1f);
                clip.SampleAnimation(model, .15f);
                float released = Quaternion.Angle(restTorso, torso.localRotation);
                Assert.Greater(released, 15f, "The release must have a readable body brace.");
                foreach (float t in new[] { .28f, .46f, .64f })
                {
                    clip.SampleAnimation(model, t);
                    float remaining = Quaternion.Angle(restTorso, torso.localRotation);
                    Assert.Less(remaining, released, "One release should settle, not pump through another skate cycle.");
                    released = remaining;
                }
            }
            finally { Object.DestroyImmediate(model); }
        }
    }
}
