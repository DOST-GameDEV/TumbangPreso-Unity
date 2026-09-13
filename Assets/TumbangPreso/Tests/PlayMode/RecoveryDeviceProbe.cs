using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    [Category("WallClock")]
    public sealed class RecoveryDeviceProbe
    {
        [UnitySetUp] public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After()=>PlayModeWorld.Reset();

        [UnityTest,Timeout(120000)]
        public IEnumerator QuickTouchDownAndUpSurvivesUntilTheRecoveryConsumer()
        {
            bool visible=TouchHud.ForceVisible;
            int oldFps=Application.targetFrameRate,oldVsync=QualitySettings.vSyncCount;
            TouchHud.ForceVisible=true;
            try
            {
                yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);
                GameServices.Round.BeginRound();
                var who=GameServices.Round.PlayerAt(1);who.Intent.Parked=false;
                var reader=who.GetComponent<PlayerInputReader>();Assert.IsNotNull(reader);reader.enabled=false;
                var hud=Object.FindFirstObjectByType<TouchHud>();Assert.IsNotNull(hud);
                var button=hud.Buttons.First(b=>b.Entry.Verb==Verb.Jump);
                var pointer=new PointerEventData(EventSystem.current){pointerId=731};
                QualitySettings.vSyncCount=0;
                foreach(int cadence in new[]{30,60,144})
                foreach(var element in new StunElement?[]{null,StunElement.Ice,StunElement.Fire,StunElement.Shock,StunElement.Stone,StunElement.Void,StunElement.Hex})
                {
                    Application.targetFrameRate=cadence;bool trip=!element.HasValue;
                    who.ClearTrip();who.ClearStun();who.Intent.Clear();who.Intent.CommitFrame();
                    TouchInput.ReleaseAll();TouchInput.Active=true;
                    if(trip)who.ApplyTrip();else who.ApplyStagger(4,element.Value,6);
                    // Two real touch callbacks and reader samples before physics,
                    // as happens when rendering outpaces the fixed-step consumer.
                    button.OnPointerDown(pointer);reader.SendMessage("Update");
                    button.OnPointerUp(pointer);reader.SendMessage("Update");
                    Assert.IsFalse(TouchInput.Pressed(Verb.Jump));
                    yield return new WaitForFixedUpdate();
                    Assert.AreEqual(1,trip?who.MashPresses:who.StunMashPresses,"Quick touch tap was lost before recovery");
                    yield return new WaitForSeconds(.15f);
                    Assert.AreEqual(1,trip?who.MashPresses:who.StunMashPresses,"Released touch became repeat recovery");
                    button.OnPointerDown(pointer);reader.SendMessage("Update");
                    yield return new WaitForSeconds(.32f);
                    Assert.AreEqual(2,trip?who.MashPresses:who.StunMashPresses,"Held touch did not produce exactly one additional press");
                    button.OnPointerUp(pointer);reader.SendMessage("Update");
                }
            }
            finally{TouchInput.ReleaseAll();TouchHud.ForceVisible=visible;Application.targetFrameRate=oldFps;QualitySettings.vSyncCount=oldVsync;}
        }

        [UnityTest,Timeout(120000)]
        public IEnumerator ConcurrentTagCannotBeShortenedByTripMashing()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);
            var who=GameServices.Round.PlayerAt(1);who.Intent.Parked=false;
            foreach(float tag in new[]{1.5f,4f})
            {
                who.ClearTrip();who.ClearStun();who.ApplyStagger(tag,StunElement.None,6);who.ApplyTrip();
                Assert.IsTrue(who.MashRecover());
                Assert.AreEqual(tag,who.StunLeft,.001f,"Getting up from a trip also changed the independent tag penalty");
            }
        }

        [UnityTest,Timeout(120000)]
        public IEnumerator DelayedRecoverySnapshotKeepsTripPredictionSeparateFromTagTime()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);
            var who=GameServices.Round.PlayerAt(1);who.IsBot=false;who.Intent.Parked=false;
            var previous=NetAuthority.Provider;
            try
            {
                NetAuthority.Provider=new OwnerProvider();int episode=who.RecoveryEpisode+1;
                Snapshot(0);Assert.IsTrue(who.RecoverFromInput());float predicted=who.TripLeft;
                Snapshot(0);Snapshot(0);
                Assert.AreEqual(predicted,who.TripLeft,.001f,"Repeated delayed snapshots spent a pending tap twice");
                Assert.AreEqual(4,who.StunLeft,.001f,"Trip prediction spent an overlapping tag timer");
                who.ApplyNetworkState(4,4,StunElement.None,6,0,predicted,2.5f,1,.22f,100,0,0,episode,1);
                Assert.AreEqual(predicted,who.TripLeft,.001f);
                Assert.AreEqual(4,who.StunLeft,.001f);
                void Snapshot(int ack)=>who.ApplyNetworkState(4,4,StunElement.None,6,0,2.5f,2.5f,0,0,100,0,0,episode,ack);
            }
            finally{NetAuthority.Provider=previous;}
        }

        private sealed class OwnerProvider:INetProvider
        {
            public bool IsHost=>false;public bool IsNetworked=>true;
            public int LocalSlot=>1;public int LocalPeerId=>1;public bool IsSeatlessReferee=>false;
        }

        [UnityTest,Timeout(180000)] public IEnumerator KeyboardRecoveryUsesTheConfiguredReaderAcrossCadences()=>HardwareMatrix(false);
        [UnityTest,Timeout(180000)] public IEnumerator GamepadRecoveryUsesTheConfiguredReaderAcrossCadences()=>HardwareMatrix(true);

        private IEnumerator HardwareMatrix(bool gamepad)
        {
            int fps=Application.targetFrameRate,vsync=QualitySettings.vSyncCount;
            var settings=InputSystem.settings;var background=settings.backgroundBehavior;var editorInput=settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputDevice device=gamepad?(InputDevice)InputSystem.AddDevice<Gamepad>():InputSystem.AddDevice<Keyboard>();
            InputActionAsset actions=null;PlayerInputReader reader=null;
            try
            {
                yield return MapRetrievalProbe.Load(SceneFlow.SaBubong);
                GameServices.Round.BeginRound();
                var who=GameServices.Round.PlayerAt(1);who.Intent.Parked=false;
                reader=who.GetComponent<PlayerInputReader>();Assert.IsNotNull(reader);
                actions=Object.Instantiate(Resources.Load<InputActionAsset>("TumbangPreso"));
                actions.devices=new[]{device};
                typeof(PlayerInputReader).GetField("_actions",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(reader,actions);
                reader.SendMessage("Awake");reader.enabled=true;InputSystem.EnableDevice(device);
                TouchInput.ReleaseAll();TouchInput.Active=false;
                QualitySettings.vSyncCount=0;
                int cases=0;
                foreach(int cadence in new[]{30,60,144})
                {
                    Application.targetFrameRate=cadence;
                    yield return new WaitForSeconds(.2f);
                    foreach(var element in new StunElement?[]{null,StunElement.Ice,StunElement.Fire,StunElement.Shock,StunElement.Stone,StunElement.Void,StunElement.Hex})
                    {
                        who.ClearTrip();who.ClearStun();who.Intent.Clear();who.Intent.CommitFrame();
                        if(element.HasValue)who.ApplyStagger(4,element.Value,6);else who.ApplyTrip();
                        Queue(true);yield return null;Queue(false);yield return null;yield return new WaitForFixedUpdate();
                        int presses=element.HasValue?who.StunMashPresses:who.MashPresses;
                        Assert.AreEqual(1,presses,$"{gamepad}/{cadence}/{element}: mapped one-frame tap did not arrive once");
                        yield return new WaitForSeconds(.15f);
                        Queue(true);yield return new WaitForSeconds(.32f);
                        Assert.AreEqual(2,element.HasValue?who.StunMashPresses:who.MashPresses,"Held hardware generated repeat presses or lost its edge");
                        Queue(false);yield return null;
                        Debug.Log($"[Recovery device] {(gamepad?"gamepad":"keyboard")} requestedFPS={cadence} observedFrameMS={Time.unscaledDeltaTime*1000:F2} context={element?.ToString()??"trip"} tap=1 hold=1");
                        cases++;
                    }
                }
                Assert.AreEqual(21,cases);
                void Queue(bool down)
                {
                    if(gamepad)InputSystem.QueueStateEvent((Gamepad)device,down?new GamepadState().WithButton(GamepadButton.South):new GamepadState());
                    else InputSystem.QueueStateEvent((Keyboard)device,down?new KeyboardState(Key.Space):new KeyboardState());
                }
            }
            finally
            {
                if(reader!=null)reader.enabled=false;
                if(actions!=null){actions.Disable();Object.Destroy(actions);}
                InputSystem.RemoveDevice(device);
                settings.backgroundBehavior=background;settings.editorInputBehaviorInPlayMode=editorInput;
                Application.targetFrameRate=fps;QualitySettings.vSyncCount=vsync;TouchInput.ReleaseAll();
            }
        }
    }
}
