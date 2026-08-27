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
    /// Every action now holds exactly ONE control and no control appears twice. The hero deck
    /// uses Q, E and F as a compact combat cluster; contextual pickup / shove / reset uses X.
    /// `SettingsPanelTests` asserts it, so the collisions cannot come back quietly.
    /// </summary>
    public static class Rebinding
    {
        /// <summary>Action names as they appear in the Input System asset or composite parts.</summary>
        public static readonly string[] RebindableActions =
        {
            "MoveForward", "MoveBackward", "MoveLeft", "MoveRight",
            "Sprint", "Jump",
            "SpecialAbility", "Grab", "Lunge", "CurveLeft", "CurveRight",
            "Skill1", "Skill2", "Ultimate",
            "ReadyUp", "CleanFeed", "AbilityInfo",
            "EmoteWheel",
            "ToggleFullscreen",

            // § SPECTATOR AND BROADCAST. See `SpectatorContext` for why these may share a key
            // with a gameplay action and nothing else may.
            "SpectatorAutopilot", "SpectatorCycleTarget", "SpectatorFreeFly", "SpectatorPov",
            "SpectatorDown", "SpectatorMark", "SpectatorRecall",
            "SpectatorPause", "SpectatorReplay", "SpectatorControls",
        };

        /// <summary>
        /// The actions that only exist while somebody is spectating.
        ///
        /// ⚠⚠ THIS SET IS WHAT LETS A SPECTATOR KEY SHARE A CONTROL WITH A GAMEPLAY ONE, AND
        /// IT IS A REFINEMENT OF `CLAUDE.md` § 4'S RULE RATHER THAN A HOLE IN IT. That rule reads
        /// *"one control, one action, in the input map"*, and what it protects against is a key
        /// that does two things AT THE SAME TIME: the panel refuses such a binding, and a shipped
        /// default that breaks it is a defect. **A spectator has no body, no seat and no
        /// `CharacterMotor`.** While spectating, every gameplay action is inert; while playing,
        /// none of these is reachable. The two sets can never both fire, so binding TAB to both
        /// "hold for ability info" and "cycle spectator target" is not one key doing two things,
        /// it is two mutually exclusive screens.
        ///
        /// ⚠⚠ AND THE ALTERNATIVE WAS WORSE. These nine keys were `Keyboard.current` reads
        /// inside `SpectatorCamera` and `Hud` until 2026-08-27: not rebindable, not visible in the
        /// panel, and not checked against anything. Giving them fresh non-clashing defaults would
        /// have moved TAB, F, B and R for every existing spectator to satisfy a rule about
        /// simultaneity that was never at risk. Naming the context is the honest fix.
        ///
        /// ⚠️ `CleanFeed` IS DELIBERATELY NOT IN HERE. Hiding the HUD is a PLAYER action that a
        /// spectator also uses, it has always been in the map, and its H default clashes with
        /// nothing.
        /// </summary>
        public static readonly string[] SpectatorContext =
        {
            "SpectatorAutopilot", "SpectatorCycleTarget", "SpectatorFreeFly", "SpectatorPov",
            "SpectatorDown", "SpectatorMark", "SpectatorRecall",
            "SpectatorPause", "SpectatorReplay", "SpectatorControls",
        };

        public static bool IsSpectatorAction(string action)
            => System.Array.IndexOf(SpectatorContext, action) >= 0;

        /// <summary>True when two actions can ever be pressed in the same screen.</summary>
        public static bool ShareAContext(string a, string b)
            => IsSpectatorAction(a) == IsSpectatorAction(b);

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
            { "CurveLeft", "Curve Left (Pektus)" },
            { "CurveRight", "Curve Right (Pektus)" },
            { "Jump", "Jump" },
            { "Sprint", "Sprint" },
            { "Skill1", "Skill 1" },
            { "Skill2", "Skill 2" },
            { "Ultimate", "Ultimate" },
            { "ReadyUp", "Ready Up" },
            { "CleanFeed", "Hide HUD" },
            { "AbilityInfo", "Hold: Ability Info" },
            { "EmoteWheel", "Emote Wheel" },
            { "SpectatorDown", "Fly Down" },
            { "ToggleFullscreen", "Fullscreen" },
            { "SpectatorAutopilot", "Autopilot On / Off" },
            { "SpectatorCycleTarget", "Next Player" },
            { "SpectatorFreeFly", "Free Flight" },
            { "SpectatorPov", "Through Their Eyes" },
            { "SpectatorMark", "Save Camera Mark" },
            { "SpectatorRecall", "Recall Camera Mark" },
            { "SpectatorPause", "Tactical Pause" },
            { "SpectatorReplay", "Instant Replay" },
            { "SpectatorControls", "Show Spectator Controls" },
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
            ("PLAYING THE GAME", new[] { "SpecialAbility", "Grab", "Lunge",
                                         "CurveLeft", "CurveRight" }),
            ("HERO POWERS", new[] { "Skill1", "Skill2", "Ultimate", "AbilityInfo" }),
            ("ROUND AND SCREEN", new[] { "ReadyUp", "EmoteWheel", "CleanFeed",
                                         "ToggleFullscreen" }),
            ("SPECTATOR CAMERA", new[] { "SpectatorAutopilot", "SpectatorCycleTarget",
                                         "SpectatorFreeFly", "SpectatorPov", "SpectatorDown",
                                         "SpectatorMark", "SpectatorRecall" }),
            ("BROADCAST GALLERY", new[] { "SpectatorPause", "SpectatorReplay",
                                          "SpectatorControls" }),
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
                    // ⚠️ ONE LINE. The blurb draws in a 20 px row under the heading and does not
                    // wrap; a second sentence was added here on 2026-08-26 and the screenshot in
                    // `Logs/shots-runtime/SettingsPanel.png` cut it off mid-word. What the curve
                    // rows do is in their own labels.
                    return "One key can do several jobs, chosen by what is in front of you.";
                case "HERO POWERS":
                    return "Hero Strike only. Classic has no powers.";
                case "SPECTATOR CAMERA":
                    // ⚠️ IT SAYS THE SHARING IS DELIBERATE, because a player who reads the panel
                    // top to bottom will otherwise see TAB and F listed twice and report it.
                    return "Watching only. These may share a key with a playing control.";
                case "BROADCAST GALLERY":
                    return "Watching only. Autopilot never uses these.";
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

            // ⚠⚠ A CLASH IS TWO ACTIONS THAT CAN FIRE ON THE SAME SCREEN, NOT TWO ACTIONS ON
            // THE SAME KEY. See `SpectatorContext`: a spectator has no body, so TAB meaning
            // "ability info" while playing and "next player" while watching is two screens, not
            // one key doing two things. Every pair inside one context is still a defect.
            foreach (var pair in owners)
            {
                var conflicting = new List<string>();

                foreach (string owner in pair.Value)
                    foreach (string other in pair.Value)
                        if (owner != other && ShareAContext(owner, other)
                            && !conflicting.Contains(owner))
                            conflicting.Add(owner);

                if (conflicting.Count > 1)
                    clashes.Add(pair.Key + ": " + string.Join(", ", conflicting));
            }

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

                // ⚠️ THE PANEL REFUSES A KEY ANOTHER action IN THE SAME CONTEXT HOLDS, and only
                // that. Refusing across contexts would make half the spectator rows unbindable to
                // the obvious key for no reason a player could ever work out from the screen.
                if (!ShareAContext(action, other)) continue;

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
