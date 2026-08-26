using System.Collections;
using NUnit.Framework;
using TumbangPreso;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        [UnityTest]
        public IEnumerator RestoredLataRejectsAnAlreadyAirborneFollowUpDuringProtection()
        {
            _root = new GameObject("RestoreProtectionWorld");
            var lata = _root.AddComponent<Lata>();
            yield return null;

            lata.HostKnockDown(-1);
            Assert.IsFalse(lata.IsUpright, "the setup knockdown did not take");

            lata.HostRestore();
            Assert.IsTrue(lata.IsUpright);
            Assert.IsTrue(lata.IsProtected,
                "restoring the lata did not open its visible protection window");

            lata.HostKnockDown(-1);
            Assert.IsTrue(lata.IsUpright,
                "an impact already in flight knocked the lata down through restore protection");

            yield return new WaitForSeconds(Balance.ThrowRestoreCooldown + 0.1f);
            Assert.IsFalse(lata.IsProtected, "restore protection never expired");

            lata.HostKnockDown(-1);
            Assert.IsFalse(lata.IsUpright,
                "the lata stayed invulnerable after the authored protection window");
        }

        [UnityTest]
        public IEnumerator ACanKnockdownDoesNotCancelAnExistingThrowCommitment()
        {
            _root = new GameObject("ChargeContinuityWorld");

            var roundGo = new GameObject("Round");
            roundGo.transform.SetParent(_root.transform, false);
            var round = roundGo.AddComponent<RoundDirector>();

            var lataGo = new GameObject("Lata");
            lataGo.transform.SetParent(_root.transform, false);
            var lata = lataGo.AddComponent<Lata>();
            round.Lata = lata;

            var attackerGo = new GameObject("Attacker");
            attackerGo.transform.SetParent(_root.transform, false);
            attackerGo.transform.position = new Vector3(Balance.ConfinementRadius + 1.0f, 0.0f, 0.0f);
            attackerGo.AddComponent<CharacterController>();
            var attacker = attackerGo.AddComponent<CharacterMotor>();
            attacker.IsDefender = false;
            attacker.HoldingSlipper = true;
            round.Register(attacker);
            round.BeginRound();
            yield return null;

            Assert.IsTrue(round.CanThrow(attacker), "the attacker did not begin in a legal throw state");
            lata.HostKnockDown(-1);

            Assert.IsFalse(round.CanThrow(attacker), "a down lata still accepted a release");
            Assert.IsTrue(round.CanMaintainThrowCharge(attacker),
                "the teammate's knockdown cancelled an existing throw animation and charge");
        }

        /// <summary>
        /// ⚠️⚠️ THE CLOCK IS RECLAIMED BEFORE EVERY TEST, NOT ONLY AFTER ONE. Nothing in this
        /// class is the only thing in the run: the scene-heavy suites load a whole arena and
        /// leave it loaded, and anything in there that stops time and dies without restoring it
        /// hands the next test a `Time.timeScale` of 0. Every test below waits on
        /// `WaitForFixedUpdate`, and at a scale of 0 that yield never resumes, so the failure
        /// arrives as "sprinting never reached fatigue" against a stamina model that is
        /// perfectly correct. It failed exactly once, only in a full 48-test run, and passed
        /// every time the class was run alone: that shape IS cross-test state and nothing else.
        ///
        /// ⚠️ THE OWNERSHIP WAS ALSO REPAIRED AT THE SOURCE rather than only papered over here.
        /// `MatchResult` stopped the clock from an instance and restored it only from a button,
        /// the same lifetime fault `Hitstop`'s header documents; it now restores on OnDisable
        /// and OnDestroy. This setup stays because a guard that only holds while every writer
        /// is well behaved is not a guard.
        /// </summary>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Hitstop.End();
            Time.timeScale = 1.0f;

            // ⚠️⚠️ AND THE ARENA THE LAST SUITE LOADED IS UNLOADED, WHICH IS THE OTHER HALF.
            // This class builds its own world instead of loading a scene, and the header above
            // explains why. What it does NOT do is unload whatever scene is already open, so a
            // run that reaches here after `BotBehaviourProbe`, `HudLayoutProbe` or
            // `GameplayShots` builds four fresh seats INSIDE a live Eskinita: the previous
            // match's bots are still thinking, its hero hazards are still pulsing, and both can
            // shove and stun a body this suite is trying to measure. A stunned unit does not
            // steer, and `Stamina.Step` is handed `moving = false` while it cannot, so the
            // sprint test's bar drains in fits and reports 4.54 s against a model that is
            // exactly right. It failed that way once in a 48 test run and passed every time the
            // class was run alone, which is the signature of a neighbour rather than a bug.
            // ⚠️ A UNIQUE NAME PER TEST. `CreateScene` throws on a name that is already loaded,
            // and the teardown below deliberately refuses to unload the last remaining scene,
            // so the previous test's room can still be open when this one starts.
            var clean = SceneManager.CreateScene($"MatchRunTestWorld{++_worldCount}");
            SceneManager.SetActiveScene(clean);

            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || scene == clean) continue;

                // ⚠️ NEVER THE RUNNER'S OWN SCENE. Unloading that takes the test framework's
                // objects with it and the run dies rather than fails.
                if (scene.name.Contains("InitTestScene")) continue;

                var unload = SceneManager.UnloadSceneAsync(scene);
                while (unload != null && !unload.isDone) yield return null;
            }

            _clean = clean;
            yield return null;
        }

        private Scene _clean;
        private static int _worldCount;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            Hitstop.End();
            Time.timeScale = 1.0f;

            // The scene this test built in goes with it, so the next one starts from the same
            // empty room rather than from whatever this one left lying about.
            if (_clean.IsValid() && _clean.isLoaded && SceneManager.sceneCount > 1)
            {
                var unload = SceneManager.UnloadSceneAsync(_clean);
                while (unload != null && !unload.isDone) yield return null;
            }

            yield return null;
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
        /// ⚠️ THE WHOLE MATCH MUST ADVANCE THROUGH EVERY ROUND IN THE SELECTED MODE AND ROTATE THE TAYA. Run at
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
