using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace TumbangPreso.PlayTests
{
    public sealed class ArrivalViewOwnershipTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private GameObject _root;private CharacterMotor _body;private Renderer _mesh;private CameraRig _rig;private Camera _camera;
        private MatchInstaller _installer;private SpectatorCamera _watcher;private Hud _hud;private MatchArrivalPresentation _arrival;private IEnumerator _run;
        private INetProvider _previous;private bool _watch,_bots,_motion;private CursorLockMode _cursor;private bool _cursorVisible;
        private sealed class Solo:INetProvider
        {public bool IsHost=>true;public bool IsNetworked=>false;public int LocalSlot=>1;public int LocalPeerId=>1;public bool IsSeatlessReferee=>false;}
        [UnitySetUp]public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();_previous=NetAuthority.Provider;NetAuthority.Provider=new Solo();
            _watch=GameLaunch.Spectator;_bots=GameLaunch.AllBots;GameLaunch.Spectator=false;GameLaunch.AllBots=false;
            _motion=Settings.SettingsStore.Current.CinematicCameraMotion;Settings.SettingsStore.Current.CinematicCameraMotion=true;
            _cursor=Cursor.lockState;_cursorVisible=Cursor.visible;GameServices.Ensure();
            _root=new GameObject("Arrival owner fixture");var body=new GameObject("Human seat",typeof(CharacterController));body.transform.SetParent(_root.transform);
            _body=body.AddComponent<CharacterMotor>();_body.enabled=false;_body.PlayerSlot=1;
            var mesh=GameObject.CreatePrimitive(PrimitiveType.Cube);mesh.transform.SetParent(body.transform);_mesh=mesh.GetComponent<Renderer>();
            var rig=new GameObject("Gameplay view");rig.transform.SetParent(_root.transform);rig.tag="MainCamera";_rig=rig.AddComponent<CameraRig>();_rig.enabled=false;
            _camera=rig.GetComponent<Camera>();_rig.Follow(_body);
            var install=new GameObject("Inactive installer");install.transform.SetParent(_root.transform);install.SetActive(false);_installer=install.AddComponent<MatchInstaller>();
            var seats=new CharacterMotor[4];seats[1]=_body;typeof(MatchInstaller).GetField("_seats",Hidden).SetValue(_installer,seats);
            var view=new GameObject("Watcher");view.transform.SetParent(_root.transform);_watcher=view.AddComponent<SpectatorCamera>();_watcher.enabled=false;view.tag="Untagged";
            var hud=new GameObject("HUD role owner");hud.transform.SetParent(_root.transform);_hud=hud.AddComponent<Hud>();
            _arrival=_root.AddComponent<MatchArrivalPresentation>();yield return null;
        }
        [UnityTearDown]public IEnumerator After()
        {
            _arrival?.Cancel();(_run as System.IDisposable)?.Dispose();if(_root!=null)Object.Destroy(_root);yield return PlayModeWorld.Reset();
            NetAuthority.Provider=_previous;GameLaunch.Spectator=_watch;GameLaunch.AllBots=_bots;Settings.SettingsStore.Current.CinematicCameraMotion=_motion;
            Cursor.lockState=_cursor;Cursor.visible=_cursorVisible;
        }
        private void Begin()
        {
            _run=_arrival.Run();Assert.IsTrue(_run.MoveNext());Assert.IsTrue(_run.MoveNext());
            Assert.IsTrue((bool)typeof(MatchArrivalPresentation).GetField("_rigActive",Hidden).GetValue(_arrival));
            Assert.IsTrue(MatchArrivalPresentation.Active);Assert.AreEqual(ShadowCastingMode.On,_mesh.shadowCastingMode);
        }
        private void Watch(bool launchFlag)
        {GameLaunch.Spectator=launchFlag;_installer.RebindLocalSeat(-1,true);Assert.IsTrue(_hud.Spectating);Assert.IsTrue(_watcher.enabled);}
        private void SampleHandoff()
        {typeof(MatchArrivalPresentation).GetMethod("Sample",Hidden).Invoke(_arrival,new object[]{8.5f,false,SceneFlow.PreviewFor("Eskinita")});}
        [Test]public void ArrivalCancellationDoesNotReclaimTheSpectatorsGameplayBody()
        {Begin();Watch(true);_arrival.Cancel();Assert.IsFalse(_camera.enabled);Assert.AreEqual(ShadowCastingMode.On,_mesh.shadowCastingMode);}
        [Test]public void ArrivalFinalHandoffHonorsLiveWatchOwnershipWithoutALaunchFlag()
        {Begin();Watch(false);SampleHandoff();Assert.IsFalse((bool)typeof(CameraRig).GetField("_active",Hidden).GetValue(_rig));Assert.AreEqual(ShadowCastingMode.On,_mesh.shadowCastingMode);}
        [Test]public void NormalGameplayCancellationStillRestoresTheActiveFppView()
        {Begin();_arrival.Cancel();Assert.IsTrue(_camera.enabled);Assert.AreEqual(ShadowCastingMode.ShadowsOnly,_mesh.shadowCastingMode);}
        [Test]public void ReseatingBeforeArrivalReturnKeepsTheCurrentGameplayView()
        {Begin();Watch(true);GameLaunch.Spectator=false;_installer.RebindLocalSeat(1,false);Assert.IsFalse(_hud.Spectating);_arrival.Cancel();Assert.IsTrue(_camera.enabled);Assert.AreEqual(ShadowCastingMode.ShadowsOnly,_mesh.shadowCastingMode);}
    }
}
