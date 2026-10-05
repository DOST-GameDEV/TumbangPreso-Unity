using System;
using System.Collections;
using System.IO;
using System.Linq;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    public sealed class NetReplayProbe : MonoBehaviour
    {
        private StreamWriter _trace;
        private float _started,_next,_finished=-1;
        private bool _running,_shot,_faultInjected,_arcClipLogged,_arcViewLogged;
        private static string Argument(string key)
        {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tp-tournament") >= 0) return;if(Argument("-tp-replaytrace")!=null)SceneFlow.PinSelectedRules(CustomGameRules.Defaults(Argument("-tp-replaymode")=="hero"?GameMode.HeroStrike:GameMode.Classic));}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tp-tournament") >= 0) return;
            string path=Argument("-tp-replaytrace");if(path==null)return;
            var root=new GameObject("~NetReplayProbe");DontDestroyOnLoad(root);var probe=root.AddComponent<NetReplayProbe>();probe._started=Time.realtimeSinceStartup;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));probe._trace=new StreamWriter(path){AutoFlush=true};
            probe._trace.WriteLine("real,local,round,epoch,score1,clips,clip,ready,half,view,remaining,held,sim,fallback,fault,liveFields,score0,score2,score3");
        }
        private void OnDisable(){_trace?.Dispose();_trace=null;}
        private void Update()
        {
            if(Time.realtimeSinceStartup-_started>100){Application.Quit();return;}
            var match=GameServices.Match;var round=GameServices.Round;
            if(!NetAuthority.IsNetworked||match==null||round?.Lata==null)return;
            foreach(var ai in FindObjectsByType<AIController>())ai.enabled=false;
            foreach(var reader in FindObjectsByType<PlayerInputReader>())reader.enabled=false;
            foreach(var actor in round.Players){actor.Intent.Clear();actor.Intent.Parked=true;}
            if(!int.TryParse(Argument("-tp-replayseat"),out int expected)||NetAuthority.LocalSlot!=expected)return;
            var ready=FindAnyObjectByType<ReadyGate>();
            if(NetAuthority.IsHost&&!_running&&round.RoundActive&&!match.IsWarmupBuffer&&(ready==null||!ready.CountingDown)&&SceneFlow.SelectedRoundSeconds-round.TimeLeft>3)
            {_running=true;StartCoroutine(Exchange());}
            if(expected==2&&!_faultInjected)
            {
                var rpc=Net.MatchRpc.Instance;var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                if(Argument("-tp-replay-fault")=="missing")
                {
                    var clips=(System.Collections.IList)typeof(Net.MatchRpc).GetField("_receivedClips",flags).GetValue(rpc);
                    if(clips.Count>0){clips.Clear();_faultInjected=true;Debug.Log("[ReplayProbe] discarded complete local clip for missing-content fallback");}
                }
                else if(Argument("-tp-replay-fault")=="corrupt")
                {
                    var incoming=typeof(Net.MatchRpc).GetField("_clipReceive",flags).GetValue(rpc);
                    if(incoming!=null){var hash=(byte[])incoming.GetType().GetField("Hash").GetValue(incoming);hash[0]^=0x40;_faultInjected=true;Debug.Log("[ReplayProbe] corrupted assembly digest before completion");}
                }
            }
            var phase=HalftimePresentation.Instance;
            if(phase?.HasReplay==true&&!_shot){_shot=true;StartCoroutine(Shot());}
            if(match.RoundNumber==5&&_finished<0)_finished=Time.realtimeSinceStartup;
            if(_finished>0&&Time.realtimeSinceStartup-_finished>2){Application.Quit();return;}
            if(_trace==null||Time.realtimeSinceStartup<_next)return;_next=Time.realtimeSinceStartup+.05f;
            var archive=FindAnyObjectByType<MatchReplayArchive>();long clip=phase?.ClipId??0;
            if(clip==0&&archive?.Clips.Count>0)clip=archive.Clips[0].Clip.Id;
            if(Argument("-tp-replay-glacial")=="1")InspectArc(archive,clip,phase);
            _trace.WriteLine(FormattableString.Invariant($"{Time.realtimeSinceStartup-_started:F3},{NetAuthority.LocalSlot},{match.RoundNumber},{match.PresentationMatchId},{match.ScoreFor(1)},{archive?.Clips.Count??0},{clip},{Net.MatchRpc.Instance.ReplayReadyCount(clip)},{(HalftimePresentation.Playing?1:0)},{(phase?.HasReplay==true?1:0)},{phase?.Remaining??0:F3},{(PresentationClock.Held?1:0)},{Time.time:F4},{(phase?.FallbackReason!=null?1:0)},{(_faultInjected?1:0)},{Net.WorldEffectSnapshot.Capture().Count},{match.ScoreFor(0)},{match.ScoreFor(2)},{match.ScoreFor(3)}"));
        }
        private IEnumerator Shot()
        {
            yield return new WaitForSecondsRealtime(1.5f);yield return new WaitForEndOfFrame();
            var target=HalftimePresentation.Instance?.ReplayFrame;if(target==null)yield break;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);var previous=RenderTexture.active;RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();RenderTexture.active=previous;File.WriteAllBytes(Path.ChangeExtension(Argument("-tp-replaytrace"),".png"),image.EncodeToPNG());Destroy(image);
        }
        private IEnumerator Exchange()
        {
            var round=GameServices.Round;var match=GameServices.Match;
            foreach(var actor in round.Players)actor.Teleport(new Vector3(6,Slipper.GroundY(new Vector3(6,0,-5+actor.PlayerSlot*3))+.1f,-5+actor.PlayerSlot*3));
            if(SceneFlow.SelectedMode==GameMode.HeroStrike)
            {
                // Long fixture lifetimes distinguish round cleanup from ordinary expiry.
                Abilities.HeroHazards.SpawnIceSheet(new Vector3(3,0,2),1.2f,30,1,1,silent:true);
                Abilities.HeroHazards.SpawnHexSigil(new Vector3(-3,0,2),1.2f,30,2,1,silent:true);
                Visual.DanteFissurePillar.Create(new Vector3(4,0,4),Vector3.forward,1,30);
                if(Argument("-tp-replay-glacial")=="1")
                {
                    var wall=Abilities.HeroHazards.SpawnIceBarricade(new Vector3(3,0,-1),Vector3.forward,30,
                        silent:true,arcLength:5,arcRadius:3).GetComponent<Abilities.HeroHazards.IceBarricadeComponent>();
                    wall.HitsToShatter=3;wall.HostSlipperHit();
                    Debug.Log("[ReplayProbeArc] live slabs="+wall.GetComponentsInChildren<MeshCollider>().Length+
                        " radius="+wall.ArcRadius+" length="+wall.ArcLength+" hits="+wall.RemainingHits);
                }
                Net.MatchRpc.Instance.BroadcastWorldSnapshot();
                var send=typeof(Net.MatchRpc).GetMethod("SendWorldFieldSnapshot",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                foreach(var peer in Unity.Netcode.NetworkManager.Singleton.ConnectedClientsIds)
                    send.Invoke(Net.MatchRpc.Instance,new object[]{peer});
            }
            bool catchContact=Argument("-tp-replay-contact")=="catch";
            if(catchContact)PlaceCatchParticipants(round);
            yield return new WaitForSeconds(2.5f);
            if(catchContact)
            {
                if(!ResolveCatchContact(round))throw new InvalidOperationException("Replay catch contact missing");
            }
            else
            {
                var thrower=round.PlayerAt(1);var shoe=FindObjectsByType<Slipper>().First(s=>s.OwnerSlot==1);
                int serial=round.Lata.HostKnockdownSerial;shoe.HostThrow(thrower,round.Lata.transform.position+new Vector3(0,1.2f,-2),Vector3.forward*12);
                float until=Time.time+2;while(round.Lata.IsUpright&&Time.time<until)yield return null;
                if(round.Lata.HostKnockdownSerial!=serial+1)throw new InvalidOperationException("Physical replay contact missing");
            }
            yield return new WaitForSeconds(1.6f);
            var archive=FindAnyObjectByType<MatchReplayArchive>();
            if(archive==null||archive.Clips.Count==0)throw new InvalidOperationException("Replay not retained: "+archive?.LastSkip);
            var retained=archive.Clips[0];Debug.Log("[NetReplay] clip="+retained.Clip.Id+" bytes="+retained.Bytes.Length+" fields="+retained.Clip.FieldFrames.Max(f=>f.Fields.Length));
            int required=int.TryParse(Argument("-tp-replay-ready-peers"),out int peers)?Mathf.Clamp(peers,1,2):2;
            double untilReady=SharedUltimatePhase.Now+35;
            while(Net.MatchRpc.Instance.ReplayReadyCount(retained.Clip.Id)<required&&SharedUltimatePhase.Now<untilReady)yield return null;
            if(Net.MatchRpc.Instance.ReplayReadyCount(retained.Clip.Id)<required)throw new InvalidOperationException("Both participants did not verify the clip");
            while(match.RoundNumber<4){round.EndRound();match.AdvanceRound();Net.MatchRpc.Instance.BroadcastWorldSnapshot();yield return new WaitForSecondsRealtime(.35f);}
            round.EndRound();match.BeginIntermission();Net.MatchRpc.Instance.BroadcastWorldSnapshot();
        }

        // An explicit catch fixture complements the existing physical knockdown
        // fixture. Both use real gameplay contact and the normal replay archive.
        private static void PlaceCatchParticipants(RoundDirector round)
        {
            var defender=round.PlayerAt(0);var victim=round.PlayerAt(1);
            var a=new Vector3(0,0,-4);var b=new Vector3(0,0,-3);
            a.y=Slipper.GroundY(a)+.1f;b.y=Slipper.GroundY(b)+.1f;
            defender.Teleport(a);victim.Teleport(b);defender.transform.forward=Vector3.forward;
        }
        private static bool ResolveCatchContact(RoundDirector round)
        {
            var defender=round.PlayerAt(0);
            return defender.GetComponent<CombatVerbs>().HostResolvePunch(defender.transform.position,defender.transform.forward);
        }

        private void InspectArc(MatchReplayArchive archive,long clip,HalftimePresentation phase)
        {
            if(!_arcClipLogged&&clip>0)
            {
                var recorded=archive?.Clips.FirstOrDefault(c=>c.Clip.Id==clip)?.Clip??Net.MatchRpc.Instance.ReceivedReplay(clip);
                if(recorded!=null)
                {
                    var fields=recorded.FieldFrames.SelectMany(f=>f.Fields).Select(f=>f.State)
                        .Where(f=>f.Type==Net.WorldEffectSnapshot.Kind.Barricade&&f.Radius>0).ToArray();
                    Debug.Log("[ReplayProbeArc] clip="+clip+" arcSamples="+fields.Length+" radius="+
                        string.Join(",",fields.Select(f=>f.Radius).Distinct())+" length="+
                        string.Join(",",fields.Select(f=>f.FirstScale).Distinct())+" hits="+
                        string.Join(",",fields.Select(f=>f.SecondScale).Distinct()));_arcClipLogged=true;
                }
            }
            if(!_arcViewLogged&&phase?.HasReplay==true)
            {
                var roots=FindObjectsByType<Transform>().Where(t=>t.name=="RecordedField-Barricade").ToArray();
                if(roots.Length==0)return;
                Debug.Log("[ReplayProbeArc] view slabs="+roots.Sum(r=>r.GetComponentsInChildren<MeshFilter>().Length)+
                    " colliders="+roots.Sum(r=>r.GetComponentsInChildren<Collider>().Length));_arcViewLogged=true;
            }
        }
    }
}
