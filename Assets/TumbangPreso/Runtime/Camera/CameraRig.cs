using System.Collections.Generic;
using UnityEngine;
// ShadowCastingMode, for the first-person self-hide. See ApplyFppSelfHide.
using UnityEngine.Rendering;

namespace TumbangPreso.CameraSystem
{
    public enum CameraMode { Fpp, Tpp }

    public enum AimSource { Mouse, Movement }

    /// <summary>
    /// The player camera. Ported from `camera_rig.gd`.
    ///
    /// ⚠️⚠️ THE GAME IS FIRST PERSON. A Person is ALWAYS FPP and a Prop is ALWAYS TPP; that is
    /// a stated directive, not a preference, and an earlier version of this port used a
    /// third-person follow camera for everything, which is a different game.
    ///
    /// ⚠️⚠️ THE CAMERA NEVER INHERITS THE BODY'S ROLL OR PITCH, AND THIS IS THE INVARIANT.
    /// It was reported three times from playtests, with screenshots: the whole 3D view rolled
    /// forty degrees while the HUD stayed level. The pivots are children of the body, so they
    /// inherit its full basis, and anything that leaves a non-yaw component on the body lands
    /// directly in the player's eye. Patching each writer was tried in pieces and failed. So
    /// the rig stops trusting its parent: every frame both pivots are given an ABSOLUTE
    /// rotation built from the body's YAW ONLY plus the rig's own pitch. Whatever the body does
    /// on the other two axes cannot reach the camera, from any code path, including ones
    /// nobody has written yet.
    ///
    /// ⚠️ AND YAW IS RECOVERED FROM THE FORWARD VECTOR, NOT FROM EULER ANGLES. Decomposing a
    /// basis that has roll in it does not give back the yaw you want, which is precisely the
    /// situation this exists to survive.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        // -------------------------------------------------------------------
        // Constants, carried over exactly.
        // -------------------------------------------------------------------

        public const float PitchMinDeg = -80.0f;
        public const float PitchMaxDeg = 70.0f;
        public const float BaseSensitivity = 0.15f;

        /// <summary>
        /// ⚠️ MEASURED, AND THE OLD 1.55 MUST NOT BE RESTORED. That value was "eye height on a
        /// 1.6 unit capsule" and was never checked against a model: it put the whole character
        /// below the near edge of the frame. 0.45 is 55% of the way up the head mesh, which is
        /// the eye line on the actual rig.
        /// </summary>
        public const float FppEyeHeight = 0.45f;

        /// <summary>Meshes whose name contains this are hidden in first person.</summary>
        public const string FppHiddenMeshHint = "head";

        public const float ViewmodelScale = 0.72f;

        /// <summary>
        /// ⚠️ PUSHED DOWN AND BACK AFTER SCALING. Shrinking the arms alone just leaves smaller
        /// arms in the same commanding spot; down clears the centre of frame and back is what
        /// actually stops them subtending half the vertical FOV.
        ///
        /// ⚠️⚠️ THE Z IS FLIPPED FROM THE .gd's `Vector3(0.0, -0.10, -0.16)` AND IT WAS COPIED
        /// ACROSS UNCHANGED, WHICH PUT THE ARMS BEHIND THE CAMERA. Godot looks down -Z, so -0.16
        /// there is 16 cm IN FRONT of the eye; Unity looks down +Z, so the same number is 16 cm
        /// BEHIND it. Every other converted vector in this file and in `ViewmodelArms` takes the
        /// flip — the arm origins, the carry anchor, the carry direction — and this one was
        /// missed because it is the only one written as a plain "push it back" offset rather
        /// than as a transcribed transform. The visible result is the arms straddling the near
        /// plane and drawing as two enormous slabs across the top of the frame, which is exactly
        /// what the report's screenshot shows.
        /// </summary>
        public static readonly Vector3 ViewmodelSeat = new Vector3(0.0f, -0.10f, 0.16f);

        public const float PersonCapsuleHeight = 1.6f;
        public const float TppMinSpringLength = 1.8f;
        public const float TppMinPitchDeg = -34.0f;
        public const float TppBaseSpringLength = 4.5f;
        public const float TppBasePitchDeg = -15.0f;
        public const float TppMountHeight = 1.2f;

        public const float VmKickTime = 0.22f;

        public const float EmotePitchMinDeg = -35.0f;
        public const float EmotePitchMaxDeg = 20.0f;

        // -------------------------------------------------------------------

        [SerializeField] private AimSource _aimSource = AimSource.Movement;
        /// <summary>
        /// ⚠️⚠️ 95 IN FIRST PERSON AND 70 IN THIRD, FROM `CameraRig.tscn`, AND A SINGLE 75 FOR
        /// BOTH WAS WRONG IN BOTH DIRECTIONS. The .tscn's FppCamera is `fov = 95.0` and its
        /// TppCamera is `fov = 70.0`; Godot's `keep_aspect` defaults to KEEP_HEIGHT, so both are
        /// VERTICAL angles and transcribe straight into Unity's `fieldOfView`.
        ///
        /// This is not a taste setting. A first-person view at 75 where the game was framed at
        /// 95 shows a third less of the street, which changes how much of the box a taya can
        /// watch at once and how early an attacker sees a lunge coming, and it is most of why
        /// the two builds' arena screenshots do not look like the same game even with identical
        /// geometry. It also decides how much of the frame the viewmodel arms occupy.
        /// </summary>
        public const float FppFieldOfView = 95.0f;
        public const float TppFieldOfView = 70.0f;

        /// <summary>
        /// The spring arm's standoff. ⚠️ DELIBERATELY LARGER THAN THE NEAR PLANE (0.15 against
        /// 0.05): the arm stops that far short of whatever it hit, and a margin under the near
        /// plane lets the wall clip through the camera exactly when the arm bottoms out.
        /// </summary>
        public const float TppArmMargin = 0.15f;

        [SerializeField] private float _fieldOfView = FppFieldOfView;

        private CharacterMotor _character;
        private CameraMode _mode = CameraMode.Fpp;

        private Transform _fppPivot;
        private Transform _tppPivot;
        private Transform _viewmodel;
        private ViewmodelArms _arms;
        private UnityEngine.Camera _camera;

