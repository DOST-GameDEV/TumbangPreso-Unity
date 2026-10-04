using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class PowerReadoutRecastTests
    {
        readonly List<GameObject> _owned = new();
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [TearDown] public void Cleanup()
        { foreach (var go in _owned) if (go != null) Object.DestroyImmediate(go); _owned.Clear(); }
        GameObject Own(GameObject go) { _owned.Add(go); return go; }
        [TestCase("available", false)]
        [TestCase("spent-but-cooled", true)]
        [TestCase("expired", true)]
        [TestCase("zapped", true)]
        public void OwnerIconMatchesTheRealSecondCutWindow(string state, bool muted)
        {
            var floor = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.name = "FloorReadout"; floor.transform.position = Vector3.down * .5f;
            floor.transform.localScale = new Vector3(40, 1, 40);
            var actor = Own(new GameObject("Readout owner"));
            var motor = actor.AddComponent<CharacterMotor>(); motor.PlayerSlot = 1; motor.enabled = false;
            motor.Mode = GameMode.HeroStrike;
            var system = actor.AddComponent<HeroAbilitySystem>(); system.enabled = false; system.BindHero("zack");
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(motor);
            GameServices.Match.ApplySnapshot(new int[4], 1, true);
            GameServices.Round.ApplySnapshot(100, true, 0, true);
            var kit = (ZackHeroKit)system.Kit;
            kit.RestoreTimedKit(motor, new TimedKitSnapshot(kit.AttackingSkill, 0, kit.Ultimate, 0, ultimatePermanent: true));
            var ctx = new AbilityContext(motor, null, null);
            var cast = kit.Skill1.CaptureLocalCastContext(ctx);
            using (NetCue.SuppressRelay()) Assert.AreEqual(HeroKit.CastOutcome.Cast, kit.CastSkill1(cast));
            kit.Skill1.Tick(ctx, .16f); kit.Skill1.Tick(ctx, .6f);
            Assert.IsTrue(kit.Skill1.ReactivateReady);
            if (state == "spent-but-cooled")
            {
                using (NetCue.SuppressRelay()) Assert.AreEqual(HeroKit.CastOutcome.Cast, kit.CastSkill1(cast));
                kit.Skill1.Tick(ctx, .16f); kit.OnObjectiveAwarded(20);
                Assert.IsTrue(kit.Skill1.IsActive); Assert.IsTrue(kit.Skill1.IsReady);
                Assert.IsFalse(kit.Skill1.ReactivateReady);
            }
            else if (state == "expired") kit.Skill1.Tick(ctx, 1.01f);
            else if (state == "zapped") motor.ApplyZapped();
            else Assert.IsFalse(kit.Skill1.IsReady, "The original cooldown continues during the real follow-up window.");
            var canvas = Own(new GameObject("Readout canvas", typeof(RectTransform), typeof(Canvas)));
            var readout = canvas.AddComponent<TumpPowerReadout>(); readout.Build(canvas.transform); readout.Tick(system, true);
            var symbols = (TumpAbilitySymbol[])typeof(TumpPowerReadout)
                .GetField("_symbols", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(readout);
            Assert.AreEqual(muted, symbols[0].Muted, "The actionable second cut must agree with the visible readiness cue.");
        }
    }
}
