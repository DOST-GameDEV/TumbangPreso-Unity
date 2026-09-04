using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    /// <summary>
    /// Everything worth knowing about a failure, in one file, with no secrets in it.
    ///
    /// ⚠️⚠️ THE PROBLEM THIS SOLVES IS AN OPERATIONS ONE AND IT HAPPENS UNDER TIME PRESSURE.
    /// When something dies at nationals, the evidence is spread across `Player.log`, the build's
    /// own folder, whatever the operator remembers, and a protocol number only readable from
    /// source. Collecting it means knowing where five things live, and the person holding the
    /// laptop is the person with a queue behind them. **By the time anybody asks for the
    /// evidence, the machine has usually been relaunched**, which is what actually loses the bug.
    ///
    /// ⚠️ SO IT IS ALWAYS COLLECTING AND NEVER WAITING TO BE SWITCHED ON. The exception ring
    /// starts at load and costs one delegate; a bundle you have to enable before the crash is a
    /// bundle you enable after it.
    ///
    /// ⚠️⚠️ AND IT CONTAINS NO CREDENTIALS, WHICH IS A RULE RATHER THAN AN INTENTION. A bundle
    /// gets pasted into a group chat. The UGS PROJECT id is included because it identifies a
    /// namespace and is already in `CLAUDE.md`; **no token, no session, no password, no account
    /// email and no join code with a password beside it goes in here.** `CustomRules.Password`
    /// is host-only and never serialised (`CustomGameRules.Parse` strips it on the way in), and
    /// this file must keep that true.
    ///
    /// Written by:
    ///   TumbangPreso.exe -tp-bundle          on launch, then quits
    ///   FailureBundle.Write("reason")        from code, at any time
    /// </summary>
    public static class FailureBundle
    {
        public const string Switch = "-tp-bundle";

        /// <summary>
        /// ⚠️ BOUNDED, BECAUSE AN UNBOUNDED LOG BUFFER IS THE MEMORY LEAK A CRASH REPORTER IS
        /// SUPPOSED TO HELP WITH. A stack of exceptions repeating every frame would otherwise
        /// grow without limit for the life of the process, which is exactly the situation where
        /// somebody wants a bundle.
        /// </summary>
        private const int MaxEntries = 64;

        private static readonly Queue<string> Recent = new Queue<string>();
        private static bool _hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            if (_hooked) return;
            _hooked = true;

            Application.logMessageReceived += OnLog;

            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (!string.Equals(arg, Switch, StringComparison.OrdinalIgnoreCase)) continue;
                Write("launched with " + Switch);
                Application.Quit(0);
                return;
            }
        }

        private static void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Assert && type != LogType.Error)
                return;

            string first = "";
            if (!string.IsNullOrEmpty(stack))
            {
                int cut = stack.IndexOf('\n');
                first = cut > 0 ? stack.Substring(0, cut) : stack;
            }

            Recent.Enqueue($"[{Time.realtimeSinceStartup:F1}s] {type}: {message}  {first}".Trim());
            while (Recent.Count > MaxEntries) Recent.Dequeue();
        }

        /// <summary>How many errors, assertions and exceptions this process has seen.</summary>
        public static int RecentCount => Recent.Count;

        /// <summary>
        /// Gather everything and write it beside the player, returning the path or null.
        ///
        /// ⚠️ EVERY SECTION IS WRAPPED SEPARATELY. A bundle is written because something is
        /// already wrong, so any one of these reads may itself throw; a bundle that dies while
        /// collecting the network state and therefore contains none of the build identity is
        /// worse than one that says "the network section failed".
        /// </summary>
        public static string Write(string reason = "")
        {
            var sb = new StringBuilder();

            sb.AppendLine("TUMBANG PRESO FAILURE BUNDLE");
            sb.AppendLine($"reason: {(string.IsNullOrEmpty(reason) ? "(none given)" : reason)}");
            sb.AppendLine($"written: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            Section(sb, "BUILD", () => BuildIdentity.Summary());
            Section(sb, "SYSTEM", SystemSummary);
            Section(sb, "TOURNAMENT READINESS", () => TournamentGuard.Report());
            Section(sb, "NETWORK", NetworkSummary);
            Section(sb, "MATCH", MatchSummary);
            Section(sb, "RECENT ERRORS", ErrorSummary);
            Section(sb, "LOG", LogPointer);

            try
            {
                string dir = Application.persistentDataPath;
                string path = Path.Combine(dir,
                    $"tumbangpreso-bundle-{DateTime.Now:yyyyMMdd-HHmmss}.txt");

                // ⚠️ THROUGH `SafeStore` LIKE EVERY OTHER WRITE, so a full disk produces a warning
                // rather than an exception thrown out of the crash handler.
                if (!SafeStore.Write(path, sb.ToString())) return null;

                Debug.Log($"[Bundle] written to {path}");
                return path;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Bundle] could not write: {e.Message}");
                return null;
            }
        }

        private static void Section(StringBuilder sb, string title, Func<string> body)
        {
            sb.AppendLine($"----- {title} -----");
            try
            {
                sb.AppendLine(body());
            }
            catch (Exception e)
            {
                sb.AppendLine($"  (this section failed: {e.GetType().Name}: {e.Message})");
            }
            sb.AppendLine();
        }

        private static string SystemSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"  os            {SystemInfo.operatingSystem}");
            sb.AppendLine($"  device        {SystemInfo.deviceModel} ({SystemInfo.deviceType})");
            sb.AppendLine($"  cpu           {SystemInfo.processorType} x{SystemInfo.processorCount}");
            sb.AppendLine($"  memory        {SystemInfo.systemMemorySize} MB");
            sb.AppendLine($"  gpu           {SystemInfo.graphicsDeviceName} " +
                          $"({SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsMemorySize} MB)");
            sb.AppendLine($"  screen        {Screen.width}x{Screen.height} " +
                          $"fullscreen={Screen.fullScreen} target={Application.targetFrameRate}");
            sb.AppendLine($"  quality       {QualitySettings.GetQualityLevel()}");
            sb.AppendLine($"  managed heap  {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
            return sb.ToString().TrimEnd();
        }

        private static string NetworkSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"  protocol      {Net.NetSession.ProtocolVersion}");
            sb.AppendLine($"  networked     {NetAuthority.IsNetworked}");
            sb.AppendLine($"  host          {NetAuthority.IsHost}");
            sb.AppendLine($"  local slot    {NetAuthority.LocalSlot}");
            sb.AppendLine($"  local peer    {NetAuthority.LocalPeerId}");
            sb.AppendLine($"  referee       {NetAuthority.IsSeatlessReferee}");

            // ⚠️ THE IDENTITY STATE, NOT THE IDENTITY. Whether sign-in succeeded is a diagnosis;
            // who signed in and with what is not this file's business.
            sb.AppendLine($"  online        {Net.NetIdentity.IsOnline}");
            return sb.ToString().TrimEnd();
        }

        private static string MatchSummary()
        {
            var match = GameServices.Match;
            if (match == null) return "  (no match director: not in a match)";

            var sb = new StringBuilder();
            sb.AppendLine($"  in progress   {match.MatchInProgress}");
            sb.AppendLine($"  round         {match.RoundNumber} of {match.TotalRounds}");
            sb.AppendLine($"  taya          seat {match.DefenderSlot}");
            sb.AppendLine($"  buffer        {match.IsWarmupBuffer}");

            var scores = new int[Balance.PlayerCount];
            for (int i = 0; i < scores.Length; i++) scores[i] = match.ScoreFor(i);
            sb.AppendLine($"  scores        {string.Join(", ", scores)}");

            var rules = UI.SceneFlow.SelectedRules;
            if (rules != null)
                sb.AppendLine($"  ruleset       {rules.Mode}, {rules.Format}, {rules.Rounds} rounds, " +
                              $"{rules.RoundSeconds}s, bots {rules.Bots}");

            // ⚠️⚠️ THE INVARIANTS ARE RUN AS PART OF THE BUNDLE, which is the difference between
            // a dump and a diagnosis. If the match state is illegal, the bundle says which rule
            // it broke rather than leaving somebody to read four numbers and notice.
            // ⚠️⚠️ THE OWNERS USED TO BE `"seat" + slot`, WHICH MADE HALF THIS CHECK A NO-OP.
            // Every entry was distinct by construction, so `CheckSeatOwnership`'s duplicate rule
            // could not fire in any state the game could reach. `SeatOwnership.Claims` reads the
            // real durable tokens off the lobby and the bodies, so the two ownership faults
            // `docs/TODO.md` § 141 is about are now findable from a bundle rather than only from
            // a screenshot.
            var claims = SeatOwnership.Claims();
            var owners = MatchInvariants.DrivenSeats(claims);
            sb.AppendLine($"  seats         {string.Join(", ", DescribeClaims(claims))}");

            var snapshot = new MatchSnapshot(match.RoundNumber, match.TotalRounds,
                                             match.DefenderSlot, match.MatchInProgress,
                                             match.IsWarmupBuffer, scores, owners, claims);

            var faults = MatchInvariants.Check(snapshot);
            sb.AppendLine(faults.Count == 0
                ? "  invariants    all hold"
                : "  invariants    VIOLATED:\n                " +
                  string.Join("\n                ", faults));

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// The claims as short readable rows.
        ///
        /// ⚠️ THE TOKEN IS CLIPPED. It is a durable reconnect identity rather than a secret, and
        /// `BuildIdentity`'s own note sets the rule this bundle follows: no secrets, ever. Twelve
        /// characters is enough to tell two claimants apart, which is all a reader needs.
        /// </summary>
        private static IEnumerable<string> DescribeClaims(SeatClaim[] claims)
        {
            if (claims == null || claims.Length == 0) return new[] { "(none)" };

            var rows = new List<string>(claims.Length);
            foreach (var c in claims)
            {
                string token = c.Owner ?? "";
                if (token.Length > 12) token = token.Substring(0, 12);
                rows.Add($"{token}->{c.Seat}{(c.Driving ? "*" : "")}{(c.Spectating ? "~" : "")}");
            }

            return rows;
        }

        private static string ErrorSummary()
        {
            if (Recent.Count == 0) return "  (none this session)";

            var sb = new StringBuilder();
            sb.AppendLine($"  {Recent.Count} of the last {MaxEntries}, oldest first:");
            foreach (string line in Recent) sb.AppendLine("    " + line);
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// ⚠️ THE LOG IS POINTED AT RATHER THAN COPIED IN. `Player.log` is megabytes and Unity
        /// holds it open for writing, so inlining it makes the bundle unusable and copying it
        /// while it is open is unreliable on Windows. The path is the useful thing: the person
        /// reading this can attach it, and until now nobody knew where it was.
        /// </summary>
        private static string LogPointer()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"  persistent    {Application.persistentDataPath}");
            sb.AppendLine($"  console log   {Application.consoleLogPath}");
            sb.AppendLine("  attach the console log beside this file: it has the stack traces, " +
                          "and this has the state they happened in.");
            return sb.ToString().TrimEnd();
        }
    }
}
