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
    // Explicit transport fixture: host creates the seven persistent field kinds;
    // actual remote players request live complete snapshots twice. No cast replay.
    [DefaultExecutionOrder(-250)]
    public sealed class NetWorldFieldProbe : MonoBehaviour
    {
        private static string _path;
        private StreamWriter _writer;
        private bool _prepared, _created, _requested, _repeated, _captured;
        private bool _cycleStarted, _cycleReturned;
        private bool _cycleMode;
        private float _next;
        private static string Argument(string key)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,key);
            return at>=0 && at+1<args.Length?args[at+1]:null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            _path=Environment.GetCommandLineArgs().Contains("-tp-tournament")?null:Argument("-tp-worldfieldtrace");
            if(_path!=null)UI.SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(_path==null)return;
            var root=new GameObject("~NetWorldFieldProbe");DontDestroyOnLoad(root);
            var probe=root.AddComponent<NetWorldFieldProbe>();
            probe._cycleMode=Environment.GetCommandLineArgs().Contains("-tp-field-rejoin");
            string path=Path.GetFullPath(_path);Directory.CreateDirectory(Path.GetDirectoryName(path));
            probe._writer=new StreamWriter(path){AutoFlush=true};
            probe._writer.WriteLine("wallTime,elapsed,local,requested,repeated,count,kind,x,y,z,fx,fy,fz,duration,remaining,radius,owner,first,second,split,cycle,process");
        }
        private void Update()
        {
            var round=GameServices.Round;
            if(!NetAuthority.IsNetworked || round==null || !round.RoundActive || GameServices.Match==null
                || GameServices.Match.RoundNumber<1)return;
            var ready=FindFirstObjectByType<ReadyGate>();if(ready!=null && ready.CountingDown)return;
            foreach(var brain in FindObjectsByType<AIController>(FindObjectsSortMode.None))brain.enabled=false;
            foreach(var input in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))input.enabled=false;
            foreach(var player in round.Players)if(player!=null){player.Intent.Clear();player.Intent.Parked=true;}
            if(!_prepared)
            {
                _prepared=true;
                foreach(var player in round.Players)
                    if(player!=null && (NetAuthority.IsHost || player.PlayerSlot==NetAuthority.LocalSlot))
                        player.Teleport(new Vector3(-10,.12f,-12+player.PlayerSlot*6));
            }
            float elapsed=UI.SceneFlow.SelectedRoundSeconds-round.TimeLeft;
            if(elapsed>19){_writer.Flush();Application.Quit();return;}
            if(NetAuthority.IsHost && !_created && elapsed>=10)
            {
                _created=true;
                HeroHazards.SpawnIceSheet(new Vector3(-6,0,-6),1.495f,5,1,1.35f);
                HeroHazards.SpawnIceBarricade(new Vector3(0,0,-6),Vector3.forward,6,1.4f,.6f,true);
                HeroHazards.SpawnFireTrail(new Vector3(6,0,-6),.8f,3,1,Vector3.right);
                HeroHazards.SpawnShockTrail(new Vector3(-6,0,0),.55f,3,2,1.45f,Vector3.back);
                HeroHazards.SpawnSupernovaCrater(Vector3.zero,2.1f,5,1);
                HeroHazards.SpawnHexSigil(new Vector3(6,0,0),1.44f,6,3,1.4f);
                DanteFissurePillar.Create(new Vector3(-6,0,6),Vector3.right,-1,5);
            }
            if(_cycleMode && NetAuthority.LocalSlot==1)
            {
                if(!_cycleStarted && elapsed>=11){_cycleStarted=true;RejoinSameProcess();return;}
            }
            else if(!NetAuthority.IsHost)
            {
                if(!_requested && elapsed>=10.4f){_requested=true;MatchRpc.Instance.RequestWorldSnapshot();}
                if(!_repeated && elapsed>=11.1f){_repeated=true;MatchRpc.Instance.RequestWorldSnapshot();}
            }
            if(Time.realtimeSinceStartup<_next)return;_next=Time.realtimeSinceStartup+.05f;
            var fields=WorldEffectSnapshot.Capture();
            if(!_captured && fields.Count==7 && elapsed>=11.65f){_captured=true;CaptureOverview();}
            if(fields.Count==0)Write(default,0,elapsed);
            else foreach(var field in fields)Write(field,fields.Count,elapsed);
        }
        private void Write(WorldEffectSnapshot.Field field,int count,float elapsed)
        {
            object[] row={DateTime.UtcNow.Ticks/(double)TimeSpan.TicksPerSecond,elapsed,NetAuthority.LocalSlot,
                _requested?1:0,_repeated?1:0,count,(int)field.Type,field.Position.x,field.Position.y,field.Position.z,
                field.Forward.x,field.Forward.y,field.Forward.z,field.Duration,field.Remaining,field.Radius,
                field.Owner,field.FirstScale,field.SecondScale,field.Split?1:0,
                _cycleReturned?2:_cycleStarted?1:0,System.Diagnostics.Process.GetCurrentProcess().Id};
            _writer.WriteLine(string.Join(",",row.Select(value=>Convert.ToString(value,CultureInfo.InvariantCulture))));
        }
        private void OnDestroy()=>_writer?.Dispose();
        private async void RejoinSameProcess()
        {
            try
            {
                int port=int.Parse(Argument("-tp-field-rejoin-port")??"8961",CultureInfo.InvariantCulture);
                var selectedMap=UI.SceneFlow.SelectedMap;
                Debug.Log("[WorldFieldProbe] starting same-process rejoin");
                bool started=await NetSession.Instance.StartClientAsync("127.0.0.1",port);
                if(!started){Debug.LogError("[WorldFieldProbe] rejoin transport refused to start");return;}
                _cycleReturned=true;
                UI.SceneFlow.Go(selectedMap);
            }
            catch(Exception exception){Debug.LogException(exception);}
        }
        private void CaptureOverview()
        {
            var root=new GameObject("WorldFieldSnapshotWitness");var camera=root.AddComponent<Camera>();
            camera.enabled=false;camera.fieldOfView=49;camera.allowHDR=true;
            camera.transform.position=new Vector3(13,19,-20);camera.transform.LookAt(Vector3.zero);
            root.AddComponent<ColourGrade>().AdoptFromScene();
            var target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
                File.WriteAllBytes(Path.ChangeExtension(Path.GetFullPath(_path),"png"),pixels.EncodeToPNG());
            }
            finally{RenderTexture.active=previous;camera.targetTexture=null;target.Release();Destroy(target);Destroy(pixels);Destroy(root);}
        }
    }
}
