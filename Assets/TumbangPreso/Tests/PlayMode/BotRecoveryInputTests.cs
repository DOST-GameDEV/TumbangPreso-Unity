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
            // Begin at the released phase, as a hardware recovery test begins with Jump up.
            typeof(AIController).GetField("_mashHeld",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(_brain,false);
            _who.Intent.Clear();_who.Intent.CommitFrame();_who.ApplyTrip();
            Assert.IsTrue(_who.IsTripped);Assert.IsFalse(_who.CanAct());
        }
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();

        [TestCase(2),TestCase(4),TestCase(6)]
        public void SeveralRenderUpdatesRetainOneRecoveryPressUntilPhysics(int renderUpdates)
        {
            for(int i=0;i<renderUpdates;i++)_brain.SendMessage("Update");
            Assert.IsTrue(_who.Intent.JustPressed(Verb.Jump),"An even render/physics ratio cancelled the bot's recovery tap.");
            _who.SendMessage("FixedUpdate");
            Assert.AreEqual(1,_who.MashPresses,"The real motor must consume the retained recovery press.");
            Assert.IsFalse(_who.Intent.JustPressed(Verb.Jump),"Physics must retire the pulse after consumption.");
            _who.SendMessage("FixedUpdate");Assert.AreEqual(1,_who.MashPresses);
            for(int i=0;i<renderUpdates;i++)_brain.SendMessage("Update");
            _who.SendMessage("FixedUpdate");
            Assert.AreEqual(1,_who.MashPresses,"Buffered bot taps must still obey the shared recovery rate limit.");
        }
    }
}
