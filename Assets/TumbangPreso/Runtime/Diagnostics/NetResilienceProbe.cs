using System;
using System.Collections;
using System.IO;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.Diagnostics
{
    // Explicit local-peer fixture. Uses ordinary lobby seating, start/readiness,
    // transport Stop and durable profiles. Never installed in tournament play.
    public sealed class NetResilienceProbe : MonoBehaviour
    {
        [Serializable] private sealed class State
        {
            public string role, scene, error, link, tier, notice, cause, lastDeparture;
            public bool networked, active, spectator, host, sawLive, revokedAtLoss, oldRoundStopped;
            public int local, round, ended, epoch, departures;
            public int[] scores;
            public long match, rtt;
            public double time, silence;
        }
        private string _path, _role;
        private double _began, _next, _seatAt, _lostAt = -1;
        private bool _started, _readySent, _live, _revoked, _capturing, _done, _rejoining;
        private ReadyGate _gate;
        private RoundDirector _oldRound;
        private MatchDirector _oldMatch;
        private int _ended;
        private StreamWriter _trace;
        private static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            if (Arg("-tp-resilience") == null || Environment.GetCommandLineArgs().Contains("-tp-tournament")) return;
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            string path = Arg("-tp-resilience");
            if (path == null || Environment.GetCommandLineArgs().Contains("-tp-tournament")) return;
            var go = new GameObject("~NetResilienceProbe"); DontDestroyOnLoad(go);
            var probe = go.AddComponent<NetResilienceProbe>();
            probe._path = Path.GetFullPath(path); probe._role = Arg("-tp-resilience-role") ?? "host";
            probe._began = Time.realtimeSinceStartupAsDouble;
            Directory.CreateDirectory(Path.GetDirectoryName(probe._path));
            probe._trace = new StreamWriter(probe._path + ".trace") { AutoFlush = true };
            NetSession.ClientDisconnected += probe.Lost;
        }
        private void Lost(string reason)
        {
            _lostAt = Time.realtimeSinceStartupAsDouble;
            _revoked = MatchAbandon.AuthorityRevoked && !NetAuthority.ShouldResolve();
        }
        private void Ended(int winner) => _ended++;
        private State Read()
        {
            var net = NetSession.Instance; var match = GameServices.Match;
            var own = GameServices.Round?.PlayerAt(NetAuthority.LocalSlot);
            var notice = TumpHub.Current?.Find<HubDisconnectedNotice>();
            return new State { role = _role, scene = SceneManager.GetActiveScene().name,
                time = Time.realtimeSinceStartupAsDouble - _began, local = NetAuthority.LocalSlot,
                networked = net?.IsNetworked == true, host = net?.IsNetworked == true && net.IsHost,
                active = GameServices.Round?.RoundActive == true, round = match?.RoundNumber ?? 0,
                match = MatchRpc.Instance?.PresentationMatchId ?? 0, spectator = GameLaunch.Spectator,
                epoch = own?.MovementEpoch ?? 0, scores = Enumerable.Range(0, 4).Select(i => match?.ScoreFor(i) ?? 0).ToArray(),
                link = NetSession.Link.ToString(), rtt = NetSession.LinkRttMs, silence = net?.LinkSilenceSeconds ?? -1,
                tier = FindAnyObjectByType<ConnectionQualityIcon>()?.Tier.ToString() ?? "Absent",
                notice = notice != null ? notice.Message : "", cause = MatchAbandon.Cause.ToString(),
                departures = MatchRpc.Instance?.PeerDepartureNotices ?? 0,
                lastDeparture = MatchRpc.Instance?.LastPeerDepartureText ?? "",
                sawLive = _live, revokedAtLoss = _revoked, ended = _ended,
                oldRoundStopped = _oldRound == null || !_oldRound.RoundActive };
        }
        private void Save(string suffix, State state) => File.WriteAllText(_path + suffix, JsonUtility.ToJson(state, true));
        private void Update()
        {
            if (_done) return;
            try
            {
                if (Time.realtimeSinceStartupAsDouble - _began > 240)
                    throw new InvalidOperationException("Resilience fixture exceeded its bounded window.");
                var rpc = MatchRpc.Instance; var net = NetSession.Instance;
                if (!_live && _role == "observer" && rpc != null && net?.IsAdmitted == true &&
                    !net.Lobby.MatchInProgress && !GameLaunch.Spectator && Time.realtimeSinceStartupAsDouble >= _seatAt)
                { _seatAt = Time.realtimeSinceStartupAsDouble + 1; rpc.RequestSeatServerRpc(-1); }
                if (!_live && _role == "host" && !_started && net?.IsAdmitted == true &&
                    net.Lobby.PlayingPeerCount() >= 2 && net.Lobby.Peers.Any(p => p.Spectator) &&
                    TumpHub.Current?.Top is HubLobby)
                { _started = true; TumpHub.Current.Host.SelectMap(SceneFlow.Eskinita); TumpHub.Current.Host.StartGame(); }
                var gate = FindFirstObjectByType<ReadyGate>();
                if (gate != _gate) { _gate = gate; _readySent = false; }
                if (!_readySent && gate != null && gate.AwaitingReady && rpc != null) _readySent = rpc.DeclareReadyServerRpc();
                if (GameServices.Round?.RoundActive == true && GameServices.Match?.IsWarmupBuffer != true)
                {
                    foreach (var brain in FindObjectsByType<AIController>()) brain.enabled = false;
                    foreach (var input in FindObjectsByType<PlayerInputReader>()) input.enabled = false;
                    foreach (var actor in GameServices.Round.Players) { actor.Intent.Clear(); actor.Intent.Parked = true; }
                    Hud.Instance?.SetSpectatorControlsVisible(false);
                    if (!_live)
                    {
                        _live = true; _oldRound = GameServices.Round; _oldMatch = GameServices.Match;
                        _oldMatch.MatchEnded += Ended; Save(".ready", Read());
                    }
                }
                if (File.Exists(_path + ".stop"))
                { File.Delete(_path + ".stop"); net?.Stop(); Save(".stopped", Read()); }
                if (!_rejoining && File.Exists(_path + ".rejoin"))
                {
                    string address = File.ReadAllText(_path + ".rejoin"); File.Delete(_path + ".rejoin");
                    Rejoin(address);
                }
                if (Time.realtimeSinceStartupAsDouble >= _next)
                {
                    _next = Time.realtimeSinceStartupAsDouble + .2;
                    var state = Read(); Save(".state", state); _trace.WriteLine(JsonUtility.ToJson(state));
                }
                if (!_capturing && File.Exists(_path + ".shot"))
                { File.Delete(_path + ".shot"); _capturing = true; StartCoroutine(Capture(".png")); }
                if (_lostAt >= 0 && !_capturing && !File.Exists(_path + ".lost") &&
                    Time.realtimeSinceStartupAsDouble - _lostAt >= 6 && TumpHub.Current?.Find<HubDisconnectedNotice>() != null)
                { Save(".lost", Read()); _capturing = true; StartCoroutine(Capture(".lost.png")); }
                if (File.Exists(_path + ".finish"))
                { Save(".result", Read()); _done = true; Application.Quit(); }
            }
            catch (Exception error)
            { var state = Read(); state.error = error.ToString(); Save(".result", state); _done = true; Application.Quit(1); }
        }
        private IEnumerator Capture(string suffix)
        {
            yield return new WaitForEndOfFrame();
            var picture = ScreenCapture.CaptureScreenshotAsTexture();
            if (picture != null) { File.WriteAllBytes(_path + suffix, picture.EncodeToPNG()); Destroy(picture); }
            _capturing = false;
        }
        private async void Rejoin(string address)
        {
            _rejoining = true;
            try
            {
                var hub = TumpHub.Current;
                if (hub == null || _lostAt < 0 || NetSession.Instance?.IsNetworked == true)
                    throw new InvalidOperationException("Rejoin requires the actual disconnected Home state.");
                if (hub.Find<HubDisconnectedNotice>()?.FirstFocus is UnityEngine.UI.Button acknowledge)
                    acknowledge.onClick.Invoke();
                if (_oldMatch != null) _oldMatch.MatchEnded -= Ended;
                _oldMatch = null; _oldRound = null; _live = false; _lostAt = -1;
                string error = await hub.Host.Join(address.Trim());
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                Save(".joined", Read());
            }
            catch (Exception error)
            { var state = Read(); state.error = error.ToString(); Save(".result", state); _done = true; Application.Quit(1); }
            finally { _rejoining = false; }
        }
        private void OnDestroy()
        {
            NetSession.ClientDisconnected -= Lost;
            if (_oldMatch != null) _oldMatch.MatchEnded -= Ended;
            _trace?.Dispose();
        }
    }
}
