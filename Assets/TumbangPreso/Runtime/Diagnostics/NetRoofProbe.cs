using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    /// <summary>Opt-in real-process edge/mash/stock evidence; never a normal match driver.</summary>
    [DefaultExecutionOrder(-250)]
    public sealed class NetRoofProbe : MonoBehaviour
    {
        public static bool Active;
        private StreamWriter _writer;
        private CharacterMotor _who;
        private Slipper _shoe;
        private double _began=-1,_next,_mashAt=-1;
        private bool _staged,_sawTrip;
        private bool _tagApplied;
        private string _shots;
        private StreamWriter _frameTimes;
        private double _nextShot;
        private int _frame;
        private RenderTexture _worldFrame,_displayFrame;
        private Texture2D _pixels;
        private Camera _uiCamera;
        private static string Argument(string key)
        {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            Active=Argument("-tp-rooftrace")!=null&&!Environment.GetCommandLineArgs().Contains("-tp-tournament");
            if(Active)SceneFlow.PinSelectedRules(CustomGameRules.Defaults(Argument("-tp-roofmode")=="hero"?GameMode.HeroStrike:GameMode.Classic));
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(!Active)return;
            var go=new GameObject("~NetRoofProbe");DontDestroyOnLoad(go);
            var probe=go.AddComponent<NetRoofProbe>();go.AddComponent<NetThrowLateSample>().Sample=probe.Record;
            var path=Path.GetFullPath(Argument("-tp-rooftrace"));Directory.CreateDirectory(Path.GetDirectoryName(path));
            probe._writer=new StreamWriter(path){AutoFlush=true};
            probe._writer.WriteLine("time,elapsed,host,local,round,x,y,z,trip,stun,mash,holding,shoeActive,shoeState,shoeX,shoeY,shoeZ,episode,ack,map");
            var shots=Argument("-tp-roofshots");
            if(!string.IsNullOrEmpty(shots))
            {
                probe._shots=Path.GetFullPath(shots);Directory.CreateDirectory(probe._shots);
                probe._frameTimes=new StreamWriter(Path.Combine(probe._shots,"frames.csv")){AutoFlush=true};
                probe._frameTimes.WriteLine("frame,real_seconds,game_seconds");
            }
        }
        private void Update()
        {
            if(!Active){_writer?.Dispose();_writer=null;enabled=false;return;}
            var round=GameServices.Round;
            if(!NetAuthority.IsNetworked||round==null||!round.RoundActive||GameServices.Match==null||GameServices.Match.RoundNumber<1)return;
            var ready=FindFirstObjectByType<ReadyGate>();if(ready!=null&&ready.CountingDown)return;
            _who=round.PlayerAt(1);if(_who==null)return;
            var slice=FindFirstObjectByType<SliceRunner>();
            if(slice?.Slippers==null)return;_shoe=slice.Slippers.FirstOrDefault(s=>s!=null&&s.SeatOfOrigin==1);
            foreach(var ai in FindObjectsByType<AIController>(FindObjectsSortMode.None))ai.enabled=false;
            foreach(var reader in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))reader.enabled=false;
            foreach(var other in round.Players)
                if(other!=null&&other!=_who){other.Intent.Clear();other.Intent.Parked=true;}
            double now=NetworkManager.Singleton.ServerTime.Time;
            if(_began<0)_began=now;float elapsed=(float)(now-_began);
            if(NetAuthority.IsHost&&!_staged&&elapsed>.8f)
            {
                _staged=true;_who.transform.rotation=Quaternion.Euler(0,90,0);
                _who.Teleport(new Vector3(RooftopRecovery.HalfX-1.2f,.1f,0));
                Debug.Log($"[RoofProbe] stage scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} bounds={AIController.PlayableHalfX}/{AIController.PlayableHalfZ} at={_who.transform.position}");
            }
            if(NetAuthority.LocalSlot==1)
            {
                _who.Intent.Parked=false;_who.Intent.ClearAim();_who.Intent.FaceAimPoint=false;
                FindFirstObjectByType<CameraRig>()?.SetAimSource(AimSource.Movement);
                if(_who.IsTripped){_sawTrip=true;if(_mashAt<0)_mashAt=now;}
                bool atFence=!_sawTrip&&_who.transform.position.x>RooftopRecovery.HalfX-1.4f;
                _who.Intent.Move=atFence?Vector2.right:Vector2.zero;
                _who.Intent.Set(Verb.Jump,(atFence&&_who.IsGrounded)||
                    (_who.CanMashUp&&_mashAt>=0&&now-_mashAt>2&&(now-_mashAt)% .16<.065));
                bool take=_sawTrip&&elapsed>16&&_shoe!=null&&_shoe.gameObject.activeSelf&&!_who.HoldingSlipper;
                if(take)
                {
                    var delta=_shoe.transform.position-_who.transform.position;
                    _who.Intent.Move=delta.magnitude>.45f?new Vector2(delta.x,delta.z).normalized:Vector2.zero;
                    _who.Intent.Set(Verb.Grab,delta.magnitude<Balance.PickupRadius*.9f);
                }
                else _who.Intent.Set(Verb.Grab,false);
            }
            if(NetAuthority.IsHost&&!_tagApplied&&_who.IsTripped&&Environment.GetCommandLineArgs().Contains("-tp-rooftag"))
            { _tagApplied=true;_who.ApplyStagger(4,StunElement.None,6); }
            if(elapsed>28){_writer?.Dispose();_writer=null;Application.Quit();}
        }
        private void Record()
        {
            if(!Active||_writer==null||_who==null||_began<0||NetworkManager.Singleton==null)return;
            double now=NetworkManager.Singleton.ServerTime.Time;
            double real=Time.realtimeSinceStartupAsDouble;
            if(_shots!=null&&now-_began>.6&&now-_began<9&&real>=_nextShot)
            {
                _nextShot=real+1.0/30;
                CapturePlayerFrame(Path.Combine(_shots,_frame.ToString("D5")+".jpg"));
                _frameTimes.WriteLine($"{_frame},{F(real)},{F(now-_began)}");_frame++;
            }
            if(now<_next)return;_next=now+.05;
            var p=_who.transform.position;var s=_shoe!=null?_shoe.transform.position:Vector3.zero;
            _writer.WriteLine(string.Join(",",new object[]{F(now),F(now-_began),NetAuthority.IsHost?1:0,NetAuthority.LocalSlot,
                GameServices.Match.RoundNumber,F(p.x),F(p.y),F(p.z),F(_who.TripLeft),F(_who.StunLeft),_who.MashPresses,
                _who.HoldingSlipper?1:0,_shoe!=null&&_shoe.gameObject.activeSelf?1:0,_shoe!=null?(int)_shoe.State:-1,
                F(s.x),F(s.y),F(s.z),_who.RecoveryEpisode,_who.RecoveryAcknowledged,
                Array.IndexOf(SceneFlow.Maps,UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)}));
        }
        private static string F(double v)=>v.ToString("F4",CultureInfo.InvariantCulture);
        private void CapturePlayerFrame(string path)
        {
            var camera=Camera.main;if(camera==null)throw new InvalidOperationException("No actual player camera for roof capture");
            if(_worldFrame==null)
            {
                _worldFrame=new RenderTexture(1280,720,24,RenderTextureFormat.DefaultHDR,RenderTextureReadWrite.Linear){antiAliasing=4};_worldFrame.Create();
                _displayFrame=new RenderTexture(1280,720,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);_displayFrame.Create();
                _pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
                _uiCamera=new GameObject("~Roof capture UI camera").AddComponent<Camera>();_uiCamera.transform.SetParent(transform,false);
                _uiCamera.enabled=false;_uiCamera.clearFlags=CameraClearFlags.Nothing;_uiCamera.cullingMask=1<<5;
                _uiCamera.orthographic=true;_uiCamera.nearClipPlane=.01f;_uiCamera.farClipPlane=10;_uiCamera.targetTexture=_displayFrame;
            }
            var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var saved=canvases.Select(c=>(c,c.worldCamera,c.planeDistance)).ToArray();
            var layers=new Dictionary<Transform,int>();var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
            try
            {
                foreach(var canvas in canvases)
                {
                    foreach(var t in canvas.GetComponentsInChildren<Transform>(true))
                    {if(!layers.ContainsKey(t))layers.Add(t,t.gameObject.layer);t.gameObject.layer=5;}
                    canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=_uiCamera;canvas.planeDistance=1;
                }
                camera.targetTexture=_worldFrame;Canvas.ForceUpdateCanvases();camera.Render();
                Graphics.Blit(_worldFrame,_displayFrame);
                Canvas.ForceUpdateCanvases();_uiCamera.Render();RenderTexture.active=_displayFrame;
                _pixels.ReadPixels(new Rect(0,0,1280,720),0,0);_pixels.Apply();File.WriteAllBytes(path,_pixels.EncodeToJPG(90));
            }
            finally
            {
                camera.targetTexture=oldTarget;RenderTexture.active=oldActive;
                foreach(var entry in saved)
                {if(entry.c==null)continue;entry.c.renderMode=RenderMode.ScreenSpaceOverlay;entry.c.worldCamera=entry.worldCamera;entry.c.planeDistance=entry.planeDistance;}
                foreach(var entry in layers)if(entry.Key!=null)entry.Key.gameObject.layer=entry.Value;
                Canvas.ForceUpdateCanvases();
            }
        }
        private void OnDestroy()
        {
            _writer?.Dispose();_writer=null;_frameTimes?.Dispose();_frameTimes=null;
            if(_worldFrame!=null){_worldFrame.Release();Destroy(_worldFrame);}
            if(_displayFrame!=null){_displayFrame.Release();Destroy(_displayFrame);}
            if(_pixels!=null)Destroy(_pixels);
            if(_uiCamera!=null)Destroy(_uiCamera.gameObject);
        }
    }
}
