using System.Collections;
using System.IO;
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
    /// The per-match frame-rate sampler, in a real arena. `docs/TODO.md` § 90.3's last bullet.
    ///
    /// ⚠️⚠️ THE UNIT TESTS OWN THE ARITHMETIC AND THIS OWNS THE WINDOW. `Core.Tests`'
    /// `FrameRateTests` proves that a stutter is visible, that a band edge sits at 50 and that a
    /// zero-length frame is dropped, in about a millisecond and with no engine at all. None of
    /// that can answer the only question that made the frame rate the open half of Phase 3: does
    /// the sampler run over the frames of a MATCH and no others. A percentile that quietly
    /// included the loading screen would pass every unit test in the repository.
    ///
    /// ⚠️ IT ALSO WRITES THE NUMBERS THIS MACHINE ACTUALLY MEASURED to `Logs/`, because the
    /// sampler's output is the point of it and an assertion that it is non-zero says nothing
    /// about whether it is plausible. `HudPerformanceProbe` does the same beside it and for the
    /// same reason.
    /// ⚠️⚠️ **DO NOT READ THAT FILE AS THIS MACHINE'S FRAME RATE.** A PlayMode run is a batchmode
    /// editor with the test runner in it; `docs/TODO.md` § 17's whole argument is that a frame
    /// rate measured by a probe is not a frame rate measured by a player, which is why
    /// `FrameCapProbe` exists in the shipped executable instead.
    /// </summary>
    public class MatchFrameRateProbe
    {
        private const string OutPath = "Logs/match-frame-rate.txt";

        private GameMode _savedMode;
        private bool _savedAllBots;
        private bool _savedTelemetry;

        [SetUp]
        public void SetUp()
        {
            _savedMode = SceneFlow.SelectedMode;
            _savedAllBots = GameLaunch.AllBots;
            _savedTelemetry = SettingsStore.Current.TelemetryEnabled;

            // ⚠️ THE SAMPLER IS BEHIND THE OPT-OUT, so a run with the setting off measures
            // nothing and passes nothing. `docs/TODO.md` § 90.3: turning it off stops the
            // COUNTING and not only the sending, and `ATelemetryOptOutStopsTheCounting` below is
            // the half of this probe that asserts it.
            SettingsStore.Current.TelemetryEnabled = true;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1.0f;

            // ⚠️ `Time.captureDeltaTime` IS GLOBAL AND PERSISTS ACROSS SCENE LOADS, so leaving it
            // set would hand every later test in the run a fixed clock. `BotBehaviourProbe` clears
            // it in its own teardown for the same reason and records why.
            Time.captureDeltaTime = 0.0f;

            SettingsStore.Current.TelemetryEnabled = _savedTelemetry;
            SceneFlow.SelectedMode = _savedMode;
            GameLaunch.AllBots = _savedAllBots;
            GameServices.Round?.EndRound();
            GameServices.Match?.ResetForNewMatch();
            GameServices.Round?.ResetForNewMatch();
        }

        private static IEnumerator LoadArenaAndBegin()
        {
            SceneFlow.SelectedMode = GameMode.Classic;
            GameLaunch.AllBots = true;

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");
            for (int i = 0; i < 30; i++) yield return null;

            var runner = UnityEngine.Object.FindFirstObjectByType<SliceRunner>();
            Assert.IsNotNull(runner, "the arena built no SliceRunner");
            if (!runner.Running) runner.Begin();

            for (int i = 0; i < 10; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator ALiveRoundFillsTheSampleAndTheGapsBetweenRoundsDoNot()
        {
            yield return LoadArenaAndBegin();

            var stats = GameServices.Stats;
            var round = GameServices.Round;
            Assert.IsNotNull(stats, "there is no MatchStatsCollector");
            Assert.IsNotNull(round, "there is no RoundDirector");
            Assert.IsTrue(round.RoundActive, "the slice did not start a round");

            long atStart = stats.FrameRate.Frames;
            for (int i = 0; i < 120; i++) yield return null;
            long afterLiveRound = stats.FrameRate.Frames;

            Assert.Greater(afterLiveRound, atStart,
                "120 frames of a live round added nothing to the sample, so the sampler is not " +
                "running at all");

            // ⚠️ THE FLOOR IS 100 OF 120 RATHER THAN 120, because a frame in which the round
            // ends between the sample and the yield is a real frame that legitimately does not
            // count. Asserting equality would make this probe fail on a timing detail it is not
            // asking about.
            Assert.GreaterOrEqual(afterLiveRound - atStart, 100L,
                "a live round dropped more than a sixth of its frames from the sample");

            // ⚠️⚠️ THIS IS THE ASSERTION THE WHOLE PROBE EXISTS FOR. `RoundActive` is false
            // through the scene load, the gaps between rounds and the whole results board, and
            // `docs/TODO.md` § 90.3 left the frame rate open on exactly that point: a percentile
            // taken over everything includes a loading screen, and a loading screen renders at
            // whatever it likes.
            round.EndRound();
            Assert.IsFalse(round.RoundActive, "EndRound left the round active");

            long atRoundEnd = stats.FrameRate.Frames;
            for (int i = 0; i < 120; i++) yield return null;

            Assert.AreEqual(atRoundEnd, stats.FrameRate.Frames,
                "the sample kept growing after the round ended, so it is counting menu, " +
                "intermission and loading-screen frames as match frames");
        }

        /// <summary>
        /// ⚠️⚠️ AN OPT-OUT THAT ONLY GATES THE UPLOAD IS NOT AN OPT-OUT. `docs/TODO.md` § 90.3:
        /// a buffer that fills anyway is a buffer a later version can decide to flush. The
        /// histogram is 256 integers rather than a growing list, so this gate buys nothing in
        /// memory and is purely the promise being kept where it would be easiest not to.
        /// </summary>
        [UnityTest]
        public IEnumerator ATelemetryOptOutStopsTheCountingAndNotOnlyTheSending()
        {
            yield return LoadArenaAndBegin();

            var stats = GameServices.Stats;
            Assert.IsNotNull(stats);
            Assert.IsTrue(GameServices.Round.RoundActive);

            SettingsStore.Current.TelemetryEnabled = false;

            long atOptOut = stats.FrameRate.Frames;
            for (int i = 0; i < 120; i++) yield return null;

            Assert.AreEqual(atOptOut, stats.FrameRate.Frames,
                "frames were still being counted with Share Anonymous Stats off");
        }

        /// <summary>
        /// ⚠️⚠️ THIS TEST WAS WRITTEN TO ASSERT THE OPPOSITE OF WHAT IT NOW ASSERTS, AND THE
        /// MEASUREMENT IS WHY. `MatchStatsCollector.SampleFrameRate`'s header claimed that under
        /// `Time.captureDeltaTime` both clocks read the captured value, so a probe run would fill
        /// the histogram with a tidy 60 fps. That was read out of documentation rather than
        /// measured. **Under a captured step of 16.67 ms the sample read 2.13 ms per frame**,
        /// which is the batchmode editor's real wall clock: about 469 fps. Every probe in this
        /// repository drives its match with a captured step, so every one of them fills this
        /// histogram with a number that has nothing to do with a player's machine.
        ///
        /// ⚠️⚠️ IT IS HARMLESS FOR EXACTLY ONE REASON AND THIS ASSERTS THAT REASON TOO.
        /// `TelemetrySink.Flush` returns immediately when no account is signed in and a probe
        /// never signs in, so nothing a probe measures has ever left the machine. `BatchesSent`
        /// is the sink's own count of batches that reached the wire, and it must be zero here.
        ///
        /// ⚠️ **DO NOT "FIX" THE SAMPLER TO READ THE CAPTURED STEP.** A fabricated 60 fps in the
        /// sample of some future run that DOES sign in is a worse failure than an obviously silly
        /// one: 469 fps is visibly not a player, and a clean 60 is indistinguishable from one.
        /// </summary>
        [UnityTest]
        public IEnumerator UnderACapturedStepTheSampleReadsWallClockAndNothingSendsIt()
        {
            yield return LoadArenaAndBegin();

            var stats = GameServices.Stats;
            Assert.IsNotNull(stats);
            Assert.IsTrue(GameServices.Round.RoundActive);

            const float step = 1.0f / 60.0f;
            Time.captureDeltaTime = step;

            long framesBefore = stats.FrameRate.Frames;
            double secondsBefore = stats.FrameRate.Seconds;

            for (int i = 0; i < 120; i++) yield return null;

            long frames = stats.FrameRate.Frames - framesBefore;
            double seconds = stats.FrameRate.Seconds - secondsBefore;
            Time.captureDeltaTime = 0.0f;

            Assert.Greater(frames, 0L, "nothing was sampled under a captured step");

            // The mean of the added durations rather than a percentile, because the running sum
            // is exact and the buckets are 0.5 ms wide.
            double perFrame = seconds / frames;
            Debug.Log($"[MatchFrameRate] under a captured step of {step:F5} s the sample read " +
                      $"{perFrame:F5} s per frame ({1.0 / perFrame:F0} fps).");

            Assert.Less(perFrame, step * 0.5,
                $"a captured step of {step:F5} s was sampled as {perFrame:F5} s per frame, which " +
                "is close enough to the captured value that Time.unscaledDeltaTime now DOES " +
                "follow Time.captureDeltaTime. That is the opposite of what was measured on " +
                "2026-08-30, so the comment in MatchStatsCollector.SampleFrameRate and this test " +
                "are both describing the old behaviour and need rewriting together.");

            var telemetry = GameServices.Telemetry;
            Assert.IsNotNull(telemetry, "there is no TelemetrySink");
            Assert.AreEqual(0, telemetry.BatchesSent,
                "a probe sent a telemetry batch. The whole reason a batchmode frame rate in this " +
                "histogram is harmless is that a probe never signs in, so TelemetrySink.Flush " +
                "returns before it reaches the wire. If that stops being true, a few hundred " +
                "fabricated frames per second are now in the real distribution.");
        }

        /// <summary>
        /// The numbers themselves, so the shape of the output is looked at rather than assumed.
        /// </summary>
        [UnityTest]
        public IEnumerator TheSampleReportsAPlausibleDistributionAndABandThatExists()
        {
            yield return LoadArenaAndBegin();

            var stats = GameServices.Stats;
            Assert.IsNotNull(stats);

            for (int i = 0; i < 300; i++) yield return null;

            var sample = stats.FrameRate;
            Assert.Greater(sample.Frames, 0L, "the sample is empty");

            double average = sample.AverageFps;
            double median = sample.LowFps(50.0);
            double fivePerCentLow = sample.LowFps(5.0);
            double onePerCentLow = sample.LowFps(1.0);
            string band = FrameRateBands.Band(median);

            Directory.CreateDirectory("Logs");
            File.WriteAllText(OutPath,
                "PER-MATCH FRAME RATE, AS SAMPLED IN A LIVE ROUND\n" +
                "⚠️ A PlayMode run is not a player. docs/TODO.md § 17 and FrameCapProbe.\n" +
                $"frames   : {sample.Frames}\n" +
                $"seconds  : {sample.Seconds:F2}\n" +
                $"fps_avg  : {average:F1}\n" +
                $"fps_p50  : {median:F1}\n" +
                $"fps_p5   : {fivePerCentLow:F1}\n" +
                $"fps_p1   : {onePerCentLow:F1}\n" +
                $"band     : {band}\n");

            Debug.Log($"[MatchFrameRate] {sample.Frames} frames, avg {average:F1}, " +
                      $"p50 {median:F1}, p5 {fivePerCentLow:F1}, p1 {onePerCentLow:F1}, " +
                      $"band {band}. Wrote {OutPath}.");

            Assert.Greater(average, 0.0, "the average frame rate came back as zero or worse");

            // ⚠️ THE ORDERING IS THE ONLY THING WORTH ASSERTING ABOUT THE VALUES THEMSELVES. A
            // batchmode editor's frame rate is whatever the machine and the test runner make it,
            // so any bound on the number would be a bound on this laptop's mood. That the worst
            // hundredth is no faster than the median is true of every possible run, and it is
            // exactly what breaks if the percentile argument is ever read from the wrong end.
            Assert.LessOrEqual(onePerCentLow, fivePerCentLow + 0.001,
                "the 1 per cent low came back faster than the 5 per cent low, so the percentile " +
                "is being read from the wrong end");
            Assert.LessOrEqual(fivePerCentLow, median + 0.001,
                "the 5 per cent low came back faster than the median");

            CollectionAssert.Contains(FrameRateBands.All, band,
                "the band is a name nothing on the server side knows");
        }
    }
}
