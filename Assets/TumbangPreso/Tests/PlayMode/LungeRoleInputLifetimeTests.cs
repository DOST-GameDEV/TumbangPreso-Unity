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
    public sealed class LungeRoleInputLifetimeTests
    {
        private sealed class Solo : INetProvider
        {
            public bool IsHost => true; public bool IsNetworked => false;
            public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false;
        }
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private INetProvider _provider;
        private GameObject _body, _canBody;
        private CharacterMotor _motor;
        private CombatVerbs _verbs;
        private Lata _previousCan;
        private MatchStatsCollector _stats;
        private bool _network, _training, _tutorial, _spectator, _allBots, _sandbox;
        // Observe only: committed contact is opened through the public host resolver.
        private float Contact => (float)typeof(CombatVerbs).GetField("_lungeActiveLeft", Hidden).GetValue(_verbs);

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
            _previousCan = GameServices.Round.Lata;
            _canBody = new GameObject("Lunge role lifetime can"); var can = _canBody.AddComponent<Lata>(); can.enabled = false;
            GameServices.Round.Lata = can; GameServices.Round.BeginRound();
            _body = new GameObject("Registered lunge role lifetime body");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 1; _motor.Mode = GameMode.Classic; _motor.RoundActive = true;
            _verbs = _body.AddComponent<CombatVerbs>(); _verbs.enabled = false;
            GameServices.Round.Register(_motor); Role(2);
            _motor.Intent.Clear(); _motor.Intent.CommitFrame(); _motor.Intent.Parked = false;
            yield return null;
            Assert.IsTrue(_motor.CanAct()); Assert.IsTrue(_motor.IsDefender);
            Assert.Greater(Time.deltaTime, 0); Assert.Less(Time.deltaTime, Balance.LungeActiveTime);
        }
        [UnityTearDown] public IEnumerator After()
        {
            GameServices.Round.Unregister(_motor);
            foreach (var go in new[] { _body, _canBody }) if (go != null) Object.Destroy(go);
            yield return null;
            GameServices.Round.Lata = _previousCan; typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            NetAuthority.Provider = _provider; SceneFlow.Networked = _network;
            GameLaunch.TrainingRange = _training; GameLaunch.GuidedTutorial = _tutorial;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots; PracticeSandbox.Wanted = _sandbox;
            Hitstop.End(); PresentationClock.RequestScale(1); yield return PlayModeWorld.Reset();
        }
        private void Role(int round)
        {
            GameServices.Match.ApplySnapshot(new int[Balance.PlayerCount], round, true);
            int defender = (round - 1) % Balance.PlayerCount;
            GameServices.Round.ApplySnapshot(80, true, defender, true);
            Assert.AreEqual(defender == _motor.PlayerSlot, _motor.IsDefender);
            Assert.IsTrue(_motor.CanAct());
        }
        private void Step() => typeof(CombatVerbs).GetMethod("Update", Hidden).Invoke(_verbs, null);
        private void Windup()
        {
            _motor.Intent.Set(Verb.Lunge, true); Step(); Step();
            Assert.Greater(_verbs.LungeChargeRatio, 0); Assert.GreaterOrEqual(_verbs.ObservedLungeCharge, 0);
            Assert.Zero(_verbs.LungeCooldownLeft); Assert.Zero(Contact);
            // The attacker role must not get a new shove from the old press edge.
            _motor.Intent.CommitFrame(); Assert.IsFalse(_motor.Intent.JustPressed(Verb.Lunge));
        }
        [Test] public void BecomingAttackerRetiresTheDefenderLungeWindupAndTell()
        {
            Windup(); Role(3); Step();
            Assert.AreEqual(-1, _verbs.ObservedLungeCharge, "An attacker retained the previous defender lunge tell.");
            Assert.Zero(_verbs.LungeChargeRatio); Assert.Zero(_verbs.LungeCooldownLeft); Assert.Zero(Contact);
        }
        [Test] public void ReturningToDefenderAfterAttackerReleaseDoesNotLaunchTheOldWindup()
        {
            Windup(); Role(3); Step(); Assert.Zero(_verbs.ShoveCooldownLeft);
            _motor.Intent.Set(Verb.Lunge, false); Step(); _motor.Intent.CommitFrame();
            Role(6); Assert.IsFalse(_motor.Intent.Pressed(Verb.Lunge)); Step();
            Assert.Zero(_verbs.LungeCooldownLeft, "The old role's windup launched on return without a fresh defender press.");
            Assert.Zero(Contact); Assert.Zero(_verbs.LungeChargeRatio);
        }
        [Test] public void SameDefenderSnapshotPreservesTheHeldLungeWindup()
        {
            Windup(); float before = _verbs.LungeChargeRatio; Role(2); Step();
            Assert.GreaterOrEqual(_verbs.LungeChargeRatio, before); Assert.GreaterOrEqual(_verbs.ObservedLungeCharge, 0);
            Assert.Zero(_verbs.LungeCooldownLeft); Assert.Zero(Contact);
        }
        [Test] public void OrdinaryDefenderReleaseStillOpensLungeContactAndCooldown()
        {
            Windup(); _motor.Intent.Set(Verb.Lunge, false); Step();
            Assert.Greater(_verbs.LungeCooldownLeft, 0); Assert.Greater(Contact, 0);
            Assert.Zero(_verbs.LungeChargeRatio); Assert.AreEqual(-1, _verbs.ObservedLungeCharge);
        }
        [Test] public void BecomingAttackerPreservesAnAlreadyCommittedLungeAndItsCooldown()
        {
            Assert.IsTrue(_verbs.HostResolveLunge(_body.transform.position, Vector3.forward, 1));
            Assert.Greater(Contact, Time.deltaTime); float cooldown = _verbs.LungeCooldownLeft;
            Role(3); Step();
            Assert.Greater(Contact, 0, "Retiring the windup also retired already committed contact.");
            Assert.Greater(_verbs.LungeCooldownLeft, 0); Assert.LessOrEqual(_verbs.LungeCooldownLeft, cooldown);
            Assert.Zero(_verbs.LungeChargeRatio); Assert.AreEqual(-1, _verbs.ObservedLungeCharge);
        }
    }
}
