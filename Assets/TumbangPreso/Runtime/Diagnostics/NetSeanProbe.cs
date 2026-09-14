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
    [DefaultExecutionOrder(-250)]
    public sealed class NetSeanProbe : MonoBehaviour
    {
        private static bool _enabled;
        private string _scenario;
        private StreamWriter _writer;
        private bool _pickSent, _seededArenaPick, _prepared, _armed;
        private double _next;
        private Slipper _shoe;
        private static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, key);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            _enabled = Argument("-tp-seantrace") != null && !Environment.GetCommandLineArgs().Contains("-tp-tournament");
            if (!_enabled) return;
            UI.SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            Settings.SettingsStore.Current.CharacterPick = Roster.IndexIn(Roster.HeroPeople, "sean");
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!_enabled) return;
            var root = new GameObject("~NetSeanProbe"); DontDestroyOnLoad(root);
            var probe = root.AddComponent<NetSeanProbe>(); probe._scenario = Argument("-tp-seancase") ?? "ignite";
            string path = Path.GetFullPath(Argument("-tp-seantrace")); Directory.CreateDirectory(Path.GetDirectoryName(path));
            probe._writer = new StreamWriter(path) { AutoFlush = true };
            probe._writer.WriteLine("time,elapsed,local,host,sean,pick,charged,held,s2charges,ultcharge,grounded,casterY,pose,craters,craterX,craterZ,embers,fireFlight,shoeState,shoeX,shoeY,shoeZ,frontStun");
        }
        private void Update()
        {
            int pick = Roster.IndexIn(Roster.HeroPeople, "sean");
            if (!_pickSent && NetAuthority.IsNetworked && NetAuthority.LocalSlot == 1 && MatchRpc.Instance != null)
            {
                var settings = Settings.SettingsStore.Current; settings.CharacterPick = pick;
                MatchRpc.Instance.SelectLobbyPickServerRpc(pick, settings.CanPick, settings.SlipperPick); _pickSent = true;
            }
            var round = GameServices.Round;
            if (!_seededArenaPick && NetAuthority.IsHost && round != null
                && MatchRpc.Instance?.GetSeatInfo(1)?.CharacterPick == pick && round.PlayerAt(1) != null)
            {
                _seededArenaPick = true;
                MatchRpc.Instance.SyncPicksClientRpc(new[] { 1, pick, -1, -1 }); MatchRpc.Instance.BroadcastPicks();
            }
            if (!NetAuthority.IsNetworked || round == null || !round.RoundActive || GameServices.Match == null
                || GameServices.Match.RoundNumber < 1) return;
            var ready = FindFirstObjectByType<ReadyGate>(); if (ready != null && ready.CountingDown) return;
            var caster = round.PlayerAt(1); var front = round.PlayerAt(2);
            if (caster == null || front == null || !(caster.AbilitySystem?.Kit is SeanHeroKit kit)) return;
            foreach (var brain in FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var player in round.Players)
                if (player != null) { player.Intent.Clear(); player.Intent.Parked = player != caster; }
            float elapsed = UI.SceneFlow.SelectedRoundSeconds - round.TimeLeft;
            if (elapsed > 24) { _writer.Flush(); Application.Quit(); return; }
            if (!_prepared)
            {
                if (caster.CharacterIndex != pick) return;
                _prepared = true; kit.AddUltimateCharge(100);
                if (NetAuthority.IsHost || NetAuthority.LocalSlot == 1)
                {
                    caster.Teleport(new Vector3(0, .12f, -8)); caster.transform.rotation = Quaternion.identity;
                    caster.ClearStun(); caster.ClearTrip();
                }
                if (NetAuthority.IsHost || NetAuthority.LocalSlot == 2) front.Teleport(new Vector3(3, .12f, -5));
                if (NetAuthority.IsHost)
                {
                    round.PlayerAt(0).Teleport(new Vector3(-9, .12f, -12));
                    round.PlayerAt(3).Teleport(new Vector3(8, .12f, 7));
                }
                Debug.Log("[SeanProbe] prepared " + _scenario + " local=" + NetAuthority.LocalSlot);
            }
            if (NetAuthority.LocalSlot == 1)
            {
                if (!_armed && elapsed >= 12) { kit.AddUltimateCharge(100); _armed = true; }
                caster.Intent.Parked = false; caster.Intent.AimPoint = new Vector3(0, .15f, -2);
                caster.Intent.FaceAimPoint = true;
                caster.Intent.Set(_scenario == "ignite" ? Verb.Skill2 : Verb.Ultimate, elapsed >= 12 && elapsed < 12.3f);
                if (_scenario == "ignite") caster.Intent.Set(Verb.SpecialAbility, elapsed >= 13.4f && elapsed < 14);
            }
            var carrier = caster.GetComponent<Carrier>(); if (_shoe == null) _shoe = carrier.Held;
            double now = NetworkManager.Singleton.ServerTime.Time;
            if (now < _next) return; _next = now + .05;
            var craters = FindObjectsByType<HeroHazards.SupernovaCraterComponent>(FindObjectsSortMode.None);
            var crater = craters.Length > 0 ? craters[0].transform.position : Vector3.zero;
            var shoePosition = _shoe != null ? _shoe.transform.position : Vector3.zero;
            object[] row = { now, elapsed, NetAuthority.LocalSlot, NetAuthority.IsHost ? 1 : 0,
                kit.HeroId == "sean" ? 1 : 0, caster.CharacterIndex, kit.IsIgnitionCannonActive ? 1 : 0,
                carrier.Held != null ? 1 : 0, kit.Skill2.ChargesRemaining, kit.UltimateCharge,
                caster.IsGrounded ? 1 : 0, caster.transform.position.y, kit.SupernovaPoseTime,
                craters.Length, crater.x, crater.z, FindObjectsByType<SeanIgnitionVisual>(FindObjectsSortMode.None).Length,
                _shoe != null && _shoe.State == SlipperState.InFlight && _shoe.Affinity == SlipperAffinity.FireExplosive ? 1 : 0,
                _shoe != null ? (int)_shoe.State : -1, shoePosition.x, shoePosition.y, shoePosition.z, front.StunLeft };
            _writer.WriteLine(string.Join(",", row.Select(value => Convert.ToString(value, CultureInfo.InvariantCulture))));
        }
        private void OnDestroy() => _writer?.Dispose();
    }
}
