using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace TumbangPreso.PlayTests
{
    public sealed class AiCurrentUltimateTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance|BindingFlags.NonPublic;
        private GameObject _root; private CharacterMotor _actor,_target; private HeroAbilitySystem _powers;
        private AIController _brain; private INetProvider _provider; private GameMode _mode; private Random.State _random;
        private sealed class Solo : INetProvider
        { public bool IsHost=>true; public bool IsNetworked=>false; public int LocalSlot=>0; public int LocalPeerId=>0; public bool IsSeatlessReferee=>false; }
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset(); _provider=NetAuthority.Provider; NetAuthority.Provider=new Solo();
            _mode=SceneFlow.SelectedMode;SceneFlow.SelectedMode=GameMode.HeroStrike;_random=Random.state;
            GameServices.Ensure();GameServices.Round.Clear();GameServices.Match.ApplySnapshot(new int[4],1,true);
            _root=new GameObject("Current ultimate decision fixture");
            _actor=Body("Caster",0,Vector3.zero);_target=Body("Observed opponent",1,new Vector3(0,0,8));
            _powers=_actor.gameObject.AddComponent<HeroAbilitySystem>();_powers.enabled=false;
            _brain=_actor.gameObject.AddComponent<AIController>();_brain.enabled=false;
            GameServices.Round.BeginRound();yield return null;
        }
        private CharacterMotor Body(string name,int seat,Vector3 at)
        {
            var go=new GameObject(name,typeof(CharacterController));go.transform.SetParent(_root.transform);go.transform.position=at;
            var body=go.AddComponent<CharacterMotor>();body.enabled=false;body.PlayerSlot=seat;body.Mode=GameMode.HeroStrike;body.IsBot=true;
            go.AddComponent<Carrier>().enabled=false;go.AddComponent<CombatVerbs>().enabled=false;
            GameServices.Round.Register(body);return body;
        }
        [UnityTearDown] public IEnumerator After()
        { if(_root!=null)Object.Destroy(_root);yield return PlayModeWorld.Reset();NetAuthority.Provider=_provider;SceneFlow.SelectedMode=_mode;Random.state=_random; }
        private HeroKit Bind(string hero)
        { _powers.BindHero(hero);return _powers.Kit; }
        private bool Catch(HeroAbility ability,bool stun)
        { return (bool)typeof(AIController).GetMethod("WouldCatch",Hidden).Invoke(_brain,new object[]{ability,stun}); }
        private void PrepareDecision(HeroKit kit)
        {
            // Controlled decision state only; no ability, effect or natural cast is forced.
            kit.AddUltimateCharge(kit.UltimateCost);
            foreach(var ability in new[]{kit.Skill1,kit.AttackingSkill,kit.DefendingSkill})
                if(ability!=null)typeof(HeroAbility).GetProperty("CooldownRemaining").SetValue(ability,100f);
            typeof(AIController).GetField("_roundLiveFor",Hidden).SetValue(_brain,100f);
            typeof(AIController).GetField("_ultimateReadyFor",Hidden).SetValue(_brain,100f);
            Assert.IsTrue(kit.IsUltimateReady);Assert.IsTrue(_actor.CanAct());
        }
        private object Decide()
        { typeof(AIController).GetMethod("StepHeroAbilities",Hidden).Invoke(_brain,new object[]{_actor.Intent,.02f});return typeof(AIController).GetField("_weighing",Hidden).GetValue(_brain); }
        [TestCase("cheska",true),TestCase("dante",false)]
        public void CurrentNonCircularUltimateRecognizesItsActualVictim(string hero,bool stun)
        { var kit=Bind(hero);Assert.IsFalse(kit.Ultimate.HasTelegraph);Assert.IsTrue(Catch(kit.Ultimate,stun)); }
        [Test] public void GlobalFreezeIsConsideredWhenReadyWithAnEligibleObservedOpponent()
        { var kit=Bind("cheska");PrepareDecision(kit);Assert.AreEqual(Verb.Ultimate,Decide()); }
        [Test] public void ForwardWaveDoesNotAvoidTheCanItCannotKnockDown()
        {
            var kit=Bind("dante");PrepareDecision(kit);var go=new GameObject("Own can");go.transform.SetParent(_root.transform);
            GameServices.Round.Lata=go.AddComponent<Lata>();Assert.IsTrue(_actor.IsDefender);
            Assert.AreEqual(Verb.Ultimate,Decide());
        }
        [Test] public void ForwardWaveDoesNotValueAnOpponentBehindTheCaster()
        { var kit=Bind("dante");_target.transform.position=Vector3.back*8;Assert.IsFalse(Catch(kit.Ultimate,false)); }
        [Test] public void GlobalFreezeDoesNotValueAnInactiveOpponent()
        { var kit=Bind("cheska");_target.RoundActive=false;Assert.IsFalse(Catch(kit.Ultimate,true)); }
        [Test] public void OrdinaryCircleStillRejectsAnOutOfRangeOpponent()
        { var kit=Bind("cheska");_target.transform.position=Vector3.right*30;Assert.IsFalse(Catch(kit.Skill1,true)); }
        [Test] public void ClassicDoesNotConsiderHeroUltimateKeys()
        { var kit=Bind("cheska");PrepareDecision(kit);SceneFlow.SelectedMode=GameMode.Classic;Assert.IsNull(Decide()); }
    }
}
