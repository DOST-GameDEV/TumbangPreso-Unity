using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class BotStatusExpiryProbe
    {
        private bool _allBots, _spectator, _tutorial, _botsEnabled, _pinned;
        private int _seat;
        private CustomRules _rules;
        private CharacterMotor _actor;
        private AIController _brain;

        [UnitySetUp] public IEnumerator Before()
        {
            _allBots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator;
            _tutorial = GameLaunch.GuidedTutorial; _seat = GameLaunch.SoloSeat;
            _botsEnabled = AIController.BotsEnabled; _pinned = SceneFlow.RulesPinned;
            _rules = SceneFlow.SelectedRules.Clone();
            yield return PlayModeWorld.Reset();
            GameLaunch.GuidedTutorial = false; AIController.BotsEnabled = true;
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.Classic);
            _actor = GameServices.Round.PlayerAt(2);
            Assert.IsNotNull(_actor); _brain = _actor.GetComponent<AIController>();
            Assert.IsNotNull(_brain); Assert.IsTrue(_actor.RoundActive);
            Assert.IsNotNull(_actor.GetComponent<Carrier>().Held, "The recovery question requires an armed bot");
            _actor.Intent.Parked = false; _brain.enabled = true;
            Assert.IsTrue(_actor.IsLocallySimulated());
            yield return null;
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = _allBots; GameLaunch.Spectator = _spectator;
            GameLaunch.GuidedTutorial = _tutorial; GameLaunch.SoloSeat = _seat;
            AIController.BotsEnabled = _botsEnabled;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }
        [UnityTest, Timeout(60000)] public IEnumerator FrozenBotReturnsToRealInputAfterNaturalExpiry()
        {
            _actor.ApplyStagger(Mathf.Max(.4f, Balance.MinStunDown + .1f), StunElement.Ice, Balance.StunBreakPressesDefault);
            Assert.IsTrue(_actor.IsFrozen);
            yield return Recovery();
        }
        [UnityTest, Timeout(60000)] public IEnumerator TaggedBotReturnsToRealInputAfterNaturalExpiry()
        {
            _actor.ApplyTagged(); Assert.IsTrue(_actor.IsTagged);
            yield return Recovery();
        }
        private IEnumerator Recovery()
        {
            Assert.IsFalse(_actor.CanAct()); yield return null;
            Assert.Less(_actor.Intent.MoveAxis.sqrMagnitude, .001f);
            float expiry = Time.realtimeSinceStartup + StatusRules.TaggedSeconds + 2;
            while (_actor.IsStunned && Time.realtimeSinceStartup < expiry) yield return null;
            Assert.IsFalse(_actor.IsStunned, "The real status timer did not expire");
            Assert.IsTrue(_actor.CanAct()); Assert.IsTrue(_brain.enabled);
            float deadline = Time.realtimeSinceStartup + 4;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (_actor.Intent.MoveAxis.sqrMagnitude > .01f || _actor.Intent.Pressed(Verb.SpecialAbility)
                    || _actor.Intent.Pressed(Verb.Grab) || _actor.Intent.Pressed(Verb.Lunge)) yield break;
                yield return null;
            }
            Assert.Fail($"Expired status but no resumed bot input: plan={_brain.Plan}, parked={_actor.Intent.Parked}, " +
                $"stun={_actor.StunLeft}, round={_actor.RoundActive}, throwLock={_actor.GetComponent<Carrier>().ThrowLockLeft}");
        }
    }
}
