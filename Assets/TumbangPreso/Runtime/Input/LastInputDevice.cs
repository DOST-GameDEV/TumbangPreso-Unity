using UnityEngine;
using UnityEngine.InputSystem;

namespace TumbangPreso.InputLayer
{
    /// <summary>Which kind of device the player last actually used.</summary>
    public enum InputDeviceKind
    {
        KeyboardMouse,
        Gamepad,
        Touch,
    }

    /// <summary>
    /// Tracks which device the player last touched, so a prompt can name the right control.
    ///
    /// ⚠️⚠️ DRIVEN BY THE LAST DEVICE USED, NOT BY A SETTING, WHICH IS WHAT `docs/FUTURE.md` § 14
    /// ASKS FOR BY NAME: *"Glyph swapping on every prompt, driven by the last device used, not by
    /// a setting."* A setting is a screen a player has to find and a state they can get wrong; a
    /// player who picks up a pad mid-match has told you which glyph they want by picking it up.
    ///
    /// ⚠️⚠️ AND IT IS WHY THE PROMPTS ARE NOT A LIE ON A PAD. `docs/VISION.md` § 3: *"Key labels
    /// come from the live binding, never from a literal. A screen that teaches the wrong key is
    /// worse than one that teaches none."* Before this, `Hud.KeyLabel` resolved binding 0, which
    /// is always the KEYBOARD one, so a controller player was told to press X to pick up a
    /// slipper while holding a device with no X on it. That is the same class of fault as a
    /// hard-coded literal: it is a correct answer to a question nobody asked.
    ///
    /// ⚠️ IT IS POLLED RATHER THAN EVENT-DRIVEN. `InputSystem.onEvent` fires for every state
    /// change on every device including ones nobody touched (a pad's sticks drift, a mouse
    /// reports position), so a subscriber has to filter for "actuated" anyway. Asking three
    /// `wasUpdatedThisFrame` flags once per frame from the one component that already runs per
    /// frame is cheaper and has no subscription to leak.
    /// </summary>
    public static class LastInputDevice
    {
        /// <summary>
        /// ⚠️ IT STARTS ON KEYBOARD AND MOUSE RATHER THAN ON "UNKNOWN". There is no honest
        /// prompt for an unknown device, and the desktop build is the overwhelmingly common
        /// first frame.
        ///
        /// ⚠️⚠️ AND THE SENTENCE THAT USED TO FINISH THAT PARAGRAPH WAS WRONG, WHICH ONLY BECAME
        /// VISIBLE ONCE PROMPTS STARTED BRANCHING ON IT. It read: *"on Android the first touch
        /// corrects it before anything is drawn."* **The first touch is not before anything is
        /// drawn.** A phone player boots into the warmup window and reads the round line, the
        /// ready prompt and the sandbox row for however long it takes them to reach for the
        /// screen, and until 2026-09-04 nothing depended on this value so nobody noticed. Now
        /// `Hud.PressCue`, `Hud.MashVerb` and `GuidedTraining.Key` all branch on it, so a
        /// keyboard default means **the first thing a phone player sees is `[X] PICK UP`,
        /// `[F1] NO COOLDOWNS` and `Press [R] when ready`** — which is the exact defect 🧑
        /// reported (*"why the fuck does it have keybinds theres no keys in mobile"*) surviving
        /// the fix for it, in the one window where those prompts are largest.
        ///
        /// ⚠️ SEEDED FROM `TouchHud.ShouldShow`, WHICH ALREADY ANSWERS THIS QUESTION HONESTLY:
        /// the platform define on Android and iOS, and `Touchscreen.current != null` elsewhere,
        /// so a touchscreen laptop still boots on keyboard and a phone boots on touch. It is a
        /// SEED and not a lock: the very next keyboard or pad press moves it, which is what makes
        /// a phone with a Bluetooth keyboard behave.
        /// </summary>
        public static InputDeviceKind Current { get; private set; } = Seed();

        private static InputDeviceKind Seed()
            => TouchHud.ShouldShow ? InputDeviceKind.Touch : InputDeviceKind.KeyboardMouse;

        /// <summary>Bumped whenever <see cref="Current"/> changes, for label caches to key on.</summary>
        public static int Revision { get; private set; }

        /// <summary>The control-path device prefix for the current kind, for `Rebinding`.</summary>
        public static string DevicePath => Current switch
        {
            InputDeviceKind.Gamepad => "<Gamepad>",
            InputDeviceKind.Touch => "<Touchscreen>",
            _ => "<Keyboard>",
        };

        /// <summary>
        /// Called once a frame by <see cref="PlayerInputReader"/>.
        ///
        /// ⚠️⚠️ THE GAMEPAD TEST IS `wasUpdatedThisFrame` PLUS AN ACTUATION CHECK, AND THE
        /// ACTUATION CHECK IS LOAD-BEARING. A connected pad reports a state update on most frames
        /// whether or not anybody is holding it: sticks drift a fraction off centre and the
        /// device re-reports. Without the second half, plugging in a pad and then playing on the
        /// keyboard would flip every prompt to pad glyphs and leave them there.
        ///
        /// ⚠️ THE ORDER IS TOUCH, PAD, KEYBOARD, and it is the order of how deliberate the input
        /// is. A finger on the screen is unambiguous; a pad press is nearly so; a mouse reports
        /// motion from a desk bump.
        /// </summary>
        public static void Sample()
        {
            var touch = Touchscreen.current;

            if (touch != null && touch.primaryTouch.press.isPressed)
            {
                Set(InputDeviceKind.Touch);
                return;
            }

            if (TouchInput.Active && TouchInput.Move.sqrMagnitude > 0.0001f)
            {
                Set(InputDeviceKind.Touch);
                return;
            }

            var pad = Gamepad.current;

            if (pad != null && pad.wasUpdatedThisFrame && IsActuated(pad))
            {
                Set(InputDeviceKind.Gamepad);
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.isPressed) Set(InputDeviceKind.KeyboardMouse);

            var mouse = Mouse.current;

            if (mouse != null
                && (mouse.leftButton.isPressed || mouse.rightButton.isPressed
                    || mouse.delta.ReadValue().sqrMagnitude > 1.0f))
                Set(InputDeviceKind.KeyboardMouse);
        }

        /// <summary>
        /// Whether anybody is actually holding the pad, as opposed to it merely being plugged in.
        ///
        /// ⚠️ THE STICK THRESHOLD IS THE SAME 0.16 DEADZONE `PlayerInputReader` USES FOR LOOK.
        /// Two different numbers for "is this stick being pushed" is how a prompt starts
        /// disagreeing with the camera about whether the player is on a pad.
        /// </summary>
        private static bool IsActuated(Gamepad pad)
        {
            const float deadzone = 0.16f;

            if (pad.leftStick.ReadValue().sqrMagnitude > deadzone * deadzone) return true;
            if (pad.rightStick.ReadValue().sqrMagnitude > deadzone * deadzone) return true;
            if (pad.leftTrigger.ReadValue() > 0.2f || pad.rightTrigger.ReadValue() > 0.2f) return true;

            foreach (var control in pad.allControls)
            {
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button
                    && button.isPressed)
                    return true;
            }

            return false;
        }

        private static void Set(InputDeviceKind kind)
        {
            if (Current == kind) return;

            Current = kind;
            Revision++;
        }
    }
}
