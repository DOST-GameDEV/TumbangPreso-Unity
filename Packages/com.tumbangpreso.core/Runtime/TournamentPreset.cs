namespace TumbangPreso.Core
{
    /// <summary>
    /// The one canonical tournament rule set, and the reasons a live match is not playing it.
    ///
    /// ⚠️⚠️ WHY THIS EXISTS: A "MOSTLY TOURNAMENT" MATCH IS THE FAILURE MODE, NOT A MISSING
    /// SETTING. Every switch this game has for testing is reachable from a menu or survives in a
    /// static field across a scene change, and an event operator starting the next match inherits
    /// whatever the last one left behind. There was no single place that said what a nationals
    /// match IS, so "is this configured correctly" was answered by reading eight fields in six
    /// files and remembering which ones matter. That is the shape of question that gets answered
    /// wrongly at 9 a.m. in a hall with a queue behind you.
    ///
    /// ⚠️ CLASSIC IS THE TOURNAMENT RULESET. `docs/VISION.md` § 1.1: *"CLASSIC IS THE TOURNAMENT
    /// RULESET UNTIL SOMEONE SAYS OTHERWISE. Hero Strike is the one being grown toward that."*
    /// This file is that sentence as a value, so changing the ruling is a one-line edit with a
    /// test failure attached rather than an argument about what the game was doing.
    ///
    /// ⚠️⚠️ AND IT COPIES NO NUMBER. `Rounds` asks `MatchRules.RoundCountFor`, `RoundSeconds` asks
    /// `Balance.RoundTime`, `Tsinelas` asks `CustomGameRules.StartingTsinelas`. `CustomGameRules
    /// .Defaults` makes exactly this argument one level up and this file is the same discipline:
    /// a preset that restates the shipped numbers is a second place for them to drift, and the
    /// drift is silent because both copies look authoritative. `Design.md`'s opening rule.
    ///
    /// ⚠️ THE MODIFIER HALF IS NAMES RATHER THAN VALUES, AND THAT IS DELIBERATE. `PracticeSandbox`,
    /// `GameLaunch.AllBots` and `MatchInstaller.PreviewOnly` are Unity-side statics that this
    /// package may never reference (`CLAUDE.md` § 4: the core must never acquire a `UnityEngine`
    /// reference). So the core owns the LIST of what must be off and the reason each one is on it,
    /// and the Unity side reports the live values against it. The list is the part that gets
    /// forgotten; reading a bool is not.
    /// </summary>
    public static class TournamentPreset
    {
        /// <summary>
        /// The mode a nationals match is played in.
        ///
        /// ⚠️ CHANGING THIS IS A TOURNAMENT RULING AND NOT A CODE CHANGE. `docs/VISION.md` § 1.1
        /// is the authority and `TournamentPresetTests` asserts the two stay in step, so a session
        /// that flips it without the document failing is impossible.
        /// </summary>
        public const GameMode Mode = GameMode.Classic;

        /// <summary>
        /// The rule set a tournament match starts from, built fresh every call.
        ///
        /// ⚠️⚠️ IT RETURNS A NEW OBJECT EVERY TIME AND MUST KEEP DOING SO. A shared static
        /// `CustomRules` is a mutable object handed to a lobby screen, and the screen edits it in
        /// place: one custom match with a changed round count would then have permanently edited
        /// what "tournament" means for the rest of the process. That is exactly the class of
        /// leftover this whole file exists to make impossible.
        /// </summary>
        public static CustomRules Rules()
        {
            var rules = CustomGameRules.Defaults(Mode);

            // ⚠️ EVERY FIELD IS RESTATED EVEN WHERE `Defaults` ALREADY AGREES, and that is the
            // point rather than redundancy. `Defaults` answers "what does a custom lobby open
            // on"; this answers "what is a tournament match". They agree today. If somebody
            // changes what a custom lobby opens on, the tournament must not move with it, and
            // `TournamentPresetTests.TheTournamentPresetIsPinnedFieldByField` is what notices.
            rules.Mode = Mode;
            rules.Format = MatchFormat.Standard;
            rules.Rounds = MatchRules.RoundCountFor(Mode);
            rules.RoundSeconds = (int)Balance.RoundTime;

            // 0 means "play every round". A score target ends a match early, which is a format
            // decision no tournament bracket has taken.
            rules.ScoreTarget = 0;

            rules.Tsinelas = CustomGameRules.StartingTsinelas;

            // ⚠️ FOUR HUMANS. A bot in a bracket match is a forfeit that scores.
            rules.Bots = 0;
            rules.BotDifficulty = (int)Difficulty.Normal;

            // ⚠️ `Private` IS LEFT AT THE SHIPPED DEFAULT ON PURPOSE. Whether a bracket match is
            // password-locked is a venue decision (one laptop per station, or one shared room),
            // and inventing an answer here would put a password prompt in front of an operator
            // who has not been given one. `Attention.md` carries the ask.
            rules.Private = false;
            rules.Password = "";

            return rules;
        }

        /// <summary>
        /// Whether a rule set is the tournament one, field by field, with the field named.
        ///
        /// ⚠️ IT ANSWERS A SENTENCE RATHER THAN A BOOL, for `CustomGameRules.Refusal`'s reason:
        /// "this is not a tournament match" with nothing after it sends an operator to read eight
        /// fields, which is the situation this file replaces. Empty string means it is legal.
        /// </summary>
        public static string RulesRefusal(CustomRules rules)
        {
            if (rules == null) return "There are no rules to check.";

            var want = Rules();

            if (rules.Mode != want.Mode)
                return $"mode is {rules.Mode}, tournament is {want.Mode}";
            if (rules.Format != want.Format)
                return $"format is {rules.Format}, tournament is {want.Format}";
            if (rules.Rounds != want.Rounds)
                return $"rounds is {rules.Rounds}, tournament is {want.Rounds}";
            if (rules.RoundSeconds != want.RoundSeconds)
                return $"round length is {rules.RoundSeconds}s, tournament is {want.RoundSeconds}s";
            if (rules.ScoreTarget != want.ScoreTarget)
                return $"score target is {rules.ScoreTarget}, tournament plays every round";
            if (rules.Tsinelas != want.Tsinelas)
                return $"tsinelas is {rules.Tsinelas}, tournament is {want.Tsinelas}";
            if (rules.Bots != want.Bots)
                return $"the lobby is filling {rules.Bots} seat(s) with bots";

            return "";
        }

        // -------------------------------------------------------------------
        // § THE MODIFIERS THAT MUST BE OFF
        //
        // ⚠️⚠️ THIS LIST IS THE DELIVERABLE. Each row is a switch that exists for a good reason,
        // is reachable during ordinary development, and would change a match without looking like
        // it had. The Unity side reads the live value for each name and hands the pairs back;
        // this file owns which names matter and why, because the name is the part somebody forgets
        // to add when they invent switch number nine.
        // -------------------------------------------------------------------

        /// <summary>One switch that must be off in a tournament match, and why it is on the list.</summary>
        public readonly struct Modifier
        {
            public readonly string Name;
            public readonly string Why;

            public Modifier(string name, string why)
            {
                Name = name;
                Why = why;
            }
        }

        /// <summary>
        /// Every developer or practice switch a tournament match must not be carrying.
        ///
        /// ⚠️ A NAME MISSING FROM THIS LIST IS THE WHOLE BUG CLASS, so adding a switch to the game
        /// and not to this list is what `TournamentPresetTests.EveryKnownModifierIsNamed` exists to
        /// make loud. It reads the list against the Unity side's reporter, so the two cannot drift
        /// in either direction.
        /// </summary>
        public static readonly Modifier[] Modifiers =
        {
            new Modifier("PracticeSandbox.Wanted",
                "No cooldowns, no charge cost and casting during the warm-up. It is already " +
                "fail-closed on `!NetAuthority.IsNetworked`, so it cannot reach a networked " +
                "match; it is here because a lit NO COOLDOWNS toggle in a tournament room is a " +
                "HUD disagreeing with the game, and because the guard is the thing under test."),

            new Modifier("GameLaunch.AllBots",
                "Fills every seat with a bot, including the one a human would take. It exists " +
                "for `BotBehaviourProbe`. A bracket match that starts with it set has four bots " +
                "and no players and looks like a hung lobby."),

            new Modifier("GameLaunch.Spectator",
                "Holds no seat and spawns no character. A player who was spectating the previous " +
                "match and is now competing must not walk into the next one seatless."),

            new Modifier("GameLaunch.GuidedTutorial",
                "Installs the guided route instead of an ordinary match. Local-only and Hero " +
                "Strike, so it is the wrong mode as well as the wrong match."),

            new Modifier("MatchInstaller.PreviewOnly",
                "Builds the arena without the match. A preview surface left set means a round " +
                "that never starts, which reads as a crash rather than as a setting."),

            new Modifier("AIController.BotsEnabled",
                "⚠️ THE ONE ROW WHOSE SAFE VALUE IS **true**. Turning bots off does not make a " +
                "match more human, it makes the seats nobody filled inert. It is on this list so " +
                "a debugging session that switched it off cannot silently ship that state."),

            new Modifier("TouchHud.ForceVisible",
                "Paints the thumb layer over a desktop match. Harmless to the rules and " +
                "immediately visible on a projector, which is exactly the kind of thing nobody " +
                "wants to discover on the stream."),

            new Modifier("SpectatorCamera.ProbeReplayRequest",
                "A probe hook that drives the replay. A stuck request replays over live play."),
        };

        /// <summary>The safe value for a modifier. Everything is false except the one row that says so.</summary>
        public static bool SafeValue(string name) => name == "AIController.BotsEnabled";

        // -------------------------------------------------------------------
        // § THE OTHER LIST, AND WHY THERE HAS TO BE ONE
        //
        // ⚠️⚠️ THE AUDIT ABOVE PROVED TWO LISTS AGREED WITH EACH OTHER AND COULD NOT SEE SWITCH
        // NUMBER NINE. `tools/audit_tournament_defaults.py` checked that every name in
        // `Modifiers` had a case in `TournamentGuard`, and that every case was a listed name.
        // Both directions, both green, and a settable static added tomorrow to neither file is
        // invisible to both of them: it is not on the roster, so no case is missing, and it has no
        // accessor, so no accessor is dead. **The whole failure mode that roster exists for lives
        // exactly in that blind spot.** `docs/TODO.md` § 145.3.
        //
        // ⚠️⚠️ SO THE AUDIT DISCOVERS CANDIDATES AND THIS IS WHERE ONE IS DISMISSED. It sweeps
        // `Assets/TumbangPreso/Runtime` for every SETTABLE public or internal `static bool` and
        // every `-tp-` launch switch, and requires each to be on `Modifiers` or on this list. A
        // new one on neither fails the audit, which is `CLAUDE.md` § 4a's construction argument:
        // *"a lookup table keyed by id is a second place to forget, and forgetting it compiles."*
        // Here forgetting it does not compile a build past the gate.
        //
        // ⚠️ SETTABLE IS THE FILTER AND IT IS WHAT KEEPS THIS SHORT RATHER THAN NOISY. There are
        // forty-one static bools in the runtime and twenty-eight of them are derived properties
        // (`NetAuthority.IsHost`, `Panel.AnyOpen`, `PracticeSandbox.Active`): nothing outside can
        // write one, so nothing can LEAVE one set, which is the entire hazard. A gate that listed
        // all forty-one would be the *"giant noisy regex gate developers learn to ignore"* the
        // brief warns against.
        //
        // ⚠️ AND A REASON IS REQUIRED, LIKE EVERY ROW ON `Modifiers`. "It is fine" is how a row
        // gets deleted in a tidy-up by somebody who cannot tell whether it was ever thought about.
        // -------------------------------------------------------------------

        /// <summary>
        /// A switch the audit will find and that a tournament match does not care about, with the
        /// reason it does not.
        /// </summary>
        public static readonly Modifier[] NotModifiers =
        {
            new Modifier("SceneFlow.Networked",
                "Not a modifier, a fact: whether this process is in a networked session. It is " +
                "written by the start paths and read by the lobby to decide whether it is a lobby " +
                "at all. Clearing it before a tournament match would put an online room on the " +
                "PRACTICE tab."),

            new Modifier("SceneFlow.BootedThroughSplash",
                "Boot bookkeeping. It stops the splash playing a second time when the player " +
                "returns to the title, and has no effect on a match in any state."),

            new Modifier("SceneFlow.LoginStepOffered",
                "Boot bookkeeping. It records that the sign-in screen has already been offered " +
                "once this session, so a player who declined is not asked again. Resetting it " +
                "would put an account prompt in front of an operator mid-bracket."),

            new Modifier("TouchInput.Active",
                "A DEVICE FACT rather than a setting: it is true when the thumb layer is driving, " +
                "which is decided by what the player last touched. Forcing it either way in a " +
                "tournament would take the controls off a phone or paint them onto a desktop. " +
                "`TouchHud.ForceVisible` is the developer override and IS on the modifier list."),

            new Modifier("Rumble.Enabled",
                "A player's own accessibility and comfort setting, restored from their settings " +
                "file every launch. A tournament that reset it would be overriding a preference " +
                "somebody set for a reason, and it cannot change what happens in a match."),

            new Modifier("-tp-host",
                "A launch route rather than a modifier: it hosts on a port. An operator station " +
                "launched this way is doing exactly what it was told to."),

            new Modifier("-tp-join",
                "A launch route: joins an address. Same argument as -tp-host."),

            new Modifier("-tp-dedicated",
                "A launch route: starts a seatless referee. `Attention.md` § 16.2 is the entry " +
                "and a referee is a legitimate tournament configuration rather than a leftover."),

            new Modifier("-tp-lobby",
                "A launch route: opens a relay lobby. Same argument as -tp-host."),

            new Modifier("-tp-lobbyjoin",
                "A launch route: joins a relay lobby by code. Same argument as -tp-host."),

            new Modifier("-tp-lobbyport",
                "A parameter of the two lobby routes above, not a switch of its own."),

            new Modifier("-tp-lobbychat",
                "A diagnostic that prints lobby chat traffic. It reads state and writes none."),

            new Modifier("-tp-profile",
                "Names which settings and career folder to use, so two processes on one machine " +
                "do not fight. It cannot change a rule."),

            new Modifier("-tp-identity",
                "Prints `BuildIdentity` and quits. It never reaches a match, which is the point " +
                "of it: an operator at a venue asking what a build is."),

            new Modifier("-tp-netreport",
                "Writes `NetStateReport` to a file after a delay. It observes and then quits."),

            new Modifier("-tp-netseconds",
                "How long -tp-netreport waits. A parameter of the row above."),

            new Modifier("-tp-report",
                "Writes a diagnostic report to a file. Observation only."),

            new Modifier("-tp-bundle",
                "Writes a failure bundle to a file. Observation only, and `FailureBundle` is " +
                "explicit that it carries no secrets."),

            new Modifier("-tp-framecap",
                "Sets the frame cap. It changes how fast the picture updates and not what the " +
                "rules do; a venue machine capped for a capture card is a legitimate setup."),

            new Modifier("-tp-map",
                "Chooses the arena to load. Which map a bracket match is played on is an " +
                "operator's decision and a lobby setting, not a developer override."),
        };

        /// <summary>
        /// Every switch the tournament audit knows about, in either direction.
        ///
        /// ⚠️ THE AUDIT READS THIS TO ANSWER "HAS ANYBODY THOUGHT ABOUT THIS ONE". A name on
        /// neither list has not been, which is the finding.
        /// </summary>
        public static bool IsAccountedFor(string name)
        {
            foreach (var m in Modifiers) if (m.Name == name) return true;
            foreach (var m in NotModifiers) if (m.Name == name) return true;
            return false;
        }

        // ⚠️⚠️ THE LAUNCH SWITCHES THAT **ARE** MODIFIERS ARE ON `Modifiers` THROUGH THE STATIC
        // THEY SET, NOT BY THEIR OWN NAME, and that is deliberate: `-tp-allbots` sets
        // `GameLaunch.AllBots`, `-tp-botmatch` sets it too, and `-tp-autostart` and
        // `-tp-autorematch` drive gates rather than leaving a flag behind. Listing a switch AND
        // the static it writes would be two rows for one hazard, which is how a roster starts
        // disagreeing with itself. `LaunchSwitchModifier` maps the ones that do.

        /// <summary>
        /// The static a gameplay-affecting launch switch leaves set, or "" when it leaves nothing.
        ///
        /// ⚠️ A SWITCH THAT LEAVES NOTHING BEHIND IS STILL ACCOUNTED FOR, through
        /// <see cref="NotModifiers"/> or through this map answering a listed static. What the
        /// audit refuses is a switch nobody has said either sentence about.
        /// </summary>
        public static string LaunchSwitchModifier(string switchName)
        {
            switch (switchName)
            {
                case "-tp-allbots": return "GameLaunch.AllBots";
                case "-tp-botmatch": return "GameLaunch.AllBots";

                // ⚠️⚠️ THESE TWO PRESS A BUTTON AND LEAVE NO FLAG, WHICH IS WHY THEY ARE HERE
                // RATHER THAN ON `NotModifiers`. `-tp-autostart` presses through the ready gate
                // and `-tp-autorematch` presses the rematch vote; both are one-shot actions taken
                // at a moment rather than state a later match inherits. They are named so that
                // adding a THIRD automation switch has to be a decision.
                case "-tp-autostart": return "";
                case "-tp-autorematch": return "";
                default: return null;
            }
        }
    }
}
