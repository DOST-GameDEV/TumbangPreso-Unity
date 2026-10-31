using System.Collections;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorActualViewTests
    {
        private GameObject _root;
        private CharacterMotor _actor;
        private CameraRig _rig;
        private SpectatorCamera _watcher;
        private INetProvider _previous;
        private bool _spectator, _bots;
        private CursorLockMode _cursor;
        private bool _cursorVisible;
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private sealed class Solo : INetProvider
        { public bool IsHost => true; public bool IsNetworked => false; public int LocalSlot => 1;
          public int LocalPeerId => 1; public bool IsSeatlessReferee => false; }
        private sealed class RemoteWatcher : INetProvider
        { public bool IsHost => false; public bool IsNetworked => true; public int LocalSlot => -1;
          public int LocalPeerId => 2; public bool IsSeatlessReferee => false; }
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset(); _previous = NetAuthority.Provider; NetAuthority.Provider = new Solo();
            _spectator = GameLaunch.Spectator; _bots = GameLaunch.AllBots; GameLaunch.Spectator = GameLaunch.AllBots = false;
            _cursor = Cursor.lockState; _cursorVisible = Cursor.visible;
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Match.ApplySnapshot(new int[4], 1, true);
            _root = new GameObject("Actual observed view fixture");
            var body = new GameObject("Player", typeof(CharacterController)); body.transform.SetParent(_root.transform);
            _actor = body.AddComponent<CharacterMotor>(); _actor.enabled = false; _actor.IsPerson = true; _actor.PlayerSlot = 1;
            _actor.transform.position = new Vector3(2, 7, -4); GameServices.Round.Register(_actor); SpectatorCamera.Register(_actor);
            var source = new GameObject("Actual player camera"); source.transform.SetParent(_root.transform);
            _rig = source.AddComponent<CameraRig>(); _rig.Follow(_actor);
            _rig.Camera.tag = "Untagged"; // The source is off-screen; the observer owns this fixture's main view.
            var observer = new GameObject("Observer"); observer.transform.SetParent(_root.transform); observer.tag = "MainCamera";
            _watcher = observer.AddComponent<SpectatorCamera>();
            Assert.IsTrue(_watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.FollowNext));
            Assert.IsTrue(_watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.Pov));
            yield return null;
        }
        [UnityTearDown] public IEnumerator After()
        {
            SpectatorCamera.Unregister(_actor); if (_root != null) Object.Destroy(_root);
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _previous;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _bots;
            Cursor.lockState = _cursor; Cursor.visible = _cursorVisible;
        }
        private void CopyFinalView()
        {
            ObservedPlayerView.Capture(_rig, _actor);
            typeof(SpectatorCamera).GetMethod("LateUpdate", Hidden).Invoke(_watcher, null);
        }
        [Test] public void ARecoveredRemoteViewReturnsDirectlyToTheOwnersEyes()
        {
            var router=new GameObject("Recovered view scope");
            try
            {
                var rpc=router.AddComponent<Net.MatchRpc>();
                typeof(Net.MatchRpc).GetProperty("PresentationMatchId").SetValue(rpc,100L);
                NetAuthority.Provider=new RemoteWatcher();GameLaunch.Spectator=true;_rig.SetActive(false);
                var state=new Net.SpectatorViewState {Match=100,Round=1,Slot=1,Epoch=_actor.MovementEpoch,Sequence=1,
                    Eye=_actor.transform.position+Vector3.up,Rotation=Quaternion.Euler(10,120,0),Fov=91,
                    HandsRotation=Quaternion.identity,Flags=Net.SpectatorViewState.FirstPerson};
                var late=typeof(SpectatorCamera).GetMethod("LateUpdate",Hidden);
                Assert.IsTrue(ObservedPlayerView.InstallRemote(state,_actor));late.Invoke(_watcher,null);
                Assert.IsTrue(_watcher.IsWatchingEyes(_actor));
                ObservedPlayerView.ClearRemote();late.Invoke(_watcher,null);
                Assert.IsFalse(_watcher.IsWatchingEyes(_actor));
                Assert.Greater(Vector3.Distance(state.Eye,_watcher.transform.position),1);
                state.Sequence++;Assert.IsTrue(ObservedPlayerView.InstallRemote(state,_actor));late.Invoke(_watcher,null);
                Assert.IsTrue(_watcher.IsWatchingEyes(_actor));
                Assert.Less(Vector3.Distance(state.Eye,_watcher.transform.position),.0001f);
                Assert.Less(Quaternion.Angle(state.Rotation,_watcher.transform.rotation),.001f);
            }
            finally { Object.DestroyImmediate(router);ObservedPlayerView.ClearRemote(); }
        }
        [TestCase(16f/9,20f/9)] [TestCase(20f/9,16f/9)] [TestCase(4f/3,20f/9)]
        public void ObservedViewportPreservesTheOwnersFrustum(float source,float output)
        {
            var fit=SpectatorCamera.FitObservedViewport(source,output);
            Assert.AreEqual(source,output*fit.width/fit.height,.0001f);
            Assert.AreEqual(.5f,fit.center.x,.0001f);Assert.AreEqual(.5f,fit.center.y,.0001f);
            Assert.LessOrEqual(fit.width,1);Assert.LessOrEqual(fit.height,1);
        }
        [Test] public void RealOwnerLensIsMatchedAndFreeFlightRestoresTheFullViewport()
        {
            var camera=_watcher.GetComponent<Camera>();
            var output=new RenderTexture(1600,720,16);var previous=camera.targetTexture;
            Assert.IsTrue(_watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.FreeFlight));
            typeof(SpectatorCamera).GetMethod("LateUpdate",Hidden).Invoke(_watcher,null);
            float near=camera.nearClipPlane,far=camera.farClipPlane;
            Assert.IsTrue(_watcher.TryWatchPlayer(1));
            try
            {
                camera.targetTexture=output;_rig.Camera.aspect=4f/3;CopyFinalView();
                Assert.AreEqual(4f/3,camera.aspect,.0001f);Assert.AreEqual(1,_watcher.ObservedViewport.height,.0001f);
                Assert.AreEqual(_rig.Camera.nearClipPlane,camera.nearClipPlane,.00001f);Assert.AreEqual(_rig.Camera.farClipPlane,camera.farClipPlane,.001f);
                Assert.Less(_watcher.ObservedViewport.width,1);
                var point=_rig.Camera.transform.TransformPoint(new Vector3(1,.5f,10));
                Assert.Less(Vector3.Distance(_rig.Camera.WorldToViewportPoint(point),camera.WorldToViewportPoint(point)),.0001f);
                var bars=Object.FindObjectsByType<Canvas>().Single(c=>c.name=="ObservedLensBars");Assert.IsTrue(bars.enabled);
                Assert.IsTrue(_watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.FreeFlight));
                typeof(SpectatorCamera).GetMethod("LateUpdate",Hidden).Invoke(_watcher,null);
                Assert.AreEqual(new Rect(0,0,1,1),camera.rect);Assert.IsFalse(bars.enabled);
                Assert.AreEqual(1600f/720,camera.aspect,.0001f);
                Assert.AreEqual(near,camera.nearClipPlane,.00001f);Assert.AreEqual(far,camera.farClipPlane,.001f);
            }
            finally { camera.targetTexture=previous;Object.DestroyImmediate(output); }
        }
        [Test] public void FollowNextUsesSeatOrderAfterADirectPovSelection()
        {
            CharacterMotor Add(int slot)
            {
                var body = new GameObject("Seat " + slot); body.transform.SetParent(_root.transform);
                var actor = body.AddComponent<CharacterMotor>(); actor.enabled = false; actor.PlayerSlot = slot;
                GameServices.Round.Register(actor); SpectatorCamera.Register(actor); return actor;
            }
            var last = Add(3); var first = Add(0);
            try
            {
                Assert.AreSame(_actor, _watcher.FollowedPlayer);
                _watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.FollowNext);
                Assert.AreSame(last, _watcher.FollowedPlayer); Assert.IsTrue(_watcher.WatchingFirstPerson);
                _watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.FollowNext); Assert.IsNull(_watcher.FollowedPlayer);
                _watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.FollowNext);
                Assert.AreSame(first, _watcher.FollowedPlayer); Assert.IsTrue(_watcher.WatchingFirstPerson);
            }
            finally { SpectatorCamera.Unregister(first); SpectatorCamera.Unregister(last); }
        }
        [Test] public void ObservesTheActualEyePitchRollAndFovWithoutTakingPlayerInput()
        {
            Vector3 body = _actor.transform.position;
            Vector3 eye = body + new Vector3(.07f, .92f, -.13f);
            Quaternion look = Quaternion.Euler(38, 147, -11);
            _rig.Camera.transform.SetPositionAndRotation(eye, look); _rig.Camera.fieldOfView = 91;
            Shader.SetGlobalFloat(Visual.NearFade.EyeDropId, .53f);
            CopyFinalView();
            Assert.That(Vector3.Distance(eye, _watcher.transform.position), Is.LessThan(.0001f));
            Assert.That(Quaternion.Angle(look, _watcher.transform.rotation), Is.LessThan(.001f));
            Assert.That(_watcher.GetComponent<Camera>().fieldOfView, Is.EqualTo(91).Within(.001f));
            Assert.AreEqual(body, _actor.transform.position); Assert.IsTrue(_rig.IsFollowing(_actor));
            Assert.That(Shader.GetGlobalFloat(Visual.NearFade.EyeDropId), Is.EqualTo(.53f).Within(.001f));
            Assert.IsTrue(_watcher.IsWatchingEyes(_actor));
        }
        [Test] public void HandActionAndHeldAimComeFromTheWatchedPlayerAndAdvanceOnlyOnce()
        {
            var source = _rig.GetComponentInChildren<ViewmodelArms>(true);
            Assert.IsNotNull(source); Assert.IsTrue(source.PlayAction("ground-call"));
            source.SetAimPreview("magma-palms"); source.StepObservedPresentation(.2f);
            CopyFinalView();
            var clone = _watcher.GetComponentInChildren<ViewmodelArms>(true);
            Assert.IsNotNull(clone); Assert.IsFalse(clone.enabled, "The clone must not also run its own LateUpdate.");
            Assert.AreEqual(source.ObservedActionName, clone.ObservedActionName);
            Assert.AreEqual(source.ObservedAimName, clone.ObservedAimName);
            Assert.That(clone.ObservedActionTime, Is.EqualTo(source.ObservedActionTime).Within(.001f));
            Assert.That(clone.ObservedVisualPhase, Is.EqualTo(source.ObservedVisualPhase).Within(.001f));
        }
        [Test] public void AnActualEmoteViewDoesNotKeepFirstPersonHandsOrHideTheBody()
        {
            _rig.BeginEmoteView();
            var eye = _actor.transform.position + new Vector3(3, 2, -5);
            _rig.Camera.transform.SetPositionAndRotation(eye, Quaternion.Euler(18, -25, 0));
            CopyFinalView();
            Assert.That(Vector3.Distance(eye, _watcher.transform.position), Is.LessThan(.0001f));
            Assert.IsFalse(_watcher.IsWatchingEyes(_actor));
            var clone = _watcher.GetComponentInChildren<ViewmodelArms>(true);
            Assert.IsTrue(clone == null || !clone.gameObject.activeInHierarchy);
        }
        [Test] public void VictimEffectsFollowTheSelectedEyesAndClearInThirdPerson()
        {
            CopyFinalView();
            var canvas = new GameObject("Observed status canvas", typeof(RectTransform), typeof(Canvas));
            canvas.transform.SetParent(_root.transform); canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var effects = canvas.AddComponent<UI.TumpHudEffects>(); effects.Build((RectTransform)canvas.transform);
            _actor.ApplyNetworkBlinded(2f); effects.Tick(null, true);
            Assert.IsTrue(effects.BlindedScreenVisible, "The watched victim's first-person blindness was omitted.");
            Assert.IsTrue(_watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.Pov));
            effects.Tick(null, true);
            Assert.IsFalse(effects.BlindedScreenVisible, "A third-person observer inherited somebody else's blind screen.");
        }
        [Test] public void AnotherNearbyBodyCannotBecomeTheViewOwner()
        {
            CopyFinalView();
            var other = new GameObject("Nearby other player", typeof(CharacterController)); other.transform.SetParent(_root.transform);
            var person = other.AddComponent<CharacterMotor>(); person.enabled = false; person.PlayerSlot = 2;
            person.transform.position = _watcher.transform.position - Vector3.up * SpectatorCamera.PovEyeHeightPerson;
            Assert.IsTrue(ViewPerspective.IsEyesOf(_watcher.GetComponent<Camera>(), _actor));
            Assert.IsFalse(ViewPerspective.IsEyesOf(_watcher.GetComponent<Camera>(), person));
        }
        [Test] public void WorldFiltersAndAudioFollowActualPlayerViewAndReleaseInManualViews()
        {
            _actor.Mode=Core.GameMode.HeroStrike;GameLaunch.Spectator=true;
            GameServices.Round.ApplySnapshot(100,true,0,true);
            _actor.ApplyHaunted();_actor.ApplyConcussed(Core.StatusRules.ConcussedSeconds-.3f);
            _actor.ApplyNetworkPossessed(Core.NemuRules.PossessedSeconds-.5f);CopyFinalView();
            var lens=_watcher.GetComponent<Camera>();var audio=GameServices.Audio;
            void Check(bool active)
            {
                Assert.AreEqual(active,Visual.HauntedPerception.Applies(lens));
                Assert.AreEqual(active,Visual.PossessedPerception.Weight(lens)>.1f);
                Assert.AreEqual(active,Visual.ConcussedPerception.Weight(lens)>.1f);
                typeof(AudioDirector).GetMethod("LateUpdate",Hidden).Invoke(audio,null);
                Assert.AreEqual(active,audio.HauntedMuffle);
            }
            Check(true);
            _rig.BeginEmoteView();CopyFinalView();Assert.IsFalse(_watcher.IsWatchingEyes(_actor));
            Check(true); // This is still the player's actual camera, including their emote view.
            _watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.Pov);Check(false);
            _watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.Pov);CopyFinalView();Check(true);
            using(audio.EnterReplayMix())Check(false);
            Check(true);_watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.FreeFlight);Check(false);
        }
        [Test] public void WatchedFirstPersonCanCarryUsesTheSameDrawingHeight()
        {
            CopyFinalView(); var can = new GameObject("View-specific carried can"); can.transform.SetParent(_root.transform);
            var lata = can.AddComponent<Lata>(); lata.enabled = false;
            float height = (float)typeof(Lata).GetMethod("HeldBaseHeightFor", Hidden).Invoke(lata, new object[] { _actor });
            Assert.That(height, Is.EqualTo(Lata.HoldBaseHeight).Within(.001f));
        }
        [Test] public void PrivateGhostReclaimCuesFollowTheViewedOwnerWithoutReclaiming()
        {
            GameLaunch.Spectator=true;CopyFinalView();
            var ghost=Abilities.NemuDormant.Spawn(Abilities.NemuDormant.Ghost.Peeker,_actor.transform.position+Vector3.right*3,
                Vector3.forward,_actor.PlayerSlot);
            try
            {
                Visual.NemuGhostRings.Ensure();var rings=Object.FindAnyObjectByType<Visual.NemuGhostRings>();Assert.IsNotNull(rings);
                var tick=typeof(Visual.NemuGhostRings).GetMethod("LateUpdate",Hidden);tick.Invoke(rings,null);
                Assert.AreEqual(1,Visual.NemuGhostRings.Count);
                _watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.Pov);tick.Invoke(rings,null);
                Assert.AreEqual(0,Visual.NemuGhostRings.Count);Assert.IsFalse(ghost.Reclaimed);
                foreach(var child in rings.GetComponentsInChildren<MeshRenderer>(true))
                    if(child.name=="NemuGhostRing")Assert.IsFalse(child.gameObject.activeInHierarchy);
                _watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.Pov);CopyFinalView();tick.Invoke(rings,null);
                Assert.AreEqual(1,Visual.NemuGhostRings.Count);Assert.IsFalse(ghost.Reclaimed);
            }
            finally { Abilities.NemuDormant.RetireAll(); }
        }
        [Test] public void TheWatchedVictimsRiderAndOwnStatusBadgeRespectTheActualView()
        {
            GameLaunch.Spectator=true;_actor.ApplyNetworkPossessed(Core.NemuRules.PossessedSeconds-.5f);CopyFinalView();
            var rider=_actor.GetComponentInChildren<Visual.PossessedPresence>();Assert.IsNotNull(rider);
            var badges=_actor.GetComponent<Visual.StatusOverhead>()??_actor.gameObject.AddComponent<Visual.StatusOverhead>();
            bool HiddenRider()=>(bool)typeof(Visual.PossessedPresence).GetMethod("HiddenFromThisViewer",Hidden).Invoke(rider,null);
            bool OwnBadges()=>(bool)typeof(Visual.StatusOverhead).GetMethod("IsLocalView",Hidden).Invoke(badges,null);
            Assert.IsTrue(HiddenRider());Assert.IsTrue(OwnBadges());
            _watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.Pov);Assert.IsFalse(HiddenRider());Assert.IsFalse(OwnBadges());
            _watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.Pov);_rig.BeginEmoteView();CopyFinalView();
            Assert.IsFalse(HiddenRider(),"A third-person emote should show the rider on the visible body.");
            Assert.IsTrue(OwnBadges(),"Actual player-view HUD status already replaces its own overhead badge.");
        }
        [Test] public void AWatchedOwnersDormantHighlightDoesNotGiveTheSpectatorAReclaimAction()
        {
            GameLaunch.Spectator=true;CopyFinalView();
            var ghost=Abilities.NemuDormant.Spawn(Abilities.NemuDormant.Ghost.Peeker,_actor.transform.position+Vector3.right*.5f,Vector3.forward,_actor.PlayerSlot);
            try
            {
                var near=typeof(Abilities.NemuDormant).GetMethod("HerReachIsHere",Hidden);
                Assert.IsTrue((bool)near.Invoke(ghost,null));
                _watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.Pov);Assert.IsFalse((bool)near.Invoke(ghost,null));
                Assert.IsFalse(ghost.Reclaimed);
            }
            finally { Abilities.NemuDormant.RetireAll(); }
        }
        [Test] public void WaitingGhostOverviewStaysVisibleAndPlayerViewUsesThatPlayersOpacity()
        {
            GameLaunch.Spectator=true;CopyFinalView();
            float age=Core.NecroRules.DontLookArmSeconds+1;
            var other=Abilities.NemuBlindfold.Place(new Vector3(3,0,2),Vector3.forward,0,age);
            var visibility=typeof(Abilities.NemuBlindfold).GetMethod("Visibility",Hidden,null,new[]{typeof(float)},null);
            float Seen(Abilities.NemuBlindfold ghost)=>(float)visibility.Invoke(ghost,new object[]{age});
            float watchedOther=Seen(other);
            _watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.Pov);float overview=Seen(other);
            Assert.Greater(overview,watchedOther,"Overview should retain the spectator's complete skill visibility.");
            _watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.Pov);CopyFinalView();
            var own=Abilities.NemuBlindfold.Place(new Vector3(4,0,2),Vector3.forward,_actor.PlayerSlot,age);
            Assert.That(Seen(own),Is.EqualTo(overview).Within(.001f));
        }
        [Test] public void AParticipantCaptionClearsFromTheirEyesButRemainsInManualViews()
        {
            GameLaunch.Spectator=true;CopyFinalView();
            var root=new GameObject("Scoped caption");root.transform.SetParent(_root.transform);
            var popup=root.AddComponent<Visual.ComicPopup>();
            typeof(Visual.ComicPopup).GetField("_firstPersonParticipant",Hidden).SetValue(popup,_actor);
            typeof(Visual.ComicPopup).GetField("_layoutSize",Hidden).SetValue(popup,new Vector2(2,1));
            typeof(Visual.ComicPopup).GetField("_animatedScale",Hidden).SetValue(popup,1f);
            root.transform.position=_watcher.transform.position+_watcher.transform.forward*4;
            var render=typeof(Visual.ComicPopup).GetMethod("ApplyView",Hidden);
            render.Invoke(popup,new object[]{_watcher.GetComponent<Camera>()});Assert.AreEqual(Vector3.zero,root.transform.localScale);
            _watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.Pov);
            render.Invoke(popup,new object[]{_watcher.GetComponent<Camera>()});Assert.Greater(root.transform.localScale.x,0);
        }
    }
}
