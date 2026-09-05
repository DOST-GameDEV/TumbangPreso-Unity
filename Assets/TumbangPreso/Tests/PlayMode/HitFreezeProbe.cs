using System.Collections;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// ⚠️⚠️ THE HIT FREEZE HAS TO BE A DEAD STOP WITH A PUNCH IN IT, AND IT WAS A RANDOM WALK.
    ///
    /// `CameraRig.StepShake` writes `transform.position += ...`. On a normal frame that is
    /// correct and deliberate: `ApplyFpp`/`ApplyTpp` have already written the position
    /// ABSOLUTELY from the followed body, so the offset is discarded and re-derived every frame
    /// and can never exceed its own amplitude. **A held frame is the one frame that skips that
    /// write**, by design, because suspending the follow is what a hitstop IS here
    /// (`CameraRig.HoldFrame`: *"`LateUpdate` stops WRITING the camera transform for a few
    /// frames"*). So during a hold the same `+=` had no baseline to be relative to and
    /// compounded, once per frame, until the follow resumed and snapped the view back.
    ///
    /// **MEASURED ON THE SHIPPED WEIGHTS**, `HitFeel.Weight.Ultimate` (a 0.11 s hold and a
    /// 0.20 x 1.40 = 0.28 m punch offset decaying over 0.16 s):
    ///
    /// | | Frames inside the hold | Sum of `punchRatio` | Accumulated drift |
    /// |---|---|---|---|
    /// | 60 Hz | 7 | 4.08 | **1.14 m** |
    /// | 144 Hz | 16 | 10.1 | **2.83 m** |
    ///
    /// ⚠️⚠️ **IT GOT WORSE ON A BETTER MACHINE, WHICH IS THE TELL.** A hold is a DURATION, so a
    /// shorter frame buys more frames and every frame is another addend. A metre of camera travel
    /// in a 14 m box (`docs/VISION.md` § 2) during the one beat that exists to read as impact,
    /// and the faster the machine the further it walked.
    ///
    /// ⚠️ **THE BAND IS THE TEST, NOT A CEILING.** Pinning only the top would be passed by a
    /// "fix" that assigns the anchor AFTER `StepShake` and erases the punch entirely, which is a
    /// hitstop with no impact in it. The floor is what stops that, so both directions are held by
    /// one measurement.
    /// </summary>
    public class HitFreezeProbe
    {
        /// <summary>
        /// The pair that makes a full-suite result mean anything. `docs/TODO.md` § 126.8 and
        /// `PlayModeWorld.Reset`.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        /// <summary>
        /// The punch's own reach is 0.28 m and the shake adds at most 0.014 m per axis, so a
        /// correct hold cannot leave the anchor by much more than 0.30 m however long it lasts
        /// and however fast the machine is. 0.45 m is that with room for the body settling under
        /// gravity during the freeze; it is nowhere near the 1.14 m the accumulator reached at
        /// the slowest frame rate anybody plays at.
        /// </summary>
        private const float DriftCeiling = 0.45f;

        /// <summary>
        /// A hold that moved the camera less than this delivered no punch at all.
        /// </summary>
        private const float PunchFloor = 0.10f;

        [UnityTest]
        public IEnumerator TheHitFreezeHoldsItsAnchorInsteadOfWalkingAwayFromIt()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 20; i++) yield return null;

            GameServices.Round.BeginRound();
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

            var rig = Object.FindFirstObjectByType<CameraRig>();
            Assert.IsNotNull(rig, "no CameraRig found in scene");

            // The taya is the seat standing still at the top of a round; every attacker is
            // holding a slipper and heading somewhere. A body that walks during the freeze would
            // move the follow target and make the drift measurement below dishonest, which is
            // why the settle guard is asserted rather than assumed.
            CharacterMotor motor = null;
            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
            {
                if (m.IsDefender) { motor = m; break; }
            }
            Assert.IsNotNull(motor, "no defender seat found");

            var ai = motor.GetComponent<AIController>();
            if (ai != null) ai.enabled = false;
            motor.Intent.Clear();

            rig.Follow(motor);

            for (int i = 0; i < 10; i++) yield return null;

            Vector3 cameraAnchor = rig.transform.position;
            Vector3 bodyAnchor = motor.transform.position;

            // The real call the game makes, from inside an ultimate's `OnActivate`. Going through
            // `HitFeel` rather than `HoldFrame` directly is deliberate: the weights, the hold
            // length and the punch strength are ITS numbers, and a probe that passed its own
            // would stop measuring the game the moment somebody retuned them.
            Visual.HitFeel.Land(motor, Visual.HitFeel.Weight.Ultimate, Color.white,
                                motor.transform.position + motor.transform.forward * 2.0f);

            float maxDrift = 0.0f;
            float watched = 0.0f;

            // Past the 0.11 s hold and past the 0.16 s punch, so the release and the snap it used
            // to produce are both inside the window rather than after it.
            while (watched < 0.25f)
            {
                yield return null;
                watched += Time.unscaledDeltaTime;
                maxDrift = Mathf.Max(maxDrift, Vector3.Distance(rig.transform.position, cameraAnchor));
            }

            float bodyMoved = Vector3.Distance(motor.transform.position, bodyAnchor);

            Assert.Less(bodyMoved, 0.15f,
                $"the followed body moved {bodyMoved:F3} m during the freeze, so the camera drift " +
                "measured below is not attributable to the hold. Re-run with the seat parked.");

            Assert.Greater(maxDrift, PunchFloor,
                $"the hit freeze moved the camera only {maxDrift:F3} m, which is less than the " +
                $"{PunchFloor:F2} m floor: the impact punch is not reaching the view at all. A hold " +
                "that restores its anchor AFTER the shake instead of before it looks exactly like " +
                "this, and is a hitstop with no hit in it.");

            Assert.Less(maxDrift, DriftCeiling,
                $"the hit freeze walked the camera {maxDrift:F3} m from where it froze, against a " +
                $"{DriftCeiling:F2} m ceiling. The punch's own reach is 0.28 m, so anything past " +
                "this is the offset compounding across held frames rather than being re-derived " +
                "from a baseline: `StepHold` skips the absolute write that `StepShake`'s `+=` " +
                "depends on. It was 1.14 m at 60 Hz and 2.83 m at 144 Hz before the anchor landed.");
        }

        /// <summary>
        /// ⚠️ THE SEAT CHANGE IS THE OTHER HALF OF THE ANCHOR, AND IT IS A BUG THE ANCHOR COULD
        /// HAVE INTRODUCED. A hold still running when the rig is pointed at a different body
        /// would pin the new seat's view to the OLD body's frozen pose for the rest of the
        /// freeze, which is a camera stuck several metres away looking at nothing. `Follow`
        /// clears the hold for the same reason it clears `_fallView`.
        /// </summary>
        [UnityTest]
        public IEnumerator AFreezeDoesNotSurviveOntoTheNextSeat()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 20; i++) yield return null;

            GameServices.Round.BeginRound();
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

            var rig = Object.FindFirstObjectByType<CameraRig>();
            Assert.IsNotNull(rig, "no CameraRig found in scene");

            CharacterMotor first = null, second = null;
            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
            {
                var ai = m.GetComponent<AIController>();
                if (ai != null) ai.enabled = false;
                m.Intent.Clear();

                if (first == null) first = m;
                else if (second == null) second = m;
            }

            Assert.IsNotNull(first, "no seats in the arena");
            Assert.IsNotNull(second, "only one seat in the arena; this needs two to change between");

            rig.Follow(first);
            for (int i = 0; i < 10; i++) yield return null;

            // A freeze long enough that it is unambiguously still running when the seat changes.
            rig.HoldFrame(0.5f);
            yield return null;

            rig.Follow(second);
            for (int i = 0; i < 10; i++) yield return null;

            float toNewSeat = Vector3.Distance(rig.transform.position, second.transform.position);
            float toOldSeat = Vector3.Distance(rig.transform.position, first.transform.position);

            Assert.Less(toNewSeat, toOldSeat,
                $"after following a new seat the camera is {toNewSeat:F2} m from it and " +
                $"{toOldSeat:F2} m from the seat it left, so a hold from the previous body is " +
                "still pinning the view. `CameraRig.Follow` must clear `_holdLeft`.");
        }
    }
}
