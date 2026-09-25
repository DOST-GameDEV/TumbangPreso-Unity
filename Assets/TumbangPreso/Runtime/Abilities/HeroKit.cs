using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Holds a hero's abilities and manages the ultimate meter. A legacy kit holds Skill 1, Skill 2
    /// and the Ultimate; a ROLE kit (ability overhaul, 2026-09-25) holds a signature (Skill 1), an
    /// attacking and a defending role ability (one of which is `Skill2` at any moment), and the
    /// Ultimate. `docs/reports/amihan-kit-2026-09-25/plan.md` § 1.
    /// </summary>
    public class HeroKit
    {
        public string HeroId { get; }
        public string HeroName { get; }
        /// <summary>
        /// The SIGNATURE ability: the same whatever the hero's role (ability overhaul, owner
        /// 2026-09-25). `Verb.Skill1`. On a legacy kit it is simply skill one.
        /// </summary>
        public HeroAbility Skill1 { get; protected set; }

        /// <summary>
        /// ⚠️⚠️ THE LIVE ROLE ABILITY, AND THE SLOT EVERY OTHER SYSTEM ALREADY READS.
        ///
        /// Owner, 2026-09-25: *"there will be 2 abilities, one signature ability that doesnt change
        /// and stays no matter what role and one that changes."* A role kit declares
        /// <see cref="AttackingSkill"/> and <see cref="DefendingSkill"/>, and this returns whichever
        /// the current role gives. The deck, the touch button, the bots, the wire and the inspect
        /// panel all read `Kit.Skill2` today; answering the role HERE is what lets every one of them
        /// keep working with no change and no second place to forget.
        ///
        /// ⚠️ A LEGACY KIT SETS THIS DIRECTLY AND IS UNTOUCHED: its setter writes the one ability
        /// it has, both role fields stay null, and the getter returns exactly what it did before.
        /// </summary>
        public HeroAbility Skill2
        {
            get => HasRoleAbilities ? (IsDefending ? DefendingSkill : AttackingSkill) : _skill2;
            protected set => _skill2 = value;
        }
        private HeroAbility _skill2;

        public HeroAbility Ultimate { get; protected set; }

        /// <summary>The role ability while ATTACKING (a thrower). Null on a legacy kit.</summary>
        public HeroAbility AttackingSkill { get; protected set; }

        /// <summary>The role ability while DEFENDING (the taya). Null on a legacy kit.</summary>
        public HeroAbility DefendingSkill { get; protected set; }

        /// <summary>True for a kit built in the new shape: a signature plus two role abilities.</summary>
        public bool HasRoleAbilities => AttackingSkill != null && DefendingSkill != null;

        /// <summary>
        /// Which role the kit is playing. ⚠️ WRITTEN ONLY BY <see cref="SetRole"/>, which
        /// `HeroAbilitySystem` calls from the body's own `IsDefender`: the taya is DERIVED,
        /// `(round - 1) % 4` (`CLAUDE.md` § 4), and a kit keeping its own opinion of it would be a
        /// second clock that could disagree with the rules.
        /// </summary>
        public bool IsDefending { get; private set; }

        /// <summary>
        /// Raised when the live role ability changes, for the deck's swap animation. Never on a
        /// legacy kit, whose slot two does not change.
        /// </summary>
        public event Action<HeroAbility, HeroAbility> RoleAbilityChanged;

        /// <summary>
        /// Puts the kit in a role.
        ///
        /// ⚠️⚠️ THE OUTGOING ROLE ABILITY IS ENDED THROUGH `ResetForRound`, NOT DROPPED. A role
        /// ability can hold a grant (Updraft's flight) and `HeroAbility.ResetForRound`'s note is
        /// the whole argument: a duration zeroed behind an ability's back leaves its grant switched
        /// on with nothing left to switch it off. The role changes at the round boundary, which is
        /// also where every cooldown is cleared, so this is the same reset the round does anyway,
        /// applied to the one ability leaving play.
        /// </summary>
        public void SetRole(bool defending, AbilityContext ctx)
        {
            if (!HasRoleAbilities) { IsDefending = defending; return; }
            if (IsDefending == defending) return;
            var outgoing = Skill2;
            IsDefending = defending;
            outgoing?.ResetForRound(ctx);
            RoleAbilityChanged?.Invoke(outgoing, Skill2);
        }

        /// <summary>
        /// Every ability the kit owns, once each: the signature, BOTH role abilities on a role kit
        /// (the live one first) or skill two on a legacy kit, and the ultimate. For anything that
        /// must cover the whole kit rather than what is live this round: tests, the screens, audits.
        /// </summary>
        public HeroAbility[] AllAbilities => HasRoleAbilities
            ? new[] { Skill1, Skill2, IdleRoleSkill, Ultimate }
            : new[] { Skill1, Skill2, Ultimate };

        /// <summary>One power as a screen shows it: what it is, what the tile is labelled, which
        /// binding casts it, and which loadout slot (1, 2, or 0 for none) its variants live in.</summary>
        public readonly struct ScreenSlot
        {
            public readonly HeroAbility Ability; public readonly string Label, Action;
            public readonly int LoadoutSlot; public readonly bool IsUltimate;
            public ScreenSlot(HeroAbility ability, string label, string action, int loadoutSlot, bool ultimate)
            { Ability = ability; Label = label; Action = action; LoadoutSlot = loadoutSlot; IsUltimate = ultimate; }
        }

        /// <summary>
        /// ⚠️ THE ONE ANSWER TO "WHAT DOES A SCREEN SHOW FOR THIS KIT" (ability overhaul, 2026-09-25).
        /// A role kit is FOUR powers out of a match (signature, attacking, defending, ultimate), and
        /// the defending one is cast with the same key as the attacking one; a legacy kit is the
        /// three it always was. Every screen that lists a kit reads this, so a new role kit cannot
        /// ship with its defending ability missing from character select.
        /// </summary>
        public ScreenSlot[] ScreenSlots => HasRoleAbilities
            ? new[]
            {
                new ScreenSlot(Skill1, "SIGNATURE", "Skill1", 1, false),
                new ScreenSlot(AttackingSkill, "ATTACKING", "Skill2", 2, false),
                new ScreenSlot(DefendingSkill, "DEFENDING", "Skill2", 0, false),
                new ScreenSlot(Ultimate, "ULTIMATE", "Ultimate", 0, true),
            }
            : new[]
            {
                new ScreenSlot(Skill1, "SKILL 1", "Skill1", 1, false),
                new ScreenSlot(Skill2, "SKILL 2", "Skill2", 2, false),
                new ScreenSlot(Ultimate, "ULTIMATE", "Ultimate", 0, true),
            };

        /// <summary>The role ability NOT in play this round, for the tray and the screens.</summary>
        public HeroAbility IdleRoleSkill => HasRoleAbilities ? (IsDefending ? AttackingSkill : DefendingSkill) : null;

        public float UltimateCharge { get; protected set; }

        /// <summary>Existing movement skills may scale wish speed; impulses and slows retain their own rules.</summary>
        public virtual float MovementSpeedScale => 1.0f;

        /// <summary>
        /// ⚠️⚠️ KEPT AS THE METER'S FULL-SCALE VALUE, NOT AS THE PRICE. It used to be both, and
        /// that is why every hero's ultimate cost the same. <see cref="UltimateCost"/> is the
        /// price now; this is only what "100 per cent" means on the widget.
        ///
        /// It stays a `const` because a dozen tests and the HUD arithmetic read it, and because
        /// a meter whose full scale varies per hero cannot be compared across the four cards on
        /// a spectator's screen.
        ///
        /// ⚠️⚠️ 20, DOWN FROM 100, BECAUSE THE METER COUNTS EVENTS NOW. One lata knockdown is
        /// one charge (`Balance.UltimateChargeLataKnock`) and the dearest ultimate in the game
        /// costs twenty of them, so full scale IS the ceiling of the cost range. 🧑 2026-08-27:
        /// *"i want downing can and tayaing to only give one point for the charges"* and
        /// *"i wanted like 10-20 charges required on ult depending on impact"*. `Balance`'s
        /// ultimate economy block carries the rescale and the pacing arithmetic.
        /// </summary>
        public const float UltimateMax = 20.0f;

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
        /// ⚠️⚠️ IT NOW MEANS "NO POWER MAY START", AND IT USED TO MEAN "EVERY POWER IS FREE".
        /// 🧑 2026-08-30: *"remove unli skill before round bcz ppl fly out of map and shit"*.
        ///
        /// ⚠️⚠️ THIS REVERSES A WRITTEN INSTRUCTION, SO HERE IS THE ONE IT REVERSES RATHER THAN
        /// A DELETION. 🧑 2026-08-23: *"its okay for ult progress to persist after round and into
        /// next rounds, i js want it to pause when the game isnt ongoing... BUt i want ppl to be
        /// able to test skills still and shit during buffer period so maybe during buffer period
        /// give them diff timers"*. That was answered by splitting the economy from the practice:
        /// the banked charge froze, and the ultimate stayed castable as often as the player liked
        /// off a counter that did not exist outside the warm-up. **The ask was granted exactly
        /// and the result is the fault above**, because the warm-up is the one window in the
        /// game where bodies may walk (`RoundDirector.ApplySnapshot`: `CharacterMotor.RoundActive`
        /// defaults TRUE so the free-roam window works) while nothing is refereeing. Dante's
        /// stomp, Sean's Supernova and Zack's charge are all impulses, `Balance` derives every
        /// one of them from `Friction` as a DISTANCE, and an unlimited number of them fired at
        /// four bodies standing in a box is people leaving the map.
        ///
        /// ⚠️ THE HALF HE ASKED FOR IN 2026-08-23 IS KEPT AND ONLY THE FREE CAST IS GONE. The
        /// banked charge still neither accrues nor is spent while the clock is stopped, which is
        /// the *"pause when the game isnt ongoing"* sentence, and it is the half that has nothing
        /// to do with launching anybody.
        ///
        /// ⚠️ IT IS NOT THE GUIDED TUTORIAL AND IT NEVER WAS. `RoundDirector.FixedUpdate` returns
        /// on `!RoundActive` before it reaches the `GameLaunch.GuidedTutorial` branch, so a
        /// tutorial round has `RoundActive` TRUE and this FALSE: the lesson still casts real
        /// powers on real cooldowns, which is what `GuidedTraining` teaches against.
        ///
        /// ⚠️ AND THE PRACTICE MATCH IS A DIFFERENT THING WITH A COLLIDING NAME. The PRACTICE
        /// button runs real rounds; `ConvertedMatchSetup` carries a note saying so.
        /// </summary>
        public bool PracticeMode { get; set; }

        /// <summary>
        /// ⚠️⚠️ NOT READY WHILE THE CLOCK IS STOPPED, AND THE OLD `PracticeMode ||` HERE WAS THE
        /// HALF THE PLAYER COULD SEE. It lit the ultimate tile through the whole warm-up, which
        /// is a promise the cast now refuses; `Hud` reads this property for that tile. The report
        /// is on <see cref="PracticeMode"/>.
        /// </summary>
        public bool IsUltimateReady =>
            !PracticeMode && UltimateCharge >= UltimateCost && (Ultimate == null || Ultimate.IsReady);

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
        /// <summary>
        /// ⚠️ `mayLower` IS FALSE ONLY FOR THE SEAT THIS PEER DRIVES, AND ONLY MID-ROUND. See
        /// <see cref="HeroAbility.ApplyNetworkSnapshot"/> for the whole chain; the short version
        /// is that a host which REFUSED a cast reports the state it has, which is no cooldown at
        /// all, and assigning that over the owner's spent cooldown hands them the ability back.
        /// </summary>
        public void ApplyNetworkSnapshot(float ultimateCharge,
                                         float skill1Cooldown, int skill1Charges,
                                         float skill2Cooldown, int skill2Charges,
                                         float ultimateCooldown,
                                         bool mayLower = true)
        {
            UltimateCharge = Mathf.Clamp(ultimateCharge, 0.0f, UltimateCost);

            Skill1?.ApplyNetworkSnapshot(skill1Cooldown, skill1Charges, mayLower);
            Skill2?.ApplyNetworkSnapshot(skill2Cooldown, skill2Charges, mayLower);
            Ultimate?.ApplyNetworkSnapshot(ultimateCooldown, 0, mayLower);
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
            // ⚠️ THE IDLE ROLE ABILITY STILL TICKS, so an effect it left in the world at the
            // moment of a role change (it cannot, because the change resets it, but a future one
            // might) runs out on its own clock rather than freezing half-finished.
            IdleRoleSkill?.Tick(ctx, dt);
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
            /// The round clock is not running, so no power may START. A refusal: answer it and
            /// clear the buffer.
            ///
            /// ⚠️⚠️ IT IS A REFUSAL RATHER THAN <see cref="CannotAct"/>, AND THE DIFFERENCE IS
            /// WHAT HAPPENS AT "GO". `CannotAct` holds the press and retries it for
            /// `InputBufferWindow`, which is exactly right for a stun — the player asked for
            /// something they are allowed to have. Reusing it here would mean every ultimate
            /// pressed in the last 0.30 s of the warm-up fired on the first frame of the round,
            /// four bodies deep, which is the fault this outcome was added to stop wearing a
            /// different hat. The tick says no and the player presses again.
            /// </summary>
            NotYet,

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
            var allowed = CheckUltimate(ctx);
            if (allowed != CastOutcome.Cast) return allowed;
            Ultimate.Activate(ctx);
            UltimateCharge = 0;
            return CastOutcome.Cast;
        }

        internal CastOutcome CheckUltimate(AbilityContext ctx)
        {
            if (Ultimate == null) return CastOutcome.Missing;

            // ⚠️ THE ORDER MATTERS AND IT IS "CAN I ACT" LAST. A stunned player with an empty
            // meter is told the meter is empty, which is the fact they can do something about,
            // rather than being told to wait for a cast that would be refused anyway.
            // ⚠️⚠️ THE WARM-UP REFUSES THE CAST NOW, AND IT USED TO MAKE IT FREE. See
            // <see cref="PracticeMode"/>: this branch read `if (!PracticeMode && UltimateCharge <
            // UltimateCost) return NoCharge;` and paid out an ultimate that cost nothing and was
            // never spent.
            if (PracticeMode) return CastOutcome.NotYet;

            if (!Ultimate.IsReady) return CastOutcome.Cooling;
            if (UltimateCharge < UltimateCost) return CastOutcome.NoCharge;
            if (ctx != null && ctx.Motor != null && !ctx.Motor.CanAct()) return CastOutcome.CannotAct;
            if (!Ultimate.CanActivate(ctx)) return CastOutcome.CannotAct;

            return CastOutcome.Cast;
        }

        internal CastOutcome ReserveUltimate(AbilityContext ctx)
        {
            var allowed = CheckUltimate(ctx);
            if (allowed != CastOutcome.Cast) return allowed;
            Ultimate.ReserveForIntroduction(); UltimateCharge = 0;
            return CastOutcome.Cast;
        }
        internal void AdoptUltimateReservation()
        { Ultimate?.ReserveForIntroduction(); UltimateCharge = 0; }

        internal CastOutcome CheckSkill(int slot,AbilityContext ctx)=>CheckFire(slot==0?Skill1:Skill2,ctx);
        private CastOutcome CheckFire(HeroAbility ability,AbilityContext ctx)
        {
            if(ability==null)return CastOutcome.Missing;
            if(ability.IsActive&&ability.CanReactivate)return CastOutcome.Cast;
            if(PracticeMode)return CastOutcome.NotYet;
            if(!ability.IsReady)return CastOutcome.Cooling;
            if(ctx?.Motor!=null&&!ctx.Motor.CanAct())return CastOutcome.CannotAct;
            return ability.CanActivate(ctx)?CastOutcome.Cast:CastOutcome.CannotAct;
        }
        private CastOutcome Fire(HeroAbility ability, AbilityContext ctx)
        {
            var allowed=CheckFire(ability,ctx);if(allowed!=CastOutcome.Cast)return allowed;
            if(ability.IsActive&&ability.CanReactivate)ability.Reactivate(ctx);else ability.Activate(ctx);
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
            IdleRoleSkill?.Reset();
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
            IdleRoleSkill?.ResetForRound(ctx);
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
