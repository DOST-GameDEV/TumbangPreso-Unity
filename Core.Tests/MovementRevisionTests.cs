using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class MovementRevisionTests
    {
        [Fact] public void RolesUseTheOwnersAbsoluteWalkAndRunSpeeds()
        {
            Assert.Equal(2.5f, Stamina.MovementSpeed(false, false));
            Assert.Equal(5f, Stamina.MovementSpeed(false, true));
            Assert.Equal(3.75f, Stamina.MovementSpeed(true, false));
            Assert.Equal(7.5f, Stamina.MovementSpeed(true, true));
            foreach (var mode in new[] { GameMode.Classic, GameMode.HeroStrike })
                for (int i = 0; i < Roster.GetPeople(mode).Count; i++)
                    Assert.Equal(1f, Roster.PersonSpeedScale(i, mode));
        }
        [Fact] public void JumpMatchesOneMetreAndHalfSecondWithoutChangingProjectiles()
        {
            Assert.Equal(1f, Balance.JumpVelocity * Balance.JumpVelocity / (2 * Balance.CharacterGravity), 5);
            Assert.Equal(.5f, 2 * Balance.JumpVelocity / Balance.CharacterGravity, 5);
            Assert.Equal(20f, Balance.Gravity);
            Assert.Equal(26f, Balance.MaxFallSpeed);
        }
        [Fact] public void StaminaMatchesTheOwnerValuesAndFatigueDoesNotSlowWalking()
        {
            Assert.Equal(250f, Balance.StaminaMax); Assert.Equal(100f, Balance.StaminaDrainRate);
            Assert.Equal(100f, Balance.StaminaRegenRate); Assert.Equal(1f, Balance.StaminaRegenDelay);
            Assert.Equal(50f, Balance.StaminaSprintFloor); Assert.Equal(2.5f, Balance.FatigueTime);
            var stamina = new Stamina(); Assert.True(stamina.Spend(250));
            Assert.True(stamina.IsFatigued); Assert.Equal(1f, stamina.SpeedZones.Value);
            stamina.Step(1, true, true); Assert.False(stamina.IsSprinting); Assert.Equal(0f, stamina.Current);
        }
        [Fact] public void LungeRecoveryScalesFromTapToFullWhileSlideStaysUnchanged()
        {
            Assert.Equal(.5f, Balance.LungeChargeTime); Assert.Equal(3.5f, Combat.LungeDash(), 4);
            Assert.True(Balance.LungeActiveTime >= Balance.LungeSpeed / Balance.Friction);
            Assert.Equal(.95f, Balance.SlideActiveTime + Balance.SlideRecoveryTime, 4);
            Assert.Equal(.5f, Combat.LungeCooldownFor(0), 4);
            Assert.Equal(.5f, Combat.LungeCooldownFor(Balance.LungeMinPower), 4);
            Assert.Equal(1.5f, Combat.LungeCooldownFor((Balance.LungeMinPower + 1) * .5f), 4);
            Assert.Equal(2.5f, Combat.LungeCooldownFor(1), 4);
            Assert.Equal(0f, Balance.ShoveStaminaCost);
            Assert.Equal(1.75f, Balance.SlideDistance); Assert.Equal(25f, Balance.SlideStaminaCost);
            Assert.Equal(2.45f, Balance.SlideCooldown); Assert.Equal(.35f, Balance.SlideSteerScale);
        }
    }
}
