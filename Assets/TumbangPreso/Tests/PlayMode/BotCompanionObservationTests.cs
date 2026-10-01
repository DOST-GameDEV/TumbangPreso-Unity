using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Unity.Profiling;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class BotCompanionObservationTests
    {
        private CharacterMotor _observer,_owner,_companion;
        private AIController _brain;
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private CharacterMotor Body(int seat,Vector3 at)
        {
            var body=new GameObject("Observed body "+seat).AddComponent<CharacterMotor>();
            body.enabled=false;body.PlayerSlot=seat;body.Mode=GameMode.HeroStrike;body.transform.position=at;return body;
        }
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();GameServices.Ensure();GameServices.Round.Clear();
            _owner=Body(0,Vector3.zero);_observer=Body(1,new Vector3(0,0,-2));
            GameServices.Round.Register(_owner);GameServices.Round.Register(_observer);
            GameServices.Match.ApplySnapshot(new int[4],2,true);GameServices.Round.ApplySnapshot(100,true,1,true);
            _companion=Body(4,new Vector3(0,0,2));Assert.IsTrue(GameServices.Round.RegisterCompanion(_companion));
            _brain=_observer.gameObject.AddComponent<AIController>();_brain.enabled=false;_brain.SeatDifficulty=Difficulty.Normal;
        }
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        private static void Observe(AIController brain,float dt=.01f)
            => typeof(AIController).GetMethod("Observe",Hidden).Invoke(brain,new object[]{dt});
        private static Vector3 At(AIController brain,CharacterMotor body)
            => (Vector3)typeof(AIController).GetMethod("At",Hidden).Invoke(brain,new object[]{body});
        private object Read(string name,params object[] args)
            => typeof(AIController).GetMethod(name,Hidden).Invoke(_brain,args);
        private void BlindFixture()
        {
            _observer.transform.position=Vector3.zero;
            _owner.transform.position=new Vector3(6,0,6);
            _owner.HoldingSlipper=true;_companion.gameObject.SetActive(false);
            var can=new GameObject("Blind bot can").AddComponent<Lata>();can.enabled=false;
            GameServices.Round.Lata=can;_observer.ApplyHaunted();
            Assert.IsTrue(_observer.IsHaunted);Assert.IsTrue(_owner.IsTaggable());
        }
        [Test] public void AnUnseenActorHasNoPositionWhileHaunted()
        {
            BlindFixture();Observe(_brain);
            Assert.IsNull(Read("At",_owner),"Unknown actor position fell back to live truth.");
            Assert.IsNull(Read("AheadOf",_owner,.35f));
            Assert.AreEqual(Vector3.zero,At(_brain,_observer));
        }
        [Test] public void HauntedSelectorsCannotAcquireAFarActor()
        {
            BlindFixture();Observe(_brain);
            Assert.IsNull(Read("TagTarget"),"Tag selection sees through Haunted.");
            Assert.IsNull(Read("LiveThreat"));
            Assert.AreEqual(0,Read("VictimsUnder",Vector3.zero,20f,false));
            Assert.AreEqual(0,Read("PaeteSentryAim",GameServices.Round,Vector3.zero));
            Assert.IsFalse((bool)Read("Facing",_owner,70f));
        }
        [Test] public void HauntedRemembersOldPositionsWithoutTrackingUnseenMovement()
        {
            BlindFixture();_observer.ClearStatuses();_owner.transform.position=Vector3.right*2;
            Observe(_brain);_observer.ApplyHaunted();_owner.transform.position=new Vector3(6,0,6);
            Observe(_brain,10);
            Assert.AreEqual(Vector3.right*2,At(_brain,_owner),"Blind memory followed live distant movement.");
            _owner.transform.position=Vector3.right*3;Observe(_brain,.01f);
            Assert.Greater(At(_brain,_owner).x,2);Assert.Less(At(_brain,_owner).x,3);
            _owner.transform.position=new Vector3(6,0,6);_observer.ClearStatuses();Observe(_brain,10);
            Assert.AreEqual(_owner.transform.position,At(_brain,_owner));
        }
        [Test] public void AnUnseenReplacementCannotInheritADeadCompanionsMemory()
        {
            Observe(_brain);_observer.ApplyHaunted();
            GameServices.Round.UnregisterCompanion(_companion);Object.DestroyImmediate(_companion.gameObject);
            _companion=Body(4,new Vector3(10,0,10));Assert.IsTrue(GameServices.Round.RegisterCompanion(_companion));
            Observe(_brain);
            Assert.IsNull(Read("At",_companion),"Unseen replacement inherited or exposed a position.");
            _companion.transform.position=new Vector3(0,0,2);Observe(_brain);
            Assert.AreEqual(_companion.transform.position,At(_brain,_companion));
        }
        [TestCase(Difficulty.Normal)] [TestCase(Difficulty.Astig)]
        public void CompanionBeliefUsesTheSameReactionLagAsPlayers(Difficulty tier)
        {
            _brain.SeatDifficulty=tier;Observe(_brain);
            _owner.transform.position+=Vector3.right*4;_companion.transform.position+=Vector3.right*4;
            _observer.transform.position+=Vector3.right*3;Observe(_brain);
            float playerX=At(_brain,_owner).x,companionX=At(_brain,_companion).x;
            Assert.Greater(playerX,0);Assert.Less(playerX,4);
            Assert.AreEqual(playerX,companionX,.0001f,"Companion movement bypassed the bot's reaction-lag model.");
            Assert.AreEqual(3,At(_brain,_observer).x,.0001f,"Own position must stay exact.");
        }
        [Test] public void AReplacementInTheSameSeatStartsWithItsOwnPosition()
        {
            Observe(_brain);_companion.transform.position+=Vector3.right*4;Observe(_brain);
            GameServices.Round.UnregisterCompanion(_companion);Object.DestroyImmediate(_companion.gameObject);
            _companion=Body(4,new Vector3(6,0,2));Assert.IsTrue(GameServices.Round.RegisterCompanion(_companion));
            Assert.AreEqual(6,At(_brain,_companion).x,.0001f,"A fresh body inherited a dead body's belief.");
            Observe(_brain);Assert.AreEqual(6,At(_brain,_companion).x,.0001f);
            _companion.transform.position+=Vector3.right;Observe(_brain);
            Assert.Greater(At(_brain,_companion).x,6);Assert.Less(At(_brain,_companion).x,7);
        }
        [Test] public void ACompanionBotStillKnowsItsOwnFeetWithoutReactionLag()
        {
            var brain=_companion.gameObject.AddComponent<AIController>();brain.enabled=false;brain.SeatDifficulty=Difficulty.Astig;
            Observe(brain);_companion.transform.position+=Vector3.right*3;Observe(brain);
            Assert.AreEqual(3,At(brain,_companion).x,.0001f);
        }
        [TestCase(Difficulty.Normal)] [TestCase(Difficulty.Astig)]
        public void TagRankingUsesObservedDepthUntilTheBotReacts(Difficulty tier)
        {
            _brain.SeatDifficulty=tier;
            _observer.transform.position=Vector3.zero;
            _owner.transform.position=Vector3.right;
            _companion.transform.position=Vector3.right*5;
            _owner.HoldingSlipper=true;_companion.HoldingSlipper=true;
            var can=new GameObject("Observed depth can").AddComponent<Lata>();can.enabled=false;
            GameServices.Round.Lata=can;
            Assert.IsTrue(can.IsUpright);Assert.IsTrue(_owner.IsTaggable());Assert.IsTrue(_companion.IsTaggable());
            Observe(_brain);
            _owner.transform.position=Vector3.right*6;
            _companion.transform.position=Vector3.right;
            var select=typeof(AIController).GetMethod("TagTarget",Hidden);
            Assert.AreSame(_owner,select.Invoke(_brain,null),"Live depth leaked into target ranking before observation.");
            Observe(_brain,10);
            typeof(AIController).GetField("_lastTagTarget",Hidden).SetValue(_brain,null);
            Assert.AreSame(_companion,select.Invoke(_brain,null),"Fresh observation must still update target ranking.");
        }
        [Test] public void WarmPerFrameObservationDoesNotAllocateAnActorEnumerator()
        {
            var observe=(System.Action<float>)System.Delegate.CreateDelegate(typeof(System.Action<float>),_brain,
                typeof(AIController).GetMethod("Observe",Hidden));
            for(int i=0;i<5;i++)observe(.01f);
            using(var calibration=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"GC.Alloc",100,ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            { Assert.IsTrue(calibration.Valid);System.GC.KeepAlive(new byte[4096]);calibration.Stop();Assert.Greater(calibration.Count,0); }
            int allocations;
            using(var recorder=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"GC.Alloc",256,ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            { for(int i=0;i<100;i++)observe(.01f);recorder.Stop();allocations=recorder.Count; }
            Assert.AreEqual(0,allocations,"The per-frame bot observer still allocates an actor enumerator.");
        }
    }
}