        private float _pitchDeg;
        private float _tppPitchDeg = TppBasePitchDeg;
        private float _tppSpringLength = TppBaseSpringLength;

        private bool _active;

        private readonly List<Renderer> _hiddenForFpp = new List<Renderer>();

        private float _shakeStrength;
        private float _shakeLeft;
        private float _vmKickLeft;
        private Vector3 _vmKickOffset;

        private bool _emoteView;
        private Social.EmotePlayer _emotes;
        private float _emoteYawDeg;
        private float _emotePitchDeg;
        private CameraMode _modeBeforeEmote = CameraMode.Fpp;

        /// <summary>
        /// How long the eye takes to travel from Nemu into Kuro when a possession starts.
        ///
        /// ⚠️⚠️ THE POSSESSION WAS ALREADY A TPP VIEW OF THE PET AND STILL DID NOT READ AS ONE.
        /// 🧑: *"it doesnt feel like im in the pet's body"*, and the first reading of that, that
        /// the camera never moved, is wrong: `ApplyCompanionPossessionView` has always mounted
        /// behind Kuro and `StepCompanionLook` has always steered him. What it did was call
        /// `SetPositionAndRotation` with the finished pose on the possession's FIRST frame.
        ///
        /// ⚠️ A CUT IS NOT A TRANSFORMATION. With no travel between the two poses the player
        /// never sees themselves leave, so there is no moment to attribute the new body to and
        /// the swap reads as a glitch or as nothing at all. Kuro is projected out AHEAD of Nemu,
        /// so simply giving the move a duration makes the camera fly from her head to his, which
        /// is the spirit leaving one body and arriving in the other, drawn rather than asserted.
        ///
        /// 0.28 s is short enough that it never costs the player a fight and long enough to be
        /// seen. `docs/Hero_Strike_Balance.md` § 8.6 is the rule that says this ability, and
        /// almost none of the others, is allowed to take the camera at all.
        /// </summary>
        private const float PossessBlendSeconds = 0.28f;

        /// <summary>True while the third-person swing is being held by a FALL rather than by an
        /// emote. See <see cref="StepFallView"/>.</summary>
        private bool _fallView;

        private bool _wasPossessing;
        private float _possessBlend;
        private Vector3 _possessFromPos;
        private Quaternion _possessFromRot;

        public CameraMode Mode => _mode;
        public AimSource Aim => _aimSource;
        public bool IsLocalFpp => _active && _mode == CameraMode.Fpp;

        /// <summary>Is this rig looking through <paramref name="who"/>? A shake or a kick
        /// applied to another unit's camera is feedback landing on the wrong screen.</summary>
        public bool IsFollowing(CharacterMotor who) => _character == who && _active;
        public UnityEngine.Camera Camera => _camera;

        /// <summary>The seat this rig is looking through, or null. Read-only: `Bind` is the one
        /// writer, and the mode is derived from the subject rather than set beside it.</summary>
        public CharacterMotor Following => _character;

        /// <summary>
        /// § THE VERB, IN THE PLAYER'S OWN HANDS. `camera_rig.gd::play_viewmodel_action`.
        ///
        /// ⚠️⚠️ A CLIP IF THE ARMS HAVE ONE, A PROCEDURAL KICK IF THEY DO NOT, AND THE SECOND
        /// HALF IS THE POINT. `ViewmodelArms.tscn` carries `throw` and `grab` and nothing else,
        /// so the punch, the shove and the lunge have no clip — and in first person the body is
        /// SHADOWS_ONLY, which means those three verbs had NO first-person feedback whatsoever.
        /// The .gd's own note: *"you pressed shove and the screen did not move"*, added for 🧑
        /// 2026-08-01: *"add visual cue for first person and for everyone else that shove and
        /// sunok and other skills and abilities shit is happening"*.
        ///
        /// The kick is not a substitute for an authored clip. It is what makes the verb legible
        /// until somebody animates it, and it disappears on its own the day a clip with that
        /// name is added, because the branch above wins.
        ///
        /// ⚠️ THE GUARD IS THE OTHER HALF. Every verb runs on all four seats, so an unguarded
        /// call would swing the PLAYER's arm every time a bot threw — three phantom throws a
        /// second, none of them theirs. Same rule the camera shake follows: feedback belongs to
        /// the person it happened to.
        ///
        /// ⚠️ AND IT IS A STATIC ON THE RIG. `CharacterAnimator` is installed with
        /// `AddComponent` and cannot carry an inspector reference (rule 3), and the rig is the
        /// only thing that knows whether it is in FPP at all.
        /// </summary>
        public static void PlayViewmodelAction(CharacterMotor who, string kind)
        {
            if (who == null) return;

            var rig = FindFirstObjectByType<CameraRig>();
            if (rig == null || !rig.IsFollowing(who) || rig._mode != CameraMode.Fpp) return;
            if (rig._arms == null || !rig._arms.gameObject.activeInHierarchy) return;

            if (rig._arms.PlayAction(kind)) return;

            rig.ViewmodelKick(Vector3.forward);
        }

        // -------------------------------------------------------------------

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            if (_camera == null) _camera = gameObject.AddComponent<UnityEngine.Camera>();

            _camera.fieldOfView = _fieldOfView;
            _camera.nearClipPlane = 0.05f;

