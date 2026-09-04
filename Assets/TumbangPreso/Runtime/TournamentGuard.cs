using System.Collections.Generic;
using System.Text;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The Unity half of <see cref="TournamentPreset"/>: read the live switches, say what is
    /// wrong in a sentence, and put the machine into a known state.
    ///
    /// ⚠️⚠️ THE FAILURE THIS EXISTS FOR IS NOT A MISSING SETTING, IT IS AN INHERITED ONE. Every
    /// switch this game has for testing lives in a `static` field, and a static field survives a
    /// scene change by definition. An operator who ran a bot match, opened practice to check a
    /// cooldown, or watched the previous round as a spectator, and then started the next bracket
    /// match, is starting it with whatever those left behind. Nothing anywhere asked.
    ///
    /// ⚠️⚠️ THE LIST OF WHAT MATTERS LIVES IN THE CORE AND THE ACCESSORS LIVE HERE, and that split
    /// is the whole design. `Packages/com.tumbangpreso.core/` may never reference `UnityEngine`
    /// (`CLAUDE.md` § 4), so it cannot read `GameLaunch.AllBots`; what it CAN own is the roster of
    /// names and the reason each is on it, which is the half that gets forgotten. Reading a bool
    /// is not the hard part. Remembering that switch number nine exists is.
    ///
    /// ⚠️⚠️ AND IT FAILS CLOSED ON A NAME IT DOES NOT KNOW. If somebody adds a row to
    /// `TournamentPreset.Modifiers` and not to <see cref="Read"/>, <see cref="Refusal"/> reports
    /// the name as unresolved rather than skipping it, so the gap is loud instead of silent. That
    /// is `InputLayer.InputCatalogue`'s no-discard-arm argument (`CLAUDE.md` § 4a) applied to a
    /// lookup that cannot be a switch expression because it is keyed by string.
    /// `TournamentGuardTests` asserts every name resolves.
    ///
    /// ⚠️ IT DOES NOT POLICE A PRACTICE SESSION. Every switch below is legitimate somewhere; the
    /// claim is only that a TOURNAMENT match starts from a known state. <see cref="Apply"/> is
    /// called on the way into one, not on every launch.
    /// </summary>
    public static class TournamentGuard
    {
        /// <summary>One live switch, its current value, and what a tournament needs it to be.</summary>
        public readonly struct Reading
        {
            public readonly string Name;
            public readonly bool Value;
            public readonly bool Safe;
            public readonly bool Known;
            public readonly string Why;

            public Reading(string name, bool value, bool safe, bool known, string why)
            {
                Name = name;
                Value = value;
                Safe = safe;
                Known = known;
                Why = why;
            }

            public bool IsSafe => Known && Value == Safe;
        }

        /// <summary>
        /// The live value of one named modifier.
        ///
        /// ⚠️ `known: false` IS THE FAIL-CLOSED ANSWER and is not the same as `false`. A switch
        /// this method has never heard of must not read as "off".
        /// </summary>
        private static bool Read(string name, out bool known)
        {
            known = true;

            switch (name)
            {
                // ⚠️ `Wanted` RATHER THAN `Active`, DELIBERATELY. `Active` ands with
                // `!NetAuthority.IsNetworked`, so in a networked match it answers false no matter
                // what the button says, and reading it here would report a clean sheet over a lit
                // toggle. The guard is the thing being checked, so the check reads the raw switch.
                case "PracticeSandbox.Wanted": return PracticeSandbox.Wanted;

                case "GameLaunch.AllBots": return GameLaunch.AllBots;
                case "GameLaunch.Spectator": return GameLaunch.Spectator;
                case "GameLaunch.GuidedTutorial": return GameLaunch.GuidedTutorial;
                case "MatchInstaller.PreviewOnly": return MatchInstaller.PreviewOnly;
                case "AIController.BotsEnabled": return AIController.BotsEnabled;
                case "TouchHud.ForceVisible": return InputLayer.TouchHud.ForceVisible;
                case "SpectatorCamera.ProbeReplayRequest": return CameraSystem.SpectatorCamera.ProbeReplayRequest;
            }

            known = false;
            return false;
        }

        /// <summary>Put one named modifier back to its tournament value.</summary>
        private static void Write(string name, bool value)
        {
            switch (name)
            {
                case "PracticeSandbox.Wanted": PracticeSandbox.Wanted = value; break;
                case "GameLaunch.AllBots": GameLaunch.AllBots = value; break;
                case "GameLaunch.Spectator": GameLaunch.Spectator = value; break;
                case "GameLaunch.GuidedTutorial": GameLaunch.GuidedTutorial = value; break;
                case "MatchInstaller.PreviewOnly": MatchInstaller.PreviewOnly = value; break;
                case "AIController.BotsEnabled": AIController.BotsEnabled = value; break;
                case "TouchHud.ForceVisible": InputLayer.TouchHud.ForceVisible = value; break;
                case "SpectatorCamera.ProbeReplayRequest":
                    CameraSystem.SpectatorCamera.ProbeReplayRequest = value; break;
            }
        }

        /// <summary>Every modifier the core names, with its live value.</summary>
        public static List<Reading> LiveModifiers()
        {
            var rows = new List<Reading>();

            foreach (var m in TournamentPreset.Modifiers)
            {
                bool value = Read(m.Name, out bool known);
                rows.Add(new Reading(m.Name, value, TournamentPreset.SafeValue(m.Name), known, m.Why));
            }

            return rows;
        }

        /// <summary>
        /// Why this machine is not ready to play a tournament match, or "" when it is.
        ///
        /// ⚠️ IT NAMES THE SWITCH AND THE CONSEQUENCE, for `CustomGameRules.Refusal`'s reason: a
        /// refusal with no sentence sends an operator to read eight fields in six files, which is
        /// the situation this whole pair of files replaces.
        /// </summary>
        public static string Refusal(CustomRules rules = null)
        {
            var sb = new StringBuilder();

            string ruleFault = TournamentPreset.RulesRefusal(rules ?? UI.SceneFlow.SelectedRules);
            if (!string.IsNullOrEmpty(ruleFault))
                sb.AppendLine($"the rule set is not the tournament one: {ruleFault}");

            foreach (var r in LiveModifiers())
            {
                if (!r.Known)
                {
                    sb.AppendLine($"{r.Name} is named as a tournament modifier and this build " +
                                  $"cannot read it. Add it to TournamentGuard.Read");
                    continue;
                }

                if (r.Value != r.Safe)
                    sb.AppendLine($"{r.Name} is {(r.Value ? "ON" : "OFF")} and a tournament match " +
                                  $"needs it {(r.Safe ? "ON" : "OFF")}: {r.Why}");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Put this machine into the tournament state, and say what had to be changed.
        ///
        /// ⚠️⚠️ IT RETURNS WHAT IT CLEARED RATHER THAN DOING IT QUIETLY. An operator who starts a
        /// match and is told nothing learns nothing; one who is told "PracticeSandbox was ON and
        /// has been cleared" knows to check the previous match's result. Silence here would turn
        /// a caught problem into an invisible one.
        /// </summary>
        public static List<string> Apply()
        {
            var changed = new List<string>();

            foreach (var r in LiveModifiers())
            {
                if (!r.Known)
                {
                    changed.Add($"{r.Name}: UNREADABLE, left alone");
                    continue;
                }

                if (r.Value == r.Safe) continue;

                Write(r.Name, r.Safe);
                changed.Add($"{r.Name}: {(r.Value ? "ON" : "OFF")} -> {(r.Safe ? "ON" : "OFF")}");
            }

            // ⚠️ THE RULES GO THROUGH `SetSelectedRules` RATHER THAN BEING ASSIGNED, so the
            // clamping and the side effects that path owns happen exactly as they do for a lobby.
            // A tournament preset that bypassed the setter would be a second way to configure a
            // match, which is the shape of the problem rather than the fix.
            UI.SceneFlow.SetSelectedRules(TournamentPreset.Rules());

            if (changed.Count > 0)
                Debug.Log("[Tournament] cleared before starting: " + string.Join(", ", changed));

            return changed;
        }

        /// <summary>The whole state, for a diagnostic screen or a failure bundle.</summary>
        public static string Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine("TOURNAMENT READINESS");
            sb.AppendLine($"  ruleset       {TournamentPreset.Mode}, " +
                          $"{UI.SceneFlow.SelectedRules?.Rounds} rounds, " +
                          $"{UI.SceneFlow.SelectedRules?.RoundSeconds}s");

            foreach (var r in LiveModifiers())
                sb.AppendLine($"  {(r.IsSafe ? "ok  " : "BAD ")} {r.Name,-38} " +
                              $"{(r.Known ? (r.Value ? "ON" : "OFF") : "UNREADABLE")}");

            string refusal = Refusal();
            sb.AppendLine();
            sb.AppendLine(string.IsNullOrEmpty(refusal)
                ? "  READY: this machine will start a tournament match from the intended defaults."
                : "  NOT READY:\n    " + refusal.Replace("\n", "\n    "));

            return sb.ToString();
        }
    }
}
