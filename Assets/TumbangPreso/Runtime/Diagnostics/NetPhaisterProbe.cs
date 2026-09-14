using System;
using System.Globalization;
using System.IO;
using System.Linq;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    // Opt-in local evidence. Normal players never install this fixture.
    [DefaultExecutionOrder(-250)]
    public sealed class NetPhaisterProbe : MonoBehaviour
    {
        private static bool _enabled;
        private string _scenario;
        private StreamWriter _writer;
        private bool _prepared, _armed, _observeExisting, _pickSent, _seededArenaPick;
        private double _next;
        private float _castStartedAt = -1;

        private void OnEnable() => HeroAbilitySystem.UltimateStarted += OnUltimateStarted;
        private void OnDisable() => HeroAbilitySystem.UltimateStarted -= OnUltimateStarted;
        private void OnUltimateStarted(CharacterMotor caster, HeroKit kit, HeroAbility ability)
        {
            if (caster != null && caster.PlayerSlot == 1 && kit.HeroId == "phaister")
                _castStartedAt = Time.time;
        }

        private static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, key);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            _enabled = Argument("-tp-phaistertrace") != null &&
                !Environment.GetCommandLineArgs().Contains("-tp-tournament");
            if (!_enabled) return;
            UI.SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            Settings.SettingsStore.Current.CharacterPick = Roster.IndexIn(Roster.HeroPeople, "phaister");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!_enabled) return;
            var root = new GameObject("~NetPhaisterProbe");
            DontDestroyOnLoad(root);
            var probe = root.AddComponent<NetPhaisterProbe>();
            probe._scenario = Argument("-tp-phaistercase") ?? "coven";
            probe._observeExisting = Environment.GetCommandLineArgs().Contains("-tp-phaister-observe-existing");
            string path = Path.GetFullPath(Argument("-tp-phaistertrace"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            probe._writer = new StreamWriter(path) { AutoFlush = true };
            probe._writer.WriteLine("time,elapsed,local,host,charge,windup,active,remaining,circles,sky,frontStun,rearStun,casterStun,frontElement,centreX,centreZ,casterX,casterZ,gameTime,realTime,frameDelta,frontX,frontZ,rearX,rearZ,phaister,charIndex,heroMode,roomPick,castAge");
        }

        private void Update()
        {
            int phaisterPick = Roster.IndexIn(Roster.HeroPeople, "phaister");
            if (!_observeExisting && !_pickSent && NetAuthority.IsNetworked &&
                NetAuthority.LocalSlot == 1 && MatchRpc.Instance != null)
            {
                // Select through the real room path. Binding a kit alone leaves
                // the authoritative pick unchanged, so a later seating snapshot
                // correctly replaces that mismatched fixture with the old hero.
                var settings = Settings.SettingsStore.Current;
                settings.CharacterPick = phaisterPick;
                MatchRpc.Instance.SelectLobbyPickServerRpc(phaisterPick, settings.CanPick, settings.SlipperPick);
                _pickSent = true;
            }
            var round = GameServices.Round;
            if (!_observeExisting && !_seededArenaPick && NetAuthority.IsHost && round != null &&
                MatchRpc.Instance?.GetSeatInfo(1)?.CharacterPick == phaisterPick && round.PlayerAt(1) != null)
            {
                // CLI verification preloads the arena before the room fills. Apply
                // its accepted pick once through normal model/kit reconciliation,
                // as the established familiar fixture does, before timed actions.
                _seededArenaPick = true;
                MatchRpc.Instance.SyncPicksClientRpc(new[] { 1, phaisterPick, -1, -1 });
                MatchRpc.Instance.BroadcastPicks();
                Debug.Log("[PhaisterProbe] initialized accepted room pick in the preloaded arena");
            }
            if (!_enabled || !NetAuthority.IsNetworked || round == null || !round.RoundActive ||
                GameServices.Match == null || GameServices.Match.RoundNumber < 1) return;
            var ready = FindFirstObjectByType<ReadyGate>();
            if (ready != null && ready.CountingDown) return;
            var caster = round.PlayerAt(1);
            var front = round.PlayerAt(2);
            var rear = round.PlayerAt(3);
            if (caster?.AbilitySystem?.Kit == null || front == null || rear == null) return;
            foreach (var brain in FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var player in round.Players)
            {
                if (player == null) continue;
                player.Intent.Clear(); player.Intent.Parked = player != caster;
            }
            float elapsed = UI.SceneFlow.SelectedRoundSeconds - round.TimeLeft;
            if (elapsed > 27) { _writer.Flush(); Application.Quit(); return; }
            if (!_prepared && _observeExisting)
            {
                // A returning peer observes the real world snapshot. Rebinding a
                // kit or relocating a target here would invalidate this evidence.
                _prepared = true;
                Debug.Log("[PhaisterProbe] observing existing match local=" + NetAuthority.LocalSlot);
            }
            if (!_prepared)
            {
                if (caster.CharacterIndex != phaisterPick || caster.AbilitySystem.Kit.HeroId != "phaister") return;
                _prepared = true;
                if (_scenario != "rejected") caster.AbilitySystem.Kit.AddUltimateCharge(100);
                if (NetAuthority.IsHost || NetAuthority.LocalSlot == 1)
                {
                    caster.Teleport(new Vector3(0, .12f, -8));
                    caster.transform.rotation = Quaternion.identity;
                    caster.ClearStun(); caster.ClearTrip();
                }
                if (NetAuthority.IsHost || NetAuthority.LocalSlot == 2)
                    front.Teleport(new Vector3(3.5f, .12f, -8));
                if (NetAuthority.IsHost)
                {
                    rear.Teleport(new Vector3(0, .12f, 7));
                    round.PlayerAt(0).Teleport(new Vector3(-12, .12f, -12));
                }
                Debug.Log("[PhaisterProbe] prepared " + _scenario + " local=" + NetAuthority.LocalSlot);
            }
            if (NetAuthority.LocalSlot == 1 && !_observeExisting)
            {
                if (!_armed && elapsed >= 12)
                {
                    // The rejected case deliberately gives only the predicting owner
                    // a meter. The real host must deny the request without spending.
                    // Set it on the press frame: an earlier authoritative meter
                    // snapshot can otherwise erase the fixture before it casts.
                    caster.AbilitySystem.Kit.AddUltimateCharge(100);
                    _armed = true;
                }
                caster.Intent.Parked = false;
                caster.Intent.Set(Verb.Ultimate, elapsed >= 12 && elapsed < 12.3f);
            }
            double now = NetworkManager.Singleton.ServerTime.Time;
            if (now >= _next)
            {
                _next = now + .05;
                var ultimate = caster.AbilitySystem.Kit.Ultimate;
                var circles = FindObjectsByType<HeroHazards.CovenCircleBuild>(FindObjectsSortMode.None);
                var centre = circles.Length > 0 ? circles[0].transform.position : Vector3.zero;
                var position = caster.transform.position;
                object[] row = { now, elapsed, NetAuthority.LocalSlot, NetAuthority.IsHost ? 1 : 0,
                    caster.AbilitySystem.Kit.UltimateCharge, ultimate.WindupRemaining,
                    ultimate.IsActive ? 1 : 0, ultimate.DurationRemaining, circles.Length,
                    FindObjectsByType<SkyEvent>(FindObjectsSortMode.None).Length,
                    front.StunLeft, rear.StunLeft, caster.StunLeft, (int)front.StunElement,
                    centre.x, centre.z, position.x, position.z,
                    Time.timeAsDouble, Time.realtimeSinceStartupAsDouble, Time.deltaTime,
                    front.transform.position.x, front.transform.position.z,
                    rear.transform.position.x, rear.transform.position.z,
                    caster.AbilitySystem.Kit.HeroId == "phaister" ? 1 : 0,
                    caster.CharacterIndex, UI.SceneFlow.SelectedMode == GameMode.HeroStrike ? 1 : 0,
                    MatchRpc.Instance?.GetSeatInfo(1)?.CharacterPick ?? -1,
                    _castStartedAt >= 0 ? Time.time - _castStartedAt : -1 };
                _writer.WriteLine(string.Join(",", row.Select(value => Convert.ToString(value, CultureInfo.InvariantCulture))));
            }
            if (elapsed > 27) { _writer.Flush(); Application.Quit(); }
        }

        private void OnDestroy() => _writer?.Dispose();
    }
}
