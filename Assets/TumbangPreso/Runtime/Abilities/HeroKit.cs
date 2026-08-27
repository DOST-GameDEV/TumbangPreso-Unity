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

        /// <summary>
        /// ⚠️⚠️ KEPT AS THE METER'S FULL-SCALE VALUE, NOT AS THE PRICE. It used to be both, and
        /// that is why every hero's ultimate cost the same. <see cref="UltimateCost"/> is the
        /// price now; this is only what "100 per cent" means on the widget.
        ///
        /// It stays a `const` because a dozen tests and the HUD arithmetic read it, and because
        /// a meter whose full scale varies per hero cannot be compared across the four cards on
        /// a spectator's screen.
        /// </summary>
        public const float UltimateMax = 100.0f;

        /// <summary>
        /// What THIS hero's ultimate costs, in the same points <see cref="UltimateCharge"/> is
        /// measured in.
        ///
        /// ⚠️⚠️ IT IS PER HERO BECAUSE THE FIVE ULTIMATES ARE NOT WORTH THE SAME. Until
        /// 2026-08-25 a Thunderstrike that stuns everyone within 4.5 m of your own feet, needs
        /// no aim and has no counterplay cost exactly what a Seance Void costs, which is a zone
        /// that drags and slows and ends no round on its own. One price for five different
        /// powers is a balance lever nobody was pulling.
        ///
        /// The costs and the reasoning behind each are `docs/Hero_Strike_Balance.md` § 3.1.
        /// A kit that does not override this pays the meter's full scale, which is the old
        /// behaviour exactly.
        /// </summary>
        public virtual float UltimateCost => UltimateMax;

        /// <summary>
        /// ⚠️ THE RATIO IS AGAINST THE COST, NOT AGAINST THE SCALE, because it is what the HUD
        /// fills. A player whose ultimate costs 90 must see a full bar at 90, not at 100, or the
        /// widget lies about a power they can already cast. Clamped, so a cheap ultimate simply
        /// reads full early rather than overflowing.
        /// </summary>
        public float UltimateRatio => Mathf.Clamp01(UltimateCharge / Mathf.Max(1.0f, UltimateCost));

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
            (PracticeMode || UltimateCharge >= UltimateCost) && (Ultimate == null || Ultimate.IsReady);

        public HeroKit(string heroId, string heroName)
        {
            HeroId = heroId;
            HeroName = heroName;
            UltimateCharge = 0.0f;
        }

        /// <summary>
        /// ⚠️ CLAMPED TO THE COST, NOT TO THE SCALE. A hero whose ultimate costs 90 stops
        /// accruing at 90; banking 100 for a 90-point power would let a player carry ten points
        /// of head start into the next ultimate, which is a small reward for having already had
        /// one and compounds across a match.
        /// </summary>
        public void AddUltimateCharge(float amount)
        {
            UltimateCharge = Mathf.Clamp(UltimateCharge + amount, 0.0f, UltimateCost);
        }

        /// <summary>
        /// Everything about this kit that has to survive a reconnect, in the order it goes on
        /// the wire. See <see cref="HeroAbility.ApplyNetworkSnapshot"/> for why durations are
        /// not in it.
        ///
        /// ⚠️ SEVEN FLOATS AND TWO INTS PER SEAT, WHICH IS 36 BYTES FOR THE WHOLE MATCH. It rides
        /// the existing world snapshot rather than getting a tick of its own: cooldowns are the
        /// slowest-moving numbers in the game (46 to 62 s), so anything faster than "whenever the
        /// world is resynced" would be spending bandwidth to be precise about a value that barely
        /// changes between packets.
        /// </summary>
        public void ApplyNetworkSnapshot(float ultimateCharge,
                                         float skill1Cooldown, int skill1Charges,
                                         float skill2Cooldown, int skill2Charges,
                                         float ultimateCooldown)
        {
            UltimateCharge = Mathf.Clamp(ultimateCharge, 0.0f, UltimateCost);

            Skill1?.ApplyNetworkSnapshot(skill1Cooldown, skill1Charges);
            Skill2?.ApplyNetworkSnapshot(skill2Cooldown, skill2Charges);
            Ultimate?.ApplyNetworkSnapshot(ultimateCooldown, 0);
        }

        /// <summary>
        /// The match telling the kit that something happened which some abilities pay for.
        ///
        /// ⚠️⚠️ ROUTED THROUGH THE KIT RATHER THAN MATCHED BY ABILITY ID AT THE CALL SITE. A
        /// lookup keyed by id is a second place to forget, which is the same reasoning
        /// `HeroAbility.Glyph` and `TelegraphRadius` are written down for: a new hero with a
        /// recharging skill must not be able to compile while silently never recharging.
        /// </summary>
        public void OnRechargeEvent(HeroAbility.Recharge what)
        {
            if (what == HeroAbility.Recharge.Never) return;

            if (Skill1 != null && Skill1.RechargedBy == what) Skill1.GrantCharge();
            if (Skill2 != null && Skill2.RechargedBy == what) Skill2.GrantCharge();
        }

        public virtual void Tick(AbilityContext ctx, float dt)
        {
            // ⚠️⚠️ THERE IS NO PASSIVE CHARGE HERE ANY MORE, AND ITS ABSENCE IS THE FEATURE.
            // DELETED 2026-08-25. DO NOT ADD A TRICKLE BACK.
            //
            // `Balance.UltimatePassiveChargePerSecond` was 1.0 against a max of 100, so a
            // player who did NOTHING AT ALL reached 90 of the 100 in a 90 s round. Objective
            // play was worth 25 for knocking the lata over and 20 for a tag, so a good round
            // added one ultimate on top of one that time was going to hand over regardless.
            // ⚠️ **The meter was a 100 second clock with a small bonus**, which is the thing
            // 🧑 2026-08-25 asked to stop: *"make it so that ult has to be charged and isnt
            // cooldown gated"*.
            //
            // It also sat directly against `docs/VISION.md` § 4, which lists **"Nothing may
            // reward waiting"** as a competitive requirement and names the ultimate charge in
            // the same sentence. The previous pass here fixed the half of that which was
            // visible (the trickle running during the warm-up) and left the half that was not.
            //
            // Every point is now earned by an act: knocking the lata over, tagging an attacker,
            // retrieving your own tsinelas, or releasing a legal throw.
            // `docs/Hero_Strike_Balance.md` § 3.1 has the table and what each is worth.

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
            if (!PracticeMode && UltimateCharge < UltimateCost) return CastOutcome.NoCharge;
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

        /// <summary>
        /// Everything to zero, ultimate meter included.
        ///
        /// ⚠️⚠️ THIS IS NOT THE ROUND BOUNDARY AND IT MUST NOT BE CALLED AT ONE. It sits one
        /// method above <see cref="ResetForRound"/>, it is shorter, its name is the obvious one
        /// to reach for, and it does the thing 🧑 has now asked against twice: *"its okay for ult
        /// progress to persist after round and into next rounds"* (2026-08-23) and *"i want ult
        /// charges to stay in between rounds ... Only ult tho"* (2026-08-25).
        ///
        /// It also skips `OnEnd`, so an ability mid-duration has its grant left switched on with
        /// no timer left to switch it off. `HeroAbility.ResetForRound` carries that story: a
        /// hero caught mid-Carapace would start the next round permanently unstunnable.
        ///
        /// ⚠️ IT HAS NO RUNTIME CALLERS AND IT IS KEPT FOR TESTS. `ResetForRound` and
        /// `ResetForMatch` are the two the game uses. If you are reaching for this from gameplay
        /// code, one of those two is what you actually want.
        /// </summary>
        public virtual void Reset()
        {
            UltimateCharge = 0.0f;
            Skill1?.Reset();
            Skill2?.Reset();
            Ultimate?.Reset();
        }

        /// <summary>
        /// Round-boundary reset. Cooldowns cleared, skill CHARGES refilled, and anything still
        /// running ended through `OnEnd` rather than dropped.
        ///
        /// ⚠️⚠️ THE ULTIMATE METER IS THE ONE THING THAT SURVIVES, AND THIS SUMMARY USED TO SAY
        /// THE OPPOSITE. It opened with "Charge to zero", which flatly contradicted the ⚠️ note
        /// in the body three lines below it saying the charge must NOT be reset here. Two
        /// instructions in one method, and the wrong one was the one a reader met first.
        /// Corrected 2026-08-25 after 🧑 asked for the behaviour that was already shipping:
        /// *"i want ult charges to stay in between rounds if u havent yet. Only ult tho"*.
        ///
        /// ⚠️⚠️ TWO KINDS OF "CHARGE" MEET AT THIS ONE CALL AND THEY GO OPPOSITE WAYS. Since the
        /// 2026-08-25 economy rework the word is overloaded, so name them before touching this:
        ///
        ///  * The **ultimate meter** (`UltimateCharge`) is a RESOURCE earned by objective play
        ///    across the whole match. It persists here. Only `ResetForMatch` empties it.
        ///  * A skill's **charges** (`HeroAbility.ChargesRemaining`) are a PER-ROUND allowance.
        ///    They refill here, through `HeroAbility.Reset`, because 🧑's rule for them was
        ///    *"charges ... that reset each round"*.
        ///
        /// `UltimateChargePersistsButSkillChargesRefill` asserts both halves together, because
        /// they are one line apart and a change aimed at either can silently take the other.
        ///
        /// ⚠️ THE ORIGINAL BUG THIS METHOD WAS WRITTEN FOR, kept because it explains the shape:
        /// `HeroAbilitySystem.ResetKit` once had NO CALL SITES anywhere in `Assets` or
        /// `Packages`, so cooldowns banked during the warm-up were still there at the start of
        /// round 2. Reported as *"why is ult charging up at ready screen"*. Charging during
        /// practice is fine and deliberate; **never clearing the timers was the bug.**
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