            // ⚠️⚠️ THE MAP'S COLOUR GRADE, WHICH NOTHING IN THE PORT APPLIED. Every Godot
            // Environment in this game enables `adjustment_*` and Eskinita runs contrast 1.03 and
            // saturation 1.18 over the whole frame. There is no RenderSettings field for that, so
            // the numbers were dropped at import and the match has been rendering ungraded
            // against a Godot build that never is. `MapGrade` is what the importer leaves behind;
            // adopting it here means the match camera and the setup screen's preview camera are
            // reading the same three numbers off the same object.
            //
            // ⚠️ ADOPTED IN Start, NOT HERE. The arena's own objects are not guaranteed to be in
            // the scene during this component's Awake, and a grade that finds nothing quietly
            // resolves to an identity blit that looks like the feature was never added.
            _grade = gameObject.GetComponent<Visual.ColourGrade>();
            if (_grade == null) _grade = gameObject.AddComponent<Visual.ColourGrade>();
        }

        private Visual.ColourGrade _grade;

        private void Start()
        {
            if (_grade != null) _grade.AdoptFromScene();
        }

        /// <summary>
        /// ⚠️ THE FOV FOLLOWS THE MODE, because the two cameras in `CameraRig.tscn` are two
        /// different lenses and the mode swap is what switches between them. Applied every frame
        /// rather than on the transition, so the emote swing and the spectator handover cannot
        /// leave the wrong one on.
        /// </summary>
        private void ApplyLens()
        {
            if (_camera == null) return;

            float want = _mode == CameraMode.Fpp && !_emoteView ? FppFieldOfView : TppFieldOfView;
            if (!Mathf.Approximately(_camera.fieldOfView, want)) _camera.fieldOfView = want;
        }

        /// <summary>
        /// Attach to a body. ⚠️ A PERSON IS ALWAYS FPP.
        ///
        /// ⚠️⚠️ AND `_emoteView` IS CLEARED HERE TOO. Nothing about swapping which body this rig
        /// follows — a spectator cycle, a seat reassignment — routes through `EndEmoteView`, so
        /// a rig mid-swing when `Follow` is called would otherwise carry `_emoteView = true`
        /// onto whichever character it picks up next with nothing left that will ever set it
        /// back to false: `ApplyFpp`/`ApplyTpp` never run while it is true, so the mode never
        /// settles and the player is stuck orbiting a stranger. A player stuck in third person
        /// with no way back is worse than any visual glitch this could instead have been.
        /// </summary>
        public void Follow(CharacterMotor character, bool makeActive = true)
        {
            UnsubscribeEmotes();

            _emoteView = false;

            // ⚠️ THE FALL FLAG CLEARS WITH THE EMOTE FLAG, because it is a claim ABOUT the emote
            // flag. This function's own note above is the reason: a rig that keeps a stale swing
            // across a seat change leaves the player orbiting a stranger with no way back. Left
            // set here, `StepFallView` would believe it had already swung out and would refuse
            // to swing again for the next fall on this body.
            _fallView = false;

            _character = character;
            _mode = CameraMode.Fpp;

            if (_character == null) return;

            SubscribeEmotes();
            BuildPivots();
            if (_arms != null && _character != null) _arms.MatchCharacter(_character);
            ApplyFppSelfHide();
            SetActive(makeActive);
        }

        /// <summary>
        /// ⚠️⚠️ THE EMOTE SWING IS WIRED HERE, ON THE RIG, AND THAT IS WHAT KEEPS IT LOCAL.
        ///
        /// 🧑 2026-08-04: *"i want the emotes to switch camera to TPP js for the emote and go
        /// back to FPP after the emote ends"*. The emote itself is replicated; the camera
        /// swing must NOT be, or every peer would spin to third person because somebody else
        /// danced.
        ///
        /// Subscribing from the rig gets that for free rather than by a flag someone has to
        /// remember: a rig only ever follows the unit this machine is looking through, so a
        /// remote player's emote has no rig subscribed to it and cannot move any camera.
        /// Wiring this the other way round — EmotePlayer reaching for a camera — would need
        /// an "am I local" test at the call site, which is the check that gets forgotten.
        /// </summary>
        private void SubscribeEmotes()
        {
            _emotes = _character.GetComponent<Social.EmotePlayer>();
            if (_emotes == null) return;

            _emotes.EmoteStarted += OnEmoteStarted;
            _emotes.EmoteStopped += OnEmoteStopped;
        }

        private void UnsubscribeEmotes()
        {
            if (_emotes == null) return;

            _emotes.EmoteStarted -= OnEmoteStarted;
            _emotes.EmoteStopped -= OnEmoteStopped;
            _emotes = null;
        }

        private void OnEmoteStarted(string id) => BeginEmoteView();

        /// ⚠️ AN EMOTE NEVER ENDS ON ITS OWN. 🧑 2026-08-15: *"the emotes only end when a
        /// user does smth to interrupt it like move or attack"*. So this fires on exactly one
        /// path — <see cref="Social.EmotePlayer.Stop"/>, reached by movement, a verb, or the
        /// unit losing the right to act — and there is no timer to race it.
        ///
        /// If a clip-finished path is ever added, it MUST route through Stop() as well.
        /// Restoring the camera from a second place is how a rig ends up stuck in third
        /// person: one path returns the view and the other silently does not.
        private void OnEmoteStopped() => EndEmoteView();

        private void OnDestroy() => UnsubscribeEmotes();

        private void BuildPivots()
        {
            // ⚠️ THE PIVOTS ARE NOT PARENTED TO THE BODY. In Godot they were children and
            // inherited its basis, which is the whole reason the roll bug existed. Here they
            // are free transforms positioned from the body every frame, so a rolled body
            // cannot reach them by construction rather than by care.
            if (_fppPivot == null)
            {
                _fppPivot = new GameObject("~FppPivot").transform;
                _tppPivot = new GameObject("~TppPivot").transform;
            }

            MountViewmodel();
        }

        /// <summary>
        /// ⚠️ FIRST PERSON GETS DEDICATED VIEWMODEL ARMS, NOT THE RIG'S OWN. From playtest:
        /// "don't see arms of ppl". The real body tops out below the eye line because the chibi
        /// head is big enough that the eye sits above the shoulders, so looking down showed
        /// nothing at all. The arms are mounted to the camera pivot and inherit its pitch, so
        /// they rise and fall with the view, and a remote player's rig is never the one being
        /// looked through.
        /// </summary>
        private void MountViewmodel()
        {
            if (_viewmodel != null) return;

            // ⚠️ BUILT IN CODE, NOT LOADED AS A PREFAB. This used to `Resources.Load` a
            // "Models/ViewmodelArms" prefab that has never existed, so it returned null and
            // returned early — every FPP view has been armless for the whole port, silently,
            // because a missing prefab is not an error. ViewmodelArms builds itself from the
            // .tscn's own baked transforms instead, so there is nothing to author and nothing
            // to go missing.
            var go = new GameObject("~ViewmodelArms");
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * ViewmodelScale;
            go.transform.localPosition = ViewmodelSeat;
            go.transform.localRotation = Quaternion.identity;

            _arms = go.AddComponent<ViewmodelArms>();

            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);

            _viewmodel = go.transform;
        }

        public void SetActive(bool active)
        {
            _active = active;
            if (_camera != null) _camera.enabled = active;

            if (_viewmodel != null) _viewmodel.gameObject.SetActive(active && _mode == CameraMode.Fpp);

            if (active) ApplyFppSelfHide();
            else RestoreSelfHide();

            ApplyCarriedSelfHide();
        }

        public void SetAimSource(AimSource source) => _aimSource = source;

        // -------------------------------------------------------------------

        private void LateUpdate()
        {
            if (_character == null || !_active) return;

            ApplyLens();
            ApplyCarriedSelfHide();

            // ⚠️ THE HITSTOP GATE SITS ABOVE EVERYTHING THAT WRITES THE TRANSFORM AND BELOW
            // EVERYTHING THAT DOES NOT. The lens and the self-hide are per-frame state rather
            // than motion, and skipping either during a hold would pop the arms or the FOV.
            // See `HoldFrame` for why this is a camera hold and not a time scale.
            if (StepHold()) return;

            StepFallView();

            if (_emoteView) { StepEmoteLook(); ApplyEmoteView(); return; }

            var visual = _character.GetComponent<Visual.CharacterVisual>();
            var companion = visual != null ? visual.Companion : null;
            bool isPossessingCompanion = companion != null && companion.IsPossessed;

            // ⚠️ THE EDGE IS CAUGHT HERE AND NOWHERE ELSE. `GhostPetCompanion` owns the
            // possession state and the rig only reads it, so the frame the flag flips is the
            // only place the rig can learn where the eye was standing when the body was left.
            // Sampling it later would blend from a pose that has already been overwritten.
            if (isPossessingCompanion && !_wasPossessing)
            {
                _possessBlend = 0.0f;
                _possessFromPos = transform.position;
                _possessFromRot = transform.rotation;
            }

            _wasPossessing = isPossessingCompanion;

            if (isPossessingCompanion)
            {
                StepCompanionLook(companion);
                ApplyCompanionPossessionView(companion);
                StepShake();
                return;
            }

            StepLook();

            if (_mode == CameraMode.Fpp) ApplyFpp();
            else ApplyTpp();

            StepShake();
            StepViewmodelKick();
        }

        private void StepCompanionLook(Visual.GhostPetCompanion companion)
        {
            if (_aimSource != AimSource.Mouse) return;
            if (UI.EmoteWheel.AnyOpen) return;

            var s = Settings.SettingsStore.Current;
            float sens = BaseSensitivity * s.MouseSensitivity;

            float dx = Input.GetAxisRaw("Mouse X") * sens;
            float dy = Input.GetAxisRaw("Mouse Y") * sens;
            if (s.InvertY) dy = -dy;

            if (Mathf.Abs(dx) > 0.0001f && companion != null)
                companion.transform.Rotate(Vector3.up, dx * 10.0f, Space.World);

            _pitchDeg = Mathf.Clamp(_pitchDeg - dy * 10.0f, PitchMinDeg, PitchMaxDeg);
        }

        private void ApplyCompanionPossessionView(Visual.GhostPetCompanion companion)
        {
            if (companion == null) return;

            // Hide human FPP viewmodel arms while possessing companion pet
            if (_viewmodel != null && _viewmodel.gameObject.activeSelf)
                _viewmodel.gameObject.SetActive(false);

            float yaw = companion.transform.eulerAngles.y;
            Vector3 mount = companion.transform.position + Vector3.up * 0.35f;
            var rot = Quaternion.Euler(Mathf.Clamp(_pitchDeg, -45.0f, 65.0f), yaw, 0.0f);
            Vector3 wanted = mount - (rot * Vector3.forward) * 2.0f;

            // ⚠️⚠️ THE ARRIVAL IS TRAVELLED, NOT CUT. See `PossessBlendSeconds`. Until the blend
            // completes the eye is carried from where it stood in Nemu's head to the mount
            // behind Kuro, which is what makes the possession read as a possession.
            if (_possessBlend < 1.0f)
            {
                _possessBlend = Mathf.Clamp01(_possessBlend + Time.deltaTime / PossessBlendSeconds);

                // ⚠️ SMOOTHSTEP RATHER THAN LINEAR. A constant-speed camera arriving at a dead
                // stop reads as a scripted move; easing both ends reads as the eye being pulled.
                float e = _possessBlend * _possessBlend * (3.0f - 2.0f * _possessBlend);

                transform.SetPositionAndRotation(Vector3.Lerp(_possessFromPos, wanted, e),
                                                 Quaternion.Slerp(_possessFromRot, rot, e));
                return;
            }

            transform.SetPositionAndRotation(wanted, rot);
        }

        private void StepLook()
        {
            if (_aimSource != AimSource.Mouse) return;
            if (_character.Intent.Parked) return;

            // ⚠️ THE EMOTE WHEEL OWNS THE MOUSE WHILE IT IS OPEN. Both this and the wheel are
            // steered by the same deltas, and without this the player's body spins on the spot
            // while they pick a slice. Godot got this from `_input` running before
            // `_unhandled_input`; Unity has no such ordering, so it is an explicit check.
            if (UI.EmoteWheel.AnyOpen) return;

            var s = Settings.SettingsStore.Current;
            float sens = BaseSensitivity * s.MouseSensitivity;

            float dx = Input.GetAxisRaw("Mouse X") * sens;
            float dy = Input.GetAxisRaw("Mouse Y") * sens;
            if (s.InvertY) dy = -dy;

            // ⚠️ YAW GOES ON THE BODY, PITCH STAYS ON THE RIG. The body turning is what makes
            // a throw leave along the sight line; a rig that yawed on its own would let the
            // player look one way and throw another.
            if (Mathf.Abs(dx) > 0.0001f)
                _character.transform.Rotate(Vector3.up, dx * 10.0f, Space.World);

            _pitchDeg = Mathf.Clamp(_pitchDeg - dy * 10.0f, PitchMinDeg, PitchMaxDeg);
        }

        /// <summary>
        /// ⚠️ RECOVERED FROM THE FORWARD VECTOR. Euler decomposition of a basis carrying roll
        /// does not return the yaw you want, and that is exactly the case this survives.
        /// </summary>
        private float BodyYawDeg()
        {
            Vector3 forward = _character.transform.forward;

            if (Mathf.Abs(forward.x) < 0.00001f && Mathf.Abs(forward.z) < 0.00001f)
                return _character.transform.eulerAngles.y; // straight up or down: degenerate

            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        private void ApplyFpp()
        {
            float yaw = BodyYawDeg();
            Vector3 eye = _character.transform.position + Vector3.up * (PersonCapsuleHeight * 0.5f + FppEyeHeight);

            // Absolute, from yaw and pitch only. The body's roll cannot reach this.
            transform.SetPositionAndRotation(eye, Quaternion.Euler(_pitchDeg, yaw, 0.0f));

            if (_viewmodel != null && !_viewmodel.gameObject.activeSelf)
                _viewmodel.gameObject.SetActive(true);

            // The hand shows what the unit is actually carrying.
            if (_arms == null) return;

            if (_character != null) _arms.MatchCharacter(_character);

            // ⚠️ ASKED PER FRAME, NOT ON A PICK-UP EVENT. What a character holds changes
            // DURING a round, and the self-hide only re-runs on activation and model changes,
            // so an event-driven version showed both slippers until the next swap.
            var carrier = _character.GetComponent<Carrier>();
            var held = carrier != null ? carrier.Held : null;

            _arms.SetHolding(held != null);

            // § THE WIND-UP, POLLED. `character_visual.gd` polls charge for the same reason it
            // polls carry scale and spin: *"charge is a continuously-varying value, not an event,
            // and a poll self-heals across a model rebuild on the round swap."*
            //
            // ⚠️ THREE SOURCES IN ONE ORDER, AND THE ORDER MATTERS, because the .gd's own note
            // records the bug it fixes: the throw branch requires something in hand, so a TAYA —
            // who holds nothing — fell through every branch and *"the attacker got an arm; the
            // defender got a statue"*. The lunge is the taya's commitment and the one thing an
            // attacker has to read to dodge it. All three rest at -1, so they compose without any
            // of them knowing about the others.
            float charge = -1.0f;

            if (held != null && carrier != null) charge = carrier.ObservedChargePower;

            if (charge < 0.0f)
            {
                var verbs = _character.GetComponent<CombatVerbs>();
                if (verbs != null) charge = verbs.ObservedLungeCharge;
            }

            _arms.SetCharge(charge);

            // ⚠️ THE VIEWMODEL WEARS THE PICKED SKIN. A player who chose CROCS held a brown
            // flip-flop in their own hands while every peer saw what they had actually picked.
            if (held != null) _arms.MatchSkin(held);
        }

        private void ApplyTpp()
        {
            float yaw = BodyYawDeg();
            Vector3 mount = _character.transform.position + Vector3.up * TppMountHeight;

            var rot = Quaternion.Euler(Mathf.Max(_tppPitchDeg, TppMinPitchDeg), yaw, 0.0f);
            Vector3 wanted = mount - (rot * Vector3.forward) * _tppSpringLength;

            // ⚠️ THE SPRING ARM EXCLUDES THE BODY IT IS WATCHING, or the cast hits the
            // character's own capsule every frame and drags the camera in against it.
            float length = _tppSpringLength;
            if (Physics.SphereCast(mount, 0.2f, (wanted - mount).normalized, out var hit,
                                   _tppSpringLength, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<CharacterMotor>() != _character)
                    length = Mathf.Max(TppMinSpringLength, hit.distance - TppArmMargin);
            }

            transform.SetPositionAndRotation(mount - (rot * Vector3.forward) * length, rot);

            if (_viewmodel != null) _viewmodel.gameObject.SetActive(false);
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE WHOLE BODY GOES SHADOWS-ONLY, NOT JUST THE HEAD, AND HEAD-ONLY IS A BUG THE
        /// GODOT BUILD ALREADY REVERTED. This hid only renderers whose name contained "head",
        /// which is B-73's original behaviour, and `camera_rig.gd` carries the note explaining
        /// why that stopped being right:
        ///
        ///   *"B-73 kept `body-mesh` visible because there was nothing else to look at in first
        ///   person. There is now — the viewmodel. Keeping the real body as well means two sets
        ///   of arms in the same frustum: the viewmodel ones mounted to the camera, and the
        ///   skinned ones hanging 0.37 below it."*
        ///
        /// Reported against this build in exactly those words: *"tf is this why do i have two
        /// sets of arms"*. It only became visible once `PERSON_SCALE` was applied, because at 42%
        /// of its height the real body genuinely did sit below the frustum and nobody could see
        /// the second pair. The scale fix did not cause this; it uncovered it.
        ///
        /// ⚠️ SHADOWS-ONLY, NEVER DISABLED. Losing your own shadow in first person destroys the
        /// ground read, and it is the only cue a Person has for where they are standing relative
        /// to the base circle. `r.enabled = false` takes the shadow with it, which is the other
        /// half of what was wrong here.
        /// </summary>
        private void ApplyFppSelfHide()
        {
            RestoreSelfHide();

            if (_character == null || _mode != CameraMode.Fpp) return;

            foreach (var r in _character.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                if (r.shadowCastingMode == ShadowCastingMode.ShadowsOnly) continue;

                _selfShadowModes.Add(r.shadowCastingMode);
                r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                _hiddenForFpp.Add(r);
            }
        }

        private void RestoreSelfHide()
        {
            for (int i = 0; i < _hiddenForFpp.Count; i++)
            {
                var r = _hiddenForFpp[i];
                if (r == null) continue;

                // ⚠️ RESTORED TO WHAT IT WAS, not to On. A mesh the artist authored as
                // shadows-only or shadowless stays that way when the rig hands it back.
                r.shadowCastingMode = i < _selfShadowModes.Count
                    ? _selfShadowModes[i]
                    : ShadowCastingMode.On;

                // The head-only pass used to disable renderers outright. Anything still carrying
                // that from an older build is put back here too.
                r.enabled = true;
            }

            _hiddenForFpp.Clear();
            _selfShadowModes.Clear();
        }

        private readonly List<ShadowCastingMode> _selfShadowModes = new List<ShadowCastingMode>();

        /// <summary>
        /// ⚠️⚠️ THE WORLD SLIPPER RIDES THE HAND BY COPYING A TRANSFORM EVERY FRAME
        /// (`Carrier.RideAnchor`), AND IT IS NEVER REPARENTED ONTO THE CHARACTER. So
        /// `ApplyFppSelfHide` above — which only reaches renderers under `_character`'s own
        /// hierarchy — never sees it, and it kept rendering in first person at wherever the
        /// (now shadows-only) hand anchor put it, at the same time the viewmodel's own
        /// dedicated `HeldSlipper` rendered mounted to the camera. Two slippers, and only one
        /// of them was ever hidden. Screenshot: *"look at the tsinelas"*.
        ///
        /// `camera_rig.gd::_apply_carried_self_hide` is the original's answer to exactly this,
        /// called from `_apply_fpp_self_hide` and every viewmodel-carry step. This is that
        /// function. It is asked every `LateUpdate`, not on a pick-up event, for the same
        /// reason the arms poll `Carrier.Held` in `ApplyFpp`: what a seat carries changes
        /// mid-round and a one-shot version only catches it on the next mode change.
        ///
        /// ⚠️ NOT GATED ON `_mode == Fpp` ALONE. Unlike the .gd, `BeginEmoteView` here does not
        /// reassign `_mode` to Tpp (see its own note on why the body is unhidden a different
        /// way), so `_mode` reads Fpp for the whole swing. `!_emoteView` is what actually turns
        /// this off during an emote, matching the body's own self-hide being restored there.
        /// </summary>
        private void ApplyCarriedSelfHide()
        {
            Slipper held = null;

            if (_active && _mode == CameraMode.Fpp && !_emoteView && _character != null)
            {
                var carrier = _character.GetComponent<Carrier>();
                held = carrier != null ? carrier.Held : null;
            }

            if (held == _hiddenCarriedSlipper) return;

            for (int i = 0; i < _hiddenCarriedRenderers.Count; i++)
            {
                var r = _hiddenCarriedRenderers[i];
                if (r == null) continue;

                r.shadowCastingMode = i < _carriedShadowModes.Count
                    ? _carriedShadowModes[i]
                    : ShadowCastingMode.On;
            }

            _hiddenCarriedRenderers.Clear();
            _carriedShadowModes.Clear();

            if (held != null)
            {
                foreach (var r in held.GetComponentsInChildren<Renderer>(true))
                {
                    if (r == null) continue;

                    _carriedShadowModes.Add(r.shadowCastingMode);
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                    _hiddenCarriedRenderers.Add(r);
                }
            }

            _hiddenCarriedSlipper = held;
        }

        private Slipper _hiddenCarriedSlipper;
        private readonly List<Renderer> _hiddenCarriedRenderers = new List<Renderer>();
        private readonly List<ShadowCastingMode> _carriedShadowModes = new List<ShadowCastingMode>();

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ SHAKE IS FOR THE PERSON IT HAPPENED TO. A body block shakes the BLOCKER's camera
        /// because it is a thing they did. Shaking everyone tells three other players something
        /// happened to them when nothing did.
        ///
        /// ⚠️ AND IT IS NOT HITSTOP. Hitstop writes a global time scale, which is fine for a
        /// shove on a long cooldown and completely wrong for a block that can fire as fast as
        /// three attackers can throw.
        /// </summary>
        private Vector3 _impactPunchOffset;
        private float _impactPunchLeft;
        private const float ImpactPunchDuration = 0.16f;

        /// <summary>
        /// Directional camera impact punch for heavy ability hits and strikes.
        /// </summary>
        public void ImpactPunch(Vector3 direction, float strength = 1.0f)
        {
            _impactPunchLeft = ImpactPunchDuration;
            _impactPunchOffset = direction.normalized * 0.20f * strength;
            Shake(strength * 0.45f, 0.22f);
        }

        public void Shake(float strength = 0.35f, float duration = 0.18f)
        {
            _shakeStrength = Mathf.Max(_shakeStrength, strength);
            _shakeLeft = Mathf.Max(_shakeLeft, duration);
        }

        // ------------------------------------------------------------------ hitstop

        /// <summary>
        /// ⚠️⚠️ THE IMPACT FRAME, AND IT IS A CAMERA HOLD RATHER THAN A TIME SCALE. A hit in
        /// this game used to land with no instant of contact at all: a `bump`, some stars and an
        /// impulse, which between them describe the aftermath and never the moment. This is the
        /// beat every fighting game spends its budget on. `Visual.HitFeel` is the only caller
        /// and it carries the reasoning and the weights.
        ///
        /// ⚠️⚠️ IT MUST NOT BECOME `Time.timeScale`, WHICH IS THE OBVIOUS IMPLEMENTATION AND IS
        /// WRONG HERE FOR THREE SEPARATE REASONS. This is a four-player game on one shared
        /// simulation: a global scale would freeze the physics step for all four, stop the round
        /// clock, and in a networked match desynchronise the host from its peers. It would also
        /// hand the anti-stall clocks in `docs/VISION.md` § 4 a free pause on every hit.
        ///
        /// What actually happens is much narrower. `LateUpdate` stops WRITING the camera
        /// transform for a few frames, so the view sticks where it was while the world carries on
        /// simulating underneath it. The player's own input is not eaten and their character
        /// keeps moving; only the picture lags, which is exactly the illusion wanted.
        ///
        /// ⚠️ UNSCALED TIME, so a hold cannot be stretched by anything else that scales time.
        /// </summary>
        public void HoldFrame(float seconds)
        {
            if (seconds <= 0.0f) return;

            _holdLeft = Mathf.Max(_holdLeft, seconds);
        }

        private float _holdLeft;

        /// <summary>
        /// ⚠️ THE SHAKE AND THE PUNCH ARE STILL STEPPED WHILE HELD, AND THAT IS NOT AN
        /// OVERSIGHT. Their timers have to keep draining or the punch that started with the hit
        /// would begin only after the freeze released, which reads as two separate events
        /// instead of one. What is suspended is the FOLLOW: the rig does not re-derive its
        /// position from the character it is tracking.
        /// </summary>
        private bool StepHold()
        {
            if (_holdLeft <= 0.0f) return false;

            _holdLeft -= Time.unscaledDeltaTime;
            StepShake();
            return true;
        }

        private void StepShake()
        {
            if (_impactPunchLeft > 0.0f)
            {
                _impactPunchLeft -= Time.deltaTime;
                float punchRatio = Mathf.Clamp01(_impactPunchLeft / ImpactPunchDuration);
                transform.position += _impactPunchOffset * punchRatio;
            }

            if (_shakeLeft <= 0.0f) return;

            _shakeLeft -= Time.deltaTime;
            float k = Mathf.Clamp01(_shakeLeft) * _shakeStrength;

            transform.position += new Vector3(
                (Random.value - 0.5f) * 2.0f * k * 0.1f,
                (Random.value - 0.5f) * 2.0f * k * 0.1f,
                0.0f);

            if (_shakeLeft <= 0.0f) _shakeStrength = 0.0f;
        }

        /// <summary>A punch of the arms toward the player on an action, so a verb is felt.</summary>
        public void ViewmodelKick(Vector3 direction, float strength = 1.0f)
        {
            _vmKickLeft = VmKickTime;
            _vmKickOffset = direction.normalized * 0.06f * strength;
        }

        private void StepViewmodelKick()
        {
            if (_viewmodel == null) return;

            if (_vmKickLeft > 0.0f) _vmKickLeft -= Time.deltaTime;

            float k = _vmKickLeft <= 0.0f ? 0.0f : _vmKickLeft / VmKickTime;
            _viewmodel.localPosition = ViewmodelSeat + _vmKickOffset * k;
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ AN EMOTE SWITCHES TO A THIRD-PERSON LOOK, because the whole point of an emote is
        /// that YOU can see it too. In first person a player performing one sees nothing at all
        /// and reasonably concludes it did not fire.
        ///
        /// ⚠️⚠️ AND THE BODY HAS TO BE GIVEN BACK, WHICH IS THE HALF THAT WAS MISSING. 🧑
        /// 2026-08-18: *"doing emote doesnt show myself in tpp, i think my body is hidden"*. He
        /// is exactly right. <see cref="ApplyFppSelfHide"/> puts every renderer on this unit into
        /// SHADOWS_ONLY, and it only re-runs on `Follow` and `SetActive` — neither of which the
        /// emote swing touches, because the swing deliberately leaves `_mode` alone so
        /// `EndEmoteView` can restore it. So the camera dutifully orbited to a third-person
        /// framing of an invisible person, and the ONE feature whose entire purpose is "you can
        /// see yourself do it" showed an empty street with a shadow on it.
        ///
        /// This is the same shape as the emote-wheel-seat bug: a state that is correct for FPP
        /// leaking into a view that is no longer FPP. The self-hide is a property of what the
        /// camera is LOOKING AT, so it is toggled by the same two functions that decide that.
        /// </summary>
        /// <summary>
        /// Swings out to third person while the local player is on the floor, and back when they
        /// stand up.
        ///
        /// ⚠️⚠️ FALLING IS ONE OF ONLY TWO THINGS IN THE GAME THAT EARN THE CAMERA, and the rule
        /// that decides it is `docs/Hero_Strike_Balance.md` § 8.6: an ability or event takes the
        /// camera only when the body the player is driving changes, or when they stop driving it.
        /// A fall is the second of those. Every hero SKILL is refused by that same rule because
        /// its arms already say it, so this is not a general licence to swing the camera around.
        ///
        /// 🧑, after playing the build: *"i dont feel like i fell down"*. In first person a fall
        /// is the floor arriving and then two and a half seconds of looking at it. The player
        /// cannot see the knockdown clip, cannot see themselves get up, and the one moment the
        /// game most wants them to feel happens entirely off screen.
        ///
        /// ⚠️ IT REUSES THE EMOTE SWING RATHER THAN ADDING A SECOND ONE, deliberately. That path
        /// already solves the hard half: `BeginEmoteView` calls `RestoreSelfHide`, and without it
        /// the camera orbits a body that `ApplyFppSelfHide` has put into SHADOWS_ONLY, which is
        /// the exact bug 🧑 reported for emotes as *"doing emote doesnt show myself in tpp, i
        /// think my body is hidden"*. A fresh fall-specific path would have rediscovered it.
        ///
        /// ⚠️ AND IT IS A CUT, NOT A BLEND, WHICH IS THE OPPOSITE OF THE POSSESSION.
        /// `PossessBlendSeconds` travels the eye because a possession is a TRANSFORMATION and the
        /// player has to see themselves leave. A fall is an IMPACT: cutting on the frame the
        /// body hits is what sells it, and easing out over a quarter second would read as a
        /// cutscene starting rather than as being knocked over.
        /// </summary>
        private void StepFallView()
        {
            bool down = _character != null && _character.IsTripped;
            if (down == _fallView) return;

            // ⚠️ AN EMOTE ALREADY OWNS THE SWING, SO DO NOT TAKE IT FROM ONE. `EmotePlayer.Stop`
            // is reached by losing the right to act, and a trip does exactly that, so an emote
            // running when the body goes down ends itself and hands the view back through
            // `OnEmoteStopped`. Grabbing it here as well would have two owners for one flag.
            if (down && _emoteView) return;

            _fallView = down;

            if (down) BeginEmoteView();
            else EndEmoteView();
        }

        public void BeginEmoteView()
        {
            if (_emoteView) return;

            _emoteView = true;
            _modeBeforeEmote = _mode;

            // ⚠️ SEEDED FROM THE BODY'S OWN FACING AND THE TPP PITCH, NO `+ 180`. That is what
            // `camera_rig.gd::begin_emote_view` does, and its note says why: the camera opens
            // BEHIND the character it is about to orbit rather than snapping to world north. The
            // extra half turn here opened it in front of the face instead, which is a different
            // shot from the one the original gives.
            _emoteYawDeg = BodyYawDeg();
            _emotePitchDeg = _tppPitchDeg;

            if (_viewmodel != null) _viewmodel.gameObject.SetActive(false);

            // The whole reason to swing out here. See this function's own note.
            RestoreSelfHide();
        }

        public void EndEmoteView()
        {
            if (!_emoteView) return;

            _emoteView = false;
            _mode = _modeBeforeEmote;

            if (_viewmodel != null)
                _viewmodel.gameObject.SetActive(_active && _mode == CameraMode.Fpp);

            // ⚠️ AND HIDE IT AGAIN ON THE WAY BACK, or the emote leaves the player looking at
            // their own shoulders and a second set of arms for the rest of the round. The guard
            // inside ApplyFppSelfHide makes this a no-op if the rig came back to TPP.
            ApplyFppSelfHide();
        }

        public bool IsEmoteView => _emoteView;

        /// <summary>
        /// § THE ORBIT. `camera_rig.gd`'s emote branch of `_unhandled_input`.
        ///
        /// ⚠️⚠️ AN EMOTE ORBITS, IT DOES NOT STEER, AND THE PORT COULD DO NEITHER. 🧑 2026-08-04
        /// on the Godot build: *"make srue i can move camera around while im emoting but its
        /// anchored to my body"*, and 🧑 2026-08-18 on this one: *"im supposed to be able to
        /// rotate my camera btw when i emote"*. `LateUpdate` returned before `StepLook` for the
        /// whole duration, so the emote view was frozen on whatever bearing it opened at: the
        /// mouse did nothing at all until the emote ended.
        ///
        /// ⚠️ IT WRITES THIS RIG'S OWN YAW AND PITCH AND NEVER TOUCHES THE BODY, which is the
        /// half that makes it an orbit. Every other frame in this class turns `_character` with
        /// the mouse; doing that here would spin the dancing body on the spot for all three
        /// other players while its owner merely looked around.
        ///
        /// ⚠️ AND THE WHEEL STILL OWNS THE MOUSE WHILE IT IS OPEN, the same check `StepLook`
        /// makes. B opens the wheel, and a player picking a slice must not also swing the camera.
        /// </summary>
        private void StepEmoteLook()
        {
            if (_aimSource != AimSource.Mouse) return;
            if (_character.Intent.Parked) return;
            if (UI.EmoteWheel.AnyOpen) return;

            var s = Settings.SettingsStore.Current;
            float sens = BaseSensitivity * s.MouseSensitivity;

            float dx = Input.GetAxisRaw("Mouse X") * sens;
            float dy = Input.GetAxisRaw("Mouse Y") * sens;
            if (s.InvertY) dy = -dy;

            _emoteYawDeg += dx * 10.0f;
            _emotePitchDeg = Mathf.Clamp(_emotePitchDeg - dy * 10.0f,
                                         EmotePitchMinDeg, EmotePitchMaxDeg);
        }

        /// <summary>
        /// ⚠️⚠️ THE SAME BOOM TPP ALREADY USES, NOT A SEPARATE ONE. `camera_rig.gd`'s emote
        /// branch writes `tpp_arm`'s transform directly and lets the SAME `SpringArm3D` — mount
        /// height, spring length, wall collision, all of it — do the rest; it is not a second,
        /// shorter arm invented for the occasion. The port's first version was exactly that: a
        /// hand-picked 2.6 m at a flat 1.0 m up, which put the shot at shoulder height on a
        /// child-sized rig and roughly half the real TPP distance. Mirroring `ApplyTpp` here
        /// means "how far behind and how high" is one number this file already has to get right
        /// for ordinary third person, not a second one to keep in sync with it by hand.
        /// </summary>
        private void ApplyEmoteView()
        {
            _emotePitchDeg = Mathf.Clamp(_emotePitchDeg, EmotePitchMinDeg, EmotePitchMaxDeg);

            Vector3 mount = _character.transform.position + Vector3.up * TppMountHeight;
            var rot = Quaternion.Euler(_emotePitchDeg, _emoteYawDeg, 0.0f);
            Vector3 wanted = mount - (rot * Vector3.forward) * _tppSpringLength;

            float length = _tppSpringLength;
            if (Physics.SphereCast(mount, 0.2f, (wanted - mount).normalized, out var hit,
                                   _tppSpringLength, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<CharacterMotor>() != _character)
                    length = Mathf.Max(TppMinSpringLength, hit.distance - TppArmMargin);
            }

            transform.SetPositionAndRotation(mount - (rot * Vector3.forward) * length, rot);
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// Where this player is aiming. ⚠️ IN FIRST PERSON IT IS THE SIGHT LINE, which is what
        /// makes the throw leave along the line the player is looking down. Measured: leaving
        /// from the hand instead sags the flight 0.38 to 0.43 m below that line and drops the
        /// slipper out of the bottom of the screen on release.
        /// </summary>
        /// <summary>How far along the crosshair to look for something to aim AT, before giving
        /// up and treating the aim as a bearing rather than a target. `carrier.gd:60`.</summary>
        public const float AimRayLength = 40.0f;

        public Vector3 AimPoint()
        {
            if (_character == null) return Vector3.zero;

            if (_mode == CameraMode.Fpp)
            {
                // ⚠️ IT CASTS, IT DOES NOT PROJECT A FIXED DISTANCE. `carrier.gd::_aim_point`
                // raycasts the crosshair and returns the hit, so aiming at the lata twelve
                // metres away throws AT the lata rather than at a point twenty metres past it.
                // A fixed projection makes every close-range throw overshoot, and the aiming
                // arc drawn from it lands somewhere the slipper never goes.
                var sight = new Ray(transform.position, transform.forward);

                if (Physics.Raycast(sight, out var hit, AimRayLength, ~0,
                                    QueryTriggerInteraction.Ignore)
                    && hit.collider.GetComponentInParent<CharacterMotor>() != _character)
                {
                    return hit.point;
                }

                return transform.position + transform.forward * AimRayLength;
            }

            var ground = new Plane(Vector3.up, _character.transform.position);
            var ray = new Ray(transform.position, transform.forward);

            return ground.Raycast(ray, out float enter)
                ? ray.GetPoint(enter)
                : _character.transform.position + _character.transform.forward * 10.0f;
        }
    }
}
