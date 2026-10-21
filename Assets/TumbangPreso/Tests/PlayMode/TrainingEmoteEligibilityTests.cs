using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Social;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class TrainingEmoteEligibilityTests
    {
        private CharacterMotor _actor;
        private EmotePlayer _emotes;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            PresentationClock.RequestScale(1);
            var owner = new GameObject("Training emote eligibility actor");
            _actor = owner.AddComponent<CharacterMotor>(); _actor.enabled = false; _actor.RoundActive = true;
            _emotes = owner.AddComponent<EmotePlayer>(); _emotes.enabled = false;
            Assert.IsTrue(_actor.CanAct()); Assert.IsFalse(_emotes.IsEmoting);
        }

        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [Test] public void LessonWithholdingTheEmoteVerbRefusesEmoteEligibility()
        {
            _actor.Intent.AllowOnly(new HashSet<Verb> { Verb.Jump });
            Assert.IsTrue(_actor.Intent.Locked(Verb.EmoteWheel));
            Assert.IsFalse(_emotes.CanEmote(), "The public emote gate bypassed the lesson's withheld verb.");
        }

        [Test] public void OrdinaryUnrestrictedActorKeepsItsEmoteEligibility()
        {
            Assert.IsFalse(_actor.Intent.Locked(Verb.EmoteWheel));
            Assert.IsTrue(_emotes.CanEmote());
        }

        [Test] public void LessonUnlockingTheEmoteVerbPermitsEmoteEligibility()
        {
            _actor.Intent.AllowOnly(new HashSet<Verb> { Verb.Jump });
            _actor.Intent.AllowOnly(new HashSet<Verb> { Verb.Jump, Verb.EmoteWheel });
            Assert.IsFalse(_actor.Intent.Locked(Verb.EmoteWheel));
            Assert.IsTrue(_emotes.CanEmote());
        }
    }
}
