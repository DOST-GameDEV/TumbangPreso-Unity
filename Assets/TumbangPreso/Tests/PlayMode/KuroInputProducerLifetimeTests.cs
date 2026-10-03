using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class KuroInputProducerLifetimeTests
    {
        private sealed class Solo : INetProvider
        {
            public bool IsHost => true; public bool IsNetworked => false;
            public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false;
        }
        private sealed class OtherLocalSeat : INetProvider
        {
            public bool IsHost => false; public bool IsNetworked => true;
            public int LocalSlot => 2; public int LocalPeerId => 2; public bool IsSeatlessReferee => false;
        }
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _body, _floor, _petRoot;
        private CharacterMotor _motor;
        private PlayerInputReader _reader;
        private GhostPetCompanion _pet;
        private INetProvider _provider;
        private bool _touch, _typing, _network, _training, _tutorial, _spectator, _allBots, _sandbox;
        private MatchStatsCollector _stats;
        private Vector2 CachedMove => (Vector2)typeof(GhostPetCompanion).GetField("_playerInput", Hidden).GetValue(_pet);

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            Assert.IsTrue(Application.isPlaying); Assert.IsFalse(UI.Hub.HubLoading.Visible);
            _provider = NetAuthority.Provider; NetAuthority.Provider = new Solo();
            _touch = TouchInput.Active; TouchInput.ReleaseAll(); TouchInput.Active = true;
            _typing = LobbyChat.AnyTyping; Typing(false);
            _network = SceneFlow.Networked; SceneFlow.Networked = false;
            _training = GameLaunch.TrainingRange; _tutorial = GameLaunch.GuidedTutorial;
            _spectator = GameLaunch.Spectator; _allBots = GameLaunch.AllBots; _sandbox = PracticeSandbox.Wanted;
            GameLaunch.TrainingRange = GameLaunch.GuidedTutorial = GameLaunch.Spectator = GameLaunch.AllBots = false;
            PracticeSandbox.Clear(); Hitstop.End(); PresentationClock.RequestScale(1);
            SceneManager.SetActiveScene(SceneManager.CreateScene(SceneFlow.Eskinita)); GameServices.Ensure();
            _stats = GameServices.Stats; typeof(GameServices).GetProperty("Stats").SetValue(null, null);
            GameServices.Round.BeginRound();
            _floor = GameObject.CreatePrimitive(PrimitiveType.Cube); _floor.name = "Kuro input isolation floor";
            _floor.transform.position = Vector3.down * .05f; _floor.transform.localScale = new Vector3(20, .1f, 20);
            _body = new GameObject("Kuro local input owner"); _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 1; _motor.Mode = GameMode.HeroStrike; _motor.IsDefender = false; _motor.RoundActive = true;
            _body.transform.position = Vector3.back * (Balance.ConfinementRadius + 1);
            _motor.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "nemu");
            var system = _body.AddComponent<HeroAbilitySystem>(); system.enabled = false; system.BindHero("nemu");
            var visual = _body.AddComponent<CharacterVisual>(); visual.enabled = false;
            var art = RosterBook.Load().FindPersonArt("nemu"); Assert.IsNotNull(art.PetModel);
            _petRoot = Object.Instantiate(art.PetModel); _pet = _petRoot.GetComponent<GhostPetCompanion>() ?? _petRoot.AddComponent<GhostPetCompanion>();
            _pet.Bind(_body.transform, Vector3.zero, CharacterVisual.PersonScale); _pet.enabled = false;
            typeof(CharacterVisual).GetProperty("Companion").SetValue(visual, _pet);
            _petRoot.transform.SetPositionAndRotation(Vector3.up * .9f, Quaternion.identity);
            Physics.SyncTransforms(); _pet.BeginPossession(_motor);
            var brain = _body.GetComponent<AIController>(); if (brain != null) brain.enabled = false;
            _reader = _body.AddComponent<PlayerInputReader>(); yield return null;
            Assert.IsTrue(_pet.IsPossessed); Assert.IsTrue(_reader.enabled); Assert.IsTrue(_motor.IsLocallySimulated());
            Assert.IsFalse(_motor.Intent.Parked); Assert.IsFalse(PresentationClock.BlocksInput);
        }
        [UnityTearDown] public IEnumerator After()
        {
            UI.Hub.HubLoading.Cancel(); _pet?.EndPossession(false);
            foreach (var go in new[] { _body, _floor, _petRoot }) if (go != null) Object.Destroy(go);
            yield return null;
            typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            TouchInput.ReleaseAll(); TouchInput.Active = _touch; Typing(_typing); NetAuthority.Provider = _provider;
            SceneFlow.Networked = _network; GameLaunch.TrainingRange = _training; GameLaunch.GuidedTutorial = _tutorial;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots; PracticeSandbox.Wanted = _sandbox;
            Hitstop.End(); PresentationClock.RequestScale(1); yield return PlayModeWorld.Reset();
        }
        private static void Typing(bool value) => typeof(LobbyChat).GetProperty("AnyTyping").SetValue(null, value);
        private void Read() => typeof(PlayerInputReader).GetMethod("Update", Hidden).Invoke(_reader, null);
        private void Move() => typeof(GhostPetCompanion).GetMethod("UpdatePossession", Hidden).Invoke(_pet, new object[] { .05f, Time.time });
        private float Travel()
        {
            Vector3 before = _petRoot.transform.position; Move(); Vector3 delta = _petRoot.transform.position - before;
            delta.y = 0; return delta.magnitude;
        }
        private void Drive()
        {
            TouchInput.Move = Vector2.up; Read(); Assert.AreEqual(Vector2.up, CachedMove);
            Assert.Greater(Travel(), .02f, "The actual public possession could not move before its input was withdrawn.");
        }
        private void Stopped()
        {
            Assert.Less(Travel(), .0001f, "The withdrawn or parked reader left Kuro moving on cached player input.");
            Assert.AreEqual(Vector2.zero, CachedMove); Assert.IsTrue(_pet.IsPossessed, "Input retirement ended the committed possession.");
        }
        [Test] public void ChatWithdrawalRetiresPossessedMovement()
        {
            Drive(); Typing(true); Read(); Stopped();
        }
        [Test] public void LoadingWithdrawalRetiresPossessedMovement()
        {
            Drive(); Assert.IsFalse(UI.Hub.HubLoading.Begin(SceneFlow.Eskinita, externallyLoaded: true));
            Assert.IsTrue(UI.Hub.HubLoading.Visible); Read(); Stopped();
        }
        [Test] public void ReaderDisableRetiresPossessedMovement()
        {
            Drive(); _reader.enabled = false; Stopped();
        }
        [Test] public void FocusLossRetiresPossessedMovement()
        {
            Drive(); _body.SendMessage("OnApplicationFocus", false, SendMessageOptions.DontRequireReceiver); Stopped();
        }
        [Test] public void ParkedReaderDoesNotDriveThePossessedFamiliar()
        {
            Drive(); _motor.Intent.Parked = true; Read(); Stopped(); Assert.IsTrue(_motor.Intent.Parked);
        }
        [Test] public void OrdinaryTouchMovementStillDrivesThePossessedFamiliar()
        {
            Drive(); Assert.Greater(Travel(), .02f); Assert.IsTrue(_pet.IsPossessed);
        }
        [Test] public void FreshMovementAfterChatStillDrivesTheSamePossession()
        {
            Drive(); Typing(true); Read(); Typing(false); TouchInput.Move = Vector2.right; Read();
            Assert.AreEqual(Vector2.right, CachedMove); Assert.Greater(Travel(), .02f); Assert.IsTrue(_pet.IsPossessed);
        }
        [Test] public void AnObsoleteReaderCannotClearAnotherSeatsFamiliarCache()
        {
            Drive(); NetAuthority.Provider = new OtherLocalSeat(); _reader.enabled = false;
            Assert.AreEqual(Vector2.up, CachedMove); Assert.IsTrue(_pet.IsPossessed);
        }
    }
}
