using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class ReaderLoadingLifetimeTests
    {
        private sealed class Solo : INetProvider
        {
            public bool IsHost => true; public bool IsNetworked => false;
            public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false;
        }
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private INetProvider _provider;
        private GameObject _body, _shoeBody, _canBody;
        private CharacterMotor _motor;
        private PlayerInputReader _reader;
        private Carrier _carrier;
        private CombatVerbs _verbs;
        private Slipper _shoe;
        private Lata _previousCan;
        private MatchStatsCollector _stats;
        private bool _touch, _network, _training, _tutorial, _spectator, _allBots, _sandbox;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            Assert.IsFalse(HubLoading.Visible); Assert.IsFalse(LobbyChat.AnyTyping);
            _provider = NetAuthority.Provider; NetAuthority.Provider = new Solo();
            _touch = TouchInput.Active; TouchInput.ReleaseAll(); TouchInput.Active = true;
            _network = SceneFlow.Networked; SceneFlow.Networked = false;
            _training = GameLaunch.TrainingRange; _tutorial = GameLaunch.GuidedTutorial;
            _spectator = GameLaunch.Spectator; _allBots = GameLaunch.AllBots; _sandbox = PracticeSandbox.Wanted;
            GameLaunch.TrainingRange = GameLaunch.GuidedTutorial = GameLaunch.Spectator = GameLaunch.AllBots = false;
            PracticeSandbox.Clear(); Hitstop.End(); PresentationClock.RequestScale(1);
            SceneManager.SetActiveScene(SceneManager.CreateScene(SceneFlow.Eskinita)); GameServices.Ensure();
            _stats = GameServices.Stats; typeof(GameServices).GetProperty("Stats").SetValue(null, null);
            _previousCan = GameServices.Round.Lata;
            _canBody = new GameObject("Loading lifetime can"); var can = _canBody.AddComponent<Lata>(); can.enabled = false;
            GameServices.Round.Lata = can; GameServices.Round.BeginRound();
            _body = new GameObject("Loading lifetime local reader");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 1; _motor.Mode = GameMode.Classic; _motor.IsDefender = false; _motor.RoundActive = true;
            _body.transform.position = Vector3.back * (Balance.ConfinementRadius + 1);
            _carrier = _body.AddComponent<Carrier>(); _carrier.enabled = false;
            _verbs = _body.AddComponent<CombatVerbs>(); _verbs.enabled = false;
            _shoeBody = new GameObject("Loading lifetime owned shoe"); _shoe = _shoeBody.AddComponent<Slipper>();
            _shoe.enabled = false; _shoe.OwnerSlot = _shoe.SeatOfOrigin = 1;
            Assert.IsTrue(_shoe.HostForceEquip(_motor));
            _reader = _body.AddComponent<PlayerInputReader>(); yield return null;
            Assert.IsTrue(_reader.enabled); Assert.IsTrue(_motor.CanAct());
        }
        [UnityTearDown] public IEnumerator After()
        {
            HubLoading.Cancel();
            foreach (var go in new[] { _body, _shoeBody, _canBody }) if (go != null) Object.Destroy(go);
            yield return null;
            Assert.IsFalse(HubLoading.Visible);
            GameServices.Round.Lata = _previousCan; typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            TouchInput.ReleaseAll(); TouchInput.Active = _touch; NetAuthority.Provider = _provider;
            SceneFlow.Networked = _network; GameLaunch.TrainingRange = _training; GameLaunch.GuidedTutorial = _tutorial;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots; PracticeSandbox.Wanted = _sandbox;
            Hitstop.End(); PresentationClock.RequestScale(1); yield return PlayModeWorld.Reset();
        }
        private static void Step(object target) => target.GetType().GetMethod("Update", Hidden).Invoke(target, null);
        private float Contact => (float)typeof(CombatVerbs).GetField("_lungeActiveLeft", Hidden).GetValue(_verbs);
        private void ShowCurtain()
        {
            // Use the actual public curtain. An external loader waits for a new scene;
            // this fixture deliberately performs no scene load and cancels its own wait.
            Assert.IsFalse(HubLoading.Begin(SceneFlow.Eskinita, externallyLoaded: true));
            Assert.IsTrue(HubLoading.Visible); Assert.IsTrue(_motor.CanAct());
            Assert.IsFalse(PresentationClock.BlocksInput); Assert.IsFalse(_motor.Intent.Parked);
        }
        private void Windup(bool lunge)
        {
            _motor.IsDefender = lunge;
            TouchInput.Set(lunge ? Verb.Lunge : Verb.SpecialAbility, true); Step(_reader);
            Step(lunge ? (object)_verbs : _carrier); Step(lunge ? (object)_verbs : _carrier);
            Assert.IsTrue(lunge ? _verbs.ObservedLungeCharge >= 0 : _carrier.IsCharging);
            Assert.AreSame(_shoe, _carrier.Held); Assert.Zero(Contact); Assert.Zero(_verbs.LungeCooldownLeft);
        }
        [Test] public void LoadingCurtainCancelsThePendingThrowBeforeARelease()
        {
            Windup(false); ShowCurtain(); Step(_reader); Step(_carrier);
            Assert.AreSame(_shoe, _carrier.Held, "Loading input withdrawal launched the pending throw.");
            Assert.AreEqual(SlipperState.Held, _shoe.State); Assert.IsFalse(_carrier.IsCharging);
            Assert.AreEqual(-1, _carrier.ObservedChargePower);
        }
        [Test] public void LoadingCurtainCancelsThePendingLungeBeforeARelease()
        {
            Windup(true); ShowCurtain(); Step(_reader); Step(_verbs);
            Assert.Zero(_verbs.LungeCooldownLeft, "Loading input withdrawal spent a new lunge cooldown.");
            Assert.Zero(Contact); Assert.AreEqual(-1, _verbs.ObservedLungeCharge);
        }
        [Test] public void LoadingCurtainPreservesAlreadyCommittedLungeContactAndCooldown()
        {
            _motor.IsDefender = true;
            Assert.IsTrue(_verbs.HostResolveLunge(_body.transform.position, Vector3.forward, 1));
            float contact = Contact, cooldown = _verbs.LungeCooldownLeft;
            Assert.Greater(contact, 0); Assert.Greater(cooldown, 0);
            ShowCurtain(); Step(_reader);
            Assert.AreEqual(contact, Contact); Assert.AreEqual(cooldown, _verbs.LungeCooldownLeft);
        }
        [UnityTest] public IEnumerator LoadingExitWaitsForReleaseAndAllowsAFreshPress()
        {
            ShowCurtain(); TouchInput.Set(Verb.SpecialAbility, true); Step(_reader); Step(_carrier);
            Assert.IsFalse(_carrier.IsCharging);
            HubLoading.Cancel(); Step(_reader); Step(_carrier); yield return null;
            Step(_reader); Step(_carrier); Assert.IsFalse(_carrier.IsCharging);
            Assert.AreSame(_shoe, _carrier.Held);
            TouchInput.Set(Verb.SpecialAbility, false); Step(_reader); Step(_carrier);
            TouchInput.Set(Verb.SpecialAbility, true); Step(_reader); Step(_carrier);
            Assert.IsTrue(_carrier.IsCharging, "The released loading button did not permit a fresh press.");
        }
    }
}
