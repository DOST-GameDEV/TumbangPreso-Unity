using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TumbangPreso.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TumbangPreso.Net
{
    /// <summary>The machine half of <see cref="LanFirewall"/>; tests replace it.</summary>
    public interface ILanFirewallBackend
    {
        /// <summary>A Windows player or Editor with a window. False everywhere else, and in batch mode.</summary>
        bool Supported { get; }
        /// <summary>The running program's own path, which is what the rule is scoped to.</summary>
        string ProgramPath { get; }
        /// <summary>The read-only probe's output, or null. Called off the main thread.</summary>
        string Probe();
        /// <summary>
        /// Runs the script elevated through UAC and waits. Returns its exit code; throws
        /// <see cref="LanFirewallDeclinedException"/> when the player says no. Called off the main thread.
        /// </summary>
        int RunElevated(string script);
    }

    public sealed class LanFirewallDeclinedException : Exception
    {
        public LanFirewallDeclinedException() : base("The elevation prompt was declined.") { }
    }

    /// <summary>
    /// The in-game fallback for a zip copy of the game: when the player hosts a LAN room on
    /// Windows, is this program reachable through the firewall on the active networks, and if not,
    /// one press that asks Windows (UAC) to add the installer's own rule.
    ///
    /// ⚠️⚠️ THE GAME NEVER RUNS ELEVATED. The read needs no admin (`LanFirewallRules.ProbeScript`
    /// reads the rule strings and the active profiles); the change is a separate PowerShell started
    /// with the "runas" verb, which Windows shows as its own UAC prompt and which exits when done.
    /// Declining it changes nothing and says so. An installed copy already has the rule from
    /// `installer/TumbangPreso.iss`, so it reads Open and the room shows nothing.
    ///
    /// ⚠️ EVERY PROCESS RUNS OFF THE MAIN THREAD and the result is applied in <see cref="Watch"/>,
    /// which the room calls each frame while the local player hosts a LAN room. Reads happen once
    /// per room, again when the window regains focus (the player may have answered Windows' own
    /// prompt, changed a network to Private, or started a VPN), and after a request.
    /// </summary>
    public static class LanFirewall
    {
        private static ILanFirewallBackend _backend = new WindowsLanFirewallBackend();
        private static Task<LanFirewallVerdict> _read;
        private static Task<int?> _request;
        private static bool _stale = true, _confirming, _focusHooked;
        private static int _port;
        private static string _logged = "";

        /// <summary>Swaps the machine side (tests). Null restores the real one.</summary>
        public static ILanFirewallBackend Backend
        {
            get => _backend;
            set { _backend = value ?? new WindowsLanFirewallBackend(); Reset(); }
        }

        public static bool Supported => _backend.Supported;
        public static LanFirewallVerdict Verdict { get; private set; } = LanFirewallVerdict.Unknown();
        public static LanFirewallAsk Ask { get; private set; }
        /// <summary>Counts confirmed fixes, so a screen can say so once without holding an event.</summary>
        public static int AllowedCount { get; private set; }

        /// <summary>The room's line for the host, or "" for nothing.</summary>
        public static string Line => Supported ? LanFirewallRules.NoticeLine(Verdict, Ask) : "";

        /// <summary>The "Allow LAN play" control is offered and pressable.</summary>
        public static bool CanRequest => Supported && Verdict.CanFix && Ask != LanFirewallAsk.Asking && _request == null;

        /// <summary>Read again at the next <see cref="Watch"/>: a new room, or a returned window.</summary>
        public static void Invalidate() => _stale = true;

        /// <summary>
        /// Call every frame while the local player hosts a LAN room on <paramref name="gamePort"/>.
        /// Applies finished work and starts a read when one is due.
        /// </summary>
        public static void Watch(int gamePort)
        {
            if (!Supported) return;
            HookFocus();
            Pump();
            if (gamePort != _port) { _port = gamePort; _stale = true; }
            if (_stale && _read == null && _request == null) StartRead();
        }

        /// <summary>The "Allow LAN play" press: one elevated helper, then a fresh read decides.</summary>
        public static void RequestAllow()
        {
            if (!CanRequest) return;
            Ask = LanFirewallAsk.Asking;
            var backend = _backend;
            string script = LanFirewallRules.AllowScript(backend.ProgramPath, Verdict.RemovableBlockRuleIds.ToArray());
            Debug.Log($"[LanFirewall] asking Windows to allow {backend.ProgramPath} " +
                      $"(removing {Verdict.RemovableBlockRuleIds.Count} block rule(s) for this program)");
            _request = Task.Run(() =>
            {
                try { return (int?)backend.RunElevated(script); }
                catch (LanFirewallDeclinedException) { return null; }
                catch (Exception error) { Debug.LogWarning("[LanFirewall] helper failed: " + error.Message); return -1; }
            });
        }

        private static void Pump()
        {
            if (_request != null && _request.IsCompleted)
            {
                int? exit = _request.IsFaulted ? -1 : _request.Result;
                _request = null;
                if (exit == null)
                {
                    Ask = LanFirewallAsk.Declined;
                    Debug.Log("[LanFirewall] the elevation prompt was declined; nothing changed");
                }
                else
                {
                    // ⚠️ THE READ DECIDES, NOT THE EXIT CODE. Whatever the helper said, the line only
                    // claims "allowed" once the firewall's own rules say so.
                    _confirming = true;
                    Debug.Log($"[LanFirewall] helper exited {exit}; reading again");
                }
                _stale = true;
            }

            if (_read != null && _read.IsCompleted)
            {
                var verdict = _read.IsFaulted || _read.Result == null ? LanFirewallVerdict.Unknown() : _read.Result;
                if (_read.IsFaulted) Debug.LogWarning("[LanFirewall] read failed: " + _read.Exception?.GetBaseException().Message);
                _read = null;
                Verdict = verdict;
                if (_confirming)
                {
                    _confirming = false;
                    if (verdict.State == LanFirewallState.Open) { Ask = LanFirewallAsk.None; AllowedCount++; }
                    else Ask = LanFirewallAsk.Failed;
                }
                else if (verdict.State == LanFirewallState.Open) Ask = LanFirewallAsk.None;
                string log = $"{verdict.State} checked={verdict.Checked} uncovered={verdict.Uncovered} " +
                             $"removable={verdict.RemovableBlockRuleIds.Count} ask={Ask}";
                if (log != _logged) { _logged = log; Debug.Log($"[LanFirewall] {_backend.ProgramPath}: {log}"); }
            }
        }

        private static void StartRead()
        {
            _stale = false;
            var backend = _backend;
            string program = backend.ProgramPath;
            int[] ports = { _port, LanBeacon.DiscoveryPort };
            _read = Task.Run(() => LanFirewallRules.Evaluate(LanFirewallRules.ParseProbe(backend.Probe()), program, ports,
                                                             Environment.ExpandEnvironmentVariables));
        }

        private static void HookFocus()
        {
            if (_focusHooked) return;
            _focusHooked = true;
            Application.focusChanged += focused => { if (focused) _stale = true; };
        }

        private static void Reset()
        {
            _read = null;
            _request = null;
            _stale = true;
            _confirming = false;
            _port = 0;
            _logged = "";
            Verdict = LanFirewallVerdict.Unknown();
            Ask = LanFirewallAsk.None;
            AllowedCount = 0;
        }

        /// <summary>A read or a request is still running in the background.</summary>
        public static bool Working => _read != null || _request != null;
    }

    /// <summary>
    /// Windows' side: PowerShell for the read (no window, no admin) and PowerShell through the
    /// "runas" verb for the change. Both use `-EncodedCommand`, so no path is ever re-quoted.
    /// </summary>
    internal sealed class WindowsLanFirewallBackend : ILanFirewallBackend
    {
        private const int Cancelled = 1223; // ERROR_CANCELLED: the UAC prompt was answered No
        private const int ReadTimeoutMs = 20000;
        private const int HelperTimeoutMs = 120000;

        public bool Supported { get; }
        public string ProgramPath { get; }

        public WindowsLanFirewallBackend()
        {
            Supported = (Application.platform == RuntimePlatform.WindowsPlayer ||
                         Application.platform == RuntimePlatform.WindowsEditor) && !Application.isBatchMode;
            ProgramPath = Supported ? CurrentProgram() : "";
        }

        private static string CurrentProgram()
        {
            try { return Process.GetCurrentProcess().MainModule.FileName; }
            catch { return Path.GetFullPath(Environment.GetCommandLineArgs()[0]); }
        }

        private static string PowerShell() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");

        public string Probe()
        {
            var info = new ProcessStartInfo(PowerShell(),
                "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + LanFirewallRules.Encode(LanFirewallRules.ProbeScript))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
            };
            using (var process = Process.Start(info))
            {
                if (process == null) return null;
                // Both pipes drain at once, so a chatty stderr can never stall the probe.
                Task<string> output = process.StandardOutput.ReadToEndAsync();
                Task<string> errors = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(ReadTimeoutMs))
                {
                    try { process.Kill(); } catch { }
                    return null;
                }
                errors.Wait(1000);
                return output.Wait(1000) ? output.Result : null;
            }
        }

        public int RunElevated(string script)
        {
            var info = new ProcessStartInfo(PowerShell(), LanFirewallRules.PowerShellArguments(script))
            {
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            Process process;
            try { process = Process.Start(info); }
            catch (Win32Exception error) when (error.NativeErrorCode == Cancelled) { throw new LanFirewallDeclinedException(); }
            if (process == null)
            {
                // No handle came back, so there is nothing to wait on; give the helper time, then let the read decide.
                Thread.Sleep(8000);
                return 0;
            }
            using (process)
            {
                if (!process.WaitForExit(HelperTimeoutMs)) return -1;
                try { return process.ExitCode; } catch { return 0; }
            }
        }
    }
}
