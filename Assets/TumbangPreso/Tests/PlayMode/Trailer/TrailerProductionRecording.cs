using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Map;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests.Trailer
{
    /// <summary>Configurable original-cinema capture. Outputs/quality are explicit; no legacy edit/audio is invoked.</summary>
    public sealed class TrailerProductionRecording
    {
        private string _settings;
        private float _audioVolume;
        [UnitySetUp] public IEnumerator Before()
        { yield return PlayModeWorld.Reset(); _settings=JsonUtility.ToJson(Settings.SettingsStore.Current); _audioVolume=AudioListener.volume; AudioListener.volume=0; }
        [UnityTearDown] public IEnumerator After()
        {
            MapFilm.Stop(); Hold(false); Time.captureFramerate=0;
            if(_settings!=null) JsonUtility.FromJsonOverwrite(_settings,Settings.SettingsStore.Current);
            AudioListener.volume=_audioVolume;
            yield return PlayModeWorld.Reset();
        }

        [UnityTest,Timeout(3600000)] public IEnumerator OpeningProbe()=>Record(true,0,20);
        [UnityTest,Timeout(7200000)] public IEnumerator OpeningMotion()=>Record(false,0,20);
        [UnityTest,Timeout(7200000)] public IEnumerator Ending()=>Record(false,20,18);
        [UnityTest,Timeout(7200000)] public IEnumerator Neighborhood()=>Record(false,0,6,true);

        private IEnumerator Record(bool probe,float start,float length,bool neighborhood=false)
        {
            LogAssert.ignoreFailingMessages=true;
            int width=Number("-trailer-width",probe?960:1920),height=Number("-trailer-height",probe?540:1080),fps=Number("-trailer-fps",30);
            string tag=Argument("-trailer-tag")??(probe?"opening-probe":"opening");
            Assert.IsTrue(tag.All(c=>char.IsLetterOrDigit(c)||c=='-'||c=='_'),"Unsafe output tag");
            string folder=Path.GetFullPath(Path.Combine("Logs","trailer","bring-it-home",tag));
            Assert.IsFalse(Directory.Exists(folder) && Directory.EnumerateFiles(folder,"frame_*.jpg").Any(),"Use a fresh output tag; capture never overwrites a prior take.");
            Directory.CreateDirectory(folder);
            SceneFlow.Networked=false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            GameLaunch.SoloSeat=1; GameLaunch.GuidedTutorial=false; GameLaunch.Spectator=false; GameLaunch.AllBots=false;
            Settings.SettingsStore.Current.CinematicCameraMotion=true;
            Settings.SettingsStore.Current.ReducedUiMotion=false;
            yield return SceneManager.LoadSceneAsync(neighborhood?SceneFlow.Eskinita:SceneFlow.Arena);
            while(HubLoading.Preparing) yield return null;
            for(int i=0;i<8;i++) yield return null;
            foreach(var gate in Object.FindObjectsByType<ReadyGate>(FindObjectsSortMode.None)) gate.enabled=false;
            foreach(var old in Object.FindObjectsByType<MatchArrivalPresentation>(FindObjectsSortMode.None)) old.Cancel();
            foreach(var ai in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) ai.enabled=false;
            foreach(var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled=false;
            Hold(true); yield return null;
            var camera=Camera.main; Assert.IsNotNull(camera,"No capture camera");
            var rig=camera.GetComponent<CameraSystem.CameraRig>(); if(rig!=null) rig.SetActive(false);
            var lens=camera.gameObject.AddComponent<TrailerLens>();
            Assert.IsTrue(lens.Ready,"Trailer focus shader failed to load/compile");
            if(MapFilm.Instance!=null) Object.Destroy(MapFilm.Instance.gameObject);
            yield return null;
            var filmRoot=new GameObject("Bring It Home original cinema");
            MapFilm director;
            if(neighborhood)director=filmRoot.AddComponent<TrailerStreetPayoffFilm>();
            else director=filmRoot.AddComponent<TrailerOpeningFilm>();
            Assert.IsTrue(director.Begin(camera,GameServices.Round.Players.OrderBy(p=>p.PlayerSlot).ToArray(),
                camera.transform.position,camera.transform.rotation,camera.fieldOfView,false),"Opening refused");
            var target=new RenderTexture(width,height,24,camera.allowHDR?RenderTextureFormat.DefaultHDR:RenderTextureFormat.ARGB32,
                camera.allowHDR?RenderTextureReadWrite.Linear:RenderTextureReadWrite.Default); target.antiAliasing=4; target.Create();
            var resolved=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB); resolved.Create();
            var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            var shot=new GameObject("~TrailerProductionShot").AddComponent<LateFrameShot>();
            var prior=camera.targetTexture;
            shot.Shoot=path=>
            {
                using(var mask=new TrailerCaptureMask())
                {
                    mask.Apply(true); camera.targetTexture=target; camera.Render(); camera.targetTexture=prior;
                    Graphics.Blit(target,resolved); var active=RenderTexture.active; RenderTexture.active=resolved;
                    pixels.ReadPixels(new Rect(0,0,width,height),0,0); pixels.Apply(); RenderTexture.active=active;
                    File.WriteAllBytes(path,pixels.EncodeToJPG(97));
                }
            };
            var samples=probe?new[]{0f,1.2f,2.4f,3f,3.7f,3.95f,4.15f,4.43f,4.47f,4.8f,5.4f,7.5f,9.9f,11.5f,12.5f,14.5f,16.5f,18.5f,19.9f}:
                Enumerable.Range(0,Mathf.CeilToInt(length*fps)).Select(i=>start+(float)i/fps).ToArray();
            File.WriteAllText(Path.Combine(folder,"capture.json"),$"{{\"kind\":\"original-cinema\",\"width\":{width},\"height\":{height},\"fps\":{fps},\"start\":{start.ToString(CultureInfo.InvariantCulture)},\"frames\":{samples.Length},\"probe\":{probe.ToString().ToLowerInvariant()}}}");
            var shotList=neighborhood?TrailerStreetPayoffFilm.Shots:TrailerOpeningFilm.Shots;
            File.WriteAllLines(Path.Combine(folder,"shots.tsv"),shotList.Select(s=>$"{s.name}\t{s.from.ToString(CultureInfo.InvariantCulture)}\t{s.to.ToString(CultureInfo.InvariantCulture)}"));
            File.WriteAllLines(Path.Combine(folder,"frame-times.tsv"),samples.Select((time,i)=>$"{i}\t{time.ToString("F5",CultureInfo.InvariantCulture)}"));
            director.HeldAge=start; director.Roll(); Time.captureFramerate=fps;
            try
            {
                for(int i=0;i<samples.Length;i++)
                {
                    director.HeldAge=samples[i]; yield return null;
                    yield return shot.Take(Path.Combine(folder,$"frame_{i:00000}.jpg"));
                    if(i%90==0) Debug.Log($"[TrailerProduction] {tag}: {i}/{samples.Length} age={samples[i]:F3}");
                }
            }
            finally
            {
                camera.targetTexture=prior; Time.captureFramerate=0;
                Object.Destroy(shot.gameObject); Object.Destroy(pixels);
                target.Release(); Object.Destroy(target); resolved.Release(); Object.Destroy(resolved);
            }
            Assert.AreEqual(samples.Length,Directory.EnumerateFiles(folder,"frame_*.jpg").Count(),"Incomplete capture");
            Debug.Log($"[TrailerProduction] complete {tag}: {samples.Length} frames, {width}x{height}, {fps} fps to {folder}");
            director.End(); yield return null;
        }

        private static string Argument(string name)
        { var args=Environment.GetCommandLineArgs(); for(int i=0;i<args.Length-1;i++) if(args[i]==name) return args[i+1]; return null; }
        private static int Number(string name,int fallback)=>int.TryParse(Argument(name),out int value)?value:fallback;
        private static void Hold(bool on)=>typeof(PresentationClock).GetMethod(on?"Hold":"Release",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
    }
}
