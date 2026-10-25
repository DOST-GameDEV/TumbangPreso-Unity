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
        // Zack's Quick Circuit and its second cut were retired on 2026-10-25. What is left to hold is the plain rule: the
        // owner's tile is muted exactly when the press would be refused (cooling, or Zapped by a Live Wire).
        [TestCase("available", false)]
        [TestCase("cooling", true)]
        [TestCase("zapped", true)]
        public void OwnerIconMutesExactlyWhenThePressWouldBeRefused(string state, bool muted)
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
            if (state == "cooling") kit.Skill1.ApplyNetworkSnapshot(10, 0);
            else if (state == "zapped") motor.ApplyZapped();
            Assert.AreEqual(state == "cooling", !kit.Skill1.IsReady);
            var canvas = Own(new GameObject("Readout canvas", typeof(RectTransform), typeof(Canvas)));
            var readout = canvas.AddComponent<TumpPowerReadout>(); readout.Build(canvas.transform); readout.Tick(system, true);
            var symbols = (TumpAbilitySymbol[])typeof(TumpPowerReadout)
                .GetField("_symbols", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(readout);
            Assert.AreEqual(muted, symbols[0].Muted, "The tile must agree with its cooldown and stun state.");
        }
    }
}
