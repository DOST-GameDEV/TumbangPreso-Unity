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
    ///
    /// ⚠⚠ ONE CONTROL, ONE ACTION, AND THE DEFAULTS NOW OBEY THAT RULE. `TryRebind` below
    /// refuses a key that another action already holds, and names the action holding it. The
    /// shipped defaults used to violate the rule the panel enforces: LEFT CLICK carried both
    /// `SpecialAbility` and `Grab`, E carried `Grab`, `Lunge` AND `Skill1`, and Q carried both
    /// `SpecialAbility` and `Skill2`. Whichever consumer ran first won, so throw felt like it
    /// was not on left click even though it was bound there, and a hero's first skill fired out
    /// of the pickup key. Rebinding anything onto E or Q was refused by our own asset.
    ///
    /// Every action now holds exactly ONE control and no control appears twice.
    /// `SettingsPanelTests` asserts it, so the collisions cannot come back quietly.
    /// </summary>
    public static class Rebinding
    {
        /// <summary>Action names as they appear in the Input System asset or composite parts.</summary>
        public static readonly string[] RebindableActions =
        {
            "MoveForward", "MoveBackward", "MoveLeft", "MoveRight",
            "Sprint", "Jump",
            "SpecialAbility", "Grab", "Lunge",
            "Skill1", "Skill2", "Ultimate",
            "ReadyUp", "CleanFeed", "AbilityInfo",
            "EmoteWheel",
            "SpectatorDown",
            "ToggleFullscreen",
        };

        /// <summary>
        /// Human-readable labels. Several are named for the JOB rather than the verb, because
        /// one key genuinely does several jobs and the player cannot guess which from a name:
        /// - THROW / PUNCH is one key doing two things by ROLE. An attacker charges and throws;
        ///   the taya, who `can_throw()` refuses outright, punches (`Design.md` §4, §5.1).
        /// - PICK UP / SHOVE / RESET is the contextual key. Tap with a tsinelas in reach picks
        ///   it up, tap with nothing grabbable shoves, hold as the taya in the lata's ring runs
        ///   the reset channel (§4, §5.2, §5.3).
        /// - LUNGE TAG is the taya's dash tag: the way to stop an attacker retrieving a slipper
        ///   inside the box (§5.2, §6).
        /// </summary>
        public static readonly Dictionary<string, string> ActionLabels = new Dictionary<string, string>
        {
            { "MoveForward", "Move Forward" },
            { "MoveBackward", "Move Backward" },
            { "MoveLeft", "Move Left" },
            { "MoveRight", "Move Right" },
            { "Move", "Move" },
            { "SpecialAbility", "Throw / Punch" },
            { "Grab", "Pick Up / Shove / Reset" },
            { "Lunge", "Lunge Tag" },
            { "Jump", "Jump" },
            { "Sprint", "Sprint" },
            { "Skill1", "Skill 1" },
            { "Skill2", "Skill 2" },
            { "Ultimate", "Ultimate" },
            { "ReadyUp", "Ready Up" },
            { "CleanFeed", "Hide HUD" },
            { "AbilityInfo", "Hold: Ability Info" },
            { "EmoteWheel", "Emote Wheel" },
            { "SpectatorDown", "Spectator Down" },
            { "ToggleFullscreen", "Fullscreen" },
        };

        public static string LabelFor(string action)
            => ActionLabels.TryGetValue(action, out string label) ? label : action;

        /// <summary>
        /// The controls list, cut into named groups in the order they should be shown.
        ///
        /// ⚠️⚠️ ONE FLAT LIST OF FOURTEEN ROWS IS WHAT THIS REPLACED, and it was rejected on
        /// sight: *"can u also organize setttings better, separet diff controls to diff groups
        /// bcz it feels overwhelming to look at now"*. Fourteen unlabelled rows is a wall. The
        /// grouping is not decoration, it is what lets somebody scan for the one line they came
        /// to change instead of reading all of them.
        ///
        /// ⚠️ THE GROUPS ARE BY WHEN YOU USE THEM, NOT BY DEVICE OR BY SUBSYSTEM. "Movement" is
        /// what you press constantly, "Playing the game" is the tumbang preso verbs, "Hero
        /// powers" only exists in Hero Strike, and "Interface" is everything you press between
        /// rounds. A player looking for the throw key does not think "mouse buttons".
        ///
        /// ⚠️ EVERY ACTION IN `RebindableActions` MUST APPEAR IN EXACTLY ONE GROUP. A row that
        /// belongs to no group would vanish from the panel with no error, which is the same
        /// silent failure the class note at the top warns about. `SettingsGroupsCoverEveryAction`
        /// asserts it.
        /// </summary>
        public static readonly (string Title, string[] Actions)[] Groups =
        {
            ("MOVEMENT", new[] { "MoveForward", "MoveBackward", "MoveLeft", "MoveRight", "Sprint", "Jump" }),
            ("PLAYING THE GAME", new[] { "SpecialAbility", "Grab", "Lunge" }),
            ("HERO POWERS", new[] { "Skill1", "Skill2", "Ultimate", "AbilityInfo" }),
            ("ROUND AND SCREEN", new[] { "ReadyUp", "EmoteWheel", "CleanFeed", "SpectatorDown",
                                         "ToggleFullscreen" }),
        };

        /// <summary>
        /// A short line under a group heading, for the two groups whose rows do more than one
        /// job and cannot say so in a label.
        /// </summary>
        public static string BlurbFor(string title)
        {
            switch (title)
            {
                case "PLAYING THE GAME":
                    return "One key can do several jobs, chosen by what is in front of you.";
                case "HERO POWERS":
                    return "Hero Strike only. Classic has no powers.";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Resolves an action name (including discrete composite movements like MoveForward)
        /// into the underlying InputAction and binding index.
        /// </summary>
        public static bool ResolveActionAndBindingIndex(InputActionAsset asset, string actionName,
            out InputAction action, out int bindingIndex)
        {
            action = null;
            bindingIndex = -1;
            if (asset == null || string.IsNullOrEmpty(actionName)) return false;

            if (actionName == "MoveForward" || actionName == "MoveBackward" ||
                actionName == "MoveLeft" || actionName == "MoveRight")
            {
                action = Find(asset, "Move");
                if (action == null) return false;

                string part = actionName == "MoveForward" ? "up" :
                              actionName == "MoveBackward" ? "down" :
                              actionName == "MoveLeft" ? "left" : "right";

                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var b = action.bindings[i];
                    if (b.isPartOfComposite && string.Equals(b.name, part, System.StringComparison.OrdinalIgnoreCase))
                    {
                        bindingIndex = i;
                        return true;
                    }
                }

                bindingIndex = FirstKeyboardBinding(action);
                return bindingIndex >= 0;
            }

            action = Find(asset, actionName);
            if (action == null) return false;

            bindingIndex = FirstKeyboardBinding(action);
            return bindingIndex >= 0;
        }

        /// <summary>
        /// Every control bound twice across two different actions, as "path: ActionA, ActionB".
        /// Empty means the map is clean.
        /// </summary>
        public static List<string> FindDuplicateBindings(InputActionAsset asset)
        {
            var owners = new Dictionary<string, List<string>>();
            var clashes = new List<string>();
            if (asset == null) return clashes;

            foreach (string action in RebindableActions)
            {
                if (!ResolveActionAndBindingIndex(asset, action, out var a, out int bindingIndex))
                    continue;

                var b = a.bindings[bindingIndex];
                string path = b.effectivePath;
                if (string.IsNullOrEmpty(path)) continue;

                if (!owners.TryGetValue(path, out var list))
                    owners[path] = list = new List<string>();

                if (!list.Contains(action)) list.Add(action);
            }

            foreach (var pair in owners)
                if (pair.Value.Count > 1)
                    clashes.Add(pair.Key + ": " + string.Join(", ", pair.Value));

            clashes.Sort();
            return clashes;
        }

        private const string OverridesKey = "tumbangpreso.bindings";

        /// <summary>
        /// The display name of whatever is currently bound to <paramref name="action"/>.
        /// </summary>
        public static string DisplayNameFor(InputActionAsset asset, string action)
        {
            if (!ResolveActionAndBindingIndex(asset, action, out var a, out int bindingIndex))
                return "-";

            return InputControlPath.ToHumanReadableString(
                a.bindings[bindingIndex].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        /// <summary>
        /// Rebind <paramref name="action"/> to <paramref name="control"/>.
        /// </summary>
        public static string TryRebind(InputActionAsset asset, string action, InputControl control)
        {
            string path = control.path;

            foreach (string other in RebindableActions)
            {
                if (other == action) continue;

                if (ResolveActionAndBindingIndex(asset, other, out var otherAction, out int otherIndex))
                {
                    if (otherAction.bindings[otherIndex].effectivePath == path)
                        return LabelFor(other);
                }
            }

            if (!ResolveActionAndBindingIndex(asset, action, out var target, out int index))
                return LabelFor(action);

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
