using System.Collections;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// A carried tsinelas rides the carrier's hand.
    ///
    /// ⚠️⚠️ THIS IS THE REGRESSION TEST FOR A COMPONENT THAT COULD NOT WORK IN ANY BUILD.
    /// `Carrier` took its hand transform from a `[SerializeField]`, and `MatchInstaller`
    /// installs it with `AddComponent`, which cannot carry an inspector reference. The field was
    /// null on every unit ever built, so the one line that keeps a held slipper in the hand
    /// never ran: a picked-up tsinelas stayed exactly where the pickup left it and its carrier
    /// walked away from it. That is the third-person half of "the slippers just float when you
    /// hold it, its completely unattached to person", and the viewmodel fix hid it from the one
    /// player who could not see it anyway.
    ///
    /// ⚠️ THE ASSERTION IS THAT IT MOVES WITH THE ARM, not that it is at some coordinate. The
    /// offset is measured off the skin at runtime, so a number here would be asserting the
    /// measurement rather than the behaviour, and the behaviour is what was broken.
    /// </summary>
    public class CarryTests
    {
        [UnityTest]
        public IEnumerator TheHandAnchorLandsOnTheHandAndRidesIt()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            for (int i = 0; i < 20; i++) yield return null;

            CharacterVisual visual = null;

            foreach (var v in Object.FindObjectsByType<CharacterVisual>(FindObjectsSortMode.None))
            {
                if (v.HandAnchor == null) continue;
                visual = v;
                break;
            }

            Assert.IsNotNull(visual,
                "No seat built a hand anchor. `arm-right` was not found on any rig, or the skin " +
                "measurement failed, and a carried slipper cannot follow the arm.");

            var anchor = visual.HandAnchor;

            // ⚠️ ON THE BODY, NOT OUT IN THE STREET. The Godot side records eight guessed
            // offsets that each landed somewhere wrong, so the cheap sanity check is that the
            // anchor is inside the character's own drawn bounds rather than half a metre beside
            // them.
            var renderer = visual.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.IsNotNull(renderer);

            var bounds = renderer.bounds;
            bounds.Expand(0.35f);

            Assert.IsTrue(bounds.Contains(anchor.position),
                $"The hand anchor is at {anchor.position}, outside the character's own bounds " +
                $"{bounds}. That is the armpit-or-neck failure the measurement exists to avoid.");

            // ⚠️ AND IT RIDES THE POSE. A child written onto the bone's own transform is
            // overwritten from the pose every frame; a child OF the bone follows it. The idle
            // clip is running, so the anchor has to move in world space.
            Vector3 was = anchor.position;

            for (int i = 0; i < 40; i++) yield return null;

            Assert.Greater(Vector3.Distance(was, anchor.position), 0.0005f,
                "The hand anchor has not moved in 40 frames of a live idle, so it is sitting at " +
                "the bone's rest transform rather than tracking the animated pose.");
        }
    }
}
