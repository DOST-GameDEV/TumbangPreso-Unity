using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class ToggleControlTests
    {
        [Fact] public void HoldModeFollowsThePhysicalButton()
        {
            var control = new ToggleControl();
            Assert.True(control.Read(true, false));
            Assert.True(control.Read(true, false));
            Assert.False(control.Read(false, false));
        }
        [Fact] public void ToggleModeChangesOnlyOnANewPress()
        {
            var control = new ToggleControl();
            Assert.True(control.Read(true, true));
            Assert.True(control.Read(true, true));
            Assert.True(control.Read(false, true));
            Assert.False(control.Read(true, true));
            Assert.False(control.Read(true, true));
        }
        [Fact] public void LosingControlCancelsTheLatchAndNeedsANewPress()
        {
            var control = new ToggleControl();
            control.Read(true, true);
            Assert.False(control.Read(true, true, false));
            Assert.False(control.Read(true, true));
            Assert.False(control.Read(false, true));
            Assert.True(control.Read(true, true));
        }
        [Fact] public void MenuOrRoundResetCannotCarryASprintAcrossTheBoundary()
        {
            var control = new ToggleControl();
            control.Read(true, true); control.Read(false, true);
            control.Reset();
            Assert.False(control.Read(true, true));
            Assert.False(control.Read(false, true));
            Assert.True(control.Read(true, true));
        }
        [Fact] public void ChangingModesDoesNotInventANewPress()
        {
            var control = new ToggleControl();
            Assert.True(control.Read(true, false));
            Assert.False(control.Read(true, true));
            Assert.False(control.Read(false, true));
            Assert.True(control.Read(true, true));
        }
        [Fact] public void AReleasedButtonCanStartAsSoonAsTheContextBecomesAvailable()
        {
            var control = new ToggleControl();
            Assert.False(control.Read(false, true, false));
            Assert.True(control.Read(true, true, true));
        }
    }
}
