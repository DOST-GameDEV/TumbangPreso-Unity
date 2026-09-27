using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class PracticeRangeRulesTests
    {
        [Theory]
        [InlineData(true, false, false, false, false, false, true)]
        [InlineData(false, false, false, false, false, false, false)]
        [InlineData(true, true, false, false, false, false, false)]
        [InlineData(true, false, true, false, false, false, false)]
        [InlineData(true, false, false, true, false, false, false)]
        [InlineData(true, false, false, false, true, false, false)]
        [InlineData(true, false, false, false, false, true, false)]
        public void OnlyAnExplicitLocalRangeAllowsTrainingControls(bool requested, bool transport,
            bool selectedNetwork, bool revoked, bool tutorial, bool observing, bool expected)
        {
            Assert.Equal(expected, PracticeRangeRules.Allowed(requested, transport,
                selectedNetwork, revoked, tutorial, observing));
        }
    }
}
