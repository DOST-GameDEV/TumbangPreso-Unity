using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null) _camera = gameObject.AddComponent<Camera>();
            _camera.fieldOfView = SpectatorFov;
            _camera.farClipPlane = SpectatorFar;

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
            map.Enable();
        }

        private void OnEnable() { if (_camera != null) _camera.enabled = true; }

        private void Update()
        {
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

            StepLook();
            StepWheel();
            StepKeys();

            // ⚠️ Update, NOT FixedUpdate. There is no physics here — nothing to step, nothing
            // to collide, nothing another body has to agree with — and a camera that moves on
            // the render frame is smoother than one moving on the physics tick.
            float delta = Time.deltaTime;

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
                    return;
                }

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

            if (kb.tabKey.wasPressedThisFrame) CycleFollow();

            if (kb.fKey.wasPressedThisFrame)
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
            if (kb.vKey.wasPressedThisFrame && _follow != null) _pov = !_pov;
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
        public static string ControlsText()
            => "SPECTATOR    WASD fly · SPACE up · CTRL down · SHIFT boost · TAB follow · V POV · F free · WHEEL speed, or follow distance while following";

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
            if (_follow != null)
            {
                if (_pov) return $"POV  {FollowName()}  ·  through their eyes";
                return $"FOLLOWING  {FollowName()}  ·  {_followDistance:F1} m";
            }
            return $"FREE FLIGHT  ·  {_speed:F1} m/s";
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
}
