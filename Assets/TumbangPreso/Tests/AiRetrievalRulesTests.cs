using NUnit.Framework;
using TumbangPreso.Core;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// C1 regression: the three overrides that let a bot fetch an unsafe slipper must also stop it
    /// deferring to a better-ranked rival. `AiRetrievalRules` carries the trace that found it.
    /// </summary>
    public sealed class AiRetrievalRulesTests
    {
        private const float Patience = 2.4f;
        private static readonly float LateAt = Balance.SlipperUnretrievedWarningTime * 0.5f;

        [Test]
        public void AFreshCautiousBotStillYieldsToTheBetterRun()
        {
            Assert.IsTrue(AiRetrievalRules.MayYieldToBetterRun(0.0f, 0.0f, Patience, true));
            Assert.IsTrue(AiRetrievalRules.MayYieldToBetterRun(LateAt - 0.01f, Patience - 0.01f, Patience, true));
        }

        [Test]
        public void TheTournamentClockOutranksTheOneRunnerRule()
        {
            Assert.IsFalse(AiRetrievalRules.MayYieldToBetterRun(LateAt, 0.0f, Patience, true));
            // The measured traces: seat clocks at 11 s, well past the grace period, still yielding.
            Assert.IsFalse(AiRetrievalRules.MayYieldToBetterRun(11.0f, 1.83f, Patience, true));
        }

        [Test]
        public void AStalledBotStopsWaitingForARivalWhoIsNotRunning()
        {
            Assert.IsFalse(AiRetrievalRules.MayYieldToBetterRun(0.0f, Patience, Patience, true));
            Assert.IsFalse(AiRetrievalRules.MayYieldToBetterRun(0.0f, 10.7f, Patience, true));
        }

        [Test]
        public void ADownedCanMakesEveryRunFreeSoNobodyYields()
        {
            Assert.IsFalse(AiRetrievalRules.MayYieldToBetterRun(0.0f, 0.0f, Patience, false));
        }

        [Test]
        public void LateIsTheSameLineTheSprintAndCautionOverrideUse()
        {
            Assert.IsFalse(AiRetrievalRules.IsLate(LateAt - 0.001f));
            Assert.IsTrue(AiRetrievalRules.IsLate(LateAt));
        }
    }
}
