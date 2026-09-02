using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Measures every arena's walls the way the game measures them, and asserts the one thing
    /// that has to be true of all three: a player, a bot and a tsinelas may not be clamped to a
    /// box the map has no floor for, and may not be let out of the one it has.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THE MEASUREMENT COULD ONLY EVER GROW. 🧑 2026-08-29: *"out of
    /// bounds sa ilalim ng tulay map"* and *"ilalim ng tulay map appears to have broken boundary
    /// logic"*. `MatchInstaller` seeded the search with Eskinita's 8.6 and 13.0 and then took
    /// `Mathf.Max` over the wall colliders, so no map could ever measure NARROWER than Eskinita.
    /// Ilalim ng Tulay is a road under a viaduct and is exactly that, so it inherited a street of
    /// houses' walls and everything the numbers clamp was free to walk past its own.
    ///
    /// ⚠️ NOTHING ELSE IN THE SUITE LOOKED AT THIS. `BotBehaviourProbe` runs on both arenas and
    /// reports what the bots DID, which is a liveness floor rather than a boundary; it caught one
    /// slipper at x = 9.28 once, and that was read as a slipper bug rather than as the arena
    /// being measured wrongly. A number that is only ever wrong by being too generous produces no
    /// exception, no log line and no visibly stuck bot: it produces a body somewhere the camera
    /// still draws.
    ///
    /// ⚠️ THE ASSERTIONS ARE RELATIONS, NOT TRANSCRIBED NUMBERS. Writing "Ilalim is 7.4" here
    /// would pin this test to today's geometry, and `tools/maps/build_*.py` emit the arenas
    /// WHOLESALE: the next layout run would make this file wrong rather than make it fail
    /// usefully. What must hold whatever the art does is that the box the round is played in
    /// fits inside the walls, with room for the body that stands on the throwing line.
    /// </summary>
    public class ArenaBoundsProbe
    {
        private const string OutPath = "Logs/arena-bounds.txt";

        private static readonly string[] Arenas = { "Eskinita", "BayanPlaza", "IlalimNgTulay" };

        /// <summary>
        /// The furthest out any RULE puts a body: the taya's box corner is the confinement
        /// radius, and an attacker stands off it to throw.
        ///
        /// ⚠️ `AIController.PlayableHalfX`'s own note states the relation this asserts:
        /// "ConfinementRadius + ThrowStandoff + a capsule &lt;= wall face", and records that two
        /// of the three numbers live in files the radius does not. This is that sentence as a
        /// test, which is the only form of it that survives somebody moving one of them.
        /// </summary>
        private static float MinimumUsableHalfWidth => Confinement.ThrowingLine();

        [UnityTest]
        public IEnumerator EveryArenaMeasuresItsOwnWalls()
        {
            var report = new StringBuilder();
            var bad = new List<string>();
            var seen = new Dictionary<string, Vector2>();

            foreach (string arena in Arenas)
            {
                if (!Application.CanStreamedLevelBeLoaded(arena))
                {
                    report.AppendLine($"{arena}: NOT IN BUILD SETTINGS");
                    bad.Add($"{arena} is not in the build settings");
                    continue;
                }

                // ⚠️ THE ARENA IS LOADED TO BE MEASURED, NOT PLAYED. `MatchInstaller.Start`
                // spawns four bodies, the can and the slippers the instant a map scene loads;
                // `PreviewOnly` is the flag the setup screen already uses for the same reason.
                MatchInstaller.PreviewOnly = true;

                var load = SceneManager.LoadSceneAsync(arena, LoadSceneMode.Single);
                yield return ProbeWait.Done(load, "scene load");

                for (int i = 0; i < 10; i++) yield return null;

                // Measured through the shipped path, not a copy of it. A probe that reimplements
                // the arithmetic it is checking proves only that it agrees with itself.
                MatchInstaller.MeasurePlayableBounds();

                // ⚠️ EVERY COLLIDER IS NAMED IN THE REPORT, because `Mathf.Min` means ONE stray
                // node parented under `Bounds` that is not a wall silently shrinks the whole
                // arena, and the number alone cannot tell that from a genuinely narrow map.
                var boundsNode = GameObject.Find("Bounds");
                if (boundsNode != null)
                {
                    foreach (var col in boundsNode.GetComponentsInChildren<BoxCollider>())
                    {
                        float f = MatchInstaller.WallFace(col.bounds, out bool cx);
                        report.AppendLine(
                            $"      {col.name}: centre {col.bounds.center} extents {col.bounds.extents} "
                            + $"-> {(f < 0.0f ? "not a wall" : (cx ? "X" : "Z") + $" face {f:0.00}")}");
                    }
                }

                float halfX = AIController.PlayableHalfX;
                float halfZ = AIController.PlayableHalfZ;
                seen[arena] = new Vector2(halfX, halfZ);

                float ceiling = AIController.PlayableCeilingY;

                report.AppendLine($"{arena}: halfX {halfX:0.00}  halfZ {halfZ:0.00}  " +
                                  $"ceiling {ceiling:0.00}  " +
                                  $"(needs at least {MinimumUsableHalfWidth:0.00})");

                // ⚠️ NO ARENA GETS A LOW LID. 🧑: *"give reasonable high ceilings in all maps"*.
                // Ilalim ng Tulay's walls are 3.0 in half height, so its own wall tops would have
                // put an invisible roof at 6 m over the one map with the lowest scenery.
                if (ceiling < MatchInstaller.MinimumCeilingY)
                {
                    bad.Add($"{arena}: ceiling {ceiling:0.00} is below the " +
                            $"{MatchInstaller.MinimumCeilingY:0.00} floor every map is owed");
                }

                if (halfX < MinimumUsableHalfWidth)
                {
                    bad.Add($"{arena}: halfX {halfX:0.00} is inside the throwing line " +
                            $"{MinimumUsableHalfWidth:0.00}; an attacker cannot stand where the " +
                            "rules put them");
                }

                if (halfZ < MinimumUsableHalfWidth)
                {
                    bad.Add($"{arena}: halfZ {halfZ:0.00} is inside the throwing line " +
                            $"{MinimumUsableHalfWidth:0.00}; an attacker cannot stand where the " +
                            "rules put them");
                }

                MatchInstaller.PreviewOnly = false;
            }

            // ⚠️⚠️ THE ARENAS MUST NOT ALL MEASURE THE SAME, and this is the assertion that would
            // have caught the original fault on the day it shipped. `Mathf.Max` seeded with
            // Eskinita's numbers returns Eskinita's numbers for every map at least as narrow as
            // Eskinita, so the symptom of the bug is three identical rows. Two arenas that
            // genuinely share a width would trip this, and that is the right cost: it is one line
            // to record why, and the alternative is a measurement nobody can tell from a
            // constant.
            if (seen.Count == Arenas.Length)
            {
                var values = new HashSet<Vector2>(seen.Values);

                if (values.Count == 1)
                {
                    bad.Add("every arena measured identically " +
                            $"({seen[Arenas[0]].x:0.00} x {seen[Arenas[0]].y:0.00}), which is what " +
                            "a measurement that cannot shrink looks like. See MeasurePlayableBounds.");
                }
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText(OutPath, report.ToString(), new UTF8Encoding(false));
            Debug.Log(report.ToString());

            Assert.IsEmpty(bad, "arena bounds:\n" + string.Join("\n", bad));
        }

        /// <summary>
        /// ⚠️ THE FACE, NOT THE CENTRE, and the thin axis is the one a wall stops you on. Both
        /// halves were wrong in the shipped measurement and neither is visible from a scene view,
        /// so they are asserted here on geometry this test owns rather than on a map that a
        /// layout run can change underneath it.
        /// </summary>
        [Test]
        public void AWallIsMeasuredAtItsInwardFaceOnTheAxisItIsThinAcross()
        {
            // A wall 12 m long down Z, 0.5 m thick, centred 8.0 m out in X. Its face is at 7.75.
            var alongZ = Box(new Vector3(8.0f, 3.0f, 0.0f), new Vector3(0.25f, 3.0f, 12.0f));

            float face = MatchInstaller.WallFace(alongZ, out bool constrainsX);

            Assert.IsTrue(constrainsX, "a wall running down Z walls off the X axis");
            Assert.AreEqual(7.75f, face, 0.001f,
                "the playable edge is the wall's inward FACE, which is half its thickness inside "
                + "its centre. Measuring the centre is worth half a wall of extra room on every "
                + "map, in the direction that lets a body out.");

            // The same wall turned across the street.
            var alongX = Box(new Vector3(0.0f, 3.0f, -13.0f), new Vector3(12.0f, 3.0f, 0.25f));

            face = MatchInstaller.WallFace(alongX, out constrainsX);

            Assert.IsFalse(constrainsX, "a wall running down X walls off the Z axis");
            Assert.AreEqual(12.75f, face, 0.001f);
        }

        /// <summary>
        /// ⚠️ A COLLIDER ACROSS THE MIDDLE IS AN OBSTACLE, NOT AN EDGE. Ilalim ng Tulay's viaduct
        /// piers stand in the play area; counting one as a wall would clamp the whole arena to the
        /// pier. `Slipper.BounceOffObstacles` is what handles those.
        /// </summary>
        /// <summary>
        /// ⚠️⚠️ A KERB IS FLOOR, AND MISTAKING ONE FOR A WALL COSTS ILALIM NG TULAY BOTH ITS
        /// PAVEMENTS. That map parents its ground under `Bounds`, so the tightest box thin across
        /// X is a 0.18 m kerb at x = 6.83, and clamping there fences the round onto the tarmac
        /// while the pavement either side is floor the game is played on. The real walls are at
        /// 11.0. Height is the only property that separates the two without matching on a node
        /// name that a map rebuild is free to change.
        /// </summary>
        [Test]
        public void AKerbIsFloorRatherThanAWall()
        {
            // Ilalim ng Tulay's KerbWest, transcribed from the probe's own report.
            var kerb = Box(new Vector3(-6.83f, -0.10f, 0.0f),
                           new Vector3(0.18f, 0.25f, 24.0f));

            Assert.Less(MatchInstaller.WallFace(kerb, out _), 0.0f,
                "a 0.25 m kerb is stepped over, not stopped at");

            // Its WallWest, from the same report.
            var wall = Box(new Vector3(-11.20f, 3.0f, 0.0f),
                           new Vector3(0.20f, 3.0f, 24.0f));

            Assert.AreEqual(11.0f, MatchInstaller.WallFace(wall, out bool constrainsX), 0.001f);
            Assert.IsTrue(constrainsX);
        }

        [Test]
        public void AColliderNearTheMiddleIsNotAWall()
        {
            var pier = Box(new Vector3(0.4f, 2.0f, 0.0f), new Vector3(0.5f, 2.0f, 0.5f));

            Assert.Less(MatchInstaller.WallFace(pier, out _), 0.0f);
        }

        /// <summary>
        /// A box from its centre and its EXTENTS, which is what `Collider.bounds` reports and
        /// what `MatchInstaller.WallFace` reads.
        ///
        /// ⚠️ `new Bounds(centre, size)` TAKES THE FULL SIZE, NOT THE HALF, so writing the
        /// probe's own report values straight into it silently halves every box. That is how the
        /// first version of these tests asserted a face of 11.00 against a wall they had in fact
        /// described as half as thick, and the assertion failed for a reason that had nothing to
        /// do with the code under test. The report prints extents, so the helper takes extents.
        /// </summary>
        private static Bounds Box(Vector3 centre, Vector3 extents)
            => new Bounds(centre, extents * 2.0f);

    }
}
