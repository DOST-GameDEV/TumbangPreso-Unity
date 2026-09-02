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
    /// The rest of the emote-camera port beyond "does it orbit": start gating, cancel-on-input,
    /// cancel-on-state-loss, and the one failure this cannot be allowed to have — a player stuck
    /// in third person with no way back.
    /// </summary>
    public class EmoteLifecycleProbe
    {
        private static CharacterMotor FindDefender()
        {
            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
                if (m.IsDefender) return m;
            return null;
        }

        private static IEnumerator Setup(System.Action<CharacterMotor, CameraRig, EmotePlayer> onReady)
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 20; i++) yield return null;

            GameServices.Round.BeginRound();
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

            var motor = FindDefender();
            Assert.IsNotNull(motor, "no defender seat found");

            var ai = motor.GetComponent<AIController>();
            if (ai != null) ai.enabled = false;
            motor.Intent.Clear();

            var rig = Object.FindFirstObjectByType<CameraRig>();
            rig.Follow(motor);

            var emotes = motor.GetComponent<EmotePlayer>();
            Assert.IsTrue(emotes.CanEmote());

            onReady(motor, rig, emotes);
        }

        [UnityTest]
        public IEnumerator UnknownEmoteIdDoesNotSwingTheCamera()
        {
            CharacterMotor motor = null;
            CameraRig rig = null;
            EmotePlayer emotes = null;
            yield return Setup((m, r, e) => { motor = m; rig = r; emotes = e; });

            emotes.HostPlay("not-a-real-emote-id");

            for (int i = 0; i < 5; i++) yield return null;

            Assert.IsFalse(emotes.IsEmoting, "an unknown id should never start the emote at all");
            Assert.IsFalse(rig.IsEmoteView, "an unknown id must not swing the camera");
        }

        [UnityTest]
        public IEnumerator CannotReTriggerWhileAlreadyEmoting()
        {
            CharacterMotor motor = null;
            CameraRig rig = null;
            EmotePlayer emotes = null;
            yield return Setup((m, r, e) => { motor = m; rig = r; emotes = e; });

            emotes.HostPlay(Emotes.All[0].Id);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsTrue(emotes.IsEmoting);

            Assert.IsFalse(emotes.CanEmote(), "CanEmote() must refuse a second press mid-emote");
        }

        [UnityTest]
        public IEnumerator MovementCancelsTheEmoteAndRestoresTheCamera()
        {
            CharacterMotor motor = null;
            CameraRig rig = null;
            EmotePlayer emotes = null;
            yield return Setup((m, r, e) => { motor = m; rig = r; emotes = e; });

            emotes.HostPlay(Emotes.All[0].Id);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsTrue(rig.IsEmoteView, "the emote never started, so cancelling it proves nothing");

            motor.Intent.Move = new Vector2(0.0f, 1.0f);
            for (int i = 0; i < 3; i++) yield return null;

            Assert.IsFalse(emotes.IsEmoting, "movement must cancel the emote");
            Assert.IsFalse(rig.IsEmoteView, "the camera must return once movement cancels it");
        }

        [UnityTest]
        public IEnumerator LosingCanActEndsTheEmoteAndRestoresTheCamera()
        {
            CharacterMotor motor = null;
            CameraRig rig = null;
            EmotePlayer emotes = null;
            yield return Setup((m, r, e) => { motor = m; rig = r; emotes = e; });

            emotes.HostPlay(Emotes.All[0].Id);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsTrue(rig.IsEmoteView);

            motor.ApplyStagger(1.0f);
            for (int i = 0; i < 3; i++) yield return null;

            Assert.IsFalse(emotes.IsEmoting, "a stun (tag/shove/etc) must end the emote");
            Assert.IsFalse(rig.IsEmoteView, "the camera must return once control is lost");
        }

        [UnityTest]
        public IEnumerator SwappingWhoTheRigFollowsNeverLeavesItStuckInEmoteView()
        {
            CharacterMotor motor = null;
            CameraRig rig = null;
            EmotePlayer emotes = null;
            yield return Setup((m, r, e) => { motor = m; rig = r; emotes = e; });

            emotes.HostPlay(Emotes.All[0].Id);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsTrue(rig.IsEmoteView);

            CharacterMotor other = null;
            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
                if (m != motor) { other = m; break; }
            Assert.IsNotNull(other, "need a second seat to swap the rig onto");

            // Nothing here ever calls Stop()/EndEmoteView() - this is the exact swap (a
            // spectator cycle, a seat reassignment) the note on Follow() describes.
            rig.Follow(other);

            Assert.IsFalse(rig.IsEmoteView,
                "Follow() must clear a stuck emote view rather than carrying it onto the next body");
        }
    }
}
