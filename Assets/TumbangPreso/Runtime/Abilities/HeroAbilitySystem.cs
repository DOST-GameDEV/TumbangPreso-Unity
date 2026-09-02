using System;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Component attached to a character in Hero Strike mode that drives active hero abilities,
    /// cooldowns, inputs, and ultimate meter.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class HeroAbilitySystem : MonoBehaviour
    {
        public HeroKit Kit { get; private set; }
        public string HeroId { get; private set; } = "dante";
        private AbilityVariant _skill1Variant;
        private AbilityVariant _skill2Variant;
        private CharacterMotor _motor;
        private Carrier _carrier;
        private CombatVerbs _verbs;
        private AbilityContext _context;

        // Phantom Phase is an approach/escape tool, not a risk-free objective carry.
        // Picking up a slipper immediately restores tag vulnerability.
        public bool IsImmuneToTags => Kit is NemuHeroKit nemu
            && nemu.IsPhantomPhaseActive
            && (_motor == null || !_motor.HoldingSlipper);
        public bool IsImmuneToStuns => Kit is DanteHeroKit dante && dante.IsDemonicCarapaceActive;

        private GroundReticle _reticle;

        // -------------------------------------------------------------------
        // § THE INPUT BUFFER
        //
        // ⚠️⚠️ A HERO KEY PRESSED WHILE THE PLAYER COULD NOT ACT USED TO BE EATEN OUTRIGHT, AND
        // THAT IS MOST OF WHAT "CLUNKY" MEANT. `Update` returned before it had even LOOKED at
        // the intent table whenever `CanAct()` was false, and `JustPressed` is a one-frame edge
        // against the snapshot `CharacterMotor.FixedUpdate` takes, so the press was gone by the
        // next frame with nothing anywhere recording that it happened.
        //
        // The states that swallow presses are not rare, they are the whole game under pressure:
        // the taya's tag is a FIVE SECOND stun, a shove staggers, Zack's shock trail and Sean's
        // fire trail both pulse 0.20 to 0.35 s staggers, and Cheska's nova freezes for 2.5. A
        // player mashing Q to get out of trouble was pressing into a hole.
        //
        // ⚠️ 0.30 s, AND THE NUMBER IS BOUNDED AT BOTH ENDS. Long enough to cover a stagger,
        // which is the 0.20 to 0.35 s band above, so the common case of "shoved mid-cast" comes
        // out the far side. Short enough that a press made a second ago cannot fire on its own
        // later, which is the failure buffering usually introduces: an ability that goes off
        // at a moment the player did not choose is worse than one that did not go off at all.
        //
        // ⚠️⚠️ ONLY `CannotAct` IS HELD. A cast refused for cooldown or for charge is ANSWERED
        // and CLEARED. Holding a cooldown refusal would mean the skill fires the instant it
        // comes back up, seconds after the ask, which is the same bug wearing a helpful face.
        // -------------------------------------------------------------------

        /// <summary>How long a press survives a state that cannot act on it. Seconds.</summary>
        public const float InputBufferWindow = 0.30f;

        private float _skill1BufferedAt = float.NegativeInfinity;
        private float _skill2BufferedAt = float.NegativeInfinity;
        private float _ultimateBufferedAt = float.NegativeInfinity;

        // -------------------------------------------------------------------
        // § THE CAST ANSWER, WHICH THE HUD READS
        //
        // ⚠️⚠️ A PRESS THAT WAS REFUSED USED TO LOOK EXACTLY LIKE A PRESS THAT WORKED, AND THAT
        // IS THE OTHER HALF OF "CLUNKY". Press Q on cooldown and the game did nothing at all:
        // no flash, no tick, no movement on the tile. The only honest reading available to the
        // player was that their input had been dropped, and they were wrong, and there was
        // nothing on screen that could have told them so.
        //
        // ⚠️ POLLED, NOT AN EVENT, FOR THE REASON `CameraRig` GIVES ABOUT THE VIEWMODEL. The HUD
        // rebuilds its three tiles every frame from live state anyway; a stamp it can read is
        // one field, while an event needs a subscription that has to survive the round swap
        // that rebuilds the local player's HUD binding.
        // -------------------------------------------------------------------

        /// <summary>Which of the three tiles a cast answer belongs to.</summary>
        public enum Slot { Skill1, Skill2, Ultimate }

        private readonly float[] _answeredAt = { float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity };
        private readonly HeroKit.CastOutcome[] _answer =
        {
            HeroKit.CastOutcome.Missing, HeroKit.CastOutcome.Missing, HeroKit.CastOutcome.Missing,
        };

        /// <summary>
        /// How long ago this slot last answered a press, in seconds, or a large number if it
        /// never has. The HUD turns this into a 0.14 s confirm flash or a 0.12 s refusal tick.
        /// </summary>
        public float SecondsSinceAnswer(Slot slot) => Time.time - _answeredAt[(int)slot];

        /// <summary>What that answer was.</summary>
        public HeroKit.CastOutcome LastAnswer(Slot slot) => _answer[(int)slot];

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            _carrier = GetComponent<Carrier>();
            _verbs = GetComponent<CombatVerbs>();
            _context = new AbilityContext(_motor, _carrier, _verbs);
            _reticle = GroundReticle.Create(transform);
        }

        public void BindHero(string heroId, HeroBuild build = null)
        {
            HeroId = string.IsNullOrEmpty(heroId) ? "dante" : heroId.ToLowerInvariant();
            Kit = CreateKitFor(HeroId);
            _skill1Variant = HeroBuildRules.Equipped(build, HeroId, 1, null)
                             ?? HeroLoadoutRules.DefaultFor(HeroId, 1);
            _skill2Variant = HeroBuildRules.Equipped(build, HeroId, 2, null)
                             ?? HeroLoadoutRules.DefaultFor(HeroId, 2);
            ApplyLoadoutToPresentation();
        }

        /// <summary>
        /// The telegraph, the reticle and the aim beacon, moved by the same fraction the effect
        /// itself is moved by.
        ///
        /// ⚠️⚠️ EVERY NUMBER HERE IS READ OFF THE VARIANT, NEVER WRITTEN AS A LITERAL. The first
        /// draft of this method hard-coded `1.25f` beside `dante.1.tremor` while the blast at the
        /// call site used `ctx.GainScale`, which is the same fraction from the table. They agreed
        /// on the day they were typed and nothing kept them agreeing: raising Arc Line from 30 to
        /// 45 per cent would have moved the shock and left the ring the player aims with at 30,
        /// so the telegraph would draw a lie. `docs/Design.md`'s opening rule as code: a number
        /// in one place or it is two numbers.
        /// </summary>
        private void ApplyLoadoutToPresentation()
        {
            if (Kit == null) return;

            // ⚠️⚠️ THE ALTERNATE'S SOUND IS ASSIGNED HERE AND CLEARED HERE, IN THAT ORDER, SO A
            // LOADOUT CHANGE CANNOT LEAVE A STALE ONE ON THE KIT. A sidegrade changes what the
            // power does, so it changes what the power sounds like doing it: Long Tremor sweeps
            // feet instead of throwing bodies and its cue has no vertical slam in it.
            // `tools/generate_skill_audio.py` builds the twelve, each by a different method.
            //
            // ⚠️ A DEFAULT VARIANT HAS NO ROW HERE AND MUST NOT. `VariantCastCue` goes back to
            // null for it, so `HeroAbility.EffectiveCastCue` falls through to the slot's own cue
            // — which is the whole point of "as tuned".
            if (Kit.Skill1 != null) Kit.Skill1.VariantCastCue = VariantCue(_skill1Variant?.Id);
            if (Kit.Skill2 != null) Kit.Skill2.VariantCastCue = VariantCue(_skill2Variant?.Id);

            switch (_skill1Variant?.Id)
            {
                // The break grows, so the ring the player stomps inside grows with it.
                case "dante.1.tremor":
                    Kit.Skill1?.ScaleLoadout(telegraphRadius: Gain("dante.1.tremor")); break;

                // A smaller sheet: the ring is the sheet, so it shrinks by the cost.
                case "cheska.1.blackice":
                    Kit.Skill1?.ScaleLoadout(telegraphRadius: Cost("cheska.1.blackice")); break;

                // A longer veil is a longer ability, and the deck's cooldown ring reads it.
                case "nemu.1.fade":
                    Kit.Skill1?.ScaleLoadout(duration: Gain("nemu.1.fade")); break;

                case "phaister.1.brand":
                    Kit.Skill1?.ScaleLoadout(telegraphRadius: Cost("phaister.1.brand")); break;
            }

            switch (_skill2Variant?.Id)
            {
                case "dante.2.plating":
                    Kit.Skill2?.ScaleLoadout(duration: Gain("dante.2.plating")); break;

                case "cheska.2.spires":
                    Kit.Skill2?.ScaleLoadout(telegraphRadius: Gain("cheska.2.spires")); break;

                // ⚠️ THE ARMED WINDOW IS THE COST ON BOTH OF THESE. Flare Shot and Snap Discharge
                // both buy speed with how long the shoe stays armed after the cast, and that
                // window IS `HeroAbility.Duration`: the deck bar draining faster is the whole
                // warning a player gets that they have to throw now.
                case "sean.2.flare":
                    Kit.Skill2?.ScaleLoadout(duration: Cost("sean.2.flare")); break;

                case "zack.2.discharge":
                    Kit.Skill2?.ScaleLoadout(duration: Cost("zack.2.discharge")); break;

                case "nemu.2.leash":
                    Kit.Skill2?.ScaleLoadout(duration: Cost("nemu.2.leash")); break;

                // ⚠️ LONG STRIDE IS THE ONE ROW WHOSE COST IS A DIVISION. It goes further (the
                // gain scales the reach) and takes proportionally LONGER to ramp to the far mark,
                // so the ramp is divided by the cost rather than multiplied by it. The player
                // sees both in the live aim beacon before releasing, which is what makes it a
                // read for the taya rather than a free upgrade.
                case "phaister.2.stride":
                    Kit.Skill2?.ScaleLoadout(telegraphRange: Gain("phaister.2.stride"),
                                             aimMax: Gain("phaister.2.stride"),
                                             aimRamp: 1.0f / Cost("phaister.2.stride"));
                    break;
            }
        }

        /// <summary>
        /// The cast sound one loadout alternate wants, or null for a default.
        ///
        /// ⚠️ A SWITCH RATHER THAN A NAME DERIVED FROM THE ID. `"zack.2.discharge"` to
        /// `"sfx_var_zack_discharge"` is a string transform that works for all twelve rows today
        /// and would silently produce a cue id with no file behind it the first time somebody
        /// added a variant whose tail did not match its wav. `AudioCues`'s own header records
        /// what an unregistered id costs: the sound simply does not play and nothing errors.
        /// Twelve explicit rows fail at compile time instead.
        /// </summary>
        private static string VariantCue(string variantId)
        {
            switch (variantId)
            {
                case "dante.1.tremor": return "sfx_var_dante_tremor";
                case "dante.2.plating": return "sfx_var_dante_plating";
                case "cheska.1.blackice": return "sfx_var_cheska_blackice";
                case "cheska.2.spires": return "sfx_var_cheska_spires";
                case "sean.1.afterburn": return "sfx_var_sean_afterburn";
                case "sean.2.flare": return "sfx_var_sean_flare";
                case "zack.1.arcline": return "sfx_var_zack_arcline";
                case "zack.2.discharge": return "sfx_var_zack_discharge";
                case "nemu.1.fade": return "sfx_var_nemu_fade";
                case "nemu.2.leash": return "sfx_var_nemu_leash";
                case "phaister.1.brand": return "sfx_var_phaister_brand";
                case "phaister.2.stride": return "sfx_var_phaister_stride";
                default: return null;
            }
        }

        /// <summary>Whether this body is running one named alternate.</summary>
        public bool HasVariant(string variantId)
            => (!string.IsNullOrEmpty(variantId))
               && ((_skill1Variant != null && _skill1Variant.Id == variantId)
                   || (_skill2Variant != null && _skill2Variant.Id == variantId));

        /// <summary>
        /// 1 plus the authored gain of a variant, whether or not this body is wearing it.
        ///
        /// ⚠️ THESE TWO ARE THE TABLE'S OWN ARITHMETIC AND ARE DELIBERATELY UNCONDITIONAL. They
        /// are only ever called from inside a `case` that has already matched the equipped id, so
        /// an ownership test here would be a second, weaker copy of the switch above.
        /// </summary>
        private static float Gain(string variantId)
        {
            var variant = HeroLoadoutRules.VariantById(variantId);
            return variant == null ? 1.0f : 1.0f + variant.Gain;
        }

        /// <summary>1 plus the authored (negative) cost of a variant. See <see cref="Gain"/>.</summary>
        private static float Cost(string variantId)
        {
            var variant = HeroLoadoutRules.VariantById(variantId);
            return variant == null ? 1.0f : 1.0f + variant.Cost;
        }

        /// <summary>
        /// The gain fraction for a variant if this body is wearing it, otherwise 1. For effect
        /// code that holds the system rather than an <see cref="AbilityContext"/>: `Carrier`
        /// scales a throw, `Slipper` scales the crater it makes on landing.
        /// </summary>
        public float VariantGain(string variantId)
            => HasVariant(variantId) ? Gain(variantId) : 1.0f;

        /// <summary>The cost fraction for a variant if this body is wearing it, otherwise 1.</summary>
        public float VariantCost(string variantId)
            => HasVariant(variantId) ? Cost(variantId) : 1.0f;

        /// <summary>The checked variant in one skill slot, defaults included.</summary>
        public AbilityVariant VariantFor(int slot) => slot == 1 ? _skill1Variant : _skill2Variant;

        public static HeroKit CreateKitFor(string heroId)
        {
            if (string.IsNullOrEmpty(heroId)) return new DanteHeroKit();

            switch (heroId.ToLowerInvariant())
            {
                case "dante":
                case "bayan":
                    return new DanteHeroKit();

                case "cheska":
                case "inday":
                    return new CheskaHeroKit();

                case "sean":
                case "kuya_boy":
                case "iggy":
                    return new SeanHeroKit();

                case "zack":
                    return new ZackHeroKit();

                case "nemu":
                    return new NemuHeroKit();

                case "phaister":
                    return new PhaisterHeroKit();

                default:
                    return new DanteHeroKit();
            }
        }

        private void Update()
        {
            if (Kit == null || _motor == null) return;

            float dt = Time.deltaTime;

            // ⚠️⚠️ PRACTICE IS "THE ROUND CLOCK IS NOT RUNNING", asked of the rules rather than
            // tracked here. `RoundDirector.RoundActive` is the same flag that gates scoring and
            // `CanAct`, so the ultimate economy pauses in exactly the periods the scoreboard
            // already calls paused: the warm-up before round one and the buffer between rounds.
            // A second opinion about when a round is live is how a HUD ends up disagreeing with
            // the rules.
            //
            // ⚠️⚠️ AND THE OFFLINE TEST BENCH IS THE ONE THING THAT MAY SAY OTHERWISE. See
            // `PracticeSandbox`: the switch is off by default, unreachable in any networked
            // session, and its whole purpose is casting during the window this line closes. It
            // is anded in HERE, on the one assignment, rather than at each of the three places
            // `PracticeMode` is read, so a future reader cannot be given a different answer
            // from the HUD tile beside it.
            Kit.PracticeMode = !PracticeSandbox.Active
                               && (GameServices.Round == null || !GameServices.Round.RoundActive);

            // ⚠️ THE REFILL IS BEFORE `Tick`, NOT AFTER IT. `Kit.Tick` is what runs an active
            // ability's duration down and what `OnEnd` hangs off; zeroing cooldowns after it
            // would be the same frame's work undone, and a duration cleared from out here would
            // strand a grant switched on with no timer left to switch it off. Only the two
            // numbers that gate a NEW cast are touched.
            if (PracticeSandbox.Active) RefillForSandbox();

            if (NetAuthority.IsNetworked)
            {
                using (NetCue.SuppressRelay()) Kit.Tick(_context, dt);
            }
            else
            {
                Kit.Tick(_context, dt);
            }

            var intent = _motor.Intent;
            if (intent == null)
            {
                if (_reticle != null) _reticle.Hide();
                return;
            }

            // ⚠️⚠️ THE PRESS IS TAKEN BEFORE ANYTHING IS GATED ON `CanAct()`, AND THE ORDER IS
            // THE FIX. The old code returned above this line while stunned, so the edge died
            // unread. Recording it first means a stun can DELAY a cast; it can no longer
            // DISAPPEAR one.
            Aim(intent, Verb.Skill1, Slot.Skill1, ref _skill1BufferedAt);
            Aim(intent, Verb.Skill2, Slot.Skill2, ref _skill2BufferedAt);
            Aim(intent, Verb.Ultimate, Slot.Ultimate, ref _ultimateBufferedAt);

            UpdateReticle(intent);

            ServiceBuffer(ref _skill1BufferedAt, Slot.Skill1);
            ServiceBuffer(ref _skill2BufferedAt, Slot.Skill2);
            ServiceBuffer(ref _ultimateBufferedAt, Slot.Ultimate);

            _reticle?.Tick(dt);
        }

        // -------------------------------------------------------------------
        // § AIMING BY HOLDING
        //
        // ⚠️⚠️ IT IS THE SAME BUFFER, FED FROM A DIFFERENT EDGE, AND THAT IS WHY IT IS SIX LINES
        // RATHER THAN A SECOND INPUT PATH. Everything above this point already handles a cast
        // that cannot resolve on the frame it was asked for: the press is recorded, retried for
        // 0.30 s, and answered once. A hold-to-aim ability changes exactly one thing, which edge
        // writes the stamp, so it writes the same stamp on RELEASE instead of on press and the
        // whole buffering, refusal and confirmation story is inherited unchanged.
        //
        // ⚠️ THE ABILITY DECIDES, NOT THE SLOT. `HeroAbility.HoldToAim` is per ability, so Q and
        // R keep firing on the press edge while E waits for the release, on the same hero, in the
        // same frame. A slot-keyed rule would have made "the second skill is the holdable one" a
        // fact about the game.
        //
        // ⚠️⚠️ AND A HOLD THAT REACHES THE CEILING FIRES RATHER THAN CANCELS. `docs/VISION.md`
        // § 4 forbids anything that rewards waiting, and a hold that could be held forever is a
        // player standing still with an escape half-pressed all round. Firing at the cap removes
        // the incentive without ever eating an input the player meant.
        // -------------------------------------------------------------------

        /// <summary>How long each slot's key has been down, or a negative number if it is not.</summary>
        private readonly float[] _heldSince = { -1.0f, -1.0f, -1.0f };

        /// <summary>Seconds the current hold has lasted, or 0 when nothing is being aimed.</summary>
        public float HeldSeconds(Slot slot)
        {
            float since = _heldSince[(int)slot];
            return since < 0.0f ? 0.0f : Time.time - since;
        }

        /// <summary>True while this slot is being aimed rather than cast.</summary>
        public bool IsAiming(Slot slot)
        {
            var ability = AbilityFor(slot);
            return ability != null && ability.HoldToAim && _heldSince[(int)slot] >= 0.0f;
        }

        // -------------------------------------------------------------------
        // § THE PRESS EDGE IS THIS COMPONENT'S OWN
        //
        // ⚠️⚠️ `InputIntent.JustPressed` IS A DIFF AGAINST A SNAPSHOT TAKEN IN THE PHYSICS STEP,
        // AND THIS METHOD RUNS IN `Update`. `CharacterMotor` calls `Intent.CommitFrame` at the
        // end of its FixedUpdate and its own note says that is deliberate. Above 50 fps there
        // are two or more Updates per FixedUpdate, so ONE physical key press reads as
        // `JustPressed` on EVERY Update until the next physics step takes a snapshot.
        //
        // ⚠️⚠️ FOR AN ORDINARY ABILITY THAT IS A HARMLESS RE-BUFFER OF THE SAME PRESS. For the
        // one `CanReactivate` power in the game it is the whole bug: `HeroKit.Fire` ACTIVATES on
        // the first read and REACTIVATES on the second, so Nemu's poltergeist went out and came
        // straight back inside a single click and nothing appeared to happen at all. 🧑
        // 2026-08-27: *"her e is kind of bugged, sometimes it doesnt cast sometimes it does? idk
        // why"*. **Sometimes is exactly right**: whether a FixedUpdate landed between two Updates
        // is a function of the frame rate, so the identical click worked or did not depending on
        // how busy the machine was.
        //
        // ⚠️ IT ALSO SHORTENED EVERY HOLD. `_heldSince` was rewritten on each Update the stale
        // edge was still true, so a hold-to-aim power lost up to a physics step of reach.
        //
        // ⚠️ DO NOT "SIMPLIFY" THIS BACK TO `intent.JustPressed`. The snapshot belongs to the
        // authoritative step and has to keep belonging to it; a consumer that runs on the RENDER
        // frame needs a render-frame edge of its own. `HeroAbilitySystemEdgeTests` asserts one
        // press produces one cast across several Updates with no physics step between them.
        // -------------------------------------------------------------------

        /// <summary>Was this slot's key down on the previous Update? One entry per slot.</summary>
        private readonly bool[] _keyWasDown = { false, false, false };

        /// <summary>
        /// The offline test bench's per-frame refill: no cooldowns, no charge cost, a full
        /// ultimate meter.
        ///
        /// ⚠️ THE METER IS TOPPED UP RATHER THAN MADE FREE, so `CastUltimate` still runs its
        /// real branch order and still spends the bank. A player testing an ultimate in the
        /// sandbox sees the same refusal messages a match would give them for everything except
        /// the two things the switch is for.
        ///
        /// ⚠️ IT IS NOT ROUTED THROUGH `HeroKit`, because a kit method called from `Tick` would
        /// be one an override could inherit and a shipped hero could accidentally use. This is a
        /// caller-side effect on a switch this class already reads.
        /// </summary>
        private void RefillForSandbox()
        {
            Kit.Skill1?.RefillForSandbox();
            Kit.Skill2?.RefillForSandbox();
            Kit.Ultimate?.RefillForSandbox();
            Kit.AddUltimateCharge(Kit.UltimateCost);
        }

        private void Aim(InputIntent intent, Verb verb, Slot slot, ref float bufferedAt)
        {
            var ability = AbilityFor(slot);
            int i = (int)slot;

            bool down = intent.Pressed(verb);
            bool justPressed = down && !_keyWasDown[i];
            bool justReleased = !down && _keyWasDown[i];
            _keyWasDown[i] = down;

            if (ability == null || !ability.HoldToAim)
            {
                _heldSince[i] = -1.0f;
                if (justPressed) bufferedAt = Time.time;
                return;
            }

            if (justPressed)
            {
                _heldSince[i] = Time.time;
                return;
            }

            if (_heldSince[i] < 0.0f) return;

            // ⚠️⚠️ LOSING THE RIGHT TO ACT CANCELS THE AIM, IT DOES NOT FIRE IT. Getting tagged
            // or shoved mid-aim must not spend the ability, and this branch is also what makes
            // the aim safe against `InputIntent.Clear`. `docs/TODO.md` § 22's trap list records
            // that `ReleaseAll` calls `Clear()`, *"so anything reading `intent.Pressed()` right
            // after gets false forever"*: treating a cleared table as a release would turn every
            // round transition into a free blink in a direction nobody chose.
            if (_motor == null || !_motor.CanAct())
            {
                _heldSince[i] = -1.0f;
                return;
            }

            float held = Time.time - _heldSince[i];

            // ⚠️ THE RELEASE IS CHECKED BEFORE THE CEILING, so a player who lets go on the exact
            // frame the cap lands gets one cast rather than two stamps a frame apart.
            //
            // ⚠️⚠️ AND AN ABILITY MAY DECLINE THE CEILING ENTIRELY. `HeroAbility.MaxAimSeconds`
            // of 0 means "only the release casts this", which 🧑 asked for on the blink after
            // playing it: *"u cant control the E of phaister and it autocasts after some
            // seconds, i want it to cast only when i let go"*. See that property's note for why
            // it does not reopen `docs/VISION.md` § 4: the reach stops growing at 0.55 s, so a
            // longer hold pays out nothing, and the anti-camp clocks never stopped running.
            bool released = justReleased;
            bool capped = !ability.CastsOnReleaseOnly && held >= ability.MaxAimSeconds;

            if (!released && !capped) return;

            // ⚠️ THE CLAMP IS AGAINST THE RAMP WHEN THERE IS NO CEILING. Clamping to
            // `MaxAimSeconds` would clamp to ZERO on a release-only power and hand every blink
            // the minimum 2.0 m, which is the same fault § HOLDING A HOLD-TO-AIM POWER records
            // against a bot's one-frame tap, arriving from the opposite direction.
            float aimCap = ability.CastsOnReleaseOnly ? ability.AimRampSeconds : ability.MaxAimSeconds;
            ability.HeldSecondsOnCast = Mathf.Min(held, aimCap);
            _heldSince[i] = -1.0f;
            bufferedAt = Time.time;
        }

        /// <summary>
        /// Retry one buffered press, and decide whether it stays buffered.
        ///
        /// ⚠️ EXPIRY IS SILENT ON PURPOSE. A press that spent its whole window inside a five
        /// second stun gets no red tick when it lapses, because by then the player has long
        /// since pressed again and a tick would be answering an input they have forgotten
        /// making. The stun itself is the feedback, and it is already the loudest thing on
        /// their screen.
        /// </summary>
        private void ServiceBuffer(ref float bufferedAt, Slot slot)
        {
            if (float.IsNegativeInfinity(bufferedAt)) return;

            if (Time.time - bufferedAt > InputBufferWindow)
            {
                bufferedAt = float.NegativeInfinity;
                return;
            }

            var outcome = Cast(slot);

            // Still not able to act. Keep it and try again next frame, until the window closes.
            if (outcome == HeroKit.CastOutcome.CannotAct) return;

            bufferedAt = float.NegativeInfinity;

            // ⚠️ `Missing` DRAWS NOTHING. A hero without a second skill is not refusing the
            // player anything, and flashing a tile that is not there would be a lie about a
            // power they do not have.
            if (outcome == HeroKit.CastOutcome.Missing) return;

            _answer[(int)slot] = outcome;
            _answeredAt[(int)slot] = Time.time;

            if (outcome == HeroKit.CastOutcome.Cast) PlayCastConfirm(slot);
            else PlayRefusal();
        }

        private HeroKit.CastOutcome Cast(Slot slot)
        {
            HeroKit.CastOutcome outcome;
            if (NetAuthority.IsNetworked)
            {
                using (NetCue.SuppressRelay()) outcome = CastWithContext(slot, _context);
            }
            else
            {
                outcome = CastWithContext(slot, _context);
            }
            if (outcome != HeroKit.CastOutcome.Cast || !NetAuthority.IsNetworked) return outcome;

            var ability = AbilityFor(slot);
            float held = ability != null ? ability.HeldSecondsOnCast : 0.0f;
            Vector3 aimPoint = _context.AimPoint;

            if (NetAuthority.IsHost)
            {
                Net.MatchRpc.Instance?.BroadcastAbilityCast(
                    _motor.PlayerSlot, (int)slot, _context.Position, _context.Forward,
                    aimPoint, held, exceptClientId: null);
            }
            else if (_motor.PlayerSlot == NetAuthority.LocalSlot)
            {
                Net.MatchRpc.Instance?.RequestAbilityCastServerRpc(
                    _motor.PlayerSlot, (int)slot, _context.Position, _context.Forward,
                    aimPoint, held);
            }

            return outcome;
        }

        private HeroKit.CastOutcome CastWithContext(Slot slot, AbilityContext context)
        {
            switch (slot)
            {
                case Slot.Skill1: return Kit.CastSkill1(context);
                case Slot.Skill2: return Kit.CastSkill2(context);
                default: return Kit.CastUltimate(context);
            }
        }

        /// <summary>
        /// Runs a host-approved cast on the host's remote seat or on an observing client.
        /// The caller decides whether normal eligibility must pass. Observers force the replay
        /// if their cosmetic cooldown drifted, because a host-approved effect must never vanish
        /// merely because one screen counted a timer a frame differently.
        /// </summary>
        public HeroKit.CastOutcome ApplyNetworkCast(Slot slot, Vector3 position,
                                                    Vector3 forward, Vector3 aimPoint,
                                                    float heldSeconds, bool authoritative)
        {
            if (Kit == null || _motor == null) return HeroKit.CastOutcome.Missing;

            var ability = AbilityFor(slot);
            if (ability == null) return HeroKit.CastOutcome.Missing;

            ability.HeldSecondsOnCast = Mathf.Max(0.0f, heldSeconds);
            var context = new AbilityContext(_motor, _carrier, _verbs,
                                             position, forward, aimPoint);

            HeroKit.CastOutcome outcome;
            using (NetCue.SuppressRelay())
            {
                outcome = CastWithContext(slot, context);
                if (!authoritative && outcome != HeroKit.CastOutcome.Cast)
                {
                    if (ability.IsActive && ability.CanReactivate)
                        ability.Reactivate(context);
                    else
                        ability.Activate(context);
                    outcome = HeroKit.CastOutcome.Cast;
                }
            }

            if (outcome == HeroKit.CastOutcome.Cast) PlayCastConfirm(slot);
            return outcome;
        }

        private void PlayCastConfirm(Slot slot)
        {
            var animator = GetComponentInChildren<Visual.CharacterAnimator>();
            var ability = AbilityFor(slot);

            // ⚠️⚠️ EVERY CAST SOUNDS FROM HERE AND FROM NOWHERE ELSE. See `HeroAbility.CastCue`
            // for what this replaces: eighteen powers opening on six shared element cues, so a
            // press told the room which hero and never which power. Playing it centrally rather
            // than from each `OnActivate` is the same argument `Glyph` and `TelegraphRadius` won
            // — eighteen call sites is eighteen places to forget, and a new hero would compile,
            // run and cast in silence.
            //
            // ⚠️⚠️ IT REACHES EVERYBODY BY BEING PLAYED ON EVERY PEER, NOT BY BEING RELAYED, AND
            // THE `SuppressRelay` IS LOAD-BEARING. 🧑 2026-09-02: *"make sure sfx can be heard by
            // everyone in all modes / not js client sided"*. This method is called on the caster
            // AND, through `ApplyNetworkCast`, on the host's copy of that seat and on every
            // observing client — that is how the animation and the ground flash already reach the
            // room. A cue that ALSO relayed from each of those would be broadcast once per peer:
            // four players means four copies of one cast, a few tens of milliseconds apart, which
            // is the flam `sfx_lrt_rumble`'s deletion records. Suppressed, every machine plays it
            // locally exactly once, and offline the suppression does nothing at all.
            //
            // ⚠️ `EffectiveCastCue` PICKS THE LOADOUT ALTERNATE'S SOUND WHEN ONE IS EQUIPPED, and
            // exactly one of the two ever plays, for the same reason.
            //
            // ⚠️ AN EMPTY CUE IS SILENCE AND NOT AN ERROR. `NetCue.Play` is simply not called,
            // rather than being handed an id with no file behind it.
            if (ability != null && !string.IsNullOrEmpty(ability.EffectiveCastCue))
            {
                using (NetCue.SuppressRelay())
                    NetCue.Play(ability.EffectiveCastCue, transform.position);
            }

            // ⚠️⚠️ COUNT THE CAST ONLY ON THE OWNER. `ApplyNetworkCast` also calls this method on
            // observers, so counting every presentation would award one step per connected peer.
            // Practice has no transport and uses `GameLaunch.SoloSeat`; a bot never earns a
            // player's local unlock even when all-bots diagnostics are running.
            bool localOwner = !NetAuthority.IsNetworked
                ? _motor != null && !_motor.IsBot && _motor.PlayerSlot == GameLaunch.SoloSeat
                : _motor != null && !_motor.IsBot && _motor.PlayerSlot == NetAuthority.LocalSlot;
            if (localOwner && (slot == Slot.Skill1 || slot == Slot.Skill2))
                Settings.SettingsStore.NoteAbilityCast(HeroId, slot == Slot.Skill1 ? 1 : 2);

            string castClip = ability?.CastAction;
            string vmClip = ability?.ViewmodelAction;

            if (string.IsNullOrEmpty(castClip))
            {
                castClip = slot == Slot.Skill1 ? "dash" : slot == Slot.Skill2 ? "shove" : "jump";
            }
            if (string.IsNullOrEmpty(vmClip))
            {
                vmClip = slot == Slot.Skill1 ? "thrust" : slot == Slot.Skill2 ? "cast" : "slam";
            }

            animator?.PlayAction(castClip, vmClip);

            if (slot == Slot.Ultimate)
            {
                PlayUltimatePresentation();
            }

            // Visual feedback: momentary cast flash
            Visual.AbilityVfx.SpawnCastFlash(transform.position, AccentColour(),
                ability != null && ability.HasTelegraph ? Mathf.Min(ability.TelegraphRadius, 2.5f) : 1.8f);

            // ⚠️⚠️ THE GROUND CONFIRM EXISTS BECAUSE THE PRE-CAST RING WAS UNREACHABLE FOR EVERY
            // TAP. Every one of these powers fires on the press edge and resolves instantly, so
            // the reticle drawn while the key is HELD appears on the same frame the ability goes
            // off and is gone again the moment the finger lifts, which on a tap is one or two
            // frames. A telegraph nobody can see is not a telegraph. Holding it for 0.35 s after
            // the cast turns it into what it should have been all along: the answer to "where
            // did that land", which is the question a player actually has.
            if (_reticle == null || ability == null || !ability.HasTelegraph) return;

            _reticle.SetStyle(ability.TelegraphStyle);

            // ⚠️⚠️ A HOLD-TO-AIM POWER'S CONFIRM GOES WHERE IT WAS AIMED, NOT AT ITS MAXIMUM
            // REACH. `TelegraphCentre` is `Position + Forward * TelegraphRange`, and
            // `TelegraphRange` is the FURTHEST an aimed power can be thrown, so a hex placed at
            // the near end of the ramp flashed its confirmation several metres past where the
            // ward actually landed. That is the exact fault this class's own header calls out by
            // name: a telegraph that lies is worse than no telegraph, because a player believes
            // it once and then stops believing all of them. `AimDestination` reads the same
            // `HeldSecondsOnCast` the kit read when it spawned the thing.
            Vector3 landed = ability.HoldToAim
                ? AimDestination(ability)
                : TelegraphCentre(ability);

            _reticle.Flash(landed, ability.TelegraphRadius, AccentColour(), 0.35f);
        }

        /// <summary>
        /// The host refused a cast this peer had already predicted. Take it back.
        ///
        /// ⚠️⚠️ THE SILENT DROP IS THE BUG THIS CLOSES, NOT THE DROP ITSELF. `MatchRpc
        /// .OnReqAbilityMsg` has six ways to refuse a request and every one of them was a bare
        /// `return`: the wrong seat, a non-finite number, no kit, an ability slot out of range,
        /// the host's own `ApplyNetworkCast` saying no, and `PlausibleIntentPose` saying the pose
        /// the client claims is more than `IntentPoseLeeway` from the host's copy of that body.
        /// The guards are right. Refusing without telling anybody is what left the client running
        /// a match the host was not refereeing: it had spent the cooldown, drawn the effect and
        /// heard the confirm for something that, on the machine that decides, did not happen.
        ///
        /// ⚠️⚠️ AND `PlausibleIntentPose` IS NOT A RARE PATH. `docs/TODO.md` § 71.3 measured why:
        /// 2.25 m of leeway is 375 ms at a sprint, and while the pose stream went out reliably a
        /// single lost packet head-of-line blocked it for longer than that. The transport half of
        /// that entry should make this uncommon. Uncommon is not the same as correct, and a
        /// player who loses an ultimate to one dropped packet has lost the round it was banked
        /// for.
        ///
        /// ⚠️ IT ANSWERS WITH THE REFUSAL THE PLAYER ALREADY KNOWS. `PlayRefusal` is the same
        /// quiet `ui_error` a cooldown press gets, and `_answer` is set to `Cooling` so the deck
        /// flashes the tile exactly as it does for any other refused press. The player does not
        /// need to be told the difference between "the host said no" and "it was not ready"; they
        /// need the power back and one unmistakable beat saying it did not go off.
        ///
        /// ⚠️ ONLY THE OWNER MAY RUN THIS, AND THE WIRE ENFORCES IT FROM THE OTHER END TOO.
        /// `OnCastDeniedMsg` refuses anything that is not the host speaking and refuses a slot
        /// that is not this peer's own; the guard here is the second half of that pair, so a
        /// future caller cannot roll back somebody else's kit.
        /// </summary>
        public void RollBackPredictedCast(Slot slot)
        {
            if (Kit == null || _motor == null) return;
            if (NetAuthority.IsHost) return;
            if (_motor.PlayerSlot != NetAuthority.LocalSlot) return;

            var ability = AbilityFor(slot);
            if (ability == null) return;

            // ⚠️ THE ROLLBACK RUNS UNDER `SuppressRelay` LIKE EVERY OTHER NETWORKED KIT CALL IN
            // THIS FILE. `EndEarly` reaches `OnEnd`, and some `OnEnd` bodies play a cue; relaying
            // those outward would have a client asking the host to announce the end of an effect
            // the host never started.
            using (NetCue.SuppressRelay()) ability.RollBackPredictedCast(_context);

            _answer[(int)slot] = HeroKit.CastOutcome.Cooling;
            _answeredAt[(int)slot] = Time.time;
            PlayRefusal();
        }

        private void PlayRefusal()
        {
            // ⚠️ QUIET, AND ON THE PLAYER RATHER THAN AT A WORLD POINT. This fires on a mash, so
            // it is mixed to be noticed once and ignored the tenth time; `ui_error` already
            // carries the menu's refusal and reusing it means the player has heard it before.
            GameServices.Audio?.PlayAtVaried("ui_error", transform.position, 0.94f, 1.06f, 0.55f);
        }

        private HeroAbility AbilityFor(Slot slot)
        {
            if (Kit == null) return null;

            switch (slot)
            {
                case Slot.Skill1: return Kit.Skill1;
                case Slot.Skill2: return Kit.Skill2;
                default: return Kit.Ultimate;
            }
        }

        private Color AccentColour() => UI.UiTheme.ColorForHero(Kit != null ? Kit.HeroId : null);

        /// <summary>The same hue at the value a ground telegraph needs. See
        /// <see cref="UI.UiTheme.BrightForHero"/>.</summary>
        private Color AccentBright() => UI.UiTheme.BrightForHero(Kit != null ? Kit.HeroId : null);

        /// <summary>
        /// Where a power's footprint goes, from the ability's own numbers.
        ///
        /// ⚠️ THE BODY'S FORWARD IS ALREADY FLAT AND ALREADY THE VIEW YAW. `CameraRig.ApplyFpp`
        /// builds the camera from `BodyYawDeg()` plus its own pitch, and mouse look turns the
        /// BODY about world up, so `transform.forward` is horizontal and points exactly where
        /// the player is looking. Flattening it again here would be a no-op, and taking the
        /// camera's forward instead would tip the ring into the floor whenever the player
        /// glanced down.
        /// </summary>
        private Vector3 TelegraphCentre(HeroAbility ability)
        {
            return _context.Position + _context.Forward * ability.TelegraphRange;
        }

        /// <summary>
        /// Which weather this hero's ultimate brings, or null for a hero with none.
        ///
        /// ⚠️ THE MINIMUM IS 2.2 s AT THE CALL SITE, NOT HERE, because three of the six
        /// ultimates have a `Duration` of 0: they are instantaneous blasts and the field means
        /// "how long the power stays active", which for a nova is nothing. A sky that lasted zero
        /// seconds would be a one-frame colour glitch, so the shortest weather is the length of a
        /// held breath and the four that run longer keep their own.
        /// </summary>
        /// <summary>
        /// Which sustained theme plays under this hero's ultimate, or null for a hero with none.
        ///
        /// ⚠️ IT RETURNS NULL RATHER THAN A DEFAULT, exactly as <see cref="LookFor"/> does, and
        /// for the same reason: a hero with no row gets SILENCE, which is a missing feature and
        /// reads as one. Giving them somebody else's theme is `docs/TODO.md` § 8 item 3's fault
        /// (*"Sean's Supernova was spawning Dante's magma"*) in the other medium, and 🧑 has now
        /// reported that class of thing twice.
        /// </summary>
        private static string ThemeFor(string heroId)
        {
            switch (heroId)
            {
                case "phaister": return "sfx_coven_summon";
                case "zack": return "sfx_ult_theme_zack";
                case "cheska": return "sfx_ult_theme_cheska";
                case "sean": return "sfx_ult_theme_sean";
                case "dante": return "sfx_ult_theme_dante";
                case "nemu": return "sfx_ult_theme_nemu";
                default: return null;
            }
        }

        private static Visual.SkyEvent.Look? LookFor(string heroId)
        {
            switch (heroId)
            {
                case "phaister": return Visual.SkyEvent.Look.Eclipse;
                case "zack": return Visual.SkyEvent.Look.Stormfront;
                case "cheska": return Visual.SkyEvent.Look.Whiteout;
                case "sean": return Visual.SkyEvent.Look.Emberfall;
                case "dante": return Visual.SkyEvent.Look.Dustveil;
                case "nemu": return Visual.SkyEvent.Look.Seance;
                default: return null;
            }
        }

        private void PlayUltimatePresentation()
        {
            // ⚠️⚠️ THE COLUMN IS THE PART THE OTHER THREE PLAYERS SEE, AND IT IS DRAWN BEFORE
            // ANY OF THE CAMERA WORK BELOW BECAUSE IT IS THE ONLY PIECE THAT IS NOT LOCAL.
            //
            // Every ultimate in this game used to resolve in the frame it was cast, so from
            // three metres away an ultimate and a skill were the same event: a flash and then
            // consequences. There was no moment, and an ultimate that costs 90 to 150 points to
            // earn has to HAVE a moment or it is just a large skill.
            //
            // ⚠️ IT IS ALSO WHAT MAKES COUNTERPLAY POSSIBLE AT ALL, which is the reason Hero
            // Strike exists. `docs/VISION.md` § 1.1: *"Combos, timing, counterplay, reading
            // which ultimate is banked."* You cannot read something that has already happened.
            // A column of hero-coloured light standing off the caster is legible from across the
            // arena and through the crowd, and it says WHO and WHERE without a word on screen,
            // which `docs/VISION.md` § 3 requires of anything the HUD is not allowed to say.
            //
            // ⚠️⚠️ THIS NOW RUNS AT THE PRESS AND THE EFFECT LANDS 0.4 s LATER, WHICH IS THE
            // OPPOSITE OF WHAT THIS NOTE USED TO SAY. It read *"IT DOES NOT DELAY THE CAST"*,
            // and gave a good reason: a wind-up would be *"a fourth thing that can be
            // interrupted, and every one of the five ultimates would need its own answer to
            // being stunned halfway through; the buffering rules in `docs/Hero_Strike_UI.md`
            // § 7 have no story for a half-cast power."*
            //
            // ⚠️ THAT OBJECTION WAS ANSWERED BY REMOVING THE INTERRUPT, NOT BY ARGUING WITH IT.
            // `HeroAbility.Windup` is uninterruptible: once the press lands the ultimate fires,
            // so there is no half-cast state for any of the five to have an answer to and § 7's
            // buffering never meets one. The meter is still spent at the press, so there is no
            // refund path either. 🧑 asked for the wind-up directly after the refusal was put to
            // him: *"u can do windup lowk"*, *"im okay with windup or holdable shit"*.
            //
            // ⚠️ SO THE COLUMN IS A TELEGRAPH NOW RATHER THAN AN ANNOUNCEMENT, which is what
            // `Hero_Strike_Balance.md` § 4.3 wanted it to be: it stands up while the caster is
            // rooted, and the other three get those 0.4 s to run, reposition or spend something
            // defensive. It is the same object doing the same job 0.4 s earlier.
            //
            // ⚠️ THE IMPACT FEEDBACK BELOW STAYS ON THE PRESS AND IS NOT A SECOND PUNCH. It
            // reads as the caster bracing into the wind-up; the blast supplies its own shake
            // when it lands, scaled per style and per radius in `HeroHazards.CreateExplosion`.
            Visual.UltimateColumn.Raise(_context.Position, AccentColour());

            // ⚠️⚠️ THE WEATHER IS THE SECOND THING THAT IS NOT LOCAL, AND IT IS HERE RATHER THAN
            // IN SIX KITS ON PURPOSE. 🧑 2026-08-26, having asked for Phaister's eclipse: *"maybe
            // give some other characters other versions of this"*. Six kits each calling
            // `SkyEvent.Play` would be six places to forget, and the seventh hero would ship with
            // the one ultimate that does not change the sky — which is exactly how
            // `docs/VISION.md` § 3 argues `AbilityGlyph` onto the ability rather than into a
            // lookup table. One call, at the single point every ultimate in the game passes
            // through, and a new hero gets weather by existing.
            //
            // ⚠️ IT IS KEYED OFF THE HERO ID AND FALLS BACK TO NOTHING RATHER THAN TO A DEFAULT
            // LOOK. A hero with no row gets no weather, which is a missing feature; giving them
            // somebody else's storm would be `docs/TODO.md` § 8 item 3's fault again, *"Sean's
            // Supernova was spawning Dante's magma ... two heroes reading as one is the most
            // expensive form of repetitive, because it costs a character."*
            // ⚠️⚠️ THE LENGTH IS `SkyEvent.SecondsFor` NOW, NOT `Mathf.Max(2.2f, Duration)`.
            // Four of the six ultimates carry a `Duration` of 0 because they are instantaneous
            // blasts, so the old expression resolved to the 2.2 s floor for most of the roster
            // and the whole event including both ramps was 2.65 s. 🧑 2026-08-27: *"dude the
            // change in weather lasts liek 2 seconds,, u dont even notice it"*. That property
            // carries the new arithmetic and why the aftermath lives in the FALL.
            var look = LookFor(Kit != null ? Kit.HeroId : null);
            if (look.HasValue && Kit != null && Kit.Ultimate != null)
            {
                Visual.SkyEvent.Play(look.Value, Visual.SkyEvent.SecondsFor(Kit.Ultimate.Duration));
            }

            // ⚠️⚠️ AND THE HERO'S OWN THEME, WIRED HERE FOR THE REASON § 26 GIVES ABOUT THE SKY:
            // one call, at the single point every ultimate in the game passes through, so a new
            // hero cannot ship as the one whose ultimate is silent under the payload.
            // 🧑 2026-08-27: *"they dont have enough auditory effects too. give everyone their own
            // theme and dont generate it the same way bcz its gonna sound the same way"*.
            //
            // ⚠️⚠️ IT IS A BED UNDER THE PAYLOAD, NOT A REPLACEMENT FOR IT. `sfx_frost_nova`,
            // `sfx_thunder_impact` and `sfx_explosion_heavy` still fire from the kits at the
            // instant the blast lands and are untouched. What was missing is that an ultimate had
            // ONE hit and then silence for the six seconds it was still happening in, which is the
            // *"not enough auditory effects"* half of his sentence.
            //
            // ⚠️ NOT `NetCue`, FOR THE SAME REASON THE WEATHER IS NOT. The ability layer is not
            // replicated at all (`docs/TODO.md` § 25.1), so this is local like everything else in
            // this method; when that entry is closed the theme travels with the rest of the
            // presentation rather than needing its own path.
            string theme = ThemeFor(Kit != null ? Kit.HeroId : null);
            if (!string.IsNullOrEmpty(theme))
            {
                GameServices.Audio?.PlayAt(theme, _context.Position);
            }

            var camera = UnityEngine.Camera.main;
            if (camera == null) return;

            var rig = camera.GetComponent<CameraSystem.CameraRig>();
            bool isLocalHero = rig != null && rig.IsFollowing(_motor);
            float distance = Vector3.Distance(camera.transform.position, _context.Position);
            float falloff = isLocalHero ? 1.0f : Mathf.InverseLerp(22.0f, 5.0f, distance);
            if (falloff <= 0.01f) return;

            // -------------------------------------------------------------------
            // § THE WEIGHT OF THE PRESS
            //
            // ⚠️⚠️ 🧑 2026-08-27: *"i want all ults to feel like they hit harder ... their ults
            // dont feel like ults nor do they feel like hard shit that just hit, they all dont
            // have impact"*. Three numbers here were tuned when the ultimate's whole presentation
            // was a flash and a 0.9 s column, and they are the ones that say "something enormous
            // just happened" to a player who is LOOKING SOMEWHERE ELSE, which is most of the
            // arena most of the time.
            //
            // ⚠️ THE SHAKE AND THE CHROMATIC ABERRATION SCALE WITH `falloff`, SO STRENGTHENING
            // THEM DOES NOT MAKE A DISTANT ULTIMATE INTRUSIVE. `falloff` is already an inverse
            // lerp from 22 m to 5 m for anyone who is not the caster, so these are multipliers on
            // a curve that is zero across most of the map. What grew is the near field, which is
            // where "that hit hard" is decided.
            //
            // ⚠️⚠️ AND NONE OF THIS TOUCHES BRIGHTNESS, WHICH IS THE ONE AXIS THAT IS SPOKEN FOR.
            // `docs/TODO.md` § 8b measured Zack's Thunderstrike blowing **62.8 per cent** of a
            // frame to white and `AbilityShowcaseProbe` now fails a run over 12. A shake, a
            // chromatic split and a held frame all read as force while adding no luminance at
            // all, which is exactly why they are the levers here and a flash is not.
            // -------------------------------------------------------------------
            Vector3 away = camera.transform.position - _context.Position;
            if (away.sqrMagnitude < 0.01f) away = -_context.Forward;

            // 0.9 -> 1.45. `ImpactPunch` drives both a 0.20 m directional shove of the eye and a
            // `Shake` at 45 per cent of the strength, so this is the single number that carries
            // most of the physicality.
            rig?.ImpactPunch(away.normalized, 1.45f * falloff);

            // 0.32 s -> 0.85 s, at a slightly higher peak. The old pulse was over before the
            // wind-up finished, so the colour split never overlapped the thing it was reacting
            // to. This one is still decaying while the blast lands.
            camera.GetComponent<Visual.ColourGrade>()?.PulseChromatic(0.95f * falloff, 0.85f);

            // ⚠️ THE SECOND SHAKE IS THE TAIL, AND IT IS A SEPARATE CALL BECAUSE `Shake` TAKES
            // THE MAXIMUM OF WHAT IS ALREADY RUNNING. `ImpactPunch`'s own shake is a sharp 0.22 s
            // hit; this is a longer, weaker rumble underneath it, so the ground is still unsettled
            // a second later. One call cannot express both, and a single long hard shake would be
            // unplayable rather than weighty.
            rig?.Shake(0.30f * falloff, 1.10f);

            // 0.045 / 0.12 -> 0.075 / 0.20. The held frame is the clearest "this is not a skill"
            // signal the game has and it costs no light and no floor. It stays local-only: a hold
            // on somebody else's screen is input lag, not impact.
            if (isLocalHero) Hitstop.Trigger(0.075f, 0.20f);
        }

        /// <summary>
        /// The ring drawn while a hero key is held down.
        ///
        /// ⚠️⚠️ THE RADII USED TO BE INVENTED HERE AND EVERY ONE OF THEM WAS WRONG. This method
        /// drew 7.5 m for ANY ultimate, 5.0 m for ANY first skill and 3.5 m for ANY second, and
        /// pushed the centre forward only when the kit happened to be `CheskaHeroKit`. Measured
        /// against what the abilities actually spawn: Dante's 2.4 m stomp drew a 5.0 m ring,
        /// Cheska's 2.3 m frost drew 5.0 m at 3.5 m out when it lands at 2.8 m, and Nemu's 3.2 m
        /// void drew 7.5 m centred on Nemu when it lands 3.5 m in front of them. Nine of the
        /// twelve numbers on screen disagreed with the game.
        ///
        /// They come off `HeroAbility` now, where a new hero cannot ship without them.
        ///
        /// ⚠️ IT ONLY DRAWS FOR A POWER THAT IS ACTUALLY AVAILABLE. Showing the ring for a skill
        /// on cooldown would promise a cast the press is about to refuse.
        /// </summary>
        private void UpdateReticle(InputIntent intent)
        {
            if (_reticle == null || Kit == null) return;

            if (Aiming(intent, Verb.Ultimate, Kit.Ultimate, Kit.IsUltimateReady)) return;
            if (Aiming(intent, Verb.Skill1, Kit.Skill1, Kit.Skill1 != null && Kit.Skill1.IsReady)) return;
            if (Aiming(intent, Verb.Skill2, Kit.Skill2, Kit.Skill2 != null && Kit.Skill2.IsReady)) return;

            _reticle.Hide();
        }

        private bool Aiming(InputIntent intent, Verb verb, HeroAbility ability, bool available)
        {
            if (ability == null || !available || !ability.HasTelegraph) return false;
            if (!intent.Pressed(verb)) return false;
            if (_motor == null || !_motor.CanAct()) return false;

            // ⚠️⚠️ A HOLD-TO-AIM POWER'S RING MOVES WHILE THE KEY IS DOWN, AND THIS IS THE ONE
            // PLACE IN THE GAME WHERE THE PRE-CAST RING IS ACTUALLY WORTH DRAWING. This class's
            // own note records why it never was before: *"every one of these powers fires on the
            // press edge and resolves instantly, so the reticle drawn while the key is HELD
            // appears on the same frame the ability goes off"*. An ability that fires on RELEASE
            // inverts that: the ring is on screen for the whole decision, which is the entire
            // point of holding the key, and `GroundReticle.Flash` still answers "where did it
            // land" afterwards.
            float range = ability.HoldToAim
                ? ability.AimRangeFor(HeldSecondsFor(ability))
                : ability.TelegraphRange;

            // ⚠️ A DESTINATION YOU ARE GOING TO STAND IN GETS A STANDING MARK AS WELL AS A RING.
            // See `GroundReticle.SetBeacon`: 🧑 2026-08-27, on the blink, *"all it shows is a
            // frigging shadow, it's very easy to miss"*, and a decal on the road seen from head
            // height at three metres is a smudge whatever colour it is.
            // ⚠️⚠️ THE HELD RING TAKES THE HERO'S **BRIGHT** ACCENT, THE POST-CAST FLASH TAKES THE
            // BASE ONE, AND THE SPLIT IS DELIBERATE. 🧑 2026-08-27, on the blink: *"I dont want
            // Phaister's E HOLD for casting To just be a shadow, keep that outline and give it
            // her color so that it could be seen more"*. The flash answers "where did that land"
            // over a lit explosion; the held ring has to be read off bare asphalt under a
            // viaduct, by one player, while they decide. Those are different legibility problems
            // and the second one is the harder of the two. `UiTheme.BrightForHero` carries why a
            // mid-value accent reads as a shadow on ghosted geometry.
            _reticle.SetBeacon(ability.AimBeacon);
            _reticle.SetStyle(ability.TelegraphStyle);
            _reticle.Show(AimPoint(ability, range), ability.TelegraphRadius, AccentBright());
            return true;
        }

        /// <summary>How long this ability's own key has been held, whichever slot it sits in.</summary>
        private float HeldSecondsFor(HeroAbility ability)
        {
            if (Kit == null) return 0.0f;
            if (ability == Kit.Skill1) return HeldSeconds(Slot.Skill1);
            if (ability == Kit.Skill2) return HeldSeconds(Slot.Skill2);
            return HeldSeconds(Slot.Ultimate);
        }

        /// <summary>
        /// Where a power aimed at <paramref name="range"/> would land, inside the arena.
        ///
        /// ⚠️⚠️ THE BOX IS A SQUARE AND X AND Z CLAMP INDEPENDENTLY. `CLAUDE.md` § 4 states this
        /// as an architecture invariant and gives the number: a radial clamp and a square clamp
        /// disagree by **2.9 m on the diagonal**, which is exactly where somebody aims when they
        /// are cutting a corner. Clamping this ring radially would draw the destination in a
        /// different place from where `CharacterMotor.Teleport` actually puts the body, and a
        /// telegraph that lies is the fault `HeroAbility.TelegraphRadius` exists to stop.
        ///
        /// ⚠️ IT CLAMPS TO THE SAME RECTANGLE `Teleport` DOES, from the same constants, so the
        /// ring and the landing cannot drift apart when one of them is retuned.
        /// </summary>
        private Vector3 AimPoint(HeroAbility ability, float range)
        {
            Vector3 at = _context.Position + _context.Forward * range;

            if (!ability.HoldToAim) return at;

            at.x = Mathf.Clamp(at.x, -AIController.PlayableHalfX, AIController.PlayableHalfX);
            at.z = Mathf.Clamp(at.z, -AIController.PlayableHalfZ, AIController.PlayableHalfZ);
            return at;
        }

        /// <summary>Where a hold-to-aim power aimed by the live hold would land. For a kit.</summary>
        public Vector3 AimDestination(HeroAbility ability)
            => AimPoint(ability, ability.AimRangeFor(ability.HeldSecondsOnCast));

        /// <summary>
        /// ⚠️ THE THREE OBJECTIVE AWARDS ARE GATED ON PRACTICE TOO. They are already unlikely to
        /// fire outside a live round because scoring is paused, but "unlikely" is not the same
        /// as "cannot", and a knockdown landing during the buffer would hand out charge the
        /// scoreboard did not pay for.
        /// </summary>
        private void Award(float amount)
        {
            if (Kit == null || Kit.PracticeMode) return;

            Kit.AddUltimateCharge(amount);
        }

        public void OnLataKnocked()
        {
            Award(Balance.UltimateChargeLataKnock);
            Recharge(HeroAbility.Recharge.LataKnocked);
        }

        public void OnTagScored() => Award(Balance.UltimateChargeTag);

        public void OnThrowReleased() => Award(Balance.UltimateChargeLegalThrow);

        /// <summary>
        /// ⚠️⚠️ THE ACT THE WHOLE GAME IS BUILT AROUND, AND IT PAID NOTHING UNTIL 2026-08-25.
        /// `docs/VISION.md` § 0: *"The tension is the retrieval, not the throw. Throwing is safe
        /// and free; going back in for your tsinelas is the only moment you can be caught."*
        /// The ultimate economy nevertheless paid 8 for a throw and 0 for a retrieval, which is
        /// the two halves of the game rewarded in exactly the wrong order.
        ///
        /// It now pays `Balance.UltimateChargeOwnSlipperRetrieved` and refills Cheska's
        /// barricade. `docs/Hero_Strike_Balance.md` § 3.1.
        /// </summary>
        public void OnOwnSlipperRetrieved()
        {
            Award(Balance.UltimateChargeOwnSlipperRetrieved);
            Recharge(HeroAbility.Recharge.OwnSlipperRetrieved);
        }

        /// <summary>
        /// ⚠️ GATED ON PRACTICE FOR THE SAME REASON `Award` IS. A charge handed back during the
        /// buffer is a charge the round did not pay for, and unlike the meter a charge cannot be
        /// spent back down: `ResetForRound` refills to the cap either way, so a practice-period
        /// grant that pushed a player over would simply be lost, and one that did not would be
        /// free. Neither is a state worth having.
        /// </summary>
        private void Recharge(HeroAbility.Recharge what)
        {
            if (Kit == null || Kit.PracticeMode) return;

            Kit.OnRechargeEvent(what);
        }

        /// <summary>
        /// Wipe the kit back to how it starts a round: no charge, no cooldowns, nothing active.
        ///
        /// ⚠⚠ CALLED FROM `ResetWorld` IN BOTH RUNNERS, WHICH IS EVERY ROUND BOUNDARY THERE
        /// IS. `SliceRunner.ResetWorld` and `MatchBootstrap.ResetWorld` are reached from
        /// `RoundStarted` and from `IntermissionStarted`, so this covers the R press that starts
        /// round 1 (the ready gate raises `RoundShouldBegin`, `Begin` calls `StartMatch`, and
        /// that raises `RoundStarted`), the rotation into every later round, and a probe that
        /// calls `ResetWorld` directly. It is idempotent, so a double call costs nothing.
        /// </summary>
        public void ResetKit()
        {
            Kit?.ResetForRound(_context);
            ClearBuffers();
        }

        /// <summary>Empties the bank as well. For the start of a whole match, not a round.</summary>
        public void ResetKitForMatch()
        {
            Kit?.ResetForMatch(_context);
            ClearBuffers();
        }

        /// <summary>
        /// ⚠️ A ROUND BOUNDARY DROPS BUFFERED PRESSES. Without this, a key pressed in the last
        /// frames of a round comes out inside the intermission, or worse, at the start of the
        /// next round against a lata nobody has touched yet. Buffering is about surviving a
        /// stagger, not about surviving the whistle.
        /// </summary>
        private void ClearBuffers()
        {
            _skill1BufferedAt = float.NegativeInfinity;
            _skill2BufferedAt = float.NegativeInfinity;
            _ultimateBufferedAt = float.NegativeInfinity;
            _reticle?.Hide();
        }
    }
}
