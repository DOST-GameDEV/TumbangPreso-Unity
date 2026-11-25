using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.Diagnostics
{
    /// <summary>
    /// Opt-in witness for the final whistle and the result board (`MatchEndBeat`) in a real player, offline or as a
    /// networked host, client or spectator. `tools/net_match_end.py` launches it and judges the traces.
    ///
    ///   -tp-matchend DIR               trace folder (one CSV, a result JSON and screenshots per role)
    ///   -tp-matchend-role R            offline | host | client | spectator
    ///   -tp-matchend-map SCENE         offline: the court (networked peers use -tp-map)
    ///   -tp-matchend-mode M            offline: classic | hero
    ///   -tp-matchend-rounds N, -tp-matchend-seconds N, -tp-matchend-target N   offline rules
    ///   -tp-matchend-case C            air (Updraft, a real Airburst release and a long fall at the whistle) | none
    ///   -tp-matchend-rejoin            client: the connection drops during the beat and CONNECTION LOST's REJOIN brings it back
    ///   -tp-matchend-rematch-after S   seconds of board before this peer presses the real REMATCH (default 3; spectators never vote)
    ///   -tp-matchend-peers N           host: START GAME once N playing peers and one spectator are seated (default 2)
    ///
    /// A draw is not driven here: bots always score, and the taya earns defence points by standing. The draw's beat is
    /// covered by `MatchEndBeatTests` with tied scores.
    ///
    /// ⚠️ IT NEVER SCORES AND NEVER ENDS A MATCH. The whistle is the real round clock and the result is the host's own.
    /// The air case puts bodies in the air with the game's own state changes (Updraft's flight, the AIRBURST hazard's
    /// release, a teleport up) on the host, which is where those changes are always made.
    /// </summary>
    public sealed class NetMatchEndProbe : MonoBehaviour
    {
        private const string Switch = "-tp-matchend";
        private string _dir, _role, _case;
        private bool _rejoin, _rejoined, _caseFired, _finished;
        private Scene _begunScene;
        private StreamWriter _csv;
        private float _started, _nextSample;
        private MatchDirector _hooked;

        private sealed class MatchTrace
        {
            public long id; public string map; public int totalRounds; public int scoreTarget; public string mode;
            public double whistleWall = -1, revealWall = -1, boardWall = -1, firstBannerWall = -1;
            public int reveals, bannerSamples, boardSamplesDuringBeat, corrections = -1;
            public bool hostTimed, rematchOffered, rematchHidden, timePausedAfterReveal, liveRoundSeen;
            public int winner = -9; public int[] scores; public int finalRound; public float revealAge = -1;
            public List<string> restAtReveal = new List<string>();
            public List<string> restLate = new List<string>();
            public bool leftoverInNext, ended; public string endPath = "";
            public float firstSeenAge = -1;
            public int clips = -1, clipsShown = -1;
        }

        private readonly List<MatchTrace> _matches = new List<MatchTrace>();
        private MatchTrace _current;
        private readonly List<string> _notes = new List<string>();
        private int _shots;

        private static string Arg(string key)
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, key);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        }

        private static bool Has(string key) => Environment.GetCommandLineArgs().Contains(key);

        private static int IntArg(string key, int fallback)
            => int.TryParse(Arg(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;

        /// <summary>The rules this run plays, pinned before the first scene on the peer that owns them (offline or the host).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ConfigureRules()
        {
            if (Arg(Switch) == null) return;
            string role = Arg("-tp-matchend-role") ?? "offline";
            if (role != "offline" && role != "host") return;
            var mode = string.Equals(Arg("-tp-matchend-mode"), "hero", StringComparison.OrdinalIgnoreCase) ? GameMode.HeroStrike : GameMode.Classic;
            var rules = CustomGameRules.Defaults(mode);
            rules.Rounds = Mathf.Clamp(IntArg("-tp-matchend-rounds", 1), CustomGameRules.MinRounds, CustomGameRules.MaxRounds);
            rules.RoundSeconds = Mathf.Clamp(IntArg("-tp-matchend-seconds", 30), CustomGameRules.MinRoundSeconds, CustomGameRules.MaxRoundSeconds);
            rules.ScoreTarget = Mathf.Clamp(IntArg("-tp-matchend-target", 0), CustomGameRules.MinScoreTarget, CustomGameRules.MaxScoreTarget);
            rules.ManualReady = false;
            rules.MapVote = false; // the court under test is the one named, not a vote's
            SceneFlow.PinSelectedRules(rules);
            SceneFlow.SelectedMode = mode;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            string dir = Arg(Switch);
            if (dir == null) return;
            var root = new GameObject("~NetMatchEndProbe");
            DontDestroyOnLoad(root);
            var probe = root.AddComponent<NetMatchEndProbe>();
            probe._dir = Path.GetFullPath(dir);
            probe._role = Arg("-tp-matchend-role") ?? "offline";
            probe._case = Arg("-tp-matchend-case") ?? "none";
            probe._rejoin = Has("-tp-matchend-rejoin");
            Directory.CreateDirectory(probe._dir);
            probe._csv = new StreamWriter(Path.Combine(probe._dir, probe._role + ".csv")) { AutoFlush = true };
            probe._csv.WriteLine("wall,role,pid,scene,networked,host,slot,spectator,match,round,total,inProgress,completed," +
                                 "roundActive,timeLeft,beatActive,beatMatch,hostTimed,beatAge,revealed,revealCount,banner,board," +
                                 "timeScale,s0,s1,s2,s3,rest0,rest1,rest2,rest3,y0,y1,y2,y3");
            probe._started = Time.realtimeSinceStartup;
            MatchEndBeat.Revealed += probe.OnRevealed;
            Application.runInBackground = true;
            Debug.Log($"[MatchEndProbe] role={probe._role} case={probe._case} rejoin={probe._rejoin} dir={probe._dir}");
            if (probe._role == "offline") probe.StartCoroutine(probe.Offline());
        }

        private static double Wall => DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond;

        // -------------------------------------------------------------------------------------- offline driver

        private IEnumerator Offline()
        {
            yield return null;
            string map = Arg("-tp-matchend-map") ?? SceneFlow.Eskinita;
            var rules = SceneFlow.SelectedRules;
            SceneFlow.Networked = false;
            SceneFlow.SelectedMap = map;
            GameLaunch.AllBots = false; GameLaunch.Spectator = false; GameLaunch.SoloSeat = 1;
            Debug.Log("[MatchEndProbe] offline rules " + CustomGameRules.ToWire(rules) + " on " + map);
            yield return SceneManager.LoadSceneAsync(map);
            yield return BeginOfflineMatch();
        }

        /// <summary>Offline, a REMATCH from the board loads the next court; it is begun the way the first one was.</summary>
        private void StepOfflineRematch()
        {
            if (_role != "offline" || _matches.Count == 0 || _matches[_matches.Count - 1].reveals == 0) return;
            var scene = SceneManager.GetActiveScene();
            if (scene == _begunScene || GameServices.Round == null || GameServices.Match == null
                || GameServices.Match.MatchInProgress) return;
            StartCoroutine(BeginOfflineMatch());
        }

        private IEnumerator BeginOfflineMatch()
        {
            _begunScene = SceneManager.GetActiveScene();
            float waited = 0;
            while (waited < 30f && (GameServices.Round == null || GameServices.Round.Players.Count < Balance.PlayerCount))
            { waited += Time.unscaledDeltaTime; yield return null; }
            // ⚠️ THE COURT BEGINS ITS OWN MATCH after its arrival. Beginning the runner here as well put a second match
            // on top of the first (2026-11-26 harness: every offline court "never ended", each traced a restart), so it
            // is only begun when the court has not begun one by itself.
            var runner = FindFirstObjectByType<SliceRunner>();
            for (float t = 0; t < 120f; t += Time.unscaledDeltaTime)
            {
                if (GameServices.Round != null && GameServices.Round.RoundActive) break;
                if (t > 45f && runner != null && !runner.Running) runner.Begin();
                var gate = FindFirstObjectByType<ReadyGate>();
                if (gate != null && gate.AwaitingReady && !gate.CountingDown) gate.StartLocalCountdown();
                yield return null;
            }
        }

        // -------------------------------------------------------------------------------------- per frame

        private void Update()
        {
            if (_finished) return;
            var match = GameServices.Match;
            if (match != _hooked)
            {
                if (_hooked != null) _hooked.MatchEnded -= OnMatchEnded;
                _hooked = match;
                if (_hooked != null) _hooked.MatchEnded += OnMatchEnded;
            }

            TrackMatch(match);
            FireCase(match);
            StepBeat(match);
            StepRejoin();
            StepOfflineRematch();
            StepHostStart();
            StepSpectator();
            StepLobbyReady();
            // The opening film is checked on its own (`OpeningGroundingTests`); a run of endings does not sit through it.
            if (Settings.SettingsStore.Current.CinematicCameraMotion) Settings.SettingsStore.Current.CinematicCameraMotion = false;

            if (Time.realtimeSinceStartup >= _nextSample) { _nextSample = Time.realtimeSinceStartup + 0.1f; Sample(match); }

            if (Time.realtimeSinceStartup - _started > IntArg("-tp-matchend-timeout", 300))
            {
                _notes.Add("timed out");
                Finish(false);
            }
        }

        private void TrackMatch(MatchDirector match)
        {
            if (match == null || match.PresentationMatchId == 0) return;
            if (_current != null && _current.id == match.PresentationMatchId) return;
            if (!match.MatchInProgress && !match.HasCompleted) return;

            if (_current != null)
            {
                // A new match id: the previous one is done. A board or banner left standing is a leftover.
                _current.leftoverInNext = MatchResult.BoardShowing || Hud.MatchOverShowing;
            }
            _current = new MatchTrace
            {
                id = match.PresentationMatchId, map = SceneManager.GetActiveScene().name, totalRounds = match.TotalRounds,
                scoreTarget = SceneFlow.SelectedRules.ScoreTarget, mode = SceneFlow.SelectedMode.ToString(),
            };
            _matches.Add(_current);
            _caseFired = false;
            Debug.Log($"[MatchEndProbe] match {_current.id} on {_current.map} ({_current.mode}, {_current.totalRounds} rounds, target {_current.scoreTarget})");
        }

        private void OnMatchEnded(int winner)
        {
            var match = GameServices.Match;
            if (_current == null || match == null || match.PresentationMatchId != _current.id) return;
            if (_current.whistleWall < 0) _current.whistleWall = Wall;
            _current.ended = true;
            _current.finalRound = match.RoundNumber;
            _current.endPath = match.RoundNumber < match.TotalRounds ? "score-target" : "rounds";
            Debug.Log($"[MatchEndProbe] whistle match={_current.id} round={match.RoundNumber}/{match.TotalRounds} winner={winner} path={_current.endPath}");
        }

        private void OnRevealed(long id, int winner)
        {
            var match = GameServices.Match;
            if (_current == null || id != _current.id || match == null) return;
            _current.reveals++;
            _current.revealWall = Wall;
            _current.winner = winner;
            _current.scores = Enumerable.Range(0, Balance.PlayerCount).Select(match.ScoreFor).ToArray();
            _current.revealAge = MatchEndBeat.Instance != null ? MatchEndBeat.Instance.Age : -1;
            _current.hostTimed = MatchEndBeat.Instance != null && MatchEndBeat.Instance.HostTimed;
            _current.corrections = MatchEndBeat.Instance != null ? MatchEndBeat.Instance.Corrections : -1;
            _current.clips = MatchEndBeat.Instance != null ? MatchEndBeat.Instance.ClipCount : -1;
            _current.clipsShown = MatchEndBeat.Instance != null ? MatchEndBeat.Instance.ClipsShown : -1;
            _current.restAtReveal = Bodies();
            StartCoroutine(AfterReveal(_current));
            Debug.Log($"[MatchEndProbe] reveal match={id} winner={winner} age={_current.revealAge:0.00} host={_current.hostTimed} " +
                      $"scores={string.Join("/", _current.scores)} bodies={string.Join(" ", _current.restAtReveal)}");
        }

        private IEnumerator AfterReveal(MatchTrace trace)
        {
            yield return null; yield return null;
            var result = FindFirstObjectByType<MatchResult>();
            if (result != null && result.IsVisible) trace.boardWall = Wall;
            var rematch = typeof(MatchResult).GetField("_rematch", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(result) as UnityEngine.UI.Button;
            trace.rematchOffered = rematch != null && rematch.gameObject.activeInHierarchy && rematch.interactable;
            trace.rematchHidden = rematch == null || !rematch.gameObject.activeInHierarchy;
            trace.timePausedAfterReveal = Time.timeScale == 0;
            for (float t = 0; t < 1.2f; t += Time.unscaledDeltaTime) yield return null;
            trace.restLate = Bodies();
            Shot("board");

            // The real REMATCH, from the real board, after this peer has looked at it for a while.
            float after = Mathf.Max(0, IntArg("-tp-matchend-rematch-after", 3)) - 1.2f;
            for (float t = 0; t < after; t += Time.unscaledDeltaTime) yield return null;
            if (result == null) result = FindFirstObjectByType<MatchResult>();
            if (result != null && result.IsVisible && _current == trace)
            {
                result.RequestRematch();
                _notes.Add($"rematch pressed on match {trace.id}");
            }
        }

        private float _nextStartCheck, _nextSeatRequest, _nextReady;
        private bool _startSent;

        /// <summary>Host: START GAME from the visible room once the players and one spectator are seated.</summary>
        private void StepHostStart()
        {
            if (_role != "host" || _startSent || Time.realtimeSinceStartup < _nextStartCheck) return;
            _nextStartCheck = Time.realtimeSinceStartup + 0.5f;
            var net = NetSession.Instance;
            var hub = UI.Hub.TumpHub.Current;
            if (net == null || !net.IsAdmitted || hub == null || !(hub.Top is UI.Hub.HubLobby)) return;
            int peers = IntArg("-tp-matchend-peers", 2);
            if (net.Lobby.PlayingPeerCount() < peers || !net.Lobby.Peers.Any(p => p.Spectator)) return;
            // A custom room's START GAME waits for its guests' READY (`HostBeginCharacterSelection`); pressed earlier it
            // does nothing, and it is pressed once.
            if (MatchRpc.Instance == null || !MatchRpc.Instance.LobbyGuestsReady) return;
            _startSent = true;
            string map = Arg("-tp-matchend-map") ?? SceneFlow.Eskinita;
            hub.Host.SelectMap(map);
            hub.Host.StartGame();
            _notes.Add("host pressed START GAME on " + map);
        }

        /// <summary>A seated guest presses READY in the room and LOCK IN at character select, as a player does.</summary>
        private void StepLobbyReady()
        {
            if (_role == "offline" || _role == "host" || _role == "spectator" || GameLaunch.Spectator
                || Time.realtimeSinceStartup < _nextReady) return;
            var net = NetSession.Instance;
            var rpc = MatchRpc.Instance;
            if (net == null || !net.IsAdmitted || rpc == null || net.Lobby.MatchInProgress
                || FindFirstObjectByType<ReadyGate>() != null) return;
            _nextReady = Time.realtimeSinceStartup + 1.0f;
            rpc.DeclareReadyServerRpc(true);
        }

        /// <summary>Spectator: ask the room for a watcher's place before the match starts.</summary>
        private void StepSpectator()
        {
            if (_role != "spectator" || GameLaunch.Spectator || Time.realtimeSinceStartup < _nextSeatRequest) return;
            var net = NetSession.Instance;
            var rpc = MatchRpc.Instance;
            if (net == null || !net.IsAdmitted || rpc == null || net.Lobby.MatchInProgress) return;
            _nextSeatRequest = Time.realtimeSinceStartup + 1.0f;
            rpc.RequestSeatServerRpc(-1);
        }

        private static List<string> Bodies()
        {
            var list = new List<string>();
            var round = GameServices.Round;
            if (round == null) return list;
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var body = round.PlayerAt(slot);
                if (body == null) { list.Add(slot + ":absent"); continue; }
                float gap = GroundGap(body);
                list.Add(string.Format(CultureInfo.InvariantCulture, "{0}:{1}:{2}:{3:0.000}:{4:0.000}", slot,
                    body.RestsForMatchEnd ? "rest" : "unrested", body.IsGrounded ? "grounded" : "air", body.transform.position.y, gap));
            }
            return list;
        }

        /// <summary>Feet above the nearest floor under the body that is not a body, a tsinelas or the lata.</summary>
        private static float GroundGap(CharacterMotor body)
        {
            var hits = Physics.RaycastAll(body.transform.position + Vector3.up * 0.3f, Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.PositiveInfinity;
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.GetComponentInParent<CharacterMotor>() != null
                    || hit.collider.GetComponentInParent<Slipper>() != null || hit.collider.GetComponentInParent<Lata>() != null) continue;
                best = Mathf.Min(best, hit.distance - 0.3f);
            }
            return float.IsInfinity(best) ? 99f : best;
        }

        private void StepBeat(MatchDirector match)
        {
            var beat = MatchEndBeat.Instance;
            if (_current == null || beat == null || !beat.Active || beat.MatchId != _current.id) return;
            if (_current.firstSeenAge < 0) _current.firstSeenAge = beat.Age;
            if (_current.whistleWall < 0) _current.whistleWall = Wall;
            if (!beat.IsRevealed)
            {
                if (Hud.MatchOverShowing) { _current.bannerSamples++; if (_current.firstBannerWall < 0) _current.firstBannerWall = Wall; }
                if (MatchResult.BoardShowing) _current.boardSamplesDuringBeat++;
                if (_shots == 0 && beat.Age >= 0.6f) Shot("banner");
                else if (_shots == 1 && beat.Age >= MatchEndBeat.Duration - 0.3f) Shot("cover");
            }
        }

        private void Shot(string name)
        {
            _shots++;
            if (Application.isBatchMode || _current == null) return;
            string path = Path.Combine(_dir, $"{_role}-{_matches.Count}-{name}.png");
            ScreenCapture.CaptureScreenshot(path);
        }

        private void FireCase(MatchDirector match)
        {
            var round = GameServices.Round;
            if (_caseFired || _current == null || match == null || round == null || !NetAuthority.ShouldResolve()) return;
            if (!match.MatchInProgress || match.RoundNumber != match.TotalRounds || !round.RoundActive) return;

            if (_case != "air" || round.TimeLeft > 0.9f) return;
            _caseFired = true;

            var caster = round.PlayerAt(0);
            var flyer = round.PlayerAt(NetAuthority.IsNetworked ? 2 : 1);
            var faller = round.PlayerAt(3);
            if (flyer != null) flyer.BeginFlight(AmihanRules.UpdraftHeight, AmihanRules.UpdraftRiseSeconds, AmihanRules.UpdraftDescentSpeed);
            if (faller != null) faller.Teleport(faller.SpawnPosition + Vector3.up * 45f);
            // AIRBURST's real release from the taya's mark toward the attackers: everyone in its fan is put mid-air where the
            // cutscene would have left them and carried on (relayed to a client's own body by the host's ordinary path).
            if (caster != null)
            {
                Vector3 toward = Vector3.zero;
                foreach (var body in round.Players) if (body != null && body != caster) toward += body.transform.position - caster.transform.position;
                toward.y = 0; if (toward.sqrMagnitude < 1e-4f) toward = Vector3.forward;
                var storm = Abilities.AmihanStorm.Spawn(caster.transform.position, toward.normalized, caster.PlayerSlot, null);
                storm.Release(afterCutscene: true);
                Destroy(storm.gameObject, 4f);
            }
            _notes.Add($"air at timeLeft={round.TimeLeft:0.00}: updraft seat {flyer?.PlayerSlot}, fall seat {faller?.PlayerSlot}, airburst from seat {caster?.PlayerSlot}");
            Debug.Log("[MatchEndProbe] " + _notes[_notes.Count - 1]);
        }

        private void StepRejoin()
        {
            if (!_rejoin || _rejoined || _role != "client" || _current == null) return;
            var beat = MatchEndBeat.Instance;
            if (beat == null || !beat.Active || beat.MatchId != _current.id || beat.Age < 0.5f) return;
            _rejoined = true;
            _notes.Add($"connection dropped during the beat at age {beat.Age:0.00}");
            StartCoroutine(Rejoin());
        }

        /// <summary>
        /// The player's own path back: the transport goes without this peer asking (as a dropped link does), CONNECTION
        /// LOST comes up at HOME, and its REJOIN joins the same room again with the same token.
        /// </summary>
        private IEnumerator Rejoin()
        {
            // The harness cuts this peer's link (`tools/net_match_end.py` watches for the request): a real timeout, with no
            // local shutdown anywhere, which is what a player whose Wi-Fi drops goes through.
            Debug.Log("[MatchEndProbe] asking the harness to cut this peer's link during the beat");
            File.WriteAllText(Path.Combine(_dir, _role + ".drop"), "cut");
            UI.Hub.HubDisconnectedNotice notice = null;
            for (float t = 0; t < 45f && notice == null; t += Time.unscaledDeltaTime)
            {
                notice = UI.Hub.TumpHub.Current?.Find<UI.Hub.HubDisconnectedNotice>();
                yield return null;
            }
            if (notice == null) { _notes.Add("no CONNECTION LOST notice after the drop"); Finish(false); yield break; }
            string offered = notice.RejoinOffered;
            _notes.Add("CONNECTION LOST offered REJOIN to [" + offered + "]");
            if (string.IsNullOrEmpty(offered)) { Finish(false); yield break; }
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime) yield return null;
            notice.Rejoin();
            for (float t = 0; t < 60f && NetSession.Instance?.IsAdmitted != true; t += Time.unscaledDeltaTime) yield return null;
            if (NetSession.Instance?.IsAdmitted != true) { _notes.Add("REJOIN was never admitted"); Finish(false); yield break; }
            _notes.Add($"REJOIN admitted to seat {NetAuthority.LocalSlot}");
        }

        private void Sample(MatchDirector match)
        {
            var round = GameServices.Round;
            var beat = MatchEndBeat.Instance;
            var row = new List<object>
            {
                Wall, _role, System.Diagnostics.Process.GetCurrentProcess().Id, SceneManager.GetActiveScene().name,
                NetAuthority.IsNetworked ? 1 : 0, NetAuthority.IsHost ? 1 : 0, NetAuthority.LocalSlot, GameLaunch.Spectator ? 1 : 0,
                match != null ? match.PresentationMatchId : 0, match != null ? match.RoundNumber : -1, match != null ? match.TotalRounds : -1,
                match != null && match.MatchInProgress ? 1 : 0, match != null && match.HasCompleted ? 1 : 0,
                round != null && round.RoundActive ? 1 : 0, round != null ? round.TimeLeft : -1,
                beat != null && beat.Active ? 1 : 0, beat != null ? beat.MatchId : 0, beat != null && beat.HostTimed ? 1 : 0,
                beat != null && beat.Active ? beat.Age : -1, beat != null && beat.IsRevealed ? 1 : 0, beat != null ? beat.RevealCount : 0,
                Hud.MatchOverShowing ? 1 : 0, MatchResult.BoardShowing ? 1 : 0, Time.timeScale,
            };
            for (int s = 0; s < Balance.PlayerCount; s++) row.Add(match != null ? match.ScoreFor(s) : 0);
            for (int s = 0; s < Balance.PlayerCount; s++) { var b = round?.PlayerAt(s); row.Add(b != null && b.RestsForMatchEnd ? 1 : 0); }
            for (int s = 0; s < Balance.PlayerCount; s++) { var b = round?.PlayerAt(s); row.Add(b != null ? b.transform.position.y : float.NaN); }
            _csv.WriteLine(string.Join(",", row.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));

            // The next match: live with no board, no banner and no beat of its own yet.
            if (_matches.Count >= 2 && _current != null && match != null && match.PresentationMatchId == _current.id
                && match.MatchInProgress && round != null && round.RoundActive && round.TimeLeft < SceneFlow.SelectedRoundSeconds - 3f)
            {
                _current.liveRoundSeen = true;
                var previous = _matches[_matches.Count - 2];
                previous.leftoverInNext |= MatchResult.BoardShowing || Hud.MatchOverShowing || MatchEndBeat.Holding;
                if (!Has("-tp-matchend-stay")) Finish(true);
            }
        }

        private void Finish(bool completed)
        {
            if (_finished) return;
            _finished = true;
            var json = new StringBuilder();
            json.Append("{\"role\":\"").Append(_role).Append("\",\"completed\":").Append(completed ? "true" : "false");
            json.Append(",\"case\":\"").Append(_case).Append("\",\"rejoined\":").Append(_rejoined ? "true" : "false");
            json.Append(",\"build\":\"").Append(Escape(BuildIdentity.OneLine())).Append("\"");
            json.Append(",\"notes\":[").Append(string.Join(",", _notes.Select(n => "\"" + Escape(n) + "\""))).Append("]");
            json.Append(",\"matches\":[");
            for (int i = 0; i < _matches.Count; i++)
            {
                var m = _matches[i];
                if (i > 0) json.Append(',');
                json.Append('{');
                json.AppendFormat(CultureInfo.InvariantCulture,
                    "\"id\":{0},\"map\":\"{1}\",\"mode\":\"{2}\",\"totalRounds\":{3},\"scoreTarget\":{4},\"ended\":{5},\"endPath\":\"{6}\",\"finalRound\":{7}," +
                    "\"whistleWall\":{8},\"firstBannerWall\":{9},\"revealWall\":{10},\"boardWall\":{11},\"reveals\":{12},\"bannerSamples\":{13}," +
                    "\"boardSamplesDuringBeat\":{14},\"hostTimed\":{15},\"revealAge\":{16},\"firstSeenAge\":{17},\"corrections\":{18},\"winner\":{19}," +
                    "\"rematchOffered\":{20},\"rematchHidden\":{21},\"timePausedAfterReveal\":{22},\"leftoverInNext\":{23},\"liveRoundSeen\":{24}," +
                    "\"clips\":{25},\"clipsShown\":{26}",
                    m.id, m.map, m.mode, m.totalRounds, m.scoreTarget, B(m.ended), m.endPath, m.finalRound,
                    m.whistleWall, m.firstBannerWall, m.revealWall, m.boardWall, m.reveals, m.bannerSamples,
                    m.boardSamplesDuringBeat, B(m.hostTimed), m.revealAge, m.firstSeenAge, m.corrections, m.winner,
                    B(m.rematchOffered), B(m.rematchHidden), B(m.timePausedAfterReveal), B(m.leftoverInNext), B(m.liveRoundSeen),
                    m.clips, m.clipsShown);
                json.Append(",\"scores\":[").Append(m.scores == null ? "" : string.Join(",", m.scores)).Append(']');
                json.Append(",\"restAtReveal\":[").Append(string.Join(",", m.restAtReveal.Select(s => "\"" + s + "\""))).Append(']');
                json.Append(",\"restLate\":[").Append(string.Join(",", m.restLate.Select(s => "\"" + s + "\""))).Append(']');
                json.Append('}');
            }
            json.Append("]}");
            File.WriteAllText(Path.Combine(_dir, _role + ".result.json"), json.ToString());
            _csv?.Flush();
            Debug.Log("[MatchEndProbe] finished completed=" + completed);
            Application.Quit(completed ? 0 : 1);
        }

        private static string B(bool value) => value ? "true" : "false";
        private static string Escape(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

        private void OnDestroy()
        {
            MatchEndBeat.Revealed -= OnRevealed;
            if (_hooked != null) _hooked.MatchEnded -= OnMatchEnded;
            _csv?.Dispose();
        }
    }
}
