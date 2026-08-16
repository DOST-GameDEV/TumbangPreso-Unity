using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// A press edge written in Update has to survive into the physics step that resolves it.
    ///
    /// ⚠️⚠️ THIS IS THE REGRESSION TEST FOR THE BUG THAT MADE JUMP AND GRAB DO NOTHING AT ALL.
    /// `JustPressed` is a diff against the last committed snapshot, and BOTH producers
    /// (`PlayerInputReader` and `AIController`) used to take that snapshot themselves at the end
    /// of their own Update. Unity runs FixedUpdate before Update inside a frame, so by the time
    /// `CharacterMotor.ApplyGravity` asked `JustPressed(Verb.Jump)` the held set had already been
    /// copied over the previous one and the answer was always false. Every verb resolved in the
    /// physics step was therefore unreachable, for a bot exactly as much as for a human.
    ///
    /// Reported as two separate complaints, *"some controls also dont exist in unity like jump"*
    /// and *"u cant grab shit"*. One fault.
    ///
    /// ⚠️ IT ASSERTS THROUGH THE INTENT, NOT THROUGH THE KEYBOARD. There is no way to synthesise
    /// a real key press in a batch-mode test, and there does not need to be: hardware is read in
    /// exactly one place and everything downstream asks `InputIntent`, so writing the intent is
    /// what a human press and a bot decision both reduce to. That is the whole point of the
    /// indirection, and it is what makes this testable at all.
    /// </summary>
    public class InputEdgeTests
    {
        private static IEnumerator LoadArena()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            // The installer builds the match in Start and the cast needs a few steps to settle
            // onto the ground before any of them is worth testing.
            for (int i = 0; i < 20; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator AJumpPressReachesThePhysicsStep()
        {
            yield return LoadArena();

            CharacterMotor mover = null;

            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
            {
                if (!m.IsPerson || !m.CanAct()) continue;
                mover = m;
                break;
            }

            Assert.IsNotNull(mover, "no Person in the arena to jump");

            // Let it settle so the test is not measuring a spawn drop.
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();

            float floor = mover.transform.position.y;

            // ⚠️ HELD ACROSS THE STEP, NOT SET AND CLEARED. A producer writes the held state
            // every Update and the motor takes the snapshot; setting and clearing inside one
            // frame is not something either producer can actually do.
            mover.Intent.Set(Verb.Jump, true);

            float peak = floor;

            for (int i = 0; i < 40; i++)
            {
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, mover.transform.position.y);
            }

            mover.Intent.Set(Verb.Jump, false);

            Assert.Greater(peak, floor + 0.25f,
                $"the capsule never left the ground: rose {peak - floor:0.000} m from {floor:0.000}. " +
                "The Jump press edge is not reaching CharacterMotor.ApplyGravity, which means " +
                "the intent snapshot is being taken before the physics step rather than at the " +
                "end of it. See the note in PlayerInputReader.Update.");
        }

        /// <summary>
        /// ⚠️ ANY ATTACKER, ANY SLIPPER. 🧑: *"make sure the slippers can actually be picked up by
        /// anyone"*. Ownership deliberately does NOT gate the pickup: `OwnerSlot` decides whose
        /// glow and whose skin a tsinelas wears and nothing else, so an attacker who reaches
        /// somebody else's slipper first is entitled to it.
        ///
        /// ⚠️ THE TAYA IS THE ONE EXCEPTION AND IT IS NOT A BUG. `carrier.gd::_step_grab` returns
        /// early on `is_defender`, because the defender's verbs are the tag and the reset
        /// channel. Asserted here so nobody "fixes" it later.
        /// </summary>
        [UnityTest]
        public IEnumerator AnyAttackerCanPickUpAnySlipper()
        {
            yield return LoadArena();

            var slippers = Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None);
            Assert.Greater(slippers.Length, 0, "no slippers in the arena");

            CharacterMotor attacker = null;
            CharacterMotor defender = null;

            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
            {
                if (!m.IsPerson) continue;
                if (m.IsDefender) defender = m;
                else if (attacker == null) attacker = m;
            }

            Assert.IsNotNull(attacker, "no attacker seat");

            // Somebody else's slipper, so the test is about eligibility rather than ownership.
            Slipper target = null;

            foreach (var s in slippers)
            {
                if (s.State != SlipperState.Loose) continue;
                if (s.OwnerSlot == attacker.PlayerSlot) continue;
                target = s;
                break;
            }

            if (target == null)
                foreach (var s in slippers)
                    if (s.State == SlipperState.Loose) { target = s; break; }

            Assert.IsNotNull(target, "no loose slipper to pick up");

            Assert.IsTrue(target.OwnerSlot != attacker.PlayerSlot || slippers.Length == 1,
                "wanted a slipper this attacker does not own, to prove ownership is not a gate");

            // ⚠️ THE SLIPPER MOVES, NOT THE CHARACTER. Putting the attacker on the slipper looks
            // equivalent and is not: `CharacterMotor.Confine` clamps X and Z back into the
            // confinement square every step, so if the slipper happens to lie outside the box the
            // capsule is dragged straight back off it and the pickup fails for a reason that has
            // nothing to do with what is being tested. A loose slipper has no such constraint.
            var stand = attacker.transform.position;
            target.transform.position = new Vector3(stand.x, target.transform.position.y, stand.z);

            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

            Assert.IsTrue(attacker.CanAct(),
                "the attacker cannot act, so no verb of theirs can be tested");

            float reach = Vector3.Distance(attacker.transform.position, target.transform.position);

            Assert.IsTrue(target.CanBeGrabbedBy(attacker),
                $"an attacker {reach:0.00} m from a loose slipper cannot grab it " +
                $"(PickupRadius is {Core.Balance.PickupRadius}, state {target.State}, " +
                $"defender {attacker.IsDefender}, canAct {attacker.CanAct()})");

            if (defender != null)
                Assert.IsFalse(target.CanBeGrabbedBy(defender),
                    "the taya must not be able to pick up ammunition: carrier.gd::_step_grab " +
                    "returns early on is_defender");

            attacker.Intent.Set(Verb.Grab, true);

            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();

            attacker.Intent.Set(Verb.Grab, false);

            Assert.IsTrue(attacker.HoldingSlipper,
                "the Grab press edge never reached Carrier: the attacker is standing on a " +
                "grabbable slipper and still holds nothing.");
        }

        /// <summary>
        /// 🧑: *"the slippers are floating for everyone"*, and *"make sure the slippers dont
        /// deattach when animations play"*.
        ///
        /// ⚠️ EVERY PERSON, NOT JUST ONE. `CarryTests` asserts that SOME seat built a hand
        /// anchor, which passes while three of the four are missing one: a unit whose rig did not
        /// resolve `arm-right` has `Hand()` return null, `Carrier` never writes the slipper's
        /// transform, and it hangs wherever the pickup left it. "Floating for everyone" is what
        /// a per-seat failure looks like from the outside.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryPersonBuildsAHandAnchor()
        {
            yield return LoadArena();

            int people = 0;

            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
            {
                if (!m.IsPerson) continue;

                people++;

                var visual = m.GetComponentInChildren<Visual.CharacterVisual>(true);

                Assert.IsNotNull(visual, $"seat {m.PlayerSlot} has no CharacterVisual");
                Assert.IsNotNull(visual.HandAnchor,
                    $"seat {m.PlayerSlot} built no hand anchor, so anything it carries will " +
                    "hang in the air instead of riding the arm");
            }

            Assert.AreEqual(Core.Balance.PlayerCount, people,
                "the arena did not build a full cast of People");
        }

        /// <summary>
        /// 🧑: *"make sure they have the outline that theyre supposed to and stuff"*. Every hero
        /// prop wears the toon material and its ink border; a slipper on the stock lit shader is
        /// the single most visible way this port stops looking like the game.
        /// </summary>
        [UnityTest]
        public IEnumerator EverySlipperAndTheLataWearTheToonOutline()
        {
            yield return LoadArena();

            int checkedProps = 0;

            foreach (var slipper in Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
                checkedProps += AssertOutlined(slipper.gameObject, "slipper");

            foreach (var lata in Object.FindObjectsByType<Lata>(FindObjectsSortMode.None))
                checkedProps += AssertOutlined(lata.gameObject, "lata");

            Assert.Greater(checkedProps, 0, "no hero prop renderers were found to check");
        }

        private static int AssertOutlined(GameObject root, string what)
        {
            int seen = 0;

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var material = r.sharedMaterial;
                if (material == null || material.shader == null) continue;

                Assert.AreEqual("TumbangPreso/Toon", material.shader.name,
                    $"a {what} renderer ({r.name}) is on '{material.shader.name}' rather than " +
                    "the toon material, so it has no ink outline and no palette remap");

                Assert.Greater(material.GetFloat("_OutlineWidth"), 0.0f,
                    $"a {what} renderer ({r.name}) is on the toon material but its outline " +
                    "width is zero, so the border is not drawn");

                seen++;
            }

            return seen;
        }
    }
}
