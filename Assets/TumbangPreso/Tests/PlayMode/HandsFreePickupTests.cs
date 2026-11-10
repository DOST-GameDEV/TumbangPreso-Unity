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
    public sealed class HandsFreePickupTests
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
            _body = new GameObject("Hands-free pickup actor");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 1; _motor.Mode = GameMode.Classic; _motor.IsDefender = false; _motor.RoundActive = true;
            _carrier = _body.AddComponent<Carrier>(); _carrier.enabled = false;
            _shoeBody = new GameObject("Hands-free owned shoe"); _shoe = _shoeBody.AddComponent<Slipper>(); _shoe.enabled = false;
            _shoe.OwnerSlot = _shoe.SeatOfOrigin = 1;
            _motor.Intent.Clear(); _motor.Intent.CommitFrame(); _motor.Intent.Parked = false;
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

        [Test] public void AnEligibleSlipperRequiresGrabEvenWhenStandingOrSlidingOverIt()
        {
            _shoeBody.transform.position = Vector3.right * .2f;
            Assert.IsTrue(_shoe.CanBeGrabbedBy(_motor), "Fixture: the shoe must be a legal pickup.");
            const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
            var step = typeof(Carrier).GetMethod("StepAttacker", instance);
            void Tick(float seconds) => step.Invoke(_carrier, new object[] { seconds });

            Tick(1f);
            Assert.IsNull(_carrier.Held, "Standing over the shoe must leave it loose.");
            Assert.AreEqual(0, _carrier.PickupBend);

            typeof(CharacterMotor).GetField("_rwSliding", instance).SetValue(_motor, true);
            Tick(.02f);
            Assert.IsNull(_carrier.Held, "Sliding over the shoe must leave it loose.");

            _motor.Intent.Set(Verb.Grab, true);
            Tick(.02f);
            Assert.AreSame(_shoe, _carrier.Held, "Grab must still work while sliding.");
            Assert.AreEqual(SlipperState.Held, _shoe.State);
        }
    }
}
