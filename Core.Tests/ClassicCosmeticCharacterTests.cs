using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class ClassicCosmeticCharacterTests
    {
        [Fact]
        public void EveryClassicCharacterHasNeutralGameplayMultipliers()
        {
            for(int i=-1;i<=Roster.ClassicPeople.Count;i++)
            {
                Assert.Equal(1f,Roster.PersonSpeedScale(i,GameMode.Classic));
                Assert.Equal(1f,Roster.PersonPowerScale(i,GameMode.Classic));
                Assert.Equal(1f,Roster.PersonGritScale(i,GameMode.Classic));
                Assert.Equal(1f,Roster.PersonSpeedScale(i));
                Assert.Equal(1f,Roster.PersonPowerScale(i));
                Assert.Equal(1f,Roster.PersonGritScale(i));
            }
        }
    }
}
