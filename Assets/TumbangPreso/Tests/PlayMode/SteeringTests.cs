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
        /// ✅ AND THE FIX ABOVE WAS THREE CENTIMETRES SHORT, WHICH IS § 143.20. Raising the
        /// seat to y = 1.05 left the capsule's SKIN inside the floor and the same ejection
        /// happened at a smaller depth: 1.079 m, in Z, on frame 0, on every run. `StandUp`
        /// derives the height from the controller now and carries the whole measurement.
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
            StandUp(go);

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
        /// Five centimetres of air under the capsule, so gravity rather than depenetration is
        /// what puts the body down.
        ///
        /// ⚠️ IT IS DAYLIGHT AND NOT A STANDING HEIGHT. The height itself is READ off the
        /// controller by `StandUp` below rather than written here; see that method for why a
        /// literal was the whole of § 143.20.
        /// </summary>
        private const float Daylight = 0.05f;

        /// <summary>
        /// Stand a seat on the floor `BuildFloor` makes, from the controller's own dimensions.
        ///
        /// ⚠️⚠️ THIS REPLACED A LITERAL `1.05f` AND THE LITERAL WAS `docs/TODO.md` § 143.20 IN
        /// FULL. § 130.14b raised the seat out of the floor and wrote the arithmetic down as
        /// *"one metre of capsule plus five centimetres of daylight"*. **`skinWidth` is not in
        /// that sentence and it is 0.08 on Unity's default controller**, so the capsule plus its
        /// skin reached to y = 1.05 - 1.0 - 0.08 = **-0.03**: the seat was still three
        /// centimetres inside the ground, and the fix that was supposed to end the burial only
        /// made it shallower.
        ///
        /// ⚠️⚠️ AND THE 1.079 m THE ASSERTION KEPT REPORTING IS `height / 2 + skinWidth`, WHICH
        /// IS WHY IT WAS THE SAME NUMBER EVERY RUN. `Settle`'s per-frame trace has it in two
        /// lines: the body is written to (0, 0.0000, 1.0800) on frame 0 and to
        /// (0, 1.0800, 1.0800) on frame 1, from a start of (0, 1.05, 0). A three-centimetre
        /// overlap is shallow enough that the controller can eject along the horizontal axis
        /// instead of the vertical one, and when it does it ejects a whole capsule-half. **One
        /// frame, and the same magnitude in Z as the resting height in Y**: that pairing is the
        /// signature, and it is what the measurement was asked for.
        ///
        /// ⚠️ THE HEIGHT IS DERIVED HERE RATHER THAN STATED, BECAUSE A STATED ONE HAS NOW GONE
        /// WRONG TWICE. `CLAUDE.md` § 4a's argument applied to a test: a number that has to be
        /// kept in step with `CharacterController`'s defaults is a number somebody will forget,
        /// and both times it was forgotten it read as a steering bug. Change the controller's
        /// height, radius or skin and this follows on its own.
        /// </summary>
        private static void StandUp(GameObject go)
        {
            var cc = go.GetComponent<CharacterController>();
            float bottom = cc.center.y - cc.height * 0.5f - cc.skinWidth;
            go.transform.position = new Vector3(0.0f, -bottom + Daylight, 0.0f);

            // ⚠️ THE SEAT IS A COLLIDER TOO. `BuildFloor` explains why a transform write is not
            // a physics-world write; a capsule placed and not synced is the same fault standing
            // on the other foot.
            Physics.SyncTransforms();
        }

        /// <summary>
        /// A 60 x 1 x 60 slab whose top face is exactly y = 0.
        ///
        /// ⚠️⚠️ THE `SyncTransforms` CALL IS THE WHOLE OF `docs/TODO.md` § 143.20 AND IT IS NOT
        /// A TIDY-UP. `GameObject.CreatePrimitive` registers a **1 x 1 x 1 box collider at the
        /// world origin** with the physics engine, and the two transform writes below are
        /// Transform writes: with `Physics.autoSyncTransforms` off, which is Unity's default,
        /// the physics world still holds the unscaled box at the origin until something syncs
        /// it. The seat is then built five centimetres over a floor the collider world does not
        /// have yet, standing INSIDE a one-metre cube that it does.
        ///
        /// ⚠️⚠️ AND THE 1.079 m IS THAT CUBE'S ARITHMETIC, WHICH IS WHY IT WAS THE SAME NUMBER
        /// EVERY RUN AND WHY RAISING THE SEAT DID NOT MOVE IT. Ejecting a capsule sideways out
        /// of a unit cube is **half the cube plus the capsule radius plus the skin**:
        /// 0.5 + 0.5 + 0.08 = **1.08**. Two sessions read that number as a steering bug and a
        /// third as a burial depth; it is neither, and it is not sensitive to the seat's height
        /// at all, which is the fact that should have ruled both readings out.
        ///
        /// ⚠️ THE TRACE THAT NAMED IT is `Settle`'s: `overlapping Cube [BoxCollider] at
        /// (0.00, 0.00, 0.00) extents (0.50, 0.50, 0.50)`, printed while this method had
        /// already scaled that same cube to 60 x 1 x 60 and moved it to y = -0.5.
        /// </summary>
        private static GameObject BuildFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.localScale = new Vector3(60.0f, 1.0f, 60.0f);
            floor.transform.position = new Vector3(0.0f, -0.5f, 0.0f);

            // ⚠️ THE COLLIDER MOVES WHEN THIS IS CALLED, NOT WHEN THE TRANSFORM IS WRITTEN.
            Physics.SyncTransforms();

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
            var cc = motor.GetComponent<CharacterController>();

            // ⚠️⚠️ THE FRAME NUMBER IS THE FINDING, NOT THE FINAL POSITION, WHICH IS WHY THIS
            // RECORDS BEFORE IT ASSERTS. § 143.20's own next step is written as a measurement:
            // "log the position every fixed update through Settle and find the frame it jumps
            // on. A single frame means a teleport or a clamp; a ramp means a force." The
            // assertion below only ever reported where the body FINISHED, and three cases
            // reporting the same finishing number every run is exactly as consistent with a
            // one-frame shove as with twenty frames of drift.
            // ⚠️⚠️ WHAT ELSE IS IN THE WORLD IS PART OF THE MEASUREMENT, BECAUSE THE MOTOR'S
            // OWN NUMBERS RULED THE MOTOR OUT. At frame 0 `_spawnSettle` is 0 (so nothing is
            // pinning the body), `_velocity` is (0, -0.40, 0) and `dt` is 0.02, so the motion
            // this file ASKED for is eight millimetres downward. The body moved 1.13 m down and
            // 1.08 m sideways in that same step. A `CharacterController.Move` that travels a
            // hundred times its own motion vector is resolving an overlap, and the only way an
            // overlap exists five centimetres above this test's floor is another collider.
            var scenes = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sc = SceneManager.GetSceneAt(i);
                scenes.Add($"{sc.name}({sc.rootCount} roots{(sc.isLoaded ? "" : ", not loaded")})");
            }

            var overlaps = Physics.OverlapCapsule(
                motor.transform.position + Vector3.up * (cc.height * 0.5f - cc.radius),
                motor.transform.position - Vector3.up * (cc.height * 0.5f - cc.radius),
                cc.radius + cc.skinWidth);

            var touching = new List<string>();
            foreach (var col in overlaps)
            {
                if (col.gameObject == motor.gameObject) continue;
                touching.Add($"{col.name} [{col.GetType().Name}] at {col.bounds.center} " +
                             $"extents {col.bounds.extents}");
            }

            var rows = new List<string>
            {
                $"controller  height {cc.height:F3}  radius {cc.radius:F3}  " +
                $"centre {cc.center}  skin {cc.skinWidth:F4}  step {cc.stepOffset:F3}",
                $"start       {motor.transform.position}  roundActive {motor.RoundActive}",
                $"scenes      {string.Join(", ", scenes)}",
                $"overlapping {(touching.Count == 0 ? "nothing" : string.Join(" | ", touching))}",
                "frame  grounded  flags            pos                              step",
            };

            Vector3 previous = motor.transform.position;

            // ⚠️⚠️ THE PRIVATE STATE IS IN THE TRACE BECAUSE THE PUBLIC ONE ALREADY RULED THE
            // OBVIOUS ANSWER OUT. Standing the seat a clear five centimetres over the floor
            // (start y = 1.13, capsule bottom + skin at 0.05) STILL landed it on
            // (0, 0.0000, 1.0800) on frame 0, and a body that falls 1.13 m in one fixed step
            // has not fallen: something wrote its position. `_spawnSettle` pins a body to
            // `_spawnSettleAt` for `Balance.SpawnSettleFrames`, and both are private, so a
            // trace without them cannot tell a pin from a shove.
            var settleField = typeof(CharacterMotor).GetField(
                "_spawnSettle", BindingFlags.Instance | BindingFlags.NonPublic);
            var settleAtField = typeof(CharacterMotor).GetField(
                "_spawnSettleAt", BindingFlags.Instance | BindingFlags.NonPublic);
            var velocityField = typeof(CharacterMotor).GetField(
                "_velocity", BindingFlags.Instance | BindingFlags.NonPublic);

            for (int i = 0; i < 20; i++)
            {
                yield return new WaitForFixedUpdate();

                Vector3 p = motor.transform.position;
                Vector3 step = p - previous;
                previous = p;

                string settle = settleField != null ? settleField.GetValue(motor).ToString() : "?";
                string settleAt = settleAtField != null ? settleAtField.GetValue(motor).ToString() : "?";
                string velocity = velocityField != null ? velocityField.GetValue(motor).ToString() : "?";

                rows.Add($"{i,5}  {cc.isGrounded,8}  {cc.collisionFlags,-15}  " +
                         $"({p.x:F4}, {p.y:F4}, {p.z:F4})  " +
                         $"({step.x:F4}, {step.y:F4}, {step.z:F4})  " +
                         $"settle {settle} at {settleAt}  vel {velocity}");
            }

            // ⚠️ ONE FILE PER CASE. Three cases call this and a shared filename would leave
            // whichever ran last standing in for all three, which is the same "one copy that
            // goes stale" fault `CLAUDE.md` § 5 records about documents.
            string name = TestContext.CurrentContext.Test.Name;
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllLines($"Logs/settle-{name}.txt", rows);

            foreach (string row in rows) Debug.Log("[Settle] " + row);

            // ⚠️ THE TRACE RIDES THE FAILURE MESSAGE AS WELL AS THE FILE. A run on somebody
            // else's machine, or through the grouped gate, hands back the .xml and not the
            // Logs folder, and a measurement nobody can read is not a measurement.
            string trace = string.Join(System.Environment.NewLine, rows);

            Assert.Less(Mathf.Abs(motor.transform.position.x), 0.01f,
                "the seat drifted sideways while settling, so it is not standing on the floor " +
                "this test built. See docs/TODO.md § 130.14 and § 143.20." + System.Environment.NewLine + trace);

            Assert.Less(Mathf.Abs(motor.transform.position.z), 0.01f,
                "the seat drifted sideways while settling, so it is not standing on the floor " +
                "this test built. See docs/TODO.md § 130.14 and § 143.20." + System.Environment.NewLine + trace);
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
            StandUp(go);

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
            // `docs/TODO.md` § 130.14, and then § 143.20 when the raised number turned out to
            // still be three centimetres short of the skin.
            StandUp(go);

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
