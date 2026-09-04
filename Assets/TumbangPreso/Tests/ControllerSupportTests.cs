using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.InputLayer;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The controller pass, asserted where it is cheap to assert.
    ///
    /// ⚠️⚠️ THE THING WORTH GUARDING HERE IS THAT **THREE INDEPENDENTLY WRITTEN TABLES LINE UP**,
    /// and nothing else in the repository can see all three. The bindings live in
    /// `InputCatalogue` and `ScreenInputCatalogue`, the anchors come out of
    /// `tools/build_controller_diagram.py` as a generated manifest, and the ring of callouts is a
    /// list in `ControllerMapScreen`. A pad binding with no slot in the ring is a control the map
    /// silently does not draw; a slot with no anchor is a callout with no line. **Both are green
    /// everywhere else**, which is `docs/TODO.md` § 96's shape exactly: a feature nobody can find,
    /// with every check passing.
    ///
    /// ⚠️ IT RUNS IN EDITMODE AND TOUCHES NO SCENE. `docs/TODO.md` § 124.11's rule: *"a bound only
    /// a twelve-minute PlayMode run can enforce is a bound somebody edits a string past on a
    /// Friday."*
    /// </summary>
    public class ControllerSupportTests
    {
        private static InputActionAsset LoadAsset()
        {
            var asset = Resources.Load<InputActionAsset>("TumbangPreso");
            Assert.IsNotNull(asset, "no InputActionAsset at Resources/TumbangPreso.");
            return asset;
        }

        /// <summary>Every pad control the game binds, outside the spectator context.</summary>
        private static Dictionary<string, string> PlayingBindings(InputActionAsset asset)
        {
            var map = new Dictionary<string, string>();

            foreach (string action in Rebinding.RebindableActions)
            {
                if (Rebinding.IsSpectatorAction(action)) continue;

                string path = Rebinding.PathFor(asset, action, InputDeviceKind.Gamepad);
                if (path.Length == 0) continue;

                map[path.Substring(Rebinding.GamepadDevice.Length + 1)] = action;
            }

            return map;
        }

        // -----------------------------------------------------------------------------------
        // § THE MAP AND THE DRAWING
        // -----------------------------------------------------------------------------------

        [Test]
        public void TheDrawingHasAnAnchorForEveryControlTheMapDrawsACalloutFor()
        {
            // ⚠️ SKIPPED RATHER THAN FAILED WHEN THE GENERATED ART IS ABSENT, which is the one
            // case `PadDiagram` designs for: a clone without it. A red test there would say the
            // ring is wrong when the truth is that a PNG is missing.
            if (PadDiagram.Art == null)
                Assert.Ignore("the generated pad diagram is not in this checkout.");

            var missing = ControllerMapScreen.MappedControls
                .Where(control => !PadDiagram.TryAnchor(control, out _))
                .ToList();

            Assert.IsEmpty(missing,
                "these callouts have nowhere to point on the drawing, so they get no leader " +
                "line: " + string.Join(", ", missing) +
                ". Re-run tools/build_controller_diagram.py, or fix ControllerMapScreen.Ring.");
        }

        [Test]
        public void EveryPadControlTheGameBindsHasASlotOnTheMap()
        {
            var asset = LoadAsset();
            var slots = new HashSet<string>(ControllerMapScreen.MappedControls);

            var unmapped = PlayingBindings(asset)
                .Where(pair => !slots.Contains(pair.Key))
                .Select(pair => $"{pair.Value} on {pair.Key}")
                .ToList();

            Assert.IsEmpty(unmapped,
                "these actions are bound to a pad control the CONTROLLER MAP has no callout " +
                "for, so the screen that exists to show every binding silently hides them: " +
                string.Join(", ", unmapped));
        }

        [Test]
        public void TheTwoSticksAreOnTheMapEvenThoughNothingCanRebindThem()
        {
            var asset = LoadAsset();
            var slots = new HashSet<string>(ControllerMapScreen.MappedControls);

            // ⚠️ `Move` AND `Look` ARE THE TWO MOST USED CONTROLS ON A PAD AND NEITHER IS IN
            // `RebindableActions`, so the loop above cannot see them. A map with both sticks
            // blank is a map of a pad nobody is holding; `Rebinding.PlainPathFor` exists for it.
            foreach (string action in new[] { "Move", "Look" })
            {
                string path = Rebinding.PlainPathFor(asset, action, InputDeviceKind.Gamepad);

                Assert.IsNotEmpty(path, $"'{action}' has no gamepad binding at all.");

                string control = path.Substring(Rebinding.GamepadDevice.Length + 1);

                Assert.IsTrue(slots.Contains(control),
                    $"'{action}' is on {control} and the map has no callout for it.");
            }
        }

        [Test]
        public void EveryCalloutCanDrawAPictureOfItsControl()
        {
            // ⚠️⚠️ THE GLYPH IS LOOKED UP BY THE SAME STRING THE SETTINGS PANEL PRINTS, which is
            // `InputControlPath.ToHumanReadableString` uppercased. `UI.InputGlyphs`' own note says
            // its keys are *"exactly what `Hud.KeyLabel` returns, uppercase"*, so a control whose
            // human name has no row falls back to text on a screen that is otherwise all pictures.
            // That is a real, silent gap: nothing draws red, one row just looks different.
            var wordy = ControllerMapScreen.MappedControls
                .Where(control => !InputGlyphs.Has(ControllerMapScreen.HumanName(control)))
                .ToList();

            Assert.IsEmpty(wordy,
                "these controls have no glyph and will draw their name as text: " +
                string.Join(", ", wordy.Select(c => c + " -> " +
                                               ControllerMapScreen.HumanName(c))));
        }

        [Test]
        public void NoControlAppearsTwiceInTheRing()
        {
            var seen = new HashSet<string>();

            foreach (string control in ControllerMapScreen.MappedControls)
                Assert.IsTrue(seen.Add(control),
                    $"'{control}' has two callouts, so one of them will always be wrong.");
        }

        // -----------------------------------------------------------------------------------
        // § PAUSE, WHICH DID NOT EXIST ON A PAD
        // -----------------------------------------------------------------------------------

        [Test]
        public void PauseIsARealActionAndNotAKeyboardLiteral()
        {
            var asset = LoadAsset();

            // ⚠️⚠️ THE POINT OF THIS TEST IS `docs/TODO.md` § 35.3 AND § 142.2. `PauseWatcher`
            // read `Input.GetKeyDown(KeyCode.Escape)` and nothing else, so a pad player could not
            // leave a running match at all, and no audit in the repository could see it because a
            // literal keyboard read has no binding to find. Asserting the action EXISTS is what
            // stops it quietly reverting to a literal.
            Assert.Contains("Pause", Rebinding.RebindableActions,
                "Pause must be rebindable, or it is a literal again.");

            Assert.IsTrue(ScreenInputCatalogue.Declares("Pause"),
                "every non-verb action has to answer the pad question, even with a null.");

            Assert.AreEqual("<Gamepad>/start",
                            Rebinding.PathFor(asset, "Pause", InputDeviceKind.Gamepad),
                            "Start has opened the menu since 1985.");

            Assert.AreEqual("<Keyboard>/escape",
                            Rebinding.PathFor(asset, "Pause", InputDeviceKind.KeyboardMouse));
        }

        [Test]
        public void TheTacticalPauseIsNotAlsoOnStart()
        {
            var asset = LoadAsset();

            // ⚠️⚠️ THIS IS THE ONE THE GENERATOR GOT WRONG AND NOBODY WOULD HAVE SEEN.
            // `InputAssetSync` only ever ADDED bindings, so moving `SpectatorPause` off Start left
            // it on Start AND on buttonSouth. `FindDuplicateBindings` passes that, because the two
            // are in different contexts — but `PausePanel` renames itself BROADCAST MENU for a
            // spectator, so `PauseWatcher` is live for the same person and one press of Start did
            // both. § 142.2.
            Assert.AreNotEqual("<Gamepad>/start",
                               Rebinding.PathFor(asset, "SpectatorPause", InputDeviceKind.Gamepad),
                               "Start is the pause menu, for a spectator too.");
        }

        [Test]
        public void NoTwoActionsShareOneControlInOneContext()
        {
            var clashes = Rebinding.FindDuplicateBindings(LoadAsset());

            Assert.IsEmpty(clashes,
                "CLAUDE.md section 4: one control, one action, per context, per device. " +
                string.Join(" | ", clashes));
        }

        // -----------------------------------------------------------------------------------
        // § THE GENERIC PAD
        // -----------------------------------------------------------------------------------

        [Test]
        public void AnUnrecognisedJoystickIsGivenAGamepadToDrive()
        {
            // ⚠️⚠️ THIS PROVES THE WIRING AND NOT THE GUESS, AND THE DIFFERENCE MATTERS. A
            // synthetic `Joystick` added here has Unity's own generic layout, not a market-stall
            // pad's HID descriptor, so what is asserted is that an unmatched controller-shaped
            // device produces a `Gamepad` for the rest of the game to read. **Whether the button
            // order is right for any real pad is `docs/TODO.md` § 138.4 step 4 and needs
            // hardware**, which is why § 142.5 leaves it open rather than claiming it here.
            bool wasEnabled = GenericPadBridge.Enabled;
            var before = Gamepad.all.Count;
            Joystick stick = null;

            try
            {
                GenericPadBridge.Enabled = true;

                stick = InputSystem.AddDevice<Joystick>("TestUnmatchedPad");
                GenericPadBridge.Sync();

                Assert.IsTrue(GenericPadBridge.Active,
                    "an unmatched Joystick should have been bridged.");

                Assert.AreEqual(before + 1, Gamepad.all.Count,
                    "the bridge should have added exactly one stand-in Gamepad.");

                // ⚠️ AND IT GOES AWAY AGAIN. A bridge that outlived its joystick would leave a
                // phantom pad in `Gamepad.all` for the rest of the run, which makes
                // `Gamepad.current` non-null on a machine with no controller and flips every
                // prompt in the game to pad glyphs.
                InputSystem.RemoveDevice(stick);
                stick = null;
                GenericPadBridge.Sync();

                Assert.IsFalse(GenericPadBridge.Active, "the bridge should have been torn down.");
                Assert.AreEqual(before, Gamepad.all.Count, "the stand-in pad was left behind.");
            }
            finally
            {
                if (stick != null) InputSystem.RemoveDevice(stick);
                GenericPadBridge.Enabled = wasEnabled;
                GenericPadBridge.Sync();
            }
        }

        [Test]
        public void SwitchingTheGenericMappingOffRemovesTheStandInPad()
        {
            // ⚠️ THE SWITCH IS NOT A PREFERENCE, IT IS THE ESCAPE FROM A FLIGHT STICK. A throttle
            // axis bridged onto the movement stick holds a verb down for a whole match, and
            // nothing in a HID descriptor tells a wheel from a pad. If turning it off left the
            // stand-in device behind, the switch would do nothing and the match would stay broken.
            bool wasEnabled = GenericPadBridge.Enabled;
            Joystick stick = null;

            try
            {
                GenericPadBridge.Enabled = true;
                stick = InputSystem.AddDevice<Joystick>("TestUnmatchedPad");
                GenericPadBridge.Sync();

                Assert.IsTrue(GenericPadBridge.Active);

                GenericPadBridge.Enabled = false;

                Assert.IsFalse(GenericPadBridge.Active,
                    "switching the generic mapping off must drop the stand-in pad.");
            }
            finally
            {
                if (stick != null) InputSystem.RemoveDevice(stick);
                GenericPadBridge.Enabled = wasEnabled;
                GenericPadBridge.Sync();
            }
        }

        // -----------------------------------------------------------------------------------
        // § BACKING OUT
        // -----------------------------------------------------------------------------------

        [Test]
        public void EveryScreenBacksOutThroughTheOneReaderRatherThanAKeyboardLiteral()
        {
            // ⚠️⚠️ READ AS TEXT, FOR `SceneScriptCheck`'S REASON ONE LEVEL UP: this is a claim
            // about SOURCE, and no running test can see a screen nobody opened. Eleven call sites
            // read `Input.GetKeyDown(KeyCode.Escape)` before 2026-09-04 and **a pad could reach
            // every screen in the front end and leave none of them** (§ 142.1). A twelfth added
            // next month would be just as silent, so the guard has to be on the shape of the code.
            //
            // ⚠️ THE TWO ALLOWED READS ARE NAMED AND BOTH HAVE A WRITTEN REASON.
            // `InputLayer.MenuNav` IS the reader, and it keeps the legacy call on purpose because
            // that is how Android reports its hardware BACK button. `PauseWatcher` keeps it beside
            // its action for the same reason. Anything else is the fault coming back.
            string[] allowed =
            {
                "Assets/TumbangPreso/Runtime/Input/MenuNav.cs",
                "Assets/TumbangPreso/Runtime/PauseWatcher.cs",

                // ⚠️ THE SETTINGS PANEL'S REBIND-CANCEL BRANCH IS DELIBERATELY KEYBOARD-ONLY: the
                // rebind operation already cancels through the pad's own B, so routing it through
                // `MenuNav` would run CancelRebind twice on one press. Its comment says so.
                "Assets/TumbangPreso/Runtime/UI/ConvertedSettingsPanel.cs",
            };

            var offenders = new List<string>();

            foreach (string file in System.IO.Directory.GetFiles(
                         "Assets/TumbangPreso/Runtime", "*.cs", System.IO.SearchOption.AllDirectories))
            {
                string tidy = file.Replace('\\', '/');
                if (allowed.Any(a => tidy.EndsWith(a.Substring(a.LastIndexOf('/') + 1)))) continue;

                if (CodeOf(file).Contains("GetKeyDown(KeyCode.Escape)")) offenders.Add(tidy);
            }

            Assert.IsEmpty(offenders,
                "these read Escape directly instead of InputLayer.MenuNav, so a controller " +
                "cannot back out of them: " + string.Join(", ", offenders));
        }

        /// <summary>
        /// A source file with its line comments removed.
        ///
        /// ⚠️⚠️ THE FIRST VERSION OF THIS TEST DID NOT DO THIS AND FAILED ON TWO COMMENTS, WHICH
        /// IS THE EXACT FAULT `tools/audit_audio_reach.py` CARRIED FOR ITS WHOLE LIFE. `CLAUDE.md`
        /// § 7.1: *"it was the only audit that did not strip comments before looking for a gate,
        /// so `NetCue`'s own header explaining the gate it replaces registered as a gate."*
        /// `PlayerHub` and `RoleSwapCard` both carry a comment that NAMES the literal in order to
        /// explain why it is gone, and a reader trusting the red would have gone hunting for a
        /// bug in the fix.
        ///
        /// ⚠️ LINE COMMENTS ONLY, AND THAT IS ENOUGH FOR THIS QUESTION. Nothing in this runtime
        /// puts a block comment around a call, and a half-written block-comment stripper is a
        /// second thing that can be wrong about the source. Every doc comment in this repository
        /// starts `///`, which is a line comment.
        /// </summary>
        private static string CodeOf(string file)
        {
            var kept = new List<string>();

            foreach (string line in System.IO.File.ReadAllLines(file))
            {
                int slashes = line.IndexOf("//", System.StringComparison.Ordinal);
                kept.Add(slashes < 0 ? line : line.Substring(0, slashes));
            }

            return string.Join("\n", kept);
        }
    }
}
