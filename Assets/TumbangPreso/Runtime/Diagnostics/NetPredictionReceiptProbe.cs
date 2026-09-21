using System;
using System.Collections;
using System.IO;
using System.Linq;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    // Explicit opt-in fault fixture: older impossible pose, newer legal cast,
    // then a refused free recall. All requests/refusals use real transport.
    public sealed class NetPredictionReceiptProbe:MonoBehaviour
    {
        private StreamWriter _trace;
        private float _started,_next;
        private bool _pickSent,_picksApplied,_ran;
        private int _case;
        private static string Arg(string key){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,key);return i>=0&&i+1<a.Length?a[i+1]:null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure(){if(Arg("-tp-predictiontrace")!=null)SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            string path=Arg("-tp-predictiontrace");if(path==null)return;
            var root=new GameObject("~PredictionReceiptProbe");DontDestroyOnLoad(root);var p=root.AddComponent<NetPredictionReceiptProbe>();p._started=Time.realtimeSinceStartup;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));p._trace=new StreamWriter(path){AutoFlush=true};p._trace.WriteLine("real,local,round,elapsed,case,active1,active2,charge2,hero1");
        }
        private void OnDisable(){_trace?.Dispose();_trace=null;}
        private void Update()
        {
            if(Time.realtimeSinceStartup-_started>58){Application.Quit();return;}
            if(!NetAuthority.IsNetworked||!int.TryParse(Arg("-tp-predictionseat"),out int local)||NetAuthority.LocalSlot!=local)return;
            int pick=Roster.IndexIn(Roster.HeroPeople,"nemu");var rpc=MatchRpc.Instance;var round=GameServices.Round;var match=GameServices.Match;
            if(!_pickSent&&rpc!=null){var s=Settings.SettingsStore.Current;rpc.SelectLobbyPickServerRpc(pick,s.CanPick,s.SlipperPick);_pickSent=true;}
            if(NetAuthority.IsHost&&!_picksApplied&&round?.PlayerAt(2)!=null&&Enumerable.Range(0,3).All(i=>rpc.GetSeatInfo(i)?.CharacterPick==pick))
            {_picksApplied=true;rpc.SyncPicksClientRpc(new[]{0,pick,-1,-1,1,pick,-1,-1,2,pick,-1,-1});rpc.BroadcastPicks();}
            if(round==null||match==null||!round.RoundActive||match.IsWarmupBuffer||round.PlayerAt(1)?.AbilitySystem?.HeroId!="nemu")return;
            foreach(var ai in FindObjectsByType<AIController>())ai.enabled=false;foreach(var reader in FindObjectsByType<PlayerInputReader>())reader.enabled=false;
            foreach(var actor in round.Players){actor.Intent.Clear();actor.Intent.Parked=true;}
            var ready=FindAnyObjectByType<ReadyGate>();if(ready!=null&&ready.CountingDown)return;
            float elapsed=SceneFlow.SelectedRoundSeconds-round.TimeLeft;
            if(elapsed<3)return;
            if(local==1&&!_ran&&elapsed>=5){_ran=true;StartCoroutine(Cases());}
            if(elapsed>18){Application.Quit();return;}
            if(_trace==null||Time.realtimeSinceStartup<_next)return;_next=Time.realtimeSinceStartup+.04f;
            var kit=round.PlayerAt(1).AbilitySystem.Kit;
            _trace.WriteLine(FormattableString.Invariant($"{Time.realtimeSinceStartup-_started:F3},{local},{match.RoundNumber},{elapsed:F3},{_case},{(kit.Skill1.IsActive?1:0)},{(kit.Skill2.IsActive?1:0)},{kit.Skill2.ChargesRemaining},{kit.HeroId}"));
        }
        private void PredictAndRequest(HeroAbilitySystem.Slot slot,bool impossiblePose)
        {
            var actor=GameServices.Round.PlayerAt(1);var system=actor.AbilitySystem;var position=actor.transform.position;var forward=actor.transform.forward;var aim=position+forward*3;
            var outcome=system.ApplyNetworkCast(slot,position,forward,aim,0,authoritative:false);
            if(outcome!=HeroKit.CastOutcome.Cast)throw new InvalidOperationException("Fixture prediction was not cast");
            var pet=actor.GetComponent<CharacterVisual>()?.Companion;
            MatchRpc.Instance.RequestAbilityCastServerRpc(1,(int)slot,position+(impossiblePose?Vector3.right*100:Vector3.zero),forward,aim,0,pet!=null,pet!=null?pet.transform.position:Vector3.zero);
        }
        private IEnumerator Cases()
        {
            var actor=GameServices.Round.PlayerAt(1);var kit=actor.AbilitySystem.Kit;
            _case=1;PredictAndRequest(HeroAbilitySystem.Slot.Skill1,true);
            kit.Skill1.EndEarly(new AbilityContext(actor,actor.GetComponent<Carrier>(),actor.GetComponent<CombatVerbs>()));kit.Skill1.RefillForSandbox();
            PredictAndRequest(HeroAbilitySystem.Slot.Skill1,false);
            yield return new WaitForSecondsRealtime(1.3f);_case=2;
            yield return new WaitForSecondsRealtime(1.7f);
            PredictAndRequest(HeroAbilitySystem.Slot.Skill2,false);yield return new WaitForSecondsRealtime(1);
            _case=3;PredictAndRequest(HeroAbilitySystem.Slot.Skill2,true);
            yield return new WaitForSecondsRealtime(1);_case=4;
        }
    }
}
