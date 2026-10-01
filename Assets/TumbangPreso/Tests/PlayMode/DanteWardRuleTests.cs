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
