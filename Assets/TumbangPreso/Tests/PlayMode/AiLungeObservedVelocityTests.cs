using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class AiLungeObservedVelocityTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private Difficulty _prior;
        [UnitySetUp]public IEnumerator Before(){_prior=AIController.ActiveDifficulty;yield return PlayModeWorld.Reset();AIController.ActiveDifficulty=Difficulty.Normal;}
        [UnityTearDown]public IEnumerator After(){yield return PlayModeWorld.Reset();AIController.ActiveDifficulty=_prior;}
        private static object Call(object owner,string method,params object[] args)=>owner.GetType().GetMethod(method,Hidden).Invoke(owner,args);
        [TestCase(5f,false),TestCase(0f,true),TestCase(-5f,true)]
        public void ReleaseReachUsesTheVelocityActuallyObserved(float retreatSpeed,bool reachable)
        {
            UI.SceneFlow.Networked=false;UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameServices.Ensure();GameServices.Round.Clear();
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=Vector3.down*.1f;floor.transform.localScale=new Vector3(30,.2f,30);
            GameServices.Round.Lata=new GameObject("Observed velocity can").AddComponent<Lata>();GameServices.Round.Lata.transform.position=new Vector3(5,0,5);
            CharacterMotor Seat(int slot,Vector3 at)
            {
                var go=new GameObject("Observed velocity seat"+slot,typeof(CharacterController));var cc=go.GetComponent<CharacterController>();
                cc.height=1.6f;cc.radius=.35f;cc.center=Vector3.up*.8f;
                var motor=go.AddComponent<CharacterMotor>();motor.enabled=false;motor.PlayerSlot=slot;motor.Mode=GameMode.Classic;motor.SpawnPosition=at;
                go.AddComponent<Carrier>().enabled=false;go.AddComponent<CombatVerbs>().enabled=false;GameServices.Round.Register(motor);return motor;
            }
            var actor=Seat(0,new Vector3(0,.1f,-3));var victim=Seat(1,actor.SpawnPosition+Vector3.forward*3);
            GameServices.Match.StartMatch();GameServices.Round.BeginRound();actor.IsDefender=true;victim.IsDefender=false;victim.HoldingSlipper=true;
            actor.transform.position=actor.SpawnPosition;victim.transform.position=victim.SpawnPosition;actor.transform.forward=Vector3.forward;
            typeof(CharacterMotor).GetField("_velocity",Hidden).SetValue(victim,Vector3.forward*retreatSpeed);Physics.SyncTransforms();
            var brain=actor.gameObject.AddComponent<AIController>();brain.enabled=false;Call(brain,"Observe",.02f);
            // Independent continuous upper bound: discrete motor friction travels
            // slightly less. Even this generous path cannot hit the retreat case.
            if(!reachable)
            {
                float closestAge=Mathf.Clamp((Balance.LungeSpeed-retreatSpeed)/Balance.Friction,0,Balance.LungeActiveTime);
                float gap=3+retreatSpeed*closestAge-(Balance.LungeSpeed*closestAge-.5f*Balance.Friction*closestAge*closestAge);
                Assert.Greater(gap,Balance.LungeTagRadius*victim.TagReachScale,"The actual observed retreat must genuinely evade the full dash.");
            }
            Assert.AreEqual(reachable,(bool)Call(brain,"LungeCanReach",victim,1f),
                "The release gate treated the perceived moving target as slower than its measured velocity.");
        }
    }
}
