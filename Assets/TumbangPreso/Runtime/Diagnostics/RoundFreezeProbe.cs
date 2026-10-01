using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    // Opt-in witness only: observes normal host/client play without advancing it.
    public sealed class RoundFreezeProbe : MonoBehaviour
    {
        StreamWriter _writer;
        string _output;
        float _next;
        int _capturedRound = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-tp-freezetrace");
            if (at < 0 || at + 1 >= args.Length || args.Contains("-tp-tournament")) return;
            var root = new GameObject("~RoundFreezeProbe"); DontDestroyOnLoad(root);
            var probe = root.AddComponent<RoundFreezeProbe>();
            probe._output = Path.GetFullPath(args[at + 1]);
            Directory.CreateDirectory(Path.GetDirectoryName(probe._output));
            probe._writer = new StreamWriter(probe._output) { AutoFlush = true };
            probe._writer.WriteLine("utc,host,slot,round,roundActive,buffer,inProgress,left,breakActive,began,remaining,held,blocked,simulationTime,frameCount,frameWidth,frameHeight,x,y,z");
        }

        void Update()
        {
            if (_writer == null || Time.realtimeSinceStartup < _next) return;
            _next = Time.realtimeSinceStartup + .1f;
            var match = GameServices.Match; var round = GameServices.Round;
            if (match == null || round == null) return;
            var phase = HalftimePresentation.Instance;
            var frames = phase != null ? phase.GetComponent<RoundBreakFrame>() : null;
            var actor = round.PlayerAt(NetAuthority.LocalSlot);
            var position = actor != null ? actor.transform.position : Vector3.zero;
            object[] values = { DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond,
                NetAuthority.IsHost ? 1 : 0, NetAuthority.LocalSlot, match.RoundNumber,
                round.RoundActive ? 1 : 0, match.IsWarmupBuffer ? 1 : 0, match.MatchInProgress ? 1 : 0,
                round.TimeLeft, phase?.Active == true ? 1 : 0, phase?.Began ?? 0, phase?.Remaining ?? 0,
                PresentationClock.Held ? 1 : 0, PresentationClock.BlocksInput ? 1 : 0, Time.time,
                frames?.CapturedFrames ?? 0, frames?.Texture?.width ?? 0, frames?.Texture?.height ?? 0,
                position.x, position.y, position.z };
            _writer.WriteLine(string.Join(",", values.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            if (phase?.Active == true && phase.Remaining < phase.Duration - .5f && _capturedRound != match.RoundNumber)
            {
                _capturedRound = match.RoundNumber;
                ScreenCapture.CaptureScreenshot(Path.ChangeExtension(_output, null) + "-round-" + match.RoundNumber + ".png");
            }
        }

        void OnDestroy() => _writer?.Dispose();
    }
}
