using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The 2026-09-04 broadcast pass: the autopilot's event model, the replay's window, and the
    /// six ultimate introductions.
    ///
    /// ⚠️⚠️ THREE OF THESE ARE SOURCE-TEXT CHECKS AND THAT IS THE STRONGEST FORM AVAILABLE FOR
    /// WHAT THEY ASSERT. 🧑's constraint is *"spectator autopilot may never trigger replay,
    /// pause, or time-scale controls"*, and there is no runtime state that means "the autopilot
    /// did not do a thing": a test that engaged the autopilot and watched for a replay would
    /// prove only that it did not happen in that run, on that seed, in those seconds. **Reading
    /// the file and asserting the call does not exist proves it cannot happen at all.**
    /// `SceneScriptCheck` and `InputSurfaceCheck` both make this argument one level up, and
    /// `DeadFeatureAudit` already greps `SpectatorCamera` for two deleted constant names.
    ///
    /// ⚠️ THEY RUN IN EDITMODE, IN MILLISECONDS, AND THAT IS `docs/TODO.md` § 124.11'S RULE:
    /// *"a bound only a twelve-minute PlayMode run can enforce is a bound somebody edits a
    /// string past on a Friday."*
    /// </summary>
    public sealed class BroadcastPassTests
    {
        private const string DirectorPath =
            "Assets/TumbangPreso/Runtime/Camera/SpectatorDirector.cs";

        private const string InterestPath =
            "Assets/TumbangPreso/Runtime/Camera/SpectatorInterest.cs";

        private const string CameraPath =
            "Assets/TumbangPreso/Runtime/Camera/SpectatorCamera.cs";

        private static string Source(string path)
        {
            string full = Path.Combine(Directory.GetCurrentDirectory(), path);
            Assert.IsTrue(File.Exists(full), $"{path} is missing.");
            return File.ReadAllText(full);
        }

        /// <summary>
        /// The file with every comment removed.
        ///
        /// ⚠️⚠️ WITHOUT THIS EVERY CHECK BELOW IS A FALSE POSITIVE, AND THE REASON IS THIS
        /// REPOSITORY'S OWN DISCIPLINE. `CLAUDE.md` § 3 asks for the WHY *"at length, in ⚠️-marked
        /// notes above the thing"*, so `SpectatorDirector`'s header explains, in prose, that
        /// *"nothing in this file calls `ToggleBroadcastPause`, `StartReplay` or
        /// `SetBroadcastScale`"*, and a naive `Contains` on the raw text sees all three names and
        /// fails the file for documenting the very rule it obeys. **A source check has to read
        /// code, not commentary.**
        ///
        /// ⚠️ IT IS DELIBERATELY CRUDE: `//` to end of line, and `/* */` spans. This project has
        /// no string literal containing a comment marker, and a real C# lexer here would be a
        /// hundred lines protecting against a case that does not exist.
        /// </summary>
        private static string Code(string path)
        {
            string src = Source(path);
            var kept = new System.Text.StringBuilder(src.Length);

            bool inBlock = false;

            foreach (string line in src.Split('\n'))
            {
                string work = line;

                if (inBlock)
                {
                    int close = work.IndexOf("*/", System.StringComparison.Ordinal);
                    if (close < 0) continue;

                    work = work.Substring(close + 2);
                    inBlock = false;
                }

                int open = work.IndexOf("/*", System.StringComparison.Ordinal);
                if (open >= 0)
                {
                    inBlock = true;
                    work = work.Substring(0, open);
                }

                int slash = work.IndexOf("//", System.StringComparison.Ordinal);
                if (slash >= 0) work = work.Substring(0, slash);

                kept.Append(work).Append('\n');
            }

            return kept.ToString();
        }

        // -------------------------------------------------------------------
        // § THE AUTOPILOT HAS NO HANDS
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ 🧑 2026-08-27: *"dont let autopilot spectator pause or replay thats for human
        /// only"*, and the 2026-09-03 brief repeats it: *"spectator autopilot may never trigger
        /// replay, pause, or time-scale controls."* Both files that make up the autopilot are
        /// read, because the interest model is new and is the obvious place for a future session
        /// to reach for a replay when it notices a big play.
        /// </summary>
        [Test]
        public void TheAutopilotCannotReplayPauseOrChangeTime()
        {
            string[] forbidden =
            {
                "StartReplay",
                "ToggleBroadcastPause",
                "SetBroadcastScale",
                "RequestBroadcastScale",
                "ProbeReplayRequest",
                "Time.timeScale",
                "Hitstop.",
            };

            foreach (string path in new[] { DirectorPath, InterestPath })
            {
                string src = Code(path);

                foreach (string name in forbidden)
                    Assert.IsFalse(src.Contains(name),
                        $"{path} names `{name}`. The autopilot may fly the camera and nothing "
                        + "else: see `SpectatorDirector`'s header and `docs/TODO.md` § 134.");
            }
        }

        /// <summary>
        /// ⚠️ THE AUTOPILOT WRITES A POSE AND NOTHING ELSE. `SpectatorCamera`'s header makes this
        /// the condition on a cinematic auto-cam existing at all, and `CLAUDE.md` § 4 makes the
        /// same separation for bots: a decision layer may never call a gameplay method.
        /// </summary>
        [Test]
        public void TheAutopilotTouchesNoGameplayState()
        {
            string src = Code(DirectorPath) + Code(InterestPath);

            string[] forbidden =
            {
                "InputIntent",
                "AddScore",
                "MatchRpc",
                "ServerRpc",
                "HostResolve",
                "AddComponent<Collider",
            };

            foreach (string name in forbidden)
                Assert.IsFalse(src.Contains(name),
                    $"the autopilot names `{name}`. It writes a pose; it owns nothing.");
        }

        /// <summary>
        /// ⚠️⚠️ REPLAY REMAINS MANUAL, AND THE SHAPE OF THE PROOF IS THAT THERE IS EXACTLY ONE
        /// CALL. 🧑 2026-08-27, with two screenshots: *"why is instant replay just spam
        /// showing"*. The cause was four independent triggers, and the fix was to delete all of
        /// them and keep the key. A second call site is how that comes back.
        /// </summary>
        [Test]
        public void ReplayHasExactlyOneTriggerAndItIsAKeyPress()
        {
            var calls = new List<string>();

            foreach (string line in Code(CameraPath).Split('\n'))
            {
                if (!line.Contains("StartReplay(")) continue;

                // The declaration is not a call.
                if (line.Contains("void StartReplay(")) continue;

                calls.Add(line.Trim());
            }

            Assert.AreEqual(1, calls.Count,
                "`StartReplay` should be reached from exactly one place, the bound replay key in "
                + "`StepBroadcastKeys`. Every automatic trigger was deleted on 2026-08-27 and "
                + "`docs/TODO.md` § 134.6 is why. Found: " + string.Join(" | ", calls));
        }

        // -------------------------------------------------------------------
        // § THE EVENT MODEL
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE PRIORITY IS `docs/VISION.md` § 0 AND NOT A RANKING OF HOW MUCH THINGS MOVE.
        /// *"The tension is the retrieval, not the throw."* If a future session ever reorders
        /// this enum so that a throw being charged outranks a retrieval under pressure, the
        /// camera stops being about this sport.
        /// </summary>
        [Test]
        public void TheRetrievalOutranksEveryOtherBeat()
        {
            Assert.AreEqual(0, (int)SpectatorBeat.Retrieval);

            Assert.Less((int)SpectatorBeat.Retrieval, (int)SpectatorBeat.Ultimate);
            Assert.Less((int)SpectatorBeat.Ultimate, (int)SpectatorBeat.LataHit);
            Assert.Less((int)SpectatorBeat.LataHit, (int)SpectatorBeat.Tag);
            Assert.Less((int)SpectatorBeat.Tag, (int)SpectatorBeat.SlipperLanded);
            Assert.Less((int)SpectatorBeat.SlipperLanded, (int)SpectatorBeat.Downed);
            Assert.Less((int)SpectatorBeat.Downed, (int)SpectatorBeat.Reset);
            Assert.Less((int)SpectatorBeat.Reset, (int)SpectatorBeat.ThrowPrep);
            Assert.Less((int)SpectatorBeat.ThrowPrep, (int)SpectatorBeat.Quiet);
        }

        /// <summary>
        /// ⚠️⚠️ NINE BEATS AND NINE COMPOSITIONS, AND THE SECOND NUMBER IS THE POINT OF THE PASS.
        /// The director this replaced had ONE camera solve and used it for everything, which is
        /// what made it read as automatic. A future session that deletes shot types to "simplify"
        /// is undoing the whole thing, and the brief says so: *"do not implement every shot as
        /// the same follow camera at different distances."*
        /// </summary>
        [Test]
        public void ThereAreNineDistinctShotCompositions()
        {
            var shots = (ShotType[])System.Enum.GetValues(typeof(ShotType));
            Assert.AreEqual(9, shots.Length);

            // ⚠️ AND EACH ONE IS ACTUALLY SOLVED DIFFERENTLY. A shot type nothing branches on is
            // a name rather than a composition, which is exactly the state the old director was
            // in with one solve and several distances.
            string src = Code(DirectorPath);

            foreach (var shot in shots)
            {
                if (shot == ShotType.Pov) continue;   // Reserved; see the enum's note.

                Assert.IsTrue(src.Contains("ShotType." + shot),
                    $"{shot} is declared and never composed. Either solve it or delete it.");
            }
        }

        /// <summary>
        /// ⚠️ THE COMMITMENT WINDOW IS WHAT STOPS THE CAMERA CUTTING OFF ITS OWN PLAY, so a
        /// duration shorter than the commitment is a beat that can never finish.
        /// </summary>
        [Test]
        public void NoBeatCommitsForLongerThanItIsExpectedToLast()
        {
            Assert.Greater(SpectatorInterestModel.RetrievalSeconds,
                           SpectatorInterestModel.MinCommit);
            Assert.Greater(SpectatorInterestModel.UltimateSeconds,
                           SpectatorInterestModel.MinCommit);
            Assert.Greater(SpectatorInterestModel.TagSeconds,
                           SpectatorInterestModel.MinCommit);
            Assert.Greater(SpectatorInterestModel.QuietSeconds,
                           SpectatorInterestModel.MinCommit);
        }

        /// <summary>
        /// ⚠️⚠️ A HELD BEAT SURVIVES ITS OWN RESOLUTION BY A BEAT, AND THAT IS THE OUTCOME FRAME.
        /// A retrieval stops being true on exactly the frame it resolves, which is the frame the
        /// viewer has been waiting for; a camera driven by the raw condition cuts there and shows
        /// the run while hiding the result.
        /// </summary>
        [Test]
        public void TheOutcomeGraceIsLongEnoughToSeeAResult()
        {
            // Constructed with a start time a second in the past: still inside the grace.
            var interest = new SpectatorInterest(
                SpectatorBeat.Retrieval, null, null, Vector3.zero, false,
                ShotType.RetrievalTwoShot, Time.unscaledTime,
                SpectatorInterestModel.RetrievalSeconds, SpectatorInterestModel.MinCommit,
                "test");

            Assert.IsTrue(interest.Committed, "a fresh beat is committed.");
            Assert.IsFalse(interest.Expired, "and it has not expired.");
        }

        // -------------------------------------------------------------------
        // § THE REPLAY WINDOW
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE CLIP MUST BE ABLE TO CONTAIN ITS OWN EVENT, AND THE OLD BUFFER COULD NOT.
        /// The window is 3.5 s of approach plus 1.3 s of outcome = 4.8 s, and the operator's
        /// reaction time comes out of the remaining buffer. At the old 7.0 s an operator who
        /// took more than 2.2 s to press had already lost the approach. **This asserts the
        /// arithmetic rather than the constants**, so raising the frame size or dropping the
        /// capacity fails here rather than silently truncating clips.
        /// </summary>
        [Test]
        public void TheReplayBufferHoldsTheWholeWindowPlusAnOperatorsReactionTime()
        {
            string src = Source(CameraPath);

            float interval = Number(src, "ReplaySampleInterval = ");
            float capacity = Number(src, "ReplayFrameCapacity = ");
            float leadIn = Number(src, "LeadInSeconds = ");
            float leadOut = Number(src, "LeadOutSeconds = ");

            float buffer = interval * capacity;
            float window = leadIn + leadOut;

            Assert.GreaterOrEqual(buffer, window + 3.0f,
                $"the buffer holds {buffer:0.0} s and the window is {window:0.0} s. That leaves "
                + "under three seconds for an operator to decide something was worth watching "
                + "again, which is not enough. See `docs/TODO.md` § 134.6.");
        }

        /// <summary>
        /// ⚠️ THE BUFFER IS HELD FOR THE WHOLE MATCH AND ANDROID IS A SHIPPING PLATFORM. Nothing
        /// measured this before 2026-09-04 and it was 86 MB.
        /// </summary>
        [Test]
        public void TheReplayBufferStaysUnderFiftyMegabytes()
        {
            string src = Source(CameraPath);

            float width = Number(src, "ReplayWidth = ");
            float height = Number(src, "ReplayHeight = ");
            float capacity = Number(src, "ReplayFrameCapacity = ");

            // RGB565 is two bytes a pixel. A change to RGB24 fails this, which is the point.
            bool rgb565 = src.Contains("TextureFormat.RGB565");
            float bytesPerPixel = rgb565 ? 2.0f : 3.0f;

            float megabytes = width * height * bytesPerPixel * capacity / (1024.0f * 1024.0f);

            Assert.Less(megabytes, 50.0f,
                $"the replay ring holds {megabytes:0.0} MB of Texture2D for the whole match. "
                + "`docs/TODO.md` § 134.6 has the arithmetic that got it from 86 MB to 46.");
        }

        private static float Number(string src, string marker)
        {
            int at = src.IndexOf(marker, System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(at, 0, $"`{marker}` is gone from the source.");

            int start = at + marker.Length;
            int end = start;

            while (end < src.Length
                   && (char.IsDigit(src[end]) || src[end] == '.' || src[end] == 'f'))
                end++;

            string text = src.Substring(start, end - start).TrimEnd('f');
            Assert.IsTrue(float.TryParse(text, System.Globalization.NumberStyles.Float,
                                         System.Globalization.CultureInfo.InvariantCulture,
                                         out float value),
                          $"could not read a number after `{marker}`: got '{text}'.");
            return value;
        }

        // -------------------------------------------------------------------
        // § THE SIX ULTIMATE INTRODUCTIONS
        // -------------------------------------------------------------------

        private static readonly string[] Heroes =
            { "dante", "cheska", "sean", "zack", "nemu", "phaister" };

        /// <summary>
        /// ⚠️⚠️ ALL SIX, BY NAME, READ OFF THE KITS. The introduction card prints
        /// `HeroKit.HeroName` and `HeroAbility.Name` rather than a table of its own, so this
        /// asserts the thing the card will actually draw. A seventh hero with no ultimate name
        /// fails here rather than shipping a blank card.
        /// </summary>
        [Test]
        public void AllSixHeroesHaveTheirOwnNamedUltimate()
        {
            var expected = new Dictionary<string, string>
            {
                { "dante", "TITAN FISSURE" },
                { "cheska", "GLACIAL NOVA" },
                { "sean", "SUPERNOVA" },
                { "zack", "THUNDERSTRIKE" },
                { "nemu", "DEVOURING SEANCE" },
                { "phaister", "GRAND COVEN" },
            };

            var seen = new HashSet<string>();

            foreach (string hero in Heroes)
            {
                var kit = HeroAbilitySystem.CreateKitFor(hero);
                Assert.IsNotNull(kit, $"{hero} has no kit.");
                Assert.IsNotNull(kit.Ultimate, $"{hero} has no ultimate.");

                Assert.AreEqual(expected[hero], kit.Ultimate.Name,
                    $"{hero}'s ultimate is named differently from what the card expects.");

                Assert.IsTrue(seen.Add(kit.Ultimate.Name),
                    $"{kit.Ultimate.Name} is used twice. Two heroes sharing an ultimate name is "
                    + "`docs/TODO.md` § 8 item 3's fault: two heroes reading as one costs a "
                    + "character.");

                Assert.IsNotEmpty(kit.HeroName, $"{hero} has no display name for the card.");
            }
        }

        /// <summary>
        /// ⚠️⚠️ EACH HERO'S CARD CARRIES A DIFFERENT SILHOUETTE, NOT JUST A DIFFERENT TINT.
        /// `CLAUDE.md` § 6.5: *"a shape difference survives a photograph and a colourblind
        /// player; a fill difference does not."* Six cards differing only in accent are one card.
        /// </summary>
        [Test]
        public void EveryHeroHasItsOwnUltimateMotif()
        {
            var seen = new Dictionary<string, string>();

            foreach (string hero in Heroes)
            {
                var sprite = UltimateMotifs.For(hero);
                Assert.IsNotNull(sprite, $"{hero} has no motif.");

                // The bake names itself after the hero, so two heroes resolving to one strip
                // would be visible as a shared name.
                Assert.IsFalse(seen.ContainsKey(sprite.name),
                    $"{hero} and {seen.GetValueOrDefault(sprite.name)} draw the same motif.");

                seen[sprite.name] = hero;
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE INTRODUCTION MAY NOT TAKE THE GAME AWAY FROM THE PLAYER, AND THE BRIEF LISTS
        /// THE SIX WAYS IT COULD. This is a source check for the same reason the autopilot ones
        /// are: there is no runtime state meaning "the card did not stop time".
        /// </summary>
        [Test]
        public void TheUltimateIntroductionNeverInterruptsPlay()
        {
            string src = Code("Assets/TumbangPreso/Runtime/UI/UltimatePresentationDirector.cs");

            string[] forbidden =
            {
                "Time.timeScale",
                "Hitstop.",
                "Cursor.lockState",

                // ⚠️⚠️ `Camera`, NOT `enabled = false`. The first draft of this list forbade the
                // latter and **failed on the director switching off its own canvas**, which is
                // the one thing it is supposed to do when the card is finished. A rule that
                // catches correct behaviour is a rule the next person deletes rather than reads.
                // What actually matters is that the introduction never touches a CAMERA: the
                // brief's hard line is *"do not cut the gameplay camera"*, and a class that never
                // names the type cannot cut one.
                "Camera",

                // Nothing gameplay-side may be switched off, including the HUD or a body.
                "SetActive(false)",
            };

            foreach (string name in forbidden)
                Assert.IsFalse(src.Contains(name),
                    $"the ultimate introduction names `{name}`. The player keeps control and "
                    + "aiming: see `docs/TODO.md` § 134.7.");

            // ⚠️ AND IT MUST NOT EAT A CLICK. § 100 records what a full-screen graphic silently
            // blocking presses cost on the boot screen.
            Assert.IsTrue(src.Contains("blocksRaycasts = false"),
                "the introduction canvas must not take presses.");
        }

        /// <summary>
        /// ⚠️ ONE CAST, ONE INTRODUCTION. The duplicate window exists because a client that
        /// predicted a cast and then received the host's confirmation runs the presentation
        /// twice, deliberately, and that is right for the effect and wrong for a title card.
        /// </summary>
        [Test]
        public void TheDuplicateWindowIsLongerThanANetworkRoundTripAndShorterThanTheCard()
        {
            Assert.Greater(UltimatePresentationDirector.DuplicateWindow, 0.1f);
            Assert.Less(UltimatePresentationDirector.DuplicateWindow,
                        UltimatePresentationDirector.PlayerSeconds,
                        "a duplicate window longer than the card itself would swallow a second "
                        + "hero's genuine ultimate.");
        }

        /// <summary>
        /// ⚠️ THE CARD SITS INSIDE THE 0.5 TO 0.9 s THE BRIEF ASKS FOR, and the spectator's is
        /// longer because a spectator is not aiming at anything.
        /// </summary>
        [Test]
        public void TheIntroductionIsShortEnoughToBeNonBlocking()
        {
            Assert.GreaterOrEqual(UltimatePresentationDirector.PlayerSeconds, 0.5f);
            Assert.LessOrEqual(UltimatePresentationDirector.PlayerSeconds, 0.9f);

            Assert.Greater(UltimatePresentationDirector.SpectatorSeconds,
                           UltimatePresentationDirector.PlayerSeconds);

            // It must not survive its own shot: `MinShotSeconds` is 2.4 s.
            Assert.Less(UltimatePresentationDirector.SpectatorSeconds,
                        SpectatorDirector.MinShotSeconds);

            // The ramps come out of the budget rather than being added to it.
            Assert.Less(UltimatePresentationDirector.EnterSeconds
                        + UltimatePresentationDirector.ExitSeconds,
                        UltimatePresentationDirector.PlayerSeconds);
        }

        /// <summary>
        /// ⚠️⚠️ NO SPOKEN CALLOUT AND NO NEW SOUND. 🧑's constraint for this pass, twice: *"no
        /// spoken or Tagalog ultimate callouts"* and *"do not replace or regenerate VFX or
        /// SFX"*. The cast cue, the hero theme and the column all still fire from
        /// `HeroAbilitySystem` and are untouched; the presentation director draws two words.
        /// </summary>
        [Test]
        public void TheIntroductionPlaysNoAudioOfItsOwn()
        {
            string src = Code("Assets/TumbangPreso/Runtime/UI/UltimatePresentationDirector.cs");

            string[] forbidden = { "NetCue", "AudioSource", "PlayAt(", "AudioCues." };

            foreach (string name in forbidden)
                Assert.IsFalse(src.Contains(name),
                    $"the ultimate introduction names `{name}`. It is a graphic; every cue it "
                    + "could want already fires from `HeroAbilitySystem.PlayUltimatePresentation`.");
        }
    }
}
