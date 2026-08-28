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
                // The rig DECLARES it; anything else that CALLS it is a second driver.
                if (Path.GetFileName(path) == "CameraRig.cs") continue;
                if (!CallsViewmodelAction(File.ReadAllText(path))) continue;

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

        /// <summary>
        /// Does this file CALL the rig's viewmodel entry point, as opposed to merely naming it?
        ///
        /// ⚠️⚠️ IT USED TO BE A BARE `text.Contains`, AND THAT COUNTS A COMMENT AS A CALLER. The
        /// assertion above is about CALL SITES, which is what its own header says and what the
        /// invariant is: one path from a verb to both views. A substring scan cannot tell a call
        /// from a mention, so the moment a file EXPLAINED the invariant by name it was counted as
        /// breaking it. That is backwards in a codebase whose commenting discipline is to name
        /// the exact thing being described, and it fired on 2026-08-28 against two comments that
        /// added no code path at all.
        ///
        /// ⚠️ IT IS A TIGHTENING, NOT A LOOSENING. Requiring the open bracket AND skipping
        /// comment lines still catches a real second caller: there is no way to invoke a method
        /// in C# without naming it and opening a bracket, and no way to hide one inside a `//`
        /// line, because a commented-out call does not run.
        /// </summary>
        private static bool CallsViewmodelAction(string text)
        {
            foreach (string line in text.Split('\n'))
            {
                string trimmed = line.TrimStart();

                // `///` is caught by this too, being a `//` prefix.
                if (trimmed.StartsWith("//") || trimmed.StartsWith("*")) continue;

                if (line.Contains("PlayViewmodelAction(")) return true;
            }

            return false;
        }

        [Test]
        public void TacticalPauseBelongsOnlyToSpectatorCamera()
        {
            string spectator = File.ReadAllText(Path.Combine(
                RuntimeRoot, "Camera", "SpectatorCamera.cs"));
            string playerMenu = File.ReadAllText(Path.Combine(
                RuntimeRoot, "UI", "PausePanel.cs"));

            // ⚠️⚠️ MATCHED AS A PATTERN, NOT AS A LITERAL, BECAUSE THE LITERAL WENT STALE
            // AND FAILED A GREEN FEATURE. This read `StringAssert.Contains("Time.timeScale =
            // 0.0f", ...)`, and `ToggleBroadcastPause` later became
            // `Time.timeScale = _broadcastPaused ? 0.0f : _selectedTimeScale` when the broadcast
            // speed keys landed. The pause was still there, still bound to P, still zeroing the
            // clock: the only thing that changed was the shape of the assignment. A source audit
            // that pins the exact spelling of a line reports a refactor as a deleted feature,
            // which is the opposite of what this file is for.
            //
            // The pattern asks the question the test's name asks: is there a write to
            // `Time.timeScale` in this file whose value can be zero. It survives a ternary, a
            // named constant and whitespace, and it still catches the removal.
            //
            // ⚠️ THE LOOKBEHIND IS LOAD-BEARING. Without it `[^;]*0(\.0+)?f` matches the `0f`
            // inside `Time.timeScale = 1.0f`, which is `PausePanel`'s RESUME line, so the second
            // assertion would fail the menu for restoring the clock rather than for stopping it.
            Assert.IsTrue(Regex.IsMatch(spectator, @"Time\.timeScale\s*=[^;]*(?<![\d.])0(\.0+)?f"),
                "Spectator broadcast controls should own tactical pause");
            Assert.IsFalse(Regex.IsMatch(playerMenu, @"Time\.timeScale\s*=[^;]*(?<![\d.])0(\.0+)?f"),
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

            // ⚠️⚠️ THIS USED TO PIN THE TOAST "LIVE PLAY CONTINUES", AND THE PREMISE UNDER IT IS
            // GONE. The replay was a picture-in-picture in the top right, so the operator needed
            // telling that the frame BEHIND it was still live. 🧑 2026-08-27: *"i alsoo really
            // dont like that instant replay on the top right"* and *"i want it to cover whole
            // screen if i click it"*. Nothing is behind it now, so a line saying the match
            // continues would be describing something the viewer cannot see.
            //
            // ⚠️ WHAT REPLACED IT IS THE PROPERTY THAT ACTUALLY MATTERED. The assertions above
            // already say the replay does not stop the clock; this says it cannot start ITSELF,
            // which is the half 🧑 reported as *"why is instant replay just spam showing"*. It
            // used to fire on every knockdown, tag and sabotage behind a 4 s floor.
            StringAssert.DoesNotContain("AutoReplayCooldown", spectator,
                "a self-starting replay is back. See the section THE REPLAY NEVER STARTS ITSELF "
                + "ANY MORE: the only trigger may be the SpectatorReplay key.");
            StringAssert.DoesNotContain("StepPendingHighlight", spectator,
                "a self-starting replay is back. The highlight queue may name the last play, "
                + "never start one.");
        }

        [Test]
        public void GuidedTrainingCoversTheLiveGameWithoutCallingVerbsForThePlayer()
        {
            string training = File.ReadAllText(Path.Combine(RuntimeRoot, "GuidedTraining.cs"));

            // ⚠️⚠️ THE ROUTE'S ENTRY POINT MOVED, AND THAT IS WHY THIS READS TWO FILES INSTEAD OF
            // ONE. It used to live on `ConvertedTutorialPanel` as a button at the bottom of a
            // six-page reference card. 🧑 2026-08-28: *"the text based tutorial is stale and should
            // be deleted and completley replaced by game tutorial"*, and *"i want it so that if u
            // click tutorial u go straight to training"*. The panel is deleted; TUTORIAL on the
            // title screen enters the route directly.
            //
            // ⚠️ BOTH HALVES ARE ASSERTED BECAUSE EITHER ONE ALONE IS SILENT. A `SceneFlow` that
            // can start training which nothing presses is a dead feature, and a menu button
            // pointing at a method that no longer sets `GuidedTutorial` is a button that loads
            // Eskinita as an ordinary practice match with no lessons in it and no error anywhere.
            string flow = File.ReadAllText(Path.Combine(RuntimeRoot, "UI", "SceneFlow.cs"));
            string menu = File.ReadAllText(Path.Combine(RuntimeRoot, "UI", "ConvertedMainMenu.cs"));

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

            StringAssert.Contains("GuidedTutorial = true", flow,
                "SceneFlow.StartTraining no longer arms the guided route, so the TUTORIAL button "
                + "loads Eskinita as an ordinary practice match with no lessons in it");

            StringAssert.Contains("SceneFlow.StartTraining", menu,
                "the title screen's TUTORIAL button no longer reaches playable training, and the "
                + "text panel that used to carry the only other way in is deleted");

            Assert.AreEqual(17, GuidedTraining.LessonCount,
                "the objective counter no longer agrees with the complete training route");
        }

        /// <summary>
        /// ⚠️⚠️ ONE PRESS IS ONE SOUND, AND FOR A WHILE IT WAS THREE. The UI click is added in
        /// three independent layers (the control on pointer down, `ConvertedScreen.WireOne` on the
        /// click it raises, and several handlers on top of that), and `AudioDirector.PlayAtVaried`
        /// has no dedupe: a map arrow started three copies of the same 40 ms recording in one
        /// frame, which sum to about +9.5 dB and read as a clipped clack. `MenuSfx.Play` is where
        /// the rule lives; this is what stops somebody "simplifying" the guard away or routing a
        /// call site straight back at the director.
        ///
        /// ⚠️ IT IS A SOURCE TEST BECAUSE THE OBSERVABLE IS A SOUND. `GameServices.Audio` is null
        /// outside a running game, so `MenuSfx.Play` is a no-op that returns void and there is
        /// nothing for an assertion to look at. What CAN be asserted is that the guard is still
        /// written down and that the one bypass that existed has not come back.
        /// </summary>
        [Test]
        public void EveryMenuSoundGoesThroughTheOncePerFrameGuard()
        {
            string sfx = File.ReadAllText(Path.Combine(RuntimeRoot, "UI", "MenuSfx.cs"));

            StringAssert.Contains("Time.frameCount", sfx,
                "MenuSfx no longer guards a cue to one play per frame, so a single press fires "
                + "the click once per layer that noticed it. See its header.");

            foreach (string file in new[] { "ConvertedScreen.cs", "GodotButton.cs",
                                            "ArrowButtonView.cs", "TextureButtonFeedback.cs" })
            {
                string text = File.ReadAllText(Path.Combine(RuntimeRoot, "UI", file));

                StringAssert.DoesNotContain("PlayAt(\"ui_", text,
                    $"{file} plays a UI cue straight at the AudioDirector, which routes around "
                    + "MenuSfx's one-per-frame guard and re-opens the stacked click.");
            }

            string screen = File.ReadAllText(Path.Combine(RuntimeRoot, "UI", "ConvertedScreen.cs"));

            StringAssert.Contains("ui_back", screen,
                "a BACK button no longer plays `ui_back`, so the button and the Escape key that "
                + "do the identical thing answer with two different sounds. See WireOne.");
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
