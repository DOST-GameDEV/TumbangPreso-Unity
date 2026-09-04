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
    }
}
