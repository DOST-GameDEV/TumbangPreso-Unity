using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
namespace TumbangPreso.PlayTests
{
    public sealed class AiSkimDecisionTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private INetProvider _provider; private CustomRules _rules; private bool _pinned;
        private sealed class Solo : INetProvider
        { public bool IsHost=>true;public bool IsNetworked=>false;public int LocalSlot=>0;public int LocalPeerId=>0;public bool IsSeatlessReferee=>false; }
        [UnitySetUp] public IEnumerator Before()
        { _provider=NetAuthority.Provider;_rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;yield return PlayModeWorld.Reset();NetAuthority.Provider=new Solo();SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike)); }
        [UnityTearDown] public IEnumerator After()
        { yield return PlayModeWorld.Reset();NetAuthority.Provider=_provider;SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules(); }
        private static void Write(object owner,string field,object value)=>owner.GetType().GetField(field,Hidden).SetValue(owner,value);
        [TestCase(true),TestCase(false)]
        public void ArmedThrowPlanConsidersSkimOnlyWithAHeldSlipper(bool held)
        {
            GameServices.Ensure();GameServices.Round.Clear();GameServices.Match.ApplySnapshot(new int[4],1,true);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=Vector3.down*.1f;floor.transform.localScale=new Vector3(30,.2f,30);
            var can=new GameObject("Skim decision can").AddComponent<Lata>();GameServices.Round.Lata=can;
            CharacterMotor Body(int slot,Vector3 at)
            {
                var go=new GameObject("Skim decision actor"+slot,typeof(CharacterController));var cc=go.GetComponent<CharacterController>();cc.height=1.6f;cc.radius=.35f;cc.center=Vector3.up*.8f;
                var body=go.AddComponent<CharacterMotor>();body.enabled=false;body.PlayerSlot=slot;body.Mode=GameMode.HeroStrike;body.IsBot=true;body.SpawnPosition=at;
                go.AddComponent<Carrier>().enabled=false;go.AddComponent<CombatVerbs>().enabled=false;GameServices.Round.Register(body);return body;
            }
            var defender=Body(0,new Vector3(0,.1f,-2));var actor=Body(1,new Vector3(0,.1f,8));
            var powers=actor.gameObject.AddComponent<HeroAbilitySystem>();powers.enabled=false;powers.BindHero("rafi");
            GameServices.Round.BeginRound();actor.transform.position=actor.SpawnPosition;defender.transform.position=defender.SpawnPosition;actor.transform.forward=Vector3.back;
            actor.IsDefender=false;defender.IsDefender=true;actor.Intent.Parked=false;
            if(held)
            {
                var shoe=new GameObject("Own held skim slipper").AddComponent<Slipper>();shoe.OwnerSlot=1;shoe.SeatOfOrigin=1;
                Assert.IsTrue(shoe.HostForceEquip(actor));Assert.IsNotNull(actor.GetComponent<Carrier>().Held);
            }
            var brain=actor.gameObject.AddComponent<AIController>();brain.enabled=false;typeof(AIController).GetProperty("Plan").SetValue(brain,AiPlan.Windup);
            Write(brain,"_roundLiveFor",100f);Write(brain,"_abilityCadenceLeft",0f);Write(brain,"_driving",false);
            Assert.IsTrue(actor.CanAct());Assert.IsTrue(powers.Kit.Skill2.IsReady);
            Write(brain,"_roundLiveFor",0f);
            typeof(AIController).GetMethod("StepHeroAbilities",Hidden).Invoke(brain,new object[]{actor.Intent,.02f});
            Assert.IsNull(typeof(AIController).GetField("_weighing",Hidden).GetValue(brain), "Opening delay must still defer a new coating.");
            ((System.Collections.Generic.HashSet<Verb>)typeof(AIController).GetField("_touched",Hidden).GetValue(brain)).Clear();
            Write(brain,"_roundLiveFor",100f);
            typeof(AIController).GetMethod("StepHeroAbilities",Hidden).Invoke(brain,new object[]{actor.Intent,.02f});
            var weighed=typeof(AIController).GetField("_weighing",Hidden).GetValue(brain);
            if(held)Assert.AreEqual(Verb.Skill2,weighed,"The current held-slipper coating must be considered before throwing, without needing a nearby pursuer or movement.");
            else Assert.IsNull(weighed,"Skim cannot coat a missing held slipper.");
            if (held)
            {
                // Continue the actual deliberation/input producer; never call Fire
                // or network-cast directly to make the load look successful.
                for (int tick = 0; tick < 80 && !actor.Intent.Pressed(Verb.Skill2); tick++)
                {
                    ((System.Collections.Generic.HashSet<Verb>)typeof(AIController).GetField("_touched",Hidden).GetValue(brain)).Clear();
                    typeof(AIController).GetMethod("StepHeroAbilities",Hidden).Invoke(brain,new object[]{actor.Intent,.02f});
                }
                Assert.IsTrue(actor.Intent.Pressed(Verb.Skill2), "A considered load never became normal skill input.");
                typeof(HeroAbilitySystem).GetMethod("Update",Hidden).Invoke(powers,null);
                var kit = (RafiHeroKit)powers.Kit;
                Assert.IsTrue(kit.IsSkimLoadedFor(actor.GetComponent<Carrier>().Held), "The real ability consumer did not coat the held slipper.");
                Assert.Greater(kit.Skill2.CooldownRemaining,0);
                actor.Intent.Set(Verb.Skill2,false); actor.Intent.CommitFrame();
                ((System.Collections.Generic.HashSet<Verb>)typeof(AIController).GetField("_touched",Hidden).GetValue(brain)).Clear();
                Write(brain,"_abilityCadenceLeft",0f);
                typeof(AIController).GetMethod("StepHeroAbilities",Hidden).Invoke(brain,new object[]{actor.Intent,.02f});
                Assert.IsNull(typeof(AIController).GetField("_weighing",Hidden).GetValue(brain), "An already loaded coating must not be weighed again.");
                var carrier=actor.GetComponent<Carrier>();var coated=carrier.Held;
                carrier.HostThrowAt(actor.transform.position+Vector3.up,Vector3.zero,1f);
                Assert.AreEqual(SlipperAffinity.Skim,coated.Affinity, "The normal authoritative throw path did not transfer the coating.");
                Assert.IsNull(carrier.Held);Assert.IsFalse(kit.IsSkimLoadedFor(coated));
            }
        }
    }
}
