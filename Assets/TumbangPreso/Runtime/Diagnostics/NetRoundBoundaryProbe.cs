using System;
using System.Globalization;
using System.IO;
using System.Linq;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    // Opt-in witness for the host-owned round timeline. No synthetic snapshots or clocks.
    public sealed class NetRoundBoundaryProbe : MonoBehaviour
    {
        private StreamWriter _writer;
        private MatchDirector _match;
        private int _intermissions, _cycle;
        private float _next, _started;

        private static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, key);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            string path = Argument("-tp-roundtrace");
            if (path == null || Environment.GetCommandLineArgs().Contains("-tp-tournament")) return;
            var root = new GameObject("~NetRoundBoundaryProbe");
            DontDestroyOnLoad(root);
            var probe = root.AddComponent<NetRoundBoundaryProbe>();
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            probe._writer = new StreamWriter(path) { AutoFlush = true };
            probe._writer.WriteLine("wall,networked,host,slot,round,active,warmup,inProgress,left,cycle,intermissions,pid");
            probe._started = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            var round = GameServices.Round;
            var match = GameServices.Match;
            if (match != _match)
            {
                if (_match != null) _match.IntermissionStarted -= OnIntermission;
                _match = match;
                if (_match != null) _match.IntermissionStarted += OnIntermission;
            }
            // Keep actions out of this timing test; readiness and all round transitions remain real.
            foreach (var brain in FindObjectsByType<AIController>(FindObjectsSortMode.None))
                brain.enabled = false;
            if (round != null && match != null)
            {
                if (_cycle == 0 && NetAuthority.IsNetworked && !NetAuthority.IsHost &&
                    NetAuthority.LocalSlot == 1 && match.RoundNumber == 1 && round.RoundActive &&
                    round.TimeLeft < UI.SceneFlow.SelectedRoundSeconds - 8)
                {
                    _cycle = 1;
                    Rejoin();
                }
                if (Time.realtimeSinceStartup >= _next)
                {
                    _next = Time.realtimeSinceStartup + .1f;
                    object[] row = { DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond,
                        NetAuthority.IsNetworked ? 1 : 0, NetAuthority.IsHost ? 1 : 0,
                        NetAuthority.LocalSlot, match.RoundNumber, round.RoundActive ? 1 : 0,
                        match.IsWarmupBuffer ? 1 : 0, match.MatchInProgress ? 1 : 0,
                        round.TimeLeft, _cycle, _intermissions, System.Diagnostics.Process.GetCurrentProcess().Id };
                    _writer.WriteLine(string.Join(",", row.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
                }
                if (NetAuthority.IsNetworked && match.RoundNumber >= 2 && round.RoundActive &&
                    round.TimeLeft < UI.SceneFlow.SelectedRoundSeconds - (NetAuthority.IsHost ? 8 : 5))
                {
                    _writer.Flush();
                    Application.Quit();
                }
            }
            if (Time.realtimeSinceStartup - _started > 150)
            {
                Debug.LogError("[RoundBoundary] timed out before the second live round");
                Application.Quit(1);
            }
        }

        private void OnIntermission(int next, int defender) => _intermissions++;

        private async void Rejoin()
        {
            try
            {
                var map = UI.SceneFlow.SelectedMap;
                int port = int.Parse(Argument("-tp-round-rejoin-port") ?? "8965", CultureInfo.InvariantCulture);
                Debug.Log("[RoundBoundary] restarting client transport and reloading arena");
                if (!await NetSession.Instance.StartClientAsync("127.0.0.1", port))
                    throw new InvalidOperationException("Round-boundary rejoin was refused");
                _cycle = 2;
                UI.SceneFlow.Go(map);
            }
            catch (Exception error) { Debug.LogException(error); Application.Quit(1); }
        }

        private void OnDestroy()
        {
            if (_match != null) _match.IntermissionStarted -= OnIntermission;
            _writer?.Dispose();
        }
    }
}
