using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
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
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code. `PlayModeWorld.Reset` has the
        /// mechanism and why BOTH hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        private static IEnumerator LoadArena()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

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

            // ⚠️⚠️ THE SEAT'S OWN PRODUCER HAS TO BE STOPPED FIRST, AND THIS TEST FAILED FOR THE
            // WHOLE OF ITS FIRST LIFE WITHOUT IT — reporting "the capsule never left the ground"
            // against a jump that works perfectly in the player. `PlayerInputReader.Update` and
            // `AIController.Update` both write the ENTIRE intent table every frame, so the
            // `Set(Jump, true)` below was overwritten with `Jump = _jump.IsPressed()` — false, in
            // a batch runner with no keyboard — before the next physics step could read it. The
            // table is designed for exactly one writer; a test that writes it is that writer.
            //
            // ⚠️ AND THAT IS NOT A WEAKER TEST. What is being asserted is that a press edge
            // written in Update survives into FixedUpdate, which is a question about
            // `Intent.CommitFrame`'s owner and nothing else. Hardware is read in exactly one
            // place and everything downstream asks `InputIntent`, so the intent IS what a human
            // press reduces to.
            Silence(mover);

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

            // ⚠️⚠️ THE SEAT'S BOT IS SWITCHED OFF BEFORE ANYTHING IS POSITIONED, AND "BEFORE" IS
            // THE LOAD-BEARING WORD. This test is about the input path reaching `Carrier`, not
            // about the planner, and it does two things a bot ruins:
            //
            //  * It drops a slipper at the attacker's feet and then holds Grab. A bot that is
            //    walking carries the body out of `Balance.PickupRadius` while the loop runs, so
            //    the failure reads as "the press never arrived" when the press arrived at a
            //    character who had left. That is why this went red the day `DoStalk` started
            //    sliding around the ring instead of standing still, and why it passed with an
            //    earlier version of the same change: it was measuring how far the bot walked.
            //  * Two writers on one `InputIntent`, which the note further down already records.
            //    `AIController.Update` writes `Grab = false` on every frame its plan does not
            //    want it, erasing the press this loop is asserting on.
            //
            // A test that drives an intent has to own that intent, and it has to own the body
            // too. Same rule as `AIController.AbilitiesEnabled` (docs/TODO.md section 42).
            // ⚠️⚠️ AND IT IS `Silence`, NOT "TURN THE BOT OFF", BECAUSE A SEAT HAS TWO POSSIBLE
            // PRODUCERS AND THIS LINE ONLY EVER SILENCED ONE OF THEM. `MatchInstaller.HumanSeat`
            // gives ONE seat a `PlayerInputReader` instead of an `AIController`, and its default
            // is `GameLaunch.SoloSeat`, which is 1. The attacker chosen above is simply the first
            // non-defender `CharacterMotor` that `FindObjectsByType(FindObjectsSortMode.None)`
            // hands back, and that order is explicitly unsorted: when it happened to hand back
            // seat 1, this test silenced an `AIController` that was not there, left
            // `PlayerInputReader.Update` writing `Grab = false` over the press every frame, and
            // failed with "the Grab press edge never reached Carrier" against a pickup that works
            // perfectly in the player. It is the same fault the sibling test three methods up
            // records having lived with "for the whole of its first life", and the same fault the
            // block comment above this one describes for the bot. `Silence` is the helper that
            // already existed for it and it turns off BOTH.
            //
            // ⚠️ IT SURFACED AS AN ORDER-DEPENDENT FLAKE rather than a hard failure, which is why
            // it survived: `AnyAttackerCanPickUpAnySlipper` passes on its own and fails after
            // `BotBehaviourProbe` has run, because what actually moved was which body the
            // unsorted find returned first.
            var seatBot = attacker.GetComponent<AIController>();
            var seatReader = attacker.GetComponent<PlayerInputReader>();
            Silence(attacker);
            for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();

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

            // ⚠️⚠️ THE KEY IS RE-ASSERTED EVERY FRAME, BECAUSE THAT IS WHAT HOLDING ONE DOES.
            // `PlayerInputReader.Update` writes the whole verb table on every frame a human is
            // playing, and an `AIController` on a bot seat does the same. Setting the intent ONCE
            // and expecting it to survive twenty fixed steps only worked while `Carrier` happened
            // to run before whichever component owns that seat's intent, and Unity's order
            // between two components at the same `DefaultExecutionOrder` is UNSPECIFIED. It broke
            // the moment `AIController` and `PlayerInputReader` were given explicit orders on
            // 2026-08-27 (`docs/TODO.md` § 42), which is the fix for Nemu's recast being erased
            // by exactly this collision in the shipping game.
            //
            // ⚠️ IT IS STILL ONE PRESS EDGE. `JustPressed` is a diff against the snapshot
            // `CharacterMotor` takes at the end of its own step, so a key held true every frame
            // produces exactly one edge, which is what a player pressing E produces.
            //
            // ⚠️ THE SEAT'S BOT IS ALREADY OFF, up where the attacker was chosen. See that note.
            for (int i = 0; i < 20; i++)
            {
                attacker.Intent.Set(Verb.Grab, true);
                yield return new WaitForFixedUpdate();
            }

            attacker.Intent.Set(Verb.Grab, false);
            if (seatBot != null) seatBot.enabled = true;
            if (seatReader != null) seatReader.enabled = true;

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

        /// <summary>
        /// Stop whatever normally drives this seat, so the test owns its intent table.
        ///
        /// ⚠️ BOTH PRODUCERS, because which one a given seat has is decided by the installer and
        /// is not this test's business: the human seat carries `PlayerInputReader`, every other
        /// carries `AIController`, and both write the whole table every Update.
        /// </summary>
        /// <summary>
        /// A mash press has to reach `CharacterMotor.MashRecover` through the ordinary physics
        /// step, and answering a fall has to be worth what `Balance` says it is worth.
        ///
        /// ⚠️⚠️ THIS EXISTS BECAUSE *"mashing still broken"* WAS REPORTED AGAINST A BUILD WHOSE
        /// ARITHMETIC WAS ALREADY CORRECT, and there was no way to tell a dead press from a
        /// misread bar without one. The HUD side of that report is answered in
        /// `Hud.UpdateGetUpPrompt`; this is the other half, and it is the half that can regress
        /// silently. The mash rides `Verb.Jump` and is read in `FixedUpdate` before
        /// `Intent.CommitFrame`, which is three ordering facts that a later refactor can break
        /// without breaking anything a compiler would notice.
        ///
        /// ⚠️ IT PRESSES AT `Balance.MashCooldown`, NOT AS FAST AS THE LOOP WILL GO. The rate cap
        /// lives in `Combat.MashRecover` and refuses anything faster, so a test that spammed
        /// every step would measure the cap rather than the mash.
        /// </summary>
        [UnityTest]
        public IEnumerator MashingShortensAFallByWhatBalanceSays()
        {
            yield return LoadArena();

            CharacterMotor faller = null;

            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
            {
                if (!m.IsPerson) continue;
                faller = m;
                break;
            }

            Assert.IsNotNull(faller, "no Person in the arena to trip");
            Silence(faller);

            for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();

            const float Trip = 2.5f;

            // ---- part one: does a press reach the motor at all? ----
            //
            // ⚠️⚠️ ASSERTED SEPARATELY FROM WHAT IT IS WORTH, BECAUSE THE TWO FAIL FOR
            // COMPLETELY DIFFERENT REASONS. A dead Jump edge and a mis-tuned constant both come
            // back as "the fall was too long"; only one of them is a bug. The first version of
            // this test measured them together, went red, and could not say which.
            faller.ClearTrip();
            faller.ApplyTrip(Trip);

            faller.Intent.Set(Verb.Jump, true);
            yield return new WaitForFixedUpdate();
            faller.Intent.Set(Verb.Jump, false);
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(1, faller.MashPresses,
                "a held Jump did not reach CharacterMotor.MashRecover through the physics step. " +
                "The read is in FixedUpdate BEFORE Intent.CommitFrame; if the snapshot moves " +
                "ahead of it, JustPressed is false for every verb the physics step resolves. " +
                "See PlayerInputReader.Update.");

            Assert.Greater(faller.MashRemoved, 0.0f,
                "the press was counted but bought nothing, so Combat.MashRecover accepted it " +
                "and then clamped it away. Check MinTripDown against the trip length.");

            // ---- part two: what is answering a fall actually worth? ----
            //
            // ⚠️ THE RATE CAP IS LEFT TO DO ITS JOB. `Combat.MashRecover` refuses anything inside
            // `Balance.MashCooldown` and changes nothing, so pressing on every physics step
            // measures the cap rather than beating it, and it takes the input timing out of a
            // question that is about the two balance constants.
            faller.ClearTrip();
            faller.ApplyTrip(Trip);

            float ignored = 0.0f;
            while (faller.IsTripped && ignored < 12.0f)
            {
                yield return new WaitForFixedUpdate();
                ignored += Time.fixedDeltaTime;
            }

            Assert.False(faller.IsTripped, $"an unanswered {Trip:0.00} s fall never ended: it was " +
                                           $"still running after {ignored:0.00} s");

            // ⚠️⚠️ AND IT LASTED THE GUARD, NOT A DECAY. Nothing bleeds a fall away any more, so
            // an unanswered one has to sit at its starting length until
            // `Balance.TripAutoRecoverSeconds` releases it. A shorter reading here means some
            // clock has been reintroduced above `MinTripDown`, which is the defect this whole
            // rework removes.
            Assert.Greater(ignored, Balance.TripAutoRecoverSeconds * 0.9f,
                $"an unanswered fall ended after {ignored:0.00} s, well inside the " +
                $"{Balance.TripAutoRecoverSeconds:0.00} s guard: something is still running the " +
                "trip down on its own.");

            // ⚠️⚠️ THE BAR IS THE GATE, SO IT MUST READ FULL AT THE MOMENT OF STANDING, INCLUDING
            // ON THE PATH NOBODY PRESSED. 🧑: *"sometimes i get up with it still at middle or
            // when i only clicked once"*. `Hud.UpdateGetUpPrompt` draws `MashRemoved` over the
            // mashable slack, so this is that frame measured rather than looked at.
            float slack = Trip - Balance.MinTripDown;
            Assert.GreaterOrEqual(faller.MashRemoved, slack - 0.01f,
                $"the fall ended with the get-up meter at {faller.MashRemoved / slack:P0}, which is " +
                "the exact frame the report was about.");

            faller.ClearTrip();
            faller.ApplyTrip(Trip);

            float mashed = 0.0f;
            while (faller.IsTripped && mashed < 12.0f)
            {
                faller.MashRecover();
                yield return new WaitForFixedUpdate();
                mashed += Time.fixedDeltaTime;
            }

            Assert.False(faller.IsTripped,
                $"a mashed fall never ended: still down after {mashed:0.00} s with " +
                $"{faller.MashPresses} accepted presses");

            Assert.GreaterOrEqual(faller.MashRemoved, slack - 0.01f,
                $"a mashed fall ended with the meter at {faller.MashRemoved / slack:P0}.");

            // ⚠️ THE BOUND IS A RATIO, NOT A TIME. `TripAutoRecoverSeconds` and
            // `MashRecoverPerPress` are both open balance numbers; what must never regress is
            // that pressing is worth substantially more than waiting. The arithmetic on those
            // two constants says 4.0x today, and 1.6x is a floor a real defect falls through
            // while a tuning pass does not.
            Assert.Greater(ignored / mashed, 1.6f,
                $"mashing bought almost nothing: an ignored fall ran {ignored:0.00} s and a " +
                $"mashed one {mashed:0.00} s over {faller.MashPresses} accepted presses.");
        }

        private static void Silence(CharacterMotor motor)
        {
            var reader = motor.GetComponent<PlayerInputReader>();
            if (reader != null) reader.enabled = false;

            var ai = motor.GetComponent<AIController>();
            if (ai != null) ai.enabled = false;

            motor.Intent.Clear();
            motor.Intent.CommitFrame();
        }

        private static int AssertOutlined(GameObject root, string what)
        {
            int seen = 0;

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                // ⚠️ AN EFFECT PARENTED TO A PROP IS NOT PART OF THE PROP. The lata's
                // restore-protection shell is a transparent sphere under the can, and a toon
                // shader would draw an ink outline round it and make it a solid object: the one
                // thing it must not be. `VfxRenderTag` is attached by `VfxMaterial` itself, so
                // this exempts every effect written later without anybody editing this test.
                if (r.GetComponent<Visual.VfxRenderTag>() != null) continue;

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
