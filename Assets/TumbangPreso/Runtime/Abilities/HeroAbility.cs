using System;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Base class for hero abilities and ultimates.
    /// </summary>
    public abstract class HeroAbility
    {
        public string Id { get; }
        public string Name { get; }

        /// <summary>
        /// The full tactical sentence, for the hold-to-read tray.
        ///
        /// ⚠️ THE TRAY IS THE ONLY PLACE THIS BELONGS. The deck at the bottom of the screen
        /// carries what is true RIGHT NOW and nothing else; 🧑 2026-08-23 on why:
        /// *"games like valorant overwatch league etc dont clog their screen with text"*.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// One short line for anywhere the full sentence does not fit, chiefly the character
        /// select ribbon's details card.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE FOUR OF THE FIFTEEN POWERS DESCRIBED THEMSELVES IN A SENTENCE
        /// THAT STOPPED MID-WORD. The card draws `Description` into a 46 px box at 14 pt with
        /// `VerticalWrapMode.Truncate`, which is about three lines, and the tactical sentences
        /// run to four or five. Truncation is silent, so the screen a player uses to CHOOSE a
        /// hero was the one screen lying to them about what the hero does.
        ///
        /// Falls back to the full description, so an ability that has not been given one is
        /// merely long rather than blank.
        /// </summary>
        public string Summary { get; }

        /// <summary>
        /// What KIND of power this is, drawn as a shape wherever it is shown.
        ///
        /// ⚠⚠ IT LIVES ON THE ABILITY, NOT IN A LOOKUP TABLE IN THE HUD. A table keyed by
        /// ability id is a second place to forget: adding a hero would compile, run, and show
        /// three blank tiles. Here the compiler cannot let a new ability exist without one.
        /// </summary>
        public UI.AbilityGlyph Glyph { get; }
        public float Cooldown { get; protected set; }
        public float Duration { get; protected set; }

        public float CooldownRemaining { get; protected set; }
        public float DurationRemaining { get; protected set; }
        public bool IsActive => DurationRemaining > 0.0f;

        // ------------------------------------------------------------------ wind-up
        //
        // ⚠️⚠️ THIS WAS DELIBERATELY REFUSED ONCE AND THE REFUSAL IS WORTH READING BEFORE
        // TOUCHING IT. `HeroAbilitySystem.PlayUltimatePresentation` carries the note: *"A real
        // 0.4 s wind-up on the ABILITY would be a fourth thing that can be interrupted, and every
        // one of the five ultimates would need its own answer to being stunned halfway through;
        // the buffering rules in `docs/Hero_Strike_UI.md` § 7 have no story for a half-cast
        // power."* That objection was correct, and it is about INTERRUPTION rather than about
        // the delay.
        //
        // ⚠️⚠️ SO THE WIND-UP IS UNINTERRUPTIBLE, WHICH DISSOLVES THE OBJECTION RATHER THAN
        // ARGUING WITH IT. Once the press lands the ultimate WILL fire: nothing stuns, trips or
        // cancels it, so there is no half-cast state for any of the five to have an answer to,
        // and § 7's buffering never sees one. The meter is spent at the press, exactly as before,
        // so no refund path exists to get wrong either.
        //
        // What the beat buys is what `Hero_Strike_Balance.md` § 4.3 actually asked for: *"The
        // other three players get a beat to react, which is what makes an ultimate an event
        // rather than a large skill."* Reacting means running, repositioning or spending a
        // defensive power, and none of those need the cast to be cancellable.
        //
        // ⚠️ THE CASTER ROOTS, AND THAT IS THE COST THE BEAT IS PAID FOR WITH. Standing still
        // for 0.4 s in a 14 m arena is real, and it is what stops the wind-up from being pure
        // spectacle. § 4.3 also floats interruptibility as a balance knob; it is NOT taken here,
        // and taking it later means writing the half-cast story the note above says is missing.
        //
        // ⚠️ ULTIMATES ONLY. 🧑, authorising this: *"just dont spam it for everything"*. The five
        // skills resolve on the frame they are pressed and should keep doing so; a wind-up on a
        // 6 s cooldown skill is a tax, not an event.

        /// <summary>
        /// The one wind-up length, shared by all five ultimates.
        ///
        /// ⚠️ IT IS A CONSTANT RATHER THAN A PER-HERO NUMBER BECAUSE THE BEAT IS A RULE, NOT A
        /// BALANCE DIAL. Five different wind-ups would mean five different windows to learn for
        /// no gain: what a player reads is "somebody is ultimate-ing", and that has to look and
        /// last the same whoever cast it. 0.4 s is `Hero_Strike_Balance.md` § 4.3's own figure.
        /// </summary>
        public const float UltimateWindup = 0.4f;

        /// <summary>Seconds between the press and the effect. 0 on everything but an ultimate.</summary>
        public float Windup { get; protected set; }

        public float WindupRemaining { get; private set; }
        public bool IsWindingUp => WindupRemaining > 0.0f;

        /// <summary>⚠️ A TRUE ROOT, and the stack takes the MINIMUM rather than the product, so
        /// this cannot compound with fatigue or a hazard into something stranger than a stop.</summary>
        private const float RootSpeed = 0.0f;

        private CharacterMotor _rooted;

        // ------------------------------------------------------------------ charges
        //
        // ⚠️⚠️ AN ABILITY IS EITHER ON A COOLDOWN OR ON CHARGES, NEVER ON BOTH, AND WHICH ONE IT
        // GETS FOLLOWS WHAT IT DOES RATHER THAN WHICH SLOT IT IS IN.
        //
        // 🧑 2026-08-25, on a build where four seats cast 44 to 56 times in a 90 s round:
        // *"game feels awkward when theres 20 abilities at once and i think the fix to this is
        // making the abilities timers longer? ... maybe make it like valorant wherein they have
        // charges for their skill that they can use once per round"*, then *"for some skills
        // they can have a cooldown instead of charges that reset each round, make it long tho
        // like 30 seconds to 45 seconds"*.
        //
        // The rule the split follows, so it is not decided case by case:
        //
        //  * An ability that LEAVES AN OBJECT ON THE FLOOR takes CHARGES. A wall or a zone is a
        //    decision the whole court then plays around, and scarcity is what makes placing it
        //    interesting. Spending your last one is meant to hurt.
        //  * An ability that MOVES OR PROTECTS YOUR OWN BODY takes a LONG COOLDOWN. A dash or an
        //    armour is a reaction, and a reaction you have permanently spent is a character who
        //    stops being able to play. A player holding their last escape charge does not
        //    escape, they hoard, and `docs/VISION.md` § 4 forbids anything that rewards waiting.
        //
        // `docs/Hero_Strike_Balance.md` § 3.1 has the per-ability table and the reasoning.

        /// <summary>
        /// How many casts this ability gets per round, or **0 for a cooldown ability**.
        ///
        /// ⚠️ ZERO IS THE DEFAULT AND MEANS "NOT A CHARGE ABILITY", not "no casts". Every
        /// ability shipped before 2026-08-25 is a cooldown ability and none of them had to
        /// change to keep working.
        /// </summary>
        public int MaxCharges { get; protected set; }

        public bool UsesCharges => MaxCharges > 0;

        /// <summary>How many are left this round. Refilled by <see cref="ResetForRound"/>.</summary>
        public int ChargesRemaining { get; protected set; }

        /// <summary>
        /// What play, if anything, hands a charge back mid-round.
        ///
        /// ⚠️⚠️ IT IS AN EVENT, NEVER A TIMER, AND THAT IS THE WHOLE POINT OF THE SPLIT. A charge
        /// that comes back on a clock is a cooldown with extra bookkeeping, and it re-creates the
        /// problem the charges were introduced to solve.
        /// </summary>
        public enum Recharge
        {
            /// <summary>It runs out. Most charge abilities.</summary>
            Never,

            /// <summary>
            /// ⚠️ THE ACT THE WHOLE GAME IS BUILT AROUND, AND IT USED TO PAY NOTHING AT ALL.
            /// `docs/VISION.md` § 0: *"The tension is the retrieval, not the throw."* Going back
            /// in for your tsinelas is the only moment you can be caught, and before this the
            /// game's entire reward for it was the tsinelas.
            /// </summary>
            OwnSlipperRetrieved,

            /// <summary>The objective. Closes the loop on a skill that buffs a throw: charge it,
            /// land it, get the charge back.</summary>
            LataKnocked,
        }

        public Recharge RechargedBy { get; protected set; } = Recharge.Never;

        /// <summary>
        /// ⚠️⚠️ READY MEANS TWO DIFFERENT THINGS AND BOTH CALLERS USED TO ASSUME THE COOLDOWN
        /// ONE. A charge ability's cooldown is 0 forever, so before this every charge ability
        /// read as permanently ready and could be cast past empty.
        /// </summary>
        public bool IsReady => UsesCharges ? ChargesRemaining > 0 : CooldownRemaining <= 0.0f;

        public float CooldownRatio => Cooldown > 0.0f ? Mathf.Clamp01(CooldownRemaining / Cooldown) : 0.0f;
        public float DurationRatio => Duration > 0.0f ? Mathf.Clamp01(DurationRemaining / Duration) : 0.0f;

        /// <summary>
        /// How wide the ground telegraph is, in metres, or 0 for a power that puts nothing on
        /// the ground.
        ///
        /// ⚠️⚠️ IT LIVES ON THE ABILITY BECAUSE THE HUD USED TO INVENT IT.
        /// `HeroAbilitySystem.UpdateReticle` drew 7.5 m for ANY ultimate, 5.0 m for ANY first
        /// skill and 3.5 m for ANY second, and offset the ring forward only when the kit
        /// happened to be Cheska's. So Dante's 2.4 m stomp drew a 5.0 m ring, Nemu's 3.2 m void
        /// drew 7.5 m, and the void landed 3.5 m in front of the ring the player had just been
        /// shown. **A telegraph that lies is worse than no telegraph**, because a player
        /// believes it once and then stops believing all of them.
        ///
        /// Same reasoning as <see cref="Glyph"/>: a lookup table keyed by ability id is a
        /// second place to forget, and a new hero would compile, run and draw three wrong
        /// rings.
        /// </summary>
        public float TelegraphRadius { get; protected set; }

        /// <summary>
        /// How far in front of the caster the telegraph centre sits, in metres. 0 means it goes
        /// off around the caster.
        ///
        /// ⚠️ IT MUST MATCH THE NUMBER THE `OnActivate` ACTUALLY SPAWNS AT. These two are the
        /// same measurement written twice, which is the shape of drift `Design.md` opens by
        /// warning about, so `TelegraphsMatchWhatTheAbilityActuallyPlaces` asserts the pair.
        /// </summary>
        public float TelegraphRange { get; protected set; }

        public bool HasTelegraph => TelegraphRadius > 0.0f;

        /// <summary>
        /// Whether the telegraph also stands a mark up at the destination.
        ///
        /// ⚠️⚠️ IT IS FOR POWERS THAT PUT **YOU** SOMEWHERE, NOT POWERS THAT LAND SOMEWHERE. 🧑
        /// 2026-08-27, on Phaister's blink: *"to teleport u have to hold her E skill and all it
        /// shows is a frigging shadow, it's very easy to miss and not in her theme at all"*. A
        /// ring on the road is exactly right for a zone: you look down and read whether you are
        /// inside it. It is exactly wrong for a teleport: the player is looking along the street
        /// at head height, where a flat decal five metres away is a few pixels of smudge.
        ///
        /// ⚠️ IT IS A FLAG RATHER THAN A SECOND TELEGRAPH SYSTEM. `GroundReticle` owns the mark
        /// and draws it in the hero's own accent, so a future dash or recall gets it by setting
        /// one bool rather than by writing a second reticle that will drift from this one.
        /// </summary>
        public bool AimBeacon { get; protected set; }

        // ------------------------------------------------------------------ hold to aim
        //
        // ⚠️⚠️ 🧑 2026-08-26, ON THE BLINK: *"let her HOLD e to control where she will go and make
        // it a teleport abilitiy and make it prettier"*. Everything below is that, and it is
        // written on the BASE class rather than inside Phaister's kit because the second ability
        // that wants it must not have to reimplement the release edge, the cap and the auto-fire.
        //
        // ⚠️⚠️ AND IT IS THE ONE PLACE `docs/VISION.md` § 4 COULD BE BROKEN BY A FEATURE THAT
        // SOUNDS HARMLESS. *"Nothing may reward waiting"* is a competitive requirement, not a
        // mood, and the obvious implementation of hold-to-aim breaks it three separate ways: a
        // hold that ROOTS you is a stall, a hold that PAUSES the anti-camp clock is a hiding
        // place, and a hold with no ceiling is a player standing in a corner with an escape
        // permanently half-pressed. So:
        //
        //   * The caster keeps full movement while aiming. There is no root and no slow.
        //   * Nothing about aiming touches `RoundDirector`'s clocks. Holding E in the taya's
        //     circle accrues exactly the penalty standing there always did.
        //   * <see cref="MaxAimSeconds"/> is a hard ceiling: the power FIRES at it rather than
        //     being cancelled, so the hold cannot be used as a parking brake and a player who
        //     was genuinely aiming still gets their cast.
        //   * The reach stops growing well before the ceiling, so the last half second of a hold
        //     buys nothing at all. That is what actually removes the incentive to wait; the cap
        //     alone would only bound it.

        /// <summary>
        /// True when the press begins AIMING and the release is what casts.
        ///
        /// ⚠️ IT CHANGES WHICH EDGE FIRES THE ABILITY, so `HeroAbilitySystem` has to know before
        /// it services the input buffer. Everything else in the game casts on `JustPressed`.
        /// </summary>
        public bool HoldToAim { get; protected set; }

        /// <summary>
        /// How long a hold may last before the ability fires on its own. Seconds.
        /// <b>Zero means it never fires on its own: only the release casts it.</b>
        ///
        /// ⚠️⚠️ IT IS ZERO ON THE ONE ABILITY THAT USES IT, AND THE CEILING IS GONE BECAUSE 🧑
        /// ASKED FOR IT AFTER PLAYING IT. 2026-08-27: *"u cant control the E of phaister and it
        /// autocasts after some seconds, i want it to cast only when i let go"*. The old
        /// derivation for 1.10 s is kept below because the reasoning is still sound about what
        /// it was solving; it was solving the wrong thing.
        ///
        /// ⚠️⚠️ AND REMOVING IT DOES NOT BREAK `docs/VISION.md` § 4's *"nothing may reward
        /// waiting"*, WHICH IS THE OBJECTION THE CEILING EXISTED TO ANSWER. Read the section
        /// note above: the ceiling was only ever ONE of four defences, and it is the weakest.
        /// The load-bearing one is that <see cref="AimRampSeconds"/> stops the reach growing at
        /// 0.55 s, so every second of hold after the first half second buys the player exactly
        /// nothing. A hold that pays out nothing is not a reward for waiting, it is a player
        /// standing still in a 14 m arena with a taya walking toward them.
        ///
        /// The other two defences are untouched and are what make that safe: the caster keeps
        /// full movement while aiming (there is no root and no slow), and nothing about aiming
        /// touches `RoundDirector`'s anti-camp or anti-stall clocks, so holding E in the taya's
        /// circle accrues exactly the penalty standing there always did.
        ///
        /// ⚠️ THE ORIGINAL DERIVATION, KEPT: 1.10 s was `HeroAbilitySystem.InputBufferWindow`
        /// (0.30 s) plus twice the ramp, *"long enough that a player turning to look behind them
        /// is never cut off, short enough that holding it is not a stance"*. What play showed is
        /// that a player turning to look behind them takes longer than 1.10 s often enough to
        /// matter, and being cut off mid-turn is indistinguishable from the game misfiring.
        /// </summary>
        public float MaxAimSeconds { get; protected set; } = 1.10f;

        /// <summary>True when only the release may cast this, with no ceiling at all.</summary>
        public bool CastsOnReleaseOnly => HoldToAim && MaxAimSeconds <= 0.0f;

        /// <summary>Nearest the power can be aimed, in metres. The reach at zero hold.</summary>
        public float AimMinRange { get; protected set; }

        /// <summary>Furthest it can be aimed. Reached at <see cref="AimRampSeconds"/>.</summary>
        public float AimMaxRange { get; protected set; }

        /// <summary>
        /// How long the reach takes to run from min to max.
        ///
        /// ⚠️ SHORTER THAN THE CEILING BY DESIGN. See the section note: a hold that keeps paying
        /// out right up to its limit is a hold a player is rewarded for maxing every time, which
        /// makes the ability one length with extra steps.
        /// </summary>
        public float AimRampSeconds { get; protected set; } = 0.55f;

        /// <summary>How far this cast is aimed, for a hold of the given length.</summary>
        public float AimRangeFor(float heldSeconds)
        {
            if (AimMaxRange <= 0.0f) return TelegraphRange;

            float t = AimRampSeconds > 0.0f
                ? Mathf.Clamp01(heldSeconds / AimRampSeconds)
                : 1.0f;

            return Mathf.Lerp(AimMinRange, AimMaxRange, t);
        }

        /// <summary>
        /// How long the finger was down on the press that produced the cast currently running.
        ///
        /// ⚠️ WRITTEN BY `HeroAbilitySystem` IMMEDIATELY BEFORE `Activate`, AND READ INSIDE
        /// `OnActivate`. It is a field rather than an `Activate` parameter because five kits and
        /// eighteen abilities override `OnActivate` with the signature it has today, and adding
        /// an argument that seventeen of them ignore is how a signature becomes noise.
        /// </summary>
        public float HeldSecondsOnCast { get; internal set; }

        /// <summary>Turns this ability into a hold-to-aim cast. Call from a kit's constructor.</summary>
        protected void AimByHolding(float minRange, float maxRange,
                                    float rampSeconds = 0.55f, float maxHoldSeconds = 1.10f)
        {
            HoldToAim = true;
            AimMinRange = minRange;
            AimMaxRange = maxRange;
            AimRampSeconds = rampSeconds;
            MaxAimSeconds = maxHoldSeconds;
        }

        /// <summary>
        /// The bespoke 3rd-person body action name (e.g. "hero-sean-dash", "hero-dante-stomp").
        /// </summary>
        public string CastAction { get; protected set; }

        /// <summary>
        /// The bespoke 1st-person viewmodel action name (e.g. "thrust-fire", "stomp-heavy").
        /// </summary>
        public string ViewmodelAction { get; protected set; }

        protected HeroAbility(string id, string name, string description, float cooldown,
                              float duration = 0.0f,
                              UI.AbilityGlyph glyph = UI.AbilityGlyph.Burst,
                              string summary = null,
                              float telegraphRadius = 0.0f,
                              float telegraphRange = 0.0f,
                              string castAction = null,
                              string viewmodelAction = null,
                              int charges = 0,
                              Recharge rechargedBy = Recharge.Never)
        {
            Id = id;
            Name = name;
            Description = description;
            Summary = string.IsNullOrEmpty(summary) ? description : summary;
            Cooldown = cooldown;
            Duration = duration;
            Glyph = glyph;
            TelegraphRadius = telegraphRadius;
            TelegraphRange = telegraphRange;
            CastAction = castAction;
            ViewmodelAction = viewmodelAction ?? castAction;
            MaxCharges = charges;
            ChargesRemaining = charges;
            RechargedBy = rechargedBy;
        }

        // -------------------------------------------------------------------
        // § WHAT A REJOINING PLAYER GETS BACK
        //
        // ⚠️⚠️ 🧑 2026-08-27, ON THE NETWORKING: *"or if u retain ur skill cooldowns and charges
        // and shi"*. The answer before this existed was NO, AND IT WAS AN EXPLOIT RATHER THAN A
        // COSMETIC GAP. `MatchRpc.BroadcastWorldSnapshot` sends the round number, the defender
        // slot, the clock, the scores, the lata, the slippers, the picks and the unit transforms.
        // **It has never sent one byte of ability state**, so a client that dropped and came back
        // rebuilt its kit from the constructor: every cooldown zero, every charge full, the
        // ultimate meter empty.
        //
        // ⚠️⚠️ BOTH DIRECTIONS ARE WRONG AND ONLY ONE OF THEM IS OBVIOUS. Reconnecting to refresh
        // a 62 s cooldown is the cheat everybody thinks of first. The one that actually gets
        // reported is the other half: a player who had banked 115 charge toward an ultimate loses
        // all of it to a dropped packet, which reads as the game stealing a round's work.
        //
        // ⚠️ AND THE HOST NEVER HAD THE BUG, WHICH IS WHY IT SURVIVED. The host's own kits are
        // continuous objects that were never rebuilt, so nothing about this is visible in a
        // single-machine test or to the person running the lobby.
        // -------------------------------------------------------------------

        /// <summary>
        /// Overwrites this ability's live timers with the host's.
        ///
        /// ⚠️⚠️ IT DELIBERATELY DOES NOT TOUCH `DurationRemaining`, AND THAT IS A CORRECTNESS
        /// RULE RATHER THAN AN OMISSION. A duration is not a number, it is a GRANT that `OnEnd`
        /// has to take back: `Reset`'s own header records that zeroing one behind an ability's
        /// back leaves Demonic Carapace's stun immunity and Phantom Phase's tag immunity switched
        /// on with no timer left to switch them off. Writing a duration in from the wire would do
        /// the same thing from a second direction, and a rejoiner would arrive permanently
        /// unstunnable. A running duration expires on its own within seconds; a cooldown does
        /// not, which is why only the cooldown is worth carrying across the wire at all.
        /// </summary>
        public void ApplyNetworkSnapshot(float cooldownRemaining, int chargesRemaining,
                                         bool mayLower = true)
        {
            // ⚠️⚠️ `mayLower` IS WHAT STOPS AN ABILITY BEING SPAMMED ON A LOSSY LINK, and it is
            // false for exactly one peer: the one that owns this seat, while a round is live.
            // 🧑 2026-08-29, of Phaister: *"si phaister need ayusin yung 2nd ability kasi
            // spammable teleport (lan problem)"*, and later *"phaister ability works right
            // everywhere else except for lan"*, *"its spammable in lan, weirdly"*.
            //
            // The chain, and none of it is Phaister-specific:
            //
            //   1. A client casts. `HeroAbilitySystem.Cast` predicts locally, which spends the
            //      cooldown on the client, and sends `RequestAbilityCast`.
            //   2. The host checks `PlausibleIntentPose`: is the pose the client claims within
            //      `IntentPoseLeeway` 2.25 m of the host's own copy of that body? If not, the
            //      request is DROPPED. Silently, and correctly as far as that guard goes.
            //   3. Because it dropped, the host never ran `Activate`, so the host's copy of that
            //      kit still reads a cooldown of ZERO.
            //   4. The next `BroadcastAbilityState` sends that zero to everyone, and the OWNER
            //      assigned it straight over the cooldown it had just spent. Ready again, one
            //      snapshot later, for as long as the player kept pressing.
            //
            // ⚠️ AND THAT IS WHY IT WAS LAN-ONLY, WHICH IS THE PART THAT LOOKS IMPOSSIBLE.
            // Nothing here branches on the transport. Step 2 fails when the host's copy of a
            // moving body is more than 2.25 m stale, which is 375 ms at a sprint, and until this
            // batch `SyncUnit` and `SubmitMove` went out `ReliableSequenced`: one lost packet
            // head-of-line blocked the whole pose stream and delivered the backlog in a burst.
            // Hamachi has real loss and a smaller MTU, so the LAN sessions produced those stalls
            // and the relay sessions did not. `MatchRpc.PoseDelivery` attacks the same root from
            // the transport end; this is the half that holds even when a packet does go missing.
            //
            // ⚠️ THE HOST CAN STILL MAKE AN ABILITY UNAVAILABLE, which is the direction authority
            // actually needs. It may raise a cooldown on anybody at any time. What it may no
            // longer do is hand the owner a cast back by reporting a state it reached by refusing
            // to act.
            float wanted = Mathf.Max(0.0f, cooldownRemaining);
            CooldownRemaining = mayLower ? wanted : Mathf.Max(CooldownRemaining, wanted);

            if (UsesCharges)
            {
                int clamped = Mathf.Clamp(chargesRemaining, 0, MaxCharges);
                ChargesRemaining = mayLower ? clamped : Mathf.Min(ChargesRemaining, clamped);
            }
        }

        /// <summary>
        /// Hands one charge back, up to the cap. Called by <see cref="HeroKit"/> when the match
        /// reports the event this ability is keyed to.
        /// </summary>
        public void GrantCharge()
        {
            if (!UsesCharges) return;
            ChargesRemaining = Mathf.Min(MaxCharges, ChargesRemaining + 1);
        }

        public virtual bool CanActivate(AbilityContext ctx)
        {
            if (ctx == null || ctx.Motor == null) return false;
            if (!IsReady) return false;

            // ⚠️ A WIND-UP ALREADY RUNNING IS NOT A SECOND CAST. Without this a player mashing
            // the ultimate key during the beat would re-enter the speed zone once per press and
            // exit it once, leaving one root behind for every extra press.
            if (IsWindingUp) return false;

            if (!ctx.Motor.CanAct()) return false;
            return true;
        }

        public virtual void Activate(AbilityContext ctx)
        {
            // ⚠️ A CHARGE ABILITY SPENDS A CHARGE AND NOTHING ELSE. Setting `CooldownRemaining`
            // as well would put it behind two gates, and the deck would then draw it as Cooling
            // while it still had charges in hand: `Cooldown` is 0 on every charge ability, so
            // this is written as a branch rather than relying on that to stay true.
            if (UsesCharges) ChargesRemaining = Mathf.Max(0, ChargesRemaining - 1);
            else CooldownRemaining = Cooldown;

            // ⚠️ THE RESOURCE IS SPENT AT THE PRESS, BEFORE THE WIND-UP, and that is deliberate:
            // the cast is committed the moment it starts, which is what lets the wind-up be
            // uninterruptible without any refund path. See the block above.
            //
            // ⚠️ NO MOTOR MEANS NO WIND-UP. The EditMode ability tests drive `Activate(null)` and
            // assert the effect synchronously, and a headless harness has nothing to root and no
            // `Tick` loop to finish the cast. Falling straight through keeps those honest.
            if (Windup > 0.0f && ctx != null && ctx.Motor != null)
            {
                WindupRemaining = Windup;

                _rooted = ctx.Motor;
                _rooted.EnterSpeedZone(RootSpeed);
                return;
            }

            DurationRemaining = Duration;
            OnActivate(ctx);
        }

        /// <summary>
        /// Lets the caster move again.
        ///
        /// ⚠️⚠️ EVERY PATH OUT OF A WIND-UP MUST REACH THIS. A speed zone is entered on one
        /// object and exited on another call, so an ability that leaves a wind-up without
        /// exiting leaves the player rooted at 0.0 for the rest of the match with nothing left
        /// to release it. This is the same class of leak `ResetForRound`'s note describes for
        /// Carapace's stun immunity, and it is worse, because a rooted player cannot play at all.
        /// </summary>
        private void ReleaseRoot()
        {
            if (_rooted == null) return;

            _rooted.ExitSpeedZone(RootSpeed);
            _rooted = null;
        }

        public virtual void Tick(AbilityContext ctx, float dt)
        {
            // ⚠️⚠️ THE COOLDOWN IS THE ONLY QUANTITY A MAP MAY SPEED UP, AND IT IS SCALED HERE
            // RATHER THAN AT THE CALL SITE ON PURPOSE. `HeroAbilitySystem` hands `Tick` one `dt`
            // that also drives `DurationRemaining` and, through `HeroKit`, the ultimate charge.
            // Scaling that `dt` would shorten every live effect and fill the meter faster, and
            // `docs/VISION.md` § 4 forbids the second of those outright: a meter that fills on a
            // timer is a reason to stand still. Scaling only the drain pays a player who is
            // already casting, which is the point of the window.
            //
            // Everywhere except under the LRT guideway on Ilalim ng Tulay this is 1.0.
            if (CooldownRemaining > 0.0f)
                CooldownRemaining = Mathf.Max(0.0f, CooldownRemaining - dt * OverheadPassWindow.CooldownRate);

            // ⚠️ THE WIND-UP RUNS BELOW THE COOLDOWN AND ABOVE THE DURATION. The cooldown must
            // keep draining (the ability is spent either way), and the duration must NOT start
            // until the effect actually exists, or a 0.4 s wind-up would silently eat 0.4 s off
            // every ultimate's live time.
            //
            // ⚠️ NOTHING HERE CAN CANCEL IT. There is no stun check and no `CanAct` check on
            // purpose: see the wind-up block at the top of the class.
            if (WindupRemaining > 0.0f)
            {
                WindupRemaining = Mathf.Max(0.0f, WindupRemaining - dt);
                if (WindupRemaining > 0.0f) return;

                ReleaseRoot();

                DurationRemaining = Duration;
                OnActivate(ctx);
                return;
            }

            if (DurationRemaining > 0.0f)
            {
                DurationRemaining = Mathf.Max(0.0f, DurationRemaining - dt);
                OnTick(ctx, dt);

                if (DurationRemaining <= 0.0f)
                    OnEnd(ctx);
            }
        }

        public virtual bool CanReactivate => false;

        public virtual void Reactivate(AbilityContext ctx)
        {
            EndEarly(ctx);
        }

        public void EndEarly(AbilityContext ctx)
        {
            if (DurationRemaining > 0.0f)
            {
                DurationRemaining = 0.0f;
                OnEnd(ctx);
            }
        }

        public virtual void Reset()
        {
            // ⚠️ THE ROOT COMES OFF FIRST. A round can end mid-wind-up, and `Reset` zeroing the
            // timer behind the wind-up's back would strand the speed zone with nothing left to
            // release it. See `ReleaseRoot`.
            ReleaseRoot();
            WindupRemaining = 0.0f;

            CooldownRemaining = 0.0f;
            DurationRemaining = 0.0f;

            // ⚠️⚠️ CHARGES REFILL AT A ROUND BOUNDARY AND ONLY THERE. 🧑 2026-08-25: *"a cooldown
            // instead of charges that reset each round"*. Four rounds is four fresh sets, so a
            // player who spends everything in round one is not punished for the rest of the
            // match, and one who hoards gains nothing by carrying them over. That symmetry is
            // what stops the charge economy from rewarding a quiet round, which
            // `docs/VISION.md` § 4 forbids.
            ChargesRemaining = MaxCharges;
        }

        /// <summary>
        /// The round-boundary reset: end cleanly first, THEN zero.
        ///
        /// ⚠⚠ THE PARAMETERLESS `Reset` DROPS `DurationRemaining` WITHOUT RUNNING `OnEnd`,
        /// and for the abilities that grant something for a duration that is a leak, not a
        /// reset. Demonic Carapace hands out stun immunity in `OnActivate` and takes it back in
        /// `OnEnd`; Phantom Phase does the same for tag immunity. Zeroing the timer behind their
        /// backs at a round boundary leaves the grant switched on with no timer left to switch
        /// it off, so a hero who happened to be mid-Carapace when the round ended would start
        /// the next one permanently unstunnable.
        ///
        /// `EndEarly` is a no-op when nothing is running, so this is safe on every ability.
        /// </summary>
        public void ResetForRound(AbilityContext ctx)
        {
            EndEarly(ctx);
            Reset();
        }

        protected virtual void OnActivate(AbilityContext ctx) { }
        protected virtual void OnTick(AbilityContext ctx, float dt) { }
        protected virtual void OnEnd(AbilityContext ctx) { }
    }
}
