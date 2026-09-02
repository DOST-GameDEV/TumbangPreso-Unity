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
    ///
    /// ⚠️⚠️ THE PEKTUS CURVE IS Z AND C, NOT THE ARROW KEYS, SINCE 2026-08-27. 🧑: *"rebind pektus
    /// to keyboard keys that are close to wasd bcz its so hard to touch the arrow keys and some
    /// keyboards dont have it"*. Both halves of that are real. The curve is held WHILE the throw
    /// charges on the left mouse button and WHILE the player is moving on WASD, so it is the one
    /// input that must overlap the movement hand, and the arrow cluster is the furthest point on
    /// the board from it. Sixty per cent boards do not carry the arrows at all.
    ///
    /// ⚠️ Z AND C, WITH X BETWEEN THEM, AND THE SHAPE IS THE REASON. `Grab` already holds X, so
    /// the bottom row reads curve-left, contextual, curve-right under a hand that never leaves
    /// WASD, and left and right map to left and right on the keyboard.
    ///
    /// ⚠️ C IS ALSO `SpectatorControls`, WHICH IS LEGAL RATHER THAN AN OVERSIGHT. See
    /// `SpectatorContext`: a spectator has no body, so no throw can be curving while that key
    /// means "show the overlay". `FindDuplicateBindings` checks per context and passes. Both
    /// curve rows stay in the panel under PLAYING THE GAME and stay rebindable, and every screen
    /// that teaches them reads the live binding through `Hud.KeyLabel`.
    ///
    /// ⚠️⚠️ AND R IS THE ONE OVERLAP THAT WAS NOT LEGAL, WHICH IS WHY IT WAS THE ONE REPORTED.
    /// 🧑 2026-08-29: *"theres a conflict, R for spectator map to replay and ready can u pls fix
    /// that"*. `SpectatorReplay` is `Y` now; the other four overlaps stay exactly as they are.
    ///
    /// **The rule above is "a spectator has no body", and READY is the one gameplay action that
    /// does not need one.** `ReadyGate` and `BufferSkipVote` both read `ReadyUp` from whoever
    /// presses it regardless of role — `docs/TODO.md` § 78.6 is a whole entry about a spectator's
    /// R reaching a quorum that excludes them — so a spectator pressing R fired the pre-round
    /// ready, the buffer-skip vote AND the replay from one key. That is not an overlap the
    /// context can separate, because both sides of it are live in the same context.
    ///
    /// ⚠️ THE SPECTATOR ACTION MOVED, NOT THE PLAYER ONE. R is taught on the ready prompt, in the
    /// rebind panel under ROUND AND SCREEN and by the buffer-skip row; replay is an operator key
    /// nobody is taught in a match. When two must part, the one with fewer readers moves.
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
        /// Every binding index that belongs to <paramref name="actionName"/>, across all devices.
        ///
        /// ⚠️⚠️ THIS EXISTS BECAUSE `ResolveActionAndBindingIndex` RETURNS THE FIRST BINDING AND
        /// AN ACTION NOW HAS SEVERAL. Before the gamepad pass every action held exactly one
        /// control, so "the first binding" and "the binding" were the same sentence, and
        /// `FindDuplicateBindings` was written against that. With a pad binding added beside each
        /// keyboard one, the first-binding version checked the keyboard half and **silently
        /// stopped checking the half that had just been added**, which is the one class of
        /// failure this file's own class note says nobody notices.
        ///
        /// ⚠️ A COMPOSITE PART BELONGS TO ITS OWN DIRECTION, NOT TO THE WHOLE COMPOSITE. Asking
        /// for MoveForward's bindings must not return MoveBackward's key, and it must not return
        /// the pad's `leftStick` either: the stick is a plain binding on `Move` that no discrete
        /// direction owns, so it is correctly nobody's and correctly never clashes.
        /// </summary>
        public static List<int> ResolveBindingIndices(InputActionAsset asset, string actionName,
                                                      out InputAction action)
        {
            var found = new List<int>();
            action = null;
            if (asset == null || string.IsNullOrEmpty(actionName)) return found;

            bool isMovePart = actionName == "MoveForward" || actionName == "MoveBackward"
                              || actionName == "MoveLeft" || actionName == "MoveRight";

            if (isMovePart)
            {
                action = Find(asset, "Move");
                if (action == null) return found;

                string part = actionName == "MoveForward" ? "up" :
                              actionName == "MoveBackward" ? "down" :
                              actionName == "MoveLeft" ? "left" : "right";

                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var b = action.bindings[i];
                    if (b.isPartOfComposite
                        && string.Equals(b.name, part, System.StringComparison.OrdinalIgnoreCase))
                        found.Add(i);
                }

                return found;
            }

            action = Find(asset, actionName);
            if (action == null) return found;

            for (int i = 0; i < action.bindings.Count; i++)
            {
                var b = action.bindings[i];
                if (b.isComposite || b.isPartOfComposite) continue;
                if (string.IsNullOrEmpty(b.effectivePath)) continue;

                found.Add(i);
            }

            return found;
        }

        /// <summary>
        /// Every control bound twice across two different actions, as "path: ActionA, ActionB".
        /// Empty means the map is clean.
        ///
        /// ⚠️⚠️ THE UNIT OF THE RULE IS A CONTROL PATH, WHICH ALREADY CARRIES THE DEVICE, SO A
        /// KEYBOARD KEY AND A PAD BUTTON CAN NEVER COLLIDE WITH EACH OTHER. `&lt;Keyboard&gt;/f`
        /// and `&lt;Gamepad&gt;/buttonNorth` are different strings, and no press produces both.
        /// That is the honest reading of `CLAUDE.md` § 4's *"one control, one action, per
        /// CONTEXT"*: what the rule forbids is two actions firing off ONE press, and the device
        /// is part of what a press is. **The rule is therefore now checked per device and per
        /// context at once, with no new concept**, which is the same narrowing
        /// <see cref="SpectatorContext"/> records rather than a second exception beside it.
        /// </summary>
        public static List<string> FindDuplicateBindings(InputActionAsset asset)
        {
            var owners = new Dictionary<string, List<string>>();
            var clashes = new List<string>();
            if (asset == null) return clashes;

            foreach (string action in RebindableActions)
            {
                var indices = ResolveBindingIndices(asset, action, out var a);
                if (a == null) continue;

                foreach (int index in indices)
                {
                    string path = a.bindings[index].effectivePath;
                    if (string.IsNullOrEmpty(path)) continue;

                    if (!owners.TryGetValue(path, out var list))
                        owners[path] = list = new List<string>();

                    if (!list.Contains(action)) list.Add(action);
                }
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
        /// What <paramref name="action"/> is bound to on ONE device, or "-" when it is on none.
        ///
        /// ⚠️⚠️ `docs/VISION.md` § 3: *"Key labels come from the live binding, never from a
        /// literal. A screen that teaches the wrong key is worse than one that teaches none."*
        /// That rule is the reason this overload exists rather than a glyph table: a player on a
        /// pad must be told BUTTON SOUTH and a player on a keyboard SPACE, and both have to
        /// follow a rebind. A table keyed by action would be a second place to forget, which is
        /// the argument `HeroAbility.Glyph` won.
        ///
        /// ⚠️ IT RETURNS "-" RATHER THAN THE KEYBOARD BINDING WHEN A DEVICE HAS NONE. Falling
        /// back would print SPACE on a pad prompt for `ToggleFullscreen`, which has no pad
        /// binding on purpose (`ScreenInputCatalogue`), and teach a control that does not exist.
        /// </summary>
        public static string DisplayNameFor(InputActionAsset asset, string action, string device)
        {
            var indices = ResolveBindingIndices(asset, action, out var a);
            if (a == null) return "-";

            foreach (int index in indices)
            {
                string path = a.bindings[index].effectivePath;
                if (DeviceOf(path) != device) continue;

                return InputControlPath.ToHumanReadableString(
                    path, InputControlPath.HumanReadableStringOptions.OmitDevice);
            }

            return "-";
        }

        /// <summary>The device prefix a gamepad binding carries. One place, so a typo cannot hide.</summary>
        public const string GamepadDevice = "<Gamepad>";

        /// <summary>
        /// Rebind <paramref name="action"/> to <paramref name="control"/>.
        /// </summary>
        /// <summary>
        /// Bumped whenever anything in this class changes what a key is bound to.
        ///
        /// ⚠️⚠️ IT EXISTS SO A READER CAN CACHE A KEY LABEL WITHOUT GOING STALE. Resolving one
        /// costs a map lookup, an action lookup, `InputControlPath.ToHumanReadableString` and an
        /// upper-casing, and `Hud.KeyLabel` was paying all four on every frame of every prompt
        /// that names a key. Caching it is only safe if there is one number that changes the
        /// moment a binding does, and `docs/VISION.md` section 3 is why it has to be exact: a
        /// screen that teaches the wrong key is worse than one that teaches none.
        ///
        /// ⚠️ EVERY MUTATION GOES THROUGH THIS FILE, WHICH IS WHAT MAKES ONE COUNTER ENOUGH.
        /// `TryRebind`, `ResetAll` and `Load` are the three, and `ConvertedSettingsPanel` calls
        /// `Invalidate` for the override its rebind operation applies and then removes before
        /// the conflict check runs.
        /// </summary>
        public static int Revision { get; private set; }

        /// <summary>Tell every cached key label that it is out of date.</summary>
        public static void Invalidate() => Revision++;

        /// <summary>
        /// The device prefix of a control path, e.g. `&lt;Gamepad&gt;` from
        /// `&lt;Gamepad&gt;/buttonSouth`. Empty when the path does not name one.
        /// </summary>
        public static string DeviceOf(string path)
        {
            if (string.IsNullOrEmpty(path) || path[0] != '<') return "";

            int close = path.IndexOf('>');
            return close < 0 ? "" : path.Substring(0, close + 1);
        }

        public static string TryRebind(InputActionAsset asset, string action, InputControl control)
        {
            string path = control.path;
            string device = DeviceOf(path);

            foreach (string other in RebindableActions)
            {
                if (other == action) continue;

                // ⚠️ THE PANEL REFUSES A KEY ANOTHER action IN THE SAME CONTEXT HOLDS, and only
                // that. Refusing across contexts would make half the spectator rows unbindable to
                // the obvious key for no reason a player could ever work out from the screen.
                if (!ShareAContext(action, other)) continue;

                // ⚠️ EVERY BINDING OF THE OTHER ACTION, NOT ITS FIRST. This compared one binding
                // when an action only had one; with a pad binding beside each keyboard one, the
                // first-binding version let a player bind their pad's A button to two different
                // actions and refused nothing, because it was only ever looking at the keys.
                var otherIndices = ResolveBindingIndices(asset, other, out var otherAction);
                if (otherAction == null) continue;

                foreach (int otherIndex in otherIndices)
                    if (otherAction.bindings[otherIndex].effectivePath == path)
                        return LabelFor(other);
            }

            var indices = ResolveBindingIndices(asset, action, out var target);
            if (target == null || indices.Count == 0) return LabelFor(action);

            // ⚠️⚠️ THE OVERRIDE LANDS ON THE BINDING FOR THE DEVICE THE PLAYER JUST PRESSED, AND
            // TARGETING THE FIRST ONE WOULD HAVE SILENTLY DESTROYED THEIR KEYBOARD. Rebinding
            // SPRINT with a pad wrote the pad's path over binding 0, which is the KEY: the row
            // then read "Button South", the key stopped working, and there was no way back to it
            // except Reset All. One row, two devices, two bindings, and each is rebound on its own.
            int index = indices[0];

            foreach (int candidate in indices)
            {
                if (DeviceOf(target.bindings[candidate].effectivePath) != device) continue;

                index = candidate;
                break;
            }

            target.ApplyBindingOverride(index, path);
            Invalidate();
            Save(asset);
            return null;
        }

        /// <summary>Every override, back to the asset's own defaults.</summary>
        public static void ResetAll(InputActionAsset asset)
        {
            if (asset == null) return;

            asset.RemoveAllBindingOverrides();
            Invalidate();
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
            Invalidate();
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
