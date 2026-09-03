using System.Collections;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The world-held slipper rides the hand by copying a transform every frame
    /// (`Carrier.RideAnchor`); it is never reparented onto the character, so the FPP self-hide
    /// sweep that walks `_character`'s own children never reached it. Reported as a visibly
    /// floating, detached tsinelas next to the viewmodel's own correctly-attached one.
    /// </summary>
    public class CarriedSlipperSelfHideProbe
    {
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code. `PlayModeWorld.Reset` has the
        /// mechanism and why BOTH hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator HeldSlipperGoesShadowsOnlyInLocalFpp()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 20; i++) yield return null;

            GameServices.Round.BeginRound();
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

            CharacterMotor motor = null;
            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
                if (!m.IsDefender) { motor = m; break; } // an attacker starts holding a slipper

            Assert.IsNotNull(motor);

            var ai = motor.GetComponent<AIController>();
            if (ai != null) ai.enabled = false;

            var carrier = motor.GetComponent<Carrier>();
            Assert.IsNotNull(carrier);

            // Force it directly rather than waiting on the round-start equip pipeline (ready
            // gate -> StartMatch -> RoundStarted -> SliceRunner.EquipOwnedSlippers), which this
            // probe has no business depending on the timing of.
            if (carrier.Held == null)
            {
                Slipper any = null;
                foreach (var s in Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
                {
                    any = s;
                    break;
                }

                Assert.IsNotNull(any, "no slipper in the scene to force onto the attacker");
                any.HostForceEquip(motor);
                yield return null;
            }

            Assert.IsNotNull(carrier.Held,
                $"the attacker (seat {motor.PlayerSlot}) is not holding a slipper to test with " +
                $"(roundActive={motor.RoundActive})");

            var rig = Object.FindFirstObjectByType<CameraRig>();
            rig.Follow(motor);

            for (int i = 0; i < 5; i++) yield return null;

            var renderer = carrier.Held.GetComponentInChildren<Renderer>();
            Assert.IsNotNull(renderer);

            Assert.AreEqual(ShadowCastingMode.ShadowsOnly, renderer.shadowCastingMode,
                "the held world slipper is still fully rendered in local FPP; it should be " +
                "shadows-only, same as the rest of the local player's own body.");

            // Leaving FPP (a Prop swap, a spectator handover, anything) must give it back.
            rig.SetActive(false);

            Assert.AreNotEqual(ShadowCastingMode.ShadowsOnly, renderer.shadowCastingMode,
                "the held slipper was not restored after the rig stopped following its carrier.");
        }
    }
}
