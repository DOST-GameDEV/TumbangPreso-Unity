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

        /// <summary>
        /// A remote unit's MESH glides while its BODY snaps.
        ///
        /// ⚠️⚠️ THE BODY MUST KEEP SNAPPING AND ONLY THE MESH MAY GLIDE. A replicated update is
        /// written straight onto the body every time one lands, because collision, the hitbox
        /// offset and every directional verb read the body transform directly. Smoothing the
        /// body would lag the gameplay; smoothing the mesh means what you see glides while what
        /// the rules read stays exact. The Godot original spells that out and this port had no
        /// counterpart at all.
        ///
        /// ⚠️ AND IT IS OFF BY DEFAULT, so a single-player match is bit-for-bit unchanged. The
        /// test turns it on rather than finding it on.
        /// </summary>
        [UnityTest]
        public IEnumerator RemoteSmoothingLagsTheMeshAndThenCatchesUp()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            for (int i = 0; i < 20; i++) yield return null;

            var visual = Object.FindFirstObjectByType<CharacterVisual>();
            Assert.IsNotNull(visual, "The arena built no character visuals.");

            var root = visual.transform.Find("Visual");
            Assert.IsNotNull(root,
                "The seat has no `Visual` child, so the mesh has nothing to lag on and the " +
                "floor alignment is moving the CharacterController instead.");

            // ⚠️ THE MOTOR AND ITS CONTROLLER ARE STOOD DOWN FOR THIS. A live seat pins itself
            // to its spawn for the first physics steps and a CharacterController fights a
            // direct position write, so both would drag the body back under the mesh and the
            // measured lag would be whatever the fight settled at. The first run of this test
            // read 0.169 m of a 2 m jump for exactly that reason.
            var motor = visual.GetComponent<CharacterMotor>();
            if (motor != null) motor.enabled = false;

            var controller = visual.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;

            // ⚠️ MEASURED AGAINST THE ALIGNED REST POSITION, NOT AGAINST ZERO. This node is
            // already offset: `AlignToCapsuleFloor` drops the rig so its feet meet the bottom of
            // the capsule, and the smoothing adds to that rather than replacing it. Comparing to
            // zero asserts the alignment away, which is how the first run of this test "failed"
            // at a residual of exactly the drop.
            Vector3 rest = root.localPosition;

            visual.SmoothRemote = true;
            visual.SnapRemoteTransform();

            yield return null;

            // A replicated jump: the body moves, the mesh must not arrive with it.
            Vector3 from = visual.transform.position;
            visual.transform.position = from + new Vector3(2.0f, 0.0f, 0.0f);

            yield return null;

            Assert.Greater((root.localPosition - rest).magnitude, 0.2f,
                "The mesh arrived with the body, so nothing is being smoothed.");

            // ⚠️⚠️ WAIT ON TIME, NOT ON FRAMES. The smoothing closes a fixed fraction of the
            // gap per SECOND, and the batch test runner renders at over 500 fps: ninety frames
            // is a sixth of a second there and the mesh is still visibly behind. The first run
            // of this test failed on exactly that and the maths was right the whole time.
            float waited = 0.0f;
            while (waited < 0.8f) { waited += Time.deltaTime; yield return null; }

            Assert.Less((root.localPosition - rest).magnitude, 0.05f,
                "The mesh never caught up with the body, so a remote unit would render " +
                "permanently beside itself.");

            // ⚠️ AND TURNING IT OFF RETURNS THE MESH IMMEDIATELY. Leaving the offset behind is
            // how every character ends up parked next to its own capsule.
            visual.transform.position = from;
            visual.SmoothRemote = false;

            yield return null;

            Assert.Less((root.localPosition - rest).magnitude, 0.0001f,
                "Turning smoothing off left the mesh offset from its body.");
        }
    }
}
