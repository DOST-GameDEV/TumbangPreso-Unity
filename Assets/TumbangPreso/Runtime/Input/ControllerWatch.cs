using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TumbangPreso.InputLayer
{
    /// <summary>
    /// Notices a controller Unity could not match, and says so.
    ///
    /// ⚠️⚠️ AN UNRECOGNISED PAD WAS COMPLETELY SILENT, WHICH IS THE WORST SHAPE THIS FAILURE CAN
    /// TAKE. 🧑 2026-09-04: *"idk how extensive controller support is"*, *"maybe add to todo that
    /// it can work for fake controllers and shit too? haha or other brands"*. `docs/TODO.md`
    /// § 138 is the write-up; this is its step 1, and it is deliberately the first thing built
    /// because it needs no pad to write and it is what turns every later report into evidence.
    ///
    /// ⚠️⚠️ HOW A PAD REACHES THE GAME, BECAUSE THE FAILURE ONLY MAKES SENSE WITH IT. The Input
    /// System matches a USB HID device against its layout table by vendor and product id.
    /// A match produces something deriving from `Gamepad` with `buttonSouth` and the sticks in
    /// known places. **A pad it does not know still gets a device, auto-generated from the HID
    /// descriptor, and that one derives from `Joystick`, not `Gamepad`.** `Gamepad.current` never
    /// returns it.
    ///
    /// ⚠️⚠️ AND EVERY CONTROLLER PATH IN THIS GAME READS `Gamepad.current` OR A `&lt;Gamepad&gt;/`
    /// BINDING. So an unmatched pad means `LastInputDevice` never reports `Gamepad` and every
    /// prompt keeps showing keys; no verb fires, because the binding paths resolve to nothing;
    /// menu focus cannot move; and the settings GAMEPAD page lists controls that can never be
    /// bound. **From the player's side the pad is simply dead**, which is indistinguishable from
    /// a broken cable or a broken game.
    ///
    /// ⚠️ IT REPORTS RATHER THAN REPAIRS, AND THAT IS THE WHOLE SCOPE. A generic fallback layout
    /// is § 138.4 step 2 and is a bigger, riskier piece: it guesses a mapping that will be wrong
    /// for some pads. This one is a fact, and the vendor and product ids it prints are how the
    /// pads people actually own get collected in the first place.
    ///
    /// ⚠️ MOST CHEAP PC PADS ARE FINE AND THIS WILL STAY QUIET FOR THEM. They ship in XInput mode
    /// or carry an X/D switch, and in XInput mode Windows presents them as an Xbox pad and Unity
    /// matches them. What falls through is DirectInput-only pads, adapters for old console pads,
    /// and some arcade sticks. ⚠️ **On Android the matching is weaker**, and that platform ships.
    /// </summary>
    public static class ControllerWatch
    {
        /// <summary>
        /// Every unmatched controller-shaped device seen this run, newest last, as a line a
        /// person can read out. Empty is the normal state.
        /// </summary>
        public static IReadOnlyList<string> Unrecognised => Seen;

        private static readonly List<string> Seen = new List<string>();

        /// <summary>True when at least one controller was found that this game cannot use.</summary>
        public static bool HasUnrecognised => Seen.Count > 0;

        /// <summary>
        /// ⚠️ `AfterSceneLoad` AND IT ALSO SWEEPS WHAT IS ALREADY THERE. `onDeviceChange` only
        /// fires on a CHANGE, so a pad that was plugged in before the game started would never
        /// raise one and would be exactly the case nobody notices.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            foreach (var device in InputSystem.devices) Consider(device);

            InputSystem.onDeviceChange += (device, change) =>
            {
                if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
                    Consider(device);
            };
        }

        /// <summary>
        /// ⚠️ A `Joystick` IS THE SIGNATURE, AND IT IS A NARROW TEST ON PURPOSE. Reporting every
        /// device that is not a `Gamepad` would report the keyboard, the mouse and the
        /// touchscreen. `Joystick` is what Unity's HID support produces for a device that
        /// declared a gamepad or joystick usage and matched no known layout, which is precisely
        /// "a controller this game cannot use" and nothing else.
        /// </summary>
        private static void Consider(InputDevice device)
        {
            if (device == null) return;
            if (device is Gamepad) return;
            if (!(device is Joystick)) return;

            var description = device.description;

            string line = $"{device.displayName} " +
                          $"(manufacturer '{description.manufacturer}', " +
                          $"product '{description.product}', " +
                          $"interface '{description.interfaceName}')";

            if (Seen.Contains(line)) return;

            Seen.Add(line);

            // ⚠️ A WARNING RATHER THAN AN ERROR. Nothing is broken in the game; a device it does
            // not understand has been attached. An error here would fail every test run on a
            // machine that happens to have a flight stick plugged in.
            Debug.LogWarning(
                "[Controller] A controller was found that this game does not recognise, so it " +
                "will not work: " + line + ". It is a Joystick rather than a Gamepad, which " +
                "means Unity matched no layout for it and every <Gamepad> binding in the game " +
                "resolves to nothing on it. See docs/TODO.md section 138.");
        }

        /// <summary>
        /// One sentence for a screen, or empty when everything attached is usable.
        ///
        /// ⚠️ IT NAMES THE COUNT RATHER THAN THE DEVICE. The settings panel is not the place for
        /// a vendor id; the log has those and this line exists to tell the player the game knows
        /// their pad is there and cannot use it, which is the fact they are missing.
        /// </summary>
        public static string StatusLine()
        {
            if (Seen.Count == 0) return "";

            return Seen.Count == 1
                ? "A controller was found that this game does not recognise, so it will not work."
                : $"{Seen.Count} controllers were found that this game does not recognise.";
        }
    }
}
