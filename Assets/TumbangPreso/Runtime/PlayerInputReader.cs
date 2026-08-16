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
    /// ⚠️ E IS CONTEXTUAL AND THAT IS RESOLVED DOWNSTREAM, NOT HERE. E is bound to BOTH
    /// `Grab` and `Lunge`, and left-click to both `Grab` and `SpecialAbility`, exactly as the
    /// Godot input map has it. This class reports both as held and lets the carrier take
    /// first refusal: tap with a slipper at your feet is a pickup, tap with nothing grabbable
    /// is a shove, hold as the taya in the lata's ring is the reset channel, and only a press
    /// nothing consumed reaches the lunge. Resolving it here would need this class to know
    /// the world state, which is how one keybind becomes three.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _actions;
        [SerializeField] private CharacterMotor _motor;
        [SerializeField] private Camera _aimCamera;

        private InputAction _move, _sprint, _jump, _special, _grab, _lunge, _emote;

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

            map.Enable();
        }

        private void Update()
        {
            if (_motor == null) return;

            var intent = _motor.Intent;

            intent.Move = _move.ReadValue<Vector2>();
            intent.Set(Verb.Sprint, _sprint.IsPressed());
            intent.Set(Verb.Jump, _jump.IsPressed());
            intent.Set(Verb.SpecialAbility, _special.IsPressed());
            intent.Set(Verb.Grab, _grab.IsPressed());
            intent.Set(Verb.Lunge, _lunge.IsPressed());
            intent.Set(Verb.EmoteWheel, _emote.IsPressed());

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
        /// Where this player is aiming, as a world point on the ground plane.
        ///
        /// ⚠️ THE THROW LEAVES FROM THE SIGHT LINE, NOT THE HAND, so this point is what the
        /// trajectory is solved against. Measured in the original: launching from the hand
        /// instead sagged the flight 0.38 to 0.43 m below the line the player was aiming
        /// along and peaked within 0.2 m of them, which drops the slipper out of the bottom
        /// of the screen the instant it is released.
        /// </summary>
        private Vector3 ReadAimPoint()
        {
            if (_aimCamera == null || Mouse.current == null)
                return transform.position + transform.forward * 10.0f;

            Ray ray = _aimCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            var ground = new Plane(Vector3.up, new Vector3(0.0f, transform.position.y, 0.0f));
            if (ground.Raycast(ray, out float enter)) return ray.GetPoint(enter);

            return transform.position + transform.forward * 10.0f;
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
