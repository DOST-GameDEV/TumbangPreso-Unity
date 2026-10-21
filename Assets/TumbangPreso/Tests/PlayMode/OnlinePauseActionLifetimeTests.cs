using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Abilities;
using TumbangPreso.Net;
using Unity.Collections;
using UnityEngine.SceneManagement;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class OnlinePauseActionLifetimeTests
    {
        private sealed class Peer : INetProvider
        {
            private readonly bool _host;
            public Peer(bool host = true) { _host = host; }
            public bool IsHost => _host; public bool IsNetworked => true;
            public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false;
        }
        private sealed class Counter : HeroAbility
        {
            public int Activations;
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            public Counter() : base("pause-hold", "Counter", "", 10, duration: 0, charges: 3)
            { AimByHolding(1, 3, maxHoldSeconds: 0); }
            protected override void OnActivate(AbilityContext context) => Activations++;
        }
        private sealed class Kit : HeroKit
        {
            public Counter Hold => (Counter)Skill1;
            public Kit() : base("pause-probe", "Counter") { Skill1 = new Counter(); }
        }
        private GameObject _menuOwner;
        private PausePanel _panel;
        private HeroAbilitySystem _system;
        private Kit _kit;
        private bool _pinned;
        private CustomRules _rules;
        private GameObject _body, _shoeBody, _canBody, _rangeBody, _localBody;
        private CharacterMotor _motor;
        private Carrier _carrier;
        private CombatVerbs _verbs;
        private PlayerInputReader _reader;
        private Slipper _shoe;
        private INetProvider _provider;
        private MatchStatsCollector _stats;
        private Lata _previousCan;
        private bool _touch, _network, _training, _tutorial, _spectator, _allBots, _sandbox;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private float Contact => (float)typeof(CombatVerbs).GetField("_lungeActiveLeft", Private).GetValue(_verbs);

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; NetAuthority.Provider = new SoloProvider();
            _touch = TouchInput.Active; TouchInput.ReleaseAll(); TouchInput.Active = true;
            _network = SceneFlow.Networked; SceneFlow.Networked = false;
            _training = GameLaunch.TrainingRange; _tutorial = GameLaunch.GuidedTutorial;
            _spectator = GameLaunch.Spectator; _allBots = GameLaunch.AllBots;
            _sandbox = PracticeSandbox.Wanted;
            PracticeSandbox.Clear(); Hitstop.End();
            _pinned = SceneFlow.RulesPinned; _rules = SceneFlow.SelectedRules.Clone();
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            SceneManager.SetActiveScene(SceneManager.CreateScene(SceneFlow.Eskinita));
            GameLaunch.TrainingRange = GameLaunch.GuidedTutorial = GameLaunch.Spectator = GameLaunch.AllBots = false;
            PresentationClock.RequestScale(1); GameServices.Ensure();
            _stats = GameServices.Stats; typeof(GameServices).GetProperty("Stats").SetValue(null, null);
            _previousCan = GameServices.Round.Lata;
            _canBody = new GameObject("Input cancellation can");
            var can = _canBody.AddComponent<Lata>(); can.enabled = false;
            GameServices.Round.Lata = can; GameServices.Round.BeginRound();
            _body = new GameObject("Input cancellation owner");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 1; _motor.IsDefender = false; _motor.RoundActive = true;
            _motor.Mode = GameMode.Classic;
            _body.transform.position = Vector3.back * (Balance.ConfinementRadius + 1);
            _carrier = _body.AddComponent<Carrier>(); _verbs = _body.AddComponent<CombatVerbs>();
            _shoeBody = new GameObject("Input cancellation owned shoe");
            _shoe = _shoeBody.AddComponent<Slipper>(); _shoe.enabled = false; _shoe.OwnerSlot = 1;
            Assert.IsTrue(_shoe.HostForceEquip(_motor));
            _reader = _body.AddComponent<PlayerInputReader>();
            yield return null;
            Assert.IsTrue(_reader.enabled); Assert.IsTrue(_motor.CanAct());
        }

        [UnityTearDown] public IEnumerator After()
        {
            if (_menuOwner != null) Object.Destroy(_menuOwner);
            if (_rangeBody != null) Object.Destroy(_rangeBody);
            if (_localBody != null) Object.Destroy(_localBody);
            if (_body != null) Object.Destroy(_body);
            if (_shoeBody != null) Object.Destroy(_shoeBody);
            if (_canBody != null) Object.Destroy(_canBody);
            yield return null;
            GameServices.Round.Lata = _previousCan;
            typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            TouchInput.ReleaseAll(); TouchInput.Active = _touch;
            NetAuthority.Provider = _provider; SceneFlow.Networked = _network;
            GameLaunch.TrainingRange = _training; GameLaunch.GuidedTutorial = _tutorial;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots;
            PracticeSandbox.Wanted = _sandbox;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
            PresentationClock.RequestScale(1);
            yield return PlayModeWorld.Reset();
        }

        private IEnumerator Windup(bool lunge)
        {
            _motor.IsDefender = lunge;
            _motor.Intent.AimPoint = Vector3.zero;
            TouchInput.Set(lunge ? Verb.Lunge : Verb.SpecialAbility, true);
            float until = Time.realtimeSinceStartup + 3;
            while ((lunge ? _verbs.ObservedLungeCharge < 0 : !_carrier.IsCharging)
                && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(lunge ? _verbs.ObservedLungeCharge >= 0 : _carrier.IsCharging,
                "Actual touch input did not begin the pending windup.");
            Assert.AreSame(_shoe, _carrier.Held); Assert.Zero(Contact);
            Assert.Zero(_verbs.LungeCooldownLeft);
        }

        private void PrepareMenu(bool online = true)
        {
            SceneFlow.Networked = online;
            NetAuthority.Provider = online ? new Peer() : new SoloProvider();
            _menuOwner = new GameObject("Pause lifetime owner");
            var watcher = _menuOwner.AddComponent<PauseWatcher>(); watcher.enabled = false; watcher.Local = _motor;
            var child = new GameObject("Prepared pause lifetime panel"); child.transform.SetParent(_menuOwner.transform);
            child.SetActive(false); _panel = child.AddComponent<PausePanel>(); _panel.Local = _motor;
            typeof(Panel).GetMethod("Prepare", Private).Invoke(_panel, null);
        }
        private void Open()
        {
            Assert.AreSame(_panel, Panel.Open<PausePanel>(_menuOwner.GetComponent<PauseWatcher>()));
            Assert.IsTrue(_motor.Intent.Parked);
        }
        private IEnumerator Pending(bool lunge)
        {
            yield return Windup(lunge); PrepareMenu(); Open();
            Assert.IsFalse(_carrier.IsCharging, "Opening a live menu must retire a pending throw synchronously.");
            Assert.AreEqual(-1, _verbs.ObservedLungeCharge, "Opening a live menu must retire a pending lunge synchronously.");
            Assert.AreEqual(1, PresentationClock.RequestedScale);
            yield return null;
            Assert.AreSame(_shoe, _carrier.Held); Assert.Zero(Contact); Assert.Zero(_verbs.LungeCooldownLeft);
        }
        [UnityTest] public IEnumerator OnlineMenuCancelsPendingThrowBeforeParking() => Pending(false);
        [UnityTest] public IEnumerator OnlineMenuCancelsPendingLungeBeforeParking() => Pending(true);
        [UnityTest] public IEnumerator OnlineMenuPreservesCommittedLungeAndCooldown()
        {
            _motor.IsDefender = true;
            Assert.IsTrue(_verbs.HostResolveLunge(_body.transform.position, Vector3.forward, 1));
            float contact = Contact, cooldown = _verbs.LungeCooldownLeft;
            PrepareMenu(); Open();
            Assert.AreEqual(contact, Contact); Assert.AreEqual(cooldown, _verbs.LungeCooldownLeft);
            Assert.AreEqual(1, PresentationClock.RequestedScale); yield return null;
        }
        [UnityTest] public IEnumerator OfflineMenuRestoresTheRequestedSpeedOnClose()
        {
            PresentationClock.RequestScale(.75f); PrepareMenu(false); Open();
            Assert.Zero(PresentationClock.RequestedScale); _panel.Close();
            Assert.AreEqual(.75f, PresentationClock.RequestedScale); Assert.IsFalse(_motor.Intent.Parked);
            yield return null;
        }
        private void PrepareHero()
        {
            _motor.Mode = GameMode.HeroStrike;
            _system = _body.AddComponent<HeroAbilitySystem>(); _system.SpeaksAsHero = false;
            _kit = new Kit(); typeof(HeroAbilitySystem).GetProperty("Kit").SetValue(_system, _kit);
        }
        private void Read() => typeof(PlayerInputReader).GetMethod("Update", Private).Invoke(_reader, null);
        private void HeroStep() => typeof(HeroAbilitySystem).GetMethod("Update", Private).Invoke(_system, null);
        [UnityTest] public IEnumerator OnlineMenuRetiresPendingHeroAimWithoutSpendingCharges()
        {
            PrepareHero(); TouchInput.Set(Verb.Skill1, true); Read(); HeroStep();
            Assert.IsTrue(_system.IsAiming(HeroAbilitySystem.Slot.Skill1));
            PrepareMenu(); Open();
            Assert.IsFalse(_system.IsAiming(HeroAbilitySystem.Slot.Skill1)); HeroStep();
            Assert.Zero(_kit.Hold.Activations); Assert.AreEqual(3, _kit.Hold.ChargesRemaining); yield return null;
        }
        [UnityTest] public IEnumerator StaleRemoteMenuOwnerPreservesReceivedHeroAimRenewal()
        {
            PrepareHero(); PrepareMenu(); _motor.PlayerSlot = 2; NetAuthority.Provider = new Peer(false);
            var aim = new AbilityAimSnapshot { Slot = 1, AbilityId = new FixedString64Bytes(_kit.Hold.Id), Held = .2f,
                Token = AbilityAimSnapshot.MakeToken(_motor.MovementEpoch, 7) };
            _system.ApplyNetworkAim(aim); Assert.IsTrue(_system.IsAiming(HeroAbilitySystem.Slot.Skill1));
            Open(); Assert.IsTrue(_system.IsAiming(HeroAbilitySystem.Slot.Skill1));
            aim.Held = .4f; _system.ApplyNetworkAim(aim);
            Assert.AreEqual(aim.Token, _system.CaptureAimPresentation().Token); yield return null;
        }
    }
}
