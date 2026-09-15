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
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
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
                Assert.AreEqual(1,who.MashPresses,"Fresh gameplay input after release must still recover");
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
        public IEnumerator CustomJumpBindingStillRecoversAfterMenuRelease()
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
                Assert.AreEqual(1,who.MashPresses,"A fresh configured key must recover once");
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
