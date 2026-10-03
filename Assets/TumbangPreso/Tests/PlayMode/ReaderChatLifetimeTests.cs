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
    public sealed class ReaderChatLifetimeTests
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
        private bool _typing, _touch, _network, _training, _tutorial, _spectator, _allBots, _sandbox;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; NetAuthority.Provider = new Solo();
            _typing = LobbyChat.AnyTyping; Typing(false);
            _touch = TouchInput.Active; TouchInput.ReleaseAll(); TouchInput.Active = true;
            _network = SceneFlow.Networked; SceneFlow.Networked = false;
            _training = GameLaunch.TrainingRange; _tutorial = GameLaunch.GuidedTutorial;
            _spectator = GameLaunch.Spectator; _allBots = GameLaunch.AllBots; _sandbox = PracticeSandbox.Wanted;
            GameLaunch.TrainingRange = GameLaunch.GuidedTutorial = GameLaunch.Spectator = GameLaunch.AllBots = false;
            PracticeSandbox.Clear(); Hitstop.End(); PresentationClock.RequestScale(1);
            SceneManager.SetActiveScene(SceneManager.CreateScene(SceneFlow.Eskinita)); GameServices.Ensure();
            _stats = GameServices.Stats; typeof(GameServices).GetProperty("Stats").SetValue(null, null);
            _previousCan = GameServices.Round.Lata;
            _canBody = new GameObject("Chat lifetime can"); var can = _canBody.AddComponent<Lata>(); can.enabled = false;
            GameServices.Round.Lata = can; GameServices.Round.BeginRound();
            _body = new GameObject("Chat lifetime local reader");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 1; _motor.Mode = GameMode.Classic; _motor.IsDefender = false; _motor.RoundActive = true;
            _body.transform.position = Vector3.back * (Balance.ConfinementRadius + 1);
            _carrier = _body.AddComponent<Carrier>(); _carrier.enabled = false;
            _verbs = _body.AddComponent<CombatVerbs>(); _verbs.enabled = false;
            _shoeBody = new GameObject("Chat lifetime owned shoe"); _shoe = _shoeBody.AddComponent<Slipper>();
            _shoe.enabled = false; _shoe.OwnerSlot = _shoe.SeatOfOrigin = 1;
            Assert.IsTrue(_shoe.HostForceEquip(_motor));
            _reader = _body.AddComponent<PlayerInputReader>(); yield return null;
            Assert.IsTrue(_reader.enabled); Assert.IsTrue(_motor.CanAct());
            Assert.IsFalse(UI.Hub.HubLoading.Visible); Assert.IsFalse(LobbyChat.AnyTyping);
        }
        [UnityTearDown] public IEnumerator After()
        {
            foreach (var go in new[] { _body, _shoeBody, _canBody }) if (go != null) Object.Destroy(go);
            yield return null;
            GameServices.Round.Lata = _previousCan; typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            TouchInput.ReleaseAll(); TouchInput.Active = _touch; NetAuthority.Provider = _provider; Typing(_typing);
            SceneFlow.Networked = _network; GameLaunch.TrainingRange = _training; GameLaunch.GuidedTutorial = _tutorial;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots; PracticeSandbox.Wanted = _sandbox;
            Hitstop.End(); PresentationClock.RequestScale(1); yield return PlayModeWorld.Reset();
        }
        // Supply the context flag, as GameplayChatInputTests does. This fixture qualifies
        // the reader and consumers, not the field-focus UI or actual hardware presses.
        private static void Typing(bool value) => typeof(LobbyChat).GetProperty("AnyTyping").SetValue(null, value);
        private static void Step(object target) => target.GetType().GetMethod("Update", Hidden).Invoke(target, null);
        private float Contact => (float)typeof(CombatVerbs).GetField("_lungeActiveLeft", Hidden).GetValue(_verbs);
        private void Windup(bool lunge)
        {
            _motor.IsDefender = lunge;
            TouchInput.Set(lunge ? Verb.Lunge : Verb.SpecialAbility, true); Step(_reader);
            Step(lunge ? (object)_verbs : _carrier); Step(lunge ? (object)_verbs : _carrier);
            Assert.IsTrue(lunge ? _verbs.ObservedLungeCharge >= 0 : _carrier.IsCharging);
            Assert.AreSame(_shoe, _carrier.Held); Assert.Zero(Contact); Assert.Zero(_verbs.LungeCooldownLeft);
        }

        [Test] public void EnteringChatCancelsThePendingThrowBeforeConsumersSeeARelease()
        {
            Windup(false); Typing(true); Step(_reader); Step(_carrier);
            Assert.AreSame(_shoe, _carrier.Held, "Entering chat launched the pending throw.");
            Assert.AreEqual(SlipperState.Held, _shoe.State); Assert.IsFalse(_carrier.IsCharging);
            Assert.AreEqual(-1, _carrier.ObservedChargePower);
        }
        [Test] public void EnteringChatCancelsThePendingLungeBeforeConsumersSeeARelease()
        {
            Windup(true); Typing(true); Step(_reader); Step(_verbs);
            Assert.Zero(_verbs.LungeCooldownLeft, "Entering chat spent a lunge cooldown.");
            Assert.Zero(Contact, "Entering chat opened a lunge contact window.");
            Assert.AreEqual(-1, _verbs.ObservedLungeCharge);
        }
        [UnityTest] public IEnumerator AHeldChatButtonCannotBeginAThrowWhenTypingCloses()
        {
            Typing(true); TouchInput.Set(Verb.SpecialAbility, true); Step(_reader); Step(_carrier);
            Assert.IsFalse(_carrier.IsCharging); Typing(false); Step(_reader); Step(_carrier);
            yield return null; Step(_reader); Step(_carrier);
            Assert.IsFalse(_carrier.IsCharging, "A button held in chat entered gameplay without release.");
            Assert.AreSame(_shoe, _carrier.Held);
        }
        [Test] public void EnteringChatPreservesAnAlreadyCommittedLungeAndItsCooldown()
        {
            _motor.IsDefender = true;
            Assert.IsTrue(_verbs.HostResolveLunge(_body.transform.position, Vector3.forward, 1));
            float contact = Contact, cooldown = _verbs.LungeCooldownLeft;
            Assert.Greater(contact, 0); Assert.Greater(cooldown, 0);
            Typing(true); Step(_reader);
            Assert.AreEqual(contact, Contact); Assert.AreEqual(cooldown, _verbs.LungeCooldownLeft);
        }
        [UnityTest] public IEnumerator AReleasedChatButtonAllowsAFreshGameplayThrowPress()
        {
            Typing(true); TouchInput.Set(Verb.SpecialAbility, true); Step(_reader);
            TouchInput.Set(Verb.SpecialAbility, false); Step(_reader);
            Typing(false); Step(_reader); yield return null;
            TouchInput.Set(Verb.SpecialAbility, true); Step(_reader); Step(_carrier);
            Assert.IsTrue(_carrier.IsCharging, "A fresh gameplay press remained blocked after chat.");
            Assert.AreSame(_shoe, _carrier.Held);
        }
    }
}
