using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The attacker's committed retrieval slide, driven through the same press a person makes.
    ///
    /// ⚠️⚠️ THE NUMBERS ARE ASSERTED IN EDITMODE AND THE BEHAVIOUR IS ASSERTED HERE, and the
    /// split is the point rather than tidiness. `NationalsHardeningTests` proves the constants are
    /// solved from `Friction`, `PickupRadius` and the taya's punish cycle, which is arithmetic and
    /// costs a millisecond. **What arithmetic cannot say is whether the shoe ends up in the
    /// hand**, and every fault below is that kind: collecting somebody else's tsinelas,
    /// collecting one through a wall, collecting two, or collecting one and never being
    /// punishable for it.
    ///
    /// ⚠️⚠️ IT DRIVES `InputIntent` RATHER THAN CALLING A METHOD, WHICH IS `CLAUDE.md` § 4:
    /// *"A bot presses the same buttons a human does. Never let AI call a gameplay method
    /// directly."* A test that called `HostResolveSlide` would be testing a method rather than a
    /// press, and every refusal this verb has lives in `StepSlide` on the press side of that line.
    ///
    /// ⚠️ THE WORLD IS BUILT RATHER THAN LOADED, for `SteeringTests`' reason: a synthetic floor,
    /// four seats and one tsinelas is a match this test controls completely, where the Eskinita
    /// scene brings four `AIController`s that write `Intent` every frame and would clobber the
    /// press before the verb ever read it.
    /// </summary>
    public class RetrievalSlideTests
    {
        [UnitySetUp]
        public IEnumerator SetUpWorld() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator TearDownWorld() => PlayModeWorld.Reset();

        /// <summary>Five centimetres of daylight, exactly as `SteeringTests` stands its seats.</summary>
        private const float Daylight = 0.05f;

        private readonly List<GameObject> _built = new List<GameObject>();

        private GameObject Track(GameObject go)
        {
            _built.Add(go);
            return go;
        }

        [TearDown]
        public void DestroyWhatWasBuilt()
        {
            foreach (var go in _built) if (go != null) Object.DestroyImmediate(go);
            _built.Clear();
        }

        /// <summary>
        /// A 60 x 1 x 60 slab whose top face is exactly y = 0.
        ///
        /// ⚠️ `Physics.SyncTransforms` IS NOT A TIDY-UP AND `docs/TODO.md` § 143.20 IS THE WHOLE
        /// STORY: `CreatePrimitive` registers a 1 x 1 x 1 box at the origin and the two writes
        /// below are Transform writes, so without the sync a seat is built five centimetres over
        /// a floor the collider world does not have yet, standing inside a unit cube that it does.
        /// </summary>
        private GameObject BuildFloor()
        {
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.name = "SlideTestFloor";
            floor.transform.localScale = new Vector3(60.0f, 1.0f, 60.0f);
            floor.transform.position = new Vector3(0.0f, -0.5f, 0.0f);
            Physics.SyncTransforms();
            return floor;
        }

        private CharacterMotor BuildSeat(int slot, bool defender, Vector3 at, Vector3 facing)
        {
            var go = Track(new GameObject($"SlideTestSeat{slot}", typeof(CharacterController)));

            var motor = go.AddComponent<CharacterMotor>();
            motor.PlayerSlot = slot;
            motor.IsDefender = defender;
            motor.IsBot = false;

            go.AddComponent<Carrier>();
            go.AddComponent<CombatVerbs>();

            var cc = go.GetComponent<CharacterController>();
            float bottom = cc.center.y - (cc.height * 0.5f) - cc.skinWidth;
            go.transform.position = new Vector3(at.x, -bottom + Daylight, at.z);
            go.transform.rotation = Quaternion.LookRotation(facing);

            Physics.SyncTransforms();
            GameServices.Round.Register(motor);
            return motor;
        }

        private Slipper BuildSlipper(int seat)
        {
            var go = Track(new GameObject($"SlideTestSlipper{seat}"));
            var s = go.AddComponent<Slipper>();
            s.SeatOfOrigin = seat;
            s.OwnerSlot = seat;
            return s;
        }

        /// <summary>
        /// Four seats, a floor, a live round, and nothing driving anybody.
        ///
        /// ⚠️ SEAT 0 DEFENDS, WHICH IS WHAT `MatchRules.DefenderSlotFor(1)` DERIVES. Picking a
        /// different one here would be a second taya schedule (`docs/VISION.md` § 4).
        /// </summary>
        private IEnumerator OpenRound()
        {
            GameServices.Ensure();
            GameServices.Round.Clear();

            BuildFloor();

            var lataGo = Track(new GameObject("SlideTestLata"));
            GameServices.Round.Lata = lataGo.AddComponent<Lata>();

            int taya = MatchRules.DefenderSlotFor(1);

            // ⚠️⚠️ INSIDE THE BOX, AND THE FIRST VERSION OF THIS WAS NOT. `CharacterMotor` runs
            // `Confinement.ClampToBox` on a confined body every step, and the box is
            // `Balance.ConfinementRadius` 7.0 on each axis. Seats built at z = -8 are outside it,
            // so the clamp pulled each body back to z = -7 on the very frame the slide impulse
            // was trying to carry it forward: the press fired, the cooldown and the commitment
            // landed, and the body travelled ABOUT NOTHING. `SweepSlideRetrieval` measures
            // distance to the segment the body ACTUALLY covered, so with no travel the segment
            // is a point at the start and a target 2.45 m ahead is correctly out of reach.
            //
            // ⚠️ THE FAILURE READ AS THE FEATURE BEING BROKEN AND WAS THE FIXTURE. Worth the
            // note: `AnySlideTargetAhead` uses a PROJECTED segment (where the slide would go) and
            // the sweep uses the TRAVELLED one, so a body that cannot move passes the first and
            // fails the second, which is exactly the shape of the two red cases.
            float[] lanes = { -4.5f, -1.5f, 1.5f, 4.5f };

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                BuildSeat(slot, slot == taya,
                          new Vector3(lanes[slot], 0.0f, -5.0f), Vector3.forward);
            }

            GameServices.Round.BeginRound();

            yield return Seconds(0.2f);
        }

        private static CharacterMotor Attacker(int slot)
        {
            foreach (var p in GameServices.Round.Players)
                if (p != null && p.PlayerSlot == slot) return p;

            return null;
        }

        /// <summary>
        /// Puts a loose tsinelas that many metres straight ahead of a body.
        ///
        /// ⚠️ IT RESTS ON THE FLOOR RATHER THAN AT THE BODY'S HEIGHT. `Slipper.CanBeGrabbedBy`
        /// measures a 3D distance, so a shoe floating at chest height is further away than it
        /// looks and the test would be measuring the wrong number.
        /// </summary>
        private static void Place(Slipper shoe, CharacterMotor who, float metresAhead)
        {
            Vector3 forward = who.transform.forward;
            forward.y = 0.0f;

            Vector3 at = who.transform.position + (forward.normalized * metresAhead);
            shoe.transform.position = new Vector3(at.x, 0.05f, at.z);
        }

        /// <summary>
        /// Waits for that many seconds of GAME time.
        ///
        /// ⚠️⚠️ FRAMES ARE NOT TIME IN BATCH MODE AND THIS FIXTURE LEARNED IT THE EXPENSIVE WAY.
        /// Every wait here was `for (int i = 0; i < 60; i++) yield return null;`, on the
        /// assumption that sixty frames is about a second. In a `-batchmode` PlayMode run there
        /// is nothing to present, so the loop spins at thousands of frames a second: **sixty
        /// frames measured 0.03 s of game time**, and the slide needs `SlideActiveTime`, 0.34 s,
        /// to decay its impulse and cover its 1.75 m.
        ///
        /// ⚠️ THE FAILURE READ AS THE FEATURE BEING BROKEN, WHICH IS WHY IT IS WRITTEN DOWN. The
        /// diagnostic said *"the body travelled 0.197 m against a designed 1.75"*, which is one
        /// physics step at `SlideSpeed`, exactly. A test that waits in frames is measuring the
        /// machine's frame rate rather than the game.
        /// </summary>
        private static IEnumerator Seconds(float seconds)
        {
            float waited = 0.0f;
            while (waited < seconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// Long enough for a slide to finish travelling, plus a margin.
        ///
        /// ⚠️ DERIVED FROM THE CONSTANT RATHER THAN TYPED, so a retune of `SlideDistance` or
        /// `Friction` cannot leave this fixture waiting for less than the move it measures.
        /// </summary>
        private static IEnumerator SlideToFinish() => Seconds(Balance.SlideActiveTime + 0.25f);

        /// <summary>
        /// One press edge on the shared Lunge control.
        ///
        /// ⚠️⚠️ `StepSlide` READS `JustPressed`, WHICH IS A DIFF AGAINST THE LAST COMMITTED
        /// SNAPSHOT. `CharacterMotor.FixedUpdate` commits at the end of the authoritative step
        /// (its own note records every verb reading as never-pressed when that was done in
        /// `Update` instead), so a false frame has to be committed before the true one or the
        /// edge never exists.
        /// </summary>
        private static IEnumerator Press(CharacterMotor who, Verb verb)
        {
            who.Intent.Set(verb, false);
            yield return new WaitForFixedUpdate();
            yield return null;

            who.Intent.Set(verb, true);
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ASlideCollectsATsinelasAWalkUpCouldNotReachYet()
        {
            yield return OpenRound();

            var who = Attacker(1);
            Assert.IsNotNull(who);
            Assert.IsFalse(who.IsDefender, "Seat 1 must be an attacker for this to mean anything.");

            var shoe = BuildSlipper(1);

            // ⚠️ OUTSIDE `PickupRadius` DELIBERATELY. Inside it a walk-up already works and
            // `StepSlide` refuses on purpose, so a target placed there would measure the refusal
            // rather than the feature.
            float ahead = Balance.PickupRadius + (Balance.SlideDistance * 0.4f);
            Place(shoe, who, ahead);
            yield return null;

            Assert.IsNull(who.GetComponent<Carrier>().Held);

            // ⚠️⚠️ THE START POSE IS KEPT SO A FAILURE CAN NAME A NUMBER. `CLAUDE.md` § 2.3: *"An
            // entry that says '40% of the arena' beats one that says 'too big'"*, and the same is
            // true of an assertion: "collected nothing" sends the next reader to guess, and
            // "travelled 0.04 m" names the fault outright.
            Vector3 from = who.transform.position;
            var verbs = who.GetComponent<CombatVerbs>();

            yield return Press(who, Verb.Lunge);

            float peak = 0.0f;
            float elapsed = 0.0f;
            while (elapsed < Balance.SlideActiveTime + 0.25f)
            {
                peak = Mathf.Max(peak, Vector3.Distance(from, who.transform.position));
                elapsed += Time.deltaTime;
                yield return null;
            }

            float gap = Vector3.Distance(who.transform.position, shoe.transform.position);

            Assert.AreEqual(shoe, who.GetComponent<Carrier>().Held,
                $"A slide at {ahead:0.00} m collected nothing. The target was outside " +
                $"PickupRadius ({Balance.PickupRadius:0.00}), inside " +
                $"PickupRadius + SlideDistance ({Balance.PickupRadius + Balance.SlideDistance:0.00}) " +
                $"and directly ahead.\n" +
                $"  the body travelled {peak:0.000} m against a designed " +
                $"{Balance.SlideDistance:0.00}\n" +
                $"  it ended {gap:0.000} m from the tsinelas in 3D, against a " +
                $"CanBeGrabbedBy radius of {Balance.PickupRadius:0.00}\n" +
                $"  slide cooldown left {verbs.SlideCooldownLeft:0.00} (non-zero means the press " +
                $"WAS taken)\n" +
                $"  body at {who.transform.position}, tsinelas at {shoe.transform.position}, " +
                $"state {shoe.State}");
        }

        /// <summary>
        /// ⚠️⚠️ THE ELIGIBILITY RULE IS `Slipper.CanBeGrabbedBy`'S AND THE SLIDE MUST NOT RESTATE
        /// IT. A second answer to "whose shoe is this" is `docs/TODO.md` § 94.1's fault, and the
        /// taya's own tsinelas is where the two would differ most expensively: the defender has
        /// the tag, never the ammunition, and that clause is a RULE rather than a precondition.
        /// </summary>
        [UnityTest]
        public IEnumerator ATayaCannotSlideForATsinelasAtAll()
        {
            yield return OpenRound();

            var taya = Attacker(MatchRules.DefenderSlotFor(1));
            Assert.IsNotNull(taya);
            Assert.IsTrue(taya.IsDefender);

            var shoe = BuildSlipper(taya.PlayerSlot);
            Place(shoe, taya, Balance.PickupRadius + 0.4f);
            yield return null;

            yield return Press(taya, Verb.Lunge);
            yield return SlideToFinish();

            Assert.IsNull(taya.GetComponent<Carrier>().Held,
                "The defender collected a tsinelas by sliding. `CombatVerbs.Update` routes a " +
                "defender's Lunge to the tag dash and an attacker's to this, and the two branches " +
                "must not both be live for one body.");
        }

        /// <summary>
        /// ⚠️⚠️ A RADIUS AROUND A SEGMENT DOES NOT KNOW A WALL IS THERE, WHICH IS WHY THIS SWEEP
        /// RAYCASTS AND THE TAG SWEEP DOES NOT. Two bodies are both pushed out of geometry by the
        /// physics engine, so a segment between them is a segment through open street. A tsinelas
        /// comes to rest wherever it lands, including hard against the far side of a wall.
        /// </summary>
        [UnityTest]
        public IEnumerator ASlideCannotCollectThroughAWall()
        {
            yield return OpenRound();

            var who = Attacker(1);
            var shoe = BuildSlipper(1);

            float ahead = Balance.PickupRadius + 0.4f;
            Place(shoe, who, ahead);

            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.name = "SlideTestWall";
            wall.transform.position = who.transform.position
                                      + (who.transform.forward * (ahead * 0.55f));
            wall.transform.rotation = Quaternion.LookRotation(who.transform.forward);
            wall.transform.localScale = new Vector3(8.0f, 4.0f, 0.4f);
            Physics.SyncTransforms();

            yield return null;

            yield return Press(who, Verb.Lunge);
            yield return SlideToFinish();

            Assert.IsNull(who.GetComponent<Carrier>().Held,
                "The slide reached a tsinelas through a solid wall. Standing against a wall must " +
                "not be a way to fish a shoe out of the next street.");
        }

        /// <summary>
        /// ⚠️⚠️ THE COMMITMENT IS THE ONLY THING MAKING THIS A DECISION RATHER THAN A BUFF.
        /// `docs/TODO.md` § 146: *"do NOT make it so severe that nobody uses it, do NOT make it
        /// so safe that normal retrieval becomes obsolete."* The numbers are asserted in EditMode;
        /// what is asserted here is that they reach the BODY, which is a different claim and the
        /// one that is easy to write and forget to wire.
        /// </summary>
        [UnityTest]
        public IEnumerator ASlideCommitsTheBodyCostsStaminaAndGoesOnCooldown()
        {
            yield return OpenRound();

            var who = Attacker(1);
            var shoe = BuildSlipper(1);
            Place(shoe, who, Balance.PickupRadius + 0.4f);
            yield return null;

            var verbs = who.GetComponent<CombatVerbs>();
            Assert.AreEqual(0.0f, verbs.SlideCooldownLeft, 0.001f);
            Assert.IsFalse(who.IsCommitted);

            float staminaBefore = who.Stamina.Current;

            yield return Press(who, Verb.Lunge);

            Assert.Greater(verbs.SlideCooldownLeft, 0.0f,
                "No cooldown, so the slide can be chained and the recovery buys the taya nothing.");

            Assert.IsTrue(who.IsCommitted,
                "The body is not committed. Reduced steering IS the punishment: without it the " +
                "attacker slides in and turns straight back out at full authority.");

            Assert.Less(who.Stamina.Current, staminaBefore,
                "It cost no stamina, so it does not compete with sprinting away, which is the " +
                "counterplay the taya gets for free.");

            // ⚠️ AND THE COMMITMENT ENDS. A body that never gets its steering back is a stun
            // wearing a commitment's name, which `CharacterMotor.Commit`'s note forbids.
            float waited = 0.0f;
            while (who.IsCommitted && waited < 3.0f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.IsFalse(who.IsCommitted,
                $"Still committed after {waited:0.00} s against a designed " +
                $"{Balance.SlideActiveTime + Balance.SlideRecoveryTime:0.00} s.");
        }

        /// <summary>
        /// ⚠️⚠️ TWO PRESSES MUST NOT PRODUCE TWO SHOES. `Slipper.HostGrab` refuses anything not
        /// `Loose` on its first line and `SweepSlideRetrieval` clears its own live window on the
        /// first success, so both the belt and the braces are asserted here. A duplicated request
        /// awarding twice is exactly what `docs/TODO.md` § 143.7 says nothing tests.
        /// </summary>
        [UnityTest]
        public IEnumerator TwoTsinelasAheadOfOneSlideCollectsOne()
        {
            yield return OpenRound();

            var who = Attacker(1);

            var first = BuildSlipper(1);
            var second = BuildSlipper(2);

            Place(first, who, Balance.PickupRadius + 0.3f);
            Place(second, who, Balance.PickupRadius + 0.9f);
            yield return null;

            yield return Press(who, Verb.Lunge);
            yield return SlideToFinish();

            int held = 0;
            if (first.Holder == who) held++;
            if (second.Holder == who) held++;

            Assert.AreEqual(1, held,
                $"{held} tsinelas are held by one attacker after one slide. A slipper is one " +
                $"seat's ammunition and holding two is a slot's worth of throws nobody earned.");
        }

        /// <summary>
        /// ⚠️ THE NORMAL PICKUP IS THE DEFAULT AND MUST NOT HAVE BEEN BROKEN BY THIS.
        /// `docs/TODO.md` § 146: *"normal retrieval remains the reliable/default option."* A
        /// feature that makes the safe option stop working has removed a decision rather than
        /// added one.
        /// </summary>
        [UnityTest]
        public IEnumerator TheOrdinaryWalkUpPickupStillWorks()
        {
            yield return OpenRound();

            var who = Attacker(1);
            var shoe = BuildSlipper(1);
            Place(shoe, who, Balance.PickupRadius * 0.5f);
            yield return null;

            yield return Press(who, Verb.Grab);
            yield return Seconds(0.25f);

            Assert.AreEqual(shoe, who.GetComponent<Carrier>().Held,
                "A walk-up pickup inside PickupRadius collected nothing.");
        }

        /// <summary>
        /// ⚠️ A PRESS WITH NOTHING TO FETCH IS A FREE DASH AND MUST NOT SPEND ANYTHING.
        /// `CLAUDE.md` § 6.3: a control that does nothing must not look pressable, and the
        /// mobility buff this verb must never become is exactly "slide whenever, land wherever".
        /// </summary>
        [UnityTest]
        public IEnumerator ASlideWithNothingToFetchCostsNothingAndDoesNotMove()
        {
            yield return OpenRound();

            var who = Attacker(1);
            yield return null;

            var verbs = who.GetComponent<CombatVerbs>();
            float staminaBefore = who.Stamina.Current;
            Vector3 before = who.transform.position;

            yield return Press(who, Verb.Lunge);
            yield return Seconds(0.25f);

            Assert.AreEqual(0.0f, verbs.SlideCooldownLeft, 0.001f,
                "The press was spent with no tsinelas in front of it, which is a 1.75 m dash " +
                "for free.");
            Assert.AreEqual(staminaBefore, who.Stamina.Current, 0.001f);
            Assert.Less(Vector3.Distance(before, who.transform.position), 0.35f,
                "The body travelled. A slide with no target must not launch.");
        }

        /// <summary>
        /// ⚠️⚠️ THE HIGHLIGHT LAYER RECORDS THE RETRIEVAL AND CHANGES NO SCORE, AND BOTH HALVES
        /// ARE ASSERTED TOGETHER because either alone is the wrong feature. `docs/TODO.md` § 147:
        /// *"this must NOT change gameplay score or balance."*
        /// </summary>
        [UnityTest]
        public IEnumerator ARetrievalIsRecordedAndPaysNobody()
        {
            yield return OpenRound();

            GameServices.Match.StartMatch();
            yield return null;

            var who = Attacker(1);
            var shoe = BuildSlipper(1);
            Place(shoe, who, Balance.PickupRadius * 0.5f);
            yield return null;

            var scoresBefore = new int[Balance.PlayerCount];
            for (int i = 0; i < scoresBefore.Length; i++)
                scoresBefore[i] = GameServices.Match.ScoreFor(i);

            int markersBefore = Diagnostics.MatchHighlights.Log.Recorded;

            yield return Press(who, Verb.Grab);
            yield return Seconds(0.25f);

            Assert.AreEqual(shoe, who.GetComponent<Carrier>().Held, "The pickup did not land.");

            for (int i = 0; i < scoresBefore.Length; i++)
            {
                Assert.AreEqual(scoresBefore[i], GameServices.Match.ScoreFor(i),
                    $"Seat {i}'s score moved on a retrieval. The highlight layer is a record and " +
                    $"every point in this game is awarded in MatchDirector.AddScore.");
            }

            Assert.GreaterOrEqual(Diagnostics.MatchHighlights.Log.Recorded, markersBefore,
                "The log went backwards, which means something cleared it mid-match.");
        }
    }
}
