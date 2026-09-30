using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class TimedPowerUiTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        sealed class Clock : HeroAbility
        {
            readonly bool _recast;
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.HostConfirmed;
            public override bool CanReactivate => _recast;
            public Clock(string id, bool recast) : base(id, "Timer", "Timer UI fixture", 5)
            { Duration = 4; _recast = recast; RestoreLiveClock(4); }
        }
        sealed class Kit : HeroKit
        {
            public Kit() : base("timer-ui", "Timer UI")
            { Skill1 = new Clock("timer1", false); Skill2 = new Clock("timer2", true); Ultimate = new Clock("timer3", false); }
        }

        [UnityTest] public IEnumerator ActiveTimersDrainForBasicRecastAndUltimateThenDisappear()
        {
            var root = new GameObject("TimedPowerReview");
            var actor = new GameObject("TimerActor", typeof(CharacterController), typeof(CharacterMotor));
            try
            {
                var system = actor.AddComponent<HeroAbilitySystem>(); system.enabled = false;
                var kit = new Kit();
                typeof(HeroAbilitySystem).GetProperty("Kit").SetValue(system, kit);
                var canvas = OwnerUiLayout.Canvas(root.transform, "TimedPowerCanvas");
                var readout = root.AddComponent<TumpPowerReadout>(); readout.Build(canvas.transform);
                var dials = canvas.GetComponentsInChildren<OwnerAbilitySeal>(true);
                var fill = typeof(OwnerAbilitySeal).GetField("_fill", BindingFlags.Instance | BindingFlags.NonPublic);
                readout.Tick(system, true);
                Assert.That((float)fill.GetValue(dials[2]), Is.EqualTo(1).Within(.001), "Active ultimate must show lifetime, not its empty objective bank.");
                kit.Skill1.Tick(null, 1); kit.Skill2.Tick(null, 1); kit.Ultimate.Tick(null, 1);
                readout.Tick(system, true);
                foreach (var dial in dials) Assert.That((float)fill.GetValue(dial), Is.EqualTo(.75f).Within(.001));
                var labels = dials.Select(d => d.GetComponentInChildren<Text>().text).ToArray();
                Assert.AreEqual("3.0s", labels[0]); Assert.AreEqual("Again\n3.0s", labels[1]); Assert.AreEqual("3.0s", labels[2]);
                yield return TumpUiCapture.Capture("Timed-powers-960x540", canvas, 960, 540, false, true);
                yield return TumpUiCapture.Capture("Timed-powers-1600x680", canvas, 1600, 680, false, true);
                kit.Skill1.Tick(null, 3); kit.Skill2.Tick(null, 3); kit.Ultimate.Tick(null, 3);
                readout.Tick(system, true);
                foreach (var dial in dials) Assert.IsFalse(dial.GetComponentInChildren<Text>().text.Contains("s"), "Expired duration must disappear.");
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(actor); }
        }
    }
}
