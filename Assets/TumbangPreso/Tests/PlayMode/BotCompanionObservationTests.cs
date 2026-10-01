using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class BotCompanionObservationTests
    {
        private CharacterMotor _observer,_owner,_companion;
        private AIController _brain;
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private CharacterMotor Body(int seat,Vector3 at)
        {
            var body=new GameObject("Observed body "+seat).AddComponent<CharacterMotor>();
            body.enabled=false;body.PlayerSlot=seat;body.Mode=GameMode.HeroStrike;body.transform.position=at;return body;
        }
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();GameServices.Ensure();GameServices.Round.Clear();
            _owner=Body(0,Vector3.zero);_observer=Body(1,new Vector3(0,0,-2));
            GameServices.Round.Register(_owner);GameServices.Round.Register(_observer);
            GameServices.Match.ApplySnapshot(new int[4],2,true);GameServices.Round.ApplySnapshot(100,true,1,true);
            _companion=Body(4,new Vector3(0,0,2));Assert.IsTrue(GameServices.Round.RegisterCompanion(_companion));
            _brain=_observer.gameObject.AddComponent<AIController>();_brain.enabled=false;_brain.SeatDifficulty=Difficulty.Normal;
        }
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        private static void Observe(AIController brain,float dt=.01f)
            => typeof(AIController).GetMethod("Observe",Hidden).Invoke(brain,new object[]{dt});
        private static Vector3 At(AIController brain,CharacterMotor body)
            => (Vector3)typeof(AIController).GetMethod("At",Hidden).Invoke(brain,new object[]{body});
        [TestCase(Difficulty.Normal)] [TestCase(Difficulty.Astig)]
        public void CompanionBeliefUsesTheSameReactionLagAsPlayers(Difficulty tier)
        {
            _brain.SeatDifficulty=tier;Observe(_brain);
            _owner.transform.position+=Vector3.right*4;_companion.transform.position+=Vector3.right*4;
            _observer.transform.position+=Vector3.right*3;Observe(_brain);
            float playerX=At(_brain,_owner).x,companionX=At(_brain,_companion).x;
            Assert.Greater(playerX,0);Assert.Less(playerX,4);
            Assert.AreEqual(playerX,companionX,.0001f,"Companion movement bypassed the bot's reaction-lag model.");
            Assert.AreEqual(3,At(_brain,_observer).x,.0001f,"Own position must stay exact.");
        }
        [Test] public void AReplacementInTheSameSeatStartsWithItsOwnPosition()
        {
            Observe(_brain);_companion.transform.position+=Vector3.right*4;Observe(_brain);
            GameServices.Round.UnregisterCompanion(_companion);Object.DestroyImmediate(_companion.gameObject);
            _companion=Body(4,new Vector3(6,0,2));Assert.IsTrue(GameServices.Round.RegisterCompanion(_companion));
            Assert.AreEqual(6,At(_brain,_companion).x,.0001f,"A fresh body inherited a dead body's belief.");
            Observe(_brain);Assert.AreEqual(6,At(_brain,_companion).x,.0001f);
            _companion.transform.position+=Vector3.right;Observe(_brain);
            Assert.Greater(At(_brain,_companion).x,6);Assert.Less(At(_brain,_companion).x,7);
        }
        [Test] public void ACompanionBotStillKnowsItsOwnFeetWithoutReactionLag()
        {
            var brain=_companion.gameObject.AddComponent<AIController>();brain.enabled=false;brain.SeatDifficulty=Difficulty.Astig;
            Observe(brain);_companion.transform.position+=Vector3.right*3;Observe(brain);
            Assert.AreEqual(3,At(brain,_companion).x,.0001f);
        }
    }
}
