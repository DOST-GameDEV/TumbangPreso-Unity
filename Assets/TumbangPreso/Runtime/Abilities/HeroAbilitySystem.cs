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

        public void BindHero(string heroId)
        {
            Kit = CreateKitFor(heroId);
        }

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
            Kit.PracticeMode = GameServices.Round == null || !GameServices.Round.RoundActive;

            Kit.Tick(_context, dt);

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
            if (intent.JustPressed(Verb.Skill1)) _skill1BufferedAt = Time.time;
            if (intent.JustPressed(Verb.Skill2)) _skill2BufferedAt = Time.time;
            if (intent.JustPressed(Verb.Ultimate)) _ultimateBufferedAt = Time.time;

            UpdateReticle(intent);

            ServiceBuffer(ref _skill1BufferedAt, Slot.Skill1);
            ServiceBuffer(ref _skill2BufferedAt, Slot.Skill2);
            ServiceBuffer(ref _ultimateBufferedAt, Slot.Ultimate);

            _reticle?.Tick(dt);
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
            switch (slot)
            {
                case Slot.Skill1: return Kit.CastSkill1(_context);
                case Slot.Skill2: return Kit.CastSkill2(_context);
                default: return Kit.CastUltimate(_context);
            }
        }

        private void PlayCastConfirm(Slot slot)
        {
            var animator = GetComponentInChildren<Visual.CharacterAnimator>();
            var ability = AbilityFor(slot);

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

            _reticle.Flash(TelegraphCentre(ability), ability.TelegraphRadius, AccentColour(), 0.35f);
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

            var camera = UnityEngine.Camera.main;
            if (camera == null) return;

            var rig = camera.GetComponent<CameraSystem.CameraRig>();
            bool isLocalHero = rig != null && rig.IsFollowing(_motor);
            float distance = Vector3.Distance(camera.transform.position, _context.Position);
            float falloff = isLocalHero ? 1.0f : Mathf.InverseLerp(22.0f, 5.0f, distance);
            if (falloff <= 0.01f) return;

            Vector3 away = camera.transform.position - _context.Position;
            if (away.sqrMagnitude < 0.01f) away = -_context.Forward;
            rig?.ImpactPunch(away.normalized, 0.9f * falloff);
            camera.GetComponent<Visual.ColourGrade>()?.PulseChromatic(0.75f * falloff, 0.32f);

            if (isLocalHero) Hitstop.Trigger(0.045f, 0.12f);
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

            _reticle.Show(TelegraphCentre(ability), ability.TelegraphRadius, AccentColour());
            return true;
        }

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
