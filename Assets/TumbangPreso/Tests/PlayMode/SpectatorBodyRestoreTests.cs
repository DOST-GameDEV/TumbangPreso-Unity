using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorBodyRestoreTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private GameObject _root;private CharacterMotor _body;private Renderer _mesh;private CameraRig _rig;private MatchInstaller _installer;
        private INetProvider _previous;private bool _watch,_bots;private CursorLockMode _cursor;private bool _visible;
        private sealed class Solo:INetProvider
        {public bool IsHost=>true;public bool IsNetworked=>false;public int LocalSlot=>1;public int LocalPeerId=>1;public bool IsSeatlessReferee=>false;}
        [UnitySetUp]public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();_previous=NetAuthority.Provider;NetAuthority.Provider=new Solo();
            _watch=GameLaunch.Spectator;_bots=GameLaunch.AllBots;GameLaunch.Spectator=false;GameLaunch.AllBots=false;
            _cursor=Cursor.lockState;_visible=Cursor.visible;
            _root=new GameObject("Body visibility handoff");var body=new GameObject("Former human",typeof(CharacterController));body.transform.SetParent(_root.transform);
            _body=body.AddComponent<CharacterMotor>();_body.enabled=false;_body.PlayerSlot=1;
            var model=GameObject.CreatePrimitive(PrimitiveType.Cube);model.name="Body mesh";model.transform.SetParent(body.transform);_mesh=model.GetComponent<Renderer>();
            var rig=new GameObject("Gameplay rig");rig.transform.SetParent(_root.transform);_rig=rig.AddComponent<CameraRig>();_rig.enabled=false;
            var holder=new GameObject("Inactive installer");holder.transform.SetParent(_root.transform);holder.SetActive(false);_installer=holder.AddComponent<MatchInstaller>();
            var seats=new CharacterMotor[4];seats[1]=_body;typeof(MatchInstaller).GetField("_seats",Hidden).SetValue(_installer,seats);
            var watcher=new GameObject("Existing watcher");watcher.transform.SetParent(_root.transform);watcher.AddComponent<SpectatorCamera>().enabled=false;
            yield return null;
        }
        [UnityTearDown]public IEnumerator After()
        {if(_root!=null)Object.Destroy(_root);yield return PlayModeWorld.Reset();NetAuthority.Provider=_previous;GameLaunch.Spectator=_watch;GameLaunch.AllBots=_bots;Cursor.lockState=_cursor;Cursor.visible=_visible;}
        private void Follow()
        {_rig.Follow(_body);Assert.AreEqual(ShadowCastingMode.ShadowsOnly,_mesh.shadowCastingMode);}
        private void Watch()
        {GameLaunch.Spectator=true;_installer.RebindLocalSeat(-1,true);Assert.IsFalse((bool)typeof(CameraRig).GetField("_active",Hidden).GetValue(_rig));}
        [Test]public void PublicWatchHandoffThenEmoteReleaseKeepsTheBodyVisible()
        {
            Follow();_rig.BeginEmoteView();Assert.AreEqual(ShadowCastingMode.On,_mesh.shadowCastingMode);Watch();
            Assert.AreEqual(ShadowCastingMode.On,_mesh.shadowCastingMode);_rig.EndEmoteView();Assert.AreEqual(ShadowCastingMode.On,_mesh.shadowCastingMode);
        }
        [Test]public void ActiveFppRestoresItsOwnHideAfterTheEmote()
        {Follow();_rig.BeginEmoteView();_rig.EndEmoteView();Assert.AreEqual(ShadowCastingMode.ShadowsOnly,_mesh.shadowCastingMode);}
        [Test]public void ImmediatePublicWatchHandoffAlreadyRestoresTheBody()
        {Follow();Watch();Assert.AreEqual(ShadowCastingMode.On,_mesh.shadowCastingMode);}
        [Test]public void InactiveFollowDoesNotLeaveTheNewBodyHidden()
        {_rig.SetActive(false);_rig.Follow(_body,false);Assert.AreEqual(ShadowCastingMode.On,_mesh.shadowCastingMode);}
        [Test]public void InactiveReleasePreservesTheAuthoredShadowlessMode()
        {
            _mesh.shadowCastingMode=ShadowCastingMode.Off;Follow();_rig.BeginEmoteView();Watch();_rig.EndEmoteView();Assert.AreEqual(ShadowCastingMode.Off,_mesh.shadowCastingMode);
        }
    }
}
