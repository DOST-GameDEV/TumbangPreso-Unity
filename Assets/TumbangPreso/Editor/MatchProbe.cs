using System;
using System.Text;
using TumbangPreso.Core;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// The port of `fair_probe` / `ai_probe`: run whole matches headlessly and report the
    /// distribution of points.
    ///
    /// ⚠️⚠️ WITHOUT THIS, "THE BALANCE SURVIVED THE PORT" IS AN OPINION. Every number in
    /// Design.md was established by a probe like this one, and the ones that matter most are
    /// not single constants but SHARES measured over a whole match: what fraction of all
    /// points defence takes, how often a tag happens, whether a passive strategy dominates.
    /// No unit test can produce those, because they are emergent.
    ///
    /// ⚠️ THE GATE THAT MATTERS IS DEFENCE ≤ 50% OF ALL POINTS UNDER A TURTLING TAYA. The
    /// original measured 47.8%, deliberately close to the line, so that a future change which
    /// inflates the passive term goes RED rather than going unnoticed. Keep the gate tight for
    /// exactly that reason: a gate with slack in it is a gate that reports nothing.
    ///
    /// ⚠️ AND THE IMPOSSIBLE-NUMBER RULE APPLIES TO ITS OWN OUTPUT. If two numbers in a report
    /// cannot both be true, the harness is the bug, not the game. That rule caught real probe
    /// faults in the original more than once.
    /// </summary>
    public static class MatchProbe
    {
        private const string ResultPath = "Logs/match-probe.txt";

        [MenuItem("Tumbang Preso/Run Match Probe")]
        public static void RunFromMenu() => Run(1);

        /// <summary>
        /// Batch entry point:
        ///   Unity.exe -batchmode -quit -nographics -projectPath . \
        ///             -executeMethod TumbangPreso.EditorTools.MatchProbe.RunBatch
        /// </summary>
        public static void RunBatch()
        {
            int failures = Run(3);
            EditorApplication.Exit(failures > 0 ? 1 : 0);
        }

        private static int Run(int matches)
        {
            var sb = new StringBuilder();
            int failures = 0;

            sb.AppendLine("MATCH PROBE");
            sb.AppendLine($"classicRounds={MatchRules.RoundCountFor(GameMode.Classic)} " +
                          $"heroStrikeRounds={MatchRules.RoundCountFor(GameMode.HeroStrike)} " +
                          $"players={Balance.PlayerCount} " +
                          $"roundTime={Balance.RoundTime}");
            sb.AppendLine();

            // ⚠️ THE THEORETICAL PASSIVE CEILING IS AN ARITHMETIC FACT, NOT A MEASUREMENT, and
            // the difference is the whole of what settled this item. The arithmetic says 900 a
            // round; the probe said a taya who presses nothing collects 38 of them, because
            // the attackers put the can down and it stays down. "Uncontested" is not a state
            // this game has.
            int ceiling = (int)(Balance.RoundTime / Balance.DefenseTickInterval) * Balance.ScoreDefensePerTick;
            sb.AppendLine($"passive ceiling per round (arithmetic, not reachable): {ceiling}");

            // Structural check: every mode is made of complete role rotations. Classic gives
            // each seat one defence; Hero Strike gives each seat two.
            foreach (GameMode mode in new[] { GameMode.Classic, GameMode.HeroStrike })
            {
                int expected = MatchRules.RoundCountFor(mode) / Balance.PlayerCount;
                for (int slot = 0; slot < Balance.PlayerCount; slot++)
                {
                    int rounds = 0;
                    for (int r = 1; r <= MatchRules.RoundCountFor(mode); r++)
                        if (MatchRules.DefenderSlotFor(r) == slot) rounds++;

                    if (rounds == expected) continue;
                    sb.AppendLine($"FAIL: {mode} seat {slot} defends {rounds} times, expected {expected}.");
                    failures++;
                }
            }
            sb.AppendLine("ok  : every mode contains complete, equal defender rotations");

            // ⚠️ A LIVE MATCH RUN NEEDS PLAY MODE AND A SCENE WITH PREFABS WIRED, which does
            // not exist yet: Phase 3 has to land first. Rather than print a fabricated
            // distribution, this says plainly what it did not measure. A probe that reports
            // numbers it did not take is worse than one that reports nothing, because the
            // numbers get quoted.
            sb.AppendLine();
            sb.AppendLine("NOT MEASURED YET (needs a Play Mode scene with prefabs wired):");
            sb.AppendLine("  - DEFENSE share of all points under a turtling taya (gate: <= 50%)");
            sb.AppendLine("  - can-upright fraction per taya policy (idle / turtle / bot)");
            sb.AppendLine("  - throws and knockdowns per match");
            sb.AppendLine("  - tag count, and sabotage frequency (was 0 in every original run)");
            sb.AppendLine($"  - requested matches: {matches}");

            sb.AppendLine();
            sb.AppendLine(failures > 0 ? $"RESULT: FAILED ({failures})" : "RESULT: OK (structural only)");

            try
            {
                System.IO.Directory.CreateDirectory("Logs");
                System.IO.File.WriteAllText(ResultPath, sb.ToString());
            }
            catch (Exception e)
            {
                Debug.LogError($"[MatchProbe] could not write {ResultPath}: {e.Message}");
            }

            Debug.Log(sb.ToString());
            return failures;
        }
    }
}
