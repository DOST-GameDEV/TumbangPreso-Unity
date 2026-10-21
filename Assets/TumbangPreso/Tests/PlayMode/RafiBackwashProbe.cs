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
    public sealed class RafiBackwashProbe
    {
        private readonly List<GameObject> _built=new List<GameObject>();
        private CharacterMotor _actor;private Carrier _carrier;private Slipper _shoe;
        private int _seat,_capture;private INetProvider _provider;private CustomRules _rules;private bool _pinned;
        private RafiHeroKit Kit=>(RafiHeroKit)_actor.AbilitySystem.Kit;
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
            var can=Track(new GameObject("Backwash can"));can.transform.position=new Vector3(6,0,6);GameServices.Round.Lata=can.AddComponent<Lata>();
            var go=Track(new GameObject("Backwash Rafi",typeof(CharacterController)));
            _actor=go.AddComponent<CharacterMotor>();_actor.PlayerSlot=1;_actor.Mode=GameMode.HeroStrike;_actor.IsBot=false;
            _actor.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,"rafi");_carrier=go.AddComponent<Carrier>();go.AddComponent<CombatVerbs>();go.AddComponent<HeroAbilitySystem>().BindHero("rafi");
            var cc=go.GetComponent<CharacterController>();go.transform.position=new Vector3(0,-(cc.center.y-cc.height*.5f-cc.skinWidth)+.05f,-8);
            GameServices.Round.Register(_actor);GameServices.Match.StartMatch();GameServices.Round.BeginRound();Physics.SyncTransforms();
            _shoe=Track(new GameObject("Backwash own slipper")).AddComponent<Slipper>();_shoe.OwnerSlot=1;_shoe.SeatOfOrigin=1;
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
        [UnityTest,Timeout(60000)] public IEnumerator RealRetrievalBoostsActualMovementThenExpiresWithoutDropRefresh()
        {
            yield return Open();Assert.AreEqual(0,Kit.BackwashRemaining);
            _actor.Intent.Move=Vector2.up;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            var start=_actor.transform.position;float t=Time.fixedTime;
            for(int i=0;i<10;i++)yield return new WaitForFixedUpdate();
            float ordinary=Vector3.Distance(start,_actor.transform.position)/(Time.fixedTime-t);
            _actor.Intent.Move=Vector2.zero;yield return new WaitForFixedUpdate();
            yield return RetrieveRealThrow();Assert.AreEqual(1.5f,Kit.BackwashRemaining,.05f);
            _actor.Intent.Move=Vector2.up;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            start=_actor.transform.position;t=Time.fixedTime;
            for(int i=0;i<10;i++)yield return new WaitForFixedUpdate();
            float boosted=Vector3.Distance(start,_actor.transform.position)/(Time.fixedTime-t);
            Assert.Greater(ordinary,1);Assert.AreEqual(1.2f,boosted/ordinary,.035f,"Actual motor travel must receive the passive.");
            _actor.Intent.Move=Vector2.zero;yield return new WaitForFixedUpdate();float remaining=Kit.BackwashRemaining;
            Assert.IsTrue(_shoe.HostDisarm());_shoe.transform.position=_actor.transform.position+Vector3.forward*.3f;
            yield return new WaitForFixedUpdate();Assert.IsTrue(_shoe.HostGrab(_actor));_carrier.NotifyHolding(_shoe);
            Assert.LessOrEqual(Kit.BackwashRemaining,remaining);
            yield return new WaitForSeconds(1.6f);Assert.AreEqual(0,Kit.BackwashRemaining);Assert.AreEqual(1,Kit.MovementSpeedScale);
        }
        [UnityTest,Timeout(60000)] public IEnumerator NewThrowKeepsOriginalMovementWindowAndResetRetiresIt()
        {
            yield return Open();yield return RetrieveRealThrow();yield return new WaitForSeconds(.2f);
            float remaining=Kit.BackwashRemaining;
            _shoe.HostThrow(_actor,_actor.transform.position+Vector3.up*.8f+Vector3.forward*.8f,Vector3.forward*2);
            Assert.AreEqual(remaining,Kit.BackwashRemaining,.001f,"Movement reward is not a next-throw charge reward.");
            Kit.ResetForRound(Context);Assert.AreEqual(0,Kit.BackwashRemaining);Assert.AreEqual(1,Kit.MovementSpeedScale);
        }
        [UnityTest,Timeout(60000)] public IEnumerator InitialEquipAndUnthrownDropStayNeutral()
        {
            yield return Open();Assert.IsTrue(_shoe.HostDisarm());yield return new WaitForFixedUpdate();
            Assert.IsTrue(_shoe.HostGrab(_actor));Assert.AreEqual(0,Kit.BackwashRemaining);
            Assert.IsTrue(_shoe.HostForceEquip(_actor));Assert.AreEqual(0,Kit.BackwashRemaining);
            yield return RetrieveRealThrow();GameServices.Round.EndRound();yield return null;yield return null;
            Assert.AreEqual(0,Kit.BackwashRemaining);Assert.AreEqual(1,Kit.MovementSpeedScale);
        }
        [UnityTest,Timeout(60000)] public IEnumerator ScopedPassiveUpdatesSurviveSettledSkimWithoutReplayingTheLoad()
        {
            yield return Open();var root=Track(new GameObject("Backwash receiver"));root.SetActive(false);
            var router=root.AddComponent<MatchRpc>();typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(router,123L);
            var apply=typeof(MatchRpc).GetMethod("ApplyTimedKitState",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            bool Apply(TimedKitState state)=>(bool)apply.Invoke(router,new object[]{state,99.5f});
            var scope=new GameplayActionScope{Match=123,Round=GameServices.Match.RoundNumber,Epoch=_actor.MovementEpoch};
            var empty=TimedKitState.Capture(Kit,Kit.CaptureTimedKit(),1,scope,1,100);Assert.IsTrue(Apply(empty));
            var state=TimedKitState.Capture(Kit,new TimedKitSnapshot(Kit.AttackingSkill,0,passiveRemaining:1.5f,passiveCapacity:1.5f),1,scope,2,100);
            var wrong=state;wrong.Scope.Round++;Assert.IsFalse(Apply(wrong));
            Assert.IsTrue(Apply(state));Assert.AreEqual(1,Kit.BackwashRemaining);Assert.IsFalse(Kit.IsSkimLoaded);
            Assert.IsFalse(Apply(state));Assert.AreEqual(1,Kit.BackwashRemaining);
            empty.Sequence=3;Assert.IsTrue(Apply(empty));Assert.AreEqual(0,Kit.BackwashRemaining);
            Assert.IsFalse(Apply(state));
        }
    }
}
