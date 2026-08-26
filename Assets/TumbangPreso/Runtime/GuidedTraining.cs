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
                    // Natural drain is one second per second. Any larger drop came from an
                    // accepted mash through CharacterMotor's real rate cap.
                    float expected = Mathf.Max(0.0f, _lastTripLeft - Time.deltaTime);
                    if (_local.TripLeft < expected - 0.05f) _metric += 1.0f;
                    _lastTripLeft = _local.TripLeft;
                    SetProgress(_metric / 5.0f);
                    if (_metric >= 5.0f) CompleteLesson();
                    break;

                case Lesson.Emote:
                    if (_emotes != null && _emotes.IsEmoting) CompleteLesson();
                    break;
            }
        }

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

        private void EnterLesson(Lesson lesson)
        {
            _lesson = lesson;
            _advancing = false;
            _metric = 0.0f;
            _lastPosition = _local.transform.position;
            _defenderResetArmed = false;
            _marker?.Bind(null);
            SetProgress(0.0f);

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

        private void OnDestroy()
        {
            GameLaunch.GuidedTutorial = false;
            if (_marker != null) Destroy(_marker.gameObject);
            if (_hud != null) Destroy(_hud.gameObject);
        }
    }

    /// <summary>Objective card for guided training, deliberately separate from the normal HUD.</summary>
    internal sealed class GuidedTrainingHud : MonoBehaviour
    {
        private Text _counter;
        private Text _title;
        private Text _body;
        private Text _action;
        private Text _complete;
        private Image _fill;
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
            rt.anchoredPosition = new Vector2(32.0f, -32.0f);
            rt.sizeDelta = new Vector2(650.0f, 228.0f);

            _counter = Label(panelGo.transform, 19, UiTheme.Highlight,
                new Vector2(24, -18), new Vector2(600, 28));
            _title = Label(panelGo.transform, 32, UiTheme.Cream,
                new Vector2(24, -52), new Vector2(600, 48));
            _body = Label(panelGo.transform, 21, UiTheme.CreamMuted,
                new Vector2(24, -100), new Vector2(600, 70));
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.verticalOverflow = VerticalWrapMode.Overflow;
            _action = Label(panelGo.transform, 21, UiTheme.Offense,
                new Vector2(24, -176), new Vector2(600, 34));

            var barBack = new GameObject("ProgressBack");
            barBack.transform.SetParent(panelGo.transform, false);
            var back = barBack.AddComponent<Image>();
            back.color = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.75f);
            Place(back.rectTransform, new Vector2(24, -212), new Vector2(600, 8));

            var fillGo = new GameObject("ProgressFill");
            fillGo.transform.SetParent(barBack.transform, false);
            _fill = fillGo.AddComponent<Image>();
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.color = UiTheme.Highlight;
            _fill.fillAmount = 0.0f;
            MenuKit.Stretch(_fill.rectTransform);

            _complete = Label(transform, 38, UiTheme.Highlight,
                new Vector2(0, -278), new Vector2(720, 60), centered: true);
            _complete.text = "LESSON COMPLETE";
            _complete.enabled = false;

            var footer = Label(transform, 18, UiTheme.CreamMuted,
                new Vector2(0, 28), new Vector2(760, 30), centered: true, fromBottom: true);
            footer.text = "[N] SKIP LESSON   ·   [BACKSPACE] EXIT TRAINING";
        }

        public void SetLesson(int lesson, int total, string title, string body, string action,
                              Color role)
        {
            _counter.text = lesson >= total ? "TRAINING COMPLETE" : $"TRAINING  ·  {lesson + 1:00} / {total:00}";
            _title.text = title;
            _body.text = body;
            _action.text = action;
            _action.color = role;
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

        private static Text Label(Transform parent, int size, Color colour, Vector2 offset,
                                  Vector2 box, bool centered = false, bool fromBottom = false)
        {
            var go = new GameObject("TrainingText");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = MenuKit.Font;
            text.fontSize = size;
            text.color = colour;
            text.alignment = centered ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft;
            text.raycastTarget = false;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = UiTheme.Ink;
            outline.effectDistance = new Vector2(3.0f, -3.0f);

            var rt = text.rectTransform;
            rt.anchorMin = fromBottom ? new Vector2(0.5f, 0.0f) : new Vector2(centered ? 0.5f : 0.0f, 1.0f);
            rt.anchorMax = rt.anchorMin;
            rt.pivot = centered ? new Vector2(0.5f, 0.5f) : new Vector2(0.0f, 1.0f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = box;
            return text;
        }

        private static void Place(RectTransform rt, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.0f, 1.0f);
            rt.anchorMax = new Vector2(0.0f, 1.0f);
            rt.pivot = new Vector2(0.0f, 1.0f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
        }
    }

    /// <summary>A narrow, non-colliding beacon that points at the current lesson target.</summary>
    internal sealed class TrainingMarker : MonoBehaviour
    {
        private Transform _target;
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
            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "ObjectiveShaft";
            shaft.transform.SetParent(transform, false);
            shaft.transform.localPosition = new Vector3(0.0f, 2.6f, 0.0f);
            shaft.transform.localScale = new Vector3(0.10f, 2.6f, 0.10f);
            Visual.VfxMaterial.Ghost(shaft.GetComponent<Renderer>(),
                new Color(UiTheme.Highlight.r, UiTheme.Highlight.g, UiTheme.Highlight.b, 0.24f),
                1.7f);
            Visual.VfxMaterial.StripCollider(shaft);

            var lightGo = new GameObject("ObjectiveLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0.0f, 0.8f, 0.0f);
            _light = lightGo.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = UiTheme.Highlight;
            _light.range = 4.0f;
            _light.shadows = LightShadows.None;
        }

        private void LateUpdate()
        {
            if (_target == null) { gameObject.SetActive(false); return; }

            Vector3 at = _target.position;
            transform.position = new Vector3(at.x, at.y + 0.03f, at.z);
            float pulse = Mathf.Sin(Time.unscaledTime * 7.0f) * 0.5f + 0.5f;
            transform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.10f, pulse);
            if (_light != null) _light.intensity = Mathf.Lerp(1.4f, 3.0f, pulse);
        }
    }
}
