using System;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.InputLayer
{
    /// <summary>Where on the touch layer a control is drawn. One thumb owns each zone.</summary>
    public enum TouchZone
    {
        /// <summary>The left thumb's stick, and the small controls that ride beside it.</summary>
        MoveStick,

        /// <summary>The right thumb's main cluster: the verbs a player presses constantly.</summary>
        ActionCluster,

        /// <summary>The rail above the cluster. Hero Strike only; hidden in Classic.</summary>
        SkillRail,

        /// <summary>A small chip out of the thumbs' way, for things pressed between rounds.</summary>
        UtilityChip,
    }

    /// <summary>
    /// How big a touch control is drawn, in the three sizes this layer has.
    ///
    /// ⚠️ THE SMALLEST IS STILL ABOVE THE FLOOR. <see cref="TouchMetrics.MinTargetUnits"/> is the
    /// bound and <see cref="TouchMetrics.UnitsFor"/> is the arithmetic; a size is a preference
    /// inside the legal range, never a way under it.
    /// </summary>
    public enum TouchSize
    {
        Small,
        Medium,
        Large,
    }

    /// <summary>
    /// Everything one <see cref="Verb"/> needs in order to be pressable on all three devices.
    ///
    /// ⚠️⚠️ EVERY FIELD IS A CONSTRUCTOR PARAMETER WITH NO DEFAULT, WHICH IS THE WHOLE POINT.
    /// A verb cannot be given a keyboard binding and left without a thumb or a pad, because the
    /// only way to build one of these is to answer all EIGHT questions. This is
    /// <see cref="Abilities.HeroAbility.Glyph"/>'s argument applied to input: a lookup table
    /// keyed by verb is a second place to forget, and forgetting it compiles.
    /// </summary>
    public readonly struct VerbInput
    {
        public readonly Verb Verb;

        /// <summary>The action name in `Resources/TumbangPreso.inputactions`.</summary>
        public readonly string Action;

        /// <summary>An Input System control path, e.g. `&lt;Gamepad&gt;/buttonSouth`.</summary>
        public readonly string GamepadPath;

        public readonly TouchZone Zone;

        /// <summary>Order within the zone, low first. Unique per zone; a test asserts it.</summary>
        public readonly int Slot;

        public readonly TouchSize Size;

        /// <summary>
        /// What this control NAMES itself, for the layout customiser and the settings panel.
        ///
        /// ⚠️⚠️ IT IS NO LONGER WHAT THE BUTTON DRAWS, AND THAT WAS THE WHOLE BUG. 🧑 2026-09-03,
        /// off the Android build: *"why the fuck does it have keybinds theres no keys in mobile"*,
        /// *"ive never seen a mobile game say GRAB or lunge, usually it has an intuitive icon for
        /// it or the skill icon"*. Three of these strings were `"Q"`, `"E"` and `"ULT"`: the
        /// names of keys on a keyboard the device does not have, painted on the one surface in
        /// the game that exists BECAUSE there is no keyboard. See <see cref="Glyph"/>.
        ///
        /// ⚠️ IT IS KEPT RATHER THAN DELETED BECAUSE A WORD IS RIGHT IN THE ONE PLACE A PICTURE
        /// is not: `TouchLayoutScreen` is a customiser where the player drags controls around and
        /// has to know which one they are holding, and `docs/TODO.md` § 125.11 records that screen
        /// as something he asked for by name.
        /// </summary>
        public readonly string TouchLabel;

        /// <summary>
        /// What the touch button DRAWS.
        ///
        /// ⚠️⚠️ A CONSTRUCTOR PARAMETER WITH NO DEFAULT, LIKE EVERY OTHER FIELD HERE, AND FOR THE
        /// REASON THE CLASS NOTE BELOW GIVES. A verb that reached a phone without somebody
        /// deciding what it looks like is exactly what shipped: the type could only hold a
        /// string, so the answer was always going to be a word, and for the hero slots the word
        /// was the keyboard key. **The type is the fix.** `UI.VerbIcons` bakes them.
        ///
        /// ⚠️ THE SKILL RAIL PREFERS THE ABILITY'S OWN ICON OVER THIS. `TouchHud` resolves the
        /// live `HeroKit` and draws `AbilityIcons.For(ability.Glyph)`, so a phone shows the same
        /// eighteen bespoke pictures the deck and character select show (`docs/VISION.md` § 3's
        /// three layers, which *"must stay in step"*). The glyph here is what a seat with no kit
        /// falls back to.
        /// </summary>
        public readonly UI.VerbGlyph Glyph;

        public VerbInput(Verb verb, string action, string gamepadPath,
                         TouchZone zone, int slot, TouchSize size, string touchLabel,
                         UI.VerbGlyph glyph)
        {
            Verb = verb;
            Action = action;
            GamepadPath = gamepadPath;
            Zone = zone;
            Slot = slot;
            Size = size;
            TouchLabel = touchLabel;
            Glyph = glyph;
        }
    }

    /// <summary>
    /// The one table that says how every verb is reached on a keyboard, a pad and a thumb.
    ///
    /// ⚠️⚠️ ADDING A `Verb` WITHOUT AN ENTRY HERE IS A COMPILE ERROR, AND THAT IS ARRANGED
    /// RATHER THAN HOPED FOR. <see cref="For"/> is a switch EXPRESSION with no discard arm, so
    /// a new enum member makes it non-exhaustive; `Assets/TumbangPreso/Runtime/csc.rsp` turns
    /// the resulting CS8509 into an error for this assembly. Delete that file and this whole
    /// section degrades to a warning nobody reads.
    ///
    /// ⚠️⚠️ THE REASON IT IS ENFORCED RATHER THAN DOCUMENTED IS THAT THREE PROBES HAVE ALREADY
    /// BEEN LEFT BEHIND BY A MOVE. `docs/TODO.md` § 96 (a door nobody found), § 114
    /// (`PlayerNameplate` no longer installed while its probe still drove it) and § 124.11 (a
    /// probe knocking on a screen that had moved) are all the same shape: a rule kept by a human
    /// remembering it. 🧑, twice: *"make that shit future proof and to update mobile and
    /// controller version every time we change ui or some shit"*, then *"anytime we add a
    /// feature, make sure all controller and mobile is considered"*. A checklist is what failed.
    ///
    /// ⚠️ THE GAMEPAD PATHS OBEY `CLAUDE.md` § 4 PER CONTEXT, NOT GLOBALLY. A keyboard control
    /// and a pad control are different strings and can never both be the same press, so the rule
    /// that matters is that no two actions inside ONE context share ONE device's control.
    /// `Settings.Rebinding.FindDuplicateBindings` checks exactly that, per device, per context.
    /// </summary>
    public static class InputCatalogue
    {
        /// <summary>
        /// ⚠️⚠️ NO DISCARD ARM. That is not an oversight to tidy up: the missing `_ =>` is what
        /// makes a new verb fail to compile until somebody has decided where a thumb and a pad
        /// reach it. See the class note.
        /// </summary>
        public static VerbInput For(Verb verb) => verb switch
        {
            // ---- the left thumb, and what rides beside it -------------------------------
            Verb.Sprint => new VerbInput(
                Verb.Sprint, "Sprint", "<Gamepad>/leftStickPress",
                TouchZone.MoveStick, 0, TouchSize.Small, "RUN", UI.VerbGlyph.Sprint),

            // ---- the right thumb's constant cluster --------------------------------------
            //
            // ⚠️ THROW IS THE RIGHT TRIGGER AND THE BIGGEST TOUCH TARGET, because it is the one
            // verb that is HELD. `Carrier` charges while it is down and releases on the edge, so
            // an analogue trigger and a finger that stays put are the same gesture. A face button
            // would work and would put the charge on the thumb that also steers the camera.
            Verb.SpecialAbility => new VerbInput(
                Verb.SpecialAbility, "SpecialAbility", "<Gamepad>/rightTrigger",
                TouchZone.ActionCluster, 0, TouchSize.Large, "THROW", UI.VerbGlyph.ThrowSlipper),

            // ⚠️ GRAB IS CONTEXTUAL AND STAYS ONE CONTROL. Tap picks up, tap with nothing in
            // reach shoves, hold as the taya runs the lata reset. `PlayerInputReader`'s note is
            // why that is resolved downstream: one key, one action, several jobs decided by the
            // world. A second touch button per job would be three controls for one verb.
            Verb.Grab => new VerbInput(
                Verb.Grab, "Grab", "<Gamepad>/buttonWest",
                TouchZone.ActionCluster, 1, TouchSize.Medium, "GRAB", UI.VerbGlyph.Hand),

            Verb.Jump => new VerbInput(
                Verb.Jump, "Jump", "<Gamepad>/buttonSouth",
                TouchZone.ActionCluster, 2, TouchSize.Medium, "JUMP", UI.VerbGlyph.Jump),

            // ⚠️ THE TAYA'S ONLY SCORING VERB GETS THE OTHER TRIGGER, so the two verbs that
            // decide a round sit under the two fingers that are not steering.
            Verb.Lunge => new VerbInput(
                Verb.Lunge, "Lunge", "<Gamepad>/leftTrigger",
                TouchZone.ActionCluster, 3, TouchSize.Medium, "LUNGE", UI.VerbGlyph.Lunge),

            // ---- Hero Strike only. The rail hides itself in Classic ----------------------
            Verb.Skill1 => new VerbInput(
                Verb.Skill1, "Skill1", "<Gamepad>/leftShoulder",
                TouchZone.SkillRail, 0, TouchSize.Medium, "SKILL 1", UI.VerbGlyph.SkillPrimary),

            Verb.Skill2 => new VerbInput(
                Verb.Skill2, "Skill2", "<Gamepad>/rightShoulder",
                TouchZone.SkillRail, 1, TouchSize.Medium, "SKILL 2", UI.VerbGlyph.SkillSecondary),

            Verb.Ultimate => new VerbInput(
                Verb.Ultimate, "Ultimate", "<Gamepad>/buttonNorth",
                TouchZone.SkillRail, 2, TouchSize.Medium, "ULTIMATE", UI.VerbGlyph.Ultimate),

            // ---- pressed between rounds, out of the thumbs' way --------------------------
            Verb.EmoteWheel => new VerbInput(
                Verb.EmoteWheel, "EmoteWheel", "<Gamepad>/dpad/up",
                TouchZone.UtilityChip, 0, TouchSize.Small, "EMOTE", UI.VerbGlyph.Emote),
        };

        /// <summary>Every verb's entry, in enum order.</summary>
        public static IReadOnlyList<VerbInput> All => Table;

        private static readonly VerbInput[] Table = BuildTable();

        private static VerbInput[] BuildTable()
        {
            var verbs = (Verb[])Enum.GetValues(typeof(Verb));
            var table = new VerbInput[verbs.Length];

            for (int i = 0; i < verbs.Length; i++) table[i] = For(verbs[i]);

            return table;
        }

        /// <summary>The entries drawn in one zone, already in slot order.</summary>
        public static List<VerbInput> InZone(TouchZone zone)
        {
            var found = new List<VerbInput>();

            foreach (var entry in Table)
                if (entry.Zone == zone) found.Add(entry);

            found.Sort((a, b) => a.Slot.CompareTo(b.Slot));
            return found;
        }

        /// <summary>
        /// The verb an action name belongs to, or null for the screen actions.
        ///
        /// ⚠️ THE SCREEN ACTIONS ARE NOT VERBS AND DELIBERATELY LIVE IN A SECOND TABLE. A verb
        /// moves a body; READY UP, HIDE HUD and the spectator set do not, they have no touch
        /// button in the thumb zones, and folding them in here would mean inventing a
        /// <see cref="TouchZone"/> that means "nowhere" — which is the escape hatch that makes
        /// the compile gate above meaningless. <see cref="ScreenInputCatalogue"/> holds them and
        /// a test asserts it covers every row of `Rebinding.RebindableActions`.
        /// </summary>
        public static Verb? VerbForAction(string action)
        {
            foreach (var entry in Table)
                if (string.Equals(entry.Action, action, StringComparison.Ordinal))
                    return entry.Verb;

            return null;
        }

        /// <summary>The gamepad path bound to an action, verb or screen action alike.</summary>
        public static string GamepadPathFor(string action)
        {
            foreach (var entry in Table)
                if (string.Equals(entry.Action, action, StringComparison.Ordinal))
                    return entry.GamepadPath;

            return ScreenInputCatalogue.GamepadPathFor(action);
        }
    }

    /// <summary>
    /// The actions that are not verbs: the round, the screen, and the spectator camera.
    ///
    /// ⚠️⚠️ A ROW HERE MAY BE `null`, AND THAT IS AN ANSWER RATHER THAN AN OMISSION. Fullscreen
    /// is a desktop window action: a pad has no business toggling it and a phone has no window to
    /// toggle. The null is written down, `EveryRebindableActionDeclaresADeviceAnswer` asserts the
    /// table covers `Rebinding.RebindableActions` exactly, so a NEW action still cannot appear
    /// without somebody answering for it. What is forbidden is silence, not "no".
    /// </summary>
    public static class ScreenInputCatalogue
    {
        /// <summary>Action name, gamepad path (null for "deliberately none"), and why.</summary>
        public static readonly (string Action, string GamepadPath, string Note)[] Rows =
        {
            // ⚠️ THE PEKTUS CURVE IS THE D-PAD, NOT THE RIGHT STICK. The right stick is the
            // camera, and the curve is held WHILE the throw charges and WHILE the player is
            // moving: on a pad that is already two sticks and a trigger, so the curve has to go
            // somewhere the thumbs are not. `Rebinding`'s note has the keyboard half of the same
            // argument, which is why the keys are Z and C rather than the arrows.
            ("CurveLeft", "<Gamepad>/dpad/left", "the throw hand is on the trigger"),
            ("CurveRight", "<Gamepad>/dpad/right", "the throw hand is on the trigger"),

            ("ReadyUp", "<Gamepad>/buttonEast", "B is the ready in every lobby on a console"),

            // ⚠️⚠️ START, WHICH IS WHAT START HAS MEANT SINCE 1985, AND `SpectatorPause` MOVED
            // OFF IT RATHER THAN SHARING IT. The two are NOT separable by context: `PausePanel`
            // opens for a spectator too and renames itself BROADCAST MENU, so one press of Start
            // would have opened the menu AND fired the tactical pause underneath it, which is
            // the exact shape of the R collision `Settings.Rebinding`'s class note records
            // (*"both sides of it are live in the same context"*). That note also says which one
            // moves: *"when two must part, the one with fewer readers moves."* Every player in
            // the game reaches the pause menu; the tactical pause is an operator key.
            ("Pause", "<Gamepad>/start", "start has opened the menu since 1985"),
            ("CleanFeed", "<Gamepad>/dpad/down", "pressed once, between rounds"),
            ("AbilityInfo", "<Gamepad>/select", "held to read, never in a fight"),

            // ⚠️ NO PAD BINDING, ON PURPOSE. A gamepad on a desktop still has a window, but the
            // player who reaches for a pad is not the player who alt-tabs, and a face button
            // spent on a window toggle is a face button not spent on a verb. On Android there is
            // no window at all: `GameSettings` hides the row.
            ("ToggleFullscreen", null, "a desktop window action; a phone has no window"),

            // § SPECTATOR CAMERA. Its own context, so these may reuse a gameplay control:
            // a spectator has no body and no `CharacterMotor`, so the two sets can never both
            // fire. `Rebinding.SpectatorContext` carries the full reasoning.
            ("SpectatorAutopilot", "<Gamepad>/leftShoulder", "spectator context"),
            ("SpectatorCycleTarget", "<Gamepad>/rightShoulder", "spectator context"),
            ("SpectatorFreeFly", "<Gamepad>/buttonNorth", "spectator context"),
            ("SpectatorPov", "<Gamepad>/buttonWest", "spectator context"),
            ("SpectatorDown", "<Gamepad>/leftTrigger", "spectator context"),
            ("SpectatorMark", "<Gamepad>/dpad/up", "spectator context"),
            ("SpectatorRecall", "<Gamepad>/dpad/down", "spectator context"),
            // ⚠️ A, BECAUSE IT WAS START UNTIL 2026-09-04 AND START IS THE PAUSE MENU NOW.
            // See the `Pause` row above for why the two could not share it. A is free in the
            // spectator context and is the one button an operator's thumb is already resting on.
            ("SpectatorPause", "<Gamepad>/buttonSouth", "spectator context"),
            ("SpectatorReplay", "<Gamepad>/dpad/left", "spectator context"),
            ("SpectatorControls", "<Gamepad>/dpad/right", "spectator context"),
        };

        public static string GamepadPathFor(string action)
        {
            foreach (var row in Rows)
                if (string.Equals(row.Action, action, StringComparison.Ordinal))
                    return row.GamepadPath;

            return null;
        }

        /// <summary>True when this action has been answered for, including a deliberate "no".</summary>
        public static bool Declares(string action)
        {
            foreach (var row in Rows)
                if (string.Equals(row.Action, action, StringComparison.Ordinal)) return true;

            return false;
        }
    }

    /// <summary>
    /// How big a thumb target has to be, and the arithmetic behind the number.
    ///
    /// ⚠️⚠️ THE FLOOR IS STATED IN CANVAS REFERENCE UNITS, NOT IN PIXELS, FOR THE REASON
    /// `AspectRatioProbes` GIVES ABOUT FONT SIZE. A physical-pixel floor is the same assertion
    /// said badly: it passes on a 1440p phone and fails on a 720p one for a control nobody
    /// changed, so the failure names a device instead of naming the control.
    ///
    /// **The arithmetic.** The accessibility floor everybody uses is a 9 mm target, which is
    /// about 48 density-independent pixels. `AspectSafeCanvas` matches on HEIGHT against a
    /// 1080-unit reference, so one reference unit is `screenHeightPx / 1080`. The worst phone
    /// this is likely to meet in landscape is 720 px tall at about 320 dpi, where 48 dp is
    /// 96 physical px, and 96 px on a 720-tall screen is `96 * 1080 / 720` = **144 units**.
    /// That is the number, and it is deliberately measured against the WORST case rather than
    /// against a 1080p phone, because a control that is big enough on a good screen and too
    /// small on a cheap one is a control that fails only for the players least able to report it.
    /// </summary>
    public static class TouchMetrics
    {
        /// <summary>The smallest a touch target may be, in canvas reference units. See the note.</summary>
        public const float MinTargetUnits = 144.0f;

        /// <summary>The gap between two touch targets, so a thumb cannot bridge both.</summary>
        public const float MinGapUnits = 24.0f;

        public static float UnitsFor(TouchSize size)
        {
            switch (size)
            {
                case TouchSize.Large: return MinTargetUnits * 1.55f; // 223: the held throw
                case TouchSize.Medium: return MinTargetUnits * 1.20f; // 173
                default: return MinTargetUnits;
            }
        }
    }
}
