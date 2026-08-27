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
    // ⚠️⚠️ IT RUNS BEFORE `PlayerInputReader`, AND UNTIL 2026-08-27 NEITHER DECLARED AN ORDER AT
    // ALL. Both write `CharacterMotor.Intent`, so with two writers and no order Unity picked one
    // arbitrarily and the loser's presses were overwritten before the physics step read them.
    // That is normally invisible because exactly one of the two is ever on a body, and there is
    // exactly one place where BOTH are: `GhostPetCompanion.BeginPossession` adds a temporary AI
    // to Nemu while the player drives Kuro. See `AbilitiesEnabled` below for what it cost.
    [DefaultExecutionOrder(-130)]
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class AIController : MonoBehaviour
    {
        /// <summary>
        /// Whether this controller is allowed to press the three hero keys.
        ///
        /// ⚠️⚠️ IT IS FALSE FOR EXACTLY ONE CONTROLLER: THE TEMPORARY ONE THAT DRIVES NEMU'S BODY
        /// WHILE SHE IS RIDING KURO. 🧑 2026-08-27: *"nemu E recast doesnt work as intended,
        /// she's supposed to teleport to where her ghost is when she recasts ... but right now
        /// recasting just extends ghost form time"*, and *"u cant end ability early"*.
        ///
        /// ⚠️⚠️ THE RECAST WAS BEING WRITTEN AND THEN ERASED IN THE SAME FRAME.
        /// `PlayerInputReader` deliberately keeps Skill2 live during a possession so the player
        /// can come home (*"Allow skill2 recast to teleport and end possession"*), and this
        /// controller writes all three hero keys every frame it runs. Two writers on one
        /// `InputIntent`, in an undefined order, and the AI wrote `Skill2 = false` after the
        /// player wrote `true` often enough that the return trip simply did not exist. The
        /// possession then ran its full 6 s and ended on the timer, which is exactly the reported
        /// *"recasting just extends ghost form time"*: nothing was extended, the press was eaten.
        ///
        /// ⚠️ THE FIX IS TO GIVE THE KEY ONE OWNER, NOT TO ORDER THE TWO WRITERS. The execution
        /// order above is the belt; this is the braces, and it is the one that states the rule:
        /// while a human is driving the pet, the human owns the hero keys and the AI owns the
        /// legs. `CLAUDE.md` § 4's *"a bot presses the same buttons a human does"* is unharmed,
        /// because this is one body with two drivers rather than a second path into the game.
        /// </summary>
        public bool AbilitiesEnabled { get; set; } = true;
        [SerializeField] private AiTier _tier = AiTier.Normal;

        /// <summary>
        /// This bot's tuning row. ⚠️ READ THROUGH <see cref="AiTuning"/> RATHER THAN COPIED
        /// INTO FIELDS, so a difficulty changed from the pause menu mid-match reaches bots
        /// that were spawned before the change. Godot did this with a `tuning_stamp` each
        /// controller compared against; a property read is the same guarantee for free.
        /// </summary>
        public static Difficulty ActiveDifficulty = Difficulty.Normal;

        /// <summary>
        /// The index that means "no bots at all", not "bots that play badly".
        ///
        /// ⚠️⚠️ IT IS APPENDED AFTER HARD RATHER THAN PREPENDED BEFORE EASY, AND THAT IS NOT A
        /// TASTE CALL. `GameSettings.AiDifficulty` is a saved int, `MatchRpc` replicates the same
        /// int to every peer in a lobby, and `Difficulty` in the core package is `(Difficulty)`
        /// cast straight off it. Inserting a value at 0 would silently reinterpret every saved
        /// setting and every in-flight lobby message by one tier. At the end, every existing
        /// index keeps the meaning it has always had and there is nothing to migrate.
        /// </summary>
        public const int NoBotsIndex = 3;

        /// <summary>
        /// False while the practice lobby is set to NONE.
        ///
        /// 🧑, 2026-08-26: *"make it so that in practice mode theres an option to turn off all
        /// bots ... just you there no bots"*.
        ///
        /// ⚠️ IT IS AN ABSENCE OF SEATS, NOT A PARKED BRAIN. `MatchInstaller` does not BUILD the
        /// other three seats when this is false. Spawning four bodies and disabling three
        /// controllers would leave three motionless characters standing on the attacker line,
        /// still registered, still scored, still on the scoreboard, which is not what "no bots"
        /// means to anybody looking at the street.
        /// </summary>
        public static bool BotsEnabled = true;

        /// <summary>
        /// Godot's `AIController.apply_difficulty()`, called off the saved setting index.
        ///
        /// ⚠️ NOTHING CALLED THIS BEFORE, so the difficulty in the settings panel was saved,
        /// displayed, and then ignored — every bot in every match played at Normal. The
        /// index is clamped rather than trusted: it comes off disk.
        ///
        /// ⚠️ THE TIER STILL CLAMPS TO 0..2 WHEN THE INDEX IS `NoBotsIndex`. Nothing reads the
        /// tier in that case, but leaving `ActiveDifficulty` holding a cast of 3 would put an
        /// out-of-range enum into `AiTuning.For`, which is a crash waiting for the first line
        /// that stops checking `BotsEnabled` first.
        /// </summary>
        public static void ApplyDifficulty(int savedIndex)
        {
            BotsEnabled = savedIndex != NoBotsIndex;
            ActiveDifficulty = (Difficulty)Mathf.Clamp(savedIndex, 0, 2);
        }

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

        /// <summary>
        /// How near the whistle a bot stops improving a shot and simply takes it, in seconds.
        ///
        /// ⚠️⚠️ IT IS THE FIRST TIME ANYTHING IN THIS FILE READS THE ROUND CLOCK. Every patience
        /// bound in the wind-up is measured from when the charge STARTED, so a bot that began one
        /// with four seconds left waited out its aim settle and its lane patience and was still
        /// holding the button at the whistle: charge discarded, slipper still in hand, nothing
        /// scored. `docs/VISION.md` § 4 asks Hero Strike for *"timing"*, and a player who cannot
        /// tell the difference between eighty seconds left and two has none.
        ///
        /// ⚠️ 0.45 s IS THE INPUT BUFFER PLUS A PHYSICS STEP, NOT A FEEL VALUE.
        /// `HeroAbilitySystem.InputBufferWindow` is 0.30 s and the throw resolves on the step
        /// after the release, so a release later than this is one the round ends before it can
        /// answer. Any larger and a bot starts throwing away good shots it had time to finish.
        /// </summary>
        private const float LastCallSeconds = 0.45f;

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

                // ⚠⚠ A BOT MASHES TO GET UP, BECAUSE A BOT PRESSES THE SAME BUTTONS A HUMAN
                // DOES. `docs/VISION.md` § 4 makes that an invariant rather than a nicety: the
                // alternative is a second path where a human answers a trip and a bot cannot,
                // which would show up in `BotBehaviourProbe` as bots spending measurably longer
                // on the floor than the same seat played by hand, and would quietly bias every
                // hazard measurement taken from that probe.
                //
                // ⚠ THE TOGGLE IS WHAT MAKES IT A MASH. `MashRecover` reads `JustPressed`, which
                // is an EDGE, so a held key produces exactly one press in a lifetime. Alternating
                // the held state gives one edge every other frame; `Combat.MashRecover`'s rate
                // cap then throws away everything above 10 Hz, so the bot is held to the same
                // ceiling as a player rather than to the frame rate.
                //
                // ⚠⚠ THE STATE LIVES IN A FIELD, AND READING IT BACK OFF THE INTENT MEANT
                // A TRIPPED BOT GOT EXACTLY ONE PRESS PER FALL. This line was
                // `intent.Set(Verb.Jump, !intent.Pressed(Verb.Jump))`, and `ReleaseAll` three
                // lines above calls `intent.Clear()`. So `Pressed(Jump)` was read from a table
                // that had just been emptied: it answered false every single frame, the toggle
                // set true every single frame, and the held state never alternated at all. After
                // `CharacterMotor.FixedUpdate` took its first snapshot, `_heldPrev` contained
                // Jump for the rest of the fall and `JustPressed` was false forever.
                //
                // ⚠ THE COMMENT ABOVE WAS ALREADY RIGHT ABOUT WHY, WHICH IS WHAT MAKES THIS
                // WORTH SPELLING OUT. It says a held key fires once in a lifetime; the code then
                // held the key. The bug was not a misunderstanding, it was reading the toggle
                // out of the one object that gets wiped immediately beforehand.
                //
                // ⚠ `Tap` WOULD NOT HAVE WORKED EITHER, for the same reason: it alternates off
                // `_pressed`, and `ReleaseAll` clears that too. A dedicated field is the only
                // state on this path that survives the release.
                //
                // ⚠ WHAT IT COST: bots ate essentially the whole of every trip while a human
                // mashed out in about 1.3 s. `BotBehaviourProbe` measures hazard penalties, so
                // every trip-hazard number ever taken from it was measured against bots that
                // could not answer a hazard. This comment's own note predicted that failure mode
                // and the code underneath it had it.
                if (_motor.IsTripped || _motor.StunElement != StunElement.None)
                {
                    _mashHeld = !_mashHeld;
                    intent.Set(Verb.Jump, _mashHeld);
                }

                return;
            }

            float dt = Time.deltaTime;
            if (_emoteCooldown > 0.0f) _emoteCooldown -= dt;

            Observe(dt);

            // ⚠️ THE PLAN IS CHOSEN HERE AND THE VERB WORK BELOW OBEYS IT. Deciding inside
            // the verb code is what produced a bot that re-decided every frame.
            _stalkTime = Plan == AiPlan.Stalk ? _stalkTime + dt : 0.0f;
            if (_headingCommitLeft > 0.0f) _headingCommitLeft -= dt;

            // ⚠️ THE BEAT IS PAID BEFORE THE GLANCE RUNS, not alongside it. A glance is a
            // movement key held for `GlanceSeconds` 0.09 s, and a heading the plan changed just
            // before it can leave a `KeyChangeBeatSeconds` 0.12 s beat owed. Decrementing both
            // together lets the beat eat the whole glance, and the bot never turns to look at
            // anything at all.
            if (_keyGapLeft > 0.0f) _keyGapLeft -= dt;
            else if (_glanceLeft > 0.0f) _glanceLeft -= dt;

            StepSprintKey(dt);
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

                // ⚠️⚠️ AND SOMEBODY ELSE MAY HAVE THE BETTER RUN. See `IHaveTheBestRun`: three
                // attackers who each decide the box is safe all enter it together, which is the
                // one thing a taya cannot lose to and the three of them cannot win.
                if (!FetchIsSafe(mine, taya) || !IHaveTheBestRun(mine, taya)) return AiPlan.Stalk;

                return AiPlan.Fetch;
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
            // ⚠️⚠️ WITH THE WHISTLE COMING, THE SHOT YOU HAVE BEATS THE SHOT YOU WANTED. Below
            // this the bot stops walking to a better bearing and plants where it stands.
            // `Position` is a walk to a scored throwing spot that can be several seconds away,
            // and arriving at a perfect angle after the round has ended is worth exactly as much
            // as never leaving. See `LastCallSeconds` for the release end of the same clock.
            //
            // ⚠️ IT IS THE FULL CHARGE PLUS THE LAST CALL, so a bot that plants on this frame can
            // still reach the power it planned for. Anything shorter makes it plant and then
            // release under-charged, which is a worse shot than the one it gave up walking to.
            if (round.RoundActive
                && round.TimeLeft <= Balance.ChargeFullTime + LastCallSeconds)
                return AiPlan.Windup;

            // ⚠️⚠️ A CHARGE ALREADY RUNNING IS NEVER RECONSIDERED, AND THAT ORDER IS LOAD-BEARING.
            // The comment above says it: a bot that re-decides mid-charge never releases. So the
            // inbound check below sits AFTER this line and can only stop a charge being STARTED.
            if (Plan == AiPlan.Windup) return AiPlan.Windup;

            // ⚠️⚠️ DO NOT THROW AT A CAN SOMEBODY ELSE IS ABOUT TO KNOCK OVER. See
            // `RivalShotIsInbound`: `RoundDirector.CanThrow` refuses a throw while the lata is
            // down, so a second shoe released into a live arc is a 2.5 s charge, a tsinelas out
            // of your hand and a retrieval run you now have to make, bought for nothing.
            if (RivalShotIsInbound(lata)) return AiPlan.Position;

            if (_arrived && Plan == AiPlan.Position) return AiPlan.Windup;

            return AiPlan.Position;
        }

        // -------------------------------------------------------------------
        // § SOMEBODY ELSE'S SHOE IS ALREADY IN THE AIR
        //
        // ⚠️⚠️ FOUR SEATS THROWING AT ONE CAN HAD NO NOTION OF EACH OTHER AT ALL. Every attacker
        // decided independently, so a knockdown was routinely followed by one or two more
        // releases inside the same second: the can was already going over, `CanThrow` refused
        // them the moment it landed, and those bots spent a full charge and their tsinelas to
        // score nothing and then had to go back into the box for it.
        //
        // ⚠️⚠️ IT IS ALSO MOST OF WHAT A KNOCKDOWN LOOKS LIKE ON SCREEN. Three arcs, three
        // impacts and three sets of debris inside a second is the pile-up 🧑 reported as the game
        // being overwhelming, and it is the AI half of it rather than the effects half.
        //
        // ⚠️ IT ASKS THE SAME QUESTION THE FLIGHT WILL. The arc is walked with the same gravity
        // and the same step `TryInterceptPoint` uses, so the answer cannot drift from what
        // actually happens; a bearing test would call a shoe sailing two metres over the can a
        // hit.
        //
        // ⚠️ AND IT ONLY DELAYS. `PlanAttacker` returns `Position`, so the bot keeps working its
        // angle and charges the moment the arc has resolved, one way or the other.
        // -------------------------------------------------------------------

        private bool RivalShotIsInbound(Lata lata)
        {
            if (lata == null) return false;

            Vector3 can = lata.transform.position;

            foreach (var s in FindObjectsByType<Slipper>(FindObjectsInactive.Exclude))
            {
                if (s == null || s.State != SlipperState.InFlight) continue;
                if (s.OwnerSlot == _motor.PlayerSlot) continue;

                Vector3 p = s.transform.position;
                Vector3 v = s.Velocity;

                for (float t = 0.0f; t < AiTuning.InterceptHorizon; t += AiTuning.InterceptStep)
                {
                    Vector3 at = p + v * t + Vector3.down * (0.5f * Balance.Gravity * t * t);

                    // Below the road means the arc is over and it missed.
                    if (at.y < can.y - 0.20f) break;

                    // ⚠️ `SlipperHitRadius + LataHitMargin` IS THE GAME'S OWN KNOCKDOWN WINDOW,
                    // not a number picked here. `Balance.LataHitMargin`'s note calls it *"the
                    // number that decides every knockdown in the game"*, and it was three
                    // different numbers in three files once; this must never become the fourth.
                    if (Flat(at, can) <= Balance.SlipperHitRadius + Balance.LataHitMargin)
                        return true;
                }
            }

            return false;
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

            var quarry = TagTarget();

            // ⚠️⚠️ AND THE CHASE HAS TO BE GOING SOMEWHERE. See § WHEN A CHASE IS OVER: nothing
            // used to ask, so a taya could be walked to the far end of Aurora Boulevard by an
            // attacker who was simply faster, leaving the can unguarded and the passive score
            // stopped for the rest of the round.
            if (quarry != null && ChaseIsGoingSomewhere(quarry, dt)) return AiPlan.Hunt;

            if (Me.Camp > 0.0f && HasCoverPoint(lata)) return AiPlan.Cover;

            return AiPlan.Guard;
        }

        // -------------------------------------------------------------------
        // § WHEN A CHASE IS OVER
        //
        // ⚠️⚠️ THE GIVE-UP IS PER QUARRY AND IT RESETS ON A NEW ONE, so abandoning one attacker
        // does not make the taya passive: the next body to step into the chalk is a fresh chase
        // with a full window. What it stops is the SAME chase running all round.
        //
        // ⚠️⚠️ AND A CHASE THAT WAS GIVEN UP CAN BE RESUMED, WHICH IS WHY THIS FORGETS RATHER
        // THAN BLACKLISTS. `Guard` posts between the can and the live threat, so a taya that
        // stops chasing walks back toward the objective and is usually CLOSER to the same
        // attacker's next approach than it was when it gave up. Blacklisting them would hand
        // that attacker a free retrieval, which is the opposite of the intent.
        //
        // ⚠️ MEASURED AGAINST THE OBSERVED POSITION like every other read of a rival here, so a
        // taya cannot judge its own progress off information its hands have not been given.
        // -------------------------------------------------------------------

        private CharacterMotor _chasing;
        private float _chaseBestDistance = float.MaxValue;
        private float _chaseStaleFor;

        private bool ChaseIsGoingSomewhere(CharacterMotor quarry, float dt)
        {
            float now = Flat(transform.position, At(quarry));

            if (_chasing != quarry)
            {
                _chasing = quarry;
                _chaseBestDistance = now;
                _chaseStaleFor = 0.0f;
                return true;
            }

            // ⚠️ THE BEST DISTANCE EVER REACHED, NOT LAST TICK'S. Comparing against the previous
            // sample makes a quarry who jinks look like progress every time they turn round;
            // comparing against the closest this chase has ever been asks the only question that
            // matters, which is whether it is getting anywhere overall.
            if (now <= _chaseBestDistance - AiTuning.ChaseProgressMetres)
            {
                _chaseBestDistance = now;
                _chaseStaleFor = 0.0f;
                return true;
            }

            _chaseStaleFor += dt;

            // ⚠️ A HELPLESS QUARRY IS NEVER ABANDONED. Somebody stunned or face down cannot run,
            // so a chase that is not closing on them is a pathing problem rather than a losing
            // race, and giving up would walk away from a free tag.
            if (quarry.IsStunned || quarry.IsTripped) return true;

            if (_chaseStaleFor < AiTuning.ChasePatienceSeconds) return true;

            _chasing = null;
            _chaseBestDistance = float.MaxValue;
            _chaseStaleFor = 0.0f;
            return false;
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
        // -------------------------------------------------------------------
        // § ONE RUNNER AT A TIME
        //
        // ⚠️⚠️ `FetchIsSafe` ASKS "IS THE BOX SAFE FOR ME", AND THREE ATTACKERS ASKING IT AT THE
        // SAME MOMENT ALL GET THE SAME ANSWER. Every one of its escapes is a fact about the
        // WORLD rather than about the asker: the can is down, the taya spent their lunge, the
        // taya is far from the shoe. So the three of them agree, enter the chalk together, and
        // hand the taya the easiest round of their life while the screen fills with bodies.
        //
        // ⚠️⚠️ WHICH IS ALSO NOT HOW THE GAME IS PLAYED. `docs/VISION.md` § 0: *"The tension is
        // the retrieval, not the throw."* A retrieval is tense because ONE person is exposed and
        // the others are waiting for the taya to commit to them. Three simultaneous runs is not
        // three times the tension, it is none: the taya cannot cover three lines, so nobody made
        // a decision and the outcome is whoever the selector happened to pick.
        //
        // ⚠️⚠️ IT IS DERIVED, NOT NEGOTIATED, WHICH IS WHY THERE IS NO SHARED STATE ANYWHERE.
        // Every bot runs the identical comparison over the identical world and reaches the
        // identical answer, exactly as `ClaimSlack` already does for "only the nearest attacker
        // goes for a loose slipper". A claim held in a static would be a channel between bots
        // that a human is not on, and `CLAUDE.md` § 4's *"a bot presses the same buttons a human
        // does"* is about there being no second path. Reading the board is not a second path: a
        // human can see the same three bodies.
        //
        // ⚠️ AND IT IS ONLY EVER A DELAY. Everything that overrides caution overrides this too,
        // by construction: `FetchIsSafe` is asked FIRST, so a bot on the tournament clock, or one
        // that has stalled long enough, or one running at a downed can, never reaches this
        // question at all.
        // -------------------------------------------------------------------

        /// <summary>
        /// Among the attackers who could go right now, do I have the best odds?
        ///
        /// The margin is how much further the taya has to come than I do. It is the honest
        /// question a player asks looking at the box: can I get there and out before they can
        /// get across.
        /// </summary>
        private bool IHaveTheBestRun(Slipper mine, CharacterMotor taya)
        {
            var round = GameServices.Round;
            if (round == null || taya == null || mine == null) return true;

            float myOdds = RunOdds(At(taya), transform.position, mine.transform.position);

            foreach (var who in round.Players)
            {
                if (who == null || who == _motor || who.IsDefender) continue;

                // Somebody already holding a tsinelas is not competing for the box, they are
                // trying to leave it.
                if (who.HoldingSlipper) continue;

                // ⚠️ A RIVAL WHO CANNOT MOVE IS NOT A RIVAL. Yielding to a stunned attacker
                // would stall the whole round behind somebody lying on the tarmac.
                if (!who.CanAct()) continue;

                var theirs = SlipperOwnedBy(round, who.PlayerSlot);
                if (theirs == null || theirs.State != SlipperState.Loose) continue;

                float theirOdds = RunOdds(At(taya), At(who), theirs.transform.position);

                // ⚠️ STRICTLY BETTER, AND THE SEAT BREAKS AN EXACT TIE. Two bots that each yield
                // to the other is a deadlock that lasts until the tournament clock breaks it, and
                // a deadlock is worse than the pile-up this exists to stop.
                if (theirOdds > myOdds + AiTuning.RunOddsMargin) return false;

                if (Mathf.Abs(theirOdds - myOdds) <= AiTuning.RunOddsMargin
                    && who.PlayerSlot < _motor.PlayerSlot) return false;
            }

            return true;
        }

        /// <summary>
        /// How much of a head start a runner has on the taya, in metres. Higher is safer.
        /// </summary>
        private static float RunOdds(Vector3 tayaAt, Vector3 runnerAt, Vector3 slipperAt)
            => Flat(tayaAt, slipperAt) - Flat(runnerAt, slipperAt);

        private static Slipper SlipperOwnedBy(RoundDirector round, int slot)
        {
            foreach (var s in FindObjectsByType<Slipper>(FindObjectsInactive.Exclude))
                if (s != null && s.OwnerSlot == slot) return s;

            return null;
        }

        private bool FetchIsSafe(Slipper mine, CharacterMotor taya)
        {
            if (Me.FetchCaution <= 0.0f || taya == null) return true;

            var round = GameServices.Round;

            // ⚠️⚠️ THE TOURNAMENT CLOCK OUTRANKS CAUTION, AND IT HAS TO INTERRUPT EARLY ENOUGH
            // TO ARRIVE. This waited until three quarters of a second before the WARNING, which
            // is 6.25 s into a 10 s grace: enough time to decide, and not enough to cross an
            // arena 13 m deep at 4.6 m/s while a hero hazard is in the way. Measured over whole
            // Hero Strike matches the bots were still walking when the fine started, and the
            // unretrieved-slipper penalty count swung between 0 and 205 on the personality roll
            // alone. Half the warning time leaves 6.5 s of travel for a 3 s crossing.
            //
            // ⚠️ IT READS THE AUTHORITATIVE TIMER, THE SAME ONE THE HUD COUNTS DOWN. A bot with
            // its own idea of how long it has been idle can be wrong in the direction that
            // costs points.
            if (round != null && round.AttackerIdleSeconds(_motor.PlayerSlot)
                >= Balance.SlipperUnretrievedWarningTime * 0.5f) return true;

            // Waited long enough. Go anyway to keep the round moving.
            if (_stalkTime >= 2.0f + Me.FetchCaution * 0.4f) return true;

            // The can is down: nobody can be tagged at all, so the run is free.
            var lata = round?.Lata;
            if (lata != null && !lata.IsUpright) return true;

            // The taya just spent their lunge.
            var tayaVerbs = taya.GetComponent<CombatVerbs>();
            if (tayaVerbs != null && tayaVerbs.LungeCooldownLeft > 0.35f) return true;

            // Somebody ELSE is taggable, so the taya has a better target than me.
            foreach (var who in round.Players)
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
            float bestScore = float.NegativeInfinity;

            Vector3 tayaAt = At(taya);

            foreach (var who in GameServices.Round.Players)
            {
                if (who == null || who == _motor || who.IsDefender) continue;

                Vector3 victimAt = At(who);

                float d = Flat(transform.position, victimAt);
                if (d > reach || d < 0.05f) continue;

                // ⚠️⚠️ WHICH WAY THE SHOVE PUSHES THEM, WHICH NOTHING HERE USED TO ASK.
                // `CombatVerbs.HostResolveShove` pushes along `victim - shover`, so the only
                // rival worth spending a shove and 25 stamina on is one the push moves TOWARD the
                // taya. This picked the nearest rival in any direction, and the header above has
                // said *"a rival worth shoving into the taya's reach"* the whole time: half of
                // every sabotage was shoving somebody to SAFETY, which is worse than not casting
                // it, and the other half was luck.
                Vector3 push = victimAt - transform.position;
                push.y = 0.0f;

                Vector3 toTaya = tayaAt - victimAt;
                toTaya.y = 0.0f;

                if (push.sqrMagnitude < 0.0001f || toTaya.sqrMagnitude < 0.0001f) continue;

                float aim = Vector3.Dot(push.normalized, toTaya.normalized);

                // ⚠️ THE BAR IS "NOT COUNTERPRODUCTIVE", NOT A TIGHT CONE, and that is the whole
                // of the trade. `Spacing` is deliberately pushing the three attackers apart, so
                // the opportunities are already rare: `SabotageTarget`'s own note records the
                // willingness dial reading ZERO sabotages over a whole match before the reach was
                // scaled by it. A cone here would take it back to zero, and a dial that changes
                // nothing is the defect that note exists about. Anything that closes on the taya
                // at all is admitted; `aim` then decides between them.
                if (aim <= 0.0f) continue;

                // ⚠️ A RIVAL CARRYING IS THE ONE A SHOVE CAN ACTUALLY COST SOMETHING. Being
                // taggable needs a tsinelas in hand and a body inside the chalk, so shoving an
                // empty-handed rival at the taya sets up nothing at all.
                float score = aim * 2.0f - AiTuning.TagDistanceWeight * d;
                if (who.HoldingSlipper) score += 1.0f;

                if (score <= bestScore) continue;

                bestScore = score;
                best = who;
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
            => LaneBlockedWithSpin(origin, target, target, power, 0.0f);

        private bool LaneBlockedWithSpin(Vector3 origin, Vector3 aimTarget, Vector3 arrivalTarget,
            float power, float spin)
        {
            var round = GameServices.Round;
            if (round == null) return false;

            int skin = _carrier != null && _carrier.Held != null ? _carrier.Held.SkinIndex : -1;
            float speed = ThrowRules.LaunchSpeedFor(skin, power);

            Vector3 flat = aimTarget - origin;
            flat.y = 0.0f;

            float distance = flat.magnitude;
            if (distance < 0.01f) return false;

            // ⚠️ THE SAME SOLVE THE THROW ITSELF USES, which is what makes this a prediction
            // rather than a second opinion. It was a fixed 45-degree lob here too, so the bot
            // was walking an arc the game does not fly and answering about the wrong lane.
            Vector3 velocity = Slipper.SolveArc(origin, aimTarget, speed) * speed;

            float step = Mathf.Clamp(AiTuning.LaneSampleArc / Mathf.Max(speed, 1.0f),
                                     AiTuning.LaneStepMin, AiTuning.LaneStepMax);

            Vector3 point = origin;

            for (int i = 0; i < AiTuning.LaneMaxSteps; i++)
            {
                velocity.y -= Balance.Gravity * step;

                if (Mathf.Abs(spin) > 0.01f)
                {
                    Vector3 flatVelocity = new Vector3(velocity.x, 0.0f, velocity.z);
                    if (flatVelocity.sqrMagnitude > 0.1f)
                    {
                        Vector3 lateral = Vector3.Cross(flatVelocity.normalized, Vector3.up).normalized;
                        velocity += lateral * (spin * Balance.PektusCurveStrength * step);
                    }
                }

                point += velocity * step;

                if (Flat(point, arrivalTarget) <= Balance.SlipperHitRadius + 0.30f)
                    return false;                       // it gets there

                if (point.y < arrivalTarget.y - 1.0f)
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

        private Vector3 CompensatedPektusAim(Vector3 origin, Vector3 target, float power, float spin)
        {
            if (Mathf.Abs(spin) < 0.01f) return target;

            int skin = _carrier != null && _carrier.Held != null ? _carrier.Held.SkinIndex : -1;
            float speed = ThrowRules.LaunchSpeedFor(skin, power);
            Vector3 flat = target - origin;
            flat.y = 0.0f;
            if (flat.sqrMagnitude < 0.01f) return target;

            Vector3 launch = Slipper.SolveArc(origin, target, speed) * speed;
            float flightTime = flat.magnitude /
                Mathf.Max(new Vector2(launch.x, launch.z).magnitude, 0.1f);
            Vector3 lateral = Vector3.Cross(flat.normalized, Vector3.up).normalized;
            Vector3 drift = lateral * (0.5f * spin * Balance.PektusCurveStrength
                                       * flightTime * flightTime);
            return target - Vector3.ClampMagnitude(drift, 3.0f);
        }

        private float ChoosePektusSpin(Vector3 origin, Vector3 target, float power)
        {
            if (ActiveDifficulty == Difficulty.Bata) return 0.0f;
            if (!LaneBlockedWithSpin(origin, target, target, power, 0.0f)) return 0.0f;

            float[] candidates = ActiveDifficulty == Difficulty.Astig
                ? new[] { -0.55f, 0.55f, -1.0f, 1.0f }
                : new[] { -0.55f, 0.55f };

            // Alternate the first side by seat so coordinated attackers do not all
            // bend into the same interception lane.
            if ((_motor.PlayerSlot & 1) != 0)
                System.Array.Reverse(candidates);

            foreach (float candidate in candidates)
            {
                Vector3 aim = CompensatedPektusAim(origin, target, power, candidate);
                if (!LaneBlockedWithSpin(origin, aim, target, power, candidate))
                    return candidate;
            }

            return 0.0f;
        }

        /// <summary>
        /// Is there a loose slipper whose retrieval line is worth sitting on?
        ///
        /// ⚠️ CAMPING IS A DESIGNED TAYA BEHAVIOUR, not an exploit. It is what puts the
        /// attacker's patience under real pressure and is why FetchIsSafe needs a bound.
        ///
        /// ⚠️⚠️ IT ASKS `TryCoverPoint`, AND IT USED TO ANSWER "ANY LOOSE SLIPPER EXISTS", WHICH
        /// IS THE SAME FAULT `TryInterceptPoint`'S OWN HEADER RECORDS ONE SCREEN UP. That one
        /// reads *"SOMETHING IS IN FLIGHT IS NOT AN INTERCEPT"*: the plan was chosen off a
        /// condition much weaker than the plan's own requirements, so the taya committed to a verb
        /// it could not execute. This was the identical shape for the identical reason.
        /// `TryCoverPoint` additionally requires the slipper to be INSIDE the box, which the taya
        /// cannot leave, and a claimant with free hands who can act; neither was tested here, so a
        /// tsinelas lying on the pavement outside the chalk with everybody carrying put every
        /// camping taya into Cover, whereupon `DoCover` fell straight through to `DoGuard`.
        ///
        /// ⚠️ THE COST WAS NOT THE FALLTHROUGH, IT WAS THE FLIPPING. A plan change costs
        /// `_self.Hesitation`, `StepPlan` clears the goal with it, and Cover and Guard traded
        /// places every time a slipper's state changed. It also made the plan LIE, which matters
        /// beyond the taya: `AiDiagnosticProbe` prints it, and four of the skill gates in
        /// `StepHeroAbilities` branch on it.
        ///
        /// ⚠️ THE UNUSED `lata` PARAMETER IS KEPT DELIBERATELY, matching `HasInterceptPoint`. Both
        /// read as a question about the board at the call site, and `PlanDefender` has already
        /// null-checked the can by the time it asks either of them.
        /// </summary>
        private bool HasCoverPoint(Lata lata) => TryCoverPoint(out _);

        /// <summary>
        /// The attacker this taya should be chasing.
        ///
        /// ⚠️⚠️ IT USED TO RETURN THE FIRST TAGGABLE ATTACKER IN `round.Players` ORDER, AND THAT
        /// IS 🧑'S *"they ... always just target the human"* IN ONE LINE. Seat order is a fixed
        /// list, so it is a fixed priority: whichever seat sits lowest in it was chased in every
        /// round it was not the taya, by every taya, for the whole match. Nothing here ever read
        /// whether a seat held a person, which is exactly why nobody found it by reading the code
        /// looking for a human check. A person who sits in one seat all night was singled out by
        /// a selector that had no idea they existed.
        ///
        /// ⚠️⚠️ AND IT DECIDED MORE THAN THE CHASE. `StepHeroAbilities` asks this same function
        /// for a DEFENDING hero's target, so the lowest seat also ate every skill and every
        /// ultimate a defending hero spent all match. One `foreach` with a `return` in it
        /// produced both halves of the report.
        ///
        /// ⚠️ THE ANSWER IS A SCORE, NOT A SHUFFLE. Picking at random would break the fixation
        /// and would also throw away the reason to chase anybody, which is that a tag is actually
        /// available. `LiveThreat` has scored the neighbouring GUARD decision this way since the
        /// port, and its own header records this identical fault being found there once already,
        /// from a playtest that read *"the defender ai only attack him"*. The two selectors look
        /// nothing alike in the file, which is why fixing one did not fix the other.
        ///
        /// ⚠️⚠️ THE COMMIT TERM POINTS THE OPPOSITE WAY TO `LiveThreat`'S AND BOTH ARE RIGHT.
        /// Guarding is a standing post, so an equal rival should pull the taya round and its
        /// anti-fixation term is a PENALTY on whoever was guarded last. Chasing is a pursuit, and
        /// a pursuit that changes target on a tie is a taya running down the middle of two
        /// attackers and catching neither. So this one is a BONUS on whoever is already being
        /// chased, worth `AiTuning.TagSwitchMargin`, and a switch has to be earned.
        /// </summary>
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
            if (round.Lata == null || !round.Lata.IsUpright) { _lastTagTarget = null; return null; }

            CharacterMotor best = null;
            float bestScore = float.NegativeInfinity;

            foreach (var who in round.Players)
            {
                if (who == null || who == _motor || who.IsDefender || !who.IsTaggable()) continue;

                float score = 0.0f;

                // ⚠️ THE ONLY CERTAIN TERM. A target already on the floor cannot run, cannot
                // dodge and cannot be shoved out of reach, so the tag is a walk rather than a
                // contest. Seat order chased past bodies lying at its feet whenever the runner
                // happened to hold the lower seat.
                if (who.IsStunned || who.IsTripped) score += AiTuning.TagHelplessBonus;

                // How far inside the chalk they are. `IsTaggable` is a yes or no and cannot tell
                // a step past the line from a stand over the lata; the one with further to run
                // back out is the one this chase can actually catch.
                float depth = Balance.ConfinementRadius
                              - Mathf.Max(Mathf.Abs(who.transform.position.x),
                                          Mathf.Abs(who.transform.position.z));
                if (depth > 0.0f) score += AiTuning.TagDepthWeight * depth;

                // ⚠️ OFF THE OBSERVED POSITION, NOT THE TRUE ONE. Every other read of a rival in
                // this file goes through `At`, which is this bot's belief lagged by its own
                // reaction time. A selector that read the truth would pick targets off
                // information the body it is steering has not been given yet.
                score -= AiTuning.TagDistanceWeight * Flat(transform.position, At(who));

                if (who == _lastTagTarget) score += AiTuning.TagSwitchMargin;

                if (score <= bestScore) continue;

                bestScore = score;
                best = who;
            }

            _lastTagTarget = best;
            return best;
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
            if (Plan != AiPlan.Windup) intent.SpinInput = 0.0f;

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
            // ⚠️ AND THE ANTI-STALL CLOCK IS A REASON TO SPRINT. The other two conditions ask
            // whether the run is dangerous or long; this one asks whether it is already late.
            // A bot that walks the last stretch while the fine is ticking is spending points to
            // save stamina it has no other use for.
            var round = GameServices.Round;
            bool late = round != null
                        && round.AttackerIdleSeconds(_motor.PlayerSlot)
                           >= Balance.SlipperUnretrievedWarningTime * 0.5f;

            bool hurry = distance > AiTuning.Reach
                         && (late || MineIsExposed(mine) || distance > AiTuning.SprintDistance);

            Goto(intent, where, AiTuning.Reach * 0.75f, hurry);

            // ⚠️ THE PICKUP IS A TAP AND A HELD BUTTON WOULD DO NOTHING AT ALL. The carrier
            // reads `JustPressed`, so holding produces exactly one edge in a lifetime and then
            // a bot that stands on its own slipper for the rest of the round. Tap alternates,
            // so an edge lands every other frame for as long as it is in range.
            //
            // ⚠️⚠️ THE RANGE IS `Balance.PickupRadius`, NOT `AiTuning.Reach`, AND THE DIFFERENCE
            // WAS A BAND WHERE A BOT COULD GRAB AND WOULD NOT TRY. `Reach` is 1.15 m, a generic
            // melee reach shared with the shove and the punch; `Slipper.CanBeGrabbedBy` measures
            // `PickupRadius`, 1.40 m. So between 1.15 and 1.40 m the pickup was legal, the bot
            // knew where the slipper was, its plan was Fetch, and it pressed nothing. `Goto`
            // above stops it at 0.86 m, but a bot is jostled, shoved and knocked back
            // constantly, and any drift into that 0.25 m band left it standing next to its own
            // ammunition doing nothing.
            //
            // `docs/TODO.md` § 6 preserved one diagnostic line as the lead for this:
            // `own=3 plan=Fetch ownerAct=True d3=1.10 grabbable=True`, a bot 1.10 m from a
            // grabbable slipper it had already decided to fetch, still not holding it. 1.10 is
            // inside `Reach`, so that particular frame is not this bug, but reading the two
            // constants side by side to check is what found it.
            //
            // ⚠️ IT READS THE RULE'S OWN CONSTANT rather than a copy, so a retune of the pickup
            // radius cannot leave the AI reaching for the old one.
            if (distance <= Balance.PickupRadius) Tap(intent, Verb.Grab);
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
                _windupSpin = 0.0f;
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

            Vector3 target = aim + _windupScatter * settle;
            Vector3 origin = _carrier.ThrowOrigin();
            _windupSpin = ChoosePektusSpin(origin, target, _windupPower);
            intent.AimPoint = CompensatedPektusAim(origin, target, _windupPower, _windupSpin);
            intent.SpinInput = _windupSpin;

            float power = _carrier.ChargeRatio;
            Press(intent, Verb.SpecialAbility, true);

            if (_windupTime >= AiTuning.WindupTimeout)
            {
                // Out of patience. Throw what we have: it may fall short, and a bot that lets
                // go is still a bot playing the game.
                ReleaseThrow(intent);
                return;
            }

            // ⚠️⚠️ THE WHISTLE, AND NOTHING IN THIS FILE HAD EVER READ THE ROUND CLOCK. Every
            // patience bound above is measured in seconds SINCE THE CHARGE STARTED, and not one
            // of them knows how many seconds the round has left. A bot that began a wind-up with
            // four seconds on the clock waited out `AimSettle`, waited out `LanePatience`, and
            // was still holding the button when the round ended: the charge is discarded, the
            // slipper stays in its hand, and the shot it spent the whole end of the round lining
            // up scored nothing at all.
            //
            // ⚠️ A HELD THROW IS WORTH ZERO AND A LOOSE ONE IS NOT. Even a poor shot can knock
            // the lata, and even a miss lands a tsinelas somewhere the next round starts from.
            // There is no case in which holding it past the whistle is better than releasing.
            //
            // ⚠️ THE MARGIN IS THE BUFFER PLUS A FRAME, NOT A ROUND NUMBER. `HeroAbilitySystem
            // .InputBufferWindow` is 0.30 s and the throw resolves on the physics step after the
            // release, so anything under that is a release the round ends before it can answer.
            if (round.RoundActive && round.TimeLeft <= LastCallSeconds)
            {
                ReleaseThrow(intent);
                return;
            }

            if (power < _windupPower) return;

            float minHold = Me.AimSettle < 90.0f
                ? Mathf.Min(Me.AimSettle, Balance.ChargeFullTime) * AiTuning.WindupMinHoldShare
                : 0.0f;

            if (_windupTime < minHold) return;

            // Charged and committed. The only question left is whether the lane is open.
            if (!_blundering && LaneBlockedWithSpin(origin, intent.AimPoint, target, power, _windupSpin))
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
                // Clear the hysteresis ring with a real movement margin. Stopping exactly
                // on the clear radius still counts as camping by the tournament rule.
                Vector3 safeGuard = lata.transform.position + Vector3.forward
                    * (Balance.TayaCampClearRadius + 0.35f);
                Goto(intent, ClampToBox(safeGuard), AiTuning.ArriveSlop, false);
                return;
            }

            // Stand BETWEEN the lata and the threat, dynamically outside the camping penalty ring!
            Vector3 toward = At(threat) - lata.transform.position;
            toward.y = 0.0f;

            if (toward.magnitude < 0.05f) toward = Vector3.forward;

            float guardRadius = Mathf.Max(AiTuning.GuardRadius,
                Balance.TayaCampClearRadius + 0.35f);
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
        /// <param name="pausesOnTurn">False for the loiter shuffle. ⚠️ A LOITER STEP IS
        /// ALREADY A SHORT BEAT WITH ITS OWN REST BEHIND IT (`LoiterStepMin` 0.07 s against
        /// `KeyChangeBeatSeconds` 0.12), so charging a key change beat in front of one would
        /// swallow the step whole and the shuffle would stop happening at all. Every step that
        /// is going somewhere pays it.</param>
        private void Drive(InputIntent intent, Vector3 direction, bool sprint,
                           bool pausesOnTurn = true)
        {
            Vector3 flat = new Vector3(direction.x, 0.0f, direction.z);

            if (flat.magnitude < 0.001f) { Stop(intent); return; }

            flat = flat.normalized;
            _driving = true;

            flat = AvoidHazards(flat);

            // ⚠️ NINETY DEGREES OFF THE WANTED HEADING WHILE UNSTICKING. Enough to clear a
            // corner, and it still makes progress ALONG the obstacle rather than backing away
            // from it — backing off just walks into the same corner again a second later.
            if (_unstickLeft > 0.0f)
                flat = new Vector3(-flat.z * _unstickSign, 0.0f, flat.x * _unstickSign);

            Vector2 committed = CommitHeading(EightWay(flat), pausesOnTurn);

            // ⚠️⚠️ THE HAND BETWEEN TWO KEYS. `CommitHeading` starts this beat when it accepts
            // a heading more than `AiTuning.KeyChangeBeatDeg` off the last one, and for its length
            // the bot holds nothing at all. See § THE KEY CHANGE BEAT.
            //
            // ⚠️ `_driving` GOES BACK OFF, AND THAT IS NOT A DETAIL. `StepUnstick` reads it as
            // "this bot asked to move and did not", so leaving it true through a deliberate pause
            // would accrue stuck time on a bot standing still on purpose and fire an unstick
            // sidestep out of nowhere every time one turned round.
            if (_keyGapLeft > 0.0f)
            {
                _driving = false;
                _sprintAsked = false;
                Stop(intent);
                return;
            }

            intent.Move = committed;

            // ⚠️ THE STAMINA QUESTION AND THE KEY QUESTION ARE SEPARATE. `MaySprint` answers
            // whether this bot is willing to spend the bar; `SprintKeyDown` answers whether its
            // hand is on the key this instant. Only the first one used to exist, which is why a
            // bot ran until it fatigued and then limped, every single crossing.
            bool wantsToRun = sprint && MaySprint();
            _sprintAsked = wantsToRun;

            intent.Set(Verb.Sprint, wantsToRun && SprintKeyDown());
        }

        // -------------------------------------------------------------------
        // § THE HEADING COMMIT
        //
        // ⚠️⚠️ WHY A BOT LOOKED LIKE IT WAS SHAKING ITS HEAD. `EightWay` snaps a wanted heading
        // onto one of eight compass directions, and the planner reruns every single frame. A
        // wanted direction that happens to sit near an octant boundary therefore alternates
        // between two neighbours frame after frame, and `CharacterMotor.Steer` then pointed the
        // BODY at whichever one won that frame. 🧑 2026-08-27: *"moving and looking back and
        // forth unnaturally, like who does that"*.
        //
        // ⚠️⚠️ IT IS A COMMIT, NOT A SMOOTH. Averaging the two neighbours would give a heading
        // between two octants, which is a direction no keyboard can produce, and `CLAUDE.md` § 4
        // is explicit that *"a bot presses the same buttons a human does"*. Committing keeps the
        // output on the eight and simply refuses to change it for `HeadingCommitSeconds`, which
        // is what a player does: press a key, hold it, then press a different one.
        //
        // ⚠️ THE BREAK CLAUSE IS WHAT KEEPS IT FROM BEING A BUG. A bot that has been shoved, or
        // whose target has run past it, must be able to abandon a committed heading rather than
        // walking it out. A neighbouring octant is 45° and the break is 90°, so the boundary
        // flapping this exists to absorb can never trip it.
        // -------------------------------------------------------------------

        private Vector2 _committedMove;
        private float _headingCommitLeft;

        private Vector2 CommitHeading(Vector2 wanted, bool pausesOnTurn)
        {
            if (wanted.sqrMagnitude < 0.0001f)
            {
                _headingCommitLeft = 0.0f;
                _committedMove = Vector2.zero;
                return wanted;
            }

            if (_headingCommitLeft > 0.0f && _committedMove.sqrMagnitude > 0.0001f)
            {
                float turn = Vector2.Angle(_committedMove, wanted);
                if (turn < AiTuning.HeadingBreakDeg) return _committedMove;
            }

            // ⚠️⚠️ THE KEY CHANGE BEAT IS CHARGED HERE BECAUSE THIS IS THE ONLY PLACE THAT
            // KNOWS A HEADING WAS ACTUALLY CHANGED. `Drive` runs every frame and mostly re-reads
            // the same committed answer back out of the line above; a commit that is ACCEPTED is
            // the hand moving to a different key, and that is the thing worth a pause.
            if (pausesOnTurn && _committedMove.sqrMagnitude > 0.0001f
                && Vector2.Angle(_committedMove, wanted) >= AiTuning.KeyChangeBeatDeg)
                _keyGapLeft = AiTuning.KeyChangeBeatSeconds;

            _committedMove = wanted;
            _headingCommitLeft = AiTuning.HeadingCommitSeconds;
            return wanted;
        }

        // -------------------------------------------------------------------
        // § THE SPRINT KEY
        //
        // ⚠️⚠️ IT WAS A STATE AND A PLAYER'S IS AN ACT. `MaySprint` asks the stamina bar a
        // question, so a bot held the key from the moment it was far from something until the bar
        // bottomed out, walked at 0.75 speed through two seconds of fatigue, and did it again.
        // Nobody plays like that, and the fatigue was self-inflicted every crossing.
        // `AiTuning.SprintBurstMin` carries the measurement.
        // -------------------------------------------------------------------

        private void StepSprintKey(float dt)
        {
            if (_sprintBurstLeft > 0.0f)
            {
                _sprintBurstLeft -= dt;

                if (_sprintBurstLeft <= 0.0f)
                {
                    _sprintBurstLeft = 0.0f;
                    _sprintRestLeft = UnityEngine.Random.Range(AiTuning.SprintRestMin,
                                                               AiTuning.SprintRestMax);
                }
            }
            else if (_sprintRestLeft > 0.0f)
            {
                _sprintRestLeft = Mathf.Max(0.0f, _sprintRestLeft - dt);
            }

            // ⚠️ THE WANT IS FED BY THE PREVIOUS FRAME'S `Drive`, which is a frame of lag on a
            // 0.15 s accumulator and is why this can live at the top of `Update` with the other
            // clocks rather than being threaded through every mover.
            _sprintWantHeld = _sprintAsked ? _sprintWantHeld + dt : 0.0f;
            _sprintAsked = false;
        }

        private bool SprintKeyDown()
        {
            if (_sprintRestLeft > 0.0f) return false;
            if (_sprintBurstLeft > 0.0f) return true;

            // ⚠️ A PERSON SETS OFF AND THEN COMMITS. Reaching top speed on the frame the
            // destination was chosen is the machine-like thing § 31.1 did not cover.
            if (_sprintWantHeld < AiTuning.SprintCommitDelay) return false;

            _sprintBurstLeft = UnityEngine.Random.Range(AiTuning.SprintBurstMin,
                                                        AiTuning.SprintBurstMax);
            return true;
        }

        private void Stop(InputIntent intent)
        {
            intent.Move = Vector2.zero;
            intent.Set(Verb.Sprint, false);
        }

        /// <summary>
        /// Bend a heading around whatever hero hazard is sitting on it.
        ///
        /// ⚠⚠ THIS IS THE FIX FOR THE HERO STRIKE PENALTY VARIANCE. `BotBehaviourProbe`
        /// measured unretrieved-slipper penalties swinging from 0 to 28 across identical Hero
        /// Strike runs while Classic held a flat 0, and the planner was never at fault: the
        /// attacker decided correctly to fetch, `Drive` pointed it at the slipper in a straight
        /// line, and the line ran through a Permafrost Sheet or a Seance Void. It was slowed,
        /// slipped or pulled off course, arrived late or never, and was billed 5 points a second
        /// for a slipper it was on its way to collect. How often a hazard happened to land
        /// between a bot and its tsinelas is the entire variance.
        ///
        /// ⚠️ IT STEERS THE HEADING, IT DOES NOT REPLACE THE PLAN. The plan still says where
        /// to go and why; this only changes the walk there, one frame at a time, so nothing in
        /// the decision layer has to know hazards exist.
        ///
        /// ⚠️ IT GIVES UP CLOSE TO THE GOAL, and that clause is not an optimisation. A
        /// slipper that lands INSIDE a hazard is still a slipper that has to be fetched; without
        /// the give-up the blocker is unavoidable by construction and the bot orbits it for the
        /// rest of the round while the penalty clock runs. Slow ground beats never arriving.
        /// </summary>
        private Vector3 AvoidHazards(Vector3 heading)
        {
            if (Abilities.HazardMap.Count == 0) return heading;

            Vector3 here = transform.position;

            // The goal is what the plan is walking to. Steering is only meaningful against a
            // destination; a bot with no goal is loitering and has nothing to path around.
            Vector3 target = _goal;
            Vector3 toGoal = target - here;
            toGoal.y = 0.0f;

            if (toGoal.sqrMagnitude < 0.0001f) return heading;
            if (toGoal.magnitude <= AiTuning.HazardAvoidGiveUp) return heading;

            // ⚠⚠ ONLY WHEN THIS DRIVE IS ACTUALLY THE WALK TO THE GOAL, AND MISSING THIS
            // CHECK COST A WHOLE MATCH. `Drive` is not only called with "head for the goal":
            // `Loiter` drives back along its leash, the unstick drives ninety degrees off, and
            // separation pushes away from a body. Bending every one of those toward a steer
            // computed against `_goal` overrides a deliberate direction with an unrelated one.
            // Measured in `BotBehaviourProbe`: Hero Strike fell to 15 throws and 645 idle
            // penalties in four rounds, with one seat travelling 27 m in the entire match,
            // because its recovery moves were being rewritten into a walk it had not asked for.
            Vector3 goalDir = toGoal.normalized;
            if (Vector3.Dot(heading, goalDir) < 0.7f) return heading;

            if (!Abilities.HazardMap.TryFindBlocker(here, target, _motor.PlayerSlot,
                                                    AiTuning.HazardAvoidMargin,
                                                    AiTuning.HazardAvoidMaxRadius, out var blocker))
                return heading;

            Vector3 steer = Abilities.HazardMap.SteerAround(here, target, blocker,
                                                            AiTuning.HazardAvoidMargin);

            return steer.sqrMagnitude > 0.0001f ? steer.normalized : heading;
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

                    // ⚠️⚠️ A REST IS WHERE A GLANCE GOES, because it is the only part of a
                    // loiter that is not already a step. A movement-aimed body can only look by
                    // walking (`AiTuning.GlanceChance` has the whole reason), so this rolls a
                    // point worth watching and the branch at the bottom presses toward it for
                    // `GlanceSeconds` instead of standing at attention.
                    // ⚠️⚠️ AN OUT PARAMETER, NOT A `Vector3.zero` SENTINEL, AND THE ARENA IS WHY.
                    // The lata stands at the centre of the box, which is the world origin, so
                    // "zero means nothing to look at" would silently throw away the single most
                    // likely thing a bot in this game wants to watch. The first draft had exactly
                    // that and it would never have looked at the can once.
                    if (UnityEngine.Random.value < AiTuning.GlanceChance
                        && TryGlanceAt(out _glanceAt))
                        _glanceLeft = AiTuning.GlanceSeconds;

                    // Occasional friendly emote during calm loitering
                    if (UnityEngine.Random.value < 0.15f)
                    {
                        string[] emotes = { "yes", "dance", "tpose", "bow", "crouch" };
                        TryTriggerEmote(emotes[UnityEngine.Random.Range(0, emotes.Length)], 0.9f);
                    }
                }
            }

            if (Mathf.Approximately(_loiterDir, 0.0f))
            {
                // ⚠️ LEASHED LIKE EVERY OTHER LOITER STEP. The branch above this one pulls the
                // body back the moment it passes `LoiterLeash`, so a glance cannot walk a bot off
                // the mark its plan put it on however the roll comes out.
                if (_glanceLeft > 0.0f)
                {
                    Vector3 look = _glanceAt - here;
                    look.y = 0.0f;

                    if (look.sqrMagnitude > 0.0001f) { Drive(intent, look, false, false); return; }
                }

                Stop(intent);
                return;
            }

            var lata = GameServices.Round?.Lata;
            Vector3 pivot = lata != null ? lata.transform.position : Vector3.zero;

            Vector3 radial = here - pivot;
            radial.y = 0.0f;
            if (radial.magnitude < 0.05f) radial = Vector3.forward;
            radial = radial.normalized;

            // Across the bearing out from the lata, so the shift never walks into or away from
            // the thing this bot is lined up on.
            Drive(intent, new Vector3(-radial.z, 0.0f, radial.x) * _loiterDir, false, false);
        }

        /// <summary>
        /// Something worth turning to look at while there is nothing to do, or false.
        ///
        /// ⚠️⚠️ IT READS STATE, IT NEVER CALLS A SELECTOR. `LiveThreat` and `TagTarget` both
        /// WRITE their anti-fixation memory as a side effect of being asked, so calling either of
        /// them for something as incidental as where a bot is looking would let the glance quietly
        /// re-decide who the taya is guarding. `_lastThreat` is the answer that selector already
        /// reached, and `DefenderOf` is a pure read.
        ///
        /// ⚠️ AND THE PICK IS ROLLED PER GLANCE, so four bots resting together do not all turn
        /// the same way at the same moment, which is the clockwork `LoiterLeash`'s own note is
        /// about.
        /// </summary>
        private bool TryGlanceAt(out Vector3 point)
        {
            point = Vector3.zero;

            var round = GameServices.Round;
            if (round == null) return false;

            int pick = UnityEngine.Random.Range(0, 3);

            if (pick == 0)
            {
                var lata = round.Lata;
                if (lata != null) { point = lata.transform.position; return true; }
            }

            if (pick == 1)
            {
                var who = _motor.IsDefender ? _lastThreat : DefenderOf(round);
                if (who != null) { point = At(who); return true; }
            }

            Slipper nearest = null;
            float best = float.MaxValue;

            foreach (var s in FindObjectsByType<Slipper>(FindObjectsInactive.Exclude))
            {
                if (s.State != SlipperState.Loose && s.State != SlipperState.InFlight) continue;

                float d = Flat(transform.position, s.transform.position);
                if (d >= best) continue;

                best = d;
                nearest = s;
            }

            if (nearest == null) return false;

            point = nearest.transform.position;
            return true;
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

                // A loose slipper near the lata must not lure the defender back into a
                // penalized can camp. Cover its approach from outside the clear radius.
                Vector3 fromCan = candidate - lata.transform.position;
                fromCan.y = 0.0f;
                float safeRadius = Balance.TayaCampClearRadius + 0.25f;
                if (fromCan.magnitude < safeRadius)
                {
                    if (fromCan.sqrMagnitude < 0.01f) fromCan = toward;
                    candidate = lata.transform.position + fromCan.normalized * safeRadius;
                }

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

        /// <summary>
        /// The get-up / break-free mash toggle.
        ///
        /// ⚠️⚠️ IT IS A FIELD BECAUSE EVERY OTHER PIECE OF PRESS STATE ON THAT PATH IS WIPED
        /// EVERY FRAME. `Update` calls `ReleaseAll` before it mashes, and that clears both
        /// `InputIntent._held` and `_pressed`, so anything derived from either answers the same
        /// thing on every frame and the alternation never happens. See the long note at the call
        /// site for what that cost.
        /// </summary>
        private bool _mashHeld;

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

        // -------------------------------------------------------------------
        // § HOLDING A HOLD-TO-AIM POWER
        //
        // ⚠️⚠️ `Tap` ALTERNATES ON AND OFF EVERY FRAME, SO A BOT'S HOLD IS ONE FRAME LONG. That
        // is exactly right for the eight verbs that read `JustPressed`, and it is silently wrong
        // for `HeroAbility.HoldToAim`: the press and the release land a sixtieth of a second
        // apart, so `AimRangeFor` returns the MINIMUM every time and every bot Phaister blinks
        // 2.0 m for the rest of the game. Nothing errors and nothing looks broken; the ability
        // simply has one length for three of the four seats.
        //
        // ⚠️ WHICH WOULD BREAK `CLAUDE.md` § 4's *"a bot presses the same buttons a human
        // does"*. It is not a rule about the API, it is a rule about there being no second path:
        // an ability whose interesting half only a human can reach is an ability the probes
        // cannot measure and the bots cannot demonstrate. So a bot holds the key, for as long as
        // holding is worth anything, and lets go.
        //
        // ⚠️ IT HOLDS FOR THE RAMP, NOT FOR THE CEILING. `AimRampSeconds` is the point past which
        // the reach stops growing; holding to `MaxAimSeconds` would buy the bot nothing and would
        // make it stand there with a key down for twice as long, which is the behaviour
        // `docs/VISION.md` § 4 forbids the ability from rewarding in the first place.
        // -------------------------------------------------------------------

        /// <summary>How long each hold-to-aim verb has been down, or -1 when it is not.</summary>
        private readonly Dictionary<Verb, float> _aimHeld = new Dictionary<Verb, float>();

        private void HoldAim(InputIntent intent, Verb verb, Abilities.HeroAbility ability, float dt)
        {
            if (ability == null || !ability.HoldToAim)
            {
                Tap(intent, verb);
                return;
            }

            float held = _aimHeld.TryGetValue(verb, out float h) ? h : -1.0f;

            if (held < 0.0f)
            {
                _aimHeld[verb] = 0.0f;
                Press(intent, verb, true);
                return;
            }

            held += dt;

            if (held >= ability.AimRampSeconds)
            {
                _aimHeld[verb] = -1.0f;
                Press(intent, verb, false);   // the release IS the cast
                return;
            }

            _aimHeld[verb] = held;
            Press(intent, verb, true);
        }

        // -------------------------------------------------------------------
        // § WOULD THIS CAST DO ANYTHING
        //
        // ⚠️⚠️ 🧑 2026-08-27: *"they also dont seem smart with using their skills"*. § 31.7 read
        // the first half of that sentence as spam and fixed the spam with a cadence. This is the
        // second half and it is a different fault: every branch below gated its cast on ONE
        // distance to ONE target, and a distance is not a question about what the cast achieves.
        // Nothing asked whether the footprint would land on anybody, whether that ground was
        // already covered, whether the victim was already on the floor, or whether the buff being
        // spent was already running.
        //
        // ⚠️⚠️ AND THE HAND-PICKED DISTANCES WERE MOSTLY WRONG. Zack's Thunderstrike puts a 4.5 m
        // circle ON ZACK and was cast at a target up to 8.0 m away, so a correct cast by the old
        // rule usually caught nobody at all. Dante's stomp is 2.2 m and was cast at 5.0. His
        // fissure is 4.5 m centred 2.2 m AHEAD and was cast at 9.0 m in any direction, including
        // behind him. Nemu's void was gated on a hand-written 4.5 m offset and a 7.5 m radius,
        // both of them the pre-footprint-pass numbers: it has been 3.5 m and 2.8 m since § 8.
        //
        // ⚠️ SO THE GATE IS THE ABILITY'S OWN TELEGRAPH, NOT A NEW TABLE. `TelegraphRadius` and
        // `TelegraphRange` already say where a power lands and how wide it is, they are already
        // asserted against what `OnActivate` actually spawns
        // (`TelegraphsMatchWhatTheAbilityActuallyPlaces`), and they are already the ring the
        // PLAYER is shown. A bot aiming at the ring the player sees needs no second set of
        // numbers, and a new hero cannot ship with a wrong one here because it does not have one
        // here to get wrong.
        //
        // ⚠️⚠️ THE ONE THING A TELEGRAPH DOES NOT MEAN IS "PAYLOAD". Phaister's blink telegraphs
        // its ARRIVAL MARK, 1.15 m at up to 5.5 m, which is where she will be standing and not an
        // area of effect; Sean's Ignition and Zack's Overcharge telegraph nothing because they
        // change the next throw. Those three are judged on state below and never through
        // `WouldCatch`.
        // -------------------------------------------------------------------

        /// <summary>
        /// Where this power's footprint would land, read off its own telegraph.
        ///
        /// ⚠️ IT MUST STAY THE SAME EXPRESSION AS `HeroAbilitySystem.TelegraphCentre`, which is
        /// what DRAWS the ring for the player. No number is duplicated between them (both read
        /// `TelegraphRange` off the ability), so this is not the drift `Design.md` warns about,
        /// but a bot aiming somewhere the ring is not drawn would be a bot playing a different
        /// game from the one on screen.
        /// </summary>
        private Vector3 FootprintOf(Abilities.HeroAbility ability)
            => transform.position + transform.forward * ability.TelegraphRange;

        /// <summary>
        /// How many other bodies a circle of this size, here, would come down on.
        ///
        /// ⚠️⚠️ `stunPayload` IS NOT A STYLE CHOICE, IT IS `CLAUDE.md` § 4's *"stuns overlap via
        /// Max(), never additively"* READ FROM THE OTHER SIDE. A freeze laid on somebody already
        /// frozen extends nothing and buys nothing, so for a power whose whole payload is a stun,
        /// a helpless body in the circle is not a victim. A power that SHOVES or LAUNCHES has no
        /// such rule: moving a stunned body is one of the better things you can do with one, so
        /// those count everybody.
        /// </summary>
        private int VictimsUnder(Vector3 centre, float radius, bool stunPayload)
        {
            var round = GameServices.Round;
            if (round == null) return 0;

            int found = 0;

            foreach (var who in round.Players)
            {
                if (who == null || who == _motor || !who.RoundActive) continue;
                if (stunPayload && (who.IsStunned || who.IsTripped)) continue;
                if (Flat(centre, At(who)) <= radius) found++;
            }

            return found;
        }

        /// <summary>Would this power's own footprint, cast right now, land on anybody?</summary>
        private bool WouldCatch(Abilities.HeroAbility ability, bool stunPayload)
        {
            if (ability == null || !ability.HasTelegraph) return false;

            return VictimsUnder(FootprintOf(ability),
                                ability.TelegraphRadius + AiTuning.AbilityVictimMargin,
                                stunPayload) > 0;
        }

        /// <summary>
        /// Is this ground worth denying, and is it not already denied?
        ///
        /// ⚠️⚠️ THE SECOND HALF IS THE ONE THAT WAS MISSING AND IT COSTS TWICE. A second frost
        /// sheet laid on the first denies no ground that was not already denied, spends a 46 to
        /// 62 s cooldown for it, and stacks two translucent plates in one place, which is the
        /// pile-up `docs/VISION.md` § 2 rule 4 forbids and § 19 records shipping a wrong colour
        /// out of. A bot doing it is playing badly AND making the arena harder to read.
        ///
        /// ⚠️ AND "WORTH" IS THE GAME'S OWN GEOMETRY, NOT A GUESS AT INTENT. Ground is worth
        /// denying when somebody is on it, when a loose tsinelas is on it because the retrieval
        /// run has to cross it, or when the lata is on it because every attacker has to come
        /// there. Those three are the only places this game forces a body to walk, so they are
        /// the only places a hazard is better than empty road.
        /// </summary>
        private bool WorthDenying(Abilities.HeroAbility ability)
        {
            if (ability == null || !ability.HasTelegraph) return false;

            Vector3 where = FootprintOf(ability);

            if (Abilities.HazardMap.AnyCentredNear(
                    where, ability.TelegraphRadius * AiTuning.AbilityDenialOverlap))
                return false;

            float reach = ability.TelegraphRadius + AiTuning.AbilityVictimMargin;

            if (VictimsUnder(where, reach, stunPayload: false) > 0) return true;

            var round = GameServices.Round;
            var lata = round?.Lata;
            if (lata != null && Flat(where, lata.transform.position) <= reach) return true;

            foreach (var s in FindObjectsByType<Slipper>(FindObjectsInactive.Exclude))
                if (s.State == SlipperState.Loose
                    && Flat(where, s.transform.position) <= reach) return true;

            return false;
        }

        /// <summary>
        /// Is there a tsinelas on the ground inside the chalk that somebody still has to fetch?
        ///
        /// ⚠️ IT IS THE TEST FOR "IS THERE A RETRIEVAL LEFT TO DENY", which is what a defensive
        /// zone is actually worth. Outside the box is not the taya's problem and nobody has to
        /// walk into danger for it; a slipper in somebody's hand has already been retrieved.
        /// </summary>
        private static bool AnyLooseSlipperInsideTheBox()
        {
            foreach (var s in FindObjectsByType<Slipper>(FindObjectsInactive.Exclude))
            {
                if (s.State != SlipperState.Loose) continue;

                if (Confinement.IsInsideBox(s.transform.position.x, s.transform.position.z))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Is there enough journey left to be worth a mobility power?
        ///
        /// ⚠️ A DASH THAT ARRIVES WHERE YOU ALREADY ARE IS A COOLDOWN SPENT ON NOTHING. Zack's
        /// rail grind and Sean's burn dash were gated on `_driving` alone, which is true on the
        /// last stride of a two-metre walk.
        /// </summary>
        private bool WorthTravelling()
            => _driving && Flat(transform.position, _goal) >= AiTuning.AbilityTravelWorthwhile;

        /// <summary>
        /// May this slot be pressed at all, before anything about the board is considered?
        ///
        /// ⚠️⚠️ A POWER THAT IS STILL RUNNING MUST NOT BE RECAST, AND NOTHING CHECKED THAT.
        /// `IsReady` only answers the cooldown or the charge count, so a charge ability with a
        /// duration (Dante's carapace, Nemu's phase, Zack's grind, Sean's dash) could be spent
        /// again on top of itself and buy nothing but a shorter meter.
        ///
        /// ⚠️ THE ONE EXCEPTION IS REAL AND IS THE POINT OF ITS ABILITY. Nemu's poltergeist is
        /// the game's only `CanReactivate` power: the second press is *"press again to follow him
        /// there"*, so refusing a press while it is active would delete half of it.
        /// </summary>
        private static bool SlotIsSpendable(Abilities.HeroAbility ability)
            => ability != null && ability.IsReady && (!ability.IsActive || ability.CanReactivate);

        private void StepHeroAbilities(InputIntent intent, float dt)
        {
            // ⚠️ IT RETURNS WITHOUT RELEASING, WHICH IS THE WHOLE POINT. `ReleaseUntouchedHero
            // Buttons` writes `false` into every hero key this controller did not touch, and
            // during a possession the player is holding one of them. See `AbilitiesEnabled`.
            if (!AbilitiesEnabled) return;

            if (UI.SceneFlow.SelectedMode != GameMode.HeroStrike)
            {
                ReleaseUntouchedHeroButtons(intent);
                return;
            }

            var abilitySystem = _motor.AbilitySystem;
            if (abilitySystem == null || abilitySystem.Kit == null)
            {
                ReleaseUntouchedHeroButtons(intent);
                return;
            }

            var kit = abilitySystem.Kit;
            var round = GameServices.Round;
            if (round == null || !round.RoundActive)
            {
                // ⚠️ THE OPENING CLOCK RESTARTS WITH THE ROUND, NOT WITH THE MATCH. Every round
                // begins with four seats stood around one lata, so every round needs the same
                // scatter before a distance gate below means anything.
                _roundLiveFor = 0.0f;
                ForgetWhatWasBeingWeighed();
                ReleaseUntouchedHeroButtons(intent);
                return;
            }

            // -------------------------------------------------------------------
            // § THE CADENCE GATE
            //
            // ⚠️⚠️ 🧑 2026-08-27: *"make sure ai doesnt just spam them all at the start"*, in the
            // same breath as *"im not sure if they even have proper ai logic for when to use
            // skills"*. There is logic: every branch below gates its cast on a distance to the
            // correct target, and the branches are per hero. The problem is that **all of those
            // gates are satisfied simultaneously at a round boundary**, because the seats spawn
            // around one lata inside a 14 m box. At t = 0 a Dante is inside 5.0 m, a Zack inside
            // 8.0 and a Phaister inside 8.5, so the ultimate, skill 1 and skill 2 all fired on
            // the first live frame, for all four seats at once.
            //
            // ⚠️ SO THE FIX IS NOT MORE CONDITIONS ON EACH BRANCH, IT IS SPACING BETWEEN THEM.
            // Tightening the distances would only move the pile-up; what was missing is that a
            // bot had no notion of having just done something. `AbilityCadenceSeconds` is one
            // clock for all three slots, which is what makes it a cadence rather than a second
            // cooldown: per-slot spacing would still allow a whole kit on one frame.
            //
            // ⚠️⚠️ AND AN IN-PROGRESS HOLD IS EXEMPT, WHICH IS NOT A LOOPHOLE. Phaister's blink
            // is the game's one hold-to-aim power and a bot holds the key across frames (see
            // § HOLDING A HOLD-TO-AIM POWER). Closing the gate mid-hold would release the key
            // early and pin every bot blink to the minimum range, which is the exact fault that
            // section exists to prevent, reintroduced from a different direction.
            // -------------------------------------------------------------------
            _roundLiveFor += dt;
            if (_abilityCadenceLeft > 0.0f) _abilityCadenceLeft -= dt;

            // ⚠️ A RELEASED HOLD LEAVES ITS KEY IN THE TABLE WITH -1 IN IT (`HoldAim` writes
            // -1.0 rather than removing the entry), so `_aimHeld.Count` latches true after the
            // first blink of the match and would have held this gate open for the rest of it.
            bool holding = false;
            foreach (var held in _aimHeld.Values)
            {
                if (held >= 0.0f) { holding = true; break; }
            }

            // ⚠️⚠️ THE OPENING GATE IS PER SEAT NOW, NOT ONE CONSTANT FOUR BOTS SHARE. See
            // `AiTuning.AbilityOpeningJitterSeconds`: a single number means all four unlock on
            // the same frame, so the 2.5 s delay turned a frame-one dump into a frame-150 dump
            // and 🧑 reported the identical feeling a build later. `Patience` is rolled off the
            // SEAT, so the spread is the same on every run and the probe still compares.
            float openAt = AiTuning.AbilityOpeningDelaySeconds
                           + _self.Patience * AiTuning.AbilityOpeningJitterSeconds;

            // ⚠️ ABOVE THE GATE, BECAUSE IT MEASURES HOW LONG THE ULTIMATE HAS BEEN READY AND NOT
            // HOW LONG THIS BOT HAS BEEN ALLOWED TO CAST. Ticking it below would stop the clock
            // for every cadence window, so `UltimateHoldSeconds` would mean a different amount of
            // real time depending on how busy the bot's kit was. See § IS THE ULTIMATE WORTH
            // SPENDING YET.
            _ultimateReadyFor = kit.IsUltimateReady ? _ultimateReadyFor + dt : 0.0f;

            bool mayOpen = _roundLiveFor >= openAt && _abilityCadenceLeft <= 0.0f;

            if (!mayOpen && !holding)
            {
                // ⚠️ A CLOSED GATE FORGETS WHATEVER IT WAS WEIGHING. Carrying a conviction window
                // across the gate would mean a bot arrives at the opening with a decision already
                // made about a board that has moved, which is the thing `Consider` exists to
                // prevent one layer down.
                ForgetWhatWasBeingWeighed();
                ReleaseUntouchedHeroButtons(intent);
                return;
            }

            _weighedThisFrame = false;

            Vector3 myPos = transform.position;
            CharacterMotor target = _motor.IsDefender ? TagTarget() : DefenderOf(round);

            // ⚠️ OFF THE OBSERVED POSITION, NOT THE TRUE ONE. This read `target.transform.position`
            // until 2026-08-27, which is the one place in the ability layer that saw through
            // `Observe`'s reaction lag: a bot answered a rival's step on the frame it happened
            // while every other decision it made about the same body was `Me.React` behind. That
            // is a power cast faster than a hand can move, and it is the kind of thing a player
            // reads as the bots cheating rather than as the bots being good.
            float targetDistance = target != null ? Flat(myPos, At(target)) : float.MaxValue;
            var lata = round.Lata;
            float lataDistance = lata != null
                ? Flat(myPos, lata.transform.position)
                : float.MaxValue;

            // ⚠️⚠️ THE ULTIMATE IS THE MOST EXPENSIVE THING A BOT OWNS AND IT WAS THE LOOSEST
            // GATED. Every branch here now asks the ability where its own circle lands and
            // whether anybody is standing there. See § WOULD THIS CAST DO ANYTHING for the three
            // kits whose hand-picked distance made a correct cast miss by construction.
            // -------------------------------------------------------------------
            // § IS THE ULTIMATE WORTH SPENDING YET
            //
            // ⚠️⚠️ THE BRANCHES BELOW ASK WHETHER A CAST WOULD LAND. THIS ASKS WHETHER IT IS WORTH
            // THE METER, WHICH NOTHING USED TO ASK AT ALL. See `AiTuning.UltimateWantsVictims`: a
            // bot spent its most expensive power on the first single body to wander into the
            // circle, on the frame the meter filled, and every seat did it every time.
            //
            // ⚠️ MEASURED WITH THE ABILITY'S OWN FOOTPRINT, so a kit that aims in front of itself
            // is counted in front of itself. `stunPayload: false` deliberately: this is a
            // "is anybody there" count for a decision about VALUE, and the per-kit branch below
            // still applies the correct stun rule to the cast itself.
            //
            // ⚠️ AND IT ONLY EVER MAKES A BOT WAIT. Both escapes below are unconditional, so a
            // window that never comes still ends in a cast.
            bool ultimateWorthIt = false;

            if (kit.IsUltimateReady && kit.Ultimate != null)
            {
                int underIt = VictimsUnder(FootprintOf(kit.Ultimate),
                                           kit.Ultimate.TelegraphRadius + AiTuning.AbilityVictimMargin,
                                           stunPayload: false);

                ultimateWorthIt =
                    underIt >= AiTuning.UltimateWantsVictims
                    || _ultimateReadyFor >= AiTuning.UltimateHoldSeconds
                    || round.TimeLeft <= AiTuning.UltimateDumpWindowSeconds;
            }

            if (kit.IsUltimateReady && kit.Ultimate != null && ultimateWorthIt)
            {
                if (kit is Abilities.DanteHeroKit)
                {
                    // ⚠️ THE FISSURE IS DIRECTIONAL: a 4.5 m circle centred 2.2 m in FRONT of
                    // him. The old 9.0 m gate had no direction in it at all, so half of every
                    // cast opened the ground behind his back. It launches rather than stuns, so
                    // a body already down is still worth catching.
                    bool safeForOwnCan = !_motor.IsDefender || lataDistance > 10.0f;
                    if (safeForOwnCan && WouldCatch(kit.Ultimate, stunPayload: false))
                        Consider(intent, Verb.Ultimate, dt);
                }
                else if (kit is Abilities.CheskaHeroKit)
                {
                    // Glacial Shatter freezes what it catches, so somebody already frozen is
                    // not a reason to spend it.
                    if (WouldCatch(kit.Ultimate, stunPayload: true)) Consider(intent, Verb.Ultimate, dt);
                }
                else if (kit is Abilities.SeanHeroKit)
                {
                    // ⚠️ AN ATTACKER MAY DELIBERATELY METEOR THE LATA, and the reach for that is
                    // the ultimate's own radius rather than the 6.0 m that used to be written
                    // here. A DEFENDING Sean must never spend one knocking over their own
                    // objective, and must not knock it over as a side effect either.
                    float smash = kit.Ultimate.TelegraphRadius + AiTuning.AbilityVictimMargin;

                    bool meteorTheCan = !_motor.IsDefender && lata != null && lata.IsUpright
                                        && lataDistance <= smash;
                    bool safeForOwnCan = !_motor.IsDefender || lataDistance > 9.0f;

                    if (meteorTheCan
                        || (safeForOwnCan && WouldCatch(kit.Ultimate, stunPayload: false)))
                        Consider(intent, Verb.Ultimate, dt);
                }
                else if (kit is Abilities.ZackHeroKit)
                {
                    // ⚠️⚠️ THUNDERSTRIKE LANDS ON ZACK. Its telegraph is 4.5 m at range 0, and
                    // the old gate fired it at a target up to 8.0 m away, which is a lightning
                    // strike on an empty piece of road with the target watching from outside it.
                    if (WouldCatch(kit.Ultimate, stunPayload: true)) Consider(intent, Verb.Ultimate, dt);
                }
                else if (kit is Abilities.NemuHeroKit)
                {
                    // ⚠️ THE 4.5 m OFFSET AND 7.5 m RADIUS THAT USED TO BE WRITTEN HERE WERE THE
                    // PRE-FOOTPRINT-PASS NUMBERS. The void has been 2.8 m at 3.5 m since § 8
                    // brought the ability sizes down, so this asked about a circle nearly three
                    // times the area of the one it casts.
                    Vector3 voidCentre = FootprintOf(kit.Ultimate);
                    float voidReach = kit.Ultimate.TelegraphRadius + AiTuning.AbilityVictimMargin;

                    if (HasRelevantVoidTarget(voidCentre, voidReach)
                        || VictimsUnder(voidCentre, voidReach, stunPayload: false) > 0)
                        Consider(intent, Verb.Ultimate, dt);
                }
                else if (kit is Abilities.PhaisterHeroKit)
                {
                    // ⚠️ § 31.4 MADE THE ECLIPSE A ZONE, AND A ZONE HAS A SECOND CORRECT USE THE
                    // OLD DISTANCE GATE COULD NOT EXPRESS. Cast over the lata by a DEFENDING
                    // Phaister it makes the retrieval run impossible for its whole duration, so
                    // it is worth its `UltimateCost` 115 with nobody standing in it yet. Cast by
                    // an attacker it is a hole in the defence, and then it needs a body in it.
                    // ⚠️⚠️ AND "IT COVERS THE LATA" ALONE IS NOT ENOUGH, BECAUSE ITS REACH IS
                    // 10.5 m IN A 14 m BOX. A defending Phaister is nearly always inside that of
                    // the can, so covering it is very close to "cast the moment it is ready",
                    // which is the frame-one dump § 31.7 spent an opening delay removing. What
                    // makes the zone worth 115 charge is that it denies a RETRIEVAL, so there has
                    // to be a retrieval left to deny: a tsinelas lying loose inside the chalk that
                    // somebody has to come back in for.
                    bool overTheCan = _motor.IsDefender && lata != null
                                      && lataDistance <= kit.Ultimate.TelegraphRadius
                                      && AnyLooseSlipperInsideTheBox();

                    if (overTheCan || WouldCatch(kit.Ultimate, stunPayload: true))
                        Consider(intent, Verb.Ultimate, dt);
                }
            }

            if (SlotIsSpendable(kit.Skill1))
            {
                if (kit is Abilities.DanteHeroKit)
                {
                    // Seismic Stomp is a 2.2 m ring around his own feet. It was cast at 5.0 m.
                    if (WouldCatch(kit.Skill1, stunPayload: false)) Consider(intent, Verb.Skill1, dt);
                }
                else if (kit is Abilities.CheskaHeroKit)
                {
                    // ⚠️ THE OLD GATE WAS "DROP IT WHILE FLEEING", AND THE SHEET DOES NOT LAND
                    // BEHIND HER. Its telegraph is 2.8 m in FRONT, so a bot fleeing laid frost
                    // across its own escape and nowhere near whoever was chasing it.
                    if (WorthDenying(kit.Skill1)) Consider(intent, Verb.Skill1, dt);
                }
                else if (kit is Abilities.SeanHeroKit)
                {
                    // The dash is travel that knocks down what it hits: worth it for the
                    // journey, or for somebody standing in the line of it.
                    if (WorthTravelling()
                        || (target != null && targetDistance <= 5.0f && Facing(target, 40.0f)))
                        Consider(intent, Verb.Skill1, dt);
                }
                else if (kit is Abilities.ZackHeroKit)
                {
                    if (WorthTravelling()) Consider(intent, Verb.Skill1, dt);
                }
                else if (kit is Abilities.NemuHeroKit)
                {
                    // Phase before the pickup or the engage. Using it while carrying breaks it
                    // instantly by design, and spending it with nothing to be untaggable FROM is
                    // the same waste one step earlier: it is worth a 52 s cooldown only when
                    // somebody could actually tag her.
                    bool phaseApproach = !_motor.HoldingSlipper
                        && (Plan == AiPlan.Fetch || Plan == AiPlan.Stalk)
                        && targetDistance <= 6.0f;
                    bool phaseDefence = _motor.IsDefender && targetDistance <= 4.0f;

                    if (phaseApproach || phaseDefence) Consider(intent, Verb.Skill1, dt);
                }
                else if (kit is Abilities.PhaisterHeroKit)
                {
                    // ⚠️ THE HEX IS A CIRCLE ON THE FLOOR 4.5 m AHEAD, and the old 6.5 m gate to
                    // a target said nothing about where it would land or whether one was already
                    // lying there. Two sigils on one another is the § 19 stacking exactly.
                    if (WorthDenying(kit.Skill1)) Consider(intent, Verb.Skill1, dt);
                }
            }

            if (SlotIsSpendable(kit.Skill2))
            {
                if (kit is Abilities.DanteHeroKit)
                {
                    // ⚠️ THE CARAPACE IS IMMUNITY, SO IT WANTS SOMETHING TO BE IMMUNE TO. The old
                    // gate spent a 62 s cooldown on being taggable anywhere on the map, which is
                    // most of a retrieval run, most of the time, with nobody near him.
                    bool aboutToBeCaught = _motor.IsTaggable() && targetDistance <= 4.5f;
                    bool onBadGround = Abilities.HazardMap.CoversPoint(myPos, 1.0f);

                    if (aboutToBeCaught || onBadGround) Consider(intent, Verb.Skill2, dt);
                }
                else if (kit is Abilities.CheskaHeroKit)
                {
                    if (WorthDenying(kit.Skill2)
                        && (!_motor.IsDefender || lataDistance > Balance.TayaCampRadius))
                        Consider(intent, Verb.Skill2, dt);
                }
                else if (kit is Abilities.SeanHeroKit || kit is Abilities.ZackHeroKit)
                {
                    // ⚠️⚠️ THESE TWO WERE THE ONE PLACE A BOT SPENT A POWER WITH NO OPPORTUNITY
                    // TEST AT ALL, AND THEY ARE WHY 🧑 SAW THE SAME SKILLS EVERY ROUND. The gate
                    // was *"holding a tsinelas and the throw is legal"*, which is true of almost
                    // every second an attacker is alive, so both heroes armed a shot on cooldown
                    // rather than on a chance. `ArmingThisShotIsWorthIt` asks the question the
                    // ability is FOR: is there a shot to arm.
                    //
                    // ⚠️ `SlotIsSpendable` is still what stops it being armed twice.
                    if (_motor.HoldingSlipper && round.CanThrow(_motor)
                        && ArmingThisShotIsWorthIt(round))
                        Consider(intent, Verb.Skill2, dt);
                }
                else if (kit is Abilities.NemuHeroKit)
                {
                    // ⚠️ THE ONE `CanReactivate` POWER IN THE GAME, so the second press is not a
                    // recast and `SlotIsSpendable` lets it through while the first is running.
                    if (_driving && (Plan == AiPlan.Fetch || Plan == AiPlan.Stalk
                                     || targetDistance <= 6.0f))
                        Consider(intent, Verb.Skill2, dt);
                }
                else if (kit is Abilities.PhaisterHeroKit)
                {
                    // ⚠️ HER BLINK IS THE ONE HOLD-TO-AIM POWER IN THE GAME. See § HOLDING A
                    // HOLD-TO-AIM POWER: `Tap` would give it a one-frame hold and pin every bot
                    // blink to the minimum 2.0 m.
                    //
                    // ⚠️⚠️ AND ITS TELEGRAPH IS NOT A PAYLOAD, SO `WouldCatch` MUST NOT JUDGE IT.
                    // `TelegraphRadius` 1.15 is the ARRIVAL MARK: where she will be standing, not
                    // an area of effect. What the power does to somebody else is the shove at the
                    // point she LEAVES, so the question is whether anybody is near her now.
                    bool shoveOnDeparture = targetDistance <= 3.0f;

                    // ⚠️ THE CONVICTION WINDOW IS IN FRONT OF THE HOLD, NOT INSTEAD OF IT.
                    // `Consider` answers "have I wanted this long enough to commit"; `HoldAim`
                    // then runs every frame after that, exactly as before, so the aim ramp is
                    // untouched. Wiring the two the other way round would make the deliberation
                    // part of the hold and pin every bot blink to the minimum range again.
                    if (_driving && (Plan == AiPlan.Withdraw || shoveOnDeparture
                                     || targetDistance <= 5.5f)
                        && Weighed(Verb.Skill2, dt))
                        HoldAim(intent, Verb.Skill2, kit.Skill2, dt);
                }
            }

            // ⚠️ THE CLOCK RESTARTS ON A TOUCH, NOT ON A CONFIRMED CAST, because this side has
            // no way to know whether the press was answered: `HeroAbilitySystem` buffers a press
            // for 0.30 s and may refuse it outright. Spacing what the bot ASKS for is the honest
            // reading of "do not spam", and it also means a refused press costs the same beat a
            // successful one does, which is what stops a bot mashing an empty meter.
            Verb? justPressed = _touched.Contains(Verb.Skill1) ? Verb.Skill1
                              : _touched.Contains(Verb.Skill2) ? Verb.Skill2
                              : _touched.Contains(Verb.Ultimate) ? (Verb?)Verb.Ultimate
                              : null;

            if (justPressed.HasValue)
            {
                // ⚠️⚠️ A DIFFERENT SLOT COSTS MORE THAN THE SAME ONE. See § NOT THE WHOLE KIT IN
                // ONE BREATH: the flat cadence stops a frame-one dump and does nothing at all
                // about Q, then E, then the ultimate over six seconds, which is what 🧑 meant by
                // *"i dont want them to use all skills consecutively"*.
                //
                // ⚠️ THE COMPARISON IS AGAINST THE LAST SLOT ACTUALLY PRESSED, so the first cast
                // of a round pays the ordinary cadence (`_lastSlotPressed` starts null and the
                // `??` makes that a match).
                bool sameIdea = justPressed == (_lastSlotPressed ?? justPressed);

                // ⚠️ ROLLED, NOT FIXED. See `AiTuning.AbilityCadenceJitterSeconds`: four bots on
                // one constant are four metronomes that agree, and two that fire together stay
                // locked together for the rest of the round.
                _abilityCadenceLeft =
                    (sameIdea ? AiTuning.AbilityCadenceSeconds : AiTuning.AbilityChainSeconds)
                    + UnityEngine.Random.Range(0.0f, AiTuning.AbilityCadenceJitterSeconds);

                _lastSlotPressed = justPressed;

                // The decision has been taken. Start the next one from nothing.
                ForgetWhatWasBeingWeighed();
            }

            // ⚠️ NOTHING WORTH CASTING THIS FRAME MEANS THE HALF-FORMED DECISION IS DROPPED. A
            // conviction window that survived the reason going away would be a delay rather than
            // a decision: the bot would press at a target who had already walked out of it.
            if (!_weighedThisFrame) ForgetWhatWasBeingWeighed();

            ReleaseUntouchedHeroButtons(intent);
        }

        /// <summary>Seconds this bot must wait before asking for another power. See § THE
        /// CADENCE GATE.</summary>
        private float _abilityCadenceLeft;

        /// <summary>How long the current round has been live, for the opening delay.</summary>
        private float _roundLiveFor;

        /// <summary>How long this bot's ultimate has been ready and unspent. See § IS THE
        /// ULTIMATE WORTH SPENDING YET.</summary>
        private float _ultimateReadyFor;

        /// <summary>The last hero key this bot actually pressed, for the chain gap. See § NOT
        /// THE WHOLE KIT IN ONE BREATH.</summary>
        private Verb? _lastSlotPressed;

        // -------------------------------------------------------------------
        // § PRETENDING TO THINK
        //
        // ⚠️⚠️ 🧑 2026-08-27: *"try to make it so that AI think or pretend to think when to use
        // skills"*, and later, *"Make sure u actually make ai better/ smarter with skill usage"*.
        // Both halves are answered by the same three lines, which is the point: a bot that has to
        // hold a reason before it acts is BOTH slower to fire and harder to bait.
        //
        // ⚠️⚠️ THE WINDOW IS CONTINUOUS AND IT IS PER SLOT. `Consider` is only reached on a frame
        // the branch's own conditions hold, so the accumulator only advances while the reason is
        // still true, and switching slots restarts it from zero with a fresh roll. Three
        // consequences, and the second and third are the "smarter" part:
        //
        //  1. A press looks decided rather than reflexive, which is the reported feeling.
        //  2. A target who steps out of a footprint during the window is not chased by a cast
        //     that was already committed. The old code fired on the first frame the distance
        //     check passed, which on a rival sprinting past is a single frame of coincidence.
        //  3. A bot cannot flip between two slots and fire both: weighing Skill1 for 0.4 s and
        //     then Skill2 for 0.4 s spends nothing, where before it would have spent both.
        //
        // ⚠️ IT IS NOT A COOLDOWN AND MUST NOT BECOME ONE. `AbilityCadenceSeconds` is the spacing
        // between casts; this is the pause before ONE cast, and it is spent while the bot is
        // already looking at the thing it is about to do.
        // -------------------------------------------------------------------

        /// <summary>Which hero key this bot is currently weighing, or null for none.</summary>
        private Verb? _weighing;

        /// <summary>How long it has been weighing that one, continuously.</summary>
        private float _weighedFor;

        /// <summary>The window this particular decision has to clear, rolled when it starts.</summary>
        private float _weighWindow;

        /// <summary>Did any branch ask about a slot this frame? See the note above.</summary>
        private bool _weighedThisFrame;

        private void ForgetWhatWasBeingWeighed()
        {
            _weighing = null;
            _weighedFor = 0.0f;
        }

        /// <summary>
        /// Has this bot wanted <paramref name="verb"/> for long enough to commit to it?
        ///
        /// ⚠️ IT ROLLS A NEW WINDOW EVERY TIME THE SUBJECT CHANGES, so a bot is not predictably
        /// half a second late on everything. Scaled by `AiPersonalityRoll.Tempo`, which is the
        /// same 0.85..1.20 that already decides how fast this bot re-plans.
        /// </summary>
        private bool Weighed(Verb verb, float dt)
        {
            _weighedThisFrame = true;

            if (_weighing != verb)
            {
                _weighing = verb;
                _weighedFor = 0.0f;

                // ⚠️⚠️ SCALED BY THIS BOT'S APPETITE FOR THIS PARTICULAR SLOT. See
                // `AiPersonalityRoll.SkillAppetite`: 🧑 asked for bots that may simply never find
                // a use for one of their powers, *"bcz thats normal and human"*. A shy bot needs
                // a longer unbroken reason, so a marginal window passes it by and a clear one
                // still gets taken. Nothing rolls a die and refuses an opportunity it saw.
                float eagerness = _self.AppetiteFor(SlotIndexOf(verb));
                float appetiteScale = Mathf.Lerp(AiTuning.AppetiteWindowShy,
                                                 AiTuning.AppetiteWindowEager, eagerness);

                _weighWindow = UnityEngine.Random.Range(AiTuning.AbilityThinkMin,
                                                        AiTuning.AbilityThinkMax)
                               * _self.Tempo * appetiteScale;
                return false;
            }

            _weighedFor += dt;
            return _weighedFor >= _weighWindow;
        }

        /// <summary>The `AiPersonalityRoll.SkillAppetite` index for a hero key.</summary>
        private static int SlotIndexOf(Verb verb)
            => verb == Verb.Skill1 ? 0 : verb == Verb.Skill2 ? 1 : 2;

        /// <summary>
        /// Is there a shot worth arming right now? For Sean's and Zack's throw buffs.
        ///
        /// ⚠️⚠️ THE POWER BUFFS THE NEXT RELEASE, SO THE OPPORTUNITY IS THE RELEASE AND NOT THE
        /// SLIPPER. Arming while walking to a bearing five seconds away spends a charge on a
        /// throw that may never happen: the bot can be tagged, shoved, tripped, or the can can
        /// go over to somebody else's shoe first, and `HeroAbility` has no way to hand it back.
        ///
        /// Three conditions, and each one removes a way the arm gets wasted:
        ///  * **The bot is about to shoot.** Planted and charging, or standing on its mark. Both
        ///    are one press from a release.
        ///  * **The lane is clear.** `LaneBlocked` walks the arc the shoe will actually fly and
        ///    asks what it will hit, so an armed throw into somebody's back is not an
        ///    opportunity, it is the buff landing on a body instead of the can.
        ///  * **Nobody else's shoe is already inbound.** The same reasoning as
        ///    `RivalShotIsInbound`, one layer earlier: a can that is going over does not need
        ///    a second, buffed tsinelas thrown at it.
        ///
        /// ⚠️ IT MAY LEGITIMATELY NEVER BE TRUE IN A ROUND, which is the point. 🧑 2026-08-27:
        /// *"i want it to be possible too for them to not use some skills at all if they cant
        /// find opportunity bcz thats normal and human"*.
        /// </summary>
        private bool ArmingThisShotIsWorthIt(RoundDirector round)
        {
            if (round == null) return false;

            bool aboutToShoot = Plan == AiPlan.Windup || (Plan == AiPlan.Position && _arrived);
            if (!aboutToShoot) return false;

            var lata = round.Lata;
            if (lata == null) return false;

            if (RivalShotIsInbound(lata)) return false;

            // ⚠️ AT FULL POWER, BECAUSE THAT IS WHAT AN ARMED SHOT IS FOR. Asking about a weak
            // throw would refuse the arm on a lane the real shot clears comfortably.
            return !LaneBlocked(transform.position, lata.transform.position, 1.0f);
        }

        /// <summary>Press <paramref name="verb"/>, but only once it has been wanted long
        /// enough. The drop-in replacement for `Tap` inside `StepHeroAbilities`.</summary>
        private void Consider(InputIntent intent, Verb verb, float dt)
        {
            if (Weighed(verb, dt)) Tap(intent, verb);
        }

        private void ReleaseUntouchedHeroButtons(InputIntent intent)
        {
            if (!_touched.Contains(Verb.Skill1)) Press(intent, Verb.Skill1, false);
            if (!_touched.Contains(Verb.Skill2)) Press(intent, Verb.Skill2, false);
            if (!_touched.Contains(Verb.Ultimate)) Press(intent, Verb.Ultimate, false);
        }

        private bool HasRelevantVoidTarget(Vector3 center, float radius)
        {
            foreach (var slipper in FindObjectsByType<Slipper>(FindObjectsInactive.Exclude))
            {
                if (slipper == null || slipper.State != SlipperState.Loose) continue;
                if (!_motor.IsDefender && slipper.OwnerSlot != _motor.PlayerSlot) continue;
                if (Flat(center, slipper.transform.position) <= radius) return true;
            }

            return false;
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

            // ⚠️ THE HUMAN-CADENCE CLOCKS RESET WITH EVERYTHING ELSE, for the reason the lunge
            // hold does one note down. A bot stunned mid-burst would otherwise stand up with the
            // sprint key still counted as held and a key change beat owed from a heading it chose
            // before it was hit.
            _keyGapLeft = 0.0f;
            _sprintBurstLeft = 0.0f;
            _sprintRestLeft = 0.0f;
            _sprintWantHeld = 0.0f;
            _sprintAsked = false;
            _glanceLeft = 0.0f;

            // ⚠️ THE AIM HOLD RESETS HERE FOR THE SAME REASON THE LUNGE CHARGE DOES, one note
            // up. A bot stunned mid-aim otherwise resumes counting from wherever it stopped and
            // fires a blink the instant it recovers, in the direction it was facing before it
            // was hit, which is a teleport nobody saw wind up.
            _aimHeld.Clear();

            // ⚠️⚠️ A SUPPRESSED CONTROLLER CLEARS THE LEGS AND LEAVES THE HERO KEYS ALONE.
            // `intent.Clear()` empties the whole table, and during a possession the player is
            // holding Skill2 to come home: wiping it because NEMU'S BODY got stunned would strand
            // the player inside the pet with no way back, which is the same fault
            // `AbilitiesEnabled` exists for, reached through the stun branch instead.
            if (AbilitiesEnabled)
            {
                intent.Clear();
            }
            else
            {
                intent.Move = Vector2.zero;
                Press(intent, Verb.Sprint, false);
                Press(intent, Verb.Jump, false);
                Press(intent, Verb.Grab, false);
                Press(intent, Verb.Lunge, false);
                Press(intent, Verb.SpecialAbility, false);
            }

            _pressed.Clear();
        }

        // ---- WIND-UP AND LOITER STATE ---------------------------------------

        private bool _goalValid;
        private bool _windup;
        private float _windupTime;
        private float _windupWait;
        private float _windupPower = 1.0f;
        private float _windupSpin;
        private Vector3 _windupScatter;
        private bool _blundering;
        private float _lungeHeld = -1.0f;
        private float _loiterLeft;
        private float _loiterDir;
        private CharacterMotor _lastThreat;

        /// <summary>Whoever this taya is chasing, so a tie does not rotate the pursuit. See
        /// <see cref="TagTarget"/>.</summary>
        private CharacterMotor _lastTagTarget;

        /// <summary>Seconds left of the gap between two movement keys. § THE KEY CHANGE BEAT.</summary>
        private float _keyGapLeft;

        private float _sprintBurstLeft;
        private float _sprintRestLeft;
        private float _sprintWantHeld;
        private bool _sprintAsked;

        private float _glanceLeft;
        private Vector3 _glanceAt;
    }
}
