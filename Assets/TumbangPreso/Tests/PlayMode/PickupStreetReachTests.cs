using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class PickupStreetReachTests
    {
        private sealed class Solo : INetProvider
        {
            public bool IsHost => true; public bool IsNetworked => false;
            public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false;
        }
        private INetProvider _provider;
        private GameObject _body, _shoeBody, _barrier, _occluderBody;
        private CharacterMotor _motor;
        private Carrier _carrier;
        private Slipper _shoe;
        private MatchStatsCollector _stats;
        private bool _network, _training, _tutorial, _spectator, _allBots;
        private Vector3 ReachFrom => _body.transform.position + Vector3.up * .5f;
        private Vector3 ReachTo => _shoeBody.transform.position + Vector3.up * .1f;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; NetAuthority.Provider = new Solo();
            _network = SceneFlow.Networked; SceneFlow.Networked = false;
            _training = GameLaunch.TrainingRange; _tutorial = GameLaunch.GuidedTutorial;
            _spectator = GameLaunch.Spectator; _allBots = GameLaunch.AllBots;
            GameLaunch.TrainingRange = GameLaunch.GuidedTutorial = GameLaunch.Spectator = GameLaunch.AllBots = false;
            Hitstop.End(); PresentationClock.RequestScale(1);
            SceneManager.SetActiveScene(SceneManager.CreateScene(SceneFlow.Eskinita)); GameServices.Ensure();
            _stats = GameServices.Stats; typeof(GameServices).GetProperty("Stats").SetValue(null, null);
            GameServices.Round.BeginRound();
            _body = new GameObject("Street reach pickup actor");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 1; _motor.Mode = GameMode.Classic; _motor.IsDefender = false; _motor.RoundActive = true;
            _body.transform.position = Vector3.back * (Balance.ConfinementRadius + 1);
            _carrier = _body.AddComponent<Carrier>(); _carrier.enabled = false;
            _shoeBody = new GameObject("Street reach owned shoe"); _shoe = _shoeBody.AddComponent<Slipper>(); _shoe.enabled = false;
            _shoe.OwnerSlot = _shoe.SeatOfOrigin = 1;
            _shoeBody.transform.position = _body.transform.position + Vector3.forward * 1.2f + Vector3.up * Balance.SlipperRestHeight;
            _motor.Intent.Clear(); _motor.Intent.CommitFrame(); _motor.Intent.Parked = false;
            Physics.SyncTransforms();
            Assert.IsTrue(_shoe.IsGrabbableIgnoringReach(_motor));
            Assert.Less(Vector3.Distance(_body.transform.position, _shoeBody.transform.position), Balance.PickupRadius);
        }
        [UnityTearDown] public IEnumerator After()
        {
            foreach (var go in new[] { _body, _shoeBody, _barrier, _occluderBody }) if (go != null) Object.Destroy(go);
            yield return null;
            typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            NetAuthority.Provider = _provider; SceneFlow.Networked = _network;
            GameLaunch.TrainingRange = _training; GameLaunch.GuidedTutorial = _tutorial;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots;
            Hitstop.End(); PresentationClock.RequestScale(1); yield return PlayModeWorld.Reset();
        }
        private Collider Barrier(float height, bool trigger = false)
        {
            _barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _barrier.name = trigger ? "Pickup trigger curtain" : height < .2f ? "Pickup low kerb" : "Pickup tall solid wall";
            _barrier.transform.position = _body.transform.position + Vector3.forward * .6f + Vector3.up * (height * .5f);
            _barrier.transform.localScale = new Vector3(2, height, .1f);
            var collider = _barrier.GetComponent<Collider>(); collider.isTrigger = trigger;
            Physics.SyncTransforms(); return collider;
        }
        private void Allowed()
        {
            Assert.IsTrue(_shoe.CanBeGrabbedBy(_motor)); Assert.IsTrue(_shoe.HostGrab(_motor));
            Assert.AreSame(_shoe, _carrier.Held); Assert.AreSame(_motor, _shoe.Holder);
            Assert.AreEqual(SlipperState.Held, _shoe.State); Assert.IsTrue(_motor.HoldingSlipper);
        }
        private Collider BodyBetween()
        {
            _occluderBody = new GameObject("Another player in front of the pickup wall");
            var other = _occluderBody.AddComponent<CharacterMotor>(); other.enabled = false;
            other.PlayerSlot = 2; other.Mode = GameMode.Classic; other.RoundActive = true;
            var capsule = _occluderBody.GetComponent<CharacterController>();
            capsule.center = Vector3.up * .5f; capsule.height = 1; capsule.radius = .12f;
            _occluderBody.transform.position = _body.transform.position + Vector3.forward * .3f;
            Physics.SyncTransforms(); return capsule;
        }
        [TestCase(false), TestCase(true)] public void AManualPickupCannotReachThroughATallSolidWall(bool bodyBeforeWall)
        {
            var wall = Barrier(4);
            Collider first = bodyBeforeWall ? BodyBetween() : wall;
            Assert.IsTrue(Physics.Linecast(ReachFrom, ReachTo, out var hit, ~0, QueryTriggerInteraction.Ignore));
            Assert.AreSame(first, hit.collider, "The expected first collider must actually obstruct the reach line.");
            Vector3 ray = ReachTo - ReachFrom;
            var hits = Physics.RaycastAll(ReachFrom, ray.normalized, ray.magnitude, ~0, QueryTriggerInteraction.Ignore);
            Assert.IsTrue(System.Array.Exists(hits, h => h.collider == wall), "The solid wall must remain behind any body occluder.");
            Assert.IsTrue(_shoe.IsGrabbableIgnoringReach(_motor));
            bool eligible = _shoe.CanBeGrabbedBy(_motor), grabbed = _shoe.HostGrab(_motor);
            Assert.IsFalse(eligible, "The pickup prompt offers a shoe through a tall solid wall.");
            Assert.IsFalse(grabbed, "The host picked up a shoe through a tall solid wall.");
            Assert.AreEqual(SlipperState.Loose, _shoe.State); Assert.IsNull(_carrier.Held);
            Assert.IsNull(_shoe.Holder); Assert.IsFalse(_motor.HoldingSlipper);
        }
        [Test] public void AnUnobstructedManualPickupStillConnects()
        {
            Assert.IsFalse(Physics.Linecast(ReachFrom, ReachTo, ~0, QueryTriggerInteraction.Ignore)); Allowed();
        }
        [Test] public void ALowKerbDoesNotBlockTheManualReach()
        {
            Barrier(.1f);
            Assert.IsFalse(Physics.Linecast(ReachFrom, ReachTo, ~0, QueryTriggerInteraction.Ignore)); Allowed();
        }
        [Test] public void AnotherPlayerDoesNotBlockAnOtherwiseClearManualReach()
        {
            var other = BodyBetween();
            Assert.IsTrue(Physics.Linecast(ReachFrom, ReachTo, out var hit, ~0, QueryTriggerInteraction.Ignore));
            Assert.AreSame(other, hit.collider); Allowed();
        }
        [Test] public void ATriggerCurtainDoesNotBlockTheManualReach()
        {
            var curtain = Barrier(4, true);
            Assert.IsTrue(Physics.Linecast(ReachFrom, ReachTo, out var hit, ~0, QueryTriggerInteraction.Collide));
            Assert.AreSame(curtain, hit.collider);
            Assert.IsFalse(Physics.Linecast(ReachFrom, ReachTo, ~0, QueryTriggerInteraction.Ignore)); Allowed();
        }
    }
}
