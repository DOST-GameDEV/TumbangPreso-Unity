using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class TimedRecoveryTests
    {
        private readonly List<GameObject> _objects=new List<GameObject>();
        private CharacterMotor _actor;
        private INetProvider _provider;
        private int _seat;
        private CustomRules _rules;
        private bool _pinned;
        private GameObject Keep(GameObject go){_objects.Add(go);return go;}
        private CharacterMotor Actor(string name,int seat,Vector3 position)
        {
            var root=Keep(new GameObject(name,typeof(CharacterController)));
            var cc=root.GetComponent<CharacterController>();cc.height=1.6f;cc.radius=.35f;cc.center=new Vector3(0,.8f,0);
            var actor=root.AddComponent<CharacterMotor>();actor.PlayerSlot=seat;actor.Mode=GameMode.HeroStrike;actor.IsBot=false;
            root.transform.position=position;GameServices.Round.Register(actor);return actor;
        }
        [UnitySetUp] public IEnumerator Before()
        {
            _provider=NetAuthority.Provider;_seat=GameLaunch.SoloSeat;
            _rules=UI.SceneFlow.SelectedRules.Clone();_pinned=UI.SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();NetAuthority.Provider=new SoloProvider();GameLaunch.SoloSeat=1;
            UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));GameServices.Ensure();GameServices.Round.Clear();
            var floor=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(30,1,30);
            var can=Keep(new GameObject("Timed recovery can"));can.transform.position=new Vector3(6,0,6);GameServices.Round.Lata=can.AddComponent<Lata>();
            _actor=Actor("Timed recovery student",1,new Vector3(0,.13f,-8));
            GameServices.Match.StartMatch();GameServices.Round.BeginRound();yield return new WaitForSeconds(.2f);
        }
        [UnityTearDown] public IEnumerator After()
        {
            foreach(var go in _objects)if(go!=null)Object.Destroy(go);_objects.Clear();yield return PlayModeWorld.Reset();
            NetAuthority.Provider=_provider;GameLaunch.SoloSeat=_seat;UI.SceneFlow.AdoptRemoteRules(_rules);
            if(_pinned)UI.SceneFlow.PinSelectedRules(_rules);else UI.SceneFlow.UnpinSelectedRules();
        }
        [UnityTest] public IEnumerator AbsoluteZeroFreezesRivalsButNeverItsCaster()
        {
            var rival = Actor("Absolute Zero rival", 2, new Vector3(3, .13f, -8));
            _actor.enabled = false; rival.enabled = false;
            var kit = new TumbangPreso.Abilities.CheskaHeroKit();
            var context = new TumbangPreso.Abilities.AbilityContext(_actor, null, null);
            using (NetCue.SuppressRelay())
            {
                kit.Ultimate.Activate(context);
                kit.Ultimate.Tick(context, 1.49f);
                Assert.IsFalse(rival.IsFrozen, "The existing windup must remain.");
                kit.Ultimate.Tick(context, .02f);
            }
            Assert.IsTrue(rival.IsFrozen, "A rival must still receive Frozen.");
            Assert.AreEqual(StatusRules.FrozenSeconds, rival.StunLeft, .001f);
            Assert.AreEqual(StatusRules.FrozenSeconds + StatusRules.ChilledSeconds, rival.ChilledLeft, .001f);
            Assert.IsFalse(_actor.IsFrozen, "The caster must not freeze herself.");
            Assert.IsFalse(_actor.IsChilled, "The caster must not receive her own thaw slow.");
            yield return null;
        }
        [UnityTest] public IEnumerator FrozenRejectsLegacyRecoveryAndKeepsItsTimer()
        {
            _actor.ApplyStagger(3,StunElement.Ice,8);_actor.enabled=false;
            float before=_actor.StunLeft;int episode=_actor.RecoveryEpisode;
            Assert.IsTrue(_actor.IsFrozen);Assert.IsFalse(_actor.CanAct());
            Assert.IsFalse(_actor.MashOutOfStun(),"Deprecated mash API must not shorten Frozen.");
            Assert.IsFalse(_actor.RecoverFromInput());Assert.IsFalse(_actor.AcceptRecoveryRequest(episode,1));
            Assert.AreEqual(before,_actor.StunLeft);Assert.AreEqual(0,_actor.StunMashPresses);Assert.IsFalse(_actor.CanMashOutOfStun);
            yield return null;
        }
        [UnityTest] public IEnumerator TripExpiresAtItsAuthoredDurationWithoutPresses()
        {
            _actor.ApplyTrip(.8f);float began=Time.time;
            Assert.IsFalse(_actor.CanMashUp,"A timed trip must not advertise a mash input.");
            while(Time.time-began<1.1f)yield return null;
            Assert.IsFalse(_actor.IsTripped,"No input must be required to leave a finished trip.");
            Assert.AreEqual(0,_actor.MashPresses);Assert.AreEqual(0,_actor.MashRemoved);
        }
        [UnityTest] public IEnumerator RepeatedJumpDoesNotShortenFrozenAndOrdinaryJumpStillWorks()
        {
            var control=Actor("No input control",2,new Vector3(3,.13f,-8));yield return null;
            _actor.ApplyStagger(1.2f,StunElement.Ice,8);control.ApplyStagger(1.2f,StunElement.Ice,8);
            for(int i=0;i<6;i++)
            {
                _actor.Intent.Set(Verb.Jump,true);_actor.Intent.BufferPress(Verb.Jump);yield return new WaitForFixedUpdate();
                _actor.Intent.Set(Verb.Jump,false);yield return new WaitForSeconds(.08f);
                Assert.AreEqual(control.StunLeft,_actor.StunLeft,.04f);
            }
            Assert.AreEqual(0,_actor.StunMashPresses);Assert.IsTrue(_actor.IsFrozen);
            yield return new WaitForSeconds(.8f);Assert.IsFalse(_actor.IsFrozen);Assert.IsTrue(_actor.CanAct());
            _actor.Intent.Clear();_actor.Intent.CommitFrame();yield return new WaitForFixedUpdate();
            _actor.Intent.Set(Verb.Jump,true);_actor.Intent.BufferPress(Verb.Jump);yield return new WaitForFixedUpdate();
            Assert.Greater(_actor.Velocity.y,0,"Retiring recovery cannot remove ordinary jump.");
            _actor.Intent.Set(Verb.Jump,false);
        }
        [UnityTest] public IEnumerator BotWaitsForTheSameFrozenClockWithoutRecoveryTaps()
        {
            _actor.gameObject.AddComponent<Carrier>();_actor.gameObject.AddComponent<CombatVerbs>();
            _actor.IsBot=true;_actor.gameObject.AddComponent<AIController>();
            _actor.ApplyStagger(1.2f,StunElement.Ice,8);float began=Time.time;
            while(Time.time-began<.7f)
            {
                Assert.IsFalse(_actor.Intent.Pressed(Verb.Jump));Assert.AreEqual(0,_actor.StunMashPresses);yield return null;
            }
            Assert.IsTrue(_actor.IsFrozen);yield return new WaitForSeconds(.7f);Assert.IsFalse(_actor.IsFrozen);
        }
        [UnityTest] public IEnumerator EdgeCatchCompletesWithoutMashOrTeleportingPastItsPhases()
        {
            var landing=new Vector3(0,.1f,-7.5f);
            Assert.IsTrue(_actor.BeginEdgeRecovery(new MapEdgeAnchor(EdgeRecoveryKind.Rooftop,new Vector3(0,1.8f,-8.5f),landing,Vector3.back)));
            bool hanging=false,pulling=false;float until=Time.time+4;
            while(_actor.IsEdgeRecovering&&Time.time<until)
            {
                hanging|=_actor.EdgePhase==1;pulling|=_actor.EdgePhase==2;
                Assert.IsFalse(_actor.Intent.Pressed(Verb.Jump));yield return null;
            }
            Assert.IsTrue(hanging);Assert.IsTrue(pulling);Assert.IsFalse(_actor.IsEdgeRecovering);
            Assert.Less(Vector3.Distance(landing,_actor.transform.position),.15f);Assert.AreEqual(0,_actor.MashPresses);
        }
    }
}
