using System.Collections;
using NUnit.Framework;
using TumbangPreso;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Runs the game and measures it.
    ///
    /// ⚠️⚠️ THIS IS THE ONLY SUITE THAT CAN CATCH AN EMERGENT FAULT. Core.Tests proves the
    /// arithmetic and the EditMode suite proves the wiring, but neither can tell you that a
    /// match never advances past round 1, that the bots stand still, or that gravity puts every
    /// seat through the floor. Those are the failures that make a build unplayable, and they
    /// only appear when something actually runs.
    ///
    /// ⚠️ IT BUILDS ITS OWN WORLD RATHER THAN LOADING A SCENE. A test that depends on a scene
    /// asset fails for two different reasons (the code is wrong, or the scene drifted) and
    /// cannot tell you which. Constructing the world here means a failure is always the code.
    /// </summary>
    public class MatchRunTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            Time.timeScale = 1.0f;
        }

        /// <summary>
        /// ⚠️ SEATS MUST NOT FALL THROUGH THE WORLD. The most basic thing that can be wrong
        /// after a physics port, and the one that invalidates every other measurement taken in
        /// the same run.
        /// </summary>
        [UnityTest]
        public IEnumerator SeatsStayOnTheGroundAndInsideTheWorld()
        {
            BuildWorld();
            yield return new WaitForSeconds(1.0f);

            foreach (var m in GameServices.Round.Players)
            {
                Assert.Greater(m.transform.position.y, -1.0f,
                    $"{m.name} fell through the floor: the controller or gravity is wrong");
                Assert.Less(m.transform.position.y, 5.0f,
                    $"{m.name} is airborne a second in: an impulse or the ground is wrong");

                float reach = Mathf.Max(Mathf.Abs(m.transform.position.x),
                                        Mathf.Abs(m.transform.position.z));
                Assert.Less(reach, AIController.PlayableHalfZ + 5.0f,
                    $"{m.name} left the world entirely");
            }
        }

        /// <summary>
        /// ⚠️ THE TAYA MUST NOT LEAVE THE BOX, EVER. Confinement is the rule the entire
        /// defensive game rests on, and it is enforced after the move rather than before, so
        /// it can only be verified by moving a body and looking.
        /// </summary>
        [UnityTest]
        public IEnumerator TheTayaIsHeldInsideTheBox()
        {
            BuildWorld();
            GameServices.Round.BeginRound();

            var taya = GameServices.Round.PlayerAt(0);
            taya.IsDefender = true;

            // Drive it hard at a corner for a while, which is where a radial clamp and a square
            // one disagree the most.
            for (int i = 0; i < 180; i++)
            {
                taya.Intent.Move = new Vector2(1.0f, 1.0f);
                taya.Intent.Set(Verb.Sprint, true);
                taya.Intent.CommitFrame();
                yield return new WaitForFixedUpdate();
            }

            float x = Mathf.Abs(taya.transform.position.x);
            float z = Mathf.Abs(taya.transform.position.z);

            Assert.LessOrEqual(x, Balance.ConfinementRadius + 0.05f,
                $"the taya escaped the box on X at {x:F2}");
            Assert.LessOrEqual(z, Balance.ConfinementRadius + 0.05f,
                $"the taya escaped the box on Z at {z:F2}");
        }

        /// <summary>
        /// ⚠️ AN ATTACKER MUST *NOT* BE CONFINED. The mirror of the test above, and the more
        /// dangerous one to get wrong: an attacker clamped into the box cannot retrieve a
        /// slipper, which deletes the loop the whole game is built on. It would also look like
        /// a movement bug rather than a rules bug.
        /// </summary>
        [UnityTest]
        public IEnumerator AnAttackerMovesFreelyThroughTheChalk()
        {
            BuildWorld();
            GameServices.Round.BeginRound();

            var attacker = GameServices.Round.PlayerAt(1);
            attacker.IsDefender = false;
            attacker.Teleport(new Vector3(0.0f, 0.0f, -Confinement.AttackerSpawnRing()));

            yield return new WaitForSeconds(0.2f);

            for (int i = 0; i < 240; i++)
            {
                attacker.Intent.Move = new Vector2(0.0f, 1.0f);
                attacker.Intent.Set(Verb.Sprint, true);
                attacker.Intent.CommitFrame();
                yield return new WaitForFixedUpdate();
            }

            Assert.Less(attacker.transform.position.z, Balance.ConfinementRadius,
                "an attacker was stopped at the chalk: confinement is being applied to the " +
                "wrong role, which deletes the retrieval loop");
        }

        /// <summary>
        /// ⚠️ SPRINT MUST COST THE BAR, AND FATIGUE MUST ARRIVE. The stamina model is asserted
        /// arithmetically in Core.Tests; this proves the MOTOR is actually driving it, which is
        /// a different claim and the one a port breaks.
        /// </summary>
        [UnityTest]
        public IEnumerator SprintingDrainsTheBarAndReachesFatigue()
        {
            BuildWorld();
            GameServices.Round.BeginRound();

            var m = GameServices.Round.PlayerAt(1);
            Assert.AreEqual(Balance.StaminaMax, m.Stamina.Current, 0.01f);

            float elapsed = 0.0f;
            while (!m.Stamina.IsFatigued && elapsed < 5.0f)
            {
                m.Intent.Move = new Vector2(1.0f, 0.0f);
                m.Intent.Set(Verb.Sprint, true);
                m.Intent.CommitFrame();
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            Assert.IsTrue(m.Stamina.IsFatigued, "sprinting never reached fatigue");
            Assert.AreEqual(Balance.StaminaMax / Balance.StaminaDrainRate, elapsed, 0.15f,
                "sprint-to-empty took the wrong amount of time on a real body");
        }

        /// <summary>
        /// ⚠️ THE WHOLE MATCH MUST ADVANCE THROUGH ALL FOUR ROUNDS AND ROTATE THE TAYA. Run at
        /// a high time scale so a six minute match fits in a test. This is the closest thing to
        /// "the game works" that can be asserted without a human.
        /// </summary>
        [UnityTest]
        public IEnumerator AWholeMatchRunsAndRotatesTheTaya()
        {
            // ⚠️ DRIVEN THROUGH SliceRunner, NOT BY CALLING StartMatch DIRECTLY. The first
            // version of this test hand-wired the directors and hung forever in round 1, which
            // was a true report of a real gap: MatchDirector and RoundDirector do not talk to
            // each other on their own. Something has to begin the round when one starts, and
            // advance the match when the intermission ends. That wirer is the bootstrap, and a
            // test that skips it is testing an arrangement that never ships.
            BuildWorld(withAi: true);

            var runner = _root.AddComponent<SliceRunner>();
            runner.Lata = GameServices.Round.Lata;
            runner.Seats = SeatArray();
            runner.Slippers = new Slipper[0];
            runner.AutoStart = false;

            var seen = new bool[Balance.PlayerCount];
            GameServices.Match.RoundStarted += (round, defender) => seen[defender] = true;

            Time.timeScale = 60.0f;
            runner.Begin();

            float guard = 0.0f;
            while (GameServices.Match.MatchInProgress && guard < 90.0f)
            {
                guard += Time.unscaledDeltaTime;
                yield return null;
            }

            Time.timeScale = 1.0f;

            Assert.IsFalse(GameServices.Match.MatchInProgress,
                "the match never ended: a round boundary is not advancing");

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
                Assert.IsTrue(seen[slot], $"seat {slot} never defended: the rotation is broken");
        }

        private CharacterMotor[] SeatArray()
        {
            var seats = new CharacterMotor[Balance.PlayerCount];
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
                seats[slot] = GameServices.Round.PlayerAt(slot);
            return seats;
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ withAi MUST BE FALSE FOR ANY TEST THAT DRIVES INPUT ITSELF, and the first run
        /// of this suite proved why. The AI writes the SAME intent table a test writes, which
        /// is the whole point of the indirection, so an AIController on the seat overwrites
        /// the test's own presses every Update and the test measures the bot instead of what
        /// it asked for. It shows up as "sprinting never reached fatigue" while the stamina
        /// model is perfectly correct.
        ///
        /// That is the indirection working exactly as designed. It just means a test has to
        /// pick: drive the seat, or let the bot drive it.
        /// </summary>
        private void BuildWorld(bool withAi = false)
        {
            _root = new GameObject("TestWorld");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.SetParent(_root.transform);
            ground.transform.localScale = Vector3.one * 6.0f;

            var lataGo = new GameObject("Lata");
            lataGo.transform.SetParent(_root.transform);
            var lata = lataGo.AddComponent<Lata>();

            GameServices.Round.Clear();
            GameServices.Round.Lata = lata;

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var go = new GameObject($"Seat{slot}");
                go.transform.SetParent(_root.transform);

                var cc = go.AddComponent<CharacterController>();
                cc.height = 1.6f;
                cc.radius = 0.35f;
                cc.center = new Vector3(0, 0.8f, 0);

                var m = go.AddComponent<CharacterMotor>();
                m.PlayerSlot = slot;
                m.CharacterIndex = slot;
                m.IsDefender = slot == 0;

                go.AddComponent<Carrier>();
                go.AddComponent<CombatVerbs>();
                if (withAi) go.AddComponent<AIController>();

                float ring = Confinement.AttackerSpawnRing();
                go.transform.position = slot == 0
                    ? new Vector3(0, 0.1f, -Balance.DefenderStartOffset)
                    : new Vector3(ring * (slot - 2), 0.1f, ring);

                GameServices.Round.Register(m);
            }
        }
    }
}
