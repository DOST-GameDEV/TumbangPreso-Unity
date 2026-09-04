using UnityEngine;
using UnityEngine.InputSystem;

namespace TumbangPreso
{
    /// <summary>
    /// Turns hardware into <see cref="InputIntent"/> for a locally-controlled unit.
    ///
    /// ⚠️⚠️ THIS IS THE ONLY PLACE IN THE GAME THAT READS HARDWARE. Everything downstream
    /// asks <see cref="InputIntent"/>, which is also what the AI writes, so one physics step
    /// serves a human and a bot and there is no second path where one can do something the
    /// other cannot. Adding a `Keyboard.current` read anywhere else quietly reintroduces the
    /// divergence the whole indirection exists to prevent.
    ///
    /// ⚠⚠ ONE CONTROL, ONE ACTION, SINCE 2026-08-23. The map used to bind E to `Grab`,
    /// `Lunge` AND `Skill1`, left click to `Grab` AND `SpecialAbility`, and Q to
    /// `SpecialAbility` AND `Skill2`, following the Godot map and then stacking the hero keys
    /// on top of it. Whichever consumer ran first won the press, so throw did not feel like it
    /// was on left click even though it was bound there, and a hero's first skill came out of
    /// the pickup key. The full table is in `Settings.Rebinding` and a test asserts no control
    /// is shared. Hero powers now use the adjacent Q, E and F cluster, while the contextual
    /// pickup key uses X so the HUD prompts and shipped controls agree without a collision.
    ///
    /// ⚠️ GRAB IS STILL CONTEXTUAL, AND THAT IS RESOLVED DOWNSTREAM, NOT HERE. One key, one
    /// action, but that action does several jobs depending on the world: tap with a slipper at
    /// your feet is a pickup, tap with nothing grabbable is a shove, hold as the taya in the
    /// lata's ring is the reset channel. The carrier takes first refusal and only a press it
    /// did not consume reaches the shove. Resolving it here would need this class to know the
    /// world state, which is how one keybind becomes three.
    ///
    /// ⚠️ THE TWO ROLES SHARE `SpecialAbility` DELIBERATELY, and that is a role split, not
    /// a collision. Left click charges the throw for an attacker; `can_throw()` refuses a
    /// defender outright, so for the taya the same button is the punch. Nobody loses anything,
    /// and no frame has both branches live.
    /// </summary>
    // ⚠️⚠️ IT RUNS AFTER `AIController`, AND THE ONE BODY THAT CARRIES BOTH IS WHY THAT IS
    // WRITTEN DOWN. `GhostPetCompanion.BeginPossession` puts a temporary AI on Nemu while the
    // player drives Kuro; both components write `CharacterMotor.Intent`, and with neither
    // declaring an order Unity chose one arbitrarily. The human is the one whose press must
    // survive, so the human writes last. See `AIController.AbilitiesEnabled` for the report.
    [DefaultExecutionOrder(-120)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _actions;
        [SerializeField] private CharacterMotor _motor;
        [SerializeField] private Camera _aimCamera;

        private InputAction _move, _sprint, _jump, _special, _grab, _lunge, _emote, _skill1, _skill2, _ultimate;

        /// <summary>
        /// The pektus curve, left and right.
        ///
        /// ⚠️⚠️ THEY WERE `Keyboard.current.leftArrowKey` READ INLINE, WHICH BREAKS THE ONE
        /// RULE THIS CLASS EXISTS FOR. `CLAUDE.md` § 4: *"One control, one action, in the input
        /// map."* A hardware read that never passes through an `InputAction` cannot be rebound,
        /// cannot be shown in the settings panel, and cannot be printed by `Hud.KeyLabel`, so the
        /// tutorial's pektus lesson had to name the arrow keys in a hard-coded string while every
        /// other lesson drew the live binding. 🧑, 2026-08-26: *"im not sure as well if pektus
        /// controls are in settings"*. They were not.
        ///
        /// ⚠️ THE MOUSE WHEEL IS STILL READ DIRECTLY AND THAT IS NOT THE SAME FAULT. A scroll
        /// axis is not a button and there is nothing to rebind it to; it is the shortcut, and
        /// these two are the binding the panel teaches.
        ///
        /// ⚠️ THE DEFAULTS ARE Z AND C SINCE 2026-08-27, NOT THE ARROWS. 🧑: *"its so hard to
        /// touch the arrow keys and some keyboards dont have it"*, and the curve has to be held
        /// while the left hand is on WASD and the throw is charging. `Settings.Rebinding`'s class
        /// note carries the reasoning and the one legal cross-context collision.
        /// </summary>
        private InputAction _curveLeft, _curveRight;

        /// <summary>
        /// The pad's right stick. There is no keyboard binding on it: a mouse reports a DELTA and
        /// a stick reports a POSITION, so the two are combined in <see cref="ReadLookDelta"/>
        /// rather than bound to one action.
        /// </summary>
        private InputAction _look;

        /// <summary>
        /// Full stick deflection, expressed in the units a mouse reports per second.
        ///
        /// ⚠️ THE ARITHMETIC, so the next person does not re-guess it. `CameraRig.StepLook` turns
        /// a raw delta into degrees as `raw * BaseSensitivity(0.15) * MouseSensitivity * 10`, so at
        /// sensitivity 1.0 one raw unit is 1.5 degrees. 150 units per second is therefore
        /// **225 degrees per second** at full deflection, which is a three-quarter turn in one
        /// second: fast enough to spin and face a taya coming from behind, slow enough that the
        /// stick is not the reason a throw misses. It rides the player's own sensitivity slider
        /// because it is expressed in the same units the slider already scales.
        /// </summary>
        private const float StickLookUnitsPerSecond = 150.0f;

        /// <summary>
        /// ⚠️ THE STICK IS SQUARED BEFORE IT IS SCALED, AND A LINEAR STICK IS WHY AIMING ON A PAD
        /// FEELS BAD. Half deflection becomes a quarter speed, so the first half of the stick's
        /// travel buys fine aim at a distant lata and the last quarter buys the fast turn. The
        /// sign is restored afterwards; squaring a negative would turn every left into a right.
        /// </summary>
        private const float StickDeadzone = 0.16f;

        private void Awake()
        {
            if (_motor == null) _motor = GetComponent<CharacterMotor>();
            if (_aimCamera == null) _aimCamera = Camera.main;

            // ⚠️⚠️ IT LOADS ITS OWN ASSET, AND WITHOUT THIS THE GAME IS UNPLAYABLE. Nothing
            // assigns the serialised field: `MatchInstaller.BuildSeat` reaches the human seat
            // through `AddComponent<PlayerInputReader>()`, which cannot carry an inspector
            // reference, so `_actions` was null on every unit in every build. The component
            // disabled itself with one line in the log and the match then ran perfectly with
            // three bots and a player who could not move. Every symptom of that points at the
            // motor, the camera or the arena rather than at an unassigned field.
            //
            // ⚠️ THE SAME PATH THE SETTINGS PANEL USES, `Resources/TumbangPreso`, so a rebind
            // made in the menu is on the asset this reads. Two copies would mean the keys the
            // player set and the keys the game listens to are different objects.
            if (_actions == null) _actions = Resources.Load<InputActionAsset>("TumbangPreso");

            if (_actions == null)
            {
                Debug.LogError("[Input] no InputActionAsset at Resources/TumbangPreso; " +
                               "this unit is unplayable.");
                enabled = false;
                return;
            }

            // ⚠️ AND THE SAVED REBINDS ARE APPLIED BEFORE THE MAP IS ENABLED. A player who
            // rebound jump in the menu and then found it back on Space would reasonably
            // conclude the setting does nothing.
            Settings.Rebinding.Load(_actions);

            var map = _actions.FindActionMap("Player", throwIfNotFound: true);
            _move = map.FindAction("Move", true);
            _sprint = map.FindAction("Sprint", true);
            _jump = map.FindAction("Jump", true);
            _special = map.FindAction("SpecialAbility", true);
            _grab = map.FindAction("Grab", true);
            _lunge = map.FindAction("Lunge", true);
            _emote = map.FindAction("EmoteWheel", true);
            _skill1 = map.FindAction("Skill1", false);
            _skill2 = map.FindAction("Skill2", false);
            _ultimate = map.FindAction("Ultimate", false);
            _curveLeft = map.FindAction("CurveLeft", false);
            _curveRight = map.FindAction("CurveRight", false);

            // ⚠️ OPTIONAL, LIKE THE HERO ACTIONS ABOVE IT. A project whose asset predates the
            // gamepad pass has no Look action, and a null here must degrade to "mouse only"
            // rather than throwing on every seat in every match.
            _look = map.FindAction("Look", false);

            map.Enable();
        }

        private float _currentPektusSpin;

        private void Update()
        {
            // ⚠️ SAMPLED BEFORE EVERY EARLY RETURN BELOW, AND THAT IS DELIBERATE. A player typing
            // in chat or driving Kuro is still holding a device, and the prompts on screen still
            // have to name the right control for it. Putting this after the chat guard would
            // freeze every glyph for as long as a message was being written.
            // ⚠️ `LastInputDevice` SAMPLES ITSELF NOW, from `InputSystem.onAfterUpdate`, because
            // this component only exists on a seat inside a match and the front end needs the
            // answer too. The call that used to be here is gone rather than kept alongside:
            // sampling twice in a frame is harmless but it hides which one is the real one.

            if (_motor == null) return;

            var intent = _motor.Intent;

            // ⚠️⚠️ A CHAT FIELD WITH THE KEYBOARD MUST NOT ALSO DRIVE THE BODY, AND "just stop
            // reading" IS THE WRONG FIX. `InputIntent.Parked`'s own note says why: a verb held
            // across the boundary stays held forever, so a player who was sprinting when they hit
            // ENTER would keep sprinting into a wall for the whole message. `Clear` releases
            // everything and `CommitFrame` publishes that release, so the frame chat opens is the
            // frame every key comes up.
            //
            // ⚠️ IT DOES NOT TOUCH `Parked`. That field already has writers in `PausePanel`,
            // `GuidedTraining`, `CharacterMotor` and `DebugPlayerSwitcher`, and adding a fifth
            // that clears it on a different schedule is exactly `docs/TODO.md` § 42.1: two writers
            // on one `InputIntent` in an undefined order. Closing chat would have un-parked a
            // paused game.
            //
            // ⚠️ AND CHAT IS THE THIRD INPUT CONTEXT, per `CLAUDE.md` § 4. A player who is typing
            // has no verbs and a player who has verbs is not typing, so the two sets can never
            // both fire, which is the same narrowing `Rebinding.SpectatorContext` records.
            if (UI.LobbyChat.AnyTyping)
            {
                intent.Clear();
                intent.CommitFrame();
                return;
            }

            var visual = _motor.GetComponent<Visual.CharacterVisual>();
            if (visual != null && visual.Companion != null && visual.Companion.IsPossessed)
            {
                // Human controls Kuro the companion pet.
                //
                // ⚠️ THE THUMB STICK AND THE LOOK DELTA REACH KURO TOO. This branch returns
                // before the ordinary read, so every device added below has to be added here as
                // well or possession is the one place in the game that is keyboard-only.
                // `StepCompanionLook` reads the same `Intent.LookAxis` the main rig does.
                var petMove = _move.ReadValue<Vector2>();
                if (InputLayer.TouchInput.Active && InputLayer.TouchInput.Move.sqrMagnitude > 0.0001f)
                    petMove = InputLayer.TouchInput.Move;

                visual.Companion.SetPlayerInput(petMove);
                intent.LookDelta = ReadLookDelta();

                // Allow skill2 recast to teleport and end possession
                if (_skill2 != null)
                    intent.Set(Verb.Skill2, _skill2.IsPressed() || InputLayer.TouchInput.Pressed(Verb.Skill2));

                return;
            }

            // ⚠️⚠️ THE THUMB LAYER IS OR-ED IN HERE, WHICH IS THE ONE PLACE IT MAY BE. A touch
            // button is a third device beside the keyboard and the pad, and every argument in
            // this class's note applies to it unchanged: it arrives as HELD state, the edges are
            // derived downstream, and nothing past this line can tell which device produced the
            // press. That is what lets `docs/TODO.md` § 124.1's five hold-to-aim powers work on a
            // finger with no ability-side code at all.
            //
            // ⚠️ OR, NOT REPLACE. An Android device can have a physical pad paired, and a desktop
            // player can be on a touchscreen laptop. Whichever is pressed wins; neither switches
            // the other off. `TouchInput.Active` is false on a build with no layer drawn, so this
            // costs a desktop seat one bool per verb per frame.
            var move = _move.ReadValue<Vector2>();
            if (InputLayer.TouchInput.Active && InputLayer.TouchInput.Move.sqrMagnitude > 0.0001f)
                move = InputLayer.TouchInput.Move;

            intent.Move = move;
            intent.Set(Verb.Sprint, _sprint.IsPressed() || InputLayer.TouchInput.Pressed(Verb.Sprint));
            intent.Set(Verb.Jump, _jump.IsPressed() || InputLayer.TouchInput.Pressed(Verb.Jump));
            intent.Set(Verb.SpecialAbility, _special.IsPressed() || InputLayer.TouchInput.Pressed(Verb.SpecialAbility));
            intent.Set(Verb.Grab, _grab.IsPressed() || InputLayer.TouchInput.Pressed(Verb.Grab));
            intent.Set(Verb.Lunge, _lunge.IsPressed() || InputLayer.TouchInput.Pressed(Verb.Lunge));
            intent.Set(Verb.EmoteWheel, _emote.IsPressed() || InputLayer.TouchInput.Pressed(Verb.EmoteWheel));
            if (_skill1 != null) intent.Set(Verb.Skill1, _skill1.IsPressed() || InputLayer.TouchInput.Pressed(Verb.Skill1));
            if (_skill2 != null) intent.Set(Verb.Skill2, _skill2.IsPressed() || InputLayer.TouchInput.Pressed(Verb.Skill2));
            if (_ultimate != null) intent.Set(Verb.Ultimate, _ultimate.IsPressed() || InputLayer.TouchInput.Pressed(Verb.Ultimate));

            intent.LookDelta = ReadLookDelta();

            // Pektus (Curve Spin) control: Independent of WASD movement!
            // Controlled via Mouse Wheel Up/Down (or Left/Right arrow keys) while charging throw.
            if (_special.IsPressed())
            {
                if (Mouse.current != null)
                {
                    float scrollY = Mouse.current.scroll.ReadValue().y;
                    if (scrollY > 0.1f) _currentPektusSpin = Mathf.Clamp(_currentPektusSpin + 0.35f, -1.0f, 1.0f);
                    else if (scrollY < -0.1f) _currentPektusSpin = Mathf.Clamp(_currentPektusSpin - 0.35f, -1.0f, 1.0f);
                }

                // ⚠️ THROUGH THE MAP, SO THE PANEL CAN REBIND THEM. See `_curveLeft`.
                if (_curveLeft != null && _curveLeft.IsPressed())
                    _currentPektusSpin = Mathf.Clamp(_currentPektusSpin - Time.deltaTime * 2.5f, -1.0f, 1.0f);

                if (_curveRight != null && _curveRight.IsPressed())
                    _currentPektusSpin = Mathf.Clamp(_currentPektusSpin + Time.deltaTime * 2.5f, -1.0f, 1.0f);
            }
            else
            {
                _currentPektusSpin = 0.0f;
            }

            intent.SpinInput = _currentPektusSpin;
            intent.AimPoint = ReadAimPoint();

            // ⚠️⚠️ THE COMMIT DOES NOT HAPPEN HERE, AND DOING IT HERE IS WHY JUMP AND GRAB DID
            // NOTHING. The edge queries are a diff against the last committed snapshot, and this
            // ran at the end of every Update. Unity's order within a frame is FixedUpdate, then
            // Update, then LateUpdate — so by the time the next FixedUpdate asked
            // `JustPressed(Jump)`, this had already copied the held set over the previous one and
            // the answer was false. Every verb resolved in the physics step therefore read as
            // never pressed, on a human AND on a bot: `CharacterMotor.ApplyGravity` reads
            // `JustPressed(Verb.Jump)` and `CombatVerbs` reads the shove and the lunge the same
            // way. Reported as *"some controls also dont exist in unity like jump"* and *"u cant
            // grab shit"*, which are one fault, not two.
            //
            // ⚠️ GODOT DOES NOT HAVE THIS PROBLEM AND THAT IS WHY IT WAS MISSED IN THE PORT. A
            // human there reads `Input.is_action_just_pressed` straight from the engine, which
            // stays true for the whole frame including `_physics_process`; only an AI-driven unit
            // keeps a prev table (`_ai_intent_prev`). Collapsing both onto one table is right and
            // is what keeps a bot pressing the same buttons a human does, but it means the
            // snapshot has to be taken by the CONSUMER, not by each producer.
            //
            // `CharacterMotor.FixedUpdate` takes it, at the end of the authoritative step. See
            // the note there.
        }

        /// <summary>
        /// This frame's look delta, from whichever of the three devices moved.
        ///
        /// ⚠️⚠️ THE CAMERA USED TO COMPUTE THIS ITSELF, IN THREE PLACES, AND THAT BROKE THIS
        /// CLASS'S ONE RULE. `CameraRig.StepLook`, `StepCompanionLook` and `StepEmoteLook` each
        /// called `Input.GetAxisRaw("Mouse X")` directly. The note at the top of this file says
        /// hardware is read HERE and nowhere else, and the cost of the exception was exactly what
        /// it warns about: a pad stick and a phone drag had no way in, and adding them would have
        /// meant a fourth and a fifth hardware read inside the camera, each with its own copy of
        /// the deadzone, the curve and the invert-Y check. The rig reads `Intent.LookAxis` now and
        /// this is the only producer.
        ///
        /// ⚠️ THE THREE SOURCES ADD RATHER THAN OVERRIDE. A pad can be paired to a phone and a
        /// touchscreen laptop still has a mouse; whichever moved contributes, and the two that did
        /// not contribute zero. There is no "current device" to get wrong.
        ///
        /// ⚠️ INVERT-Y IS NOT APPLIED HERE. `CameraRig` owns it, applies it once, and applies it
        /// to all three sources for free by virtue of them arriving as one number.
        /// </summary>
        private Vector2 ReadLookDelta()
        {
            // The mouse, in the raw units every sensitivity number in this game is written
            // against. Legacy axes are live because `activeInputHandler` is Both.
            var delta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

            if (_look != null)
            {
                Vector2 stick = _look.ReadValue<Vector2>();

                // ⚠️ THE DEADZONE IS ON THE VECTOR'S LENGTH, NOT PER AXIS. A per-axis deadzone
                // makes a stick pushed diagonally snap to whichever axis cleared first, which is
                // the classic "the camera will only move in eight directions" complaint.
                if (stick.sqrMagnitude > StickDeadzone * StickDeadzone)
                {
                    // Squared response, sign preserved. See `StickDeadzone`'s note.
                    stick = new Vector2(stick.x * Mathf.Abs(stick.x), stick.y * Mathf.Abs(stick.y));
                    delta += stick * (StickLookUnitsPerSecond * Time.deltaTime);
                }
            }

            // ⚠️ THE DRAG IS CONSUMED, NOT SAMPLED. `TouchInput.LookDelta` accumulates what the
            // finger moved since the last read; leaving it standing would apply the same drag on
            // every frame after the finger stopped, which is a camera that never settles.
            if (InputLayer.TouchInput.Active)
            {
                delta += InputLayer.TouchInput.LookDelta;
                InputLayer.TouchInput.LookDelta = Vector2.zero;
            }

            return delta;
        }

        /// <summary>
        /// Where this player is aiming, in world space.
        ///
        /// ⚠️ THE THROW LEAVES FROM THE SIGHT LINE, NOT THE HAND, so this point is what the
        /// trajectory is solved against. Measured in the original: launching from the hand
        /// instead sagged the flight 0.38 to 0.43 m below the line the player was aiming
        /// along and peaked within 0.2 m of them, which drops the slipper out of the bottom
        /// of the screen the instant it is released.
        ///
        /// ⚠️⚠️ IT ASKS THE RIG, AND COMPUTING ITS OWN ANSWER HERE WAS *"throw also randomly
        /// breaks"* AND MOST OF *"THIS charge outline is so ugly, it doesnt behave naturally"*.
        /// This method used to intersect the mouse ray with a HORIZONTAL PLANE AT THE PLAYER'S
        /// OWN FEET, which is the TPP form — `CameraRig.AimPoint` uses exactly that expression,
        /// but only in its TPP branch, and this game is FPP for every Person (§3a).
        ///
        /// Against a plane you are standing ON, a sight line at or above the horizon is
        /// PARALLEL TO IT OR POINTING AWAY. So across one small movement of the mouse the aim
        /// point ran out to hundreds of metres, then failed the intersection entirely and
        /// snapped back to a hard-coded 10 m in front. `Slipper.SolveArc` is solved against that
        /// distance: at 400 m the discriminant goes negative and it bails to a flat throw along
        /// the line, at 10 m it produces a sane arc — so the same key, held the same way, threw
        /// completely differently depending on whether the crosshair was a few pixels above or
        /// below the horizon. The preview reads the same function, which is why the outline
        /// jumped about with it.
        ///
        /// The rig already branches correctly and its FPP half CASTS along the sight line and
        /// falls back to a fixed `AimRayLength` down that same line, so the answer degrades
        /// along the direction the player is actually pointing instead of collapsing onto the
        /// floor. Asking it here keeps ONE implementation: `Carrier.AimPoint` already prefers
        /// `Intent.AimPoint` when it is set, and since this class sets it every frame, the rig's
        /// version was unreachable for a human. Two answers to one question, and the wrong one
        /// won every time.
        /// </summary>
        private Vector3 ReadAimPoint()
        {
            // ⚠️ RE-RESOLVED WHILE IT IS NULL, NOT ONCE IN Awake, FOR THE REASON
            // `CharacterMotor.ResolveRig` GIVES. `MatchInstaller` adds this component at
            // BuildSeat and does not build the camera until sixty lines later, and
            // `AddComponent` runs `Awake` synchronously — so `Camera.main` is genuinely null at
            // the moment this seat wakes up. Caching that would leave the human aiming down the
            // fallback line for the whole match.
            if (_aimCamera == null) _aimCamera = Camera.main;

            var rig = _aimCamera != null
                ? _aimCamera.GetComponent<CameraSystem.CameraRig>()
                : null;

            if (rig != null && rig.IsFollowing(_motor)) return rig.AimPoint();

            // No rig on this seat yet. `MatchInstaller` builds the rig after the seats, so this
            // is the ordinary answer for the first frame or two rather than an error.
            return transform.position + transform.forward * CameraSystem.CameraRig.AimRayLength;
        }

        private void OnDisable()
        {
            // ⚠️ RELEASE EVERYTHING ON THE WAY OUT. A verb held across a disable stays held
            // in the intent table forever, and the player walks back in already sprinting.
            _motor?.Intent.Clear();
            _motor?.Intent.CommitFrame();
        }
    }
}
