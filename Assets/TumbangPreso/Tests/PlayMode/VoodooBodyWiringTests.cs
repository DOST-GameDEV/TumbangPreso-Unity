using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// ⚠️ PHAISTER'S VOODOO STATE ON A BODY, WIRED INTO THE BODY (HERO-10 v3). `CharacterMotor.Voodoo.cs` held the state and the
    /// wire carried it (protocol 73); these check that the body now RUNS it: the status clock steps it, the passive reaches the
    /// speed, the HUD lists the curses, and a cleanse and a round reset end them. The owner's table: *"Whenever Phaister marks
    /// someone she takes 10% of their speed and they slow down by 10%"*, DRAINED *"Depletes stamina to 0. Prevents stamina recovery
    /// for 2.5 seconds."*, HEXED *"Hallucinations of slippers randomly appear on your screen for 7.5 seconds."*
    /// </summary>
    public sealed class VoodooBodyWiringTests
    {
        [UnitySetUp] public IEnumerator Before() { yield return PlayModeWorld.Reset(); }
        [UnityTearDown] public IEnumerator After() { yield return PlayModeWorld.Reset(); }

        private static CharacterMotor Body(int slot)
        {
            var go = new GameObject("Voodoo body " + slot);
            var motor = go.AddComponent<CharacterMotor>();
            motor.PlayerSlot = slot;
            motor.enabled = false;
            GameServices.Ensure();
            GameServices.Round.Register(motor);
            return motor;
        }

        private static void Step(CharacterMotor body, float seconds)
        {
            var step = typeof(CharacterMotor).GetMethod("StepStatuses",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            for (float t = 0; t < seconds; t += 0.05f) step.Invoke(body, new object[] { 0.05f });
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator TheMarkSlowsItsTargetSpeedsHerAndGoesOffAsDrained()
        {
            var her = Body(0);
            var them = Body(1);
            yield return null;

            them.HostVoodooMark(VoodooMarkKind.Drain, her.PlayerSlot);
            Assert.AreEqual(VoodooRules.PassiveSpeedScale(isTheCaster: false), them.StatusSpeedScale, 1e-4f,
                "The marked one does not run 10 per cent slower.");
            Assert.AreEqual(VoodooRules.PassiveSpeedScale(isTheCaster: true), her.StatusSpeedScale, 1e-4f,
                "She does not take the 10 per cent while her mark lives.");

            Step(them, VoodooRules.DrainDelaySeconds + 0.1f);
            Assert.IsTrue(them.IsDrained, "The drain mark did not go off after its delay (the status clock does not step it).");
            Assert.Greater(them.StatusLeft(StatusKind.Drained), 0.0f);
            Assert.IsTrue(them.Stamina.RecoveryBlocked);
            Assert.AreEqual(VoodooMarkKind.None, them.VoodooMark);
            Assert.AreEqual(1.0f, them.StatusSpeedScale, 1e-4f, "The passive outlived its mark.");
            Assert.AreEqual(1.0f, her.StatusSpeedScale, 1e-4f);

            var live = new List<StatusKind>();
            StatusIcons.Live(them, live);
            CollectionAssert.Contains(live, StatusKind.Drained, "The HUD does not list DRAINED.");
            Assert.IsNotNull(StatusIcons.For(StatusKind.Drained), "No DRAINED icon.");

            Step(them, StatusRules.DrainedSeconds + 0.1f);
            Assert.IsFalse(them.IsDrained);
            Assert.IsFalse(them.Stamina.RecoveryBlocked, "Recovery stayed blocked after DRAINED ended.");
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ACleanseAndARoundResetEndTheCurses()
        {
            var her = Body(0);
            var them = Body(1);
            yield return null;

            them.ApplyHexed();
            var live = new List<StatusKind>();
            StatusIcons.Live(them, live);
            CollectionAssert.Contains(live, StatusKind.Hexed, "The HUD does not list HEXED.");
            Assert.IsNotNull(StatusIcons.For(StatusKind.Hexed), "No HEXED icon.");
            Assert.Greater(them.StatusLeft(StatusKind.Hexed), 0.0f);

            them.HostVoodooMark(VoodooMarkKind.Hex, her.PlayerSlot);
            them.CleanseStatuses();
            Assert.IsFalse(them.IsHexed, "A cleanse left HEXED.");
            Assert.AreEqual(VoodooMarkKind.None, them.VoodooMark, "A cleanse left her curse waiting.");

            them.ApplyDrained();
            them.HostVoodooMark(VoodooMarkKind.Hex, her.PlayerSlot);
            them.ClearStatuses();
            Assert.IsFalse(them.IsDrained, "The round reset left DRAINED.");
            Assert.IsFalse(them.Stamina.RecoveryBlocked);
            Assert.AreEqual(VoodooMarkKind.None, them.VoodooMark, "The round reset left her mark.");
        }
    }
}
