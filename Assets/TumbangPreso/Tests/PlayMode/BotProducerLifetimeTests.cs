using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class BotProducerLifetimeTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator DisablingProducerRetiresOldWindupsWithoutClearingSharedHeroInput()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            var body = GameServices.Round.PlayerAt(2); var brain = body.GetComponent<AIController>();
            Assert.IsNotNull(brain);
            object Read(string name) => typeof(AIController).GetField(name, Hidden).GetValue(brain);
            void Write(string name, object value) => typeof(AIController).GetField(name, Hidden).SetValue(brain, value);
            foreach (bool ownsHeroKeys in new[] { true, false })
            {
                brain.AbilitiesEnabled = ownsHeroKeys; brain.enabled = true;
                Write("_windup", true); Write("_lungeHeld", .7f); Write("_goalValid", true);
                Write("_hopHeld", true); Write("_sprintAsked", true);
                var aim = (Dictionary<Verb, float>)Read("_aimHeld"); aim[Verb.Skill1] = .6f;
                var pressed = (HashSet<Verb>)Read("_pressed"); pressed.Add(Verb.Lunge);
                // Practice owns parking/consumer cancellation. This producer exit must
                // only retire its private clocks, including when a human owns hero keys.
                body.Intent.Clear(); body.Intent.CommitFrame(); body.Intent.Parked = false;
                body.Intent.Set(Verb.Skill2, true); body.Intent.CommitFrame();
                brain.enabled = false;
                Assert.IsFalse((bool)Read("_windup"), "Old throw windup survived producer disable.");
                Assert.AreEqual(-1f, (float)Read("_lungeHeld"), "Old lunge hold survived producer disable.");
                Assert.IsFalse((bool)Read("_goalValid")); Assert.IsFalse((bool)Read("_hopHeld"));
                Assert.IsFalse((bool)Read("_sprintAsked")); Assert.IsEmpty(aim); Assert.IsEmpty(pressed);
                Assert.IsTrue(body.Intent.Pressed(Verb.Skill2), "Disabling a producer must preserve another writer's hero hold.");
                Assert.IsFalse(body.Intent.JustReleased(Verb.Skill2), "Producer retirement cannot manufacture a human release.");
                brain.enabled = true;
                Assert.IsFalse((bool)Read("_windup")); Assert.AreEqual(-1f, (float)Read("_lungeHeld"));
                brain.enabled = false;
            }
        }
    }
}
