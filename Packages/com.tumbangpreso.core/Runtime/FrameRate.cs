using System;

namespace TumbangPreso.Core
{
    /// <summary>
    /// Frame durations counted into fixed buckets, so a whole match's frame rate costs 256
    /// integers and no allocation at all.
    ///
    /// ⚠️⚠️ IT IS A HISTOGRAM AND NOT A LIST OF FRAMES, AND THAT IS THE ONLY REASON THIS CAN RUN
    /// IN A SHIPPED MATCH. An eight-round Hero Strike set is 720 seconds, which is about 43,000
    /// frames at 60 fps: kept as a growing list that is 170 KB of managed memory reallocated
    /// through a dozen doublings while somebody is playing, and `HudPerformanceProbe` exists
    /// because a single HUD string rebuilt per frame already cost the 6x probe an eighth of its
    /// frames. Counting into a fixed array costs one index and one increment, forever.
    ///
    /// ⚠️ THE PERCENTILES ARE THEREFORE BUCKET-ACCURATE RATHER THAN EXACT. Half a bucket is
    /// 0.25 ms, so the worst error is 0.8 per cent at 30 fps, 1.5 per cent at 60 and 3.6 per cent
    /// at 144: it grows with the frame rate, because a fixed slice of TIME is a widening slice of
    /// RATE. Every question this is built to answer is a band question ("is anybody playing under
    /// 50 fps", `docs/TODO.md` § 17), so the resolution is spent where the band edges are rather
    /// than where the headline number is.
    ///
    /// ⚠️ THE AVERAGE IS EXACT EVEN THOUGH THE PERCENTILES ARE NOT. `Seconds` is a running sum of
    /// what was actually added rather than a sum over the buckets, so `AverageFps` never inherits
    /// the bucketing error. The two disagree slightly by construction, and that is correct.
    /// </summary>
    public sealed class FrameRateHistogram
    {
        /// <summary>The width of one bucket, in seconds.</summary>
        public const double BucketSeconds = 0.0005;

        /// <summary>
        /// How many buckets, so the range is 0 to 128 ms.
        ///
        /// ⚠️ THE LAST BUCKET IS OPEN-ENDED AND HOLDS EVERYTHING SLOWER. A loading hitch, an
        /// alt-tab and a genuinely dying machine all land in it, and it is read back as its LOWER
        /// edge (7.8 fps), so a run full of them reports "at least this bad" rather than a number
        /// invented from a bucket with no top. Clamping instead of overflowing would file a 3 s
        /// hitch in the same place as a 130 ms frame, with nothing left recording that it
        /// happened.
        /// </summary>
        public const int BucketCount = 256;

        private readonly int[] _buckets = new int[BucketCount];

        /// <summary>How many frames have been counted.</summary>
        public long Frames { get; private set; }

        /// <summary>The exact wall clock those frames took, in seconds.</summary>
        public double Seconds { get; private set; }

        /// <summary>Frames divided by the time they took. Exact, not bucketed.</summary>
        public double AverageFps => Frames > 0 && Seconds > 0.0 ? Frames / Seconds : 0.0;

        /// <summary>
        /// Counts one frame of the given duration.
        ///
        /// ⚠️ A NON-POSITIVE OR NON-FINITE DURATION IS DROPPED RATHER THAN COUNTED AS FAST. The
        /// first frame after a scene load, a step taken while the editor is paused and a
        /// `Time.captureDeltaTime` of zero all produce one, and every one of them would otherwise
        /// land in bucket 0 and read back as thousands of frames per second. A frame that took no
        /// time did not happen.
        /// </summary>
        public void Add(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0.0) return;

            int bucket = (int)(seconds / BucketSeconds);
            if (bucket >= BucketCount) bucket = BucketCount - 1;

            _buckets[bucket]++;
            Frames++;
            Seconds += seconds;

