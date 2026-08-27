using System;
using System.IO;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Which branch this build came off, for the corner stamp.
    ///
    /// ⚠️⚠️ THE CORNER LABEL EXISTS TO ANSWER "IS THIS THE BUILD I ASKED FOR", AND A VERSION
    /// NUMBER STOPPED ANSWERING IT. `bundleVersion` is bumped per change, not per branch, so
    /// several branches in flight at once all read `v4.72` and the only way to tell which .exe
    /// was on the Desktop was to diff files. That is the same failure `GameVersion`'s own note
    /// records from the PGH project, one level up: there the stale build was four days old, here
    /// it is the wrong branch entirely. 🧑, 2026-08-27: *"for every branch made it would replace
    /// the version number on the bottom right corner with the branch name instead"*.
    ///
    /// ⚠️⚠️ `main` KEEPS THE VERSION NUMBER, AND THAT IS THE POINT OF THE RULE RATHER THAN AN
    /// EXCEPTION TO IT. A branch name in the corner means "this is work in flight". A build off
    /// `main` is the game, and the number on it is what goes in a screenshot to a sponsor, which
    /// is what the stamp was originally for.
    ///
    /// ⚠️ IT IS THE LABEL ONLY. `Application.version` still carries the real version everywhere
    /// it means something to a machine: `NetSession.ProtocolVersion`'s sibling in the LAN beacon
    /// payload, the online lobby record and the approval hello all keep reading it. A branch name
    /// on the wire would refuse two peers built from the same commit on different branches.
    ///
    /// ⚠️ A PLAYER HAS NO GIT. The name is resolved in the editor and written to
    /// `Resources/BuildBranch.txt` by `GameBuilder` on every build, so the shipped .exe reads a
    /// text asset. In the editor the file is ignored and git is read live, because a stamp from
    /// the last build is exactly the stale thing this is meant to prevent.
    /// </summary>
    public static class BuildBranch
    {
        /// <summary>Where <c>GameBuilder</c> writes the name and where the player reads it.</summary>
        public const string ResourceName = "BuildBranch";

        /// <summary>
        /// The branch whose builds keep the version number instead of a name.
        /// </summary>
        public const string ReleaseBranch = "main";

        private static string _cached;
        private static bool _resolved;

        /// <summary>
        /// The branch name, or "" when this build should show its version number instead:
        /// on <see cref="ReleaseBranch"/>, on a detached HEAD, or when there is no git at all.
        /// </summary>
        public static string Name
        {
            get
            {
                if (_resolved) return _cached;

                _resolved = true;
                _cached = Resolve() ?? "";
                return _cached;
            }
        }

        /// <summary>Test seam. Also what a screen calls after a branch switch in the editor.</summary>
        public static void Forget()
        {
            _resolved = false;
            _cached = null;
        }

        private static string Resolve()
        {
#if UNITY_EDITOR
            // ⚠️ GIT WINS OVER THE FILE IN THE EDITOR. The text asset is a build artefact and is
            // one build behind for the whole of the next branch's work.
            string live = FromGit(Path.GetDirectoryName(Application.dataPath));
            if (!string.IsNullOrEmpty(live)) return Displayable(live);
#endif
            var asset = Resources.Load<TextAsset>(ResourceName);
            return asset == null ? null : Displayable(asset.text);
        }

        /// <summary>"" for the branch that should show a number, the trimmed name otherwise.</summary>
        private static string Displayable(string raw)
        {
            string name = (raw ?? "").Trim();
            if (name.Length == 0) return "";
            return name == ReleaseBranch ? "" : name;
        }

        /// <summary>
        /// The checked-out branch under <paramref name="repoRoot"/>, or null.
        ///
        /// ⚠️⚠️ IT HANDLES A WORKTREE, AND EVERY SESSION IN THIS PROJECT RUNS IN ONE. In a
        /// linked worktree `.git` is a FILE reading `gitdir: <path>`, not a directory, so the
        /// obvious `repoRoot/.git/HEAD` does not exist and the naive version reports "no git" on
        /// exactly the checkouts this stamp is for.
        /// </summary>
        public static string FromGit(string repoRoot)
        {
            if (string.IsNullOrEmpty(repoRoot)) return null;

            try
            {
                string dotGit = Path.Combine(repoRoot, ".git");

                if (File.Exists(dotGit))
                {
                    string pointer = GitDirFromPointer(File.ReadAllText(dotGit));
                    if (string.IsNullOrEmpty(pointer)) return null;

                    // The pointer is usually absolute, but git writes a relative one when the
                    // worktree and the repository share a parent.
                    dotGit = Path.IsPathRooted(pointer) ? pointer : Path.Combine(repoRoot, pointer);
                }

                string head = Path.Combine(dotGit, "HEAD");
                return File.Exists(head) ? BranchFromHead(File.ReadAllText(head)) : null;
            }
            catch (Exception e)
            {
                // ⚠️ A MISSING OR UNREADABLE REPO IS NOT AN ERROR. A player has no git and a
                // zip of the source has no `.git`; both should quietly show the version number.
                Debug.Log($"[Build] no branch name available ({e.GetType().Name}); using the version.");
                return null;
            }
        }

        /// <summary>The path out of a `gitdir: ...` pointer file, or null.</summary>
        public static string GitDirFromPointer(string contents)
        {
            const string prefix = "gitdir:";
            string text = (contents ?? "").Trim();

            return text.StartsWith(prefix, StringComparison.Ordinal)
                ? text.Substring(prefix.Length).Trim()
                : null;
        }

        /// <summary>
        /// The branch out of a HEAD file, or null on a detached HEAD.
        ///
        /// ⚠️ THE WHOLE REF PATH AFTER `refs/heads/` IS THE NAME, slashes included.
        /// `claude/multiplayer-lobby-switching-bugs-d1546c` and `fix/hud-calm-down` are both
        /// branches with a slash in them, and taking only the last segment would print
        /// `hud-calm-down` for one of several branches that could have produced the build.
        /// </summary>
        public static string BranchFromHead(string contents)
        {
            const string prefix = "ref: refs/heads/";
            string text = (contents ?? "").Trim();

            return text.StartsWith(prefix, StringComparison.Ordinal)
                ? text.Substring(prefix.Length).Trim()
                : null;   // detached HEAD: a bare sha, which is not a branch
        }
    }
}
