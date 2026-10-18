using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class AiTagCommitmentTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        private static void Call(object owner, string method, params object[] args)
            => owner.GetType().GetMethod(method, Hidden).Invoke(owner, args);

        [TestCase(90f, 3f, 0f), TestCase(25f, 3.5f, 0f), TestCase(25f, 3.5f, 4f)]
        public void ChargedLungeWaitsForAReachableAim(float angle, float distance, float walkingRight)
        {
            UI.SceneFlow.Networked=false;
            UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameServices.Ensure();GameServices.Round.Clear();
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=Vector3.down*.1f;floor.transform.localScale=new Vector3(30,.2f,30);
            GameServices.Round.Lata=new GameObject("Lunge aim can").AddComponent<Lata>();
            GameServices.Round.Lata.transform.position=new Vector3(5,0,5);
            CharacterMotor Seat(int slot,Vector3 at)
            {
                var go=new GameObject("Lunge aim seat"+slot,typeof(CharacterController));
                var cc=go.GetComponent<CharacterController>();cc.height=1.6f;cc.radius=.35f;cc.center=Vector3.up*.8f;
                var motor=go.AddComponent<CharacterMotor>();motor.enabled=false;motor.PlayerSlot=slot;motor.Mode=GameMode.Classic;motor.SpawnPosition=at;
                go.AddComponent<Carrier>();go.AddComponent<CombatVerbs>().enabled=false;GameServices.Round.Register(motor);return motor;
            }
            var actor=Seat(0,new Vector3(0,.1f,-3));
            Vector3 offset=Quaternion.Euler(0,angle,0)*Vector3.forward*distance;
            var victim=Seat(1,actor.SpawnPosition+offset);
            GameServices.Match.StartMatch();GameServices.Round.BeginRound();actor.IsDefender=true;victim.IsDefender=false;victim.HoldingSlipper=true;
            actor.transform.position=actor.SpawnPosition;victim.transform.position=victim.SpawnPosition;actor.transform.forward=Vector3.forward;
            var ai=actor.gameObject.AddComponent<AIController>();ai.enabled=false;typeof(AIController).GetProperty("Plan").SetValue(ai,AiPlan.Hunt);
            var verbs=actor.GetComponent<CombatVerbs>();actor.Intent.Set(Verb.Lunge,true);Call(verbs,"StepLunge",1f);
            typeof(AIController).GetField("_lungeHeld",Hidden).SetValue(ai,1f);actor.Intent.CommitFrame();Physics.SyncTransforms();
            typeof(CharacterMotor).GetField("_velocity",Hidden).SetValue(actor,Vector3.right*walkingRight);
            Assert.IsTrue(victim.IsTaggable());
            Assert.Greater(Mathf.Abs(offset.x),Balance.LungeTagRadius*victim.TagReachScale,
                "Even an angle inside the planner cone can miss the real lunge corridor");
            Call(ai,"StepLungeIntent",actor.Intent,victim,.016f);Call(verbs,"StepLunge",.016f);
            if (walkingRight > 0)
            {
                Assert.Greater(verbs.LungeCooldownLeft,0,
                    "Current walking travel makes this dash reachable despite the static corridor");
                Assert.That(verbs.PunchCooldownLeft,Is.EqualTo(0));
                return;
            }
            Assert.That(verbs.LungeCooldownLeft,Is.EqualTo(0),"A full charge cannot justify a dash aimed away from the visible target");
            Assert.Greater(verbs.ObservedLungeCharge,0,"The real held input must retain the charge while the body turns");
            Assert.Greater(actor.Intent.MoveAxis.x,.7f,"The bot must actively turn toward the target rather than hold forever");
            actor.transform.forward=(victim.transform.position-actor.transform.position).normalized;
            actor.Intent.CommitFrame();
            Call(ai,"StepLungeIntent",actor.Intent,victim,.016f);Call(verbs,"StepLunge",.016f);
            Assert.Greater(verbs.LungeCooldownLeft,0,"Once aligned, the normal release edge must spend the committed lunge");
            Assert.That(verbs.PunchCooldownLeft,Is.EqualTo(0),"Do not substitute another tag for the committed charge");
        }

        [TestCase(false), TestCase(true)]
        public void NearTargetUsesOneTagCommitment(bool alreadyCharging)
        {
            UI.SceneFlow.Networked = false;
            UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameServices.Ensure(); GameServices.Round.Clear();
            GameServices.Round.Lata = new GameObject("Bot tag can").AddComponent<Lata>();
            GameServices.Round.Lata.transform.position = new Vector3(5, 0, 5);
            CharacterMotor Seat(int slot, Vector3 position)
            {
                var go = new GameObject("Bot tag seat " + slot, typeof(CharacterController));
                var cc = go.GetComponent<CharacterController>(); cc.height = 1.6f; cc.radius = .35f; cc.center = Vector3.up * .8f;
                var motor = go.AddComponent<CharacterMotor>(); motor.enabled = false;
                motor.PlayerSlot = slot; motor.Mode = GameMode.Classic; motor.SpawnPosition = position;
                go.AddComponent<Carrier>(); go.AddComponent<CombatVerbs>().enabled = false;
                go.transform.position = position; go.transform.forward = Vector3.forward;
                GameServices.Round.Register(motor); return motor;
            }
            var actor = Seat(0, new Vector3(0, 1, -3));
            var victim = Seat(1, new Vector3(0, 1, -2));
            GameServices.Match.StartMatch(); GameServices.Round.BeginRound();
            actor.IsDefender = true; victim.IsDefender = false; victim.HoldingSlipper = true;
            actor.transform.position = new Vector3(0, 1, -3);
            victim.transform.position = new Vector3(0, 1, -2);
            Assert.IsTrue(actor.CanAct()); Assert.IsTrue(victim.IsTaggable());
            var ai = actor.gameObject.AddComponent<AIController>(); ai.enabled = false;
            typeof(AIController).GetProperty("Plan").SetValue(ai, AiPlan.Hunt);
            var verbs = actor.GetComponent<CombatVerbs>();
            if (alreadyCharging)
            {
                // Seed an already committed charge through the real input consumer.
                // The private planner clock mirrors that same held duration.
                actor.Intent.Set(Verb.Lunge, true);
                Call(verbs, "StepLunge", .2f);
                typeof(AIController).GetField("_lungeHeld", Hidden).SetValue(ai, .2f);
                actor.Intent.CommitFrame();
                Assert.Greater(verbs.ObservedLungeCharge, 0);
            }
            Call(ai, "Act", actor.Intent, AiTuning.LungeHoldTime);
            Call(verbs, "StepPunch");
            Call(verbs, "StepLunge", .016f);
            int spent = (verbs.PunchCooldownLeft > 0 ? 1 : 0) + (verbs.LungeCooldownLeft > 0 ? 1 : 0);
            Assert.AreEqual(1, spent, "A close target must not trigger a punch plus an accidental charged dash.");
            if (alreadyCharging) Assert.AreEqual(0, verbs.PunchCooldownLeft, "Finish the lunge already being held.");
            else Assert.AreEqual(0, verbs.LungeCooldownLeft, "A fresh close target should get the immediate punch.");
        }
    }
}
