using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// § 149.8's OPEN HALF: THE SECOND MATCH IN ONE PROCESS.
    ///
    /// ⚠️⚠️ EVERY OTHER LIFECYCLE MEASUREMENT IN THIS REPOSITORY IS OF A SYNTHETIC WORLD OR OF A
    /// FIRST MATCH, AND THE GAP BETWEEN THEM IS EXACTLY WHERE § 149.8's DEFECT LIVED.
    /// `MatchSoakProbe` runs six matches and is the strongest thing here, but it builds its own
    /// root and its own runner: it never touches `GameLaunch`, never spectates, and never leaves
    /// through `SceneFlow.LeaveMatchToMainMenu`, **which is the single exit every real player
    /// uses** (the pause panel, the results board and both result screens all come through it).
    /// So the launch block could outlive a match and no probe in the suite could see it.
    ///
    /// ⚠️⚠️ AND THE FIELD WITH TEETH IS `GameLaunch.Spectator`. `MatchInstaller.HumanSeat`
    /// answers **-1** while it is set, and the installer reads that to decide who gets a
    /// `PlayerInputReader` and which seats get an `AIController`. A player who spectated one
    /// match and then started a solo one got an arena **in which nobody was driving their seat**.
    /// `ConvertedMatchSetup` cleared it on the way into the lobby, which covers the lobby route
    /// and not the ones that skip it.
    ///
    /// ⚠️ THE SHAPE OF THE STRESS IS THE BRIEF'S: a first match with bots and a spectator, torn
    /// down, then a second with materially different settings and a real player in a seat.
    /// </summary>
    public class SecondMatchLifecycleProbe
    {
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code. `PlayModeWorld.Reset` has the
        /// mechanism and why BOTH hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        /// <summary>
        /// ⚠️⚠️ THE WHOLE LAUNCH BLOCK, THROUGH THE REAL EXIT, WITH THE ONE DELIBERATE EXCEPTION
        /// ASSERTED AS WELL.
        ///
        /// This drives `SceneFlow.LeaveMatchToMainMenu` rather than `GameLaunch.Reset` directly,
        /// on purpose: the defect § 149.8 found was not that `Reset` did the wrong thing, it was
        /// that **the exit never called it**. A test that calls `Reset` itself passes against the
        /// bug, which is `SessionRestartTests`' own lesson one level up (*"it asserts the second
        /// start, not the first. A test that only hosts once passes against the bug"*).
        ///
        /// ⚠️⚠️ `GameLaunch.AllBots` MUST SURVIVE, AND THAT IS ASSERTED HERE SO SOMEBODY "TIDYING"
        /// IT INTO `Reset()` FAILS RATHER THAN SILENTLY BREAKING EVERY MULTI-MATCH HARNESS. It is
        /// written by `-tp-allbots` and belongs to the PROCESS rather than to a match:
        /// `tools/net_matrix.py` and `tools/referee_run.py` would measure three parked bodies from
        /// their second match onwards. `TournamentGuard` is what clears it for a bracket match.
        /// </summary>
        [UnityTest]
        public IEnumerator LeavingAMatchClearsTheLaunchBlockAndKeepsTheProcessSwitch()
        {
            bool allBotsBefore = GameLaunch.AllBots;

            try
            {
                // ---- match 1: a spectated Hero Strike room with bots and a reconnect claim ----
                GameLaunch.Spectator = true;
                GameLaunch.GuidedTutorial = true;
                GameLaunch.PendingAction = "join";
                GameLaunch.PendingJoinAddress = "203.0.113.9";
                GameLaunch.PendingStatusMessage = "joining";
                GameLaunch.SeatTokens[2] = "seat-two-claim";
                GameLaunch.AllBots = true;
                PracticeSandbox.Wanted = true;

                UI.SceneFlow.LeaveMatchToMainMenu();

                // The exit loads the menu scene; give it the frames to actually arrive so this
                // measures the state a SECOND match would start from rather than the state one
                // frame into leaving the first.
                for (int i = 0; i < 20; i++) yield return null;

                Assert.IsFalse(GameLaunch.Spectator,
                    "GameLaunch.Spectator survived the single exit. MatchInstaller.HumanSeat "
                    + "answers -1 while it is set, so the next match installs an arena with "
                    + "nobody driving the player's seat. docs/TODO.md § 149.8.");

                Assert.IsFalse(GameLaunch.GuidedTutorial,
                    "GameLaunch.GuidedTutorial survived the single exit, so the next match parks "
                    + "three seats and runs the tutorial's cast.");

                Assert.IsEmpty(GameLaunch.PendingAction, "PendingAction survived the single exit.");
                Assert.IsEmpty(GameLaunch.PendingJoinAddress,
                    "PendingJoinAddress survived the single exit, so the next launch re-dials a "
                    + "room this player walked out of.");

                Assert.IsEmpty(GameLaunch.SeatTokens,
                    "SeatTokens survived the single exit. It is a reconnect claim on the match "
                    + "being left; somebody returning to the main menu is giving it up.");

                Assert.IsFalse(PracticeSandbox.Wanted,
                    "PracticeSandbox.Wanted survived the single exit, so the next room draws a "
                    + "lit NO COOLDOWNS toggle where the switch can never apply.");

                Assert.IsTrue(GameLaunch.AllBots,
                    "GameLaunch.AllBots was cleared by the exit. It belongs to the PROCESS and "
                    + "not to a match: -tp-allbots asked for a driven session and every "
                    + "multi-match harness expects the second match to be driven too. It is "
                    + "deliberately NOT in GameLaunch.Reset(); see that method and § 149.8.");
            }
            finally
            {
                GameLaunch.AllBots = allBotsBefore;
                GameLaunch.Reset();
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE DEFECT ITSELF, STATED AS THE THING A PLAYER WOULD NOTICE: after spectating,
        /// the next match has somebody in the seat.
        ///
        /// The assertion above is about a flag. This one is about what the flag DOES, which is
        /// the half that would still be wrong if `HumanSeat` learned a second way to answer -1.
        /// `CLAUDE.md` § 4a's whole argument: a probe that asserts the plate is on screen is not
        /// making the same claim as "somebody can reach it".
        /// </summary>
        [UnityTest]
        public IEnumerator AfterSpectatingOneMatchTheNextOneHasSomebodyInTheSeat()
        {
            bool allBotsBefore = GameLaunch.AllBots;

            // ⚠️ BUILT AFTER THE EXIT, NOT BEFORE IT, AND THE FIRST VERSION OF THIS TEST GOT IT
            // WRONG. `LeaveMatchToMainMenu` LOADS A SCENE, so an object made before the call is
            // destroyed by the load and `AddComponent` on it throws `MissingReferenceException`
            // from the harness rather than failing the assertion. `MatchSoakProbe` records the
            // same lesson in its own words: *"a check whose first run accuses the game of the
            // harness's mistake teaches everybody to distrust the harness."*
            GameObject probe = null;

            try
            {
                // ⚠️ `AllBots` OFF FOR THIS ONE, AND IT IS NOT A CONTRADICTION WITH THE TEST
                // ABOVE. That switch legitimately answers -1 too, so leaving it set would make
                // this assertion pass for the wrong reason and go on passing if `Spectator`
                // started leaking again.
                GameLaunch.AllBots = false;
                GameLaunch.Spectator = true;

                UI.SceneFlow.LeaveMatchToMainMenu();
                for (int i = 0; i < 20; i++) yield return null;

                // A fresh installer answers the question the next arena would ask it. Its `Start`
                // is what builds seats, and it is not reached here: `HumanSeat` is a pure read of
                // the launch block plus the live session, which is exactly what is under test.
                probe = new GameObject("~seat-question");
                var installer = probe.AddComponent<MatchInstaller>();

                Assert.GreaterOrEqual(installer.HumanSeat, 0,
                    "MatchInstaller.HumanSeat is -1 entering the second match, so no seat gets a "
                    + "PlayerInputReader and every seat gets an AIController: the player watches "
                    + "four bots play. This is § 149.8's spectator leak, reached through the "
                    + "single exit rather than through the lobby.");

                Assert.Less(installer.HumanSeat, Balance.PlayerCount,
                    "HumanSeat is outside the seat range, which installs nothing at all.");
            }
            finally
            {
                if (probe != null) Object.DestroyImmediate(probe);
                GameLaunch.AllBots = allBotsBefore;
                GameLaunch.Reset();
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE `sceneLoaded` SUBSCRIPTION QUESTION, ANSWERED BY IDENTITY RATHER THAN BY
        /// READING THE CODE.
        ///
        /// `AudioDirector.Awake` subscribes to `SceneManager.sceneLoaded` with an **anonymous
        /// delegate**, which can never be unsubscribed. That is only safe if exactly one
        /// `AudioDirector` is ever constructed in a process: a second one would add a second
        /// permanent handler, and the first one's captured `this` would by then be a destroyed
        /// MonoBehaviour, so every later scene load would run `KeepOneListener` against a dead
        /// object.
        ///
        /// **The claim that makes it safe is `GameServices.Ensure`'s `if (_root != null) return;`
        /// over a `DontDestroyOnLoad` + `HideAndDontSave` root**, and this asserts it across the
        /// exact boundary that would break it: leaving a match, loading another scene, and asking
        /// again. ⚠️ IDENTITY, NOT A COUNT, because the root is `HideAndDontSave` and
        /// `FindObjectsByType` **cannot see it**: `AudioDirector.Awake`'s own header records two
        /// enabled listeners coexisting unseen for a whole session for exactly that reason, so a
        /// count would measure zero and pass no matter what.
        /// </summary>
        [UnityTest]
        public IEnumerator TheAudioDirectorIsBuiltOnceForTheProcessSoItsSceneHandlerCannotAccumulate()
        {
            GameServices.Ensure();

            var audioBefore = GameServices.Audio;
            var matchBefore = GameServices.Match;

            Assert.IsNotNull(audioBefore, "GameServices.Ensure() built no AudioDirector.");

            UI.SceneFlow.LeaveMatchToMainMenu();
            for (int i = 0; i < 20; i++) yield return null;

            GameServices.Ensure();

            Assert.AreSame(audioBefore, GameServices.Audio,
                "a SECOND AudioDirector exists after a match boundary. Its Awake subscribes to "
                + "SceneManager.sceneLoaded with an anonymous delegate that nothing can remove, "
                + "so the handlers now accumulate for the life of the process and the older one "
                + "runs KeepOneListener against a destroyed component on every scene load.");

            Assert.AreSame(matchBefore, GameServices.Match,
                "a second MatchDirector exists after a match boundary. It is DontDestroyOnLoad "
                + "and every subscription in the game points at the first one.");

            GameLaunch.Reset();
        }

        /// <summary>
        /// ⚠️ BOT DIFFICULTY IS A PROCESS STATIC AND IS **RE-DERIVED** RATHER THAN INHERITED,
        /// WHICH IS WHY IT IS NOT IN `GameLaunch.Reset()` AND MUST NOT BE ADDED TO IT.
        ///
        /// `AIController.ActiveDifficulty` and `AIController.BotsEnabled` are both statics and
        /// both survive a match by construction. They are safe because **`MatchInstaller.Start`
        /// calls `ApplyDifficultyFromSettings()` before it builds a single seat**, and every
        /// screen that chooses a difficulty (`CustomGameScreen.SetBots`, `ConvertedMatchSetup`)
        /// writes the saved setting first. So the second match reads the setting, not the
        /// leftover.
        ///
        /// ⚠️ THIS IS THE "PROVE IT IS FINE" HALF OF THE AUDIT AND IT IS WORTH A TEST. § 149's
        /// standing rule: *"A lead that turned out to be a false positive closes with the proof
        /// that it is one and the test that keeps it that way."* If somebody ever removes that
        /// call from the installer, a NONE-bots practice match would leak an empty arena into the
        /// next real one, and this is what would say so.
        /// </summary>
        [UnityTest]
        public IEnumerator BotDifficultyIsReDerivedForEachMatchRatherThanInheritedFromTheLastOne()
        {
            var difficultyBefore = AIController.ActiveDifficulty;
            bool enabledBefore = AIController.BotsEnabled;

            try
            {
                // The state a practice match set to NONE leaves behind.
                AIController.BotsEnabled = false;
                AIController.ActiveDifficulty = Difficulty.Astig;

                AIController.ApplyDifficultyFromSettings();

                int saved = Settings.SettingsStore.Current.AiDifficulty;

                Assert.AreEqual(saved != AIController.NoBotsIndex, AIController.BotsEnabled,
                    "BotsEnabled did not follow the saved setting, so a NONE-bots practice match "
                    + "leaks an empty arena into the next real one. MatchInstaller.Start calls "
                    + "this before it builds a seat, and that call is the whole reason these "
                    + "statics are safe to leave out of GameLaunch.Reset().");

                Assert.AreEqual((Difficulty)Mathf.Clamp(saved, 0, 2), AIController.ActiveDifficulty,
                    "ActiveDifficulty did not follow the saved setting, so the previous match's "
                    + "tier is still tuning every bot in this one.");
            }
            finally
            {
                AIController.ActiveDifficulty = difficultyBefore;
                AIController.BotsEnabled = enabledBefore;
            }

            yield return null;
        }
    }
}
