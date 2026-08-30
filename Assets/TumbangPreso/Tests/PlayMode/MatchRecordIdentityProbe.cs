using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Runs a real solo match to its last round and asks who the record says was playing.
    ///
    /// ⚠️⚠️ THIS IS THE PROBE THAT WOULD HAVE CAUGHT § 94.1 THE DAY PHASE 2 SHIPPED, AND
    /// NOTHING IN THE PROJECT COULD SEE IT. `Core.Tests` proves what `MatchRecordRules` does with
    /// a record it is handed; `UgsServicesProbe` proves the endpoint answers. Between those two
    /// sits the only question that mattered, which is what the game actually WRITES into a record
    /// when a real match ends, and it was wrong from the first day: every line carried
    /// `PlayerAccount.ConnectionToken`, which is the machine's local settings token whenever UGS
    /// has not signed in by the whistle, and `ugs/cloud-code/match-record.js` looks the submitter
    /// up by `context.playerId`. The endpoint threw, Cloud Code answered 422, `CareerStore` logged
    /// one warning and kept the record queued, and no career ever reached the server.
    ///
    /// ⚠️⚠️ AND IT ASSERTS THE SEAT, NOT ONLY THE ID. The same records had all four lines marked
    /// `IsBot: false` carrying one id, so even a correct id would have credited this player with
    /// whichever line came first. `MatchRecordRules.LineFor` returns the FIRST non-bot line with
    /// a matching id, so "exactly one line is mine" is a stronger and more useful claim than
    /// "some line is mine", and it is the one that was false.
    ///
    /// ⚠️ IT RUNS ON SEAT 1 RATHER THAN SEAT 0 ON PURPOSE. Seat 0 is the answer a great many
    /// wrong implementations give by accident: `Mathf.Max(0, HumanSeat)`, the first element of a
    /// list, and the first non-bot line all land there. `MatchInstaller.HumanSeat` reads
    /// `GameLaunch.SoloSeat`, so a seat that is not 0 is the only one that can tell a correct
    /// answer from a coincidence. `CLAUDE.md` § 7.1 records the same trap costing a whole
    /// probe's worth of numbers when `SoloSeat` defaulted to 1 and nobody noticed.
    /// </summary>
    public class MatchRecordIdentityProbe
    {
        private const int ProbeSeat = 1;

        private GameMode _savedMode;
        private bool _savedAllBots;
        private int _savedSoloSeat;
        private Net.CareerStore _suspendedCareer;

        [SetUp]
        public void SetUp()
        {
            _savedMode = SceneFlow.SelectedMode;
            _savedAllBots = GameLaunch.AllBots;
            _savedSoloSeat = GameLaunch.SoloSeat;
        }

        /// <summary>
        /// ⚠️⚠️ IT LEAVES AN EMPTY SCENE BEHIND, AND THE FIRST RUN OF THIS PROBE IS WHY.
        /// PlayMode cases run in one process in name order, so an arena left loaded is handed to
        /// whoever runs next. `PlayerHubLayoutProbe` looks up the menu's corner chip with
        /// `Find("Nameplate")`, and a loaded arena contains objects by that name that are not the
        /// menu's and are not under a `Canvas` at all, so it found one and dereferenced null.
        /// **The suite that changed the world is the one that has to put it back**; the
        /// alternative is every later probe learning to defend itself against every earlier one.
        /// ⚠️ `CreateScene` plus `SetActiveScene` rather than loading a menu scene, because a
        /// menu scene is not nothing either: it brings its own canvases, its own overlays and its
        /// own nameplate.
        /// </summary>
        [UnityTearDown]
        public IEnumerator TearDownScene()
        {
            SceneFlow.SelectedMode = _savedMode;
            GameLaunch.AllBots = _savedAllBots;
            GameLaunch.SoloSeat = _savedSoloSeat;
            GameServices.Round?.EndRound();
            GameServices.Match?.ResetForNewMatch();
            GameServices.Round?.ResetForNewMatch();

            var blank = SceneManager.CreateScene($"IdentityProbeBlank{Time.frameCount}");
            SceneManager.SetActiveScene(blank);

            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene == blank || !scene.isLoaded) continue;

                var unload = SceneManager.UnloadSceneAsync(scene);
                while (unload != null && !unload.isDone) yield return null;
            }

            yield return null;
        }

        /// <summary>
        /// ⚠️⚠️ THE CAREER STORE IS DESTROYED FOR THE DURATION AND THAT IS NOT TIDINESS, IT IS
        /// THE DIFFERENCE BETWEEN A PROBE AND A PROBE THAT EDITS THE PLAYER'S SAVE. The editor
        /// and the built player resolve `Application.persistentDataPath` to the SAME folder, so
        /// `MatchStatsCollector.Adopt` reaching a live `CareerStore` would count this synthetic
        /// match into the real `career.json`, into the real match history, and into the upload
        /// queue. `Adopt` null-checks `CareerStore.Instance`, so removing the component is the
        /// whole mechanism. `GameServices.Ensure` rebuilds it on the next boot.
        /// </summary>
        private void SuspendTheCareerStore()
        {
            _suspendedCareer = Net.CareerStore.Instance;
            if (_suspendedCareer != null) Object.DestroyImmediate(_suspendedCareer);
        }

        [UnityTest]
        public IEnumerator AFinishedMatchNamesExactlyOneSeatAsThisPlayer()
        {
            SceneFlow.SelectedMode = GameMode.Classic;
            GameLaunch.AllBots = false;
            GameLaunch.SoloSeat = ProbeSeat;

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;
            for (int i = 0; i < 30; i++) yield return null;

            SuspendTheCareerStore();

            var runner = Object.FindFirstObjectByType<SliceRunner>();
            Assert.IsNotNull(runner, "the arena built no SliceRunner");
            if (!runner.Running) runner.Begin();
            for (int i = 0; i < 10; i++) yield return null;

            var stats = GameServices.Stats;
            var match = GameServices.Match;
            Assert.IsNotNull(stats, "there is no MatchStatsCollector");
            Assert.IsNotNull(match, "there is no MatchDirector");

            // ⚠️ THE MATCH IS ENDED THROUGH `AdvanceRound`, WHICH IS THE PATH THE GAME USES.
            // `MatchEnded` is what `MatchStatsCollector.OnMatchEnded` hangs off, and calling that
            // handler directly would prove nothing about whether the game ever reaches it. Four
            // rounds of Classic is four advances plus the one that runs off the end.
            for (int i = 0; i <= match.TotalRounds && stats.Last == null; i++)
            {
                match.AdvanceRound();
                yield return null;
            }

            var record = stats.Last;
            Assert.IsNotNull(record,
                "the match ended and MatchStatsCollector authored no record at all");

            // ---------------------------------------------------------------
            // Exactly one seat is a person, and it is the seat this run sat in.
            // ---------------------------------------------------------------
            int humans = 0;
            foreach (var p in record.Players) if (p != null && !p.IsBot) humans++;

            Assert.AreEqual(1, humans,
                $"a solo match against three bots produced {humans} non-bot lines. Every one of " +
                "them is a person the server will try to find, and the first with a matching id " +
                "wins, so this is how a player gets credited with a bot's seat. " +
                "docs/TODO.md § 94.1.");

            var mine = record.Players[ProbeSeat];
            Assert.IsNotNull(mine, $"there is no line for seat {ProbeSeat}");
            Assert.IsFalse(mine.IsBot,
                $"seat {ProbeSeat} is the seat this run played and the record calls it a bot");

            // ---------------------------------------------------------------
            // The id is the one the endpoint compares against, and nothing else.
            // ---------------------------------------------------------------
            string me = Net.CareerStore.LocalPlayerId;
            Assert.AreEqual(me, mine.PlayerId,
                "the record stamped a different id on this player's own line than " +
                "CareerStore.LocalPlayerId, which is what every screen and every submission " +
                "looks the player up by");

            Assert.AreNotEqual(SettingsStore.Current.PlayerToken, mine.PlayerId,
                "the record carries GameSettings.PlayerToken, which is the machine's local " +
                "connection token and is never a UGS account id. `match-record.js` finds the " +
                "submitter with `p.PlayerId === context.playerId` and will answer 422 for ever. " +
                "This is the exact value found in the player's own career.json on 2026-08-30. " +
                "docs/TODO.md § 94.1.");

            Assert.AreEqual(mine, MatchRecordRules.LineFor(record, me),
                $"LineFor found a different line than seat {ProbeSeat} for this player");

            // ---------------------------------------------------------------
            // A bot carries no id, so it can never be matched by one.
            // ---------------------------------------------------------------
            for (int slot = 0; slot < record.Players.Length; slot++)
            {
                if (slot == ProbeSeat) continue;
                var bot = record.Players[slot];
                Assert.IsTrue(bot.IsBot, $"seat {slot} had nobody in it and is not marked a bot");
                Assert.IsEmpty(bot.PlayerId ?? "",
                    $"bot seat {slot} carries an account id. IdentifySeats blanks it on purpose " +
                    "so that an id can never be matched to a seat nobody sat in.");
            }

            // ---------------------------------------------------------------
            // And the whole thing is something the endpoint would accept.
            // ---------------------------------------------------------------
            Assert.AreEqual(MatchRecordRules.SubmitVerdict.Ok,
                MatchRecordRules.Submittable(record, me),
                "the record this match produced is one `match-record.js` would refuse, so it " +
                "would sit at the head of the upload queue for ever with every later match " +
                "behind it. CareerStore.DropUnsubmittable is the other half of this.");

            Debug.Log($"[MatchRecordIdentityProbe] seat {ProbeSeat} is '{mine.Handle}' " +
                      $"id '{mine.PlayerId}', three bots carry no id, record is submittable");
        }
    }
}
