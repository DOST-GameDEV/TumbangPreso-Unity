using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Holds the three abilities for a hero (Skill 1, Skill 2, Ultimate) and manages the ultimate meter.
    /// </summary>
    public class HeroKit
    {
        public string HeroId { get; }
        public string HeroName { get; }
        public HeroAbility Skill1 { get; protected set; }
        public HeroAbility Skill2 { get; protected set; }
        public HeroAbility Ultimate { get; protected set; }

        public float UltimateCharge { get; protected set; }
        public const float UltimateMax = 100.0f;
        public float UltimateRatio => Mathf.Clamp01(UltimateCharge / UltimateMax);

        /// <summary>
        /// True whenever the round clock is NOT running: the warm-up before round one, and the
        /// buffer between rounds.
        ///
        /// ⚠️⚠️ IT IS A PRACTICE RANGE, NOT A PAUSE, AND THE DISTINCTION IS THE WHOLE FEATURE.
        /// 🧑 2026-08-23: *"its okay for ult progress to persist after round and into next
        /// rounds, i js want it to pause when the game isnt ongoing... BUt i want ppl to be able
        /// to test skills still and shit during buffer period so maybe during buffer period give
        /// them diff timers"*. Two requirements that pull against each other, and the answer is
        /// to split the economy from the practice:
        ///
        ///  * The BANKED charge is frozen. It does not accrue and casting cannot spend it, so
        ///    warm-up time neither earns an ultimate nor costs one.
        ///  * The ultimate is nevertheless castable, as many times as the player likes, off a
        ///    separate practice counter that exists only while this is true.
        ///  * Skill cooldowns run normally, because the point of practice is learning the real
        ///    timings.
        ///
        /// The alternative, gating the ultimate behind the real meter during warm-up, means the
        /// only players who can ever practise their ultimate are the ones who already banked one
        /// last round. Nobody would ever practise it in round one.
        /// </summary>
        public bool PracticeMode { get; set; }

        /// <summary>
        /// ⚠️ ALWAYS READY IN PRACTICE. The card lights up, the cast works, nothing is spent.
        /// `Ultimate.IsReady` is still consulted so a practice cast cannot re-fire mid-animation.
        /// </summary>
        public bool IsUltimateReady =>
            (PracticeMode || UltimateCharge >= UltimateMax) && (Ultimate == null || Ultimate.IsReady);

        public HeroKit(string heroId, string heroName)
        {
            HeroId = heroId;
            HeroName = heroName;
            UltimateCharge = 0.0f;
        }

        public void AddUltimateCharge(float amount)
        {
            UltimateCharge = Mathf.Clamp(UltimateCharge + amount, 0.0f, UltimateMax);
        }

        public virtual void Tick(AbilityContext ctx, float dt)
        {
            // Objective play is the main source of charge. The slow passive trickle
            // prevents a dry round without rewarding ability spam or hiding.
            //
            // ⚠️⚠️ IT DOES NOT RUN WHILE THE ROUND CLOCK IS STOPPED. This used to trickle on
            // every frame the kit ticked, warm-up and intermission included, which is what
            // produced *"why is ult charging up at ready screen"*. A player who sat through the
            // practice period arrived at the whistle with charge nobody earned, and the longer
            // they waited before pressing ready the more they got. Charge is a reward for
            // objective play; time spent not playing is not objective play.
            if (!PracticeMode && UltimateCharge < UltimateMax)
            {
                AddUltimateCharge(Balance.UltimatePassiveChargePerSecond * dt);
            }

            // ⚠️ COOLDOWNS TICK IN PRACTICE, DELIBERATELY. The point of a practice range is to
            // learn the real rhythm of the kit, and a kit with no cooldowns teaches the wrong
            // one. Only the ultimate ECONOMY is suspended.
            Skill1?.Tick(ctx, dt);
            Skill2?.Tick(ctx, dt);
            Ultimate?.Tick(ctx, dt);
        }

        public virtual bool TryActivateSkill1(AbilityContext ctx)
        {
            if (Skill1 == null) return false;
            if (Skill1.IsActive && Skill1.CanReactivate)
            {
                Skill1.Reactivate(ctx);
                return true;
            }
            if (Skill1.CanActivate(ctx))
            {
                Skill1.Activate(ctx);
                return true;
            }
            return false;
        }

        public virtual bool TryActivateSkill2(AbilityContext ctx)
        {
            if (Skill2 == null) return false;
            if (Skill2.IsActive && Skill2.CanReactivate)
            {
                Skill2.Reactivate(ctx);
                return true;
            }
            if (Skill2.CanActivate(ctx))
            {
                Skill2.Activate(ctx);
                return true;
            }
            return false;
        }

        public virtual bool TryActivateUltimate(AbilityContext ctx)
        {
            if (!IsUltimateReady || Ultimate == null || !Ultimate.CanActivate(ctx)) return false;

            Ultimate.Activate(ctx);

            // ⚠️⚠️ A PRACTICE CAST IS FREE AND THAT IS THE POINT. Spending the meter here would
            // mean a player who tested their ultimate in the warm-up starts the round without
            // the one they had banked from last round, which is a punishment for reading the
            // tutorial. Outside practice the meter is spent exactly as before.
            if (!PracticeMode) UltimateCharge = 0.0f;

            return true;
        }

        public virtual void Reset()
        {
            UltimateCharge = 0.0f;
            Skill1?.Reset();
            Skill2?.Reset();
            Ultimate?.Reset();
        }

        /// <summary>
        /// Round-boundary reset. Charge to zero, cooldowns cleared, and anything still running
        /// ended through `OnEnd` rather than dropped.
        ///
        /// ⚠⚠ THE CHARGE RESET IS THE POINT, AND NOTHING USED TO CALL IT. `Tick` trickles
        /// `Balance.UltimatePassiveChargePerSecond` every frame the kit ticks, practice time
        /// included, and `HeroAbilitySystem.ResetKit` had NO CALL SITES anywhere in `Assets` or
        /// `Packages`. So charge banked during the warm-up, and during round 1, was still
        /// sitting there at the start of round 2. Reported as *"why is ult charging up at ready
        /// screen"*: charging there is fine and deliberate, **never clearing is the bug**.
        /// </summary>
        public virtual void ResetForRound(AbilityContext ctx)
        {
            // ⚠️⚠️ THE CHARGE SURVIVES A ROUND BOUNDARY, ON INSTRUCTION. 🧑 2026-08-23: *"its
            // okay for ult progress to persist after round and into next rounds"*. An earlier
            // pass in this same session zeroed it here; that was the wrong reading of *"make
            // sure it resets each round"*, which was about the warm-up trickle rather than about
            // the bank. `Tick` is where the pause lives now. **Do not add a reset back here.**
            //
            // What DOES clear is everything that is a timer rather than a resource: cooldowns
            // and anything still running. A player must not open a round mid-Carapace or still
            // cooling from a skill they cast in the practice period.
            Skill1?.ResetForRound(ctx);
            Skill2?.ResetForRound(ctx);
            Ultimate?.ResetForRound(ctx);
        }

        /// <summary>
        /// A brand new match. Everything to zero, charge included.
        ///
        /// ⚠️ THIS IS THE ONE PLACE THE BANK IS EMPTIED. A match carries charge across its four
        /// rounds; it does not carry it into the next match.
        /// </summary>
        public virtual void ResetForMatch(AbilityContext ctx)
        {
            UltimateCharge = 0.0f;
            ResetForRound(ctx);
        }
    }
}
