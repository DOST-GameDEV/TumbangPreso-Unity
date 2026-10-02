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
        private bool _holdCharge;
        private bool _observeExisting;
        private bool _advancedCinderRound;
        private float _steadyGrabAt=-1,_steadyGrantAt=-1,_steadyRegrabAt=-1,_steadyChargeAt=-1;
        private bool _steadyDropped,_steadyRegrabbed;
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
            probe._holdCharge = Environment.GetCommandLineArgs().Contains("-tp-holdcharge");
            probe._observeExisting = Environment.GetCommandLineArgs().Contains("-tp-sean-observe-existing");
            string path = Path.GetFullPath(Argument("-tp-seantrace")); Directory.CreateDirectory(Path.GetDirectoryName(path));
            probe._writer = new StreamWriter(path) { AutoFlush = true };
            probe._writer.WriteLine("time,elapsed,local,host,sean,pick,charged,held,s2charges,ultcharge,grounded,casterY,pose,craters,craterX,craterZ,embers,fireFlight,shoeState,shoeX,shoeY,shoeZ,frontStun,chargeRemaining,wallTime,cooldown,heldAffinity,frontX,frontY,frontZ,casterX,casterZ,s1Windup,s1Remaining,s1Cooldown,canAct,canMove,fireTrails,gates,gateId,gateSpent,gateRemaining,gateZ,gateDirection,frontVelocityZ,defending,round,passiveRemaining,chargeRate,chargePower,chargeActive,steadyRegrabbed,fullChargeTime");
        }
        private void Update()
        {
            int pick = Roster.IndexIn(Roster.HeroPeople, "sean");
            if (!_observeExisting && !_pickSent && NetAuthority.IsNetworked && NetAuthority.LocalSlot == 1 && MatchRpc.Instance != null)
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
            if (_scenario == "cinder" && GameServices.Match.RoundNumber < 2)
            {
                // Use the real round schedule so the client-owned Sean is the defender.
                if (NetAuthority.IsHost && !_advancedCinderRound
                    && UI.SceneFlow.SelectedRoundSeconds - round.TimeLeft > 4)
                { _advancedCinderRound = true; GameServices.Match.AdvanceRound(); }
                return;
            }
            var caster = round.PlayerAt(1); var front = round.PlayerAt(2);
            if (caster == null || front == null || !(caster.AbilitySystem?.Kit is SeanHeroKit kit)) return;
            foreach (var brain in FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var player in round.Players)
                if (player != null) { player.Intent.Clear(); player.Intent.Parked = player != caster; }
            float elapsed = UI.SceneFlow.SelectedRoundSeconds - round.TimeLeft;
            if (elapsed > 24) { _writer.Flush(); Application.Quit(); return; }
            if (!_prepared && _observeExisting)
            {
                _prepared = true;
                Debug.Log("[SeanProbe] observing the restored world local=" + NetAuthority.LocalSlot);
            }
            if (!_prepared)
            {
                if (caster.CharacterIndex != pick) return;
                _prepared = true; kit.AddUltimateCharge(100);
                if (NetAuthority.IsHost || NetAuthority.LocalSlot == 1)
                {
                    caster.Teleport(new Vector3(0, .12f, _scenario == "cinder" ? -5 : -8)); caster.transform.rotation = Quaternion.identity;
                    caster.ClearStun(); caster.ClearTrip();
                }
                if (NetAuthority.IsHost || NetAuthority.LocalSlot == 2) front.Teleport(_scenario == "cinder" ? new Vector3(0,.12f,-2.65f) : _scenario == "empowered" ? new Vector3(.8f, .12f, -2) : new Vector3(3, .12f, -5));
                if (NetAuthority.IsHost)
                {
                    round.PlayerAt(0).Teleport(new Vector3(-9, .12f, -12));
                    round.PlayerAt(3).Teleport(new Vector3(8, .12f, 7));
                }
                Debug.Log("[SeanProbe] prepared " + _scenario + " local=" + NetAuthority.LocalSlot);
            }
            if (_scenario == "empowered") front.Intent.Parked = false;
            if (_scenario == "cinder")
            {
                var gate = SeanCinderGate.Active.FirstOrDefault();
                front.Intent.Parked = false;
                FindFirstObjectByType<CameraSystem.CameraRig>()?.SetAimSource(CameraSystem.AimSource.Movement);
                front.Intent.Move = elapsed >= 13 && elapsed < 14 && gate != null && !gate.Spent ? Vector2.up : Vector2.zero;
            }
            if (NetAuthority.LocalSlot == 1 && !_observeExisting && _scenario != "steady")
            {
                if (!_armed && elapsed >= 12) { kit.AddUltimateCharge(100); _armed = true; }
                caster.Intent.Parked = false; caster.Intent.AimPoint = new Vector3(0, .15f, -2);
                caster.Intent.FaceAimPoint = true;
                if(_scenario=="stoke" && elapsed>=12.2f && elapsed<12.65f)caster.Intent.Move=Vector2.right;
                caster.Intent.Set(_scenario == "stoke" ? Verb.Skill1 : ((_scenario == "ignite" || _scenario == "empowered" || _scenario == "cinder") ? Verb.Skill2 : Verb.Ultimate), elapsed >= 12 && elapsed < 12.3f);
                if ((_scenario == "ignite" || _scenario == "empowered") && !_holdCharge) caster.Intent.Set(Verb.SpecialAbility, elapsed >= 13.4f && elapsed < 14);
            }
            var carrier = caster.GetComponent<Carrier>(); if (_shoe == null) _shoe = carrier.Held;
            if (_scenario == "steady" && _shoe != null) DriveSteady(caster,carrier,kit,elapsed);
            double now = NetworkManager.Singleton.ServerTime.Time;
            if (now < _next) return; _next = now + .05;
            var craters = FindObjectsByType<HeroHazards.SupernovaCraterComponent>(FindObjectsSortMode.None);
            var crater = craters.Length > 0 ? craters[0].transform.position : Vector3.zero;
            var shoePosition = _shoe != null ? _shoe.transform.position : Vector3.zero;
            var cinder = SeanCinderGate.Active.FirstOrDefault();
            var cinderState = cinder != null ? cinder.Capture() : default;
            object[] row = { now, elapsed, NetAuthority.LocalSlot, NetAuthority.IsHost ? 1 : 0,
                kit.HeroId == "sean" ? 1 : 0, caster.CharacterIndex, kit.IsIgnitionCannonActive ? 1 : 0,
                carrier.Held != null ? 1 : 0, kit.Skill2.ChargesRemaining, kit.UltimateCharge,
                caster.IsGrounded ? 1 : 0, caster.transform.position.y, kit.SupernovaPoseTime,
                craters.Length, crater.x, crater.z, FindObjectsByType<SeanIgnitionVisual>(FindObjectsSortMode.None).Length,
                _shoe != null && _shoe.State == SlipperState.InFlight && _shoe.Affinity == SlipperAffinity.FireExplosive ? 1 : 0,
                _shoe != null ? (int)_shoe.State : -1, shoePosition.x, shoePosition.y, shoePosition.z, front.StunLeft,
                kit.Skill2.DurationRemaining, DateTime.UtcNow.Ticks/(double)TimeSpan.TicksPerSecond,
                kit.Skill2.CooldownRemaining, carrier.Held != null ? (int)carrier.Held.Affinity : -1,
                front.transform.position.x, front.transform.position.y, front.transform.position.z,
                caster.transform.position.x,caster.transform.position.z,kit.Skill1.WindupRemaining,
                kit.Skill1.DurationRemaining,kit.Skill1.CooldownRemaining,caster.CanAct()?1:0,caster.CanMove()?1:0,
                FindObjectsByType<HeroHazards.FireTrailComponent>().Length,
                SeanCinderGate.Active.Count,cinderState.EventId,cinder!=null&&cinder.Spent?1:0,
                cinder!=null?cinder.Remaining:0,cinderState.Position.z,cinderState.SecondScale,
                front.PresentationTravelVelocity.z,caster.IsDefender?1:0,GameServices.Match.RoundNumber,
                kit.SteadyEmberRemaining,kit.ThrowChargeRate,
                NetAuthority.LocalSlot==1?carrier.ChargeRatio:carrier.ObservedChargePower,
                carrier.IsCharging?1:0,_steadyRegrabbed?1:0,Balance.ChargeFullTime };
            _writer.WriteLine(string.Join(",", row.Select(value => Convert.ToString(value, CultureInfo.InvariantCulture))));
        }
        private void DriveSteady(CharacterMotor caster,Carrier carrier,SeanHeroKit kit,float elapsed)
        {
            bool owns=NetAuthority.IsHost||NetAuthority.LocalSlot==1;
            if (_steadyGrantAt<0 && _steadyGrabAt<0 && elapsed>13 && _shoe.State==SlipperState.Loose)
            {
                if(owns)caster.Teleport(_shoe.transform.position+Vector3.back*.3f);
                _steadyGrabAt=elapsed+.5f;
            }
            if (_steadyGrantAt<0 && kit.SteadyEmberRemaining>0)_steadyGrantAt=elapsed;
            if (NetAuthority.IsHost && !_steadyDropped && _steadyGrantAt>=0 && elapsed>=_steadyGrantAt+.5f && carrier.Held==_shoe)
            {
                _steadyDropped=_shoe.HostDisarm();
                if(_steadyDropped)_shoe.transform.position=caster.transform.position+Vector3.forward*.3f;
            }
            if (_steadyGrantAt>=0 && !_steadyRegrabbed && _steadyRegrabAt<0 && _shoe.State==SlipperState.Loose)
                _steadyRegrabAt=elapsed+.3f;
            if (_steadyRegrabAt>=0 && !_steadyRegrabbed && carrier.Held==_shoe && _shoe.State==SlipperState.Held && _shoe.Holder==caster)
            {
                _steadyRegrabbed=true;
                if(owns)caster.Teleport(new Vector3(0,.12f,-8));
                _steadyChargeAt=elapsed+.4f;
            }
            if(NetAuthority.LocalSlot!=1)return;
            caster.Intent.Parked=false;caster.Intent.AimPoint=new Vector3(0,.15f,-2);caster.Intent.FaceAimPoint=true;
            bool firstGrab=_steadyGrantAt<0&&_steadyGrabAt>=0&&elapsed>=_steadyGrabAt&&elapsed<_steadyGrabAt+.3f;
            bool regrab=_steadyRegrabAt>=0&&!_steadyRegrabbed&&elapsed>=_steadyRegrabAt&&elapsed<_steadyRegrabAt+.3f;
            caster.Intent.Set(Verb.Grab,firstGrab||regrab);
            bool firstThrow=elapsed>=12&&elapsed<12.3f;
            bool boostedThrow=_steadyChargeAt>=0&&elapsed>=_steadyChargeAt&&elapsed<_steadyChargeAt+.3f;
            caster.Intent.Set(Verb.SpecialAbility,firstThrow||boostedThrow);
        }
        private void OnDestroy() => _writer?.Dispose();
    }
}