            // ⚠️⚠️ THE WORST FRAME IS KEPT EXACTLY, NOT READ BACK OUT OF A BUCKET, BECAUSE THE
            // TOP BUCKET HAS NO CEILING. `FpsForBucket` reads the last bucket at its FLOOR and
            // says so, which is the honest answer for a percentile and useless for "how bad was
            // the worst hitch": every frame from 0.5 s to two seconds reads the same. A single
            // double is what turns "something spiked" into a number somebody can act on.
            if (seconds > MaxSeconds) MaxSeconds = seconds;
        }

        /// <summary>
        /// The longest single frame counted, in seconds. Zero when nothing has been counted.
        ///
        /// ⚠️⚠️ TOURNAMENT PAIN IS A HITCH, NOT A LOW AVERAGE, AND THIS IS THE STATISTIC THAT
        /// SEES ONE. An average frame rate is the one number that cannot: a match that runs at a
        /// steady 90 FPS and drops two 200 ms frames at the moment somebody throws still averages
        /// about 89, and the player who lost the throw is the only one who knows. The percentiles
        /// above narrow it and a single worst frame names it.
        /// </summary>
        public double MaxSeconds { get; private set; }

        /// <summary>
        /// How many frames took at least <paramref name="thresholdSeconds"/>.
        ///
        /// ⚠️ A COUNT AND NOT A RATIO, because one 250 ms stall in a round is a thing that
        /// happened and 0.02 per cent is a number that reads as noise. `docs/TODO.md` § 143 asks
        /// for "long-frame count above useful thresholds" in exactly those words.
        ///
        /// ⚠️ IT IS BUCKET-RESOLUTION AND SAYS SO. The histogram is the storage, so this counts
        /// every frame in a bucket whose FLOOR is at or above the threshold; a threshold falling
        /// inside a bucket is rounded up to that bucket's edge rather than being interpolated,
        /// which keeps the answer a fact about frames rather than an estimate.
        /// </summary>
        public long LongFrames(double thresholdSeconds)
        {
            if (thresholdSeconds <= 0.0) return Frames;

            int first = (int)System.Math.Ceiling(thresholdSeconds / BucketSeconds);
            if (first < 0) first = 0;

            long count = 0;
            for (int bucket = first; bucket < BucketCount; bucket++) count += _buckets[bucket];
            return count;
        }

        public void Clear()
        {
            Array.Clear(_buckets, 0, _buckets.Length);
            Frames = 0;
            Seconds = 0.0;
            MaxSeconds = 0.0;
        }

        /// <summary>
        /// The frame rate that the slowest <paramref name="lowPercent"/> per cent of frames were
        /// at or below. 50 is the median frame, 5 is the "5 per cent low", 1 is the "1 per cent
        /// low". Zero when nothing has been counted.
        ///
        /// ⚠️⚠️ IT IS A PERCENTILE OF FRAME TIME READ BACK AS A RATE, WHICH IS WHY THE ARGUMENT
        /// LOOKS INVERTED. The fifth percentile of frames-per-second is the ninety-fifth
        /// percentile of seconds-per-frame: the same frames, named from the two ends. The
        /// argument here is the share of frames that were WORSE, because that is what anybody
        /// reading this actually asks for ("what does the bad 1 per cent look like"), and because
        /// an average frame rate is the one statistic that cannot see a stutter at all.
        /// </summary>
        public double LowFps(double lowPercent)
        {
            if (Frames <= 0) return 0.0;
            if (lowPercent < 0.0) lowPercent = 0.0;
            if (lowPercent > 100.0) lowPercent = 100.0;

            // How many of the slowest frames the answer has to cover. At least one, so asking for
            // the worst 0.1 per cent of 200 frames names the single worst frame rather than
            // nothing at all.
            long target = (long)Math.Ceiling(Frames * lowPercent / 100.0);
            if (target < 1) target = 1;

            long seen = 0;
            for (int bucket = BucketCount - 1; bucket >= 0; bucket--)
            {
                seen += _buckets[bucket];
                if (seen >= target) return FpsForBucket(bucket);
            }

            return FpsForBucket(0);
        }

