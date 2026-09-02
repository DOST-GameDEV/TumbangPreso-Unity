using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// SPECTATOR MODE — a free-flying camera with no body. `Design.md` §9.
    /// Converted from `scripts/systems/spectator_camera.gd` (431 lines).
    ///
    /// Human instruction, 2026-07-30: *"implement a Spectator option available in both
    /// Multiplayer and Singleplayer. The spectator acts as a free-flying camera with no
    /// physical model, capable of clipping through all geometry to fly anywhere."*
    ///
    /// ⚠️⚠️ THE CLIPPING IS BY CONSTRUCTION, NOT BY A COLLISION MASK, AND THAT IS THE WHOLE
    /// DESIGN OF THIS FILE. This is a plain Transform with a Camera on it. It is not a
    /// CharacterController, it has no Collider, it is on no physics layer, and it never
    /// calls Move — so there is nothing for the physics engine to resolve and no layer mask
    /// anyone can get wrong later. Moving it is one `transform.position +=`.
    ///
    /// The alternative — a body with its layer mask cleared — looks equivalent and is not:
    /// it still enters the broadphase, it still generates depenetration against anything
    /// that masks IT, and it is one accidental inspector edit away from a spectator who can
    /// be bumped by a slipper.
    ///
    /// ⚠️ AND IT SPAWNS NO CHARACTER AT ALL. A spectator claims no seat (seat -1), is
    /// skipped by the spawn path, and is excluded from the ready gate. Its slot is filled by
    /// the same placeholder-AI path that already fills an empty one, so a 2v2 stays a 2v2.
    ///
    /// ⚠️ IT IS NOT A PLAYER AND MUST NEVER BECOME ONE. Nothing here writes gameplay state,
    /// sends an RPC, or resolves a hit. If a future pass wants a spectator to be able to
    /// nudge anything, that is a different component.
    ///
    /// ⚠️⚠️ AND IT IS DRIVEN BY A HUMAN, ONLY, BY CONSTRUCTION. 🧑 human instruction,
    /// 2026-07-31: *"dont give spectator AI... spectator should only be controllable by a
    /// person."* In Godot that was held by three separate properties. In Unity ONE
    /// structural fact holds it: the AI writes <see cref="InputIntent"/> and never touches
    /// hardware, and this class reads hardware DIRECTLY and never reads an intent. There is
    /// no <see cref="CharacterMotor"/> here for an AIController to attach to either.
    ///
    /// ⚠️ SO DO NOT "TIDY" THIS ONTO PlayerInputReader. That class exists to funnel hardware
    /// into an intent a bot can also write; routing the spectator through it would put a
    /// bot one line away from flying the camera, which is the exact thing the instruction
    /// above forbids.
    ///
    /// If a "cinematic auto-cam" is ever wanted it is a new component with a new name.
    ///
    /// ⚠️⚠️ **THAT AUTO-CAM WAS WANTED ON 2026-08-27 AND IT EXISTS: <see cref="SpectatorDirector"/>.**
    /// 🧑: *"add autopilot option in spectator that moves on its own naturally and looks good"*.
    /// **The 2026-07-31 instruction three paragraphs up is SUPERSEDED by the same person, and
    /// this note is here because that paragraph on its own now reads as forbidding a feature
    /// that ships.** Read them together: what was asked for then, and what is still true, is that
    /// nothing may drive the spectator's INPUT except a person. That holds. The director writes a
    /// POSE onto a transform; this class is still the only thing in the game that reads a
    /// spectator's hardware, and there is still no `CharacterMotor` here for an `AIController` to
    /// attach to. The line above was also right about the shape of the answer, so it was
    /// followed to the letter: a new component, with a new name.
    ///
    /// ⚠️⚠️ AND THE AUTOPILOT MUST NEVER PAUSE OR REPLAY. 🧑, immediately after: *"dont let
    /// autopilot spectator pause or replay thats for human only"*. This class USED to replay by
    /// itself on a knockdown, a tag or a score play, and `Update` suppressed that while the
    /// autopilot was engaged. **The self-replay is deleted outright as of 2026-08-27**, so the
    /// promise no longer rests on a suppression that a later branch could forget: a replay has
    /// one trigger, the `SpectatorReplay` key, and the autopilot has no hands. See § THE REPLAY
    /// NEVER STARTS ITSELF ANY MORE.
    /// </summary>
    public sealed class SpectatorCamera : MonoBehaviour
    {
        /// Metres per second at the base speed. Faster than a Person's 4.6 walk — a
        /// spectator is covering a whole map, not a lane.
        ///
        /// ⚠️⚠️ 12.0 -> 6.0 ON 2026-08-01, ON DIRECT HUMAN INSTRUCTION. 🧑: *"can u allow
        /// spectator to slow down huhu why is it so fast, barely controllable"*.
        /// ⚠️⚠️ 6.0 -> 3.6 ON 2026-08-02, ALSO ON DIRECT HUMAN INSTRUCTION, FOR A USE THIS
        /// CONSTANT HAD NOT BEEN TUNED FOR. 🧑: *"slow down spectator bcz it so fast, cant
        /// record anything with it ... spectator will be used as camera for cinematics but
        /// dont make it too slow"*.
        ///
        /// 3.6 is deliberately BELOW a Person's 4.6 walk. That is the property that matters
        /// for a tracking shot — the camera drifts back through a moving subject rather than
        /// pulling ahead of them. It still crosses the 15 m court in about four seconds, so
        /// it is not a tripod. DO NOT "fix" this to match walk speed.
        public const float BaseSpeed = 3.6f;

        /// Hold Sprint to boost. No stamina: the meter exists to make a chase a decision,
        /// and a spectator has nothing to decide.
        ///
        /// ⚠️ 3.0 -> 2.5, BECAUSE THE BOOST IS THE REPOSITIONING GEAR AND NOT A SECOND
        /// CAMERA. 2.5 against 3.6 is 9.0 m/s: the court in under two seconds when a shot is
        /// being SET UP, and still slow enough to be brought to rest on a mark.
        public const float BoostScale = 2.5f;

        /// Wheel adjusts base speed between these, so framing a close shot of the can and
        /// crossing Bayan Plaza are not fighting the same number.
        public const float SpeedMin = 1.2f;
        public const float SpeedMax = 40.0f;
        public const float SpeedStep = 1.35f;

        /// Wider than the gameplay rig's clamp, because a free camera genuinely wants to
        /// look straight down at the circle. Stops just short of the poles, where yaw and
        /// pitch become the same axis and the view rolls.
        public const float PitchLimitDeg = 88.0f;

        /// Exponential smoothing on POSITION, so a hard stop reads as a camera being flown
        /// rather than as a teleport. Rotation is deliberately NOT smoothed — mouse-look
        /// with any smoothing on it feels like input lag.
        public const float MoveSmoothRate = 14.0f;

        /// ⚠️ §2.6 — FOLLOW DISTANCE IS THE OTHER HALF OF "WIDE SHOTS AND CLOSE SHOTS BOTH".
        /// The wheel retunes FLY speed, which does nothing while following, so the follow
        /// shot used to be a single fixed 6.5 m framing. Same wheel, same gesture, and which
        /// number it moves depends on which mode you are in — because in each mode that is
        /// the only one of the two that does anything.
        public const float FollowDistance = 6.5f;
        public const float FollowDistanceMin = 1.2f;
        public const float FollowDistanceMax = 30.0f;

        /// Metres above the followed unit's origin, scaled with distance rather than held
        /// flat: a 1.2 m close-up wants eye level and a 30 m wide wants to look down.
        public const float FollowLiftRatio = 0.34f;

        // -------------------------------------------------------------------
        /// ⚠️⚠️ POV MODE — V — AND IT IS **THIS** CAMERA AT THEIR EYES, NOT THEIR RIG.
        ///
        /// 🧑 2026-07-31: *"spectator should be allowed to go to anywhere in the map and
        /// watch the povs of people/ai, thats why its called camera."*
        ///
        /// ⚠️ IT DOES NOT ACTIVATE THE TARGET'S CameraRig, AND THAT IS NOT A SHORTCUT — IT
        /// IS THE RULE. Going through the rig would not be free even if it were allowed:
        /// <see cref="CameraRig.SetActive"/> also enables that rig's look pipeline, so a
        /// spectator pressing V would start feeding a live AI unit this machine's mouse —
        /// from a component whose entire contract is that it writes no gameplay state.
        /// Watching somebody must not change what they do.
        ///
        /// So POV is a placement, not a takeover: this camera is parked at the unit's eye
        /// height and its YAW is locked to the unit's facing. Nothing is written to the unit
        /// at all — it does not know it is being watched.
        ///
        /// ⚠️ PITCH STAYS WITH THE OPERATOR. A unit's pitch lives on its rig, and that rig
        /// is inactive for a bot — so there is no honest pitch to copy, and inventing one
        /// would be a made-up number presented as somebody else's view. Leaving pitch on the
        /// mouse is also the better camera: it lets a POV shot tilt down to the lata.
        public const float PovEyeHeightPerson = 1.45f;

        /// A lata or a tsinelas is ankle-height and its "eyes" are a fiction anyway.
        public const float PovEyeHeightProp = 0.42f;

        /// ⚠️ AND IT SITS SLIGHTLY IN FRONT OF THE EYES, NOT INSIDE THE HEAD. Rendered a POV
        /// shot and looked at it: the watched Person's own hat and shoulder hung in the
        /// bottom-left of frame, because a real FPP rig HIDES the head mesh and this camera
        /// is a bystander that has not been given the right to hide anything. Stepping
        /// forward off the unit's own facing clears the model without writing a single
        /// property to it, which is the whole reason POV is a placement rather than a takeover.
        public const float PovForwardOffset = 0.34f;

        /// A wider FOV than the gameplay rigs': a spectator is watching four units at once
        /// rather than aiming at one, and the extra field is what makes the whole circle
        /// readable from the side of the arena.
        public const float SpectatorFov = 78.0f;

        /// Well past the map so a shot from outside the arena does not clip the rooflines.
        public const float SpectatorFar = 400.0f;

        // -------------------------------------------------------------------

        /// <summary>Every unit a spectator may cycle to. Godot used the `spectatable`
        /// group; Unity has no groups, so units register here on spawn. Rebuilt-on-read
        /// semantics are preserved by filtering dead entries at cycle time.</summary>
        private static readonly List<CharacterMotor> Spectatable = new List<CharacterMotor>();

        public static void Register(CharacterMotor unit)
        {
            if (unit != null && !Spectatable.Contains(unit)) Spectatable.Add(unit);
        }

        public static void Unregister(CharacterMotor unit) => Spectatable.Remove(unit);

        private float _yawDeg;

        /// ⚠️ UNITY'S SIGN, NOT GODOT'S. Godot's rotation.x is positive looking UP; Unity's
        /// euler X is positive looking DOWN, so every pitch constant carried over from the
        /// .gd is negated on the way in. The Godot source starts at -26 (looking down at the
        /// circle); that is +26 here. The ±88 clamp is symmetric so it needs no flip.
        private float _pitchDeg = 26.0f;

        private float _speed = BaseSpeed;
        private Vector3 _targetPosition;
        private Camera _camera;

        /// Which unit the camera is following, or null for free flight. Tab cycles, F frees.
        private CharacterMotor _follow;
        private int _followIndex = -1;
        private float _followDistance = FollowDistance;

        /// POV rather than over-the-shoulder. V toggles. Sticky across a Tab cycle on
        /// purpose: somebody filming POV shots wants to step through all four units in POV,
        /// not re-press V at every one.
        private bool _pov;

        private InputAction _move, _jump, _sprint, _down;

        private const float ReplaySeconds = 5.5f;
        private const float ReplaySampleInterval = 0.10f;
        private const int ReplayFrameCapacity = 70;
        private const int ReplayWidth = 854;
        private const int ReplayHeight = 480;
        // ⚠️ THE ROLL-IN DELAY AND THE FLOOR BETWEEN TWO SELF-STARTED REPLAYS ARE BOTH DELETED,
        // because nothing starts a replay but a key now. `DeadFeatureAudit` greps this file for
        // their names, so do not reintroduce either one even as a comment.

        private sealed class ReplayFrame
        {
            public Texture2D Image;
        }

        private readonly List<ReplayFrame> _replayFrames = new List<ReplayFrame>(ReplayFrameCapacity);
        private readonly List<ReplayFrame> _replayClip = new List<ReplayFrame>(ReplayFrameCapacity);
        private float _replayRecordAccum;
        private bool _captureReplayFrame;
        private bool _replaying;
        private float _replayClock;
        private string _replayReason = "LAST PLAY";
        private Canvas _replayCanvas;
        private RawImage _replayImage;
        private Text _replayLabel;

        private string _pendingHighlight;
        private float _pendingHighlightAt = -100.0f;
        private bool _lataStateKnown;
        private bool _lastLataUpright;
        private bool _scoreStateKnown;
        private readonly int[] _lastScores = new int[Balance.PlayerCount];
        private MatchDirector _highlightMatch;

        private bool _broadcastPaused;
        private float _selectedTimeScale = 1.0f;
        private float _initialTimeScale = 1.0f;
        private bool _ownsTimeScale;

        private bool _hasBookmark;
        private Vector3 _bookmarkPosition;
        private Quaternion _bookmarkRotation;
        private float _bookmarkFov;

        private void Awake()
        {
            _initialTimeScale = Time.timeScale > 0.0f ? Time.timeScale : 1.0f;
            _selectedTimeScale = _initialTimeScale;
            _camera = GetComponent<Camera>();
            if (_camera == null) _camera = gameObject.AddComponent<Camera>();
            _camera.fieldOfView = SpectatorFov;
            _camera.farClipPlane = SpectatorFar;
            BuildReplayOverlay();

            // ⚠️⚠️ THE MAP'S GRADE AND ITS TONEMAP, WHICH THIS CAMERA HAD NEITHER OF, AND THAT
            // IS "the characters are all light as frick, same with map and game overall".
            //
            // `TumbangPreso/Toon` is a surface shader, so Unity's forward path ADDS
            // `RenderSettings.ambientLight` after the lit pass. Eskinita's ambient is
            // (0.62, 0.58, 0.52) x 1.65 = (1.02, 0.96, 0.86) — over 1.0 on the red channel
            // before a single light is counted — because the Godot Environment it was measured
            // from tonemaps the composited frame and rolls that back down. Unity's built-in
            // pipeline has no tonemapper at all, so without a pass on the camera the whole
            // frame CLIPS: skin and shirt both land on white, the street goes pale, and the
            // ink outline is the only thing still reading as itself.
            //
            // `CameraRig` has carried `ColourGrade` since the grade was converted. This camera
            // is a fourth rig with its own object (§3a) and was simply never given one, so the
            // fault was invisible to anybody playing in first person and total for anybody
            // spectating — which is every screenshot and every recording, because spectator is
            // the cinematics camera (see BaseSpeed's note).
            //
            // ⚠️ ADOPTED IN Start, NOT HERE, for the reason CameraRig.Awake gives: the arena's
            // own objects are not guaranteed to exist during this component's Awake, and a
            // grade that finds no MapGrade quietly resolves to an identity blit that looks
            // exactly like the feature was never added.
            _grade = GetComponent<Visual.ColourGrade>();
            if (_grade == null) _grade = gameObject.AddComponent<Visual.ColourGrade>();

            // ⚠️⚠️ BETWEEN THE GRADE AND THE REPLAY CAPTURE, AND BOTH SIDES OF THAT MATTER.
            // Unity runs image effects in component order. It goes AFTER `ColourGrade` because
            // it thresholds luma against display-referred numbers and needs a frame that has
            // already been tonemapped out of HDR. It goes BEFORE `SpectatorReplayCapture`
            // (added in `Start`, one method below) because the replay records the picture the
            // spectator saw, and a recording of the pre-filter frame is a recording that is
            // jagged in exactly the footage this camera exists to produce.
            if (GetComponent<Visual.PostAntiAlias>() == null)
                gameObject.AddComponent<Visual.PostAntiAlias>();

            // ⚠️⚠️ THE INK PASS, AND WITHOUT IT THE SPECTATOR WAS WATCHING A DIFFERENT GAME.
            // 🧑 2026-08-29, holding a spectator frame beside a first-person one: *"is it js me
            // or the shaders are very dif for spectator and actual"*, then *"spectatator might
            // not be getting shaders"*. He was reading it correctly and this is the whole of it.
            //
            // `CameraRig.Awake` adds three passes — `ColourGrade`, `PostAntiAlias` and
            // `WorldOutline` — and this camera was given the first two and never the third. The
            // ink edge is not an effect on this game, it IS the art direction (`VISION.md` § 6,
            // *"his UI art is the design system ... wood, amber, cream, ink"*), so a frame
            // without it does not read as a subtler picture, it reads as an untextured one: the
            // screenshots show flat pale facades with no line anywhere against the same street
            // drawn in first person with a black edge on every silhouette.
            //
            // ⚠️⚠️ THIS IS THE SECOND HALF OF THE FAULT `ColourGrade`'S NOTE ABOVE RECORDS, AND
            // IT ARRIVED THE SAME WAY. That note says this camera "is a fourth rig with its own
            // object (§3a) and was simply never given one" — the grade was then added here and
            // the outline, added to `CameraRig` in a different session for a different reason,
            // was not. Two rigs that must look identical and are built by two methods is the
            // shape `docs/TODO.md` §§ 53.1, 57.1, 60, 62.1 and 63.1 each are, one surface
            // further out. **Anything added to `CameraRig`'s post stack belongs here too.**
            //
            // ⚠️ AFTER `PostAntiAlias` AND BEFORE `SpectatorReplayCapture`, WHICH IS EXACTLY
            // `CameraRig`'S ORDER. Unity runs image effects in component order, so matching the
            // order is what makes the two cameras produce the same picture rather than merely
            // carry the same components. The replay is added in `Start`, so it still records
            // last, and it now records the frame the spectator actually saw.
            //
            // ⚠️ `PrototypeEnabled` IS SET FOR THE REASON `CameraRig` SETS IT: the component's
            // own toggle defaults false, so attaching it alone would leave the pass inert and
            // reproduce the *"i dont see any world outlines"* report on this camera only.
            var outline = GetComponent<Visual.WorldOutline>();
            if (outline == null) outline = gameObject.AddComponent<Visual.WorldOutline>();
            outline.PrototypeEnabled = true;

            BindActions();

            // Start above and behind the base circle, looking at it. The circle is at the
            // world origin on every map (`Art_Direction.md` §3), so this is map-independent
            // by construction rather than by a per-map marker somebody has to remember.
            //
            // ⚠️ THE Z IS NEGATED FROM THE GODOT SOURCE, WHICH READS (0, 9, 14). Godot is
            // right-handed with -Z forward; Unity is left-handed with +Z forward. The whole
            // map conversion negates Z for the same reason. At +14 with yaw 0 this camera
            // would open the mode looking at an empty street with the match behind it.
            transform.position = new Vector3(0.0f, 9.0f, -14.0f);
            _targetPosition = transform.position;
            _yawDeg = 0.0f;
            ApplyRotation();

            // Mouse-look needs the cursor locked, exactly as the gameplay rigs do.
            // Re-asserting is harmless and covers being created from a screen that released it.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void BindActions()
        {
            var asset = Resources.Load<InputActionAsset>("TumbangPreso");
            if (asset == null)
            {
                Debug.LogWarning("[Spectator] no InputActionAsset; flight controls are dead.");
                return;
            }

            var map = asset.FindActionMap("Player", throwIfNotFound: false);
            if (map == null) return;

            _move = map.FindAction("Move", false);
            _jump = map.FindAction("Jump", false);
            _sprint = map.FindAction("Sprint", false);
            // ⚠️ `SpectatorDown`, NOT a gameplay action. In Godot this read `guard_dash`,
            // which was deleted with Can-Dash and Flick Dash, and threw
            // "The InputMap action doesn't exist" every single frame a spectator was live.
            _down = map.FindAction("SpectatorDown", false);

            // ⚠️ AN ACTION, NOT A `Keyboard.current` READ, AND THAT IS THE WHOLE POINT OF ADDING
            // IT THIS WAY. 🧑 2026-08-27: *"make sure all keys are in settings and properly
            // classified"*. Every other spectator key in this file is read off the hardware
            // directly and therefore cannot be rebound or even SEEN in the settings panel; the
            // autopilot toggle is the first one that can, and § SPECTATOR AND BROADCAST in
            // `Settings.Rebinding` is where the rest followed it.
            _autopilotToggle = map.FindAction("SpectatorAutopilot", false);

            // § THE REST OF THE SPECTATOR SET, for the same reason. Every one of these was a
            // `Keyboard.current` read until 2026-08-27, which meant the panel could not show it
            // and `Rebinding.FindDuplicateBindings` could not check it.
            _cycleTarget = map.FindAction("SpectatorCycleTarget", false);
            _freeFly = map.FindAction("SpectatorFreeFly", false);
            _povToggle = map.FindAction("SpectatorPov", false);
            _mark = map.FindAction("SpectatorMark", false);
            _recall = map.FindAction("SpectatorRecall", false);
            _pauseKey = map.FindAction("SpectatorPause", false);
            _replayKey = map.FindAction("SpectatorReplay", false);

            map.Enable();
        }

        private Visual.ColourGrade _grade;
        private SpectatorReplayCapture _replayCapture;

        private void Start()
        {
            if (_grade != null) _grade.AdoptFromScene();

            // Added after ColourGrade so the replay records the same graded picture the
            // spectator saw, not the bright pre-tonemap frame that enters the grade pass.
            _replayCapture = gameObject.AddComponent<SpectatorReplayCapture>();
            _replayCapture.Owner = this;
        }

        private void OnEnable() { if (_camera != null) _camera.enabled = true; }

        private void OnDisable()
        {
            EndReplay(showLiveToast: false);
            UnhookHighlights();

            // ⚠️ THE BORROWED BODY GOES BACK WHEN THIS CAMERA DOES. A spectator leaving the arena
            // with a unit still set to `ShadowsOnly` deletes that player from every other view,
            // including their own, and nothing else would ever put them back. See `StepPovArms`.
            RestorePovBody();
            if (_povViewmodel != null) _povViewmodel.gameObject.SetActive(false);

            if (_ownsTimeScale)
            {
                Hitstop.End();
                Time.timeScale = _initialTimeScale;
                _ownsTimeScale = false;
            }
        }

        private void Update()
        {
            // ⚠️⚠️ NOT WHILE AN OVERLAY IS UP. The pause card releases the cursor so its buttons
            // can be clicked, and `Time.timeScale = 0` does not stop an Update — so without this
            // the wheel still retuned the fly speed, Tab still cycled the follow target, and
            // every mouse move meant to reach a button also swung the view behind the menu. The
            // player then resumes pointing somewhere they never aimed.
            //
            // ⚠️ ReclaimView IS SKIPPED WITH THE REST, WHICH IS CORRECT RATHER THAN LAZY: the
            // rigs it defends against are all disabled while the match is stopped, and a camera
            // that keeps raising its own depth against a paused frame is doing nothing.
            if (UI.Panel.AnyOpen) return;

            // ⚠️⚠️ IT RE-CLAIMS THE VIEW EVERY FRAME, AND WITHOUT THIS THE WHOLE MODE IS A LIE.
            //
            // Found in Godot by rendering a spectated match and LOOKING at the frame: every
            // control measured correctly — the speed changed, TAB picked up a target, the HUD
            // stripped — and the picture was a Person's first-person view with its orange
            // viewmodel arms across the bottom of the shot. The camera was flying perfectly
            // and nobody was looking through it.
            //
            // The Unity failure is the same shape with a different mechanism: the highest
            // `depth` enabled Camera renders, and the debug player switcher enables a
            // CameraRig on the seat a spectator has just vacated. A one-shot assert in Awake
            // would only move the race. This is authoritative instead, and that is the
            // correct reading rather than a workaround: a spectator has no rig, no body and
            // no seat, so for as long as this component exists there is no other legitimate
            // owner of the view.
            ReclaimView();

            StepAutopilotKey();

            StepBroadcastKeys();
            if (_replaying)
            {
                StepReplay();
            }

            RecordReplayFrame();

            // ⚠️⚠️ THIS ONLY RECORDS WHAT THE LAST NOTABLE PLAY WAS, AND SINCE 2026-08-27 IT
            // CANNOT START ANYTHING. See § THE REPLAY NEVER STARTS ITSELF ANY MORE. The autopilot
            // suppression that used to live here is gone with the thing it was suppressing: a
            // replay now has exactly one trigger, which is a human pressing the key, and the
            // autopilot has no hands.
            PollHighlights();

            // The replay covers the screen while it runs. The match keeps advancing behind it
            // and the operator keeps the wheel, rather than the camera returning early and
            // freezing both the game and the controls.

            // ⚠⚠ THE HUMAN TAKES THE WHEEL BY MOVING IT, AND THAT IS CHECKED BEFORE ANY OF THE
            // THREE STEPS BELOW RUN. A broadcast operator reaching for the mouse mid-play must
            // not have to find a toggle first, and a camera that argues with its operator for
            // even a few frames is worse than one that never offered to help.
            if (AutopilotEngaged)
            {
                if (ManualTakeover()) _director.Engaged = false;
                else return;   // `SpectatorDirector.LateUpdate` owns the pose this frame
            }

            StepLook();
            StepWheel();
            StepKeys();

            // ⚠️ Update, NOT FixedUpdate. There is no physics here — nothing to step, nothing
            // to collide, nothing another body has to agree with — and a camera that moves on
            // the render frame is smoother than one moving on the physics tick.
            // Broadcast cameras remain responsive during a tactical pause and do not become
            // syrupy in slow motion. The match clock is scaled; the operator is not.
            float delta = Time.unscaledDeltaTime;

            if (_follow != null)
            {
                if (_pov)
                {
                    // ⚠️ SNAPPED, NOT SMOOTHED. MoveSmoothRate is what makes a flown camera
                    // read as flown, and it is exactly wrong here: an eye that lags its own
                    // head by a few frames is the one camera artefact everybody reads as
                    // nauseating. A POV shot is rigid or it is not a POV shot.
                    _targetPosition = _follow.transform.position
                        + Vector3.up * PovEyeHeight()
                        + _follow.transform.forward * PovForwardOffset;
                    transform.position = _targetPosition;

                    // Yaw is TAKEN from the unit; pitch stays on the mouse.
                    _yawDeg = _follow.transform.eulerAngles.y;
                    ApplyRotation();

                    // ⚠️ THE HANDS OF WHOEVER IS BEING WATCHED. See `StepPovArms`.
                    StepPovArms(delta);
                    return;
                }

                // Leaving POV puts the body back and takes the borrowed hands away.
                StepPovArms(delta);

                // Follow mode holds a fixed offset in the camera's own current bearing, so
                // the player still owns the angle and only gives up the position.
                Vector3 back = -transform.forward;
                _targetPosition = _follow.transform.position
                    + Vector3.up * (_followDistance * FollowLiftRatio)
                    + back * _followDistance;
            }
            else
            {
                Vector2 dir = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
                Vector3 move = transform.forward * dir.y + transform.right * dir.x;

                if (_jump != null && _jump.IsPressed()) move += Vector3.up;
                if (_down != null && _down.IsPressed()) move += Vector3.down;

                if (move.magnitude > 0.001f)
                {
                    bool boosting = _sprint != null && _sprint.IsPressed();
                    float speed = _speed * (boosting ? BoostScale : 1.0f);
                    _targetPosition += move.normalized * speed * delta;
                }
            }

            float t = 1.0f - Mathf.Exp(-MoveSmoothRate * delta);
            transform.position = Vector3.Lerp(transform.position, _targetPosition, t);
        }

        private void ReclaimView()
        {
            if (_camera == null) return;
            if (!_camera.enabled) _camera.enabled = true;

            // Depth, not just enabled: another rig left enabled would otherwise win on ties.
            foreach (var cam in Camera.allCameras)
            {
                if (cam == _camera) continue;
                if (cam.enabled && cam.depth >= _camera.depth) _camera.depth = cam.depth + 1.0f;
            }
        }

        /// <summary>
        /// ⚠️ THE SAME SENSITIVITY MODEL THE GAMEPLAY RIG USES, read through the same
        /// settings multiplier and the same ×10 degree factor as
        /// <see cref="CameraRig.StepLook"/> — a spectator whose look speed disagrees with
        /// the game's reads as a different game.
        /// </summary>
        private void StepLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            var s = Settings.SettingsStore.Current;
            float sens = CameraRig.BaseSensitivity * s.MouseSensitivity;

            float dx = Input.GetAxisRaw("Mouse X") * sens;
            float dy = Input.GetAxisRaw("Mouse Y") * sens;
            if (s.InvertY) dy = -dy;

            // ⚠️ YAW ADDS HERE AND SUBTRACTS IN THE .gd. Same handedness flip as the start
            // position above: in Godot a rightward mouse move decreases yaw, in Unity it
            // increases it. Copying the sign across would invert mouse-look for spectators
            // only, which reads as a broken build rather than as a convention mismatch.
            _yawDeg += dx * 10.0f;
            _pitchDeg = Mathf.Clamp(_pitchDeg - dy * 10.0f, -PitchLimitDeg, PitchLimitDeg);
            ApplyRotation();
        }

        /// <summary>
        /// Following: the wheel pulls in and pushes out. Free: it retunes the fly speed.
        /// See <see cref="FollowDistance"/> for why one control does both.
        /// </summary>
        private void StepWheel()
        {
            if (Mouse.current == null) return;
            float wheel = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) < 0.01f) return;

            bool following = _follow != null;
            if (wheel > 0.0f)
            {
                if (following)
                    _followDistance = Mathf.Clamp(_followDistance / SpeedStep,
                        FollowDistanceMin, FollowDistanceMax);
                else
                    _speed = Mathf.Clamp(_speed * SpeedStep, SpeedMin, SpeedMax);
            }
            else
            {
                if (following)
                    _followDistance = Mathf.Clamp(_followDistance * SpeedStep,
                        FollowDistanceMin, FollowDistanceMax);
                else
                    _speed = Mathf.Clamp(_speed / SpeedStep, SpeedMin, SpeedMax);
            }
        }

        // -------------------------------------------------------------------
        // BROADCAST CONTROLS.
        //
        // ⚠⚠ PAUSE AND SPEED ARE NETWORKED NOW, AND THIS BLOCK USED TO REFUSE THEM OUTRIGHT.
        // It read *"offline-only by construction: a remote viewer must never acquire authority
        // over a live tournament simply by spectating"* and answered every press with
        // `LIVE NETWORK · TIME CONTROLS LOCKED`. 🧑 2026-08-30, after being asked which of the
        // game's two pauses he meant: *"pause is for spectatotr"*, *"give spectators the authority
        // to pause, all of them can pause"*, *"make sure time pauses if u pause as well as
        // everything happening and spectator can move"*, *"liek in game like mobile legends"*.
        //
        // The old rule guarded a tournament against a stranger. The spectators here are the people
        // waiting for the next match and whoever is casting it, and an observer stopping the game
        // to talk over a fight is the feature he is naming. **Every spectator can**, which he said
        // twice, so there is no leader check.
        //
        // ⚠ THE HOST REMAINS THE ONLY WRITER OF THE CLOCK. This sends a REQUEST and applies
        // nothing locally; `MatchRpc.RequestTimeScaleServerRpc` carries the whole reasoning and
        // the refusal for a peer that holds a seat. Four peers each writing `Time.timeScale` is
        // four matches.
        //
        // ⚠ SOLO IS UNCHANGED and still writes the clock directly, because there is nobody to
        // ask and no second machine to disagree with.
        //
        // Replay is a local pixel overlay and is safe on either side of the wire.

        private void StepBroadcastKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (_replaying)
            {
                if (Fired(_replayKey) || kb.escapeKey.wasPressedThisFrame)
                    EndReplay();
                return;
            }

            if (Fired(_mark))
            {
                _bookmarkPosition = transform.position;
                _bookmarkRotation = transform.rotation;
                _bookmarkFov = _camera != null ? _camera.fieldOfView : SpectatorFov;
                _hasBookmark = true;
                UI.Hud.Instance?.ShowToast("CAMERA MARK SAVED  ·  [N] TO RECALL", 1.2f);
            }

            if (Fired(_recall) && _hasBookmark)
            {
                _follow = null;
                _followIndex = -1;
                _pov = false;
                transform.SetPositionAndRotation(_bookmarkPosition, _bookmarkRotation);
                _targetPosition = _bookmarkPosition;
                if (_camera != null) _camera.fieldOfView = _bookmarkFov;
                SyncAnglesFromTransform();
                UI.Hud.Instance?.ShowToast("CAMERA MARK RECALLED", 0.9f);
            }

            if (kb.f1Key.wasPressedThisFrame) SelectPlayerPov(0);
            if (kb.f2Key.wasPressedThisFrame) SelectPlayerPov(1);
            if (kb.f3Key.wasPressedThisFrame) SelectPlayerPov(2);
            if (kb.f4Key.wasPressedThisFrame) SelectPlayerPov(3);

            // ⚠️ THE THREE SPEED DIGITS STAY LITERAL AND THAT IS DELIBERATE. They are a
            // NUMBERED SET (quarter, half, three quarter speed) the way F1 to F4 are a POSITIONAL
            // set, and splitting either into separate rebindable rows would add seven lines to
            // the settings panel to let somebody move "2" to "5". `ControlsText` names them.
            bool askedForTime = Fired(_pauseKey)
                                || kb.digit1Key.wasPressedThisFrame
                                || kb.digit2Key.wasPressedThisFrame
                                || kb.digit3Key.wasPressedThisFrame;

            // ⚠⚠ A NETWORKED TIME PRESS IS A REQUEST, AND ONLY A SPECTATOR MAY MAKE ONE. The
            // host answers and tells everybody; see the header above and
            // `MatchRpc.RequestTimeScaleServerRpc`. A peer holding a chair is refused there too,
            // so this check is the courtesy message rather than the rule.
            if (askedForTime && NetAuthority.IsNetworked)
            {
                if (!GameLaunch.Spectator)
                {
                    UI.Hud.Instance?.ShowToast("LIVE MATCH  ·  ONLY A SPECTATOR MAY PAUSE", 1.5f);
                    return;
                }

                // ⚠ THE LOCAL BOOKKEEPING STILL MOVES, so the next press toggles the other way and
                // the overlay reads correctly. What it does NOT do is write `Time.timeScale`: the
                // number arrives back through `SyncTime`, which is what keeps four screens on one
                // clock even when a packet is late.
                if (Fired(_pauseKey))
                {
                    _broadcastPaused = !_broadcastPaused;
                    Net.MatchRpc.Instance?.RequestTimeScaleServerRpc(
                        _broadcastPaused ? 0.0f : _selectedTimeScale);
                }

                if (kb.digit1Key.wasPressedThisFrame) RequestBroadcastScale(0.25f);
                if (kb.digit2Key.wasPressedThisFrame) RequestBroadcastScale(0.50f);
                if (kb.digit3Key.wasPressedThisFrame) RequestBroadcastScale(1.00f);
                return;
            }

            if (Fired(_pauseKey)) ToggleBroadcastPause();
            // ⚠️ THE ONE AND ONLY TRIGGER. See § THE REPLAY NEVER STARTS ITSELF ANY MORE. The
            // reason is looked up rather than passed so the clip is titled after the play it
            // actually contains.
            if (Fired(_replayKey)) StartReplay(RecentHighlightReason());
            if (kb.digit1Key.wasPressedThisFrame) SetBroadcastScale(0.25f);
            if (kb.digit2Key.wasPressedThisFrame) SetBroadcastScale(0.50f);
            if (kb.digit3Key.wasPressedThisFrame) SetBroadcastScale(1.00f);
        }

        private void ToggleBroadcastPause()
        {
            Hitstop.End();
            _ownsTimeScale = true;
            _broadcastPaused = !_broadcastPaused;
            Time.timeScale = _broadcastPaused ? 0.0f : _selectedTimeScale;
            UI.Hud.Instance?.ShowToast(_broadcastPaused
                ? "TACTICAL PAUSE  ·  CAMERA STILL LIVE"
                : $"BACK TO ACTION  ·  {_selectedTimeScale:0.##}x", 1.1f);
        }

        /// <summary>
        /// The networked half of <see cref="SetBroadcastScale"/>: pick a speed and ask for it.
        ///
        /// ⚠ IT SETS `_selectedTimeScale` LOCALLY SO THE NEXT UN-PAUSE ASKS FOR THE RIGHT SPEED.
        /// That field is this camera's memory of what "back to action" means, and it is not
        /// authoritative over anything: the clock everybody actually runs on is whatever
        /// `SyncTime` last delivered.
        /// </summary>
        private void RequestBroadcastScale(float scale)
        {
            _broadcastPaused = false;
            _selectedTimeScale = Mathf.Clamp(scale, 0.25f, 1.0f);
            Net.MatchRpc.Instance?.RequestTimeScaleServerRpc(_selectedTimeScale);
        }

        private void SetBroadcastScale(float scale)
        {
            Hitstop.End();
            _ownsTimeScale = true;
            _broadcastPaused = false;
            _selectedTimeScale = Mathf.Clamp(scale, 0.25f, 1.0f);
            Time.timeScale = _selectedTimeScale;
            UI.Hud.Instance?.ShowToast(_selectedTimeScale < 1.0f
                ? $"BROADCAST SLOW-MO  ·  {_selectedTimeScale:0.##}x"
                : "BROADCAST SPEED  ·  LIVE", 1.1f);
        }

        private void RecordReplayFrame()
        {
            if (_broadcastPaused) return;

            _replayRecordAccum += Time.unscaledDeltaTime;
            if (_replayRecordAccum < ReplaySampleInterval) return;
            _replayRecordAccum %= ReplaySampleInterval;

            _captureReplayFrame = true;
        }

        /// <summary>
        /// Copies a small post-render frame into the replay ring. The replay is pixels rather
        /// than rewound scene transforms, so showing it cannot move a live player, lata or
        /// slipper and cannot require <c>Time.timeScale = 0</c>.
        /// </summary>
        internal void CaptureReplayFrame(RenderTexture source)
        {
            if (!_captureReplayFrame) return;
            _captureReplayFrame = false;

            var scratch = RenderTexture.GetTemporary(ReplayWidth, ReplayHeight, 0,
                                                      RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;

            try
            {
                Graphics.Blit(source, scratch);
                RenderTexture.active = scratch;

                var texture = new Texture2D(ReplayWidth, ReplayHeight, TextureFormat.RGB24,
                                            mipChain: false)
                {
                    name = "SpectatorReplayFrame",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                texture.ReadPixels(new Rect(0, 0, ReplayWidth, ReplayHeight), 0, 0, false);
                texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);

                _replayFrames.Add(new ReplayFrame { Image = texture });
                while (_replayFrames.Count > ReplayFrameCapacity)
                {
                    DestroyFrame(_replayFrames[0]);
                    _replayFrames.RemoveAt(0);
                }
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(scratch);
            }
        }

        private void StartReplay(string reason)
        {
            if (_broadcastPaused)
            {
                UI.Hud.Instance?.ShowToast("RESUME LIVE PLAY BEFORE REPLAY", 1.2f);
                return;
            }

            int wanted = Mathf.CeilToInt(ReplaySeconds / ReplaySampleInterval);
            if (_replayFrames.Count < Mathf.Min(12, wanted))
            {
                UI.Hud.Instance?.ShowToast("REPLAY BUFFER IS STILL WARMING UP", 1.2f);
                return;
            }

            if (_replaying) EndReplay(showLiveToast: false);

            int first = Mathf.Max(0, _replayFrames.Count - wanted);
            for (int i = 0; i < first; i++) DestroyFrame(_replayFrames[i]);

            _replayClip.Clear();
            for (int i = first; i < _replayFrames.Count; i++)
                _replayClip.Add(_replayFrames[i]);
            _replayFrames.Clear();

            _replayClock = 0.0f;
            _replaying = true;
            _replayReason = string.IsNullOrEmpty(reason) ? "LAST PLAY" : reason;

            if (_replayCanvas != null) _replayCanvas.enabled = true;
            if (_replayLabel != null) _replayLabel.text = "INSTANT REPLAY  ·  " + _replayReason;
            if (_replayImage != null && _replayClip.Count > 0)
                _replayImage.texture = _replayClip[0].Image;

            // ⚠️ NO TOAST. The overlay now covers the screen and titles itself in 30 pt across the
            // top; a line underneath it saying the same words is the redundancy 🧑 asked to be rid
            // of across the whole HUD on 2026-08-27.
        }

        private void StepReplay()
        {
            int available = _replayClip.Count;
            if (available < 2) { EndReplay(); return; }

            // A restrained 0.82x lets the decisive beat read while the match remains live in
            // the rest of the screen.
            _replayClock += Time.unscaledDeltaTime * 0.82f;
            float sample = _replayClock / ReplaySampleInterval;
            int localIndex = Mathf.Clamp(Mathf.FloorToInt(sample), 0, available - 1);

            if (sample >= available)
            {
                EndReplay();
                return;
            }

            if (_replayImage != null) _replayImage.texture = _replayClip[localIndex].Image;
        }

        private void EndReplay(bool showLiveToast = true)
        {
            if (!_replaying) return;

            _replaying = false;
            if (_replayCanvas != null) _replayCanvas.enabled = false;
            if (_replayImage != null) _replayImage.texture = null;

            foreach (var frame in _replayClip) DestroyFrame(frame);
            _replayClip.Clear();

            if (showLiveToast) UI.Hud.Instance?.ShowToast("LIVE", 0.8f);
        }

        private void BuildReplayOverlay()
        {
            var canvasGo = new GameObject("InstantReplayOverlay");
            canvasGo.transform.SetParent(transform, false);

            _replayCanvas = canvasGo.AddComponent<Canvas>();
            _replayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _replayCanvas.overrideSorting = true;
            _replayCanvas.sortingOrder = 500;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
            scaler.matchWidthOrHeight = 1.0f;

            // ⚠️⚠️ THE ONE CANVAS IN THE GAME THAT WAS STILL MISSING THE ASPECT RULE.
            // `AspectSafeCanvas` opens by calling itself *"one rule for every canvas in the
            // game"*, and every other screen-space canvas here routes through it: the HUD, the
            // menus, the result board, the role-swap card, the splash, the you-card, the arrows
            // and every imported screen via `ConvertedScreen`. This one was built with a bare
            // `matchWidthOrHeight = 1.0`, which is match-on-HEIGHT, so on anything narrower than
            // 16:9 the spectator's picture-in-picture was cropped off the side of the display.
            // `ComicPopup` is the only other holdout and it is correctly exempt: it is a WORLD
            // canvas on `ConstantPixelSize`, which `Apply` no-ops on by design.
            UI.AspectSafeCanvas.Apply(scaler);

            var panelGo = new GameObject("ReplayPictureInPicture");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panel = panelGo.AddComponent<Image>();
            panel.sprite = UI.GodotTheme.Box(UI.UiTheme.WoodDark, UI.UiTheme.Highlight,
                                             UI.GodotTheme.WoodBorderWidth,
                                             UI.GodotTheme.WoodCornerRadius);
            panel.type = Image.Type.Sliced;
            panel.raycastTarget = false;

            // ⚠️⚠️ THE WHOLE SCREEN, NOT A CORNER BOX. 🧑 2026-08-27: *"i alsoo really dont like
            // that instant replay on the top right"* and *"i want it to cover whole screen if i
            // click it"*. A picture-in-picture was the right shape for something that opened by
            // itself while the operator was still framing a live shot; now that a replay only
            // exists because a human asked for it, the live shot is not what they are watching.
            // A 45 per cent box in the corner was also the worst of both: too small to read a
            // play in and big enough to ruin the frame behind it.
            var panelRt = panel.rectTransform;
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var imageGo = new GameObject("ReplayImage");
            imageGo.transform.SetParent(panelGo.transform, false);
            _replayImage = imageGo.AddComponent<RawImage>();
            _replayImage.color = Color.white;
            _replayImage.raycastTarget = false;
            _replayImage.uvRect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);

            var imageRt = _replayImage.rectTransform;
            imageRt.anchorMin = Vector2.zero;
            imageRt.anchorMax = Vector2.one;
            imageRt.offsetMin = new Vector2(10.0f, 10.0f);
            imageRt.offsetMax = new Vector2(-10.0f, -62.0f);

            // ⚠️⚠️ THE CLIP KEEPS ITS OWN ASPECT INSIDE THAT RECT. The buffer is captured at a
            // fixed 854 x 480, and stretching 16:9 frames to fill an arbitrary window distorts
            // every body in the shot; a 4:3 or ultrawide panel would make the replay visibly a
            // different game from the live view it is covering. `FitInParent` letterboxes instead,
            // which is what every broadcast replay does and what the corner box got for free by
            // being authored at 16:9.
            var fit = imageGo.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = ReplayWidth / (float)ReplayHeight;

            var labelGo = new GameObject("ReplayLabel");
            labelGo.transform.SetParent(panelGo.transform, false);
            _replayLabel = labelGo.AddComponent<Text>();
            _replayLabel.font = UI.MenuKit.Font;
            _replayLabel.fontSize = 30;
            _replayLabel.alignment = TextAnchor.MiddleLeft;
            _replayLabel.color = UI.UiTheme.Highlight;
            _replayLabel.raycastTarget = false;
            _replayLabel.text = "INSTANT REPLAY";

            var outline = labelGo.AddComponent<Outline>();
            outline.effectColor = UI.UiTheme.Ink;
            outline.effectDistance = new Vector2(3.0f, -3.0f);

            var labelRt = _replayLabel.rectTransform;
            labelRt.anchorMin = new Vector2(0.0f, 1.0f);
            labelRt.anchorMax = new Vector2(1.0f, 1.0f);
            labelRt.pivot = new Vector2(0.5f, 1.0f);
            labelRt.offsetMin = new Vector2(24.0f, -56.0f);
            labelRt.offsetMax = new Vector2(-24.0f, -10.0f);

            _replayCanvas.enabled = false;
        }

        private void PollHighlights()
        {
            TryHookHighlights();

            bool lataKnockedNow = false;

            var lata = GameServices.Round != null ? GameServices.Round.Lata : null;
            if (lata == null)
            {
                _lataStateKnown = false;
            }
            else if (!_lataStateKnown)
            {
                _lataStateKnown = true;
                _lastLataUpright = lata.IsUpright;
            }
            else
            {
                lataKnockedNow = _lastLataUpright && !lata.IsUpright;
                if (lataKnockedNow) QueueHighlight("LATA KNOCKDOWN");
                _lastLataUpright = lata.IsUpright;
            }

            var match = GameServices.Match;
            if (match == null)
            {
                _scoreStateKnown = false;
                return;
            }

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                int score = match.ScoreFor(slot);
                if (_scoreStateKnown && !lataKnockedNow)
                {
                    int gain = score - _lastScores[slot];
                    if (gain >= 100)
                        QueueHighlight(slot == match.DefenderSlot ? "TAG" : "SCORE PLAY");
                    else if (gain >= 50)
                        QueueHighlight("SABOTAGE");
                }
                _lastScores[slot] = score;
            }
            _scoreStateKnown = true;
        }

        private void TryHookHighlights()
        {
            var match = GameServices.Match;
            if (_highlightMatch == match) return;

            UnhookHighlights();
            _highlightMatch = match;
            if (_highlightMatch != null) _highlightMatch.Scored += OnHighlightScored;
        }

        private void UnhookHighlights()
        {
            if (_highlightMatch != null) _highlightMatch.Scored -= OnHighlightScored;
            _highlightMatch = null;
        }

        private void OnHighlightScored(int slot, ScoreEvent scoreEvent)
        {
            switch (scoreEvent)
            {
                case ScoreEvent.LataKnocked:
                    QueueHighlight("LATA KNOCKDOWN");
                    break;
                case ScoreEvent.Tag:
                    QueueHighlight("TAG");
                    break;
                case ScoreEvent.Sabotage:
                    QueueHighlight("SABOTAGE");
                    break;
            }
        }

        // -------------------------------------------------------------------
        // § THE REPLAY NEVER STARTS ITSELF ANY MORE
        //
        // ⚠️⚠️ 🧑 2026-08-27, with two screenshots: *"why is instant replay just spam showing"*,
        // *"i alsoo really dont like that instant replay on the top right"*, and *"i want it to
        // cover whole screen if i click it and i dont want it to just loop every second"*.
        //
        // ⚠️⚠️ IT WAS NEVER LOOPING. `StepReplay` plays the clip once and ends. What produced the
        // "every second" reading is that it fired on EVERY scoring event with a 4.0 s floor
        // between them, and Hero Strike scores constantly: a knockdown, a tag and a sabotage are
        // three separate triggers, and `PollHighlights` adds a fourth by watching the lata on top
        // of the `Scored` event that already reports the same knockdown. In an 8-round match
        // that is a picture-in-picture window opening again about as fast as the cooldown allows,
        // forever, in the corner of the shot the operator is trying to frame.
        //
        // ⚠️⚠️ AND A REPLAY THAT ARRIVES UNINVITED IS THE WRONG FEATURE ANYWAY. The whole value
        // of an instant replay is that a human decided the last five seconds were worth seeing
        // again. `SpectatorReplay` is a bound, rebindable key; that press is the trigger now, and
        // it is the only one. This also finishes what `AutopilotSuppressesAutoReplay` started:
        // that suppressed self-replay for the AUTOPILOT only, and the same argument (*"thats for
        // human only"*) applies just as well to a human flying the camera by hand.
        //
        // ⚠️ THE HIGHLIGHT REASONS SURVIVE AS A LABEL, NOT AS A TRIGGER. `PollHighlights` still
        // records what the last notable play was, so a manual replay is titled `INSTANT REPLAY ·
        // TAG` rather than `LAST PLAY`. Naming what you are about to watch costs nothing and is
        // the only part of the highlight reel that was ever earning its place.
        // -------------------------------------------------------------------

        private void QueueHighlight(string reason)
        {
            _pendingHighlight = reason;
            _pendingHighlightAt = Time.unscaledTime;
        }

        /// <summary>
        /// The last notable play, if it is recent enough to still be inside the replay buffer.
        ///
        /// ⚠️ IT EXPIRES WITH THE BUFFER. `ReplaySeconds` is what a manual press actually gets to
        /// show, so a reason older than that would title the clip after a play that is no longer
        /// in it. Past that it falls back to LAST PLAY, which is honest.
        /// </summary>
        private string RecentHighlightReason()
        {
            if (string.IsNullOrEmpty(_pendingHighlight)) return "LAST PLAY";
            return Time.unscaledTime - _pendingHighlightAt <= ReplaySeconds
                ? _pendingHighlight
                : "LAST PLAY";
        }

        private void DestroyFrame(ReplayFrame frame)
        {
            if (frame != null && frame.Image != null) Destroy(frame.Image);
        }

        private void OnDestroy()
        {
            UnhookHighlights();
            foreach (var frame in _replayFrames) DestroyFrame(frame);
            foreach (var frame in _replayClip) DestroyFrame(frame);
            _replayFrames.Clear();
            _replayClip.Clear();
        }

        private void SyncAnglesFromTransform()
        {
            Vector3 euler = transform.eulerAngles;
            _yawDeg = euler.y;
            _pitchDeg = euler.x > 180.0f ? euler.x - 360.0f : euler.x;
        }

        // -------------------------------------------------------------------
        // § THE AUTOPILOT HANDOVER
        //
        // ⚠️⚠️ EVERYTHING IN THIS SECTION IS POSE, NOT CONTROL. `SpectatorDirector` decides
        // where the camera should be; this class remains the only thing in the game that reads
        // the spectator's hardware, which is what keeps the 2026-07-31 instruction
        // (*"spectator should only be controllable by a person"*) structurally true even though
        // the 2026-08-27 request added a camera that flies itself. See that class's header.
        // -------------------------------------------------------------------

        private SpectatorDirector _director;

        public bool AutopilotEngaged => _director != null && _director.Engaged;

        /// <summary>
        /// Writes a pose the director computed back into this class's own state.
        ///
        /// ⚠️⚠️ IT SETS `_targetPosition` AS WELL AS THE TRANSFORM, AND MISSING THAT IS A ONE
        /// FRAME SNAP AT EVERY HANDOVER. `Update` eases `transform.position` toward
        /// `_targetPosition` every frame it owns the camera, so a director that moved only the
        /// transform would hand the human a camera that immediately flies back to wherever the
        /// autopilot was engaged from. The angles are the same story for `StepLook`.
        /// </summary>
        public void AdoptPose(Vector3 position, float yawDeg, float pitchDeg)
        {
            _targetPosition = position;
            _yawDeg = yawDeg;
            _pitchDeg = Mathf.Clamp(pitchDeg, -PitchLimitDeg, PitchLimitDeg);
        }

        /// <summary>Take the angles that are actually on screen. See <see cref="AdoptPose"/>.</summary>
        public void AdoptCurrentAngles()
        {
            SyncAnglesFromTransform();
            _targetPosition = transform.position;
        }

        /// <summary>
        /// Did the operator just ask for the camera back?
        ///
        /// ⚠️ THE MOUSE THRESHOLD IS NOT ZERO AND IT IS NOT TASTE. A mouse at rest still reports
        /// single-count jitter on most sensors, and a zero test hands the camera back within a
        /// second of engaging every single time, which reads as the feature not working. A tenth
        /// of a degree of deliberate movement clears it and no resting hand does.
        ///
        /// ⚠️ THE BROADCAST KEYS ARE NOT IN HERE ON PURPOSE. Pause, replay, mark and recall are
        /// the operator working the GALLERY, not the camera, and a director should not be thrown
        /// out for calling a replay of the shot it just covered.
        /// </summary>
        private bool ManualTakeover()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                float dx = Mathf.Abs(Input.GetAxisRaw("Mouse X"));
                float dy = Mathf.Abs(Input.GetAxisRaw("Mouse Y"));
                if (dx + dy > 0.01f) return true;
            }

            if (_move != null && _move.ReadValue<Vector2>().sqrMagnitude > 0.0001f) return true;
            if (_jump != null && _jump.IsPressed()) return true;
            if (_down != null && _down.IsPressed()) return true;

            var kb = Keyboard.current;
            if (kb != null && (kb.tabKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame
                               || kb.vKey.wasPressedThisFrame))
                return true;

            if (Mouse.current != null
                && Mathf.Abs(Mouse.current.scroll.ReadValue().y) > 0.01f) return true;

            return false;
        }

        /// <summary>
        /// The autopilot toggle, read every frame in both states.
        ///
        /// ⚠️ IT CANNOT LIVE IN `StepKeys`, which is one of the three steps the autopilot skips.
        /// A toggle that only works while the feature is off is a feature that cannot be turned
        /// off.
        /// </summary>
        private void StepAutopilotKey()
        {
            if (_director == null) _director = GetComponent<SpectatorDirector>();
            if (_director == null) _director = gameObject.AddComponent<SpectatorDirector>();

            if (_autopilotToggle != null && _autopilotToggle.WasPressedThisFrame())
            {
                _director.Toggle();
                UI.Hud.Instance?.ShowToast(
                    _director.Engaged ? "AUTOPILOT ON  ·  MOVE TO TAKE OVER" : "AUTOPILOT OFF",
                    1.2f);
            }
        }

        private InputAction _autopilotToggle;
        private InputAction _cycleTarget, _freeFly, _povToggle, _mark, _recall;
        private InputAction _pauseKey, _replayKey;

        /// <summary>
        /// True when an action exists and fired this frame.
        ///
        /// ⚠️ THE NULL CHECK IS NOT DEFENSIVE PADDING. `FindAction(..., false)` returns null
        /// rather than throwing when an asset predates an action, and a spectator camera that
        /// null-referenced every frame on somebody's older `TumbangPreso.inputactions` would take
        /// the whole broadcast down rather than losing one key.
        /// </summary>
        private static bool Fired(InputAction a) => a != null && a.WasPressedThisFrame();

        /// <summary>
        /// Direct broadcast cut to a seat's eye line. Function keys avoid colliding with the
        /// number-row slow-motion controls and give an operator four predictable camera cuts
        /// without tabbing through the roster on air.
        /// </summary>
        // -------------------------------------------------------------------
        // § THE HANDS OF WHOEVER IS BEING WATCHED
        //
        // ⚠️⚠️ A POV CUT SHOWED A FIRST-PERSON VIEW WITH NO FIRST PERSON IN IT. 🧑 2026-08-29:
        // *"f1-f4 for spectator show FPP arms of the ppl ur lookinga t in fpp"*. `CameraRig`
        // mounts `ViewmodelArms` on the LOCAL player's camera and drives them from that player's
        // `Carrier` and `CombatVerbs`; this camera is a different object, so pressing F1 parked a
        // lens at somebody's eyes and drew an empty street. The whole point of a POV cut is that
        // it is what THEY see.
        //
        // ⚠️ THE BODY IS HIDDEN AT THE SAME TIME, AND WITHOUT THAT IT LOOKS WORSE THAN NO ARMS.
        // `PovForwardOffset` puts the lens 0.34 m in front of the eyes so the chibi head is not
        // rendered from inside it, which means the unit's REAL arms are in frame. Adding a
        // viewmodel on top gives four arms. `CameraRig.ApplyFppSelfHide` solves the same problem
        // for the local player with the same mechanism: `ShadowsOnly`, so the body still casts
        // its shadow into the shot and only the camera stops seeing it.
        //
        // ⚠️ AND IT IS RESTORED WHENEVER POV ENDS, INCLUDING ON A TARGET SWITCH. The hide is per
        // renderer and per target; leaving it on a unit the operator has cut away from would take
        // a player out of every other camera in the room, including their own.
        // -------------------------------------------------------------------

        private CameraSystem.ViewmodelArms _povArms;
        private Transform _povViewmodel;
        private CharacterMotor _povHidden;
        private readonly List<Renderer> _povHiddenRenderers = new List<Renderer>();
        private readonly List<UnityEngine.Rendering.ShadowCastingMode> _povShadowModes =
            new List<UnityEngine.Rendering.ShadowCastingMode>();

        private void StepPovArms(float delta)
        {
            bool wanted = _pov && _follow != null;

            if (!wanted)
            {
                if (_povViewmodel != null) _povViewmodel.gameObject.SetActive(false);
                RestorePovBody();
                return;
            }

            EnsurePovArms();
            if (_povArms == null) return;

            if (_povHidden != _follow) HidePovBody(_follow);

            if (!_povViewmodel.gameObject.activeSelf) _povViewmodel.gameObject.SetActive(true);

            _povArms.MatchCharacter(_follow);

            // ⚠️ POLLED, NOT EVENT-DRIVEN, for the reason `CameraRig` gives on the same three
            // lines: what a unit holds changes DURING a round, and an event-driven copy shows the
            // wrong shoe until the next swap.
            var carrier = _follow.GetComponent<Carrier>();
            var held = carrier != null ? carrier.Held : null;

            _povArms.SetHolding(held != null);

            // ⚠️ THE SAME THREE SOURCES IN THE SAME ORDER AS THE LOCAL RIG: a throw wind-up needs
            // something in hand, so a TAYA would fall through every branch and the POV cut of the
            // one player everybody is watching would be the one with a dead arm.
            float charge = -1.0f;
            if (held != null && carrier != null) charge = carrier.ObservedChargePower;

            if (charge < 0.0f)
            {
                var verbs = _follow.GetComponent<CombatVerbs>();
                if (verbs != null) charge = verbs.ObservedLungeCharge;
            }

            _povArms.SetCharge(charge);

            if (held != null) _povArms.MatchSkin(held);

            _povArms.StepVisuals(delta);
        }

        private void EnsurePovArms()
        {
            if (_povArms != null) return;

            // ⚠️ THE SAME SEAT AND SCALE THE LOCAL RIG USES, read from it rather than retyped.
            // Two viewmodels that disagree about where a hand is would make a POV cut look like a
            // different game from the player's own screen, which is the one thing it must not.
            var go = new GameObject("~SpectatorViewmodelArms");
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * CameraSystem.CameraRig.ViewmodelScale;
            go.transform.localPosition = CameraSystem.CameraRig.ViewmodelSeat;
            go.transform.localRotation = Quaternion.identity;

            _povArms = go.AddComponent<CameraSystem.ViewmodelArms>();

            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);

            _povViewmodel = go.transform;
        }

        private void HidePovBody(CharacterMotor who)
        {
            RestorePovBody();
            if (who == null) return;

            _povHidden = who;

            foreach (var r in who.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                if (r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)
                    continue;

                _povShadowModes.Add(r.shadowCastingMode);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                _povHiddenRenderers.Add(r);
            }
        }

        private void RestorePovBody()
        {
            for (int i = 0; i < _povHiddenRenderers.Count; i++)
            {
                var r = _povHiddenRenderers[i];
                if (r == null) continue;
                r.shadowCastingMode = i < _povShadowModes.Count
                    ? _povShadowModes[i]
                    : UnityEngine.Rendering.ShadowCastingMode.On;
            }

            _povHiddenRenderers.Clear();
            _povShadowModes.Clear();
            _povHidden = null;
        }

        private void SelectPlayerPov(int slot)
        {
            CharacterMotor wanted = null;

            foreach (var unit in Spectatable)
            {
                if (unit == null || unit.PlayerSlot != slot) continue;
                wanted = unit;
                break;
            }

            if (wanted == null)
            {
                foreach (var unit in FindObjectsByType<CharacterMotor>(FindObjectsInactive.Exclude,
                                                                       FindObjectsSortMode.None))
                {
                    if (unit == null || unit.PlayerSlot != slot) continue;
                    wanted = unit;
                    break;
                }
            }

            if (wanted == null)
            {
                UI.Hud.Instance?.ShowToast($"P{slot + 1} POV IS NOT AVAILABLE", 1.0f);
                return;
            }

            _follow = wanted;
            _followIndex = -1;
            _pov = true;
            UI.Hud.Instance?.ShowToast($"POV CUT  ·  {wanted.DisplayName()}", 0.9f);
        }

        /// <summary>
        /// Tab / F / V, read straight off the keyboard device.
        ///
        /// ⚠️ RAW KEYS RATHER THAN INPUT ACTIONS, ON PURPOSE AND CARRIED OVER FROM THE .gd:
        /// adding three actions for a spectator-only convenience would mean three more rows
        /// in the rebind panel, three more conflict checks, and a settings migration — for a
        /// mode with no gameplay stake at all.
        ///
        /// The Godot original had to fight for Tab specifically (it is bound to
        /// `ui_focus_next` and the Viewport ate it during the GUI phase, so the follow cycle
        /// could not be reached at all and read as "not built"). Unity's Input System does
        /// not route keys through UI focus the same way, so this is a plain device read —
        /// but if a UI package is ever added that captures Tab, THAT is the bug this note is
        /// here to name.
        /// </summary>
        private void StepKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (Fired(_cycleTarget)) CycleFollow();

            if (Fired(_freeFly))
            {
                _follow = null;
                _followIndex = -1;
                _pov = false;
                // Leaving follow hands the camera back where it currently IS rather than
                // where it was when follow started, or the view would jump across the map.
                _targetPosition = transform.position;
            }

            // A no-op in free flight rather than an error: there is no POV of nobody, and a
            // key that silently arms a mode you cannot see is worse than one that waits.
            if (Fired(_povToggle) && _follow != null) _pov = !_pov;
        }

        private void ApplyRotation()
            => transform.rotation = Quaternion.Euler(_pitchDeg, _yawDeg, 0.0f);

        /// <summary>
        /// Cycles the follow target through every live unit, then back to free flight.
        /// Rebuilt on every press rather than cached: a unit can be spawned, destroyed or
        /// handed to an AI mid-match, and a stale list would follow a dangling reference.
        /// </summary>
        private void CycleFollow()
        {
            var units = new List<CharacterMotor>();
            foreach (var unit in Spectatable)
                if (unit != null) units.Add(unit);

            if (units.Count == 0)
            {
                // Fall back to a scan when nothing registered — the registry is populated at
                // spawn, and a probe scene that builds characters by hand does not go
                // through it.
                units.AddRange(FindObjectsByType<CharacterMotor>(FindObjectsInactive.Exclude));
            }

            if (units.Count == 0)
            {
                _follow = null;
                _followIndex = -1;
                return;
            }

            _followIndex += 1;
            if (_followIndex >= units.Count)
            {
                _follow = null;
                _followIndex = -1;
                _targetPosition = transform.position;
                return;
            }

            _follow = units[_followIndex];
        }

        /// <summary>The on-screen legend. Built by the match installer rather than here so
        /// the spectator stays a camera and nothing else — the same rule that keeps gameplay
        /// state out of it.</summary>
        /// <summary>
        /// The on-screen legend, built from the LIVE BINDINGS.
        ///
        /// ⚠⚠ IT WAS A STRING LITERAL NAMING TAB, V, F, R, P, B, N AND C, AND EVERY ONE OF
        /// THOSE IS REBINDABLE AS OF 2026-08-27. `docs/VISION.md` § 3 is explicit about what that
        /// costs: *"Key labels come from the live binding, never from a literal. A screen that
        /// teaches the wrong key is worse than one that teaches none."* The literal was correct
        /// on the day it was written and would have started lying the first time anybody opened
        /// the settings panel.
        ///
        /// ⚠️ F1 TO F4 AND THE THREE SPEED DIGITS STAY SPELLED OUT, because they are a
        /// positional and a numeric set rather than single actions. See `StepBroadcastKeys`.
        /// </summary>
        public static string ControlsText()
        {
            var asset = Resources.Load<InputActionAsset>("TumbangPreso");

            string Key(string action) => Settings.Rebinding.DisplayNameFor(asset, action).ToUpperInvariant();

            // ⚠️ `WASD fly` IS GONE ON PURPOSE. 🧑 2026-08-29, pointing at this overlay:
            // *"remove live netwrok here as well as WASD FLY"*. It is the one item on the line
            // that teaches nothing: every other entry names a key the player would not guess,
            // while WASD is the same walk the whole game is already played with, and the status
            // line above it already says `FREE FLIGHT` with the speed.
            return "SPECTATOR    F1-F4 player POV · " + Key("SpectatorCycleTarget")
                 + " follow · " + Key("SpectatorPov") + " POV/chase · " + Key("SpectatorFreeFly")
                 + " free · WHEEL speed/zoom · " + Key("SpectatorAutopilot") + " autopilot\n"
                 + "BROADCAST    " + Key("SpectatorReplay") + " replay · " + Key("SpectatorPause")
                 + " pause · 1/2/3 speed .25/.5/1x · " + Key("SpectatorMark") + " save cam · "
                 + Key("SpectatorRecall") + " recall · " + Key("SpectatorControls") + " controls";
        }

        /// <summary>
        /// ⚠️ §2.6 — WHAT THE CAMERA IS DOING RIGHT NOW, which the static legend cannot say.
        /// Polled once a frame by the HUD's spectator branch. Both numbers on it are ones a
        /// person framing a shot is actively changing and cannot otherwise see: turning the
        /// wheel produced no feedback at all, so "am I at 3 m/s or 40" was answered by flying
        /// and finding out — twice, because the wheel means two different things.
        ///
        /// Returns a plain string and reads nothing outside this component, so the HUD does
        /// not have to know what a follow target is.
        /// </summary>
        public string StatusText()
        {
            string broadcast = "";
            if (_replaying)
                broadcast = $"⏪ REPLAY {_replayReason}  ·  {_replayClock:0.0}s / {ReplaySeconds:0.0}s  ·  LIVE CONTINUES  |  ";
            else if (_broadcastPaused)
                broadcast = "⏸ TACTICAL PAUSE  |  ";
            else if (_selectedTimeScale < 0.99f)
                broadcast = $"SLOW-MO {_selectedTimeScale:0.##}x  |  ";

            // ⚠️⚠️ THERE IS NO `● LIVE NETWORK` PREFIX ANY MORE. 🧑 2026-08-29: *"remove live
            // netwrok here as well as WASD FLY"*, and *"remove live here too"* about the red bug
            // in the corner, which is the same word in the other place.
            //
            // ⚠️ THE THREE BRANCHES ABOVE STAY, AND THAT IS THE WHOLE DISTINCTION. Replay, pause
            // and slow-mo each say the frame is NOT the present moment, which a watcher cannot
            // work out by looking. Live was the else: it fired whenever none of those did, so it
            // only ever announced the ordinary case, and it announced it on a networked match
            // and stayed silent on a local one, which makes it a netcode readout wearing a
            // broadcast label.

            // ⚠️ THE AUTOPILOT ANNOUNCES ITSELF, AND IT HAS TO. A camera that moves on its own
            // with nothing on screen saying so is indistinguishable from a camera somebody else
            // is flying, which is the first thing an operator would report as a bug.
            if (AutopilotEngaged)
                return $"{broadcast}AUTOPILOT  ·  {_director.ShotName()}  ·  move to take over";

            if (_follow != null)
            {
                if (_pov) return $"{broadcast}POV  {FollowName()}  ·  through their eyes";
                return $"{broadcast}FOLLOWING  {FollowName()}  ·  {_followDistance:F1} m";
            }
            return $"{broadcast}FREE FLIGHT  ·  {_speed:F1} m/s";
        }

        /// <summary>Where this unit's eyes are. A Person stands; a lata and a tsinelas lie on
        /// the street. Read off IsPerson — the same property the camera directive itself is
        /// derived from — rather than off a per-class table, so a new roster entry needs no
        /// edit here.</summary>
        private float PovEyeHeight()
            => _follow == null || _follow.IsPerson ? PovEyeHeightPerson : PovEyeHeightProp;

        /// <summary>
        /// The followed unit's name, in the words the rest of the game uses for it rather
        /// than its object name — a legend that says `TeamAProp@3` is a debug print with a
        /// nicer font.
        ///
        /// ⚠️⚠️ THIS THREW ON EVERY CALL IN GODOT UNTIL 2026-08-01 AND NOTHING CAUGHT IT. It
        /// read `character.team`, a property the HARRYDAKS pivot renamed to `player_slot`, so
        /// every frame the legend drew it raised "Invalid access to property or key 'team'".
        /// It survived because the spectator's own probes never rendered the legend.
        ///
        /// ⚠️ AND THE STRING IT WAS BUILDING DESCRIBED A DELETED GAME. There are no teams
        /// (`Design.md` §1 — four players, one taya, role derived from the round number) and
        /// no playable props (§12), so "TEAM A · LATA" was three wrong words out of three.
        /// </summary>
        private string FollowName()
        {
            if (_follow == null) return "";
            return $"{_follow.DisplayName()} · {(_follow.IsDefender ? "TAYA" : "ATTACKER")}";
        }
    }

    /// <summary>
    /// Final local render pass for the spectator's replay buffer. It is attached at runtime
    /// after the colour grade, has no network component, and only copies pixels owned by this
    /// camera. Keeping it separate also means a gameplay camera can never record or display a
    /// replay by sharing a helper intended for the spectator.
    /// </summary>
    internal sealed class SpectatorReplayCapture : MonoBehaviour
    {
        public SpectatorCamera Owner { get; set; }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination);
            Owner?.CaptureReplayFrame(source);
        }
    }
}
