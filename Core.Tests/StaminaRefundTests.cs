using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>Refund only resources spent by a verb refused by the host.</summary>
    public class StaminaRefundTests
    {
        private const int Places = 3;

        [Fact]
        public void ARefundReturnsExactlyWhatTheSpendTook()
        {
            var s = new Stamina();
            float before = s.Current;
            Assert.True(s.Spend(Balance.SlideStaminaCost));
            Assert.Equal(before - Balance.SlideStaminaCost, s.Current, Places);
            s.Refund(Balance.SlideStaminaCost);
            Assert.Equal(before, s.Current, Places);
        }

        [Fact]
        public void ARefundAfterDepletionAllowsSprintAsSoonAsTheFloorIsMet()
        {
            var s = new Stamina();
            Assert.True(s.Spend(Balance.StaminaMax));
            Assert.False(s.IsFatigued);
            Assert.Equal(0, s.Current);
            s.Refund(Balance.StaminaSprintFloor);
            Assert.Equal(Balance.StaminaSprintFloor, s.Current);
            s.Step(.1f, moving: true, sprintHeld: true);
            Assert.True(s.IsSprinting);
            Assert.Equal(40f, s.Current);
        }

        [Fact]
        public void DepletionAndRefundPreserveExistingSpeedZones()
        {
            var zones = new SpeedZoneStack();
            zones.Enter(.5f);
            var s = new Stamina(zones);
            Assert.True(s.Spend(Balance.StaminaMax));
            Assert.False(s.IsFatigued);
            Assert.Equal(1, zones.Count);
            s.Refund(Balance.SlideStaminaCost);
            Assert.Equal(1, zones.Count);
            Assert.Equal(.5f, zones.Value, Places);
        }

        [Fact]
        public void ARefundNeverPushesTheBarOverTheMaximum()
        {
            var s = new Stamina();
            Assert.True(s.Spend(Balance.SlideStaminaCost));
            s.Refund(Balance.SlideStaminaCost);
            s.Refund(Balance.SlideStaminaCost);
            s.Refund(Balance.StaminaMax);
            Assert.Equal(Balance.StaminaMax, s.Current, Places);
        }

        [Fact]
        public void ARefundOfNothingCannotRefillAnEmptyBar()
        {
            var s = DrainedToEmpty();
            s.Refund(0);
            s.Refund(-Balance.StaminaMax);
            Assert.Equal(0, s.Current);
            Assert.False(s.IsFatigued);
        }

        [Fact]
        public void AnEmptyBarCannotPayForAnotherSlide()
        {
            var s = DrainedToEmpty();
            Assert.False(s.Spend(Balance.SlideStaminaCost));
            Assert.Equal(0, s.Current);
            Assert.False(s.IsFatigued);
        }

        private static Stamina DrainedToEmpty()
        {
            var s = new Stamina();
            const float dt = 1.0f / 60.0f;
            int guard = 0;
            while (s.Current > 0 && guard++ < 2000)
                s.Step(dt, moving: true, sprintHeld: true);
            Assert.Equal(0, s.Current);
            return s;
        }
    }
}
