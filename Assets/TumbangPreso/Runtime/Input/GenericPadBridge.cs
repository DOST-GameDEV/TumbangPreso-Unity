using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace TumbangPreso.InputLayer
{
    /// <summary>
    /// Makes a controller Unity does not recognise behave like one it does, by guessing.
    ///
    /// ⚠️⚠️ THIS IS `docs/TODO.md` § 138.4 STEPS 2 AND 3, AND STEP 1 (`ControllerWatch`) IS WHAT
    /// PROVED IT WAS WORTH BUILDING. § 138.1 has the four-step walk of how a pad reaches a Unity
    /// game; the short version is that the Input System matches a USB HID device against a
    /// layout table, a match produces something deriving from `Gamepad`, and **a pad it does not
    /// know still gets a device, auto-generated from the HID descriptor, that derives from
    /// `Joystick` instead.** `Gamepad.current` never returns one of those.
    ///
    /// ⚠️⚠️ AND EVERY CONTROLLER PATH IN THIS GAME READS `Gamepad.current` OR A `&lt;Gamepad&gt;/`
    /// BINDING, so an unmatched pad is not degraded, it is DEAD: no verb fires, no prompt
    /// switches to pad glyphs, menu focus cannot move, and the settings GAMEPAD page lists
    /// controls that can never be bound. § 138.2's table is that sentence one row at a time.
    /// 🧑 2026-09-04: *"maybe add to todo that it can work for fake controllers and shit too?
    /// haha or other brands"*.
    ///
    /// § THE SHAPE OF THE FIX, AND WHY IT IS A TRANSLATOR RATHER THAN A LAYOUT
    ///
    /// ⚠️⚠️ § 138.4 STEP 2 ASKS FOR A REGISTERED LAYOUT AND THAT ROUTE IS SHUT, WHICH IS WORTH
    /// WRITING DOWN SO NOBODY SPENDS A DAY REDISCOVERING IT. Unity's own HID support hangs off
    /// `InputSystem.onFindLayoutForDevice`, and `InputManager` takes the **FIRST** callback that
    /// answers: `if (!string.IsNullOrEmpty(newLayout) &amp;&amp; !haveOverriddenLayoutName)`. HID's
    /// callback is registered during the Input System's own static initialisation, which is
    /// triggered by the first touch of `InputSystem` — **including the touch that would register
    /// ours** — so there is no order in which this game's callback runs first. Beating it with
    /// `RegisterLayoutMatcher` instead means out-scoring a matcher HID builds from the vendor id,
    /// the product id and the usage, per device, at runtime.
    ///
    /// **So the joystick is left exactly where it is and a `Gamepad` is created beside it**, and
    /// this class copies one into the other. That is worth more than it sounds, because it is the
    /// whole of step 3 for free: § 138.4 says *"once a pad is a `Gamepad`-derived device the
    /// existing GAMEPAD page already works, which is the argument for making the fallback a
    /// `Gamepad` rather than teaching the whole game about `Joystick`."* Nothing else in this
    /// repository learns a new concept. `LastInputDevice`, `ScreenFocus`, `Rumble`, every
    /// `<Gamepad>/` binding, the settings pages and `ControllerMapScreen` all just work.
    ///
    /// ⚠️ THE COST IS ONE FRAME OF LATENCY, and it is the right frame to spend. A state event
    /// queued from `onAfterUpdate` is processed by the next update, so a generic pad is one
    /// frame behind a recognised one. A pad that is one frame late is a pad; a pad that is dead
    /// is a broken game.
    ///
    /// § THE GUESS
    ///
    /// ⚠️⚠️ THE MAPPING IS A CONVENTION AND IT WILL BE WRONG FOR SOME PADS, WHICH IS THE
    /// DECISION § 138.4 ALREADY TOOK: *"it will be wrong for some pads and right for many, and a
    /// wrong mapping the player can SEE beats a dead pad they cannot."* The cure for a wrong
    /// guess is the CONTROLLER MAP screen, where every control is drawn and every one of them is
    /// rebindable, and that screen exists because of this class as much as the other way round.
    ///
    /// ⚠️ IT DOES NOT PARSE THE HID DESCRIPTOR. Unity has already done that: an unmatched HID
    /// joystick's controls are NAMED by `HID.HIDElementDescriptor.DetermineName`, which is
    /// `trigger` for button 1, `button2` upward after it, the generic-desktop axis names (`x`,
    /// `y`, `z`, `rx`, `ry`, `rz`) lower-cased, and `hat` for a hat switch. Reading those names
    /// off the built device is the same information with none of the parsing.
    /// </summary>
    public static class GenericPadBridge
    {
        /// <summary>The name every bridged pad is given, so a player can see what it is.</summary>
        public const string ProductName = "Generic Controller";

        /// <summary>One unmatched joystick and the gamepad standing in for it.</summary>
        private sealed class Bridge
        {
            public Joystick Source;
            public Gamepad Target;
            public GamepadState Last;
            public bool Primed;
        }

        private static readonly List<Bridge> Bridges = new List<Bridge>();

        /// <summary>True while at least one controller is being driven by the guess.</summary>
        public static bool Active => Bridges.Count > 0;

        /// <summary>
        /// ⚠️ IT CAN BE TURNED OFF, AND THE REASON IS A PAD THAT IS WORSE BRIDGED THAN ABSENT.
        /// A flight stick or a racing wheel is also a `Joystick` that matched no gamepad layout,
        /// and bridging one puts a throttle axis on the movement stick and holds a verb down for
        /// the whole match. There is no way to tell the two apart from the descriptor, so the
        /// answer is a switch the player can find, in the settings CONTROLS page beside the row
        /// that reports the device.
        /// </summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(EnabledKey, 1) != 0;
            set
            {
                PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
                PlayerPrefs.Save();
                Sync();
            }
        }

        private const string EnabledKey = "tumbangpreso.genericpad";

        /// <summary>
        /// ⚠️ `AfterSceneLoad` AND IT SWEEPS WHAT IS ALREADY THERE, for `ControllerWatch`'s
        /// reason in its own words: *"`onDeviceChange` only fires on a CHANGE, so a pad that was
        /// plugged in before the game started would never raise one and would be exactly the case
        /// nobody notices."*
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Sync();

            InputSystem.onDeviceChange += (device, change) => Sync();

            // ⚠️⚠️ `onAfterUpdate` RATHER THAN A `MonoBehaviour`, AND THAT IS NOT A STYLE CHOICE.
            // The one component that already ticks input every frame is `PlayerInputReader`, and
            // it only exists on a seat inside a match. A pad has to work in the MENUS, which is
            // where the player plugs it in and where they go looking when it does nothing.
            InputSystem.onAfterUpdate += Pump;
        }

        /// <summary>
        /// Brings the bridge list into step with what is attached. Idempotent and cheap.
        /// </summary>
        public static void Sync()
        {
            for (int i = Bridges.Count - 1; i >= 0; i--)
            {
                var bridge = Bridges[i];
                bool gone = bridge.Source == null || !bridge.Source.added || !Enabled;

                if (!gone) continue;

                if (bridge.Target != null && bridge.Target.added)
                    InputSystem.RemoveDevice(bridge.Target);

                Bridges.RemoveAt(i);
            }

            if (!Enabled) return;

            foreach (var device in InputSystem.devices)
            {
                if (!(device is Joystick joystick)) continue;

                // ⚠️ A `Gamepad` IS ALSO A `Joystick` ON SOME LAYOUTS, so the recognised pads
                // have to be excluded explicitly or every real controller gets a twin.
                if (device is Gamepad) continue;
                if (Find(joystick) != null) continue;

                var target = InputSystem.AddDevice<Gamepad>(ProductName);
                Bridges.Add(new Bridge { Source = joystick, Target = target });

                Debug.Log($"[Controller] '{joystick.displayName}' is being driven by the " +
                          "generic mapping. Open SETTINGS, CONTROLS, CONTROLLER MAP to see what " +
                          "each button does and to change any of it. See docs/TODO.md § 138.");
            }
        }

        private static Bridge Find(Joystick source)
        {
            foreach (var bridge in Bridges)
                if (ReferenceEquals(bridge.Source, source)) return bridge;

            return null;
        }

        /// <summary>
        /// ⚠️⚠️ A STATE EVENT IS QUEUED ONLY WHEN SOMETHING CHANGED, AND THAT IS WHAT KEEPS
        /// `Gamepad.current` HONEST. `Gamepad.current` is whichever pad last received an event,
        /// so a bridge that queued sixty identical events a second would steal `current` from a
        /// real controller plugged in beside it and hold it for ever. It would also flip
        /// `LastInputDevice` to `Gamepad` on a machine where nobody is touching the thing, which
        /// is the exact fault that class's own `IsActuated` check exists to prevent.
        /// </summary>
        private static void Pump()
        {
            for (int i = 0; i < Bridges.Count; i++)
            {
                var bridge = Bridges[i];
                if (bridge.Source == null || bridge.Target == null) continue;

                var state = Read(bridge.Source);

                if (bridge.Primed && Same(state, bridge.Last)) continue;

                bridge.Last = state;
                bridge.Primed = true;
                InputSystem.QueueStateEvent(bridge.Target, state);
            }
        }

        private static bool Same(GamepadState a, GamepadState b)
            => a.buttons == b.buttons
               && a.leftStick == b.leftStick && a.rightStick == b.rightStick
               && Mathf.Approximately(a.leftTrigger, b.leftTrigger)
               && Mathf.Approximately(a.rightTrigger, b.rightTrigger);

        /// <summary>
        /// The guess, in one place.
        ///
        /// ⚠️⚠️ THE BUTTON ORDER IS THE XInput-STYLE DirectInput ORDER: A, B, X, Y, then the two
        /// bumpers, the two triggers, SELECT, START, and the two stick clicks. That is what the
        /// large majority of no-name PC pads report, because they are copies of a pad that
        /// reports it. **It is a guess and the PlayStation-style families disagree with it**,
        /// putting square first, so on one of those the face buttons come out rotated. That is
        /// the failure this whole class accepts on purpose: § 138.4's *"a wrong mapping the
        /// player can SEE beats a dead pad they cannot"*, and CONTROLLER MAP is where they see
        /// it and fix it.
        ///
        /// ⚠️ THE RIGHT STICK IS `z`/`rz` BEFORE `rx`/`ry`, WHICH IS THE OTHER COMMON SPLIT.
        /// Pads that report the right stick as Z and Rz outnumber the Rx/Ry ones, and asking for
        /// both in order costs nothing: a pad that has neither simply has no right stick, and
        /// `Vector2.zero` is the honest answer for a control that is not there.
        /// </summary>
        private static GamepadState Read(Joystick source)
        {
            var state = new GamepadState
            {
                leftStick = source.stick != null
                    ? source.stick.ReadValue()
                    : new Vector2(Axis(source, "x"), Axis(source, "y")),
            };

            float rx = Axis(source, "z");
            float ry = Axis(source, "rz");

            if (rx == 0.0f && ry == 0.0f)
            {
                rx = Axis(source, "rx");
                ry = Axis(source, "ry");
            }

            state.rightStick = new Vector2(rx, ry);

            // ⚠️ BUTTON 1 IS CALLED `trigger`, NOT `button1`, AND THAT IS UNITY'S NAMING RATHER
            // THAN A PAD'S. `HID.HIDElementDescriptor.DetermineName` returns "trigger" for usage
            // 1 on the button page and `button{usage}` for everything after it, so a table that
            // started at `button1` would silently lose the pad's A button.
            Set(ref state, GamepadButton.South, Pressed(source, "trigger"));
            Set(ref state, GamepadButton.East, Pressed(source, "button2"));
            Set(ref state, GamepadButton.West, Pressed(source, "button3"));
            Set(ref state, GamepadButton.North, Pressed(source, "button4"));
            Set(ref state, GamepadButton.LeftShoulder, Pressed(source, "button5"));
            Set(ref state, GamepadButton.RightShoulder, Pressed(source, "button6"));
            Set(ref state, GamepadButton.Select, Pressed(source, "button9"));
            Set(ref state, GamepadButton.Start, Pressed(source, "button10"));
            Set(ref state, GamepadButton.LeftStick, Pressed(source, "button11"));
            Set(ref state, GamepadButton.RightStick, Pressed(source, "button12"));

            // ⚠️⚠️ THE TRIGGERS ARE DIGITAL HERE AND THAT COSTS THE THROW ITS CHARGE ON PAPER,
            // BUT NOT IN PRACTICE. `SpecialAbility` is the right trigger and `Carrier` charges
            // while it is HELD, which a 0-or-1 button still does perfectly: the charge is a timer
            // on the hold, not a function of how far the trigger travelled. Nothing in this game
            // reads an analogue trigger value. A generic pad that reports its triggers as axes
            // instead is handled by the two `Axis` reads below, so the analogue ones stay
            // analogue and the digital ones are honest about being buttons.
            state.leftTrigger = Mathf.Max(Pressed(source, "button7") ? 1.0f : 0.0f,
                                          Mathf.Clamp01(Axis(source, "slider")));
            state.rightTrigger = Mathf.Max(Pressed(source, "button8") ? 1.0f : 0.0f,
                                           Mathf.Clamp01(Axis(source, "dial")));

            var hat = source.hatswitch;

            if (hat != null)
            {
                var value = hat.ReadValue();

                Set(ref state, GamepadButton.DpadUp, value.y > 0.5f);
                Set(ref state, GamepadButton.DpadDown, value.y < -0.5f);
                Set(ref state, GamepadButton.DpadLeft, value.x < -0.5f);
                Set(ref state, GamepadButton.DpadRight, value.x > 0.5f);
            }

            return state;
        }

        private static void Set(ref GamepadState state, GamepadButton button, bool pressed)
        {
            if (pressed) state.buttons |= (uint)1 << (int)button;
        }

        private static bool Pressed(Joystick source, string control)
            => source.TryGetChildControl<ButtonControl>(control)?.isPressed ?? false;

        private static float Axis(Joystick source, string control)
            => source.TryGetChildControl<AxisControl>(control)?.ReadValue() ?? 0.0f;
    }
}
