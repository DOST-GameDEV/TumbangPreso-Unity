using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class InputProducerCancellationTests
    {
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

        private IEnumerator CheckCancelled(bool lunge, bool focus)
        {
            yield return Windup(lunge);
            if (focus) _body.SendMessage("OnApplicationFocus", false, SendMessageOptions.DontRequireReceiver);
            else _reader.enabled = false;
            // Cancellation must happen synchronously, before a consumer can read the release.
            Assert.IsFalse(_carrier.IsCharging); Assert.AreEqual(-1, _verbs.ObservedLungeCharge);
            Assert.IsTrue(_carrier.enabled); Assert.IsTrue(_verbs.enabled);
            yield return null;
            Assert.AreSame(_shoe, _carrier.Held, "Producer interruption released the pending throw.");
            Assert.AreEqual(SlipperState.Held, _shoe.State);
            Assert.Zero(Contact, "Producer interruption opened a lunge contact window.");
            Assert.Zero(_verbs.LungeCooldownLeft, "Producer interruption spent a new lunge cooldown.");
        }

        [UnityTest] public IEnumerator FocusLossCancelsPendingThrow() => CheckCancelled(false, true);
        [UnityTest] public IEnumerator FocusLossCancelsPendingLunge() => CheckCancelled(true, true);
        [UnityTest] public IEnumerator ReaderDisableCancelsPendingThrow() => CheckCancelled(false, false);
        [UnityTest] public IEnumerator ReaderDisableCancelsPendingLunge() => CheckCancelled(true, false);

        [UnityTest] public IEnumerator ReaderReenableRequiresReleaseBeforeASecondThrowWindup()
        {
            yield return Windup(false);
            _reader.enabled = false; _reader.enabled = true;
            yield return null;
            Assert.IsFalse(_carrier.IsCharging, "Held touch input restarted the retired windup.");
            Assert.AreSame(_shoe, _carrier.Held);
            TouchInput.Set(Verb.SpecialAbility, false); yield return null;
            TouchInput.Set(Verb.SpecialAbility, true); yield return null;
            Assert.IsTrue(_carrier.IsCharging, "A fresh press could not begin a new windup.");
        }

        private IEnumerator CheckInteractRelease(bool focus)
        {
            TouchInput.Set(Verb.Interact, true); yield return null;
            Assert.IsTrue(_motor.Intent.Pressed(Verb.Interact));
            if (focus) _body.SendMessage("OnApplicationFocus", false, SendMessageOptions.DontRequireReceiver);
            else { _reader.enabled = false; _reader.enabled = true; }
            yield return null;
            Assert.IsFalse(_motor.Intent.Pressed(Verb.Interact), "Retired held Interact restarted without release.");
            TouchInput.Set(Verb.Interact, false); yield return null;
            TouchInput.Set(Verb.Interact, true); yield return null;
            Assert.IsTrue(_motor.Intent.Pressed(Verb.Interact), "Fresh Interact press remained blocked.");
        }

        [UnityTest] public IEnumerator FocusLossRequiresAFreshInteractPress() => CheckInteractRelease(true);
        [UnityTest] public IEnumerator ReaderReenableRequiresAFreshInteractPress() => CheckInteractRelease(false);

        [UnityTest] public IEnumerator ActualThrowReleaseStillLaunchesTheOwnedShoe()
        {
            yield return Windup(false); TouchInput.Set(Verb.SpecialAbility, false);
            float until = Time.realtimeSinceStartup + 3;
            while (_carrier.Held != null && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsNull(_carrier.Held); Assert.AreEqual(SlipperState.InFlight, _shoe.State);
        }

        [UnityTest] public IEnumerator ActualLungeReleaseStillSpendsCooldownAndOpensContact()
        {
            yield return Windup(true); TouchInput.Set(Verb.Lunge, false);
            float until = Time.realtimeSinceStartup + 3;
            while (_verbs.LungeCooldownLeft <= 0 && Time.realtimeSinceStartup < until) yield return null;
            Assert.Greater(_verbs.LungeCooldownLeft, 0); Assert.Greater(Contact, 0);
        }

        [UnityTest] public IEnumerator FocusLossKeepsAnAlreadyCommittedLungeAndItsCooldown()
        {
            _motor.IsDefender = true;
            Assert.IsTrue(_verbs.HostResolveLunge(_body.transform.position, Vector3.forward, 1));
            float contact = Contact, cooldown = _verbs.LungeCooldownLeft;
            _body.SendMessage("OnApplicationFocus", false, SendMessageOptions.DontRequireReceiver);
            Assert.AreEqual(contact, Contact); Assert.AreEqual(cooldown, _verbs.LungeCooldownLeft);
            yield return null;
        }

        private IEnumerator CheckPracticeIdle(bool lunge)
        {
            yield return Windup(lunge);
            // Readiness is a supplied seam; this exercises SetBot's active-body path,
            // not loading, PausePanel, bot planning, travel or full operator acceptance.
            _localBody = new GameObject("Input cancellation practice local");
            var local = _localBody.AddComponent<CharacterMotor>(); local.enabled = false;
            _rangeBody = new GameObject("Input cancellation prepared range");
            var range = _rangeBody.AddComponent<PracticeRange>();
            typeof(PracticeRange).GetProperty("Instance").SetValue(null, range);
            typeof(PracticeRange).GetProperty("Local").SetValue(range, local);
            var seats = new CharacterMotor[Balance.PlayerCount]; seats[0] = local; seats[1] = _motor;
            typeof(PracticeRange).GetField("_seats", Private).SetValue(range, seats);
            typeof(PracticeRange).GetField("_ready", Private).SetValue(range, true);
            GameLaunch.TrainingRange = true;
            Assert.IsTrue(range.SetBot(1, true, true)); Assert.IsTrue(_motor.Intent.Parked);
            Assert.IsFalse(_carrier.IsCharging); Assert.AreEqual(-1, _verbs.ObservedLungeCharge);
            yield return null;
            Assert.AreSame(_shoe, _carrier.Held); Assert.Zero(Contact); Assert.Zero(_verbs.LungeCooldownLeft);
        }

        [UnityTest] public IEnumerator PracticeIdleCancelsPendingThrow() => CheckPracticeIdle(false);
        [UnityTest] public IEnumerator PracticeIdleCancelsPendingLunge() => CheckPracticeIdle(true);
    }
}
