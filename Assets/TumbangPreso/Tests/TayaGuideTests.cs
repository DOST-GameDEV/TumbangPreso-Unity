using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.UI;

namespace TumbangPreso.Tests
{
    public sealed class TayaGuideTests
    {
        [Test] public void EveryRefusedTouchNamesItsReasonAndALegalOneNamesNone()
        {
            Assert.AreEqual(TayaGuide.NoSlipper, TayaGuide.WhyNoTag(true, false, false, false, true));
            Assert.AreEqual(TayaGuide.OutsideBox, TayaGuide.WhyNoTag(true, false, false, true, false));
            Assert.AreEqual(TayaGuide.CanDown, TayaGuide.WhyNoTag(false, false, false, true, true));
            Assert.AreEqual(TayaGuide.OutOfReach, TayaGuide.WhyNoTag(true, true, false, true, true));
            Assert.IsNull(TayaGuide.WhyNoTag(true, false, false, true, true));
            Assert.IsNull(TayaGuide.WhyNoTag(true, false, true, false, false), "Vulnerable is taggable anywhere, slipper or not");
        }
        [Test] public void TheReplayLandsItsTouchStillMovingThenStopsThenFollowsThrough()
        {
            const float start = 10, contact = 11.6f, end = 11.78f;
            float At(float elapsed) => CatchReconstruction.RecordedTimeAt(start, contact, end, elapsed);
            float previous = At(0);
            for (float e = .01f; e < CatchReconstruction.AnimationDuration; e += .01f)
            {
                float now = At(e);
                Assert.GreaterOrEqual(now + .0001f, previous, "Recorded time never runs backward");
                previous = now;
            }
            float touch = CatchReconstruction.ContactPlayback;
            Assert.AreEqual(contact, At(touch), .001f);
            Assert.Greater((At(touch) - At(touch - .05f)) / .05f, .2f, "The touch must not arrive at a standstill");
            Assert.AreEqual(contact, At(touch + CatchReconstruction.HitStop * .9f), .001f, "The hit stop holds the touch");
            Assert.AreEqual(end, At(CatchReconstruction.AnimationDuration), .001f);
        }
    }
}
