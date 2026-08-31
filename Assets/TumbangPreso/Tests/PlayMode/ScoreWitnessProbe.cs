using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Runs a real match and asks the one question Phase 8 rests on: does the score EVENT stream
    /// reproduce the final scoreboard exactly?
    ///
    /// ⚠️⚠️ IF IT DOES NOT, EVERY HONEST MATCH IN THE GAME READS AS DISPUTED AND NO RATING EVER
    /// MOVES, AND NOTHING ANYWHERE LOGS AN ERROR. `ScoreWitness` tallies `MatchDirector.Scored`
    /// and compares its total with the record `MatchStatsCollector` writes from
    /// `MatchDirector.ScoreFor`. Both are supposed to be the same sum of the same events, because
    /// `AddScore` is the single writer of every point in the game and announces every one of them.
    /// **"Supposed to be" is exactly the kind of claim this repository has been wrong about
    /// before**: `docs/TODO.md` § 90.5 is a whole entry about a green endpoint that had never
    /// received a single career, and § 94.2b about a server script that paid 0 XP with
    /// `applied:true`. A point awarded anywhere that does not raise `Scored` would be invisible
    /// here in exactly the same way.
    ///
    /// ⚠️ IT IS THE HALF OF PHASE 8 THAT CAN BE TESTED ON ONE MACHINE. What it cannot test is a
    /// second machine's copy of the same stream arriving intact, which is a two-laptop pass and is
    /// written into `docs/TODO.md` § 104.7 as outstanding beside § 102.5's.
    ///
    /// ⚠️ THE CAREER STORE IS DESTROYED FOR THE DURATION, for the reason
    /// `MatchRecordIdentityProbe.SuspendTheCareerStore` gives at length: the editor and the built
    /// player share `Application.persistentDataPath`, so a synthetic match reaching a live
    /// `CareerStore` writes into his real `career.json`.
    /// </summary>
    public class ScoreWitnessProbe
    {
        private const int ProbeSeat = 1;

        private GameMode _savedMode;
        private bool _savedAllBots;
        private int _savedSoloSeat;

        [SetUp]
        public void SetUp()
        {
            _savedMode = SceneFlow.SelectedMode;
            _savedAllBots = GameLaunch.AllBots;
            _savedSoloSeat = GameLaunch.SoloSeat;
        }

        /// <summary>⚠️ IT LEAVES AN EMPTY SCENE BEHIND. `MatchRecordIdentityProbe.TearDownScene`
        /// records what an arena handed to the next probe in name order costs.</summary>
        [UnityTearDown]
        public IEnumerator TearDownScene()
        {
            SceneFlow.SelectedMode = _savedMode;
            GameLaunch.AllBots = _savedAllBots;
            GameLaunch.SoloSeat = _savedSoloSeat;
            GameServices.Round?.EndRound();
            GameServices.Match?.ResetForNewMatch();
            GameServices.Round?.ResetForNewMatch();

            var blank = SceneManager.CreateScene($"WitnessProbeBlank{Time.frameCount}");
            SceneManager.SetActiveScene(blank);

            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene == blank || !scene.isLoaded) continue;

                var unload = SceneManager.UnloadSceneAsync(scene);
                yield return ProbeWait.Done(unload, "scene unload");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator TheEventStreamReproducesEverySeatsFinalScore()
        {
            SceneFlow.SelectedMode = GameMode.Classic;
            GameLaunch.AllBots = false;
            GameLaunch.SoloSeat = ProbeSeat;

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");
            for (int i = 0; i < 30; i++) yield return null;

            var career = Net.CareerStore.Instance;
            if (career != null) Object.DestroyImmediate(career);

            var runner = Object.FindFirstObjectByType<SliceRunner>();
            Assert.IsNotNull(runner, "the arena built no SliceRunner");
            if (!runner.Running) runner.Begin();
            for (int i = 0; i < 10; i++) yield return null;

            var stats = GameServices.Stats;
            var match = GameServices.Match;
            Assert.IsNotNull(stats, "there is no MatchStatsCollector");
            Assert.IsNotNull(match, "there is no MatchDirector");

            var witness = stats.GetComponent<Net.ScoreWitness>();
            Assert.IsNotNull(witness,
                "MatchStatsCollector started a match and no ScoreWitness was attached. " +
                "Nothing would ever corroborate a result and every match would read as pending " +
                "for ever, silently. docs/TODO.md § 104.2.");

            Assert.IsTrue(witness.Complete,
                "the witness says it missed the start of a match it was present for. " +
                "Complete is what decides whether this peer submits a digest at all, and a " +
                "false here is a peer that can never corroborate anything.");

            // ⚠️ THE MATCH IS PLAYED FOR A WHILE BEFORE IT IS ENDED, so there are real points to
            // compare. `MatchRecordIdentityProbe` advances straight to the end because it is
            // asking about identity; this one is asking about a SUM, and a sum of nothing is
            // equal to a different nothing.
            for (int i = 0; i < 600 && stats.Last == null; i++) yield return null;

            // ⚠️⚠️ AND THEN A KNOWN SET OF POINTS IS AWARDED THROUGH `MatchDirector.AddScore`,
            // WHICH IS THE SHIPPING PATH AND NOT A FIXTURE. This is not the probe faking a score:
            // `AddScore` is the single writer of every point in the game (`CLAUDE.md` § 4) and
            // the announcement this whole mechanism reads is made INSIDE it. Calling it is exactly
            // what a knockdown does.
            //
            // ⚠️⚠️ IT IS HERE BECAUSE THE FIRST RUN OF THIS PROBE ENDED 0-0-0-0 AND PASSED
            // EVERY COMPARISON. Ten seconds of headless bot play produced no points at all, so the
            // seat-by-seat check compared four zeroes with four zeroes and proved nothing; the
            // liveness floor below caught it, which is what liveness floors are for
            // (`CLAUDE.md` § 7.1). Awarding a deterministic, asymmetric set makes the comparison
            // sharp: **different seats end on different totals**, so a witness that tallied into
            // the wrong slot, double-counted, or read the wrong value from `MatchRules.PointsFor`
            // cannot agree with the record by coincidence.
            match.AddScore(0, ScoreEvent.LataKnocked);
            match.AddScore(0, ScoreEvent.Sabotage);
            match.AddScore(1, ScoreEvent.Tag);
            match.AddScore(2, ScoreEvent.DefenseTick);
            yield return null;

            for (int i = 0; i <= match.TotalRounds && stats.Last == null; i++)
            {
                match.AdvanceRound();
                yield return null;
            }

            var record = stats.Last;
            Assert.IsNotNull(record, "the match ended and MatchStatsCollector authored no record");

            // ---------------------------------------------------------------
            // The tally and the record agree, seat by seat.
            // ---------------------------------------------------------------
            int totalPoints = 0;

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                int fromTheRecord = record.Players[slot]?.Score ?? 0;
                int fromTheEvents = witness.ScoreFor(slot);
                totalPoints += fromTheRecord;

                Assert.AreEqual(fromTheRecord, fromTheEvents,
                    $"seat {slot} finished on {fromTheRecord} in the record and " +
                    $"{fromTheEvents} by the events. **Every honest match in the game would read " +
                    "as disputed and no rating would ever move.** A point is being awarded " +
                    "somewhere that does not go through MatchDirector.AddScore, or does not " +
                    "raise Scored, and MatchDirector's own header says that cannot happen. " +
                    "docs/TODO.md § 104.7.");
            }

            // ⚠️⚠️ AND IT REFUSES A MATCH THAT SCORED NOTHING, because four zeroes agree with four
            // zeroes and would pass this whole probe while proving nothing at all. `CLAUDE.md`
            // § 7.1: the bot probe's numbers are LIVENESS FLOORS for exactly this reason.
            Assert.Greater(totalPoints, 0,
                "the match ended with nobody having scored a single point, so the comparison " +
                "above compared nothing. Four AddScore calls were made on the shipping path " +
                "before the whistle, so this means AddScore refused them: MatchInProgress was " +
                "false, the warmup buffer was still up, or NetAuthority.ShouldResolve() said no.");

            // ⚠️ AND THE SEATS DISAGREE WITH EACH OTHER, so an agreement cannot be a
            // coincidence. Four equal totals would be satisfied by a witness that tallied every
            // event into every slot.
            Assert.AreNotEqual(record.Players[0].Score, record.Players[1].Score,
                "seat 0 and seat 1 finished level, so this run cannot tell a correct per-seat " +
                "tally from one that credits everybody with everything");

            // ---------------------------------------------------------------
            // And the digest a peer would submit matches the host's own record.
            // ---------------------------------------------------------------
            Assert.AreEqual(IntegrityRules.Digest(record), witness.Digest(record),
                "the witness derived a different digest from the record it witnessed. The scores " +
                "agree seat by seat above, so this is a placement, a winner or a ranked flag " +
                "being re-derived differently by ScoreWitness.AsWitnessed than by " +
                "MatchRecordRules.AssignPlacements.");

            // ---------------------------------------------------------------
            // A peer that missed the start says nothing rather than accusing anybody.
            // ---------------------------------------------------------------
            witness.MarkIncomplete();
            Assert.IsEmpty(witness.Digest(record),
                "a witness that has declared itself incomplete still produced a digest. " +
                "Backfill and reconnect both land peers mid-match, and a short tally from one of " +
                "them would accuse an honest host. docs/TODO.md § 104.2.");
        }
    }
}
