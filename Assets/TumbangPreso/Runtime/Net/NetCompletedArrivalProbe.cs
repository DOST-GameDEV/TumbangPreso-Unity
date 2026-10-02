using System;
using System.IO;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TumbangPreso.Net
{
    // Opt-in observation of one naturally completed custom match and normal cold rejoin.
    public sealed class NetCompletedArrivalProbe : MonoBehaviour
    {
        [Serializable]
        private sealed class Receipt
        {
            public string role, phase, error, initialRecordId, recordId, scene;
            public bool passed, sawLive, originHumanSeats, admitted, hostStayedEnded = true;
            public bool recordHumanOrigins;
            public bool postJoinSpectator;
            public int pid, matchEndedEvents, recordReadyEvents, beforeEndEvents, beforeRecordEvents;
            public int sceneBefore, sceneAfter, rounds, roundSeconds, winner;
            public int postJoinSlot = -1;
            public long presentationMatch;
            public int[] scores, recordScores;
            public string scope = "Short custom Hero one-round/30-second cold completed arrival; no retained-seat, force finish, autorematch, AllBots, SDK or physical-input claim.";
        }
        private readonly Receipt _receipt = new Receipt();
        private string _folder;
        private float _began, _next;
        private bool _returning, _rejoining, _done;
        private MatchDirector _match;
        private MatchStatsCollector _stats;

        private static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, key);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            string folder = Argument("-tp-completed-arrival"), role = Argument("-tp-completed-role");
            if (folder == null || (role != "host" && role != "client") ||
                Environment.GetCommandLineArgs().Contains("-tp-tournament")) return;
            var root = new GameObject("~NetCompletedArrivalProbe"); DontDestroyOnLoad(root);
            var probe = root.AddComponent<NetCompletedArrivalProbe>();
            probe._folder = Path.GetFullPath(folder); Directory.CreateDirectory(probe._folder);
            probe._began = Time.realtimeSinceStartup; probe._receipt.role = role;
            probe._receipt.pid = System.Diagnostics.Process.GetCurrentProcess().Id;
            probe._receipt.phase = "starting";
            probe.ObserveServices(); SceneManager.sceneLoaded += probe.SceneLoaded; probe.Save();
        }

        private void ObserveServices()
        {
            if (GameServices.Match != _match)
            {
                if (_match != null) _match.MatchEnded -= Ended;
                _match = GameServices.Match; if (_match != null) _match.MatchEnded += Ended;
            }
            if (GameServices.Stats != _stats)
            {
                if (_stats != null) _stats.RecordReady -= Record;
                _stats = GameServices.Stats; if (_stats != null) _stats.RecordReady += Record;
            }
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode) => ObserveServices();

        private void Update()
        {
            if (_done) return;
            ObserveServices();
            if (Time.realtimeSinceStartup - _began > 110) { Finish(false, "Completed arrival did not finish within 110 seconds."); return; }
            if (GameLaunch.AllBots || Environment.GetCommandLineArgs().Contains("-tp-autorematch"))
            { Finish(false, "AllBots/autorematch is outside this scenario."); return; }
            var net = NetSession.Instance; var round = GameServices.Round;
            if (_receipt.phase == "starting" && net != null && net.IsNetworked)
            { _receipt.phase = "network_ready"; Save(); }
            if (_match == null || round == null) return;
            if (_match.MatchInProgress && round.RoundActive && !_receipt.sawLive)
            {
                _receipt.sawLive = true;
                _receipt.originHumanSeats = round.PlayerAt(0) != null && !round.PlayerAt(0).IsBot &&
                    round.PlayerAt(1) != null && !round.PlayerAt(1).IsBot;
                _receipt.rounds = _match.TotalRounds; _receipt.roundSeconds = (int)SceneFlow.SelectedRoundSeconds;
                if (!_receipt.originHumanSeats || _receipt.rounds != 1 || _receipt.roundSeconds != 30)
                { Finish(false, "The actual match did not use two human-origin seats and custom 1/30 rules."); return; }
                _receipt.phase = "live"; Save();
            }
            if (_receipt.role == "host") ObserveHost(); else ObserveClient();
            if (Time.realtimeSinceStartup >= _next) { _next = Time.realtimeSinceStartup + .5f; Save(); }
        }

        private void ObserveHost()
        {
            if (!_receipt.sawLive) return;
            if (_receipt.initialRecordId != null && _match.MatchInProgress)
            { _receipt.hostStayedEnded = false; Finish(false, "Host restarted before the completed arrival was verified."); return; }
            if (_match.MatchInProgress || _receipt.matchEndedEvents == 0 || !CaptureRecord()) return;
            if (_receipt.initialRecordId == null)
            {
                _receipt.initialRecordId = _receipt.recordId;
                _receipt.presentationMatch = _match.PresentationMatchId;
                _receipt.phase = "host_completed"; Save();
            }
            var client = Read("client");
            if (client != null && client.passed)
            {
                bool same = client.recordId == _receipt.initialRecordId && client.scores.SequenceEqual(_receipt.scores)
                    && _match.PresentationMatchId == _receipt.presentationMatch;
                Finish(same, same ? null : "Host/client terminal identity or scores disagree.");
            }
        }

        private void ObserveClient()
        {
            var host = Read("host");
            if (host == null || host.phase != "host_completed") return;
            if (!_returning && !_rejoining && _receipt.sawLive && !_match.MatchInProgress && CaptureRecord())
            {
                if (_receipt.recordId != host.initialRecordId) return;
                var result = FindFirstObjectByType<MatchResult>();
                if (result == null || !result.IsVisible) return;
                var buttons = FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .Where(button => button.name == "ResultMainMenu" && button.isActiveAndEnabled).ToArray();
                if (buttons.Length != 1) { Finish(false, "Expected exactly one current ResultMainMenu action."); return; }
                _returning = true; _receipt.initialRecordId = _receipt.recordId;
                _receipt.beforeEndEvents = _receipt.matchEndedEvents; _receipt.beforeRecordEvents = _receipt.recordReadyEvents;
                _receipt.sceneBefore = SceneManager.GetActiveScene().handle;
                _receipt.phase = "operator_return_home"; Save();
                // The actual current result action owns Stop and home navigation.
                buttons[0].onClick.Invoke();
            }
            var hub = TumpHub.Current;
            if (_returning && !_rejoining && hub != null && hub.Top is HubHome && hub.Canvas.enabled)
            { _rejoining = true; Rejoin(); }
            if (!_receipt.admitted || _match.MatchInProgress || _receipt.matchEndedEvents <= _receipt.beforeEndEvents ||
                _receipt.recordReadyEvents <= _receipt.beforeRecordEvents || !CaptureRecord()) return;
            var board = FindFirstObjectByType<MatchResult>();
            _receipt.sceneAfter = SceneManager.GetActiveScene().handle;
            bool same = board != null && board.IsVisible && _receipt.sceneAfter != _receipt.sceneBefore &&
                _receipt.recordId == host.initialRecordId && _receipt.scores.SequenceEqual(host.scores);
            if (same) Finish(true, null);
        }

        private async void Rejoin()
        {
            try
            {
                int port = int.Parse(Argument("-tp-completed-port"));
                var net = NetSession.Instance;
                _receipt.beforeEndEvents = _receipt.matchEndedEvents;
                _receipt.beforeRecordEvents = _receipt.recordReadyEvents;
                _receipt.phase = "rejoin_started"; Save();
                // The result MAIN MENU already called public Stop. Trusted Seating alone loads the arena.
                if (net == null || !await net.StartClientAsync("127.0.0.1", port) || !await net.WaitForConnectionAsync())
                    throw new InvalidOperationException("The ordinary completed-match rejoin was not admitted.");
                _receipt.admitted = true; _receipt.phase = "rejoin_admitted"; Save();
                _receipt.postJoinSlot = net.LocalSlot;
                _receipt.postJoinSpectator = GameLaunch.Spectator;
                Save();
            }
            catch (Exception error) { Finish(false, error.Message); }
        }

        private bool CaptureRecord()
        {
            var record = _stats?.Last;
            if (record == null || string.IsNullOrWhiteSpace(record.MatchId) || record.Players == null || record.Players.Length != 4) return false;
            _receipt.scores = Enumerable.Range(0, 4).Select(_match.ScoreFor).ToArray();
            _receipt.recordScores = new int[4];
            for (int slot = 0; slot < 4; slot++)
            {
                var lines = record.Players.Where(line => line != null && line.Slot == slot).ToArray();
                if (lines.Length != 1) return false;
                _receipt.recordScores[slot] = lines[0].Score;
            }
            if (!_receipt.scores.SequenceEqual(_receipt.recordScores) || record.Rounds != 1) return false;
            _receipt.recordHumanOrigins = record.Players.Where(line => line.Slot == 0 || line.Slot == 1).All(line => !line.IsBot);
            if (!_receipt.recordHumanOrigins) return false;
            _receipt.recordId = record.MatchId; _receipt.winner = record.WinningSlot;
            return true;
        }
        private void Ended(int winner) { _receipt.matchEndedEvents++; Save(); }
        private void Record(MatchRecord record) { _receipt.recordReadyEvents++; Save(); }
        private Receipt Read(string role)
        {
            try { return JsonUtility.FromJson<Receipt>(File.ReadAllText(Path.Combine(_folder, role + ".json"))); }
            catch (Exception) { return null; }
        }
        private void Save()
        {
            _receipt.scene = SceneManager.GetActiveScene().name;
            File.WriteAllText(Path.Combine(_folder, _receipt.role + ".json"), JsonUtility.ToJson(_receipt, true));
        }
        private void Finish(bool passed, string error)
        {
            if (_done) return; _done = true; _receipt.passed = passed; _receipt.error = error;
            _receipt.phase = passed ? "passed" : "failed"; Save();
            Debug.Log("[CompletedArrival] " + _receipt.role + ": " + _receipt.phase + (error == null ? "" : " " + error));
            Application.Quit(passed ? 0 : 1);
        }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            if (_match != null) _match.MatchEnded -= Ended;
            if (_stats != null) _stats.RecordReady -= Record;
        }
    }
}
