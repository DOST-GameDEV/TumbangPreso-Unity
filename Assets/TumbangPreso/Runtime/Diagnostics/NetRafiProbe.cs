using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    // Opt-in integration route: real owning input, host effects, repeated normal
    // world snapshots and two remote views. No production profile or service.
    [DefaultExecutionOrder(-250)]
    public sealed class NetRafiProbe : MonoBehaviour
    {
        [Serializable] private sealed class Water
        {
            public int id, kind, owner;
            public float remaining, duration, range;
            public Vector3 position, forward;
            public Vector3[] path;
        }
        [Serializable] private sealed class Frame
        {
            public double wall;
            public float elapsed, ultimate, passive, movementScale, travelSpeed;
            public int shoeState;public bool regrabbed,defending;public Vector3 casterPosition, targetPosition, loosePosition;public float targetVelocity;public bool canUpright;
            public int local, q, e, starts, snapshots;
            public long epoch;
            public string bodyClip;
            public Water[] fields;
        }
        private static string _path,_case;
        private float _grabAt=-1,_grantAt=-1,_regrabAt=-1,_walkAt=-1;
        private bool _dropped,_regrabbed,_integrationStaged;
        private StreamWriter _writer;
        private bool _pickSent, _pickApplied, _prepared, _q, _e, _ultimate;
        private float _began, _next;
        private int _starts, _snapshots;
        private readonly HashSet<int> _repeated = new HashSet<int>();
        private static string Arg(string key)
        {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            _case=Arg("-tp-raficase")??"integration";
            _path=Environment.GetCommandLineArgs().Contains("-tp-tournament")?null:Arg("-tp-rafitrace");
            if(_path==null)return;
            UI.SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            Settings.SettingsStore.Current.CharacterPick=Roster.IndexIn(Roster.HeroPeople,"rafi");
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(_path==null)return;
            var go=new GameObject("~NetRafiProbe");DontDestroyOnLoad(go);
            var probe=go.AddComponent<NetRafiProbe>();probe._began=Time.realtimeSinceStartup;
            var path=Path.GetFullPath(_path);Directory.CreateDirectory(Path.GetDirectoryName(path));
            probe._writer=new StreamWriter(path){AutoFlush=true};
        }
        private void OnEnable()=>HeroAbilitySystem.UltimateStarted+=Started;
        private void OnDisable(){HeroAbilitySystem.UltimateStarted-=Started;_writer?.Dispose();_writer=null;}
        private void Started(CharacterMotor who,HeroKit kit,HeroAbility ability)
        {if(who.PlayerSlot==1&&kit.HeroId=="rafi")_starts++;}
        private void Update()
        {
            if(Time.realtimeSinceStartup-_began>65){Application.Quit(2);return;}
            int pick=Roster.IndexIn(Roster.HeroPeople,"rafi");
            if(!_pickSent&&NetAuthority.IsNetworked&&NetAuthority.LocalSlot==1&&MatchRpc.Instance!=null)
            {
                var s=Settings.SettingsStore.Current;
                MatchRpc.Instance.SelectLobbyPickServerRpc(pick,s.CanPick,s.SlipperPick);_pickSent=true;
            }
            var round=GameServices.Round;
            if(!_pickApplied&&NetAuthority.IsHost&&round?.PlayerAt(1)!=null&&MatchRpc.Instance?.GetSeatInfo(1)?.CharacterPick==pick)
            {_pickApplied=true;MatchRpc.Instance.SyncPicksClientRpc(new[]{1,pick,-1,-1});MatchRpc.Instance.BroadcastPicks();}
            if(!NetAuthority.IsNetworked||round==null||!round.RoundActive||GameServices.Match==null||GameServices.Match.IsWarmupBuffer)return;
            var ready=FindAnyObjectByType<ReadyGate>();if(ready!=null&&ready.CountingDown)return;
            var caster=round.PlayerAt(1);if(caster?.AbilitySystem?.Kit is not RafiHeroKit kit)return;
            foreach(var ai in FindObjectsByType<AIController>())ai.enabled=false;
            foreach(var input in FindObjectsByType<PlayerInputReader>())input.enabled=false;
            foreach(var switcher in FindObjectsByType<DebugPlayerSwitcher>())switcher.enabled=false;
            foreach(var actor in round.Players){actor.Intent.Clear();actor.Intent.Parked=actor!=caster;}
            if(!_prepared)
            {
                _prepared=true;
                if(NetAuthority.IsHost||NetAuthority.LocalSlot==1)
                {caster.Teleport(new Vector3(0,.12f,_case=="water"?-6:-Balance.ConfinementRadius-1));caster.transform.rotation=Quaternion.identity;kit.AddUltimateCharge(100);}
                if(NetAuthority.IsHost)
                {
                    foreach(var actor in round.Players)if(actor!=caster)actor.Teleport(new Vector3(8,.12f,-5+actor.PlayerSlot*3));
                    foreach(var shoe in FindObjectsByType<Slipper>()){shoe.HostDisarm();shoe.transform.position=new Vector3(9,shoe.RestHeight,shoe.SeatOfOrigin*2);}
                    if(_case=="backwash"||_case=="integration")
                    {
                        var own=FindObjectsByType<Slipper>().FirstOrDefault(shoe=>shoe.OwnerSlot==1);
                        own?.HostForceEquip(caster);
                    }
                    MatchRpc.Instance.BroadcastWorldSnapshot();
                }
            }
            float elapsed=UI.SceneFlow.SelectedRoundSeconds-round.TimeLeft;
            if(elapsed>27){Application.Quit();return;}
            if(_case=="integration")
            {
                if(elapsed<17)Backwash(caster,kit,elapsed);
                else Integration(caster,kit,elapsed);
            }
            else if(_case=="backwash")Backwash(caster,kit,elapsed);
            else if(NetAuthority.LocalSlot==1)
            {
                caster.Intent.Parked=false;caster.Intent.FaceAimPoint=true;caster.Intent.AimPoint=new Vector3(0,.1f,2);
                void Press(Verb verb){caster.Intent.Set(verb,true);caster.Intent.BufferPress(verb);}
                if(!_q&&elapsed>=12){_q=true;Press(Verb.Skill1);}
                if(elapsed>=14&&elapsed<14.6f)caster.Intent.Move=Vector2.right*.5f;
                if(!_e&&elapsed>=15){_e=true;Press(Verb.Skill2);}
                if(!_ultimate&&elapsed>=18){_ultimate=true;Press(Verb.Ultimate);}
            }
            var fields=WorldEffectSnapshot.Capture().Where(f=>RafiWaterField.IsWater(f.Type)).ToArray();
            if(NetAuthority.IsHost)
                foreach(var field in fields)
                    if(field.Duration-field.Remaining>=.2f&&_repeated.Add(field.EventId))
                    {MatchRpc.Instance.BroadcastWorldSnapshot();MatchRpc.Instance.BroadcastWorldSnapshot();_snapshots+=2;}
            if(Time.realtimeSinceStartup<_next)return;_next=Time.realtimeSinceStartup+.035f;
            var frame=new Frame{wall=DateTime.UtcNow.Ticks/(double)TimeSpan.TicksPerSecond,elapsed=elapsed,
                passive=kit.BackwashRemaining,movementScale=kit.MovementSpeedScale,
                travelSpeed=new Vector2(caster.PresentationTravelVelocity.x,caster.PresentationTravelVelocity.z).magnitude,
                casterPosition=caster.transform.position,defending=caster.IsDefender,regrabbed=_regrabbed,
                targetPosition=round.PlayerAt(2)?.transform.position??Vector3.zero,
                targetVelocity=round.PlayerAt(2)?.PresentationTravelVelocity.z??0,
                loosePosition=FindObjectsByType<Slipper>().FirstOrDefault(shoe=>shoe.OwnerSlot==3)?.transform.position??Vector3.zero,
                canUpright=round.Lata!=null&&round.Lata.IsUpright,
                shoeState=(int)(FindObjectsByType<Slipper>().FirstOrDefault(shoe=>shoe.OwnerSlot==1)?.State??SlipperState.Loose),
                local=NetAuthority.LocalSlot,epoch=MatchRpc.Instance.PresentationMatchId,q=kit.Skill1.ChargesRemaining,
                e=kit.Skill2.ChargesRemaining,ultimate=kit.UltimateCharge,starts=_starts,snapshots=_snapshots,
                bodyClip=caster.GetComponentInChildren<CharacterAnimator>(true)?.CurrentClipName??"",
                fields=fields.Select(f=>new Water{id=f.EventId,kind=(int)f.Type,owner=f.Owner,remaining=f.Remaining,
                    duration=f.Duration,range=f.SecondScale,position=f.Position,forward=f.Forward,path=f.Path}).ToArray()};
            _writer.WriteLine(JsonUtility.ToJson(frame));
        }
        private void Integration(CharacterMotor caster,RafiHeroKit kit,float elapsed)
        {
            var target=GameServices.Round.PlayerAt(2);
            if(!_integrationStaged)
            {
                _integrationStaged=true;
                if(NetAuthority.IsHost||NetAuthority.LocalSlot==1)caster.Teleport(new Vector3(0,.12f,-8));
                if(target!=null&&(NetAuthority.IsHost||NetAuthority.LocalSlot==2))target.Teleport(new Vector3(0,.12f,-3));
                if(NetAuthority.IsHost)
                {
                    var loose=FindObjectsByType<Slipper>().FirstOrDefault(shoe=>shoe.OwnerSlot==3);
                    if(loose!=null){loose.HostDisarm();loose.transform.position=new Vector3(2,Slipper.GroundY(new Vector3(2,0,-1))+loose.RestHeight,-1);}
                    MatchRpc.Instance.BroadcastWorldSnapshot();
                }
            }
            if(NetAuthority.LocalSlot!=1)return;
            caster.Intent.Parked=false;caster.Intent.FaceAimPoint=true;caster.Intent.AimPoint=new Vector3(0,.1f,5);
            if(!_ultimate&&elapsed>=18){_ultimate=true;caster.Intent.Set(Verb.Ultimate,true);caster.Intent.BufferPress(Verb.Ultimate);}
        }
        private void Backwash(CharacterMotor caster,RafiHeroKit kit,float elapsed)
        {
            var shoe=FindObjectsByType<Slipper>().FirstOrDefault(s=>s.OwnerSlot==1);
            var carrier=caster.GetComponent<Carrier>();if(shoe==null||carrier==null)return;
            bool owns=NetAuthority.IsHost||NetAuthority.LocalSlot==1;
            if(_grantAt<0&&_grabAt<0&&elapsed>13&&shoe.State==SlipperState.Loose)
            {
                if(owns)caster.Teleport(shoe.transform.position+Vector3.back*.3f);
                _grabAt=elapsed+.5f;
            }
            if(_grantAt<0&&kit.BackwashRemaining>0)_grantAt=elapsed;
            if(NetAuthority.IsHost&&!_dropped&&_grantAt>=0&&elapsed>=_grantAt+.2f&&carrier.Held==shoe)
            {
                _dropped=shoe.HostDisarm();if(_dropped)shoe.transform.position=caster.transform.position+Vector3.forward*.3f;
            }
            if(_grantAt>=0&&!_regrabbed&&_regrabAt<0&&shoe.State==SlipperState.Loose)_regrabAt=elapsed+.15f;
            if(_regrabAt>=0&&!_regrabbed&&carrier.Held==shoe&&shoe.State==SlipperState.Held&&shoe.Holder==caster)
            {_regrabbed=true;_walkAt=elapsed+.05f;}
            if(NetAuthority.LocalSlot!=1)return;
            caster.Intent.Parked=false;caster.Intent.FaceAimPoint=true;caster.Intent.AimPoint=caster.transform.position+Vector3.forward*6;
            bool grab=_grantAt<0&&_grabAt>=0&&elapsed>=_grabAt&&elapsed<_grabAt+.3f;
            bool regrab=_regrabAt>=0&&!_regrabbed&&elapsed>=_regrabAt&&elapsed<_regrabAt+.3f;
            caster.Intent.Set(Verb.Grab,grab||regrab);
            caster.Intent.Set(Verb.SpecialAbility,elapsed>=12&&elapsed<12.3f);
            bool ordinary=elapsed>=10&&elapsed<10.8f;
            bool boosted=_walkAt>=0&&elapsed>=_walkAt&&elapsed<_walkAt+.5f;
            if(ordinary||boosted)caster.Intent.Move=Vector2.right;
        }
    }
}
