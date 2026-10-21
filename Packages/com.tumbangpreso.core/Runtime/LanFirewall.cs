using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TumbangPreso.Core
{
    /// <summary>The Windows Firewall profiles, with the bit values `NET_FW_PROFILE2` uses.</summary>
    [Flags]
    public enum FirewallProfiles { None = 0, Domain = 1, Private = 2, Public = 4, All = 7 }

    /// <summary>What the host's firewall does to players dialling this program's LAN room.</summary>
    public enum LanFirewallState
    {
        /// <summary>The read did not finish. Say nothing rather than guess.</summary>
        Unknown,
        /// <summary>Every active Private or Public network lets this program's UDP in.</summary>
        Open,
        /// <summary>No allow rule covers it on an active network. Adding <see cref="LanFirewallRules.RuleName"/> fixes it.</summary>
        NeedsRule,
        /// <summary>A block rule naming exactly this program wins over any allow. Removing it and adding ours fixes it.</summary>
        BlockedByRule,
        /// <summary>"Block all incoming connections", or a block rule for every program. The game must not touch either.</summary>
        BlockedByPolicy,
    }

    /// <summary>Where an "Allow LAN play" request stands, for the line beside the control.</summary>
    public enum LanFirewallAsk { None, Asking, Declined, Failed }

    /// <summary>One inbound rule, read from its registry string (the [MS-GPFAS] rule grammar).</summary>
    public sealed class FirewallRule
    {
        public string Id = "";
        public string Name = "";
        /// <summary>True for the Group Policy store, which always applies and which the game never edits.</summary>
        public bool FromPolicy;
        public bool Active;
        public bool Inbound;
        public bool Allow;
        public bool Block;
        /// <summary>IP protocol number, or -1 for any protocol (no `Protocol=` token).</summary>
        public int Protocol = -1;
        /// <summary>The profiles named by `Profile=` tokens; <see cref="FirewallProfiles.All"/> when none are.</summary>
        public FirewallProfiles Profiles = FirewallProfiles.All;
        /// <summary>The program path as stored (it may hold %Variables%), or null for every program.</summary>
        public string App;
        /// <summary>`LPort=` values as stored: a number, a range "a-b", or a keyword such as RPC. Empty means any port.</summary>
        public readonly List<string> LocalPorts = new List<string>();
        /// <summary>
        /// The rule only matches some traffic: one service, some remote or local addresses, some
        /// interfaces, remote ports, IPsec, an app container or a user. Such a rule neither lets a
        /// VPN peer in for certain nor blocks it for certain, so it is not counted either way.
        /// </summary>
        public bool Narrowed;
    }

    /// <summary>Everything the read-only probe found.</summary>
    public sealed class FirewallSnapshot
    {
        /// <summary>The probe ran to its last line. A cut-off read is <see cref="LanFirewallState.Unknown"/>.</summary>
        public bool Complete;
        /// <summary>The firewall's active profiles were read (`CurrentProfileTypes`).</summary>
        public bool ActiveKnown;
        public FirewallProfiles Active;
        /// <summary>Profiles whose firewall is on. Assumed on until the probe says otherwise.</summary>
        public FirewallProfiles Enabled = FirewallProfiles.All;
        /// <summary>Profiles with "Block all incoming connections" set, which ignores every allow rule.</summary>
        public FirewallProfiles ShieldsUp;
        /// <summary>Profiles where Group Policy turned off local rules (`AllowLocalPolicyMerge` = 0).</summary>
        public FirewallProfiles LocalRulesIgnored;
        public readonly List<FirewallRule> Rules = new List<FirewallRule>();
    }

    /// <summary>The answer for one program and one set of ports.</summary>
    public sealed class LanFirewallVerdict
    {
        public LanFirewallState State;
        /// <summary>The active profiles the program had to be reachable on (Private and Public only).</summary>
        public FirewallProfiles Checked;
        /// <summary>Of <see cref="Checked"/>, the ones that would drop a joining player.</summary>
        public FirewallProfiles Uncovered;
        /// <summary>Ids of local-store block rules naming exactly this program that stand in the way.</summary>
        public readonly List<string> RemovableBlockRuleIds = new List<string>();

        /// <summary>True when the elevated helper can make the room reachable.</summary>
        public bool CanFix => State == LanFirewallState.NeedsRule || State == LanFirewallState.BlockedByRule;
        public bool NeedsAttention => CanFix || State == LanFirewallState.BlockedByPolicy;

        public static LanFirewallVerdict Unknown() => new LanFirewallVerdict { State = LanFirewallState.Unknown };
    }

    /// <summary>
    /// The host's side of a LAN room on Windows: which firewall rule lets players in, how to tell
    /// whether one already does, and the words for both.
    ///
    /// ⚠️⚠️ WHY THIS EXISTS (2026-10-20, LAN-VPN-JOIN-1020). A LAN row over Hamachi sat on
    /// "Joining..." because the HOST's firewall dropped every inbound packet to UDP 8910. The room
    /// was still listed, because the beacon is an outbound broadcast. Windows files Hamachi, Radmin
    /// and ZeroTier adapters under PUBLIC networks, and its first-run prompt ticks Private only, so a
    /// game that was "allowed" is blocked exactly where a VPN room lives. Worse, answering that
    /// prompt with Public unticked writes an explicit BLOCK rule for the program on Public, and a
    /// block rule beats every allow rule; adding an allow alone changes nothing.
    ///
    /// So there is ONE rule, <see cref="RuleName"/>: inbound, allow, UDP, Private and Public, scoped
    /// to the program's own path. The installer adds it with <see cref="NetshAddArguments"/> and
    /// removes it with <see cref="NetshDeleteArguments"/>; the in-game helper runs the same two
    /// commands elevated, after removing only the block rules that name exactly this program.
    /// No port rule, no rule for any other program, nothing for TCP: the game listens on UDP only
    /// (Unity Transport on the game port, `LanBeacon` on 8911, which also answers discovery
    /// queries, so the host needs both inbound).
    ///
    /// ⚠️ READING NEEDS NO ADMIN, AND READS NOTHING LOCALISED. Rules come from the registry strings
    /// the firewall service keeps (a documented grammar, [MS-GPFAS] 2.2.2.10) in the local and the
    /// Group Policy stores; the active profiles and the per-profile switches come from the
    /// `HNetCfg.FwPolicy2` object. `netsh ... show rule` was rejected because its labels are
    /// translated on a non-English Windows. A standard user can read all of it; measured on the
    /// reporting PC unelevated (678 rules, active profile Public).
    ///
    /// ⚠️ NOT COVERED, AND SAID SO RATHER THAN GUESSED: a third-party firewall that replaces
    /// Windows Firewall, Group Policy profile switches beyond `AllowLocalPolicyMerge`, and a Domain
    /// network (the rule is Private and Public by design, so a Domain-only machine is not checked).
    ///
    /// ⚠️ ENGINE-FREE, so every reading is asserted in Core.Tests against strings captured from a
    /// real machine, and the installer script is checked against the same command text.
    /// </summary>
    public static class LanFirewallRules
    {
        /// <summary>The one rule's display name. The installer, the uninstaller and the helper all key on it.</summary>
        public const string RuleName = "Tumbang Preso (LAN)";

        public const string RuleDescription = "Lets players on your network or VPN join rooms you host in Tumbang Preso.";

        /// <summary>UDP's protocol number in a rule string.</summary>
        public const int Udp = 17;

        /// <summary>`netsh` arguments that add the rule for <paramref name="programPath"/>.</summary>
        public static string NetshAddArguments(string programPath) =>
            "advfirewall firewall add rule name=\"" + RuleName + "\" dir=in action=allow program=\"" + programPath +
            "\" protocol=UDP profile=private,public enable=yes description=\"" + RuleDescription + "\"";

        /// <summary>
        /// `netsh` arguments that delete the rule for <paramref name="programPath"/> and nothing else:
        /// the name AND the program must both match, so another copy of the game keeps its own rule.
        /// </summary>
        public static string NetshDeleteArguments(string programPath) =>
            "advfirewall firewall delete rule name=\"" + RuleName + "\" dir=in program=\"" + programPath + "\"";

        // -----------------------------------------------------------------------------------------
        // The read
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The read-only PowerShell probe. One line per fact, tab-separated, ending in `end` so a cut
        /// read is told apart from an empty one.
        /// ⚠️ UTF-8 OUT, because a rule's program path can hold any character and the console's own
        /// code page would mangle it before the comparison ever ran.
        /// </summary>
        public const string ProbeScript =
            "$ErrorActionPreference = 'SilentlyContinue'\n" +
            "$ProgressPreference = 'SilentlyContinue'\n" +
            "[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding $false\n" +
            "try {\n" +
            "  $fw = New-Object -ComObject HNetCfg.FwPolicy2\n" +
            "  'active=' + [int]$fw.CurrentProfileTypes\n" +
            "  foreach ($p in 1, 2, 4) { 'profile=' + $p + \"`t\" + [int][bool]$fw.FirewallEnabled($p) + \"`t\" + [int][bool]$fw.BlockAllInboundTraffic($p) }\n" +
            "} catch { 'note=no policy object' }\n" +
            "$stores = @(@('local', 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\FirewallRules'),\n" +
            "            @('policy', 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\WindowsFirewall\\FirewallRules'))\n" +
            "foreach ($store in $stores) {\n" +
            "  $key = Get-Item -LiteralPath $store[1]\n" +
            "  if ($key) { foreach ($n in $key.GetValueNames()) { $v = [string]$key.GetValue($n)\n" +
            "    if ($v -like '*|Dir=In|*') { 'rule=' + $store[0] + \"`t\" + ($n -replace '[\\t\\r\\n]', ' ') + \"`t\" + ($v -replace '[\\t\\r\\n]', ' ') } } }\n" +
            "}\n" +
            "foreach ($pair in @(@(1, 'DomainProfile'), @(2, 'PrivateProfile'), @(4, 'PublicProfile'))) {\n" +
            "  $merge = (Get-ItemProperty -LiteralPath ('HKLM:\\SOFTWARE\\Policies\\Microsoft\\WindowsFirewall\\' + $pair[1])).AllowLocalPolicyMerge\n" +
            "  if ($merge -ne $null) { 'merge=' + $pair[0] + \"`t\" + [int]$merge }\n" +
            "}\n" +
            "'end'\n";

        /// <summary>Reads the probe's output. Unrecognised lines are ignored.</summary>
        public static FirewallSnapshot ParseProbe(string output)
        {
            var snapshot = new FirewallSnapshot();
            if (string.IsNullOrEmpty(output)) return snapshot;
            foreach (string raw in output.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                if (line == "end") { snapshot.Complete = true; continue; }
                int equals = line.IndexOf('=');
                if (equals <= 0) continue;
                string key = line.Substring(0, equals);
                string[] fields = line.Substring(equals + 1).Split('\t');
                switch (key)
                {
                    case "active":
                        if (TryInt(fields[0], out int active))
                        {
                            snapshot.ActiveKnown = true;
                            snapshot.Active = (FirewallProfiles)active & FirewallProfiles.All;
                        }
                        break;
                    case "profile":
                        if (fields.Length >= 3 && TryInt(fields[0], out int bit) && TryInt(fields[1], out int on) &&
                            TryInt(fields[2], out int shields))
                        {
                            var profile = (FirewallProfiles)bit & FirewallProfiles.All;
                            snapshot.Enabled = on != 0 ? snapshot.Enabled | profile : snapshot.Enabled & ~profile;
                            if (shields != 0) snapshot.ShieldsUp |= profile;
                        }
                        break;
                    case "merge":
                        if (fields.Length >= 2 && TryInt(fields[0], out int mergeBit) && TryInt(fields[1], out int merge) && merge == 0)
                            snapshot.LocalRulesIgnored |= (FirewallProfiles)mergeBit & FirewallProfiles.All;
                        break;
                    case "rule":
                        if (fields.Length >= 3)
                        {
                            var rule = ParseRule(fields[1], fields[2], fields[0] == "policy");
                            if (rule != null) snapshot.Rules.Add(rule);
                        }
                        break;
                }
            }
            return snapshot;
        }

        /// <summary>
        /// One registry rule string, such as
        /// `v2.10|Action=Block|Active=TRUE|Dir=In|Protocol=17|Profile=Public|App=C:\Games\TumbangPreso.exe|Name=TumbangPreso|`.
        /// Null when the value is not a rule string.
        /// </summary>
        public static FirewallRule ParseRule(string id, string value, bool fromPolicy)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith("v", StringComparison.Ordinal)) return null;
            var rule = new FirewallRule { Id = id ?? "", FromPolicy = fromPolicy };
            bool sawProfile = false;
            foreach (string token in value.Split('|'))
            {
                int equals = token.IndexOf('=');
                if (equals <= 0) continue;
                string key = token.Substring(0, equals);
                string text = token.Substring(equals + 1);
                switch (key)
                {
                    case "Action":
                        rule.Allow = text.Equals("Allow", StringComparison.OrdinalIgnoreCase);
                        rule.Block = text.Equals("Block", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "Active": rule.Active = text.Equals("TRUE", StringComparison.OrdinalIgnoreCase); break;
                    case "Dir": rule.Inbound = text.Equals("In", StringComparison.OrdinalIgnoreCase); break;
                    case "Protocol": rule.Protocol = TryInt(text, out int protocol) ? protocol : -2; break;
                    case "Profile":
                        if (!sawProfile) { rule.Profiles = FirewallProfiles.None; sawProfile = true; }
                        rule.Profiles |= ProfileNamed(text);
                        break;
                    case "App": rule.App = text; break;
                    case "Name": rule.Name = text; break;
                    case "LPort":
                    case "LPort2_10":
                        foreach (string port in text.Split(','))
                            if (port.Trim().Length > 0) rule.LocalPorts.Add(port.Trim());
                        break;
                    case "RA4":
                    case "RA6":
                    case "LA4":
                    case "LA6":
                        if (text != "*" && !text.Equals("Any", StringComparison.OrdinalIgnoreCase)) rule.Narrowed = true;
                        break;
                    case "Security":
                        if (!text.Equals("NotRequired", StringComparison.OrdinalIgnoreCase)) rule.Narrowed = true;
                        break;
                    case "Svc":
                    case "IF":
                    case "IFType":
                    case "RPort":
                    case "RPort2_10":
                    case "PFN":
                    case "LUAuth":
                    case "LUOwn":
                    case "RUAuth":
                    case "RMauth":
                    case "ICMP4":
                    case "ICMP6":
                        rule.Narrowed = true;
                        break;
                }
            }
            return rule;
        }

        private static FirewallProfiles ProfileNamed(string text)
        {
            if (text.Equals("Private", StringComparison.OrdinalIgnoreCase)) return FirewallProfiles.Private;
            if (text.Equals("Public", StringComparison.OrdinalIgnoreCase)) return FirewallProfiles.Public;
            if (text.Equals("Domain", StringComparison.OrdinalIgnoreCase)) return FirewallProfiles.Domain;
            return FirewallProfiles.None;
        }

        // -----------------------------------------------------------------------------------------
        // The verdict
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Whether a player can reach <paramref name="programPath"/> on every port in
        /// <paramref name="ports"/> over each active Private or Public network.
        /// <paramref name="expand"/> resolves `%Variables%` in stored program paths (the game passes
        /// `Environment.ExpandEnvironmentVariables`; tests pass a fixed table).
        /// </summary>
        public static LanFirewallVerdict Evaluate(FirewallSnapshot snapshot, string programPath, IReadOnlyList<int> ports,
                                                  Func<string, string> expand)
        {
            if (snapshot == null || !snapshot.Complete || string.IsNullOrWhiteSpace(programPath) || ports == null || ports.Count == 0)
                return LanFirewallVerdict.Unknown();

            var verdict = new LanFirewallVerdict { State = LanFirewallState.Open };
            // ⚠️ AN UNKNOWN ACTIVE SET CHECKS BOTH PROFILES THE RULE COVERS, because a VPN adapter can
            // appear as Public at any moment and the cost of a needless line is one press.
            FirewallProfiles reachable = FirewallProfiles.Private | FirewallProfiles.Public;
            verdict.Checked = snapshot.ActiveKnown ? snapshot.Active & reachable : reachable;
            string program = NormalisePath(programPath);
            bool policyBlocked = false;

            foreach (FirewallProfiles profile in new[] { FirewallProfiles.Private, FirewallProfiles.Public })
            {
                if ((verdict.Checked & profile) == 0) continue;
                if ((snapshot.Enabled & profile) == 0) continue; // firewall off on this profile: open
                if ((snapshot.ShieldsUp & profile) != 0)
                {
                    verdict.Uncovered |= profile;
                    policyBlocked = true;
                    continue;
                }

                foreach (int port in ports)
                {
                    bool allowed = false, blockedHere = false;
                    foreach (var rule in snapshot.Rules)
                    {
                        if (!Applies(rule, snapshot, profile, port, program, expand)) continue;
                        if (rule.Block)
                        {
                            blockedHere = true;
                            if (rule.App != null && !rule.FromPolicy)
                            {
                                if (!verdict.RemovableBlockRuleIds.Contains(rule.Id)) verdict.RemovableBlockRuleIds.Add(rule.Id);
                            }
                            else policyBlocked = true;
                        }
                        else if (rule.Allow) allowed = true;
                    }
                    if (blockedHere || !allowed) verdict.Uncovered |= profile;
                }
            }

            if (verdict.Uncovered == FirewallProfiles.None)
            {
                verdict.RemovableBlockRuleIds.Clear();
                return verdict;
            }
            verdict.State = policyBlocked ? LanFirewallState.BlockedByPolicy
                          : verdict.RemovableBlockRuleIds.Count > 0 ? LanFirewallState.BlockedByRule
                          : LanFirewallState.NeedsRule;
            return verdict;
        }

        private static bool Applies(FirewallRule rule, FirewallSnapshot snapshot, FirewallProfiles profile, int port,
                                    string program, Func<string, string> expand)
        {
            if (!rule.Active || !rule.Inbound || rule.Narrowed || (!rule.Allow && !rule.Block)) return false;
            if ((rule.Profiles & profile) == 0) return false;
            if (!rule.FromPolicy && (snapshot.LocalRulesIgnored & profile) != 0) return false;
            if (rule.Protocol != -1 && rule.Protocol != Udp) return false;
            if (rule.App != null)
            {
                string app = rule.App;
                if (expand != null && app.IndexOf('%') >= 0) app = expand(app) ?? app;
                if (!string.Equals(NormalisePath(app), program, StringComparison.OrdinalIgnoreCase)) return false;
            }
            return PortMatches(rule.LocalPorts, port);
        }

        private static bool PortMatches(List<string> specs, int port)
        {
            if (specs.Count == 0) return true;
            foreach (string spec in specs)
            {
                if (spec == "*" || spec.Equals("Any", StringComparison.OrdinalIgnoreCase)) return true;
                int dash = spec.IndexOf('-');
                if (dash > 0)
                {
                    if (TryInt(spec.Substring(0, dash), out int low) && TryInt(spec.Substring(dash + 1), out int high) &&
                        port >= low && port <= high) return true;
                }
                else if (TryInt(spec, out int single) && single == port) return true;
                // A keyword (RPC, Ply2Disc, mDNS ...) names a system service's ports, never ours.
            }
            return false;
        }

        /// <summary>Case and separator neutral, so a prompt rule's lower-cased path still matches.</summary>
        public static string NormalisePath(string path)
        {
            if (path == null) return "";
            string p = path.Trim().Trim('"').Replace('/', '\\');
            while (p.Contains("\\\\")) p = p.Replace("\\\\", "\\");
            return p.ToLowerInvariant();
        }

        // -----------------------------------------------------------------------------------------
        // The elevated helper
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The script the "Allow LAN play" control runs elevated, once, through UAC.
        ///
        /// ⚠️ IT DELETES ONLY WHAT THE READ NAMED: the local-store block rules whose program is
        /// exactly this one (the ones Windows writes when its first-run prompt is answered with Public
        /// unticked), each by its exact id, plus an older copy of our own rule for this same path.
        /// Then it adds the rule with the installer's own `netsh` text. `Start-Process` with one
        /// argument string hands netsh exactly that text, so the two can never write different rules.
        /// The game itself keeps running unelevated; only this script gets the token.
        /// </summary>
        public static string AllowScript(string programPath, IEnumerable<string> blockRuleIds)
        {
            var script = new StringBuilder();
            script.Append("$ErrorActionPreference = 'Stop'\n");
            script.Append("try {\n");
            if (blockRuleIds != null)
            {
                foreach (string id in blockRuleIds)
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    string literal = Quote(id);
                    // ⚠️ THE EXACT-NAME FILTER IS THE SAFETY, the wildcard escape only narrows the query:
                    // a path can hold [ ] and the CIM layer may treat other characters as patterns.
                    script.Append("  Get-NetFirewallRule -Name ([WildcardPattern]::Escape(").Append(literal)
                          .Append(")) -ErrorAction SilentlyContinue | Where-Object { $_.Name -eq ").Append(literal)
                          .Append(" } | Remove-NetFirewallRule\n");
                }
            }
            script.Append("  $netsh = Join-Path $env:SystemRoot 'System32\\netsh.exe'\n");
            script.Append("  Start-Process -FilePath $netsh -ArgumentList ").Append(Quote(NetshDeleteArguments(programPath)))
                  .Append(" -Wait -WindowStyle Hidden\n");
            script.Append("  $added = Start-Process -FilePath $netsh -ArgumentList ").Append(Quote(NetshAddArguments(programPath)))
                  .Append(" -Wait -PassThru -WindowStyle Hidden\n");
            script.Append("  exit $added.ExitCode\n");
            script.Append("} catch { exit 1 }\n");
            return script.ToString();
        }

        /// <summary>A PowerShell single-quoted literal: nothing inside it is expanded.</summary>
        public static string Quote(string text) => "'" + (text ?? "").Replace("'", "''") + "'";

        /// <summary>`-EncodedCommand` form: base64 of the UTF-16LE script, so no path is ever re-quoted.</summary>
        public static string Encode(string script) => Convert.ToBase64String(Encoding.Unicode.GetBytes(script ?? ""));

        /// <summary>The PowerShell arguments that run <paramref name="script"/> with no profile and no window.</summary>
        public static string PowerShellArguments(string script) =>
            "-NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand " + Encode(script);

        // -----------------------------------------------------------------------------------------
        // The words
        // -----------------------------------------------------------------------------------------

        /// <summary>The control's label.</summary>
        public const string AllowLabel = "ALLOW LAN PLAY";

        /// <summary>The toast once the rule is in place and the read agrees.</summary>
        public const string AllowedToast = "LAN play allowed in Windows Firewall.";

        /// <summary>
        /// The room's one line for the host, or "" for nothing to say. It names Hamachi and Radmin when
        /// only Public networks are unreachable, because that is where Windows files them.
        /// ⚠️ EVERY LINE FITS TWO ROWS of the 28-unit floor in the room's 430-unit column (about 27
        /// characters a row, measured on the first render, where a longer line ran into the address).
        /// </summary>
        public static string NoticeLine(LanFirewallVerdict verdict, LanFirewallAsk ask)
        {
            if (ask == LanFirewallAsk.Asking) return "Waiting for Windows to allow LAN play...";
            if (verdict == null || !verdict.NeedsAttention) return "";
            if (verdict.State == LanFirewallState.BlockedByPolicy)
                return "A Windows Firewall setting blocks players joining.";
            if (ask == LanFirewallAsk.Declined) return "Not changed: Windows did not get permission.";
            if (ask == LanFirewallAsk.Failed) return "Windows Firewall still blocks players joining.";
            return verdict.Uncovered == FirewallProfiles.Public
                ? "Windows Firewall may block Hamachi or Radmin players."
                : "Windows Firewall may block players joining this room.";
        }

        private static bool TryInt(string text, out int value) =>
            int.TryParse((text ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
