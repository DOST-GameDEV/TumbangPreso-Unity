using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class PracticeResetCombatLifetimeTests
    {
        private GameObject _body, _canBody, _rangeBody;
        private CharacterMotor _motor;
        private CombatVerbs _verbs;
        private PracticeRange _range;
        private INetProvider _provider;
        private MatchStatsCollector _stats;
        private Lata _previousCan;
        private bool _network, _training, _tutorial, _spectator, _allBots, _sandbox;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private float Contact => (float)typeof(CombatVerbs).GetField("_lungeActiveLeft", Private).GetValue(_verbs);

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; NetAuthority.Provider = new SoloProvider();
            _network = SceneFlow.Networked; SceneFlow.Networked = false;
            _training = GameLaunch.TrainingRange; _tutorial = GameLaunch.GuidedTutorial;
            _spectator = GameLaunch.Spectator; _allBots = GameLaunch.AllBots;
            _sandbox = PracticeSandbox.Wanted;
            GameLaunch.TrainingRange = true;
            GameLaunch.GuidedTutorial = GameLaunch.Spectator = GameLaunch.AllBots = false;
            PresentationClock.RequestScale(1); GameServices.Ensure();
            _stats = GameServices.Stats; typeof(GameServices).GetProperty("Stats").SetValue(null, null);
            _previousCan = GameServices.Round.Lata;
            GameServices.Match.ApplySnapshot(new int[Balance.PlayerCount], 1, true);
            _canBody = new GameObject("Practice reset can");
            var can = _canBody.AddComponent<Lata>(); can.enabled = false;
            GameServices.Round.Lata = can;
            _body = new GameObject("Practice reset defender");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 0; _motor.IsDefender = true; _motor.RoundActive = true;
            _motor.Mode = GameMode.Classic;
            _verbs = _body.AddComponent<CombatVerbs>();
            GameServices.Round.Register(_motor); GameServices.Round.BeginRound();
            _rangeBody = new GameObject("Practice reset prepared owner");
            var runner = _rangeBody.AddComponent<SliceRunner>(); runner.AutoStart = false;
            var seats = new CharacterMotor[Balance.PlayerCount]; seats[0] = _motor;
            var shoes = new Slipper[Balance.PlayerCount];
            runner.Seats = seats; runner.Slippers = shoes; runner.Lata = can;
            _range = _rangeBody.AddComponent<PracticeRange>();
            // Supply startup readiness only. World reset and lunge creation use their
            // real public APIs; this does not qualify a shipping map or PausePanel.
            typeof(PracticeRange).GetProperty("Instance").SetValue(null, _range);
            typeof(PracticeRange).GetProperty("Local").SetValue(_range, _motor);
            typeof(PracticeRange).GetField("_seats", Private).SetValue(_range, seats);
            typeof(PracticeRange).GetField("_slippers", Private).SetValue(_range, shoes);
            typeof(PracticeRange).GetField("_lata", Private).SetValue(_range, can);
            typeof(PracticeRange).GetField("_runner", Private).SetValue(_range, runner);
            typeof(PracticeRange).GetField("_ready", Private).SetValue(_range, true);
            Assert.IsTrue(_range.CanEdit); Assert.IsTrue(_motor.CanAct());
        }

        [UnityTearDown] public IEnumerator After()
        {
            if (_rangeBody != null) Object.Destroy(_rangeBody);
            if (_body != null) Object.Destroy(_body);
            if (_canBody != null) Object.Destroy(_canBody);
            yield return null;
            GameServices.Round.Lata = _previousCan;
            typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            NetAuthority.Provider = _provider; SceneFlow.Networked = _network;
            GameLaunch.TrainingRange = _training; GameLaunch.GuidedTutorial = _tutorial;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots;
            PracticeSandbox.Wanted = _sandbox; PresentationClock.RequestScale(1);
            yield return PlayModeWorld.Reset();
        }

        private void OpenContact()
        {
            _motor.Teleport(new Vector3(6, 0, 6));
            Assert.IsTrue(_verbs.HostResolveLunge(_body.transform.position, Vector3.forward, 1));
            Assert.Greater(Contact, 0); Assert.Greater(_verbs.LungeCooldownLeft, 0);
        }

        [UnityTest] public IEnumerator ResetRangeRetiresPreTeleportContactWithoutRefundingCooldown()
        {
            OpenContact(); float cooldown = _verbs.LungeCooldownLeft;
            Vector3 previous = _body.transform.position;
            Assert.IsTrue(_range.ResetRange());
            Assert.Greater(Vector3.Distance(previous, _body.transform.position), 1);
            Assert.LessOrEqual(Contact, 0, "Range reset retained the pre-teleport lunge segment.");
            Assert.AreEqual(cooldown, _verbs.LungeCooldownLeft, "Reset refunded a spent lunge cooldown.");
            Assert.IsTrue(_body.activeInHierarchy); Assert.IsTrue(_verbs.enabled);
            yield return null; Assert.LessOrEqual(Contact, 0);
        }

        [UnityTest] public IEnumerator SetDefenderRetiresThePreviousRoleContact()
        {
            OpenContact(); float cooldown = _verbs.LungeCooldownLeft;
            Assert.IsTrue(_range.SetDefender(1)); Assert.IsFalse(_motor.IsDefender);
            Assert.LessOrEqual(Contact, 0, "Changing the practice defender retained the prior role's lunge.");
            Assert.AreEqual(cooldown, _verbs.LungeCooldownLeft);
            yield return null;
        }

        [UnityTest] public IEnumerator ResetRangeCancelsPendingLungeWindupBeforeMovingTheBody()
        {
            _motor.Intent.Set(Verb.Lunge, true);
            float until = Time.realtimeSinceStartup + 3;
            while (_verbs.ObservedLungeCharge < 0 && Time.realtimeSinceStartup < until) yield return null;
            Assert.GreaterOrEqual(_verbs.ObservedLungeCharge, 0);
            Assert.IsTrue(_range.ResetRange());
            Assert.AreEqual(-1, _verbs.ObservedLungeCharge, "Reset retained the old lunge windup.");
            Assert.LessOrEqual(Contact, 0); Assert.Zero(_verbs.LungeCooldownLeft);
        }

        [UnityTest] public IEnumerator OrdinaryPausePreservesTheCommittedContactAndCooldown()
        {
            OpenContact(); float contact = Contact, cooldown = _verbs.LungeCooldownLeft;
            PresentationClock.RequestScale(0); yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(contact, Contact); Assert.AreEqual(cooldown, _verbs.LungeCooldownLeft);
            Assert.IsTrue(_body.activeInHierarchy); Assert.IsTrue(_verbs.enabled);
        }

        [UnityTest] public IEnumerator InvalidDefenderChoiceLeavesTheExistingContactUntouched()
        {
            OpenContact(); float contact = Contact, cooldown = _verbs.LungeCooldownLeft;
            Assert.IsFalse(_range.SetDefender(Balance.PlayerCount));
            Assert.AreEqual(contact, Contact); Assert.AreEqual(cooldown, _verbs.LungeCooldownLeft);
            yield return null;
        }
    }
}
