using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TumbangPreso.Diagnostics
{
    /// <summary>Explicit player-only acceptance for the real full-resolution host-map background.</summary>
    public sealed class MapPreviewPlayerProbe : MonoBehaviour
    {
        [Serializable] sealed class Sample
        {
            public string map;
            public int visit,frames,width,height,decoderCount;
            public float selectMs,firstFrameMs,p95Ms,worstMs;
            public long allocatedBytes;
        }
        [Serializable] sealed class Receipt
        {
            public bool passed;
            public bool lifetimeOnly,lifetimePassed;
            public string error,cpu,gpu;
            public int width,height;
            public List<Sample> samples=new List<Sample>();
        }
        string _folder;
        bool _lifetimeOnly;
        readonly Receipt _receipt=new Receipt();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tp-map-preview-probe");
            if(Application.isEditor||at<0||at+1>=args.Length)return;
            Application.runInBackground=true;
            var root=new GameObject("~MapPreviewPlayerProbe");DontDestroyOnLoad(root);
            var probe=root.AddComponent<MapPreviewPlayerProbe>();probe._folder=Path.GetFullPath(args[at+1]);
            probe._lifetimeOnly=Array.IndexOf(args,"-tp-map-preview-lifetime-only")>=0;
            Directory.CreateDirectory(probe._folder);probe.StartCoroutine(probe.Observe(probe.Run()));
        }
        IEnumerator Observe(IEnumerator work)
        {
            var stack=new Stack<IEnumerator>();stack.Push(work);
            while(stack.Count>0)
            {
                bool moved;object current=null;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}
                catch(Exception error)
                {
                    while(stack.Count>0)try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception cleanup){Debug.LogException(cleanup);}
                    Finish(false,error.ToString());yield break;
                }
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator nested)stack.Push(nested);else yield return current;
            }
            Finish(true,null);
        }
        static void Require(bool condition,string error){if(!condition)throw new InvalidOperationException(error);}
        static IEnumerator WaitFor(Func<bool> ready,string label,float seconds=45)
        {
            float until=Time.realtimeSinceStartup+seconds;
            while(!ready()&&Time.realtimeSinceStartup<until)yield return null;
            Require(ready(),label+" did not become ready");
        }
        IEnumerator Run()
        {
            Settings.SettingsStore.Current.Fullscreen=false;Settings.SettingsStore.Current.GraphicsQuality=2;
            Settings.GraphicsProfiles.Apply(2);Settings.SettingsStore.Current.ReducedUiMotion=false;
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            yield return WaitFor(()=>Screen.width==1920&&Screen.height==1080,"1080p window");
            yield return WaitFor(()=>FindButton("GuestAccount")!=null||TumpHub.Current!=null,"usable login");
            if(TumpHub.Current==null)FindButton("GuestAccount").onClick.Invoke();
            yield return WaitFor(()=>TumpHub.Current!=null&&TumpHub.Current.Top is HubHome,"automatic Home");
            var hub=TumpHub.Current;hub.Push<HubHost>();yield return null;yield return null;
            Require(hub.Top is HubHost,"Host-game map screen is missing");
            if(_lifetimeOnly)
            {
                _receipt.lifetimeOnly=true;
                yield return Lifetime(hub.Host.Preview);
                _receipt.lifetimePassed=true;hub.Home();yield return null;yield break;
            }
            for(int visit=0;visit<2;visit++)foreach(string map in SceneFlow.Maps)yield return Measure(hub,map,visit);
            var view=hub.Host.Preview;var media=view.GetComponent<MapPreviewVideo>();
            view.SetRenderingEnabled(false);yield return new WaitForSecondsRealtime(.3f);view.SetRenderingEnabled(true);
            yield return WaitFor(()=>media.HasFirstFrame&&view.GetComponent<VideoPlayer>().isPlaying,"hidden preview resume");
            Settings.SettingsStore.Current.ReducedUiMotion=true;view.SetRenderingEnabled(true);yield return null;
            Require(view.GetComponent<RawImage>().texture is Texture2D,"Reduced motion did not show the matching poster");
            Settings.SettingsStore.Current.ReducedUiMotion=false;view.SetRenderingEnabled(true);yield return null;
            Require(view.GetComponent<VideoPlayer>().isPlaying,"Normal motion did not resume playback");
            hub.Home();yield return null;
        }
        IEnumerator Lifetime(MapPreviewSurface actualView)
        {
            // Pause the actual map background while one owned probe view tests
            // the packaged decoder. This mode makes no all-map timing claim.
            actualView.SetRenderingEnabled(false);
            var root=new GameObject("~RecordedPreviewLifetime",typeof(RectTransform),typeof(Canvas));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.enabled=false;
            var imageRoot=new GameObject("RecordedCourt",typeof(RectTransform),typeof(RawImage));
            imageRoot.transform.SetParent(root.transform,false);HubKit.Stretch((RectTransform)imageRoot.transform);
            var media=imageRoot.AddComponent<MapPreviewVideo>();var image=imageRoot.GetComponent<RawImage>();
            try
            {
                Require(media.Show(SceneFlow.Arena),"Hidden Arena poster missing");
                Require(image.texture==MapPreviewVideo.PosterFor(SceneFlow.Arena),"Hidden Canvas lost its poster");
                Require(imageRoot.GetComponent<VideoPlayer>()==null,"Hidden Canvas opened a decoder");
                canvas.enabled=true;yield return WaitFor(()=>media.HasFirstFrame,"Canvas first frame");
                var player=imageRoot.GetComponent<VideoPlayer>();
                Require(player.clip.width==1920&&player.clip.height==1080,"Lifetime clip lost capture resolution");
                canvas.enabled=false;yield return null;yield return null;
                Require(!player.isPlaying,"Canvas overlay kept decoding");
                canvas.enabled=true;yield return null;yield return null;
                Require(player.isPlaying&&player==imageRoot.GetComponent<VideoPlayer>(),"Canvas resume replaced or failed its decoder");
                Settings.SettingsStore.Current.ReducedUiMotion=true;yield return null;yield return null;
                Require(!player.isPlaying&&image.texture==MapPreviewVideo.PosterFor(SceneFlow.Arena),"Direct reduced-motion setting did not pause/show poster");
                Settings.SettingsStore.Current.ReducedUiMotion=false;yield return null;yield return null;
                Require(player.isPlaying,"Direct motion re-enable did not resume");
                media.Stop();yield return null;
                Require(media.Show(SceneFlow.Eskinita),"Preparation control poster missing");
                player=imageRoot.GetComponent<VideoPlayer>();Require(player!=null&&!player.isPrepared,"Preparation control was not staged before readiness");
                Settings.SettingsStore.Current.ReducedUiMotion=true;yield return null;yield return null;
                yield return WaitFor(()=>player.isPrepared,"Reduced-motion native preparation");yield return null;yield return null;
                Require(!player.isPlaying&&image.texture==MapPreviewVideo.PosterFor(SceneFlow.Eskinita),"Late preparation callback ignored reduced motion");
                Settings.SettingsStore.Current.ReducedUiMotion=false;yield return null;yield return null;
                yield return WaitFor(()=>media.HasFirstFrame,"Prepared movie resume");
                Require(player.isPlaying&&image.texture is RenderTexture,"Prepared movie did not resume");
            }
            finally
            {
                Settings.SettingsStore.Current.ReducedUiMotion=false;media.Stop();Destroy(root);actualView.SetRenderingEnabled(true);
            }
        }
        IEnumerator Measure(TumpHub hub,string map,int visit)
        {
            var view=hub.Host.Preview;int scenes=SceneManager.sceneCount;
            var sample=new Sample{map=map,visit=visit};var watch=System.Diagnostics.Stopwatch.StartNew();
            hub.Host.SelectMap(map);sample.selectMs=(float)watch.Elapsed.TotalMilliseconds;
            Require(view.GetComponent<RawImage>().texture!=null,map+" showed no immediate poster");
            var media=view.GetComponent<MapPreviewVideo>();Require(media!=null&&media.Map==map,map+" is not using its recording");
            yield return WaitFor(()=>media.HasFirstFrame,map+" native first frame");sample.firstFrameMs=(float)watch.Elapsed.TotalMilliseconds;
            var player=view.GetComponent<VideoPlayer>();sample.width=(int)player.clip.width;sample.height=(int)player.clip.height;
            Require(sample.width==1920&&sample.height==1080,map+" lost capture resolution");
            Require(SceneManager.sceneCount==scenes,map+" loaded another arena for its movie");
            var frames=new List<float>();long previous=System.Diagnostics.Stopwatch.GetTimestamp();long allocated=GC.GetAllocatedBytesForCurrentThread();
            float until=Time.realtimeSinceStartup+3;
            while(Time.realtimeSinceStartup<until)
            {
                yield return null;long now=System.Diagnostics.Stopwatch.GetTimestamp();frames.Add((float)((now-previous)*1000.0/System.Diagnostics.Stopwatch.Frequency));previous=now;
            }
            sample.allocatedBytes=GC.GetAllocatedBytesForCurrentThread()-allocated;sample.frames=frames.Count;frames.Sort();
            Require(frames.Count>0,map+" produced no steady frames");sample.p95Ms=frames[(int)((frames.Count-1)*.95f)];sample.worstMs=frames[frames.Count-1];
            sample.decoderCount=view.GetComponents<VideoPlayer>().Count(p=>p.clip!=null);Require(sample.decoderCount==1,map+" opened duplicate decoders");
            _receipt.samples.Add(sample);Debug.Log("[MapPreviewPlayer] "+map+" visit="+visit+" firstMs="+sample.firstFrameMs+" p95="+sample.p95Ms);
        }
        static Button FindButton(string name)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name==name&&b.gameObject.activeInHierarchy);
        void Finish(bool passed,string error)
        {
            _receipt.passed=passed;_receipt.error=error;_receipt.cpu=SystemInfo.processorType;_receipt.gpu=SystemInfo.graphicsDeviceName;_receipt.width=Screen.width;_receipt.height=Screen.height;
            File.WriteAllText(Path.Combine(_folder,"result.json"),JsonUtility.ToJson(_receipt,true));Debug.Log("[MapPreviewPlayer] passed="+passed+" "+error);Application.Quit(passed?0:1);
        }
    }
}
