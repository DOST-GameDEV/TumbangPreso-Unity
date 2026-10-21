using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class AiLungeMotorAimTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
        private static object Call(object owner,string method,params object[] args)=>owner.GetType().GetMethod(method,Hidden).Invoke(owner,args);
        private static void Write(object owner,string field,object value)=>owner.GetType().GetField(field,Hidden).SetValue(owner,value);
        [TestCase(20f),TestCase(22f)]
        public void ParallelPursuitUsesTheRealMotorTurnAndFinishesItsCharge(float bearing)
        {
            UI.SceneFlow.Networked=false;UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameServices.Ensure();GameServices.Round.Clear();
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=Vector3.down*.1f;floor.transform.localScale=new Vector3(30,.2f,30);
            GameServices.Round.Lata=new GameObject("Parallel pursuit can").AddComponent<Lata>();GameServices.Round.Lata.transform.position=new Vector3(5,0,5);
            CharacterMotor Seat(int slot,Vector3 at)
            {
                var go=new GameObject("Parallel pursuit seat"+slot,typeof(CharacterController));var cc=go.GetComponent<CharacterController>();
                cc.height=1.6f;cc.radius=.35f;cc.center=Vector3.up*.8f;
                var motor=go.AddComponent<CharacterMotor>();motor.enabled=false;motor.PlayerSlot=slot;motor.IsBot=true;motor.Mode=GameMode.Classic;motor.SpawnPosition=at;
                go.AddComponent<Carrier>().enabled=false;go.AddComponent<CombatVerbs>().enabled=false;GameServices.Round.Register(motor);return motor;
            }
            var actor=Seat(0,new Vector3(0,.1f,-3));var toward=Quaternion.Euler(0,bearing,0)*Vector3.forward;
            var victim=Seat(1,actor.SpawnPosition+toward*4.3f);
            GameServices.Match.StartMatch();GameServices.Round.BeginRound();
            actor.IsDefender=true;victim.IsDefender=false;victim.HoldingSlipper=true;actor.Intent.Parked=false;victim.Intent.Parked=false;
            actor.transform.position=actor.SpawnPosition;victim.transform.position=victim.SpawnPosition;actor.transform.forward=Vector3.forward;
            Write(actor,"_velocity",Vector3.forward*5);Write(victim,"_velocity",Vector3.forward*5);Physics.SyncTransforms();
            var brain=actor.gameObject.AddComponent<AIController>();brain.enabled=false;typeof(AIController).GetProperty("Plan").SetValue(brain,AiPlan.Hunt);
            var verbs=actor.GetComponent<CombatVerbs>();actor.Intent.Set(Verb.Lunge,true);Call(verbs,"StepLunge",.6f);Write(brain,"_lungeHeld",.6f);actor.Intent.CommitFrame();
            Assert.IsTrue(victim.IsTaggable());
            Assert.Greater(victim.transform.position.x-actor.transform.position.x,
                Balance.LungeTagRadius*victim.TagReachScale,"The original forward corridor must genuinely miss.");
            for(int tick=0;tick<40&&verbs.LungeCooldownLeft<=0;tick++)
            {
                Call(brain,"StepLungeIntent",actor.Intent,victim,.02f);
                // Use the actual bounded motor turn, never directly rotate the
                // actor to manufacture an aligned release.
                Call(actor,"Steer",actor.Intent.MoveAxis,.02f);
                Call(verbs,"StepLunge",.02f);actor.Intent.CommitFrame();
                if(verbs.LungeCooldownLeft<=0)
                {
                    actor.GetComponent<CharacterController>().Move(Vector3.forward*.1f);
                    victim.GetComponent<CharacterController>().Move(Vector3.forward*.1f);
                }
            }
            Assert.Greater(verbs.LungeCooldownLeft,0,"The charged parallel pursuit stayed on its nearest digital heading and never completed an aimed release.");
            Assert.Greater(actor.transform.forward.x,.1f,"The actual motor must turn toward the off-axis target.");
            Assert.AreEqual(0,verbs.PunchCooldownLeft);
        }
    }
}
