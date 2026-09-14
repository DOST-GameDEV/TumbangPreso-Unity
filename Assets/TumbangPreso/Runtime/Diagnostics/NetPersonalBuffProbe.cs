using System;
using System.Globalization;
using System.IO;
using System.Linq;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    // Explicit three-player snapshot qualification. The observer replaces only
    // its local kit, then asks the real host for current world state. This is not
    // labelled a disconnected-process restart or an artificial extended buff.
    [DefaultExecutionOrder(-250)]
    public sealed class NetPersonalBuffProbe : MonoBehaviour
    {
        private static bool _enabled;
        private static string _hero, _scenario;
        private StreamWriter _writer;
        private bool _pickSent, _seededPick, _prepared, _refreshed;
        private float _refreshAt=-1, _next;
        private static string Argument(string key)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,key);
            return at>=0 && at+1<args.Length?args[at+1]:null;
        }
        private static HeroBuild FixtureBuild()=>new HeroBuild{HeroId=_hero,
            Slot1VariantId=_scenario=="fade"?"nemu.1.fade":null,
            Slot2VariantId=_scenario=="plating"?"dante.2.plating":null};
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            _enabled=Argument("-tp-personaltrace")!=null && !Environment.GetCommandLineArgs().Contains("-tp-tournament");
            if(!_enabled)return;
            _scenario=Argument("-tp-personalcase")??"ward";
            _hero=_scenario=="veil" || _scenario=="fade"?"nemu":"dante";
            UI.SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            Settings.SettingsStore.Current.CharacterPick=Roster.IndexIn(Roster.HeroPeople,_hero);
            var target=Settings.SettingsStore.HeroBuildFor(_hero);var build=FixtureBuild();
            target.Slot1VariantId=build.Slot1VariantId;target.Slot2VariantId=build.Slot2VariantId;
            foreach(var id in new[]{build.Slot1VariantId,build.Slot2VariantId})
                if(!string.IsNullOrEmpty(id))
                {
                    var progress=Settings.SettingsStore.Current.AbilityChallenges;
                    progress.RemoveAll(row=>row.VariantId==id);
                    progress.Add(new AbilityChallengeProgress{VariantId=id,Count=999});
                }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(!_enabled)return;
            var root=new GameObject("~NetPersonalBuffProbe");DontDestroyOnLoad(root);
            var probe=root.AddComponent<NetPersonalBuffProbe>();
            string path=Path.GetFullPath(Argument("-tp-personaltrace"));Directory.CreateDirectory(Path.GetDirectoryName(path));
            probe._writer=new StreamWriter(path){AutoFlush=true};
            probe._writer.WriteLine("wallTime,elapsed,local,active,remaining,cooldown,visuals,orbit,slow,immune,held,refreshed,x,y,z,ultcharge,variant");
        }
        private void Update()
        {
            int pick=Roster.IndexIn(Roster.HeroPeople,_hero);
            if(!_pickSent && NetAuthority.IsNetworked && NetAuthority.LocalSlot==1 && MatchRpc.Instance!=null)
            {
                var settings=Settings.SettingsStore.Current;
                MatchRpc.Instance.SelectLobbyPickServerRpc(pick,settings.CanPick,settings.SlipperPick);_pickSent=true;
            }
            var round=GameServices.Round;
            if(!_seededPick && NetAuthority.IsHost && round!=null
                && MatchRpc.Instance?.GetSeatInfo(1)?.CharacterPick==pick && round.PlayerAt(1)!=null)
            {
                _seededPick=true;MatchRpc.Instance.SyncPicksClientRpc(new[]{1,pick,-1,-1});MatchRpc.Instance.BroadcastPicks();
            }
            if(!NetAuthority.IsNetworked || round==null || !round.RoundActive || GameServices.Match==null
                || GameServices.Match.RoundNumber<1)return;
            var ready=FindFirstObjectByType<ReadyGate>();if(ready!=null && ready.CountingDown)return;
            var caster=round.PlayerAt(1);
            if(caster?.AbilitySystem?.Kit==null || caster.AbilitySystem.Kit.HeroId!=_hero)return;
            foreach(var brain in FindObjectsByType<AIController>(FindObjectsSortMode.None))brain.enabled=false;
            foreach(var input in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))input.enabled=false;
            foreach(var player in round.Players)if(player!=null){player.Intent.Clear();player.Intent.Parked=player!=caster;}
            float elapsed=UI.SceneFlow.SelectedRoundSeconds-round.TimeLeft;
            if(elapsed>21){_writer.Flush();Application.Quit();return;}
            if(!_prepared)
            {
                _prepared=true;
                if(NetAuthority.IsHost || NetAuthority.LocalSlot==1)
                {
                    caster.Teleport(new Vector3(0,.12f,-8));caster.transform.rotation=Quaternion.identity;
                    caster.ClearStun();caster.ClearTrip();
                }
                if(NetAuthority.IsHost)
                    foreach(var player in round.Players)if(player!=caster)player.Teleport(new Vector3(-9,.12f,-12+player.PlayerSlot*4));
            }
            if(NetAuthority.LocalSlot==1)
            {
                caster.Intent.Parked=false;caster.Intent.FaceAimPoint=true;caster.Intent.AimPoint=new Vector3(0,.2f,-2);
                caster.Intent.Set(_hero=="nemu"?Verb.Skill1:Verb.Skill2,elapsed>=12 && elapsed<12.3f);
            }
            var ability=caster.AbilitySystem;
            var skill=_hero=="nemu"?ability.Kit.Skill1:ability.Kit.Skill2;
            if(NetAuthority.LocalSlot==2 && !_refreshed)
            {
                if(skill.IsActive && _refreshAt<0)_refreshAt=Time.realtimeSinceStartup+.25f;
                if(_refreshAt>0 && Time.realtimeSinceStartup>=_refreshAt)
                {
                    _refreshed=true;
                    ability.BindHero(_hero,FixtureBuild());
                    MatchRpc.Instance.RequestWorldSnapshot();
                    skill=_hero=="nemu"?ability.Kit.Skill1:ability.Kit.Skill2;
                }
            }
            if(Time.realtimeSinceStartup<_next)return;_next=Time.realtimeSinceStartup+.025f;
            var orbit=caster.transform.Find("DanteOrbitingWard");var at=caster.transform.position;
            int visuals=_hero=="nemu"?caster.GetComponentsInChildren<NemuVeilPresentation>().Length
                :caster.GetComponentsInChildren<DanteCarapaceVisual>().Length;
            object[] row={DateTime.UtcNow.Ticks/(double)TimeSpan.TicksPerSecond,elapsed,NetAuthority.LocalSlot,
                skill.IsActive?1:0,skill.DurationRemaining,skill.CooldownRemaining,visuals,orbit!=null?orbit.childCount:0,
                caster.SpeedMultiplier,(_hero=="nemu"?ability.IsImmuneToTags:ability.IsImmuneToStuns)?1:0,
                caster.HoldingSlipper?1:0,_refreshed?1:0,at.x,at.y,at.z,ability.Kit.UltimateCharge,
                ability.HasVariant(_hero=="nemu"?"nemu.1.fade":"dante.2.plating")?1:0};
            _writer.WriteLine(string.Join(",",row.Select(value=>Convert.ToString(value,CultureInfo.InvariantCulture))));
        }
        private void OnDestroy()=>_writer?.Dispose();
    }
}
