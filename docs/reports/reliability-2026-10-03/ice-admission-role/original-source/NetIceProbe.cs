using System;
using System.Globalization;
using System.IO;
using System.Linq;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    [DefaultExecutionOrder(-250)]
    public sealed class NetIceProbe : MonoBehaviour
    {
        private static bool _enabled;
        private string _scenario;
        private StreamWriter _writer;
        private bool _pickSent, _seededPick, _prepared;
        private bool _firstPredicted, _secondPredicted, _firstDenied, _secondDenied;
        private bool _seanUltimateSent;
        private int _seanUltimateStarts;
        private float _seanStarted, _seanImpact = -1;
        private double _next;
        private static string Argument(string key)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,key);
            return at>=0 && at+1<args.Length?args[at+1]:null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            _enabled=Argument("-tp-icetrace")!=null && !Environment.GetCommandLineArgs().Contains("-tp-tournament");
            if(!_enabled)return;
            UI.SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            bool sean=Argument("-tp-icecase")=="cheska-sean";
            bool joining=Environment.GetCommandLineArgs().Contains("-tp-join") ||
                !string.IsNullOrEmpty(Argument("-tp-lobbyjoin"));
            Settings.SettingsStore.Current.CharacterPick=Roster.IndexIn(Roster.HeroPeople,
                sean && joining ? "sean" : "cheska");
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(!_enabled)return;
            var root=new GameObject("~NetIceProbe");DontDestroyOnLoad(root);
            var probe=root.AddComponent<NetIceProbe>();probe._scenario=Argument("-tp-icecase")??"both";
            probe._seanStarted=Time.realtimeSinceStartup;
            string path=Path.GetFullPath(Argument("-tp-icetrace"));Directory.CreateDirectory(Path.GetDirectoryName(path));
            probe._writer=new StreamWriter(path){AutoFlush=true};
            probe._writer.WriteLine(probe._scenario=="cheska-sean"
                ? "time,wallTime,local,host,cheska,sean,ultStarted,phaseSeen,phase,phaseAge,phaseDuration,held,scale,stun,trip,rooted,chilled,canMove,simulated,parked,moveX,moveY,seanX,seanZ"
                : "time,elapsed,local,host,cheska,firstPredicted,secondPredicted,firstDenied,secondDenied,qcharges,echarges,sheets,walls,colliders,sheetX,sheetZ,wallX,wallZ,casterX,casterZ,wallTime");
        }
        private void OnEnable()=>HeroAbilitySystem.UltimateStarted+=OnUltimateStarted;
        private void OnUltimateStarted(CharacterMotor actor,HeroKit kit,HeroAbility ability)
        {
            if(_scenario=="cheska-sean" && NetAuthority.IsHost && actor!=null &&
               actor.PlayerSlot==0 && kit?.HeroId=="cheska")_seanUltimateStarts++;
        }
        private void Update()
        {
            if(_scenario=="cheska-sean"){UpdateSean();return;}
            int pick=Roster.IndexIn(Roster.HeroPeople,"cheska");
            if(!_pickSent && NetAuthority.IsNetworked && NetAuthority.LocalSlot==1 && MatchRpc.Instance!=null)
            {
                var settings=Settings.SettingsStore.Current;settings.CharacterPick=pick;
                MatchRpc.Instance.SelectLobbyPickServerRpc(pick,settings.CanPick,settings.SlipperPick);_pickSent=true;
            }
            var round=GameServices.Round;
            if(!_seededPick && NetAuthority.IsHost && round!=null && MatchRpc.Instance?.GetSeatInfo(1)?.CharacterPick==pick && round.PlayerAt(1)!=null)
            {
                _seededPick=true;MatchRpc.Instance.SyncPicksClientRpc(new[]{1,pick,-1,-1});MatchRpc.Instance.BroadcastPicks();
            }
            if(!NetAuthority.IsNetworked || round==null || !round.RoundActive || GameServices.Match==null || GameServices.Match.RoundNumber<1)return;
            var ready=FindFirstObjectByType<ReadyGate>();if(ready!=null && ready.CountingDown)return;
            var caster=round.PlayerAt(1);
            if(caster==null || caster.CharacterIndex!=pick || !(caster.AbilitySystem?.Kit is CheskaHeroKit kit))return;
            foreach(var brain in FindObjectsByType<AIController>(FindObjectsSortMode.None))brain.enabled=false;
            foreach(var input in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))input.enabled=false;
            foreach(var player in round.Players)if(player!=null){player.Intent.Clear();player.Intent.Parked=player!=caster;}
            float elapsed=UI.SceneFlow.SelectedRoundSeconds-round.TimeLeft;
            if(elapsed>25){_writer.Flush();Application.Quit();return;}
            if(!_prepared)
            {
                _prepared=true;
                if(NetAuthority.IsHost || NetAuthority.LocalSlot==1)
                {
                    caster.Teleport(new Vector3(0,.12f,-8));caster.transform.rotation=Quaternion.identity;
                    caster.ClearStun();caster.ClearTrip();
                }
                if(NetAuthority.IsHost)
                {
                    round.PlayerAt(0).Teleport(new Vector3(-9,.12f,-12));
                    round.PlayerAt(2).Teleport(new Vector3(9,.12f,5));
                    round.PlayerAt(3).Teleport(new Vector3(8,.12f,7));
                }
                Debug.Log("[IceProbe] prepared "+_scenario+" local="+NetAuthority.LocalSlot);
            }
            bool denied=_scenario=="denied";
            bool pair=_scenario=="sheet-then-denied";
            if(NetAuthority.IsHost && (denied || pair && elapsed>=14))
            {
                kit.Skill1.ApplyNetworkSnapshot(0,0);
                if(denied)kit.Skill2.ApplyNetworkSnapshot(0,0);
            }
            if(NetAuthority.LocalSlot==1)
            {
                // Arrange a stale owner resource estimate for explicit denial
                // cases only. The real host keeps zero and decides the requests.
                if(denied && elapsed>=12 && elapsed<12.68f)kit.Skill1.RefillForSandbox();
                if((denied || pair) && elapsed>=15 && elapsed<15.68f)
                    (pair?kit.Skill1:kit.Skill2).RefillForSandbox();
                caster.Intent.Parked=false;caster.Intent.AimPoint=new Vector3(0,.1f,-2);caster.Intent.FaceAimPoint=true;
                caster.Intent.Set(Verb.Skill1,elapsed>=12 && elapsed<12.6f || pair && elapsed>=15 && elapsed<15.6f);
                if(!pair)caster.Intent.Set(Verb.Skill2,elapsed>=15 && elapsed<15.6f);
                if(elapsed>=12.65f && elapsed<13.4f)caster.Intent.Move=Vector2.left;
                var system=caster.AbilitySystem;
                if(elapsed>=12.6f && elapsed<14)
                {
                    _firstPredicted|=system.LastAnswer(HeroAbilitySystem.Slot.Skill1)==HeroKit.CastOutcome.Cast;
                    _firstDenied|=system.LastAnswer(HeroAbilitySystem.Slot.Skill1)==HeroKit.CastOutcome.Cooling;
                }
                if(elapsed>=15.6f && elapsed<17.5f)
                {
                    var slot=pair?HeroAbilitySystem.Slot.Skill1:HeroAbilitySystem.Slot.Skill2;
                    _secondPredicted|=system.LastAnswer(slot)==HeroKit.CastOutcome.Cast;
                    _secondDenied|=system.LastAnswer(slot)==HeroKit.CastOutcome.Cooling;
                }
            }
            double now=NetworkManager.Singleton.ServerTime.Time;if(now<_next)return;_next=now+.05;
            var sheets=FindObjectsByType<HeroHazards.IceSheetComponent>(FindObjectsSortMode.None);
            var walls=FindObjectsByType<HeroHazards.IceBarricadeComponent>(FindObjectsSortMode.None);
            Vector3 sheet=sheets.Length>0?sheets[0].transform.position:new Vector3(float.NaN,0,float.NaN);
            Vector3 wall=walls.Length>0?walls[0].transform.position:new Vector3(float.NaN,0,float.NaN);
            int colliders=walls.Sum(w=>w.GetComponentsInChildren<Collider>().Count(c=>c.enabled && !c.isTrigger));
            object[] row={now,elapsed,NetAuthority.LocalSlot,NetAuthority.IsHost?1:0,kit.HeroId=="cheska"?1:0,
                _firstPredicted?1:0,_secondPredicted?1:0,_firstDenied?1:0,_secondDenied?1:0,
                kit.Skill1.ChargesRemaining,kit.Skill2.ChargesRemaining,sheets.Length,walls.Length,colliders,
                sheet.x,sheet.z,wall.x,wall.z,caster.transform.position.x,caster.transform.position.z,
                DateTime.UtcNow.Ticks/(double)TimeSpan.TicksPerSecond};
            _writer.WriteLine(string.Join(",",row.Select(v=>Convert.ToString(v,CultureInfo.InvariantCulture))));
        }
        private void UpdateSean()
        {
            if(Time.realtimeSinceStartup-_seanStarted>50){Application.Quit();return;}
            int cheska=Roster.IndexIn(Roster.HeroPeople,"cheska");
            int sean=Roster.IndexIn(Roster.HeroPeople,"sean");
            int local=NetAuthority.LocalSlot;
            if(!NetAuthority.IsNetworked || local<0 || local>1)return;
            int pick=local==0?cheska:sean;
            if(!_pickSent && MatchRpc.Instance!=null)
            {
                var settings=Settings.SettingsStore.Current;
                settings.CharacterPick=pick;
                MatchRpc.Instance.SelectLobbyPickServerRpc(pick,settings.CanPick,settings.SlipperPick);
                _pickSent=true;
            }
            var round=GameServices.Round;
            if(NetAuthority.IsHost && !_seededPick && round?.PlayerAt(1)!=null &&
               MatchRpc.Instance?.GetSeatInfo(0)?.CharacterPick==cheska &&
               MatchRpc.Instance?.GetSeatInfo(1)?.CharacterPick==sean)
            {
                _seededPick=true;
                MatchRpc.Instance.SyncPicksClientRpc(new[]{0,cheska,-1,-1,1,sean,-1,-1});
                MatchRpc.Instance.BroadcastPicks();
            }
            if(round==null || !round.RoundActive || GameServices.Match==null ||
               GameServices.Match.RoundNumber<1 || GameServices.Match.IsWarmupBuffer)return;
            var ready=FindFirstObjectByType<ReadyGate>();
            if(ready!=null && ready.CountingDown)return;
            var caster=round.PlayerAt(0);
            var victim=round.PlayerAt(1);
            if(caster?.AbilitySystem?.HeroId!="cheska" ||
               victim?.AbilitySystem?.HeroId!="sean" ||
               !(caster.AbilitySystem.Kit is CheskaHeroKit kit))return;
            if(!_prepared)
            {
                _prepared=true;
                foreach(var brain in FindObjectsByType<AIController>(FindObjectsSortMode.None))brain.enabled=false;
                foreach(var input in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))input.enabled=false;
                foreach(var player in round.Players)
                    if(player!=null){player.Intent.Clear();player.Intent.Parked=player!=caster && player!=victim;}
                if(NetAuthority.IsHost)
                {
                    caster.Teleport(new Vector3(-9,.12f,-12));
                    victim.Teleport(new Vector3(0,.12f,-8));
                    victim.ClearStun();victim.ClearTrip();
                    kit.AddUltimateCharge(kit.UltimateCost);
                }
                else
                {
                    victim.Teleport(new Vector3(0,.12f,-8));
                    victim.ClearStun();victim.ClearTrip();
                }
                Debug.Log("[IceProbe] prepared cheska-sean local="+local);
            }
            float elapsed=UI.SceneFlow.SelectedRoundSeconds-round.TimeLeft;
            if(NetAuthority.IsHost && !_seanUltimateSent && elapsed>=6)
            {
                _seanUltimateSent=true;
                caster.Intent.Set(Verb.Ultimate,true);
                caster.Intent.BufferPress(Verb.Ultimate);
            }
            else if(NetAuthority.IsHost && _seanUltimateSent)
                caster.Intent.Set(Verb.Ultimate,false);
            if(local==1)
            {
                victim.Intent.Parked=false;
                victim.Intent.Move=elapsed<5.5f || _seanImpact>=0 && Time.realtimeSinceStartup-_seanImpact>3.2f &&
                    Time.realtimeSinceStartup-_seanImpact<3.45f ? Vector2.zero : Vector2.up;
            }
            if(victim.StunLeft>0 && _seanImpact<0)_seanImpact=Time.realtimeSinceStartup;
            if(_seanImpact>=0 && Time.realtimeSinceStartup-_seanImpact>5.5f)
            {_writer.Flush();Application.Quit();return;}
            double now=NetworkManager.Singleton.ServerTime.Time;
            if(now<_next)return;
            _next=now+.05;
            var phase=SharedUltimatePhase.Instance;
            bool active=phase!=null && phase.Active;
            bool phaseSeen=phase!=null && phase.PhaseId>0;
            Vector2 axis=victim.Intent.MoveAxis;
            object[] row={now,DateTime.UtcNow.Ticks/(double)TimeSpan.TicksPerSecond,
                local,NetAuthority.IsHost?1:0,caster.AbilitySystem.HeroId=="cheska"?1:0,
                victim.AbilitySystem.HeroId=="sean"?1:0,_seanUltimateStarts,
                phaseSeen?1:0,active?1:0,active?SharedUltimatePhase.Now-phase.Began:0,
                active?phase.Duration:0,PresentationClock.Held?1:0,Time.timeScale,
                victim.StunLeft,victim.TripLeft,victim.RootedLeft,victim.ChilledLeft,
                victim.CanMove()?1:0,victim.IsLocallySimulated()?1:0,
                victim.Intent.Parked?1:0,axis.x,axis.y,
                victim.transform.position.x,victim.transform.position.z};
            _writer.WriteLine(string.Join(",",row.Select(v=>Convert.ToString(v,CultureInfo.InvariantCulture))));
        }
        private void OnDestroy()
        {
            HeroAbilitySystem.UltimateStarted-=OnUltimateStarted;
            _writer?.Dispose();
        }
    }
}
