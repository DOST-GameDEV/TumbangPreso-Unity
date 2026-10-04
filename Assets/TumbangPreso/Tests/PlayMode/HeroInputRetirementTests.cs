using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using TumbangPreso.Net;
using TumbangPreso.UI;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class HeroInputRetirementTests
    {
        private enum Exit { Focus, Reader, PracticeIdle }
        private sealed class Counter : HeroAbility
        {
            public int Activations;
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            public Counter(bool hold) : base(hold ? "retirement-hold" : "retirement-instant", "Counter", "", 10,
                duration: hold ? 0 : 5, charges: hold ? 3 : 0)
            { if (hold) AimByHolding(1, 3, maxHoldSeconds: 0); }
            protected override void OnActivate(AbilityContext context) => Activations++;
        }
        private sealed class CounterKit : HeroKit
        {
            public Counter Hold => (Counter)Skill1;
            public Counter Instant => (Counter)Skill2;
            public CounterKit() : base("retirement-probe", "Counter")
            { Skill1 = new Counter(true); Skill2 = new Counter(false); }
        }
        private sealed class Solo : INetProvider
        {
            public bool IsHost => true; public bool IsNetworked => false;
            public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false;
        }
        private sealed class Observer : INetProvider
        {
            public bool IsHost => false; public bool IsNetworked => true;
            public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false;
        }
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private INetProvider _provider;
        private GameObject _body, _can, _rangeBody, _localBody;
        private CharacterMotor _motor;
        private PlayerInputReader _reader;
        private HeroAbilitySystem _system;
        private CounterKit _kit;
        private Lata _previousCan;
        private MatchStatsCollector _stats;
        private bool _touch, _network, _training, _tutorial, _spectator, _allBots, _sandbox, _pinned;
        private CustomRules _rules;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; NetAuthority.Provider = new Solo();
            _touch = TouchInput.Active; TouchInput.ReleaseAll(); TouchInput.Active = true;
            _network = SceneFlow.Networked; _training = GameLaunch.TrainingRange; _tutorial = GameLaunch.GuidedTutorial;
            _spectator = GameLaunch.Spectator; _allBots = GameLaunch.AllBots; _sandbox = PracticeSandbox.Wanted;
            _pinned = SceneFlow.RulesPinned; _rules = SceneFlow.SelectedRules.Clone();
            SceneFlow.Networked = false; MatchAbandon.Forget(); PracticeSandbox.Clear();
            GameLaunch.TrainingRange = GameLaunch.GuidedTutorial = GameLaunch.Spectator = GameLaunch.AllBots = false;
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike)); PresentationClock.RequestScale(1);
            SceneManager.SetActiveScene(SceneManager.CreateScene(SceneFlow.Eskinita));
            GameServices.Ensure(); _stats = GameServices.Stats; typeof(GameServices).GetProperty("Stats").SetValue(null, null);
            _previousCan = GameServices.Round.Lata;
            _can = new GameObject("Hero input retirement can"); var can = _can.AddComponent<Lata>(); can.enabled = false;
            GameServices.Round.Lata = can; GameServices.Round.BeginRound();
            _body = new GameObject("Hero input retirement owner");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 1; _motor.Mode = GameMode.HeroStrike; _motor.IsDefender = false; _motor.RoundActive = true;
            _body.AddComponent<Carrier>(); _body.AddComponent<CombatVerbs>();
            _system = _body.AddComponent<HeroAbilitySystem>(); _system.SpeaksAsHero = false;
            _kit = new CounterKit(); typeof(HeroAbilitySystem).GetProperty("Kit").SetValue(_system, _kit);
            _reader = _body.AddComponent<PlayerInputReader>();
            yield return null;
            Assert.IsTrue(GameServices.Round.RoundActive); Assert.IsTrue(_motor.CanAct()); Assert.IsFalse(_kit.PracticeMode);
            Assert.IsTrue(_reader.enabled); Assert.IsTrue(_system.enabled);
        }

        [UnityTearDown] public IEnumerator After()
        {
            foreach (var go in new[] { _rangeBody, _localBody, _body, _can }) if (go != null) Object.Destroy(go);
            yield return null;
            GameServices.Round.Lata = _previousCan; typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            TouchInput.ReleaseAll(); TouchInput.Active = _touch;
            NetAuthority.Provider = _provider; SceneFlow.Networked = _network; PracticeSandbox.Wanted = _sandbox;
            GameLaunch.TrainingRange = _training; GameLaunch.GuidedTutorial = _tutorial;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
            PresentationClock.RequestScale(1); yield return PlayModeWorld.Reset();
        }

        private void Read() => typeof(PlayerInputReader).GetMethod("Update", Hidden).Invoke(_reader, null);
        private void Step() => typeof(HeroAbilitySystem).GetMethod("Update", Hidden).Invoke(_system, null);
        private void Hold()
        {
            TouchInput.Set(Verb.Skill1, true); Read(); Step();
            Assert.IsTrue(_system.IsAiming(HeroAbilitySystem.Slot.Skill1), "Actual touch input must begin the release-only hold.");
            Assert.Zero(_kit.Hold.Activations); Assert.AreEqual(3, _kit.Hold.ChargesRemaining);
        }
        private void BufferInstant()
        {
            _motor.ApplyStagger(.15f); Assert.IsFalse(_motor.CanAct());
            TouchInput.Set(Verb.Skill2, true); Read(); Step();
            Assert.Zero(_kit.Instant.Activations);
            Assert.IsTrue(_motor.Intent.Pressed(Verb.Skill2));
        }
        private void Retire(Exit exit)
        {
            if (exit == Exit.Focus) _body.SendMessage("OnApplicationFocus", false, SendMessageOptions.DontRequireReceiver);
            else if (exit == Exit.Reader) _reader.enabled = false;
            else
            {
                // Supplied range readiness isolates the public active-to-idle API;
                // the separate operator fixture exercises the actual menu route.
                _localBody = new GameObject("Hero retirement practice local");
                var local = _localBody.AddComponent<CharacterMotor>(); local.enabled = false;
                _rangeBody = new GameObject("Hero retirement prepared range"); var range = _rangeBody.AddComponent<PracticeRange>();
                typeof(PracticeRange).GetProperty("Instance").SetValue(null, range);
                typeof(PracticeRange).GetProperty("Local").SetValue(range, local);
                var seats = new CharacterMotor[Balance.PlayerCount]; seats[0] = local; seats[1] = _motor;
                typeof(PracticeRange).GetField("_seats", Hidden).SetValue(range, seats);
                typeof(PracticeRange).GetField("_ready", Hidden).SetValue(range, true);
                GameLaunch.TrainingRange = true;
                Assert.IsTrue(range.SetBot(1, true, true)); Assert.IsTrue(_motor.Intent.Parked);
            }
        }
        private void CancelAim(Exit exit)
        {
            Hold(); Retire(exit);
            bool remained = _system.IsAiming(HeroAbilitySystem.Slot.Skill1);
            Step(); Assert.Zero(_kit.Hold.Activations, "Producer retirement was interpreted as a hero release.");
            Assert.IsFalse(remained, "Producer retirement left a pending hero aim until its next Update.");
            Assert.AreEqual(3, _kit.Hold.ChargesRemaining); Assert.IsTrue(_system.enabled);
        }
        private void CancelBuffer(Exit exit)
        {
            BufferInstant(); Retire(exit); _motor.ClearStun(); Step();
            Assert.Zero(_kit.Instant.Activations, "A retired buffered hero press cast after recovery.");
            Assert.Zero(_kit.Instant.CooldownRemaining);
        }

        [Test] public void FocusLossCancelsPendingHeroAim() => CancelAim(Exit.Focus);
        [Test] public void ReaderDisableCancelsPendingHeroAim() => CancelAim(Exit.Reader);
        [Test] public void PracticeIdleCancelsPendingHeroAim() => CancelAim(Exit.PracticeIdle);
        [Test] public void FocusLossDiscardsPendingHeroBuffer() => CancelBuffer(Exit.Focus);
        [Test] public void ReaderDisableDiscardsPendingHeroBuffer() => CancelBuffer(Exit.Reader);
        [Test] public void PracticeIdleDiscardsPendingHeroBuffer() => CancelBuffer(Exit.PracticeIdle);

        [Test] public void HeroComponentDisableDiscardsPendingHeroBuffer()
        {
            BufferInstant(); _system.enabled = false; _system.enabled = true; _motor.ClearStun(); Step();
            Assert.Zero(_kit.Instant.Activations, "A hero component retained its buffered press across disable/re-enable.");
        }

        [Test] public void OrdinaryTouchReleaseStillCastsOnceAndSpendsOneCharge()
        {
            Hold(); TouchInput.Set(Verb.Skill1, false); Read(); Step();
            Assert.AreEqual(1, _kit.Hold.Activations); Assert.AreEqual(2, _kit.Hold.ChargesRemaining);
            Step(); Assert.AreEqual(1, _kit.Hold.Activations);
        }
        [Test] public void OrdinaryStunBufferSurvivesUntilTheActorCanAct()
        {
            BufferInstant(); _motor.ClearStun(); Step();
            Assert.AreEqual(1, _kit.Instant.Activations, "Ordinary stun buffering was erased.");
            Assert.Greater(_kit.Instant.CooldownRemaining, 0);
        }
        private void KeepActive(Exit exit)
        {
            var context = new AbilityContext(_motor, _body.GetComponent<Carrier>(), _body.GetComponent<CombatVerbs>());
            Assert.AreEqual(HeroKit.CastOutcome.Cast, _kit.CastSkill2(context));
            float duration = _kit.Instant.DurationRemaining, cooldown = _kit.Instant.CooldownRemaining;
            int charges = _kit.Hold.ChargesRemaining;
            Retire(exit);
            Assert.IsTrue(_kit.Instant.IsActive); Assert.AreEqual(duration, _kit.Instant.DurationRemaining);
            Assert.AreEqual(cooldown, _kit.Instant.CooldownRemaining); Assert.AreEqual(charges, _kit.Hold.ChargesRemaining);
            Assert.AreEqual(1, _kit.Instant.Activations);
        }
        [Test] public void FocusLossPreservesAnActivatedAbilityAndResources() => KeepActive(Exit.Focus);
        [Test] public void ReaderDisablePreservesAnActivatedAbilityAndResources() => KeepActive(Exit.Reader);

        private void KeepRemoteAim(Exit exit)
        {
            _motor.PlayerSlot = 2; NetAuthority.Provider = new Observer();
            var aim = new AbilityAimSnapshot { Slot = 1, AbilityId = new FixedString64Bytes(_kit.Hold.Id), Held = .2f,
                Token = AbilityAimSnapshot.MakeToken(_motor.MovementEpoch, 7) };
            _system.ApplyNetworkAim(aim); Assert.IsTrue(_system.IsAiming(HeroAbilitySystem.Slot.Skill1));
            Retire(exit);
            Assert.IsTrue(_system.IsAiming(HeroAbilitySystem.Slot.Skill1), "An obsolete remote reader cancelled a received tell.");
            aim.Held = .4f; _system.ApplyNetworkAim(aim);
            Assert.AreEqual(aim.Token, _system.CaptureAimPresentation().Token, "The same accepted remote hold could not renew.");
            Assert.GreaterOrEqual(_system.HeldSeconds(HeroAbilitySystem.Slot.Skill1), .4f);
            Assert.Zero(_kit.Hold.Activations);
        }
        [Test] public void RemoteReaderFocusLossPreservesReceivedAimAndRenewal() => KeepRemoteAim(Exit.Focus);
        [Test] public void RemoteReaderDisablePreservesReceivedAimAndRenewal() => KeepRemoteAim(Exit.Reader);
    }
}
