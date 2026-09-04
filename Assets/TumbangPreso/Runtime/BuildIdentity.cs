using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// What exactly this build is, answerable without opening the source.
    ///
    /// ⚠️⚠️ THE QUESTION THIS EXISTS FOR IS ASKED AT A VENUE, BY SOMEBODY WHO CANNOT READ CODE,
    /// WITH A QUEUE BEHIND THEM: **"are these two machines running the same game?"** Until this
    /// file, nothing in a shipped player could answer it. The corner reads `v1.00` on every build
    /// off `main` (`BuildBranch`, deliberately), `Application.version` moves per change rather than
    /// per commit, and `NetSession.ProtocolVersion` was only observable by trying to join
    /// something and watching it fail.
    ///
    /// ⚠️⚠️ AND FAILING TO ANSWER IT LOOKS EXACTLY LIKE A NETWORK BUG. Peers on different protocol
    /// numbers **refuse each other by design** (`CLAUDE.md` § 4a), so a Windows player and an .apk
    /// built from different commits produce a join that never completes, with no message that
    /// names the cause. `Attention.md` § 1 already warns a person about this in prose, and prose
    /// is not a gate: *"Both players must come from the same commit ... if the two builds on your
    /// machines are from different commits they will refuse each other correctly, and it reads
    /// exactly like a bug."*
    ///
    /// ⚠️ IT IS A LABEL AND NOTHING HERE GOES ON THE WIRE. `BuildBranch`'s own note carries the
    /// rule and the reason: `Application.version` is what the LAN beacon, the lobby record and the
    /// approval hello compare, and putting anything else in that comparison would refuse two peers
    /// built from the same commit. This type is read by humans and by `tools/qualify.py`.
    ///
    /// ⚠️ NO SECRETS. The UGS project id is already in `CLAUDE.md` and in `ProjectSettings.asset`
    /// and identifies a namespace rather than granting anything; no token, key or credential is
    /// read here, and the crash bundle that ships this must keep it that way.
    ///
    /// ⚠️ A PLAYER HAS NO GIT, which is `BuildBranch`'s problem solved the same way: the editor
    /// resolves it live, `GameBuilder` writes `Resources/BuildIdentity.json` on every build, and
    /// the shipped player reads the text asset.
    /// </summary>
    public static class BuildIdentity
    {
        /// <summary>Where <c>GameBuilder</c> writes it and where the player reads it.</summary>
        public const string ResourceName = "BuildIdentity";

        /// <summary>
        /// Print the identity and quit. For a player at a venue, or a build machine.
        ///
        /// ⚠️ A COMMAND-LINE SWITCH RATHER THAN A KEY, AND THAT IS DELIBERATE. A new key would
        /// need a row in the input catalogue and a device answer for a pad and a thumb
        /// (`CLAUDE.md` § 4a), which is the right rule and the wrong tool for a question asked by
        /// whoever is holding the laptop. `NetStateReport.ReportSwitch` set the pattern.
        /// </summary>
        public const string PrintSwitch = "-tp-identity";

        [Serializable]
        public sealed class Record
        {
            public string sha = "";
            public string branch = "";
            public int protocol;
            public string target = "";
            public string appVersion = "";
            public string ugsProject = "";
            public string ugsEnvironment = "";
            public string builtAt = "";

            /// <summary>
            /// True unless the tree was PROVEN clean.
            ///
            /// ⚠️⚠️ IT IS "NOT PROVEN CLEAN" AND NOT "PROVEN DIRTY", AND THE ASYMMETRY IS THE
            /// WHOLE FIX. Until 2026-09-05 this was set from an mtime comparison between
            /// `.git/index` and the branch ref, which cannot see an ordinary unstaged edit at
            /// all, since editing a file does not rewrite the index, and it answered false outright
            /// whenever the ref was packed, which is exactly the state a freshly cloned build
            /// machine is in. So the two commonest situations both stamped `dirty: false`, and a
            /// flag that says "clean" when it means "I could not tell" is worse than no flag:
            /// somebody at a venue reads it and stops looking.
            ///
            /// ⚠️ READ <see cref="treeState"/> FOR THE THREE-WAY ANSWER. This stays a bool because
            /// it is on a serialised record that shipped players already carry, and because every
            /// reader of it wants "may I trust the SHA", which is exactly this question.
            /// </summary>
            public bool dirty;

            /// <summary>
            /// `clean`, `dirty` or `unknown`.
            ///
            /// ⚠️⚠️ `unknown` IS A REAL ANSWER AND MUST NEVER BE COLLAPSED INTO EITHER OF THE
            /// OTHER TWO. A build machine with no `git` on PATH, a source export with no
            /// repository and a `git` that timed out are all states this can genuinely be in, and
            /// `docs/TODO.md` § 145.2's brief is explicit: *"Do not turn 'cannot determine' into
            /// 'clean'."* `tools/qualify.py` refuses to certify one, which is the point of saying
            /// so out loud rather than guessing.
            ///
            /// ⚠️ EMPTY MEANS THE STAMP PREDATES THIS FIELD. A player built before 2026-09-05
            /// carries `dirty` and nothing else; readers treat an empty string as `unknown`
            /// rather than believing the old flag.
            /// </summary>
            public string treeState = "";
        }

        /// <summary>What a working tree was, when the artifact was stamped.</summary>
        public enum TreeState
        {
            /// <summary>Nothing could be established. ⚠️ NOT THE SAME AS CLEAN.</summary>
            Unknown = 0,

            /// <summary>`git status --porcelain` was empty.</summary>
            Clean = 1,

            /// <summary>`git status --porcelain` had tracked changes in it.</summary>
            Dirty = 2,
        }

        /// <summary>
        /// The three-way answer for a record, with an old stamp reading Unknown.
        ///
        /// ⚠️ A PRE-2026-09-05 STAMP CANNOT BE UPGRADED BY GUESSING. Its `dirty` flag came from
        /// the mtime heuristic, which could not see an unstaged edit; reading `false` there as
        /// `Clean` would carry that heuristic's blind spot forward into the gate built to replace
        /// it. An old build is one whose tree state nobody knows, and that is what it says.
        /// </summary>
        public static TreeState StateOf(Record record)
        {
            if (record == null) return TreeState.Unknown;

            switch ((record.treeState ?? "").Trim().ToLowerInvariant())
            {
                case "clean": return TreeState.Clean;
                case "dirty": return TreeState.Dirty;
                default: return TreeState.Unknown;
            }
        }

        private static Record _cached;
        private static bool _resolved;

        /// <summary>The identity, resolved once.</summary>
        public static Record Current
        {
            get
            {
                if (_resolved) return _cached;

                _resolved = true;
                _cached = Resolve();
                return _cached;
            }
        }

        /// <summary>Test seam, and what a rebuild calls in the editor.</summary>
        public static void Forget()
        {
            _resolved = false;
            _cached = null;
        }

        public static string ShortSha
        {
            get
            {
                string sha = Current.sha ?? "";
                return sha.Length >= 12 ? sha.Substring(0, 12) : sha;
            }
        }

        /// <summary>
        /// The one line worth printing in a log or reading down a phone.
        ///
        /// ⚠️ THE SHA AND THE PROTOCOL ARE THE TWO THAT DECIDE WHETHER TWO MACHINES CAN PLAY.
        /// Everything else on the record is context for afterwards.
        /// </summary>
        public static string OneLine()
        {
            var r = Current;
            string tree = StateOf(r) == TreeState.Clean
                ? ""
                : StateOf(r) == TreeState.Dirty ? "+dirty" : "+unverified";

            return $"TUMBANG PRESO {r.appVersion} | {(string.IsNullOrEmpty(ShortSha) ? "no-sha" : ShortSha)}" +
                   $"{tree} | protocol {r.protocol} | {r.target}";
        }

        /// <summary>The whole record, for a diagnostic screen or a failure bundle.</summary>
        public static string Summary()
        {
            var r = Current;
            var sb = new StringBuilder();
            sb.AppendLine("BUILD IDENTITY");
            sb.AppendLine($"  commit        {(string.IsNullOrEmpty(r.sha) ? "(unstamped)" : r.sha)}");
            sb.AppendLine($"  working tree  {StateOf(r).ToString().ToLowerInvariant()}" +
                          (StateOf(r) == TreeState.Dirty
                              ? "  ⚠ built with uncommitted changes; the commit above is not what ran"
                              : StateOf(r) == TreeState.Unknown
                                  ? "  ⚠ could not be established at build time"
                                  : ""));
            sb.AppendLine($"  branch        {(string.IsNullOrEmpty(r.branch) ? "(none)" : r.branch)}");
            sb.AppendLine($"  protocol      {r.protocol}");
            sb.AppendLine($"  target        {r.target}");
            sb.AppendLine($"  app version   {r.appVersion}");
            sb.AppendLine($"  ugs project   {r.ugsProject}");
            sb.AppendLine($"  ugs env       {r.ugsEnvironment}");
            sb.AppendLine($"  built         {r.builtAt}");
            sb.AppendLine($"  running on    {Application.platform}");
            return sb.ToString();
        }

        /// <summary>
        /// Whether this build could play against one carrying <paramref name="other"/>.
        ///
        /// ⚠️ THE PROTOCOL IS THE ONLY THING THAT ACTUALLY REFUSES, and the SHA is the thing that
        /// explains it. Two builds from the same commit always agree; two from different commits
        /// usually do, and the answer says which case you are in rather than only yes or no.
        /// </summary>
        public static string CompatibilityWith(Record other)
        {
            if (other == null) return "there is no identity to compare against";

            if (other.protocol != Current.protocol)
                return $"protocol {Current.protocol} against {other.protocol}: these two refuse " +
                       $"each other by design. Rebuild both from one commit";

            if (!string.Equals(other.sha, Current.sha, StringComparison.Ordinal))
                return $"same protocol, different commits ({ShortSha} against " +
                       $"{(other.sha ?? "").Substring(0, Math.Min(12, (other.sha ?? "").Length))}). " +
                       $"They will connect; anything changed between the two is a real difference";

            return "same commit, same protocol";
        }

        /// <summary>
        /// Print it once, on every launch, before anything else can fail.
        ///
        /// ⚠️⚠️ THE LOG IS THE ROUTE THAT WORKS ON A PHONE. A command-line switch answers the
        /// question on a desktop and there is no command line on Android, so the first line of
        /// every `Player.log` and every `logcat` is the identity. This is also what makes a crash
        /// report from a venue worth reading: the exception without the commit is half a bug
        /// report, and the half that is missing is the one nobody can reconstruct afterwards.
        ///
        /// ⚠️ IT RUNS BEFORE THE FIRST SCENE, so a build that dies during load still says what it
        /// was. That is the case where the identity matters most and where a screen cannot help.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AnnounceOnLaunch()
        {
            Debug.Log("[Build] " + OneLine());

            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (!string.Equals(arg, PrintSwitch, StringComparison.OrdinalIgnoreCase)) continue;

                Debug.Log(Summary());

                // ⚠️ WRITTEN AS WELL AS LOGGED, because the person who needs it is copying it into
                // a message rather than reading a log file with a text editor.
                try
                {
                    File.WriteAllText("build-identity.txt", Summary());
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[BuildIdentity] could not write build-identity.txt: {e.Message}");
                }

                Application.Quit(0);
                return;
            }
        }

        // -------------------------------------------------------------------

        private static Record Resolve()
        {
#if UNITY_EDITOR
            // ⚠️ GIT WINS IN THE EDITOR, for `BuildBranch.Resolve`'s reason: the text asset is a
            // build artefact and is one build behind for the whole of the next branch's work.
            var live = FromEditor();
            if (live != null) return live;
#endif
            var asset = Resources.Load<TextAsset>(ResourceName);
            if (asset != null && !string.IsNullOrEmpty(asset.text))
            {
                try
                {
                    var parsed = JsonUtility.FromJson<Record>(asset.text);
                    if (parsed != null) return parsed;
                }
                catch (Exception e)
                {
                    // ⚠️ AN UNREADABLE STAMP IS NOT A CRASH. A build that cannot say what it is
                    // is a diagnostics problem; refusing to run over it would turn a label into
                    // an outage.
                    Debug.LogWarning($"[BuildIdentity] stamp unreadable: {e.Message}");
                }
            }

            return Unstamped();
        }

        private static Record Unstamped() => new Record
        {
            sha = "",
            branch = BuildBranch.Name,
            protocol = Net.NetSession.ProtocolVersion,
            target = Application.platform.ToString(),
            appVersion = Application.version,
            ugsProject = Application.cloudProjectId ?? "",
            ugsEnvironment = "",
            builtAt = "",
        };

