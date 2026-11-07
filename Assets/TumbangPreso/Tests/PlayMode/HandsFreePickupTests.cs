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
    /// <summary>
    /// The hands-free retrieval (owner, 2026-11-07, from the external feedback document): standing on a tsinelas
    /// you may take collects it after `Balance.AutoPickupSeconds`, walking past it does not, and the taya never
    /// collects anything. The press stays the instant route and is covered by `CarrierPickupPressLifetimeTests`.
    /// </summary>
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

        private void Dwell(float seconds)
            => typeof(Carrier).GetMethod("StepHandsFreePickup", BindingFlags.Instance | BindingFlags.NonPublic)
                              .Invoke(_carrier, new object[] { seconds });

        private void PlaceShoe(float metres)
        {
            _shoeBody.transform.position = Vector3.right * metres;
            Assert.IsTrue(_shoe.CanBeGrabbedBy(_motor), "Fixture: the shoe must be a legal pickup.");
        }

        [Test] public void StandingOnYourShoeCollectsItAfterTheDwell()
        {
            PlaceShoe(0.2f);
            Dwell(Balance.AutoPickupSeconds * 0.6f);
            Assert.IsNull(_carrier.Held, "The hands-free pickup fired before its dwell.");
            Assert.AreEqual(0.6f, _carrier.AutoPickupRatio, 0.01f, "The HUD bar must show how far the dwell has run.");
            Dwell(Balance.AutoPickupSeconds * 0.5f);
            Assert.AreSame(_shoe, _carrier.Held, "Standing on your own shoe for the dwell did not collect it.");
            Assert.AreEqual(0.0f, _carrier.AutoPickupRatio, "A finished pickup must clear the bar.");
        }

        [Test] public void InPressReachButOutsideTheStandingRadiusNeverCollects()
        {
            Assume.That(Balance.AutoPickupRadius, Is.LessThan(Balance.PickupRadius - 0.2f));
            PlaceShoe(Balance.AutoPickupRadius + 0.2f);
            Dwell(Balance.AutoPickupSeconds * 3.0f);
            Assert.IsNull(_carrier.Held, "Walking past a shoe inside press reach collected it with no press.");
            Assert.AreEqual(0.0f, _carrier.AutoPickupRatio);
        }

        [Test] public void SteppingOffResetsTheDwell()
        {
            PlaceShoe(0.2f);
            Dwell(Balance.AutoPickupSeconds * 0.8f);
            _shoeBody.transform.position = Vector3.right * (Balance.AutoPickupRadius + 0.2f);
            Dwell(0.02f);
            Assert.AreEqual(0.0f, _carrier.AutoPickupRatio, "Leaving the shoe must drop the count, not pause it.");
            PlaceShoe(0.2f);
            Dwell(Balance.AutoPickupSeconds * 0.5f);
            Assert.IsNull(_carrier.Held, "A dwell resumed from where it stopped instead of starting again.");
        }

        [Test] public void TheTayaNeverCollectsHandsFree()
        {
            _shoeBody.transform.position = Vector3.right * 0.2f;
            _motor.IsDefender = true;
            Dwell(Balance.AutoPickupSeconds * 3.0f);
            Assert.IsNull(_carrier.Held, "The taya collected a tsinelas hands-free.");
        }
    }
}
