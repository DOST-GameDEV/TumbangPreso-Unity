using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Refreshes the editor the moment git moves this checkout to another branch or commit, without
    /// waiting for the editor window to be focused again.
    ///
    /// ⚠️⚠️ WHY IT EXISTS, 2026-10-17: a branch was switched with the editor open, and the player's
    /// room still drew the old screen. The editor's Auto Refresh was already ON; it simply rescans
    /// only when its window regains focus, so a switch made from a terminal, a git client or an
    /// agent while Unity sat on another monitor left it compiling the previous branch's scripts
    /// for as long as nobody clicked into it. Nothing said so, which reads as "the change did not
    /// land".
    ///
    /// ⚠️ IT WATCHES HEAD AND THE BRANCH TIP, NOT THE WORKING FILES. Both change on a switch, a pull,
    /// a reset and a commit, and both are two small files, so polling once a second costs nothing.
    /// The working tree is the editor's own job once it has been told to look.
    ///
    /// ⚠️ A WORKTREE'S `.git` IS A FILE, NOT A FOLDER. It names the real git folder, and the branch
    /// tips live in the shared `commondir` rather than beside that worktree's own HEAD.
    ///
    /// ⚠️ IT STAYS OUT OF THE WAY: never in batch mode (tests and builds own their own refresh),
    /// never in or entering play mode, never mid-compile or mid-import.
    /// </summary>
    [InitializeOnLoad]
    internal static class GitBranchRefresh
    {
        private const double PollSeconds = 1.0;
        private static readonly string GitDir, CommonDir;
        private static string _seen;
        private static double _next;

        static GitBranchRefresh()
        {
            if (Application.isBatchMode) return;
            string root = Path.GetDirectoryName(Application.dataPath);
            GitDir = ResolveGitDir(root);
            if (GitDir == null) return;
            string common = Read(Path.Combine(GitDir, "commondir"))?.Trim();
            CommonDir = string.IsNullOrEmpty(common) ? GitDir : Path.GetFullPath(Path.Combine(GitDir, common));
            _seen = Snapshot();
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now < _next) return;
            _next = now + PollSeconds;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
                || EditorApplication.isUpdating) return;

            string state = Snapshot();
            if (state == null || state == _seen) return;
            _seen = state;
            Debug.Log("[Git] This checkout moved to " + Describe(state) + "; refreshing the editor.");
            AssetDatabase.Refresh();
        }

        /// <summary>HEAD plus the commit its branch points at, so a pull or a commit counts too.</summary>
        private static string Snapshot()
        {
            string head = Read(Path.Combine(GitDir, "HEAD"))?.Trim();
            if (string.IsNullOrEmpty(head)) return null;
            if (!head.StartsWith("ref: ", StringComparison.Ordinal)) return head;

            string reference = head.Substring(5).Trim();
            string tip = Read(Path.Combine(CommonDir, reference))?.Trim() ?? Packed(reference) ?? "";
            return head + "@" + tip;
        }

        private static string Packed(string reference)
        {
            string packed = Read(Path.Combine(CommonDir, "packed-refs"));
            if (packed == null) return null;
            foreach (var line in packed.Split('\n'))
            {
                var parts = line.Trim().Split(' ');
                if (parts.Length == 2 && parts[1] == reference) return parts[0];
            }
            return null;
        }

        private static string Describe(string state)
        {
            const string prefix = "ref: refs/heads/";
            if (!state.StartsWith(prefix, StringComparison.Ordinal)) return "a detached commit";
            int at = state.IndexOf('@');
            return at > 0 ? state.Substring(prefix.Length, at - prefix.Length) : state.Substring(prefix.Length);
        }

        private static string ResolveGitDir(string root)
        {
            string dotGit = Path.Combine(root, ".git");
            if (Directory.Exists(dotGit)) return dotGit;
            string pointer = Read(dotGit)?.Trim();
            if (pointer == null || !pointer.StartsWith("gitdir:", StringComparison.Ordinal)) return null;
            return Path.GetFullPath(Path.Combine(root, pointer.Substring(7).Trim()));
        }

        private static string Read(string path)
        {
            try { return File.Exists(path) ? File.ReadAllText(path) : null; }
            catch (IOException) { return null; }      // git is mid-write; the next poll reads it
            catch (UnauthorizedAccessException) { return null; }
        }
    }
}
