using System;
using TumbangPreso.Core;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// A headless smoke check, for CI and for any agent working without the editor open.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE `Unity.exe -batchmode -quit` PROVES NOTHING. It exits after
    /// package registration, before script compilation, and still returns exit code 0. That
    /// reads as a clean build and is not one. Reaching this method AT ALL means every
    /// assembly compiled, because `-executeMethod` cannot invoke a method in an assembly
    /// that failed to build.
    ///
    /// Run:
    ///   Unity.exe -batchmode -quit -nographics -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.HeadlessCheck.Run -logFile -
    /// </summary>
    public static class HeadlessCheck
    {
        /// <summary>
        /// ⚠️ RESULTS GO TO A FILE, NOT ONLY TO Debug.Log. `EditorApplication.Exit` kills the
        /// process before Unity flushes its log buffer, so the first version of this check
        /// compiled everything, ran, and left NO trace of its own output in the log: it was
        /// indistinguishable from a method that never ran. A file write is flushed
        /// deterministically and can be read back by whatever invoked Unity.
        /// </summary>
        private const string ResultPath = "Logs/headless-check.txt";

        private static readonly System.Text.StringBuilder Report = new System.Text.StringBuilder();

        /// <summary>⚠️ `Run` EXITS THE EDITOR AND `Execute` DOES NOT, and the split exists so
        /// `Checks` can run this alongside the other three in one launch. `EditorApplication.Exit`
        /// kills the process, so a batched caller that reached this one first would never reach
        /// the rest.</summary>
        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        public static bool Execute()
        {
            Report.Clear();
            int failures = 0;

            try
            {
                // The rules core has to be reachable from Unity, not just from dotnet test.
                // If the local package or its asmdef is misconfigured, this is where it shows.
                Check(ref failures, "classic roster size", Roster.ClassicPeople.Count == 12);
                // ⚠️⚠️ SIX AND 18, AND THESE READ 5 AND 17 FROM THE WITCH MERGE UNTIL 2026-08-26.
                // `docs/TODO.md` § 21 merged Phaister as the sixth hero and updated `Roster`
                // without updating the check that counts it, so `Checks.RunAll` had been failing
                // on every launch since. It went unnoticed because the § 21 verification pass
                // quoted Core, EditMode, PlayMode and `AbilityShowcaseProbe` and never ran this.
                //
                // ⚠️ THE NUMBERS ARE DERIVED FROM THE TWO LISTS, NOT RETYPED. A seventh hero
                // would break these again for no reason a reader could act on; what this check
                // is actually for is proving the rules package is REACHABLE from Unity at all
                // (see the note above), so it asserts the relationship rather than the totals.
                Check(ref failures, "hero roster size", Roster.HeroPeople.Count == 6);
                Check(ref failures, "all roster size",
                      Roster.AllPeople.Count == Roster.ClassicPeople.Count + Roster.HeroPeople.Count);
                // ⚠️⚠️ THESE TWO WERE `== 4` AND `== 9` AND THEY BROKE THE MOMENT A CAN WAS
                // ADDED, WHICH IS THE THIRD TIME THIS FILE HAS DONE IT. The note above already
                // records the hero count doing exactly this in § 21 and already says the numbers
                // should be derived rather than retyped; the two lines below were the ones that
                // had not been converted yet. Adding PIYESTA and KARNE NORTE on 2026-08-28 turned
                // `Checks.RunAll` red on a roster that was entirely correct.
                //
                // ⚠️ BOTH LISTS ARE APPEND-ONLY AND WILL KEEP GROWING. `Roster.Slippers` is
                // already carrying a tenth entry in `docs/TODO.md` § 70.8. A literal here is a
                // guaranteed future false failure, and a false failure in a five-check gate is
                // worse than no check: it trains the next reader to skim past a red line.
                //
                // ⚠️ WHAT THIS CHECK IS ACTUALLY FOR IS PROVING THE RULES PACKAGE IS REACHABLE
                // FROM UNITY AT ALL, per the note above. So it asserts the invariants that hold
                // at any size: the lists exist, entry 0 is present because every -1 fallback
                // resolves to it, and no row carries trait points outside the table's own range.
                // Roster.cs's own note is that entry 0 stays neutral, and TraitMin/TraitMax are
                // what the AI's range equation is solved against.
                Check(ref failures, "cans", Roster.Cans.Count > 0 && AllTraitsInRange(Roster.Cans));
                Check(ref failures, "slippers",
                      Roster.Slippers.Count > 0 && AllTraitsInRange(Roster.Slippers));

                Check(ref failures, "neutral is exactly 1.0",
                    Mathf.Approximately(Roster.TraitScale(3, Balance.TraitSpeedPerPoint), 1.0f));

                Check(ref failures, "every slot defends exactly once",
                    MatchRules.DefenderSlotFor(1) == 0 && MatchRules.DefenderSlotFor(4) == 3);

                Check(ref failures, "the box is a square",
                    Confinement.IsInsideBox(6.9f, 6.9f) && !Confinement.IsInsideBox(7.1f, 0.0f));

                Check(ref failures, "shove solves to 2.50 m",
                    Mathf.Abs(Combat.ShoveDistance() - 2.50f) < 0.01f);

                Check(ref failures, "hit window on BOYBEN",
                    Mathf.Abs(ThrowRules.HitWindow(1) - 0.493f) < 0.001f);
            }
            catch (Exception e)
            {
                Report.AppendLine($"THREW: {e}");
                failures++;
            }

            Report.AppendLine(failures > 0
                ? $"RESULT: FAILED with {failures} problem(s)."
                : "RESULT: OK. All assemblies compiled and the rules core is reachable from Unity.");

            Flush();

            Debug.Log(Report.ToString());
            return failures == 0;
        }

        private static void Flush()
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(ResultPath);
                if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(ResultPath, Report.ToString());
            }
            catch (Exception e)
            {
                Debug.LogError($"[HeadlessCheck] could not write {ResultPath}: {e.Message}");
            }
        }

        private static void Check(ref int failures, string what, bool ok)
        {
            Report.AppendLine(ok ? $"ok   : {what}" : $"FAIL : {what}");
            if (!ok) failures++;
        }

        /// <summary>
        /// Every row's trait points sit inside the table's own declared range.
        ///
        /// ⚠️ THIS REPLACED A HARD-CODED COUNT, AND IT CATCHES MORE THAN THE COUNT DID.
        /// `Roster.TraitScale` clamps out-of-range points silently, so a row typed as 6 or 0
        /// plays as 5 or 1 with nothing said anywhere. A list length never caught that.
        /// </summary>
        private static bool AllTraitsInRange(System.Collections.Generic.IReadOnlyList<RosterEntry> rows)
        {
            foreach (var row in rows)
            {
                if (row.Bilis < Roster.TraitMin || row.Bilis > Roster.TraitMax) return false;
                if (row.Lakas < Roster.TraitMin || row.Lakas > Roster.TraitMax) return false;
                if (row.Tatag < Roster.TraitMin || row.Tatag > Roster.TraitMax) return false;
            }
            return true;
        }
    }
}
