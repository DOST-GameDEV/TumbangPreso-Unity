using System.Collections;
using System.Collections.Generic;
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
            Assert.AreEqual(7,_touch.Canvas.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("SpectatorTouch")));
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
            Button("Feed").onClick.Invoke();Assert.IsTrue(_hud.IsCleanFeed);yield return null;Assert.IsTrue(Canvas.enabled);
            Button("Feed").onClick.Invoke();Assert.IsFalse(_hud.IsCleanFeed);
            Button("Controls").onClick.Invoke();Assert.IsFalse(_hud.SpectatorControlsVisible);
            Button("Controls").onClick.Invoke();Assert.IsTrue(_hud.SpectatorControlsVisible);yield return null;
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
