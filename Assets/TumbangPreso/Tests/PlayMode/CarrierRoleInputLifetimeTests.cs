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
    public sealed class CarrierRoleInputLifetimeTests
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
        private Carrier _carrier;
        private Slipper _shoe;
        private Lata _previousCan;
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
            _previousCan = GameServices.Round.Lata;
            _canBody = new GameObject("Role lifetime can"); var can = _canBody.AddComponent<Lata>(); can.enabled = false;
            GameServices.Round.Lata = can; GameServices.Round.BeginRound();
            _body = new GameObject("Registered role lifetime carrier");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 1; _motor.Mode = GameMode.Classic; _motor.RoundActive = true;
            _body.transform.position = Vector3.back * (Balance.ConfinementRadius + 1);
            _carrier = _body.AddComponent<Carrier>(); _carrier.enabled = false;
            _body.AddComponent<CombatVerbs>().enabled = false;
            GameServices.Round.Register(_motor); Role(1);
            _motor.Intent.Clear(); _motor.Intent.CommitFrame(); _motor.Intent.Parked = false;
            _shoeBody = new GameObject("Role lifetime owned shoe"); _shoe = _shoeBody.AddComponent<Slipper>();
            _shoe.enabled = false; _shoe.OwnerSlot = _shoe.SeatOfOrigin = 1;
            Assert.IsTrue(_shoe.HostForceEquip(_motor)); yield return null;
            Assert.IsTrue(_motor.CanAct()); Assert.AreSame(_shoe, _carrier.Held);
            Assert.Greater(Time.deltaTime, 0); Assert.IsFalse(_motor.IsDefender);
        }
        [UnityTearDown] public IEnumerator After()
        {
            GameServices.Round.Unregister(_motor);
            foreach (var go in new[] { _body, _shoeBody, _canBody }) if (go != null) Object.Destroy(go);
            yield return null;
            GameServices.Round.Lata = _previousCan; typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            NetAuthority.Provider = _provider; SceneFlow.Networked = _network;
            GameLaunch.TrainingRange = _training; GameLaunch.GuidedTutorial = _tutorial;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots; PracticeSandbox.Wanted = _sandbox;
            Hitstop.End(); PresentationClock.RequestScale(1); yield return PlayModeWorld.Reset();
        }
        // Both shipping snapshot APIs are public; no role flag or pending state is seeded.
        private void Role(int round)
        {
            GameServices.Match.ApplySnapshot(new int[Balance.PlayerCount], round, true);
            int defender = (round - 1) % Balance.PlayerCount;
            GameServices.Round.ApplySnapshot(80, true, defender, true);
            Assert.AreEqual(defender == _motor.PlayerSlot, _motor.IsDefender);
            Assert.IsTrue(_motor.CanAct());
        }
        private void Step() => typeof(Carrier).GetMethod("Update", Hidden).Invoke(_carrier, null);
        private void Charge()
        {
            _motor.Intent.Set(Verb.SpecialAbility, true); Step(); Step();
            Assert.IsTrue(_carrier.IsCharging); Assert.Greater(_carrier.ChargeRatio, 0);
            Assert.GreaterOrEqual(_carrier.ObservedChargePower, 0);
        }
        private void Channel()
        {
            Assert.IsTrue(_shoe.HostDisarm()); Role(2);
            _body.transform.position = GameServices.Round.Lata.transform.position;
            GameServices.Round.Lata.HostKnockDown(-1); Hitstop.End();
            Assert.IsFalse(GameServices.Round.Lata.IsUpright); Assert.IsTrue(_carrier.HasResetTarget);
            _motor.Intent.Set(Verb.Grab, true); Step();
            Assert.Greater(_carrier.ChannelRatio, 0); Assert.IsTrue(_carrier.IsBusy);
        }
        [Test] public void BecomingDefenderRetiresTheOldAttackerThrowCharge()
        {
            Charge(); Role(2); Step();
            Assert.IsFalse(_carrier.IsCharging, "An active defender snapshot retained the attacker's throw windup.");
            Assert.Zero(_carrier.ChargeRatio); Assert.AreEqual(-1, _carrier.ObservedChargePower);
            Assert.AreSame(_shoe, _carrier.Held, "Retiring input must not disarm the held shoe.");
            Role(5); _motor.Intent.Set(Verb.SpecialAbility, false); Step();
            Assert.AreSame(_shoe, _carrier.Held, "The retired charge threw on a later attacker release.");
        }
        [Test] public void BecomingAttackerRetiresTheOldDefenderResetChannel()
        {
            Channel(); Role(3); _motor.Intent.Set(Verb.Grab, false); _motor.Intent.CommitFrame(); Step();
            Assert.Zero(_carrier.ChannelRatio, "An active attacker snapshot retained the defender's reset progress.");
            Assert.IsFalse(_carrier.IsBusy, "The retired reset channel still owns attacker action availability.");
            Assert.IsFalse(GameServices.Round.Lata.IsUpright);
        }
        [Test] public void SameAttackerSnapshotPreservesTheHeldThrowCharge()
        {
            Charge(); float before = _carrier.ChargeRatio; Role(1); Step();
            Assert.IsTrue(_carrier.IsCharging); Assert.GreaterOrEqual(_carrier.ChargeRatio, before);
            Assert.GreaterOrEqual(_carrier.ObservedChargePower, 0); Assert.AreSame(_shoe, _carrier.Held);
        }
        [Test] public void SameDefenderSnapshotPreservesTheHeldResetChannel()
        {
            Channel(); float before = _carrier.ChannelRatio; Role(2); Step();
            Assert.GreaterOrEqual(_carrier.ChannelRatio, before); Assert.IsTrue(_carrier.IsBusy);
            Assert.IsFalse(GameServices.Round.Lata.IsUpright);
        }
        [Test] public void OrdinaryAttackerReleaseStillThrowsTheOwnedShoe()
        {
            Charge(); _motor.Intent.Set(Verb.SpecialAbility, false); Step();
            Assert.IsFalse(_carrier.IsCharging); Assert.IsNull(_carrier.Held);
            Assert.AreEqual(SlipperState.InFlight, _shoe.State);
        }
    }
}
