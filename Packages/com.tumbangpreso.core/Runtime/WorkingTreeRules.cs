using System;

namespace TumbangPreso.Core
{
    /// <summary>
    /// Whether an UNTRACKED path could have changed what was compiled or shipped.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE BOTH SIDES OF THE GATE DROPPED EVERY UNTRACKED FILE, AND IN A
    /// UNITY PROJECT THAT IS UNSAFE. `GameBuilder.WorkingTreeState` and `tools/qualify.py` each
    /// skipped every `??` row on the reasoning that `Logs/`, `Builds/` and a scratch file are not
    /// differences in what was built. That is true of those three and false of the general case:
    ///
    ///   * an untracked `.cs` anywhere under `Assets/` or `Packages/` **compiles**;
    ///   * an untracked `.shader`, `.prefab`, `.unity`, `.mat` or anything under `Resources/`
    ///     or `StreamingAssets/` **ships inside the player**;
    ///   * `ProjectSettings/` decides the splash, the app version, the build target and the UGS
    ///     project a join code is resolved in.
    ///
    /// Every one of those changes the artifact while HEAD still points at a commit that does not
    /// contain it, and the report said **SHA X / tree clean** over the top of it.
    /// `docs/TODO.md` § 145.9.
    ///
    /// ⚠️⚠️ THE RULE IS DEFAULT-DENY AND THAT IS THE WHOLE DESIGN. The obvious shape is a list of
    /// directories that ARE source, and it is the brittle one: somebody adds `Assets/NewThing/`
    /// next year, nobody edits the list, and the gate goes quiet in the one direction nobody
    /// checks. Under default-deny a path nobody has thought about is DIRTY, which is loud, and
    /// the only way to make this gate quieter is to add a row below with a reason attached.
    ///
    /// ⚠️ `.gitignore` IS THE FIRST FILTER AND IT DOES MOST OF THE WORK. Both callers ask
    /// `git ls-files --others --exclude-standard`, which does not list ignored files at all, so
    /// `Library/`, `Temp/`, `Logs/`, `Builds/`, the two build stamps and the shader-variant
    /// collection never reach this class. What does reach it is a
    /// file somebody added and git was never told to ignore, and the honest default for that is
    /// "source". The roots below are named anyway, because a `.gitignore` edit must not turn
    /// build output into a certification failure at a venue.
    ///
    /// ⚠️⚠️ AND IT IS IN THE CORE SO THAT BOTH SIDES CAN BE ONE RULE. `IntegrityRules.Digest` set
    /// the precedent: a rule written in C# and again in another language, with nothing comparing
    /// the two, is a rule that silently disagrees with itself. `tools/qualify.py` holds the
    /// Python copy and `tools/audit_harness_contracts.py` asserts the two lists are identical,
    /// so a root added on one side and not the other fails a gate instead of going quiet.
    ///
    /// ⚠️⚠️ AND THE **TRACKED** HALF IS NOT ASKED HERE AT ALL, WHICH IS DELIBERATE AND IS
    /// `docs/TODO.md` § 149.11. Both callers ask `git diff --name-only HEAD`, a CONTENT
    /// comparison, because `git status --porcelain` reported a file that is byte-identical to
    /// HEAD: with `core.autocrlf` true and Unity's YAML stored as LF, every launch that rewrites
    /// a tracked `.asset` leaves a stat-dirty entry that `git update-index --refresh` will not
    /// clear. A gate that refuses a dirty tree then refuses a checkout nobody has edited. There
    /// is no classification to do on a tracked path: if its bytes differ from the commit, it is
    /// a difference in the source that was tested.
    ///
    /// ⚠️ ENGINE-FREE, LIKE EVERYTHING HERE (`CLAUDE.md` § 4). It takes a string and answers a
    /// bool; the process launching, the ten-second bound and the three-way verdict stay in
    /// `GameBuilder`, which is where the `UnityEngine` half belongs.
    /// </summary>
    public static class WorkingTreeRules
    {
        /// <summary>
        /// Roots whose untracked contents genuinely cannot reach a build.
        ///
        /// ⚠️ EVERY ROW CARRIES ITS REASON IN THE COMMENT BESIDE IT. A path on a list with no
        /// reason is a path the next person deletes because it looks arbitrary, or copies
        /// because it looks like a pattern.
        /// </summary>
        public static readonly string[] NonSourceUntrackedRoots =
        {
            // Editor and toolchain output.
            "Logs/",
            "Library/",
            "Temp/",
            "obj/",
            "bin/",
            "Build/",
            "Builds/",
            "build/",
            "UserSettings/",
            "MemoryCaptures/",
            "Recordings/",
            ".utmp/",
            ".vs/",
            ".vscode/",

            // One-shot working files. `.gitignore`'s own note: they are worthless the moment
            // they have run, and the record of why a change was made is the comment in the code.
            "scratchpad/",

            // ⚠️⚠️ `docs/reports/` IS THE ONE ROW THAT IS NOT OBVIOUS AND IT HAS TO BE HERE.
            // `tools/qualify.py` WRITES `docs/reports/qualification-<sha>.md` as its own last
            // act, so without this row the first run leaves the tree non-certifiable and every
            // run afterwards fails on the evidence the previous run produced. A generated report
            // is not source: nothing under it compiles, ships, or changes a byte of the artifact.
            "docs/reports/",
        };

        /// <summary>
        /// Whether an untracked path could have changed what was compiled or shipped.
        ///
        /// ⚠️ THE DEFAULT IS TRUE. A path this method has never heard of is source.
        /// </summary>
        public static bool IsSourceSensitive(string path)
        {
            string row = Normalise(path);
            if (row.Length == 0) return false;

            foreach (string root in NonSourceUntrackedRoots)
            {
                string bare = root.TrimEnd('/');
                if (row.Equals(bare, StringComparison.Ordinal)) return false;
                if (row.StartsWith(root, StringComparison.Ordinal)) return false;
            }

            return true;
        }

        /// <summary>
        /// One path, as git prints it, reduced to something the roots above can be matched on.
        ///
        /// ⚠️⚠️ GIT QUOTES A PATH WITH A SPACE OR A NON-ASCII CHARACTER IN IT, and
        /// `git ls-files --others` is where those arrive from. The surrounding quotes are
        /// stripped here; the C-escapes inside a quoted path are NOT decoded, and that is safe
        /// by construction rather than by luck: this rule is default-deny, so a path this method
        /// mangles is still classified as SOURCE and still makes the tree dirty. **Every parsing
        /// mistake fails towards refusing to certify**, which is the direction that cannot ship
        /// the wrong build.
        /// </summary>
        private static string Normalise(string path)
        {
            string row = (path ?? "").Trim();

            if (row.Length >= 2 && row[0] == '"' && row[row.Length - 1] == '"')
                row = row.Substring(1, row.Length - 2);

            row = row.Replace('\\', '/');

            while (row.StartsWith("./", StringComparison.Ordinal)) row = row.Substring(2);
            while (row.StartsWith("/", StringComparison.Ordinal)) row = row.Substring(1);

            return row;
        }
    }
}
