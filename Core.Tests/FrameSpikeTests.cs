using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The statistics that can see a hitch, which an average frame rate cannot.
    ///
    /// ⚠️⚠️ THE FAILURE THESE EXIST FOR IS A TOURNAMENT ONE AND IT DOES NOT LOOK LIKE A
    /// PERFORMANCE PROBLEM IN ANY EXISTING NUMBER. A match that runs at a steady 90 FPS and drops
    /// two 200 ms frames at the moment somebody throws still averages about 89, and the only
    /// person who knows is the player who lost the throw. `docs/TODO.md` § 143 asks for the
    /// maximum frame time and a long-frame count in those words, and neither existed:
    /// `FrameRate.LowFps` narrows a percentile and `Frames / Seconds` is the average.
    ///
    /// ⚠️ AND THE HISTOGRAM CANNOT ANSWER "HOW BAD WAS THE WORST ONE" BY ITSELF. `BucketSeconds`
    /// is 0.5 ms across `BucketCount` 256, so the top bucket is **128 ms or more, with no
    /// ceiling**, and `FpsForBucket` reads it at its floor and says so. A 130 ms frame and a two
    /// second freeze are the same bucket. That is the right answer for a percentile and useless
    /// for a spike, which is why `MaxSeconds` is kept exactly rather than read back out.
    /// </summary>
    public class FrameSpikeTests
    {
        [Fact]
        public void NothingCountedIsNoSpike()
        {
            var f = new FrameRateHistogram();
            Assert.Equal(0.0, f.MaxSeconds);
            Assert.Equal(0, f.LongFrames(0.1));
        }

        [Fact]
        public void TheWorstFrameIsKeptExactly()
        {
            var f = new FrameRateHistogram();
            f.Add(1.0 / 90.0);
            f.Add(0.2);              // the hitch
            f.Add(1.0 / 90.0);

            Assert.Equal(0.2, f.MaxSeconds, 6);
        }

        [Fact]
        public void AFrameWorseThanTheTopBucketIsStillReportedExactly()
        {
            // ⚠️⚠️ THE CASE THE HISTOGRAM ALONE GETS WRONG. The top bucket is 128 ms upward with
            // no ceiling, so a two second freeze and a 130 ms stutter land in the same place and
            // read back identically. This is the whole reason MaxSeconds is a field.
            var f = new FrameRateHistogram();
            f.Add(0.130);
            Assert.Equal(0.130, f.MaxSeconds, 6);

            f.Add(2.0);
            Assert.Equal(2.0, f.MaxSeconds, 6);
        }

        [Fact]
        public void AnAverageCannotSeeAHitchAndThatIsThePoint()
        {
            // 600 frames at 90 FPS plus two 200 ms stalls. The average barely moves; the spike
            // statistics name it outright.
            var f = new FrameRateHistogram();
            for (int i = 0; i < 600; i++) f.Add(1.0 / 90.0);
            f.Add(0.2);
            f.Add(0.2);

            double averageFps = f.Frames / f.Seconds;
            Assert.InRange(averageFps, 80.0, 90.0);

            Assert.Equal(0.2, f.MaxSeconds, 6);
            Assert.Equal(2, f.LongFrames(0.1));
        }

        [Fact]
        public void LongFramesCountsAtOrAboveTheThreshold()
        {
            var f = new FrameRateHistogram();
            for (int i = 0; i < 10; i++) f.Add(0.010);   // 10 ms, fine
            for (int i = 0; i < 3; i++) f.Add(0.050);    // 50 ms, a visible stutter
            f.Add(0.250);                                // 250 ms, a freeze

            Assert.Equal(4, f.LongFrames(0.050));
            Assert.Equal(1, f.LongFrames(0.100));
            Assert.Equal(0, f.LongFrames(0.500));
        }

        [Fact]
        public void ADroppedFrameDoesNotBecomeASpike()
        {
            // `Add` drops non-positive and non-finite durations, and its own note says why: a
            // frame that took no time did not happen. None of them may show up as the worst one.
            var f = new FrameRateHistogram();
            f.Add(0.0);
            f.Add(-1.0);
            f.Add(double.NaN);
            f.Add(double.PositiveInfinity);

            Assert.Equal(0, f.Frames);
            Assert.Equal(0.0, f.MaxSeconds);
        }

        [Fact]
        public void ClearForgetsTheWorstFrameToo()
        {
            // ⚠️ A MAX THAT SURVIVES A RESET IS THE WORST KIND OF STALE NUMBER: it reports a
            // spike from a match that is over, and the next reader goes looking for it in this
            // one.
            var f = new FrameRateHistogram();
            f.Add(0.5);
            f.Clear();

            Assert.Equal(0.0, f.MaxSeconds);
            Assert.Equal(0, f.LongFrames(0.1));
        }
    }
}
