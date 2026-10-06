using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorTouchConsumerTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private GameObject _root;private SpectatorCamera _watcher;private CursorLockMode _cursor;private bool _visible;
        private bool _active;private Vector2 _move,_look;
        [UnitySetUp]public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();_active=TouchInput.Active;_move=TouchInput.Move;_look=TouchInput.LookDelta;
            _cursor=Cursor.lockState;_visible=Cursor.visible;Cursor.lockState=CursorLockMode.None;
            _root=new GameObject("Touch watcher fixture");_watcher=_root.AddComponent<SpectatorCamera>();_watcher.enabled=false;
            _watcher.AdoptPose(Vector3.zero,0,0);_watcher.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            TouchInput.Active=true;TouchInput.Move=Vector2.zero;TouchInput.LookDelta=Vector2.zero;yield return null;
        }
        [UnityTearDown]public IEnumerator After()
        {if(_root!=null)Object.Destroy(_root);yield return PlayModeWorld.Reset();TouchInput.Active=_active;TouchInput.Move=_move;TouchInput.LookDelta=_look;Cursor.lockState=_cursor;Cursor.visible=_visible;}
        private bool Takeover()=>(bool)typeof(SpectatorCamera).GetMethod("ManualTakeover",Hidden).Invoke(_watcher,null);
        private void Look()=>typeof(SpectatorCamera).GetMethod("StepLook",Hidden).Invoke(_watcher,null);
        [Test]public void ThumbMovementTakesOverTheAutomaticCamera()
        {TouchInput.Move=Vector2.up;Assert.IsTrue(Takeover());}
        [Test]public void TouchDragTakesOverTheAutomaticCamera()
        {TouchInput.LookDelta=Vector2.right*10;Assert.IsTrue(Takeover());}
        [Test]public void TouchDragRotatesOnceAndDoesNotRepeatWithoutAnotherDrag()
        {
            TouchInput.LookDelta=Vector2.right*10;Look();float yaw=(float)typeof(SpectatorCamera).GetField("_yawDeg",Hidden).GetValue(_watcher);
            Assert.Greater(yaw,.01f);Assert.AreEqual(Vector2.zero,TouchInput.LookDelta);Look();Assert.AreEqual(yaw,(float)typeof(SpectatorCamera).GetField("_yawDeg",Hidden).GetValue(_watcher));
        }
        [Test]public void ThumbMovementMovesTheFreeFlightTarget()
        {
            TouchInput.Move=Vector2.up;typeof(SpectatorCamera).GetMethod("Update",Hidden).Invoke(_watcher,null);
            var target=(Vector3)typeof(SpectatorCamera).GetField("_targetPosition",Hidden).GetValue(_watcher);Assert.Greater(target.z,.0001f);
        }
        [Test]public void InactiveTouchDataDoesNotTakeOverOrRotateTheView()
        {TouchInput.Active=false;TouchInput.Move=Vector2.up;TouchInput.LookDelta=Vector2.right*10;Assert.IsFalse(Takeover());Look();Assert.AreEqual(0,(float)typeof(SpectatorCamera).GetField("_yawDeg",Hidden).GetValue(_watcher));}
    }
}
