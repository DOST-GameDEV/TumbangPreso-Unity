using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorPrivateViewTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root, _proxy;
        private CharacterMotor _owner, _other;
        private Slipper _shoe;
        private CameraRig _rig;
        private SpectatorCamera _watcher;
        private Abilities.HeroAbilitySystem _abilities;
        private INetProvider _provider;
        private bool _spectator, _bots;
        private int _soloSeat;
        private object _render;
        private Type _renderType;
        private sealed class Solo : INetProvider
        { public bool IsHost => true; public bool IsNetworked => false; public int LocalSlot => 1;
          public int LocalPeerId => 0; public bool IsSeatlessReferee => false; }
        private sealed class DisconnectedView : INetProvider
        { public bool IsHost => false; public bool IsNetworked => true; public int LocalSlot => -1;
          public int LocalPeerId => 2; public bool IsSeatlessReferee => false; }
        [UnitySetUp] public IEnumerator Before()
        {
            _provider=NetAuthority.Provider;_spectator=GameLaunch.Spectator;_bots=GameLaunch.AllBots;_soloSeat=GameLaunch.SoloSeat;
            yield return PlayModeWorld.Reset();NetAuthority.Provider=new Solo();GameLaunch.Spectator=GameLaunch.AllBots=false;
            GameLaunch.SoloSeat=1;
            GameServices.Ensure();GameServices.Round.Clear();GameServices.Match.ApplySnapshot(new int[4],1,true);
            GameServices.Round.ApplySnapshot(100,true,0,true);
            _root=new GameObject("Private view fixture");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(_root.transform);
            floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(30,1,30);
            _owner=Actor(1,Vector3.zero);_other=Actor(0,new Vector3(3,0,4));
            _abilities=_owner.gameObject.AddComponent<Abilities.HeroAbilitySystem>();_abilities.enabled=false;
            var source=new GameObject("Source camera");source.transform.SetParent(_root.transform);source.tag="MainCamera";
            _rig=source.AddComponent<CameraRig>();_rig.Follow(_owner);_rig.enabled=false;
            _rig.Camera.transform.SetPositionAndRotation(new Vector3(0,1.45f,0),Quaternion.Euler(20,0,0));
            var shoe=GameObject.CreatePrimitive(PrimitiveType.Cube);shoe.name="Source slipper";shoe.transform.SetParent(_root.transform);
            shoe.transform.position=new Vector3(1,.2f,5);shoe.transform.localScale=Vector3.one*.3f;
            _shoe=shoe.AddComponent<Slipper>();_shoe.enabled=false;_shoe.SeatOfOrigin=0;_shoe.OwnerSlot=0;_shoe.SkinIndex=0;
            _proxy=new GameObject("Observed render parent");_proxy.transform.SetParent(_root.transform);
            _renderType=typeof(SpectatorCamera).Assembly.GetType("TumbangPreso.CameraSystem.ObservedPrivateView");Assert.IsNotNull(_renderType);
            Physics.SyncTransforms();yield return null;
        }
        private CharacterMotor Actor(int slot,Vector3 at)
        {
            var body=new GameObject("Actor "+slot,typeof(CharacterController));body.transform.SetParent(_root.transform);body.transform.position=at;
            var actor=body.AddComponent<CharacterMotor>();actor.enabled=false;actor.PlayerSlot=slot;actor.IsPerson=true;actor.Mode=Core.GameMode.HeroStrike;
            var mesh=GameObject.CreatePrimitive(PrimitiveType.Cube);mesh.name="Body mesh";mesh.transform.SetParent(body.transform,false);
            mesh.transform.localPosition=Vector3.up;Object.DestroyImmediate(mesh.GetComponent<Collider>());
            GameServices.Round.Register(actor);SpectatorCamera.Register(actor);return actor;
        }
        [UnityTearDown] public IEnumerator After()
        {
            if(_render!=null)_renderType.GetMethod("Dispose").Invoke(_render,null);
            SpectatorCamera.Unregister(_owner);SpectatorCamera.Unregister(_other);
            if(_root!=null)Object.Destroy(_root);yield return PlayModeWorld.Reset();
            NetAuthority.Provider=_provider;GameLaunch.Spectator=_spectator;GameLaunch.AllBots=_bots;GameLaunch.SoloSeat=_soloSeat;
        }
        private SpectatorPrivateView Capture()
        {
            _owner.ApplyHexed();_owner.ApplyDisoriented();
            var hex=Object.FindAnyObjectByType<HexedPhantomSlippers>();Assert.IsNotNull(hex);
            Assert.IsTrue((bool)typeof(HexedPhantomSlippers).GetMethod("VictimView",Hidden).Invoke(hex,null));
            typeof(HexedPhantomSlippers).GetMethod("Spawn",Hidden).Invoke(hex,null);
            var confused=Object.FindAnyObjectByType<DisorientedHallucinations>();Assert.IsNotNull(confused);
            typeof(DisorientedHallucinations).GetMethod("Update",Hidden).Invoke(confused,null);
            var marker=(GroundReticle)typeof(Abilities.HeroAbilitySystem).GetField("_reticle",Hidden).GetValue(_abilities);Assert.IsNotNull(marker);
            marker.SetStyle(GroundReticle.Style.Maw);marker.SetBeacon(true);marker.Show(new Vector3(2,0,4),3,new Color32(71,19,209,255));marker.Tick(.01f);
            object[] args={default(SpectatorViewState)};
            Assert.IsTrue((bool)typeof(CameraRig).GetMethod("TryObservedView",Hidden).Invoke(_rig,args));
            var view=((SpectatorViewState)args[0]).Private;Assert.IsTrue(view.IsValid);
            return view;
        }
        private void MakeRenderer(Transform parent)
        { _render=Activator.CreateInstance(_renderType,new object[]{parent}); }
        private void Apply(SpectatorPrivateView view) => _renderType.GetMethod("Apply").Invoke(_render,new object[]{view});
        private int Count => (int)_renderType.GetProperty("Count").GetValue(_render);
        [Test] public void OwnerCaptureUsesExistingVisibleRootsAndExactHeldMarker()
        {
            var view=Capture();Assert.GreaterOrEqual(view.Phantoms.Length,3);
            Assert.IsTrue(view.Marker.Present);Assert.AreEqual(3,view.Marker.Radius);Assert.AreEqual(6,view.Marker.Style);
            Assert.IsTrue(view.Marker.Beacon);
            Assert.AreEqual(new Color32(71,19,209,255),view.Marker.Colour);
            var marker=(GroundReticle)typeof(Abilities.HeroAbilitySystem).GetField("_reticle",Hidden).GetValue(_abilities);
            Assert.AreEqual(marker.transform.position,view.Marker.Position);
            var hex=Object.FindAnyObjectByType<HexedPhantomSlippers>();
            var live=(IEnumerable)typeof(HexedPhantomSlippers).GetField("_phantoms",Hidden).GetValue(hex);
            foreach(var phantom in live)
            {
                var body=(GameObject)phantom.GetType().GetField("Body").GetValue(phantom);
                bool matched=false;foreach(var item in view.Phantoms)
                    if(item.Kind==SpectatorPhantom.HexShoe && item.Position==body.transform.position && item.Rotation==body.transform.rotation
                        && item.Scale==body.transform.lossyScale)matched=true;
                Assert.IsTrue(matched,"The sampled hallucination was invented instead of copied from its owner.");
            }
        }
        [Test] public void ObservedCopiesAreRenderOnlyAndDoNotMutateOrDestroyTheirSources()
        {
            var view=Capture();Vector3 owner=_owner.transform.position,other=_other.transform.position,shoe=_shoe.transform.position;
            var mesh=_shoe.GetComponent<MeshFilter>().sharedMesh;MakeRenderer(_proxy.transform);Apply(view);
            Assert.AreEqual(view.Phantoms.Length,Count);Assert.IsTrue((bool)_renderType.GetProperty("MarkerVisible").GetValue(_render));
            foreach(Transform child in _proxy.transform)
            {
                if(child.name!="Observed private phantom")continue;
                Assert.AreEqual(0,child.GetComponentsInChildren<Collider>(true).Length);
                Assert.AreEqual(0,child.GetComponentsInChildren<Rigidbody>(true).Length);
                Assert.AreEqual(0,child.GetComponentsInChildren<MonoBehaviour>(true).Length);
                foreach(var renderer in child.GetComponentsInChildren<Renderer>())
                    Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off,renderer.shadowCastingMode);
            }
            Assert.AreEqual(owner,_owner.transform.position);Assert.AreEqual(other,_other.transform.position);Assert.AreEqual(shoe,_shoe.transform.position);
            _renderType.GetMethod("Clear").Invoke(_render,null);Assert.AreEqual(0,Count);
            Assert.IsFalse((bool)_renderType.GetProperty("MarkerVisible").GetValue(_render));Assert.IsNotNull(mesh);
            Assert.IsTrue(_owner.IsHexed && _owner.IsDisoriented);
        }
        [Test] public void AllNineRenderRootsPreserveWorldPoseUnderATransformedCameraParent()
        {
            var view=Capture();view.Phantoms.Clear();
            for(int i=0;i<SpectatorPrivateView.MaxPhantoms;i++)
                view.Phantoms.Add(new SpectatorPhantom {Kind=SpectatorPhantom.HexShoe,Id=(uint)i+1,SourceSeat=0,Skin=0,
                    Position=new Vector3(i*.4f,3+i*.1f,-2),Rotation=Quaternion.Euler(i*3,i*17,2),Scale=new Vector3(1.3f,.8f,1.1f)});
            _proxy.transform.SetPositionAndRotation(new Vector3(5,7,-3),Quaternion.Euler(10,50,4));_proxy.transform.localScale=Vector3.one*2;
            MakeRenderer(_proxy.transform);Apply(view);Assert.AreEqual(9,Count);int index=0;
            foreach(Transform child in _proxy.transform)
            {
                if(child.name!="Observed private phantom")continue;
                var expected=view.Phantoms[index++];
                Assert.That(Vector3.Distance(child.position,expected.Position),Is.LessThan(.0001f));
                Assert.That(Quaternion.Angle(child.rotation,expected.Rotation.normalized),Is.LessThan(.05f));
                Assert.That(Vector3.Distance(child.lossyScale,expected.Scale),Is.LessThan(.0001f));
            }
            Assert.AreEqual(9,index);
            var marker=_proxy.GetComponentInChildren<GroundReticle>();Assert.IsNotNull(marker);
            Assert.That(Vector3.Distance(marker.transform.lossyScale,Vector3.one),Is.LessThan(.0001f));
            var beacon=(GameObject)typeof(GroundReticle).GetField("_beaconGo",Hidden).GetValue(marker);Assert.IsTrue(beacon.activeInHierarchy);
        }
        [Test] public void TheOriginalPrivateGeometryAlsoHidesAfterItsCameraIsReleased()
        {
            Capture();_rig.SetActive(false);
            foreach(var type in new[]{typeof(HexedPhantomSlippers),typeof(DisorientedHallucinations)})
            {
                var component=Object.FindAnyObjectByType(type);
                type.GetMethod("LateUpdate",Hidden).Invoke(component,null);
                var phantoms=(IEnumerable)type.GetField("_phantoms",Hidden).GetValue(component);
                foreach(var phantom in phantoms)
                    Assert.IsFalse(((GameObject)phantom.GetType().GetField("Body").GetValue(phantom)).activeSelf);
            }
            Assert.IsTrue(_owner.IsHexed && _owner.IsDisoriented);
        }
        [Test] public void ManualMissingAndPresentationHeldViewsClearPrivateCopies()
        {
            var view=Capture();_rig.Camera.tag="Untagged";
            var camera=new GameObject("Witness");camera.transform.SetParent(_root.transform);camera.tag="MainCamera";
            _watcher=camera.AddComponent<SpectatorCamera>();Assert.IsTrue(_watcher.TryWatchPlayer(1));
            MakeRenderer(camera.transform);typeof(SpectatorCamera).GetField("_privateView",Hidden).SetValue(_watcher,_render);
            Apply(view);Assert.Greater(Count,0);_watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.Pov);
            var late=typeof(SpectatorCamera).GetMethod("LateUpdate",Hidden);late.Invoke(_watcher,null);Assert.AreEqual(0,Count);
            _watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.Pov);Apply(view);_rig.SetActive(false);late.Invoke(_watcher,null);Assert.AreEqual(0,Count);
            Apply(view);typeof(PresentationClock).GetMethod("Hold",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            try { late.Invoke(_watcher,null);Assert.AreEqual(0,Count); }
            finally { typeof(PresentationClock).GetMethod("Release",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null); }
        }
        [UnityTest] public IEnumerator AMenuCannotKeepPrivateCopiesAliveAfterTheViewStreamDisappears()
        {
            var view=Capture();_rig.SetActive(false);_rig.Camera.tag="Untagged";
            var camera=new GameObject("Witness with menu");camera.transform.SetParent(_root.transform);camera.tag="MainCamera";
            _watcher=camera.AddComponent<SpectatorCamera>();Assert.IsTrue(_watcher.TryWatchPlayer(1));
            MakeRenderer(camera.transform);typeof(SpectatorCamera).GetField("_privateView",Hidden).SetValue(_watcher,_render);Apply(view);
            typeof(SpectatorCamera).GetField("_observedActor",Hidden).SetValue(_watcher,_owner);
            typeof(SpectatorCamera).GetField("_observedEpoch",Hidden).SetValue(_watcher,_owner.MovementEpoch);
            typeof(SpectatorCamera).GetField("_observedViewAvailable",Hidden).SetValue(_watcher,true);
            GameLaunch.Spectator=true;NetAuthority.Provider=new DisconnectedView();
            var panel=UI.Panel.Open<UI.PausePanel>(_watcher);
            yield return null;
            try
            {
                Assert.IsTrue(UI.Panel.AnyOpen);
                typeof(SpectatorCamera).GetField("_privateView",Hidden).SetValue(_watcher,_render);Apply(view);
                typeof(SpectatorCamera).GetField("_observedActor",Hidden).SetValue(_watcher,_owner);
                typeof(SpectatorCamera).GetField("_observedEpoch",Hidden).SetValue(_watcher,_owner.MovementEpoch);
                typeof(SpectatorCamera).GetField("_observedViewAvailable",Hidden).SetValue(_watcher,true);
                typeof(SpectatorCamera).GetMethod("LateUpdate",Hidden).Invoke(_watcher,null);
                Assert.AreEqual(0,Count);Assert.IsFalse(_watcher.PrivateMarkerVisible);Assert.IsNull(_watcher.PlayerViewTarget);
            }
            finally { panel.Close(); }
        }
    }
}
