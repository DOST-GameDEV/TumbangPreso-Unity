using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// ⚠️ THE TIERS ARE `Difficulty` IN THE CORE PACKAGE — Bata / Normal / Astig, by their
    /// Filipino names as in the original. This alias exists only so older serialized scenes
    /// that stored Easy/Normal/Hard still deserialize; new code takes `Difficulty`.
    public enum AiTier { Easy = 0, Normal = 1, Hard = 2 }

    /// <summary>
    /// A bot seat.
    ///
    /// ⚠️⚠️ IT PRESSES BUTTONS. IT DOES NOT CALL GAMEPLAY METHODS. Every decision this class
    /// makes ends as a write to <see cref="InputIntent"/>, the same table a human's keyboard
    /// writes, and one physics step serves both. That indirection is the single reason there
    /// is no second code path where a bot can do something a player cannot, or dodge a rule a
    /// player is held to, and it is why this file is a transcription in the port rather than a
    /// redesign.
    ///
    /// Every shortcut here is a temptation to break that. "Just call ResolveTag directly, it
    /// is only for the AI" is how a bot ends up tagging through a rule the human obeys.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class AIController : MonoBehaviour
    {
        [SerializeField] private AiTier _tier = AiTier.Normal;

        /// <summary>
        /// This bot's tuning row. ⚠️ READ THROUGH <see cref="AiTuning"/> RATHER THAN COPIED
        /// INTO FIELDS, so a difficulty changed from the pause menu mid-match reaches bots
        /// that were spawned before the change. Godot did this with a `tuning_stamp` each
        /// controller compared against; a property read is the same guarantee for free.
        /// </summary>
        public static Difficulty ActiveDifficulty = Difficulty.Normal;

        /// <summary>
        /// Godot's `AIController.apply_difficulty()`, called off the saved setting index.
        ///
        /// ⚠️ NOTHING CALLED THIS BEFORE, so the difficulty in the settings panel was saved,
        /// displayed, and then ignored — every bot in every match played at Normal. The
        /// index is clamped rather than trusted: it comes off disk.
        /// </summary>
        public static void ApplyDifficulty(int savedIndex)
            => ActiveDifficulty = (Difficulty)Mathf.Clamp(savedIndex, 0, 2);

        public static void ApplyDifficultyFromSettings()
            => ApplyDifficulty(Settings.SettingsStore.Current.AiDifficulty);

        private AiPersonality Me => AiTuning.For(ActiveDifficulty);

        /// <summary>
        /// This bot's own jitter on top of the tier, seeded from its SEAT so two runs of the
        /// same match give the same four characters.
        /// </summary>
        private AiPersonalityRoll _self;

        /// <summary>The plan this bot is committed to, and the clocks that hold it.
        ///
        /// ⚠️ A PLAN IS CHOSEN ON A THINK TICK AND HELD, NOT RE-DECIDED EVERY FRAME. A bot
        /// that re-evaluates continuously oscillates between two nearly-equal options and
        /// reads as indecisive rather than as thinking.</summary>
        public AiPlan Plan { get; private set; } = AiPlan.Idle;

        private float _thinkLeft;
        private float _commitLeft;

        /// <summary>key -> seconds a condition has been continuously true. A reaction is a
        /// condition HELD for the tier's React time, not an instant trigger — which is what
        /// stops a bot answering something it could not have seen yet.</summary>
        private readonly Dictionary<string, float> _gates = new Dictionary<string, float>();

        /// <summary>
        /// Has <paramref name="condition"/> been true long enough for this bot to react to it?
        /// Resets the moment it stops being true, so a flicker never accumulates.
        /// </summary>
        private bool Reacted(string key, bool condition, float dt)
        {
            if (!condition) { _gates[key] = 0.0f; return false; }

            float held = (_gates.TryGetValue(key, out float h) ? h : 0.0f) + dt;
            _gates[key] = held;

            return held >= Me.React * _self.Nerves;
        }

        /// <summary>
        /// Re-plan, at most once per think tick, and never while a hesitation beat is running.
        /// </summary>
        private void StepPlan(float dt)
        {
            _thinkLeft -= dt;
            _commitLeft = Mathf.Max(0.0f, _commitLeft - dt);

            if (_thinkLeft > 0.0f || _commitLeft > 0.0f) return;

            _thinkLeft = Me.Think * _self.Tempo;

            AiPlan chosen = _motor.IsDefender ? PlanDefender(dt) : PlanAttacker(dt);
            if (chosen == Plan) return;

            // ⚠️ A NEW PLAN COSTS A BEAT. Per-bot, so the three of them do not hesitate
            // together — which is what stops a plan change reading as a broadcast.
            _commitLeft = _self.Hesitation;
            Plan = chosen;

            // A new plan gets a new goal. Carrying the last one over is how a bot ends up
            // walking to a throwing spot it chose two verbs ago.
            if (chosen != AiPlan.Position) _goal = Vector3.zero;
        }

        /// <summary>Where you stand to throw: just outside the chalk.</summary>
        private const float ThrowStandoff = AiTuning.ThrowStandoff;

        /// ⚠️ WAS 0.35 AND THAT WAS A DIVERGENCE, NOT A CHOICE. The .gd has 0.55; the tighter
        /// value made bots jitter on arrival instead of settling on their mark.
        private const float ArriveSlop = AiTuning.ArriveSlop;

        /// <summary>
        /// ⚠️ THE ANSWER TO "EVERY BOT CONVERGES ON THE NEAREST SLIPPER". Only the nearest
        /// eligible attacker goes for a loose slipper, so three bots do not stack on one.
        /// </summary>
        private const float ClaimSlack = 0.5f;

        /// <summary>
        /// ⚠️⚠️ A DISTANCE HANDICAP ON A HUMAN'S OWN SLIPPER, NOT A BAN. Any attacker may take
        /// any slipper, which is what keeps the three-way rivalry real. But without this a bot
        /// takes a human's slipper whenever it is one metre nearer, which reads as being
        /// griefed rather than contested. The instruction was explicit: bots may take from
        /// you, but not all the time. So a human's own slipper is treated as further away than
        /// it is, and a bot only goes for it when that is CLEARLY the better play.
        /// </summary>
        private const float HumanSlipperBias = 3.5f;

        private CharacterMotor _motor;
        private Carrier _carrier;
        private float _repathTimer;
        private Vector3 _goal;
        private float _emoteCooldown;

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            _carrier = GetComponent<Carrier>();
            _self = new AiPersonalityRoll(_motor.PlayerSlot);
        }

        private void OnEnable()
        {
            var round = GameServices.Round;
            if (round != null) round.Tagged += OnRoundTagged;
        }

        private void OnDisable()
        {
            var round = GameServices.Round;
            if (round != null) round.Tagged -= OnRoundTagged;
        }

        private void OnRoundTagged(int defenderSlot, int attackerSlot)
        {
            if (_motor.PlayerSlot == defenderSlot)
            {
                // Defender scores tag celebration
                string[] emotes = { "yes", "dance", "bow", "crouch" };
                TryTriggerEmote(emotes[UnityEngine.Random.Range(0, emotes.Length)], 0.85f);
            }
            else if (_motor.PlayerSlot == attackerSlot)
            {
                // Attacker tagged
                TryTriggerEmote("no", 0.55f);
            }
        }

        public void TryTriggerEmote(string emoteKey, float chance = 1.0f)
        {
            if (_emoteCooldown > 0.0f) return;
            if (UnityEngine.Random.value > chance) return;

            var ep = GetComponent<Social.EmotePlayer>();
            if (ep != null && ep.CanEmote())
            {
                ep.HostPlay(emoteKey);
                _emoteCooldown = UnityEngine.Random.Range(3.5f, 7.0f);
            }
        }

        private void Update()
        {
            var intent = _motor.Intent;

            if (!_motor.CanAct())
            {
                ReleaseAll(intent);
                return;
            }

            float dt = Time.deltaTime;
            if (_emoteCooldown > 0.0f) _emoteCooldown -= dt;

            Observe(dt);

            // ⚠️ THE PLAN IS CHOSEN HERE AND THE VERB WORK BELOW OBEYS IT. Deciding inside
            // the verb code is what produced a bot that re-decided every frame.
            _stalkTime = Plan == AiPlan.Stalk ? _stalkTime + dt : 0.0f;
            StepUnstick(dt);
            StepPlan(dt);

            Act(intent, dt);
            StepHeroAbilities(intent, dt);

            // ⚠️⚠️ NO COMMIT HERE ANY MORE, AND IT USED TO BE ON THIS LINE. The snapshot is taken
            // by the consumer at the end of `CharacterMotor.FixedUpdate`, not by each producer at
            // the end of its own Update. A producer that commits its own frame erases the press
            // edge before the physics step that resolves the verb ever runs, which silently gave
            // every bot a shove and a lunge that never fired. See the long note in
            // `PlayerInputReader.Update`; it is the same fault for a bot and for a human, which
            // is exactly the property `InputIntent` exists to guarantee.
        }

        /// <summary>
        /// THE ATTACKER: retrieve, get an angle, throw — and stay alive in between.
        ///
        /// ⚠️⚠️ THE ORDER IS THE PRIORITY AND IT IS NOT ARBITRARY. Evading a lunge beats
        /// everything, because being tagged costs the round's whole point. Sabotage is checked
        /// three separate times in the original — before the fetch branch, inside the
        /// can't-throw branch, and again before the windup — because the opportunity is
        /// fleeting and a bot that only checks it once misses it.
        /// </summary>
        private AiPlan PlanAttacker(float dt)
        {
            var round = GameServices.Round;
            var lata = round?.Lata;
            var taya = round != null ? DefenderOf(round) : null;

            if (ShouldEvade(taya, dt)) return AiPlan.Evade;
            if (SabotageTarget(taya) != null) return AiPlan.Sabotage;

            if (_carrier == null || _carrier.Held == null)
            {
                var mine = MySlipper();

                // No slipper of mine to fetch, or it is still in the air: take an angle
                // instead of chasing something that has not landed.
                if (mine == null || mine.State == SlipperState.InFlight) return AiPlan.Position;

                return FetchIsSafe(mine, taya) ? AiPlan.Fetch : AiPlan.Stalk;
            }

            // ⚠️ ARMED AND INSIDE THE BOX IS THE ONE TAGGABLE STATE. Getting out beats
            // everything else an attacker could be doing.
            if (Confinement.IsInsideBox(transform.position.x, transform.position.z))
                return AiPlan.Withdraw;

            if (lata == null || !lata.IsUpright || !round.CanThrow(_motor))
            {
                if (SabotageTarget(taya) != null) return AiPlan.Sabotage;
                return AiPlan.Position;
            }

            if (SabotageTarget(taya) != null) return AiPlan.Sabotage;
            if (_carrier.ThrowLocked) return AiPlan.Position;

            // Arrived on a throwing spot: plant and charge. Staying in Windup once entered is
            // deliberate — a bot that re-decides mid-charge never releases.
            if (Plan == AiPlan.Windup) return AiPlan.Windup;
            if (_arrived && (Plan == AiPlan.Position || Plan == AiPlan.Windup))
                return AiPlan.Windup;

            return AiPlan.Position;
        }

        /// <summary>
        /// THE TAYA.
        ///
        /// ⚠️⚠️ A DOWNED LATA BEATS EVERYTHING. No tag is legal until it is standing, so a
        /// taya that hunts with the can on its side is spending the round on a verb that
        /// cannot score. Reset first, then hunt.
        /// </summary>
        private AiPlan PlanDefender(float dt)
        {
            var round = GameServices.Round;
            var lata = round?.Lata;

            if (lata == null) return AiPlan.Idle;
            if (!lata.IsUpright) return AiPlan.Reset;

            // Stepping into a slipper already in the air. Gated on a held reaction, so a bot
            // cannot answer a throw on the frame it leaves the hand.
            if (Me.Intercept > 0.0f && HasInterceptPoint(lata))
            {
                if (Reacted("incoming", true, dt)) return AiPlan.Intercept;
            }
            else
            {
                _gates["incoming"] = 0.0f;
            }

            if (TagTarget() != null) return AiPlan.Hunt;
            if (Me.Camp > 0.0f && HasCoverPoint(lata)) return AiPlan.Cover;

            return AiPlan.Guard;
        }

        private bool _arrived;

        /// <summary>Seconds spent continuously in Stalk. See <see cref="FetchIsSafe"/>.</summary>
        private float _stalkTime;

        // -------------------------------------------------------------------
        // UNSTICKING — a general safety net rather than a fix for one plan.
        //
        // ⚠️⚠️ A BOT CAN PRESS A DIRECTION AND GO NOWHERE, AND NOTHING ELSE HERE WOULD EVER
        // NOTICE. The planner sets a goal, the mover presses toward it, and neither reads
        // back whether the body actually moved. Against a wall, a kerb or another unit, that
        // is a bot leaning into geometry for the rest of the round while its plan stays
        // perfectly reasonable. This watches the RESOLVED speed instead of the intent.
        // -------------------------------------------------------------------

        private float _stuckTime;
        private float _unstickLeft;
        private float _unstickSign = 1.0f;

        /// <summary>True on any frame this bot actually asked to move. Cleared each step, so
        /// standing still on purpose is never mistaken for being stuck.</summary>
        private bool _driving;

        private void StepUnstick(float dt)
        {
            if (_unstickLeft > 0.0f)
            {
                _unstickLeft = Mathf.Max(0.0f, _unstickLeft - dt);
                return;
            }

            var v = _motor.Velocity;
            float speed = new Vector2(v.x, v.z).magnitude;

            if (_driving && speed < AiTuning.StuckSpeed)
            {
                _stuckTime += dt;

                if (_stuckTime >= AiTuning.StuckTrigger)
                {
                    _stuckTime = 0.0f;
                    _unstickLeft = AiTuning.UnstickTime;

                    // Alternate, so a bot that picks the wrong way out of a corner does not
                    // keep picking it.
                    _unstickSign = -_unstickSign;
                }
            }
            else
            {
                _stuckTime = 0.0f;
            }

            _driving = false;
        }

        /// <summary>
        /// ⚠️⚠️ PATIENCE IS BOUNDED, AND IT COST A WHOLE ROUND BEFORE IT WAS. Measured: one bot
        /// spent 64.4 s of a 90 s round in STALK — 13 throws against 22-28 for the other three
        /// — because the taya camped its slipper and every "the taya is busy" condition below
        /// stayed false. That is correct reasoning with no stopping rule, which is not what a
        /// person does: a player who cannot get a free run eventually takes an unfree one.
        ///
        /// The bound is a tier property, so a cautious bot waits longer than a reckless one.
        /// **Do not remove the first branch to make bots "smarter".**
        /// </summary>
        private bool FetchIsSafe(Slipper mine, CharacterMotor taya)
        {
            if (Me.FetchCaution <= 0.0f || taya == null) return true;

            // Waited long enough or unretrieved stall timer running. Go anyway to avoid penalty and keep game moving!
            if (_stalkTime >= 2.0f + Me.FetchCaution * 0.4f) return true;

            // The can is down: nobody can be tagged at all, so the run is free.
            var lata = GameServices.Round?.Lata;
            if (lata != null && !lata.IsUpright) return true;

            // The taya just spent their lunge.
            var tayaVerbs = taya.GetComponent<CombatVerbs>();
            if (tayaVerbs != null && tayaVerbs.LungeCooldownLeft > 0.35f) return true;

            // Somebody ELSE is taggable, so the taya has a better target than me.
            foreach (var who in GameServices.Round.Players)
                if (who != null && who != _motor && who.IsTaggable()) return true;

            // Or it is simply far enough from them to risk.
            return Flat(taya.transform.position, mine.transform.position) > Me.FetchCaution;
        }

        /// <summary>
        /// Is a lunge winding up at me right now?
        ///
        /// ⚠️ IT READS THE TAYA'S OBSERVED CHARGE, NOT THEIR INTENT. A bot that could see the
        /// intent table would dodge a lunge before the animation started, which no player can
        /// do. The 4.5 m gate is the range past which a lunge cannot reach anyway.
        /// </summary>
        private bool ShouldEvade(CharacterMotor taya, float dt)
        {
            if (Me.Dodge <= 0.0f || taya == null || !_motor.IsTaggable())
            {
                _gates["lunge"] = 0.0f;
                return false;
            }

            // ⚠️⚠️ `ObservedLungeCharge`, NOT `LungeChargeRatio`, AND THE WRONG ONE WAS ALWAYS
            // TRUE. `LungeChargeRatio` is a `Clamp01`, so `>= 0.0f` against it is a tautology and
            // this read "a lunge is winding up at me" on every frame the taya was within 4.5 m —
            // which turned a reaction to a TELL into a proximity rule, and spent the dodge
            // budget on nothing. The .gd asks `observed_lunge_charge() >= 0.0`, whose rest value
            // is -1 precisely so that comparison means something.
            var verbs = taya.GetComponent<CombatVerbs>();
            bool winding = verbs != null && verbs.ObservedLungeCharge >= 0.0f
                           && Flat(transform.position, taya.transform.position) < 4.5f;

            return Reacted("lunge", winding, dt);
        }

        /// <summary>
        /// A rival worth shoving into the taya's reach.
        ///
        /// ⚠️ THE KNOB IS A REACH, NOT A COIN FLIP. Measured over a whole match at Normal:
        /// ZERO sabotages, because willingness was only ever read as "> 0" against a fixed
        /// 4.16 m radius — while `Spacing` is deliberately pushing the three attackers apart,
        /// so two of them are rarely that close. A willingness dial that changes nothing is
        /// the same defect as a control that does nothing, so the value scales the radius.
        /// </summary>
        private CharacterMotor SabotageTarget(CharacterMotor taya)
        {
            if (Me.Sabotage <= 0.0f || taya == null) return null;
            if (GameServices.Round == null) return null;

            var myVerbs = GetComponent<CombatVerbs>();
            if (myVerbs != null && myVerbs.ShoveCooldownLeft > 0.0f) return null;

            // A shove you cannot pay for is not an option.
            if (_motor.Stamina.Current < Balance.ShoveStaminaCost + 2.0f) return null;

            float reach = 4.16f * Me.Sabotage;
            CharacterMotor best = null;
            float bestDistance = reach;

            foreach (var who in GameServices.Round.Players)
            {
                if (who == null || who == _motor || who.IsDefender) continue;

                float d = Flat(transform.position, who.transform.position);
                if (d > bestDistance) continue;

                best = who;
                bestDistance = d;
            }

            return best;
        }

        /// <summary>
        /// MY slipper — including one I threw that is still in the air.
        ///
        /// ⚠️⚠️ THE IN-FLIGHT CASE IS NOT OPTIONAL AND LEAVING IT OUT COST HALF THE OFFENCE.
        /// The planner asks this while not holding, then checks whether it is flying so the
        /// bot can walk to where its own throw will LAND. A version that only considered
        /// loose slippers made a bot's slipper invisible to it the instant it was released:
        /// measured throws 27 → 14, hit rate 48.1% → 28.6%, and defence went from 31.7% to
        /// 70.8% of every point scored. The bots threw once and stood still.
        /// </summary>
        private Slipper MySlipper()
        {
            foreach (var s in FindObjectsByType<Slipper>(FindObjectsInactive.Exclude))
                if (s.OwnerSlot == _motor.PlayerSlot) return s;

            return null;
        }

        /// <summary>
        /// Where a slipper already in the air can actually be stepped into, or null.
        ///
        /// ⚠️⚠️ "SOMETHING IS IN FLIGHT" IS NOT AN INTERCEPT AND THIS USED TO RETURN EXACTLY
        /// THAT. The plan then ran with no point to run to, so the taya committed to a verb it
        /// could not execute and stood still while a throw sailed past. `_intercept_point()`
        /// walks the arc and takes the first sample inside the band a body can actually block.
        ///
        /// ⚠️ AND THE BAND IS HALF A CAPSULE, NOT THE WHOLE ARC. A slipper passing two metres
        /// overhead is unblockable, and running under it is running nowhere.
        /// </summary>
        private bool TryInterceptPoint(out Vector3 point)
        {
            point = Vector3.zero;

            foreach (var s in FindObjectsByType<Slipper>(FindObjectsInactive.Exclude))
            {
                if (s.State != SlipperState.InFlight) continue;

                Vector3 p = s.transform.position;
                Vector3 v = s.Velocity;
                float t = 0.0f;

                while (t < AiTuning.InterceptHorizon)
                {
                    t += AiTuning.InterceptStep;

                    Vector3 at = p + v * t
                                 + Vector3.down * (0.5f * Balance.Gravity * t * t);

                    float rise = at.y - transform.position.y;
                    if (rise < -AiTuning.InterceptBand || rise > AiTuning.InterceptBand) continue;

                    point = new Vector3(at.x, transform.position.y, at.z);
                    return true;
                }
            }

            return false;
        }

        private bool HasInterceptPoint(Lata lata) => TryInterceptPoint(out _);

        /// <summary>
        /// Walks the arc the slipper will actually fly and asks the same question the flight
        /// itself will ask of it, frame by frame.
        ///
        /// ⚠️⚠️ NOTHING IN THIS PORT ASKED IT AT ALL, so a bot threw straight through whoever
        /// happened to be standing between it and the can, every time, and read as an AI that
        /// simply misses. The .gd has had this since the AI rewrite.
        ///
        /// ⚠️ IT RETURNS TRUE FOR A THROW THAT NEVER ARRIVES, and that is not a shortcut: a
        /// shot that falls short is as useless as a blocked one, and the bot should go and find
        /// a better angle either way.
        /// </summary>
        private bool LaneBlocked(Vector3 origin, Vector3 target, float power)
        {
            var round = GameServices.Round;
            if (round == null) return false;

            int skin = _carrier != null && _carrier.Held != null ? _carrier.Held.SkinIndex : -1;
            float speed = ThrowRules.LaunchSpeedFor(skin, power);

            Vector3 flat = target - origin;
            flat.y = 0.0f;

            float distance = flat.magnitude;
            if (distance < 0.01f) return false;

            // ⚠️ THE SAME SOLVE THE THROW ITSELF USES, which is what makes this a prediction
            // rather than a second opinion. It was a fixed 45-degree lob here too, so the bot
            // was walking an arc the game does not fly and answering about the wrong lane.
            Vector3 launch = Slipper.SolveArc(origin, target, speed) * speed;

            float step = Mathf.Clamp(AiTuning.LaneSampleArc / Mathf.Max(speed, 1.0f),
                                     AiTuning.LaneStepMin, AiTuning.LaneStepMax);

            float t = 0.0f;

            for (int i = 0; i < AiTuning.LaneMaxSteps; i++)
            {
                t += step;

                Vector3 point = origin + launch * t
                                + Vector3.down * (0.5f * Balance.Gravity * t * t);

                if (Flat(point, target) <= Balance.SlipperHitRadius + 0.30f)
                    return false;                       // it gets there

                if (point.y < target.y - 1.0f)
                    return true;                        // it fell short of the can's hit band

                foreach (var who in round.Players)
                {
                    if (who == null || who == _motor) continue;

                    // ⚠️ THE CAPSULE COMES OFF THE CONTROLLER, not off a constant. The one
                    // number that must not be guessed here is how wide a body is, because it
                    // is what decides whether a sample falls between two people.
                    var body = who.GetComponent<CharacterController>();
                    float radius = body != null ? body.radius : 0.35f;
                    float height = body != null ? body.height : CameraSystem.CameraRig.PersonCapsuleHeight;

                    if (Flat(point, who.transform.position) >
                        Balance.SlipperHitRadius + radius) continue;

                    float rise = point.y - who.transform.position.y;
                    if (rise < 0.0f || rise > height) continue;

                    return true;
                }
            }

            return true;
        }

        /// <summary>
        /// Is there a loose slipper whose retrieval line is worth sitting on?
        ///
        /// ⚠️ CAMPING IS A DESIGNED TAYA BEHAVIOUR, not an exploit — it is what puts the
        /// attacker's patience under real pressure and is why FetchIsSafe needs a bound.
        /// </summary>
        private bool HasCoverPoint(Lata lata)
        {
            foreach (var s in FindObjectsByType<Slipper>(FindObjectsInactive.Exclude))
                if (s.State == SlipperState.Loose) return true;

            return false;
        }

        /// <summary>An attacker the taya could legally tag right now.</summary>
        private CharacterMotor TagTarget()
        {
            var round = GameServices.Round;
            if (round == null) return null;

            // ⚠️⚠️ NOBODY IS TAGGABLE WHILE THE LATA IS DOWN, AND THE BOT HAS TO KNOW THAT.
            // Reported as "AI still doesnt TAG" and measured at ONE tag across two rounds
            // while attackers spent 67 combined seconds taggable. The AI was not the bug:
            // every tag verb opens with "a tag requires the can standing" and returns early,
            // so a bot hunting with the can down is spending the round on a verb that cannot
            // fire. Reset first, then hunt.
            if (round.Lata == null || !round.Lata.IsUpright) return null;

            foreach (var who in round.Players)
                if (who != null && who != _motor && !who.IsDefender && who.IsTaggable())
                    return who;

            return null;
        }

        /// <summary>Whoever holds the taya role this round.</summary>
        private static CharacterMotor DefenderOf(RoundDirector round)
        {
            foreach (var p in round.Players)
                if (p != null && p.IsDefender) return p;

            return null;
        }

        /// <summary>Snap a heading to the eight directions a keyboard can produce.</summary>
        private static Vector2 EightWay(Vector3 dir)
        {
            float x = 0.0f, z = 0.0f;

            if (dir.x > AiTuning.EightWayThreshold) x = 1.0f;
            else if (dir.x < -AiTuning.EightWayThreshold) x = -1.0f;

            if (dir.z > AiTuning.EightWayThreshold) z = 1.0f;
            else if (dir.z < -AiTuning.EightWayThreshold) z = -1.0f;

            var v = new Vector2(x, z);

            // A diagonal is still one unit of speed, exactly as the input system normalises a
            // two-key press — otherwise a bot moving diagonally outruns a player doing the same.
            return v.sqrMagnitude > 1.0f ? v.normalized : v;
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0.0f;
            b.y = 0.0f;
            return Vector3.Distance(a, b);
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE PLAN IS THE THING THAT ACTS. THE PORT COMPUTED A PLAN AND THEN IGNORED IT.
        ///
        /// `PlanAttacker`/`PlanDefender` were transcribed faithfully, and what consumed them was
        /// a pair of hand-written routines that re-derived a much simpler behaviour from
        /// scratch. So thirteen plans existed, were chosen, were held through their commit beat
        /// — and then nothing read the answer. Everything the plans exist to express went with
        /// it: Evade, Sabotage, Stalk, Withdraw, Intercept, Cover and Reset had no bodies at
        /// all, the taya never punched and never charged a lunge, and no bot ever loitered,
        /// separated, or picked a throwing bearing.
        ///
        /// This is `ai_controller.gd::_act()`, which is a `match` on the plan and nothing else.
        ///
        /// ⚠️ AND THE RELEASE SWEEP AT THE BOTTOM IS LOAD-BEARING. An intent table is STICKY: it
        /// holds whatever was last written, so a plan that simply stops mentioning a verb leaves
        /// the previous plan's press held for the rest of the round. The .gd records what the
        /// naive version of that sweep cost — it tested the PLAN ID rather than whether the
        /// button had been touched, and since the punch and the throw charge share one action,
        /// the janitor cleared the taya's punch in the same frame the hunt pressed it. Measured
        /// then: the punch fired zero times in the game's life, reported as *"AI cant tag human
        /// for some reason"*. Ask what was touched, never which plan is running.
        /// </summary>
        private void Act(InputIntent intent, float dt)
        {
            _touched.Clear();

            // ⚠️ THE STICK IS CLEARED FIRST, THE VERBS ARE NOT. Every branch below either
            // drives or stops, exactly as the .gd's do, and clearing here makes "the plan
            // forgot to move" a stand rather than the last plan's heading held forever. The
            // VERBS cannot be cleared the same way: a throw charge and a lunge are both held
            // across frames on purpose, which is what the touch sweep at the bottom is for.
            intent.Move = Vector2.zero;
            intent.Set(Verb.Sprint, false);

            switch (Plan)
            {
                case AiPlan.Idle:      DoIdle(intent); break;
                case AiPlan.Fetch:     DoFetch(intent); break;
                case AiPlan.Stalk:     DoStalk(intent); break;
                case AiPlan.Withdraw:  DoWithdraw(intent); break;
                case AiPlan.Position:  DoPosition(intent); break;
                case AiPlan.Windup:    DoWindup(intent, dt); break;
                case AiPlan.Evade:     DoEvade(intent); break;
                case AiPlan.Sabotage:  DoSabotage(intent); break;
                case AiPlan.Reset:     DoReset(intent); break;
                case AiPlan.Intercept: DoIntercept(intent); break;
                case AiPlan.Hunt:      DoHunt(intent, dt); break;
                case AiPlan.Cover:     DoCover(intent); break;
                case AiPlan.Guard:     DoGuard(intent); break;
            }

            if (!_touched.Contains(Verb.SpecialAbility)) Press(intent, Verb.SpecialAbility, false);
            if (Plan != AiPlan.Windup) _windup = false;
            if (!_touched.Contains(Verb.Lunge)) Press(intent, Verb.Lunge, false);
            if (Plan != AiPlan.Hunt) _lungeHeld = -1.0f;
            if (!_touched.Contains(Verb.Grab)) Press(intent, Verb.Grab, false);
        }

        // ---- ATTACKER VERBS -------------------------------------------------

        private void DoFetch(InputIntent intent)
        {
            var mine = MySlipper();

            if (mine == null) { Stop(intent); return; }

            Vector3 where = mine.transform.position;
            float distance = Flat(transform.position, where);

            // ⚠️ SPRINT THE LAST STRETCH INTO THE BOX AND NOTHING ELSE. The retrieval is the
            // only moment an attacker is taggable, and the whole bar is 1.25 s of sprint.
            // Spending it anywhere else spends it where it does not matter.
            bool hurry = distance > AiTuning.Reach
                         && (MineIsExposed(mine) || distance > AiTuning.SprintDistance);

            Goto(intent, where, AiTuning.Reach * 0.75f, hurry);

            // ⚠️ THE PICKUP IS A TAP AND A HELD BUTTON WOULD DO NOTHING AT ALL. The carrier
            // reads `JustPressed`, so holding produces exactly one edge in a lifetime and then
            // a bot that stands on its own slipper for the rest of the round. Tap alternates,
            // so an edge lands every other frame for as long as it is in range.
            if (distance <= AiTuning.Reach) Tap(intent, Verb.Grab);
            else Press(intent, Verb.Grab, false);
        }

        /// <summary>True while the slipper is somewhere the taya can contest. Decides a sprint
        /// against a walk and nothing else.</summary>
        private bool MineIsExposed(Slipper mine)
        {
            var taya = GameServices.Round != null ? DefenderOf(GameServices.Round) : null;
            return taya != null && Flat(At(taya), mine.transform.position) < 4.5f;
        }

        private void DoStalk(InputIntent intent)
        {
            // My slipper is in the box and the taya is sitting on it. Standing on the line
            // outside, at my own bearing, is the safest place to be and the place the run
            // starts from, and it keeps the bot MOVING, which is what a person waiting for an
            // opening actually looks like.
            var mine = MySlipper();
            Vector3 anchor = mine != null ? mine.transform.position : Vector3.zero;
            float bearing = Mathf.Atan2(anchor.x, anchor.z);

            Goto(intent, RingPoint(bearing, Balance.ConfinementRadius + 0.6f),
                 AiTuning.ArriveSlop, false);

            if (_arrived) Loiter(intent);
        }

        private void DoWithdraw(InputIntent intent)
        {
            // Straight out along the bearing already held: a step back, not a lap of the
            // arena, and sprinting, because this is the taggable window.
            Goto(intent, SafeSpot(), AiTuning.ArriveSlop, true);
        }

        private void DoPosition(InputIntent intent)
        {
            if (_carrier == null || _carrier.Held == null)
            {
                // Waiting for my own throw to resolve: walk to where it will come down, so the
                // retrieval starts from the right side of the court.
                var mine = MySlipper();

                // ⚠️ NOTHING FETCHABLE MEANS GO WHERE ONE WILL BE. `MySlipper` is null whenever
                // every slipper is in a hand or in the air, and standing still through that
                // window is the reported bug. The nearest slipper already in flight is the one
                // that becomes available first, whoever threw it.
                if (mine == null) mine = NearestFlyingSlipper();

                if (mine != null && mine.State == SlipperState.InFlight
                    && TryPredictedLanding(mine, out Vector3 landing))
                {
                    Goto(intent, PullOutside(landing, 0.4f), AiTuning.ArriveSlop, false);
                    return;
                }

                // ⚠️ STILL NOTHING IN THE AIR: WAIT ON THE THROWING RING, NOT WHEREVER YOU
                // HAPPEN TO BE STANDING. Loitering alone is what a screenshot caught — three
                // empty-handed attackers milling about wherever their last plan left them,
                // which from outside reads as the bots having given up.
                if (!_goalValid) { _goal = ThrowSpot(); _goalValid = true; }

                Goto(intent, _goal, AiTuning.ArriveSlop, false);
                if (_arrived) Loiter(intent);
                return;
            }

            if (!_goalValid) { _goal = ThrowSpot(); _goalValid = true; }

            Goto(intent, _goal, AiTuning.ArriveSlop,
                 Flat(transform.position, _goal) > AiTuning.SprintDistance);

            Claim(Mathf.Atan2(_goal.x, _goal.z));

            // ⚠️⚠️ ARRIVING IS NOT A REASON TO STOP EXISTING, AND THIS COST A MEASUREMENT IN THE
            // ORIGINAL. Two bots stood still for 22 s and 57 s inside live rounds, both here:
            // armed, in position, and refused the throw because the lata was lying down, so
            // they walked to their spot, arrived, and politely stopped for as long as the taya
            // took to stand it up. A plan that can WAIT needs somewhere to put the waiting.
            if (_arrived) Loiter(intent);
        }

        /// <summary>
        /// The wind-up: solve the power, hold the charge, and release only into a clear lane.
        ///
        /// ⚠️⚠️ A BOT HOLDS THE SHOT RATHER THAN FIRING THE INSTANT IT IS CHARGED, and that is
        /// not flavour. Nothing in this file reads whether a player is human, but the taya's
        /// threat model pays for "is charging" — a person aims for most of the 2.5 s and a bot
        /// that released on the first legal frame was charging for a fraction of one, so the
        /// only attacker ever visibly winding up was the human and the taya guarded them
        /// permanently. Reported from a playtest as *"the defender ai only attack him"*. It is
        /// also the counterplay the charge exists for, in the other direction: a human taya can
        /// now read a bot the same way a bot taya reads them.
        ///
        /// ⚠️ THE HOLD IS DERIVED FROM `AimSettle`, NOT A NEW TIER KNOB. Bata carries the 99.0
        /// "never settles" sentinel and therefore holds nothing, which is exactly the impatient
        /// kid it is meant to be; Normal holds 0.91 s and Astig 0.52 s, because a better player
        /// lines a shot up faster rather than staring at it longer.
        /// </summary>
        private void DoWindup(InputIntent intent, float dt)
        {
            var round = GameServices.Round;
            var lata = round?.Lata;

            if (lata == null || _carrier == null || _carrier.Held == null
                || !round.CanThrow(_motor))
            {
                // The gate closed under us. The carrier has already cancelled the charge, so
                // holding the button here would wait out the timeout for nothing.
                _windup = false;
                Press(intent, Verb.SpecialAbility, false);
                Plan = AiPlan.Position;
                _goalValid = false;
                return;
            }

            if (!_windup)
            {
                _windup = true;
                _windupTime = 0.0f;
                _windupWait = 0.0f;
                _blundering = Blunder();
                _windupScatter = RollScatter();
                _windupPower = PlanPower(lata);
            }

            _windupTime += dt;
            Stop(intent);

            Vector3 aim = lata.transform.position + Vector3.up * AiTuning.AimHeight;

            // ⚠️ THE SCATTER SHRINKS AS THE SHOT IS HELD, and that is what `AimSettle` buys. A
            // bot whose error is constant reads as a dice roll; one whose error closes over the
            // wind-up reads as somebody lining a shot up.
            float settle = 1.0f;

            if (Me.AimSettle < 90.0f)
                settle = Mathf.Lerp(1.0f, AiTuning.AimSettleFloor,
                                    Mathf.Clamp01(_windupTime / Mathf.Max(Me.AimSettle, 0.05f)));

            intent.AimPoint = aim + _windupScatter * settle;

            float power = _carrier.ChargeRatio;
            Press(intent, Verb.SpecialAbility, true);

            if (_windupTime >= AiTuning.WindupTimeout)
            {
                // Out of patience. Throw what we have: it may fall short, and a bot that lets
                // go is still a bot playing the game.
                ReleaseThrow(intent);
                return;
            }

            if (power < _windupPower) return;

            float minHold = Me.AimSettle < 90.0f
                ? Mathf.Min(Me.AimSettle, Balance.ChargeFullTime) * AiTuning.WindupMinHoldShare
                : 0.0f;

            if (_windupTime < minHold) return;

            // Charged and committed. The only question left is whether the lane is open.
            Vector3 origin = _carrier.ThrowOrigin();

            if (!_blundering && LaneBlocked(origin, intent.AimPoint, power))
            {
                _windupWait += dt;
                if (_windupWait < Me.LanePatience) return;

                // Waited long enough and still blocked: give up the ANGLE rather than the
                // round. Dropping the plan sends this bot to a new spot on the ring, which is
                // what a player does when somebody stands in front of them.
                _windup = false;
                _goalValid = false;
                Plan = AiPlan.Position;
                Press(intent, Verb.SpecialAbility, false);
                return;
            }

            ReleaseThrow(intent);
        }

        private void ReleaseThrow(InputIntent intent)
        {
            Press(intent, Verb.SpecialAbility, false);   // the release IS the throw
            intent.SpinInput = 0.0f;
            _windup = false;
            _goalValid = false;
            _commitLeft = 0.0f;
            Plan = AiPlan.Idle;
        }

        private void DoEvade(InputIntent intent)
        {
            var round = GameServices.Round;
            var taya = round != null ? DefenderOf(round) : null;

            if (taya == null) { DoWithdraw(intent); return; }

            // ⚠️ BREAK PERPENDICULAR TO THE LUNGE, NOT AWAY FROM IT. A 2.5 m dash beats a
            // 3.45 m/s attacker running in a straight line down the same axis; stepping across
            // it is the only answer the geometry allows.
            Vector3 toward = transform.position - At(taya);
            toward.y = 0.0f;
            if (toward.magnitude < 0.05f) toward = Vector3.forward;

            Vector3 across = new Vector3(-toward.z, 0.0f, toward.x).normalized;
            if (Vector3.Dot(across, OutOfBoxDir()) < 0.0f) across = -across;

            Drive(intent, (across * 0.75f + OutOfBoxDir() * 0.75f).normalized, true);
            Press(intent, Verb.Grab, false);
        }

        private void DoSabotage(InputIntent intent)
        {
            var round = GameServices.Round;
            var victim = SabotageTarget(round != null ? DefenderOf(round) : null);

            if (victim == null) { Stop(intent); return; }

            // ⚠️ IT DRIVES ALL THE WAY IN AND NEVER PARKS, for the same reason the hunt does:
            // the body only turns on a frame it walks, and the shove tests a 70 degree arc off
            // the facing. Arriving and stopping freezes the facing at whatever the approach
            // happened to end on, and the shove then fires into the wrong quadrant.
            float distance = Flat(transform.position, victim.transform.position);
            Vector3 toward = victim.transform.position - transform.position;
            toward.y = 0.0f;

            Drive(intent, toward, distance > 3.0f);

            if (distance <= Balance.ShoveRange * 0.9f
                && Facing(victim, Balance.ShoveArcDeg * 0.6f))
                Tap(intent, Verb.Grab);
            else
                Press(intent, Verb.Grab, false);
        }

        private void DoIdle(InputIntent intent) => Loiter(intent);

        // ---- DEFENDER VERBS -------------------------------------------------

        private void DoReset(InputIntent intent)
        {
            var lata = GameServices.Round?.Lata;

            if (lata == null) { Stop(intent); return; }

            bool inside = Flat(transform.position, lata.transform.position)
                          <= Balance.InteractionRadius;

            if (inside) Stop(intent);
            else Goto(intent, lata.transform.position, Balance.InteractionRadius * 0.55f, true);

            // ⚠️ HELD, NOT TAPPED, AND THIS IS THE ONE PLACE THAT IS TRUE. The reset channel
            // reads `Pressed` and zeroes itself the instant it goes false, so an alternating
            // tap would restart the channel every other frame and never finish it.
            Press(intent, Verb.Grab, inside);
        }

        private void DoIntercept(InputIntent intent)
        {
            if (!TryInterceptPoint(out Vector3 point)) { DoGuard(intent); return; }

            Goto(intent, point, 0.3f, true);
        }

        private void DoHunt(InputIntent intent, float dt)
        {
            var victim = TagTarget();

            if (victim == null) { DoGuard(intent); return; }

            // Close on where they are GOING. `Lead` is the tier's willingness to do that and is
            // 0 on the kid, which is why the kid chases a shadow.
            Vector3 toward = AheadOf(victim, 0.35f) - transform.position;
            toward.y = 0.0f;

            // ⚠️⚠️ IT NEVER STOPS CLOSING, AND THAT IS FORCED BY THE GAME RATHER THAN CHOSEN.
            // The body only turns on a frame it actually MOVES, and the lunge fires along the
            // facing, so a taya that parks next to its target can never aim the dash at it.
            // Measured in the original with an arrival stop here: one seat stood still for
            // 42.9 s of a 90 s round, adjacent to a vulnerable attacker, firing lunges into
            // whatever direction it had last walked in.
            Drive(intent, toward, MaySprint() && toward.magnitude > 1.5f);
            StepLungeIntent(intent, victim, dt);
        }

        private void DoCover(InputIntent intent)
        {
            if (!TryCoverPoint(out Vector3 point)) { DoGuard(intent); return; }

            Goto(intent, point, AiTuning.ArriveSlop, false);
            if (_arrived) Loiter(intent);
        }

        private void DoGuard(InputIntent intent)
        {
            var lata = GameServices.Round?.Lata;

            if (lata == null) { Stop(intent); return; }

            var threat = LiveThreat();

            if (threat == null)
            {
                // Stand outside the can-camping penalty ring (at least 2.7m away)
                Vector3 safeGuard = lata.transform.position + Vector3.forward * (Balance.TayaCampRadius + 0.6f);
                Goto(intent, ClampToBox(safeGuard), AiTuning.ArriveSlop, false);
                return;
            }

            // Stand BETWEEN the lata and the threat, dynamically outside the camping penalty ring!
            Vector3 toward = At(threat) - lata.transform.position;
            toward.y = 0.0f;

            if (toward.magnitude < 0.05f) toward = Vector3.forward;

            float guardRadius = Mathf.Max(AiTuning.GuardRadius, Balance.TayaCampRadius + 0.6f);
            Vector3 post = lata.transform.position + toward.normalized * guardRadius;

            Goto(intent, ClampToBox(post), AiTuning.ArriveSlop,
                 Flat(transform.position, post) > AiTuning.SprintDistance);

            // Keep moving and patrolling actively!
            if (_arrived) Loiter(intent);
        }

        /// <summary>
        /// The taya's two tag verbs, in the order a person would reach for them.
        ///
        /// ⚠️⚠️ THE PUNCH COMES FIRST WHEN IT IS IN RANGE, and a bot that only knew the lunge
        /// would charge half a second at a target standing next to it — which is exactly the
        /// case the punch was added for, and exactly long enough for the attacker to leave.
        ///
        /// ⚠️ THE LUNGE CHARGES BY HOLDING AND FIRES BY RELEASING, the same contract a human's
        /// right-click has. Holding it forever charges and never lunges.
        ///
        /// ⚠️ AND BOTH ARE AIMED. Both verbs fire along the facing and the body only turns on a
        /// frame it walks, so a taya that releases while side-stepping dashes past at 12 m/s
        /// and puts its own tag on cooldown for 1.5 s.
        /// </summary>
        private void StepLungeIntent(InputIntent intent, CharacterMotor victim, float dt)
        {
            var verbs = GetComponent<CombatVerbs>();
            if (verbs == null) return;

            if (verbs.PunchCooldownLeft <= 0.0f && victim != null
                && Flat(transform.position, victim.transform.position) <= Balance.PunchRange
                && Facing(victim, Balance.PunchArcDeg))
            {
                // ⚠️ A TAP, NOT A HOLD. The punch reads `JustPressed`, so it needs a false
                // frame before the true one.
                Tap(intent, Verb.SpecialAbility);
                return;
            }

            Press(intent, Verb.SpecialAbility, false);

            if (verbs.LungeCooldownLeft > 0.0f)
            {
                _lungeHeld = -1.0f;
                Press(intent, Verb.Lunge, false);
                return;
            }

            float reach = Flat(transform.position, AheadOf(victim, AiTuning.LungeHoldTime));

            if (_lungeHeld < 0.0f)
            {
                // ⚠️ THERE IS NO LOWER BOUND HERE, AND THE ONE THAT USED TO BE WAS A DEADLOCK.
                // It refused to start the charge inside 0.9 m on the reasoning that walking
                // would tag them anyway, but the tag has not been passive since the punch
                // landed, so a taya 0.78 m from a vulnerable attacker had no verb at all.
                if (reach > Me.LungeRange) { Press(intent, Verb.Lunge, false); return; }

                _lungeHeld = 0.0f;
            }

            _lungeHeld += dt;

            // ⚠️ THE CONE IS FLOORED. An eight-way heading cannot aim finer than
            // `LungeConeFloor`, so a tighter tier value would ask for an angle the bot has no
            // key for and the release would never pass its own test.
            if (_lungeHeld >= AiTuning.LungeHoldTime
                && Facing(victim, AiTuning.EffectiveLungeCone(ActiveDifficulty)))
            {
                _lungeHeld = -1.0f;
                Press(intent, Verb.Lunge, false);   // the release edge is what fires it
                return;
            }

            if (_lungeHeld >= AiTuning.LungeHoldTime + 0.45f)
            {
                // Fully charged and still not lined up. Let it go rather than hold a dash for
                // ever: the cooldown is 1.5 s and the attacker is leaving.
                _lungeHeld = -1.0f;
                Press(intent, Verb.Lunge, false);
                return;
            }

            Press(intent, Verb.Lunge, true);
        }

        // ---- THE THROW SOLVE ------------------------------------------------
        //
        // Everything here is arithmetic on the flight model the slipper actually uses,
        // deliberately, so the AI cannot be right about a flight the game then flies
        // differently.

        /// <summary>
        /// The smallest power whose launch speed has ANY solution from origin to target.
        ///
        /// The arc discriminant is v⁴ - g(g·d² + 2·Δy·v²) &gt;= 0; solving for u = v² gives
        /// u &gt;= g·(Δy + sqrt(Δy² + d²)).
        ///
        /// ⚠️ AT EXACTLY THAT SPEED THE SOLUTION IS THE GRAZING ONE: maximum range, maximum
        /// airtime, minimum speed — the easiest possible throw to body-block and the one most
        /// damaged by aim scatter. `PowerMargin` is what buys a flatter shot, and it is a tier
        /// knob for exactly that reason.
        /// </summary>
        private float MinPowerFor(Vector3 origin, Vector3 target)
        {
            float flat = new Vector2(target.x - origin.x, target.z - origin.z).magnitude;
            float rise = target.y - origin.y;

            float speed = Mathf.Sqrt(Mathf.Max(
                Balance.Gravity * (rise + Mathf.Sqrt(rise * rise + flat * flat)), 0.0f));

            return PowerForSpeed(speed);
        }

        /// <summary>Inverts the per-skin launch speed back to the 0..1 charge that produces
        /// it, so the solve above lands on a number the carrier can actually hold to.</summary>
        private float PowerForSpeed(float speed)
        {
            int skin = _carrier != null && _carrier.Held != null ? _carrier.Held.SkinIndex : -1;

            float full = ThrowRules.LaunchSpeedFor(skin, 1.0f);
            float min = ThrowRules.LaunchSpeedFor(skin, 0.0f);

            if (full - min < 0.01f) return 1.0f;

            return Mathf.Clamp01((speed - min) / (full - min));
        }

        /// <summary>The power this wind-up commits to, rolled once when it starts.</summary>
        private float PlanPower(Lata lata)
        {
            Vector3 aim = lata.transform.position + Vector3.up * AiTuning.AimHeight;
            Vector3 origin = _carrier != null ? _carrier.ThrowOrigin()
                                              : transform.position + Vector3.up * 0.9f;

            float floorPower = MinPowerFor(origin, aim);

            // The margin is applied in SPEED, not in power, because it is a statement about
            // the flight and power is only a dial onto it.
            float flat = new Vector2(aim.x - origin.x, aim.z - origin.z).magnitude;
            float rise = aim.y - origin.y;

            float wanted = Mathf.Sqrt(Mathf.Max(
                Balance.Gravity * (rise + Mathf.Sqrt(rise * rise + flat * flat)), 0.0f));

            // ⚠️ THE KID'S CHARACTERISTIC MISS IS A THROW THAT ONLY JUST GETS THERE, which
            // floats, and which the taya can walk into. A readable mistake rather than noise:
            // the point of `Mistake` is that a player can SEE the bot make one.
            float margin = _blundering ? 1.0f : Me.PowerMargin;

            return Mathf.Clamp01(Mathf.Max(PowerForSpeed(wanted * margin), floorPower + 0.02f));
        }

        /// <summary>
        /// Metres of scatter, rolled once per wind-up.
        ///
        /// ⚠️ PER SHOT, NOT PER FRAME AND NOT PER THINK TICK. Re-rolling inside a charge
        /// averages to a perfect shot over the length of it, which is the opposite of what an
        /// aim error is for.
        ///
        /// ⚠️ AND IT SCALES WITH RANGE. A fixed metre of scatter is a wide miss at 7 m and an
        /// impossible one at 12 m; quoting it at a reference range and scaling keeps the tier's
        /// ANGULAR error constant, which is what a person's actually is.
        /// </summary>
        private Vector3 RollScatter()
        {
            var lata = GameServices.Round?.Lata;
            float rangeScale = 1.0f;

            if (lata != null)
                rangeScale = Mathf.Clamp(
                    Flat(transform.position, lata.transform.position) / AiTuning.AimReferenceRange,
                    AiTuning.AimRangeScaleMin, AiTuning.AimRangeScaleMax);

            float spread = Me.AimError * rangeScale * (_blundering ? 2.2f : 1.0f);
            float bearing = UnityEngine.Random.Range(-Mathf.PI, Mathf.PI);
            float reach = Mathf.Sqrt(UnityEngine.Random.value) * spread;

            return new Vector3(Mathf.Cos(bearing) * reach, 0.0f, Mathf.Sin(bearing) * reach);
        }

        /// <summary>One coin flip per wind-up, at the tier's mistake rate.</summary>
        private bool Blunder() => UnityEngine.Random.value < Me.Mistake;

        private Slipper ChooseSlipper()
        {
            Slipper best = null;
            float bestScore = float.MaxValue;

            foreach (var s in FindObjectsByType<Slipper>(FindObjectsSortMode.None))
            {
                if (s.State != SlipperState.Loose) continue;

                float d = Vector3.Distance(transform.position, s.transform.position);

                // A human's own slipper is treated as further than it is.
                if (s.OwnerSlot >= 0 && IsHumanSlot(s.OwnerSlot)) d += HumanSlipperBias;

                if (!IsNearestClaimant(s, d)) continue;
                if (d >= bestScore) continue;

                bestScore = d;
                best = s;
            }

            return best;
        }

        private bool IsNearestClaimant(Slipper s, float myDistance)
        {
            var round = GameServices.Round;
            if (round == null) return true;

            foreach (var p in round.Players)
            {
                if (p == null || p == _motor || p.IsDefender) continue;
                if (p.GetComponent<AIController>() == null) continue; // only defer to other bots
                if (p.HoldingSlipper) continue;

                float theirs = Vector3.Distance(p.transform.position, s.transform.position);
                if (theirs + ClaimSlack < myDistance) return false;
            }

            return true;
        }

        private static bool IsHumanSlot(int slot)
        {
            var round = GameServices.Round;
            var who = round?.PlayerAt(slot);
            return who != null && who.GetComponent<AIController>() == null;
        }

        // -------------------------------------------------------------------

        private Vector3 ClampToPlayable(Vector3 goal)
        {
            float halfX = PlayableHalfX, halfZ = PlayableHalfZ;
            goal.x = Mathf.Clamp(goal.x, -halfX, halfX);
            goal.z = Mathf.Clamp(goal.z, -halfZ, halfZ);
            return goal;
        }

        /// <summary>
        /// The WALL FACES, measured off the map's Bounds colliders at load. These defaults are
        /// Eskinita's house facades.
        ///
        /// ⚠️ THIS IS THE WALL, NOT THE RING. The standoff ring sits at
        /// ConfinementRadius + ThrowStandoff = 8.2, and the wall is at 8.6; confusing the two
        /// makes the clamp reject the very positions it exists to permit. The limit to
        /// remember when growing the box is
        /// ConfinementRadius + ThrowStandoff + a capsule &lt;= wall face, and two of those
        /// three numbers live in files the radius does not.
        /// </summary>
        public static float PlayableHalfX = 8.6f;
        public static float PlayableHalfZ = 13.0f;

        private Vector3 RingPoint(float radius)
        {
            // The square ring, matching the confinement shape rather than a circle.
            Vector3 from = transform.position;
            float ax = Mathf.Abs(from.x), az = Mathf.Abs(from.z);

            Vector3 p = ax > az
                ? new Vector3(Mathf.Sign(from.x) * radius, 0.0f, Mathf.Clamp(from.z, -radius, radius))
                : new Vector3(Mathf.Clamp(from.x, -radius, radius), 0.0f, Mathf.Sign(from.z) * radius);

            p.y = from.y;
            return ClampToPlayable(p);
        }

        /// <summary>
        /// Walk to a point, stopping inside <paramref name="stopAt"/> and not resuming until
        /// well outside it. Returns true once arrived.
        ///
        /// ⚠️⚠️ THE HYSTERESIS IS WHY A BOT SETTLES INSTEAD OF SHUFFLING. The port stopped and
        /// started on the same radius, so a body pushed a centimetre past it by a neighbour, by
        /// a slope or by its own last step immediately walked back — which reads as twitching
        /// rather than as standing. `ArriveHysteresis` is 1.8, so leaving costs almost twice
        /// what arriving did.
        ///
        /// ⚠️ AND A GOAL THAT HAS MOVED FAR RESETS THE ARRIVAL. Without `GoalMoved` a bot that
        /// arrived at one point counts as arrived at the next one it is handed, so the plan
        /// after this one starts by believing it is already standing where it wants to be.
        /// </summary>
        private bool Goto(InputIntent intent, Vector3 point, float stopAt, bool sprint)
        {
            point = ClampToPlayable(point);

            if (Flat(_goal, point) > AiTuning.GoalMoved) _arrived = false;

            _goal = point;

            Vector3 delta = point - transform.position;
            delta.y = 0.0f;

            float distance = delta.magnitude;
            float threshold = _arrived ? stopAt * AiTuning.ArriveHysteresis : stopAt;

            if (distance <= threshold)
            {
                _arrived = true;
                Stop(intent);
                return true;
            }

            _arrived = false;

            Vector3 heading = delta / Mathf.Max(distance, 0.001f);

            // ⚠️⚠️ SEPARATION, AND ITS ABSENCE IS WHY BODIES ENDED UP PRESSED AGAINST THE
            // PLAYER'S LENS. `AiTuning` has carried both constants since the tuning table was
            // ported and nothing ever read them, so three attackers converging on one box
            // arrived as one clump — measured from a live round with `GameplayShots`, a
            // neighbouring head 0.87 m from the first-person camera covering 28% of the frame.
            // This is not collision, which the motor already does; it is the reason three
            // people read as three people.
            heading += Separation() * AiTuning.SeparationWeight;

            Drive(intent, heading, sprint && distance > AiTuning.Reach);
            return false;
        }

        /// <summary>
        /// Four digital presses in world space. See the class header on why a bot is given a
        /// keyboard rather than a bearing.
        ///
        /// ⚠️ `EightWayThreshold` IS sin(22.5°) AND NOT A ROUND NUMBER ON PURPOSE. It is the
        /// exact bisector between two adjacent keyboard headings, so a desired bearing always
        /// resolves to its NEAREST of the eight rather than to a band where both neighbours
        /// qualify and the bot presses three keys.
        /// </summary>
        private void Drive(InputIntent intent, Vector3 direction, bool sprint)
        {
            Vector3 flat = new Vector3(direction.x, 0.0f, direction.z);

            if (flat.magnitude < 0.001f) { Stop(intent); return; }

            flat = flat.normalized;
            _driving = true;

            // ⚠️ NINETY DEGREES OFF THE WANTED HEADING WHILE UNSTICKING. Enough to clear a
            // corner, and it still makes progress ALONG the obstacle rather than backing away
            // from it — backing off just walks into the same corner again a second later.
            if (_unstickLeft > 0.0f)
                flat = new Vector3(-flat.z * _unstickSign, 0.0f, flat.x * _unstickSign);

            intent.Move = EightWay(flat);
            intent.Set(Verb.Sprint, sprint && MaySprint());
        }

        private void Stop(InputIntent intent)
        {
            intent.Move = Vector2.zero;
            intent.Set(Verb.Sprint, false);
        }

        /// <summary>
        /// ⚠️ THE RESERVE IS THE POINT AND IT IS A DIFFICULTY KNOB. The bar is 1.25 seconds of
        /// sprint and fatigue costs two seconds at three-quarter speed with regen locked. A bot
        /// that sprints whenever it is far away arrives fatigued and is then tagged standing
        /// still, which is precisely what "the AI gives up" looks like from the stands. Bata's
        /// 0 reserve is that mistake, kept on purpose.
        /// </summary>
        private bool MaySprint()
        {
            var stamina = _motor.Stamina;
            if (stamina == null) return true;

            return !stamina.IsFatigued && stamina.Ratio > Me.SprintReserve;
        }

        /// <summary>
        /// Steers away from bodies that are too close. Not collision — the motor does that —
        /// but the reason three attackers converging on one box read as three people rather
        /// than as one clump.
        /// </summary>
        private Vector3 Separation()
        {
            var round = GameServices.Round;
            if (round == null) return Vector3.zero;

            Vector3 push = Vector3.zero;
            Vector3 here = transform.position;

            foreach (var who in round.Players)
            {
                if (who == null || who == _motor) continue;

                Vector3 away = here - who.transform.position;
                away.y = 0.0f;

                float distance = away.magnitude;
                if (distance > AiTuning.SeparationRadius || distance < 0.01f) continue;

                push += (away / distance) * (1.0f - distance / AiTuning.SeparationRadius);
            }

            return push;
        }

        /// <summary>
        /// A bot with nothing to do shifts its weight instead of standing at attention.
        ///
        /// ⚠️⚠️ LEASHED TO WHERE IT IS STANDING, AND THAT LEASH IS THE BUG FIX. Without it the
        /// drift walks the body out of arrival range, the next `Goto` walks it back, and the
        /// two pace against each other for as long as the plan holds. Nothing here may take the
        /// body further than `LoiterLeash` from its anchor, so arrival cannot flip.
        ///
        /// ⚠️ THE BEATS ARE ROLLED, NOT PHASED. A sine gives every bot the same rhythm at a
        /// different offset, which still reads as clockwork once you watch two of them. Each
        /// beat draws its own length and its own direction, so a bot can step twice the same
        /// way or stand for three seconds, and four of them never fall into step.
        /// </summary>
        private void Loiter(InputIntent intent)
        {
            Vector3 here = transform.position;
            Vector3 anchor = Flat(here, _goal) <= AiTuning.ArriveSlop ? _goal : here;

            Vector3 outward = here - anchor;
            outward.y = 0.0f;

            if (outward.magnitude > AiTuning.LoiterLeash)
            {
                // Past the leash. Come back and stand a while — deliberately NOT "step the
                // other way", which is the alternation that read as pacing to begin with.
                _loiterDir = 0.0f;
                _loiterLeft = UnityEngine.Random.Range(AiTuning.LoiterRestMin,
                                                       AiTuning.LoiterRestMax);
                Drive(intent, -outward, false);
                return;
            }

            _loiterLeft -= Time.deltaTime;

            if (_loiterLeft <= 0.0f)
            {
                if (Mathf.Approximately(_loiterDir, 0.0f))
                {
                    _loiterDir = UnityEngine.Random.value < 0.5f ? 1.0f : -1.0f;
                    _loiterLeft = UnityEngine.Random.Range(AiTuning.LoiterStepMin,
                                                           AiTuning.LoiterStepMax);
                }
                else
                {
                    _loiterDir = 0.0f;
                    _loiterLeft = UnityEngine.Random.Range(0.25f, 0.65f);

                    // Occasional friendly emote during calm loitering
                    if (UnityEngine.Random.value < 0.15f)
                    {
                        string[] emotes = { "yes", "dance", "tpose", "bow", "crouch" };
                        TryTriggerEmote(emotes[UnityEngine.Random.Range(0, emotes.Length)], 0.9f);
                    }
                }
            }

            if (Mathf.Approximately(_loiterDir, 0.0f)) { Stop(intent); return; }

            var lata = GameServices.Round?.Lata;
            Vector3 pivot = lata != null ? lata.transform.position : Vector3.zero;

            Vector3 radial = here - pivot;
            radial.y = 0.0f;
            if (radial.magnitude < 0.05f) radial = Vector3.forward;
            radial = radial.normalized;

            // Across the bearing out from the lata, so the shift never walks into or away from
            // the thing this bot is lined up on.
            Drive(intent, new Vector3(-radial.z, 0.0f, radial.x) * _loiterDir, false);
        }

        // ---- THE BOARD ------------------------------------------------------

        /// <summary>
        /// Where to throw from, scored over sixteen bearings.
        ///
        /// ⚠️ NOBODY IS COOPERATING. Each bot picks the bearing that is best FOR IT, and "not
        /// where my rivals already are" is part of that for the same reason it is for a human:
        /// two attackers on the same bearing share one taya, one blocking body and one blocked
        /// lane. The claim board is a way to READ the court, not an agreement.
        /// </summary>
        private Vector3 ThrowSpot()
        {
            var round = GameServices.Round;
            var lata = round?.Lata;

            if (lata == null) return SafeSpot();

            float ring = Balance.ConfinementRadius + AiTuning.ThrowStandoff;
            Vector3 here = transform.position;

            var taya = DefenderOf(round);
            float tayaBearing = 0.0f;

            if (taya != null)
            {
                Vector3 offset = At(taya) - lata.transform.position;
                tayaBearing = Mathf.Atan2(offset.x, offset.z);
            }

            var rivals = RivalBearings();

            Vector3 best = SafeSpot();
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < SpotSamples; i++)
            {
                float bearing = -Mathf.PI + 2.0f * Mathf.PI * i / SpotSamples;
                Vector3 point = RingPoint(bearing, ring);

                float score = 0.0f;

                // Away from the taya. Half a turn is the ideal and is worth the most.
                if (taya != null)
                    score += 2.4f * (Mathf.Abs(AngleBetween(bearing, tayaBearing)) / Mathf.PI);

                // Away from my rivals, weighted by the tier's spacing.
                float nearestRival = Mathf.PI;
                foreach (float claimed in rivals)
                    nearestRival = Mathf.Min(nearestRival,
                                             Mathf.Abs(AngleBetween(bearing, claimed)));

                score += 2.0f * Me.Spacing * (nearestRival / Mathf.PI);

                // My own corner of the court, so the four of them do not all drift to the same
                // side of the map over a round.
                score += 0.5f * (1.0f - Mathf.Abs(AngleBetween(bearing, _self.HomeBearing))
                                        / Mathf.PI);

                // And it has to be worth walking to.
                score -= 0.11f * Flat(here, point);

                if (score <= bestScore) continue;

                bestScore = score;
                best = point;
            }

            return best;
        }

        private const int SpotSamples = 16;

        /// <summary>Bearings other bots have staked out recently, as a list. Entries older than
        /// <see cref="AiTuning.ClaimTtl"/> are ignored rather than removed, so a bot that dies
        /// mid-round cannot hold a bearing for ever.</summary>
        private List<float> RivalBearings()
        {
            var found = new List<float>();
            float now = Time.time;

            foreach (var pair in _claims)
            {
                if (pair.Key == _motor.PlayerSlot) continue;
                if (now - pair.Value.At > AiTuning.ClaimTtl) continue;

                found.Add(pair.Value.Bearing);
            }

            return found;
        }

        private void Claim(float bearing)
            => _claims[_motor.PlayerSlot] = new BearingClaim(bearing, Time.time);

        private readonly struct BearingClaim
        {
            public readonly float Bearing;
            public readonly float At;

            public BearingClaim(float bearing, float at) { Bearing = bearing; At = at; }
        }

        /// <summary>⚠️ SHARED ACROSS EVERY BOT ON PURPOSE, exactly as the .gd's `static var`
        /// is. It is a board, not a negotiation: each bot writes its own row and reads the
        /// others, and nothing waits for anybody.</summary>
        private static readonly Dictionary<int, BearingClaim> _claims =
            new Dictionary<int, BearingClaim>();

        // ---- GEOMETRY -------------------------------------------------------

        /// <summary>
        /// The nearest point outside the box, straight out along the bearing already held.
        ///
        /// ⚠️⚠️ IT PROJECTS ONTO THE SQUARE, NOT ONTO A CIRCLE, AND THAT IS WHY BOTS USED TO
        /// FREEZE IN THE ORIGINAL. The box is a SQUARE: X and Z clamp independently and the
        /// throw gate asks max(|x|,|z|). Normalising a bearing and multiplying lands on a
        /// CIRCLE, and a circle of radius r is INSIDE a square of half-width r everywhere but
        /// the four edge midpoints — so on a diagonal the bot walked to its "safe spot", was
        /// still inside the box, was refused the throw, and walked to the same spot again for
        /// the rest of the round. Scaling by the CHEBYSHEV distance puts the point exactly on
        /// the square ring for every bearing, by construction.
        /// </summary>
        private Vector3 SafeSpot()
        {
            Vector3 here = transform.position;
            var flat = new Vector2(here.x, here.z);

            float reach = Mathf.Max(Mathf.Abs(flat.x), Mathf.Abs(flat.y));

            if (reach < 0.01f) { flat = new Vector2(0.0f, 1.0f); reach = 1.0f; }

            float ring = Balance.ConfinementRadius + AiTuning.ThrowStandoff;
            flat *= ring / reach;

            return ClampToPlayable(new Vector3(flat.x, 0.0f, flat.y));
        }

        /// <summary>A point on the square ring at the given Chebyshev radius and bearing. Same
        /// projection as <see cref="SafeSpot"/>, for a bearing this bot chose rather than the
        /// one it happens to be standing on.</summary>
        private Vector3 RingPoint(float bearing, float ring)
        {
            var direction = new Vector2(Mathf.Sin(bearing), Mathf.Cos(bearing));
            float reach = Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y));

            if (reach < 0.001f) return new Vector3(0.0f, 0.0f, ring);

            direction *= ring / reach;
            return ClampToPlayable(new Vector3(direction.x, 0.0f, direction.y));
        }

        /// <summary>The shortest way out of the box from here, as a unit heading.</summary>
        private Vector3 OutOfBoxDir()
        {
            Vector3 here = transform.position;

            if (Mathf.Abs(here.x) >= Mathf.Abs(here.z))
                return new Vector3(Mathf.Abs(here.x) > 0.01f ? Mathf.Sign(here.x) : 1.0f, 0, 0);

            return new Vector3(0, 0, Mathf.Abs(here.z) > 0.01f ? Mathf.Sign(here.z) : 1.0f);
        }

        /// <summary>Pushes a point a margin outside the box along its own bearing. Used to
        /// stand NEAR a landing spot without standing inside the danger zone waiting for
        /// it.</summary>
        private static Vector3 PullOutside(Vector3 point, float margin)
        {
            float reach = Mathf.Max(Mathf.Abs(point.x), Mathf.Abs(point.z));
            float ring = Balance.ConfinementRadius + margin;

            if (reach >= ring || reach < 0.01f) return point;

            var flat = new Vector2(point.x, point.z) * (ring / reach);
            return new Vector3(flat.x, 0.0f, flat.y);
        }

        /// <summary>Keeps a taya's goal inside its own box, so it walks somewhere it can stand
        /// rather than pressing itself against the confinement clamp — which looks exactly like
        /// a bot stuck on a wall, because it is one.</summary>
        private static Vector3 ClampToBox(Vector3 point)
        {
            float edge = Balance.ConfinementRadius - 0.35f;
            return new Vector3(Mathf.Clamp(point.x, -edge, edge), point.y,
                               Mathf.Clamp(point.z, -edge, edge));
        }

        /// <summary>Where a slipper already in flight will come down.</summary>
        private static bool TryPredictedLanding(Slipper slipper, out Vector3 landing)
        {
            landing = Vector3.zero;

            Vector3 launch = slipper.Velocity;
            if (launch.magnitude < 0.5f) return false;

            Vector3 from = slipper.transform.position;

            for (float t = 0.0f; t < Balance.MaxFlightTime; t += 0.05f)
            {
                Vector3 point = from + launch * t
                                + Vector3.down * (0.5f * Balance.Gravity * t * t);

                if (point.y > from.y - 1.2f && point.y > 0.2f) continue;

                landing = new Vector3(point.x, 0.0f, point.z);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Is this unit inside <paramref name="cone"/> degrees of the way the body faces? The
        /// lunge, the punch and the shove all fire along the facing, so this is the difference
        /// between a dash that tags and one that misses by a metre.
        /// </summary>
        private bool Facing(CharacterMotor who, float cone)
        {
            if (who == null) return false;

            Vector3 forward = transform.forward;
            forward.y = 0.0f;

            Vector3 toward = who.transform.position - transform.position;
            toward.y = 0.0f;

            if (forward.magnitude < 0.01f || toward.magnitude < 0.01f) return false;

            return Vector3.Angle(forward.normalized, toward.normalized) <= cone;
        }

        /// <summary>Signed shortest angle from b to a, in radians.</summary>
        private static float AngleBetween(float a, float b)
        {
            float d = Mathf.Repeat(a - b + Mathf.PI, 2.0f * Mathf.PI) - Mathf.PI;
            return d;
        }

        // ---- READING THE BOARD ----------------------------------------------

        /// <summary>
        /// The attacker this taya should be standing in front of.
        ///
        /// ⚠️⚠️ THE CHARGE BONUS IS SMALL ON PURPOSE, AND A BIG ONE SINGLED OUT THE HUMAN.
        /// Nothing here reads whether a player is human: the bias was entirely about TIME. A
        /// person aims for most of the 2.5 s charge and a bot releases the moment it has
        /// enough power, so a large "is charging" bonus was a bonus only one of the three
        /// attackers ever held, and the taya spent whole rounds standing in front of them.
        /// Reported from a playtest as *"the defender ai only attack him"*. At this weight it
        /// is what it was meant to be: a tiebreak that says "this one is about to throw",
        /// which distance and possession can still outweigh.
        ///
        /// ⚠️ AND THE ANTI-FIXATION TERM IS THE OTHER HALF. Whoever was guarded last tick is
        /// worth slightly less than an equal rival, so a genuine tie rotates instead of
        /// sticking. Small, so it never pulls the taya off somebody who really is the threat.
        /// </summary>
        private CharacterMotor LiveThreat()
        {
            var round = GameServices.Round;
            if (round == null) return null;

            CharacterMotor best = null;
            float bestScore = float.NegativeInfinity;

            foreach (var who in round.Players)
            {
                if (who == null || who.IsDefender) continue;

                float score = 0.0f;

                if (who.HoldingSlipper) score += 2.0f;
                if (!Confinement.IsInsideBox(who.transform.position.x, who.transform.position.z))
                    score += 1.0f;

                var carrier = who.GetComponent<Carrier>();
                if (carrier != null && carrier.ObservedChargePower >= 0.0f)
                    score += 1.0f + carrier.ObservedChargePower;

                score -= 0.08f * Flat(transform.position, At(who));

                if (who == _lastThreat) score -= 0.6f;

                if (score <= bestScore) continue;

                bestScore = score;
                best = who;
            }

            _lastThreat = best;
            return best;
        }

        /// <summary>
        /// Where a taya waits for a retrieval: between a loose slipper in its box and the
        /// attacker who has to come and get it.
        ///
        /// ⚠️ WHOEVER IS ACTUALLY COMING, NOT WHOEVER OWNS IT. Any attacker may pick up any
        /// slipper, so camping the OWNER's bearing puts the taya on an approach nobody is
        /// using — and it skips a spare slipper with no owner entirely, which is every spare
        /// slipper in a short-handed match.
        /// </summary>
        private bool TryCoverPoint(out Vector3 point)
        {
            point = Vector3.zero;

            var round = GameServices.Round;
            var lata = round?.Lata;
            if (lata == null) return false;

            bool found = false;
            float bestDistance = float.MaxValue;

            foreach (var s in FindObjectsByType<Slipper>(FindObjectsInactive.Exclude))
            {
                if (s.State != SlipperState.Loose) continue;

                Vector3 at = s.transform.position;

                // Outside the box: not the taya's problem, and not reachable.
                if (Mathf.Max(Mathf.Abs(at.x), Mathf.Abs(at.z)) >= Balance.ConfinementRadius)
                    continue;

                var holder = NearestClaimantTo(s);
                if (holder == null) continue;

                Vector3 toward = At(holder) - at;
                toward.y = 0.0f;
                if (toward.magnitude < 0.05f) continue;

                // Sit on the approach line, one body-length out from the slipper, so the
                // retrieval has to come through the taya rather than around it. `Camp` decides
                // how far up the line that is: 0 leaves it standing on the can.
                Vector3 candidate = at + toward.normalized * (0.6f + 0.9f * Me.Camp);
                float distance = Flat(transform.position, candidate);

                if (distance >= bestDistance) continue;

                bestDistance = distance;
                point = ClampToBox(candidate);
                found = true;
            }

            return found;
        }

        /// <summary>The attacker most likely to come for a slipper: the nearest one with free
        /// hands. Mirrors the attackers' own claim rule, so the taya camps the line the bot
        /// that is actually coming will walk up.</summary>
        private static CharacterMotor NearestClaimantTo(Slipper slipper)
        {
            var round = GameServices.Round;
            if (round == null) return null;

            CharacterMotor best = null;
            float bestDistance = float.MaxValue;

            foreach (var who in round.Players)
            {
                if (who == null || who.IsDefender || !who.CanAct()) continue;
                if (who.HoldingSlipper) continue;

                float d = Vector3.Distance(who.transform.position, slipper.transform.position);
                if (d >= bestDistance) continue;

                bestDistance = d;
                best = who;
            }

            return best;
        }

        /// <summary>
        /// The slipper in the air that will land nearest this bot, whoever threw it.
        ///
        /// ⚠️ DELIBERATELY IGNORES OWNERSHIP AND CLAIMS. This is only reached when NOTHING is
        /// fetchable, so there is no claim to respect and no rival being cut off, and the
        /// alternative is standing still.
        /// </summary>
        private Slipper NearestFlyingSlipper()
        {
            Slipper best = null;
            float bestDistance = float.MaxValue;

            foreach (var s in FindObjectsByType<Slipper>(FindObjectsInactive.Exclude))
            {
                if (s.State != SlipperState.InFlight) continue;

                float d = Vector3.Distance(transform.position, s.transform.position);
                if (d >= bestDistance) continue;

                bestDistance = d;
                best = s;
            }

            return best;
        }

        // ---- PERCEPTION -----------------------------------------------------

        /// <summary>
        /// What this bot BELIEVES about where everybody is, lagged by its own reaction time.
        ///
        /// ⚠️ THE BOT'S OWN BODY IS NEVER LAGGED. Proprioception is not perception: a player
        /// always knows exactly where their own feet are, and a bot steering off a lagged copy
        /// of ITSELF oscillates around every goal it is given.
        /// </summary>
        private void Observe(float dt)
        {
            var round = GameServices.Round;
            if (round == null) return;

            float alpha = 1.0f - Mathf.Exp(-dt / Mathf.Max(Me.React, 0.02f));

            foreach (var who in round.Players)
            {
                if (who == null) continue;

                int slot = who.PlayerSlot;
                Vector3 truth = who.transform.position;
                Vector3 velocity = new Vector3(who.Velocity.x, 0.0f, who.Velocity.z);

                if (who == _motor || !_seenPos.ContainsKey(slot))
                {
                    _seenPos[slot] = truth;
                    _seenVel[slot] = velocity;
                    continue;
                }

                _seenPos[slot] = Vector3.Lerp(_seenPos[slot], truth, alpha);
                _seenVel[slot] = Vector3.Lerp(_seenVel[slot], velocity, alpha);
            }
        }

        /// <summary>Where this bot believes a unit is.</summary>
        private Vector3 At(CharacterMotor who)
        {
            if (who == null) return Vector3.zero;

            return _seenPos.TryGetValue(who.PlayerSlot, out Vector3 p)
                ? p : who.transform.position;
        }

        /// <summary>Where this bot believes a unit will be, at its tier's willingness to
        /// extrapolate. A `Lead` of 0 is a bot that runs at your shadow.</summary>
        private Vector3 AheadOf(CharacterMotor who, float horizon)
        {
            if (who == null) return Vector3.zero;

            Vector3 velocity = _seenVel.TryGetValue(who.PlayerSlot, out Vector3 v)
                ? v : Vector3.zero;

            return At(who) + velocity * horizon * Me.Lead;
        }

        private readonly Dictionary<int, Vector3> _seenPos = new Dictionary<int, Vector3>();
        private readonly Dictionary<int, Vector3> _seenVel = new Dictionary<int, Vector3>();

        // ---- THE BUTTONS ----------------------------------------------------

        /// <summary>Verbs written during the current <see cref="Act"/> frame, so the release
        /// sweep can tell "the plan chose to hold this" from "the plan forgot about it". An
        /// explicit release counts as a touch: writing false over false is the same state.
        /// </summary>
        private readonly HashSet<Verb> _touched = new HashSet<Verb>();

        private readonly HashSet<Verb> _pressed = new HashSet<Verb>();

        private void Press(InputIntent intent, Verb verb, bool pressed)
        {
            intent.Set(verb, pressed);

            if (pressed) _pressed.Add(verb);
            else _pressed.Remove(verb);

            _touched.Add(verb);
        }

        /// <summary>Produces a real press EDGE by alternating. The pickup, the shove and the
        /// punch all read `JustPressed`, which needs a false frame before every true one — a
        /// button simply held down fires once in a lifetime, which is how a bot ends up
        /// standing on its own slipper for ninety seconds.</summary>
        private void Tap(InputIntent intent, Verb verb)
            => Press(intent, verb, !_pressed.Contains(verb));

        private void StepHeroAbilities(InputIntent intent, float dt)
        {
            if (SceneFlow.SelectedMode != GameMode.HeroStrike) return;

            var abilitySystem = _motor.AbilitySystem;
            if (abilitySystem == null || abilitySystem.Kit == null) return;

            var kit = abilitySystem.Kit;
            var round = GameServices.Round;
            if (round == null || !round.RoundActive) return;

            Vector3 myPos = transform.position;

            // 1. Ultimate Decision
            if (kit.IsUltimateReady)
            {
                if (kit is Abilities.DanteHeroKit)
                {
                    foreach (var p in round.Players)
                    {
                        if (p != null && p.PlayerSlot != _motor.PlayerSlot && Vector3.Distance(myPos, p.transform.position) <= 9.0f)
                        {
                            Tap(intent, Verb.Ultimate);
                            break;
                        }
                    }
                }
                else if (kit is Abilities.CheskaHeroKit)
                {
                    foreach (var p in round.Players)
                    {
                        if (p != null && p.PlayerSlot != _motor.PlayerSlot && Vector3.Distance(myPos, p.transform.position) <= 7.0f)
                        {
                            Tap(intent, Verb.Ultimate);
                            break;
                        }
                    }
                }
                else if (kit is Abilities.SeanHeroKit)
                {
                    if (round.Lata != null && Vector3.Distance(myPos, round.Lata.transform.position) <= 6.0f)
                    {
                        Tap(intent, Verb.Ultimate);
                    }
                    else
                    {
                        foreach (var p in round.Players)
                        {
                            if (p != null && p.PlayerSlot != _motor.PlayerSlot && Vector3.Distance(myPos, p.transform.position) <= 7.5f)
                            {
                                Tap(intent, Verb.Ultimate);
                                break;
                            }
                        }
                    }
                }
                else if (kit is Abilities.ZackHeroKit)
                {
                    foreach (var p in round.Players)
                    {
                        if (p != null && p.PlayerSlot != _motor.PlayerSlot && Vector3.Distance(myPos, p.transform.position) <= 8.0f)
                        {
                            Tap(intent, Verb.Ultimate);
                            break;
                        }
                    }
                }
                else if (kit is Abilities.NemuHeroKit)
                {
                    Tap(intent, Verb.Ultimate);
                }
            }

            // 2. Skill 1 Decision
            if (kit.Skill1 != null && kit.Skill1.IsReady)
            {
                if (kit is Abilities.DanteHeroKit)
                {
                    foreach (var p in round.Players)
                    {
                        if (p != null && p.PlayerSlot != _motor.PlayerSlot && Vector3.Distance(myPos, p.transform.position) <= 5.0f)
                        {
                            Tap(intent, Verb.Skill1);
                            break;
                        }
                    }
                }
                else if (kit is Abilities.CheskaHeroKit)
                {
                    if (_driving) Tap(intent, Verb.Skill1);
                }
                else if (kit is Abilities.SeanHeroKit)
                {
                    if (_driving && (_motor.HoldingSlipper || Plan == AiPlan.Retrieve || Plan == AiPlan.Withdraw))
                    {
                        Tap(intent, Verb.Skill1);
                    }
                }
                else if (kit is Abilities.ZackHeroKit)
                {
                    if (_driving) Tap(intent, Verb.Skill1);
                }
                else if (kit is Abilities.NemuHeroKit)
                {
                    if (_motor.IsTaggable() || _motor.IsDefender)
                    {
                        Tap(intent, Verb.Skill1);
                    }
                }
            }

            // 3. Skill 2 Decision
            if (kit.Skill2 != null && kit.Skill2.IsReady)
            {
                if (kit is Abilities.DanteHeroKit)
                {
                    if (_motor.IsTaggable() || _motor.IsDefender)
                    {
                        Tap(intent, Verb.Skill2);
                    }
                }
                else if (kit is Abilities.CheskaHeroKit)
                {
                    if (_motor.IsDefender && round.Lata != null && Vector3.Distance(myPos, round.Lata.transform.position) <= 4.0f)
                    {
                        Tap(intent, Verb.Skill2);
                    }
                }
                else if (kit is Abilities.SeanHeroKit)
                {
                    if (_motor.HoldingSlipper)
                    {
                        Tap(intent, Verb.Skill2);
                    }
                }
                else if (kit is Abilities.ZackHeroKit)
                {
                    if (_motor.HoldingSlipper)
                    {
                        Tap(intent, Verb.Skill2);
                    }
                }
                else if (kit is Abilities.NemuHeroKit)
                {
                    if (_driving)
                    {
                        Tap(intent, Verb.Skill2);
                    }
                }
            }
        }

        /// <summary>
        /// Everything down, and every accumulator with it.
        ///
        /// ⚠️ THE LUNGE HOLD RESETS HERE TOO, NOT JUST THE BUTTON. A bot stunned mid-charge
        /// otherwise resumes from wherever its accumulator stopped and fires the instant it
        /// recovers, which is a lunge nobody saw wind up.
        /// </summary>
        private void ReleaseAll(InputIntent intent)
        {
            _windup = false;
            _lungeHeld = -1.0f;
            _goalValid = false;
            _arrived = false;
            _stuckTime = 0.0f;
            _unstickLeft = 0.0f;
            _driving = false;

            intent.Clear();
            _pressed.Clear();
        }

        // ---- WIND-UP AND LOITER STATE ---------------------------------------

        private bool _goalValid;
        private bool _windup;
        private float _windupTime;
        private float _windupWait;
        private float _windupPower = 1.0f;
        private Vector3 _windupScatter;
        private bool _blundering;
        private float _lungeHeld = -1.0f;
        private float _loiterLeft;
        private float _loiterDir;
        private CharacterMotor _lastThreat;
    }
}
