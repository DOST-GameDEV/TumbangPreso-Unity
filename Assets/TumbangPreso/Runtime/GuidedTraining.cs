using System.Collections;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Social;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TumbangPreso
{
    /// <summary>
    /// A playable training route launched from the existing How to Play panel.
    ///
    /// The reference pages remain the place to look rules up. This component is the other half:
    /// one objective at a time, performed with the real input, real character controller, real
    /// lata, real tsinelas and real hero kit. It never calls a gameplay verb on the player's
    /// behalf. Setup may place a dummy or return ammunition between lessons, but completion is
    /// always observed from the same state the live game observes.
    /// </summary>
    public sealed class GuidedTraining : MonoBehaviour
    {
        public enum Lesson
        {
            Look,
            Move,
            Sprint,
            Jump,
            Throw,
            Retrieve,
            Pektus,
            Shove,
            AbilityInfo,
            Skill1,
            Skill2,
            Ultimate,
            DefenderReset,
            Punch,
            Lunge,
            TripRecovery,
            Emote,
            Complete,
        }

        public const int LessonCount = (int)Lesson.Complete;

        private CharacterMotor _local;
        private CharacterMotor _dummy;
        private Lata _lata;
        private CharacterMotor[] _seats;
        private Slipper[] _slippers;
        private Slipper _ownSlipper;
        private SliceRunner _runner;
        private Carrier _carrier;
        private CombatVerbs _verbs;
        private HeroAbilitySystem _abilities;
        private EmotePlayer _emotes;
        private InputAction _abilityInfo;

        private GuidedTrainingHud _hud;
        private TrainingMarker _marker;
        private Lesson _lesson;
        private bool _ready;
        private bool _advancing;
        private bool _defenderResetArmed;

        /// <summary>
        /// The arming coroutine for <see cref="Lesson.DefenderReset"/>, held so it can be
        /// stopped.
        ///
        /// ⚠️⚠️ IT WAS FIRE AND FORGET AND IT COULD KNOCK THE CAN OVER DURING THE **NEXT**
        /// LESSON. `ArmDefenderReset` waits out the restore protection before it topples the
        /// can, and `Update` completes the current lesson on `N`, so a player who pressed the
        /// skip key while that wait was running arrived at PUNCH and the can went over under
        /// them a moment later. `RoundDirector.ResolveTag` opens with `if (Lata == null ||
        /// !Lata.IsUpright) return;` — **so every punch and every lunge for the rest of the
        /// route was refused in silence**, which is one half of 🧑 2026-09-02's *"u cant raise
        /// can and tag ppl"*. A lesson's own timers end with the lesson.
        /// </summary>
        private Coroutine _armRoutine;

        /// <summary>
        /// `Time.time` when the current lesson began, so a lesson can ask "has this happened
        /// SINCE I started" rather than "has this ever happened".
        ///
        /// ⚠️ IT IS WHAT MAKES A HIT READABLE WITHOUT A COUNTER TO ZERO. See
        /// <see cref="CombatVerbs.LastShoveLandedAt"/>.
        /// </summary>
        private float _lessonBeganAt;

        /// <summary>
        /// `Time.time` of the last tag the STUDENT scored, written from
        /// <see cref="RoundDirector.Tagged"/>.
        ///
        /// ⚠️ OFF THE EVENT RATHER THAN OFF A COOLDOWN, because a cooldown is set before the
        /// cone is searched. `CombatVerbs.StepPunch` writes `_punchCooldown` on line two and
        /// finds its victim on line fourteen; the lesson was reading line two.
        /// </summary>
        private float _lastTagByStudentAt = -999.0f;

        private float _metric;
        private float _lastTripLeft;
        private Vector3 _lastPosition;

        public Lesson CurrentLesson => _lesson;

        public void Configure(CharacterMotor local, Lata lata, CharacterMotor[] seats,
                              Slipper[] slippers, SliceRunner runner)
        {
            _local = local;
            _lata = lata;
            _seats = seats;
            _slippers = slippers;
            _runner = runner;

            if (_local == null || _lata == null || _runner == null)
            {
                Debug.LogError("[Training] arena did not provide the player, lata and runner.");
                enabled = false;
                return;
            }

            _carrier = _local.GetComponent<Carrier>();
            _verbs = _local.GetComponent<CombatVerbs>();

            // ⚠️ THE TAG IS SUBSCRIBED TO, NOT POLLED. `RoundDirector.Tagged` fires from
            // `ResolveTag`, which is the ONE function that decides a tag happened, for the jab
            // and the lunge and for a bot and a human alike. Watching a cooldown instead is what
            // let PUNCH and LUNGE be completed by pressing the key at nobody.
            if (GameServices.Round != null) GameServices.Round.Tagged += OnSomebodyTagged;
            _abilities = _local.AbilitySystem;
            _emotes = _local.GetComponent<EmotePlayer>();

            foreach (var seat in _seats)
            {
                if (seat == null || seat == _local) continue;

                var brain = seat.GetComponent<AIController>();
                if (brain != null) brain.enabled = false;
                seat.Intent.Parked = true;

                if (_dummy == null && !seat.IsDefender) _dummy = seat;
            }

            if (_dummy == null)
            {
                foreach (var seat in _seats)
                    if (seat != null && seat != _local) { _dummy = seat; break; }
            }

            _hud = GuidedTrainingHud.Build(transform);
            _marker = TrainingMarker.Build();

            var input = Resources.Load<InputActionAsset>("TumbangPreso");
            _abilityInfo = input?.FindActionMap("Player", false)?.FindAction("AbilityInfo", false);
            _abilityInfo?.Enable();

            StartCoroutine(BeginAfterInstall());
        }

        private IEnumerator BeginAfterInstall()
        {
            // MatchInstaller finishes constructing the camera and HUD on this frame. Beginning
            // on the next lets the ordinary runner perform the exact round-start handoff first.
            yield return null;

            _runner.Begin();

            // ⚠️⚠️ OWNERSHIP IS DEALT BY `Begin`, NOT BY THE SLIPPER'S NAME, AND ASKING
            // BEFORE IT RAN IS WHERE THE FLOATING TSINELAS CAME FROM.
            // `SliceRunner.EquipOwnedSlippers` REWRITES `OwnerSlot` every round: it walks the
            // attackers in seat order and hands them `Slippers[0]`, `[1]`, `[2]`, so with seat 0
            // as the taya the local seat 1 owns SLIPPER 0, not slipper 1. This used to run in
            // `Configure`, one frame earlier, and matched on the pre-round ownership, so
            // `_ownSlipper` was a tsinelas belonging to somebody else. `HideTheCast` then
            // switched off the one that was really in the player's hand and KEPT the other,
            // which was in a hidden seat's hand: a shoe hanging 0.85 m over an empty road, which
            // is the reported *"theres a floating slipper check ss"*. Measured by
            // `TrainingStreetProbe`, which is why it prints the holder of every tsinelas.
            ResolveOwnSlipper();
            HideTheCast();

            // ⚠️ THE KIT IS HIDDEN UNTIL ITS OWN LESSON. See `Hud.SetTrainingDeckHidden`.
            UI.Hud.Instance?.SetTrainingDeckHidden(true);

            _ready = true;
            EnterLesson(Lesson.Look);
        }

        private void Update()
        {
            if (!_ready || _local == null) return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.backspaceKey.wasPressedThisFrame)
            {
                ExitTraining();
                return;
            }

            if (_lesson == Lesson.Complete)
            {
                if (keyboard != null && keyboard.enterKey.wasPressedThisFrame) ExitTraining();
                return;
            }

            if (_advancing) return;

            if (keyboard != null && keyboard.nKey.wasPressedThisFrame)
            {
                CompleteLesson();
                return;
            }

            // ⚠️⚠️ A LESSON THE SEAT CANNOT ANSWER IS SKIPPED, NOT WAITED ON. The four hero
            // lessons check `HeroAbilitySystem`, and a seat with no kit has no way to satisfy
            // any of them: pressing the key produces no cast, so `WasSuccessfulCast` is false
            // forever and the route stops at step 10 of 17. The N key would carry a player past
            // it, but a tutorial whose only exit is the skip key is a tutorial that has failed.
            // Classic is a shipping mode with no powers at all (`CLAUDE.md` § 1), so this is a
            // real seat, not a broken one.
            if (LessonNeedsAKit(_lesson) && (_abilities == null || _abilities.Kit == null))
            {
                CompleteLesson();
                return;
            }

            EvaluateLesson();
        }

        private void EvaluateLesson()
        {
            float dt = Time.unscaledDeltaTime;

            switch (_lesson)
            {
                case Lesson.Look:
                    if (Mouse.current != null)
                        _metric += Mouse.current.delta.ReadValue().magnitude;
                    SetProgress(_metric / 520.0f);
                    if (_metric >= 520.0f) CompleteLesson();
                    break;

                case Lesson.Move:
                    AddTravel();
                    SetProgress(_metric / 4.0f);
                    if (_metric >= 4.0f) CompleteLesson();
                    break;

                case Lesson.Sprint:
                    if (_local.Intent.Pressed(Verb.Sprint)
                        && _local.Intent.MoveAxis.sqrMagnitude > 0.1f)
                        _metric += dt;
                    SetProgress(_metric / 1.0f);
                    if (_metric >= 1.0f) CompleteLesson();
                    break;

                case Lesson.Jump:
                    if (!_local.IsGrounded && _local.Velocity.y > 0.1f) CompleteLesson();
                    break;

                case Lesson.Throw:
                    if (_ownSlipper != null && _ownSlipper.State == SlipperState.InFlight
                        && _ownSlipper.ThrowerSlot == _local.PlayerSlot)
                        CompleteLesson();
                    break;

                case Lesson.Retrieve:
                    if (_carrier != null && _carrier.Held != null
                        && _carrier.Held.OwnerSlot == _local.PlayerSlot)
                        CompleteLesson();
                    break;

                case Lesson.Pektus:
                    if (_ownSlipper != null && _ownSlipper.State == SlipperState.InFlight
                        && _ownSlipper.ThrowerSlot == _local.PlayerSlot
                        && Mathf.Abs(_ownSlipper.PektusSpin) >= 0.30f)
                        CompleteLesson();
                    break;

                // ⚠️⚠️ THE THREE CONTACT LESSONS ASK WHETHER THE VERB **LANDED**, AND ALL THREE
                // USED TO ASK WHETHER IT FIRED. 🧑 2026-09-02: *"sometimes some tasks get marked
                // even if u dont rlly do them like pushing ppl (as long as u click push it gets
                // marked as done)"*. Each read `<verb>CooldownLeft > _baselineCooldown + 0.05f`,
                // and in `CombatVerbs` every one of those cooldowns is written BEFORE the cone
                // is searched — `StepPunch` sets `_punchCooldown` on its second line and finds
                // its victim on its fourteenth. So a press aimed at the sky completed the
                // lesson, and the one thing these three lessons exist to teach, which is that
                // the taya's verbs have a reach and an arc, was the thing the tick did not ask
                // about.
                //
                // ⚠️ THE SIGNALS ARE THE ONES THE GAME ITSELF USES. `LastShoveLandedAt` is
                // written inside `ApplyShoveTo`, the single place a shove moves anybody, and the
                // tag comes off `RoundDirector.Tagged`, the event `ResolveTag` raises. Neither
                // can report a hit the match did not resolve.
                case Lesson.Shove:
                    if (_verbs != null && _verbs.LastShoveLandedAt > _lessonBeganAt)
                        CompleteLesson();
                    break;

                case Lesson.AbilityInfo:
                    if (_abilityInfo != null && _abilityInfo.IsPressed())
                    {
                        _metric += dt;
                        SetProgress(_metric / 0.65f);
                        if (_metric >= 0.65f) CompleteLesson();
                    }
                    break;

                case Lesson.Skill1:
                    if (WasSuccessfulCast(HeroAbilitySystem.Slot.Skill1)) CompleteLesson();
                    break;

                case Lesson.Skill2:
                    if (WasSuccessfulCast(HeroAbilitySystem.Slot.Skill2)) CompleteLesson();
                    break;

                case Lesson.Ultimate:
                    if (WasSuccessfulCast(HeroAbilitySystem.Slot.Ultimate)) CompleteLesson();
                    break;

                case Lesson.DefenderReset:
                    if (_defenderResetArmed && _lata.IsUpright) CompleteLesson();
                    break;

                // ⚠️ BOTH TAG LESSONS READ THE SAME SIGNAL, because both verbs end in the same
                // call. `StepPunch` reaches `ResolveTag` directly and `SweepLungeTag` reaches it
                // from the dash sweep; a lesson that watched the lunge's own cooldown was
                // measuring the dash rather than the tag it is for.
                case Lesson.Punch:
                case Lesson.Lunge:
                    if (_lastTagByStudentAt > _lessonBeganAt) CompleteLesson();
                    break;

                case Lesson.TripRecovery:
                    // A press is detected as a drop LARGER than one frame of real time, so it
                    // counts only presses `CharacterMotor` actually accepted through the real
                    // rate cap.
                    //
                    // ⚠️ THERE IS NO BLEED AT ALL ABOVE `Balance.MinTripDown` AS OF 2026-08-26,
                    // so above the floor the expected drop is zero and any movement is a press.
                    // The `Time.deltaTime` allowance is kept because the LAST stretch of a fall,
                    // under the floor, does still run at real time and would otherwise be
                    // credited as presses nobody made.
                    float expected = Mathf.Max(0.0f, _lastTripLeft - Time.deltaTime);
                    if (_local.TripLeft < expected - 0.05f) _metric += 1.0f;
                    _lastTripLeft = _local.TripLeft;
                    SetProgress(_metric / 5.0f);

                    if (_metric >= 5.0f)
                    {
                        CompleteLesson();
                        break;
                    }

                    // ⚠️⚠️ THE LESSON PUTS YOU BACK DOWN, AND WITHOUT THIS IT COULD STRAND THE
                    // PLAYER. The trip is applied ONCE, on entering the lesson, and the exit
                    // condition is five ACCEPTED presses. A fall holds at most
                    // (2.50 - 0.35) / 0.22 = 10 of them, so a player who watches the first fall
                    // out instead of mashing reaches zero with the counter short and nothing
                    // left to press: the lesson can then never be completed and the route stops
                    // dead at step 15 of 17. Re-applying is also the honest teaching, because
                    // the thing being taught is that mashing is what ends a fall.
                    if (!_local.IsTripped)
                    {
                        _local.ApplyTrip();
                        _lastTripLeft = _local.TripLeft;
                    }
                    break;

                case Lesson.Emote:
                    if (_emotes != null && _emotes.IsEmoting) CompleteLesson();
                    break;
            }
        }

        /// <summary>Lessons that only exist for a seat carrying a hero kit.
        ///
        /// ⚠️ `AbilityInfo` IS IN HERE TOO. It teaches holding the key that inspects the kit,
        /// and with no kit the panel it opens has nothing in it to read.</summary>
        private static bool LessonNeedsAKit(Lesson lesson)
            => lesson == Lesson.AbilityInfo
               || lesson == Lesson.Skill1
               || lesson == Lesson.Skill2
               || lesson == Lesson.Ultimate;

        private bool WasSuccessfulCast(HeroAbilitySystem.Slot slot)
            => _abilities != null
               && _abilities.SecondsSinceAnswer(slot) <= 0.22f
               && _abilities.LastAnswer(slot) == HeroKit.CastOutcome.Cast;

        private void AddTravel()
        {
            Vector3 now = _local.transform.position;
            Vector3 moved = now - _lastPosition;
            moved.y = 0.0f;
            _metric += Mathf.Min(moved.magnitude, 0.5f);
            _lastPosition = now;
        }

        private void SetProgress(float ratio) => _hud?.SetProgress(Mathf.Clamp01(ratio));

        /// <summary>
        /// A lesson is answered. Flash the card, sound it, and move on after a beat.
        ///
        /// ⚠️⚠️ THE ROUTE WAS SILENT, AND CLEARING A STEP IS THE ONE MOMENT IT HAS TO PAY OFF.
        /// 🧑 2026-09-02: *"can u add sfx too for eac stage cleared in tutorial?"*. Seventeen
        /// lessons went by with a 0.70 s scale pop on a card the student is not looking at, since
        /// they are looking at the thing they just did.
        ///
        /// ⚠️⚠️ THE PITCH RISES ACROSS THE ROUTE AND THAT IS THE FEATURE, NOT DECORATION. One
        /// unchanging ping seventeen times is a notification; a tone that climbs a fifth from
        /// lesson one to lesson seventeen is progress you can hear without reading `03 / 17` off
        /// the card. `AudioDirector.PlayAtVaried` takes a min and a max and picks between them,
        /// so passing the same value twice is how you ask it for an exact pitch.
        ///
        /// ⚠️ `score_award` RATHER THAN A NEW CUE, AND `AudioCueCheck` IS WHY. Every id in
        /// `AudioCues` must have a file on disk; inventing `tutorial_step` here would register a
        /// cue with nothing behind it, which `sfx_lrt_pass`'s note records costing two months of
        /// silence. `score_award` is already the game's "you did the thing" ping and this is
        /// exactly that.
        ///
        /// ⚠️ AT `Vector3.zero`, LIKE `MenuSfx`. This is a UI event and not something that
        /// happened at a place on the court, so a positioned voice would pan it away from a
        /// student standing off-centre.
        /// </summary>
        private void CompleteLesson()
        {
            if (_advancing) return;
            _advancing = true;
            _hud?.FlashComplete();

            // 1.00 at LOOK to 1.50 at EMOTE, which is a fifth over the seventeen.
            float climb = LessonCount > 1
                ? Mathf.Clamp01((int)_lesson / (float)(LessonCount - 1))
                : 0.0f;
            float pitch = Mathf.Lerp(1.00f, 1.50f, climb);

            GameServices.Audio?.PlayAtVaried("score_award", Vector3.zero, pitch, pitch, 0.85f);

            StartCoroutine(AdvanceAfterBeat());
        }

        private IEnumerator AdvanceAfterBeat()
        {
            yield return new WaitForSecondsRealtime(0.70f);
            EnterLesson((Lesson)((int)_lesson + 1));
        }

        /// <summary>
        /// The verb this lesson is about. Everything up to and including it is unlocked;
        /// everything after it is dead until the route gets there.
        ///
        /// ⚠️⚠️ 🧑, 2026-08-26: *"i dont want there to be bots and other shit like skills or
        /// throwing until the tutorial wants u to actually do it bcz its confusing that i can do
        /// a lot of shit, theres a tendency to not follow and focus on tutorial"*. He is
        /// describing a real failure of the route: every verb in the game was live on lesson
        /// one, so the tutorial was a suggestion laid over a sandbox rather than a sequence.
        ///
        /// ⚠️ CUMULATIVE, NOT EXCLUSIVE, AND THAT IS NOT A SOFTENING OF IT. Several lessons NEED
        /// an earlier verb to be performed at all: the retrieval run wants sprint and jump, the
        /// shove wants you to walk into somebody, and the trip lesson is answered with the jump
        /// key. Locking to exactly one verb would make half the route unplayable. What is
        /// removed is the ability to run AHEAD of the lesson, which is the thing he described.
        ///
        /// ⚠️ `Verb.None` MEANS THE LESSON TEACHES NO BUTTON. Look and Move are the mouse and
        /// the movement axis, neither of which is a `Verb`, and both are always available.
        /// </summary>
        private static Verb? VerbTaughtBy(Lesson lesson)
        {
            switch (lesson)
            {
                case Lesson.Look:           return null;
                case Lesson.Move:           return null;
                case Lesson.Sprint:         return Verb.Sprint;
                case Lesson.Jump:           return Verb.Jump;
                case Lesson.Throw:          return Verb.SpecialAbility;
                case Lesson.Retrieve:       return Verb.Grab;
                case Lesson.Pektus:         return Verb.SpecialAbility;
                case Lesson.Shove:          return Verb.Grab;
                case Lesson.AbilityInfo:    return null;
                case Lesson.Skill1:         return Verb.Skill1;
                case Lesson.Skill2:         return Verb.Skill2;
                case Lesson.Ultimate:       return Verb.Ultimate;
                case Lesson.DefenderReset:  return Verb.Grab;
                case Lesson.Punch:          return Verb.SpecialAbility;
                case Lesson.Lunge:          return Verb.Lunge;
                case Lesson.TripRecovery:   return Verb.Jump;
                case Lesson.Emote:          return Verb.EmoteWheel;
                default:                    return null;
            }
        }

        private readonly System.Collections.Generic.HashSet<Verb> _unlocked =
            new System.Collections.Generic.HashSet<Verb>();

        /// <summary>
        /// ⚠️ THE LOCK IS LIFTED ENTIRELY ON `Complete`, so the free-play window at the end of
        /// the route is the real game. A tutorial that hands back a crippled character is worse
        /// than one that never restricted anything.
        ///
        /// ⚠️ AND IT IS RELEASED IN `OnDestroy` TOO. `InputIntent` outlives this component with
        /// the seat it belongs to, so exiting training mid-route without clearing this would hand
        /// the next match a player who cannot throw.
        /// </summary>
        private void ApplyVerbLock(Lesson lesson)
        {
            if (_local == null) return;

            if (lesson >= Lesson.Complete)
            {
                _local.Intent.AllowOnly(null);
                return;
            }

            for (var step = Lesson.Look; step <= lesson; step++)
            {
                var taught = VerbTaughtBy(step);
                if (taught.HasValue) _unlocked.Add(taught.Value);
            }

            _local.Intent.AllowOnly(_unlocked);
        }

        /// <summary>
        /// Which side of the game a lesson is about.
        ///
        /// ⚠️⚠️ THE ROLE WAS INHERITED FROM WHICHEVER LESSON RAN BEFORE, AND THAT IS THE WHOLE
        /// FAULT. 🧑 2026-08-29 gave the diagnosis himself and it was right: *"i think its bcz the
        /// role doesnt change in between those phases"*, with *"can hold x to reset here"* and
        /// *"u also cant tag"*.
        ///
        /// **`DefenderReset` was the only lesson in the route that made you the taya.** `Punch`
        /// and `Lunge` come straight after it, are titled `PUNCH A VULNERABLE ATTACKER` and
        /// `LUNGE`, and set no role at all — they only READ `_local.IsDefender` to decide where
        /// to stand the dummy. And `CombatVerbs` refuses both outright:
        ///
        ///     if (... || !_motor.IsDefender || !_motor.CanAct()) return false;
        ///
        /// So any route that reached them without passing through `DefenderReset` asked for two
        /// verbs the player was structurally incapable of performing, and refused every press in
        /// silence. **That route is one keypress away**: `Update` completes the current lesson on
        /// `N`, and it auto-completes the four hero lessons for a seat with no kit. Somebody who
        /// pressed N because the reset looked stuck arrived at PUNCH as an attacker and could not
        /// tag, which is both halves of his report from one cause.
        ///
        /// ⚠️ SO THE ROLE IS DECLARED BY THE LESSON RATHER THAN LEFT TO THE ORDER. The route can
        /// then be entered anywhere — skipped through, jumped into by a probe, restarted — and
        /// still be coherent, which is what makes `TutorialDefenderProbe` a test of the game
        /// rather than a test of one path through it.
        /// </summary>
        private static bool LessonIsTheTayas(Lesson lesson)
            => lesson == Lesson.DefenderReset
            || lesson == Lesson.Punch
            || lesson == Lesson.Lunge;

        /// <summary>
        /// Puts the student on the side the lesson is about, and only when they are not already
        /// on it.
        ///
        /// ⚠️ ONLY ON A CHANGE, because `BecomeDefender` teleports to the can and drops whatever
        /// is in hand. Re-running it on every lesson would yank a player across the street
        /// between PUNCH and LUNGE for no reason and undo `PrepareDummyInFront`'s placement.
        ///
        /// ⚠️ AND THE ATTACKER SIDE IS APPLIED TOO, NOT JUST THE TAYA. `TripRecovery` and `Emote`
        /// follow the two taya lessons, and a player left holding the taya's role through them is
        /// being taught the wrong half of the game: the trip lesson's own text is about being an
        /// attacker put on the road.
        /// </summary>
        private void ApplyLessonRole(Lesson lesson)
        {
            if (_local == null || lesson >= Lesson.Complete) return;

            bool wantsTaya = LessonIsTheTayas(lesson);
            if (_local.IsDefender == wantsTaya) return;

            if (wantsTaya) BecomeDefender();
            else BecomeAttacker();
        }

        /// <summary>
        /// Everything a lesson has to hand the NEXT one, cleared before it starts.
        ///
        /// ⚠️⚠️ A LESSON WAS ABLE TO STUN THE LESSON AFTER IT. `DefenderReset` is step 13 and
        /// `Ultimate` is step 12, and his screenshot of the failure is drenched in the magenta of
        /// Nemu's DEVOURING SEANCE. A live hazard zone from a practice cast stuns whoever stands
        /// in it; `CharacterMotor.CanAct()` is `RoundActive &amp;&amp; !IsStunned`, and
        /// `Carrier.Update` returns before it ever reaches `StepDefender`.
        ///
        /// ⚠️ `ResetHeroKit` DID NOT AND COULD NOT COVER THIS. It resets the KIT — cooldowns and
        /// charge — not the objects a cast has already put in the world, and the four hero
        /// lessons call it on the way IN with nothing cleaning up on the way out.
        ///
        /// ⚠️⚠️ AND IT IS RIGHT WHETHER OR NOT IT WAS A CAUSE. A tutorial that asks you to perform
        /// a verb has to start you able to perform it; a student held by their own previous
        /// exercise has been given an objective and had the means to meet it taken away. This is
        /// a route with an instructor, not a match.
        ///
        /// ⚠️ THE HAZARDS GO FIRST AND THE STUN SECOND, because clearing the stun while the zone
        /// that applied it is still on the road buys exactly one frame before it lands again.
        /// </summary>
        private void ClearTheLastLessonsMess()
        {
            foreach (var volume in FindObjectsByType<Abilities.HazardVolume>(
                         FindObjectsSortMode.None))
            {
                if (volume != null && volume.gameObject != null) Destroy(volume.gameObject);
            }

            if (_local == null) return;

            _local.ClearStun();
            _local.ClearTrip();
            _local.Stamina.RefillAndClearFatigue();
        }

        private void EnterLesson(Lesson lesson)
        {
            _lesson = lesson;
            _advancing = false;
            _metric = 0.0f;
            _lastPosition = _local.transform.position;
            _defenderResetArmed = false;
            _lessonBeganAt = Time.time;
            _marker?.Bind(null);
            SetProgress(0.0f);
            ApplyVerbLock(lesson);

            // ⚠️⚠️ THE PREVIOUS LESSON'S ARMING TIMER IS STOPPED FIRST. See `_armRoutine`: a
            // wait left running from `DefenderReset` topples the can inside the lesson AFTER it,
            // and a can on its side refuses every tag in the game.
            if (_armRoutine != null)
            {
                StopCoroutine(_armRoutine);
                _armRoutine = null;
            }

            // ⚠️ BOTH BEFORE THE SWITCH, NOT AFTER IT. `Lesson.TripRecovery` opens by calling
            // `_local.ApplyTrip()` and `Lesson.DefenderReset` opens by calling `BecomeDefender`;
            // clearing or re-roling afterwards would undo the two lessons whose whole subject is
            // the state being cleared.
            ClearTheLastLessonsMess();

            // ⚠️⚠️ AND THE CAN GOES BACK ON ITS MARK BEFORE THE ROLE IS APPLIED, WHICH IS AN
            // ORDERING FIX AND THE OTHER HALF OF 🧑's *"u cant raise can"*. `BecomeDefender`
            // teleports the student to `_lata.transform.position + back * 1.15`, measured
            // against wherever the can happens to be lying — and `Lata.HostRestore` **moves the
            // can**: its own note says *"IT GOES BACK ON ITS MARK AND THEN STANDS UP"*, because
            // a can that stands up where it was knocked to is a can the next throw cannot miss.
            // `ArmDefenderReset` called that restore one frame AFTER the teleport, so on any
            // route where an earlier lesson had knocked the can off its mark (the THROW and
            // PEKTUS lessons are two of them, and the can rolls) the student was set down beside
            // the empty patch of road the can had just left. `Carrier.StepDefender` needs
            // `Balance.InteractionRadius`, 1.6 m, so holding the key there does nothing at all
            // and there is no message saying why.
            //
            // ⚠️ IT COVERS PUNCH AND LUNGE TOO, AND THERE IT IS NOT ABOUT PLACEMENT. Both are
            // tag lessons, and `RoundDirector.ResolveTag` refuses outright while the can is
            // down. Neither lesson stood it up, so both inherited whatever `DefenderReset` had
            // left — which, if it was skipped or abandoned, is a can on its side.
            //
            // ⚠️ IT RESTORES UNCONDITIONALLY RATHER THAN ONLY WHEN THE CAN IS DOWN, because
            // `HostRestore` is what puts it back on the MARK and an upright can can still be
            // metres from it: `Lata.HostKnockDown` starts a roll and `HostRestore` is the only
            // thing that ends one. PUNCH and LUNGE both stand the dummy relative to
            // `_lata.transform.position` inside `PrepareDummyInFront`, so a drifted can moves
            // the whole exercise off the chalk box it is supposed to be taught inside.
            if (LessonIsTheTayas(lesson) && _lata != null) _lata.HostRestore();

            ApplyLessonRole(lesson);

            // ⚠️ THE DUMMY GOES AWAY AGAIN BETWEEN THE LESSONS THAT WANT IT. Three of the
            // seventeen need a body in front of you; the other fourteen do not, and a character
            // standing on the road for all of them is the *"other shit"* the route was asked to
            // stop showing. `PrepareDummyInFront` brings it back on the frame it is needed.
            if (_dummy != null && _dummy.gameObject.activeSelf) _dummy.gameObject.SetActive(false);

            string title;
            string body;
            string action;

            switch (lesson)
            {
                case Lesson.Look:
                    title = "LOOK AROUND";
                    body = "Move the mouse and find the lata. Your camera is also your aim.";
                    action = "MOUSE  ·  LOOK AND AIM";
                    _marker?.Bind(_lata.transform);
                    break;

                case Lesson.Move:
                    title = "MOVE THROUGH THE STREET";
                    body = "Move four metres. The defender is faster; attackers must plan their route back out.";
                    action = Key("Move") + "  ·  MOVE";
                    break;

                case Lesson.Sprint:
                    title = "SPRINT";
                    body = "Sprint while moving for one second. A full stamina bar buys roughly one crossing of the danger box.";
                    action = Key("Sprint") + " + " + Key("Move");
                    break;

                case Lesson.Jump:
                    title = "JUMP";
                    body = "Jump once. Use it to clear street clutter, not to escape the defender's box.";
                    action = Key("Jump") + "  ·  JUMP";
                    break;

                case Lesson.Throw:
                    PrepareAttackerThrow();
                    title = "THROW AT THE LATA";
                    body = "Hold to charge, aim at the lata, then release. Throwing is safe; retrieving is the risk.";
                    action = Key("SpecialAbility") + "  ·  HOLD, AIM, RELEASE";
                    _marker?.Bind(_lata.transform);
                    break;

                case Lesson.Retrieve:
                    // ⚠️ THE SHOE GOES ON THE ROAD FIRST, WHATEVER THE THROW LESSON LEFT
                    // BEHIND. See `PlaceOwnSlipperOnTheRoad`: skipping the throw arrives here
                    // still holding it, and you cannot pick up what is already in your hand.
                    if (_ownSlipper == null || _ownSlipper.State != SlipperState.Loose)
                        PlaceOwnSlipperTowardTheLata();
                    title = "GET YOUR TSINELAS BACK";
                    body = "Walk to your own slipper and press the pickup key. Holding it inside the box makes you taggable.";
                    action = Key("Grab") + "  ·  PICK UP";
                    _marker?.Bind(_ownSlipper != null ? _ownSlipper.transform : null);
                    break;

                case Lesson.Pektus:
                    PrepareAttackerThrow();
                    title = "CURVE A PEKTUS THROW";
                    body = "Charge another throw, add spin, then release. Strong spin can bank once. The mouse wheel does it too.";
                    // ⚠️ THE LIVE BINDING, NOT THE WORD "ARROWS". The curve is two real actions
                    // in the map as of 2026-08-26 (`PlayerInputReader._curveLeft`), so this
                    // lesson teaches whatever the player has bound, like every other one.
                    action = Key("SpecialAbility") + " + " + Key("CurveLeft") + " / " + Key("CurveRight");
                    _marker?.Bind(_lata.transform);
                    break;

                case Lesson.Shove:
                    // ⚠️ 1.40 m, OUT FROM 1.15. `TrainingStreetProbe` measured the dummy's body
                    // mesh 1.09 m from the eye on this lesson, which is a character filling the
                    // frame rather than one standing in front of you. `Balance.ShoveRange` is
                    // 1.60, so this is still comfortably inside the verb being taught.
                    PrepareDummyInFront(1.40f, attacker: true);
                    _local.Stamina.RefillAndClearFatigue();
                    title = "SHOVE AN ATTACKER";
                    body = "Shove the training dummy. It costs stamina you may need for the run back out.";
                    action = Key("Grab") + "  ·  SHOVE";
                    _marker?.Bind(_dummy != null ? _dummy.transform : null);
                    break;

                case Lesson.AbilityInfo:
                    // ⚠️ THIS IS THE LESSON THE DECK EXISTS FOR, so this is where it appears.
                    UI.Hud.Instance?.SetTrainingDeckHidden(false);
                    title = "READ YOUR HERO KIT";
                    body = "Hold the info key to inspect every power without filling the live HUD with instructions.";
                    action = Key("AbilityInfo") + "  ·  HOLD FOR DETAILS";
                    break;

                case Lesson.Skill1:
                    ResetHeroKit();
                    title = "USE " + AbilityName(HeroAbilitySystem.Slot.Skill1, "SKILL 1");
                    body = AbilityDescription(HeroAbilitySystem.Slot.Skill1);
                    action = Key("Skill1") + "  ·  SKILL 1";
                    break;

                case Lesson.Skill2:
                    ResetHeroKit();
                    title = "USE " + AbilityName(HeroAbilitySystem.Slot.Skill2, "SKILL 2");
                    body = AbilityDescription(HeroAbilitySystem.Slot.Skill2);
                    action = Key("Skill2") + "  ·  SKILL 2";
                    break;

                case Lesson.Ultimate:
                    ResetHeroKit();
                    if (_abilities?.Kit != null)
                        _abilities.Kit.AddUltimateCharge(_abilities.Kit.UltimateCost);
                    title = "USE " + AbilityName(HeroAbilitySystem.Slot.Ultimate, "ULTIMATE");
                    body = "Ultimates are earned by playing the objective. Training fills the meter once so you can learn the cast.";
                    action = Key("Ultimate") + "  ·  ULTIMATE";
                    break;

                case Lesson.DefenderReset:
                    // ⚠️ NO `BecomeDefender()` HERE ANY MORE. `ApplyLessonRole` above already
                    // ran it, from `LessonIsTheTayas`, and now runs it AFTER the can is back on
                    // its mark, which is the whole point of the reordering. The second call was
                    // a duplicate that happened to be the one whose placement was wrong.
                    title = "ROLE SWAP: DEFENDER";
                    body = "You are now the taya. Stay inside the chalk box and hold the pickup key by the down lata to stand it up.";
                    action = Key("Grab") + "  ·  HOLD TO RESET";
                    _marker?.Bind(_lata.transform);
                    _armRoutine = StartCoroutine(ArmDefenderReset());
                    break;

                case Lesson.Punch:
                    // ⚠️ 1.50 m against `Balance.PunchRange` 1.70, for the reason on the shove.
                    PrepareDummyInFront(1.50f, attacker: true);
                    title = "PUNCH A VULNERABLE ATTACKER";
                    body = "The defender's left click is a quick stationary tag. The dummy is holding a slipper inside your box.";
                    action = Key("SpecialAbility") + "  ·  PUNCH";
                    _marker?.Bind(_dummy != null ? _dummy.transform : null);
                    break;

                case Lesson.Lunge:
                    PrepareDummyInFront(3.0f, attacker: true);
                    title = "LUNGE";
                    body = "Hold to charge, release to dash, and sweep through the dummy. Use this when an attacker is running past you.";
                    action = Key("Lunge") + "  ·  HOLD, THEN RELEASE";
                    _marker?.Bind(_dummy != null ? _dummy.transform : null);
                    break;

                case Lesson.TripRecovery:
                    _local.ApplyTrip();
                    _lastTripLeft = _local.TripLeft;
                    title = "RECOVER FROM A FALL";
                    body = "Trips put you on the road. Mash the live jump binding to shorten the knockdown instead of waiting it out.";
                    action = Key("Jump") + "  ·  MASH TO GET UP";
                    break;

                case Lesson.Emote:
                    title = "EMOTE";
                    body = "Hold the wheel, choose an emote, and release. Movement or another action interrupts it.";
                    action = Key("EmoteWheel") + "  ·  HOLD, CHOOSE, RELEASE";
                    break;

                default:
                    title = "TRAINING COMPLETE";
                    body = "You tested movement, stamina, jumping, throwing, retrieval, Pektus, hero powers, both roles, tags, fall recovery and emotes.";
                    action = "ENTER  ·  RETURN TO MAIN MENU";
                    _marker?.Bind(null);

                    // ⚠️ THE END OF THE ROUTE GETS THE MATCH FANFARE, NOT AN EIGHTEENTH PING.
                    // `CompleteLesson` climbs a fifth over seventeen steps and then stops; a
                    // route that ended on the same sound as step sixteen would be seventeen
                    // notifications rather than an arc with a finish on it.
                    GameServices.Audio?.PlayAt("match_win", Vector3.zero);
                    break;
            }

            _hud?.SetLesson((int)lesson, LessonCount, title, body, action,
                            _local.IsDefender ? UiTheme.Defense : UiTheme.Offense);
        }

        /// <summary>
        /// Topples the can so there is something for the student to stand back up.
        ///
        /// ⚠️⚠️ THE RESTORE MOVED OUT OF HERE, INTO `EnterLesson`, AND THAT IS THE ORDERING FIX.
        /// It ran one frame after `BecomeDefender` had already placed the student beside the
        /// can's OLD position, and `HostRestore` teleports the can to its mark. See the note at
        /// the `LessonIsTheTayas` restore for the whole story.
        ///
        /// ⚠️⚠️ AND IT WAITS ON THE FLAG RATHER THAN ON A DURATION. `WaitForSeconds(
        /// Balance.ThrowRestoreCooldown + 0.08f)` was the right length by arithmetic and the
        /// wrong thing to ask: `HostKnockDown` refuses while `Lata.IsProtected`, so anything
        /// that shortens or lengthens that window silently leaves this lesson unarmable, with
        /// `_defenderResetArmed` false, the can standing, and a student holding the pickup key
        /// at an upright can for ever. Polling the real flag cannot drift from the real gate,
        /// and it costs nothing when there is no protection to wait out.
        ///
        /// ⚠️⚠️ THE GUARD IS ELAPSED TIME AND IT WAS A FRAME COUNT FOR ONE COMMIT. A protection
        /// that never expires would spin this coroutine for the rest of the session, so it needs
        /// a ceiling; 600 frames is ten seconds at 60 Hz **and about a second in batchmode**,
        /// where nothing caps the frame rate. `TutorialDefenderProbe` failed on exactly that: the
        /// guard ran out while `IsProtected` was still true, the knockdown was refused, and the
        /// error below fired on a build that was working. **A frame is not a unit of time in this
        /// project**, and `Balance.ThrowRestoreCooldown` is 1.25 s, so 5 s is four times the real
        /// window on any machine.
        /// </summary>
        private IEnumerator ArmDefenderReset()
        {
            for (float waited = 0.0f; _lata.IsProtected && waited < 5.0f; waited += Time.deltaTime)
                yield return null;

            _lata.HostKnockDown(-1);
            _defenderResetArmed = !_lata.IsUpright;

            if (!_defenderResetArmed)
                Debug.LogError("[Training] the can refused to go over, so ROLE SWAP: DEFENDER "
                               + "cannot be completed. IsProtected="
                               + _lata.IsProtected + " IsUpright=" + _lata.IsUpright);

            _armRoutine = null;
        }

        private void PrepareAttackerThrow()
        {
            if (_local.IsDefender) BecomeAttacker();
            if (_ownSlipper != null) _ownSlipper.HostForceEquip(_local);
            if (!_lata.IsUpright) _lata.HostRestore();
        }

        private void BecomeAttacker()
        {
            int defender = _local.PlayerSlot == 0 ? 1 : 0;
            ApplyRoles(defender);
        }

        private void BecomeDefender()
        {
            ApplyRoles(_local.PlayerSlot);

            // ⚠️ THROUGH THE ONE PLACEMENT, so this drop is grounded like every other. It
            // used to write the player's own Y, which is the sole of the foot and not the road.
            if (_ownSlipper != null && _ownSlipper.State == SlipperState.Held)
                PlaceOwnSlipperOnTheRoad(_local.transform.right, 3.0f);
            _carrier?.NotifyHolding(null);
            _local.HoldingSlipper = false;

            Vector3 at = _lata.transform.position + Vector3.back * 1.15f;
            _local.Teleport(at);
            Face(_local, _lata.transform.position);
        }

        private void ApplyRoles(int defenderSlot)
        {
            int roundNumber = defenderSlot + 1;
            int[] scores = new int[Balance.PlayerCount];
            for (int i = 0; i < scores.Length; i++) scores[i] = GameServices.Match.ScoreFor(i);

            GameServices.Match.ApplySnapshot(scores, roundNumber, true);
            GameServices.Round.ApplySnapshot(Balance.RoundTime, true, defenderSlot);
        }

        private void PrepareDummyInFront(float distance, bool attacker)
        {
            if (_dummy == null) return;

            // ⚠️ THE DUMMY IS SHOWN HERE AND NOWHERE ELSE. See `HideTheCast`: the whole street
            // is empty for a training run, and this is the one moment a second body belongs in
            // it. Bringing it back for the lesson that needs it, rather than parking four
            // characters on the road for the whole route, is what "no bots" has to mean for a
            // tutorial that still teaches the shove, the punch and the lunge.
            if (!_dummy.gameObject.activeSelf) _dummy.gameObject.SetActive(true);

            _dummy.IsDefender = !attacker;
            _dummy.RoundActive = true;
            _dummy.HoldingSlipper = attacker;
            _dummy.Intent.Parked = true;

            Vector3 forward = _local.transform.forward;
            forward.y = 0.0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 target = _local.transform.position + forward * distance;
            if (_local.IsDefender)
            {
                Vector3 can = _lata.transform.position;
                float localBack = Mathf.Min(Balance.ConfinementRadius - 0.5f, distance + 1.0f);
                _local.Teleport(can + Vector3.back * localBack);
                target = can + Vector3.back;
                target.x = Mathf.Clamp(target.x, -Balance.ConfinementRadius + 0.5f,
                                       Balance.ConfinementRadius - 0.5f);
                target.z = Mathf.Clamp(target.z, -Balance.ConfinementRadius + 0.5f,
                                       Balance.ConfinementRadius - 0.5f);
            }

            _dummy.Teleport(target);
            Face(_local, target);
            Face(_dummy, _local.transform.position);
        }

        private static void Face(CharacterMotor who, Vector3 point)
        {
            if (who == null) return;
            Vector3 direction = point - who.transform.position;
            direction.y = 0.0f;
            if (direction.sqrMagnitude > 0.01f)
                who.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void ResetHeroKit() => _abilities?.ResetKit();

        private string AbilityName(HeroAbilitySystem.Slot slot, string fallback)
        {
            var ability = Ability(slot);
            return ability != null ? ability.Name.ToUpperInvariant() : fallback;
        }

        private string AbilityDescription(HeroAbilitySystem.Slot slot)
        {
            var ability = Ability(slot);
            return ability != null ? ability.Description : "Activate the highlighted power.";
        }

        private HeroAbility Ability(HeroAbilitySystem.Slot slot)
        {
            var kit = _abilities?.Kit;
            if (kit == null) return null;
            if (slot == HeroAbilitySystem.Slot.Skill1) return kit.Skill1;
            if (slot == HeroAbilitySystem.Slot.Skill2) return kit.Skill2;
            return kit.Ultimate;
        }

        private static string Key(string action) => "[" + Hud.KeyLabelFor(action) + "]";

        private void ExitTraining()
        {
            GameLaunch.GuidedTutorial = false;
            Hitstop.End();
            SceneFlow.Go(SceneFlow.MainMenu);
        }

        /// <summary>
        /// Takes the other three off the street until a lesson asks for one.
        ///
        /// ⚠️⚠️ 🧑, 2026-08-26: *"i dont want there to be bots"*. Parking their intent, which is
        /// what this used to do on its own, stops them PLAYING and leaves them standing on the
        /// attacker line for the whole route: three motionless characters, three nameplates and
        /// three tsinelas on the ground, all of them things the player is being asked not to look
        /// at while a card tells them where to look. `PrepareDummyInFront` brings one back for
        /// the three lessons that need a body to hit.
        ///
        /// ⚠️ THE INTENT IS STILL PARKED AS WELL AS THE OBJECT DISABLED. `Configure` does that
        /// above, and it has to stay: the dummy comes back for a lesson and must arrive with an
        /// empty input table rather than whatever it was holding when it was switched off.
        ///
        /// ⚠️ AND THEY ARE DISABLED, NOT DESTROYED. `SliceRunner` holds the seat array and
        /// `RoundDirector` holds the registry; destroying a registered seat mid-round is a null
        /// in four loops, and the route may hand one back at any point.
        /// </summary>
        private void HideTheCast()
        {
            foreach (var seat in _seats)
            {
                if (seat == null || seat == _local) continue;
                if (seat.gameObject.activeSelf) seat.gameObject.SetActive(false);
            }

            // ⚠️ AND THEIR AMMUNITION WITH THEM. Three spare tsinelas on the road is three things
            // that light up, three the pickup prompt can fire on, and three the retrieval lesson
            // can be completed with instead of the one the marker is pointing at. The lesson says
            // "your own slipper"; leaving the others out makes that sentence false.
            if (_slippers == null) return;

            foreach (var slipper in _slippers)
            {
                if (slipper == null || slipper == _ownSlipper) continue;
                if (slipper.gameObject.activeSelf) slipper.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Which tsinelas is actually the player's, asked AFTER the round has dealt them.
        ///
        /// ⚠️ THE CARRIER IS ASKED FIRST BECAUSE IT CANNOT BE WRONG. What is in the hand is
        /// the answer to "your own slipper" for every lesson on the route; `OwnerSlot` is the
        /// fallback for the frame before the equip, and matching on neither leaves the retrieval
        /// lesson pointing at nothing, which is worth logging rather than silently surviving.
        /// </summary>
        private void ResolveOwnSlipper()
        {
            _ownSlipper = _carrier != null ? _carrier.Held : null;

            if (_ownSlipper == null && _slippers != null)
            {
                foreach (var slipper in _slippers)
                {
                    if (slipper != null && slipper.OwnerSlot == _local.PlayerSlot)
                    {
                        _ownSlipper = slipper;
                        break;
                    }
                }
            }

            if (_ownSlipper == null)
                Debug.LogWarning("[Training] the player owns no tsinelas; the retrieval lesson " +
                                 "will have nothing to point at.");
        }

        /// <summary>
        /// Puts the player's own tsinelas on the road in front of them, so the retrieval lesson
        /// has something to retrieve.
        ///
        /// ⚠️⚠️ WITHOUT THIS THE LESSON POINTED AT THE PLAYER'S OWN HAND. The route reaches
        /// RETRIEVE with the shoe still held whenever the throw lesson was skipped with N, and
        /// you cannot pick up what you are already holding: the lesson could never complete, and
        /// the objective marker bound to the slipper drew its 0.70 m ring **0.55 m from the eye**
        /// (`TrainingStreetProbe`, viewport 0.83, 0.29). That is the reported *"i clicked N skip
        /// and this shit showed up wtf is this yellow shit on me??"*, and it was the marker
        /// rather than the shoe.
        ///
        /// ⚠️ IT IS GROUNDED THROUGH `Slipper.GroundY` RATHER THAN THE PLAYER'S OWN Y. A LOOSE
        /// slipper does not fall: `Slipper.FixedUpdate` only integrates a flight, so a drop point
        /// written in the air stays in the air forever. Every other placement in this file had
        /// the same hole.
        ///
        /// ⚠️ AND IT IS CLAMPED INSIDE THE BOX, because a player facing the wall on the last
        /// lesson would otherwise be sent to fetch a tsinelas from outside the arena.
        /// </summary>
        private void PlaceOwnSlipperOnTheRoad(Vector3 direction, float distance)
        {
            if (_ownSlipper == null) return;

            direction.y = 0.0f;
            if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
            direction.Normalize();

            Vector3 at = _local.transform.position + direction * distance;

            // ⚠️ THE ARENA, NOT THE DANGER BOX. `Balance.ConfinementRadius` is 7.0 and the
            // attacker line stands at z = 9.0, well outside it: clamping to the box would have
            // put the retrieval lesson's tsinelas BEHIND a player standing on his own mark. These
            // are the arena bounds `MatchInstaller` writes, with a metre of margin, and they are
            // a guard against a wall rather than the thing that chooses the spot.
            at.x = Mathf.Clamp(at.x, -8.0f, 8.0f);
            at.z = Mathf.Clamp(at.z, -12.0f, 12.0f);
            at.y = Slipper.GroundY(at) + _ownSlipper.RestHeight;

            _ownSlipper.ApplySnapshotState(SlipperState.Loose, null, at,
                _ownSlipper.transform.rotation, Vector3.zero, 0.0f, SlipperAffinity.Normal, -1);

            _carrier?.NotifyHolding(null);
            _local.HoldingSlipper = false;
        }

        /// <summary>
        /// The retrieval lesson's own placement: on the line between the player and the lata,
        /// which is where a thrown tsinelas actually ends up.
        ///
        /// ⚠️ HALF THE DISTANCE, CAPPED AT FOUR METRES. `docs/VISION.md` § 0: *"the tension is
        /// the retrieval"*, and the run this lesson is teaching is the run IN toward the can. A
        /// shoe dropped behind the player teaches the walk and not the risk. Half the gap keeps
        /// it inside the street whatever mark the player is standing on, and the four metre cap
        /// stops it landing on top of the lata when they are already close.
        /// </summary>
        private void PlaceOwnSlipperTowardTheLata()
        {
            Vector3 toLata = _lata.transform.position - _local.transform.position;
            toLata.y = 0.0f;

            float reach = Mathf.Min(4.0f, Mathf.Max(2.0f, toLata.magnitude * 0.5f));
            PlaceOwnSlipperOnTheRoad(toLata, reach);
        }

        /// <summary>The student landed a tag. Which lesson cares is decided in `EvaluateLesson`.</summary>
        private void OnSomebodyTagged(int tayaSlot, int victimSlot)
        {
            if (_local != null && tayaSlot == _local.PlayerSlot) _lastTagByStudentAt = Time.time;
        }

        private void OnDestroy()
        {
            GameLaunch.GuidedTutorial = false;

            // ⚠️ UNSUBSCRIBED WITH THE ROUTE. `RoundDirector` outlives this component, and a
            // handler on a destroyed MonoBehaviour throws once per tag for the rest of the
            // session. `MatchInstaller` records the same fault on `LataRestored`.
            if (GameServices.Round != null) GameServices.Round.Tagged -= OnSomebodyTagged;

            // ⚠️⚠️ THE VERB LOCK IS RELEASED WITH THE ROUTE, AND NOT DOING THIS WOULD SHIP A
            // PLAYER WHO CANNOT THROW. `InputIntent` belongs to the SEAT, which outlives this
            // component; a player who quits training on lesson three and starts a match would
            // otherwise carry a three-verb character into it with nothing on screen to say why.
            if (_local != null) _local.Intent.AllowOnly(null);

            if (_marker != null) Destroy(_marker.gameObject);
            if (_hud != null) Destroy(_hud.gameObject);
        }
    }

    /// <summary>
    /// The training screen. Deliberately separate from `Hud`, and now deliberately unlike it.
    ///
    /// ⚠️⚠️ 🧑, 2026-08-26: *"ui for it really sucks i think u can make it bettter"* and *"make
    /// it an actual dedicated tutorial not js a copy pasted shit from the game"*. Three things
    /// were wrong and only one of them was this class.
    ///
    /// 1. The MATCH was still on screen behind it: a frozen 90 s clock, ROUND 1 / 8, a
    ///    scoreboard of parked seats and a lata alert firing over the card. That half is
    ///    `Hud.StripToTrainingChrome` and `MatchInstaller`, not here.
    /// 2. The keys were raw binding paths. `[2DVECTOR(MODE:2)]` is what `Hud.KeyLabel` returned
    ///    for a composite action's head; fixed at the source, and the keys now draw as KEY CAPS
    ///    rather than as bracketed words in a sentence, which is what every game that teaches a
    ///    control does and what makes one scannable at a glance.
    /// 3. The card itself was four labels and a hairline: no sense of where you were in the
    ///    route, and one 8 px bar doing double duty as the lesson's progress and as the card's
    ///    bottom border.
    ///
    /// ⚠️ THE ROUTE RAIL IS THE PART THAT MAKES IT A TUTORIAL RATHER THAN A PROMPT. Seventeen
    /// pips, one per lesson, lit behind you and dim ahead: *"03 / 17"* is a fact you have to
    /// read, and the rail is the same fact you can see. It also makes the length of the route
    /// honest before a player commits to it.
    ///
    /// ⚠️ IT DRAWS IN THE GAME'S OWN LANGUAGE AND NOT A NEW ONE. Wood, amber, cream, ink
    /// (`docs/VISION.md` § 6): anything here in a different visual language would be the thing
    /// that looks broken, not the thing that looks new.
    /// </summary>
    /// <remarks>⚠️ PUBLIC SO THE EDITOR CAN PHOTOGRAPH IT. `TrainingCardProbe` builds the REAL
    /// card rather than a mock, for the reason `HeroUiProbe` records about the inspect tray: a
    /// mock photographs whatever the probe author believed the layout was, which is the one thing
    /// a screenshot is supposed to rule out. This card has now been rejected twice on its
    /// layout.</remarks>
    public sealed class GuidedTrainingHud : MonoBehaviour
    {
        private const float CardWidth = 690.0f;
        private const float Pad = 26.0f;

        /// <summary>Inner width available to anything inside the card.</summary>
        private const float Inner = CardWidth - Pad * 2.0f;

        private Text _counter;
        private Text _title;
        private Text _body;
        private Text _complete;
        private Image _fill;
        private RectTransform _keyRow;
        private readonly System.Collections.Generic.List<Image> _pips =
            new System.Collections.Generic.List<Image>();

        private float _completeLeft;

        public static GuidedTrainingHud Build(Transform owner)
        {
            var go = new GameObject("GuidedTrainingHud");
            go.transform.SetParent(owner, false);
            var hud = go.AddComponent<GuidedTrainingHud>();
            hud.BuildUi();
            return hud;
        }

        private void BuildUi()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 240;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
            scaler.matchWidthOrHeight = 1.0f;

            // ⚠️ THE SAME ASPECT GUARD THE REST OF THE GAME'S UI USES, or the card drifts
            // against the HUD it sits beside on anything that is not 16:9.
            AspectSafeCanvas.Apply(scaler);

            // ⚠️⚠️ THE CARD IS A LAYOUT NOW AND ITS HEIGHT IS ITS CONTENT'S. The first version
            // placed six rows at hand-written offsets inside a fixed 274 px box, and 🧑 answered
            // it with *"the ui has problems like big open space"*. Absolute offsets cannot be
            // right for a card whose title is one line on one lesson and whose body is three on
            // another: every row that came up short left a hole, and a row that came up long
            // drew over its neighbour. A `VerticalLayoutGroup` under a `ContentSizeFitter` makes
            // dead space impossible by construction, because there is no space that is not a
            // row, and it means a lesson with a longer sentence grows the card instead of
            // spilling out of it.
            var panelGo = new GameObject("ObjectiveCard");
            panelGo.transform.SetParent(transform, false);
            var panel = panelGo.AddComponent<Image>();
            panel.sprite = GodotTheme.Box(UiTheme.WoodDark, UiTheme.Amber,
                                          GodotTheme.WoodBorderWidth,
                                          GodotTheme.WoodCornerRadius);
            panel.type = Image.Type.Sliced;
            panel.raycastTarget = false;

            var rt = panel.rectTransform;
            rt.anchorMin = new Vector2(0.0f, 1.0f);
            rt.anchorMax = new Vector2(0.0f, 1.0f);
            rt.pivot = new Vector2(0.0f, 1.0f);
            rt.anchoredPosition = new Vector2(36.0f, -36.0f);
            rt.sizeDelta = new Vector2(CardWidth, 0.0f);

            var column = panelGo.AddComponent<VerticalLayoutGroup>();
            column.childAlignment = TextAnchor.UpperLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            column.spacing = 10.0f;
            column.padding = new RectOffset((int)Pad, (int)Pad, (int)Pad, (int)Pad);

            var fitter = panelGo.AddComponent<ContentSizeFitter>();

            // ⚠️ VERTICAL ONLY. The width is a design decision (a card that changes width with
            // the sentence in it reads as broken), the height is a consequence.
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ---- header row: the word on the left, the count on the right ----
            var header = Row(panelGo.transform, "HeaderRow", 30.0f);
            var headerBg = header.gameObject.AddComponent<Image>();
            headerBg.sprite = GodotTheme.Plain(6);
            headerBg.type = Image.Type.Sliced;
            headerBg.color = new Color(UiTheme.Amber.r, UiTheme.Amber.g, UiTheme.Amber.b, 0.16f);
            headerBg.raycastTarget = false;

            var headerRow = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerRow.childAlignment = TextAnchor.MiddleLeft;
            headerRow.childControlWidth = true;
            headerRow.childControlHeight = true;
            headerRow.childForceExpandWidth = true;
            headerRow.childForceExpandHeight = true;
            headerRow.padding = new RectOffset(12, 12, 0, 0);

            var word = Label(header, 19, UiTheme.Amber, TextAnchor.MiddleLeft);
            word.text = "TRAINING";

            _counter = Label(header, 19, UiTheme.Highlight, TextAnchor.MiddleRight);
            _counter.text = "01 / 17";

            // ---- the route rail ----
            BuildRail(panelGo.transform, GuidedTraining.LessonCount);

            // ---- title ----
            _title = Label(panelGo.transform, 34, UiTheme.Cream, TextAnchor.UpperLeft);
            _title.text = "TRAINING";

            // ⚠️ A ROW STILL NEEDS A HEIGHT ON THE FIRST PASS. A legacy `Text` reports a
            // preferred height of ZERO before it has a width, which is the exact fault
            // `Hud.BuildClock`'s note records: the row collapses and the card closes over it.
            Box(_title, 42.0f);

            // ---- body ----
            _body = Label(panelGo.transform, 20, UiTheme.CreamMuted, TextAnchor.UpperLeft);
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.verticalOverflow = VerticalWrapMode.Overflow;
            _body.text = "";

            // ⚠️⚠️ A FLOOR ONLY, SO THE BODY IS AS TALL AS ITS SENTENCE AND NO TALLER. A fixed
            // two-line box left a hole under every one-line lesson, which is the same fault as
            // the one this whole card was rebuilt for, one row further down. `preferredHeight` is
            // deliberately left at -1 so `LayoutUtility` falls through to the `Text`'s own
            // measurement, and the floor covers the first pass, before the label has a width and
            // reports zero.
            var bodyBox = _body.gameObject.AddComponent<LayoutElement>();
            bodyBox.minHeight = 26.0f;
            bodyBox.preferredHeight = -1.0f;

            // ---- the key caps ----
            var keyGo = new GameObject("KeyRow", typeof(RectTransform));
            keyGo.transform.SetParent(panelGo.transform, false);
            _keyRow = keyGo.GetComponent<RectTransform>();

            var keyLayout = keyGo.AddComponent<HorizontalLayoutGroup>();
            keyLayout.childAlignment = TextAnchor.MiddleLeft;
            keyLayout.childControlWidth = true;
            keyLayout.childControlHeight = true;
            keyLayout.childForceExpandWidth = false;
            keyLayout.childForceExpandHeight = false;
            keyLayout.spacing = 8.0f;

            var keyBox = keyGo.AddComponent<LayoutElement>();
            keyBox.minHeight = 36.0f;
            keyBox.preferredHeight = 36.0f;

            // ---- the lesson's own progress ----
            var barBack = new GameObject("ProgressBack");
            barBack.transform.SetParent(panelGo.transform, false);
            var back = barBack.AddComponent<Image>();
            back.sprite = GodotTheme.Plain(4);
            back.type = Image.Type.Sliced;
            back.color = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.80f);
            back.raycastTarget = false;

            var barBox = barBack.AddComponent<LayoutElement>();
            barBox.minHeight = 12.0f;
            barBox.preferredHeight = 12.0f;

            var fillGo = new GameObject("ProgressFill");
            fillGo.transform.SetParent(barBack.transform, false);
            _fill = fillGo.AddComponent<Image>();
            _fill.sprite = GodotTheme.Plain(4);
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.color = UiTheme.Offense;
            _fill.raycastTarget = false;
            _fill.fillAmount = 0.0f;
            MenuKit.Stretch(_fill.rectTransform);

            // ---- centre flash and the two route controls ----
            _complete = Free(transform, 40, UiTheme.Highlight,
                             new Vector2(0.5f, 0.5f), new Vector2(0.0f, 150.0f),
                             new Vector2(760.0f, 60.0f));
            _complete.alignment = TextAnchor.MiddleCenter;
            _complete.text = "LESSON COMPLETE";
            _complete.enabled = false;

            // ⚠️⚠️ THE ROUTE CONTROLS ARE THE CARD'S LAST ROW, NOT A STRIP AT THE BOTTOM OF THE
            // SCREEN. Bottom-centre belongs to the ability deck, and putting them there drew them
            // straight over the Q / E / F tiles: 🧑, off the 4.69 player, *"the skills are covered
            // there"*. The deck is the one HUD element a hero lesson needs the player to read, so
            // a tutorial that hides it is worse than one with no footer at all.
            //
            // ⚠️ INSIDE THE CARD RATHER THAN FLOATING UNDER IT. The card's height is its
            // content's now, so anything positioned relative to its bottom edge has to be chased
            // every frame; a row in the same column cannot be in the wrong place by construction.
            BuildFooter(panelGo.transform);
        }

        /// <summary>One row of the card, sized by the layout unless it says otherwise.</summary>
        private static Transform Row(Transform parent, string name, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var box = go.AddComponent<LayoutElement>();
            box.minHeight = height;
            box.preferredHeight = height;
            return go.transform;
        }

        /// <summary>
        /// ⚠️ BOTH ENDS OF THE HEIGHT, ALWAYS. `LayoutUtility.GetPreferredHeight` is
        /// `Max(minHeight, preferredHeight)`, so writing one of the two is how a box ends up a
        /// size nobody asked for. That is the whole of the hero picker's dead band, recorded in
        /// `docs/TODO.md` § 13.7, and it is the same class of mistake this card just made.
        /// </summary>
        private static void Box(Component on, float height)
        {
            var box = on.gameObject.AddComponent<LayoutElement>();
            box.minHeight = height;
            box.preferredHeight = height;
        }

        /// <summary>
        /// One pip per lesson, sized to fit whatever `GuidedTraining.LessonCount` is.
        ///
        /// ⚠️ A HORIZONTAL LAYOUT RATHER THAN SOLVED OFFSETS, so a lesson added tomorrow
        /// re-spaces the rail instead of overflowing the card.
        /// </summary>
        private void BuildRail(Transform parent, int count)
        {
            var railGo = new GameObject("RouteRail", typeof(RectTransform));
            railGo.transform.SetParent(parent, false);

            var rail = railGo.AddComponent<HorizontalLayoutGroup>();
            rail.childAlignment = TextAnchor.MiddleCenter;
            rail.childControlWidth = true;
            rail.childControlHeight = true;
            rail.childForceExpandWidth = true;
            rail.childForceExpandHeight = true;
            rail.spacing = 4.0f;

            var railBox = railGo.AddComponent<LayoutElement>();
            railBox.minHeight = 7.0f;
            railBox.preferredHeight = 7.0f;

            for (int i = 0; i < count; i++)
            {
                var pipGo = new GameObject($"Pip{i}");
                pipGo.transform.SetParent(railGo.transform, false);

                var pip = pipGo.AddComponent<Image>();
                pip.sprite = GodotTheme.Plain(2);
                pip.type = Image.Type.Sliced;
                pip.color = RailDim;
                pip.raycastTarget = false;
                _pips.Add(pip);
            }
        }

        private static readonly Color RailDim = new Color(UiTheme.Cream.r, UiTheme.Cream.g,
                                                          UiTheme.Cream.b, 0.18f);

        /// <summary>
        /// The two route controls.
        ///
        /// ⚠️⚠️ NOT BOTTOM-CENTRE. That lane belongs to the ability deck, and putting this strip
        /// there drew it straight over the Q / E / F tiles: 🧑, off the 4.69 player, *"the skills
        /// are covered there"*. The deck is the one HUD element a hero lesson needs the player to
        /// be able to read, so a tutorial that hides it is worse than one with no footer at all.
        ///
        /// ⚠️ TOP-LEFT, UNDER THE CARD, so every piece of tutorial furniture is in one corner and
        /// the rest of the screen is the game. Anchored to the card's own edge rather than to a
        /// screen offset, because the card's height is its content's now and a fixed offset would
        /// drift the moment a lesson had a longer sentence.
        /// </summary>
        private void BuildFooter(Transform parent)
        {
            var footGo = new GameObject("RouteControls", typeof(RectTransform));
            footGo.transform.SetParent(parent, false);

            var plate = footGo.AddComponent<Image>();
            plate.sprite = GodotTheme.Plain(6);
            plate.type = Image.Type.Sliced;
            plate.color = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.55f);
            plate.raycastTarget = false;

            var layout = footGo.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            // ⚠️⚠️ 30, AND THE OLD 10 IS THE ENTIRE BUG. 🧑 2026-08-26, with a crop of this bar:
            // *"confusing to look at this tutorial ui, didnt know clicking n would let u skip it
            // or backspace would let u quit"*.
            //
            // It read `[N] SKIP · [BACKSPACE] QUIT TRAINING` as five children of ONE row at a
            // uniform 10 px gap, so the gap between a key and ITS OWN action was the same as the
            // gap between the two unrelated pairs. Nothing in the spacing said which word went
            // with which cap, and proximity is the only thing that ever says so. The two pairs
            // are sub-rows now: 7 px inside a pair, 30 px between them, so the grouping is
            // visible before a single word is read.
            //
            // ⚠️ AND THE ACTION IS NO LONGER THE QUIET HALF. `KeyCap` draws a cream plate with an
            // amber border and ink lettering, which is the loudest thing in this bar, while the
            // action word was `CreamMuted` — so the eye landed on the box, read "N" as the label
            // of a button, and skipped the grey word that says what it does. The cap is the
            // MODIFIER and the verb is the message; the verb gets full cream now.
            layout.spacing = 30.0f;
            layout.padding = new RectOffset(12, 12, 4, 4);

            var box = footGo.AddComponent<LayoutElement>();
            box.minHeight = 42.0f;
            box.preferredHeight = 42.0f;

            // ⚠️ "SKIP LESSON", NOT "SKIP". On its own "SKIP" reads as skipping the whole
            // tutorial, which is what BACKSPACE does; naming the unit each key acts on is what
            // separates them. The two verbs are now the difference between one step and the lot.
            ControlPair(footGo.transform, SkipKeyLabel, "SKIP LESSON");
            ControlPair(footGo.transform, QuitKeyLabel, "QUIT TRAINING");
        }

        /// <summary>
        /// The label on the key that advances one lesson, and the one that leaves training.
        ///
        /// ⚠️⚠️ THEY ARE CONSTANTS SO THE BAR AND THE READER CANNOT DRIFT APART. `Update` reads
        /// `keyboard.nKey` and `keyboard.backspaceKey` directly, and these two strings were typed
        /// into `BuildFooter` by hand: two independent statements of the same fact, and
        /// `docs/VISION.md` § 3 is blunt that a screen teaching the wrong key is worse than one
        /// teaching none. These are hard-wired rather than rebindable on purpose (they are
        /// tutorial chrome, not gameplay verbs), so the binding cannot be asked for its label the
        /// way `Hud.KeyLabel` asks; naming them once is the next best guarantee.
        /// </summary>
        private const string SkipKeyLabel = "N";

        private const string QuitKeyLabel = "BACKSPACE";

        /// <summary>
        /// One control: the key, then what it does, tight enough together to read as a unit.
        /// </summary>
        private static void ControlPair(Transform parent, string key, string action)
        {
            var pairGo = new GameObject($"Control_{key}", typeof(RectTransform));
            pairGo.transform.SetParent(parent, false);

            var row = pairGo.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            row.spacing = 7.0f;

            KeyCap(pairGo.transform, key);
            Chip(pairGo.transform, action, UiTheme.Cream);
        }

        public void SetLesson(int lesson, int total, string title, string body, string action,
                              Color role)
        {
            _counter.text = lesson >= total
                ? "COMPLETE"
                : $"{lesson + 1:00} / {total:00}";

            _title.text = title;
            _body.text = body;

            for (int i = 0; i < _pips.Count; i++)
            {
                if (_pips[i] == null) continue;
                _pips[i].color = i < lesson ? UiTheme.Amber
                               : i == lesson ? UiTheme.Highlight
                               : RailDim;
            }

            RebuildKeys(action, role);
        }

        /// <summary>
        /// Turns `"[LEFT SHIFT] + [WASD]"` into key caps and the words between them.
        ///
        /// ⚠️⚠️ THE BRACKETS ARE THE CONTRACT WITH `GuidedTraining.Key`, which is the only thing
        /// that writes them. Anything inside a pair of square brackets is a CONTROL and gets a
        /// cap; everything else is prose and stays prose. That keeps one line of lesson text
        /// readable as a sentence in the source while drawing as something scannable on screen,
        /// and it means a lesson that names two keys needs no new API.
        ///
        /// ⚠️ THE CAPS ARE REBUILT, NOT POOLED. This runs once per lesson, seventeen times in a
        /// whole route, so a pool would be optimising a thing that happens less often than the
        /// player blinks.
        /// </summary>
        private void RebuildKeys(string action, Color role)
        {
            if (_keyRow == null) return;

            for (int i = _keyRow.childCount - 1; i >= 0; i--)
                Destroy(_keyRow.GetChild(i).gameObject);

            if (string.IsNullOrEmpty(action)) return;

            int at = 0;
            while (at < action.Length)
            {
                int open = action.IndexOf('[', at);

                if (open < 0)
                {
                    AddWords(action.Substring(at), role);
                    break;
                }

                if (open > at) AddWords(action.Substring(at, open - at), role);

                int close = action.IndexOf(']', open + 1);
                if (close < 0)
                {
                    AddWords(action.Substring(open), role);
                    break;
                }

                string key = action.Substring(open + 1, close - open - 1).Trim();
                if (key.Length > 0) KeyCap(_keyRow, key);

                at = close + 1;
            }
        }

        private void AddWords(string words, Color role)
        {
            string trimmed = words.Trim();
            if (trimmed.Length == 0) return;
            Chip(_keyRow, trimmed, role);
        }

        /// <summary>A control, drawn as a key on a keyboard rather than as a word in brackets.</summary>
        private static void KeyCap(Transform parent, string key)
        {
            var go = new GameObject($"Key_{key}");
            go.transform.SetParent(parent, false);

            var plate = go.AddComponent<Image>();
            plate.sprite = GodotTheme.Box(UiTheme.Cream, UiTheme.Amber, 3, 6);
            plate.type = Image.Type.Sliced;
            plate.raycastTarget = false;

            var text = new GameObject("Cap");
            text.transform.SetParent(go.transform, false);
            var label = text.AddComponent<Text>();
            label.font = MenuKit.Font;
            label.fontSize = 20;
            label.fontStyle = FontStyle.Bold;
            label.color = UiTheme.Ink;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.text = key;
            MenuKit.Stretch(label.rectTransform);

            // ⚠️ SIZED OFF THE STRING, NOT OFF A LAYOUT PASS. `BACKSPACE` and `Q` sit in the same
            // row, and letting UGUI measure the text would make one cap a square and the other a
            // sliver. 15 px per character with a 42 px floor keeps a single letter square and a
            // word legible without either overrunning the card.
            var box = go.AddComponent<LayoutElement>();
            box.preferredWidth = Mathf.Max(42.0f, 15.0f * key.Length + 20.0f);
            box.preferredHeight = 34.0f;
            box.minHeight = 34.0f;
        }

        private static void Chip(Transform parent, string words, Color? colour = null)
        {
            var go = new GameObject("Words");
            go.transform.SetParent(parent, false);

            var label = go.AddComponent<Text>();
            label.font = MenuKit.Font;
            label.fontSize = 19;
            label.color = colour ?? UiTheme.CreamMuted;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.text = words;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = UiTheme.Ink;
            outline.effectDistance = new Vector2(2.0f, -2.0f);

            var box = go.AddComponent<LayoutElement>();
            box.preferredWidth = 10.5f * words.Length + 8.0f;
            box.preferredHeight = 34.0f;
            box.minHeight = 34.0f;
        }

        public void SetProgress(float ratio)
        {
            if (_fill != null) _fill.fillAmount = Mathf.Clamp01(ratio);
        }

        public void FlashComplete()
        {
            _completeLeft = 0.70f;
            _complete.enabled = true;
            _complete.rectTransform.localScale = Vector3.one * 1.22f;
        }

        private void Update()
        {
            if (_completeLeft <= 0.0f) return;
            _completeLeft = Mathf.Max(0.0f, _completeLeft - Time.unscaledDeltaTime);
            float t = 1.0f - _completeLeft / 0.70f;
            _complete.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.22f, 1.0f, t);
            if (_completeLeft <= 0.0f) _complete.enabled = false;
        }

        /// <summary>
        /// A label that a layout group owns. No offsets, no box: the row it is in decides both.
        ///
        /// ⚠️ THIS REPLACED A VERSION THAT TOOK AN ANCHORED POSITION AND A `sizeDelta`, which is
        /// what let the card grow a hole in the middle of it. A label positioned by hand inside
        /// a layout is a second opinion about where it goes, and the layout does not know it has
        /// been overruled.
        /// </summary>
        private static Text Label(Transform parent, int size, Color colour, TextAnchor align)
        {
            var go = new GameObject("TrainingText");
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.font = MenuKit.Font;
            text.fontSize = size;
            text.color = colour;
            text.alignment = align;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = UiTheme.Ink;
            outline.effectDistance = new Vector2(3.0f, -3.0f);

            return text;
        }

        /// <summary>
        /// A label that answers to nobody, for the one piece of this screen that is not in the
        /// card: the centre-screen LESSON COMPLETE flash.
        /// </summary>
        private static Text Free(Transform parent, int size, Color colour,
                                 Vector2 anchor, Vector2 offset, Vector2 box)
        {
            var go = new GameObject("TrainingText");
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.font = MenuKit.Font;
            text.fontSize = size;
            text.color = colour;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = UiTheme.Ink;
            outline.effectDistance = new Vector2(3.0f, -3.0f);

            var rt = text.rectTransform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = box;
            return text;
        }
    }

    /// <summary>
    /// A low, non-colliding beacon that points at the current lesson target.
    ///
    /// ⚠️⚠️ IT USED TO BE A 5.2 m COLUMN AND THAT IS THE *"what are these big ass lines"* IN
    /// THE PLAYED BUILD. A `PrimitiveType.Cylinder` is two units tall, so a local scale of 2.6
    /// draws 5.2 m of pole, and the pulse on the parent took it to 5.7. This game is FPP for
    /// every Person (`CLAUDE.md` § 4), the eye sits at about 1.6 m and a marker bound to a
    /// tsinelas is met at three or four metres: the arithmetic puts the top of that pole
    /// straight off the top of the frame, so the one thing the marker exists to point AT was
    /// the thing it was standing in front of.
    ///
    /// ⚠️ IT IS A GROUND RING AND A SMALL FLOATING PIP NOW, AND NEITHER CROSSES THE HORIZON.
    /// The ring says WHERE on a floor the player is already reading, and the pip bobbing under
    /// eye height is what carries the eye to it from across the street.
    /// </summary>
    internal sealed class TrainingMarker : MonoBehaviour
    {
        /// <summary>Ring radius. Deliberately NOT `Balance.PickupRadius`: the ring is a "look
        /// there", not a "stand here", and drawing it at the real pickup window would teach a
        /// distance no lesson actually checks.</summary>
        private const float RingRadius = 0.70f;

        /// <summary>How high the pip floats. Under a 1.6 m eye height on purpose, so it is
        /// always something the player looks slightly DOWN at rather than a shape crossing the
        /// sky.</summary>
        private const float PipHeight = 1.05f;

        /// <summary>
        /// Closer than this to the eye and the marker draws nothing.
        ///
        /// ⚠️⚠️ A POINTER YOU ARE STANDING INSIDE IS NOT A POINTER, IT IS A WALL. Measured by
        /// `TrainingStreetProbe`: on the retrieval lesson the ring sat **0.55 m** from the
        /// camera at viewport (0.83, 0.29), and on the shove and reset lessons it sat at 1.15 m
        /// filling the bottom of the frame. 🧑 photographed all three and could not tell what he
        /// was looking at: *"genuinely wtf is happening what is that haha"*. A 0.70 m ring seen
        /// from half a metre is a coloured plane over the whole screen, which is
        /// `docs/VISION.md` § 2 rule 5 with a tutorial marker in the role of the ultimate.
        ///
        /// ⚠️ 1.10 m, AND THE BOUND IS THE THREE LESSONS THAT PUT SOMETHING CLOSE ON PURPOSE:
        /// the shove at 1.40 m, the punch at 1.50 and the defender reset with the lata at 1.15.
        /// A marker on the ground in front of you at those distances is doing its job. Half a
        /// metre is not a distance any lesson asks for, so anything that near is the pointer
        /// having ended up on the player.
        /// </summary>
        private const float HideWithin = 1.10f;

        private Transform _target;
        private Transform _pip;
        private Light _light;
        private Renderer[] _shapes;
        private bool _drawn = true;
        private Camera _eye;

        public static TrainingMarker Build()
        {
            var go = new GameObject("TrainingObjectiveMarker");
            var marker = go.AddComponent<TrainingMarker>();
            marker.BuildVisual();
            go.SetActive(false);
            return marker;
        }

        public void Bind(Transform target)
        {
            _target = target;
            gameObject.SetActive(target != null);
        }

        private void BuildVisual()
        {
            // ⚠️⚠️ A FLAT FAN, AND `NovaShell` WAS THE WHOLE OF *"wtf is this yellow shit on
            // me"*. `VfxShapes.Lay` scales X and Z by the radius and LEAVES Y AT 1.0, because
            // every other caller in the game hands it a flat mesh (`Crystal`, `Star`, `Splat`,
            // `Ring`, all fans at y = 0). `NovaShell` is a unit SPHERE shell, y from -1 to +1, so
            // this line drew a translucent amber ball 1.40 m wide and **2.00 m tall** standing on
            // the target, half of it under the road. `TrainingStreetProbe` printed its bounds:
            // `size (1.40, 2.00, 1.39)`. Met at half a metre it is a yellow wall; met at the
            // dummy's feet it is a yellow cone around her, which is what he photographed twice.
            //
            // ⚠️ THIS IS THE SECOND OVERSIZED MARKER, so the shape is now measured rather than
            // argued. `docs/TODO.md` § 13.6 replaced a 5.2 m pole with "a 0.70 m ground ring",
            // and the ring it reached for was a ball. `Crystal(22)` is the flat 22-sided fan the
            // hazard footprints use, unit radius, ZERO height.
            var ring = Visual.VfxShapes.Lay(transform, "ObjectiveRing",
                                            Visual.VfxShapes.Crystal(22), RingRadius, 0.04f);
            Visual.VfxMaterial.Ghost(ring.GetComponent<Renderer>(),
                new Color(UiTheme.Highlight.r, UiTheme.Highlight.g, UiTheme.Highlight.b, 0.32f),
                1.5f);
            Visual.VfxMaterial.StripCollider(ring);

            var pip = Visual.VfxShapes.Lay(transform, "ObjectivePip",
                                           Visual.VfxShapes.Star(5, 0.44f), 0.24f, PipHeight);

            // ⚠️ TIPPED UPRIGHT. `Lay` places a shape FLAT, which is right for the ring and
            // wrong for the pip: a flat star seen from eye height is a line, which is the exact
            // silhouette this whole change is removing.
            pip.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            Visual.VfxMaterial.Ghost(pip.GetComponent<Renderer>(),
                new Color(UiTheme.Highlight.r, UiTheme.Highlight.g, UiTheme.Highlight.b, 0.85f),
                2.0f);
            Visual.VfxMaterial.StripCollider(pip);
            _pip = pip.transform;

            var lightGo = new GameObject("ObjectiveLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0.0f, 0.6f, 0.0f);
            _light = lightGo.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = UiTheme.Highlight;

            // ⚠️ 2.6 m OF RANGE AT 1.8 INTENSITY, DOWN FROM 4.0 AT 3.0. The old light washed a
            // four metre bubble of road, which on Ilalim ng Tulay is most of a lane.
            // `docs/VISION.md` § 2 rule 5 applies to a tutorial marker exactly as much as to an
            // ultimate: if the frame stops showing the street, the thing lighting it is too big.
            _light.range = 2.6f;
            _light.shadows = LightShadows.None;
        }

        private void LateUpdate()
        {
            if (_target == null) { gameObject.SetActive(false); return; }

            Vector3 at = _target.position;
            transform.position = new Vector3(at.x, at.y + 0.03f, at.z);

            float pulse = Mathf.Sin(Time.unscaledTime * 4.5f) * 0.5f + 0.5f;

            // ⚠️ THE PULSE MOVES THE PIP, NOT THE ROOT. Scaling the root scaled the ring's
            // radius with it, so the footprint drawn on the floor changed size seven times a
            // second and read as a live ability zone rather than as a pointer.
            if (_pip != null)
            {
                _pip.localPosition = new Vector3(0.0f, PipHeight + Mathf.Lerp(-0.06f, 0.10f, pulse), 0.0f);
                _pip.localRotation = Quaternion.Euler(90.0f, Time.unscaledTime * 60.0f, 0.0f);
            }

            if (_light != null) _light.intensity = Mathf.Lerp(0.9f, 1.8f, pulse);

            HideIfTheEyeIsInsideIt();
        }

        /// <summary>Switch the shapes off while the camera is on top of them. See
        /// <see cref="HideWithin"/>.</summary>
        private void HideIfTheEyeIsInsideIt()
        {
            // ⚠️ `Camera.main` IS NOT ENOUGH ON ITS OWN. It resolves by TAG, and the rig's camera
            // is built from code; a probe run measured this method doing nothing at all because
            // of it. The rig is asked first and the result cached, so this is one lookup.
            if (_eye == null)
            {
                var rig = FindFirstObjectByType<CameraSystem.CameraRig>();
                _eye = rig != null && rig.Camera != null ? rig.Camera : Camera.main;
            }

            var cam = _eye;
            if (cam == null) return;

            bool draw = Vector3.Distance(cam.transform.position, transform.position) > HideWithin;
            if (draw == _drawn) return;

            _drawn = draw;

            // ⚠️ THE RENDERERS, NOT THE OBJECT. `LateUpdate` is what re-tests the distance, so an
            // object that switched ITSELF off could never come back when the player walked away.
            if (_shapes == null || _shapes.Length == 0)
                _shapes = GetComponentsInChildren<Renderer>(true);

            foreach (var r in _shapes)
                if (r != null) r.enabled = draw;

            // The light goes with them: a point light inside your own head is a white screen.
            if (_light != null) _light.enabled = draw;
        }
    }
}
