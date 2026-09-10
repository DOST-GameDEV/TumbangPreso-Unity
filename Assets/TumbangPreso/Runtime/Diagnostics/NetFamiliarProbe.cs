using System;
using System.Globalization;
using System.IO;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    // Explicit verification fixture. Each process has separate simulation, transport
    // and CSV evidence. Only the owning client presses the actual skill inputs.
    public sealed class NetFamiliarProbe : MonoBehaviour
    {
        public static bool Active;
        private string _path,_scenario;
        private StreamWriter _writer;
        private CharacterMotor _who;
        private double _began=-1,_next;
        private bool _prepared, _stunApplied;
        private double _mashStarted=-1;
        private static string Argument(string name)
        {
            var args=Environment.GetCommandLineArgs();
            int at=Array.IndexOf(args,name);
            return at>=0 && at+1<args.Length?args[at+1]:null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            Active=!string.IsNullOrEmpty(Argument("-tp-familiartrace"));
            if(!Active)return;
            if(Environment.GetCommandLineArgs().Contains("-tp-tournament")){Active=false;return;}
            UI.SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            Settings.SettingsStore.Current.CharacterPick=Roster.GetPeople(GameMode.HeroStrike)
                .Select((p,i)=>(p,i)).First(p=>p.p.Id=="nemu").i;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(!Active)return;
            var go=new GameObject("~NetFamiliarProbe");DontDestroyOnLoad(go);
            var probe=go.AddComponent<NetFamiliarProbe>();
            probe._path=Argument("-tp-familiartrace");
            probe._scenario=Argument("-tp-familiarcase")??"recall";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(probe._path)));
            probe._writer=new StreamWriter(probe._path){AutoFlush=true};
            probe._writer.WriteLine("time,elapsed,host,local,possessed,devouring,x,y,z,bodyX,bodyZ,fieldX,fieldZ,fieldCount,s2active,ultactive,s2charges,ultcharge,modelX,modelZ,sourceCharges,sourceX,sourceZ,stunLeft,mashPresses,tripPresses");
        }
        private void Update()
        {
            if(!Active){enabled=false;return;}
            var round=GameServices.Round;
            if(!NetAuthority.IsNetworked || round==null || !round.RoundActive ||
                GameServices.Match==null || GameServices.Match.RoundNumber<1)return;
            var ready=FindFirstObjectByType<ReadyGate>();
            if(ready!=null && ready.CountingDown)return;
            _who=round.PlayerAt(1);
            if(_who==null || _who.AbilitySystem?.Kit==null)return;
            if(_who.AbilitySystem.Kit.HeroId!="nemu")
            {
                if(NetAuthority.IsHost)
                {
                    _who.CharacterIndex=Roster.GetPeople(GameMode.HeroStrike).Select((p,i)=>(p,i)).First(p=>p.p.Id=="nemu").i;
                    var entry=Resources.Load<RosterBook>("RosterBook").People.First(p=>p.Id=="nemu");
                    _who.GetComponent<CharacterVisual>().ApplyModel(entry.Model,entry.Tint,entry.Clips,entry.Palette,entry.PetModel);
                    _who.AbilitySystem.BindHero("nemu");MatchRpc.Instance.BroadcastPicks();
                }
                return;
            }
            double now=NetworkManager.Singleton.ServerTime.Time;
            if(_began<0)_began=now;
            float elapsed=(float)(now-_began);
            // Park the fixture's bystanders; this does not run without the switch.
            foreach(var brain in FindObjectsByType<AIController>(FindObjectsSortMode.None))brain.enabled=false;
            foreach(var reader in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))reader.enabled=false;
            foreach(var motor in round.Players)
                if(motor!=null){motor.Intent.Move=Vector2.zero;motor.Intent.Parked=motor!=_who && !(_scenario=="impact" && motor.PlayerSlot==0);}
            if(!_prepared)
            {
                if(_who.AbilitySystem.Kit.HeroId!="nemu")return;
                _prepared=true;
                _who.Intent.Parked=false;
                if(_scenario=="impact" && NetAuthority.IsHost)
                {
                    var source=round.PlayerAt(0);
                    source.CharacterIndex=Roster.GetPeople(GameMode.HeroStrike).Select((p,i)=>(p,i)).First(p=>p.p.Id=="dante").i;
                    var entry=Resources.Load<RosterBook>("RosterBook").People.First(p=>p.Id=="dante");
                    source.GetComponent<CharacterVisual>().ApplyModel(entry.Model,entry.Tint,entry.Clips,entry.Palette,entry.PetModel);
                    source.AbilitySystem.BindHero("dante");source.Teleport(new Vector3(0,.12f,-4));
                    source.transform.rotation=Quaternion.identity;MatchRpc.Instance.BroadcastPicks();
                }
                if(_scenario!="observe")_who.AbilitySystem.Kit.AddUltimateCharge(100);
                if(_scenario!="observe" && (NetAuthority.IsHost || NetAuthority.LocalSlot==1))_who.Teleport(new Vector3(0,.12f,_scenario=="impact"?-2.6f:-8));
                Debug.Log($"[FamiliarProbe] prepared local={NetAuthority.LocalSlot} host={NetAuthority.IsHost} case={_scenario}");
            }
            var pet=_who.GetComponent<CharacterVisual>()?.Companion;
            if(pet==null)return;
            if(_scenario=="impact" && NetAuthority.IsHost)
                round.PlayerAt(0).Intent.Set(Verb.Skill1,elapsed>=3 && elapsed<3.18f);
            if(_scenario=="mash")
            {
                if(NetAuthority.IsHost && elapsed>=3 && !_stunApplied)
                {
                    _stunApplied=true;_who.ApplyStagger(4,StunElement.Ice,6);
                }
                if(NetAuthority.LocalSlot==1 && _who.StunElement==StunElement.Ice)
                {
                    if(_mashStarted<0)_mashStarted=now;
                    double mashAge=now-_mashStarted;
                    _who.Intent.Set(Verb.Jump,mashAge<1.4 && mashAge%0.16<0.05);
                }
            }
            if(NetAuthority.LocalSlot==1 && (_scenario=="recall" || _scenario=="ultimate"))
            {
                _who.Intent.Set(Verb.Skill2,(elapsed>=3 && elapsed<3.18f) ||
                    (_scenario=="recall" && elapsed>=5.5f && elapsed<5.68f));
                _who.Intent.Set(Verb.Ultimate,_scenario=="ultimate" && elapsed>=5.5f && elapsed<5.68f);
                if(pet.IsPossessed)
                {
                    pet.transform.rotation=Quaternion.identity;
                    pet.SetPlayerInput(elapsed>=3.5f && elapsed<4.8f?Vector2.up*.6f:Vector2.zero);
                }
            }
            if(now>=_next)
            {
                _next=now+.05;
                var fields=FindObjectsByType<Abilities.HeroHazards.SeanceVoidComponent>(FindObjectsSortMode.None)
                    .Where(f=>f.OwnerSlot==1 && f.isActiveAndEnabled).ToArray();
                var pos=pet.IsDevouring?pet.DevourGround:pet.transform.position;
                var body=_who.transform.position;
                var field=fields.Length>0?fields[0].transform.position:new Vector3(float.NaN,0,float.NaN);
                _writer.WriteLine(string.Join(",",new object[]{now.ToString("F4",CultureInfo.InvariantCulture),
                    elapsed.ToString("F4",CultureInfo.InvariantCulture),NetAuthority.IsHost?1:0,NetAuthority.LocalSlot,
                    pet.IsPossessed?1:0,pet.IsDevouring?1:0,F(pos.x),F(pos.y),F(pos.z),F(body.x),F(body.z),
                    F(field.x),F(field.z),fields.Length,_who.AbilitySystem.Kit.Skill2.IsActive?1:0,
                    _who.AbilitySystem.Kit.Ultimate.IsActive?1:0,
                    _who.AbilitySystem.Kit.Skill2.ChargesRemaining,F(_who.AbilitySystem.Kit.UltimateCharge),
                    F(_who.GetComponent<CharacterVisual>().ModelRoot.position.x),F(_who.GetComponent<CharacterVisual>().ModelRoot.position.z),
                    round.PlayerAt(0).AbilitySystem.Kit.Skill1.ChargesRemaining,
                    F(round.PlayerAt(0).transform.position.x),F(round.PlayerAt(0).transform.position.z),F(_who.StunLeft),_who.StunMashPresses,_who.MashPresses}));
            }
            if(elapsed>(NetAuthority.IsHost?24:20)){_writer.Flush();Application.Quit();}
        }
        private static string F(float value)=>value.ToString("F4",CultureInfo.InvariantCulture);
        private void OnDestroy(){_writer?.Dispose();}
    }
}
