using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class ThrowGestureContinuityTests
    {
        [Test]
        public void QuickPreparationIsVisibleAndReleaseReturnsToCurrentLocomotion()
        {
            Assert.Greater(Quaternion.Angle(Quaternion.identity,ThrowGesture.Prepare(.04f,0).Right),6,
                "A tenth-second hold should already have a readable gather.");
            var basis=new ThrowGesture.Pose(Vector3.zero,Vector3.zero,new Vector3(-80,0,0),Vector3.zero);
            var start=ThrowGesture.Pose.Apply(basis,ThrowGesture.Prepare(.6f,-.7f));
            var moving=new ThrowGesture.Pose(new Vector3(2,3,0),new Vector3(-2,1,0),new Vector3(24,0,0),new Vector3(-24,0,0));
            Equal(start,ThrowGesture.Release(basis,start,moving,0,-.7f));
            Equal(moving,ThrowGesture.Release(basis,start,moving,ThrowGesture.ReleaseSeconds,-.7f));
        }

        [Test]
        public void ContactAndFollowThroughHaveNoPoseDiscontinuity()
        {
            var basis=new ThrowGesture.Pose(Vector3.zero,Vector3.zero,new Vector3(-80,0,0),Vector3.zero);
            var start=ThrowGesture.Pose.Apply(basis,ThrowGesture.Prepare(1,1));
            foreach(float boundary in new[]{ThrowGesture.ContactSeconds,ThrowGesture.FollowSeconds})
            {
                var a=ThrowGesture.Release(basis,start,ThrowGesture.Rest,boundary-.00001f,1);
                var b=ThrowGesture.Release(basis,start,ThrowGesture.Rest,boundary+.00001f,1);
                Assert.Less(Quaternion.Angle(a.Right,b.Right),.2f);
                Assert.Less(Quaternion.Angle(a.Torso,b.Torso),.2f);
            }
        }

        [Test]
        public void LeftAndRightPektusHaveDistinctPreparationAndWristRelease()
        {
            Assert.Greater(Quaternion.Angle(ThrowGesture.Prepare(.8f,-1).Right,ThrowGesture.Prepare(.8f,1).Right),25);
            Assert.Greater(Quaternion.Angle(ThrowGesture.ReleaseHand(Quaternion.identity,ThrowGesture.ContactSeconds,-1),
                ThrowGesture.ReleaseHand(Quaternion.identity,ThrowGesture.ContactSeconds,1)),25);
        }

        private static void Equal(ThrowGesture.Pose expected,ThrowGesture.Pose actual)
        {
            Assert.Less(Quaternion.Angle(expected.Torso,actual.Torso),.05f);
            Assert.Less(Quaternion.Angle(expected.Head,actual.Head),.05f);
            Assert.Less(Quaternion.Angle(expected.Right,actual.Right),.05f);
            Assert.Less(Quaternion.Angle(expected.Left,actual.Left),.05f);
        }
    }
}
