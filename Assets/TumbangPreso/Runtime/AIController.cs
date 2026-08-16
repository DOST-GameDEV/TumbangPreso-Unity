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

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            _carrier = GetComponent<Carrier>();
            _self = new AiPersonalityRoll(_motor.PlayerSlot);
        }

        private void Update()
        {
            var intent = _motor.Intent;

            if (!_motor.CanAct())
            {
                intent.Clear();
                intent.CommitFrame();
                return;
            }

            float dt = Time.deltaTime;

            // ⚠️ THE PLAN IS CHOSEN HERE AND THE VERB WORK BELOW OBEYS IT. Deciding inside
            // the verb code is what produced a bot that re-decided every frame.
            _stalkTime = Plan == AiPlan.Stalk ? _stalkTime + dt : 0.0f;
            StepUnstick(dt);
            StepPlan(dt);

            if (_motor.IsDefender) ThinkDefender(intent);
            else ThinkAttacker(intent);

            // ⚠️ COMMIT ONCE, AT THE END. The edge queries are derived from the diff against
            // this snapshot, so committing mid-think makes a tap-only verb never fire.
            intent.CommitFrame();
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

            // Waited long enough. Go anyway.
            if (_stalkTime >= AiTuning.StalkPatienceBase + Me.FetchCaution) return true;

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

            var verbs = taya.GetComponent<CombatVerbs>();
            bool winding = verbs != null && verbs.LungeChargeRatio >= 0.0f
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

            // The same 45-degree launch the throw itself uses, which is what makes this a
            // prediction rather than a second opinion.
            Vector3 launch = (flat.normalized + Vector3.up).normalized * speed;

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

        private void ThinkAttacker(InputIntent intent)
        {
            var round = GameServices.Round;
            if (round == null) return;

            if (_carrier != null && _carrier.Held != null)
            {
                // Holding: get to a legal throwing position and let go.
                Vector3 ring = RingPoint(Balance.ConfinementRadius + ThrowStandoff);
                if (MoveToward(intent, ring)) return;

                if (round.CanThrow(_motor))
                {
                    Vector3 mark = round.Lata != null ? round.Lata.transform.position : Vector3.zero;
                    intent.AimPoint = mark;

                    // Charge, then release. Held across frames until the charge is enough.
                    float need = MinPowerForRange();
                    bool longEnough = _carrier.ChargeRatio >= need;

                    // ⚠️⚠️ AND THE LANE HAS TO BE CLEAR. Without this the bot releases into
                    // whoever is standing between it and the can, every single time, which
                    // reads as an AI that cannot aim rather than as one with no idea anybody
                    // is there. Blocked means step sideways and try again, not never throw:
                    // the ring point below moves on its own and the shot opens up.
                    if (longEnough && LaneBlocked(transform.position, mark, need))
                    {
                        intent.Set(Verb.SpecialAbility, true);   // hold the charge
                        MoveToward(intent, RingPoint(Balance.ConfinementRadius + ThrowStandoff));
                        return;
                    }

                    intent.Set(Verb.SpecialAbility, !longEnough);
                }
                return;
            }

            // Not holding: go get one, if this bot is the nearest claimant.
            Slipper target = ChooseSlipper();
            if (target == null)
            {
                MoveToward(intent, RingPoint(Balance.ConfinementRadius + ThrowStandoff));
                return;
            }

            if (!MoveToward(intent, target.transform.position))
                intent.Set(Verb.Grab, true); // arrived: tap to pick up
        }

        /// <summary>
        /// ⚠️ THE CHARGE IS SOLVED, NOT GUESSED, by inverting the range equation against the
        /// launch speed. This is exactly why a per-skin launch speed had to stay narrow: it is
        /// an error term inside this solve, which lives in a different file from the stat that
        /// moves it. At the shipping 5% spread it sits inside the margin already charged to;
        /// at 20% every bot holding a slow slipper would fall short, and it would read as an
        /// AI regression rather than as a balance change.
        /// </summary>
        private float MinPowerForRange()
        {
            var round = GameServices.Round;
            if (round?.Lata == null) return 1.0f;

            float dist = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(round.Lata.transform.position.x, 0, round.Lata.transform.position.z));

            int skin = _carrier != null && _carrier.Held != null ? _carrier.Held.SkinIndex : -1;
            float full = ThrowRules.MaxRange(Balance.LaunchSpeed * Roster.SlipperFlightScale(skin));
            if (full <= 0.0f) return 1.0f;

            // range scales with speed², and speed scales with power, so power = sqrt(d/full).
            return Mathf.Clamp(Mathf.Sqrt(dist / full), Balance.ChargeMinPower, 1.0f);
        }

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

        private void ThinkDefender(InputIntent intent)
        {
            var round = GameServices.Round;
            var lata = round?.Lata;
            if (lata == null) return;

            // Can down: get to the ring and hold E. Passive defence pays only while it stands,
            // so this is the highest-value thing a taya can be doing.
            if (!lata.IsUpright)
            {
                if (!MoveToward(intent, lata.transform.position, Balance.InteractionRadius * 0.6f))
                    intent.Set(Verb.Grab, true);
                return;
            }

            // Can up: guard, and lunge at a vulnerable attacker if one is in reach.
            CharacterMotor prey = NearestTaggable();
            if (prey != null)
            {
                Vector3 to = prey.transform.position - transform.position;
                to.y = 0.0f;

                // ⚠️ RANGE AND CONE, NOT A TIER CHECK. The .gd gates a lunge on `lunge_range`
                // (Bata 1.9 / Normal 2.6 / Astig 3.1) AND on the target being inside
                // `lunge_cone` — which is a HALF-ANGLE where smaller is stricter, so Astig's
                // 28° is more disciplined than Bata's 55°. The earlier "not Easy" test gave
                // Bata and Astig identical lunges, which is most of why every tier felt the same.
                if (to.magnitude <= Mathf.Min(Me.LungeRange, Combat.LungeReach())
                    && WithinLungeCone(to))
                {
                    FaceToward(prey.transform.position);
                    intent.Set(Verb.Lunge, true);
                    return;
                }

                MoveToward(intent, prey.transform.position);
                return;
            }

            // Nobody to chase: hold the post between the can and the nearest attacker.
            MoveToward(intent, GuardPost(lata.transform.position));
        }

        private CharacterMotor NearestTaggable()
        {
            var round = GameServices.Round;
            if (round == null) return null;

            CharacterMotor best = null;
            float bestDist = float.MaxValue;

            foreach (var p in round.Players)
            {
                if (p == null || !p.IsTaggable()) continue;

                float d = Vector3.Distance(transform.position, p.transform.position);
                if (d >= bestDist) continue;

                bestDist = d;
                best = p;
            }

            return best;
        }

        private Vector3 GuardPost(Vector3 lataPos)
        {
            var round = GameServices.Round;
            if (round == null) return lataPos;

            CharacterMotor threat = null;
            float bestDist = float.MaxValue;

            foreach (var p in round.Players)
            {
                if (p == null || p.IsDefender) continue;

                float d = Vector3.Distance(lataPos, p.transform.position);
                if (d >= bestDist) continue;

                bestDist = d;
                threat = p;
            }

            if (threat == null) return lataPos;

            Vector3 dir = threat.transform.position - lataPos;
            dir.y = 0.0f;
            return lataPos + dir.normalized * Balance.DefenderStartOffset;
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ EVERY GOAL IS CLAMPED INTO THE PLAYABLE AREA, AND THAT IS NOT BELT AND BRACES.
        /// A goal outside the world is not merely unreachable, it looks like a completely
        /// different bug: bots jammed against a wall were reported as "pathfinding broken,
        /// they just walk up the houses", when the houses have no collision at all and the
        /// bots were simply pinned trying to reach a point they could never stand on. It cost
        /// most of the offence in the match, and the radius alone was not the fix. Making an
        /// out-of-world goal IMPOSSIBLE TO GENERATE is.
        /// </summary>
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

        /// <summary>Returns true while still travelling.</summary>
        private bool MoveToward(InputIntent intent, Vector3 goal, float slop = ArriveSlop)
        {
            goal = ClampToPlayable(goal);

            Vector3 to = goal - transform.position;
            to.y = 0.0f;

            if (to.magnitude <= slop)
            {
                intent.Move = Vector2.zero;
                _arrived = true;
                return false;
            }

            _arrived = false;
            _driving = true;

            Vector3 dir = to.normalized;

            // ⚠️ NINETY DEGREES OFF THE WANTED HEADING WHILE UNSTICKING. Enough to clear a
            // corner, and it still makes progress ALONG the obstacle rather than backing away
            // from it — backing off just walks into the same corner again a second later.
            if (_unstickLeft > 0.0f)
                dir = new Vector3(-dir.z * _unstickSign, 0.0f, dir.x * _unstickSign);

            // ⚠️⚠️ EIGHT-WAY, NOT ANALOGUE, AND THAT IS A FAIRNESS RULE. A keyboard player has
            // exactly eight headings; a bot writing a continuous vector glides along angles no
            // human can hold, which is invisible in a screenshot and obvious in play. The
            // threshold is sin(22.5°) — the half-angle of a 45° sector — so a heading snaps to
            // the same key combination a player would have pressed.
            intent.Move = EightWay(dir);

            // ⚠️ DISTANCE AND A STAMINA RESERVE, NOT A TIER CHECK. The .gd sprints past
            // `SPRINT_DISTANCE` (5.0) and holds back `sprint_reserve` of the meter — Bata
            // spends everything (0.0), Astig keeps nearly half (0.45) so it still has a
            // chase left when it matters. Gating on tier alone meant Normal never sprinted.
            intent.Set(Verb.Sprint,
                to.magnitude > AiTuning.SprintDistance && StaminaFraction() > Me.SprintReserve);

            FaceToward(goal);
            return true;
        }

        /// <summary>Fraction of the stamina meter still available, 0..1.</summary>
        private float StaminaFraction() => _motor.Stamina?.Ratio ?? 1.0f;

        /// <summary>
        /// Is <paramref name="to"/> inside this tier's lunge cone, measured off the body's
        /// facing? The cone is a HALF-ANGLE in degrees and is floored at
        /// <see cref="AiTuning.LungeConeFloor"/>, because an eight-way heading cannot aim
        /// finer than that and a tighter cone would ask for an angle the bot has no key for.
        /// </summary>
        private bool WithinLungeCone(Vector3 to)
        {
            if (to.sqrMagnitude < 0.0001f) return true;

            float half = AiTuning.EffectiveLungeCone(ActiveDifficulty);
            return Vector3.Angle(transform.forward, to.normalized) <= half;
        }

        private void FaceToward(Vector3 point)
        {
            Vector3 to = point - transform.position;
            to.y = 0.0f;
            if (to.sqrMagnitude < 0.01f) return;

            transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        }
    }
}
