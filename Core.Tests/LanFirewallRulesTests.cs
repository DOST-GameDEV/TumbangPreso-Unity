using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// ⚠️ THE HOST'S FIREWALL READ, ASSERTED ON THE STRINGS WINDOWS ACTUALLY WRITES (LAN-VPN-JOIN-1020).
    /// Every rule string here has the shape read unelevated from the reporting PC's registry on
    /// 2026-10-20: the first-run prompt's "TCP/UDP Query User{guid}path" rules with a lower-cased
    /// path, and the Unity Editor's Allow-on-Domain plus Block-on-Public pair. Paths are generic.
    /// </summary>
    public sealed class LanFirewallRulesTests
    {
        private const string Exe = @"C:\Games\Tumbang Preso\TumbangPreso.exe";
        private static readonly int[] Ports = { 8910, 8911 };

        private static string Expand(string path) =>
            path.Replace("%ProgramFiles%", @"C:\Program Files").Replace("%SystemRoot%", @"C:\Windows");

        private static string Probe(int active, params string[] rules)
        {
            var text = new StringBuilder();
            text.Append("active=").Append(active).Append('\n');
            text.Append("profile=1\t1\t0\nprofile=2\t1\t0\nprofile=4\t1\t0\n");
            foreach (string rule in rules) text.Append(rule).Append('\n');
            text.Append("end\n");
            return text.ToString();
        }

        private static string Rule(string id, string value, string store = "local") => "rule=" + store + "\t" + id + "\t" + value;

        private static LanFirewallVerdict Judge(string probe, string exe = Exe) =>
            LanFirewallRules.Evaluate(LanFirewallRules.ParseProbe(probe), exe, Ports, Expand);

        private static string PromptRule(string action, int protocol, string profile, string path) =>
            "v2.10|Action=" + action + "|Active=TRUE|Dir=In|Protocol=" + protocol + "|Profile=" + profile + "|App=" + path +
            "|Name=tumbangpreso|Desc=tumbangpreso|Defer=User|";

        private static readonly string OurRule =
            "v2.33|Action=Allow|Active=TRUE|Dir=In|Protocol=17|Profile=Private|Profile=Public|App=" + Exe +
            "|Name=" + LanFirewallRules.RuleName + "|Desc=" + LanFirewallRules.RuleDescription + "|";

        [Fact]
        public void NoRuleForTheProgramNeedsOne()
        {
            var verdict = Judge(Probe(4));
            Assert.Equal(LanFirewallState.NeedsRule, verdict.State);
            Assert.Equal(FirewallProfiles.Public, verdict.Uncovered);
            Assert.True(verdict.CanFix);
            Assert.Empty(verdict.RemovableBlockRuleIds);
        }

        [Fact]
        public void OurOwnRuleOpensPrivateAndPublic()
        {
            var verdict = Judge(Probe(6, Rule("{D9A909F1-5BF5-4B80-BFCC-30EC1D94CBE8}", OurRule)));
            Assert.Equal(LanFirewallState.Open, verdict.State);
            Assert.Equal(FirewallProfiles.Private | FirewallProfiles.Public, verdict.Checked);
            Assert.Equal(FirewallProfiles.None, verdict.Uncovered);
        }

        [Fact]
        public void APromptRuleWithALowerCasedPathStillCountsForTheProgram()
        {
            string lower = Exe.ToLowerInvariant();
            var verdict = Judge(Probe(4, Rule("UDP Query User{F5FF962F-082D-4268-8D5A-96132185E281}" + lower,
                                              PromptRule("Allow", 17, "Public", lower))));
            Assert.Equal(LanFirewallState.Open, verdict.State);
        }

        [Fact]
        public void PrivateOnlyIsBlockedOverAPublicVpnAndFineAtHome()
        {
            string allowPrivate = Rule("UDP Query User{1}" + Exe, PromptRule("Allow", 17, "Private", Exe));
            var vpn = Judge(Probe(4, allowPrivate));
            Assert.Equal(LanFirewallState.NeedsRule, vpn.State);
            Assert.Equal(FirewallProfiles.Public, vpn.Uncovered);
            Assert.Equal("Windows Firewall may block Hamachi or Radmin players.",
                         LanFirewallRules.NoticeLine(vpn, LanFirewallAsk.None));

            var home = Judge(Probe(2, allowPrivate));
            Assert.Equal(LanFirewallState.Open, home.State);
            Assert.Equal("", LanFirewallRules.NoticeLine(home, LanFirewallAsk.None));
        }

        [Fact]
        public void APromptBlockRuleBeatsEveryAllowAndIsTheOnlyThingNamedForRemoval()
        {
            // The pattern on the reporting PC, for the Editor: allowed somewhere, BLOCKED on Public.
            string blockId = "UDP Query User{6C852D9C-85B4-4325-893A-58E63F4D643F}" + Exe.ToLowerInvariant();
            string tcpBlockId = "TCP Query User{3A7ABD27-4A2B-4074-834F-4ACED4219CFB}" + Exe.ToLowerInvariant();
            var verdict = Judge(Probe(4,
                Rule("{ours}", OurRule),
                Rule(blockId, PromptRule("Block", 17, "Public", Exe.ToLowerInvariant())),
                Rule(tcpBlockId, PromptRule("Block", 6, "Public", Exe.ToLowerInvariant())),
                Rule("UDP Query User{other}c:\\other\\game.exe", PromptRule("Block", 17, "Public", @"c:\other\game.exe"))));
            Assert.Equal(LanFirewallState.BlockedByRule, verdict.State);
            Assert.True(verdict.CanFix);
            Assert.Equal(new[] { blockId }, verdict.RemovableBlockRuleIds);
        }

        [Fact]
        public void TheEditorPatternFromTheReportingPcReadsBlockedOnPublic()
        {
            const string unity = @"C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe";
            string probe = Probe(4,
                Rule("{A1297CD2-799E-4659-8FCE-C3E877508310}", "v2.33|Action=Allow|Active=TRUE|Dir=In|Profile=Domain|App=" + unity + "|Name=Unity 6000.5.8f1 Editor|"),
                Rule("{6C852D9C-85B4-4325-893A-58E63F4D643F}", "v2.33|Action=Block|Active=TRUE|Dir=In|Profile=Public|App=" + unity + "|Name=Unity 6000.5.8f1 Editor|"));
            var verdict = Judge(probe, unity);
            Assert.Equal(LanFirewallState.BlockedByRule, verdict.State);
            Assert.Equal(new[] { "{6C852D9C-85B4-4325-893A-58E63F4D643F}" }, verdict.RemovableBlockRuleIds);
        }

        [Fact]
        public void ABlockRuleForEveryProgramIsPolicyAndNotTouched()
        {
            var verdict = Judge(Probe(4, Rule("{ours}", OurRule),
                Rule("{all}", "v2.33|Action=Block|Active=TRUE|Dir=In|Protocol=17|Profile=Public|LPort=8910|Name=No games|")));
            Assert.Equal(LanFirewallState.BlockedByPolicy, verdict.State);
            Assert.False(verdict.CanFix);
            Assert.Empty(verdict.RemovableBlockRuleIds);
            Assert.Equal("A Windows Firewall setting blocks players joining.",
                         LanFirewallRules.NoticeLine(verdict, LanFirewallAsk.None));
        }

        [Fact]
        public void AGroupPolicyBlockOnThisProgramIsNotRemovable()
        {
            var verdict = Judge(Probe(4, Rule("{ours}", OurRule),
                Rule("{gpo}", PromptRule("Block", 17, "Public", Exe), "policy")));
            Assert.Equal(LanFirewallState.BlockedByPolicy, verdict.State);
            Assert.Empty(verdict.RemovableBlockRuleIds);
        }

        [Fact]
        public void BlockAllIncomingConnectionsIgnoresEveryAllow()
        {
            string probe = Probe(4, Rule("{ours}", OurRule)).Replace("profile=4\t1\t0", "profile=4\t1\t1");
            var verdict = Judge(probe);
            Assert.Equal(LanFirewallState.BlockedByPolicy, verdict.State);
            Assert.Equal(FirewallProfiles.Public, verdict.Uncovered);
        }

        [Fact]
        public void AFirewallSwitchedOffOnTheProfileIsOpen()
        {
            string probe = Probe(4).Replace("profile=4\t1\t0", "profile=4\t0\t0");
            Assert.Equal(LanFirewallState.Open, Judge(probe).State);
        }

        [Fact]
        public void GroupPolicyThatIgnoresLocalRulesIgnoresOursToo()
        {
            string probe = Probe(4, Rule("{ours}", OurRule)).Replace("end\n", "merge=4\t0\nend\n");
            Assert.Equal(LanFirewallState.NeedsRule, Judge(probe).State);
            string withPolicyAllow = probe.Replace("end\n", Rule("{gpo-allow}", OurRule, "policy") + "\nend\n");
            Assert.Equal(LanFirewallState.Open, Judge(withPolicyAllow).State);
        }

        [Fact]
        public void BothPortsMustBeLetIn()
        {
            string gameOnly = Rule("{p}", "v2.33|Action=Allow|Active=TRUE|Dir=In|Protocol=17|LPort=8910|Name=Game port|");
            Assert.Equal(LanFirewallState.NeedsRule, Judge(Probe(4, gameOnly)).State);

            string both = Rule("{q}", "v2.33|Action=Allow|Active=TRUE|Dir=In|Protocol=17|LPort=8911|Name=Beacon|");
            Assert.Equal(LanFirewallState.Open, Judge(Probe(4, gameOnly, both)).State);

            string range = Rule("{r}", "v2.33|Action=Allow|Active=TRUE|Dir=In|Protocol=17|LPort=8900-8920|Name=Range|");
            Assert.Equal(LanFirewallState.Open, Judge(Probe(4, range)).State);

            string keyword = Rule("{k}", "v2.33|Action=Allow|Active=TRUE|Dir=In|Protocol=17|LPort=RPC|Name=Keyword|");
            Assert.Equal(LanFirewallState.NeedsRule, Judge(Probe(4, keyword)).State);
        }

        [Fact]
        public void TcpOnlyInactiveOutboundAndOtherProgramsDoNotCount()
        {
            Assert.Equal(LanFirewallState.NeedsRule, Judge(Probe(4,
                Rule("{tcp}", PromptRule("Allow", 6, "Public", Exe)),
                Rule("{off}", OurRule.Replace("Active=TRUE", "Active=FALSE")),
                Rule("{out}", OurRule.Replace("Dir=In", "Dir=Out")),
                Rule("{other}", OurRule.Replace(Exe, @"C:\Games\Other\TumbangPreso.exe")))).State);
        }

        [Fact]
        public void NarrowedRulesNeitherAllowNorBlock()
        {
            string[] narrowing = { "RA4=LocalSubnet", "Svc=Spooler", "IFType=Wireless", "RPort=5000", "Security=Authenticate", "LA4=10.0.0.1" };
            foreach (string token in narrowing)
            {
                string allow = OurRule.Replace("|Name=", "|" + token + "|Name=");
                Assert.Equal(LanFirewallState.NeedsRule, Judge(Probe(4, Rule("{n}", allow))).State);
                string block = PromptRule("Block", 17, "Public", Exe).Replace("|Name=", "|" + token + "|Name=");
                Assert.Equal(LanFirewallState.Open, Judge(Probe(4, Rule("{ours}", OurRule), Rule("{b}", block))).State);
            }
            Assert.Equal(LanFirewallState.Open,
                         Judge(Probe(4, Rule("{star}", OurRule.Replace("|Name=", "|RA4=*|Name=")))).State);
        }

        [Fact]
        public void AnAnyProtocolAllProgramsRuleCoversAndEnvironmentPathsExpand()
        {
            Assert.Equal(LanFirewallState.Open,
                         Judge(Probe(4, Rule("{any}", "v2.33|Action=Allow|Active=TRUE|Dir=In|Profile=Public|Name=Everything|"))).State);

            const string installed = @"C:\Program Files\Tumbang Preso\TumbangPreso.exe";
            string stored = OurRule.Replace(Exe, @"%ProgramFiles%\Tumbang Preso\TumbangPreso.exe");
            Assert.Equal(LanFirewallState.Open, Judge(Probe(4, Rule("{env}", stored)), installed).State);
        }

        [Fact]
        public void OnlyActivePrivateAndPublicProfilesAreChecked()
        {
            var domainOnly = Judge(Probe(1));
            Assert.Equal(LanFirewallState.Open, domainOnly.State);
            Assert.Equal(FirewallProfiles.None, domainOnly.Checked);

            // Hamachi Public beside Ethernet Private, allowed on Private only.
            var both = Judge(Probe(6, Rule("{p}", PromptRule("Allow", 17, "Private", Exe))));
            Assert.Equal(FirewallProfiles.Public, both.Uncovered);
        }

        [Fact]
        public void AnUnknownActiveSetChecksBothProfilesAndACutReadIsUnknown()
        {
            string noCom = "note=no policy object\n" + Rule("{p}", PromptRule("Allow", 17, "Private", Exe)) + "\nend\n";
            var verdict = Judge(noCom);
            Assert.Equal(FirewallProfiles.Private | FirewallProfiles.Public, verdict.Checked);
            Assert.Equal(LanFirewallState.NeedsRule, verdict.State);

            string cut = Probe(4, Rule("{ours}", OurRule)).Replace("end\n", "");
            Assert.Equal(LanFirewallState.Unknown, Judge(cut).State);
            Assert.Equal(LanFirewallState.Unknown, Judge("").State);
            Assert.Equal(LanFirewallState.Unknown, Judge(null).State);
            Assert.Equal("", LanFirewallRules.NoticeLine(Judge(cut), LanFirewallAsk.None));
        }

        [Fact]
        public void CarriageReturnsAndJunkLinesAreTolerated()
        {
            string probe = Probe(4, Rule("{ours}", OurRule)).Replace("\n", "\r\n") + "garbage\r\n=\r\nrule=local\tonly-two\r\n";
            Assert.Equal(LanFirewallState.Open, Judge(probe).State);
        }

        [Fact]
        public void TheNetshCommandsNameTheRuleAndTheProgramAndNothingElse()
        {
            Assert.Equal(
                "advfirewall firewall add rule name=\"Tumbang Preso (LAN)\" dir=in action=allow program=\"" + Exe +
                "\" protocol=UDP profile=private,public enable=yes description=\"" + LanFirewallRules.RuleDescription + "\"",
                LanFirewallRules.NetshAddArguments(Exe));
            Assert.Equal("advfirewall firewall delete rule name=\"Tumbang Preso (LAN)\" dir=in program=\"" + Exe + "\"",
                         LanFirewallRules.NetshDeleteArguments(Exe));
            Assert.DoesNotContain("\"", LanFirewallRules.RuleDescription);
        }

        [Fact]
        public void TheHelperRemovesOnlyTheNamedBlockRulesAndAddsTheInstallersRule()
        {
            const string quoted = @"D:\Andrei's Games\TumbangPreso.exe";
            string id = "UDP Query User{6C852D9C}" + quoted.ToLowerInvariant();
            string script = LanFirewallRules.AllowScript(quoted, new[] { id });

            Assert.Contains("Where-Object { $_.Name -eq 'UDP Query User{6C852D9C}d:\\andrei''s games\\tumbangpreso.exe' }", script);
            Assert.Single(Regex.Matches(script, "Remove-NetFirewallRule").Cast<Match>());
            Assert.Contains(LanFirewallRules.Quote(LanFirewallRules.NetshAddArguments(quoted)), script);
            Assert.Contains(LanFirewallRules.Quote(LanFirewallRules.NetshDeleteArguments(quoted)), script);
            Assert.True(script.IndexOf("delete rule", StringComparison.Ordinal) < script.IndexOf("add rule", StringComparison.Ordinal),
                        "an older copy of our rule for this path goes first, so a second press never doubles it");
            Assert.DoesNotContain("New-NetFirewallRule", script);

            string none = LanFirewallRules.AllowScript(Exe, new string[0]);
            Assert.DoesNotContain("Remove-NetFirewallRule", none);

            string arguments = LanFirewallRules.PowerShellArguments(script);
            string encoded = arguments.Substring(arguments.LastIndexOf(' ') + 1);
            Assert.Equal(script, Encoding.Unicode.GetString(Convert.FromBase64String(encoded)));
            Assert.StartsWith("-NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand ", arguments);
        }

        [Fact]
        public void TheProbeOnlyReads()
        {
            string script = LanFirewallRules.ProbeScript;
            foreach (string verb in new[] { "Set-", "New-Net", "New-Item", "Remove-", "netsh", "Add-", "Enable-", "Disable-", ".Add(", ".Remove(" })
                Assert.DoesNotContain(verb, script);
            Assert.EndsWith("'end'\n", script);
        }

        [Fact]
        public void TheLineFollowsTheRequest()
        {
            var needs = Judge(Probe(6));
            Assert.Equal("Windows Firewall may block players joining this room.", LanFirewallRules.NoticeLine(needs, LanFirewallAsk.None));
            Assert.Equal("Waiting for Windows to allow LAN play...", LanFirewallRules.NoticeLine(needs, LanFirewallAsk.Asking));
            Assert.Equal("Not changed: Windows did not get permission.", LanFirewallRules.NoticeLine(needs, LanFirewallAsk.Declined));
            Assert.Equal("Windows Firewall still blocks players joining.", LanFirewallRules.NoticeLine(needs, LanFirewallAsk.Failed));
            Assert.Equal("", LanFirewallRules.NoticeLine(null, LanFirewallAsk.None));
        }

        /// <summary>
        /// ⚠️ THE INSTALLER AND THE GAME MUST WRITE ONE RULE, NOT TWO THAT DRIFT. The installer is
        /// Inno Setup, which cannot call this code, so its command text is checked against it here.
        /// </summary>
        [Fact]
        public void TheInstallerAddsAndRemovesExactlyTheGamesRule()
        {
            string iss = File.ReadAllText(Path.Combine(RepoRoot(), "installer", "TumbangPreso.iss"));
            var parameters = Regex.Matches(iss, @"Parameters:\s*""((?:[^""]|"""")*)""")
                                  .Cast<Match>().Select(m => m.Groups[1].Value.Replace("\"\"", "\"")).ToList();
            const string installed = @"{app}\TumbangPreso.exe";
            string add = LanFirewallRules.NetshAddArguments(installed);
            string delete = LanFirewallRules.NetshDeleteArguments(installed);

            Assert.Contains(add, parameters);
            Assert.Contains(delete, parameters);
            Assert.All(parameters.Where(p => p.Contains("advfirewall")), p => Assert.True(p == add || p == delete, p));

            // Removal on uninstall, before the files go; per machine; the game is never launched elevated.
            Assert.Matches(new Regex(@"\[UninstallRun\][^\[]*" + Regex.Escape("netsh.exe"), RegexOptions.Singleline), iss);
            Assert.Matches(new Regex(@"^PrivilegesRequired=admin\s*$", RegexOptions.Multiline), iss);
            foreach (Match launch in Regex.Matches(iss, @"^Filename:\s*""\{app\}\\TumbangPreso\.exe"".*$", RegexOptions.Multiline))
                Assert.Contains("runasoriginaluser", launch.Value);
        }

        /// <summary>
        /// The probe against THIS machine's real firewall, read only, when TUMP_FIREWALL_LIVE names a
        /// program path (for example the Unity Editor's). It changes nothing and needs no admin; it
        /// prints the verdict so the evidence is the real machine, not a fixture.
        /// </summary>
        [Fact]
        public void LiveReadOfThisMachine()
        {
            string program = Environment.GetEnvironmentVariable("TUMP_FIREWALL_LIVE");
            if (string.IsNullOrEmpty(program) || !OperatingSystem.IsWindows()) return;
            var info = new System.Diagnostics.ProcessStartInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe"),
                "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + LanFirewallRules.Encode(LanFirewallRules.ProbeScript))
            {
                UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true, StandardOutputEncoding = Encoding.UTF8,
            };
            using var process = System.Diagnostics.Process.Start(info);
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            var snapshot = LanFirewallRules.ParseProbe(output);
            var verdict = LanFirewallRules.Evaluate(snapshot, program, Ports, Environment.ExpandEnvironmentVariables);
            _output.WriteLine($"program={program}");
            _output.WriteLine($"complete={snapshot.Complete} active={snapshot.Active} enabled={snapshot.Enabled} shields={snapshot.ShieldsUp} rules(in)={snapshot.Rules.Count}");
            _output.WriteLine($"state={verdict.State} checked={verdict.Checked} uncovered={verdict.Uncovered} removable=[{string.Join(", ", verdict.RemovableBlockRuleIds)}]");
            _output.WriteLine($"line={LanFirewallRules.NoticeLine(verdict, LanFirewallAsk.None)}");
            Assert.True(snapshot.Complete, "the probe did not reach its last line");
            Assert.True(snapshot.ActiveKnown, "the firewall policy object did not answer");
        }

        /// <summary>
        /// ⚠️ BOTH SCRIPTS ARE PARSED BY WINDOWS POWERSHELL ITSELF, NEVER RUN. A syntax slip in the
        /// helper would only show on a player's PC after they had said yes to UAC.
        /// </summary>
        [Fact]
        public void BothScriptsParseInWindowsPowerShell()
        {
            if (!OperatingSystem.IsWindows()) return;
            string helper = LanFirewallRules.AllowScript(@"D:\Andrei's Games [x]\TumbangPreso.exe",
                                                         new[] { @"UDP Query User{6C852D9C}d:\andrei's games [x]\tumbangpreso.exe", "{A1297CD2}" });
            foreach (string script in new[] { LanFirewallRules.ProbeScript, helper })
            {
                string check = "$e = $null; [void][System.Management.Automation.Language.Parser]::ParseInput(" +
                               "[Text.Encoding]::Unicode.GetString([Convert]::FromBase64String('" + LanFirewallRules.Encode(script) +
                               "')), [ref]$null, [ref]$e); 'errors=' + $e.Count; $e | ForEach-Object { $_.Message }";
                var info = new System.Diagnostics.ProcessStartInfo(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe"),
                    "-NoProfile -NonInteractive -EncodedCommand " + LanFirewallRules.Encode(check))
                { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                using var process = System.Diagnostics.Process.Start(info);
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                Assert.StartsWith("errors=0", output);
            }
        }

        private readonly Xunit.Abstractions.ITestOutputHelper _output;
        public LanFirewallRulesTests(Xunit.Abstractions.ITestOutputHelper output) { _output = output; }

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AGENTS.md"))) dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
        }
    }
}
