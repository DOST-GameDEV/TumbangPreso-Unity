using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
namespace TumbangPreso.PlayTests
{
    public sealed class CreditsCodeInputTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private GameObject _owner;
        private OwnerCreditsView _view;
        private Keyboard _keys;
        private Gamepad _pad;
        private Mouse _mouse;
        private Touchscreen _touch;
        private InputSettings.BackgroundBehavior _background;
        private InputSettings.EditorInputBehaviorInPlayMode _editor;
        private static readonly Key[] Keys={Key.UpArrow,Key.UpArrow,Key.DownArrow,Key.DownArrow,Key.LeftArrow,Key.RightArrow,Key.LeftArrow,Key.RightArrow};
        private static readonly GamepadButton[] Buttons={GamepadButton.DpadUp,GamepadButton.DpadUp,GamepadButton.DpadDown,GamepadButton.DpadDown,GamepadButton.DpadLeft,GamepadButton.DpadRight,GamepadButton.DpadLeft,GamepadButton.DpadRight};
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            Assert.IsFalse(WalletStore.CanTransact,"This fixture never writes a real account.");
            _background=InputSystem.settings.backgroundBehavior;_editor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _keys=InputSystem.AddDevice<Keyboard>();_pad=InputSystem.AddDevice<Gamepad>();_mouse=InputSystem.AddDevice<Mouse>();_touch=InputSystem.AddDevice<Touchscreen>();
            _owner=new GameObject("Credits code input check");_view=_owner.AddComponent<OwnerCreditsView>();_view.Open(_owner.transform,()=>{});
        }
        [UnityTearDown] public IEnumerator After()
        {
            var canvas=(Canvas)typeof(OwnerCreditsView).GetField("_canvas",Private).GetValue(_view);
            if(canvas!=null)Object.DestroyImmediate(canvas.gameObject);
            Object.DestroyImmediate(_owner);
            InputSystem.RemoveDevice(_keys);InputSystem.RemoveDevice(_pad);InputSystem.RemoveDevice(_mouse);InputSystem.RemoveDevice(_touch);
            InputSystem.settings.backgroundBehavior=_background;InputSystem.settings.editorInputBehaviorInPlayMode=_editor;
            yield return PlayModeWorld.Reset();
        }
        private string Request => (string)typeof(OwnerCreditsView).GetField("_rewardRequest",Private).GetValue(_view);
        private void Tick(){InputSystem.Update();_view.SendMessage("Update");}
        private void KeyPress(Key key){InputSystem.QueueStateEvent(_keys,new KeyboardState(key));Tick();InputSystem.QueueStateEvent(_keys,new KeyboardState());Tick();}
        [Test] public void KeyboardExactSequenceRequestsRewardOnlyAtFinalDirection()
        { for(int i=0;i<Keys.Length;i++){KeyPress(Keys[i]);if(i<7)Assert.IsNull(Request);}Assert.AreEqual(32,Request.Length);Assert.IsFalse(WalletStore.CanTransact); }
        [Test] public void DpadExactSequenceWorksAndRetryKeepsReceipt()
        { for(int pass=0;pass<2;pass++){string before=Request;foreach(var key in Buttons){InputSystem.QueueStateEvent(_pad,new GamepadState().WithButton(key));Tick();InputSystem.QueueStateEvent(_pad,new GamepadState());Tick();}Assert.NotNull(Request);if(pass>0)Assert.AreEqual(before,Request);} }
        [Test] public void ClosingAndReopeningCannotCompleteAnOldPartialSequence()
        { KeyPress(Keys[0]);KeyPress(Keys[1]);_view.SendMessage("Close");_view.Open(_owner.transform,()=>{});for(int i=2;i<8;i++)KeyPress(Keys[i]);Assert.IsNull(Request);foreach(var key in Keys)KeyPress(key);Assert.NotNull(Request); }
        [Test] public void ClosedCreditsIgnoreDirections()
        { _view.SendMessage("Close");foreach(var key in Keys)KeyPress(key);Assert.IsNull(Request); }
        [Test] public void MouseDirectionalDragsRecognizeTheSameCode()
        { foreach(var direction in new[]{Vector2.up,Vector2.up,Vector2.down,Vector2.down,Vector2.left,Vector2.right,Vector2.left,Vector2.right}){InputSystem.QueueStateEvent(_mouse,new MouseState{position=new Vector2(400,400),buttons=1});Tick();InputSystem.QueueStateEvent(_mouse,new MouseState{position=new Vector2(400,400)+direction*100});Tick();}Assert.NotNull(Request); }
        [Test] public void TouchDirectionalDragsRecognizeTheSameCode()
        { int id=0;foreach(var direction in new[]{Vector2.up,Vector2.up,Vector2.down,Vector2.down,Vector2.left,Vector2.right,Vector2.left,Vector2.right}){id++;InputSystem.QueueStateEvent(_touch,new TouchState{touchId=id,phase=UnityEngine.InputSystem.TouchPhase.Began,position=new Vector2(400,400)});Tick();InputSystem.QueueStateEvent(_touch,new TouchState{touchId=id,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=new Vector2(400,400)+direction*100});Tick();}Assert.NotNull(Request); }
        [Test] public void TinyPointerDragsCannotBecomeASecretSequence()
        { foreach(var direction in new[]{Vector2.up,Vector2.up,Vector2.down,Vector2.down,Vector2.left,Vector2.right,Vector2.left,Vector2.right}){InputSystem.QueueStateEvent(_mouse,new MouseState{position=new Vector2(400,400),buttons=1});Tick();InputSystem.QueueStateEvent(_mouse,new MouseState{position=new Vector2(400,400)+direction*10});Tick();}Assert.IsNull(Request); }
        [Test] public void AmbiguousTwoDirectionPressResetsPartialEntry()
        { KeyPress(Key.UpArrow);KeyPress(Key.UpArrow);InputSystem.QueueStateEvent(_keys,new KeyboardState(Key.LeftArrow,Key.RightArrow));Tick();InputSystem.QueueStateEvent(_keys,new KeyboardState());Tick();for(int i=2;i<8;i++)KeyPress(Keys[i]);Assert.IsNull(Request); }
    }
}
