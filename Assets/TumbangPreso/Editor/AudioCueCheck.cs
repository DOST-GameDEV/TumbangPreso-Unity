using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TumbangPreso.Audio;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Cross-references the cue table against the audio files that actually exist.
    ///
    /// ⚠️⚠️ THIS CHECKS BOTH DIRECTIONS, AND THE SECOND ONE IS THE ONE THAT BIT.
    /// A cue with no file is the obvious failure and it is loud. A FILE with no cue is silent
    /// and shipped for weeks: `slipper_land` was registered, given its own mix level, and
    /// never called, so a throw that hit a body played one sound, a throw that hit the can
    /// played another, and a throw that simply MISSED, by far the most common outcome at 38 of
    /// 71 flights in the baseline, made no sound at all. The one shot whose result the
    /// attacker most needs to hear was the one the game said nothing about.
    ///
    /// ⚠️ AND A DELIVERY'S EXTENSION LIES. Voice arrived once as AAC-in-3GP named .wav, and the
    /// soundtrack as MP3 named .wav. A mislabelled container loads as null, so a full folder
    /// with correct names and correct wiring produces silence, which is indistinguishable from
    /// "not recorded yet". This sniffs magic bytes rather than trusting the suffix.
    ///
    /// Run:
    ///   Unity.exe -batchmode -quit -nographics -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.AudioCueCheck.Run
    /// </summary>
    public static class AudioCueCheck
    {
        private const string SfxDir = "Assets/TumbangPreso/Art/audio/sfx";
        private const string RuntimeRoot = "Assets/TumbangPreso/Runtime";

        /// <summary>Every way the runtime asks for a cue by name.
        /// ⚠️ KEEP IT IN STEP WITH `AudioManager`'s PUBLIC SURFACE. A play method added
        /// later and not added here is a call site this check cannot see, which is exactly
        /// the hole direction 3 exists to close. The longer names come first so the
        /// alternation cannot match `Play` inside `PlayAtVaried`.
        ///
        /// ⚠️⚠️ IT IS ANCHORED ON `Audio`, AND WITHOUT THAT IT REPORTS THE MUSIC BED AS
        /// A MISSING SOUND EFFECT. `GameServices.Music.Play(""match"", ...)` and
        /// `Play(""menu"", ...)` name TRACKS, which live in `AudioCues.Music` and are
        /// nowhere near `Live`. The first run of this check flagged all three as silent
        /// cues; they are neither silent nor cues.</summary>
        private const string CallSitePattern =
            @"Audio\??\.(?:PlayAtVaried|PlayAt2D|PlayAt|PlayUi|Play)\s*\(\s*""(?<cue>[a-zA-Z0-9_]+)""";
        private const string MusicDir = "Assets/TumbangPreso/Art/audio/music";
        private const string ResultPath = "Logs/audio-cue-check.txt";

        [MenuItem("Tumbang Preso/Check Audio Cues")]
        public static void RunFromMenu() => Execute();

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        /// <summary>⚠️ PUBLIC SO `Checks` CAN RUN IT WITHOUT A SECOND UNITY LAUNCH. See that
        /// class: the launches, not the assertions, are what a verification pass costs.</summary>
        public static bool Execute()
        {
            var sb = new StringBuilder();
            int problems = 0;

            var onDisk = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(SfxDir))
                foreach (var f in Directory.GetFiles(SfxDir, "*.wav"))
                    onDisk.Add(Path.GetFileNameWithoutExtension(f));

            sb.AppendLine("AUDIO CUE CHECK");
            sb.AppendLine($"sfx files on disk: {onDisk.Count}");
            sb.AppendLine($"live cues declared: {AudioCues.Live.Count}");
            sb.AppendLine();

            // Direction 1: a cue with no file. Resolve aliases first or six report falsely.
            var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            sb.AppendLine("-- cues with no file --");
            foreach (var cue in AudioCues.Live)
            {
                string stem = AudioCues.FileStemFor(cue);
                reachable.Add(stem);

                if (onDisk.Contains(stem)) continue;

                sb.AppendLine($"  MISSING: cue '{cue}' resolves to '{stem}.wav', which is absent.");
                problems++;
            }
            if (problems == 0) sb.AppendLine("  none");

            // ⚠️⚠️ DIRECTION 3, ADDED 2026-08-26: A CUE THE CODE FIRES THAT NOTHING
            // DECLARES. This check had two directions and both started from `AudioCues.Live`,
            // so it could only answer questions about cues somebody had remembered to
            // declare. `LrtTrainFlyby` called `PlayAtVaried(""ui_move"", ...)` for the map's
            // signature 24 s event; there is no `ui_move` in `Live` and no `ui_move.wav`
            // anywhere, so every pass wrote `[Audio] no cue registered for 'ui_move'` into
            // the player log and played silence, in every build, for the whole life of the
            // map, with this check green. A typo in a string literal is the easiest way to
            // ship a silent feature and it was the one direction nothing looked at.
            //
            // ⚠️ IT READS THE RUNTIME AS TEXT, the same idiom `SceneScriptCheck` and
            // `MapGradeSanityTests` use and for the same reason: the call sites are string
            // literals and there is nothing to reflect over. Only literals are checked; a
            // cue built from a variable cannot be resolved here, and every one in the tree
            // today is a literal.
            sb.AppendLine();
            sb.AppendLine("-- cues fired in code that nothing declares --");
            int undeclared = 0;

            // ⚠️ A SET, NOT `IReadOnlyList.Contains`. `AudioCues.Live` is a list, and calling
            // Contains on it inside a per-file loop is a linear scan of ~90 strings for every
            // call site in the runtime. It also binds to the wrong overload outright: the
            // MemoryExtensions span extension wins and demands a StringComparison.
            var declared = new HashSet<string>(AudioCues.Live, StringComparer.Ordinal);

            foreach (string file in Directory.GetFiles(RuntimeRoot, "*.cs",
                                                       SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);

                foreach (Match m in Regex.Matches(text, CallSitePattern))
                {
                    string cue = m.Groups["cue"].Value;
                    if (declared.Contains(cue)) continue;

                    sb.AppendLine($"  UNDECLARED: {Path.GetFileName(file)} fires '{cue}', " +
                                  "which is in no cue list, so it plays silence.");
                    undeclared++;
                    problems++;
                }
            }
            if (undeclared == 0) sb.AppendLine("  none");

            // Direction 2: a file no cue can reach. The silent failure.
            sb.AppendLine();
            sb.AppendLine("-- files no live cue can reach --");
            var orphans = new List<string>();
            foreach (var stem in onDisk)
                if (!reachable.Contains(stem)) orphans.Add(stem);

            orphans.Sort(StringComparer.OrdinalIgnoreCase);
            if (orphans.Count == 0)
            {
                sb.AppendLine("  none");
            }
            else
            {
                foreach (var o in orphans)
                {
                    bool known = false;
                    foreach (var dead in AudioCues.DeletedAbilityCues)
                        if (string.Equals(dead, o, StringComparison.OrdinalIgnoreCase)) { known = true; break; }

                    sb.AppendLine(known
                        ? $"  known-dead: '{o}.wav' belongs to the deleted ability layer. " +
                          "Shipping, unreachable, and a human decides whether it leaves the build."
                        : $"  ORPHAN: '{o}.wav' exists and no cue reaches it. Either a call site " +
                          "is missing or the file is dead weight. Do not assume the latter.");

                    if (!known) problems++;
                }
            }

            // Direction 3: the extension lies.
            sb.AppendLine();
            sb.AppendLine("-- containers that do not match their extension --");
            int mislabelled = 0;
            foreach (var dir in new[] { SfxDir, MusicDir })
            {
                if (!Directory.Exists(dir)) continue;

                foreach (var f in Directory.GetFiles(dir))
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext != ".wav" && ext != ".mp3") continue;

                    string actual = SniffContainer(f);
                    if (actual == null) continue;

                    bool ok = (ext == ".wav" && actual == "wav") || (ext == ".mp3" && actual == "mp3");
                    if (ok) continue;

                    sb.AppendLine($"  MISLABELLED: {Path.GetFileName(f)} is really {actual}. " +
                                  "It will load as null and be silent, which reads as 'not " +
                                  "delivered yet' rather than as a broken file.");
                    mislabelled++;
                    problems++;
                }
            }
            if (mislabelled == 0) sb.AppendLine("  none");

            sb.AppendLine();
            sb.AppendLine(problems > 0 ? $"RESULT: {problems} problem(s)." : "RESULT: OK.");

            try
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText(ResultPath, sb.ToString());
            }
            catch (Exception e)
            {
                Debug.LogError($"[AudioCueCheck] could not write {ResultPath}: {e.Message}");
            }

            Debug.Log(sb.ToString());
            return problems == 0;
        }

        /// <summary>Magic bytes, never the suffix. Returns null when unrecognised.</summary>
        private static string SniffContainer(string path)
        {
            try
            {
                using var fs = File.OpenRead(path);
                var head = new byte[12];
                if (fs.Read(head, 0, 12) < 12) return null;

                // "RIFF" .... "WAVE"
                if (head[0] == 'R' && head[1] == 'I' && head[2] == 'F' && head[3] == 'F' &&
                    head[8] == 'W' && head[9] == 'A' && head[10] == 'V' && head[11] == 'E')
                    return "wav";

                // "ID3" tag, or an MPEG frame sync.
                if (head[0] == 'I' && head[1] == 'D' && head[2] == '3') return "mp3";
                if (head[0] == 0xFF && (head[1] & 0xE0) == 0xE0) return "mp3";

                // "OggS"
                if (head[0] == 'O' && head[1] == 'g' && head[2] == 'g' && head[3] == 'S')
                    return "ogg";

                // ISO base media: "....ftyp". This is the AAC-in-3GP case that shipped silent.
                if (head[4] == 'f' && head[5] == 't' && head[6] == 'y' && head[7] == 'p')
                    return "mp4/3gp";

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
