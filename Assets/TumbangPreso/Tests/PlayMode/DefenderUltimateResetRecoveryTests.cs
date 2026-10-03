using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class DefenderUltimateResetRecoveryTests
    {
        readonly List<GameObject> _objects=new List<GameObject>();
        INetProvider _provider;CustomRules _rules;bool _pinned,_tutorial;int _seat;
        CharacterMotor _defender,_caster;Carrier _reset;Lata _can;
        GameObject Keep(GameObject value){_objects.Add(value);return value;}
        CharacterMotor Actor(string name,int slot,Vector3 position)
        {
            var go=Keep(new GameObject(name,typeof(CharacterController)));var cc=go.GetComponent<CharacterController>();
            cc.height=1.6f;cc.radius=.35f;cc.center=new Vector3(0,.8f,0);
            var actor=go.AddComponent<CharacterMotor>();actor.PlayerSlot=slot;actor.Mode=GameMode.HeroStrike;
            actor.transform.position=position;go.AddComponent<Carrier>();go.AddComponent<CombatVerbs>();
            GameServices.Round.Register(actor);return actor;
        }
        [UnitySetUp] public IEnumerator Before()
        {
            _provider=NetAuthority.Provider;_rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;
            _seat=GameLaunch.SoloSeat;_tutorial=GameLaunch.GuidedTutorial;
            yield return PlayModeWorld.Reset();NetAuthority.Provider=new SoloProvider();GameLaunch.SoloSeat=1;GameLaunch.GuidedTutorial=false;
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));GameServices.Ensure();GameServices.Round.Clear();
            var floor=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(40,1,40);
            _can=Keep(new GameObject("Reset recovery can")).AddComponent<Lata>();GameServices.Round.Lata=_can;
            _defender=Actor("AI defender",0,new Vector3(0,.13f,.65f));_defender.IsBot=true;
            _reset=_defender.GetComponent<Carrier>();_defender.gameObject.AddComponent<AIController>();
            _caster=Actor("Ultimate caster",1,new Vector3(10,.13f,10));
            _caster.gameObject.AddComponent<HeroAbilitySystem>().BindHero("zack");
            GameServices.Match.StartMatch();
            // SliceRunner normally assigns these roles; this isolated stage supplies its two actors.
            foreach(var actor in GameServices.Round.Players) actor.IsDefender=actor.PlayerSlot==GameServices.Match.DefenderSlot;
            GameServices.Round.BeginRound();yield return new WaitForSeconds(.2f);
        }
        [UnityTearDown] public IEnumerator After()
        {
            SharedUltimatePhase.Instance?.Cancel();PresentationClock.RequestScale(1);
            foreach(var go in _objects)if(go!=null)Object.Destroy(go);_objects.Clear();yield return PlayModeWorld.Reset();
            NetAuthority.Provider=_provider;GameLaunch.SoloSeat=_seat;GameLaunch.GuidedTutorial=_tutorial;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        [UnityTest,Timeout(45000)] public IEnumerator DefenderFinishesItsResetAndMovesAfterAnAcceptedUltimate()
        {
            Assert.IsTrue(_defender.IsDefender);_can.HostKnockDown(-1);Assert.IsFalse(_can.IsUpright);
            float deadline=Time.realtimeSinceStartup+5;
            while(_reset.ChannelRatio<=.02f&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.Greater(_reset.ChannelRatio,.02f,"Control: the real defender must already be holding a real reset channel.");
            Assert.IsTrue(_defender.Intent.Pressed(Verb.Grab));
            var system=_caster.AbilitySystem;system.Kit.AddUltimateCharge(system.Kit.UltimateCost);
            _caster.Intent.Set(Verb.Ultimate,true);_caster.Intent.BufferPress(Verb.Ultimate);
            deadline=Time.realtimeSinceStartup+2;
            while(SharedUltimatePhase.Instance?.Active!=true&&Time.realtimeSinceStartup<deadline)yield return null;
            var phase=SharedUltimatePhase.Instance;Assert.IsNotNull(phase);Assert.IsTrue(phase.Active);
            Assert.IsTrue(PresentationClock.Held);_caster.Intent.Set(Verb.Ultimate,false);
            deadline=Time.realtimeSinceStartup+(float)phase.Duration+2;
            while(phase.Active&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsFalse(phase.Active);Assert.IsFalse(PresentationClock.Held);Assert.IsFalse(PresentationClock.BlocksInput);
            Assert.IsFalse(_can.IsUpright,"The can cannot reset while the ultimate holds simulation.");
            deadline=Time.realtimeSinceStartup+6;
            while(!_can.IsUpright&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsTrue(_can.IsUpright,$"Defender stuck after ultimate: plan={_defender.GetComponent<AIController>().Plan}, "
                +$"grab={_defender.Intent.Pressed(Verb.Grab)}, channel={_reset.ChannelRatio}, canAct={_defender.CanAct()}");
            Vector3 before=_defender.transform.position;deadline=Time.realtimeSinceStartup+3;
            while(Vector3.Distance(before,_defender.transform.position)<.65f&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.Greater(Vector3.Distance(before,_defender.transform.position),.65f,"After restoring the can the defender must physically resume its patrol.");
        }
    }
}
