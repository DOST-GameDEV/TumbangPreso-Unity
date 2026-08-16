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
        /// § THE SLIPPER STAYS ON THE ARM, NO MATTER WHAT. 🧑 2026-08-16: *"make sure the
        /// slippers in unity stay on the arm no matter what — for others and for yourself in ur
        /// FPP"*.
        ///
        /// ⚠️⚠️ THREE THINGS ARE ASSERTED AND THEY ARE THREE DIFFERENT FAILURES. The report has
        /// been made twice about two unrelated causes, so the check covers all of the ways a
        /// carried tsinelas has actually come off:
        ///
        ///  1. **It rides a MOVING, ANIMATING carrier**, frame by frame, not just at rest. The
        ///     original detachment was a one-frame lag that is invisible standing still and
        ///     obvious the moment an arm swings — *"the slippers deattach when animations play"*.
        ///  2. **It survives the anchor disappearing.** A rig whose arm bone does not resolve
        ///     leaves `HandAnchor` null, and the old code returned early and abandoned the
        ///     slipper in the street. It rides the body now; this destroys the anchor outright
        ///     and asserts the slipper still travels with its owner.
        ///  3. **The local player sees one in their own hand.** The viewmodel carries its OWN
        ///     copy, because the real hand is below the frustum in first person, so "attached"
        ///     is two separate mechanisms and only one of them is the world object.
        /// </summary>
        [UnityTest]
        public IEnumerator AHeldSlipperStaysOnTheArmThroughMovementAndAMissingAnchor()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            for (int i = 0; i < 20; i++) yield return null;

            CharacterMotor carrier = null;

            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
            {
                if (!m.IsPerson || m.IsDefender) continue;
                if (m.GetComponent<CharacterVisual>()?.HandAnchor == null) continue;
                carrier = m;
                break;
            }

            Assert.IsNotNull(carrier, "no attacker seat with a hand anchor to carry anything");

            Slipper slipper = null;

            foreach (var s in Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
            {
                if (s.State != SlipperState.Loose) continue;
                slipper = s;
                break;
            }

            Assert.IsNotNull(slipper, "no loose slipper in the arena");

            // ⚠️ THE SLIPPER MOVES ONTO THE CARRIER, NOT THE OTHER WAY AROUND. `Confine` clamps a
            // unit back into the box every step, so walking the capsule to a slipper that happens
            // to lie outside it fails the pickup for a reason that has nothing to do with this.
            var stand = carrier.transform.position;
            slipper.transform.position = new Vector3(stand.x, slipper.transform.position.y, stand.z);

            carrier.RoundActive = true;

            for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();

            Assert.IsTrue(slipper.HostGrab(carrier), "the harness failed to put a slipper in hand");

            var visual = carrier.GetComponent<CharacterVisual>();

            // 1 — it rides a moving, animating carrier.
            carrier.Intent.Move = new Vector2(0.0f, 1.0f);

            float worst = 0.0f;

            for (int i = 0; i < 60; i++)
            {
                yield return null;

                var anchor = visual.HandAnchor;
                if (anchor == null) continue;

                // The carry lifts the slipper off the anchor by its own rest height, so the
                // distance is never zero. It must never GROW, which is what coming off looks like.
                float lift = slipper.RestHeight * anchor.lossyScale.y;
                float slack = Vector3.Distance(slipper.transform.position, anchor.position) - lift;

                worst = Mathf.Max(worst, Mathf.Abs(slack));
            }

            carrier.Intent.Move = Vector2.zero;

            Assert.Less(worst, 0.05f,
                $"a held slipper drifted {worst:0.000} m from the hand while its carrier walked. " +
                "The carry has to run in LateUpdate: Unity evaluates the Animator between Update " +
                "and LateUpdate, so a bone read in Update is the PREVIOUS frame's pose and the " +
                "slipper trails the hand by one frame of animation.");

            // 2 — it survives the anchor going away.
            Object.DestroyImmediate(visual.HandAnchor.gameObject);

            yield return null;

            Vector3 body = carrier.transform.position;
            float reach = Vector3.Distance(slipper.transform.position, body);

            Assert.Less(reach, 2.0f,
                $"with no hand anchor the slipper sat {reach:0.00} m from its carrier, so it was " +
                "abandoned rather than falling back to the body. See Carrier.CarryAnchor.");

            Vector3 before = slipper.transform.position;
            carrier.Teleport(body + new Vector3(3.0f, 0.0f, 0.0f));

            for (int i = 0; i < 8; i++) yield return null;

            Assert.Greater(Vector3.Distance(before, slipper.transform.position), 1.0f,
                "the carrier moved 3 m and the slipper stayed put, which is exactly the reported " +
                "\"the slippers just float when you hold it, its completely unattached to person\".");
        }

        /// <summary>
        /// The first-person half: the local player has a tsinelas in their OWN hands.
        ///
        /// ⚠️ A SECOND OBJECT, NOT THE WORLD ONE. The world slipper sits in the real hand, which
        /// in first person is hidden and below the frustum entirely; moving the visible hand onto
        /// the world slipper instead is what made every other player see a tsinelas hovering
        /// beside its carrier's head. Two views, two objects.
        /// </summary>
        [UnityTest]
        public IEnumerator TheViewmodelCarriesItsOwnSlipperInFirstPerson()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            for (int i = 0; i < 20; i++) yield return null;

            var rig = Object.FindFirstObjectByType<CameraSystem.CameraRig>();
            Assert.IsNotNull(rig, "no camera rig in the arena");

            var arms = rig.GetComponentInChildren<CameraSystem.ViewmodelArms>(true);
            Assert.IsNotNull(arms, "the rig built no viewmodel arms");

            Transform held = null;

            foreach (var t in arms.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "HeldSlipper") continue;
                held = t;
                break;
            }

            Assert.IsNotNull(held,
                "the viewmodel has no HeldSlipper node, so the local player holds nothing " +
                "visible in first person however well the world object is attached.");

            Assert.IsFalse(held.gameObject.activeSelf,
                "the viewmodel slipper is showing before anything was picked up");

            var mine = rig.Following;
            Assert.IsNotNull(mine, "the rig is following no character");

            Slipper loose = null;

            foreach (var s in Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
            {
                if (s.State != SlipperState.Loose) continue;
                loose = s;
                break;
            }

            Assert.IsNotNull(loose, "no loose slipper in the arena");

            mine.RoundActive = true;
            loose.transform.position = mine.transform.position;

            yield return new WaitForFixedUpdate();

            Assert.IsTrue(loose.HostGrab(mine), "the harness failed to put a slipper in hand");

            // The rig writes the viewmodel in LateUpdate, so give it a whole frame.
            for (int i = 0; i < 3; i++) yield return null;

            Assert.IsTrue(held.gameObject.activeSelf,
                "the local player picked a slipper up and their own hands are still empty. " +
                "CameraRig.ApplyFpp calls ViewmodelArms.SetHolding; nothing else does.");

            var renderer = held.GetComponent<Renderer>();

            Assert.IsNotNull(renderer, "the viewmodel slipper has no renderer, so it draws nothing");
            Assert.IsTrue(renderer.enabled, "the viewmodel slipper's renderer is off");
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
