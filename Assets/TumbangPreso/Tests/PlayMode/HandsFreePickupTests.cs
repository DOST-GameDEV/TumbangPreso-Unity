using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.CameraSystem;
using TumbangPreso.Visual;
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
            Assert.Greater(_carrier.PickupBend, .5f, "The dwell must be spent reaching down before the shoe is equipped.");
            Assert.AreEqual(0.6f, _carrier.AutoPickupRatio, 0.01f, "The HUD bar must show how far the dwell has run.");
            Dwell(Balance.AutoPickupSeconds * 0.5f);
            Assert.AreSame(_shoe, _carrier.Held, "Standing on your own shoe for the dwell did not collect it.");
            Assert.AreEqual(0.0f, _carrier.AutoPickupRatio, "A finished pickup must clear the bar.");
            Assert.Greater(_carrier.PickupBend, .95f, "The lift must continue from the completed downward reach.");
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

        [UnityTest] public IEnumerator TheDwellLowersTheViewBeforePossessionAndTheShoeLiftsFromTheStreet()
        {
            var cameraBody = new GameObject("Pickup motion eyes");
            var camera = cameraBody.AddComponent<Camera>(); camera.tag = "MainCamera";
            var rig = cameraBody.AddComponent<CameraRig>();
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            try
            {
                Settings.SettingsStore.Current.ReducedUiMotion = false;
                _shoeBody.transform.position = Vector3.forward * .6f;
                var art = RosterBook.Load().Slippers[0];
                if (art != null && art.Model != null) Object.Instantiate(art.Model, _shoeBody.transform);
                rig.Follow(_motor, true); rig.SetAimSource(AimSource.Movement);
                yield return null; yield return null;
                float standing = camera.transform.position.y;
                Dwell(.25f);
                yield return new WaitForSecondsRealtime(.15f);
                Assert.IsNull(_carrier.Held, "The downward motion must happen before the pickup succeeds.");
                Assert.Less(camera.transform.position.y, standing - .15f, "Auto pickup did not lower the actual camera.");
                Dwell(.24f);
                yield return new WaitForSecondsRealtime(.1f);
                Assert.IsNull(_carrier.Held);
                Assert.Less(camera.transform.position.y, standing - .4f, "The end of the dwell must feel like a crouch.");
                Dwell(.02f);
                yield return null;
                Assert.AreSame(_shoe, _carrier.Held);
                Assert.AreEqual(0, _carrier.PickupLift, .001f, "The first pickup frame snapped to the carrying hand.");
                var arms = camera.GetComponentInChildren<ViewmodelArms>(true);
                Assert.IsNotNull(arms);
                var drawnShoe = arms.transform.Find("RightPivot/Arm/HeldSlipper");
                Assert.IsNotNull(drawnShoe);
                Vector3 from = drawnShoe.position;
                float until = Time.realtimeSinceStartup + .5f;
                while (Time.realtimeSinceStartup < until)
                {
                    typeof(Carrier).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_carrier, null);
                    yield return null;
                }
                Assert.Greater(Vector3.Distance(from, drawnShoe.position), .15f, "The first-person slipper did not travel through the lift.");
                Assert.AreEqual(0, _carrier.PickupBend);
                Assert.Greater(camera.transform.position.y, standing - .05f, "The eye did not rise after collecting.");
            }
            finally
            {
                Settings.SettingsStore.Current.ReducedUiMotion = reduced;
                Object.Destroy(cameraBody);
            }
        }

        [UnityTest] public IEnumerator LeavingDuringTheReachReturnsUprightWithoutEquippingTheShoe()
        {
            var cameraBody = new GameObject("Interrupted pickup eyes");
            var camera = cameraBody.AddComponent<Camera>(); camera.tag = "MainCamera";
            var rig = cameraBody.AddComponent<CameraRig>();
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            try
            {
                Settings.SettingsStore.Current.ReducedUiMotion = false;
                _shoeBody.transform.position = Vector3.forward * .6f;
                Vector3 ground = _shoeBody.transform.position;
                rig.Follow(_motor, true); rig.SetAimSource(AimSource.Movement);
                yield return null; yield return null;
                float standing = camera.transform.position.y;
                Dwell(.4f); yield return new WaitForSecondsRealtime(.12f);
                Assert.Less(camera.transform.position.y, standing - .35f);
                _motor.transform.position += Vector3.right * 2;
                Dwell(.01f);
                Assert.IsNull(_carrier.Held);
                Assert.AreEqual(0, _carrier.AutoPickupRatio);
                Assert.AreEqual(ground, _carrier.PickupGround, "The retreating hand lost the shoe's location.");
                yield return new WaitForSecondsRealtime(.2f);
                Assert.Greater(camera.transform.position.y, standing - .05f);
                Assert.AreEqual(SlipperState.Loose, _shoe.State);
            }
            finally
            {
                Settings.SettingsStore.Current.ReducedUiMotion = reduced;
                Object.Destroy(cameraBody);
            }
        }
    }
}
