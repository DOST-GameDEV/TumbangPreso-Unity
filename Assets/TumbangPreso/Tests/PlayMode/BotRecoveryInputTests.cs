using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class BotRecoveryInputTests
    {
        private CharacterMotor _who;
        private AIController _brain;
        [UnitySetUp]
        public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            GameServices.Round.BeginRound();
            _who=GameServices.Round.PlayerAt(2);_who.IsBot=true;_who.Intent.Parked=false;
            _brain=_who.GetComponent<AIController>();_brain.enabled=false;
            _who.Intent.Clear();_who.Intent.CommitFrame();_who.ApplyTrip();
            Assert.IsTrue(_who.IsTripped);Assert.IsFalse(_who.CanAct());
        }
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();

        [TestCase(2),TestCase(4),TestCase(6)]
        public void SeveralRenderUpdatesNeverGenerateRecoveryPresses(int renderUpdates)
        {
            for(int i=0;i<renderUpdates;i++)_brain.SendMessage("Update");
            Assert.IsFalse(_who.Intent.JustPressed(Verb.Jump),"An incapacitated bot must not generate recovery taps.");
            _who.SendMessage("FixedUpdate");
            Assert.AreEqual(0,_who.MashPresses,"The motor must not consume retired recovery input.");
            Assert.IsFalse(_who.Intent.JustPressed(Verb.Jump),"Physics must retire the pulse after consumption.");
            _who.SendMessage("FixedUpdate");Assert.AreEqual(0,_who.MashPresses);
            for(int i=0;i<renderUpdates;i++)_brain.SendMessage("Update");
            _who.SendMessage("FixedUpdate");
            Assert.AreEqual(0,_who.MashPresses,"Repeated AI updates must not revive mash recovery.");
        }
    }
}
