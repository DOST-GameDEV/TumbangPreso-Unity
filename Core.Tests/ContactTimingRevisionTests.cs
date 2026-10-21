using TumbangPreso.Core;
using Xunit;
namespace TumbangPreso.Core.Tests
{
    public sealed class ContactTimingRevisionTests
    {
        [Fact] public void OwnerRevisedContactTimingsAndThrowChargeAreExact()
        {
            Assert.Equal(1.5f, Balance.ChargeFullTime);
            Assert.Equal(1, ThrowRules.ChargeRatio(1.5f));
            Assert.Equal(.25f, Balance.PunchHitCooldown); Assert.Equal(.5f, Balance.PunchCooldown);
            Assert.Equal(7.5f, Balance.ShoveCooldown); Assert.Equal(.5f, Balance.ShoveMissCooldown);
            Assert.Equal(.5f, Balance.LungeChargeTime);
            Assert.Equal(.5f, Combat.LungeCooldownFor(0)); Assert.Equal(2.5f, Combat.LungeCooldownFor(1));
        }
    }
}
