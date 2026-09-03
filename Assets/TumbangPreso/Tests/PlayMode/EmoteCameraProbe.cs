using System.Collections;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Social;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Does an emote turn the camera to look at the character from behind (correct: an orbit)
    /// or does it spin the character itself around to face the camera (the reported bug)?
    /// </summary>
    public class EmoteCameraProbe
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
        public IEnumerator EmoteOrbitsWithoutTurningTheBody()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 20; i++) yield return null;

            GameServices.Round.BeginRound();
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

            var rig = Object.FindFirstObjectByType<CameraRig>();
            Assert.IsNotNull(rig, "no CameraRig found in scene");

            // The taya doesn't carry a slipper, so it is the seat that can actually emote right
            // after a round begins; every attacker starts holding one and CanEmote() forbids it.
            CharacterMotor motor = null;
            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
            {
                if (m.IsDefender) { motor = m; break; }
            }
            Assert.IsNotNull(motor, "no defender seat found");

            // The AI would immediately press a move/verb intent and abort the emote on its own,
            // which is correct bot behaviour but not what this probe is measuring.
            var ai = motor.GetComponent<AIController>();
            if (ai != null) ai.enabled = false;
            motor.Intent.Clear();

            rig.Follow(motor);

            var emotes = motor.GetComponent<EmotePlayer>();
            Assert.IsNotNull(emotes, "local player has no EmotePlayer");
            Assert.IsTrue(emotes.CanEmote(),
                $"CanEmote() is false (roundActive={motor.RoundActive} stunned={motor.IsStunned} " +
                $"holdingSlipper={motor.HoldingSlipper})");

            Vector3 forwardBefore = motor.transform.forward;
            Quaternion rotBefore = motor.transform.rotation;

            emotes.HostPlay(Emotes.All[0].Id);

            for (int i = 0; i < 5; i++) yield return null;

            Assert.IsTrue(rig.IsEmoteView, "emote did not switch the rig to the emote view");

            Vector3 forwardAfter = motor.transform.forward;
            Assert.Less(Quaternion.Angle(rotBefore, motor.transform.rotation), 0.5f,
                $"the character's own rotation changed during the emote " +
                $"({forwardBefore} -> {forwardAfter}); an emote must orbit the camera, not steer the body.");

            // Camera should sit BEHIND the character (on the opposite side of its facing) and
            // look in roughly the same direction the character faces, not in front of it
            // looking back at the face.
            Vector3 toCamera = (rig.transform.position - motor.transform.position);
            toCamera.y = 0.0f;
            float behindDot = Vector3.Dot(toCamera.normalized, forwardAfter);

            Assert.Less(behindDot, 0.0f,
                $"the emote camera is in FRONT of the character (dot={behindDot:F2}), looking " +
                "back at their face, instead of opening behind them.");

            emotes.Stop();
        }
    }
}
