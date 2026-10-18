using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Social;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class AiUnavailableEmoteTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private Random.State _random;
        [UnitySetUp] public IEnumerator Before(){_random=Random.state;yield return PlayModeWorld.Reset();}
        [UnityTearDown] public IEnumerator After(){yield return PlayModeWorld.Reset();Random.state=_random;}

        private static (CharacterMotor motor,EmotePlayer emotes,AIController brain) PlayableActor()
        {
            PresentationClock.RequestScale(1);
            var go=new GameObject("Bot with actual playable rig");
            var motor=go.AddComponent<CharacterMotor>();motor.enabled=false;motor.PlayerSlot=1;motor.IsBot=true;motor.RoundActive=true;
            go.AddComponent<Carrier>().enabled=false;
            var emotes=go.AddComponent<EmotePlayer>();emotes.enabled=false;
            var visual=go.AddComponent<TumbangPreso.Visual.CharacterVisual>();
            var art=RosterBook.Load().FindPersonArt("rafi");
            visual.ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
            var brain=go.AddComponent<AIController>();brain.enabled=false;
            return (motor,emotes,brain);
        }

        [UnityTest]
        public IEnumerator PlayableMotionStillStartsAndHoldsItsVisibleEmote()
        {
            var actor=PlayableActor();yield return null;
            Assert.IsTrue(actor.emotes.HasEmoteClip("dance"),"Positive control must have actual resolved motion.");
            Assert.IsTrue(actor.emotes.CanEmote());
            typeof(AIController).GetField("_wantedEmote",Hidden).SetValue(actor.brain,"dance");
            actor.motor.Intent.Move=Vector2.up;
            bool consumed=(bool)typeof(AIController).GetMethod("StepSocial",Hidden).Invoke(actor.brain,new object[]{actor.motor.Intent,.016f});
            Assert.IsTrue(consumed);Assert.IsTrue(actor.emotes.IsEmoting);Assert.AreEqual("dance",actor.emotes.Current);
            Assert.AreEqual(Vector2.zero,actor.motor.Intent.Move);
            Assert.Greater((float)typeof(AIController).GetField("_emoteHoldLeft",Hidden).GetValue(actor.brain),0);
        }

        [UnityTest]
        public IEnumerator PendingPlayableMotionRetainsItsBoundedDeliveryGrace()
        {
            var actor=PlayableActor();yield return null;
            Assert.IsTrue(actor.emotes.HasEmoteClip("dance"));Assert.IsFalse(actor.emotes.IsEmoting);
            typeof(AIController).GetField("_emoteHoldLeft",Hidden).SetValue(actor.brain,1f);
            typeof(AIController).GetField("_emoteHeldFor",Hidden).SetValue(actor.brain,0f);
            bool waiting=(bool)typeof(AIController).GetMethod("StepSocial",Hidden).Invoke(actor.brain,new object[]{actor.motor.Intent,.05f});
            Assert.IsTrue(waiting,"A playable request awaiting delivery keeps the existing startup grace.");
            actor.motor.Intent.Move=Vector2.up;
            bool expired=(bool)typeof(AIController).GetMethod("StepSocial",Hidden).Invoke(actor.brain,new object[]{actor.motor.Intent,.26f});
            Assert.IsFalse(expired);Assert.AreEqual(Vector2.up,actor.motor.Intent.Move);
            Assert.AreEqual(0f,(float)typeof(AIController).GetField("_emoteHoldLeft",Hidden).GetValue(actor.brain));
        }

        [Test]
        public void UnavailableMotionDoesNotConsumeTheBotsSocialFrame()
        {
            PresentationClock.RequestScale(1);
            var go=new GameObject("Bot awaiting rendered emote rig");
            var motor=go.AddComponent<CharacterMotor>();motor.enabled=false;motor.PlayerSlot=1;motor.IsBot=true;motor.RoundActive=true;
            go.AddComponent<Carrier>().enabled=false;
            var emotes=go.AddComponent<EmotePlayer>();emotes.enabled=false;
            var brain=go.AddComponent<AIController>();brain.enabled=false;
            Assert.IsTrue(motor.CanAct());Assert.IsTrue(emotes.CanEmote());Assert.IsFalse(emotes.IsEmoting);
            Assert.IsTrue(Emotes.IsKnown("dance"));
            Assert.IsNull(go.GetComponent<TumbangPreso.Visual.CharacterAnimator>(),"Rig unavailability is the actual playback refusal condition.");
            Assert.IsTrue((bool)typeof(AIController).GetMethod("SafeToEmote",Hidden).Invoke(brain,null));
            motor.Intent.Move=new Vector2(.25f,1);
            motor.Intent.Set(Verb.Grab,true);
            typeof(AIController).GetField("_wantedEmote",Hidden).SetValue(brain,"dance");
            bool consumed=(bool)typeof(AIController).GetMethod("StepSocial",Hidden).Invoke(brain,new object[]{motor.Intent,.016f});
            Assert.IsFalse(emotes.IsEmoting,"No rendered clip actually started.");
            Assert.IsFalse(consumed,"A missing rendered motion may not consume the AI's frame.");
            Assert.AreEqual(new Vector2(.25f,1),motor.Intent.Move);
            Assert.IsTrue(motor.Intent.Pressed(Verb.Grab),"Unavailable art may not release a gameplay key.");
            Assert.AreEqual(0f,(float)typeof(AIController).GetField("_emoteHoldLeft",Hidden).GetValue(brain));
        }
    }
}
