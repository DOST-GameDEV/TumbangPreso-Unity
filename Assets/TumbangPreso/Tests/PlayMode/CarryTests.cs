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
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code. `PlayModeWorld.Reset` has the
        /// mechanism and why BOTH hooks are needed rather than one.
        /// </summary>
        private bool _bots, _spectator, _pinned;
        private int _soloSeat;
        private Core.CustomRules _rules;
        [UnitySetUp]
        public IEnumerator ResetWorldBefore()
        {
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _soloSeat = GameLaunch.SoloSeat;
            _pinned = UI.SceneFlow.RulesPinned; _rules = UI.SceneFlow.SelectedRules.Clone();
            yield return PlayModeWorld.Reset();
        }

        [UnityTearDown]
        public IEnumerator ResetWorldAfter()
        {
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _soloSeat;
            UI.SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) UI.SceneFlow.PinSelectedRules(_rules); else UI.SceneFlow.UnpinSelectedRules();
        }

        [DefaultExecutionOrder(10000)]
        private sealed class AfterCarryPose : MonoBehaviour
        {
            public System.Action Sample;
            private void LateUpdate() => Sample?.Invoke();
        }

        private static IEnumerator OpenPlayableCarryRound()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            Assert.IsTrue(GameServices.Round.RoundActive, "Carry checks need an actual started round.");
            foreach (var actor in GameServices.Round.Players)
            {
                actor.Intent.Clear(); actor.Intent.Parked = false;
            }
        }

        private static Slipper PlaceOwnedSlipperForPickup(CharacterMotor carrier)
        {
            var slipper = carrier.GetComponent<Carrier>().Held;
            Assert.IsNotNull(slipper, "The round must provide the attacker's actual owned slipper.");
            Assert.AreEqual(carrier.PlayerSlot, slipper.OwnerSlot);
            Assert.IsTrue(slipper.HostDisarm());
            slipper.transform.position = carrier.transform.position + Vector3.up * slipper.RestHeight;
            Physics.SyncTransforms();
            return slipper;
        }

        [UnityTest]
        public IEnumerator TheHandAnchorLandsOnTheHandAndRidesIt()
        {
            yield return OpenPlayableCarryRound();
            var visual = GameServices.Round.PlayerAt(1).GetComponent<CharacterVisual>();
            Assert.IsNotNull(visual.HandAnchor, "The selected attacker needs a resolved hand anchor.");

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

            // A carrying idle may deliberately hold the palm still. Request a
            // real emote, then sample its motion rather than compare two endpoints
            // that can coincide when a looping clip returns to its starting pose.
            var emotes = visual.GetComponent<Social.EmotePlayer>();
            Assert.IsTrue(emotes.CanEmote());
            Vector3 was = anchor.position;
            emotes.HostPlay("tpose");
            Assert.IsTrue(emotes.IsEmoting, "The motion witness must actually start.");
            float furthest = 0;
            try
            {
                for (int i = 0; i < 40; i++)
                {
                    yield return null;
                    furthest = Mathf.Max(furthest, Vector3.Distance(was, anchor.position));
                }
                Assert.Greater(furthest, .0005f,
                    "The hand anchor must track the accepted emote's animated arm.");
            }
            finally { emotes.Stop(); }

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
            yield return OpenPlayableCarryRound();
            var carrier = GameServices.Round.PlayerAt(1);
            var slipper = PlaceOwnedSlipperForPickup(carrier);
            Assert.IsTrue(slipper.HostGrab(carrier), "Pickup must be accepted in the started round.");

            var visual = carrier.GetComponent<CharacterVisual>();

            // 1 — it rides a moving, animating carrier.
            Vector3 walkedFrom = carrier.transform.position;
            carrier.Intent.Move = new Vector2(0.0f, 1.0f);

            float worst = 0, worstOrigin = 0; int samples = 0;
            var observation = carrier.gameObject.AddComponent<AfterCarryPose>();
            // Test coroutines resume before the final body/carry LateUpdates.
            // Observe their completed pose, just as the existing film capture does.
            observation.Sample = () =>
            {
                var anchor = visual.HandAnchor;
                if (anchor == null) return;
                float lift = slipper.CarrySupportExtent(anchor.up);
                Vector3 drawn = slipper.transform.position + slipper.DrawnCentreOffset;
                worst = Mathf.Max(worst, Vector3.Distance(drawn, anchor.position + anchor.up * lift));
                worstOrigin = Mathf.Max(worstOrigin, Mathf.Abs(Vector3.Distance(slipper.transform.position, anchor.position) - lift));
                samples++;
            };
            try { for (int i = 0; i < 60; i++) yield return null; }
            finally { observation.Sample = null; Object.Destroy(observation); }
            carrier.Intent.Move = Vector2.zero;
            Assert.GreaterOrEqual(samples, 50, "Measure actual completed frames, not an empty observation.");
            Assert.Greater(Vector3.Distance(walkedFrom, carrier.transform.position), .1f, "The carry witness must actually walk.");
            Assert.Less(worst, .05f,
                $"A held slipper's drawn centre drifted {worst:0.000}m after the carry update " +
                $"(origin offset {worstOrigin:0.000}m). Preserve the existing 5cm bound.");

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
            yield return OpenPlayableCarryRound();

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

            var mine = rig.Following;
            Assert.IsNotNull(mine, "The rig must follow the actual local attacker.");
            Assert.IsFalse(mine.IsDefender);
            var loose = PlaceOwnedSlipperForPickup(mine);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsFalse(held.gameObject.activeSelf, "The viewmodel must hide after the owned slipper is dropped.");
            Assert.IsTrue(loose.HostGrab(mine), "Pickup must be accepted in the started round.");

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
            yield return ProbeWait.Done(load, "scene load");

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
            // Floor seating advances in seconds; a fixed load-frame count can
            // end inside that transient at a high batch frame rate.
            yield return new WaitForSeconds(.2f);
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

        /// <summary>
        /// § THE SHOE FLOATS RIGHT ABOVE THE HAND, NOT IN IT. 🧑 2026-08-18: *"there is still a
        /// bug where tsinelas floats right above the characters hands"*.
        ///
        /// ⚠️⚠️ `Carrier.RideAnchor()` MULTIPLIED AN ALREADY-WORLD-SPACE LENGTH BY THE HAND'S
        /// SCALE A SECOND TIME. `Slipper.RestHeight` is `Renderer.bounds.extents.y`, a WORLD
        /// AABB read off the slipper's own (unscaled) transform — the same value
        /// `GroundY(p) + RestHeight` uses directly, with no scale factor, to rest a loose
        /// slipper on the ground. `RideAnchor` then wrote
        /// `hand.up * (RestHeight * hand.lossyScale.y)`, where `hand` is a descendant of the
        /// character's model root and inherits `CharacterVisual.PersonScale` (2.38) from
        /// `_instance.transform.localScale`. `hand.up` is already a world-space unit vector, so
        /// no scale conversion was needed at all — the multiply just inflated a correct
        /// world-space lift by 2.38x.
        ///
        /// Measured live off a throwaway probe before this fix: `RestHeight` 0.0714 m,
        /// `hand.lossyScale.y` 2.3800, and the held slipper sitting 0.1639 m from the anchor —
        /// against the 0.0714 m the un-scaled lift alone should have put it at. That is the
        /// shoe floating roughly 9 cm above the hand, which is exactly "right above" rather
        /// than "in" it.
        ///
        /// ⚠️ GODOT NEVER HAD THIS BUG, AND FOR A REVEALING REASON: `slipper.gd::_attach_to_hand()`
        /// re-parents the shoe onto the bone attachment, so it inherits the SAME 2.38x every
        /// other child of the rig does — and its own comment (`UNDO THE RIG'S SCALE OR THE
        /// SLIPPER COMES OUT 2.38x`) divides that scale back OUT before applying any offset.
        /// Unity's port does not reparent — `RideAnchor` copies a position every frame instead
        /// (see that function's own note on why) — so there was never an inherited scale to
        /// undo, and multiplying one in was pure invention.
        /// </summary>
        [UnityTest]
        public IEnumerator AHeldSlipperSitsOnTheHandNotFloatingAboveIt()
        {
            yield return OpenPlayableCarryRound();
            var carrier = GameServices.Round.PlayerAt(1);
            var slipper = PlaceOwnedSlipperForPickup(carrier);
            Assert.IsTrue(slipper.HostGrab(carrier), "Pickup must be accepted in the started round.");

            var visual = carrier.GetComponent<CharacterVisual>();

            for (int i = 0; i < 3; i++) yield return null;

            var anchor = visual.HandAnchor;
            Assert.IsNotNull(anchor);

            float lift = slipper.CarrySupportExtent(anchor.up);
            Vector3 drawn = slipper.transform.position + slipper.DrawnCentreOffset;
            float dist = Vector3.Distance(drawn, anchor.position + anchor.up * lift);

            // Measure the visible centre against support along the tilted palm,
            // not the mesh origin or the world-space height of an upright shoe.
            Assert.Less(dist, 0.03f,
                $"a held slipper's visible centre misses its palm support by {dist:0.000} m, with lift " +
                $"{lift:0.000} m. That gap is the shoe floating above the " +
                "hand rather than resting on it — see whether RideAnchor is scaling the lift " +
                "by the character's PersonScale a second time.");
        }
    }
}
