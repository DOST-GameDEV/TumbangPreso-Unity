using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class AiUltimateInputRecoveryTests
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject _root,_roundRoot;CharacterMotor _actor;AIController _brain;SharedUltimatePhase _phase;
        RoundDirector _priorRound;float _scale;
        [SetUp] public void Before()
        {
            Assert.IsFalse(PresentationClock.Held);_scale=Time.timeScale;_priorRound=GameServices.Round;
            _roundRoot=new GameObject("Ultimate handoff round");var round=_roundRoot.AddComponent<RoundDirector>();round.enabled=false;
            typeof(GameServices).GetProperty("Round").SetValue(null,round);
            _root=new GameObject("Resetting defender",typeof(CharacterController));
            _actor=_root.AddComponent<CharacterMotor>();_actor.PlayerSlot=0;_actor.IsDefender=true;_actor.IsBot=true;_actor.RoundActive=true;
            _root.AddComponent<Carrier>();_brain=_root.AddComponent<AIController>();
            typeof(AIController).GetMethod("Awake",Hidden).Invoke(_brain,null);round.Register(_actor);
            _phase=_roundRoot.AddComponent<SharedUltimatePhase>();_phase.enabled=false;
        }
        [TearDown] public void After()
        {
            Clock("Release");Object.DestroyImmediate(_root);Object.DestroyImmediate(_roundRoot);
            typeof(GameServices).GetProperty("Round").SetValue(null,_priorRound);Time.timeScale=_scale;
        }
        static void Clock(string method)=>typeof(PresentationClock).GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        void ClearPhaseActions()=>typeof(SharedUltimatePhase).GetMethod("ClearActions",Hidden).Invoke(_phase,null);
        void HoldAndResume(bool stepBrain)
        {
            Clock("Hold");ClearPhaseActions();Assert.IsTrue(PresentationClock.BlocksInput);
            if(stepBrain)typeof(AIController).GetMethod("Update",Hidden).Invoke(_brain,null);
            ClearPhaseActions();Clock("Release");Assert.IsFalse(PresentationClock.BlocksInput);
        }
        [Test] public void DefenderResetGrabMustRearmAfterTheActualSharedHandoffClearsActions()
        {
            _actor.Intent.Set(Verb.Grab,true);HoldAndResume(true);
            _actor.Intent.Set(Verb.Grab,true);
            Assert.IsTrue(_actor.Intent.Pressed(Verb.Grab),"AI released during the hold, so its next reset decision must not remain latched off until round end.");
        }
        [Test] public void SuppressedBodyAiRearmsItsResetButKeepsTheHumansHeroReleaseGate()
        {
            _brain.AbilitiesEnabled=false;_actor.Intent.Set(Verb.Grab,true);_actor.Intent.Set(Verb.Skill2,true);HoldAndResume(true);
            _actor.Intent.Set(Verb.Grab,true);_actor.Intent.Set(Verb.Skill2,true);
            Assert.IsTrue(_actor.Intent.Pressed(Verb.Grab));
            Assert.IsFalse(_actor.Intent.Pressed(Verb.Skill2),"Body AI must not manufacture a hardware release for the human's hero key.");
            _actor.Intent.Set(Verb.Skill2,false);_actor.Intent.Set(Verb.Skill2,true);Assert.IsTrue(_actor.Intent.Pressed(Verb.Skill2));
        }
        [Test] public void HumanHeldActionsStillRequireARealReleaseAfterTheHandoff()
        {
            _actor.Intent.Set(Verb.Grab,true);HoldAndResume(false);_actor.Intent.Set(Verb.Grab,true);
            Assert.IsFalse(_actor.Intent.Pressed(Verb.Grab));
            _actor.Intent.Set(Verb.Grab,false);_actor.Intent.Set(Verb.Grab,true);Assert.IsTrue(_actor.Intent.Pressed(Verb.Grab));
        }
    }
}
