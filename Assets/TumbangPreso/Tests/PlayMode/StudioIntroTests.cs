using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Video;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class StudioIntroTests
    {
        private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        private GameObject _owner;
        private SplashScreen _splash;
        private T Get<T>(string field)=>(T)typeof(SplashScreen).GetField(field,Flags).GetValue(_splash);
        private object Call(string method,params object[] args)=>typeof(SplashScreen).GetMethod(method,Flags).Invoke(_splash,args);
        private void Create(bool clip=true)
        {
            _owner=new GameObject("StudioIntroFixture");_splash=_owner.AddComponent<SplashScreen>();_splash.enabled=false;
#if UNITY_EDITOR
            if(clip)
            {
                typeof(SplashScreen).GetField("_clip",Flags).SetValue(_splash,
                    UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/TumbangPreso/Art/video/opening_animation.mp4"));
                typeof(SplashScreen).GetField("_portableClip",Flags).SetValue(_splash,
                    UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/TumbangPreso/Art/video/opening_animation_portable.webm"));
            }
#endif
            if(clip)Assert.IsNotNull(typeof(SplashScreen).GetProperty("StudioClip",Flags).GetValue(_splash));
        }
        [UnitySetUp] public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After()
        { if(_owner!=null)Object.Destroy(_owner);yield return PlayModeWorld.Reset(); }

        [UnityTest,Timeout(20000)]
        public IEnumerator AuthoredVideoPlaysBeforeLoadingAndEndsOnWhite()
        {
            Create();var run=(IEnumerator)Call("Run");Assert.IsTrue(run.MoveNext());
            Assert.IsNull(Get<GameObject>("_canvas"),"Loading UI was built before the studio intro.");
            Assert.AreEqual(0,Get<float>("_targetProgress"));Assert.IsNull(Get<AsyncOperation>("_menu"));
            var intro=run.Current as IEnumerator;Assert.IsNotNull(intro);bool captured=false;
            while(intro.MoveNext())
            {
                Assert.IsFalse(Get<bool>("_assetsPreloaded"));Assert.IsNull(Get<AsyncOperation>("_menu"));
                var player=Get<VideoPlayer>("_studioPlayer");
                if(!captured&&Get<bool>("_studioFrameReady")&&player.time>1.8)
                {
                    captured=true;var canvas=Get<GameObject>("_studioCanvas").GetComponent<Canvas>();
                    Assert.IsFalse(canvas.GetComponentsInChildren<Text>(true).Any(t=>t.text.ToLowerInvariant().Contains("skip")));
                    player.Pause();yield return TumpUiCapture.Capture("Studio-intro-logo",canvas,1280,720,false,true);player.Play();
                }
                yield return intro.Current;
            }
            Assert.IsTrue(captured,"The authored video never produced a visible native frame.");
            Assert.IsTrue(Get<bool>("_studioEnded"));Assert.IsFalse(Get<bool>("_studioFailed"));
            Assert.AreEqual(Color.white,Get<Image>("_studioFade").color);
            yield return TumpUiCapture.Capture("Studio-intro-white",Get<GameObject>("_studioCanvas").GetComponent<Canvas>(),1280,720,false,true);
            Assert.IsNull(Get<AsyncOperation>("_menu"),"Presentation must not activate the menu itself.");
            Call("BuildSurface");Call("SetFadeColour",Color.white);Call("SetFade",1f);Call("ReleaseStudioIntro");
            Assert.IsNull(Get<GameObject>("_studioCanvas"));Assert.IsNotNull(Get<GameObject>("_canvas"));
            Assert.AreEqual(Color.white,Get<Image>("_fade").color);
        }

        [UnityTest,Timeout(30000)]
        public IEnumerator KeyboardMousePadAndTouchSkipOnlyTheIntroThroughWhite()
        {
            var inputSettings=InputSystem.settings;
            var background=inputSettings.backgroundBehavior;var editorInput=inputSettings.editorInputBehaviorInPlayMode;
            inputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keys=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
            var pad=InputSystem.AddDevice<Gamepad>();var touch=InputSystem.AddDevice<Touchscreen>();
            InputSystem.EnableDevice(keys);InputSystem.EnableDevice(mouse);InputSystem.EnableDevice(pad);InputSystem.EnableDevice(touch);
            try
            {
                for(int device=0;device<4;device++)
                {
                    Create();var intro=(IEnumerator)Call("PlayStudioIntro");Assert.IsTrue(intro.MoveNext());yield return null;
                    InputSystem.QueueStateEvent(keys,new KeyboardState());InputSystem.QueueStateEvent(mouse,new MouseState());
                    InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();
                    if(device==0)InputSystem.QueueStateEvent(keys,new KeyboardState(Key.Escape));
                    if(device==1)InputSystem.QueueStateEvent(mouse,new MouseState{buttons=2});
                    if(device==2)InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.East));
                    if(device==3)InputSystem.QueueStateEvent(touch,new TouchState{touchId=42,phase=UnityEngine.InputSystem.TouchPhase.Began,position=new Vector2(100,100)});
                    InputSystem.Update();Assert.IsTrue(MenuNav.StudioSkipPressed,"No skip edge for device "+device);
                    float began=Time.realtimeSinceStartup;bool intermediate=false;
                    while(intro.MoveNext())
                    {
                        float alpha=Get<Image>("_studioFade").color.a;
                        intermediate|=alpha>0&&alpha<1;
                        yield return intro.Current;
                    }
                    Assert.IsTrue(Get<bool>("_studioSkipped"));Assert.IsTrue(intermediate,"Skip jumped instead of fading.");
                    Assert.AreEqual(Color.white,Get<Image>("_studioFade").color);
                    Assert.Less(Time.realtimeSinceStartup-began,1f);Assert.IsFalse(Get<bool>("_assetsPreloaded"));
                    Assert.IsNull(Get<AsyncOperation>("_menu"));
                    Object.Destroy(_owner);yield return null;
                    Assert.IsFalse(ScreenTakeover.AnyOpen);
                }
            }
            finally
            {
                InputSystem.RemoveDevice(keys);InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(pad);InputSystem.RemoveDevice(touch);
                inputSettings.backgroundBehavior=background;inputSettings.editorInputBehaviorInPlayMode=editorInput;
            }
        }

        [UnityTest,Timeout(15000)]
        public IEnumerator MissingFailedAndDestroyedIntroReleaseWithoutClaimingReadiness()
        {
            Create(false);var missing=(IEnumerator)Call("PlayStudioIntro");Assert.IsFalse(missing.MoveNext());
            Assert.IsNull(Get<GameObject>("_studioCanvas"));Object.Destroy(_owner);yield return null;
            Create();var intro=(IEnumerator)Call("PlayStudioIntro");Assert.IsTrue(intro.MoveNext());
            Call("StudioVideoFailed",Get<VideoPlayer>("_studioPlayer"),"test decoder failure");
            while(intro.MoveNext())yield return intro.Current;
            Assert.IsTrue(Get<bool>("_studioFailed"));Assert.AreEqual(Color.white,Get<Image>("_studioFade").color);
            Assert.IsFalse(Get<bool>("_assetsPreloaded"));var canvas=Get<GameObject>("_studioCanvas");var target=Get<RenderTexture>("_studioTarget");
            Object.Destroy(_owner);yield return null;Assert.IsTrue(canvas==null);Assert.IsTrue(target==null);
            Assert.IsFalse(ScreenTakeover.AnyOpen);
            Create();intro=(IEnumerator)Call("PlayStudioIntro");Assert.IsTrue(intro.MoveNext());
            canvas=Get<GameObject>("_studioCanvas");target=Get<RenderTexture>("_studioTarget");
            Object.Destroy(_owner);yield return null;Assert.IsTrue(canvas==null);Assert.IsTrue(target==null);
            Assert.IsFalse(ScreenTakeover.AnyOpen);
        }
    }
}
