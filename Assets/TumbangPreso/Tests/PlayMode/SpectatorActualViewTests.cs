using System.Collections;
using System.Reflection;
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
        [Test] public void WatchedFirstPersonCanCarryUsesTheSameDrawingHeight()
        {
            CopyFinalView(); var can = new GameObject("View-specific carried can"); can.transform.SetParent(_root.transform);
            var lata = can.AddComponent<Lata>(); lata.enabled = false;
            float height = (float)typeof(Lata).GetMethod("HeldBaseHeightFor", Hidden).Invoke(lata, new object[] { _actor });
            Assert.That(height, Is.EqualTo(Lata.HoldBaseHeight).Within(.001f));
        }
    }
}
