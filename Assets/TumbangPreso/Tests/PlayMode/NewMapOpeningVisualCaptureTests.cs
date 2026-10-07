using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class NewMapOpeningVisualCaptureTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private CustomRules _rules;private bool _pinned,_bots;private string _settings;
        [UnitySetUp]public IEnumerator Before()
        {
            _rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;_bots=GameLaunch.AllBots;
            _settings=JsonUtility.ToJson(Settings.SettingsStore.Current);yield return PlayModeWorld.Reset();
        }
        [UnityTearDown]public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
            GameLaunch.AllBots=_bots;JsonUtility.FromJsonOverwrite(_settings,Settings.SettingsStore.Current);
        }
        [UnityTest]public IEnumerator BridgeHeldOpeningAt1080()=>Capture(SceneFlow.IlalimNgTulay);
        [UnityTest]public IEnumerator CoveHeldOpeningAt1080()=>Capture(SceneFlow.LagoonCove);
        [UnityTest]public IEnumerator KantoHeldOpeningAt1080()=>Capture(SceneFlow.Kanto);
        private static object Read(MatchArrivalPresentation arrival,string field)=>typeof(MatchArrivalPresentation).GetField(field,Hidden).GetValue(arrival);
        private IEnumerator Capture(string map)
        {
            SceneFlow.Networked=false;var rules=CustomGameRules.Defaults(GameMode.HeroStrike);rules.Bots=CustomGameRules.MaxBots;rules.ManualReady=false;
            SceneFlow.PinSelectedRules(rules);GameLaunch.AllBots=true;Settings.SettingsStore.Current.CinematicCameraMotion=true;Settings.SettingsStore.Current.ReducedUiMotion=false;
            string output=System.Environment.GetEnvironmentVariable("TUMP_OPENING_CAPTURE");
            if(string.IsNullOrEmpty(output))Assert.Ignore("Dedicated visual capture requires TUMP_OPENING_CAPTURE.");
            string folder=Path.Combine(output,map);Directory.CreateDirectory(folder);
            // The hidden batch GameView defaults to640x480. Set the actual
            // camera lens before the arrival coroutine measures its portraits.
            var lens=new GameObject("Opening capture widescreen lens").AddComponent<OpeningCaptureWideLens>();
            Object.DontDestroyOnLoad(lens.gameObject);
            yield return SceneManager.LoadSceneAsync(map);
            var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);target.Create();
            var image=new Texture2D(1920,1080,TextureFormat.RGBA32,false);var lines=new System.Text.StringBuilder();
            float next=0,until=Time.realtimeSinceStartup+60;int captures=0;bool sawHeld=false;Camera camera=null;
            var observer=new GameObject("Held opening camera capture").AddComponent<DirectedArrivalFrameObserver>();
            try
            {
                while(GameServices.Round?.RoundActive!=true&&Time.realtimeSinceStartup<until)
                {
                    // Batchmode does not invoke WaitForEndOfFrame. Observe after
                    // the presentation and ordinary camera have finished LateUpdate.
                    bool observed=false;System.Exception captureError=null;
                    observer.Observe=()=>
                    {
                        try
                        {
                            var arrival=Object.FindAnyObjectByType<MatchArrivalPresentation>();
                            if(arrival==null||!MatchArrivalPresentation.Active||!PresentationClock.Held)return;
                            sawHeld=true;if(Time.realtimeSinceStartup<next)return;next=Time.realtimeSinceStartup+.6f;
                            camera=(Camera)Read(arrival,"_camera");if(camera==null)return;
                            Assert.AreEqual(16f/9f,camera.aspect,.0001f,"Capture must use the widescreen lens, not stretch a4:3 view.");
                            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
                            try
                            {
                                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                                image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                                File.WriteAllBytes(Path.Combine(folder,$"opening-{captures:D3}.png"),image.EncodeToPNG());
                            }
                            finally{camera.targetTexture=oldTarget;RenderTexture.active=oldActive;}
                            var title=(UnityEngine.UI.Text)Read(arrival,"_title");var detail=(UnityEngine.UI.Text)Read(arrival,"_detail");
                            lines.AppendLine($"frame={captures} time={Time.realtimeSinceStartup:F3} eye={camera.transform.position} forward={camera.transform.forward} fov={camera.fieldOfView:F2} aspect={camera.aspect:F4} screen={Screen.width}x{Screen.height} held={PresentationClock.Held} title={title?.text} detail={detail?.text}");
                            captures++;
                        }
                        catch(System.Exception error){captureError=error;}
                        finally{observed=true;}
                    };
                    while(!observed)yield return null;
                    if(captureError!=null)throw captureError;
                }
                Assert.IsTrue(sawHeld,"The actual automatic held introduction never appeared.");
                Assert.GreaterOrEqual(captures,10,"Capture the actual held direction and portraits rather than the finished countdown.");
                Assert.IsTrue(GameServices.Round.RoundActive);Assert.IsFalse(PresentationClock.Held);
            }
            finally{File.WriteAllText(Path.Combine(folder,"capture.txt"),lines.ToString());observer.Observe=null;Object.Destroy(observer.gameObject);Object.Destroy(lens.gameObject);if(camera!=null&&camera.targetTexture==target)camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(image);}
        }
    }

    [DefaultExecutionOrder(-32000)]
    public sealed class OpeningCaptureWideLens:MonoBehaviour
    {
        private void Update(){foreach(var camera in Camera.allCameras)camera.aspect=16f/9f;}
    }
}
