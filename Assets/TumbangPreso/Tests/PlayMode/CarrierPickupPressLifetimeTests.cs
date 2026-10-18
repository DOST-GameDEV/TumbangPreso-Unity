using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class CarrierPickupPressLifetimeTests
    {
        private sealed class Solo : INetProvider
        {
            public bool IsHost => true; public bool IsNetworked => false;
            public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false;
        }
        private INetProvider _provider;
        private GameObject _body, _shoeBody;
        private CharacterMotor _motor;
        private Carrier _carrier;
        private Slipper _shoe;
        private MatchStatsCollector _stats;
        private bool _network, _training, _tutorial, _spectator, _allBots;

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
            _body = new GameObject("Pickup press lifetime actor");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 1; _motor.Mode = GameMode.Classic; _motor.IsDefender = false; _motor.RoundActive = true;
            _carrier = _body.AddComponent<Carrier>(); _carrier.enabled = false;
            _shoeBody = new GameObject("Pickup press owned shoe"); _shoe = _shoeBody.AddComponent<Slipper>(); _shoe.enabled = false;
            _shoe.OwnerSlot = _shoe.SeatOfOrigin = 1; _shoeBody.transform.position = Vector3.right * .2f;
            _motor.Intent.Clear(); _motor.Intent.CommitFrame(); _motor.Intent.Parked = false;
            Assert.IsTrue(_shoe.CanBeGrabbedBy(_motor));
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_body != null) Object.Destroy(_body); if (_shoeBody != null) Object.Destroy(_shoeBody);
            yield return null;
            typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            NetAuthority.Provider = _provider; SceneFlow.Networked = _network;
            GameLaunch.TrainingRange = _training; GameLaunch.GuidedTutorial = _tutorial;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots;
            Hitstop.End(); PresentationClock.RequestScale(1); yield return PlayModeWorld.Reset();
        }
        private MatchStatsCollector InstallAccounting()
        {
            var stats = _body.AddComponent<MatchStatsCollector>(); stats.enabled = false;
            typeof(GameServices).GetProperty("Stats").SetValue(null, stats);
            typeof(MatchStatsCollector).GetMethod("BeginMatch", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(stats, null);
            return stats;
        }
        private PlayerMatchStats AccountingLine(MatchStatsCollector stats)
            => ((PlayerMatchStats[])typeof(MatchStatsCollector).GetField("_lines", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(stats))[1];

        [Test] public void UnthrownOwnPickupDoesNotCreateAnImpossibleRetrievalRecord()
        {
            var stats = InstallAccounting();
            Assert.IsTrue(_shoe.HostGrab(_motor));
            Assert.AreSame(_shoe, _carrier.Held, "The pickup itself must still succeed.");
            _carrier.NotifyHolding(_shoe);
            Assert.AreEqual(0, AccountingLine(stats).Throws);
            Assert.AreEqual(0, AccountingLine(stats).Retrievals,
                "An unthrown own-shoe pickup created retrievals greater than throws.");
        }
        [Test] public void OwnThrownShoePickupCountsOnceButItsDroppedRegrabDoesNot()
        {
            var stats = InstallAccounting();
            _shoe.HostForceEquip(_motor);
            stats.NoteThrow(1);
            _shoe.HostThrow(_motor, _motor.transform.position + Vector3.right * .2f, Vector3.zero);
            // This stands in for landing; preserve the real held-release episode.
            typeof(Slipper).GetMethod("SetState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_shoe, new object[] { SlipperState.Loose });
            _shoe.transform.position = _motor.transform.position + Vector3.right * .2f;
            Assert.IsTrue(_shoe.HostGrab(_motor)); _carrier.NotifyHolding(_shoe);
            Assert.AreEqual(1, AccountingLine(stats).Retrievals, "A real own throw must still count on retrieval.");
            Assert.IsTrue(_shoe.HostDisarm());
            Assert.IsTrue(_shoe.HostGrab(_motor)); _carrier.NotifyHolding(_shoe);
            Assert.AreEqual(1, AccountingLine(stats).Retrievals, "Dropping and re-grabbing that shoe granted a second retrieval.");
        }
        [Test] public void UnownedSparePickupRemainsAvailableWithoutRetrievalCredit()
        {
            // Ownerless equipment is an explicit one-human training exception.
            GameLaunch.TrainingRange = true;
            GameServices.Round.Register(_motor);
            var stats = InstallAccounting(); _shoe.OwnerSlot = -1; _shoe.SeatOfOrigin = 2;
            Assert.IsTrue(_shoe.HostGrab(_motor));
            Assert.AreSame(_shoe, _carrier.Held);
            Assert.AreEqual(0, AccountingLine(stats).Retrievals);
        }

        private void Step() => typeof(Carrier).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_carrier, null);
        private void FirstPickupAndHandLoss()
        {
            _motor.Intent.Set(Verb.Grab, true); Step(); Assert.AreSame(_shoe, _carrier.Held);
            Assert.IsTrue(_carrier.IsBusy); Assert.IsTrue(_motor.Intent.JustPressed(Verb.Grab));
            Assert.IsTrue(_shoe.HostDisarm());
            Assert.IsNull(_carrier.Held); Assert.IsTrue(_shoe.CanBeGrabbedBy(_motor), "The disarmed shoe must still be eligible and in reach.");
        }
        [Test] public void ASuccessfulPickupCannotReuseItsPressAfterSameFrameHandLoss()
        {
            FirstPickupAndHandLoss(); Step();
            Assert.IsNull(_carrier.Held, "One successful pickup edge was reused after synchronous hand loss.");
            Assert.AreEqual(SlipperState.Loose, _shoe.State); Assert.IsTrue(_motor.Intent.JustPressed(Verb.Grab));
        }
        [Test] public void AReleaseAndFreshPressCanRetrieveTheDisarmedShoe()
        {
            FirstPickupAndHandLoss(); _motor.Intent.Set(Verb.Grab, false); Step(); _motor.Intent.CommitFrame();
            _motor.Intent.Set(Verb.Grab, true); Step();
            Assert.AreSame(_shoe, _carrier.Held); Assert.AreEqual(SlipperState.Held, _shoe.State);
        }
        [Test] public void ARefusedPickupDoesNotConsumeThePressBeforeAnEligibleTargetArrives()
        {
            _shoeBody.transform.position = Vector3.right * (Balance.PickupRadius + 1);
            _motor.Intent.Set(Verb.Grab, true); Step(); Assert.IsNull(_carrier.Held); Assert.IsFalse(_carrier.IsBusy);
            _shoeBody.transform.position = Vector3.right * .2f; Assert.IsTrue(_shoe.CanBeGrabbedBy(_motor));
            Step(); Assert.AreSame(_shoe, _carrier.Held); Assert.IsTrue(_carrier.IsBusy);
        }
    }
}
