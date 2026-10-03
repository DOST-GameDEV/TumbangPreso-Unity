using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class KuroPauseContactLifetimeTests
    {
        private sealed class Solo : INetProvider
        {
            public bool IsHost => true; public bool IsNetworked => false;
            public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false;
        }
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private INetProvider _provider;
        private GameObject _ownerBody, _victimBody, _petRoot, _floor;
        private CharacterMotor _owner, _victim;
        private GhostPetCompanion _pet;
        private MatchStatsCollector _stats;
        private bool _network, _training, _tutorial, _spectator, _allBots, _sandbox;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; NetAuthority.Provider = new Solo();
            _network = SceneFlow.Networked; SceneFlow.Networked = false;
            _training = GameLaunch.TrainingRange; _tutorial = GameLaunch.GuidedTutorial;
            _spectator = GameLaunch.Spectator; _allBots = GameLaunch.AllBots; _sandbox = PracticeSandbox.Wanted;
            GameLaunch.TrainingRange = GameLaunch.GuidedTutorial = GameLaunch.Spectator = GameLaunch.AllBots = false;
            PracticeSandbox.Clear(); Hitstop.End(); PresentationClock.RequestScale(1);
            SceneManager.SetActiveScene(SceneManager.CreateScene(SceneFlow.Eskinita)); GameServices.Ensure();
            _stats = GameServices.Stats; typeof(GameServices).GetProperty("Stats").SetValue(null, null);
            GameServices.Round.BeginRound();
            _floor = GameObject.CreatePrimitive(PrimitiveType.Cube); _floor.name = "Kuro contact clock floor";
            _floor.transform.position = Vector3.down * .05f; _floor.transform.localScale = new Vector3(20, .1f, 20);
            _ownerBody = new GameObject("Kuro contact owner"); _owner = _ownerBody.AddComponent<CharacterMotor>(); _owner.enabled = false;
            _owner.PlayerSlot = 1; _owner.Mode = GameMode.HeroStrike; _owner.RoundActive = true; _owner.IsDefender = false;
            _ownerBody.transform.position = Vector3.back * (Balance.ConfinementRadius + 1);
            _owner.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "nemu");
            var system = _ownerBody.AddComponent<HeroAbilitySystem>(); system.enabled = false; system.BindHero("nemu");
            var visual = _ownerBody.AddComponent<CharacterVisual>(); visual.enabled = false;
            var art = RosterBook.Load().FindPersonArt("nemu"); Assert.IsNotNull(art.PetModel);
            _petRoot = Object.Instantiate(art.PetModel); _pet = _petRoot.GetComponent<GhostPetCompanion>() ?? _petRoot.AddComponent<GhostPetCompanion>();
            _pet.Bind(_owner.transform, Vector3.zero, CharacterVisual.PersonScale); _pet.enabled = false;
            typeof(CharacterVisual).GetProperty("Companion").SetValue(visual, _pet);
            _petRoot.transform.SetPositionAndRotation(Vector3.up * .9f, Quaternion.identity);
            Physics.SyncTransforms(); _pet.BeginPossession(_owner); _pet.SetPlayerInput(Vector2.zero);
            var brain = _ownerBody.GetComponent<AIController>(); if (brain != null) brain.enabled = false;
            _victimBody = new GameObject("Kuro contact victim"); _victim = _victimBody.AddComponent<CharacterMotor>(); _victim.enabled = false;
            _victim.PlayerSlot = 2; _victim.Mode = GameMode.Classic; _victim.RoundActive = true; _victim.IsDefender = false;
            _victimBody.transform.position = Vector3.right * 4;
            GameServices.Round.Register(_owner); GameServices.Round.Register(_victim);
            yield return null;
            Assert.IsTrue(_pet.IsPossessed); Assert.IsFalse(_victim.IsStunned); Assert.Greater(Time.deltaTime, 0);
            Assert.IsFalse(PresentationClock.Held); Assert.Greater(PresentationClock.RequestedScale, 0);
        }
        [UnityTearDown] public IEnumerator After()
        {
            PresentationClock.RequestScale(1); _pet?.EndPossession(false);
            foreach (var go in new[] { _ownerBody, _victimBody, _petRoot, _floor }) if (go != null) Object.Destroy(go);
            yield return null;
            typeof(GameServices).GetProperty("Stats").SetValue(null, _stats); NetAuthority.Provider = _provider;
            SceneFlow.Networked = _network; GameLaunch.TrainingRange = _training; GameLaunch.GuidedTutorial = _tutorial;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots; PracticeSandbox.Wanted = _sandbox;
            Hitstop.End(); PresentationClock.RequestScale(1); yield return PlayModeWorld.Reset();
        }
        private void Near()
        {
            Vector3 at = _petRoot.transform.position; at.y = 0;
            _victimBody.transform.position = at + Vector3.right * .9f; Physics.SyncTransforms();
            Assert.IsFalse(_victim.IsStunned); Assert.AreSame(_victim, GameServices.Round.PlayerAt(2));
            Vector3 delta = _victimBody.transform.position - _petRoot.transform.position; delta.y = 0;
            Assert.Less(delta.magnitude, 1.6f);
        }
        private void Tick() => typeof(GhostPetCompanion).GetMethod("LateUpdate", Hidden).Invoke(_pet, null);

        [UnityTest] public IEnumerator OrdinaryPauseCannotApplyANewPossessionContactStagger()
        {
            Near(); Vector3 before = _petRoot.transform.position;
            PresentationClock.RequestScale(0); yield return null;
            Assert.Zero(Time.deltaTime); Assert.Zero(PresentationClock.RequestedScale); Assert.IsFalse(PresentationClock.Held);
            Assert.IsTrue(PresentationClock.BlocksInput); Tick();
            Assert.IsFalse(_victim.IsStunned, "A zero-speed possession tick applied new gameplay stagger during an ordinary pause.");
            Assert.Zero(_victim.StunLeft); Assert.IsTrue(_pet.IsPossessed);
            Vector3 moved = _petRoot.transform.position - before; moved.y = 0; Assert.Less(moved.magnitude, .0001f);
        }
        [Test] public void LivePossessionContactStillStaggersANearbyPlayer()
        {
            Near(); Tick(); Assert.IsTrue(_victim.IsStunned); Assert.Greater(_victim.StunLeft, .3f);
            Assert.IsTrue(_pet.IsPossessed);
        }
        [UnityTest] public IEnumerator ResumeKeepsThePossessionAndAllowsItsFirstLiveContact()
        {
            Near(); PresentationClock.RequestScale(0); yield return null;
            Assert.Zero(Time.deltaTime); Assert.IsTrue(_pet.IsPossessed); Assert.IsFalse(_victim.IsStunned);
            PresentationClock.RequestScale(1); yield return null;
            Assert.Greater(Time.deltaTime, 0); Assert.IsTrue(_pet.IsPossessed); Tick();
            Assert.IsTrue(_victim.IsStunned); Assert.Greater(_victim.StunLeft, .3f);
            Assert.IsTrue(_pet.IsPossessed);
        }
    }
}
