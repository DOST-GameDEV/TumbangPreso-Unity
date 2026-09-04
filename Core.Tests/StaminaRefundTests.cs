using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// THE REVERSE OF A SPEND, WHICH IS THE ONLY THING `Stamina.Refund` MAY BE.
    ///
    /// ⚠️⚠️ IT EXISTS FOR ONE CALLER AND THESE TESTS ARE WHAT KEEP IT THAT NARROW.
    /// `CombatVerbs.RollBackRefusedVerb` calls it when the host refused a shove this peer had
    /// already paid for, which is `docs/TODO.md` § 135.2. The bar is escape distance
    /// (`CLAUDE.md` § 4: *the real price of a shove is the sprint*), so a refund that could be
    /// reached any other way, or that gave back more than was taken, is a way to buy escape
    /// distance for free.
    ///
    /// ⚠️⚠️ THE FATIGUE HALF IS THE PART THAT IS NOT OBVIOUS AND IS WHY THIS FILE EXISTS
    /// RATHER THAN ONE MORE ASSERT IN `BalanceTests`. `Spend` calls `EnterFatigue` when a cost
    /// empties the bar, so a refund that returned the points and left the lockout running would
    /// give back the cheap half of the price and keep the expensive one.
    /// </summary>
    public class StaminaRefundTests
    {
        private const int Places = 3;

        /// <summary>The plain case: what was taken comes back, exactly.</summary>
        [Fact]
        public void ARefundReturnsExactlyWhatTheSpendTook()
        {
            var s = new Stamina();
            float before = s.Current;

            Assert.True(s.Spend(Balance.ShoveStaminaCost));
            Assert.Equal(before - Balance.ShoveStaminaCost, s.Current, Places);

            s.Refund(Balance.ShoveStaminaCost);
            Assert.Equal(before, s.Current, Places);
        }

        /// <summary>
        /// ⚠️⚠️ THE CASE THE WHOLE METHOD IS FOR. A shove that empties the bar starts the
        /// fatigue lockout, and a refused shove must not leave the player locked out for a dash
        /// the host never ran.
        /// </summary>
        [Fact]
        public void ARefundClearsTheFatigueThatTheSpendItReversesStarted()
        {
            var s = new Stamina();

            // Drain to just inside one shove of empty, so the shove is what tips it over.
            const float dt = 1.0f / 60.0f;
            int guard = 0;
            while (s.Current > Balance.ShoveStaminaCost && guard++ < 5000)
                s.Step(dt, moving: true, sprintHeld: true);

            Assert.False(s.IsFatigued);
            Assert.True(s.Spend(s.Current));
            Assert.True(s.IsFatigued);
            Assert.Equal(0.0f, s.Current, Places);

            s.Refund(Balance.ShoveStaminaCost);

            Assert.False(s.IsFatigued);
            Assert.Equal(Balance.ShoveStaminaCost, s.Current, Places);
        }

        /// <summary>
        /// ⚠️ THE SPEED PENALTY IS ON THE ZONE STACK, NOT ON A MULTIPLIER, so clearing the
        /// lockout has to leave the stack the width it was or the player keeps the slow walk
        /// with nothing on screen saying why. `EnterFatigue` pushes; the refund must pop.
        /// </summary>
        [Fact]
        public void ClearingFatigueOnARefundAlsoLeavesTheSpeedStackWhereItStarted()
        {
            var zones = new SpeedZoneStack();
            var s = new Stamina(zones);
            int widthAtRest = zones.Count;

            const float dt = 1.0f / 60.0f;
            int guard = 0;
            while (s.Current > Balance.ShoveStaminaCost && guard++ < 5000)
                s.Step(dt, moving: true, sprintHeld: true);

            Assert.True(s.Spend(s.Current));
            Assert.True(s.IsFatigued);
            Assert.Equal(widthAtRest + 1, zones.Count);

            s.Refund(Balance.ShoveStaminaCost);

            Assert.Equal(widthAtRest, zones.Count);
            Assert.Equal(1.0f, zones.Value, Places);
        }

        /// <summary>
        /// ⚠️ A REFUND THAT ARRIVES AFTER THE BAR REGENERATED IS A RACE, NOT A CALLER BUG, and
        /// the answer to it is a full bar rather than an overfull one. On a 600 ms link the
        /// refusal can land a long way behind the press.
        /// </summary>
        [Fact]
        public void ARefundNeverPushesTheBarOverTheMaximum()
        {
            var s = new Stamina();
            Assert.True(s.Spend(Balance.ShoveStaminaCost));

            s.Refund(Balance.ShoveStaminaCost);
            s.Refund(Balance.ShoveStaminaCost);
            s.Refund(Balance.StaminaMax);

            Assert.Equal(Balance.StaminaMax, s.Current, Places);
        }

        /// <summary>
        /// ⚠️ A REFUND OF NOTHING CHANGES NOTHING, INCLUDING A LIVE LOCKOUT. The guard reads
        /// `amount <= 0`, so a zero or negative refund cannot be used as a fatigue cure with no
        /// spend behind it.
        /// </summary>
        [Fact]
        public void ARefundOfNothingIsNotAFatigueCure()
        {
            var s = DrainedToFatigue();
            Assert.True(s.IsFatigued);
            float current = s.Current;

            s.Refund(0.0f);
            s.Refund(-Balance.StaminaMax);

            Assert.True(s.IsFatigued);
            Assert.Equal(current, s.Current, Places);
        }

        /// <summary>
        /// ⚠️⚠️ THE ONE THAT KEEPS THE METHOD HONEST ABOUT WHAT IT MAY CLEAR. Fatigue that a
        /// SPRINT ran into is not fatigue a refund reverses: the bar is at zero, the refund
        /// refills it, and the lockout that was already running when the spend would have
        /// happened must survive. `Spend` refuses outright while fatigued, so in the real flow
        /// this state cannot have been produced by the shove being reversed.
        /// </summary>
        [Fact]
        public void ASprintIntoFatigueIsStillFatigueTheNextFrame()
        {
            var s = DrainedToFatigue();
            Assert.True(s.IsFatigued);

            // The lockout is live, so the shove that would have been refused was never paid:
            // `Spend` returns false and takes nothing.
            Assert.False(s.Spend(Balance.ShoveStaminaCost));

            // And with nothing taken there is nothing for `RollBackRefusedVerb` to give back,
            // which is why the caller only ever refunds a spend that returned true.
            Assert.Equal(0.0f, s.Current, Places);
            Assert.True(s.IsFatigued);
        }

        private static Stamina DrainedToFatigue()
        {
            var s = new Stamina();
            const float dt = 1.0f / 60.0f;
            int guard = 0;
            while (!s.IsFatigued && guard++ < 2000)
                s.Step(dt, moving: true, sprintHeld: true);
            return s;
        }
    }
}