#if UNITY_EDITOR
        private static Record FromEditor()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            var record = Unstamped();
            record.sha = HeadSha(root) ?? "";
            record.branch = BuildBranch.FromGit(root) ?? "";
            record.target = "Editor/" + Application.platform;
            record.builtAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            return record;
        }
#endif

        /// <summary>
        /// The commit under <paramref name="repoRoot"/>, or null.
        ///
        /// ⚠️⚠️ IT HANDLES A WORKTREE AND A PACKED REF, AND BOTH ARE ORDINARY HERE.
        /// `BuildBranch.FromGit` already records the worktree half: `.git` is a FILE reading
        /// `gitdir: <path>` in a linked worktree, so `repoRoot/.git/HEAD` does not exist. The
        /// packed half is the other one: a freshly cloned or garbage-collected repository has no
        /// loose file at `refs/heads/<branch>` and the ref lives in `packed-refs` instead, so
        /// reading only the loose path reports "no commit" on exactly the clean checkout a build
        /// machine makes.
        /// </summary>
        public static string HeadSha(string repoRoot)
        {
            if (string.IsNullOrEmpty(repoRoot)) return null;

            try
            {
                string dotGit = Path.Combine(repoRoot, ".git");

                if (File.Exists(dotGit))
                {
                    string pointer = BuildBranch.GitDirFromPointer(File.ReadAllText(dotGit));
                    if (string.IsNullOrEmpty(pointer)) return null;
                    dotGit = Path.IsPathRooted(pointer) ? pointer : Path.Combine(repoRoot, pointer);
                }

                string headFile = Path.Combine(dotGit, "HEAD");
                if (!File.Exists(headFile)) return null;

                string head = File.ReadAllText(headFile).Trim();

                // A detached HEAD is the sha itself, which is the case a build machine is in.
                if (!head.StartsWith("ref:", StringComparison.Ordinal))
                    return LooksLikeSha(head) ? head : null;

                string refName = head.Substring(4).Trim();

                string loose = Path.Combine(dotGit, refName.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(loose))
                {
                    string sha = File.ReadAllText(loose).Trim();
                    if (LooksLikeSha(sha)) return sha;
                }

                string packed = Path.Combine(dotGit, "packed-refs");
                if (File.Exists(packed))
                {
                    foreach (string line in File.ReadAllLines(packed))
                    {
                        if (line.Length == 0 || line[0] == '#' || line[0] == '^') continue;

                        int space = line.IndexOf(' ');
                        if (space <= 0) continue;

                        if (string.Equals(line.Substring(space + 1).Trim(), refName,
                                          StringComparison.Ordinal))
                        {
                            string sha = line.Substring(0, space).Trim();
                            if (LooksLikeSha(sha)) return sha;
                        }
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                Debug.Log($"[BuildIdentity] no commit available ({e.GetType().Name}).");
                return null;
            }
        }

        private static bool LooksLikeSha(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length < 7 || s.Length > 64) return false;

            foreach (char c in s)
            {
                bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!hex) return false;
            }

            return true;
        }
    }
}
