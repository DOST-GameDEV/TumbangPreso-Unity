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
        /// <summary>
        /// ⚠️⚠️ THE FOLDER THE GAME LOADS FROM, AND IT WAS `Art/audio/sfx` UNTIL 2026-09-04.
        /// `AudioDirector` reaches every cue through `Resources.Load<AudioClip>($"Sfx/{stem}")`,
        /// and `Resources.Load` can only resolve inside a folder literally named `Resources`.
        /// `Art/audio/sfx` is not under one, so **every file this build gate graded was
        /// unreachable by the running game.**
        ///
        /// ⚠️⚠️ AND THE TWO COPIES HAD DRIFTED: 21 of 117 cues differed before the 2026-09-04
        /// pass, because the 2026-09-03 source pass wrote to `Resources/Sfx` while this went on
        /// checking the untouched originals. A cue could have gone missing, silent or clipped in
        /// the shipped folder and this check would have passed on the other copy.
        ///
        /// ⚠️ `tools/audit_cue_audio.py` CARRIED THE SAME CONSTANT AND MOVED IN THE SAME COMMIT.
        /// Two gates on one subsystem both pointing at the wrong copy is why neither noticed the
        /// other. `docs/TODO.md` § 144.3.
        /// </summary>
        private const string SfxDir = "Assets/TumbangPreso/Resources/Sfx";
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
        /// ⚠️⚠️ `TryGetClip` IS A CONSUMER TOO, AND IT IS NOT A `Play` METHOD. It hands a caller
        /// the clip so it can drive its own `AudioSource`, which is how the LRT consist carries a
        /// moving, dopplered rumble instead of a one-shot pinned to the spot it was fired at
        /// (`LrtTrainFlyby`, § THE PASS). Without it here, moving `sfx_lrt_pass` onto that path
        /// would make this check report the file as one nothing plays, which is the exact
        /// direction-3 hole the note above says it exists to close.
        /// ⚠️⚠️ `NetCue` IS A SECOND PLAY SURFACE AND THIS CHECK WAS BLIND TO IT THE MOMENT IT
        /// LANDED. The note above predicted exactly this (*"a play method added later and not
        /// added here is a call site this check cannot see"*) and it came true within the hour:
        /// `TumbangPreso.NetCue` was added on 2026-08-26 so a world sound reaches every peer
        /// (`docs/TODO.md` § 25), five Phaister call sites moved onto it, and the first run of
        /// this check afterwards reported **"cues fired in code that nothing declares: none"**
        /// while being unable to see any of them. A typo behind `NetCue` would have been the
        /// `sfx_ghost_appear` fault again, with the check green, which is the whole reason
        /// direction 3 exists.
        ///
        /// ⚠️ ANCHORED ON `Audio` OR ON `NetCue`, NOT ON A BARE `Play`. Dropping the anchor would
        /// match `Music.Play("match")`, `EmotePlayer.Play(id)` and every other `Play` in the tree,
        /// and the note above records the music beds being reported as silent sound effects the
        /// first time that was tried.
        private const string CallSitePattern =
            @"(?:Audio\??\.|NetCue\.)(?:PlayAtVaried|PlayAt2D|PlayAt|PlayUi|PlayVaried|TryGetClip|Play)\s*\(\s*""(?<cue>[a-zA-Z0-9_]+)""";
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

            // Every literal cue name any runtime file passes to a play call. Gathered on the
            // pass below and read by direction 3.
            var fired = new HashSet<string>(StringComparer.Ordinal);

            foreach (string file in Directory.GetFiles(RuntimeRoot, "*.cs",
                                                       SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);

                foreach (Match m in Regex.Matches(text, CallSitePattern))
                {
                    string cue = m.Groups["cue"].Value;

                    // Direction 3's evidence, gathered on the pass that is already reading
                    // every runtime file. See the block below.
                    fired.Add(cue);

                    if (declared.Contains(cue)) continue;

                    sb.AppendLine($"  UNDECLARED: {Path.GetFileName(file)} fires '{cue}', " +
                                  "which is in no cue list, so it plays silence.");
                    undeclared++;
                    problems++;
                }
            }
            if (undeclared == 0) sb.AppendLine("  none");

            // ⚠️⚠️ DIRECTION 3: A CUE THAT IS DECLARED, SHIPPED AS A FILE, AND PLAYED BY NOTHING.
            // 🧑 2026-08-30: *"No audio cue on victory, like wala ung jingle unlke last time"*.
            // `match_win` was in `AudioCues.Live`, had a real `.wav` behind it, and was one of the
            // six names in `DuckTriggers` — so the music bed was written to get out of the way of
            // a sound nothing ever played. `docs/TODO.md` § 85.2.
            //
            // ⚠️ THE OTHER TWO DIRECTIONS COULD NOT SEE IT, AND THAT IS STRUCTURAL RATHER THAN AN
            // OVERSIGHT. "Cues with no file" asks about the catalogue against the disk, and "files
            // no live cue can reach" asks about the disk against the catalogue. **Both sides were
            // present and correct.** The missing question is about the catalogue against the
            // CODE, and it is the only one of the three a player can hear.
            //
            // ⚠️ IT IS A REPORT, NOT A FAILURE, so it does not increment `problems`. A cue can be
            // legitimately dormant: `boot_sting` is played through a video path this regex cannot
            // see, and `AudioCues` itself documents five `.wav` files orphaned when the ability
            // layer was deleted. Failing the gate on those would teach the next person to ignore
            // this block, which is worse than not printing it. **Read the list and know why each
            // line is on it** — that is the same standard `docs/TODO.md` § 6 sets for the
            // wall-clock probes.
            sb.AppendLine();
            sb.AppendLine("-- live cues that no code plays --");
            int dormant = 0;

            foreach (string cue in AudioCues.Live)
            {
                if (fired.Contains(cue)) continue;

                sb.AppendLine($"  DORMANT: '{cue}' is declared and has a file, and no call site " +
                              "in Runtime plays it. Either it is a feature nobody selected, or " +
                              "it is fired from somewhere this text scan cannot see.");
                dormant++;
            }
            if (dormant == 0) sb.AppendLine("  none");

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
