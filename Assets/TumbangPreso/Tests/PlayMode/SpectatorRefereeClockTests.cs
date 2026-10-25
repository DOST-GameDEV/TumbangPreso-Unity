using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorRefereeClockTests
    {
        private sealed class Host : INetProvider
        {
            public bool Referee;
            public bool IsHost => true; public bool IsNetworked => true;
            public int LocalSlot => Referee ? -1 : 0; public int LocalPeerId => 0;
            public bool IsSeatlessReferee => Referee;
        }
        private INetProvider _old;
        private bool _watch;
        private GameObject _root;
        private SpectatorCamera _camera;
        private Host _host;
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset(); GameServices.Ensure(); NetSession.Ensure();
            _old = NetAuthority.Provider; _watch = GameLaunch.Spectator;
            GameLaunch.Spectator = false; _host = new Host { Referee = true }; NetAuthority.Provider = _host;
            _root = new GameObject("Seatless referee controls"); _camera = _root.AddComponent<SpectatorCamera>(); _camera.enabled = false;
        }
        [UnityTearDown] public IEnumerator After()
        {
            Time.timeScale = 1; Object.Destroy(_root);
            NetAuthority.Provider = _old; GameLaunch.Spectator = _watch;
            yield return PlayModeWorld.Reset();
        }
        private bool Command(SpectatorCamera.ViewCommand command)
            => (bool)typeof(SpectatorCamera).GetMethod("ExecuteOperatorCommand", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_camera, new object[] { command });
        [Test] public void TheSeatlessRefereePausesAndResumesWithoutTheClientLaunchFlag()
        {
            Assert.IsFalse(GameLaunch.Spectator);
            Assert.IsTrue(Command(SpectatorCamera.ViewCommand.Pause)); Assert.AreEqual(0, PresentationClock.RequestedScale);
            Assert.IsTrue(Command(SpectatorCamera.ViewCommand.Pause)); Assert.AreEqual(1, PresentationClock.RequestedScale);
        }
        [Test] public void RefereeSlowMotionRemainsTheResumeSpeed()
        {
            Assert.IsTrue(Command(SpectatorCamera.ViewCommand.Speed)); Assert.AreEqual(.25f, PresentationClock.RequestedScale);
            Assert.IsTrue(Command(SpectatorCamera.ViewCommand.Pause)); Assert.AreEqual(0, PresentationClock.RequestedScale);
            Assert.IsTrue(Command(SpectatorCamera.ViewCommand.Pause)); Assert.AreEqual(.25f, PresentationClock.RequestedScale);
        }
        [Test] public void APlayingHostDoesNotGainSpectatorClockControls()
        {
            _host.Referee = false;
            Assert.IsFalse(Command(SpectatorCamera.ViewCommand.Pause));
            Assert.IsFalse(Command(SpectatorCamera.ViewCommand.Speed)); Assert.AreEqual(1, PresentationClock.RequestedScale);
        }
        [TestCase(true,2)] [TestCase(false,0)]
        public void ActualPlayerViewDemandUsesTheRefereeRoleWithoutGrantingItToAPlayingHost(bool referee,int expected)
        {
            _host.Referee=referee;Assert.IsFalse(GameLaunch.Spectator);
            var body=new GameObject("Watched seat",typeof(CharacterController));body.transform.SetParent(_root.transform);
            var actor=body.AddComponent<CharacterMotor>();actor.enabled=false;actor.PlayerSlot=1;
            ObservedPlayerView.SetTarget(actor);Assert.AreEqual(1,ObservedPlayerView.RequestedSlot);
            Assert.IsNotNull(MatchRpc.Instance);
            var needed=typeof(MatchRpc).GetMethod("NeededViewMask",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.AreEqual(expected,(byte)needed.Invoke(MatchRpc.Instance,null));
        }
    }
}
