using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.EventSystems;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorTouchUiTests
    {
        private GameObject _root;private Hud _hud;private TouchHud _touch;private SpectatorCamera _watcher;private CharacterMotor _actor;
        private Touchscreen _screen;private bool _force,_watch,_bots;private CursorLockMode _cursor;private bool _cursorVisible;
        [UnitySetUp]public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();_force=TouchHud.ForceVisible;TouchHud.ForceVisible=false;
            _watch=GameLaunch.Spectator;_bots=GameLaunch.AllBots;GameLaunch.Spectator=true;GameLaunch.AllBots=false;
            _cursor=Cursor.lockState;_cursorVisible=Cursor.visible;_screen=InputSystem.AddDevice<Touchscreen>();GameServices.Ensure();
            _root=new GameObject("Touch spectator UI fixture");var body=new GameObject("Follow subject",typeof(CharacterController));body.transform.SetParent(_root.transform);
            _actor=body.AddComponent<CharacterMotor>();_actor.enabled=false;_actor.PlayerSlot=1;SpectatorCamera.Register(_actor);
            var camera=new GameObject("Watcher");camera.transform.SetParent(_root.transform);_watcher=camera.AddComponent<SpectatorCamera>();
            var view=new GameObject("Match HUD");view.transform.SetParent(_root.transform);_hud=view.AddComponent<Hud>();_hud.EnterSpectatorMode();
            _touch=TouchHud.Install();Assert.IsNotNull(_touch);yield return null;yield return null;
        }
        [UnityTearDown]public IEnumerator After()
        {
            SpectatorCamera.Unregister(_actor);if(_touch!=null)Object.Destroy(_touch.gameObject);if(_root!=null)Object.Destroy(_root);
            if(_screen!=null&&_screen.added)InputSystem.RemoveDevice(_screen);yield return PlayModeWorld.Reset();
            TouchHud.ForceVisible=_force;GameLaunch.Spectator=_watch;GameLaunch.AllBots=_bots;Cursor.lockState=_cursor;Cursor.visible=_cursorVisible;
        }
        private Button Button(string name)=>_touch.Canvas.GetComponentsInChildren<Button>(true).Single(b=>b.name=="SpectatorTouch"+name);
        private Canvas Canvas=>_touch.Canvas;
        [UnityTest]public IEnumerator WatcherHasViewControlsAndNoGameplayButtons()
        {
            Assert.IsTrue(_touch.ShouldBeOnScreen);Assert.IsTrue(Canvas.enabled);
            Assert.AreEqual(5,_touch.Canvas.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("SpectatorTouch")));
            Assert.IsFalse(_touch.Canvas.GetComponentsInChildren<TouchButton>().Any());Assert.IsFalse(Button("Pov").interactable);
            Assert.IsFalse(_touch.Canvas.GetComponentsInChildren<Button>().Any(b=>b.name=="SandboxToggle"));
            yield return TumpUiCapture.Capture("spectator-touch-landscape",Canvas,960,540,false,underlays:new[]{_hud.GetComponent<TumpMatchReadout>().Canvas});
            yield return TumpUiCapture.Capture("spectator-touch-portrait",Canvas,720,1280,false,underlays:new[]{_hud.GetComponent<TumpMatchReadout>().Canvas});
        }
        [UnityTest]public IEnumerator FollowPovFreeAndAutoUseTheExistingCamera()
        {
            Button("Follow").onClick.Invoke();yield return null;Assert.IsTrue(_watcher.HasFollowTarget);Assert.IsTrue(Button("Pov").interactable);
            Button("Pov").onClick.Invoke();StringAssert.Contains("POV",_watcher.StatusText());
            Button("Free").onClick.Invoke();Assert.IsFalse(_watcher.HasFollowTarget);StringAssert.Contains("FREE FLIGHT",_watcher.StatusText());
            Button("Auto").onClick.Invoke();Assert.IsTrue(_watcher.AutopilotEngaged);yield return null;
            Button("Auto").onClick.Invoke();Assert.IsFalse(_watcher.AutopilotEngaged);
        }
        [UnityTest]public IEnumerator CleanFeedAndControlHintsCanBeRestoredFromTouch()
        {
            Button("Tools").onClick.Invoke();yield return null;Button("MatchTab").onClick.Invoke();
            Button("Feed").onClick.Invoke();Assert.IsTrue(_hud.IsCleanFeed);yield return null;Assert.IsTrue(Canvas.enabled);
            Button("Feed").onClick.Invoke();Assert.IsFalse(_hud.IsCleanFeed);
            var help=Canvas.GetComponentsInChildren<Text>(true).Single(t=>t.name=="TouchHelp");
            Button("Controls").onClick.Invoke();Assert.IsFalse(help.enabled);
            Button("Controls").onClick.Invoke();Assert.IsTrue(help.enabled);yield return null;
            yield return TumpUiCapture.Capture("spectator-touch-match-tools",Canvas,1280,720,false,underlays:new[]{_hud.GetComponent<TumpMatchReadout>().Canvas});
        }
        [UnityTest]public IEnumerator PauseKeepsResumeAndCameraToolsReachable()
        {
            Button("Pause").onClick.Invoke();yield return null;Assert.AreEqual(0,PresentationClock.RequestedScale);
            Assert.IsTrue(Canvas.enabled);Assert.IsTrue(Button("Pause").isActiveAndEnabled);
            StringAssert.Contains("RESUME",Button("Pause").GetComponentInChildren<Text>().text);
            Button("Tools").onClick.Invoke();yield return null;Assert.IsTrue(Button("Save").isActiveAndEnabled);
            Button("Save").onClick.Invoke();yield return null;Assert.IsTrue(Button("Return").interactable);
            yield return TumpUiCapture.Capture("spectator-touch-camera-tools-paused",Canvas,1600,680,false,underlays:new[]{_hud.GetComponent<TumpMatchReadout>().Canvas});
            Button("Pause").onClick.Invoke();yield return null;Assert.Greater(PresentationClock.RequestedScale,0);
        }
        [UnityTest]public IEnumerator BackgroundCancelsTouchesAndRequiresAFreshLookPress()
        {
            var look=Canvas.GetComponentInChildren<TouchLookArea>();
            var data=new PointerEventData(EventSystem.current){pointerId=18,button=PointerEventData.InputButton.Left,delta=Vector2.right*30};
            look.OnPointerDown(data);look.OnDrag(data);Assert.Greater(TouchInput.LookDelta.x,0);
            TouchInput.Move=Vector2.one;TouchInput.Set(Verb.SpecialAbility,true);
            _touch.SendMessage("OnApplicationPause",true);yield return null;
            Assert.AreEqual(Vector2.zero,TouchInput.Move);Assert.IsFalse(TouchInput.Pressed(Verb.SpecialAbility));Assert.IsFalse(Canvas.enabled);
            _touch.SendMessage("OnApplicationPause",false);yield return null;Assert.IsTrue(Canvas.enabled);
            look.OnDrag(data);Assert.AreEqual(Vector2.zero,TouchInput.LookDelta);
            look.OnPointerDown(data);look.OnDrag(data);Assert.Greater(TouchInput.LookDelta.x,0);
        }
        [UnityTest]public IEnumerator ReplayHasItsOwnVisibleRaycastableReturnToLive()
        {
            const BindingFlags hidden=BindingFlags.NonPublic|BindingFlags.Instance;
            typeof(SpectatorCamera).GetMethod("CheckReplayMatch",hidden).Invoke(_watcher,null);
            var ring=(IList)typeof(SpectatorCamera).GetField("_replayFrames",hidden).GetValue(_watcher);
            var type=typeof(SpectatorCamera).GetNestedType("ReplayFrame",BindingFlags.NonPublic);
            for(int i=0;i<60;i++)
            {
                var frame=System.Activator.CreateInstance(type,true);
                var image=new Texture2D(2,2,TextureFormat.RGB565,false);image.Apply();
                type.GetField("Image").SetValue(frame,image);type.GetField("Aspect").SetValue(frame,16f/9);
                type.GetField("CapturedAt").SetValue(frame,Time.unscaledTime-6f+i*.1f);type.GetField("Slot").SetValue(frame,-1);
                ring.Add(frame);
            }
            Assert.IsTrue(_watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.Replay));Assert.IsTrue(_watcher.Replaying);
            var replay=_watcher.GetComponentsInChildren<Canvas>(true).Single(c=>c.name=="InstantReplayOverlay");
            var exit=replay.GetComponentsInChildren<Button>().Single(b=>b.name=="ReplayReturnToLive");
            Assert.IsTrue(exit.IsInteractable());float before=PresentationClock.RequestedScale;
            RaycastResult HitReturn()
            {
                var rect=(RectTransform)exit.transform;var point=RectTransformUtility.WorldToScreenPoint(replay.worldCamera,rect.TransformPoint(rect.rect.center));
                var pointer=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.Greater(hits.Count,0);
                Assert.AreSame(exit,hits[0].gameObject.GetComponentInParent<Button>());
                return hits[0];
            }
            yield return TumpUiCapture.Capture("spectator-touch-replay-controls",replay,1280,720,false,inspectViewport:()=>{HitReturn();});
            UnityEngine.Canvas.ForceUpdateCanvases();var hit=HitReturn();
            ExecuteEvents.ExecuteHierarchy(hit.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
            Assert.IsFalse(_watcher.Replaying);Assert.IsFalse(replay.enabled);Assert.AreEqual(before,PresentationClock.RequestedScale);
        }
        [UnityTest]public IEnumerator ResizingTheTouchSurfaceRetiresOldFingerCoordinates()
        {
            var look=Canvas.GetComponentInChildren<TouchLookArea>();
            var pointer=new PointerEventData(EventSystem.current){pointerId=11,delta=Vector2.right*30};
            look.OnPointerDown(pointer);look.OnDrag(pointer);_touch.Stick.SetValue(Vector2.one);TouchInput.Set(Verb.Grab,true);
            bool wide=Mathf.RoundToInt(Canvas.pixelRect.width)!=1600;
            yield return TumpUiCapture.Capture("spectator-touch-resized",Canvas,wide?1600:720,wide?680:1280,false,inspectViewport:()=>
            {
                Assert.AreEqual(Vector2.zero,TouchInput.Move);Assert.AreEqual(Vector2.zero,TouchInput.LookDelta);
                Assert.IsFalse(TouchInput.Pressed(Verb.Grab));look.OnDrag(pointer);Assert.AreEqual(Vector2.zero,TouchInput.LookDelta);
            });
        }
        [UnityTest]public IEnumerator MenuRetiresTouchAxesAndRestoresTheStripAfterResume()
        {
            TouchInput.Move=Vector2.up;TouchInput.LookDelta=Vector2.right*5;Button("Menu").onClick.Invoke();yield return null;
            Assert.IsTrue(Panel.AnyOpen);Assert.IsFalse(Canvas.enabled);Assert.AreEqual(Vector2.zero,TouchInput.Move);
            var panel=_touch.GetComponentInChildren<PausePanel>(true);Assert.IsNotNull(panel);
            Object.FindObjectsByType<Button>().Where(b=>b.gameObject.activeInHierarchy).Single(b=>b.name=="ResumeMatch").onClick.Invoke();yield return null;
            Assert.IsFalse(Panel.AnyOpen);Assert.IsTrue(Canvas.enabled);
        }
        [UnityTest]public IEnumerator NativeRaycastCanActivateTheVisibleCommandOnBothAspects()
        {
            foreach(var size in new[]{new Vector2Int(960,540),new Vector2Int(720,1280),new Vector2Int(1600,680)})
            {
                bool before=_watcher.AutopilotEngaged;
                yield return TumpUiCapture.Capture("spectator-touch-hit-"+size.x+"x"+size.y,Canvas,size.x,size.y,false,
                    underlays:new[]{_hud.GetComponent<TumpMatchReadout>().Canvas},inspectViewport:()=>
                    {
                        var target=Button("Auto");var rect=(RectTransform)target.transform;
                        var point=RectTransformUtility.WorldToScreenPoint(Canvas.worldCamera,rect.TransformPoint(rect.rect.center));
                        var pointer=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
                        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
                        Assert.IsTrue(hits.Count>0);Assert.AreSame(target,hits[0].gameObject.GetComponentInParent<Button>());
                        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
                        Assert.AreEqual(!before,_watcher.AutopilotEngaged);
                    });
            }
        }
        [UnityTest]public IEnumerator ReSeatingReturnsGameplayControlsAndRetiresCameraCommands()
        {
            _hud.ExitSpectatorMode();GameLaunch.Spectator=false;_watcher.enabled=false;_actor.Intent.Parked=false;_touch.Bind(_actor);yield return null;
            Assert.IsTrue(_touch.ShouldBeOnScreen);Assert.IsTrue(_touch.Canvas.GetComponentsInChildren<TouchButton>().Any());
            Assert.IsFalse(_touch.Canvas.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("SpectatorTouch")));
            Assert.IsFalse(_watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.FreeFlight));
        }
    }
}
