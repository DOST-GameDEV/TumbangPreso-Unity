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

        // -------------------------------------------------------------------
        // CASTING
        //
        // ⚠️⚠️ THE OUTCOME IS REPORTED, NOT JUST WHETHER IT WORKED, AND THAT IS THE ANTI-CLUNK
        // FIX. These used to return a bare `bool`, so the one caller could not tell "your skill
        // is on cooldown" from "you are stunned and cannot act yet" from "the meter is empty".
        // All three came back false and all three drew NOTHING, which left the player with only
        // one honest reading available to them: the game dropped my input.
        //
        // It had not. Two of the three are refusals the UI now answers with a red tick and a
        // sound, and the third is not a refusal at all: it is buffered and retried. They are
        // different states and they need different answers, so they need different values.
        // `docs/Hero_Strike_UI.md` § 6 has the table.
        // -------------------------------------------------------------------

        /// <summary>What happened when a hero key was pressed.</summary>
        public enum CastOutcome
        {
            /// <summary>It went off. Play the confirm.</summary>
            Cast,

            /// <summary>There is no such power on this hero. Draw nothing at all.</summary>
            Missing,

            /// <summary>Still cooling. A refusal: answer it and clear the buffer.</summary>
            Cooling,

            /// <summary>The meter is not full. A refusal: answer it and clear the buffer.</summary>
            NoCharge,

            /// <summary>
            /// Stunned, staggered, or the round clock is not running yet.
            ///
            /// ⚠️ NOT A REFUSAL. The player asked for something they are allowed to have and
            /// the world was not ready. Hold the press and try again; see § 7 of the design.
            /// </summary>
            CannotAct,
        }

        public virtual CastOutcome CastSkill1(AbilityContext ctx) => Fire(Skill1, ctx);

        public virtual CastOutcome CastSkill2(AbilityContext ctx) => Fire(Skill2, ctx);

        public virtual CastOutcome CastUltimate(AbilityContext ctx)
        {
            if (Ultimate == null) return CastOutcome.Missing;

            // ⚠️ THE ORDER MATTERS AND IT IS "CAN I ACT" LAST. A stunned player with an empty
            // meter is told the meter is empty, which is the fact they can do something about,
            // rather than being told to wait for a cast that would be refused anyway.
            if (!Ultimate.IsReady) return CastOutcome.Cooling;
            if (!PracticeMode && UltimateCharge < UltimateMax) return CastOutcome.NoCharge;
            if (ctx != null && ctx.Motor != null && !ctx.Motor.CanAct()) return CastOutcome.CannotAct;
            if (!Ultimate.CanActivate(ctx)) return CastOutcome.CannotAct;

            Ultimate.Activate(ctx);

            // ⚠️⚠️ A PRACTICE CAST IS FREE AND THAT IS THE POINT. Spending the meter here would
            // mean a player who tested their ultimate in the warm-up starts the round without
            // the one they had banked from last round, which is a punishment for reading the
            // tutorial. Outside practice the meter is spent exactly as before.
            if (!PracticeMode) UltimateCharge = 0.0f;

            return CastOutcome.Cast;
        }

        private static CastOutcome Fire(HeroAbility ability, AbilityContext ctx)
        {
            if (ability == null) return CastOutcome.Missing;

            // ⚠️ A REACTIVATION IS NOT GATED ON THE COOLDOWN. Nemu's Astral Projection is one
            // press out and one press back, and the return trip has to be available for the
            // whole time the decoy is alive even though the ability is very much not "ready".
            if (ability.IsActive && ability.CanReactivate)
            {
                ability.Reactivate(ctx);
                return CastOutcome.Cast;
            }

            if (!ability.IsReady) return CastOutcome.Cooling;
            if (ctx != null && ctx.Motor != null && !ctx.Motor.CanAct()) return CastOutcome.CannotAct;
            if (!ability.CanActivate(ctx)) return CastOutcome.CannotAct;

            ability.Activate(ctx);
            return CastOutcome.Cast;
        }

        // ⚠️ THE THREE `bool` WRAPPERS STAY. `RuntimeLayerTests`, `InputMapAndAbilityTests` and
        // `BotBehaviourProbe` all ask "did it cast" and none of them cares why not; making them
        // spell out an enum comparison would add noise to sixty assertions to say the same
        // thing. They are one line each and cannot drift from the real implementation.

        public bool TryActivateSkill1(AbilityContext ctx) => CastSkill1(ctx) == CastOutcome.Cast;

        public bool TryActivateSkill2(AbilityContext ctx) => CastSkill2(ctx) == CastOutcome.Cast;

        public bool TryActivateUltimate(AbilityContext ctx) => CastUltimate(ctx) == CastOutcome.Cast;

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
