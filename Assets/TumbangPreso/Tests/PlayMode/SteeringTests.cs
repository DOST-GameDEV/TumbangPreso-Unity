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
        /// </summary>
        [UnityTest]
        public IEnumerator MouseAimedMovementIsRelativeToTheBody()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.localScale = new Vector3(60.0f, 1.0f, 60.0f);
            floor.transform.position = new Vector3(0.0f, -0.5f, 0.0f);

            var go = new GameObject("Seat", typeof(CharacterController));
            go.transform.position = new Vector3(0.0f, 0.2f, 0.0f);

            var motor = go.AddComponent<CharacterMotor>();

            // Face due EAST. With world-space steering, W would still walk north.
            go.transform.rotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);

            var camGo = new GameObject("Rig", typeof(UnityEngine.Camera));
            var rig = camGo.AddComponent<CameraRig>();
            rig.Follow(motor);
            rig.SetAimSource(AimSource.Mouse);

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
        /// ⚠️ AND A BOT TURNS TO FACE WHERE IT IS GOING. `character_base.gd:926` calls `look_at`
        /// on the movement vector for a unit that is not mouse-aimed, and every verb it can use
        /// fires along `-basis.z`. Without it a bot slides sideways while its punch, its lunge
        /// and its shove all leave along a forward vector that never moved.
        /// </summary>
        [UnityTest]
        public IEnumerator AMovementAimedSeatTurnsToFaceItsDirection()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.localScale = new Vector3(60.0f, 1.0f, 60.0f);
            floor.transform.position = new Vector3(0.0f, -0.5f, 0.0f);

            var go = new GameObject("Bot", typeof(CharacterController));
            go.transform.position = new Vector3(0.0f, 0.2f, 0.0f);

            var motor = go.AddComponent<CharacterMotor>();

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
