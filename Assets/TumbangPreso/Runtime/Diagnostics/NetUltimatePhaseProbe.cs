using System;
using System.IO;
using System.Linq;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    [DefaultExecutionOrder(-250)]
    public sealed class NetUltimatePhaseProbe : MonoBehaviour
    {
        private StreamWriter _trace;
        private float _started,_next;
        private double _firstPhase=-1;
        private bool _prepared,_sent,_joined,_lateRequest,_resent,_oldResent,_pickSent,_picksApplied;
        private int[] _starts=new int[4];
        private bool _disconnected;
        private float _initialWarning;
        private long _savedMatch,_savedPhase;
        private int _savedRound;
        private double _savedBegan;
        private float _savedClock;
        private UltimateCommit[] _saved;
        private static string Arg(string key)
        {var a=Environment.GetCommandLineArgs();int at=Array.IndexOf(a,key);return at>=0&&at+1<a.Length?a[at+1]:null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tp-tournament") >= 0) return;
            if(Arg("-tp-ulttrace")==null)return;
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            Settings.SettingsStore.Current.CharacterPick=Roster.IndexIn(Roster.HeroPeople,"phaister");
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tp-tournament") >= 0) return;
            string path=Arg("-tp-ulttrace");if(path==null)return;
            var go=new GameObject("~NetUltimatePhaseProbe");DontDestroyOnLoad(go);var probe=go.AddComponent<NetUltimatePhaseProbe>();
            probe._started=Time.realtimeSinceStartup;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            probe._trace=new StreamWriter(path){AutoFlush=true};
            probe._trace.WriteLine("real,server,local,round,clock,phase,id,count,scale,requested,charge0,charge1,charge2,starts0,starts1,starts2,warning1,initialWarning,hero1,bot1,activeUlt1");
        }
        private void OnEnable()=>HeroAbilitySystem.UltimateStarted+=Started;
        private void OnDisable(){HeroAbilitySystem.UltimateStarted-=Started;_trace?.Dispose();_trace=null;}
        private void Started(CharacterMotor actor,HeroKit kit,HeroAbility ability)
        {_starts[actor.PlayerSlot]++;if(actor.PlayerSlot==1)_initialWarning=ability.WindupRemaining;}
        private void Update()
        {
            if(Time.realtimeSinceStartup-_started>60){Application.Quit();return;}
            var round=GameServices.Round;var match=GameServices.Match;
            if(!NetAuthority.IsNetworked||!int.TryParse(Arg("-tp-ultseat"),out int local)||NetAuthority.LocalSlot!=local)return;
            int pick=Roster.IndexIn(Roster.HeroPeople,"phaister");
            if(!_pickSent&&MatchRpc.Instance!=null)
            {
                var settings=Settings.SettingsStore.Current;
                MatchRpc.Instance.SelectLobbyPickServerRpc(pick,settings.CanPick,settings.SlipperPick);_pickSent=true;
            }
            if(NetAuthority.IsHost&&!_picksApplied&&round?.PlayerAt(2)!=null&&MatchRpc.Instance!=null
                &&Enumerable.Range(0,3).All(i=>MatchRpc.Instance.GetSeatInfo(i)?.CharacterPick==pick))
            {
                _picksApplied=true;MatchRpc.Instance.SyncPicksClientRpc(new[]{0,pick,-1,-1,1,pick,-1,-1,2,pick,-1,-1});
                MatchRpc.Instance.BroadcastPicks();
            }
            if(round==null||match==null||!round.RoundActive||match.IsWarmupBuffer)return;
            if(Enumerable.Range(0,3).Any(i=>round.PlayerAt(i)?.AbilitySystem?.HeroId!="phaister"))return;
            foreach(var ai in FindObjectsByType<AIController>())ai.enabled=false;
            foreach(var reader in FindObjectsByType<PlayerInputReader>())reader.enabled=false;
            foreach(var switcher in FindObjectsByType<DebugPlayerSwitcher>())switcher.enabled=false;
            var ready=FindAnyObjectByType<ReadyGate>();if(ready!=null&&ready.CountingDown)return;
            float elapsed=SceneFlow.SelectedRoundSeconds-round.TimeLeft;
            if(elapsed<3)return;
            Hud.Instance?.ShowReadyPrompt(false);
            if(!_prepared)
            {
                _prepared=true;
                foreach(var actor in round.Players)
                {actor.Intent.Clear();actor.Intent.Parked=false;if(local!=3)actor.AbilitySystem?.Kit?.AddUltimateCharge(100);}
                MatchRpc.Instance?.BroadcastWorldSnapshot();
            }
            var phase=SharedUltimatePhase.Instance;
            if(local==1&&!_sent&&elapsed>=6)
            {
                _sent=true;var actor=round.PlayerAt(1);actor.Intent.Set(Verb.Ultimate,true);actor.Intent.BufferPress(Verb.Ultimate);
            }
            // Join the cohort in the exact host frame that receives the remote request.
            if(NetAuthority.IsHost&&!_joined&&SharedUltimatePhase.Collecting)
            {
                _joined=true;var actor=round.PlayerAt(0);actor.Intent.Set(Verb.Ultimate,true);actor.Intent.BufferPress(Verb.Ultimate);
            }
            phase=SharedUltimatePhase.Instance;
            if(phase!=null&&phase.Active&&phase.Sealed)
            {
                if(_firstPhase<0)_firstPhase=phase.Began;
                if(_saved==null){_saved=phase.Commits.ToArray();_savedMatch=phase.MatchId;_savedPhase=phase.PhaseId;_savedRound=phase.Round;_savedBegan=phase.Began;_savedClock=phase.FrozenRoundTime;}
                double age=SharedUltimatePhase.Now-phase.Began;
                if(local==1&&!_disconnected&&Arg("-tp-ult-disconnect")=="caster"&&age>.7)
                {
                    _disconnected=true;Debug.Log("[UltimateProbe] accepted caster disconnecting during shared hold");_trace?.Flush();
                    NetSession.Instance.Stop();Application.Quit();return;
                }
                if(local==2&&!_lateRequest&&age>.6)
                {
                    _lateRequest=true;var actor=round.PlayerAt(2);
                    MatchRpc.Instance.RequestSharedUltimate(2,actor.transform.position,actor.transform.forward,Vector3.zero,0);
                }
                if(NetAuthority.IsHost&&!_resent&&age>.9)
                {_resent=true;MatchRpc.Instance.BroadcastUltimatePhase(phase);MatchRpc.Instance.BroadcastUltimatePhase(phase);}
            }
            if(_firstPhase>=0&&SharedUltimatePhase.Now-_firstPhase>4.5&&!_oldResent)
            {
                _oldResent=true;
                if(NetAuthority.IsHost){MatchRpc.Instance.HostSetTimeScale(.5f);SendOldPhase();}
            }
            if(_firstPhase>=0&&SharedUltimatePhase.Now-_firstPhase>(Arg("-tp-ult-latejoin")=="1"?20:7)){Application.Quit();return;}
            if(local==3&&Time.realtimeSinceStartup-_started>18){Application.Quit();return;}
            if(_trace==null||Time.realtimeSinceStartup<_next)return;_next=Time.realtimeSinceStartup+.035f;
            float Charge(int seat)=>round.PlayerAt(seat)?.AbilitySystem?.Kit?.UltimateCharge??-1;
            var remote=round.PlayerAt(1)?.AbilitySystem;
            _trace.WriteLine(FormattableString.Invariant($"{Time.realtimeSinceStartup-_started:F4},{SharedUltimatePhase.Now:F4},{local},{match.RoundNumber},{round.TimeLeft:F4},{(phase!=null&&phase.Active?1:0)},{phase?.PhaseId??0},{phase?.Commits.Count??0},{Time.timeScale:F3},{PresentationClock.RequestedScale:F3},{Charge(0):F3},{Charge(1):F3},{Charge(2):F3},{_starts[0]},{_starts[1]},{_starts[2]},{remote?.Kit?.Ultimate?.WindupRemaining??0:F4},{_initialWarning:F4},{remote?.HeroId},{(round.PlayerAt(1)?.IsBot==true?1:0)},{(remote?.Kit?.Ultimate?.IsActive==true?1:0)}"));
        }
        private void SendOldPhase()
        {
            if(_saved==null)return;
            using var writer=new FastBufferWriter(512,Allocator.Temp);
            writer.WriteValueSafe(_savedMatch);writer.WriteValueSafe(_savedRound);writer.WriteValueSafe(_savedPhase);writer.WriteValueSafe(_savedBegan);
            writer.WriteValueSafe(1f);writer.WriteValueSafe(_savedClock);writer.WriteValueSafe(_saved.Length);
            foreach(var cast in _saved)
            {
                writer.WriteValueSafe(cast.Seat);writer.WriteValueSafe(cast.Request);writer.WriteValueSafe(cast.Position);
                writer.WriteValueSafe(cast.Forward);writer.WriteValueSafe(cast.Aim);writer.WriteValueSafe(cast.Held);
                writer.WriteValueSafe(cast.HasFamiliar);writer.WriteValueSafe(cast.FamiliarPosition);
            }
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("UltimatePhase",writer);
        }
    }
}
