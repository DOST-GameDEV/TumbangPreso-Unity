using System;
using System.IO;
using System.Text;
using TumbangPreso.Core;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// The replacement for `floorcheck.py`: refuse to ship an arena whose geometry disagrees
    /// with the rules.
    ///
    /// ⚠️⚠️ THE ORIGINAL COUPLING CANNOT SURVIVE AND MUST BE REPLACED, NOT DROPPED.
    /// `floorcheck.py` REGEXED `CONFINEMENT_RADIUS` straight out of `character_base.gd`, and
    /// both map builders drew the chalk from it. That is ugly and it was exactly right: it
    /// meant the chalk could not drift from the rule. Here the constant lives in
    /// `TumbangPreso.Core.Balance`, so this reads it directly, which is the same guarantee
    /// without the regex.
    ///
    /// ⚠️ IT ABORTS RATHER THAN WARNS, deliberately, and the original did too. An arena that
    /// is subtly wrong does not look like a bounds problem when it fails. It looked like
    /// broken pathfinding: bots reported as "walking up the houses" were pinned against a wall
    /// trying to reach a standoff point that lay inside it, and throws over a whole match went
    /// 14 to 59 and knockdowns 5 to 23 once the ring fitted again. Most of the offence in the
    /// game was being suppressed by a bound nobody had written down.
    /// </summary>
    public static class ArenaCheck
    {
        /// <summary>Where an attacker stands to throw, relative to the chalk.</summary>
        public const float ThrowStandoff = 1.2f;

        /// <summary>Body radius allowance for the standoff check.</summary>
        public const float CapsuleRadius = 0.4f;

        private const string ResultPath = "Logs/arena-check.txt";

        [MenuItem("Tumbang Preso/Check Arena Bounds")]
        public static void RunFromMenu() => Execute(AIController.PlayableHalfX, AIController.PlayableHalfZ);

        public static void Run() =>
            EditorApplication.Exit(Execute(AIController.PlayableHalfX, AIController.PlayableHalfZ) ? 0 : 1);

        /// <summary>
        /// All three bounds. ⚠️ THE THIRD ONE IS THE ONE NOBODY HAD WRITTEN DOWN, and it is
        /// the reason this function exists rather than a comment.
        /// </summary>
        public static bool Execute(float wallHalfX, float wallHalfZ)
        {
            var sb = new StringBuilder();
            int failures = 0;

            float r = Balance.ConfinementRadius;
            float throwLine = Confinement.ThrowingLine();
            float spawnRing = Confinement.AttackerSpawnRing();
            float standoff = r + ThrowStandoff + CapsuleRadius;

            sb.AppendLine("ARENA CHECK");
            sb.AppendLine($"  confinement radius : {r:F2}   (a SQUARE, |x| = |z| = r)");
            sb.AppendLine($"  throwing line      : {throwLine:F2}");
            sb.AppendLine($"  attacker spawn ring: {spawnRing:F2}");
            sb.AppendLine($"  AI standoff + body : {standoff:F2}");
            sb.AppendLine($"  wall faces         : x +/-{wallHalfX:F2}, z +/-{wallHalfZ:F2}");
            sb.AppendLine();

            // BOUND 1. The throwing line must sit outside the chalk, or an attacker standing
            // legally still cannot throw.
            if (throwLine <= r)
            {
                sb.AppendLine($"FAIL bound 1: the throwing line ({throwLine:F2}) is not outside " +
                              $"the chalk ({r:F2}). An attacker on the line would be inside the " +
                              "box, which is where throwing is refused.");
                failures++;
            }
            else sb.AppendLine("ok  bound 1: the throwing line sits outside the chalk");

            // BOUND 2. Every legal throwing position must be able to REACH the can, checked
            // against the slowest slipper rather than the average.
            float worstRange = float.MaxValue;
            string worstName = "?";
            for (int i = 0; i < Roster.Slippers.Count; i++)
            {
                float range = ThrowRules.MaxRange(Roster.SlipperLaunchSpeed(i));
                if (range >= worstRange) continue;

                worstRange = range;
                worstName = Roster.Slippers[i].Name;
            }

            if (worstRange <= throwLine)
            {
                sb.AppendLine($"FAIL bound 2: {worstName} reaches {worstRange:F2} m, short of the " +
                              $"throwing line at {throwLine:F2}. The slowest slipper must be able " +
                              "to reach the can from the nearest legal position.");
                failures++;
            }
            else
            {
                sb.AppendLine($"ok  bound 2: the slowest slipper ({worstName}) reaches " +
                              $"{worstRange:F2} m against a {throwLine:F2} m line");
            }

            // ⚠️⚠️ BOUND 3. THE ONE THAT WAS NEVER WRITTEN DOWN. The AI sends every attacker to
            // a square ring at radius + standoff. If that ring lands inside a wall face, the
            // goal is unreachable and the bot jams against the wall trying to get there.
            if (standoff > wallHalfX)
            {
                sb.AppendLine($"FAIL bound 3: the AI standoff ring reaches {standoff:F2} against a " +
                              $"wall face at {wallHalfX:F2}. Bots will be sent to a point they can " +
                              "never stand on and will pin against the wall. THIS DOES NOT LOOK " +
                              "LIKE A BOUNDS BUG. It gets reported as broken pathfinding, and it " +
                              "suppresses most of the offence in the match.");
                failures++;
            }
            else
            {
                sb.AppendLine($"ok  bound 3: the standoff ring ({standoff:F2}) fits inside the wall " +
                              $"face ({wallHalfX:F2})");
            }

            // BOUND 4. The spawn ring has to be on the map at all.
            if (spawnRing > wallHalfZ)
            {
                sb.AppendLine($"FAIL bound 4: the attacker spawn ring ({spawnRing:F2}) is outside " +
                              $"the map on Z ({wallHalfZ:F2}).");
                failures++;
            }
            else sb.AppendLine($"ok  bound 4: the spawn ring fits on Z");

            // BOUND 5. Spawns must be OUTSIDE the box, or an attacker is vulnerable on frame one.
            if (Confinement.IsInsideBox(spawnRing, 0.0f))
            {
                sb.AppendLine("FAIL bound 5: the spawn ring is inside the box. Attackers would be " +
                              "VULNERABLE on frame one, which reads as a rules bug rather than as " +
                              "the placement bug it is.");
                failures++;
            }
            else sb.AppendLine("ok  bound 5: attackers spawn outside the box");

            sb.AppendLine();
            sb.AppendLine(failures > 0
                ? $"RESULT: ABORT. {failures} bound(s) violated. Do not ship this arena."
                : "RESULT: OK. All five bounds hold.");

            try
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText(ResultPath, sb.ToString());
            }
            catch (Exception e)
            {
                Debug.LogError($"[ArenaCheck] could not write {ResultPath}: {e.Message}");
            }

            if (failures > 0) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());

            return failures == 0;
        }
    }
}
