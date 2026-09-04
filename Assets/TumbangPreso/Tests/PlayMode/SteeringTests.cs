using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// W goes where the player is looking.
    ///
    /// ⚠️⚠️ THIS IS THE REGRESSION TEST FOR "controls are inverted and most dont work", WHICH
    /// WAS TWO FAULTS WEARING ONE HAT.
    ///
    ///  1. Nothing in the project ever called `CameraRig.SetAimSource`. It defaults to MOVEMENT,
    ///     `StepLook` returns on its first line unless it is MOUSE, and there was no call site
    ///     at all: the mouse turned nothing and the body never yawed for the whole match.
    ///  2. `CharacterMotor` then steered in WORLD space for every unit. `character_base.gd:912`
    ///     is `transform.basis * Vector3(x, 0, y)` for a mouse-aimed unit and a bare world
    ///     vector only for one that steers by movement, so W walked the player along a fixed
    ///     compass heading whichever way they were facing. Exactly one of the four cardinal
    ///     headings behaved, which is why it reads as "inverted" rather than as "not wired".
    ///
    /// Both are asserted from the OUTSIDE, on where the body actually ends up, because that is
    /// the thing the player was reporting.
    /// </summary>
    public class SteeringTests
    {
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES THIS SUITE'S RESULT MEAN SOMETHING IN A FULL RUN.
        /// `docs/TODO.md` § 126.8: this fixture is one of the five named by stack trace in two
        /// full PlayMode runs that came back 42 red and 41 red **with eleven suites swapping
        /// sides**, and it had no teardown of any kind. `PlayModeWorld.Reset` has the mechanism
        /// and why both hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator SetUpWorld() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator TearDownWorld() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator TagSelectionServesEveryEligibleSeatBeforeRepeating()
        {
            GameServices.Ensure();
            GameServices.Round.Clear();

            var lataGo = new GameObject("FairTargetLata");
            GameServices.Round.Lata = lataGo.AddComponent<Lata>();

            var defenderGo = new GameObject("FairTargetDefender", typeof(CharacterController));
            var defender = defenderGo.AddComponent<CharacterMotor>();
            defender.PlayerSlot = 0;
            defender.IsDefender = true;
            defenderGo.AddComponent<Carrier>();
            var brain = defenderGo.AddComponent<AIController>();
            GameServices.Round.Register(defender);

            var attackers = new List<CharacterMotor>();
            for (int slot = 1; slot < Balance.PlayerCount; slot++)
            {
                var go = new GameObject($"FairTargetSeat{slot}", typeof(CharacterController));
                var motor = go.AddComponent<CharacterMotor>();
                motor.PlayerSlot = slot;
                motor.IsDefender = false;
                motor.IsBot = slot == 2;
                motor.HoldingSlipper = true;
                attackers.Add(motor);
                GameServices.Round.Register(motor);
            }

            GameServices.Round.BeginRound();
            yield return null;

            MethodInfo select = typeof(AIController).GetMethod(
                "TagTarget", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo focusUntil = typeof(AIController).GetField(
                "_tagFocusUntil", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(select);
            Assert.IsNotNull(focusUntil);

            var counts = new int[Balance.PlayerCount];
            for (int pick = 0; pick < 6; pick++)
            {
                focusUntil.SetValue(brain, 0.0f);
                var target = (CharacterMotor)select.Invoke(brain, null);
                Assert.IsNotNull(target);
                counts[target.PlayerSlot]++;
            }

            Assert.AreEqual(2, counts[1]);
            Assert.AreEqual(2, counts[2]);
            Assert.AreEqual(2, counts[3]);

            GameServices.Round.Clear();
            foreach (var attacker in attackers) Object.DestroyImmediate(attacker.gameObject);
            Object.DestroyImmediate(defenderGo);
            Object.DestroyImmediate(lataGo);
        }

        [UnityTest]
        public IEnumerator TheArenaGivesTheLocalSeatMouseAim()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 20; i++) yield return null;

            var rig = Object.FindFirstObjectByType<CameraRig>();
            Assert.IsNotNull(rig, "The arena built no camera rig.");

            Assert.AreEqual(AimSource.Mouse, rig.Aim,
                "The local rig is on MOVEMENT aim, so the mouse steers nothing and the body " +
                "never yaws. The match is unplayable and nothing says so.");
        }

        /// <summary>
        /// ⚠️ A BARE SEAT, NOT THE ARENA'S. The ready gate parks input until R is pressed, and a
        /// test that fought that would be asserting the gate rather than the steering. What is
        /// under test is one line of `CharacterMotor.Steer`.
        ///
        /// ⚠️⚠️ THE SEAT STANDS ON THE FLOOR NOW RATHER THAN INSIDE IT, AND THAT IS THE WHOLE OF
        /// `docs/TODO.md` § 130.14. This test was red, deterministically, to eight significant
        /// figures, on `550ba0f` and on every commit since, and two sessions measured it as
        /// pre-existing and left it. **The steering was never wrong.**
        /// `TheSteeringFrameByFrameIsWrittenOut` below prints the answer in one table:
        ///
        ///     frame  mouseAimed  yaw       pos
        ///         0        True    90.000  (0.0506, -0.0080, 1.0800)
        ///         1        True    90.000  (0.0506,  1.0800, 1.0800)
        ///        ...
        ///        39        True    90.000  (1.9734,  1.0800, 1.0800)
        ///
        /// **`mouseAimed` is true on every frame, the yaw is exactly 90.000 on every frame, and
        /// x advances by exactly 0.0506 m per step, due east, forty times.** The steering is
        /// perfect. The entire 1.08 m of northward drift the assertion was reporting happens on
        /// **frame 0**, before a single step of movement, and never moves again.
        ///
        /// ⚠️⚠️ IT IS `CharacterController` DEPENETRATION, AND THE SETUP ASKED FOR IT. The
        /// controller is left at Unity's defaults here (height 2, radius 0.5, centre 0,0,0), so a
        /// body placed at **y = 0.2** has its capsule reaching down to **y = -0.8** while the
        /// floor's top face is at **y = 0**: the seat starts **0.8 m inside the ground**. The
        /// first `_cc.Move` resolves that overlap and shoves the capsule out, and the shove is
        /// not purely vertical: it lands as (0, +1.08, +1.08). The 1.08 in Z is the whole defect.
        ///
        /// ⚠️ AND THE OLD FAILURE MESSAGE WAS ARITHMETICALLY PERFECT AND POINTED AT THE WRONG
        /// FILE. `moved.x` came back **1.97339928** against a bound of `magnitude * 0.9` =
        /// **2.0246408**, and `Balance.Speed * AttackerSpeedScale * 40 * (1/50)` is **2.0240**:
        /// the number the test wanted was the honest distance and the number it measured was that
        /// distance minus the one step the depenetration ate. **A red whose two numbers are both
        /// correct is a red about the setup.**
        ///
        /// ⚠️⚠️ THE GAME ITSELF NEVER MEETS THIS, WHICH IS WHY NO PLAYER EVER REPORTED IT.
        /// `CharacterMotor.Teleport` disables the controller, writes the position, re-enables it
        /// and sets `_spawnSettle` to `Balance.SpawnSettleFrames`, and `FixedUpdate` then PINS the
        /// body there for those frames before any movement runs. Every spawn in the game goes
        /// through it. This test built a seat by hand and skipped all of it, which is legitimate
        /// (see the note above about the ready gate) as long as the body starts somewhere legal.
        ///
        /// ⚠️ **THIS IS NOT A WIDENED BOUND**, which § 130.14 forbids in as many words: the bound
        /// is untouched, the assertion is untouched, and what changed is that the seat is stood on
        /// the ground instead of buried in it. `Settle` below is the shared fix and
        /// `AMovementAimedSeatTurnsToFaceItsDirection` was carrying the same latent fault while
        /// passing, because it only ever asserted a FACING.
        /// </summary>
        [UnityTest]
        public IEnumerator MouseAimedMovementIsRelativeToTheBody()
        {
            var floor = BuildFloor();

            var go = new GameObject("Seat", typeof(CharacterController));
            go.transform.position = StandingHeight;

            var motor = go.AddComponent<CharacterMotor>();

            // Face due EAST. With world-space steering, W would still walk north.
            go.transform.rotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);

            var camGo = new GameObject("Rig", typeof(UnityEngine.Camera));
            var rig = camGo.AddComponent<CameraRig>();
            rig.Follow(motor);
            rig.SetAimSource(AimSource.Mouse);

            yield return Settle(motor);

            motor.Intent.Move = new Vector2(0.0f, 1.0f);      // W
            motor.Intent.CommitFrame();

            Vector3 from = go.transform.position;

            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();

            Vector3 moved = go.transform.position - from;
            moved.y = 0.0f;

            Assert.Greater(moved.magnitude, 0.5f, "The seat did not move at all.");

            Assert.Greater(moved.x, moved.magnitude * 0.9f,
                $"Facing east, W moved the seat {moved} instead of along its own forward. " +
                "Movement is being steered in world space rather than relative to the body.");

            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(floor);
        }

        /// <summary>
        /// Where a default `CharacterController` has to stand so its capsule is not inside the
        /// floor this file builds.
        ///
        /// ⚠️ THE NUMBER IS THE CAPSULE, NOT A GUESS. Unity's default controller is 2 m tall with
        /// its centre on the transform, so the capsule reaches 1 m below the object; the floor
        /// below is a unit cube scaled to 60 x 1 x 60 sitting at y = -0.5, so its top face is at
        /// exactly y = 0. **1.05 is one metre of capsule plus five centimetres of daylight**, and
        /// the daylight is what lets gravity, rather than depenetration, put the body down.
        /// </summary>
        private static readonly Vector3 StandingHeight = new Vector3(0.0f, 1.05f, 0.0f);

        private static GameObject BuildFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.localScale = new Vector3(60.0f, 1.0f, 60.0f);
            floor.transform.position = new Vector3(0.0f, -0.5f, 0.0f);
            return floor;
        }

        /// <summary>
        /// Let the seat come to rest before anything is measured from it.
        ///
        /// ⚠️⚠️ THE POINT IS THAT THE FIRST MEASURED FRAME IS NOT THE FIRST PHYSICS FRAME.
        /// Even from five centimetres up there is a landing, and a landing is a vertical velocity
        /// being zeroed and a `CollisionFlags.Below` arriving. Sampling the start position before
        /// that has happened puts a transient in the middle of a measurement about steering.
        /// `CharacterMotor.Teleport` does the same job in the game with `_spawnSettle`.
        ///
        /// ⚠️ IT ASSERTS THE SEAT IS ACTUALLY DOWN rather than trusting a frame count, so a
        /// change to gravity or to the controller's size fails here, where the reason is written,
        /// instead of one assertion further on where it is not.
        /// </summary>
        private static IEnumerator Settle(CharacterMotor motor)
        {
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();

            Assert.Less(Mathf.Abs(motor.transform.position.x), 0.01f,
                "the seat drifted sideways while settling, so it is not standing on the floor " +
                "this test built. See docs/TODO.md § 130.14.");

            Assert.Less(Mathf.Abs(motor.transform.position.z), 0.01f,
                "the seat drifted sideways while settling, so it is not standing on the floor " +
                "this test built. See docs/TODO.md § 130.14.");
        }

        /// <summary>
        /// ⚠️⚠️ THE DIAGNOSIS FOR `docs/TODO.md` § 130.14, WHICH IS A TEST THAT PRINTS RATHER
        /// THAN ONE THAT ASSERTS. That entry measured the red as pre-existing and deterministic
        /// to eight significant figures and then stopped, and its own "done looks like" is
        /// **the cause named in `CharacterMotor.Steer` or `CameraRig`, not a widened bound**.
        /// Naming a cause needs the per-frame picture, and the failing assertion only reports
        /// where the body finished.
        ///
        /// It writes `Logs/steering-frames.txt`: one line per fixed update, carrying the frame,
        /// whether the motor considered itself mouse-aimed, the body's yaw, and the position.
        /// **A run where the yaw starts at anything other than 90, or where `mouseAimed` is
        /// false on any frame, is the answer**; a run where both hold for all 40 frames means the
        /// fault is in the movement rather than in the steering and `CharacterController`
        /// depenetration is the next thing to look at.
        /// </summary>
        [UnityTest]
        public IEnumerator TheSteeringFrameByFrameIsWrittenOut()
        {
            var floor = BuildFloor();

            var go = new GameObject("Seat", typeof(CharacterController));
            go.transform.position = StandingHeight;

            var motor = go.AddComponent<CharacterMotor>();
            go.transform.rotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);

            var camGo = new GameObject("Rig", typeof(UnityEngine.Camera));
            var rig = camGo.AddComponent<CameraRig>();
            rig.Follow(motor);
            rig.SetAimSource(AimSource.Mouse);

            yield return Settle(motor);

            motor.Intent.Move = new Vector2(0.0f, 1.0f);
            motor.Intent.CommitFrame();

            // ⚠️ THE MOTOR'S OWN ANSWER, NOT A RESTATEMENT OF IT. `MouseAimed` is private and it
            // is the branch under test; asking the rig instead would be asserting that the test
            // and the motor agree, which is the thing in doubt.
            var mouseAimed = typeof(CharacterMotor).GetProperty(
                "MouseAimed", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mouseAimed, "CharacterMotor.MouseAimed is gone or renamed");

            var lines = new List<string>
            {
                "frame  mouseAimed  yaw       pos",
            };

            Vector3 from = go.transform.position;

            for (int i = 0; i < 40; i++)
            {
                yield return new WaitForFixedUpdate();

                bool aimed = (bool)mouseAimed.GetValue(motor);
                float yaw = go.transform.eulerAngles.y;
                Vector3 p = go.transform.position;

                lines.Add($"{i,5}  {aimed,10}  {yaw,8:F3}  ({p.x:F4}, {p.y:F4}, {p.z:F4})");
            }

            Vector3 moved = go.transform.position - from;
            moved.y = 0.0f;

            lines.Add("");
            lines.Add($"moved {moved}  magnitude {moved.magnitude:F6}  " +
                      $"bearing {Mathf.Atan2(moved.z, moved.x) * Mathf.Rad2Deg:F3} deg north of east");

            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllLines("Logs/steering-frames.txt", lines);

            foreach (string line in lines) Debug.Log("[Steering] " + line);

            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(floor);
        }

        /// <summary>
        /// ⚠️ AND A BOT TURNS TO FACE WHERE IT IS GOING. `character_base.gd:926` calls `look_at`
        /// on the movement vector for a unit that is not mouse-aimed, and every verb it can use
        /// fires along `-basis.z`. Without it a bot slides sideways while its punch, its lunge
        /// and its shove all leave along a forward vector that never moved.
        /// </summary>
        [UnityTest]
        public IEnumerator AMovementAimedSeatTurnsToFaceItsDirection()
        {
            var floor = BuildFloor();

            var go = new GameObject("Bot", typeof(CharacterController));

            // ⚠️ STANDING, NOT BURIED. This spawned at y = 0.2 with a capsule reaching to -0.8,
            // which is 0.8 m inside the floor, and the first physics step then threw the body a
            // metre sideways. It passed anyway because it only asserts a FACING, so the fault sat
            // here undetected while its sibling above was red for it every run.
            // `docs/TODO.md` § 130.14.
            go.transform.position = StandingHeight;

            var motor = go.AddComponent<CharacterMotor>();

            yield return Settle(motor);

            motor.Intent.Move = new Vector2(1.0f, 0.0f);      // due east
            motor.Intent.CommitFrame();

            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();

            Assert.Greater(Vector3.Dot(go.transform.forward, Vector3.right), 0.9f,
                $"A movement-aimed seat walking east is facing {go.transform.forward}.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(floor);
        }

        [UnityTest]
        public IEnumerator APlantedBotTurnsTowardItsAimWithoutSliding()
        {
            var go = new GameObject("AimingBot", typeof(CharacterController));
            // No floor on purpose. The assertion is about horizontal intent; a physics contact
            // would add CharacterController depenetration to the thing being measured.
            go.transform.position = new Vector3(0.0f, 3.0f, 0.0f);
            go.transform.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
            var motor = go.AddComponent<CharacterMotor>();

            Vector3 planted = go.transform.position;
            motor.Intent.Move = Vector2.zero;
            motor.Intent.AimPoint = Vector3.forward * 10.0f;
            motor.Intent.FaceAimPoint = true;
            motor.Intent.CommitFrame();

            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();

            Vector3 drift = go.transform.position - planted;
            drift.y = 0.0f;
            Assert.Less(drift.magnitude, 0.05f,
                "turning to aim made the planted thrower slide");
            Assert.Greater(Vector3.Dot(go.transform.forward, Vector3.forward), 0.97f,
                $"the planted thrower still faces {go.transform.forward} instead of its aim");

            Object.DestroyImmediate(go);
        }
    }
}
