using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class RecoveryMenuBoundaryProbe
    {
        private int _qualityBefore,_mipBefore;
        [UnitySetUp] public IEnumerator Before()
        {
            _qualityBefore=QualitySettings.GetQualityLevel();_mipBefore=QualitySettings.globalTextureMipmapLimit;
            QualitySettings.SetQualityLevel(0,true);QualitySettings.globalTextureMipmapLimit=2;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();QualitySettings.SetQualityLevel(_qualityBefore,true);
            QualitySettings.globalTextureMipmapLimit=_mipBefore;
        }
        [UnityTest,Timeout(60000)]
        public IEnumerator MenuResumeSubmitDoesNotAlsoMashRecovery()
        {
            var inputSettings=InputSystem.settings;
            var background=inputSettings.backgroundBehavior;var editor=inputSettings.editorInputBehaviorInPlayMode;
            inputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var pad=InputSystem.AddDevice<Gamepad>();InputActionAsset actions=null;PlayerInputReader reader=null;
            try
            {
                yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);GameServices.Round.BeginRound();
                var who=GameServices.Round.PlayerAt(1);who.Intent.Parked=false;
                reader=who.GetComponent<PlayerInputReader>();
                actions=Object.Instantiate(Resources.Load<InputActionAsset>("TumbangPreso"));actions.devices=new InputDevice[]{pad};
                typeof(PlayerInputReader).GetField("_actions",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(reader,actions);
                reader.SendMessage("Awake");reader.enabled=true;InputSystem.EnableDevice(pad);
                InputLayer.TouchInput.ReleaseAll();InputLayer.TouchInput.Active=false;
                who.ClearTrip();who.ClearStun();who.ApplyTrip();
                var watcher=Object.FindAnyObjectByType<PauseWatcher>();
                var pause=Panel.Open<PausePanel>(watcher);pause.Local=who;yield return null;
                Assert.True(who.Intent.Parked);
                var resume=Object.FindObjectsByType<Button>().First(b=>b.name=="ResumeMatch"&&b.isActiveAndEnabled);
                EventSystem.current.SetSelectedGameObject(resume.gameObject);yield return null;
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));
                yield return null;yield return null;yield return new WaitForFixedUpdate();
                Assert.False(pause.gameObject.activeInHierarchy,"The real controller Submit did not resume the menu");
                Debug.Log("[RecoveryMenuBoundary] after Submit: presses="+who.MashPresses+" parked="+who.Intent.Parked);
                Assert.AreEqual(0,who.MashPresses,"One controller press resumed the menu and also shortened recovery");
                yield return new WaitForSeconds(.25f);
                Assert.AreEqual(0,who.MashPresses,"Holding the menu Submit after resume must stay consumed");
                InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;yield return new WaitForFixedUpdate();
                yield return new WaitForSeconds(.15f);
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));
                yield return null;yield return new WaitForFixedUpdate();
                Assert.AreEqual(0,who.MashPresses,"Fresh gameplay input must not revive retired recovery");
            }
            finally
            {
                if(reader!=null)reader.enabled=false;
                if(actions!=null){actions.Disable();Object.Destroy(actions);}
                InputSystem.RemoveDevice(pad);inputSettings.backgroundBehavior=background;inputSettings.editorInputBehaviorInPlayMode=editor;
                InputLayer.TouchInput.ReleaseAll();
            }
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator ResumeDoesNotReplayAPendingTouchLookDrag()
        {
            bool previousTouch=InputLayer.TouchInput.Active;
            GameObject gestureRoot=null;PausePanel pause=null;
            try
            {
                yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);GameServices.Round.BeginRound();
                var who=GameServices.Round.PlayerAt(1);who.Intent.Parked=false;
                var reader=who.GetComponent<PlayerInputReader>();
                InputLayer.TouchInput.ReleaseAll();InputLayer.TouchInput.Active=true;
                reader.SendMessage("Update");
                gestureRoot=new GameObject("Menu look gesture");
                var area=gestureRoot.AddComponent<InputLayer.TouchLookArea>();
                var drag=new PointerEventData(EventSystem.current){pointerId=0,delta=new Vector2(80,40)};
                area.OnDrag(drag);reader.SendMessage("Update");
                Assert.Greater(who.Intent.LookAxis.sqrMagnitude,1,"Control: the real touch look producer must reach gameplay.");
                var watcher=Object.FindAnyObjectByType<PauseWatcher>();
                pause=Panel.Open<PausePanel>(watcher);pause.Local=who;yield return null;
                Assert.IsTrue(who.Intent.Parked);
                // UI drag callbacks arrive after the early gameplay input Update.
                // A second finger can resume while the first look pointer remains held.
                InputLayer.TouchInput.Set(Verb.Sprint,true);
                area.OnDrag(drag);
                var resume=Object.FindObjectsByType<Button>().First(b=>b.name=="ResumeMatch"&&b.isActiveAndEnabled);
                resume.onClick.Invoke();Assert.IsFalse(pause.gameObject.activeInHierarchy);
                Assert.IsFalse(who.Intent.Parked);
                yield return null;reader.SendMessage("Update");
                Assert.AreEqual(Vector2.zero,who.Intent.LookAxis,"A menu-time drag was replayed into the camera after Resume.");
                Assert.IsTrue(InputLayer.TouchInput.Pressed(Verb.Sprint),"Handback must preserve unrelated held touch state.");
                area.OnDrag(drag);reader.SendMessage("Update");
                Assert.Greater(who.Intent.LookAxis.sqrMagnitude,1,"Fresh gameplay look must still work.");
            }
            finally
            {
                if(pause!=null&&pause.gameObject.activeInHierarchy)pause.Close();
                if(gestureRoot!=null)Object.DestroyImmediate(gestureRoot);
                InputLayer.TouchInput.ReleaseAll();InputLayer.TouchInput.Active=previousTouch;
            }
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator CustomJumpBindingWorksNormallyWithoutMashAfterMenuRelease()
        {
            var settings=InputSystem.settings;var background=settings.backgroundBehavior;var editor=settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>();InputActionAsset actions=null;PlayerInputReader reader=null;
            try
            {
                yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);GameServices.Round.BeginRound();
                var who=GameServices.Round.PlayerAt(1);who.Intent.Parked=false;
                reader=who.GetComponent<PlayerInputReader>();actions=Object.Instantiate(Resources.Load<InputActionAsset>("TumbangPreso"));
                actions.devices=new InputDevice[]{keyboard};
                typeof(PlayerInputReader).GetField("_actions",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(reader,actions);
                reader.SendMessage("Awake");reader.enabled=true;
                Assert.True(Settings.Rebinding.ResolveBindingIndexFor(actions,"Jump",InputLayer.InputDeviceKind.KeyboardMouse,out var jump,out int binding));
                jump.ApplyBindingOverride(binding,"<Keyboard>/j");
                InputLayer.TouchInput.ReleaseAll();InputLayer.TouchInput.Active=false;
                who.ClearTrip();who.ClearStun();who.ApplyTrip();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));yield return null;yield return new WaitForFixedUpdate();
                Assert.AreEqual(0,who.MashPresses,"The removed default binding must not recover");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                who.Intent.Parked=true;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.J));yield return null;
                reader.DiscardMenuButtonsUntilRelease();who.Intent.Parked=false;
                yield return new WaitForSeconds(.2f);
                Assert.AreEqual(0,who.MashPresses,"Configured key held across menu close must stay consumed");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return new WaitForFixedUpdate();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.J));yield return null;yield return new WaitForFixedUpdate();
                Assert.AreEqual(0,who.MashPresses,"A custom key must not shorten a timed trip");
                who.ClearTrip();who.Intent.Clear();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                for(int i=0;i<5;i++)yield return new WaitForFixedUpdate();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.J));yield return null;yield return new WaitForFixedUpdate();
                Assert.Greater(who.Velocity.y,0,"The custom binding must still perform ordinary Jump after recovery");
            }
            finally
            {
                if(reader!=null)reader.enabled=false;if(actions!=null){actions.Disable();Object.Destroy(actions);}
                InputSystem.RemoveDevice(keyboard);settings.backgroundBehavior=background;settings.editorInputBehaviorInPlayMode=editor;
                InputLayer.TouchInput.ReleaseAll();
            }
        }
    }
}
