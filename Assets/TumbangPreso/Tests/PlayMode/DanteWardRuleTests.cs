using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class DanteWardRuleTests
    {
        readonly List<GameObject> _built=new List<GameObject>();
        CharacterMotor _actor;
        [UnitySetUp] public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After()=>PlayModeWorld.Reset();
        [TearDown] public void Cleanup(){foreach(var go in _built)if(go!=null)Object.DestroyImmediate(go);_built.Clear();}
        GameObject Track(GameObject go){_built.Add(go);return go;}
        IEnumerator Open()
        {
            UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));GameServices.Ensure();GameServices.Round.Clear();
            var floor=Track(GameObject.CreatePrimitive(PrimitiveType.Cube));floor.transform.localScale=new Vector3(30,1,30);floor.transform.position=Vector3.down*.5f;
            var can=Track(new GameObject("Ward can"));can.transform.position=new Vector3(6,0,6);GameServices.Round.Lata=can.AddComponent<Lata>();
            var go=Track(new GameObject("Ward Dante",typeof(CharacterController)));
            _actor=go.AddComponent<CharacterMotor>();_actor.PlayerSlot=1;_actor.Mode=GameMode.HeroStrike;_actor.IsBot=false;
            go.AddComponent<Carrier>();go.AddComponent<CombatVerbs>();var system=go.AddComponent<HeroAbilitySystem>();system.BindHero("dante");
            var cc=go.GetComponent<CharacterController>();go.transform.position=new Vector3(0,-(cc.center.y-cc.height*.5f-cc.skinWidth)+.05f,-4);
            GameServices.Round.Register(_actor);GameServices.Match.StartMatch();GameServices.Round.BeginRound();Physics.SyncTransforms();
            yield return new WaitForSeconds(.2f);
        }
        [Test] public void WardAndBastionMatchCurrentWiki()
        {
            var kit=new DanteHeroKit();Assert.AreEqual("UNSTOPPABLE",kit.Skill1.Name);
            Assert.AreEqual(15,kit.Skill1.Duration);Assert.AreEqual(40,kit.Skill1.Cooldown);
            Assert.AreEqual("BASTION",kit.DefendingSkill.Name);Assert.AreEqual(7.5f,kit.DefendingSkill.Duration);
            Assert.AreEqual(35,kit.DefendingSkill.Cooldown);
        }
        [UnityTest] public IEnumerator FrozenPlayerCanPressUnstoppable()
        {
            yield return Open();_actor.ApplyStagger(2.5f,StunElement.Ice,int.MaxValue);Assert.IsTrue(_actor.IsFrozen);
            var context=new AbilityContext(_actor,_actor.GetComponent<Carrier>(),_actor.GetComponent<CombatVerbs>());
            Assert.IsFalse(_actor.AbilitySystem.Kit.TryActivateSkill2(context),"The cleanse exception cannot enable an ordinary frozen skill.");
            _actor.Intent.Set(Verb.Skill1,true);yield return null;yield return null;_actor.Intent.Set(Verb.Skill1,false);
            Assert.IsFalse(_actor.IsFrozen,"A cleanse signature must accept the frozen player's press.");
            Assert.IsTrue(_actor.AbilitySystem.IsImmuneToStatuses);
        }
        [UnityTest] public IEnumerator AcceptedWardCannotEraseANewerTag()
        {
            yield return Open();_actor.ApplyStagger(StatusRules.TaggedSeconds);Assert.IsTrue(_actor.IsTagged);
            var ctx=new AbilityContext(_actor,_actor.GetComponent<Carrier>(),_actor.GetComponent<CombatVerbs>());
            Assert.IsFalse(_actor.AbilitySystem.Kit.TryActivateSkill1(ctx),"Tagged cannot initiate a cleanse.");
            _actor.AbilitySystem.Kit.Skill1.Activate(ctx);
            Assert.IsTrue(_actor.IsTagged,"Delayed accepted presentation cannot erase a newer tag.");
        }
        [UnityTest] public IEnumerator HostValidatedFrozenCastUsesTheSameCleanse()
        {
            yield return Open();_actor.ApplyStagger(2.5f,StunElement.Ice,int.MaxValue);
            var result=_actor.AbilitySystem.ApplyNetworkCast(HeroAbilitySystem.Slot.Skill1,
                _actor.transform.position,Vector3.forward,Vector3.forward,0,true,"dante_skill1",false);
            Assert.AreEqual(HeroKit.CastOutcome.Cast,result);Assert.IsFalse(_actor.IsFrozen);
        }
        [UnityTest] public IEnumerator WardExpiresAtTheCurrentFifteenSeconds()
        {
            yield return Open();_actor.AbilitySystem.enabled=false;
            var ctx=new AbilityContext(_actor,_actor.GetComponent<Carrier>(),_actor.GetComponent<CombatVerbs>());
            var kit=_actor.AbilitySystem.Kit;Assert.IsTrue(kit.TryActivateSkill1(ctx));
            kit.Tick(ctx,14.9f);Assert.IsTrue(_actor.AbilitySystem.IsImmuneToStatuses);
            kit.Tick(ctx,.11f);Assert.IsFalse(_actor.AbilitySystem.IsImmuneToStatuses);
        }
        [UnityTest] public IEnumerator TheCleanseShattersWhatHeHadAndARefusalSplashes()
        {
            yield return Open();_actor.ApplyConcussed();Assert.IsTrue(_actor.IsConcussed);
            var ctx=new AbilityContext(_actor,_actor.GetComponent<Carrier>(),_actor.GetComponent<CombatVerbs>());
            Assert.IsTrue(_actor.AbilitySystem.Kit.TryActivateSkill1(ctx));
            Assert.IsFalse(_actor.IsConcussed);
            Assert.IsNotNull(Object.FindAnyObjectByType<Visual.DanteStatusShatter>(),"What he carried shatters off him.");
            StatusKind refused=StatusKind.Tagged;_actor.StatusRefused+=(m,k)=>refused=k;
            _actor.ApplyConcussed();
            Assert.IsFalse(_actor.IsConcussed,"Immune.");
            Assert.AreEqual(StatusKind.Concussed,refused,"The refusal is raised so the plate can flash.");
            yield return new WaitForSeconds(1.3f);
            Assert.IsNull(Object.FindAnyObjectByType<Visual.DanteStatusShatter>(),"The shell crumbles into the court and is gone.");
        }
        [UnityTest] public IEnumerator BastionStandsAsTheReflectAreaAndSinksWhenItEnds()
        {
            yield return Open();
            var ctx=new AbilityContext(_actor,_actor.GetComponent<Carrier>(),_actor.GetComponent<CombatVerbs>());
            var kit=_actor.AbilitySystem.Kit;_actor.AbilitySystem.enabled=false;
            kit.DefendingSkill.Activate(ctx);
            yield return new WaitForSeconds(.6f);
            var wall=Object.FindAnyObjectByType<Visual.DanteBastionWall>();
            Assert.IsNotNull(wall,"Bastion stands a wall.");
            Assert.AreEqual(7,wall.SlabCount,"Seven slabs.");
            if(wall.DrawsApproved)
            {
                // The approved wall (owner 2026-10-19, "bastion b8 can go in game now") is the bench effect at its own size,
                // and its size IS the rule: if one of these moves, the picture no longer shows where slippers turn back.
                Assert.AreEqual(GeoRules.BarrierWidth,Visual.FxStudyDanteBastion.Width,1e-4f,"The approved wall is as wide as the reflect area.");
                Assert.AreEqual(GeoRules.BarrierForward,Visual.FxStudyDanteBastion.Forward,1e-4f,"It stands on the reflect line.");
                Assert.AreEqual(Visual.DanteBastionWall.FieldTop,Visual.FxStudyDanteBastion.FieldTop,1e-4f,"Its field reaches as high as the reflect area.");
                Assert.AreEqual(GeoRules.BarrierSeconds,Visual.FxStudyDanteBastion.Life,1e-4f,"It lasts as long as the skill.");
                Transform face=null;foreach(var t in wall.GetComponentsInChildren<Transform>(true))if(t.name=="wall")face=t;
                Assert.IsNotNull(face,"The approved wall's slabs.");
                Vector3 local=_actor.transform.InverseTransformPoint(face.position);
                Assert.AreEqual(0f,local.x,.01f);Assert.AreEqual(GeoRules.BarrierForward,local.z,.01f,"Drawn on the line it reflects on.");
                Assert.Greater(Vector3.Dot(face.forward,_actor.transform.forward),.999f,"Its face turned to the thrower.");
                var slabs=face.Find("slabs").GetComponent<Renderer>();Assert.IsTrue(slabs.enabled&&slabs.gameObject.activeInHierarchy,"The slabs stand.");
            }
            else
            {
                var field=wall.transform.Find("BastionHeatField");
                Assert.IsNotNull(field,"The heat field.");
                var b=field.GetComponent<Renderer>().bounds;
                Assert.AreEqual(GeoRules.BarrierWidth,b.size.x,.05f,"The field is exactly as wide as the reflect area.");
                Assert.AreEqual(Visual.DanteBastionWall.FieldTop,b.max.y-_actor.transform.position.y+(_actor.transform.position.y-Slipper.GroundY(_actor.transform.position)),.15f);
            }
            kit.Tick(ctx,GeoRules.BarrierSeconds+.1f);
            // The approved wall is eaten and sunk by 0.63 s and its last dust is gone by 0.95 s; ASTRA's sinks in 0.6 s.
            yield return new WaitForSeconds(wall.DrawsApproved?1.2f:.8f);
            Assert.IsTrue(wall==null,"It sinks into the court and is gone.");
        }
        [UnityTest] public IEnumerator PauseAndWarmupStillRefuseCleanse()
        {
            yield return Open();_actor.ApplyStagger(2.5f,StunElement.Ice,int.MaxValue);
            var ctx=new AbilityContext(_actor,_actor.GetComponent<Carrier>(),_actor.GetComponent<CombatVerbs>());
            var kit=_actor.AbilitySystem.Kit;kit.PracticeMode=true;Assert.IsFalse(kit.TryActivateSkill1(ctx));kit.PracticeMode=false;
            PresentationClock.RequestScale(0);
            try { Assert.IsFalse(kit.TryActivateSkill1(ctx));Assert.IsTrue(_actor.IsFrozen); }
            finally { PresentationClock.RequestScale(1); }
        }
    }
}
