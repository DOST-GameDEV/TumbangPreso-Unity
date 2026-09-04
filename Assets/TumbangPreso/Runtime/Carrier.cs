using System;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Holding, charging, throwing, and the lata reset channel.
    ///
    /// ⚠️⚠️ E DOES THREE JOBS AND PICKS BY WHAT IS IN FRONT OF YOU. Rather than inventing two
    /// more keybinds for a game whose entire brief is "simpler", the press resolves against
    /// context, and THIS COMPONENT GETS FIRST REFUSAL. Only a press that neither a pickup nor
    /// a channel consumed falls through to the shove or the lunge.
    ///
    /// | press            | condition                                   | result           |
    /// |------------------|---------------------------------------------|------------------|
    /// | E tap            | Attacker, loose slipper within PickupRadius  | pick up          |
    /// | E tap            | Attacker, nothing grabbable                  | shove, instantly |
    /// | E hold           | Defender, in the lata's ring, lata down      | reset the lata   |
    /// | E hold 0.5 s     | Defender, anything else                      | lunge            |
    ///
    /// ⚠️ WHILE THE CHANNEL IS RUNNING THE LUNGE CHARGE IS CANCELLED, so resetting the can can
    /// never fire a lunge out of it. That is the one interaction between the two that a player
    /// would otherwise hit constantly, because both are E held as the taya.
    /// </summary>
    /// <remarks>
    /// ⚠️⚠️ THE EXECUTION ORDER IS LOAD-BEARING, NOT TIDINESS, AND IT SERVES TWO SEPARATE
    /// GUARANTEES IN TWO DIFFERENT PHASES. Unity's order between two components is otherwise
    /// UNSPECIFIED, so both would work or not depending on the order Unity happened to build
    /// the seat in — the worst kind of bug, because it looks fixed on the machine it was
    /// written on. The three orders in play are `CharacterMotor` (-100), this (0) and
    /// <see cref="CombatVerbs"/> (+50):
    ///
    ///  * **Update** — this must run BEFORE the shove. `IsBusy` is how a connecting pickup tells
    ///    `CombatVerbs` that the frame's E press is already spent, and a flag set after the
    ///    reader has read it is no flag at all. In the .gd there is no ordering question:
    ///    `character_base.gd:913` calls `_carrier.input_step(delta)` and then `_step_shove(delta)`
    ///    in one function. This is what buys the port the same guarantee.
    ///
    ///  * **FixedUpdate** — this must run AFTER the motor has moved. See `FixedUpdate` below;
    ///    a carry that ran first would leave the tsinelas one step of walking behind the hand.
    ///
    /// ⚠️ IT IS NOT NEGATIVE ANY MORE, AND THAT MATTERS. An earlier attempt put this at -50 for
    /// the Update guarantee alone, which bought it at the cost of the FixedUpdate one: the carry
    /// then ran BEFORE the move every step. Measured, that was still an 0.085 m drift. The
    /// guarantee wanted is an ORDER BETWEEN THREE COMPONENTS, so it is spelled out on all three
    /// rather than pushed onto one.
    /// </remarks>
    [DefaultExecutionOrder(0)]
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class Carrier : MonoBehaviour
    {
        [SerializeField] private Transform _hand;

        /// <summary>
        /// Where a carried slipper rides.
        ///
        /// ⚠️⚠️ THE SERIALISED FIELD ABOVE WAS NEVER ASSIGNED BY ANYTHING, IN ANY BUILD.
        /// `MatchInstaller` installs this component with `AddComponent`, which cannot carry an
        /// inspector reference (rule 3), so `_hand` was null on every unit and the block that
        /// keeps a held slipper in the hand never ran once. A picked-up tsinelas simply stayed
        /// where the pickup left it and the carrier walked away from it, which is the
        /// third-person half of *"the slippers just float when you hold it, its completely
        /// unattached to person"*. The viewmodel fix addressed the local player's own view and
        /// left every OTHER player seeing exactly the reported bug.
        ///
        /// ⚠️ RE-ASKED WHILE IT IS NULL, NOT CACHED ONCE. `CharacterVisual` rebuilds the anchor
        /// on every model swap, and a Prop round swaps models mid-match.
        /// </summary>
        private Transform Hand()
        {
            if (_hand != null) return _hand;

            var visual = GetComponent<Visual.CharacterVisual>();
            return visual != null ? visual.HandAnchor : null;
        }

        /// <summary>
        /// The hand if there is one, and the BODY if there is not.
        ///
        /// ⚠️⚠️ A HELD SLIPPER MUST NEVER BE LEFT BEHIND, AND `return`-ING ON A NULL ANCHOR DID
        /// EXACTLY THAT. 🧑 2026-08-16: *"make sure the slippers in unity stay on the arm no
        /// matter what"*. The anchor is built from the skin's own weighted vertices, so it is
        /// absent for as long as a model is missing, mid-swap, or authored with a bone this
        /// project's resolver does not recognise — and the old early return meant the tsinelas
        /// simply stopped where it was and the carrier walked away from it. That is the reported
        /// *"the slippers just float when you hold it, its completely unattached to person"*,
        /// reachable again through any rig whose arm does not resolve.
        ///
        /// A body-relative pose is worse-looking than the measured one and is not the fault
        /// anybody reports: an object in roughly the right place moving with its owner reads as
        /// held. An object standing still in the street does not.
        ///
        /// ⚠️ AND IT IS A FALLBACK, NOT A DEFAULT. `Hand()` is asked first on every frame, so a
        /// model that finishes loading, or a swap that completes, takes the real anchor back on
        /// the very next frame with no state to reset.
        /// </summary>
        private Transform CarryAnchor()
        {
            var hand = Hand();
            if (hand != null) return hand;

            var visual = GetComponent<Visual.CharacterVisual>();
            if (visual != null && visual.ModelRoot != null) return visual.ModelRoot;

            return transform;
        }

        /// <summary>
        /// How far in front of and above the body the fallback pose sits, in the body's own
        /// frame. Chest height and a hand's reach forward: it is where a carried thing looks
        /// like it is being carried, and it is only ever seen when the measured anchor is
        /// missing.
        /// </summary>
        private static readonly Vector3 FallbackCarryOffset = new Vector3(0.28f, 1.05f, 0.32f);

        private CharacterMotor _motor;

        private float _charge;
        private bool _charging;
        private float _throwLockLeft;
        private float _channel;

        /// <summary>The channel time the reset gesture was last fired at, so the read repeats on
        /// its own length instead of once per press. See <see cref="StepDefender"/>.
        /// ⚠️ IT NEEDS NO RESET OF ITS OWN: the opening frame is detected from `_channel` being
        /// zero and rewrites this on the way past, so a stale value cannot survive a press.
        /// </summary>
        private float _lastResetGesture;

        public Slipper Held { get; private set; }
        public float ChargeRatio => ThrowRules.ChargeRatio(_charge);
        public float ChannelRatio { get; private set; }

        private float _pektusSpin;
        public float CurrentPektusSpin => _charging ? _pektusSpin : 0.0f;

        /// <summary>True while this unit is winding a throw up. Read by the aim arc and by the
        /// YOU card's charge meter.</summary>
        public bool IsCharging => _charging;

        /// <summary>
        /// ⚠️⚠️ THE WIND-UP EVERY OTHER PLAYER CAN SEE, and it is a SEPARATE value from
        /// <see cref="ChargeRatio"/> on purpose. `carrier.gd`'s header states it: the charge
        /// clock only ticks on the peer that controls the unit, so a third-person wind-up pose
        /// driven from it is invisible to the person being aimed at — which is the whole
        /// counterplay the 2.5 s charge exists to create.
        ///
        /// -1 when nobody is winding up.
        /// </summary>
        public float ObservedChargePower =>
            _observedCharge < 0.0f ? -1.0f : Mathf.Clamp01(_observedCharge / Balance.ChargeFullTime);

        private float _observedCharge = -1.0f;

        /// <summary>
        /// Seconds until this unit may throw again after a pickup.
        ///
        /// ⚠️ EXPOSED SO THE AI CAN SEE WHAT A PLAYER SEES. A bot that plants and charges
        /// during its own throw lock stands still doing nothing visible and reads as stuck.
        /// </summary>
        public float ThrowLockLeft => _throwLockLeft;

        public bool ThrowLocked => _throwLockLeft > 0.0f;

        /// <summary>
        /// True while this unit is mid-commitment, so <see cref="CombatVerbs"/> knows an E press
        /// was already spent on something else.
        ///
        /// ⚠️⚠️ `_grabConsumedThisFrame` IS THE THIRD CASE AND ITS ABSENCE WAS THE REPORTED
        /// *"SHOVE GETS USED EVEN WHEN I HAVE SLIPPER"*. `carrier.gd::is_busy()` is
        /// `_is_charging or _channelling or _grab_consumed_this_frame` and only the first two
        /// were ported. E is contextual: a tap with a tsinelas at your feet is a pickup, and a
        /// tap with nothing grabbable is a shove. In the .gd those are two branches of ONE
        /// function, `input_step()` then `_step_shove()`, so the pickup can tell the shove the
        /// press is spent. Here they are two components with two `Update`s, and nothing carried
        /// that word between them: every successful pickup ALSO fired a shove on the same press,
        /// spending 25 stamina and putting SHOVE CD on screen at the exact moment the player had
        /// just picked a slipper up. That is why the cooldown appeared while carrying, and why
        /// the shove then seemed never to be available when it was actually wanted.
        ///
        /// See the execution-order attribute on this class for what makes the ordering real
        /// rather than incidental.
        /// </summary>
        public bool IsBusy => _channel > 0.0f || _charging || _grabConsumedThisFrame;

        /// <summary>
        /// ⚠️ A ONE-FRAME FLAG, NOT A FOURTH PERSISTENT STATE. A grab has nothing to stay busy
        /// WITH once it has resolved, so it only says "already spent" for the remainder of the
        /// frame it fired on. Cleared at the top of every step, exactly as the .gd clears it at
        /// the top of `input_step()`, so a later frame's press is never shadowed by an old one.
        /// </summary>
        private bool _grabConsumedThisFrame;

        private void Awake() => _motor = GetComponent<CharacterMotor>();

        /// <summary>
        /// HOST-SIDE pickup, shared by the solo path and the networked request.
        ///
        /// ⚠️ THE HOST RE-CHECKS ELIGIBILITY EVEN THOUGH THE CLIENT ALREADY DID. A client one
        /// frame behind asking for a slipper somebody else just took is ordinary, not an
        /// error; refuse it quietly rather than trusting the request.
        /// </summary>
        public void HostPickUp(Slipper what)
        {
            if (!NetAuthority.ShouldResolve()) return;
            if (what == null || !what.CanBeGrabbedBy(_motor)) return;

            what.HostGrab(_motor);
            NotifyHolding(what);
            Net.MatchRpc.Instance?.BroadcastSlipperState(what);
        }

        /// <summary>
        /// HOST-SIDE throw from an explicit origin and aim point, so a networked throw leaves
        /// along the line the CLIENT was aiming rather than the one the host sees a frame later.
        /// </summary>
        public void HostThrowAt(Vector3 origin, Vector3 aimPoint, float charge, float spin = 0.0f)
        {
            if (!NetAuthority.ShouldResolve() || Held == null) return;

            // ⚠️⚠️ `NetCue`, NOT `GameServices.Audio`, AND THIS LINE IS WHY THAT CLASS EXISTS.
            // It sits inside `HostThrowAt`, which opens with `if (!NetAuthority.ShouldResolve())
            // return;`, so on a client it is never reached: **no peer but the host has ever
            // heard a throw leave a hand in a networked match**, and the throw is the most
            // frequent verb in the game. Found by `tools/audit_audio_reach.py`, which walks every
            // audio call in the runtime tree and reports the ones behind an open authority gate.
            //
            // ⚠️ THE GATE IS CORRECT AND STAYS. Only the host may DECIDE a throw happened. What
            // was wrong is that deciding and announcing were one line; `NetCue` separates them,
            // which is the shape `NetAuthority`'s class note already describes for every verb.
            NetCue.PlayVaried("throw_release", origin, 0.94f, 1.07f, 0.95f);
            GetComponentInChildren<Visual.CharacterAnimator>()?.PlayAction("throw");
            GetComponentInChildren<Visual.CharacterSquashStretch>()?.DashStretch(transform.forward, 0.14f);
            // ⚠️⚠️ RELAYED, BECAUSE THE ENCLOSING VERB IS HOST-RESOLVED AND WHAT IT DRAWS IS
            // FOR EVERYBODY. 🧑 2026-08-29: *"make sure that all host sided shit is seen by
            // everyone and not js host"*. See `Visual.MatchFlair` and
            // `tools/audit_presentation_reach.py`, which is what found this one.
            Visual.MatchFlair.Announce(Visual.MatchFlair.Kind.Throw,
                                       _motor.PlayerSlot, -1, origin, spin);

            // ⚠️ COUNTED HERE AND NOT AT THE INPUT, BECAUSE A PRESS IS NOT A THROW. This body
            // is behind `NetAuthority.ShouldResolve()` and a live `Held`, so it is reached once
            // per tsinelas that actually left a hand. `MatchStatsCollector` gates itself again
            // for the same reason every verb does; the two guards are cheap and independent.
            GameServices.Stats?.NoteThrow(_motor.PlayerSlot);

            var ability = _motor.AbilitySystem;
            ability?.OnThrowReleased();

            Vector3 velocity = Held.LaunchVelocityTo(origin, aimPoint, Mathf.Clamp01(charge));
            SlipperAffinity affinity = SlipperAffinity.Normal;

            if (ability != null && ability.Kit is ZackHeroKit zack &&
                (zack.IsOverchargeThrowActive || zack.IsThunderstrikeActive))
            {
                // ⚠️ THE ALTERNATE'S FRACTION COMES OFF THE TABLE, NOT OUT OF THIS LINE. `2.4f`
                // was written here as `1.6 x 1.5` at a moment when Snap Discharge happened to be
                // +50 per cent; the row is the only place that number may live, or the label the
                // player reads and the shoe they throw are two different numbers.
                velocity *= 1.6f * ability.VariantGain("zack.2.discharge");
                affinity = SlipperAffinity.ElectricZap;
                zack.IsOverchargeThrowActive = false;
            }
            else if (ability != null && ability.Kit is SeanHeroKit sean && sean.IsIgnitionCannonActive)
            {
                velocity *= 1.3f * ability.VariantGain("sean.2.flare");
                affinity = SlipperAffinity.FireExplosive;
                sean.IsIgnitionCannonActive = false;
            }
            else if (ability != null && ability.Kit is PhaisterHeroKit phaister &&
                     (phaister.IsWitchfireInfused || phaister.IsEclipseActive))
            {
                velocity *= 1.35f;
                phaister.IsWitchfireInfused = false;
            }

            var thrown = Held;
            thrown.HostThrow(_motor, origin, velocity, affinity, spin);

            Held = null;
            _motor.HoldingSlipper = false;
            _charge = 0.0f;
            _pektusSpin = 0.0f;

            // ⚠️ THE ARM SWINGS ON EVERY SCREEN. `PlayAction` above runs on the host only, and the
            // owning client predicts its own; without this the other two peers saw a tsinelas
            // leave a body that never moved. `BroadcastAction` skips the host itself, and the
            // thrower is excluded by `PredictThrowPresentation` having already played it.
            Net.MatchRpc.Instance?.BroadcastActionExceptOwner(_motor.PlayerSlot, "throw");
            Net.MatchRpc.Instance?.BroadcastSlipperState(thrown);
        }

        /// <summary>
        /// ⚠️ THE LOCK IS SET AFTER A PICKUP THE PLAYER HAS ALREADY WALKED OVER AND MADE, so
        /// it covers the beat between HAVING the slipper and being able to throw it. It is
        /// emphatically not a "return" mechanic: nothing in this game hands a slipper back,
        /// and a label implying otherwise promised a mechanic that does not exist.
        /// </summary>
        public void NotifyHolding(Slipper what)
        {
            // ⚠️ IDEMPOTENT, BECAUSE THERE ARE NOW TWO CALLERS ON ONE PICKUP. `Slipper.HostGrab`
            // tells this component itself (see its own note on owning the relationship) and
            // `HostPickUp` calls it again straight afterwards. Without this guard the pickup
            // sound plays twice on the same frame and the grab clip restarts on its second frame.
            if (Held == what && what != null) return;

            Held = what;
            _motor.HoldingSlipper = what != null;

            if (what == null) return;

            GameServices.Audio?.PlayAtVaried("pickup", transform.position, 0.96f, 1.08f, 0.9f);

            // Reaching down for a loose tsinelas — the literal clip for the job, and it now
            // reaches the first-person arm through the same call. See CharacterAnimator.PlayAction.
            GetComponentInChildren<Visual.CharacterAnimator>()?.PlayAction("grab");
            GetComponentInChildren<Visual.CharacterSquashStretch>()?.Squash(0.13f);
            // ⚠️⚠️ THE CALLOUT NAMES WHICH RETRIEVAL IT WAS, AND THE AWARD DOES NOT CHANGE.
            // `docs/TODO.md` § 146: a committed slide is harder than a walk-up and deserves its
            // own word; paying it more would be `docs/VISION.md` § 1.1's *"do not give Classic
            // powers"* arriving through the cosmetic bar. **This is the one funnel every pickup
            // reaches** (`HostPickUp`, the proximity grab and `Slipper.HostGrab` all land here,
            // and the guard above makes it idempotent), which is why the slide has no award of
            // its own: `CombatVerbs.SweepSlideRetrieval` had one for a day and
            // `tools/audit_presentation_reach.py` reported it as the only host-only presentation
            // call site in the game.
            //
            // ⚠️ `IsCommitted` IS LOCAL STATE AND THAT IS CORRECT HERE. `Hud.ApplyStyle` draws
            // only for the LOCAL seat, and both peers that can reach this for a given body, the
            // owner, which predicted the slide, and the host, which resolved it, have the
            // commitment running. Every other peer computes a string it will not draw.
            UI.Hud.ReportStyle(_motor.PlayerSlot, 14.0f,
                               _motor.IsCommitted ? "SIPA RESCUE!" : "SNATCH!");

            // ⚠️⚠️ THE RETRIEVAL PAYS THE HERO ECONOMY, AND IT IS WIRED HERE BECAUSE THIS IS THE
            // ONE FUNNEL EVERY PICKUP GOES THROUGH. `HostPickUp`, the proximity grab and
            // `Slipper.HostGrab` all arrive here, and the guard above makes it idempotent, so a
            // reward placed here is paid exactly once per pickup and cannot be paid twice by the
            // double call the guard exists for.
            //
            // `docs/VISION.md` § 0: *"The tension is the retrieval, not the throw. Throwing is
            // safe and free; going back in for your tsinelas is the only moment you can be
            // caught."* The ultimate economy paid 8 for the throw and nothing at all for this
            // until 2026-08-25, which is the two halves of the game rewarded in exactly the
            // wrong order. `docs/Hero_Strike_Balance.md` § 3.1.
            //
            // ⚠️ ONLY YOUR OWN TSINELAS COUNTS. Picking up somebody else's is a denial play and
            // a fine one, but it is not the run this game is built around and it carries none of
            // the same risk. `OwnerSlot` is authoritative; a slipper nobody owns pays nothing.
            // ⚠️ MEASURED AT THE SAME PLACE THE ECONOMY IS PAID, AND FOR THE SAME REASON.
            // This method is the one funnel every pickup arrives through, and the guard above
            // makes it idempotent, so a retrieval is counted exactly once and cannot be counted
            // twice by the double call that guard exists for. The distance to the taya is taken
            // NOW: it is what decides whether this was a run under pressure or a walk, and one
            // frame later the defender has moved.
            GameServices.Stats?.NoteRetrieval(
                _motor.PlayerSlot, what.OwnerSlot == _motor.PlayerSlot, DistanceToTaya());

            if (what.OwnerSlot == _motor.PlayerSlot)
                _motor.AbilitySystem?.OnOwnSlipperRetrieved();

            _throwLockLeft = what.ThrowLock;
        }

        /// <summary>
        /// Flat distance from this body to the current taya, or -1 when there is not one to
        /// measure against.
        ///
        /// ⚠️ FLAT, LIKE EVERY OTHER CONTACT MEASUREMENT IN THE GAME. Height is what a jump
        /// and a kerb change, and `CLAUDE.md` § 4's distance rule is about the floor plan.
        ///
        /// ⚠️ -1 IS NOT ZERO. Zero would read as the taya standing on top of you, and would
        /// score every pickup in a Practice round that has no defender in it as made under
        /// maximum pressure.
        /// </summary>
        private float DistanceToTaya()
        {
            var round = GameServices.Round;
            var match = GameServices.Match;
            if (round == null || match == null) return -1.0f;

            var taya = round.PlayerAt(match.DefenderSlot);
            if (taya == null || taya == _motor) return -1.0f;

            Vector3 a = transform.position;
            Vector3 b = taya.transform.position;
            a.y = 0.0f;
            b.y = 0.0f;
            return Vector3.Distance(a, b);
        }

        /// <summary>
        /// The round-start hand-over. Same relationship as <see cref="NotifyHolding"/>, none of
        /// the pickup's feedback.
        ///
        /// ⚠️ NO THROW LOCK EITHER. The lock covers the beat between walking onto a loose
        /// tsinelas and being able to throw it; a slipper you were HANDED at the whistle has no
        /// such beat, and charging one on the first frame of a round is the opening the game is
        /// tuned around.
        /// </summary>
        public void NotifyEquipped(Slipper what)
        {
            if (Held == what && what != null) return;

            Held = what;
            _motor.HoldingSlipper = what != null;
            _throwLockLeft = 0.0f;

            // Put it in the hand THIS frame rather than on the next LateUpdate, so the first
            // thing a player sees of the round is not their tsinelas flying in from the mark.
            RideAnchor();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // ⚠️ CLEARED HERE, ONCE, BEFORE ANY BRANCH BELOW CAN SET IT. See _grabConsumedThisFrame.
            _grabConsumedThisFrame = false;

            if (_throwLockLeft > 0.0f) _throwLockLeft = Mathf.Max(0.0f, _throwLockLeft - dt);

            // The observed wind-up runs on every peer, including the ones that are not driving
            // this unit. See ObservedChargePower.
            if (_observedCharge >= 0.0f)
                _observedCharge = Mathf.Min(_observedCharge + dt, Balance.ChargeFullTime);

            if (!_motor.CanAct())
            {
                CancelAll();
                return;
            }

            if (_motor.IsDefender) StepDefender(dt);
            else StepAttacker(dt);
        }

        /// <summary>
        /// Rides the hand. Godot parents the tsinelas to a `BoneAttachment3D`, so it inherits the
        /// arm bone's transform for free and cannot come off it; Unity has no bone-parenting on a
        /// SkinnedMeshRenderer, so the carry is a per-frame follow instead. Same result, and the
        /// TPP object and the viewmodel one stay in sync because both are driven from the same
        /// measured anchor rather than from two hand-tuned offsets.
        ///
        /// ⚠️⚠️ IT HAS TO BE LateUpdate, AND IN Update IT DETACHED WHENEVER A CLIP PLAYED. Unity
        /// evaluates the Animator BETWEEN Update and LateUpdate, so a bone read during Update is
        /// the pose from the PREVIOUS frame. Standing still that is invisible, because last
        /// frame's hand and this frame's hand are the same place. The moment an arm actually
        /// moves — the pickup, the wind-up, the throw, a run cycle — the slipper trails the hand
        /// by exactly one frame of animation, which is the *"slippers deattach when animations
        /// play"* report and is worst during precisely the clips a player is looking at.
        ///
        /// ⚠️ AND IT IS NOT GATED ON CanAct. A stunned carrier still holds their tsinelas, and
        /// returning early above used to leave it frozen in the air where the stun started.
        /// </summary>
        private void LateUpdate() => RideAnchor();

        /// <summary>
        /// ⚠️⚠️ THE CARRY ALSO RUNS AFTER THE PHYSICS STEP, AND WITHOUT THIS IT LAGGED THE BODY
        /// BY A WHOLE STEP OF WALKING. 🧑 on this build: *"slippers are floating from hand of
        /// everyone"*, and `CarryTests.AHeldSlipperStaysOnTheArmThroughMovementAndAMissingAnchor`
        /// was already failing on the shipped commit — measured drift 0.98 m against a 0.05 m
        /// bound, so this is the reported bug with a number on it rather than a guess.
        ///
        /// LateUpdate alone is necessary but NOT sufficient, and the two halves fix different
        /// frames. LateUpdate is what makes the slipper follow the ARM: the Animator is
        /// evaluated between Update and LateUpdate, so a bone read any earlier is the previous
        /// frame's pose (see the note on RideAnchor). This is what makes it follow the BODY:
        /// `CharacterMotor` moves the capsule in FixedUpdate, and every FixedUpdate that lands
        /// after the last LateUpdate moves the hand while the slipper stays where it was put.
        /// At attacker walk speed one 0.02 s step is ~0.09 m, which is exactly the residue left
        /// once the LateUpdate half was working. It shows worst during the sprint out of the box,
        /// which is when the tsinelas is being looked at hardest.
        ///
        /// ⚠️ THE ORDER ATTRIBUTES ON THIS CLASS AND ON `CharacterMotor` ARE WHAT MAKE IT
        /// "AFTER". Same-frame, same-phase order between two components is otherwise unspecified
        /// in Unity, and a carry that ran before the move would reintroduce the identical lag
        /// while looking correct in the source.
        /// </summary>
        private void FixedUpdate() => RideAnchor();

        private void RideAnchor()
        {
            if (Held == null) return;

            var hand = Hand();

            if (hand != null)
            {
                // ⚠️ THE CARRY ROTATION IS PART OF THE POSE, not decoration. Without it the
                // slipper lies sideways across the palm.
                //
                // ⚠️ AND THE SOLE HANGS BELOW THE ORIGIN BY AN AMOUNT THAT DIFFERS PER SKIN, so
                // the last lift comes off the drawn bounds rather than from a constant. A clog
                // and a flip-flop cannot share one number, which `slipper.gd::_attach_to_hand()`
                // records.
                //
                // ⚠️⚠️ NO `* hand.lossyScale.y` HERE, AND THAT EXTRA FACTOR WAS THE FLOAT. 🧑
                // 2026-08-18: *"tsinelas floats right above the characters hands"*. `RestHeight`
                // is `Renderer.bounds.extents.y`, a WORLD-space length read off the slipper's own
                // (unscaled) transform — the same value `GroundY(p) + RestHeight` uses directly,
                // with no scale factor, to rest a loose slipper on the ground. `hand.up` is
                // already a world-space unit vector. Multiplying the lift by `hand.lossyScale.y`
                // (the character's `PersonScale`, 2.38, inherited by every descendant of the model
                // root) scaled an already-correct world length a second time. Measured before this
                // fix: RestHeight 0.0714 m, hand.lossyScale.y 2.3800, held slipper sitting 0.1639 m
                // from the anchor against the 0.0714 m the un-scaled lift should have put it at —
                // the shoe floating roughly 9 cm above the hand it was meant to rest on.
                //
                // ⚠️ GODOT NEVER HAD THIS, AND THE REASON IS INSTRUCTIVE. `slipper.gd`
                // re-parents the shoe onto the bone attachment, so it inherits the same 2.38x
                // every other rig child does — and its own `_attach_to_hand()` divides that scale
                // back OUT before applying any offset. `RideAnchor` does not reparent, it copies a
                // position every frame instead (see this function's own note above), so there was
                // never an inherited scale here to undo, and multiplying one in was pure invention.
                // ⚠️⚠️ THE ROTATION IS WRITTEN FIRST AND THE DRAWN CENTRE IS SUBTRACTED, AND
                // WITHOUT THAT SECOND TERM THE SHOE HANGS OFF THE HAND FOR EVERY UNIT. 🧑
                // 2026-08-29: *"slipper floats for everyone including bots, it isnt on their arms
                // ... it floats for all poses"*. `docs/TODO.md` § 80.5.
                //
                // The line above placed the slipper's ORIGIN at the anchor, and § 70.2 fixes
                // every slipper mesh as centred on XY and seated on Z = 0, so that origin is on
                // the SOLE at one END of the shoe. What the player sees is the mesh, which was
                // therefore always offset from the hand by however far its author put the origin
                // from its middle — a different amount for each of the nine skins.
                //
                // ⚠️ THE ROTATION HAS TO BE SET BEFORE THE OFFSET IS READ. `DrawnCentreOffset`
                // comes off `Renderer.bounds`, which is world space, so it is only the correct
                // vector once the shoe is already turned the way it will be drawn. Reading it
                // first and rotating after corrects along the wrong axis, which is exactly the
                // trap `ViewmodelArms.NormaliseHeldSize` records for the first-person copy.
                //
                // ⚠️ `CarryTests` CANNOT SEE THIS AND STILL CANNOT. It asserts on the ORIGIN's
                // distance from the anchor, which this still satisfies; see `DrawnCentreOffset`.
                // The float was never a violation of anything that was being measured.
                Held.transform.rotation = hand.rotation * Slipper.CarryRotation;
                Held.transform.position =
                    hand.position + hand.up * Held.RestHeight - Held.DrawnCentreOffset;

                return;
            }

            // No anchor this frame. See CarryAnchor: it rides the body rather than being
            // abandoned in the street.
            var fallback = CarryAnchor();

            Held.transform.SetPositionAndRotation(
                fallback.TransformPoint(FallbackCarryOffset),
                fallback.rotation * Slipper.CarryRotation);
        }

        // -------------------------------------------------------------------

        private void StepAttacker(float dt)
        {
            var intent = _motor.Intent;

            // First refusal: a tap with something grabbable at your feet is a pickup, and
            // nothing else gets to see that press.
            //
            // ⚠️ THE FLAG IS SET AFTER THE PICKUP IS ALREADY COMMITTED, NOT AS A GATE ABOVE.
            // `carrier.gd::_step_grab` is emphatic about the ordering for the same reason: a
            // grab that did NOT connect must still fall through to the shove, so only a
            // CONNECTING grab may mark the press spent.
            if (intent.JustPressed(Verb.Grab) && Held == null && TryPickup())
            {
                _grabConsumedThisFrame = true;
                return;
            }

            if (Held == null)
            {
                CancelCharge();
                return;
            }

            bool canThrow = GameServices.Round != null
                            && GameServices.Round.CanThrow(_motor)
                            && _throwLockLeft <= 0.0f;
            bool canMaintainCharge = GameServices.Round != null
                                     && GameServices.Round.CanMaintainThrowCharge(_motor);

            // ⚠️⚠️ `Pressed`, NOT `JustPressed`, TO START A CHARGE, AND THAT IS MEASURED. The
            // .gd records it: `input_probe` drove a synthetic press that lasted one frame and
            // the just-pressed form charged to 0.000. A bot writes its intent for a window
            // rather than for an edge, so the edge form silently gave every AI seat a
            // zero-power throw.
            if (!_charging && intent.Pressed(Verb.SpecialAbility))
            {
                if (!canThrow) return;

                _charging = true;
                _charge = 0.0f;
                BroadcastCharge(true);

                // The wind-up is audible as well as visible. It is the taya's cue that a throw
                // is coming, and it is the only one that reaches a player who is looking away.
                GameServices.Audio?.PlayAt("throw_charge", transform.position);
                return;
            }

            if (!_charging) return;

            if (intent.Pressed(Verb.SpecialAbility))
            {
                _charge = Mathf.Min(_charge + dt, Balance.ChargeFullTime);
                _pektusSpin = Mathf.Clamp(intent.SpinInput, -Balance.MaxPektusSpin, Balance.MaxPektusSpin);

                // Walking into the box, losing the slipper or ending the round cancels the
                // commitment. The lata going down does not. That state is often caused by a
                // teammate during somebody else's wind-up, and snapping every charged arm to
                // idle on that frame made the shared knockdown feel like an animation error.
                // Release legality is still checked below, so holding the pose cannot bank an
                // illegal shot inside the box or launch through restoration protection.
                if (!canMaintainCharge) CancelCharge();
                return;
            }

            // Released.
            float power = ChargeRatio;
            float spin = _pektusSpin;
            CancelCharge();

            if (canThrow) Release(power, spin);
        }

        /// <summary>
        /// Ends a wind-up from one place — released, cancelled, the slipper knocked out of the
        /// hands, the round reset. An arc left on screen after the throw has gone is worse than
        /// one that never appeared, so the clear belongs with the cancel and not at each site.
        /// </summary>
        private void CancelCharge()
        {
            if (!_charging) return;

            _charging = false;
            _charge = 0.0f;
            _pektusSpin = 0.0f;
            BroadcastCharge(false);
        }

        /// <summary>
        /// ⚠️ IT TICKS ON EVERY PEER, which is what makes the wind-up counterplay work. In the
        /// local game this is simply the same clock; the shape is kept so the networked half
        /// has one place to become an RPC.
        /// </summary>
        private void BroadcastCharge(bool active)
        {
            ApplyObservedCharge(active);
            if (NetAuthority.IsNetworked)
                Net.MatchRpc.Instance?.SetThrowCharge(_motor.PlayerSlot, active);
        }

        /// <summary>Applies another peer's visible throw wind-up without touching local input.</summary>
        public void ApplyObservedCharge(bool active) => _observedCharge = active ? 0.0f : -1.0f;

        private bool TryPickup()
        {
            var round = GameServices.Round;
            if (round == null) return false;

            Slipper best = null;
            float bestDist = float.MaxValue;

            foreach (var s in FindObjectsByType<Slipper>(FindObjectsSortMode.None))
            {
                if (!s.CanBeGrabbedBy(_motor)) continue;

                float d = Vector3.Distance(transform.position, s.transform.position);
                if (d >= bestDist) continue;

                bestDist = d;
                best = s;
            }

            if (best == null) return false;

            if (NetAuthority.ShouldRequest())
            {
                Net.MatchRpc.Instance?.RequestGrabServerRpc(_motor.PlayerSlot, best.OwnerSlot);
                return true;
            }

            if (!best.HostGrab(_motor)) return false;

            NotifyHolding(best);
            return true;
        }

        /// <summary>
        /// Where the aim ray points. ⚠️ AN AI AIMS AT A POINT IT WAS TOLD, NOT DOWN A CAMERA.
        /// A non-mouse-aimed unit's camera follows its body and its body yaw is the direction it
        /// last WALKED, so the whole cast resolves to "wherever I was heading". Measured over 20
        /// AI rounds in the original, throws that reached the can: zero.
        /// </summary>
        public Vector3 AimPoint()
        {
            if (_motor.Intent.HasAimPoint) return _motor.Intent.AimPoint;

            var rig = UnityEngine.Camera.main != null
                ? UnityEngine.Camera.main.GetComponent<CameraSystem.CameraRig>()
                : null;

            if (rig != null && rig.IsFollowing(_motor)) return rig.AimPoint();

            return transform.position + transform.forward * CameraSystem.CameraRig.AimRayLength;
        }

        /// <summary>
        /// ⚠️⚠️ THE THROW LEAVES FROM THE SIGHT LINE, NOT THE HAND, and this was measured rather
        /// than chosen. From the hand a throw sags 0.38 to 0.43 m below the line the player is
        /// aiming along and peaks within 0.2 m of them, so the slipper drops out of the bottom of
        /// the screen the instant it is released. From the sight line the same throws sag 0.001
        /// to 0.043 m. The path was right; the starting height was not.
        /// </summary>
        public Vector3 ThrowOrigin()
        {
            var rig = UnityEngine.Camera.main != null
                ? UnityEngine.Camera.main.GetComponent<CameraSystem.CameraRig>()
                : null;

            // ⚠️ THE RIG'S OWN EYE WHEN THERE IS ONE, AND THE .gd's 0.9 FALLBACK WHEN THERE IS
            // NOT. A bot has no rig looking through it, and `throw_origin_for` uses exactly this
            // constant for that case rather than the FPP eye height, so a bot's throw and a
            // probe's throw leave from the same place.
            Vector3 eye = rig != null && rig.IsFollowing(_motor)
                ? rig.transform.position
                : transform.position + Vector3.up * 0.9f;

            Vector3 toAim = AimPoint() - eye;
            if (toAim.magnitude < 0.01f) return eye;

            return eye + toAim.normalized * Balance.MuzzleForward;
        }

        /// <summary>
        /// The velocity the throw WOULD leave with right now.
        ///
        /// ⚠️ THE AIM ARC ASKS THIS SAME FUNCTION, so the dotted line and the flight are one
        /// line by construction rather than by two implementations agreeing. A per-skin launch
        /// speed applied to only one of them would quietly land the preview where a neutral
        /// slipper lands and the real one five per cent away.
        /// </summary>
        public Vector3 LaunchVelocityNow()
        {
            if (Held == null) return Vector3.zero;

            Vector3 vel = Held.LaunchVelocityTo(ThrowOrigin(), AimPoint(), ChargeRatio);
            var ability = _motor.AbilitySystem;
            if (ability != null && ability.Kit is ZackHeroKit zack && (zack.IsOverchargeThrowActive || zack.IsThunderstrikeActive))
            {
                vel *= 1.6f;
            }
            else if (ability != null && ability.Kit is SeanHeroKit sean && sean.IsIgnitionCannonActive)
            {
                vel *= 1.3f;
            }
            else if (ability != null && ability.Kit is PhaisterHeroKit phaister && (phaister.IsWitchfireInfused || phaister.IsEclipseActive))
            {
                vel *= 1.35f;
            }
            return vel;
        }

        private void Release(float power, float spin = 0.0f)
        {
            if (Held == null) return;

            Vector3 origin = ThrowOrigin();
            Vector3 aimPoint = AimPoint();

            if (NetAuthority.ShouldRequest())
            {
                // ⚠️⚠️ THE PICTURE IS PREDICTED AND THE PHYSICS IS NOT, WHICH IS THE WHOLE SPLIT.
                // A client's throw used to send the request and do nothing else, so the arm did
                // not swing, the view did not kick and the hype did not move until the host's
                // answer came back. Everything below is presentation on the thrower's own screen
                // and cannot change where the tsinelas goes: `HostThrowAt` still decides that,
                // from the origin and aim point sent with the request.
                //
                // ⚠️ IT DOES NOT CLEAR `Held`. Optimistically emptying the hand would stop the
                // local carry moving the shoe while the wire, correctly, does not write a HELD
                // slipper's position either (see `Slipper.ApplySnapshotState`), so the tsinelas
                // would hang in the air for a round trip. It leaves the hand when the host says
                // it is in flight, which on a LAN is a frame or two.
                PredictThrowPresentation(origin, spin);

                Net.MatchRpc.Instance?.RequestThrowServerRpc(
                    _motor.PlayerSlot, origin, aimPoint, power, spin);
                return;
            }

            HostThrowAt(origin, aimPoint, power, spin);
        }

        /// <summary>
        /// What the thrower sees on the frame they let go, on a peer that does not resolve the
        /// throw.
        ///
        /// ⚠️ NO SOUND AND NO HYPE HERE, AND BOTH OMISSIONS ARE DELIBERATE. `HostThrowAt` plays
        /// `throw_release` through `NetCue`, which reaches this peer anyway, and awards the hype
        /// through `Hud.ReportStyle`, which the host now relays to the seat's owner. Predicting
        /// either would give this one player the sound twice a few tens of milliseconds apart and
        /// the hype twice at full value. What cannot arrive late is the ANIMATION, because the
        /// arm is the feedback that the press registered.
        /// </summary>
        private void PredictThrowPresentation(Vector3 origin, float spin)
        {
            GetComponentInChildren<Visual.CharacterAnimator>()?.PlayAction("throw");
            GetComponentInChildren<Visual.CharacterSquashStretch>()?.DashStretch(transform.forward, 0.14f);
        }

        // -------------------------------------------------------------------

        private void StepDefender(float dt)
        {
            var round = GameServices.Round;
            var lata = round?.Lata;
            var intent = _motor.Intent;

            bool inRing = lata != null
                          && Vector3.Distance(
                                 new Vector3(transform.position.x, 0, transform.position.z),
                                 new Vector3(lata.transform.position.x, 0, lata.transform.position.z))
                             <= Balance.InteractionRadius;

            bool canChannel = lata != null && !lata.IsUpright && inRing;

            if (!canChannel || !intent.Pressed(Verb.Grab))
            {
                // ⚠️ LETTING GO ZEROES THE CHANNEL. It does not pause and it does not decay:
                // a partial reset that survives being interrupted would let the taya nibble
                // at it between throws, which is exactly the pressure the channel creates.
                if (_channel > 0.0f) ReportResetPhase(Net.MatchRpc.ResetPhase.Cancel);
                _channel = 0.0f;
                ChannelRatio = 0.0f;
                return;
            }

            // § THE ARM DOES THE WORK, and in first person the arm IS the viewmodel: righting
            // the can is the same reach-down gesture as picking a tsinelas up. 🧑 2026-08-16:
            // *"make sure my arm moves or does an animation when i interact with objects like in
            // the real game — raise can, tag someone"*. Raising the can is this.
            //
            // ⚠️ NOT EVERY FRAME, or the clip restarts before it has moved and the arm never
            // leaves its first pose. That is the trap this branch has always guarded against.
            //
            // ⚠️⚠️ BUT ONCE PER PRESS WAS TOO FEW, AND THAT IS THE OTHER HALF OF 🧑's *"no
            // animation when ... raising lata"*. The gesture runs `ViewmodelArms.GrabSeconds`,
            // 0.40 s, and the channel is held for `Lata.ResetChannelTime`, so the arm reached
            // down once, came home, and then stood perfectly still for the rest of the hold while
            // the meter filled and the can stayed on its side. Re-firing on the read's own length
            // makes it a repeated reach, which is what righting a can looks like, and it goes
            // through the one call site so both views get it rather than the viewmodel being
            // posed behind the body's back.
            //
            // ⚠️⚠️ THE REPEAT IS GATED ON BOTH CLIPS, and either alone re-opens the trap from the
            // other side. The body's read is a one-shot whose length comes off the rig, so waiting
            // only on `IsPlayingAction` would restart the arm every frame on a rig with no
            // `pick-up`: `PlayOneShot` returns silently on a missing clip and the timer never
            // starts. Waiting only on the arm's length would cut a longer body clip off
            // part-played. Both means the read repeats at the pace of whichever view is slower.
            //
            // ⚠️ `ReportResetPhase(Start)` STAYS ON THE OPENING FRAME ALONE. It is a network
            // event announcing that a channel began, not a cosmetic one, and firing it four times
            // for one hold would tell every peer the reset restarted three times.
            var anim = GetComponentInChildren<Visual.CharacterAnimator>();

            bool opening = _channel <= 0.0f;
            bool bodyFinished = anim == null || !anim.IsPlayingAction;
            bool armFinished = _channel - _lastResetGesture >= CameraSystem.ViewmodelArms.GrabSeconds;

            if (opening || (bodyFinished && armFinished))
            {
                _lastResetGesture = _channel;
                anim?.PlayAction("grab");
            }

            // ⚠️ AND NO CUE HERE. `Lata.SetUpright` owns the channel's sound off the can's own
            // state; a `PlayAt` on this frame would be a second source for the same event, which
            // matters more now that the read above fires more than once.
            if (opening) ReportResetPhase(Net.MatchRpc.ResetPhase.Start);

            _channel += dt;
            ChannelRatio = Mathf.Clamp01(_channel / lata.ResetChannelTime);

            if (_channel < lata.ResetChannelTime) return;

            // ⚠️⚠️ THE CLIENT SHOWS ITS OWN BAR AND THE HOST STANDS THE CAN UP, AND THAT SPLIT IS
            // THE WHOLE POINT. Before this the channel ran entirely locally and called
            // `Lata.HostRestore` from whichever peer was holding the key, so a client righted the
            // can on its own screen, the host's stream knocked it straight back down, and the taya
            // saw the reset flicker and fail. The bar is prediction; the restore is a decision, and
            // only one process makes decisions.
            GetComponentInChildren<Visual.CharacterSquashStretch>()?.Stretch(0.18f);
            _channel = 0.0f;
            ChannelRatio = 0.0f;

            if (NetAuthority.ShouldRequest())
            {
                ReportResetPhase(Net.MatchRpc.ResetPhase.Complete);
                return;
            }

            lata.HostRestore();
            UI.Hud.ReportStyle(_motor.PlayerSlot, 24.0f, "BANGON!");
            Net.MatchRpc.Instance?.BroadcastLataState();
        }

        /// <summary>
        /// Tells the host where this seat's channel has got to. Silent offline and on the host,
        /// which resolve the channel directly.
        /// </summary>
        private void ReportResetPhase(Net.MatchRpc.ResetPhase phase)
        {
            if (!NetAuthority.ShouldRequest()) return;
            if (_motor.PlayerSlot != NetAuthority.LocalSlot) return;

            Net.MatchRpc.Instance?.RequestLataResetServerRpc(_motor.PlayerSlot, phase);
        }

        private void CancelAll()
        {
            CancelCharge();
            _channel = 0.0f;
            ChannelRatio = 0.0f;
        }
    }
}
