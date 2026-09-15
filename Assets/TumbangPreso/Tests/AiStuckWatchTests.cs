using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// C1 regression: a bot pressing into geometry is detected from its resolved position, so a
    /// wished velocity can no longer hide it. `AiStuckWatch` carries the Ilalim trace.
    /// </summary>
    public sealed class AiStuckWatchTests
    {
        private const float Frame = 1.0f / 60.0f;

        private static int FramesUntilTriggered(AiStuckWatch watch, System.Func<int, Vector3> at,
                                                System.Func<int, bool> driving, int limit)
        {
            for (int i = 0; i < limit; i++)
                if (watch.Step(at(i), driving(i), Frame)) return i;
            return -1;
        }

        [Test]
        public void ABodyPinnedInPlaceWhileDrivingIsDetected()
        {
            // The traced case: position unchanged at (-3.34, 0.08, 9.90) while pressing left.
            var pinned = new Vector3(-3.34f, 0.08f, 9.90f);
            int frame = FramesUntilTriggered(new AiStuckWatch(), _ => pinned, _ => true, 600);

            Assert.GreaterOrEqual(frame, 0, "A pinned, driving bot was never reported stuck.");
            Assert.LessOrEqual(frame * Frame, AiTuning.StuckTrigger + AiStuckWatch.Window * 2.0f,
                "Detection should land within the trigger plus a measurement window.");
        }

        [Test]
        public void ABodyWalkingAtOrdinarySpeedIsNeverStuck()
        {
            float step = Balance.Speed * Frame;
            int frame = FramesUntilTriggered(new AiStuckWatch(), i => new Vector3(i * step, 0, 0), _ => true, 1200);
            Assert.AreEqual(-1, frame);
        }

        [Test]
        public void FramesWithoutAPhysicsStepDoNotFakeAStall()
        {
            // 60 fps against a 0.02 s physics step: one frame in six carries no movement.
            float step = Balance.Speed * 0.02f;
            var x = 0.0f;
            int frame = FramesUntilTriggered(new AiStuckWatch(), i => { if (i % 6 != 5) x += step; return new Vector3(x, 0, 0); },
                                             _ => true, 1200);
            Assert.AreEqual(-1, frame);
        }

        [Test]
        public void StandingStillOnPurposeIsNeverStuck()
        {
            int frame = FramesUntilTriggered(new AiStuckWatch(), _ => Vector3.zero, _ => false, 1200);
            Assert.AreEqual(-1, frame);
        }

        [Test]
        public void ResetClearsAnAccumulatedStall()
        {
            var watch = new AiStuckWatch();
            for (int i = 0; i < 50; i++) watch.Step(Vector3.zero, true, Frame);
            Assert.Greater(watch.StuckSeconds, 0.0f);
            watch.Reset();
            Assert.AreEqual(0.0f, watch.StuckSeconds);
        }
    }
}
