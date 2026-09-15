using NUnit.Framework;
using TumbangPreso.Core;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// C2 regression: a bot taya does not open a lunge charge its own punch will make pointless.
    /// `AiLungeRules` carries the trace. None of these values is a human balance number.
    /// </summary>
    public sealed class AiLungeRulesTests
    {
        private const float Hold = AiTuning.LungeHoldTime;

        [Test]
        public void AReadyPunchThatWillBeInRangeWhenTheHoldEndsRefusesTheCharge()
        {
            // The traced case: charges opened at 1.8 to 2.6 m while walking in at full speed.
            Assert.IsTrue(AiLungeRules.PunchWillArriveFirst(2.5f, 0.0f, Hold, Balance.Speed));
            Assert.IsTrue(AiLungeRules.PunchWillArriveFirst(Balance.PunchRange, 0.0f, Hold, 0.0f));
        }

        [Test]
        public void AVictimTheWalkCannotCatchStillGetsTheLunge()
        {
            // AheadOf already carries the victim's escape, so a runner matching the taya's pace
            // leaves the predicted gap outside the punch and the lunge is the right verb.
            float gapWhenHoldEnds = Balance.PunchRange + 0.3f;
            float reach = gapWhenHoldEnds + Balance.Speed * Hold;
            Assert.IsFalse(AiLungeRules.PunchWillArriveFirst(reach, 0.0f, Hold, Balance.Speed));
        }

        [Test]
        public void APunchStillCoolingPastTheHoldLeavesTheLungeAvailable()
        {
            Assert.IsFalse(AiLungeRules.PunchWillArriveFirst(1.0f, Hold + 0.01f, Hold, Balance.Speed));
            Assert.IsTrue(AiLungeRules.PunchWillArriveFirst(1.0f, Hold, Hold, Balance.Speed));
        }
    }
}
