using System.Collections.Generic;
using System.IO;
using System.Text;
using TumbangPreso.InputLayer;
using TumbangPreso.Settings;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Writes the gamepad half of `Resources/TumbangPreso.inputactions` FROM
    /// <see cref="InputCatalogue"/>, and reports when the two have drifted apart.
    ///
    /// ⚠️⚠️ THE ASSET IS GENERATED, NOT MAINTAINED, AND THAT IS THE POINT. A hand-edited
    /// `.inputactions` is a second table beside the catalogue, and `Settings.Rebinding`'s own
    /// class note records what a second table costs here: *"a stale row in either table is not
    /// cosmetic... here a missing action silently produces a dead row instead, which is worse,
    /// because nobody notices."* Adding a verb now edits ONE file and runs ONE command.
    ///
    /// ⚠️ THE KEYBOARD HALF IS LEFT ALONE. Those bindings are 🧑's own choices, several of them
    /// argued out in `Rebinding`'s notes (Z and C for the curve, X for the contextual grab), and
    /// a generator that rewrote them would silently revert a decision that took a conversation.
    /// This adds and repairs gamepad bindings and nothing else.
    ///
    /// Run it with:
    /// <code>
    /// Unity.exe -batchmode -quit -projectPath . \
    ///   -executeMethod TumbangPreso.EditorTools.InputAssetSync.Regenerate
    /// </code>
    /// </summary>
    public static class InputAssetSync
    {
        private const string AssetPath = "Assets/TumbangPreso/Resources/TumbangPreso.inputactions";

        /// <summary>The binding group every generated binding carries.</summary>
        public const string GamepadScheme = "Gamepad";

        public const string KeyboardScheme = "Keyboard&Mouse";
        public const string TouchScheme = "Touch";

        /// <summary>
        /// The pad's look stick.
        ///
        /// ⚠️ IT IS ITS OWN ACTION AND HAS NO KEYBOARD BINDING, because a mouse reports a DELTA
        /// and a stick reports a POSITION. Binding both to one action would make
        /// `ReadValue&lt;Vector2&gt;` mean two different physical quantities depending on which
        /// device moved last, and the sensitivity number would be wrong for one of them whatever
        /// value it took. `PlayerInputReader.ReadLookDelta` combines them instead.
        /// </summary>
        public const string LookAction = "Look";

        public const string LookPath = "<Gamepad>/rightStick";

        /// <summary>The pad's movement stick, added beside the WASD composite rather than into it.</summary>
        public const string MovePath = "<Gamepad>/leftStick";

        [MenuItem("Tumbang Preso/Input/Regenerate gamepad bindings")]
        public static void Regenerate()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);

            if (asset == null)
            {
                Debug.LogError($"[Input] no InputActionAsset at {AssetPath}.");
                EditorApplication.Exit(1);
                return;
            }

            var map = asset.FindActionMap("Player", throwIfNotFound: false);

            if (map == null)
            {
                Debug.LogError("[Input] the asset has no 'Player' action map.");
                EditorApplication.Exit(1);
                return;
            }

            var log = new StringBuilder();
            int added = 0;

            // ---- control schemes ---------------------------------------------------------
            //
            // ⚠️ THE SCHEMES ARE DECLARED EVEN THOUGH NOTHING SWITCHES ON THEM YET. They are what
            // lets the settings panel show one device's bindings at a time and what
            // `InputSystemUIInputModule` uses to decide which device is driving the menu, and a
            // scheme added later cannot retro-label bindings that were written without a group.
            added += EnsureScheme(asset, KeyboardScheme, new[] { "<Keyboard>", "<Mouse>" }, log);
            added += EnsureScheme(asset, GamepadScheme, new[] { "<Gamepad>" }, log);
            added += EnsureScheme(asset, TouchScheme, new[] { "<Touchscreen>" }, log);

            // ---- the look stick ----------------------------------------------------------
            var look = map.FindAction(LookAction, throwIfNotFound: false);

            if (look == null)
            {
                look = map.AddAction(LookAction, InputActionType.Value, expectedControlLayout: "Vector2");
                log.AppendLine($"  + action {LookAction}");
                added++;
            }

            added += EnsureBinding(look, LookPath, log);

            // ---- movement ----------------------------------------------------------------
            //
            // ⚠️⚠️ THE STICK IS A PLAIN BINDING BESIDE THE `WASD` COMPOSITE, NOT A PART OF IT. A
            // 2DVector composite reads four BUTTONS and synthesises an axis; feeding a stick into
            // one would quantise it back to the corners of a square and throw away every analogue
            // value, so a pad would walk at exactly one speed in exactly eight directions.
            var move = map.FindAction("Move", throwIfNotFound: false);
            if (move != null) added += EnsureBinding(move, MovePath, log);

            // ---- every verb, from the catalogue ------------------------------------------
            foreach (var entry in InputCatalogue.All)
            {
                var action = map.FindAction(entry.Action, throwIfNotFound: false);

                if (action == null)
                {
                    Debug.LogError($"[Input] catalogue names action '{entry.Action}' for " +
                                   $"{entry.Verb}, and the asset has no such action.");
                    EditorApplication.Exit(1);
                    return;
                }

                added += EnsureBinding(action, entry.GamepadPath, log);
            }

            // ---- the screen and spectator actions ----------------------------------------
            foreach (var row in ScreenInputCatalogue.Rows)
            {
                if (row.GamepadPath == null) continue; // a written-down "no". See the table.

                var action = map.FindAction(row.Action, throwIfNotFound: false);

                if (action == null)
                {
                    Debug.LogError($"[Input] catalogue names action '{row.Action}', and the " +
                                   "asset has no such action.");
                    EditorApplication.Exit(1);
                    return;
                }

                added += EnsureBinding(action, row.GamepadPath, log);
            }

            // ---- write it back -----------------------------------------------------------
            //
            // ⚠️ THROUGH `ToJson` AND A FILE WRITE, NOT `EditorUtility.SetDirty`. An
            // `.inputactions` file IS its JSON; marking the imported object dirty saves the
            // imported copy and leaves the text on disk untouched, so the change survives until
            // the next reimport and then vanishes.
            File.WriteAllText(AssetPath, asset.ToJson(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Input] gamepad sync wrote {added} change(s) to {AssetPath}\n{log}");
        }

        /// <summary>
        /// Every catalogue row the asset does not carry. Empty means the two agree.
        ///
        /// ⚠️ IT IS THE VERIFIER THE TEST USES, and it deliberately does not repair anything. A
        /// check that fixes what it finds reports green for ever and nobody learns the asset had
        /// drifted; `Regenerate` is the repair and it is a separate, deliberate command.
        /// </summary>
        public static List<string> Missing(InputActionAsset asset)
        {
            var gaps = new List<string>();
            if (asset == null) return gaps;

            var map = asset.FindActionMap("Player", throwIfNotFound: false);

            if (map == null)
            {
                gaps.Add("the asset has no 'Player' action map");
                return gaps;
            }

            foreach (var entry in InputCatalogue.All)
            {
                var action = map.FindAction(entry.Action, throwIfNotFound: false);

                if (action == null)
                {
                    gaps.Add($"{entry.Verb}: no action '{entry.Action}' in the asset");
                    continue;
                }

                if (!HasBinding(action, entry.GamepadPath))
                    gaps.Add($"{entry.Verb}: '{entry.Action}' has no binding on {entry.GamepadPath}");
            }

            foreach (var row in ScreenInputCatalogue.Rows)
            {
                if (row.GamepadPath == null) continue;

                var action = map.FindAction(row.Action, throwIfNotFound: false);

                if (action == null)
                {
                    gaps.Add($"no action '{row.Action}' in the asset");
                    continue;
                }

                if (!HasBinding(action, row.GamepadPath))
                    gaps.Add($"'{row.Action}' has no binding on {row.GamepadPath}");
            }

            var look = map.FindAction(LookAction, throwIfNotFound: false);

            if (look == null) gaps.Add($"no '{LookAction}' action: a pad cannot turn the camera");
            else if (!HasBinding(look, LookPath)) gaps.Add($"'{LookAction}' is not on {LookPath}");

            var move = map.FindAction("Move", throwIfNotFound: false);

            if (move != null && !HasBinding(move, MovePath))
                gaps.Add($"'Move' has no {MovePath} binding: a pad cannot walk");

            return gaps;
        }

        private static bool HasBinding(InputAction action, string path)
        {
            foreach (var binding in action.bindings)
                if (binding.path == path) return true;

            return false;
        }

        private static int EnsureBinding(InputAction action, string path, StringBuilder log)
        {
            if (HasBinding(action, path)) return 0;

            action.AddBinding(path, groups: GamepadScheme);
            log.AppendLine($"  + {action.name} -> {path}");
            return 1;
        }

        private static int EnsureScheme(InputActionAsset asset, string name, string[] devices,
                                        StringBuilder log)
        {
            foreach (var scheme in asset.controlSchemes)
                if (scheme.name == name) return 0;

            var builder = asset.AddControlScheme(name);

            foreach (string device in devices)
                builder = builder.WithRequiredDevice(device);

            builder.Done();

            log.AppendLine($"  + control scheme {name}");
            return 1;
        }
    }
}
