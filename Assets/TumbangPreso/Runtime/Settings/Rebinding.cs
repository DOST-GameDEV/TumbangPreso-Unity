using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TumbangPreso.Settings
{
    /// <summary>
    /// Which actions the player may rebind, what to call them, and the override store.
    /// Converted from `settings_manager.gd`'s `REBINDABLE_ACTIONS` / `ACTION_LABELS` and the
    /// rebind half of `settings_panel.gd`.
    ///
    /// ⚠️⚠️ A STALE ROW IN EITHER TABLE IS NOT COSMETIC. In Godot, rebinding walked every
    /// listed action at boot and errored loudly on one that no longer existed (`bump`,
    /// `guard_dash`). Here a missing action silently produces a dead row instead — which is
    /// worse, because nobody notices. **If an action is renamed or deleted, fix these lists
    /// in the same commit.**
    ///
    /// ⚠️ THE LIST ONCE HELD "grab" TWICE AND OMITTED "lunge" ENTIRELY — the taya's only
    /// scoring verb had no rebind row at all. Both `lunge` and `emote_wheel` belong; they
    /// arrived from different branches and taking either side alone drops the other's control
    /// off the panel, which is a defect nobody would go looking for.
    /// </summary>
    public static class Rebinding
    {
        /// <summary>Action names as they appear in the Input System asset.</summary>
        public static readonly string[] RebindableActions =
        {
            "Move",              // the four directions are one composite here
            "SpecialAbility", "Grab", "Lunge", "Jump", "Sprint",
            "Skill1", "Skill2", "Ultimate",
            "ReadyUp", "CleanFeed",
            "EmoteWheel",
            "ToggleFullscreen",
        };

        /// <summary>
        /// Human-readable labels. Two of these are named for the job the player cannot guess
        /// from the verb:
        /// - GRAB is also the hold that carries a displaced lata home (`Design.md` §5.2).
        /// - LUNGE is the taya's tag: the only way to stop an attacker retrieving a slipper
        ///   inside the box (§5.2, §6).
        /// </summary>
        public static readonly Dictionary<string, string> ActionLabels = new Dictionary<string, string>
        {
            { "Move", "Move" },
            { "SpecialAbility", "Throw" },
            { "Grab", "Grab" },
            { "Lunge", "Lunge" },
            { "Jump", "Jump" },
            { "Sprint", "Sprint" },
            { "Skill1", "Skill 1" },
            { "Skill2", "Skill 2" },
            { "Ultimate", "Ultimate" },
            { "ReadyUp", "Ready Up" },
            { "CleanFeed", "Hide HUD" },
            { "EmoteWheel", "Emote Wheel" },
            { "ToggleFullscreen", "Fullscreen" },
        };

        public static string LabelFor(string action)
            => ActionLabels.TryGetValue(action, out string label) ? label : action;

        private const string OverridesKey = "tumbangpreso.bindings";

        /// <summary>
        /// The display name of whatever is currently bound to <paramref name="action"/>.
        /// </summary>
        public static string DisplayNameFor(InputActionAsset asset, string action)
        {
            var a = Find(asset, action);
            if (a == null) return "-";

            int binding = FirstKeyboardBinding(a);
            if (binding < 0) return "-";

            return InputControlPath.ToHumanReadableString(
                a.bindings[binding].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        /// <summary>
        /// Rebind <paramref name="action"/> to <paramref name="control"/>.
        ///
        /// ⚠️ IT REFUSES A KEY ALREADY IN USE AND SAYS WHICH ACTION HAS IT, rather than
        /// silently creating a double binding. Godot returned the conflicting action's label
        /// for exactly this message; two verbs on one key is unplayable and undiagnosable.
        /// Returns null on success, or the conflicting action's LABEL on refusal.
        /// </summary>
        public static string TryRebind(InputActionAsset asset, string action, InputControl control)
        {
            string path = control.path;

            foreach (string other in RebindableActions)
            {
                if (other == action) continue;

                var o = Find(asset, other);
                if (o == null) continue;

                foreach (var b in o.bindings)
                    if (b.effectivePath == path) return LabelFor(other);
            }

            var target = Find(asset, action);
            if (target == null) return LabelFor(action);

            int index = FirstKeyboardBinding(target);
            if (index < 0) return LabelFor(action);

            target.ApplyBindingOverride(index, path);
            Save(asset);
            return null;
        }

        /// <summary>Every override, back to the asset's own defaults.</summary>
        public static void ResetAll(InputActionAsset asset)
        {
            if (asset == null) return;

            asset.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(OverridesKey);
            PlayerPrefs.Save();
        }

        public static void Save(InputActionAsset asset)
        {
            if (asset == null) return;

            PlayerPrefs.SetString(OverridesKey, asset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }

        /// <summary>
        /// ⚠️ CALL THIS BEFORE ANY SCREEN READS A BINDING. Overrides live outside the asset,
        /// so an unloaded asset silently shows and uses the defaults — which reads to the
        /// player as their rebind having been forgotten.
        /// </summary>
        public static void Load(InputActionAsset asset)
        {
            if (asset == null) return;

            string json = PlayerPrefs.GetString(OverridesKey, "");
            if (!string.IsNullOrEmpty(json)) asset.LoadBindingOverridesFromJson(json);
        }

        private static InputAction Find(InputActionAsset asset, string action)
        {
            var map = asset != null ? asset.FindActionMap("Player", false) : null;
            return map?.FindAction(action, false);
        }

        /// <summary>The first keyboard binding on an action, skipping composite parents.</summary>
        private static int FirstKeyboardBinding(InputAction action)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var b = action.bindings[i];
                if (b.isComposite) continue;
                if (string.IsNullOrEmpty(b.effectivePath)) continue;

                return i;
            }

            return -1;
        }
    }
}
