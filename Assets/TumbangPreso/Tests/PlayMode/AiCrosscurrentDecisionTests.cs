using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class AiCrosscurrentDecisionTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private INetProvider _provider;private CustomRules _rules;private bool _pinned;
        private sealed class Solo:INetProvider
        {public bool IsHost=>true;public bool IsNetworked=>false;public int LocalSlot=>0;public int LocalPeerId=>0;public bool IsSeatlessReferee=>false;}
        [UnitySetUp]public IEnumerator Before()
        {_provider=NetAuthority.Provider;_rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;yield return PlayModeWorld.Reset();NetAuthority.Provider=new Solo();SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));}
        [UnityTearDown]public IEnumerator After()
        {yield return PlayModeWorld.Reset();NetAuthority.Provider=_provider;SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();}
        private static void Write(object owner,string field,object value)=>owner.GetType().GetField(field,Hidden).SetValue(owner,value);
        [TestCase(true),TestCase(false)]
        public void IncomingFlightSetsTheRealCurrentsAimRatherThanTheCan(bool incoming)
            => CheckIncoming(incoming,false);
        [Test]
        public void CompetingReadyPowersStillProduceANormalCast()=>CheckIncoming(true,true);
        [Test]
        public void AcquiredAimedPowerKeepsItsHoldAcrossTheCadenceGate()=>CheckIncoming(false,true,true);
        private static void CheckIncoming(bool incoming,bool competing,bool checkHold=false)
        {
            GameServices.Ensure();GameServices.Round.Clear();GameServices.Match.ApplySnapshot(new int[4],1,true);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=Vector3.down*.1f;floor.transform.localScale=new Vector3(30,.2f,30);
            var can=new GameObject("Crosscurrent decision can").AddComponent<Lata>();GameServices.Round.Lata=can;
            CharacterMotor Body(int slot,Vector3 at)
            {
                var go=new GameObject("Crosscurrent decision actor"+slot,typeof(CharacterController));var capsule=go.GetComponent<CharacterController>();
                capsule.height=1.6f;capsule.radius=.35f;capsule.center=Vector3.up*.8f;
                var body=go.AddComponent<CharacterMotor>();body.enabled=false;body.PlayerSlot=slot;body.Mode=GameMode.HeroStrike;body.IsBot=true;body.SpawnPosition=at;
                go.AddComponent<Carrier>().enabled=false;go.AddComponent<CombatVerbs>().enabled=false;GameServices.Round.Register(body);return body;
            }
            var actor=Body(0,new Vector3(0,.1f,0));var thrower=Body(1,new Vector3(0,.1f,8));
            actor.IsDefender=true;
            var powers=actor.gameObject.AddComponent<HeroAbilitySystem>();powers.enabled=false;powers.BindHero("rafi");
            GameServices.Round.BeginRound();actor.transform.position=actor.SpawnPosition;thrower.transform.position=thrower.SpawnPosition;
            actor.IsDefender=true;thrower.IsDefender=false;actor.Intent.Parked=false;
            typeof(HeroAbilitySystem).GetMethod("Update",Hidden).Invoke(powers,null);
            Assert.IsTrue(powers.Kit.IsDefending,"Use the actual role-bound wall opportunity.");
            Assert.IsTrue(actor.IsDefender);Assert.IsTrue(actor.CanAct());
            var shoe=new GameObject("Incoming flight away from the can").AddComponent<Slipper>();shoe.enabled=false;shoe.OwnerSlot=1;shoe.SeatOfOrigin=1;
            Assert.IsTrue(shoe.HostForceEquip(thrower));
            shoe.HostThrow(thrower,new Vector3(3,.7f,0),Vector3.right*(incoming?-4:4));
            Assert.AreEqual(SlipperState.InFlight,shoe.State);
            if(competing)
            {
                var target=Body(2,new Vector3(0,.1f,3));target.transform.position=target.SpawnPosition;target.IsDefender=false;target.Intent.Parked=false;
                var held=new GameObject("Taggable carrier while a flight approaches").AddComponent<Slipper>();held.enabled=false;held.OwnerSlot=2;held.SeatOfOrigin=2;
                Assert.IsTrue(held.HostForceEquip(target));Assert.IsTrue(target.IsTaggable(),"The wall opportunity needs a real nearby taggable carrier.");
            }
            var brain=actor.gameObject.AddComponent<AIController>();brain.enabled=false;
            typeof(AIController).GetProperty("Plan").SetValue(brain,AiPlan.Guard);
            Write(brain,"_roundLiveFor",100f);Write(brain,"_abilityCadenceLeft",0f);Write(brain,"_driving",competing);
            actor.Intent.AimPoint=Vector3.forward*6; // Ordinary can-facing aim before the skill choice.
            var requested=checkHold?Verb.Skill2:Verb.Skill1;
            for(int tick=0;tick<100&&!actor.Intent.Pressed(requested);tick++)
            {
                ((System.Collections.Generic.HashSet<Verb>)typeof(AIController).GetField("_touched",Hidden).GetValue(brain)).Clear();
                typeof(AIController).GetMethod("StepHeroAbilities",Hidden).Invoke(brain,new object[]{actor.Intent,.02f});
            }
            if(checkHold)
            {
                Assert.IsTrue(actor.Intent.Pressed(Verb.Skill2),"The wall must acquire its normal aimed input first.");
                Assert.IsTrue(powers.Kit.Skill2.HoldToAim);
                typeof(HeroAbilitySystem).GetMethod("Update",Hidden).Invoke(powers,null);actor.Intent.CommitFrame();
                float ramp=powers.Kit.Skill2.AimRampSeconds;
                int holdTicks=Mathf.CeilToInt(ramp/.02f)-1;
                Assert.Greater(holdTicks,0);
                for(int tick=0;tick<holdTicks;tick++)
                {
                    if(tick==4)shoe.HostThrow(thrower,new Vector3(3,.7f,0),Vector3.left*4);
                    ((System.Collections.Generic.HashSet<Verb>)typeof(AIController).GetField("_touched",Hidden).GetValue(brain)).Clear();
                    typeof(AIController).GetMethod("StepHeroAbilities",Hidden).Invoke(brain,new object[]{actor.Intent,.02f});
                    Assert.IsTrue(actor.Intent.Pressed(Verb.Skill2),$"An acquired aimed power released: tick={tick} ramp={powers.Kit.Skill2.AimRampSeconds} held={((System.Collections.Generic.Dictionary<Verb,float>)typeof(AIController).GetField("_aimHeld",Hidden).GetValue(brain))[Verb.Skill2]} ready={powers.Kit.Skill2.IsReady} active={powers.Kit.Skill2.IsActive}");
                    Assert.IsFalse(actor.Intent.Pressed(Verb.Skill1),"A newly eligible current must not take over an acquired wall hold.");
                    typeof(HeroAbilitySystem).GetMethod("Update",Hidden).Invoke(powers,null);actor.Intent.CommitFrame();
                }
                Assert.AreEqual(0,powers.Kit.Skill2.CooldownRemaining,"The producer must not prematurely finish the pending cast.");
                for(int tick=0;tick<2&&actor.Intent.Pressed(Verb.Skill2);tick++)
                {
                    ((System.Collections.Generic.HashSet<Verb>)typeof(AIController).GetField("_touched",Hidden).GetValue(brain)).Clear();
                    typeof(AIController).GetMethod("StepHeroAbilities",Hidden).Invoke(brain,new object[]{actor.Intent,.02f});
                }
                Assert.IsFalse(actor.Intent.Pressed(Verb.Skill2),"The acquired aim must release at its actual ramp rather than hold forever.");
                return;
            }
            if(!incoming)
            {Assert.IsFalse(actor.Intent.Pressed(Verb.Skill1),"An outgoing flight must not spend the current.");return;}
            Assert.IsTrue(actor.Intent.Pressed(Verb.Skill1),"The incoming opportunity must reach normal skill input.");
            typeof(HeroAbilitySystem).GetMethod("Update",Hidden).Invoke(powers,null);
            Assert.Greater(powers.Kit.Skill1.CooldownRemaining,0,"The real consumer must accept the cast.");
            var field=RafiWaterField.Active.Single(f=>f.Capture().Type==WorldEffectSnapshot.Kind.Current);
            var desired=shoe.transform.position-actor.transform.position;desired.y=0;
            Assert.Greater(Vector3.Dot(field.Capture().Forward,desired.normalized),.99f,
                "The accepted narrow current points toward the can instead of its chosen incoming flight.");
        }
    }
}
