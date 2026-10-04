using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class AiRoundOpeningLifetimeTests
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject _body, _directors;
        AIController _brain;
        CharacterMotor _motor;
        MatchDirector _match, _priorMatch;
        RoundDirector _round, _priorRound;
        Random.State _random;

        [SetUp] public void Before()
        {
            _random = Random.state;
            _priorMatch = GameServices.Match; _priorRound = GameServices.Round;
            _directors = new GameObject("Bot opening directors");
            _match = _directors.AddComponent<MatchDirector>(); _match.enabled = false;
            _round = _directors.AddComponent<RoundDirector>(); _round.enabled = false;
            typeof(GameServices).GetProperty("Match").SetValue(null, _match);
            typeof(GameServices).GetProperty("Round").SetValue(null, _round);
            _body = new GameObject("Bot opening seat", typeof(CharacterController));
            _motor = _body.AddComponent<CharacterMotor>(); _motor.PlayerSlot = 1; _motor.IsBot = true;
            _body.AddComponent<Carrier>(); _brain = _body.AddComponent<AIController>();
            Call("Awake"); Call("Subscribe");
        }

        [TearDown] public void After()
        {
            Object.DestroyImmediate(_body); Object.DestroyImmediate(_directors);
            typeof(GameServices).GetProperty("Match").SetValue(null, _priorMatch);
            typeof(GameServices).GetProperty("Round").SetValue(null, _priorRound);
            Random.state = _random;
        }

        void Call(string name) => typeof(AIController).GetMethod(name, Hidden).Invoke(_brain, null);
        void Set(string name, object value) => typeof(AIController).GetField(name, Hidden).SetValue(_brain, value);
        float Clock => (float)typeof(AIController).GetField("_roundLiveFor", Hidden).GetValue(_brain);
        void OldOpening() { Set("_roundLiveFor", 25f); Set("_weighing", (Verb?)Verb.Skill1); Set("_weighedFor", .8f); }

        [Test] public void RoundEventStartsANewOpeningWithoutAnInactiveAbilityTick()
        {
            OldOpening(); _match.AdvanceRound();
            Assert.AreEqual(0f, Clock, "The next round inherited the previous round's open ability gate.");
        }

        [Test] public void ActualInactiveUpdateCannotSubstituteForTheRoundEvent()
        {
            OldOpening(); _motor.RoundActive = false; Call("Update");
            Assert.AreEqual(25f, Clock, "Inactive Update exits before StepHeroAbilities; this is the causal path.");
            _motor.RoundActive = true; _match.AdvanceRound();
            Assert.AreEqual(0f, Clock, "A real inactive frame still left the next round's opening unlocked.");
        }

        [Test] public void SuppressedBodyAiStartsFreshWithoutReleasingTheHumansHeroKey()
        {
            _brain.AbilitiesEnabled = false; _motor.Intent.Set(Verb.Skill2, true);
            OldOpening(); _match.AdvanceRound();
            Assert.IsTrue(_motor.Intent.Pressed(Verb.Skill2));
            Assert.IsFalse(_motor.Intent.JustReleased(Verb.Skill2));
            Assert.AreEqual(0f, Clock);
        }

        [Test] public void RoundEventDropsThePreviousDecisionButPreservesCastCadence()
        {
            OldOpening(); Set("_abilityCadenceLeft", .7f); _match.AdvanceRound();
            Assert.AreEqual(.7f, (float)typeof(AIController).GetField("_abilityCadenceLeft", Hidden).GetValue(_brain));
            Assert.IsNull(typeof(AIController).GetField("_weighing", Hidden).GetValue(_brain));
            Assert.AreEqual(0f, (float)typeof(AIController).GetField("_weighedFor", Hidden).GetValue(_brain));
        }

        [Test] public void SameRoundInactiveUpdatePreservesTheOpeningAndCadence()
        {
            OldOpening(); Set("_abilityCadenceLeft", .7f); _motor.RoundActive = false; Call("Update");
            Assert.AreEqual(25f, Clock);
            Assert.AreEqual(.7f, (float)typeof(AIController).GetField("_abilityCadenceLeft", Hidden).GetValue(_brain));
        }
    }
}
