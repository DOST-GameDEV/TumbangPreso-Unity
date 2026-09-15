using System.Collections;
using NUnit.Framework;
using TumbangPreso.Abilities;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class PendingPreparationTests
    {
        private INetProvider _net;
        [UnitySetUp]public IEnumerator Before()
        { _net=NetAuthority.Provider;yield return PlayModeWorld.Reset();NetAuthority.Provider=new SoloProvider(); }
        [UnityTearDown]public IEnumerator After()
        { yield return PlayModeWorld.Reset();NetAuthority.Provider=_net; }
        private sealed class Preparation : HeroAbility
        {
            public int Impacts;public Vector3 At,Aim;
            public Preparation():base("pending-test","Pending test","",0,1,charges:2)
            { Windup=.4f;SupportsPendingSnapshot=true; }
            protected override void OnActivate(AbilityContext context)
            { Impacts++;At=context.Position;Aim=context.AimPoint; }
        }
        private static CharacterMotor Motor(string name)
        {
            var go=new GameObject(name);go.AddComponent<CharacterController>();
            var motor=go.AddComponent<CharacterMotor>();motor.PlayerSlot=GameLaunch.SoloSeat;motor.IsBot=true;motor.enabled=false;
            return motor;
        }
        private static AbilityContext Context(CharacterMotor motor,Vector3 position,Vector3 aim)
            =>new AbilityContext(motor,null,null,position,Vector3.right,aim);

        [UnityTest]
        public IEnumerator RestorationPreservesCapturedAimAndSpendsNoSecondCharge()
        {
            var original=Motor("Original");var replica=Motor("Replica");
            var at=new Vector3(2,0,4);var aim=new Vector3(9,.1f,4);
            var source=new Preparation();var restored=new Preparation();
            var originalContext=Context(original,at,aim);var replicaContext=Context(replica,at,aim);
            try
            {
                source.Activate(originalContext);source.Tick(originalContext,.1f);
                Assert.IsTrue(source.CapturePendingPreparation(out var captured,out float remaining));
                Assert.AreEqual(at,captured.Position);Assert.AreEqual(aim,captured.AimPoint);
                restored.ApplyNetworkSnapshot(source.CooldownRemaining,source.ChargesRemaining);
                Assert.IsTrue(restored.RestoreJoiningPreparation(replicaContext,remaining,.37f));
                Assert.AreEqual(1,restored.ChargesRemaining);Assert.AreEqual(.37f,restored.HeldSecondsOnCast);
                Assert.AreEqual(original.SpeedMultiplier,replica.SpeedMultiplier);Assert.Less(replica.SpeedMultiplier,1);
                replica.transform.position=new Vector3(99,0,99);
                var live=Context(replica,replica.transform.position,Vector3.back*30);
                restored.Tick(live,.29f);Assert.AreEqual(0,restored.Impacts);
                restored.Tick(live,.02f);Assert.AreEqual(1,restored.Impacts);
                Assert.AreEqual(at,restored.At);Assert.AreEqual(aim,restored.Aim);
                Assert.AreEqual(1,restored.ChargesRemaining);Assert.AreEqual(1,replica.SpeedMultiplier);
                Assert.IsFalse(restored.RestoreJoiningPreparation(replicaContext,.2f,.8f));
                Assert.AreEqual(1,restored.Impacts);
            }
            finally
            {
                source.ResetForRound(originalContext);restored.ResetForRound(replicaContext);
                Object.DestroyImmediate(original.gameObject);Object.DestroyImmediate(replica.gameObject);
            }
            yield return null;
        }
        [UnityTest]
        public IEnumerator EmptyOrKnownCastStateCannotBeRearmedAndResetReleasesTheRoot()
        {
            var motor=Motor("Known");var context=Context(motor,Vector3.zero,Vector3.forward);
            var empty=new Preparation();var known=new Preparation();
            try
            {
                Assert.IsFalse(empty.RestoreJoiningPreparation(context,0,0));
                Assert.IsFalse(empty.RestoreJoiningPreparation(context,.2f,0));
                Assert.AreEqual(0,empty.Impacts);Assert.AreEqual(1,motor.SpeedMultiplier);
                known.Activate(context);known.Tick(context,.1f);
                Assert.IsFalse(known.RestoreJoiningPreparation(context,.39f,0));
                Assert.That(known.WindupRemaining,Is.EqualTo(.3f).Within(.0001f));
                known.ResetForRound(context);Assert.AreEqual(1,motor.SpeedMultiplier);
                Assert.IsFalse(known.RestoreJoiningPreparation(context,.2f,0));
                Assert.AreEqual(0,known.Impacts);
            }
            finally { known.ResetForRound(context);Object.DestroyImmediate(motor.gameObject); }
            yield return null;
        }
        [UnityTest]
        public IEnumerator InvalidInitialTimingDoesNotPoisonTheNextValidSnapshot()
        {
            var motor=Motor("Timing");var context=Context(motor,Vector3.zero,Vector3.forward);var ability=new Preparation();
            try
            {
                foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,-.1f,.5f})
                    Assert.IsFalse(ability.RestoreJoiningPreparation(context,invalid,0));
                Assert.IsFalse(ability.RestoreJoiningPreparation(context,.2f,float.NaN));
                Assert.IsTrue(ability.RestoreJoiningPreparation(context,.2f,.25f));
                ability.ResetForRound(context);Assert.AreEqual(1,motor.SpeedMultiplier);
            }
            finally { ability.ResetForRound(context);Object.DestroyImmediate(motor.gameObject); }
            yield return null;
        }
    }
}
