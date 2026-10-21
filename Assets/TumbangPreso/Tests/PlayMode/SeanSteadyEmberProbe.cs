using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class SeanSteadyEmberProbe
    {
        private readonly List<GameObject> _built=new List<GameObject>();
        private CharacterMotor _actor;private Carrier _carrier;private Slipper _shoe;
        private int _seat,_capture;private INetProvider _provider;private CustomRules _rules;private bool _pinned;
        private SeanHeroKit Kit=>(SeanHeroKit)_actor.AbilitySystem.Kit;
        private AbilityContext Context=>new AbilityContext(_actor,_carrier,_actor.GetComponent<CombatVerbs>());
        [UnitySetUp] public IEnumerator Before()
        {
            _seat=GameLaunch.SoloSeat;_capture=Time.captureFramerate;_provider=NetAuthority.Provider;
            _rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();GameLaunch.SoloSeat=1;NetAuthority.Provider=new SoloProvider();Time.captureFramerate=50;
        }
        [UnityTearDown] public IEnumerator After()
        {
            foreach(var go in _built)if(go!=null)Object.Destroy(go);_built.Clear();
            yield return PlayModeWorld.Reset();GameLaunch.SoloSeat=_seat;NetAuthority.Provider=_provider;Time.captureFramerate=_capture;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        private GameObject Track(GameObject go){_built.Add(go);return go;}
        private IEnumerator Open()
        {
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));GameServices.Ensure();GameServices.Round.Clear();
            var floor=Track(GameObject.CreatePrimitive(PrimitiveType.Cube));floor.transform.localScale=new Vector3(30,1,30);floor.transform.position=Vector3.down*.5f;
            var can=Track(new GameObject("Ember can"));can.transform.position=new Vector3(6,0,6);GameServices.Round.Lata=can.AddComponent<Lata>();
            var go=Track(new GameObject("Ember Sean",typeof(CharacterController)));
            _actor=go.AddComponent<CharacterMotor>();_actor.PlayerSlot=1;_actor.Mode=GameMode.HeroStrike;_actor.IsBot=false;
            _actor.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,"sean");_carrier=go.AddComponent<Carrier>();go.AddComponent<CombatVerbs>();go.AddComponent<HeroAbilitySystem>().BindHero("sean");
            var cc=go.GetComponent<CharacterController>();go.transform.position=new Vector3(0,-(cc.center.y-cc.height*.5f-cc.skinWidth)+.05f,-8);
            GameServices.Round.Register(_actor);GameServices.Match.StartMatch();GameServices.Round.BeginRound();Physics.SyncTransforms();
            _shoe=Track(new GameObject("Ember own slipper")).AddComponent<Slipper>();_shoe.OwnerSlot=1;_shoe.SeatOfOrigin=1;
            Assert.IsTrue(_shoe.HostForceEquip(_actor));yield return new WaitForSeconds(.3f);
        }
        private IEnumerator RetrieveRealThrow()
        {
            _shoe.HostThrow(_actor,_actor.transform.position+Vector3.up*.8f+Vector3.forward*.8f,Vector3.forward*2);
            Assert.AreEqual(SlipperState.InFlight,_shoe.State);
            float end=Time.time+4;
            while(_shoe.State==SlipperState.InFlight&&Time.time<end)yield return new WaitForFixedUpdate();
            Assert.AreEqual(SlipperState.Loose,_shoe.State,"The own throw did not actually land.");
            _actor.Teleport(_shoe.transform.position+Vector3.back*.3f);
            for(int i=0;i<3;i++)yield return new WaitForFixedUpdate();
            Assert.IsTrue(_shoe.HostGrab(_actor),"The actor could not manually retrieve its landed throw.");
        }
        [UnityTest,Timeout(60000)] public IEnumerator ScopedRecoveryAgesTheWindowAndNewerEmptyStateClosesIt()
        {
            yield return Open();var root=Track(new GameObject("Ember recovery receiver"));root.SetActive(false);
            var router=root.AddComponent<MatchRpc>();typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router,123L);
            var apply=typeof(MatchRpc).GetMethod("ApplyTimedKitState",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            bool Apply(TimedKitState state)=>(bool)apply.Invoke(router,new object[]{state,98f});
            var state=TimedKitState.Capture(Kit,new TimedKitSnapshot(Kit.AttackingSkill,0,passiveRemaining:4,passiveCapacity:4),1,
                new GameplayActionScope{Match=123,Round=GameServices.Match.RoundNumber,Epoch=_actor.MovementEpoch},1,100);
            var wrong=state;wrong.Scope.Round++;Assert.IsFalse(Apply(wrong));Assert.AreEqual(0,Kit.SteadyEmberRemaining);
            Assert.IsTrue(Apply(state));Assert.AreEqual(2,Kit.SteadyEmberRemaining);
            Assert.IsFalse(Apply(state));Assert.AreEqual(2,Kit.SteadyEmberRemaining);
            var empty=state;empty.Sequence=2;empty.PassiveRemaining=0;
            Assert.IsTrue(Apply(empty));Assert.AreEqual(0,Kit.SteadyEmberRemaining);
            Assert.IsFalse(Apply(state));Assert.AreEqual(0,Kit.SteadyEmberRemaining);
            GameServices.Round.EndRound();state.Sequence=3;Assert.IsFalse(Apply(state));
            empty.Sequence=3;Assert.IsTrue(Apply(empty));Assert.AreEqual(0,Kit.SteadyEmberRemaining);
        }
        [UnityTest,Timeout(60000)] public IEnumerator OwnThrowRetrievalGrantsOnceAndDropRegrabCannotRefresh()
        {
            yield return Open();Assert.AreEqual(0,Kit.SteadyEmberRemaining,"Initial equip is not a retrieval.");
            yield return RetrieveRealThrow();Assert.AreEqual(4,Kit.SteadyEmberRemaining,.05f);Assert.AreEqual(1.25f,Kit.ThrowChargeRate);
            yield return new WaitForSeconds(.5f);float remaining=Kit.SteadyEmberRemaining;
            Assert.IsTrue(_shoe.HostDisarm());yield return new WaitForFixedUpdate();Assert.IsTrue(_shoe.HostGrab(_actor));
            _carrier.NotifyHolding(_shoe);
            Assert.LessOrEqual(Kit.SteadyEmberRemaining,remaining,"Drop/regrab or duplicate notification refreshed the reward.");
            yield return new WaitForSeconds(4);Assert.AreEqual(0,Kit.SteadyEmberRemaining);Assert.AreEqual(1,Kit.ThrowChargeRate);
        }
        [UnityTest,Timeout(60000)] public IEnumerator ActualChargeUsesOnePointTwoFiveRateAndTheSameMaximumThenConsumes()
        {
            yield return Open();yield return RetrieveRealThrow();
            _actor.Teleport(new Vector3(0,_actor.transform.position.y,-8));_actor.Intent.AimPoint=new Vector3(0,.1f,0);
            yield return new WaitForSeconds(.5f);Assert.IsTrue(GameServices.Round.CanThrow(_actor));
            _actor.Intent.Set(Verb.SpecialAbility,true);float end=Time.time+1;
            while(!_carrier.IsCharging&&Time.time<end)yield return null;
            Assert.IsTrue(_carrier.IsCharging);float t=Time.time,start=_carrier.ChargeRatio;
            yield return new WaitForSeconds(.2f);
            float expected=(Time.time-t)*1.25f/Balance.ChargeFullTime;
            Assert.AreEqual(expected,_carrier.ChargeRatio-start,.035f,"The actual held input lost its passive rate.");
            yield return new WaitForSeconds(1);Assert.AreEqual(1,_carrier.ChargeRatio,.001f,"The boost cannot raise maximum power.");
            _actor.Intent.Set(Verb.SpecialAbility,false);yield return null;yield return null;
            Assert.AreEqual(SlipperState.InFlight,_shoe.State);Assert.AreEqual(0,Kit.SteadyEmberRemaining);
        }
        [UnityTest,Timeout(60000)] public IEnumerator AutomaticEquipAndAnUnthrownDropDoNotGrantAndRoundResetClears()
        {
            yield return Open();Assert.IsTrue(_shoe.HostDisarm());yield return new WaitForFixedUpdate();
            Assert.IsTrue(_shoe.HostGrab(_actor));Assert.AreEqual(0,Kit.SteadyEmberRemaining);
            yield return RetrieveRealThrow();Assert.Greater(Kit.SteadyEmberRemaining,0);
            Kit.ResetForRound(Context);Assert.AreEqual(0,Kit.SteadyEmberRemaining);
            Assert.IsTrue(_shoe.HostForceEquip(_actor));Assert.AreEqual(0,Kit.SteadyEmberRemaining);
        }
    }
}
