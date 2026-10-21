using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class BotHopInputTests
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
        [UnityTest]
        public IEnumerator AnIssuedHopSurvivesItsRenderReleaseBeforePhysics()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);GameServices.Round.BeginRound();
            var who=GameServices.Round.PlayerAt(0);who.IsBot=true;who.Intent.Parked=false;
            var brain=who.GetComponent<AIController>();brain.enabled=false;
            Assert.IsTrue(who.CanAct());Assert.IsFalse(who.IsTaggable());
            var hidden=BindingFlags.Instance|BindingFlags.NonPublic;
            var step=typeof(AIController).GetMethod("StepHop",hidden);
            var countdown=typeof(AIController).GetField("_hopCountdown",hidden);
            var held=typeof(AIController).GetField("_hopHeld",hidden);
            var random=Random.state;bool issued=false;
            try
            {
                // Exercise the real chance/eligibility branch without changing its tuning.
                for(int seed=0;seed<1024&&!issued;seed++)
                {
                    Random.InitState(seed);countdown.SetValue(brain,-1f);held.SetValue(brain,false);
                    who.Intent.Clear();who.Intent.CommitFrame();
                    step.Invoke(brain,new object[]{who.Intent,.016f});issued=who.Intent.Pressed(Verb.Jump);
                }
                Assert.IsTrue(issued,"No eligible normal hop was issued under the unchanged chance rules.");
                Assert.IsTrue(who.Intent.JustPressed(Verb.Jump));
                step.Invoke(brain,new object[]{who.Intent,.016f});
                Assert.IsFalse(who.Intent.Pressed(Verb.Jump),"The hop still releases its held key on the next render update.");
                Assert.IsTrue(who.Intent.JustPressed(Verb.Jump),"The render release cancelled an issued hop before physics.");
                who.SendMessage("FixedUpdate");
                Assert.IsFalse(who.Intent.JustPressed(Verb.Jump),"The physical consumer must retire the buffered edge.");
            }
            finally{Random.state=random;}
        }
    }
}
