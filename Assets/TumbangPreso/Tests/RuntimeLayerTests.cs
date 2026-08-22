using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// Tests for the parts of the port that CANNOT live in Core.Tests, because they need
    /// UnityEngine types.
    ///
    /// ⚠️ EVERYTHING THAT CAN BE ASSERTED WITHOUT UNITY BELONGS IN Core.Tests INSTEAD. Those
    /// run in 89 ms from a terminal with no editor involved. Only put a test here when it
    /// genuinely needs a GameObject, a Transform, or a MonoBehaviour lifecycle: this suite is
    /// orders of magnitude slower to run and correspondingly less likely to be run.
    /// </summary>
    public class RuntimeLayerTests
    {
        // -------------------------------------------------------------------
        // InputIntent: the shared human/AI table.
        // -------------------------------------------------------------------

        [Test]
        public void Intent_DerivesEdgesFromTheCommittedFrame()
        {
            var i = new InputIntent();

            i.Set(Verb.Grab, true);
            Assert.IsTrue(i.Pressed(Verb.Grab));
            Assert.IsTrue(i.JustPressed(Verb.Grab), "first frame held is a press edge");

            i.CommitFrame();
            Assert.IsTrue(i.Pressed(Verb.Grab));
            Assert.IsFalse(i.JustPressed(Verb.Grab), "still held is not a new press");

            i.Set(Verb.Grab, false);
            Assert.IsTrue(i.JustReleased(Verb.Grab));

            i.CommitFrame();
            Assert.IsFalse(i.JustReleased(Verb.Grab));
        }

        /// <summary>
        /// ⚠️ PARKED IS NOT THE SAME AS NO INPUT. A verb held across a park boundary must read
        /// as released, or the player walks out of the pause menu already sprinting.
        /// </summary>
        [Test]
        public void Intent_ParkedReportsEverythingReleased()
        {
            var i = new InputIntent();
            i.Set(Verb.Sprint, true);
            i.Move = new Vector2(1.0f, 0.0f);
            i.CommitFrame();

            i.Parked = true;

            Assert.IsFalse(i.Pressed(Verb.Sprint));
            Assert.IsFalse(i.JustPressed(Verb.Sprint));
            Assert.AreEqual(Vector2.zero, i.MoveAxis);
        }

        // -------------------------------------------------------------------
        // Confinement, through a real Transform.
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ THE TAYA IS CLAMPED IN; EVERYONE ELSE IS MERELY IN DANGER. An attacker clamped by
        /// mistake cannot retrieve a slipper at all, which deletes the game.
        /// </summary>
        [Test]
        public void OnlyTheDefenderIsConfined()
        {
            Assert.IsTrue(Confinement.IsConfined(roundActive: true, isDefender: true));
            Assert.IsFalse(Confinement.IsConfined(roundActive: true, isDefender: false));

            // ⚠️ AND NOBODY IS CONFINED WHILE THE ROUND IS NOT LIVE, or a taya is trapped in
            // the box through the intermission and cannot walk to their next mark.
            Assert.IsFalse(Confinement.IsConfined(roundActive: false, isDefender: true));
        }

        [Test]
        public void ClampKeepsTheCornerReachable()
        {
            // The corner of the square is legal ground for the taya. A radial clamp would
            // pull them off it, and the corner is exactly where a taya stands to cover one.
            float x = Balance.ConfinementRadius - 0.01f;
            float z = Balance.ConfinementRadius - 0.01f;
            float ox = x, oz = z;

            Confinement.ClampToBox(ref x, ref z);

            Assert.AreEqual(ox, x, 0.0001f, "the corner must survive the clamp untouched");
            Assert.AreEqual(oz, z, 0.0001f);
            Assert.IsTrue(Confinement.IsInsideBox(x, z));
        }

        // -------------------------------------------------------------------
        // Scoring, through the director.
        // -------------------------------------------------------------------

        [Test]
        public void Scoreboard_AccumulatesAcrossRoundsAndReportsDraws()
        {
            var board = new Scoreboard();

            board.Add(0, ScoreEvent.LataKnocked);
            board.Add(0, ScoreEvent.DefenseTick);
            Assert.AreEqual(110, board[0]);

            board.Add(1, ScoreEvent.Tag);
            board.Add(1, ScoreEvent.DefenseTick);
            Assert.AreEqual(-1, board.WinningSlot(), "an exact tie is an honest draw");

            board.Add(1, ScoreEvent.Sabotage);
            Assert.AreEqual(1, board.WinningSlot());
        }

        /// <summary>
        /// ⚠️ AN OUT-OF-RANGE SLOT MUST NOT THROW AND MUST NOT SCORE. This is read on paths fed
        /// by a replicated int, where -1 is a legitimate "no seat" value.
        /// </summary>
        [Test]
        public void Scoreboard_IgnoresSlotsThatDoNotExist()
        {
            var board = new Scoreboard();

            Assert.DoesNotThrow(() => board.Add(-1, ScoreEvent.Tag));
            Assert.DoesNotThrow(() => board.Add(99, ScoreEvent.Tag));
            Assert.AreEqual(0, board.Total);
            Assert.AreEqual(0, board[-1]);
        }

        // -------------------------------------------------------------------
        // The spawn ring, which is derived rather than authored.
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ AN ATTACKER SPAWNED INSIDE THE BOX IS VULNERABLE ON FRAME ONE, and it reads as a
        /// rules bug rather than as the placement bug it is. That is precisely why spawns are
        /// computed from the box instead of read from map markers.
        /// </summary>
        [Test]
        public void EverySpawnRingPointIsOutsideTheBox()
        {
            float ring = Confinement.AttackerSpawnRing();

            for (int i = 0; i < 360; i += 5)
            {
                float a = i * Mathf.Deg2Rad;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                float scale = 1.0f / Mathf.Max(Mathf.Abs(c), Mathf.Abs(s));

                float x = c * ring * scale;
                float z = s * ring * scale;

                Assert.IsFalse(Confinement.IsInsideBox(x, z),
                    $"spawn at {i} degrees ({x:F2}, {z:F2}) is inside the box");
            }
        }

        /// <summary>
        /// ⚠️ THE THROWING LINE, NOT THE BOX, IS WHAT HAS TO FIT THE MAP, and there is a THIRD
        /// bound past it that nobody had written down until bots jammed against a wall: the
        /// AI's standoff ring has to fit inside the wall faces too.
        /// </summary>
        [Test]
        public void TheStandoffRingFitsInsideThePlayableArea()
        {
            const float throwStandoff = 1.2f;
            const float capsuleRadius = 0.4f;

            float ring = Balance.ConfinementRadius + throwStandoff + capsuleRadius;

            Assert.LessOrEqual(ring, AIController.PlayableHalfX,
                "the standoff ring lands inside a wall: bots will jam against it trying to " +
                "reach a goal they can never stand on, and it reads as broken pathfinding");
        }

        // -------------------------------------------------------------------
        // Hero Ability System & Gamemode Tests
        // -------------------------------------------------------------------

        [Test]
        public void HeroKits_CreateSuccessfully_ForEveryHero()
        {
            string[] heroes = { "zack", "cheska", "dante", "nemu", "sean" };
            foreach (var h in heroes)
            {
                var kit = Abilities.HeroAbilitySystem.CreateKitFor(h);
                Assert.IsNotNull(kit, $"kit for {h} must be created");
                Assert.IsNotNull(kit.Skill1, $"skill 1 for {h} must exist");
                Assert.IsNotNull(kit.Skill2, $"skill 2 for {h} must exist");
                Assert.IsNotNull(kit.Ultimate, $"ultimate for {h} must exist");
            }
        }

        [Test]
        public void HeroKit_ChargesAndActivates_Ultimate()
        {
            var kit = new Abilities.ZackHeroKit();
            Assert.AreEqual(0.0f, kit.UltimateCharge);
            Assert.IsFalse(kit.IsUltimateReady);

            kit.AddUltimateCharge(50.0f);
            Assert.AreEqual(0.5f, kit.UltimateRatio, 0.001f);
            Assert.IsFalse(kit.IsUltimateReady);

            kit.AddUltimateCharge(50.0f);
            Assert.AreEqual(1.0f, kit.UltimateRatio, 0.001f);
            Assert.IsTrue(kit.IsUltimateReady);
        }

        [Test]
        public void GameMode_Rosters_AreDistinctAndCorrectSizes()
        {
            var classic = Roster.GetPeople(GameMode.Classic);
            var heroes = Roster.GetPeople(GameMode.HeroStrike);

            Assert.AreEqual(12, classic.Count);
            Assert.AreEqual(5, heroes.Count);
            Assert.AreEqual("bayan", classic[0].Id);
            Assert.AreEqual("dante", heroes[0].Id);
        }
    }
}
