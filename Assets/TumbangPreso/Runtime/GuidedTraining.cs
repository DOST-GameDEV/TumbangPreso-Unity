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
        private float _metric;
        private float _baselineCooldown;
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
            _abilities = _local.AbilitySystem;
            _emotes = _local.GetComponent<EmotePlayer>();

            foreach (var slipper in _slippers)
            {
                if (slipper != null && slipper.OwnerSlot == _local.PlayerSlot)
                {
                    _ownSlipper = slipper;
                    break;
                }
            }

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

            HideTheCast();

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

                case Lesson.Shove:
                    if (_verbs != null && _verbs.ShoveCooldownLeft > _baselineCooldown + 0.05f)
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

                case Lesson.Punch:
                    if (_verbs != null && _verbs.PunchCooldownLeft > _baselineCooldown + 0.05f)
                        CompleteLesson();
                    break;

                case Lesson.Lunge:
                    if (_verbs != null && _verbs.LungeCooldownLeft > _baselineCooldown + 0.05f)
                        CompleteLesson();
                    break;

                case Lesson.TripRecovery:
                    // A press is detected as a drop LARGER than the passive bleed could have
                    // produced in one frame, so it counts only presses `CharacterMotor` actually
                    // accepted through the real rate cap.
                    //
                    // ⚠️ THE BLEED IS NO LONGER ONE SECOND PER SECOND, and this comment used to
                    // say it was. Above `Balance.MinTripDown` it runs at
                    // `Balance.TripPassiveDecayRate`, so the real per-frame drop is SMALLER than
                    // the `Time.deltaTime` assumed here. That only widens the gap a press has to
                    // clear, so the detector still cannot credit the bleed as a press.
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
                    // (2.50 - 0.90) / 0.20 = 8 of them, so a player who watches the first fall
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

        private void CompleteLesson()
        {
            if (_advancing) return;
            _advancing = true;
            _hud?.FlashComplete();
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

        private void EnterLesson(Lesson lesson)
        {
            _lesson = lesson;
            _advancing = false;
            _metric = 0.0f;
            _lastPosition = _local.transform.position;
            _defenderResetArmed = false;
            _marker?.Bind(null);
            SetProgress(0.0f);
            ApplyVerbLock(lesson);

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
                    title = "GET YOUR TSINELAS BACK";
                    body = "Walk to your own slipper and press the pickup key. Holding it inside the box makes you taggable.";
                    action = Key("Grab") + "  ·  PICK UP";
                    _marker?.Bind(_ownSlipper != null ? _ownSlipper.transform : null);
                    break;

                case Lesson.Pektus:
                    PrepareAttackerThrow();
                    title = "CURVE A PEKTUS THROW";
                    body = "Charge another throw, add spin with the wheel or arrow keys, then release. Strong spin can bank once.";
                    action = Key("SpecialAbility") + " + MOUSE WHEEL / ARROWS";
                    _marker?.Bind(_lata.transform);
                    break;

                case Lesson.Shove:
                    PrepareDummyInFront(1.15f, attacker: true);
                    _local.Stamina.RefillAndClearFatigue();
                    _baselineCooldown = _verbs != null ? _verbs.ShoveCooldownLeft : 0.0f;
                    title = "SHOVE AN ATTACKER";
                    body = "Shove the training dummy. It costs stamina you may need for the run back out.";
                    action = Key("Grab") + "  ·  SHOVE";
                    _marker?.Bind(_dummy != null ? _dummy.transform : null);
                    break;

                case Lesson.AbilityInfo:
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
                    BecomeDefender();
                    title = "ROLE SWAP: DEFENDER";
                    body = "You are now the taya. Stay inside the chalk box and hold the pickup key by the down lata to stand it up.";
                    action = Key("Grab") + "  ·  HOLD TO RESET";
                    _marker?.Bind(_lata.transform);
                    StartCoroutine(ArmDefenderReset());
                    break;

                case Lesson.Punch:
                    PrepareDummyInFront(1.25f, attacker: true);
                    _baselineCooldown = _verbs != null ? _verbs.PunchCooldownLeft : 0.0f;
                    title = "PUNCH A VULNERABLE ATTACKER";
                    body = "The defender's left click is a quick stationary tag. The dummy is holding a slipper inside your box.";
                    action = Key("SpecialAbility") + "  ·  PUNCH";
                    _marker?.Bind(_dummy != null ? _dummy.transform : null);
                    break;

                case Lesson.Lunge:
                    PrepareDummyInFront(3.0f, attacker: true);
                    _baselineCooldown = _verbs != null ? _verbs.LungeCooldownLeft : 0.0f;
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
                    break;
            }

            _hud?.SetLesson((int)lesson, LessonCount, title, body, action,
                            _local.IsDefender ? UiTheme.Defense : UiTheme.Offense);
        }

        private IEnumerator ArmDefenderReset()
        {
            _lata.HostRestore();
            yield return new WaitForSeconds(Balance.ThrowRestoreCooldown + 0.08f);
            _lata.HostKnockDown(-1);
            _defenderResetArmed = !_lata.IsUpright;
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

            if (_ownSlipper != null && _ownSlipper.State == SlipperState.Held)
            {
                Vector3 drop = _local.transform.position + _local.transform.right * 3.0f;
                _ownSlipper.ApplySnapshotState(SlipperState.Loose, null, drop,
                    _ownSlipper.transform.rotation, Vector3.zero, 0.0f,
                    SlipperAffinity.Normal, -1);
            }
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

        private void OnDestroy()
        {
            GameLaunch.GuidedTutorial = false;

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
            layout.spacing = 10.0f;
            layout.padding = new RectOffset(12, 12, 4, 4);

            var box = footGo.AddComponent<LayoutElement>();
            box.minHeight = 42.0f;
            box.preferredHeight = 42.0f;

            KeyCap(footGo.transform, "N");
            Chip(footGo.transform, "SKIP");
            Chip(footGo.transform, "·");
            KeyCap(footGo.transform, "BACKSPACE");
            Chip(footGo.transform, "QUIT TRAINING");
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

        private Transform _target;
        private Transform _pip;
        private Light _light;

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
            var ring = Visual.VfxShapes.Lay(transform, "ObjectiveRing",
                                            Visual.VfxShapes.NovaShell(2, 22), RingRadius, 0.04f);
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
        }
    }
}
