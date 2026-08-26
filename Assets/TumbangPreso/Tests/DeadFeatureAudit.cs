using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TumbangPreso.Audio;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// Finds things this port DECLARED and never CALLS.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THAT IS THIS PORT'S SIGNATURE FAILURE AND IT KEEPS BEING FOUND BY
    /// A HUMAN LOOKING AT THE GAME. 🧑 2026-08-16, after three of them in a row: *"theres
    /// details like this man that u missed — can u thoroughly find them"*. Fair. Every one of
    /// these was a registry entry, an asset on disk and a mix level, with nothing anywhere that
    /// fired it:
    ///
    ///  * `slipper_land` — the most common outcome in the game, 38 of 71 flights, silent.
    ///  * `lata_knockdown` — the loudest moment in the game, silent.
    ///  * `reset_channel_start` / `reset_complete` — the taya's 1.5 s channel, silent.
    ///  * `TrajectoryPreview` — built, never instantiated.
    ///  * `UiClickProbe`'s whole first pass — every button unreachable and every test green.
    ///
    /// The shape is always the same: something is CONVERTED, nothing REFERENCES it, and no
    /// compile, test or screenshot can tell. A registry plus a text search over the runtime is
    /// the cheapest thing that can, and unlike a screenshot it finds ALL of them in one run.
    ///
    /// ⚠️ IT SEARCHES THE SOURCE, NOT THE ASSEMBLY, and that is deliberate. Reflection finds
    /// methods, and the fault here is a STRING that nothing passes — a cue name, a clip name.
    /// Those do not exist as symbols and are invisible to every static tool that only sees types.
    ///
    /// ⚠️ AND IT IS AN EDIT-MODE TEST so it runs in a second, on every single test pass, rather
    /// than being a tool somebody remembers to invoke.
    /// </summary>
    public class DeadFeatureAudit
    {
        private const string RuntimeRoot = "Assets/TumbangPreso/Runtime";
        private const string SfxRoot = "Assets/TumbangPreso/Resources/Sfx";

        /// <summary>
        /// Cues that are deliberately never fired from the runtime, with the reason. Anything
        /// NOT in here has to have a call site.
        /// </summary>
        private static readonly Dictionary<string, string> AllowedSilent =
            new Dictionary<string, string>
            {
                // The alias TARGETS. The call sites use the alias names, which is the whole
                // point of `AudioCues.Aliases`; the file stem behind them is resolved at play
                // time and is not expected to appear in the source.
                { "bump",                   "alias target of hit_body" },
                { "dash",                   "alias target of bump_swing" },
                { "lata_knockdown",         "alias target of can_knockdown" },
                { "reset_channel_complete", "alias target of reset_complete" },
                { "grab",                   "alias target of pickup" },
                { "throw_whoosh",           "alias target of throw_release" },
            };

        [Test]
        public void EveryLiveCueHasAFileAndACaller()
        {
            string source = ReadRuntimeSource();

            var noFile = new List<string>();
            var noCaller = new List<string>();

            foreach (string cue in AudioCues.Live)
            {
                string stem = AudioCues.FileStemFor(cue);

                if (!File.Exists($"{SfxRoot}/{stem}.wav")) noFile.Add($"{cue} -> {stem}.wav");

                if (AllowedSilent.ContainsKey(cue)) continue;

                // The name has to appear as a STRING LITERAL somewhere in the runtime. A cue
                // mentioned only in the registry that declares it is not a cue anybody hears.
                if (!source.Contains($"\"{cue}\"")) noCaller.Add(cue);
            }

            var report = new StringBuilder();

            if (noFile.Count > 0)
                report.AppendLine("cues with no .wav on disk:\n  " + string.Join("\n  ", noFile));

            if (noCaller.Count > 0)
            {
                report.AppendLine(
                    "cues that NOTHING IN THE RUNTIME EVER PLAYS — registered, mixed, shipped " +
                    "and silent:\n  " + string.Join("\n  ", noCaller));
            }

            Assert.IsEmpty(report.ToString(), report.ToString());
        }

        /// <summary>
        /// ⚠️ THE VIEWMODEL'S OWN CLIPS. `ViewmodelArms.tscn` ships an `AnimationPlayer` with
        /// `idle`, `throw` and `grab`, and the port had only the idle breathe — so the
        /// first-person arm never moved for a throw, a pickup, a tag or the reset channel while
        /// the third-person body animated correctly for all four. 🧑: *"make sure my arm moves
        /// or does an animation when i interact with objects like in the real game"*.
        /// </summary>
        [Test]
        public void EveryViewmodelClipIsPlayedFromSomewhere()
        {
            string source = ReadRuntimeSource();

            var missing = new List<string>();

            foreach (string clip in new[] { "throw", "grab" })
                if (!source.Contains($"PlayAction(\"{clip}\")")) missing.Add(clip);

            Assert.IsEmpty(missing,
                "the viewmodel ships these clips and nothing plays them, so the player's own arm " +
                "does not move for the verb: " + string.Join(", ", missing));
        }

        /// <summary>
        /// ⚠️ AND THE VERBS THAT SHOULD SWING ONE. A tag, a shove and a punch are all things the
        /// player DOES with their arm, and all three animated the body only.
        /// </summary>
        /// <summary>
        /// ⚠️⚠️ EVERY VERB REACHES THE FIRST-PERSON ARM THROUGH **ONE** CALL SITE, and this
        /// asserts that it is still one. `character_visual.gd::play_action` opens with
        /// `rig.play_viewmodel_action(kind)` precisely *"so the two views can never disagree
        /// about whether a throw happened"*, and the first pass at this port scattered the call
        /// across `Carrier` and `CombatVerbs` instead — which is the structure the original
        /// deliberately avoids, and which a verb added later can silently forget to join.
        /// </summary>
        [Test]
        public void TheViewmodelIsDrivenFromExactlyOnePlace()
        {
            var callers = new List<string>();

            foreach (string path in Directory.GetFiles(RuntimeRoot, "*.cs",
                                                       SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(path);

                // The rig DECLARES it; anything else that names it is a second driver.
                if (Path.GetFileName(path) == "CameraRig.cs") continue;
                if (!text.Contains("PlayViewmodelAction")) continue;

                callers.Add(Path.GetFileName(path));
            }

            Assert.AreEqual(1, callers.Count,
                "the first-person arm should be driven from CharacterAnimator.PlayAction and " +
                "nowhere else, so no verb can reach one view without the other. Callers found: "
                + string.Join(", ", callers));

            Assert.AreEqual("CharacterAnimator.cs", callers[0],
                "the single caller should be CharacterAnimator.PlayAction, matching "
                + "character_visual.gd::play_action. Found: " + callers[0]);
        }

        [Test]
        public void TacticalPauseBelongsOnlyToSpectatorCamera()
        {
            string spectator = File.ReadAllText(Path.Combine(
                RuntimeRoot, "Camera", "SpectatorCamera.cs"));
            string playerMenu = File.ReadAllText(Path.Combine(
                RuntimeRoot, "UI", "PausePanel.cs"));

            StringAssert.Contains("Time.timeScale = 0.0f", spectator,
                "Spectator broadcast controls should own tactical pause");
            StringAssert.DoesNotContain("Time.timeScale = 0.0f", playerMenu,
                "The ordinary player menu must never freeze the live match");
        }

        [Test]
        public void ReplayIsLocalToTheSpectatorAndNeverStopsTheMatch()
        {
            string spectator = File.ReadAllText(Path.Combine(
                RuntimeRoot, "Camera", "SpectatorCamera.cs"));

            int start = spectator.IndexOf("private void StartReplay", StringComparison.Ordinal);
            int end = spectator.IndexOf("private void StepReplay", start, StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, "SpectatorCamera has no replay entry point");
            Assert.Greater(end, start, "the replay entry point could not be isolated for audit");

            string startReplay = spectator.Substring(start, end - start);

            StringAssert.DoesNotContain("Time.timeScale", startReplay,
                "starting a replay must not pause or slow the live match");
            StringAssert.DoesNotContain("SetPositionAndRotation", startReplay,
                "a local replay must never rewind a live actor or camera transform");
            StringAssert.DoesNotContain("ServerRpc", startReplay,
                "spectator replay is local presentation and must send no server message");
            StringAssert.DoesNotContain("ClientRpc", startReplay,
                "spectator replay is local presentation and must send no peer message");

            StringAssert.Contains("RawImage", spectator,
                "the replay should display captured pixels without touching live state");
            StringAssert.Contains("LIVE PLAY CONTINUES", spectator,
                "the spectator must be told that the match is still running behind the replay");
        }

        [Test]
        public void GuidedTrainingCoversTheLiveGameWithoutCallingVerbsForThePlayer()
        {
            string training = File.ReadAllText(Path.Combine(RuntimeRoot, "GuidedTraining.cs"));
            string panel = File.ReadAllText(Path.Combine(
                RuntimeRoot, "UI", "ConvertedTutorialPanel.cs"));

            foreach (string lesson in new[]
            {
                "Look", "Move", "Sprint", "Jump", "Throw", "Retrieve", "Pektus",
                "Shove", "AbilityInfo", "Skill1", "Skill2", "Ultimate", "DefenderReset",
                "Punch", "Lunge", "TripRecovery", "Emote",
            })
            {
                StringAssert.Contains("case Lesson." + lesson, training,
                    "guided training dropped the lesson for " + lesson);
            }

            foreach (string forbidden in new[]
            {
                "HostThrowAt(", "HostResolvePunch(", "HostResolveLunge(",
                "HostResolveShove(", "TryActivateSkill1(", "TryActivateSkill2(",
                "TryActivateUltimate(",
            })
            {
                StringAssert.DoesNotContain(forbidden, training,
                    "training completed a gameplay verb for the player instead of observing input");
            }

            StringAssert.Contains("START TRAINING", panel,
                "the existing How to Play panel has no way to launch playable training");
            Assert.AreEqual(17, GuidedTraining.LessonCount,
                "the objective counter no longer agrees with the complete training route");
        }

        private static string ReadRuntimeSource()
        {
            var text = new StringBuilder();

            foreach (string path in Directory.GetFiles(RuntimeRoot, "*.cs",
                                                       SearchOption.AllDirectories))
            {
                text.Append(File.ReadAllText(path));
            }

            return text.ToString();
        }
    }
}
