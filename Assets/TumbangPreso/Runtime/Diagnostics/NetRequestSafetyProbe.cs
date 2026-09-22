using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Diagnostics
{
    /// <summary>
    /// Opt-in evidence for `docs/TODO.md` § 149.4 (C4 in `docs/CLAUDE_REQUEST_SAFETY_LANE.md`):
    /// what a HOST does with duplicated, stale-seat and wrong-role gameplay requests arriving
    /// over a real transport from a separate player process.
    ///
    /// ⚠️⚠️ IT SENDS THROUGH THE SAME PUBLIC REQUEST METHODS THE GAME DOES AND BYPASSES NO HOST
    /// GUARD. Repeated calls carry the same action values; current skill calls receive fresh
    /// receipt IDs. Exact-ID replay is separately exercised by NetPredictionReceiptProbe.
    /// Every admission check (`SenderOwnsClaimedSeat`, `PlausibleIntentPose`, the
    /// `HostResolve*` predicates, `CanThrow`, `CanBeGrabbedBy`, the kit's own readiness) runs
    /// unchanged on the host. What the fixture DOES stage is the world: where bodies and the
    /// loose tsinelas stand, with bots and local input switched off, the same staging
    /// `NetDanteProbe` does.
    ///
    /// ⚠️ THE REAL PRODUCER IS EXERCISED TOO. Round 2 presses the punch, the lunge and the
    /// stomp through `InputIntent`, so the accepted legitimate path is measured beside the
    /// adversarial one rather than assumed.
    ///
    /// Inactive unless `-tp-requestsafety &lt;csv&gt;` is on the command line. Seat 1 must be the
    /// joining client; the host writes the authoritative trace and the client writes its own
    /// view plus one marker row per send. `tools/net_request_safety.py` runs and evaluates it.
    /// </summary>
    [DefaultExecutionOrder(-250)]
    public sealed class NetRequestSafetyProbe : MonoBehaviour
    {
        public const string TraceSwitch = "-tp-requestsafety";

        // ⚠️ THE HANDOVER ARM. The client leaves mid-match at this round-1 elapsed time, the
        // host hands seat 1 to a bot, and a relaunch with the same named profile reclaims it
        // through the ordinary reconnect path; the rejoined process then runs only round 2.
        public const string LeaveSwitch = "-tp-requestsafety-leave";
        public const string RejoinSwitch = "-tp-requestsafety-rejoin";

        // With this the leaving client does NOT quit: it marks the moment and goes silent, and
        // the runner kills the process, so the host still holds a live connection for that
        // token when the relaunch arrives. That is `NetSession`'s "Replaced by reconnect" path.
        public const string KillSwitch = "-tp-requestsafety-kill";

        // ⚠️ THE MATCH-BOUNDARY ARM. With `-tp-autorematch` the real result board's REMATCH is
        // pressed and the whole script runs again in the same sessions, so the second match
        // shows whether anything a request touched leaked across the boundary.
        public const string MatchesSwitch = "-tp-requestsafety-matches";
        private const int Caster = 1;

        private static bool _active;
        private float _leaveAt = -1;
        private bool _rejoined;
        private bool _awaitKill;
        private bool _silent;
        private float _nextState;
        private int _matchesWanted = 1;
        private int _match;
        private bool _wasInProgress;
        private StreamWriter _trace;
        private StreamWriter _markers;
        private readonly HashSet<string> _done = new HashSet<string>();
        private int _stagedRound;
        private int _lastRound;
        private float _pressUntil = -1;
        private Verb _pressVerb;
        private int _lastKitIdentity;
        private bool _rehost, _rehosting, _rehosted;
        private long _oldEpoch;

        private static readonly FieldInfo DenialsSent =
            typeof(MatchRpc).GetField("_denialsSent", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo DenialsTaken =
            typeof(MatchRpc).GetField("_denialsTaken", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo StatsLine =
            typeof(MatchStatsCollector).GetMethod("LineFor", BindingFlags.Instance | BindingFlags.NonPublic);

        private static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, key);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            _active = Argument(TraceSwitch) != null &&
                      !Environment.GetCommandLineArgs().Contains("-tp-tournament");
            if (!_active) return;

            // ⚠️ SHORT ROUNDS SO ROUND 2 (SEAT 1 AS THE TAYA) ARRIVES INSIDE ONE RUN. Rules are
            // pinned on every process, which is how the other fixtures agree on a match shape.
            var rules = CustomGameRules.Defaults(GameMode.HeroStrike);
            rules.Rounds = 2;
            rules.RoundSeconds = 30;
            UI.SceneFlow.PinSelectedRules(rules);
            Settings.SettingsStore.Current.CharacterPick = Roster.GetPeople(GameMode.HeroStrike)
                .Select((person, index) => (person, index)).First(pair => pair.person.Id == "dante").index;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!_active) return;
            var root = new GameObject("~NetRequestSafetyProbe");
            DontDestroyOnLoad(root);
            var probe = root.AddComponent<NetRequestSafetyProbe>();
            if (float.TryParse(Argument(LeaveSwitch), NumberStyles.Float, CultureInfo.InvariantCulture, out float leave)) probe._leaveAt = leave;
            probe._rejoined = Environment.GetCommandLineArgs().Contains(RejoinSwitch);
            probe._awaitKill = Environment.GetCommandLineArgs().Contains(KillSwitch);
            if (int.TryParse(Argument(MatchesSwitch), out int matches) && matches > 1) probe._matchesWanted = matches;
            probe._rehost = Argument("-tp-requestsafety-rehost") != null;
            if (int.TryParse(Argument("-tp-rehost-phase"), out int phase)) probe._match = phase - 1;
            long.TryParse(Argument("-tp-rehost-old-epoch"), out probe._oldEpoch);
            string path = Path.GetFullPath(Argument(TraceSwitch));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            probe._trace = new StreamWriter(path) { AutoFlush = true };
            probe._trace.WriteLine("real,host,local,match,round,elapsed,defender,punchCd,lungeCd,shoveCd,slideCd,stamina," +
                                   "holding,shoeState,shoeHolder,throws,retrievals,shoveAttempts,lungeAttempts,tags," +
                                   "denPunch,denLunge,denShove,denSlide,stompCharges,stompWindup,carapaceCd,carapaceActive," +
                                   "seat0PunchCd,seat2Holding,seat2Stun,x,z,seat1Bot,heroIndex,characterPick,lobbyPick,characterMode,kitIdentity,epoch");
            probe._markers = new StreamWriter(Path.ChangeExtension(path, ".markers.csv")) { AutoFlush = true };
            probe._markers.WriteLine("real,local,match,round,elapsed,name");
        }

        private void Update()
        {
            if (_rehosting) return;
            var round = GameServices.Round;
            var match = GameServices.Match;
            // Once a second, whatever the gates below decide, so a peer that stops sampling says
            // which gate stopped it.
            if (Time.realtimeSinceStartup >= _nextState)
            {
                _nextState = Time.realtimeSinceStartup + 1;
                var seat = round != null ? round.PlayerAt(Caster) : null;
                Debug.Log($"[RequestSafetyProbe] state local={NetAuthority.LocalSlot} networked={NetAuthority.IsNetworked} " +
                          $"rpc={MatchRpc.Instance != null} round={match?.RoundNumber} active={round?.RoundActive} " +
                          $"inProgress={match?.MatchInProgress} warmup={match?.IsWarmupBuffer} timeLeft={round?.TimeLeft:F2} " +
                          $"seat1={(seat != null)} kit={(seat != null && seat.AbilitySystem != null)} canAct={seat?.CanAct()}");
            }
            if (!NetAuthority.IsNetworked || MatchRpc.Instance == null || round == null || match == null) return;

            // A match begins on the rising edge of `MatchInProgress`, which a rematch raises again.
            if (match.MatchInProgress && !_wasInProgress) { _match++; _lastRound = 0; _stagedRound = 0; }
            _wasInProgress = match.MatchInProgress;
            // ⚠️ `IsWarmupBuffer` IS ONLY TRUSTED ON THE HOST. A client's `RoundDirector` raises the
            // intermission from its own clock and only a client `SliceRunner` that saw the match
            // start ever advances it again, so a reclaimed seat reads true for the whole next
            // live round (measured in the handover arm, see the C4 report).
            if (!round.RoundActive || match.RoundNumber < 1 || (NetAuthority.IsHost && match.IsWarmupBuffer)) return;

            var caster = round.PlayerAt(Caster);
            if (caster == null || round.PlayerAt(0) == null || round.PlayerAt(2) == null || round.PlayerAt(3) == null) return;
            if (caster.AbilitySystem == null) return;

            foreach (var brain in FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var reader in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) reader.enabled = false;
            foreach (var player in round.Players)
            {
                if (player == null) continue;
                if (player != caster || NetAuthority.LocalSlot != Caster) { player.Intent.Clear(); player.Intent.Parked = true; }
            }

            int number = match.RoundNumber;
            if (number != _lastRound) { _lastRound = number; _done.Clear(); }
            float elapsed = UI.SceneFlow.SelectedRoundSeconds - round.TimeLeft;

            // A rejoined client that lands inside round 1 leaves that round's world alone.
            if (_rejoined && number == 1) { Sample(round, caster, number, elapsed); return; }
            if (_stagedRound != number && elapsed >= 4.5f) Stage(round, caster, number);
            if (_stagedRound == number)
            {
                int dante = Roster.IndexIn(Roster.HeroPeople, "dante");
                if (caster.CharacterIndex != dante || caster.AbilitySystem.HeroId != "dante")
                {
                    // Allow the host's staged pick announcement to reach the client,
                    // but never send requests against a different body/kit silently.
                    if (elapsed >= 6.5f)
                    {
                        Mark(number, elapsed, "fixture caster identity did not settle");
                        _trace.Flush(); _markers.Flush(); Application.Quit(2);
                    }
                    return;
                }
                if (NetAuthority.IsHost) HostStaging(round, caster, number, elapsed);
                if (NetAuthority.LocalSlot == Caster) ClientSends(caster, number, elapsed);
            }

            Sample(round, caster, number, elapsed);
            if (_rehost && !_rehosted && NetAuthority.IsHost && _match == 1 && number >= 2 && elapsed > 24)
            { BeginRehost(); return; }
            // ⚠️ THE HOST LEAVES FIRST. When the client left first the host's AI takeover of seat 1
            // changed that seat's kit on the way out, which is a seat handover and not one of the
            // requests under test, and it landed inside the last sampled second.
            if (_match >= _matchesWanted && number >= 2 && elapsed > (NetAuthority.IsHost ? 24.0f : 26.0f)) { _trace.Flush(); _markers.Flush(); Application.Quit(); }
        }

        private async void BeginRehost()
        {
            _rehosting = _rehosted = true;
            _oldEpoch = GameServices.Match.PresentationMatchId;
            Scene previousScene = SceneManager.GetActiveScene();
            string map = UI.SceneFlow.SelectedMap;
            try
            {
                Mark(GameServices.Match.RoundNumber, UI.SceneFlow.SelectedRoundSeconds - GameServices.Round.TimeLeft, "host begins fresh session in same process");
                int port = int.Parse(Argument("-tp-requestsafety-rehost"), CultureInfo.InvariantCulture);
                if (!await NetSession.Instance.StartHostAsync(port)) throw new InvalidOperationException("Same-process rehost refused.");
                UI.SceneFlow.Go(map);
                StartCoroutine(AfterRehostScene(previousScene, map));
            }
            catch (Exception error) { Debug.LogException(error); Application.Quit(2); }
        }

        private IEnumerator AfterRehostScene(Scene previousScene, string map)
        {
            float until = Time.realtimeSinceStartup + 25;
            bool Loaded() => SceneManager.GetActiveScene() != previousScene
                && SceneManager.GetActiveScene().name == map && SceneManager.GetActiveScene().isLoaded;
            while (!Loaded() && Time.realtimeSinceStartup < until) yield return null;
            if (!Loaded())
            { Debug.LogError("[RequestSafetyProbe] Rehost scene did not reload."); Application.Quit(2); yield break; }
            _wasInProgress = false; _lastRound = _stagedRound = 0; _done.Clear();
            _rehosting = false;
            string signal = Path.ChangeExtension(Argument(TraceSwitch), ".rehost");
            File.WriteAllText(signal + ".tmp", _oldEpoch.ToString(CultureInfo.InvariantCulture));
            File.Move(signal + ".tmp", signal);
            Debug.Log("[RequestSafetyProbe] same host process listening in a fresh session; previous epoch=" + _oldEpoch);
        }

        private void Stage(RoundDirector round, CharacterMotor caster, int number)
        {
            _stagedRound = number;
            if (NetAuthority.IsHost)
            {
                // Direct-arena automation may begin with a bot's existing body.
                // Binding only its kit makes the next legitimate appearance refresh
                // restore that body's original hero. Stage the actual caster pick
                // through the same snapshot path used by the other network probes.
                int dante = Roster.IndexIn(Roster.HeroPeople, "dante");
                MatchRpc.Instance.SyncPicksClientRpc(new[] { Caster, dante, -1, -1 });
                MatchRpc.Instance.BroadcastPicks();
            }
            caster.AbilitySystem.BindHero("dante", new HeroBuild { HeroId = "dante" });

            // Round 1 seat 1 attacks from outside the box; round 2 it is the taya inside it.
            Vector3 casterAt = number == 1 ? new Vector3(0, .12f, -8) : new Vector3(-3, .12f, -3);
            Vector3 victimAt = number == 1 ? new Vector3(0, .12f, -6.9f) : new Vector3(-3, .12f, -2.1f);
            if (NetAuthority.IsHost || NetAuthority.LocalSlot == Caster)
            {
                caster.Teleport(casterAt);
                caster.transform.rotation = Quaternion.identity;
                caster.ClearStun();
                caster.ClearTrip();
            }
            if (NetAuthority.IsHost)
            {
                round.PlayerAt(2).Teleport(victimAt);
                round.PlayerAt(3).Teleport(new Vector3(-8, .12f, -12));
                round.PlayerAt(0).Teleport(number == 1 ? new Vector3(6, .12f, 2) : new Vector3(8, .12f, -12));
                var shoe = CasterShoe();
                if (shoe != null) { shoe.HostDisarm(); Place(shoe, casterAt + new Vector3(.5f, 0, 0)); }
            }
            Mark(number, 0, "staged");
        }

        /// <summary>Host-only world staging between the client's sends. It moves objects, never
        /// grants a state a request is being asked to earn.</summary>
        private void HostStaging(RoundDirector round, CharacterMotor caster, int number, float elapsed)
        {
            if (number != 1) return;
            if (Once("stage-slide", elapsed >= 15.0f))
            {
                var shoe = CasterShoe();
                if (shoe != null && shoe.State == SlipperState.Loose)
                {
                    round.PlayerAt(2).Teleport(new Vector3(3, .12f, -8));
                    Place(shoe, caster.transform.position + Vector3.forward * 1.4f);
                    Mark(number, elapsed, "host staged slide shoe");
                }
                else
                {
                    _done.Remove("stage-slide");
                }
            }
        }

        private void ClientSends(CharacterMotor caster, int number, float elapsed)
        {
            var rpc = MatchRpc.Instance;
            Vector3 at = caster.transform.position;
            Vector3 facing = caster.transform.forward;
            Vector3 aim = at + facing * 3;
            if (_oldEpoch != 0 && _match == 2 && number == 1 && Once("old-session-skill", elapsed >= 6.7f))
            {
                // Current valid pose/role/round, but the prior session's match stamp.
                // A high sequence also checks that rejection cannot poison fresh IDs.
                // Keep this opt-in packet in step with MatchRpc's current ReqAbility schema.
                using var writer = new FastBufferWriter(128, Allocator.Temp);
                writer.WriteValueSafe(Caster); writer.WriteValueSafe(0); writer.WriteValueSafe(at);
                writer.WriteValueSafe(facing); writer.WriteValueSafe(aim); writer.WriteValueSafe(0f);
                writer.WriteValueSafe(false); writer.WriteValueSafe(Vector3.zero);
                writer.WriteValueSafe(_oldEpoch); writer.WriteValueSafe(number); writer.WriteValueSafe(1000000L);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("ReqAbility", NetworkManager.ServerClientId, writer);
                Mark(number, elapsed, "stale previous-session skill request");
            }

            if (number == 1)
            {
                if (_rejoined) return;
                if (_silent) return;
                if (_leaveAt > 0 && Once("r1-leave", elapsed >= _leaveAt))
                {
                    if (_awaitKill)
                    {
                        Mark(number, elapsed, "client waits to be killed for the handover arm");
                        _markers.Flush();
                        _silent = true;
                        return;
                    }
                    Mark(number, elapsed, "client leaves for the handover arm");
                    _trace.Flush(); _markers.Flush();
                    Application.Quit();
                    return;
                }
                if (Once("r1-stale-grab", elapsed >= 7.0f))
                { rpc.RequestGrabServerRpc(2, Caster); Mark(number, elapsed, "stale-seat grab claiming seat 2"); }
                if (Once("r1-dup-grab", elapsed >= 8.0f))
                { rpc.RequestGrabServerRpc(Caster, Caster); rpc.RequestGrabServerRpc(Caster, Caster); Mark(number, elapsed, "duplicate grab x2"); }
                if (Once("r1-dup-throw", elapsed >= 10.0f))
                {
                    Vector3 origin = at + Vector3.up * 1.3f;
                    Vector3 target = new Vector3(4, .1f, -4);
                    rpc.RequestThrowServerRpc(Caster, origin, target, .6f);
                    rpc.RequestThrowServerRpc(Caster, origin, target, .6f);
                    Mark(number, elapsed, "duplicate throw x2");
                }
                if (Once("r1-stale-shove", elapsed >= 11.5f))
                { rpc.RequestShoveServerRpc(2, GameServices.Round.PlayerAt(2).transform.position, facing); Mark(number, elapsed, "stale-seat shove claiming seat 2"); }
                if (Once("r1-role-punch", elapsed >= 12.0f))
                { rpc.RequestPunchServerRpc(Caster, at, facing); Mark(number, elapsed, "wrong-role punch as attacker"); }
                if (Once("r1-dup-shove", elapsed >= 13.0f))
                { rpc.RequestShoveServerRpc(Caster, at, facing); rpc.RequestShoveServerRpc(Caster, at, facing); Mark(number, elapsed, "duplicate shove x2"); }
                if (Once("r1-dup-slide", elapsed >= 17.0f))
                { rpc.RequestSlideServerRpc(Caster, at, facing); rpc.RequestSlideServerRpc(Caster, at, facing); Mark(number, elapsed, "duplicate slide x2"); }
                if (Once("r1-dup-carapace", elapsed >= 19.5f))
                {
                    rpc.RequestAbilityCastServerRpc(Caster, 1, at, facing, aim, 0);
                    rpc.RequestAbilityCastServerRpc(Caster, 1, at, facing, aim, 0);
                    Mark(number, elapsed, "duplicate carapace cast x2");
                }
                if (Once("r1-triple-stomp", elapsed >= 21.0f))
                {
                    for (int i = 0; i < 3; i++) rpc.RequestAbilityCastServerRpc(Caster, 0, at, facing, aim, 0);
                    Mark(number, elapsed, "stomp cast x3 in one frame");
                }
                if (Once("r1-stomp-4", elapsed >= 22.5f))
                { rpc.RequestAbilityCastServerRpc(Caster, 0, at, facing, aim, 0); Mark(number, elapsed, "stomp cast 4 after windup"); }
                if (Once("r1-stomp-5", elapsed >= 24.0f))
                { rpc.RequestAbilityCastServerRpc(Caster, 0, at, facing, aim, 0); Mark(number, elapsed, "stomp cast 5 with no charge"); }
                // The last active frames of the round: a throw that reaches the host after the
                // whistle must not land in the intermission or carry into round 2.
                if (Once("r1-boundary-throw", elapsed >= 29.9f))
                {
                    Vector3 origin = at + Vector3.up * 1.3f;
                    rpc.RequestThrowServerRpc(Caster, origin, new Vector3(4, .1f, -4), .6f);
                    rpc.RequestThrowServerRpc(Caster, origin, new Vector3(4, .1f, -4), .6f);
                    Mark(number, elapsed, "duplicate throw at the round boundary");
                }
                return;
            }

            if (Once("r2-dup-punch", elapsed >= 7.0f))
            { rpc.RequestPunchServerRpc(Caster, at, facing); rpc.RequestPunchServerRpc(Caster, at, facing); Mark(number, elapsed, "duplicate punch x2"); }
            if (Once("r2-dup-lunge", elapsed >= 9.0f))
            { rpc.RequestLungeServerRpc(Caster, at, facing, 1); rpc.RequestLungeServerRpc(Caster, at, facing, 1); Mark(number, elapsed, "duplicate lunge x2"); }
            if (Once("r2-role-shove", elapsed >= 11.0f))
            { rpc.RequestShoveServerRpc(Caster, at, facing); Mark(number, elapsed, "wrong-role shove as taya"); }
            if (Once("r2-stale-punch", elapsed >= 12.0f))
            { rpc.RequestPunchServerRpc(0, GameServices.Round.PlayerAt(0).transform.position, facing); Mark(number, elapsed, "stale-seat punch claiming seat 0"); }
            if (Once("r2-legit-punch", elapsed >= 14.0f)) { Press(Verb.SpecialAbility, .1f); Mark(number, elapsed, "legitimate punch input"); }
            if (Once("r2-legit-lunge", elapsed >= 16.0f)) { Press(Verb.Lunge, .3f); Mark(number, elapsed, "legitimate lunge input"); }
            if (Once("r2-legit-stomp", elapsed >= 19.0f)) { Press(Verb.Skill1, .1f); Mark(number, elapsed, "legitimate stomp input"); }

            caster.Intent.Parked = false;
            caster.Intent.Move = Vector2.zero;
            if (_pressUntil > 0) caster.Intent.Set(_pressVerb, Time.realtimeSinceStartup < _pressUntil);
        }

        private void Press(Verb verb, float seconds)
        {
            _pressVerb = verb;
            _pressUntil = Time.realtimeSinceStartup + seconds;
        }

        private bool Once(string key, bool due)
        {
            if (!due || _done.Contains(key)) return false;
            _done.Add(key);
            return true;
        }

        private static Slipper CasterShoe()
            => FindObjectsByType<Slipper>(FindObjectsInactive.Include)
                .FirstOrDefault(s => s.SeatOfOrigin == Caster);

        private static void Place(Slipper shoe, Vector3 at)
        {
            at.y = Slipper.GroundY(at) + shoe.RestHeight;
            shoe.transform.position = at;
        }

        private void Mark(int number, float elapsed, string name)
        {
            _markers.WriteLine(string.Join(",", Time.realtimeSinceStartupAsDouble.ToString("F4", CultureInfo.InvariantCulture),
                NetAuthority.LocalSlot, _match, number, elapsed.ToString("F3", CultureInfo.InvariantCulture), name));
            Debug.Log($"[RequestSafetyProbe] local={NetAuthority.LocalSlot} round={number} elapsed={elapsed:F3} {name}");
        }

        private void Sample(RoundDirector round, CharacterMotor caster, int number, float elapsed)
        {
            var verbs = caster.GetComponent<CombatVerbs>();
            var shoe = CasterShoe();
            object line = GameServices.Stats != null && StatsLine != null
                ? StatsLine.Invoke(GameServices.Stats, new object[] { Caster }) : null;
            var stats = line as PlayerMatchStats;
            var denials = (NetAuthority.IsHost ? DenialsSent : DenialsTaken)?.GetValue(MatchRpc.Instance) as int[];
            var kit = caster.AbilitySystem.Kit;
            int kitIdentity = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(kit);
            int heroIndex = Roster.IndexIn(Roster.HeroPeople, caster.AbilitySystem.HeroId);
            int lobbyPick = MatchRpc.Instance.GetSeatInfo(Caster)?.CharacterPick ?? -1;
            if (_lastKitIdentity != kitIdentity)
            {
                Debug.Log($"[RequestSafetyIdentity] match={_match} round={number} elapsed={elapsed:F3} " +
                    $"kit={caster.AbilitySystem.HeroId} characterPick={caster.CharacterIndex} lobbyPick={lobbyPick} " +
                    $"mode={caster.Mode} identity={kitIdentity} previous={_lastKitIdentity}");
                _lastKitIdentity = kitIdentity;
            }
            var seat0 = round.PlayerAt(0).GetComponent<CombatVerbs>();
            var seat2 = round.PlayerAt(2);

            object[] row =
            {
                Time.realtimeSinceStartupAsDouble, NetAuthority.IsHost ? 1 : 0, NetAuthority.LocalSlot, _match, number, elapsed,
                caster.IsDefender ? 1 : 0, verbs.PunchCooldownLeft, verbs.LungeCooldownLeft, verbs.ShoveCooldownLeft,
                verbs.SlideCooldownLeft, caster.Stamina.Current, caster.HoldingSlipper ? 1 : 0,
                shoe != null ? (int)shoe.State : -1, shoe != null && shoe.Holder != null ? shoe.Holder.PlayerSlot : -1,
                stats?.Throws ?? -1, stats?.Retrievals ?? -1, stats?.ShoveAttempts ?? -1, stats?.LungeAttempts ?? -1, stats?.Tags ?? -1,
                denials != null ? denials[0] : -1, denials != null ? denials[1] : -1,
                denials != null ? denials[2] : -1, denials != null ? denials[3] : -1,
                kit.Skill1.ChargesRemaining, kit.Skill1.WindupRemaining, kit.Skill2.CooldownRemaining, kit.Skill2.IsActive ? 1 : 0,
                seat0 != null ? seat0.PunchCooldownLeft : -1, seat2.HoldingSlipper ? 1 : 0, seat2.StunLeft,
                caster.transform.position.x, caster.transform.position.z, caster.IsBot ? 1 : 0,
                heroIndex, caster.CharacterIndex, lobbyPick, (int)caster.Mode, kitIdentity, GameServices.Match.PresentationMatchId
            };
            _trace.WriteLine(string.Join(",", row.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
        }

        private void OnDestroy()
        {
            _trace?.Dispose();
            _markers?.Dispose();
        }
    }
}
