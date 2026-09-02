using System.Collections.Generic;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The per-match frame-rate sample. `docs/TODO.md` § 90.3's last open bullet.
    ///
    /// ⚠️ THE THING WORTH TESTING HERE IS THAT A STUTTER IS VISIBLE. An average frame rate is the
    /// one statistic that cannot see one, and "the game feels bad on my laptop" is a stutter far
    /// more often than it is a low average. Every assertion below is about a number that stays
    /// honest when the frames are not all the same length.
    /// </summary>
    public sealed class FrameRateTests
    {
        private const double Sixty = 1.0 / 60.0;

        private static FrameRateHistogram Of(double seconds, int count)
        {
            var histogram = new FrameRateHistogram();
            for (int i = 0; i < count; i++) histogram.Add(seconds);
            return histogram;
        }

        [Fact]
        public void AnEmptySampleAnswersZeroRatherThanDividingByIt()
        {
            var histogram = new FrameRateHistogram();

            Assert.Equal(0, histogram.Frames);
            Assert.Equal(0.0, histogram.AverageFps);
            Assert.Equal(0.0, histogram.LowFps(1.0));
            Assert.Equal(0.0, histogram.LowFps(50.0));
        }

        /// <summary>
        /// ⚠️ A FRAME THAT TOOK NO TIME DID NOT HAPPEN. The first frame after a scene load and a
        /// step taken with `Time.captureDeltaTime` at zero both produce one, and counted at face
        /// value each is a frame at infinite speed sitting in the fastest bucket. One of them is
        /// enough to move a median.
        /// </summary>
        [Fact]
        public void AFrameOfNoLengthIsDroppedRatherThanCountedAsAnInfinitelyFastOne()
        {
            var histogram = new FrameRateHistogram();
            histogram.Add(0.0);
            histogram.Add(-1.0);
            histogram.Add(double.NaN);
            histogram.Add(double.PositiveInfinity);

            Assert.Equal(0, histogram.Frames);
            Assert.Equal(0.0, histogram.Seconds);
        }

        [Fact]
        public void ASteadySixtyReadsBackAsSixty()
        {
            var histogram = Of(Sixty, 600);

            Assert.Equal(600, histogram.Frames);
            Assert.Equal(10.0, histogram.Seconds, 3);

            // Exact, because the average is a running sum rather than a walk over the buckets.
            Assert.Equal(60.0, histogram.AverageFps, 6);

            // Bucketed, so within the 0.5 ms resolution the histogram is built on.
            Assert.InRange(histogram.LowFps(50.0), 58.0, 62.0);
            Assert.InRange(histogram.LowFps(1.0), 58.0, 62.0);
        }

        /// <summary>
        /// ⚠️⚠️ THIS IS THE WHOLE REASON THE LOWS ARE SENT AND NOT ONLY THE AVERAGE, AND THE
        /// MEASURED NUMBERS BELOW ARE WORSE THAN THE ARGUMENT FOR IT. Ninety-nine frames at 60
        /// fps and one at 10 is a match that hitches ten times over seventeen and a half seconds,
        /// and it averages **57.1 fps**: under three frames a second off a perfect run, on a
        /// machine that visibly stalls. The median says exactly 60 and only the 1 per cent low
        /// says 10.
        /// </summary>
        [Fact]
        public void TheOnePerCentLowSeesAStutterThatTheAverageCannot()
        {
            var histogram = new FrameRateHistogram();
            for (int i = 0; i < 990; i++) histogram.Add(Sixty);
            for (int i = 0; i < 10; i++) histogram.Add(0.1);

            Assert.InRange(histogram.AverageFps, 56.0, 58.0);
            Assert.InRange(histogram.LowFps(50.0), 58.0, 62.0);
            Assert.InRange(histogram.LowFps(1.0), 9.0, 11.0);
        }

        /// <summary>
        /// ⚠️ THE PERCENTILE IS OF THE FRAMES THAT WERE WORSE, so a bigger argument names a
        /// FASTER frame. `LowFps(50)` is the median and `LowFps(1)` is the worst hundredth, and
        /// the ordering between them is the only way to catch the two being swapped: swapped,
        /// every number still looks plausible and the stutter column reports the good frames.
        /// </summary>
        [Fact]
        public void AWiderPercentileNamesAFasterFrame()
        {
            // 1 per cent of these thousand frames is the ten at 12 fps, 5 per cent reaches into
            // the ninety at 30, and the median is in the nine hundred at 60.
            var histogram = new FrameRateHistogram();
            for (int i = 0; i < 900; i++) histogram.Add(Sixty);
            for (int i = 0; i < 90; i++) histogram.Add(1.0 / 30.0);
            for (int i = 0; i < 10; i++) histogram.Add(1.0 / 12.0);

            Assert.True(histogram.LowFps(1.0) < histogram.LowFps(5.0));
            Assert.True(histogram.LowFps(5.0) < histogram.LowFps(50.0));

            Assert.InRange(histogram.LowFps(1.0), 11.0, 13.0);
            Assert.InRange(histogram.LowFps(5.0), 29.0, 31.0);
            Assert.InRange(histogram.LowFps(50.0), 58.0, 62.0);
        }

        /// <summary>
        /// ⚠️ ASKING FOR A SHARE SMALLER THAN ONE FRAME NAMES THE SINGLE WORST FRAME, not
        /// nothing. A 200-frame match asked for its worst 0.1 per cent is asking about a fifth of
        /// a frame; answering zero there would report a machine that stopped rendering.
        /// </summary>
        [Fact]
        public void AShareSmallerThanOneFrameStillNamesTheWorstFrame()
        {
            var histogram = new FrameRateHistogram();
            for (int i = 0; i < 199; i++) histogram.Add(Sixty);
            histogram.Add(0.05);

            Assert.InRange(histogram.LowFps(0.1), 19.0, 21.0);
        }

        /// <summary>
        /// ⚠️ THE LAST BUCKET IS OPEN-ENDED AND IS READ AT ITS FLOOR, so a three-second hitch is
        /// reported as "at least 7.8 fps" rather than as a number invented from a bucket with no
        /// top. It is optimistic on purpose: the alternative is a made-up ceiling.
        /// </summary>
        [Fact]
        public void AFrameSlowerThanTheLastBucketIsReportedAsAtLeastThatSlow()
        {
            var histogram = new FrameRateHistogram();
            histogram.Add(3.0);

            double floor = 1.0 / ((FrameRateHistogram.BucketCount - 1) * FrameRateHistogram.BucketSeconds);
            Assert.Equal(floor, histogram.LowFps(1.0), 3);

            // The average is not bucketed, so it still knows the frame took three seconds.
            Assert.Equal(3.0, histogram.Seconds, 6);
        }

        [Fact]
        public void ClearingLeavesNothingOfThePreviousMatch()
        {
            var histogram = Of(Sixty, 300);
            histogram.Clear();

            Assert.Equal(0, histogram.Frames);
            Assert.Equal(0.0, histogram.Seconds);
            Assert.Equal(0.0, histogram.LowFps(50.0));
        }

        /// <summary>
        /// ⚠️⚠️ THE BAND EDGES ARE `docs/TODO.md` § 17 AND NOT ROUND NUMBERS. 50 fps is where one
        /// rendered frame takes one whole 0.02 s physics step, and that is the configuration in
        /// which the same build threw 18 times and cast NOTHING against 40 to 90 throws and 27 to
        /// 38 skill uses at 1/60. The reason this telemetry exists is to find out whether real
        /// players are down there, so an edge that drifted off 50 would answer a different
        /// question while looking like the same one.
        /// </summary>
        [Theory]
        [InlineData(12.0, "fps_under_30")]
        [InlineData(29.9, "fps_under_30")]
        [InlineData(30.0, "fps_30_50")]
        [InlineData(49.9, "fps_30_50")]
        [InlineData(50.0, "fps_50_60")]
        [InlineData(59.9, "fps_50_60")]
        [InlineData(60.0, "fps_60_90")]
        [InlineData(89.9, "fps_60_90")]
        [InlineData(90.0, "fps_90_up")]
        [InlineData(240.0, "fps_90_up")]
        public void TheBandEdgesSitWhereTheBotsFallOver(double fps, string band)
        {
            Assert.Equal(band, FrameRateBands.Band(fps));
        }

        [Fact]
        public void TheStepEdgeIsThePhysicsRateRatherThanACoincidence()
        {
            Assert.Equal(50.0, FrameRateBands.PhysicsStepFps);
            Assert.Equal(FrameRateBands.ThirtyToStep,
                         FrameRateBands.Band(FrameRateBands.PhysicsStepFps - 0.1));
            Assert.Equal(FrameRateBands.StepToSixty,
                         FrameRateBands.Band(FrameRateBands.PhysicsStepFps));
        }

        /// <summary>
        /// ⚠️ A BAND NAME IS A STORED LABEL, so it has to survive the same rules every other
        /// label does. `TelemetryRules.Label` refuses rather than truncates, so a band renamed
        /// past the length cap would not be shortened: it would be dropped, and the column would
        /// vanish from every event with nothing anywhere saying so.
        /// </summary>
        [Fact]
        public void EveryBandNameSurvivesTheTelemetryRules()
        {
            foreach (string band in FrameRateBands.All)
                Assert.Equal(band, TelemetryRules.Label(band));
        }

        [Fact]
        public void TheFrameRateEventIsKnownAndIsNotAFunnelStep()
        {
            Assert.True(TelemetryRules.IsKnownEvent(TelemetryEvents.MatchFrameRate));
            Assert.Equal(-1, TelemetryRules.FunnelIndex(TelemetryEvents.MatchFrameRate));
        }

        /// <summary>
        /// ⚠️⚠️ THE EVENT SITS EXACTLY ON `MaxParametersPerEvent` AND THIS IS WHAT NOTICES WHEN A
        /// NINTH COLUMN ARRIVES. `TelemetryRules.Accept` trims the extras rather than refusing
        /// the event, deliberately, so a ninth column is not an error: it is one of the eight
        /// silently gone, chosen by ordinal key order, in a series nobody is watching. This
        /// mirrors `TelemetrySink.NoteFrameRate` column for column; if that method gains one, this
        /// fails first.
        /// </summary>
        [Fact]
        public void TheFrameRateEventKeepsEveryColumnItSends()
        {
            var candidate = new TelemetryEvent { Name = TelemetryEvents.MatchFrameRate };
            candidate.Labels["mode"] = "HeroStrike";
            candidate.Labels["map"] = "Eskinita";
            candidate.Labels["band"] = FrameRateBands.SixtyToNinety;
            candidate.Numbers["fps_avg"] = 58.4;
            candidate.Numbers["fps_p50"] = 59.7;
            candidate.Numbers["fps_p5"] = 41.2;
            candidate.Numbers["fps_p1"] = 22.9;
            candidate.Numbers["frames"] = 21600;

            Assert.True(TelemetryRules.Accept(candidate));
            Assert.Equal(3, candidate.Labels.Count);
            Assert.Equal(5, candidate.Numbers.Count);
            Assert.Equal(TelemetryRules.MaxParametersPerEvent,
                         candidate.Labels.Count + candidate.Numbers.Count);
        }

        /// <summary>
        /// ⚠️ TWO MATCHES WITH DIFFERENT FRAME RATES MUST NOT FOLD INTO ONE ROW. The numbers are
        /// part of an event's signature for exactly this reason, and a frame rate is the most
        /// continuous number this game sends: folding would produce a count of two carrying one
        /// of the two distributions.
        /// </summary>
        [Fact]
        public void TwoMatchesWithDifferentFrameRatesStaySeparateRows()
        {
            var buffer = new Dictionary<string, TelemetryEvent>();

            var first = new TelemetryEvent { Name = TelemetryEvents.MatchFrameRate };
            first.Numbers["fps_p50"] = 59.7;

            var second = new TelemetryEvent { Name = TelemetryEvents.MatchFrameRate };
            second.Numbers["fps_p50"] = 47.1;

            Assert.True(TelemetryRules.Fold(buffer, first));
            Assert.True(TelemetryRules.Fold(buffer, second));
            Assert.Equal(2, buffer.Count);
        }
    }
}
