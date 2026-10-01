using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ClosedCircuitTests
    {
        private readonly List<GameObject> _built=new List<GameObject>();
        private INetProvider _old;
        [UnitySetUp] public IEnumerator Before(){_old=NetAuthority.Provider;NetAuthority.Provider=null;yield return PlayModeWorld.Reset();}
        [UnityTearDown] public IEnumerator After(){yield return PlayModeWorld.Reset();NetAuthority.Provider=_old;}
        [TearDown] public void Cleanup(){foreach(var go in _built)if(go!=null)Object.DestroyImmediate(go);_built.Clear();}
        private GameObject Track(GameObject go){_built.Add(go);return go;}
        private CharacterMotor Body(int seat,Vector3 at)
        {
            var body=Track(new GameObject("Circuit seat "+seat)).AddComponent<CharacterMotor>();body.enabled=false;body.PlayerSlot=seat;
            var capsule=body.GetComponent<CharacterController>();capsule.height=1.6f;capsule.radius=.4f;capsule.center=Vector3.up*.8f;
            body.Teleport(at);body.Intent.Parked=false;GameServices.Round.Register(body);return body;
        }
        private AbilityContext Stage(out ZackHeroKit kit,out CharacterMotor target)
        {
            GameServices.Ensure();GameServices.Round.Clear();
            var floor=Track(GameObject.CreatePrimitive(PrimitiveType.Cube));floor.name="Circuit floor";floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(30,1,30);
            var caster=Body(0,Vector3.up*.1f);target=Body(1,new Vector3(0,.1f,3));
            Body(2,new Vector3(2,.1f,3));Body(3,new Vector3(-8,.1f,8));
            var system=caster.gameObject.AddComponent<HeroAbilitySystem>();system.enabled=false;system.BindHero("zack");
            GameServices.Match.ApplySnapshot(new int[4],1,true);GameServices.Round.ApplySnapshot(100,true,0,true);
            caster.IsDefender=true;target.IsDefender=false;
            caster.transform.rotation=Quaternion.identity;caster.Intent.AimPoint=target.transform.position+Vector3.up*.8f;
            kit=(ZackHeroKit)system.Kit;var ctx=new AbilityContext(caster,null,null);kit.SetRole(true,ctx);
            Physics.SyncTransforms();return ctx;
        }
        private static void Overclock(ZackHeroKit kit)=>typeof(ZackHeroKit).GetMethod("RestoreOverclock",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(kit,null);
        [Test] public void LockCommitsOnlyAfterMaintainedAimWithoutRooting()
        {
            var ctx=Stage(out var kit,out var target);
            Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(ctx));Assert.AreEqual(0,kit.Skill2.CooldownRemaining);
            Assert.IsFalse(kit.Skill2.IsWindingUp);Assert.IsTrue(ctx.Motor.CanAct());
            kit.Skill2.Tick(ctx,.39f);Assert.IsFalse(target.IsZapped);
            kit.Skill2.Tick(ctx,.02f);Assert.IsTrue(target.IsZapped);Assert.AreEqual(2,target.ZappedLeft,.001f);
            Assert.AreEqual(35,kit.Skill2.CooldownRemaining,.001f);Assert.AreEqual(CircuitPhase.Idle,kit.CircuitStage);
        }
        [Test] public void BrokenAimAndNewWallCancelWithoutSpendingCooldown()
        {
            var ctx=Stage(out var kit,out var target);Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(ctx));
            ctx.Motor.Intent.AimPoint=Vector3.left*5;kit.Skill2.Tick(ctx,.2f);
            Assert.AreEqual(CircuitPhase.Idle,kit.CircuitStage);Assert.IsFalse(target.IsZapped);Assert.AreEqual(0,kit.Skill2.CooldownRemaining);
            ctx.Motor.Intent.AimPoint=target.transform.position+Vector3.up*.8f;
            Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(ctx));
            var wall=Track(GameObject.CreatePrimitive(PrimitiveType.Cube));wall.transform.position=new Vector3(0,1,1.5f);wall.transform.localScale=new Vector3(2,2,.2f);Physics.SyncTransforms();
            kit.Skill2.Tick(ctx,.4f);Assert.IsFalse(target.IsZapped);Assert.AreEqual(0,kit.Skill2.CooldownRemaining);
        }
        [Test] public void RangeBehindAndRoundEndCannotCompleteALock()
        {
            var ctx=Stage(out var kit,out var target);
            target.Teleport(new Vector3(0,.1f,7));ctx.Motor.Intent.AimPoint=target.transform.position+Vector3.up*.8f;Physics.SyncTransforms();
            Assert.AreNotEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(ctx));
            target.Teleport(new Vector3(0,.1f,-3));ctx.Motor.Intent.AimPoint=target.transform.position+Vector3.up*.8f;Physics.SyncTransforms();
            Assert.AreNotEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(ctx));
            target.Teleport(new Vector3(0,.1f,3));ctx.Motor.Intent.AimPoint=target.transform.position+Vector3.up*.8f;Physics.SyncTransforms();
            Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(ctx));
            GameServices.Round.ApplySnapshot(0,false,0,true);kit.Skill2.Tick(ctx,.5f);
            Assert.IsFalse(target.IsZapped);Assert.AreEqual(CircuitPhase.Idle,kit.CircuitStage);
        }
        [Test] public void OverclockRequiresDifferentTargetAndCannotRefillAThirdCast()
        {
            var ctx=Stage(out var kit,out var first);Overclock(kit);
            Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(ctx));kit.Skill2.Tick(ctx,.4f);
            Assert.AreEqual(CircuitPhase.Followup,kit.CircuitStage);Assert.IsTrue(first.IsZapped);
            float cooldown=kit.Skill2.CooldownRemaining;kit.ApplyObjectiveCooldown(20,false,true);
            Assert.AreEqual(cooldown,kit.Skill2.CooldownRemaining,"Amped-Up cannot refill the circuit sequence.");
            var second=GameServices.Round.PlayerAt(2);ctx.Motor.Intent.AimPoint=second.transform.position+Vector3.up*.8f;
            Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(ctx));kit.Skill2.Tick(ctx,.39f);Assert.IsFalse(second.IsZapped);
            kit.Skill2.Tick(ctx,.02f);Assert.IsTrue(second.IsZapped);Assert.AreEqual(CircuitPhase.Idle,kit.CircuitStage);
            Assert.AreNotEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(ctx));
        }
        [Test] public void RepeatingTheFirstVictimEndsTheOptionalWindow()
        {
            var ctx=Stage(out var kit,out var target);Overclock(kit);kit.CastSkill2(ctx);kit.Skill2.Tick(ctx,.4f);
            float left=target.ZappedLeft;kit.CastSkill2(ctx);
            Assert.AreEqual(CircuitPhase.Idle,kit.CircuitStage);Assert.AreEqual(left,target.ZappedLeft);
            Assert.Greater(kit.Skill2.CooldownRemaining,34);
        }
        private sealed class RemoteHost : INetProvider
        {public bool IsHost=>true;public bool IsNetworked=>true;public int LocalSlot=>3;public int LocalPeerId=>0;public bool IsSeatlessReferee=>false;}
        private sealed class Replica : INetProvider
        {public bool IsHost=>false;public bool IsNetworked=>true;public int LocalSlot=>0;public int LocalPeerId=>1;public bool IsSeatlessReferee=>false;}
        [Test] public void RemoteAimMustBeFreshScopedToTheAcquisitionAndActuallyMaintained()
        {
            var ctx=Stage(out var kit,out var target);NetAuthority.Provider=new RemoteHost();ctx.Motor.IsBot=false;
            Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(ctx));
            var aim=new ZackCircuitAim{Seat=0,Scope=new GameplayActionScope{Match=1,Round=1,Epoch=0},Episode=kit.CircuitEpisode,Sequence=1,Point=Vector3.left*5};
            Assert.IsTrue(kit.ReceiveCircuitAim(ctx.Motor,aim));Assert.IsFalse(kit.ReceiveCircuitAim(ctx.Motor,aim));
            kit.Skill2.Tick(ctx,.1f);Assert.IsFalse(target.IsZapped);Assert.AreEqual(CircuitPhase.Idle,kit.CircuitStage);
            Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(ctx));
            Assert.IsFalse(kit.ReceiveCircuitAim(ctx.Motor,aim),"Old episode must not steer a new lock.");
            GameServices.Round.ApplySnapshot(GameServices.Round.TimeLeft-.3f,true,0,true);kit.Skill2.Tick(ctx,.3f);
            Assert.IsFalse(target.IsZapped);Assert.AreEqual(CircuitPhase.Idle,kit.CircuitStage);
        }
        [Test] public void RecoveryAgesTheTellWithoutApplyingAHitOrReopeningAnExpiredWindow()
        {
            var ctx=Stage(out var kit,out var target);kit.CastSkill2(ctx);var state=kit.CaptureCircuit();
            state.Seat=0;state.Sequence=1;state.Scope=new GameplayActionScope{Match=1,Round=1,Epoch=0};state.RoundClock=100;
            NetAuthority.Provider=new Replica();Assert.IsTrue(kit.RestoreCircuit(ctx.Motor,state,.2f));
            Assert.AreEqual(.2f,kit.CircuitRemaining,.001f);kit.Skill2.Tick(ctx,.3f);Assert.IsFalse(target.IsZapped);
            Assert.IsTrue(kit.RestoreCircuit(ctx.Motor,state,.8f));Assert.AreEqual(CircuitPhase.Idle,kit.CircuitStage);Assert.IsFalse(target.IsZapped);
        }
        [UnityTest] public IEnumerator TellIsVisibleWhileAcquiringAndDisappearsOnCancellation()
        {
            var ctx=Stage(out var kit,out var target);kit.CastSkill2(ctx);yield return null;
            var tell=ctx.Motor.GetComponentInChildren<ZackCircuitTell>();Assert.IsNotNull(tell);
            var line=tell.GetComponent<LineRenderer>();Assert.IsTrue(line.enabled);Assert.AreEqual(4,line.positionCount);
            Assert.Less(Vector3.Distance(line.GetPosition(3),target.transform.position+Vector3.up*.8f),.001f);
            Assert.IsEmpty(tell.GetComponents<Collider>());
            ctx.Motor.Intent.AimPoint=Vector3.left*5;kit.Skill2.Tick(ctx,.1f);yield return null;
            Assert.IsFalse(line.enabled);Assert.IsFalse(target.IsZapped);
        }
    }
}