        /// <summary>
        /// ⚠️ A BUCKET IS READ AT ITS MIDPOINT, EXCEPT THE LAST, WHICH IS READ AT ITS FLOOR. The
        /// midpoint is the unbiased reading of a closed bucket; the last one has no top, so its
        /// midpoint would be invented. Its floor answers the only honest thing about it, which is
        /// that the frame took at least that long.
        /// </summary>
        private static double FpsForBucket(int bucket)
        {
            double seconds = bucket >= BucketCount - 1
                ? bucket * BucketSeconds
                : (bucket + 0.5) * BucketSeconds;
            return seconds > 0.0 ? 1.0 / seconds : 0.0;
        }
    }

    /// <summary>
    /// The bands a match's frame rate is reported in.
    ///
    /// ⚠️⚠️ THE BOUNDARIES ARE `docs/TODO.md` § 17 AND NOT ROUND NUMBERS. That entry measured the
    /// bots falling off a cliff when the frame step reaches the physics step: at 1/60 s they
    /// throw 40 to 90 times a match and cast 27 to 38 skills, and at 0.02 s, which is 50 fps, the
    /// same build threw 18 times and cast NOTHING. The shipped physics rate is 50 Hz
    /// (`ProjectSettings/TimeManager.asset`), so a 50 Hz panel, vsync on a heavy scene, or a
    /// laptop under load puts a real player in the row with zero casting in it. § 17's first need
    /// is *"reproduce it in the player"*, and the half of that nothing can answer from here is
    /// whether anybody actually plays down there. **These bands are what answers it**, so an edge
    /// sits at 50 rather than at a tidier 45, and 50 to 60 is kept as its own narrow band because
    /// "just above the step" is the case nobody has measured in either direction.
    ///
    /// ⚠️ A BAND NAME IS A STORED LABEL AND THEREFORE A CONTRACT, exactly like an event name.
    /// `TelemetryEvents`' header carries the argument: renaming `fps_30_50` restarts a counter at
    /// zero beside a history it can never be joined to again. If a band ever has to change, SPLIT
    /// one at a new edge and name both halves anew; never re-point an existing name at a
    /// different range.
    /// </summary>
    public static class FrameRateBands
    {
        /// <summary>
        /// The frame rate at which one rendered frame takes one whole physics step, 0.02 s.
        ///
        /// ⚠️ IT IS WRITTEN HERE AS A NUMBER BECAUSE THE CORE CANNOT READ `TimeManager.asset`,
        /// and `FrameCapProbe` carries the same understanding for the same reason. If the project
        /// ever moves off a 50 Hz fixed step, this constant and § 17's whole table move with it.
        /// </summary>
        public const double PhysicsStepFps = 50.0;

        public const string UnderThirty = "fps_under_30";
        public const string ThirtyToStep = "fps_30_50";
        public const string StepToSixty = "fps_50_60";
        public const string SixtyToNinety = "fps_60_90";
        public const string NinetyUp = "fps_90_up";

        public static readonly string[] All =
        {
            UnderThirty, ThirtyToStep, StepToSixty, SixtyToNinety, NinetyUp,
        };

        /// <summary>
        /// Which band a match belongs in.
        ///
        /// ⚠️ IT IS CHOSEN FROM THE MEDIAN FRAME AND NOT FROM THE AVERAGE. An average is dragged
        /// up by a long stretch of an empty round and down by one loading hitch, and this label
        /// is answering "what did this match feel like for most of it". The average and the two
        /// lows travel beside it as their own numbers, so nothing is lost by the label being the
        /// boring one.
        /// </summary>
        public static string Band(double medianFps)
        {
            if (medianFps < 30.0) return UnderThirty;
            if (medianFps < PhysicsStepFps) return ThirtyToStep;
            if (medianFps < 60.0) return StepToSixty;
            if (medianFps < 90.0) return SixtyToNinety;
            return NinetyUp;
        }
    }
}
