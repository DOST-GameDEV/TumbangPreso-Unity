using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class CarrierPossessionLifetimeTests
    {
        private sealed class Solo : INetProvider
        {
            public bool IsHost => true; public bool IsNetworked => false;
            public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false;
        }
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private INetProvider _provider;
        private GameObject _body, _shoeBody, _replacementBody, _canBody;
        private CharacterMotor _motor;
        private PlayerInputReader _reader;
        private Carrier _carrier;
        private Slipper _shoe;
        private Lata _previousCan;
        private MatchStatsCollector _stats;
        private bool _touch, _network, _training, _tutorial, _spectator, _allBots, _sandbox;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
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
            _canBody = new GameObject("Possession retirement can"); var can = _canBody.AddComponent<Lata>(); can.enabled = false;
            GameServices.Round.Lata = can; GameServices.Round.BeginRound();
            _body = new GameObject("Possession retirement carrier");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 1; _motor.Mode = GameMode.Classic; _motor.IsDefender = false; _motor.RoundActive = true;
            _body.transform.position = Vector3.back * (Balance.ConfinementRadius + 1);
            _carrier = _body.AddComponent<Carrier>(); _body.AddComponent<CombatVerbs>();
            _shoeBody = new GameObject("Original owned shoe"); _shoe = _shoeBody.AddComponent<Slipper>();
            _shoe.enabled = false; _shoe.OwnerSlot = _shoe.SeatOfOrigin = 1;
            Assert.IsTrue(_shoe.HostForceEquip(_motor));
            _reader = _body.AddComponent<PlayerInputReader>(); yield return null;
            Assert.IsTrue(_motor.CanAct()); Assert.AreSame(_shoe, _carrier.Held);
        }
        [UnityTearDown] public IEnumerator After()
        {
            foreach (var go in new[] { _body, _shoeBody, _replacementBody, _canBody }) if (go != null) Object.Destroy(go);
            yield return null;
            GameServices.Round.Lata = _previousCan; typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            TouchInput.ReleaseAll(); TouchInput.Active = _touch; NetAuthority.Provider = _provider;
            SceneFlow.Networked = _network; GameLaunch.TrainingRange = _training; GameLaunch.GuidedTutorial = _tutorial;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots; PracticeSandbox.Wanted = _sandbox;
            Hitstop.End(); PresentationClock.RequestScale(1); yield return PlayModeWorld.Reset();
        }
        private void Read() => typeof(PlayerInputReader).GetMethod("Update", Hidden).Invoke(_reader, null);
        private void Step() => typeof(Carrier).GetMethod("Update", Hidden).Invoke(_carrier, null);
        private void Charge()
        {
            TouchInput.Set(Verb.SpecialAbility, true); Read(); Step(); Step();
            Assert.IsTrue(_carrier.IsCharging); Assert.Greater(_carrier.ChargeRatio, 0);
            Assert.GreaterOrEqual(_carrier.ObservedChargePower, 0);
        }
        private void Retired()
        {
            Assert.IsFalse(_carrier.IsCharging, "The old shoe's pending throw charge survived a possession transition.");
            Assert.AreEqual(-1, _carrier.ObservedChargePower, "The old shoe's visible charge tell survived a possession transition.");
            Assert.Zero(_carrier.ChargeRatio); Assert.Zero(_carrier.CurrentPektusSpin);
        }

        [Test] public void DisarmAndImmediateReequipRetireTheOldThrowCharge()
        {
            Charge(); Assert.IsTrue(_shoe.HostDisarm()); Assert.IsTrue(_shoe.HostForceEquip(_motor));
            Assert.AreSame(_shoe, _carrier.Held); Retired();
            TouchInput.Set(Verb.SpecialAbility, false); Read(); Step();
            Assert.AreSame(_shoe, _carrier.Held); Assert.AreEqual(SlipperState.Held, _shoe.State,
                "The retired charge threw the immediately re-equipped shoe on release.");
        }
        [Test] public void ForceEquippingAReplacementRetiresTheDisplacedShoeCharge()
        {
            Charge(); _replacementBody = new GameObject("Replacement owned shoe");
            var replacement = _replacementBody.AddComponent<Slipper>(); replacement.enabled = false;
            replacement.OwnerSlot = replacement.SeatOfOrigin = 1;
            Assert.IsTrue(replacement.HostForceEquip(_motor)); Assert.AreSame(replacement, _carrier.Held);
            Assert.AreEqual(SlipperState.Loose, _shoe.State); Assert.IsNull(_shoe.Holder); Retired();
            TouchInput.Set(Verb.SpecialAbility, false); Read(); Step();
            Assert.AreSame(replacement, _carrier.Held); Assert.AreEqual(SlipperState.Held, replacement.State);
        }
        [Test] public void DisarmRetiresAnObservedTellWithoutALocalCharge()
        {
            _carrier.ApplyObservedCharge(true, .7f, .2f); Assert.IsFalse(_carrier.IsCharging);
            Assert.Greater(_carrier.ObservedChargePower, 0); Assert.Greater(_carrier.ObservedPektusSpin, 0);
            Assert.IsTrue(_shoe.HostDisarm());
            Assert.AreEqual(-1, _carrier.ObservedChargePower, "The detached shoe left an observer-only throw tell active.");
            Assert.Zero(_carrier.ObservedPektusSpin);
        }
        [Test] public void SameHeldNotificationsPreserveTheExistingCharge()
        {
            Charge(); float charge = _carrier.ChargeRatio, observed = _carrier.ObservedChargePower;
            _carrier.NotifyHolding(_shoe); _carrier.NotifyEquipped(_shoe);
            Assert.IsTrue(_shoe.HostForceEquip(_motor));
            Assert.IsTrue(_carrier.IsCharging); Assert.AreEqual(charge, _carrier.ChargeRatio);
            Assert.AreEqual(observed, _carrier.ObservedChargePower); Assert.AreSame(_shoe, _carrier.Held);
        }
        [Test] public void EmptyHandNotificationsPreserveAnUnrelatedDefenderResetChannel()
        {
            Assert.IsTrue(_shoe.HostDisarm()); _motor.IsDefender = true;
            _body.transform.position = GameServices.Round.Lata.transform.position;
            GameServices.Round.Lata.HostKnockDown(-1); Hitstop.End(); Assert.IsFalse(GameServices.Round.Lata.IsUpright);
            TouchInput.Set(Verb.Grab, true); Read(); Step();
            Assert.Greater(_carrier.ChannelRatio, 0); float channel = _carrier.ChannelRatio;
            Assert.IsNull(_carrier.Held); _carrier.NotifyHolding(null); _carrier.NotifyEquipped(null);
            Assert.AreEqual(channel, _carrier.ChannelRatio,
                "A null-to-null hand notification cancelled unrelated can restore work.");
        }
    }
}
